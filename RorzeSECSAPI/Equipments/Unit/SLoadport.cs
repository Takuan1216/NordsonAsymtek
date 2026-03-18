using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Rorze.Equipment.Unit
{
    
    public  class SLoadport
    {
        public enum PortState
        {
            Unknown = 0, Disabled = 1, ReadyToLoad = 2, Arrived = 3, Clamped = 4, Docked = 5, FuncitonSetup = 6, Processing = 7, Completed = 8,
            Stoped = 9, Undocked = 10, Unclamped = 11, ReadyToUnload = 12, Removed = 13, Error = 14, FunctionSetupNG = 15, Docking=16,Undocking=17,Stopping=18,
        }
        public enum E84PortStates
        { OUTOFSERVICE=-1, ReadytoLoad = 1, TransferBlock = 2, ReadytoUnload = 3 }
        public enum E84Mode
        { Manual = 0, Auto }

        int _number;
        PortState _State;
        E84Mode _E84Mode;
        string _unitname;
        SCarrier _Carrier;
        E84PortStates _E84Status;
        public class EFEMPortStatusEventArgs : EventArgs
        {
            public string UnitName;
            public int PortNo;
            public PortState State;
            public E84PortStates E84Status;
            public E84Mode E84Mode;
            public string PJID;
            public string CJID;
            public string CarrierID;
            public EFEMPortStatusEventArgs(string name,int No, PortState states, E84Mode mode,string ID, string PJ="",string CJ="")
            {
                UnitName = name;
                PortNo = No;
                State = states;
                E84Mode = mode;
                E84Status = (states== PortState.ReadyToLoad)? E84PortStates.ReadytoLoad:(states == PortState.ReadyToUnload)? E84PortStates.ReadytoUnload: E84PortStates.TransferBlock;
                PJID = PJ;
                CJID = CJ;
               CarrierID = ID;
            }
        }
        public delegate void EFEMPortStatuHandler(object sender, EFEMPortStatusEventArgs e);
        public event EFEMPortStatuHandler OnEFEMPortStatusChange;
       // public event EFEMPortStatuHandler OnEFEME84ModeChange;
       
        public int GetNumber { get { return _number; } }
        public PortState State {get { return _State; }
            set
            {
                _State = value;
               
                switch (_State)
                {
                    case PortState.Arrived:
                        if(_Carrier != null)
                            _Carrier = null;
                        _Carrier = new SCarrier(_unitname, _number);
                        if (OnEFEMPortStatusChange != null)
                            OnEFEMPortStatusChange(this, new EFEMPortStatusEventArgs(_unitname,_number,_State,_E84Mode,""));
                        //_Carrier.CarrierCreate();
                        break;

                    case PortState.Removed:
                        if (OnEFEMPortStatusChange != null)
                            OnEFEMPortStatusChange(this, new EFEMPortStatusEventArgs(_unitname,_number, _State, _E84Mode,_Carrier.ID));
                        _Carrier = null;
                        break;
                    case PortState.FuncitonSetup:
                    case PortState.Processing:
                    case PortState.Completed:
                    case PortState.Stoped:
                        break;
                    default:
                        _E84Status = (_State == PortState.ReadyToLoad) ? E84PortStates.ReadytoLoad : (_State == PortState.ReadyToUnload) ? E84PortStates.ReadytoUnload : E84PortStates.TransferBlock;
                        if (OnEFEMPortStatusChange != null)
                        {
                            if (_Carrier == null)
                               OnEFEMPortStatusChange(this, new EFEMPortStatusEventArgs(_unitname, _number, _State, _E84Mode,""));
                            else
                               OnEFEMPortStatusChange(this, new EFEMPortStatusEventArgs(_unitname, _number, _State, _E84Mode, _Carrier.ID));
                        }
                        break;

                }
               
            }
        }
        public E84Mode E84ModeStatus
        { get { return _E84Mode; }
          set
          {
                _E84Mode = value;
                {
                    if (_Carrier == null)
                        OnEFEMPortStatusChange(this, new EFEMPortStatusEventArgs(_unitname, _number, _State, _E84Mode, ""));
                    else
                        OnEFEMPortStatusChange(this, new EFEMPortStatusEventArgs(_unitname, _number, _State, _E84Mode, _Carrier.ID));
                }
            }
        }
        public E84PortStates E84PortStatus
        { get { return _E84Status; } }
        public SCarrier Carrier{ get { return _Carrier;} set{_Carrier = value;}}
        public SLoadport(string Name,int No)
        {
            _unitname = Name;
            _number = No;
            _State = PortState.Unknown;
            
        }
        public void JobStateChange(string CJ,string PJ)
        {  
              if (OnEFEMPortStatusChange != null)
                  OnEFEMPortStatusChange(this, new EFEMPortStatusEventArgs(_unitname,_number, _State, _E84Mode, _Carrier.ID, PJ,CJ));

        }
    }
}
