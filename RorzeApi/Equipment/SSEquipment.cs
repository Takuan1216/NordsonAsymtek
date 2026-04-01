using RorzeApi;
using RorzeApi.Class;
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.Camera.Enum;
using RorzeUnit.Class.EQ.Enum;
using RorzeUnit.Class.EQ.Event;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace RorzeUnit.Class.EQ
{
    public class SSEquipment
    {
        //==============================================================================
        #region =========================== private ============================================
        private string m_strCmdStart = "";  //  與eq定義通訊格式 起始碼
        private string m_strCmdEnd = "\x0d\x0a";    //  與eq定義通訊格式 結尾碼
        private string m_strIP;
        private int m_nPort;
        private int m_nErrorCode = 0;

        private List<I_RC5X0_IO> ListDIO;

        private bool m_bProcessing = false;
        private bool m_bFirstCheck = false;

        private SLogger m_errorlog = SLogger.GetLogger("Errorlog");          //  log
        private SLogger m_logger;  //  log    
        private SLogger m_executelog = SLogger.GetLogger("ExecuteLog");      //  log
        private SSEQ_Socket m_Socket;
        private SWafer m_wafer;
        private SGroupRecipeManager m_grouprecipe;
        private enumControlState m_ControlState;
        List<string> m_listRecipe = new List<string>();

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[EQ{0}] : {1}  at line {2} ({3})", _BodyNo, strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }

        Dictionary<string, string> m_dicReportData = new Dictionary<string, string>();

        private int m_nAckTimeout = 5000;
        private int m_nProcessTimeout = 60000 * 40; // 客戶說動作最長時間30分鐘內會完成，timeout抓40分鐘

        private enumSendCmd m_eLastSend = enumSendCmd.Unknow;

        private bool m_bRobotExtand;

        
        #endregion
        #region =========================== public =============================================
        public bool Simulate { get; private set; }
        public bool Connected
        {
            get
            {
                if (Simulate) return true;
                if (m_Socket.isConnected())
                {
                    try
                    {
                        if (m_bFirstCheck) return true;
                        m_bFirstCheck = true;
                        return true;
                    }
                    catch (SException e)
                    {
                        m_logger.WriteLog(e.ToString());
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
        public bool Disable { get; private set; }
        public int _BodyNo { get; private set; }
        public string _Name { get; private set; }
        public bool IsProcessing { get { return m_bProcessing; } set { m_bProcessing = value; } }
        public bool IsError { get { return ErrorMSG != "" ; } }
        public bool CanUnldFromXYZ { get; set; } = false; // HSC
        public bool ProcessFinish { get; set; } = false;// HSC
        public SWafer Wafer
        {
            get { return m_wafer; }
            set
            {
                m_wafer = value;

                EQ_WaferChange?.Invoke(this, new WaferDataEventArgs(m_wafer));  //  更新UI  //  更新UI

                if (m_wafer == null) return;

                OnAssignWaferData?.Invoke(this, new WaferDataEventArgs(m_wafer));//更改Wafer位置為EQ//更改Wafer位置為EQ
            }
        }
        public List<string> RecipeList()
        {
            List<string> listR = new List<string>();
            foreach (string str in m_listRecipe)
                listR.Add(str);
            return listR;
        }
        public Dictionary<string, string> ReportData()
        {
            Dictionary<string, string> dicReportData = new Dictionary<string, string>();
            foreach (var item in m_dicReportData)
                dicReportData.Add(item.Key, item.Value);
            return dicReportData;
        }

        public enumControlState ControlState
        {
            get { return m_ControlState; }
            set
            {
                if (m_ControlState != value)
                {
                    m_ControlState = value;

                    //if (_AutoRemote && m_ControlState == enumControlState.ONLINELOCAL)
                    //{
                    //    m_ControlState = enumControlState.ONLINEREMOTE;
                    //}
                }
            }
        }

        //public bool IsRobotExtend { get { return m_bRobotExtand; } }
        //public bool SetRobotExtend { set { m_bRobotExtand = value; } }

        // XYZ
        public bool WaferInEQ { get; private set; }
        public enumMachineStatus XYZ_Status { get; private set; }
        public string ErrorMSG { get; private set; } = "";
        public List<string[]> Result_content_list { get; private set; }







        #endregion
        #region =========================== Event ==============================================
        public event EventHandler<WaferDataEventArgs> EQ_WaferChange;       //  EventHandler    更新UI
        public event EventHandler<WaferDataEventArgs> OnAssignWaferData;    //  EventHandler    收到 Wafer
        public event EventHandler<WaferDataEventArgs> OnWaferMeasureEnd;    //  EventHandler    Wafer量測結束 //v1.000 Jacky Hsiung Add
        public event EventHandler<bool> OnOrgnComplete;                     //  EventHandler    動作完成有特殊要觸發事件
		public event EventHandler<bool> OnSutterDoorOpenComplete;
        public event EventHandler<bool> OnSutterDoorCloseComplete;

        public event AutoProcessingEventHandler DoAutoProcessing;           //  EventHandler    自動流程使用

        public event MessageEventHandler OnReadData;                        //  EventHandler    TCP收到資料
        public event OccurErrorEventHandler OnOccurError;                   //  EventHandler    發生異常
        public event OccurErrorEventHandler OnOccurErrorRest;               //  EventHandler    解除異常

        public event EventHandler OnProcessStart;
        public event EventHandler OnProcessEnd;
        public event EventHandler OnProcessAbort;
        #endregion
        #region =========================== Thread =============================================
        private SPollingThread _exePolling;         // TCPIP Recive
        private SPollingThread _pollingAuto;        // Auto process

        private SInterruptOneThread _threadOrgn;    //  Thread
		private SInterruptOneThread _threadShutterDoorOpenW;
        private SInterruptOneThread _threadShutterDoorCloseW;
        private SInterruptOneThread _threadGetRecipe;//  Thread
        #endregion
        #region =========================== Delegate ===========================================
        public dlgv_wafer AssignToRobotQueue { get; set; }
        public dlgb_Object DlgWaferExist { get; set; }//委派外層
        public dlgb_Object DlgStageReady { get; set; }//委派外層
        public dlgb_Object DlgShutterDoorOpen { get; set; }
        public dlgb_Object DlgShutterDoorClose { get; set; }
        public dlgb_Object DlgVacuumOff { get; set; }//委派外層
        public dlgb_Object DlgSetDoorOpenW { get; set; }
        public dlgb_Object DlgSetDoorOpen { get; set; }
        public dlgb_Object DlgSetDoorCloseW { get; set; }
        public dlgb_o_b DlgSetRobotExtendIO { get; set; }

        public dlgb_Object DlgReadyUnload { get; set; }//委派外層
        public dlgb_Object DlgReadyLoad { get; set; }//委派外層 

        public dlgb_o_b DlgGetSMEMA { get; set; }//委派外層
        public dlgb_o_b DlgPutSMEMA { get; set; }//委派外層 

        public bool IsReadyUnload { get { return DlgReadyUnload != null && DlgReadyUnload(this); } }
        public bool IsReadyLoad { get { return DlgReadyLoad != null && DlgReadyLoad(this); } }

        public bool IsShutterDoorOpen { get { return DlgShutterDoorOpen != null && DlgShutterDoorOpen(this); } }
        public bool IsShutterDoorClose { get { return DlgShutterDoorClose != null && DlgShutterDoorClose(this); } }
        public bool SetDoorOpenW()
        {         
            if (DlgSetDoorOpenW == null)
            {
                ErrorMSG = "DlgSetDoorOpenW is null (no door open handler).";
                return false;
            }

            return DlgSetDoorOpenW(this);
        }
        public bool SetDoorOpen()
        {
            if (DlgSetDoorOpen == null)
            {
                ErrorMSG = "DlgSetDoorOpen is null (no door open handler).";
                return false;
            }

            return DlgSetDoorOpen(this);
        }
        public bool SetDoorCloseW()
        {
            if (DlgSetDoorCloseW == null)
            {
                ErrorMSG = "DlgSetDoorCloseW is null (no door close handler).";
                return false;
            }

            return DlgSetDoorCloseW(this);
        }

        public bool SetRobotExtendIO(bool bExtend)
        {
            if (DlgSetRobotExtendIO == null)
            {
                ErrorMSG = "DlgSetRobotExtendIO is null.";
                return false;
            }

            return DlgSetRobotExtendIO(this, bExtend);
        }

        public bool SetRobotGetSMEMA(bool bExtend)
        {
            if (DlgGetSMEMA == null)
            {
                ErrorMSG = "DlgGetSMEMA is null.";
                return false;
            }

            return DlgGetSMEMA(this, bExtend);
        }

        public bool SetRobotPutSMEMA(bool bExtend)
        {
            if (DlgPutSMEMA == null)
            {
                ErrorMSG = "DlgPutSMEMA is null.";
                return false;
            }

            return DlgPutSMEMA(this, bExtend);
        }

        //參考用的
        /*public dlgb_o_b DlgSetDoorOpenClose { get; set; }
        public bool SetDoorOpenClose(bool bIsOpen)
        {
            bool bResult = DlgSetDoorOpenClose == null? true : DlgSetDoorOpenClose(this, bIsOpen);
            return bResult;
        }*/

        #endregion

        public bool IsWaferExist { get { return WaferInEQ; } }
        public bool IsReady { get { return XYZ_Status != enumMachineStatus.ACTION; } }

        //========================= Constructor ========================================
        public SSEquipment(int nBodyNo, string strIP, int nPort, List<I_RC5X0_IO> dioList, bool bDisable, bool bSimulate, string strName, SGroupRecipeManager grouprecipe, sServer Sever = null)
        {
            _BodyNo = nBodyNo;
            m_strIP = bSimulate ? "127.0.0.1" : strIP;
            m_nPort = nPort;
            ListDIO = dioList;
            Disable = bDisable;
            Simulate = bSimulate;
            _Name = strName;
            m_grouprecipe = grouprecipe;
            Result_content_list = new List<string[]>();

            //  建立連線Socket類別
            m_Socket = new SSEQ_Socket(m_strIP, m_nPort, Simulate, m_strCmdStart, m_strCmdEnd, Sever);

            //  EventWaitHandle 表示執行緒同步處理事件，讓主執行緒發出封鎖的執行緒，然後等候執行緒完成工作。
            foreach (string name in System.Enum.GetNames(typeof(enumSendCmd)))
            {
                _signalAck.Add((enumSendCmd)System.Enum.Parse(typeof(enumSendCmd), name, true), new SSignal(false, EventResetMode.ManualReset));
            }
            //foreach (string name in System.Enum.GetNames(typeof(enumReceiveCmd)))
            //{
            //    _signalsReceive.Add((enumReceiveCmd)System.Enum.Parse(typeof(enumReceiveCmd), name, true), new SSignal(false, EventResetMode.ManualReset));
            //}

            _signalSubSequence = new SSignal(false, EventResetMode.ManualReset);

            //  One Thread  
            _threadOrgn = new SInterruptOneThread(ExeOrgn);
			_threadShutterDoorOpenW = new SInterruptOneThread(ExeShutterDoorOpenW);
            _threadShutterDoorCloseW = new SInterruptOneThread(ExeShutterDoorCloseW);
            _threadGetRecipe = new SInterruptOneThread(ExeGetRecipe);

            _pollingAuto = new SPollingThread(100);
            _pollingAuto.DoPolling += _pollingAuto_DoPolling;

            // TCPIP Recive
            _exePolling = new SPollingThread(1);
            _exePolling.DoPolling += _exePolling_DoPolling;
            if (false == Disable) { _exePolling.Set(); }

            CreateMessage();

            if (Disable == false)
            {
                m_logger = SLogger.GetLogger("EQCommunicationLog");  //  log  
            }
        }
        //============================================================================== 
        public bool isError(object sender, EQProtoclEventArgs e)
        {
            if (e.Frame.Result == "1")
            {
                return true;
            }
            return false;
        }
        public void Open() { if (false == Disable) m_Socket.Open(); }
        private void _exePolling_DoPolling()
        {
            try
            {
                //  判斷TCP收到
                string[] astrFrame;
                if (false == m_Socket.QueRecvBuffer.TryDequeue(out astrFrame)) return;
                //  傳送到外部顯示畫面用
                OnReadData?.Invoke(this, new MessageEventArgs(astrFrame));

                /*foreach (string strFrame in astrFrame)
                {
                    if (strFrame.Length == 0) continue;

                    string str = strFrame;

                    if (strFrame.IndexOf(m_strCmdStart) == 0)//去掉起始碼
                    {
                        str = strFrame.Substring(1);
                    }

                    m_logger.WriteLog("[recive]:" + str);

                    m_executelog.WriteLog("m_eLastSend = " + m_eLastSend);
                    AnalysisReceive(this, new EQProtoclEventArgs(strFrame));
                }*/

                foreach (string strFrame in astrFrame)
                {
                    if (strFrame.Length == 0) continue;
                    WriteLog("receive <- " + strFrame);//已經去除要結尾碼
                    bool bUnknownCmd = true;
                    foreach (var item in _dicSendCmdCmd)
                    {
                        if (strFrame.IndexOf(item.Value, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            //cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == strCmd).Key;
                            bUnknownCmd = false;//認識這個指令
                            break;
                        }
                    }

                    if (bUnknownCmd) // 不認識的封包
                    {
                        WriteLog("<<<ByPassReceive>>> Got unknown frame and pass to process. [" + strFrame + "]");
                        continue;
                    }
                    AnalysisReceive(this, new EQProtoclEventArgs(strFrame));
                }
            }
            catch (SException ex)
            {
                m_errorlog.WriteLog("[ EQ ] <<SException>> EQ _exePolling_DoPolling:" + ex);
            }
            catch (Exception ex)
            {
                m_errorlog.WriteLog("[ EQ ] <<Exception>> EQ _exePolling_DoPolling:" + ex);
            }
        }
        private void OnOrder(object sender, EQProtoclEventArgs e)
        {
            enumSendCmd cmd = _dicSendCmdCmd.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                //case enumEQCommand.AdjReq:
                //    AnalysisAdjustRequest(e.Frame.Value);
                //    break;
                default:
                    break;
            }
        }
        private void OnAck(object sender, EQProtoclEventArgs e)
        {
            enumSendCmd cmd = _dicSendCmdCmd.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                case enumSendCmd.Status:
                    AnalysisStatus(e.Frame.Data);
                    break;
                case enumSendCmd.RecipeList:
                    AnalysisRecipeList(cmd, e.Frame.Data);
                    break;
                case enumSendCmd.ProcessFinish:
                    m_logger.WriteLog("ProcessFinish");
                    ProcessFinish = true;
                    //AnalysisProcessFinish(e.Frame.Data);
                    break;
                case enumSendCmd.Result:
                    m_logger.WriteLog("Result");
                    AnalysisProcessFinish(e.Frame.Data);
                    break;
                case enumSendCmd.UnloadWafer:
                    m_logger.WriteLog("UnloadWafer");
                    CanUnldFromXYZ = true;
                    break;

                default:
                    break;
            }
        }
        private void OnError(object sender, EQProtoclEventArgs e)
        {
            if (e.Frame.Data != null)
            {
                ErrorMSG = e.Frame.Data[0];
                XYZ_Status = enumMachineStatus.ALARM;
            }
        }
        private void AnalysisStatus(string[] strFrame)
        {
            WaferInEQ = strFrame[0] == "1";
            switch(strFrame[1])
            {
                case "0":
                    XYZ_Status = enumMachineStatus.IDLE;
                    break;
                case "1":
                    XYZ_Status = enumMachineStatus.ACTION;
                    break;
                case "2":
                    XYZ_Status = enumMachineStatus.IDLE;
                    //AlarmW(); // When EQ happen alarm.
                    break;
            }
        }
        private void AnalysisRecipeList(enumSendCmd cmd, string[] strFrame)
        {
            m_listRecipe.Clear();
            if(strFrame.Length == 0)
            {
                return;
            }
            foreach (string str in strFrame)
            {
                m_listRecipe.Add(str);
            }

        }
        private void AnalysisProcessFinish(string[] strFrame)
        {
            //if (strFrame.Length < 6) // 理論上要有 HSC
            //{
            //    SendAlmMsg(enumEQError.OccursError);
            //    string error_rcv = string.Join(",", strFrame);
            //    m_errorlog.WriteLog("Parameter amounbt of result abnormal : " + error_rcv);
            //    return ;
            //}
            Result_content_list.Add(strFrame);
            m_logger.WriteLog(strFrame.ToString());
        }

        private void AnalysisReceive(object sender, EQProtoclEventArgs e)
        {
            {
                try
                {
                    enumSendCmd cmd = enumSendCmd.Unknow;
                    foreach (var item in _dicSendCmdCmd)
                    {
                        if (e.Frame.Command == item.Value)
                        {
                            cmd = item.Key;//認識這個指令
                            break;
                        }
                    }

                    if (cmd == enumSendCmd.Unknow && e.Frame.Command != null)
                    {
                        foreach (var item in _dicSendCmdCmd)
                        {
                            string temp = e.Frame.Command.Substring(1);
                            if (temp == item.Value)
                            {
                                cmd = item.Key;//認識這個指令
                                break;
                            }
                        }
                    }

                    switch (e.Frame.Header) //命令種類
                    {
                        case '@':
                            OnOrder(this, e);
                            break;
                        case 'a'://ack
                            OnAck(this, e);
                            _signalAck[cmd].Set();
                            break;
                        case 'e'://event
                            if (isError(this, e))
                            {
                                OnError(this, e);
                            }
                            else
                            {
                                OnAck(this, e);
                            }
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    m_executelog.WriteLog("<<Exception>> AnalysisReceive:" + ex);
                }
            }
        }

        public void AutoProcessStart()
        {
            this._pollingAuto.Set();
            if (OnProcessStart != null)
                OnProcessStart(this, new EventArgs());
        }
        public void AutoProcessEnd()
        {
            _pollingAuto.Reset();
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
                m_errorlog.WriteLog("[ EQ ] <<SException>> Robot DoAutoProcessing thread:" + ex);
                _pollingAuto.Reset();

                if (OnProcessAbort != null)
                    OnProcessAbort(this, new EventArgs());
            }
            catch (Exception ex)
            {
                m_errorlog.WriteLog("[ EQ ] <<Exception>> Robot DoAutoProcessing thread:" + ex);
                _pollingAuto.Reset();

                if (OnProcessAbort != null)
                    OnProcessAbort(this, new EventArgs());
            }
        }
        //========================= One Thread ========================================       
        public void tOrgnSet() { _threadOrgn.Set(); }
		public void tShutterDoorOpenSetW() { _threadShutterDoorOpenW.Set(); }
        public void tShutterDoorCloseSetW() { _threadShutterDoorCloseW.Set(); }
        public void tGetRecipeSet() { _threadGetRecipe.Set(); }
        public void ExeOrgn()
        {
            try
            {
                //待確認原點流程
                int test = _BodyNo;
                if (test == 4)
                {

                }
                m_executelog.WriteLog("[ EQ ] ExeOrgn:Start");

<<<<<<< HEAD
                if (!Simulate && !SetDoorCloseW())
=======
                if (!SetDoorCloseW())
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
                    throw new SException((int)enumEQError.ShutterDoorCloseFail, string.Format("ShutterDoorClose failed"));

                GetRecipeListW();
                IsProcessing = false;
                ErrorMSG = ""; // HSC 
                OnOrgnComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                m_errorlog.WriteLog("[ EQ ] <<SException>> ExeOrgn:" + ex);
                OnOrgnComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                m_errorlog.WriteLog("[ EQ ] <<Exception>> ExeOrgn:" + ex);
                OnOrgnComplete?.Invoke(this, false);
            }
        }
        public void ExeShutterDoorOpenW()
        {
            m_executelog.WriteLog("[ EQ ] ExeShutterDoorOpenW:Start");

            bool succeed = true;
            try
            {
                ErrorMSG = "";

                if (!GParam.theInst.IsSimulate)
                {
                    succeed = SetDoorOpenW();               //會等門開好
                    if (!succeed && string.IsNullOrEmpty(ErrorMSG))
                    {
                        ErrorMSG = "Set Door OpenW failed.";
                    }
                }

                OnSutterDoorOpenComplete?.Invoke(this, succeed);
            }
            catch (SException ex)
            {
                succeed = false;
                ErrorMSG = ex.Message; // 或你想要的格式
                m_errorlog.WriteLog("[ EQ ] <<SException>> ExeShutterDoorOpenW:" + ex);
                OnSutterDoorOpenComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                succeed = false;
                ErrorMSG = ex.Message;
                m_errorlog.WriteLog("[ EQ ] <<Exception>> ExeShutterDoorOpenW:" + ex);
                OnSutterDoorOpenComplete?.Invoke(this, false);
            }
        }

        public void ExeShutterDoorCloseW()
        {
            m_executelog.WriteLog("[ EQ ] ExeShutterDoorCloseW:Start");

            bool succeed = true;
            try
            {
                ErrorMSG = "";

                if (!GParam.theInst.IsSimulate)
                {
                    succeed = SetDoorCloseW();
                    if (!succeed && string.IsNullOrEmpty(ErrorMSG))
                    {
                        ErrorMSG = "Set DoorW Close failed.";
                    }
                }

                OnSutterDoorCloseComplete?.Invoke(this, succeed);
            }
            catch (SException ex)
            {
                succeed = false;
                ErrorMSG = ex.Message; // 或你想要的格式
                m_errorlog.WriteLog("[ EQ ] <<SException>> ExeShutterDoorCloseW:" + ex);
                OnSutterDoorCloseComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                succeed = false;
                ErrorMSG = ex.Message;
                m_errorlog.WriteLog("[ EQ ] <<Exception>> ExeShutterDoorCloseW:" + ex);
                OnSutterDoorCloseComplete?.Invoke(this, false);
            }
        }
        public void ExeGetRecipe()
        {
            try
            {
                m_executelog.WriteLog("[ EQ ] ExeGetRecipe:Start");
                GetRecipeListW();
            }
            catch (SException ex)
            {
                m_errorlog.WriteLog("[ EQ ] <<SException>> ExeGetRecipe:" + ex);

            }
            catch (Exception ex)
            {
                m_errorlog.WriteLog("[ EQ ] <<Exception>> ExeGetRecipe:" + ex);
            }
        }

        //========================= Send Command ======================================= 
        public void SendCommand(string strCommand)
        {
            //if (m_Socket.isConnected() == false)
            //{
            //    SendAlmMsg(enumEQError.Socket_Disconnected);
            //    throw new SException((int)enumEQError.Socket_Disconnected, string.Format("EQ Socket is disconnected!"));
            //}
            m_Socket.SendCommand(strCommand);
            OnReadData?.Invoke(this, new MessageEventArgs(new string[] { "send:" + strCommand }));
        }
        public void SendCommand(string format, params object[] args)
        {
            if (m_Socket.isConnected() == false)
            {
                SendAlmMsg(enumEQError.Socket_Disconnected);
                throw new SException((int)enumEQError.Socket_Disconnected, string.Format("EQ Socket is disconnected!"));
            }
            SendCommand(string.Format(format, args));
        }
        public void SendCommand_JSON(string strKey, object strValue)
        {
            string str = string.Format("{0}\"command\":\"{1}\",\"value\":\"{2}\"{3}", '{', strKey, strValue, '}');
            SendCommand(str);
        }
        public void SendCommand_JSON_NoValue(string strKey)
        {
            string str = string.Format("{0}\"command\":\"{1}\"\"{2}", '{', strKey, '}');
            SendCommand(str);
        }
        //==============================================================================
        #region =========================== XYZ Commands===========================
        private void SendCommandWithAck(enumSendCmd eCmd, Action sendAction)
        {
            SendCommandWithAck<object>(eCmd, _ => sendAction(), null);
        }

        private void SendCommandWithAck<T>(enumSendCmd eCmd, Action<T> sendAction, T parameter)
        {
            m_eLastSend = eCmd;
            int nTimeout = m_nAckTimeout;
            if (eCmd == enumSendCmd.RecipeList || eCmd == enumSendCmd.PutWaferFinish || eCmd == enumSendCmd.PrepareToReceiveWafer)
            {
                nTimeout = 60000; // recipe timeout = 60 sec
            }
            _signalSubSequence.Reset();
            if (Simulate == false)
            {
                sendAction(parameter);
                if (!_signalAck[eCmd].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumEQError.AckTimeout);
                    throw new SException((int)enumEQError.AckTimeout, $"Wait {eCmd} Ack was timeout. [Timeout = {nTimeout} ms]");
                }
                if (_signalAck[eCmd].bAbnormalTerminal)
                {
                    SendAlmMsg(enumEQError.AckAbnormal);
                    throw new SException((int)enumEQError.AckAbnormal, $"Wait {eCmd} Ack and Ack was abnormal end.");
                }
            }
            else
            {
                string strFrame = string.Format("{0}a{1}:", m_strCmdStart, _dicSendCmdCmd[eCmd]);
                m_executelog.WriteLog("receive <- " + strFrame);
                //OnAck(this, new EQProtoclEventArgs(strFrame));
            }
            _signalSubSequence.Set();
        }
        #region ==================== Hello ====================
        public void Hello()
        {
            enumSendCmd eCmd = enumSendCmd.Hello;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void HelloW()
        {
            SendCommandWithAck(enumSendCmd.Hello, Hello);
        }
        #endregion

        #region ==================== RecipeList ====================
        public void GetRecipeList()
        {
            enumSendCmd eCmd = enumSendCmd.RecipeList;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("o{0}", _dicSendCmdCmd[eCmd]));
        }

        public void GetRecipeListW()
        {
            int nTimeout = GParam.theInst.GetEQAckTimeout;
            enumSendCmd eCmd = enumSendCmd.RecipeList;
            _signalSubSequence.Reset();
            GetRecipeList();
            if (Simulate == false)
            {
                if (false == _signalAck[eCmd].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumEQError.AckTimeout);
                    throw new SException((int)enumEQError.AckTimeout, string.Format("Wait {0} Ack was timeout. [Timeout = {1} ms]", eCmd, nTimeout));
                }
                if (_signalAck[eCmd].bAbnormalTerminal)
                {
                    SendAlmMsg(enumEQError.AckAbnormal);
                    throw new SException((int)enumEQError.AckAbnormal, string.Format("Wait {0} Ack and Ack was abnormal end.", eCmd));
                }
            }
            else
            {
                //aRECIPELIST:RecipeName1@RecipeName2@……          
                string strFrame = string.Format("{0}a{1}:name111@name2223@name3@name4@name5@name6", m_strCmdStart, _dicSendCmdCmd[eCmd]);
                WriteLog("receive <- " + strFrame);
                OnAck(this, new EQProtoclEventArgs(strFrame));
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region ==================== PrepareToReceiveWafer ====================
        public void PrepareToReceiveWafer()
        {
            enumSendCmd eCmd = enumSendCmd.PrepareToReceiveWafer;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void PrepareToReceiveWaferW()
        {
            SendCommandWithAck(enumSendCmd.PrepareToReceiveWafer, PrepareToReceiveWafer);
        }
        #endregion

        #region ==================== PutWaferFinish ====================
        public void PutWaferFinish()
        {
            enumSendCmd eCmd = enumSendCmd.PutWaferFinish;
            Result_content_list.Clear();
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void PutWaferFinishW()
        {
            SendCommandWithAck(enumSendCmd.PutWaferFinish, PutWaferFinish);
        }
        #endregion

        #region ==================== ProcessWafer ====================
        public void ProcessWafer(string mode, string RecipeName, string LotID , string WaferID )
        {
            enumSendCmd eCmd = enumSendCmd.ProcessWafer;
            _signalAck[eCmd].Reset();
            if(Simulate == false)
            {
                if (mode == "M")
                {
                    SendCommand(string.Format("@{0},0,{1}", _dicSendCmdCmd[eCmd], mode));
                }
                else if (mode == "A")
                {
                    SendCommand(string.Format("@{0},0,{1},{2},{3},{4}", _dicSendCmdCmd[eCmd], mode, RecipeName, LotID, WaferID));
                }
            }
            else
            {
                string strFrame = string.Format("{0}a{1}:", m_strCmdStart, _dicSendCmdCmd[eCmd]);
                m_executelog.WriteLog("receive <- " + strFrame);
            }
        }

        public void ProcessWaferW(string mode = "M", string RecipeName = "Recipe", string LotID = "1", string WaferID = "TEST123")
        {
            m_eLastSend = enumSendCmd.ProcessWafer;
            enumSendCmd eCmd = enumSendCmd.ProcessWafer;
            int nTimeout = m_nAckTimeout;
            _signalSubSequence.Reset();
            ProcessWafer( mode, RecipeName, LotID, WaferID );
            if (Simulate == false)
            {
                if (!_signalAck[eCmd].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumEQError.AckTimeout);
                    throw new SException((int)enumEQError.AckTimeout, $"Wait {eCmd} Ack was timeout. [Timeout = {nTimeout} ms]");
                }
                if (_signalAck[eCmd].bAbnormalTerminal)
                {
                    SendAlmMsg(enumEQError.AckAbnormal);
                    throw new SException((int)enumEQError.AckAbnormal, $"Wait {eCmd} Ack and Ack was abnormal end.");
                }
            }
            else
            {
                string strFrame = string.Format("{0}a{1}:", m_strCmdStart, _dicSendCmdCmd[eCmd]);
                m_executelog.WriteLog("receive <- " + strFrame);
                //OnAck(this, new EQProtoclEventArgs(strFrame));
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region ==================== GetWaferFinish ====================
        public void GetWaferFinish()
        {
            enumSendCmd eCmd = enumSendCmd.GetWaferFinish;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void GetWaferFinishW()
        {
            SendCommandWithAck(enumSendCmd.GetWaferFinish, GetWaferFinish);
        }
        #endregion

        #region ==================== Stop ====================
        public void Stop()
        {
            enumSendCmd eCmd = enumSendCmd.Stop;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void StopW()
        {
            SendCommandWithAck(enumSendCmd.Stop, Stop);
        }
        #endregion

        #region ==================== Retry ====================
        public void Retry()
        {
            enumSendCmd eCmd = enumSendCmd.Retry;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void RetryW()
        {
            SendCommandWithAck(enumSendCmd.Retry, Retry);
        }
        #endregion

        #region ==================== Abort ====================
        public void Abort()
        {
            enumSendCmd eCmd = enumSendCmd.Abort;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void AbortW()
        {
            SendCommandWithAck(enumSendCmd.Abort, Abort);
        }
        #endregion

        #region ==================== Status ====================
        public void Status()
        {
            enumSendCmd eCmd = enumSendCmd.Status;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void StatusW()
        {
            SendCommandWithAck(enumSendCmd.Status, Status);
        }
        #endregion

        #region ==================== UnloadWafer ====================
        public void UnloadWafer()
        {
            enumSendCmd eCmd = enumSendCmd.UnloadWafer;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void UnloadWaferW()
        {
            SendCommandWithAck(enumSendCmd.UnloadWafer, UnloadWafer);
        }
        #endregion

        #region ==================== Alarm ====================
        public void Alarm()
        {
            enumSendCmd eCmd = enumSendCmd.Alarm;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void AlarmW()
        {
            SendCommandWithAck(enumSendCmd.Alarm, Alarm);
        }
        #endregion

        #region ==================== SoftwareVersion ====================
        public void SoftwareVersion()
        {
            enumSendCmd eCmd = enumSendCmd.SoftwareVersion;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void SoftwareVersionW()
        {
            SendCommandWithAck(enumSendCmd.SoftwareVersion, SoftwareVersion);
        }
        #endregion

        #region ==================== MachineType ====================
        public void MachineType()
        {
            enumSendCmd eCmd = enumSendCmd.MachineType;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void MachineTypeW()
        {
            SendCommandWithAck(enumSendCmd.MachineType, MachineType);
        }
        #endregion

        #region ==================== EQToSafePosition ====================
        public void EQToSafePosition()
        {
            enumSendCmd eCmd = enumSendCmd.EQToSafePosition;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0}", _dicSendCmdCmd[eCmd]));
        }

        public void EQToSafePositionW()
        {
            SendCommandWithAck(enumSendCmd.EQToSafePosition, EQToSafePosition);
        }
        #endregion

        #region ==================== ModeLock ====================
        public void ModeLock(string mode)
        {
            enumSendCmd eCmd = enumSendCmd.ModeLock;
            _signalAck[eCmd].Reset();
            SendCommand(string.Format("@{0},0,{1}", _dicSendCmdCmd[eCmd],mode));
        }

        public void ModeLockW(string mode)
        {
            SendCommandWithAck(enumSendCmd.ModeLock,(string Pmode) => ModeLock(Pmode),mode); // MODELOCK ?????
        }
        #endregion

        #endregion
        //==============================================================================
        #region =========================== CommandTable =======================================
        //public Dictionary<enumReceiveCmd, string> _dicReceiveCmd;
        public Dictionary<enumSendCmd, string> _dicSendCmdCmd;
        #endregion 
        #region =========================== Signals ============================================
        protected Dictionary<enumSendCmd, SSignal> _signalAck = new Dictionary<enumSendCmd, SSignal>();            //  EventWaitHandle Tcp Ack
        //protected Dictionary<enumReceiveCmd, SSignal> _signalsReceive = new Dictionary<enumReceiveCmd, SSignal>();  //  EventWaitHandle Tcp Evnt
        private SSignal _signalSubSequence;                                                                         //  EventWaitHandle 管控cmd   
        #endregion ==========================================================================
        #region =========================== OnOccurError =======================================
        //  發生異常
        private void SendAlmMsg(string strCode)//23 11 00000
        {
            m_errorlog.WriteLog("[ EQ ] Occur stat Error : {0}", strCode);
            m_executelog.WriteLog("[ EQ ] Occur stat Error : {0}", strCode);
            int nCode = Convert.ToInt32(strCode, 10) + 23 * 10000000 + 11 * 100000;
            OnOccurError?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除異常
        private void RestAlmMsg(string strCode)
        {
            m_errorlog.WriteLog("[ EQ ] Rest stat Error : {0}", strCode);
            m_executelog.WriteLog("[ EQ ] Rest stat Error : {0}", strCode);
            if (strCode.Length != 5) return;
            int nCode = Convert.ToInt32(strCode, 10) + 23 * 10000000 + 11 * 100000;
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生自定義異常
        private void SendAlmMsg(enumEQError eCode)//23 10 00000
        {
            int nCode = (int)eCode;
            m_errorlog.WriteLog(string.Format("[ EQ ] eAlarm Error : {0}_{1}", nCode, eCode));
            m_executelog.WriteLog(string.Format("[ EQ ] eAlarm Error : {0}{0}_{1}", nCode, eCode));
            OnOccurError?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        public void TriggerSException(enumEQError eEQError)
        {
            SendAlmMsg(eEQError);
            throw new SException((int)(eEQError), "SException:" + eEQError);
        }
        #endregion
        #region =========================== CreateMessage ======================================
        public Dictionary<int, string> m_dicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicError { get; } = new Dictionary<int, string>();
        void CreateMessage()
        {

            m_dicController[0] = "[00:Ellipsometry] ";

            m_dicError[1] = "01:alarm";
            m_dicError[2] = "02:recipe not exist";
            m_dicError[3] = "03:no initial";

            //_dicReceiveCmd = new Dictionary<enumReceiveCmd, string>()
            //{
            //    {enumReceiveCmd.UnloadComplete,"UnloadComplete"},
            //    {enumReceiveCmd.CanLoad,"CanLoad"},
            //    {enumReceiveCmd.LoadComplete,"LoadComplete"},
            //    {enumReceiveCmd.MeasureEnd,"MeasureEnd"},
            //    {enumReceiveCmd.CanUnload,"CanUnload"},

            //    {enumReceiveCmd.GetRecipeList,"RequsetRecipeList"},
            //    {enumReceiveCmd.CheckWafer,"CheckWafer" },
            //    {enumReceiveCmd.Error,"Error"},
            //};

            _dicSendCmdCmd = new Dictionary<enumSendCmd, string>()
            {
                { enumSendCmd.Hello, "Hello" },
                { enumSendCmd.RecipeList, "RECIPELIST" },
                { enumSendCmd.PrepareToReceiveWafer, "PrepareToReceiveWafer" },
                { enumSendCmd.PutWaferFinish, "PutWaferFinish" },
                { enumSendCmd.ProcessWafer, "ProcessWafer" },
                { enumSendCmd.GetWaferFinish, "GetWaferFinish" },
                { enumSendCmd.Stop, "Stop" },
                { enumSendCmd.Retry, "Retry" },
                { enumSendCmd.Abort, "Abort" },
                { enumSendCmd.Status, "Status" },
                { enumSendCmd.UnloadWafer, "UnloadWafer" },
                { enumSendCmd.Alarm, "Alarm" },
                { enumSendCmd.SoftwareVersion, "SoftwareVersion" },
                { enumSendCmd.MachineType, "MachineType" },
                { enumSendCmd.ModeLock, "ModeLock" },
                {enumSendCmd.EQToSafePosition, "EQToSafePosition" },
                {enumSendCmd.ProcessFinish, "ProcessFinish" },
                {enumSendCmd.Result, "Result" }
            };
        }
        #endregion
        
    }
}
