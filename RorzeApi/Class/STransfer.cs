using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using RorzeUnit.Class;
using RorzeUnit.Interface;
using RorzeComm.Log;
using RorzeUnit.Event;
using RorzeUnit.Class.E84.Enum;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit;
using RorzeUnit.Class.E84;
using RorzeUnit.Class.Robot.Enum;
using RorzeApi.SECSGEM;
using static RorzeUnit.Class.SWafer;
using RorzeUnit.Class.Loadport.Event;
using Rorze.Secs;
using System.Windows.Forms;
using RorzeUnit.Class.Aligner.Enum;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using RorzeApi.GUI;
using System.Linq.Expressions;
using Rorze.Equipments.Unit;
using System.Timers;
using static System.Windows.Forms.AxHost;
using RorzeComm.Threading;
using static RorzeApi.SECSGEM.SProcessJobObject;
using RorzeUnit.Class.EQ.Enum;
using RorzeUnit.Class.EQ;
using static RorzeUnit.Net.Sockets.sClient;
using System.Reflection;
using RorzeUnit.Class.Robot;
using RorzeUnit.Class.ADAM;
using RorzeComm;

namespace RorzeApi.Class
{

    public class STransfer
    {
        private List<I_Robot> ListTRB;
        private List<I_Loadport> ListSTG;
        private List<I_E84> ListE84;
        private List<I_Aligner> ListALN;
        private List<I_OCR> ListOCR;
        private List<I_Buffer> ListBUF;

        private List<SSEquipment> ListEQM;
        private List<ADAM6066> ListAdam;

        SGroupRecipeManager m_grouprecipe;
        SGEM300 m_Gem;
        SProcessDB m_dbProcess;
        SAlarm m_alarm;
        PJCJManager m_JobControl;
        SSSorterSQL m_MySQL;
        SLogger _logger = SLogger.GetLogger("ExecuteLog");
        VIDManager m_VID;

        // jobAutoProcess

        SPollingThread jobAutoProcess;

        public dlgb_v dlgE84LoadUnldAllow { get; set; }

        private string m_strXYZRecipe;

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }
        private int m_nTransferCount = 0;
        bool m_bFinish = false;
        bool m_Undo = false; // 執行Undo 功能
        bool m_bUndoForReadFail = false;

