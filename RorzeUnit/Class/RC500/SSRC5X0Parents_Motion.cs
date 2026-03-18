using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RorzeUnit.Interface;
using RorzeUnit.Event;
using RorzeUnit.Class.RC500.Event;
using RorzeApi;
using RorzeUnit.Net.Sockets;
using System.Globalization;
using System.Windows;
using RorzeComm.Threading;
using RorzeUnit.Class.RC500.RCEnum;
using RorzeComm;
using RorzeComm.Log;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Diagnostics.Eventing.Reader;
using static System.Net.Mime.MediaTypeNames;
using RorzeUnit.Class.Robot.Enum;

namespace RorzeUnit.Class.RC500
{
    public abstract class SSRC5X0Parents_Motion : I_RC5X0_Motion
    {
        //==============================================================================
        #region =========================== private ============================================
        private enumRC5X0Mode m_eStatMode;      //記憶的STAT S1第1 bit
        private bool m_bStatOrgnComplete;       //記憶的STAT S1第2 bit
        private bool m_bStatProcessed;          //記憶的STAT S1第3 bit
        private enumRC5X0Status m_eStatInPos;   //記憶的STAT S1第4 bit
        private int m_nSpeed;                   //記憶的STAT S1第5 bit
        private string m_strErrCode = "0000";   //記憶的STAT S2

        private sRorzeSocket m_Socket;

        private int m_nOriginTimeout = 120000;
        private int m_nMotionTimeout = 60000;
        protected int m_nAckTimeout = 3000;
        private int m_nDMNTAxs = -1;//下命令DMNT 是帶哪一軸 

        private object m_lockStat = new object();
        private object m_lockGpio = new object();

        private string[] m_AxisName;
        private int[] m_AxisPulse;
        private int[] m_AxisEncode;
        private bool[] m_AxisOrgnSen;

        private bool[] m_GPIO_Input;
        private bool[] m_GPIO_Output;
        private string m_strGmap;
        private string m_strGmap_result;
        private string[] m_strCurrentDMPR;
        #endregion
        //==============================================================================
        #region =========================== public =============================================
        public bool Simulate { get; private set; }
        public bool Connected { get; private set; }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }
        public string VersionData { get; private set; }

