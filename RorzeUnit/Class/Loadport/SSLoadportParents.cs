using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeComm;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using RorzeApi.SECSGEM;
using static RorzeUnit.Class.SWafer;
using RorzeApi;

namespace RorzeUnit.Class.Loadport
{
    public abstract class SSLoadportParents : I_Loadport
    {
        
        #region =========================== private ============================================
        protected enumLoadPortMode m_eStatMode;       //記憶的STAT S1第1 bit
        protected bool m_bStatOrgnComplete;           //記憶的STAT S1第2 bit
        protected bool m_bStatProcessed;              //記憶的STAT S1第3 bit
        protected enumLoadPortStatus m_eStatInPos;    //記憶的STAT S1第4 bit
        protected int m_nSpeed;                       //記憶的STAT S1第5 bit
        protected string m_strErrCode = "0000";       //記憶的STAT S2


        protected SLogger _logger = SLogger.GetLogger("CommunicationLog");
        protected int m_nAckTimeout = 3000;
        protected int m_nMotionTimeout = 60000;
        protected int m_nWaferTotal = 25;// 由mapping完成時改變
        protected bool m_bMoving;//卡控用，自己ON由stat判斷OFF
        protected bool m_bRobotExtand = false;
        protected string m_strFoupID;
        protected string m_strFoupTypeName;
        protected string _MappingData = "";//2022.07.08
        protected int[] m_nTrbMapStgNo0to399;
        //  DPRM 
        protected string[][] _sDPRMData = new string[32][];//0~63
        //  DMPR
        protected string[] _sDMPRData;
        //  DCST
        protected string[] _sDCSTData;
        protected string[] _Rac2Data = { "00000000", "00000000", "00000000" };
        private List<bool> m_LPInfoPadEnableList;//ini設定啟用哪些info-pad
        private List<bool> m_TrbMapInfoEnableList;//ini設定啟用哪些info-pad可以mapping
        private List<SWafer> _jobschedule; // Kevin 建立哪幾片WAFER要執行
        private enumStateMachine _StatusMachine = enumStateMachine.PS_ReadyToLoad;
        private DateTime m_FoupArrivalTime = DateTime.Now;//紀錄foup到達時間
        private DateTime m_FoupDockedTime = DateTime.Now;//紀錄foup開門時間
        protected enumLoadPortPos _Yaxispos;
        protected enumLoadPortPos _Zaxispos;
        protected CarrierIDStats _CarrierIDstatus;
        protected CarrierSlotMapStats _SlotMappingStats;
        protected CarrierAccessStats _CarrierAccessStats;
        protected CarrierState _CarrierState;
        I_BarCode m_Barcode;
        #endregion
        #region =========================== property ===========================================
        public bool Simulate { get; protected set; }
        public bool Connected { get; protected set; }
        public int BodyNo { get; protected set; }
        public bool Disable { get; protected set; }

        public bool IsRobotExtend { get { return m_bRobotExtand; } }
        public bool SetRobotExtend { set { m_bRobotExtand = value; } }

        //STAT S1第1 bit
        public enumLoadPortMode StatMode { get { return m_eStatMode; } }
        public bool IsInitialized { get { return m_eStatMode == enumLoadPortMode.Remote; } }
        //STAT S1第2 bit
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }
        //STAT S1第3 bit
        public bool IsProcessing { get { return m_bStatProcessed; } }
        //STAT S1第4 bit
        public enumLoadPortStatus InPos { get { return m_eStatInPos; } }
        public bool IsMoving { get { return m_bMoving || m_eStatInPos == enumLoadPortStatus.Moving; } }
        //STAT S1第5 bit
        public int GetSpeed { get { return m_nSpeed; } }
        //STAT S2
        public bool IsError { get { return (m_strErrCode != "0000"); } }
        public int WaferTotal { get { return m_nWaferTotal; } }//Foup內部有幾層，並不是有幾片
        public abstract bool UseAdapter { get; }
        public abstract bool IsProtrude { get; }
        public abstract bool IsPresenceON { get; }
        public abstract bool IsPresenceleftON { get; }
        public abstract bool IsPresencerightON { get; }
        public abstract bool IsPresencemiddleON { get; }
        public abstract bool IsDoorOpen { get; }
        public abstract bool IsUnclamp { get; }//close 是勾住
        public bool IsPSPL_AllOn { get { return IsPresenceON && IsPresenceleftON && IsPresencerightON && IsPresencemiddleON; } }
        public bool IsPSPL_AllOf { get { return !IsPresenceON && !IsPresenceleftON && !IsPresencerightON && !IsPresencemiddleON; } }
        public enumFoupType eFoupType { get; protected set; }//對應到Inforoad 16 組
        public string FoupTypeName
        {
            get { return m_strFoupTypeName; }
            protected set
            {
                if (m_strFoupTypeName != value)
                {
                    m_strFoupTypeName = value;
                    if (OnFoupTypeChange != null)
                        OnFoupTypeChange(this, m_strFoupTypeName);
                }
            }
        }
        public string[][] GetDPRMData { get { return _sDPRMData; } }  //  DPRM      
        public string[] GetDMPRData { get { return _sDMPRData; } }  //  DMPR
        public string[] GetDCSTData { get { return _sDCSTData; } } //  DCST
        public enumWaferSize[] LoadportWaferType { get; protected set; }//ini設定infopad對應的晶圓種類
        public enumWaferSize GetCurrentLoadportWaferType()// 11111111111111110000000000000000
        {
            return UseAdapter ? (LoadportWaferType[(int)eFoupType + 16]) : (LoadportWaferType[(int)eFoupType]);
        }
        public enumStateMachine StatusMachine
        {
            get { return _StatusMachine; }
            set
            {
                if (_StatusMachine != value)
                {
                    switch (value)
                    {
                        case RorzeUnit.Class.Loadport.Enum.enumStateMachine.PS_FoupOn:
                            {
                                m_FoupArrivalTime = DateTime.Now;
                            }
                            break;
                        case RorzeUnit.Class.Loadport.Enum.enumStateMachine.PS_Docked:
                            {
                                m_FoupDockedTime = DateTime.Now;
                            }
                            break;
                    }
                    _StatusMachine = value;
                    if (OnStatusMachineChange != null)
                        OnStatusMachineChange(this, new OccurStateMachineChangEventArgs(_StatusMachine));

                }

            }
        }
        public enumLoadPortPos GetYaxispos { get { return _Yaxispos; } }
        public enumLoadPortPos GetZaxispos { get { return _Zaxispos; } }
        public CarrierIDStats CarrierIDstatus { get { return _CarrierIDstatus; } set { _CarrierIDstatus = value; } }
        public CarrierSlotMapStats SlotMappingStats { get { return _SlotMappingStats; } set { _SlotMappingStats = value; } }
        public CarrierAccessStats CarrierAccessStats { get { return _CarrierAccessStats; } set { _CarrierAccessStats = value; } }

