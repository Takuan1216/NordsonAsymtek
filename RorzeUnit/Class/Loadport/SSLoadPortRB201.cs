using Advantech.Adam;
using Rorze.Equipments.Unit;
using RorzeApi.Class;
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static RorzeUnit.Class.SWafer;

namespace RorzeUnit.Class.Loadport.Type
{
    public class SSLoadPortRB201 : SSLoadportParents
    {
        #region =========================== private ============================================
        private sRorzeSocket m_Socket;
        //  DPRM
        private string[] m_strDprm; //這是站存區
        private LoadPortGPIO _gpio;
        private SPollingThread _exePolling;
		private bool ORGN_Compltete_check = false;

        private I_Robot m_robot;
        #endregion
        #region =========================== property ===========================================
        public override bool UseAdapter { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIAdapter]; } }
        public override bool IsProtrude { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIProtrusion]; } }
        public override bool IsPresenceON { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIPresence]; } }
        public override bool IsPresenceleftON { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceleft]; } }
        public override bool IsPresencerightON { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceright]; } }
        public override bool IsPresencemiddleON { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIPresencemiddle]; } }
        public override bool IsDoorOpen { get { return _gpio.GetD0List[LoadPortGPIO.LoadPortDO._DODoorOpen]; } }
        public override bool IsUnclamp { get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen]; } }//close 是勾住

        private int CrossCount = 0;
		private SLogger _executeLogger = SLogger.GetLogger("ExecuteLog");
        #endregion
        #region =========================== event ==============================================
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
        #region =========================== CreateMessage ======================================
        protected override void CreateMessage()
        {
            // m_dicCancel - Cancel codes
            m_dicCancel[0x0200] = "0200:The operating objective is not supported.";
            m_dicCancel[0x0300] = "0300:The composition elements of command are too few.";
            m_dicCancel[0x0310] = "0310:The composition elements of command are too many.";
            m_dicCancel[0x0400] = "0400:Command is not supported.";
            m_dicCancel[0x0500] = "0500:Too few parameters.";
            m_dicCancel[0x0510] = "0510:Too many parameters.";
            m_dicCancel[0x0520] = "0520:Improper number of parameters.";
            // 060X: The value of the No. (X+1) parameter is too small.
            for (int i = 0; i <= 0xF; i++)
                m_dicCancel[0x0600 + i] = string.Format("{0:X4}:The value of the No. {1} parameter is too small.", 0x0600 + i, i + 1);
            // 061X: The value of the No. (X+1) parameter is too large.
            for (int i = 0; i <= 0xF; i++)
                m_dicCancel[0x0610 + i] = string.Format("{0:X4}:The value of the No. {1} parameter is too large.", 0x0610 + i, i + 1);
            // 062X: The No. (X+1) parameter is not numerical number.
            for (int i = 0; i <= 0xF; i++)
                m_dicCancel[0x0620 + i] = string.Format("{0:X4}:The No. {1} parameter is not numerical number.", 0x0620 + i, i + 1);
            // 063X: The digit number of the No. (X+1) parameter is not proper.
            for (int i = 0; i <= 0xF; i++)
                m_dicCancel[0x0630 + i] = string.Format("{0:X4}:The digit number of the No. {1} parameter is not proper.", 0x0630 + i, i + 1);
            // 064X: The No. (X+1) parameter is not a hexadecimal numeral.
            for (int i = 0; i <= 0xF; i++)
                m_dicCancel[0x0640 + i] = string.Format("{0:X4}:The No. {1} parameter is not a hexadecimal numeral.", 0x0640 + i, i + 1);
            // 065X: The No. (X+1) parameter is not proper.
            for (int i = 0; i <= 0xF; i++)
                m_dicCancel[0x0650 + i] = string.Format("{0:X4}:The No. {1} parameter is not proper.", 0x0650 + i, i + 1);
            // 066X: The No. (X+1) parameter is not pulse.
            for (int i = 0; i <= 0xF; i++)
                m_dicCancel[0x0660 + i] = string.Format("{0:X4}:The No. {1} parameter is not pulse.", 0x0660 + i, i + 1);
            m_dicCancel[0x0700] = "0700:Abnormal Mode: Not ready.";
            m_dicCancel[0x0702] = "0702:Abnormal Mode: Not in the maintenance mode.";
            // 08XX: The setting data of the No. (XX+1) is not proper.
            for (int i = 0; i <= 0xFF; i++)
                m_dicCancel[0x0800 + i] = string.Format("{0:X4}:The setting data of the No. {1} is not proper.", 0x0800 + i, i + 1);
            m_dicCancel[0x0920] = "0920:Improper setting.";
            m_dicCancel[0x0A00] = "0A00:Origin search not completed.";
            m_dicCancel[0x0A01] = "0A01:Origin reset not completed.";
            m_dicCancel[0x0B00] = "0B00:Processing.";
            m_dicCancel[0x0B01] = "0B01:Moving.";
            m_dicCancel[0x0D00] = "0D00:Abnormal flash memory.";
            m_dicCancel[0x0F00] = "0F00:Error-occurred state.";
            m_dicCancel[0x1000] = "1000:Movement is unable due to carrier presence.";
            m_dicCancel[0x1001] = "1001:Movement is unable due to no carrier presence.";
            m_dicCancel[0x1002] = "1002:Improper setting.";
            m_dicCancel[0x1003] = "1003:Improper current position.";
            m_dicCancel[0x1004] = "1004:Movement is unable due to small designated position.";
            m_dicCancel[0x1005] = "1005:Movement is unable due to large designated position.";
            m_dicCancel[0x1006] = "1006:Presence of the adapter cannot be identified.";
            m_dicCancel[0x1007] = "1007:Origin search cannot be performed due to abnormal presence state of the adapter.";
            m_dicCancel[0x1008] = "1008:Adapter not prepared.";
            m_dicCancel[0x1009] = "1009:Cover not closed.";
            // 103X: Interfering with the No. (X+1) axis.
            for (int i = 0; i <= 0xF; i++)
                m_dicCancel[0x1030 + i] = string.Format("{0:X4}:Interfering with the No. {1} axis.", 0x1030 + i, i + 1);
            m_dicCancel[0x1100] = "1100:Emergency stop signal is ON.";
            m_dicCancel[0x1200] = "1200:Pause signal is ON / Area sensor beam is blocked.";
            m_dicCancel[0x1300] = "1300:Interlock signal is ON.";
            m_dicCancel[0x1400] = "1400:Drive power is OFF.";
            m_dicCancel[0x2000] = "2000:No response from the ID reader/writer.";
            m_dicCancel[0x2100] = "2100:Command for the ID reader/writer is cancelled.";

            // m_dicController - Controller codes
            m_dicController[0x00] = "[00:Others] ";
            m_dicController[0x01] = "[01:Y-axis] ";
            m_dicController[0x02] = "[02:Y2-axis] ";
            m_dicController[0x03] = "[03:Z-axis] ";
            m_dicController[0x04] = "[04:Lifting/Lowering mechanism for reading the carrier ID] ";
            m_dicController[0x05] = "[05:Stage retaining mechanism] ";
            m_dicController[0x06] = "[06:Rotation Table] ";
            m_dicController[0x07] = "[07:Chuck (Vacuum)] ";
            m_dicController[0x08] = "[08:Clamp] ";
            m_dicController[0x09] = "[09:Clamp Lift] ";
            m_dicController[0x0A] = "[0A:Latch Key] ";
            m_dicController[0x0B] = "[0B:Mapping Bar] ";
            m_dicController[0x0C] = "[0C:Stopper] ";
            m_dicController[0x0D] = "[0D:N2-axis] ";
            m_dicController[0x0E] = "[0E:N2 purge] ";
            m_dicController[0x0F] = "[0F:Adapter] ";
            m_dicController[0x10] = "[10:Driver] ";
            m_dicController[0x11] = "[11:Door Detection] ";
            m_dicController[0x12] = "[12:Placement Sensor] ";
            m_dicController[0x13] = "[13:E84] ";

            // m_dicError - Error codes
            m_dicError[0x01] = "01:Motor stall";
            m_dicError[0x02] = "02:Sensor abnormal";
            m_dicError[0x03] = "03:Emergency stop";
            m_dicError[0x04] = "04:Command error";
            m_dicError[0x05] = "05:Communication error";
            m_dicError[0x06] = "06:Chucking sensor abnormal";
            m_dicError[0x08] = "08:Obstacle detection sensor error";
            m_dicError[0x09] = "09:Origin sensor abnormal";
            m_dicError[0x0A] = "0A:Mapping sensor abnormal";
            m_dicError[0x0B] = "0B:Wafer protrusion detection sensor abnormal";
            m_dicError[0x0E] = "0E:Driver abnormal";
            m_dicError[0x20] = "20:Control power abnormal";
            m_dicError[0x21] = "21:Drive power abnormal";
            m_dicError[0x22] = "22:EEPROM abnormal";
            m_dicError[0x23] = "23:Z search error";
            m_dicError[0x24] = "24:Overheat";
            m_dicError[0x25] = "25:Overcurrent";
            m_dicError[0x26] = "26:Motor cable abnormal";
            m_dicError[0x27] = "27:Motor stall (position deviation)";
            m_dicError[0x28] = "28:Motor stall (time over)";
            m_dicError[0x29] = "29:Motor servo ON abnormal";
            m_dicError[0x2A] = "2A:Motor encoder read abnormal";
            m_dicError[0x2B] = "2B:Motor origin search abnormal";
            m_dicError[0x2C] = "2C:Motor position command abnormal";
            m_dicError[0x40] = "40:Memory abnormal";
            m_dicError[0x41] = "41:REM library initialization failed";
            m_dicError[0x42] = "42:REM library exception occurred";
            m_dicError[0x43] = "43:Unsupported system type";
            m_dicError[0x44] = "44:Function mapping initialization abnormal";
            m_dicError[0x45] = "45:Invalid parameter";
            m_dicError[0x71] = "71:Axis position abnormal";
            m_dicError[0x72] = "72:Axis not in idle state";
            m_dicError[0x73] = "73:Suspected air leakage causing offset";
            m_dicError[0x74] = "74:Unknown axis position";
            m_dicError[0x76] = "76:Sensor value abnormal";
            m_dicError[0x79] = "79:Clamp lift on position not detected";
            m_dicError[0x7A] = "7A:Clamp lift off position not detected";
            m_dicError[0x81] = "81:Chucking on signal not detected";
            m_dicError[0x82] = "82:Chucking off signal not detected";
            m_dicError[0x83] = "83:Stopper on signal not detected";
            m_dicError[0x84] = "84:Stopper off signal not detected";
            m_dicError[0x85] = "85:Motion timeout";
            m_dicError[0x86] = "86:CDA pressure out of range";
            m_dicError[0x89] = "89:Exhaust fan signal not detected";
            m_dicError[0x92] = "92:Carrier Clamp disabled";
            m_dicError[0x93] = "93:Carrier Unclamp disabled";
            m_dicError[0x94] = "94:Latch key lock disabled";
            m_dicError[0x96] = "96:Latch key release disabled";
            m_dicError[0x97] = "97:Mapping sensor preparation disabled";
            m_dicError[0x98] = "98:Mapping sensor containing disabled";
            m_dicError[0x99] = "99:Chucking disabled";
            m_dicError[0x9A] = "9A:Wafer protrusion";
            m_dicError[0x9B] = "9B:FOUP door missing or FOSB has door";
            m_dicError[0x9C] = "9C:Carrier improperly taken";
            m_dicError[0x9D] = "9D:FOSB door detection";
            m_dicError[0x9E] = "9E:Carrier improperly placed";
            m_dicError[0x9F] = "9F:Carrier detection error";
            m_dicError[0xA0] = "A0:Cover lock disabled";
            m_dicError[0xA1] = "A1:Cover unlock disabled";
            m_dicError[0xA7] = "A7:Adapter communication off";
            m_dicError[0xA8] = "A8:Adapter communication on";
            m_dicError[0xA9] = "A9:Adapter detection off";
            m_dicError[0xAA] = "AA:Adapter detection on";
            m_dicError[0xAB] = "AB:Adapter lock off";
            m_dicError[0xAC] = "AC:Adapter lock on";
            m_dicError[0xB0] = "B0:TR_REQ timeout";
            m_dicError[0xB1] = "B1:BUSY ON timeout";
            m_dicError[0xB2] = "B2:Load Carrier timeout";
            m_dicError[0xB3] = "B3:Unload Carrier timeout";
            m_dicError[0xB4] = "B4:BUSY OFF timeout";
            m_dicError[0xB6] = "B6:VALID OFF timeout";
            m_dicError[0xB7] = "B7:CONTINUE timeout";
            m_dicError[0xB8] = "B8:Signal abnormal: VALID_CS_O=ON to TR_REQ=ON transition";
            m_dicError[0xB9] = "B9:Signal abnormal: TR_REQ=ON to BUSY=ON transition";
            m_dicError[0xBA] = "BA:Signal abnormal: BUSY=ON to Placement=ON transition";
            m_dicError[0xBB] = "BB:Signal abnormal: Placement=ON to COMPLETE=ON transition";
            m_dicError[0xBC] = "BC:Signal abnormal: COMPLETE=ON to VALID=OFF transition";
			m_dicError[0xBD] = "BD:E84 manual stop";
            m_dicError[0xBF] = "BF:VALID_CS_O signal abnormal";
            m_dicError[0xC0] = "C0:State Machine abnormal";
            m_dicError[0xD0] = "D0:Mapping unable to identify FOUP type";
            m_dicError[0xD1] = "D1:Mapping sensor abnormal";
            m_dicError[0xD2] = "D2:Mapping data format mismatch";
            m_dicError[0xD3] = "D3:Mapping search slot ID abnormal";
            m_dicError[0xD4] = "D4:Mapping result abnormal";
            m_dicError[0xD5] = "D5:Mapping sensor (X/Y axis) position lost";
            m_dicError[0xD8] = "D8:Vision Mapping communication error";
            m_dicError[0xE0] = "E0:N2 purge state initialization not completed";
            m_dicError[0xE1] = "E1:N2 purge state busy";
            m_dicError[0xE2] = "E2:N2 state abnormal";
            m_dicError[0xE3] = "E3:N2 axis position abnormal";
            m_dicError[0xE4] = "E4:DNPM flag parameter exceeds limit";
            m_dicError[0xE5] = "E5:N2 purge OFF state detection abnormal";
            m_dicError[0xE6] = "E6:N2 pressure insufficient (gauge abnormal)";
            m_dicError[0xE7] = "E7:N2 main flow abnormal";
            m_dicError[0xE8] = "E8:N2 sub flow abnormal";

            // =========================================================

            _dicCmdsTable = new Dictionary<enumLoadPortCommand, string>()
            {
                {enumLoadPortCommand.Orgn,"ORGN"},
                {enumLoadPortCommand.Clamp,"CLMP"},
                {enumLoadPortCommand.UnClamp,"UCLM"},
                {enumLoadPortCommand.Mapping,"WMAP"},
                {enumLoadPortCommand.E84Load,"LOAD"},
                {enumLoadPortCommand.E84UnLoad,"UNLD"},
                {enumLoadPortCommand.SetEvent,"EVNT"},
                {enumLoadPortCommand.Reset,"RSTA"},
                {enumLoadPortCommand.Initialize,"INIT"},
                {enumLoadPortCommand.Stop,"STOP"},
                {enumLoadPortCommand.Pause,"PAUS"},
                {enumLoadPortCommand.Mode,"MODE"},
                {enumLoadPortCommand.Wtdt,"WTDT"},
                {enumLoadPortCommand.GetData,"RTDT"},
                {enumLoadPortCommand.TransferData,"TRDT"},
                {enumLoadPortCommand.Speed,"SSPD"},
                {enumLoadPortCommand.SetIO,"SPOT" },
                {enumLoadPortCommand.Status,"STAT"},
                {enumLoadPortCommand.GetIO,"GPIO"},
                {enumLoadPortCommand.GetRAC2,"RCA2.GPOS"},
                {enumLoadPortCommand.GetMappingData,"GMAP"},
                {enumLoadPortCommand.GetVersion,"GVER"},
                {enumLoadPortCommand.GetLog,"GLOG"},
                {enumLoadPortCommand.SetDateTime,"STIM"},
                {enumLoadPortCommand.GetDateTime,"GTIM"},
                {enumLoadPortCommand.GetPos,"GPOS"},
                {enumLoadPortCommand.GetType,"GWID" },
                {enumLoadPortCommand.SetType,"SWID" },
                {enumLoadPortCommand.ZaxStep,"ZAX1.STEP"},
                {enumLoadPortCommand.ZaxHome,"ZAX1.HOME"},
                {enumLoadPortCommand.YaxHome,"YAX1.HOME"},
                {enumLoadPortCommand.GetDPRM,"DPRM.GTDT" },
                {enumLoadPortCommand.SetDPRM,"DPRM.STDT" },
                {enumLoadPortCommand.GetDMPR,"DMPR.GTDT" },
                {enumLoadPortCommand.SetDMPR,"DMPR.STDT" },
                {enumLoadPortCommand.GetDCST,"DCST.GTDT" },
                {enumLoadPortCommand.SetDCST,"DCST.STDT" },
                {enumLoadPortCommand.Read,"READ"},
                {enumLoadPortCommand.WriteID,"WRIT"},
                {enumLoadPortCommand.ClientConnected,"CNCT"},
             };
        }
        #endregion
        //==============================================================================
        public SSLoadPortRB201(I_Robot robot, I_E84 e84, string strIP, int nPortID, int nBodyNo, bool bDisable, bool bSimulate, int[] nTrbMapStgNo0to399,
            string strLoadportWaferType, I_BarCode barcode, sServer sever = null)
            : base(e84, nBodyNo, bDisable, bSimulate, nTrbMapStgNo0to399, strLoadportWaferType, barcode, sever)
        {
            m_robot = robot;
            m_Socket = new sRorzeSocket(strIP, nPortID, nBodyNo, "STG", bSimulate, sever);

            OnIOChange += SSLoadPort_OnIOChange;

            _Yaxispos = enumLoadPortPos.Home;
            _Zaxispos = enumLoadPortPos.Home;

            _exePolling = new SPollingThread(1);
            _exePolling.DoPolling += _exePolling_DoPolling;

            //if (Simulate)
            {
                _gpio = new LoadPortGPIO("000000000000000", "000000000000000");
            }

            if (!Disable)
            {
                _exePolling.Set();
            }
        }
        ~SSLoadPortRB201()
        {
            _exePolling.Close();
            _exePolling.Dispose();
        }
        public override void Open() { m_Socket.Open(); }
        public override void Close() { }
        //==============================================================================
	#region =========================== E84 Handshaking =======================================
        /// <summary>
        /// 透過 RB201 GPIO DI 判斷是否正在 E84 交握
        /// </summary>
        public override bool IsCS0On
        {
            get { return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICS_0]; }
        }
        public override bool IsE84Handshaking
        {
            get
            {
                return _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICS_0] ||
                       _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DIBUSY] ||
                       _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DITR_REQ];
            }
        }
        #endregion
        //==============================================================================
        #region =========================== AutoStopE84IfNeeded ============================
        /// <summary>
        /// 若有 E84 指令正在等待交握，自動送 STOP 後清除狀態，確保後續指令可執行
        /// 若 E84 正在交握中（GPIO 訊號已亮），則拋出異常擋住新指令
        /// </summary>
        private void AutoStopE84IfNeeded()
        {
            if (IsE84CommandSent)
            {
                if (IsE84Handshaking)
                {
                    WriteLog("AutoStopE84: E84 handshaking in progress, cannot execute new command.");
                    //throw new SException((int)enumLoadPortError.E84_Handshake, "E84 handshaking in progress");
                    SendAlmMsg(enumLoadPortError.E84_Handshake);
                }
                WriteLog("AutoStopE84: E84 command pending, sending STOP first.");
                StopW(3000);
            }
        }
        #endregion
        //==============================================================================
        #region 處理TCP接收到的內容
        private void _exePolling_DoPolling()
        {
            try
            {
                int Emptycount = 0;
                string[] astrFrame;

                if (!m_Socket.QueRecvBuffer.TryDequeue(out astrFrame)) return;
                string strFrame;

                if (OnReadData != null) OnReadData(this, new MessageEventArgs(astrFrame));

                for (int nCnt = 0; nCnt < astrFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                {
                    if (astrFrame[nCnt].Length == 0)
                    {
                        Emptycount += 1;

                        continue;
                    }

                    strFrame = astrFrame[nCnt];

                    enumLoadPortCommand cmd = enumLoadPortCommand.GetVersion;
                    bool bUnknownCmd = true;

                    foreach (string scmd in _dicCmdsTable.Values) //查字典
                    {
                        if (strFrame.Contains(string.Format("STG{0}.{1}", this.BodyNo.ToString("X"), scmd)))
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
                    WriteLog(string.Format("Recv:{0}", strFrame));

                    switch (strFrame[0]) //命令種類
                    {
                        case 'c': //cancel
                            OnCancelAck(this, new LoadPortProtoclEventArgs(strFrame));
                            break;
                        case 'n': //nak
                            _signalAck[cmd].bAbnormalTerminal = true;
                            _signalAck[cmd].Set();
                            break;
                        case 'a': //ack
                            OnAck(this, new LoadPortProtoclEventArgs(strFrame));
                            _signalAck[cmd].Set();
                            break;
                        case 'e':
                            OnAck(this, new LoadPortProtoclEventArgs(strFrame));
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
        private void OnAck(object sender, LoadPortProtoclEventArgs e)
        {
            enumLoadPortCommand cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                case enumLoadPortCommand.GetMappingData:
                    AssignGMAP(e.Frame.Value);
                    break;
                case enumLoadPortCommand.Status:
                    AnalysisStatus(e.Frame.Value);
                    break;
                case enumLoadPortCommand.GetIO:
                    AnalysisGPIO(e.Frame.Value);
                    break;
                case enumLoadPortCommand.GetPos:
                    AnalysisGPOS(e.Frame.Value);
                    break;
                case enumLoadPortCommand.GetVersion:

                    break;
                case enumLoadPortCommand.GetDateTime:
                    break;
                case enumLoadPortCommand.GetType:
                    AnalysisGWID(e.Frame.Value);
                    break;
                case enumLoadPortCommand.GetRAC2:
                    AnalysisRAC2(e.Frame.Value);
                    break;
                // case PodRobotCommand.GetMappingData:
                //      if (OnRobotMappingCompleted != null)
                //          OnRobotMappingCompleted(this, new MessageEventArgs(e.Frame.Value));
                //      break;
                case enumLoadPortCommand.GetDPRM:
                    AnalysisDPRM(e.Frame.Value);
                    break;
                case enumLoadPortCommand.GetDMPR:
                    AnalysisDMPR(e.Frame.Value);
                    break;
                case enumLoadPortCommand.GetDCST:
                    AnalysisDCST(e.Frame.Value);
                    break;
				case enumLoadPortCommand.Read:
                    AnalysisRFID(e.Frame.Value);
                    break;
                case enumLoadPortCommand.ClientConnected:
                    _signalAck[cmd].Set();
                    Connected = true;
                    //  _exeClientConnecting.Set();
                    break;
                default:
                    break;
            }
        }
        private void OnCancelAck(object sender, LoadPortProtoclEventArgs e)
        {
            enumLoadPortCommand cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            // RFID 讀取無回應（錯誤代碼 0x2000 = "No response from the ID reader/writer"）
            // 不視為錯誤，不報警。清空 FoupID，正常 signal ReadW，讓上層收到空 FoupID 即可。
            if (cmd == enumLoadPortCommand.Read && e.Frame.Value == "2000")
            {
                FoupID = string.Empty;
                _signalAck[enumLoadPortCommand.Read].bAbnormalTerminal = false;
                _signalAck[enumLoadPortCommand.Read].Set();
                WriteLog("<<<RFID>>> No response from ID reader/writer (cancel 0x2000), treat as no RFID data.");
                return;
            }
            AnalysisCancel(e.Frame.Value);
        }
        private void AssignGMAP(string strFrame)
        {
            //if (strFrame.Contains("3")) // HSC for test
            //{
            //    CrossCount++;
            //    strFrame = strFrame.Replace("3", "0");
            //    _executeLogger.WriteLog($"[STG{BodyNo}] *****Cross wafer exist. Count: {CrossCount}*****");
            //}
            MappingData = strFrame;
        }
        private void AnalysisStatus(string strFrame)
        {
            if (!strFrame.Contains('/'))
            {
                WriteLog(string.Format("the format of STAT has error, '/' not found! [{0}]", strFrame));
                return;
            }
            string[] str = strFrame.Split('/');
            string s1 = str[0];
            string s2 = str[1];

            //S1.bit#1 operation mode
            switch (s1[0])
            {
                case '0':
                    m_eStatMode = enumLoadPortMode.Initializing;
                    //_signals[enumLoadPortSignalTable.Remote].Reset();
                    break;
                case '1':
                    m_eStatMode = enumLoadPortMode.Remote;
                    _signals[enumLoadPortSignalTable.Remote].Set();
                    break;
                case '2':
                    m_eStatMode = enumLoadPortMode.Maintenance;
                    _signals[enumLoadPortSignalTable.Remote].Set();
                    break;
                case '3':
                    m_eStatMode = enumLoadPortMode.Recovery;
                    break;
                default: break;
            }

            //S1.bit#2 origin return complete
            if (s1[1] == '0') _signals[enumLoadPortSignalTable.OPRCompleted].Reset();
            else _signals[enumLoadPortSignalTable.OPRCompleted].Set();
            m_bStatOrgnComplete = s1[1] == '1';

            //S1.bit#3 processing command
            if (s1[2] == '0') _signals[enumLoadPortSignalTable.ProcessCompleted].Set();
            else _signals[enumLoadPortSignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = s1[2] == '1';

            //S1.bit#4 operation status
            switch (s1[3])
            {
                case '0': m_eStatInPos = enumLoadPortStatus.InPos; break;
                case '1': m_eStatInPos = enumLoadPortStatus.Moving; break;
                case '2': m_eStatInPos = enumLoadPortStatus.Pause; break;
            }

            //S1.bit#5 operation speed
            if (s1[4] >= '0' && s1[4] <= '9') m_nSpeed = s1[4] - '0';
            else if (s1[4] >= 'A' && s1[4] <= 'K') m_nSpeed = s1[4] - 'A' + 10;
            if (m_nSpeed == 0) m_nMotionTimeout = 60000;
            else m_nMotionTimeout = 60000 * 3;

            //S2
            if (Convert.ToInt32(s2, 16) > 0)
            {
                _signals[enumLoadPortSignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumLoadPortSignalTable.MotionCompleted].Set();
                SendAlmMsg(s2);
                m_strErrCode = s2;
            }
            else
            {
                if (m_eStatInPos == enumLoadPortStatus.InPos)//運動到位               
                    _signals[enumLoadPortSignalTable.MotionCompleted].Set();
                else
                    _signals[enumLoadPortSignalTable.MotionCompleted].Reset();

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
                _signals[enumLoadPortSignalTable.MotionCompleted].bAbnormalTerminal = true;
                _signals[enumLoadPortSignalTable.MotionCompleted].Set(); //有moving過才可以Set

                SendCancelMsg(strFrame);
            }
        }
        private void AnalysisGPIO(string strFrame)
        {
            if (!strFrame.Contains('/'))
            {
                // _logger.WriteLog("<<<Error>>> the format of GPIO frame has error, [{0}]", strFrame);
                return;
            }
            _gpio = new LoadPortGPIO(strFrame.Split('/')[0], strFrame.Split('/')[1]);

            OnIOChange?.Invoke(this, new RorzenumLoadportIOChengeEventArgs(_gpio));
        }
        private void AnalysisGPOS(string strFrame)
        {
            try
            {
                if (!strFrame.Contains('/'))
                {
                    return;
                }


                _Yaxispos = (RorzeUnit.Class.Loadport.Enum.enumLoadPortPos)int.Parse(strFrame.Split('/')[0]);
                //y2
                _Zaxispos = (RorzeUnit.Class.Loadport.Enum.enumLoadPortPos)int.Parse(strFrame.Split('/')[1]);
            }
            catch (Exception ex)
            {
                WriteLog("<Exception>:" + ex);
            }
        }
        private void AnalysisDPRM(string strFrame)
        {
            try
            {
                m_strDprm = strFrame.Split(',');
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        private void AnalysisDMPR(string strFrame)
        {
            try
            {
                _sDMPRData = strFrame.Split(',');
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        private void AnalysisDCST(string strFrame)
        {
            try
            {
                strFrame = strFrame.Replace("\"", "");
                _sDCSTData = strFrame.Split(',');
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        private void AnalysisGWID(string strFrame)//  AUTO/OCP1
        {
            try
            {
                string strName = strFrame.Split('/')[1];
                eFoupType = 0;
                //  0~31會有32組，前16組正常後16組對應到Adapter
                for (int i = 0; i < _sDPRMData.Length; i++)
                {
                    if (_sDPRMData[i][16].Contains(strName))
                    {
                        eFoupType = (enumFoupType)(i % 16);//不管使不使用Adapter都是0~15                    
                        break;
                    }
                }
                FoupTypeName = strName;
            }
            catch (Exception ex)
            {
                WriteLog("<Exception>:" + ex);
            }
        }
		private void AnalysisRFID(string strFrame)// aREAD:123456
        {
            FoupID = strFrame;
        }
        private void AnalysisRAC2(string strFrame)
        {
            try
            {
                _Rac2Data = strFrame.Split(',');
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        #endregion
        //==============================================================================
        #region OneThread 
        protected override void ExeINIT()
        {
            try
            {
                WriteLog("ExeINIT:Start");
                AutoStopE84IfNeeded();
                this.InitW(m_nAckTimeout);
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
                AutoStopE84IfNeeded();

                this.ResetProcessCompleted();
                this.EventW(m_nAckTimeout);
                this.WaitProcessCompleted(3000);
                if (IsMoving)
                {
                    this.ResetProcessCompleted();
                    this.ResetInPos();
                    this.StopW(m_nAckTimeout);
                    this.WaitInPos(m_nMotionTimeout);
                    this.WaitProcessCompleted(3000);
                }			

                this.ResetChangeModeCompleted();
                this.InitW(m_nAckTimeout);
                this.WaitChangeModeCompleted(3000);

                this.StimW(m_nAckTimeout);

                this.ResetInPos();
                this.OrgnW(m_nAckTimeout);
                this.WaitInPos(m_nMotionTimeout);

				ORGN_Compltete_check = true;
                this.ExeCheckFoupExist();// 應該要重新確認

                if (Simulate)// INIT會把IO rest 可以正常辨識，模擬要直接指定
                {
                    StatusMachine = FoupExist ? enumStateMachine.PS_Arrived : enumStateMachine.PS_ReadyToLoad;
                    if (FoupExist)//Close勾住
                    {
                        _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = true;
                        _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = false;
                    }
                    else
                    {
                        _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = false;
                        _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = true;
                    }
                }
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
				AutoStopE84IfNeeded();
                if (dlgLoadInterlock != null && dlgLoadInterlock(this))
                {
                    SendAlmMsg(enumLoadPortError.InterlockStop);
                    throw new SException((int)(enumLoadPortError.InterlockStop), "Load InterlockStop");
                }

                _LoadCompletSignal.Reset();
                StatusMachine = enumStateMachine.PS_Docking;
                if (Simulate)//close勾住
                {
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = true;
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = false;
                }
                E84Status = E84PortStates.TransferBlock;

                this.ResetInPos();
                var motionType = MotionEventManager.MotionType.LoadPortDoorOpening(this.BodyNo);
                // 觸發 Motion Start
                MotionEventManager.Instance.TriggerMotionStart("Loadport", this.BodyNo, motionType);
                this.ClmpW(m_nAckTimeout);
                this.WaitInPos(m_nMotionTimeout);

                this.GmapW(m_nAckTimeout);

                _LoadCompletSignal.Set();

                StatusMachine = enumStateMachine.PS_Docked;
                if (Simulate) { AnalysisGPOS("02/02"); }

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
				AutoStopE84IfNeeded();
                E84Status = E84PortStates.TransferBlock;
                this.ResetInPos();
                this.ClmpW(3000, 1);
                this.WaitInPos(5000);

                StatusMachine = enumStateMachine.PS_Clamped;
                if (Simulate)//close勾住
                {
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = true;
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = false;
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
				AutoStopE84IfNeeded();
                this.UndockQueueByHost = false;
                //動作條件檢查
                if (dlgLoadInterlock != null && dlgLoadInterlock(this))
                {
                    SendAlmMsg(enumLoadPortError.InterlockStop);
                    throw new SException((int)(enumLoadPortError.InterlockStop), "Unload InterlockStop");
                }

                StatusMachine = enumStateMachine.PS_UnDocking;

                _UnLoadCompletSignal.Reset();
                this.ResetInPos();
                // HSC No door close event
                //var motionType = MotionEventManager.MotionType.LoadPortDoorClosing(this.BodyNo);
                //// 觸發 Motion Start
                //MotionEventManager.Instance.TriggerMotionStart("Loadport", this.BodyNo, motionType);
                this.UclmW(3000);
                this.WaitInPos(20000);
                _UnLoadCompletSignal.Set();

                StatusMachine = enumStateMachine.PS_UnDocked;
                StatusMachine = enumStateMachine.PS_UnClamped;

                if (Simulate)//close勾住
                {
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = false;
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = true;
                }

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
				AutoStopE84IfNeeded();

                this.ResetInPos();
                this.UclmW(3000, 1);
                this.WaitInPos(5000);

                if (Simulate)//close勾住
                {
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = false;
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = true;
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
                this.ResetW(3000, nMode);
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

                    bool bExist = false;

                    if (UseAdapter)
                    {
                        bExist = _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DI200mm] &&
                                 _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICommon];
                    }
                    else
                    {
                        bExist = this.IsPSPL_AllOn;
                    }

                    //have Foup 
                    if (bExist)
                    {
                        if (!FoupExist || ORGN_Compltete_check)
                        {
                            WriteLog("CheckFoupExist:Foup:[" + FoupID + "]Arrived");
                            StatusMachine = enumStateMachine.PS_FoupOn;

                            FoupExist = true;
                            if (Simulate) { AnalysisGWID("AUTO/FUP1"); }

                            _Waferlist = new List<SWafer>();

							ORGN_Compltete_check = false;
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
                        if (Simulate) { AnalysisGWID("AUTO/----"); }

                        _MappingData = "";//2022.07.08

                        _Waferlist = new List<SWafer>();

                        StatusMachine = enumStateMachine.PS_ReadyToLoad;
                        E84Status = E84PortStates.ReadytoLoad;
                        FoupID = string.Empty;
                        OnFoupExistChenge?.Invoke(this, new FoupExisteChangEventArgs(FoupExist));
                    }
					else
                    {
                        _MappingData = "";//2022.07.08

                        _Waferlist = new List<SWafer>();

                        StatusMachine = enumStateMachine.PS_ReadyToLoad;
                        E84Status = E84PortStates.ReadytoLoad;
                        FoupID = string.Empty;
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


                this.ResetInPos();
                this.WmapW(8000);
                this.WaitInPos(60000);

                this.GmapW(2000);


                _MappingCompletSignal.Set();

                if (OnMappingComplete != null)
                    OnMappingComplete(this, new LoadPortEventArgs(_MappingData, BodyNo, true));

                if (OnMappingComplete != null)
                { OnClmpComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, true)); }


            }
            catch (SException ex)
            {
                WriteLog("<<Exception>> ExeWMAP:" + ex);
                _MappingCompletSignal.bAbnormalTerminal = true;
                _MappingCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeWMAP:" + ex);
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
				AutoStopE84IfNeeded();

                _JigDockCompletSignal.Reset();

                this.ResetInPos();
                this.ZaxHomeW(5000, 2);
                this.WaitInPos(8000);

                SpinWait.SpinUntil(() => false, 500);

                this.ResetInPos();
                this.YaxHomeW(5000, 2);
                this.WaitInPos(8000);

                _JigDockCompletSignal.Set();

                OnJigDockComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, true));
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> ExeJigDock:" + ex);
                _JigDockCompletSignal.bAbnormalTerminal = true;
                _JigDockCompletSignal.Set();
                StatusMachine = enumStateMachine.PS_Error;
                OnJigDockComplete?.Invoke(this, new LoadPortEventArgs(_MappingData, BodyNo, false));
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> ExeJigDock:" + ex);
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
                //if (_e84.GetAutoMode && false == IsE84Handshaking)  // E84 Auto 非交握中 人為取放
                //{
                //    if (FoupExist)
                //    {
                //        //有FOUP PL PS訊號消失
                //        if (!e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceright] ||
                //            !e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresencemiddle] ||
                //            !e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceleft] ||
                //            !e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresence])
                //        {
                //            WriteLog("E84 Auto , Foup is Remove.");
                //            SendAlmMsg(enumLoadPortError.E84Auto_FOUPManualRemove);
                //            //return;
                //        }
                //    }
                //    else
                //    {
                //        //無FOUP PL PS訊號出現
                //        if (e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceright] ||
                //            e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresencemiddle] ||
                //            e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceleft] ||
                //            e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresence])
                //        {
                //            WriteLog("E84 Auto , Foup IO have detect.");
                //            SendAlmMsg(enumLoadPortError.E84Auto_FOUPManualDetect);
                //            //return;
                //        }
                //    }
                //}
                // --------------------------------------------------------------------------------
                if (true/*_e84.GetAutoMode == false*/)
                {
                    if (e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceright] &&   //No ->Have
                        e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresencemiddle] &&
                        e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresenceleft] &&
                        e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DIPresence]
                        && !FoupExist)
                    {
                        _threadFoupExist.Set();
                    }
                    else if (FoupExist)
                    {
                        _threadFoupExist.Set();
                    }
                    else if (UseAdapter &&
                             e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DI200mm] &&      //No ->Have      
                             e.Frame.GetDIList[LoadPortGPIO.LoadPortDI._DICommon]
                             && !FoupExist)
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
        public override void SetFoupExistChenge()
        {
            _Waferlist = new List<SWafer>();
            if (OnFoupExistChenge != null)
                OnFoupExistChenge(this, new FoupExisteChangEventArgs(FoupExist));
        }
        public override void SimulateFoupOn(bool bOn)
        {
            if (Simulate == false) return;
            string strDi = _gpio.GetDIstr;
            string strDo = _gpio.GetDOstr;
            SetStringIO(ref strDi, (int)LoadPortGPIO.LoadPortDI._DIPresence, bOn);
            SetStringIO(ref strDi, (int)LoadPortGPIO.LoadPortDI._DIPresenceleft, bOn);
            SetStringIO(ref strDi, (int)LoadPortGPIO.LoadPortDI._DIPresenceright, bOn);
            SetStringIO(ref strDi, (int)LoadPortGPIO.LoadPortDI._DIPresencemiddle, bOn);
            AnalysisGPIO(string.Format("{0}/{1}", strDi, strDo));
        }

        //==============================================================================
        #region =========================== ORGN =======================================
        private void Orgn(int nVariable)
        {
            _signalAck[enumLoadPortCommand.Orgn].Reset();
            m_Socket.SendCommand(string.Format("ORGN(" + nVariable + ")"));
        }
        public override void OrgnW(int nTimeout, int nVariable = 0)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                Orgn(nVariable);
                if (!_signalAck[enumLoadPortCommand.Orgn].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Orgn]));
                }
                if (_signalAck[enumLoadPortCommand.Orgn].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Orgn]));
                }
            }
            else
            {

            }
            IsKeepClamp = false;
            _Waferlist = new List<SWafer>();
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== CLMP =======================================
        private void Clmp(int nVariable)
        {
            try
            {
                _signalAck[enumLoadPortCommand.Clamp].Reset();
                m_Socket.SendCommand(string.Format("CLMP(" + nVariable + ")"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public override void ClmpW(int nTimeout, int nVariable = 0)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                this.ResetInPos();

                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                Clmp(nVariable);
                if (!_signalAck[enumLoadPortCommand.Clamp].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Clamp]));
                }
                if (_signalAck[enumLoadPortCommand.Clamp].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Clamp]));
                }
            }
            else
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
            _signalSubSequence.Reset();
            if (Connected)
            {
                WriteLog("ClmpW_WithoutMapping:Start");
                if (dlgLoadInterlock != null && dlgLoadInterlock(this))
                {
                    SendAlmMsg(enumLoadPortError.InterlockStop);
                    throw new SException((int)(enumLoadPortError.InterlockStop), "Load InterlockStop");
                }

                _LoadCompletSignal.Reset();
                StatusMachine = enumStateMachine.PS_Docking;
                if (Simulate)//close勾住
                {
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampClose] = true;
                    _gpio.GetDIList[LoadPortGPIO.LoadPortDI._DICarrierClampOpen] = false;
                }
                E84Status = E84PortStates.TransferBlock;


                this.ResetInPos();

                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                Clmp(nVariable);
                if (!_signalAck[enumLoadPortCommand.Clamp].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Clamp]));
                }
                if (_signalAck[enumLoadPortCommand.Clamp].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Clamp]));
                }
                this.WaitInPos(60000);//要等待LP dock完成

                _LoadCompletSignal.Set();

                StatusMachine = enumStateMachine.PS_Docked;
                if (Simulate) { AnalysisGPOS("02/02"); }

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
            else
            {
                SpinWait.SpinUntil(() => false, 100);
                if (OnSimulateCLMP != null)
                    OnSimulateCLMP(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== UCLM =======================================
        private void Uclm(int nVariable)
        {
            try
            {
                _signalAck[enumLoadPortCommand.UnClamp].Reset();

                if (nVariable == 0)//undock
                {
                    if (IsKeepClamp)
                    {
                        m_Socket.SendCommand("UCLM(0,1)");//勾住
                    }
                    else
                    {
                        m_Socket.SendCommand(string.Format("UCLM(" + nVariable + ")"));
                    }
                }
                else
                {
                    m_Socket.SendCommand(string.Format("UCLM(" + nVariable + ")"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public override void UclmW(int nTimeout, int nVariable = 0)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                Uclm(nVariable);
                if (!_signalAck[enumLoadPortCommand.UnClamp].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.UnClamp]));
                }
                if (_signalAck[enumLoadPortCommand.UnClamp].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.UnClamp]));
                }
            }
            else
            {
                SpinWait.SpinUntil(() => false, 100);
                if (OnSimulateUCLM != null)
                    OnSimulateUCLM(this, new EventArgs());
            }
            _signalSubSequence.Set();
        }
        #endregion =====================================================================

        #region =========================== WMAP =======================================
        private void Wmap()
        {
            _signalAck[enumLoadPortCommand.Mapping].Reset();
            m_Socket.SendCommand(string.Format("WMAP"));
        }
        public override void WmapW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                Wmap();
                if (!_signalAck[enumLoadPortCommand.Mapping].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Mapping]));
                }
                if (_signalAck[enumLoadPortCommand.Mapping].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Mapping]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== LOAD =======================================
        private void Load()
        {
            try
            {
                _signalAck[enumLoadPortCommand.E84Load].Reset();
                m_Socket.SendCommand(string.Format("LOAD"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public override void LoadW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
				AutoStopE84IfNeeded();
                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                Load();
                if (!_signalAck[enumLoadPortCommand.E84Load].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.E84Load]));
                }
                if (_signalAck[enumLoadPortCommand.E84Load].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.E84Load]));
                }
				IsE84CommandSent = true;
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== UNLD =======================================
        private void Unld()
        {
            try
            {
                _signalAck[enumLoadPortCommand.E84UnLoad].Reset();
                m_Socket.SendCommand(string.Format("UNLD"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public override void UnldW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                AutoStopE84IfNeeded();
                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                Unld();
                if (!_signalAck[enumLoadPortCommand.E84UnLoad].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.E84UnLoad]));
                }
                if (_signalAck[enumLoadPortCommand.E84UnLoad].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.E84UnLoad]));
                }
                IsE84CommandSent = true;
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== EVNT =======================================
        private void Event()
        {
            _signalAck[enumLoadPortCommand.SetEvent].Reset();
            m_Socket.SendCommand("EVNT(0,1)");
        }
        public override void EventW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Event();
                if (!_signalAck[enumLoadPortCommand.SetEvent].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetEvent]));
                }
                if (_signalAck[enumLoadPortCommand.SetEvent].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetEvent]));
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
            _signalAck[enumLoadPortCommand.Reset].Reset();
            m_Socket.SendCommand(string.Format("RSTA(" + nReset + ")"));
        }
        public override void ResetW(int nTimeout, int nReset = 0)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Reset(nReset);
                if (!_signalAck[enumLoadPortCommand.Reset].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Reset]));
                }
                if (_signalAck[enumLoadPortCommand.Reset].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Reset]));
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
            _signalAck[enumLoadPortCommand.Initialize].Reset();
            m_Socket.SendCommand(string.Format("INIT"));
        }
        public override void InitW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Init();
                if (!_signalAck[enumLoadPortCommand.Initialize].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Initialize]));
                }
                if (_signalAck[enumLoadPortCommand.Initialize].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Initialize]));
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
            _signalAck[enumLoadPortCommand.Stop].Reset();
            m_Socket.SendCommand(string.Format("STOP"));
        }
        public override void StopW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Stop();
                if (!_signalAck[enumLoadPortCommand.Stop].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Stop]));
                }
                if (_signalAck[enumLoadPortCommand.Stop].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Stop]));
                }
                IsE84CommandSent = false;
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
            _signalAck[enumLoadPortCommand.Pause].Reset();
            m_Socket.SendCommand(string.Format("PAUS"));
        }
        public override void PausW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Paus();
                if (!_signalAck[enumLoadPortCommand.Pause].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Pause]));
                }
                if (_signalAck[enumLoadPortCommand.Pause].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Pause]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== MODE =======================================
        private void Mode(int nMode)
        {
            _signalAck[enumLoadPortCommand.Mode].Reset();
            m_Socket.SendCommand(string.Format("MODE(" + nMode + ")"));
        }
        public override void ModeW(int nTimeout, int nMode)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Mode(nMode);
                if (!_signalAck[enumLoadPortCommand.Mode].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Mode]));
                }
                if (_signalAck[enumLoadPortCommand.Mode].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Mode]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== WTDT =======================================
        private void Wtdt()
        {
            _signalAck[enumLoadPortCommand.Wtdt].Reset();
            m_Socket.SendCommand(string.Format("WTDT"));
        }
        public override void WtdtW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Wtdt();
                if (!_signalAck[enumLoadPortCommand.Wtdt].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Wtdt]));
                }
                if (_signalAck[enumLoadPortCommand.Wtdt].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Wtdt]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SSPD =======================================
        private void Sspd(int nVariable)
        {
            _signalAck[enumLoadPortCommand.Speed].Reset();
            m_Socket.SendCommand(string.Format("SSPD(" + nVariable + ")"));
        }
        public override void SspdW(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Sspd(nVariable);
                if (!_signalAck[enumLoadPortCommand.Speed].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Speed]));
                }
                if (_signalAck[enumLoadPortCommand.Speed].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Speed]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== SPOT =======================================
        private void Spot(int nBit, bool bOn)
        {
            _signalAck[enumLoadPortCommand.SetIO].Reset();
            m_Socket.SendCommand(string.Format("SPOT({0},{1})", nBit, bOn ? 1 : 0));
        }
        public override void SpotW(int nTimeout, int nBit, bool bOn)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Spot(nBit, bOn);
                if (!_signalAck[enumLoadPortCommand.SetIO].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetIO]));
                }
                if (_signalAck[enumLoadPortCommand.SetIO].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetIO]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STAT =======================================
        private void Stat()
        {
            _signalAck[enumLoadPortCommand.Status].Reset();
            m_Socket.SendCommand(string.Format("STAT"));
        }
        public override void StatW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Stat();
                if (!_signalAck[enumLoadPortCommand.Status].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Status]));
                }
                if (_signalAck[enumLoadPortCommand.Status].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Status]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPIO =======================================
        private void Gpio()
        {
            _signalAck[enumLoadPortCommand.GetIO].Reset();
            m_Socket.SendCommand(string.Format("GPIO"));
        }
        public override void GpioW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Gpio();
                if (!_signalAck[enumLoadPortCommand.GetIO].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetIO]));
                }
                if (_signalAck[enumLoadPortCommand.GetIO].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetIO]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GMAP =======================================
        private void Gmap()
        {
            _signalAck[enumLoadPortCommand.GetMappingData].Reset();
            m_Socket.SendCommand(string.Format("GMAP"));
        }
        public override void GmapW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Gmap();
                if (!_signalAck[enumLoadPortCommand.GetMappingData].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetMappingData]));
                }
                if (_signalAck[enumLoadPortCommand.GetMappingData].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetMappingData]));
                }
            }
            else
            {
                if (OnSimulateMapping != null)
                    OnSimulateMapping(this, new EventArgs());

            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== RCA2 =======================================
        private void Rca2(int nVariable)
        {
            _signalAck[enumLoadPortCommand.GetRAC2].Reset();
            m_Socket.SendCommand(string.Format("RCA2.GPOS(" + nVariable + ")"));
        }
        public override void Rca2W(int nTimeout, int nVariable)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Rca2(nVariable);
                if (!_signalAck[enumLoadPortCommand.GetRAC2].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetRAC2]));
                }
                if (_signalAck[enumLoadPortCommand.GetRAC2].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetRAC2]));
                }
            }
            else
            {
                if (OnSimulateMapping != null)
                    OnSimulateMapping(this, new EventArgs());

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

        #region =========================== GVER =======================================
        private void Gver()
        {
            _signalAck[enumLoadPortCommand.GetVersion].Reset();
            m_Socket.SendCommand(string.Format("GVER"));
        }
        public override void GverW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Gver();
                if (!_signalAck[enumLoadPortCommand.GetVersion].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetVersion]));
                }
                if (_signalAck[enumLoadPortCommand.GetVersion].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetVersion]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STIM =======================================
        private void Stim()
        {
            _signalAck[enumLoadPortCommand.SetDateTime].Reset();
            m_Socket.SendCommand("STIM(" + DateTime.Now.ToString("yyyy, MM, dd, HH, mm, ss") + ")");
        }
        public override void StimW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Stim();
                if (!_signalAck[enumLoadPortCommand.SetDateTime].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetDateTime]));
                }
                if (_signalAck[enumLoadPortCommand.SetDateTime].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetDateTime]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GPOS =======================================
        private void Gpos()
        {
            _signalAck[enumLoadPortCommand.GetPos].Reset();
            m_Socket.SendCommand(string.Format("GPOS"));
        }
        public override void GposW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Gpos();
                if (!_signalAck[enumLoadPortCommand.GetPos].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetPos]));
                }
                if (_signalAck[enumLoadPortCommand.GetPos].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetPos]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== GWID =======================================
        private void Gwid()
        {
            _signalAck[enumLoadPortCommand.GetType].Reset();
            m_Socket.SendCommand(string.Format("GWID"));
        }
        public override void GwidW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Gwid();
                if (!_signalAck[enumLoadPortCommand.GetType].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetType]));
                }
                if (_signalAck[enumLoadPortCommand.GetType].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetType]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== READ =======================================
        private void Read()
        {
            _signalAck[enumLoadPortCommand.Read].Reset();
            m_Socket.SendCommand("READ(1,2)");
        }
        public void ReadW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                Read();
                if (!_signalAck[enumLoadPortCommand.Read].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.Read]));
                }
                if (_signalAck[enumLoadPortCommand.Read].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.Read]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== SWID =======================================
        private void Swid(string strId)
        {
            _signalAck[enumLoadPortCommand.SetType].Reset();
            m_Socket.SendCommand(string.Format("SWID(" + strId + ")"));
        }
        public override void SwidW(int nTimeout, string strId)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                Swid(strId);
                if (!_signalAck[enumLoadPortCommand.SetType].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetType]));
                }
                if (_signalAck[enumLoadPortCommand.SetType].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetType]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== STEP =======================================
        private void YaxSTEP(string strStep)
        {
            _signalAck[enumLoadPortCommand.YaxStep].Reset();
            m_Socket.SendCommand(string.Format("YAX1.STEP(" + strStep + ")"));
        }
        public override void YaxStepW(int nTimeout, string strStep)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                YaxSTEP(strStep);
                if (!_signalAck[enumLoadPortCommand.YaxStep].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.YaxStep]));
                }
                if (_signalAck[enumLoadPortCommand.YaxStep].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.YaxStep]));
                }

            }
            _signalSubSequence.Set();
        }
        private void ZaxSTEP(string strStep)
        {
            _signalAck[enumLoadPortCommand.ZaxStep].Reset();
            m_Socket.SendCommand(string.Format("ZAX1.STEP(" + strStep + ")"));
        }
        public override void ZaxStepW(int nTimeout, string strStep)
        {
            _signalSubSequence.Reset();
            _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
            ZaxSTEP(strStep);
            if (!Simulate)
            {
                if (!_signalAck[enumLoadPortCommand.ZaxStep].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.ZaxStep]));
                }
                if (_signalAck[enumLoadPortCommand.ZaxStep].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.ZaxStep]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== HOME =======================================
        private void YaxHOME(int nHome)
        {
            _signalAck[enumLoadPortCommand.YaxHome].Reset();
            m_Socket.SendCommand(string.Format("YAX1.HOME(" + nHome + ")"));
        }
        public override void YaxHomeW(int nTimeout, int nHome)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                YaxHOME(nHome);
                if (!_signalAck[enumLoadPortCommand.YaxHome].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.YaxHome]));
                }
                if (_signalAck[enumLoadPortCommand.YaxHome].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.YaxHome]));
                }
            }
            _signalSubSequence.Set();
        }
        private void ZaxHOME(int nHome)
        {
            _signalAck[enumLoadPortCommand.ZaxHome].Reset();
            m_Socket.SendCommand(string.Format("ZAX1.HOME(" + nHome + ")"));
        }
        public override void ZaxHomeW(int nTimeout, int nHome)
        {
            _signalSubSequence.Reset();
            if (Connected)
            {
                _signals[enumLoadPortSignalTable.MotionCompleted].Reset();
                ZaxHOME(nHome);
                if (!_signalAck[enumLoadPortCommand.ZaxHome].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.ZaxHome]));
                }
                if (_signalAck[enumLoadPortCommand.ZaxHome].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.ZaxHome]));
                }
            }
            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== DPRM =======================================
        private void GetDPRM(int p1)
        {
            _signalAck[enumLoadPortCommand.GetDPRM].Reset();
            m_Socket.SendCommand(string.Format("DPRM.GTDT[{0}]", p1));
        }
        public override void GetDprmW(int nTimeout, int p1)
        {
            _signalSubSequence.Reset();
            if (!Simulate || Connected)
            {
                GetDPRM(p1);
                if (!_signalAck[enumLoadPortCommand.GetDPRM].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetDPRM]));
                }
                if (_signalAck[enumLoadPortCommand.GetDPRM].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetDPRM]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        private void SetDPRM(int p1, string str)
        {
            _signalAck[enumLoadPortCommand.SetDPRM].Reset();
            m_Socket.SendCommand(string.Format("DPRM.STDT[{0}]={1}", p1, str));
        }
        public override void SetDprmW(int nTimeout, int p1, string strData)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                SetDPRM(p1, strData);
                if (!_signalAck[enumLoadPortCommand.SetDPRM].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetDPRM]));
                }
                if (_signalAck[enumLoadPortCommand.SetDPRM].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetDPRM]));
                }
            }

            _sDPRMData[p1] = strData.Split(',');

            _signalSubSequence.Set();
        }
        #endregion 

        #region =========================== DMPR =======================================
        private void GetDMPR()
        {
            _signalAck[enumLoadPortCommand.GetDMPR].Reset();
            m_Socket.SendCommand(string.Format("DMPR.GTDT"));
        }
        public override void GetDmprW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (Simulate == false)
            {
                GetDMPR();
                if (!_signalAck[enumLoadPortCommand.GetDMPR].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetDMPR]));
                }
                if (_signalAck[enumLoadPortCommand.GetDMPR].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetDMPR]));
                }
            }
            else
            {
                _sDMPRData = new string[] { "+0002500", "064", "+0000200", "+0001300", "+0000000", "00", "+0042500", "+0000000", "+0282500", "+0000000", "+0000000", "+0000000", "1", "025", "+0000000", "+0000000", "+0000000", "+0000000", "+0000000", "+0000000", "+0000000", "+0000000", "+0000000", "+0000000", "+0000000", "\"\"" };
            }

            _signalSubSequence.Set();
        }
        private void SetDMPR(int p1, string str)
        {
            _signalAck[enumLoadPortCommand.SetDMPR].Reset();
            m_Socket.SendCommand(string.Format("DMPR.STDT[{0}]={1}", p1, str));
        }
        public override void SetDmprW(int nTimeout, int p1, string strData)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                SetDMPR(p1, strData);
                if (!_signalAck[enumLoadPortCommand.SetDMPR].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetDMPR]));
                }
                if (_signalAck[enumLoadPortCommand.SetDMPR].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetDMPR]));
                }
            }

            _sDMPRData[p1] = strData;

            _signalSubSequence.Set();
        }
        #endregion

        #region =========================== DCST =======================================
        private void GetDCST()
        {
            _signalAck[enumLoadPortCommand.GetDCST].Reset();
            m_Socket.SendCommand(string.Format("DCST.GTDT"));
        }
        public override void GetDcstW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                GetDCST();
                if (!_signalAck[enumLoadPortCommand.GetDCST].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetDCST]));
                }
                if (_signalAck[enumLoadPortCommand.GetDCST].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.GetDCST]));
                }
            }
            else
            {
                //假帳
                _sDCSTData = new string[] { "FUP1", "FUP2", "FUP3", "FUP4", "FUP5", "FUP6", "FUP7", "FSB1", "FSB2", "FSB3", "FSB4", "FSB5", "OCP1", "OCP2", "OCP3", "FPO1" };
            }
            _signalSubSequence.Set();
        }
        private void SetDCST(string str)
        {
            _signalAck[enumLoadPortCommand.SetDCST].Reset();
            m_Socket.SendCommand(string.Format("DCST.STDT={0}", str));
        }
        public override void SetDCSTW(int nTimeout, string strData)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                SetDCST(strData);
                if (!_signalAck[enumLoadPortCommand.SetDCST].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumLoadPortError.AckTimeout);
                    throw new SException((int)enumLoadPortError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetDCST]));
                }
                if (_signalAck[enumLoadPortCommand.SetDCST].bAbnormalTerminal)
                {
                    SendAlmMsg(enumLoadPortError.SendCommandFailure);
                    throw new SException((int)enumLoadPortError.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumLoadPortCommand.SetDCST]));
                }
            }
            _sDCSTData = strData.Split(',');
            _signalSubSequence.Set();
        }
        #endregion

        //#region =========================== CreateMessage ==============================
        //protected override void CreateMessage()
        //{
        //    m_dicCancel[0x0200] = "0200:The operating objective is not supported";
        //    m_dicCancel[0x0300] = "0300:The composition elements of command are too few";
        //    m_dicCancel[0x0310] = "0310:The composition elements of command are too many";
        //    m_dicCancel[0x0400] = "0400:Command is not supported";
        //    m_dicCancel[0x0500] = "0500:Too few parameters";
        //    m_dicCancel[0x0510] = "0510:Too many parameters";
        //    for (int i = 0; i < 0x10; i++)
        //    {
        //        m_dicCancel[0x0600 + i] = string.Format("{0:X4}:The parameter No.{1} is too small", 0x0600 + i, i + 1);
        //        m_dicCancel[0x0610 + i] = string.Format("{0:X4}:The Parameter No.{1} is too large", 0x0610 + i, i + 1);
        //        m_dicCancel[0x0620 + i] = string.Format("{0:X4}:The Parameter No.{1} is not numeral", 0x0620 + i, i + 1);
        //        m_dicCancel[0x0630 + i] = string.Format("{0:X4}:The Parameter No.{1} is not correct", 0x0630 + i, i + 1);
        //        m_dicCancel[0x0640 + i] = string.Format("{0:X4}:The Parameter No.{1} is not a hexadecimal numeral", 0x0640 + i, i + 1);
        //        m_dicCancel[0x0650 + i] = string.Format("{0:X4}:The Parameter No.{1} is not correct", 0x0650 + i, i + 1);
        //        m_dicCancel[0x0660 + i] = string.Format("{0:X4}:The Parameter No.{1} is not pulse", 0x0660 + i, i + 1);

        //        m_dicCancel[0x1030 + i] = string.Format("{0:X4}:Interfering with No.{1} axis", 0x1030 + i, i + 1);
        //    }
        //    m_dicCancel[0x0700] = "0700:Abnormal Mode: Not ready";
        //    m_dicCancel[0x0702] = "0702:Abnormal Mode: Not in the maintenance mode";
        //    for (int i = 0; i < 0x0100; i++)
        //    {
        //        m_dicCancel[0x0800 + i] = string.Format("{0:X4}:Setting data of No.{1} is not correct!", 0x0800 + i, i + 1);
        //    }
        //    m_dicCancel[0x0920] = "0902:Improper setting";
        //    m_dicCancel[0x0A00] = "0A00:Origin search not completed";
        //    m_dicCancel[0x0A01] = "0A01:Origin reset not completed";
        //    m_dicCancel[0x0B00] = "0B00:Processing";
        //    m_dicCancel[0x0B01] = "0B01:Moving";
        //    m_dicCancel[0x0D00] = "0D00:Abnormal flash memory";
        //    m_dicCancel[0x0F00] = "0F00:Error-occurred state";
        //    m_dicCancel[0x1000] = "1000:Movement is unable due to carrier presence";
        //    m_dicCancel[0x1001] = "1001:Movement is unable due to no carrier presence";
        //    m_dicCancel[0x1002] = "1002:Improper setting";
        //    m_dicCancel[0x1003] = "1003:Improper current position";
        //    m_dicCancel[0x1004] = "1004:Movement is unable due to small designated position";
        //    m_dicCancel[0x1005] = "1005:Movement is unable due to large designated position";
        //    m_dicCancel[0x1006] = "1006:Presence of the adapter cannot be identified";
        //    m_dicCancel[0x1007] = "1007:Origin search cannot be perfomed due to abnormal presence state of the adapter";
        //    m_dicCancel[0x1008] = "1008:Adapter not prepared";
        //    m_dicCancel[0x1009] = "1009:Cover not closed";
        //    m_dicCancel[0x1100] = "1100:Emergency stop signal is ON";
        //    m_dicCancel[0x1200] = "1200:Pause signal is On./Area sensor beam is blocked";
        //    m_dicCancel[0x1300] = "1300:Interlock signal is ON";
        //    m_dicCancel[0x1400] = "1400:Driver power is OFF";
        //    m_dicCancel[0x2000] = "2000:No response from the ID reader/writer";
        //    m_dicCancel[0x2100] = "2100:Command for the ID reader/writer is cancelled";

        //    m_dicController[0x00] = "[00:Others] ";
        //    m_dicController[0x01] = "[01:Y-axis] ";
        //    m_dicController[0x02] = "[02:Z-axis] ";
        //    m_dicController[0x03] = "[03:Lifting/Lowering mechanism for reading the carrier ID] ";
        //    m_dicController[0x04] = "[04:Stage retaining mechanism] ";
        //    m_dicController[0x05] = "[05:Rotation table] ";
        //    m_dicController[0x0F] = "[06:Driver] ";

        //    for (int i = 0; i < 0x10; i++)
        //    {
        //        m_dicController[0x10 + i] = string.Format("[{0:x2}:IO unit {1}] ", 0x10 + i, i + 1);
        //    }

        //    m_dicError[0x01] = "01:Motor stall";
        //    m_dicError[0x02] = "02:Sensor abnormal";
        //    m_dicError[0x03] = "03:Emergency stop";
        //    m_dicError[0x04] = "04:Command error";
        //    m_dicError[0x05] = "05:Communication error";
        //    m_dicError[0x06] = "06:Chucking sensor abnormal";
        //    m_dicError[0x07] = "07:(Reserved)";
        //    m_dicError[0x08] = "08:Obstacle detection sensor error";
        //    m_dicError[0x09] = "09:Second origin sensor abnormal";
        //    m_dicError[0x0A] = "0A:Mapping sensor abnormal";
        //    m_dicError[0x0B] = "0B:Wafer protrusion sensor abnormal";
        //    m_dicError[0x0E] = "0E:Driver abnormal";
        //    m_dicError[0x0F] = "0F:Power abnormal";
        //    m_dicError[0x20] = "20:Control power abnormal";
        //    m_dicError[0x21] = "21:Driver power abnormal";
        //    m_dicError[0x22] = "22:EEPROM abnormal";
        //    m_dicError[0x23] = "23:Z search abnormal";
        //    m_dicError[0x24] = "24:Overheat";
        //    m_dicError[0x25] = "25:Overcurrent";
        //    m_dicError[0x26] = "26:Motor cable abnormal";
        //    m_dicError[0x27] = "27:Motor stall (position deviation)";
        //    m_dicError[0x28] = "28:Motor stall (time over)";
        //    m_dicError[0x2F] = "2F:Memory check abnormal";
        //    m_dicError[0x89] = "89:Exhaust fan abnormal";
        //    m_dicError[0x92] = "92:FOUP clamp/rotation disabled";
        //    m_dicError[0x93] = "93:FOUP unclamp/rotation disabled";
        //    m_dicError[0x94] = "94:Latch key lock disabled";
        //    m_dicError[0x95] = "95:(Reserved)";
        //    m_dicError[0x96] = "96:Latch key release disabled";
        //    m_dicError[0x97] = "97:Mapping sensor preparation disabled";
        //    m_dicError[0x98] = "98:Mapping sensor containing disabled";
        //    m_dicError[0x99] = "99:Chucking on disabled";
        //    m_dicError[0x9A] = "9A:Wafer protrusion";
        //    m_dicError[0x9B] = "9B:No cover on FOUP/With cover on FOSB";
        //    m_dicError[0x9C] = "9C:Carrier improperly taken";
        //    m_dicError[0x9D] = "9D:FSOB door detection";
        //    m_dicError[0x9E] = "9E:Carrier improperly placed";
        //    m_dicError[0xA0] = "A0:Cover lock disabled";
        //    m_dicError[0xA1] = "A1:Cover unlock disabled";
        //    m_dicError[0xB0] = "B0:TR_REQ timeout";
        //    m_dicError[0xB1] = "B1:BUSY ON timeout";
        //    m_dicError[0xB2] = "B2:Carrier carry-in timeout";
        //    m_dicError[0xB3] = "B3:Carrier carry-out timeout";
        //    m_dicError[0xB4] = "B4:BUSY OFF timeout";
        //    m_dicError[0xB5] = "B5:(Reserved)";
        //    m_dicError[0xB6] = "56:VALID OFF timeout";
        //    m_dicError[0xB7] = "B7:CONTINUE timeout";
        //    m_dicError[0xB8] = "B8:Signal abnormal detected from VALID,CS_0=ON to TR_REQ=ON";
        //    m_dicError[0xB9] = "B9:Signal abnormal detected from TR_REQ=ON to BUSY=ON";
        //    m_dicError[0xBA] = "BA:Signal abnormal detected from BUSY=ON to Placement=ON";
        //    m_dicError[0xBB] = "BB:Signal abnormal detected from Placement=ON to COMPLETE=ON";
        //    m_dicError[0xBC] = "BC:Signal abnormal detected from COMPLETE=ON to VALID=OFF";
        //    m_dicError[0xBF] = "BF:VALID, CS_0 signal abnormal";

        //    //==============================================================================
        //    _dicCmdsTable = new Dictionary<enumLoadPortCommand, string>()
        //    {
        //        {enumLoadPortCommand.Orgn,"ORGN"},
        //        {enumLoadPortCommand.Clamp,"CLMP"},
        //        {enumLoadPortCommand.UnClamp,"UCLM"},
        //        {enumLoadPortCommand.Mapping,"WMAP"},
        //        {enumLoadPortCommand.E84Load,"LOAD"},
        //        {enumLoadPortCommand.E84UnLoad,"UNLD"},
        //        {enumLoadPortCommand.SetEvent,"EVNT"},
        //        {enumLoadPortCommand.Reset,"RSTA"},
        //        {enumLoadPortCommand.Initialize,"INIT"},
        //        {enumLoadPortCommand.Stop,"STOP"},
        //        {enumLoadPortCommand.Pause,"PAUS"},
        //        {enumLoadPortCommand.Mode,"MODE"},
        //        {enumLoadPortCommand.Wtdt,"WTDT"},
        //        {enumLoadPortCommand.GetData,"RTDT"},
        //        {enumLoadPortCommand.TransferData,"TRDT"},
        //        {enumLoadPortCommand.Speed,"SSPD"},
        //        {enumLoadPortCommand.SetIO,"SPOT" },
        //        {enumLoadPortCommand.Status,"STAT"},
        //        {enumLoadPortCommand.GetIO,"GPIO"},
        //        {enumLoadPortCommand.GetRAC2,"RCA2.GPOS"},
        //        {enumLoadPortCommand.GetMappingData,"GMAP"},
        //        {enumLoadPortCommand.GetVersion,"GVER"},
        //        {enumLoadPortCommand.GetLog,"GLOG"},
        //        {enumLoadPortCommand.SetDateTime,"STIM"},
        //        {enumLoadPortCommand.GetDateTime,"GTIM"},
        //        {enumLoadPortCommand.GetPos,"GPOS"},
        //        {enumLoadPortCommand.GetType,"GWID" },
        //        {enumLoadPortCommand.SetType,"SWID" },
        //        {enumLoadPortCommand.ZaxStep,"ZAX1.STEP"},
        //        {enumLoadPortCommand.ZaxHome,"ZAX1.HOME"},
        //        {enumLoadPortCommand.YaxHome,"YAX1.HOME"},
        //        {enumLoadPortCommand.GetDPRM,"DPRM.GTDT" },
        //        {enumLoadPortCommand.SetDPRM,"DPRM.STDT" },
        //        {enumLoadPortCommand.GetDMPR,"DMPR.GTDT" },
        //        {enumLoadPortCommand.SetDMPR,"DMPR.STDT" },
        //        {enumLoadPortCommand.GetDCST,"DCST.GTDT" },
        //        {enumLoadPortCommand.SetDCST,"DCST.STDT" },
        //        {enumLoadPortCommand.ReadID,"READ"},
        //        {enumLoadPortCommand.WriteID,"WRIT"},
        //        {enumLoadPortCommand.ClientConnected,"CNCT"},
        //};
        //}
        //#endregion


    }
}
