using System;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Event;
using System.Linq;
using System.IO;
using RorzeComm.Log;
using RorzeUnit.Class.RC500;
using RorzeUnit.Class.BarCode;
using RorzeComm.Threading;
using RorzeComm;
using System.Runtime.CompilerServices;

namespace RorzeUnit.Class.Loadport.Type
{
    public class SSLoadPortRC550 : SSLoadportParents
    {
        public struct IO_Identification
        {
            public IO_Identification(int body, int hcl, int bit)
            {
                Body = body;
                HCL = hcl;
                Bit = bit;
            }
            public int Body { get; }
            public int HCL { get; }
            public int Bit { get; }
        }

        #region =========================== private ============================================
        LoadPortGPIO _gpio;
        Dictionary<LoadPortGPIO.LoadPortDI, IO_Identification> m_dicDI;
        private I_RC5X0_IO m_rc550;
        private I_Robot m_robot;
        //private static readonly string csHead = "LoadPort";
        //private static readonly string csMark = "Teach";
        //private static readonly string csExtension = ".dat";
        #endregion
        #region =========================== Property ===========================================
        public override bool UseAdapter { get { return false/*_gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIAdapter]*/; } }
        public override bool IsProtrude { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIProtrusion]; } }
        public override bool IsPresenceON { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIPresence]; } }
        public override bool IsPresenceleftON { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceleft]; } }
        public override bool IsPresencerightON { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceright]; ; } }
        public override bool IsPresencemiddleON { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIPresencemiddle]; ; } }
        public override bool IsDoorOpen { get { return true/*_gpio.GetD0List[LoadPortGPIO.LoadPortDO._DODoorOpen]*/; } }
        public override bool IsUnclamp { get { return true/*_gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen]*/; } }//close 是勾住
        #endregion
        #region =========================== Event ==============================================
        public override event FoupExistChangEventHandler OnFoupExistChenge;

        public override event EventHandler<bool> OnORGNComplete;
        public override event EventHandler<bool> OnGetDataComplete;
        public override event EventHandler<LoadPortEventArgs> OnJigDockComplete;
        public override event EventHandler<LoadPortEventArgs> OnClmpComplete;
        public override event EventHandler<LoadPortEventArgs> OnClmp1Complete;
        public override event EventHandler<LoadPortEventArgs> OnUclmComplete;
        public override event EventHandler<LoadPortEventArgs> OnUclm1Complete;
        public override event EventHandler<LoadPortEventArgs> OnMappingComplete;

        public override event RorzenumLoadportIOChangelHandler OnIOChange;

        public override event MessageEventHandler OnReadData;

        // ================= Simulate =================
        public override event EventHandler OnSimulateCLMP;
        public override event EventHandler OnSimulateUCLM;
        public override event EventHandler OnSimulateMapping;
        #endregion

        public SSLoadPortRC550(I_RC5X0_IO rc550, I_Robot robot, I_E84 e84, int nBodyNo, bool bDisable, bool bSimulate, int[] nTrbMapStgNo0to399,
            string strLoadportWaferType, Dictionary<LoadPortGPIO.LoadPortDI, IO_Identification> dicDI, I_BarCode barcode)
            : base(e84, nBodyNo, bDisable, bSimulate, nTrbMapStgNo0to399, strLoadportWaferType, barcode, null)
        {
            m_rc550 = rc550;
            m_robot = robot;
            m_dicDI = dicDI;
            m_rc550.OnNotifyEvntGDIO += rc550_NotifyEvntGDIO;//這是收到GDIO
            m_rc550.OnNotifyEvntCNCT += rc550_NotifyEvntCNCT;
            OnIOChange += SSLoadPort_OnIOChange;//這是針對PL&PS改變

            _gpio = new LoadPortGPIO("000000000000000", "000000000000000");

            if (Simulate)
            {
                Connected = true;
                _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIProtrusion] = false;
            }

        }

        public override void Open() { }
        public override void Close() { }

        //==============================================================================
        #region OneThread 
        protected override void ExeINIT()
        {
            try
            {
                WriteLog("ExeINIT:Start");
                //this.InitW(m_nAckTimeout);
                SpinWait.SpinUntil(() => false, 3000);
            }
            catch (SException ex)
            { WriteLog("<<SException>> ExeINIT:" + ex); }
            catch (Exception ex)
            { WriteLog("<<SException>> ExeINIT:" + ex); }
        }
        protected override void ExeORGN()
        {
            try
            {
                WriteLog("ExeORGN:Start");

                //this.ResetChangeModeCompleted();
                this.EventW(m_nAckTimeout);
                //this.WaitChangeModeCompleted(3000);

                //this.ResetChangeModeCompleted();
                //this.InitW(m_nAckTimeout);
                //this.WaitChangeModeCompleted(3000);

                this.StimW(m_nAckTimeout);

                //this.ResetInPos();
                this.OrgnW(m_nAckTimeout);
                //this.WaitInPos(m_nMotionTimeout);

                this.ExeCheckFoupExist();// 應該要重新確認

                //if (Simulate)// INIT會把IO rest 可以正常辨識，模擬要直接指定
                {
                    StatusMachine = FoupExist ? enumStateMachine.PS_Arrived : enumStateMachine.PS_ReadyToLoad;
                    //if (FoupExist)//Close勾住
                    //{
                    //    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = true;
                    //    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = false;
                    //}
                    //else
                    //{
                    //    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = false;
                    //    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = true;
                    //}
                }
  
                _Yaxispos = enumLoadPortPos.Home;//假訊號
                _Zaxispos = enumLoadPortPos.Home;//假訊號
                OnORGNComplete?.Invoke(this, true);
                m_bRobotExtand = false;//能完成原點Robot已經縮回
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> ExeORGN:" + ex);
                OnORGNComplete?.Invoke(this, false);
                StatusMachine = enumStateMachine.PS_Error;
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeORGN:" + ex);
                OnORGNComplete?.Invoke(this, false);
                StatusMachine = enumStateMachine.PS_Error;
            }
        }
        protected override void ExeCLMP()
        {
            try
            {
                WriteLog("ExeCLMP:Start");
                if (dlgLoadInterlock != null && dlgLoadInterlock(this))
                {
                    SendAlmMsg(enumLoadPortError.InterlockStop);
                    throw new SException((int)(enumLoadPortError.InterlockStop), "Load InterlockStop");
                }

                _LoadCompletSignal.Reset();
                StatusMachine = enumStateMachine.PS_Docking;
                //if (Simulate)//close勾住
                //{
                //    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = true;//騙訊號
                //    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = false;//騙訊號
                //}
                E84Status = E84PortStates.TransferBlock;

                _Yaxispos = enumLoadPortPos.Dock;//假訊號
                _Zaxispos = enumLoadPortPos.Dock;//假訊號

                if (SpinWait.SpinUntil(() => m_robot.GetRunningPermissionForStgMap(BodyNo), 60000 * 3))
                {
                    int nStgNo0to399 = UseAdapter ? m_nTrbMapStgNo0to399[1] : m_nTrbMapStgNo0to399[0];
                    nStgNo0to399 += (int)eFoupType;
                    //搶到使用權
                    WriteLog(string.Format("robot{0} start mapping", m_robot.BodyNo));
                    m_robot.ResetInPos();
                    m_robot.WmapW(m_robot.GetAckTimeout, nStgNo0to399);
                    m_robot.WaitInPos(m_robot.GetMotionTimeout);
                    WriteLog(string.Format("robot{0} get mapping", m_robot.BodyNo));
                    m_robot.GmapW(m_robot.GetAckTimeout, nStgNo0to399);

                    MappingData = m_robot.GetMappingData;

                    //釋放使用權
                    m_robot.ReleaseRunningPermissionForStgMap(BodyNo);
                }
                else
                {
                    SendAlmMsg(enumLoadPortError.RobotIsBusy);
                    throw new SException((int)(enumLoadPortError.RobotIsBusy), "Wait for robot mapping timeout");
                }

                _LoadCompletSignal.Set();

                StatusMachine = enumStateMachine.PS_Docked;

                //if (Simulate) { AnalysisGPOS("02/02"); }

                OnClmpComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, true));

                if (_MappingData.Contains('2'))
                    SendAlmMsg(enumLoadPortError.Mapping_Thickness_Thick);
                if (_MappingData.Contains('3'))
                    SendAlmMsg(enumLoadPortError.Mapping_Cross);
                if (_MappingData.Contains('4'))
                    SendAlmMsg(enumLoadPortError.Mapping_FrontBow);
                if (_MappingData.Contains('7'))
                    SendAlmMsg(enumLoadPortError.Mapping_Double);
                if (_MappingData.Contains('8'))
                    SendAlmMsg(enumLoadPortError.Mapping_Thickness_Thin);
                if (_MappingData.Contains('9'))
                    SendAlmMsg(enumLoadPortError.Mapping_Abnormal);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> ExeCLMP:" + ex);
                _LoadCompletSignal.bAbnormalTerminal = true;
                _LoadCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
                OnClmpComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, false));
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeCLMP:" + ex);
                _LoadCompletSignal.bAbnormalTerminal = true;
                _LoadCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
            }
        }
        protected override void ExeClamp1()
        {
            try
            {
                WriteLog("ExeClamp1:Start");
                E84Status = E84PortStates.TransferBlock;
                //this.ResetInPos();
                //this.ClmpW(3000, 1);
                //this.WaitInPos(5000);

                StatusMachine = enumStateMachine.PS_Clamped;
                if (Simulate)//close勾住
                {
                    //_gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = true;//假訊號
                    //_gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = false;//假訊號
                }
                if (OnClmp1Complete != null)
                    OnClmp1Complete(this, new LoadPortEventArgs(_MappingData, BodyNo, true));
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> ExeClamp1:" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeClamp1:" + ex);
            }
        }
        protected override void ExeUCLM()
        {
            try
            {
                WriteLog("ExeUCLM:Start");
                this.UndockQueueByHost = false;
                //動作條件檢查
                if (dlgLoadInterlock != null && dlgLoadInterlock(this))
                {
                    SendAlmMsg(enumLoadPortError.InterlockStop);
                    throw new SException((int)(enumLoadPortError.InterlockStop), "Unload InterlockStop");
                }

                StatusMachine = enumStateMachine.PS_UnDocking;

                //_UnLoadCompletSignal.Reset();
                //this.ResetInPos();
                //this.UclmW(3000);
                //this.WaitInPos(20000);
                //_UnLoadCompletSignal.Set();

                StatusMachine = enumStateMachine.PS_UnDocked;
                StatusMachine = enumStateMachine.PS_UnClamped;

                _Yaxispos = enumLoadPortPos.Home;//假訊號
                _Zaxispos = enumLoadPortPos.Home;//假訊號

                if (Simulate)//close勾住
                {
                    //_gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = false;//假訊號
                    //_gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = true;//假訊號
                }

                SpinWait.SpinUntil(() => false, 1000);//故意延遲

                E84Status = E84PortStates.ReadytoUnload;
                StatusMachine = enumStateMachine.PS_ReadyToUnload;

                OnUclmComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, true));
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> ExeUCLM:" + ex);
                _UnLoadCompletSignal.bAbnormalTerminal = true;
                _UnLoadCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
                OnUclmComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, false));
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeUCLM:" + ex);
                _UnLoadCompletSignal.bAbnormalTerminal = true;
                _UnLoadCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
                OnUclmComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, false));
            }
        }
        protected override void ExeUClamp1()
        {
            try
            {
                WriteLog("ExeUClamp1:Start");

                //this.ResetInPos();
                //this.UclmW(3000, 1);
                //this.WaitInPos(5000);

                if (Simulate)//close勾住
                {
                    //_gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = false;//假訊號
                    //_gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = true;//假訊號
                }
                StatusMachine = enumStateMachine.PS_UnClamped;

                if (FoupExist)
                    StatusMachine = enumStateMachine.PS_ReadyToUnload;
                else
                    StatusMachine = enumStateMachine.PS_ReadyToLoad;

                if (OnUclm1Complete != null)
                    OnUclm1Complete(this, new LoadPortEventArgs(_MappingData, BodyNo, true));
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> ExeUClamp1:" + ex);
                StatusMachine = enumStateMachine.PS_Error;
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeUClamp1:" + ex);
                StatusMachine = enumStateMachine.PS_Error;
            }
        }
        protected override void ExeRsta(int nMode)
        {
            try
            {
                WriteLog("ExeRsta:Start");
                //this.ResetW(3000, nMode);
            }
            catch (SException ex)
            { WriteLog("<<SException>> ExeRsta:" + ex); }
            catch (Exception ex)
            { WriteLog("<<Exception>> ExeRsta:" + ex); }
        }
        protected override void ExeCheckFoupExist()
        {
            try
            {
                lock (this)
                {
                    WriteLog("ExeCheckFoupExist:Start");
                    System.Threading.SpinWait.SpinUntil(() => false, 500);
                    //have Foup 
                    if (this.IsPSPL_AllOn)
                    {
                        if (!FoupExist)
                        {
                            WriteLog("CheckFoupExist:Foup:[" + FoupID + "]Arrived");
                            StatusMachine = enumStateMachine.PS_FoupOn;

                            FoupExist = true;
                            eFoupType = enumFoupType.OCP1;     //沒有InfoPad寫死一種
                            FoupTypeName = eFoupType.ToString();//沒有InfoPad寫死一種
                            _Waferlist = new List<SWafer>();

                            StatusMachine = enumStateMachine.PS_Arrived;
                            E84Status = E84PortStates.ReadytoUnload;
                            OnFoupExistChenge?.Invoke(this, new FoupExisteChangEventArgs(FoupExist));//ReadRFID                     
                        }
                    }
                    //No Foup 
                    else if (FoupExist)
                    {
                        WriteLog("CheckFoupExist:Foup:[" + FoupID + "]Remove");
                        StatusMachine = enumStateMachine.PS_Removed;
                        FoupExist = false;
                        eFoupType = enumFoupType.OCP1;     //沒有InfoPad寫死一種
                        FoupTypeName = "----";             //沒有InfoPad寫死一種
                        _MappingData = "";//2022.07.08

                        _Waferlist = new List<SWafer>();

                        StatusMachine = enumStateMachine.PS_ReadyToLoad;
                        E84Status = E84PortStates.ReadytoLoad;
                        FoupID = string.Empty;
                        OnFoupExistChenge?.Invoke(this, new FoupExisteChangEventArgs(FoupExist));
                    }

                    WriteLog("ExeCheckFoupExist:Finish");
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> CheckFoupExist:" + ex);
            }
        }
        protected override void ExeWMAP()
        {
            try
            {
                WriteLog("ExeWMAP:Start");

                _MappingCompletSignal.Reset();

                if (SpinWait.SpinUntil(() => m_robot.GetRunningPermissionForStgMap(BodyNo), 60000 * 3))
                {
                    int nStgNo0to399 = UseAdapter ? m_nTrbMapStgNo0to399[1] : m_nTrbMapStgNo0to399[0];
                    nStgNo0to399 += (int)eFoupType;
                    //搶到使用權
                    WriteLog(string.Format("robot{0} start mapping", m_robot.BodyNo));

                    this.ResetProcessCompleted();
                    this.SspdW(m_robot.GetAckTimeout, 10);
                    this.WaitProcessCompleted(3000);

                    m_robot.ResetInPos();
                    m_robot.WmapW(m_robot.GetAckTimeout, nStgNo0to399);
                    m_robot.WaitInPos(m_robot.GetMotionTimeout);

                    WriteLog(string.Format("robot{0} get mapping", m_robot.BodyNo));

                    m_robot.GmapW(m_robot.GetAckTimeout, nStgNo0to399);

                    MappingData = m_robot.GetMappingData;

                    //釋放使用權
                    m_robot.ReleaseRunningPermissionForStgMap(BodyNo);
                }
                else
                {
                    SendAlmMsg(enumLoadPortError.InterlockStop);
                    throw new SException((int)(enumLoadPortError.InterlockStop), "Wait for robot mapping timeout");
                }

                _MappingCompletSignal.Set();

                if (OnMappingComplete != null)
                    OnMappingComplete(this, new LoadPortEventArgs(_MappingData, BodyNo, true));
            }
            catch (SException ex)
            {
                WriteLog("<<Alarm>> ExeWMAP:" + ex);
                _MappingCompletSignal.bAbnormalTerminal = true;
                _MappingCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
            }
            catch (Exception ex)
            {
                WriteLog("<<Alarm>> ExeWMAP:" + ex);
                _MappingCompletSignal.bAbnormalTerminal = true;
                _MappingCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
            }
        }
        protected override void ExeJigDock()
        {
            try
            {
                WriteLog("ExeJigDock:Start");

                _JigDockCompletSignal.Reset();

                //this.ResetInPos();
                //this.ZaxHomeW(5000, 2);
                //this.WaitInPos(8000);

                SpinWait.SpinUntil(() => false, 500);

                //this.ResetInPos();
                //this.YaxHomeW(5000, 2);
                //this.WaitInPos(8000);

                _JigDockCompletSignal.Set();

                OnJigDockComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, true));
            }
            catch (SException ex)
            {
                WriteLog("<<Alarm>> ExeJigDock:" + ex);
                _JigDockCompletSignal.bAbnormalTerminal = true;
                _JigDockCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
                OnJigDockComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, false));
            }
            catch (Exception ex)
            {
                WriteLog("<<Alarm>> ExeJigDock:" + ex);
                _JigDockCompletSignal.bAbnormalTerminal = true;
                _JigDockCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
            }
        }
        protected override void ExeGetData()
        {
            try
            {
                WriteLog("ExeGET DPRM:Start");
                /*
                //  DPRM
                for (int nDprm = 0; nDprm < _sDPRMData.Length; nDprm++)
                {
                    this.GetDprmW(5000, nDprm);

                    if (Simulate == false) _sDPRMData[nDprm] = m_strDprm;
                }
                WriteLog("ExeGET DMPR:Start");
                //  DMPR
                GetDmprW(5000);
                WriteLog("ExeGET DCST:Start");
                //  DCST
                GetDcstW(5000);
                //  GWID 問完DPRM後才找的到gwid
                //GwidW(3000);
                */
                OnGetDataComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> ExeGetData:" + ex);
                OnGetDataComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeGetData:" + ex);
                OnGetDataComplete?.Invoke(this, false);
            }
        }
        #endregion
        //==============================================================================
        private void SSLoadPort_OnIOChange(object sender, RorzenumLoadportIOChengeEventArgs e)//GPIO改變
        {
            try
            {
                if (_e84.GetAutoMode && false == _e84.isBusyOn)  // E84 Auto BusyOff 人為取放
                {
                    if (IsPSPL_AllOn && !FoupExist)
                    {
                        FoupExist = true;
                        WriteLog("E84 Auto , Foup is Detect.");
                        SendAlmMsg(enumLoadPortError.E84Auto_FOUPManualDetect);
                            //return;
                    }
                    else if (FoupExist)
                    {
                        if (e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceright] ||
                            e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresencemiddle] ||
                            e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceleft] ||
                            e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresence])
                        {
                            FoupExist = false;
                            WriteLog("E84 Auto , Foup IO have  Removed.");
                            SendAlmMsg(enumLoadPortError.E84Auto_FOUPManualRemove);
                            //return;
                        }
                    }

                    //if (FoupExist)  //20241015有問題，不使用
                    //{
                    //    有FOUP PL PS訊號消失
                    //    if (!e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceright] ||
                    //        !e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresencemiddle] ||
                    //        !e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceleft] ||
                    //        !e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresence])
                    //    {
                    //        WriteLog("E84 Auto , Foup is Remove.");
                    //        SendAlmMsg(enumLoadPortError.E84Auto_FOUPManualRemove);
                    //        return;
                    //    }
                    //}
                    //else
                    //{
                    //    無FOUP PL PS訊號出現
                    //    if (e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceright] ||
                    //        e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresencemiddle] ||
                    //        e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceleft] ||
                    //        e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresence])
                    //    {
                    //        WriteLog("E84 Auto , Foup IO have detect.");
                    //        SendAlmMsg(enumLoadPortError.E84Auto_FOUPManualDetect);
                    //        return;
                    //    }
                    //}
                }
                // --------------------------------------------------------------------------------
                if (_e84.GetAutoMode == false)
                {
                    if (IsPSPL_AllOn && !FoupExist)
                    {
                        _threadFoupExist.Set();
                    }
                    else if (FoupExist)
                    {
                        _threadFoupExist.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> LoadPortIOChange:" + ex);
            }
        }
        private void rc550_NotifyEvntGDIO(object sender, RC500.Event.NotifyGDIOEventArgs e)//RC550 GDIO改變
        {
            try
            {
                I_RC5X0_IO dio = sender as I_RC5X0_IO;

                bool bChange = false;
                string strDi = _gpio.GetDIstr;
                string strDo = _gpio.GetDOstr;
                foreach (var item in m_dicDI.ToArray())
                {
                    if (item.Value.Body != dio.BodyNo) continue;
                    if (item.Value.HCL != e.HCLID) continue;

                    // 404/00000000

                    bool bOld = _gpio.GetDIList[item.Key];
                    bool bNow = e.Input[item.Value.Bit];

                    switch (item.Key)
                    {
                        case LoadPortGPIO.LoadPortDI._DIPresenceleft:                 
                        case LoadPortGPIO.LoadPortDI._DIPresenceright:                  
                        case LoadPortGPIO.LoadPortDI._DIPresencemiddle:               
                        case LoadPortGPIO.LoadPortDI._DIPresence:
                        case LoadPortGPIO.LoadPortDI._DIProtrusion:
                            //RJ是Normal Off，電控設計Normal On，此反轉維持一致
                            bNow = !bNow;
                            break;                     
                        default:
                            break;
                    }

                    if (bOld != bNow)
                    {
                        SetStringIO(ref strDi, (int)item.Key, bNow);
                        bChange |= true;
                    }
                }

                if (bChange)
                {
                    _gpio = new LoadPortGPIO(strDi, strDo);
                    if (Simulate)
                    {
                        _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIProtrusion] = false;
                    }
                    OnIOChange?.Invoke(this, new RorzenumLoadportIOChengeEventArgs(_gpio));
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Alarm>> Loadport{0} LoadPortIOChange:" + ex);
            }
        }
        private void rc550_NotifyEvntCNCT(object sender, bool b)
        {
            try
            {
                Connected = true;
            }
            catch (Exception ex)
            {
                WriteLog("<<Alarm>> Loadport{0} rc550_NotifyEvntCNCT:" + ex);
            }
        }
        public override void SetFoupExistChenge()
        {
            _Waferlist = new List<SWafer>();
            if (OnFoupExistChenge != null)
                OnFoupExistChenge(this, new FoupExisteChangEventArgs(FoupExist));
        }
        public override void SimulateFoupOn(bool bOn)
        {
            if (Simulate == false) return;
            m_rc550.SetGDIO_InputStatus(
                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresence].HCL,
                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresence].Bit, bOn);
            m_rc550.SetGDIO_InputStatus(
                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresenceleft].HCL,
                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresenceleft].Bit, bOn);
            m_rc550.SetGDIO_InputStatus(
                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresenceright].HCL,
                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresenceright].Bit, bOn);
            m_rc550.SetGDIO_InputStatus(
                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresencemiddle].HCL,
                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresencemiddle].Bit, bOn);
        }
        //==============================================================================
        #region =========================== ORGN =======================================    
        public override void OrgnW(int nTimeout, int nVariable = 0)
        {
            _signalSubSequence.Reset();
            {
                IsKeepClamp = false;
                _Waferlist = new List<SWafer>();
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== CLMP =======================================   
        public override void ClmpW(int nTimeout, int nVariable = 0)
        {
            _signalSubSequence.Reset();
            {
                SpinWait.SpinUntil(() => false, 100);
                if (OnSimulateCLMP != null)
                    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== CLMP without mapping =======================================
        public override void ClmpW_WithoutMapping(int nTimeout, int nVariable = 0)
        {
            throw new NotImplementedException();
        }
        #endregion 

        #region =========================== UCLM =======================================
        public override void UclmW(int nTimeout, int nVariable = 0)
        {
            _signalSubSequence.Reset();
            {
                SpinWait.SpinUntil(() => false, 100);
                if (OnSimulateUCLM != null)
                    OnSimulateUCLM(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== WMAP =======================================
        public override void WmapW(int nTimeout)
        {
            _signalSubSequence.Reset();
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== LOAD =======================================

        public override void LoadW(int nTimeout)
        {

        }
        #endregion 

        #region =========================== UNLD =======================================

        public override void UnldW(int nTimeout)
        {

        }
        #endregion 

        #region =========================== EVNT =======================================
        public override void EventW(int nTimeout)
        {
            _signalSubSequence.Reset();
            {
                m_rc550.EvntW();
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== RSTA =======================================
        public override void ResetW(int nTimeout, int nReset = 0)
        {
            _signalSubSequence.Reset();
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== INIT =======================================
        public override void InitW(int nTimeout)
        {
            _signalSubSequence.Reset();
            {
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STOP =======================================

        public override void StopW(int nTimeout) { }
        #endregion 

        #region =========================== PAUS =======================================

        public override void PausW(int nTimeout) { }
        #endregion 

        #region =========================== MODE =======================================    
        public override void ModeW(int nTimeout, int nMode) { }
        #endregion 

        #region =========================== WTDT =======================================
        public override void WtdtW(int nTimeout) { }
        #endregion 

        #region =========================== SSPD =======================================      
        public override void SspdW(int nTimeout, int nVariable) { }
        #endregion 

        #region =========================== SPOT =======================================
        public override void SpotW(int nTimeout, int nBit, bool bOn)
        {
            _signalSubSequence.Reset();
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STAT =======================================
        public override void StatW(int nTimeout) { }
        #endregion 

        #region =========================== GPIO =======================================
        public override void GpioW(int nTimeout)
        {
            _signalSubSequence.Reset();
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GMAP =======================================

        public override void GmapW(int nTimeout)
        {
            _signalSubSequence.Reset();
            {
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GMAP =======================================

        public override void Rca2W(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            {
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== GVER =======================================
        public override void GverW(int nTimeout) { }
        #endregion 

        #region =========================== STIM =======================================
        public override void StimW(int nTimeout) { }
        #endregion 

        #region =========================== GPOS =======================================
        public override void GposW(int nTimeout)
        {
            _signalSubSequence.Reset();
            {
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GWID =======================================
        public override void GwidW(int nTimeout)
        {
            _signalSubSequence.Reset();
            {
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SWID =======================================
        public override void SwidW(int nTimeout, string strId)
        {
            _signalSubSequence.Reset();
            {

            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== STEP =======================================

        public override void YaxStepW(int nTimeout, string strStep) { }
        public override void ZaxStepW(int nTimeout, string strStep) { }
        #endregion

        #region =========================== HOME =======================================
        public override void YaxHomeW(int nTimeout, int nHome) { }
        public override void ZaxHomeW(int nTimeout, int nHome) { }
        #endregion 

        #region =========================== DPRM =======================================
        public override void GetDprmW(int nTimeout, int p1) { }
        public override void SetDprmW(int nTimeout, int p1, string strData) { }
        #endregion

        #region =========================== DMPR =======================================
        public override void GetDmprW(int nTimeout) { }
        public override void SetDmprW(int nTimeout, int p1, string strData) { }
        #endregion 

        #region =========================== DCST =======================================   
        public override void GetDcstW(int nTimeout) { }
        public override void SetDCSTW(int nTimeout, string strData) { }
        #endregion 
        //==============================================================================
        #region =========================== CreateMessage
        protected override void CreateMessage()
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
        }

        #endregion



    }
}
