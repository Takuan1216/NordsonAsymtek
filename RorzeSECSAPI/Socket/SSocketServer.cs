using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;

namespace Rorze.SocketObject
{
    public class SSocketServer
    {
        protected SLogger _logger;
        private List<Socket> _lstSockets = new List<Socket>();

        public event MessageEventHandler OnReadData;
        
        private List<SInterruptOneThread> _pollings;
        
        public SSocketServer(IPAddress localAddr, int port, string strLogName)
        {
            //系統日誌物件
            _logger = SLogger.GetLogger(strLogName);
            //socket連線物件
            _lstSockets.Add(new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            _lstSockets[0].Bind(new IPEndPoint(IPAddress.Any, port));
            //掃描每一個client資料
            _pollings = new List<SInterruptOneThread>();
            _pollings.Add(new SInterruptOneThread(RunClientAccept));
            //開始接聽
            _lstSockets[0].Listen(10); //最大允許10個Client.
           // _lstSockets[0].NoDelay = true;
           //另闢執行緒等待client端連線
           _pollings[0].Set();
        }

        void RunClientAccept()
        {
            Socket socket = _lstSockets[0].Accept(); //等待連線 (連線未應答時, thread在此凍結)
            _lstSockets.Add(socket); //加入socket集合
            //========== 連線應答
            _logger.WriteLog("Client is connected!! remote=[{0}].", socket.RemoteEndPoint);
            _pollings.Add(new SInterruptOneThread(RunClientAccept));
            _pollings[_pollings.Count - 1].Set(); //開闢下一個執行緒等待下一個client端連線
            //========== 接收資料
            string strReceive;
            byte[] byteReceive = new byte[1024];
            try
            {
                while (true)
                {
                    Array.Clear(byteReceive, 0, byteReceive.Length); //陣列初始化                
                    socket.Receive(byteReceive);
                    strReceive = Encoding.Default.GetString(byteReceive);
                    strReceive = strReceive.Trim('\0');
                    if (strReceive.Length > 0)
                    {
                        if (OnReadData != null) OnReadData(this, new MessageEventArgs(strReceive));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                _lstSockets.Remove(socket);
                _logger.WriteLog("<<<Warning>>> Socket has error. terminal current client and re-connect again.");
            }
          
        }

        public bool Write(params byte[] bytes)
        {
            try
            {
                for (int nCnt = 1; nCnt < _lstSockets.Count; nCnt++) //從1開始, 0是server listener端
                {
                    if (_lstSockets[nCnt] != null)
                    {
                        if (_lstSockets[nCnt].Connected)
                        {
                            _lstSockets[nCnt].Send(bytes);
                        }
                        else 
                        {
                            _logger.WriteLog("<<<Error>>> Client is disconnect. index = [{0}], data = [{1}].", nCnt, Encoding.ASCII.GetString(bytes));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return false;
            }
            return true;
        }
        public bool Write(string str)
        {
            return Write(Encoding.ASCII.GetBytes(str));
        }
        public bool Write(string format, params object[] args)
        {
            return Write(string.Format(format, args));
        }
    }
}
