using RorzeComm;
using RorzeComm.Threading;
using RorzeUnit.Class.RC500.RCEnum;
using RorzeUnit.Class.RC500.Event;
using RorzeUnit.Event;
using RorzeComm.Log;
using RorzeUnit.Net.Sockets;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;

namespace RorzeUnit.Class.RC500
{
    public abstract class SSRC550ParentsIO : I_RC5X0_IO
    {
        //==============================================================================
        #region =========================== private ============================================
        protected enumRC5X0Mode m_eStatMode;      //記憶的STAT S1第1 bit
        protected bool m_bStatOrgnComplete;       //記憶的STAT S1第2 bit
        protected bool m_bStatProcessed;          //記憶的STAT S1第3 bit
        protected enumRC5X0Status m_eStatInPos;   //記憶的STAT S1第4 bit
        protected int m_nSpeed;                   //記憶的STAT S1第5 bit
        protected string m_strErrCode = "0000";   //記憶的STAT S2

        protected int m_nAckTimeout = 3000;
        protected int m_nMotionTimeout = 60000;

        protected SLogger _logger = SLogger.GetLogger("CommunicationLog");
        protected sRorzeSocket m_Socket;

        protected object m_lockGPIO = new object();
        protected object m_lockGDIO = new object();
        #endregion
        //==============================================================================
        #region =========================== public =============================================
        public bool Simulate { get; private set; }
        public bool Connected { get { return m_Socket.isConnected(); } }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }
        public string VersionData { get; private set; }
        //STAT S1第1 bit
        public enumRC5X0Mode StatMode { get { return m_eStatMode; } }
        public bool IsInitialized { get { return m_eStatMode == enumRC5X0Mode.Remote; } }
        //STAT S1第2 bit
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }
        //STAT S1第3 bit
        public bool IsProcessing { get { return m_bStatProcessed; } }
        //STAT S1第4 bit
        public enumRC5X0Status InPos { get { return m_eStatInPos; } }
        //STAT S1第5 bit
        public int GetSpeed { get { return m_nSpeed; } }
        //STAT S2
        public bool IsError { get { return (m_strErrCode != "0000"); } }
        public virtual int[] GetFanGrev { get { return new int[19]; } }// only rc550
        public virtual int[] GetSenGprs { get { return new int[11]; } }// only rc550
        #endregion
        //==============================================================================
        #region =========================== Event ==============================================
        public event MessageEventHandler OnReadData;        // TCPIP Recive
        public event EventHandler OnInitializationComplete; //對應ExeINIT
        public event EventHandler OnInitializationFail;     //對應ExeINIT

        public event OccurErrorEventHandler OnOccurStatErr;
        public event OccurErrorEventHandler OnOccurCancel;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public event OccurErrorEventHandler OnOccurErrorRest;

        public event NotifyGDIOEventHandler OnNotifyEvntGDIO;

        public event EventHandler<bool> OnNotifyEvntCNCT;

        public virtual event EventHandler<int[]> OnOccurGPRS;     //壓差計

        protected virtual void SendEvent_OnInitializationComplete(object sender, EventArgs e)
        {
            if (OnInitializationComplete != null) OnInitializationComplete(this, e);
        }
        protected virtual void SendEvent_OnInitializationFail(object sender, EventArgs e)
        {
            if (OnInitializationFail != null) OnInitializationFail(this, e);
        }
        protected virtual void SendEvent_OnNotifyEvntGDIO(object sender, NotifyGDIOEventArgs e)
        {
            OnNotifyEvntGDIO?.Invoke(this, e);
        }
        #endregion
        #region =========================== Thread =============================================
        private SPollingThread _exePolling;// TCPIP Recive
        private SInterruptOneThread _threadInit;
        private SInterruptOneThread _threadReset;
        #endregion
        //==============================================================================
        public SSRC550ParentsIO(string strIp, int nPort, int nBodyNo, bool bDisable, bool bSimulate, sServer Sever = null)
        {
            m_Socket = new sRorzeSocket(strIp, nPort, nBodyNo, "DIO", Simulate);

            BodyNo = nBodyNo;
            Simulate = bSimulate;
            Disable = bDisable;

            _signalSubSequence = new SSignal(false, EventResetMode.ManualReset);

            for (int i = 0; i < (int)enumRC5X0Command_IO.Max; i++)
                _signalAck.Add((enumRC5X0Command_IO)i, new SSignal(false, EventResetMode.ManualReset));

            for (int i = 0; i < (int)enumRC500SignalTable.Max; i++)
                _signals.Add((enumRC500SignalTable)i, new SSignal(false, EventResetMode.ManualReset));
            _signals[enumRC500SignalTable.ProcessCompleted].Set();

            _threadInit = new SInterruptOneThread(ExeINIT);
            _threadReset = new SInterruptOneThread(ExeRsta);

            _exePolling = new SPollingThread(1);
            _exePolling.DoPolling += _exePolling_DoPolling;
            _exePolling.Set();

            CreateMessage();
        }
        ~SSRC550ParentsIO()
        {
            _exePolling.Close();
            _exePolling.Dispose();
        }
        //==============================================================================
        public void Open() { m_Socket.Open(); }
        private void _exePolling_DoPolling()
        {
            try
            {
                //  判斷收到          
                string[] astrFrame;
                if (!m_Socket.QueRecvBuffer.TryDequeue(out astrFrame)) return;
                //  傳送到外部顯示畫面用
                if (OnReadData != null) OnReadData(this, new MessageEventArgs(astrFrame));

                for (int i = 0; i < astrFrame.Count(); i++) //只處理第一個封包 2014.11.24
                {
                    if (astrFrame[i].Length == 0) continue;

                    string strFrame = astrFrame[i];

                    if (strFrame.Contains("GPRS") || strFrame.Contains("GREV") || strFrame.Contains("GPIO"))
                    {
                        //壓差不需要紀錄
                        //FFU轉速
                    }
                    else
                        _logger.WriteLog("[DIO{0}]:{1}", this.BodyNo, strFrame);

                    enumRC5X0Command_IO cmd = enumRC5X0Command_IO.GVER;
                    bool bUnknownCmd = true;
                    foreach (string scmd in _dicCmdsTable.Values) //查字典
                    {
                        if (strFrame.Contains(string.Format("DIO{0}.{1}", this.BodyNo.ToString("X"), scmd)))
                        {
                            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                            bUnknownCmd = false;//認識這個指令
                            break;
                        }
                    }
                    if (bUnknownCmd) //不認識的封包
                    {
                        _logger.WriteLog("[DIO{0}] <<<ByPassReceive>>> Got unknown frame and pass to process. [{1}]", this.BodyNo, strFrame);
                        continue;
                    }
                    switch (strFrame[0]) //命令種類
                    {
                        case 'c': //cancel
                            OnCancelAck(this, new RorzeProtoclEventArgs(strFrame));
                            break;
                        case 'n': //nak
                            _signalAck[cmd].bAbnormalTerminal = true;
                            _signalAck[cmd].Set();
                            break;
                        case 'a': //ack
                            OnAck(this, new RorzeProtoclEventArgs(strFrame));
                            _signalAck[cmd].Set();
                            break;
                        case 'e':
                            OnAck(this, new RorzeProtoclEventArgs(strFrame));
                            break;
                        default:

                            break;
                    }
                }
            }
            catch (SException ex)
            {
                _logger.WriteLog("[DIO{0}] <<SException>> _exePolling_DoPolling:" + ex, this.BodyNo);
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> _exePolling_DoPolling:" + ex, this.BodyNo);
            }
        }
        protected virtual void OnAck(object sender, RorzeProtoclEventArgs e)
        {
            enumRC5X0Command_IO cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                case enumRC5X0Command_IO.STAT:
                    AnalysisStatus(e.Frame.Value);
                    break;
                case enumRC5X0Command_IO.GDIO:
                    AnalysisGDIO(e.Frame.Value);
                    break;
                case enumRC5X0Command_IO.GVER:
                    VersionData = e.Frame.Value;
                    break;
                case enumRC5X0Command_IO.CNCT:
                    //Connected = true;
                    INIT();

                    OnNotifyEvntCNCT?.Invoke(this, true);

                    break;
                default:
                    break;
            }

            //if (!Connected)
            //{
            //	Connected = true;
            //}
        }
        void OnCancelAck(object sender, RorzeProtoclEventArgs e)
        {
            enumRC5X0Command_IO cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;
            AnalysisCancel(e.Frame.Value);
        }
        private void AnalysisCancel(string strFrame)
        {
            if (Convert.ToInt32(strFrame, 16) > 0)
            {
                _signals[enumRC500SignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumRC500SignalTable.MotionCompleted].Set(); //有moving過才可以Set
                SendCancelMsg(strFrame);
            }
        }
        private void AnalysisStatus(string strFrame)
        {
            if (!strFrame.Contains('/'))
            {
                _logger.WriteLog("[DIO{0}] the format of STAT frame has error, '/' not found! [{1}]", this.BodyNo, strFrame);
                return;
            }
            string[] str = strFrame.Split('/');
            string s1 = str[0];
            string s2 = str[1];

            //S1.bit#1 operation mode
            switch (s1[0])
            {
                case '0':
                    m_eStatMode = enumRC5X0Mode.Initializing;
                    //_signals[enumRC500SignalTable.Remote].Reset();
                    break;
                case '1':
                    m_eStatMode = enumRC5X0Mode.Remote;
                    _signals[enumRC500SignalTable.Remote].Set();
                    break;
                case '2':
                    m_eStatMode = enumRC5X0Mode.Maintenance;
                    _signals[enumRC500SignalTable.Remote].Set();
                    break;
                case '3':
                    m_eStatMode = enumRC5X0Mode.Recovery;
                    break;
                default: break;
            }

            //S1.bit#2 origin return complete
            if (s1[1] == '0')
                _signals[enumRC500SignalTable.OPRCompleted].Reset();
            else
                _signals[enumRC500SignalTable.OPRCompleted].Set();
            m_bStatOrgnComplete = s1[1] == '1';

            //S1.bit#3 processing command
            if (s1[2] == '0')
                _signals[enumRC500SignalTable.ProcessCompleted].Set();
            else
                _signals[enumRC500SignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = s1[2] == '1';

            //S1.bit#4 operation status
            switch (s1[3])
            {
                case '0': m_eStatInPos = enumRC5X0Status.InPos; break;
                case '1': m_eStatInPos = enumRC5X0Status.Moving; break;
                case '2': m_eStatInPos = enumRC5X0Status.Pause; break;
            }

            //S1.bit#5 operation speed
            if (s1[4] >= '0' && s1[4] <= '9') m_nSpeed = s1[4] - '0';
            else if (s1[4] >= 'A' && s1[4] <= 'K') m_nSpeed = s1[4] - 'A' + 10;
            if (m_nSpeed == 0) m_nMotionTimeout = 60000;
            else m_nMotionTimeout = 60000 * 3;

            //S2
            if (Convert.ToInt32(s2, 16) > 0)
            {
                _signals[enumRC500SignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumRC500SignalTable.MotionCompleted].Set();
                SendAlmMsg(s2);
                m_strErrCode = s2;
            }
            else
            {
                if (m_eStatInPos == enumRC5X0Status.InPos)//運動到位 
                    _signals[enumRC500SignalTable.MotionCompleted].Set();
                else
                    _signals[enumRC500SignalTable.MotionCompleted].Reset();

                if (m_strErrCode != "0000")
                {
                    RestAlmMsg(m_strErrCode);
                    m_strErrCode = "0000";
                }
            }
        }
        public void StopPollingThread()
        {
            _exePolling.Close();
            _exePolling.Dispose();
        }
        //==============================================================================
        public void INIT() { _threadInit.Set(); }
        public void RSTA() { _threadReset.Set(); }
        protected virtual void ExeINIT()
        {
            try
            {
                _logger.WriteLog(string.Format("[DIO{0}] ExeINIT start", BodyNo));
                this.EvntW();


                this.ResetProcessCompleted();
                this.InitW();
                this.WaitProcessCompleted(3000);

                this.StimW();

                if (OnInitializationComplete != null)
                    OnInitializationComplete(this, new EventArgs());
            }
            catch (SException ex)
            {
                _logger.WriteLog("[DIO{0}] <<SException>> ExeINIT:" + ex, this.BodyNo);

                if (OnInitializationFail != null)
                    OnInitializationFail(this, new EventArgs());
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> ExeINIT:" + ex, this.BodyNo);

                if (OnInitializationFail != null)
                    OnInitializationFail(this, new EventArgs());
            }
        }
        private void ExeRsta()
        {
            try
            {
                _logger.WriteLog("[STG{0}] ExeRsta start", this.BodyNo);
                ResetProcessCompleted();
                this.RstaW();
                WaitProcessCompleted(3000);
            }
            catch (SException ex)
            {
                _logger.WriteLog("[DIO{0}] <<SException>> ExeRsta:" + ex, this.BodyNo);
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> ExeRsta:" + ex, this.BodyNo);
            }
        }
        //==============================================================================
        public void InitW()
        {
            int nTimeout = 10000;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.INIT].Reset();
                m_Socket.SendCommand(string.Format("INIT"));
                if (!_signalAck[enumRC5X0Command_IO.INIT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.INIT]));
                }
                if (_signalAck[enumRC5X0Command_IO.INIT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.INIT]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        protected void StimW()
        {
            int nTimeout = 10000;
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signalAck[enumRC5X0Command_IO.STIM].Reset();
                m_Socket.SendCommand("STIM(" + DateTime.Now.ToString("yyyy, MM, dd, HH, mm, ss") + ")");
                if (!_signalAck[enumRC5X0Command_IO.STIM].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.STIM]));
                }
                if (_signalAck[enumRC5X0Command_IO.STIM].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.STIM]));
                }
            }
            _signalSubSequence.Set();
        }
        public void EvntW()
        {
            int nTimeout = 10000;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.EVNT].Reset();
                m_Socket.SendCommand("EVNT(0,1)");
                if (!_signalAck[enumRC5X0Command_IO.EVNT].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.EVNT]));
                }
                if (_signalAck[enumRC5X0Command_IO.EVNT].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.EVNT]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        public virtual void MoveW(int nValue) { }// only rc550
        public virtual void StopW(int nValue) { }// only rc550
        public void RstaW()
        {
            int nTimeout = 10000;
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.RSTA].Reset();
                m_Socket.SendCommand(string.Format("RSTA"));
                if (!_signalAck[enumRC5X0Command_IO.RSTA].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.RSTA]));
                }
                if (_signalAck[enumRC5X0Command_IO.RSTA].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.RSTA]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        public virtual void SdobW(int nID, int nBit, bool bOn)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {

                _signalAck[enumRC5X0Command_IO.SDOB].Reset();
                m_Socket.SendCommand("SDOB(" + Convert.ToInt32(nID) + "," + Convert.ToInt32(nBit) + "," + Convert.ToInt32(bOn) + ",0,0)");
                if (!_signalAck[enumRC5X0Command_IO.SDOB].WaitOne(m_nAckTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.SDOB]));
                }
                if (_signalAck[enumRC5X0Command_IO.SDOB].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.SDOB]));
                }
            }
            else
            {
                SetGDIO_OutputStatus(nID, nBit, bOn);
            }
            _signalSubSequence.Set();
        }
        public virtual void SdouW(int nID, int nBit, bool bOn)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.SDOU].Reset();

                int nData = bOn ? (0x01 << nBit) : 0;
                int nMask = (0x01 << nBit);
                string strCmd = string.Format("SDOU({0:D3},{1:X4}/{2:X4})", nID, nData, nMask);
                m_Socket.SendCommand(strCmd);
                if (!_signalAck[enumRC5X0Command_IO.SDOU].WaitOne(m_nAckTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.SDOU]));
                }
                if (_signalAck[enumRC5X0Command_IO.SDOU].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.SDOU]));
                }
            }
            else
            {
                SetGDIO_OutputStatus(nID, nBit, bOn);
            }
            _signalSubSequence.Set();
        }
        public virtual void SdouW(int nID, int nBit1, int nBit2) { }// only rc550
        public abstract bool GdioW(int nID, int Bit);
        //==============================================================================

        private void ResetChangeModeCompleted()
        {
            _signals[enumRC500SignalTable.Remote].Reset();
        }
        private void WaitChangeModeCompleted(int nTimeout)
        {
            if (!Simulate)
            {
                if (!_signals[enumRC500SignalTable.Remote].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.InitialFailure);
                    throw new SException((int)enumRC500Error.InitialFailure, string.Format("Wait Mode was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumRC500SignalTable.Remote].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.InitialFailure);
                    throw new SException((int)enumRC500Error.InitialFailure, string.Format("Motion is Mode end."));
                }
            }
        }

        protected void ResetProcessCompleted()
        {
            _signals[enumRC500SignalTable.ProcessCompleted].Reset();
        }
        protected void WaitProcessCompleted(int nTimeout)
        {
            if (!Simulate)
            {
                if (!_signals[enumRC500SignalTable.ProcessCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumRC500Error.ProcessFlagTimeout);
                    throw new SException((int)enumRC500Error.ProcessFlagTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (_signals[enumRC500SignalTable.ProcessCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.ProcessFlagAbnormal);
                    throw new SException((int)enumRC500Error.ProcessFlagAbnormal, string.Format("Motion is abnormal end."));
                }
            }
            else
                Thread.Sleep(500);
        }

        //==============================================================================


        #region =========================== CommandTable =======================================
        public Dictionary<enumRC5X0Command_IO, string> _dicCmdsTable;
        #endregion
        #region =========================== Signals ============================================
        protected Dictionary<enumRC5X0Command_IO, SSignal> _signalAck = new Dictionary<enumRC5X0Command_IO, SSignal>();
        protected Dictionary<enumRC500SignalTable, SSignal> _signals = new Dictionary<enumRC500SignalTable, SSignal>();
        protected SSignal _signalSubSequence;
        #endregion
        #region =========================== OnOccurError =======================================
        //  發生STAT異常
        protected void SendAlmMsg(string strCode)
        {
            _logger.WriteLog("[DIO{0}] Occur stat Error : {1}", this.BodyNo, strCode);
            if (strCode.Length != 4) return;
            //  DIO0 25 11 00000
            //  DIO1 26 11 00000
            //  DIO2 27 11 00000           
            int nCode = Convert.ToInt32(strCode, 16) /*+ (25 + BodyNo) * 10000000 + 11 * 100000*/;
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除STAT異常
        protected void RestAlmMsg(string strCode)
        {
            _logger.WriteLog("[DIO{0}] Rest stat Error : {1}", this.BodyNo, strCode);
            if (strCode.Length != 4) return;
            //  DIO0 25 11 00000
            //  DIO1 26 11 00000
            //  DIO2 27 11 00000        
            int nCode = Convert.ToInt32(strCode, 16) /*+ (25 + BodyNo) * 10000000 + 11 * 100000*/;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));

        }
        //  Cancel Code
        protected void SendCancelMsg(string strCode)
        {
            _logger.WriteLog("[DIO{0}] Occur cancel Error : {1}", this.BodyNo, strCode);
            if (strCode.Length != 4) return;
            //  DIO1 25 12 00000
            //  DIO1 25 12 00000
            //  DIO2 27 12 00000        
            int nCode = Convert.ToInt32(strCode, 16) /*+ (25 + BodyNo) * 10000000 + 12 * 100000*/;
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  Custom Error
        protected void SendAlmMsg(enumRC500Error eAlarm)
        {
            _logger.WriteLog("[DIO{0}] Occur eAlarm Error : {1}", this.BodyNo, eAlarm);
            //  DIO0 25 10 00000
            //  DIO1 26 10 00000
            //  DIO2 27 10 00000        
            int nCode = (int)eAlarm /*+ (25 + BodyNo) * 10000000 + 10 * 100000*/;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        #endregion
        #region =========================== Signals ============================================
        public Dictionary<int, string> m_dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();
        protected abstract void CreateMessage();

        #endregion
        #region =========================== AnalysisGDIO =======================================
        protected Dictionary<int, string> m_dicGDIO_I = new Dictionary<int, string>();
        protected Dictionary<int, string> m_dicGDIO_O = new Dictionary<int, string>();
        protected abstract void AnalysisGDIO(string strFrame);
        public bool GetGDIO_InputStatus(int nHCLID, int nBit)
        {
            try
            {
                bool bOn = false;
                lock (m_lockGDIO)
                {
                    if (m_dicGDIO_I.ContainsKey(nHCLID) == false)
                        bOn = false;
                    else
                        bOn = IsBitOn(m_dicGDIO_I[nHCLID], nBit);
                }
                return bOn;
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> GetGDIO_InputStatus:" + ex, this.BodyNo);
                return false;
            }
        }
        public bool GetGDIO_OutputStatus(int nHCLID, int nBit)
        {
            try
            {
                bool bOn = false;
                lock (m_lockGDIO)
                {
                    if (m_dicGDIO_O.ContainsKey(nHCLID) == false)
                        bOn = false;
                    else
                        bOn = IsBitOn(m_dicGDIO_O[nHCLID], nBit);
                }
                return bOn;
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> GetGDIO_OutputStatus:" + ex, this.BodyNo);
                return false;
            }
        }
        public void SetGDIO_InputStatus(int nHCLID, int nBit, bool bOn)//Simulate
        {
            try
            {
                if (Simulate == false) return;
                lock (m_lockGDIO)
                {
                    if (m_dicGDIO_I.ContainsKey(nHCLID) == false)
                        m_dicGDIO_I.Add(nHCLID, "0000");
                    if (m_dicGDIO_O.ContainsKey(nHCLID) == false)
                        m_dicGDIO_O.Add(nHCLID, "0000");
                    string strHex = SetBitOn(m_dicGDIO_I[nHCLID], nBit, bOn);
                    m_dicGDIO_I[nHCLID] = strHex;
                }
                //  Notify Evnt              
                OnNotifyEvntGDIO?.Invoke(this, new NotifyGDIOEventArgs(nHCLID, m_dicGDIO_I[nHCLID], m_dicGDIO_O[nHCLID]));
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> SetGDIO_InputStatus:" + ex, this.BodyNo);
            }
        }
        public void SetGDIO_OutputStatus(int nHCLID, int nBit, bool bOn)//Simulate
        {
            try
            {
                if (Simulate == false) return;
                lock (m_lockGDIO)
                {
                    if (m_dicGDIO_I.ContainsKey(nHCLID) == false)
                        m_dicGDIO_I.Add(nHCLID, "0000");
                    if (m_dicGDIO_O.ContainsKey(nHCLID) == false)
                        m_dicGDIO_O.Add(nHCLID, "0000");
                    string strHex = SetBitOn(m_dicGDIO_O[nHCLID], nBit, bOn);
                    m_dicGDIO_O[nHCLID] = strHex;
                }
                //  Notify Evnt              
                OnNotifyEvntGDIO?.Invoke(this, new NotifyGDIOEventArgs(nHCLID, m_dicGDIO_I[nHCLID], m_dicGDIO_O[nHCLID]));
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> SetGDIO_OutputStatus:" + ex, this.BodyNo);
            }
        }
        bool IsBitOn(string strValue, int nBit)
        {
            Int64 nValue = Convert.ToInt64(strValue, 16);
            Int64 nV = 0x01 << nBit;
            return ((nValue & nV) == nV);
        }
        string SetBitOn(string strValue, int nBit, bool bOn)
        {
            Int64 nValue = Convert.ToInt64(strValue, 16);
            Int64 nV = 0x01 << nBit;

            string strHex;
            if (bOn)
            {
                strHex = (nValue | nV).ToString("X4");
            }
            else
            {
                strHex = (nValue & ~nV).ToString("X4");
            }
            return strHex;
        }
        #endregion


    }
}

