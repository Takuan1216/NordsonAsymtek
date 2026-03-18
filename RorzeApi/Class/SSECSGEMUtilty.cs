using RorzeApi.SECSGEM;
using RorzeUnit.Class;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Loadport.Type;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RorzeUnit.Class.SRecipe;

namespace RorzeApi.Class
{


    public class SSECSGEMUtilty // for SECS Gem API
    {
        public Dictionary<int, I_Loadport> LoadPortList;
        public Dictionary<int, I_Aligner> AlignList;
        public List<I_Robot> RobotList;

        public SAlarm Alarm;
        public SGroupRecipeManager Recipe;
        private STransfer _autoProcess;      //  自動傳片流程
        public List<I_OCR> OCRList;

       private SSSorterSQL m_MySQL;
        // event 
        public event FoupChangeEventHandler OnPortFoupExistChenge;
        public event FoupChangeEventHandler OnReadIDcomplete;
        public event FoupChangeEventHandler OnPortClamped;
        public event FoupChangeEventHandler OnPortUnClamped;
        public event FoupChangeEventHandler OnPortDocked;
        public event FoupChangeEventHandler OnPortUnDocked;

        public event EventHandler<WaferDataEventArgs> OnWaferInAlinger;
        public event EventHandler<WaferDataEventArgs> OnRobotUppArmTake;
        public event EventHandler<WaferDataEventArgs> OnRobotLowArmTake;
        public event EventHandler<WaferDataEventArgs> OnRobotUppArmPut;
        public event EventHandler<WaferDataEventArgs> OnRobotLowArmPut;
        public event EventHandler<WaferDataEventArgs> OnWaferProcessStart;
        public event EventHandler<WaferDataEventArgs> OnWaferProcessEnd;
        public event EventHandler<WaferDataEventArgs> OnWaferReadOCRComplete;
        public event EventHandler<WaferDataEventArgs> OnWaferMeasureEnd; //v1.000 Jacky Hsiung Add
        public event EventHandler<AlarmEventArgs> AlarmSet;
        public event EventHandler<AlarmEventArgs> AlarmReset;

        public event EventHandler<E84ChangeEventEventArgs> OnPortE84StatusChange;
        public event OccurStateMachineChangEventHandler OnStatusMachineChange; //LoadPort 狀態改變

        public SSECSGEMUtilty(List<I_Robot> listTRB, Dictionary<int, I_Loadport> Ports, Dictionary<int, I_Aligner> Aligns, SAlarm Alarms, SGroupRecipeManager recipes, List<I_OCR> OCR, STransfer autoProcess, SSSorterSQL mySQL)
        {
            RobotList = listTRB;
            LoadPortList = Ports;
            AlignList = Aligns;
            Recipe = recipes;
            Alarm = Alarms;

            OCRList = OCR;
            m_MySQL = mySQL;
        
            _autoProcess = autoProcess;
            // dbLog = Log;
            foreach (I_Loadport Loadport in LoadPortList.Values)
            {
                if (Loadport == null)
                    continue;

                //  Loadport.OnFoupExistChenge += Loadport_OnFoupExistChenge; // 
                //  Loadport.OnReadIDcomplete += Loadport_OnReadIDcomplete;
                Loadport.OnClmp1Complete += Loadport_OnClmp1Complete;
                Loadport.OnUclm1Complete += Loadport_OnUclmp1Complete;

                Loadport.OnClmpComplete += Loadport_OnClmpComplete;  //Docked & Mapping
                Loadport.OnUclmComplete += Loadport_OnUclmComplete;
                Loadport.E84Object.OnAceessModeChange += E84Object_OnAceessModeChange;

                Loadport.OnStatusMachineChange += Loadport_OnStatusMachineChange;

            }


            foreach (I_Aligner Aligner in AlignList.Values)
            {
                if (Aligner == null)
                    continue;

                Aligner.OnAssignWaferData += Aligner_OnAssignWaferData;
                Aligner.OnAligCompelet += Aligner_OnAligCompelet;
            }

            foreach (I_Robot Robot in RobotList.ToArray())
            {

                Robot.OnAssignLowerArmWaferData += Robot_OnAssignLowerArmWaferData;
                Robot.OnAssignUpperArmWaferData += Robot_OnAssignUpperArmWaferData;
                Robot.OnLeaveLowerArmWaferData += Robot_OnLeaveLowerArmWaferData;
                Robot.OnLeaveUpperArmWaferData += Robot_OnLeaveUpperArmWaferData;

                Robot.OnWaferStart += Robot_OnWaferStart;
                Robot.OnWaferEnd += Robot_OnWaferEnd;
                Robot.OnWaferMeasureEnd += OnWaferMeasureComplete;
            }

            Alarms.OnAlarmOccurred += Alarms_OnAlarmOccurred;
            Alarms.OnAlarmRemove += Alarms_OnAlarmRemove;

        }




