using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.Stock.Enum;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Concurrent;
using static RorzeUnit.Class.SWafer;
using static RorzeUnit.Class.Stock.Evnt.TowerEventArgs;
using Advantech.Common;
using RorzeApi;
using static RorzeUnit.Net.Sockets.sClient;
using static System.Windows.Forms.AxHost;
using System.Security.Claims;
using System.Windows.Forms;
using static RorzeUnit.Class.Stock.Evnt.TowerEventArgs.TowerGPIO;
using System.Windows.Controls;
using Advantech.Adam;


namespace RorzeUnit.Class.Stock
{
    public class SSStock : I_Stock
    {
        #region =========================== Private =====================================
        private enumTowerMode m_eStatMode;       //記憶的STAT S1第1
        private bool m_bStatOrgnComplete;        //記憶的STAT S1第2
        private bool m_bStatProcessed;           //記憶的STAT S1第3
        private enumTowerStatus m_eStatInPos;    //記憶的STAT S1第4
        private int m_nSpeed;                    //記憶的STAT S1第5
        private List<string> m_strRecordsStatErr = new List<string>();     //記憶的STAT S2

        protected int m_nOrgTimeout = 60000 * 5;
        protected int m_nMapTimeout = 60000 * 5;

        protected int m_nAckTimeout = 5000 * 2;

        protected int m_nInitAckTimeout = 60000 * 5;
        protected int m_nMotionTimeout = 60000 * 1;

        private bool m_bMoving;//Robot運動中
        private bool m_bRobotExtand = false;
        private bool m_bManualMoving = false;
        private bool ProcessStart { get; set; }

        private bool m_bPassPurge;

        private string m_strTowerEnable;//硬體可以關閉Enable

        private int m_nSeveralSlotsInTheArea = 25;//Area是25片



        private ConcurrentQueue<string[]> m_queRecvBuffer;
        SSStockPcon m_StockIcon;

        SWafer.enumPosition m_eTowerFirst;
        SLogger m_logger = SLogger.GetLogger("CommunicationLog");

        List<SWafer> _jobschedule;

        #endregion
        #region =========================== Public ======================================
        public bool Simulate { get; private set; }
        public bool Connected { get; private set; }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }
        public string VersionData { get; private set; }
        public bool IsRobotExtend { get { return m_bRobotExtand; } }
        public bool SetRobotExtend { set { m_bRobotExtand = value; } }
        public int GetAckTimeout { get { return m_nAckTimeout; } }          //Timeout
        public int GetMotionTimeout { get { return m_nMotionTimeout; } }    //Timeout

