using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using RorzeUnit.Class;
using RorzeUnit.Interface;
using RorzeComm.Log;
using RorzeUnit.Event;
using RorzeUnit.Class.E84.Enum;
using System.Windows.Forms;
using RorzeComm.Threading;
using System.Threading;
using System.Runtime.CompilerServices;
using RorzeComm;
using RorzeUnit.Class.CIPC;
using RorzeUnit.Class.E84.Enum;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.RC500;
using RorzeUnit.Class.Aligner;
using RorzeUnit.Class.Camera;

namespace RorzeApi.Class
{
    public class AlarmEventArgs : EventArgs
    {
        public int AlarmID { get; set; }        //  10 00 00000
        public string Type { get; set; }        //  alarm cancel
        public string UnitType { get; set; }    //  system RobotA AlignerA Loadport1
        public string AlarmMsg { get; set; }
        public DateTime CreateTime { get; set; }

        public AlarmEventArgs(int _AlarmID, string _Type, string _UnitType, string _AlarmMsg, DateTime _CreateTime)
        {
            this.AlarmID = _AlarmID;
            this.Type = _Type;
            this.UnitType = _UnitType;
            this.AlarmMsg = _AlarmMsg;
            this.CreateTime = _CreateTime;
        }
    }
    public class SAlarm
    {
        public delegate void AlarmEventHandler(AlarmEventArgs args); //使用自定繼承    

        //========== 定義結構
        public struct CurrentAlarmItem
        {
            public int AlarmID;
            public string Type;
            public string UnitType;
            public string AlarmMsg;
            public DateTime CreateTime;
        }

        //==========  db;
        private SProcessDB _accessDBlog;
        private SAlarmListDB _dbAlarmList;

        //==========  Unit     
        private List<I_Robot> ListTRB;
        private List<I_Loadport> ListSTG;
        private List<I_Aligner> ListALN;
        private List<I_RC5X0_IO> ListDIO;
        private List<I_E84> ListE84;
        private List<I_Buffer> ListBUF;

        //========== EventHandler
        public event EventHandler NotifyCloseSoftware;
        public event AlarmEventHandler OnAlarmOccurred;     //通知外部事件回傳
        public event AlarmEventHandler OnAlarmRemove;       //通知外部事事件回傳

        public dlgb_v dlgIsTransfer { get; set; }
        //========== OneThread
        public SInterruptOneThread exeAlarmReset;
        public SInterruptOneThreadINT exeWarningReset;

        public List<CurrentAlarmItem> CurrentAlarm;//發生的異常儲存在裡面

        #region ENUM
        public enum enumAlarmType : int
        { CustomError = 10, UnitError = 11, UnitCancel = 12, E84 = 13, Warning = 14 };
        public enum enumAlarmUnit : int
        {
            System = 10,
            TRB1 = 11, TRB2 = 12,
            ALN1 = 13, ALN2 = 14,
            STG1 = 15, STG2 = 16, STG3 = 17, STG4 = 18, STG5 = 19, STG6 = 20, STG7 = 21, STG8 = 22,

            DIO0 = 25, DIO1 = 26, DIO2 = 27, DIO3 = 28, DIO4 = 29, DIO5 = 30,
            TBL1 = 31, TBL2 = 32, TBL3 = 33, TBL4 = 34, TBL5 = 35, TBL6 = 36,

            CAM1 = 37, CAM2 = 38,

            EQ = 23,


        };

        //  系統異常 AABBCCCCC = 10XXXXXXX
        public enum enumAlarmCode : int
        {
            System_EMO_is_turned_on_and_the_system_will_shut_down = 101000000,
            OCR_Reading_Failed_M12 = 101000001,
            OCR_Reading_Failed_T7 = 101000002,
            OCR_Manually_KeyIn_Abort = 101000003,
            OCR_Result_Mismatch_With_Host_Assign = 101000004,
            Oxygen_concentration_abnormally_lower_than_19_5 = 101400003,

            //RC550 DIO0 Card
            Pressure_difference_abnormal_EFEM = 101400016,

            DVR_abnormal = 101400019,
            PowerFan1_abnormal = 101400020,
            PowerFan2_abnormal = 101400021,

            //RC530 DIO1 Card
            DIO1_bit00_signal_abnormal = 101400100,
            DIO1_bit01_signal_abnormal = 101400101,
            DIO1_bit02_signal_abnormal = 101400102,
            DIO1_bit03_signal_abnormal = 101400103,
            DIO1_bit04_signal_abnormal = 101400104,
            DIO1_bit05_signal_abnormal = 101400105,
            DIO1_bit06_signal_abnormal = 101400106,
            DIO1_bit07_signal_abnormal = 101400107,
            DIO1_bit08_signal_abnormal = 101400108,
            DIO1_bit09_signal_abnormal = 101400109,
            DIO1_bit10_signal_abnormal = 101400110,
            DIO1_bit11_signal_abnormal = 101400111,
            DIO1_bit12_signal_abnormal = 101400112,
            DIO1_bit13_signal_abnormal = 101400113,
            DIO1_bit14_signal_abnormal = 101400114,
            DIO1_bit15_signal_abnormal = 101400115,