        public void ReadIDcomplete(I_Loadport port, RorzeUnit.Class.Loadport.Event.FoupExisteChangEventArgs e)
        {
            if (OnReadIDcomplete != null)
                OnReadIDcomplete(this, new FoupChangeEventEventArgs(true, port.BodyNo, port.FoupID, port.MappingData));
        }

        private void OnWaferMeasureComplete(object sender, WaferDataEventArgs e)
        {
            if (OnWaferMeasureEnd != null)
                OnWaferMeasureEnd(this, new WaferDataEventArgs(e.Wafer));
        }


        private void Aligner_OnAligCompelet(object sender, WaferDataEventArgs e)
        {
            if (OnWaferReadOCRComplete != null && e.Wafer != null) // HSC GRPC
                OnWaferReadOCRComplete(this, new WaferDataEventArgs(e.Wafer));
        }


        //public List<string> GetAllRecipeList()
        //{
        //    return EQ.RecipeList;
        //}
        //public void GetRecipebody(string RecipeName)
        //{
        //    EQ.RecipeContentW(5000, RecipeName);
        //}
        //public Dictionary<eRecipeConent, string> GetRecipeContent()
        //{
        //    return EQ.GetRecipeContent;
        //}



        private void Robot_OnWaferEnd(object sender, WaferDataEventArgs e)
        {
            if (OnWaferProcessEnd != null)
                OnWaferProcessEnd(this, new WaferDataEventArgs(e.Wafer));
        }

        private void Robot_OnWaferStart(object sender, WaferDataEventArgs e)
        {
            if (OnWaferProcessStart != null)
                OnWaferProcessStart(this, new WaferDataEventArgs(e.Wafer));
        }

        
        private void Loadport_OnStatusMachineChange(object sender, OccurStateMachineChangEventArgs e) {

           
            if (OnStatusMachineChange != null)
                OnStatusMachineChange(sender, new OccurStateMachineChangEventArgs(e.StatusMachine ));


            
        }
        private void E84Object_OnAceessModeChange(object sender, RorzeUnit.Class.E84.Event.E84ModeChangeEventArgs e)
        {
            I_E84 E84 = (I_E84)sender;

            if (OnPortE84StatusChange != null)
                OnPortE84StatusChange(this, new E84ChangeEventEventArgs(e.Auto, E84.BodyNo));
        }

        private void Alarms_OnAlarmRemove(AlarmEventArgs args)
        {
            if (AlarmReset != null)
                AlarmReset(this, new AlarmEventArgs(args.AlarmID, args.Type, args.UnitType, args.AlarmMsg, args.CreateTime));
        }

        private void Alarms_OnAlarmOccurred(AlarmEventArgs args)
        {
            if (AlarmSet != null)
                AlarmSet(this, new AlarmEventArgs(args.AlarmID, args.Type, args.UnitType, args.AlarmMsg, args.CreateTime));
        }

