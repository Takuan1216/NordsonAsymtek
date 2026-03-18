using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using AAComm;
using AAMotion;
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.Aligner.Enum;
using RorzeUnit.Class.RC500.Event;
using RorzeUnit.Class.RC500.RCEnum;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System.Diagnostics;
using System.Collections;
using System;
using RorzeUnit.Class.Agito;
using RorzeUnit.Class.Agito.Enum;
using System.Collections.Concurrent;


namespace RorzeUnit.Class.Agito
{
    public class SSAGD301_Motion
    {
        //==============================================================================
        #region =========================== private ============================================
        string m_sIP = "";
        int m_iPortID;
        ISingleAxis[] m_MAxis = new ISingleAxis[3];
        AGD301 m_MCtrl = new AGD301();
        //CommAPI m_MCtrlComm = new CommAPI();
        SSPanelAlignerParm.SSAgitoParm AgitoParm;
        object m_LockMCtrlComm = new object();
        Dictionary<enumMotionTimeout, int> Motiontimeout = new Dictionary<enumMotionTimeout, int>();
        int m_iInput = 0;
        int m_iOutput = 0;
        int m_nInputNow;
        int m_nOutputNow;

        bool m_bStatOrgnComplete = false;
        //private Dictionary<enumAlignerSignalTable, SSignal> m_Signals;
        private SSignal m_SignalSubSequence;
        private Dictionary<enumAlignerSignalTable, SSignal> m_ThisSignals = new Dictionary<enumAlignerSignalTable, SSignal>();
        bool m_bSimulate = false;
        private enumAlignerStatus m_eStatInPos;
        Dictionary<int, string> m_dictErroringList = new Dictionary<int, string>();
        string[] m_strAxisName = new string[3] { "R", "Y", "X" };

        private void SetMotionTimeout()
        {
            Motiontimeout[enumMotionTimeout.ORGN] = 60000;
            Motiontimeout[enumMotionTimeout.MREL] = 60000;
            Motiontimeout[enumMotionTimeout.MABS] = 60000;
            Motiontimeout[enumMotionTimeout.STOP] = 5000;

        }
        private SLogger _logger;
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[PAM{0}] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        private void CreateMessage()
        {

        }

        private class GMotionTimer
        {
            Stopwatch stopWatch = new Stopwatch();
            int m_imsTimeout = 0;
            public GMotionTimer(int msTimeout)
            {
                stopWatch.Start();
                m_imsTimeout = msTimeout;
            }
            public bool IsTimeout()
            {
                return stopWatch.ElapsedMilliseconds > m_imsTimeout ? true : false;
            }
        }
        #endregion
        //==============================================================================

        public bool Simulate { get; private set; }
        public bool Connected { get; private set; }
        public bool IsError
        {
            get
            {
                if (m_dictErroringList.Count != 0)
                {
                    foreach (KeyValuePair<int, string> item in m_dictErroringList)
                        WriteLog(item.Value);
                    return true;
                }
                return false;
                //    (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0 ||
                //m_MAxis[(int)enumAGD301Axis.AXS2].ConFlt != 0 ||
                //m_MAxis[(int)enumAGD301Axis.AXS3].ConFlt != 0);
            }
        }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }
        public string VersionData { get; private set; }
        public bool IsInitialized { get { return Connected; } }

        public int Axis1Pos { get { return (int)(m_MAxis[0].Pos / AgitoParm.GetAxisResolution(0)); } protected set { } }
        public int Axis2Pos { get { return (int)(m_MAxis[1].Pos / AgitoParm.GetAxisResolution(1)); } protected set { } }
        public int Axis3Pos { get { return (int)(m_MAxis[2].Pos / AgitoParm.GetAxisResolution(2)); } protected set { } }


        //STAT S1第2 bit
        public bool IsOrgnComplete { get { return m_bStatOrgnComplete; } }

        public bool IsMoving
        {
            get
            {
                return m_MCtrl.A.InTargetStat == 2 || m_MCtrl.A.InTargetStat == 2 || m_MCtrl.A.InTargetStat == 2;

            }
        }

        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();


        //==============================================================================
        #region =========================== Thread =============================================
        private SPollingThread _exePolling;// TCPIP Recive
        private SInterruptOneThread _threadInitial;
        private SInterruptOneThreadINT_INT _threadOrgn;
        private SInterruptOneThreadINT_INT _threadSTEP;
        private SInterruptOneThreadINT_INT _threadMABS;
        private SInterruptOneThreadINT_INT _threadMREL;
        private SInterruptOneThreadINT _threadRSTA;
        private SInterruptOneThread _threadSTOP;
        #endregion
        //==============================================================================
        #region =========================== Event ==============================================
        #endregion


