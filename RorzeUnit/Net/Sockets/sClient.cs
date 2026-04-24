using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Windows;
using RorzeComm.Log;

namespace RorzeUnit.Net.Sockets
{
    public class sClient
    {
        Socket clientSocket;
        public event EventHandler<bool> OnConnectChange;
        public event SocketEventHandler OnAssgnSocket;
        public event EventHandler<string> EventHandlerLog;//外部註冊紀錄log

        public delegate void SocketEventHandler(object Sender, SocketEventArgs e);
        public class SocketEventArgs : EventArgs
        {
            public int _PortNo;
            public string _IP;
            public Socket _Socket;
            public SocketEventArgs(Socket Sockets, string IP, int PortNo)
            {
                _Socket = Sockets;
                _IP = IP;
                _PortNo = PortNo;
            }
        }


        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.          
            public const int BufferSize = 2048;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }

        private Thread _thStat;
        public bool Close = false;

        IAsyncResult Sockets;

        string m_strIP;
        int m_nPort;


        public sClient()
        {
            _thStat = new Thread(RunConnectStat);
            _thStat.IsBackground = true;

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _thStat.Start();
        }
        public void connect(string sIP, int nPort)
        {
            try
            {
                IPAddress IP = IPAddress.Parse(sIP);
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(IP, nPort);

                m_strIP = sIP;
                m_nPort = nPort;

                OnAssgnSocket?.Invoke(this, new SocketEventArgs(clientSocket, m_strIP, m_nPort));
                EventHandlerLog?.Invoke(this, string.Format("Client {0}:{1} connect", m_strIP, m_nPort));
                ReciveData(clientSocket);
            }
            catch (Exception ex)
            {

                EventHandlerLog?.Invoke(this, string.Format("Client {0}:{1} exception:{2}", m_strIP, m_nPort, ex));

                clientSocket.Close();
                //clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //SpinWait.SpinUntil(() => false, 1000);

                //str = string.Format("Client {0}:{1} reconnect", m_strIP, m_nPort);
                //MessageBox.Show(str, "Reconnect");
                //connect(m_strIP, m_nPort);// 連線失敗重新連線
            }
        }
        public void disconnect()
        {
            try
            {
                if (clientSocket.Connected)
                {
                    Close = true;
                    clientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void Write(String data)
        {
            try
            {
                if (clientSocket == null) return;
                if (clientSocket.Connected)
                {
                    byte[] byteData;
                    if (data.Contains('\r') == false)
                        byteData = Encoding.ASCII.GetBytes(data + '\r');
                    else
                        byteData = Encoding.ASCII.GetBytes(data);

                    clientSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void Write(byte[] data)
        {
            try
            {
                if (clientSocket == null) return;
                if (clientSocket.Connected)
                {
                    clientSocket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }




        public void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int bytesSent = client.EndSend(ar);

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        private void ReciveData(Socket socket)
        {
            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = socket;

            // Begin receiving the data from the remote device.
            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
        }
        protected void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Sockets = ar;
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // ======
                    byte[] tempData = new byte[bytesRead];
                    for (int i = 0; i < bytesRead; i++)
                        tempData[i] = state.buffer[i];
                    onDataReciveByByte?.Invoke(new ReciveByByte(tempData));

                    // ======
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    string strCommand = state.sb.ToString();
                    strCommand = strCommand.Replace(" ", "");
                    onDataRecive?.Invoke(new Recive(strCommand));

                    state.sb.Clear();
                    client.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    string response;
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.

                    EventHandlerLog?.Invoke(this, string.Format("Client {0}:{1} disconnect", m_strIP, m_nPort));

                    // 斷線重新連線
                    connect(m_strIP, m_nPort);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }




      

        public event ReciveEventHandler onDataRecive;
        public delegate void ReciveEventHandler(Recive args);
        public class Recive : EventArgs
        {
            public string sReciveData { get; set; }
            public Recive(string _sReciveData)
            {
                sReciveData = _sReciveData;
            }
        }

        public event ReciveByByteEventHandler onDataReciveByByte;
        public delegate void ReciveByByteEventHandler(ReciveByByte args);
        public class ReciveByByte : EventArgs
        {
            public byte[] sReciveData { get; set; }
            public ReciveByByte(byte[] _sReciveData)
            {
                sReciveData = _sReciveData;
            }
        }

        void RunConnectStat()
        {
            int nCount = 0;

            while (!Close)
            {
                SpinWait.SpinUntil(() => false, 1000);
                if (m_strIP == null) continue;
                try
                {
                    Ping ping = new Ping();
                    PingReply reply;
                    reply = ping.Send(m_strIP, 1000);

                    if (reply.Status == IPStatus.TimedOut)
                    {
                        nCount++;
                        if (nCount >= 5)//第二次機會
                        {
                            if (ConnectStat == true)
                            {
                                ConnectStat = false;
                                EventHandlerLog?.Invoke(this, string.Format("Client {0}:{1} disconnect!?", m_strIP, m_nPort));
                                clientSocket.Dispose();
                                clientSocket.Close();

                                // 斷線重新連線
                                //connect(m_strIP, m_nPort);
                            }
                        }
                    }
                    else if (reply.Status == IPStatus.DestinationHostUnreachable)
                    {

                    }
                    else if (reply.Status == IPStatus.Success)
                    {
                        nCount = 0;
                        if (ConnectStat == false)
                        {
                            if (clientSocket.Connected)
                            {
                                ConnectStat = true;
                            }
                            else
                            {
                                connect(m_strIP, m_nPort);
                            }
                        }
                    }

                }
                catch
                {
                    SpinWait.SpinUntil(() => false, 100);
                    continue;
                }

            }
        }

        private bool _bisConnect;
        public bool ConnectStat
        {
            get { return _bisConnect; }
            set { _bisConnect = value; if (OnConnectChange != null) OnConnectChange(this, _bisConnect); }
        }
    }
}
