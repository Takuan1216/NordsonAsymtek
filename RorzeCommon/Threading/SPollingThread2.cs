using RorzeComm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RorzeComm/*.Threading*/
{
    public class SPollingThread2 : EventWaitHandle
    {
        public event dlgv_v DoPolling;
        private Task _task;
        private int _nDueTime;
        public SPollingThread2(int dueTime)
            : base(false, EventResetMode.ManualReset)
        {
            _nDueTime = dueTime;
            _task = new Task(Execute, TaskCreationOptions.AttachedToParent);
            _task.Start();
        }
        private void Execute()
        {
            while (true)
            {
                this.WaitOne();
                if (DoPolling != null) DoPolling();
                if (_nDueTime > 0)
                    Thread.Sleep(_nDueTime);
            }
        }
    }
}
