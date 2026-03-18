using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RorzeUnit.Class;

namespace RorzeUnit.Class.ADAM.Event
{
    //==============================================================================
    public delegate void IOAdam6066EventHandler(object sender, IOAdam6066DataEventArgs e);
    public class IOAdam6066DataEventArgs : EventArgs
    {
        public bool[] Input;
        public bool[] Output;
        public IOAdam6066DataEventArgs(bool[] strOutput, bool[] strInput)
        {
            Input = strInput;
            Output = strOutput;            
        }
    }
}
