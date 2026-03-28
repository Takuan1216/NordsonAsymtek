using Rorze.Equipments.Unit;
using RorzeApi;
using RorzeApi.Class;
using RorzeAPI;
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.RC500;
using RorzeUnit.Class.RC500.RCEnum;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Class.Robot.Event;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Documents;
using static Rorze.Equipments.Unit.SRobot;
using static RorzeApi.Class.MotionEventManager;
using static RorzeUnit.Class.SWafer;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using RobotPos = RorzeUnit.Class.Robot.Enum.RobotPos;

namespace RorzeUnit.Class.Robot
{
    public class SSRobotRR75x : I_Robot
    {
        //==============================================================================
        #region =========================== private ============================================
        private bool m_bSimulate;
        private enumRobotMode m_eStatMode;       //記憶的STAT S1第1 bit
        private bool m_bStatOrgnComplete;        //記憶的STAT S1第2 bit
        private bool m_bStatProcessed;           //記憶的STAT S1第3 bit
        private enumRobotStatus m_eStatInPos;    //記憶的STAT S1第4 bit
        private int m_nSpeed;                    //記憶的STAT S1第5 bit
        private List<string> m_strRecordsStatErr = new List<string>();     //記憶的STAT S2

        enumRC550Axis m_eXAX1 = enumRC550Axis.AXS1;

        protected int m_nInitAckTimeout = 60000 * 1;
        protected int m_nAckTimeout = 5000;
        protected int m_nMotionTimeout = 60000;
        private string m_strMappingData;
        private bool _bManualMoving = false;
        private bool m_bProcessStart = false;
        private bool m_bExtXaxisDisable = true;
        private SLogger _logger = SLogger.GetLogger("CommunicationLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[TRB{0}] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        private sRorzeSocket m_Socket;

        private SWafer _waferUpper;
        private SWafer _waferLower;

        private object _objLockQueue = new object();
        private object m_lockExtandFlag = new object();

        private enumArmFunction _upperArmFunc = enumArmFunction.NONE;//Finger type
        private enumArmFunction _lowerArmFunc = enumArmFunction.NONE;//Finger type
        private int m_nFrameArmBackPulse = 0;

        private bool[] m_AllowPort, m_AllowAligner, m_AllowEquipment;
        private I_BarCode m_Barcode;

        private string[] _Rac2Data = { "00000000", "00000000", "00000000" };//robot mapping

        private string[] m_strDAPM_Ack;
        #endregion
        //==============================================================================
        #region =========================== public =============================================
        public bool ExtXaxisDisable { get { return m_bExtXaxisDisable; } }
        public bool Connected { get; private set; }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }
        public bool XaxsDisable { get; private set; }
        public string VersionData { get; private set; }
        public bool UseArmSameMovement { get; private set; }

        //STAT S1第1 bit
        public enumRobotMode StatMode { get { return m_eStatMode; } }
        public bool IsInitialized { get { return m_eStatMode == enumRobotMode.Remote; } }
        //STAT S1第2 bit
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }
        //STAT S1第3 bit
        public bool IsProcessing { get { return m_bStatProcessed; } }
        //STAT S1第4 bit
        public enumRobotStatus InPos { get { return m_eStatInPos; } }
        public bool IsMoving { get { return m_eStatInPos == enumRobotStatus.Moving || TBL_560.IsMoving; } }
        //STAT S1第5 bit
        public int GetSpeed { get { return m_nSpeed; } }
        //STAT S2
        public bool IsError { get { return m_strRecordsStatErr.Count != 0; } }

        public SRR757GPIO GPIO { get; protected set; }                      //GPIO
        public int GetAckTimeout { get { return m_nAckTimeout; } }          //Timeout
        public int GetMotionTimeout { get { return m_nMotionTimeout; } }    //Timeout
        public object objLockQueue { get { return _objLockQueue; } }
        public enumArmFunction UpperArmFunc { get { return _upperArmFunc; } }//Finger type
        public enumArmFunction LowerArmFunc { get { return _lowerArmFunc; } }//Finger type
        public int FrameArmBackPulse { get { return m_nFrameArmBackPulse; } }
        public string[] DEQUData { get; set; }
        public string[] DMPRData { get; set; }
        public string[] DRCIData { get; set; }
        public string[] DRCSData { get; set; }
        public string[] DTRBData { get; set; }
        public string[] DTULData { get; set; }
        public string[] DCFGData { get; set; }
        public Dictionary<int, string[]> DAPMData { get; set; } = new Dictionary<int, string[]>();
        public string[] GetRac2Data { get { return _Rac2Data; } }//robot mapping

        public bool ProcessStart { get { return m_bProcessStart; } set { m_bProcessStart = value; } }
        public string GetMappingData { get { return m_strMappingData; } }
        public SWafer UpperArmWafer
        {
            get
            {
                return _waferUpper;
            }
            set
            {
                if (value == null)
                {
                    if (OnLeaveUpperArmWaferData != null && _waferUpper != null)
                        OnLeaveUpperArmWaferData(this, new WaferDataEventArgs(_waferUpper));

                    if (_waferUpper != null && _waferUpper.ProcessStatus == SWafer.enumProcessStatus.Processed)
                    {

                        if (OnWaferMeasureEnd != null)
                            OnWaferMeasureEnd(this, new WaferDataEventArgs(_waferUpper));


                        if (OnWaferEnd != null)
                            OnWaferEnd(this, new WaferDataEventArgs(_waferUpper));
                    }
                    _waferUpper = value;
                }
                else
                {
                    _waferUpper = value;
                    if (_waferUpper.ProcessStatus == SWafer.enumProcessStatus.Processing && _waferUpper.Position > (SWafer.enumPosition)0 && _waferUpper.Position <= (SWafer.enumPosition)4)
                    {
                        if (OnWaferStart != null)
                            OnWaferStart(this, new WaferDataEventArgs(_waferUpper));
                    }
                    if (OnAssignUpperArmWaferData != null && _waferUpper != null)
                        OnAssignUpperArmWaferData(this, new WaferDataEventArgs(_waferUpper));
                }
                if (UpperArmWaferChange != null)
                    UpperArmWaferChange(this, new WaferDataEventArgs(_waferUpper));//更新UI
            }
        }
        public SWafer LowerArmWafer
        {
            get { return _waferLower; }
            set
            {
                //  No Wafer
                if (value == null)
                {
                    if (OnLeaveLowerArmWaferData != null && _waferLower != null)
                        OnLeaveLowerArmWaferData(this, new WaferDataEventArgs(_waferLower));

                    if (_waferLower != null && _waferLower.ProcessStatus == SWafer.enumProcessStatus.Processed)
                    {

                        if (OnWaferMeasureEnd != null)
                            OnWaferMeasureEnd(this, new WaferDataEventArgs(_waferLower));


                        if (OnWaferEnd != null)
                            OnWaferEnd(this, new WaferDataEventArgs(_waferLower));
                    }
                    _waferLower = value;
                }
                else //  Load Wafer
                {
                    _waferLower = value;
                    if (_waferLower.ProcessStatus == SWafer.enumProcessStatus.Processing && _waferLower.Position > (SWafer.enumPosition)0 && _waferLower.Position <= (SWafer.enumPosition)4)
                    {
                        if (OnWaferStart != null)
                            OnWaferStart(this, new WaferDataEventArgs(_waferLower));
                    }
                    if (OnAssignLowerArmWaferData != null && _waferLower != null)
                        OnAssignLowerArmWaferData(this, new WaferDataEventArgs(_waferLower));
                }

                if (LowerArmWaferChange != null)
                    LowerArmWaferChange(this, new WaferDataEventArgs(_waferLower));
            }


        }

        public SWafer PrepareUpperWafer { get; set; }
        public SWafer PrepareLowerWafer { get; set; }

        public ConcurrentQueue<SWafer> queCommand { get; set; }
        public ConcurrentQueue<SWafer> quePreCommand { get; set; }
        public SRR757Axis UpperArm { get; set; }
        public SRR757Axis LowerArm { get; set; }
        public SRR757Axis Rotater { get; set; }
        public SRR757Axis Lifter { get; set; }
        public SRR757Axis Lifter2 { get; set; }
        public SRR757Axis Traverse { get; set; }
        public SSRC560_Motion TBL_560 { get; set; }

        public I_BarCode Barcode { get { return m_Barcode; } }

        public bool PinExtend { get; private set; } = false;//2024.4.10針對NPD TRB2新增
        public bool PinSafety { get; private set; } = true;//2024.4.10針對NPD TRB2新增

        public bool EnableMap { get { return DEQUData == null ? false : DEQUData[15] != ((int)enumDEQU_15_waferSearch.None).ToString(); } }

        public enumDEQU_15_waferSearch MappingType { get; private set; }
        public bool EnableUpperAlignment { get; private set; }
        public bool EnableLowerAlignment { get; private set; }

        #endregion
        //==============================================================================
        #region =========================== Event ==============================================
        public event EventHandler<WaferDataEventArgs> OnAssignUpperArmWaferData;
        public event EventHandler<WaferDataEventArgs> OnAssignLowerArmWaferData;

        public event EventHandler<WaferDataEventArgs> OnLeaveUpperArmWaferData;
        public event EventHandler<WaferDataEventArgs> OnLeaveLowerArmWaferData;

        public event EventHandler<WaferDataEventArgs> UpperArmWaferChange;  //  手臂上 Wafer 取或放
        public event EventHandler<WaferDataEventArgs> LowerArmWaferChange;  //  手臂上 Wafer 取或放

        public event EventHandler<LoadUnldEventArgs> OnLoadExchangeComplete;//load finish
        public event EventHandler<LoadUnldEventArgs> OnLoadComplete;//load finish
        public event EventHandler<LoadUnldEventArgs> OnUnldComplete;//unld finish

        public event EventHandler<bool> OnArmExtendShiftWaferComplete;//Demo用於旋轉Panel完成

        public event EventHandler OnProcessStart;
        public event EventHandler OnProcessEnd;
        public event EventHandler OnProcessAbort;

        public event EventHandler<bool> OnManualCompleted;
        public event EventHandler<bool> OnORGNComplete;
        public event EventHandler<bool> OnPAUSComplete;
        public event EventHandler<bool> OnRSTAComplete;
        public event EventHandler<bool> OnSTOPComplete;
        public event EventHandler<bool> OnMODEComplete;
        public event EventHandler<bool> OnJobFunctionCompleted;//Step
        public event EventHandler<bool> OnExtdFunctionCompleted;//Absolute
        public event EventHandler<bool> OnSSPDComplete;
        public event EventHandler<bool> OnWmapFunctionCompleted;//mapping
        public event EventHandler<bool> OnGetTeachDataCompleted;
        public event EventHandler<bool> OnSetTeachDataCompleted;
        public event EventHandler<bool> OnGetDmprDataCompleted;//robot mapping
        public event EventHandler<bool> OnSetDmprDataCompleted;//robot mapping
        public event EventHandler<bool> OnHOMEComplete;

        public event EventHandler<bool> OnGetAlignmentDataCompleted;//Alignment
        public event EventHandler<bool> OnSetAlignmentDataCompleted;//Alignment





        public event AutoProcessingEventHandler DoManualProcessing;
        public event AutoProcessingEventHandler DoAutoProcessing;

        public event EventHandler<WaferDataEventArgs> OnWaferStart;
        public event EventHandler<WaferDataEventArgs> OnWaferEnd;
        public event EventHandler<WaferDataEventArgs> OnWaferMeasureEnd;

        public event MessageEventHandler OnReadData;

        public event OccurErrorEventHandler OnOccurStatErr;
        public event OccurErrorEventHandler OnOccurCancel;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public event OccurErrorEventHandler OnOccurErrorRest;

        public event SRR757IOChangelHandler OnIOChange;

        public event EventHandler<string> OnNotifyVibration;
        #endregion
        #region =========================== Thread =============================================
        private SInterruptOneThread _threadManualFunc;

        private SInterruptOneThread _threadEvnt;
        private SInterruptOneThread _threadOrgn;
        private SInterruptOneThread _threadPause;
        private SInterruptOneThreadINT _threadReset;
        private SInterruptOneThread _threadStop;
        private SInterruptOneThreadINT _threadMode;
        private SInterruptOneThreadobkj_INT _threadRobotJog;
        private SInterruptOneThreadobkj_INT _threadRobotExtd;
        private SInterruptOneThreadINT _threadWmap;
        private SInterruptOneThreadINT _threadSpeed;
        private SInterruptOneThreadINT _threadGetTeachData;
        private SInterruptOneThreadINT _threadSetTeachData;
        private SInterruptOneThreadINT _threadGetDMPRData;
        private SInterruptOneThreadINT _threadSetDMPRData;
        private SInterruptOneThread _threadHome;
        private SInterruptOneThreadINT _threadGetDAPMData;
        private SInterruptOneThreadINT _threadSetDAPMData;

        private SPollingThread _pollingAuto;
        private SPollingThread _exePolling;
        #endregion
        #region =========================== Delegate ===========================================
        //  Robot 與其他unit之間的Interlock
        private dlgb_o_o_o_o[] m_pStageInterlock;  //  委派
        public void AddInterlock(int nStg0to399, dlgb_o_o_o_o interlock) { m_pStageInterlock[nStg0to399] = interlock; }

        public dlgb_v LoadEQ_BeforeOK { get; set; }   //手臂取Wafer前要做的事情
        public dlgb_v LoadEQ_AfterOK { get; set; }    //手臂取Wafer後要做的事情
        public dlgb_v UnldEQ_BeforeOK { get; set; }   //手臂放Wafer前要做的事情
        public dlgb_v UnldEQ_AfterOK { get; set; }    //手臂放Wafer後要做的事情
        public dlgb_v GetEQExtendFlag { get; set; }//true手臂伸入EQ
        public dlgv_b SetEQExtendFlag { get; set; }//true手臂伸入EQ
        public dlgn_o_n GetFromLoaderStagIndx { get; set; }//獲取stage indx 0~399
        public dlgn_o_n GetPositionStagIndx { get; set; }//獲取stage indx 0~399

        public dlgb_Enum DlgPanelMisalign { get; set; }//委派外層

        public bool _IsPanelMisalign(enumPosition e) { return DlgPanelMisalign != null && DlgPanelMisalign(e); }
        #endregion
        //==============================================================================

        RobotPos m_CurrePos;
        public RobotPos GetCurrePos { get { return m_CurrePos; } }
        public RobotPos SetCurrePos { set { m_CurrePos = value; } }

        /// <summary>
        /// 找可用的Arm (無Wafer 且 進出貨專用參數符合)
        /// Return "Both Arm" 表示無可用的Arm
        /// </summary>
        /// <param name="armFunc"></param>
        /// <returns></returns>
        public enumRobotArms GetAvailableArm(enumArmFunction armFunc)
        {
            //  找上臂有沒有空
            if (UpperArmFunc == armFunc && this.UpperArmWafer == null)
            {
                if (m_bSimulate || GPIO.DI_UpperPresence1 == false)
                    return enumRobotArms.UpperArm;
            }
            //  找下臂有沒有空
            if (LowerArmFunc == armFunc && this.LowerArmWafer == null)
            {
                if (m_bSimulate || GPIO.DI_LowerPresence1 == false)
                    return enumRobotArms.LowerArm;
            }
            //找不到
            return enumRobotArms.Empty;
        }

