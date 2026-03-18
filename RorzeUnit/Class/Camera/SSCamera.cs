using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.Camera.Enum;
using RorzeUnit.Class.Camera.Evnt;
using RorzeUnit.Event;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeUnit.Class.Camera
{
    public class SSCamera
    {
        #region =========================== private ============================================
        private string m_strName;
        private bool m_bSimulate;
        private enumStat1_Mode m_eStatMode;         //記憶的STAT S1第1 bit  
        private bool m_bStatOrgnComplete;           //記憶的STAT S1第2 bit
        private bool m_bStatProcessed;              //記憶的STAT S1第3 bit
        private enumStat4_Move m_eStatInPos;        //記憶的STAT S1第4 bit   
        private int m_nSpeed;                       //記憶的STAT S1第5 bit     
        private string m_strErrCode = "0000";       //記憶的STAT S2

        private int m_nAckTimeout = 3000;
        private int m_nMotionTimeout = 60000;
        private SLogger m_Logger = SLogger.GetLogger("CommunicationLog");

        private sRorzeSocket m_Socket;

        private string m_GtlcResult = "";
        private string m_CllcResult = "";
        private bool m_bExisResult;
        private bool m_bConnect;
        #endregion
        #region =========================== Signals ============================================
        protected Dictionary<enumCommand, SSignal> m_SignalAck = new Dictionary<enumCommand, SSignal>();
        protected Dictionary<enumSignalTable, SSignal> m_Signals = new Dictionary<enumSignalTable, SSignal>();
        protected SSignal m_SignalSubSequence;
        #endregion
        #region =========================== Thread =============================================
        private SPollingThread m_PollingDequeueRecv;

        private SInterruptOneThread m_threadInit;
        private SInterruptOneThreadINT_INT m_threadCllc;
        private SInterruptOneThreadINT m_threadGtlc;
        private SInterruptOneThread m_threadExis;
        private SInterruptOneThread m_threadRsta;
        private SInterruptOneThread m_threadRset;

        #endregion
        #region =========================== Event ==============================================
        public event EventHandler<string[]> OnReadData;

        public event OccurErrorEventHandler OnOccurStatErr;
        public event OccurErrorEventHandler OnOccurCancel;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public event OccurErrorEventHandler OnOccurErrorRest;
        public event OccurErrorEventHandler OnOccurWarning;
        public event OccurErrorEventHandler OnOccurWarningRest;
        #endregion
        #region =========================== CreateMessage ======================================

        protected Dictionary<enumCommand, string> m_dicCmdsTable = new Dictionary<enumCommand, string>()
        {
            {enumCommand.CNCT,"CNCT"},
            {enumCommand.INIT,"INIT"},
            {enumCommand.STAT,"STAT"},
            {enumCommand.EVNT,"EVNT"},
            {enumCommand.CLLC,"CLLC"},
            {enumCommand.RSTA,"RSTA"},
            {enumCommand.STIM,"STIM"},
            {enumCommand.GTIM,"GTIM"},
            {enumCommand.RSET,"RSET"},
            {enumCommand.STLV,"STLV"},
            {enumCommand.GTLV,"GTLV"},
            {enumCommand.SVLV,"SVLV"},
            {enumCommand.STSN,"STSN"},
            {enumCommand.GTSN,"GTSN"},
            {enumCommand.STET,"STET"},
            {enumCommand.GTET,"GTET"},
            {enumCommand.SVET,"SVET"},
            {enumCommand.STGN,"STGN"},
            {enumCommand.GTGN,"GTGN"},
            {enumCommand.SVGN,"SVGN"},
            {enumCommand.SARE,"SARE"},
            {enumCommand.RRCP,"RRCP"},
            {enumCommand.GTLC,"GTLC"},
            {enumCommand.EXIS,"EXIS"},
        };
        public Dictionary<int, string> _dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> _dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> _dicError { get; } = new Dictionary<int, string>();
        private void CreateMessage()
        {
            _dicCancel[0x0001] = "0001:Command is not defined";
            _dicCancel[0x0002] = "0002:Too few parameters";
            _dicCancel[0x0003] = "0003:Too many parameters";
            _dicCancel[0x0004] = "0004:Not initialized yet, or Moving/Processing";
            _dicCancel[0x0005] = "0005:Invalid parameters";
            _dicCancel[0x0006] = "0006:Invalid command";
            _dicCancel[0x0007] = "0007:Unit ID is wrong";
            _dicCancel[0x0008] = "0008:ROI is not set";
            _dicCancel[0x0009] = "0009:Image is empty";
            _dicCancel[0x000A] = "000A:DWOK operation failed, no value can be stored";
            _dicCancel[0x000B] = "000B:ROI is too large";
            _dicCancel[0x000C] = "000C:Teaching Tool is not connected";
            for (int i = 0; i < 0x10; i++)
            {
                _dicCancel[0x0010 + i] = string.Format("{0:X4}:The parameter No.{1} is too small", 0x0010 + i, i + 1);
                _dicCancel[0x0020 + i] = string.Format("{0:X4}:The Parameter No.{1} is too large", 0x0020 + i, i + 1);
                _dicCancel[0x0030 + i] = string.Format("{0:X4}:The Parameter No.{1} is not invalid", 0x0030 + i, i + 1);
            }

            _dicController[0x00] = "[00:COR entire CMP module] ";
            _dicController[0x01] = "[01:LIG light source] ";
            _dicController[0x02] = "[02:CAM camera] ";
            _dicController[0x03] = "[03:ALG algorithm] ";

            _dicError[0x01] = "01:Module socket error/Socket network initialization error";
            _dicError[0x02] = "02:Timeout and no response";
            _dicError[0x03] = "03:Initialize error";
            _dicError[0x04] = "04:Image incomplete";
            _dicError[0x05] = "05:Different foup/Teaching foup size is inconsistent with the original opening";
            _dicError[0x06] = "06:Parameter setting error";
            _dicError[0x07] = "07:Algorithm calculation fail";
            _dicError[0x08] = "08:Parameter getting error";
            _dicError[0x09] = "09:Grab error Image";
            _dicError[0x0A] = "0A:Save recipe data error";
            _dicError[0x0B] = "0B:Not found wafer and no wafer angle data";
            _dicError[0xFF] = "FF:Fatal error/Undefined";
        }
        #endregion
        #region =========================== property ===========================================
        public virtual bool _Connected { get { return m_bConnect; } }
        public int _BodyNo { get; private set; }
        public bool _Disable { get; private set; }

        //STAT S1第1 
        public enumStat1_Mode _StatMode { get { return m_eStatMode; } }
        public bool IsInitialized { get { return m_eStatMode == enumStat1_Mode.Remote; } }
        //STAT S1第2 
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }
        //STAT S1第3 
        public bool IsProcessing { get { return m_bStatProcessed; } }
        //STAT S1第4 
        public enumStat4_Move _InPos { get { return m_eStatInPos; } }
        public bool IsMoving { get { return m_eStatInPos == enumStat4_Move.Moving; } }
        //STAT S1第5 bit
        public int _Speed { get { return m_nSpeed; } }
        //STAT S2
        public bool IsError { get { return (m_strErrCode != "0000"); } }


        public string GetGtlcResult { get { return m_GtlcResult; } }
        public string GetCllcResult { get { return m_CllcResult; } }

        public bool GetExisResult { get { return m_bExisResult; } }
        #endregion
        //==============================================================================

        public SSCamera(string strIP, int nPort, int nBodyNo, bool bDisable, bool bSimulate, sServer Sever = null)
        {

            _BodyNo = nBodyNo;
            _Disable = bDisable;
            m_bSimulate = bSimulate;
            m_strName = string.Format("CMP{0}", _BodyNo);
            foreach (enumCommand item in System.Enum.GetValues(typeof(enumCommand)))
                m_SignalAck.Add(item, new SSignal(false, EventResetMode.ManualReset));
            foreach (enumSignalTable item in System.Enum.GetValues(typeof(enumSignalTable)))
                m_Signals.Add(item, new SSignal(false, EventResetMode.ManualReset));
            m_SignalSubSequence = new SSignal(false, EventResetMode.ManualReset);

            m_Socket = new sRorzeSocket(strIP, nPort, nBodyNo, "CMP", bSimulate, Sever);
            m_Socket.OnConnectChange += Socket_OnConnectChange;

            m_threadInit = new SInterruptOneThread(ExeInit);
            m_threadCllc = new SInterruptOneThreadINT_INT(ExeCllc);
            m_threadGtlc = new SInterruptOneThreadINT(ExeGtlc);
            m_threadExis = new SInterruptOneThread(ExeExis);
            m_threadRsta = new SInterruptOneThread(ExeRsta);
            m_threadRset = new SInterruptOneThread(ExeRset);

            CreateMessage();

            m_PollingDequeueRecv = new SPollingThread(1);
            m_PollingDequeueRecv.DoPolling += PollingDequeueRecv;
            m_PollingDequeueRecv.Set();


        }

        private void Socket_OnConnectChange(object sender, bool bConnect)
        {
            if (bConnect == false)
            {
                SendAlmMsg(enumCustomError.Disconnect);
            }
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[{0}] : {1}  at line {2} ({3})", m_strName, strContent, lineNumber, meberName);
            m_Logger.WriteLog(strMsg);
        }

        //==============================================================================

        public void Open()
        {
            if (m_bSimulate)
                m_bConnect = true;
            else
                m_Socket.Open();
        }

        private void PollingDequeueRecv()
        {
            try
            {
                string[] astrFrame;

                if (!m_Socket.QueRecvBuffer.TryDequeue(out astrFrame)) return;
                string strFrame;

                if (OnReadData != null) OnReadData(this, astrFrame);

                for (int nCnt = 0; nCnt < astrFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                {
                    if (astrFrame[nCnt].Length == 0)
                        continue;

                    strFrame = astrFrame[nCnt];

                    enumCommand eCmd = enumCommand.None;

                    foreach (string scmd in m_dicCmdsTable.Values) //查字典
                    {
                        if (strFrame.Contains(string.Format("{0}.{1}", m_strName, scmd)))
                        {
                            eCmd = m_dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;

                            break;
                        }
                    }

                    if (eCmd == enumCommand.None) //不認識的封包
                    {
                        WriteLog(string.Format("<<<ByPassReceive>>> Got unknown frame and pass to process. [{0}]", strFrame));
                        continue;
                    }

                    WriteLog(strFrame);

                    switch (strFrame[0]) //命令種類
                    {
                        case 'c': //cancel
                            AnalysisCancel(new ProtoclEventArgs(strFrame));
                            break;
                        case 'n': //nak
                            m_SignalAck[eCmd].bAbnormalTerminal = true;
                            m_SignalAck[eCmd].Set();
                            break;
                        case 'a': //ack
                            OnAck(this, new ProtoclEventArgs(strFrame));
                            m_SignalAck[eCmd].Set();
                            break;
                        case 'e':
                            OnAck(this, new ProtoclEventArgs(strFrame));
                            break;
                        default:
                            break;
                    }
                }
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

        private void AnalysisCancel(ProtoclEventArgs e)
        {
            enumCommand cmd = m_dicCmdsTable.FirstOrDefault(x => x.Value == e._Frame._Command).Key;

            if (Convert.ToInt32(e._Frame._Value, 16) > 0)
            {
                m_Signals[enumSignalTable.MotionCompleted].bAbnormalTerminal = true;
                m_Signals[enumSignalTable.MotionCompleted].Set(); //有moving過才可以Set
                SendCancelMsg(e._Frame._Value);
            }
        }

        private void OnAck(object sender, ProtoclEventArgs e)
        {
            enumCommand cmd = m_dicCmdsTable.FirstOrDefault(x => x.Value == e._Frame._Command).Key;

            switch (cmd)
            {
                case enumCommand.STAT:
                    AnalysisStatus(e._Frame._Value);
                    break;
                case enumCommand.CNCT:
                    m_bConnect = true;
                    break;
                //jan
                case enumCommand.CLLC:
                    AnalysisCLLC(e._Frame._Value);
                    break;
                case enumCommand.GTLC:
                    AnalysisGTLC(e._Frame._Value);
                    break;
                case enumCommand.EXIS:
                    AnalysisGTLC(e._Frame._Value);
                    break;
                default:
                    break;
            }
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
                    m_eStatMode = enumStat1_Mode.Initializing;
                    //_signals[enumSignalTable.Remote].Reset();
                    break;
                case '1':
                    m_eStatMode = enumStat1_Mode.Remote;
                    m_Signals[enumSignalTable.Remote].Set();
                    break;
                case '2':
                    m_eStatMode = enumStat1_Mode.Maintenance;
                    m_Signals[enumSignalTable.Remote].Set();
                    break;
                case '3':
                    m_eStatMode = enumStat1_Mode.Recovery;
                    break;
                default: break;
            }

            //S1.bit#2 origin return complete
            if (s1[1] == '0')
                m_Signals[enumSignalTable.OriginCompleted].Reset();
            else
                m_Signals[enumSignalTable.OriginCompleted].Set();
            m_bStatOrgnComplete = s1[1] == '1';

            //S1.bit#3 processing command
            if (s1[2] == '0')
                m_Signals[enumSignalTable.ProcessCompleted].Set();
            else
                m_Signals[enumSignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = s1[2] == '1';

            //S1.bit#4 operation status
            switch (s1[3])
            {
                case '0': m_eStatInPos = enumStat4_Move.InPos; break;
                case '1': m_eStatInPos = enumStat4_Move.Moving; break;
                case '2': m_eStatInPos = enumStat4_Move.Pause; break;
            }

            //S1.bit#5 operation speed   
            if (s1[4] >= '0' && s1[4] <= '9') m_nSpeed = s1[4] - '0';
            else if (s1[4] >= 'A' && s1[4] <= 'K') m_nSpeed = s1[4] - 'A' + 10;

            m_nMotionTimeout = (m_nSpeed == 0) ? 60000 : 60000 * 3;

            //S2
            if (Convert.ToInt32(s2, 16) > 0)
            {
                m_Signals[enumSignalTable.MotionCompleted].bAbnormalTerminal = true;
                m_Signals[enumSignalTable.MotionCompleted].Set();
                SendAlmMsg(s2);
                m_strErrCode = s2;
            }
            else
            {
                if (m_eStatInPos == enumStat4_Move.InPos)//運動到位      
                    m_Signals[enumSignalTable.MotionCompleted].Set();
                else
                    m_Signals[enumSignalTable.MotionCompleted].Reset();

                if (m_strErrCode != "0000")
                {
                    RestAlmMsg(m_strErrCode);
                    m_strErrCode = "0000";
                }
            }
        }

        private void AnalysisGTLC(string strFrame)
        {
            if (!strFrame.Contains(','))
            {
                WriteLog("the format of GTLC frame has error, ',' not found! [" + strFrame + "]");
                return;
            }
            m_GtlcResult = strFrame;
        }

        private void AnalysisCLLC(string strFrame)
        {
            if (!strFrame.Contains(','))
            {
                WriteLog("the format of CLLC frame has error, ',' not found! [" + strFrame + "]");
                return;
            }
            m_CllcResult = strFrame;
        }

        private void AnalysisEXIS(string strFrame)
        {
            if (strFrame != "1" || strFrame != "0")
            {
                WriteLog("the format of EXIS frame has error, ',' not found! [" + strFrame + "]");
                return;
            }
            m_bExisResult = strFrame == "1";
        }

        #region  =========================== One Thread =========================================     
        public void INIT() { m_threadInit.Set(); }
        private void ExeInit()
        {
            try
            {
                WriteLog("ExeInit:Start");

                this.ResetChangeModeCompleted();
                this.EvntW(m_nAckTimeout);
                this.WaitChangeModeCompleted(m_nAckTimeout);

                this.StimW(m_nAckTimeout);

                this.ResetInPos();
                this.InitW(m_nAckTimeout);
                this.WaitInPos(m_nAckTimeout);

            }
            catch (SException ex) { WriteLog("<<SException>> :" + ex); }
            catch (Exception ex) { WriteLog("<<Exception>> :" + ex); }
        }

        public void CLLC(int CllcNo, int StageNo) { m_threadCllc.Set(CllcNo, StageNo); }
        private void ExeCllc(int CllcNo, int StageNo)
        {
            try
            {
                WriteLog("ExeCllc:Start");

                this.ResetInPos();
                this.CllcW(m_nAckTimeout, CllcNo, StageNo);
                this.WaitInPos(m_nAckTimeout);

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

        public void GTLC() { m_threadGtlc.Set(); }
        private void ExeGtlc(int Target)
        {
            try
            {
                WriteLog("ExeGtlc:Start");

                this.ResetInPos();
                this.GtlcW(m_nAckTimeout, Target);
                this.WaitInPos(m_nAckTimeout);

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
        public void EXIS() { m_threadExis.Set(); }
        private void ExeExis()
        {
            try
            {
                WriteLog("ExeExis:Start");

                this.ResetInPos();
                this.ExisW(m_nAckTimeout);
                this.WaitInPos(m_nAckTimeout);

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
        public void RSTA() { m_threadRsta.Set(); }
        private void ExeRsta()
        {
            try
            {
                WriteLog("ExeRsta:Start");

                this.ResetInPos();
                this.RstaW(m_nAckTimeout);
                this.WaitInPos(m_nAckTimeout);

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
        public void RSET() { m_threadRset.Set(); }
        private void ExeRset()
        {
            try
            {
                WriteLog("ExeRset:Start");

                this.ResetInPos();
                this.RsetW(m_nAckTimeout);
                this.WaitInPos(m_nAckTimeout);

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
        #endregion

        #region =========================== OnOccurError =======================================
        //  發生STAT異常
        private void SendAlmMsg(string strCode, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode), meberName, lineNumber);
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));
            SendAlmMsg(enumCustomError.Status_Error);
        }
        //  解除STAT異常
        private void RestAlmMsg(string strCode, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Rest stat Error : {0}", strCode), meberName, lineNumber);
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));

        }
        //  Cancel Code
        private void SendCancelMsg(string strCode, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur cancel Error : {0}", strCode), meberName, lineNumber);
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        // 邏輯判斷錯誤
        private void SendAlmMsg(enumCustomError eError, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur eAlarm Error : {0}", eError), meberName, lineNumber);
            int nCode = (int)eError;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生警告
        private void SendWarningMsg(enumCustomWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Occur Warning  : {0}", eWarning), meberName, lineNumber);
            int nCode = (int)eWarning;
            OnOccurWarning?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除警告
        private void RestWarningMsg(enumCustomWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            WriteLog(string.Format("Reset Warning  : {0}", eWarning), meberName, lineNumber);
            int nCode = (int)eWarning;
            OnOccurWarningRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        #endregion

        #region =========================== Send Command =======================================
        private void CommandW(int nTimeout, enumCommand eCommand, params object[] args)
        {
            string strCmd = m_dicCmdsTable[eCommand];
            if (args == null || args.Length == 0)
            {
                strCmd += "()";
            }
            else
            {
                string placeholders = string.Join(",", Enumerable.Range(0, args.Length).Select(i => "{" + i + "}"));
                strCmd = string.Format(strCmd + "(" + placeholders + ")", args);//
            }

            m_SignalSubSequence.Reset();
            if (m_bSimulate == false)
            {
                m_SignalAck[eCommand].Reset();

                m_Socket.SendCommand(strCmd);

                if (!m_SignalAck[eCommand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumCustomError.SendCommandAckTimeout);
                    int intErrorId = GetAlignerErrorCode(enumCustomError.SendCommandAckTimeout);
                    throw new SException(intErrorId, string.Format("Send command and wait Ack was timeout. [{0}]", eCommand));
                }
                if (m_SignalAck[eCommand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumCustomError.SendCommandFailure);
                    int intErrorId = GetAlignerErrorCode(enumCustomError.SendCommandFailure);
                    throw new SException(intErrorId, string.Format("Send command and wait Ack was failure. [{0}]", eCommand));
                }
            }
            else
            {
                switch (eCommand)
                {
                    case enumCommand.GTLC:
                        {
                            SpinWait.SpinUntil(() => false, 1000);
                            AnalysisGTLC("1,0,0,0");
                        }
                        break;

                }


            }
            m_SignalSubSequence.Set();
        }

        public void InitW(int nTimeout) { CommandW(nTimeout, enumCommand.INIT); }
        public void StatW(int nTimeout) { CommandW(nTimeout, enumCommand.STAT); }
        public void EvntW(int nTimeout) { CommandW(nTimeout, enumCommand.EVNT, 0, 1); }

        //計算panel 位移、角度以及notch存在與否
        public void CllcW(int nTimeout, int CllcNo, int StageNo) { CommandW(nTimeout, enumCommand.CLLC, CllcNo, StageNo); }
        public void GtlcW(int nTimeout, int Target) { CommandW(nTimeout, enumCommand.GTLC, Target); }
        public void ExisW(int nTimeout) { CommandW(nTimeout, enumCommand.EXIS); }
        public void RstaW(int nTimeout) { CommandW(nTimeout, enumCommand.RSTA); }
        public void StimW(int nTimeout) { CommandW(nTimeout, enumCommand.STIM, DateTime.Now.ToString("yyyy, MM, dd, HH, mm, ss")); }
        public void GtimW(int nTimeout) { CommandW(nTimeout, enumCommand.GTIM); }
        public void RsetW(int nTimeout) { CommandW(nTimeout, enumCommand.RSET); }

        //設定光源亮度。不會儲存光源亮度進recipe。
        public void StlvW(int nTimeout, int nLightLevel) { CommandW(nTimeout, enumCommand.STLV, nLightLevel); }
        public void GtlvW(int nTimeout) { CommandW(nTimeout, enumCommand.GTLV); }
        public void SvlvW(int nTimeout, int nCameraIndx, int nLightLevel) { CommandW(nTimeout, enumCommand.SVLV, nCameraIndx, nLightLevel); }
        public void StsnW(int nTimeout, int nCameraIndx, string nSN) { CommandW(nTimeout, enumCommand.STSN, nCameraIndx, nSN); }
        public void GtsnW(int nTimeout, int nCameraIndx) { CommandW(nTimeout, enumCommand.GTSN, nCameraIndx); }

        //設定相機曝光值。不會儲存曝光值進recipe。
        public void StetW(int nTimeout, int nCameraIndx, int nExposure) { CommandW(nTimeout, enumCommand.STET, nCameraIndx, nExposure); }
        public void GtetW(int nTimeout, int nCameraIndx) { CommandW(nTimeout, enumCommand.GTET, nCameraIndx); }
        public void SvetW(int nTimeout, int nCameraIndx) { CommandW(nTimeout, enumCommand.SVET, nCameraIndx); }

        //設定相機Gain值。不會儲存Gain進recipe。
        public void StgnW(int nTimeout, int nCameraIndx, int nExposure) { CommandW(nTimeout, enumCommand.STGN, nCameraIndx, nExposure); }
        public void GtgnW(int nTimeout, int nCameraIndx) { CommandW(nTimeout, enumCommand.GTGN, nCameraIndx); }
        public void SvgnW(int nTimeout, int nCameraIndx) { CommandW(nTimeout, enumCommand.SVGN, nCameraIndx); }

        //儲存系統recipe。
        public void SareW(int nTimeout) { CommandW(nTimeout, enumCommand.SARE); }
        public void RRCPW(int nTimeout) { CommandW(nTimeout, enumCommand.RRCP); }
        #endregion

        #region =========================== Status Flag Signals ================================

        public void ResetChangeModeCompleted()
        {
            m_Signals[enumSignalTable.Remote].Reset();
        }
        public void WaitChangeModeCompleted(int nTimeout)
        {
            if (!m_bSimulate)
            {
                if (!m_Signals[enumSignalTable.Remote].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumCustomError.InitialTimeout);
                    int intErrorId = GetAlignerErrorCode(enumCustomError.InitialTimeout);
                    throw new SException(intErrorId, string.Format("Wait initial flag was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (m_Signals[enumSignalTable.Remote].bAbnormalTerminal)
                {
                    SendAlmMsg(enumCustomError.InitialFailure);
                    int intErrorId = GetAlignerErrorCode(enumCustomError.InitialFailure);
                    throw new SException(intErrorId, string.Format("Initial failure."));
                }
            }
        }

        public void ResetOrgnSinal()
        {
            m_Signals[enumSignalTable.OriginCompleted].Reset();
            m_bStatOrgnComplete = false;
        }
        public void WaitOrgnCompleted(int nTimeout)
        {
            if (!m_bSimulate)
            {
                if (!m_Signals[enumSignalTable.OriginCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumCustomError.OriginTimeout);
                    int intErrorId = GetAlignerErrorCode(enumCustomError.OriginTimeout);
                    throw new SException(intErrorId, string.Format("Wait origin flag was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (m_Signals[enumSignalTable.OriginCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumCustomError.OriginFailure);
                    int intErrorId = GetAlignerErrorCode(enumCustomError.OriginFailure);
                    throw new SException(intErrorId, "Origin Failure");
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
            m_Signals[enumSignalTable.MotionCompleted].Reset();
            m_eStatInPos = enumStat4_Move.Moving;
        }
        public void WaitInPos(int nTimeout)
        {
            SpinWait.SpinUntil(() => false, 200);
            if (!m_bSimulate)
            {
                if (!m_Signals[enumSignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumCustomError.MotionTimeout);
                    int intErrorId = GetAlignerErrorCode(enumCustomError.MotionTimeout);
                    throw new SException((int)intErrorId, string.Format("Wait calculation complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (m_Signals[enumSignalTable.MotionCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumCustomError.MotionAbnormal);
                    int intErrorId = GetAlignerErrorCode(enumCustomError.MotionAbnormal);
                    throw new SException(intErrorId, string.Format("Calculation Failure."));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
                m_eStatInPos = enumStat4_Move.InPos;
            }
        }

        #endregion

        private int GetAlignerErrorCode(string StateErrorCode)
        {
            string strErrorCode = StateErrorCode.Substring(2, 2);
            int intErrorCode = int.Parse(strErrorCode);
            return intErrorCode + 0x2001;
        }
        private int GetAlignerErrorCode(enumCustomError ErrorCode)
        {
            int intErrorCode = (int)ErrorCode + 0x20C0;
            return intErrorCode;
        }


    }
}