        private void Robot_OnLeaveUpperArmWaferData(object sender, WaferDataEventArgs e)
        {
            if (OnRobotUppArmPut != null && e.Wafer != null)
                OnRobotUppArmPut(sender, new WaferDataEventArgs(e.Wafer));
        }

        private void Robot_OnLeaveLowerArmWaferData(object sender, WaferDataEventArgs e)
        {

            if (OnRobotLowArmPut != null && e.Wafer != null)
                OnRobotLowArmPut(sender, new WaferDataEventArgs(e.Wafer));
        }

        private void Robot_OnAssignUpperArmWaferData(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            if (OnRobotUppArmTake != null && e.Wafer != null)
                OnRobotUppArmTake(sender, new WaferDataEventArgs(e.Wafer));
        }

        private void Robot_OnAssignLowerArmWaferData(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            if (OnRobotLowArmTake != null && e.Wafer != null)
                OnRobotLowArmTake(sender, new WaferDataEventArgs(e.Wafer));
        }

        private void Aligner_OnAssignWaferData(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            if (OnWaferInAlinger != null && e.Wafer != null)
                OnWaferInAlinger(this, new WaferDataEventArgs(e.Wafer));
        }

        private void Loadport_OnClmp1Complete(object sender, RorzeUnit.Class.Loadport.Event.LoadPortEventArgs e)
        {
            I_Loadport port = (I_Loadport)sender;

            if (port.FoupExist == false) return;
            if (OnPortClamped != null)
                OnPortClamped(this, new FoupChangeEventEventArgs(port.FoupExist, port.BodyNo, port.FoupID, port.MappingData));
        }

        private void Loadport_OnUclmp1Complete(object sender, RorzeUnit.Class.Loadport.Event.LoadPortEventArgs e)
        {
            I_Loadport port = (I_Loadport)sender;

            if (port.FoupExist == false) return;
            if (OnPortUnClamped  != null)
                OnPortUnClamped(this, new FoupChangeEventEventArgs(port.FoupExist, port.BodyNo, port.FoupID, port.MappingData));
        }

        private void Loadport_OnUclmComplete(object sender, RorzeUnit.Class.Loadport.Event.LoadPortEventArgs e)
        {
            I_Loadport port = (I_Loadport)sender;


            if (OnPortUnDocked != null)
                OnPortUnDocked(this, new FoupChangeEventEventArgs(true, port.BodyNo, port.FoupID, port.MappingData));
        }

        private void Loadport_OnClmpComplete(object sender, RorzeUnit.Class.Loadport.Event.LoadPortEventArgs e)
        {
            I_Loadport port = (I_Loadport)sender;

            if (OnPortDocked != null)
                OnPortDocked(this, new FoupChangeEventEventArgs(true, port.BodyNo, port.FoupID, port.MappingData));
        }

        public void FoupExistChenge(I_Loadport port, RorzeUnit.Class.Loadport.Event.FoupExisteChangEventArgs e)
        {
            // I_Loadport port = (I_Loadport)sender;

            if (OnPortFoupExistChenge != null)
                OnPortFoupExistChenge(this, new FoupChangeEventEventArgs(e.FoupExist, port.BodyNo, port.FoupID, port.MappingData));
        }

        public void Exejob(bool bNoAign, string strCJID)
        {
            _autoProcess.ExecuteCJPJ(bNoAign, strCJID);
        }




        //public List<string> GetAllRecipeList()
        //{
        //    return recipeMgr.GetRecipeNameList(99).ToList();
        //}

        //public List<string> GetAllRecipeList(int Size)
        //{
        //    return recipeMgr.GetRecipeNameList(Size).ToList();
        //}

        //public bool CheckRecipeExit(string PPID)
        //{
        //    return recipeMgr.CheckRecipeExist(PPID);
        //}

        //public SSToolRecipeInfo[] GetRecipeInfo(int Size, string PPID)
        //{
        //    return recipeMgr.GetRecipeData(Size, PPID);
        //}




