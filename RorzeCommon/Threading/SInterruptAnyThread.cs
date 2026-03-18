using RorzeComm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptAnyThread: List<EventWaitHandle>, IDisposable
    {
        private Thread _thread;
        private dlgv_n _callback;
        private bool _bTerminated;
        public SInterruptAnyThread(dlgv_n callback, int nNumOfSignal )
            :base(nNumOfSignal)
        {
            if (nNumOfSignal > 64) 
                throw new NotSupportedException("Number of signal should be less (or equal) 64.");
            for (int nIdx = 0; nIdx < nNumOfSignal; nIdx++)
                this.Add(new EventWaitHandle(false, EventResetMode.ManualReset));
            _callback = callback;
            _bTerminated = false;
            _thread = new Thread(new ThreadStart(Execute));
            _thread.IsBackground = true;
            _thread.Start();
         
        }
        public SInterruptAnyThread(dlgv_n callback, params EventWaitHandle[] signals)
            :base(signals)
        {
            _callback = callback;
            _bTerminated = false;
            _thread = new Thread(new ThreadStart(Execute));
            _thread.IsBackground = true;
            _thread.Start();            
        }

        private void Execute()
        {
            while (true)
            {
                int nEvtID = EventWaitHandle.WaitAny(this.ToArray());
                if (_bTerminated) break;
                if (_callback != null) _callback(nEvtID);
                this[nEvtID].Reset();
            }
        }
        public void Dispose()
        {
            _bTerminated = true;
            this[0].Set();
        }
    }
}
