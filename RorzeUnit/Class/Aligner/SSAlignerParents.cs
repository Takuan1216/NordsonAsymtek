using RorzeApi;
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.Aligner.Enum;
using RorzeUnit.Class.Aligner.Event;
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
using static RorzeUnit.Class.SWafer;

namespace RorzeUnit.Class.Aligner
{
    public abstract class SSAlignerParents : I_Aligner
    {
        //==============================================================================
        #region =========================== private ============================================
        protected bool m_bSimulate;
        protected enumAlignerMode m_eStatMode;        //記憶的STAT S1第1 bit  
        protected bool m_bStatOrgnComplete;           //記憶的STAT S1第2 bit
        protected bool m_bStatProcessed;              //記憶的STAT S1第3 bit
        protected enumAlignerStatus m_eStatInPos;     //記憶的STAT S1第4 bit   
        protected int m_nSpeed;                       //記憶的STAT S1第5 bit     
        protected string m_strErrCode = "0000";       //記憶的STAT S2

        protected int m_nAckTimeout = 3000;
        protected int m_nMotionTimeout = 60000;
        protected SLogger _logger = SLogger.GetLogger("CommunicationLog");
        protected sRorzeSocket m_Socket;

        protected bool m_bZaxsInBottom = false;
        protected bool m_bUnClampLiftPinUp = false;//對應 I finger
        private bool m_bAlignmentStart = false;
        private bool m_bProcessStart = false;

        private SWafer _wafer;
        private bool m_bRobotExtand = false;

