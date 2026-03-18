using System;
using System.Collections.Generic;
using System.Text;
using RorzeAPI.Equipments;
using Rorze.Equipments;
using Rorze.SocketObject;

using System.Threading;
using System.Linq;
using Rorze.Equipment.Unit;
using RorzeComm;
using Rorze.Equipments.Unit;
using RorzeComm.Log;
using RorzeComm.Threading;

namespace RorzeAPI.Equipments.Combination
{
   public class SScreen : SEFEM
    {
        public event EFEMAlarmhappenMsgHandler OnAlarmHappen;
        public event EFEMRecipeChangeHandler OnRecipeChange;
        public event EFEMVIDUpdateHandler OnVIDUpdate;
        public event ScreenProcessHandler OnProcessChange;
        public event EventHandler<SMaterial> OnAssingTakePanel;
        public event PanelEQTransferHandler OnPanelTransferEQ;
        public enum ScreenEvent
        {
            Unkown = -1,
            RecipeChange ,
            AlarmStatus,
            SendData,

        }
        public enum ScreenCommand
        {
            Unkown = -1,
            SetDateTime,
            SetTerminalMsg,
            SetPanelInfo,
            RecipeNameList,
            RecipeBody,
            GetControlState,
            GetAlarms,

            Max,
        }
        public enum DataType
        {
            Unkown,
            ECID,
            FDC,
            ProcessStartData,
            ProcessendData,
            Location,
        }

        public Dictionary<ScreenEvent, string> _dicEventTable = new Dictionary<ScreenEvent, string>()
        {
            {ScreenEvent.AlarmStatus,"AlarmStatus" },
            {ScreenEvent.RecipeChange,"RecipeChange" },
            {ScreenEvent.SendData,"SendData" },

        };


       public Dictionary<ScreenCommand, string> _dicCmdsTable = new Dictionary<ScreenCommand, string>()
        {
             {ScreenCommand.SetDateTime,"SetDateTime" },
             {ScreenCommand.GetAlarms,"Alarms" },
             {ScreenCommand.GetControlState,"ControlState" },
             {ScreenCommand.RecipeBody,"RecipeBody"},
             {ScreenCommand.RecipeNameList,"RecipeNameList" },
             {ScreenCommand.SetPanelInfo,"SetPanelInfo" },
             {ScreenCommand.SetTerminalMsg,"SetTerminalMsg" }
        };

        public Dictionary<DataType, string> _dicSendDataTable = new Dictionary<DataType, string>()
        {
            {DataType.ECID,"ECID" },
            {DataType.FDC,"FDC" },
            {DataType.Location,"Location" },
            {DataType.ProcessendData,"ProcessEndData" },
            {DataType.ProcessStartData,"ProcessStartData" }
        };

        SEQType _eqobject;
        SLogger _logger;
        
