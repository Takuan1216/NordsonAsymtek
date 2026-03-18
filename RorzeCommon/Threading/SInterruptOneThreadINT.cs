using RorzeComm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptOneThreadINT : EventWaitHandle, IDisposable
    {
        private Thread _thread;
        private int _nTimeout;
        private bool _bEnableTimeout;
        private dlgv_n _callback;
        private bool _bTerminated;
        private int _Data;
        private ConcurrentQueue<int> intdatabuffer;
        public SInterruptOneThreadINT(dlgv_n callback) : base(false, EventResetMode.ManualReset)
        {
            _bTerminated = false;
            intdatabuffer = new ConcurrentQueue<int>();
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
        public void Set(int Data)
        {
            intdatabuffer.Enqueue(Data);
            this.Set();
        }
        private void Execute()
        {
            while (true)
            {

                if (_bEnableTimeout) this.WaitOne(_nTimeout);

                else if (intdatabuffer.Count <= 0) this.WaitOne();

                this.Reset();

                intdatabuffer.TryDequeue(out _Data);

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
