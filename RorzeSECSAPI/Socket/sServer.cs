using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

using RorzeComm;

namespace Rorze.SocketObject
{
     public class sServer: ISocket
    {
        protected List<Socket> _lstSockets = new List<Socket>();
        private Dictionary<string ,Socket>  _dicSock = new Dictionary<string, Socket>();
        public event SocketEventHandler OnAssgnSocket;
       // public event MessageEventHandler OnReadData;
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public class MessageEventArgs : EventArgs
        {
            public MessageEventArgs(string strMessage)
            {
                Message = strMessage;
            }
            public string Message { get; set; }
        }

        //SPollingThread _exeCheckClinetConnect;

        //public event EventHandler OnGuiConnected;
        protected List<Thread> _pollings;
        
        public string _localAddr;
        public int _port;
        public string strLogName;
        string ServerName = "RorzeSystem";

        public bool IsOpen;

        //bool CheckByteStuts = false;
        byte[] CheckLiveByte = new byte[1];
        int ClinetCount;

        List<SocketInfo> _SocketInfo;
        SocketInfo ServerInfo;
        bool IsSimulation;
        public sServer(SocketInfo Info, int Count,bool Simulation)//(string localAddr, int port, string strLogName)
        {
            IsSimulation = Simulation;
             IsOpen = false;
             ClinetCount = Count;
            _pollings = new List<Thread>();
             ServerInfo = Info;
            _pollings.Add(new Thread(RunClientAccept));
            //_exeCheckClinetConnect = new SPollingThread(1000);
            //_exeCheckClinetConnect.DoPolling += _exeCheckClinetConnect_DoPolling;


        }
        public void InitConnect(List<SocketInfo> Info)
        {
            
            
            var FindClient = from Sockets in Info
                             where Sockets.UnitName != ServerInfo.UnitName && Sockets.type == SocketTypes.Server
                             select Sockets;

            List<SocketInfo> ClintInfo = FindClient.ToList();
            _SocketInfo = ClintInfo;

            //  ServerName = _SocketInfo[0].UnitName;
            _dicSock.Add(ServerName, new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
           // _dicSock[ServerName].NoDelay = true;
            //   _dicSock[ServerName].Bind(new IPEndPoint(IPAddress.Parse(ServerInfo.SocketIP), ServerInfo.SocketPort));
            _dicSock[ServerName].Bind(new IPEndPoint(IPAddress.Any, ServerInfo.SocketPort));
            _dicSock[ServerName].Listen(ClinetCount + 1);
        }
        private void _exeCheckClinetConnect_DoPolling()
        {
            try
            {
                bool Close = false;
                if (_dicSock.Count == 0) return;
                foreach (string Clinet in _dicSock.Keys)
                {
                    if (Clinet == "RorzeSystem") continue;
                    if (_dicSock[Clinet].Connected
                                && _dicSock[Clinet].Poll(0, SelectMode.SelectRead))
                        Close = _dicSock[Clinet].Receive(CheckLiveByte, SocketFlags.Peek) == 0;
                    if (_dicSock[Clinet].Connected && !Close)
                        continue;
                    else
                        RemoveClint(Clinet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }



        public void ServerStart()
        {
            IsOpen = true;
            _pollings[0].Start();
          //  _exeCheckClinetConnect.Set();
        }
        public void ServerClose()
        {
            IsOpen = false;
            //   _pollings[0].Abort();
            _dicSock[ServerName].Close();
            _dicSock[ServerName].Dispose();
            _dicSock.Clear();
          //  _exeCheckClinetConnect.Close();
        }

        public void RunClientAccept()
        {
            try
            {
                while (IsOpen)
                {
                    Socket socket = _dicSock[ServerName].Accept();

                    string RemoteportID = socket.RemoteEndPoint.ToString().Split(':')[1];
                    string RemoteIP = socket.RemoteEndPoint.ToString().Split(':')[0];
                    var Findunit = from Sockets in _SocketInfo
                               where Sockets.SocketIP == RemoteIP
                               && Sockets.UnitName != ServerName
                               select Sockets;
                    if (IsSimulation)
                    {
                         Findunit = from Sockets in _SocketInfo
                                       where Sockets.SocketIP == RemoteIP
                                       && Sockets.UnitName != ServerName
                                       && Sockets.SocketPort == Convert.ToInt32(RemoteportID)
                                       select Sockets;
                    }               
                    SocketInfo SocketUnit = Findunit.ElementAtOrDefault(0);
                    // if (_SocketInfo.Where(x => x.SocketIP == RemoteIP && x.SocketPort == int.Parse(RemoteportID)).Count() > 0)
                    if(Findunit.Count()>0)
                    {
                        if (OnAssgnSocket != null)
                            OnAssgnSocket(this, new SocketEventArgs(SocketUnit.UnitName, socket, RemoteIP, int.Parse(RemoteportID), SocketTypes.Server));
                       
                        AddClintList(SocketUnit.UnitName, socket);
                    }
                    Thread.Sleep(100);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        

        }


        private byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
        {
            byte[] buffer = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(buffer, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(buffer, 4);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(buffer, 8);
            return buffer;
        }

        public bool Connected(string UnitName)
        {
            if (_dicSock.ContainsKey(UnitName))
                return _dicSock[UnitName].Connected;
            else
                return false;
        }
        public bool AddClintList(string Unit,Socket Sockets)
        {
            if (_dicSock.Keys.Contains(Unit))
                _dicSock.Remove(Unit);


               _dicSock.Add(Unit, Sockets);
                return true;
            
          
        }

        public void RemoveClint(string Unit)
        {
            if (_dicSock.Keys.Contains(Unit))
                _dicSock.Remove(Unit);
        }

        void AutoReply(string RemoteportID,string sReplyData)
        {
            string sNewReply = sReplyData.Substring(0, 1).Replace('o', 'a');
            sNewReply += sReplyData.Substring(1, sReplyData.Length - 1);
            Write(RemoteportID,sNewReply);
        }

        public bool Write(int SocketNum, string str)
        {
            str = str.Replace(" ", "");
            str = str + "\r";
            string[] sRes = new string[] { };
            sRes = str.Split('\r');
            bool bData = false;
            for (int nDataCount = 0; nDataCount < sRes.Length - 1; nDataCount++)
            {
                bData = Write(SocketNum, Encoding.ASCII.GetBytes(sRes[nDataCount] + '\r'));
                if (!bData) break;
            }
            return bData;

        }
        public bool Write(int SocketNum, params byte[] bytes)
        {
            try
            {
                        if (_lstSockets[SocketNum].Connected)
                        {
                            _lstSockets[SocketNum].Send(bytes);
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
        public bool Write(string Unit, string str)
        {
            //str = str.Replace(" ", "");
             str = (char)0x01+str + "\r";
            //str = " " + str + "\r";
            string[] sRes = new string[] { };
            sRes = str.Split('\r');
            bool bData = false;
            for (int nDataCount = 0; nDataCount < sRes.Length - 1; nDataCount++)
            {
                bData = Write(Unit, Encoding.ASCII.GetBytes(sRes[nDataCount] + '\r'));
                if (!bData) break;
            }
            return bData;

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

        public bool Open()
        {
            this.ServerStart();
            return IsOpen;
        }

        public bool Close()
        {
            this.ServerClose();
            return IsOpen;
        }
    }
    
}
