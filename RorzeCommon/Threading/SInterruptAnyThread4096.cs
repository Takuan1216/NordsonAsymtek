using RorzeComm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeComm.Threading
{
    public class SInterruptAnyThread4096 : List<EventWaitHandle>
    {
        //========== variable
        private dlgv_n _callback;

        private Thread[] _athreadSub;
        private EventWaitHandle[][] _asignals;

        //========== constructor
        public SInterruptAnyThread4096(dlgv_n callback, int numberOfSignal)
        {
            _callback = callback;
            _asignals = new EventWaitHandle[(numberOfSignal / 64) + 1][];
            _athreadSub = new Thread[(numberOfSignal / 64) + 1];
            MultiSignalManager signalMgr;

            for (int idx = 0; idx < _asignals.Length; idx++)
            {
                _asignals[idx] = new EventWaitHandle[64];
                for (int addr = 0; addr < _asignals[idx].Length; addr++)
                {
                    _asignals[idx][addr] = new EventWaitHandle(false, EventResetMode.ManualReset);
                    this.Add(_asignals[idx][addr]);
                }
                signalMgr = new MultiSignalManager(idx * 64, _asignals[idx]);
                _athreadSub[idx] = new Thread(new ParameterizedThreadStart(RunSubProcess));
                _athreadSub[idx].IsBackground = true;
                _athreadSub[idx].Start(signalMgr);                
            }
            
        }

        //========== member function
        private void RunSubProcess(object obj)
        {
            MultiSignalManager signals = obj as MultiSignalManager;
            if (signals == null) 
                return;
            while (true)
            {
                try
                {
                    int nEventID = EventWaitHandle.WaitAny(signals.ToArray());
                    signals[nEventID].Reset();
                    if (_callback != null)
                        _callback(signals.Offset + nEventID);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} Exception caught.", ex);
                }
            }
        }

    }

    class MultiSignalManager: List<EventWaitHandle>
    {
        public int Offset { get; set; }
        public MultiSignalManager(int offsetAddr, params EventWaitHandle[] args)
            :base(args)
        {
            Offset = offsetAddr;
        }
    }
}
