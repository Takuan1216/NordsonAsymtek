using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptOneThreadobkj_INT : EventWaitHandle, IDisposable
    {
        private Thread _thread;
        private int _nTimeout;
        private bool _bEnableTimeout;
        private dlgv_Object_INT _callback;
        private bool _bTerminated;
        int _Data = 0;
        private object _Value;

        public SInterruptOneThreadobkj_INT(dlgv_Object_INT callback)
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
        public void Set(object Value, int datas)
        {
            _Value = Value;
            _Data = datas;
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
            //try
            //{
            while (true)
            {
                if (_bEnableTimeout)
                    this.WaitOne(_nTimeout);
                else
                    this.WaitOne();

                this.Reset();

                if (_bTerminated) break;

                if (_callback != null) _callback(_Value, _Data);
            }
            //}
            //catch (SException ex)
            //{
            //}
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