        public CarrierState CarrierState { get { return _CarrierState; } set { _CarrierState = value; } }

        public bool FoupExist { get; set; }
        public string FoupID
        {
            get { return m_strFoupID; }
            set
            {
                m_strFoupID = value;
                if (OnFoupIDChange != null)
                    OnFoupIDChange(this, new EventArgs());
            }
        }
        public string CJID { get; set; }
        public string ExcutPJID { get; set; }
        public int FoupArrivalIdleTimeout { get; set; }
        public int FoupWaitTransferTimeout { get; set; }
        public bool UndockQueueByHost { get; set; }//docking過程客戶想要退掉，keep住
        public string[] GetRac2Data { get { return _Rac2Data; } }
        #endregion
        #region =========================== event ==============================================
        public event EventHandler<WaferDataEventArgs> OnAssignWaferData;
        public event SlotEventHandler OnWaferDataDelete;

        public abstract event FoupExistChangEventHandler OnFoupExistChenge;

        public abstract event EventHandler<bool> OnORGNComplete;
        public abstract event EventHandler<bool> OnGetDataComplete;
        public abstract event EventHandler<LoadPortEventArgs> OnJigDockComplete;
        public abstract event EventHandler<LoadPortEventArgs> OnClmpComplete;
        public abstract event EventHandler<LoadPortEventArgs> OnClmp1Complete;
        public abstract event EventHandler<LoadPortEventArgs> OnUclmComplete;
        public abstract event EventHandler<LoadPortEventArgs> OnUclm1Complete;
        public abstract event EventHandler<LoadPortEventArgs> OnMappingComplete;

        public event EventHandler OnProcessStart;
        public event EventHandler OnProcessEnd;
        public event EventHandler OnProcessAbort;

        public event AutoProcessingEventHandler DoManualProcessing;
        public event AutoProcessingEventHandler DoAutoProcessing;

        public abstract event RorzenumLoadportIOChangelHandler OnIOChange;

        public event OccurStateMachineChangEventHandler OnStatusMachineChange;

        public abstract event MessageEventHandler OnReadData;

        public event OccurErrorEventHandler OnOccurStatErr;
        public event OccurErrorEventHandler OnOccurCancel;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public event OccurErrorEventHandler OnOccurErrorRest;
        public event OccurErrorEventHandler OnOccurWarning;
        public event OccurErrorEventHandler OnOccurWarningRest;

        public event EventHandler OnReadyRunningLot;

        public event EventHandler OnFoupIDChange; //  更新UI

        public event EventHandler<string> OnFoupTypeChange;//  更新UI

        public event dlgv_n OnTakeWaferOutFoup;           //wafer從foup中被取出
        public event dlgv_n OnTakeWaferInFoup;            //wafer被放回foup

        // ================= Simulate =================
        public abstract event EventHandler OnSimulateCLMP;
        public abstract event EventHandler OnSimulateUCLM;
        public abstract event EventHandler OnSimulateMapping;
        #endregion 
        #region =========================== thread =============================================
        protected SInterruptOneThread _threadInit;        //初始化控制(private 流程, 問Status/機況同步)
        protected SInterruptOneThread _threadOrgn;        //原點復歸控制
        protected SInterruptOneThread _threadClamp;       //進貨控制(掃片)
        protected SInterruptOneThread _threadClamp1;      // Foup 底部鎖住 for SECS
        protected SInterruptOneThread _threadUClamp;      //出貨控制(掃片)
        protected SInterruptOneThread _threadUClamp1;     // Foup 底部解鎖 for SECS
        protected SInterruptOneThread _threadMapping;     //掃片單動控制  
        protected SInterruptOneThreadINT _threadReset;    //異常復歸控制
        protected SInterruptOneThread _threadGetData;
        protected SInterruptOneThread _threadFoupExist;
        protected SInterruptOneThread _threadJigDock;