        //STAT S1第1 bit
        public enumRC5X0Mode StatMode { get { return m_eStatMode; } }
        public bool IsInitialized { get { return m_eStatMode == enumRC5X0Mode.Remote; } }
        //STAT S1第2 bit
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }
        //STAT S1第3 bit
        public bool IsProcessing { get { return m_bStatProcessed; } }
        //STAT S1第4 bit
        public enumRC5X0Status InPos { get { return m_eStatInPos; } }
        public bool IsMoving { get { return m_eStatInPos == enumRC5X0Status.Moving; } }
        //STAT S1第5 bit
        public int GetSpeed { get { return m_nSpeed; } }
        //STAT S2
        public bool IsError { get { return (m_strErrCode != "0000"); } }

        public string[] GetCurrentDMPR { get { return m_strCurrentDMPR; } }
        #endregion
        //==============================================================================
        #region =========================== Event ==============================================
        public event MessageEventHandler OnReadData;// TCPIP Recive

        public event EventHandler<bool> OnORGNComplete;
        public event EventHandler<bool> OnMoveStepComplete;
        public event EventHandler<string> OnSensorChange;

        public event NotifyGPIOEventHandler OnIOChange;

        public event OccurErrorEventHandler OnOccurStatErr;
        public event OccurErrorEventHandler OnOccurCancel;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public event OccurErrorEventHandler OnOccurErrorRest;

        public event EventHandler<bool> OnNotifyEvntCNCT;
        #endregion
        #region =========================== Thread =============================================
        private SPollingThread _exePolling;// TCPIP Recive
        private SInterruptOneThread _threadInitial;
        private SInterruptOneThreadINT _threadOrgn;
        private SInterruptOneThreadINT_INT _threadSTEP;
        private SInterruptOneThreadINT_INT _threadMABS;
        private SInterruptOneThreadINT_INT _threadMREL;
        private SInterruptOneThreadINT _threadRSTA;
        private SInterruptOneThread _threadSTOP;
        private SInterruptOneThread _threadEVNT;
        #endregion
        //==============================================================================
        public SSRC5X0Parents_Motion(string IP, int PortID, int nBodyNo, bool bDisable, bool bSimulate, sServer Sever = null)
        {
            BodyNo = nBodyNo;
            Disable = bDisable;
            Simulate = bSimulate;

            Connected = false;

            for (int nCnt = 0; nCnt < (int)enumRC5X0Command_Motion.Max; nCnt++)
                _signalAck.Add((enumRC5X0Command_Motion)nCnt, new SSignal(false, EventResetMode.ManualReset));

            for (int i = 0; i < (int)enumRC500SignalTable.Max; i++)
                _signals.Add((enumRC500SignalTable)i, new SSignal(false, EventResetMode.ManualReset));

            _signals[enumRC500SignalTable.ProcessCompleted].Set();

            _signalSubSequence = new SSignal(false, EventResetMode.ManualReset);

            m_Socket = new sRorzeSocket(IP, PortID, BodyNo, "TBL", Simulate, Sever);

            _threadInitial = new SInterruptOneThread(ExeINIT);
            _threadOrgn = new SInterruptOneThreadINT(ExeORGN);
            _threadMABS = new SInterruptOneThreadINT_INT(ExeMABS);
            _threadMREL = new SInterruptOneThreadINT_INT(ExeMREL);
            _threadSTEP = new SInterruptOneThreadINT_INT(ExeSTEP);
            _threadRSTA = new SInterruptOneThreadINT(ExeRSTA);
            _threadSTOP = new SInterruptOneThread(ExeSTOP);
            _threadEVNT = new SInterruptOneThread(ExeEVNT);
            _exePolling = new SPollingThread(1);
            _exePolling.DoPolling += _exePolling_DoPolling;
            _exePolling.Set();

            m_AxisName = new string[6] { "AXS1", "AXS2", "AXS3", "AXS4", "AXS5", "AXS6" };
            m_AxisPulse = new int[6];
            m_AxisEncode = new int[6];
            m_AxisOrgnSen = new bool[6];
            m_GPIO_Input = new bool[32];
            m_GPIO_Output = new bool[32];
            if (Simulate)
            {
                m_eStatMode = enumRC5X0Mode.Remote;
                m_bStatOrgnComplete = true;
            }
            CreateMessage();

        }

        private SLogger _logger = SLogger.GetLogger("CommunicationLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[TBL{0}] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }
        //==============================================================================
        public void Open() { m_Socket.Open(); }
        private void _exePolling_DoPolling()
        {
            try
            {
                string[] astrFrame;

                if (!m_Socket.QueRecvBuffer.TryDequeue(out astrFrame)) return;
                string strFrame;

                OnReadData?.Invoke(this, new MessageEventArgs(astrFrame));

                for (int nCnt = 0; nCnt < astrFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                {
                    if (astrFrame[nCnt].Length == 0)
                        continue;

                    strFrame = astrFrame[nCnt];

                    enumRC5X0Command_Motion cmd = enumRC5X0Command_Motion.GVER;
                    bool bUnknownCmd = true;

                    foreach (string scmd in _dicCmdsTable.Values) //查字典
                    {
                        if (strFrame.Contains(string.Format("{0}", scmd)))
                        {
                            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                            bUnknownCmd = false; //認識這個指令
                            break;
                        }
                    }

                    if (bUnknownCmd) //不認識的封包
                    {
                        WriteLog("<<<ByPassReceive>>> Got unknown frame and pass to process. [{0}]", strFrame);
                        continue;
                    }
                    WriteLog("Received : " + strFrame);
                    switch (strFrame[0]) //命令種類
                    {
                        case 'c': //cancel
                            OnCancelAck(this, new RorzeProtoclEventArgs(strFrame));
                            break;
                        case 'n': //nak
                            _signalAck[cmd].bAbnormalTerminal = true;
                            _signalAck[cmd].Set();
                            break;
                        case 'a': //ack
                            OnAck(this, new RorzeProtoclEventArgs(strFrame));
                            _signalAck[cmd].Set();
                            break;
                        case 'e':
                            OnAck(this, new RorzeProtoclEventArgs(strFrame));
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
        }
        private void OnAck(object sender, RorzeProtoclEventArgs e)
        {
            enumRC5X0Command_Motion cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                case enumRC5X0Command_Motion.CNCT:
                    if (Connected == false) EVNT();
                    Connected = true;
                    OnNotifyEvntCNCT?.Invoke(this, true);
                    break;
                case enumRC5X0Command_Motion.STAT:
                    AnalysisStatus(e.Frame.Value);
                    break;
                case enumRC5X0Command_Motion.GPIO:
                    AnalysisGPIO(e.Frame.Value);
                    break;
                case enumRC5X0Command_Motion.GVER:
                    VersionData = e.Frame.Value;
                    break;
                case enumRC5X0Command_Motion.GPOS:
                    AnalysisGPOS(e.Frame.AttachCommand, e.Frame.Value);
                    break;
                case enumRC5X0Command_Motion.GTDT:
                    AnalysisGTDT(e.Frame.AttachCommand, e.Frame.Data);
                    break;
                case enumRC5X0Command_Motion.GPSX:
                    AnalysisGPSX(e.Frame.Value);
                    break;
                case enumRC5X0Command_Motion.GMAP:
                    AnalysisGMAP(e.Frame.Value);
                    break;

                default:
                    break;
            }

        }
        private void OnCancelAck(object sender, RorzeProtoclEventArgs e)
        {
            enumRC5X0Command_Motion cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            if (cmd == enumRC5X0Command_Motion.SMAP)
            {
                //oTBL1.SMAP
                //cTBL1.SMAP:000C
            }
            else
                AnalysisCancel(e.Frame.Value);
        }
        private void AnalysisStatus(string strFrame)
        {
            lock (m_lockStat)
            {
                if (!strFrame.Contains('/'))
                {
                    WriteLog(string.Format("<<<Error>>> the format of STAT frame has error, '/' not found! [{0}]", strFrame));
                    return;
                }

                string[] str = strFrame.Split('/');
                string s1 = str[0];
                string s2 = str[1];

                //S1.bit#1 operation mode
                switch (s1[0])
                {
                    case '0':
                        m_eStatMode = enumRC5X0Mode.Initializing;
                        //_signals[enumRC500SignalTable.Remote].Reset();
                        break;
                    case '1':
                        m_eStatMode = enumRC5X0Mode.Remote;
                        _signals[enumRC500SignalTable.Remote].Set();
                        break;
                    case '2':
                        m_eStatMode = enumRC5X0Mode.Maintenance;
                        _signals[enumRC500SignalTable.Remote].Set();
                        break;
                    case '3':
                        m_eStatMode = enumRC5X0Mode.Recovery;
                        break;
                    default: break;
                }

                //S1.bit#2 origin return complete
                if (s1[1] == '0')
                    _signals[enumRC500SignalTable.OPRCompleted].Reset();
                else
                    _signals[enumRC500SignalTable.OPRCompleted].Set();

                m_bStatOrgnComplete = s1[1] == '1';

                //S1.bit#3 processing command
                if (s1[2] == '0')
                    _signals[enumRC500SignalTable.ProcessCompleted].Set();
                else
                    _signals[enumRC500SignalTable.ProcessCompleted].Reset();
                m_bStatProcessed = s1[2] == '1';

                //S1.bit#4 operation status
                switch (s1[3])
                {
                    case '0': m_eStatInPos = enumRC5X0Status.InPos; break;
                    case '1': m_eStatInPos = enumRC5X0Status.Moving; break;
                    case '2': m_eStatInPos = enumRC5X0Status.Pause; break;
                }

                //S1.bit#5 operation speed
                if (s1[4] >= '0' && s1[4] <= '9')
                    m_nSpeed = s1[4] - '0';
                else if (s1[4] >= 'A' && s1[4] <= 'K')
                    m_nSpeed = s1[4] - 'A' + 10;

                if (Convert.ToInt32(s2, 16) > 0)
                {
                    _signals[enumRC500SignalTable.MotionCompleted].bAbnormalTerminal = true;
                    _signals[enumRC500SignalTable.MotionCompleted].Set(); //有moving過才可以Set
                    SendAlmMsg(s2);
                    m_strErrCode = s2;
                }
                else
                {
                    if ((m_eStatInPos == enumRC5X0Status.InPos))
                        _signals[enumRC500SignalTable.MotionCompleted].Set();
                    else
                        _signals[enumRC500SignalTable.MotionCompleted].Reset();

                    //覆蓋上一次的狀態
                    if (m_strErrCode != "0000")
                    {
                        RestAlmMsg(m_strErrCode);
                        m_strErrCode = "0000";
                    }
                }
            }
        }
        private void AnalysisCancel(string strFrame)
        {
            if (Convert.ToInt32(strFrame, 16) > 0)
            {
                _signals[enumRC500SignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumRC500SignalTable.MotionCompleted].Set(); //有moving過才可以Set

                SendCancelMsg(strFrame);
            }
        }
        private void AnalysisGPIO(string strIO)
        {
            lock (m_lockGpio)
            {
                if (!strIO.Contains('/'))
                {
                    WriteLog(string.Format("<<<Error>>> the format of GPIO frame has error, [{0}]", strIO));
                    return;
                }
                else
                {
                    string ioData = strIO.Split(':')[0];
                    string[] arrIO = ioData.Split('/');
                    string inputData = arrIO[0].Substring(0, 8);
                    string outputData = arrIO[1].Substring(0, 8);

                    string binarydataDI = Convert.ToString(Int32.Parse(inputData, NumberStyles.HexNumber), 2).PadLeft(32, '0');
                    string binarydataDO = Convert.ToString(Int32.Parse(outputData, NumberStyles.HexNumber), 2).PadLeft(32, '0');
                    string ReverseBinarydataDI = new string(binarydataDI.ToCharArray().Reverse().ToArray());
                    string ReverseBinarydataDO = new string(binarydataDO.ToCharArray().Reverse().ToArray());
                    m_GPIO_Input = ReverseBinarydataDI.Select(i => Convert.ToBoolean(int.Parse(i.ToString()))).ToArray();
                    m_GPIO_Output = ReverseBinarydataDO.Select(i => Convert.ToBoolean(int.Parse(i.ToString()))).ToArray();
                }
            }
            OnIOChange?.Invoke(this, new NotifyGPIOEventArgs(m_GPIO_Input, m_GPIO_Output));
        }
        private void AnalysisGTDT(string strAttachCommand, string nparam)
        {
            if (strAttachCommand == null || strAttachCommand == string.Empty)
            {
                if (nparam.Contains("GTDT:DRV/"))//RC550判斷原點sensor GTDT[1]
                {
                    //GTDT:DRV/ 1080-0000-00C0,1080-0000-00C0,1080-0000-00C0,1080-0000-00C0,1080-0000-00C0,1080-0000-00C0
                    //GTDT:DRV/ xxxx xxxx 1xxx xxxx-0000-00C0,......
                    int nV = 1 << 7;
                    string str = nparam.Substring(nparam.IndexOf("/") + 1);
                    string[] strArray = str.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < 6; i++)
                    {
                        int n = Convert.ToInt32(strArray[i].Substring(0, 4), 16);
                        m_AxisOrgnSen[i] = ((n & nV) != 0);
                    }
                    OnSensorChange?.Invoke(this, str);
                }
                else if (nparam.Contains("GTDT:DST/"))//RC560判斷原點sensor GTDT[11]
                {
                    //GTDT:DST/ aaaa:bbbb:cccc,aaaa:bbbb:cccc,aaaa:bbbb:cccc,aaaa:bbbb:cccc,aaaa:bbbb:cccc,aaaa:bbbb:cccc
                    //GTDT:DST/ xxxx xxx1 xxxx xxxx:bbbb:cccc,......
                    int nV = 1 << 8;
                    string str = nparam.Substring(nparam.IndexOf("/") + 1);
                    string[] strArray = str.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < 6; i++)
                    {
                        int n = Convert.ToInt32(strArray[i].Substring(0, 4), 16);
                        m_AxisOrgnSen[i] = ((n & nV) != 0);
                    }
                }
            }
            else if (strAttachCommand.Contains("DMNT"))//  預計主動去問軸的名稱
            {
                //  TBL1.DMNT.GTDT:-8888000,8888000,1000,50,50,250,1000,0,0,1,1,1000,-,-,"XAX1",""  14個
                if (nparam.Contains(',') && m_nDMNTAxs != -1)
                {
                    m_AxisName[m_nDMNTAxs] = nparam.Split(',')[14].Replace("\"", "");
                }
            }
            else if (strAttachCommand.Contains("DMPR"))// MAPPING
            {
                //2022/04/15 11:28:59.055     [oTBL] oTBL1.DMPR.GTDT[0]
                //2022/04/15 11:28:59.102     [Recv] aTBL1.DMPR.GTDT:28,0,13,1,46834,166834,1000,2000,-,-,-,-,1700,8000,5000,""
                if (nparam.Contains(','))
                {
                    m_strCurrentDMPR = nparam.Split(',');
                }
            }
        }
        private void AnalysisGPOS(string axisIndex, string strPos)
        {
            if (axisIndex == null || axisIndex == string.Empty)// GPOS:000/000/999/999/999/999
                return;

            //單軸位置 oTBL.AXS1.GPOS:XXXX
            int pos = 0;
            if (Int32.TryParse(strPos, out pos) == false)
            {
                WriteLog(string.Format("<<<Error>>> the format of GPOS frame has error, [{0}]", strPos));
                return;
            }

            int index = Array.IndexOf(m_AxisName, axisIndex);//尋找第幾軸
            if (index != -1) m_AxisPulse[index] = pos;
        }
        private void AnalysisGPSX(string strPos)
        {
            // GPSX:000/000/999/999/999/999
            if (strPos.Contains("/") == false) return;
            string[] strArray = strPos.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length != 6) return;
            for (int i = 0; i < strArray.Length; i++)
            {
                m_AxisEncode[i] = int.Parse(strArray[i]);
            }
        }
        private void AnalysisGMAP(string str)
        {
            //oTBL1.GMAP
            //aTBL1.GMAP:1000000000001

            //oTBL1.GMAP(1)
            //aTBL1.GMAP:1,+000047936,+000049735,+000001799

            if (str.Contains(','))
            {
                m_strGmap_result = str;
            }
            else
            {
                //反轉 RJ回的是1->13
                //char[] charArray = str.ToCharArray();
                //Array.Reverse(charArray);
                //str = new string(charArray);
                //先不反轉後13->1
                m_strGmap = str;
            }

        }


        //==============================================================================    
        public void INIT() { _threadInitial.Set(); }
        public void ORGN(int nAxis) { _threadOrgn.Set(nAxis); }
        public void STEP(int nAxis, int nPluse) { _threadSTEP.Set(nAxis, nPluse); }
        public void MABS(int nAxis, int nPluse) { _threadMABS.Set(nAxis, nPluse); }
        public void MREL(int nAxis, int nPluse) { _threadMREL.Set(nAxis, nPluse); }
        public void RSTA(int nReset) { _threadRSTA.Set(nReset); }
        public void STOP() { _threadSTOP.Set(); }
        public void EVNT() { _threadEVNT.Set(); }
        //==============================================================================
        private void ExeINIT()
        {
            try
            {
                InitW(m_nAckTimeout);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
        }
        private void ExeORGN(int nAxis)
        {
            try
            {
                EventW(m_nAckTimeout);

                ResetProcessCompleted();
                InitW(m_nAckTimeout);
                WaitProcessCompleted(m_nMotionTimeout);

                StimW(m_nAckTimeout);


                enumRC550Axis eAxis = (enumRC550Axis)nAxis;

                if (Enum.IsDefined(typeof(enumRC550Axis), nAxis))
                {
                    ResetInPos();
                    ResetOrgnSinal();
                    OrgnW(m_nAckTimeout, eAxis);
                    WaitOrgnCompleted(m_nOriginTimeout);
                    WaitInPos(m_nAckTimeout);
                }
                else
                {
                    //throw new Exception("Error ORGN Parameter");

                    ResetInPos();
                    ResetOrgnSinal();
                    OrgnW(m_nAckTimeout, enumRC550Axis.None);
                    WaitOrgnCompleted(m_nOriginTimeout);
                    WaitInPos(m_nAckTimeout);
                }

                OnORGNComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnORGNComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnORGNComplete?.Invoke(this, false);
            }
        }
        private void ExeSTEP(int nAxis, int nPluse)
        {
            try
            {
                if (Enum.IsDefined(typeof(enumRC550Axis), nAxis) == false)
                {
                    throw new Exception("Error Parameter");
                }
                enumRC550Axis eAxis = (enumRC550Axis)nAxis;
                ResetInPos();
                AxisStepW(m_nAckTimeout, eAxis, nPluse);
                WaitInPos(m_nMotionTimeout);

                OnMoveStepComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnMoveStepComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnMoveStepComplete?.Invoke(this, false);
            }
        }
        private void ExeMABS(int nAxis, int nPluse)
        {
            try
            {
                if (Enum.IsDefined(typeof(enumRC550Axis), nAxis) == false)
                {
                    throw new Exception("Error Parameter");
                }
                enumRC550Axis eAxis = (enumRC550Axis)nAxis;
                ResetInPos();
                AxisMabsW(m_nAckTimeout, eAxis, nPluse);
                WaitInPos(m_nMotionTimeout);

                OnMoveStepComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnMoveStepComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("ExeMABS :" + ex.ToString());
                OnMoveStepComplete?.Invoke(this, false);
            }
        }
        private void ExeMREL(int nAxis, int nPluse)
        {
            try
            {
                if (Enum.IsDefined(typeof(enumRC550Axis), nAxis) == false)
                {
                    throw new Exception("Error Parameter");
                }
                enumRC550Axis eAxis = (enumRC550Axis)nAxis;
                ResetInPos();
                AxisMrelW(m_nAckTimeout, eAxis, nPluse);
                WaitInPos(m_nMotionTimeout);
                OnMoveStepComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnMoveStepComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnMoveStepComplete?.Invoke(this, false);
            }
        }
        private void ExeRSTA(int nReset)
        {
            try
            {
                WriteLog("ExeRSTA:Start");
                this.ResetW(3000, nReset);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);

            }
        }
        private void ExeSTOP()
        {
            try
            {
                StopW(m_nAckTimeout);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("ExeMABS :" + ex);
            }
        }
        private void ExeEVNT()
        {
            try
            {

                EventW(m_nAckTimeout);

                //怕被暫停
                StatW(m_nAckTimeout);
                if (m_eStatInPos == enumRC5X0Status.Pause)
                    StopW(m_nAckTimeout);

                ExctW(m_nAckTimeout, 1);



                AxisGposW(m_nAckTimeout, enumRC550Axis.AXS1);
                AxisGposW(m_nAckTimeout, enumRC550Axis.AXS2);
                AxisGposW(m_nAckTimeout, enumRC550Axis.AXS3);
                AxisGposW(m_nAckTimeout, enumRC550Axis.AXS4);
                AxisGposW(m_nAckTimeout, enumRC550Axis.AXS5);
                AxisGposW(m_nAckTimeout, enumRC550Axis.AXS6);







            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("ExeMABS :" + ex);
            }
        }


        //==============================================================================

        #region =========================== ORGN ===============================================
        private void OrgnEncoderClear(enumRC550Axis axis)//RC550才能用
        {
            _signalAck[enumRC5X0Command_Motion.ORGN].Reset();
            m_Socket.SendCommand(string.Format("{0}.ORGN(4,99)", m_AxisName[(int)axis]));
        }
        private void Orgn(enumRC550Axis axis)
        {
            _signalAck[enumRC5X0Command_Motion.ORGN].Reset();

            if (axis == enumRC550Axis.None)
                m_Socket.SendCommand(string.Format("ORGN"));
            else
                m_Socket.SendCommand(string.Format("{0}.ORGN(1)", m_AxisName[(int)axis]));
        }
        public void OrgnW(int nTimeout, enumRC550Axis axis)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumRC500SignalTable.MotionCompleted].Reset();

                Orgn(axis);

                if (!_signalAck[enumRC5X0Command_Motion.ORGN].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.ORGN]));
                }
                if (_signalAck[enumRC5X0Command_Motion.ORGN].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.ORGN]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== HOME ===============================================
        private void Home(enumRC550Axis axis)
        {
            _signalAck[enumRC5X0Command_Motion.HOME].Reset();
            if (axis == enumRC550Axis.None)
                m_Socket.SendCommand(string.Format("HOME"));//全軸回原點
            else
                m_Socket.SendCommand(string.Format("{0}.HOME", m_AxisName[(int)axis]));//單軸回原點
        }
        public void HomeW(int nTimeout, enumRC550Axis axis)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumRC500SignalTable.MotionCompleted].Reset();

                Home(axis);

                if (!_signalAck[enumRC5X0Command_Motion.HOME].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.HOME]));
                }
                if (_signalAck[enumRC5X0Command_Motion.HOME].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.HOME]));
                }
            }
            _signalSubSequence.Set();
        }

        #endregion 

        #region =========================== AxisMABS ===========================================
        private void AxisMabs(enumRC550Axis axis, int pluse, int spd)
        {
            _signalAck[enumRC5X0Command_Motion.MABS].Reset();
            m_Socket.SendCommand(string.Format("{0}.MABS({1},{2})", m_AxisName[(int)axis], pluse, spd));
        }
        public void AxisMabsW(int nTimeout, enumRC550Axis axis, int pluse, int spd = 0)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumRC500SignalTable.MotionCompleted].Reset();

                AxisMabs(axis, pluse, spd);

                if (!_signalAck[enumRC5X0Command_Motion.MABS].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.MABS]));
                }
                if (_signalAck[enumRC5X0Command_Motion.MABS].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.MABS]));
                }
            }
            else
            {
                switch (axis)
                {
                    case enumRC550Axis.AXS1: m_AxisPulse[0] = pluse; break;
                    case enumRC550Axis.AXS2: m_AxisPulse[1] = pluse; break;
                    case enumRC550Axis.AXS3: m_AxisPulse[2] = pluse; break;
                    case enumRC550Axis.AXS4: m_AxisPulse[3] = pluse; break;
                    case enumRC550Axis.AXS5: m_AxisPulse[4] = pluse; break;
                    case enumRC550Axis.AXS6: m_AxisPulse[5] = pluse; break;
                }
            }
            _signalSubSequence.Set();
        }

        public void AxisMabsW(int nTimeout, int pluse, int spd = -1)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumRC500SignalTable.MotionCompleted].Reset();

                AxisMabs(enumRC550Axis.AXS1, pluse, spd);

                if (!_signalAck[enumRC5X0Command_Motion.MABS].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.MABS]));
                }
                if (_signalAck[enumRC5X0Command_Motion.MABS].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.MABS]));
                }
            }
            else
            {
                m_AxisPulse[0] = pluse;
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== AxisMREL ===========================================
        private void AxisMrel(enumRC550Axis axis, int pluse, int spd)
        {
            _signalAck[enumRC5X0Command_Motion.MREL].Reset();
            m_Socket.SendCommand(string.Format("{0}.MREL({1},{2})", m_AxisName[(int)axis], pluse, spd));
        }
        public void AxisMrelW(int nTimeout, enumRC550Axis axis, int pluse, int spd = 0)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumRC500SignalTable.MotionCompleted].Reset();

                AxisMrel(axis, pluse, spd);

                if (!_signalAck[enumRC5X0Command_Motion.MREL].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.MREL]));
                }
                if (_signalAck[enumRC5X0Command_Motion.MREL].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.MREL]));
                }
            }
            else
            {
                switch (axis)
                {
                    case enumRC550Axis.AXS1: m_AxisPulse[0] += pluse; break;
                    case enumRC550Axis.AXS2: m_AxisPulse[1] += pluse; break;
                    case enumRC550Axis.AXS3: m_AxisPulse[2] += pluse; break;
                    case enumRC550Axis.AXS4: m_AxisPulse[3] += pluse; break;
                    case enumRC550Axis.AXS5: m_AxisPulse[4] += pluse; break;
                    case enumRC550Axis.AXS6: m_AxisPulse[5] += pluse; break;
                }
            }
            _signalSubSequence.Set();
        }

        #endregion 

        #region =========================== EVNT ===============================================
        private void Event()
        {
            _signalAck[enumRC5X0Command_Motion.EVNT].Reset();
            m_Socket.SendCommand("EVNT(0,1)");
        }
        public void EventW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Event();
                if (!_signalAck[enumRC5X0Command_Motion.EVNT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.EVNT]));
                }
                if (_signalAck[enumRC5X0Command_Motion.EVNT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.EVNT]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== RSTA ===============================================
        private void Reset(int nReset)
        {
            _signalAck[enumRC5X0Command_Motion.RSTA].Reset();
            m_Socket.SendCommand(string.Format("RSTA(" + nReset + ")"));
        }
        public void ResetW(int nTimeout, int nReset = 0)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Reset(nReset);
                if (!_signalAck[enumRC5X0Command_Motion.RSTA].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.RSTA]));
                }
                if (_signalAck[enumRC5X0Command_Motion.RSTA].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.RSTA]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== INIT ===============================================
        private void Init()
        {
            _signalAck[enumRC5X0Command_Motion.INIT].Reset();
            m_Socket.SendCommand(string.Format("INIT"));
        }
        public void InitW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumRC500SignalTable.Remote].Reset();
                Init();
                if (!_signalAck[enumRC5X0Command_Motion.INIT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.INIT]));
                }
                if (_signalAck[enumRC5X0Command_Motion.INIT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.INIT]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STOP ===============================================
        private void Stop()
        {
            _signalAck[enumRC5X0Command_Motion.STOP].Reset();
            m_Socket.SendCommand(string.Format("STOP"));
        }
        public void StopW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                if (IsMoving)
                {
                    _signals[enumRC500SignalTable.MotionCompleted].bAbnormalTerminal = true;
                }
                Stop();
                if (!_signalAck[enumRC5X0Command_Motion.STOP].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.STOP]));
                }
                if (_signalAck[enumRC5X0Command_Motion.STOP].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.STOP]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== PAUS ===============================================
        private void Paus()
        {
            _signalAck[enumRC5X0Command_Motion.PAUS].Reset();
            m_Socket.SendCommand(string.Format("PAUS"));
        }
        public void PausW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Paus();

                if (!_signalAck[enumRC5X0Command_Motion.PAUS].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.PAUS]));
                }
                if (_signalAck[enumRC5X0Command_Motion.PAUS].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.PAUS]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }

        #endregion 

        #region =========================== MODE ===============================================
        private void Mode(int nMode)
        {
            _signalAck[enumRC5X0Command_Motion.MODE].Reset();
            m_Socket.SendCommand(string.Format("MODE(" + nMode + ")"));
        }
        public void ModeW(int nTimeout, int nMode)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Mode(nMode);
                if (!_signalAck[enumRC5X0Command_Motion.MODE].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.MODE]));
                }
                if (_signalAck[enumRC5X0Command_Motion.MODE].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.MODE]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== WTDT ===============================================
        private void Wtdt()
        {
            _signalAck[enumRC5X0Command_Motion.WTDT].Reset();
            m_Socket.SendCommand(string.Format("WTDT"));
        }
        public void WtdtW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Wtdt();
                if (!_signalAck[enumRC5X0Command_Motion.WTDT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.WTDT]));
                }
                if (_signalAck[enumRC5X0Command_Motion.WTDT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.WTDT]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== RTDT ===============================================
        private void Rtdt()
        {
            _signalAck[enumRC5X0Command_Motion.RTDT].Reset();
            m_Socket.SendCommand(string.Format("RTDT"));
        }
        public void RtdtW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Rtdt();
                if (!_signalAck[enumRC5X0Command_Motion.RTDT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.RTDT]));
                }
                if (_signalAck[enumRC5X0Command_Motion.RTDT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.RTDT]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SSPD ===============================================
        private void Sspd(int nSpeed)
        {
            _signalAck[enumRC5X0Command_Motion.SSPD].Reset();
            m_Socket.SendCommand(string.Format("SSPD(" + nSpeed + ")"));
        }
        public void SspdW(int nTimeout, int nSpeed)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumRC500SignalTable.MotionCompleted].Reset();
                Sspd(nSpeed);
                if (!_signalAck[enumRC5X0Command_Motion.SSPD].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SSPD]));
                }
                if (_signalAck[enumRC5X0Command_Motion.SSPD].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SSPD]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== EXCT ===============================================
        private void Exct(int nVariable)
        {
            _signalAck[enumRC5X0Command_Motion.EXCT].Reset();
            m_Socket.SendCommand(string.Format("EXCT(" + nVariable + ")"));
        }
        public void ExctW(int nTimeout, int nVariable)//0:Turn off 1:Turn on
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Exct(nVariable);
                if (!_signalAck[enumRC5X0Command_Motion.EXCT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.EXCT]));
                }
                if (_signalAck[enumRC5X0Command_Motion.EXCT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.EXCT]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== TORQ ===============================================
        private void Torq(int nVariable)
        {
            _signalAck[enumRC5X0Command_Motion.TORQ].Reset();
            m_Socket.SendCommand(string.Format("TORQ(" + nVariable + ")"));
        }
        public void TorqW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Torq(nVariable);
                if (!_signalAck[enumRC5X0Command_Motion.TORQ].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.TORQ]));
                }
                if (_signalAck[enumRC5X0Command_Motion.TORQ].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.TORQ]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region ============================ STAT ================================================
        private void Stat()
        {
            _signalAck[enumRC5X0Command_Motion.STAT].Reset();
            m_Socket.SendCommand(string.Format("STAT"));
        }
        public void StatW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Stat();
                if (!_signalAck[enumRC5X0Command_Motion.STAT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.STAT]));
                }
                if (_signalAck[enumRC5X0Command_Motion.STAT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.STAT]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPIO ===============================================
        private void Gpio()
        {
            _signalAck[enumRC5X0Command_Motion.GPIO].Reset();
            m_Socket.SendCommand(string.Format("GPIO"));
        }
        public void GpioW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Gpio();
                if (!_signalAck[enumRC5X0Command_Motion.GPIO].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GPIO]));
                }
                if (_signalAck[enumRC5X0Command_Motion.GPIO].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GPIO]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GVER ===============================================
        private void Gver()
        {
            _signalAck[enumRC5X0Command_Motion.GVER].Reset();
            m_Socket.SendCommand(string.Format("GVER"));
        }
        public void GverW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Gver();
                if (!_signalAck[enumRC5X0Command_Motion.GVER].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GVER]));
                }
                if (_signalAck[enumRC5X0Command_Motion.GVER].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GVER]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GLOG ===============================================
        private void Glog()
        {
            _signalAck[enumRC5X0Command_Motion.GLOG].Reset();
            m_Socket.SendCommand(string.Format("GLOG"));
        }
        public void GlogW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Glog();
                if (!_signalAck[enumRC5X0Command_Motion.GLOG].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GLOG]));
                }
                if (_signalAck[enumRC5X0Command_Motion.GLOG].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GLOG]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STIM ===============================================
        private void Stim()
        {
            _signalAck[enumRC5X0Command_Motion.STIM].Reset();
            m_Socket.SendCommand("STIM(" + DateTime.Now.ToString("yyyy, MM, dd, HH, mm, ss") + ")");
        }
        public void StimW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Stim();
                if (!_signalAck[enumRC5X0Command_Motion.STIM].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.STIM]));
                }
                if (_signalAck[enumRC5X0Command_Motion.STIM].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.STIM]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPOS ===============================================
        private void AxisGpos(enumRC550Axis axis)
        {
            _signalAck[enumRC5X0Command_Motion.GPOS].Reset();
            m_Socket.SendCommand(string.Format("{0}.GPOS", m_AxisName[(int)axis]));
        }
        public void AxisGposW(int nTimeout, enumRC550Axis axis)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                AxisGpos(axis);
                if (!_signalAck[enumRC5X0Command_Motion.GPOS].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GPOS]));
                }
                if (_signalAck[enumRC5X0Command_Motion.GPOS].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GPOS]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== TDST ===============================================
        private void Tdst()
        {
            _signalAck[enumRC5X0Command_Motion.TDST].Reset();
            m_Socket.SendCommand(string.Format("TDST"));
        }
        public void TdstW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Tdst();
                if (!_signalAck[enumRC5X0Command_Motion.TDST].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.TDST]));
                }
                if (_signalAck[enumRC5X0Command_Motion.TDST].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.TDST]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GTDT ===============================================
        private void Gtdt(int nP)
        {
            _signalAck[enumRC5X0Command_Motion.GTDT].Reset();
            m_Socket.SendCommand(string.Format("GTDT[{0}]", nP));
        }
        public void GtdtW(int nTimeout, int nP)//RC560 GTDT[11]/RC550 GTDT[1]
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                enumRC5X0Command_Motion cmd = enumRC5X0Command_Motion.GTDT;

                Gtdt(nP);

                if (!_signalAck[cmd].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[cmd]));
                }
                if (_signalAck[cmd].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[cmd]));
                }
            }
            _signalSubSequence.Set();
        }
        protected abstract void GTDT_OrgnSensorW();
        #endregion 

        #region =========================== GPSX ===============================================
        private void Gpsx(int n)// 0:controller  1:encoder
        {
            _signalAck[enumRC5X0Command_Motion.GPSX].Reset();
            m_Socket.SendCommand(string.Format("GPSX(" + n + ")"));
        }
        public void GpsxW(int nTimeout, int n = 1)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Gpsx(n);
                if (!_signalAck[enumRC5X0Command_Motion.GPSX].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GPSX]));
                }
                if (_signalAck[enumRC5X0Command_Motion.GPSX].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GPSX]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SPOT ===============================================
        private void Spot(int nBit, bool bOn)
        {
            _signalAck[enumRC5X0Command_Motion.SPOT].Reset();
            m_Socket.SendCommand(string.Format("SPOT({0},{1})", nBit, bOn ? 1 : 0));
        }
        public void SpotW(int nTimeout, int nBit, bool bOn)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Spot(nBit, bOn);
                if (!_signalAck[enumRC5X0Command_Motion.SPOT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SPOT]));
                }
                if (_signalAck[enumRC5X0Command_Motion.SPOT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SPOT]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GTDT DMNT ==========================================
        private void DMNTGtdt(enumRC550Axis axis)
        {
            _signalAck[enumRC5X0Command_Motion.GTDT].Reset();
            m_Socket.SendCommand(string.Format("DMNT.GTDT[{0}]", m_AxisName[(int)axis]));
        }
        public void DMNTGtdtW(int nTimeout, enumRC550Axis axis)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                enumRC5X0Command_Motion cmd = enumRC5X0Command_Motion.GTDT;

                m_nDMNTAxs = (int)axis;

                DMNTGtdt(axis);

                if (!_signalAck[cmd].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[cmd]));
                }
                if (_signalAck[cmd].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[cmd]));
                }
            }
            m_nDMNTAxs = -1;
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== AxisStep =============================================
        private void AxisStep(enumRC550Axis axis, int pluse)
        {
            _signalAck[enumRC5X0Command_Motion.STEP].Reset();
            m_Socket.SendCommand(string.Format("{0}.STEP({1})", m_AxisName[(int)axis], pluse));
        }
        public void AxisStepW(int nTimeout, enumRC550Axis axis, int pluse)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumRC500SignalTable.MotionCompleted].Reset();

                AxisStep(axis, pluse);

                if (!_signalAck[enumRC5X0Command_Motion.STEP].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.STEP]));
                }
                if (_signalAck[enumRC5X0Command_Motion.STEP].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.STEP]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }

        #endregion

        #region =========================== GMAP =================================================
        private void Gmap(int n)
        {
            _signalAck[enumRC5X0Command_Motion.GMAP].Reset();
            string strCmd;
            if (n == -1)
                strCmd = "GMAP";
            else
                strCmd = string.Format("GMAP({0})", n);
            m_Socket.SendCommand(strCmd);
        }
        public void GmapW(int nTimeout, int n = -1)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Gmap(n);
                if (!_signalAck[enumRC5X0Command_Motion.GMAP].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GMAP]));
                }
                if (_signalAck[enumRC5X0Command_Motion.GMAP].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GMAP]));
                }
            }
            else
            {
                switch (n)
                {
                    case -1:
                    case 0: m_strGmap = "1100000000001"; break;//slot1->slot13
                    case 1: m_strGmap_result = "1,+000047885,+000049686,+000001801"; break;
                    case 2: m_strGmap_result = "1,+000056837,+000058635,+000001798"; break;
                    case 13: m_strGmap_result = "1,+000167886,+000169685,+000001799"; break;
                    default: m_strGmap_result = "0,+000000000,+000000000,+000000000"; break;
                }

            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== SMAP =================================================
        private void Smap(int n)
        {
            _signalAck[enumRC5X0Command_Motion.SMAP].Reset();
            string strCmd;
            if (n == -1)
                strCmd = "SMAP";
            else
                strCmd = string.Format("SMAP({0})", n);
            m_Socket.SendCommand(strCmd, n);
        }
        public void SmapW(int nTimeout, int n = -1)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Smap(n);
                if (!_signalAck[enumRC5X0Command_Motion.SMAP].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SMAP]));
                }
                if (_signalAck[enumRC5X0Command_Motion.SMAP].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SMAP]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        public void SmapW_BypassCancel(int n = -1)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Smap(n);
                if (!_signalAck[enumRC5X0Command_Motion.SMAP].WaitOne(100))
                {
                    //SendAlmMsg(enumRC500Error.AckTimeout);
                    //throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SMAP]));
                }
                //if (_signalAck[enumRC5X0Command_Motion.SMAP].bAbnormalTerminal)
                //{
                //    SendAlmMsg(enumRC500Error.SendCommandFailure);
                //    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SMAP]));
                //}
            }
            else
            {

            }
            _signalSubSequence.Set();
        }

        #endregion

        #region =========================== DMPR =================================================
        private void GetDmpr(int n)
        {
            _signalAck[enumRC5X0Command_Motion.GetDMPR].Reset();
            m_Socket.SendCommand(string.Format("DMPR.GTDT[{0}]", n));
        }
        public void GetDmprW(int nTimeout, int n)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                GetDmpr(n);
                if (!_signalAck[enumRC5X0Command_Motion.GetDMPR].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GetDMPR]));
                }
                if (_signalAck[enumRC5X0Command_Motion.GetDMPR].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.GetDMPR]));
                }
            }
            else
            {
                AnalysisGTDT("aTBL1.DMPR.GTDT", "28,0,13,1,46834,166834,1000,2000,-,-,-,-,1700,8000,5000,\"\"");
            }
            _signalSubSequence.Set();
        }
        private void SetDmpr(int n, string strDat)
        {
            _signalAck[enumRC5X0Command_Motion.SetDMPR].Reset();
            m_Socket.SendCommand(string.Format("DMPR.STDT[{0}]={1}", n, strDat));
        }
        public void SetDmprW(int nTimeout, int n, string strDat)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                SetDmpr(n, strDat);
                if (!_signalAck[enumRC5X0Command_Motion.SetDMPR].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SetDMPR]));
                }
                if (_signalAck[enumRC5X0Command_Motion.SetDMPR].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_Motion.SetDMPR]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion
        //==============================================================================
        public void ResetChangeModeCompleted()
        {
            _signals[enumRC500SignalTable.Remote].Reset();
        }
        public void WaitChangeModeCompleted(int nTimeout)
        {
            if (!Simulate)
            {
                if (!_signals[enumRC500SignalTable.Remote].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.InitialFailure);
                    throw new SException((int)enumRC500Error.InitialFailure, string.Format("Wait Mode was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumRC500SignalTable.Remote].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.InitialFailure);
                    throw new SException((int)enumRC500Error.InitialFailure, string.Format("Motion is Mode end."));
                }
            }
        }


        public void ResetProcessCompleted()
        {
            _signals[enumRC500SignalTable.ProcessCompleted].Reset();
        }
        public void WaitProcessCompleted(int nTimeout)
        {
            if (Connected)
            {
                if (!_signals[enumRC500SignalTable.ProcessCompleted].WaitOne(nTimeout))
                    throw new SException((int)enumRC500Error.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                if (_signals[enumRC500SignalTable.ProcessCompleted].bAbnormalTerminal)
                    throw new SException((int)enumRC500Error.MotionAbnormal, string.Format("Motion is abnormal end."));
            }
            else
                SpinWait.SpinUntil(() => false, 500);
        }
        private bool _RetryOneTimes = true;
        public void ResetInPos()
        {
            _signals[enumRC500SignalTable.MotionCompleted].Reset();
            _RetryOneTimes = true;
        }
        public void WaitInPos(int nTimeout)
        {
            if (Connected)
            {
                if (!_signals[enumRC500SignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    if (_RetryOneTimes)
                    {
                        WriteLog("當移動到目前位置，曾經發生過先收到11000在收到11010");
                        WriteLog("因此再補一個STAT，更新狀態");
                        WriteLog("這是馬達問題");
                        StatW(1000);

                        if (!_signals[enumRC500SignalTable.MotionCompleted].WaitOne(nTimeout))
                            throw new SException((int)enumRC500Error.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                        if (_signals[enumRC500SignalTable.MotionCompleted].bAbnormalTerminal)
                            throw new SException((int)enumRC500Error.MotionAbnormal, string.Format("Motion is abnormal end."));
                        _RetryOneTimes = false;
                    }
                    else
                        throw new SException((int)enumRC500Error.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumRC500SignalTable.MotionCompleted].bAbnormalTerminal)
                    throw new SException((int)enumRC500Error.MotionAbnormal, string.Format("Motion is abnormal end."));

                GTDT_OrgnSensorW();
            }
            else
                SpinWait.SpinUntil(() => false, 500);
        }
        public void WaitInPos(int nTimeout, enumRC550Axis axis, int nPulse)
        {
            if (!Simulate)
            {
                if (!_signals[enumRC500SignalTable.MotionCompleted].WaitOne(nTimeout))
                    throw new SException((int)enumRC500Error.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                if (_signals[enumRC500SignalTable.MotionCompleted].bAbnormalTerminal)
                    throw new SException((int)enumRC500Error.MotionAbnormal, string.Format("Motion is abnormal end."));
                //SpinWait.SpinUntil(() => false, 100);//等等再問位置
                AxisGposW(m_nAckTimeout, axis);
                if (Math.Abs(GetPulse(axis) - nPulse) > 100)
                {
                    throw new SException((int)enumRC500Error.MotionAbnormal, string.Format("Motion is abnormal end.Pulse[{0}]", GetPulse(axis)));
                }

                if (Math.Abs(nPulse) < 10000) { GTDT_OrgnSensorW(); }//原點附近問一下原點sensor
            }
            else
            {
                m_AxisOrgnSen[(int)axis] = Math.Abs(nPulse) < 5000;
                SpinWait.SpinUntil(() => false, 500);
            }
        }

        public void ResetOrgnSinal()
        {
            _signals[enumRC500SignalTable.OPRCompleted].Reset();
        }
        public void WaitOrgnCompleted(int TimeOut)
        {
            if (Connected)
            {
                if (!_signals[enumRC500SignalTable.OPRCompleted].WaitOne(TimeOut))
                    throw new SException((int)(enumRC500Error.OriginPosReturnFailure), "Robot Orgn Fail");
                if (_signals[enumRC500SignalTable.OPRCompleted].bAbnormalTerminal)
                    throw new SException((int)(enumRC500Error.OriginPosReturnFailure), "Robot Orgn Fail");
            }
            else
                SpinWait.SpinUntil(() => false, 500);
        }

        //==============================================================================
        #region =========================== CommandTable =======================================
        private Dictionary<enumRC5X0Command_Motion, string> _dicCmdsTable = new Dictionary<enumRC5X0Command_Motion, string>()
        {
            {enumRC5X0Command_Motion.ORGN,"ORGN"},
            {enumRC5X0Command_Motion.HOME,"HOME"},
            {enumRC5X0Command_Motion.MABS,"MABS"},
            {enumRC5X0Command_Motion.MREL,"MREL"},
            {enumRC5X0Command_Motion.CLMP,"CLMP"},//沒用到
            {enumRC5X0Command_Motion.UCLM,"UCLM"},//沒用到

            {enumRC5X0Command_Motion.EVNT,"EVNT"},
            {enumRC5X0Command_Motion.RSTA,"RSTA"},
            {enumRC5X0Command_Motion.INIT,"INIT"},
            {enumRC5X0Command_Motion.STOP,"STOP"},
            {enumRC5X0Command_Motion.PAUS,"PAUS"},
            {enumRC5X0Command_Motion.MODE,"MODE"},
            {enumRC5X0Command_Motion.WTDT,"WTDT"},
            {enumRC5X0Command_Motion.RTDT,"RTDT"},
            {enumRC5X0Command_Motion.SSPD,"SSPD"},
            {enumRC5X0Command_Motion.EXCT,"EXCT"},
            {enumRC5X0Command_Motion.TORQ,"TORQ"},

            {enumRC5X0Command_Motion.SMAP,"SMAP"},//mapping
            {enumRC5X0Command_Motion.GMAP,"GMAP"},//mapping
            {enumRC5X0Command_Motion.GetDMPR,"DMPR.GTDT"},//mapping
            {enumRC5X0Command_Motion.SetDMPR,"DMPR.STDT"},//mapping

            {enumRC5X0Command_Motion.STAT,"STAT"},
            {enumRC5X0Command_Motion.GPIO,"GPIO"},
            {enumRC5X0Command_Motion.GVER,"GVER"},
            {enumRC5X0Command_Motion.GLOG,"GLOG"},
            {enumRC5X0Command_Motion.STIM,"STIM"},
            {enumRC5X0Command_Motion.GTIM,"GTIM"},//沒用到
            {enumRC5X0Command_Motion.GPOS,"GPOS"},
            {enumRC5X0Command_Motion.TDST,"TDST"},
            {enumRC5X0Command_Motion.GTDT,"GTDT"},
            {enumRC5X0Command_Motion.GPSX,"GPSX"},
            {enumRC5X0Command_Motion.SPOT,"SPOT"},
            {enumRC5X0Command_Motion.GTAD,"GTAD"},
            {enumRC5X0Command_Motion.CNCT,"CNCT"},
            {enumRC5X0Command_Motion.STEP,"STEP"},





        };
        #endregion
        #region =========================== Signals ============================================
        Dictionary<enumRC5X0Command_Motion, SSignal> _signalAck = new Dictionary<enumRC5X0Command_Motion, SSignal>();
        Dictionary<enumRC500SignalTable, SSignal> _signals = new Dictionary<enumRC500SignalTable, SSignal>();
        SSignal _signalSubSequence;
        #endregion
        #region =========================== OnOccurError =======================================
        //  發生STAT異常
        protected void SendAlmMsg(string strCode)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));

        }
        //  解除STAT異常
        protected void RestAlmMsg(string strCode)
        {
            WriteLog(string.Format("Rest stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));

        }
        //  Cancel Code
        protected void SendCancelMsg(string strCode)
        {
            WriteLog(string.Format("Occur cancel Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  Custom Error
        protected void SendAlmMsg(enumRC500Error eAlarm)
        {
            WriteLog(string.Format("Occur eAlarm Error : {0}", eAlarm));
            int nCode = (int)eAlarm;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        #endregion =================================================================
        #region =========================== CreateMessage ======================================
        public Dictionary<int, string> m_dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();
        protected abstract void CreateMessage();
        #endregion


        public int GetPulse(enumRC550Axis axis, bool oGPOS = false)
        {
            int n = 0;
            try
            {
                if (oGPOS)
                    this.AxisGposW(m_nAckTimeout, axis);

                switch (axis)
                {
                    case enumRC550Axis.AXS1:

                        n = m_AxisPulse[0];
                        break;
                    case enumRC550Axis.AXS2:
                        n = m_AxisPulse[1];
                        break;
                    case enumRC550Axis.AXS3:
                        n = m_AxisPulse[2];
                        break;
                    case enumRC550Axis.AXS4:
                        n = m_AxisPulse[3];
                        break;
                    case enumRC550Axis.AXS5:
                        n = m_AxisPulse[4];
                        break;
                    case enumRC550Axis.AXS6:
                        n = m_AxisPulse[5];
                        break;
                    case enumRC550Axis.None:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex) { _logger.WriteLog("[TBL{0}] <<Exception>> :" + ex, this.BodyNo); }
            return n;
        }
        public bool GetOrgnSensor(enumRC550Axis axis)
        {
            bool b = false;
            try
            {
                switch (axis)
                {
                    case enumRC550Axis.AXS1:
                        b = m_AxisOrgnSen[0];
                        break;
                    case enumRC550Axis.AXS2:
                        b = m_AxisOrgnSen[1];
                        break;
                    case enumRC550Axis.AXS3:
                        b = m_AxisOrgnSen[2];
                        break;
                    case enumRC550Axis.AXS4:
                        b = m_AxisOrgnSen[3];
                        break;
                    case enumRC550Axis.AXS5:
                        b = m_AxisOrgnSen[4];
                        break;
                    case enumRC550Axis.AXS6:
                        b = m_AxisOrgnSen[5];
                        break;
                    case enumRC550Axis.None:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex) { _logger.WriteLog("[TBL{0}] <<Exception>> :" + ex, this.BodyNo); }
            return b;
        }
        public bool GetInput(int nBit)
        {
            if (m_GPIO_Input.Length < nBit) return false;
            return m_GPIO_Input[nBit];
        }
        public void SetInput(int nBit, bool bOn)
        {
            if (m_GPIO_Input.Length < nBit || Simulate == false) return;
            m_GPIO_Input[nBit] = bOn;
            OnIOChange?.Invoke(this, new NotifyGPIOEventArgs(m_GPIO_Input, m_GPIO_Output));
        }
        public bool GetOutput(int nBit)
        {
            if (m_GPIO_Output.Length < nBit) return false;
            return m_GPIO_Output[nBit];
        }
        public void SetOutput(int nBit, bool bOn)
        {

            this.SpotW(m_nAckTimeout, nBit, bOn);
        }
        public string GetGmap(int n = -1)
        {
            if (n < 0)
                return m_strGmap;
            else
                return m_strGmap_result;
        }



    }


}
