using RorzeComm.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using RorzeUnit.Net.Sockets;
using System.Linq;

namespace RorzeUnit.Class.EQ
{
    class SSEQ_Socket
    {
        private sServer m_Sever;
        private Socket m_Socket;
        private sClient m_Client;

        bool m_bSimulate;
        string m_strIP;
        int m_nPortNo;
        string m_strCmdStart = "\x02";  //  與eq定義通訊格式 起始碼
        string m_strCmdEnd = "\r\n";    //  與eq定義通訊格式 結尾碼
        string m_strUnitName;
        bool m_bUnitCheckPort = false;
        SLogger m_logger;

        private ConcurrentQueue<string[]> _queRecvBuffer;
        public ConcurrentQueue<string[]> QueRecvBuffer { get { return _queRecvBuffer; } }
        public SSEQ_Socket(string strIP, int nPort, bool bSimulate, string strStartSymbol, string strEndSymbol, sServer Sever = null)
        {
            m_Sever = Sever;
            m_strIP = strIP;
            m_nPortNo = nPort;
            m_bSimulate = bSimulate;

            m_strCmdStart = strStartSymbol;
            m_strCmdEnd = strEndSymbol;
            m_strUnitName = "EQ";

            if (m_Sever == null)
            {
                m_Client = new sClient();
                m_Client.OnAssgnSocket += _Client_OnAssgnSocket;//收到連線成功
                m_Client.onDataRecive += _Client_OnReadData;
            }
            else
            {
                m_Sever.OnAssgnSocket += _Sever_OnAssgnSocket;//用於Sever收到連線後執行
            }
            _queRecvBuffer = new ConcurrentQueue<string[]>();
        }
        //  Sever
        private void _Sever_OnAssgnSocket(object Sender, sServer.SocketEventArgs e)//Server收到Client連上線
        {
            if (m_bUnitCheckPort) { if (e._PortNo != m_nPortNo) return; }
            else if (e._IP.ToString().Contains("127.0.0.")) { }//Ming 測試用
            //else { if (e._IP != m_strIP) return; }

            m_logger = SLogger.GetLogger("EQCommunicationLog");

            m_logger.WriteLog("Socket accept client connected {0}", e._Socket.RemoteEndPoint);

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

                    if (strReceive == "")// 210721 Ming 
                    {
                        m_logger.WriteLog("Client is disConnected {0}", m_Socket.RemoteEndPoint);
                        m_Sever.RemoveClint(m_strUnitName);
                        break;//斷線了
                    }

                    //  上一輪中斷的資料接上
                    if (m_strBuffer != "")
                    {
                        strReceive = m_strBuffer + strReceive;
                        m_strBuffer = "";
                    }

                    //  判斷收到資料可能沒有結束碼
                    int nIdx = strReceive.LastIndexOf(m_strCmdEnd);
                    if (nIdx != strReceive.Length - 1)
                    {
                        m_strBuffer = strReceive.Substring(nIdx + 1);
                        strReceive = strReceive.Substring(0, nIdx + 1);
                    }

                    //  收到東西後會以 結束符號 來判斷分割後加入到 Queue
                    string[] astrFrame = strReceive.Split(new string[] { m_strCmdEnd }, System.StringSplitOptions.RemoveEmptyEntries);
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
                if (!m_Socket.Connected)
                    m_Sever.RemoveClint(m_strUnitName);  // 斷線後 Remove Socket item ...
                m_logger.WriteLog("Exception :" + ex.ToString());
            }
        }
        //  Client
        public void Open()
        {
            if (m_Sever != null) return;
            m_logger = SLogger.GetLogger("EQCommunicationLog");
            m_Client.connect(m_strIP, m_nPortNo);
        }
        private void _Client_OnAssgnSocket(object Sender, sClient.SocketEventArgs e)//Client 連線到Server通知
        {
            //if (m_bUnitCheckPort) { if (e._PortNo != m_nPortNo) return; }
            //else { if (e._IP != m_strIP) return; }

            m_Socket = e._Socket;
            m_logger.WriteLog("Socket client connected to {0}", e._Socket.RemoteEndPoint.ToString());
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
            /*int nIdx = strReceive.LastIndexOf('\r');
            if (nIdx != strReceive.Length - 1)
            {
                m_strBuffer = strReceive.Substring(nIdx + 1);
                strReceive = strReceive.Substring(0, nIdx + 1);
            }*/

            //  收到東西後會以 結束符號 來判斷分割後加入到 Queue
            string[] astrFrame = strReceive.Split(new string[] { m_strCmdEnd }, System.StringSplitOptions.RemoveEmptyEntries);
            _queRecvBuffer.Enqueue(astrFrame);
        }
        //  Send
        public void SendCommand(string strCommand)
        {
            //string strFrame = string.Format("{0}{1}{2}", strCommand, (char)0x0A, (char)0x0D); //尾碼 \r\n = ascii[13][10] = CrL  
            string strFrame = strCommand;          
            if (strCommand.Contains(m_strCmdStart) == false)
                strFrame = m_strCmdStart + strFrame;
            if (strCommand.Contains(m_strCmdEnd) == false)
                strFrame = strFrame + ":" + m_strCmdEnd;

            m_logger.WriteLog("[{0}] send:{1}", m_strUnitName, strFrame);

            if (isConnected() == false) return;

            if (m_Sever == null)
            {
                m_Client.Write(strFrame);
            }
            else
            {
                if (m_Sever.Write(m_strUnitName, strFrame) == false)
                {
                    // alarm .... 
                    m_logger.WriteLog("send fail.");
                }
            }
        }
        public void SendCommand(string format, params object[] args)
        {
            SendCommand(string.Format(format, args));
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
                return (m_Socket != null && m_Sever.IsClientConnect(m_strUnitName));
            }
        }
    }
}