        public SSAGD301_Motion(int nBodyNo, bool bSimulate)
        {
            BodyNo = nBodyNo;
            Simulate = bSimulate;
            _logger = SLogger.GetLogger("ALN" + nBodyNo.ToString());
            AgitoParm = new SSPanelAlignerParm.SSAgitoParm(nBodyNo.ToString(), 3);
            for (int i = 0; i < (int)enumAlignerSignalTable.Max; i++)
                m_ThisSignals.Add((enumAlignerSignalTable)i, new SSignal(false, EventResetMode.ManualReset));
            AgitoParm.LoadIni();
            m_sIP = AgitoParm.GetIP;
            Connected = false;
            SetMotionTimeout();
            _threadInitial = new SInterruptOneThread(ExeINIT);
            _threadOrgn = new SInterruptOneThreadINT_INT(ExeORGN);
            _threadMABS = new SInterruptOneThreadINT_INT(ExeMABS);
            _threadMREL = new SInterruptOneThreadINT_INT(ExeMREL);
            _threadRSTA = new SInterruptOneThreadINT(ExeRSTA);
            _threadSTOP = new SInterruptOneThread(ExeSTOP);
            _exePolling = new SPollingThread(100);
            _exePolling.DoPolling += _exePolling_DoPolling;
            m_MAxis[0] = m_MCtrl.A;
            m_MAxis[1] = m_MCtrl.B;
            m_MAxis[2] = m_MCtrl.C;
            m_MCtrl.ErrorOccurred += (ErrorCode, SentMsg, ErrorMsg) =>
            {
                string Msg = $"[Controller] [{ErrorCode}]:{ErrorMsg}";
                if (m_dictErroringList.ContainsKey(ErrorCode) == false)
                {
                    m_dictErroringList.Add(ErrorCode, Msg);
                    WriteLog(Msg);
                }
            };
            if (Simulate)
            {

            }
            //CreateMessage();

        }
        private void _exePolling_DoPolling()
        {
            lock (m_LockMCtrlComm)
            {
                if (m_MCtrl.IsConnected == true)
                {
                    m_nInputNow = m_MCtrl.IO.DInPort;
                    m_nOutputNow = m_MCtrl.IO.DOutPort;

                    if (m_iInput != m_nInputNow)
                    {
                        WriteLog(string.Format("DI Change {0} => {1}", m_iInput, m_nInputNow));
                        m_iInput = m_nInputNow;
                    }
                    if (m_iOutput != m_nOutputNow)
                    {
                        WriteLog(string.Format("DO Change {0} => {1}", m_iInput, m_nOutputNow));
                        m_iOutput = m_nOutputNow;
                    }
                }
            }

        }
        //--------------------------------------------------
        public void Open() { return;/*use AAComm,not socket*/ }
        //--------------------------------------------------
        public void INIT() { _threadInitial.Set(); }
        public void ORGN(int mode, int Axis) { _threadOrgn.Set(mode, Axis); }
        public void MABS(int nAxis, int nPluse) { _threadMABS.Set(nAxis, nPluse); }
        public void MREL(int nAxis, int nPluse) { _threadMREL.Set(nAxis, nPluse); }
        public void RSTA(int nReset) { _threadRSTA.Set(nReset); }
        public void STOP() { _threadSTOP.Set(); }
        public void ExeINIT()
        {
            try
            {
                Init();
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
        private void ExeORGN(int mode, int Axis)
        {
            try
            {
                if (System.Enum.IsDefined(typeof(enumAGD301Axis), Axis) == false)
                {
                    throw new Exception("Error Parameter");
                }
                Orgn(Motiontimeout[enumMotionTimeout.ORGN], (enumAGD301Axis)Axis, mode);
            }
            catch (SException ex)
            {
                WriteLog(" ExeORGN MotionCompleted Set");
                m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
                WriteLog("<<Exception>>" + ex);
            }
        }

        private void ExeMABS(int nAxis, int nPluse)
        {
            try
            {
                if (System.Enum.IsDefined(typeof(enumAGD301Axis), nAxis) == false)
                {
                    throw new Exception("Error Parameter");
                }
                AxisMabs(Motiontimeout[enumMotionTimeout.MABS], (enumAGD301Axis)nAxis, nPluse);
            }
            catch (SException ex)
            {
                m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
                WriteLog("ExeMABS :" + ex.ToString());
            }
        }
        private void ExeMREL(int nAxis, int nPluse)
        {
            try
            {
                if (System.Enum.IsDefined(typeof(enumAGD301Axis), nAxis) == false)
                {
                    throw new Exception("Error Parameter");
                }
                enumAGD301Axis eAxis = (enumAGD301Axis)nAxis;
                AxisMrel(Motiontimeout[enumMotionTimeout.MREL], eAxis, nPluse);
            }
            catch (SException ex)
            {
                m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
                WriteLog("<<Exception>>" + ex);
            }
        }
        private void ExeRSTA(int nReset)
        {
            try
            {
                Reset(3000, nReset);
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
        private void ExeSTOP()
        {
            try
            {
                Stop(Motiontimeout[enumMotionTimeout.STOP]);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("ExeSTOP :" + ex);
            }
        }
        //--------------------------------------------------
        public void Orgn(int timeout, enumAGD301Axis Axis, int mode = 0)
        {
            WriteLog("Start Orgn");
            bool GetErr = false;
            SException sException = null;
            if (m_MAxis[(int)enumAGD301Axis.AXS1].MotorOn == 0 ||
                m_MAxis[(int)enumAGD301Axis.AXS2].MotorOn == 0 ||
                m_MAxis[(int)enumAGD301Axis.AXS3].MotorOn == 0)
            {
                Exct(3000, 1);
            }
            if (mode == 3)
            {
                //Be careful!! 
                //Reset encoder
                switch (Axis)
                {
                    case enumAGD301Axis.ALL:
                        {
                            //GMotionTimer Timer = new GMotionTimer(Motiontimeout[enumMotionTimeout.ORGN]);
                            m_MAxis[(int)enumAGD301Axis.AXS1].Home();
                            m_MAxis[(int)enumAGD301Axis.AXS2].Home();
                            m_MAxis[(int)enumAGD301Axis.AXS3].Home();
                            if (!SpinWait.SpinUntil
                                (
                                    () =>
                                    {
                                        if (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0 || m_MAxis[(int)enumAGD301Axis.AXS2].ConFlt != 0 || m_MAxis[(int)enumAGD301Axis.AXS3].ConFlt != 0)
                                        {
                                            GetErr = true;
                                            return true;
                                        }
                                        return (m_MAxis[(int)enumAGD301Axis.AXS1].HomingStat == (int)enumHomeStat.HomingComplete &&
                                                m_MAxis[(int)enumAGD301Axis.AXS2].HomingStat == (int)enumHomeStat.HomingComplete &&
                                                m_MAxis[(int)enumAGD301Axis.AXS3].HomingStat == (int)enumHomeStat.HomingComplete);
                                    }, Motiontimeout[enumMotionTimeout.ORGN]
                                ))
                            {
                                throw new SException((int)enumAgitoError.Axis123MovingTimeout, string.Format("Homing timeout. [{0}]", "ORGN"));
                            }

                            break;
                        }
                    case enumAGD301Axis.AXS1:
                        {
                            AxisHome_SetEncoderZero(enumAGD301Axis.AXS1);
                            break;
                        }
                    case enumAGD301Axis.AXS2:
                        {
                            AxisHome_SetEncoderZero(enumAGD301Axis.AXS2);
                            break;
                        }
                    case enumAGD301Axis.AXS3:
                        {
                            AxisHome_SetEncoderZero(enumAGD301Axis.AXS3);
                            break;
                        }
                }
            }
            else
            {
                int TargetPos1 = (int)AgitoParm.GetAxisHomeOffset((int)enumAGD301Axis.AXS1);
                int TargetPos2 = (int)AgitoParm.GetAxisHomeOffset((int)enumAGD301Axis.AXS2);
                int TargetPos3 = (int)AgitoParm.GetAxisHomeOffset((int)enumAGD301Axis.AXS3);
                WriteLog($"ORGN(Pos) :R({TargetPos1})Y({TargetPos2})X({TargetPos3})");
                //Go to encoder 0
                lock (m_LockMCtrlComm)
                {
                    sException = CheckAxisStatus("ORGN", enumAGD301Axis.AXS1);
                    if (sException != null)
                        throw sException;
                    m_MAxis[(int)enumAGD301Axis.AXS1].Home();

                    sException = CheckAxisStatus("ORGN", enumAGD301Axis.AXS2);
                    if (sException != null)
                        throw sException;
                    m_MAxis[(int)enumAGD301Axis.AXS2].MoveAbs(TargetPos2/*, AgitoParm.GetAxisHomingVel((int)enumAGD301Axis.AXS2), AgitoParm.GetAxisHomingAcc((int)enumAGD301Axis.AXS2), AgitoParm.GetAxisHomingDec((int)enumAGD301Axis.AXS2)*/);

                    sException = CheckAxisStatus("ORGN", enumAGD301Axis.AXS3);
                    if (sException != null)
                        throw sException;
                    m_MAxis[(int)enumAGD301Axis.AXS3].MoveAbs(TargetPos3/*, AgitoParm.GetAxisHomingVel((int)enumAGD301Axis.AXS3), AgitoParm.GetAxisHomingAcc((int)enumAGD301Axis.AXS3), AgitoParm.GetAxisHomingDec((int)enumAGD301Axis.AXS3)*/);
                }
                if (!SpinWait.SpinUntil(() =>
                {
                    lock (m_LockMCtrlComm)
                    {
                        if (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0 || m_MAxis[(int)enumAGD301Axis.AXS2].ConFlt != 0 || m_MAxis[(int)enumAGD301Axis.AXS3].ConFlt != 0)
                        {
                            GetErr = true;
                            return true;
                        }
                        return (m_MAxis[(int)enumAGD301Axis.AXS1].HomingStat == (int)enumHomeStat.HomingComplete) &&
                                (m_MAxis[(int)enumAGD301Axis.AXS2].InTargetStat == (int)enumInPosStat.InPos) &&
                                (m_MAxis[(int)enumAGD301Axis.AXS3].InTargetStat == (int)enumInPosStat.InPos);
                    }
                }, Motiontimeout[enumMotionTimeout.ORGN]))
                {
                    string CmdType = "ORGN";
                    int AgitoError = (int)enumAgitoError.Axis123MovingTimeout;
                    string ErrorMsg = string.Format("AXS123 {0} timeout. [{0}]", CmdType, CmdType);
                    if (m_MAxis[(int)enumAGD301Axis.AXS1].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS1 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis1MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    SpinWait.SpinUntil(() => false, 100);
                    if (m_MAxis[(int)enumAGD301Axis.AXS2].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS2 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis2MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    SpinWait.SpinUntil(() => false, 100);
                    if (m_MAxis[(int)enumAGD301Axis.AXS3].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS3 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis3MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop all");
                    Stop(3000);
                    SpinWait.SpinUntil(() => false, 500);
                    WriteLog($"[{CmdType}](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
                    throw new SException(AgitoError, ErrorMsg);
                }
                if (GetErr)
                {
                    throw MotionErrorProcess("ORGN", enumAGD301Axis.ALL);
                }
                lock (m_LockMCtrlComm)
                {
                    sException = CheckAxisStatus("ORGN", enumAGD301Axis.AXS1);
                    if (sException != null)
                        throw sException;
                    m_MAxis[(int)enumAGD301Axis.AXS1].MoveAbs(TargetPos1/*, AgitoParm.GetAxisHomingVel((int)enumAGD301Axis.AXS2), AgitoParm.GetAxisHomingAcc((int)enumAGD301Axis.AXS2), AgitoParm.GetAxisHomingDec((int)enumAGD301Axis.AXS2)*/);
                }
                if (!SpinWait.SpinUntil(() =>
                {
                    lock (m_LockMCtrlComm)
                    {
                        if (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0)
                        {
                            GetErr = true;
                            return true;
                        }
                        return (m_MAxis[(int)enumAGD301Axis.AXS1].InTargetStat == (int)enumInPosStat.InPos);
                    }
                }, Motiontimeout[enumMotionTimeout.ORGN]))
                    throw MotionTimeoutProcess("ORGN", enumAGD301Axis.AXS1);
                if (GetErr)
                {
                    throw MotionErrorProcess("ORGN", enumAGD301Axis.AXS1);
                }
                m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
                m_ThisSignals[enumAlignerSignalTable.OPRCompleted]?.Set();
            }
            WriteLog("End Orgn");
        }
        public void AxisHome_SetEncoderZero(enumAGD301Axis Axis)
        {
            bool GetErr = false;
            GMotionTimer Timer = new GMotionTimer(Motiontimeout[enumMotionTimeout.ORGN]);
            m_MAxis[(int)Axis].Home();
            if (!SpinWait.SpinUntil(() =>
            {
                lock (m_LockMCtrlComm)
                {
                    if (m_MAxis[(int)Axis].ConFlt != 0)
                    {
                        GetErr = true;
                        return true;
                    }
                    return (m_MAxis[(int)Axis].HomingStat == (int)enumHomeStat.HomingComplete);
                }
            }, Motiontimeout[enumMotionTimeout.ORGN]))
                throw new SException((int)enumAgitoError.Axis123HomingTimeout + (int)(Axis + 1), string.Format(Axis.ToString() + $"homing timeout. [{0}]", "ORGN"));
            if (GetErr)
            {
                throw new SException((int)enumAgitoError.Axis123HomingError + (int)(Axis + 1), string.Format("Controller error. [{0}]", "ORGN"));
            }
        }

        public void Mabs(int nTimeout, int Axis1pluse, int Axis2pluse, int Axis3pluse, int spd = 0)
        {
            WriteLog("Start Mabs");
            //Axis1pluse 1000 = 1degree
            //Axis2pluse 1000 = 1mm
            //Axis3pluse 1000 = 1mm
            bool GetErr = false;
            int Speed1 = spd == 0 ? AgitoParm.GetAxisRunVel((int)enumAGD301Axis.AXS1) : (int)(spd * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS1));
            int Speed2 = spd == 0 ? AgitoParm.GetAxisRunVel((int)enumAGD301Axis.AXS2) : (int)(spd * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS2));
            int Speed3 = spd == 0 ? AgitoParm.GetAxisRunVel((int)enumAGD301Axis.AXS3) : (int)(spd * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS3));
            int TargetPos1 = (int)(Axis1pluse * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS1));
            int TargetPos2 = (int)(Axis2pluse * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS2));
            int TargetPos3 = (int)(Axis3pluse * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS3));
            WriteLog($"[MABS](InPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            WriteLog($"[MABS](speed,TargetPos) :R({Speed1},{TargetPos1})Y({Speed2},{TargetPos2})X({Speed3},{TargetPos3})");
            //AAMotionAPI.VectorLinear(m_MCtrl, AxisRef.A, AxisRef.A | AxisRef.B | AxisRef.C, Convert.ToInt32(Vec_Speed_textBox.Text), Convert.ToInt32(Vec_Acc_textBox.Text), Convert.ToInt32(Vec_Dec_textBox.Text), Convert.ToInt32(Vec_Dec_textBox.Text), 0, new int[] { Convert.ToInt32(Vec_A_Target_textBox.Text), Convert.ToInt32(Vec_B_Target_textBox.Text), Convert.ToInt32(Vec_C_Target_textBox.Text) });
            //m_MCtrl.A.Begin();
            lock (m_LockMCtrlComm)
            {
                SException sException = null;
                sException = CheckAxisStatus("MABS", enumAGD301Axis.AXS1);
                if (sException != null)
                    throw sException;
                sException = CheckAxisStatus("MABS", enumAGD301Axis.AXS2);
                if (sException != null)
                    throw sException;
                sException = CheckAxisStatus("MABS", enumAGD301Axis.AXS3);
                if (sException != null)
                    throw sException;
                m_MAxis[(int)enumAGD301Axis.AXS1].MoveAbs(TargetPos1, Speed1);
                if (m_MAxis[(int)enumAGD301Axis.AXS2].InTargetStat == (int)enumInPosStat.Moving)
                    throw new SException((int)enumAgitoError.Axis1IsMoving, string.Format("AXS2 is moving. [{0}]", "MABS"));
                m_MAxis[(int)enumAGD301Axis.AXS2].MoveAbs(TargetPos2, Speed2);
                if (m_MAxis[(int)enumAGD301Axis.AXS3].InTargetStat == (int)enumInPosStat.Moving)
                    throw new SException((int)enumAgitoError.Axis1IsMoving, string.Format("AXS3 is moving. [{0}]", "MABS"));
                m_MAxis[(int)enumAGD301Axis.AXS3].MoveAbs(TargetPos3, Speed3);
            }
            if (!SpinWait.SpinUntil(() =>
            {
                lock (m_LockMCtrlComm)
                {
                    SpinWait.SpinUntil(() => false, 100);
                    if (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0 ||
                        m_MAxis[(int)enumAGD301Axis.AXS2].ConFlt != 0 ||
                         m_MAxis[(int)enumAGD301Axis.AXS3].ConFlt != 0)
                    {
                        GetErr = true;
                        return true;
                    }
                    return (m_MAxis[(int)enumAGD301Axis.AXS1].InTargetStat == (int)enumInPosStat.InPos) &&
                            (m_MAxis[(int)enumAGD301Axis.AXS2].InTargetStat == (int)enumInPosStat.InPos) &&
                            (m_MAxis[(int)enumAGD301Axis.AXS3].InTargetStat == (int)enumInPosStat.InPos);
                }
            }, Motiontimeout[enumMotionTimeout.MABS]))
            {
                throw MotionTimeoutProcess("MABS", enumAGD301Axis.ALL);
            }
            WriteLog($"[MABS](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            if (GetErr)
            {
                throw MotionErrorProcess("MREL", enumAGD301Axis.ALL);
            }
            m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
            WriteLog("End Mabs");
        }
        public void AxisMabs(int nTimeout, enumAGD301Axis axis, int pluse, int spd = 0)
        {
            WriteLog("Start AxisMabs");
            //Axis1pluse input 1000 = 1degree
            //Axis2pluse input 1000 = 1mm
            //Axis3pluse input 1000 = 1mm
            if ((int)axis > (int)enumAGD301Axis.AXS3)
                throw new SException((int)enumAgitoError.InputAxisNoError, string.Format(axis.ToString() + "Input AXISNo abnormal. [{1}]", axis.ToString(), "MABS"));
            int Speed = 0;
            int TargetPos = 0;
            double Resolution = AgitoParm.GetAxisResolution((int)axis);
            TargetPos = (int)(pluse * Resolution);
            if (spd == 0)
                Speed = AgitoParm.GetAxisRunVel((int)axis);
            else
                Speed = (int)(spd * Resolution);
            WriteLog($"[MABS](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            WriteLog($"[MABS](TargetPos) :{m_strAxisName[(int)axis]}({TargetPos})");
            bool GetErr = false;
            lock (m_LockMCtrlComm)
            {
                SException sException = null;
                sException = CheckAxisStatus("MABS", enumAGD301Axis.AXS1);
                if (sException != null)
                    throw sException;
                m_MAxis[(int)axis].MoveAbs((int)(pluse * Resolution), Speed);
            }
            if (!SpinWait.SpinUntil(() =>
            {
                lock (m_LockMCtrlComm)
                {
                    if (m_MAxis[(int)axis].ConFlt != 0)
                    {
                        GetErr = true;
                        return true;
                    }
                    return (m_MAxis[(int)axis].InTargetStat == (int)enumInPosStat.InPos);
                }
            }, Motiontimeout[enumMotionTimeout.MABS]))
            {
                throw MotionTimeoutProcess("MABS", axis);
            }
            WriteLog($"[MABS](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            if (GetErr)
                throw MotionErrorProcess("MREL", axis);
            m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
            //m_Signals[enumAlignerSignalTable.MotionCompleted]?.Set();
            WriteLog("End AxisMabs");
        }
        public void Mrel(int nTimeout, int Axis1pluse, int Axis2pluse, int Axis3pluse, int spd = 0)
        {
            WriteLog("Start Mrel");
            SException sException = null;
            //Axis1pluse input 1000 = 1degree
            //Axis2pluse input 1000 = 1mm
            //Axis3pluse input 1000 = 1mm
            bool GetErr = false;
            int Speed1 = spd == 0 ? AgitoParm.GetAxisRunVel((int)enumAGD301Axis.AXS1) : (int)(spd * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS1));
            int Speed2 = spd == 0 ? AgitoParm.GetAxisRunVel((int)enumAGD301Axis.AXS2) : (int)(spd * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS2));
            int Speed3 = spd == 0 ? AgitoParm.GetAxisRunVel((int)enumAGD301Axis.AXS3) : (int)(spd * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS3));
            int TargetPos1 = (int)(Axis1pluse * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS1));
            int TargetPos2 = (int)(Axis2pluse * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS2));
            int TargetPos3 = (int)(Axis3pluse * AgitoParm.GetAxisResolution((int)enumAGD301Axis.AXS3));
            WriteLog($"[MREL](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            WriteLog($"[MREL](speed,TargetPos) :R({Speed1},{TargetPos1})Y({Speed2},{TargetPos2})X({Speed3},{TargetPos3})");
            //AAMotionAPI.VectorLinear(m_MCtrl, AxisRef.A, AxisRef.A | AxisRef.B | AxisRef.C, Convert.ToInt32(Vec_Speed_textBox.Text), Convert.ToInt32(Vec_Acc_textBox.Text), Convert.ToInt32(Vec_Dec_textBox.Text), Convert.ToInt32(Vec_Dec_textBox.Text), 0, new int[] { Convert.ToInt32(Vec_A_Target_textBox.Text), Convert.ToInt32(Vec_B_Target_textBox.Text), Convert.ToInt32(Vec_C_Target_textBox.Text) });
            //m_MCtrl.A.Begin();
            lock (m_LockMCtrlComm)
            {
                sException = CheckAxisStatus("MREL", enumAGD301Axis.AXS1);
                if (sException != null)
                    throw sException;
                sException = CheckAxisStatus("MREL", enumAGD301Axis.AXS2);
                if (sException != null)
                    throw sException;
                sException = CheckAxisStatus("MREL", enumAGD301Axis.AXS3);
                if (sException != null)
                    throw sException;
                m_MAxis[(int)enumAGD301Axis.AXS1].MoveRel(TargetPos1, Speed1);
                if (m_MAxis[(int)enumAGD301Axis.AXS2].InTargetStat == (int)enumInPosStat.Moving)
                    throw new SException((int)enumAgitoError.Axis2IsMoving, string.Format("Axis2 is moving. [{0}]", "MABS"));
                m_MAxis[(int)enumAGD301Axis.AXS2].MoveRel(TargetPos2, Speed2);
                if (m_MAxis[(int)enumAGD301Axis.AXS3].InTargetStat == (int)enumInPosStat.Moving)
                    throw new SException((int)enumAgitoError.Axis3IsMoving, string.Format("Axis3 is moving. [{0}]", "MABS"));
                m_MAxis[(int)enumAGD301Axis.AXS3].MoveRel(TargetPos3, Speed3);
            }
            if (!SpinWait.SpinUntil(() =>
            {
                lock (m_LockMCtrlComm)
                {
                    if (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0 ||
                    m_MAxis[(int)enumAGD301Axis.AXS2].ConFlt != 0 ||
                    m_MAxis[(int)enumAGD301Axis.AXS3].ConFlt != 0
                    )
                    {
                        GetErr = true;
                        return true;
                    }
                    return (m_MAxis[(int)enumAGD301Axis.AXS1].InTargetStat == (int)enumInPosStat.InPos) &&
                            (m_MAxis[(int)enumAGD301Axis.AXS2].InTargetStat == (int)enumInPosStat.InPos) &&
                            (m_MAxis[(int)enumAGD301Axis.AXS3].InTargetStat == (int)enumInPosStat.InPos);
                }
            }, Motiontimeout[enumMotionTimeout.MREL]))
                 throw MotionTimeoutProcess("MREL", enumAGD301Axis.ALL);
            WriteLog($"[MREL](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            if (GetErr)
                throw MotionErrorProcess("MREL", enumAGD301Axis.ALL);
            m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
            WriteLog("End Mrel");
        }
        public void AxisMrel(int nTimeout, enumAGD301Axis axis, int pluse, int spd = 0)
        {
            WriteLog("Start AxisMrel");
            //Axis1pluse input 1000 = 1degree
            //Axis2pluse input 1000 = 1mm
            //Axis3pluse input 1000 = 1mm
            
            if ((int)axis > (int)enumAGD301Axis.AXS3)
                throw new SException((int)enumAgitoError.InputAxisNoError, string.Format("AXS{0} Input AXISNo abnormal. [{1}]", axis.ToString(), "MREL"));
            bool GetErr = false;
            int Speed = 0;
            int TargetPos = 0;
            double Resolution = AgitoParm.GetAxisResolution((int)axis);
            TargetPos = (int)(pluse * Resolution);
            WriteLog($"[MREL](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            WriteLog($"[MREL](TargetPos) :{m_strAxisName[(int)axis]}({TargetPos})");
            if (spd == 0)
                Speed = AgitoParm.GetAxisRunVel((int)axis);
            else
                Speed = (int)(spd * Resolution);
            lock (m_LockMCtrlComm)
            {
                SException sException = null;
                sException = CheckAxisStatus("MREL", axis);
                if (sException != null)
                    throw sException;
                m_MAxis[(int)axis].MoveAbs((int)(pluse * Resolution), Speed);
            }
            if (!SpinWait.SpinUntil(() =>
            {
                lock (m_LockMCtrlComm)
                {
                    if (m_MAxis[(int)axis].ConFlt != 0)
                    {
                        GetErr = true;
                        return true;
                    }
                    return (m_MAxis[(int)axis].InTargetStat == (int)enumInPosStat.InPos);
                }
            }, Motiontimeout[enumMotionTimeout.MREL]))
                throw MotionTimeoutProcess("MREL", axis);
            WriteLog($"[MREL](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            if (GetErr)
                throw MotionErrorProcess("MREL", axis);
            m_ThisSignals[enumAlignerSignalTable.MotionCompleted]?.Set();
            WriteLog("End AxisMrel");
        }
        public void Reset(int nTimeout, int nReset = 0)
        {
            WriteLog("Start Reset");
            string Result;
            lock (m_LockMCtrlComm)
            {
                if (m_MCtrl.IsConnected == true)
                {
                    if (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0)
                        m_MCtrl.SendCommandString("BConFlt=0", out Result);
                    if (m_MAxis[(int)enumAGD301Axis.AXS2].ConFlt != 0)
                        m_MCtrl.SendCommandString("AConFlt=0", out Result);
                    if (m_MAxis[(int)enumAGD301Axis.AXS3].ConFlt != 0)
                        m_MCtrl.SendCommandString("CConFlt=0", out Result);
                }
                m_dictErroringList.Clear();
            }
            WriteLog("End Reset");
        }
        public void Stop(int nTimeout)
        {
            WriteLog("Start Stop");
            lock (m_LockMCtrlComm)
            {
                m_MAxis[(int)enumAGD301Axis.AXS1].Stop();
                m_MAxis[(int)enumAGD301Axis.AXS2].Stop();
                m_MAxis[(int)enumAGD301Axis.AXS3].Stop();
            }
            WriteLog("End Stop");
        }
        public void Sspd(int nTimeout, int nSpeed)
        {

        }
        public void Exct(int nTimeout, int nVariable)
        {
            WriteLog("Start Exct");
            lock (m_LockMCtrlComm)
            {
                if (nVariable == 1)
                {
                    m_MAxis[(int)enumAGD301Axis.AXS1].MotorOn = 1;
                    m_MAxis[(int)enumAGD301Axis.AXS2].MotorOn = 1;
                    m_MAxis[(int)enumAGD301Axis.AXS3].MotorOn = 1;
                }
                else
                {
                    m_MAxis[(int)enumAGD301Axis.AXS1].MotorOn = 0;
                    m_MAxis[(int)enumAGD301Axis.AXS2].MotorOn = 0;
                    m_MAxis[(int)enumAGD301Axis.AXS3].MotorOn = 0;
                }
            }
            WriteLog("End Exct");
        }

        public void SetOutput(int bit, bool OnOff)
        {
            WriteLog($"Set Output{bit}:{OnOff}");
            lock (m_LockMCtrlComm)
            {
                if (OnOff)
                    m_MCtrl.IO.DOutSetBit(bit);
                else
                    m_MCtrl.IO.DOutClearBit(bit);
            }
        }
        public bool GetOutput(int bit)
        {
            lock (m_LockMCtrlComm)
            {
                m_iOutput = m_MCtrl.IO.DOutPort;
                return (m_iOutput & (0x1 << bit)) > 0;
            }
        }
        public bool GetInput(int bit)
        {
            lock (m_LockMCtrlComm)
            {
                //int iInput = m_MCtrl.IO.DInPort;
                BitArray b_Input = new BitArray(new int[] { m_iInput });
                return b_Input[bit];
            }
        }

        public void Init()
        {
            WriteLog("Start Init");
            lock (m_LockMCtrlComm)
            {
                if (m_MCtrl.IsConnected == false)
                {
                    if (!m_MCtrl.Connect(m_sIP))
                        throw new SException((int)enumAgitoError.InitialFailure, string.Format("Controller connection failed. [{0}]", "INIT"));
                    SpinWait.SpinUntil(() => false, 1000);
                }
                if (m_MAxis[0].ConFlt != 0 || m_MAxis[1].ConFlt != 0 || m_MAxis[2].ConFlt != 0)
                {
                    foreach (KeyValuePair<int, string> item in m_dictErroringList)
                        WriteLog(item.Value);
                    throw new SException((int)enumAgitoError.InitialFailure, string.Format("Controller is in error status. [{0}]", "INIT"));
                }
                if (m_MAxis[0].IsCommutated() == false)
                    throw new SException((int)enumAgitoError.Axis1AutoPhaseError, string.Format("Axis1 auto-phase is failed. [{0}]", "INIT"));
                if (m_MAxis[1].IsCommutated() == false)
                    throw new SException((int)enumAgitoError.Axis2AutoPhaseError, string.Format("Axis2 auto-phase is failed. [{0}]", "INIT"));
                if (m_MAxis[2].IsCommutated() == false)
                    throw new SException((int)enumAgitoError.Axis3AutoPhaseError, string.Format("Axis3 auto-phase is failed. [{0}]", "INIT"));

                _exePolling.Set();
            }
            m_dictErroringList.Clear();
            SetControllerAxisParameter();
            m_ThisSignals[enumAlignerSignalTable.MotionCompleted].Set();


            Connected = m_MCtrl.IsConnected;
            WriteLog("End Init");
            //Check controller error
        }

        private void SetControllerAxisParameter()
        {
            int AxisCount = AgitoParm.GetAxisCount();
            for (int i = 0; i < AxisCount; i++)
            {
                lock (m_LockMCtrlComm)
                {
                    m_MAxis[i].Speed = AgitoParm.GetAxisRunVel(i);
                    m_MAxis[i].Accel = AgitoParm.GetAxisRunAcc(i);
                    m_MAxis[i].Decel = AgitoParm.GetAxisRunDec(i);
                }
            }
        }

        #region =========================== OnOccurError =======================================
        //  發生STAT異常
        protected void SendAlmMsg(string strCode)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);

        }
        //  解除STAT異常
        protected void RestAlmMsg(string strCode)
        {
            WriteLog(string.Format("Rest stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);

        }
        //  Cancel Code
        protected void SendCancelMsg(string strCode)
        {
            WriteLog(string.Format("Occur cancel Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
        }
        //  Custom Error
        protected void SendAlmMsg(enumAgitoError eAlarm)
        {
            WriteLog(string.Format("Occur eAlarm Error : {0}", eAlarm));
            int nCode = (int)eAlarm;
        }
        #endregion =================================================================
        public void ResetInPos()
        {
            m_ThisSignals[enumAlignerSignalTable.MotionCompleted].Reset();
            m_eStatInPos = enumAlignerStatus.Moving;
        }
        public void WaitInPos(int nTimeout)
        {
            SpinWait.SpinUntil(() => false, 200);
            if (!m_bSimulate)
            {
                if (!m_ThisSignals[enumAlignerSignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    throw new SException((int)enumAgitoError.Axis123MovingTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (m_ThisSignals[enumAlignerSignalTable.MotionCompleted].bAbnormalTerminal)
                {
                    throw new SException((int)enumAgitoError.Axis123MovingError, string.Format("Motion is abnormal end."));
                }
                m_eStatInPos = enumAlignerStatus.InPos;
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
                m_eStatInPos = enumAlignerStatus.InPos;
            }
        }
        private SException MotionTimeoutProcess(string CmdType, enumAGD301Axis Axis)
        {
            int AgitoError = (int)enumAgitoError.Axis123MovingTimeout;
            string ErrorMsg = string.Format("AXS123 {0} timeout. [{0}]", CmdType, CmdType);
            switch (Axis)
            {
                case enumAGD301Axis.AXS1:
                    if (m_MAxis[(int)enumAGD301Axis.AXS1].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS1 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis1MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop {m_strAxisName[(int)Axis]}");
                    m_MAxis[(int)enumAGD301Axis.AXS1].Abort();
                    SpinWait.SpinUntil(() => false, 500);
                    break;
                case enumAGD301Axis.AXS2:
                    if (m_MAxis[(int)enumAGD301Axis.AXS2].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS2 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis2MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop {m_strAxisName[(int)Axis]}");
                    m_MAxis[(int)enumAGD301Axis.AXS2].Abort();
                    SpinWait.SpinUntil(() => false, 500);
                    break;
                case enumAGD301Axis.AXS3:
                    if (m_MAxis[(int)enumAGD301Axis.AXS3].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS3 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis3MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop {m_strAxisName[(int)Axis]}");
                    m_MAxis[(int)enumAGD301Axis.AXS3].Abort();
                    SpinWait.SpinUntil(() => false, 500);
                    break;
                case enumAGD301Axis.ALL:
                    if (m_MAxis[(int)enumAGD301Axis.AXS1].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS1 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis1MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    SpinWait.SpinUntil(() => false, 100);
                    if (m_MAxis[(int)enumAGD301Axis.AXS2].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS2 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis2MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    SpinWait.SpinUntil(() => false, 100);
                    if (m_MAxis[(int)enumAGD301Axis.AXS3].InTargetStat != (int)enumInPosStat.InPos)
                    {
                        ErrorMsg = string.Format("AXS3 {0} timeout. [{0}]", CmdType, CmdType);
                        AgitoError = (int)enumAgitoError.Axis3MovingTimeout;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop all");
                    Stop(3000);
                    SpinWait.SpinUntil(() => false, 500);
                    break;
            }
            WriteLog($"[{CmdType}](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            return new SException(AgitoError, ErrorMsg);
        }
        private SException MotionErrorProcess(string CmdType, enumAGD301Axis Axis)
        {
            int AgitoError = (int)enumAgitoError.Axis123MovingControllerError + ((int)Axis + 1);
            string ErrorMsg = string.Format($"{Axis.ToString()} Controller error. [{CmdType}]");
            switch (Axis)
            {
                case enumAGD301Axis.AXS1:
                    if (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0)
                    {
                        ErrorMsg = string.Format($"{Axis.ToString()} Controller error. [{CmdType}]");
                        AgitoError = (int)enumAgitoError.Axis1MovingControllerError;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop {m_strAxisName[(int)Axis]}");
                    m_MAxis[(int)enumAGD301Axis.AXS1].Abort();
                    SpinWait.SpinUntil(() => false, 500);
                    break;
                case enumAGD301Axis.AXS2:
                    if (m_MAxis[(int)enumAGD301Axis.AXS2].ConFlt != 0)
                    {
                        ErrorMsg = string.Format($"{Axis.ToString()} Controller error. [{CmdType}]");
                        AgitoError = (int)enumAgitoError.Axis2MovingControllerError;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop {m_strAxisName[(int)Axis]}");
                    m_MAxis[(int)enumAGD301Axis.AXS2].Abort();
                    SpinWait.SpinUntil(() => false, 500);
                    break;
                case enumAGD301Axis.AXS3:
                    if (m_MAxis[(int)enumAGD301Axis.AXS3].ConFlt != 0)
                    {
                        ErrorMsg = string.Format($"{Axis.ToString()} Controller error. [{CmdType}]");
                        AgitoError = (int)enumAgitoError.Axis3MovingControllerError;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop {m_strAxisName[(int)Axis]}");
                    m_MAxis[(int)enumAGD301Axis.AXS3].Abort();
                    SpinWait.SpinUntil(() => false, 500);
                    break;
                case enumAGD301Axis.ALL:
                    if (m_MAxis[(int)enumAGD301Axis.AXS1].ConFlt != 0)
                    {
                        ErrorMsg = string.Format($"AXS1 Controller error. [{CmdType}]");
                        AgitoError = (int)enumAgitoError.Axis1MovingControllerError;
                        WriteLog(ErrorMsg);
                    }
                    SpinWait.SpinUntil(() => false, 100);
                    if (m_MAxis[(int)enumAGD301Axis.AXS2].ConFlt != 0)
                    {
                        ErrorMsg = string.Format($"AXS2 Controller error. [{CmdType}]");
                        AgitoError = (int)enumAgitoError.Axis2MovingControllerError;
                        WriteLog(ErrorMsg);
                    }
                    SpinWait.SpinUntil(() => false, 100);
                    if (m_MAxis[(int)enumAGD301Axis.AXS3].ConFlt != 0)
                    {
                        ErrorMsg = string.Format($"AXS3 Controller error. [{CmdType}]");
                        AgitoError = (int)enumAgitoError.Axis3MovingControllerError;
                        WriteLog(ErrorMsg);
                    }
                    WriteLog($"[{CmdType}] Stop all");
                    Stop(3000);
                    SpinWait.SpinUntil(() => false, 500);
                    break;
            }
            WriteLog($"[{CmdType}](CurrentPos) :R({m_MAxis[(int)enumAGD301Axis.AXS1].Pos})Y({m_MAxis[(int)enumAGD301Axis.AXS2].Pos})X({m_MAxis[(int)enumAGD301Axis.AXS3].Pos})");
            return new SException(AgitoError, ErrorMsg);
        }

        private SException CheckAxisStatus(string CmdType, enumAGD301Axis Axis)
        {
            if (m_MAxis[(int)Axis].InTargetStat == (int)enumInPosStat.Moving)
                return new SException((int)(enumAgitoError.Axis1IsMoving+(int)Axis), $"Can't move. {m_strAxisName[(int)Axis]} is moving. [{CmdType}]");
            if (m_MAxis[(int)Axis].ConFlt != 0)
                return new SException((int)(enumAgitoError.Axis1MovingControllerError + (int)Axis), string.Format($"{m_strAxisName[(int)Axis]} Controller error. [{CmdType}]"));
            return null;
        }
        ~SSAGD301_Motion()
        {
            if (m_MCtrl.IsConnected)
                m_MCtrl.Disconnect();
            //m_MCtrlComm.CloseAACommServer();
            m_MCtrl.CloseAACommServer();
            CommAPI MCtrlComm = new CommAPI();
            MCtrlComm.CloseAACommServer(true);
        }
        private void Wait(int msTime)
        {
            SpinWait.SpinUntil(() => false, msTime);
        }
    }
}
