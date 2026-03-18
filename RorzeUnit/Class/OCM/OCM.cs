using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using RorzeComm.Log;
using RorzeComm.Threading;

namespace RorzeUnit.Class.CIPC
{
    public abstract class OCM
    {
        protected enum enumRTUfnc
        {
            //ReadCoilStatus = 0x01,
            //ReadInputStatus = 0x02,
            ReadHoldingRegisters = 0x03,
            //ReadInputRegisters = 0x04,
            //ForceSingleCoil = 0x05,
            //PresetSingleRegister = 0x06,
        }
        protected enum enumCheckSumType { CRC16, Lowerbyte }

        protected int m_nBody = 1;
        protected int m_nCom = 0;
        protected int m_nStationMax = 1;//站數
        protected int m_nStation = 0;
        protected bool m_bSimulate;
        protected const int ThreadPollingTime = 2000;

        //polling要一直問當前的氧濃度
        protected Thread m_threadPolling;
        protected Thread m_threadReceived;
        protected SLogger _logger = SLogger.GetLogger("OCM");
        protected SerialPort m_Comport;
        private object m_lockSend = new object();
        private object m_lockRecv = new object();
        protected double[] m_dOxygen;//用量存氧濃度結果
        private ConcurrentQueue<string> m_queReceived = new ConcurrentQueue<string>();

        public bool IsComOpen { get { return (m_Comport != null && m_Comport.IsOpen); } }
        public bool Disable { get { return m_nCom == 0; } }
        public OCM(int nCom, int nBody, int nStationMax, bool bSimulate = false)
        {
            m_nCom = nCom;
            m_bSimulate = bSimulate;
            m_nBody = nBody;
            m_nStationMax = nStationMax;

            m_dOxygen = new double[nStationMax];//用量存氧濃度結果

            if (m_bSimulate)
            {
                m_dOxygen[0] = 20.0;
            }
            else
            {
                try
                {
                    if (m_nCom > 0)
                    {
                        string strCom = "COM" + m_nCom.ToString();
                        m_Comport = new SerialPort(strCom, 115200, Parity.None, 8, StopBits.One);
                        m_Comport.ReadTimeout = 1000;
                        m_Comport.WriteTimeout = 1000;
                        m_Comport.Open();
                        //連線成功即訂閱序列傳輸接收資料之事件
                        m_Comport.DataReceived += new SerialDataReceivedEventHandler(OnReceivedEvent);

                        m_threadReceived = new Thread(ThreadReceived);
                        m_threadReceived.IsBackground = true;
                        m_threadReceived.Start();

                        m_threadPolling = new Thread(ThreadPolling);
                        m_threadPolling.IsBackground = true;
                        m_threadPolling.Start();
                    }
                }
                catch
                {

                }
            }


        }
        ~OCM() { if (IsComOpen) m_Comport.Close(); }
        /// <summary>
        /// 紀錄LOG
        /// </summary>
        /// <param name="strContent"></param>
        /// <param name="meberName"></param>
        /// <param name="lineNumber"></param>
        protected void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }
        /// <summary>
        /// 收到內容塞入Queue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceivedEvent(object sender, SerialDataReceivedEventArgs e)
        {
            int nByteToRead = (sender as SerialPort).BytesToRead;
            lock (m_lockRecv)
            {
                StringBuilder sb = new StringBuilder();
                int nIdx = 0;
                while (nIdx < nByteToRead)
                {
                    int nHex = m_Comport.ReadByte();
                    sb.Append(nHex.ToString("X2"));
                    nIdx++;
                }
                string strContent = sb.ToString();
                WriteLog("Recv:" + strContent);
                m_queReceived.Enqueue(strContent);
            }
        }
        /// <summary>
        /// 獨立執行序處理收到內容
        /// </summary>
        private void ThreadReceived()
        {
            while (true)
            {
                SpinWait.SpinUntil(() => false, 1);
                if (IsComOpen == false) continue;
                string strContent;
                if (m_queReceived.TryDequeue(out strContent) == false) continue;
                ProcessingRecive(strContent);
            }
        }
        /// <summary>
        /// 子類別撰寫:收到信息底層要解析
        /// </summary>
        /// <param name="strContent"></param>
        /// <returns></returns>
        protected abstract bool ProcessingRecive(string strContent);
        /// <summary>
        /// 子類別撰寫:需要輪循的東西
        /// </summary>
        /// <returns></returns>
        protected abstract void ThreadPolling();
        /// <summary>
        /// 問氧濃度
        /// </summary>
        /// <param name="nStation"></param>
        /// <returns></returns>
        public string GetReply(int nStation)
        {
            return m_dOxygen[nStation - 1].ToString();
        }


