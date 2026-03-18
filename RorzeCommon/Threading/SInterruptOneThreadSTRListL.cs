using RorzeComm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptOneThreadSTRListL : EventWaitHandle, IDisposable
    {
        private Thread _thread;
        private int _nTimeout;
        private bool _bEnableTimeout;
        private dlgv_slistL _callback;
        private bool _bTerminated;
        private string _Data;
        private List<string>[] _Datalist;
        public SInterruptOneThreadSTRListL(dlgv_slistL callback)
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
        public void Set(string Data, params List<string>[] Datalist)
        {
            _Data = Data;
            _Datalist = Datalist;
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

                if (_callback != null) _callback(_Data, _Datalist);
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
