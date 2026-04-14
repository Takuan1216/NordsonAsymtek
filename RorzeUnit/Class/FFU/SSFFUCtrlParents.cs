using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Markup;


namespace RorzeUnit.Class.FFU
{
    public abstract class SSFFUCtrlParents
    {
        protected enum enumRTUfnc
        {
            ReadCoilStatus = 0x01,
            ReadInputStatus = 0x02,
            ReadHoldingRegisters = 0x03,
            ReadInputRegisters = 0x04,
            ForceSingleCoil = 0x05,
            PresetSingleRegister = 0x06,
        }
        protected enum enumCheckSumType { CRC16, Lowerbyte }

        private SLogger m_logger = SLogger.GetLogger("FFU");

        protected object m_lockSend = new object();
        protected object m_lockRecv = new object();

        protected ConcurrentQueue<string> m_QueRecvBuffer = new ConcurrentQueue<string>();
        private SPollingThread m_PollingDequeueRecv;
        private SPollingThread m_PollingStatus;

        private int m_nBodyNo;
        protected bool m_bSimulate;
        protected bool m_bDisable;
        protected bool m_bConnect;

        protected bool[] m_bOnOff;
        protected int[] m_nSpeed;
        protected int[] m_nSpeedMax;
        protected int[] m_nSpeedMin;

        public bool _Disable { get { return m_bDisable; } }
        public int _SlaveCount { get { return m_nSpeed.Length; } }
        public bool _IsConnect { get { return m_bConnect; } }

        public SSFFUCtrlParents(int nBody, int nSlaveCount, bool bSimulate, bool bDisable)
        {
            m_nBodyNo = nBody;
            m_bSimulate = bSimulate;
            m_bDisable = bDisable;

            m_PollingDequeueRecv = new SPollingThread(10);
            m_PollingDequeueRecv.DoPolling += PollingDequeueRecv;
            m_PollingDequeueRecv.Set();

            m_PollingStatus = new SPollingThread(1000);
            m_PollingStatus.DoPolling += PollingStatus;
            m_PollingStatus.Set();

            m_bOnOff = new bool[nSlaveCount];
            m_nSpeed = new int[nSlaveCount];
            m_nSpeedMax = new int[nSlaveCount];
            m_nSpeedMin = new int[nSlaveCount];
			if (m_bDisable == false)
            {
                m_logger = SLogger.GetLogger("FFU");
            }
        }

        ~SSFFUCtrlParents() { }

        /// <summary>
        /// 紀錄LOG
        /// </summary>
        /// <param name="strContent"></param>
        /// <param name="meberName"></param>
        /// <param name="lineNumber"></param>
        protected void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[FFU{0}] : {1}  at line {2} ({3})", m_nBodyNo, strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }
        /// <summary>
        /// 取得checksum
        /// </summary>
        /// <param name="strInput">使用空白來間格輸入 ex: 00 01 02 03</param>
        /// <param name="type">checksumType</param>
        /// <returns></returns>
        protected static string getChecksum(string strInput, enumCheckSumType eType)
        {

            strInput = strInput.Replace(" ", "");

            string[] strArry = new string[strInput.Length / 2];

            for (int i = 0; i < strInput.Length / 2; i++)
            {
                strArry[i] = strInput.Substring(i * 2, 2);
            }



            switch (eType)
            {
                case enumCheckSumType.CRC16:
                    return calChecksum_CRC16(strArry);
                case enumCheckSumType.Lowerbyte:
                    return calChecksum_lowerbyte(strArry);
                default:
                    return string.Empty;
            }
        }
        /// <summary>
        /// 計算 總和的低位元 checksum
        /// </summary>
        /// <param name="strInput">計算的字串，陣列內每個字元軍需大寫</param>
        /// <returns></returns>
        private static string calChecksum_lowerbyte(string[] strInput)
        {
            string strResult = string.Empty;
            int reg_crc = 0x0000;
            foreach (string str in strInput)
            {
                checkString(str);
                reg_crc += Convert.ToInt32(str, 16);
            }
            strResult = Convert.ToString(reg_crc, 16);

            strResult = addString("0", 4, strResult);

            strResult = strResult.Substring(2, 2).ToUpper();

            return strResult;
        }
        /// <summary>
        /// 計算CRC16 checksum
        /// </summary>
        /// <param name="strInput">計算的字串，陣列內每個字元軍需大寫</param>
        /// <returns></returns>
        private static string calChecksum_CRC16(string[] strInput)
        {
            int reg_crc = 0xFFFF;

            foreach (string str in strInput)
            {
                if (str == string.Empty || str == " ")
                { continue; }
                checkString(str);
                reg_crc ^= Convert.ToInt32(str, 16);
                for (int i = 0; i < 8; i++)
                {

                    if ((reg_crc & 0x01) == 1)
                    {
                        reg_crc = (reg_crc >> 1) ^ 0xA001;
                    }
                    else
                    {
                        reg_crc = reg_crc >> 1;
                    }
                }
            }
            string strResult = Convert.ToString(reg_crc, 16);

            strResult = addString("0", 4, strResult);

            string[] strArray = new string[2];
            strArray[1] = strResult.Substring(0, 2).ToUpper();
            strArray[0] = strResult.Substring(2, 2).ToUpper();
            string strReturn = string.Format($"{strArray[0]} {strArray[1]}");
            return strReturn;
        }
        /// <summary>
        /// 檢查字串是否合法
        /// </summary>
        /// <param name="strInput"></param>
        /// <returns></returns>
        private static void checkString(string strInput)
        {
            if (strInput.Length > 2)
            {
                throw new Exception(string.Format("{0},maximum input is 2 digits", strInput));
            }
            char[] chArray = strInput.ToCharArray();
            foreach (char ch in chArray)
            {
                if (ch > 'F')
                {
                    throw new Exception(string.Format("{0}:{1} need between 0 to F", strInput, ch));
                }
            }
        }
        /// <summary>
        /// 在字串前面增加字串,直到指定長度
        /// </summary>
        /// <param name="addStr">要增加的字串</param>
        /// <param name="number">字串需求長度</param>
        /// <param name="str">要增加的字串</param>
        /// <returns></returns>
        private static string addString(string addStr, int number, string str)
        {
            while (str.Length < number)
            {
                str = addStr + str;
            }
            return str;
        }
        /// <summary>
        /// 16進位字串轉Byte
        /// </summary>
        /// <param name="strInput"></param>
        /// <returns></returns>
        protected static byte[] convertHexStringToByteArray(string strInput)
        {

            strInput = strInput.Replace(" ", "");

            string[] strArry = new string[strInput.Length / 2];

            for (int i = 0; i < strInput.Length / 2; i++)
            {
                strArry[i] = strInput.Substring(i * 2, 2);
            }

            int length = strArry.Length;
            byte[] byteArray = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byteArray[i] = Convert.ToByte(Convert.ToInt32(strArry[i], 16));
            }
            return byteArray;
        }





