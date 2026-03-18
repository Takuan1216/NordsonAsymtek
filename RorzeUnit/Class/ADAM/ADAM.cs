using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Advantech.Adam;
using Advantech.Common;
using RorzeComm.Log;

namespace RorzeUnit.Class.ADAM
{
    public abstract class ADAM
    {
        //開啟讀取PXV資料之執行緒
        protected Thread mReadCipcThread;
        protected bool mReadCipcSwitch = false;

        protected SLogger _logger = SLogger.GetLogger("OCM");
        protected void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        protected int m_nCom = 0;
        protected int m_iAddr;
        protected int m_iCount;

        protected Adam4000Config m_adamConfig;

        ~ADAM()
        {
            if (adamCom != null && adamCom.IsOpen)
                adamCom.CloseComPort();
        }
        protected bool m_bSimulate;

        protected AdamCom adamCom;
        //protected SerialPort m_Comport;
        protected bool m_bConnect;
        protected const int m_nDevice = 1;
        protected string m_strCommand;
        protected string m_strReply;

        public string mStatus = "";
        protected double mO2SensorCapacity = 0;
        protected const int ThreadSleepTime = 2000;
        protected const int WriteByteNum = 8;
        protected const int ResponseByteNum = 9;

        protected void InitCommand()
        {
            m_strReply = "";
        }
        public bool IsConnect()
        {
            return m_bConnect;
        }
        public string GetCommand()
        {
            return m_strCommand;
        }

        public abstract bool CheckReader();
        public abstract bool Read(out float[] response);
        public abstract string GetReply();

        protected void Wait(int ms)
        {
            DateTime dtStart = DateTime.Now;

            while (true)
            {
                TimeSpan ts = DateTime.Now - dtStart;
                if (ts.TotalMilliseconds >= ms)
                {
                    return;
                }

                Thread.Sleep(1);
            }
        }
    }

    // CPIP NPort_5430
    public sealed class ADAM_4017 : ADAM
    {
        public ADAM_4017(int nCom, bool bSimulate = false)
        {
            m_bSimulate = bSimulate;

            m_iAddr = 1;   // the slave address is 1
            m_iCount = 0; // the counting start from 0

            string strCom = "COM" + nCom.ToString();
            if (!bSimulate)
            {
                try
                {
                    //m_Comport = new SerialPort(strCom, 9600, Parity.None, 8, StopBits.One);
                    adamCom = new AdamCom(nCom);

                    adamCom.Checksum = false;
                    if (adamCom.OpenComPort())
                    {
                        // set COM port state,9600,N,8,1
                        adamCom.SetComPortState(Baudrate.Baud_9600,
                        Databits.Eight, Advantech.Common.Parity.None, Stopbits.One);
                        // set COM port timeout
                        adamCom.SetComPortTimeout(500, 500, 0, 500, 0);
                        m_iCount = 0; // reset thereading counter
                        //              // get module config
                        //if (!m_Comport.Configuration(m_iAddr).GetModuleConfig(out
                        //m_adamConfig))
                        //{
                        //    m_Comport.CloseComPort();

                        //    return;
                        //}

                        adamCom.Configuration(m_iAddr).GetModuleConfig(out m_adamConfig);

                        //連線成功即訂閱序列傳輸接收資料之事件
                        //adamCom.DataReceived += new SerialDataReceivedEventHandler(OnReceivedData);
                    }
                }
                catch
                {
                    m_bConnect = false;
                }
            }

            m_nCom = nCom;

            //連線成功即開啟讀取資料執行緒
            //mReadCipcThread = new Thread(ReadCipcData);
            if (m_bConnect)
                mReadCipcSwitch = true;
            //mReadCipcThread.Start();


            //float[] response = new float[8];
            //Read(out response);
        }

        private void ReadCipcData()
        {
           /// while (mReadCipcSwitch)
           // {
                SendCommand("$AAF");
             //   Thread.Sleep(ThreadSleepTime);
               // SendCommand("2");
                //Thread.Sleep(ThreadSleepTime);
           // }
        }

        public bool SendCommand(string cmd, string strOffset = null, string strLen = null, string strData = null)
        {
            bool bSucc = false;
            InitCommand();

            // if (m_Comport.IsOpen)
            //  {
            //Clear in/out buffers:
 
            //byte[] message = new byte[WriteByteNum];
            byte[] message = Encoding.Unicode.GetBytes(cmd);
            try
            {
               // m_Comport.Write(cmd);

         

                //m_strReply = m_Comport.ReadLine();
                //if (m_strReply.Length > 0)
                //{
                //    if (m_strReply[0] == '~')
                //   {
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


        public override bool Read(out float[] response)
        {

            response = new float[8];

            Adam4000_ChannelStatus[] status;

            ushort[] response2 = new ushort[8];


            m_iAddr = 00029;
            adamCom.AnalogOutput(m_iAddr).GetValues(1, 8, out response2);

            float[] values; //顯示屏幕值

            adamCom.Send("$AA2");

            if (adamCom.AnalogInput(m_iAddr).GetValues(8, out values, out status))
            //取得各屏幕數值與狀態
            {
                //取得數值存入指定集合中
                //_d = new data();
                //_d.AI0 = values[0];
                //_d.AI1 = values[1];
                //_d.AI2 = values[2];
                //_d.AI_TIME = m_iCount;
                //_DATA.Add(_d);
                //drawLine(m_iCount);//每秒重繪折線圖
                string a = values[0].ToString();
                string a1 = values[1].ToString();
                string a2 = values[2].ToString();
            }
            return adamCom.AnalogInput(m_iAddr).GetValues(8, out response, out status);
        }

        public override bool CheckReader()
        {
            bool bSucc = SendCommand("RU");
            if (false == bSucc)
            {
                WriteLog(string.Format("[Com{0}] Unision RFID Send 'RU' to RFIDReader Failed", m_nCom));
                return false;
            }

            string strReply = m_strReply;
            bool bOkHead = strReply.IndexOf("RUR") >= 0 ? true : false;
            bool bOkEnd = strReply[strReply.Length - 1] == '*';
            if (false == bOkHead || false == bOkEnd)
            {
                WriteLog(string.Format("[Com{0}] Unision RFID Receive 'RUR' Reply From RFIDReader Failed.", m_nCom));
                return false;
            }
            WriteLog(string.Format("[Com{0}] Unision RFID Reader is connected!!", m_nCom));
            WriteLog(string.Format("[Com{0}] {1}", m_nCom, m_strCommand));
            WriteLog(string.Format("[Com{0}] {1}", m_nCom, m_strReply));

            return true;
        }

        public override string GetReply()
        {
            return m_strReply;
        }

        #region Received Data
        private void OnReceivedData(object sender, SerialDataReceivedEventArgs e)
        {
            List<byte> dataBuffer = new List<byte>();
   
            byte[] dataMatrix;
            int dataLength;

    

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
        }
        #endregion

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
                m_strReply = mO2SensorCapacity.ToString();
               // if (mO2SensorCapacity > 0)
               //      DataUpdatedEvent("O2SensorCapacity", mO2SensorCapacity, station);
            }
        }
    }
}
    