        public enumStateMachine GetPortStatus(int PotID)
        {
            return LoadPortList[PotID].StatusMachine;
        }

        public void SetPortStatus(int PotID, enumStateMachine Status)
        {
             LoadPortList[PotID].StatusMachine = Status;
        }


        public CarrierIDStats GetCarrierIDstatus(int PotID)
        {
            return LoadPortList[PotID].CarrierIDstatus;
        }

        public void SetCarrierIDstatus(int PotID, CarrierIDStats Status)
        {
            LoadPortList[PotID].CarrierIDstatus = Status;
        }

        public CarrierSlotMapStats GetSlotMappingStats(int PotID)
        {
            return LoadPortList[PotID].SlotMappingStats;
        }

        public void SetCarrierSlotMapStats(int PotID, CarrierSlotMapStats Status)
        {
            LoadPortList[PotID].SlotMappingStats = Status;
        }

        public CarrierAccessStats GetCarrierAccessStats(int PotID)
        {
            return LoadPortList[PotID].CarrierAccessStats;
        }

        public void SetCarrierAccessStats(int PotID, CarrierAccessStats Status)
        {
            LoadPortList[PotID].CarrierAccessStats = Status;
        }

        public CarrierState GetCarrierState(int PotID)
        {
            return LoadPortList[PotID].CarrierState;
        }

        public void SetCarrierState(int PotID, CarrierState Status)
        {
            LoadPortList[PotID].CarrierState = Status;
        }

        

        public void Dock(int PotID)
        {
            LoadPortList[PotID].CLMP(true); //Need Check Foup Type
        }
        public void UnDock(int PotID)
        {
            LoadPortList[PotID].UCLM();
        }
        public void Clamp(int PotID)
        {
            LoadPortList[PotID].CLMP1();
        }
        public void UnClamp(int PotID)
        {
            LoadPortList[PotID].UCLM1();
        }

        public string GetMappingData(int PortID)
        {
            return LoadPortList[PortID].MappingData;
        }
        //210915 Ming Merge PTI dTEK V1.001 ↓
        public List<string> GroupRecipeList()
        {
            return Recipe.GetRecipeGroupList.Keys.ToList();
        }
        public SGroupRecipe GroupRecipe(string Name)
        {
            if (Recipe.GetRecipeGroupList.ContainsKey(Name))
                return Recipe.GetRecipeGroupList[Name];
            else
                return null;
        }
        //210915 Ming Merge PTI dTEK V1.001 ↑

    }
    public delegate void FoupChangeEventHandler(object sender, FoupChangeEventEventArgs e);
    public class FoupChangeEventEventArgs : EventArgs
    {
        public bool FoupExist;
        public int PortNo;
        public string CarrierID;
        public string MappingData;
        public FoupChangeEventEventArgs(bool OnFoup, int No, string ID, string Mapping)
        {
            FoupExist = OnFoup;
            PortNo = No;
            CarrierID = ID;
            MappingData = Mapping;
        }
    }

    public delegate void E84ChangeEventHandler(object sender, E84ChangeEventEventArgs e);
    public class E84ChangeEventEventArgs : EventArgs
    {
        public bool IsAuto;
        public int PortNo;
        public E84ChangeEventEventArgs(bool Auto, int No)
        {
            IsAuto = Auto;
            PortNo = No;

        }
    }

    public delegate void ProcessJobChangeEventHandler(object sender, ProcessJobChangeEventEventArgs e);
    public class ProcessJobChangeEventEventArgs : EventArgs
    {
        public string CJID;
        public string PJID;
        public string CarrierID;
        public int PortID;

        public ProcessJobChangeEventEventArgs(string CJ, string PJ, string FoupID, int Port)
        {
            CJID = CJ;
            PJID = PJ;
            CarrierID = FoupID;
            PortID = Port;
        }
    }

}
