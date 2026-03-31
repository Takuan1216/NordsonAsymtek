using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rorze.Secs;
using Rorze.DB;
using RorzeComm;

using Rorze.Equipment.Unit;
using System.Windows.Forms;
using System.Data;
using static RorzeComm.sKernel32;
using System.Globalization;
using static RorzeApi.SECSGEM.SProcessJobObject;
using RorzeUnit;
using System.Collections.Concurrent;
using RorzeApi.Class;
using RorzeUnit.Class.Loadport.Type;
using RorzeUnit.Interface;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class;
using RorzeUnit.Class.Loadport.Event;
using static RorzeUnit.Class.SRecipe;

using RorzeComm.Log;
using RorzeComm.Threading;
using System.Runtime.CompilerServices;
using System.Threading;


namespace RorzeApi.SECSGEM
{
    //Collection Event CEID

    //S1F13 CR  Communication Request               H<->E
    //S1F14 CRA Communications Request Acknowledge  H<->E

    //S6F11 ERS Event Report Send                   H<-E 
    //S6F12 ERA Event Report Acknowledge            H->E 
    //S6F15 ERR Event Report Request                H->E 
    //S6F16 ERD Event Report Data                   H<-E 

    //S16F15 Process Job Multiple Create                H->E
    //S16F16 Process Job Multiple Create Acknowledge    H<-E
    //S16F17 Process Job Dequeue                        H->E
    //S16F18 Process Job Dequeue Acknowledge            H<-E

    public enum enumComparisonStatus { WaferUnKnow = 0, WaferResume, WaferCancel }//211022 Ming secs需要比對wafer id
    enum CommandAction { Delete = 0, Modify = 1 };
    enum CommandEventAction { Disable = 0, Enable = 1 };
    enum CTLJOBCMD { CJStart = 1, CJPause, CJResume, CJCanecl, CJDeselect, CJStop, CJAbort, CJHOQ }
    public class FunctionSetupEventArgs : EventArgs
    {
        public string CJID;
        public List<SProcessJobObject> ExcutePJ;
        public SControlJobObject ExcuteCJ;

        public FunctionSetupEventArgs(string CJname, List<SProcessJobObject> PJ, SControlJobObject CJ)
        {
            CJID = CJname;
            ExcutePJ = PJ;
            ExcuteCJ = CJ;
        }
    }
    public delegate void FunctionSetupHandler(object sender, FunctionSetupEventArgs e);
    public class SGEM300
    {
        // GemStat
        GEMControlStats _GEMControlstats;
        GEMControlStats _preGEMControlstats;

        GEMProcessStats _GEMProcessStats;
        GEMProcessStats _preGEMProcessStats;

        Dictionary<GEMProcessStats, string> dicProcessStats = new Dictionary<GEMProcessStats, string>()
        {
            {GEMProcessStats.Init, "ProcessInit" },
            {GEMProcessStats.Idle, "ProcessIdle" },
            {GEMProcessStats.FOUPClamp, "ProcessFOUPClamp" },
            {GEMProcessStats.ComMID, "ProcessComMID" },
            {GEMProcessStats.FOUPDocking, "ProcessFOUPDocking" },
            {GEMProcessStats.FOUPReady, "ProcessFOUPReady" },
            {GEMProcessStats.FunctionSetup, "ProcessFunctionSetup" },
            {GEMProcessStats.FunctionSetupFail, "ProcessFunctionSetupFail" },
            {GEMProcessStats.Executing,"ProcessExecuting"},
            {GEMProcessStats.Finish,"ProcessFinish" },
            {GEMProcessStats.TagWriteData,"ProcessTagWriteData" },
            {GEMProcessStats.Abort,"ProcessAbort" },
            {GEMProcessStats.Pause,"ProcessPause"},
            {GEMProcessStats.Resume,"ProcessResume"},
            {GEMProcessStats.Stop,"ProcessStop" },
            {GEMProcessStats.FOUPUnDock,"ProcessFOUPUnDock" },
            {GEMProcessStats.FOUPUnClamp,"ProcessFOUPUnClamp" },
            {GEMProcessStats.PodReadyToMoveOut,"ProcessPodReadyToMoveOut"}

        };

        public GEMControlStats GEMControlStatus
        {
            get { return _GEMControlstats; }
            set
            {
                if (_GEMControlstats != _preGEMControlstats)
                    _preGEMControlstats = _GEMControlstats;

                _GEMControlstats = value;
            }
        }
        public GEMControlStats PreGEMControlStatus
        { get { return _preGEMControlstats; } set { _preGEMControlstats = value; } }
        public GEMCommStats GetGEMCommStats
        {
            get
            {
                if (!_secsdriver.GetSecsStarted())
                {
                    this.SetGEMControlStatus = GEMControlStats.OFFLINE;
                    return GEMCommStats.DISABLE;
                }
                if (_secsdriver.GetInternalConnected())
                    return GEMCommStats.COMMUNICATION;
                this.SetGEMControlStatus = GEMControlStats.OFFLINE;
                return GEMCommStats.NOTCOMMUNICATION;
            }

        }
        public GEMControlStats SetGEMControlStatus
        {
            set
            {
                List<DVID_Obj> DVID_List = new List<DVID_Obj>();
                List<CEID_Obj> CEID_List = new List<CEID_Obj>();

                if (_GEMControlstats == value) return;
                _preGEMControlstats = _GEMControlstats;

                if (value == GEMControlStats.ATTEMTPONLINE)
                    _PollingHotbeat.Set();
                else
                    _PollingHotbeat.Reset();
                string PreStatus = (_preGEMControlstats == GEMControlStats.OFFLINE) ? "OFFLINE" : (_preGEMControlstats == GEMControlStats.EQUIPMENTOFFLINE) ? "EQUIPMENTOFFLINE" : (_preGEMControlstats == GEMControlStats.HOSTOFFLINE) ? "HOSTOFFLINE" : (_preGEMControlstats == GEMControlStats.ATTEMTPONLINE) ? "ATTEMTPONLINE" : (_preGEMControlstats == GEMControlStats.ONLINELOCAL) ? "ONLINELOCAL" : "ONLINEREMOTE";

                string NowStatus = (value == GEMControlStats.OFFLINE) ? "OFFLINE" : (value == GEMControlStats.EQUIPMENTOFFLINE) ? "EQUIPMENTOFFLINE" : (value == GEMControlStats.HOSTOFFLINE) ? "HOSTOFFLINE" : (value == GEMControlStats.ATTEMTPONLINE) ? "ATTEMTPONLINE" : (value == GEMControlStats.ONLINELOCAL) ? "ONLINELOCAL" : "ONLINEREMOTE";
                WriteLog(string.Format("SECS Change status[{0} to {1}]", PreStatus, NowStatus));
                string Mode = (value == GEMControlStats.ONLINEREMOTE) ? "GemControlStateREMOTE" : (value == GEMControlStats.ONLINELOCAL) ? "GemControlStateLOCAL" : "GemEquipmentOFFLINE";

                if (this.CurrntGEMProcessStats == GEMProcessStats.Executing && value == GEMControlStats.ONLINEREMOTE)
                {

                    value = GEMControlStats.ONLINELOCAL;
                    Mode = "GemControlStateLOCAL";
                }



                if (Mode == "GemEquipmentOFFLINE")
                {
                    //S_S6F11(_CEIDcontrol.CEIDList[Mode]);
                    CEID_List.Add(new CEID_Obj(Mode));
                    SetupDVID_EVENT(CEID_List, DVID_List);
                }

                _GEMControlstats = value;
                //S_S6F11(_CEIDcontrol.CEIDList[Mode]);
                CEID_List.Add(new CEID_Obj(Mode));
                SetupDVID_EVENT(CEID_List, DVID_List);


                // Port in Service
                if (_GEMControlstats == GEMControlStats.ONLINEREMOTE)
                {
                    string PortStr = "";
                    for (int i = 0; i < 4; i++)
                    {
                        if (SecsGEMUtilty.LoadPortList[i + 1].Disable == false)
                        {

                            SecsGEMUtilty.LoadPortList[i + 1].UndockQueueByHost = false;

                            if (SecsGEMUtilty.LoadPortList[i + 1].FoupExist == false)
                            {
                                SecsGEMUtilty.LoadPortList[i + 1].E84Status = E84PortStates.ReadytoLoad;
                                PortStr = (i + 1 == 1) ? "PORTAMIR" : (i + 1 == 2) ? "PORTBMIR" : (i + 1 == 3) ? "PORTCMIR" : "PORTDMIR";
                                //S_S6F11(_CEIDcontrol.CEIDList[PortStr]);
                                CEID_List.Add(new CEID_Obj(PortStr));

                            }
                            else if (SecsGEMUtilty.LoadPortList[i + 1].FoupExist == true
                                && (SecsGEMUtilty.LoadPortList[i + 1].StatusMachine == enumStateMachine.PS_Arrived || SecsGEMUtilty.LoadPortList[i + 1].StatusMachine == enumStateMachine.PS_UnClamped))
                            {
                                SecsGEMUtilty.LoadPortList[i + 1].E84Status = E84PortStates.ReadytoUnload;
                                PortStr = (i + 1 == 1) ? "PORTAMOR" : (i + 1 == 2) ? "PORTBMOR" : (i + 1 == 3) ? "PORTCMOR" : "PORTDMOR";
                                // S_S6F11(_CEIDcontrol.CEIDList[PortStr]);
                                CEID_List.Add(new CEID_Obj(PortStr));


                            }
                            else if (SecsGEMUtilty.LoadPortList[i + 1].FoupExist)
                            {
                                SecsGEMUtilty.LoadPortList[i + 1].E84Status = E84PortStates.TransferBlock;
                            }

                            //_VIDcontrol.DVIDList["PortID"].CurrentValue = i + 1;
                            //S_S6F11(_CEIDcontrol.CEIDList["PortTransferSMTrans02"]);

                            DVID_List.Add(new DVID_Obj("PortID", i + 1));
                            CEID_List.Add(new CEID_Obj("PortTransferSMTrans02"));
                            SetupDVID_EVENT(CEID_List, DVID_List);


                        }


                    }
                }


            }
        }
        public GEMProcessStats CurrntGEMProcessStats
        {
            get { return _GEMProcessStats; }
            set
            {
                bool IsChange = false;


                if (value == _GEMProcessStats)
                    return;

                List<DVID_Obj> DVID_List = new List<DVID_Obj>();
                List<CEID_Obj> CEID_List = new List<CEID_Obj>();



                //if (_GEMControlstats < GEMControlStats.ONLINELOCAL)
                //    return;
                if (value > _GEMProcessStats && (value == GEMProcessStats.FOUPUnClamp || value == GEMProcessStats.PodReadyToMoveOut))
                {

                    foreach (I_Loadport Port in SecsGEMUtilty.LoadPortList.Values)
                    {
                        if (Port.StatusMachine == enumStateMachine.PS_UnDocked)
                            return;
                    }


                }
                else if (value < _GEMProcessStats && value != GEMProcessStats.Idle && value != GEMProcessStats.Executing && value != GEMProcessStats.FunctionSetup && value != GEMProcessStats.FunctionSetupFail)
                {
                    return;
                }

                _preGEMProcessStats = _GEMProcessStats;

                if (value != _GEMProcessStats)
                {
                    _GEMProcessStats = value;
                    // S_S6F11(_CEIDcontrol.CEIDList[dicProcessStats[_GEMProcessStats]]);
                    //S_S6F11(_CEIDcontrol.CEIDList["ProcessStateChange"]);
                    CEID_List.Add(new CEID_Obj("ProcessStateChange"));
                    SetupDVID_EVENT(CEID_List, DVID_List);
                }
            }

        }
        public GEMProcessStats PreGEMProcessStats
        {
            get { return _preGEMProcessStats; }
            set
            {
                _preGEMProcessStats = value;
            }

        }


        private enumComparisonStatus m_ComparisonStatus = enumComparisonStatus.WaferUnKnow;//211022 Ming secs需要比對wafer id
        public enumComparisonStatus ComparisonStatus//211022 Ming secs需要比對wafer id
        {
            get { return m_ComparisonStatus; }
            set
            {
                m_ComparisonStatus = value;
            }
        }



        SPollingThread _PollingHotbeat;        // S1F13 .. HotBeats
        SPollingThread _pollingTraceData;                       //polling to collect trace data
        SInterruptOneThread _exeSECSStart;
        SInterruptOneThread _exeSECSStop;
        Dictionary<string, List<HostWaferInfo>> _hostwaferInfo;

        public event EventHandler OnAlarmStatusChange;
        public event EventHandler<string> OnHostMessageSend;
        public event FunctionSetupHandler OnFunctionSetup;
        public event EventHandler OnProcessJobUpdate;
        public event EventHandler OnControlJobUpdate;
        public event EventHandler OnSECSOpen;
        public event EventHandler OnSECSClose;
        // Trace Data
        private Dictionary<int, List<int>> _dicTraceContent;            //trace ID link 的 SVID查表
        private Dictionary<int, int> _dicTraceTOTSMP;            //trace ID link 的 SVID查表
        private Dictionary<int, int> _dicCurrentTOTSMP;            //trace ID link 的 SVID查表
        public string CurrentExeCJ;
        public string CurrentExePJ;

        public SProcessJobObject CurrentPJ;
        public SControlJobObject CurrentCJ;

        SLogger _logger;
        ISECS _secsdriver;
        SSECSParameter _parameter;
        CEIDManager _CEIDcontrol;
        PJCJManager _jobcontrol;
        VIDManager _VIDcontrol;
        MainDB _DB;
        SWafer TempWafer;
        SWafer InspectWafer; //不確定量測資料是否馬上出來，使用TempWafer怕會有可能被變動，自成一家

        STransfer _autoProcess;      //  自動傳片流程
        SAlarmListDB _AlarmListDB;

        SGroupRecipeManager _GroupRecipeManager;

        object objectSubWafer = new object();
        object objectS6F11 = new object();
        object objectWaferData = new object();
        object objectPortStates = new object();
        object objectSetupSECSData = new object(); //20241017，只能用在SetupDVID_EVENT
        int TempPortforSTS;
        string TempCarrierforSTS;
        List<string> TempWaferListforSTS = new List<string>();
        public ISECS GetSECSDriver { get { return _secsdriver; } set { _secsdriver = value; } }

        SSECSGEMUtilty SecsGEMUtilty;

        bool m_bSECS_Enable;
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[SGEM300] {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            if (_logger != null) _logger.WriteLog(strMsg);
        }

        public bool SECSOpenbusy = false;
        public bool SECSClosebusy = false;

        public SGEM300(MainDB DB, SSECSParameter parm
           , CEIDManager CEID, PJCJManager Job, VIDManager VID, SSECSGEMUtilty SecsUity, STransfer autoProcess, SAlarmListDB dbAlarmList, bool secsEnable, SGroupRecipeManager GroupRecipeManager)
        {

            _DB = DB;
            _GEMControlstats = GEMControlStats.OFFLINE;
            _preGEMControlstats = _GEMControlstats;
            _parameter = parm;

            _GroupRecipeManager = GroupRecipeManager;

            _autoProcess = autoProcess;
            _CEIDcontrol = CEID;
            _jobcontrol = Job;
            _VIDcontrol = VID;
            SecsGEMUtilty = SecsUity;
            _AlarmListDB = dbAlarmList;
            m_bSECS_Enable = secsEnable;

            if (m_bSECS_Enable) _logger = new SLogger("Gem300");
            _dicTraceContent = new Dictionary<int, List<int>>();
            _dicTraceTOTSMP = new Dictionary<int, int>();
            _dicCurrentTOTSMP = new Dictionary<int, int>();
            _PollingHotbeat = new SPollingThread(10000);
            _PollingHotbeat.DoPolling += _PollingHotbeat_DoPolling;
            _pollingTraceData = new SPollingThread(1000);
            _pollingTraceData.DoPolling += _pollingTraceData_DoPolling;
            _hostwaferInfo = new Dictionary<string, List<HostWaferInfo>>();
            switch (_parameter.GetSecsConnectConfig.Driver)
            {
                //case SECSDriver.ITRI:
                //    _secsdriver = new ITRISecs(_parameter.GetSecsConnectConfig, _parameter.GetSECSParameterConfig);
                //    break;

                case SECSDriver.SDR:
                    _secsdriver = new GWSecs(m_bSECS_Enable);
                    break;
            }
            _secsdriver.InternalStatusChange += _secsdriver_InternalStatusChange;
            _secsdriver.ReceiveSecsMessage += _secsdriver_ReceiveSecsMessage;
            _exeSECSStart = new SInterruptOneThread(RumSECSStart);
            _exeSECSStop = new SInterruptOneThread(RunSECSStop);

            SecsGEMUtilty.OnPortFoupExistChenge += SecsGEMUtilty_OnPortFoupExistChenge;
            SecsGEMUtilty.OnReadIDcomplete += SecsGEMUtilty_OnReadIDcomplete;
            SecsGEMUtilty.OnPortClamped += SecsGEMUtilty_OnPortClamped;
            SecsGEMUtilty.OnPortUnClamped += SecsGEMUtilty_OnPortUnClamped;

            SecsGEMUtilty.OnPortDocked += SecsGEMUtilty_OnPortDocked;
            SecsGEMUtilty.OnPortUnDocked += SecsGEMUtilty_OnPortUnDocked;

            SecsGEMUtilty.OnWaferInAlinger += SecsGEMUtilty_OnWaferInAlinger;
            SecsGEMUtilty.OnRobotUppArmTake += SecsGEMUtilty_OnRobotUppArmTake;
            SecsGEMUtilty.OnRobotLowArmTake += SecsGEMUtilty_OnRobotLowArmTake;
            SecsGEMUtilty.OnRobotUppArmPut += SecsGEMUtilty_OnRobotUppArmPut;
            SecsGEMUtilty.OnRobotLowArmPut += SecsGEMUtilty_OnRobotLowArmPut;

            SecsGEMUtilty.OnWaferProcessStart += SecsGEMUtilty_OnWaferProcessStart;
            SecsGEMUtilty.OnWaferProcessEnd += SecsGEMUtilty_OnWaferProcessEnd;

            SecsGEMUtilty.AlarmSet += SecsGEMUtilty_AlarmSet;
            SecsGEMUtilty.AlarmReset += SecsGEMUtilty_AlarmReset;

            SecsGEMUtilty.OnPortE84StatusChange += SecsGEMUtilty_OnPortE84StatusChange;

            SecsGEMUtilty.OnWaferMeasureEnd += SecsGEMUtilty_OnWaferMeasureEnd; //v1.000 Jacky Hsiung Add
            SecsGEMUtilty.OnWaferReadOCRComplete += SecsGEMUtilty_OnWaferReadOCRComplete;

            SecsGEMUtilty.OnStatusMachineChange += SecsGEMUtilty_OnLoadPortStatusChange;




        }

