using RorzeComm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
  public  class SInterruptOneThreadobkj: EventWaitHandle, IDisposable
    {
        private Thread _thread;
        private int _nTimeout;
        private bool _bEnableTimeout;
        private dlgv_Object _callback;
        private bool _bTerminated;
        private object _Data;
        public SInterruptOneThreadobkj(dlgv_Object callback)
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
        public void Set(object Data)
        {
            _Data = Data;
            this.Set();
        }
        private void Execute()
        {
            while (true)
            {
                if (_bEnableTimeout) this.WaitOne(_nTimeout);
                else this.WaitOne();

                this.Reset();

                if (_bTerminated) break;

                if (_callback != null) _callback(_Data);
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