            //RC530 DIO2 Card
            DIO2_bit00_signal_abnormal = 101400120,
            DIO2_bit01_signal_abnormal = 101400121,
            DIO2_bit02_signal_abnormal = 101400122,
            DIO2_bit03_signal_abnormal = 101400123,
            DIO2_bit04_signal_abnormal = 101400124,
            DIO2_bit05_signal_abnormal = 101400125,
            DIO2_bit06_signal_abnormal = 101400126,
            DIO2_bit07_signal_abnormal = 101400127,
            DIO2_bit08_signal_abnormal = 101400128,
            DIO2_bit09_signal_abnormal = 101400129,
            DIO2_bit10_signal_abnormal = 101400130,
            DIO2_bit11_signal_abnormal = 101400131,
            DIO2_bit12_signal_abnormal = 101400132,
            DIO2_bit13_signal_abnormal = 101400133,
            DIO2_bit14_signal_abnormal = 101400134,
            DIO2_bit15_signal_abnormal = 101400135,
            //RC530 DIO3 Card
            DIO3_bit00_signal_abnormal = 101400130,
            DIO3_bit01_signal_abnormal = 101400131,
            DIO3_bit02_signal_abnormal = 101400132,
            DIO3_bit03_signal_abnormal = 101400133,
            DIO3_bit04_signal_abnormal = 101400134,
            DIO3_bit05_signal_abnormal = 101400135,
            DIO3_bit06_signal_abnormal = 101400136,
            DIO3_bit07_signal_abnormal = 101400137,
            DIO3_bit08_signal_abnormal = 101400138,
            DIO3_bit09_signal_abnormal = 101400139,
            DIO3_bit10_signal_abnormal = 101400140,
            DIO3_bit11_signal_abnormal = 101400141,
            DIO3_bit12_signal_abnormal = 101400142,
            DIO3_bit13_signal_abnormal = 101400143,
            DIO3_bit14_signal_abnormal = 101400144,
            DIO3_bit15_signal_abnormal = 101400145,
            //RC530 DIO4 Card
            DIO4_bit00_signal_abnormal = 101400150,
            DIO4_bit01_signal_abnormal = 101400151,
            DIO4_bit02_signal_abnormal = 101400152,
            DIO4_bit03_signal_abnormal = 101400153,
            DIO4_bit04_signal_abnormal = 101400154,
            DIO4_bit05_signal_abnormal = 101400155,
            DIO4_bit06_signal_abnormal = 101400156,
            DIO4_bit07_signal_abnormal = 101400157,
            DIO4_bit08_signal_abnormal = 101400158,
            DIO4_bit09_signal_abnormal = 101400159,
            DIO4_bit10_signal_abnormal = 101400160,
            DIO4_bit11_signal_abnormal = 101400161,
            DIO4_bit12_signal_abnormal = 101400162,
            DIO4_bit13_signal_abnormal = 101400163,
            DIO4_bit14_signal_abnormal = 101400164,
            DIO4_bit15_signal_abnormal = 101400165,            
        }
        #endregion

