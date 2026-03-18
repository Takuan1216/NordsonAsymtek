using System;
using System.Collections.Generic;
using System.Text;
using Rorze.SocketObject;

using Rorze.Equipment.Unit;
using System.Linq;
using System.Threading;
using Rorze.Equipments.Unit;
using RorzeComm;
using RorzeComm.Threading;
using RorzeComm.Log;
using RorzeUnit.Class;

namespace Rorze.Equipments
{
  public abstract class SEFEMType: SEFEM
  {
        public enum EFEMTypeCommand
        {
            RecipeNameList,
            Clamp,
            Dock,
            SetWaferInfor,
            WaferSelect,
            WaferStart,
            UnDock,
            UnClamp,
            RecipeBody,
            SetRecipeBody,
            RecipeDelete,
            SetE84Mode,
            HostMsg,
            
            Max,
        }
        public enum EFEMTypeEvent
        {
            Unkown = -1,
            PortState,
            WfTransState,
            AlarmStatus,
            E84Mode,
            Init,
        }

        Dictionary<EFEMTypeEvent, string> _dicEventTable = new Dictionary<EFEMTypeEvent, string>()
        {
            {EFEMTypeEvent.AlarmStatus,"AlarmStatus" },
            {EFEMTypeEvent.PortState,"PortState" },
            {EFEMTypeEvent.E84Mode,"E84Mode" },
            {EFEMTypeEvent.WfTransState,"WfTransState" },
             {EFEMTypeEvent.Init,"Init" }
        };


        Dictionary<EFEMTypeCommand, string> _dicCmdsTable = new Dictionary<EFEMTypeCommand, string>()
        {
            {EFEMTypeCommand.RecipeNameList,"RecipeNameList"},
            {EFEMTypeCommand.Clamp,"Clamp"},
            {EFEMTypeCommand.Dock,"Dock"},
            {EFEMTypeCommand.SetWaferInfor,"SetWaferInfor"},
            {EFEMTypeCommand.WaferSelect,"WfSelect"},
            {EFEMTypeCommand.WaferStart,"WfStart"},
            {EFEMTypeCommand.UnClamp,"UnClamp" },
            {EFEMTypeCommand.UnDock,"UnDock" },
            {EFEMTypeCommand.RecipeBody,"RecipeBody" },
            {EFEMTypeCommand.SetRecipeBody,"SetRecipeBody" },
            {EFEMTypeCommand.RecipeDelete,"RecipeDelete" },
            {EFEMTypeCommand.SetE84Mode,"SetE84Mode" },
            {EFEMTypeCommand.HostMsg,"ShowMsg"}
        };
        public enum ArmSelect {UpArm=1, LowArm }

        Dictionary<string, SSignal> _signalAck = new Dictionary<string, SSignal>();
        Dictionary<int, SLoadport> _loadportlist;
        public Dictionary<ArmSelect, SMaterial> _robotunit;
        Dictionary<int, SUnitModel> _Alinger;
        public SRobot _robot;

        // event
        public event EFEMAlarmhappenMsgHandler OnAlarmHappen;
        public event EFEMWaferTranferEventHandler OnWaferTranfer;
        public event EventHandler OnEFENINIT;

        public Dictionary<int, SLoadport> GetPort { get { return _loadportlist; } set { _loadportlist = value; } }
        SInterruptOneThreadINT _exeAutoClamp;
        Dictionary<int, string> _unitposition;
        SLogger _logger;
        public SEFEMType(string name,SocketControl control,int LoadportMax,int RobotArmMax,int AlignerMax,Dictionary<int,string> unitPos) :
            base(name, control)
        {
            _logger = new SLogger(name+"_Debug");
            _loadportlist = new Dictionary<int, SLoadport>();
            _unitposition = new Dictionary<int, string>();
            _unitposition = unitPos;
            _robotunit = new Dictionary<ArmSelect, SMaterial>();
            _Alinger = new Dictionary<int, SUnitModel>();
            for (int i = 0; i < RobotArmMax; i++)
                _robotunit.Add((ArmSelect)i, null);
            for (int i = 0; i < AlignerMax; i++)
                _Alinger.Add(i + 1, new SUnitModel(string.Format("Alinger{0}",i+1)));

            for (int i = 0; i < LoadportMax; i++)
                _loadportlist.Add(i + 1, new SLoadport(name, i + 1));
            for (int nCnt = 0; nCnt < (int)EFEMTypeCommand.Max; nCnt++)
                _signalAck.Add(_dicCmdsTable[(EFEMTypeCommand)nCnt], new SSignal(false, EventResetMode.ManualReset));

            _exeAutoClamp = new SInterruptOneThreadINT(AutoClamp);
            _robot = new SRobot();

        }

      

