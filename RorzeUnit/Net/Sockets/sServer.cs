using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RorzeUnit.Net.Sockets
{
    public class sServer
    {
        //protected List<Socket> _lstSockets = new List<Socket>();
        private Dictionary<string, Socket> _dicSock = new Dictionary<string, Socket>();//裝所有Socket的容器



        public event SocketEventHandler OnAssgnSocket;

        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public class MessageEventArgs : EventArgs
        {
            public MessageEventArgs(string strMessage)
            {
                Message = strMessage;
            }
            public string Message { get; set; }
        }


        protected List<Thread> _pollings;

        public string localAddr;
        public int port;
        public string strLogName;
        string ServerName = "RorzeSystem";
        public sServer(string localAddr, int port)//(string localAddr, int port, string strLogName)
        {

            _dicSock.Add(ServerName, new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));

            _dicSock[ServerName].Bind(new IPEndPoint(IPAddress.Parse(localAddr), port));

            _pollings = new List<Thread>();


            _pollings.Add(new Thread(RunClientAccept));
            _dicSock[ServerName].Listen(10);

        }

        public void ServerStart()//開始連線
        {
            _pollings[0].Start();
        }

        public void RunClientAccept()//等待client接受
        {
            while (true)
            {
                SpinWait.SpinUntil(() => false, 10);

                Socket socket = _dicSock[ServerName].Accept();

                string RemoteportID = socket.RemoteEndPoint.ToString().Split(':')[1];
                string RemoteIP = socket.RemoteEndPoint.ToString().Split(':')[0];

                if (OnAssgnSocket != null)
                    OnAssgnSocket(this, new SocketEventArgs(socket, RemoteIP, int.Parse(RemoteportID)));
            }
        }


        public bool AddClintList(string Unit, Socket Sockets)//Client判斷socket後要加入字典裡面
        {
            if (_dicSock.Keys.Contains(Unit))
                _dicSock.Remove(Unit);

            _dicSock.Add(Unit, Sockets);
            return true;
        }

        public void RemoveClint(string Unit)//斷線將socket移除字典
        {
            if (_dicSock.Keys.Contains(Unit))
                _dicSock.Remove(Unit);
        }


        public bool IsClientConnect(string Unit)
        {
            return _dicSock.Keys.Contains(Unit);
        }


        public bool Write(string Unit, string str)
        {
            str = str.Replace(" ", "");

            string[] strArrary = str.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);//231215  Ming

            foreach (string s in strArrary) //  210727  Ming
            {
                byte[] byteData;
                if (s.Contains('\r') == false)
                    byteData = Encoding.ASCII.GetBytes(s + '\r');
                else
                    byteData = Encoding.ASCII.GetBytes(s);

                if (Write(Unit, byteData) == false)
                    return false;
            }
            return true;
        }
        public bool Write(string Unit, params byte[] bytes)
        {
            try
            {
                if (_dicSock[Unit].Connected)
                {
                    _dicSock[Unit].Send(bytes);
                }
                else
                {
                    //_logger.WriteLog("<<<Error>>> Client is disconnect. index = [{0}], data = [{1}].", nCnt, Encoding.ASCII.GetString(bytes));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                return false;
            }
            return true;
        }


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

    }
}
