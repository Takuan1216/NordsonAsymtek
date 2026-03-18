using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeUnit.Class.BarCode
{
    public class BarcodeReder : I_BarCode
    {

        private SLogger m_Logger = SLogger.GetLogger("CommunicationLog");


        private object m_lockRecv = new object();

        SSignal m_SignalAck = new SSignal(false, EventResetMode.ManualReset);

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[COM{0}] : {1}  at line {2} ({3})", m_nCom, strContent, lineNumber, meberName);
            m_Logger.WriteLog(strMsg);
        }

        ~BarcodeReder()
        {
            if (m_Comport != null && m_Comport.IsOpen)
                m_Comport.Close();
        }

        private SerialPort m_Comport;
        private int m_nCom;
        private string m_strReply = "";
        private bool m_bConnect = false;

        public bool Connected { get { return m_bConnect; } }
        public bool Disable { get; }


        public BarcodeReder(int nCom)
        {
            m_nCom = nCom;
            string strCom = "COM" + nCom.ToString();
            m_Comport = new SerialPort(strCom, 115200, Parity.Even, 8, StopBits.One);
            Open();
        }

        private void Comport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int nByteToRead = (sender as SerialPort).BytesToRead;
            lock (m_lockRecv)
            {
                if (nByteToRead > 0)
                {
                    byte[] buff = new byte[nByteToRead];
                    m_Comport.Read(buff, 0, nByteToRead);
                    string receivedata = Encoding.Default.GetString(buff);
                    m_strReply = receivedata;
                    WriteLog("Recv : " + receivedata);
                    m_SignalAck.Set();
                }
            }
        }

        public void Open()
        {
            try
            {
                if (false == m_Comport.IsOpen)
                {
                    m_Comport.Open();
                }

                m_bConnect = m_Comport.IsOpen;

                if (m_bConnect)
                {
                    m_Comport.DataReceived -= Comport_DataReceived;
                    m_Comport.DataReceived += Comport_DataReceived;
                }
            }
            catch
            {
                m_bConnect = false;
            }
        }

        public string Read()
        {
            m_strReply = "";
         
            if (false == SendCommand("LON\r"))
            {
                SendCommand("LOFF\r");
            }
            
            return m_strReply;
        }

        public void ReadTest()
        {
            SendCommand("TEST1\r");
        }

        public void Quit()
        {
            SendCommand("QUIT\r");
        }

        public string GetReply()
        {
            return m_strReply;
        }

        public bool SendCommand(string strCmd)
        {
            bool bSucc = false;
            try
            {
                m_SignalAck.Reset();

                WriteLog(strCmd);
                m_Comport.Write(strCmd);
                bSucc = m_SignalAck.WaitOne(3000);
            }
            catch (Exception ex)
            {
                WriteLog("<Exception> :" + ex);
            }
            return bSucc;
        }


    }
}
