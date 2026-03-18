using RorzeComm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
   public class SInterruptOneThreadSTRList : EventWaitHandle, IDisposable
    {
        private Thread _thread;
        private int _nTimeout;
        private bool _bEnableTimeout;
        private dlgv_slist _callback;
        private bool _bTerminated;
        private string _Data;
        private string[] _DataList;
        List<string> datavalue;
        private ConcurrentQueue<List<string>> STRLISTdatabuffer;
        public SInterruptOneThreadSTRList(dlgv_slist callback)
            : base(false, EventResetMode.ManualReset)
        {
            _bTerminated = false;
            STRLISTdatabuffer = new ConcurrentQueue<List<string>>();
            datavalue = new List<string>();
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
        public void Set(string Data,params string[] DataList)
        {
            // _Data = Data;
            List<string> DataL = new List<string>();
            // _DataList = DataList;
            DataL.Add(Data);
            for(int i=0;i< DataList.Count();i++)
             DataL.Add(DataList[i]);
            STRLISTdatabuffer.Enqueue(DataL);

            this.Set();
        }
        private void Execute()
        {
            while (true)
            {
                if (_bEnableTimeout) this.WaitOne(_nTimeout);

                else if (STRLISTdatabuffer.Count <= 0) this.WaitOne();

                this.Reset();

                if (STRLISTdatabuffer.TryDequeue(out datavalue))
                {
                    _Data = datavalue[0];
                    datavalue.RemoveAt(0);
                    _DataList = datavalue.ToArray();
                }
                if (_bTerminated) break;

                if (_callback != null) _callback(_Data, _DataList);
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