        public abstract bool ToConnect();


        /// <summary>
        /// 獨立執行序處理收到內容
        /// </summary>
        private void PollingDequeueRecv()
        {
            try
            {
                SpinWait.SpinUntil(() => false, 1);
                if (_IsConnect == false) return;
                string strContent;
                if (m_QueRecvBuffer.TryDequeue(out strContent) == false) return;
                ProcessingRecive(strContent);
            }
            catch (Exception ex)
            {
                WriteLog("Exception:" + ex);
            }
        }
        /// <summary>
        /// 收到信息底層要解析
        /// </summary>
        /// <param name="strContent"></param>
        /// <returns></returns>
        protected abstract bool ProcessingRecive(string strContent);
        protected virtual void PollingStatus()
        {
            try
            {
                if (_IsConnect == false) return;

                for (int i = 0; i < _SlaveCount; i++)
                {
                    GetSpeedInformation(i + 1);
                }
                //for (int i = 0; i < m_nSpeed.Count(); i++)
                //{
                //    GetSpeedInformation(i + 1, true);
                //    SpinWait.SpinUntil(() => false, 1);
                //}
            }
            catch (Exception ex)
            {
                WriteLog("Exception:" + ex);
            }
        }

        #region 方法
        /// <summary>
        /// FFU ON/OFF
        /// </summary>
        /// <param name="nFFU_address"></param>
        /// <param name="bIsOpen"></param>
        /// <returns></returns>
        public abstract bool SetOperationCtrl(int nFFU_address, bool bIsOpen);
        /// <summary>
        /// 設定FFU轉速
        /// </summary>
        /// <param name="iFFU_Number"></param>
        /// <param name="nSpeed"></param>
        /// <returns></returns>
        public abstract bool SetSpeedSetting(int nFFU_address, int nSpeed);
        /// <summary>
        /// 設定速度上限
        /// </summary>
        /// <param name="nFFU_address"></param>
        /// <param name="nSpeed"></param>
        /// <returns></returns>
        public abstract bool SetSpeedLimitMax(int nFFU_address, int nSpeed);
        /// <summary>
        /// 設定速度下限
        /// </summary>
        /// <param name="nFFU_address"></param>
        /// <param name="nSpeed"></param>
        /// <returns></returns>
        public abstract bool SetSpeedLimitMin(int nFFU_address, int nSpeed);
        /// <summary>
        /// 一次問所有的Address
        /// </summary>
        /// <param name="nFFU_address"></param>
        /// <returns></returns>
        public abstract bool GetSettingInfo(int nFFU_address);
        /// <summary>
        /// 單問轉速
        /// </summary>
        /// <param name="nFFU_address"></param>
        /// <returns></returns>
        public abstract bool GetSpeedInformation(int nFFU_address, bool bPassLog = false);
        /// <summary>
        /// 問上限
        /// </summary>
        /// <param name="nFFU_address"></param>
        /// <returns></returns>
        public abstract bool GetSpeedLimitMax(int nFFU_address);
        /// <summary>
        /// 問下限
        /// </summary>
        /// <param name="nFFU_address"></param>
        /// <returns></returns>
        public abstract bool GetSpeedLimitMin(int nFFU_address);
        #endregion