        #region Mobus Send

        /// <summary>
        /// Comport send
        /// </summary>
        /// <param name="strCmd"></param>
        /// <returns></returns>
        protected bool ComportSend(string strCmd, bool bPassLog = false)
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
                    m_Comport.Write(byteArry, 0, byteArry.Length);
                    bSucc = true;
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            return bSucc;
        }
        /// <summary>
        /// 取得checksum
        /// </summary>
        /// <param name="strInput">使用空白來間格輸入 ex: 00 01 02 03</param>
        /// <param name="type">checksumType</param>
        /// <returns></returns>
        private static string getChecksum(string strInput, enumCheckSumType eType)
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
        private static byte[] convertHexStringToByteArray(string strInput)
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

        #endregion
    }

    // CPIP NPort_5430
    public sealed class OCM_CPIPNPort5430 : OCM
    {
        private string mStatus = "";
        private double mO2SensorCapacity = 0;
        private const int WriteByteNum = 8;
        private const int ResponseByteNum = 9;
        public OCM_CPIPNPort5430(int nCom, int nBody, int nStationMax, bool bSimulate)
            : base(nCom, nBody, nStationMax, bSimulate)
        {

        }
        /// <summary>
        /// 一站一站問氧濃度
        /// </summary>
        protected override void ThreadPolling()
        {
            while (m_Comport != null && m_Comport.IsOpen)
            {
                for (int n = 0; n < m_nStationMax; n++)
                {
                    m_nStation = n;

                    SendCommand((n + 1).ToString());

                    SpinWait.SpinUntil(() => false, ThreadPollingTime);
                }
            }
        }
        /// <summary>
        /// 寫死的發命令問氧濃度
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="strOffset"></param>
        /// <param name="strLen"></param>
        /// <param name="strData"></param>
        /// <returns></returns>
        private bool SendCommand(string cmd, string strOffset = null, string strLen = null, string strData = null)
        {
            bool bSucc = false;


            // if (m_Comport.IsOpen)
            //  {
            //Clear in/out buffers:
            m_Comport.DiscardOutBuffer();
            m_Comport.DiscardInBuffer();
            byte[] message = new byte[WriteByteNum];
            switch (cmd)
            {
                //參照通用協議，讀取O2 Sensor 01數值主機發送：01 04 00 02 00 02 D0 0B
                case "1":
                    message[0] = 1;
                    message[1] = 4;
                    message[2] = 0;
                    message[3] = 2;
                    message[4] = 0;
                    message[5] = 2;
                    message[6] = 208;
                    message[7] = 11;
                    break;
                //參照通用協議，讀取O2 Sensor 02數值主機發送：02 04 00 02 00 02 D0 3B
                case "2":
                    message[0] = 2;
                    message[1] = 4;
                    message[2] = 0;
                    message[3] = 2;
                    message[4] = 0;
                    message[5] = 2;
                    message[6] = 208;
                    message[7] = 56;
                    break;
                //參照通用協議，讀取O2 Sensor 03數值主機發送：03 04 00 02 00 02 D1 E9
                case "3":
                    message[0] = 3;
                    message[1] = 4;
                    message[2] = 0;
                    message[3] = 2;
                    message[4] = 0;
                    message[5] = 2;
                    message[6] = 209;
                    message[7] = 233;
                    break;
                //參照通用協議，讀取O2 Sensor 04數值主機發送：04 04 00 02 00 02 D0 5E
                case "4":
                    message[0] = 4;
                    message[1] = 4;
                    message[2] = 0;
                    message[3] = 2;
                    message[4] = 0;
                    message[5] = 2;
                    message[6] = 208;
                    message[7] = 94;
                    break;
                //參照通用協議，讀取O2 Sensor 05數值主機發送：05 04 00 02 00 02 D1 8F
                case "5":
                    message[0] = 5;
                    message[1] = 4;
                    message[2] = 0;
                    message[3] = 2;
                    message[4] = 0;
                    message[5] = 2;
                    message[6] = 209;
                    message[7] = 143;
                    break;
            }

            try
            {
                m_Comport.Write(message, 0, message.Length);

                //m_strReply = m_Comport.ReadLine();
                //if (m_strReply.Length > 0)
                //{
                //    if (m_strReply[0] == '~')
                //    {
                //        bSucc = true;
                //    }
                //}
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("[Com{0}] Exception : {1}", m_nCom, ex));
            }

            if (m_bSimulate)
                bSucc = true;

            return bSucc;
        }
        /// <summary>
        /// 處理收到的內容要解析氧濃度
        /// </summary>
        /// <param name="strContent"></param>
        /// <returns></returns>
        protected override bool ProcessingRecive(string strContent)
        {
            //List<byte> dataBuffer = new List<byte>();
            //int bytesNum = m_Comport.BytesToRead;
            //byte[] dataMatrix;
            //int dataLength;

            //if (bytesNum == ResponseByteNum)
            //{
            //    try
            //    {
            //        if (m_Comport.IsOpen)
            //        {
            //            for (int i = 0; i < bytesNum; i++)
            //            {
            //                dataBuffer.Add((byte)m_Comport.ReadByte());
            //            }
            //            dataLength = dataBuffer.Count();
            //            dataMatrix = new byte[dataLength];
            //            dataMatrix = dataBuffer.ToArray();
            //            DataDecoder(dataMatrix, dataLength);
            //            dataBuffer.Clear();
            //        }
            //    }
            //    catch (Exception error)
            //    {
            //        mStatus = error.ToString();
            //    }
            //}
            return true;
        }
        private void DataDecoder(byte[] rawData, int length)
        {
            int station = 1;
            //監測ResponseByteNum長度
            if (length == ResponseByteNum)
            {
                station = Convert.ToInt16(rawData[0]);
                //計算通訊得到的電壓值
                byte[] rawVolt = new byte[4];
                rawVolt[0] = rawData[3];
                rawVolt[1] = rawData[4];
                rawVolt[2] = rawData[5];
                rawVolt[3] = rawData[6];

                string source = BitConverter.ToString(rawVolt).Replace("-", string.Empty);
                var number = int.Parse(source, System.Globalization.NumberStyles.AllowHexSpecifier);
                byte[] numberArray = BitConverter.GetBytes(number);
                float actual = BitConverter.ToSingle(numberArray, 0);
                mO2SensorCapacity = Convert.ToDouble(actual);
                //m_strReplyList[m_nStation] = Math.Round((mO2SensorCapacity / 10000), 2).ToString("f1");//小數點一位
                m_dOxygen[m_nStation] = Math.Round((mO2SensorCapacity / 10000), 2);//小數點一位

            }
        }

    }



    public sealed class OCM_MicroProgram : OCM
    {

        private enum enumAddress
        {
            Oxygen = 0x00,//氧濃度          
        }
        Dictionary<enumAddress, SSignal> _signalAck = new Dictionary<enumAddress, SSignal>();
        private enumAddress m_eAddress_Cmd;
        public OCM_MicroProgram(int nCom, int nBody, int nStationMax, bool bSimulate = false) :
            base(nCom, nBody, nStationMax, bSimulate)
        {
            foreach (enumAddress enumType in System.Enum.GetValues(typeof(enumAddress)))
            {
                _signalAck.Add(enumType, new SSignal(false, EventResetMode.ManualReset));
            }
        }

        /// <summary>
        /// 處理收到的內容要解析氧濃度
        /// </summary>
        /// <param name="strContent"></param>
        /// <returns></returns>
        protected override bool ProcessingRecive(string strContent)
        {
            bool bSuc = false;

            while (strContent != "")
            {

                //string[] strMsg = strContent.Split('-');//收到的byte array 轉換時會自動用-來連結 ex:00-01-FF

                string[] strMsg = new string[strContent.Length / 2];

                for (int i = 0; i < strContent.Length / 2; i++)
                {
                    strMsg[i] = strContent.Substring(i * 2, 2);
                }

                int nSlaveAddress = 0;
                if (int.TryParse(strMsg[0], out nSlaveAddress) == false)
                    break;
                int nFunction = 0;
                if (int.TryParse(strMsg[1], out nFunction) == false)
                    break;
                int nDataCount = 0;
                if (int.TryParse(strMsg[2], out nDataCount) == false)
                    break;

                string[] strArryDat = new string[nDataCount / 2];

                for (int i = 0; i < nDataCount / 2; i++)
                {
                    strArryDat[i] = strMsg[3 + 2 * i] + strMsg[3 + 2 * i + 1];
                }

                //bool bSend = strMsg[strMsg.Length - 1] == "01";//底層會增加 最後一碼為是否傳送給Server

                //if (nFunction == 0x83 || nFunction == 0x84)//fnc x03/x04失敗回傳
                //    break;

                //if (nFunction == 0x86)//fnc x06失敗回傳
                //    break;

                if (nFunction == 3)
                {

                    switch (m_eAddress_Cmd)
                    {
                        case enumAddress.Oxygen:
                            int value1 = Convert.ToInt32(strArryDat[0].Substring(0, 2), 16);

                            int value2 = Convert.ToInt32(strArryDat[0].Substring(2, 2), 16);

                            string str = string.Format("{0}.{1}", value1, value2);

                            m_dOxygen[nSlaveAddress - 1] = double.Parse(str);
                            break;
                        default:
                            break;
                    }
                }
                _signalAck[m_eAddress_Cmd].Set();

                bSuc = true;
                break;
            }
            return bSuc;
        }
        /// <summary>
        /// 一站一站問氧濃度
        /// </summary>
        protected override void ThreadPolling()
        {
            while (m_Comport != null && m_Comport.IsOpen)
            {
                SpinWait.SpinUntil(() => false, 10);
                for (int i = 0; i < m_nStationMax; i++)//根據站號依序詢問
                {
                    m_nStation = i;
                    GetOxygen(i + 1);
                    SpinWait.SpinUntil(() => false, ThreadPollingTime);
                }
            }
        }

        public bool SendCommand(string cmd, string strOffset = null, string strLen = null, string strData = null)
        {
            bool bSucc = false;


            //m_Comport.DiscardOutBuffer();
            //m_Comport.DiscardInBuffer();
            //byte[] message = new byte[WriteByteNum];
            //switch (cmd)
            //{
            //    //參照通用協議，讀取O2 Sensor 01數值主機發送：01 03 00 00 00 01 D0 0B
            //    case "1":
            //        message[0] = 1;
            //        message[1] = 4;
            //        message[2] = 0;
            //        message[3] = 2;
            //        message[4] = 0;
            //        message[5] = 2;
            //        message[6] = 208;
            //        message[7] = 11;
            //        break;
            //    //參照通用協議，讀取O2 Sensor 02數值主機發送：02 04 00 02 00 02 D0 3B
            //    case "2":
            //        message[0] = 2;
            //        message[1] = 4;
            //        message[2] = 0;
            //        message[3] = 2;
            //        message[4] = 0;
            //        message[5] = 2;
            //        message[6] = 208;
            //        message[7] = 56;
            //        break;
            //}

            //try
            //{
            //    m_Comport.Write(message, 0, message.Length);
            //    bSucc = true;
            //}
            //catch (Exception ex)
            //{
            //    WriteLog(string.Format("[Com{0}] Exception : {1}", m_nCom, ex));
            //}

            return bSucc || m_bSimulate;
        }

        //X03
        private bool GetOxygen(int nFFU_address, bool bPassLog = false)
        {
            //example 01 - 03 - 00 - 00 - 00 - 01 - 84 - 0A
            string strSlaveAddress = string.Format("{0:X2}", nFFU_address);
            string strFunction = string.Format("{0:X2}", (int)enumRTUfnc.ReadHoldingRegisters);
            string strStartAddress = string.Format("{0:X4}", (int)enumAddress.Oxygen);
            string strData = string.Format("{0:X4}", 1);
            string strCmd = string.Format("{0} {1} {2} {3}", strSlaveAddress, strFunction, strStartAddress, strData);

            bool bSuc = SendCmd(enumAddress.Oxygen, strCmd, bPassLog);
            return bSuc;
        }
        private bool SendCmd(enumAddress eAddress, string strCmd, bool bPassLog = false)
        {
            bool bSuc = false;
            _signalAck[eAddress].Reset();
            while (true)
            {
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


    }
}