        public SEQType EQObject { set { _eqobject = value; } get { return _eqobject; } }
        Dictionary<string, SSignal> _signalAck = new Dictionary<string, SSignal>();
        public SScreen(string UnitName,SocketControl Control,int ChamberCount,Dictionary<int,string> ChamberSpacelist,bool Logopen = false) 
            :base(UnitName, Control)
        {
            _eqobject = new SEQType("Screen", ChamberCount, ChamberSpacelist); // Create EQ object

            if (Logopen)
                _logger = new SLogger("Screen_Debug");

            for (int nCnt = 0; nCnt < (int)ScreenCommand.Max; nCnt++)
                _signalAck.Add(_dicCmdsTable[(ScreenCommand)nCnt], new SSignal(false, EventResetMode.ManualReset));



        }

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
        public override bool ParseEventMsg(EFEMFrame frame)
        {

            if (base.ParseEventMsg(frame))
                return true;
            // Override Function 
           // if (!_dicEventTable.ContainsValue(frame.Command))
           if(_dicEventTable.Where(x=>x.Value == frame.Command).Count()<=0)
                return false;

            ScreenEvent Event = ScreenEvent.Unkown;
            Event = _dicEventTable.FirstOrDefault(x => x.Value == frame.Command).Key;
            switch (Event)
            {
                case ScreenEvent.AlarmStatus:
                    int AlarmID =Convert.ToInt32(frame.Parameter.Split(',')[0]);
                    bool IsSet = (frame.Parameter.Split(',')[1] == "1") ? true : false;
                   // _eqobject.ChamberList[0].Stats = Chamber.ChamberStats.Error;
                  //  _eqobject.ChamberList[1].Stats = Chamber.ChamberStats.Error;
                    if (OnAlarmHappen != null)
                        OnAlarmHappen(this, new EFEMAlarmhappenEventArgs(AlarmID, IsSet));
                    break;

                case ScreenEvent.RecipeChange:
                    if (OnRecipeChange != null)
                        OnRecipeChange(this, new EFEMRecipeChangeEventArgs(frame.Parameter));
                    break;
                case ScreenEvent.SendData:
                    DataType SendDataType = DataType.Unkown;
                    List<string> ValueData = new List<string>();
                    SendDataType = _dicSendDataTable.FirstOrDefault(x => x.Value == frame.Parameter.Split(',')[0]).Key;
                    if(SendDataType != DataType.Unkown)
                    {
                        ValueData = frame.Parameter.Split(',').ToList();
                        ValueData.RemoveAt(0); // 
                    }
                    switch (SendDataType)
                    {
                        case DataType.ECID:
                        case DataType.FDC:                  
                            if (OnVIDUpdate !=null)
                                OnVIDUpdate(this, new EFEMVIDUpdateEventArgs(SendDataType,ValueData));
                            break;
                        case DataType.Location:
                          for(int i= ValueData.Count; i>0;i-- )
                                UpdateOvenUnit(new UnitDataFrame(ValueData[i-1]));
                            break;
                        case DataType.ProcessendData:
                            // event Wafer end 
                            string ProcessEndData = string.Empty;
                            foreach (string datavid in ValueData)
                                ProcessEndData = ProcessEndData + datavid+",";

                            

                            if (OnProcessChange != null)
                                OnProcessChange(this, new ProcessEventArgs(false, new UnitDataFrame(ProcessEndData)));
                            break;

                        case DataType.ProcessStartData:
                            // event Wafer Start 
                            if (OnProcessChange != null)
                                OnProcessChange(this, new ProcessEventArgs(true, new UnitDataFrame(ValueData[0])));
                            break;
                        default:

                            break;
                    }
                    break;
                default:

                    break;

            }



            return false;
        }
        public override bool ParseCmdMsg(EFEMFrame frame)
        {
            bool Ack = false;

            if (base.ParseCmdMsg(frame))
                return true;
            // Override Function 
            //if (!_dicCmdsTable.ContainsValue(frame.Command))
             if (_dicCmdsTable.Where(x => x.Value == frame.Command).Count() <= 0)
                  return false;

            ScreenCommand CMD = ScreenCommand.Unkown;
            CMD = _dicCmdsTable.FirstOrDefault(x => x.Value == frame.Command).Key;
            switch (CMD)
            {
                case ScreenCommand.GetAlarms:
                    Ack = (frame.Parameter.Split(',')[0] == "0") ? true : false;
                    List<string> AlarmMessage = new List<string>();
                    AlarmMessage = frame.Parameter.Split(',').ToList();
                    if (Ack)
                    {
                        if (AlarmMessage.Count() > 2)
                        {
                            if (_eqobject.ChamberList[0].Stats != Chamber.ChamberStats.Error)
                                _eqobject.ChamberList[0].Stats = Chamber.ChamberStats.Error;

                            AlarmMessage.RemoveAt(0);

                            foreach(string AlarmID in AlarmMessage)
                            {
                                if (OnAlarmHappen != null)
                                    OnAlarmHappen(this, new EFEMAlarmhappenEventArgs(Convert.ToInt32( AlarmID), true));
                            }
                        }
                        else
                        {
                            // GetAlarm Error...
                           
                            AlarmMessage.RemoveAt(0);
                            string ErrorMsg = AlarmMessage[0];
                            _signalAck[frame.Command].bAbnormalTerminal = true;
                        }
                    }
                    break;
                case ScreenCommand.GetControlState:
                    int GetState=-1;
                    Ack = (frame.Parameter.Split(',')[0] == "0") ? true : false;
                    if (Ack)
                    {
                        int.TryParse(frame.Parameter.Split(',')[1], out GetState);
                        SetControlState = (EFEMControlState)Convert.ToInt32(GetState);
                    }
                    else
                    {
                        string ErrorMsg = frame.Parameter.Split(',')[1];
                        _signalAck[frame.Command].bAbnormalTerminal = true;
                    }
                    break;
                case ScreenCommand.RecipeBody:
                    // check Send Screen data
                    Ack = (frame.Parameter.Split(',')[0] == "0") ? true : false;
                    if(Ack)
                    {
                        _eqobject.RecipeObject.ReccipBody = frame.Parameter.Split(',').ToList();
                        _eqobject.RecipeObject.ReccipBody.RemoveAt(0);

                    }
                    else
                    {

                    }
                    break;
                case ScreenCommand.RecipeNameList:
                    Ack = (frame.Parameter.Split(',')[0] == "0") ? true : false;
                    if(Ack)
                    {
                        _eqobject.RecipeObject.RecpieList = frame.Parameter.Split(',').ToList();
                        _eqobject.RecipeObject.RecpieList.RemoveAt(0);
                    }
                    else
                    {
                        string ErrorMsg = frame.Parameter.Split(',')[1];
                        _signalAck[frame.Command].bAbnormalTerminal = true;
                    }
                    break;
                case ScreenCommand.SetDateTime:
                case ScreenCommand.SetPanelInfo:
                case ScreenCommand.SetTerminalMsg:
                    Ack = (frame.Parameter.Split(',')[0] == "0") ? true : false;
                    if (!Ack)
                    {
                        string ErrorMsg;
                        if (frame.Parameter.Split(',').Count()>2)
                            ErrorMsg = frame.Parameter.Split(',')[1];
                        _signalAck[frame.Command].bAbnormalTerminal = true;
                    }
                    break;
                  
                default:


                    return false;

            }
            _signalAck[frame.Command].Set();
            return true;
        }