        //========== constructor
        public STransfer(List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN, List<I_E84> listE84,
            List<I_OCR> listOCR, List<I_Buffer> listBUF,
            PJCJManager jobControl,
            SGroupRecipeManager grouprecipe,
            SProcessDB dbProcess,
            SAlarm alarm, SSSorterSQL mySQL, VIDManager Vid, List<SSEquipment> listEQM, List<ADAM6066> listAdam)
        {
            try
            {
                ListTRB = listTRB;
                ListSTG = listSTG;
                ListALN = listALN;
                ListE84 = listE84;
                ListOCR = listOCR;
                ListBUF = listBUF;
                m_JobControl = jobControl;

                m_grouprecipe = grouprecipe;
                m_dbProcess = dbProcess;
                m_alarm = alarm;
                m_MySQL = mySQL;
                m_VID = Vid;
                ListEQM = listEQM;
                ListAdam = listAdam;
                // job 
                jobAutoProcess = new SPollingThread(100);
                jobAutoProcess.DoPolling += JobAutoProcess_DoPolling;

                //  E84
                foreach (I_E84 e84 in ListE84)
                {
                    if (GParam.theInst.E84Type == enumE84Type.SB058)
                        e84.DoAutoProcessing += _e84_DoAutoProcessing;
                }

                //  Robot
                foreach (I_Robot item in ListTRB)
                {
                    if (item == null || item.Disable) continue;
                    item.DoAutoProcessing += _robot_DoAutoProcessing;
                }

                //  Loadport        
                foreach (I_Loadport item in ListSTG)
                {
                    if (item == null || item.Disable) continue;
                    item.DoAutoProcessing += _loadport_DoAutoProcessing;
                    item.OnProcessAbort += AutoProcessAbort;
                    foreach (I_Robot trb in ListTRB)
                    {
                        if (trb.RobotHardwareAllow(enumPosition.Loader1 + item.BodyNo - 1))
                        {
                            item.AssignToRobotQueue = trb.AssignQueue;
                            break;
                        }
                    }
                }
                //  Aligner
                foreach (I_Aligner item in ListALN)
                {
                    if (item == null || item.Disable) continue;
                    item.DoAutoProcessing += _aligner_DoAutoProcessing;
                    item.OnProcessAbort += AutoProcessAbort;
                    item.AssignToRobotQueue = (SWafer wafer) =>
                    {
                        foreach (I_Robot trb in ListTRB)
                        {
                            if (trb.RobotHardwareAllow(wafer.Position))//這裡需要思考其他形式的可能
                            {
                                trb.AssignQueue(wafer);
                                break;
                            }
                        }
                    };
                }
                //Buffer
                foreach (I_Buffer item in ListBUF)
                {
                    if (item == null || item.Disable) continue;
                    item.DoAutoProcessing += _buffer_DoAutoProcessing;
                    item.OnProcessAbort += AutoProcessAbort;
                    item.AssignToRobotQueue = (SWafer wafer) =>
                    {
                        foreach (I_Robot trb in ListTRB)
                        {
                            if (wafer.AlgnComplete == false)//送去aligner的robot
                            {
                                if (trb.RobotHardwareAllow(enumPosition.AlignerA) || trb.RobotHardwareAllow(enumPosition.AlignerB))
                                {
                                    WriteLog(string.Format("[BUF{0}]  AssignToRobot{1} slot[{2}]", item.BodyNo, trb.BodyNo, wafer.Slot));
                                    trb.AssignQueue(wafer);
                                    break;
                                }
                            }
                            else if (trb.RobotHardwareAllow(wafer.ToLoadport))//送去目標的robot
                            {
                                WriteLog(string.Format("[BUF{0}]  AssignToRobot{1} slot[{2}]", item.BodyNo, trb.BodyNo, wafer.Slot));
                                trb.AssignQueue(wafer);
                                break;
                            }
                        }
                    };
                }
                //  Equipment
                foreach (SSEquipment item in ListEQM)
                {
                    item.DoAutoProcessing += _equipment_DoAutoProcessing;
                    item.OnProcessAbort += AutoProcessAbort;
                    item.AssignToRobotQueue = (SWafer wafer) =>
                    {
                        foreach (I_Robot robot in ListTRB)
                        {
                            SWafer.enumPosition ePosition = enumPosition.EQM1 + item._BodyNo - 1;
                            if (robot.RobotHardwareAllow(ePosition))
                            {
                                robot.AssignQueue(wafer);
                                break;
                            }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]:" + ex);
            }
        }

        //==============================================================================
        void _e84_DoAutoProcessing(object sender, SB058_E84 Manual)
        {
            //取得E84
            I_E84 e84Unit = sender as I_E84;
            int nIndex = e84Unit.BodyNo - 1;
            I_Loadport loaderUnit = ListSTG[nIndex];
            try
            {
                if (loaderUnit.Disable || e84Unit.Disable || e84Unit.GetAutoMode == false) { return; }

                // LPBuiltInE84 Type
                if (loaderUnit.IsE84Handshaking) { return; } // E84交握中，不送指令
                if (loaderUnit.IsE84CommandSent) { return; } // E84指令已送出，等待交握或STOP
                if (dlgE84LoadUnldAllow != null && dlgE84LoadUnldAllow() == false) { return; } // DIO1 bit8 OFF，不允許
                if (loaderUnit.IsCS0On) { return; } // CS_0 (input 57) 仍為 ON，不送指令
                if (loaderUnit.FoupExist == false && loaderUnit.StatusMachine == enumStateMachine.PS_ReadyToLoad && loaderUnit.IsMoving == false && loaderUnit.InPos == enumLoadPortStatus.InPos)  // LOAD
                {
                    loaderUnit.LoadW(3000);
                }
                else if (loaderUnit.FoupExist == true && loaderUnit.StatusMachine == enumStateMachine.PS_ReadyToUnload && loaderUnit.IsMoving == false && loaderUnit.InPos == enumLoadPortStatus.InPos) // UNLD
                {
                    loaderUnit.UnldW(3000);
                }


                //switch (e84Unit.E84Step)
                //{
                //    case enumE84Step.Ready:
                //        {
                //            if (GParam.theInst.IsSimulate)
                //            {
                //                if (e84Unit.isSetAvbl == false)
                //                {
                //                    WriteLog(string.Format("[STG{0}]: OK HOAVBL ON", e84Unit.BodyNo));
                //                    e84Unit.SetAvbl(true);
                //                }
                //            }
                //            else
                //            {
                //                if (e84Unit.AreaTrigger || loaderUnit.IsMoving || loaderUnit.IsError
                //                    || loaderUnit.GetYaxispos != enumLoadPortPos.Home
                //                    || loaderUnit.GetZaxispos != enumLoadPortPos.Home
                //                    || loaderUnit.IsUnclamp == false)//close 是勾住
                //                {
                //                    //不能開始
                //                    if (e84Unit.isSetAvbl) e84Unit.SetAvbl(false);
                //                }
                //                else
                //                {
                //                    if (e84Unit.isSetAvbl == false)
                //                    {
                //                        WriteLog(string.Format("[STG{0}]: OK HOAVBL ON", e84Unit.BodyNo));
                //                        e84Unit.SetAvbl(true);
                //                    }
                //                }
                //            }
                //            //-------------------------------------------------------
                //            if (e84Unit.isCs0On && e84Unit.isSetAvbl && e84Unit.isSetEs)
                //            {
                //                WriteLog(string.Format("[STG{0}]: TD timer start", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.CsOn;
                //                e84Unit.TmrTD = DateTime.Now;

                //                if (loaderUnit.FoupExist
                //                    && loaderUnit.GetYaxispos == enumLoadPortPos.Home
                //                    && loaderUnit.GetZaxispos == enumLoadPortPos.Home)
                //                {
                //                    e84Unit.E84_Proc = enumE84Proc.Unloading;
                //                }
                //                else if (!loaderUnit.FoupExist
                //                         && loaderUnit.GetYaxispos == enumLoadPortPos.Home
                //                         && loaderUnit.GetZaxispos == enumLoadPortPos.Home)
                //                {
                //                    e84Unit.E84_Proc = enumE84Proc.Loading;
                //                }
                //                else
                //                {
                //                    e84Unit.E84Step = enumE84Step.StageBusy;
                //                }
                //            }
                //        }
                //        break;
                //    case enumE84Step.CsOn:
                //        {
                //            if (e84Unit.AreaTrigger)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LightCurtain", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.LightCurtain;
                //            }
                //            if (loaderUnit.IsMoving || loaderUnit.IsError || loaderUnit.IsUnclamp == false)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LP moving:{1},error:{2},clamp:{3}", e84Unit.BodyNo, loaderUnit.IsMoving, loaderUnit.IsError, !loaderUnit.IsUnclamp));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            if (loaderUnit.IsPSPL_AllOn == false && loaderUnit.IsPSPL_AllOf == false)
                //            {
                //                WriteLog(string.Format("[STG{0}]: PS:{1}, PLL:{2}, PLR:{3}, PLM:{4}", e84Unit.BodyNo, loaderUnit.IsPresenceON, loaderUnit.IsPresenceleftON, loaderUnit.IsPresencerightON, loaderUnit.IsPresencemiddleON));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }

                //            if (e84Unit.isTimeoutTD())//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP0 timeout error", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.TimeoutTD;
                //            }
                //            else if (false == e84Unit.isCs0On)//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP0 signal error Cs0 {1}", e84Unit.BodyNo, e84Unit.isCs0On));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            else if (e84Unit.isValidOn)//OK
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP1 timer start", e84Unit.BodyNo));
                //                e84Unit.TmrTP[0] = DateTime.Now;
                //                if (e84Unit.E84_Proc == enumE84Proc.Unloading)
                //                {
                //                    e84Unit.SetUReq(true);
                //                }
                //                else
                //                {
                //                    e84Unit.SetLReq(true);
                //                }
                //                e84Unit.E84Step = enumE84Step.ValidOn;
                //            }
                //        }
                //        break;
                //    case enumE84Step.ValidOn:
                //        {
                //            if (e84Unit.AreaTrigger)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LightCurtain", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.LightCurtain;
                //            }
                //            if (loaderUnit.IsMoving || loaderUnit.IsError || loaderUnit.IsUnclamp == false)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LP moving:{1},error:{2},clamp:{3}", e84Unit.BodyNo, loaderUnit.IsMoving, loaderUnit.IsError, !loaderUnit.IsUnclamp));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            if (loaderUnit.IsPSPL_AllOn == false && loaderUnit.IsPSPL_AllOf == false)
                //            {
                //                WriteLog(string.Format("[STG{0}]: PS:{1}, PLL:{2}, PLR:{3}, PLM:{4}", e84Unit.BodyNo, loaderUnit.IsPresenceON, loaderUnit.IsPresenceleftON, loaderUnit.IsPresencerightON, loaderUnit.IsPresencemiddleON));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }

                //            if (e84Unit.isTimeoutTP1())//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP1 timeout err", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.TimeoutTp1;
                //            }
                //            else if (false == e84Unit.isCs0On || false == e84Unit.isValidOn)//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP1 signal error CS0 {1}, Valid {2}", e84Unit.BodyNo, e84Unit.isCs0On, e84Unit.isValidOn));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            else if (e84Unit.isTrReqOn)//OK
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP2 timer start", e84Unit.BodyNo));
                //                e84Unit.TmrTP[1] = DateTime.Now;
                //                e84Unit.E84Step = enumE84Step.TrReq;
                //                e84Unit.SetReady(true);
                //            }
                //        }
                //        break;
                //    case enumE84Step.TrReq:
                //        {
                //            if (e84Unit.AreaTrigger)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LightCurtain", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.LightCurtain;
                //            }
                //            if (loaderUnit.IsMoving || loaderUnit.IsError || loaderUnit.IsUnclamp == false)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LP moving:{1},error:{2},clamp:{3}", e84Unit.BodyNo, loaderUnit.IsMoving, loaderUnit.IsError, !loaderUnit.IsUnclamp));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            if (loaderUnit.IsPSPL_AllOn == false && loaderUnit.IsPSPL_AllOf == false)
                //            {
                //                WriteLog(string.Format("[STG{0}]: PS:{1}, PLL:{2}, PLR:{3}, PLM:{4}", e84Unit.BodyNo, loaderUnit.IsPresenceON, loaderUnit.IsPresenceleftON, loaderUnit.IsPresencerightON, loaderUnit.IsPresencemiddleON));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }

                //            if (e84Unit.isTimeoutTP2())//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP2 Timeout error", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.TimeoutTp2;
                //            }
                //            else if (false == e84Unit.isCs0On || false == e84Unit.isValidOn || false == e84Unit.isTrReqOn)//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP2 signal error CS0 {1}, Valid {2}, TrReq {3}", e84Unit.BodyNo, e84Unit.isCs0On, e84Unit.isValidOn, e84Unit.isTrReqOn));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            else if (e84Unit.isBusyOn)//OK
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP3 timer start", e84Unit.BodyNo));
                //                e84Unit.TmrTP[2] = DateTime.Now;
                //                e84Unit.E84Step = enumE84Step.BusyTp3;
                //            }
                //        }
                //        break;
                //    case enumE84Step.BusyTp3:
                //        {
                //            if (e84Unit.AreaTrigger)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LightCurtain", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.LightCurtainBusyOn;
                //            }
                //            if (loaderUnit.IsMoving || loaderUnit.IsError)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LP moving:{1},error:{2}", e84Unit.BodyNo, loaderUnit.IsMoving, loaderUnit.IsError));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            else if (e84Unit.isTimeoutTP3())//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP3 timeout error", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.TimeoutTp3;
                //            }
                //            else if (false == e84Unit.isCs0On || false == e84Unit.isValidOn
                //                || false == e84Unit.isTrReqOn || false == e84Unit.isBusyOn)//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP3 signal error CS0 {1}, Valid {2}, TrReq {3}, Busy {4}", e84Unit.BodyNo, e84Unit.isCs0On, e84Unit.isValidOn, e84Unit.isTrReqOn, e84Unit.isBusyOn));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            else if (e84Unit.E84_Proc == enumE84Proc.Loading)//OK
                //            {
                //                if (loaderUnit.IsPSPL_AllOn)   // Check IO
                //                {
                //                    WriteLog(string.Format("[STG{0}]: TP4 timer start", e84Unit.BodyNo));
                //                    e84Unit.TmrTP[3] = DateTime.Now;
                //                    e84Unit.SetLReq(false);
                //                    e84Unit.E84Step = enumE84Step.BusyTp4;
                //                }
                //            }
                //            else if (e84Unit.E84_Proc == enumE84Proc.Unloading)//OK
                //            {
                //                if (loaderUnit.IsUnclamp == false)
                //                {
                //                    WriteLog(string.Format("[STG{0}]: LP clamp:{1}", e84Unit.BodyNo, !loaderUnit.IsUnclamp));
                //                    e84Unit.E84Step = enumE84Step.SignalError;
                //                }
                //                if (loaderUnit.IsPSPL_AllOf)   // Check IO
                //                {
                //                    WriteLog(string.Format("[STG{0}]: TP4 timer start", e84Unit.BodyNo));
                //                    e84Unit.TmrTP[3] = DateTime.Now;
                //                    e84Unit.SetUReq(false);
                //                    e84Unit.E84Step = enumE84Step.BusyTp4;
                //                }
                //            }
                //        }
                //        break;
                //    case enumE84Step.BusyTp4:
                //        {
                //            if (e84Unit.AreaTrigger)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LightCurtain", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.LightCurtainBusyOn;
                //            }
                //            if (loaderUnit.IsMoving || loaderUnit.IsError)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LP moving:{1},error:{2}", e84Unit.BodyNo, loaderUnit.IsMoving, loaderUnit.IsError));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }

                //            if (e84Unit.isTimeoutTP4())//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP4 timeout error", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.TimeoutTp4;
                //            }
                //            else if (false == e84Unit.isCs0On || false == e84Unit.isValidOn)//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP4 signal error CS0 {1}, Valid {2}", e84Unit.BodyNo, e84Unit.isCs0On, e84Unit.isValidOn));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            else if (false == e84Unit.isTrReqOn && false == e84Unit.isBusyOn && e84Unit.isComptOn)//OK
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP5 timer start", e84Unit.BodyNo));
                //                e84Unit.TmrTP[4] = DateTime.Now;
                //                e84Unit.SetReady(false);
                //                e84Unit.E84Step = enumE84Step.ComptOn;
                //            }
                //        }
                //        break;
                //    case enumE84Step.ComptOn:
                //        {
                //            if (e84Unit.AreaTrigger)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LightCurtain", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.LightCurtain;
                //            }
                //            if (loaderUnit.IsMoving || loaderUnit.IsError)
                //            {
                //                WriteLog(string.Format("[STG{0}]: LP moving:{1},error:{2}", e84Unit.BodyNo, loaderUnit.IsMoving, loaderUnit.IsError));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }

                //            if (e84Unit.isTimeoutTP5())//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP5 timeout error", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.TimeoutTp5;
                //            }
                //            else if (e84Unit.isBusyOn)//NG
                //            {
                //                WriteLog(string.Format("[STG{0}]: TP5 signal error Busy {1}", e84Unit.BodyNo, e84Unit.isBusyOn));
                //                e84Unit.E84Step = enumE84Step.SignalError;
                //            }
                //            else if (false == e84Unit.isCs0On && false == e84Unit.isValidOn && false == e84Unit.isComptOn)//OK
                //            {
                //                loaderUnit.CheckFoupExist();//通知LP E84完成FOUP通知存在
                //                e84Unit.E84Step = enumE84Step.Ready;
                //                WriteLog(string.Format("[STG{0}]: Finish", e84Unit.BodyNo));
                //            }
                //        }
                //        break;
                //    case enumE84Step.SignalError:
                //    case enumE84Step.LightCurtain:
                //    case enumE84Step.TimeoutTD:
                //    case enumE84Step.TimeoutTp1:
                //    case enumE84Step.TimeoutTp2:
                //    case enumE84Step.TimeoutTp5:
                //        {
                //            if (e84Unit.isSetAvbl) e84Unit.SetAvbl(false);

                //            if (loaderUnit.IsPSPL_AllOn == false && loaderUnit.IsPSPL_AllOf == false) { return; }//狀態不對不能做auto recovery

                //            if (!e84Unit.isValidOn
                //             && !e84Unit.isCs0On
                //             && !e84Unit.isCs1On
                //             && !e84Unit.isTrReqOn
                //             && !e84Unit.isBusyOn
                //             && !e84Unit.isComptOn
                //             && !e84Unit.isContOn
                //             && !loaderUnit.IsMoving
                //             && !loaderUnit.IsError
                //             && !e84Unit.AreaTrigger
                //             && loaderUnit.IsUnclamp)//close 是勾住
                //            {
                //                e84Unit.ResetFlag = false;
                //                WriteLog(string.Format("[STG{0}] auto recovery", e84Unit.BodyNo));
                //                e84Unit.E84Step = enumE84Step.Ready;
                //                e84Unit.ClearSignal();
                //                if (e84Unit.GetAutoMode) e84Unit.SetAvbl(true);
                //            }
                //        }
                //        break;
                //    case enumE84Step.TimeoutTp3:
                //    case enumE84Step.TimeoutTp4:
                //    case enumE84Step.LightCurtainBusyOn:
                //        {
                //            if (e84Unit.isSetAvbl) e84Unit.SetAvbl(false);

                //            if (e84Unit.ResetFlag)// SET RESET 
                //            {
                //                e84Unit.ResetFlag = false;

                //                if (!e84Unit.isValidOn
                //                 && !e84Unit.isCs0On
                //                 && !e84Unit.isCs1On
                //                 && !e84Unit.isTrReqOn
                //                 && !e84Unit.isBusyOn
                //                 && !e84Unit.isComptOn
                //                 && !e84Unit.isContOn
                //                 && !loaderUnit.IsMoving
                //                 && !loaderUnit.IsError
                //                 && !e84Unit.AreaTrigger
                //                 && loaderUnit.IsUnclamp)//close 是勾住
                //                {
                //                    WriteLog(string.Format("[STG{0}] manual reset recovery", e84Unit.BodyNo));
                //                    e84Unit.E84Step = enumE84Step.Ready;
                //                    e84Unit.ClearSignal();
                //                    if (e84Unit.GetAutoMode) e84Unit.SetAvbl(true);
                //                }
                //            }
                //        }
                //        break;
                //    case enumE84Step.StageBusy:
                //        WriteLog(string.Format("[STG{0}] E84Step : Stage is Busy", e84Unit.BodyNo));
                //        if (!e84Unit.isCs0On)
                //            e84Unit.E84Step = enumE84Step.Ready;
                //        break;
                //}
            }
            catch (SException ex)
            {
                WriteLog("[SException]<<SException>> E84 DoAutoProcessing thread:" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("[sProcess]<<Exception>> E84 DoAutoProcessing thread:" + ex);
            }
        }


        object m_obj = new object();
        static private object _objSTGJobLock = new object(); //鎖Loadport Job

        //load port自動流程
        void _loadport_DoAutoProcessing(object sender)
        {
            //取得load port
            I_Loadport loaderUnit = sender as I_Loadport;
            I_Robot robotUnit = null;
            try
            {

                #region 判斷Loadport對應的Robot
                foreach (I_Robot robot in ListTRB.ToArray())
                {
                    if (robot.RobotHardwareAllow(enumPosition.Loader1 + loaderUnit.BodyNo - 1))
                    {
                        robotUnit = robot;
                        break;
                    }
                }
                if (robotUnit == null) return;
                #endregion

                switch (GMotion.theInst.eTransfeStatus)
                {
                    /*case enumTransfeStatus.Stop:
                        foreach (SWafer wafer in loaderUnit.Waferlist)
                        {
                            if (wafer != null && wafer.ReadyToProcess == false)
                            {
                                wafer.ProcessStatus = enumProcessStatus.Cancel;
                            }
                        }
                        break;*/
                    case enumTransfeStatus.Abort:
                        foreach (SWafer wafer in loaderUnit.Waferlist)
                        {
                            if (wafer != null && wafer.ReadyToProcess == false)
                            {
                                wafer.ProcessStatus = enumProcessStatus.Abort;
                            }
                        }
                        break;
                }

                //沒有帳
                //  if (loaderUnit.StatusMachine != enumStateMachine.PS_Process)
                //   {
                //自己停掉自動流程
                //       loaderUnit.AutoProcessEnd();
                //        return;
                //    }

                if (loaderUnit.StatusMachine == enumStateMachine.PS_Complete || loaderUnit.StatusMachine == enumStateMachine.PS_Abort)
                {
                    if (loaderUnit.IsRobotExtend) return;
                    goto ReadyToUnload;//都做完可以Undocking
                }
                else if (loaderUnit.StatusMachine == enumStateMachine.PS_Process)
                {
                    goto ReadyProcess;
                }

                return;
            ReadyToUnload:
                {
                    #region ReadyToUnload
                    // WriteLog(string.Format("[STG{0}]  Unload CJID {1}.", loaderUnit.BodyNo, loaderUnit.CJID));

                    /*
                    switch (GMotion.theInst.eTransfeStatus)
                    {
                        case enumTransfeStatus.Stop:
                            loaderUnit.StatusMachine = enumStateMachine.PS_Stop;
                            break;
                        case enumTransfeStatus.Abort:
                            loaderUnit.StatusMachine = enumStateMachine.PS_Abort;
                            break;
                        default:
                            loaderUnit.StatusMachine = enumStateMachine.PS_Complete;
                            break;
                    }
                    */




                    //GMotion.theInst.eTransfeStatus = enumTransfeStatus.Idle;

                    if (m_Gem != null && m_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE &&
                        (m_VID.ECIDMappingList.Keys.Contains("DockAfterCJPort" + loaderUnit.BodyNo)
                        && (string)m_VID.ECIDList[m_VID.ECIDMappingList["DockAfterCJPort" + loaderUnit.BodyNo]].CurrentValue == "255")
                        && loaderUnit.StatusMachine == enumStateMachine.PS_Complete
                        ) return;

                    if (loaderUnit.StatusMachine == enumStateMachine.PS_Complete ||
                        loaderUnit.StatusMachine == enumStateMachine.PS_Abort ||
                        loaderUnit.StatusMachine == enumStateMachine.PS_Stop)
                    {
                        loaderUnit.UCLM();
                    }
                    else
                    {
                        WriteLog(string.Format("[STG{0}] Can't unload Foup due to state machine not is LoadComplete.", loaderUnit.BodyNo));
                    }

                    //自己停掉自動流程
                    loaderUnit.AutoProcessEnd();

                    return;
                    #endregion
                }
            ReadyProcess:
                {
                    if (GMotion.theInst.eTransfeStatus == enumTransfeStatus.Abort) return;

                    lock (_objSTGJobLock)
                    {
                        // Check Job schedule
                        foreach (SWafer ExeWafer in loaderUnit.Getjobschedule().ToArray())
                        {

                            if (ExeWafer.ReadyToProcess == true) continue;
                            if (ExeWafer.ProcessStatus != SWafer.enumProcessStatus.WaitProcess) continue;

                            #region 用數量判斷是否能派給Robot

                            int nOutSideWafer = 0;
                            foreach (SWafer w in loaderUnit.Waferlist.ToArray())
                            {
                                if (w != null && w.ReadyToProcess == true && w.ProcessStatus != enumProcessStatus.Processed)
                                {
                                    nOutSideWafer += 1;
                                }
                            }

                            //  計算能夠乘載Wafer個數
                            int nWaferCanBuffer = 0;
                            switch (ExeWafer.WaferSize)
                            {
                                case enumWaferSize.Inch08:
                                    if (robotUnit.UpperArmFunc == enumArmFunction.NORMAL) nWaferCanBuffer += 1;
                                    if (robotUnit.LowerArmFunc == enumArmFunction.NORMAL) nWaferCanBuffer += 1;
                                    break;
                                case enumWaferSize.Inch12:
                                    if (robotUnit.UpperArmFunc == enumArmFunction.NORMAL) nWaferCanBuffer += 1;
                                    if (robotUnit.LowerArmFunc == enumArmFunction.NORMAL) nWaferCanBuffer += 1;
                                    break;
                                case enumWaferSize.Frame:
                                    if (robotUnit.UpperArmFunc == enumArmFunction.FRAME) nWaferCanBuffer += 1;
                                    if (robotUnit.LowerArmFunc == enumArmFunction.FRAME) nWaferCanBuffer += 1;
                                    break;
                                case enumWaferSize.Panel:
                                    if (robotUnit.UpperArmFunc == enumArmFunction.NORMAL) nWaferCanBuffer += 1;
                                    if (robotUnit.LowerArmFunc == enumArmFunction.NORMAL) nWaferCanBuffer += 1;
                                    break;
                                default:
                                    return;
                            }
                            if (ListALN[0] != null && ListALN[0].Disable == false) nWaferCanBuffer += 1;
                            if (ListALN[1] != null && ListALN[1].Disable == false) nWaferCanBuffer += 1;
                            int EQCount = 0;
                            EQCount = ExeWafer.WApplyEQ.Count(x => x) - 1;
                            if(EQCount > 0)
                            nWaferCanBuffer += EQCount;

                            if (robotUnit.RobotHardwareAllow(ExeWafer.ToLoadport))//出發與抵達是同一隻手臂 Sorter 傳送
                            {
                                if (ListALN[0] != null && ListALN[0].Disable &&
                                    ListALN[1] != null && ListALN[1].Disable)
                                {
                                    if (nWaferCanBuffer <= nOutSideWafer)
                                        return;
                                }
                                else
                                {
                                    if (nWaferCanBuffer <= nOutSideWafer + 1)// 4 < 0+1 -> 4 < 1+1 -> 4 < 2+1 -> 4 <= 3+1 
                                        return;
                                }
                            }
                            else//wafer in out
                            {
                                if (nWaferCanBuffer <= nOutSideWafer)
                                    return;
                            }
                            #endregion


                            //手臂雙手空
                            if (robotUnit.UpperArmWafer == null && robotUnit.LowerArmWafer == null &&
                                (ExeWafer.Position == (SWafer.enumPosition.Loader1 + loaderUnit.BodyNo - 1)))
                            {

                                if (m_bFinish)
                                {
                                    ExeWafer.ReadyToProcess = false;
                                    ExeWafer.ProcessStatus = enumProcessStatus.Cancel;
                                    WriteLog(string.Format("[STG{0}] Finish wafer.{1} to processed.", loaderUnit.BodyNo, ExeWafer.Slot));
                                }

                                else
                                {
                                    ExeWafer.ReadyToProcess = true;
                                    loaderUnit.AssignToRobotQueue(ExeWafer);//丟給robot作排程
                                    WriteLog(string.Format("[STG{0}] Assign wafer.{1} to process.", loaderUnit.BodyNo, ExeWafer.Slot));

                                    loaderUnit.deletejobschedule(ExeWafer);
                                }

                            }
                            return;

                        }
                    }
                    return;
                }
            }
            catch (SException ex)
            {
                AutoProcessAbort(this, new EventArgs());
                WriteLog("[SException] Loadport DoAutoProcessing thread:" + ex);
            }
            catch (Exception ex)
            {
                AutoProcessAbort(this, new EventArgs());
                WriteLog("[Exception] Loadport DoAutoProcessing thread:" + ex);
            }
        }
        //aligner自動流程
        void _aligner_DoAutoProcessing(object sender)
        {
            //取得load port
            I_Aligner alignerManual = sender as I_Aligner;
            try
            {
                //buffer區排入schedule 
                if (alignerManual.quePreCommand.Count > 0)
                {
                    while (alignerManual.quePreCommand.Count > 0)
                    {
                        SWafer theWafer;
                        if (alignerManual.quePreCommand.TryDequeue(out theWafer))
                            alignerManual.queCommand.Enqueue(theWafer);
                    }
                }

                //是否有待處理命令
                if (alignerManual.queCommand.Count <= 0) return;

                if (alignerManual.queCommand.Count > 2)
                {
                    WriteLog(string.Format("[ALN{0}]  The command over 2 in queue.", alignerManual.BodyNo));
                }

                if (alignerManual.IsHoldPermission() == true) return;//TRUE 表示是被其他HOLD

                //處理第一筆
                SWafer waferData;
                if (alignerManual.queCommand.TryDequeue(out waferData) == false)
                {
                    WriteLog(string.Format("[ALN{0}]  TryDequeue Failure.", alignerManual.BodyNo));
                    return;
                }

                //沒有帳
                if (waferData == null)
                {
                    WriteLog(string.Format("[ALN{0}]  The wafer data is null", alignerManual.BodyNo));
                    return;
                }

                //紀錄Log                 
                WriteLog(string.Format("[ALN{0}]  Start alignment wafer", alignerManual.BodyNo));
                alignerManual.AlignmentStart = true;

                // Wafer transfer
                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                    waferData.FoupID, waferData.CJID, waferData.PJID,
                    waferData.RecipeID,
                    waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                    "Wafer alignment Start");


                #region Check Recipe


                SGroupRecipe groupRecipe = null;
                if (m_grouprecipe.GetRecipeGroupList.ContainsKey(waferData.RecipeID))
                    groupRecipe = m_grouprecipe.GetRecipeGroupList[waferData.RecipeID];
                OCRecipeData ocrRecipe_Front = null;
                OCRecipeData ocrRecipe_Back = null;

                if (groupRecipe !=null && groupRecipe._M12 != "")
                {
                    // 搜尋GroupRecipe中裡面OCR名稱的哪一個 
                    for (int i = 0; i < GParam.theInst.GetOCRRecipeIniFile(true).Count; i++)
                    {
                        if (GParam.theInst.GetOCRRecipeIniFile(true)[i].Name == groupRecipe._M12)
                        {
                            ocrRecipe_Front = GParam.theInst.GetOCRRecipeIniFile(true)[i];
                            break;
                        }
                    }
                }

                if (groupRecipe != null && groupRecipe._T7 != "")
                {
                    // 搜尋GroupRecipe中裡面OCR名稱的哪一個 
                    for (int i = 0; i < GParam.theInst.GetOCRRecipeIniFile(false).Count; i++)
                    {
                        if (GParam.theInst.GetOCRRecipeIniFile(false)[i].Name == groupRecipe._T7)
                        {
                            ocrRecipe_Back = GParam.theInst.GetOCRRecipeIniFile(false)[i];
                            break;
                        }
                    }
                }
                #endregion

                #region Check OCR
                I_OCR ORC_Front, ORC_Back;
                ORC_Front = ListOCR[(alignerManual.BodyNo - 1) * 2];
                ORC_Back = ListOCR[(alignerManual.BodyNo - 1) * 2 + 1];
                #endregion

                switch (alignerManual.WaferType)
                {
                    case enumWaferSize.Inch12:
                    case enumWaferSize.Inch08:
                    case enumWaferSize.Inch06:
                        {
                            alignerManual.ResetInPos();
                            alignerManual.Algn1W(alignerManual._AckTimeout);
                            alignerManual.WaitInPos(alignerManual._MotionTimeout);

                            if ((ocrRecipe_Front != null || ocrRecipe_Back != null))
                            {
                                #region OCR Front
                                if (ORC_Front != null && false == ORC_Front.Disable && ocrRecipe_Front != null && ocrRecipe_Front.Stored == 1)
                                {
                                    #region 轉角度
                                    string strAngle = "0";
                                    if (alignerManual.BodyNo == 1)
                                        strAngle = ocrRecipe_Front.Angle_A.ToString();
                                    else if (alignerManual.BodyNo == 2)
                                        strAngle = ocrRecipe_Front.Angle_B.ToString();

                                    double tempAngle = Double.Parse(strAngle) * 1000;

                                    alignerManual.ResetInPos();
                                    //alignerManual.AlgnDW(3000, strAngle);
                                    alignerManual.RotationExtdW(3000, (int)tempAngle);
                                    alignerManual.WaitInPos(30000);
                                    #endregion

                                    #region 讀取OCR
                                    string OcrID = "NoRead";//實體讀取                        

                                    if (!GParam.theInst.IsSimulate)
                                    {
                                        if (true == ORC_Front.OnLine())
                                        {
                                            ORC_Front.OffLine();
                                            ORC_Front.SetRecipe(ocrRecipe_Front.Name);
                                            ORC_Front.OnLine();
                                            SpinWait.SpinUntil(() => false, 1);
                                            ORC_Front.Read(ref OcrID, GParam.theInst.GetOCR_ReadSucGetImage, waferData.FoupID, waferData.LotID);
                                            //過濾
                                            if (GParam.theInst.WaferIDFilterBit > 0 && GParam.theInst.WaferIDFilterBit < OcrID.Length)
                                            {
                                                OcrID = OcrID.Substring(0, GParam.theInst.WaferIDFilterBit);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Random crandom = new Random();
                                        OcrID = crandom.Next(1, 10) > 5 ? "noread" : "ABCDEFG-" + waferData.Slot;
                                    }

                                    WriteLog(string.Format("[ALN{0}]  Front OCR Reader ID [{1}]", alignerManual.BodyNo, OcrID));

                                    bool bReadSuc;
                                    if (OcrID.IndexOf("NoRead", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("ReadFail", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("*****", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("?", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("fail", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID == "*"
                                     || OcrID == "")
                                    {
                                        bReadSuc = false;
                                        m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);

                                        switch (GParam.theInst.GetOCRReadFailProcess)
                                        {
                                            case enumOCRReadFailProcess.Continue:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                break;
                                            case enumOCRReadFailProcess.Abort:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                throw new SException((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12, "OCR M12 ReadFail");
                                            case enumOCRReadFailProcess.BackFoup:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                break;
                                            case enumOCRReadFailProcess.UserKeyIn:
                                                frmMessageBoxForReadFail frm = new frmMessageBoxForReadFail("OCR reading failure whether to terminate the process or input manually..", "Question", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, ORC_Front.SavePicturePath);
                                                if (frm.ShowDialog() == DialogResult.OK)
                                                {
                                                    OcrID = frm.GetID;
                                                    bReadSuc = true;
                                                }
                                                else
                                                {
                                                    m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Manually_KeyIn_Abort);
                                                    SpinWait.SpinUntil(() => false, 2000);
                                                    m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Manually_KeyIn_Abort);
                                                }
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//等做完解除
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        bReadSuc = true;
                                    }
                                    if (bReadSuc == false || GParam.theInst.GetOCR_ReadSucGetImage)
                                    {
                                        string strTime = DateTime.Now.ToString("yyyyMMdd HH_mm_ss");
                                        string strName = string.Format("{0} {1}_{2} {3} {4}", strTime, waferData.FoupID, waferData.Slot, ORC_Back.Name, bReadSuc ? "OK" : "NG");
                                        ORC_Front.SaveImage(strName);
                                    }
                                    #endregion

                                    waferData.WaferID_F = OcrID;

                                    #region Host ID 比對                      
                                    if (m_Gem != null && m_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE &&
                                        waferData.WaferIDComparison == enumWaferIDComparison.UnKnow)
                                    {
                                        if (bReadSuc == true)
                                        {
                                            if (OcrID == waferData.WaferInforID_F || waferData.WaferInforID_F == "")// waferData.WaferCompare_F =>Host 會給secs應該要有的ID
                                            {
                                                waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                                WriteLog(string.Format("[ALN{0}]  Transfer data Front OCR compare [{1}][{2}]", alignerManual.BodyNo, OcrID, waferData.WaferInforID_F));
                                            }
                                            else
                                            {
                                                waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                                WriteLog(string.Format("[ALN{0}]  Transfer data Front OCR failed to compare [{1}][{2}]", alignerManual.BodyNo, OcrID, waferData.WaferInforID_F));
                                                m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Result_Mismatch_With_Host_Assign);
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Result_Mismatch_With_Host_Assign);
                                            }
                                        }
                                        else
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                    }
                                    else
                                    {
                                        if (bReadSuc)
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                        else if (GParam.theInst.GetOCRReadFailProcess == enumOCRReadFailProcess.BackFoup)
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                        else
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;//預設讀失敗一樣送到EQ                               
                                    }
                                    #endregion

                                    WriteLog(string.Format("[ALN{0}]  Front OCR Algn Complete ID:{1}", alignerManual.BodyNo, OcrID));
                                    m_dbProcess.UpdateProcessWafer_M12(waferData.WaferID_F, waferData.CJID, waferData.PJID, waferData.Slot);
                                }
                                #endregion

                                #region OCR Back
                                if (ORC_Back != null && false == ORC_Back.Disable && ocrRecipe_Back != null && ocrRecipe_Back.Stored == 1)
                                {
                                    #region 轉角度
                                    string strAngle = "0";
                                    if (alignerManual.BodyNo == 1)
                                        strAngle = ocrRecipe_Back.Angle_A.ToString();
                                    else if (alignerManual.BodyNo == 2)
                                        strAngle = ocrRecipe_Back.Angle_B.ToString();

                                    double tempAngle = Double.Parse(strAngle) * 1000;

                                    alignerManual.ResetInPos();
                                    //alignerManual.AlgnDW(3000, strAngle);
                                    alignerManual.RotationExtdW(3000, (int)tempAngle);
                                    alignerManual.WaitInPos(30000);
                                    #endregion

                                    #region 讀取OCR
                                    string OcrID = "NoRead";//實體讀取                        

                                    if (!GParam.theInst.IsSimulate)
                                    {
                                        if (true == ORC_Back.OnLine())
                                        {
                                            ORC_Back.OffLine();
                                            ORC_Back.SetRecipe(ocrRecipe_Back.Name);
                                            ORC_Back.OnLine();
                                            SpinWait.SpinUntil(() => false, 1);
                                            ORC_Back.Read(ref OcrID, GParam.theInst.GetOCR_ReadSucGetImage, waferData.FoupID, waferData.LotID);
                                            //過濾
                                            if (GParam.theInst.WaferIDFilterBit > 0 && GParam.theInst.WaferIDFilterBit < OcrID.Length)
                                            {
                                                OcrID = OcrID.Substring(0, GParam.theInst.WaferIDFilterBit);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (waferData.Slot >= 100)
                                            OcrID = "ABCDEFG-" + waferData.Slot.ToString("D3");
                                        else if (waferData.Slot == 1)
                                            OcrID = "ABCDEFG-" + 2.ToString("D2");
                                        else if (waferData.Slot == 2)
                                            OcrID = "ABCDEFG-" + 1.ToString("D2");
                                        else
                                            OcrID = "ABCDEFG-" + waferData.Slot.ToString("D2");
                                    }

                                    WriteLog(string.Format("[ALN{0}]  Back OCR Reader ID [{1}]", alignerManual.BodyNo, OcrID));

                                    bool bReadSuc;
                                    if (OcrID.IndexOf("NoRead", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("ReadFail", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("*****", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("?", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("fail", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID == "*"
                                     || OcrID == "")
                                    {
                                        bReadSuc = false;
                                        m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);

                                        switch (GParam.theInst.GetOCRReadFailProcess)
                                        {
                                            case enumOCRReadFailProcess.Continue:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                break;
                                            case enumOCRReadFailProcess.Abort:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                throw new SException((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12, "OCR M12 ReadFail");
                                            case enumOCRReadFailProcess.BackFoup:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                break;
                                            case enumOCRReadFailProcess.UserKeyIn:
                                                frmMessageBoxForReadFail frm = new frmMessageBoxForReadFail("OCR reading failure whether to terminate the process or input manually..", "Question", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, ORC_Front.SavePicturePath);
                                                if (frm.ShowDialog() == DialogResult.OK)
                                                {
                                                    OcrID = frm.GetID;
                                                    bReadSuc = true;
                                                }
                                                else
                                                {
                                                    m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Manually_KeyIn_Abort);
                                                    SpinWait.SpinUntil(() => false, 2000);
                                                    m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Manually_KeyIn_Abort);
                                                }
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//等做完解除
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        bReadSuc = true;
                                    }
                                    if (bReadSuc == false || GParam.theInst.GetOCR_ReadSucGetImage)
                                    {
                                        string strTime = DateTime.Now.ToString("yyyyMMdd HH_mm_ss");
                                        string strName = string.Format("{0} {1}_{2} {3} {4}", strTime, waferData.FoupID, waferData.Slot, ORC_Back.Name, bReadSuc ? "OK" : "NG");
                                        ORC_Front.SaveImage(strName);
                                    }
                                    #endregion

                                    waferData.WaferID_B = OcrID;

                                    #region Host ID 比對          
                                    if (m_Gem != null && m_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE &&
                                        waferData.WaferIDComparison == enumWaferIDComparison.UnKnow)
                                    {

                                        if (bReadSuc == true)
                                        {
                                            if (OcrID == waferData.WaferInforID_B || waferData.WaferInforID_B == "")
                                            {
                                                waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                                WriteLog(string.Format("[ALN{0}]  Transfer data Back OCR compare [{1}][{2}]", alignerManual.BodyNo, OcrID, waferData.WaferInforID_B));
                                            }
                                            else
                                            {
                                                waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                                WriteLog(string.Format("[ALN{0}]  Transfer data Back OCR failed to compare [{1}][{2}]", alignerManual.BodyNo, OcrID, waferData.WaferInforID_B));
                                                m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Result_Mismatch_With_Host_Assign);
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Result_Mismatch_With_Host_Assign);
                                            }
                                        }
                                        else
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;

                                    }
                                    else
                                    {
                                        if (bReadSuc)
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                        else if (GParam.theInst.GetOCRReadFailProcess == enumOCRReadFailProcess.BackFoup)
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                        else
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;//不是remote就繼續
                                    }
                                    #endregion

                                    WriteLog(string.Format("[ALN{0}]  Back OCR Algn Complete ID:{1}", alignerManual.BodyNo, OcrID));
                                    m_dbProcess.UpdateProcessWafer_T7(waferData.WaferID_B, waferData.CJID, waferData.PJID, waferData.Slot);
                                }
                                #endregion                              

                                if (m_IsCycle == false)
                                {
                                    #region 傳送中讀ID失敗執行UNDO回朔
                                    if (waferData.WaferIDComparison == enumWaferIDComparison.IDAbort)//當下讀取失敗先切Flag
                                    {
                                        //  if (waferData.IsWaferTransferToStocker)//只有送去TOWER才有UNDO
                                        {
                                            WriteLog(string.Format("[ALN{0}]  m_bUndoForReadFail trigger", alignerManual.BodyNo));
                                            m_bUndoForReadFail = true;
                                        }
                                    }

                                    if (m_bUndoForReadFail)//只要Flag啟動，後續所有片都要回原本的地方
                                    {
                                        PrepareForEnd();//UNDO要強制結束

                                        //不應該要更動傳送的目標，應用WaferIDComparison判斷才是
                                        //waferData.ToLoadport = waferData.Owner;
                                        //waferData.ToSlot = waferData.Slot;
                                    }
                                    #endregion
                                }
                            }
                            else
                            {
                                waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                            }

                            int nAngle = 0/*180 - GParam.theInst.GetToEqNotchAngle*/;
                            while (nAngle < 0) { nAngle += 360; }
                            while (nAngle >= 360) { nAngle -= 360; }
                            nAngle = nAngle * 1000;

                            if (waferData.NotchAngle != -1)
                            {
                                nAngle = (int)(waferData.NotchAngle * 1000);
                                if (ListTRB[0].RobotHardwareAllow(waferData.Position))
                                {
                                    nAngle += GParam.theInst.GetNotchData(1, alignerManual.BodyNo, waferData.ToLoadport - enumFromLoader.LoadportA + 1);
                                }
                                else if (ListTRB[1].RobotHardwareAllow(waferData.Position))
                                {
                                    nAngle += GParam.theInst.GetNotchData(2, alignerManual.BodyNo, waferData.ToLoadport - enumFromLoader.LoadportA + 1);
                                }
                            }
                            else
                            {

                            }

                            alignerManual.ResetInPos();
                            //alignerManual.AlgnDW(3000, (((float)nAngle) / 1000).ToString());
                            alignerManual.RotationExtdW(3000, nAngle);
                            alignerManual.WaitInPos(30000);

                        }
                        break;
                    case enumWaferSize.Frame:
                        {
                            //alignerManual.ResetInPos();
                            alignerManual.ClmpW(3000);
                            //alignerManual.WaitInPos(30000);

                            SpinWait.SpinUntil(() => false, 1000);

                            string strBarcode;
                            //read
                            if (waferData.WaferID_F == "" && alignerManual.Raxispos == GParam.theInst.GetTurnTable_angle_0(alignerManual.BodyNo - 1))
                            {
                                strBarcode = alignerManual.BarcodeRead();
                                waferData.WaferID_F = strBarcode;
                            }

                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;

                            alignerManual.ResetInPos();
                            //若position在0度，轉180度
                            if (alignerManual.Raxispos == GParam.theInst.GetTurnTable_angle_0(alignerManual.BodyNo - 1))
                            {
                                alignerManual.Rot1EXTD(GParam.theInst.GetTurnTable_angle_180(alignerManual.BodyNo - 1));
                            }
                            else//若position在180度，轉0度
                            {
                                alignerManual.Rot1EXTD(GParam.theInst.GetTurnTable_angle_0(alignerManual.BodyNo - 1));
                            }
                            alignerManual.WaitInPos(100000);

                            //alignerManual.ResetInPos();
                            alignerManual.UclmW(3000);
                            //alignerManual.WaitInPos(30000);

                            SpinWait.SpinUntil(() => false, 1000);
                        }
                        break;
                    case enumWaferSize.Panel:
                        {

                            if (ocrRecipe_Front != null || ocrRecipe_Back != null)
                            {
                                #region OCR Front
                                if (ORC_Front != null && false == ORC_Front.Disable && ocrRecipe_Front != null && ocrRecipe_Front.Stored == 1)
                                {
                                    #region 轉角度
                                    string strAngle = "0";
                                    if (alignerManual.BodyNo == 1)
                                        strAngle = ocrRecipe_Front.Angle_A.ToString();
                                    else if (alignerManual.BodyNo == 2)
                                        strAngle = ocrRecipe_Front.Angle_B.ToString();

                                    double tempAngle = Double.Parse(strAngle);

                                    alignerManual.ResetInPos();
                                    alignerManual.AlgnDW(alignerManual._AckTimeout, strAngle);
                                    alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                    #endregion

                                    #region 讀取OCR
                                    string OcrID = "NoRead";//實體讀取                        

                                    if (!GParam.theInst.IsSimulate)
                                    {
                                        if (true == ORC_Front.OnLine())
                                        {
                                            ORC_Front.OffLine();
                                            ORC_Front.SetRecipe(ocrRecipe_Front.Name);
                                            ORC_Front.OnLine();
                                            SpinWait.SpinUntil(() => false, 1);
                                            ORC_Front.Read(ref OcrID, GParam.theInst.GetOCR_ReadSucGetImage, waferData.FoupID, waferData.LotID);
                                            //過濾
                                            if (GParam.theInst.WaferIDFilterBit > 0 && GParam.theInst.WaferIDFilterBit < OcrID.Length)
                                            {
                                                OcrID = OcrID.Substring(0, GParam.theInst.WaferIDFilterBit);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Random crandom = new Random();
                                        OcrID = crandom.Next(1, 10) > 5 ? "noread" : "ABCDEFG-" + waferData.Slot;
                                    }

                                    WriteLog(string.Format("[ALN{0}]  Front OCR Reader ID [{1}]", alignerManual.BodyNo, OcrID));

                                    bool bReadSuc;
                                    if (OcrID.IndexOf("NoRead", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("ReadFail", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("*****", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("?", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("fail", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID == "*"
                                     || OcrID == "")
                                    {
                                        bReadSuc = false;
                                        m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);

                                        switch (GParam.theInst.GetOCRReadFailProcess)
                                        {
                                            case enumOCRReadFailProcess.Continue:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                break;
                                            case enumOCRReadFailProcess.Abort:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                throw new SException((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12, "OCR M12 ReadFail");
                                            case enumOCRReadFailProcess.BackFoup:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                break;
                                            case enumOCRReadFailProcess.UserKeyIn:
                                                frmMessageBoxForReadFail frm = new frmMessageBoxForReadFail("OCR reading failure whether to terminate the process or input manually..", "Question", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, ORC_Front.SavePicturePath);
                                                if (frm.ShowDialog() == DialogResult.OK)
                                                {
                                                    OcrID = frm.GetID;
                                                    bReadSuc = true;
                                                }
                                                else
                                                {
                                                    m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Manually_KeyIn_Abort);
                                                    SpinWait.SpinUntil(() => false, 2000);
                                                    m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Manually_KeyIn_Abort);
                                                }
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//等做完解除
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        bReadSuc = true;
                                    }

                                    if (bReadSuc == false || GParam.theInst.GetOCR_ReadSucGetImage)
                                    {
                                        string strTime = DateTime.Now.ToString("yyyyMMdd HH_mm_ss");
                                        string strName = string.Format("{0} {1}_{2} {3} {4}", strTime, waferData.FoupID, waferData.Slot, ORC_Back.Name, bReadSuc ? "OK" : "NG");
                                        ORC_Front.SaveImage(strName);
                                    }
                                    #endregion

                                    waferData.WaferID_F = OcrID;

                                    #region Host ID 比對                      
                                    if (m_Gem != null && m_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE &&
                                        waferData.WaferIDComparison == enumWaferIDComparison.UnKnow)
                                    {
                                        if (bReadSuc == true)
                                        {
                                            if (OcrID == waferData.WaferInforID_F || waferData.WaferInforID_F == "")// waferData.WaferCompare_F =>Host 會給secs應該要有的ID
                                            {
                                                waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                                WriteLog(string.Format("[ALN{0}]  Transfer data Front OCR compare [{1}][{2}]", alignerManual.BodyNo, OcrID, waferData.WaferInforID_F));
                                            }
                                            else
                                            {
                                                waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                                WriteLog(string.Format("[ALN{0}]  Transfer data Front OCR failed to compare [{1}][{2}]", alignerManual.BodyNo, OcrID, waferData.WaferInforID_F));
                                                m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Result_Mismatch_With_Host_Assign);
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Result_Mismatch_With_Host_Assign);
                                            }
                                        }
                                        else
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                    }
                                    else
                                    {
                                        if (bReadSuc)
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                        else if (GParam.theInst.GetOCRReadFailProcess == enumOCRReadFailProcess.BackFoup)
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                        else
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;//預設讀失敗一樣送到EQ                               
                                    }
                                    #endregion

                                    WriteLog(string.Format("[ALN{0}]  Front OCR Algn Complete ID:{1}", alignerManual.BodyNo, OcrID));
                                    m_dbProcess.UpdateProcessWafer_M12(waferData.WaferID_F, waferData.CJID, waferData.PJID, waferData.Slot);
                                }
                                #endregion

                                #region OCR Back
                                if (ORC_Back != null && false == ORC_Back.Disable && ocrRecipe_Back != null && ocrRecipe_Back.Stored == 1)
                                {
                                    #region 轉角度
                                    string strAngle = "0";
                                    if (alignerManual.BodyNo == 1)
                                        strAngle = ocrRecipe_Back.Angle_A.ToString();
                                    else if (alignerManual.BodyNo == 2)
                                        strAngle = ocrRecipe_Back.Angle_B.ToString();

                                    double tempAngle = Double.Parse(strAngle);

                                    alignerManual.ResetInPos();
                                    alignerManual.AlgnDW(alignerManual._AckTimeout, "0"); // HSC
                                    alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                    #endregion

                                    #region 讀取OCR
                                    string OcrID = "NoRead";//實體讀取                        

                                    if (!GParam.theInst.IsSimulate)
                                    {
                                        if (true == ORC_Back.OnLine())
                                        {
                                            ORC_Back.OffLine();
                                            ORC_Back.SetRecipe(ocrRecipe_Back.Name);
                                            ORC_Back.OnLine();
                                            SpinWait.SpinUntil(() => false, 1);
                                            ORC_Back.Read(ref OcrID, GParam.theInst.GetOCR_ReadSucGetImage, waferData.FoupID, waferData.LotID);
                                            //過濾
                                            if (GParam.theInst.WaferIDFilterBit > 0 && GParam.theInst.WaferIDFilterBit < OcrID.Length)
                                            {
                                                OcrID = OcrID.Substring(0, GParam.theInst.WaferIDFilterBit);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (waferData.Slot >= 100)
                                            OcrID = "ABCDEFG-" + waferData.Slot.ToString("D3");
                                        else if (waferData.Slot == 1)
                                            OcrID = "ABCDEFG-" + 2.ToString("D2");
                                        else if (waferData.Slot == 2)
                                            OcrID = "ABCDEFG-" + 1.ToString("D2");
                                        else
                                            OcrID = "ABCDEFG-" + waferData.Slot.ToString("D2");
                                    }

                                    WriteLog(string.Format("[ALN{0}]  Back OCR Reader ID [{1}]", alignerManual.BodyNo, OcrID));

                                    bool bReadSuc;
                                    if (OcrID.IndexOf("NoRead", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("ReadFail", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("*****", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("?", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID.IndexOf("fail", StringComparison.OrdinalIgnoreCase) > -1
                                     || OcrID == "*"
                                     || OcrID == ""
                                     || OcrID == "N/A")
                                    {
                                        bReadSuc = false;
                                        m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);

                                        switch (GParam.theInst.GetOCRReadFailProcess)
                                        {
                                            case enumOCRReadFailProcess.Continue:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                break;
                                            case enumOCRReadFailProcess.Abort:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                throw new SException((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12, "OCR M12 ReadFail");
                                            case enumOCRReadFailProcess.BackFoup:
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//自動解除
                                                break;
                                            case enumOCRReadFailProcess.UserKeyIn:
                                                frmMessageBoxForReadFail frm = new frmMessageBoxForReadFail("OCR reading failure whether to terminate the process or input manually..", "Question", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, ORC_Front.SavePicturePath);
                                                if (frm.ShowDialog() == DialogResult.OK)
                                                {
                                                    OcrID = frm.GetID;
                                                    bReadSuc = true;
                                                }
                                                else
                                                {
                                                    m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Manually_KeyIn_Abort);
                                                    SpinWait.SpinUntil(() => false, 2000);
                                                    m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Manually_KeyIn_Abort);
                                                }
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Reading_Failed_M12);//等做完解除
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        bReadSuc = true;
                                    }

                                    if (bReadSuc == false || GParam.theInst.GetOCR_ReadSucGetImage)
                                    {
                                        string strTime = DateTime.Now.ToString("yyyyMMdd HH_mm_ss");
                                        string strName = string.Format("{0} {1}_{2} {3} {4}", strTime, waferData.FoupID, waferData.Slot, ORC_Back.Name , bReadSuc?"OK":"NG");
                                        ORC_Back.SaveImage(strName);
                                    }

                                    #endregion

                                    waferData.WaferID_B = OcrID;

                                    #region Host ID 比對          
                                    if (m_Gem != null && m_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE &&
                                        waferData.WaferIDComparison == enumWaferIDComparison.UnKnow)
                                    {

                                        if (bReadSuc == true)
                                        {
                                            if (OcrID == waferData.WaferInforID_B || waferData.WaferInforID_B == "")
                                            {
                                                waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                                WriteLog(string.Format("[ALN{0}]  Transfer data Back OCR compare [{1}][{2}]", alignerManual.BodyNo, OcrID, waferData.WaferInforID_B));
                                            }
                                            else
                                            {
                                                waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                                WriteLog(string.Format("[ALN{0}]  Transfer data Back OCR failed to compare [{1}][{2}]", alignerManual.BodyNo, OcrID, waferData.WaferInforID_B));
                                                m_alarm.writeAlarm((int)SAlarm.enumAlarmCode.OCR_Result_Mismatch_With_Host_Assign);
                                                SpinWait.SpinUntil(() => false, GParam.theInst.OCRWarningsAutoRestTime);
                                                m_alarm.AlarmReset((int)SAlarm.enumAlarmCode.OCR_Result_Mismatch_With_Host_Assign);
                                            }
                                        }
                                        else
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;

                                    }
                                    else
                                    {
                                        if (bReadSuc)
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                        else if (GParam.theInst.GetOCRReadFailProcess == enumOCRReadFailProcess.BackFoup)
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAbort;
                                        else
                                            waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;//不是remote就繼續
                                    }
                                    #endregion

                                    WriteLog(string.Format("[ALN{0}]  Back OCR Algn Complete ID:{1}", alignerManual.BodyNo, OcrID));
                                    m_dbProcess.UpdateProcessWafer_T7(waferData.WaferID_B, waferData.CJID, waferData.PJID, waferData.Slot);
                                }
                                #endregion                              

                                if (m_IsCycle == false)
                                {
                                    #region 傳送中讀ID失敗執行UNDO回朔
                                    if (waferData.WaferIDComparison == enumWaferIDComparison.IDAbort)//當下讀取失敗先切Flag
                                    {
                                        //  if (waferData.IsWaferTransferToStocker)//只有送去TOWER才有UNDO
                                        {
                                            WriteLog(string.Format("[ALN{0}]  m_bUndoForReadFail trigger", alignerManual.BodyNo));
                                            m_bUndoForReadFail = true;
                                        }
                                    }

                                    if (m_bUndoForReadFail)//只要Flag啟動，後續所有片都要回原本的地方
                                    {
                                        PrepareForEnd();//UNDO要強制結束

                                        //不應該要更動傳送的目標，應用WaferIDComparison判斷才是
                                        //waferData.ToLoadport = waferData.Owner;
                                        //waferData.ToSlot = waferData.Slot;
                                    }
                                    #endregion
                                }


                            }


                            if (waferData.NotchAngle != -1)
                            {
                                // A ┌───────┐ B
                                //   │       │   
                                //   │       │   
                                // D └───────┘ C
                                //      ╚╦╝ FINGER
                                //       ║                          
                                switch (waferData.GetUsingEQ)
                                {
                                    case enumPosition.EQM1:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, waferData.NotchAngle.ToString());//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                    case enumPosition.EQM2:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, waferData.NotchAngle.ToString());//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                    case enumPosition.EQM3:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, waferData.NotchAngle.ToString());//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                    case enumPosition.EQM4:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, waferData.NotchAngle.ToString());//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;

                                    default:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, waferData.NotchAngle.ToString());//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                }

                                waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                            }


                            if (ocrRecipe_Front == null && ocrRecipe_Back == null && waferData.NotchAngle == -1)//甚麼都不要~
                            {
                                switch (waferData.GetUsingEQ)
                                {
                                    case enumPosition.EQM1:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, "0");//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                    case enumPosition.EQM2:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, "0");//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                    case enumPosition.EQM3:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, "0");//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                    case enumPosition.EQM4:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, "0");//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                    default:
                                        alignerManual.ResetInPos();
                                        alignerManual.AlgnDW(alignerManual._AckTimeout, "0");//0~360
                                        alignerManual.WaitInPos(alignerManual._MotionTimeout);
                                        break;
                                }
                                waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                            }
                        }
                        break;
                }


                alignerManual.AligCompelet(waferData);//waferData.AlgnComplete = true 通知
                WriteLog(string.Format("[ALN{0}] Algn Complete", alignerManual.BodyNo));

                //host給的id跟我讀到的不一樣
                if (m_Gem != null && m_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE &&
                    waferData.WaferIDComparison == enumWaferIDComparison.UnKnow &&
                    m_Gem.ComparisonStatus != enumComparisonStatus.WaferUnKnow)
                {
                    WriteLog(string.Format("[ALN{0}]  Transfer Wafer ID Comparison to Confirm By Host", alignerManual.BodyNo));

                    switch (m_Gem.ComparisonStatus)
                    {
                        case enumComparisonStatus.WaferResume:
                            alignerManual.Wafer.WaferIDComparison = SWafer.enumWaferIDComparison.IDAgree;
                            break;
                        case enumComparisonStatus.WaferCancel:
                            alignerManual.Wafer.WaferIDComparison = SWafer.enumWaferIDComparison.IDAbort;
                            break;
                    }

                    alignerManual.ResetInPos();
                    alignerManual.UclmW(3000);
                    alignerManual.WaitInPos(30000);

                    // Wafer transfer
                    m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                        waferData.FoupID, waferData.CJID, waferData.PJID,
                        waferData.RecipeID,
                        waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                        "Wafer alignment Complete");

                    alignerManual.AlignmentStart = false;

                    alignerManual.AssignToRobotQueue(waferData);//丟給robot作排程
                    m_Gem.ComparisonStatus = enumComparisonStatus.WaferUnKnow;
                    System.Threading.SpinWait.SpinUntil(() => false, 100);
                }
                else if (waferData.WaferIDComparison != enumWaferIDComparison.UnKnow)
                {
                    WriteLog(string.Format("[ALN{0}]  Transfer Wafer ID Comparison to Confirm", alignerManual.BodyNo));
                    alignerManual.ResetInPos();
                    alignerManual.UclmW(3000);
                    alignerManual.WaitInPos(30000);

                    alignerManual.AlignmentStart = false;

                    // Wafer transfer
                    m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                        waferData.FoupID, waferData.CJID, waferData.PJID,
                        waferData.RecipeID,
                        waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                        "Wafer alignment Complete");
                    alignerManual.AssignToRobotQueue(waferData);//丟給robot作排程
                    System.Threading.SpinWait.SpinUntil(() => false, 100);
                }
                else//secs 還沒給我比對失敗處理
                {
                    goto DontMove;
                }

                return;
            DontMove:
                //條件不滿足, 重新排隊
                alignerManual.queCommand.Enqueue(waferData);
                return;
            }
            catch (SException ex)
            {
                AutoProcessAbort(this, new EventArgs());

                WriteLog("[SException] Aligner DoAutoProcessing thread:" + ex);
            }
            catch (Exception ex)
            {
                AutoProcessAbort(this, new EventArgs());

                WriteLog("[Exception] Aligner DoAutoProcessing thread:" + ex);
            }
        }
        //robot自動流程
        void _robot_DoAutoProcessing(object sender)
        {
            try
            {
                //取得robot
                I_Robot robotManual = sender as I_Robot;

                if (robotManual.IsMoving == true) return;



                if (GMotion.theInst.eTransfeStatus == enumTransfeStatus.Pause) return;

                //buffer區排入schedule 
                if (robotManual.quePreCommand.Count > 0)
                {
                    lock (robotManual.objLockQueue) //hold loader and chamber to assign command at this moment.
                    {
                        while (robotManual.quePreCommand.Count > 0)
                        {
                            SWafer theWafer;
                            if (robotManual.quePreCommand.TryDequeue(out theWafer) == false) continue;
                            robotManual.queCommand.Enqueue(theWafer);
                        }
                    }
                }

                //是否有待處理命令
                if (robotManual.queCommand.Count <= 0 && robotManual.quePreCommand.Count <= 0)
                {
                    //流程結束要將Robot的序給停止                   
                    if (m_JobControl.CJlist.Count == 0)
                    {
                        WriteLog(string.Format("[TRB{0}]:Auto Process END.", robotManual.BodyNo));
                        AutoProcessEnd(this, new EventArgs());
                    }
                    return;
                }

                if (robotManual.queCommand.Count > 26) WriteLog(string.Format("[TRB{0}]:the commands over 26 in queue.", robotManual.BodyNo));

                if (robotManual.GetRunningPermissionForStgMap() == false) return;//與Loadport互卡使用權

                //處理第一筆
                SWafer waferData;
                if (robotManual.queCommand.TryDequeue(out waferData) == false)
                {
                    return;
                    //throw new Exception(string.Format("[TRB{0}]:TryDequeue fail", robotManual.BodyNo));
                }

                //chech  Random by host 

                //多取下一個看看
                SWafer waferData_Prepare;
                if (robotManual.queCommand.TryPeek(out waferData_Prepare) == false)
                {
                    SpinWait.SpinUntil(() => false, 100);
                    if (robotManual.quePreCommand.Count > 0) goto DontMove;
                }

                ////20250923 HSC
                //bool[] EQEnableFlags;
                //if (waferData.RecipeID == "")
                //{
                //    EQEnableFlags = null;
                //}
                //else
                //{
                //    SGroupRecipe rcpeContent = m_grouprecipe.GetRecipeGroupList[waferData.RecipeID];
                //    m_strXYZRecipe = rcpeContent.GetEQ_Recipe()[0];
                //    EQEnableFlags = rcpeContent.GetEQ_ProcessEnable();
                //}

                enumRobotArms arm = enumRobotArms.Empty;
                int nStgeIndx = -1;
                //from?
                switch (waferData.Position)
                {
                    case SWafer.enumPosition.Loader1: //to fork
                    case SWafer.enumPosition.Loader2: //to fork
                    case SWafer.enumPosition.Loader3: //to fork
                    case SWafer.enumPosition.Loader4: //to fork
                    case SWafer.enumPosition.Loader5: //to fork
                    case SWafer.enumPosition.Loader6: //to fork
                    case SWafer.enumPosition.Loader7: //to fork
                    case SWafer.enumPosition.Loader8: //to fork
                        #region Loader
                        {
                            //interlock 撞機檢查
                            int nLoadportIndex = waferData.Position - SWafer.enumPosition.Loader1;
                            if (ListSTG[nLoadportIndex].StatusMachine != enumStateMachine.PS_Process)
                                throw new Exception(string.Format("[TRB{0}]:Loadport{1} statusMachine not process", robotManual.BodyNo, nLoadportIndex + 1));

                            if (waferData != null && waferData.ProcessStatus != enumProcessStatus.WaitProcess)
                            {
                                if (waferData.ProcessStatus == enumProcessStatus.Cancel && m_bFinish == true)
                                {
                                    WriteLog(string.Format("Wafer Soruce port = {0} , Source Slot = {1} status Cancel and Finish ,So Do move ...", waferData.FoupID, waferData.Slot));
                                    waferData.Robotorder = false;
                                    robotManual.ReleaseRunningPermissionForStgMap();
                                    return;
                                }
                                throw new Exception(string.Format("[TRB{0}]:waferData status is'not WaitProcess", robotManual.BodyNo));
                            }

                            if (waferData_Prepare != null && waferData_Prepare.ProcessStatus != enumProcessStatus.WaitProcess && waferData_Prepare.ProcessStatus != enumProcessStatus.Processing)
                            {
                                if (waferData_Prepare.ProcessStatus == enumProcessStatus.Cancel && m_bFinish == true)
                                {
                                    WriteLog(string.Format("waferData_Prepare Soruce port = {0} , Source Slot = {1} status Cancel and Finish ,So Do move ...", waferData_Prepare.FoupID, waferData_Prepare.Slot));
                                    waferData_Prepare.Robotorder = false;
                                    robotManual.ReleaseRunningPermissionForStgMap();
                                    return;
                                }
                                throw new Exception(string.Format("[TRB{0}]:waferData status is'not WaitProcess", robotManual.BodyNo));
                            }

                            bool bHardwareHasALN = false;
                            bool bALN_Empty = false;
                            foreach (I_Aligner aln in ListALN.ToArray())
                            {
                                bHardwareHasALN |= (aln != null && aln.Disable == false);
                                bALN_Empty |= (aln != null && aln.Disable == false && aln.Wafer == null);
                            }

                            if (bHardwareHasALN == false)
                            {
                                //沒有Aligner，單純loadport傳送
                            }
                            else
                            {
                                //  Sorter傳送要考慮的，手上已經有standby的Wafer即跳開(手上有片且沒過製成不能再去取)
                                if (robotManual.UpperArmWafer != null && robotManual.UpperArmWafer.ProcessStatus == SWafer.enumProcessStatus.Processing) goto DontMove;
                                if (robotManual.LowerArmWafer != null && robotManual.LowerArmWafer.ProcessStatus == SWafer.enumProcessStatus.Processing) goto DontMove;
                            }

                            //  卡控上下Finger Type 不相同且Alinger上面有Wafer
                            if (robotManual.UpperArmFunc != robotManual.LowerArmFunc)
                            {
                                if (ListALN[0] != null && ListALN[0].Wafer != null) goto DontMove;
                                if (ListALN[1] != null && ListALN[1].Wafer != null) goto DontMove;
                            }
                            //  卡控上下Finger Type 不相同，Frame type因為從EQ出來還要去Turntable，所以一次只能出一片wafer
                            if (robotManual.UpperArmFunc != robotManual.LowerArmFunc
                                //&& waferData.WaferSize == enumWaferSize.Frame
                                && ListEQM.Any(eqm => eqm.Wafer != null)
                                ) goto DontMove;

                            //  Wafer Size 找用哪一隻手臂
                            switch (waferData.WaferSize)
                            {
                                case enumWaferSize.Inch12:
                                case enumWaferSize.Inch08:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去LP取片
                                    if (arm == enumRobotArms.Empty)
                                        arm = robotManual.GetAvailableArm(enumArmFunction.I);//找去LP取片
                                    break;
                                case enumWaferSize.Frame:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.FRAME);//找去LP取片
                                    break;
                                case enumWaferSize.Panel:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去LP取片
                                    break;

                            }

                            switch (arm)
                            {
                                case enumRobotArms.UpperArm: robotManual.PrepareUpperWafer = waferData; break;
                                case enumRobotArms.LowerArm: robotManual.PrepareLowerWafer = waferData; break;
                                default: goto DontMove; //沒有可用的arm
                            }

                            int nSlot = waferData.Slot;
                            //考慮雙取
                            if (waferData != null)
                                waferData.Robotorder = true;

                            if (robotManual.UseArmSameMovement && bALN_Empty)
                            {
                                if (robotManual.UpperArmFunc == robotManual.LowerArmFunc &&
                                    robotManual.UpperArmWafer == null &&
                                    robotManual.LowerArmWafer == null &&
                                    waferData_Prepare != null)
                                {
                                    //假如取slot3 slot4 Load要下 slot4
                                    if ((waferData_Prepare.Slot - waferData.Slot) == 1 && waferData.Position == waferData_Prepare.Position)
                                    {
                                        nSlot = waferData_Prepare.Slot;
                                        arm = enumRobotArms.BothArms;//取片
                                        robotManual.PrepareUpperWafer = waferData_Prepare;
                                        robotManual.PrepareLowerWafer = waferData;

                                        if (waferData_Prepare != null)
                                            waferData_Prepare.Robotorder = true;

                                    }
                                    if ((waferData.Slot - waferData_Prepare.Slot) == 1 && waferData.Position == waferData_Prepare.Position)
                                    {
                                        arm = enumRobotArms.BothArms;//取片
                                        robotManual.PrepareUpperWafer = waferData;
                                        robotManual.PrepareLowerWafer = waferData_Prepare;

                                        if (waferData_Prepare != null)
                                            waferData_Prepare.Robotorder = true;
                                    }
                                }
                            }

                            //================================================== 取片

                            WriteLog(string.Format("[TRB{0}]:Take wafer from stage[{1}] slot[{2}] Arm[{3}].", robotManual.BodyNo, waferData.Position, waferData.Slot, arm));
                            m_dbProcess.CreateProcessWafer(DateTime.Now,
                                waferData.FoupID, waferData.CJID, waferData.PJID,
                                waferData.RecipeID,
                                waferData.Slot, waferData.WaferID_F, waferData.WaferID_B);

                            // Wafer transfer 
                            m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                waferData.FoupID, waferData.CJID, waferData.PJID,
                                waferData.RecipeID,
                                waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                string.Format("The robot_{0} take wafer from {1} slot{2} use {3}", robotManual.BodyNo, waferData.Position, waferData.Slot, arm));

                            if (arm == enumRobotArms.BothArms)
                            {
                                m_dbProcess.CreateProcessWafer(DateTime.Now,
                                    waferData_Prepare.FoupID, waferData_Prepare.CJID, waferData_Prepare.PJID,
                                    waferData_Prepare.RecipeID,
                                    waferData_Prepare.Slot, waferData_Prepare.WaferID_F, waferData_Prepare.WaferID_B);
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData_Prepare.FoupID, waferData_Prepare.CJID, waferData_Prepare.PJID,
                                    waferData_Prepare.RecipeID,
                                    waferData_Prepare.Slot, waferData_Prepare.WaferID_F, waferData_Prepare.WaferID_B,
                                    string.Format("The robot_{0} take wafer from {1} slot{2} use {3}", robotManual.BodyNo, waferData_Prepare.Position, waferData_Prepare.Slot, arm));
                            }

                            #region 判斷 loadport 對應到 robot address

                            RobotPos pos = RobotPos.LoadPort1;

                            switch (waferData.Position)
                            {
                                case SWafer.enumPosition.Loader1:
                                case SWafer.enumPosition.Loader2:
                                case SWafer.enumPosition.Loader3:
                                case SWafer.enumPosition.Loader4:
                                case SWafer.enumPosition.Loader5:
                                case SWafer.enumPosition.Loader6:
                                case SWafer.enumPosition.Loader7:
                                case SWafer.enumPosition.Loader8:
                                    {
                                        I_Loadport loader = ListSTG[waferData.Position - SWafer.enumPosition.Loader1];
                                        nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, waferData.Position, loader.UseAdapter) + (int)loader.eFoupType;
                                        pos = RobotPos.LoadPort1 + loader.BodyNo - 1;
                                    }
                                    break;
                                default: throw new Exception(string.Format("[TRB{0}]:waferData position incorrect", robotManual.BodyNo));
                            }
                            #endregion

                            //move standby pos
                            robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, false, waferData.Position, arm, nStgeIndx, nSlot);
                            robotManual.SetCurrePos = pos;

                            //move robot to pick
                            //robotManual.TakeWaferByInterLockW(robotManual.GetAckTimeout, arm, nStgeIndx, nSlot);
                            //if (arm == enumRobotArms.UpperArm)
                            if (GParam.theInst.GetRobotAlignment_Enable()) // HSC ROBOT Alignment
                            {
                                robotManual.TakeWaferAlignmentByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, waferData.Position, nStgeIndx, nSlot, waferData);
                                int nAlexAddress = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALEX);
                                robotManual.ResetInPos();
                                robotManual.AlexW(robotManual.GetAckTimeout, arm, nAlexAddress, 1, 0, 0);
                                robotManual.WaitInPos(robotManual.GetMotionTimeout);
                            }
                            else
                            {
                                robotManual.TakeWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, waferData.Position, nStgeIndx, nSlot, waferData);
                            }
                            //else
                            //robotManual.TwoStepTakeWaferW(robotManual.GetAckTimeout, arm, GParam.theInst.GetRobot_FrameTwoStepLoadArmBackPulse(robotManual.BodyNo - 1), nStgeIndx, nSlot);

                            //取出來從waitprocess轉成Processing                
                            waferData.ProcessStatus = SWafer.enumProcessStatus.Processing;
                            //robot load完成資料需要塞回Queue
                            robotManual.queCommand.Enqueue(waferData);
                            WriteLog("[Demo]:get wafer ");
                            if (arm == enumRobotArms.BothArms)
                            {
                                WriteLog("[Demo]:get wafer BothArms");
                                waferData_Prepare.ProcessStatus = SWafer.enumProcessStatus.Processing;//取出來從waitprocess轉成Processing
                                robotManual.queCommand.TryDequeue(out waferData_Prepare);
                                robotManual.quePreCommand.Enqueue(waferData_Prepare);
                            }
                            m_nTransferCount++;
                            WriteLog(string.Format("[TRB{0}]:Transfer Wafer Quantity :[{1}].", robotManual.BodyNo, m_nTransferCount));

                        }
                        break;
                    #endregion
                    case SWafer.enumPosition.UpperArm:
                    case SWafer.enumPosition.LowerArm:
                        #region Arm
                        SGroupRecipe rcpeContent = m_grouprecipe.GetRecipeGroupList[waferData.RecipeID];
                        bool[] EQEnableFlags = rcpeContent.GetEQ_ProcessEnable();
                        if (waferData.ProcessStatus == SWafer.enumProcessStatus.Processing) //================================================== 進貨
                        {
                            arm = waferData.Position == SWafer.enumPosition.UpperArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm;

                            bool bAllowALN = robotManual.RobotHardwareAllow(enumPosition.AlignerA) && ListALN[0] != null && ListALN[0].Disable == false;
                            bAllowALN |= robotManual.RobotHardwareAllow(enumPosition.AlignerB) && ListALN[1] != null && ListALN[1].Disable == false;



                            //  Wafer需要做Align，機構允許送入Aligner
                            if (waferData.AlgnComplete == false && waferData.WaferSize != enumWaferSize.Frame && bAllowALN)
                            {
                                #region Put Aligner   
                                //================================================== aligner放片

                                bool bNeedOCR1 = false, bNeedOCR2 = false;
                                if (m_grouprecipe.GetRecipeGroupList.ContainsKey(waferData.RecipeID) == true)
                                {
                                    SGroupRecipe RecipeContent = m_grouprecipe.GetRecipeGroupList[waferData.RecipeID];
                                    bNeedOCR1 = RecipeContent._M12 != "";
                                    bNeedOCR2 = RecipeContent._T7 != "";
                                }

                                I_Aligner aligner;
                                RobotPos pos;
                                enumPosition ePos = enumPosition.UnKnow;

                                if (bNeedOCR1 && !ListOCR[0].Disable && !ListALN[0].Disable && ListALN[0].Wafer == null && robotManual.RobotHardwareAllow(enumPosition.AlignerA))
                                {
                                    if (!robotManual.GetRunningPermissionForALN(1)) goto DontMove;//Put Aligner
                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALN1);
                                    aligner = ListALN[0];
                                    pos = RobotPos.AlignerA;
                                    ePos = enumPosition.AlignerA;
                                }
                                else if (bNeedOCR2 && !ListOCR[1].Disable && !ListALN[0].Disable && ListALN[0].Wafer == null && robotManual.RobotHardwareAllow(enumPosition.AlignerA))
                                {
                                    if (!robotManual.GetRunningPermissionForALN(1)) goto DontMove;//Put Aligner
                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALN1);
                                    aligner = ListALN[0];
                                    pos = RobotPos.AlignerA;
                                    ePos = enumPosition.AlignerA;
                                }
                                else if (bNeedOCR1 && !ListOCR[2].Disable && !ListALN[1].Disable && ListALN[1].Wafer == null && robotManual.RobotHardwareAllow(enumPosition.AlignerB))
                                {
                                    if (!robotManual.GetRunningPermissionForALN(2)) goto DontMove;//Put Aligner
                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALN2);
                                    aligner = ListALN[1];
                                    pos = RobotPos.AlignerB;
                                }
                                else if (bNeedOCR2 && !ListOCR[3].Disable && !ListALN[1].Disable && ListALN[1].Wafer == null && robotManual.RobotHardwareAllow(enumPosition.AlignerB))
                                {
                                    if (!robotManual.GetRunningPermissionForALN(2)) goto DontMove;//Put Aligner
                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALN2);
                                    aligner = ListALN[1];
                                    pos = RobotPos.AlignerB;
                                }
                                else if (!ListALN[0].Disable && ListALN[0].Wafer == null && robotManual.RobotHardwareAllow(enumPosition.AlignerA))
                                {
                                    if (!robotManual.GetRunningPermissionForALN(1)) goto DontMove;//Put Aligner
                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALN1);
                                    aligner = ListALN[0];
                                    pos = RobotPos.AlignerA;
                                    ePos = enumPosition.AlignerA;
                                }
                                else if (!ListALN[1].Disable && ListALN[1].Wafer == null && robotManual.RobotHardwareAllow(enumPosition.AlignerB))
                                {
                                    if (!robotManual.GetRunningPermissionForALN(2)) goto DontMove;//Put Aligner
                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALN2);
                                    aligner = ListALN[1];
                                    pos = RobotPos.AlignerB;
                                }
                                else
                                {
                                    goto DontMove;
                                }


                                //put wafer to aligner
                                WriteLog(string.Format("[TRB{0}]:Put wafer to stage[{1}] slot[{2}] Arm[{3}].", robotManual.BodyNo, pos, 1, arm));
                                // Wafer transfer 
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData.FoupID, waferData.CJID, waferData.PJID,
                                    waferData.RecipeID,
                                    waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                    string.Format("The robot_{0} put wafer to {1} slot{2} use {3}", robotManual.BodyNo, pos, 1, arm));

                                aligner.HOME();//偷跑

                                //move standby pos
                                robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, true, ePos, arm, nStgeIndx, 1);
                                robotManual.SetCurrePos = pos;

                                if (SpinWait.SpinUntil(() => aligner.IsReadyToLoad(), 10000) == false)
                                {
                                    aligner.TriggerSException(enumAlignerError.ModeTimeout);
                                }

                                //move robot 
                                robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, ePos, nStgeIndx, 1, waferData);

                                //Queue to aligner
                                aligner.AssignQueue(waferData);
                                //沒有要run貨就釋放run貨權
                                robotManual.ReleaseRunningPermissionForALN(aligner.BodyNo);

                                #endregion
                            }
                            //  FRAME需要讀Barcode
                            else if (waferData.AlgnComplete == false && waferData.WaferSize == enumWaferSize.Frame && bAllowALN)
                            {
                                #region Put Turntable   
                                //================================================== aligner放片
                                I_Aligner aligner;
                                RobotPos pos;
                                enumPosition ePos = enumPosition.UnKnow;
                                if (!ListALN[0].Disable && ListALN[0].Wafer == null && robotManual.RobotHardwareAllow(enumPosition.AlignerA) && GParam.theInst.GetAlignerMode(0) == enumAlignerType.TurnTable)
                                {
                                    if (!robotManual.GetRunningPermissionForALN(1)) goto DontMove;//Put Aligner

                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALN1);
                                    aligner = ListALN[0];
                                    pos = RobotPos.AlignerA;
                                    ePos = enumPosition.AlignerA;
                                }
                                else if (!ListALN[1].Disable && ListALN[1].Wafer == null && robotManual.RobotHardwareAllow(enumPosition.AlignerB) && GParam.theInst.GetAlignerMode(1) == enumAlignerType.TurnTable)
                                {
                                    if (!robotManual.GetRunningPermissionForALN(2)) goto DontMove;//Put Aligner

                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.ALN2);
                                    aligner = ListALN[1];
                                    pos = RobotPos.AlignerB;
                                }
                                else
                                {
                                    goto DontMove;
                                }