        protected SPollingThread _pollingAuto;            //自動流程控管   
        protected SPollingThread _pollingMonitor;         //監測FOUP IDLE
        #endregion
        #region =========================== delegate ===========================================
        public dlgb_Object dlgLoadInterlock { get; set; }                // 不可以load
        public dlgb_Object dlgUnloadInterlock { get; set; }              // 不可以unload
        public dlgv_wafer AssignToRobotQueue { get; set; }  //丟給robot作排程
        #endregion
        #region =========================== wafer ==============================================
        public string MappingData
        {
            get
            {
                return _MappingData;
            }
            set
            {
                _MappingData = value;
                m_nWaferTotal = _MappingData.Length;

                #region 在mapping完成後會建立資料，容器是 Waferlist

                //利用GWID得到的type判斷是哪一種type
                SWafer.enumWaferSize eWaferSize = GetCurrentLoadportWaferType();
              
                DateTime dt = DateTime.Now;
                Waferlist.Clear();
                for (int i = 0; i < _MappingData.Length; i++)
                {
                    if (_MappingData[i] == '1')
                    {
                        _Waferlist.Add(new SWafer(
                            m_strFoupID,
                            "lotID",
                            "CJID-" + dt.ToString("yyyyMMddHHmmss"),
                            "PJID-" + dt.ToString("yyyyMMddHHmmss"),
                            "RECIPE",
                            i + 1,//20220708
                            eWaferSize,
                            SWafer.enumPosition.Loader1 + BodyNo - 1,
                            SWafer.enumFromLoader.LoadportA + BodyNo - 1,
                            GParam.theInst.EqmDisableArray,
                            SWafer.enumProcessStatus.Sleep)
                            );
                    }
                    else if (_MappingData[i] == '0')
                    {
                        Waferlist.Add(null);
                    }
                    else
                    {
                        _Waferlist.Add(new SWafer(
                          m_strFoupID,
                          "lotID",
                          "CJID-" + dt.ToString("yyyyMMddHHmmss"),
                          "PJID-" + dt.ToString("yyyyMMddHHmmss"),
                          "RECIPE",
                          i + 1,//20220708
                          eWaferSize,
                          SWafer.enumPosition.Loader1 + BodyNo - 1,
                          SWafer.enumFromLoader.LoadportA + BodyNo - 1,
                          GParam.theInst.EqmDisableArray,
                          SWafer.enumProcessStatus.Error)
                          );
                    }

                }
                #endregion
            }
        }
        public string SimulateMappingData
        {
            set
            {
                MappingData = value;
            }
        }

