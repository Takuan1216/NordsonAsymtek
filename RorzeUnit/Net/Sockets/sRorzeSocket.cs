using RorzeComm.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeUnit.Net.Sockets
{
    public class sRorzeSocket
    {
        public event EventHandler<bool> OnConnectChange;

        private sServer m_Sever;
        private Socket m_Socket;
        private sClient m_Client;

        bool m_bSimulate;
        string m_strIP;
        int m_nPortNo;

        string m_strUnitName;
        bool m_bUnitCheckPort = false;
        bool m_bByteMode = false;
        SLogger m_logger = SLogger.GetLogger("CommunicationLog");

        private ConcurrentQueue<string[]> _queRecvBuffer;
        public ConcurrentQueue<string[]> QueRecvBuffer { get { return _queRecvBuffer; } }

        private ConcurrentQueue<byte[]> _queRecvByteBuffer;
        public ConcurrentQueue<byte[]> QueRecvByteBuffer { get { return _queRecvByteBuffer; } }
        public sRorzeSocket(string strIP, int nPort, int nBodyNO, string strUnitName, bool bSimulate, sServer Sever = null, bool bByteMode = false)
        {
            m_Sever = Sever;
            m_strIP = strIP;
            m_nPortNo = nPort;

            m_bSimulate = bSimulate;
            m_bByteMode = bByteMode;
            m_strUnitName = strUnitName + nBodyNO.ToString();

            if (m_Sever == null)
            {
                m_Client = new sClient();
                m_Client.OnAssgnSocket += _Client_OnAssgnSocket;//收到連線成功
                m_Client.onDataRecive += _Client_OnReadData;
                m_Client.OnConnectChange += OnConnectChange;
                m_Client.EventHandlerLog += (object sender, string message) =>
                {
                    WriteLog(message);
                };
                if (m_bByteMode)
                {
                    m_Client.onDataReciveByByte += _Client_OnReadByteData;
                }
            }
            else
            {
                m_Sever.OnAssgnSocket += _Sever_OnAssgnSocket;//收到連線成功
            }
            _queRecvBuffer = new ConcurrentQueue<string[]>();
            _queRecvByteBuffer = new ConcurrentQueue<byte[]>();
        }
        //  Server   
        private void _Sever_OnAssgnSocket(object Sender, sServer.SocketEventArgs e)//Server收到Client連上線
        {
            if (m_bUnitCheckPort) { if (e._PortNo != m_nPortNo) return; }
            else { if (e._IP != m_strIP) return; }

            WriteLog(string.Format("Socket accept client connected.{0}_[{1}]", m_strUnitName, e._Socket.RemoteEndPoint));

            if (m_Sever.AddClintList(m_strUnitName, e._Socket))
            {
                m_Socket = e._Socket;
                StartReceive();
            }
        }
        private void StartReceive()
        {
            Thread OnReceive = new Thread(Receive);
            OnReceive.IsBackground = true;
            OnReceive.Start();
        }

        string m_strBuffer = "";
        private void Receive()
        {
            try
            {
                string strReceive;
                byte[] byteReceive = new byte[1024];
                while (m_Socket.Connected)
                {
                    SpinWait.SpinUntil(() => false, 1);
                    // 從已繫結的 Socket 接收資料。
                    Array.Clear(byteReceive, 0, byteReceive.Length);
                    m_Socket.Receive(byteReceive);

                    //  資料轉換成字串
                    strReceive = Encoding.Default.GetString(byteReceive);
                    strReceive = strReceive.Trim('\0');

                    //  上一輪中斷的資料接上
                    if (m_strBuffer != "")
                    {
                        strReceive = m_strBuffer + strReceive;
                        m_strBuffer = "";
                    }
                    //  判斷收到資料可能沒有結束碼
                    int nIdx = strReceive.LastIndexOf('\r');
                    if (nIdx != strReceive.Length - 1)
                    {
                        m_strBuffer = strReceive.Substring(nIdx + 1);
                        strReceive = strReceive.Substring(0, nIdx + 1);
                    }

                    //  收到東西後會以 \r 來判斷分割後加入到 Queue
                    string[] astrFrame = strReceive.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    _queRecvBuffer.Enqueue(astrFrame);
                }

                if (!m_Socket.Connected)
                {
                    m_Sever.RemoveClint(m_strUnitName);  // 斷線後 Remove Socket item ...
                    //close progame
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
                if (!m_Socket.Connected)
                    m_Sever.RemoveClint(m_strUnitName);  // 斷線後 Remove Socket item ...
            }
        }
        //  Client
        public void Open()
        {
            if (m_Sever != null) return;
            m_Client.connect(m_strIP, m_nPortNo);
        }
        private void _Client_OnAssgnSocket(object Sender, sClient.SocketEventArgs e)//Client 連線到Server通知
        {
            //if (m_bUnitCheckPort) { if (e._PortNo != m_nPortNo) return; }
            //else { if (e._IP != m_strIP) return; }

            m_Socket = e._Socket;
            WriteLog(string.Format("Socket client connected to {0}_[{1}]", m_strUnitName, e._Socket.RemoteEndPoint));
        }
        private void _Client_OnReadData(sClient.Recive args)
        {
            string strReceive = args.sReciveData;

            //  上一輪中斷的資料接上
            if (m_strBuffer != "")
            {
                strReceive = m_strBuffer + strReceive;
                m_strBuffer = "";
            }
            //  判斷收到資料可能沒有結束碼
            int nIdx = strReceive.LastIndexOf('\r');
            if (nIdx != strReceive.Length - 1)
            {
                m_strBuffer = strReceive.Substring(nIdx + 1);
                strReceive = strReceive.Substring(0, nIdx + 1);
            }

            //  收到東西後會以 \r 來判斷分割後加入到 Queue
            string[] astrFrame = strReceive.Split(new char[] { '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            _queRecvBuffer.Enqueue(astrFrame);
        }
        private void _Client_OnReadByteData(sClient.ReciveByByte args)
        {
            byte[] byteReceive = args.sReciveData;
            _queRecvByteBuffer.Enqueue(byteReceive);
        }
        //  send
        public void SendCommand(string strCommand, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            //string strFrame = string.Format("{0}{1}{2}", strCommand, (char)0x0A, (char)0x0D); //尾碼 \r\n = ascii[13][10] = CrL  
            string strFrame = string.Format("o{0}.{1}{2}", m_strUnitName, strCommand, (char)0x0D); //尾碼 \r\n = ascii[13][10] = CrL
            WriteLog(string.Format("send : {0}", strFrame));

            if (m_bSimulate) return;
            if (m_Sever == null)
            {
                m_Client.Write(strFrame);
            }
            else
            {
                if (!m_Sever.Write(m_strUnitName, strFrame))
                {
                    // alarm ....                  
                    WriteLog(string.Format("send fail.[{0}]", strFrame));
                }
            }
        }
        //  send for stock
        public void SendCommand(int nBodyNum, string strCommand, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            //string strFrame = string.Format("{0}{1}{2}", strCommand, (char)0x0A, (char)0x0D); //尾碼 \r\n = ascii[13][10] = CrL  
            string strFrame = string.Format("o{0}.{1}{2}", m_strUnitName.Substring(0, 3) + nBodyNum, strCommand.ToUpper(), (char)0x0D); //尾碼 \r\n = ascii[13][10] = CrL
            WriteLog(string.Format("send : {0}", strFrame));

            if (m_bSimulate) return;
            if (m_Sever == null)
            {
                m_Client.Write(strFrame);
            }
            else
            {
                if (!m_Sever.Write(m_strUnitName, strFrame))
                {
                    // alarm ....                  
                    WriteLog(string.Format("send fail.[{0}]", strFrame));
                }
            }
        }

        //---------------------------------------------------------------------
        public bool isConnected()
        {
            if (m_Sever == null)
            {
                return m_Client.ConnectStat;
            }
            else
            {
                return m_Socket != null;
            }
        }





        private void WriteLog(string strMsg, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            try
            {
                strMsg = strMsg + " at line " + lineNumber + " (" + caller + ")";
                m_logger.WriteLog("[{0}] : {1}", m_strUnitName, strMsg);
            }
            catch (Exception ex)
            {
                strMsg = ex + " at line " + lineNumber + " (" + caller + ")";
                m_logger.WriteLog("[{0}] : {1}", m_strUnitName, strMsg);
            }
        }
    }
}