        // Screen CMD
       public void SendRecipeNameList()
        {
            _signalAck[_dicCmdsTable[ScreenCommand.RecipeNameList]].Reset();
            this.SendOrder(_dicCmdsTable[ScreenCommand.RecipeNameList]);
            if (!_signalAck[_dicCmdsTable[ScreenCommand.RecipeNameList]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[ScreenCommand.RecipeNameList]));
            if (_signalAck[_dicCmdsTable[ScreenCommand.RecipeNameList]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[ScreenCommand.RecipeNameList]));
        }
       public void SendRecipeBody(string RecipeID)
        {
            _signalAck[_dicCmdsTable[ScreenCommand.RecipeBody]].Reset();
            this.SendOrder(_dicCmdsTable[ScreenCommand.RecipeBody],RecipeID);
            if (!_signalAck[_dicCmdsTable[ScreenCommand.RecipeBody]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[ScreenCommand.RecipeBody]));
            if (_signalAck[_dicCmdsTable[ScreenCommand.RecipeBody]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[ScreenCommand.RecipeBody]));
        }
       public void SendSetPanellnfo(string LotID,string CarrierID,int SlotID,string PanelID,string RecipeID,bool IsLotEnd)
        {
            _signalAck[_dicCmdsTable[ScreenCommand.SetPanelInfo]].Reset();
            string strlotend = (IsLotEnd) ? "1" : "0";
            this.SendOrder(_dicCmdsTable[ScreenCommand.SetPanelInfo], LotID, CarrierID, SlotID.ToString(), PanelID, RecipeID, strlotend);
            if (!_signalAck[_dicCmdsTable[ScreenCommand.SetPanelInfo]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[ScreenCommand.SetPanelInfo]));
            if (_signalAck[_dicCmdsTable[ScreenCommand.SetPanelInfo]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[ScreenCommand.SetPanelInfo]));
        }
       public void SendSetDateTime(string StrdateTime)
        {
            _signalAck[_dicCmdsTable[ScreenCommand.SetDateTime]].Reset();
            
            this.SendOrder(_dicCmdsTable[ScreenCommand.SetDateTime], StrdateTime);
            if (!_signalAck[_dicCmdsTable[ScreenCommand.SetDateTime]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[ScreenCommand.SetDateTime]));
            if (_signalAck[_dicCmdsTable[ScreenCommand.SetDateTime]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[ScreenCommand.SetDateTime]));
        }

