using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using RorzeApi;
using System.Runtime.InteropServices.ComTypes;
using RorzeComm.Log;
using System.Runtime.CompilerServices;
using RorzeComm;
using RorzeUnit.Interface;
using RorzeUnit.Event;
using RorzeUnit.Class.E84.Enum;
using RorzeUnit.Class.E84.Event;

namespace RorzeUnit.Class.E84
{
    public class FITC_E84 : I_E84
    {
        public dlgb_v dlgAreaTrigger { get; set; }
        public bool AreaTrigger { get { return (dlgAreaTrigger != null && dlgAreaTrigger()); } }
        public int BodyNo { get; private set; }// 對應到Loadport body no
        public bool Simulate { get; private set; }
        public bool Disable { get; private set; }

        private bool m_bFrancesE84Use_D007;

        private bool m_bAutoMode = false;
        public bool GetAutoMode
        {
            get { return m_bAutoMode; }
            private set
            {
                if (m_bAutoMode != value)
                {
                    m_bAutoMode = value;
                    OnAceessModeChange?.Invoke(this, new E84ModeChangeEventArgs(m_bAutoMode));
                }
            }
        }


        public event E84ModeChangeEventHandler OnAceessModeChange;  // Auto/Manual chage    
        public event OccurErrorEventHandler OnOccurError;           // Error chage
        public event OccurErrorEventHandler OnOccurErrorRest;       // Error chage  
        public event EventHandler OnOccurE84InIOChange;             // 收到RC550 GDIO判斷E84IO改變
        public event AutoProcessingEventHandler DoAutoProcessing;   // AutoProcess

        public dlgb_b dlgCheckFoupOn { get; set; }
        public dlgb_b dlgCtrlStgErrorLED { get; set; }
        public dlgb_b dlgCtrlStgLULED { get; set; }