        private void SecsGEMUtilty_OnWaferReadOCRComplete(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {


            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            lock (objectWaferData)
            {
                TempWafer = e.Wafer;
                WriteLog($"SetUp TempWafer By OnWaferReadOCRComplete() FoupID={TempWafer.FoupID},LotID={TempWafer.LotID},Slot={TempWafer.Slot}");
                string strWaferID_T7 = TempWafer.WaferID_B == "" ? TempWafer.WaferInforID_B : TempWafer.WaferID_B;
                string strWaferID_M12 = TempWafer.WaferID_F == "" ? TempWafer.WaferInforID_F : TempWafer.WaferID_F;

                /*_VIDcontrol.DVIDList["SubstID"].CurrentValue = strWaferID;
                _VIDcontrol.DVIDList["FromCarrierID"].CurrentValue = TempWafer.FoupID;
                _VIDcontrol.DVIDList["FromPortNum"].CurrentValue = (int)TempWafer.Owner;
                _VIDcontrol.DVIDList["FromSlotNum"].CurrentValue = TempWafer.Slot;

                _VIDcontrol.DVIDList["ToCarrierID"].CurrentValue = TempWafer.ToFoupID;
                _VIDcontrol.DVIDList["ToPortNum"].CurrentValue = (int)TempWafer.ToLoadport;
                _VIDcontrol.DVIDList["ToSlotNum"].CurrentValue = TempWafer.ToSlot;

                S_S6F11(_CEIDcontrol.CEIDList["WaferIDRead"]);
                S_S6F11(_CEIDcontrol.CEIDList["WaferDataEvent"]);
                S_S6F11(_CEIDcontrol.CEIDList["ScribeReadComplete2"]);
                */
                DVID_List.Add(new DVID_Obj("SubstID", strWaferID_T7));


                DVID_List.Add(new DVID_Obj("M12_WaferID", strWaferID_M12));
                DVID_List.Add(new DVID_Obj("T7_WaferID", strWaferID_T7));


                DVID_List.Add(new DVID_Obj("M12_WaferID_ByHost", TempWafer.WaferInforID_F));
                DVID_List.Add(new DVID_Obj("T7_WaferID_ByHost", TempWafer.WaferInforID_B));
                



                DVID_List.Add(new DVID_Obj("FromCarrierID", TempWafer.FoupID));
                DVID_List.Add(new DVID_Obj("FromPortNum", (int)TempWafer.Owner));
                DVID_List.Add(new DVID_Obj("FromSlotNum", TempWafer.Slot));
                DVID_List.Add(new DVID_Obj("ToCarrierID", TempWafer.ToFoupID));
                DVID_List.Add(new DVID_Obj("ToPortNum", (int)TempWafer.ToLoadport));
                DVID_List.Add(new DVID_Obj("ToSlotNum", TempWafer.ToSlot));

                CEID_List.Add(new CEID_Obj("WaferIDRead"));
                CEID_List.Add(new CEID_Obj("WaferDataEvent"));
                // CEID_List.Add(new CEID_Obj("ScribeReadComplete2"));

            }
            SetupDVID_EVENT(CEID_List, DVID_List);
        }

        private void SecsGEMUtilty_OnReadIDcomplete(object sender, FoupChangeEventEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            lock (objectPortStates)
            {

                try
                {
                    /* _VIDcontrol.DVIDList["PortID"].CurrentValue = e.PortNo;
                     _VIDcontrol.DVIDList["CarrierID"].CurrentValue = e.CarrierID;
                     //  CurrntGEMProcessStats = GEMProcessStats.FOUPClamp;
                     if (e.CarrierID != "ReadFail")
                     {
                         SecsGEMUtilty.SetCarrierIDstatus(e.PortNo, CarrierIDStats.IDRead);
                         S_S6F11(_CEIDcontrol.CEIDList["CarrierIDRead"]);
                     }
                     else
                     {
                         SecsGEMUtilty.SetCarrierIDstatus(e.PortNo, CarrierIDStats.IDReadFail);
                         //S_S6F11(_CEIDcontrol.CEIDList["E87_Legacy_Carrier_IDReadFailed"]);
                         S_S6F11(_CEIDcontrol.CEIDList["CarrierIDReadFail"]);

                     }
                 //  S_S6F11(_CEIDcontrol.CEIDList["CarrierClamped"]);
                 //  S_S6F11(_CEIDcontrol.CEIDList[string.Format("Port{0}Clamp", e.PortNo.ToString())]);
                 //  S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans03"]);
                 //  CurrntGEMProcessStats = GEMProcessStats.ComMID;
                    */
                    DVID_List.Add(new DVID_Obj("PortID", e.PortNo));
                    DVID_List.Add(new DVID_Obj("CarrierID", e.CarrierID));
                    if (e.CarrierID != "ReadFail")
                    {
                        SecsGEMUtilty.SetCarrierIDstatus(e.PortNo, CarrierIDStats.IDRead);
                        CEID_List.Add(new CEID_Obj("CarrierIDRead"));
                    }
                    else
                    {
                        SecsGEMUtilty.SetCarrierIDstatus(e.PortNo, CarrierIDStats.IDReadFail);
                        CEID_List.Add(new CEID_Obj("CarrierIDReadFail"));
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                }
            }

            SetupDVID_EVENT(CEID_List, DVID_List);
        }

        //v1.000 Jacky Hsiung Add Start
        public void SecsGEMUtilty_OnWaferMeasureEnd(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            lock (objectWaferData)
            {
                InspectWafer = e.Wafer;
                WriteLog($"SetUp InspectWafer By OnWaferMeasureEnd() FoupID={InspectWafer.FoupID},LotID={InspectWafer.LotID},Slot={InspectWafer.Slot}");
                //S_S6F11(_CEIDcontrol.CEIDList["WaferMeasureEnd"]);
                //CEID_List.Add(new CEID_Obj("WaferMeasureEnd"));
                CEID_List.Add(new CEID_Obj("WaferData"));

            }

            SetupDVID_EVENT(CEID_List, DVID_List);

        }
        //v1.000 Jacky Hsiung Add End

        private void SecsGEMUtilty_OnWaferProcessEnd(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();
            lock (objectWaferData)
            {
                TempWafer = e.Wafer;
                WriteLog($"SetUp TempWafer By OnWaferProcessEnd() FoupID={TempWafer.FoupID},LotID={TempWafer.LotID},Slot={TempWafer.Slot}");
                // S_S6F11(_CEIDcontrol.CEIDList["WaferEnd"]);

            }
        }

        private void SecsGEMUtilty_OnWaferProcessStart(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();
            try
            {
                lock (objectWaferData)
                {
                    TempWafer = e.Wafer;
                    WriteLog($"SetUp TempWafer By OnWaferProcessStart() FoupID={TempWafer.FoupID},LotID={TempWafer.LotID},Slot={TempWafer.Slot}");
                    //  S_S6F11(_CEIDcontrol.CEIDList["WaferStart"]);
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void SecsGEMUtilty_OnPortE84StatusChange(object sender, E84ChangeEventEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();
            lock (objectPortStates)
            {

                try
                {
                    //_VIDcontrol.DVIDList["PortID"].CurrentValue = e.PortNo;
                    //_VIDcontrol.DVIDList["PortAccessMode"].CurrentValue = (e.IsAuto) ? 1 : 0;
                    DVID_List.Add(new DVID_Obj("PortID", e.PortNo));
                    DVID_List.Add(new DVID_Obj("PortAccessMode", (e.IsAuto) ? 1 : 0));

                    if (e.IsAuto)
                    {
                        //S_S6F11(_CEIDcontrol.CEIDList["AccessSMGoAuto"]);
                        CEID_List.Add(new CEID_Obj("AccessSMGoAuto"));
                    }
                    else
                    {
                        //S_S6F11(_CEIDcontrol.CEIDList["AccessSMGoManual"]);
                        CEID_List.Add(new CEID_Obj("AccessSMGoManual"));

                    }

                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                }
            }

            SetupDVID_EVENT(CEID_List, DVID_List);
        }

        private void SecsGEMUtilty_AlarmSet(object sender, AlarmEventArgs e)
        {
            WriteLog(string.Format("Alarm set ={0}", e.AlarmID));
            S_S5F1(e.AlarmID, true, e.AlarmMsg);
        }

        private void SecsGEMUtilty_AlarmReset(object sender, AlarmEventArgs e)
        {
            WriteLog(string.Format("Alarm Reset ={0}", e.AlarmID));
            S_S5F1(e.AlarmID, false);
        }




        private void SecsGEMUtilty_OnRobotLowArmPut(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();
            try
            {
                I_Robot Robot = (I_Robot)sender;
                string StrPosition = "";
                lock (objectWaferData)
                {
                    TempWafer = e.Wafer;
                    WriteLog($"SetUp TempWafer By OnRobotLowArmPut() FoupID={TempWafer.FoupID},LotID={TempWafer.LotID},Slot={TempWafer.Slot}");
                    string strWaferID = TempWafer.WaferID_B == "" ? TempWafer.WaferInforID_B : TempWafer.WaferID_B;

                    /*_VIDcontrol.DVIDList["SubstID"].CurrentValue = strWaferID;
                    _VIDcontrol.DVIDList["WaferScribeGetPutWaferCmp"].CurrentValue = strWaferID;

                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = TempWafer.ToFoupID;
                    _VIDcontrol.DVIDList["PortID"].CurrentValue = (int)TempWafer.ToLoadport;
                    */
                    DVID_List.Add(new DVID_Obj("SubstID", strWaferID));
                        DVID_List.Add(new DVID_Obj("T7_WaferID", strWaferID));
                        DVID_List.Add(new DVID_Obj("M12_WaferID_ByHost", TempWafer.WaferInforID_F));
                        DVID_List.Add(new DVID_Obj("T7_WaferID_ByHost", TempWafer.WaferInforID_B));
                    DVID_List.Add(new DVID_Obj("WaferScribeGetPutWaferCmp", strWaferID));
                    DVID_List.Add(new DVID_Obj("CarrierID", TempWafer.ToFoupID));
                    DVID_List.Add(new DVID_Obj("PortID", (int)TempWafer.ToLoadport));

                    if (Robot.BodyNo == 1)
                    {
                        //_VIDcontrol.DVIDList["SubstLocID"].CurrentValue = TempWafer.Position.ToString();
                        DVID_List.Add(new DVID_Obj("SubstLocID", TempWafer.Position.ToString()));
                    }
                    else
                    {

                        //_VIDcontrol.DVIDList["SubstLocID"].CurrentValue = TempWafer.Position.ToString();
                        DVID_List.Add(new DVID_Obj("SubstLocID", TempWafer.Position.ToString()));


                    }
                    /*_VIDcontrol.DVIDList["EndEffectorGetPutWaferCmp"].CurrentValue = 2;
                    _VIDcontrol.DVIDList["SlotNumGetPutWaferCmp"].CurrentValue = TempWafer.Slot;
                    _VIDcontrol.DVIDList["CtrlJobID"].CurrentValue = TempWafer.CJID;
                    _VIDcontrol.DVIDList["PRJobId"].CurrentValue = TempWafer.PJID;*/

                    DVID_List.Add(new DVID_Obj("EndEffectorGetPutWaferCmp", 2));
                    DVID_List.Add(new DVID_Obj("SlotNumGetPutWaferCmp", TempWafer.Slot));
                    DVID_List.Add(new DVID_Obj("CtrlJobID", TempWafer.CJID));
                    DVID_List.Add(new DVID_Obj("PRJobId", TempWafer.PJID));

                    //  if (TempWafer.ProcessStatus == SWafer.enumProcessStatus.Processed)  // procee End -> occupied
                    if (TempWafer.Owner == TempWafer.ToLoadport && TempWafer.Slot == TempWafer.ToSlot)
                    {
                        //S_S6F11(_CEIDcontrol.CEIDList["SubstLocSMTrans01"]);
                        //S_S6F11(_CEIDcontrol.CEIDList["WaferEnd"]);
                        CEID_List.Add(new CEID_Obj("SubstLocSMTrans01"));
                        CEID_List.Add(new CEID_Obj("WaferEnd"));

                    }
                    //S_S6F11(_CEIDcontrol.CEIDList["WaferPut"]);
                    //S_S6F11(_CEIDcontrol.CEIDList["PutWaferComplete"]);

                    CEID_List.Add(new CEID_Obj("WaferPut"));
                    CEID_List.Add(new CEID_Obj("PutWaferComplete"));

                }

                SetupDVID_EVENT(CEID_List, DVID_List);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void SecsGEMUtilty_OnRobotUppArmPut(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();
            try
            {
                I_Robot Robot = (I_Robot)sender;
                string StrPosition = "";
                lock (objectWaferData)
                {
                    TempWafer = e.Wafer;
                    WriteLog($"SetUp TempWafer By OnRobotUppArmPut() FoupID={TempWafer.FoupID},LotID={TempWafer.LotID},Slot={TempWafer.Slot}");
                    string strWaferID = TempWafer.WaferID_B == "" ? TempWafer.WaferInforID_B : TempWafer.WaferID_B;

                    /*_VIDcontrol.DVIDList["SubstID"].CurrentValue = strWaferID;
                    _VIDcontrol.DVIDList["WaferScribeGetPutWaferCmp"].CurrentValue = strWaferID;

                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = TempWafer.ToFoupID;
                    _VIDcontrol.DVIDList["PortID"].CurrentValue = (int)TempWafer.ToLoadport;
                    */
                    DVID_List.Add(new DVID_Obj("SubstID", strWaferID));
                        DVID_List.Add(new DVID_Obj("T7_WaferID", strWaferID));
                        DVID_List.Add(new DVID_Obj("M12_WaferID_ByHost", TempWafer.WaferInforID_F));
                        DVID_List.Add(new DVID_Obj("T7_WaferID_ByHost", TempWafer.WaferInforID_B));
                    DVID_List.Add(new DVID_Obj("WaferScribeGetPutWaferCmp", strWaferID));
                    DVID_List.Add(new DVID_Obj("CarrierID", TempWafer.ToFoupID));
                    DVID_List.Add(new DVID_Obj("PortID", (int)TempWafer.ToLoadport));


                    if (Robot.BodyNo == 1)
                    {
                        //_VIDcontrol.DVIDList["SubstLocID"].CurrentValue = TempWafer.Position.ToString();
                        DVID_List.Add(new DVID_Obj("SubstLocID", TempWafer.Position.ToString()));
                    }
                    else
                    {
                        //_VIDcontrol.DVIDList["SubstLocID"].CurrentValue = TempWafer.Position.ToString();
                        DVID_List.Add(new DVID_Obj("SubstLocID", TempWafer.Position.ToString()));
                    }
                    /*_VIDcontrol.DVIDList["EndEffectorGetPutWaferCmp"].CurrentValue = 1;
                    _VIDcontrol.DVIDList["SlotNumGetPutWaferCmp"].CurrentValue = TempWafer.Slot;

                    _VIDcontrol.DVIDList["CtrlJobID"].CurrentValue = TempWafer.CJID;
                    _VIDcontrol.DVIDList["PRJobId"].CurrentValue = TempWafer.PJID;
                    */
                    DVID_List.Add(new DVID_Obj("EndEffectorGetPutWaferCmp", 1));
                    DVID_List.Add(new DVID_Obj("SlotNumGetPutWaferCmp", TempWafer.Slot));
                    DVID_List.Add(new DVID_Obj("CtrlJobID", TempWafer.CJID));
                    DVID_List.Add(new DVID_Obj("PRJobId", TempWafer.PJID));
                    DVID_List.Add(new DVID_Obj("SubstLocID", TempWafer.Position.ToString()));



                    DVID_List.Add(new DVID_Obj("FromCarrierID", TempWafer.FoupID));
                    DVID_List.Add(new DVID_Obj("FromPortNum", (int)TempWafer.FromLoadport));
                    DVID_List.Add(new DVID_Obj("FromSlotNum", TempWafer.Slot));
                    DVID_List.Add(new DVID_Obj("ToCarrierID", TempWafer.ToFoupID));
                    DVID_List.Add(new DVID_Obj("ToPortNum", (int)TempWafer.ToLoadport));
                    DVID_List.Add(new DVID_Obj("ToSlotNum", TempWafer.ToSlot));


                    // if (TempWafer.ProcessStatus == SWafer.enumProcessStatus.Processed)  // procee End -> occupied
                    if (TempWafer.Owner == TempWafer.ToLoadport && TempWafer.Slot == TempWafer.ToSlot)
                    {
                        //S_S6F11(_CEIDcontrol.CEIDList["SubstLocSMTrans01"]);
                        //S_S6F11(_CEIDcontrol.CEIDList["WaferEnd"]);

                        CEID_List.Add(new CEID_Obj("SubstLocSMTrans01"));
                        CEID_List.Add(new CEID_Obj("WaferEnd"));

                    }
                    //S_S6F11(_CEIDcontrol.CEIDList["WaferPut"]);
                    //S_S6F11(_CEIDcontrol.CEIDList["PutWaferComplete"]);
                    CEID_List.Add(new CEID_Obj("WaferPut"));
                    CEID_List.Add(new CEID_Obj("PutWaferComplete"));
                }
                SetupDVID_EVENT(CEID_List, DVID_List);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void SecsGEMUtilty_OnRobotLowArmTake(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();
            try
            {
                I_Robot Robot = (I_Robot)sender;

                lock (objectWaferData)
                {
                    TempWafer = e.Wafer;
                    WriteLog($"SetUp TempWafer By OnRobotLowArmTake() FoupID={TempWafer.FoupID},LotID={TempWafer.LotID},Slot={TempWafer.Slot}");

                    string strWaferID = TempWafer.WaferID_B == "" ? TempWafer.WaferInforID_B : TempWafer.WaferID_B;

                    /*_VIDcontrol.DVIDList["SubstID"].CurrentValue = strWaferID;
                    _VIDcontrol.DVIDList["WaferScribeGetPutWaferCmp"].CurrentValue = strWaferID;

                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = TempWafer.FoupID;
                    _VIDcontrol.DVIDList["PortID"].CurrentValue = (int)TempWafer.Owner;
                    */

                    DVID_List.Add(new DVID_Obj("SubstID", strWaferID));
                        DVID_List.Add(new DVID_Obj("T7_WaferID", strWaferID));
      
                    DVID_List.Add(new DVID_Obj("WaferScribeGetPutWaferCmp", strWaferID));
                    DVID_List.Add(new DVID_Obj("CarrierID", TempWafer.FoupID));
                    DVID_List.Add(new DVID_Obj("PortID", (int)TempWafer.Owner));


                    if (Robot.BodyNo == 1)
                    {
                        //_VIDcontrol.DVIDList["SubstLocID"].CurrentValue = "RobotA_LowerArm";
                        DVID_List.Add(new DVID_Obj("SubstLocID", "RobotA_LowerArm"));
                    }
                    else
                    {
                        //_VIDcontrol.DVIDList["SubstLocID"].CurrentValue = "RobotB_LowerArm";
                        DVID_List.Add(new DVID_Obj("SubstLocID", "RobotB_LowerArm"));
                    }
                    /*_VIDcontrol.DVIDList["EndEffectorGetPutWaferCmp"].CurrentValue = 2;
                    _VIDcontrol.DVIDList["SlotNumGetPutWaferCmp"].CurrentValue = TempWafer.Slot;

                    _VIDcontrol.DVIDList["CtrlJobID"].CurrentValue = TempWafer.CJID;
                    _VIDcontrol.DVIDList["PRJobId"].CurrentValue = TempWafer.PJID;
                    */
                    DVID_List.Add(new DVID_Obj("EndEffectorGetPutWaferCmp", 2));
                    DVID_List.Add(new DVID_Obj("SlotNumGetPutWaferCmp", TempWafer.Slot));
                    DVID_List.Add(new DVID_Obj("CtrlJobID", TempWafer.CJID));
                    DVID_List.Add(new DVID_Obj("PRJobId", TempWafer.PJID));
                    DVID_List.Add(new DVID_Obj("SubstLocID", TempWafer.Position.ToString()));



                    if (TempWafer.ProcessStatus == SWafer.enumProcessStatus.WaitProcess)  // 剛出LP or Tower -> unoccupied
                    {
                        //S_S6F11(_CEIDcontrol.CEIDList["SubstLocSMTrans02"]);
                        //S_S6F11(_CEIDcontrol.CEIDList["WaferStart"]);

                        //CEID_List.Add(new CEID_Obj("SubstLocSMTrans02"));
                        CEID_List.Add(new CEID_Obj("WaferStart"));

                    }
                    //S_S6F11(_CEIDcontrol.CEIDList["WaferGet"]);
                    //S_S6F11(_CEIDcontrol.CEIDList["GetWaferComplete"]);
                    CEID_List.Add(new CEID_Obj("WaferGet"));
                    CEID_List.Add(new CEID_Obj("GetWaferComplete"));

                }
                SetupDVID_EVENT(CEID_List, DVID_List);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void SecsGEMUtilty_OnRobotUppArmTake(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();
            try
            {
                I_Robot Robot = (I_Robot)sender;

                lock (objectWaferData)
                {
                    TempWafer = e.Wafer;
                    WriteLog($"SetUp TempWafer By OnRobotUppArmTake() FoupID={TempWafer.FoupID},LotID={TempWafer.LotID},Slot={TempWafer.Slot}");
                    string strWaferID = TempWafer.WaferID_B == "" ? TempWafer.WaferInforID_B : TempWafer.WaferID_B;

                    /*_VIDcontrol.DVIDList["SubstID"].CurrentValue = strWaferID;
                    _VIDcontrol.DVIDList["WaferScribeGetPutWaferCmp"].CurrentValue = strWaferID;

                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = TempWafer.FoupID;
                    _VIDcontrol.DVIDList["PortID"].CurrentValue = (int)TempWafer.Owner;
                    */
                    DVID_List.Add(new DVID_Obj("SubstID", strWaferID));
                        DVID_List.Add(new DVID_Obj("T7_WaferID", strWaferID));
                    DVID_List.Add(new DVID_Obj("WaferScribeGetPutWaferCmp", strWaferID));
                    DVID_List.Add(new DVID_Obj("CarrierID", TempWafer.FoupID));
                    DVID_List.Add(new DVID_Obj("PortID", (int)TempWafer.Owner));

                    if (Robot.BodyNo == 1)
                    {
                        //_VIDcontrol.DVIDList["SubstLocID"].CurrentValue = "RobotA_UpperArm";
                        DVID_List.Add(new DVID_Obj("SubstLocID", "RobotA_UpperArm"));
                    }
                    else
                    {
                        //_VIDcontrol.DVIDList["SubstLocID"].CurrentValue = "RobotB_UpperArm";
                        DVID_List.Add(new DVID_Obj("SubstLocID", "RobotB_UpperArm"));
                    }
                    /*_VIDcontrol.DVIDList["EndEffectorGetPutWaferCmp"].CurrentValue = 1;
                    _VIDcontrol.DVIDList["SlotNumGetPutWaferCmp"].CurrentValue = TempWafer.Slot;

                    _VIDcontrol.DVIDList["CtrlJobID"].CurrentValue = TempWafer.CJID;
                    _VIDcontrol.DVIDList["PRJobId"].CurrentValue = TempWafer.PJID;
                    */
                    DVID_List.Add(new DVID_Obj("EndEffectorGetPutWaferCmp", 1));
                    DVID_List.Add(new DVID_Obj("SlotNumGetPutWaferCmp", TempWafer.Slot));
                    DVID_List.Add(new DVID_Obj("CtrlJobID", TempWafer.CJID));
                    DVID_List.Add(new DVID_Obj("PRJobId", TempWafer.PJID));
                    DVID_List.Add(new DVID_Obj("SubstLocID", TempWafer.Position.ToString()));



                    if (TempWafer.ProcessStatus == SWafer.enumProcessStatus.WaitProcess)  // 剛出LP or Tower -> unoccupied
                    {
                        //S_S6F11(_CEIDcontrol.CEIDList["SubstLocSMTrans02"]);
                        //S_S6F11(_CEIDcontrol.CEIDList["WaferStart"]);

                        //CEID_List.Add(new CEID_Obj("SubstLocSMTrans02"));
                        CEID_List.Add(new CEID_Obj("WaferStart"));
                    }
                    //S_S6F11(_CEIDcontrol.CEIDList["WaferGet"]);
                    //S_S6F11(_CEIDcontrol.CEIDList["GetWaferComplete"]);
                    CEID_List.Add(new CEID_Obj("WaferGet"));
                    CEID_List.Add(new CEID_Obj("GetWaferComplete"));
                }

                SetupDVID_EVENT(CEID_List, DVID_List);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }






        private void SecsGEMUtilty_OnWaferInAlinger(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {

        }

        private void SecsGEMUtilty_OnPortUnDocked(object sender, FoupChangeEventEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            lock (objectPortStates)
            {
                try
                {
                    /*_VIDcontrol.DVIDList["PortID"].CurrentValue = e.PortNo;
                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = e.CarrierID;
                    _VIDcontrol.DVIDList["PortTransferState"].CurrentValue = (int)SecsGEMUtilty.LoadPortList[e.PortNo].E84Status;
                    */
                    DVID_List.Add(new DVID_Obj("PortID", e.PortNo));
                    DVID_List.Add(new DVID_Obj("CarrierID", e.CarrierID));
                    DVID_List.Add(new DVID_Obj("PortTransferState", (int)SecsGEMUtilty.LoadPortList[e.PortNo].E84Status));

                    // S_S6F11(_CEIDcontrol.CEIDList[string.Format("Port{0}UnDocked", e.PortNo.ToString())]);
                    /*S_S6F11(_CEIDcontrol.CEIDList["CarrierUndocked"]);
                    S_S6F11(_CEIDcontrol.CEIDList["CarrierUnClamped"]);
                    S_S6F11(_CEIDcontrol.CEIDList["PortTransferSMTrans09"]); //  TransferBlock to ReadytoUnload
					*/
                    CEID_List.Add(new CEID_Obj("CarrierUndocked"));
                    CEID_List.Add(new CEID_Obj("CarrierUnClamped"));
                    CEID_List.Add(new CEID_Obj("PortTransferSMTrans09"));



                    // S_S6F11(_CEIDcontrol.CEIDList["ReadyToUnload"]);
                    /// CurrntGEMProcessStats = GEMProcessStats.FOUPUnClamp;
                    // CurrntGEMProcessStats = GEMProcessStats.PodReadyToMoveOut;
                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                }
            }
            SetupDVID_EVENT(CEID_List, DVID_List);
        }

        private void SecsGEMUtilty_OnPortDocked(object sender, FoupChangeEventEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();
            try
            {
                /*_VIDcontrol.DVIDList["PortID"].CurrentValue = e.PortNo;
                _VIDcontrol.DVIDList["CarrierID"].CurrentValue = e.CarrierID;
                _VIDcontrol.DVIDList["CarrierSlotMap"].CurrentValue = e.MappingData;
                */
                DVID_List.Add(new DVID_Obj("PortID", e.PortNo));
                DVID_List.Add(new DVID_Obj("CarrierID", e.CarrierID));
                DVID_List.Add(new DVID_Obj("CarrierSlotMap", e.MappingData));

                SecsGEMUtilty.SetCarrierSlotMapStats(e.PortNo, CarrierSlotMapStats.SlotMappingOK);
                //_VIDcontrol.DVIDList["CarrierSlotMapStatus"].CurrentValue = (int)SecsGEMUtilty.GetSlotMappingStats(e.PortNo);
                DVID_List.Add(new DVID_Obj("CarrierSlotMapStatus", (int)SecsGEMUtilty.GetSlotMappingStats(e.PortNo)));

                //  S_S6F11(_CEIDcontrol.CEIDList[string.Format("Port{0}Docked", e.PortNo.ToString())]);

                /*S_S6F11(_CEIDcontrol.CEIDList["CarrierDocked"]);
                S_S6F11(_CEIDcontrol.CEIDList["MappingComplete"]);
                SecsGEMUtilty.SetCarrierState(e.PortNo, CarrierState.CarrierSMTrans14);
                S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans14"]); // waiting for Host
                */

                SecsGEMUtilty.SetCarrierState(e.PortNo, CarrierState.CarrierSMTrans14);
                CEID_List.Add(new CEID_Obj("CarrierDocked"));
                CEID_List.Add(new CEID_Obj("MappingComplete"));
                CEID_List.Add(new CEID_Obj("CarrierSMTrans14"));// waiting for Host

                SetupDVID_EVENT(CEID_List, DVID_List);

                if (SecsGEMUtilty.LoadPortList[e.PortNo].UndockQueueByHost == true) // 要退foup ....
                {
                    SecsGEMUtilty.LoadPortList[e.PortNo].UndockQueueByHost = false;
                    SecsGEMUtilty.UnDock(e.PortNo);
                }

                // CurrntGEMProcessStats = GEMProcessStats.FOUPReady;
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void SecsGEMUtilty_OnPortClamped(object sender, FoupChangeEventEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            lock (objectPortStates)
            {
                try
                {
                    /*_VIDcontrol.DVIDList["PortID"].CurrentValue = e.PortNo;
                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = e.CarrierID;
                    */
                    DVID_List.Add(new DVID_Obj("PortID", e.PortNo));
                    DVID_List.Add(new DVID_Obj("CarrierID", e.CarrierID));


                    /*

                // CurrntGEMProcessStats = GEMProcessStats.FOUPClamp;
                    SecsGEMUtilty.SetCarrierIDstatus(e.PortNo, CarrierIDStats.IDRead);
                    // S_S6F11(_CEIDcontrol.CEIDList["CarrierIDRead"]);
                    S_S6F11(_CEIDcontrol.CEIDList["CarrierClamped"]);
                    // S_S6F11(_CEIDcontrol.CEIDList[string.Format("Port{0}Clamp", e.PortNo.ToString())]);
                    SecsGEMUtilty.SetCarrierState(e.PortNo, CarrierState.CarrierSMTrans03);
                    S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans03"]);
                    */

                    SecsGEMUtilty.SetCarrierIDstatus(e.PortNo, CarrierIDStats.IDRead);
                    SecsGEMUtilty.SetCarrierState(e.PortNo, CarrierState.CarrierSMTrans03);
                    CEID_List.Add(new CEID_Obj("CarrierClamped"));
                    CEID_List.Add(new CEID_Obj("CarrierSMTrans03"));


                    // CurrntGEMProcessStats = GEMProcessStats.ComMID;
                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                }
            }
            SetupDVID_EVENT(CEID_List, DVID_List);
        }

        private void SecsGEMUtilty_OnPortUnClamped(object sender, FoupChangeEventEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            lock (objectPortStates)
            {
                try
                {
                    /*_VIDcontrol.DVIDList["PortID"].CurrentValue = e.PortNo;
                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = e.CarrierID;
                    S_S6F11(_CEIDcontrol.CEIDList["CarrierUnClamped"]);

                    //S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans09"]);
                    S_S6F11(_CEIDcontrol.CEIDList["PortTransferSMTrans09"]); //  TransferBlock to ReadytoUnload
                    */

                    DVID_List.Add(new DVID_Obj("PortID", e.PortNo));
                    DVID_List.Add(new DVID_Obj("CarrierID", e.CarrierID));
                    CEID_List.Add(new CEID_Obj("CarrierUnClamped"));
                    CEID_List.Add(new CEID_Obj("PortTransferSMTrans09"));



                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                }
            }

            SetupDVID_EVENT(CEID_List, DVID_List);
        }



        private void SecsGEMUtilty_OnPortFoupExistChenge(object sender, FoupChangeEventEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            lock (objectPortStates)
            {
                try
                {
                    if (e.FoupExist)
                    {
                        /*_VIDcontrol.DVIDList["PortID"].CurrentValue = e.PortNo;
                        _VIDcontrol.DVIDList["CarrierID"].CurrentValue = "";
                        */
                        DVID_List.Add(new DVID_Obj("PortID", e.PortNo));
                        DVID_List.Add(new DVID_Obj("CarrierID", ""));


                        SecsGEMUtilty.SetCarrierIDstatus(e.PortNo, CarrierIDStats.NoStatus);
                        SecsGEMUtilty.SetCarrierAccessStats(e.PortNo, CarrierAccessStats.NotAccess);

                        /*S_S6F11(_CEIDcontrol.CEIDList["GemMaterialReceivedEvent"]);
                        // S_S6F11(_CEIDcontrol.CEIDList[string.Format("Port{0}Arrive", e.PortNo.ToString())]);
                        SecsGEMUtilty.SetCarrierState(e.PortNo, CarrierState.CarrierSMTrans01);
                        S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans01"]);
                        */
                        SecsGEMUtilty.SetCarrierState(e.PortNo, CarrierState.CarrierSMTrans01);

                        CEID_List.Add(new CEID_Obj("GemMaterialReceivedEvent"));
                        CEID_List.Add(new CEID_Obj("CarrierSMTrans01"));

                        //if (this.GEMControlStatus == GEMControlStats.ONLINEREMOTE)
                        //    SecsGEMUtilty.Clamp(e.PortNo);

                    }
                    else
                    {
                        /*_VIDcontrol.DVIDList["PortID"].CurrentValue = e.PortNo;
                        _VIDcontrol.DVIDList["CarrierID"].CurrentValue = e.CarrierID;
                        */
                        DVID_List.Add(new DVID_Obj("PortID", e.PortNo));
                        DVID_List.Add(new DVID_Obj("CarrierID", e.CarrierID));

                        SecsGEMUtilty.SetCarrierIDstatus(e.PortNo, CarrierIDStats.NoStatus);
                        SecsGEMUtilty.SetCarrierSlotMapStats(e.PortNo, CarrierSlotMapStats.NoStatus);
                        SecsGEMUtilty.SetCarrierAccessStats(e.PortNo, CarrierAccessStats.NoStatus);

                        /*S_S6F11(_CEIDcontrol.CEIDList["GemMaterialRemovedEvent"]);
                        // S_S6F11(_CEIDcontrol.CEIDList[string.Format("Port{0}Remove", e.PortNo.ToString())]);
                        SecsGEMUtilty.SetCarrierState(e.PortNo, CarrierState.CarrierSMTrans21);
                        S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans21"]);
                        S_S6F11(_CEIDcontrol.CEIDList["PortTransferSMTrans08"]);
                        */
                        SecsGEMUtilty.SetCarrierState(e.PortNo, CarrierState.CarrierSMTrans21);
                        CEID_List.Add(new CEID_Obj("GemMaterialRemovedEvent"));
                        CEID_List.Add(new CEID_Obj("CarrierSMTrans21"));
                        CEID_List.Add(new CEID_Obj("PortTransferSMTrans08"));

                        //  CurrntGEMProcessStats = GEMProcessStats.Idle;
                        if (_hostwaferInfo.ContainsKey(e.CarrierID))
                            _hostwaferInfo.Remove(e.CarrierID);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                }
            }
            SetupDVID_EVENT(CEID_List, DVID_List);
        }


        //LoadPort Status
        private void SecsGEMUtilty_OnLoadPortStatusChange(object sender, OccurStateMachineChangEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();


            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int PortNo = loaderUnit.BodyNo;
            string CarrierID = loaderUnit.FoupID;
            enumStateMachine eStatus = e.StatusMachine;
            switch (eStatus)
            {
                case enumStateMachine.PS_Abort:

                    break;
                case enumStateMachine.PS_Arrived:

                    break;
                case enumStateMachine.PS_Clamped:

                    break;
                case enumStateMachine.PS_Complete:

                    SecsGEMUtilty.SetCarrierState(PortNo, CarrierState.CarrierSMTrans19);
                    SecsGEMUtilty.SetCarrierAccessStats(PortNo, CarrierAccessStats.CarrierComplete);

                    /*_VIDcontrol.DVIDList["CarrierAccessingStatus"].CurrentValue = (int)SecsGEMUtilty.GetCarrierAccessStats(PortNo);
                    _VIDcontrol.DVIDList["PortID"].CurrentValue = PortNo;
                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = CarrierID;
                    S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans19"]);
                    */

                    DVID_List.Add(new DVID_Obj("CarrierAccessingStatus", (int)SecsGEMUtilty.GetCarrierAccessStats(PortNo)));
                    DVID_List.Add(new DVID_Obj("PortID", PortNo));
                    DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                    CEID_List.Add(new CEID_Obj("CarrierSMTrans19"));

                    SetupDVID_EVENT(CEID_List, DVID_List);

                    break;
                case enumStateMachine.PS_Disable:

                    break;
                case enumStateMachine.PS_Docked:

                    break;
                case enumStateMachine.PS_Docking:

                    break;
                case enumStateMachine.PS_Error:

                    break;
                case enumStateMachine.PS_FoupOn:

                    break;
                case enumStateMachine.PS_FuncSetup:

                    break;
                case enumStateMachine.PS_FuncSetupNG:

                    break;
                case enumStateMachine.PS_Process:

                    break;
                case enumStateMachine.PS_ReadyToLoad:

                    break;
                case enumStateMachine.PS_ReadyToUnload:

                    break;
                case enumStateMachine.PS_Removed:

                    break;
                case enumStateMachine.PS_Stop:
                    SecsGEMUtilty.SetCarrierState(PortNo, CarrierState.CarrierSMTrans20);
                    SecsGEMUtilty.SetCarrierAccessStats(PortNo, CarrierAccessStats.CarrierStop);

                    /*_VIDcontrol.DVIDList["CarrierAccessingStatus"].CurrentValue = (int)SecsGEMUtilty.GetCarrierAccessStats(PortNo);
                    _VIDcontrol.DVIDList["PortID"].CurrentValue = PortNo;
                    _VIDcontrol.DVIDList["CarrierID"].CurrentValue = CarrierID;
                    S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans20"]);
                    */

                    DVID_List.Add(new DVID_Obj("CarrierAccessingStatus", (int)SecsGEMUtilty.GetCarrierAccessStats(PortNo)));
                    DVID_List.Add(new DVID_Obj("PortID", PortNo));
                    DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                    CEID_List.Add(new CEID_Obj("CarrierSMTrans20"));

                    SetupDVID_EVENT(CEID_List, DVID_List);



                    break;
                case enumStateMachine.PS_UnClamped:

                    break;
                case enumStateMachine.PS_UnDocked:

                    break;
                case enumStateMachine.PS_UnDocking:

                    break;
                case enumStateMachine.PS_Unknown:

                    break;
            }


        }

        void RumSECSStart()
        {
            _secsdriver.InitSecs();
            _DB.ResetAllAlarm();
            if (OnSECSOpen != null)
                OnSECSOpen(this, new EventArgs());
            SECSOpenbusy = false;
        }

        public void SECSStart()
        {
            SECSOpenbusy = true;
            _exeSECSStart.Set();
        }

        public void SECSStop()
        {
            SECSClosebusy = true;
            _exeSECSStop.Set();
        }

        public void RunSECSStop()
        {
            _secsdriver.SecsStop();
            if (OnSECSClose != null)
                OnSECSClose(this, new EventArgs());
            SECSClosebusy = false;

        }

        private void _secsdriver_InternalStatusChange(SecsInternalArgs Internal)
        {
            if (Internal.GetInternalConnect)
            {

                SetGEMControlStatus = GEMControlStats.ATTEMTPONLINE;

            }
            else
            {

                SetGEMControlStatus = GEMControlStats.OFFLINE;
            }

        }

        private void _secsdriver_ReceiveSecsMessage(SecsMessageObject1Args secsMsg)
        {
            if (_GEMControlstats != GEMControlStats.ONLINELOCAL && _GEMControlstats != GEMControlStats.ONLINEREMOTE)
            {
                if (_GEMControlstats == GEMControlStats.ATTEMTPONLINE)
                {
                    if (!CheckConnecteFirstCMD(secsMsg) && !(secsMsg.Station == QsStream.S1 && secsMsg.Function == QsFunction.F0)) return;
                }
                else if (_GEMControlstats == GEMControlStats.HOSTOFFLINE)
                {
                    if (!(secsMsg.Station == QsStream.S1 && secsMsg.Function == QsFunction.F17)
                        && !(secsMsg.Station == QsStream.S1 && secsMsg.Function == QsFunction.F13))
                        return;
                }
                else
                {
                    if (!(secsMsg.Station == QsStream.S1 && secsMsg.Function == QsFunction.F17))
                    {
                        switch (secsMsg.Station)
                        {
                            case QsStream.S1:
                                S_S1F0(secsMsg.GetSecsMessage, secsMsg);
                                break;

                        }
                        return;
                    }
                }
            }
            switch (secsMsg.Station)
            {
                case QsStream.S1:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F0: R_S1F0(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F1: R_S1F1(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F2: R_S1F2(secsMsg.GetSecsMessage); break;
                        case QsFunction.F3: R_S1F3(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F4: break;
                        case QsFunction.F11: R_S1F11(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F12: break;
                        case QsFunction.F13: R_S1F13(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F14: break;
                        case QsFunction.F15: R_S1F15(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F16: break;
                        case QsFunction.F17: R_S1F17(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F18: break;
                        case QsFunction.F65: break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            // S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S2:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F13: R_S2F13(secsMsg.GetSecsMessage, secsMsg); break; // requst ECID 
                        case QsFunction.F14: break;
                        case QsFunction.F15: R_S2F15(secsMsg.GetSecsMessage, secsMsg); break; // modify ECID
                        case QsFunction.F16: break;
                        case QsFunction.F17: R_S2F17(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F18: break;
                        case QsFunction.F23: R_S2F23(secsMsg.GetSecsMessage, secsMsg); break; //FDC
                        case QsFunction.F24: break;
                        case QsFunction.F29: R_S2F29(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F30: break;
                        case QsFunction.F31: R_S2F31(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F32: break;
                        case QsFunction.F33: R_S2F33(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F34: break;
                        case QsFunction.F35: R_S2F35(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F36: break;
                        case QsFunction.F37: R_S2F37(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F38: break;
                        case QsFunction.F41: R_S2F41(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F42: break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            //  S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S3:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F17: R_S3F17(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F18: break;
                        case QsFunction.F27: R_S3F27(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F28: break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            // S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S5:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F1: break;
                        case QsFunction.F2: break;
                        case QsFunction.F3: R_S5F3(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F4: break;
                        case QsFunction.F5: R_S5F5(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F6: break;
                        case QsFunction.F7: R_S5F7(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F8: break;

                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            //  S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S6:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F1: break;
                        case QsFunction.F2: break;
                        case QsFunction.F11: break;
                        case QsFunction.F12: break;
                        case QsFunction.F15: R_S6F15(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F16: break;
                        case QsFunction.F19: R_S6F19(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F20: break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            // S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S7:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F19: R_S7F19(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F20: break;
                        case QsFunction.F25: R_S7F25(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F26: break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            // S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S9:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F1: break;
                        case QsFunction.F2: break;
                        case QsFunction.F3: break;
                        case QsFunction.F4: break;
                        case QsFunction.F5: break;
                        case QsFunction.F6: break;
                        case QsFunction.F7: break;
                        case QsFunction.F8: break;
                        case QsFunction.F9: break;
                        case QsFunction.F10: break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            // S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S10:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F1: R_S10F1(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F2: break;
                        case QsFunction.F3: R_S10F3(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F4: break;
                        case QsFunction.F5: R_S10F5(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F6: break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            // S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S14:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F1: R_S14F1(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F7: R_S14F7(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F9: R_S14F9(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F10: break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            //   S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                case QsStream.S16:
                    switch (secsMsg.Function)
                    {
                        case QsFunction.F5: R_S16F5(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F11: R_S16F11(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F15: R_S16F15(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F17: R_S16F17(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F16: break;
                        case QsFunction.F19: R_S16F19(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F21: R_S16F21(secsMsg.GetSecsMessage, secsMsg); break;
                        case QsFunction.F27: R_S16F27(secsMsg.GetSecsMessage, secsMsg); break;
                        default:
                            WriteLog(string.Format("<<<Error>>> Undefined message ID {0}{1}.", secsMsg.Station, secsMsg.Function));
                            //  S_S9Fx(QsFunction.F5, sfData);
                            break;
                    }
                    break;
                default:
                    break;
            }


        }

        bool CheckConnecteFirstCMD(SecsMessageObject1Args secsMsg)
        {
            if ((secsMsg.Station == _parameter.GetSECSParameterConfig.ConnectFunction.Stream &&
                (secsMsg.Function == _parameter.GetSECSParameterConfig.ConnectFunction.Function
                || secsMsg.Function == _parameter.GetSECSParameterConfig.ConnectFunction.Function + 1)))
                return true;

            return false;
        }

        private void _PollingHotbeat_DoPolling()
        {
            if (_parameter.GetSECSParameterConfig.ConnectFunction.Stream == QsStream.S1 && _parameter.GetSECSParameterConfig.ConnectFunction.Function == QsFunction.F13)
                S_S1F13();
            else if (_parameter.GetSECSParameterConfig.ConnectFunction.Stream == QsStream.S1 && _parameter.GetSECSParameterConfig.ConnectFunction.Function == QsFunction.F1)
                S_S1F1();
        }



        /// <summary>
        /// S1FX 
        /// </summary>
        void S_S1F1()
        {
            object secsMsg = new object();
            _secsdriver.DataIteminitoutRequest(QsStream.S1, QsFunction.F1, true, ref secsMsg);
            _secsdriver.SendMessage(QsStream.S1, QsFunction.F1, secsMsg);

        }
        void R_S1F1(object secsMsg, SecsMessageObject1Args secsObject)
        {
            S_S1F2(secsMsg, secsObject);
        }
        void S_S1F2(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                string name = "Gem300";
                string GemVersion = Application.ProductVersion;

                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, (object)name);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, (object)GemVersion);
                _secsdriver.SendMessage(QsStream.S1, QsFunction.F2, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        void R_S1F2(object secsMsg)
        {
            //  _PollingHotbeat.Reset();
            if (_GEMControlstats == GEMControlStats.ATTEMTPONLINE)
            {
                SetGEMControlStatus = _parameter.GetSECSParameterConfig.OnlineSubStats;

                if (_parameter.GetSECSParameterConfig.OnlineSubStats == GEMControlStats.ONLINEREMOTE)
                {


                }
            }
        }
        void S_S1F0(object secsMsg, SecsMessageObject1Args secsObject)
        {
            _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
            _secsdriver.SendMessage(QsStream.S1, QsFunction.F0, secsMsg, secsObject);

        }
        void R_S1F0(object secsMsg, SecsMessageObject1Args secsObject)
        {


        }
        void R_S1F3(object secsMsg, SecsMessageObject1Args secsObject)
        {
            List<int> VIDs = new List<int>();
            try
            {
                object Value = new object();

                int listCount = 0;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                listCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < listCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                    VIDs.Add((int)Value);
                }
                if (listCount == 0)
                {
                    //foreach (VIDObject vid in _VIDcontrol.DVIDList.Values)
                    //    VIDs.Add(vid.VID);
                    foreach (VIDObject vid in _VIDcontrol.SVIDList.Values)
                        VIDs.Add(vid.VID);
                    //foreach (VIDObject vid in _VIDcontrol.FDCList.Values)
                    //    VIDs.Add(vid.VID);
                }
                S_S1F4(secsMsg, VIDs, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                S_S1F4(secsMsg, VIDs, secsObject);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                S_S1F4(secsMsg, VIDs, secsObject);
            }
        }
        void S_S1F4(object secsMsg, List<int> VIDs, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, VIDs.Count);
                for (int i = 0; i < VIDs.Count; i++)
                {
                    AssignVIDValue(ref secsMsg, VIDs[i]);
                }
                _secsdriver.SendMessage(QsStream.S1, QsFunction.F4, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }

        }

        void S_S1F13()
        {
            try
            {
                object secsMsg = new object();
                string name = "Gem300";
                string GemVersion = Application.ProductVersion;
                _secsdriver.DataIteminitoutRequest(QsStream.S1, QsFunction.F13, true, ref secsMsg);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);//list有2個
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, (object)name);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, (object)GemVersion);
                _secsdriver.SendMessage(QsStream.S1, QsFunction.F13, secsMsg);
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S1F3 " + ex);
            }
        }
        void R_S1F13(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {

                if (_GEMControlstats == GEMControlStats.ATTEMTPONLINE || _GEMControlstats == GEMControlStats.HOSTOFFLINE)
                {
                    _PollingHotbeat.Reset();
                    //_EQ.SetFirstOnlineReset(0);
                    //if (_parameter.GetSECSParameterConfig.OnlineSubStats == GEMControlStats.ONLINEREMOTE)
                    //{
                    //    _EQ.GoRemote();

                    //}
                    S_S1F14(secsMsg, secsObject);
                    //  SetSecsStatus = _SecsParameter.OnlineSubStats; // Setting Mode
                    SetGEMControlStatus = _parameter.GetSECSParameterConfig.OnlineSubStats;
                    return;
                }
                S_S1F14(secsMsg, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                // Go Remote Fail
                WriteLog("<Secs_Error> S1F13 " + ex);

                SetGEMControlStatus = GEMControlStats.ONLINELOCAL;
                S_S1F14(secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S1F13 " + ex);
            }
        }
        void S_S1F14(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                string name = "Gem300";
                string GemVersion = Application.ProductVersion;
                int binary = 0;
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);

                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, binary, 1);  //b 0x00

                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, (object)name);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, (object)GemVersion);
                _secsdriver.SendMessage(QsStream.S1, QsFunction.F14, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S1F4 " + ex);
            }
        }

        void R_S1F15(object secsMsg, SecsMessageObject1Args secsObject)
        {
            S_S1F16(secsMsg, secsObject);
        }
        void S_S1F16(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                int binary = 0;
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, binary, 1);  //b 0x00
                _secsdriver.SendMessage(QsStream.S1, QsFunction.F16, secsMsg, secsObject);
                if (binary == 0)
                {
                    // S_S6F11(CEIDList["GemEquipmentOFFLINE"]);
                    // _PreSECSStatus = _SECSStatus;
                    //  SetSecsStatus = SECSStats.HOSTOFFLINE;
                    //  _EQP.GoOffline();
                    // _EQ.GoOffline();
                    SetGEMControlStatus = GEMControlStats.HOSTOFFLINE;
                }
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S1F16 " + ex);
            }
        }

        void R_S1F17(object secsMsg, SecsMessageObject1Args secsObject)
        {
            S_S1F18(secsMsg, secsObject);
        }
        void S_S1F18(object secsMsg, SecsMessageObject1Args secsObject)
        {
            // GEMControlStats LastControlStats = _preGEMControlstats;
            try
            {
                int binary = 0;
                if (_GEMControlstats >= GEMControlStats.ONLINELOCAL)
                    binary = 2;
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, binary, 1);  //b 0x00
                _secsdriver.SendMessage(QsStream.S1, QsFunction.F18, secsMsg, secsObject);
                if (binary == 0)
                {
                    //SetSecsStatus = _PreSECSStatus;
                    SetGEMControlStatus = _parameter.GetSECSParameterConfig.OnlineSubStats;

                    //_EQP.Goline();

                    if (_GEMControlstats == GEMControlStats.ONLINEREMOTE)
                    {
                        //   _EQ.GoOnlineToRemote();
                    }
                    // _EQP.GoRomte();
                    else
                    {
                        //  _EQ.Goline();
                    }
                    //  _EQP.GoLocal();
                }
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S1F18 " + ex);
            }
        }

        void R_S1F11(object secsMsg, SecsMessageObject1Args secsObject)
        {
            List<int> VIDs = new List<int>();
            try
            {
                object Value = new object();

                int listCount = 0;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                listCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < listCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                    VIDs.Add((int)Value);
                }
                if (listCount == 0)
                {
                    foreach (VIDObject vid in _VIDcontrol.SVIDList.Values)
                        VIDs.Add(vid.VID);

                }
                S_S1F12(secsMsg, VIDs, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                S_S1F12(secsMsg, VIDs, secsObject);
        }
            catch (Exception ex)
        {
                Console.WriteLine("{0} Exception caught.", ex);
                S_S1F12(secsMsg, VIDs, secsObject);
            }



        }
        void S_S1F12(object secsMsg, List<int> VIDs, SecsMessageObject1Args secsObject)
        {
            _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);

            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, VIDs.Count);
            for (int i = 0; i < VIDs.Count; i++)
            {
                foreach (VIDObject vid in _VIDcontrol.SVIDList.Values)
                {
                    if (VIDs[i] == vid.VID)
                    {
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, vid.VID);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, vid.VIDName);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, vid.Unit);
                        break;
            }

                }
            }


            /*foreach (VIDObject vid in _VIDcontrol.DVIDList.Values)
            {
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, vid.VID);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, vid.VIDName);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, vid.Unit);
            }*/

            _secsdriver.SendMessage(QsStream.S1, QsFunction.F12, secsMsg, secsObject);

        }

        void R_S1F65(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                S_S1F66(secsMsg, secsObject);
                //if (_SECSStatus == SECSStats.ATTEMTPONLINE)
                //{
                //    //  _PollingHotbeat.Reset();
                //    SetSecsStatus = _SecsParameter.OnlineSubStats; // Setting Mode

                //}
            }
            catch (StreamFuntionException ex)
            {
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S1F65 " + ex);
            }
        }
        void S_S1F66(object secsMsg, SecsMessageObject1Args secsObject)
        {

            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                _secsdriver.SendMessage(QsStream.S1, QsFunction.F66, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> SF66 " + ex);
            }
        }


        /// <summary>
        /// S2FX 
        /// </summary>
        void R_S2F13(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                List<int> ECIDs = new List<int>();
                object Value = new object();

                int listCount = 0;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                listCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < listCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);


                    ECIDs.Add((int)Value);
                }
                if (listCount == 0)
                {
                    foreach (VIDObject vid in _VIDcontrol.ECIDList.Values)
                        ECIDs.Add(vid.VID);
                }


                S_S2F14(secsMsg, secsObject, ECIDs);
            }
            catch (StreamFuntionException ex)
            {
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S2F13 " + ex);
            }
        }
        void S_S2F14(object secsMsg, SecsMessageObject1Args secsObject, List<int> ECIDs)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, ECIDs.Count);
                for (int i = 0; i < ECIDs.Count; i++)
                {
                    AssignECIDValue(ref secsMsg, ECIDs[i]);
                }
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F14, secsMsg, secsObject);

            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S2F14 " + ex);
            }
        }

        void R_S2F15(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int Ack = 0;  // 0:OK,1: not find ECID , 2: Tool is busy , 3: ECID Out of range, 4 : Formate illegal , 5: Others error
            Dictionary<int, object> ChangeEClist = new Dictionary<int, object>();
            int TempECID = 0;
            int listCount = 0;
            int valueMax = 0;
            int valueMin = 0;

            try
            {
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                object Value = new object();
                listCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < listCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                    _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                    TempECID = (int)Value;
                    if (_VIDcontrol.ECIDList.ContainsKey(TempECID) == true)
                    {
                        _secsdriver.DataItemIn(_VIDcontrol.ECIDList[TempECID].ValueType, ref secsMsg, ref Value);

                        switch (_VIDcontrol.ECIDList[TempECID].ValueType)
                        {
                            case SecsFormateType.U1:  // int need check max min...
                            case SecsFormateType.U2:
                            case SecsFormateType.U4:

                                if (_VIDcontrol.ECIDList[TempECID].Max != "" && _VIDcontrol.ECIDList[TempECID].Min != "")
                                {
                                    if (int.TryParse(_VIDcontrol.ECIDList[TempECID].Max, out valueMax) == true &&
                                          int.TryParse(_VIDcontrol.ECIDList[TempECID].Min, out valueMin) == true)
                                    {

                                        if (!((int)Value >= valueMin && (int)Value <= valueMax))
                                        {
                                            Ack = 3; //ECID Out of range,
                                            WriteLog(string.Format("<<<Warning>>> EC not found in. EC={0}", TempECID));
                                        }

                                    }



                                }
                                break;
                        }

                        if (Ack == 0)
                        {
                            if (ChangeEClist.ContainsKey(TempECID) == false)
                                ChangeEClist.Add(TempECID, Value);

                        }


                    }
                    else
                    {
                        Ack = 1; //ECID not found!!
                        WriteLog(string.Format("<<<Warning>>> EC not found in. EC={0}", TempECID));
                        break;
                    }

                }

                S_S2F16(secsMsg, Ack, ChangeEClist, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                Ack = 4;
                S_S2F16(secsMsg, Ack, ChangeEClist, secsObject);

                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                Ack = 5;
                S_S2F16(secsMsg, Ack, ChangeEClist, secsObject);
                WriteLog("<Secs_Error> S2F15 " + ex);
            }
        }
        void S_S2F16(object secsMsg, int ACK, Dictionary<int, object> ECChang, SecsMessageObject1Args secsObject)
        {
            try
            {


                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, ACK);
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F16, secsMsg, secsObject);

                if (ACK == 0 && ECChang.Count > 0)
                {
                    foreach (int ECID in ECChang.Keys)
                        UpdateECIDValue(ECID, ECChang[ECID]);

                }


            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S2F16 " + ex);
            }
        }

        void R_S2F17(object secsMsg, SecsMessageObject1Args secsObject)
        {

            S_S2F18(secsMsg, secsObject);

        }
        void S_S2F18(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                string NowTime = DateTime.Now.ToString("yyyyMMddHHmmssff");
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, (object)NowTime);
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F18, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<Secs_Error> S2F18 " + ex);
            }
        }

        void R_S2F31(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            try
            {
                object Value = new object();
                string SetTime = string.Empty;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                SetTime = (string)Value;


                DateTime dt = DateTime.ParseExact(SetTime, "yyyyMMddHHmmssff", CultureInfo.InvariantCulture);
                SystemTime st = new SystemTime();
                st.wYear = (ushort)dt.Year;
                st.wMonth = (ushort)dt.Month;
                st.wDay = (ushort)dt.Day;
                st.wHour = (ushort)dt.Hour;
                st.wMinute = (ushort)dt.Minute;
                st.wSecond = (ushort)dt.Second;
                st.wMilliseconds = (ushort)dt.Millisecond;
                SetLocalTime(ref st);
                S_S2F32(secsMsg, nAck, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                WriteLog("<<<Warning>>>S2F31:{0}" + ex.Message);
                S_S2F32(secsMsg, nAck, secsObject);
            }
            catch (Exception ex)
            {
                nAck = 1;
                WriteLog("<<<Warning>>>S2F31:{0}" + ex.Message);
                S_S2F32(secsMsg, nAck, secsObject);
            }
        }
        void S_S2F32(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F32, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F32:{0}" + ex.Message);
            }
        }

        void R_S2F23(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            try
            {
                object Value = new object();
                int ListCount = 0;
                // int SubListCount = 0;
                int nTRID = 0;
                string strDSPER = string.Empty;
                int nTOTSMP = 0;
                int nREPGSZ = 0;
                int nSVID = 0;

                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                nTRID = (int)Value;

                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                strDSPER = (string)Value;
                _secsdriver.DataItemIn(SecsFormateType.U2, ref secsMsg, ref Value);
                nTOTSMP = (int)Value;
                if (!_dicTraceTOTSMP.ContainsKey(nTRID))
                    _dicTraceTOTSMP.Add(nTRID, nTOTSMP);

                _dicTraceTOTSMP[nTRID] = nTOTSMP;
                if (!_dicCurrentTOTSMP.ContainsKey(nTRID))
                    _dicCurrentTOTSMP.Add(nTRID, 0);

                _dicCurrentTOTSMP[nTRID] = 0;

                _secsdriver.DataItemIn(SecsFormateType.U2, ref secsMsg, ref Value);
                nREPGSZ = (int)Value;

                ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value); ;

                for (int i = 0; i < ListCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                    nSVID = (int)Value;
                    if (!_dicTraceContent.ContainsKey(nTRID))
                        _dicTraceContent.Add(nTRID, new List<int>());

                    _dicTraceContent[nTRID].Add(nSVID);
                }

                if (nTOTSMP == 0)
                {
                    _dicTraceContent.Remove(nTRID); //停止trace data
                    _dicTraceTOTSMP.Remove(nTRID);
                }
                S_S2F24(secsMsg, nAck, secsObject);

                _pollingTraceData.Set();

            }
            catch (StreamFuntionException ex)
            {
                WriteLog("<<<Warning>>>S2F24:{0}" + ex.Message);
                S_S2F24(secsMsg, ex.nAck, secsObject);
            }
            catch (Exception ex)
            {
                nAck = 1;
                WriteLog("<<<Warning>>>S2F24:{0}" + ex.Message);
                S_S2F24(secsMsg, nAck, secsObject);
            }
        }
        void S_S2F24(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {

            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F24, secsMsg, secsObject);

            }

            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F24:{0}" + ex.Message);
            }
        }

        void R_S2F29(object secsMsg, SecsMessageObject1Args secsObject)
        {
            List<int> ECIDs = new List<int>();
            try
            {

                object Value = new object();

                int listCount = 0;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                listCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < listCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                    ECIDs.Add((int)Value);
                }
                if (listCount == 0)
                {
                    foreach (VIDObject vid in _VIDcontrol.ECIDList.Values)
                        ECIDs.Add(vid.VID);


                }
                S_S2F30(secsMsg, ECIDs, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                // SSDR_170.SDRMSG msg = (SSDR_170.SDRMSG)secsObject.GetSecsMessage;
                // _logger.WriteLog("StreamFuncException,S1F3 Tick Num={0} , length={1},", secsObject._Tick, msg.length, msg);
                S_S2F30(secsMsg, ECIDs, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S2F30(secsMsg, ECIDs, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S2F30(object secsMsg, List<int> ECIDs, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, ECIDs.Count);
                for (int i = 0; i < ECIDs.Count; i++)
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 6);
                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, ECIDs[i]);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _VIDcontrol.ECIDList[ECIDs[i]].VIDName);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _VIDcontrol.ECIDList[ECIDs[i]].Min);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _VIDcontrol.ECIDList[ECIDs[i]].Max);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _VIDcontrol.ECIDList[ECIDs[i]].CurrentValue);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _VIDcontrol.ECIDList[ECIDs[i]].Unit);

                }
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F30, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        void R_S2F33(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            try
            {

                object Value = new object();
                int ListCount = 0;
                int SubListCount = 0;
                int RPID = 0;
                // int Lcount = 0;
                CommandAction Action; // 0 = Delete , 1= modify
                Dictionary<int, List<int>> _dicReports = new Dictionary<int, List<int>>(); //Report list

                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                Action = (CommandAction)Value;
                ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                if (ListCount == 0)
                    Action = CommandAction.Delete;
                else
                    Action = CommandAction.Modify;
                switch (Action)
                {
                    case CommandAction.Delete:
                        // Delete to DB
                        _DB.SQLExec("Delete * From  RPIDVIDLink");
                        break;
                    case CommandAction.Modify:
                        // ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        for (int i = 0; i < ListCount; i++)
                        {
                            _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                            RPID = (int)Value;
                            if (_dicReports.ContainsKey(RPID))
                            {
                                nAck = 3;
                                S_S2F34(secsMsg, nAck, secsObject);
                                WriteLog(string.Format("S2F33 PRID={0} is exist", RPID));
                                return;
                            }
                            _dicReports.Add(RPID, new List<int>());
                            SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            for (int j = 0; j < SubListCount; j++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                                _dicReports[RPID].Add((int)Value);
                            }

                        }
                        // insert to DB
                        DataSet VIDlist = _DB.Reader(string.Format("Select * From VIDList"));
                        DataSet RPIDVIDLinkList = _DB.Reader("Select DISTINCT RPID From RPIDVIDLink");
                        foreach (int nRPTID in _dicReports.Keys)
                        {
                            //if (RPIDVIDLinkList.Tables[0].AsEnumerable().Where(row => Convert.ToInt32(row["RPID"]) == nRPTID).Count() == 1)
                            //{
                            //    nAck = 3; //Report already
                            //    S_S2F34(secsMsg, nAck, secsObject);
                            //    _logger.WriteLog("S2F33 RPID={0} is  exist", nRPTID);
                            //    return;
                            //}
                            if (_dicReports[nRPTID].Count == 0)
                            {
                                _DB.SQLExec("Delete * From  RPIDVIDLink where RPID={0}", nRPTID);
                            }
                            else
                            {
                                foreach (int nVID in _dicReports[nRPTID])
                                {
                                    if (VIDlist.Tables[0].AsEnumerable().Where(row => Convert.ToInt32(row["VID"]) == nVID).Count() <= 0)
                                    {
                                        nAck = 4; //DVID not found.
                                        S_S2F34(secsMsg, nAck, secsObject);
                                        WriteLog(string.Format("S2F33 VID={0} is not exist", nVID));
                                        return;
                                    }
                                }
                            }

                        }
                        if (nAck == 0)
                        {
                            foreach (int nRPTID in _dicReports.Keys)
                            {
                                foreach (int nVID in _dicReports[nRPTID])
                                {
                                    WriteLog(string.Format("Define RPTID[{0}] link VID[{1}].", nRPTID, nVID));
                                    _DB.SQLExec("Insert Into RPIDVIDLink (RPID, VID) Values ({0}, {1})", nRPTID, nVID);
                                }
                            }
                        }
                        break;
                }

                S_S2F34(secsMsg, nAck, secsObject);

            }
            catch (StreamFuntionException ex)
            {
                WriteLog("<<<Warning>>>S2F33:" + ex.Message);
                S_S2F34(secsMsg, ex.nAck, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F33:" + ex.Message);
                nAck = 2;
                S_S2F34(secsMsg, nAck, secsObject);
            }
        }
        void S_S2F34(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F34, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F34:" + ex.Message);
            }

        }

        void R_S2F35(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            try
            {
                object Value = new object();
                int ListCount = 0;
                int SubListCount = 0;
                int CEID = 0;
                CommandAction Action; // 0 = Delete , 1= modify
                Dictionary<int, List<int>> _dicCEID = new Dictionary<int, List<int>>(); //Report list

                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value); ;
                Action = (ListCount > 0) ? CommandAction.Modify : CommandAction.Delete;

                switch (Action)
                {
                    case CommandAction.Delete:
                        // Detect to DB
                        _DB.SQLExec("Delete * From CEIDRPIDLink");
                        break;

                    case CommandAction.Modify:
                        for (int i = 0; i < ListCount; i++)
                        {
                            _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                            CEID = (int)Value;
                            if (_dicCEID.ContainsKey(CEID))
                            {
                                nAck = 3;
                                S_S2F36(secsMsg, nAck, secsObject);
                                WriteLog(string.Format("S2F35 CEID={0} already defined.", CEID));
                                return;
                            }
                            _dicCEID.Add(CEID, new List<int>());
                            SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            for (int j = 0; j < SubListCount; j++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                                _dicCEID[CEID].Add((int)Value);
                            }
                        }
                        // check CEID list
                        DataSet CEIDLists = _DB.Reader(string.Format("Select * From CEIDList"));
                        foreach (int nCEID in _dicCEID.Keys)
                        {
                            if (CEIDLists.Tables[0].AsEnumerable().Where(row => Convert.ToInt32(row["CEID"]) == nCEID).Count() <= 0)
                            {
                                nAck = 4; //CEID not found!!
                                WriteLog(string.Format("<<<Warning>>> CEID not found in S2F35. CEID={0}", nCEID));
                                S_S2F36(secsMsg, nAck, secsObject);
                                return;
                            }
                        }
                        // check RPID list
                        DataSet RPIDList = _DB.Reader("Select DISTINCT RPID From RPIDVIDLink");
                        foreach (int nCEID in _dicCEID.Keys)
                        {
                            if (_dicCEID[nCEID].Count == 0)
                            {
                                _DB.SQLExec("Delete * From CEIDRPIDLink Where CEID={0}", nCEID);
                            }
                            else
                            {
                                foreach (int nRPID in _dicCEID[nCEID])
                                {
                                    if (RPIDList.Tables[0].AsEnumerable().Where(row => Convert.ToInt32(row["RPID"]) == nRPID).Count() <= 0)
                                    {
                                        nAck = 5; //RPID not found!!
                                        WriteLog(string.Format("<<<Warning>>> RPID not found in S2F35. RPID={0}", nRPID));
                                        S_S2F36(secsMsg, nAck, secsObject);
                                        return;
                                    }
                                }
                            }
                        }
                        // Insert To DB
                        foreach (int nCEID in _dicCEID.Keys)
                        {
                            foreach (int nRPTID in _dicCEID[nCEID])
                            {
                                WriteLog(string.Format("Define CEID{0} link RPTID{1}", nCEID, nRPTID));
                                _DB.SQLExec("Insert Into CEIDRPIDLink (CEID, RPID) Values ({0}, {1})", nCEID, nRPTID);
                            }

                        }

                        break;
                }
                S_S2F36(secsMsg, nAck, secsObject);

            }
            catch (StreamFuntionException ex)
            {
                WriteLog("<<<Warning>>>S2F35:" + ex.Message);
                S_S2F36(secsMsg, ex.nAck, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F35:" + ex.Message);
                nAck = 7;
                S_S2F36(secsMsg, nAck, secsObject);
            }
        }
        void S_S2F36(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F36, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F36:" + ex.Message);
            }
        }

        void R_S2F37(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            try
            {
                int ListCount = 0;
                object Value = new object();
                List<int> lstCEIDs = new List<int>();
                bool EventAction;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.Bool, ref secsMsg, ref Value);
                EventAction = ((int)Value == 0) ? false : true;
                ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < ListCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                    lstCEIDs.Add((int)Value);
                }

                DataSet ds = _DB.Reader(string.Format("Select * From CEIDList"));
                foreach (int nCEID in lstCEIDs)
                {
                    if (ds.Tables[0].AsEnumerable().Where(row => Convert.ToInt32(row["CEID"]) == nCEID).Count() <= 0)
                    {
                        nAck = 1; //CEID not found.
                        WriteLog(string.Format("<<<Warning>>> CEID not found in S2F37. CEID = {0}", nCEID));
                        S_S2F38(secsMsg, nAck, secsObject);
                        return;
                    }
                }
                if (lstCEIDs.Count <= 0)
                    lstCEIDs.AddRange(ds.Tables[0].AsEnumerable().Select(row => Convert.ToInt32(row["CEID"])).ToArray());

                foreach (int nCEID in lstCEIDs)
                {
                    WriteLog(string.Format("CEID{0} {1} in S2F37.", nCEID, EventAction ? "Enable" : "Disable"));
                    _DB.SQLExec("Update CEIDList Set Enable={0} Where CEID={1}", EventAction.ToString(), nCEID);
                }
                S_S2F38(secsMsg, nAck, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                WriteLog("<<<Warning>>>S2F37:" + ex.Message);
                S_S2F38(secsMsg, ex.nAck, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F37:" + ex.Message);
                nAck = 2;
                S_S2F38(secsMsg, nAck, secsObject);
            }
        }
        void S_S2F38(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S2, QsFunction.F38, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F38:" + ex.Message);
            }
        }

        void R_S2F41(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string ErrorParam = string.Empty;
            int ErrorCode = 0;
            try
            {
                int ListCount = 0;
                string RemoteCMD = string.Empty;
                object Value = new object();
                string StrTemp = string.Empty;
                string SubstrateID = string.Empty;
                //int BoatNo = 0;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                RemoteCMD = (string)Value;
                switch (RemoteCMD.ToUpper())
                {
                    case "GOREMOTE":
                    case "GOLOCAL":

                        GEMControlStats HostCmdStats = (RemoteCMD.ToUpper() == "GOREMOTE") ? GEMControlStats.ONLINEREMOTE : GEMControlStats.ONLINELOCAL;
                        if (_GEMControlstats == HostCmdStats)
                            throw new StreamFuntionException(2, 5, "Cannot perform now.");

                        ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        if (ListCount > 0)
                            throw new StreamFuntionException(3, 5, "At least one parameter is invalid.");
                        if (HostCmdStats == GEMControlStats.ONLINELOCAL)
                        {

                        }
                        else if (HostCmdStats == GEMControlStats.ONLINEREMOTE)
                        {
                            if (SecsGEMUtilty.LoadPortList.Where(x => x.Value.StatusMachine == enumStateMachine.PS_Process).Count() > 0)
                                throw new StreamFuntionException(2, 5, "Cannot perform now.");
                        }


                        S_S2F42(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);
                        SetGEMControlStatus = HostCmdStats;
                        break;
                    case "PROCEEDWITHSUBSTRATE":
                    case "CANCELSUBSTRATE":
                        _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                        StrTemp = (string)Value;
                        if (StrTemp.ToUpper() != "SUBSTRATEID") //Check CPName
                            throw new StreamFuntionException(3, 1, "At least one parameter is invalid.");
                        _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                        SubstrateID = (string)Value;

                        if (RemoteCMD.ToUpper() == "PROCEEDWITHSUBSTRATE")
                            ComparisonStatus = enumComparisonStatus.WaferResume;
                        else if (RemoteCMD.ToUpper() == "CANCELSUBSTRATE")
                            ComparisonStatus = enumComparisonStatus.WaferCancel;

                        break;
                    default:
                        nAck = 1;
                        S_S2F42(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);
                        break;
                }

            }
            catch (StreamFuntionException ex)
            {

                WriteLog("[StreamFuntionException] " + ex);
                if (ex.nAck == 2)
                    S_S2F42(secsMsg, ex.nAck, "Cannot perform now.", ex.nErrorCode, secsObject);
                else
                    S_S2F42(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, secsObject);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
                nAck = 5;
                S_S2F42(secsMsg, nAck, ex.Message, ErrorCode, secsObject);
            }
        }
        void S_S2F42(object secsMsg, int Ack, string ErrorParam, int ErrorCode, SecsMessageObject1Args secsObject)
        {
            _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
            _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
            if (Ack == 0)
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
            else if (Ack > 0 && ErrorCode > 0)
            {
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, ErrorCode);
            }
            else if (Ack > 0) {

                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

            }

            _secsdriver.SendMessage(QsStream.S2, QsFunction.F42, secsMsg, secsObject);
        }

        /// <summary>
        /// S3FX
        /// </summary>
        void R_S3F17(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string ErrorParam = string.Empty;
            int ErrorCode = 0;

            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            try
            {
                if (_GEMControlstats != GEMControlStats.ONLINEREMOTE)
                    throw new StreamFuntionException(2, 13, "Current mode is not Online Remote, Command Fail");

                int ListCount = 0;
                int SubListCount = 0;
                string CarrierAction = string.Empty;
                string CarrierID = string.Empty;
                string LOTID = string.Empty;
                string WaferID = string.Empty;
                string MappingData = string.Empty;
                List<string> HostWaferID = new List<string>();
                List<string> HostLotID = new List<string>();
                //bool IsFindCarrier = false;
                int PortID;
                int Temp = 0;
                int slot = 0;
                string AttributeID = string.Empty;
                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                CarrierAction = (string)Value;
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                CarrierID = (string)Value;
                _secsdriver.DataItemIn(SecsFormateType.U2, ref secsMsg, ref Value);
                PortID = (int)Value;
                if (!SecsGEMUtilty.LoadPortList.ContainsKey(PortID))
                {
                    throw new StreamFuntionException(2, 5, "PortID is Error");
                }

                switch (CarrierAction.ToUpper())
                {
                    #region PROCEEDWITHCARRIER
                    case "PROCEEDWITHCARRIER":

                        if (!SecsGEMUtilty.LoadPortList[PortID].FoupExist)
                            throw new StreamFuntionException(2, 5, "Can not find Carrier  ");

                        /*if (SecsGEMUtilty.PortStatus(PortID) != enumStateMachine.PS_Clamped
                            && SecsGEMUtilty.PortStatus(PortID) != enumStateMachine.PS_Docked
                            )
                            throw new StreamFuntionException(2, 5, "Carrier is not Clamped. ");*/
                        if (SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_Arrived
                            && SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_Clamped
                             && SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_Docked
                            )
                            throw new StreamFuntionException(2, 5, "Carrier Status Error. ");

                        if (SecsGEMUtilty.LoadPortList[PortID].FoupID != CarrierID)
                            throw new StreamFuntionException(2, 5, "Carrier ID Not Match.");

                        if ((SecsGEMUtilty.GetPortStatus(PortID) == enumStateMachine.PS_Arrived || SecsGEMUtilty.GetPortStatus(PortID) == enumStateMachine.PS_Clamped || SecsGEMUtilty.GetPortStatus(PortID) == enumStateMachine.PS_UnClamped)
                            && SecsGEMUtilty.GetCarrierIDstatus(PortID) == CarrierIDStats.IDRead)
                        {
                            if (SecsGEMUtilty.LoadPortList[PortID].IsInfoPadEnable() == false)
                                throw new StreamFuntionException(2, 5, "Foup Type is Error.");

                            S_S3F18(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);
                            SecsGEMUtilty.Dock(PortID);
                            SecsGEMUtilty.SetCarrierIDstatus(PortID, CarrierIDStats.IDVerificationok);

                            /*_VIDcontrol.DVIDList["PortID"].CurrentValue = PortID;
                            _VIDcontrol.DVIDList["CarrierID"].CurrentValue = CarrierID;
                            SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans08);
                            S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans08"]);
                            */
                            DVID_List.Add(new DVID_Obj("PortID", PortID));
                            DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                            SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans08);
                            CEID_List.Add(new CEID_Obj("CarrierSMTrans08"));
                            SetupDVID_EVENT(CEID_List, DVID_List);

                            return;
                        }
                        else if (SecsGEMUtilty.GetPortStatus(PortID) == enumStateMachine.PS_Docked
                            && SecsGEMUtilty.GetCarrierIDstatus(PortID) == CarrierIDStats.IDVerificationok
                            && SecsGEMUtilty.GetSlotMappingStats(PortID) == CarrierSlotMapStats.SlotMappingOK
                            )
                        {
                            ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            for (int i = 0; i < ListCount; i++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                                AttributeID = (string)Value;
                                switch (AttributeID.ToUpper())
                                {
                                    case "CONTENTMAP":
                                        {
                                            SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                            if (SubListCount != SecsGEMUtilty.LoadPortList[PortID].WaferTotal)
                                                throw new StreamFuntionException(2, 5, "Contentmap Count have Error");


                                            for (int j = 0; j < SubListCount; j++)
                                            {
                                                slot = j + 1;
                                                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                                                LOTID = (string)Value;
                                                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                                                WaferID = (string)Value;

                                                if (LOTID == "" && WaferID == "")
                                                    continue;

                                                if (SecsGEMUtilty.GetMappingData(PortID)[slot - 1] != '1')
                                                    throw new StreamFuntionException(2, 5, string.Format("CONTENTMAP Paramter is error, Slot{0} not have wafer ", slot));

                                                if (SecsGEMUtilty.LoadPortList[PortID].Waferlist[slot - 1] != null)
                                                {

                                                    SecsGEMUtilty.LoadPortList[PortID].Waferlist[slot - 1].LotID = LOTID;
                                                    SecsGEMUtilty.LoadPortList[PortID].Waferlist[slot - 1].WaferInforID_B = WaferID;
                                                }

                                            }
                                        }
                                        break;
                                    case "SLOTMAP":
                                        {
                                            SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                            if (SubListCount != SecsGEMUtilty.LoadPortList[PortID].WaferTotal)
                                                throw new StreamFuntionException(2, 5, "SLOTMAP Count have Error");
                                            for (int j = 0; j < SubListCount; j++)
                                            {
                                                _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                                                Temp = (int)Value;
                                                switch (Temp)
                                                {
                                                    case 1:
                                                        MappingData = MappingData + "0";
                                                        break;
                                                    case 3:
                                                        MappingData = MappingData + "1";
                                                        break;
                                                }
                                            }

                                            if (SecsGEMUtilty.GetMappingData(PortID) != MappingData)
                                                throw new StreamFuntionException(2, 5, "SLOTMAP Not match");
                                        }
                                        break;
                                    case "CAPACITY":
                                        _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);

                                        break;
                                    case "SUBSTRATECOUNT":
                                        _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);

                                        break;
                                    case "USAGE":
                                        _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);

                                        break;
                                }
                            }
                            S_S3F18(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);
                            SecsGEMUtilty.SetCarrierSlotMapStats(PortID, CarrierSlotMapStats.SlotMappingVerificationok);
                            SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans15);
                            //S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans15"]);

                            DVID_List.Add(new DVID_Obj("PortID", PortID));
                            DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                            CEID_List.Add(new CEID_Obj("CarrierSMTrans15"));
                            SetupDVID_EVENT(CEID_List, DVID_List);
                            return;
                        }
                        else
                            throw new StreamFuntionException(2, 5, "Carrier states is Error.");
                        break;
                    #endregion
                    #region CARRIERRELEASE/CANCELCARRIER/CANCELCARRIERATPORT
                    case "CARRIERRELEASE":
                    case "CANCELCARRIER":
                    case "CANCELCARRIERATPORT":

                        if (!SecsGEMUtilty.LoadPortList[PortID].FoupExist)
                            throw new StreamFuntionException(2, 5, "Can not find Carrier.");

                        if (SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_Docked &&
                            SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_Clamped &&
                             SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_Complete &&
                             SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_Docking
                            )
                            throw new StreamFuntionException(2, 5, "Carrier status is error");




                        //if (!GMotion.theInst.STG[PortID - 1].isClamped() || !GMotion.theInst.STG[PortID - 1].isDocked())
                        //    throw new StreamFuntionException(2, 5, "Carrier is not Clamped");
                        if (CarrierAction.ToUpper() == "CANCELCARRIER" || CarrierAction.ToUpper() == "CARRIERRELEASE")
                        {
                            if (SecsGEMUtilty.LoadPortList[PortID].FoupID != CarrierID)
                                throw new StreamFuntionException(2, 5, "Carrier ID is Not Match.");
                        }

                        S_S3F18(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);

                        if (CarrierAction.ToUpper() == "CANCELCARRIER" || CarrierAction.ToUpper() == "CANCELCARRIERATPORT")
                        {
                            switch (SecsGEMUtilty.GetPortStatus(PortID))
                            {
                                case enumStateMachine.PS_Clamped:
                                    SecsGEMUtilty.LoadPortList[PortID].SendWarningMsg(enumLoadPortWarning.RFID_Of_Port_Verification_Failed);
                                    //  SpinWait.SpinUntil(() => false, 200);
                                    //    SecsGEMUtilty.LoadPortList[PortID].RestWarningMsg(enumLoadPortWarning.RFID_Of_Port_Verification_Failed);
                                    break;
                                case enumStateMachine.PS_Docked:
                                case enumStateMachine.PS_Docking:
                                    SecsGEMUtilty.LoadPortList[PortID].SendWarningMsg(enumLoadPortWarning.SlotMap_Of_Port_Verification_Failed);
                                    // SpinWait.SpinUntil(() => false, 200);
                                    // SecsGEMUtilty.LoadPortList[PortID].RestWarningMsg(enumLoadPortWarning.SlotMap_Of_Port_Verification_Failed);
                                    break;

                            }


                        }

                        if (CarrierAction.ToUpper() == "CANCELCARRIER")
                        {
                            if (SecsGEMUtilty.GetCarrierState(PortID) == CarrierState.CarrierSMTrans01 || SecsGEMUtilty.GetCarrierState(PortID) == CarrierState.CarrierSMTrans14)
                            {
                                SecsGEMUtilty.SetCarrierSlotMapStats(PortID, CarrierSlotMapStats.SlotMappingVerificationFail);
                                SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans16);

                                /*_VIDcontrol.DVIDList["PortID"].CurrentValue = PortID;
                                _VIDcontrol.DVIDList["CarrierID"].CurrentValue = CarrierID;
                                S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans16"]);
                                */
                                DVID_List.Add(new DVID_Obj("PortID", PortID));
                                DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                                CEID_List.Add(new CEID_Obj("CarrierSMTrans16"));
                                SetupDVID_EVENT(CEID_List, DVID_List);



                            }
                            else if (SecsGEMUtilty.GetCarrierState(PortID) == CarrierState.CarrierSMTrans03 || SecsGEMUtilty.GetCarrierState(PortID) == CarrierState.CarrierSMTrans07 || SecsGEMUtilty.GetCarrierIDstatus(PortID) == CarrierIDStats.IDRead)
                            {
                                SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans09);
                                SecsGEMUtilty.SetCarrierIDstatus(PortID, CarrierIDStats.IDVerificationFail);

                                /*_VIDcontrol.DVIDList["PortID"].CurrentValue = PortID;
                                _VIDcontrol.DVIDList["CarrierID"].CurrentValue = CarrierID;
                                S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans09"]);
                                */
                                DVID_List.Add(new DVID_Obj("PortID", PortID));
                                DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                                CEID_List.Add(new CEID_Obj("CarrierSMTrans09"));
                                SetupDVID_EVENT(CEID_List, DVID_List);

                            }
                        }




                        if (SecsGEMUtilty.GetPortStatus(PortID) == enumStateMachine.PS_Docking) //做完Docking 後再退
                        {
                            SecsGEMUtilty.LoadPortList[PortID].UndockQueueByHost = true;
                        }
                        else if (SecsGEMUtilty.GetPortStatus(PortID) == enumStateMachine.PS_Clamped)
                        {

                            SecsGEMUtilty.UnClamp(PortID);
                        }
                        else
                            SecsGEMUtilty.UnDock(PortID);
                        return;
                    #endregion


                    #region CARRIERRESET
                    case "CARRIERRESET":
                        if (!SecsGEMUtilty.LoadPortList[PortID].FoupExist)
                        {
                            throw new StreamFuntionException(2, 5, "Can not find Carrier.");
                        }

                        if ((SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_UnClamped && SecsGEMUtilty.GetPortStatus(PortID) != enumStateMachine.PS_ReadyToUnload) &&
                                (SecsGEMUtilty.GetCarrierState(PortID) != CarrierState.CarrierSMTrans15 && SecsGEMUtilty.GetCarrierState(PortID) != CarrierState.CarrierSMTrans16 ||
                                 SecsGEMUtilty.GetCarrierState(PortID) != CarrierState.CarrierSMTrans19 && SecsGEMUtilty.GetCarrierState(PortID) != CarrierState.CarrierSMTrans20))
                        {
                            throw new StreamFuntionException(2, 5, "Carrier status is error");
                        }

                        if (SecsGEMUtilty.LoadPortList[PortID].FoupID != CarrierID)
                        {
                            throw new StreamFuntionException(2, 5, "Carrier ID is Not Match.");
                        }


                        S_S3F18(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);


                        /*_VIDcontrol.DVIDList["PortID"].CurrentValue = PortID;
                        _VIDcontrol.DVIDList["CarrierID"].CurrentValue = CarrierID;
                        S_S6F11(_CEIDcontrol.CEIDList["PortTransferSMTrans07"]);
                        */
                        DVID_List.Add(new DVID_Obj("PortID", PortID));
                        DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                        CEID_List.Add(new CEID_Obj("PortTransferSMTrans07"));
                        SetupDVID_EVENT(CEID_List, DVID_List);



                        SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans21);
                        //S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans21"]);

                        DVID_List.Add(new DVID_Obj("PortID", PortID));
                        DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                        CEID_List.Add(new CEID_Obj("CarrierSMTrans21"));
                        SetupDVID_EVENT(CEID_List, DVID_List);



                        SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans01);
                        SecsGEMUtilty.SetCarrierIDstatus(PortID, CarrierIDStats.IDNotRead);

                        /*_VIDcontrol.DVIDList["CarrierIDStatus"].CurrentValue = (int)SecsGEMUtilty.GetCarrierIDstatus(PortID);
                        S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans01"]);
                        */

                        DVID_List.Add(new DVID_Obj("CarrierIDStatus", (int)SecsGEMUtilty.GetCarrierIDstatus(PortID)));
                        DVID_List.Add(new DVID_Obj("PortID", PortID));
                        DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                        CEID_List.Add(new CEID_Obj("CarrierSMTrans01"));
                        SetupDVID_EVENT(CEID_List, DVID_List);



                        SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans03);
                        SecsGEMUtilty.SetCarrierIDstatus(PortID, CarrierIDStats.IDRead);

                        /*_VIDcontrol.DVIDList["CarrierIDStatus"].CurrentValue = (int)SecsGEMUtilty.GetCarrierIDstatus(PortID);
                        S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans03"]);
                        */
                        DVID_List.Add(new DVID_Obj("CarrierIDStatus", (int)SecsGEMUtilty.GetCarrierIDstatus(PortID)));
                        DVID_List.Add(new DVID_Obj("PortID", PortID));
                        DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                        CEID_List.Add(new CEID_Obj("CarrierSMTrans03"));
                        SetupDVID_EVENT(CEID_List, DVID_List);




                        SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans12);
                        SecsGEMUtilty.SetCarrierSlotMapStats(PortID, CarrierSlotMapStats.NotSlotMap);

