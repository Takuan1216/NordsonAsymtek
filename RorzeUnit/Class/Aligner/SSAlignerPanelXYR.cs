
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.Agito;
using RorzeUnit.Class.Aligner.Enum;
using RorzeUnit.Class.Aligner.Event;
using RorzeUnit.Class.Camera;
using RorzeUnit.Class.RC500.RCEnum;
using RorzeUnit.Class.Agito.Enum;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;
using YamlDotNet.Core.Tokens;
using static RorzeUnit.Class.Agito.SSAGD301_Motion;
using static RorzeUnit.Class.Agito.SSPanelAlignerParm;
using static RorzeUnit.Class.Agito.SSPanelAlignerParm.SSAlignerParm;
using static RorzeUnit.Class.SWafer;
using System.Globalization;

namespace RorzeUnit.Class.Aligner
{
    public class SSAlignerPanelXYR : I_Aligner
    {
        private bool m_bSimulate;
        private enumAlignerMode m_eStatMode;        //記憶的STAT S1第1 bit  
        private bool m_bStatOrgnComplete;           //記憶的STAT S1第2 bit
        private bool m_bStatProcessed;              //記憶的STAT S1第3 bit
        private enumAlignerStatus m_eStatInPos;     //記憶的STAT S1第4 bit   
        private int m_nSpeed;                       //記憶的STAT S1第5 bit     
        private string m_strErrCode = "0000";       //記憶的STAT S2
        private bool m_bIsError = false;

        private SLogger m_Logger = SLogger.GetLogger("CommunicationLog");
        private SLogger m_ALNLogger;
        private SSCamera m_Camera;
        private SWafer m_Wafer;
        I_BarCode m_Barcode;

        private int m_nAckTimeout = 3000;
        private int m_nMotionTimeout = 60000;
        private bool m_bAlignmentStart = false;
        private bool m_bProcessStart = false;
        private bool m_bRobotExtend = false;
        private bool m_bPanelExistUseVac = false;

        private int m_XPos = 0;
        private int m_YPos = 0;
        private int m_ZPos = 0;
        private int m_RPos = 0;

        SSAGD301_Motion m_Motion;
        SSAlignerParm m_AlgnParam;
        public virtual bool Connected
        {
            get
            {
                if (m_bSimulate)
                    return true;
                return m_Motion.Connected && m_Camera._Connected;
            }
        }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }
        public bool AlignmentStart { get { return m_bAlignmentStart; } set { m_bAlignmentStart = value; } }
        public bool ProcessStart { get { return m_bProcessStart; } set { m_bProcessStart = value; } }
        public int Xaxispos { get { return m_bSimulate == true ? m_XPos : m_Motion.Axis3Pos; } protected set { } }
        public int Yaxispos { get { return m_bSimulate == true ? m_YPos : m_Motion.Axis2Pos; } protected set { } }
        public int Zaxispos { get { return m_ZPos; } protected set { } }
        public int Raxispos { get { return m_bSimulate == true ? m_RPos : m_Motion.Axis1Pos; } protected set { } }

        public int Raxispos_Approximation
        {
            get
            {
                float angle = Raxispos / 1000; // 輸入角度
                float baseAngle = 90f; // 基準角度間隔
                // 計算最近的基準角度
                float normalizedAngle = (angle + baseAngle / 2) % 360; // 避免負數，並偏移 45° 讓 22° 對齊到 0°
                int nearestBaseAngle = (int)(normalizedAngle / baseAngle) * (int)baseAngle;
                nearestBaseAngle = nearestBaseAngle >= 360 ? 0 : nearestBaseAngle; // 處理 360° 的情況
                return nearestBaseAngle * 1000;
            }
        }

