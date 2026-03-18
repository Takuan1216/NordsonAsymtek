using System;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using RorzeComm.Log;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using RorzeUnit.Interface;

namespace RorzeUnit.Class.BarCode
{
    public class DM370 : I_BarCode
    {
        public class SocketState
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;//256   26738
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }

        public ManualResetEvent m_mutWaitResponse = new ManualResetEvent(false);
        public static object m_lockCmd = new object();
        private string m_strReceive = "";
        private string m_strIP = "127.0.0.1";
        private Socket m_scBR = null;
        private int m_nBRPort = 23;

        private SLogger _logger = SLogger.GetLogger("CommunicationLog");

        public bool Simulate { get; private set; }
        public bool Connected { get { return Simulate ? true : m_scBR == null ? false : m_scBR.Connected; } }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }


        public DM370(string strIP, int nBodyNo, bool bDisable, bool bSimulate)
        {
            Disable = bDisable;
            Simulate = bSimulate;
            BodyNo = nBodyNo;
            m_strIP = strIP;           
        }
        ~DM370()
        {
            if (m_scBR != null)
            {
                if (m_scBR.Connected)
                {
                    m_scBR.Close();
                }
                m_scBR = null;
            }

        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0} at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(string.Format("[Barcode{0}]{1}", BodyNo, strMsg));
        }
        private bool MakeConnect(ref Socket sc, string strIP, int nPort)
        {
            bool bSucc = false;
            try
            {
                sc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(strIP), nPort);
                sc.BeginConnect(remoteEP, new AsyncCallback(OnConnect), sc);
                bSucc = true;
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
            return bSucc;
        }
        private void OnConnect(IAsyncResult ar)
        {
            Socket mySocket = (Socket)ar.AsyncState;

            try
            {
                if (mySocket.Connected)
                {
                    SocketState state = new SocketState();
                    state.workSocket = mySocket;

                    mySocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReadCallback), state);
                    mySocket.EndConnect(ar);
                    WriteLog("BarcodeReader connected!!");
                }
                else
                {
                    WriteLog("Unable to connect to BarcodeScanner , Retry");
                    MakeConnect(ref m_scBR, m_strIP, m_nBRPort);
                }
            }
            catch (Exception ex) 
            {
                WriteLog(ex.ToString()); 
            }
        }
        private void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            try
            {
                SocketState state = (SocketState)ar.AsyncState;
                Socket mySocket = state.workSocket;

                // Read data from the client socket. 
                int bytesRead = mySocket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    string strProcess = state.sb.ToString();
                    state.sb.Clear();
                    mySocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReadCallback), state);

                    ProcessReceive(strProcess);
                }
                else
                {
                    // Do something else
                }
            }
            catch (Exception ex) { WriteLog(ex.ToString()); }
        }
        private void ProcessReceive(string strData)
        {
            if (strData.Length == 0)
                return;
            WriteLog("Recv " + strData);
            m_strReceive = strData;
            m_mutWaitResponse.Set();
        }
        private void SendData(string strMsg)
        {
            m_mutWaitResponse.Reset();
            WriteLog("Send " + strMsg);
            if (m_scBR != null && m_scBR.Connected)
            {
                byte[] myByte = Encoding.ASCII.GetBytes(strMsg);
                m_scBR.BeginSend(myByte, 0, myByte.Length, 0, new AsyncCallback(SendCallback), m_scBR);
                bool bSucc = m_mutWaitResponse.WaitOne(3000);
                if (false == bSucc)
                {
                    m_strReceive = "Fail";
                    WriteLog("send command failure!");
                }
            }
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception ex) { WriteLog(ex.ToString()); }
        }


        public void Open()
        {
            if (false == Simulate && false == Disable)
            {
                MakeConnect(ref m_scBR, m_strIP, m_nBRPort);
                SpinWait.SpinUntil(() => false, 100);//不能拿掉
            }
        }


        public string Read()
        {
            if (Simulate) { return "test"; }

            SendData("+");

            m_strReceive = m_strReceive.Replace("\r\n", "");//host不需要

            return m_strReceive;
        }
        public void ReadTest()
        {
            Read();
        }
        public string GetReply()
        {
            return m_strReceive;
        }
        public void Quit()
        {

        }

    }
}
