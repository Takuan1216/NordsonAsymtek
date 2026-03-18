using RorzeComm.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace RorzeUnit.Class
{
    class SSignalManager
    {
        private List<SSignal> _signals;
        private SInterruptOneThread[] _threads = new SInterruptOneThread[] { };
        public SSignalManager()
        {
            _signals = new List<SSignal>();
        }

        public void Add(SSignal signal)
        {
            _signals.Add(signal);
        }

        

    }
}
