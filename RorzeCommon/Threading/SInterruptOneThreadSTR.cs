using RorzeComm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptOneThreadSTR : EventWaitHandle, IDisposable
    {

        private Thread _thread;
        private int _nTimeout;
        private bool _bEnableTimeout;
        private dlgv_s _callback;
        private bool _bTerminated;
        private string _Data;
        private ConcurrentQueue<string> STRdatabuffer;
        public SInterruptOneThreadSTR(dlgv_s callback) : base(false, EventResetMode.ManualReset)
        {
            _bTerminated = false;
            STRdatabuffer = new ConcurrentQueue<string>();
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
        public void Set(string Data)
        {
            //_Data = Data;
            STRdatabuffer.Enqueue(Data);
            this.Set();
        }
        private void Execute()
        {
            while (true)
            {
                if (_bEnableTimeout) this.WaitOne(_nTimeout);

                else if (STRdatabuffer.Count <= 0) this.WaitOne();

                this.Reset();

                STRdatabuffer.TryDequeue(out _Data);

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
