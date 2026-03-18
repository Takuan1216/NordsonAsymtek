using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeUnit.Class.RFID
{
    public class SmartRFID
    {
        public event EventHandler<string> OnReadID;

        private SLogger m_Logger;
        private sServer m_Sever;
        private Socket m_Socket;

        private SPollingThread m_PollingDequeueRecv;
        private ConcurrentQueue<string> m_QueRecvBuffer;//receive

        private string m_strUnitName;
        private string m_strIP;
        private bool m_bSimulate;
        private bool m_bDisable;
        public bool m_bConnect;
        public bool _Connected { get { return m_bConnect; } }
        public bool _Disable { get { return m_bDisable; } }

        public SmartRFID(string strIP, bool bSimulate, bool bDisable, sServer sever)
        {
            m_strIP = strIP;
            m_bSimulate = bSimulate;
            m_bDisable = bDisable;
            m_Sever = sever;
            m_strUnitName = "SmartRFID";
            m_Sever.OnAssgnSocket += Sever_OnAssgnSocket;//收到連線成功

            m_QueRecvBuffer = new ConcurrentQueue<string>();

            m_PollingDequeueRecv = new SPollingThread(10);
            m_PollingDequeueRecv.DoPolling += PollingDequeueRecv;

            if (m_bSimulate == false)
            {
                m_PollingDequeueRecv.Set();
            }
            if (_Disable == false)
            {
                m_Logger = SLogger.GetLogger("SmartRFID");
            }
        }

        ~SmartRFID()
        {
            m_PollingDequeueRecv.Close();
            m_PollingDequeueRecv.Dispose();
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[SmartRFID] : {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_Logger.WriteLog(strMsg);
        }

        #region 處理TCP接收到的內容
        private void PollingDequeueRecv()
        {
            string strFrame;
            try
            {

                if (!m_QueRecvBuffer.TryDequeue(out strFrame)) return;

                //紀錄log
                WriteLog("Receive : " + strFrame + "\r\n");

                string[] id = strFrame.Split(' ');
                if (id.Length > 1)            
                    if (OnReadID != null) OnReadID(this, id[1]);

                //if (OnReadData != null) OnReadData(this, new MessageEventArgs(astrFrame));

                //for (int i = 0; i < astrFrame.Count(); i++) //只處理第一個封包 2014.11.24
                //{
                //    if (astrFrame[i].Length == 0)
                //    {
                //        continue;
                //    }

                //    strFrame = astrFrame[i];

                //    string[] id = strFrame.Split(' ');
                //    if (id.Length > 1)
                //        WriteLog("Receive User ID : " + id[1]);
                //}
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> _exePolling_DoPolling:" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> _exePolling_DoPolling:" + ex);
            }
        }
        #endregion


        //  Server   
        private void Sever_OnAssgnSocket(object Sender, sServer.SocketEventArgs e)//Server收到Client連上線        
        {
            if (e._IP != m_strIP) return;

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

                    int bytesReceived = m_Socket.Receive(byteReceive);//一個位元組包含8 個位元(bit)
                    byte[] result = new byte[bytesReceived];
                    Array.Copy(byteReceive, result, bytesReceived);

                    //  資料轉換成字串
                    //strReceive = BitConverter.ToString(result);

                    strReceive = Encoding.Default.GetString(result);

                    m_QueRecvBuffer.Enqueue(strReceive.Replace("\0", ""));
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
    }
}