        // Update Status
        void UpdateOvenUnit(UnitDataFrame Unitvalue)
        {
            try
            {
                if (!_eqobject.ChamberList[1].SpaceList.ContainsKey(Unitvalue.GetLocationID))
                    return;


                if (Unitvalue.GetSlotID == 0) // no panel 
                {
                   // if (Unitvalue.GetLocationID == 39) // Transfer Unloader Postion,  Assgin data to robot.. 
                        return;

                    // if (_eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material != null)
                    //  _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material = null;


                }
                else  //have Panel
                {

                   // SMaterial TempPanel;

                    if (Unitvalue.GetLocationID == 1) // Transfer loader Postion, robot Assgin data.. 
                    {
                        return;
                    }

                    var PanelObject = from Panel in _eqobject.ChamberList[1].SpaceList
                                      where Panel.Value.Material !=null && Panel.Value.Material.GetID == Unitvalue.GetPanelID
                                 select Panel.Key;

                    if (PanelObject.Count() > 0)
                    {
                        foreach (int Unit in PanelObject)
                        {
                            if (Unit == Unitvalue.GetLocationID)
                                return;

                            _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material = _eqobject.ChamberList[1].SpaceList[Unit].Material;
                            _eqobject.ChamberList[1].SpaceList[Unit].Material = null;
                            if (OnPanelTransferEQ != null)
                                OnPanelTransferEQ(this, new PanelEQTransferEventArgs(_eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material, Unitvalue.GetLocationID));
                            break;
                        }
                    }
                    else
                    {
                        _logger.WriteLog("Lost PanelID ={0}, but it find from stage = {1}", Unitvalue.GetPanelID, Unitvalue.GetLocationID);

                        var prePanelObject = from Panel in _eqobject.ChamberList[1].SpaceList
                                             where Panel.Value.PreMaterial != null && Panel.Value.PreMaterial.GetID == Unitvalue.GetPanelID
                                             select Panel.Key;
                        if (prePanelObject.Count() > 0)
                        {

                            foreach (int Unit in prePanelObject)
                            {
                                if (Unit == Unitvalue.GetLocationID && _eqobject.ChamberList[1].SpaceList[Unit].Material != null)
                                {
                                    _logger.WriteLog("Lost PanelID ={0}, stage = {1} has data ... is error ", Unitvalue.GetPanelID, Unit);
                                    return;
                                }
                                // _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material = _eqobject.ChamberList[1].SpaceList[Unit].Material;
                                // _eqobject.ChamberList[1].SpaceList[Unit].Material = null;
                                _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material = _eqobject.ChamberList[1].SpaceList[Unit].PreMaterial;
                                _eqobject.ChamberList[1].SpaceList[Unit].PreMaterial = null;
                                if (OnPanelTransferEQ != null)
                                    OnPanelTransferEQ(this, new PanelEQTransferEventArgs(_eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material, Unitvalue.GetLocationID));
                                break;
                            }
                        }
                        else
                        {
                            _logger.WriteLog("Lost PanelID ={0}, is not find anything", Unitvalue.GetPanelID);
                        }  
                    }
                    if (Unitvalue.GetLocationID == 39 && _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material != null && !_eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material.AssingTake)
                    {
                        _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material.AssingTake = true;
                        if (OnAssingTakePanel != null)
                            OnAssingTakePanel(this, _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material);
                        return;
                    }







                    //if (_eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material != null && _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material.GetID == Unitvalue.GetPanelID)
                    //    return;
                    //else if(Unitvalue.GetLocationID == 26)
                    //{
                    //    if (_eqobject.ChamberList[1].SpaceList[19].Material !=null && _eqobject.ChamberList[1].SpaceList[19].Material.GetID == Unitvalue.GetPanelID)
                    //    {
                    //        _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material = _eqobject.ChamberList[1].SpaceList[19].Material;
                    //        _eqobject.ChamberList[1].SpaceList[19].Material = null;
                    //        _logger.WriteLog(string.Format("PanelID={0},Pos change 19 to {1}", Unitvalue.GetPanelID, Unitvalue.GetLocationID));
                    //    }
                    //    return;
                    //}
                    //else if (_eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID - 1].Material.GetID == Unitvalue.GetPanelID)
                    //{
                    //    _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material = _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID - 1].Material;
                    //    _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID - 1].Material = null;
                    //    _logger.WriteLog(string.Format("PanelID={0},Pos change {1} to {2}", Unitvalue.GetPanelID, Unitvalue.GetLocationID - 1, Unitvalue.GetLocationID));
                    //}

                 
                    //else // unknow Panel ??
                    //{
                    //    _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material = null;
                    //    _eqobject.ChamberList[1].SpaceList[Unitvalue.GetLocationID].Material = new Rorze.Equipment.Unit.SMaterial("Unkown",Unitvalue.GetPanelID, Unitvalue.GetLotID, Unitvalue.GetSlotID, -1, "Unknow Source");
                    //}
                }
            }
            catch (Exception ex)
            {
                
                _logger.WriteLog(ex);
            }
        }

