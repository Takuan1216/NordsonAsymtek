using RorzeComm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptAllThread : List<EventWaitHandle>, IDisposable
    {
        private Thread _thread;
        private dlgv_v _callback;
        private bool _bTerminated;
        private bool _bEnableTimeout;
        private int _nTimeout;
        public SInterruptAllThread(dlgv_v callback, int nNumOfSignal)
            :base(nNumOfSignal)
        {
            for (int nIdx = 0; nIdx < nNumOfSignal; nIdx++)
                this.Add(new EventWaitHandle(false, EventResetMode.ManualReset));
            _callback = callback;
            _bTerminated = false;
            _bEnableTimeout = false;
            _nTimeout = 30000;
            _thread = new Thread(new ThreadStart(Execute));
            _thread.IsBackground = true;
            _thread.Start();
        }
        public SInterruptAllThread(dlgv_v callback, params EventWaitHandle[] signals)
            :base(signals)
        {
            _callback = callback;
            _bTerminated = false;
            _bEnableTimeout = false;
            _nTimeout = 30000;
            _thread = new Thread(new ThreadStart(Execute));
            _thread.IsBackground = true;
            _thread.Start();
        }
        private void Execute()
        {
            while (true)
            {
                if (_bEnableTimeout)
                    EventWaitHandle.WaitAll(this.ToArray(), _nTimeout);
                else
                    EventWaitHandle.WaitAll(this.ToArray());

                if (_bTerminated) break;
                if (_callback != null) _callback();
                foreach (EventWaitHandle item in this)
                    item.Reset();
            }
        }
        public bool bEnableTimeout
        {
            set { _bEnableTimeout = value; }
            get { return _bEnableTimeout; }
        }
        public void Dispose()
        {
            _bTerminated = true;
            foreach (EventWaitHandle item in this)
                item.Set();            
        }
    }
}
