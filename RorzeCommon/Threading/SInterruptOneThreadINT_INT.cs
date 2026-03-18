using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptOneThreadINT_INT : EventWaitHandle, IDisposable
    {
        private Thread _thread;
        private int _nTimeout;
        private bool _bEnableTimeout;
        private dlgv_n_n _callback;
        private bool _bTerminated;
        int _n1 = 0, _n2 = 0;


        public SInterruptOneThreadINT_INT(dlgv_n_n callback)
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
        public void Set(int n1, int n2)
        {
            _n1 = n1;
            _n2 = n2;
            this.Set();
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
                if (_bEnableTimeout)
                    this.WaitOne(_nTimeout);
                else
                    this.WaitOne();

                this.Reset();

                if (_bTerminated) break;

                if (_callback != null) _callback(_n1, _n2);
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