        //==============================================================================
        public SSRobotRR75x(string IP, int PortID, int nBodyNo, bool bDisable, bool bSimulate, bool bXDisable,
            enumArmFunction UpperArmFunc,
            enumArmFunction LowerArmFunc,
            int nFrameArmBackPulse,
            bool bUseArmSameMovement,
            string strAllowPort,
            string strAllowAligner,
            string strAllowEquipment,
            I_BarCode barcode,
            SSRC560_Motion xAxis560 = null,
            sServer sever = null)
        {
            Disable = bDisable;
            XaxsDisable = bXDisable;
            m_bSimulate = bSimulate;
            BodyNo = nBodyNo;
            m_Barcode = barcode;
            m_AllowPort = new bool[strAllowPort.Length];
            for (int i = 0; i < strAllowPort.Length; i++) { m_AllowPort[i] = strAllowPort[i] == '1'; }
            m_AllowAligner = new bool[strAllowAligner.Length];
            for (int i = 0; i < strAllowAligner.Length; i++) { m_AllowAligner[i] = strAllowAligner[i] == '1'; }
            //m_AllowEquipment = new bool[strAllowEquipment.Length];
            //for (int i = 0; i < strAllowEquipment.Length; i++) { m_AllowEquipment[i] = strAllowEquipment[i] == '1'; }
            m_AllowEquipment = strAllowEquipment.Select(c => c == '1').ToArray();

            UseArmSameMovement = bUseArmSameMovement;

            m_pStageInterlock = new dlgb_o_o_o_o[400]; //400個stage interlock，因為Robot的位置可以儲存0~399



            m_CurrePos = RobotPos.NotORGN;

            _upperArmFunc = UpperArmFunc;
            _lowerArmFunc = LowerArmFunc;
            m_nFrameArmBackPulse = nFrameArmBackPulse;
            for (int nCnt = 0; nCnt < (int)enumRobotCommand.Max; nCnt++)
                _signalAck.Add((enumRobotCommand)nCnt, new SSignal(false, EventResetMode.ManualReset));

            for (int i = 0; i < (int)enumRobotSignalTable.Max; i++)
                _signals.Add((enumRobotSignalTable)i, new SSignal(false, EventResetMode.ManualReset));

            _signals[enumRobotSignalTable.ProcessCompleted].Set();



            m_Socket = new sRorzeSocket(IP, PortID, nBodyNo, "TRB", m_bSimulate, sever);

            UpperArm = new SRR757Axis(m_Socket.SendCommand, "ARM1", _signals[enumRobotSignalTable.MotionCompleted], m_bSimulate);
            LowerArm = new SRR757Axis(m_Socket.SendCommand, "ARM2", _signals[enumRobotSignalTable.MotionCompleted], m_bSimulate);
            Rotater = new SRR757Axis(m_Socket.SendCommand, "ROT1", _signals[enumRobotSignalTable.MotionCompleted], m_bSimulate);
            Lifter = new SRR757Axis(m_Socket.SendCommand, "ZAX1", _signals[enumRobotSignalTable.MotionCompleted], m_bSimulate);
            Traverse = new SRR757Axis(m_Socket.SendCommand, "XAX1", _signals[enumRobotSignalTable.MotionCompleted], m_bSimulate);
            Lifter2 = new SRR757Axis(m_Socket.SendCommand, "ZAX2", _signals[enumRobotSignalTable.MotionCompleted], m_bSimulate);
            TBL_560 = xAxis560;

                m_bExtXaxisDisable = TBL_560 == null;
            string strGpio = m_bSimulate ? "000000E0/DF080241" : "00000000/00000000";

            GPIO = new SRR757GPIO(strGpio.Split('/')[0], strGpio.Split('/')[1]);


            this.OnReadData += (object sender, MessageEventArgs e) =>
            {
                UpperArm.PassingReceiveData(sender, e);
                LowerArm.PassingReceiveData(sender, e);
                Rotater.PassingReceiveData(sender, e);
                Lifter.PassingReceiveData(sender, e);
                Traverse.PassingReceiveData(sender, e);

            };
            _threadManualFunc = new SInterruptOneThread(RunManualFunction);
            _threadEvnt = new SInterruptOneThread(ExeEVNT);
            _threadOrgn = new SInterruptOneThread(ExeORGN);
            _threadPause = new SInterruptOneThread(ExePAUS);
            _threadReset = new SInterruptOneThreadINT(ExeRSTA);
            _threadStop = new SInterruptOneThread(ExeSTOP);
            _threadMode = new SInterruptOneThreadINT(ExeMODE);
            _threadRobotJog = new SInterruptOneThreadobkj_INT(ExeSTEP);
            _threadRobotExtd = new SInterruptOneThreadobkj_INT(ExeEXTD);
            _threadWmap = new SInterruptOneThreadINT(ExeWMAP);
            _threadSpeed = new SInterruptOneThreadINT(ExeSSPD);
            _threadGetTeachData = new SInterruptOneThreadINT(ExeGetTeachData);
            _threadSetTeachData = new SInterruptOneThreadINT(ExeSetTeachData);
            _threadGetDMPRData = new SInterruptOneThreadINT(ExeGetDMPRData);
            _threadSetDMPRData = new SInterruptOneThreadINT(ExeSetDMPRData);
            _threadHome = new SInterruptOneThread(ExeHOME);

            _threadGetDAPMData = new SInterruptOneThreadINT(ExeGetDAPMData);
            _threadSetDAPMData = new SInterruptOneThreadINT(ExeSetDAPMData);

            Cleanjobschedule();

            _pollingAuto = new SPollingThread(1);
            _pollingAuto.DoPolling += _pollingAuto_DoPolling;

            _exePolling = new SPollingThread(1);
            _exePolling.DoPolling += _exePolling_DoPolling;

            if (!Disable)
            {
                _exePolling.Set();
            }
            CreateMessage();
        }
        ~SSRobotRR75x()
        {
            _pollingAuto.Close();
            _pollingAuto.Dispose();
            _exePolling.Close();
            _exePolling.Dispose();
        }
        public void Open() { m_Socket.Open(); }
        //==============================================================================
        #region 處理TCP接收到的內容
        private void _exePolling_DoPolling()
        {
            try
            {
                string[] astrFrame;

                if (!m_Socket.QueRecvBuffer.TryDequeue(out astrFrame)) return;

                string strFrame;

                if (OnReadData != null)
                    OnReadData(this, new MessageEventArgs(astrFrame));

                for (int nCnt = 0; nCnt < astrFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                {
                    if (astrFrame[nCnt].Length == 0)
                        continue;

                    strFrame = astrFrame[nCnt];

                    //Console.WriteLine(strFrame);
                    enumRobotCommand cmd = enumRobotCommand.GetVersion;
                    bool bUnknownCmd = true;

                    foreach (string scmd in _dicCmdsTable.Values) //查字典
                    {
                        if (strFrame.Contains(string.Format("TRB{0}.{1}", this.BodyNo.ToString("X"), scmd)))
                        {
                            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                            bUnknownCmd = false; //認識這個指令
                            break;
                        }
                    }

                    if (bUnknownCmd) //不認識的封包
                    {
                        WriteLog(string.Format("<<ByPassReceive>>> Got unknown frame and pass to process. [{0}]", strFrame));
                        continue;
                    }

                    WriteLog("Received : " + strFrame);

                    switch (strFrame[0]) //命令種類
                    {
                        case 'c': //cancel
                            OnCancelAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                            break;
                        case 'n': //nak
                            _signalAck[cmd].bAbnormalTerminal = true;
                            _signalAck[cmd].Set();
                            break;
                        case 'a': //ack
                            OnAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                            _signalAck[cmd].Set();
                            break;
                        case 'e':
                            OnAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
        }
        void OnAck(object sender, RorzenumRobotProtoclEventArgs e)
        {
            enumRobotCommand cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                case enumRobotCommand.GetStatus:
                    AnalysisStatus(e.Frame.Value);
                    break;
                case enumRobotCommand.GetIO:
                    AnalysisGPIO(e.Frame.Value);
                    break;
                case enumRobotCommand.DequGTDT:
                    AnalysisDEQU_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.DmntGTDT:
                    AnalysisDMNT_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.DmprGTDT:
                    AnalysisDMPR_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.DrciGTDT:
                    AnalysisDRCI_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.DrcsGTDT:
                    AnalysisDRCS_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.DtrbGTDT:
                    AnalysisDTRB_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.DtulGTDT:
                    AnalysisDTUL_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.DcfgGTDT:
                    AnalysisDCFG_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.DapmGTDT:
                    AnalysisDAPM_GTDT(e.Frame.Value);
                    break;
                case enumRobotCommand.GetMappingData:
                    m_strMappingData = e.Frame.Value;
                    break;
                case enumRobotCommand.GetRAC2:
                    AnalysisRAC2(e.Frame.Value);
                    break;
                case enumRobotCommand.GetVersion:
                    VersionData = e.Frame.Value;
                    break;
                case enumRobotCommand.GetDateTime:
                    break;
                case enumRobotCommand.ClientConnected:
                    _signalAck[cmd].Set();
                    Connected = true;
                    _threadEvnt.Set();
                    break;
                case enumRobotCommand.GTPN://2024.4.10針對TRB2新增
                    AnalysisGTPN(e.Frame.Value);//2024.4.10針對TRB2新增
                    break;
                default:
                    break;
            }

        }
        void OnCancelAck(object sender, RorzenumRobotProtoclEventArgs e)
        {
            enumRobotCommand cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;
            AnalysisCancel(e.Frame.Value);
        }
        private void AnalysisStatus(string strFrame)
        {
            if (strFrame.Contains('/') == false)
            {
                WriteLog(string.Format("the format of STAT frame has error, '/' not found! [{0}]", strFrame));
                return;
            }
            string[] str = strFrame.Split('/');
            string s1 = str[0];
            string s2 = str[1];

            //S1.bit#1 operation mode
            switch (s1[0])
            {
                case '0': m_eStatMode = enumRobotMode.Initializing; break;
                case '1':
                    m_eStatMode = enumRobotMode.Remote;
                    _signals[enumRobotSignalTable.Remote].Set(); break;
                case '2':
                    m_eStatMode = enumRobotMode.Maintenance;
                    _signals[enumRobotSignalTable.Remote].Set(); break;
                case '3': m_eStatMode = enumRobotMode.Recovery; break;
                case '4': m_eStatMode = enumRobotMode.TeachingPendent; break;
                default: break;
            }

            //S1.bit#2 origin return complete
            if (s1[1] == '0')
                _signals[enumRobotSignalTable.OPRCompleted].Reset();
            else
                _signals[enumRobotSignalTable.OPRCompleted].Set();
            m_bStatOrgnComplete = s1[1] == '1';

            //S1.bit#3 processing command
            if (s1[2] == '0')
                _signals[enumRobotSignalTable.ProcessCompleted].Set();
            else
                _signals[enumRobotSignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = s1[2] == '1';

            //S1.bit#4 operation status
            switch (s1[3])
            {
                case '0': m_eStatInPos = enumRobotStatus.InPos; break;
                case '1': m_eStatInPos = enumRobotStatus.Moving; break;
                case '2': m_eStatInPos = enumRobotStatus.Pause; break;
            }

            //S1.bit#5 operation speed
            if (s1[4] >= '0' && s1[4] <= '9') m_nSpeed = s1[4] - '0';
            else if (s1[4] >= 'A' && s1[4] <= 'K') m_nSpeed = s1[4] - 'A' + 10;
            if (m_nSpeed == 0) m_nMotionTimeout = 60000;
            else m_nMotionTimeout = 60000 * 3;

            //S2
            if (Convert.ToInt32(s2, 16) > 0)
            {
                _signals[enumRobotSignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumRobotSignalTable.MotionCompleted].Set(); //有moving過才可以Set
                SendAlmMsg(s2);
                m_strRecordsStatErr.Add(s2);
            }
            else
            {
                if ((m_eStatInPos == enumRobotStatus.InPos))//運動到位              
                    _signals[enumRobotSignalTable.MotionCompleted].Set();
                else
                    _signals[enumRobotSignalTable.MotionCompleted].Reset();

                if (IsError)
                {
                    foreach (string item in m_strRecordsStatErr)
                    {
                        RestAlmMsg(item);
                    }
                    m_strRecordsStatErr.Clear();
                }
            }
        }
        private void AnalysisCancel(string strFrame)
        {
            if (Convert.ToInt32(strFrame, 16) > 0)
            {
                _signals[enumRobotSignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumRobotSignalTable.MotionCompleted].Set(); //有moving過才可以Set

                SendCancelMsg(strFrame);
            }
        }
        private void AnalysisGPIO(string strFrame)
        {
            if (!strFrame.Contains('/'))
            {
                //_logger.WriteLog("<<<Error>>> the format of GPIO frame has error, [{0}]", strFrame);
                return;
            }

            GPIO = new SRR757GPIO(strFrame.Split('/')[0], strFrame.Split('/')[1]);

            OnIOChange?.Invoke(this, new SRR757IOChengeEventArgs(GPIO));
        }
        private void AnalysisDEQU_GTDT(string strFrame)
        {
            if (strFrame.Split(',').Length > 1)//
                DEQUData = strFrame.Split(',');

            if (DEQUData != null && DEQUData.Length == 71)
            {
                enumDEQU_15_waferSearch eMappingType;
                if (System.Enum.TryParse(DEQUData[15], out eMappingType))
                    MappingType = eMappingType;

                //finger type
                int nValue = int.Parse(DEQUData[16]);

                int nBit08_15 = (nValue >> 8) & 0xFF;
                int nBit16_23 = (nValue >> 16) & 0xFF;

                EnableUpperAlignment = (nBit08_15 == 0x1F || nBit08_15 == 0x1E);
                EnableLowerAlignment = (nBit16_23 == 0x1F || nBit16_23 == 0x1E);
            }

        }
        private void AnalysisDMNT_GTDT(string strFrame)
        {
            if (!strFrame.Contains('/'))
            {
                //_logger.WriteLog("<<<Error>>> the format of GPIO frame has error, [{0}]", strFrame);
                return;
            }
            //_gpio = new SRR757GPIO(strFrame.Split('/')[0], strFrame.Split('/')[1]);
            //if (OnIOChange != null)
            //    OnIOChange(this, new SRR757IOChengeEventArgs(_gpio));
        }
        private void AnalysisDMPR_GTDT(string strFrame)
        {
            DMPRData = strFrame.Split(',');
        }
        private void AnalysisDRCI_GTDT(string strFrame)
        {
            //if (!strFrame.Contains('/'))
            //{
            //    _logger.WriteLog("<<<Error>>> the format of GPIO frame has error, [{0}]", strFrame);
            //    return;
            //}
            DRCSData = strFrame.Split(',');
        }
        private void AnalysisDRCS_GTDT(string strFrame)
        {
            DRCSData = strFrame.Split(',');
        }
        private void AnalysisDTRB_GTDT(string strFrame)
        {
            DTRBData = strFrame.Split(',');
        }
        private void AnalysisDTUL_GTDT(string strFrame)
        {
            DTULData = strFrame.Split(',');
        }
        private void AnalysisDCFG_GTDT(string strFrame)
        {
            DCFGData = strFrame.Split(',');
        }
        private void AnalysisDAPM_GTDT(string strFrame)
        {
            m_strDAPM_Ack = strFrame.Split(',');
        }
        private void AnalysisGTPN(string strFrame)//2024.4.10針對NPD TRB2新增
        {
            //eTRB2.GTPN:xxxx/xxxx
            if (BodyNo != 2) return;
            if (strFrame.Contains('/') == false) return;
            if (strFrame.Split('/').Length != 2) return;
            if (strFrame.Split('/')[0].Length != 4) return;
            if (strFrame.Split('/')[1].Length != 4) return;
            //Input
            //bit0 伸 一代機
            //bit1 縮 一代機
            //bit2 伸 一代機
            //bit3 縮 一代機
            //Output
            //bit0 伸 一代機
            //bit1 縮 一代機            
            int _nPi = Convert.ToInt32(strFrame.Split('/')[0], 16);
            int _nPo = Convert.ToInt32(strFrame.Split('/')[1], 16);
            //DI
            bool bExtend1 = (_nPi & 1 << 0) != 0;
            bool bSafety1 = (_nPi & 1 << 1) != 0;
            bool bExtend2 = (_nPi & 1 << 2) != 0;
            bool bSafety2 = (_nPi & 1 << 3) != 0;

            PinExtend = bExtend1 && bExtend2;
            PinSafety = bSafety1 && bSafety2;
            //PinSafety = (bSafety1 || bSafety2) && PinExtend == false;

            //GPIO = new SRR757GPIO(strFrame.Split('/')[0], strFrame.Split('/')[1]);
            //OnIOChange?.Invoke(this, new SRR757IOChengeEventArgs(GPIO));
        }
        private void AnalysisRAC2(string strFrame)
        {
            _Rac2Data = strFrame.Split(',');
        }
        #endregion
        //==============================================================================
        #region AutoProcess
        public void AutoProcessStart()
        {
            m_bProcessStart = true;
            this._pollingAuto.Set();
            if (OnProcessStart != null)
                OnProcessStart(this, new EventArgs());
        }
        public void AutoProcessEnd()
        {
            _pollingAuto.Reset();
            m_bProcessStart = false;
            if (OnProcessEnd != null)
                OnProcessEnd(this, new EventArgs());
        }
        private void _pollingAuto_DoPolling()
        {
            try
            {
                if (DoAutoProcessing != null) DoAutoProcessing(this);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                _pollingAuto.Reset();
                OnProcessAbort?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                _pollingAuto.Reset();
                OnProcessAbort?.Invoke(this, new EventArgs());
            }
        }
        public void AssignQueue(SWafer wafer)
        {
            lock (_objLockQueue)
            {
                quePreCommand.Enqueue(wafer);
            }
        }
        #endregion
        //==============================================================================
        #region OneThread 
        public void StartManualFunction() { _threadManualFunc.Set(); }
        public void ORGN() { _threadOrgn.Set(); }
        public void PAUS() { _threadPause.Set(); }
        public void RSTA(int nReset) { _threadReset.Set(nReset); }
        public void STOP() { _threadStop.Set(); }
        public void MODE(int nMode) { _threadMode.Set(nMode); }
        public void STEP(object Axis, int step) { _threadRobotJog.Set(Axis, step); }
        public void EXTD(object Axis, int extd) { _threadRobotExtd.Set(Axis, extd); }
        public void WMAP(int nStage) { _threadWmap.Set(nStage); }
        public void SSPD(int nSpeed) { _threadSpeed.Set(nSpeed); }
        public void GetTeachData(int nStage) { _threadGetTeachData.Set(nStage); }
        public void SetTeachData(int nStage) { _threadSetTeachData.Set(nStage); }
        public void GetDMPRData(int nStage) { _threadGetDMPRData.Set(nStage); }
        public void SetDMPRData(int nStage) { _threadSetDMPRData.Set(nStage); }
        public void HOME() { _threadHome.Set(); }
        public void GetDAPMData(int nStage) { _threadGetDAPMData.Set(nStage); }
        public void SetDAPMData(int nStage) { _threadSetDAPMData.Set(nStage); }
        //==============================================================================
        private void RunManualFunction()
        {
            try
            {
                WriteLog("RunManualFunction:Start");
                if (_bManualMoving)//防呆
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    return;
                }
                _signals[enumRobotSignalTable.ProcessCompleted].Reset();
                _bManualMoving = true;
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

            _bManualMoving = false;
        }
        private void ExeEVNT()
        {
            try
            {
                this.EventW(m_nAckTimeout);
                if (!ExtXaxisDisable)
                    TBL_560.EventW(m_nAckTimeout);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
        }
        private void ExeORGN()
        {
            try
            {
                WriteLog("ExeORGN:Start");

                this.ResetChangeModeCompleted();
                this.EventW(m_nAckTimeout);
                this.WaitChangeModeCompleted(3000);

                if (!ExtXaxisDisable)
                {
                    TBL_560.ResetChangeModeCompleted();
                    TBL_560.EventW(m_nAckTimeout);
                    TBL_560.WaitChangeModeCompleted(3000);
                }

                if (BodyNo == 1)//NPD TRB2 沒支援這個指令
                {
                    GtdtW(m_nAckTimeout, enumRobotDataType.DEQU);
                }

                if (BodyNo == 2 && PinSafety == false)
                {
                    this.MtpnW(m_nAckTimeout, 0);
                    if (SpinWait.SpinUntil(() => PinSafety, 5000) == false)
                    {
                        SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                        throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "InterlockStop Pin not safety invalid");
                    }
                }

                SpinWait.SpinUntil(() => false, 100);

                this.ResetChangeModeCompleted();
                this.InitW(m_nInitAckTimeout);
                this.WaitChangeModeCompleted(5000);
                if (!ExtXaxisDisable)
                {
                    TBL_560.ResetChangeModeCompleted();
                    TBL_560.InitW(m_nAckTimeout);
                    TBL_560.WaitChangeModeCompleted(3000);
                }


                this.StimW(m_nAckTimeout);
                if (!ExtXaxisDisable)
                    TBL_560.StimW(m_nAckTimeout);

                this.ResetProcessCompleted();
                this.ResetW(m_nAckTimeout, 1);
                this.WaitProcessCompleted(5000);
                if (!ExtXaxisDisable)
                {
                    TBL_560.ResetProcessCompleted();
                    TBL_560.ResetW(m_nAckTimeout, 1);
                    TBL_560.WaitProcessCompleted(5000);
                }

                this.ResetProcessCompleted();

                if (!ExtXaxisDisable)
                    TBL_560.ResetProcessCompleted();

                this.ExctW(m_nAckTimeout, 1);
                this.WaitProcessCompleted(5000);

                if (!ExtXaxisDisable)
                    TBL_560.WaitProcessCompleted(5000);

                this.ResetInPos();
                this.ResetOrgnSinal();
                this.OrgnW(m_nAckTimeout);
                if (!ExtXaxisDisable)
                {
                    TBL_560.ResetInPos();
                    TBL_560.ResetOrgnSinal();
                    TBL_560.OrgnW(m_nAckTimeout, m_eXAX1);
                    TBL_560.WaitInPos(300000);
                    TBL_560.WaitOrgnCompleted(m_nAckTimeout);

                    TBL_560.WtdtW(m_nAckTimeout);
                }

                SpinWait.SpinUntil(() => false, 2000);

                this.WaitInPos(300000);
                this.WaitOrgnCompleted(m_nAckTimeout);

                this.GpioW(m_nAckTimeout);
                if (!ExtXaxisDisable)
                    TBL_560.GpioW(m_nAckTimeout);

                if (GPIO.DO_LowerArmOrigin && GPIO.DO_UpperArmOrigin)
                {

                }

                m_CurrePos = RobotPos.Home;

                Cleanjobschedule();

                SetEQExtendFlag?.Invoke(false);//回完原點//回完原點
                OnORGNComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnORGNComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnORGNComplete?.Invoke(this, false);
            }
        }
        private void ExePAUS()
        {
            try
            {
                WriteLog("ExePAUS:Start");
                this.PausW(3000);
                if (!ExtXaxisDisable)
                    TBL_560.PausW(3000);
                OnPAUSComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnPAUSComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnPAUSComplete?.Invoke(this, false);
            }
        }
        private void ExeRSTA(int nReset)
        {
            try
            {
                WriteLog("ExeRSTA:Start");
                this.ResetW(3000, nReset);
                if (!ExtXaxisDisable)
                    TBL_560.ResetW(3000);

                OnRSTAComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnRSTAComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnRSTAComplete?.Invoke(this, false);
            }
        }
        private void ExeSTOP()
        {
            try
            {
                WriteLog("ExeSTOP:Start");
                this.StopW(3000);
                if (!ExtXaxisDisable)
                    TBL_560.StopW(3000);

                OnSTOPComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnSTOPComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnSTOPComplete?.Invoke(this, false);
            }
        }
        private void ExeMODE(int nMode)
        {
            bool bSuc = false;
            try
            {
                WriteLog("ExeMODE : start change to " + nMode);
                this.ModeW(m_nAckTimeout, nMode);
                bSuc = true;
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            OnMODEComplete?.Invoke(this, bSuc);
        }
        private void ExeSTEP(object Axis, int Step)
        {
            try
            {
                WriteLog("ExeSTEP:Start");
                this.ResetInPos();
                switch ((enumRobotAxis)Axis)
                {
                    case enumRobotAxis.Arm1:
                        UpperArm.JogW(3000, Step);
                        this.WaitInPos(60000);
                        UpperArm.GetPosW(3000);
                        break;
                    case enumRobotAxis.Arm2:
                        LowerArm.JogW(3000, Step);
                        this.WaitInPos(60000);
                        LowerArm.GetPosW(3000);
                        break;
                    case enumRobotAxis.Rot:
                        Rotater.JogW(3000, Step);
                        this.WaitInPos(60000);
                        Rotater.GetPosW(3000);
                        break;
                    case enumRobotAxis.Zax:
                        Lifter.JogW(3000, Step);
                        this.WaitInPos(60000);
                        Lifter.GetPosW(3000);
                        break;
                    case enumRobotAxis.Xax:
                        Traverse.JogW(3000, Step);
                        this.WaitInPos(60000);
                        Traverse.GetPosW(3000);
                        break;
                    case enumRobotAxis.ExtX:
                        TBL_560.AxisMrelW(3000, m_eXAX1, Step);
                        TBL_560.WaitInPos(60000);
                        TBL_560.AxisGposW(3000, m_eXAX1);
                        break;
                }

                OnJobFunctionCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnJobFunctionCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnJobFunctionCompleted?.Invoke(this, false);
            }
        }
        private void ExeEXTD(object Axis, int Extd)
        {
            try
            {
                WriteLog("ExeEXTD:Start");
                this.ResetInPos();

                switch ((enumRobotAxis)Axis)
                {
                    case enumRobotAxis.Arm1:
                        UpperArm.AbsolutePosW(3000, Extd);
                        this.WaitInPos(60000);
                        UpperArm.GetPosW(3000);
                        break;
                    case enumRobotAxis.Arm2:
                        LowerArm.AbsolutePosW(3000, Extd);
                        this.WaitInPos(60000);
                        LowerArm.GetPosW(3000);
                        break;
                    case enumRobotAxis.Rot:
                        Rotater.AbsolutePosW(3000, Extd);
                        this.WaitInPos(60000);
                        Rotater.GetPosW(3000);
                        break;
                    case enumRobotAxis.Zax:
                        Lifter.AbsolutePosW(3000, Extd);
                        this.WaitInPos(60000);
                        Lifter.GetPosW(3000);
                        break;
                    case enumRobotAxis.Xax:
                        Traverse.AbsolutePosW(3000, Extd);
                        this.WaitInPos(60000);
                        Traverse.GetPosW(3000);
                        break;
                    case enumRobotAxis.ExtX:
                        TBL_560.AxisMabsW(3000, m_eXAX1, Extd);
                        this.WaitInPos(60000);
                        TBL_560.AxisGposW(3000, m_eXAX1);
                        break;
                }

                OnExtdFunctionCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnExtdFunctionCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnExtdFunctionCompleted?.Invoke(this, false);
            }
        }
        private void ExeWMAP(int nStg0to399)
        {
            try
            {
                WriteLog("ExeWMAP:Start");

                if (SpinWait.SpinUntil(() => IsMoving == false/*GetRunningPermissionForStgMap(BodyNo)*/, 60000 * 3))
                {

                    //ResetProcessCompleted();
                    //SspdW(m_nAckTimeout, 10);
                    //WaitProcessCompleted(3000);

                    ResetInPos();
                    WmapW(m_nAckTimeout, nStg0to399);
                    WaitInPos(60000);

                    WriteLog(string.Format("robot{0} get mapping", BodyNo));

                    GmapW(m_nAckTimeout, nStg0to399);
                }
                else
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)enumRobotError.InterlockStop, "Wait for PermissionForStgMap timeout");
                }

                OnWmapFunctionCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnWmapFunctionCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnWmapFunctionCompleted?.Invoke(this, false);
            }
        }
        private void ExeSSPD(int nSpeed)
        {
            try
            {
                WriteLog("ExeSSPD:Start");

                this.ResetProcessCompleted();
                this.SspdW(3000, nSpeed);
                this.WaitProcessCompleted(3000);
                if (ExtXaxisDisable == false)
                {
                    TBL_560.ResetProcessCompleted();
                    TBL_560.SspdW(3000, nSpeed);
                    TBL_560.WaitProcessCompleted(3000);
                }

                OnSSPDComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnSSPDComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnSSPDComplete?.Invoke(this, false);
            }
        }
        private void ExeGetTeachData(int nStage)
        {
            try
            {
                WriteLog("ExeGetTeachData:Start");

                GtdtW(m_nAckTimeout, enumRobotDataType.DEQU);

                GtdtW(m_nAckTimeout, enumRobotDataType.DTRB, nStage);

                GtdtW(m_nAckTimeout, enumRobotDataType.DTUL, nStage);

                GtdtW(m_nAckTimeout, enumRobotDataType.DCFG, nStage);

                OnGetTeachDataCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnGetTeachDataCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnGetTeachDataCompleted?.Invoke(this, false);
            }
        }
        private void ExeSetTeachData(int nStage)
        {
            try
            {
                WriteLog("ExeSetTeachData:Start");

                if (m_bExtXaxisDisable)
                {
                    if (DEQUData != null)
                        StdtW(3000, enumRobotDataType.DEQU, 8, DEQUData[8]);

                    StdtW(3000, enumRobotDataType.DTRB, nStage, 0, DTRBData[0]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 1, DTRBData[1]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 2, DTRBData[2]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 3, DTRBData[3]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 4, DTRBData[4]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 5, DTRBData[5]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 6, DTRBData[6]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 7, DTRBData[7]);

                    StdtW(3000, enumRobotDataType.DTRB, nStage, 9, DTRBData[9]);//edge clamp
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 10, DTRBData[10]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 11, DTRBData[11]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 12, DTRBData[12]);

                    StdtW(3000, enumRobotDataType.DTRB, nStage, 15, DTRBData[15]);//二段式取片上升比例

                    StdtW(3000, enumRobotDataType.DTRB, nStage, 17, DTRBData[17]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 18, DTRBData[18]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 19, DTRBData[19]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 20, DTRBData[20]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 21, DTRBData[21]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 22, DTRBData[22]);
                    //StdtW(3000, enumRobotDataType.DTRB, nStage, 23, DTRBData[23]);
                    //StdtW(3000, enumRobotDataType.DTRB, nStage, 24, DTRBData[24]);

                    StdtW(3000, enumRobotDataType.DTUL, nStage, 6, DTULData[6]);//unload offset
                    StdtW(3000, enumRobotDataType.DTUL, nStage, 7, DTULData[7]);//unload offset
                    StdtW(3000, enumRobotDataType.DTUL, nStage, 9, DTULData[9]);//二段式取片會往後縮
                    StdtW(3000, enumRobotDataType.DTUL, nStage, 15, DTULData[15]);//edge clamp
                    StdtW(3000, enumRobotDataType.DTUL, nStage, 16, DTULData[16]);//edge clamp

                    StdtW(3000, enumRobotDataType.DCFG, nStage, 5, DCFGData[5]);
                    StdtW(3000, enumRobotDataType.DCFG, nStage, 6, DCFGData[6]);//edge clamp
                    StdtW(3000, enumRobotDataType.DCFG, nStage, 7, DCFGData[7]);//edge clamp
                    StdtW(3000, enumRobotDataType.DCFG, nStage, 8, DCFGData[8]);
                    StdtW(3000, enumRobotDataType.DCFG, nStage, 9, DCFGData[9]);

                    WtdtW(15000);


                }
                else
                {
                    if (DEQUData != null)
                        StdtW(3000, enumRobotDataType.DEQU, 8, DEQUData[8]);

                    StdtW(3000, enumRobotDataType.DTRB, nStage, 0, DTRBData[0]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 1, DTRBData[1]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 2, DTRBData[2]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 3, DTRBData[3]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 4, DTRBData[4]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 5, DTRBData[5]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 6, DTRBData[6]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 7, DTRBData[7]);

                    StdtW(3000, enumRobotDataType.DTRB, nStage, 9, DTRBData[9]);//edge clamp
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 10, DTRBData[10]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 11, DTRBData[11]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 12, DTRBData[12]);

                    StdtW(3000, enumRobotDataType.DTRB, nStage, 15, DTRBData[15]);//二段式取片上升比例

                    StdtW(3000, enumRobotDataType.DTRB, nStage, 17, DTRBData[17]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 18, DTRBData[18]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 19, DTRBData[19]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 20, DTRBData[20]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 21, DTRBData[21]);
                    StdtW(3000, enumRobotDataType.DTRB, nStage, 22, DTRBData[22]);
                    //StdtW(3000, enumRobotDataType.DTRB, nStage, 23, DTRBData[23]);
                    //StdtW(3000, enumRobotDataType.DTRB, nStage, 24, DTRBData[24]);

                    StdtW(3000, enumRobotDataType.DTUL, nStage, 6, DTULData[6]);//unload offset
                    StdtW(3000, enumRobotDataType.DTUL, nStage, 7, DTULData[7]);//unload offset
                    StdtW(3000, enumRobotDataType.DTUL, nStage, 9, DTULData[9]);//二段式取片會往後縮
                    StdtW(3000, enumRobotDataType.DTUL, nStage, 15, DTULData[15]);//edge clamp
                    StdtW(3000, enumRobotDataType.DTUL, nStage, 16, DTULData[16]);//edge clamp

                    StdtW(3000, enumRobotDataType.DCFG, nStage, 5, DCFGData[5]);
                    StdtW(3000, enumRobotDataType.DCFG, nStage, 6, DCFGData[6]);//edge clamp
                    StdtW(3000, enumRobotDataType.DCFG, nStage, 7, DCFGData[7]);//edge clamp
                    StdtW(3000, enumRobotDataType.DCFG, nStage, 8, DCFGData[8]);
                    StdtW(3000, enumRobotDataType.DCFG, nStage, 9, DCFGData[9]);

                    WtdtW(15000);
                    //寫入ini紀錄
                    GParam.theInst.WriteRobotPos();
                }

                OnSetTeachDataCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnSetTeachDataCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnSetTeachDataCompleted?.Invoke(this, false);
            }
        }
        private void ExeGetDMPRData(int nStage)//robot mapping
        {
            try
            {
                WriteLog("ExeGetDMPRData:Start");

                GtdtW(3000, enumRobotDataType.DMPR, nStage);

                OnGetDmprDataCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnGetDmprDataCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnGetDmprDataCompleted?.Invoke(this, false);
            }
        }
        private void ExeSetDMPRData(int nStage)//robot mapping
        {
            try
            {
                WriteLog("ExeSetDMPRData:Start");

                StdtW(3000, enumRobotDataType.DMPR, nStage, 0, DMPRData[0]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 1, DMPRData[1]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 2, DMPRData[2]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 3, DMPRData[3]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 4, DMPRData[4]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 5, DMPRData[5]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 6, DMPRData[6]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 7, DMPRData[7]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 8, DMPRData[8]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 9, DMPRData[9]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 10, DMPRData[10]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 11, DMPRData[11]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 12, DMPRData[12]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 13, DMPRData[13]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 14, DMPRData[14]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 15, DMPRData[15]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 16, DMPRData[16]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 17, DMPRData[17]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 18, DMPRData[18]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 19, DMPRData[19]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 20, DMPRData[20]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 21, DMPRData[21]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 22, DMPRData[22]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 23, DMPRData[23]);
                StdtW(3000, enumRobotDataType.DMPR, nStage, 24, DMPRData[24]);
                WtdtW(15000);

                OnSetDmprDataCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnSetDmprDataCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnSetDmprDataCompleted?.Invoke(this, false);
            }
        }
        private void ExeHOME()
        {
            try
            {
                WriteLog("ExeHOME:Start");

                this.ResetInPos();
                this.MoveToStandbyPosW(3000);
                this.WaitInPos(120000);

                if (!ExtXaxisDisable)
                {
                    TBL_560.ResetInPos();
                    RobPos pos = GParam.theInst.DicRobPos[enumPosition.HOME];
                    TBL_560.AxisMabsW(3000, m_eXAX1, pos.Pos_ARM1);
                    TBL_560.WaitInPos(m_nMotionTimeout);
                }
                

                OnHOMEComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnHOMEComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnHOMEComplete?.Invoke(this, false);
            }
        }
        private void ExeGetDAPMData(int nStage)
        {
            try
            {
                WriteLog("ExeGetDAPMData:Start");

                GtdtW(m_nAckTimeout, enumRobotDataType.DEQU);

                m_strDAPM_Ack = null;
                GtdtW(3000, enumRobotDataType.DAPM, 0);
                DAPMData[0] = m_strDAPM_Ack;

                m_strDAPM_Ack = null;
                GtdtW(3000, enumRobotDataType.DAPM, 1);
                DAPMData[1] = m_strDAPM_Ack;

                m_strDAPM_Ack = null;
                GtdtW(3000, enumRobotDataType.DAPM, 2);
                DAPMData[2] = m_strDAPM_Ack;

                GtdtW(m_nAckTimeout, enumRobotDataType.DCFG, nStage);

                OnGetAlignmentDataCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
                OnGetAlignmentDataCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                OnGetAlignmentDataCompleted?.Invoke(this, false);
            }
        }
        private void ExeSetDAPMData(int nStage)
        {
            bool bSuc = false;
            try
            {
                WriteLog("ExeSetDAPMData:Start");


                StdtW(3000, enumRobotDataType.DEQU, 9, DEQUData[9]);

                StdtW(3000, enumRobotDataType.DCFG, nStage, 5, DCFGData[5]);

                StdtW(3000, enumRobotDataType.DAPM, 0, 0, DAPMData[0][0]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 1, DAPMData[0][1]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 2, DAPMData[0][2]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 3, DAPMData[0][3]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 4, DAPMData[0][4]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 5, DAPMData[0][5]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 6, DAPMData[0][6]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 7, DAPMData[0][7]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 8, DAPMData[0][8]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 9, DAPMData[0][9]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 10, DAPMData[0][10]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 11, DAPMData[0][11]);
                StdtW(3000, enumRobotDataType.DAPM, 0, 12, DAPMData[0][12]);

                StdtW(3000, enumRobotDataType.DAPM, 1, 0, DAPMData[1][0]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 1, DAPMData[1][1]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 2, DAPMData[1][2]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 3, DAPMData[1][3]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 4, DAPMData[1][4]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 5, DAPMData[1][5]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 6, DAPMData[1][6]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 7, DAPMData[1][7]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 8, DAPMData[1][8]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 9, DAPMData[1][9]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 10, DAPMData[1][10]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 11, DAPMData[1][11]);
                StdtW(3000, enumRobotDataType.DAPM, 1, 12, DAPMData[1][12]);

                StdtW(3000, enumRobotDataType.DAPM, 2, 0, DAPMData[2][0]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 1, DAPMData[2][1]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 2, DAPMData[2][2]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 3, DAPMData[2][3]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 4, DAPMData[2][4]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 5, DAPMData[2][5]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 6, DAPMData[2][6]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 7, DAPMData[2][7]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 8, DAPMData[2][8]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 9, DAPMData[2][9]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 10, DAPMData[2][10]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 11, DAPMData[2][11]);
                StdtW(3000, enumRobotDataType.DAPM, 2, 12, DAPMData[2][12]);

                WtdtW(15000);

                bSuc = true;
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            OnSetAlignmentDataCompleted?.Invoke(this, bSuc);
        }
        #endregion
        //==============================================================================
        /// <summary>
        /// Robot Home
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="HaveWafer"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStgeIndx"></param>
        /// <param name="nSlot"></param>
        /// <exception cref="SException"></exception>
        /// <remarks>nStgeIndx:0~399</remarks>
        public void MoveToStandbyByInterLockW(int nTimeout, bool HaveWafer, enumRobotArms eRobotArms, int nStg0to399, int nSlot)
        {
            WriteLog("MoveToStandbyByInterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "MoveToStandbyByInterLock InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "MoveToStandbyByInterLock InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Standby, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "MoveToStandbyByInterLock InterlockStop stg0~399:" + nStg0to399);
                }
            }



            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, HaveWafer, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

        }
        public void MoveToStandbyByInterLockW_ExtXaxis(int nTimeout, bool HaveWafer, enumPosition ePosition, enumRobotArms eRobotArms, int nStg0to399, int nSlot)
        {
            WriteLog("MoveToStandbyByInterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "MoveToStandbyByInterLock InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "MoveToStandbyByInterLock InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (!GParam.theInst.DicRobPos.ContainsKey(ePosition))
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "MoveToStandbyByInterLock InterlockStop position can not find position: " + ePosition.ToString() + " " + eRobotArms + " invalid");
            }
            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Standby, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "MoveToStandbyByInterLock InterlockStop stg0~399:" + nStg0to399);
                }
            }



            this.ResetInPos();
            this.MoveToStandbyPosW_Ext_Xaxis(nTimeout, HaveWafer, ePosition, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

        }
        /// <summary>
        /// Robot Unld
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStgeIndx"></param>
        /// <param name="nSlot"></param>
        /// <exception cref="SException"></exception>
        /// <remarks>nStgeIndx:0~399</remarks>
        public void PutWaferByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, SWafer WaferData = null)
        {
            WriteLog("PutWaferByInterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "MoveToStandbyByInterLock InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            
            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, true, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Unlaod, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399:" + nStg0to399);
                }
            }

            
            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: false  // PutWafer
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

                this.ResetInPos();
            this.UnldW(nTimeout, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (OnUnldComplete != null) OnUnldComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        public void PutWaferByInterLockW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, SWafer WaferData = null)
        {
            WriteLog("PutWaferByInterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "PutWaferByInterLock InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (!GParam.theInst.DicRobPos.ContainsKey(ePosition))
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop position can not find position: " + ePosition.ToString() + " " + eRobotArms + " invalid");
            }

            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyByInterLockW_ExtXaxis(nTimeout, true, ePosition, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Unlaod, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399:" + nStg0to399);
                }
            }

            switch (ePosition)
            {
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    this.ResetInPos();
                    ExtdW(nTimeout, 2, eRobotArms, nStg0to399, nSlot);
                    this.WaitInPos(GetMotionTimeout);

                    if (SpinWait.SpinUntil(() => this._IsPanelMisalign(ePosition) == true, 1000) == true)
                    {
                        SendAlmMsg(enumRobotError.Panel_Misalign);
                        throw new SException((int)(enumRobotError.Panel_Misalign), "PutWaferByInterLock Panel Misalign:" + nStg0to399);
                    }
                    break;
            }


            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: false  // PutWafer
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            this.ResetInPos();
            this.UnldW(nTimeout, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (OnUnldComplete != null) OnUnldComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        /// <summary>
        /// Robot Unld
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStgeIndx"></param>
        /// <param name="nSlot"></param>
        /// <param name="nCheckTime"></param>
        /// <exception cref="SException"></exception>
        /// <remarks>nStgeIndx:0~399</remarks>
        public void PutWaferByInterLockClampCheckW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, int nCheckTime, SWafer WaferData = null)
        {
            WriteLog("PutWaferByInterLockClampCheckW:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "MoveToStandbyByInterLock InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
                    }
            }

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Unlaod, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399:" + nStg0to399);
                }
            }

            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: false  // PutWafer
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            //放片完成後手臂伸出
            this.ResetInPos();
            this.UnldW(nTimeout, eRobotArms, nStg0to399, nSlot, 1);
            this.WaitInPos(GetMotionTimeout);


            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    //吸吸看
                    this.ResetInPos();
                    this.ClmpW(nTimeout, eRobotArms, nCheckTime);
                    this.WaitInPos(GetMotionTimeout);
                    if (GPIO.DI_UpperPresence1)
                    {
                        SendAlmMsg(enumRobotError.UnldDetectWafer);
                        throw new SException((int)(enumRobotError.UnldDetectWafer), string.Format("PutWafer detection of wafer in the upper arm stg0~399:[{0}]", nStg0to399));
                    }
                    break;
                case enumRobotArms.LowerArm:
                    //吸吸看
                    this.ResetInPos();
                    this.ClmpW(nTimeout, eRobotArms, nCheckTime);
                    this.WaitInPos(GetMotionTimeout);
                    if (GPIO.DI_LowerPresence1)
                    {
                        SendAlmMsg(enumRobotError.UnldDetectWafer);
                        throw new SException((int)(enumRobotError.UnldDetectWafer), string.Format("PutWafer detection of wafer in the lower arm stg0~399:[{0}]", nStg0to399));
                    }
                    break;
                case enumRobotArms.BothArms:
                    //吸吸看
                    this.ResetInPos();
                    this.ClmpW(nTimeout, enumRobotArms.UpperArm, nCheckTime);
                    this.WaitInPos(GetMotionTimeout);
                    if (GPIO.DI_UpperPresence1)
                    {
                        SendAlmMsg(enumRobotError.UnldDetectWafer);
                        throw new SException((int)(enumRobotError.UnldDetectWafer), string.Format("PutWafer detection of wafer in the upper arm stg0~399:[{0}]", nStg0to399));
                    }
                    //吸吸看
                    this.ResetInPos();
                    this.ClmpW(nTimeout, enumRobotArms.LowerArm, nCheckTime);
                    this.WaitInPos(GetMotionTimeout);
                    if (GPIO.DI_LowerPresence1)
                    {
                        SendAlmMsg(enumRobotError.UnldDetectWafer);
                        throw new SException((int)(enumRobotError.UnldDetectWafer), string.Format("PutWafer detection of wafer in the lower arm stg0~399:[{0}]", nStg0to399));
                    }
                    break;
                default:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399:" + nStg0to399);
            }

            //手臂縮回
            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, false, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (OnLoadComplete != null) OnLoadComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        public void PutWaferByInterLockClampCheckW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, int nCheckTime, SWafer WaferData = null)
        {
            WriteLog("PutWaferByInterLockClampCheckW_ExtXaxis:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (!GParam.theInst.DicRobPos.ContainsKey(ePosition))
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop position can not find position: " + ePosition.ToString() + " " + eRobotArms + " invalid");
            }
            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
                    }
            }

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Unlaod, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLockClampCheckW_ExtXaxis InterlockStop stg0~399:" + nStg0to399);
                }
            }

            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: false  // PutWafer
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            this.ResetInPos();
            this.MoveToStandbyPosW_Ext_Xaxis(nTimeout, false, ePosition, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            //放片完成後手臂伸出
            this.ResetInPos();
            this.UnldW(nTimeout, eRobotArms, nStg0to399, nSlot, 1);
            this.WaitInPos(GetMotionTimeout);


            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    //吸吸看
                    this.ResetInPos();
                    this.ClmpW(nTimeout, eRobotArms, nCheckTime);
                    this.WaitInPos(GetMotionTimeout);
                    if (GPIO.DI_UpperPresence1)
                    {
                        SendAlmMsg(enumRobotError.UnldDetectWafer);
                        throw new SException((int)(enumRobotError.UnldDetectWafer), string.Format("PutWafer detection of wafer in the upper arm stg0~399:[{0}]", nStg0to399));
                    }
                    break;
                case enumRobotArms.LowerArm:
                    //吸吸看
                    this.ResetInPos();
                    this.ClmpW(nTimeout, eRobotArms, nCheckTime);
                    this.WaitInPos(GetMotionTimeout);
                    if (GPIO.DI_LowerPresence1)
                    {
                        SendAlmMsg(enumRobotError.UnldDetectWafer);
                        throw new SException((int)(enumRobotError.UnldDetectWafer), string.Format("PutWafer detection of wafer in the lower arm stg0~399:[{0}]", nStg0to399));
                    }
                    break;
                case enumRobotArms.BothArms:
                    //吸吸看
                    this.ResetInPos();
                    this.ClmpW(nTimeout, enumRobotArms.UpperArm, nCheckTime);
                    this.WaitInPos(GetMotionTimeout);
                    if (GPIO.DI_UpperPresence1)
                    {
                        SendAlmMsg(enumRobotError.UnldDetectWafer);
                        throw new SException((int)(enumRobotError.UnldDetectWafer), string.Format("PutWafer detection of wafer in the upper arm stg0~399:[{0}]", nStg0to399));
                    }
                    //吸吸看
                    this.ResetInPos();
                    this.ClmpW(nTimeout, enumRobotArms.LowerArm, nCheckTime);
                    this.WaitInPos(GetMotionTimeout);
                    if (GPIO.DI_LowerPresence1)
                    {
                        SendAlmMsg(enumRobotError.UnldDetectWafer);
                        throw new SException((int)(enumRobotError.UnldDetectWafer), string.Format("PutWafer detection of wafer in the lower arm stg0~399:[{0}]", nStg0to399));
                    }
                    break;
                default:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399:" + nStg0to399);
            }

            //手臂縮回
            this.ResetInPos();
            this.MoveToStandbyPosW_Ext_Xaxis(nTimeout, false, ePosition, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (OnLoadComplete != null) OnLoadComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        /// <summary>
        /// Robot Load
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStgeIndx"></param>
        /// <param name="nSlot"></param>
        /// <exception cref="SException"></exception>
        /// <remarks>nStgeIndx:0~399</remarks>
        public void TakeWaferByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, SWafer WaferData = null)
        {
            WriteLog("TakeWaferByInterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "MoveToStandbyByInterLock InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, false, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Load, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop stg0~399: " + nStg0to399);
                }
            }

            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: true  // TakeWafer = 從位置取料
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            this.ResetInPos();
            this.LoadW(nTimeout, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (OnLoadComplete != null) OnLoadComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        public void TakeWaferByInterLockW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, SWafer WaferData = null)
        {
            WriteLog("TakeWaferByInterLockW_ExtXaxis:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "TakeWaferByInterLockW_ExtXaxis InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (!GParam.theInst.DicRobPos.ContainsKey(ePosition))
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop position can not find position: " + ePosition.ToString() + " " + eRobotArms + " invalid");
            }
            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyByInterLockW_ExtXaxis(nTimeout, false, ePosition, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Load, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399);
                }
            }

            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: true  // TakeWafer = 從位置取料
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            this.ResetInPos();
            this.LoadW(nTimeout, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (OnLoadComplete != null) OnLoadComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        /// <summary>
        /// Aligner可以
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStg0to399"></param>
        /// <param name="nSlot"></param>
        /// <exception cref="SException"></exception>
        public void TakeWaferExchangeByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot)
        {
            WriteLog("Take Wafer Exchange By InterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferExchangeByInterLockW InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferExchangeByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferExchangeByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                default:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferExchangeByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, false, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Load, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop stg0~399: " + nStg0to399);
                }
            }

            this.ResetInPos();
            this.ExchW(nTimeout, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (OnLoadExchangeComplete != null) OnLoadExchangeComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        /// <summary>
        /// 為了Frame
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="armSelect"></param>
        /// <param name="nArmPos"></param>
        /// <param name="pos"></param>
        /// <param name="nSlot"></param>
        /// <exception cref="SException"></exception>
        public void TwoStepTakeWaferW(int nTimeout, enumRobotArms eRobotArms, int nArmPos, int nStg0to399, int nSlot)
        {
            WriteLog("TwoStepTakeWafer:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "MoveToStandbyByInterLock InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TwoStepTakeWafer InterlockStop pos: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TwoStepTakeWafer InterlockStop pos: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TwoStepTakeWafer InterlockStop pos: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TwoStepTakeWafer InterlockStop pos: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop pos: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Standby, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TwoStepTakeWaferW InterlockStop stg0~399: " + nStg0to399);
                }
            }
            //二段取片
            this.ResetInPos();

            int ZPosLow = 0;
            int ZPos2Top = 0;
            int ZPos = 0;
            int nSpeed = m_nSpeed;

            this.ResetProcessCompleted();
            this.SspdW(m_nAckTimeout, 10);
            this.WaitProcessCompleted(m_nAckTimeout);

            this.ResetInPos();
            this.ExtdW(m_nAckTimeout, 1, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(m_nMotionTimeout);

            this.Lifter.GetPosW(m_nAckTimeout);

            ZPosLow = Lifter.Position;

            this.ResetInPos();
            this.ExtdW(m_nAckTimeout, 2, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(m_nMotionTimeout);

            this.ResetInPos();
            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    this.UpperArm.JogW(m_nAckTimeout, -1 * nArmPos);
                    break;
                case enumRobotArms.LowerArm:
                    this.LowerArm.JogW(m_nAckTimeout, -1 * nArmPos);
                    break;
            }
            this.WaitInPos(m_nMotionTimeout);

            this.Lifter.GetPosW(m_nAckTimeout);
            ZPos2Top = this.Lifter.Position;
            ZPos = ZPos2Top - ZPosLow;

            this.ResetInPos();
            this.Lifter.JogW(m_nAckTimeout, -1 * ZPos);
            this.WaitInPos(m_nMotionTimeout);

            this.ResetInPos();
            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    this.UpperArm.JogW(m_nAckTimeout, nArmPos);
                    break;
                case enumRobotArms.LowerArm:
                    this.LowerArm.JogW(m_nAckTimeout, nArmPos);
                    break;
            }
            this.WaitInPos(m_nMotionTimeout);

            this.ResetInPos();
            this.Lifter.JogW(m_nAckTimeout, ZPos);
            this.WaitInPos(m_nMotionTimeout);

            this.ResetInPos();
            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    this.ClmpW(m_nAckTimeout, 1);
                    break;
                case enumRobotArms.LowerArm:
                    this.ClmpW(m_nAckTimeout, 2);
                    break;
            }
            this.WaitInPos(5000);

            this.ResetInPos();
            this.HomeW(m_nAckTimeout, 2, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(m_nMotionTimeout);

            this.ResetProcessCompleted();
            this.SspdW(m_nAckTimeout, nSpeed);
            this.WaitProcessCompleted(m_nAckTimeout);

            if (OnLoadComplete != null) OnLoadComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        /// <summary>
        /// ForRobotAligner取片
        /// </summary>
        public void TakeWaferAlignmentByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, SWafer WaferData = null)
        {
            WriteLog("Take Wafer Alignment By InterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferExchangeByInterLockW InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferExchangeByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferExchangeByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                default:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferExchangeByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            
            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, false, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Load, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLock InterlockStop stg0~399: " + nStg0to399);
                }
            }

            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: true  // TakeWafer = 從位置取料
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            ResetInPos();
            AlldW(GetAckTimeout, eRobotArms, nStg0to399, nSlot, 0, 0);
            WaitInPos(GetMotionTimeout);

            if (OnLoadComplete != null) OnLoadComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));

            GaldW(GetAckTimeout, eRobotArms);

        }
        public void TakeWaferAlignmentByInterLockW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, SWafer WaferData = null)
        {
            WriteLog("Take Wafer Alignment ExtXaxis By InterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferAlignmentByInterLockW_ExtXaxis InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "TakeWaferByInterLockW_ExtXaxis InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (!GParam.theInst.DicRobPos.ContainsKey(ePosition))
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferByInterLockW_ExtXaxis InterlockStop position can not find position: " + ePosition.ToString() + " " + eRobotArms + " invalid");
            }
            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                default:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW_Ext_Xaxis(nTimeout, false, ePosition, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Load, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "TakeWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399);
                }
            }

            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: true  // TakeWafer = 從位置取料
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            ResetInPos();
            AlldW(GetAckTimeout, eRobotArms, nStg0to399, nSlot, 0, 0);
            WaitInPos(GetMotionTimeout);

            if (OnLoadComplete != null) OnLoadComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));

            GaldW(GetAckTimeout, eRobotArms);

        }
        public void PutWaferAlignmentByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, SWafer WaferData = null)
        {
            WriteLog("Put Wafer Alignment ByInterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "MoveToStandbyByInterLock InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, true, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Unlaod, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399:" + nStg0to399);
                }
            }

            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: false  // PutWafer
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            this.ResetInPos();
            this.AlulW(nTimeout, eRobotArms, nStg0to399, nSlot, 0);
            this.WaitInPos(GetMotionTimeout);

            if (OnUnldComplete != null) OnUnldComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        public void PutWaferAlignmentByInterLockW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, SWafer WaferData = null)
        {
            WriteLog("Put Wafer Alignment ExtXaxis ByInterLock:Start");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (BodyNo == 2 && PinSafety == false)
            {
                SendAlmMsg(enumRobotError.Robot_Pin_Not_Safety);
                throw new SException((int)(enumRobotError.Robot_Pin_Not_Safety), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop Pin not safety stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            if (!GParam.theInst.DicRobPos.ContainsKey(ePosition))
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop position can not find position: " + ePosition.ToString() + " " + eRobotArms + " invalid");
            }

            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                    if (UpperArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    if (LowerArmWafer == null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.Empty:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW_Ext_Xaxis(nTimeout, true, ePosition, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Unlaod, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferAlignmentByInterLockW_ExtXaxis InterlockStop stg0~399:" + nStg0to399);
                }
            }

            // Add GRPC start - 設定並觸發 Motion Start HSC
            try
            {
                // 轉換手臂類型並建構 Motion Type
                var armType = MotionEventManager.MotionType.FromRobotArms(eRobotArms);
                var motionType = MotionEventManager.MotionType.BuildTransferFromPosition(
                    position: nStg0to399,
                    robotId: this.BodyNo,
                    armType: armType,
                    slotId: nSlot,
                    isLoad: false  // PutWafer
                );

                // 觸發 Motion Start
                if (WaferData == null)
                {
                    MotionEventManager.Instance.TriggerMotionStart("Robot", this.BodyNo, motionType);
                }
                else
                {
                    MotionEventManager.Instance.TriggerMotionStartWithWafer("Robot", this.BodyNo, motionType, WaferData);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GRPC motion start failed: {ex.Message}");
            }

            this.ResetInPos();
            this.AlulW(nTimeout, eRobotArms, nStg0to399, nSlot, 0);
            this.WaitInPos(GetMotionTimeout);

            if (OnUnldComplete != null) OnUnldComplete(this, new LoadUnldEventArgs(eRobotArms, nStg0to399, nSlot));
        }
        public void FlipByInterLockW(int nTimeout, int nSide, enumRobotArms eRobotArms, int nStg0to399, int nSlot)
        {
            WriteLog("Flip By InterLock:Start");
            nStg0to399 = nStg0to399 + 200;
            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "FlipByInterLockW InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }
            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "FlipByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "FlipByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has wafer");
                    }
                    break;
                default:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "FlipByInterLockW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, false, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Flip, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "FlipByInterLockW InterlockStop stg0~399: " + nStg0to399);
                }
            }


            ResetInPos();
            FlipW(GetAckTimeout, nSide, eRobotArms, nStg0to399, nSlot);
            WaitInPos(GetMotionTimeout);
        }
        /// <summary>
        /// DEMO用的旋轉放片
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStg0to399"></param>
        /// <param name="nSlot"></param>
        /// <exception cref="SException"></exception>
        public void ArmExtendShiftWaferW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot)
        {
            WriteLog("ArmExtendShiftWaferW");

            if (IsError)
            {
                SendAlmMsg(enumRobotError.InterlockStop);
                throw new SException((int)(enumRobotError.InterlockStop), "ArmExtendShiftWaferW InterlockStop is error stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            switch (eRobotArms)//檢查Finger有無Wafer
            {
                case enumRobotArms.UpperArm:
                    if (UpperArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "ArmExtendShiftWaferW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.LowerArm:
                    if (LowerArmWafer != null)
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "ArmExtendShiftWaferW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " has no wafer");
                    }
                    break;
                case enumRobotArms.BothArms:
                case enumRobotArms.Empty:
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "ArmExtendShiftWaferW InterlockStop stg0~399: " + nStg0to399 + " " + eRobotArms + " invalid");
            }

            //Home moving check INP
            this.ResetInPos();
            this.MoveToStandbyPosW(nTimeout, false, eRobotArms, nStg0to399, nSlot);
            this.WaitInPos(GetMotionTimeout);

            if (m_pStageInterlock[nStg0to399] != null)
            {
                if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Load, eRobotArms, nSlot))
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "PutWaferByInterLock InterlockStop stg0~399:" + nStg0to399);
                }
            }

            ResetInPos();
            ExtdW(nTimeout, 1, eRobotArms, nStg0to399, nSlot);
            WaitInPos(GetMotionTimeout);

            Lifter.JogW(nTimeout, 9000);
            this.WaitInPos(GetMotionTimeout);
            Lifter.GetPosW(nTimeout);

            Traverse.JogW(3000, 1000);
            this.WaitInPos(GetMotionTimeout);
            Traverse.GetPosW(3000);

            Rotater.JogW(nTimeout, 100);
            this.WaitInPos(GetMotionTimeout);
            Rotater.GetPosW(nTimeout);

            Traverse.JogW(3000, 1000);
            this.WaitInPos(GetMotionTimeout);
            Traverse.GetPosW(3000);

            Rotater.JogW(nTimeout, 100);
            this.WaitInPos(GetMotionTimeout);
            Rotater.GetPosW(nTimeout);

            Traverse.JogW(3000, 1000);
            this.WaitInPos(GetMotionTimeout);
            Traverse.GetPosW(3000);

            Rotater.JogW(nTimeout, 100);
            this.WaitInPos(GetMotionTimeout);
            Rotater.GetPosW(nTimeout);

            Lifter.JogW(nTimeout, -9000);
            this.WaitInPos(GetMotionTimeout);
            Lifter.GetPosW(nTimeout);

            ResetInPos();
            HomeW(nTimeout, 1, eRobotArms, nStg0to399, nSlot);
            WaitInPos(GetMotionTimeout);

            OnArmExtendShiftWaferComplete?.Invoke(this, true);
        }

        #region =========================== ORGN ===============================================
        private void Orgn()
        {
            _signalAck[enumRobotCommand.Orgn].Reset();
            m_Socket.SendCommand(string.Format("ORGN(2)"));
        }
        public void OrgnW(int nTimeout)
        {

            //加入震動指令
            string strCmd = string.Format("ORGN(2)");
            OnNotifyVibration?.Invoke(this, string.Format("oTRB{0}.{1}", BodyNo, strCmd));

            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.OPRCompleted].Reset();
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Orgn();
                enumRobotCommand e = enumRobotCommand.Orgn;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }

            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== HOME ===============================================
        public virtual void Home(bool HaveWafer, enumRobotArms armSelect, int nStage, int nSlot)
        {
            _signalAck[enumRobotCommand.Home].Reset();
            string str = string.Format("HOME({0},{1},{2},{3})",
                (HaveWafer == true) ? "2" : "1",
                armSelect == enumRobotArms.LowerArm ? 2 : 1,
                nStage + 1,
                nSlot);
            m_Socket.SendCommand(str);
        }
        public virtual void MoveToStandbyPosW(int nTimeout, bool HaveWafer, enumRobotArms armSelect, int nStage, int nSlot)
        {
            //加入震動指令
            string strCmd = string.Format("HOME({0},{1},{2},{3})",
                (HaveWafer == true) ? "2" : "1",
                armSelect == enumRobotArms.LowerArm ? 2 : 1,
                nStage + 1,
                nSlot);
            OnNotifyVibration?.Invoke(this, string.Format("oTRB{0}.{1}", BodyNo, strCmd));


            _signalSubSequence.Reset();


            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Home(HaveWafer, armSelect, nStage, nSlot);
                enumRobotCommand e = enumRobotCommand.Home;

                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
            }
            _signalSubSequence.Set();
        }
        public virtual void MoveToStandbyPosW_Ext_Xaxis(int nTimeout, bool HaveWafer, enumPosition ePosition, enumRobotArms armSelect, int nStage, int nSlot)
        {
            //加入震動指令
            string strCmd = string.Format("HOME({0},{1},{2},{3})",
                (HaveWafer == true) ? "2" : "1",
                armSelect == enumRobotArms.LowerArm ? 2 : 1,
                nStage + 1,
                nSlot);
            OnNotifyVibration?.Invoke(this, string.Format("oTRB{0}.{1}", BodyNo, strCmd));

            _signalSubSequence.Reset();


            if (!m_bSimulate)
            {
                RobPos pos = null;
                if (GParam.theInst.DicRobPos.ContainsKey(ePosition))
                    pos = GParam.theInst.DicRobPos[ePosition];
                else
                    SendAlmMsg(enumRobotError.InterlockStop);

                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Home(HaveWafer, armSelect, nStage, nSlot);
                enumRobotCommand e = enumRobotCommand.Home;

                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }

                if (!ExtXaxisDisable)
                {
                    TBL_560.ResetInPos();
                    if (armSelect == enumRobotArms.UpperArm)
                        TBL_560.AxisMabsW(nTimeout, pos.Pos_ARM1);
                    else if (armSelect == enumRobotArms.LowerArm)
                        TBL_560.AxisMabsW(nTimeout, pos.Pos_ARM2);
                    TBL_560.WaitInPos(m_nMotionTimeout);
                }
                    
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
            }
            _signalSubSequence.Set();
        }

        public virtual void Home()
        {
            _signalAck[enumRobotCommand.Home].Reset();
            m_Socket.SendCommand("HOME");
        }
        public virtual void MoveToStandbyPosW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Home();
                enumRobotCommand e = enumRobotCommand.Home;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
            }
            _signalSubSequence.Set();
        }

        private void Home(int id, enumRobotArms armSelect, int nStage, int nSlot)
        {
            try
            {
                _signalAck[enumRobotCommand.Home].Reset();
                string str = string.Format("HOME({0},{1},{2},{3})",
                    id,
                    armSelect == enumRobotArms.LowerArm ? 2 : 1,
                    nStage + 1,
                    nSlot);
                m_Socket.SendCommand(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void HomeW(int nTimeout, int id, enumRobotArms armSelect, int nStage, int nSlot)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Home(id, armSelect, nStage, nSlot);

                enumRobotCommand e = enumRobotCommand.Home;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                Thread.Sleep(500);
            }
            _signalSubSequence.Set();
        }

        #endregion 

        #region =========================== LOAD ===============================================      
        private void Load(enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg = 0)
        {
            try
            {
                _signalAck[enumRobotCommand.TakeWafer].Reset();
                string str = string.Format("LOAD({0},{1},{2},{3})",
                    TransferSelectArmString(armSelect),
                    nStg0to399 + 1,
                    nSlot,
                    nflg);
                m_Socket.SendCommand(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void LoadW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg = 0)
        {
            //加入震動指令
            string strCmd = string.Format("LOAD({0},{1},{2},{3})",
                    TransferSelectArmString(armSelect),
                    nStg0to399 + 1,
                    nSlot,
                    nflg);
            OnNotifyVibration?.Invoke(this, string.Format("oTRB{0}.{1}_{2},{3}&CarrierID", BodyNo, strCmd, UpperArmWafer == null ? 0 : 1, LowerArmWafer == null ? 0 : 1));

            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Load(armSelect, nStg0to399, nSlot, nflg);
                enumRobotCommand e = enumRobotCommand.TakeWafer;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, BodyNo == 1 ? 2000 : 3000);


            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== UNLD ===============================================
        /// <summary>
        /// 放片 [UNLD]
        /// </summary>
        /// <param name="nTimeout">ACK等待逾時</param>
        /// <param name="armSelect">選擇的Finger</param>
        /// <param name="nStage">選擇的Stage</param>
        /// <param name="nSlot">選擇的Slot</param>
        private void Unld(enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg = 0)
        {
            try
            {
                _signalAck[enumRobotCommand.PutWafer].Reset();
                string str = string.Format("UNLD({0},{1},{2},{3})",
                    TransferSelectArmString(armSelect),
                    nStg0to399 + 1,
                    nSlot,
                    nflg);
                m_Socket.SendCommand(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void UnldW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg = 0)//nStage0~399
        {
            //加入震動指令
            string strCmd = string.Format("UNLD({0},{1},{2},{3})",
                    TransferSelectArmString(armSelect),
                    nStg0to399 + 1,
                    nSlot,
                    nflg);
            OnNotifyVibration?.Invoke(this, string.Format("oTRB{0}.{1}_{2},{3}&CarrierID", BodyNo, strCmd, UpperArmWafer == null ? 0 : 1, LowerArmWafer == null ? 0 : 1));

            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Unld(armSelect, nStg0to399, nSlot, nflg);
                enumRobotCommand e = enumRobotCommand.PutWafer;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, BodyNo == 1 ? 2500 : 5000);

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== TRNS ===============================================
        private void Trns()
        {
            try
            {
                _signalAck[enumRobotCommand.TransferWafer].Reset();
                m_Socket.SendCommand(string.Format("UNLD"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void TrnsW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Trns();
                enumRobotCommand e = enumRobotCommand.TransferWafer;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                Thread.Sleep(500);
                //if (OnSimulateUCLM != null)
                //    OnSimulateUCLM(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== EXTD ===============================================
        private void Extd(int id, enumRobotArms armSelect, int nStage, int nSlot, int flg = 0)
        {
            try
            {
                _signalAck[enumRobotCommand.ExtendingArm].Reset();

                string str = string.Format("EXTD({0},{1},{2},{3},{4})",
                     id,
                    TransferSelectArmString(armSelect),
                    nStage + 1,
                    nSlot,
                    flg);
                m_Socket.SendCommand(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void ExtdW(int nTimeout, int id, enumRobotArms armSelect, int nStage, int nSlot, int flg = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Extd(id, armSelect, nStage, nSlot, flg);

                enumRobotCommand e = enumRobotCommand.ExtendingArm;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                Thread.Sleep(500);
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== CLMP ===============================================
        private void Clmp(enumRobotArms armSelect, int nCheckTime = 0)
        {
            try
            {
                string strCheckTimeFormat = string.Format("{0},,{1}", armSelect == enumRobotArms.UpperArm ? "4" : "5", nCheckTime);
                _signalAck[enumRobotCommand.VacuumOn].Reset();

                switch (armSelect)
                {
                    case enumRobotArms.UpperArm:
                        m_Socket.SendCommand(string.Format("CLMP({0})", nCheckTime > 0 ? strCheckTimeFormat : "1")); break; //upper arm
                    case enumRobotArms.LowerArm:
                        m_Socket.SendCommand(string.Format("CLMP({0})", nCheckTime > 0 ? strCheckTimeFormat : "2")); break; //lower arm
                    case enumRobotArms.BothArms:
                        m_Socket.SendCommand(string.Format("CLMP({0})", nCheckTime > 0 ? strCheckTimeFormat : "3")); break; //both arms
                }
            }
            catch (Exception ex)
            {
                WriteLog("<Exception>" + ex);
            }
        }
        public void ClmpW(int nTimeout, enumRobotArms armSelect, int nCheckTime = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Clmp(armSelect, nCheckTime);
                enumRobotCommand e = enumRobotCommand.VacuumOn;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();

        }

        private void Clmp(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.VacuumOn].Reset();
                m_Socket.SendCommand(string.Format("CLMP({0})", nVariable));
            }
            catch (Exception ex)
            {
                WriteLog("<Exception>" + ex);
            }
        }
        public void ClmpW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Clmp(nVariable);
                enumRobotCommand e = enumRobotCommand.VacuumOn;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== UCLM ===============================================
        private void Uclm(enumRobotArms armSelect, int nCheckTime = 0)
        {
            try
            {
                _signalAck[enumRobotCommand.VacuumOff].Reset();
                switch (armSelect)
                {
                    case enumRobotArms.UpperArm: m_Socket.SendCommand("UCLM(1)"); break;
                    case enumRobotArms.LowerArm: m_Socket.SendCommand("UCLM(2)"); break;
                    case enumRobotArms.BothArms: m_Socket.SendCommand("UCLM(3)"); break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void UclmW(int nTimeout, enumRobotArms armSelect, int nCheckTime = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Uclm(armSelect, nCheckTime);
                enumRobotCommand e = enumRobotCommand.VacuumOff;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }

        private void Uclm(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.VacuumOff].Reset();
                m_Socket.SendCommand(string.Format("UCLM({0})", nVariable));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void UclmW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Uclm(nVariable);
                enumRobotCommand e = enumRobotCommand.VacuumOff;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== WMAP ===============================================
        private void Wmap(int nStg0to399)
        {
            try
            {
                _signalAck[enumRobotCommand.Mapping].Reset();
                m_Socket.SendCommand(string.Format("WMAP({0})", nStg0to399 + 1));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void WmapW(int nTimeout, int nStg0to399)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                if (IsError)
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "WmapW InterlockStop is error stg0~399: " + nStg0to399 + " " + enumRobotArms.LowerArm + " invalid");
                }

                if (LowerArmWafer != null) //因為是用下Arm mapping，檢查Lower Finger有無Wafer
                {
                    SendAlmMsg(enumRobotError.InterlockStop);
                    throw new SException((int)(enumRobotError.InterlockStop), "WmapW InterlockStop stg0~399: " + nStg0to399 + " " + enumRobotArms.LowerArm + " has wafer");
                }

                if (m_pStageInterlock[nStg0to399] != null)
                {
                    if (m_pStageInterlock[nStg0to399](this, enumRobotAction.Standby, enumRobotArms.LowerArm, 0))//因為是用下Arm mapping，檢查下Arm無Wafer
                    {
                        SendAlmMsg(enumRobotError.InterlockStop);
                        throw new SException((int)(enumRobotError.InterlockStop), "WmapW InterlockStop stg0~399: " + nStg0to399);
                    }
                }

                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Wmap(nStg0to399);
                enumRobotCommand e = enumRobotCommand.Mapping;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }

                //if (OnLoadComplete != null) OnLoadComplete(this, new LoadUnldEventArgs(enumRobotArms.LowerArm, nStg0to399, 0));
            }
            else
            {
                SpinWait.SpinUntil(() => false, 3000);
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== UTRN ===============================================
        private void Utrn(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.TransferWafer2].Reset();
                m_Socket.SendCommand(string.Format("UTRN(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void UtrnW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();


                Utrn(nMode);

                enumRobotCommand e = enumRobotCommand.TransferWafer2;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MGET ===============================================
        private void Mget(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.CarryOutWafer].Reset();
                m_Socket.SendCommand(string.Format("MGET(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void MgetW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Mget(nMode);

                enumRobotCommand e = enumRobotCommand.CarryOutWafer;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MGT1 ===============================================
        private void Mgt1(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.CarryOutWafer1].Reset();
                m_Socket.SendCommand(string.Format("MGT1(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void Mgt1W(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Mgt1(nMode);

                enumRobotCommand e = enumRobotCommand.CarryOutWafer1;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MGT2 ===============================================
        private void Mgt2(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.CarryOutWafer2].Reset();
                m_Socket.SendCommand(string.Format("MGT2(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void Mgt2W(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Mgt2(nMode);

                enumRobotCommand e = enumRobotCommand.CarryOutWafer2;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MPUT ===============================================
        private void Mput(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.CarryInWafer].Reset();
                m_Socket.SendCommand(string.Format("MPUT(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void MputW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Mput(nMode);

                enumRobotCommand e = enumRobotCommand.CarryInWafer;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MPT1 ===============================================
        private void Mpt1(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.CarryInWafer1].Reset();
                m_Socket.SendCommand(string.Format("MPT1(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void Mpt1W(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Mpt1(nMode);

                enumRobotCommand e = enumRobotCommand.CarryInWafer1;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MPT2 ===============================================
        private void Mpt2(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.CarryInWafer2].Reset();
                m_Socket.SendCommand(string.Format("MPT2(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void Mpt2W(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Mpt2(nMode);

                enumRobotCommand e = enumRobotCommand.CarryInWafer2;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== WCHK ===============================================
        private void Wchk(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.CheckWaferInSlot].Reset();
                m_Socket.SendCommand(string.Format("MPUT(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void WchkW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Wchk(nMode);

                enumRobotCommand e = enumRobotCommand.CheckWaferInSlot;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== ALEX ===============================================
        private void Alex(enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg, int naf)
        {
            try
            {
                _signalAck[enumRobotCommand.ALEX].Reset();
                string str = string.Format("ALEX({0},{1},{2},{3},{4})",
                 TransferSelectArmString(armSelect),
                 nStg0to399 + 1,
                 nSlot,
                 nflg,
                 naf);
                m_Socket.SendCommand(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void AlexW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg, int naf)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Alex(armSelect, nStg0to399, nSlot, nflg, naf);
                enumRobotCommand e = enumRobotCommand.ALEX;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ALLD ===============================================
        private void Alld(enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg, int naf)
        {
            try
            {
                _signalAck[enumRobotCommand.ALLD].Reset();
                string str = string.Format("ALLD({0},{1},{2},{3},{4})",
                   TransferSelectArmString(armSelect),
                   nStg0to399 + 1,
                   nSlot,
                   nflg,
                   naf);
                m_Socket.SendCommand(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void AlldW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg, int naf)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Alld(armSelect, nStg0to399, nSlot, nflg, naf);

                enumRobotCommand e = enumRobotCommand.ALLD;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ALUL ===============================================
        private void Alul(enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg)
        {
            try
            {
                _signalAck[enumRobotCommand.ALUL].Reset();
                string str = string.Format("ALUL({0},{1},{2},{3})",
                 TransferSelectArmString(armSelect),
                 nStg0to399 + 1,
                 nSlot,
                 nflg);
                m_Socket.SendCommand(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void AlulW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Alul(armSelect, nStg0to399, nSlot, nflg);

                enumRobotCommand e = enumRobotCommand.ALUL;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion          

        #region =========================== ALGT ===============================================
        private void Algt(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.ALGT].Reset();
                m_Socket.SendCommand(string.Format("ALGT(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void AlgtW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Algt(nMode);

                enumRobotCommand e = enumRobotCommand.ALGT;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ALEA ===============================================
        private void Alea(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.ALEA].Reset();
                m_Socket.SendCommand(string.Format("ALEA(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void AleaW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Algt(nMode);

                enumRobotCommand e = enumRobotCommand.ALEA;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ALMV ===============================================
        private void Almv(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.ALMV].Reset();
                m_Socket.SendCommand(string.Format("ALMV(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void AlmvW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();


                Algt(nMode);

                enumRobotCommand e = enumRobotCommand.ALMV;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ZMOV ===============================================
        private void Zmov(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.ZMOV].Reset();
                m_Socket.SendCommand(string.Format("ZMOV(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void ZmovW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Zmov(nMode);

                enumRobotCommand e = enumRobotCommand.ZMOV;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MSSC ===============================================
        private void Mssc(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.MSSC].Reset();
                m_Socket.SendCommand(string.Format("MSSC(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void MsscW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Mssc(nMode);

                enumRobotCommand e = enumRobotCommand.MSSC;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== EXCC ===============================================
        private void Excc(int nVariable)
        {
            try
            {
                _signalAck[enumRobotCommand.EXCC].Reset();
                m_Socket.SendCommand(string.Format("EXCC(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void ExccW(int nTimeout, int nMode = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();

                Excc(nMode);

                enumRobotCommand e = enumRobotCommand.EXCC;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //if (OnSimulateCLMP != null)
                //    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== FLIP ===============================================
        private void Flip(int nSide, enumRobotArms armSelect, int nStage, int nSlot)
        {
            try
            {
                _signalAck[enumRobotCommand.Flip].Reset();
                string str = string.Format("FLIP({0},{1},{2},{3},{4})",
                    nSide,
                    armSelect,
                    nStage + 1,
                    nSlot);
                m_Socket.SendCommand(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void FlipW(int nTimeout, int nSide, enumRobotArms armSelect, int nStage, int nSlot)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Flip(nSide, armSelect, nStage, nSlot);
                enumRobotCommand e = enumRobotCommand.Flip;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, BodyNo == 1 ? 2000 : 3000);
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== EVNT ===============================================
        private void Event()
        {
            _signalAck[enumRobotCommand.SetEvent].Reset();
            m_Socket.SendCommand("EVNT(0,1)");
        }
        public void EventW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Event();
                enumRobotCommand e = enumRobotCommand.SetEvent;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== RSTA ===============================================
        private void Reset(int nReset)
        {
            _signalAck[enumRobotCommand.Reset].Reset();
            m_Socket.SendCommand(string.Format("RSTA(" + nReset + ")"));
        }
        public void ResetW(int nTimeout, int nReset = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Reset(nReset);
                enumRobotCommand e = enumRobotCommand.Reset;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== INIT ===============================================
        private void Init()
        {
            _signalAck[enumRobotCommand.Initialize].Reset();
            m_Socket.SendCommand(string.Format("INIT"));
        }
        public void InitW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.Remote].Reset();
                Init();
                enumRobotCommand e = enumRobotCommand.Initialize;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STOP ===============================================
        private void Stop()
        {
            _signalAck[enumRobotCommand.Stop].Reset();
            m_Socket.SendCommand(string.Format("STOP"));
        }
        public void StopW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Stop();
                enumRobotCommand e = enumRobotCommand.Stop;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }

                if (!ExtXaxisDisable)
                    TBL_560.StopW(nTimeout);
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== PAUS ===============================================
        private void Paus()
        {
            _signalAck[enumRobotCommand.Pause].Reset();
            m_Socket.SendCommand(string.Format("PAUS"));
        }
        public void PausW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Paus();
                enumRobotCommand e = enumRobotCommand.Pause;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MODE ===============================================
        private void Mode(int nMode)
        {
            _signalAck[enumRobotCommand.SetMode].Reset();
            m_Socket.SendCommand(string.Format("MODE(" + nMode + ")"));
        }
        public void ModeW(int nTimeout, int nMode)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.Remote].Reset();
                Mode(nMode);
                enumRobotCommand e = enumRobotCommand.SetMode;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }

                if (!ExtXaxisDisable)
                    TBL_560.ModeW(nTimeout, nMode);
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== WTDT ===============================================
        private void Wtdt()
        {
            _signalAck[enumRobotCommand.StoreDate].Reset();
            m_Socket.SendCommand(string.Format("WTDT"));
        }
        public void WtdtW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Wtdt();
                enumRobotCommand e = enumRobotCommand.StoreDate;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== RTDT ===============================================
        private void Rtdt()
        {
            _signalAck[enumRobotCommand.TransferDate].Reset();
            m_Socket.SendCommand(string.Format("RTDT"));
        }
        public void RtdtW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Rtdt();
                enumRobotCommand e = enumRobotCommand.TransferDate;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SSPD ===============================================
        private void Sspd(int nSpeed)
        {
            _signalAck[enumRobotCommand.SetSpeed].Reset();
            m_Socket.SendCommand(string.Format("SSPD(" + nSpeed + ")"));
        }
        public void SspdW(int nTimeout, int nSpeed)
        {
            //if (BodyNo == 1)
            //    nSpeed = 10;
            //if (BodyNo == 2)
            //    nSpeed = 5;

            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.ProcessCompleted].Reset();
                Sspd(nSpeed);
                enumRobotCommand e = enumRobotCommand.SetSpeed;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }

                if (!ExtXaxisDisable)
                    TBL_560.SspdW(nTimeout, nSpeed);
                
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SPOS ===============================================
        private void Spos(int nVariable)
        {
            _signalAck[enumRobotCommand.SetPos].Reset();
            m_Socket.SendCommand(string.Format("SPOS(" + nVariable + ")"));
        }
        public void SposW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Spos(nVariable);
                enumRobotCommand e = enumRobotCommand.SetPos;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== TORQ ===============================================
        private void Torq(int nVariable)
        {
            _signalAck[enumRobotCommand.SetTorque].Reset();
            m_Socket.SendCommand(string.Format("TORQ(" + nVariable + ")"));
        }
        public void TorqW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Torq(nVariable);
                enumRobotCommand e = enumRobotCommand.SetTorque;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== EXCT ===============================================
        private void Exct(int nVariable)
        {
            _signalAck[enumRobotCommand.ExcitationControl].Reset();
            m_Socket.SendCommand(string.Format("EXCT(" + nVariable + ")"));
        }
        public void ExctW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Exct(nVariable);
                enumRobotCommand e = enumRobotCommand.ExcitationControl;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }

                if (!ExtXaxisDisable)
                    TBL_560.ExctW(nTimeout, nVariable);//0:Turn off 1:Turn on
                
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== BRAK ===============================================
        private void Brak(int nVariable)
        {
            _signalAck[enumRobotCommand.BRAK].Reset();
            m_Socket.SendCommand(string.Format("BRAK(" + nVariable + ")"));
        }
        public void BrakW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Brak(nVariable);
                enumRobotCommand e = enumRobotCommand.BRAK;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SVAC ===============================================
        private void Svac(int nVariable)
        {
            _signalAck[enumRobotCommand.SVAC].Reset();
            m_Socket.SendCommand(string.Format("SVAC(" + nVariable + ")"));
        }
        public void SvacW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Svac(nVariable);
                enumRobotCommand e = enumRobotCommand.SVAC;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SPOT ===============================================
        private void Spot(int nVariable)
        {
            _signalAck[enumRobotCommand.SPOT].Reset();
            m_Socket.SendCommand(string.Format("SPOT(" + nVariable + ")"));
        }
        public void SpotW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumRobotSignalTable.MotionCompleted].Reset();
                Spot(nVariable);
                enumRobotCommand e = enumRobotCommand.SPOT;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STAT ===============================================
        private void Stat()
        {
            _signalAck[enumRobotCommand.GetStatus].Reset();
            m_Socket.SendCommand(string.Format("STAT"));
        }
        public void StatW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Stat();
                enumRobotCommand e = enumRobotCommand.GetStatus;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPIO ===============================================
        private void Gpio()
        {
            _signalAck[enumRobotCommand.GetIO].Reset();
            m_Socket.SendCommand(string.Format("GPIO"));
        }
        public void GpioW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gpio();
                enumRobotCommand e = enumRobotCommand.GetIO;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GMAP ===============================================
        private void Gmap(int nStg0to399)
        {
            _signalAck[enumRobotCommand.GetMappingData].Reset();
            m_Socket.SendCommand(string.Format("GMAP({0})", nStg0to399 + 1));
        }
        public void GmapW(int nTimeout, int nStg0to399)
        {
            _signalSubSequence.Reset();

            if (!m_bSimulate)
            {
                Gmap(nStg0to399);
                enumRobotCommand e = enumRobotCommand.GetMappingData;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                m_strMappingData = "1111111111111111111111111"; //如果是LoadPortRC550模擬時來這邊調整Mapping結果
                m_strMappingData = "0000000000000000000000000";
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== RCA2 =======================================
        private void Rca2(int nVariable)
        {
            _signalAck[enumRobotCommand.GetRAC2].Reset();
            m_Socket.SendCommand(string.Format("RCA2.GPOS(" + nVariable + ")"));
        }
        public void Rca2W(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Rca2(nVariable);
                enumRobotCommand e = enumRobotCommand.GetRAC2;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send RAC2 command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send RAC2 command is Canecl. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {
                //SWafer waferShow;
                //_MappingData = "";
                //if (Lots.Count > 0)
                //{
                //    for (int nSlot = 1; nSlot <= 25; nSlot++)
                //    {
                //        GetWafer(nSlot, out waferShow);
                //        if (waferShow == null)//empty
                //            _MappingData = _MappingData + "0";
                //        else
                //        {
                //            if (waferShow.Position == SWafer.enumPosition.Loader1 || waferShow.Position == SWafer.enumPosition.Loader2)
                //                _MappingData = _MappingData + "1";
                //            else
                //                _MappingData = _MappingData + "0";
                //        }
                //    }
                //}
                //else
                //{
                //    MappingData = "1111111111111111111111111";
                //}
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GVER ===============================================
        private void Gver()
        {
            _signalAck[enumRobotCommand.GetVersion].Reset();
            m_Socket.SendCommand(string.Format("GVER"));
        }
        public void GverW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gver();
                enumRobotCommand e = enumRobotCommand.GetVersion;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GLOG ===============================================
        private void Glog()
        {
            _signalAck[enumRobotCommand.GetLog].Reset();
            m_Socket.SendCommand(string.Format("GLOG"));
        }
        public void GlogW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Glog();
                enumRobotCommand e = enumRobotCommand.GetLog;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STIM ===============================================
        private void Stim()
        {
            _signalAck[enumRobotCommand.SetDateTime].Reset();
            m_Socket.SendCommand("STIM(" + DateTime.Now.ToString("yyyy, MM, dd, HH, mm, ss") + ")");
        }
        public void StimW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Stim();
                enumRobotCommand e = enumRobotCommand.SetDateTime;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GTIM ===============================================
        private void Gtim()
        {
            _signalAck[enumRobotCommand.GetDateTime].Reset();
            m_Socket.SendCommand("GTIM");
        }
        public void GtimW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gtim();
                enumRobotCommand e = enumRobotCommand.GetDateTime;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPOS ===============================================
        private void Gpos()
        {
            _signalAck[enumRobotCommand.GetPos].Reset();
            m_Socket.SendCommand(string.Format("GPOS"));
        }
        public void GposW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gpos();
                enumRobotCommand e = enumRobotCommand.GetPos;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GWID ===============================================
        private void Gwid()
        {
            _signalAck[enumRobotCommand.GetWaferID].Reset();
            m_Socket.SendCommand(string.Format("GWID"));
        }
        public void GwidW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gwid();
                enumRobotCommand e = enumRobotCommand.GetWaferID;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== TDST ===============================================
        private void Tdst()
        {
            _signalAck[enumRobotCommand.TDST].Reset();
            m_Socket.SendCommand(string.Format("TDST"));
        }
        public void TdstW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Tdst();
                enumRobotCommand e = enumRobotCommand.TDST;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MOVT ===============================================
        private void Movt()
        {
            _signalAck[enumRobotCommand.MOVT].Reset();
            m_Socket.SendCommand(string.Format("MOVT"));
        }
        public void MovtW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Movt();
                enumRobotCommand e = enumRobotCommand.MOVT;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GVAC ===============================================
        private void Gvac()
        {
            _signalAck[enumRobotCommand.GVAC].Reset();
            m_Socket.SendCommand(string.Format("GVAC"));
        }
        public void GvacW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gvac();
                enumRobotCommand e = enumRobotCommand.GVAC;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== EXST ===============================================
        private void Exst()
        {
            _signalAck[enumRobotCommand.EXST].Reset();
            m_Socket.SendCommand(string.Format("EXST"));
        }
        public void ExstW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Exst();
                enumRobotCommand e = enumRobotCommand.EXST;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GCLM ===============================================
        private void Gclm()
        {
            _signalAck[enumRobotCommand.GCLM].Reset();
            m_Socket.SendCommand(string.Format("GCLM"));
        }
        public void GclmW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gclm();
                enumRobotCommand e = enumRobotCommand.GCLM;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GCHK ===============================================
        private void Gchk(int nSsiz)
        {
            _signalAck[enumRobotCommand.GCHK].Reset();
            m_Socket.SendCommand(string.Format("GCHK(" + nSsiz + ")"));
        }
        public void SsizW(int nTimeout, int nSsiz = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gchk(nSsiz);
                enumRobotCommand e = enumRobotCommand.GCHK;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== WAIT ===============================================
        private void Wait(int nSsiz)
        {
            _signalAck[enumRobotCommand.WAIT].Reset();
            m_Socket.SendCommand(string.Format("WAIT(" + nSsiz + ")"));
        }
        public void WaitW(int nTimeout, int nSsiz = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Wait(nSsiz);
                enumRobotCommand e = enumRobotCommand.WAIT;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== TDSA ===============================================
        private void Tdsa(int nSsiz)
        {
            _signalAck[enumRobotCommand.TDSA].Reset();
            m_Socket.SendCommand(string.Format("TDSA(" + nSsiz + ")"));
        }
        public void TdsaW(int nTimeout, int nSsiz = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Tdsa(nSsiz);
                enumRobotCommand e = enumRobotCommand.TDSA;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GAEX ===============================================
        private void Gaex(int nSsiz)
        {
            _signalAck[enumRobotCommand.GAEX].Reset();
            m_Socket.SendCommand(string.Format("GAEX(" + nSsiz + ")"));
        }
        public void GaexW(int nTimeout, int nSsiz = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gaex(nSsiz);
                enumRobotCommand e = enumRobotCommand.GAEX;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GALD ===============================================
        private void Gald(enumRobotArms armSelect)
        {
            _signalAck[enumRobotCommand.GALD].Reset();
            m_Socket.SendCommand(string.Format("GALD(" + TransferSelectArmString(armSelect) + ",1)"));
        }
        public void GaldW(int nTimeout, enumRobotArms armSelect)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gald(armSelect);
                enumRobotCommand e = enumRobotCommand.GALD;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SDRV ===============================================
        private void Sdrv(int nSsiz)
        {
            _signalAck[enumRobotCommand.SDRV].Reset();
            m_Socket.SendCommand(string.Format("SDRV(" + nSsiz + ")"));
        }
        public void SdrvW(int nTimeout, int nSsiz = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Sdrv(nSsiz);
                enumRobotCommand e = enumRobotCommand.SDRV;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STDT ===============================================
        private void Stdt(enumRobotCommand Command, enumRobotDataType eType, int nStage, int nPos, string Value)
        {
            _signalAck[Command].Reset();
            m_Socket.SendCommand(string.Format(eType.ToString() + ".STDT[" + nStage + "]" + "[" + nPos + "]=" + Value));
        }
        public void StdtW(int nTimeout, enumRobotDataType eType, int nStage, int nPos, string Value)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                enumRobotCommand Command;
                switch (eType)
                {
                    case enumRobotDataType.DTRB: Command = enumRobotCommand.DtrbSTDT; break;
                    case enumRobotDataType.DTUL: Command = enumRobotCommand.DtulSTDT; break;
                    case enumRobotDataType.DEQU: Command = enumRobotCommand.DequSTDT; break;
                    case enumRobotDataType.DRCI: Command = enumRobotCommand.DrciSTDT; break;
                    case enumRobotDataType.DRCS: Command = enumRobotCommand.DrcsSTDT; break;
                    case enumRobotDataType.DMNT: Command = enumRobotCommand.DmntSTDT; break;
                    case enumRobotDataType.DMPR: Command = enumRobotCommand.DmprSTDT; break;
                    case enumRobotDataType.DCFG: Command = enumRobotCommand.DcfgSTDT; break;
                    case enumRobotDataType.DAPM: Command = enumRobotCommand.DapmSTDT; break;
                    default: throw new SException((int)enumRobotError.ProgramError, string.Format("Send STDT command and Command is not default."));
                }

                Stdt(Command, eType, nStage, nPos, Value);

                if (!_signalAck[Command].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send STDT command and wait Ack was timeout. [{0}]", _dicCmdsTable[Command]));
                }
                if (_signalAck[Command].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send STDT command is Canecl. [{0}]", _dicCmdsTable[Command]));
                }
            }
            _signalSubSequence.Set();
        }



        private void Stdt(enumRobotCommand Command, enumRobotDataType eType, int nIdex, string strValue)
        {
            _signalAck[Command].Reset();
            m_Socket.SendCommand(string.Format("{0}.STDT[{1}]={2}", eType, nIdex, strValue));
        }
        public void StdtW(int nTimeout, enumRobotDataType eType, int nIdex, string strValue)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                enumRobotCommand Command;
                switch (eType)
                {
                    case enumRobotDataType.DTRB: Command = enumRobotCommand.DtrbSTDT; break;
                    case enumRobotDataType.DTUL: Command = enumRobotCommand.DtulSTDT; break;
                    case enumRobotDataType.DEQU: Command = enumRobotCommand.DequSTDT; break;
                    case enumRobotDataType.DRCI: Command = enumRobotCommand.DrciSTDT; break;
                    case enumRobotDataType.DRCS: Command = enumRobotCommand.DrcsSTDT; break;
                    case enumRobotDataType.DMNT: Command = enumRobotCommand.DmntSTDT; break;
                    case enumRobotDataType.DMPR: Command = enumRobotCommand.DmprSTDT; break;
                    case enumRobotDataType.DCFG: Command = enumRobotCommand.DcfgSTDT; break;
                    case enumRobotDataType.DAPM: Command = enumRobotCommand.DapmSTDT; break;
                    default: throw new SException((int)enumRobotError.ProgramError, string.Format("Send STDT command and Command is not default."));
                }

                Stdt(Command, eType, nIdex, strValue);

                if (!_signalAck[Command].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send STDT command and wait Ack was timeout. [{0}]", _dicCmdsTable[Command]));
                }
                if (_signalAck[Command].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send STDT command is Canecl. [{0}]", _dicCmdsTable[Command]));
                }
            }
            _signalSubSequence.Set();
        }


        #endregion 

        #region =========================== GTDT ===============================================
        /// <summary>
        /// 原點復歸 [GTDT]
        /// </summary>
        /// <param name="nTimeout">ACK等待逾時</param>
        /// <param name="bCheckWafer">是否檢查wafer</param>
        private void Gtdt(enumRobotCommand Command, enumRobotDataType eType, int nStage)
        {
            _signalAck[Command].Reset();
            m_Socket.SendCommand(string.Format(eType.ToString() + ".GTDT[" + nStage + "]"));
        }
        private void Gtdt(enumRobotCommand Command, enumRobotDataType eType)
        {
            _signalAck[Command].Reset();
            m_Socket.SendCommand(string.Format(eType.ToString() + ".GTDT"));
        }
        public void GtdtW(int nTimeout, enumRobotDataType eType, int nStage = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                enumRobotCommand Command;
                switch (eType)
                {
                    case enumRobotDataType.DTRB:
                        Command = enumRobotCommand.DtrbGTDT;
                        Gtdt(Command, eType, nStage);//0~399
                        break;
                    case enumRobotDataType.DTUL:
                        Command = enumRobotCommand.DtulGTDT;
                        Gtdt(Command, eType, nStage);//0~399
                        break;
                    case enumRobotDataType.DEQU:
                        Command = enumRobotCommand.DequGTDT;
                        Gtdt(Command, eType);
                        break;
                    case enumRobotDataType.DRCI:
                        Command = enumRobotCommand.DrciGTDT;
                        Gtdt(Command, eType, nStage);//0~4
                        break;
                    case enumRobotDataType.DRCS:
                        Command = enumRobotCommand.DrcsGTDT;
                        Gtdt(Command, eType, nStage);//0~4
                        break;
                    case enumRobotDataType.DMNT:
                        Command = enumRobotCommand.DmntGTDT;
                        Gtdt(Command, eType, nStage);//0~4
                        break;
                    case enumRobotDataType.DMPR:
                        Command = enumRobotCommand.DmprGTDT;
                        Gtdt(Command, eType, nStage);//0~399
                        break;
                    case enumRobotDataType.DCFG:
                        Command = enumRobotCommand.DcfgGTDT;
                        Gtdt(Command, eType, nStage);//0~399
                        break;
                    case enumRobotDataType.DAPM:
                        Command = enumRobotCommand.DapmGTDT;
                        Gtdt(Command, eType, nStage);//0~2
                        break;
                    default: throw new SException((int)enumRobotError.ProgramError, string.Format("Send STDT command and Command is not default."));
                }

                if (!_signalAck[Command].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send GTDT command and wait Ack was timeout. [{0}]", _dicCmdsTable[Command]));
                }
                if (_signalAck[Command].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send GTDT command is Canecl. [{0}]", _dicCmdsTable[Command]));
                }
            }
            else
            {
                switch (eType)
                {
                    case enumRobotDataType.DEQU:
                        AnalysisDEQU_GTDT("\"\", 16777343, 0, 12, 000300.0, 000300.0, 0, 1, 49413, 0, 2, 2, 2, 2, 0, 4, 1052672, 0, 1, 3, 300, 20000, 3000, 3000, 50000, 8000, 0, 100000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 258, 0, 0, 0, 0, 3000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 12000, -938928980,\"\"");
                        //DEQUData[15] = ((int)enumDEQU_15_waferSearch.UpperLowerFinger).ToString();//模擬FINGER MAPPING
                        break;
                    case enumRobotDataType.DTRB:
                        AnalysisDTRB_GTDT("-0000003800,-0000003800,+0000037000,+0000046800,+0000099900,+0000099900,+0000428353,+0000428346,+0000000000,+0000000000,+0000007000,+0000010000,025,+0000000000,+0000000000,+0000000000,+0000000000,500000,500000,003000,010000,500000,000000,+0000000000,+0000000000,\"\"");
                        break;
                    case enumRobotDataType.DTUL:
                        AnalysisDTUL_GTDT("+0000000000,+0000000000,+0000000000,+0000010000,+0000000000,+0000000000,+0000000000,-0000002000,+0000000000,+0000000000,+0000007000,+0000010000,000,+0000000000,+0000000000,+0000000000,+0000000000,050000,150000,003000,050000,120000,000000,\"\"");
                        break;
                    case enumRobotDataType.DCFG:
                        AnalysisDCFG_GTDT("\"\",\"\",\"\",\"\",\"\",00000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000");
                        break;
                    case enumRobotDataType.DMPR:
                        AnalysisDMPR_GTDT("+0000001300,020,+0000000200,+0000001300,+0000000000,00,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,1,000,+0000000000,+0000000000,+0000000000,+0000030000,+0000500000,+0000200000,+0000000000,+0000000000,+0000000000,+0000000000,+0000000000,\"\"");
                        DMPRData[13] = "25";//slot number

                        break;

                }


            }

            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ABSC ===============================================
        /// <summary>
        /// 原點復歸 [ABSC]
        /// </summary>
        /// <param name="nTimeout">ACK等待逾時</param>
        /// <param name="bCheckWafer">是否檢查wafer</param>
        public virtual void Absc()
        {
            _signalAck[enumRobotCommand.ABSC].Reset();
            m_Socket.SendCommand("ABSC");
            //  _nCurrStage = 0;
        }
        public virtual void AbscW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Absc();
                enumRobotCommand e = enumRobotCommand.ABSC;
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== EXCH ===============================================
        /// <summary>
        /// Aligner [EXCH]
        /// </summary>
        /// <param name="armSelect"></param>
        /// <param name="nStg0to399"></param>
        /// <param name="nSlot"></param>
        /// <param name="nflg"></param>
        private void Exch(enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg = 0)
        {
            _signalAck[enumRobotCommand.ExchangeArm].Reset();
            string str = string.Format("EXCH({0},{1},{2},{3})",
                 TransferSelectArmString(armSelect),
                 nStg0to399 + 1,
                 nSlot,
                 nflg);
            m_Socket.SendCommand(str);
        }
        private void ExchW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg = 0)
        {
            enumRobotCommand e = enumRobotCommand.ExchangeArm;
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Exch(armSelect, nStg0to399, nSlot, nflg);
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== MTPN ===============================================    
        //2024.4.10針對TRB2新增控制PIN伸縮,0是縮
        private void Mtpn(int n)
        {
            _signalAck[enumRobotCommand.MTPN].Reset();
            string str = string.Format("MTPN({0})", n);
            m_Socket.SendCommand(str);
        }
        public void MtpnW(int nTimeout, int n)
        {
            enumRobotCommand e = enumRobotCommand.MTPN;
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Mtpn(n);
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== GTPN ===============================================    
        //2024.4.10針對TRB2新增詢問PIN狀態
        private void Gtpn()
        {
            _signalAck[enumRobotCommand.GTPN].Reset();
            string str = string.Format("GTPN");
            m_Socket.SendCommand(str);
        }
        public void GtpnW(int nTimeout)
        {
            enumRobotCommand e = enumRobotCommand.GTPN;
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gtpn();
                if (!_signalAck[e].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.AckTimeout);
                    throw new SException((int)enumRobotError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[e]));
                }
                if (_signalAck[e].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.SendCommandFailure);
                    throw new SException((int)enumRobotError.SendCommandFailure, string.Format("Send command and wait Ack was Failure. [{0}]", _dicCmdsTable[e]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 


        //==============================================================================
        public void ResetChangeModeCompleted()
        {
            _signals[enumRobotSignalTable.Remote].Reset();
        }
        public void WaitChangeModeCompleted(int nTimeout)
        {
            if (!m_bSimulate)
            {
                if (!_signals[enumRobotSignalTable.Remote].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.InitialFailure);
                    throw new SException((int)enumRobotError.InitialFailure, string.Format("Wait Mode was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumRobotSignalTable.Remote].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.InitialFailure);
                    throw new SException((int)enumRobotError.InitialFailure, string.Format("Motion is Mode end."));
                }
            }
        }

        public void ResetOrgnSinal()
        {
            _signals[enumRobotSignalTable.OPRCompleted].Reset();
            m_bStatOrgnComplete = false;
        }
        public void WaitOrgnCompleted(int TimeOut)
        {
            if (!m_bSimulate)
            {
                if (!_signals[enumRobotSignalTable.OPRCompleted].WaitOne(TimeOut))
                {
                    SendAlmMsg(enumRobotError.OriginPosReturnTimeout);
                    throw new SException((int)(enumRobotError.OriginPosReturnTimeout), "Robot Orgn Fail");
                }
                if (_signals[enumRobotSignalTable.OPRCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.OriginPosReturnFailure);
                    throw new SException((int)(enumRobotError.OriginPosReturnFailure), "Robot Orgn Fail");
                }
            }
            else
            {
                m_bStatOrgnComplete = true;
                SpinWait.SpinUntil(() => false, 100);
                //Thread.Sleep(500);
            }
        }

        public void ResetProcessCompleted()
        {
            _signals[enumRobotSignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = true;
        }
        public void WaitProcessCompleted(int nTimeout)
        {
            if (!m_bSimulate)
            {
                if (!_signals[enumRobotSignalTable.ProcessCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.ProcessFlagTimeout);
                    throw new SException((int)enumRobotError.ProcessFlagTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumRobotSignalTable.ProcessCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.ProcessFlagAbnormal);
                    throw new SException((int)enumRobotError.ProcessFlagAbnormal, string.Format("Motion is abnormal end."));
                }
            }
            else
                SpinWait.SpinUntil(() => false, 100);
            m_bStatProcessed = false;
        }

        public void ResetInPos()
        {
            _signals[enumRobotSignalTable.MotionCompleted].Reset();
            m_eStatInPos = enumRobotStatus.Moving;
        }
        public void WaitInPos(int nTimeout)
        {
            //Thread.Sleep(200);//增加等待回傳的時間
            SpinWait.SpinUntil(() => false, 200);
            if (!m_bSimulate)
            {
                if (!_signals[enumRobotSignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRobotError.MotionTimeout);
                    throw new SException((int)enumRobotError.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumRobotSignalTable.MotionCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRobotError.MotionTimeout);
                    throw new SException((int)enumRobotError.MotionAbnormal, string.Format("Motion is abnormal end."));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
                m_eStatInPos = enumRobotStatus.InPos;
            }

            if (GPIO.DO_LowerArmOrigin && GPIO.DO_UpperArmOrigin)
            {

            }


        }

        //==============================================================================
        #region =========================== CommandTable =======================================
        protected Dictionary<enumRobotCommand, string> _dicCmdsTable;
        #endregion
        #region =========================== Signals ============================================
        protected Dictionary<enumRobotCommand, SSignal> _signalAck = new Dictionary<enumRobotCommand, SSignal>();
        protected Dictionary<enumRobotSignalTable, SSignal> _signals = new Dictionary<enumRobotSignalTable, SSignal>();
        private SSignal _signalSubSequence = new SSignal(false, EventResetMode.ManualReset);
        #endregion
        #region =========================== OnOccurError =======================================
        //  發生STAT異常
        private void SendAlmMsg(string strCode)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  TRB1 11 11 00000
            //  TRB2 12 11 00000     
            int nCode = Convert.ToInt32(strCode, 16) /*+ (11 + BodyNo - 1) * 10000000 + 11 * 100000*/;
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));
            SendAlmMsg(enumRobotError.Status_Error);
        }
        //  解除STAT異常
        private void RestAlmMsg(string strCode)
        {
            WriteLog(string.Format("Rest stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  TRB1 11 11 00000
            //  TRB2 12 11 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (11 + BodyNo - 1) * 10000000 + 11 * 100000*/;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  Cancel Code
        private void SendCancelMsg(string strCode)
        {
            WriteLog(string.Format("Occur cancel Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  TRB1 11 12 00000
            //  TRB2 12 12 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (11 + BodyNo - 1) * 10000000 + 12 * 100000*/;
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }

        private void SendAlmMsg(enumRobotError eAlarm)
        {
            WriteLog(string.Format("Occur eAlarm Error : {0}", eAlarm));
            //  TRB1 11 10 00000
            //  TRB2 12 10 00000   
            int nCode = (int)eAlarm /*+ (11 + BodyNo - 1) * 10000000 + 10 * 100000*/;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        #endregion
        #region =========================== CreateMessage ======================================
        public Dictionary<int, string> m_dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();
        void CreateMessage()
        {
            m_dicCancel[0x0200] = "0200:Motion target is not supported";
            m_dicCancel[0x0300] = "0300:Too few command elements";
            m_dicCancel[0x0310] = "0310:Too many command elements";
            m_dicCancel[0x0400] = "0400:Command is not supported";
            m_dicCancel[0x0500] = "0500:Too few parameters";
            m_dicCancel[0x0510] = "0510:Too many parameters";
            m_dicCancel[0x0520] = "0520:Improper parameter number";

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

            m_dicCancel[0x0700] = "0700:Abnormal mode: Preparation not completed";
            m_dicCancel[0x0702] = "0702:Abnormal mode: Not in maintenance mode";
            m_dicCancel[0x0704] = "0704:Command, which cannot be executed by the teaching pendant, is received";
            m_dicCancel[0x090F] = "090F:The setting for two-step lifting/lowering is less than 10";
            m_dicCancel[0x091F] = "091F:The setting for two-step lifting/lowering is exceeds 90";
            m_dicCancel[0x0920] = "0920:Improper slot designation";
            m_dicCancel[0x0921] = "0921:The number of slots not set";
            m_dicCancel[0x0A00] = "0A00:Origin search not completed";
            m_dicCancel[0x0A01] = "0A01:Origin reset not completed";
            m_dicCancel[0x0B00] = "0B00:Processing";
            m_dicCancel[0x0B01] = "0B01:Moving";
            m_dicCancel[0x0D00] = "0D00:Abnormal flash memory";
            m_dicCancel[0x0F00] = "0F00:Error-occurred state";
            m_dicCancel[0x1002] = "1002:Improper setting";
            m_dicCancel[0x1003] = "1003:Improper current position";
            m_dicCancel[0x1004] = "1004:Motion cannot be performed due to small designated position";
            m_dicCancel[0x1005] = "1005:Motion cannot be performed due to large designated position";
            m_dicCancel[0x1010] = "1010:No Wafer on upper finger";
            m_dicCancel[0x1011] = "1011:No Wafer on lower finger";
            m_dicCancel[0x1020] = "1020:Wafer exist on upper finger";
            m_dicCancel[0x1021] = "1021:Wafer exist on lower finger";
            m_dicCancel[0x1100] = "1100:Emergency stop signal is turned on";
            m_dicCancel[0x1200] = "1200:Temporary stop signal is turned on";
            m_dicCancel[0x1300] = "1300:Interlock signal is turned on";
            m_dicCancel[0x1400] = "1400:Drive power is turned off";

            m_dicController[0x00] = "[00:Robot Others] ";
            m_dicController[0x01] = "[01:Robot X-axis] ";
            m_dicController[0x02] = "[02:Robot Z-axis] ";
            m_dicController[0x03] = "[03:Robot Rotation] ";
            m_dicController[0x04] = "[04:Robot Upper arm] ";
            m_dicController[0x05] = "[05:Robot Lower arm] ";
            m_dicController[0x06] = "[06:Robot Both arms] ";
            m_dicController[0x07] = "[07:Robot Upper finger] ";
            m_dicController[0x08] = "[08:Robot Lower finger] ";
            m_dicController[0x0F] = "[0F:Robot Driver] ";
            for (int i = 10; i < 0x1F; i++)
            {
                m_dicController[i] = string.Format("[{0:X2}:Robot I/O] ", i);
            }
            m_dicController[0xFF] = "[FF:System] ";

            m_dicError[0x01] = "01:Motor stall.";
            m_dicError[0x02] = "02:Sensor abnormal.";
            m_dicError[0x03] = "03:Emergency stop.";
            m_dicError[0x04] = "04:Command error.";
            m_dicError[0x05] = "05:Controller communication abnormal.";
            m_dicError[0x06] = "06:Wafer retaining check sensor abnormal.";
            m_dicError[0x08] = "08:Wafer fall.";
            m_dicError[0x0C] = "0C:Origin reset not completed.";
            m_dicError[0x0E] = "0E:Driver abnormal.";
            m_dicError[0x11] = "11:Driver voltage too low.";
            m_dicError[0x12] = "12:Regeneration unit overheat.";
            m_dicError[0x14] = "14:Exhaust fan abnormal.";
            m_dicError[0x15] = "15:Operating position abnormal.";
            m_dicError[0x16] = "16:Encoder data lost.";
            m_dicError[0x17] = "17:Encoder communication error.";
            m_dicError[0x18] = "18:Encoder over speed.";
            m_dicError[0x19] = "19:Encoder EEPROM error.";
            m_dicError[0x1A] = "1A:Encoder ABS-INC error.";
            m_dicError[0x1B] = "1B:Encoder coefficient block error.";
            m_dicError[0x1C] = "1C:Encoder temperature abnormal.";
            m_dicError[0x1D] = "1D:Encoder reset error.";
            m_dicError[0x20] = "20:Control power abnormal.";
            m_dicError[0x21] = "21:Drive power abnormal.";
            m_dicError[0x22] = "22:EEPROM abnormal.";
            m_dicError[0x24] = "24:Overheat.";
            m_dicError[0x25] = "25:Over current.";
            m_dicError[0x26] = "26:Motor cable abnormal.";

            m_dicError[0x7F] = "7F:Memory abnormal.";

            m_dicError[0x83] = "83:Origin search disabled.";
            m_dicError[0x84] = "84:Wafer retaining error.";
            m_dicError[0x85] = "85:Interlock signal abnormal.";
            m_dicError[0x86] = "86:Alignment error.";
            m_dicError[0x89] = "89:Exhaust fan abnormal.";
            m_dicError[0x8A] = "8A:Battery voltage too low.";
            m_dicError[0x92] = "92:Clamp error.";
            m_dicError[0x93] = "93:Wafer presence abnormal.";

            m_dicError[0xC0] = "C0:N2 flow rate abnormal.";
            m_dicError[0xC1] = "C1:N2 source pressure abnormal.";

            m_dicError[0x30] = "30: DRIVER_01: Communication abnormal 1";
            m_dicError[0x31] = "31: DRIVER_02: Communication abnormal 2";
            m_dicError[0x32] = "32: DRIVER_03: Communication abnormal 3";
            m_dicError[0x33] = "33: DRIVER_04:";
            m_dicError[0x34] = "34: DRIVER_21: Overcurrent";
            m_dicError[0x35] = "35: DRIVER_22: Current detection abnormal 0";
            m_dicError[0x36] = "36: DRIVER_23: Current detection abnormal 1";
            m_dicError[0x37] = "37: DRIVER_24: Current detection abnormal 2";
            m_dicError[0x38] = "38: DRIVER_41: Overload 1";
            m_dicError[0x39] = "39: DRIVER_42: Overload 2";
            m_dicError[0x3A] = "3A: DRIVER_43: Regeneration abnormal";
            m_dicError[0x3B] = "3B: DRIVER_44: Fixed excitation abnormal";
            m_dicError[0x3C] = "3C: DRIVER_51: Amplifier overheat";
            m_dicError[0x3D] = "3D: DRIVER_52: Inrush prevention resistor overheat";
            m_dicError[0x3E] = "3E: DRIVER_53: DB resistor overheat";
            m_dicError[0x3F] = "3F: DRIVER_54: Internal overheat";
            m_dicError[0x40] = "40: DRIVER_61: Overvoltage";
            m_dicError[0x41] = "41: DRIVER_62: Too low voltage of main circuit";
            m_dicError[0x42] = "42: DRIVER_63: Open - phase of main power";
            m_dicError[0x43] = "43: DRIVER_64:";
            m_dicError[0x44] = "44: DRIVER_71: Too low voltage of control power";
            m_dicError[0x45] = "45: DRIVER_72: Insufficient power of + 12V power";
            m_dicError[0x46] = "46: DRIVER_73:";
            m_dicError[0x47] = "47: DRIVER_74:";
            m_dicError[0x48] = "48: DRIVER_81: Encoder pulse signal abnormal";
            m_dicError[0x49] = "49: DRIVER_82:";
            m_dicError[0x4A] = "4A: DRIVER_83: External encoder signal abnormal.";
            m_dicError[0x4B] = "4B: DRIVER_84:Communication between encoder and amplifier abnormal.";
            m_dicError[0x4C] = "4C: DRIVER_85: Encoder initialization failure.";
            m_dicError[0x4D] = "4D: DRIVER_86: CS abnormal.";
            m_dicError[0x4E] = "4E: DRIVER_87: CS wire broken.";
            m_dicError[0x4F] = "4F: DRIVER_88:.";
            m_dicError[0x50] = "50: DRIVER_C1: Over speed.";
            m_dicError[0x51] = "51: DRIVER_C2: Speed control abnormal.";
            m_dicError[0x52] = "52: DRIVER_C3: Speed feedback abnormal.";
            m_dicError[0x53] = "53: DRIVER_C4: Reached speed abnormal.";
            m_dicError[0x54] = "54: DRIVER_D1: Too large position deviation.";
            m_dicError[0x55] = "55: DRIVER_D2: Position instruction pulse frequency abnormal 1.";
            m_dicError[0x56] = "56: DRIVER_D3: Position instruction pulse frequency abnormal 2.";
            m_dicError[0x57] = "57: DRIVER_D4: Synchronous deviation abnormal.";
            m_dicError[0x58] = "58: DRIVER_E1: EEPROM abnormal.";
            m_dicError[0x59] = "59: DRIVER_E2: EEPROM checksum abnormal.";
            m_dicError[0x5A] = "5A: DRIVER_E3: Internal RAM abnormal.";
            m_dicError[0x5B] = "5B: DRIVER_E4: Process between CPU and ASIC abnormal.";
            m_dicError[0x5C] = "5C: DRIVER_E5: Parameter abnormal 1.";
            m_dicError[0x5D] = "5D: DRIVER_E6: Parameter abnormal 2.";
            m_dicError[0x5E] = "5E: DRIVER_E7:.";
            m_dicError[0x5F] = "5F: DRIVER_E8: Parameter abnormal 3.";
            m_dicError[0x60] = "60: DRIVER_F1: Task process abnormal.";
            m_dicError[0x61] = "61: DRIVER_F2: Initialization timeout.";
            m_dicError[0x62] = "62: DRIVER_F3:.";
            m_dicError[0x63] = "63: DRIVER_F4:.";


            //==============================================================================

            _dicCmdsTable = new Dictionary<enumRobotCommand, string>()
            {
                {enumRobotCommand.Orgn,"ORGN"},
                {enumRobotCommand.Home,"HOME"},
                {enumRobotCommand.ExtendingArm,"EXTD"},
                {enumRobotCommand.TakeWafer,"LOAD"},
                {enumRobotCommand.PutWafer,"UNLD"},
                {enumRobotCommand.TransferWafer,"TRNS"},
                {enumRobotCommand.TransferWafer2,"UTRN"},
                {enumRobotCommand.ExchangeArm,"EXCH"},
                {enumRobotCommand.VacuumOn,"CLMP"},
                {enumRobotCommand.VacuumOff,"UCLM"},
                {enumRobotCommand.Mapping,"WMAP"},
                {enumRobotCommand.CarryOutWafer,"MGET"},
                {enumRobotCommand.CarryOutWafer1,"MGT1"},
                {enumRobotCommand.CarryOutWafer2,"MGT2"},
                {enumRobotCommand.CarryInWafer,"MPUT"},
                {enumRobotCommand.CarryInWafer1,"MPT1"},
                {enumRobotCommand.CarryInWafer2,"MPT2"},
                {enumRobotCommand.CheckWaferInSlot,"WCHK"},
                {enumRobotCommand.ALEX,"ALEX"},
                {enumRobotCommand.ALLD,"ALLD"},
                {enumRobotCommand.ALUL,"ALUL"},
                {enumRobotCommand.ALGT,"ALGT"},
                {enumRobotCommand.ALEA,"ALEA"},
                {enumRobotCommand.ALMV,"ALMV"},
                {enumRobotCommand.ZMOV,"ZMOV"},
                {enumRobotCommand.MSSC,"MSSC"},
                {enumRobotCommand.EXCC,"EXCC"},
                {enumRobotCommand.Flip,"FLIP"},
                {enumRobotCommand.SetEvent,"EVNT"},
                {enumRobotCommand.Reset,"RSTA"},
                {enumRobotCommand.Initialize,"INIT"},
                {enumRobotCommand.Stop,"STOP"},
                {enumRobotCommand.Pause,"PAUS"},
                {enumRobotCommand.SetMode,"MODE"},
                {enumRobotCommand.StoreDate,"WTDT"},
                {enumRobotCommand.ReadData,"RTDT"},
                {enumRobotCommand.TransferDate,"TRDT"},
                {enumRobotCommand.SetSpeed,"SSPD"},
                {enumRobotCommand.SetPos,"SPOS"},
                {enumRobotCommand.SetTorque,"TORQ"},
                {enumRobotCommand.ExcitationControl,"EXCT"},
                {enumRobotCommand.BRAK,"BRAK"},
                {enumRobotCommand.SVAC,"SVAC"},
                {enumRobotCommand.SPOT,"SPOT"},
                {enumRobotCommand.GetStatus,"STAT"},
                {enumRobotCommand.GetIO,"GPIO"},
                {enumRobotCommand.GetMappingData,"GMAP"},
                {enumRobotCommand.GetRAC2,"RCA2.GPOS"},
                {enumRobotCommand.GetVersion,"GVER"},
                {enumRobotCommand.GetLog,"GLOG"},
                {enumRobotCommand.SetDateTime,"STIM"},
                {enumRobotCommand.GetDateTime,"GTIM"},
                {enumRobotCommand.GetPos,"GPOS"},
                {enumRobotCommand.GetWaferID,"GWID"},
                {enumRobotCommand.TDST,"TDST"},
                {enumRobotCommand.MOVT,"MOVT"},
                {enumRobotCommand.GVAC,"GVAC"},
                {enumRobotCommand.EXST,"EXST"},
                {enumRobotCommand.GCLM,"GCLM"},
                {enumRobotCommand.GCHK,"GCHK"},
                {enumRobotCommand.WAIT,"WAIT"},
                {enumRobotCommand.TDSA,"TDSA"},
                {enumRobotCommand.GAEX,"GAEX"},
                {enumRobotCommand.GALD,"GALD"},
                {enumRobotCommand.SDRV,"SDRV"},
                {enumRobotCommand.DequSTDT,"DEQU.STDT"},
                {enumRobotCommand.DtrbSTDT,"DTRB.STDT"},
                {enumRobotCommand.DtulSTDT,"DTUL.STDT"},
                {enumRobotCommand.DrciSTDT,"DRCI.STDT"},
                {enumRobotCommand.DrcsSTDT,"DRCS.STDT"},
                {enumRobotCommand.DmntSTDT,"DMNT.STDT"},
                {enumRobotCommand.DmprSTDT,"DMPR.STDT"},
                {enumRobotCommand.DcfgSTDT,"DCFG.STDT"},
                {enumRobotCommand.DequGTDT,"DEQU.GTDT"},
                {enumRobotCommand.DtrbGTDT,"DTRB.GTDT"},
                {enumRobotCommand.DtulGTDT,"DTUL.GTDT"},
                {enumRobotCommand.DrciGTDT,"DRCI.GTDT"},
                {enumRobotCommand.DrcsGTDT,"DRCS.GTDT"},
                {enumRobotCommand.DmntGTDT,"DMNT.GTDT"},
                {enumRobotCommand.DmprGTDT,"DMPR.GTDT"},
                {enumRobotCommand.DcfgGTDT,"DCFG.GTDT"},

                {enumRobotCommand.DapmGTDT,"DAPM.GTDT"},
                {enumRobotCommand.DapmSTDT,"DAPM.STDT"},

                {enumRobotCommand.ABSC,"ABSC"},
                {enumRobotCommand.Xax1Step,"XAX1.STEP"},
                {enumRobotCommand.Zax1Step,"ZAX1.STEP"},
                {enumRobotCommand.Rot1Step,"ROT1.STEP"},
                {enumRobotCommand.Arm1Step,"ARM1.STEP"},
                {enumRobotCommand.Arm2Step,"ARM2.STEP"},
                {enumRobotCommand.Xax1Gpos,"XAX1.GPOS"},
                {enumRobotCommand.Zax1Gpos,"ZAX1.GPOS"},
                {enumRobotCommand.Rot1Gpos,"ROT1.GPOS"},
                {enumRobotCommand.Arm1Gpos,"ARM1.GPOS"},
                {enumRobotCommand.Arm2Gpos,"ARM2.GPOS"},
                {enumRobotCommand.Xax1Extd,"XAX1.EXTD"},
                {enumRobotCommand.Zax1Extd,"ZAX1.EXTD"},
                {enumRobotCommand.Rot1Extd,"ROT1.EXTD"},
                {enumRobotCommand.Arm1Extd,"ARM1.EXTD"},
                {enumRobotCommand.Arm2Extd,"ARM2.EXTD"},
                {enumRobotCommand.Arm1Clmp,"ARM1.CLMP"},
                {enumRobotCommand.Arm2Clmp,"ARM2.CLMP"},
                {enumRobotCommand.Arm1Uclm,"ARM1.UCLM"},
                {enumRobotCommand.Arm2Uclm,"ARM2.UCLM"},
                {enumRobotCommand.MTPN,"MTPN"},//2024.4.10針對TRB2新增
                {enumRobotCommand.GTPN,"GTPN"},//2024.4.10針對TRB2新增
                {enumRobotCommand.ClientConnected,"CNCT"},
             };
        }
        #endregion


        public bool RobotHardwareAllow(SWafer.enumFromLoader ePos)
        {
            switch (ePos)
            {
                case SWafer.enumFromLoader.LoadportA: return m_AllowPort[0];
                case SWafer.enumFromLoader.LoadportB: return m_AllowPort[1];
                case SWafer.enumFromLoader.LoadportC: return m_AllowPort[2];
                case SWafer.enumFromLoader.LoadportD: return m_AllowPort[3];
                case SWafer.enumFromLoader.LoadportE: return m_AllowPort[4];
                case SWafer.enumFromLoader.LoadportF: return m_AllowPort[5];
                case SWafer.enumFromLoader.LoadportG: return m_AllowPort[6];
                case SWafer.enumFromLoader.LoadportH: return m_AllowPort[7];
                default: return false;
            }
        }

        public bool RobotHardwareAllow(SWafer.enumPosition ePos)
        {
            switch (ePos)
            {
                case SWafer.enumPosition.Loader1: return m_AllowPort[0];
                case SWafer.enumPosition.Loader2: return m_AllowPort[1];
                case SWafer.enumPosition.Loader3: return m_AllowPort[2];
                case SWafer.enumPosition.Loader4: return m_AllowPort[3];
                case SWafer.enumPosition.Loader5: return m_AllowPort[4];
                case SWafer.enumPosition.Loader6: return m_AllowPort[5];
                case SWafer.enumPosition.Loader7: return m_AllowPort[6];
                case SWafer.enumPosition.Loader8: return m_AllowPort[7];
                case SWafer.enumPosition.AlignerA: return m_AllowAligner[0];
                case SWafer.enumPosition.AlignerB: return m_AllowAligner[1];
                case SWafer.enumPosition.BufferA:
                case SWafer.enumPosition.BufferB: return true;
                case SWafer.enumPosition.EQM1: return m_AllowEquipment[0];
                case SWafer.enumPosition.EQM2: return m_AllowEquipment[1];
                case SWafer.enumPosition.EQM3: return m_AllowEquipment[2];
                case SWafer.enumPosition.EQM4: return m_AllowEquipment[3];
                default: return false;
            }
        }

        public bool RobotHardwareAllowBarcode() { return m_Barcode != null; }

        private int TransferSelectArmString(enumRobotArms arm)
        {
            int temp = 0;
            switch (arm)
            {
                case enumRobotArms.UpperArm:
                    temp = 1;
                    break;
                case enumRobotArms.LowerArm:
                    temp = 2;
                    break;
                case enumRobotArms.BothArms:
                    temp = 3;
                    break;
                default:
                    temp = 0;
                    break;
            }
            return temp;
        }

        #region 鎖臨界區間
        static private object m_objHoldingALN1 = new object(); //鎖臨界區間
        static private object m_objHoldingALN2 = new object(); //鎖臨界區間
        static private bool m_bHoldALN1 = false; //run貨權
        static private bool m_bHoldALN2 = false; //run貨權
        private bool m_bRunningALN1 = false;
        private bool m_bRunningALN2 = false;
        static private object m_objHoldingEQ1 = new object(); //鎖臨界區間
        static private object m_objHoldingEQ2 = new object(); //鎖臨界區間
        static private object m_objHoldingEQ3 = new object(); //鎖臨界區間
        static private object m_objHoldingEQ4 = new object(); //鎖臨界區間
        private bool m_bRunningEQ1 = false;
        private bool m_bRunningEQ2 = false;
        private bool m_bRunningEQ3 = false;
        private bool m_bRunningEQ4 = false;
        static private bool m_bHoldEQ1 = false; //run貨權
        static private bool m_bHoldEQ2 = false; //run貨權
        static private bool m_bHoldEQ3 = false; //run貨權
        static private bool m_bHoldEQ4 = false; //run貨權
        private bool GetRunningPermissionForALN1()
        {
            lock (m_objHoldingALN1)
            {
                if (m_bRunningALN1) return true; //已經搶到run貨權, 直接return true.
                if (m_bHoldALN1) return false; //run貨權已經被搶走

                m_bRunningALN1 = true; //搶到了!
                m_bHoldALN1 = true;
                return m_bHoldALN1;
            }
        }
        private void ReleaseRunningPermissionForALN1()
        {
            lock (m_objHoldingALN1)
            {
                if (!m_bRunningALN1) return; //沒搶到run貨權就不能釋放                 
                m_bRunningALN1 = false;
                m_bHoldALN1 = false;
            }
        }
        private bool GetRunningPermissionForALN2()
        {
            lock (m_objHoldingALN2)
            {
                if (m_bRunningALN2) return true; //已經搶到run貨權, 直接return true.
                if (m_bHoldALN2) return false; //run貨權已經被搶走

                m_bRunningALN2 = true; //搶到了!
                m_bHoldALN2 = true;
                return m_bHoldALN2;
            }
        }
        private void ReleaseRunningPermissionForALN2()
        {
            lock (m_objHoldingALN2)
            {
                if (!m_bRunningALN2) return; //沒搶到run貨權就不能釋放                 
                m_bRunningALN2 = false;
                m_bHoldALN2 = false;
            }
        }
        public bool GetRunningPermissionForALN(int nBody)
        {
            bool b = false;
            switch (nBody)
            {
                case 1: b = GetRunningPermissionForALN1(); break;
                case 2: b = GetRunningPermissionForALN2(); break;
            }
            return b;
        }
        public void ReleaseRunningPermissionForALN(int nBody)
        {
            switch (nBody)
            {
                case 1: ReleaseRunningPermissionForALN1(); break;
                case 2: ReleaseRunningPermissionForALN2(); break;
            }
        }
        private bool GetRunningPermissionForEQ1()
        {
            lock (m_objHoldingEQ1)
            {
                if (m_bRunningEQ1) return true; //已經搶到run貨權, 直接return true.
                if (m_bHoldEQ1) return false; //run貨權已經被搶走

                m_bRunningEQ1 = true; //搶到了!
                m_bHoldEQ1 = true;
                return m_bHoldEQ1;
            }
        }

        private bool GetRunningPermissionForEQ2()
        {
            lock (m_objHoldingEQ2)
            {
                if (m_bRunningEQ2) return true; //已經搶到run貨權, 直接return true.
                if (m_bHoldEQ2) return false; //run貨權已經被搶走

                m_bRunningEQ2 = true; //搶到了!
                m_bHoldEQ2 = true;
                return m_bHoldEQ2;
            }
        }

        private bool GetRunningPermissionForEQ3()
        {
            lock (m_objHoldingEQ3)
            {
                if (m_bRunningEQ3) return true; //已經搶到run貨權, 直接return true.
                if (m_bHoldEQ3) return false; //run貨權已經被搶走

                m_bRunningEQ3 = true; //搶到了!
                m_bHoldEQ3 = true;
                return m_bHoldEQ3;
            }
        }

        private bool GetRunningPermissionForEQ4()
        {
            lock (m_objHoldingEQ4)
            {
                if (m_bRunningEQ4) return true; //已經搶到run貨權, 直接return true.
                if (m_bHoldEQ4) return false; //run貨權已經被搶走

                m_bRunningEQ4 = true; //搶到了!
                m_bHoldEQ4 = true;
                return m_bHoldEQ4;
            }
        }

        private void ReleaseRunningPermissionForEQ1()
        {
            lock (m_objHoldingEQ1)
            {
                if (!m_bRunningEQ1) return; //沒搶到run貨權就不能釋放                 
                m_bRunningEQ1 = false;
                m_bHoldEQ1 = false;
            }
        }

        private void ReleaseRunningPermissionForEQ2()
        {
            lock (m_objHoldingEQ2)
            {
                if (!m_bRunningEQ2) return; //沒搶到run貨權就不能釋放                 
                m_bRunningEQ2 = false;
                m_bHoldEQ2 = false;
            }
        }

        private void ReleaseRunningPermissionForEQ3()
        {
            lock (m_objHoldingEQ3)
            {
                if (!m_bRunningEQ3) return; //沒搶到run貨權就不能釋放                 
                m_bRunningEQ3 = false;
                m_bHoldEQ3 = false;
            }
        }

        private void ReleaseRunningPermissionForEQ4()
        {
            lock (m_objHoldingEQ4)
            {
                if (!m_bRunningEQ4) return; //沒搶到run貨權就不能釋放                 
                m_bRunningEQ4 = false;
                m_bHoldEQ4 = false;
            }
        }

        public bool GetRunningPermissionForEQ(int nBody)
        {
            bool b = false;
            switch (nBody)
            {
                case 1: b = GetRunningPermissionForEQ1(); break;
                case 2: b = GetRunningPermissionForEQ2(); break;
                case 3: b = GetRunningPermissionForEQ3(); break;
                case 4: b = GetRunningPermissionForEQ4(); break;
            }
            return b;
        }

        public void ReleaseRunningPermissionForEQ(int nBody)
        {
            switch (nBody)
            {
                case 1: ReleaseRunningPermissionForEQ1(); break;
                case 2: ReleaseRunningPermissionForEQ2(); break;
                case 3: ReleaseRunningPermissionForEQ3(); break;
                case 4: ReleaseRunningPermissionForEQ4(); break;
            }
        }


        static private object m_objHoldingBUF1 = new object(); //鎖臨界區間
        static private object m_objHoldingBUF2 = new object(); //鎖臨界區間
        static private bool m_bHoldBUF1 = false; //run貨權
        static private bool m_bHoldBUF2 = false; //run貨權
        private bool m_bRunningBUF1 = false;
        private bool m_bRunningBUF2 = false;
        private bool GetRunningPermissionForBUF1()
        {
            lock (m_objHoldingBUF1)
            {
                if (m_bRunningBUF1) return true; //已經搶到run貨權, 直接return true.
                if (m_bHoldBUF1) return false; //run貨權已經被搶走

                m_bRunningBUF1 = true; //搶到了!
                m_bHoldBUF1 = true;
                return m_bHoldBUF1;
            }
        }
        private void ReleaseRunningPermissionForBUF1()
        {
            lock (m_objHoldingBUF1)
            {
                if (!m_bRunningBUF1) return; //沒搶到run貨權就不能釋放                 
                m_bRunningBUF1 = false;
                m_bHoldBUF1 = false;
            }
        }
        private bool GetRunningPermissionForBUF2()
        {
            lock (m_objHoldingBUF2)
            {
                if (m_bRunningBUF2) return true; //已經搶到run貨權, 直接return true.
                if (m_bHoldBUF2) return false; //run貨權已經被搶走

                m_bRunningBUF2 = true; //搶到了!
                m_bHoldBUF2 = true;
                return m_bHoldBUF2;
            }
        }
        private void ReleaseRunningPermissionForBUF2()
        {
            lock (m_objHoldingBUF2)
            {
                if (!m_bRunningBUF2) return; //沒搶到run貨權就不能釋放                 
                m_bRunningBUF2 = false;
                m_bHoldBUF2 = false;
            }
        }
        public bool GetRunningPermissionForBUF(int nBody)
        {
            bool b = false;
            switch (nBody)
            {
                case 1: b = GetRunningPermissionForBUF1(); break;
                case 2: b = GetRunningPermissionForBUF2(); break;
            }
            return b;
        }
        public void ReleaseRunningPermissionForBUF(int nBody)
        {
            switch (nBody)
            {
                case 1: ReleaseRunningPermissionForBUF1(); break;
                case 2: ReleaseRunningPermissionForBUF2(); break;
            }
        }


        private object m_objHoldingStgMap = new object(); //鎖臨界區間
        private bool[] m_bHoldStgMap = new bool[8]; //run貨權 8port
        private bool m_bHoldTrbMap; //run貨權
        public bool GetRunningPermissionForStgMap(int nStgBody)
        {
            int nIndx = nStgBody - 1;
            lock (m_objHoldingStgMap)
            {
                if (LowerArmWafer != null/* || UpperArmWafer != null*/ || m_bHoldTrbMap) return false;//Robot佔用了 //下Arm才有mapping bar
                if (m_bHoldStgMap[nIndx] == true) return true; //已經搶到run貨權, 直接return true.
                foreach (bool b in m_bHoldStgMap) { if (b) return false; } //run貨權已經被搶走
                m_bHoldStgMap[nIndx] = true; //搶到了!       
                return m_bHoldStgMap[nIndx];
            }
        }
        public void ReleaseRunningPermissionForStgMap(int nStgBody)
        {
            int nIndx = nStgBody - 1;
            lock (m_objHoldingStgMap)
            {
                if (m_bHoldStgMap[nIndx] == false) return; //沒搶到run貨權就不能釋放                 
                m_bHoldStgMap[nIndx] = false;
            }
        }
        public bool GetRunningPermissionForStgMap()
        {
            lock (m_objHoldingStgMap)
            {
                if (m_bHoldTrbMap == true) return true; //已經搶到run貨權, 直接return true.
                foreach (bool b in m_bHoldStgMap) { if (b) return false; } //run貨權已經被搶走
                m_bHoldTrbMap = true; //搶到了!       
                return m_bHoldTrbMap;
            }
        }
        public void ReleaseRunningPermissionForStgMap()
        {
            lock (m_objHoldingStgMap)
            {
                if (m_bHoldTrbMap == false) return; //沒搶到run貨權就不能釋放                 
                m_bHoldTrbMap = false;
            }
        }
        #endregion

        public void TriggerSException(enumRobotError eRobotError)
        {
            SendAlmMsg(eRobotError);
            throw new SException((int)(eRobotError), "SException:" + eRobotError);
        }

        public void Cleanjobschedule()//20240704
        {
            queCommand = new System.Collections.Concurrent.ConcurrentQueue<SWafer>();
            quePreCommand = new System.Collections.Concurrent.ConcurrentQueue<SWafer>();
        }


    }

    public class SRR757Axis
    {
        private SendCommandHandler _fpSendCommand;
        /// <summary>
        /// 單軸名稱
        /// XAX1: X-axis
        /// ZAX1: Z-axis
        /// ROT1: Rotation
        /// ARM1: Upper arm
        /// ARM2: Lower arm
        /// </summary>
        private string _strAxisName;            //單軸名稱
        private int _nPos;                      //目前encoder位置
        private bool _bJogMoving;               //單軸吋動動作中
        private int _nJogPulse = 100;
        public int JogPulse
        {
            get { return _nJogPulse; }
            set
            {
                //if (value < 1) return;
                _nJogPulse = value;
            }
        }

        private bool _bSimulate;
        /// <summary>
        /// 單軸吋動動作中
        /// </summary>
        public bool JogMoving { get { return _bJogMoving; } }
        private SSignal _signalInPos;
        public event ErrorEventHandler OnAlarm;
        private SInterruptOneThread _exeJoged;
        public enum AxisCommand
        {
            /// <summary>
            /// 原點搜尋 [ORGN]
            /// </summary>
            OPR,
            /// <summary>
            /// 吋動 [STEP]
            /// </summary>
            Jog,
            /// <summary>
            /// 絕對位置控制 [EXTD]
            /// </summary>
            AbsolutePos,
            /// <summary>
            /// 絕對位置控制 [CLMP]
            /// </summary>
            ArmClamp,
            /// <summary>
            /// 絕對位置控制 [CLMP]
            /// </summary>
            ArmUnClamp,
            /// <summary>
            /// 問encoder [GPOS]
            /// </summary>
            GetPos,
        }
        public event RobotPositionChangedHandler OnPositionChanged;
        public Dictionary<AxisCommand, SSignal> _signals = new Dictionary<AxisCommand, SSignal>() //Ack訊號管理
        {
            {AxisCommand.OPR, new SSignal(false, System.Threading.EventResetMode.ManualReset)},
            {AxisCommand.Jog, new SSignal(false, System.Threading.EventResetMode.ManualReset)},
            {AxisCommand.AbsolutePos, new SSignal(false, System.Threading.EventResetMode.ManualReset)},
            {AxisCommand.ArmClamp, new SSignal(false, System.Threading.EventResetMode.ManualReset)},
            {AxisCommand.ArmUnClamp, new SSignal(false, System.Threading.EventResetMode.ManualReset)},
            {AxisCommand.GetPos, new SSignal(false, System.Threading.EventResetMode.ManualReset)},
        };
        /// <summary>
        /// 建構單軸控制物件
        /// </summary>
        /// <param name="sendCallback">命令傳送委派</param>
        /// <param name="strAxisName">軸名稱
        /// XAX1: X-axis
        /// ZAX1: Z-axis
        /// ROT1: Rotation
        /// ARM1: Upper arm
        /// ARM2: Lower arm
        /// </param>
        public SRR757Axis(SendCommandHandler sendCallback, string strAxisName, SSignal InPos, bool bSimulate)
        {
            _fpSendCommand = sendCallback;
            _strAxisName = strAxisName;
            _signalInPos = InPos;
            _bSimulate = bSimulate;

            JogPulse = 100;
            _bJogMoving = false;
            _eventMgr = new SInterruptAnyThread(RunEventPassing, (int)EventTable.Max);
            _exeJog = new SPollingThread(100);
            _exeJog.DoPolling += new dlgv_v(_exeJog_DoPolling);
            _exeJoged = new SInterruptOneThread(_exeJog_DoPolling);
        }

        void _exeJog_DoPolling()
        {
            try
            {
                _fpSendCommand("STAT");
                _signalInPos.WaitOne(10000); //無法多軸同動, 等其他軸 in-position

                _signalInPos.Reset();
                JogW(10000, JogPulse);
                if (!_signalInPos.WaitOne(20000))
                {
                    throw new SException((int)enumRobotError.AckTimeout, "");
                }
                GetPosW(10000);
            }
            catch (SException alarm)
            {
                _exeJog.Reset();
                if (OnAlarm != null)
                    OnAlarm(this, new ErrorEventArgs(string.Format("Axis [{0}] has alarm", this._strAxisName), alarm.ErrorID));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        private SInterruptAnyThread _eventMgr;
        private SPollingThread _exeJog;
        public void JogStartd(int pulse)
        {
            JogPulse = pulse;
            _exeJoged.Set();
        }
        public void JogStart(int pulse)
        {
            JogPulse = pulse;
            _exeJog.Set();
        }
        public void JogStart()
        {
            _exeJog.Set();
        }
        public void JogStop()
        {
            _exeJog.Reset();
        }
        private enum EventTable : int
        {
            PositionBeChanged,
            Max,
        }

        public void Jog(int nDirection)
        {
            _signals[AxisCommand.Jog].Reset();
            SendCommand("STEP({0})", nDirection > 0 ? "+" + nDirection.ToString() : nDirection.ToString());
        }
        public void JogW(int nTimeout, int nDirection)
        {
            if (!_bSimulate)
            {
                Jog(nDirection);
                if (!_signals[AxisCommand.Jog].WaitOne(nTimeout))
                {
                    throw new SException((int)enumRobotError.SingleAxisAckTimeout, string.Format("Send single axis Jog command and wait ACK was timeout. [{0}.{1}].", _strAxisName, "STEP"));
                }
                if (_signals[AxisCommand.Jog].bAbnormalTerminal)
                {
                    throw new SException((int)enumRobotError.SingleAxisJogMovingFailure, string.Format("Single axis Jog command be rejected. [{0}.{1}]", _strAxisName, "STEP"));
                }
            }
            else
            {

            }
            //GetPosW(3000);
        }

        public void AbsolutePos(int nPosition)
        {
            _signals[AxisCommand.AbsolutePos].Reset();
            SendCommand("EXTD({0})", nPosition);
        }
        public void AbsolutePosW(int nTimeout, int nPosition)
        {
            if (!_bSimulate)
            {
                AbsolutePos(nPosition);
                _signalInPos.Reset();
                if (!_signals[AxisCommand.AbsolutePos].WaitOne(nTimeout))
                    throw new SException((int)enumRobotError.SingleAxisAckTimeout, string.Format("Do absolution position moving and wait ACK command was timeout. [{0}.{1}]", _strAxisName, "EXTD"));
                if (_signals[AxisCommand.AbsolutePos].bAbnormalTerminal)
                    throw new SException((int)enumRobotError.SingleAxisAbsoluteMovingFailure, string.Format("Absolution position moving be rejected. [{0}.{1}]", _strAxisName, "EXTD"));
            }
            else
            {

            }
            //GetPosW(3000);
        }

        public void Clamp()
        {
            _signals[AxisCommand.ArmClamp].Reset();
            SendCommand("CLMP");
        }
        public void ClampW(int nTimeout)
        {
            if (!_bSimulate)
            {
                Clamp();
                _signalInPos.Reset();
                if (!_signals[AxisCommand.ArmClamp].WaitOne(nTimeout))
                    throw new SException((int)enumRobotError.SingleAxisAckTimeout, string.Format("Do absolution position moving and wait ACK command was timeout. [{0}.{1}]", _strAxisName, "CLMP"));
                if (_signals[AxisCommand.ArmClamp].bAbnormalTerminal)
                    throw new SException((int)enumRobotError.SingleAxisAbsoluteMovingFailure, string.Format("Absolution position moving be rejected. [{0}.{1}]", _strAxisName, "CLMP"));
            }
            else
            {

            }
            //GetPosW(3000);
        }

        public void UnClamp()
        {
            _signals[AxisCommand.ArmUnClamp].Reset();
            SendCommand("UCLM");
        }
        public void UnClampW(int nTimeout)
        {
            if (!_bSimulate)
            {
                UnClamp();
                _signalInPos.Reset();
                if (!_signals[AxisCommand.ArmUnClamp].WaitOne(nTimeout))
                    throw new SException((int)enumRobotError.SingleAxisAckTimeout, string.Format("Do absolution position moving and wait ACK command was timeout. [{0}.{1}]", _strAxisName, "UCLM"));
                if (_signals[AxisCommand.ArmUnClamp].bAbnormalTerminal)
                    throw new SException((int)enumRobotError.SingleAxisAbsoluteMovingFailure, string.Format("Absolution position moving be rejected. [{0}.{1}]", _strAxisName, "UCLM"));
            }
            else
            {

            }
            //GetPosW(3000);
        }

        public void OPR()
        {
            _signals[AxisCommand.OPR].Reset();
            SendCommand("ORGN");
        }
        public void OPRW(int nTimeout)
        {
            if (!_bSimulate)
            {
                OPR();
                if (!_signals[AxisCommand.OPR].WaitOne(nTimeout))
                    throw new SException((int)enumRobotError.SingleAxisAckTimeout, string.Format("Send single axis OPR command and wait ACK was timeout. [{0}.{1}].", _strAxisName, "ORGN"));
                if (_signals[AxisCommand.OPR].bAbnormalTerminal)
                    throw new SException((int)enumRobotError.SingleAxisOPRFailure, string.Format("Single axis OPR command be rejected. [{0}.{1}]", _strAxisName, "ORGN"));
                GetPosW(3000);
            }
            else
            {

            }
        }

        public void GetPos()
        {
            _signals[AxisCommand.GetPos].Reset();
            SendCommand("GPOS");
        }
        public void GetPosW(int nTimeout)
        {
            if (!_bSimulate)
            {
                GetPos();
                if (!_signals[AxisCommand.GetPos].WaitOne(nTimeout))
                    throw new SException((int)enumRobotError.SingleAxisAckTimeout, string.Format("Get axis position and wait ACK was timeout. [{0}.{1}]", _strAxisName, "GPOS"));
                if (_signals[AxisCommand.GetPos].bAbnormalTerminal)
                    throw new SException((int)enumRobotError.SingleAxisGetPosFailure, string.Format("Get single axis position failure. [{0}.{1}]", _strAxisName, "GPOS"));
            }
            else
            {

            }
        }

        internal void PassingReceiveData(object sender, MessageEventArgs e)
        {
            try
            {
                string[] astrFrame = e.Message;
                foreach (string strFrame in astrFrame)
                {
                    if (!strFrame.Contains("TRB")) continue;
                    if (strFrame.Length <= 0) continue;
                    if (!strFrame.Contains(_strAxisName + ".")) continue; //只處理單軸訊號

                    bool bAbnormal = strFrame[0] == 'n' || strFrame[0] == 'c'; //是否異常結束

                    if (strFrame.Contains("ORGN"))
                    {
                        _signals[AxisCommand.OPR].bAbnormalTerminal = bAbnormal;
                        _signals[AxisCommand.OPR].Set();
                    }
                    else if (strFrame.Contains("STEP"))
                    {
                        _signals[AxisCommand.Jog].bAbnormalTerminal = bAbnormal;
                        _signals[AxisCommand.Jog].Set();
                    }
                    else if (strFrame.Contains("EXTD"))
                    {
                        _signals[AxisCommand.AbsolutePos].bAbnormalTerminal = bAbnormal;
                        _signals[AxisCommand.AbsolutePos].Set();
                    }
                    else if (strFrame.Contains("CLMP"))
                    {
                        _signals[AxisCommand.ArmClamp].bAbnormalTerminal = bAbnormal;
                        _signals[AxisCommand.ArmClamp].Set();
                    }
                    else if (strFrame.Contains("UCLM"))
                    {
                        _signals[AxisCommand.ArmUnClamp].bAbnormalTerminal = bAbnormal;
                        _signals[AxisCommand.ArmUnClamp].Set();
                    }
                    else if (strFrame.Contains("GPOS"))
                    {
                        _signals[AxisCommand.GetPos].bAbnormalTerminal = bAbnormal;

                        if (!bAbnormal) _nPos = Convert.ToInt32(strFrame.Split(':')[1]);
                        // _logger.WriteLog("set [{0}] axis position [{1}], field = [{2}], frame=[{3}]", _strAxisName, _nPos, strFrame, e.Message);
                        _eventMgr[(int)EventTable.PositionBeChanged].Set();
                        _signals[AxisCommand.GetPos].Set();
                        return; //just process first frame only.
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        /// <summary>
        /// 目前encoder
        /// </summary>
        public int Position { get { return _nPos; } }
        private void SendCommand(string strCommand)
        {
            _fpSendCommand(string.Format("{0}.{1}",
                _strAxisName,
                strCommand));
        }
        private void SendCommand(string format, params object[] args)
        {
            SendCommand(string.Format(format, args));
        }
        private void RunEventPassing(int EventID)
        {
            _eventMgr[EventID].Reset();
            EventTable ent = (EventTable)EventID;
            switch (ent)
            {
                case EventTable.PositionBeChanged:
                    if (OnPositionChanged != null) OnPositionChanged(this, new RobotPositionEventArgs(_nPos));
                    break;

            }
        }







    }
}