        //Recipe Object

        public RcipeManage GetRecipeManage { get { return _eqobject.RecipeObject; } }
       
    }
     public class UnitDataFrame
    {
        private string _strFrame;
        private int _slotID =-1;
        private string _panelID;
        private string _cassetteID;
        private string _lotID;
        private string _PPID;
        private int _locationID=-1;
        private string _VIDdata;
        public UnitDataFrame(string frame)
        {
            _strFrame = frame;

            int.TryParse(frame.Split('/')[0], out _slotID);
            _panelID = frame.Split('/')[1];
            _cassetteID = frame.Split('/')[2];
            _lotID = frame.Split('/')[3];
            _PPID = frame.Split('/')[4];
            if (_strFrame.Split('/').Count() == 6)
            {
                if (frame.Split('/')[5].Split(',').Count() > 1)
                    _VIDdata = frame.Split('/')[5];
                else
                    int.TryParse(frame.Split('/')[5], out _locationID);
            }

        }
        
        public int GetSlotID { get { return _slotID; } }
        public string GetPanelID { get { return _panelID; } }
        public string GetCassetteID { get { return _cassetteID; } }
        public string GetLotID { get { return _lotID; } }
        public string GetPPID { get { return _PPID; } }
        public int GetLocationID { get { return _locationID; } }
        public string GetProcessEndData { get { return _VIDdata; } }
    }

    public class ProcessEventArgs : EventArgs
    {
        public UnitDataFrame PanelData;
        public bool IsProcessStart;
        public ProcessEventArgs(bool IsStart,UnitDataFrame DataValue)
        {
            IsProcessStart = IsStart;
            PanelData = DataValue;
        }
    }
    public delegate void ScreenProcessHandler(object sender, ProcessEventArgs e);

    public class PanelEQTransferEventArgs : EventArgs
    {
        public SMaterial Panel;
        public int CurrntPos;

        public PanelEQTransferEventArgs(SMaterial exePanel, int Pos)
        {
            Panel = exePanel;
            CurrntPos = Pos;

        }

    }
    public delegate void PanelEQTransferHandler(object sender, PanelEQTransferEventArgs e);

}
