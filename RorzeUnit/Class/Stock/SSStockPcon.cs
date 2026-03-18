using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeUnit.Class.Stock
{
    public class SSStockPcon
    {
        private sRorzeSocket m_Socket;
        private SPollingThread _exePolling;
        private SLogger m_logger = SLogger.GetLogger("CommunicationLog");
        public event EventHandler<string[]> OnReadData;

        public SSStockPcon(string strIP, int nPortID, bool bSimulate, sServer sever = null)
        {
            m_Socket = new sRorzeSocket(strIP, nPortID, 1, "STK", bSimulate, sever);

            _exePolling = new SPollingThread(1);
            _exePolling.DoPolling += _exePolling_DoPolling;
            _exePolling.Set();
        }

        public void Open() { m_Socket.Open(); }

        public void SendCommand(int nBodyNo, string strMsg)
        {
            lock (this)
            {
                SpinWait.SpinUntil(() => false, 100);
                m_Socket.SendCommand(nBodyNo, strMsg);
                SpinWait.SpinUntil(() => false, 100);
            }
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[ICON] : {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }

        private void _exePolling_DoPolling()
        {
            try
            {
                string[] astrFrame;
                if (!m_Socket.QueRecvBuffer.TryDequeue(out astrFrame)) return;
                OnReadData?.Invoke(this, astrFrame);
            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
            }
        }

    }
}