        // Threading
        void AutoClamp(int PortID)
        {
            try
            {
                Clamp(PortID);
            }
            catch (SException ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        // CMD
        public virtual void Clamp(int PortID)
        {
            SendOrder(_dicCmdsTable[EFEMTypeCommand.Clamp], PortID.ToString());
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.Clamp]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.Clamp]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.Clamp]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.Clamp]));
        }
        public virtual void UnClamp(int PortID)
        {
            SendOrder(_dicCmdsTable[EFEMTypeCommand.UnClamp], PortID.ToString());
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.UnClamp]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.UnClamp]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.UnClamp]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.UnClamp]));
        }
        public virtual void Dock(int PortID, string CarrierID = "")
        {
            if(CarrierID !="")
             SendOrder(_dicCmdsTable[EFEMTypeCommand.Dock], PortID.ToString(), CarrierID);
            else
             SendOrder(_dicCmdsTable[EFEMTypeCommand.Dock], PortID.ToString());
            if (CarrierID!="")
                _loadportlist[PortID].Carrier.AssignCarrierIDToHost(CarrierID); // Host Carrier ID
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.Dock]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.Dock]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.Dock]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.Dock]));
        }
        public virtual void UnDock(int PortID)
        {
            SendOrder(_dicCmdsTable[EFEMTypeCommand.UnDock], PortID.ToString());
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.UnDock]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.UnDock]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.UnDock]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.UnDock]));
        }
        public virtual void SetE84Mode(int PortID, int Mode)
        {
            SendOrder(_dicCmdsTable[EFEMTypeCommand.SetE84Mode], PortID.ToString(),Mode.ToString());
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.SetE84Mode]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.SetE84Mode]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.SetE84Mode]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.SetE84Mode]));
        }
        public virtual void QueryRecipeList()
        {
            SendOrder(_dicCmdsTable[EFEMTypeCommand.RecipeNameList]);
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.RecipeNameList]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.RecipeNameList]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.RecipeNameList]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.RecipeNameList]));
        }
        public virtual void QueryRecipebody(string Name)
        {
            SendOrder(_dicCmdsTable[EFEMTypeCommand.RecipeBody], Name);
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.RecipeBody]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.RecipeBody]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.RecipeBody]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.RecipeBody]));
        }
        public virtual void SetWaferInfor(int PortID,List<string> WaferInfo)
        {
            string Info = ParseWaferInfo(WaferInfo);
            SendOrder(_dicCmdsTable[EFEMTypeCommand.SetWaferInfor], PortID.ToString(), Info);
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.SetWaferInfor]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.SetWaferInfor]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.SetWaferInfor]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.SetWaferInfor]));
        }
        public virtual void WaferSelect(int portID, string CJID, string PJID, string slotmap, string ProcessMode, string RecipeName, bool UseRead)
        {
            string UseReadCMD = UseRead ? "1" : "0";

            SendOrder(_dicCmdsTable[EFEMTypeCommand.WaferSelect], portID.ToString(), CJID, PJID, slotmap, ProcessMode, RecipeName, UseReadCMD);
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.WaferSelect]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.WaferSelect]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.WaferSelect]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.WaferSelect]));
        }
        public virtual void WaferStart(int portID, string CJID, string PJID)
        {
            SendOrder(_dicCmdsTable[EFEMTypeCommand.WaferStart], portID.ToString(), CJID, PJID);
            if (!_signalAck[_dicCmdsTable[EFEMTypeCommand.WaferStart]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMTypeCommand.WaferStart]));
            if (_signalAck[_dicCmdsTable[EFEMTypeCommand.WaferStart]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMTypeCommand.WaferStart]));
        }
        //public void GoOnline()
        //{
        //    this.SendCMDGoOnline();
        //}
        //public void GoLocal()
        //{
        //    this.SendCMDGoLocal();
        //}
        //public void GoRemote()
        //{
        //    this.SendCMDGoRemote();
        //}

        // Check Unit message Formate , can override this Function...  
        public override bool CheckEventexist(string Msg)
        {
           
            if (base.CheckEventexist(Msg))
                return true;
            // Override Function 
            foreach (string scmd in _dicEventTable.Values) //查字典
            {
                if (Msg.Contains(string.Format("{0}", scmd)))
                    return true;         
            }

            return false;
        }
        public override bool CheckCmdexist(string Msg)
        {
            if (base.CheckCmdexist(Msg))
                return true;
            // Override Function 
            foreach (string scmd in _dicCmdsTable.Values) //查字典
            {
                if (Msg.Contains(string.Format("{0}", scmd)))
                    return true;
                
            }
            return false;
        }

        // Parse unit message , can override this Function...  
        public override bool ParseEventMsg(EFEMFrame frame)
        {
          
            if (base.ParseEventMsg(frame))
                return true;
            // Override Function 
            if (!_dicEventTable.ContainsValue(frame.Command))
                return false;
            EFEMTypeEvent Event = EFEMTypeEvent.Unkown;
            Event = _dicEventTable.FirstOrDefault(x => x.Value == frame.Command).Key;
            switch (Event)
            {
                case EFEMTypeEvent.PortState:
                    int portNo = Convert.ToInt32(frame.Parameter.Split(',')[0]);
                    if (!_loadportlist.ContainsKey(portNo))
                        return false;
                    _loadportlist[portNo].State = (SLoadport.PortState)Convert.ToInt32(frame.Parameter.Split(',')[1]);
                    _logger.WriteLog(string.Format("Port{0} stats = {1}", portNo, frame.Parameter.Split(',')[1]));
                    switch (_loadportlist[portNo].State)
                    {
                        case SLoadport.PortState.Arrived:
                            string carrierID = "";
                            if (frame.Parameter.Split(',').Count() > 2)
                                carrierID = frame.Parameter.Split(',')[2];
                           // _loadportlist[portNo].Carrier.AssignCarrierID(carrierID);

                            _exeAutoClamp.Set(portNo); // Auto Clamp 
                            break;

                        case SLoadport.PortState.Docked:
                            string MappingData = "";
                            if (frame.Parameter.Split(',').Count() > 2)
                                MappingData = frame.Parameter.Split(',')[2];
                            //_loadportlist[portNo].Carrier.CreateMaterialObject(MappingData);
                            break;

                        case SLoadport.PortState.Removed:
                        case SLoadport.PortState.ReadyToLoad:
                            _loadportlist[portNo].Carrier = null;
                            break;
                        case SLoadport.PortState.FuncitonSetup:
                        case SLoadport.PortState.Completed:
                        case SLoadport.PortState.Processing:
                        case SLoadport.PortState.Stoped:
                            string CJID = frame.Parameter.Split(',')[2];
                            string PJID = frame.Parameter.Split(',')[3];
                            _loadportlist[portNo].JobStateChange(CJID, PJID);
                            if (_loadportlist[portNo].State == SLoadport.PortState.Completed)
                                _robot.CurrentPos = SRobot.RobotPos.Home;
                            break;

                    }

                    return true;
                case EFEMTypeEvent.AlarmStatus:
                    int AlarmID = Convert.ToInt32(frame.Parameter.Split(',')[0]);
                    bool AlarmSet = (frame.Parameter.Split(',')[1] == "1") ? true : false;
                    if (OnAlarmHappen != null)
                        OnAlarmHappen(this, new EFEMAlarmhappenEventArgs(AlarmID, AlarmSet));
                    return true;
                case EFEMTypeEvent.E84Mode:
                    int port = Convert.ToInt16(frame.Parameter.Split(',')[0]);
                    _loadportlist[port].E84ModeStatus = (SLoadport.E84Mode)Convert.ToInt16(frame.Parameter.Split(',')[1]);
                    return true;
                case EFEMTypeEvent.WfTransState:
                    string WaferID = "";
                    
                    bool IsRobotTake = (Convert.ToInt16(frame.Parameter.Split(',')[0]) == 0) ? true : false;
                    int UnitPosNo =Convert.ToInt16(frame.Parameter.Split(',')[1]);
                   
                    int slot = Convert.ToInt16(frame.Parameter.Split(',')[2]);
                   // if(frame.Parameter.Split(',').Count()==4)
                    WaferID = frame.Parameter.Split(',')[3];
                    int ArmNo = Convert.ToInt16(frame.Parameter.Split(',')[4]);
                    if (OnWaferTranfer != null)
                        OnWaferTranfer(this, new EFEMWaferTranferEventArgs(IsRobotTake, UnitPosNo, WaferID, slot, ArmNo));
                    return true;
                case EFEMTypeEvent.Init:
                    if (OnEFENINIT != null)
                        OnEFENINIT(this, new EventArgs());
                    break;
            }
            return false;
        }
        public override bool ParseACKMsg(EFEMFrame frame)
        {
           
            if (base.ParseACKMsg(frame))
                return true;
            // Override Function 

            return false;
        }
        public override bool ParseCmdMsg(EFEMFrame frame)
        {
           
            if (base.ParseCmdMsg(frame))
                return true;
            // Override Function 
            if (!_dicCmdsTable.ContainsValue(frame.Command))
                return false;
            EFEMTypeCommand cmd = EFEMTypeCommand.Max;
            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == frame.Command).Key;
            switch (cmd)
            {
                case EFEMTypeCommand.RecipeBody:

                    _signalAck[_dicCmdsTable[cmd]].Set();
                    return true;
                case EFEMTypeCommand.RecipeNameList:

                    _signalAck[_dicCmdsTable[cmd]].Set();
                    return true;
                
                default:
                    if (frame.Parameter.Split(',')[0] != "0")
                        _signalAck[_dicCmdsTable[cmd]].bAbnormalTerminal = true;
                    _signalAck[_dicCmdsTable[cmd]].Set();
                    return true;

            }
        }

        // other
        string ParseWaferInfo(List<string> Info)
        {
            string Parm = "";
            for (int i = 0; i < Info.Count; i++)
            {
                if(i==0)
                 Parm = Info[i];
                else
                 Parm = Parm + "," + Info[i];
            }
            return Parm;
        }
    }
}
