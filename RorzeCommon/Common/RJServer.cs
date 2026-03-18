using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace RorzeComm.Log
{
    public class RJServer
    {
        public class SocketState
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 256;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }

        private int m_nPort = 11300;
        private string m_strHost = "127.0.0.1";
        private string m_strLogName = "";

        private Socket m_scServer = null;
        private bool m_bConnected = false;

        private List<string> m_LstBuffer = new List<string>();

        // private System.Windows.Forms.Timer m_tmr = new System.Windows.Forms.Timer();

        public ManualResetEvent m_mutWaitResponse = new ManualResetEvent(false);
        public ManualResetEvent m_mutConnect = new ManualResetEvent(false);

        public RJServer(string log_name)
        {
            m_strLogName = log_name;
            m_LstBuffer.Clear();
            //m_tmr.Enabled = false;
            //m_tmr.Interval = 1000;
            //m_tmr.Tick += new EventHandler(this.m_tmr_Tick);

            MakeConnect();
        }

        ~RJServer()
        {
            //m_tmr.Enabled = false;

            if (m_scServer != null)
            {
                if (m_scServer.Connected)
                {
                    m_scServer.Close();
                }
                m_scServer = null;
            }

        }

        private void m_tmr_Tick(object sender, EventArgs e)
        {
            m_bConnected = false;
            if (false == m_bConnected)
                MakeConnect();
            //m_tmr.Enabled = false;
        }

        private bool MakeConnect()
        {
            bool bSucc = false;
            try
            {
                m_scServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(m_strHost), m_nPort);
                m_scServer.BeginConnect(remoteEP, new AsyncCallback(OnConnect), m_scServer);
                if (m_mutConnect.WaitOne(1000))
                {
                    bSucc = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
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

                    //mySocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReadCallback), state);
                    mySocket.EndConnect(ar);

                    SendTitle();
                    ResendBuffer();

                    m_mutConnect.Set();
                    m_bConnected = true;
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }

        }

        private void SendTitle()
        {
            string strTitle = "#LOGIN#," + m_strLogName + "\r";
            byte[] myByte = Encoding.ASCII.GetBytes(strTitle);
            m_scServer.BeginSend(myByte, 0, myByte.Length, 0, new AsyncCallback(SendCallback), m_scServer);
        }

        private void ResendBuffer()
        {
            while (m_LstBuffer.Count > 0)
            {
                SendData(m_LstBuffer[0]);
                m_LstBuffer.RemoveAt(0);
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
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    //Console.WriteLine(state.sb.ToString());

                    //ProcessReceive(state.sb.ToString());
                    state.sb.Clear();
                    //mySocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReadCallback), state);
                }
                else
                {
                    // Do something else
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        public void SendData(string strMsg)
        {
            int nLen = strMsg.Length;
            if (strMsg[nLen - 1] != '\r')
                strMsg += '\r';

            if (m_scServer == null || false == m_scServer.Connected)
            {
                m_LstBuffer.Add(strMsg);

                if (m_LstBuffer.Count >= 100)
                    m_LstBuffer.RemoveAt(0);
            }
            else
            {
                byte[] myByte = Encoding.ASCII.GetBytes(strMsg);
                m_scServer.BeginSend(myByte, 0, myByte.Length, 0, new AsyncCallback(SendCallback), m_scServer);
            }
        }

        public bool IsConnect { get { return m_bConnected; } }
    }
}
