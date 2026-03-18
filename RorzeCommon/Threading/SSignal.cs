using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace RorzeComm.Threading
{
    public class SSignal : EventWaitHandle
    {
        public bool bAbnormalTerminal { get; set; }
        public SSignal(bool initialState, EventResetMode mode) : base(initialState, mode)
        {
            bAbnormalTerminal = false;
        }

        public override bool WaitOne()
        {
            bAbnormalTerminal = false;
            return base.WaitOne();
        }
        public override bool WaitOne(int millisecondsTimeout)
        {
            bAbnormalTerminal = false;
            return base.WaitOne(millisecondsTimeout);
        }
        public override bool WaitOne(int millisecondsTimeout, bool exitContext)
        {
            bAbnormalTerminal = false;
            return base.WaitOne(millisecondsTimeout, exitContext);
        }
        public override bool WaitOne(TimeSpan timeout)
        {
            bAbnormalTerminal = false;
            return base.WaitOne(timeout);
        }
        public override bool WaitOne(TimeSpan timeout, bool exitContext)
        {
            bAbnormalTerminal = false;
            return base.WaitOne(timeout, exitContext);
        }

        public static int WaitAny(int nTimeout, params SSignal[] signals)
        {
            return EventWaitHandle.WaitAny(signals, nTimeout);
        }
        public static bool WaitAll(int nTimeout, params SSignal[] signals)
        {
            return EventWaitHandle.WaitAll(signals, nTimeout);
        }
    }


}
