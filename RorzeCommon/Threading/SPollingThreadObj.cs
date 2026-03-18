using RorzeComm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RorzeComm.Threading
{
   public  class SPollingThreadObj : EventWaitHandle
    {
        //private Task _task;
        private Thread _thread;
        private bool _bTerminated;
        private bool _bClosed;
        private int _nDueTime;
        public event dlgv_Object DoPolling;
        private object ExeObject;
        public SPollingThreadObj(int dueTime)
            : base(false, EventResetMode.ManualReset)
        {
            _bTerminated = false;
            _bClosed = false;
            _nDueTime = dueTime;
            _thread = new Thread(Execute);
            _thread.IsBackground = true;
            _thread.Start();
            //_task = new Task(Execute, TaskCreationOptions.AttachedToParent);
            //_task.Start();
        }
        ~SPollingThreadObj()
        {
            Close();
        }

        private void Execute()
        {
            while (true)
            {
                if (_bTerminated) break;
                this.WaitOne();
                if (_bTerminated) break;
                if (DoPolling != null) DoPolling(ExeObject);

                if (_nDueTime > 0)
                    Thread.Sleep(_nDueTime);

            }

            Console.WriteLine("Polling thread be terminal.");
        }

        public int nDueTime
        {
            set { _nDueTime = value; }
            get { return _nDueTime; }
        }

        public new void Close()
        {
            _bTerminated = true;
            if (!_bClosed) this.Set();
            _bClosed = true;
        }
        protected override void Dispose(bool explicitDisposing)
        {
            Close();
            base.Dispose(explicitDisposing);
        }
        public void Set(object Data)
        {
            ExeObject = Data;
            this.Set();
        }
    }
}