        //STAT S1第1 bit
        public enumTowerMode StatMode { get { return m_eStatMode; } }
        public bool IsInitialized { get { return m_eStatMode == enumTowerMode.Remote; } }
        //STAT S1第2 bit
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }
        //STAT S1第3 bit
        public bool IsProcessing { get { return m_bStatProcessed; } }
        //STAT S1第4 bit
        public enumTowerStatus InPos { get { return m_eStatInPos; } }
        public bool IsMoving { get { return m_bMoving || m_eStatInPos == enumTowerStatus.Moving; } }
        //STAT S1第5 bit
        public int GetSpeed { get { return m_nSpeed; } }
        //STAT S2
        public bool IsError { get { return m_strRecordsStatErr.Count != 0; } }

        public SWafer.enumWaferSize WaferType { get; private set; }//讀取ini   

        /// <summary>
        /// 10 50 90 130
        /// Body1:011~018,021~028,031~038,041~048
        /// Body2:051~058,061~068,071~078,081~088
        /// Body3:091~098,101~108,111~118,121~128
        /// Body4:131~138,141~148,151~158,161~168
        /// </summary>
        public int StageNo { get; private set; }
        /// <summary>
        /// Tower一般四個面
        /// </summary>
        public int TowerCount { get { return m_strTowerEnable.Length; } }
        public bool TowerEnable(int nIndx)
        {
            if (Disable)
            { return false; }

            if (nIndx < m_strTowerEnable.Length)
                return m_strTowerEnable[nIndx] == '1';
            else
                return false;
        }
        public int TheTowerSlotNumber { get; private set; }//200/400
        public int TheTowerStgeNumber { get; private set; }//8/16
        public TowerGPIO GPIO { get; private set; }  //GPIO
        public TowerGPIO GPIO_old { get; private set; }  //GPIO

        object m_oLockGPIO = new object();
        object m_oLockOrder = new object();

        public bool PassPurge
        {
            get { return m_bPassPurge; }
            set
            {
                m_bPassPurge = value;
                if (m_bPassPurge)
                {
                    foreach (enumTowerWarning item in System.Enum.GetValues(typeof(enumTowerWarning)))
                    {
                        if (item == enumTowerWarning.IonizerError) continue;
                        RestWarningMsg(item);
                    }
                }
            }
        }



        #endregion
        #region =========================== Event =======================================
        //public event EventHandler<WaferDataEventArgs> AssignToRobotQueue;
        //public event EventHandler<int> OnWaferDataDelete;
        public event EventHandler<bool> OnManualCompleted;
        public event EventHandler<bool> OnINITComplete;
        public event EventHandler<bool> OnORGNComplete;
        public event EventHandler<bool> OnGetDataComplete;
        public event EventHandler<bool> OnLOADComplete;
        public event EventHandler<bool> OnHOMEComplete;
        public event EventHandler<TowerGMAP_EventArgs> OnMappingComplete;
        public event EventHandler<string> OnMappingCompleteAll;
        public event EventHandler OnMappingError;

        public event EventHandler OnProcessStart;
        public event EventHandler OnProcessEnd;
        public event EventHandler OnProcessAbort;

        public event AutoProcessingEventHandler DoManualProcessing;
        public event AutoProcessingEventHandler DoAutoProcessing;

        public event MessageEventHandler OnReadData;

        public event OccurErrorEventHandler OnOccurStatErr;
        public event OccurErrorEventHandler OnOccurCancel;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public event OccurErrorEventHandler OnOccurErrorRest;
        public event OccurErrorEventHandler OnOccurWarning;
        public event OccurErrorEventHandler OnOccurWarningRest;

        public event dlgv_n_n OnTakeWaferOutFoup;              //wafer從foup中被取出
        public event dlgv_n_n OnTakeWaferInFoup;               //wafer被放回foup

        public event EventHandler<enumStateMachine> OnStatusMachineChange;

        #endregion
        #region =========================== Thread ======================================
        private SInterruptOneThread _threadManualFunc;
        private SInterruptOneThread _threadInit;             //初始化控制(private 流程, 問Status/機況同步)
        private SInterruptOneThread _threadOrgn;             //原點復歸控制

        private SInterruptOneThread _threadMapping;       //掃片單動控制  
        private SInterruptOneThreadINT _threadReset;         //異常復歸控制
        private SInterruptOneThread _threadGetData;
        private SInterruptOneThreadINT_INT _threadLoad;      //轉動開門
        private SInterruptOneThreadINT _threadHome;      //轉動開門

        private SPollingThread _pollingAuto;                 //自動流程控管   

        private SPollingThread _exePolling;
        #endregion
        #region =========================== Delegate ====================================
        public dlgb_v pLoadInterlock { get; set; }           // 不可以load
        public dlgb_v pUnloadInterlock { get; set; }         // 不可以unload
        public dlgv_wafer AssignToRobotQueue { get; set; }   //丟給robot作排程
        #endregion
        #region =========================== Signals =====================================
        private Dictionary<enumTowerCommand, SSignal> _signalAck = new Dictionary<enumTowerCommand, SSignal>();
        private Dictionary<enumTowerSignalTable, SSignal> _signals = new Dictionary<enumTowerSignalTable, SSignal>();
        private SSignal _signalSubSequence;
        #endregion
        #region =========================== Property ====================================

        private int m_nRaxisPos;
        private int m_nAaxisPos;
        private int m_nClampPos;
        private int m_nZaxisPos;
        public int GetRaxisPos { get { return m_nRaxisPos; } }
        public int GetAaxisPos { get { return m_nAaxisPos; } }
        public int GetClampPos { get { return m_nClampPos; } }
        public int GetZaxisPos { get { return m_nZaxisPos; } }

        private enumStateMachine _StatusMachine = enumStateMachine.PS_Ready;
        public enumStateMachine StatusMachine
        {
            get { return _StatusMachine; }
            set
            {
                if (_StatusMachine != value)
                {
                    _StatusMachine = value;
                    if (OnStatusMachineChange != null)
                        OnStatusMachineChange(this, _StatusMachine);
                }
            }
        }

        private string[] m_strCJID = new string[4];
        public void SetCJID(int nIndx, string strCJID) { m_strCJID[nIndx] = strCJID; }
        public string GetCJID(int nIndx) { return m_strCJID[nIndx]; }

        public bool IsClampOpenSlotNotSafety
        {
            get
            {
                bool b5 = GPIO.GetDIList[enumTowerDI.L_CLAMP_2] == false;//沒有放開
                bool b6 = GPIO.GetDIList[enumTowerDI.R_CLAMP_2] == false;//沒有放開

                bool b1 = GPIO.GetDIList[enumTowerDI.L_CLAMP_1] == false;//沒有放開
                bool b2 = GPIO.GetDIList[enumTowerDI.R_CLAMP_1] == false;//沒有放開

                bool b3 = GetClampPos != 0;
                bool b4 = GetZaxisPos != 0;
                return /*b1 || b2 ||*/ b3 || b4;
            }
        }

        public bool OpenerZaxsMoveSynchronization { get; set; }

        #endregion
        #region =========================== Wafer Data ==================================
        private string m_strCurrentMappingData;//當下的結果
        private object m_oLockWaferData = new object();

        Dictionary<int, string> m_MappingData = new Dictionary<int, string>();          // stage number
        Dictionary<int, List<SWafer>> m_WaferData = new Dictionary<int, List<SWafer>>();// stage number        
        Dictionary<int, bool[]> m_strOrder = new Dictionary<int, bool[]>();//紀錄那些位置鎖定正在傳送 建帳->1 LU->0

        public Dictionary<int, List<SWafer>> WaferData { get { return m_WaferData; } }

        /// <summary>
        /// Tower1to16,Slot1to200or400
        /// </summary>
        /// <param name="nTower1to16">Tower:1~16</param>
        /// <param name="nTowerSlot">Slot:1~200/400</param>
        /// <returns>WaferData</returns>
        public SWafer GetWafer(int nTower1to16, int nTowerSlot)
        {
            int nStge = FaceIndx0to3Slot1to200TransferStgNum((nTower1to16 - 1) % TowerCount, nTowerSlot);
            int nSlot1to25 = Slot1to200TransferSlot1to25(nTowerSlot);
            if (WaferData.ContainsKey(nStge))
                return WaferData[nStge][nSlot1to25 - 1];
            else
                return null;
        }
        /// <summary>
        /// Stge1to400,Slot1to25
        /// </summary>
        /// <param name="nStg1to400">Stage:1~400</param>
        /// <param name="nSlot1to25">Slot:1~25</param>
        /// <returns>WaferData</returns>
        public SWafer GetWaferByRobotStgSlot(int nStg1to400, int nSlot1to25)
        {
            if (WaferData.ContainsKey(nStg1to400))
                return WaferData[nStg1to400][nSlot1to25 - 1];
            else
                return null;
        }
        /// <summary>
        /// Slot:1~800or1600
        /// </summary>
        /// <param name="nStockSlot">Slot1to800or1600</param>
        /// <returns>WaferData</returns>
        public SWafer GetWaferByStockSlot(int nStockSlot)
        {
            int nFaceIndx = StockSlotTransfer0to3(nStockSlot);
            int nTowerSlot = (nStockSlot - 1) % TheTowerSlotNumber + 1;
            int nStge = FaceIndx0to3Slot1to200TransferStgNum(nFaceIndx, nTowerSlot);
            int nSlot1to25 = Slot1to200TransferSlot1to25(nTowerSlot);
            if (WaferData.ContainsKey(nStge))
                return WaferData[nStge][nSlot1to25 - 1];
            else
                return null;
        }
        /// <summary>
        /// Transfer Wafer Count
        /// </summary>
        /// <param name="nTower1to16">Tower:1~16</param>
        /// <returns></returns>
        public int GetTransferWaferCount(int nTower1to16)
        {
            int nOutSideWafer = 0;
            foreach (var item1 in m_WaferData.ToArray())
            {
                if (StgNum1to400Transfer0to15(item1.Key) != (nTower1to16 - 1))
                    continue;
                foreach (SWafer w in item1.Value.ToArray())
                {
                    if (w != null && w.ReadyToProcess == true && w.ProcessStatus != enumProcessStatus.Processed)
                    { nOutSideWafer += 1; }
                }
            }
            return nOutSideWafer;
        }
        /// <summary>
        /// GMAP result 4*200/400
        /// </summary>
        /// <returns>1111....1111...1111(800/1600)</returns>
        public string GetMapDataAll()
        {
            string strMappingDataAll = "";
            foreach (string item in m_MappingData.Values.ToArray())
                strMappingDataAll += item;
            return strMappingDataAll;
        }
        /// <summary>
        /// GMAP result 200/400
        /// </summary>
        /// <returns>1111....1111...1111(200/400)</returns>
        public string GetMapDataOneTower(int nFaceIndx)
        {
            string strMappingDataOneTower = "";
            foreach (var item in m_MappingData.ToArray())
            {
                if (StgNum1to400Transfer0to3(item.Key) == nFaceIndx)//只要對應的面
                    strMappingDataOneTower += item.Value;
            }
            return strMappingDataOneTower;
        }
        /// <summary>
        /// 看GMAP裡面有幾個1
        /// </summary>
        /// <returns></returns>
        public int GetMapDataWaferCount()
        {
            int n = 0;
            foreach (var item in m_MappingData.ToArray())
            {
                foreach (char c in item.Value)
                    if (c != '0') n++;
            }
            return n;
        }

        //直接使用外部告知MAPPING結果，為了測機不需要mapping
        public void SetMapDataTower(int nFaceIndx, string strMap1to400)   //直接使用外部告知MAPPING結果
        {

            if (TowerEnable(nFaceIndx) == false) return;

            int nStageForMapping = TheTowerStgeNumber > 10 ? (StageNo + 20 * nFaceIndx) : (StageNo + 10 * nFaceIndx);

            string strMappingDataOneTower = "";
            for (int j = 0; j < TheTowerStgeNumber; j++)//總共要問8/16次
            {
                int nStge1to400 = nStageForMapping + j + 1;//下10會對11~18

                m_MappingData[nStge1to400] = strMap1to400.Substring(0 + 25 * j, 25);

                #region 在mapping完成後會建立資料，容器是 Waferlist

                List<SWafer> listWafer = new List<SWafer>();
                string strMappingData = m_MappingData[nStge1to400];
                strMappingDataOneTower += strMappingData;//每次25個疊加
                int nTower0to15 = StgNum1to400Transfer0to15(nStge1to400);
                for (int k = 0; k < strMappingData.Length; k++)
                {
                    SWafer.enumPosition ePos = m_eTowerFirst + StgNum1to400Transfer0to3(nStge1to400);
                    SWafer wafer = null;
                    if (strMappingData[k] != '0')
                    {
                        wafer = new SWafer("", "lotID", "CJID-", "PJID-", "RECIPE", k + 1,
                            WaferType,
                            (SWafer.enumPosition)StgNum1to400Transfer0to15(nStge1to400),
                            SWafer.enumFromLoader.Tower01 + nTower0to15,
                            strMappingData[k] == '1' ? SWafer.enumProcessStatus.Sleep : SWafer.enumProcessStatus.Error);
                    }
                    listWafer.Add(wafer);
                }

                if (m_WaferData.ContainsKey(nStge1to400)) { m_WaferData[nStge1to400] = listWafer; }
                else { m_WaferData.Add(nStge1to400, listWafer); }

                #endregion
            }

            if (strMappingDataOneTower.Length == TheTowerSlotNumber)//防呆
                OnMappingComplete?.Invoke(this, new TowerGMAP_EventArgs(nFaceIndx, strMappingDataOneTower));//以200/400片更新

        }

        /// <summary>
        /// 設定那些位置被鎖定傳送中
        /// </summary>
        /// <param name="nFaceIndx"></param>
        /// <param name="nSlot1to400"></param>
        public void SetSlotOrder(int nFaceIndx, int nSlot1to400, bool bOrder)//紀錄那些位置鎖定正在傳送 建帳->1 LU->0
        {
            lock (m_oLockOrder)
            {
                m_strOrder[nFaceIndx][nSlot1to400 - 1] = bOrder;
            }
        }
        /// <summary>
        /// 查詢位置是否鎖定
        /// </summary>
        /// <param name="nTower1to16"></param>
        /// <param name="nSlot1to400"></param>
        /// <returns></returns>
        public bool IsTowerSlotOrder(int nTower1to16, int nSlot1to400)
        {
            bool b = false;
            lock (m_oLockOrder)
            {
                int nFaceIndx = (nTower1to16 - 1) % TowerCount;
                b = m_strOrder[nFaceIndx][nSlot1to400 - 1];
            }
            return b;
        }



        #endregion
        #region =========================== OnOccurError ================================
        //  發生STAT異常
        private void SendAlmMsg(string strCode, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode), meberName, lineNumber);
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));
            SendAlmMsg(enumTowerCustomError.Status_Error);
        }
        //  解除STAT異常
        private void RestAlmMsg(string strCode, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Reset stat Error : {0}", strCode), meberName, lineNumber);
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  Cancel Code
        private void SendCancelMsg(string strCode, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur cancel : {0}", strCode), meberName, lineNumber);
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生自定義異常
        private void SendAlmMsg(enumTowerCustomError eAlarm, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur custom error : {0}", eAlarm), meberName, lineNumber);
            int nCode = (int)eAlarm;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生警告
        private void SendWarningMsg(enumTowerWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur Warning  : {0}", eWarning), meberName, lineNumber);
            int nCode = (int)eWarning;
            OnOccurWarning?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除警告
        private void RestWarningMsg(enumTowerWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            //WriteLog(string.Format("Reset Warning  : {0}", eWarning), meberName, lineNumber);
            int nCode = (int)eWarning;
            OnOccurWarningRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }


        #endregion
        #region =========================== CreateMessage ===============================
        private Dictionary<enumTowerCommand, string> _dicCmdsTable;
        public Dictionary<int, string> m_dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();
        private void CreateMessage()
        {
            m_dicCancel[0xFF00] = "xFF00:Parameter format is wrong.";
            m_dicCancel[0xFF01] = "xFF00:Header is wrong.";
            m_dicCancel[0xFF02] = "xFF10:Unit ID is wrong.";
            m_dicCancel[0xFF03] = "xFF00:Command is wrong.";
            m_dicCancel[0x0200] = "Motion target is not supported";
            m_dicCancel[0x0300] = "Too few command elements";
            m_dicCancel[0x0310] = "Too many command elements";
            m_dicCancel[0x0400] = "Command is not supported";
            m_dicCancel[0x0500] = "Too few/ Too many parameters";
            m_dicCancel[0x0510] = "Too few/ Too many parameters";
            m_dicCancel[0x0520] = "Improper parameter number";

            for (int i = 0; i < 0x10; i++)
            {
                m_dicCancel[0x0600 + i] = string.Format("{0:X4}:The parameter No.{1} is too small", 0x0600 + i, i + 1);
                m_dicCancel[0x0610 + i] = string.Format("{0:X4}:The Parameter No.{1} is too large", 0x0610 + i, i + 1);
                m_dicCancel[0x0620 + i] = string.Format("{0:X4}:The Parameter No.{1} is not numeral", 0x0620 + i, i + 1);
                m_dicCancel[0x0630 + i] = string.Format("{0:X4}:The Parameter No.{1} is not correct", 0x0630 + i, i + 1);
                m_dicCancel[0x0640 + i] = string.Format("{0:X4}:The Parameter No.{1} is not a hexadecimal numeral", 0x0640 + i, i + 1);
                m_dicCancel[0x0650 + i] = string.Format("{0:X4}:The Parameter No.{1} is not correct", 0x0650 + i, i + 1);
                m_dicCancel[0x0660 + i] = string.Format("{0:X4}:The Parameter No.{1} is not pulse", 0x0660 + i, i + 1);

                m_dicCancel[0x0900 + i] = string.Format("{0:X4}:The teaching data of the axis No.{1} is too small", 0x0900 + i, i + 1);
                m_dicCancel[0x0910 + i] = string.Format("{0:X4}:The Teaching data of the axis No.{1} is too large", 0x0910 + i, i + 1);
            }

            for (int i = 0; i < 0x0100; i++)
            {
                m_dicCancel[0x0800 + i] = string.Format("{0:X4}:Setting data of No.{1} is not correct!", 0x0800 + i, i + 1);
            }

            m_dicCancel[0x0700] = "Abnormal mode: Preparation not completed";
            m_dicCancel[0x0702] = "Abnormal mode: Not in maintenance mode";
            m_dicCancel[0x0704] = "Command, which cannot be executed by the teaching pendant, is received.";
            m_dicCancel[0x0920] = "Improper slot designation";
            m_dicCancel[0x0921] = "The number of slots not set";
            m_dicCancel[0x0A00] = "Origin search not completed";
            m_dicCancel[0x0A01] = "Origin reset not completed";
            m_dicCancel[0x0B00] = "Processing";
            m_dicCancel[0x0B01] = "Moving";
            m_dicCancel[0x0B02] = "Abnormal memory processing";
            m_dicCancel[0x0D00] = "Abnormal flash memory";
            m_dicCancel[0x0F00] = "Error - occurred state";
            m_dicCancel[0x1002] = "Improper setting";
            m_dicCancel[0x1003] = "Improper current position";
            m_dicCancel[0x1004] = "Motion cannot be performed due to too small designated position";
            m_dicCancel[0x1005] = "Motion cannot be performed due to too large designated position";
            m_dicCancel[0x1010] = "No wafer on the upper finger";
            m_dicCancel[0x1011] = "No wafer on the lower finger";
            m_dicCancel[0x1020] = "Wafer exists on the upper finger.";
            m_dicCancel[0x1021] = "Wafer exists on the lower finger.";
            m_dicCancel[0x1100] = "The emergency stop signal is turned on.";
            m_dicCancel[0x1200] = "The temporary stop signal is turned on.";
            m_dicCancel[0x1300] = "The interlock signal is turned on.";
            m_dicCancel[0x1400] = "The drive power is turned off.";
            m_dicCancel[0x1500] = "Needs HOME command begore ofst command";
            m_dicCancel[0x1600] = "Wrong ofst axis";
            m_dicCancel[0x1700] = "Interlock is not allow";
            m_dicCancel[0x1701] = "Area sensor Interlock not allow";
            m_dicCancel[0x1702] = "Protrude sensor Interlock not allow";
            m_dicCancel[0x1703] = "Maintance Door Open work not allow";
            m_dicCancel[0x1704] = "Clamp interlock not allow";
            m_dicCancel[0x1800] = "Warpage Error";

            m_dicController[0x00] = "[00:All] ";
            m_dicController[0x01] = "[01:Rot1] ";
            m_dicController[0x02] = "[02:Zax1] ";
            m_dicController[0x03] = "[03:CLM1] ";
            m_dicController[0x04] = "[02:CLM2] ";


            m_dicError[0x01] = "x01:Motor stall";
            m_dicError[0x02] = "x02:Sensor abnormal";
            m_dicError[0x03] = "x03:Emergency stop";
            m_dicError[0x04] = "x04:Command error";
            m_dicError[0x05] = "x05:Communication error";
            m_dicError[0x0C] = "x0C:ORGN reset not completed";
            m_dicError[0x0E] = "x0E:Driver abnormal";

            m_dicError[0x12] = "x12:Regeneration unit overheat";
            m_dicError[0x14] = "x14:Exhaust fan abnormal";
            m_dicError[0x15] = "x15:Operating position abnormal";
            m_dicError[0x16] = "x16:Encoder data lost";
            m_dicError[0x17] = "x17:Encoder communication error";
            m_dicError[0x18] = "x18:Encoder overspeed";
            m_dicError[0x19] = "x19:Encoder EEPROM  error";
            m_dicError[0x1A] = "x1A:Encoder ABS - INC error";
            m_dicError[0x1B] = "x1B:Encoder coefficient block error";
            m_dicError[0x1C] = "x1C:Encoder temperature error";
            m_dicError[0x1D] = "x1D:Encoder reset error";
            m_dicError[0x20] = "x20:Control power abnormal";
            m_dicError[0x21] = "x21:Drive power abnormal";
            m_dicError[0x22] = "x22:EEPROM abnormal";
            m_dicError[0x24] = "x24:Overheat";
            m_dicError[0x25] = "x25:Over current";
            m_dicError[0x26] = "x26:Motor cable abnormal";
            m_dicError[0x27] = "x27:Following error warning range";
            m_dicError[0x28] = "x28:Following error exceeds limit";
            m_dicError[0x29] = "x29:In forward hardware limit";
            m_dicError[0x30] = "x30:In reverse hardware limit";
            m_dicError[0x31] = "x31:FS Limit active";
            m_dicError[0x32] = "x32:RS Limit active";
            m_dicError[0x33] = "x33:Axis FS Limit active";
            m_dicError[0x34] = "x34:Axis RS Limit active";
            m_dicError[0x35] = "x35:EtherCAT emergency message received from remote drive";
            m_dicError[0x36] = "x36:EtherCAT slave number abnormal";
            m_dicError[0x37] = "x37:Clamp motion close abnormal";
            m_dicError[0x38] = "x38:Clamp motion open abnormal";
            m_dicError[0x39] = "x39:LOAD abnormal";
            m_dicError[0x40] = "x40:UNLD abnormal";
            m_dicError[0x41] = "x41:Warpage Sensor abnormal";
            m_dicError[0x42] = "x42:Diff-X Sensor abnormal";
            m_dicError[0x43] = "x43:RH Sensor abnormal";
            m_dicError[0x44] = "x44:Area Sensor abnormal";
            m_dicError[0x45] = "x45:Ionizer01 abnormal";
            m_dicError[0x46] = "x46:Ionizer02 abnormal";
            m_dicError[0x47] = "x47:Low Flow abnormal";
            m_dicError[0x48] = "x48:High Flow abnormal";
            m_dicError[0x49] = "x49:HOME abnormal";
            m_dicError[0x50] = "x50:Frame detect abnormal";


            m_dicError[0x83] = "x83:Origin search disabled";
            m_dicError[0x84] = "x84:Foup retaining error";
            m_dicError[0x85] = "x85Interlock signal abnormal";
            m_dicError[0x86] = "x86:Alignment error";
            m_dicError[0x89] = "x89:Exhaust fan abnormal";
            m_dicError[0x8A] = "x8A:Battery voltage too low";
            m_dicError[0x92] = "x92:Clamp error";
            m_dicError[0x93] = "x93:FOUP presence abnormal";
            m_dicError[0x95] = "x95:Zaxis not at wait position";
            m_dicError[0x96] = "x96:Clm not at wait position";



            //==============================================================================
            _dicCmdsTable = new Dictionary<enumTowerCommand, string>()
            {
                {enumTowerCommand.Orgn,"ORGN"},
                {enumTowerCommand.Home,"HOME"},
                {enumTowerCommand.Extend,"EXTD"},
                {enumTowerCommand.Load,"LOAD"},
                {enumTowerCommand.Unload,"UNLD"},

                {enumTowerCommand.Clamp,"CLMP"},
                {enumTowerCommand.UnClamp,"UCLM"},
                {enumTowerCommand.Mapping,"WMAP"},
                //{enumTowerCommand.E84Load,"LOAD"},
                //{enumTowerCommand.E84UnLoad,"UNLD"},
                {enumTowerCommand.SetEvent,"EVNT"},
                {enumTowerCommand.Reset,"RSTA"},
                {enumTowerCommand.Initialize,"INIT"},
                {enumTowerCommand.Stop,"STOP"},
                {enumTowerCommand.Pause,"PAUS"},
                {enumTowerCommand.Mode,"MODE"},
                {enumTowerCommand.Wtdt,"WTDT"},
                {enumTowerCommand.GetData,"RTDT"},
                {enumTowerCommand.TransferData,"TRDT"},
                {enumTowerCommand.Speed,"SSPD"},
                {enumTowerCommand.SetIO,"SPOT" },
                {enumTowerCommand.Status,"STAT"},
                {enumTowerCommand.GetIO,"GPIO"},
                {enumTowerCommand.GetMappingData,"GMAP"},
                {enumTowerCommand.GetVersion,"GVER"},
                {enumTowerCommand.GetLog,"GLOG"},
                {enumTowerCommand.SetDateTime,"STIM"},
                {enumTowerCommand.GetDateTime,"GTIM"},
                {enumTowerCommand.GetPos,"GPOS"},
                {enumTowerCommand.EPOS,"EPOS"},
                {enumTowerCommand.GetType,"GWID" },
                {enumTowerCommand.SetType,"SWID" },
                {enumTowerCommand.ZaxStep,"ZAX1.STEP"},
                {enumTowerCommand.ZaxHome,"ZAX1.HOME"},
                {enumTowerCommand.YaxHome,"YAX1.HOME"},
                {enumTowerCommand.GetDPRM,"DPRM.GTDT" },
                {enumTowerCommand.SetDPRM,"DPRM.STDT" },
                {enumTowerCommand.GetDMPR,"DMPR.GTDT" },
                {enumTowerCommand.SetDMPR,"DMPR.STDT" },
                {enumTowerCommand.GetDCST,"DCST.GTDT" },
                {enumTowerCommand.SetDCST,"DCST.STDT" },
                {enumTowerCommand.ReadID,"READ"},
                {enumTowerCommand.WriteID,"WRIT"},
                {enumTowerCommand.ClientConnected,"CNCT"},
        };
        }
        #endregion

        public SSStock(int nBodyNo, bool bDisable, bool bSimulate,
            int nWaferType, string strTowerEnable, int nTowerSlotNumber, int nStageNo, bool openerZaxsMoveSynchronization,
            SWafer.enumPosition eTowerFirst, SSStockPcon stockIcon)
        {
            Simulate = bSimulate;
            Disable = bDisable;
            BodyNo = nBodyNo;   //  1 2 3 4    
                        
            WaferType = (SWafer.enumWaferSize)nWaferType;

            m_strTowerEnable = strTowerEnable;
            TheTowerSlotNumber = nTowerSlotNumber;       //一面塔200層
            TheTowerStgeNumber = TheTowerSlotNumber / m_nSeveralSlotsInTheArea;//一面塔200層25個一組200/25=8, 400/25=16

            //Body1:011~018,021~028,031~038,041~048
            //Body2:051~058,061~068,071~078,081~088
            //Body3:091~098,101~108,111~118,121~128
            //Body4:131~138,141~148,151~158,161~168
            StageNo = nStageNo;//10 50 90 130
            OpenerZaxsMoveSynchronization = openerZaxsMoveSynchronization;
            m_eTowerFirst = eTowerFirst;

            m_StockIcon = stockIcon;
            if (Disable == false)
                m_StockIcon.OnReadData += _StockIcon_OnReadData;

            m_queRecvBuffer = new ConcurrentQueue<string[]>();

            for (int nCnt = 0; nCnt < (int)enumTowerCommand.Max; nCnt++)
                _signalAck.Add((enumTowerCommand)nCnt, new SSignal(false, EventResetMode.ManualReset));

            for (int i = 0; i < (int)enumTowerSignalTable.Max; i++)
                _signals.Add((enumTowerSignalTable)i, new SSignal(false, EventResetMode.ManualReset));

            _signals[enumTowerSignalTable.ProcessCompleted].Set();
            _threadManualFunc = new SInterruptOneThread(RunManualFunction);
            _threadInit = new SInterruptOneThread(ExeINIT);
            _threadOrgn = new SInterruptOneThread(ExeORGN);
            _threadReset = new SInterruptOneThreadINT(ExeRsta);
            _threadMapping = new SInterruptOneThread(ExeWMAP);
            _threadGetData = new SInterruptOneThread(ExeGetData);
            _threadLoad = new SInterruptOneThreadINT_INT(ExeLOAD);
            _threadHome = new SInterruptOneThreadINT(ExeHOME);
            _signalSubSequence = new SSignal(false, EventResetMode.ManualReset);

            _pollingAuto = new SPollingThread(1);
            _pollingAuto.DoPolling += _pollingAuto_DoPolling;

            _exePolling = new SPollingThread(1);
            _exePolling.DoPolling += _exePolling_DoPolling;

            //if (Simulate)
            {
                string strZero = string.Format("{0:D48}", 0);
                //eSTK2.GPIO:FFFFFFFF000045F8/0000000000000001
                GPIO = GPIO_old = new TowerGPIO(strZero, strZero);
            }

            for (int i = 0; i < m_strTowerEnable.Length; i++)
            {
                m_strOrder[i] = new bool[TheTowerSlotNumber];
            }

            if (!Disable)
            {
                _exePolling.Set();
            }
            _jobschedule = new List<SWafer>();
            CreateMessage();
        }
        public void Close()
        {
            _pollingAuto.Close();
            _exePolling.Close();
        }
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[STK{0}] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }
        #region AutoProcess
        public void AutoProcessStart()
        {
            ProcessStart = true;
            this._pollingAuto.Set();

            OnProcessStart?.Invoke(this, new EventArgs());
        }
        public void AutoProcessEnd()
        {
            _pollingAuto.Reset();
            ProcessStart = false;
        }
        private void _pollingAuto_DoPolling()
        {
            try
            {
                DoAutoProcessing?.Invoke(this);
            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
                _pollingAuto.Reset();//停止自動流程
                OnProcessAbort?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
                _pollingAuto.Reset();//停止自動流程
                OnProcessAbort?.Invoke(this, new EventArgs());
            }
        }
        #endregion
        #region 處理TCP接收到的內容
        private void _StockIcon_OnReadData(object sender, string[] e)
        {
            m_queRecvBuffer.Enqueue(e);
            //foreach(string ssss in e)
            //    WriteLog(ssss);
        }
        private void _exePolling_DoPolling()
        {
            try
            {
                int Emptycount = 0;
                string[] astrFrame;

                if (m_queRecvBuffer.TryDequeue(out astrFrame) == false) return;
                string strFrame;

                OnReadData?.Invoke(this, new MessageEventArgs(astrFrame));

                for (int nCnt = 0; nCnt < astrFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                {
                    if (astrFrame[nCnt].Length == 0)
                    {
                        Emptycount += 1;

                        continue;
                    }

                    strFrame = astrFrame[nCnt];

                    if (strFrame.Contains(string.Format("STK{0}", this.BodyNo)) == false)
                    {
                        continue;
                    }

                    enumTowerCommand cmd = enumTowerCommand.GetVersion;
                    bool bUnknownCmd = true;

                    foreach (string scmd in _dicCmdsTable.Values) //查字典
                    {
                        if (strFrame.Contains(string.Format("STK{0}.{1}", this.BodyNo.ToString("X"), scmd)))
                        {
                            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                            bUnknownCmd = false; //認識這個指令
                            break;
                        }
                    }

                    if (bUnknownCmd) //不認識的封包
                    {
                        WriteLog(string.Format("Got unknown frame and pass to process.[{0}]", strFrame));
                        continue;
                    }

                    //if (strFrame.Contains("GPIO"))
                    //{ }
                    //else
                    WriteLog(string.Format("Receive : {0} cmd:{1}", strFrame, cmd));

                    switch (strFrame[0]) //命令種類
                    {
                        case 'c': //cancel
                            OnCancelAck(this, new TowerProtoclEventArgs(strFrame));
                            _signalAck[cmd].bAbnormalTerminal = true;
                            //_signalAck[cmd].Set();//這裡不能給set，底層架構可能會誤判，因為
                            break;
                        case 'n': //nak
                            _signalAck[cmd].bAbnormalTerminal = true;
                            _signalAck[cmd].Set();
                            break;
                        case 'a': //ack
                            OnAck(this, new TowerProtoclEventArgs(strFrame));
                            _signalAck[cmd].Set();
                            break;
                        case 'e':
                            OnAck(this, new TowerProtoclEventArgs(strFrame));
                            break;
                        default:

                            break;
                    }

                }
            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
            }
        }
        void OnAck(object sender, TowerProtoclEventArgs e)
        {
            enumTowerCommand cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                case enumTowerCommand.GetMappingData:
                    AssignGMAP(e.Frame.Value);
                    break;
                case enumTowerCommand.Status:
                    AnalysisStatus(e.Frame.Value);
                    break;
                case enumTowerCommand.GetIO:
                    AnalysisGPIO(e.Frame.Value);
                    break;
                case enumTowerCommand.GetPos:
                    AnalysisGPOS(e.Frame.Value);
                    break;
                case enumTowerCommand.GetVersion:
                    break;
                case enumTowerCommand.GetDateTime:
                    break;
                case enumTowerCommand.ClientConnected:
                    _signalAck[cmd].Set();
                    Connected = true;
                    break;
                default:
                    break;
            }
        }
        void OnCancelAck(object sender, TowerProtoclEventArgs e)
        {
            enumTowerCommand cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;
            AnalysisCancel(e.Frame.Value);
        }
        private void AssignGMAP(string strFrame) { m_strCurrentMappingData = strFrame; }
        private void AnalysisStatus(string strFrame)
        {
            try
            {
                if (!strFrame.Contains('/'))
                {
                    WriteLog(string.Format("The format of STAT has error, [{0}]", strFrame));
                    return;
                }
                string[] str = strFrame.Split('/');
                string s1 = str[0];
                string s2 = str[1];

                //S1.bit#1 operation mode
                switch (s1[0])
                {
                    case '0':
                        m_eStatMode = enumTowerMode.Initializing;
                        break;
                    case '1':
                        m_eStatMode = enumTowerMode.Remote;
                        _signals[enumTowerSignalTable.Remote].Set();
                        break;
                    case '2':
                        m_eStatMode = enumTowerMode.Maintenance;
                        _signals[enumTowerSignalTable.Remote].Set();
                        break;
                    case '3':
                        m_eStatMode = enumTowerMode.Recovery;
                        break;
                    default: break;
                }

                //S1.bit#2 origin return complete
                if (s1[1] == '0') _signals[enumTowerSignalTable.OPRCompleted].Reset();
                else _signals[enumTowerSignalTable.OPRCompleted].Set();
                m_bStatOrgnComplete = s1[1] == '1';

                //S1.bit#3 processing command
                if (s1[2] == '0') _signals[enumTowerSignalTable.ProcessCompleted].Set();
                else _signals[enumTowerSignalTable.ProcessCompleted].Reset();
                m_bStatProcessed = s1[2] == '1';

                //S1.bit#4 operation status
                switch (s1[3])
                {
                    case '0': m_eStatInPos = enumTowerStatus.InPos; break;
                    case '1': m_eStatInPos = enumTowerStatus.Moving; break;
                    case '2': m_eStatInPos = enumTowerStatus.Pause; break;
                }

                //S1.bit#5 operation speed
                if (s1[4] >= '0' && s1[4] <= '9') m_nSpeed = s1[4] - '0';
                else if (s1[4] >= 'A' && s1[4] <= 'K') m_nSpeed = s1[4] - 'A' + 10;

                //if (m_nSpeed == 0 || m_nSpeed == 20) m_nMotionTimeout = 60000;
                //else m_nMotionTimeout = 60000 * 5;

                //S2
                if (Convert.ToInt32(s2, 16) > 0)
                {
                    _signals[enumTowerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                    _signals[enumTowerSignalTable.MotionCompleted].Set();
                    SendAlmMsg(s2);
                    m_strRecordsStatErr.Add(s2);
                }
                else
                {
                    if (m_eStatInPos == enumTowerStatus.InPos)//運動到位               
                        _signals[enumTowerSignalTable.MotionCompleted].Set();
                    else
                        _signals[enumTowerSignalTable.MotionCompleted].Reset();

                    foreach (string item in m_strRecordsStatErr)
                    {
                        RestAlmMsg(item);
                    }
                    m_strRecordsStatErr.Clear();
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
            }
        }
        private void AnalysisCancel(string strFrame)
        {
            try
            {
                if (Convert.ToInt32(strFrame, 16) > 0)
                {
                    _signals[enumTowerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                    _signals[enumTowerSignalTable.MotionCompleted].Set(); //有moving過才可以Set
                    SendCancelMsg(strFrame);
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
            }
        }
        private void AnalysisGPIO(string strFrame)
        {
            try
            {
                if (strFrame.Contains('/') == false)
                {
                    WriteLog(string.Format("The format of GPIO has error, [{0}]", strFrame));
                    return;
                }

                lock (m_oLockGPIO)
                {
                    GPIO = new TowerGPIO(strFrame.Split('/')[0], strFrame.Split('/')[1]);

                    foreach (var item in GPIO.GetDIList)
                    {
                        if (item.Value == GPIO_old.GetDIList[item.Key]) continue;//狀態相同

                        switch (item.Key)
                        {
                            case enumTowerDI.N2_Source_Pressure:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.N2_Source_Pressure);
                                else RestWarningMsg(enumTowerWarning.N2_Source_Pressure);
                                break;
                            case enumTowerDI.XCDA_Source_Pressure:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.XCDA_Source_Pressure);
                                else RestWarningMsg(enumTowerWarning.XCDA_Source_Pressure);
                                break;
                            case enumTowerDI.Adj_Pre_1:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.Adj_Pre_1);
                                else RestWarningMsg(enumTowerWarning.Adj_Pre_1);
                                break;
                            case enumTowerDI.Adj_Pre_2:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.Adj_Pre_2);
                                else RestWarningMsg(enumTowerWarning.Adj_Pre_2);
                                break;
                            case enumTowerDI.Adj_Pre_3:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.Adj_Pre_3);
                                else RestWarningMsg(enumTowerWarning.Adj_Pre_3);
                                break;
                            case enumTowerDI.Adj_Pre_4:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.Adj_Pre_4);
                                else RestWarningMsg(enumTowerWarning.Adj_Pre_4);
                                break;
                            case enumTowerDI.DoorOpen:
                                break;
                            case enumTowerDI.IonizerError:
                                if (item.Value == false) SendWarningMsg(enumTowerWarning.IonizerError);
                                else RestWarningMsg(enumTowerWarning.IonizerError);
                                break;
                            case enumTowerDI.Slot1_Flow_P1_011:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS1_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T1_CAS1_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot1_Flow_P2_011:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS1_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T1_CAS1_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot1_Flow_P1_031:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS1_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T2_CAS1_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot1_Flow_P2_031:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS1_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T2_CAS1_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot1_Flow_P1_051:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS1_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T3_CAS1_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot1_Flow_P2_051:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS1_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T3_CAS1_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot1_Flow_P1_071:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS1_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T4_CAS1_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot1_Flow_P2_071:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS1_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T4_CAS1_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot2_Flow_P1_011:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS2_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T1_CAS2_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot2_Flow_P2_011:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS2_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T1_CAS2_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot2_Flow_P1_031:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS2_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T2_CAS2_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot2_Flow_P2_031:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS2_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T2_CAS2_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot2_Flow_P1_051:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS2_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T3_CAS2_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot2_Flow_P2_051:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS2_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T3_CAS2_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot2_Flow_P1_071:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS2_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T4_CAS2_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot2_Flow_P2_071:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS2_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T4_CAS2_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot3_Flow_P1_011:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS3_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T1_CAS3_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot3_Flow_P2_011:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS3_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T1_CAS3_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot3_Flow_P1_031:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS3_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T2_CAS3_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot3_Flow_P2_031:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS3_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T2_CAS3_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot3_Flow_P1_051:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS3_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T3_CAS3_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot3_Flow_P2_051:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS3_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T3_CAS3_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot3_Flow_P1_071:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS3_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T4_CAS3_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot3_Flow_P2_071:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS3_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T4_CAS3_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot4_Flow_P1_011:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS4_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T1_CAS4_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot4_Flow_P2_011:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS4_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T1_CAS4_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot4_Flow_P1_031:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS4_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T2_CAS4_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot4_Flow_P2_031:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS4_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T2_CAS4_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot4_Flow_P1_051:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS4_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T3_CAS4_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot4_Flow_P2_051:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS4_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T3_CAS4_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot4_Flow_P1_071:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS4_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T4_CAS4_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot4_Flow_P2_071:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS4_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T4_CAS4_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot5_Flow_P1_011:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS5_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T1_CAS5_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot5_Flow_P2_011:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS5_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T1_CAS5_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot5_Flow_P1_031:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS5_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T2_CAS5_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot5_Flow_P2_031:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS5_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T2_CAS5_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot5_Flow_P1_051:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS5_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T3_CAS5_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot5_Flow_P2_051:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS5_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T3_CAS5_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot5_Flow_P1_071:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS5_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T4_CAS5_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot5_Flow_P2_071:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS5_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T4_CAS5_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot6_Flow_P1_011:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS6_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T1_CAS6_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot6_Flow_P2_011:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS6_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T1_CAS6_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot6_Flow_P1_031:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS6_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T2_CAS6_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot6_Flow_P2_031:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS6_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T2_CAS6_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot6_Flow_P1_051:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS6_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T3_CAS6_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot6_Flow_P2_051:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS6_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T3_CAS6_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot6_Flow_P1_071:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS6_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T4_CAS6_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot6_Flow_P2_071:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS6_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T4_CAS6_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot7_Flow_P1_011:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS7_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T1_CAS7_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot7_Flow_P2_011:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS7_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T1_CAS7_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot7_Flow_P1_031:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS7_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T2_CAS7_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot7_Flow_P2_031:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS7_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T2_CAS7_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot7_Flow_P1_051:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS7_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T3_CAS7_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot7_Flow_P2_051:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS7_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T3_CAS7_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot7_Flow_P1_071:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS7_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T4_CAS7_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot7_Flow_P2_071:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS7_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T4_CAS7_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot8_Flow_P1_011:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS8_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T1_CAS8_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot8_Flow_P2_011:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T1_CAS8_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T1_CAS8_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot8_Flow_P1_031:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS8_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T2_CAS8_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot8_Flow_P2_031:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T2_CAS8_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T2_CAS8_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot8_Flow_P1_051:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS8_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T3_CAS8_Purge_P1warning);
                                break;
                            //case enumTowerDI.Slot8_Flow_P2_051:
                            //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T3_CAS8_Purge_P2warning);
                            //    else RestWarningMsg(enumTowerWarning.T3_CAS8_Purge_P2warning);
                            //    break;
                            case enumTowerDI.Slot8_Flow_P1_071:
                                if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS8_Purge_P1warning);
                                else RestWarningMsg(enumTowerWarning.T4_CAS8_Purge_P1warning);
                                break;
                                //case enumTowerDI.Slot8_Flow_P2_071:
                                //    if (item.Value == false && PassPurge == false) SendWarningMsg(enumTowerWarning.T4_CAS8_Purge_P2warning);
                                //    else RestWarningMsg(enumTowerWarning.T4_CAS8_Purge_P2warning);
                                //    break;
                        }

                    }


                    GPIO_old = GPIO;
                }

            }
            catch (Exception ex) { WriteLog(string.Format("Exception : {0}", ex)); }
        }
        private void AnalysisGPOS(string strFrame)
        {
            try
            {
                if (!strFrame.Contains('/'))
                {
                    return;
                }
                else
                {
                    // Rot1/syncArm1/clm1/zax1
                    if (strFrame.Split('/').Length > 0)
                        m_nRaxisPos = int.Parse(strFrame.Split('/')[0]);
                    if (strFrame.Split('/').Length > 1)
                        m_nAaxisPos = int.Parse(strFrame.Split('/')[1]);
                    if (strFrame.Split('/').Length > 2)
                        m_nClampPos = int.Parse(strFrame.Split('/')[2]);
                    if (strFrame.Split('/').Length > 3)
                        m_nZaxisPos = int.Parse(strFrame.Split('/')[3]);
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
            }
        }
        #endregion
        #region OneThread 
        public void StartManualFunction() { _threadManualFunc.Set(); }
        public void INIT() { _threadInit.Set(); }
        public void ORGN() { _threadOrgn.Set(); }
        public void RSTA(int nNum) { _threadReset.Set(nNum); }
        public void WMAP() { _threadMapping.Set(); }
        public void GetData() { _threadGetData.Set(); }
        public void LOAD(int nStg, int nSlot) { _threadLoad.Set(nStg, nSlot); }
        public void HOME(int nStg) { _threadHome.Set(nStg); }
        //==============================================================================
        private void RunManualFunction()
        {
            try
            {
                WriteLog("RunManualFunction:Start");
                if (m_bManualMoving)//防呆
                {
                    SendAlmMsg(enumTowerCustomError.InterlockStop);
                    return;
                }

                m_bManualMoving = true;
                if (DoManualProcessing != null)
                    DoManualProcessing(this);
                DoManualProcessing = null; //做一次即清除, 再做一次需要再註冊一次

                OnManualCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                DoManualProcessing = null; //發生異常也要清除動作程序     
                OnManualCompleted?.Invoke(this, false);

            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                DoManualProcessing = null; //程式有bug須清除手動程序
                OnManualCompleted?.Invoke(this, false);
            }
            m_bManualMoving = false;
        }
        private void ExeINIT()
        {
            try
            {
                WriteLog(string.Format("ExeINIT Start"));

                this.ResetChangeModeCompleted();
                this.InitW(m_nInitAckTimeout);
                this.WaitChangeModeCompleted(m_nInitAckTimeout);
                SpinWait.SpinUntil(() => false, 500);

                OnINITComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
                OnINITComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
                OnINITComplete?.Invoke(this, false);
            }
        }
        private void ExeORGN()
        {
            try
            {
                WriteLog(string.Format("ExeORGN Start"));

                //this.ResetChangeModeCompleted();
                //this.ResetW(m_nAckTimeout);
                //this.WaitChangeModeCompleted(m_nAckTimeout);

                this.ResetChangeModeCompleted();
                this.EventW(m_nAckTimeout);
                this.WaitChangeModeCompleted(m_nAckTimeout);
                SpinWait.SpinUntil(() => false, 1000);

                if (BodyNo == 1)
                {
                    this.ResetProcessCompleted();
                    this.ResetChangeModeCompleted();
                    this.InitW(m_nInitAckTimeout);
                    this.WaitProcessCompleted(m_nInitAckTimeout);
                    this.WaitChangeModeCompleted(m_nInitAckTimeout);
                }
                else
                {
                    this.ResetChangeModeCompleted();
                    this.InitW(m_nInitAckTimeout);
                    this.WaitProcessCompleted(m_nInitAckTimeout);
                }

                SpinWait.SpinUntil(() => false, 500);

                this.StimW(m_nInitAckTimeout);

                this.ResetInPos();
                this.OrgnW(m_nAckTimeout);
                this.WaitInPos(m_nOrgTimeout);

                for (int i = 0; i < m_strTowerEnable.Length; i++)//清除鎖定
                    m_strOrder[i] = new bool[TheTowerSlotNumber];


                OnORGNComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
                OnORGNComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
                OnORGNComplete?.Invoke(this, false);
            }
        }
        private void ExeRsta(int nMode)
        {
            try
            {
                WriteLog(string.Format("ExeRsta Start"));
                this.ResetW(m_nAckTimeout, nMode);
            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
            }
        }
        private void ExeWMAP()//1~400 ex:oSTK1.WMAP(30,0,1)
        {
            try
            {
                WriteLog(string.Format("ExeWMAP Start"));

                for (int i = 0; i < TowerCount; i++)//要mapping四面
                {
                    if (TowerEnable(i) == false) continue;

                    int nStageForMapping = TheTowerStgeNumber > 10 ? (StageNo + 20 * i) : (StageNo + 10 * i);
                    this.ResetInPos();
                    this.WmapW(m_nAckTimeout, nStageForMapping, 0, 1);//每一面總共要問8/16次gmap
                    this.WaitInPos(m_nMapTimeout);

                    if (Simulate) SpinWait.SpinUntil(() => false, 1000);

                    string strMappingDataOneTower = "";
                    for (int j = 0; j < TheTowerStgeNumber; j++)//總共要問8/16次
                    {
                        int nStge1to400 = nStageForMapping + j + 1;//下10會對11~18
                        this.GmapW(m_nAckTimeout, nStge1to400);

                        #region 在mapping完成後會建立資料，容器是 Waferlist

                        List<SWafer> listWafer = new List<SWafer>();
                        string strMappingData = m_MappingData[nStge1to400];
                        strMappingDataOneTower += strMappingData;//每次25個疊加
                        int nTower0to15 = StgNum1to400Transfer0to15(nStge1to400);
                        for (int k = 0; k < strMappingData.Length; k++)
                        {
                            SWafer.enumPosition ePos = m_eTowerFirst + StgNum1to400Transfer0to3(nStge1to400);
                            SWafer wafer = null;
                            if (strMappingData[k] != '0')
                            {
                                wafer = new SWafer("", "lotID", "CJID-", "PJID-", "RECIPE", k + 1,
                                    WaferType,
                                    (SWafer.enumPosition)StgNum1to400Transfer0to15(nStge1to400),
                                    SWafer.enumFromLoader.Tower01 + nTower0to15,
                                    strMappingData[k] == '1' ? SWafer.enumProcessStatus.Sleep : SWafer.enumProcessStatus.Error);
                            }
                            listWafer.Add(wafer);
                        }

                        if (m_WaferData.ContainsKey(nStge1to400)) { m_WaferData[nStge1to400] = listWafer; }
                        else { m_WaferData.Add(nStge1to400, listWafer); }

                        #endregion
                    }

                    if (strMappingDataOneTower.Length == TheTowerSlotNumber)//防呆
                        OnMappingComplete?.Invoke(this, new TowerGMAP_EventArgs(i, strMappingDataOneTower));//以200/400片更新
                }

                //委派更新GUI
                string strMappingDataAll = GetMapDataAll();
                if (strMappingDataAll.Length == TheTowerSlotNumber * TowerCount)//防呆
                    OnMappingCompleteAll?.Invoke(this, strMappingDataAll);//全部一起更新

            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
                OnMappingError?.Invoke(this, new EventArgs());//全部一起更新
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
                OnMappingError?.Invoke(this, new EventArgs());//全部一起更新
            }
        }
        private void ExeGetData()
        {
            try
            {
                WriteLog(string.Format("ExeGetData Start"));

                //預留

                OnGetDataComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
                OnGetDataComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
                OnGetDataComplete?.Invoke(this, false);
            }
        }
        private void ExeLOAD(int nStg, int nSlot)
        {
            bool bSuc = false;
            try
            {
                WriteLog(string.Format("ExeLOAD Stg:{0},Slot:{1}", nStg, nSlot));

                this.ResetInPos();
                this.LoadW(m_nAckTimeout, nStg, nSlot, 0);
                this.WaitInPos(m_nMotionTimeout);

                bSuc = true;
            }
            catch (SException ex)
            {
                WriteLog(string.Format("SException : {0}", ex));
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
            }
            OnLOADComplete?.Invoke(this, bSuc);
        }
        private void ExeHOME(int nStg)
        {
            bool bSuc = false;
            try
            {
                WriteLog(string.Format("ExeHOME Stg:{0}", nStg));
                this.ResetInPos();
                this.HomeW(m_nAckTimeout, nStg);
                this.WaitInPos(m_nMotionTimeout);
                bSuc = true;
            }
            catch (SException ex) { WriteLog(string.Format("SException : {0}", ex)); }
            catch (Exception ex) { WriteLog(string.Format("Exception : {0}", ex)); }
            OnHOMEComplete?.Invoke(this, bSuc);
        }

        #endregion
        //=======================================================================
        #region =========================== ORGN =======================================
        private void Orgn(int nVariable)
        {
            _signalAck[enumTowerCommand.Orgn].Reset();
            //m_StockIcon.SendCommand(string.Format("ORGN(" + nVariable + ")"));
            m_StockIcon.SendCommand(BodyNo, string.Format("ORGN"));
        }
        public void OrgnW(int nTimeout, int nVariable = 0)
        {
            enumTowerCommand eComand = enumTowerCommand.Orgn;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signals[enumTowerSignalTable.MotionCompleted].Reset();
                Orgn(nVariable);
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== HOME =======================================
        private void Home(int nStg, int nSlot, int nFlag)
        {
            _signalAck[enumTowerCommand.Home].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("HOME({0},{1},{2})", nStg, nSlot, nFlag));
        }
        public void HomeW(int nTimeout, int nStg, int nSlot, int nFlag)
        {
            enumTowerCommand eComand = enumTowerCommand.Home;
            _signalSubSequence.Reset();
            if (nStg == 0)
            {

            }
            if (!Simulate)
            {
                if (nStg == 0)
                {

                }

                _signals[enumTowerSignalTable.MotionCompleted].Reset();
                Home(nStg, nSlot, nFlag);
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        public void HomeW(int nTimeout, int nStg)//這會回到最上面
        {
            HomeW(nTimeout, nStg, 0, 0);
        }
        public void HomeForTowerSlotW(int nTimeout, int nFaceIndx, int nTowerSlot1to400)//移動到正前方
        {
            int nStg = FaceIndx0to3Slot1to200TransferStgNum(nFaceIndx, nTowerSlot1to400);
            int nSlot = Slot1to200TransferSlot1to25(nTowerSlot1to400);
            HomeW(nTimeout, nStg, nSlot, 0);
        }
        public void HomeForTowerTopW(int nTimeout, int nFaceIndx, int nTowerSlot1to400)//移動到上方
        {
            int nStg = FaceIndx0to3Slot1to200TransferStgNum(nFaceIndx, nTowerSlot1to400);
            int nSlot = Slot1to200TransferSlot1to25(nTowerSlot1to400);
            HomeW(nTimeout, nStg, 0, 0);
        }

        #endregion
        #region =========================== EXTD =======================================
        private void Extd(int nStg, int nSlot)
        {
            _signalAck[enumTowerCommand.Extend].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("EXTD({0},{1})", nStg, nSlot));
        }
        public void ExtdW(int nTimeout, int nStg, int nSlot)
        {
            enumTowerCommand eComand = enumTowerCommand.Extend;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signals[enumTowerSignalTable.MotionCompleted].Reset();
                Extd(nStg, nSlot);
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== LOAD =======================================
        /// <summary>
        /// 開門
        /// </summary>
        /// <param name="nStg"></param>
        /// <param name="nSlot"></param>
        /// <param name="nFlag">0:馬達分開動,1:馬達同動</param>
        private void Load(int nStg, int nSlot, int nFlag)
        {
            _signalAck[enumTowerCommand.Load].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("Load({0},{1},{2})", nStg, nSlot, 1));
        }
        public void LoadW(int nTimeout, int nStg, int nSlot, int nFlag)//Opens the shutter 
        {
            enumTowerCommand eComand = enumTowerCommand.Load;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signals[enumTowerSignalTable.MotionCompleted].Reset();
                Load(nStg, nSlot, nFlag);
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        public void LoadForTowerSlotW(int nTimeout, int nFaceIndx, int nTowerSlot1to400)//Opens the shutter 
        {
            int nStg = FaceIndx0to3Slot1to200TransferStgNum(nFaceIndx, nTowerSlot1to400);
            int nSlot = Slot1to200TransferSlot1to25(nTowerSlot1to400);
            LoadW(nTimeout, nStg, nSlot, 0);
        }
        #endregion
        #region =========================== UNLD =======================================
        /// <summary>
        /// 關門
        /// </summary>
        /// <param name="nFlg">0:馬達分開動,1:馬達同動</param>
        private void Unld(int nFlg)
        {
            _signalAck[enumTowerCommand.Unload].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("Unld({0})", 1));
        }
        public void UnldW(int nTimeout, int nFlg)//Closes the shutter.
        {
            enumTowerCommand eComand = enumTowerCommand.Unload;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signals[enumTowerSignalTable.MotionCompleted].Reset();
                Unld(nFlg);
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== CLMP =======================================
        private void Clmp()
        {
            _signalAck[enumTowerCommand.Clamp].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("CLMP"));
        }
        public void ClmpW(int nTimeout)//Opens the bar for retaining the shutter.
        {
            enumTowerCommand eComand = enumTowerCommand.Clamp;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signals[enumTowerSignalTable.MotionCompleted].Reset();
                Clmp();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== UCLM =======================================
        private void Uclm()
        {
            _signalAck[enumTowerCommand.UnClamp].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("UCLM"));
        }
        public void UclmW(int nTimeout)//Contains the bar for retaining the shutter.
        {
            enumTowerCommand eComand = enumTowerCommand.UnClamp;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signals[enumTowerSignalTable.MotionCompleted].Reset();
                Uclm();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== WMAP =======================================
        private void Wmap(int nStg, int nId, int nFlag)
        {
            _signalAck[enumTowerCommand.Mapping].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("WMAP({0},{1},{2})", nStg, nId, nFlag));
        }
        //1~400 ex:oSTK1.WMAP(30,0,1)
        public void WmapW(int nTimeout, int nStg, int nId, int nFlag)//Performs the mapping operation on the designated stage.
        {
            enumTowerCommand eComand = enumTowerCommand.Mapping;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signals[enumTowerSignalTable.MotionCompleted].Reset();
                Wmap(nStg, nId, nFlag);
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== SPOT =======================================
        private void Spot(int nBit, bool bOn)
        {
            _signalAck[enumTowerCommand.SetIO].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("SPOT({0},{1})", nBit, bOn ? '1' : '0'));
        }
        public void SpotW(int nTimeout, int nBit, bool bOn)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Spot(nBit, bOn);
                if (!_signalAck[enumTowerCommand.SetIO].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumTowerCommand.SetIO]));
                }
                if (_signalAck[enumTowerCommand.SetIO].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumTowerCommand.SetIO]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion =====================================================================

        #region =========================== EVNT =======================================
        private void Event()
        {
            _signalAck[enumTowerCommand.SetEvent].Reset();
            m_StockIcon.SendCommand(BodyNo, "EVNT(0,1)");
        }
        public void EventW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.SetEvent;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Event();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== RSTA =======================================
        private void Reset(int nReset)
        {
            _signalAck[enumTowerCommand.Reset].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("RSTA(" + nReset + ")"));
        }
        public void ResetW(int nTimeout, int nReset = 0)
        {
            enumTowerCommand eComand = enumTowerCommand.Reset;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Reset(nReset);
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== INIT =======================================
        private void Init()
        {
            _signalAck[enumTowerCommand.Initialize].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("INIT"));
        }
        public void InitW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.Initialize;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Init();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== STOP =======================================
        private void Stop()
        {
            _signalAck[enumTowerCommand.Stop].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("STOP"));
        }
        public void StopW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.Stop;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Stop();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== PAUS =======================================
        private void Paus()
        {
            _signalAck[enumTowerCommand.Pause].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("PAUS"));
        }
        public void PausW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.Pause;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Paus();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== MODE =======================================
        private void Mode()
        {
            _signalAck[enumTowerCommand.Mode].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("MODE"));
        }
        public void ModeW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.Mode;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Mode();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== STAT =======================================
        private void Stat()
        {
            _signalAck[enumTowerCommand.Status].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("STAT"));
        }
        public void StatW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.Status;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Stat();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== GPIO =======================================
        private void Gpio()
        {
            _signalAck[enumTowerCommand.GetIO].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("GPIO"));
        }
        public void GpioW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.GetIO;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Gpio();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== GMAP =======================================
        private void Gmap(int nStg)
        {
            _signalAck[enumTowerCommand.GetMappingData].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("GMAP({0})", nStg));
        }
        //23.08.02 16:28:14.924 [STK1] oSTK1.GMAP(31)
        //23.08.02 16:28:14.928 [STK1] aSTK1.GMAP:0000000000000000000000000
        //23.08.02 16:28:14.928 [STK1] oSTK1.GMAP(32)
        //23.08.02 16:28:14.932 [STK1] aSTK1.GMAP:0000000000000000000000000
        //23.08.02 16:28:14.932 [STK1] oSTK1.GMAP(33)
        //23.08.02 16:28:14.936 [STK1] aSTK1.GMAP:1111111111111111111100000
        //23.08.02 16:28:14.936 [STK1] oSTK1.GMAP(34)
        //23.08.02 16:28:14.940 [STK1] aSTK1.GMAP:0000000000000000000000000
        //23.08.02 16:28:14.940 [STK1] oSTK1.GMAP(35)
        //23.08.02 16:28:14.944 [STK1] aSTK1.GMAP:0000000000000000000000000
        //23.08.02 16:28:14.944 [STK1] oSTK1.GMAP(36)
        //23.08.02 16:28:14.949 [STK1] aSTK1.GMAP:0000000000000000000000000
        //23.08.02 16:28:14.949 [STK1] oSTK1.GMAP(37)
        //23.08.02 16:28:14.953 [STK1] aSTK1.GMAP:0000000000000000000000000
        //23.08.02 16:28:14.953 [STK1] oSTK1.GMAP(38)
        //23.08.02 16:28:14.957 [STK1] aSTK1.GMAP:0000000000000000000000000
        public void GmapW(int nTimeout, int nStg)//Acquires the mapping pattern.
        {
            enumTowerCommand eComand = enumTowerCommand.GetMappingData;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Gmap(nStg);
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            else
            {
                //slot1->slot25
                if (nStg == 11) m_strCurrentMappingData = "1000000000000000000000000";
                //else if (nStg > 11 && nStg < 17) m_strCurrentMappingData = "1111111111111111111111111";
                //else if (nStg == 18) m_strCurrentMappingData = "0011111110000000000000000";
                //else if (nStg == 26) m_strCurrentMappingData = "0000000000000000000011111";
                //else if (nStg == 28) m_strCurrentMappingData = "1111000000000000000001111";
                //else if (nStg == 38) m_strCurrentMappingData = "1111110000000000000111111";
                //else if (nStg == 48) m_strCurrentMappingData = "1111111100000000011111111";
                //else if (nStg == 58) m_strCurrentMappingData = "1111111110000001111111111";
                //else if (nStg == 68) m_strCurrentMappingData = "1111111111100111111111111";
                //else if (nStg == 78) m_strCurrentMappingData = "1111111111111111111111111";
                //else if (nStg == 91) m_strCurrentMappingData = "0000000000000000000000011";
                else if (nStg == 21) m_strCurrentMappingData = "0000000000000000000000000";
                else m_strCurrentMappingData = "0000000000000000000000000";
            }

            //if (true)//mapping沒調好要先屏蔽
            //    m_strCurrentMappingData = "0000000000000000000000000";



            if (m_MappingData.ContainsKey(nStg))  //1~25
            {
                m_MappingData[nStg] = m_strCurrentMappingData;
            }
            else
            {
                m_MappingData.Add(nStg, m_strCurrentMappingData);
            }

            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== GVER =======================================
        private void Gver()
        {
            _signalAck[enumTowerCommand.GetIO].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("GVER"));
        }
        public void GverW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.GetVersion;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Gver();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== GLOG =======================================
        private void Glog()
        {
            _signalAck[enumTowerCommand.GetLog].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("GLOG"));
        }
        public void GlogW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.GetLog;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Glog();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion
        #region =========================== STIM =======================================
        private void Stim()
        {
            _signalAck[enumTowerCommand.SetDateTime].Reset();
            m_StockIcon.SendCommand(BodyNo, "STIM(" + DateTime.Now.ToString("yyyy, MM, dd, HH, mm, ss") + ")");
        }
        public void StimW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.SetDateTime;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Stim();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== GPOS =======================================
        private void Gpos()
        {
            _signalAck[enumTowerCommand.GetPos].Reset();
            m_StockIcon.SendCommand(BodyNo, string.Format("GPOS"));
        }
        public void GposW(int nTimeout)
        {
            enumTowerCommand eComand = enumTowerCommand.GetPos;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Gpos();
                if (!_signalAck[eComand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.AckTimeout);
                    throw new SException((int)enumTowerCustomError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[eComand]));
                }
                if (_signalAck[eComand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.SendCommandFailure);
                    throw new SException((int)enumTowerCustomError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[eComand]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion

        public void ResetChangeModeCompleted()
        {
            _signals[enumTowerSignalTable.Remote].Reset();
        }
        public void WaitChangeModeCompleted(int nTimeout)
        {
            if (!Simulate)
            {
                if (!_signals[enumTowerSignalTable.Remote].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.InitialFailure);
                    throw new SException((int)enumTowerCustomError.InitialFailure, string.Format("Wait Mode was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumTowerSignalTable.Remote].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.InitialFailure);
                    throw new SException((int)enumTowerCustomError.InitialFailure, string.Format("Motion is Mode end."));
                }
            }
        }
        public void ResetProcessCompleted()
        {
            _signals[enumTowerSignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = true;
        }
        public void WaitProcessCompleted(int nTimeout)
        {
            if (!Simulate)
            {
                if (!_signals[enumTowerSignalTable.ProcessCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.ProcessFlagTimeout);
                    throw new SException((int)enumTowerCustomError.ProcessFlagTimeout, string.Format("Wait process flag complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumTowerSignalTable.ProcessCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.ProcessFlagAbnormal);
                    throw new SException((int)enumTowerCustomError.ProcessFlagAbnormal, string.Format("Wait process flag complete was failure. [Timeout = {0} ms]", nTimeout));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 500);
            }
        }
        public void ResetInPos()
        {
            _signals[enumTowerSignalTable.MotionCompleted].Reset();
            m_bMoving = true;
        }
        public void WaitInPos(int nTimeout)
        {
            if (!Simulate)
            {
                //motion complete
                if (!_signals[enumTowerSignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumTowerCustomError.MotionTimeout);
                    throw new SException((int)enumTowerCustomError.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumTowerSignalTable.MotionCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumTowerCustomError.MotionAbnormal);
                    throw new SException((int)enumTowerCustomError.MotionAbnormal, string.Format("Wait process flag complete was failure. [Timeout = {0} ms]", nTimeout));
                }
                m_bMoving = false;
            }
            else
            {
                m_bMoving = false;
                SpinWait.SpinUntil(() => false, 500);
            }
        }


        /// <summary>
        /// BodyNo:1->Tower01,Tower02,Tower03,Tower04
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns>Tower01,Tower02,Tower03,Tower04</returns>
        public string TowerName(int nIndex)
        {
            string strID = string.Format("Tower{0:D2}", TowerCount * (BodyNo - 1) + nIndex + 1);
            return strID;
        }
        /// <summary>
        /// Robot take wafer out.
        /// </summary>
        /// <param name="nStge1to400">stage number 1~400</param>
        /// <param name="nSlot1to25">slot number 1~25</param>
        /// <returns>wafer data</returns>
        /// <remarks>nStge:1~400, slot:1~25</remarks>
        public SWafer TakeWaferOut(int nStge1to400, int nSlot1to25)//Robot從Tower取出
        {
            SWafer wafer = null;
            if (m_WaferData.ContainsKey(nStge1to400) && m_MappingData.ContainsKey(nStge1to400))
            {
                wafer = m_WaferData[nStge1to400][nSlot1to25 - 1];
                m_WaferData[nStge1to400][nSlot1to25 - 1] = null;

                string str = m_MappingData[nStge1to400];
                str = str.Remove(nSlot1to25 - 1, 1);
                str = str.Insert(nSlot1to25 - 1, "0");
                m_MappingData[nStge1to400] = str;
                #region 解除位置鎖定
                int nFaceIndx = StgNum1to400Transfer0to3(nStge1to400);
                int nTowerSlot = StgNum1to400Slot1to25TransferTowerSlot(nStge1to400, nSlot1to25);
                SetSlotOrder(nFaceIndx, nTowerSlot, false);
                #endregion
                OnTakeWaferOutFoup?.Invoke(nStge1to400, nSlot1to25);//stage slot
                return wafer;
            }
            return null;
        }
        /// <summary>
        /// Robot take wafer out.
        /// </summary>
        /// <param name="nFaceIndx">index 0,1,2,3</param>
        /// <param name="nTowerSlot">slot number 1~200/400</param>
        /// <returns>wafer data</returns>
        /// <remarks>index:0~3, slot:1~200/400</remarks>
        public SWafer TakeWaferOut2(int nFaceIndx, int nTowerSlot)//Robot從Tower取出
        {
            //body:1_10
            int nStge1to400 = FaceIndx0to3Slot1to200TransferStgNum(nFaceIndx, nTowerSlot);
            int nSlot1to25 = Slot1to200TransferSlot1to25(nTowerSlot);
            SWafer wafer = TakeWaferOut(nStge1to400, nSlot1to25);
            return wafer;
        }
        /// <summary>
        /// Robot take wafer in.
        /// </summary>
        /// <param name="nStge1to400">stage number 1~400</param>
        /// <param name="nSlot1to25">slot number 1~25</param>
        /// <param name="wafer">wafer data</param>
        /// <remarks>nStge:1~400, slot:1~25</remarks>
        public void TakeWaferIn(int nStge1to400, int nSlot1to25, SWafer wafer)//Robot放到Tower裡面
        {
            if (m_WaferData.ContainsKey(nStge1to400) && m_MappingData.ContainsKey(nStge1to400))
            {
                //Tower1 11,12,13,14...| 11~26
                //Tower2 21,22,23,24...| 31~46
                //Tower3 31,32,33,34...| 51~66
                //Tower4 41,42,43,44...| 71~86

                int nIndexFace = StgNum1to400Transfer0to3(nStge1to400);
                int nTowerSlot = StgNum1to400Slot1to25TransferTowerSlot(nStge1to400, nSlot1to25);//1~200/400

                int nTower0to15 = StgNum1to400Transfer0to15(nStge1to400);

                wafer.Position = enumPosition.Tower01 + nTower0to15;
                wafer.SetOwner(enumFromLoader.Tower01 + nTower0to15);

                wafer.Slot = nTowerSlot;//重新定義位置

                m_WaferData[nStge1to400][nSlot1to25 - 1] = wafer;

                string str = m_MappingData[nStge1to400];
                str = str.Remove(nSlot1to25 - 1, 1);
                str = str.Insert(nSlot1to25 - 1, "1");
                m_MappingData[nStge1to400] = str;
                #region 解除位置鎖定                   
                SetSlotOrder(nIndexFace, nTowerSlot, false);
                #endregion

                OnTakeWaferInFoup?.Invoke(nStge1to400, nSlot1to25);//stage slot
            }
        }


        /// <summary>
        /// Stage:1~400
        /// </summary>
        /// <param name="nStge1to400">Stage:1~400</param>
        /// <returns>Tower Indx:0~15</returns>
        private int StgNum1to400Transfer0to15(int nStge1to400)//Calculate
        {
            int nIndx0to15 = -1;
            int nFaceIndx = StgNum1to400Transfer0to3(nStge1to400);
            if (nFaceIndx != -1)
                nIndx0to15 = (BodyNo - 1) * TowerCount + nFaceIndx;
            return nIndx0to15;
        }
        /// <summary>
        /// Stage:1~400
        /// </summary>
        /// <param name="nStge1to400">Stage:1~400</param>
        /// <returns>FaceIndex:0~3</returns>
        private int StgNum1to400Transfer0to3(int nStge1to400)//Calculate
        {
            int nFaceIndx = -1;
            if (TheTowerStgeNumber < 10)//200片25一組:11~18,21~28,31~38,41~48 => 0,1,2,3
                nFaceIndx = (nStge1to400 - StageNo) / 10;
            else if (TheTowerStgeNumber < 20)//400片25一組:11~26,31~46,51~66,71~86 => 0,1,2,3
                nFaceIndx = (nStge1to400 - StageNo) / 20;
            return nFaceIndx;
        }
        /// <summary>
        /// FaceIndx0~3 Slot1~200/400
        /// </summary>
        /// <param name="nFaceIndx">FaceIndex:0~3</param>
        /// <param name="nTowerSlot">Slot:1~200/400</param>
        /// <returns>Stage:1~400</returns>
        private int FaceIndx0to3Slot1to200TransferStgNum(int nFaceIndx, int nTowerSlot)//Calculate
        {
            int nStgNum1to400 = -1;
            if (TheTowerStgeNumber < 10)//200片25一組:11~18,21~28,31~38,41~48
                nStgNum1to400 = StageNo + 1 + nFaceIndx * 10 + (nTowerSlot - 1) / m_nSeveralSlotsInTheArea;
            else if (TheTowerStgeNumber < 20)//400片25一組:11~26,31~46,51~66,71~86
                nStgNum1to400 = StageNo + 1 + nFaceIndx * 20 + (nTowerSlot - 1) / m_nSeveralSlotsInTheArea;
            return nStgNum1to400;
        }
        /// <summary>
        /// Slot:1~200or400
        /// </summary>
        /// <param name="nTowerSlot">Slot:1~200or400</param>
        /// <returns>Slot:1~25</returns>
        private int Slot1to200TransferSlot1to25(int nTowerSlot)//Calculate
        {
            return (nTowerSlot - 1) % m_nSeveralSlotsInTheArea + 1;
        }
        /// <summary>
        /// Stage:1~400,Slot:1~25
        /// </summary>
        /// <param name="nStge1to400">Stage:1~400</param>
        /// <param name="nSlot1to25">Slot:1~25</param>
        /// <returns>Slot:1~200/400</returns>
        private int StgNum1to400Slot1to25TransferTowerSlot(int nStge1to400, int nSlot1to25)//Calculate
        {
            int nTowerSlot = -1;
            if (TheTowerStgeNumber < 10)//200片25一組:11~18,21~28,31~38,41~48
                nTowerSlot = ((nStge1to400 - StageNo) % 10 - 1) * m_nSeveralSlotsInTheArea + nSlot1to25;
            else if (TheTowerStgeNumber < 20)//400片25一組:11~26,31~46,51~66,71~86
                nTowerSlot = ((nStge1to400 - StageNo) % 20 - 1) * m_nSeveralSlotsInTheArea + nSlot1to25;
            return nTowerSlot/*(nStge1to400 % 10 - 1) * 25 + nSlot1to25*/;
        }
        /// <summary>
        /// Slot:1~800or1600
        /// </summary>
        /// <param name="nStockSlot">Slot:1~800or1600</param>
        /// <returns>FaceIndex:0~3</returns>
        private int StockSlotTransfer0to3(int nStockSlot)//Calculate
        {
            return (nStockSlot - 1) / TheTowerSlotNumber;//1,2,3,4
        }

        public int GetStockerTotalSlot()
        {
            int nTotalSlot = m_strTowerEnable.Length * TheTowerSlotNumber;
            return nTotalSlot;
        }


        //Interlock Status       
        public bool IsAreaSesorTrigger()
        {
            //return false;
            //true應該是正常
            if (Simulate) return false;
            bool b1 = (GPIO.GetDIList[TowerGPIO.enumTowerDI.Robot_Check] == false);
            return b1;
        }
        public bool IsWaferExist()
        {
            bool b1 = (GPIO.GetDIList[TowerGPIO.enumTowerDI.Z_Warpage_2] == true);
            return b1;
        }
        public bool IsOpenDetectFrame()
        {
            //開門狀況下必須有Frame，滑落會偵測到
            bool b1 = (GPIO.GetDIList[TowerGPIO.enumTowerDI.CST_Tilt_L] == true);
            bool b2 = (GPIO.GetDIList[TowerGPIO.enumTowerDI.CST_Tilt_R] == true);
            return b1 && b2;
        }
        public bool IsOpenZaxisDownSensor()
        {
            //開門狀況下必須偵測到Zaxis在下定位
            //bool b1 = (GPIO.GetDIList[TowerGPIO.enumTowerDI.L_UD_CY_2] == true);
            //bool b2 = (GPIO.GetDIList[TowerGPIO.enumTowerDI.R_UD_CY_2] == true);
            bool b3 = SpinWait.SpinUntil(() =>
            GPIO.GetDIList[TowerGPIO.enumTowerDI.L_UD_CY_2] && GPIO.GetDIList[TowerGPIO.enumTowerDI.R_UD_CY_2], 1000);
            return b3;
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




    }
}
