using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RorzeUnit.Class;

namespace RorzeUnit.Event
{
    public delegate void WaferDataEventHandler(object sender, WaferDataEventArgs e);
    public class WaferDataEventArgs : EventArgs
    {
        public int Slot;
        public SWafer Wafer { get; set; }
        public WaferDataEventArgs(SWafer wafer, int slot = 1)
        {
            Wafer = wafer;
            Slot = slot;
        }
    }

    public delegate void OccurErrorEventHandler(object sender, OccurErrorEventArgs e);
    public class OccurErrorEventArgs : EventArgs
    {

        public int ErrorCode { get; set; }

        public OccurErrorEventArgs(int Code)
        {
            ErrorCode = Code;
        }
    }


}