                                //put wafer to aligner
                                WriteLog(string.Format("[TRB{0}]:Put wafer to stage[{1}] slot[{2}] Arm[{3}].", robotManual.BodyNo, pos, 1, arm));
                                // Wafer transfer 
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData.FoupID, waferData.CJID, waferData.PJID,
                                    waferData.RecipeID,
                                    waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                    string.Format("The robot_{0} put wafer to {1} slot{2} use {3}", robotManual.BodyNo, pos, 1, arm));

                                //move standby pos
                                robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, true, ePos, arm, nStgeIndx, 1);
                                robotManual.SetCurrePos = pos;
                                //move robot 
                                robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, ePos, nStgeIndx, 1, waferData);
                                //Queue to aligner
                                aligner.AssignQueue(waferData);
                                //沒有要run貨就釋放run貨權
                                robotManual.ReleaseRunningPermissionForALN(aligner.BodyNo);

                                #endregion
                            }
                            //  FRAME需要讀Barcode，機構允許執行讀碼
                            else if (waferData.AlgnComplete == false && waferData.WaferSize == enumWaferSize.Frame && robotManual.RobotHardwareAllowBarcode())
                            {
                                #region To BarCode



                                nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.BarCode);

                                //Extend wafer to barcode
                                WriteLog(string.Format("[TRB{0}]:Extend wafer to stage[{1}] slot[{2}] Arm[{3}].", robotManual.BodyNo, waferData.Position, waferData.Slot, arm));
                                // Wafer transfer 
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData.FoupID, waferData.CJID, waferData.PJID,
                                    waferData.RecipeID,
                                    waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                    string.Format("The robot_{0} extend wafer to {1} slot{2} use {3}", robotManual.BodyNo, waferData.Position, waferData.Slot, arm));

                                //move standby pos //此case不需要extend 讀barcode動作，安全起見先註解             
                                //robotManual.MoveToStandbyByInterLockW(robotManual.GetAckTimeout, true, arm, nStgeIndx, 1);
                                //robotManual.SetCurrePos = RobotPos.BarCodeReader;
                                //move robot 
                                //robotManual.ResetInPos();
                                //robotManual.ExtdW(robotManual.GetAckTimeout, 1, arm, nStgeIndx, 1); 
                                //robotManual.WaitInPos(robotManual.GetMotionTimeout);

                                #region read barcode
                                string BarcodeID = "";      //實體讀取
                                if (GParam.theInst.IsSimulate)
                                {
                                    BarcodeID = "ABCDEFG-" + waferData.Slot;
                                }
                                else
                                {
                                    BarcodeID = robotManual.Barcode.Read().Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");
                                }
                                //  以前有功能要比較 Wafer ID，先移除一律顯示成功
                                waferData.WaferIDComparison = enumWaferIDComparison.IDAgree;
                                waferData.WaferID_F = BarcodeID;
                                waferData.AlgnComplete = true;
                                WriteLog(string.Format("[TRB{0}]:BarCodeReader ID [{1}]", robotManual.BodyNo, BarcodeID));
                                #endregion

                                robotManual.ResetInPos();
                                robotManual.HomeW(robotManual.GetAckTimeout, 1, arm, nStgeIndx, 1);
                                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                                goto DontMove;
                                #endregion
                            }
                            else if (waferData.AlgnComplete == true && EQEnableFlags[0] ==true && waferData.GetUsingEQ != enumPosition.UnKnow && waferData.EqmComplete == false && waferData.WaferIDComparison == enumWaferIDComparison.IDAgree)
                            {
                                SSEquipment equipment = ListEQM[waferData.GetUsingEQ - enumPosition.EQM1];
                                #region Put Equipment

                                if (equipment.Simulate)
                                {
                                    switch (equipment._BodyNo)
                                    {
                                        case 1:
                                            ListAdam[0].setInputValue(0, true);
                                            break;
                                        case 2:
                                            ListAdam[0].setInputValue(2, true);
                                            break;
                                        case 3:
                                            ListAdam[1].setInputValue(0, true);
                                            break;
                                        case 4:
                                            ListAdam[1].setInputValue(2, true);
                                            break;
                                    }                                         
                                }

                                if (equipment.Wafer != null) { goto DontMove; }//有片子不能傳
                                if (equipment.IsWaferExist == true) { goto DontMove; }//有片子不能傳
                                if (equipment.IsReadyLoad == false && GParam.theInst.EqmSimulate(equipment._BodyNo - 1) != true) { goto DontMove; }//
                                // if (equipment.IsReady == false) { goto DontMove; }//位置不對不能傳 HSC bypass
                                arm = waferData.Position == SWafer.enumPosition.UpperArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm;

                                nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, SWafer.enumPosition.EQM1 + equipment._BodyNo - 1);
                                if (robotManual.GetCurrePos != (RobotPos.Equipment1 + equipment._BodyNo - 1))
                                {
                                    //move standby pos                                     

                                    robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, true, enumPosition.EQM1 + equipment._BodyNo - 1, arm, nStgeIndx, 1);
                                    robotManual.SetCurrePos = (RobotPos.Equipment1 + equipment._BodyNo - 1);
                                    WriteLog(string.Format("[TRB{0}]:Wait AccessAllowed wafer[{1}]. stage = [{2}], arm = [{3}].",
                                        robotManual.BodyNo, waferData.Slot, SWafer.enumPosition.EQM1 + equipment._BodyNo - 1, arm));
                                }

                                if (robotManual.UnldEQ_BeforeOK != null && robotManual.UnldEQ_BeforeOK() == false)//手臂伸入前需要確認
                                {
                                    goto DontMove;//失敗不能做
                                }

                                // Wafer transfer 
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now, waferData.FoupID, waferData.CJID, waferData.PJID, waferData.RecipeID, waferData.Slot,
                                       waferData.WaferID_F, waferData.WaferID_B, string.Format("The robot put wafer to equipment use {0}.", arm));

                                //move robot 
                                robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, enumPosition.EQM1 + equipment._BodyNo - 1, nStgeIndx, 1, waferData);

                                if (equipment.Simulate)
                                {
                                    switch (equipment._BodyNo)
                                    {
                                        case 1:
                                            ListAdam[0].setInputValue(0, false);
                                            break;
                                        case 2:
                                            ListAdam[0].setInputValue(2, false);
                                            break;
                                        case 3:
                                            ListAdam[1].setInputValue(0, false);
                                            break;
                                        case 4:
                                            ListAdam[1].setInputValue(2, false);
                                            break;
                                    }
                                }

                                //過帳
                                equipment.Wafer = waferData;

                                if (arm == enumRobotArms.UpperArm) robotManual.UpperArmWafer = null;
                                else if (arm == enumRobotArms.LowerArm) robotManual.LowerArmWafer = null;

                                /*if (robotManual.UnldEQ_AfterOK != null && robotManual.UnldEQ_AfterOK() == false)//  委派 手臂伸入前通知EQ
                                {
                                    WriteLog(string.Format("[TRB{0}]:robotManual.UnldEQ_After fail!!!", robotManual.BodyNo));
                                    throw new SException((int)(enumEQError.ReadFileDeleteException), "Can not delete read file");
                                }*/

                                WriteLog(string.Format("[TRB{0}]:Put wafer[{1}]. stage = [{2}], arm = [{3}].", robotManual.BodyNo, waferData.Slot, SWafer.enumPosition.EQM1 + equipment._BodyNo - 1, arm));
                                #endregion
                            }
                            //  需要Align但機構不允許/送去目標不允許，送去Buffer
                            else if ((waferData.AlgnComplete == false || robotManual.RobotHardwareAllow(waferData.ToLoadport) == false))
                            {
                                #region Put Buffer

                                I_Buffer buffer = null;
                                RobotPos pos = RobotPos.BufferA;
                                int nEmptySlot = -1;

                                #region 還有一隻手是空的，Alinger有Wafer，希望去取那一片wafer等等再執行雙放
                                if (robotManual.UpperArmWafer == null || robotManual.LowerArmWafer == null)
                                {
                                    if (ListALN[0].Disable == false && robotManual.RobotHardwareAllow(enumPosition.AlignerA) && ListALN[0].Wafer != null && ListALN[0].Wafer.AlgnComplete)
                                    {
                                        WriteLog(string.Format("[TRB{0}]:Prioritize AlignerA.", robotManual.BodyNo));
                                        goto DontMove;
                                    }
                                    if (ListALN[1].Disable == false && robotManual.RobotHardwareAllow(enumPosition.AlignerB) && ListALN[1].Wafer != null && ListALN[1].Wafer.AlgnComplete)
                                    {
                                        WriteLog(string.Format("[TRB{0}]:Prioritize AlignerB.", robotManual.BodyNo));
                                        goto DontMove;
                                    }
                                }
                                #endregion

                                #region 手臂支援雙取放 & FingerType相同 & 雙手有片 =>判斷是否可以雙放     
                                if (robotManual.UseArmSameMovement && robotManual.UpperArmFunc == robotManual.LowerArmFunc
                                    && (robotManual.UpperArmWafer != null && robotManual.LowerArmWafer != null)
                                    && (robotManual.UpperArmWafer.AlgnComplete == robotManual.LowerArmWafer.AlgnComplete)
                                    && (waferData_Prepare != null))
                                {
                                    //要把另一隻手上的Wafer取出來
                                    bool bCanDoubleArm = false;
                                    while (robotManual.queCommand.Count > 0)
                                    {
                                        if (waferData_Prepare == robotManual.UpperArmWafer)
                                        {
                                            if (waferData_Prepare.WaferIDComparison == enumWaferIDComparison.IDAgree)//20230401
                                                //要放下手臂此時上手也能放
                                                bCanDoubleArm = true;

                                            break;
                                        }
                                        if (waferData_Prepare == robotManual.LowerArmWafer)
                                        {
                                            if (waferData_Prepare.WaferIDComparison == enumWaferIDComparison.IDAgree)//20230401
                                                //要放上手臂此時下手也能放
                                                bCanDoubleArm = true;

                                            break;
                                        }
                                        else
                                        {
                                            robotManual.queCommand.TryDequeue(out waferData_Prepare);
                                            robotManual.quePreCommand.Enqueue(waferData_Prepare);
                                            robotManual.queCommand.TryPeek(out waferData_Prepare);
                                        }
                                    }

                                    if (bCanDoubleArm)
                                    {
                                        //看BUFFER的位置OK?
                                        foreach (I_Buffer item in ListBUF)
                                        {
                                            if (item.Disable) continue;
                                            if (item.HardwareSlot < 2) continue;//slot 只有1
                                            if (item.GetWaferCount() != 0) continue;
                                            if (item.IsSlotDisable(0) == false && item.IsSlotDisable(1) == false)
                                            {
                                                if (item.IsWaferDetectOn(0) == false && item.IsWaferDetectOn(1) == false
                                                    && item.GetWafer(0) == null && item.GetWafer(1) == null)//slot1 slot2都是空的
                                                {
                                                    if (robotManual.GetRunningPermissionForBUF(item.BodyNo) == false) continue;
                                                    //符合雙放
                                                    buffer = item;
                                                    nEmptySlot = 2;//雙放用上手去看放哪個slot
                                                    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.BUF1 + item.BodyNo - 1);
                                                    pos = RobotPos.BufferA + item.BodyNo - 1;
                                                    arm = enumRobotArms.BothArms;//改成雙手

                                                    WriteLog(string.Format("[TRB{0}]:Transfer Wafer put buffer use both arm slot[{1}/{2}]", robotManual.BodyNo, 1, 2));
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                                switch (arm)
                                {
                                    case enumRobotArms.UpperArm:
                                    case enumRobotArms.LowerArm:
                                        #region 找放Buffer哪裡可以放，之後考慮雙取雙放可能要改變邏輯，改成依照1234放
                                        if (!ListBUF[0].Disable && ListBUF[0].GetWaferCount() == 0 && robotManual.GetRunningPermissionForBUF(1))
                                        {
                                            buffer = ListBUF[0];
                                            nEmptySlot = buffer.GetEmptySlot();
                                            nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.BUF1);
                                            pos = RobotPos.BufferA;
                                        }
                                        else if (!ListBUF[0].Disable && ListBUF[0].GetEmptySlot() > 0 && robotManual.GetRunningPermissionForBUF(1))
                                        {
                                            buffer = ListBUF[0];
                                            nEmptySlot = buffer.GetEmptySlot();
                                            nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.BUF1);
                                            pos = RobotPos.BufferA;
                                        }
                                        else if (!ListBUF[1].Disable && ListBUF[1].GetWaferCount() == 0 && robotManual.GetRunningPermissionForBUF(2))
                                        {
                                            buffer = ListBUF[1];
                                            nEmptySlot = buffer.GetEmptySlot();
                                            nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.BUF2);
                                            pos = RobotPos.BufferB;
                                        }
                                        else if (!ListBUF[1].Disable && ListBUF[1].GetEmptySlot() > 0 && robotManual.GetRunningPermissionForBUF(2))
                                        {
                                            buffer = ListBUF[1];
                                            nEmptySlot = buffer.GetEmptySlot();
                                            nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, enumRbtAddress.BUF2);
                                            pos = RobotPos.BufferB;
                                        }
                                        else { goto DontMove; }
                                        #endregion
                                        break;
                                    case enumRobotArms.BothArms:
                                        //有判斷到可以雙放了
                                        break;
                                }
                                if (nEmptySlot <= 0) goto DontMove;


                                //put wafer to buffer                        
                                WriteLog(string.Format("[TRB{0}]:Put wafer to stage[{1}] slot[{2}] Arm[{3}].", robotManual.BodyNo, waferData.Position, nEmptySlot, arm));
                                // Wafer transfer 
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData.FoupID, waferData.CJID, waferData.PJID,
                                    waferData.RecipeID,
                                    waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                    string.Format("The robot_{0} put wafer to {1} slot{2} use {3}", robotManual.BodyNo, waferData.Position, nEmptySlot, arm));
                                if (arm == enumRobotArms.BothArms)
                                {
                                    m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                        waferData_Prepare.FoupID, waferData_Prepare.CJID, waferData_Prepare.PJID,
                                        waferData_Prepare.RecipeID,
                                        waferData_Prepare.Slot, waferData_Prepare.WaferID_F, waferData_Prepare.WaferID_B,
                                        string.Format("The robot_{0} put wafer to {1} slot{2} use {3}", robotManual.BodyNo, waferData_Prepare.Position, waferData_Prepare.Slot, arm));
                                }



                                //move standby pos
                                robotManual.MoveToStandbyByInterLockW(robotManual.GetAckTimeout, true, arm, nStgeIndx, nEmptySlot);
                                robotManual.SetCurrePos = pos;


                                //move robot 
                                robotManual.PutWaferByInterLockW(robotManual.GetAckTimeout, arm, nStgeIndx, nEmptySlot, waferData);

                                //Queue to buffer                          
                                buffer.AssignQueue(waferData);
                                if (arm == enumRobotArms.BothArms)
                                {
                                    buffer.AssignQueue(waferData_Prepare);
                                    robotManual.queCommand.TryDequeue(out waferData_Prepare);//移除Robot排程
                                }
                                //沒有要run貨就釋放run貨權                  
                                robotManual.ReleaseRunningPermissionForBUF(buffer.BodyNo);


                                if (buffer.AroundTrigger())
                                    robotManual.TriggerSException(enumRobotError.BufferTriggerAroundSensor);
                                #endregion
                            }
                            //
                            else if (waferData.WaferIDComparison != enumWaferIDComparison.UnKnow)
                            {
                                #region Put Loadport or Tower

                                int nToSlot1to25 = -1;
                                #region 考慮雙放                                
                                switch (waferData.ToLoadport)//改成指定目標
                                {
                                    case SWafer.enumFromLoader.LoadportA:
                                    case SWafer.enumFromLoader.LoadportB:
                                    case SWafer.enumFromLoader.LoadportC:
                                    case SWafer.enumFromLoader.LoadportD:
                                    case SWafer.enumFromLoader.LoadportE:
                                    case SWafer.enumFromLoader.LoadportF:
                                    case SWafer.enumFromLoader.LoadportG:
                                    case SWafer.enumFromLoader.LoadportH:
                                        if (robotManual.UseArmSameMovement && robotManual.UpperArmFunc == robotManual.LowerArmFunc
                                            && robotManual.UpperArmWafer != null && robotManual.LowerArmWafer != null
                                            && (robotManual.UpperArmWafer.ToSlot - robotManual.LowerArmWafer.ToSlot) == 1
                                            && robotManual.UpperArmWafer.AlgnComplete == robotManual.LowerArmWafer.AlgnComplete)
                                        {
                                            if (waferData_Prepare != null)
                                            {
                                                while (robotManual.queCommand.Count > 0)
                                                {
                                                    if (waferData.Position == enumPosition.LowerArm &&
                                                        waferData.ToLoadport == waferData_Prepare.ToLoadport
                                                        && (waferData_Prepare.ToSlot - waferData.ToSlot) == 1)
                                                    {
                                                        break;
                                                    }
                                                    if (waferData.Position == enumPosition.UpperArm &&
                                                       waferData.ToLoadport == waferData_Prepare.ToLoadport
                                                       && (waferData.ToSlot - waferData_Prepare.ToSlot) == 1)
                                                    {
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        robotManual.queCommand.TryDequeue(out waferData_Prepare);
                                                        robotManual.quePreCommand.Enqueue(waferData_Prepare);
                                                        robotManual.queCommand.TryPeek(out waferData_Prepare);
                                                    }
                                                }

                                                if (Math.Abs(waferData_Prepare.ToSlot - waferData.ToSlot) == 1
                                                  && waferData.ToLoadport == waferData_Prepare.ToLoadport)
                                                {
                                                    arm = enumRobotArms.BothArms;//改成雙手
                                                }
                                            }
                                        }
                                        nToSlot1to25 = waferData.ToSlot;
                                        if (arm == enumRobotArms.BothArms && waferData_Prepare.ToSlot > waferData.ToSlot)
                                        {
                                            nToSlot1to25 = waferData_Prepare.ToSlot;//雙放用上手去看放哪個slot
                                        }
                                        break;
                                }
                                #endregion

                                #region 判斷 loadport 對應到 robot address    
                                RobotPos pos;
                                enumPosition ePos = enumPosition.UnKnow;
                                switch (waferData.ToLoadport)//改成指定目標
                                {
                                    case SWafer.enumFromLoader.LoadportA:
                                    case SWafer.enumFromLoader.LoadportB:
                                    case SWafer.enumFromLoader.LoadportC:
                                    case SWafer.enumFromLoader.LoadportD:
                                    case SWafer.enumFromLoader.LoadportE:
                                    case SWafer.enumFromLoader.LoadportF:
                                    case SWafer.enumFromLoader.LoadportG:
                                    case SWafer.enumFromLoader.LoadportH:
                                        {
                                            #region 只要有一隻手空，Aligner有已經好的，這一次先不要放wafer，先去取片再一起放
                                            if (robotManual.UpperArmFunc == robotManual.LowerArmFunc)
                                            {
                                                if (robotManual.UpperArmWafer == null || robotManual.LowerArmWafer == null)
                                                    foreach (I_Aligner item in ListALN)
                                                        if (item != null && item.Disable == false && item.IsMoving == false && item.Wafer != null && item.Wafer.AlgnComplete)
                                                        {
                                                            WriteLog(string.Format("[TRB{0}]:put wafer pass({1}/{2}) Aligner{3} first", robotManual.BodyNo, waferData.Position, waferData.Slot, item.BodyNo));
                                                            goto DontMove;
                                                        }
                                            }
                                            #endregion

                                            I_Loadport loadport = ListSTG[(waferData.ToLoadport - SWafer.enumFromLoader.LoadportA)];
                                            pos = RobotPos.LoadPort1 + loadport.BodyNo - 1;
                                            nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, waferData.ToLoadport, loadport.UseAdapter) + (int)loadport.eFoupType;
                                            ePos = enumPosition.Loader1 + (int)(waferData.ToLoadport - enumFromLoader.LoadportA);
                                            //不是原去原回的話，檢查目標沒有帳空的
                                            if (waferData.ToLoadport != waferData.Owner && loadport.Waferlist[waferData.ToSlot - 1] != null)
                                                throw new SException(9999, string.Format("Target port {0} , Slot ={1} have wafer , Error !!", (int)waferData.ToLoadport, waferData.ToSlot));
                                        }
                                        break;
                                    default: nStgeIndx = -1; goto DontMove;
                                }
                                #endregion

                                //put wafer to loadport         
                                WriteLog(string.Format("[TRB{0}]:Put wafer[{1}]. stage = [{2}], arm = [{3}].", robotManual.BodyNo, waferData.ToSlot, waferData.ToLoadport, arm));
                                // Wafer transfer 
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData.FoupID, waferData.CJID, waferData.PJID,
                                    waferData.RecipeID,
                                    waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                    string.Format("The robot_{0} put wafer to {1} slot{2} use {3}", robotManual.BodyNo, waferData.ToLoadport, waferData.Slot, arm));

                                if (arm == enumRobotArms.BothArms)
                                {
                                    m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                        waferData_Prepare.FoupID, waferData_Prepare.CJID, waferData_Prepare.PJID,
                                        waferData_Prepare.RecipeID,
                                        waferData_Prepare.Slot, waferData_Prepare.WaferID_F, waferData_Prepare.WaferID_B,
                                        string.Format("The robot_{0} put wafer to {1} slot{2} use {3}", robotManual.BodyNo, waferData_Prepare.ToLoadport, waferData_Prepare.Slot, arm));
                                }

                                DateTime dtStart = DateTime.Now;
                                #region Robot unload wafer to target
                                int nClampCheckTime = GParam.theInst.GetRobot_UnldUseClmpCheckWaferTime(robotManual.BodyNo - 1);
                                if (nClampCheckTime > 0)
                                    robotManual.PutWaferByInterLockClampCheckW_ExtXaxis(robotManual.GetAckTimeout, arm, ePos, nStgeIndx, nToSlot1to25, nClampCheckTime, waferData);
                                else
                                {
                                    if( GParam.theInst.GetRobotAlignment_Enable()) // HSC ROBOT Alignment
                                    {
                                        // robotManual.PutWaferAlignmentByInterLockW(robotManual.GetAckTimeout, arm, nStgeIndx, nToSlot1to25, waferData);
                                        robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, ePos, nStgeIndx, nToSlot1to25, waferData); // HSC use noramal unload
                                    }
                                    else
                                    {
                                        robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, ePos, nStgeIndx, nToSlot1to25, waferData);
                                    }
                                }
                                #endregion
                                double dDuration = (DateTime.Now - dtStart).TotalMilliseconds;
                                if (robotManual.BodyNo == 2) WriteLog("[Demo]:robot2 unld tower time :" + dDuration);

                                robotManual.SetCurrePos = pos;

                                waferData.ProcessStatus = SWafer.enumProcessStatus.Processed;
                                waferData.Robotorder = false;
                                m_CycleCount++;
                                WriteLog("[Demo]:put wafer " + m_CycleCount);
                                if (arm == enumRobotArms.BothArms)
                                {
                                    waferData_Prepare.ProcessStatus = SWafer.enumProcessStatus.Processed;//取出來從waitprocess轉成Processing
                                    robotManual.queCommand.TryDequeue(out waferData_Prepare);
                                    m_CycleCount++;
                                    WriteLog("[Demo]:put wafer " + m_CycleCount);
                                }

                                //DB END
                                m_dbProcess.UpdateProcessWafer_EndTime(DateTime.Now, waferData.CJID, waferData.PJID, waferData.Slot);
                                if (arm == enumRobotArms.BothArms)
                                    m_dbProcess.UpdateProcessWafer_EndTime(DateTime.Now, waferData_Prepare.CJID, waferData_Prepare.PJID, waferData_Prepare.Slot);
                                CycleEndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                /*this.BeginInvoke(new Action(() =>
                                {
                                    m_autoProcess.CycleEndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                }));*/


                                #endregion
                            }
                            else
                            {
                                goto DontMove;
                            }
                        }
                        else if (waferData.ProcessStatus == SWafer.enumProcessStatus.Processed ||
                                 waferData.ProcessStatus == SWafer.enumProcessStatus.Error)//================================================== 出貨
                        {
                            //異常不改動，原去原回
                            #region Put Loadport                           

                            arm = waferData.Position == SWafer.enumPosition.UpperArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm;
                            enumPosition ePos = enumPosition.UnKnow;

                            #region 判斷 loadport 對應到 robot address       
                            switch (waferData.ToLoadport)//改成指定目標
                            {
                                case SWafer.enumFromLoader.LoadportA:
                                case SWafer.enumFromLoader.LoadportB:
                                case SWafer.enumFromLoader.LoadportC:
                                case SWafer.enumFromLoader.LoadportD:
                                case SWafer.enumFromLoader.LoadportE:
                                case SWafer.enumFromLoader.LoadportF:
                                case SWafer.enumFromLoader.LoadportG:
                                case SWafer.enumFromLoader.LoadportH:
                                    {
                                        I_Loadport loadport = ListSTG[(waferData.ToLoadport - SWafer.enumFromLoader.LoadportA)];
                                        nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, waferData.ToLoadport, loadport.UseAdapter) + (int)loadport.eFoupType;
                                        ePos = enumPosition.Loader1 + (int)(waferData.ToLoadport - enumFromLoader.LoadportA);
                                    }
                                    break;
                                //case SWafer.enumFromLoader.Tower01:
                                //case SWafer.enumFromLoader.Tower02:
                                //case SWafer.enumFromLoader.Tower03:
                                //case SWafer.enumFromLoader.Tower04:
                                //case SWafer.enumFromLoader.Tower05:
                                //case SWafer.enumFromLoader.Tower06:
                                //case SWafer.enumFromLoader.Tower07:
                                //case SWafer.enumFromLoader.Tower08:
                                //case SWafer.enumFromLoader.Tower09:
                                //case SWafer.enumFromLoader.Tower10:
                                //case SWafer.enumFromLoader.Tower11:
                                //case SWafer.enumFromLoader.Tower12:
                                //case SWafer.enumFromLoader.Tower13:
                                //case SWafer.enumFromLoader.Tower14:
                                //case SWafer.enumFromLoader.Tower15:
                                //case SWafer.enumFromLoader.Tower16:
                                //    nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, waferData.ToLoadport) + (waferData.ToSlot - 1) % 25;//10,11,12,13,14,15,16,17
                                //    nToSlot = (waferData.ToSlot - 1) % 25 + 1;//1~200 -> 1~25
                                //    break;
                                default: nStgeIndx = -1; goto DontMove;
                            }
                            #endregion

                            //put wafer to loadport    
                            WriteLog(string.Format("[TRB{0}]:Put wafer[{1}]. stage = [{2}], arm = [{3}].", robotManual.BodyNo, waferData.Slot, waferData.ToLoadport, arm));
                            // Wafer transfer 
                            m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                waferData.FoupID, waferData.CJID, waferData.PJID,
                                waferData.RecipeID,
                                waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                string.Format("The robot_{0} put wafer to {1} slot{2} use {3}", robotManual.BodyNo, waferData.ToLoadport, waferData.Slot, arm));

                            //unload
                            robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, ePos, nStgeIndx, waferData.Slot, waferData);

                            //DB END
                            m_dbProcess.UpdateProcessWafer_EndTime(DateTime.Now, waferData.CJID, waferData.PJID, waferData.Slot);
                            if (arm == enumRobotArms.BothArms)
                                m_dbProcess.UpdateProcessWafer_EndTime(DateTime.Now, waferData_Prepare.CJID, waferData_Prepare.PJID, waferData_Prepare.Slot);

                            #endregion
                        }
                        #endregion
                        break;
                    case SWafer.enumPosition.AlignerA:
                    case SWafer.enumPosition.AlignerB:
                        #region Aligner                                  
                        {
                            I_Aligner aligner = ListALN[waferData.Position - SWafer.enumPosition.AlignerA];
                            if (aligner.AlignmentStart == true || waferData.AlgnComplete == false || robotManual.RobotHardwareAllow(waferData.Position) == false) goto DontMove;

                            //  兩隻手都有空，去看看Loadport是不是還有片要去取，如果有就放棄這次取alinger，先去取loadport
                            if (robotManual.LowerArmFunc == robotManual.UpperArmFunc)//兩手臂相同TYPE
                            {
                                if (robotManual.RobotHardwareAllow(waferData.Owner))//出發與抵達是同一隻手臂
                                {
                                    if (robotManual.UpperArmWafer == null && robotManual.LowerArmWafer == null)
                                    {
                                        if (robotManual.quePreCommand.Count > 0 || robotManual.queCommand.Count > 0)
                                        {
                                            foreach (SWafer item in robotManual.queCommand.ToArray())
                                            {
                                                switch (item.Position)
                                                {
                                                    case enumPosition.Loader1:
                                                    case enumPosition.Loader2:
                                                    case enumPosition.Loader3:
                                                    case enumPosition.Loader4:
                                                    case enumPosition.Loader5:
                                                    case enumPosition.Loader6:
                                                    case enumPosition.Loader7:
                                                    case enumPosition.Loader8:
                                                        goto DontMove;//這次跳過，下一個que取出來就會去取loadport                                
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //兩隻手不一樣
                                /*if (waferData.EqProcessComplete == false && ListEQM.Any(eqm => eqm.Disable == false && eqm.Wafer != null))
                                {
                                    goto DontMove;//EQ有片了，如果取片手臂滿就塞車了
                                }*/
                            }
                            //  找用哪一隻手臂
                            switch (waferData.WaferSize)
                            {
                                case enumWaferSize.Inch12:
                                case enumWaferSize.Inch08:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去ALN取片
                                    if (arm == enumRobotArms.Empty)
                                        arm = robotManual.GetAvailableArm(enumArmFunction.I);//找去ALN取片
                                    break;
                                case enumWaferSize.Frame:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.FRAME);
                                    break;
                                case enumWaferSize.Panel:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);
                                    break;
                                default:
                                    goto DontMove;  //不可能發生                                 
                            }
                            if (arm == enumRobotArms.Empty) goto DontMove; //沒有可用的arm

                            //搶Align
                            if (!robotManual.GetRunningPermissionForALN(aligner.BodyNo)) goto DontMove;//TakeWaferByInterLockW

                            switch (arm)
                            {
                                case enumRobotArms.UpperArm: robotManual.PrepareUpperWafer = waferData; break;
                                case enumRobotArms.LowerArm: robotManual.PrepareLowerWafer = waferData; break;
                                default: goto DontMove; //沒有可用的arm
                            }

                            #region 考慮EXCH Exchange
                            bool bAlignerExchange = false;
                            if (waferData_Prepare != null && GParam.theInst.GetRobot_AlignerExchange(robotManual.BodyNo - 1))
                                switch (arm)
                                {
                                    case enumRobotArms.UpperArm:
                                        if (robotManual.LowerArmWafer != null && robotManual.LowerArmWafer.AlgnComplete == false)
                                        {
                                            //要把另一隻手上的Wafer取出來
                                            while (robotManual.queCommand.Count > 0)
                                            {
                                                if (waferData_Prepare == robotManual.LowerArmWafer)
                                                {
                                                    bAlignerExchange = true;
                                                    break;
                                                }
                                                else
                                                {
                                                    robotManual.queCommand.TryDequeue(out waferData_Prepare);
                                                    robotManual.quePreCommand.Enqueue(waferData_Prepare);
                                                    robotManual.queCommand.TryPeek(out waferData_Prepare);
                                                }
                                            }
                                        }
                                        break;
                                    case enumRobotArms.LowerArm:
                                        if (robotManual.UpperArmWafer != null && robotManual.UpperArmWafer.AlgnComplete == false)
                                        {
                                            //要把另一隻手上的Wafer取出來                                   
                                            while (robotManual.queCommand.Count > 0)
                                            {
                                                if (waferData_Prepare == robotManual.UpperArmWafer)
                                                {
                                                    bAlignerExchange = true;
                                                    break;
                                                }
                                                else
                                                {
                                                    robotManual.queCommand.TryDequeue(out waferData_Prepare);
                                                    robotManual.quePreCommand.Enqueue(waferData_Prepare);
                                                    robotManual.queCommand.TryPeek(out waferData_Prepare);
                                                }
                                            }
                                        }
                                        break;
                                }
                            #endregion

                            //================================================== 取片
                            nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, waferData.Position);

                            if (bAlignerExchange)
                            {
                                #region LOG
                                //take wafer from aligner
                                WriteLog(string.Format("[TRB{0}]:Take wafer from stage[{1}] slot[{2}] Arm[{3}] exchange.", robotManual.BodyNo, waferData.Position, 1, arm));
                                // Load Wafer transfer
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData.FoupID, waferData.CJID, waferData.PJID,
                                    waferData.RecipeID,
                                    waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                    string.Format("The robot_{0} take wafer from {1} slot{2} use {3} exchange", robotManual.BodyNo, waferData.Position, 1, arm));
                                //Take wafer to aligner
                                WriteLog(string.Format("[TRB{0}]:Take wafer to stage[{1}] slot[{2}] Arm[{3}] exchange.", robotManual.BodyNo, waferData.Position, 1, arm));
                                // Load Wafer transfer 
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData_Prepare.FoupID, waferData_Prepare.CJID, waferData_Prepare.PJID,
                                    waferData_Prepare.RecipeID,
                                    waferData_Prepare.Slot, waferData_Prepare.WaferID_F, waferData_Prepare.WaferID_B,
                                    string.Format("The robot_{0} gut wafer to {1} slot{2} use {3} exchange", robotManual.BodyNo, waferData_Prepare.Position, 1, arm));
                                #endregion

                                //move robot 
                                robotManual.TakeWaferExchangeByInterLockW(robotManual.GetAckTimeout, arm, nStgeIndx, 1);

                                //robot load完成資料需要塞回Queue
                                robotManual.queCommand.Enqueue(waferData);

                                //Queue to aligner
                                aligner.AssignQueue(waferData_Prepare);
                                robotManual.queCommand.TryDequeue(out waferData_Prepare);

                                //沒有要run貨就釋放run貨權
                                robotManual.ReleaseRunningPermissionForALN(aligner.BodyNo);
                            }
                            else
                            {
                                //take wafer from aligner
                                WriteLog(string.Format("[TRB{0}]:Take wafer from stage[{1}] slot[{2}] Arm[{3}].", robotManual.BodyNo, waferData.Position, 1, arm));
                                // Wafer transfer
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData.FoupID, waferData.CJID, waferData.PJID,
                                    waferData.RecipeID,
                                    waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                    string.Format("The robot_{0} take wafer from {1} slot{2} use {3}", robotManual.BodyNo, waferData.Position, 1, arm));

                                //move robot 
                                robotManual.TakeWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, waferData.Position, nStgeIndx, 1, waferData);
                                //robot load完成資料需要塞回Queue
                                robotManual.queCommand.Enqueue(waferData);
                                //沒有要run貨就釋放run貨權
                                robotManual.ReleaseRunningPermissionForALN(aligner.BodyNo);
                            }
                        }
                        #endregion
                        break;
                    case enumPosition.BufferA:
                    case enumPosition.BufferB:
                        #region 取Buffer
                        {
                            I_Buffer buffer = ListBUF[waferData.Position - SWafer.enumPosition.BufferA];


                            if (buffer.HardwareSlot == buffer.GetWaferCount())
                            {
                                //滿批
                            }
                            else
                            {
                                foreach (I_Buffer item in ListBUF)
                                {
                                    if (item.HardwareSlot == item.GetWaferCount())
                                    {
                                        //其他人滿批
                                        if (buffer != item)
                                            goto DontMove;//優先滿批的取

                                    }
                                }
                            }


                            #region Wafer Size 對應到手臂的形式
                            switch (waferData.WaferSize)
                            {
                                case enumWaferSize.Inch12:
                                case enumWaferSize.Inch08:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去LP取片
                                    if (arm == enumRobotArms.Empty)
                                        arm = robotManual.GetAvailableArm(enumArmFunction.I);//找去LP取片
                                    break;
                                case enumWaferSize.Frame:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.FRAME);//找去LP取片
                                    break;
                                case enumWaferSize.Panel:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);
                                    break;
                            }

                            switch (arm)
                            {
                                case enumRobotArms.UpperArm: robotManual.PrepareUpperWafer = waferData; break;
                                case enumRobotArms.LowerArm: robotManual.PrepareLowerWafer = waferData; break;
                                default: goto DontMove; //沒有可用的arm
                            }
                            #endregion

                            bool bAlignerHasEmpty = false;
                            if (robotManual.BodyNo == 1)
                            {
                                #region 要去LP且要經過aligner，
                                if (waferData.AlgnComplete)
                                {
                                    bAlignerHasEmpty = true;
                                }
                                #endregion
                            }
                            else if (robotManual.BodyNo == 2)
                            {
                                bAlignerHasEmpty = true;//第二面不需要判斷
                            }

                            if (robotManual.GetRunningPermissionForBUF(buffer.BodyNo) == false) goto DontMove;//搶使用權

                            //當下那片Wafer在Buffer的哪一層Slot
                            int nBufferSlot = buffer.GetWaferInSlot(waferData);
                            #region 考慮雙取    
                            if (robotManual.UseArmSameMovement && robotManual.UpperArmFunc == robotManual.LowerArmFunc
                                && robotManual.UpperArmWafer == null && robotManual.LowerArmWafer == null
                                && waferData_Prepare != null && waferData_Prepare.Position == waferData.Position && bAlignerHasEmpty)
                            {
                                int nWaferInBufSlot = buffer.GetWaferInSlot(waferData);
                                int nWaferPrepareInBufSlot = buffer.GetWaferInSlot(waferData_Prepare);
                                //假如取slot3 slot4 Load要下 slot4
                                if ((nWaferPrepareInBufSlot - nWaferInBufSlot) == 1)
                                {
                                    nBufferSlot = nWaferPrepareInBufSlot;
                                    arm = enumRobotArms.BothArms;//取片
                                    robotManual.PrepareUpperWafer = waferData_Prepare;
                                    robotManual.PrepareLowerWafer = waferData;
                                }
                                if ((nWaferInBufSlot - nWaferPrepareInBufSlot) == 1)
                                {
                                    nBufferSlot = nWaferInBufSlot;
                                    arm = enumRobotArms.BothArms;//取片
                                    robotManual.PrepareUpperWafer = waferData;
                                    robotManual.PrepareLowerWafer = waferData_Prepare;
                                }
                                WriteLog(string.Format("[TRB{0}]:Transfer Wafer use both arm buffer slot[{1}/{2}]", robotManual.BodyNo, nWaferInBufSlot, nWaferPrepareInBufSlot));
                            }
                            #endregion

                            switch (arm)
                            {
                                case enumRobotArms.UpperArm: robotManual.PrepareUpperWafer = waferData; break;
                                case enumRobotArms.LowerArm: robotManual.PrepareLowerWafer = waferData; break;
                                case enumRobotArms.BothArms: break;
                                default:
                                    //沒有要就釋放run貨權
                                    robotManual.ReleaseRunningPermissionForBUF(buffer.BodyNo);
                                    goto DontMove; //沒有可用的arm
                            }

                            //================================================== 取片               
                            nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, waferData.Position);
                            if (nBufferSlot <= 0)
                                goto DontMove;




                            //take wafer from aligner
                            WriteLog(string.Format("[TRB{0}]:Take wafer from stage[{1}] slot[{2}] Arm[{3}].", robotManual.BodyNo, waferData.Position, nBufferSlot, arm));
                            #region Wafer transfer to DB
                            m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                waferData.FoupID, waferData.CJID, waferData.PJID,
                                waferData.RecipeID,
                                waferData.Slot, waferData.WaferID_F, waferData.WaferID_B,
                                string.Format("The robot_{0} take wafer from {1} slot{2} use {3}", robotManual.BodyNo, waferData.Position, nBufferSlot, arm));
                            if (arm == enumRobotArms.BothArms)
                            {
                                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now,
                                    waferData_Prepare.FoupID, waferData_Prepare.CJID, waferData_Prepare.PJID,
                                    waferData_Prepare.RecipeID,
                                    waferData_Prepare.Slot, waferData_Prepare.WaferID_F, waferData_Prepare.WaferID_B,
                                    string.Format("The robot take wafer from {0} slot{1} use {2}", waferData_Prepare.Position, waferData_Prepare.Slot, arm));
                            }
                            #endregion
                            //move robot 
                            robotManual.TakeWaferByInterLockW(robotManual.GetAckTimeout, arm, nStgeIndx, nBufferSlot, waferData);
                            robotManual.SetCurrePos = buffer.BodyNo == 1 ? RobotPos.BufferA : RobotPos.BufferB;
                            //robot load完成資料需要塞回Queue
                            robotManual.queCommand.Enqueue(waferData);
                            //robot load雙取完成要把waferData_Prepare取出塞回排程
                            if (arm == enumRobotArms.BothArms)
                            {
                                robotManual.queCommand.TryDequeue(out waferData_Prepare);
                                robotManual.quePreCommand.Enqueue(waferData_Prepare);
                            }
                            //沒有要run貨就釋放run貨權
                            robotManual.ReleaseRunningPermissionForBUF(buffer.BodyNo);
                        }
                        #endregion
                        break;
                    case SWafer.enumPosition.EQM1:
                    case SWafer.enumPosition.EQM2:
                    case SWafer.enumPosition.EQM3:
                    case SWafer.enumPosition.EQM4:
                        #region Equipment
                        {
                            SSEquipment equipment = ListEQM[waferData.Position - SWafer.enumPosition.EQM1];

                            #region 取EQ前檢查兩隻手是否其中一隻有片，如果有必須是做完Aligner(在製程完成需過align回到loadport會卡住所以要判斷)
                            //  上手臂已經有片=>手上那片沒做過algn，如果此時align有片拿了這片就卡住了
                            if (robotManual.UpperArmWafer != null)
                            {
                                if (robotManual.UpperArmWafer.AlgnComplete == false)
                                    goto DontMove;
                            }
                            //  下手臂已經有片=>手上那片沒做過algn，如果此時align有片拿了這片就卡住了
                            else if (robotManual.LowerArmWafer != null)
                            {
                                if (robotManual.LowerArmWafer.AlgnComplete == false)
                                    goto DontMove;
                            }
                            #endregion

                            // if (equipment.IsWaferExist == false) { goto DontMove; }//沒片子不能取 HSC bypass
                            if (equipment.Simulate)
                            {
                                switch (equipment._BodyNo)
                                {
                                    case 1:
                                        ListAdam[0].setInputValue(1, true);
                                        break;
                                    case 2:
                                        ListAdam[0].setInputValue(3, true);
                                        break;
                                    case 3:
                                        ListAdam[1].setInputValue(1, true);
                                        break;
                                    case 4:
                                        ListAdam[1].setInputValue(3, true);
                                        break;
                                }
                            }
                            if (equipment.IsReadyUnload == false && GParam.theInst.EqmSimulate(equipment._BodyNo - 1) != true) { goto DontMove; }//位置不對不能傳  HSC bypass

                            nStgeIndx = GParam.theInst.GetDicPosRobot(robotManual.BodyNo, waferData.Position);

                            //搶EQ
                            if (!robotManual.GetRunningPermissionForEQ(equipment._BodyNo)) goto DontMove;

                            //找用哪一隻手臂
                            switch (waferData.WaferSize)
                            {
                                case enumWaferSize.Inch12:
                                case enumWaferSize.Inch08:
                                case enumWaferSize.Panel:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去EQ取片
                                    if (arm == enumRobotArms.Empty)
                                        arm = robotManual.GetAvailableArm(enumArmFunction.I);//找去EQ取片
                                    break;
                                case enumWaferSize.Frame:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.FRAME);//找去EQ取片      
                                    break;
                            }
                            switch (arm)
                            {
                                case enumRobotArms.UpperArm: robotManual.PrepareUpperWafer = waferData; break;//EQ取片前
                                case enumRobotArms.LowerArm: robotManual.PrepareLowerWafer = waferData; break;//EQ取片前
                                default: goto DontMove; //沒有可用的arm
                            }

                            //move standby pos
                            robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, false, waferData.Position, arm, nStgeIndx, 1);
                            robotManual.SetCurrePos = RobotPos.Equipment1 + equipment._BodyNo - 1;

                            if (robotManual.LoadEQ_BeforeOK != null && robotManual.LoadEQ_BeforeOK() == false)//  委派 手臂伸入前通知EQ
                            {
                                goto DontMove;//失敗不能做
                            }

                            //  take wafer
                            WriteLog(string.Format("[TRB{0}]:Take wafer. stage = [{1}], arm = [{2}].", robotManual.BodyNo, waferData.Position, arm));

                            // Wafer transfer
                            m_dbProcess.CreateProcessWaferbyStation(DateTime.Now, waferData.FoupID, waferData.CJID, waferData.PJID, waferData.RecipeID, waferData.Slot,
                                    waferData.WaferID_F, waferData.WaferID_B, string.Format("The robot take wafer from EQ use {0}.", arm));

                            //  move robot 
                            robotManual.TakeWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, waferData.Position, nStgeIndx, 1, waferData);

                            if (equipment.Simulate)
                            {
                                switch (equipment._BodyNo)
                                {
                                    case 1:
                                        ListAdam[0].setInputValue(1, false);
                                        break;
                                    case 2:
                                        ListAdam[0].setInputValue(3, false);
                                        break;
                                    case 3:
                                        ListAdam[1].setInputValue(1, false);
                                        break;
                                    case 4:
                                        ListAdam[1].setInputValue(3, false);
                                        break;
                                }
                            }

                            //  move data
                            WriteLog(string.Format("[TRB{0}]:Transfer data from [{1}] to [{2}].", robotManual.BodyNo, waferData.Position, arm));

                            //  過帳
                            if (arm == enumRobotArms.UpperArm)
                                robotManual.UpperArmWafer = waferData;
                            else
                                robotManual.LowerArmWafer = waferData;

                            equipment.Wafer = null;

                            if (robotManual.LoadEQ_AfterOK != null && robotManual.LoadEQ_AfterOK() == false)//  委派 手臂伸入前通知EQ
                            {
                                goto DontMove;//失敗不能做
                            }
                            robotManual.queCommand.Enqueue(waferData);

                        }
                        #endregion
                        break;
                    default:
                        break;
                }
                robotManual.ReleaseRunningPermissionForStgMap();
                return;

            DontMove:
                //條件不滿足, 重新排隊
                robotManual.queCommand.Enqueue(waferData);
                robotManual.ReleaseRunningPermissionForStgMap();
                return;
            }
            catch (SException ex)
            {
                AutoProcessAbort(this, new EventArgs());
                WriteLog("[SException] Robot DoAutoProcessing thread:" + ex);
            }
            catch (Exception ex)
            {
                AutoProcessAbort(this, new EventArgs());
                WriteLog("[Exception] Robot DoAutoProcessing thread:" + ex);
            }
        }
        //buffer自動流程
        void _buffer_DoAutoProcessing(object sender)

        {
            //取得load port
            I_Buffer bufferManual = sender as I_Buffer;
            try
            {
                //buffer區排入schedule 
                if (bufferManual.quePreCommand.Count > 0)
                {
                    while (bufferManual.quePreCommand.Count > 0)
                    {
                        SWafer theWafer;
                        if (bufferManual.quePreCommand.TryDequeue(out theWafer))
                            bufferManual.queCommand.Enqueue(theWafer);
                    }
                }

                //是否有待處理命令
                if (bufferManual.queCommand.Count <= 0) return;

                if (bufferManual.queCommand.Count > 2)
                {
                    WriteLog(string.Format("[BUF{0}]  The command over 2 in queue.", bufferManual.BodyNo));
                }

                //處理第一筆
                SWafer waferData;
                if (bufferManual.queCommand.TryDequeue(out waferData) == false)
                {
                    WriteLog(string.Format("[BUF{0}]  TryDequeue Failure.", bufferManual.BodyNo));
                    return;
                }

                //沒有帳
                if (waferData == null)
                {
                    WriteLog(string.Format("[BUF{0}]  The wafer data is null", bufferManual.BodyNo));
                    return;
                }
                // Wafer transfer
                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now, waferData.FoupID, waferData.CJID, waferData.PJID, waferData.RecipeID, waferData.Slot,
                                       waferData.WaferID_F, waferData.WaferID_B,
                                       "Wafer in buffer AssignToRobot");

                bufferManual.AssignToRobotQueue(waferData);//丟給robot作排程

                return;

            DontMove:
                //條件不滿足, 重新排隊
                bufferManual.queCommand.Enqueue(waferData);
                return;
            }
            catch (SException ex)
            {
                AutoProcessAbort(this, new EventArgs());

                WriteLog("[SException] Buffer DoAutoProcessing thread:" + ex);
            }
            catch (Exception ex)
            {
                AutoProcessAbort(this, new EventArgs());

                WriteLog("[Exception] Buffer DoAutoProcessing thread:" + ex);
            }
        }

        // job contorller 
        private void JobAutoProcess_DoPolling()
        {

            try
            {
                I_Loadport lp;

                List<int> UnitEndList = new List<int>();

                SControlJobObject ExecuteCJ = null;
                SProcessJobObject ExecutePJ = null;
                int ExecuteCJCount = 0;
                int ExecutePJCount = 0;
                SWafer wafer;
                bool FindPJEnd = true;
                bool UndoJobCreate = false;

                if (m_JobControl.CJlist.Count <= 0) return;


                if (m_JobControl.CJlist.Values.Where(x => x.Status == JobStatus.EXECUTING).Count() > 0) // Check End
                {
                    // Find ExecuteCJ
                    UnitEndList.Clear();
                    while ((m_JobControl.CJlist.Values.Where(a => a.Status == JobStatus.EXECUTING).Count() > ExecuteCJCount))
                    {

                        foreach (SControlJobObject CJ in m_JobControl.CJlist.Values.ToArray())
                        {
                            if (CJ.Status == JobStatus.EXECUTING)
                            {
                                ExecuteCJ = CJ;
                                ExecuteCJCount++;
                                break;
                            }
                        }

                        ExecutePJCount = 0;
                        #region Check Finish ...
                        if (m_bFinish)
                        {
                            while ((ExecuteCJ.PJList.Values.Where(a => a.Status == JobStatus.EXECUTING).Count() > ExecutePJCount))
                            {
                                foreach (int PJNo in ExecuteCJ.PJList.Keys.ToArray())
                                {
                                    if (ExecuteCJ.PJList[PJNo].Status == JobStatus.EXECUTING && ExecutePJCount != PJNo)
                                    {
                                        ExecutePJ = ExecuteCJ.PJList[PJNo];
                                        ExecutePJCount = PJNo;
                                        break;
                                    }
                                }

                                FindPJEnd = true;
                                lp = null;

                                foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in ExecutePJ.SourceTransInfoList)  // Check Source Info 
                                {
                                    foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)
                                    {

                                        // Find Source Wafer

                                        lp = ListSTG[sourceTransInfo.SourceSTG - 1];
                                        wafer = lp.Waferlist[transferInfo.SourceSlot - 1];

                                        if (wafer != null && wafer.Position == (enumPosition)sourceTransInfo.SourceSTG && wafer.Robotorder == false && wafer.ProcessStatus != enumProcessStatus.Cancel)
                                        {
                                            wafer.ProcessStatus = enumProcessStatus.Cancel;
                                            WriteLog(string.Format("[Finish] Wafer Source Port = {0}, Source slot = {1}, Status change Canecl ..", wafer.Owner.ToString(), wafer.Slot));
                                        }

                                    }
                                }
                            }

                        }
                        #endregion

                        ExecutePJ = null;
                        ExecutePJCount = 0;

                        #region Check PJ End 
                        // Find ExecutePJ
                        while ((ExecuteCJ.PJList.Values.Where(a => a.Status == JobStatus.EXECUTING).Count() > ExecutePJCount))
                        {
                            foreach (int PJNo in ExecuteCJ.PJList.Keys.ToArray())
                            {
                                if (ExecuteCJ.PJList[PJNo].Status == JobStatus.EXECUTING && ExecutePJCount != PJNo)
                                {
                                    ExecutePJ = ExecuteCJ.PJList[PJNo];
                                    ExecutePJCount = PJNo;
                                    break;
                                }
                            }

                            FindPJEnd = true;
                            lp = null;

                            // Check PJ end
                            foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in ExecutePJ.SourceTransInfoList)  // Check Source Info 
                            {
                                foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)
                                {
                                    // Find Target Wafer
                                    lp = ListSTG[sourceTransInfo.TargetSTG - 1];
                                    wafer = lp.Waferlist[transferInfo.TargetSlot - 1];

                                    if ((wafer == null) || (wafer.ProcessStatus != enumProcessStatus.Cancel && wafer != null && wafer.ProcessStatus != enumProcessStatus.Processed)) // procees not end 
                                    {
                                        #region Check Finish ...
                                        if (wafer == null) // Finish
                                        {

                                            // Find Source Wafer
                                            lp = ListSTG[sourceTransInfo.SourceSTG - 1];
                                            wafer = lp.Waferlist[transferInfo.SourceSlot - 1];

                                            if (wafer == null) // 再外面
                                                FindPJEnd = false;

                                            if (wafer != null && wafer.Robotorder == true)
                                                FindPJEnd = false;

                                            if (wafer != null && (wafer.ProcessStatus != enumProcessStatus.Cancel && wafer.ProcessStatus != enumProcessStatus.Processed))
                                            {
                                                FindPJEnd = false;
                                            }

                                        }
                                        #endregion
                                        else
                                            FindPJEnd = false;
                                        //break;
                                    }
                                    //    else
                                    //       FindPJEnd = false;

                                }

                            }

                            if (FindPJEnd) //PJ End
                            {
                                if (m_bFinish == true)
                                {

                                    ExecutePJ.Status = JobStatus.ABORT;
                                    WriteLog(string.Format("[ Check PJ end] PJID ={0} Status Change To Abort..", ExecutePJ.ID));
                                }
                                else
                                {

                                    if (m_Undo == true)
                                    {
                                        ExecutePJ.Status = JobStatus.STOPPING;
                                        WriteLog(string.Format("[ Check PJ end] PJID ={0} Status Change To STOPPING..", ExecutePJ.ID));
                                    }
                                    else
                                    {
                                        ExecutePJ.Status = JobStatus.COMPLETED;
                                        WriteLog(string.Format("[ Check PJ end] PJID ={0} Status Change To COMPLETED..", ExecutePJ.ID));
                                    }
                                }

                            }

                        }
                        #endregion

                        #region Check CJ End 

                        if (ExecuteCJ.PJList.Values.Where(b => b.Status == JobStatus.EXECUTING || b.Status == JobStatus.QUEUED || b.Status == JobStatus.ABORT).Count() == 0)
                        {
                            if (m_Undo == true)
                            {
                                ExecuteCJ.Status = JobStatus.STOPPING;
                                ExecuteCJ.Status = JobStatus.COMPLETED;

                                WriteLog(string.Format("[ Check CJ End]CJID ={0} Status Change To STOPPING..", ExecuteCJ.ID));
                            }
                            else
                            {
                                ExecuteCJ.Status = JobStatus.COMPLETED;
                                WriteLog(string.Format("[ Check CJ End]CJID ={0} Status Change To COMPLETED..", ExecuteCJ.ID));
                            }
                            m_MySQL.TruncateWaferTransfer();

                            foreach (SProcessJobObject PJ in ExecuteCJ.PJList.Values.ToArray())
                            {
                                PJ.Status = JobStatus.Destroy;
                                foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in PJ.SourceTransInfoList.ToArray())  // Check Source Info 
                                {
                                    foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList.ToArray())
                                    {
                                        UnitEndList.Add(sourceTransInfo.SourceSTG);

                                        if (UnitEndList.Contains(sourceTransInfo.TargetSTG) == false)
                                            UnitEndList.Add(sourceTransInfo.TargetSTG);
                                    }

                                }
                                WriteLog(PJ.ID + " is Remove");
                                m_JobControl.PJlist.Remove(PJ.ID);
                                m_dbProcess.UpdateProcessLotInfo(DateTime.Now, ExecuteCJ.ID, PJ.ID);
                            }

                            ExecuteCJ.Status = JobStatus.Destroy;
                            m_JobControl.CJlist.Remove(ExecuteCJ.ID);
                            m_Gem.CurrntGEMProcessStats = Rorze.Secs.GEMProcessStats.Finish;

                            lock (_objSTGJobLock)
                            {
                                foreach (int UnitNo in UnitEndList)
                                {

                                    if (m_Undo == false)
                                        ListSTG[UnitNo - 1].StatusMachine = enumStateMachine.PS_Complete;
                                    else
                                        ListSTG[UnitNo - 1].StatusMachine = enumStateMachine.PS_Abort;
                                    ListSTG[UnitNo - 1].Cleanjobschedule();
                                    WriteLog(string.Format("[ Check CJ End] LoadPort ={0} State Machine to Complete", ListSTG[UnitNo - 1].BodyNo));

                                }
                            }

                            if (m_Undo == true)
                                m_Undo = false;
                        }
                        #endregion

                        #region Check CJ Stop  
                        else if (ExecuteCJ.PJList.Values.Where(b => b.Status == JobStatus.ABORT).Count() == (ExecuteCJ.PJCount - ExecuteCJ.PJList.Values.Where(b => b.Status == JobStatus.COMPLETED).Count())) // Stop走這段
                        {
                            #region Check m_Undo

                            m_bFinish = false;

                            lock (_objSTGJobLock)
                            {
                                if (m_Undo) // 執行undo ,先判斷是否元去回?? , 重新塞Job
                                {

                                    WriteLog(string.Format("[Check CJ Stop] Trigger Undo check  .."));
                                    // 先清除Jobschedule
                                    #region 先清除Jobschedule

                                    foreach (SProcessJobObject PJ in ExecuteCJ.PJList.Values.ToArray())
                                    {

                                        foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in PJ.SourceTransInfoList.ToArray())  // Check Source Info 
                                        {
                                            foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList.ToArray())
                                            {
                                                UnitEndList.Add(sourceTransInfo.SourceSTG);
                                                if (UnitEndList.Contains(sourceTransInfo.TargetSTG) == false)
                                                    UnitEndList.Add(sourceTransInfo.TargetSTG);

                                            }
                                        }
                                    }

                                    foreach (int UnitNo in UnitEndList)
                                    {
                                        ListSTG[UnitNo - 1].Cleanjobschedule();
                                    }
                                    #endregion



                                    // 設定回去的資訊
                                    #region 設定回去的資訊
                                    foreach (SProcessJobObject PJ in ExecuteCJ.PJList.Values.ToArray())
                                    {

                                        if (PJ.Status != JobStatus.ABORT)
                                            continue;
                                        ExecutePJ = PJ;

                                        foreach (SSourceTransInfo sourceTransInfo in ExecutePJ.SourceTransInfoList.ToArray())
                                        {
                                            I_Loadport targetLoader = ListSTG[sourceTransInfo.TargetSTG - 1];

                                            foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)//TransferInfo
                                            {
                                                SWafer theWafer = targetLoader.Waferlist[transferInfo.TargetSlot - 1];

                                                if (theWafer == null)
                                                    continue;

                                                if (sourceTransInfo.SourceSTG > 0)  // 檢查Foup to Foup 
                                                {
                                                    if (transferInfo.SourceSlot == transferInfo.TargetSlot && sourceTransInfo.SourceSTG == sourceTransInfo.TargetSTG) // 原去原回, 不建立
                                                    {
                                                        continue;
                                                    }
                                                }

                                                if (theWafer != null) // 要回去
                                                {
                                                    theWafer.ProcessStatus = SWafer.enumProcessStatus.Sleep;
                                                    theWafer.Position = SWafer.enumPosition.Loader1 + sourceTransInfo.TargetSTG - 1;
                                                    theWafer.Slot = transferInfo.TargetSlot;
                                                    theWafer.AlgnComplete = (transferInfo.UseAligner == true) ? false : true;
                                                    theWafer.WaferIDComparison = theWafer.AlgnComplete ? SWafer.enumWaferIDComparison.IDAgree : SWafer.enumWaferIDComparison.UnKnow;
                                                    theWafer.ReadyToProcess = false;
                                                    theWafer.RecipeID = "";

                                                    theWafer.ToSlot = transferInfo.SourceSlot;
                                                    theWafer.ToLoadport = SWafer.enumFromLoader.LoadportA + sourceTransInfo.SourceSTG - 1;


                                                    ListSTG[sourceTransInfo.TargetSTG - 1].Addjobschedule(theWafer); // Set job schedule
                                                    WriteLog(string.Format("[Undo] Add job schedule , ToFoupID = {0} , ToSlot {1}", theWafer.ToFoupID, theWafer.ToSlot));
                                                    UndoJobCreate = true;
                                                }

                                            }




                                        }

                                        #region Undo job start
                                        if (UndoJobCreate)
                                        {

                                            WriteLog(string.Format("[Check CJ Stop] Undo job Create .. "));
                                            ExecutePJ.Status = SECSGEM.JobStatus.EXECUTING;
                                            foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in ExecutePJ.SourceTransInfoList)
                                            {
                                                foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)
                                                {

                                                    lp = ListSTG[sourceTransInfo.TargetSTG - 1];
                                                    wafer = lp.Waferlist[transferInfo.TargetSlot - 1];
                                                    if (wafer != null && wafer.ProcessStatus != SWafer.enumProcessStatus.WaitProcess)
                                                        wafer.ProcessStatus = SWafer.enumProcessStatus.WaitProcess;
                                                }


                                                lp = ListSTG[sourceTransInfo.TargetSTG - 1];
                                                if (lp.StatusMachine != enumStateMachine.PS_Process)
                                                    lp.StatusMachine = enumStateMachine.PS_Process;

                                                lp = ListSTG[sourceTransInfo.SourceSTG - 1];
                                                if (lp.StatusMachine != enumStateMachine.PS_Process)
                                                    lp.StatusMachine = enumStateMachine.PS_Process;
                                            }

                                            m_Gem.CurrntGEMProcessStats = Rorze.Secs.GEMProcessStats.Executing;


                                            m_MySQL.TruncateWaferTransfer();//傳送開始清空Undo紀錄

                                            this.AutoProcessStart(this, new EventArgs());//自動開始

                                            WriteLog(string.Format("[Check CJ Stop] Undo job Start Now .. "));
                                        }
                                        #endregion
                                    }
                                    #endregion


                                    // job Start 

                                    if (ExecuteCJ.PJList.Where(x => x.Value.Status == JobStatus.EXECUTING).Count() == 0) // Job End 
                                    {

                                        ExecuteCJ.Status = JobStatus.STOPPING;
                                        ExecuteCJ.Status = JobStatus.COMPLETED;


                                        foreach (SProcessJobObject PJ in ExecuteCJ.PJList.Values.ToArray())
                                        {
                                            PJ.Status = JobStatus.EXECUTING;
                                            PJ.Status = JobStatus.STOPPING;
                                            PJ.Status = JobStatus.Destroy;
                                            foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in PJ.SourceTransInfoList.ToArray())  // Check Source Info 
                                            {
                                                foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList.ToArray())
                                                {
                                                    UnitEndList.Add(sourceTransInfo.SourceSTG);

                                                    if (UnitEndList.Contains(sourceTransInfo.TargetSTG) == false)
                                                        UnitEndList.Add(sourceTransInfo.TargetSTG);
                                                }

                                            }
                                            m_JobControl.PJlist.Remove(PJ.ID);
                                        }

                                        ExecuteCJ.Status = JobStatus.Destroy;
                                        m_JobControl.CJlist.Remove(ExecuteCJ.ID);
                                        m_Gem.CurrntGEMProcessStats = Rorze.Secs.GEMProcessStats.Finish;

                                        foreach (int UnitNo in UnitEndList)
                                        {
                                            if (m_Undo == false)
                                                ListSTG[UnitNo - 1].StatusMachine = enumStateMachine.PS_Complete;
                                            else
                                                ListSTG[UnitNo - 1].StatusMachine = enumStateMachine.PS_Abort;
                                            ListSTG[UnitNo - 1].Cleanjobschedule();
                                        }

                                        if (m_Undo == true)
                                            m_Undo = false;

                                    }

                                    //m_Undo = false; // 解除Undo


                                }
                                #endregion

                                else // Stop job 
                                {

                                    WriteLog(string.Format("[Check CJ Stop] Undo job don't  need Create , So Stop job Now"));
                                    ExecuteCJ.Status = JobStatus.STOPPING;
                                    ExecuteCJ.Status = JobStatus.COMPLETED;
                                    m_MySQL.TruncateWaferTransfer();

                                    foreach (SProcessJobObject PJ in ExecuteCJ.PJList.Values.ToArray())
                                    {
                                        PJ.Status = JobStatus.EXECUTING;
                                        PJ.Status = JobStatus.STOPPING;
                                        PJ.Status = JobStatus.Destroy;
                                        foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in PJ.SourceTransInfoList.ToArray())  // Check Source Info 
                                        {
                                            foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList.ToArray())
                                            {
                                                UnitEndList.Add(sourceTransInfo.SourceSTG);
                                                if (UnitEndList.Contains(sourceTransInfo.TargetSTG) == false)
                                                    UnitEndList.Add(sourceTransInfo.TargetSTG);
                                            }

                                        }
                                        m_JobControl.PJlist.Remove(PJ.ID);
                                    }

                                    ExecuteCJ.Status = JobStatus.Destroy;
                                    m_JobControl.CJlist.Remove(ExecuteCJ.ID);
                                    m_Gem.CurrntGEMProcessStats = Rorze.Secs.GEMProcessStats.Finish;

                                    foreach (int UnitNo in UnitEndList)
                                    {

                                        ListSTG[UnitNo - 1].StatusMachine = enumStateMachine.PS_Complete;
                                        ListSTG[UnitNo - 1].Cleanjobschedule();

                                    }
                                }
                            }
                        }
                        #endregion

                        #region  Check Next PJ 
                        else //if (FindPJEnd) // Check Next PJ 
                        {
                            ExecutePJ = null;
                            foreach (SProcessJobObject PJ in ExecuteCJ.PJList.Values.ToArray())
                            {
                                if (PJ.Status == JobStatus.QUEUED && PJ.AutoStart == true)
                                {
                                    ExecutePJ = PJ;
                                }

                                if (ExecutePJ == null)
                                    continue;

                                //PJ Change Queue to FunctionSetup
                                ExecutePJ.Status = JobStatus.FunctionSetup;

                                WriteLog(string.Format("[Check Next PJ] Find Next PJ , FunctionSetup Now..."));

                                // find execute port / slot / 

                                foreach (SSourceTransInfo sourceTransInfo in ExecutePJ.SourceTransInfoList.ToArray())
                                {
                                    string strToFoupID;

                                    I_Loadport targetLoader = ListSTG[sourceTransInfo.TargetSTG - 1];
                                    targetLoader.CJID = ExecuteCJ.ID;
                                    strToFoupID = targetLoader.FoupID;

                                    I_Loadport sourceLoader = ListSTG[sourceTransInfo.SourceSTG - 1];
                                    foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)//TransferInfo
                                    {
                                        SWafer theWafer = sourceLoader.Waferlist[transferInfo.SourceSlot - 1];
                                        theWafer.CJID = ExecuteCJ.ID;
                                        theWafer.PJID = ExecutePJ.ID;
                                        theWafer.FoupID = sourceLoader.FoupID;
                                        //theWafer.RecipeID = pj.RecipeName;
                                        /*if (transferInfo.UseOCR)
                                            theWafer.RecipeID = transferInfo.OCRName;
                                        else
                                            theWafer.RecipeID = "";*/
                                        theWafer.RecipeID = transferInfo.OCRName;
                                        theWafer.Slot = transferInfo.SourceSlot;
                                        theWafer.Position = SWafer.enumPosition.Loader1 + sourceTransInfo.SourceSTG - 1;
                                        theWafer.ProcessStatus = SWafer.enumProcessStatus.Sleep;
                                        theWafer.AlgnComplete = (transferInfo.UseAligner == true) ? false : true;
                                        theWafer.WaferIDComparison = theWafer.AlgnComplete ? SWafer.enumWaferIDComparison.IDAgree : SWafer.enumWaferIDComparison.UnKnow;
                                        theWafer.NotchAngle = transferInfo.NotchAngle;
                                        theWafer.WaferInforID_B = transferInfo.WaferIDByHost; // T7 
                                        theWafer.WApplyEQ = transferInfo.ApplyEQ;
                                        
                                        SGroupRecipe rcpeContent = m_grouprecipe.GetRecipeGroupList[theWafer.RecipeID];
                                        bool[] EQEnableFlags = rcpeContent.GetEQ_ProcessEnable();
                                        // 依 ApplyEQ 輪流指定 EQM1~EQM4
                                        if ((!GParam.theInst.EqmDisable(0) || !GParam.theInst.EqmDisable(1) || !GParam.theInst.EqmDisable(2) || !GParam.theInst.EqmDisable(3)) && EQEnableFlags[0] == true)

                                        {
                                            theWafer.SetUsingEQ(GetNextAvailableEQ(transferInfo.ApplyEQ, ref eqRoundRobinIndex));
                                        }

                                        //theWafer.WaferID_B = transferInfo.WaferIDByHost;    //這不應該要把HOST寫入，要由OCR讀取決定                                          

                                        theWafer.LotID = transferInfo.LotIDByHost;
                                        theWafer.ReadyToProcess = false;
                                        //Taget                                          
                                        theWafer.ToLoadport = SWafer.enumFromLoader.LoadportA + sourceTransInfo.TargetSTG - 1;
                                        theWafer.ToFoupID = strToFoupID;
                                        theWafer.ToSlot = transferInfo.TargetSlot;

                                        ListSTG[sourceTransInfo.SourceSTG - 1].Addjobschedule(theWafer); // Set job schedule
                                        WriteLog(string.Format("[Check Next PJ] Add job schedule , ToFoupID = {0} , ToSlot {1}", theWafer.ToFoupID, theWafer.ToSlot));
                                    }

                                }
                                InitalStopFlag();
                                m_JobControl.CJlist[ExecuteCJ.ID].Status = SECSGEM.JobStatus.EXECUTING;
                                // foreach (SProcessJobObject pj in m_JobControl.CJlist[ExecuteCJ.ID].PJList.Values)
                                {

                                    ExecutePJ.Status = SECSGEM.JobStatus.EXECUTING;
                                    foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in ExecutePJ.SourceTransInfoList)
                                    {
                                        foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)
                                        {
                                            lp = ListSTG[sourceTransInfo.SourceSTG - 1];
                                            wafer = lp.Waferlist[transferInfo.SourceSlot - 1];
                                            if (wafer.ProcessStatus != SWafer.enumProcessStatus.WaitProcess)
                                                wafer.ProcessStatus = SWafer.enumProcessStatus.WaitProcess;
                                        }

                                        lp = ListSTG[sourceTransInfo.SourceSTG - 1];
                                        if (lp.StatusMachine != enumStateMachine.PS_Process)
                                            lp.StatusMachine = enumStateMachine.PS_Process;

                                        lp = ListSTG[sourceTransInfo.TargetSTG - 1];
                                        if (lp.StatusMachine != enumStateMachine.PS_Process)
                                            lp.StatusMachine = enumStateMachine.PS_Process;

                                    }
                                }

                                //===========Create Wafer End && Start process
                                m_Gem.CurrntGEMProcessStats = Rorze.Secs.GEMProcessStats.Executing;

                                m_MySQL.TruncateWaferTransfer();//傳送開始清空Undo紀錄

                                this.AutoProcessStart(this, new EventArgs());//自動開始 

                                //_logger.WriteLog(string.Format("[Check Next PJ] CJID ={0} PJID ={1},Process Start ... ", ExecuteCJ.ID, ExecutePJ.ID));

                            }


                        }
                        #endregion


                    }

                }

                if (m_JobControl.CJlist.Values.Where(x => x.Status == JobStatus.EXECUTING).Count() > 0) // 限制1個CJ , 可以多PJ一起Start 
                    return;

                #region 建立傳送帳料 New CJ Start
                if (m_JobControl.CJlist.Values.Where(x => x.Status == JobStatus.QUEUED).Count() > 0)
                {


                    foreach (SControlJobObject CJ in m_JobControl.CJlist.Values.ToArray()) // Select CJ
                    {
                        if (CJ.Status == JobStatus.QUEUED && CJ.AutoStart == true)
                        {
                            ExecuteCJ = CJ;
                            break;
                        }
                    }

                    if (ExecuteCJ == null) return;

                    // CJ Change Queue to Select
                    ExecuteCJ.Status = JobStatus.Select;
                    //  ExecuteCJ.Status = JobStatus.EXECUTING;
                    ExecuteCJ.Status = SECSGEM.JobStatus.EXECUTING;
                    InitalStopFlag();
                    //Select PJ
                    WriteLog(string.Format("[New CJ Start ] CJID ={0} Status to  EXECUTING", ExecuteCJ.ID));

                    foreach (SProcessJobObject PJ in ExecuteCJ.PJList.Values.ToArray())
                    {
                        ExecutePJ = null;
                        if (PJ.Status == JobStatus.QUEUED && PJ.AutoStart == true)
                        {
                            ExecutePJ = PJ;
                        }

                        if (ExecutePJ == null)
                        {
                            continue;
                        }

                        //PJ Change Queue to FunctionSetup
                        ExecutePJ.Status = JobStatus.FunctionSetup;

                        // find execute port / slot / 

                        lock (_objSTGJobLock)
                        {
                            foreach (SSourceTransInfo sourceTransInfo in ExecutePJ.SourceTransInfoList.ToArray())
                            {
                                string strToFoupID;

                                I_Loadport targetLoader = ListSTG[sourceTransInfo.TargetSTG - 1];
                                targetLoader.CJID = ExecuteCJ.ID;
                                strToFoupID = targetLoader.FoupID;

                                I_Loadport sourceLoader = ListSTG[sourceTransInfo.SourceSTG - 1];
                                foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)//TransferInfo
                                {
                                    SWafer theWafer = sourceLoader.Waferlist[transferInfo.SourceSlot - 1];
                                    theWafer.CJID = ExecuteCJ.ID;
                                    theWafer.PJID = ExecutePJ.ID;
                                    theWafer.FoupID = sourceLoader.FoupID;
                                    theWafer.RecipeID = transferInfo.OCRName;
                                    theWafer.Slot = transferInfo.SourceSlot;
                                    theWafer.Position = SWafer.enumPosition.Loader1 + sourceTransInfo.SourceSTG - 1;
                                    theWafer.ProcessStatus = SWafer.enumProcessStatus.Sleep;
                                    theWafer.AlgnComplete = (transferInfo.UseAligner == true) ? false : true;
                                    theWafer.WaferIDComparison = theWafer.AlgnComplete ? SWafer.enumWaferIDComparison.IDAgree : SWafer.enumWaferIDComparison.UnKnow;
                                    theWafer.NotchAngle = transferInfo.NotchAngle;
                                    theWafer.WaferInforID_B = transferInfo.WaferIDByHost;
                                    theWafer.WApplyEQ = transferInfo.ApplyEQ;

                                    SGroupRecipe rcpeContent = m_grouprecipe.GetRecipeGroupList[theWafer.RecipeID];
                                    bool[] EQEnableFlags = rcpeContent.GetEQ_ProcessEnable();
                                    // 依 ApplyEQ 輪流指定 EQM1~EQM4
                                    if ((!GParam.theInst.EqmDisable(0) || !GParam.theInst.EqmDisable(1) || !GParam.theInst.EqmDisable(2) || !GParam.theInst.EqmDisable(3)) && EQEnableFlags[0] == true)

                                    {
                                        theWafer.SetUsingEQ(GetNextAvailableEQ(transferInfo.ApplyEQ, ref eqRoundRobinIndex));
                                    }
                                    //if (theWafer.WaferID_B == "")//有經有ID，UNDO的流程
                                    //    theWafer.WaferID_B = transferInfo.WaferIDByHost;

                                    theWafer.LotID = transferInfo.LotIDByHost;
                                    theWafer.ReadyToProcess = false;
                                    theWafer.ToLoadport = SWafer.enumFromLoader.LoadportA + sourceTransInfo.TargetSTG - 1;
                                    theWafer.ToFoupID = strToFoupID;
                                    theWafer.ToSlot = transferInfo.TargetSlot;

                                    ListSTG[sourceTransInfo.SourceSTG - 1].Addjobschedule(theWafer); // Set job schedule
                                    WriteLog(string.Format("[New CJ Start] Add job schedule , ToFoupID = {0} , ToSlot {1}", theWafer.ToFoupID, theWafer.ToSlot));
                                }

                            }
                        }

                        //  foreach (SProcessJobObject pj in m_JobControl.CJlist[ExecuteCJ.ID].PJList.Values)
                        //{

                        ExecutePJ.Status = SECSGEM.JobStatus.EXECUTING;

                        WriteLog(string.Format("[New PJ Start ] PJID ={0} Status to  EXECUTING", ExecutePJ.ID));

                        foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in ExecutePJ.SourceTransInfoList)
                        {
                            foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)
                            {

                                lp = ListSTG[sourceTransInfo.SourceSTG - 1];
                                wafer = lp.Waferlist[transferInfo.SourceSlot - 1];
                                if (wafer.ProcessStatus != SWafer.enumProcessStatus.WaitProcess)
                                    wafer.ProcessStatus = SWafer.enumProcessStatus.WaitProcess;

                            }


                            lp = ListSTG[sourceTransInfo.SourceSTG - 1];
                            if (lp.StatusMachine != enumStateMachine.PS_Process)
                                lp.StatusMachine = enumStateMachine.PS_Process;


                            lp = ListSTG[sourceTransInfo.TargetSTG - 1];
                            if (lp.StatusMachine != enumStateMachine.PS_Process)
                                lp.StatusMachine = enumStateMachine.PS_Process;

                        }
                        //}
                    }

                    //===========Create Wafer End && Start process
                    m_Gem.CurrntGEMProcessStats = Rorze.Secs.GEMProcessStats.Executing;


                    m_MySQL.TruncateWaferTransfer();//傳送開始清空Undo紀錄

                    this.AutoProcessStart(this, new EventArgs());//自動開始 

                    WriteLog(string.Format("[New CJ Start ] CJID ={0} PJID ={1},Process Start ... ", ExecuteCJ.ID, ExecutePJ.ID));
                }
                #endregion

            }
            catch (Exception ex)
            {
                m_JobControl.CJlist.Clear();
                m_JobControl.PJlist.Clear();
                AutoProcessAbort(this, new EventArgs());
                WriteLog("[JobAutoProcess_DoPolling] Exception" + ex);
            }
        }


        //equipment自動流程
        void _equipment_DoAutoProcessing(object sender)
        {
            try
            {
                SSEquipment equipment = sender as SSEquipment;
                int nIndex = equipment._BodyNo - 1;

                //  檢查是否有晶片=>沒有wafer或已經完成製成
                if (equipment.Wafer == null || equipment.Wafer.EqmComplete) { goto DontMove; }
                //if (equipment.IsWaferExist == false) { goto DontMove; }//沒片不能做
                //if (equipment.IsReady == false) { goto DontMove; }

                //  ready flag off
                WriteLog("[Equipment]:start and Processing flag on");
                equipment.IsProcessing = true;

                //  wafer data
                SWafer waferData = equipment.Wafer;
                //  Wafer transfer
                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now, waferData.FoupID, waferData.CJID, waferData.PJID, waferData.RecipeID, waferData.Slot,
                    waferData.WaferID_F, waferData.WaferID_B,
                    "Equipment Process Start");

                //  wafer information to eq
                WriteLog("[Equipment]:send wafer information to eq");

                SGroupRecipe RecipeContent = m_grouprecipe.GetRecipeGroupList[waferData.RecipeID];
                string[] test = RecipeContent.GetEQ_Recipe();
                WriteLog("[Equipment]:wafer process complete flag on");
                waferData.EqmComplete = true;

                /*if (GParam.theInst.IsAfterProcessToAln || equipment.Wafer.WaferSize == enumWaferSize.Frame)//使用者想要回foup前再過一次aligner
                {
                    WriteLog("[Equipment]:processed AlgnComplete flag off");
                    waferData.AlgnComplete = false;
                }*/

                //  ready flag on
                WriteLog("[Equipment]:Processing flag off");
                equipment.IsProcessing = false;

                // Wafer transfer
                m_dbProcess.CreateProcessWaferbyStation(DateTime.Now, waferData.FoupID, waferData.CJID, waferData.PJID, waferData.RecipeID, waferData.Slot,
                        waferData.WaferID_F, waferData.WaferID_B,
                        "Equipment Process Complete");
                System.Threading.SpinWait.SpinUntil(() => false, 8000);


                //測試先讓回foup前再過一次aligner
                //equipment.Wafer.AlgnComplete = false;

                SpinWait.SpinUntil(() => false, 100);

                // Process finish
                WriteLog("[Equipment]:process finish and assignQueue to robot");
                equipment.AssignToRobotQueue(equipment.Wafer);//丟給robot作排程        


                return;
            DontMove:
                //條件不滿足, 重新排隊
                //equipment.queCommand.Enqueue(waferData);
                return;
            }
            catch (SException ex)
            {
                WriteLog("[Equipment]:<<SException>> " + ex);

                if (m_IsCycle == false)
                {
                    frmMessageBox frm;
                    frm = new frmMessageBox(string.Format("Equipment process complete timeout , click [yes] process end or [no] retry"), "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (frm.ShowDialog() == DialogResult.Yes)
                    {
                        AutoProcessAbort(this, new EventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                AutoProcessAbort(this, new EventArgs());
                WriteLog("[Equipment]:<<Exception>> " + ex);
            }
        }

        //==============================================================================

        public void SetSecsGem(SGEM300 gem) { m_Gem = gem; }

        //Process Start
        public void AutoProcessStart(object sender, EventArgs e)
        {
            foreach (I_Robot item in ListTRB)
            {
                if (item != null && item.Disable == false) item.AutoProcessStart();
            }

            foreach (I_Loadport item in ListSTG)
            {
                if (item != null && item.Disable == false) item.AutoProcessStart();
            }

            foreach (I_Aligner item in ListALN)
            {
                if (item != null && item.Disable == false) item.AutoProcessStart();
            }

            foreach (I_Buffer item in ListBUF)
            {
                if (item != null && item.Disable == false) item.AutoProcessStart();
            }

            foreach (SSEquipment item in ListEQM)
            {
                if (item != null && item.Disable == false) item.AutoProcessStart();
            }

            if (GMotion.theInst.eTransfeStatus == enumTransfeStatus.Idle)
            {

            }

            GMotion.theInst.eTransfeStatus = enumTransfeStatus.Transfe;

            jobAutoProcess.Set();
        }
        //Process Abort
        void AutoProcessEnd(object sender, EventArgs e)
        {
            foreach (I_Robot item in ListTRB)
            {
                if (item != null && item.Disable == false) item.AutoProcessEnd();
            }

            foreach (I_Aligner item in ListALN)
            {
                if (item != null && item.Disable == false) item.AutoProcessEnd();
            }

        }
        //Process Abort
        void AutoProcessAbort(object sender, EventArgs e)
        {
            m_IsCycle = false;

            foreach (I_Robot item in ListTRB)
            {
                if (item.Disable) continue;
                item.AutoProcessEnd();
                item.Cleanjobschedule();//20240704 清除JOB
            }

            foreach (I_Loadport item in ListSTG)
            {
                if (item.Disable) continue;

                item.AutoProcessEnd();
                if (item.StatusMachine == enumStateMachine.PS_Process)
                    item.StatusMachine = enumStateMachine.PS_Error;
                item.Cleanjobschedule();//20240704 清除JOB

            }

            foreach (I_Aligner item in ListALN)
            {
                if (item.Disable) continue;
                item.AutoProcessEnd();
                item.Cleanjobschedule();//20240704 清除JOB
            }

            foreach (I_Buffer item in ListBUF)
            {
                if (item.Disable) continue;
                item.Cleanjobschedule();//20240704 清除JOB
            }

            // JobController
            jobAutoProcess.Reset();
            m_JobControl.CJlist.Clear();
            m_JobControl.PJlist.Clear();


        }


        public bool IsPrepareForEnd()
        {
            return m_bFinish || m_Undo;
        }

        public void PrepareForEnd()
        {
            m_IsCycle = false;
            m_bFinish = true;
            m_Undo = true;
            GMotion.theInst.eTransfeStatus = enumTransfeStatus.Stop;
        }
        public void InitalStopFlag()
        {
            m_bFinish = false;
            m_bUndoForReadFail = false;
            m_Undo = false;
        }

        //============================================================================== CYCLE
        bool m_IsCycleDoAlign = false;
        string m_CycleRcpName = "";
        bool m_IsCycle = false;
        int m_CycleCount = 0;
        public bool IsCycle { get { return m_IsCycle; } }
        public bool IsCycleDoAlign { get { return m_IsCycleDoAlign; } }
        public string CycleRcpName { get { return m_CycleRcpName; } }
        public void SetCycleRunInfo(bool isAlign, string rcpName)
        {
            m_IsCycleDoAlign = isAlign;
            m_CycleRcpName = rcpName;
        }
        public void StartCycleRun()
        {
            m_CycleCount = 0;
            CycleStartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            CycleEndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            m_IsCycle = true;

        }
        public void StopCycleRun()
        {
            m_IsCycle = false;
        }

        public int CycleCount { get { return m_CycleCount; } }
        public string CycleStartTime { get; set; }
        public string CycleEndTime { get; set; }
        public bool CheckPJCJDone()
        {
            foreach (SControlJobObject cj in m_JobControl.CJlist.Values)
            {
                if (cj.Status == JobStatus.COMPLETED)
                    continue;
                foreach (SProcessJobObject pj in cj.PJList.Values)
                {
                    if (pj.Status == JobStatus.COMPLETED)
                        continue;
                }
            }
            int nCount = m_JobControl.CJlist.Count;
            return nCount == 0;
        }



        #region 建立傳送帳料
        private string m_strRecipeRecord;
        public bool CreateJob(ref ConcurrentQueue<clsSelectWaferInfo> selectWaferInfo, bool bNoAign, string strRecipe, bool[] applyEQ)
        {
            bool bSuccess = false;
            lock (this)
            {
                try
                {
                    clsSelectWaferInfo selectInfo = null;
                    if (false == selectWaferInfo.TryPeek(out selectInfo))
                    {
                        WriteLog(string.Format("selectWaferInfo TryPeek Error, Please check select wafer."));
                        return false;
                    }

                    ConcurrentQueue<clsSelectWaferInfo> SelectWaferInfoQueue = selectWaferInfo;

                    #region  ============ Safety ============    

                    if (m_alarm.CurrentAlarm != null && m_alarm.CurrentAlarm.Count > 0)
                    {
                        if (m_alarm.IsOnlyWarning() == false)
                        {
                            WriteLog(string.Format("There are uncleared abnormalities, please confirm the machine status first."));
                            return false;
                        }
                    }

                    foreach (I_Robot rb in ListTRB)
                    {
                        if (rb != null && rb.Disable == false && rb.IsOrgnComplete == false)
                        {
                            WriteLog(string.Format("Robot{0} is not ready.", rb.BodyNo));
                            return false;
                        }
                    }

                    foreach (clsSelectWaferInfo clsSelectWaferInfo in SelectWaferInfoQueue.ToArray())
                    {
                        if (clsSelectWaferInfo.SourceLpBodyNo > 0)
                        {
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].FoupExist == false)
                            {
                                WriteLog("Loadport has no foup.");
                                return false;
                            }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].StatusMachine == enumStateMachine.PS_Process)
                            {
                                WriteLog("Loadport StatusMachine is PS_Process.");
                                return false;
                            }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].StatusMachine != enumStateMachine.PS_Docked)
                            {
                                WriteLog("Loadport StatusMachine is not PS_Docked.");
                                return false;
                            }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].FoupID.Trim().Length <= 0)
                            {
                                WriteLog("Loadport foup id is empty.");
                                return false;
                            }
                            foreach (char item in ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].MappingData)
                            {
                                if (item != '0' && item != '1')
                                {
                                    WriteLog("Loadport mapping error.");
                                    return false;
                                }
                            }
                        }

                        if (clsSelectWaferInfo.TargetLpBodyNo > 0)
                        {
                            if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].FoupExist == false)
                            {
                                WriteLog("Loadport has no foup.");
                                return false;
                            }
                            else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].StatusMachine == enumStateMachine.PS_Process && m_bUndoForReadFail == false)//UNDO需要執行先PASS
                            {
                                WriteLog("Loadport StatusMachine is PS_Process.");
                                return false;
                            }
                            else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].StatusMachine != enumStateMachine.PS_Docked && m_bUndoForReadFail == false)//UNDO需要執行先PASS
                            {
                                WriteLog("Loadport StatusMachine is not PS_Docked.");
                                return false;
                            }
                            else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].FoupID.Trim().Length <= 0)
                            {
                                WriteLog("Loadport foup id is empty.");
                                return false;
                            }
                            foreach (char item in ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].MappingData)
                            {
                                if (item != '0' && item != '1')
                                {
                                    WriteLog("Loadport mapping error.");
                                    return false;
                                }
                            }
                        }
                    }

                    //只要EQ能選擇Recipe或是有OCR就要考慮Grouprecipe
                    if (GParam.theInst.IsAllOcrDisable() == false)
                    {
                        if (m_grouprecipe.GetRecipeGroupList.ContainsKey(strRecipe) == false)
                        {
                            WriteLog(string.Format("Recipe is empty or wrong."));
                            return false;
                        }
                    }
                    #endregion

                    bool bUndoForReadFail = m_bUndoForReadFail;
                    InitalStopFlag();

                    string strCJID = "CJID-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");//CJ:Foup_Slot to Foup_Slot        
                    WriteLog("Create CJ ->" + strCJID);
                    if (CreateCJPJ(ref selectWaferInfo, strCJID, applyEQ, bUndoForReadFail, strRecipe, bNoAign) == false)
                    {
                        WriteLog("Create CJPJ Fail.");
                        return false;
                    }
                    if (ExecuteCJPJ(strRecipe, bNoAign, strCJID, applyEQ) == false)//鎖ORDER                  
                    {
                        WriteLog("Execute CJPJ Fail.");
                        return false;
                    }
                    m_strRecipeRecord = strRecipe;

                    m_MySQL.TruncateWaferTransfer();//傳送開始清空Undo紀錄

                    this.AutoProcessStart(this, new EventArgs());//自動開始    
                    bSuccess = true;
                }
                catch (Exception ex) { WriteLog("<Exception>" + ex); }
            }
            return bSuccess;
        }
        private bool CreateCJPJ(ref ConcurrentQueue<clsSelectWaferInfo> selectWaferInfo, string strCJID, bool[] applyEQ, bool bUndoForReadFail = false, string OCR_Recipe = "", bool UseAligner = false)
        {
            lock (this)
            {
                bool bSuccess = false;
                string StrFunction = "";
                try
                {
                    #region Safety          
                    //異常不能做
                    if (m_alarm.CurrentAlarm != null && m_alarm.IsAlarm())
                    {
                        if (m_alarm.IsOnlyWarning() == false)
                            return false;
                    }
                    //LP狀態不對不做
                    foreach (clsSelectWaferInfo clsSelectWaferInfo in selectWaferInfo.ToArray())
                    {
                        if (clsSelectWaferInfo.SourceLpBodyNo > 0)
                        {
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].FoupExist == false) { return false; }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].StatusMachine == enumStateMachine.PS_Process) { return false; }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].StatusMachine != enumStateMachine.PS_Docked) { return false; }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].FoupID.Trim().Length <= 0) { return false; }
                            foreach (char item in ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].MappingData) { if (item != '0' && item != '1') { return false; } }
                        }

                        if (clsSelectWaferInfo.TargetLpBodyNo > 0)
                        {
                            if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].FoupExist == false) { return false; }
                            if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].StatusMachine == enumStateMachine.PS_Process && bUndoForReadFail == false) { return false; }
                            if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].StatusMachine != enumStateMachine.PS_Docked && bUndoForReadFail == false) { return false; }
                            if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].FoupID.Trim().Length <= 0) { return false; }
                            foreach (char item in ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].MappingData) { if (item != '0' && item != '1') { return false; } }
                        }
                    }
                    #endregion
                    //=========== 資料準備
                    string strPJID = string.Empty;
                    m_JobControl.CreateCJ(strCJID);//建立CJ sTransfer
                    //===========解析使用者選擇資料，轉換成 CJ PJ
                    while (selectWaferInfo.Count() > 0)
                    {
                        SpinWait.SpinUntil(() => false, 100);//遇過兩個PJ建立太快名稱相同造成執行Transfer判斷錯誤
                        clsSelectWaferInfo temp, temp_next;
                        if (selectWaferInfo.TryPeek(out temp) == false) { break; }
                        //建立一個PJ
                        strPJID = "PJID-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");//PJ:單Foup哪幾片要做
                        m_JobControl.CreatePJ(strPJID);
                        WriteLog("Create PJ->" + strPJID);
                        // 建立 PJ 中 SourceTransInfo
                        string strFoupID = "Unknow";

                        strFoupID = ListSTG[temp.SourceLpBodyNo - 1].FoupID;

                        m_JobControl.PJlist[strPJID].CreateSourceTransInfo(temp.SourceLpBodyNo, strFoupID);

                        //建立DB第一層 ProcessLog
                        m_dbProcess.CreateProcessLotInfo(DateTime.Now, strFoupID, strCJID, strPJID);

                        while (selectWaferInfo.Count() > 0)
                        {
                            if (selectWaferInfo.TryDequeue(out temp) == false) { break; }

                            string strSourceCarrierID = "Unknow";
                            strSourceCarrierID = ListSTG[temp.SourceLpBodyNo - 1].FoupID;

                            string strTargetCarrierID = "Unknow";
                            strTargetCarrierID = ListSTG[temp.TargetLpBodyNo - 1].FoupID;


                            if (temp.SourceLpBodyNo != -1 && temp.TargetLpBodyNo != -1)
                            {
                                StrFunction = "Sorter Function";
                            }

                            m_JobControl.PJlist[strPJID].RecipeName = StrFunction;
                            // PJ 中 SourceTransInfo 加入 TransInfo slot to slot
                            m_JobControl.PJlist[strPJID].CreateSourceSlotInfo(strSourceCarrierID, temp.SourceSlotIdx + 1);
                            m_JobControl.PJlist[strPJID].AssginSourceSlotInfo(
                                strSourceCarrierID, temp.SourceSlotIdx + 1,
                                strTargetCarrierID, temp.TargetSlotIdx + 1,
                                temp.TargetLpBodyNo,
                                applyEQ,
                                temp.NotchAngle,
                                "",
                                "",
                                !UseAligner,
                                (OCR_Recipe != "NoOCR") ? true : false
                                , OCR_Recipe
                                );


                            //檢視下一筆
                            if (selectWaferInfo.TryPeek(out temp_next))
                            {
                                if (temp.SourceLpBodyNo != temp_next.SourceLpBodyNo ||
                                    temp.TargetLpBodyNo != temp_next.TargetLpBodyNo)//目標不相同因此需要重新建立 PJ 與 Lot
                                {
                                    m_JobControl.CJlist[strCJID].AssignPJ(m_JobControl.CJlist[strCJID].PJCount + 1, m_JobControl.PJlist[strPJID]); // SECS CJ Assgin PJ
                                    break;
                                }
                            }

                        }
                    }
                    m_JobControl.CJlist[strCJID].AssignPJ(m_JobControl.CJlist[strCJID].PJCount + 1, m_JobControl.PJlist[strPJID]); // SECS CJ Assgin PJ                  
                    m_Gem.MaunalProcessRegistCJ(strCJID);
                    m_JobControl.CJlist[strCJID].AutoStart = true;
                    m_JobControl.CJlist[strCJID].Status = JobStatus.QUEUED;

                    foreach (SProcessJobObject ExePj in m_JobControl.CJlist[strCJID].PJList.Values.ToArray())
                    {
                        ExePj.AutoStart = true;
                        ExePj.Status = JobStatus.QUEUED;
                    }

                    bSuccess = true;
                }
                catch (Exception ex) { WriteLog("<Exception>" + ex); }
                return bSuccess;
            }
        }
        private bool ExecuteCJPJ(string strRecipe, bool bNoAign, string strCJID, bool[] applyEQ)
        {
            //bool bWaferInStocker, bWaferOutStocker;
            //===========依照 CJ 中的 PJ 更改 Wafer 資料
            foreach (SProcessJobObject pj in m_JobControl.CJlist[strCJID].PJList.Values)//PJ
            {
                foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in pj.SourceTransInfoList)//SSourceTransInfo
                {
                    string strToFoupID;

                    I_Loadport targetLoader = ListSTG[sourceTransInfo.TargetSTG - 1];
                    targetLoader.CJID = strCJID;
                    strToFoupID = targetLoader.FoupID;

                    I_Loadport sourceLoader = ListSTG[sourceTransInfo.SourceSTG - 1];
                    foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList)//TransferInfo
                    {

                        SWafer theWafer = sourceLoader.Waferlist[transferInfo.SourceSlot - 1];

                        //Taget

                        // theWafer.ToLoadport = SWafer.enumFromLoader.LoadportA + sourceTransInfo.TargetSTG - 1;
                        sourceLoader.StatusMachine = enumStateMachine.PS_Process;

                        theWafer.ToFoupID = strToFoupID;
                        theWafer.ToSlot = transferInfo.TargetSlot;
                    }


                }
            }

            return true;
        }


        public bool ExecuteCJPJ(bool bNoAign, string strCJID)
        {

            this.AutoProcessStart(this, new EventArgs());//自動開始   

            return true;
        } //Remote

        private int eqRoundRobinIndex = 0;
        private SWafer.enumPosition GetNextAvailableEQ(bool[] applyEQ, ref int rrIndex)
        {
            if (applyEQ == null || applyEQ.Length < 4)
                throw new ArgumentException("ApplyEQ 必須至少有 4 個元素");

            for (int i = 0; i < 4; i++)
            {
                int idx = (rrIndex + i) % 4;

                if (applyEQ[idx])
                {
                    rrIndex = (idx + 1) % 4;
                    return SWafer.enumPosition.EQM1 + idx;
                }
            }

            throw new Exception("ApplyEQ 沒有任何可用的 EQ");
        }




        #endregion







    }
}