                        /*_VIDcontrol.DVIDList["CarrierSlotMapStatus"].CurrentValue = (int)SecsGEMUtilty.GetSlotMappingStats(PortID);
                        S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans12"]);
                        */
                        DVID_List.Add(new DVID_Obj("CarrierSlotMapStatus", (int)SecsGEMUtilty.GetSlotMappingStats(PortID)));
                        DVID_List.Add(new DVID_Obj("PortID", PortID));
                        DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                        CEID_List.Add(new CEID_Obj("CarrierSMTrans12"));
                        SetupDVID_EVENT(CEID_List, DVID_List);


                        SecsGEMUtilty.SetCarrierState(PortID, CarrierState.CarrierSMTrans17);
                        SecsGEMUtilty.SetCarrierAccessStats(PortID, CarrierAccessStats.NotAccess);

                        /*_VIDcontrol.DVIDList["CarrierAccessingStatus"].CurrentValue = (int)SecsGEMUtilty.GetCarrierAccessStats(PortID);
                        S_S6F11(_CEIDcontrol.CEIDList["CarrierSMTrans17"]);
                        */
                        DVID_List.Add(new DVID_Obj("CarrierAccessingStatus", (int)SecsGEMUtilty.GetCarrierAccessStats(PortID)));
                        DVID_List.Add(new DVID_Obj("PortID", PortID));
                        DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                        CEID_List.Add(new CEID_Obj("CarrierSMTrans17"));
                        SetupDVID_EVENT(CEID_List, DVID_List);


