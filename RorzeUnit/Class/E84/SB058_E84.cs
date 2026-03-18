using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.E84.Enum;
using RorzeUnit.Class.E84.Event;
using RorzeUnit.Class.RC500.Event;
using RorzeUnit.Event;

using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RorzeUnit.Class.E84
{

    public delegate void AutoProcessingEventHandler(object sender, SB058_E84 Manual);

    public class SB058_E84 : I_E84
    {
        public dlgb_v dlgAreaTrigger { get; set; }
        public bool AreaTrigger { get { return (dlgAreaTrigger != null && dlgAreaTrigger()); } }

        public int BodyNo { get; private set; }// 對應到Loadport body no
        public bool Simulate { get; private set; }

        private bool m_Disable = false;
        public bool Disable { get { return m_Disable || _DIOControl.Disable; } }

        private bool m_bAutoMode = false;
        public bool GetAutoMode { get { return m_bAutoMode; } }

        public int HCLID { get; private set; }// 對應到550 IO 位置 400/402/404/406

        private I_RC5X0_IO _DIOControl;//RC550

        private bool m_bResetFlag = false;
        public bool ResetFlag { get { return m_bResetFlag; } set { m_bResetFlag = value; } }

        private DateTime[] m_tmrTP = new DateTime[5];
        public DateTime[] TmrTP { get { return m_tmrTP; } set { m_tmrTP = value; } }

        private DateTime m_tmrTD;
        public DateTime TmrTD { get { return m_tmrTD; } set { m_tmrTD = value; } }

        private enumE84Step eStep = enumE84Step.Ready;
        public enumE84Step E84Step
        {
            get { return eStep; }
            set
            {
                if (eStep != value)
                {
                    //狀態改變 新狀態是異常要處理:OccurAlarm
                    switch (value)
                    {
                        case enumE84Step.TimeoutTD:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.TD0_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp1:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.TP1_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp2:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.TP2_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp3:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.TP3_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp4:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.TP4_TimeOut);
                            break;
                        case enumE84Step.TimeoutTp5:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.TP5_TimeOut);
                            break;
                        case enumE84Step.SignalError:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.SignalError);
                            break;
                        case enumE84Step.StageBusy:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.StageIsBusy);
                            break;
                        case enumE84Step.LightCurtain:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.LightCurtain);
                            break;
                        case enumE84Step.LightCurtainBusyOn:
                            if (isSetAvbl) SetAvbl(false);
                            SendAlmMsg(enumE84Warning.LightCurtainBusyOn);
                            break;
                    }

                    //改變狀態 舊狀態是異常:RestAlarm
                    switch (eStep)
                    {
                        case enumE84Step.TimeoutTD: RestAlmMsg(enumE84Warning.TD0_TimeOut); break;
                        case enumE84Step.TimeoutTp1: RestAlmMsg(enumE84Warning.TP1_TimeOut); break;
                        case enumE84Step.TimeoutTp2: RestAlmMsg(enumE84Warning.TP2_TimeOut); break;
                        case enumE84Step.TimeoutTp3: RestAlmMsg(enumE84Warning.TP3_TimeOut); break;
                        case enumE84Step.TimeoutTp4: RestAlmMsg(enumE84Warning.TP4_TimeOut); break;
                        case enumE84Step.TimeoutTp5: RestAlmMsg(enumE84Warning.TP5_TimeOut); break;
                        case enumE84Step.SignalError: RestAlmMsg(enumE84Warning.SignalError); break;
                        case enumE84Step.StageBusy: RestAlmMsg(enumE84Warning.StageIsBusy); break;
                        case enumE84Step.LightCurtain: RestAlmMsg(enumE84Warning.LightCurtain); break;
                        case enumE84Step.LightCurtainBusyOn: RestAlmMsg(enumE84Warning.LightCurtainBusyOn); break;
                    }

                    eStep = value;
                }
            }
        }

        private enumE84Proc m_E84_Proc = enumE84Proc.Loading;
        public enumE84Proc E84_Proc { get { return m_E84_Proc; } set { m_E84_Proc = value; } }

        private int m_nTimeLimitTD = 2;

        private int[] m_nTpTime = new int[] { 2, 2, 60, 60, 2 };
        public int[] SetTpTime { set { m_nTpTime = value; } }


        private SPollingThread _pollingAuto = null;//自動流程

        public event E84ModeChangeEventHandler OnAceessModeChange;  // Auto/Manual chage     
        public event OccurErrorEventHandler OnOccurError;           // Error chage
        public event OccurErrorEventHandler OnOccurErrorRest;       // Error chage  
        public event EventHandler OnOccurE84InIOChange;             // 收到RC550 GDIO判斷E84IO改變
        public event AutoProcessingEventHandler DoAutoProcessing;   // AutoProcess

        public SLogger _e84Log;

        public SB058_E84(int nBodyNo, I_RC5X0_IO DIO, bool bDisable, int nHCLID, bool bSimulate)
        {
            BodyNo = nBodyNo;
            m_Disable = bDisable;
            _DIOControl = DIO;
            HCLID = nHCLID;
            Simulate = bSimulate;

            for (int i = 0; i < 5; i++)
                m_tmrTP[i] = DateTime.Now;
            m_tmrTD = DateTime.Now;

            _pollingAuto = new SPollingThread(1);
            _pollingAuto.DoPolling += threadPolling_DoPolling;
            _pollingAuto.Set();

            _DIOControl.OnNotifyEvntGDIO += _DIOControl_NotifyEvntGDIO;//GDIO change

            if (m_Disable == false) _e84Log = SLogger.GetLogger("E84Log");
        }

        private void _DIOControl_NotifyEvntGDIO(object sender, RC500.Event.NotifyGDIOEventArgs e)
        {
            if (e.HCLID == HCLID || e.HCLID == HCLID + 1)
            {
                OnOccurE84InIOChange?.Invoke(this, new EventArgs());
            }
        }
        public void Close()
        {
            _pollingAuto.Close();
        }
        private void threadPolling_DoPolling()
        {
            try
            {
                if (DoAutoProcessing != null) DoAutoProcessing(this, this);
            }
            //catch (SException alarm)
            //{
            //    //  _threadAlarmMgr[alarm.ErrorID].Set();
            //    _pollingAuto.Reset(); //停止自動流程
            //                          //  ModeChange(false); //切手動                
            //}
            catch (Exception ex)
            {
                //  logger.WriteLog(ex);
                _pollingAuto.Reset();
                //  ModeChange(false); //切手動
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public bool ResetError()
        {
            bool bSignalOn = (isCs0On || isValidOn || isCs1On || isAvblOn
                    || isTrReqOn || isBusyOn || isComptOn || isContOn);

            if (bSignalOn)
            {
                return false;
            }
            else
            {
                m_bResetFlag = true;
                return true;
            }
        }
        public void ClearSignal()
        {
            _e84Log.WriteLog("[STG{0}]:   clear signal", BodyNo);
            _DIOControl.SdouW(HCLID, 0, 8);//ES-ON

            //SetLReq(false);
            //SetUReq(false);
            //SetVa(false);
            //SetReady(false);

            //SetVs0(false);
            //SetVs1(false);
            //SetAvbl(false);
            //SetEs(true);
        }
        public bool SetAutoMode(bool bOn)
        {
            bool bSucc = false;

            if (bOn != m_bAutoMode)
            {
                if (false == bOn)//Manual
                {
                    if (isCs0On || isCs1On || isValidOn || isBusyOn || isComptOn)
                    {
                        _e84Log.WriteLog("[STG{0}]:   signal on, cannot switch to manual mode", BodyNo);
                    }
                    else
                    {
                        m_bAutoMode = false;//注意先切狀態
                        ClearSignal();//才能清除訊號

                        _e84Log.WriteLog("[STG{0}]:   switch to Manual mode", BodyNo);
                        bSucc = true;
                        OnAceessModeChange?.Invoke(this, new E84ModeChangeEventArgs(m_bAutoMode));

                    }
                }
                else//Auto
                {
                    ClearSignal();
                    //SetAvbl(true);改到流程判斷可以開始
                    m_bAutoMode = true;

                    _e84Log.WriteLog("[STG{0}]:   switch to Auto mode", BodyNo);
                    bSucc = true;
                    OnAceessModeChange?.Invoke(this, new E84ModeChangeEventArgs(m_bAutoMode));
                }
            }
            else
            {
                bSucc = true;
            }
            return bSucc;
        }

        #region =========================== 計時器，計算 tp timeout
        private bool isTpTimeout(enumTpTime nTp)
        {
            //int nIdx = nTp - 1;
            DateTime dt = DateTime.Now;
            double dDuration = (dt - m_tmrTP[(int)(nTp)]).TotalMilliseconds;
            double dLimit = 1000.0d * m_nTpTime[(int)nTp];

            bool bTimeout = (dLimit <= dDuration);
            if (bTimeout)
            {
                _e84Log.WriteLog("[STG{0}]:  {1} timeout! Limit:{2}", BodyNo, nTp, dLimit);
            }
            return bTimeout;
        }
        public bool isTimeoutTD()
        {
            DateTime dt = DateTime.Now;
            double dDuration = (dt - m_tmrTD).TotalMilliseconds;
            double dLimit = 1000.0d * m_nTimeLimitTD;
            bool bTimeout = (dLimit <= dDuration);
            if (bTimeout)
            {
                _e84Log.WriteLog("[STG{0}]:  TD timeout! Limit:{1}", BodyNo, dLimit);
            }
            return bTimeout;
        }
        public bool isTimeoutTP1()
        {
            return isTpTimeout(enumTpTime.TP1);
        }
        public bool isTimeoutTP2()
        {
            return isTpTimeout(enumTpTime.TP2);
        }
        public bool isTimeoutTP3()
        {
            return isTpTimeout(enumTpTime.TP3);
        }
        public bool isTimeoutTP4()
        {
            return isTpTimeout(enumTpTime.TP4);
        }
        public bool isTimeoutTP5()
        {
            return isTpTimeout(enumTpTime.TP5);
        }
        #endregion

        #region IO input output get
        public bool isValidOn { get { return _DIOControl.GetGDIO_InputStatus(HCLID, 0); } }
        public bool isCs0On { get { return _DIOControl.GetGDIO_InputStatus(HCLID, 1); } }
        public bool isCs1On { get { return _DIOControl.GetGDIO_InputStatus(HCLID, 2); } }
        public bool isAvblOn { get { return _DIOControl.GetGDIO_InputStatus(HCLID, 3); } }

        public bool isTrReqOn { get { return _DIOControl.GetGDIO_InputStatus(HCLID, 4); } }
        public bool isBusyOn { get { return _DIOControl.GetGDIO_InputStatus(HCLID, 5); } }
        public bool isComptOn { get { return _DIOControl.GetGDIO_InputStatus(HCLID, 6); } }
        public bool isContOn { get { return _DIOControl.GetGDIO_InputStatus(HCLID, 7); } }

        public bool isSetLReq { get { return _DIOControl.GetGDIO_OutputStatus(HCLID, 0); } }
        public bool isSetUReq { get { return _DIOControl.GetGDIO_OutputStatus(HCLID, 1); } }
        public bool isSetVa { get { return _DIOControl.GetGDIO_OutputStatus(HCLID, 2); } }
        public bool isSetReady { get { return _DIOControl.GetGDIO_OutputStatus(HCLID, 3); } }
        public bool isSetVs0 { get { return _DIOControl.GetGDIO_OutputStatus(HCLID + 1, 0); } }
        public bool isSetVs1 { get { return _DIOControl.GetGDIO_OutputStatus(HCLID + 1, 1); } }
        public bool isSetAvbl 
        { 
            get 
            { 
                return _DIOControl.GetGDIO_OutputStatus(HCLID + 1, 2); 
            } 
        }
        public bool isSetEs { get { return _DIOControl.GetGDIO_OutputStatus(HCLID + 1, 3); } }
        #endregion
        #region IO input simulate set
        public void SetValidOn(bool bTurn)//Simulate
        {
            if (Simulate == false) return;
            _DIOControl.SetGDIO_InputStatus(HCLID, 0, bTurn);
        }
        public void SetCs0On(bool bTurn)//Simulate
        {
            if (Simulate == false) return;
            _DIOControl.SetGDIO_InputStatus(HCLID, 1, bTurn);
        }
        public void SetCs1On(bool bTurn)//Simulate
        {
            if (Simulate == false) return;
            _DIOControl.SetGDIO_InputStatus(HCLID, 2, bTurn);
        }
        public void SetAvblOn(bool bTurn)//Simulate
        {
            if (Simulate == false) return;
            _DIOControl.SetGDIO_InputStatus(HCLID, 3, bTurn);
        }
        public void SetTrReqOn(bool bTurn)//Simulate
        {
            if (Simulate == false) return;
            _DIOControl.SetGDIO_InputStatus(HCLID, 4, bTurn);
        }
        public void SetBusyOn(bool bTurn)//Simulate
        {
            if (Simulate == false) return;
            _DIOControl.SetGDIO_InputStatus(HCLID, 5, bTurn);
        }
        public void SetComptOn(bool bTurn)//Simulate
        {
            if (Simulate == false) return;
            _DIOControl.SetGDIO_InputStatus(HCLID, 6, bTurn);
        }
        public void SetContOn(bool bTurn)//Simulate
        {
            if (Simulate == false) return;
            _DIOControl.SetGDIO_InputStatus(HCLID, 7, bTurn);
        }
        #endregion
        #region IO output set
        public void SetLReq(bool bOn)
        {
            if (isSetLReq != bOn)
            {
                if (bOn)
                    _e84Log.WriteLog("[STG{0}]:   Set L_Req ON", BodyNo);
                else
                    _e84Log.WriteLog("[STG{0}]:   Set L_Req OFF", BodyNo);
            }
            _DIOControl.SdobW(HCLID, 0, bOn);
        }
        public void SetUReq(bool bOn)
        {
            if (isSetUReq != bOn)
            {
                if (bOn)
                    _e84Log.WriteLog("[STG{0}]:   Set U_Req ON", BodyNo);
                else
                    _e84Log.WriteLog("[STG{0}]:   Set U_Req OFF", BodyNo);
            }
            _DIOControl.SdobW(HCLID, 1, bOn);
        }
        public void SetVa(bool bOn)
        {
            if (isSetVa != bOn)
            {
                if (bOn)
                    _e84Log.WriteLog("[STG{0}]:   Set VA ON", BodyNo);
                else
                    _e84Log.WriteLog("[STG{0}]:   Set VA OFF", BodyNo);
            }
            _DIOControl.SdobW(HCLID, 2, bOn);
        }
        public void SetReady(bool bOn)
        {
            if (isSetReady != bOn)
            {
                if (bOn)
                    _e84Log.WriteLog("[STG{0}]:   Set Ready ON", BodyNo);
                else
                    _e84Log.WriteLog("[STG{0}]:   Set Ready OFF", BodyNo);
            }
            _DIOControl.SdobW(HCLID, 3, bOn);
        }

        public void SetVs0(bool bOn)
        {
            if (isSetVs0 != bOn)
            {
                if (bOn)
                    _e84Log.WriteLog("[STG{0}]:   Set VS0 ON", BodyNo);
                else
                    _e84Log.WriteLog("[STG{0}]:   Set VS0 OFF", BodyNo);
            }
            _DIOControl.SdobW(HCLID + 1, 0, bOn);
        }
        public void SetVs1(bool bOn)
        {
            if (isSetVs1 != bOn)
            {
                if (bOn)
                    _e84Log.WriteLog("[STG{0}]:   Set VS1 ON", BodyNo);
                else
                    _e84Log.WriteLog("[STG{0}]:   Set VS1 OFF", BodyNo);
            }
            _DIOControl.SdobW(HCLID + 1, 1, bOn);
        }
        public void SetAvbl(bool bOn)
        {
            if (isSetAvbl != bOn)
            {
                //m_bSetAvbl = bOn;
                if (bOn)
                    _e84Log.WriteLog("[STG{0}]:   Set AVBL ON", BodyNo);
                else
                    _e84Log.WriteLog("[STG{0}]:   Set AVBL OFF", BodyNo);
            }
            _DIOControl.SdobW(HCLID + 1, 2, bOn);
        }
        public void SetEs(bool bOn)
        {
            if (isSetEs != bOn)
            {
                //m_bSetEs = bOn;
                if (bOn)
                    _e84Log.WriteLog("[STG{0}]:   Set Es ON", BodyNo);
                else
                    _e84Log.WriteLog("[STG{0}]:   Set Es OFF", BodyNo);
            }
            _DIOControl.SdobW(HCLID + 1, 3, bOn);
        }
        #endregion

        #region =========================== OnOccurError ===========================     
        //  發生E84異常
        private void SendAlmMsg(string strCode)
        {
            if (strCode.Length != 4) return;
            //  STG1 15 13 00000
            //  STG2 16 13 00000
            //  STG3 17 13 00000
            //  STG4 18 13 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (15 + BodyNo - 1) * 10000000 + 13 * 100000*/;
            OnOccurError?.Invoke(this, new OccurErrorEventArgs(nCode));

        }
        //  解除E84異常
        private void RestAlmMsg(string strCode)
        {
            if (strCode.Length != 4) return;
            //  STG1 15 13 00000
            //  STG2 16 13 00000
            //  STG3 17 13 00000
            //  STG4 18 13 00000
            int nCode = Convert.ToInt32(strCode, 16) /*+ (15 + BodyNo - 1) * 10000000 + 13 * 100000*/;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生自定義異常
        private void SendAlmMsg(enumE84Warning eAlarm)
        {
            //  STG1 15 13 00000
            //  STG2 16 13 00000
            //  STG3 17 13 00000
            //  STG4 18 13 00000
            //  STG5 19 13 00000
            //  STG6 20 13 00000
            //  STG7 21 13 00000
            //  STG8 22 13 00000
            int nCode = (int)eAlarm /*+ (15 + BodyNo - 1) * 10000000 + 13 * 100000*/;
            OnOccurError?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除E84異常
        private void RestAlmMsg(enumE84Warning eAlarm)
        {
            //  STG1 15 13 00000
            //  STG2 16 13 00000
            //  STG3 17 13 00000
            //  STG4 18 13 00000
            int nCode = (int)eAlarm /*+ (15 + BodyNo - 1) * 10000000 + 13 * 100000*/;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        #endregion
    }
}