        private enumE84Step eStep = enumE84Step.Ready;
        public enumE84Step E84Step
        {
            get { return eStep; }
            set
            {
                if (eStep != value)
                {
                    //狀態改變 新狀態是異常要處理:OccurAlarm
                    switch (value)
                    {
                        case enumE84Step.TimeoutTD:
                            SendAlmMsg(enumE84Warning.TD0_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp1:
                            SendAlmMsg(enumE84Warning.TP1_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp2:
                            SendAlmMsg(enumE84Warning.TP2_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp3:
                            SendAlmMsg(enumE84Warning.TP3_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp4:
                            SendAlmMsg(enumE84Warning.TP4_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp5:
                            SendAlmMsg(enumE84Warning.TP5_TimeOut);
                            break;
                        case enumE84Step.SignalError:
                            SendAlmMsg(enumE84Warning.SignalError);
                            break;
                        case enumE84Step.StageBusy:
                            SendAlmMsg(enumE84Warning.StageIsBusy);
                            break;
                        case enumE84Step.LightCurtain:
                            SendAlmMsg(enumE84Warning.LightCurtain);
                            break;
                        case enumE84Step.LightCurtainBusyOn:
                            SendAlmMsg(enumE84Warning.LightCurtainBusyOn);
                            break;
                    }

                    //改變狀態 舊狀態是異常:RestAlarm
                    switch (eStep)
                    {
                        case enumE84Step.TimeoutTD: RestAlmMsg(enumE84Warning.TD0_TimeOut); break;
                        case enumE84Step.TimeoutTp1: RestAlmMsg(enumE84Warning.TP1_TimeOut); break;
                        case enumE84Step.TimeoutTp2: RestAlmMsg(enumE84Warning.TP2_TimeOut); break;
                        case enumE84Step.TimeoutTp3: RestAlmMsg(enumE84Warning.TP3_TimeOut); break;
                        case enumE84Step.TimeoutTp4: RestAlmMsg(enumE84Warning.TP4_TimeOut); break;
                        case enumE84Step.TimeoutTp5: RestAlmMsg(enumE84Warning.TP5_TimeOut); break;
                        case enumE84Step.SignalError: RestAlmMsg(enumE84Warning.SignalError); break;
                        case enumE84Step.StageBusy: RestAlmMsg(enumE84Warning.StageIsBusy); break;
                        case enumE84Step.LightCurtain: RestAlmMsg(enumE84Warning.LightCurtain); break;
                        case enumE84Step.LightCurtainBusyOn: RestAlmMsg(enumE84Warning.LightCurtainBusyOn); break;
                    }

                    eStep = value;
                }
            }
        }

        private enumE84Proc m_E84_Proc = enumE84Proc.Loading;
        public enumE84Proc E84_Proc { get { return m_E84_Proc; } set { m_E84_Proc = value; } }

        private DateTime[] m_tmrTP = new DateTime[5];//沒用
        public DateTime[] TmrTP { get { return m_tmrTP; } set { m_tmrTP = value; } }//沒用

        private DateTime m_tmrTD;//沒用
        public DateTime TmrTD { get { return m_tmrTD; } set { m_tmrTD = value; } }//沒用

        private bool m_bResetFlag = false;//沒用
        public bool ResetFlag { get { return m_bResetFlag; } set { m_bResetFlag = value; } }//沒用
        public int HCLID { get; private set; }//沒用

        #region =========================== 計時器，計算 tp timeout
        public bool isTimeoutTD() { return false; }
        public bool isTimeoutTP1() { return false; }
        public bool isTimeoutTP2() { return false; }
        public bool isTimeoutTP3() { return false; }
        public bool isTimeoutTP4() { return false; }
        public bool isTimeoutTP5() { return false; }
        #endregion


        private SerialPort m_comport = new SerialPort();
        private bool m_bSimulate = false;

        private string m_strResp = "";


        private bool m_bCs0_Active = false;
        private bool m_bGo_Active = false;
        private bool m_bBusy_Active = false;
        private bool m_HO_AVBL_Active = false;
        private bool m_bCOMPT_Active = false;
        private bool m_bError = false;
        private string m_strErrorCode = "";

        private bool[] m_bTpTimeoutFlag = new bool[5] { false, false, false, false, false };
        private ManualResetEvent m_mutWaitResponse = new ManualResetEvent(false);
        private object m_lockCmd = new object();
        private object m_lockRecv = new object();

        private static string m_strBuffer = "";

        public SLogger _logger;
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[E84_{0}] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        public FITC_E84(int nBodyNo, bool bDisable, int nComport, bool bSimulate, bool francesE84Use_D007)
        {
            BodyNo = nBodyNo;
            Disable = bDisable;
            Simulate = bSimulate;
            m_bFrancesE84Use_D007 = francesE84Use_D007;

            if (false == bSimulate && nComport != 0)
            {
                if (false == openComport(nComport))
                {
                    System.Environment.Exit(Environment.ExitCode);
                }
            }
            else
            {
                m_bSimulate = true;
            }


            if (Disable) return;

            _logger = SLogger.GetLogger("E84Log");
            TestConnect1();
            TestConnect2();
            getE84Signal();

        }

        ~FITC_E84()
        {
            closeComport();
        }

        public bool openComport(int nCom)
        {
            bool bSucc = false;
            try
            {
                m_comport.PortName = "COM" + nCom.ToString();
                m_comport.BaudRate = 115200;
                m_comport.Parity = Parity.None;
                m_comport.StopBits = StopBits.One;
                m_comport.DataBits = 8;

                m_comport.DataReceived += new SerialDataReceivedEventHandler(GciReceivedData);
                m_comport.Open();
                m_comport.DiscardInBuffer();
                m_comport.DiscardOutBuffer();
                bSucc = m_comport.IsOpen;
            }
            catch (Exception e)
            {
                string strTile = "[E84 Comport " + nCom.ToString() + " create error]";
                MessageBoxButtons myBtn = MessageBoxButtons.OK;
                MessageBoxIcon myIcon = MessageBoxIcon.Error;
                MessageBox.Show(e.ToString(), strTile, myBtn, myIcon);
                bSucc = false;
            }
            return bSucc;
        }

        public void closeComport()
        {
            if (m_comport != null && m_comport.IsOpen)
            {
                m_comport.DataReceived -= new SerialDataReceivedEventHandler(GciReceivedData);
                m_comport.DiscardInBuffer();
                m_comport.DiscardOutBuffer();
                m_comport.Close();
            }
        }

        public string SendCmd(string strCmd)
        {
            lock (m_lockCmd)
            {
                m_mutWaitResponse.Reset();
                m_strResp = "";

                WriteLog("[Send] " + strCmd);

                byte[] enCmd = cmd_encoder(strCmd);
                m_comport.Write(enCmd, 0, enCmd.Length);
                bool bSucc = m_mutWaitResponse.WaitOne(3000);
                string strResp = m_strResp;
                return strResp;
            }
        }

        private byte[] cmd_encoder(string strCmd)
        {
            byte[] cmd_En = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                string ss = strCmd.Substring(2 * i, 2);
                cmd_En[i] = Convert.ToByte(Convert.ToInt32(ss, 16));
            }
            return cmd_En;
        }

        private void GciReceivedData(object sender, SerialDataReceivedEventArgs e)
        {
            int nByteToRead = (sender as SerialPort).BytesToRead;

            lock (m_lockRecv)
            {
                int nIdx = 0;
                StringBuilder sb = new StringBuilder();
                sb.Append(m_strBuffer);

                //List<int> lstPosEndChar = new List<int>();

                while (nIdx < nByteToRead)
                {
                    int nHex = m_comport.ReadByte();
                    sb.Append(nHex.ToString("X2"));
                    nIdx++;
                }

                string strTotal = sb.ToString();

                WriteLog("[Recv] " + strTotal);

                int nPos = -1;
                while ((nPos = strTotal.IndexOf("BB")) >= 0)
                {
                    int nStart = strTotal.IndexOf("AA");


                    string strMsg = strTotal.Substring(nStart, nPos + 2);
                    ProcessRecvDate(strMsg);
                    strTotal = strTotal.Substring(nPos + 2);
                }

                if (strTotal != "")
                    m_strBuffer = strTotal;
            }
        }

        public bool E84error { get { return m_bError; } }


        private void ProcessRecvDate(string strData)
        {
            if (strData.IndexOf("AA00") >= 0)
            {
                //問版本
            }
            else
            {
                if (m_bFrancesE84Use_D007)
                {
                    if (strData.Length != 10)//AA __ __ __ BB 5*Byte
                    {
                        m_bError = true;
                        WriteLog("Receive message count error:" + strData);
                        return;
                    }
                }
                else
                {
                    if (strData.Length != 12)//AA __ __ __ __ BB 6*Byte 
                    {
                        m_bError = true;
                        WriteLog("Receive message count error:" + strData);
                        return;
                    }
                }
            }


            if (m_bFrancesE84Use_D007)
            {
                if (strData.Length != 10)//AA __ __ __ BB 5*Byte
                {
                    m_bError = true;
                    WriteLog("Receive message count error:" + strData);
                    return;
                }
            }
            else
            {
                if (strData.Length != 12)//AA __ __ __ __ BB 6*Byte 
                {
                    m_bError = true;
                    WriteLog("Receive message count error:" + strData);
                    return;
                }
            }

            if (strData.IndexOf("AA55") >= 0)
            {
                m_strErrorCode = strData.Substring(4, 2);
                string strError = "E84_" + m_strErrorCode.ToString() + "_ Evnt :" + strData;

                WriteLog(strError);

                ProcessEvent(strData);
            }
            else if (strData.IndexOf("AA") >= 0)
            {
                m_strResp = strData;
                m_mutWaitResponse.Set();
            }
            else
            {
                WriteLog("Unknown E84 message:" + strData);
                m_bError = true;
            }
        }
        public bool TestConnect1()//問版本
        {
            string strCmd = "550000BB";
            WriteLog("[CMD] Ask version ");

            if (m_bSimulate)
                return true;

            string strReply = SendCmd(strCmd);

            if (strReply.IndexOf("00D007") > 0)
            {
                m_bFrancesE84Use_D007 = true;
            }
            return (strReply.Length > 0);
        }
        public bool TestConnect2()//問版本
        {
            string strCmd = "550001BB";
            WriteLog("[CMD] Ask version ");

            if (m_bSimulate)
                return true;

            string strReply = SendCmd(strCmd);
            return (strReply.Length > 0);
        }
        private bool setAutoMode()
        {
            bool bSucc = false;
            string strCmd = "550100BB";
            WriteLog("[CMD] Set auto-mode ");

            if (m_bSimulate)
            {
                GetAutoMode = true;
                return true;
            }
            string strReply = SendCmd(strCmd);
            if (strReply.IndexOf("AA0100") >= 0)
            {
                WriteLog("Set auto-mode success");
                bSucc = true;
                GetAutoMode = true;
            }
            else if (strReply.IndexOf("AA0101") >= 0)
            {
                WriteLog("Set auto-mode failure");
                WriteLog("The environment signal is not ready.");
            }
            else if (strReply.IndexOf("AA0102") >= 0)
            {
                WriteLog("Set auto-mode failure");
                WriteLog("Is not in  Manual Mode or STANDBY mode.");
            }
            return bSucc;
        }
        private bool setManualMode()
        {
            bool bSucc = false;
            string strCmd = "550200BB";
            WriteLog("[CMD] Set manual-mode ");

            if (m_bSimulate)
            {
                GetAutoMode = false;
                return true;
            }
            string strReply = SendCmd(strCmd);
            if (strReply.IndexOf("AA0200") > 0)
            {
                WriteLog("Set manual-on success");
                bSucc = true;
                GetAutoMode = false;
            }
            else if (strReply.IndexOf("AA0201") > 0)
            {
                WriteLog("E84-Timing , can't cut to Manual mode.");
            }
            return bSucc;
        }



        public bool getE84Signal()
        {
            bool bSucc = false;
            string strCmd = "559000BB";
            WriteLog(string.Format("[CMD] Get E84 Signal."));

            if (m_bSimulate) return true;

            string strReply = SendCmd(strCmd);
            if (m_bFrancesE84Use_D007 == false)//新版才有的功能
                if (strReply.IndexOf("90") >= 0)
                {
                    WriteLog(string.Format("[CMD] Get E84 Signal success."));
                    bSucc = true;

                    string strInput = strReply.Substring(4, 2);
                    bool bCS0 = isBitOn(1, strInput);
                    bool bBusy = isBitOn(5, strInput);
                    m_bCs0_Active = bCS0;
                    m_bBusy_Active = bBusy;
                    WriteLog(string.Format("[CMD] CS0_{0} Busy_{1}", bCS0, bBusy));
                    string strStatus = strReply.Substring(8, 2);
                    bool bGo = isBitOn(0, strStatus);
                    bool bAuto = isBitOn(4, strStatus);
                    bool bManual = isBitOn(5, strStatus);
                    m_bGo_Active = bGo;
                    GetAutoMode = bAuto;//同步狀態
                    WriteLog(string.Format("[CMD] Auto_{0} Manual_{1} Go_{2}", bAuto, bManual, bGo));
                }

            return bSucc;
        }
        private bool isBitOn(int nBit, string strData)
        {
            char temp;
            int nValue = 0;
            int nIdx = 0;

            nIdx = nBit / 4;
            nBit %= 4;

            temp = strData.ToUpper()[strData.Length - 1 - nIdx];
            if (temp >= 'A' && temp <= 'F')
            {
                nValue = temp - 'A' + 10;
            }
            else if (temp >= '0' && temp <= '9')
            {
                nValue = temp - '0';
            }

            return ((nValue & (0x01 << nBit)) != 0);
        }

        public bool setTpTime(int nIdx, int sec)
        {
            if (Disable) return false;
            bool bSucc = false;
            
            string strHead = (5570 + nIdx).ToString();
            string strTime = sec.ToString("X2");
            string strCmd = strHead + strTime + "BB";

            WriteLog("Set TP" + (nIdx+1).ToString() + " time: " + sec.ToString() + " s");

            if (m_bSimulate) return true;

            string strReply = SendCmd(strCmd);
            string check_string = "7" + nIdx.ToString() + "0000BB";
            if (strReply.IndexOf(check_string) >= 0)
            {
                WriteLog("Set TP-time success");
                bSucc = true;
            }
            else
            {

            }
            return bSucc;
        }

        private int[] m_nTpTime = new int[] { 2, 2, 60, 60, 2 };
        public int[] SetTpTime
        {
            set
            {
                m_nTpTime = value;
                for (int i = 0; i < m_nTpTime.Length; i++)
                {
                    setTpTime(i, m_nTpTime[i]);
                }
            }
        }
        public bool SetAutoMode(bool bOn)
        {
            WriteLog(bOn ? "[CMD] Set auto-mode " : "[CMD] Set manual-mode ");


            bool bSucc = false;

            if (bOn != m_bAutoMode)
            {
                if (false == bOn)//Manual
                {
                    if (isCs0On || isCs1On || isValidOn || isBusyOn || isComptOn)
                    {
                        WriteLog(string.Format("[STG{0}]:   signal on, cannot switch to manual mode", BodyNo));
                    }
                    else
                    {
                        WriteLog(string.Format("[STG{0}]:   switch to Manual mode", BodyNo));
                        bSucc = setManualMode();
                    }
                }
                else//Auto
                {
                    WriteLog(string.Format("[STG{0}]:   switch to Auto mode", BodyNo));
                    bSucc = setAutoMode();
                }
            }
            else
            {
                bSucc = true;
            }
            return bSucc;
        }
        public bool ResetError()
        {
            bool bSignalOn = (isCs0On || isValidOn || isCs1On || isAvblOn
                    || isTrReqOn || isBusyOn || isComptOn || isContOn);

            if (GetAutoMode == false)
            {
                WriteLog("[CMD] Reset E84 mode:" + GetAutoMode);
                return false;
            }
            else if (m_bGo_Active)
            {
                WriteLog("[CMD] Go Signal can not Reset");
                return false;
            }
            //else if (bSignalOn)
            //{
            //    WriteLog("[CMD] Signal On can't reset");
            //    return false;
            //}

            bool bSucc = false;
            string strCmd = "550400BB";
            WriteLog("[CMD] Reset E84 ");

            if (m_bSimulate) return true;

            string strReply = SendCmd(strCmd);
            if (strReply.IndexOf("0400") >= 0)
            {
                WriteLog("Reset E84 success");
                m_bError = false;
                m_bGo_Active = false;
                m_bCs0_Active = false;
                m_bBusy_Active = false;
                m_HO_AVBL_Active = false;
                m_bCOMPT_Active = false;
                bSucc = true;

                RestAlmMsg(enumE84Warning.TP3_TimeOut);
                RestAlmMsg(enumE84Warning.TP4_TimeOut);

            }
            else
            {
                WriteLog("Reset E84 fail.");
            }
            return bSucc;

        }
        public void ClearSignal() { }






        private void ProcessEvent(string strData)
        {
            string strCode = strData.Substring(strData.IndexOf("AA55") + 4, 2);
            int nCode = Convert.ToInt32(strCode, 16);

            //string strError = "";
            string strDspMsg = "E84 Event Data =" + strCode + " ";

            switch (nCode)
            {
                case 0x06:                           //Event Data --------------------------                    
                    GetAutoMode = true;
                    strDspMsg += "STANDBY FOR E84 PORCEDURE(WAIT CS_0 ON -<Auto Mode>";
                    UpdateStageLoadUnloadLight(BodyNo);
                    UpdateStageAlarmLight(BodyNo, false);
                    break;
                case 0x09:                           //Event Data --------------------------                    
                    GetAutoMode = true;
                    strDspMsg += "STANDBY FOR E84 PORCEDURE(WAIT CS_0 ON -<Auto Mode>";
                    UpdateStageLoadUnloadLight(BodyNo);
                    UpdateStageAlarmLight(BodyNo, false);
                    break;
                case 0x10:
                    m_bGo_Active = true;
                    strDspMsg += "GO ON (OHT Arrival)";
                    break;
                case 0x11:
                    m_bCs0_Active = true;
                    m_bCOMPT_Active = false;//V2.003
                    strDspMsg += "CS_0 ON (First Signal from OHT,E84 handshake Start Signal)";
                    break;
                case 0x12:
                    strDspMsg += "VALID ON";
                    break;
                case 0x13:
                    E84_Proc = enumE84Proc.Loading;
                    strDspMsg += "L_REQ ON";
                    break;
                case 0x14:
                    E84_Proc = enumE84Proc.Unloading;
                    strDspMsg += "U_REQ ON";
                    break;
                case 0x15:
                    strDspMsg += "TR_REQ ON";
                    break;
                case 0x16:
                    strDspMsg += "READY ON";
                    break;
                case 0x17:
                    m_bBusy_Active = true;
                    strDspMsg += "BUSY ON";
                    break;
                case 0x18:
                    E84_Proc = enumE84Proc.Loading;
                    strDspMsg += "L_REQ OFF";
                    break;
                case 0x19:
                    E84_Proc = enumE84Proc.Unloading;
                    strDspMsg += "U_REQ OFF";
                    break;
                case 0x1A:
                    m_bBusy_Active = false;
                    strDspMsg += "BUSY OFF";
                    break;
                case 0x1B:
                    strDspMsg += "TR_REQ OFF";
                    break;
                case 0x1C:
                    strDspMsg += "COMPT ON";
                    m_bCOMPT_Active = true;//V2.003
                    break;
                case 0x1D:
                    strDspMsg += "READY OFF";
                    break;
                case 0x1E:
                    strDspMsg += "VALID OFF";
                    break;
                case 0x1F:
                    strDspMsg += "COMPT OFF";
                    m_bCOMPT_Active = false;
                    break;
                case 0x20:
                    m_bCs0_Active = false;
                    strDspMsg += "CS_0 OFF";
                    break;
                case 0x21:
                    m_bGo_Active = false;
                    strDspMsg += "GO OFF (OHT leave)";
                    break;
                case 0x22:
                    strDspMsg += "CONTINUE HAND-OFF";
                    break;
                case 0x24:
                    strDspMsg += "CONT ON";//琺藍希絲新版韌體
                    break;
                case 0x25:
                    strDspMsg += "CONT OFF";//琺藍希絲新版韌體
                    break;
                case 0x26:
                    m_HO_AVBL_Active = true;
                    strDspMsg += "HO-AVBL ON";//琺藍希絲新版韌體
                    break;
                case 0x27:
                    m_HO_AVBL_Active = false;
                    strDspMsg += "HO-AVBL OFF";//琺藍希絲新版韌體
                    break;
                case 0x28:
                    strDspMsg += "CS_1 ON";//琺藍希絲新版韌體
                    break;
                case 0x29:
                    strDspMsg += "CS_1 OFF";//琺藍希絲新版韌體
                    break;
                case 0x2A:
                    strDspMsg += "VS_0 ON";//琺藍希絲新版韌體
                    break;
                case 0x2B:
                    strDspMsg += "VS_0 OFF";//琺藍希絲新版韌體
                    break;
                case 0x2C:
                    strDspMsg += "VS_1 ON";//琺藍希絲新版韌體
                    break;
                case 0x2D:
                    strDspMsg += "VS_1 OFF";//琺藍希絲新版韌體
                    break;
                case 0x30:
                    E84_Proc = enumE84Proc.Loading;
                    strDspMsg += "FOUP transfer(LOADING) START (LOAD FOUP 傳送中感應偵測開始)";
                    break;
                case 0x31:
                    strDspMsg += "PRESENCE SENSOR ON";
                    break;
                case 0x32:
                    strDspMsg += "PLACEMENT SENSOR ON";
                    break;
                case 0x33:
                    strDspMsg += "FOUP sensor all ON,(In LOAD TP3,PS and PL detect by no matter sequence condition)";//琺藍希絲新版韌體
                    break;
                case 0x37:
                    strDspMsg += "FOUP SENSOR current status";//琺藍希絲新版韌體
                    break;
                case 0x38:
                    E84_Proc = enumE84Proc.Unloading;
                    strDspMsg += "FOUP transfer(UNLOADING) START (UNLOAD FOUP 傳送中感應偵測開始)";
                    break;
                case 0x39:
                    strDspMsg += "PLACEMENT SENSOR OFF";
                    break;
                case 0x3A:
                    strDspMsg += "PRESENCE SENSOR OFF";
                    break;
                case 0x3B:
                    strDspMsg += "FOUP sensor all OFF,(In LOAD TP3,PS and PL detect by no matter sequence condition)";//琺藍希絲新版韌體
                    break;
                case 0x3D:
                    strDspMsg += "PS NONE DEFINED";//琺藍希絲新版韌體

                    break;
                case 0x3E:
                    strDspMsg += "PL NONE DEFINED";//琺藍希絲新版韌體

                    break;
                case 0x3F:
                    strDspMsg += "FOUP SENSOR STATUS CHANGE";//琺藍希絲新版韌體

                    break;
                case 0x58:
                    strDspMsg += "E84 Finish";//琺藍希絲新版韌體
                    if (E84_Proc == enumE84Proc.Loading)
                    {
                        //TsmcInterface.theInst.SendEvent_FoupPlace(BodyNo);
                    }
                    else
                    {
                        //TsmcInterface.theInst.SendEvent_FoupRemove(BodyNo);
                    }
                    break;
                case 0x59:
                    strDspMsg += "customised";//琺藍希絲新版韌體
                    break;
                case 0x60:
                    strDspMsg += "[DI-Event]-[ON/OFF]";//琺藍希絲新版韌體
                    break;
                case 0x61:
                    strDspMsg += "[DO-Event]-[ON/OFF]";//琺藍希絲新版韌體
                    break;
                case 0x62:
                    strDspMsg += "[DI]：General event code for DIx Signal alarm";//琺藍希絲新版韌體
                    break;
                case 0x70:
                    strDspMsg += "HO-AVBL disable by PC Command";//琺藍希絲新版韌體
                    break;
                case 0x72:
                    strDspMsg += "KEY-RESET or DI-RESET (E84-RESET)";
                    break;
                case 0x73:
                    m_bError = false;
                    strDspMsg += "AUTO RECOVER";

                    m_bCs0_Active=false;//這一定要
                    RestAlmMsg(enumE84Warning.TP1_TimeOut);
                    RestAlmMsg(enumE84Warning.TP2_TimeOut);
                    RestAlmMsg(enumE84Warning.TP5_TimeOut);

                    break;
                case 0x78:
                    strDspMsg += "DO-PULSE ON";//琺藍希絲新版韌體
                    break;
                case 0x79:
                    strDspMsg += "DO-PULSE OFF";//琺藍希絲新版韌體
                    break;
                case 0x7C:
                    strDspMsg += "DI#8 ON";//琺藍希絲新版韌體
                    break;
                case 0x7D:
                    strDspMsg += "DI#8 OFF";//琺藍希絲新版韌體
                    break;

                case 0x7E:
                    strDspMsg += "CURRENT DI STATUS";//琺藍希絲新版韌體
                    break;
                case 0x7F:
                    strDspMsg += "CURRENT E84-Sensor INx/OUTx STATUS";//琺藍希絲新版韌體
                    break;

                //case 0x80:                                          
                //    UpdateStageAlarmLight(m_nIdx - 1, true);
                //    strDspMsg += "CS_0 Timeout";
                //    sendTsmcE84Event("Evt23");
                //    break;

                case 0x81://TP Timeout --------------------------        
                    //m_bTd0_Timeout = true;
                    UpdateStageAlarmLight(BodyNo, true);
                    strDspMsg += "TD0 Timeout";
                    break;
                case 0x82:
                    m_bTpTimeoutFlag[0] = true;
                    UpdateStageAlarmLight(BodyNo, true);
                    strDspMsg += "TP1 Timeout";
                    SendAlmMsg(enumE84Warning.TP1_TimeOut);
                    break;
                case 0x83:
                    m_bTpTimeoutFlag[1] = true;
                    UpdateStageAlarmLight(BodyNo, true);
                    strDspMsg += "TP2 Timeout";
                    SendAlmMsg(enumE84Warning.TP2_TimeOut);
                    break;
                case 0x84:
                    m_bTpTimeoutFlag[2] = true;
                    UpdateStageAlarmLight(BodyNo, true);
                    strDspMsg += "TP3-LOAD Timeout";
                    SendAlmMsg(enumE84Warning.TP3_TimeOut);
                    break;
                case 0x86:
                    m_bTpTimeoutFlag[2] = true;
                    UpdateStageAlarmLight(BodyNo, true);
                    strDspMsg += "TP3-UNLOAD Timeout";
                    SendAlmMsg(enumE84Warning.TP3_TimeOut);
                    break;
                case 0x88:
                    m_bTpTimeoutFlag[3] = true;
                    UpdateStageAlarmLight(BodyNo, true);
                    strDspMsg += "TP4 Timeout";
                    SendAlmMsg(enumE84Warning.TP4_TimeOut);
                    break;
                case 0x89:
                    m_bError = true;
                    strDspMsg += "Customize timeout error, named by customer\r\nSUB_CODE1/SUB_CODE2: Customize timeout sub-event code, depends on projec";//琺藍希絲新版韌體         
                    break;
                case 0x8B:
                    m_bTpTimeoutFlag[4] = true;
                    UpdateStageAlarmLight(BodyNo, true);
                    strDspMsg += "TP5 Timeout";
                    SendAlmMsg(enumE84Warning.TP5_TimeOut);
                    break;
                case 0x8C:
                    UpdateStageAlarmLight(BodyNo, true);
                    strDspMsg += "TP6 Timeout";
                    break;                             //TP Timeout --------------------------

                /*case 0xA0:
                    m_bHandshakeError = true;
                    strDspMsg += "Wait GO ON => CS_0,VALID,TR_REQ,BUSY,COMPT anyone ON";          //E84 Signal Error -----------------
                    break;*/
                /*case 0xA1:
                    m_bHandshakeError = true;
                    strDspMsg += "Wait CS_0 ON => GO OFF";
                    break;*/
                /*case 0xA2:
                    m_bHandshakeError = true;
                    strDspMsg += "Wait CS_0 ON => VALID,TR_REQ,BUSY,COMPT anyone ON";
                    break;*/
                case 0xA3:
                    strDspMsg += "Wait VALID ON => GO,CS_0 anyone OFF";
                    m_bError = true;
                    break;
                case 0xA4:
                    strDspMsg += "Wait VALID ON => TR_REQ,BUSY,COMPT anyone ON";
                    m_bError = true;
                    break;
                /*case 0xA5:
                    strDspMsg += "Wait TR_REQ ON => GO,CS_0,VALID anyone OFF";
                    m_bHandshakeError = true;
                    break;*/
                /*case 0xA6:
                    strDspMsg += "Wait TR_REQ ON => BUSY,COMPT anyone ON";
                    m_bHandshakeError = true;
                    break;*/
                case 0xA7:
                    strDspMsg += "Wait BUSY ON => GO,CS_0,VALID,TR_REQ anyone OFF";
                    m_bError = true;
                    break;
                case 0xA8:
                    strDspMsg += "Wait BUSY ON => COMPT ON";
                    m_bError = true;
                    break;
                /*case 0xA9:
                    strDspMsg += "Wait BUSY OFF,TR_REQ OFF,COMPT ON => GO,CS_0,VALID anyone OFF";
                    m_bHandshakeError = true;
                    break;*/
                case 0xAB:
                    strDspMsg += "Wait BUSY ON => Other E84 Timing Input Signal not in right level";//琺藍希絲新版韌體                   
                    m_bError = true;
                    break;
                case 0xAD:
                    strDspMsg += "Wait BUSY,TR_REQ OFF,COMPT ON => Other E84 Timing Input Signal not in right level";//琺藍希絲新版韌體                   
                    m_bError = true;
                    break;
                case 0xAF:
                    strDspMsg += "Wait VALID OFF,COMPT OFF,CS_0 OFF => GO OFF";
                    m_bError = true;
                    break;
                case 0xB0:
                    strDspMsg += "Wait VALID OFF,COMPT OFF,CS_0 OFF => TR_REQ,BUSY anyone ON";
                    m_bError = true;
                    break;
                case 0xC0:
                    strDspMsg += "BUSY ON,LOADING process,wait PS ON => GO,VALID,CS_0,TR_REQ,BUSY anyone OFF";
                    m_bError = true;
                    break;
                case 0xC1:
                    strDspMsg += "BUSY ON,LOADING process,wait PS ON => COMPT ON";
                    m_bError = true;
                    break;
                case 0xC2:
                    strDspMsg += "BUSY ON,LOADING process,wait PL ON => GO,VALID,CS_0,TR_REQ,BUSY anyone OFF";
                    m_bError = true;
                    break;
                case 0xC3:
                    strDspMsg += "BUSY ON,LOADING process,wait PL ON => COMPT ON";
                    m_bError = true;
                    break;
                case 0xC4:
                    strDspMsg += "BUSY ON,UNLOADING process,wait PL OFF => GO,VALID,CS_0,TR_REQ,BUSY anyone OFF";
                    m_bError = true;
                    break;
                case 0xC5:
                    strDspMsg += "BUSY ON,UNLOADING process,wait PL OFF => COMPT ON";
                    m_bError = true;
                    break;
                case 0xC6:
                    strDspMsg += "BUSY ON,UNLOADING process,wait PS OFF => GO,VALID,CS_0,TR_REQ,BUSY anyone OFF";
                    m_bError = true;
                    break;
                case 0xC7:
                    strDspMsg += "BUSY ON,UNLOADING process,wait PS OFF => COMPT ON";             //E84 Signal Error -----------------
                    m_bError = true;
                    break;
                case 0xD0:
                    strDspMsg += "Wait GO ON => Presence Sensor(PS) or Placement Sensor(PL) Signal Error";     //FOUP Sensor Error -----------------
                    m_bError = true;
                    break;
                case 0xD1:
                    strDspMsg += "Wait CS_0 ON => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xD2:
                    strDspMsg += "Wait VALID ON => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xD3:
                    strDspMsg += "TA1 period => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xD4:
                    strDspMsg += "Wait TR_REQ ON => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xD5:
                    strDspMsg += "TA2 period => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xD6:
                    strDspMsg += "Wait BUSY ON => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xDC:
                    strDspMsg += "LOADING process,wait PS ON detected PL ON";
                    m_bError = true;
                    break;
                case 0xDD:
                    strDspMsg += "LOADING process,wait PL OFF detected PS OFF";
                    m_bError = true;
                    break;
                case 0xDE:
                    strDspMsg += "UNLOADING process,wait PL OFF detected PS OFF";
                    m_bError = true;
                    break;
                case 0xDF:
                    strDspMsg += "UNLOADING process,wait PS OFF detected PL ON";
                    m_bError = true;
                    break;
                case 0xE0:
                    strDspMsg += "Wait BUSY OFF => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xE1:
                    strDspMsg += "TA3 period => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xE2:
                    strDspMsg += "Wait TR_REQ OFF => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xE3:
                    strDspMsg += "Wait COMPT ON => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xE4:
                    strDspMsg += "Wait VALID OFF => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xE5:
                    strDspMsg += "Wait COMPT OFF => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xE6:
                    strDspMsg += "Wait CS_0 OFF => PS or PL Signal Error";
                    m_bError = true;
                    break;
                case 0xE7:
                    strDspMsg += "Wait GO OFF => PS or PL Signal Error";             //FOUP Sensor Error -----------------
                    m_bError = true;
                    break;
                case 0xF0:
                    strDspMsg += "ES ERROR";//琺藍希絲新版韌體                   
                    m_bError = true;
                    break;
                case 0xF1:
                    strDspMsg += "EQ-ALARM ERROR";//琺藍希絲新版韌體                   
                    break;
                case 0xF2:
                    strDspMsg += "CLAMP ON(NOT E84 ERROR,but OHT not available)";                 //Other Type Error -----------------
                    break;
                case 0xF3:
                    strDspMsg += "CLAMP OFF";
                    break;
                case 0xF4:
                    strDspMsg += "CLAMP ON ERROR";
                    break;
                case 0xF5:
                    strDspMsg += "Light Curtain ON(NOT E84 ERROR,but OHT not available)";
                    break;
                case 0xF6:
                    strDspMsg += "Light Curtain OFF";
                    break;
                case 0xF7:
                    strDspMsg += "Light Curtain ON ERROR";
                    break;
                case 0xF8:
                    strDspMsg += "EQ ALARM ON";
                    break;
                case 0xF9:
                    strDspMsg += "EQ ALARM OFF";//琺藍希絲新版韌體                   
                    break;
                case 0xFA:
                    strDspMsg += "ES ON";//琺藍希絲新版韌體                   
                    break;
                case 0xFB:
                    strDspMsg += "ES ERROR";
                    break;
                case 0xFC:
                    strDspMsg += "POWER ERRPR";
                    break;
                case 0xFD:
                    strDspMsg += "MANUAL MODE(NOT E84 ERROR,but OHT not available)";
                    GetAutoMode = false;
                    break;
                case 0xFE:
                    strDspMsg += "AUTO MODE";
                    GetAutoMode = true;
                    break;
                case 0xFF:
                    strDspMsg += "Environment Signal not Ready for Standby Condition(ext. BUSY not in OFF status...)";
                    break;
                default:
                    strDspMsg += "??????--";
                    break;
            }

            WriteLog("[Evnt] " + strDspMsg);


        }




        private void UpdateStageLoadUnloadLight(int nIdx)
        {
            //RV_Stage myStage = GMotion.theInst.STG[nIdx];
            //if (myStage.isFoupExist())
            //{
            //    myStage.SetLight_Load(eStageLight.Off);
            //    if (myStage.isClamped() || myStage.isDocked())
            //    {
            //        myStage.SetLight_Unload(eStageLight.Off);
            //    }
            //    else
            //    {
            //        myStage.SetLight_Unload(eStageLight.On);
            //    }
            //}
            //else
            //{
            //    myStage.SetLight_Load(eStageLight.On);
            //    myStage.SetLight_Unload(eStageLight.Off);
            //}
            if (dlgCtrlStgLULED != null)
                dlgCtrlStgLULED(true);
        }

        private void UpdateStageAlarmLight(int nIdx, bool bAlarm)
        {
            //RV_Stage myStage = GMotion.theInst.STG[nIdx];

            //if (bAlarm)
            //    myStage.SetLight_Error(eStageLight.On);
            //else
            //    myStage.SetLight_Error(eStageLight.Off);

            if (dlgCtrlStgErrorLED != null)
                dlgCtrlStgErrorLED(bAlarm);



        }




        #region IO input output get
        public bool isValidOn { get; }
        public bool isCs0On { get { return m_bCs0_Active; } }
        public bool isCs1On { get; }
        public bool isAvblOn { get; }
        public bool isTrReqOn { get; }
        public bool isBusyOn { get { return m_bBusy_Active; } }
        public bool isComptOn { get { return m_bCOMPT_Active; } }
        public bool isContOn { get; }

        public bool isSetLReq { get; }
        public bool isSetUReq { get; }
        public bool isSetVa { get; }
        public bool isSetReady { get; }
        public bool isSetVs0 { get; }
        public bool isSetVs1 { get; }
        public bool isSetAvbl { get; }
        public bool isSetEs { get; }
        #endregion

        #region IO output set
        public void SetLReq(bool bOn) { }
        public void SetUReq(bool bOn) { }
        public void SetVa(bool bOn) { }
        public void SetReady(bool bOn) { }
        public void SetVs0(bool bOn) { }
        public void SetVs1(bool bOn) { }
        public void SetAvbl(bool bOn) { }
        public void SetEs(bool bOn)
        {
            WriteLog("[CMD] Set ES " + (bOn ? "On" : "Off"));
            string strCmd = bOn ? "550301BB" : "550300BB";
            if (m_bSimulate) return;
            string strReply = SendCmd(strCmd);
            if (strReply.IndexOf("0300") > 0) { WriteLog("Set ES-on success"); }
            else if (strReply.IndexOf("03FF") >= 0) { WriteLog(string.Format("Non-support Set ES.")); }
        }
        #endregion

        #region =========================== OnOccurError ===========================     
        //  發生E84異常
        private void SendAlmMsg(string strCode)
        {
            if (strCode.Length != 4) return;
            //  STG1 15 13 00000
            //  STG2 16 13 00000
            //  STG3 17 13 00000
            //  STG4 18 13 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (15 + BodyNo - 1) * 10000000 + 13 * 100000*/;
            OnOccurError?.Invoke(this, new OccurErrorEventArgs(nCode));

        }
        //  解除E84異常
        private void RestAlmMsg(string strCode)
        {
            if (strCode.Length != 4) return;
            //  STG1 15 13 00000
            //  STG2 16 13 00000
            //  STG3 17 13 00000
            //  STG4 18 13 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (15 + BodyNo - 1) * 10000000 + 13 * 100000*/;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生自定義異常
        private void SendAlmMsg(enumE84Warning eAlarm)
        {
            //  STG1 15 13 00000
            //  STG2 16 13 00000
            //  STG3 17 13 00000
            //  STG4 18 13 00000
            //  STG5 19 13 00000
            //  STG6 20 13 00000
            //  STG7 21 13 00000
            //  STG8 22 13 00000
            int nCode = (int)eAlarm /*+ (15 + BodyNo - 1) * 10000000 + 13 * 100000*/;
            OnOccurError?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除E84異常
        private void RestAlmMsg(enumE84Warning eAlarm)
        {
            //  STG1 15 13 00000
            //  STG2 16 13 00000
            //  STG3 17 13 00000
            //  STG4 18 13 00000
            int nCode = (int)eAlarm /*+ (15 + BodyNo - 1) * 10000000 + 13 * 100000*/;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        #endregion
    }
}