        public int _AckTimeout { get { return m_nAckTimeout; } }
        public int _MotionTimeout { get { return m_nMotionTimeout; } }
        public ConcurrentQueue<SWafer> queCommand { get; set; }
        public ConcurrentQueue<SWafer> quePreCommand { get; set; }
        public SWafer Wafer
        {
            get { return m_Wafer; }
            set
            {
                m_Wafer = value;

                Aligner_WaferChange?.Invoke(this, new WaferDataEventArgs(m_Wafer));  //  更新UI  //  更新UI

                if (m_Wafer == null)
                {
                    //CheckWaferExisClampVacuum(true);//Panel拿走後更新Panel exist狀態
                    return;
                }

                OnAssignWaferData?.Invoke(this, new WaferDataEventArgs(m_Wafer));
            }
        }
        public SRA320GPIO GPIO { get; protected set; }
        public bool IsRobotExtend { get { return m_bRobotExtend; } }
        public bool SetRobotExtend { set { m_bRobotExtend = value; } }
        public enumWaferSize WaferType { get; } = enumWaferSize.Panel;
        //STAT S1第1 bit
        //public enumAlignerMode StatMode { get { return m_eStatMode; } }
        //public bool IsInitialized { get { return m_eStatMode == enumAlignerMode.Remote; } }
        //STAT S1第2 bit
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }
        //STAT S1第3 bit
        //public bool IsProcessing { get { return m_bStatProcessed; } }
        //STAT S1第4 bit
        //public enumAlignerStatus InPos { get { return m_eStatInPos; } }
        public bool IsMoving { get { return m_eStatInPos == enumAlignerStatus.Moving; } }
        //STAT S1第5 bit
        //public int GetSpeed { get { return m_nSpeed; } }
        //STAT S2
        public bool IsError
        {
            get
            {
                if (m_bSimulate)
                    return false;
                if (m_bIsError == false)
                    return m_Motion.IsError || m_Camera.IsError;
                else
                    return m_bIsError;
            }
        }



        public SSCamera Camera { get { return m_Camera; } }


        public dlgv_wafer AssignToRobotQueue { get; set; }//丟給robot作排程

        #region =========================== Event ==============================================
        public event EventHandler<WaferDataEventArgs> OnAssignWaferData;
        public event EventHandler<WaferDataEventArgs> Aligner_WaferChange;  //  更新UI
        public event EventHandler<WaferDataEventArgs> OnAligCompelet;  //  alig end 

        public event EventHandler<bool> OnManualCompleted;
        public event EventHandler<bool> OnORGNComplete;
        public event EventHandler<bool> OnRot1StepComplete;

        public event EventHandler OnProcessStart;
        public event EventHandler OnProcessEnd;
        public event EventHandler OnProcessAbort;

        public event MessageEventHandler OnReadData;

        public event AutoProcessingEventHandler DoManualProcessing;
        public event AutoProcessingEventHandler DoAutoProcessing;

        public event OccurErrorEventHandler OnOccurStatErr;
        public event OccurErrorEventHandler OnOccurCancel;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public event OccurErrorEventHandler OnOccurErrorRest;
        public event OccurErrorEventHandler OnOccurWarning;
        public event OccurErrorEventHandler OnOccurWarningRest;

        #endregion

        #region =========================== Thread =============================================
        private SInterruptOneThread _threadManualFunc;
        private SInterruptOneThread _threadOrgn;             //原點復歸控制
        private SInterruptOneThreadINT_INT _threadHome;      //
        private SInterruptOneThread _threadClmp;             //
        private SInterruptOneThread _threadUclm;             //
        private SInterruptOneThreadINT _threadAlgn;          //
        private SInterruptOneThread _threadAlgn1;            //
        private SInterruptOneThreadINT _threadRot1Extd;      //
        private SInterruptOneThreadINT _threadRot1Step;      //
        private SInterruptOneThread _threadStop;             //
        private SInterruptOneThread _threadPause;            //
        private SInterruptOneThreadINT _threadMode;          //
        private SInterruptOneThread _threadWtdt;             //
        private SInterruptOneThreadINT _threadSspd;          //
        private SInterruptOneThread _threadGver;             //
        private SInterruptOneThreadINT _threadSsiz;          //
        private SInterruptOneThread _threadGsiz;             //
        private SInterruptOneThreadINT _threadReset;         //異常復歸控制
        private SInterruptOneThread _threadInit;             //初始化控制(private 流程, 問Status/機況同步)
        private SPollingThread _pollingAuto;                 //自動流程控管   
        private SPollingThread _exePolling;
        #endregion

        #region =========================== Signals ============================================
        //protected Dictionary<enumAlignerCommand, SSignal> m_SignalAck = new Dictionary<enumAlignerCommand, SSignal>();
        protected Dictionary<enumAlignerSignalTable, SSignal> m_Signals = new Dictionary<enumAlignerSignalTable, SSignal>();
        protected SSignal m_SignalSubSequence;
        #endregion

        #region =========================== CreateMessage ======================================
        public Dictionary<int, string> m_dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();
        void CreateMessage()
        {

        }
        #endregion


        public SSAlignerPanelXYR(int nBodyNo, bool bDisable, bool bSimulate)
        {
            BodyNo = nBodyNo;
            Disable = bDisable;
            m_bSimulate = bSimulate;
            m_bIsError = false;
            m_ALNLogger = SLogger.GetLogger($"ALN{nBodyNo}");
            for (int i = 0; i < (int)enumAlignerSignalTable.Max; i++)
                m_Signals.Add((enumAlignerSignalTable)i, new SSignal(false, EventResetMode.ManualReset));
            m_Signals[enumAlignerSignalTable.ProcessCompleted].Set();
            m_SignalSubSequence = new SSignal(false, EventResetMode.ManualReset);
            m_AlgnParam = new SSAlignerParm(nBodyNo.ToString());
            if (bSimulate == false)
            {
                m_Motion = new SSAGD301_Motion(BodyNo, bSimulate);
            }
            m_Camera = new SSCamera("172.20.9.70", 12000, 1, false, false);
            m_Camera.OnOccurStatErr += Camera_OnOccurStatErr;
            m_Camera.OnOccurCancel += Camera_OnOccurCancel;
            m_Camera.OnOccurCustomErr += Camera_OnOccurCustomErr;

            _threadOrgn = new SInterruptOneThread(ExeORGN);             //原點復歸控制
            _threadHome = new SInterruptOneThreadINT_INT(ExeHome);
            _threadClmp = new SInterruptOneThread(ExeClmp);          //
            _threadUclm = new SInterruptOneThread(ExeUclm);          //
            _threadAlgn = new SInterruptOneThreadINT(ExeAlgn);          //
            _threadAlgn1 = new SInterruptOneThread(ExeAlgn1);            //
            _threadRot1Extd = new SInterruptOneThreadINT(ExeRot1Extd);      //
            _threadRot1Step = new SInterruptOneThreadINT(ExeRot1Step);      //
            _threadStop = new SInterruptOneThread(ExeStop);             //

            _threadReset = new SInterruptOneThreadINT(ExeRsta);         //異常復歸控制
            _threadInit = new SInterruptOneThread(ExeInit);             //初始化控制(private 流程, 問Status/機況同步)
            //_pollingAuto = new SInterruptOneThread();                 //自動流程控管   
            //_exePolling = new SInterruptOneThread();


            _threadManualFunc = new SInterruptOneThread(ExeManualFunction);

            _pollingAuto = new SPollingThread(1);
            _pollingAuto.DoPolling += _pollingAuto_DoPolling;

            CreateMessage();
        }

        ~SSAlignerPanelXYR()
        { 
        
        }

        private void Camera_OnOccurCustomErr(object sender, OccurErrorEventArgs e)
        {
            WriteLog(string.Format("Occur Camera custom error : {0}", e.ErrorCode));
            SendAlmMsg(enumAlignerError.CameraCustomError);
        }

        private void Camera_OnOccurCancel(object sender, OccurErrorEventArgs e)
        {
            WriteLog(string.Format("Occur Camera cancel error : {0:X}_{1}", e.ErrorCode, e.ErrorCode));
            SendAlmMsg(enumAlignerError.CameraCancelError);
        }

        private void Camera_OnOccurStatErr(object sender, OccurErrorEventArgs e)
        {
            WriteLog(string.Format("Occur Camera status error : {0:X}_{1}", e.ErrorCode, e.ErrorCode));
            SendAlmMsg(enumAlignerError.CameraStatusError);
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[ALN{0}] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            m_Logger.WriteLog(strMsg);
        }

        public void Open()
        {
            m_Camera.Open();
            m_Motion.Init();

        }

        //==============================================================================

        #region Auto Process
        private void _pollingAuto_DoPolling() //  自動流程
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


        public void AutoProcessStart()
        {
            m_bProcessStart = true;
            this._pollingAuto.Set();
            if (OnProcessStart != null) OnProcessStart(this, new EventArgs());
        }
        public void AutoProcessEnd()
        {
            _pollingAuto.Reset();
            m_bProcessStart = false;
            if (OnProcessEnd != null) OnProcessEnd(this, new EventArgs());
        }
        public void AligCompelet(SWafer wafer = null) // HSC GRPC
        {
            wafer.AlgnComplete = true;
            if (OnAligCompelet != null)
                OnAligCompelet(this, new WaferDataEventArgs(wafer));
        }
        public void AssignQueue(SWafer wafer)
        {
            quePreCommand.Enqueue(wafer);
        }

        public void Cleanjobschedule()//20240704
        {
            queCommand = new System.Collections.Concurrent.ConcurrentQueue<SWafer>();
            quePreCommand = new System.Collections.Concurrent.ConcurrentQueue<SWafer>();
        }
        #endregion

        #region OneThread
        public void StartManualFunction() { _threadManualFunc.Set(); }
        public void ORGN() { _threadOrgn.Set(); }
        public void HOME() { _threadHome.Set(); }
        public void CLMP() { _threadClmp.Set(); }
        public void UCLM() { _threadUclm.Set(); }
        public void ALGN(int nNum) { _threadAlgn.Set(nNum); }
        public void ALGN1() { _threadAlgn1.Set(); }
        public void Rot1EXTD(int nPos) { _threadRot1Extd.Set(nPos); }
        public void Rot1STEP(int nPos) { _threadRot1Step.Set(nPos); }
        public void RSTA(int nNum) { _threadReset.Set(nNum); }
        public void INIT() { _threadInit.Set(); }
        public void STOP() { _threadStop.Set(); }
        public void PAUS() { /*_threadPause.Set(); */}
        public void MODE(int nNum) {/* _threadMode.Set(nNum);*/ }
        public void WTDT() { /*_threadWtdt.Set(); */}
        public void SSPD(int nNum) {/* _threadSspd.Set(nNum);*/ }
        public void SSIZ(int nNum) {/* _threadSsiz.Set(nNum); */}
        public void GSIZ() {/* _threadGsiz.Set(); */}
        //==============================================================================
        void ExeManualFunction()
        {
            try
            {
                WriteLog("ExeManualFunction:Start");
                m_Signals[enumAlignerSignalTable.ProcessCompleted].Reset();
                if (DoManualProcessing != null) DoManualProcessing(this);
                OnManualCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
                OnManualCompleted?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
                OnManualCompleted?.Invoke(this, false);
            }
            DoManualProcessing = null;

        }
        protected virtual void ExeORGN()
        {
            try
            {
                WriteLog("ExeORGN:Start");
                ResetW(3000, 1);
                this.InitW(m_nMotionTimeout);

                this.OrgnW(m_nMotionTimeout, 2);

                SpinWait.SpinUntil(() => false, 500);

                Cleanjobschedule();


                if (WaferExists())
                {
                    WriteLog("ExeORGN:Detect Panel");

                    ResetInPos();
                    Algn1W(m_nAckTimeout);
                    WaitInPos(m_nMotionTimeout);

                    //ResetInPos();
                    //UclmW(m_nAckTimeout);
                    //WaitInPos(m_nMotionTimeout);
                }

                OnORGNComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
                UCLM();
                OnORGNComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
                OnORGNComplete?.Invoke(this, false);
            }
        }
        void ExeHome(int n1, int n2)
        {
            try
            {
                WriteLog("ExeHome:Start");
                this.HomeW(3000, 0, 0);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        void ExeClmp()
        {
            try
            {
                WriteLog("ExeClmp:Start");
                this.ClmpW(m_nMotionTimeout);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        void ExeUclm()
        {
            try
            {
                WriteLog("ExeUclm:Start");
                this.UclmW(m_nMotionTimeout);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        void ExeAlgn(int nMode)
        {
            try
            {
                WriteLog("ExeAlgn:Start");
                this.AlgnW(m_nMotionTimeout, nMode);

            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);

            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);

            }
        }
        void ExeAlgn(int nMode, int nPos)
        {
            try
            {
                WriteLog("ExeAlgn:Start");
                this.AlgnW(m_nMotionTimeout, nMode, nPos);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        void ExeAlgn1()
        {
            try
            {
                WriteLog("ExeAlgn1:Start");
                this.Algn1W(m_nMotionTimeout);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        void ExeRot1Extd(int nPos)
        {
            try
            {
                WriteLog("ExeRot1Extd:Start");
                this.RotationExtdW(m_nMotionTimeout, nPos);//1 nPos = 1 drgree 
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        void ExeRot1Step(int nPos)
        {
            try
            {
                WriteLog("ExeRot1Step:Start");
                this.RotStepW(m_nMotionTimeout, nPos);

                OnRot1StepComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
                OnRot1StepComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
                OnRot1StepComplete?.Invoke(this, false);
            }
        }
        void ExeRsta(int nMode)
        {
            try
            {
                WriteLog("ExeRsta:Start");
                this.ResetW(m_nMotionTimeout, nMode);
                //WriteLog(" ResetW MotionCompleted Set");

                m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        void ExeInit()
        {
            try
            {
                WriteLog("ExeInit:Start");
                this.InitW(m_nMotionTimeout);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        void ExeStop()
        {
            try
            {
                WriteLog("ExeStop:Start");
                this.StopW(m_nMotionTimeout);
            }
            catch (SException ex)
            {
                CatchErrorCode(ex.ErrorID);
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }
        //void ExePause()
        //{
        //    try
        //    {
        //        WriteLog("ExePause:Start");
        //        this.PausW(3000);
        //    }
        //    catch (SException ex)
        //    {
        //        WriteLog("<<SException>> :" + ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("<<Exception>> :" + ex);
        //    }
        //}
        //void ExeMode(int nMode)
        //{
        //    try
        //    {
        //        WriteLog("ExeMode:Start");
        //        this.ModeW(3000, nMode);
        //    }
        //    catch (SException ex)
        //    {
        //        WriteLog("<<SException>> :" + ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("<<Exception>> :" + ex);
        //    }
        //}
        //void ExeWtdt()
        //{
        //    try
        //    {
        //        WriteLog("ExeWtdt:Start");
        //        this.WtdtW(15000);
        //    }
        //    catch (SException ex)
        //    {
        //        WriteLog("<<SException>> :" + ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("<<Exception>> :" + ex);
        //    }
        //}
        //void ExeSspd(int nMode)
        //{
        //    try
        //    {
        //        WriteLog("ExeSspd:Start");
        //        this.SspdW(3000, nMode);
        //    }
        //    catch (SException ex)
        //    {
        //        WriteLog("<<SException>> :" + ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("<<Exception>> :" + ex);
        //    }
        //}
        //void ExeGver()
        //{
        //    try
        //    {
        //        WriteLog("ExeGver:Start");
        //        this.GverW(3000);
        //    }
        //    catch (SException ex)
        //    {
        //        WriteLog("<<SException>> :" + ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("<<Exception>> :" + ex);
        //    }
        //}
        //void ExeSsiz(int nMode)
        //{
        //    try
        //    {
        //        WriteLog("ExeSsiz:Start");
        //        this.SsizW(3000, nMode);
        //    }
        //    catch (SException ex)
        //    {
        //        WriteLog("<<SException>> ExeSsiz:" + ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("<<Exception>> ExeSsiz:" + ex);
        //    }
        //}
        //void ExeGsiz()
        //{
        //    try
        //    {
        //        WriteLog("ExeGsiz:Start");
        //        this.GsizW(3000);
        //    }
        //    catch (SException ex)
        //    {
        //        WriteLog("<<SException>> :" + ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("<<Exception>> :" + ex);
        //    }
        //}
        #endregion

        #region =========================== ORGN ===============================================

        public void OrgnW(int nTimeout, int nOrgn = 0)
        {
            m_SignalSubSequence.Reset();
            m_Signals[enumAlignerSignalTable.OPRCompleted].Reset();
            m_Signals[enumAlignerSignalTable.MotionCompleted].Reset();

            eClampWorkVacuumStatus PanelStatus = eClampWorkVacuumStatus.VacuumError;
            if (!m_bSimulate)
            {
                switch (nOrgn)
                {
                    case 2://ORGN預設
                        {
                            //檢查真空和是否有產品，如果有產品，就吸住產品回原點，沒產品的話關閉真空回原點

                            SpinWait.SpinUntil(() => false, 1000);//未查到相機存在指令有時候沒收到

                            m_Camera.ExisW(10000);

                            bool bExis = m_Camera.GetExisResult;

                            PanelStatus = CheckWaferExisClampVacuum(true);
                            if (PanelStatus == eClampWorkVacuumStatus.WorksExist || bExis)
                                ClampProduct();
                            else if (PanelStatus == eClampWorkVacuumStatus.WorksNoExist)
                                UnclampProduct();
                            else
                            {
                                SendAlmMsg(enumAlignerError.TurnOnVacuumFailure);
                                throw new SException((int)enumAlignerError.TurnOnVacuumFailure, string.Format("TurnOnVacuumFailure. [{0}]", "ORGN"));
                            }
                            try
                            {
                                m_Motion.ResetInPos();
                                m_Motion.Orgn(3000, enumAGD301Axis.ALL);
                                m_Motion.WaitInPos(60000);
                            }
                            catch (SException ex)
                            {
                                CatchErrorCode(ex.ErrorID);
                                throw ex;
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            break;
                        }
                    case 1:
                        {
                            //檢查真空和是否有產品，如果有產品，報發錯誤，沒有產品才執行原點程序

                            PanelStatus = CheckWaferExisClampVacuum(true);
                            if (PanelStatus == eClampWorkVacuumStatus.WorksExist)
                            {
                                SendAlmMsg(enumAlignerError.OriginPosReturnFailure);
                                throw new SException((int)enumAlignerError.OriginPosReturnFailure, string.Format("ModeError(PanelExist). [{0}]", "ORGN"));
                            }
                            else if (PanelStatus == 0)
                                UnclampProduct();
                            else
                            {
                                SendAlmMsg(enumAlignerError.TurnOnVacuumFailure);
                                throw new SException((int)enumAlignerError.TurnOnVacuumFailure, string.Format("TurnOnVacuumFailure. [{0}]", "ORGN"));
                            }
                            try
                            {

                                m_Motion.ResetInPos();
                                m_Motion.ORGN(0, (int)enumAGD301Axis.ALL);
                                m_Motion.WaitInPos(60000);

                            }
                            catch (SException ex)
                            {
                                CatchErrorCode(ex.ErrorID);
                                throw ex;
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            break;
                        }
                    case 3:
                        {

                            PanelStatus = CheckWaferExisClampVacuum(true);
                            if (PanelStatus == eClampWorkVacuumStatus.WorksExist)
                                ClampProduct();
                            else if (PanelStatus == 0)
                            {
                                SendAlmMsg(enumAlignerError.OriginPosReturnFailure);
                                throw new SException((int)enumAlignerError.OriginPosReturnFailure, string.Format("ModeError(PanelNotExist). [{0}]", "ORGN"));
                            }
                            else
                            {
                                SendAlmMsg(enumAlignerError.TurnOnVacuumFailure);
                                throw new SException((int)enumAlignerError.TurnOnVacuumFailure, string.Format("TurnOnVacuumFailure. [{0}]", "ORGN"));
                            }
                            try
                            {

                                m_Motion.ResetInPos();
                                m_Motion.ORGN(0, (int)enumAGD301Axis.ALL);
                                m_Motion.WaitInPos(60000);

                            }
                            catch (SException ex)
                            {
                                CatchErrorCode(ex.ErrorID);
                                throw ex;
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            break;
                        }
                    default:
                        {
                            //不檢查是否有產品，關閉真空，執行原點程序                   
                            try
                            {
                                UnclampProduct();
                                m_Motion.ResetInPos();
                                m_Motion.ORGN(0, (int)enumAGD301Axis.ALL);
                                m_Motion.WaitInPos(60000);
                            }
                            catch (SException ex)
                            {
                                CatchErrorCode(ex.ErrorID);
                                throw ex;
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            break;
                        }
                }
                SetSimulatPosition(0, 0, 0, 0);
                m_bStatOrgnComplete = true;
            }
            else
            {
                m_bStatOrgnComplete = true;
            }
            //WriteLog(" OrgnW MotionCompleted Set");
            m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== HOME ===============================================

        public void HomeW(int nTimeout, int nP1, int nP2)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                try
                {
                    HOMEPosition PosData = m_AlgnParam.GetHomePos(0, nP1);
                    m_Motion.ResetInPos();
                    m_Motion.Mabs(nTimeout, PosData.R, PosData.X, PosData.Y);
                    SetSimulatPosition(PosData.R, PosData.X, PosData.Y, 0);
                    m_Motion.WaitInPos(nTimeout);
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                }
                catch (SException ex)
                {
                    CatchErrorCode(ex.ErrorID);
                    throw ex;
                }
                catch (Exception ex)
                {
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                    m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                    SendAlmMsg(enumAlignerError.MotionAbnormal);
                    WriteLog("<<Exception>> :" + ex);
                    throw ex;
                }
                /*_signals[enumAlignerSignalTable.MotionCompleted].Reset();
                Home(nP1, nP2);
                if (!_signalAck[enumAlignerCommand.Home].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send HOME command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Home]));
                }
                if (_signalAck[enumAlignerCommand.Home].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send HOME command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Home]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== CLMP ===============================================

        //protected abstract void ClmpAfterLiftPinDown(bool bCheckVac);
        public virtual void ClmpW(int nTimeout, bool bCheckVac = true)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                m_Signals[enumAlignerSignalTable.MotionCompleted].Reset();
                if (!ClampProduct())
                {
                    SendAlmMsg(enumAlignerError.ControllerSetOutputError);
                    throw new SException((int)enumAlignerError.ControllerSetOutputError, string.Format("Set IO Failed. [{0}]", "CLMP"));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 500);
            }
            //WriteLog(" ClmpW MotionCompleted Set");
            m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
            m_SignalSubSequence.Set();
        }

        private bool ClampProduct()
        {
            m_ALNLogger.WriteLog($"[ALN{BodyNo}]Clamp the product.");
            if (!m_bSimulate)
            {
                m_Motion.SetOutput(1, false);
                m_Motion.SetOutput(2, true);
                if (CheckWaferExisClampVacuum(false) == eClampWorkVacuumStatus.VacuumError)
                {
                    SendAlmMsg(enumAlignerError.TurnOnVacuumFailure);
                    throw new SException((int)enumAlignerError.TurnOnVacuumFailure, string.Format("TurnOnVacuumFailure. [{0}]", "CLMP"));
                }
                if (m_Motion.IsError)
                {
                    return false;
                }
            }
            return true;
        }
        private bool UnclampProduct()
        {
            m_ALNLogger.WriteLog($"[ALN{BodyNo}]Unlamp the product.");
            if (!m_bSimulate)
            {
                m_Motion.SetOutput(1, true);
                m_Motion.SetOutput(2, false);
                if (m_Motion.IsError)
                    return false;
            }
            m_bPanelExistUseVac = false;
            return true;
        }
        private eClampWorkVacuumStatus CheckWaferExisClampVacuum(bool isControlOutput)
        {
            eClampWorkVacuumStatus ret = 0;
            bool In1 = false;
            bool In2 = false;
            int InputTryCount = 0;
            if (m_bSimulate)
            {
                if (m_Wafer == null)
                    return eClampWorkVacuumStatus.WorksNoExist;
                else
                    return eClampWorkVacuumStatus.WorksExist;
            }
            else
            {
                while (true)
                {
                    if (isControlOutput)
                    {
                        UnclampProduct();
                        SpinWait.SpinUntil(() => false, 200);
                        ClampProduct();
                        SpinWait.SpinUntil(() => false, 400);
                    }
                    In1 = m_Motion.GetInput(1);
                    In2 = m_Motion.GetInput(2);
                    if (In1 == true && In2 == true)
                    {
                        WriteLog("Check Panel Exist");
                        m_bPanelExistUseVac = true;
                        ret = eClampWorkVacuumStatus.WorksExist;//有物體Input:1&2 ON 
                        break;
                    }
                    else if (In1 == false && In2 == true)
                    {
                        WriteLog("Check Panel Empty");
                        m_bPanelExistUseVac = false;
                        ret = eClampWorkVacuumStatus.WorksNoExist; //沒物體，有開真空 Input 1: OFF Input 2: ON
                        break;
                    }
                    else
                    {
                        WriteLog("Check Panel unknow");
                        m_bPanelExistUseVac = true;
                        ret = eClampWorkVacuumStatus.VacuumError;//真空失效
                        SpinWait.SpinUntil(() => false, 500);
                        if (InputTryCount > 2)
                            break;
                    }
                    InputTryCount++;
                }
                if (isControlOutput)
                    UnclampProduct();
                return ret;
            }
        }

        #endregion 

        #region =========================== UCLM ===============================================

        //protected abstract void UclmAfterLiftPinUp();
        //protected abstract void UclmAfterLiftPinDown();
        public virtual void UclmW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                m_Signals[enumAlignerSignalTable.MotionCompleted].Reset();
                if (UnclampProduct() == false)
                {
                    SendAlmMsg(enumAlignerError.ControllerSetOutputError);
                    throw new SException((int)enumAlignerError.ControllerSetOutputError, string.Format("Set IO Failed. [{0}]", "UCLM"));
                }
            }
            else
            {
                //SpinWait.SpinUntil(() => false, 1000);//看LOG需一秒
                SpinWait.SpinUntil(() => false, 500);
            }
            //WriteLog(" UclmW MotionCompleted Set");
            m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== ALGN ===============================================


        public void AlgnW(int nTimeout, int nMode = 0, int nPos = -1)
        {
            ALGNTest(nTimeout);
        }

        // A ┌───────┐ B
        //   │       │   
        //   │       │   
        // D └───────┘ C        ╲
        //      ╚╦╝ FINGER
        //       ║   
        void ALGNTest(int Timeout, int nAngleType = 1 , int PhotographMethod = 2)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                int Stage = 0;
                string rawGtlcResult = "";
                string rawCllcResult = "";
                if (false == ClampProduct())
                {
                    SendAlmMsg(enumAlignerError.ControllerSetOutputError);
                    throw new SException((int)enumAlignerError.ControllerSetOutputError, string.Format("Set IO Failed. [{0}]", "CLMP"));
                }
                try
                {

                    m_Motion.ResetInPos();
                    m_Motion.Mabs(m_nMotionTimeout, (int)0, (int)0, (int)0);
                    m_Motion.WaitInPos(m_nMotionTimeout);
                    SetSimulatPosition(0, 0, 0, 0);
                    m_Camera.ResetInPos();
                    m_Camera.CllcW(3000, 0, Stage);
                    m_Camera.WaitInPos(m_nMotionTimeout);

                    //jan
                    if (PhotographMethod == 2)
                    {
                        m_Motion.ResetInPos();
                        m_Motion.AxisMrel(m_nMotionTimeout, enumAGD301Axis.AXS1, 90000);
                        SetSimulatPosition(m_RPos + 90000, m_XPos, m_YPos, m_ZPos);
                        m_Motion.WaitInPos(m_nMotionTimeout);


                        m_Camera.ResetInPos();
                        m_Camera.CllcW(30000, 1, Stage);
                        m_Camera.WaitInPos(m_nMotionTimeout);

                        m_Camera.GtlcW(3000, nAngleType);

                        rawGtlcResult = m_Camera.GetGtlcResult;
                        if (rawGtlcResult.Length == 0)
                        {
                            SendAlmMsg(enumAlignerError.CameraAckLengthZero);
                            throw new SException((int)enumAlignerError.CameraAckLengthZero, string.Format("Get GTLC data length = 0.[{0}]", "ALGN"));
                        }
                        string[] GtlcResult = rawGtlcResult.Split(',');
                        m_ALNLogger.WriteLog(" GTLC:" + rawGtlcResult);
                        if (GtlcResult.Length == 0 || GtlcResult.Length != 5)
                        {
                            SendAlmMsg(enumAlignerError.CameraAckParameterLengthError);
                            throw new SException((int)enumAlignerError.CameraAckParameterLengthError, string.Format("Get GTLC return data format error.", "ALGN"));
                        }
                        double NotchLocation = double.Parse(GtlcResult[0]);
                        double Front = double.Parse(GtlcResult[1]);
                        double Shift_X = double.Parse(GtlcResult[2]) * 1000;
                        double Shift_Y = double.Parse(GtlcResult[3]) * 1000;
                        double Angle = double.Parse(GtlcResult[4]) * 1000;

                        //Shift_X = Shift_Y = Angle = 0;
                        //Angle = 0;
                        if (NotchLocation != 1 && NotchLocation != 2 && NotchLocation != 3 && NotchLocation != 4)
                        {
                            SendAlmMsg(enumAlignerError.CameraNotFoundWorksAndNotch);
                            throw new SException((int)enumAlignerError.CameraNotFoundWorksAndNotch, string.Format("The Camera Not Found Works or Notch.", "ALGN"));

                        }

                        if (Front != 0)//0:front 1:back
                        {
                            SendAlmMsg(enumAlignerError.CameraNotFindFront);
                            throw new SException((int)enumAlignerError.CameraNotFindFront, string.Format("The Camera Not Found Front.", "ALGN"));
                        }

                        if (CheckCameraCalibrateOK(Angle, Shift_Y, Shift_X) == false)
                        {
                            SendAlmMsg(enumAlignerError.CameraCalibrateOffsetOutrange);
                            throw new SException((int)enumAlignerError.CameraCalibrateOffsetOutrange, string.Format("The CalibrateOffset is out of range.", "ALGN"));
                        }
                        if (Math.Abs(Shift_Y) > 5000 || Math.Abs(Shift_X) > 5000)
                        {
                            SendAlmMsg(enumAlignerError.CameraCalibrateOffsetOutrange);
                            throw new SException((int)enumAlignerError.CameraCalibrateOffsetOutrange, string.Format("The CalibrateOffset is out of range.", "ALGN"));
                        }

                        m_Motion.ResetInPos();
                        m_Motion.Mabs(m_nMotionTimeout, (int)Angle, (int)Shift_X, (int)Shift_Y);
                        SetSimulatPosition((int)Angle, (int)Shift_X, (int)Shift_Y, m_ZPos);
                        m_Motion.WaitInPos(m_nMotionTimeout);

                    }
                    else
                    {
                        rawCllcResult = m_Camera.GetCllcResult;

                        if (rawCllcResult.Length == 0)
                        {
                            SendAlmMsg(enumAlignerError.CameraAckLengthZero);
                            throw new SException((int)enumAlignerError.CameraAckLengthZero, string.Format("Get CLLC data length = 0.[{0}]", "ALGN"));
                        }

                        string[] CllcResult = rawCllcResult.Split(',');


                        m_ALNLogger.WriteLog(" ClLC:" + rawCllcResult);

                        if (CllcResult.Length == 0 || CllcResult.Length != 3)
                        {
                            SendAlmMsg(enumAlignerError.CameraAckParameterLengthError);
                            throw new SException((int)enumAlignerError.CameraAckParameterLengthError, string.Format("Get CLLC return data format error.", "ALGN"));
                        }

                        double Shift_X = double.Parse(CllcResult[0]) * 1000;
                        double Shift_Y = double.Parse(CllcResult[1]) * 1000;
                        double Angle = double.Parse(CllcResult[2]) * 1000;


                        if (CheckCameraCalibrateOK(Angle, Shift_Y, Shift_X) == false)
                        {
                            SendAlmMsg(enumAlignerError.CameraCalibrateOffsetOutrange);
                            throw new SException((int)enumAlignerError.CameraCalibrateOffsetOutrange, string.Format("The CalibrateOffset is out of range.", "ALGN"));
                        }
                        if (Math.Abs(Shift_Y) > 5000 || Math.Abs(Shift_X) > 5000)
                        {
                            SendAlmMsg(enumAlignerError.CameraCalibrateOffsetOutrange);
                            throw new SException((int)enumAlignerError.CameraCalibrateOffsetOutrange, string.Format("The CalibrateOffset is out of range.", "ALGN"));
                        }

                        m_Motion.ResetInPos();
                        m_Motion.Mabs(m_nMotionTimeout, (int)Angle, (int)Shift_X, (int)Shift_Y);
                        SetSimulatPosition((int)Angle, (int)Shift_X, (int)Shift_Y, m_ZPos);
                        m_Motion.WaitInPos(m_nMotionTimeout);

                    }

                    //if (false == UnclampProduct())
                    //{
                    //    SendAlmMsg(enumAlignerError.ControllerSetOutputError);
                    //    throw new SException((int)enumAlignerError.ControllerSetOutputError, string.Format("Set IO Failed. [{0}]", "ULMP"));
                    //}
                }
                catch (SException ex)
                {
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                    CatchErrorCode(ex.ErrorID);
                    throw ex;
                }
                catch (Exception ex)
                {
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                    m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                    SendAlmMsg(enumAlignerError.MotionAbnormal);
                    WriteLog("<<Exception>> :" + ex);
                    throw ex;
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 1000);
            }
            //WriteLog(" AlgnW MotionCompleted Set");

            m_Signals[enumAlignerSignalTable.MotionCompleted].Set();

            m_SignalSubSequence.Set();
        }

        bool CheckCameraCalibrateOK(double OffsetR, double OffsetY, double OffsetX)
        {
            double cRot = (OffsetR / 180000) * Math.PI;     //1000 = 1 degree 
            double FingerWidth = 164000;                    //1000 = 1mm 
            double GapFingerToNozzle = 4000;                //1000 = 1mm 
            double NozzleRadius = 140500;                   //1000 = 1mm




            double nX1 = NozzleRadius * Math.Cos(cRot + Math.PI / 4) + OffsetX;
            double nY1 = NozzleRadius * Math.Sin(cRot + Math.PI / 4) + OffsetY;

            double nX2 = NozzleRadius * Math.Cos(cRot + Math.PI / 4 + Math.PI / 2) + OffsetX;
            double nY2 = NozzleRadius * Math.Sin(cRot + Math.PI / 4 + Math.PI / 2) + OffsetY;

            double nX3 = NozzleRadius * Math.Cos(cRot + Math.PI / 4 + Math.PI) + OffsetX;
            double nY3 = NozzleRadius * Math.Sin(cRot + Math.PI / 4 + Math.PI) + OffsetY;

            double nX4 = NozzleRadius * Math.Cos(cRot + Math.PI / 4 + Math.PI * 3 / 2) + OffsetX;
            double nY4 = NozzleRadius * Math.Sin(cRot + Math.PI / 4 + Math.PI * 3 / 2) + OffsetY;

            WriteLog(string.Format("cal:{0},{1}/{2},{3}/{4},{5}/{6},{7}", nX1, nY1, nX2, nY2, nX3, nY3, nX4, nY4));


            if (Math.Abs(nX1) < FingerWidth / 2)
            {
                WriteLog(string.Format("Fail x1"));
                return false;
            }
            if (Math.Abs(nY1) < FingerWidth / 2)
            {
                WriteLog(string.Format("Fail y1"));
                return false;
            }
            if (Math.Abs(nX2) < FingerWidth / 2)
            {
                WriteLog(string.Format("Fail x2"));
                return false;
            }
            if (Math.Abs(nY2) < FingerWidth / 2)
            {
                WriteLog(string.Format("Fail y2"));
                return false;
            }
            if (Math.Abs(nX3) < FingerWidth / 2)
            {
                WriteLog(string.Format("Fail x3"));
                return false;
            }
            if (Math.Abs(nY3) < FingerWidth / 2)
            {
                WriteLog(string.Format("Fail y3"));
                return false;
            }
            if (Math.Abs(nX4) < FingerWidth / 2)
            {
                WriteLog(string.Format("Fail x4"));
                return false;
            }
            if (Math.Abs(nY4) < FingerWidth / 2)
            {
                WriteLog(string.Format("Fail y4"));
                return false;
            }
            return true;
        }
        public void Algn1W(int nTimeout)
        {
            ALGNTest(nTimeout);
        }

        public void AlgnDW(int nTimeout, string strPos)//0~360
        {
            float f = float.Parse(strPos);

            //0~360
            while (f < 0 && f != 0) { f += 360; }
            while (f > 360) { f -= 360; }

            //0:1
            //90:2
            //180:3
            //270:4
            int result = (int)((f + 45) % 360 / 90) + 1;
            result = result > 4 ? 1 : result; // 315~360 要回到 1

            ALGNTest(nTimeout, result);
        }
        /// <summary>
        /// 設定一次性拍照還是兩次性
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="strPos"></param>
        public void AlgnDW(int nTimeout, string strPos, int PhotographMethod)//0~360
        {
            float f = float.Parse(strPos);

            //0~360
            while (f < 0 && f != 0) { f += 360; }
            while (f > 360) { f -= 360; }

            //0:1
            //90:2
            //180:3
            //270:4
            int result = (int)((f + 45) % 360 / 90) + 1;
            result = result > 4 ? 1 : result; // 315~360 要回到 1

            ALGNTest(nTimeout, result, PhotographMethod);
        }
        #endregion

        #region =========================== CALN ===============================================

        //public void CalnW(int nTimeout)
        //{
        //    m_SignalSubSequence.Reset();
        //    if (!m_bSimulate)
        //    {
        //        /*Caln();
        //        if (!_signalAck[enumAlignerCommand.CameraAlignment].WaitOne(nTimeout))
        //        {
        //            SendAlmMsg(enumAlignerError.AckTimeout);
        //            throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send CALN command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.CameraAlignment]));
        //        }
        //        if (_signalAck[enumAlignerCommand.CameraAlignment].bAbnormalTerminal)
        //        {
        //            SendAlmMsg(enumAlignerError.SendCommandFailure);
        //            throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send CALN command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.CameraAlignment]));
        //        }*/
        //    }
        //    else
        //    {

        //    }
        //    m_SignalSubSequence.Set();
        //}
        #endregion

        #region =========================== Rot1EXTD ===========================================

        public virtual void RotationExtdW(int nTimeout, int nPos)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                m_Signals[enumAlignerSignalTable.MotionCompleted].Reset();
                int n = nPos;
                //0~360
                while (n > 360) { n -= 360; }
                try
                {
                    m_Motion.ResetInPos();
                    m_Motion.AxisMabs(nTimeout, enumAGD301Axis.AXS1, n * 1000);//1度 = 1000
                    SetSimulatPosition(n * 1000, m_XPos, m_YPos, m_ZPos);
                    m_Motion.WaitInPos(nTimeout);
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                }
                catch (SException ex)
                {
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                    m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                    CatchErrorCode(ex.ErrorID);
                    WriteLog("<<SException>> :" + ex);
                    throw ex;
                }
                catch (Exception ex)
                {
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                    m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                    SendAlmMsg(enumAlignerError.MotionAbnormal);
                    WriteLog("<<Exception>> :" + ex);
                    throw ex;
                }
            }
            else
            {
                while (nPos >= 360000)
                    nPos -= 360000;
                Raxispos += nPos;
            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== Rot1Step ===========================================

        protected virtual void RotStepW(int nTimeout, int nPos)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                m_Signals[enumAlignerSignalTable.MotionCompleted].Reset();
                try
                {
                    m_Motion.ResetInPos();
                    m_Motion.AxisMrel(nTimeout, enumAGD301Axis.AXS1, nPos);//1度 = 1000 nPos
                    m_Motion.WaitInPos(nTimeout);
                    SetSimulatPosition(m_RPos + nPos, m_XPos, m_YPos, m_ZPos);
                    //WriteLog(" RotStepW MotionCompleted Set");
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                }
                catch (SException ex)
                {
                    m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                    CatchErrorCode(ex.ErrorID);
                    WriteLog("<<SException>> :" + ex);
                    throw ex;
                }
                catch (Exception ex)
                {
                    m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                    SendAlmMsg(enumAlignerError.MotionAbnormal);
                    m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                    WriteLog("<<Exception>> :" + ex);
                    throw ex;
                }
            }
            else
            {
                while (nPos >= 360000)
                    nPos -= 360000;
                Raxispos += nPos;
            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== READ ===============================================

        public void ReadW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Read();
                if (!_signalAck[enumAlignerCommand.ReadID].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send READ command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.ReadID]));
                }
                if (_signalAck[enumAlignerCommand.ReadID].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send READ command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.ReadID]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== EVNT ===============================================

        public void EventW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Event();
                if (!_signalAck[enumAlignerCommand.Event].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Event command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Event]));
                }
                if (_signalAck[enumAlignerCommand.Event].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Event command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Event]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== RSTA ===============================================

        public void ResetW(int nTimeout, int nReset = 0)
        {
            m_SignalSubSequence.Reset();
            m_Signals[enumAlignerSignalTable.ProcessCompleted].Reset();
            if (!m_bSimulate)
            {
                m_Camera.RstaW(nTimeout);
                m_Motion.Reset(nTimeout);
                if (IsError == false)
                    m_strErrCode = "0000";
                if (m_Camera.IsError)//status error
                {
                    m_Camera.RstaW(m_nAckTimeout);
                }
            }
            else
            {
                m_strErrCode = "0000";
            }
            m_Signals[enumAlignerSignalTable.ProcessCompleted].Set();
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== INIT ===============================================

        public void InitW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                m_bIsError = false;
                m_Motion.ResetInPos();
                m_Motion.INIT();
                m_Motion.ResetInPos();
                m_Camera.INIT();
                m_bIsError = IsError;
                //m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                //m_Signals[enumAlignerSignalTable.Remote].Set();
                if (m_bIsError)
                    throw new SException((int)enumAlignerError.InitialFailure, string.Format("Controller or camera has error.[{0}]", "INIT"));
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== STOP ===============================================

        public void StopW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                m_Motion.STOP();
                //WriteLog(" StopW MotionCompleted Set");
                m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
            }
            else
            {
                SpinWait.SpinUntil(() => false, 1000);
            }
            m_SignalSubSequence.Set();
        }
        #endregion \

        #region =========================== PAUS ===============================================

        public void PausW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Paus();
                if (!_signalAck[enumAlignerCommand.Pause].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Pause command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Pause]));
                }
                if (_signalAck[enumAlignerCommand.Pause].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Pause command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Pause]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== MODE ===============================================

        public void ModeW(int nTimeout, int nMode)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Mode(nMode);
                if (!_signalAck[enumAlignerCommand.Mode].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Mode command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Mode]));
                }
                if (_signalAck[enumAlignerCommand.Mode].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Mode command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Mode]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== WTDT ===============================================

        public void WtdtW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            m_AlgnParam.WriteIni();
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== RTDT ===============================================

        public void RtdtW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            m_AlgnParam.LoadIni();
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== SSPD ===============================================

        public void SspdW(int nTimeout, int nVariable)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*_signals[enumAlignerSignalTable.ProcessCompleted].Reset();
                Sspd(nVariable);
                if (!_signalAck[enumAlignerCommand.Speed].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send SSPD command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Speed]));
                }
                if (_signalAck[enumAlignerCommand.Speed].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send SSPD command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Speed]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== STAT ===============================================

        public void StatW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Stat();
                if (!_signalAck[enumAlignerCommand.Status].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Status command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Status]));
                }
                if (_signalAck[enumAlignerCommand.Status].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Status command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Status]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GPIO ===============================================

        public void GpioW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gpio();
                if (!_signalAck[enumAlignerCommand.GetIO].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GPIO command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetIO]));
                }
                if (_signalAck[enumAlignerCommand.GetIO].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GPIO command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetIO]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GVER ===============================================

        public void GverW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gver();
                if (!_signalAck[enumAlignerCommand.GetVersion].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GVER command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetVersion]));
                }
                if (_signalAck[enumAlignerCommand.GetVersion].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GVER command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetVersion]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GLOG ===============================================

        public void GlogW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Glog();
                if (!_signalAck[enumAlignerCommand.GetLog].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GLOG command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetLog]));
                }
                if (_signalAck[enumAlignerCommand.GetLog].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GLOG command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetLog]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== STIM ===============================================

        public void StimW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Stim();
                if (!_signalAck[enumAlignerCommand.SetDateTime].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send STIM command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.SetDateTime]));
                }
                if (_signalAck[enumAlignerCommand.SetDateTime].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send STIM command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.SetDateTime]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GTIM ===============================================

        public void GtimW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gtim();
                if (!_signalAck[enumAlignerCommand.GetDateTime].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GTIM command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetDateTime]));
                }
                if (_signalAck[enumAlignerCommand.GetDateTime].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GTIM command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetDateTime]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GPOS ===============================================

        public void GposW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            /*Gpos();
             if (!_signalAck[enumAlignerCommand.GetPos].WaitOne(nTimeout))
             {
                 SendAlmMsg(enumAlignerError.AckTimeout);
                 throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetPos]));
             }
             if (_signalAck[enumAlignerCommand.GetPos].bAbnormalTerminal)
             {
                 SendAlmMsg(enumAlignerError.SendCommandFailure);
                 throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetPos]));
             }*/
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== XAX1.GPOS ==========================================

        public void GposXW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            /*GposX();
            if (!_signalAck[enumAlignerCommand.GetXAxisPos].WaitOne(nTimeout))
            {
                SendAlmMsg(enumAlignerError.AckTimeout);
                throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send XAxis GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetXAxisPos]));
            }
            if (_signalAck[enumAlignerCommand.GetXAxisPos].bAbnormalTerminal)
            {
                SendAlmMsg(enumAlignerError.SendCommandFailure);
                throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send XAxis GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetXAxisPos]));
            }*/
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== YAX1.GPOS ==========================================


        public void GposYW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            /*GposY();
            if (!_signalAck[enumAlignerCommand.GetYAxisPos].WaitOne(nTimeout))
            {
                SendAlmMsg(enumAlignerError.AckTimeout);
                throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send YAxis GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetYAxisPos]));
            }
            if (_signalAck[enumAlignerCommand.GetYAxisPos].bAbnormalTerminal)
            {
                SendAlmMsg(enumAlignerError.SendCommandFailure);
                throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send YAxis GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetYAxisPos]));
            }*/
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== ZAX1.GPOS ==========================================

        public void GposZW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            /*GposZ();
            if (!_signalAck[enumAlignerCommand.GetZAxisPos].WaitOne(nTimeout))
            {
                SendAlmMsg(enumAlignerError.AckTimeout);
                throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send ZAxis GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetZAxisPos]));
            }
            if (_signalAck[enumAlignerCommand.GetZAxisPos].bAbnormalTerminal)
            {
                SendAlmMsg(enumAlignerError.SendCommandFailure);
                throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send ZAxis GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetZAxisPos]));
            }*/
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== ROT1.GPOS ==========================================

        public void GposRW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*GposR();
                if (!_signalAck[enumAlignerCommand.GetRAxisPos].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Rot1 GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetRAxisPos]));
                }
                if (_signalAck[enumAlignerCommand.GetRAxisPos].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Rot1 GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetRAxisPos]));
                }*/
            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GWID ===============================================

        public void GwidW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gwid();
                if (!_signalAck[enumAlignerCommand.GetType].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GWID command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetType]));
                }
                if (_signalAck[enumAlignerCommand.GetType].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GWID command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetType]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== TDST ===============================================

        public void TdstW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Tdst();
                if (!_signalAck[enumAlignerCommand.NotchStopPos].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send TDST command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.NotchStopPos]));
                }
                if (_signalAck[enumAlignerCommand.NotchStopPos].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send TDST command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.NotchStopPos]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GTAD ===============================================

        public void GtadW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gtad();
                if (!_signalAck[enumAlignerCommand.GetSensorValue].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GTAD command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetSensorValue]));
                }
                if (_signalAck[enumAlignerCommand.GetSensorValue].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GTAD command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetSensorValue]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GPRS ===============================================

        public void GprsW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gprs();
                if (!_signalAck[enumAlignerCommand.GetVacuumValue].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GPRS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetVacuumValue]));
                }
                if (_signalAck[enumAlignerCommand.GetVacuumValue].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GPRS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetVacuumValue]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion 

        #region =========================== GSIZ ===============================================

        public void GsizW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gsiz();
                if (!_signalAck[enumAlignerCommand.GetSize].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GSIZ command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetSize]));
                }
                if (_signalAck[enumAlignerCommand.GetSize].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GSIZ command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetSize]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== SSIZ ===============================================

        public void SsizW(int nTimeout, int nSsiz = 0)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Ssiz(nSsiz);
                if (!_signalAck[enumAlignerCommand.SetSize].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send SSIZ command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.SetSize]));
                }
                if (_signalAck[enumAlignerCommand.SetSize].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send SSIZ command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.SetSize]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GTID ===============================================

        public void GtidW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gtid();
                if (!_signalAck[enumAlignerCommand.GetID].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GTID command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetID]));
                }
                if (_signalAck[enumAlignerCommand.GetID].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GTID command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetID]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }
        #endregion

        #region =========================== GTMP ===============================================


        public void GtmpW(int nTimeout)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
                /*Gtmp();
                if (!_signalAck[enumAlignerCommand.GetMP].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GTMP command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetMP]));
                }
                if (_signalAck[enumAlignerCommand.GetMP].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GTMP command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetMP]));
                }*/
            }
            else
            {

            }
            m_SignalSubSequence.Set();
        }

        #endregion
        #region =========================== DALN STDT GTDT ===============================================
        public void SetDmprW(int nTimeout, int P1, int P2, string strDat)
        {
            m_SignalSubSequence.Reset();
            m_AlgnParam.SetDALN(P1, P2, strDat);
            m_SignalSubSequence.Set();
        }
        public string[] GetDALNData(int P1)
        {
            return m_AlgnParam.GetDALN(P1);
        }

        public void GetDalnW(int nTimeout, int P1, string strDat)
        {
            m_SignalSubSequence.Reset();
            if (!m_bSimulate)
            {
            }
            else
            {
            }
            m_SignalSubSequence.Set();
        }
        #endregion



        public void ResetChangeModeCompleted()
        {
            m_Signals[enumAlignerSignalTable.Remote].Reset();
        }
        public void WaitChangeModeCompleted(int nTimeout)
        {
            if (!m_bSimulate)
            {
                if (!m_Signals[enumAlignerSignalTable.Remote].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.InitialFailure);
                    throw new SException((int)enumAlignerError.InitialFailure, string.Format("Wait Mode was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (m_Signals[enumAlignerSignalTable.Remote].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.InitialFailure);
                    throw new SException((int)enumAlignerError.InitialFailure, string.Format("Motion is Mode end."));
                }
            }
        }

        public void ResetOrgnSinal()
        {
            m_Signals[enumAlignerSignalTable.OPRCompleted].Reset();
            m_bStatOrgnComplete = false;
        }
        public void WaitOrgnCompleted(int TimeOut)
        {
            if (!m_bSimulate)
            {
                if (!m_Signals[enumAlignerSignalTable.OPRCompleted].WaitOne(TimeOut))
                {
                    SendAlmMsg(enumAlignerError.OriginPosReturnTimeout);
                    throw new SException((int)(enumAlignerError.OriginPosReturnTimeout), "Aligner Orgn Fail");
                }
                if (m_Signals[enumAlignerSignalTable.OPRCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.OriginPosReturnFailure);
                    throw new SException((int)(enumAlignerError.OriginPosReturnFailure), "Aligner Orgn Fail");
                }
            }
            else
            {
                m_bStatOrgnComplete = true;
                SpinWait.SpinUntil(() => false, 100);
            }
        }

        public void ResetInPos()
        {
            m_Signals[enumAlignerSignalTable.MotionCompleted].Reset();
            m_eStatInPos = enumAlignerStatus.Moving;
        }
        public void WaitInPos(int nTimeout)
        {
            SpinWait.SpinUntil(() => false, 200);
            if (!m_bSimulate)
            {
                if (!m_Signals[enumAlignerSignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.MotionTimeout);
                    throw new SException((int)enumAlignerError.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.MotionAbnormal);
                    throw new SException((int)enumAlignerError.MotionAbnormal, string.Format("Motion is abnormal end."));
                }
                m_eStatInPos = m_Motion.IsMoving ? enumAlignerStatus.Moving : enumAlignerStatus.InPos;
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
                m_eStatInPos = enumAlignerStatus.InPos;
            }
        }

        #region =========================== OnOccurError =======================================
        //  發生STAT異常
        protected void SendAlmMsg(string strCode)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  ALN1 13 11 00000
            //  ALN2 14 11 00000          
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));
            SendAlmMsg(enumAlignerError.Status_Error);
        }
        protected void SendAlmMsg(int nCode)
        {
            WriteLog(string.Format("Occur stat Error : {0}", nCode));
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));
            SendAlmMsg(enumAlignerError.Status_Error);
        }
        //  解除STAT異常
        protected void RestAlmMsg(string strCode)
        {
            WriteLog(string.Format("Rest stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  ALN1 15 11 00000
            //  ALN2 16 11 00000          
            int nCode = Convert.ToInt32(strCode, 16) /*+ (13 + BodyNo - 1) * 10000000 + 11 * 100000*/;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));

        }
        //  Cancel Code
        protected void SendCancelMsg(string strCode)
        {
            WriteLog(string.Format("Occur cancel Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  ALN1 15 11 00000
            //  ALN2 16 11 00000          
            int nCode = Convert.ToInt32(strCode, 16) /*+ (13 + BodyNo - 1) * 10000000 + 12 * 100000*/;
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        protected void SendCancelMsg(int nCode)
        {
            WriteLog(string.Format("Occur cancel Error : {0}", nCode));
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        protected void SendAlmMsg(enumAlignerError eAlarm)
        {
            m_strErrCode = ((int)eAlarm).ToString("X4");
            WriteLog(string.Format("Occur eAlarm Error : {0}", eAlarm));
            int nCode = (int)eAlarm;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }

        //  發生警告
        public void SendWarningMsg(enumAlignerWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur Warning  : {0}", eWarning), meberName, lineNumber);
            int nCode = (int)eWarning;
            OnOccurWarning?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除警告
        public void RestWarningMsg(enumAlignerWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Reset Warning  : {0}", eWarning), meberName, lineNumber);
            int nCode = (int)eWarning;
            OnOccurWarningRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        #endregion

        #region Run貨權
        static private object _objHolding = new object(); //鎖臨界區間
        static private bool _blnHold = false; //run貨權
        private bool _bIsRunning = false;
        public bool IsHoldPermission()//TRUE 表示是被其他HOLD
        {
            lock (_objHolding)
            {
                return _blnHold && _bIsRunning == false;
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

        //==============================================================================     
        public I_BarCode Barcode { get { return m_Barcode; } }
        public void BarcodeOpen() { if (m_Barcode != null) m_Barcode.Open(); }//20240828
        public bool IsBarcodeConnected { get { return m_Barcode != null && m_Barcode.Connected; } }//20240828
        public bool IsBarcodeEnable { get { return m_Barcode != null; } }//20240828
        public string BarcodeRead() { return m_Barcode == null ? "No Barcode" : m_Barcode.Read(); }//20240828
        //==============================================================================     
        public bool IsZaxsInBottom() { return true; }

        public bool WaferExists()
        {
            //需要處理
            //CheckWaferExisClampVacuum(true);
            return m_bPanelExistUseVac;
        }
        public bool IsClamp()
        {
            //需要處理
            if (m_Motion.GetInput(1) == true || m_Motion.GetInput(2) == true)
            {
                m_bPanelExistUseVac = true;
                return true; //有物體Input:1&2 ON 
            }
            else
            {
                return false; //沒物體，有開真空 Input 1: OFF Input 2: ON
            }
        }
        public bool IsUnClamp()
        {
            //需要處理
            if (m_Motion.GetInput(1) == true || m_Motion.GetInput(2) == true)
            {
                //m_bPanelExistUseVac = true;
                return false; //有物體Input:1&2 ON 
            }
            else
            {
                return true; //沒物體，有開真空 Input 1: OFF Input 2: ON
            }
        }
        public bool IsAirOK() { throw new NotImplementedException(); }
        public bool IsFanOK() { throw new NotImplementedException(); }


        public void AlignerMoveTest_MABS(int R, int X, int Y)
        {

            try
            {
                m_Motion.ResetInPos();
                m_Motion.Mabs(30000, R, X, Y);
                m_Motion.WaitInPos(60000);
                //WriteLog(" AlignerMoveTest_MABS MotionCompleted Set");
                m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
            }
            catch (SException ex)
            {
                m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                CatchErrorCode(ex.ErrorID);
                m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                WriteLog("<<SException>> :" + ex);
                throw ex;
            }
            catch (Exception ex)
            {
                m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                SendAlmMsg(enumAlignerError.MotionAbnormal);
                m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                WriteLog("<<Exception>> :" + ex);
                throw ex;
            }
        }
        public void AlignerMoveTest_MREL(int R, int X, int Y)
        {
            try
            {
                m_Motion.ResetInPos();
                m_Motion.Mrel(60000, R, X, Y);
                m_Motion.WaitInPos(60000);
                //WriteLog(" AlignerMoveTest_MREL MotionCompleted Set");
                m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
            }
            catch (SException ex)
            {
                m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                WriteLog("<<SException>> :" + ex);
                CatchErrorCode(ex.ErrorID);
                throw ex;
            }
            catch (Exception ex)
            {
                m_Signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                SendAlmMsg(enumAlignerError.MotionAbnormal);
                m_Signals[enumAlignerSignalTable.MotionCompleted].Set();
                WriteLog("<<Exception>> :" + ex);
            }

        }

        void CatchErrorCode(int ErrorID)
        {
            switch (ErrorID)
            {
                //Agito Controller
                case (int)enumAgitoError.ControllerConnectError:
                    SendAlmMsg(enumAlignerError.ControllerConnectError);
                    break;
                case (int)enumAgitoError.InitialFailure:
                    SendAlmMsg(enumAlignerError.InitialFailure);
                    break;
                case (int)enumAgitoError.InputAxisNoError:
                    SendAlmMsg(enumAlignerError.InputAxisNoError);
                    break;
                case (int)enumAgitoError.Axis123HomingError:
                    SendAlmMsg(enumAlignerError.ControllerHomingError);
                    break;
                case (int)enumAgitoError.Axis123HomingTimeout:
                    SendAlmMsg(enumAlignerError.ControllerHomingTimeout);
                    break;
                case (int)enumAgitoError.Axis123MovingError:
                    SendAlmMsg(enumAlignerError.ControllerMovingError);
                    break;
                case (int)enumAgitoError.Axis123MovingTimeout:
                    SendAlmMsg(enumAlignerError.ControllerMovingTimeout);
                    break;
                case (int)enumAgitoError.Axis123MovingControllerError:
                    SendAlmMsg(enumAlignerError.ControllerMovingError);
                    break;
                case (int)enumAgitoError.Axis123IsMoving:
                    SendAlmMsg(enumAlignerError.ControllerIsMoving);
                    break;

                case (int)enumAgitoError.Axis1HomingError:
                    SendAlmMsg(enumAlignerError.AxisRHomingError);
                    break;
                case (int)enumAgitoError.Axis1HomingTimeout:
                    SendAlmMsg(enumAlignerError.AxisRHomingTimeout);
                    break;
                case (int)enumAgitoError.Axis1MovingError:
                case (int)enumAgitoError.Axis1IsMoving:
                case (int)enumAgitoError.Axis1MovingControllerError:
                    SendAlmMsg(enumAlignerError.AxisRMovingError);
                    break;
                case (int)enumAgitoError.Axis1MovingTimeout:
                    SendAlmMsg(enumAlignerError.AxisRMovingTimeout);
                    break;


                case (int)enumAgitoError.Axis2HomingError:
                    SendAlmMsg(enumAlignerError.AxisYHomingError);
                    break;
                case (int)enumAgitoError.Axis2HomingTimeout:
                    SendAlmMsg(enumAlignerError.AxisYHomingTimeout);
                    break;
                case (int)enumAgitoError.Axis2MovingError:
                case (int)enumAgitoError.Axis2IsMoving:
                case (int)enumAgitoError.Axis2MovingControllerError:
                    SendAlmMsg(enumAlignerError.AxisYMovingError);
                    break;
                case (int)enumAgitoError.Axis2MovingTimeout:
                    SendAlmMsg(enumAlignerError.AxisYMovingTimeout);
                    break;


                case (int)enumAgitoError.Axis3HomingError:
                    SendAlmMsg(enumAlignerError.AxisXHomingError);
                    break;
                case (int)enumAgitoError.Axis3HomingTimeout:
                    SendAlmMsg(enumAlignerError.AxisXHomingTimeout);
                    break;
                case (int)enumAgitoError.Axis3MovingError:
                case (int)enumAgitoError.Axis3IsMoving:
                case (int)enumAgitoError.Axis3MovingControllerError:
                    SendAlmMsg(enumAlignerError.AxisXMovingError);
                    break;
                case (int)enumAgitoError.Axis3MovingTimeout:
                    SendAlmMsg(enumAlignerError.AxisXMovingTimeout);
                    break;

                //Camera
                case (int)enumAlignerError.CameraSocketError:
                case (int)enumAlignerError.CameraTimeoutError:
                case (int)enumAlignerError.CameraInitialError:
                case (int)enumAlignerError.CameraImageIncompleteError:
                case (int)enumAlignerError.CameraFOUPTypeError:
                case (int)enumAlignerError.CameraParameterSettingFail:
                case (int)enumAlignerError.CameraAlgorithmCalculationFail:
                case (int)enumAlignerError.CameraGetParameterFail:
                case (int)enumAlignerError.CameraGrabImageError:
                case (int)enumAlignerError.CameraSaveTeachingDataError:
                case (int)enumAlignerError.CameraNotFoundWorksAndNotch:

                case (int)enumAlignerError.CameraCalibrateOffsetOutrange:
                case (int)enumAlignerError.CameraAckLengthZero:
                case (int)enumAlignerError.CameraAckParameterLengthError:
                case (int)enumAlignerError.CameraCustomError:
                case (int)enumAlignerError.CameraCancelError:

                case (int)enumAlignerError.CameraDisconnect:
                case (int)enumAlignerError.CameraStatusError:
                case (int)enumAlignerError.CameraSendCommandFailure:
                case (int)enumAlignerError.CameraSendCommandAckTimeout:
                case (int)enumAlignerError.CameraInitialTimeout:
                case (int)enumAlignerError.CameraInitialFailure:
                case (int)enumAlignerError.CameraOriginTimeout:
                case (int)enumAlignerError.CameraOriginFailure:
                case (int)enumAlignerError.CameraMotionTimeout:
                case (int)enumAlignerError.CameraMotionAbnormal:
                    SendAlmMsg((enumAlignerError)ErrorID);
                    break;
                default:
                    SendAlmMsg(ErrorID);
                    break;


            }


        }

        //==============================================================================

        public virtual bool IsReadyToLoad()
        {
            bool b = Math.Abs(Xaxispos) < 100 && Math.Abs(Yaxispos) < 100 && Math.Abs(Raxispos) < 100 && IsMoving == false && Wafer == null;

            return b;
        }
        public void TriggerSException(enumAlignerError eError)
        {
            SendAlmMsg(eError);
            throw new SException((int)(eError), "SException:" + eError);
        }

        private void SetSimulatPosition(int RPos, int XPos, int YPos, int ZPos)
        {
            m_XPos = XPos;
            m_YPos = YPos;
            m_ZPos = ZPos;
            m_RPos = RPos;
        }


    }
}