        I_BarCode m_Barcode;
        #endregion
        //==============================================================================
        #region =========================== public =============================================      
        public virtual bool Connected { get; private set; }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }
        public string VersionData { get; private set; }
        //STAT S1第1 bit
        public enumAlignerMode StatMode { get { return m_eStatMode; } }
        public bool IsInitialized { get { return m_eStatMode == enumAlignerMode.Remote; } }
        //STAT S1第2 bit
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }
        //STAT S1第3 bit
        public bool IsProcessing { get { return m_bStatProcessed; } }
        //STAT S1第4 bit
        public enumAlignerStatus InPos { get { return m_eStatInPos; } }
        public bool IsMoving { get { return m_eStatInPos == enumAlignerStatus.Moving; } }
        //STAT S1第5 bit
        public int GetSpeed { get { return m_nSpeed; } }
        //STAT S2
        public bool IsError { get { return (m_strErrCode != "0000"); } }
        public int Xaxispos { get; protected set; }
        public int Yaxispos { get; protected set; }
        public int Zaxispos { get; protected set; }
        public int Raxispos { get; protected set; }
        public int Raxispos_Approximation { get; protected set; }
        public bool AlignmentStart { get { return m_bAlignmentStart; } set { m_bAlignmentStart = value; } }
        public bool ProcessStart { get { return m_bProcessStart; } set { m_bProcessStart = value; } }
        public SRA320GPIO GPIO { get; protected set; }
        public SWafer Wafer
        {
            get { return _wafer; }
            set
            {
                _wafer = value;

                Aligner_WaferChange?.Invoke(this, new WaferDataEventArgs(_wafer));  //  更新UI  //  更新UI

                if (_wafer == null) return;

                OnAssignWaferData?.Invoke(this, new WaferDataEventArgs(_wafer));
            }
        }
        public ConcurrentQueue<SWafer> queCommand { get; set; }
        public ConcurrentQueue<SWafer> quePreCommand { get; set; }

        public bool IsRobotExtend { get { return m_bRobotExtand; } }
        public bool SetRobotExtend { set { m_bRobotExtand = value; } }
        public enumWaferSize WaferType { get; protected set; }
        public I_BarCode Barcode { get { return m_Barcode; } }


        public int _AckTimeout { get { return m_nAckTimeout; } }
        public int _MotionTimeout { get { return m_nMotionTimeout; } }


        #endregion
        //==============================================================================
        #region =========================== Event ==============================================
        public event EventHandler<WaferDataEventArgs> OnAssignWaferData;
        public event EventHandler<WaferDataEventArgs> Aligner_WaferChange;  //  更新UI
        public event EventHandler<WaferDataEventArgs> OnAligCompelet;  //  alig end 

        public event EventHandler<bool> OnManualCompleted;
        public virtual event EventHandler<bool> OnORGNComplete;
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

        protected event SRA320IOChangelHandler OnIOChange;

        #endregion
        #region =========================== Thread =============================================
        private SInterruptOneThread _threadOrgn;             //原點復歸控制
        private SInterruptOneThreadINT_INT _threadHome;      //
        private SInterruptOneThread _threadClmp;          //
        private SInterruptOneThread _threadUclm;          //
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
        private SInterruptOneThread _threadManualFunc;
        private SPollingThread _pollingAuto;                 //自動流程控管   
        private SPollingThread _exePolling;
        #endregion
        #region =========================== Delegate ===========================================
        public dlgv_wafer AssignToRobotQueue { get; set; }//丟給robot作排程
        #endregion
        //==============================================================================
        public SSAlignerParents(string strIP, int nPort, int nBodyNo, bool bDisable, bool bSimulate, bool bUnClampLiftPinUp, I_BarCode barcode
            , sServer Sever = null)
        {
            BodyNo = nBodyNo;
            Disable = bDisable;
            m_bSimulate = bSimulate;
            m_bUnClampLiftPinUp = bUnClampLiftPinUp;//對應 I finger
            m_Barcode = barcode;
            GPIO = new SRA320GPIO("00000000", "00000000");

            for (int nCnt = 0; nCnt < (int)enumAlignerCommand.Max; nCnt++)
                _signalAck.Add((enumAlignerCommand)nCnt, new SSignal(false, EventResetMode.ManualReset));

            for (int i = 0; i < (int)enumAlignerSignalTable.Max; i++)
                _signals.Add((enumAlignerSignalTable)i, new SSignal(false, EventResetMode.ManualReset));

            _signals[enumAlignerSignalTable.ProcessCompleted].Set();

            _signalSubSequence = new SSignal(false, EventResetMode.ManualReset);

            if (GParam.theInst.GetAlignerMode(nBodyNo - 1) != enumAlignerType.TurnTable)
            {
                m_Socket = new sRorzeSocket(strIP, nPort, nBodyNo, "ALN", bSimulate, Sever);
            }
            else
            {
                m_Socket = new sRorzeSocket(strIP, nPort, nBodyNo, "TBL", bSimulate, Sever);
            }

            _threadOrgn = new SInterruptOneThread(ExeORGN);
            _threadHome = new SInterruptOneThreadINT_INT(ExeHome);
            _threadClmp = new SInterruptOneThread(ExeClmp);
            _threadUclm = new SInterruptOneThread(ExeUclm);
            _threadAlgn = new SInterruptOneThreadINT(ExeAlgn);
            _threadAlgn1 = new SInterruptOneThread(ExeAlgn1);
            _threadRot1Extd = new SInterruptOneThreadINT(ExeRot1Extd);
            _threadRot1Step = new SInterruptOneThreadINT(ExeRot1Step);
            _threadStop = new SInterruptOneThread(ExeStop);
            _threadPause = new SInterruptOneThread(ExePause);
            _threadMode = new SInterruptOneThreadINT(ExeMode);
            _threadWtdt = new SInterruptOneThread(ExeWtdt);
            _threadSspd = new SInterruptOneThreadINT(ExeSspd);
            _threadGver = new SInterruptOneThread(ExeGver);
            _threadSsiz = new SInterruptOneThreadINT(ExeSsiz);
            _threadGsiz = new SInterruptOneThread(ExeGsiz);
            _threadReset = new SInterruptOneThreadINT(ExeRsta);
            _threadInit = new SInterruptOneThread(ExeInit);

            _threadManualFunc = new SInterruptOneThread(ExeManualFunction);

            _pollingAuto = new SPollingThread(1);
            _pollingAuto.DoPolling += _pollingAuto_DoPolling;

            _exePolling = new SPollingThread(1);
            _exePolling.DoPolling += _exeDequeueMsg_DoPolling;


            m_bStatOrgnComplete = false;

            quePreCommand = new ConcurrentQueue<SWafer>();
            queCommand = new ConcurrentQueue<SWafer>();


            _exePolling.Set();

            CreateMessage();
            m_Barcode = barcode;
        }
        ~SSAlignerParents()
        {
            _pollingAuto.Close();
            _pollingAuto.Dispose();

            _exePolling.Close();
            _exePolling.Dispose();
        }
        //==============================================================================
        public virtual void Open() { m_Socket.Open(); }
        private void _exeDequeueMsg_DoPolling()
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

                    enumAlignerCommand cmd = enumAlignerCommand.GetVersion;
                    bool bUnknownCmd = true;

                    foreach (string scmd in _dicCmdsTable.Values) //查字典
                    {
                        if (strFrame.Contains(string.Format("ALN{0}.{1}", this.BodyNo, scmd)))
                        {
                            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                            bUnknownCmd = false; //認識這個指令
                            break;
                        }
                        else if (GParam.theInst.GetAlignerMode(BodyNo - 1) == enumAlignerType.TurnTable && strFrame.Contains(string.Format("TBL{0}.{1}", this.BodyNo, scmd)))
                        {
                            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                            bUnknownCmd = false; //認識這個指令
                            break;
                        }
                    }

                    if (bUnknownCmd) //不認識的封包
                    {
                        WriteLog(string.Format("<<<ByPassReceive>>> Got unknown frame and pass to process. [{0}]", strFrame));
                        continue;
                    }
                    WriteLog(strFrame);

                    switch (strFrame[0]) //命令種類
                    {
                        case 'c': //cancel
                            OnCancelAck(this, new AlignerProtoclEventArgs(strFrame));
                            break;
                        case 'n': //nak
                            _signalAck[cmd].bAbnormalTerminal = true;
                            _signalAck[cmd].Set();
                            break;
                        case 'a': //ack
                            OnAck(this, new AlignerProtoclEventArgs(strFrame));
                            _signalAck[cmd].Set();
                            break;
                        case 'e':
                            OnAck(this, new AlignerProtoclEventArgs(strFrame));
                            break;
                        default:

                            break;
                    }
                }
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> _exePolling_DoPolling:" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> _exePolling_DoPolling:" + ex);
            }
        }
        void OnAck(object sender, AlignerProtoclEventArgs e)
        {
            enumAlignerCommand cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                case enumAlignerCommand.Status:
                    AnalysisStatus(e.Frame.Value);
                    break;
                case enumAlignerCommand.GetIO:
                    AnalysisGPIO(e.Frame.Value);
                    break;
                case enumAlignerCommand.GetPos:
                    AnalysisGPOS(e.Frame.Value);
                    break;
                case enumAlignerCommand.GetXAxisPos:
                    Xaxispos = int.Parse(e.Frame.Value);
                    break;
                case enumAlignerCommand.GetYAxisPos:
                    Yaxispos = int.Parse(e.Frame.Value);
                    break;
                case enumAlignerCommand.GetZAxisPos:
                    Zaxispos = int.Parse(e.Frame.Value);
                    break;
                case enumAlignerCommand.GetRAxisPos:
                    Raxispos = int.Parse(e.Frame.Value);
                    break;
                case enumAlignerCommand.GetVersion:
                    VersionData = e.Frame.Value;
                    break;
                case enumAlignerCommand.GetDateTime:
                    break;
                case enumAlignerCommand.GetDCST:
                    //AnalysisDCST(e.Frame.Value);
                    break;
                case enumAlignerCommand.GetDPRM:
                    //AnalysisDPRM(e.Frame.Value);
                    break;
                case enumAlignerCommand.ClientConnected:
                    Connected = true;
                    break;
                default:
                    break;
            }
        }
        void OnCancelAck(object sender, AlignerProtoclEventArgs e)
        {
            enumAlignerCommand cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;
            AnalysisCancel(e.Frame.Value);
        }
        private void AnalysisStatus(string strFrame)
        {
            if (!strFrame.Contains('/'))
            {
                WriteLog("the format of STAT frame has error, '/' not found! [" + strFrame + "]");
                return;
            }
            string[] str = strFrame.Split('/');
            string s1 = str[0];
            string s2 = str[1];

            //S1.bit#1 operation mode
            switch (s1[0])
            {
                case '0':
                    m_eStatMode = enumAlignerMode.Initializing;
                    //_signals[enumAlignerSignalTable.Remote].Reset();
                    break;
                case '1':
                    m_eStatMode = enumAlignerMode.Remote;
                    _signals[enumAlignerSignalTable.Remote].Set();
                    break;
                case '2':
                    m_eStatMode = enumAlignerMode.Maintenance;
                    _signals[enumAlignerSignalTable.Remote].Set();
                    break;
                case '3':
                    m_eStatMode = enumAlignerMode.Recovery;
                    break;
                default: break;
            }

            //S1.bit#2 origin return complete
            if (s1[1] == '0')
                _signals[enumAlignerSignalTable.OPRCompleted].Reset();
            else
                _signals[enumAlignerSignalTable.OPRCompleted].Set();
            m_bStatOrgnComplete = s1[1] == '1';

            //S1.bit#3 processing command
            if (s1[2] == '0')
                _signals[enumAlignerSignalTable.ProcessCompleted].Set();
            else
                _signals[enumAlignerSignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = s1[2] == '1';

            //S1.bit#4 operation status
            switch (s1[3])
            {
                case '0': m_eStatInPos = enumAlignerStatus.InPos; break;
                case '1': m_eStatInPos = enumAlignerStatus.Moving; break;
                case '2': m_eStatInPos = enumAlignerStatus.Pause; break;
            }

            //S1.bit#5 operation speed   
            if (s1[4] >= '0' && s1[4] <= '9') m_nSpeed = s1[4] - '0';
            else if (s1[4] >= 'A' && s1[4] <= 'K') m_nSpeed = s1[4] - 'A' + 10;
            if (m_nSpeed == 0) m_nMotionTimeout = 60000;
            else m_nMotionTimeout = 60000 * 3;

            //S2
            if (Convert.ToInt32(s2, 16) > 0)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumAlignerSignalTable.MotionCompleted].Set();
                SendAlmMsg(s2);
                m_strErrCode = s2;
            }
            else
            {
                if (m_eStatInPos == enumAlignerStatus.InPos)//運動到位      
                    _signals[enumAlignerSignalTable.MotionCompleted].Set();
                else
                    _signals[enumAlignerSignalTable.MotionCompleted].Reset();

                if (m_strErrCode != "0000")
                {
                    RestAlmMsg(m_strErrCode);
                    m_strErrCode = "0000";
                }
            }
        }
        private void AnalysisCancel(string strFrame)
        {
            if (Convert.ToInt32(strFrame, 16) > 0)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumAlignerSignalTable.MotionCompleted].Set(); //有moving過才可以Set

                SendCancelMsg(strFrame);
            }
        }
        protected virtual void AnalysisGPOS(string strFrame)
        {
            try
            {
                if (!strFrame.Contains('/')) { return; }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> AnalysisGPOS:" + ex);
            }
        }
        protected virtual void AnalysisGPIO(string strFrame)
        {
            if (!strFrame.Contains('/')) { return; }

            GPIO = new SRA320GPIO(strFrame.Split('/')[0], strFrame.Split('/')[1]);

            OnIOChange?.Invoke(this, new SRA32IOChengeEventArgs(GPIO));
        }
        //==============================================================================
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
        public void AligCompelet(SWafer wafer)
        {
            wafer.AlgnComplete = true;
            if (OnAligCompelet != null)
                OnAligCompelet(this, new WaferDataEventArgs(wafer));
        }
        public void AssignQueue(SWafer wafer)
        {
            quePreCommand.Enqueue(wafer);
        }
        //========================= One Thread ========================================   
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
        public void PAUS() { _threadPause.Set(); }
        public void MODE(int nNum) { _threadMode.Set(nNum); }
        public void WTDT() { _threadWtdt.Set(); }
        public void SSPD(int nNum) { _threadSspd.Set(nNum); }
        public void SSIZ(int nNum) { _threadSsiz.Set(nNum); }
        public void GSIZ() { _threadGsiz.Set(); }
        //==============================================================================
        void ExeManualFunction()
        {
            try
            {
                WriteLog("ExeManualFunction:Start");
                _signals[enumAlignerSignalTable.ProcessCompleted].Reset();
                if (DoManualProcessing != null) DoManualProcessing(this);
                OnManualCompleted?.Invoke(this, true);
            }
            catch (SException ex)
            {
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

                this.ResetChangeModeCompleted();
                this.EventW(3000);
                this.WaitChangeModeCompleted(3000);

                this.ResetChangeModeCompleted();
                this.InitW(3000);
                this.WaitChangeModeCompleted(3000);

                this.StimW(3000);

                this.ResetOrgnSinal();
                this.ResetInPos();
                this.OrgnW(3000, 2);
                this.WaitInPos(120000);
                this.WaitOrgnCompleted(3000);

                if (WaferExists())
                {
                    WriteLog("ExeORGN:Detect Panel");

                    ResetInPos();
                    AlgnDW(m_nAckTimeout, "0");
                    WaitInPos(m_nMotionTimeout);

                    //ResetInPos();
                    //UclmW(m_nAckTimeout);
                    //WaitInPos(m_nMotionTimeout);
                }

                SpinWait.SpinUntil(() => false, 500);

                /*this.ResetInPos();
                this.UclmW(3000);
                this.WaitInPos(10000);*/

                Cleanjobschedule();

                OnORGNComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
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
                this.HomeW(3000, n1, n2);
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
                this.ResetInPos();
                this.ClmpW(3000);
                this.WaitInPos(30000);
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
        void ExeUclm()
        {
            try
            {
                WriteLog("ExeUclm:Start");
                this.ResetInPos();
                this.UclmW(3000);
                this.WaitInPos(30000);
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
        void ExeAlgn(int nMode)
        {
            try
            {
                WriteLog("ExeAlgn:Start");
                this.ResetInPos();
                this.AlgnW(3000, nMode);
                this.WaitInPos(30000);
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
        void ExeAlgn(int nMode, int nPos)
        {
            try
            {
                this.ResetInPos();
                this.AlgnW(3000, nMode, nPos);
                this.WaitInPos(30000);
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
        void ExeAlgn1()
        {
            try
            {
                WriteLog("ExeAlgn1:Start");
                this.ResetInPos();
                this.Algn1W(3000);
                this.WaitInPos(30000);
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
        void ExeRot1Extd(int nPos)
        {
            try
            {
                WriteLog("ExeRot1Extd:Start");
                this.ResetInPos();
                this.RotationExtdW(3000, nPos);
                this.WaitInPos(30000);
                this.GposRW(3000);
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
        void ExeRot1Step(int nPos)
        {
            try
            {
                WriteLog("ExeRot1Step:Start");
                this.ResetInPos();
                this.RotStepW(3000, nPos);
                this.WaitInPos(30000);

                this.GposRW(3000);
                OnRot1StepComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
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
                this.ResetW(3000, nMode);
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
        void ExeInit()
        {
            try
            {
                WriteLog("ExeInit:Start");
                this.InitW(3000);
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
        void ExeStop()
        {
            try
            {
                WriteLog("ExeStop:Start");
                this.StopW(3000);
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
        void ExePause()
        {
            try
            {
                WriteLog("ExePause:Start");
                this.PausW(3000);
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
        void ExeMode(int nMode)
        {
            try
            {
                WriteLog("ExeMode:Start");
                this.ModeW(3000, nMode);
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
        void ExeWtdt()
        {
            try
            {
                WriteLog("ExeWtdt:Start");
                this.WtdtW(15000);
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
        void ExeSspd(int nMode)
        {
            try
            {
                WriteLog("ExeSspd:Start");
                this.SspdW(3000, nMode);
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
        void ExeGver()
        {
            try
            {
                WriteLog("ExeGver:Start");
                this.GverW(3000);
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
        void ExeSsiz(int nMode)
        {
            try
            {
                WriteLog("ExeSsiz:Start");
                this.SsizW(3000, nMode);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> ExeSsiz:" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeSsiz:" + ex);
            }
        }
        void ExeGsiz()
        {
            try
            {
                WriteLog("ExeGsiz:Start");
                this.GsizW(3000);
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
        //==============================================================================

        #region =========================== ORGN ===============================================
        private void Orgn(int nOrgn)
        {
            _signalAck[enumAlignerCommand.Orgn].Reset();
            m_Socket.SendCommand(string.Format("ORGN(" + nOrgn + ")"));
        }
        public void OrgnW(int nTimeout, int nOrgn = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].Reset();
                Orgn(nOrgn);
                if (!_signalAck[enumAlignerCommand.Orgn].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Orgn command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Orgn]));
                }
                if (_signalAck[enumAlignerCommand.Orgn].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Orgn command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Orgn]));
                }
            }
            else
            {
                m_bStatOrgnComplete = true;
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== HOME ===============================================
        protected abstract void Home(int nP1, int nP2);
        public void HomeW(int nTimeout, int nP1, int nP2)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].Reset();
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
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== CLMP ===============================================
        protected void Clmp(int nVariable, bool bCheckVac)
        {
            try
            {
                _signalAck[enumAlignerCommand.Clamp].Reset();
                if (bCheckVac)
                    m_Socket.SendCommand(string.Format("CLMP({0})", nVariable));
                else
                    m_Socket.SendCommand(string.Format("CLMP({0},1)", nVariable));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        protected abstract void ClmpAfterLiftPinDown(bool bCheckVac);
        public virtual void ClmpW(int nTimeout, bool bCheckVac = true)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].Reset();

                ClmpAfterLiftPinDown(bCheckVac);

                if (!_signalAck[enumAlignerCommand.Clamp].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send CLMP command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Clamp]));
                }
                if (_signalAck[enumAlignerCommand.Clamp].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send CLMP command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Clamp]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== UCLM ===============================================
        protected void Uclm(int nVariable)
        {
            try
            {
                _signalAck[enumAlignerCommand.UnClamp].Reset();
                m_Socket.SendCommand(string.Format("UCLM(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        protected abstract void UclmAfterLiftPinUp();
        protected abstract void UclmAfterLiftPinDown();
        public virtual void UclmW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].Reset();

                if (m_bUnClampLiftPinUp)
                    UclmAfterLiftPinUp();
                else
                    UclmAfterLiftPinDown();

                if (!_signalAck[enumAlignerCommand.UnClamp].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send UCLM command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.UnClamp]));
                }
                if (_signalAck[enumAlignerCommand.UnClamp].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send UCLM command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.UnClamp]));
                }
            }
            else
            {
                //SpinWait.SpinUntil(() => false, 1000);//看LOG需一秒
                SpinWait.SpinUntil(() => false, 500);
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ALGN ===============================================
        private void Algn(int nMode)
        {
            try
            {
                _signalAck[enumAlignerCommand.Alignment].Reset();
                m_Socket.SendCommand(string.Format("ALGN(" + nMode + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        protected virtual void Algn(int nMode, int nPos)
        {
            try
            {
                _signalAck[enumAlignerCommand.Alignment].Reset();
                m_Socket.SendCommand(string.Format("ALGN(" + nMode + "," + nPos + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void AlgnW(int nTimeout, int nMode = 0, int nPos = -1)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                if (nPos > -1)
                    Algn(nMode, nPos);
                else
                    Algn(nMode);

                if (!_signalAck[enumAlignerCommand.Alignment].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send ALGN command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Alignment]));
                }
                if (_signalAck[enumAlignerCommand.Alignment].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send ALGN command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Alignment]));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 1000);
            }
            _signalSubSequence.Set();
        }

        private void Algn1()
        {
            try
            {
                _signalAck[enumAlignerCommand.Alignment].Reset();
                m_Socket.SendCommand(string.Format("ALGN(1,0,1)"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public void Algn1W(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Algn1();

                if (!_signalAck[enumAlignerCommand.Alignment].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send ALGN command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Alignment]));
                }
                if (_signalAck[enumAlignerCommand.Alignment].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send ALGN command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Alignment]));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 500);
                Raxispos = 0;
            }
            _signalSubSequence.Set();
        }

        protected virtual void AlgnD(string strPos)//0~360
        {
            try
            {
                float n = float.Parse(strPos);
                //0~360
                while (n > 360) { n -= 360; }

                _signalAck[enumAlignerCommand.Alignment].Reset();
                m_Socket.SendCommand(string.Format("ALGN(1,D" + n + ",1)"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        //jan
        public void AlgnDW(int nTimeout, string strPos, int PhotographMethod = 1)//0~360
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                AlgnD(strPos);
                if (!_signalAck[enumAlignerCommand.Alignment].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send ALGN command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Alignment]));
                }
                if (_signalAck[enumAlignerCommand.Alignment].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send ALGN command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Alignment]));
                }
            }
            else
            {
                //SpinWait.SpinUntil(() => false, 6000);//看LOG需六秒
                SpinWait.SpinUntil(() => false, 1000);


                double value = Convert.ToDouble(strPos);
                Raxispos = (int)(value * 1000);
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== CALN ===============================================
        private void Caln()
        {
            _signalAck[enumAlignerCommand.CameraAlignment].Reset();
            m_Socket.SendCommand("CALN");
        }
        public void CalnW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Caln();
                if (!_signalAck[enumAlignerCommand.CameraAlignment].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send CALN command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.CameraAlignment]));
                }
                if (_signalAck[enumAlignerCommand.CameraAlignment].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send CALN command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.CameraAlignment]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== Rot1EXTD ===========================================
        protected virtual void RotationExtd(int nPos)
        {
            while (nPos >= 360000)
                nPos -= 360000;

            _signalAck[enumAlignerCommand.RotationExtd].Reset();
            m_Socket.SendCommand("ROT1.EXTD(" + nPos + ")");
        }
        public virtual void RotationExtdW(int nTimeout, int nPos)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                RotationExtd(nPos);
                if (!_signalAck[enumAlignerCommand.RotationExtd].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Rotation Extd command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.RotationExtd]));
                }
                if (_signalAck[enumAlignerCommand.RotationExtd].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Rotation Extd command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.RotationExtd]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== Rot1Step ===========================================
        protected virtual void RotStep(int nPos)
        {
            while (nPos >= 360000)
                nPos -= 360000;

            _signalAck[enumAlignerCommand.RotationStep].Reset();
            m_Socket.SendCommand("ROT1.STEP(" + nPos + ")");
        }
        protected virtual void RotStepW(int nTimeout, int nPos)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                RotStep(nPos);
                if (!_signalAck[enumAlignerCommand.RotationStep].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Rotation Step command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.RotationStep]));
                }
                if (_signalAck[enumAlignerCommand.RotationStep].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Rotation Step command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.RotationStep]));
                }
            }
            else
            {
                while (nPos >= 360000)
                    nPos -= 360000;
                Raxispos += nPos;
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== READ ===============================================
        private void Read()
        {
            _signalAck[enumAlignerCommand.ReadID].Reset();
            m_Socket.SendCommand("READ");
        }
        public void ReadW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Read();
                if (!_signalAck[enumAlignerCommand.ReadID].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send READ command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.ReadID]));
                }
                if (_signalAck[enumAlignerCommand.ReadID].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send READ command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.ReadID]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== EVNT ===============================================
        private void Event()
        {
            _signalAck[enumAlignerCommand.Event].Reset();
            m_Socket.SendCommand("EVNT(0,1)");
        }
        public void EventW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Event();
                if (!_signalAck[enumAlignerCommand.Event].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Event command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Event]));
                }
                if (_signalAck[enumAlignerCommand.Event].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Event command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Event]));
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
            _signalAck[enumAlignerCommand.Reset].Reset();
            m_Socket.SendCommand(string.Format("RSTA(" + nReset + ")"));
        }
        public void ResetW(int nTimeout, int nReset = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Reset(nReset);
                if (!_signalAck[enumAlignerCommand.Reset].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Reset command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Reset]));
                }
                if (_signalAck[enumAlignerCommand.Reset].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Reset command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Reset]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== INIT ===============================================
        private void Init()
        {
            _signalAck[enumAlignerCommand.Initialize].Reset();
            m_Socket.SendCommand(string.Format("INIT"));
        }
        public void InitW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Init();
                if (!_signalAck[enumAlignerCommand.Initialize].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Initialize command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Initialize]));
                }
                if (_signalAck[enumAlignerCommand.Initialize].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.InitialFailure);
                    throw new SException((int)enumAlignerError.InitialFailure, string.Format("Send Initialize command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Initialize]));
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
            _signalAck[enumAlignerCommand.Stop].Reset();
            m_Socket.SendCommand(string.Format("STOP"));
        }
        public void StopW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Stop();
                if (!_signalAck[enumAlignerCommand.Stop].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Stop command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Stop]));
                }
                if (_signalAck[enumAlignerCommand.Stop].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Stop command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Stop]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion \

        #region =========================== PAUS ===============================================
        private void Paus()
        {
            _signalAck[enumAlignerCommand.Pause].Reset();
            m_Socket.SendCommand(string.Format("PAUS"));
        }
        public void PausW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Paus();
                if (!_signalAck[enumAlignerCommand.Pause].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Pause command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Pause]));
                }
                if (_signalAck[enumAlignerCommand.Pause].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Pause command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Pause]));
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
            _signalAck[enumAlignerCommand.Mode].Reset();
            m_Socket.SendCommand(string.Format("MODE(" + nMode + ")"));
        }
        public void ModeW(int nTimeout, int nMode)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Mode(nMode);
                if (!_signalAck[enumAlignerCommand.Mode].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Mode command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Mode]));
                }
                if (_signalAck[enumAlignerCommand.Mode].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Mode command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Mode]));
                }
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
            _signalAck[enumAlignerCommand.Wtdt].Reset();
            m_Socket.SendCommand(string.Format("WTDT"));
        }
        public void WtdtW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Wtdt();
                if (!_signalAck[enumAlignerCommand.Wtdt].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send WTDT command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Wtdt]));
                }
                if (_signalAck[enumAlignerCommand.Wtdt].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send WTDT command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Wtdt]));
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
            _signalAck[enumAlignerCommand.GetData].Reset();
            m_Socket.SendCommand(string.Format("RTDT"));
        }
        public void RtdtW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Rtdt();
                if (!_signalAck[enumAlignerCommand.GetData].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send RTDT command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetData]));
                }
                if (_signalAck[enumAlignerCommand.GetData].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send RTDT command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetData]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SSPD ===============================================
        private void Sspd(int nVariable)
        {
            _signalAck[enumAlignerCommand.Speed].Reset();
            m_Socket.SendCommand(string.Format("SSPD(" + nVariable + ")"));
        }
        public void SspdW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumAlignerSignalTable.ProcessCompleted].Reset();
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
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STAT ===============================================
        private void Stat()
        {
            _signalAck[enumAlignerCommand.Status].Reset();
            m_Socket.SendCommand(string.Format("STAT"));
        }
        public void StatW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Stat();
                if (!_signalAck[enumAlignerCommand.Status].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Status command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Status]));
                }
                if (_signalAck[enumAlignerCommand.Status].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Status command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.Status]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPIO ===============================================
        private void Gpio()
        {
            _signalAck[enumAlignerCommand.GetIO].Reset();
            m_Socket.SendCommand(string.Format("GPIO"));
        }
        public void GpioW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gpio();
                if (!_signalAck[enumAlignerCommand.GetIO].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GPIO command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetIO]));
                }
                if (_signalAck[enumAlignerCommand.GetIO].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GPIO command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetIO]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GVER ===============================================
        private void Gver()
        {
            _signalAck[enumAlignerCommand.GetVersion].Reset();
            m_Socket.SendCommand(string.Format("GVER"));
        }
        public void GverW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gver();
                if (!_signalAck[enumAlignerCommand.GetVersion].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GVER command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetVersion]));
                }
                if (_signalAck[enumAlignerCommand.GetVersion].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GVER command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetVersion]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GLOG ===============================================
        private void Glog()
        {
            _signalAck[enumAlignerCommand.GetLog].Reset();
            m_Socket.SendCommand(string.Format("GLOG"));
        }
        public void GlogW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Glog();
                if (!_signalAck[enumAlignerCommand.GetLog].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GLOG command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetLog]));
                }
                if (_signalAck[enumAlignerCommand.GetLog].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GLOG command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetLog]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STIM ===============================================
        private void Stim()
        {
            _signalAck[enumAlignerCommand.SetDateTime].Reset();
            m_Socket.SendCommand("STIM(" + DateTime.Now.ToString("yyyy, MM, dd, HH, mm, ss") + ")");
        }
        public void StimW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Stim();
                if (!_signalAck[enumAlignerCommand.SetDateTime].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send STIM command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.SetDateTime]));
                }
                if (_signalAck[enumAlignerCommand.SetDateTime].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send STIM command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.SetDateTime]));
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
            _signalAck[enumAlignerCommand.GetDateTime].Reset();
            m_Socket.SendCommand("GTIM");
        }
        public void GtimW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gtim();
                if (!_signalAck[enumAlignerCommand.GetDateTime].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GTIM command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetDateTime]));
                }
                if (_signalAck[enumAlignerCommand.GetDateTime].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GTIM command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetDateTime]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPOS ===============================================
        private void Gpos()
        {
            _signalAck[enumAlignerCommand.GetPos].Reset();
            m_Socket.SendCommand(string.Format("GPOS"));
        }
        public void GposW(int nTimeout)
        {
            _signalSubSequence.Reset();
            Gpos();
            if (!_signalAck[enumAlignerCommand.GetPos].WaitOne(nTimeout))
            {
                SendAlmMsg(enumAlignerError.AckTimeout);
                throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetPos]));
            }
            if (_signalAck[enumAlignerCommand.GetPos].bAbnormalTerminal)
            {
                SendAlmMsg(enumAlignerError.SendCommandFailure);
                throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetPos]));
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== XAX1.GPOS ==========================================
        private void GposX()
        {
            _signalAck[enumAlignerCommand.GetXAxisPos].Reset();
            m_Socket.SendCommand(string.Format("XAX1.GPOS"));
        }
        public void GposXW(int nTimeout)
        {
            _signalSubSequence.Reset();
            GposX();
            if (!_signalAck[enumAlignerCommand.GetXAxisPos].WaitOne(nTimeout))
            {
                SendAlmMsg(enumAlignerError.AckTimeout);
                throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send XAxis GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetXAxisPos]));
            }
            if (_signalAck[enumAlignerCommand.GetXAxisPos].bAbnormalTerminal)
            {
                SendAlmMsg(enumAlignerError.SendCommandFailure);
                throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send XAxis GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetXAxisPos]));
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== YAX1.GPOS ==========================================
        public virtual void GposY()
        {

        }

        public void GposYW(int nTimeout)
        {
            _signalSubSequence.Reset();
            GposY();
            if (!_signalAck[enumAlignerCommand.GetYAxisPos].WaitOne(nTimeout))
            {
                SendAlmMsg(enumAlignerError.AckTimeout);
                throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send YAxis GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetYAxisPos]));
            }
            if (_signalAck[enumAlignerCommand.GetYAxisPos].bAbnormalTerminal)
            {
                SendAlmMsg(enumAlignerError.SendCommandFailure);
                throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send YAxis GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetYAxisPos]));
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ZAX1.GPOS ==========================================
        private void GposZ()
        {
            _signalAck[enumAlignerCommand.GetZAxisPos].Reset();
            m_Socket.SendCommand(string.Format("ZAX1.GPOS"));
        }
        public void GposZW(int nTimeout)
        {
            _signalSubSequence.Reset();
            GposZ();
            if (!_signalAck[enumAlignerCommand.GetZAxisPos].WaitOne(nTimeout))
            {
                SendAlmMsg(enumAlignerError.AckTimeout);
                throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send ZAxis GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetZAxisPos]));
            }
            if (_signalAck[enumAlignerCommand.GetZAxisPos].bAbnormalTerminal)
            {
                SendAlmMsg(enumAlignerError.SendCommandFailure);
                throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send ZAxis GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetZAxisPos]));
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== ROT1.GPOS ==========================================
        private void GposR()
        {
            _signalAck[enumAlignerCommand.GetRAxisPos].Reset();
            m_Socket.SendCommand(string.Format("ROT1.GPOS"));
        }
        public void GposRW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                GposR();
                if (!_signalAck[enumAlignerCommand.GetRAxisPos].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Rot1 GPOS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetRAxisPos]));
                }
                if (_signalAck[enumAlignerCommand.GetRAxisPos].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Rot1 GPOS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetRAxisPos]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GWID ===============================================
        private void Gwid()
        {
            _signalAck[enumAlignerCommand.GetType].Reset();
            m_Socket.SendCommand(string.Format("GWID"));
        }
        public void GwidW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gwid();
                if (!_signalAck[enumAlignerCommand.GetType].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GWID command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetType]));
                }
                if (_signalAck[enumAlignerCommand.GetType].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GWID command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetType]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== TDST ===============================================
        private void Tdst()
        {
            _signalAck[enumAlignerCommand.NotchStopPos].Reset();
            m_Socket.SendCommand(string.Format("TDST"));
        }
        public void TdstW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Tdst();
                if (!_signalAck[enumAlignerCommand.NotchStopPos].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send TDST command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.NotchStopPos]));
                }
                if (_signalAck[enumAlignerCommand.NotchStopPos].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send TDST command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.NotchStopPos]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GTAD ===============================================
        private void Gtad()
        {
            _signalAck[enumAlignerCommand.GetSensorValue].Reset();
            m_Socket.SendCommand(string.Format("GTAD"));
        }
        public void GtadW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gtad();
                if (!_signalAck[enumAlignerCommand.GetSensorValue].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GTAD command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetSensorValue]));
                }
                if (_signalAck[enumAlignerCommand.GetSensorValue].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GTAD command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetSensorValue]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPRS ===============================================
        protected virtual void Gprs()
        {

        }
        public void GprsW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gprs();
                if (!_signalAck[enumAlignerCommand.GetVacuumValue].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GPRS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetVacuumValue]));
                }
                if (_signalAck[enumAlignerCommand.GetVacuumValue].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GPRS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetVacuumValue]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GSIZ ===============================================
        private void Gsiz()
        {
            _signalAck[enumAlignerCommand.GetSize].Reset();
            m_Socket.SendCommand(string.Format("GSIZ"));
        }
        public void GsizW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gsiz();
                if (!_signalAck[enumAlignerCommand.GetSize].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GSIZ command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetSize]));
                }
                if (_signalAck[enumAlignerCommand.GetSize].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GSIZ command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetSize]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SSIZ ===============================================
        private void Ssiz(int nSsiz)
        {
            _signalAck[enumAlignerCommand.SetSize].Reset();
            m_Socket.SendCommand(string.Format("SSIZ(" + nSsiz + ")"));
        }
        public void SsizW(int nTimeout, int nSsiz = 0)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Ssiz(nSsiz);
                if (!_signalAck[enumAlignerCommand.SetSize].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send SSIZ command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.SetSize]));
                }
                if (_signalAck[enumAlignerCommand.SetSize].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send SSIZ command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.SetSize]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GTID ===============================================
        protected virtual void Gtid()
        {

        }
        public void GtidW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gtid();
                if (!_signalAck[enumAlignerCommand.GetID].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GTID command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetID]));
                }
                if (_signalAck[enumAlignerCommand.GetID].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GTID command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetID]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion      

        #region =========================== GTMP ===============================================
        protected virtual void Gtmp()
        {

        }

        public void GtmpW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                Gtmp();
                if (!_signalAck[enumAlignerCommand.GetMP].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send GTMP command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.GetMP]));
                }
                if (_signalAck[enumAlignerCommand.GetMP].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send GTMP command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.GetMP]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion

        //==============================================================================     

        public void ResetChangeModeCompleted()
        {
            _signals[enumAlignerSignalTable.Remote].Reset();
        }
        public void WaitChangeModeCompleted(int nTimeout)
        {
            if (!m_bSimulate)
            {
                if (!_signals[enumAlignerSignalTable.Remote].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.InitialFailure);
                    throw new SException((int)enumAlignerError.InitialFailure, string.Format("Wait Mode was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumAlignerSignalTable.Remote].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.InitialFailure);
                    throw new SException((int)enumAlignerError.InitialFailure, string.Format("Motion is Mode end."));
                }
            }
        }

        public void ResetOrgnSinal()
        {
            _signals[enumAlignerSignalTable.OPRCompleted].Reset();
            m_bStatOrgnComplete = false;
        }
        public void WaitOrgnCompleted(int TimeOut)
        {
            if (!m_bSimulate)
            {
                if (!_signals[enumAlignerSignalTable.OPRCompleted].WaitOne(TimeOut))
                {
                    SendAlmMsg(enumAlignerError.OriginPosReturnTimeout);
                    throw new SException((int)(enumAlignerError.OriginPosReturnTimeout), "Aligner Orgn Fail");
                }
                if (_signals[enumAlignerSignalTable.OPRCompleted].bAbnormalTerminal)
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
            _signals[enumAlignerSignalTable.MotionCompleted].Reset();
            m_eStatInPos = enumAlignerStatus.Moving;
        }
        public void WaitInPos(int nTimeout)
        {
            SpinWait.SpinUntil(() => false, 200);
            if (!m_bSimulate)
            {
                if (!_signals[enumAlignerSignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.MotionTimeout);
                    throw new SException((int)enumAlignerError.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.MotionAbnormal);
                    throw new SException((int)enumAlignerError.MotionAbnormal, string.Format("Motion is abnormal end."));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
                m_eStatInPos = enumAlignerStatus.InPos;
            }
        }
        public bool IsZaxsInBottom()
        {
            if (m_bSimulate) return true;
            GposW(1000);
            return m_bZaxsInBottom;
        }
        //==============================================================================
        #region =========================== CommandTable =======================================
        protected Dictionary<enumAlignerCommand, string> _dicCmdsTable = new Dictionary<enumAlignerCommand, string>()
        {
            {enumAlignerCommand.Orgn,"ORGN"},
            {enumAlignerCommand.Home,"HOME"},
            {enumAlignerCommand.Clamp,"CLMP"},
            {enumAlignerCommand.UnClamp,"UCLM"},
            {enumAlignerCommand.Alignment,"ALGN"},
            {enumAlignerCommand.CameraAlignment,"CALN"},

            {enumAlignerCommand.RotationExtd,"ROT1.EXTD"},

            {enumAlignerCommand.ReadID,"READ"},
            {enumAlignerCommand.Event,"EVNT"},
            {enumAlignerCommand.Reset,"RSTA"},
            {enumAlignerCommand.Initialize,"INIT"},
            {enumAlignerCommand.Stop,"STOP"},
            {enumAlignerCommand.Pause,"PAUS"},
            {enumAlignerCommand.RotationStep,"ROT1.STEP"},
            {enumAlignerCommand.Mode,"MODE"},
            {enumAlignerCommand.Wtdt,"WTDT"},
            {enumAlignerCommand.GetData,"RTDT"},
            {enumAlignerCommand.Speed,"SSPD"},
            {enumAlignerCommand.Status,"STAT"},
            {enumAlignerCommand.GetIO,"GPIO"},
            {enumAlignerCommand.GetVersion,"GVER"},
            {enumAlignerCommand.GetLog,"GLOG"},
            {enumAlignerCommand.SetDateTime,"STIM"},
            {enumAlignerCommand.GetDateTime,"GTIM"},
            {enumAlignerCommand.GetPos,"GPOS"},
            {enumAlignerCommand.GetXAxisPos,"XAX1.GPOS"},
            {enumAlignerCommand.GetYAxisPos,"YAX1.GPOS"},
            {enumAlignerCommand.GetZAxisPos,"ZAX1.GPOS"},
            {enumAlignerCommand.GetRAxisPos,"ROT1.GPOS"},
            {enumAlignerCommand.GetType,"GWID" },
            {enumAlignerCommand.NotchStopPos,"TDST" },
            {enumAlignerCommand.GetSensorValue,"GTAD" },
            {enumAlignerCommand.GetVacuumValue,"GPRS" },
            {enumAlignerCommand.GetSize,"GSIZ" },
            {enumAlignerCommand.SetSize,"SSIZ" },
            {enumAlignerCommand.GetID,"GTID" },
            {enumAlignerCommand.GetMP,"GTMP" },
            {enumAlignerCommand.GetDPRM,"DPRM.GTDT" },
            {enumAlignerCommand.GetDCST,"DCST.GTDT" },
            {enumAlignerCommand.SetDCST,"DCST.STDT" },
            {enumAlignerCommand.ClientConnected,"CNCT"},

            {enumAlignerCommand.SetOutputBit,"SPOT"},

            {enumAlignerCommand.Exct,"EXCT"},

            {enumAlignerCommand.RAxisAbsolute,"ROT1.MABS"},
            {enumAlignerCommand.RAxisRelative,"ROT1.MREL"},
        };
        #endregion 
        #region =========================== Signals ============================================
        protected Dictionary<enumAlignerCommand, SSignal> _signalAck = new Dictionary<enumAlignerCommand, SSignal>();
        protected Dictionary<enumAlignerSignalTable, SSignal> _signals = new Dictionary<enumAlignerSignalTable, SSignal>();
        protected SSignal _signalSubSequence;
        #endregion 
        #region =========================== OnOccurError =======================================
        //  發生STAT異常
        protected void SendAlmMsg(string strCode)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            //  ALN1 13 11 00000
            //  ALN2 14 11 00000          
            int nCode = Convert.ToInt32(strCode, 16) /*+ (13 + BodyNo - 1) * 10000000 + 11 * 100000*/;
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
        protected void SendAlmMsg(enumAlignerError eAlarm)
        {
            WriteLog(string.Format("Occur eAlarm Error : {0}", eAlarm));
            //  ALN1 15 10 00000
            //  ALN2 16 10 00000          
            int nCode = (int)eAlarm /*+ (13 + BodyNo - 1) * 10000000 + 10 * 100000*/;
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
        #region =========================== CreateMessage ======================================
        public Dictionary<int, string> m_dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();
        protected virtual void CreateMessage()
        {
            m_dicCancel[0x0001] = "0001:Command not designated";
            m_dicCancel[0x0002] = "0002:The designated target motion not equipped";
            m_dicCancel[0x0003] = "0003:Too many/few parameters";
            m_dicCancel[0x0004] = "0004:Command not equipped";
            m_dicCancel[0x0005] = "0005:Too many/few parameters";
            m_dicCancel[0x0006] = "0006:Abnormal range of the parameter";
            m_dicCancel[0x0007] = "0007:Abnormal mode";
            m_dicCancel[0x0008] = "0008:Abnormal data";
            m_dicCancel[0x0009] = "0009:System in preparation";
            m_dicCancel[0x000A] = "000A:Origin search not completed";
            m_dicCancel[0x000B] = "000B:Moving/Processing";
            m_dicCancel[0x000C] = "000C:No motion";
            m_dicCancel[0x000D] = "000D:Abnormal flash memory";
            m_dicCancel[0x000E] = "000E:Insufficient memory";
            m_dicCancel[0x000F] = "000F:Error-occurred state";
            m_dicCancel[0x0010] = "0010:Origin search is completed but interlock on";
            m_dicCancel[0x0011] = "0011:The emergency stop signal is turned on";
            m_dicCancel[0x0012] = "0012:The temporarily stop signal is turned on";
            m_dicCancel[0x0013] = "0013:Abnormal interlock signal";
            m_dicCancel[0x0014] = "0014:Drive power is turned off";
            m_dicCancel[0x0015] = "0015:Not excited";
            m_dicCancel[0x0016] = "0016:Abnormal current position";
            m_dicCancel[0x0017] = "0017:Abnormal target position";
            m_dicCancel[0x0018] = "0018:Command processing";
            m_dicCancel[0x0019] = "0019:Invalid work state";

            m_dicController[0x00] = "[00:Whole of the Aligner] ";
            m_dicController[0x01] = "[01:X-axis] ";
            m_dicController[0x02] = "[02:Y-axis] ";
            m_dicController[0x03] = "[03:Z-axis] ";
            m_dicController[0x04] = "[04:Spindle axis] ";
            m_dicController[0x05] = "[05:Alignment camera] ";
            m_dicController[0xFF] = "[FF:Sysem] ";

            m_dicError[0x01] = "01:Motor stall";
            m_dicError[0x02] = "02:Limit";
            m_dicError[0x03] = "03:Position error";
            m_dicError[0x04] = "04:Command error";
            m_dicError[0x05] = "05:Communication error";
            m_dicError[0x06] = "06:Abnormal chucking sensor";
            m_dicError[0x07] = "07:Driver EMS error";
            m_dicError[0x08] = "08:Work dropped error";
            m_dicError[0x0E] = "0E:Abnormal driver";
            m_dicError[0x0F] = "0F:Abnormal drive power";
            m_dicError[0x10] = "10:Abnormal control power";
            m_dicError[0x13] = "13:Abnormal temperature of driver";
            m_dicError[0x14] = "14:Driver FPGA error";
            m_dicError[0x15] = "15:Motor wire broken";
            m_dicError[0x16] = "16:Motor over load";
            m_dicError[0x17] = "17:Motor motion error";
            m_dicError[0x18] = "18:Abnormal Alignment sensor";
            m_dicError[0x19] = "19:Abnormal exhaust fan state";
            m_dicError[0x40] = "40:Internal error(abnormal device driver)";
            m_dicError[0x41] = "41:Internal error(abnormal driver control)";
            m_dicError[0x42] = "42:Internal error(task start failed)";
            m_dicError[0x45] = "45:Reading setting data failed";
            m_dicError[0x7F] = "7F:Intarnal memory error";
            m_dicError[0x83] = "83:Origin search failed";
            m_dicError[0x84] = "84:Chucking error";
            m_dicError[0x90] = "90:Notch detection error";
            m_dicError[0x91] = "91:Alignment sensor detects obstacle";
            m_dicError[0x92] = "92:Retry over(alignment failed)";
            m_dicError[0x93] = "93:ID reading failed";
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

        protected void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[ALN{0}] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        public virtual bool IsAirOK()
        {
            return false;
        }

        public virtual bool WaferExists()
        {
            bool b = (/*(GPIO.DI_LowerSpindlePresence && GPIO.DI_UpperSpindlePresence) ||*/ GPIO.DI_LowerSpindlePresence); // HSC need mod ?
            return b;
        }

        public virtual bool IsClamp()
        {
            //bool b = ((GPIO.DI_LowerSpindlePresence == true && GPIO.DI_UpperSpindlePresence == true));
            bool b = GPIO.DI_VacPressureOut_2;
            return b;
        }

        public virtual bool IsFanOK()
        {
            return false;
        }

        public virtual bool IsUnClamp()
        {
            // bool b = ((GPIO.DI_LowerSpindlePresence == false && GPIO.DI_UpperSpindlePresence == false));
            bool b = GPIO.DI_VacPressureOut_2;
            return b;
        }

        public void Cleanjobschedule()//20240704
        {
            queCommand = new System.Collections.Concurrent.ConcurrentQueue<SWafer>();
            quePreCommand = new System.Collections.Concurrent.ConcurrentQueue<SWafer>();
        }



        //==============================================================================     
        public void BarcodeOpen() { if (m_Barcode != null) m_Barcode.Open(); }//20240828
        public bool IsBarcodeConnected { get { return m_Barcode != null && m_Barcode.Connected; } }//20240828
        public bool IsBarcodeEnable { get { return m_Barcode != null; } }//20240828
        public string BarcodeRead() { return m_Barcode == null ? "No Barcode" : m_Barcode.Read(); }//20240828


        public virtual bool IsReadyToLoad() { return Wafer == null; }

        public void TriggerSException(enumAlignerError eError)
        {
            SendAlmMsg(eError);
            throw new SException((int)(eError), "SException:" + eError);
        }

    }
}
