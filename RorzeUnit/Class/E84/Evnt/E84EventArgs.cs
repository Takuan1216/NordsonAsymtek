using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.E84.Event
{
    public class E84ModeChangeEventArgs : EventArgs
    {
        public bool Auto { get; set; }
        public E84ModeChangeEventArgs(bool IsAuto)
        {
            Auto = IsAuto;
        }
    }
    public delegate void E84ModeChangeEventHandler(object sender, E84ModeChangeEventArgs e);


    public class E84EventArgs : EventArgs
    {
        public int _UnitNo { get; set; }
        public E84EventArgs(int unitNo)
        {
            _UnitNo = unitNo;
        }
    }
    public delegate void E84EventHandler(object sender, E84EventArgs e);

    public class E84ProcessEventArgs : EventArgs
    {
        public bool _ProcessOnOff { get; set; }
        public E84ProcessEventArgs(bool OnOff)
        {
            _ProcessOnOff = OnOff;
        }
    }
    public delegate void E84ProcessEventHandler(object sender, E84ProcessEventArgs e);


    public class E84WarningEventArgs : EventArgs
    {
        public int StageNo { get; set; }
        public int WarningCode { get; set; }
        public E84WarningEventArgs(int No, int Code)
        {
            WarningCode = Code;
            StageNo = No;
        }
    }
    public delegate void E84WarningEventHandler(object sender, E84WarningEventArgs e);

    public class E84SignalErrorEventArgs : EventArgs
    {
        public string _strSignalError { get; set; }
        public bool _bSignalErrorOnOff { get; set; }
        public E84SignalErrorEventArgs(string str, bool OnOff)
        {
            _strSignalError = str;
            _bSignalErrorOnOff = OnOff;
        }
    }
    public delegate void E84SignalErrorEventHandler(object sender, E84SignalErrorEventArgs e);




}
