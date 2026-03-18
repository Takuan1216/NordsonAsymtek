using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RorzeUnit.Class.BarCode
{
    public class SR1000 : I_BarCode
    {
        private SSignal _signalSubSequence = new SSignal(false, EventResetMode.ManualReset);
        private SSignal _signalAck = new SSignal(false, EventResetMode.ManualReset);
        private Socket m_Socket;
        private sClient m_Client;
        private SPollingThread _exeDequeueMsg;
        private ConcurrentQueue<string[]> _queRecvBuffer;
        private SLogger m_logger = SLogger.GetLogger("CommunicationLog");


        public bool Connected { get { return m_Client.ConnectStat; } }
        public bool Simulate { get; private set; }
        public bool Disable { get; private set; }

        private string m_strIP = "127.0.0.1";
        private int m_nPort = 23;
        private string m_strReceive = "";

        public SR1000(string ip, int nPort, bool bDisable, bool bSimulate)// to Unit
        {
            m_strIP = ip;
            m_nPort = nPort;
            Disable = bDisable;
            Simulate = bSimulate;

            _queRecvBuffer = new ConcurrentQueue<string[]>();

            m_Client = new sClient();
            m_Client.OnAssgnSocket += _Client_OnAssgnSocket;//收到連線成功
            m_Client.onDataRecive += _Client_OnReadData;
            m_Client.EventHandlerLog += (object sender, string message) => { WriteLog(message); };

            _exeDequeueMsg = new SPollingThread(100);
            _exeDequeueMsg.DoPolling += _exeDequeueMsg_DoPolling; ;
            _exeDequeueMsg.Set();

        }
        private void _Client_OnAssgnSocket(object Sender, sClient.SocketEventArgs e)//Client 連線到Server通知
        {
            m_Socket = e._Socket;
            WriteLog(string.Format("Socket client connected to [{0}]", e._Socket.RemoteEndPoint));
        }
        private void _Client_OnReadData(sClient.Recive args)//Client接收到訊息
        {
            string strReceive = args.sReciveData;

            //  收到東西後會以 \r 來判斷分割後加入到 Queue
            string[] astrFrame = strReceive.Split(new char[] { '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            _queRecvBuffer.Enqueue(astrFrame);
        }
        private void WriteLog(string strMsg, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            try
            {
                strMsg = strMsg + " at line " + lineNumber + " (" + caller + ")";
                m_logger.WriteLog("[BCRD] : " + strMsg);
            }
            catch (Exception ex)
            {
                strMsg = ex + " at line " + lineNumber + " (" + caller + ")";
                m_logger.WriteLog("[BCRD] : " + strMsg);
            }
        }
        private void _exeDequeueMsg_DoPolling()
        {
            try
            {
                string[] astrFrame;
                if (!_queRecvBuffer.TryDequeue(out astrFrame)) return;
                string strFrame;

                for (int i = 0; i < astrFrame.Count(); i++)
                {
                    strFrame = astrFrame[i];
                    WriteLog("Recv " + strFrame);
                    m_strReceive = strFrame;
                    _signalAck.Set();
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
        }
        //  send 
        private void SendCommand(string strCommand, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            //string strFrame = string.Format("{0}{1}{2}", strCommand, (char)0x0A, (char)0x0D); //尾碼 \r\n = ascii[13][10] = CrL  
            string strFrame = string.Format("{0}{1}", strCommand.ToUpper(), (char)0x0D); //尾碼 \r\n = ascii[13][10] = CrL
            WriteLog(string.Format("send : {0}", strFrame), lineNumber, caller);
            if (Simulate) return;
            m_Client.Write(strFrame);
        }

        //---------------------------------------------------------------------------------------
        public void Open()
        {
            if (false == Simulate && false == Disable)
            {
                m_Client.connect(m_strIP, m_nPort);
            }
        }
        public string Read()
        {
            string strID;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck.Reset();
                SendCommand("LON");
                if (!_signalAck.WaitOne(3000))
                {
                    WriteLog("send command failure(timeout)!");
                    strID = "Fail";
                }
                else
                {
                    strID = m_strReceive.Replace("\r\n", "");                 
                }
                SendCommand("LOFF");
            }
            else
            {
                strID = "test";
            }
            _signalSubSequence.Set();
            return strID;
        }
        public void ReadTest()
        {
            _signalSubSequence.Reset();
            SendCommand("TEST1");
            _signalSubSequence.Set();
        }
        public void Quit()
        {
            _signalSubSequence.Reset();
            SendCommand("QUIT");
            _signalSubSequence.Set();
        }
        public string GetReply()
        {
            return m_strReceive;
        }

    }
}
