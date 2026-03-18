using RorzeComm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptOneThread : EventWaitHandle, IDisposable
    {
        private Thread _thread;
        private int _nTimeout;
        private bool _bEnableTimeout;
        private dlgv_v _callback;
        private bool _bTerminated;
        public SInterruptOneThread(dlgv_v callback)
            : base(false, EventResetMode.ManualReset)
        {
            _bTerminated = false;
            _callback = callback;
            _thread = new Thread(new ThreadStart(Execute));
            _thread.IsBackground = true;
            _thread.Start();
            _nTimeout = 30000;
            _bEnableTimeout = false;
        }
        public void Abort()
        {
            _thread.Abort();
            this.Reset();
            _thread = new Thread(new ThreadStart(Execute));
            _thread.IsBackground = true;
            _thread.Start();            
        }
        private void Execute()
        {
            while (true)
            {
                if (_bEnableTimeout) this.WaitOne(_nTimeout);
                else this.WaitOne();

                this.Reset();

                if (_bTerminated) break;

                if (_callback != null) _callback();
            }
        }

        public bool bEnableTimeout
        {
            set { _bEnableTimeout = value; }
            get { return _bEnableTimeout; }
        }

        void IDisposable.Dispose()
        {            
            _bTerminated = true;
            this.Set();
        }
    }
}
