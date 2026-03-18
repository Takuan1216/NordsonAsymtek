
using RorzeComm.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RC550.RorzeUnit
{
    public class SR1000Analysis : SR1000
    {
        private SPollingThread _exeDequeueMsg;
        private SPollingThread _exeSendDequeueMsg;

        public SR1000Analysis(string ip, int nPort, bool bDisable, bool bSimulate)
            : base(ip, nPort, bDisable, bSimulate)
        {
            _exeDequeueMsg = new SPollingThread(100);
            _exeDequeueMsg.DoPolling += _exeDequeueMsg_DoPolling; ;
            _exeDequeueMsg.Set();

            _exeSendDequeueMsg = new SPollingThread(100);
            _exeSendDequeueMsg.DoPolling += _exeSendDequeueMsg_DoPolling; ;
            _exeSendDequeueMsg.Set();
        }

        private void _exeDequeueMsg_DoPolling()
        {
            string strFrame = "";

            try
            {
                if (!_queRecvBuffer.TryDequeue(out strFrame)) return;

                if (OnMessageUpData != null)
                    OnMessageUpData(this, new MessageUpDataEventArgs(strFrame));

                waitReadCompleted.Set();
            }
            catch (Exception ex)
            {
                //_log.WriteLog(ex);
                //_log.WriteLog(strFrame);
            }
        }
        private void _exeSendDequeueMsg_DoPolling()
        {
            string strFrame;
            if (!_queSendBuffer.TryDequeue(out strFrame)) return;

            Sendcommand(strFrame);

            if (OnSendMessageUpData != null)
                OnSendMessageUpData(this, new MessageUpDataEventArgs(strFrame));
        }

        public event MessageUpDataHandler OnSendMessageUpData;
        public event MessageUpDataHandler OnMessageUpData;
        public delegate void MessageUpDataHandler(object sender, MessageUpDataEventArgs e);
        public class MessageUpDataEventArgs : EventArgs
        {
            public MessageUpDataEventArgs(string strMessage)
            {
                Message = strMessage;
            }
            public string Message { get; set; }
        }


    }
}