        public bool[] GetOnOff() { return m_bOnOff.ToArray(); }
        public int[] GetSpeed() { return m_nSpeed.ToArray(); }
        public int[] GetSpeedMax() { return m_nSpeedMax.ToArray(); }
        public int[] GetSpeedMin() { return m_nSpeedMin.ToArray(); }


    }


    public class SSFFU_TOPWELL : SSFFUCtrlParents
    {
        enum enumAddress
        {
            OperationControl = 0x00,
            SpeedSetting = 0x01,
            SpeedStepInformation = 0x02,//當前轉入級距
            AlarmCode = 0x03,//當前異常
            SpeedInformation = 0x04,//當前速度
            IPMTemperature = 0x08,//IPM溫度
            ExtendDOcontrol = 0x09,//輸出控制            
            FFUaddress = 0x0C,//指撥通信地址
            SpeedLimitMax = 0x0D,
            SpeedLimitMin = 0x0E,
            GroupIDSetting = 0x0F,
            BoardVersionPIC = 0x10,
            Dip1Satus = 0x11,
            DSPVER = 0x12,
        }

        Dictionary<enumAddress, SSignal> _signalAck = new Dictionary<enumAddress, SSignal>();

        private enumAddress m_eAddress_Cmd;
        private int m_nCom;
        private SerialPort m_comport = new SerialPort();

        public SSFFU_TOPWELL(int nBody, int nSlaveCount, bool bSimulate, bool bDisable, int nCom) : base(nBody, nSlaveCount, bSimulate, bDisable)
        {
            m_nCom = nCom;

            foreach (enumAddress enumType in System.Enum.GetValues(typeof(enumAddress)))
            {
                _signalAck.Add(enumType, new SSignal(false, EventResetMode.ManualReset));
            }

        }

        #region Modbus RTU 通訊
        public override bool ToConnect()
        {
            bool bSucc = false;
            try
            {
                m_comport.PortName = "COM" + m_nCom.ToString();
                m_comport.BaudRate = 9600;
                m_comport.Parity = Parity.None;
                m_comport.StopBits = StopBits.One;
                m_comport.DataBits = 8;

                m_comport.DataReceived += new SerialDataReceivedEventHandler(ReceivedEvent);
                m_comport.Open();
                m_comport.DiscardInBuffer();
                m_comport.DiscardOutBuffer();
                bSucc = m_comport.IsOpen;

                if (bSucc)
                {

                    for (int i = 0; i < m_nSpeed.Length; i++)
                    {
                        GetSpeedLimitMax(i + 1);
                        SpinWait.SpinUntil(() => false, 10);
                        GetSpeedLimitMin(i + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            return bSucc;
        }

        /// <summary>
        /// Comport send
        /// </summary>
        /// <param name="strCmd"></param>
        /// <returns></returns>
        private bool ComportSend(string strCmd, bool bPassLog = false)
        {
            bool bSucc = false;
            try
            {
                lock (m_lockSend)
                {
                    strCmd += " " + getChecksum(strCmd, enumCheckSumType.CRC16);
                    if (bPassLog == false) WriteLog("Send:" + strCmd);
                    //byte[] byteArry = System.Text.ASCIIEncoding.ASCII.GetBytes(strCmd);//第一種方案
                    byte[] byteArry = convertHexStringToByteArray(strCmd);//第二組方案
                    m_comport.Write(byteArry, 0, byteArry.Length);
                    bSucc = true;
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            return bSucc;
        }
        private bool SendCmd(enumAddress eAddress, string strCmd, bool bPassLog = false)
        {
            bool bSuc = false;
            _signalAck[eAddress].Reset();
            while (true)
            {
                m_eAddress_Cmd = eAddress;

                if (ComportSend(strCmd, bPassLog) == false)
                    break;
                if (_signalAck[eAddress].WaitOne(3000) == false)
                    break;
                if (_signalAck[eAddress].bAbnormalTerminal)
                    break;
                bSuc = true;
                break;
            }
            return bSuc;
        }

        /// <summary>
        /// 收到內容塞入Queue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceivedEvent(object sender, SerialDataReceivedEventArgs e)
        {
            int nByteToRead = (sender as SerialPort).BytesToRead;
            lock (m_lockRecv)
            {
                StringBuilder sb = new StringBuilder();
                int nIdx = 0;
                while (nIdx < nByteToRead)
                {
                    int nHex = m_comport.ReadByte();
                    sb.Append(nHex.ToString("X2"));
                    nIdx++;
                }
                string strContent = sb.ToString();
                WriteLog("Recv:" + strContent);
                m_QueRecvBuffer.Enqueue(strContent);
            }
        }
        #endregion

        protected override bool ProcessingRecive(string strContent)
        {
            bool bSuc = false;
            try
            {
                while (true)
                {
                    if (strContent == "")
                        break;

                    //string[] strMsg = strContent.Split('-');//收到的byte array 轉換時會自動用-來連結 ex:00-01-FF

                    string[] strMsg = new string[strContent.Length / 2];

                    for (int i = 0; i < strContent.Length / 2; i++)
                    {
                        strMsg[i] = strContent.Substring(i * 2, 2);
                    }

                    int nSlaveAddress = 0;
                    if (strMsg.Length > 0 && int.TryParse(strMsg[0], out nSlaveAddress) == false)
                        break;
                    int nFunction = 0;
                    if (strMsg.Length > 1 && int.TryParse(strMsg[1], out nFunction) == false)
                        break;

                    int nDataCount = 0;
                    if (strMsg.Length > 2 && int.TryParse(strMsg[2], out nDataCount) == false)
                        break;

                    string[] strArryDat = new string[nDataCount / 2];

                    for (int i = 0; i < nDataCount / 2; i++)
                    {
                        strArryDat[i] = strMsg[3 + 2 * i] + strMsg[3 + 2 * i + 1];
                    }


                    bool bSend = strMsg[strMsg.Length - 1] == "01";//底層會增加 最後一碼為是否傳送給Server

                    if (nFunction == 0x83 || nFunction == 0x84)//fnc x03/x04失敗回傳
                        break;

                    if (nFunction == 0x86)//fnc x06失敗回傳
                        break;

                    if (nFunction == 3)
                    {
                        int value;
                        switch (m_eAddress_Cmd)
                        {
                            case enumAddress.OperationControl:
                                break;
                            case enumAddress.SpeedSetting:

                                break;
                            case enumAddress.SpeedStepInformation:
                                break;
                            case enumAddress.AlarmCode:
                                break;
                            case enumAddress.SpeedInformation:
                                value = Convert.ToInt32(strArryDat[0], 16);
                                m_nSpeed[nSlaveAddress - 1] = value;
                                break;
                            case enumAddress.IPMTemperature:
                                break;
                            case enumAddress.ExtendDOcontrol:
                                break;
                            case enumAddress.FFUaddress:
                                break;
                            case enumAddress.SpeedLimitMax:
                                value = Convert.ToInt32(strArryDat[0], 16);
                                m_nSpeedMax[nSlaveAddress - 1] = value;
                                break;
                            case enumAddress.SpeedLimitMin:
                                value = Convert.ToInt32(strArryDat[0], 16);
                                m_nSpeedMin[nSlaveAddress - 1] = value;
                                break;
                            case enumAddress.GroupIDSetting:
                                break;
                            case enumAddress.BoardVersionPIC:
                                break;
                            case enumAddress.Dip1Satus:
                                break;
                            case enumAddress.DSPVER:
                                break;
                            default:
                                break;
                        }
                    }
                    _signalAck[m_eAddress_Cmd].Set();

                    bSuc = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                WriteLog("Exception:" + ex);
            }
            return bSuc;
        }

        #region 繼承方法
        //X06
        public override bool SetOperationCtrl(int nSlave, bool bOn)
        {
            //example 01 - 06 - 00 - 00 - 00 - 01 - 48 - 0A
            string strSlaveAddress = string.Format("{0:X2}", nSlave);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.PresetSingleRegister);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.OperationControl);
            string strData = string.Format("{0:X4}", bOn ? 1 : 0);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);
            bool bSuc = SendCmd(enumAddress.OperationControl, strCmd);
            if (bSuc) m_bOnOff[nSlave] = bOn;
            return bSuc;
        }
        public override bool SetSpeedSetting(int nSlave, int nSpeed)
        {
            //example 01 - 06 - 00 - 01 - 02 - BC - D8 - DB
            string strSlaveAddress = string.Format("{0:X2}", nSlave);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.PresetSingleRegister);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.SpeedSetting);
            string strData = string.Format("{0:X4}", nSpeed);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);
            bool bSuc = SendCmd(enumAddress.SpeedSetting, strCmd);
            return bSuc;
        }
        public override bool SetSpeedLimitMax(int nSlave, int nSpeed)
        {
            //example 01 - 06 - 00 - 0D - 06 - 72 - 9B - BC
            string strSlaveAddress = string.Format("{0:X2}", nSlave);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.PresetSingleRegister);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.SpeedLimitMax);
            string strData = string.Format("{0:X4}", nSpeed);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);
            bool bSuc = SendCmd(enumAddress.SpeedLimitMax, strCmd);
            return bSuc;
        }
        public override bool SetSpeedLimitMin(int nSlave, int nSpeed)
        {
            //example 01 - 06 - 00 - 0E - 00 - 00 - E8 - 09
            string strSlaveAddress = string.Format("{0:X2}", nSlave);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.PresetSingleRegister);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.SpeedLimitMin);
            string strData = string.Format("{0:X4}", nSpeed);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);
            bool bSuc = SendCmd(enumAddress.SpeedLimitMin, strCmd);
            return bSuc;
        }
        //X03
        public override bool GetSettingInfo(int nSlave)//取得資料,起停控制,轉速設定,轉速級距反饋,警報代碼,轉速反饋....等15筆
        {
            //example 01 - 03 - 00 - 00 - 00 - 0F - 05 - CE
            string strSlaveAddress = string.Format("{0:X2}", nSlave);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.ReadHoldingRegisters);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.OperationControl);
            string strData = string.Format("{0:X4}", 15);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);

            bool bSuc = SendCmd(enumAddress.OperationControl, strCmd);
            return bSuc;
        }
        public override bool GetSpeedInformation(int nSlave, bool bPassLog = false)
        {
            //example 01 - 03 - 00 - 04 - 00 - 01 - C5 - CB
            string strSlaveAddress = string.Format("{0:X2}", nSlave);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.ReadHoldingRegisters);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.SpeedInformation);
            string strData = string.Format("{0:X4}", 1);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);

            bool bSuc = SendCmd(enumAddress.SpeedInformation, strCmd, bPassLog);
            return bSuc;
        }
        public override bool GetSpeedLimitMax(int nSlave)
        {
            //example 01 - 03 - 00 - 04 - 00 - 01 - C5 - CB
            string strSlaveAddress = string.Format("{0:X2}", nSlave);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.ReadHoldingRegisters);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.SpeedLimitMax);
            string strData = string.Format("{0:X4}", 1);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);

            bool bSuc = SendCmd(enumAddress.SpeedLimitMax, strCmd);
            return bSuc;
        }
        public override bool GetSpeedLimitMin(int nSlave)
        {
            //example 01 - 03 - 00 - 04 - 00 - 01 - C5 - CB
            string strSlaveAddress = string.Format("{0:X2}", nSlave);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.ReadHoldingRegisters);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.SpeedLimitMin);
            string strData = string.Format("{0:X4}", 1);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);

            bool bSuc = SendCmd(enumAddress.SpeedLimitMin, strCmd);
            return bSuc;
        }
        #endregion

    }


    public class SSFFU_AirTech : SSFFUCtrlParents
    {
        enum enumAddress
        {
            CheckStatus = 0x0004,//40005

            Slave1_SpeedInformation = 0x0005,//40006 當前轉速
            Slave1_OperationControl = 0x0008,//40009
            Slave1_OperatSpeed1 = 0x001C, //40029
            Slave1_OperatSpeed2 = 0x001D, //40030
            Slave1_OperatSpeed3 = 0x001E, //40031

            Slave2_SpeedInformation = 0x0005 + 100,
            Slave2_OperationControl = 0x0008 + 100,
            Slave2_OperatSpeed1 = 0x001C + 100,
            Slave2_OperatSpeed2 = 0x001D + 100,
            Slave2_OperatSpeed3 = 0x001E + 100,

            Slave3_SpeedInformation = 0x0005 + 200,
            Slave3_OperationControl = 0x0008 + 200,
            Slave3_OperatSpeed1 = 0x001C + 200,
            Slave3_OperatSpeed2 = 0x001D + 200,
            Slave3_OperatSpeed3 = 0x001E + 200,

            Slave4_SpeedInformation = 0x0005 + 300,
            Slave4_OperationControl = 0x0008 + 300,
            Slave4_OperatSpeed1 = 0x001C + 300,
            Slave4_OperatSpeed2 = 0x001D + 300,
            Slave4_OperatSpeed3 = 0x001E + 300,



        }

        Dictionary<enumAddress, SSignal> m_dicSignalAck = new Dictionary<enumAddress, SSignal>();

        private enumAddress m_eAddress_Cmd;
        private sClient m_Client;
        private string m_strIP;
        private int m_nPort;

        private ushort[] m_nOperationCtrl;

        private Dictionary<int, ushort> m_dicAddressBuf = new Dictionary<int, ushort>();

        public SSFFU_AirTech(int nBody, int nSlaveCount, bool bSimulate, bool bDisable, string strIP, int nPort) : base(nBody, nSlaveCount, bSimulate, bDisable)
        {
            m_strIP = strIP;
            m_nPort = nPort;
            m_bSimulate = bSimulate;
            m_Client = new sClient();

            m_nOperationCtrl = new ushort[nSlaveCount];

            foreach (enumAddress enumType in System.Enum.GetValues(typeof(enumAddress)))
            {
                m_dicSignalAck.Add(enumType, new SSignal(false, EventResetMode.ManualReset));
            }

            m_Client.onDataReciveByByte += Client_OnDataRecive;
            m_Client.OnConnectChange += Client_OnConnectChange;

            for (int i = 0; i < m_nSpeedMax.Length; i++)
            {
                m_nSpeedMax[i] = 1300;
            }
            for (int i = 0; i < m_nSpeedMin.Length; i++)
            {
                m_nSpeedMin[i] = 700;
            }

        }

        #region Modbus TCP 通訊
        public override bool ToConnect()
        {
            bool bSucc = m_bSimulate;
            try
            {
                if (m_bSimulate == false)
                    m_Client.connect(m_strIP, m_nPort);
            }
            catch (Exception ex)
            {
                WriteLog("<Exception>:" + ex);
            }
            return bSucc;

        }

        private void Client_OnDataRecive(sClient.ReciveByByte args)
        {
            byte[] byteData = args.sReciveData;
            string strData = BitConverter.ToString(byteData);
            m_QueRecvBuffer.Enqueue(strData);
            WriteLog("RECV : " + strData);
        }

        private void Client_OnConnectChange(object sender, bool bConnect)
        {
            WriteLog(string.Format("Client {0}:{1} connected.", m_strIP, m_nPort));
            m_bConnect = bConnect;
        }

        private void Sendcommand_Get(int nSlave, short startAddress, ushort numRegisters)
        {
            lock (m_lockSend)
            {
                try
                {
                    byte[] frame = new byte[12];

                    // 事務標識符 (任意)
                    frame[0] = 0x00;
                    frame[1] = 0x01;

                    // 協議標識符 (0=Modbus)
                    frame[2] = 0x00;
                    frame[3] = 0x00;

                    // 長度字段 (後面的字節數)
                    frame[4] = 0x00;
                    frame[5] = 0x06;

                    // 單元標識符
                    frame[6] = 0x01; //(byte)nSlave; 控制面板包一層只有一站

                    // 功能碼 (0x03 = 讀保持寄存器)
                    frame[7] = 0x03;

                    // 起始地址
                    frame[8] = (byte)(startAddress >> 8);
                    frame[9] = (byte)startAddress;

                    // 寄存器數量
                    frame[10] = (byte)(numRegisters >> 8);
                    frame[11] = (byte)numRegisters;

                    WriteLog("Send : " + BitConverter.ToString(frame));
                    m_Client.Write(frame);
                }
                catch (Exception ex)
                {
                    WriteLog("<Exception>" + ex);
                }
            }
        }

        private void Sendcommand_Set(int nSlave, short startAddress, ushort numRegisters)
        {
            lock (m_lockSend)
            {
                try
                {
                    byte[] frame = new byte[12];

                    // 事務標識符 (任意)
                    frame[0] = 0x00;
                    frame[1] = 0x01;

                    // 協議標識符 (0=Modbus)
                    frame[2] = 0x00;
                    frame[3] = 0x00;

                    // 長度字段 (後面的字節數)
                    frame[4] = 0x00;
                    frame[5] = 0x06;

                    // 單元標識符
                    frame[6] = 0x01; //(byte)nSlave; 控制面板包一層只有一站

                    // 功能碼 (0x06 = 寫保持寄存器)
                    frame[7] = 0x06;

                    // 起始地址
                    frame[8] = (byte)(startAddress >> 8);
                    frame[9] = (byte)startAddress;

                    // 寄存器數值
                    frame[10] = (byte)(numRegisters >> 8);
                    frame[11] = (byte)numRegisters;

                    WriteLog("Send : " + BitConverter.ToString(frame));
                    m_Client.Write(frame);
                }
                catch (Exception ex)
                {
                    WriteLog("<Exception>" + ex);
                }
            }
        }

        #endregion

        protected override bool ProcessingRecive(string strContent)
        {
            //位置    長度  字段          說明
            //0 - 1     2   事務標識符   請求 / 響應匹配ID
            //2 - 3     2   協議標識符   Modbus協議固定為0
            //4 - 5     2   長度         後面跟隨的字節數
            //6         1   單元標識符   從站地址
            //7         1   功能碼       讀保持寄存器功能
            //8         1   字節計數     後面數據的字節數
            //9 - 10    2   寄存器數據   實際讀取的寄存器值

            bool bSuc = false;
            try
            {
                while (true)
                {
                    if (strContent == "")
                        break;


                    // 1. 將字符串轉換為字節數組
                    byte[] bytes = strContent.Split('-').Select(s => Convert.ToByte(s, 16)).ToArray();

                    // 2. 檢查最小長度
                    if (bytes.Length < 9)
                    {
                        WriteLog("Error: Received message length insufficient");
                        break;
                    }

                    // 3. 解析MBAP頭部
                    ushort transactionId = (ushort)((bytes[0] << 8) | bytes[1]);
                    ushort protocolId = (ushort)((bytes[2] << 8) | bytes[3]);
                    ushort length = (ushort)((bytes[4] << 8) | bytes[5]);
                    byte unitId = bytes[6];
                    byte functionCode = bytes[7];

                    Console.WriteLine("=== MBAP頭部 ===");
                    Console.WriteLine($"事務標識符: 0x{transactionId:X4}");
                    Console.WriteLine($"協議標識符: 0x{protocolId:X4} (Modbus)");
                    Console.WriteLine($"長度字段: {length} 字節");
                    Console.WriteLine($"單元標識符: {unitId}");
                    Console.WriteLine($"功能碼: 0x{functionCode:X2}");

                    // 4. 檢查錯誤響應
                    if ((functionCode & 0x80) != 0)
                    {
                        byte errorCode = bytes[8];
                        WriteLog($"Error Response! Code: 0x{errorCode:X2}");
                        break;
                    }

                    // 5. 根據功能碼解析
                    switch (functionCode)
                    {
                        case 0x03: // 讀保持寄存器
                            {
                                if (bytes.Length < 9 + bytes[8])
                                {
                                    WriteLog("Error: Data length does not match declaration");
                                    break;
                                }

                                byte byteCount = bytes[8];
                                Console.WriteLine($"\n=== 寄存器數據 ===");
                                Console.WriteLine($"字節計數: {byteCount}");

                                // 解析寄存器值 (每2字節一個寄存器)
                                for (int i = 0; i < byteCount; i += 2)
                                {
                                    if (i + 9 + 1 >= bytes.Length) break;

                                    ushort registerValue = (ushort)((bytes[9 + i] << 8) | bytes[10 + i]);
                                    WriteLog($" Register{i / 2} value: 0x{registerValue:X4} ({registerValue}) {Convert.ToString(registerValue, 2).PadLeft(16, '0')}");
                                    /*
                                    // 示例中的 00-40 解析
                                    if (i == 0 && byteCount >= 2)
                                    {
                                        Console.WriteLine("\n詳細解析 00-40:");
                                        Console.WriteLine($"十進制: {registerValue}");
                                        Console.WriteLine($"十六進制: 0x{registerValue:X4}");
                                        Console.WriteLine($"二進制: {Convert.ToString(registerValue, 2).PadLeft(16, '0')}");

                                        // 00-40 實際值為 64
                                        Console.WriteLine($"實際值(十進制): {registerValue}");
                                    }
                                    */

                                    switch (m_eAddress_Cmd)
                                    {
                                        case enumAddress.Slave1_SpeedInformation:
                                            m_nSpeed[0] = registerValue;
                                            break;
                                        case enumAddress.Slave1_OperationControl:
                                            m_nOperationCtrl[0] = registerValue;
                                            break;
                                        case enumAddress.Slave1_OperatSpeed1:
                                            break;
                                        case enumAddress.Slave1_OperatSpeed2:
                                            break;
                                        case enumAddress.Slave1_OperatSpeed3:
                                            break;
                                        case enumAddress.Slave2_SpeedInformation:
                                            m_nSpeed[1] = registerValue;
                                            break;
                                        case enumAddress.Slave2_OperationControl:
                                            m_nOperationCtrl[1] = registerValue;
                                            break;
                                        case enumAddress.Slave2_OperatSpeed1:
                                            break;
                                        case enumAddress.Slave2_OperatSpeed2:
                                            break;
                                        case enumAddress.Slave2_OperatSpeed3:
                                            break;
                                        case enumAddress.Slave3_SpeedInformation:
                                            m_nSpeed[2] = registerValue;
                                            break;
                                        case enumAddress.Slave3_OperationControl:
                                            m_nOperationCtrl[2] = registerValue;
                                            break;
                                        case enumAddress.Slave3_OperatSpeed1:
                                            break;
                                        case enumAddress.Slave3_OperatSpeed2:
                                            break;
                                        case enumAddress.Slave3_OperatSpeed3:
                                            break;
                                        case enumAddress.Slave4_SpeedInformation:
                                            m_nSpeed[3] = registerValue;
                                            break;
                                        case enumAddress.Slave4_OperationControl:
                                            m_nOperationCtrl[3] = registerValue;
                                            break;
                                        case enumAddress.Slave4_OperatSpeed1:
                                            break;
                                        case enumAddress.Slave4_OperatSpeed2:
                                            break;
                                        case enumAddress.Slave4_OperatSpeed3:
                                            break;
                                        default:
                                            break;
                                    }

                                }
                            }
                            break;
                        case 0x06: // 寫保持寄存器
                            {
                                if (bytes.Length == 12 && bytes[7] == 0x06)
                                {
                                    WriteLog($" Successfully written to register {(ushort)((bytes[8] << 8) | bytes[9])} Value: {(ushort)((bytes[10] << 8) | bytes[11])}");
                                }
                            }
                            break;
                        default:
                            WriteLog($"Unimplemented function codes: 0x{functionCode:X2}");
                            break;
                    }


                    m_dicSignalAck[m_eAddress_Cmd].Set();

                    bSuc = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                WriteLog("Exception:" + ex);
            }
            return bSuc;
        }

        #region 繼承方法
        //X06 Preset Single registers.
        public override bool SetOperationCtrl(int nSlave, bool bOn)
        {
            bool bSuc = false;
            m_eAddress_Cmd = enumAddress.Slave1_OperationControl + (100 * (nSlave - 1));

            m_dicSignalAck[m_eAddress_Cmd].Reset();
            ushort nValue = 0;
            if (m_dicAddressBuf.ContainsKey((short)m_eAddress_Cmd))
                nValue = m_dicAddressBuf[(short)m_eAddress_Cmd];//4009 地址 8 操作狀態

            if (bOn)
            {
                nValue = (ushort)(nValue & ~(1 << 6));// 將bit6設為0停止
                nValue = (ushort)(nValue | (1 << 7));// 將bit7設為1啟動
                nValue = (ushort)(nValue | (1 << 0));
                nValue = (ushort)(nValue & ~(1 << 1));
                nValue = (ushort)(nValue & ~(1 << 2));
            }
            else
            {

                nValue = (ushort)(nValue | (1 << 6));// 將bit6設為1停止
                nValue = (ushort)(nValue & ~(1 << 7));// 將bit7設為0啟動
                nValue = (ushort)(nValue & ~(1 << 0));
                nValue = (ushort)(nValue & ~(1 << 1));
                nValue = (ushort)(nValue & ~(1 << 2));
            }

            // 切換bit8 (0變1，1變0)
            //nValue = (ushort)(nValue ^ (1 << 7));

            Sendcommand_Set(nSlave, (short)m_eAddress_Cmd, nValue);//4009 地址 8 操作狀態

            bSuc = m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);
            if (bSuc == false)
            {
                WriteLog("Send : Ack timeout");
            }
            else
            {
                m_bOnOff[nSlave] = bOn;
            }
            return bSuc;
        }
        public override bool SetSpeedSetting(int nSlave, int nSpeed)
        {
            bool bSuc = false;
            ushort nValue = (ushort)nSpeed;

            m_eAddress_Cmd = enumAddress.Slave1_OperatSpeed1 + (100 * (nSlave - 1));
            m_dicSignalAck[m_eAddress_Cmd].Reset();
            Sendcommand_Set(nSlave, (short)m_eAddress_Cmd, nValue);//4009 地址 8 操作狀態
            bSuc = m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);

            m_eAddress_Cmd = enumAddress.Slave1_OperatSpeed2 + (100 * (nSlave - 1));
            m_dicSignalAck[m_eAddress_Cmd].Reset();
            Sendcommand_Set(nSlave, (short)m_eAddress_Cmd, nValue);//4009 地址 8 操作狀態
            bSuc &= m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);

            m_eAddress_Cmd = enumAddress.Slave1_OperatSpeed3 + (100 * (nSlave - 1));
            m_dicSignalAck[m_eAddress_Cmd].Reset();
            Sendcommand_Set(nSlave, (short)m_eAddress_Cmd, nValue);//4009 地址 8 操作狀態
            bSuc &= m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);

            if (bSuc == false)
            {
                WriteLog("Send : Ack timeout");
            }
            return bSuc;
        }
        public override bool SetSpeedLimitMax(int nSlave, int nSpeed)
        {
            throw new NotImplementedException();
        }
        public override bool SetSpeedLimitMin(int nSlave, int nSpeed)
        {
            throw new NotImplementedException();
        }

        //X03 Reads the contents of holding registers.
        public override bool GetSettingInfo(int nSlave)
        {
            bool bSuc = false;
            m_eAddress_Cmd = enumAddress.Slave1_OperationControl + (100 * (nSlave - 1));
            m_dicSignalAck[m_eAddress_Cmd].Reset();

            Sendcommand_Get(nSlave, (short)m_eAddress_Cmd, 1);//4009 地址 8

            bSuc = m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);
            if (bSuc == false)
            {
                WriteLog("RECV : Ack timeout");
            }

            return bSuc;
        }
        public override bool GetSpeedInformation(int nSlave, bool bPassLog = false)
        {
            bool bSuc = false;
            m_eAddress_Cmd = enumAddress.Slave1_SpeedInformation + (100 * (nSlave - 1));
            m_dicSignalAck[m_eAddress_Cmd].Reset();

            Sendcommand_Get(nSlave, (short)m_eAddress_Cmd, 1);//4006 地址 5

            bSuc = m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);
            if (bSuc == false)
            {
                WriteLog("RECV : Ack timeout");
            }

            return bSuc;
        }
        public override bool GetSpeedLimitMax(int nSlave)
        {
            //Sendcommand_Get(nSlave, 4, 1);
            return true;
        }
        public override bool GetSpeedLimitMin(int nSlave)
        {
            //Sendcommand_Get(nSlave, 4, 1);
            return true;
        }
        #endregion




    }

    public class SSFFU_NicotraGebhardt : SSFFUCtrlParents
    {
        enum enumAddress
        {
            Slave1_SpeedInformation = 0x1581,




            //Slave1_SpeedInformation = 0x0005,//40006 當前轉速
            //Slave1_OperationControl = 0x0008,//40009
            Slave1_OperatSpeed1 = 0x157C,
            //Slave1_OperatSpeed2 = 0x001D, //40030
            //Slave1_OperatSpeed3 = 0x001E, //40031

            Slave2_SpeedInformation = 0x1581 + 20,
            //Slave2_OperationControl = 0x0008 + 100,
            Slave2_OperatSpeed1 = 0x157C + 20,
            //Slave2_OperatSpeed2 = 0x001D + 100,
            //Slave2_OperatSpeed3 = 0x001E + 100,

            Slave3_SpeedInformation = 0x1581 + 40,
            //Slave3_OperationControl = 0x0008 + 200,
            Slave3_OperatSpeed1 = 0x157C + 40,
            //Slave3_OperatSpeed2 = 0x001D + 200,
            //Slave3_OperatSpeed3 = 0x001E + 200,

            Slave4_SpeedInformation = 0x1581 + 60,
            //Slave4_OperationControl = 0x0008 + 300,
            Slave4_OperatSpeed1 = 0x157C + 60,
            //Slave4_OperatSpeed2 = 0x001D + 300,
            //Slave4_OperatSpeed3 = 0x001E + 300,



        }

        Dictionary<enumAddress, SSignal> m_dicSignalAck = new Dictionary<enumAddress, SSignal>();

        private enumAddress m_eAddress_Cmd;
        private sClient m_Client;
        private string m_strIP;
        private int m_nPort;

        private ushort[] m_nOperationCtrl;

        private Dictionary<int, ushort> m_dicAddressBuf = new Dictionary<int, ushort>();

        public SSFFU_NicotraGebhardt(int nBody, int nSlaveCount, bool bSimulate, bool bDisable, string strIP, int nPort) : base(nBody, nSlaveCount, bSimulate, bDisable)
        {
            m_strIP = strIP;
            m_nPort = nPort;
            m_bSimulate = bSimulate;
            m_Client = new sClient();

            m_nOperationCtrl = new ushort[nSlaveCount];

            foreach (enumAddress enumType in System.Enum.GetValues(typeof(enumAddress)))
            {
                m_dicSignalAck.Add(enumType, new SSignal(false, EventResetMode.ManualReset));
            }

            m_Client.onDataReciveByByte += Client_OnDataRecive;
            m_Client.OnConnectChange += Client_OnConnectChange;

            for (int i = 0; i < m_nSpeedMax.Length; i++)
            {
                m_nSpeedMax[i] = 1600;
            }
            for (int i = 0; i < m_nSpeedMin.Length; i++)
            {
                m_nSpeedMin[i] = 300;
            }

        }

        #region Modbus TCP 通訊
        public override bool ToConnect()
        {
            bool bSucc = m_bSimulate;
            try
            {
                if (m_bSimulate == false)
                    m_Client.connect(m_strIP, m_nPort);
            }
            catch (Exception ex)
            {
                WriteLog("<Exception>:" + ex);
            }
            return bSucc;

        }

        private void Client_OnDataRecive(sClient.ReciveByByte args)
        {
            byte[] byteData = args.sReciveData;
            string strData = BitConverter.ToString(byteData);
            m_QueRecvBuffer.Enqueue(strData);
            WriteLog("RECV : " + strData);
        }

        private void Client_OnConnectChange(object sender, bool bConnect)
        {
            WriteLog(string.Format("Client {0}:{1} connected.", m_strIP, m_nPort));
            m_bConnect = bConnect;
        }

        private void Sendcommand_Get(int nSlave, short startAddress, ushort numRegisters)
        {
            lock (m_lockSend)
            {
                try
                {
                    byte[] frame = new byte[12];

                    // 事務標識符 (任意)
                    frame[0] = 0x00;
                    frame[1] = 0x01;

                    // 協議標識符 (0=Modbus)
                    frame[2] = 0x00;
                    frame[3] = 0x00;

                    // 長度字段 (後面的字節數)
                    frame[4] = 0x00;
                    frame[5] = 0x06;

                    // 單元標識符
                    frame[6] = 0x01; //(byte)nSlave; 控制面板包一層只有一站

                    // 功能碼 (0x03 = 讀保持寄存器)
                    frame[7] = 0x03;

                    // 起始地址
                    frame[8] = (byte)(startAddress >> 8);
                    frame[9] = (byte)startAddress;

                    // 寄存器數量
                    frame[10] = (byte)(numRegisters >> 8);
                    frame[11] = (byte)numRegisters;

                    WriteLog("Send : " + BitConverter.ToString(frame));
                    m_Client.Write(frame);
                }
                catch (Exception ex)
                {
                    WriteLog("<Exception>" + ex);
                }
            }
        }

        private void Sendcommand_Set(int nSlave, short startAddress, ushort numRegisters)
        {
            lock (m_lockSend)
            {
                try
                {
                    byte[] frame = new byte[12];

                    // 事務標識符 (任意)
                    frame[0] = 0x00;
                    frame[1] = 0x01;

                    // 協議標識符 (0=Modbus)
                    frame[2] = 0x00;
                    frame[3] = 0x00;

                    // 長度字段 (後面的字節數)
                    frame[4] = 0x00;
                    frame[5] = 0x06;

                    // 單元標識符
                    frame[6] = 0x01; //(byte)nSlave; 控制面板包一層只有一站

                    // 功能碼 (0x06 = 寫保持寄存器)
                    frame[7] = 0x06;

                    // 起始地址
                    frame[8] = (byte)(startAddress >> 8);
                    frame[9] = (byte)startAddress;

                    // 寄存器數值
                    frame[10] = (byte)(numRegisters >> 8);
                    frame[11] = (byte)numRegisters;

                    WriteLog("Send : " + BitConverter.ToString(frame));
                    m_Client.Write(frame);
                }
                catch (Exception ex)
                {
                    WriteLog("<Exception>" + ex);
                }
            }
        }

        #endregion

        protected override bool ProcessingRecive(string strContent)
        {
            //位置    長度  字段          說明
            //0 - 1     2   事務標識符   請求 / 響應匹配ID
            //2 - 3     2   協議標識符   Modbus協議固定為0
            //4 - 5     2   長度         後面跟隨的字節數
            //6         1   單元標識符   從站地址
            //7         1   功能碼       讀保持寄存器功能
            //8         1   字節計數     後面數據的字節數
            //9 - 10    2   寄存器數據   實際讀取的寄存器值

            bool bSuc = false;
            try
            {
                while (true)
                {
                    if (strContent == "")
                        break;


                    // 1. 將字符串轉換為字節數組
                    byte[] bytes = strContent.Split('-').Select(s => Convert.ToByte(s, 16)).ToArray();

                    // 2. 檢查最小長度
                    if (bytes.Length < 9)
                    {
                        WriteLog("Error: Received message length insufficient");
                        break;
                    }

                    // 3. 解析MBAP頭部
                    ushort transactionId = (ushort)((bytes[0] << 8) | bytes[1]);
                    ushort protocolId = (ushort)((bytes[2] << 8) | bytes[3]);
                    ushort length = (ushort)((bytes[4] << 8) | bytes[5]);
                    byte unitId = bytes[6];
                    byte functionCode = bytes[7];

                    Console.WriteLine("=== MBAP頭部 ===");
                    Console.WriteLine($"事務標識符: 0x{transactionId:X4}");
                    Console.WriteLine($"協議標識符: 0x{protocolId:X4} (Modbus)");
                    Console.WriteLine($"長度字段: {length} 字節");
                    Console.WriteLine($"單元標識符: {unitId}");
                    Console.WriteLine($"功能碼: 0x{functionCode:X2}");

                    // 4. 檢查錯誤響應
                    if ((functionCode & 0x80) != 0)
                    {
                        byte errorCode = bytes[8];
                        WriteLog($"Error Response! Code: 0x{errorCode:X2}");
                        break;
                    }

                    // 5. 根據功能碼解析
                    switch (functionCode)
                    {
                        case 0x03: // 讀保持寄存器
                            {
                                if (bytes.Length < 9 + bytes[8])
                                {
                                    WriteLog("Error: Data length does not match declaration");
                                    break;
                                }

                                byte byteCount = bytes[8];
                                Console.WriteLine($"\n=== 寄存器數據 ===");
                                Console.WriteLine($"字節計數: {byteCount}");

                                // 解析寄存器值 (每2字節一個寄存器)
                                for (int i = 0; i < byteCount; i += 2)
                                {
                                    if (i + 9 + 1 >= bytes.Length) break;

                                    ushort registerValue = (ushort)((bytes[9 + i] << 8) | bytes[10 + i]);
                                    WriteLog($" Register{i / 2} value: 0x{registerValue:X4} ({registerValue}) {Convert.ToString(registerValue, 2).PadLeft(16, '0')}");
                                    /*
                                    // 示例中的 00-40 解析
                                    if (i == 0 && byteCount >= 2)
                                    {
                                        Console.WriteLine("\n詳細解析 00-40:");
                                        Console.WriteLine($"十進制: {registerValue}");
                                        Console.WriteLine($"十六進制: 0x{registerValue:X4}");
                                        Console.WriteLine($"二進制: {Convert.ToString(registerValue, 2).PadLeft(16, '0')}");

                                        // 00-40 實際值為 64
                                        Console.WriteLine($"實際值(十進制): {registerValue}");
                                    }
                                    */

                                    switch (m_eAddress_Cmd)
                                    {
                                        case enumAddress.Slave1_SpeedInformation:
                                            m_nSpeed[0] = registerValue;
                                            break;
                                        //case enumAddress.Slave1_OperationControl:
                                        //    m_nOperationCtrl[0] = registerValue;
                                        //    break;
                                        //case enumAddress.Slave1_OperatSpeed1:
                                        //    break;
                                        //case enumAddress.Slave1_OperatSpeed2:
                                        //    break;
                                        //case enumAddress.Slave1_OperatSpeed3:
                                        //    break;
                                        case enumAddress.Slave2_SpeedInformation:
                                            m_nSpeed[1] = registerValue;
                                            break;
                                        //case enumAddress.Slave2_OperationControl:
                                        //    m_nOperationCtrl[1] = registerValue;
                                        //    break;
                                        //case enumAddress.Slave2_OperatSpeed1:
                                        //    break;
                                        //case enumAddress.Slave2_OperatSpeed2:
                                        //    break;
                                        //case enumAddress.Slave2_OperatSpeed3:
                                        //    break;
                                        case enumAddress.Slave3_SpeedInformation:
                                            m_nSpeed[2] = registerValue;
                                            break;
                                        //case enumAddress.Slave3_OperationControl:
                                        //    m_nOperationCtrl[2] = registerValue;
                                        //    break;
                                        //case enumAddress.Slave3_OperatSpeed1:
                                        //    break;
                                        //case enumAddress.Slave3_OperatSpeed2:
                                        //    break;
                                        //case enumAddress.Slave3_OperatSpeed3:
                                        //    break;
                                        case enumAddress.Slave4_SpeedInformation:
                                            m_nSpeed[3] = registerValue;
                                            break;
                                        //case enumAddress.Slave4_OperationControl:
                                        //    m_nOperationCtrl[3] = registerValue;
                                        //    break;
                                        //case enumAddress.Slave4_OperatSpeed1:
                                        //    break;
                                        //case enumAddress.Slave4_OperatSpeed2:
                                        //    break;
                                        //case enumAddress.Slave4_OperatSpeed3:
                                        //    break;
                                        default:
                                            break;
                                    }

                                }
                            }
                            break;
                        case 0x06: // 寫保持寄存器
                            {
                                if (bytes.Length == 12 && bytes[7] == 0x06)
                                {
                                    WriteLog($" Successfully written to register {(ushort)((bytes[8] << 8) | bytes[9])} Value: {(ushort)((bytes[10] << 8) | bytes[11])}");
                                }
                            }
                            break;
                        default:
                            WriteLog($"Unimplemented function codes: 0x{functionCode:X2}");
                            break;
                    }


                    m_dicSignalAck[m_eAddress_Cmd].Set();

                    bSuc = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                WriteLog("Exception:" + ex);
            }
            return bSuc;
        }

        #region 繼承方法
        //X06 Preset Single registers.
        public override bool SetOperationCtrl(int nSlave, bool bOn)
        {
            throw new NotImplementedException();
        }
        public override bool SetSpeedSetting(int nSlave, int nSpeed)
        {
            bool bSuc = false;
            ushort nValue = (ushort)nSpeed;

            m_eAddress_Cmd = enumAddress.Slave1_OperatSpeed1 + (20 * (nSlave - 1));
            m_dicSignalAck[m_eAddress_Cmd].Reset();
            Sendcommand_Set(nSlave, (short)m_eAddress_Cmd, nValue);//4009 地址 8 操作狀態
            bSuc = m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);

            //m_eAddress_Cmd = enumAddress.Slave1_OperatSpeed2 + (100 * (nSlave - 1));
            //m_dicSignalAck[m_eAddress_Cmd].Reset();
            //Sendcommand_Set(nSlave, (short)m_eAddress_Cmd, nValue);//4009 地址 8 操作狀態
            //bSuc &= m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);

            //m_eAddress_Cmd = enumAddress.Slave1_OperatSpeed3 + (100 * (nSlave - 1));
            //m_dicSignalAck[m_eAddress_Cmd].Reset();
            //Sendcommand_Set(nSlave, (short)m_eAddress_Cmd, nValue);//4009 地址 8 操作狀態
            //bSuc &= m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);

            if (bSuc == false)
            {
                WriteLog("Send : Ack timeout");
            }
            return bSuc;
        }
        public override bool SetSpeedLimitMax(int nSlave, int nSpeed)
        {
            throw new NotImplementedException();
        }
        public override bool SetSpeedLimitMin(int nSlave, int nSpeed)
        {
            throw new NotImplementedException();
        }

        //X03 Reads the contents of holding registers.
        public override bool GetSettingInfo(int nSlave)
        {
            bool bSuc = false;
            //m_eAddress_Cmd = enumAddress.Slave1_OperationControl + (100 * (nSlave - 1));
            //m_dicSignalAck[m_eAddress_Cmd].Reset();

            //Sendcommand_Get(nSlave, (short)m_eAddress_Cmd, 1);//4009 地址 8

            //bSuc = m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);
            //if (bSuc == false)
            //{
            //    WriteLog("RECV : Ack timeout");
            //}

            return bSuc;
        }
        public override bool GetSpeedInformation(int nSlave, bool bPassLog = false)
        {
            bool bSuc = false;
            m_eAddress_Cmd = enumAddress.Slave1_SpeedInformation + (20 * (nSlave - 1));
            m_dicSignalAck[m_eAddress_Cmd].Reset();

            Sendcommand_Get(nSlave, (short)m_eAddress_Cmd, 1);//4006 地址 5

            bSuc = m_dicSignalAck[m_eAddress_Cmd].WaitOne(3000);
            if (bSuc == false)
            {
                WriteLog("RECV : Ack timeout");
            }

            return bSuc;
        }
        public override bool GetSpeedLimitMax(int nSlave)
        {
            //Sendcommand_Get(nSlave, 4, 1);
            return true;
        }
        public override bool GetSpeedLimitMin(int nSlave)
        {
            //Sendcommand_Get(nSlave, 4, 1);
            return true;
        }
        #endregion




    }

}
