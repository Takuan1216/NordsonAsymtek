using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Shapes;


namespace RorzeUnit.Class.ElectrostaticDetect
{
    public class Keyence_DL_RS1A
    {
        public bool IsConnect { get { return m_Comport.IsOpen; } }

        enum enumCommand
        {
            None,

            //從指定的感測放大器讀取
            SR,
            //從所有感測放大器統一讀取數據
            M0,
            //從所有感測放大器一齊讀取控制輸出狀態和數據
            MS,

            //向指定的感測放大器寫入
            SW,
            //向所有感測放大器統一寫入
            AW,
        }

        //通訊模組，可以讀取放大器上的數值
        SLogger m_Logger;
        SerialPort m_Comport;
        ConcurrentQueue<string> m_QueRecvBuffer = new ConcurrentQueue<string>();
        SPollingThread m_PollingDequeueRecv;
        SPollingThread m_PollingStatus;
        Dictionary<int, string> m_dicAmplifierValue = new Dictionary<int, string>();
        Dictionary<enumCommand, SSignal> m_SignalAck = new Dictionary<enumCommand, SSignal>();
        private bool m_bSimulate;
        private int m_nCom;
        private object m_lockRecv = new object();
        private object m_lockSend = new object();

        public Keyence_DL_RS1A(int nCom,bool bSimulate)
        {
            m_bSimulate = bSimulate;
            m_Comport = new SerialPort();
            m_nCom = nCom;

            foreach (enumCommand enumType in System.Enum.GetValues(typeof(enumCommand)))
            {
                m_SignalAck.Add(enumType, new SSignal(false, EventResetMode.ManualReset));
            }

            m_PollingDequeueRecv = new SPollingThread(10);
            m_PollingDequeueRecv.DoPolling += PollingDequeueRecv;

            m_PollingStatus = new SPollingThread(3000);
            m_PollingStatus.DoPolling += PollingStatus;
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[COM{0}] : {1}  at line {2} ({3})", m_nCom, strContent, lineNumber, meberName);
            m_Logger.WriteLog(strMsg);
        }

        // Step1 : 收到事件
        private void ReceivedEvent(object sender, SerialDataReceivedEventArgs e)
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
                //WriteLog("Recv:" + strContent);
                m_QueRecvBuffer.Enqueue(strContent);
            }
        }
        // Step2 : 解Queue
        private void PollingDequeueRecv()
        {
            try
            {
                SpinWait.SpinUntil(() => false, 1);
                if (IsConnect == false) return;
                string strContent;
                if (m_QueRecvBuffer.TryDequeue(out strContent) == false) return;
                ProcessingRecive(strContent);
            }
            catch (Exception ex)
            {
                WriteLog("Exception:" + ex);
            }
        }
        // Step3 : 解析
        private bool ProcessingRecive(string strContent)
        {
            bool bSuc = false;
            try
            {
                while (true)
                {
                    //2025 / 06 / 02 19:31:07.563[COM7] : Send: M0 at line 174(ComportSend)
                    //2025 / 06 / 02 19:31:09.690[COM7] : Recv: 4D302C2D30302E3030372C2B30302E3031380D0A at line 88(ReceivedEvent)

                    if (strContent == "") break;

                    string hexString = strContent;

                    byte[] bytes = new byte[hexString.Length / 2];

                    for (int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                    }

                    string asciiString = Encoding.ASCII.GetString(bytes);

                    WriteLog("Recv:" + asciiString);

                    string[] strArray = asciiString.Split(new char[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    enumCommand eCommand = enumCommand.None;
                    if (Enum.TryParse(strArray[0], out eCommand) == false)
                        break;

                    switch (eCommand)
                    {
                        case enumCommand.SR:

                            break;
                        case enumCommand.M0:
                            for (int i = 0; i < strArray.Length; i++)
                            {
                                m_dicAmplifierValue[i] = strArray[i];
                            }
                            break;
                        case enumCommand.MS:

                            break;
                        case enumCommand.SW:

                            break;
                        case enumCommand.AW:

                            break;
                        default:
                            break;
                    }
                    m_SignalAck[eCommand].Set();

                    bSuc = true;
                    break;
                }
            }
            catch (Exception ex) { WriteLog("Exception:" + ex); }
            return bSuc;
        }

        private void PollingStatus()
        {
            try
            {
                if (IsConnect == false) return;
                CmdAmplifierValueW();
                SpinWait.SpinUntil(() => false, 100);
                CmdAmplifierCtrlW();
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
        }

        private bool ComportSend(string strCmd, bool bPassLog = false)
        {
            bool bSucc = false;
            try
            {
                lock (m_lockSend)
                {
                    if (bPassLog == false) WriteLog("Send:" + strCmd);
                    //string strCmd = "SR,01,136\r\n"; // 包含 CR LF
                    byte[] byteArry = Encoding.ASCII.GetBytes(strCmd);
                    m_Comport.Write(byteArry, 0, byteArry.Length);
                    bSucc = true;
                }
            }
            catch (Exception ex) { WriteLog("<<SException>>" + ex); }
            return bSucc;
        }

        private bool SendCmd(enumCommand eCmd, string strCmd, bool bPassLog = false)
        {
            bool bSuc = false;
            while (true)
            {
                if (ComportSend(strCmd, bPassLog) == false)
                    break;
                if (m_SignalAck[eCmd].WaitOne(3000) == false)
                    break;
                if (m_SignalAck[eCmd].bAbnormalTerminal)
                    break;
                bSuc = true;
                break;
            }
            return bSuc;
        }

        //從所有感測放大器統一讀取數據
        private bool CmdAmplifierValueW()
        {
            bool bSuc = SendCmd(enumCommand.M0, "M0\r\n");
            return bSuc;
        }

        //從所有感測放大器一齊讀取控制輸出狀態和數據
        private bool CmdAmplifierCtrlW()
        {
            bool bSuc = SendCmd(enumCommand.MS, "MS\r\n");
            return bSuc;
        }

        // ===================================================================

        public bool StartCommunication()
        {
            bool bSucc = false;
            try
            {
                m_Logger = SLogger.GetLogger("DL_RS1A");
         
                m_Comport.PortName = "COM" + m_nCom.ToString();
                m_Comport.BaudRate = 9600;
                m_Comport.Parity = Parity.None;
                m_Comport.StopBits = StopBits.One;
                m_Comport.DataBits = 8;

                if (m_bSimulate == false)
                {
                    m_Comport.DataReceived += new SerialDataReceivedEventHandler(ReceivedEvent);
                    m_Comport.Open();
                    m_Comport.DiscardInBuffer();
                    m_Comport.DiscardOutBuffer();
                    bSucc = m_Comport.IsOpen;

                    if (bSucc)
                    {
                        m_PollingDequeueRecv.Set();
                        m_PollingStatus.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            return bSucc;
        }

        public string GetAmplifierValue(int nAddress)
        {
            string strValue = "0.00";
            if (m_dicAmplifierValue.ContainsKey(nAddress))
                strValue = m_dicAmplifierValue[nAddress];
            return strValue;
        }

    }
}