        protected List<SWafer> _Waferlist = new List<SWafer>();
        public List<SWafer> Waferlist { get { return _Waferlist; } }
        public SWafer TakeWaferOutFoup(int nIndex)//手動模式下被Robot取出
        {
            SWafer wafer = Waferlist[nIndex];
            Waferlist[nIndex] = null;

            OnTakeWaferOutFoup?.Invoke(nIndex + 1);

            string str = _MappingData;
            str = str.Remove(nIndex, 1);
            str = str.Insert(nIndex, "0");
            _MappingData = str;

            return wafer;
        }
        public void TakeWaferInFoup(int nIndex, SWafer wafer)//Robot放wafer到Loadport裡面
        {
            //重新定義位置
            wafer.SetOwner(SWafer.enumFromLoader.LoadportA + BodyNo - 1);

            wafer.Slot = nIndex + 1;//重新定義位置

            Waferlist[nIndex] = wafer;

            OnTakeWaferInFoup?.Invoke(nIndex + 1);//用於UI//用於UI

            string str = _MappingData;
            str = str.Remove(nIndex, 1);
            str = str.Insert(nIndex, "1");
            _MappingData = str;
        }
        public void TakeWaferSlotExchange(int nSlot1, int nSlot2)
        {
            SWafer temp = Waferlist[nSlot1 - 1];

            Waferlist[nSlot1 - 1] = Waferlist[nSlot2 - 1];

            Waferlist[nSlot2 - 1] = temp;
        }
        public void SetWaferIDCompare(int nSlot, string WaferID_F, string WaferID_B)
        {
            try
            {
                int nIndex = nSlot - 1;
                if (nIndex < Waferlist.Count)
                {
                    Waferlist[nIndex].WaferInforID_F = WaferID_F;
                    Waferlist[nIndex].WaferInforID_B = WaferID_B;
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        #endregion
        #region =========================== e84 ================================================
        protected I_E84 _e84;
        public I_E84 E84Object { get { return _e84; } set { _e84 = value; } }

        protected E84PortStates _E84Status;
        public E84PortStates E84Status { get { return _E84Status; } set { _E84Status = value; } }
        public bool IsE84Auto { get { return _e84 == null ? false : _e84.GetAutoMode; } }
        public void SetE84AutoMode(bool bAuto) { if (_e84 != null) _e84.SetAutoMode(bAuto); }
        public void SetE84TPtime(int[] nTime) { if (_e84 != null) _e84.SetTpTime = nTime; }
        #endregion

        public SSLoadportParents(I_E84 e84, int nBodyNo, bool bDisable, bool bSimulate, int[] nTrbMapStgNo0to399, string strLoadportWaferType, I_BarCode barcode, sServer sever = null)
        {
            _e84 = e84;
            BodyNo = nBodyNo;   //  1 2 3 4      
            Simulate = bSimulate;
            Disable = bDisable;
            m_nTrbMapStgNo0to399 = nTrbMapStgNo0to399;
            LoadportWaferType = new enumWaferSize[strLoadportWaferType.Length];// 11111111111111110000000000000000
            m_Barcode = barcode;
            for (int i = 0; i < strLoadportWaferType.Length; i++)
                LoadportWaferType[i] = (enumWaferSize)int.Parse(strLoadportWaferType[i].ToString());

            for (int nCnt = 0; nCnt < (int)enumLoadPortCommand.Max; nCnt++)
                _signalAck.Add((enumLoadPortCommand)nCnt, new SSignal(false, EventResetMode.ManualReset));

            for (int i = 0; i < (int)enumLoadPortSignalTable.Max; i++)
                _signals.Add((enumLoadPortSignalTable)i, new SSignal(false, EventResetMode.ManualReset));

            _threadInit = new SInterruptOneThread(ExeINIT);
            _threadOrgn = new SInterruptOneThread(ExeORGN);
            _threadClamp = new SInterruptOneThread(ExeCLMP);
            _threadClamp1 = new SInterruptOneThread(ExeClamp1);
            _threadUClamp = new SInterruptOneThread(ExeUCLM);
            _threadUClamp1 = new SInterruptOneThread(ExeUClamp1);
            _threadReset = new SInterruptOneThreadINT(ExeRsta);
            _threadMapping = new SInterruptOneThread(ExeWMAP);
            _threadFoupExist = new SInterruptOneThread(ExeCheckFoupExist);
            _threadJigDock = new SInterruptOneThread(ExeJigDock);
            _threadGetData = new SInterruptOneThread(ExeGetData);

            _signalSubSequence = new SSignal(false, EventResetMode.ManualReset);
            _LoadCompletSignal = new SSignal(false, EventResetMode.ManualReset);
            _UnLoadCompletSignal = new SSignal(false, EventResetMode.ManualReset);
            _MappingCompletSignal = new SSignal(false, EventResetMode.ManualReset);
            _JigDockCompletSignal = new SSignal(false, EventResetMode.ManualReset);

            _pollingAuto = new SPollingThread(1);
            _pollingAuto.DoPolling += _pollingAuto_DoPolling;

            _pollingMonitor = new SPollingThread(100);
            _pollingMonitor.DoPolling += _pollingMonitor_DoPolling;

            this.FoupID = "";
            this._CarrierIDstatus = CarrierIDStats.Create;
            this._SlotMappingStats = CarrierSlotMapStats.NoStatus;
            this._CarrierAccessStats = CarrierAccessStats.NoStatus;
            this.E84Status = E84PortStates.OUTOFSERVICE;
            this.UndockQueueByHost = false;
            this._CarrierState = CarrierState.NoStatus;

            _jobschedule = new List<SWafer>();

            string[] DefineTeachingData = new string[]
            {
               "000, 2, 0, 25, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FUP1",
               "000, 2, 0, 25, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FUP2",
               "000, 2, 0, 25, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FUP3",
               "000, 2, 0, 25, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FUP4",
               "000, 2, 0, 25, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FUP5",
               "000, 2, 0, 25, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FUP6",
               "000, 2, 0, 25, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FUP7",
               "007, 2, 0, 25, 00010000, 00000200, 00001300, +0000100, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FSB1",
               "007, 2, 0, 25, 00010000, 00000200, 00001300, +0000100, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FSB2",
               "007, 2, 0, 25, 00010000, 00000200, 00001300, +0000100, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FSB3",
               "007, 2, 0, 25, 00010000, 00000200, 00001300, +0000100, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FSB4",
               "007, 2, 0, 25, 00010000, 00000200, 00001300, +0000100, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FSB5",
               "008, 2, 0, 25, 00010000, 00000200, 00001300, +0011000, +0001500, +0000000, +0000000, +0000000, +0180301, +0000000, +0000000, +0000000,OCP1",
               "008, 2, 0, 25, 00010000, 00000200, 00001300, +0011000, +0001500, +0000000, +0000000, +0000000, +0180301, +0000000, +0000000, +0000000,OCP2",
               "008, 2, 0, 25, 00010000, 00000200, 00001300, +0011000, +0001500, +0000000, +0000000, +0000000, +0180301, +0000000, +0000000, +0000000,OCP3",
               "010, 2, 0, 01, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,FPO1",

               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCP4",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCP5",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCP6",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCP7",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCP8",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCP9",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPA",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPB",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPC",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPD",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPE",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPF",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPG",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPH",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPI",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPJ",
               "008, 2, 0, 13, 00010000, 00000200, 00001300, +0000000, +0001300, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000, +0000000,OCPK",
            };
            for (int nData = 0; nData < _sDPRMData.Length; nData++)
                _sDPRMData[nData] = DefineTeachingData[nData].Split(',');

            CreateMessage();

            if (!Disable)
            {
                _pollingMonitor.Set();
            }

        }
        ~SSLoadportParents()
        {
            _pollingAuto.Close();
            _pollingAuto.Dispose();
            _pollingMonitor.Close();
            _pollingMonitor.Dispose();
        }
        protected void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[STG{0}] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }
        protected void SetStringIO(ref string strValue, int nBit, bool bOn)
        {
            Int64 nValue = Convert.ToInt64(strValue, 16);
            Int64 nV = 0x01 << nBit;
            string strHex;
            if (bOn)
            {
                strHex = (nValue | nV).ToString("X16");
            }
            else
            {
                strHex = (nValue & ~nV).ToString("X16");
            }
            strValue = strHex;
        }
        public abstract void Open();
        public abstract void Close();
        //==============================================================================
        #region AutoProcess
        #region Run貨權
        static private object _objHolding = new object(); //鎖臨界區間
        static private bool _blnHold = false; //run貨權
        private bool _bIsRunning = false;

        public bool IsRunning
        {
            get { return _bIsRunning; }
            private set
            {
                _bIsRunning = value;
                if (_bIsRunning)
                {
                    //搶到running貨權觸發事件
                    OnReadyRunningLot?.Invoke(this, new EventArgs());
                }
            }
        }
        /// <summary>
        /// 搶run貨權
        /// </summary>
        /// <returns></returns>
        public bool GetRunningPermission()
        {
            lock (_objHolding)
            {
                if (_bIsRunning) return true; //已經搶到run貨權, 直接return true.
                if (_blnHold) return false; //run貨權已經被搶走

                _bIsRunning = true; //搶到了!
                _blnHold = true;
                return _blnHold;
            }
        }
        /// <summary>
        /// 釋放run貨權
        /// </summary>
        public void ReleaseRunningPermission()
        {
            lock (_objHolding)
            {
                if (!_bIsRunning) return; //沒搶到run貨權就不能釋放                 
                _bIsRunning = false;
                _blnHold = false;
            }
        }
        #endregion
        public void AutoProcessStart()
        {
            this._pollingAuto.Set();
            if (OnProcessStart != null)
                OnProcessStart(this, new EventArgs());
        }
        public void AutoProcessEnd()
        {
            _pollingAuto.Reset();
        }
        protected void _pollingAuto_DoPolling()
        {
            try
            {
                if (DoAutoProcessing != null) DoAutoProcessing(this);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> DoAutoProcessing thread:" + ex);
                _pollingAuto.Reset(); //停止自動流程

                if (OnProcessAbort != null)
                    OnProcessAbort(this, new EventArgs());
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> DoAutoProcessing thread:" + ex);
                _pollingAuto.Reset();

                if (OnProcessAbort != null)
                    OnProcessAbort(this, new EventArgs());
            }
        }
     
        public List<SWafer> Getjobschedule()
        {
            return _jobschedule;
        }
        
        public void Addjobschedule(SWafer Wafer)
        {
            _jobschedule.Add(Wafer);
        }
        
        public void deletejobschedule(SWafer Wafer)
        {
            _jobschedule.Remove(Wafer);
        }
        
        public void Cleanjobschedule()
        {
            _jobschedule.Clear();
        }

        public bool AssignWafer(string strLotID, int nSlot, SWafer wafer)
        {
            OnAssignWaferData?.Invoke(this, new WaferDataEventArgs(wafer));
            return true;
        }
        #endregion
        //==============================================================================
        #region OneThread 
        public void INIT() { _threadInit.Set(); }
        public void ORGN() { _threadOrgn.Set(); }
        public void CLMP(bool NeedCheckFoupType = false)
        {
            if (NeedCheckFoupType)
            {
                if (m_LPInfoPadEnableList[(int)eFoupType] == false)
                {
                    SendWarningMsg(enumLoadPortWarning.Foup_Type_Diable);
                    _threadUClamp.Set();
                    return;
                }
            }
            _threadClamp.Set();
        }
        public void CLMP1() { _threadClamp1.Set(); }
        public void UCLM() { _threadUClamp.Set(); }
        public void UCLM1() { _threadUClamp1.Set(); }
        public void WMAP() { _threadMapping.Set(); }
        public void RSTA(int nNum) { _threadReset.Set(nNum); }
        public void JigDock() { _threadJigDock.Set(); }
        public void GetData() { _threadGetData.Set(); }
        public void CheckFoupExist() { _threadFoupExist.Set(); }
        //==============================================================================
        protected abstract void ExeINIT();
        protected abstract void ExeORGN();
        protected abstract void ExeCLMP();
        protected abstract void ExeClamp1();
        protected abstract void ExeUCLM();
        protected abstract void ExeUClamp1();
        protected abstract void ExeRsta(int nMode);
        protected abstract void ExeCheckFoupExist();
        protected abstract void ExeWMAP();
        protected abstract void ExeJigDock();
        protected abstract void ExeGetData();
        #endregion
        //==============================================================================
        public abstract void OrgnW(int nTimeout, int nVariable = 0);
        public abstract void ClmpW(int nTimeout, int nVariable = 0);
        public abstract void UclmW(int nTimeout, int nVariable = 0);
        public abstract void WmapW(int nTimeout);
        public abstract void LoadW(int nTimeout);
        public abstract void UnldW(int nTimeout);
        public abstract void EventW(int nTimeout);
        public abstract void ResetW(int nTimeout, int nReset = 0);
        public abstract void InitW(int nTimeout);
        public abstract void StopW(int nTimeout);
        public abstract void PausW(int nTimeout);
        public abstract void ModeW(int nTimeout, int nMode);
        public abstract void WtdtW(int nTimeout);
        public abstract void SspdW(int nTimeout, int nVariable);
        public abstract void SpotW(int nTimeout, int nBit, bool bOn);
        public abstract void StatW(int nTimeout);
        public abstract void GpioW(int nTimeout);
        public abstract void GmapW(int nTimeout);
        public abstract void Rca2W(int nTimeout, int nVariable);
        public abstract void GverW(int nTimeout);
        public abstract void StimW(int nTimeout);

        public abstract void GposW(int nTimeout);
        public abstract void GwidW(int nTimeout);
        public abstract void SwidW(int nTimeout, string strId);
        public abstract void YaxStepW(int nTimeout, string strStep);
        public abstract void ZaxStepW(int nTimeout, string strStep);
        public abstract void YaxHomeW(int nTimeout, int nHome);
        public abstract void ZaxHomeW(int nTimeout, int nHome);
        public abstract void GetDprmW(int nTimeout, int p1);
        public abstract void SetDprmW(int nTimeout, int p1, string strData);
        public abstract void GetDmprW(int nTimeout);
        public abstract void SetDmprW(int nTimeout, int p1, string strData);
        public abstract void GetDcstW(int nTimeout);
        public abstract void SetDCSTW(int nTimeout, string strData);
        //==============================================================================
        public void ResetChangeModeCompleted()
        {
            _signals[enumLoadPortSignalTable.Remote].Reset();
        }
        public void WaitChangeModeCompleted(int nTimeout)
        {
            if (Connected)
            {
                if (!_signals[enumLoadPortSignalTable.Remote].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.InitialFailure);
                    throw new SException((int)enumLoadPortError.InitialFailure, string.Format("Wait Mode was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumLoadPortSignalTable.Remote].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.InitialFailure);
                    throw new SException((int)enumLoadPortError.InitialFailure, string.Format("Motion is Mode end."));
                }
            }
        }
        public void ResetProcessCompleted()
        {
            _signals[enumLoadPortSignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = true;
        }
        public void WaitProcessCompleted(int nTimeout)
        {
            if (Connected)
            {
                if (!_signals[enumLoadPortSignalTable.ProcessCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.ProcessFlagTimeout);
                    throw new SException((int)enumLoadPortError.ProcessFlagTimeout, string.Format("Wait process flag complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumLoadPortSignalTable.ProcessCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.ProcessFlagAbnormal);
                    throw new SException((int)enumLoadPortError.ProcessFlagAbnormal, string.Format("Wait process flag complete was failure. [Timeout = {0} ms]", nTimeout));
                }
            }
            else
            {
                Thread.Sleep(500);
            }
        }
        public void ResetInPos()
        {
            _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
            m_bMoving = true;
        }
        public void WaitInPos(int nTimeout)
        {
            if (Connected)
            {
                //motion complete
                if (!_signals[enumLoadPortSignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.MotionTimeout);
                    throw new SException((int)enumLoadPortError.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumLoadPortSignalTable.MotionCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.MotionAbnormal);
                    throw new SException((int)enumLoadPortError.MotionAbnormal, string.Format("Wait process flag complete was failure. [Timeout = {0} ms]", nTimeout));
                }
                m_bMoving = false;
            }
            else if (Simulate)
            {
                m_bMoving = false;
                SpinWait.SpinUntil(() => false, 500);
            }
            else
            {
                SpinWait.SpinUntil(() => false, 500);
            }
        }
        //==============================================================================     
        #region =========================== CommandTable =======================================
        protected Dictionary<enumLoadPortCommand, string> _dicCmdsTable;
        #endregion 
        #region =========================== Signals ============================================
        protected Dictionary<enumLoadPortCommand, SSignal> _signalAck = new Dictionary<enumLoadPortCommand, SSignal>();
        protected Dictionary<enumLoadPortSignalTable, SSignal> _signals = new Dictionary<enumLoadPortSignalTable, SSignal>();
        protected SSignal _signalSubSequence;

        protected SSignal _LoadCompletSignal;
        protected SSignal _UnLoadCompletSignal;
        protected SSignal _MappingCompletSignal;
        protected SSignal _JigDockCompletSignal;
        #endregion
        #region =========================== OnOccurError =======================================
        //  發生STAT異常
        protected void SendAlmMsg(string strCode)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  STG1 15 11 00000
            //  STG2 16 11 00000
            //  STG3 17 11 00000
            //  STG4 18 11 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (15 + BodyNo - 1) * 10000000 + 11 * 100000*/;
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));
            SendAlmMsg(enumLoadPortError.Status_Error);
        }
        //  解除STAT異常
        protected void RestAlmMsg(string strCode)
        {
            WriteLog(string.Format("Rest stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  STG1 15 11 00000
            //  STG2 16 11 00000
            //  STG3 17 11 00000
            //  STG4 18 11 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (15 + BodyNo - 1) * 10000000 + 11 * 100000*/;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  Cancel Code
        protected void SendCancelMsg(string strCode)
        {
            WriteLog(string.Format("Occur cancel Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  STG1 15 12 00000
            //  STG2 16 12 00000
            //  STG3 17 12 00000
            //  STG4 18 12 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (15 + BodyNo - 1) * 10000000 + 12 * 100000*/;
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生自定義異常
        protected void SendAlmMsg(enumLoadPortError eAlarm)
        {
            WriteLog(string.Format("Occur eAlarm Error : {0}", eAlarm));
            //  STG1 15 10 00000
            //  STG2 16 10 00000
            //  STG3 17 10 00000
            //  STG4 18 10 00000
            int nCode = (int)eAlarm /*+ (15 + BodyNo - 1) * 10000000 + 10 * 100000*/;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生警告
        public void SendWarningMsg(enumLoadPortWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur Warning  : {0}", eWarning), meberName, lineNumber);
            int nCode = (int)eWarning;
            OnOccurWarning?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除警告
        public void RestWarningMsg(enumLoadPortWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Reset Warning  : {0}", eWarning), meberName, lineNumber);
            int nCode = (int)eWarning;
            OnOccurWarningRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        public void TriggerSException(enumLoadPortError eAlarm, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            SendAlmMsg(eAlarm);
            throw new SException((int)(eAlarm), "SException:" + eAlarm);
        }
        #endregion
        #region =========================== CreateMessage ======================================
        public Dictionary<int, string> m_dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();
        protected virtual void CreateMessage()
        {
            m_dicCancel[0x0200] = "0200:The operating objective is not supported";
            m_dicCancel[0x0300] = "0300:The composition elements of command are too few";
            m_dicCancel[0x0310] = "0310:The composition elements of command are too many";
            m_dicCancel[0x0400] = "0400:Command is not supported";
            m_dicCancel[0x0500] = "0500:Too few parameters";
            m_dicCancel[0x0510] = "0510:Too many parameters";
            for (int i = 0; i < 0x10; i++)
            {
                m_dicCancel[0x0600 + i] = string.Format("{0:X4}:The parameter No.{1} is too small", 0x0600 + i, i + 1);
                m_dicCancel[0x0610 + i] = string.Format("{0:X4}:The Parameter No.{1} is too large", 0x0610 + i, i + 1);
                m_dicCancel[0x0620 + i] = string.Format("{0:X4}:The Parameter No.{1} is not numeral", 0x0620 + i, i + 1);
                m_dicCancel[0x0630 + i] = string.Format("{0:X4}:The Parameter No.{1} is not correct", 0x0630 + i, i + 1);
                m_dicCancel[0x0640 + i] = string.Format("{0:X4}:The Parameter No.{1} is not a hexadecimal numeral", 0x0640 + i, i + 1);
                m_dicCancel[0x0650 + i] = string.Format("{0:X4}:The Parameter No.{1} is not correct", 0x0650 + i, i + 1);
                m_dicCancel[0x0660 + i] = string.Format("{0:X4}:The Parameter No.{1} is not pulse", 0x0660 + i, i + 1);

                m_dicCancel[0x1030 + i] = string.Format("{0:X4}:Interfering with No.{1} axis", 0x1030 + i, i + 1);
            }
            m_dicCancel[0x0700] = "0700:Abnormal Mode: Not ready";
            m_dicCancel[0x0702] = "0702:Abnormal Mode: Not in the maintenance mode";
            for (int i = 0; i < 0x0100; i++)
            {
                m_dicCancel[0x0800 + i] = string.Format("{0:X4}:Setting data of No.{1} is not correct!", 0x0800 + i, i + 1);
            }
            m_dicCancel[0x0920] = "0902:Improper setting";
            m_dicCancel[0x0A00] = "0A00:Origin search not completed";
            m_dicCancel[0x0A01] = "0A01:Origin reset not completed";
            m_dicCancel[0x0B00] = "0B00:Processing";
            m_dicCancel[0x0B01] = "0B01:Moving";
            m_dicCancel[0x0D00] = "0D00:Abnormal flash memory";
            m_dicCancel[0x0F00] = "0F00:Error-occurred state";
            m_dicCancel[0x1000] = "1000:Movement is unable due to carrier presence";
            m_dicCancel[0x1001] = "1001:Movement is unable due to no carrier presence";
            m_dicCancel[0x1002] = "1002:Improper setting";
            m_dicCancel[0x1003] = "1003:Improper current position";
            m_dicCancel[0x1004] = "1004:Movement is unable due to small designated position";
            m_dicCancel[0x1005] = "1005:Movement is unable due to large designated position";
            m_dicCancel[0x1006] = "1006:Presence of the adapter cannot be identified";
            m_dicCancel[0x1007] = "1007:Origin search cannot be perfomed due to abnormal presence state of the adapter";
            m_dicCancel[0x1008] = "1008:Adapter not prepared";
            m_dicCancel[0x1009] = "1009:Cover not closed";
            m_dicCancel[0x1100] = "1100:Emergency stop signal is ON";
            m_dicCancel[0x1200] = "1200:Pause signal is On./Area sensor beam is blocked";
            m_dicCancel[0x1300] = "1300:Interlock signal is ON";
            m_dicCancel[0x1400] = "1400:Driver power is OFF";
            m_dicCancel[0x2000] = "2000:No response from the ID reader/writer";
            m_dicCancel[0x2100] = "2100:Command for the ID reader/writer is cancelled";

            m_dicController[0x00] = "[00:Others] ";
            m_dicController[0x01] = "[01:Y-axis] ";
            m_dicController[0x02] = "[02:Z-axis] ";
            m_dicController[0x03] = "[03:Lifting/Lowering mechanism for reading the carrier ID] ";
            m_dicController[0x04] = "[04:Stage retaining mechanism] ";
            m_dicController[0x05] = "[05:Rotation table] ";
            m_dicController[0x0F] = "[06:Driver] ";

            for (int i = 0; i < 0x10; i++)
            {
                m_dicController[0x10 + i] = string.Format("[{0:x2}:IO unit {1}] ", 0x10 + i, i + 1);
            }

            m_dicError[0x01] = "01:Motor stall";
            m_dicError[0x02] = "02:Sensor abnormal";
            m_dicError[0x03] = "03:Emergency stop";
            m_dicError[0x04] = "04:Command error";
            m_dicError[0x05] = "05:Communication error";
            m_dicError[0x06] = "06:Chucking sensor abnormal";
            m_dicError[0x07] = "07:(Reserved)";
            m_dicError[0x08] = "08:Obstacle detection sensor error";
            m_dicError[0x09] = "09:Second origin sensor abnormal";
            m_dicError[0x0A] = "0A:Mapping sensor abnormal";
            m_dicError[0x0B] = "0B:Wafer protrusion sensor abnormal";
            m_dicError[0x0E] = "0E:Driver abnormal";
            m_dicError[0x0F] = "0F:Power abnormal";
            m_dicError[0x20] = "20:Control power abnormal";
            m_dicError[0x21] = "21:Driver power abnormal";
            m_dicError[0x22] = "22:EEPROM abnormal";
            m_dicError[0x23] = "23:Z search abnormal";
            m_dicError[0x24] = "24:Overheat";
            m_dicError[0x25] = "25:Overcurrent";
            m_dicError[0x26] = "26:Motor cable abnormal";
            m_dicError[0x27] = "27:Motor stall (position deviation)";
            m_dicError[0x28] = "28:Motor stall (time over)";
            m_dicError[0x2F] = "2F:Memory check abnormal";
            m_dicError[0x89] = "89:Exhaust fan abnormal";
            m_dicError[0x92] = "92:FOUP clamp/rotation disabled";
            m_dicError[0x93] = "93:FOUP unclamp/rotation disabled";
            m_dicError[0x94] = "94:Latch key lock disabled";
            m_dicError[0x95] = "95:(Reserved)";
            m_dicError[0x96] = "96:Latch key release disabled";
            m_dicError[0x97] = "97:Mapping sensor preparation disabled";
            m_dicError[0x98] = "98:Mapping sensor containing disabled";
            m_dicError[0x99] = "99:Chucking on disabled";
            m_dicError[0x9A] = "9A:Wafer protrusion";
            m_dicError[0x9B] = "9B:No cover on FOUP/With cover on FOSB";
            m_dicError[0x9C] = "9C:Carrier improperly taken";
            m_dicError[0x9D] = "9D:FSOB door detection";
            m_dicError[0x9E] = "9E:Carrier improperly placed";
            m_dicError[0xA0] = "A0:Cover lock disabled";
            m_dicError[0xA1] = "A1:Cover unlock disabled";
            m_dicError[0xB0] = "B0:TR_REQ timeout";
            m_dicError[0xB1] = "B1:BUSY ON timeout";
            m_dicError[0xB2] = "B2:Carrier carry-in timeout";
            m_dicError[0xB3] = "B3:Carrier carry-out timeout";
            m_dicError[0xB4] = "B4:BUSY OFF timeout";
            m_dicError[0xB5] = "B5:(Reserved)";
            m_dicError[0xB6] = "56:VALID OFF timeout";
            m_dicError[0xB7] = "B7:CONTINUE timeout";
            m_dicError[0xB8] = "B8:Signal abnormal detected from VALID,CS_0=ON to TR_REQ=ON";
            m_dicError[0xB9] = "B9:Signal abnormal detected from TR_REQ=ON to BUSY=ON";
            m_dicError[0xBA] = "BA:Signal abnormal detected from BUSY=ON to Placement=ON";
            m_dicError[0xBB] = "BB:Signal abnormal detected from Placement=ON to COMPLETE=ON";
            m_dicError[0xBC] = "BC:Signal abnormal detected from COMPLETE=ON to VALID=OFF";
            m_dicError[0xBF] = "BF:VALID, CS_0 signal abnormal";

            //==============================================================================
            _dicCmdsTable = new Dictionary<enumLoadPortCommand, string>()
            {
                {enumLoadPortCommand.Orgn,"ORGN"},
                {enumLoadPortCommand.Clamp,"CLMP"},
                {enumLoadPortCommand.UnClamp,"UCLM"},
                {enumLoadPortCommand.Mapping,"WMAP"},
                {enumLoadPortCommand.E84Load,"LOAD"},
                {enumLoadPortCommand.E84UnLoad,"UNLD"},
                {enumLoadPortCommand.SetEvent,"EVNT"},
                {enumLoadPortCommand.Reset,"RSTA"},
                {enumLoadPortCommand.Initialize,"INIT"},
                {enumLoadPortCommand.Stop,"STOP"},
                {enumLoadPortCommand.Pause,"PAUS"},
                {enumLoadPortCommand.Mode,"MODE"},
                {enumLoadPortCommand.Wtdt,"WTDT"},
                {enumLoadPortCommand.GetData,"RTDT"},
                {enumLoadPortCommand.TransferData,"TRDT"},
                {enumLoadPortCommand.Speed,"SSPD"},
                {enumLoadPortCommand.SetIO,"SPOT" },
                {enumLoadPortCommand.Status,"STAT"},
                {enumLoadPortCommand.GetIO,"GPIO"},
                {enumLoadPortCommand.GetRAC2,"RCA2.GPOS"},
                {enumLoadPortCommand.GetMappingData,"GMAP"},
                {enumLoadPortCommand.GetVersion,"GVER"},
                {enumLoadPortCommand.GetLog,"GLOG"},
                {enumLoadPortCommand.SetDateTime,"STIM"},
                {enumLoadPortCommand.GetDateTime,"GTIM"},
                {enumLoadPortCommand.GetPos,"GPOS"},
                {enumLoadPortCommand.GetType,"GWID" },
                {enumLoadPortCommand.SetType,"SWID" },
                {enumLoadPortCommand.ZaxStep,"ZAX1.STEP"},
                {enumLoadPortCommand.ZaxHome,"ZAX1.HOME"},
                {enumLoadPortCommand.YaxHome,"YAX1.HOME"},
                {enumLoadPortCommand.GetDPRM,"DPRM.GTDT" },
                {enumLoadPortCommand.SetDPRM,"DPRM.STDT" },
                {enumLoadPortCommand.GetDMPR,"DMPR.GTDT" },
                {enumLoadPortCommand.SetDMPR,"DMPR.STDT" },
                {enumLoadPortCommand.GetDCST,"DCST.GTDT" },
                {enumLoadPortCommand.SetDCST,"DCST.STDT" },
                {enumLoadPortCommand.ReadID,"READ"},
                {enumLoadPortCommand.WriteID,"WRIT"},
                {enumLoadPortCommand.ClientConnected,"CNCT"},
            };
        }
        #endregion

        #region 20220103新增 客戶要勾住Adapter        
        public bool IsKeepClamp { get; protected set; }
        public void KeepClamp(bool bClamp)
        {
            IsKeepClamp = bClamp;
            if (bClamp)
            {
                CLMP1();
            }
            else
            {
                UCLM1();
            }
        }
        #endregion

        public void UpdateInfoPadEnable(List<bool> InfoPadList)
        {
            m_LPInfoPadEnableList = InfoPadList;
        }
        public bool IsInfoPadEnable()
        {
            bool b = false;
            if (FoupExist == true)
            {
                if (m_LPInfoPadEnableList[(int)eFoupType] == false)
                {
                    SendWarningMsg(enumLoadPortWarning.Foup_Type_Diable);
                }
                else
                {
                    b = true;
                }
            }
            return b;
        }
        public void UpdateTrbMapInfoEnable(List<bool> enableList)
        {
            m_TrbMapInfoEnableList = enableList;
        }
        public bool IsInfoPadTrbMapEnable()
        {
            bool b = false;
            b = m_TrbMapInfoEnableList[(int)eFoupType];
            return b;
        }
        public abstract void SetFoupExistChenge();
        public abstract void SimulateFoupOn(bool bOn);

        private void _pollingMonitor_DoPolling()
        {
            try
            {
                switch (StatusMachine)
                {
                    case RorzeUnit.Class.Loadport.Enum.enumStateMachine.PS_FoupOn:
                    case RorzeUnit.Class.Loadport.Enum.enumStateMachine.PS_Arrived:
                    case RorzeUnit.Class.Loadport.Enum.enumStateMachine.PS_Clamped:
                    case RorzeUnit.Class.Loadport.Enum.enumStateMachine.PS_UnClamped:
                    case RorzeUnit.Class.Loadport.Enum.enumStateMachine.PS_ReadyToUnload:
                        {
                            int nTime = FoupArrivalIdleTimeout * 1000;
                            if (nTime > 0)
                            {
                                TimeSpan ts = DateTime.Now - m_FoupArrivalTime;
                                double dElapseTime = ts.TotalMilliseconds;
                                if (dElapseTime > nTime)
                                {
                                    m_FoupArrivalTime = DateTime.Now;
                                    SendWarningMsg(enumLoadPortWarning.Foup_Arrival_Idle_Timeout);
                                }
                            }
                        }
                        break;
                    case RorzeUnit.Class.Loadport.Enum.enumStateMachine.PS_Docked:

                        {
                            int nTime = FoupWaitTransferTimeout * 1000;
                            if (nTime > 0)
                            {
                                TimeSpan ts = DateTime.Now - m_FoupDockedTime;
                                double dElapseTime = ts.TotalMilliseconds;
                                if (dElapseTime > nTime)
                                {
                                    m_FoupDockedTime = DateTime.Now;
                                    SendWarningMsg(enumLoadPortWarning.Foup_Wait_Transfer_Timeout);
                                }
                            }
                        }
                        break;
                }
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> _pollingMonitor_DoPolling:" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> _pollingMonitor_DoPolling:" + ex);
            }
        }

        //==============================================================================     
        public void BarcodeOpen() { if (m_Barcode != null) m_Barcode.Open(); }//20240828
        public bool IsBarcodeConnected { get { return m_Barcode != null && m_Barcode.Connected; } }//20240828
        public bool IsBarcodeEnable { get { return m_Barcode != null; } }//20240828
        public string BarcodeRead() { return m_Barcode == null ? "No Barcode" : m_Barcode.Read(); }//20240828

        public abstract void ClmpW_WithoutMapping(int nTimeout, int nVariable = 0);
    }
}