                        SecsGEMUtilty.SetPortStatus(PortID, enumStateMachine.PS_Arrived);


                        //S_S6F11(_CEIDcontrol.CEIDList["CarrierRecreate"]);

                        DVID_List.Add(new DVID_Obj("PortID", PortID));
                        DVID_List.Add(new DVID_Obj("CarrierID", CarrierID));
                        CEID_List.Add(new CEID_Obj("CarrierRecreate"));
                        SetupDVID_EVENT(CEID_List, DVID_List);


                        break;
                        #endregion


                }

                S_S3F18(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);

            }
            catch (StreamFuntionException ex)
            {
                S_S3F18(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, secsObject);
            }
            catch (Exception ex)
            {
                nAck = 6;
                S_S3F18(secsMsg, nAck, ex.Message, 6, secsObject);
            }
        }
        void S_S3F18(object secsMsg, int Ack, string ErrorParam, int ErrorCode, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, Ack);
                if (Ack == 0)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, ErrorCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);
                }

                _secsdriver.SendMessage(QsStream.S3, QsFunction.F18, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S3F18:" + ex.Message);
            }

        }

        void R_S3F27(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string ErrorParam = string.Empty;
            int ErrorCode = 0;
            List<int> _PortList = new List<int>();
            try
            {
                if (_GEMControlstats != GEMControlStats.ONLINEREMOTE)
                    throw new StreamFuntionException(2, 13, "Current mode is not Online Remote, Command Fail");

                object Value = new object();
                SLoadport.E84Mode ChangeMode;
                int ListCount = 0;
                int port = 0;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                ChangeMode = (SLoadport.E84Mode)((int)Value);
                ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                if (ListCount < 1)
                    throw new StreamFuntionException(2, 5, "List Count is Error");
                for (int i = 0; i < ListCount; i++)
                {

                    _secsdriver.DataItemIn(SecsFormateType.U2, ref secsMsg, ref Value);
                    port = (int)Value;


                    if (port < 0 || port > 4)
                        throw new StreamFuntionException(2, 5, string.Format("Port {0} is not find ", port));
                    if ((SecsGEMUtilty.LoadPortList[port].E84Object.GetAutoMode && ChangeMode == SLoadport.E84Mode.Auto) ||
                        (!SecsGEMUtilty.LoadPortList[port].E84Object.GetAutoMode && ChangeMode == SLoadport.E84Mode.Manual)
                        )
                        throw new StreamFuntionException(2, 5, string.Format("Port {0} E84 Mode is same Now", port));



                    _PortList.Add(port);

                }
                S_S3F28(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);
                for (int i = 0; i < _PortList.Count; i++)
                {

                    SecsGEMUtilty.LoadPortList[_PortList[i]].E84Object.SetAutoMode((ChangeMode == SLoadport.E84Mode.Auto) ? true : false);

                }

            }
            catch (StreamFuntionException ex)
            {
                S_S3F28(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S3F28(secsMsg, 3, "Other Error", 5, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S3F28(object secsMsg, int Ack, string ErrorParam, int ErrorCode, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, Ack);
                if (Ack == 0)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, ErrorCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);
                }

                _secsdriver.SendMessage(QsStream.S3, QsFunction.F28, secsMsg, secsObject);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        /// <summary>
        /// S5FX
        /// </summary>
        void S_S5F1(int AlarmID, bool AlarmSet, string Msg = "")
        {
            try
            {
                if (this.GEMControlStatus < GEMControlStats.ONLINELOCAL)
                    return;

                string AlarmMsg = "";
                int StgNo = 0;
                DataSet Alarm = _AlarmListDB.Reader(string.Format("Select * From AlarmList where AlarmID='{0}'", AlarmID.ToString()));
                if (Alarm.Tables[0] == null || Alarm.Tables[0].Rows.Count < 1)
                    throw new Exception(string.Format("Not find AlarmID={0},Please Check it.", AlarmID));
                if (Alarm.Tables[0].Rows[0]["Enable"].ToString() == "True")
                {
                    object secsMsg = new object();
                    _secsdriver.DataIteminitoutRequest(QsStream.S5, QsFunction.F1, _parameter.GetSECSParameterConfig.S5Wbit, ref secsMsg);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);

                    int AlarmLevel = 7;

                    if (AlarmSet)
                        AlarmLevel = AlarmLevel + 128;
                    if (Msg != "")
                    {
                        AlarmMsg = Msg;
                    }
                    else
                    {
                        AlarmMsg = Alarm.Tables[0].Rows[0]["AlarmMsg"].ToString();

                        if (Alarm.Tables[0].Rows[0]["Type"].ToString() == "Warning" && Alarm.Tables[0].Rows[0]["UnitType"].ToString().Contains("STG")) // 加RFID...
                        {
                            if (int.TryParse(Alarm.Tables[0].Rows[0]["UnitType"].ToString().Split('G')[1], out StgNo) == true)
                            {
                                if (SecsGEMUtilty.LoadPortList[StgNo].FoupID != "")
                                {
                                    AlarmMsg = AlarmMsg + "-CarrierID:" + SecsGEMUtilty.LoadPortList[StgNo].FoupID;
                                }
                            }
                        }

                    }
                    _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, AlarmLevel);
                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, AlarmID);
                    //_secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Alarm.Tables[0].Rows[0]["AlarmMsg"].ToString());
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, AlarmMsg);
                    _secsdriver.SendMessage(QsStream.S5, QsFunction.F1, secsMsg);

                    if (AlarmSet)
                    {
                        S_S6F11(_CEIDcontrol.CEIDList["AlarmSet"]); //Alarm 暫時未列入SetupDVID_EVENT
                    }
                    else
                    {
                        S_S6F11(_CEIDcontrol.CEIDList["AlarmCleared"]);  //Alarm 暫時未列入SetupDVID_EVENT
                    }
                }

                if (OnAlarmStatusChange != null)
                    OnAlarmStatusChange(this, new EventArgs());

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        void S_S5F1_Reset()
        {
            try
            {
                DataSet Alarm = _DB.Reader(string.Format("Select * From ALIDList where Ocur=True"));

                for (int i = 0; i < Alarm.Tables[0].Rows.Count; i++)
                {


                    object secsMsg = new object();
                    _secsdriver.DataIteminitoutRequest(QsStream.S5, QsFunction.F1, _parameter.GetSECSParameterConfig.S5Wbit, ref secsMsg);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                    int AlarmLevel = Convert.ToInt32(Alarm.Tables[0].Rows[i]["Alarmlevel"].ToString());
                    int AlarmID = Convert.ToInt32(Alarm.Tables[0].Rows[i]["AlarmID"].ToString());
                    AlarmLevel = AlarmLevel + 128;

                    _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, AlarmLevel);
                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, AlarmID);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Alarm.Tables[0].Rows[i]["AlarmMsg"].ToString());
                    _secsdriver.SendMessage(QsStream.S5, QsFunction.F1, secsMsg);
                    S_S6F11(_CEIDcontrol.CEIDList["AlarmCleared"]); //Alarm 暫時未列入SetupDVID_EVENT

                    _DB.SQLExec("Update ALIDList Set Ocur =False where AlarmID={0}", AlarmID);
                    //  _DB.SQLExec("Update ALIDList Set OcurTime='{1}' where AlarmID={0}", AlarmID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                }
                // Update DB 




            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        void R_S5F3(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string ErrorParam = string.Empty;
            // int ErrorCode = 0;
            try
            {
                object Value = new object();
                bool IsEnable = false;
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.B, ref secsMsg, ref Value);
                if ((int)Value != 0 && (int)Value != 128)
                    throw new StreamFuntionException(1, 13, "Value is Error");
                IsEnable = ((int)Value == 0) ? false : true;
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);

                // _DB.SQLExec("Update ALIDList Set Enable={0},Ocur=False", IsEnable.ToString()); // Disable/enable Alarm and Reset All Alarm

                _AlarmListDB.SQLExec("Update AlarmList Set Enable ={0}", IsEnable.ToString());

                S_S5F4(secsMsg, nAck, secsObject);

            }
            catch (StreamFuntionException ex)
            {
                S_S5F4(secsMsg, ex.nAck, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S5F4(secsMsg, 2, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S5F4(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);

                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S5, QsFunction.F4, secsMsg, secsObject);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        void R_S5F5(object secsMsg, SecsMessageObject1Args secsObject)
        {
            List<int> ALID = new List<int>();
            int[] ALIDArray;
            try
            {

                //int ListCount = 0;
                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                try
                {
                    ALIDArray = (int[])Value;
                }
                catch (Exception ex)
                {
                    if ((int)Value != 0)
                        ALID.Add((int)Value);
                    S_S5F6(secsMsg, secsObject, ALID);
                    WriteLog("[Exception] " + ex);
                    return;
                }


                for (int i = 0; i < ALIDArray.Count(); i++)
                {
                    // _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                    ALID.Add(ALIDArray[i]);
                }
                S_S5F6(secsMsg, secsObject, ALID);
            }
            catch (Exception ex)
            {
                S_S5F6(secsMsg, secsObject, ALID);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S5F6(object secsMsg, SecsMessageObject1Args secsObject, List<int> AlarmID)
        {
            // int Ack = 2;

            if (AlarmID.Count > 0)
            {


                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, AlarmID.Count);

                for (int i = 0; i < AlarmID.Count; i++)
                {
                    DataSet Alarm = _AlarmListDB.Reader(string.Format("Select * From AlarmList where AlarmID={0}", AlarmID[i].ToString()));
                    //if (Alarm.Tables[0] == null || Alarm.Tables[0].Rows.Count < 1)
                    //    Ack = 0; //Not Use
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);

                    // int AlarmLevel = Convert.ToInt32(Alarm.Tables[0].Rows[0]["Alarmlevel"].ToString());
                    int AlarmLevel = 7;
                    _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, AlarmLevel);

                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, AlarmID);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Alarm.Tables[0].Rows[0]["AlarmMsg"].ToString());
                }
            }
            else
            {
                DataSet Alarm = _AlarmListDB.Reader(string.Format("Select * From AlarmList"));
                int AlarmLevel = 0;

                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, Alarm.Tables[0].Rows.Count);

                for (int i = 0; i < Alarm.Tables[0].Rows.Count; i++)
                {
                    // AlarmLevel = Convert.ToInt32(Alarm.Tables[0].Rows[i]["Alarmlevel"].ToString());
                    AlarmLevel = 7;
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                    _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, AlarmLevel);
                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, Convert.ToInt32(Alarm.Tables[0].Rows[i]["AlarmID"].ToString()));
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Alarm.Tables[0].Rows[i]["AlarmMsg"].ToString());

                }
            }
            _secsdriver.SendMessage(QsStream.S5, QsFunction.F6, secsMsg, secsObject);


        }


        void R_S5F7(object secsMsg, SecsMessageObject1Args secsObject)
        {
            S_S5F8(secsMsg, secsObject);
        }
        void S_S5F8(object secsMsg, SecsMessageObject1Args secsObject)
        {
            DataSet Alarm = _DB.Reader(string.Format("Select * From ALIDList"));
            int AlarmLevel = 0;

            _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, Alarm.Tables[0].Rows.Count);

            for (int i = 0; i < Alarm.Tables[0].Rows.Count; i++)
            {
                AlarmLevel = Convert.ToInt32(Alarm.Tables[0].Rows[i]["Alarmlevel"].ToString());
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, AlarmLevel);
                _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, Convert.ToInt32(Alarm.Tables[0].Rows[i]["AlarmID"].ToString()));
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Alarm.Tables[0].Rows[i]["AlarmMsg"].ToString());

            }
            _secsdriver.SendMessage(QsStream.S5, QsFunction.F8, secsMsg, secsObject);


        }



        /// <summary>
        /// S6FX
        /// </summary>
        private void _pollingTraceData_DoPolling()
        {
            try
            {
                int[] anTraceIDs = _dicTraceContent.Keys.ToArray();
                foreach (int traceID in anTraceIDs)
                    S_S6F1(traceID);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S6F1(int nTraceID)
        {
            try
            {
                if (!_dicTraceContent.ContainsKey(nTraceID))
                    return;
                object secsMsg = new object();
                _dicCurrentTOTSMP[nTraceID] += 1;
                if (_dicCurrentTOTSMP[nTraceID] > _dicTraceTOTSMP[nTraceID])
                {
                    _dicTraceContent.Remove(nTraceID);
                    _dicCurrentTOTSMP.Remove(nTraceID);
                    _dicTraceTOTSMP.Remove(nTraceID);

                    return;
                }

                _secsdriver.DataIteminitoutRequest(QsStream.S6, QsFunction.F1, true, ref secsMsg);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 4);
                _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, nTraceID);
                _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, _dicCurrentTOTSMP[nTraceID]);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, DateTime.Now.ToString("yyyyMMddHHmmss"));
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _dicTraceContent[nTraceID].Count);
                for (int i = 0; i < _dicTraceContent[nTraceID].Count; i++)
                {
                    AssignVIDValue(ref secsMsg, _dicTraceContent[nTraceID][i]);
                }

                _secsdriver.SendMessage(QsStream.S6, QsFunction.F1, secsMsg);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        public void S_S6F11(int CEID)
        {
            // lock (objectS6F11) 
            //20241017，此Function (S_S6F11)，只能透過 SetupDVID_EVENT 呼叫
            try
            {
                {
                    if (_GEMControlstats != GEMControlStats.ONLINELOCAL && _GEMControlstats != GEMControlStats.ONLINEREMOTE)
                        return;
                    if (!GetSECSDriver.GetSecsStarted())
                        return;
                    DataSet dsCEID = _DB.Reader("Select * From CEIDList Where CEID = {0}", CEID);
                    if (!Convert.ToBoolean(dsCEID.Tables[0].Rows[0]["Enable"].ToString()))
                        return; //event be disable

                    //string strName = dsCEID.Tables[0].Rows[0]["Name"].ToString();

                    DataSet dsRPTID = _DB.Reader("Select * From CEIDRPIDLink Where CEID={0}", CEID);
                    List<int> lstRPTIDs = new List<int>();
                    lstRPTIDs.AddRange(dsRPTID.Tables[0].AsEnumerable().Select(row => Convert.ToInt32(row["RPID"])).ToArray());

                    object secsMsg = new object();
                    _secsdriver.DataIteminitoutRequest(QsStream.S6, QsFunction.F11, _parameter.GetSECSParameterConfig.S6Wbit, ref secsMsg);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, 1);
                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, CEID);

                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, lstRPTIDs.Count);
                    foreach (int nRPTID in lstRPTIDs)
                    {
                        DataSet dsVID = _DB.Reader("Select * From RPIDVIDLink Where RPID = {0}", nRPTID);
                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                        _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, nRPTID);
                        int ValueCount = dsVID.Tables[0].Rows.Count;
                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, ValueCount);
                        for (int nVIDRow = 0; nVIDRow < dsVID.Tables[0].Rows.Count; nVIDRow++)
                        {
                            int nVID = Convert.ToInt32(dsVID.Tables[0].Rows[nVIDRow]["VID"]);
                            AssignVIDValue(ref secsMsg, nVID);

                        }
                        // ListCount += 1;
                        dsVID.Clear();
                    }
                    _secsdriver.SendMessage(QsStream.S6, QsFunction.F11, secsMsg);


                    //

                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        public void SendSTSSouceInfo(int port, string carrier, List<string> waferList)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();

            TempPortforSTS = port;
            TempCarrierforSTS = carrier;
            TempWaferListforSTS = waferList;

            //S_S6F11(_CEIDcontrol.CEIDList["STSATSource"]);

            CEID_List.Add(new CEID_Obj("STSATSource"));
            SetupDVID_EVENT(CEID_List, DVID_List);

        }

        private void R_S6F15(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                object Value = new object();
                int nCEID = 0;

                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                nCEID = (int)Value;
                DataSet dsVID = _DB.Reader("Select * From CEIDList Where CEID = {0}", nCEID);
                if (dsVID.Tables[0].Columns.Count < 1)
                    throw new StreamFuntionException(2, 5, "List Count is Error");

                S_S6F16(secsMsg, secsObject, nCEID);
            }
            catch (StreamFuntionException ex)
            {
                S_S6F16(secsMsg, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S6F16(secsMsg, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S6F16(object secsMsg, SecsMessageObject1Args secsObject, int CEID = 0)
        {
            _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
            if (CEID == 0)
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
            else
            {
                DataSet dsRPTID = _DB.Reader("Select * From CEIDRPIDLink Where CEID={0}", CEID);
                List<int> lstRPTIDs = new List<int>();
                lstRPTIDs.AddRange(dsRPTID.Tables[0].AsEnumerable().Select(row => Convert.ToInt32(row["RPID"])).ToArray());

                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, 1);
                _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, CEID);

                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, lstRPTIDs.Count);
                foreach (int nRPTID in lstRPTIDs)
                {
                    DataSet dsVID = _DB.Reader("Select * From RPIDVIDLink Where RPID = {0}", nRPTID);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, nRPTID);
                    int ValueCount = dsVID.Tables[0].Rows.Count;
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, ValueCount);
                    for (int nVIDRow = 0; nVIDRow < dsVID.Tables[0].Rows.Count; nVIDRow++)
                    {
                        int nVID = Convert.ToInt32(dsVID.Tables[0].Rows[nVIDRow]["VID"]);
                        AssignVIDValue(ref secsMsg, nVID);

                    }
                    // ListCount += 1;
                    dsVID.Clear();

                }
            }
            _secsdriver.SendMessage(QsStream.S6, QsFunction.F16, secsMsg, secsObject);
        }

        private void R_S6F19(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                object Value = new object();
                int nRPTID = 0;
                List<int> PRIDList = new List<int>();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                nRPTID = (int)Value;
                DataSet dsVID = _DB.Reader("Select * From RPIDVIDLink Where RPID = {0}", nRPTID);
                if (dsVID.Tables[0].Columns.Count < 1)
                    throw new StreamFuntionException(2, 5, "List Count is Error");
                PRIDList.Add(nRPTID);
                S_S6F20(secsMsg, secsObject, PRIDList);
            }
            catch (StreamFuntionException ex)
            {
                S_S6F20(secsMsg, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S6F20(secsMsg, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S6F20(object secsMsg, SecsMessageObject1Args secsObject, List<int> RPIDList = null)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                if (RPIDList == null)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, RPIDList.Count);
                    foreach (int nRPTID in RPIDList)
                    {
                        DataSet dsVID = _DB.Reader("Select * From RPIDVIDLink Where RPID = {0}", nRPTID);
                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                        _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, nRPTID);
                        int ValueCount = dsVID.Tables[0].Rows.Count;
                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, ValueCount);
                        for (int nVIDRow = 0; nVIDRow < dsVID.Tables[0].Rows.Count; nVIDRow++)
                        {
                            int nVID = Convert.ToInt32(dsVID.Tables[0].Rows[nVIDRow]["VID"]);
                            AssignVIDValue(ref secsMsg, nVID);

                        }
                        // ListCount += 1;
                        dsVID.Clear();

                    }

                }
                _secsdriver.SendMessage(QsStream.S6, QsFunction.F20, secsMsg, secsObject);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        /// <summary>
        /// S7FX
        /// </summary>
        private void R_S7F19(object secsMsg, SecsMessageObject1Args secsObject)
        {
            S_S7F20(secsMsg, secsObject);
        }
        private void S_S7F20(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {

                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);


                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _GroupRecipeManager.GetRecipeGroupList.Count);

                foreach (var key in _GroupRecipeManager.GetRecipeGroupList.Keys)
                {
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, key);
                }


                _secsdriver.SendMessage(QsStream.S7, QsFunction.F20, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void R_S7F25(object secsMsg, SecsMessageObject1Args secsObject)
        {
            string RecipeName = string.Empty;
            try
            {
                object Value = new object();

                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                RecipeName = (string)Value;
                // _EQ.RecipeBoby(RecipeName);
                R_S7F26(RecipeName, secsMsg, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                WriteLog("[StreamFuntionException] " + ex);
                R_S7F26(RecipeName, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
                R_S7F26(RecipeName, secsMsg, secsObject);
            }
        }
        private void R_S7F26(string RecipeName, object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                //  Recipe移除需要確認
                //SSToolRecipeInfo[] Recipebody = SecsGEMUtilty.GetRecipeInfo(12, RecipeName);

                //_secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                //_secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 4);

                //_secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, RecipeName);
                //_secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "NewGem300");
                //_secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Ver-1.0.0.0");

                //if (Recipebody[0] != null)
                //{
                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 14);

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "RecipeName");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].RecipeName);

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ModelName");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].ModelName);

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Aligne");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nAligne.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "UserID");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].UserID);

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CassetteType");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].CassetteType);

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "WaferSize");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nWaferSize.ToString());


                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Slot");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nSlot.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Magnetic Put[V]");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nMagneticPut.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Magnetic Put Max[K0e]");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nMagneticPutMax.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Magnetic Put Min[K0e]");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nMagneticPutMin.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Wait Time Put[ms]");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nWaitTimePut.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Magnetic Take[V]");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nMagneticTake.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Magnetic Take Max[K0e]");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nMagneticTakeMax.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Magnetic Take Min[K0e]");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nMagneticTakeMin.ToString());

                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Wait Time Take[ms]");
                //    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Recipebody[0].nWaitTimeTake.ToString());

                //}
                //else
                //{
                //    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                //}

                _secsdriver.SendMessage(QsStream.S7, QsFunction.F26, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }

        }
        /// <summary>
        /// S10FX
        /// </summary>
        /// 
        private void R_S10F1(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string Msg = string.Empty;
            try
            {
                object Value = new object();

                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.B, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                Msg = (string)Value;

                S_S10F4(secsMsg, nAck, secsObject);

                if (OnHostMessageSend != null)
                    OnHostMessageSend(this, Msg);

            }
            catch (StreamFuntionException ex)
            {
                S_S10F2(secsMsg, nAck, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S10F2(secsMsg, nAck, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        private void S_S10F2(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S10, QsFunction.F2, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S10F2:" + ex.Message);
            }
        }

        private void R_S10F3(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string Msg = string.Empty;
            try
            {
                object Value = new object();

                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.B, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                Msg = (string)Value;

                S_S10F4(secsMsg, nAck, secsObject);

                if (OnHostMessageSend != null)
                    OnHostMessageSend(this, Msg);

            }
            catch (StreamFuntionException ex)
            {
                S_S10F4(secsMsg, nAck, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S10F4(secsMsg, nAck, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        private void S_S10F4(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S10, QsFunction.F4, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S10F4:" + ex.Message);
            }
        }

        private void R_S10F5(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string Msg = string.Empty;
            int ListCount = 0;
            try
            {
                object Value = new object();

                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.B, ref secsMsg, ref Value);
                ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < ListCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    if (Msg == string.Empty)
                        Msg = (string)Value;
                    else
                        Msg = Msg + "," + (string)Value;
                }

                S_S10F6(secsMsg, nAck, secsObject);

                if (OnHostMessageSend != null)
                    OnHostMessageSend(this, Msg);
                // _EQP.HostMsg(Msg);
            }
            catch (StreamFuntionException ex)
            {
                S_S10F6(secsMsg, nAck, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S10F6(secsMsg, nAck, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        private void S_S10F6(object secsMsg, int Ack, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, Ack);
                _secsdriver.SendMessage(QsStream.S10, QsFunction.F6, secsMsg, secsObject);



            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S2F36:" + ex.Message);
            }
        }


        /// <summary>
        /// S14FX
        /// </summary>
        /// 
        void R_S14F1(object secsMsg, SecsMessageObject1Args secsObject)
        {
            string objspec = string.Empty;
            string objtype = string.Empty;
            int ErrorCode = 0;
            string ErrorText = string.Empty;
            int ObjIDCount = 0;
            int AttrCount = 0;
            int AttrParamCount = 0;
            int AttrIDCount = 0;
            string AttrID = string.Empty;
            int AttrData = 0;
            int AttrReln = 0;
            List<Attribute> _listAttr = new List<Attribute>();
            List<string> _listAttrID = new List<string>();
            List<string> _listObjID = new List<string>();
            try
            {
                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                objspec = (string)Value;
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                objtype = (string)Value;
                ObjIDCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < ObjIDCount; i++) {

                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    _listObjID.Add((string)Value);
;                }

                AttrCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < AttrCount; i++)
                {
                    AttrParamCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                    if (AttrParamCount != 3)
                        throw new StreamFuntionException(2, 7, "List Count is Error");
                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    AttrID = (string)Value;
                    _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                    AttrData = (int)Value;
                    _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                    AttrReln = (int)Value;

                    _listAttr.Add(new Attribute(AttrID, AttrData, AttrReln));
                }
                AttrIDCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < AttrIDCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    _listAttrID.Add((string)Value);
                }

                S_S14F2(secsMsg, secsObject, objtype, _listAttr, _listAttrID, ErrorCode, ErrorText);

            }
            catch (StreamFuntionException ex)
            {
                ErrorCode = ex.nErrorCode;
                ErrorText = ex.Message;

                S_S14F2(secsMsg, secsObject, objtype, _listAttr, _listAttrID, ErrorCode, ErrorText);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
                ErrorCode = 16;
                ErrorText = "unavailable";
            }
            /*
             0 - ok
1 - unknown object
2 - unknown class
3 - unknown object instance
4 - unknown attribute type
5 - read-only attribute
6 - unknown class
7 - invalid attribute value
8 - syntax error
9 - verification error
10 - validation error
11 - object ID in use
12 - improper parameters
13 - missing parameters
14 - unsupported option requested
15 - busy
16 - unavailable
17 - command not valid in current state
18 - no material altered
19 - partially processed
20 - all material processed
21 - recipe specification error
22 - failure when processing
23 - failure when not processing
24 - lack of material
25 - job aborted
26 - job stopped
27 - job cancelled
28 - cannot change selected recipe
29 - unknown event
30 - duplicate report ID
31 - unknown data report
32 - data report not linked
33 - unknown trace report
34 - duplicate trace ID
35 - too many reports
36 - invalid sample period
37 - group size too large
38 - recovery action invalid
39 - busy with previous recovery
40 - no active recovery
41 - recovery failed
42 - recovery aborted
43 - invalid table element
44 - unknown table element
45 - cannot delete predefined
46 - invalid token
47 - invalid parameter
             */

        }
        void S_S14F2(object secsMsg, SecsMessageObject1Args secsObject, string ObjType, List<Attribute> ListAttr, List<string> ListAttrID, int ErrCode, string ErrText)
        {
            try
            {
                int nAck = 0;
                int wafercount = 0;
                bool FindCarrier = false;
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                if (ErrCode > 0)
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                    nAck = 1;
                }
                else
                {
                    switch (ObjType)
                    {

                        #region Carrier
                        case "Carrier":

                            int CarrierCount = 0;

                            //for (int i = 0; i < 4; i++)
                            //{
                            //    if (GMotion.theInst.STG[i].isFoupExist())
                            //        CarrierCount++;
                            //}
                            foreach (I_Loadport Port in SecsGEMUtilty.LoadPortList.Values)
                            {
                                if (Port.FoupExist)
                                    CarrierCount++;
                            }

                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, CarrierCount);



                            // for (int i = 0; i < 4; i++)
                            foreach (I_Loadport Port in SecsGEMUtilty.LoadPortList.Values)
                            {
                                if (!Port.FoupExist) continue;



                                FindCarrier = true;
                                if (ListAttrID.Count > 0)
                                {
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Port.FoupID);
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, ListAttrID.Count);
                                    for (int j = 0; j < ListAttrID.Count; j++)
                                    {
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ListAttrID[j]);
                                        switch (ListAttrID[j])
                                        {
                                            case "ObjType":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Carrier");
                                                break;
                                            case "ObjID":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Port.FoupID);
                                                break;
                                            case "Capacity":
                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 25);
                                                break;
                                            /*
                                        case "CarrierAccessingStatus":
                                            _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, _EQ.GetLoadPort(i + 1).Carrier.AccessStats);
                                            break;
                                        case "CarrierIDStatus":
                                            _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, _EQ.GetLoadPort(i + 1).Carrier.IDStatus);
                                            break;
                                            */
                                            case "ContentMap":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 25);
                                                bool FindWafer = false;
                                                for (int k = 0; k < 25; k++)
                                                {
                                                    FindWafer = false;
                                                    if (Port.MappingData[k] == '1')
                                                    {
                                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);

                                                        foreach (SWafer wafer in Port.Waferlist)
                                                        {
                                                            //foreach (SWafer wafers in lot.Wafers.Values)
                                                            //{
                                                            if (wafer == null)
                                                            {
                                                                continue;
                                                            }

                                                            if (wafer.Slot == k + 1)
                                                                {
                                                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.LotID);
                                                                //_secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.WaferID_F + '/' + wafer.WaferID_B);
                                                                   _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.WaferID_F);
                                                                    FindWafer = true;
                                                                    break;
                                                                }
                                                            //}
                                                            if (FindWafer)
                                                                break;
                                                        }
                                                        if (!FindWafer)
                                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                    }
                                                    else
                                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

                                                }


                                                break;
                                            case "LocationID":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, string.Format("Port{0}", Port.BodyNo));
                                                break;
                                            case "SlotMap":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 25);
                                                for (int k = 0; k < 25; k++)
                                                {
                                                    if (Port.MappingData[k] == '1')
                                                        _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 3);
                                                    else
                                                        _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 1);

                                                }
                                                break;
                                                /*
                                            case "SlotMapStatus":
                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, _EQ.GetLoadPort(i + 1).Carrier.SlotMapStats);
                                                break;
                                            case "Usage":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _EQ.GetLoadPort(i + 1).Carrier.Usage);
                                                break;
                                                */
                                        }
                                    }
                                }
                                else
                                {
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Port.FoupID);
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 6);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjType");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Carrier");

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjID");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, Port.FoupID);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Capacity");
                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 25);

                                    /*
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CarrierAccessingStatus");
                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, _EQ.GetLoadPort(i + 1).Carrier.AccessStats);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CarrierIDStatus");
                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, _EQ.GetLoadPort(i + 1).Carrier.IDStatus);
                                    */

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ContentMap");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 25);
                                    bool FindWafer = false;
                                    for (int k = 0; k < 25; k++)
                                    {
                                        FindWafer = false;
                                        if (Port.MappingData[k] == '1')
                                        {
                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);

                                            foreach (SWafer wafer in Port.Waferlist)
                                                {
                                                //foreach (SWafer wafers in lot.Wafers.Values)
                                                //{
                                                if (wafer == null) {
                                                    continue;
                                                }

                                                if (wafer.Slot == k + 1)
                                                    {
                                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.LotID);
                                                    //_secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.WaferID_F + '/' + wafer.WaferID_B);
                                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.WaferID_F);
                                                        FindWafer = true;
                                                        break;
                                                    }
                                                //}
                                                if (FindWafer)
                                                    break;
                                            }
                                            if (!FindWafer)
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                        }
                                        else
                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

                                    }



                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "LocationID");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, string.Format("Port{0}", Port.BodyNo));

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "SlotMap");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 25);
                                    for (int k = 0; k < 25; k++)
                                    {
                                        if (Port.MappingData[k] == '1')
                                            _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 3);
                                        else
                                            _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 1);

                                    }
                                    /*
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "SlotMapStatus");
                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, _EQ.GetLoadPort(i + 1).Carrier.SlotMapStats);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Usage");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _EQ.GetLoadPort(i + 1).Carrier.Usage);
                                    */
                                }
                            }
                            if (!FindCarrier)
                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

                            break;
                        #endregion

                        #region ProcessJob
                        case "ProcessJob":
                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.PJlist.Count);

                            // for(int i=0;i < _jobcontrol.PJlist.Count;i++)
                            foreach (string PJName in _jobcontrol.PJlist.Keys)
                            {
                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJName);

                                if (ListAttrID.Count > 0)
                                {
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, ListAttrID.Count);
                                    for (int j = 0; j < ListAttrID.Count; j++)
                                    {
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ListAttrID[j]);
                                        switch (ListAttrID[j])
                                        {
                                            case "ObjType":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ProcessJob");
                                                break;
                                            case "ObjID":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJName);
                                                break;
                                            case "PauseEvent":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                break;
                                            case "PrJobState":
                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, (int)_jobcontrol.PJlist[PJName].Status);
                                                break;
                                            case "PrMtlNameList":
                                                /*
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.PJlist[PJName].SourceTransInfo.Count);
                                                foreach (string SourceCarrier in _jobcontrol.PJlist[PJName].SourceTransInfo.Keys)
                                                {
                                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                                    // Source Carrier
                                                    int SourcePortID = _jobcontrol.PJlist[PJName].SourceTransInfo[SourceCarrier].PortID;
                                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, SourceCarrier);
                                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 6);
                                                    if (_EQ.GetLoadPort(SourcePortID).Carrier != null)
                                                    {
                                                        for (int k = 0; k < _EQ.GetLoadPort(SourcePortID).Carrier.GetMappingdata.Length; k++)
                                                        {
                                                            if (_EQ.GetLoadPort(SourcePortID).Carrier.MaterialList.ContainsKey(k + 1))
                                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 3);
                                                            else
                                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 1);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        for (int k = 0; k < 6; k++)
                                                            _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);
                                                    }

                                                    // Target Port
                                                    int TargetPortID = _jobcontrol.PJlist[PJName].SourceTransInfo[SourceCarrier].TargetPortID;
                                                    if (_EQ.GetLoadPort(TargetPortID).Carrier != null)
                                                    {
                                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _EQ.GetLoadPort(TargetPortID).Carrier.ID);
                                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 6);
                                                        for (int k = 0; k < _EQ.GetLoadPort(TargetPortID).Carrier.GetMappingdata.Length; k++)
                                                        {
                                                            if (_EQ.GetLoadPort(TargetPortID).Carrier.MaterialList.ContainsKey(k + 1))
                                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 3);
                                                            else
                                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 1);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "");
                                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 6);
                                                        for (int k = 0; k < 6; k++)
                                                        {
                                                            _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);
                                                        }
                                                    }

                                                }
                                                */
                                                break;
                                            case "PrMtlType":
                                                _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, 0x0D);
                                                break;
                                            case "PrProcessStart":
                                                int AutoStart = (_jobcontrol.PJlist[PJName].AutoStart) ? 255 : 0;
                                                _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, AutoStart);
                                                break;
                                            case "PrRecipeMethod":
                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);
                                                break;
                                            case "RecID":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _jobcontrol.PJlist[PJName].RecipeName);
                                                break;
                                            case "RecVariableList":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 9);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjType");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ProcessJob");

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjID");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJName);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PauseEvent");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);


                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrJobState");
                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, (int)_jobcontrol.PJlist[PJName].Status);

                                    /*
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrMtlNameList");
                                    
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.PJlist[PJName].SourceTransInfo.Count);
                                    foreach (string SourceCarrier in _jobcontrol.PJlist[PJName].SourceTransInfo.Keys)
                                    {
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                        // Source Carrier
                                        int SourcePortID = _jobcontrol.PJlist[PJName].SourceTransInfo[SourceCarrier].PortID;
                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, SourceCarrier);
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 6);
                                        
                                        if (_EQ.GetLoadPort(SourcePortID).Carrier != null)
                                        {
                                            for (int k = 0; k < _EQ.GetLoadPort(SourcePortID).Carrier.GetMappingdata.Length; k++)
                                            {
                                                if (_EQ.GetLoadPort(SourcePortID).Carrier.MaterialList.ContainsKey(k + 1))
                                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 3);
                                                else
                                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 1);
                                            }
                                        }
                                        else
                                        {
                                            for (int k = 0; k < 6; k++)
                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);
                                        }
                                        
                                        // Target Port
                                        int TargetPortID = _jobcontrol.PJlist[PJName].SourceTransInfo[SourceCarrier].TargetPortID;
                                        if (_EQ.GetLoadPort(TargetPortID).Carrier != null)
                                        {
                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                            _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _EQ.GetLoadPort(TargetPortID).Carrier.ID);
                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 6);
                                            for (int k = 0; k < _EQ.GetLoadPort(TargetPortID).Carrier.GetMappingdata.Length; k++)
                                            {
                                                if (_EQ.GetLoadPort(TargetPortID).Carrier.MaterialList.ContainsKey(k + 1))
                                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 3);
                                                else
                                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 1);
                                            }
                                        }
                                        else
                                        {
                                            _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "");
                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 6);
                                            for (int k = 0; k < 6; k++)
                                            {
                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);
                                            }
                                        }

                                    }
                                    */
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrMtlType");
                                    _secsdriver.DataItemOut(SecsFormateType.B, ref secsMsg, 0x0D);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrProcessStart");
                                    int AutoStart = (_jobcontrol.PJlist[PJName].AutoStart) ? 255 : 0;
                                    _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, AutoStart);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrRecipeMethod");
                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "RecID");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, _jobcontrol.PJlist[PJName].RecipeName);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "RecVariableList");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                }
                            }
                            break;
                        #endregion

                        #region ControlJob
                        case "ControlJob":
                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.CJlist.Count);
                            foreach (string CJName in _jobcontrol.CJlist.Keys)
                            {
                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, CJName);

                                if (ListAttrID.Count > 0)
                                {
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, ListAttrID.Count);
                                    for (int j = 0; j < ListAttrID.Count; j++)
                                    {
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ListAttrID[j]);
                                        switch (ListAttrID[j])
                                        {
                                            case "ObjType":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ControlJob");
                                                break;
                                            case "ObjID":
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, CJName);
                                                break;
                                            case "CarrierInputSpec":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.CJlist[CJName].CarrierInputSpecID.Count);
                                                foreach (string CarrierSpec in _jobcontrol.CJlist[CJName].CarrierInputSpecID)
                                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, CarrierSpec);
                                                break;
                                            case "CurrentPrJob":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.CJlist[CJName].PJList.Count);
                                                foreach (SProcessJobObject PJObject in _jobcontrol.CJlist[CJName].PJList.Values)
                                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.ID);
                                                break;
                                            case "DataCollectionPlan":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                break;
                                            case "MtrlOutByStatus":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                break;
                                            case "MtrlOutSpec":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                //foreach (SProcessJobObject PJObject in _jobcontrol.CJlist[CJName].PJList.Values)
                                                //{
                                                //    for (int z = 0; z < PJObject.SourceTransInfo[PJObject.SourceCarrier].TransferList.Count; z++)
                                                //    {
                                                //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                                //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                                //        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.SourceCarrier);
                                                //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                                                //        _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, PJObject.SourceTransInfo[PJObject.SourceCarrier].TransferList[z].SouceSlot);
                                                //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                                //        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.SourceTransInfo[PJObject.SourceCarrier].TargetCarrierID);
                                                //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                                                //        _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, PJObject.SourceTransInfo[PJObject.SourceCarrier].TransferList[z].TargetSlot);
                                                //    }
                                                //}
                                                break;
                                            case "PauseEvent":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                break;
                                            case "ProcessingCtrlSpec":
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.CJlist[CJName].PJCount);
                                                foreach (SProcessJobObject PJObject in _jobcontrol.CJlist[CJName].PJList.Values)
                                                {
                                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.ID);
                                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                                }
                                                break;
                                            case "ProcessOrderMgmt":
                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);
                                                break;
                                            case "StartMethod":
                                                int AutoStart = (_jobcontrol.CJlist[CJName].AutoStart == true) ? 255 : 0;
                                                _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, AutoStart);
                                                break;
                                            case "State":
                                                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, (int)_jobcontrol.CJlist[CJName].Status);
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 12);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjType");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ControlJob");

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjID");
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, CJName);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CarrierInputSpec");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.CJlist[CJName].CarrierInputSpecID.Count);
                                    foreach (string CarrierSpec in _jobcontrol.CJlist[CJName].CarrierInputSpecID)
                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, CarrierSpec);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CurrentPrJob");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.CJlist[CJName].PJList.Count);
                                    foreach (SProcessJobObject PJObject in _jobcontrol.CJlist[CJName].PJList.Values)
                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.ID);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "DataCollectionPlan");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "MtrlOutByStatus");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "MtrlOutSpec");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                    //foreach (SProcessJobObject PJObject in _jobcontrol.CJlist[CJName].PJList.Values)
                                    //{
                                    //    for (int z = 0; z < PJObject.SourceTransInfo[PJObject.SourceCarrier].TransferList.Count; z++)
                                    //    {
                                    //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    //        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.SourceCarrier);
                                    //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                                    //        _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, PJObject.SourceTransInfo[PJObject.SourceCarrier].TransferList[z].SouceSlot);
                                    //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    //        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.SourceTransInfo[PJObject.SourceCarrier].TargetCarrierID);
                                    //        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                                    //        _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, PJObject.SourceTransInfo[PJObject.SourceCarrier].TransferList[z].TargetSlot);
                                    //    }
                                    //}

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PauseEvent");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ProcessingCtrlSpec");
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.CJlist[CJName].PJCount);
                                    foreach (SProcessJobObject PJObject in _jobcontrol.CJlist[CJName].PJList.Values)
                                    {
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.ID);
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                                    }

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ProcessOrderMgmt");
                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "StartMethod");
                                    int AutoStart = (_jobcontrol.CJlist[CJName].AutoStart == true) ? 255 : 0;
                                    _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, AutoStart);

                                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "State");
                                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, (int)_jobcontrol.CJlist[CJName].Status);

                                }

                            }
                            break;
                        #endregion

                        #region waferobject
                        case "Wafer":
                              _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2); // L1=Wafer ,L2 = <U1 0>

                            /*for (int i = 0; i < SecsGEMUtilty.ListSTK.Count; i++)//0,1,2,3
                            {
                                foreach (List<SWafer> item1 in SecsGEMUtilty.ListSTK[i].WaferData.Values.ToArray())
                                {


                                    wafercount = wafercount + item1.Where(x => x != null).Count();
                                }

                            }

                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, wafercount);

                            for (int i = 0; i < SecsGEMUtilty.ListSTK.Count; i++)//0,1,2,3
                            {

                                foreach (var item1 in SecsGEMUtilty.ListSTK[i].WaferData.ToArray())
                                {
                                    if (item1.Value == null) continue;

                                    List<SWafer> listWafer = item1.Value.ToList();//一次看25片
                                    for (int j = 0; j < listWafer.Count; j++)//0~24
                                    {
                                        SWafer wafer = listWafer[j];
                                        if (wafer == null)
                                            continue;
                                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2); // waferID , ListAttrID

                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.WaferID_B);

                                        if (ListAttrID.Count > 0)
                                        {
                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, ListAttrID.Count);
                                            for (int z = 0; z < ListAttrID.Count; z++)
                                            {
                                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ListAttrID[z]);
                                                switch (ListAttrID[z])
                                                {
                                                    case "ObjID":
                                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.WaferID_B);
                                                        break;

                                                    case "CatID":
                                                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.GradeID);
                                                        break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                            _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjID");
                                            _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.WaferID_B);

                                            _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                                            _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CatID");
                                            _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, wafer.GradeID);

                                        }

                                    }
                                }

                            }

                            // _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2); // L1=Wafer ,L2 = <U1 0>
                            //  _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, 0);
                            // _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

                            */


                            break;
                        #endregion


                        default:
                            nAck = 1;
                            ErrCode = 4;
                            ErrText = "unknown attribute type";
                            break;

                    }
                }
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, nAck);
                if (nAck == 0)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, ErrCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrText);
                }
                _secsdriver.SendMessage(QsStream.S14, QsFunction.F2, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("<<<Warning>>>S14F2:" + ex.Message);
            }
        }

        void R_S14F7(object secsMsg, SecsMessageObject1Args secsObject)
        {
            string objspec = string.Empty;
            string objtype = string.Empty;
            int ErrorCode = 0;
            string ErrorText = string.Empty;
            int ObjIDCount = 0;
            List<string> _lisOBJTYPE = new List<string>();
            string OBJTYPE = string.Empty;
            List<string> _listAttrID = new List<string>();
            try
            {
                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                ObjIDCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                for (int i = 0; i < ObjIDCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    OBJTYPE = (string)Value;
                    switch (OBJTYPE.ToUpper())
                    {
                        case "CARRIER":
                        case "CONTROLJOB":
                        case "PROCESSJOB":
                            _lisOBJTYPE.Add(OBJTYPE);
                            break;
                        default:
                            throw new StreamFuntionException(1, 1, string.Format("OBJTYPE ={0} is not Exist", OBJTYPE));
                    }

                }
                S_S14F8(secsMsg, secsObject, _lisOBJTYPE, 0, ErrorCode, ErrorText);
            }
            catch (StreamFuntionException ex)
            {
                S_S14F8(secsMsg, secsObject, _lisOBJTYPE, ex.nAck, ex.nErrorCode, ex.Message);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S14F8(secsMsg, secsObject, _lisOBJTYPE, 1, 2, "Other Error");
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S14F8(object secsMsg, SecsMessageObject1Args secsObject, List<string> OBJTYPEList, int ACK, int ErrCode, string ErrText)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                if (ACK == 0)
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, OBJTYPEList.Count);
                    foreach (string OBJTYPE in OBJTYPEList)
                    {
                        _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                        _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, OBJTYPE);
                        switch (OBJTYPE)
                        {
                            case "Carrier":
                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 10);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjType");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjID");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Capacity");
                                // _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CarrierAccessingStatus");
                                // _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CarrierIDStatus");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ContentMap");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "LocationID");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "SlotMap");
                                // _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "SlotMapStatus");
                                // _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "Usage");
                                break;

                            case "ProcessJob":
                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 10);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjType");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjID");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PauseEvent");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrJobState");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrMtlNameList");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrMtlType");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrProcessStart");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PrRecipeMethod");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "RecID");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "RecVariableList");
                                break;
                            case "ControlJob":
                                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 12);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjType");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ObjID");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CarrierInputSpec");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "CurrentPrJob");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "DataCollectionPlan");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "MtrlOutByStatus");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "MtrlOutSpec");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "PauseEvent");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ProcessingCtrlSpec");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "ProcessOrderMgmt");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "StartMethod");
                                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, "State");

                                break;

                        }
                    }
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, ACK);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);

                }
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, ACK);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U4, ref secsMsg, ErrCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrText);

                }

                _secsdriver.SendMessage(QsStream.S14, QsFunction.F8, secsMsg, secsObject);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }


        private void R_S14F9(object secsMsg, SecsMessageObject1Args secsObject)//CJ
        {
            int nAck = 0, SublistCount2 = 0;
            string ErrorParam = string.Empty;
            List<string> CJIDlist = new List<string>();
            int ErrorCode = 0;
            string CJID = string.Empty;
            string ErrorAttributeName = string.Empty;
            string ErrorAttributeValue = string.Empty;
            List<string> waferIDList = new List<string>();
            Dictionary<int, List<int>> Dc_WaferList = new Dictionary<int, List<int>>();
            List<int> L_WaferMap = new List<int>();

            SWafer CurrentWafer = null;

            SProcessJobObject PJobject;

            try
            {
                if (_GEMControlstats != GEMControlStats.ONLINEREMOTE)
                    throw new StreamFuntionException(2, 13, "Current mode is not Online Remote, Command Fail");


                int ListCount = 0;
                int SubListCount = 0;

                int PJListCount = 0;
                int MTRLOUTSPECCount = 0;
                bool AutoStrat = false;
                bool FindCarrier = false;
                Dictionary<string, int> carrierList = new Dictionary<string, int>();
                Dictionary<string, int> ExutePjList = new Dictionary<string, int>();
                SControlJobObject CJobject;
                List<string> CarrierInputSpecID = new List<string>();
                int PjCount = 1;
                string AttributeName = string.Empty;
                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                if (((string)Value).ToUpper() != "CONTROLJOB")
                    throw new StreamFuntionException(2, 5, string.Format("Attribute Error,CONTROLJOB != {0}", (string)Value));
                ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);

                for (int j = 1; j < SecsGEMUtilty.LoadPortList.Count; j++)
                {
                    L_WaferMap = new List<int>();
                    for (int k = 0; k < SecsGEMUtilty.LoadPortList[j].Waferlist.Count; k++)
                    {

                        if (SecsGEMUtilty.LoadPortList[j].Waferlist[k] != null)
                        {
                            L_WaferMap.Add(1); //有Wafer
                        }
                        else
                        {

                            L_WaferMap.Add(0);//沒有Wafer
                        }

                    }
                    Dc_WaferList.Add(j, L_WaferMap);

                }








                for (int i = 0; i < ListCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    AttributeName = (string)Value;
                    switch (AttributeName.ToUpper())
                    {
                        #region OBJID

                        case "OBJID":
                            _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                            CJID = (string)Value;
                            if (_jobcontrol.CJlist.ContainsKey(CJID))
                            {
                                ErrorAttributeName = "OBJID";
                                ErrorAttributeValue = "CJID";
                                throw new StreamFuntionException(2, 5, string.Format("CJID={0} is exist", CJID));
                            }
                            break;
                        #endregion

                        #region DATACOLLECTIONPLAN

                        case "DATACOLLECTIONPLAN":
                            _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                            break;
                        #endregion

                        #region CARRIERINPUTSPEC
                        case "CARRIERINPUTSPEC":
                            SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);

                            for (int j = 0; j < SubListCount; j++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);

                                foreach (I_Loadport port in SecsGEMUtilty.LoadPortList.Values)
                                {
                                    if (port.FoupID == (string)Value && (port.StatusMachine == enumStateMachine.PS_Docked || port.StatusMachine == enumStateMachine.PS_Complete))
                                    {
                                        FindCarrier = true;
                                        carrierList.Add((string)Value, port.BodyNo);
                                        break;
                                    }
                                }
                                if (!FindCarrier)
                                    throw new StreamFuntionException(2, 5, string.Format("CarrierID={0} is not exist", (string)Value));
                            }

                            break;
                        #endregion

                        #region MTRLOUTSPEC
                        case "MTRLOUTSPEC":
                            MTRLOUTSPECCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            bool[] bApplyEQ = new bool[4] { false, false, false, false };
                            for (int j = 0; j < MTRLOUTSPECCount; j++)
                            {
                                SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                if (SubListCount != 2)
                                    throw new StreamFuntionException(2, 5, string.Format("MTRLOUTSPEC list  count error."));

                                string SourceCarrierID = string.Empty;
                                string TagetCarrierID = string.Empty;
                                int SourcePort = 0;
                                int SourceSlot = 0;
                                int TagetSlot = 0;
                                int TagetPort = 0;
                                string ExcutPJID = string.Empty;
                                string TempWafer_LotID = "";
                                string TempWafer_WaferID_B = "";
                                FindCarrier = false;
                                
                                for (int k = 0; k < SubListCount; k++)

                                {
                                    SublistCount2 = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                    if (SublistCount2 != 2)
                                        throw new StreamFuntionException(2, 5, string.Format("MTRLOUTSPEC Sub List count error."));
                                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                                    if (!FindCarrier)
                                    {
                                        SourceCarrierID = (string)Value;
                                        _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                        _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                                        SourceSlot = (int)Value;
                                        CurrentWafer = null;

                                        foreach (string PJID in _jobcontrol.PJlist.Keys)
                                        {
                                            foreach (string CarrierID in _jobcontrol.PJlist[PJID].GetSourceCarrierList())
                                            {
                                                if (CarrierID == SourceCarrierID
                                                    //&& _jobcontrol.PJlist[PJID].SourceTransInfo[CarrierID].TransferList.Contains(SourceSlot))
                                                    && _jobcontrol.PJlist[PJID].ContainsSourceSlot(CarrierID, SourceSlot))
                                                {
                                                    for (int l = 1; l < SecsGEMUtilty.LoadPortList.Count; l++)
                                                    {
                                                        if (SecsGEMUtilty.LoadPortList[l].FoupID == SourceCarrierID && (SecsGEMUtilty.LoadPortList[l].StatusMachine == enumStateMachine.PS_Docked || SecsGEMUtilty.LoadPortList[l].StatusMachine == enumStateMachine.PS_Complete))
                                                        {
                                                            ExcutPJID = PJID;

                                                            SourcePort = l;
                                                            FindCarrier = true;
                                                            CurrentWafer = SecsGEMUtilty.LoadPortList[l].Waferlist[SourceSlot - 1];
                                                            break;
                                                        }
                                                    }

                                                    if (FindCarrier)
                                                        break;
                                                }

                                            }
                                        }
                                        if (!FindCarrier)
                                            throw new StreamFuntionException(2, 5, string.Format("MTRLOUTSPEC Source CarrierID={0} is not find defind or is not exist.", (string)Value));

                                        if (_jobcontrol.PJlist[ExcutPJID].ContainsSourceSlot(SourceCarrierID, SourceSlot) == false)
                                            throw new StreamFuntionException(2, 5, string.Format("MTRLOUTSPEC Source CarrierID={0} , Slot={1} is not exist.", SourceCarrierID, SourceSlot));
                                    }
                                    else
                                    {
                                        FindCarrier = false;
                                        TagetCarrierID = (string)Value;
                                        TagetPort = 0;
                                        for (int l = 1; l < SecsGEMUtilty.LoadPortList.Count; l++)
                                        {
                                            if (SecsGEMUtilty.LoadPortList[l].FoupID == TagetCarrierID && (SecsGEMUtilty.LoadPortList[l].StatusMachine == enumStateMachine.PS_Docked || SecsGEMUtilty.LoadPortList[l].StatusMachine == enumStateMachine.PS_Complete))
                                            {
                                                TagetPort = l;
                                                FindCarrier = true;
                                                break;
                                            }
                                        }
                                        if (!FindCarrier)
                                            throw new StreamFuntionException(2, 5, string.Format("MTRLOUTSPEC Target CarrierID={0} is not find defind or is not exist.", (string)Value));
                                        _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                        _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                                        TagetSlot = (int)Value;

                                        if ((SourcePort != TagetPort && Dc_WaferList[TagetPort][TagetSlot - 1] != 0)
                                            ||
                                            (SourcePort == TagetPort && SourceSlot != TagetSlot && Dc_WaferList[TagetPort][TagetSlot - 1] != 0)
                                            )
                                            throw new StreamFuntionException(2, 5, string.Format("MTRLOUTSPEC Target CarrierID={0} , Slot={1} is  exist.", TagetCarrierID, TagetSlot));

                                    }

                                }

                                if (_jobcontrol.PJlist[ExcutPJID].ContainsTargetSlot(TagetCarrierID, TagetSlot))
                                {
                                    _jobcontrol.PJlist[ExcutPJID].ClearSourceTransferInfo();
                                    throw new StreamFuntionException(2, 5, string.Format("MTRLOUTSPEC Target CarrierID={0} , Slot={1} is  Ready assign .", TagetCarrierID, TagetSlot));
                                }


                                //221025...
                                //_jobcontrol.PJlist[ExcutPJID].SourceTransInfoList[SourceCarrierID].AssignInfo(SourceSlot, TagetSlot);
                                //_jobcontrol.PJlist[ExcutPJID].SourceTransInfoList[SourceCarrierID].TargetBodyNo = TagetPort;
                                //_jobcontrol.PJlist[ExcutPJID].SourceTransInfoList[SourceCarrierID].TargetCarrierID = TagetCarrierID;


                                PJobject = _jobcontrol.PJlist[ExcutPJID];

 								 if (CurrentWafer != null)
                                {
                                    TempWafer_LotID = CurrentWafer.LotID;
                                    TempWafer_WaferID_B = CurrentWafer.WaferInforID_B;

                                }
                                else
                                {
                                    TempWafer_LotID = "";
                                    TempWafer_WaferID_B = "";
                                }



                                _jobcontrol.PJlist[ExcutPJID].AssginSourceSlotInfo(SourceCarrierID, SourceSlot, TagetCarrierID, TagetSlot, TagetPort, bApplyEQ,
                                    WaferID: TempWafer_WaferID_B, lotID: TempWafer_LotID,
                                    dNotchAngle: PJobject.Align_Angle, UseAligner: PJobject.Use_Align, UseOCR: PJobject.Use_OCR, OCR_Recipe: PJobject.RecipeName);

                                FindCarrier = false;

                                Dc_WaferList[SourcePort][SourceSlot - 1] = 0;
                                Dc_WaferList[TagetPort][TagetSlot - 1] = 1;



                                if (j == MTRLOUTSPECCount - 1)
                                {
                                    _jobcontrol.PJlist[ExcutPJID].CheckAssginSourceSlotInfoResult(SourceCarrierID);

                                }

                            }

                            break;
                        #endregion

                        case "MTRLOUTBYSTATUS":
                            _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            break;

                        case "PAUSEEVENT":
                            _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            break;

                        #region PROCESSINGCTRLSPEC
                        case "PROCESSINGCTRLSPEC":
                            PJListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            if (PJListCount < 1)
                            {
                                ErrorAttributeName = "PROCESSINGCTRLSPEC";
                                ErrorAttributeValue = "";
                                throw new StreamFuntionException(2, 5, string.Format("PROCESSINGCTRLSPEC have Error."));
                            }
                            for (int j = 0; j < PJListCount; j++)
                            {
                                SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                                if (ExutePjList.ContainsKey((string)Value))
                                {
                                    ErrorAttributeName = "PROCESSINGCTRLSPEC";
                                    ErrorAttributeValue = (string)Value;
                                    throw new StreamFuntionException(2, 5, string.Format("PJID={0} is assign again.", (string)Value));
                                }
                                if (!_jobcontrol.PJlist.ContainsKey((string)Value))
                                {
                                    ErrorAttributeName = "PROCESSINGCTRLSPEC";
                                    ErrorAttributeValue = (string)Value;
                                    throw new StreamFuntionException(2, 5, string.Format("PJID={0} is not find defind or is not exist.", (string)Value));
                                }
                                ExutePjList.Add((string)Value, j);
                                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                            }

                            break;
                        #endregion

                        case "PROCESSORDERMGMT":
                            _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                            break;
                        case "STARTMETHOD":
                            _secsdriver.DataItemIn(SecsFormateType.Bool, ref secsMsg, ref Value);
                            if ((int)Value > 0)
                                AutoStrat = true;
                            else
                                AutoStrat = false;
                            break;
                    }
                }


                if (MTRLOUTSPECCount == 0) // MTRLOUTSPEC if count =0 , 原去原回，目前只支援DISPLAY
                {
                    foreach (string excuteCarrier in carrierList.Keys)
                    {
                        foreach (string ExcutePJ in ExutePjList.Keys)
                        {

                            /*
                            if (_jobcontrol.PJlist[ExcutePJ].Action != BWSAction.SorterFunc)
                                continue;

                            if (_jobcontrol.PJlist[ExcutePJ].RecipeName.ToUpper() != "DISPLAY")
                                continue;
                            */

                            if (_jobcontrol.PJlist[ExcutePJ].SourceTransInfoList.Where(x => x.SourceCarrierID == excuteCarrier).Count() < 1)
                            //if (_jobcontrol.PJlist[ExcutePJ].SourceTransInfoList.Where(x => x.Key == excuteCarrier).Count() < 1)
                            { throw new StreamFuntionException(2, 5, string.Format("PJID={0} is not find carrier = {1} .", ExcutePJ, excuteCarrier)); }



                            PJobject = _jobcontrol.PJlist[ExcutePJ];
                            bool[] bApplyEQ = new bool[4] { false, false, false, false };

                            for (int i = 1; i <= 25; i++)
                            {

                                if (PJobject.ContainsSourceSlot(excuteCarrier, i) == true)
                                {
                                    _jobcontrol.PJlist[ExcutePJ].AssginSourceSlotInfo(excuteCarrier, i, excuteCarrier, i, carrierList[excuteCarrier], bApplyEQ,
                                    dNotchAngle: PJobject.Align_Angle, UseAligner: PJobject.Use_Align, UseOCR: PJobject.Use_OCR, OCR_Recipe: PJobject.RecipeName);



                                }



                            }

                        }
                    }
                }

                S_S14F10(secsMsg, nAck, ErrorParam, ErrorCode, CJID, ErrorAttributeName, ErrorAttributeValue, secsObject);
                CJobject = new SControlJobObject(CJID);
                CJobject.CarrierInputSpecID = CarrierInputSpecID;
                foreach (string ExcutePJ in ExutePjList.Keys)
                {
                    CJobject.AssignPJ(PjCount++, _jobcontrol.PJlist[ExcutePJ]);
                }

                CJobject.AutoStart = AutoStrat;
                _jobcontrol.CJlist.Add(CJID, CJobject);
                _jobcontrol.CJlist[CJID].MTRLOUTSPECCount = MTRLOUTSPECCount;
                _jobcontrol.CJlist[CJID].OnControlJobStatesChange += SGEM300_OnControlJobStatesChange;

                _jobcontrol.CJlist[CJID].Status = JobStatus.QUEUED;



                if (AutoStrat == false)
                {
                    _jobcontrol.CJlist[CJID].Status = JobStatus.Select;
                    _jobcontrol.CJlist[CJID].Status = JobStatus.WaitFotHost;
                }

                SecsGEMUtilty.Exejob(false, CJID);

            }
            catch (StreamFuntionException ex)
            {
                S_S14F10(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, CJID, ErrorAttributeName, ErrorAttributeValue, secsObject);

                if (_jobcontrol.CJlist.ContainsKey(CJID))
                {
                    _jobcontrol.CJlist[CJID].OnControlJobStatesChange -= SGEM300_OnControlJobStatesChange;
                    _jobcontrol.CJlist.Clear();
                }
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S14F10(secsMsg, 5, "Other Error", 2, CJID, ErrorAttributeName, ErrorAttributeValue, secsObject);

                if (_jobcontrol.CJlist.ContainsKey(CJID))
                {
                    _jobcontrol.CJlist[CJID].OnControlJobStatesChange -= SGEM300_OnControlJobStatesChange;
                    _jobcontrol.CJlist.Clear();
                }
                WriteLog("[Exception] " + ex);
            }
        }
        private void S_S14F10(object secsMsg, int Ack, string ErrorParam, int ErrorCode, string CJID, string ErrorAttributeName, string ErrorAttributeValue, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 3);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, CJID);
                if (Ack == 0)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorAttributeName);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorAttributeValue);

                }

                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, Ack);
                if (Ack == 0)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U2, ref secsMsg, ErrorCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);
                }

                _secsdriver.SendMessage(QsStream.S14, QsFunction.F10, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        /// <summary>
        /// S16FX
        /// </summary>
        void R_S16F5(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string ErrorParam = string.Empty;
            int ErrorCode = 0;
            string PJName = string.Empty;
            string PRCMD = string.Empty;
            object Value = new object();
            try
            {
                if (_GEMControlstats != GEMControlStats.ONLINEREMOTE)
                    throw new StreamFuntionException(2, 13, "Current mode is not Online Remote, Command Fail");



                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                PJName = (string)Value;
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                PRCMD = (string)Value;
                if (!_jobcontrol.PJlist.ContainsKey(PJName))
                    throw new StreamFuntionException(1, 1, string.Format("Not find PJ Name = {0}", PJName));

                switch (PRCMD.ToUpper())
                {
                    #region START
                    case "START":
                        //if (_jobcontrol.PJlist[PJName].Status == JobStatus.QUEUED && !_jobcontrol.PJlist[PJName].AutoStart)
                        //{
                        //    _jobcontrol.PJlist[PJName].AutoStart = true;
                        //    if (GMotion.theInst.Transfer.ProsessStart == false)
                        //        GMotion.theInst.Transfer.ProsessStart = true;
                        //}
                        //else
                        //    throw new StreamFuntionException(1, 1, string.Format("PJ Name = {0} Status Error", PJName));


                        break;
                    #endregion

                    #region CANCEL
                    case "ABORT":
                    case "CANCEL":
                        if (_jobcontrol.PJlist[PJName].Status == JobStatus.QUEUED)
                        {
                            _jobcontrol.PJlist[PJName].Status = JobStatus.Destroy;
                            _jobcontrol.PJlist.Remove(PJName);
                        }
                        else
                            throw new StreamFuntionException(1, 1, string.Format("PJ Name = {0} Status Error", PJName));
                        break;
                    #endregion

                    case "STOP":
                        if (_jobcontrol.PJlist[PJName].Status == JobStatus.EXECUTING)
                        {
                            _jobcontrol.PJlist[PJName].Status = JobStatus.STOPPING;

                        }
                        else
                            throw new StreamFuntionException(1, 1, string.Format("PJ Name = {0} Status Error", PJName));
                        break;
                    case "PAUSE":
                    case "RESUME":

                    default:
                        throw new StreamFuntionException(1, 1, string.Format("CMD = {0} is Not Support", PJName));

                }

                S_S16F6(secsMsg, nAck, ErrorParam, ErrorCode, PJName, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                S_S16F6(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, PJName, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S16F6(secsMsg, 1, "Other Error", 1, PJName, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S16F6(object secsMsg, int Ack, string ErrorParam, int ErrorCode, string PJName, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJName);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                int nack = (Ack == 0) ? 255 : 0;
                _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, nack);
                if (Ack == 0)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, ErrorCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);

                }

                _secsdriver.SendMessage(QsStream.S16, QsFunction.F6, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        void R_S16F11(object secsMsg, SecsMessageObject1Args secsObject)//create pj single
        {
            int nAck = 0;
            string ErrorParam = string.Empty;
            List<string> PjIDlist = new List<string>();
            int ErrorCode = 0;

            Dictionary<string, List<int>> SourceSlotList = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> TargetSlotList = new Dictionary<string, List<int>>();

            bool FindWaferID = false;
            bool FindGrade = false;
            BWSAction Action = BWSAction.SorterFunc;

            Dictionary<string, string> ExecuteWaferList = new Dictionary<string, string>();//Wafer IN
            Dictionary<string, ExecuteWaferOutInfo> WaferOutInfo = new Dictionary<string, ExecuteWaferOutInfo>();

            string TempWaferID = string.Empty;
            string TempTowerSlot = string.Empty;
            string TempCarrierID = string.Empty;
            int TempFoupSlot = 0;
            string TempGrade = string.Empty;
            int Subcount = 0;
            DataTable GradeTable;
            DataTable WaferTable;

            bool WaferOutUseAligner = false;
            bool WaferOutUseOCR = false;

            string OCR_Recipe = "";
            double Align_Angle = -1;
           

            bool Use_Align = false;
            bool Use_OCR = false;

            Dictionary<string, SProcessJobObject> PJlistTemp = new Dictionary<string, SProcessJobObject>();
            try
            {
                if (this._GEMControlstats != GEMControlStats.ONLINEREMOTE)
                    throw new StreamFuntionException(2, 13, "Current mode is not Online Remote, Command Fail");

                int PJListCount = 0;
                int CarrierListCount = 0;
                int SlotListCount = 0;
                int SubListCount = 0;
                int WaferInOutCount = 0;
                int UsePort = 0;
                int targetPort = 0;
                int UseSlot = 0;
                int UseRecipeTuring = 0;
                string RecipeName = string.Empty;
                int Slot = 0;
                string PJID = string.Empty;
                string CarrierID = string.Empty;
                bool FindCarrier = false;
                string RecipeParamName = string.Empty;

                string HostLotID = string.Empty;
                int AssignSlotCount = 0;
                int AssignTargetSlot = 0;

                SProcessJobObject PJobject;
                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);

                UsePort = 0;
                SourceSlotList.Clear();
                 SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);


                if (SubListCount != 7)
                        throw new StreamFuntionException(2, 5, string.Format("List count Error"));

                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    PJID = (string)Value;

                    if (_jobcontrol.PJlist.ContainsKey(PJID))
                    {
                        PjIDlist.Add(PJID);
                        throw new StreamFuntionException(2, 5, string.Format("PJID={0} is exist", PJID));
                    }

                    PJobject = new SProcessJobObject(PJID);
                    _secsdriver.DataItemIn(SecsFormateType.B, ref secsMsg, ref Value);
                    CarrierListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);

                    if (CarrierListCount == 0)
                    {
                        PjIDlist.Add(PJID);
                        throw new StreamFuntionException(2, 5, "Carrier List have Error");
                    }

                    for (int j = 0; j < CarrierListCount; j++)
                    {
                        _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                        CarrierID = (string)Value;

                        #region FixBuffer

                        foreach (I_Loadport Port in SecsGEMUtilty.LoadPortList.Values)
                        {

                            if (Port.FoupID == CarrierID)
                            {

                                if (Port.StatusMachine != enumStateMachine.PS_Docked && Port.StatusMachine != enumStateMachine.PS_Complete)
                                    break;
                                if (Port.CarrierIDstatus != CarrierIDStats.IDVerificationok)
                                    break;
                                if (Port.SlotMappingStats != CarrierSlotMapStats.SlotMappingVerificationok)
                                    break;

                                FindCarrier = true;
                                if (j == 0)
                                UsePort = Port.BodyNo;
                                else if (j == 1)
                                    targetPort = Port.BodyNo;
                                break;

                            }
                        }
                        if (!FindCarrier)
                        {
                            PjIDlist.Add(PJID);
                            throw new StreamFuntionException(2, 5, "Carrier not find.");
                        }

                        FindCarrier = false;
                        SlotListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        if (SlotListCount == 0)
                        {
                            PjIDlist.Add(PJID);
                            throw new StreamFuntionException(2, 5, "Slot List is Error.");
                        }

                        // CarrierID 找到對應的Loadport要傳送
                        if (j == 0)
                        {
                            /*for (int k = 0; k < SlotListCount; k++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                                Slot = (int)Value;

                                if (!SourceSlotList.ContainsKey(CarrierID))
                                    SourceSlotList.Add(CarrierID, new List<int>());

                                SourceSlotList[CarrierID].Add(Slot);
                            }*/
                        PJobject.CreateSourceTransInfo(UsePort, CarrierID);//建立CarrierID要傳送 ming

                        for (int k = 0; k < SlotListCount; k++)
                        {
                            _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                            Slot = (int)Value;

                            if (SecsGEMUtilty.GetMappingData(UsePort)[Slot - 1] != '1')
                                throw new StreamFuntionException(2, 5, string.Format("Slot {0} is not find", Slot));

                            if (true == PJobject.ContainsSourceSlot(CarrierID, Slot))
                                throw new StreamFuntionException(2, 5, string.Format("Slot {0} have Assign again.", Slot));

                            PJobject.CreateSourceSlotInfo(CarrierID, Slot);//建立CarrierID中哪一片slot要傳送 ming

                        }
                        }
                        else
                        {
                            for (int k = 0; k < SlotListCount; k++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                                Slot = (int)Value;

                                if (!TargetSlotList.ContainsKey(CarrierID))
                                    TargetSlotList.Add(CarrierID, new List<int>());

                                TargetSlotList[CarrierID].Add(Slot);
                            }
                        }
                        //   break;

                        #endregion


                    }
                    _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                    _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                    UseRecipeTuring = (int)Value;

                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    RecipeName = (string)Value;

                    // Check Recipe 
                    //if (SecsGEMUtilty.GetAllRecipeList((int)SecsGEMUtilty.LoadPortList[UsePort].FoupSize).Where(x => x == RecipeName).Count() < 1)
                    //    throw new StreamFuntionException(2, 5, "Recipe not find ");

                    PJobject.RecipeName = RecipeName;


                    if (//PJobject.RecipeName.ToUpper() == "VERIFY" ||
                        PJobject.RecipeName.ToUpper() == "TRANSFER" ||
                        PJobject.RecipeName.ToUpper() == "DISPLAY"
                        //PJobject.RecipeName.ToUpper() == "COMPRESS" ||
                        //PJobject.RecipeName.ToUpper() == "MONITOR"
                        )
                    {
                        Action = BWSAction.SorterFunc;
                        PJobject.Action = BWSAction.SorterFunc;
                    }
                    else
                    {
                        //Action = BWSAction.WaferIn;
                        throw new StreamFuntionException(2, 5, string.Format("RecipeName={0} is error", RecipeName));

                    }


                    SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);

                    #region RecipeParamter
                    for (int x = 0; x < SubListCount; x++)
                    {
                        _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);

                        switch (((string)Value).ToUpper())
                        {
                            case "TRANSFER_MAP":

                                break;

                            case "SOURCE_CARRIERID":

                                break;
                            case "ALIGN_ANGLE":
                                _secsdriver.DataItemIn(SecsFormateType.F8, ref secsMsg, ref Value);

                                if ((double)Value >= 0 && (double)Value <= 360)
                                {
                                    //User要求不要卡角度
                                    Use_Align = true;
                                    Align_Angle = (double)Value;

                                    if ((double)Value == 0 || (double)Value == 45 || (double)Value == 90 || (double)Value == 135 || (double)Value == 180
                                        || (double)Value == 225 || (double)Value == 270 || (double)Value == 315 || (double)Value == 360)
                                    {
                                        Use_Align = true;
                                        Align_Angle = (double)Value;
                                        if (Align_Angle == 360)
                                        { //單體只能接受0~359
                                            Align_Angle = 0;

                                        }
                                    }
                                    else
                                    {
                                        throw new StreamFuntionException(2, 5, string.Format("ALIGN_ANGLE={0} is error", Value));

                                    }



                                }
                                else if ((double)Value == -1)
                                {
                                    Use_Align = false;
                                }
                                else
                                {

                                    throw new StreamFuntionException(2, 5, string.Format("ALIGN_ANGLE={0} is error,must -1~360", Value));
                                }


                                break;

                            case "OCR_RECIPE":

                                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);

                                if (Value.ToString() != "")
                                {

                                    OCR_Recipe = Value.ToString();
                                    if (_GroupRecipeManager.GetRecipeGroupList.ContainsKey(OCR_Recipe) == false)
                                    {
                                        OCR_Recipe = "";
                                        throw new StreamFuntionException(2, 5, string.Format("Can Not Find Recipe={0}", Value));

                                    }
                                    Use_OCR = true;

                                }
                                else
                                {
                                    Use_OCR = false;
                                }
                                break;



                            
                        }

                    }

                    #endregion


                    _secsdriver.DataItemIn(SecsFormateType.Bool, ref secsMsg, ref Value);
                    if ((int)Value > 0)
                    PJobject.AutoStart = true;
                    else
                        PJobject.AutoStart = false;
                    _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);



                    //檢查各Recipe必要參數
                    if (PJobject.RecipeName.ToUpper() == "TRANSFER")
                    {
                        //User給什麼就做什麼
                    }

                    else if (PJobject.RecipeName.ToUpper() == "DISPLAY")
                    {
                        if (Use_OCR != true)
                        {
                            throw new StreamFuntionException(2, 5, string.Format("Recipe DISPLAY Need Parameter OCR_RECIPE"));
                        }
                        if (Use_Align != true)
                        {
                            throw new StreamFuntionException(2, 5, string.Format("Recipe DISPLAY Need Parameter ALIGN_ANGLE"));


                        }



                    }


                    if (Use_OCR == true)
                    {
                        /*
                        //要做OCR就一定要做Align
                        if (Use_Align != true)
                        {
                            throw new StreamFuntionException(2, 5, string.Format("Do OCR Need Parameter ALIGN_ANGLE"));


                        }*/
                    }





                    PJobject.Use_Align = Use_Align;
                    PJobject.Use_OCR = Use_OCR;
                    PJobject.OCR_Recipe = OCR_Recipe;
                    PJobject.Align_Angle = Align_Angle;
                  



                    PJlistTemp.Add(PJID, PJobject);
                    PjIDlist.Add(PJID);



                S_S16F12(secsMsg, nAck, ErrorParam, ErrorCode, PjIDlist, secsObject);

                foreach (string NewPJ in PJlistTemp.Keys)
                {
                    _jobcontrol.PJlist.Add(NewPJ, PJlistTemp[NewPJ]);
                    _jobcontrol.PJlist[NewPJ].OnProcessJobStatesChange += SGEM300_OnProcessJobStatesChange;
                    _jobcontrol.PJlist[NewPJ].Status = JobStatus.QUEUED;
                }
                PJlistTemp.Clear();

            }
            catch (StreamFuntionException ex)
            {
                PJlistTemp.Clear();
                S_S16F12(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, PjIDlist, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                PJlistTemp.Clear();
                S_S16F12(secsMsg, 2, "Other Error", 5, PjIDlist, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }

        void S_S16F12(object secsMsg, int Ack, string ErrorParam, int ErrorCode, List<string> PJ, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                if (PJ.Count > 0)
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJ[0]);
                else
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, " ");
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                int Success = (Ack == 0) ? 255 : 0;
                _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, Success);
                if (Success == 255)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U2, ref secsMsg, ErrorCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);

                }
                _secsdriver.SendMessage(QsStream.S16, QsFunction.F12, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }



        void R_S16F15(object secsMsg, SecsMessageObject1Args secsObject)//create pj multi
        {

            int nAck = 0;
            string ErrorParam = string.Empty;
            List<string> PjIDlist = new List<string>();
            int ErrorCode = 0;

            Dictionary<string, List<int>> SourceSlotList = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> TargetSlotList = new Dictionary<string, List<int>>();

            bool FindWaferID = false;
            bool FindGrade = false;
            BWSAction Action = BWSAction.SorterFunc;

            Dictionary<string, string> ExecuteWaferList = new Dictionary<string, string>();//Wafer IN
            Dictionary<string, ExecuteWaferOutInfo> WaferOutInfo = new Dictionary<string, ExecuteWaferOutInfo>();

            string TempWaferID = string.Empty;
            string TempTowerSlot = string.Empty;
            string TempCarrierID = string.Empty;
            int TempFoupSlot = 0;
            string TempGrade = string.Empty;
            int Subcount = 0;
            DataTable GradeTable;
            DataTable WaferTable;

            bool WaferOutUseAligner = false;
            bool WaferOutUseOCR = false;



            string OCR_Recipe = "";
            double Align_Angle = -1;

            bool Use_Align = false;
            bool Use_OCR = false;

            Dictionary<string, SProcessJobObject> PJlistTemp = new Dictionary<string, SProcessJobObject>();
            try
            {
                if (this._GEMControlstats != GEMControlStats.ONLINEREMOTE)
                    throw new StreamFuntionException(2, 13, "Current mode is not Online Remote, Command Fail");

                int PJListCount = 0;
                int CarrierListCount = 0;
                int SlotListCount = 0;
                int SubListCount = 0;
                int WaferInOutCount = 0;
                int UsePort = 0;
                int targetPort = 0;
                int UseSlot = 0;
                int UseRecipeTuring = 0;
                string RecipeName = string.Empty;
                int Slot = 0;
                string PJID = string.Empty;
                string CarrierID = string.Empty;
                bool FindCarrier = false;
                string RecipeParamName = string.Empty;

                string HostLotID = string.Empty;
                int AssignSlotCount = 0;
                int AssignTargetSlot = 0;

                SProcessJobObject PJobject;
                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.U4, ref secsMsg, ref Value);
                PJListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);

                for (int i = 0; i < PJListCount; i++)
                {
                    UsePort = 0;
                    SourceSlotList.Clear();
                    SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                    if (SubListCount != 6)
                        throw new StreamFuntionException(2, 5, string.Format("List count Error"));

                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    PJID = (string)Value;

                    if (_jobcontrol.PJlist.ContainsKey(PJID))
                    {
                        PjIDlist.Add(PJID);
                        throw new StreamFuntionException(2, 5, string.Format("PJID={0} is exist", PJID));
                    }

                    PJobject = new SProcessJobObject(PJID);
                    _secsdriver.DataItemIn(SecsFormateType.B, ref secsMsg, ref Value);
                    CarrierListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                    if (CarrierListCount == 0)
                    {
                        PjIDlist.Add(PJID);
                        throw new StreamFuntionException(2, 5, "Carrier List have Error");
                    }
                    for (int j = 0; j < CarrierListCount; j++)
                    {
                        _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                        CarrierID = (string)Value;

                        #region FixBuffer

                        foreach (I_Loadport Port in SecsGEMUtilty.LoadPortList.Values)
                        {

                            if (Port.FoupID == CarrierID)
                            {

                                if (Port.StatusMachine != enumStateMachine.PS_Docked && Port.StatusMachine != enumStateMachine.PS_Complete)
                                    break;
                                if (Port.CarrierIDstatus != CarrierIDStats.IDVerificationok)
                                    break;
                                if (Port.SlotMappingStats != CarrierSlotMapStats.SlotMappingVerificationok)
                                    break;

                                FindCarrier = true;
                                if (j == 0)
                                    UsePort = Port.BodyNo;
                                else if (j == 1)
                                    targetPort = Port.BodyNo;
                                break;

                            }
                        }
                        if (!FindCarrier)
                        {
                            PjIDlist.Add(PJID);
                            throw new StreamFuntionException(2, 5, "Carrier not find.");
                        }

                        FindCarrier = false;
                        SlotListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        if (SlotListCount == 0)
                        {
                            PjIDlist.Add(PJID);
                            throw new StreamFuntionException(2, 5, "Slot List is Error.");
                        }

                        // CarrierID 找到對應的Loadport要傳送
                        if (j == 0)
                        {
                            /*for (int k = 0; k < SlotListCount; k++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                                Slot = (int)Value;

                                if (!SourceSlotList.ContainsKey(CarrierID))
                                    SourceSlotList.Add(CarrierID, new List<int>());

                                SourceSlotList[CarrierID].Add(Slot);
                            }*/
                            PJobject.CreateSourceTransInfo(UsePort, CarrierID);//建立CarrierID要傳送 ming
                            for (int k = 0; k < SlotListCount; k++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                                Slot = (int)Value;

                                if (SecsGEMUtilty.GetMappingData(UsePort)[Slot - 1] != '1')
                                    throw new StreamFuntionException(2, 5, string.Format("Slot {0} is not find", Slot));

                                if (true == PJobject.ContainsSourceSlot(CarrierID, Slot))
                                    throw new StreamFuntionException(2, 5, string.Format("Slot {0} have Assign again.", Slot));

                                PJobject.CreateSourceSlotInfo(CarrierID, Slot);//建立CarrierID中哪一片slot要傳送 ming

                            }
                        }
                        else
                        {
                            for (int k = 0; k < SlotListCount; k++)
                            {
                                _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                                Slot = (int)Value;

                                if (!TargetSlotList.ContainsKey(CarrierID))
                                    TargetSlotList.Add(CarrierID, new List<int>());

                                TargetSlotList[CarrierID].Add(Slot);
                            }
                        }
                        //   break;

                        #endregion
                    }



                    _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                    _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                    UseRecipeTuring = (int)Value;

                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    RecipeName = (string)Value;

                    // Check Recipe 
                    //if (SecsGEMUtilty.GetAllRecipeList((int)SecsGEMUtilty.LoadPortList[UsePort].FoupSize).Where(x => x == RecipeName).Count() < 1)
                    //    throw new StreamFuntionException(2, 5, "Recipe not find ");

                    if (_GroupRecipeManager.GetRecipeGroupList.ContainsKey(RecipeName) == false)
                    {
                        RecipeName = "";
                        throw new StreamFuntionException(2, 5, string.Format("Can Not Find Recipe={0}", Value));

                    }


                    PJobject.RecipeName = RecipeName;



                    /*if (//PJobject.RecipeName.ToUpper() == "VERIFY" ||
                        PJobject.RecipeName.ToUpper() == "TRANSFER" ||
                        PJobject.RecipeName.ToUpper() == "DISPLAY"
                        //PJobject.RecipeName.ToUpper() == "COMPRESS" ||
                        //PJobject.RecipeName.ToUpper() == "MONITOR"
                        )
                    {
                        Action = BWSAction.SorterFunc;
                        PJobject.Action = BWSAction.SorterFunc;
                    }
                    else
                    {
                        //Action = BWSAction.WaferIn;
                        throw new StreamFuntionException(2, 5, string.Format("RecipeName={0} is error", RecipeName));

                    }*/


                    SubListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);

                    #region RecipeParamter
                    for (int x = 0; x < SubListCount; x++)
                    {
                        _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);



                                            switch (((string)Value).ToUpper())
                                            {


                                case "TRANSFER_MAP":

                                    break;

                                case "SOURCE_CARRIERID":

                                    break;


                                case "ALIGN_ANGLE":
                                    _secsdriver.DataItemIn(SecsFormateType.F8, ref secsMsg, ref Value);

                                    if ((double)Value >= 0 && (double)Value <= 360)
                                    {
                                       
                                        Use_Align = true;
                                        Align_Angle = (double)Value;

                                        if ((double)Value == 0 ||  (double)Value == 90 ||  (double)Value == 180
                                            ||  (double)Value == 270 || (double)Value == 360)
                                        {
                                            Use_Align = true;
                                            Align_Angle = (double)Value;
                                            if (Align_Angle == 360)
                                            { //單體只能接受0~359
                                                Align_Angle = 0;

                                        }

                                        }
                                        else
                                        {
                                            throw new StreamFuntionException(2, 5, string.Format("ALIGN_ANGLE={0} is error", Value));

                                        }



                                    }
                                    else if ((double)Value == -1)
                                    {
                                        Use_Align = false;
                                    }
                                    else
                                    {

                                        throw new StreamFuntionException(2, 5, string.Format("ALIGN_ANGLE={0} is error,must -1~360", Value));
                                    }


                                    break;

                                case "OCR_RECIPE":

                                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);

                                    if (Value.ToString() != "")
                                    {

                                        OCR_Recipe = Value.ToString();
                                        if (_GroupRecipeManager.GetRecipeGroupList.ContainsKey(OCR_Recipe) == false)
                                        {
                                            OCR_Recipe = "";
                                            throw new StreamFuntionException(2, 5, string.Format("Can Not Find Recipe={0}", Value));

                                        }
                                        Use_OCR = true;

                                    }
                                    else
                                    {
                                        Use_OCR = false;
                                    }
                                    break;



                            }

                        }
                        #endregion





                    _secsdriver.DataItemIn(SecsFormateType.Bool, ref secsMsg, ref Value);
                    if ((int)Value > 0)
                        PJobject.AutoStart = true;
                    else
                        PJobject.AutoStart = false;
                    _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);



                    //檢查各Recipe必要參數
                    if (PJobject.RecipeName.ToUpper() == "TRANSFER")
                    {
                        //User給什麼就做什麼
                    }

                    else if (PJobject.RecipeName.ToUpper() == "DISPLAY")
                    {
                        if (Use_OCR != true)
                        {
                            throw new StreamFuntionException(2, 5, string.Format("Recipe DISPLAY Need Parameter OCR_RECIPE"));
                        }
                        if (Use_Align != true)
                        {
                            throw new StreamFuntionException(2, 5, string.Format("Recipe DISPLAY Need Parameter ALIGN_ANGLE"));


                        }



                    }

                    if (Use_OCR == true)
                    {
                        /*
                        //要做OCR就一定要做Align
                        if (Use_Align != true)
                        {
                            throw new StreamFuntionException(2, 5, string.Format("Do OCR Need Parameter ALIGN_ANGLE"));


                        }*/
                    }

                    if (Use_Align || Use_OCR) {
                        //要讀OCR 就必須要給Align，如果不需要額外轉角度，就給-1
                        PJobject.Use_Align = true;
                    }
                    PJobject.Use_OCR = Use_OCR;
                    PJobject.OCR_Recipe = OCR_Recipe;
                    PJobject.Align_Angle = 0; //EFEM 固定0
                    PJobject.Use_Align = true;//EFEM 固定要過



                    PJlistTemp.Add(PJID, PJobject);
                    PjIDlist.Add(PJID);

                }
                if (PJListCount == 0)
                    throw new StreamFuntionException(2, 5, "PJ List have Error");

                S_S16F16(secsMsg, nAck, ErrorParam, ErrorCode, PjIDlist, secsObject);

                foreach (string NewPJ in PJlistTemp.Keys)
                {
                    _jobcontrol.PJlist.Add(NewPJ, PJlistTemp[NewPJ]);
                    _jobcontrol.PJlist[NewPJ].OnProcessJobStatesChange += SGEM300_OnProcessJobStatesChange;

                    _jobcontrol.PJlist[NewPJ].Status = JobStatus.QUEUED;
                }
                PJlistTemp.Clear();

            }
            catch (StreamFuntionException ex)
            {
                PJlistTemp.Clear();
                S_S16F16(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, PjIDlist, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                PJlistTemp.Clear();
                S_S16F16(secsMsg, 2, "Other Error", 5, PjIDlist, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S16F16(object secsMsg, int Ack, string ErrorParam, int ErrorCode, List<string> list, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, list.Count);
                for (int i = 0; i < list.Count; i++)
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, list[i]);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                int Success = (Ack == 0) ? 255 : 0;
                _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, Success);
                if (Success == 255)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U2, ref secsMsg, ErrorCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);

                }
                _secsdriver.SendMessage(QsStream.S16, QsFunction.F16, secsMsg, secsObject);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        void R_S16F17(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string ErrorParam = string.Empty;
            int ErrorCode = 0;
            List<string> PJIDList = new List<string>();
            string PJID = string.Empty;
            try
            {
                if (_GEMControlstats != GEMControlStats.ONLINEREMOTE)
                    throw new StreamFuntionException(2, 13, "Current mode is not Online Remote, Command Fail");

                int ListCount = 0;

                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                ListCount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                if (ListCount < 1)
                    throw new StreamFuntionException(2, 5, "PJ List Count error .");
                for (int i = 0; i < ListCount; i++)
                {
                    _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                    PJID = (string)Value;
                    PJIDList.Add(PJID);
                    /*
                    if (!PJlist.ContainsKey(PJID))
                        throw new StreamFuntionException(2, 5, string.Format("PJID={0} is not find .", PJID));
                    if (PJlist[PJID].Status != JobStatus.QUEUED)
                        throw new StreamFuntionException(2, 5, string.Format("PJID={0} state is not queue  ,  not delete .", PJID));
                    PJlist.Remove(PJID);
                    */

                }
                S_S16F18(secsMsg, nAck, ErrorParam, ErrorCode, PJIDList, secsObject);
                /*
                if (OnProcessJobUpdate != null)
                    OnProcessJobUpdate(this, new EventArgs()); // update VIEW
                    */
            }
            catch (StreamFuntionException ex)
            {
                S_S16F18(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, PJIDList, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S16F18(secsMsg, 2, "Other Error", 5, PJIDList, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S16F18(object secsMsg, int Ack, string ErrorParam, int ErrorCode, List<string> List, SecsMessageObject1Args secsObject)
        {
            try
            {
                for (int i = 0; i < List.Count; i++)
                {
                    if (!_jobcontrol.PJlist.ContainsKey(List[i]))
                    {
                        ErrorParam = string.Format("S16F17 - Process Job-{0} Dequeue Request Failed Due to the Specificated Process Job Does Not Exist!! .", List[i]);
                        Ack = 1;
                        ErrorCode = 1;
                        break;
                    }

                    if (_jobcontrol.PJlist[List[i]].Status != JobStatus.QUEUED)
                    {
                        ErrorParam = string.Format("PJID={0} state is not queue  ,  not delete .", List[i]);
                        Ack = 1;
                        ErrorCode = 1;
                        break;
                    }
                }
                if (Ack == 0)
                {
                    foreach (string PJName in List)
                    {
                        _jobcontrol.PJlist[PJName].Status = JobStatus.Destroy;
                        _jobcontrol.PJlist.Remove(PJName);
                    }
                    this.PJRemove();
                }
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, List.Count);
                for (int i = 0; i < List.Count; i++)
                {
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, List[i]);
                }
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                int Acks = (Ack == 0) ? 255 : 0;
                _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, Acks);
                if (Ack == 0)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 1);
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, ErrorCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);
                }
                _secsdriver.SendMessage(QsStream.S16, QsFunction.F18, secsMsg, secsObject);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        void R_S16F19(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                S_S16F20(secsMsg, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S16F20(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, _jobcontrol.PJlist.Count);
                foreach (SProcessJobObject PJObject in _jobcontrol.PJlist.Values)
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, PJObject.ID);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, (int)PJObject.Status);
                }

                _secsdriver.SendMessage(QsStream.S16, QsFunction.F20, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        void R_S16F21(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                S_S16F22(secsMsg, secsObject);
            }
            catch (StreamFuntionException ex)
            {
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S16F22(object secsMsg, SecsMessageObject1Args secsObject)
        {
            try
            {
                int PJCount = 100 - _jobcontrol.PJlist.Count();
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, PJCount);

                _secsdriver.SendMessage(QsStream.S16, QsFunction.F22, secsMsg, secsObject);


            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        void R_S16F27(object secsMsg, SecsMessageObject1Args secsObject)
        {
            int nAck = 0;
            string ErrorParam = string.Empty;
            int ErrorCode = 0;
            int Listcount = 0;
            string CJName = string.Empty;
            CTLJOBCMD CJCMD;
            List<string> waferIDList = new List<string>();
            List<string> PJList = new List<string>();
            try
            {
                if (_GEMControlstats != GEMControlStats.ONLINEREMOTE)
                    throw new StreamFuntionException(2, 13, "Current mode is not Online Remote, Command Fail");

                object Value = new object();
                _secsdriver.DataIteminitin(ref secsMsg, secsObject);
                _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                CJName = (string)Value;
                _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                CJCMD = (CTLJOBCMD)Value;
                Listcount = _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                if (!_jobcontrol.CJlist.ContainsKey(CJName))
                    throw new StreamFuntionException(1, 1, "Not find CJ Name");

                switch (CJCMD)
                {
                    case CTLJOBCMD.CJStart:


                        if (_jobcontrol.CJlist[CJName].Status != JobStatus.WaitFotHost && _jobcontrol.CJlist[CJName].Status != JobStatus.QUEUED)
                            throw new StreamFuntionException(1, 1, string.Format("CJ ={0} Status is not Wait for host", CJName));

                        S_S16F28(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);
                        _jobcontrol.CJlist[CJName].Status = JobStatus.QUEUED;
                        _jobcontrol.CJlist[CJName].AutoStart = true;


                        break;

                    case CTLJOBCMD.CJCanecl:
                        if (_jobcontrol.CJlist[CJName].Status != JobStatus.QUEUED && _jobcontrol.CJlist[CJName].Status != JobStatus.WaitFotHost)
                            throw new StreamFuntionException(1, 1, string.Format("CJ ={0} Status is not QUEUED or WaitFotHost", CJName));

                        S_S16F28(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);


                        //  Listcount= _secsdriver.DataItemIn(SecsFormateType.L, ref secsMsg, ref Value);
                        if (Listcount != 0)
                        {
                            _secsdriver.DataItemIn(SecsFormateType.A, ref secsMsg, ref Value);
                            _secsdriver.DataItemIn(SecsFormateType.U1, ref secsMsg, ref Value);
                            if ((int)Value == 1)
                            {
                                foreach (SProcessJobObject Pj in _jobcontrol.CJlist[CJName].PJList.Values)
                                {
                                    Pj.Status = JobStatus.Destroy;
                                    PJList.Add(Pj.ID);
                                }
                                foreach (string PJID in PJList)
                                {
                                    _jobcontrol.PJlist.Remove(PJID);
                                }
                                this.PJRemove();
                            }
                        }
                        _jobcontrol.CJlist[CJName].Status = JobStatus.ABORTING;
                        _jobcontrol.CJlist[CJName].Status = JobStatus.COMPLETED;
                        _jobcontrol.CJlist[CJName].Status = JobStatus.Destroy;
                        _jobcontrol.CJlist.Remove(CJName);
                        this.CJRemove();
                        break;
                    case CTLJOBCMD.CJAbort:
                    case CTLJOBCMD.CJStop:
                        if (_jobcontrol.CJlist[CJName].Status != JobStatus.EXECUTING)
                            throw new StreamFuntionException(1, 1, string.Format("CJ ={0} Status is not EXECUTING", CJName));

                        _autoProcess.PrepareForEnd(); // Stop job and Undo

                        S_S16F28(secsMsg, nAck, ErrorParam, ErrorCode, secsObject);

                        break;
                }

            }
            catch (StreamFuntionException ex)
            {
                S_S16F28(secsMsg, ex.nAck, ex.Message, ex.nErrorCode, secsObject);
                WriteLog("[StreamFuntionException] " + ex);
            }
            catch (Exception ex)
            {
                S_S16F28(secsMsg, 1, "Other Error", 1, secsObject);
                WriteLog("[Exception] " + ex);
            }
        }
        void S_S16F28(object secsMsg, int Ack, string ErrorParam, int ErrorCode, SecsMessageObject1Args secsObject)
        {
            try
            {
                _secsdriver.DataIteminitoutResponse(ref secsMsg, secsObject);
                _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                int Nack = (Ack == 0) ? 255 : 0;
                _secsdriver.DataItemOut(SecsFormateType.Bool, ref secsMsg, Nack);
                if (Ack == 0)
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 0);
                else
                {
                    _secsdriver.DataItemOut(SecsFormateType.L, ref secsMsg, null, 2);
                    _secsdriver.DataItemOut(SecsFormateType.U1, ref secsMsg, ErrorCode);
                    _secsdriver.DataItemOut(SecsFormateType.A, ref secsMsg, ErrorParam);
                }

                _secsdriver.SendMessage(QsStream.S16, QsFunction.F28, secsMsg, secsObject);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }



        void AssignVIDValue(ref object Msg, int VID)
        {
            // check VID object
            VIDObject VIDObj = null;
            if (_VIDcontrol.SVIDList.ContainsKey(VID))
            {
                VIDObj = _VIDcontrol.SVIDList[VID];
            }
            else if (_VIDcontrol.FDCList.ContainsKey(VID))
            {
                VIDObj = _VIDcontrol.FDCList[VID];
            }
            else
            {
                var Ividobject = from VIDs in _VIDcontrol.DVIDList
                                 where _VIDcontrol.DVIDList[VIDs.Key].VID == VID
                                 select _VIDcontrol.DVIDList[VIDs.Key];
                VIDObj = Ividobject.ElementAtOrDefault(0);
            }
            // Secs Check DVID OR SVID
            if (VIDObj == null)
            {
                _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, 0);
                return;
            }

            if (VIDObj._type == VIDType.DVID)
                AssignDVIDValue(ref Msg, VIDObj);
            else if (VIDObj._type == VIDType.SVID)
            {

                AssignSVIDValue(ref Msg, VIDObj);
            }
            else
                WriteLog("Not Find");
        }
        void AssignDVIDValue(ref object Msg, VIDObject VID)
        {
            if (VID.ValueType == SecsFormateType.L)
            {
                // Defind by your self 
                switch (VID.VIDName.ToUpper())
                {
                    case "CARRIERSLOTMAP":
                        if (VID.CurrentValue == null)
                            VID.CurrentValue = "9999999999999999999999999"; // Empty
                        _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, VID.CurrentValue.ToString().Length);
                        for (int i = 0; i < VID.CurrentValue.ToString().Length; i++)
                        {
                            int values = (VID.CurrentValue.ToString()[i] == '1') ? 3 : (VID.CurrentValue.ToString()[i] == '0') ? 1 : (VID.CurrentValue.ToString()[i] == '9') ? 0 : 2;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, values);
                        }
                        return;
                    case "CURRENTPRJOB":
                        _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, _jobcontrol.PJlist.Count);
                        foreach (string PJName in _jobcontrol.PJlist.Keys)
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, PJName);

                        return;

                    case "GemTime":
                        _secsdriver.DataItemOut(VID.ValueType, ref Msg, DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                        return;
                    case "QueuedCJobs":
                        _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, _jobcontrol.CJlist.Where(x => x.Value.Status == JobStatus.QUEUED).Count());
                        foreach (SControlJobObject CJ in _jobcontrol.CJlist.Values)
                        {
                            if (CJ.Status == JobStatus.QUEUED)
                                _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, CJ.ID);

                        }
                        return;
                    case "STSSUBDATA":
                        //Wafer TempWafer;
                        //if (_SubWaferData.TryDequeue(out TempWafer))
                        {
                            WriteLog(string.Format(" STSSUBDATA  Assing VID , Wafer ID = {0}, PJ = {1},CJ ={2},Lot ={3}", TempWafer.WaferID_F + '/' + TempWafer.WaferID_B, TempWafer.PJID, TempWafer.CJID, TempWafer.LotID));
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 7);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.WaferID_F + '/' + TempWafer.WaferID_B);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.FoupID);
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, TempWafer.Slot);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.LotID);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.RecipeID);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.CJID);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.PJID);


                        }
                        //else
                        //{
                        //    _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 0);
                        //}
                        return;
                    case "STSSORUCEDATA":
                        _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 3);
                        _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, TempPortforSTS);
                        _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempCarrierforSTS);
                        _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, TempWaferListforSTS.Count);
                        for (int i = 0; i < TempWaferListforSTS.Count; i++)
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWaferListforSTS[i]);

                        return;
                    case "INSPECTDATA":
                     
                     {
                            if (InspectWafer != null)
                            {
                                _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 5);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, InspectWafer.FoupID);
                                _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, InspectWafer.Slot);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, InspectWafer.LotID);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, InspectWafer.WaferID_B);
                                _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, InspectWafer.GetInspectData().Count);
                                foreach (var item in TempWafer.GetInspectData()) {
                                    _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 6);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, item.Data_1);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, item.Data_2);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, item.Data_3);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, item.Data_4);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, item.Data_5);
                                    _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, item.Data_6);

                                }



                            }
                            else {
                                _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 0);


                            }


                                return;
                     }

                    //v1.000 Jacky Hsiung Add Start
                    case "MEASDATA":
                        if (TempWafer != null)
                        {
                            //v1.002                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 5);
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 6); //v1.002 Jacky Hsiung Add
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.LotID);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.WaferID_B); //v1.002 Jacky Hsiung Add
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.RecipeID);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.CJID);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.PJID);
                            //_secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, TempWafer.GetMeasureData().Count);
                            //foreach (var OnePoint in TempWafer.GetMeasureData())
                            //{
                            //    _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 2);
                            //    _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, OnePoint.Key);
                            //    _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, OnePoint.Value.Count);
                            //    foreach (var OneID in OnePoint.Value)
                            //    {
                            //        string[] ItemArrary = OneID.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            //        _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, ItemArrary.Length - 1);//Don't use first ItemValue it's foldername
                            //        for (int i = 1; i < ItemArrary.Length; i++)
                            //        {
                            //            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 2);
                            //            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, ItemArrary[i].ToString().Split(':')[0]);
                            //            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, ItemArrary[i].ToString().Split(':')[1]);
                            //        }
                            //    }
                            //}
                        }
                        return;
                    //v1.000 Jacky Hsiung Add End
                    //v1.002 Jacky Hsiung Add Start
                    case "LOTINFO":
                        if (TempWafer != null)
                        {
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 6);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.LotID);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.WaferID_F);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.FoupID);
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, TempWafer.Slot);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, TempWafer.ToFoupID);
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, TempWafer.ToSlot);
                        }
                        return;
                    //v1.002 Jacky Hsiung Add Start

                    default:
                        _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 0);
                        return;
                }
            }
            else
            {
                if (VID.CurrentValue == null)
                {
                    if (VID.ValueType == SecsFormateType.A)
                        VID.CurrentValue = " ";
                    else
                        VID.CurrentValue = 0;
                }

                _secsdriver.DataItemOut(VID.ValueType, ref Msg, VID.CurrentValue);
            }
        }
        void AssignSVIDValue(ref object Msg, VIDObject VID)
        {
            object value = new object();
            if (_VIDcontrol.FDCList.ContainsKey(VID.VID)) // Check VID is FDC 
            {
                _secsdriver.DataItemOut(VID.ValueType, ref Msg, _VIDcontrol.FDCList[VID.VID].CurrentValue);
                return;
            }

            switch (VID.VIDName)   //Check VID is Gem Status;
            {
                case "GemControlState":
                    value = (int)_GEMControlstats;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "GemPreviousControlState":
                    value = (int)_preGEMControlstats;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "GemPreviousProcessState":
                    value = (int)_preGEMProcessStats;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;

                case "GemProcessState":
                    value = (int)_GEMProcessStats;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;


                case "GemClock":
                    value = DateTime.Now.ToString("yyyyMMddHHmmssff");
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;


                case "CtrlMaxJobSpace":
                    value = 4;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "QueueAvailableSpace":
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, 100 - _jobcontrol.PJlist.Count);
                    return;
                case "CtrlJobQueueAvailableSpace":
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, 30 - _jobcontrol.CJlist.Count);
                    return;

                case "RFIDA":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                        value = SecsGEMUtilty.LoadPortList[1].FoupID;
                    else
                        value = "";
                    if ((string)value == "")
                        value = " ";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "RFIDB":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                        value = SecsGEMUtilty.LoadPortList[2].FoupID;
                    else
                        value = "";
                    if ((string)value == "")
                        value = " ";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "RFIDC":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                        value = SecsGEMUtilty.LoadPortList[3].FoupID;
                    else
                        value = "";
                    if ((string)value == "")
                        value = " ";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "RFIDD":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                        value = SecsGEMUtilty.LoadPortList[4].FoupID;
                    else
                        value = "";
                    if ((string)value == "")
                        value = " ";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAccessMode1":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                        value = (SecsGEMUtilty.LoadPortList[1].E84Object.GetAutoMode) ? 1 : 0;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAccessMode2":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                        value = (SecsGEMUtilty.LoadPortList[2].E84Object.GetAutoMode) ? 1 : 0;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAccessMode3":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                        value = (SecsGEMUtilty.LoadPortList[3].E84Object.GetAutoMode) ? 1 : 0;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAccessMode4":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                        value = (SecsGEMUtilty.LoadPortList[4].E84Object.GetAutoMode) ? 1 : 0;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortTransferState1":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                        value = (int)SecsGEMUtilty.LoadPortList[1].E84Status;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortTransferState2":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                        value = (int)SecsGEMUtilty.LoadPortList[2].E84Status;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortTransferState3":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                        value = (int)SecsGEMUtilty.LoadPortList[3].E84Status;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortTransferState4":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                        value = (int)SecsGEMUtilty.LoadPortList[4].E84Status;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "StageAStatus":

                    if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                        value = (int)SecsGEMUtilty.LoadPortList[1].StatusMachine;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);

                    return;
                case "StageBStatus":

                    if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                        value = (int)SecsGEMUtilty.LoadPortList[2].StatusMachine;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);

                    return;
                case "StageCStatus":

                    if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                        value = (int)SecsGEMUtilty.LoadPortList[3].StatusMachine;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);

                    return;
                case "StageDStatus":

                    if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                        value = (int)SecsGEMUtilty.LoadPortList[4].StatusMachine;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);

                    return;
                case "GemAlarmState":
                    if (SecsGEMUtilty.Alarm.IsAlarm() == true)
                        value = 1;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "LocationID1":
                    value = "LP1";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "LocationID2":
                    value = "LP2";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "LocationID3":
                    value = "LP3";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "LocationID4":
                    value = "LP4";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAssociationState1":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                        value = (int)(SecsGEMUtilty.LoadPortList[1].CarrierAccessStats);
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAssociationState2":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                        value = (int)(SecsGEMUtilty.LoadPortList[2].CarrierAccessStats);
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAssociationState3":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                        value = (int)(SecsGEMUtilty.LoadPortList[3].CarrierAccessStats);
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAssociationState4":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                        value = (int)(SecsGEMUtilty.LoadPortList[4].CarrierAccessStats);
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PRMaxJobSpace":

                    value = 30;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortID1":
                    value = 1;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortID2":
                    value = 2;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortID3":
                    value = 3;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortID4":
                    value = 4;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortAStatus":
                case "PodAStatus":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                        value = (SecsGEMUtilty.LoadPortList[1].FoupExist) ? 1 : 0;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortBStatus":
                case "PodBStatus":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                        value = (SecsGEMUtilty.LoadPortList[2].FoupExist) ? 1 : 0;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortCStatus":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                        value = (SecsGEMUtilty.LoadPortList[3].FoupExist) ? 1 : 0;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;
                case "PortDStatus":
                    if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                        value = (SecsGEMUtilty.LoadPortList[4].FoupExist) ? 1 : 0;
                    else
                        value = 0;
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    return;



            }

            switch (VID.ValueType)
            {
                case SecsFormateType.A:
                    // _EQ.GetSVIDValue(VID.VIDName, ref value);
                    if (value == null)
                        value = " ";
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    break;
                case SecsFormateType.L:
                    switch (VID.VIDName)
                    {

                        case "QueuedCJobs":
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, _jobcontrol.CJlist.Count);




                            foreach (string CJID in _jobcontrol.CJlist.Keys)
                            {
                                _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 2);
                                _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, CJID);
                                _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, (int)_jobcontrol.CJlist[CJID].Status);
                            }

                            return;
                        case "PortTransferStateList":
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 4);

                            if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                                value = (int)SecsGEMUtilty.LoadPortList[1].E84Status;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);

                            if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                                value = (int)SecsGEMUtilty.LoadPortList[2].E84Status;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);
                            if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                                value = (int)SecsGEMUtilty.LoadPortList[3].E84Status;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);
                            if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                                value = (int)SecsGEMUtilty.LoadPortList[4].E84Status;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);


                            return;
                        case "CarrierIDList":

                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 4);

                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 3);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, "A");
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, 1);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, SecsGEMUtilty.LoadPortList[1].FoupID);

                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 3);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, "B");
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, 2);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, SecsGEMUtilty.LoadPortList[2].FoupID);

                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 3);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, "C");
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, 3);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, SecsGEMUtilty.LoadPortList[3].FoupID);

                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 3);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, "D");
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, 4);
                            _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, SecsGEMUtilty.LoadPortList[4].FoupID);


                            return;
                        case "PortAccessModeList":
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 4);



                            if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                                value = (SecsGEMUtilty.LoadPortList[1].E84Object.GetAutoMode) ? 1 : 0;
                            else
                                value = 0;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);

                            if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                                value = (SecsGEMUtilty.LoadPortList[2].E84Object.GetAutoMode) ? 1 : 0;
                            else
                                value = 0;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);

                            if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                                value = (SecsGEMUtilty.LoadPortList[3].E84Object.GetAutoMode) ? 1 : 0;
                            else
                                value = 0;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);

                            if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                                value = (SecsGEMUtilty.LoadPortList[4].E84Object.GetAutoMode) ? 1 : 0;
                            else
                                value = 0;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);


                            return;

                        case "PortAssociationStateList":
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 4);



                            if (SecsGEMUtilty.LoadPortList.ContainsKey(1))
                                value = (int)(SecsGEMUtilty.LoadPortList[1].CarrierAccessStats);
                            else
                                value = 0;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);

                            if (SecsGEMUtilty.LoadPortList.ContainsKey(2))
                                value = (int)(SecsGEMUtilty.LoadPortList[2].CarrierAccessStats);
                            else
                                value = 0;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);

                            if (SecsGEMUtilty.LoadPortList.ContainsKey(3))
                                value = (int)(SecsGEMUtilty.LoadPortList[3].CarrierAccessStats);
                            else
                                value = 0;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);

                            if (SecsGEMUtilty.LoadPortList.ContainsKey(4))
                                value = (int)(SecsGEMUtilty.LoadPortList[4].CarrierAccessStats);
                            else
                                value = 0;
                            _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);


                            return;

                        case "PortStateInfoList":
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 4);

                            for (int i = 0; i < 4; i++)
                            {
                                _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 2);
                                if (SecsGEMUtilty.LoadPortList.ContainsKey(i + 1))
                                    value = (int)(SecsGEMUtilty.LoadPortList[i + 1].CarrierAccessStats);
                                else
                                    value = 0;
                                _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);

                                if (SecsGEMUtilty.LoadPortList.ContainsKey(i + 1))
                                    value = (int)(SecsGEMUtilty.LoadPortList[i + 1].E84Status);
                                else
                                    value = 0;
                                _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);


                            }



                            return;
                        case "PortIDList":

                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 4);

                            for (int i = 0; i < 4; i++)
                            {
                                value = i + 1;
                                _secsdriver.DataItemOut(SecsFormateType.U1, ref Msg, value);
                            }

                            return;
                      


                        default:
                            _secsdriver.DataItemOut(SecsFormateType.L, ref Msg, null, 0);
                            break;

                    }
                    break;
                default:
                    // _EQ.GetSVIDValue(VID.VIDName, ref value);
                    _secsdriver.DataItemOut(VID.ValueType, ref Msg, value);
                    break;
            }

        }
        void AssignECIDValue(ref object Msg, int ECID)
        {
            int TempValue = 0;

            if (!_VIDcontrol.ECIDList.ContainsKey(ECID))
                _secsdriver.DataItemOut(SecsFormateType.A, ref Msg, "");

            switch (_VIDcontrol.ECIDList[ECID].ValueType)
            {
                case SecsFormateType.A:
                    _secsdriver.DataItemOut(_VIDcontrol.ECIDList[ECID].ValueType, ref Msg, (string)_VIDcontrol.ECIDList[ECID].CurrentValue);
                    break;

                case SecsFormateType.U1:
                case SecsFormateType.U2:
                case SecsFormateType.U4:

                    int.TryParse((string)_VIDcontrol.ECIDList[ECID].CurrentValue, out TempValue);

                    _secsdriver.DataItemOut(_VIDcontrol.ECIDList[ECID].ValueType, ref Msg, TempValue);
                    break;
                case SecsFormateType.Bool:
                    TempValue = (((string)_VIDcontrol.ECIDList[ECID].CurrentValue).ToUpper() == "255") ? 255 : 0;
                    _secsdriver.DataItemOut(_VIDcontrol.ECIDList[ECID].ValueType, ref Msg, TempValue);
                    break;


            }
            //   _secsdriver.DataItemOut(_VIDcontrol.ECIDList[ECID].ValueType, ref Msg, _VIDcontrol.ECIDList[ECID].CurrentValue);

        }
        void UpdateECIDValue(int ECID, object Value)
        {
            if (_VIDcontrol.ECIDList.ContainsKey(ECID))
            {
                _VIDcontrol.ECIDList[ECID].CurrentValue = (string)Value.ToString();

                _DB.SQLExec("Update ECIDList Set ECValue ={1} where ECID={0}", ECID, Value);

            }

        }

        public void CheckNextPJ(string CJID)
        {
            if (OnFunctionSetup != null)
                OnFunctionSetup(this, new FunctionSetupEventArgs(CJID, _jobcontrol.CJlist[CJID].PJList.Values.ToList(), _jobcontrol.CJlist[CJID]));

        }

        // Job Event
        private void SGEM300_OnProcessJobStatesChange(object sender, SProcessJobObject.ProcessJobStatesEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();



            //_VIDcontrol.DVIDList["PRJobId"].CurrentValue = e.PJID;
            //_VIDcontrol.DVIDList["PRJobState"].CurrentValue = (int)e.Status;

            DVID_List.Add(new DVID_Obj("PRJobId", e.PJID));

            DVID_List.Add(new DVID_Obj("PRJobState", (int)e.Status));



            switch (e.PreStatus)
            {
                case JobStatus.Non:
                    switch (e.Status)
                    {
                        case JobStatus.QUEUED:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans01"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans01"));
                            break;
                    }
                    break;
                case JobStatus.QUEUED:
                    switch (e.Status)
                    {
                        case JobStatus.FunctionSetup:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans02"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans02"));
                            break;
                        case JobStatus.Destroy:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans18"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans18"));
                            break;
                    }
                    break;
                case JobStatus.FunctionSetup:
                    switch (e.Status)
                    {
                        case JobStatus.EXECUTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans04"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans04"));
                            break;
                        case JobStatus.WaitFotHost:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans03"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans03"));
                            break;

                    }
                    break;
                case JobStatus.WaitFotHost:
                    switch (e.Status)
                    {

                        case JobStatus.EXECUTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans05"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans05"));
                            break;
                    }
                    break;
                case JobStatus.EXECUTING:
                    switch (e.Status)
                    {
                        case JobStatus.COMPLETED:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans06"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans06"));
                            break;
                        case JobStatus.PAUSING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans08"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans08"));
                            break;
                        case JobStatus.STOPPING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans11"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans11"));
                            break;
                        case JobStatus.ABORTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans13"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans13"));
                            break;
                    }
                    break;
                case JobStatus.COMPLETED:
                    switch (e.Status)
                    {
                        case JobStatus.Destroy:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans07"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans07"));
                            break;
                    }
                    break;
                case JobStatus.PAUSING:
                    switch (e.Status)
                    {
                        case JobStatus.PAUSED:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans09"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans09"));
                            break;
                        case JobStatus.ABORTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans14"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans14"));
                            break;

                    }
                    break;
                case JobStatus.PAUSED:
                    switch (e.Status)
                    {
                        case JobStatus.EXECUTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans10"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans10"));
                            break;
                        case JobStatus.STOPPING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans12"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans12"));
                            break;
                        case JobStatus.ABORTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans15"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans15"));
                            break;
                    }
                    break;
                case JobStatus.ABORTING:
                    switch (e.Status)
                    {

                        case JobStatus.Destroy:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans16"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans16"));
                            break;
                    }
                    break;
                case JobStatus.STOPPING:
                    switch (e.Status)
                    {

                        case JobStatus.Destroy:
                            //S_S6F11(_CEIDcontrol.CEIDList["PRJobSMTrans17"]);
                            CEID_List.Add(new CEID_Obj("PRJobSMTrans17"));
                            break;
                    }
                    break;
            }

            SetupDVID_EVENT(CEID_List, DVID_List);

            if (OnProcessJobUpdate != null)
                OnProcessJobUpdate(this, new EventArgs());

        }
        private void SGEM300_OnControlJobStatesChange(object sender, SControlJobObject.ControlJobStatesEventArgs e)
        {
            List<DVID_Obj> DVID_List = new List<DVID_Obj>();
            List<CEID_Obj> CEID_List = new List<CEID_Obj>();


            //_VIDcontrol.DVIDList["CtrlJobID"].CurrentValue = e.CJID;
            //_VIDcontrol.DVIDList["CtrlJobState"].CurrentValue = (int)e.Status;

            DVID_List.Add(new DVID_Obj("CtrlJobID", e.CJID));
            DVID_List.Add(new DVID_Obj("CtrlJobState", (int)e.Status));



            switch (e.PreStatus)
            {
                case JobStatus.Non:
                    switch (e.Status)
                    {
                        case JobStatus.QUEUED:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans01"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans01"));
                            break;
                    }
                    break;
                case JobStatus.QUEUED:
                    switch (e.Status)
                    {
                        case JobStatus.Destroy:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans02"]); // Host S16F27 CJAbort, CJCancel, or CjStop 
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans02"));
                            break;
                        case JobStatus.Select:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans03"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans03"));
                            break;

                    }
                    break;
                case JobStatus.Select:
                    switch (e.Status)
                    {
                        case JobStatus.QUEUED:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans04"]); //Select to Queue
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans04"));
                            break;
                        case JobStatus.EXECUTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans05"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans05"));

                            // S_S6F11(_CEIDcontrol.CEIDList["ProcessStart"]);
                            break;
                        case JobStatus.WaitFotHost:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans06"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans06"));
                            break;
                    }
                    break;
                case JobStatus.WaitFotHost:
                    switch (e.Status)
                    {
                        case JobStatus.EXECUTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans07"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans07"));
                            break;
                    }
                    break;
                case JobStatus.EXECUTING:
                    switch (e.Status)
                    {
                        case JobStatus.PAUSED:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans08"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans08"));
                            break;
                        case JobStatus.COMPLETED:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans10"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans10"));

                            //S_S6F11(_CEIDcontrol.CEIDList["ProcessEnd"]);
                            break;
                    }
                    break;
                case JobStatus.PAUSED:
                    switch (e.Status)
                    {
                        case JobStatus.EXECUTING:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans09"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans09"));
                            break;
                    }
                    break;
                case JobStatus.STOPPING:
                    switch (e.Status)
                    {
                        case JobStatus.COMPLETED:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans11"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans11"));
                            break;
                    }
                    break;
                case JobStatus.ABORTING:
                    switch (e.Status)
                    {
                        case JobStatus.COMPLETED:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans12"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans12"));
                            break;
                    }
                    break;
                case JobStatus.COMPLETED:
                    switch (e.Status)
                    {
                        case JobStatus.Destroy:
                            //S_S6F11(_CEIDcontrol.CEIDList["CtrlJobSMTrans13"]);
                            CEID_List.Add(new CEID_Obj("CtrlJobSMTrans13"));
                            break;
                    }
                    break;

            }

            SetupDVID_EVENT(CEID_List, DVID_List);

            if (OnControlJobUpdate != null)
                OnControlJobUpdate(this, new EventArgs());
        }

        public void CJRemove()
        {
            if (OnControlJobUpdate != null)
                OnControlJobUpdate(this, new EventArgs());
        }

        public void PJRemove()
        {
            if (OnProcessJobUpdate != null)
                OnProcessJobUpdate(this, new EventArgs());
        }

        public void MaunalProcessRegistPJ(string PJName)
        {
            _jobcontrol.PJlist[PJName].OnProcessJobStatesChange += SGEM300_OnProcessJobStatesChange;
        }
        public void MaunalProcessRegistCJ(string CJName)
        {
            _jobcontrol.CJlist[CJName].OnControlJobStatesChange += SGEM300_OnControlJobStatesChange;
        }

        public DataSet PJListForDataSet()
        {
            DataSet DataList = new DataSet("PJ Space");
            try
            {
                System.Data.DataRow dr;
                DataList.Tables.Add("PJ List");
                DataList.Tables["PJ List"].Columns.Add("PJ Name");

                DataList.Tables["PJ List"].Columns.Add("PJ Status");
                DataList.Tables["PJ List"].Columns.Add("Recipe");

                dr = DataList.Tables["PJ List"].NewRow();
                foreach (string PJID in _jobcontrol.PJlist.Keys)
                {
                    dr["PJ Name"] = _jobcontrol.PJlist[PJID].ID;
                    dr["PJ Status"] = _jobcontrol.PJlist[PJID].Status.ToString();
                    dr["Recipe"] = _jobcontrol.PJlist[PJID].RecipeName;

                    DataList.Tables["PJ List"].Rows.Add(dr);
                    dr = DataList.Tables["PJ List"].NewRow();
                }

                return DataList;
            }

            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return DataList;
            }
        }
        public DataSet CJListForDataSet()
        {
            DataSet DataList = new DataSet("CJ Space");
            try
            {

                System.Data.DataRow dr;
                DataList.Tables.Add("CJ List");
                DataList.Tables["CJ List"].Columns.Add("CJ Name");
                DataList.Tables["CJ List"].Columns.Add("CJ Status");
                DataList.Tables["CJ List"].Columns.Add("Excute PJID");
                dr = DataList.Tables["CJ List"].NewRow();
                foreach (string CJID in _jobcontrol.CJlist.Keys)
                {
                    dr["CJ Name"] = CJID;
                    dr["CJ Status"] = _jobcontrol.CJlist[CJID].Status.ToString();



                    foreach (int PJNO in _jobcontrol.CJlist[CJID].PJList.Keys)
                    {
                        if (_jobcontrol.CJlist[CJID].PJList.Count > 1)
                            dr["Excute PJID"] += _jobcontrol.CJlist[CJID].PJList[PJNO].ID + "/";
                        else
                            dr["Excute PJID"] += _jobcontrol.CJlist[CJID].PJList[PJNO].ID;
                    }



                    DataList.Tables["CJ List"].Rows.Add(dr);
                    dr = DataList.Tables["CJ List"].NewRow();
                }

                return DataList;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return DataList;
            }
        }



        private bool SetupDVID_EVENT(List<CEID_Obj> _CeidObj, List<DVID_Obj> _DvidObj)
        {
            //***前一層如果有用其他Lock，都不要包到SetupDVID_EVENT，避免發生卡死***
            //S_S6F11，統一由SetupDVID_EVENT呼叫
            int i;
            bool ret = true;
            lock (objectSetupSECSData)
            {
                try
                {
                    if (_DvidObj != null)
                    {
                        for (i = 0; i < _DvidObj.Count; i++)
                        {

                            if (_VIDcontrol.DVIDList.ContainsKey(_DvidObj[i].Name))
                            {
                                _VIDcontrol.DVIDList[_DvidObj[i].Name].CurrentValue = _DvidObj[i].Value;
                            }
                            else
                            {
                                WriteLog("Error - DVID({0}) Not Find In DVIDList", _DvidObj[i].Name);
                                ret = false;
                            }
                        }

                        _DvidObj.Clear();

                    }

                    if (_CeidObj != null)
                    {
                        for (i = 0; i < _CeidObj.Count; i++)
                        {
                            if (_CEIDcontrol.CEIDList.ContainsKey(_CeidObj[i].Name))
                            {
                                S_S6F11(_CEIDcontrol.CEIDList[_CeidObj[i].Name]);
                            }
                            else
                            {
                                WriteLog("Error - Event({0}) Not Find In CEIDList", _CeidObj[i].Name);
                                ret = false;
                            }
                        }
                        _CeidObj.Clear();
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                    ret = false;

                }

                return ret;
            }

        }


    }

    public class Attribute
    {
        public string ATTRID;
        public int ATTRDATA;
        public int ATTRRELN;

        public Attribute(string ID, int Data, int Reln)
        {
            ATTRID = ID;
            ATTRDATA = Data;
            ATTRRELN = Reln;
        }
    }

    public class HostWaferInfo
    {
        public string LotID;
        public string WaferID;
        public string CarrireID;
        public string PJID;
        public int SlotID;
        public string GradeName;
        public HostWaferInfo(string Lot, string Wafer, string carrier, string PJ, int slot)
        {
            LotID = Lot;
            WaferID = Wafer;
            CarrireID = carrier;
            PJID = PJ;
            SlotID = slot;
            GradeName = "";
        }
    }

    public class ExecuteJob
    {
        public int SourcePort;
        public int targetPort;

        public ExecuteJob()
        {

        }



    }


    public class ExecuteWaferOutInfo
    {
        public string WaferID;
        public string SourceTower;
        public int TargetSlot;
        public string TargetID;

        public ExecuteWaferOutInfo()
        {

        }
    }







}