        private SLogger _logger = SLogger.GetLogger("Errorlog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[sALARM] {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
            //GMotion.theInst.SendWeChatMessageAsync(strContent, strContent);
        }
        //========== constructor
        public SAlarm(SProcessDB accessDBlog, SAlarmListDB dbAlarmList,
            List<I_Robot> robotList,
            List<I_Loadport> loadportList,
            List<I_E84> e84List,
            List<I_Aligner> alnList,
            List<I_RC5X0_IO> dioList, List<I_Buffer> bufferList)
        {
            try
            {
                //========== assign all of device
                _accessDBlog = accessDBlog;         //  RecordsLog.MDB
                _dbAlarmList = dbAlarmList; //  AlarmList.MDB

                ListTRB = robotList;
                ListSTG = loadportList;
                ListE84 = e84List;
                ListALN = alnList;
                ListDIO = dioList;
                ListBUF = bufferList;

                #region 異常相關註冊

                foreach (I_Robot robot in ListTRB)
                {
                    if (robot == null || robot.Disable) continue;
                    robot.OnOccurStatErr += new OccurErrorEventHandler(_OnOccurTRB_StatErr);
                    robot.OnOccurCancel += new OccurErrorEventHandler(_OnOccurTRB_Cancel);
                    robot.OnOccurCustomErr += new OccurErrorEventHandler(_OnOccurTRB_CustomErr);
                    robot.OnOccurErrorRest += new OccurErrorEventHandler(_OnOccurTRB_RestErr);
                }

                foreach (I_Aligner item in ListALN)
                {
                    if (item == null || item.Disable) continue;
                    item.OnOccurStatErr += new OccurErrorEventHandler(_OnOccurALN_StatErr);
                    item.OnOccurCancel += new OccurErrorEventHandler(_OnOccurALN_Cancel);
                    item.OnOccurCustomErr += new OccurErrorEventHandler(_OnOccurALN_CustomErr);
                    item.OnOccurErrorRest += new OccurErrorEventHandler(_OnOccurALN_RestErr);
                    item.OnOccurWarning += new OccurErrorEventHandler(_OnOccurALN_Warning);
                    item.OnOccurWarningRest += new OccurErrorEventHandler(_OnOccurALN_WarningRest);

                    if (item is SSAlignerPanelXYR)
                    {
                        SSAlignerPanelXYR alignerPanelXYR = (SSAlignerPanelXYR)item;

                        alignerPanelXYR.Camera.OnOccurStatErr += new OccurErrorEventHandler(_OnOccurCAM_StatErr);
                        alignerPanelXYR.Camera.OnOccurCancel += new OccurErrorEventHandler(_OnOccurCAM_Cancel);
                        alignerPanelXYR.Camera.OnOccurCustomErr += new OccurErrorEventHandler(_OnOccurCAM_CustomErr);
                        alignerPanelXYR.Camera.OnOccurErrorRest += new OccurErrorEventHandler(_OnOccurCAM_RestErr);

                    }


                }

                foreach (I_Loadport loader in ListSTG)
                {
                    if (loader == null || loader.Disable) continue;
                    loader.OnOccurStatErr += new OccurErrorEventHandler(_OnOccurSTG_StatErr);
                    loader.OnOccurCancel += new OccurErrorEventHandler(_OnOccurSTG_Cancel);
                    loader.OnOccurCustomErr += new OccurErrorEventHandler(_OnOccurSTG_CustomErr);
                    loader.OnOccurErrorRest += new OccurErrorEventHandler(_OnOccurSTG_RestErr);
                    loader.OnOccurWarning += new OccurErrorEventHandler(_OnOccurSTG_Warning);
                    loader.OnOccurWarningRest += new OccurErrorEventHandler(_OnOccurSTG_RestWarning);
                }

                foreach (I_RC5X0_IO dio in ListDIO)
                {
                    if (dio == null || dio.Disable) continue;
                    dio.OnOccurStatErr += new OccurErrorEventHandler(_OnOccurDIO_StatErr);
                    dio.OnOccurCancel += new OccurErrorEventHandler(_OnOccurDIO_Cancel);
                    dio.OnOccurCustomErr += new OccurErrorEventHandler(_OnOccurDIO_CustomErr);
                    dio.OnOccurErrorRest += new OccurErrorEventHandler(_OnOccurDIO_RestErr);
                }

                foreach (I_E84 e84 in ListE84)
                {
                    if (e84 == null || e84.Disable) continue;
                    e84.OnOccurError += new OccurErrorEventHandler(_OnOccurE84_CustomErr);
                    e84.OnOccurErrorRest += new OccurErrorEventHandler(_OnOccurE84_RestErr);
                }

                foreach (SSEquipment eq in SystemContext.Instance.ListEQM)
                {
                    if (eq == null || eq.Disable) continue;
                    eq.OnOccurError += new OccurErrorEventHandler(_OnOccurEQ_CustomErr);
                    eq.OnOccurErrorRest += new OccurErrorEventHandler(_OnOccurEQ_RestErr);
                }

                #endregion

                foreach (I_RC5X0_IO dio in ListDIO)
                {
                    if (dio == null || dio.Disable) continue;
                    if (dio is SSRC550_IO)
                    {
                        dio.OnOccurGPRS += _rc550_OnOccurSensorChange;
                        dio.OnNotifyEvntGDIO += _rc550_OnOccurInIOChange;
                    }
                    else if (dio is SSRC530_IO)
                    {
                        dio.OnNotifyEvntGDIO += _rc530_OnOccurInIOChange;
                    }
                }

                CurrentAlarm = new List<CurrentAlarmItem>();

                exeAlarmReset = new SInterruptOneThread(RunAlarmReset);//異常解除的一次序
                exeWarningReset = new SInterruptOneThreadINT(RunWarningReset);
                RestAlarmList();//  將DB裡面紀錄發生過的異常清除

            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }


        ~SAlarm() { AlarmBuzzerOff(); }



        void _OnOccurTRB_StatErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Robot unit = sender as I_Robot;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.TRB1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurTRB_Cancel(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Robot unit = sender as I_Robot;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.TRB1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurTRB_CustomErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Robot unit = sender as I_Robot;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.TRB1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurTRB_RestErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Robot unit = sender as I_Robot;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.TRB1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        //=========================================================================
        void _OnOccurALN_StatErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Aligner unit = sender as I_Aligner;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.ALN1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurALN_Cancel(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Aligner unit = sender as I_Aligner;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.ALN1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurALN_CustomErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Aligner unit = sender as I_Aligner;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.ALN1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurALN_RestErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Aligner unit = sender as I_Aligner;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.ALN1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurALN_WarningRest(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Aligner unit = sender as I_Aligner;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.ALN1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.Warning * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurALN_Warning(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Aligner unit = sender as I_Aligner;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.ALN1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.Warning * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex) { WriteLog("<<Exception>>:" + ex); }
        }
        //-------------------------------------------------------------------------
        void _OnOccurCAM_StatErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                SSCamera unit = sender as SSCamera;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.CAM1 + unit._BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurCAM_Cancel(object sender, OccurErrorEventArgs e)
        {
            try
            {
                SSCamera unit = sender as SSCamera;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.CAM1 + unit._BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurCAM_CustomErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                SSCamera unit = sender as SSCamera;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.CAM1 + unit._BodyNo - 1) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurCAM_RestErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                SSCamera unit = sender as SSCamera;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.CAM1 + unit._BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        //=========================================================================
        void _OnOccurSTG_StatErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Loadport unit = sender as I_Loadport;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.STG1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurSTG_Cancel(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Loadport unit = sender as I_Loadport;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.STG1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurSTG_CustomErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Loadport unit = sender as I_Loadport;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.STG1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurSTG_RestErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Loadport unit = sender as I_Loadport;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.STG1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurSTG_Warning(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Loadport unit = sender as I_Loadport;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.STG1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.Warning * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex) { WriteLog("<<Exception>>:" + ex); }
        }
        void _OnOccurSTG_RestWarning(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_Loadport unit = sender as I_Loadport;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.STG1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.Warning * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex) { WriteLog("<<Exception>>:" + ex); }
        }
        //=========================================================================
        void _OnOccurDIO_StatErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_RC5X0_IO unit = sender as I_RC5X0_IO;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.DIO0 + unit.BodyNo) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurDIO_Cancel(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_RC5X0_IO unit = sender as I_RC5X0_IO;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.DIO0 + unit.BodyNo) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurDIO_CustomErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_RC5X0_IO unit = sender as I_RC5X0_IO;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.DIO0 + unit.BodyNo) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurDIO_RestErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_RC5X0_IO unit = sender as I_RC5X0_IO;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.DIO0 + unit.BodyNo) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        //=========================================================================
        void _OnOccurE84_CustomErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_E84 unit = sender as I_E84;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.STG1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.E84 * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurE84_RestErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                I_E84 unit = sender as I_E84;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.STG1 + unit.BodyNo - 1) * 10000000 + (int)enumAlarmType.E84 * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        //=========================================================================
        void _OnOccurEQ_CustomErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                SSEquipment unit = sender as SSEquipment;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.EQ ) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                writeAlarm(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        void _OnOccurEQ_RestErr(object sender, OccurErrorEventArgs e)
        {
            try
            {
                SSEquipment unit = sender as SSEquipment;
                int alarmID = e.ErrorCode + ((int)enumAlarmUnit.EQ) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                AlarmReset(alarmID);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        //=========================================================================
        void _rc550_OnOccurSensorChange(object sender, int[] nValue)
        {
            try
            {
                I_RC5X0_IO dio = sender as I_RC5X0_IO;
                if (dio is SSRC550_IO)
                    if (nValue != null && nValue.Length > 0 && GParam.theInst.RC550Pressure_Enable)
                    {
                        //Pa
                        double dValue = Convert.ToDouble(nValue[0]) / 1000;
                        if (dValue < GParam.theInst.RC550Pressure_Threshold && GParam.theInst.RC550Pressure_Threshold != 0)
                            writeAlarm((int)enumAlarmCode.Pressure_difference_abnormal_EFEM);
                        else
                            AlarmReset((int)enumAlarmCode.Pressure_difference_abnormal_EFEM);
                    }
            }
            catch (Exception ex) { WriteLog("<<Exception>>:" + ex); }
        }
        void _rc550_OnOccurInIOChange(object sender, RorzeUnit.Class.RC500.Event.NotifyGDIOEventArgs e)
        {
            try
            {
                if (e.Input == null || e.Input.Length != 16) return;

                I_RC5X0_IO dio = sender as I_RC5X0_IO;

                if ((dio is SSRC550_IO) && e.HCLID == 8)
                {
                    for (int i = 0; i < 16; i++)
                        switch (i)
                        {
                            case 0:
                                if (GParam.theInst.PowerFan1Alarmn_Disable) continue;

                                if (e.Input[i] == false)//FAN1
                                    writeAlarm((int)enumAlarmCode.PowerFan1_abnormal);
                                else
                                    AlarmReset((int)enumAlarmCode.PowerFan1_abnormal);
                                break;
                            case 1:
                                if (GParam.theInst.PowerFan2Alarmn_Disable) continue;

                                if (e.Input[i] == false)//FAN2
                                    writeAlarm((int)enumAlarmCode.PowerFan2_abnormal);
                                else
                                    AlarmReset((int)enumAlarmCode.PowerFan2_abnormal);
                                break;
                            case 2:
                                if (GParam.theInst.DVRAlarmn_Disable) continue;

                                if (e.Input[i] == true && !GParam.theInst.DVRAlarmn_Disable)//DVR
                                    writeAlarm((int)enumAlarmCode.DVR_abnormal);
                                else
                                    AlarmReset((int)enumAlarmCode.DVR_abnormal);
                                break;
                        }
                }

            }
            catch (Exception ex) { WriteLog("<<Exception>>:" + ex); }
        }
        void _rc530_OnOccurInIOChange(object sender, RorzeUnit.Class.RC500.Event.NotifyGDIOEventArgs e)
        {
            try
            {
                if (e.HCLID != 0 || e.Input == null || e.Input.Length != 16) return;

                I_RC5X0_IO dio = sender as I_RC5X0_IO;

                if (dio is SSRC530_IO)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        bool bAbnormal = GParam.theInst.Check_DIO_IO_Abnormal(dio.BodyNo, i, e.Input[i]);

                        string strIOName = GParam.theInst.GetDIO_DIName(dio.BodyNo, 0)[i];

                        if (bAbnormal)
                            writeAlarm((int)enumAlarmCode.DIO1_bit00_signal_abnormal + i + 20 * (dio.BodyNo - 1), " " + strIOName);
                        else
                            AlarmReset((int)enumAlarmCode.DIO1_bit00_signal_abnormal + i + 20 * (dio.BodyNo - 1));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }



        //  發生異常 檢查處理 DB Alarmlist
        public void writeAlarm(int alarmID, string strAddDescription = "")
        {
            DateTime TimeNow = DateTime.Now;
            try
            {
                //  找DB AlarmCode && Ocur
                DataSet AlarmList = _dbAlarmList.SelectAlarmOcur(alarmID, true);

                //檢查是否已經發生,已發生return
                if (AlarmList.Tables[0] == null || AlarmList.Tables[0].Rows.Count > 0)
                    return;
                //  找DB AlarmCode
                AlarmList = _dbAlarmList.SelectAlarmList(alarmID);

                if (AlarmList.Tables[0] == null) return;
                //  先掃一遍如果找不到先加進去
                string strType = "Unknow", strUnitType = "Unknow", strAlarmMsg = "Unknow";
                //  辨識Unit與Type
                AnalysisAlarmID(alarmID, ref strType, ref strUnitType);
                //  有在AlarmList找到對應
                if (AlarmList.Tables[0].Rows.Count > 0)
                {
                    _dbAlarmList.SetAlarmOcur(alarmID, true);// 設定已發生
                    strType = AlarmList.Tables[0].Rows[0]["Type"].ToString();
                    strUnitType = AlarmList.Tables[0].Rows[0]["UnitType"].ToString();
                    strAlarmMsg = AlarmList.Tables[0].Rows[0]["AlarmMsg"].ToString();

                    if (strAddDescription != "") strAlarmMsg += strAddDescription;

                    #region 客戶需求想要在異常敘述增加文字
                    //Loadport1~8
                    for (int i = 0; i < Enum.GetNames(typeof(enumLoadport)).Count(); i++)
                    {
                        if (strUnitType.Contains("STG" + (i + 1)))//Loadport異常
                        {
                            if (strType.Contains(enumAlarmType.Warning.ToString()))
                                strAlarmMsg = strAlarmMsg.Replace("Port", "Port" + (i + 1));//Port -> Port1


                            if (strType.Contains(enumAlarmType.CustomError.ToString()))//Slot
                            {
                                if (strAlarmMsg.Contains("Mapping Thickness Thick"))
                                { strAlarmMsg += (" slot:" + (ListSTG[i].MappingData.IndexOf('2') + 1)); }
                                if (strAlarmMsg.Contains("Mapping Cross"))
                                { strAlarmMsg += (" slot:" + (ListSTG[i].MappingData.IndexOf('3') + 1)); }
                                if (strAlarmMsg.Contains("Mapping FrontBow"))
                                { strAlarmMsg += (" slot:" + (ListSTG[i].MappingData.IndexOf('4') + 1)); }
                                if (strAlarmMsg.Contains("Mapping Double"))
                                { strAlarmMsg += (" slot:" + (ListSTG[i].MappingData.IndexOf('7') + 1)); }
                                if (strAlarmMsg.Contains("Mapping Thickness Thin"))
                                { strAlarmMsg += (" slot:" + (ListSTG[i].MappingData.IndexOf('8') + 1)); }
                                if (strAlarmMsg.Contains("Mapping Abnormal"))
                                { strAlarmMsg += (" slot:" + (ListSTG[i].MappingData.IndexOf('9') + 1)); }
                            }

                            if (ListSTG[i].FoupExist && ListSTG[i].FoupID != "")
                                strAlarmMsg += "-CarrierID:" + ListSTG[i].FoupID;
                        }
                    }
                    #endregion
                }
                else
                {
                    //  如果沒有加進去DB
                    _dbAlarmList.UpdataAlarmList(alarmID, strType, strUnitType, "Unknow");
                }



                //  紀錄Alarm Log
                _accessDBlog.InsertAlarmLog(TimeNow.ToString("yyyy/MM/dd HH:mm:ss"), strType, strUnitType, alarmID.ToString(), strAlarmMsg);
                //  儲存置軟體紀錄，用於後續解除異常判斷
                AddAlarm(new CurrentAlarmItem() { AlarmID = alarmID, Type = strType, UnitType = strUnitType, AlarmMsg = strAlarmMsg, CreateTime = TimeNow });
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
                _accessDBlog.InsertAlarmLog(TimeNow.ToString("yyyy-MM-dd HH:mm:ss"), "Warning", "System", "99999", "Unknown exception code");
            }
        }
        //  異常解除 檢查處理 DB Alarmlist
        public void AlarmReset(int alarmID)
        {
            DateTime TimeNow = DateTime.Now;
            try
            {
                DataSet AlarmList = _dbAlarmList.SelectAlarmOcur(alarmID, false);

                // 檢查是否已經發生,已發生return
                if (AlarmList.Tables[0] == null || AlarmList.Tables[0].Rows.Count > 0)
                    return;

                //檢查AlarmList,是否存在,不存在return
                AlarmList = _dbAlarmList.SelectAlarmList(alarmID);
                // 找不到
                if (AlarmList.Tables[0] == null || AlarmList.Tables[0].Rows.Count < 1)
                    return;

                _dbAlarmList.SetAlarmOcur(alarmID, false);

                //  中找 CurrentAlarm
                foreach (SAlarm.CurrentAlarmItem item in CurrentAlarm)
                {
                    if (item.AlarmID == alarmID)
                    {
                        RemoveAlarm(item);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }


        //  處理 list CurrentAlarm 與通知外部
        private void AddAlarm(CurrentAlarmItem item)
        {
            try
            {
                WriteLog(string.Format("Occur Alarm Code:{0} {1},{2},{3}", item.AlarmID, item.Type, item.UnitType, item.AlarmMsg));
                CurrentAlarm.Add(item);

                OnAlarmOccurred?.Invoke(new AlarmEventArgs(item.AlarmID, item.Type, item.UnitType, item.AlarmMsg, item.CreateTime));
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        //  處理 list CurrentAlarm 與通知外部
        private void RemoveAlarm(CurrentAlarmItem item)
        {
            try
            {
                WriteLog(string.Format("Remove Alarm Code:{0} {1},{2},{3}", item.AlarmID, item.Type, item.UnitType, item.AlarmMsg));
                if (CurrentAlarm.Contains(item))
                    CurrentAlarm.Remove(item);

                OnAlarmRemove?.Invoke(new AlarmEventArgs(item.AlarmID, item.Type, item.UnitType, item.AlarmMsg, item.CreateTime));
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }


        //  將DB裡面紀錄發生過的異常清除
        private void RestAlarmList()
        {
            DateTime TimeNow = DateTime.Now;
            try
            {
                DataSet AlarmList = _dbAlarmList.SelectAlarmOcur(true);
                //  檢查DB中發生異常全部消除
                if (AlarmList.Tables[0] == null || AlarmList.Tables[0].Rows.Count < 1)
                    return;
                for (int nCount = 0; nCount < AlarmList.Tables[0].Rows.Count; nCount++)
                    _dbAlarmList.SetAlarmOcur(int.Parse(AlarmList.Tables[0].Rows[nCount]["AlarmID"].ToString()), false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        //  OneThread Alarm Reset
        private void RunAlarmReset()
        {
            try
            {
                if (CurrentAlarm.Count == 0) return;

                SpinWait.SpinUntil(() => false, 100);

                //  以異常代碼判斷處理
                foreach (CurrentAlarmItem item in CurrentAlarm.ToArray())
                {
                    SpinWait.SpinUntil(() => false, 1);

                    #region 判斷要做的事情
                    //  EMO
                    if (item.AlarmID == (int)enumAlarmCode.System_EMO_is_turned_on_and_the_system_will_shut_down)
                    {
                        frmMessageBox frmMbox = new frmMessageBox("EMO is turned on and the system will shut down!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Question);
                        frmMbox.ShowDialog();
                        frmMbox.Focus();
                        NotifyCloseSoftware?.Invoke(this, new EventArgs());
                    }
                    else if (item.Type == enumAlarmType.E84.ToString())
                    {
                        //  STG1_E84 ErrorCode 151300000 
                        if (item.UnitType == enumAlarmUnit.STG1.ToString())
                        {
                            if (item.AlarmID % 100000 == (int)enumE84Warning.TP3_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.TP4_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.LightCurtainBusyOn)
                                ListE84[0].ResetError();
                        }
                        //  STG2_E84 ErrorCode 161300000 
                        else if (item.UnitType == enumAlarmUnit.STG2.ToString())
                        {
                            if (item.AlarmID % 100000 == (int)enumE84Warning.TP3_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.TP4_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.LightCurtainBusyOn)
                                ListE84[1].ResetError();
                        }
                        //  STG3_E84 ErrorCode 171300000 
                        else if (item.UnitType == enumAlarmUnit.STG3.ToString())
                        {
                            if (item.AlarmID % 100000 == (int)enumE84Warning.TP3_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.TP4_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.LightCurtainBusyOn)
                                ListE84[2].ResetError();
                        }
                        //  STG4_E84 ErrorCode 181300000 
                        else if (item.AlarmID / 100000 == 1813 && ListSTG[3].Disable == false)
                        {
                            if (item.AlarmID % 100000 == (int)enumE84Warning.TP3_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.TP4_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.LightCurtainBusyOn)
                                ListE84[3].ResetError();
                        }
                        //  STG5_E84 ErrorCode 191300000 
                        else if (item.AlarmID / 100000 == 1913 && ListSTG[4].Disable == false)
                        {
                            if (item.AlarmID % 100000 == (int)enumE84Warning.TP3_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.TP4_TimeOut)
                                ListE84[4].ResetError();
                        }
                        //  STG6_E84 ErrorCode 201300000 
                        else if (item.AlarmID / 100000 == 2013 && ListSTG[5].Disable == false)
                        {
                            if (item.AlarmID % 100000 == (int)enumE84Warning.TP3_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.TP4_TimeOut)
                                ListE84[5].ResetError();
                        }
                        //  STG7_E84 ErrorCode 211300000 
                        else if (item.AlarmID / 100000 == 2113 && ListSTG[6].Disable == false)
                        {
                            if (item.AlarmID % 100000 == (int)enumE84Warning.TP3_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.TP4_TimeOut)
                                ListE84[6].ResetError();
                        }
                        //  STG8_E84 ErrorCode 221300000 
                        else if (item.AlarmID / 100000 == 2213 && ListSTG[7].Disable == false)
                        {
                            if (item.AlarmID % 100000 == (int)enumE84Warning.TP3_TimeOut || item.AlarmID % 100000 == (int)enumE84Warning.TP4_TimeOut)
                                ListE84[7].ResetError();
                        }
                    }
                    else if (item.Type == enumAlarmType.UnitError.ToString())
                    {
                        //  TRB1 ErrorCode 111100000
                        if (item.UnitType == enumAlarmUnit.TRB1.ToString()) { ListTRB[0].RSTA(1); }
                        //  TRB2 ErrorCode 121100000
                        else if (item.UnitType == enumAlarmUnit.TRB2.ToString()) { ListTRB[1].RSTA(1); }
                        //  ALN1 ErrorCode 131100000
                        else if (item.UnitType == enumAlarmUnit.ALN1.ToString()) { ListALN[0].RSTA(1); }
                        //  ALN2 ErrorCode 141100000
                        else if (item.UnitType == enumAlarmUnit.ALN2.ToString()) { ListALN[1].RSTA(1); }
                        //  STG1 ErrorCode 151100000
                        else if (item.UnitType == enumAlarmUnit.STG1.ToString()) { ListSTG[0].RSTA(1); }
                        //  STG2 ErrorCode 161100000
                        else if (item.UnitType == enumAlarmUnit.STG2.ToString()) { ListSTG[1].RSTA(1); }
                        //  STG3 ErrorCode 171100000
                        else if (item.UnitType == enumAlarmUnit.STG3.ToString()) { ListSTG[2].RSTA(1); }
                        //  STG4 ErrorCode 181100000
                        else if (item.UnitType == enumAlarmUnit.STG4.ToString()) { ListSTG[3].RSTA(1); }
                        //  STG5 ErrorCode 191100000
                        else if (item.UnitType == enumAlarmUnit.STG5.ToString()) { ListSTG[4].RSTA(1); }
                        //  STG6 ErrorCode 201100000
                        else if (item.UnitType == enumAlarmUnit.STG6.ToString()) { ListSTG[5].RSTA(1); }
                        //  STG7 ErrorCode 211100000
                        else if (item.UnitType == enumAlarmUnit.STG7.ToString()) { ListSTG[6].RSTA(1); }
                        //  STG8 ErrorCode 221100000
                        else if (item.UnitType == enumAlarmUnit.STG8.ToString()) { ListSTG[7].RSTA(1); }
                        //  DIO0 ErrorCode 251100000
                        else if (item.UnitType == enumAlarmUnit.DIO0.ToString()) { ListDIO[0].RSTA(); }
                        //  DIO1 ErrorCode 261100000
                        else if (item.UnitType == enumAlarmUnit.DIO1.ToString()) { ListDIO[1].RSTA(); }
                        //  DIO2 ErrorCode 271100000
                        else if (item.UnitType == enumAlarmUnit.DIO2.ToString()) { ListDIO[2].RSTA(); }
                        //  DIO3 ErrorCode 281100000
                        else if (item.UnitType == enumAlarmUnit.DIO3.ToString()) { ListDIO[2].RSTA(); }


                        //  
                        else if (item.UnitType == enumAlarmUnit.CAM1.ToString()) { ((SSAlignerPanelXYR)ListALN[0]).Camera.RSTA(); }
                    }
                    #endregion

                    if (item.Type == enumAlarmType.Warning.ToString() && item.AlarmID / 10000000 == (int)enumAlarmUnit.System)//System
                    {
                        //系統警告不能解除，由RC530判斷IO自動reset
                    }
                    else if (item.Type == enumAlarmType.E84.ToString()/*item.AlarmID / 100000 % 100 == 13*/) //ErrorCode 151300000
                    {
                        //E84 ERROR
                        //if (item.AlarmID % 100000 != (int)enumE84Warning.TP3_TimeOut && item.AlarmID % 100000 != (int)enumE84Warning.TP4_TimeOut)
                        //    AlarmReset(item.AlarmID);
                    }
                    else if (item.Type == enumAlarmType.UnitError.ToString())
                    {
                        // Unit stat 異常不解除，由發送rsat(1)後應該會解除,Equipment視專案
                    }
                    else
                    {
                        AlarmReset(item.AlarmID);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }

        void RunWarningReset(int PortNo)
        {
            try
            {
                string PortStr = "STG" + PortNo.ToString();

                if (CurrentAlarm.Count == 0) return;

                SpinWait.SpinUntil(() => false, 100);

                //  以異常代碼判斷處理
                foreach (CurrentAlarmItem item in CurrentAlarm.ToArray())
                {
                    SpinWait.SpinUntil(() => false, 1);

                    if (item.Type == enumAlarmType.Warning.ToString())
                    {
                        if (item.UnitType == PortStr)
                        {
                            AlarmReset(item.AlarmID);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }

        // ========== public
        public void AlarmLightBuzzerOn(bool blinking)
        {
            try
            {
                if (ListDIO[1].Connected == false) return;
                //  燈塔亮燈             
                AlarmBuzzerOn();
                AlarmLightOn(blinking);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        public void AlarmLightOn(bool blinking)
        {
            try
            {
                if (ListDIO[1].Connected == false) return;
                //  燈塔亮燈
                if (ListDIO[1].GetGDIO_OutputStatus(0, blinking ? 4 : 0) == false)
                {
                    ListDIO[1].SdobW(0, blinking ? 4 : 0, true);//閃爍紅燈                   
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        public void AlarmBuzzerOn()
        {
            try
            {
                if (ListDIO[1].Connected == false) return;
                //  燈塔亮燈
                if (ListDIO[1].GetGDIO_OutputStatus(0, 4) == false)
                {
                    if (ListDIO[1].GetGDIO_OutputStatus(0, 8) == false)
                        ListDIO[1].SdobW(0, 8, true);//Buzzer on
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        public void AlarmBuzzer2On()
        {
            try
            {
                if (ListDIO[1].Connected == false) return;
                //  燈塔亮燈  
                if (ListDIO[1].GetGDIO_OutputStatus(0, 4) == false)
                {
                    if (ListDIO[1].GetGDIO_OutputStatus(0, 9) == false)
                        ListDIO[1].SdobW(0, 9, true);//Buzzer on
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        public void AlarmLightOff()
        {
            try
            {
                if (ListDIO[1].Connected == false) return;
                //  燈塔亮燈
                //if (ListDIO[1].GetOutput(0, 0) == true)
                //    ListDIO[1].SDOB(0, 0, false);//紅燈      
                if (ListDIO[1].GetGDIO_OutputStatus(0, 4) == true)
                    ListDIO[1].SdobW(0, 4, false);//閃爍紅燈         
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }
        public void AlarmBuzzerOff()
        {
            try
            {
                if (ListDIO[1].Connected == false) return;

                if (ListDIO[1].GetGDIO_OutputStatus(0, 8) == true)
                    ListDIO[1].SdobW(0, 8, false);//Buzzer off
                if (ListDIO[1].GetGDIO_OutputStatus(0, 9) == true)
                    ListDIO[1].SdobW(0, 9, false);//Buzzer off
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }

        public bool IsAlarm()
        {
            if (CurrentAlarm == null) return false;
            return CurrentAlarm.Count != 0;
        }
        public bool IsOnlyWarning()//所有異常中有一個是alarm就不算Warning
        {
            if (CurrentAlarm.Count == 0) return false;
            foreach (CurrentAlarmItem item in CurrentAlarm)
                if (item.Type != enumAlarmType.Warning.ToString() && item.Type != enumAlarmType.E84.ToString()
                   /* && item.Type != "Cancel"*/)
                    return false;

            return true;
        }

        //檢查是否有OcurTime欄位
        public void CheckColumn_OcurTime()
        {
            _dbAlarmList.AddColumnToDB("OcurTime", "TEXT", 255);
            //
        }

        //直接更新覆寫DB
        public void UpdataAlarmList()
        {
            string strType = "Unknow", strUnit = "Unknow";
            #region 建立 [System]
            //  檢查 RorzeApi.Class.SAlarm.enumAlarmCode 加入DB中,自定義異常
            foreach (object value in Enum.GetValues(typeof(RorzeApi.Class.SAlarm.enumAlarmCode)))
            {
                AnalysisAlarmID((int)value, ref strType, ref strUnit);
                _dbAlarmList.AddAlarmCodeToDB((int)value, strType, strUnit, value.ToString());
            }
            #endregion
            #region 建立 [TRB] stat error code/cancel code/Custom error code,並且更新Alarm list DB內容
            for (int i = 0; i < ListTRB.Count; i++)
            {
                if (ListTRB[i] == null || ListTRB[i].Disable) continue;
                string strTitle = "Robot" + (i + 1) + " ";
                Dictionary<int, string> m_dicCancel = ListTRB[i].m_dicCancel;
                Dictionary<int, string> m_dicController = ListTRB[i].m_dicController;
                Dictionary<int, string> m_dicError = ListTRB[i].m_dicError;
                // Cancel Code
                foreach (var item in m_dicCancel)
                {
                    //  TRB1 11 12 00000                  
                    int nCode = item.Key + ((int)enumAlarmUnit.TRB1 + i) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + item.Value);
                }
                // Status Error Code
                foreach (var item1 in m_dicController)
                    foreach (var item2 in m_dicError)
                    {
                        //  TRB1 11 11 00000                      
                        string strCode = item1.Key.ToString("X2") + item2.Key.ToString("X2");
                        int nCode = Convert.ToInt32(strCode, 16) + ((int)enumAlarmUnit.TRB1 + i) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                        AnalysisAlarmID(nCode, ref strType, ref strUnit);
                        _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + item1.Value + item2.Value);
                    }
                // Custom Error Code(Robot.Enum.enumRobotError)   
                foreach (object value in Enum.GetValues(typeof(RorzeUnit.Class.Robot.Enum.enumRobotError)))
                {
                    //  TRB1 11 10 00000
                    //  TRB2 12 10 00000   
                    int nCode = (int)value + ((int)enumAlarmUnit.TRB1 + i) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + value.ToString());
                }
            }
            #endregion
            #region 建立 [ALN] stat error code/cancel code/Custom error code,並且更新Alarm list DB內容

            List<SSCamera> listCAM = new List<SSCamera>();
            for (int i = 0; i < ListALN.Count; i++)
            {
                if (ListALN[i] == null || ListALN[i].Disable) continue;
                string strTitle = "Aligner" + (i + 1) + " ";
                Dictionary<int, string> m_dicCancel = ListALN[i].m_dicCancel;
                Dictionary<int, string> m_dicController = ListALN[i].m_dicController;
                Dictionary<int, string> m_dicError = ListALN[i].m_dicError;
                // Cancel Code
                foreach (var item in m_dicCancel)
                {
                    //  ALN1 13 12 00000                  
                    int nCode = item.Key + ((int)enumAlarmUnit.ALN1 + i) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + item.Value);
                }
                // Status Error Code
                foreach (var item1 in m_dicController)
                    foreach (var item2 in m_dicError)
                    {
                        //  ALN1 13 11 00000                      
                        string strCode = item1.Key.ToString("X2") + item2.Key.ToString("X2");
                        int nCode = Convert.ToInt32(strCode, 16) + ((int)enumAlarmUnit.ALN1 + i) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                        AnalysisAlarmID(nCode, ref strType, ref strUnit);
                        _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + item1.Value + item2.Value);
                    }
                // Custom Error Code  
                foreach (object value in Enum.GetValues(typeof(RorzeUnit.Class.Aligner.Enum.enumAlignerError)))
                {
                    //  ALN1 13 10 00000
                    //  ALN2 13 10 00000   
                    int nCode = (int)value + ((int)enumAlarmUnit.ALN1 + i) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + value.ToString());
                }

                if (ListALN[i] is SSAlignerPanelXYR)
                {
                    SSAlignerPanelXYR alignerPanelXYR = (SSAlignerPanelXYR)ListALN[i];
                    listCAM.Add(alignerPanelXYR.Camera);
                }
            }

            for (int i = 0; i < listCAM.Count; i++)
            {
                if (listCAM[i] == null || listCAM[i]._Disable) continue;
                int nIndex = listCAM[i]._BodyNo - 1;
                string strTitle = "Camera" + (nIndex + 1) + " ";
                Dictionary<int, string> m_dicCancel = listCAM[i]._dicCancel;
                Dictionary<int, string> m_dicController = listCAM[i]._dicController;
                Dictionary<int, string> m_dicError = listCAM[i]._dicError;
                // Cancel Code
                foreach (var item in m_dicCancel)
                {
                    int nCode = item.Key + ((int)enumAlarmUnit.CAM1 + nIndex) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + item.Value);
                }
                // Status Error Code
                foreach (var item1 in m_dicController)
                    foreach (var item2 in m_dicError)
                    {
                        string strCode = item1.Key.ToString("X2") + item2.Key.ToString("X2");
                        int nCode = Convert.ToInt32(strCode, 16) + ((int)enumAlarmUnit.CAM1 + nIndex) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                        AnalysisAlarmID(nCode, ref strType, ref strUnit);
                        _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + item1.Value + item2.Value);
                    }
                // Custom Error Code  
                foreach (object value in Enum.GetValues(typeof(RorzeUnit.Class.Camera.Enum.enumCustomError)))
                {
                    int nCode = (int)value + ((int)enumAlarmUnit.CAM1 + nIndex) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + value.ToString());
                }

            }
            #endregion
            #region 建立 [STG] stat error code/cancel code/Custom error code,並且更新Alarm list DB內容
            for (int i = 0; i < ListSTG.Count; i++)
            {
                if (ListSTG[i] == null || ListSTG[i].Disable) continue;
                string strTitle = "Loadport" + (i + 1) + " ";
                foreach (object value in Enum.GetValues(typeof(enumE84Warning)))
                {
                    //  STG1 15 13 00000                  
                    int nCode = (int)value + ((int)enumAlarmUnit.STG1 + i) * 10000000 + (int)enumAlarmType.E84 * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + value.ToString());
                }

                Dictionary<int, string> m_dicCancel = ListSTG[i].m_dicCancel;
                Dictionary<int, string> m_dicController = ListSTG[i].m_dicController;
                Dictionary<int, string> m_dicError = ListSTG[i].m_dicError;
                // Cancel Code
                foreach (var item in m_dicCancel)
                {
                    //  STG1 15 12 00000                  
                    int nCode = item.Key + ((int)enumAlarmUnit.STG1 + i) * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + item.Value);
                }
                // Status Error Code
                foreach (var item1 in m_dicController)
                    foreach (var item2 in m_dicError)
                    {
                        //  STG1 15 11 00000                      
                        string strCode = item1.Key.ToString("X2") + item2.Key.ToString("X2");
                        int nCode = Convert.ToInt32(strCode, 16) + ((int)enumAlarmUnit.STG1 + i) * 10000000 + (int)enumAlarmType.UnitError * 100000;
                        AnalysisAlarmID(nCode, ref strType, ref strUnit);
                        _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + item1.Value + item2.Value);
                    }
                // Custom Error Code
                foreach (object value in Enum.GetValues(typeof(RorzeUnit.Class.Loadport.Enum.enumLoadPortError)))
                {
                    //  STG1 15 10 00000
                    //  STG1 16 10 00000   
                    int nCode = (int)value + ((int)enumAlarmUnit.STG1 + i) * 10000000 + (int)enumAlarmType.CustomError * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + value.ToString());
                }
                // Warning
                foreach (object value in Enum.GetValues(typeof(RorzeUnit.Class.Loadport.Enum.enumLoadPortWarning)))
                {
                    //  STG1 15 14 00000
                    //  STG2 16 14 00000   
                    int nCode = (int)value + ((int)enumAlarmUnit.STG1 + i) * 10000000 + (int)enumAlarmType.Warning * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, strTitle + value.ToString());
                }
            }
            #endregion
            #region 建立 [DIO] stat error code/cancel code/Custom error code,並且更新Alarm list DB內容
            for (int i = 0; i < ListDIO.Count; i++)
            {
                if (ListDIO[i] == null || ListDIO[i].Disable) continue;
                Dictionary<int, string> m_dicCancel = ListDIO[i].m_dicCancel;
                Dictionary<int, string> m_dicController = ListDIO[i].m_dicController;
                Dictionary<int, string> m_dicError = ListDIO[i].m_dicError;
                int nUnit = (int)enumAlarmUnit.DIO0 + i;
                // Cancel Code
                foreach (var item in m_dicCancel)
                {
                    //  DIO0 25 12 00000                 
                    int nCode = item.Key + nUnit * 10000000 + (int)enumAlarmType.UnitCancel * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, item.Value);
                }
                // Status Error Code
                foreach (var item1 in m_dicController)
                    foreach (var item2 in m_dicError)
                    {
                        //  DIO0 25 11 00000                       
                        string strCode = item1.Key.ToString("X2") + item2.Key.ToString("X2");
                        int nCode = Convert.ToInt32(strCode, 16) + nUnit * 10000000 + (int)enumAlarmType.UnitError * 100000;
                        AnalysisAlarmID(nCode, ref strType, ref strUnit);
                        _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, item1.Value + item2.Value);
                    }
                // Custom Error Code(RC500.RCEnum.enumRC500Error)  
                foreach (object value in Enum.GetValues(typeof(RorzeUnit.Class.RC500.RCEnum.enumRC500Error)))
                {
                    //  DIO0 25 10 00000                  
                    int nCode = (int)value + nUnit * 10000000 + (int)enumAlarmType.CustomError * 100000;
                    AnalysisAlarmID(nCode, ref strType, ref strUnit);
                    _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, value.ToString());
                }
            }
            #endregion
            #region 建立 [EQ]Custom error code,並且更新Alarm list DB內容
            foreach (object value in Enum.GetValues(typeof(RorzeUnit.Class.EQ.Enum.enumEQError)))
            {              
                int nCode = (int)value + (int)enumAlarmUnit.EQ * 10000000 + (int)enumAlarmType.CustomError * 100000;
                AnalysisAlarmID(nCode, ref strType, ref strUnit);
                _dbAlarmList.AddAlarmCodeToDB(nCode, strType, strUnit, value.ToString());
            }
            #endregion
        }

        //用error號碼辨識單元
        public void AnalysisAlarmID(int nAlarmID, ref string Type, ref string Unit)
        {
            try
            {
                //  解析 Alarm code : AABBCCCCC        
                string strAlarmID = nAlarmID.ToString("D9");
                string strType = strAlarmID.Substring(2, 2);//BB
                string strUnit = strAlarmID.Substring(0, 2);//AA

                enumAlarmType eAlarmType;
                if (Enum.TryParse(strType, out eAlarmType) == false)
                    Type = "Other";
                else
                    Type = eAlarmType.ToString();

                enumAlarmUnit eAlarmUnit;
                if (Enum.TryParse(strUnit, out eAlarmUnit) == false)
                    Unit = "Other";
                else
                    Unit = eAlarmUnit.ToString();
            }
            catch { }
        }

        public void ResetLPWarning(int PortNo)
        {
            exeWarningReset.Set(PortNo);
        }


    }



}
