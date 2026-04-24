using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RorzeApi.Class;
using RorzeUnit;
using RorzeComm.Log;
using RorzeUnit.Class;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using RorzeApi.GUI;
using System.Drawing.Drawing2D;
using RorzeUnit.Interface;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using RorzeUnit.Class.FFU;
using System.Windows.Media.Media3D;

namespace RorzeApi
{
    enum enumGradeAreaSelect { For25slot = 1, For50slot = 2 }
    public partial class frmParameter : Form
    {
        float frmX;
        float frmY;
        bool isLoaded = false;

        PropertyParameterCommunication ParameterGrid = new PropertyParameterCommunication();

        private SPermission _permission;
        List<I_Robot> ListTRB;
        List<I_Loadport> ListSTG;

        private string _strSaveGroup;
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        private bool m_bIsRunMode = false;
        public frmParameter(List<I_Robot> listTRB, List<I_Loadport> listSTG, SPermission permission, bool bIsRunMode)
        {
            try
            {
                InitializeComponent();

                ListTRB = listTRB;
                ListSTG = listSTG;

                _permission = permission;
                m_bIsRunMode = bIsRunMode;
                if (GParam.theInst.FreeStyle)
                {
                    btnSave.Image = Properties.Resources._32_save_;
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        #region Form Zoom
        public void SetGUISize(float frmWidth, float frmHeight)
        {
            if (isLoaded == false)
            {
                frmX = this.Width;  //獲取窗體的寬度
                frmY = this.Height; //獲取窗體的高度      
                isLoaded = true;    // 已設定各控制項的尺寸到Tag屬性中
                SetTag(this);       //調用方法
            }
            float tempX = frmWidth / frmX;  //計算比例
            float tempY = frmHeight / frmY; //計算比例
            SetControls(tempX, tempY, this);
        }
        private void SetTag(Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                con.Tag = con.Width + ":" + con.Height + ":" + con.Left + ":" + con.Top + ":" + con.Font.Size;
                if (con.Controls.Count > 0)
                    SetTag(con);
            }
        }
        private void SetControls(float newx, float newy, Control cons)
        {
            //遍歷窗體中的控制項，重新設置控制項的值
            foreach (Control con in cons.Controls)
            {
                string[] mytag = con.Tag.ToString().Split(new char[] { ':' });//獲取控制項的Tag屬性值，並分割後存儲字元串數組
                float a = System.Convert.ToSingle(mytag[0]) * newx;//根據窗體縮放比例確定控制項的值，寬度
                con.Width = (int)a;//寬度
                a = System.Convert.ToSingle(mytag[1]) * newy;//高度
                con.Height = (int)(a);
                a = System.Convert.ToSingle(mytag[2]) * newx;//左邊距離
                con.Left = (int)(a);
                a = System.Convert.ToSingle(mytag[3]) * newy;//上邊緣距離
                con.Top = (int)(a);
                Single currentSize = System.Convert.ToSingle(mytag[4]) * newy;//字體大小
                con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                if (con.Controls.Count > 0)
                {
                    SetControls(newx, newy, con);
                }
            }
        }
        #endregion

        //========= 
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                bool bTRB1_SpeedOK = true, bTRB2_SpeedOK = true;
                if (ParameterGrid.TRB1RunSpeed % 5 != 0 || ParameterGrid.TRB1RunSpeed < 1 || ParameterGrid.TRB1RunSpeed > 100)
                {
                    new frmMessageBox("RobotA run speed abnormal.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    bTRB1_SpeedOK = false;
                }
                if (ParameterGrid.TRB1MaintSpeed % 5 != 0 || ParameterGrid.TRB1MaintSpeed < 1 || ParameterGrid.TRB1MaintSpeed > 30)
                {
                    new frmMessageBox("RobotA maint speed abnormal.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    bTRB1_SpeedOK = false;
                }
                /*if (ParameterGrid.TRB2RunSpeed % 5 != 0 || ParameterGrid.TRB2RunSpeed < 1 || ParameterGrid.TRB2RunSpeed > 100)
                {
                    new frmMessageBox("RobotB run speed abnormal.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    bTRB2_SpeedOK = false;
                }
                if (ParameterGrid.TRB2MaintSpeed % 5 != 0 || ParameterGrid.TRB2MaintSpeed < 1 || ParameterGrid.TRB2MaintSpeed > 30)
                {
                    new frmMessageBox("RobotB maint speed abnormal.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    bTRB2_SpeedOK = false;
                }

                if (ParameterGrid.EFEM_FFU_Speed < 300 || ParameterGrid.EFEM_FFU_Speed > 1300)
                {
                    new frmMessageBox("FFU speed abnormal.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                if (ParameterGrid.Stocker_FFU_Speed < 300 || ParameterGrid.Stocker_FFU_Speed > 1300)
                {
                    new frmMessageBox("FFU speed abnormal.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }*/


                frmMessageBox frmMbox = new frmMessageBox("Data has change  do you want to save data?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (frmMbox.ShowDialog() == DialogResult.Yes)
                {
                    _strSaveGroup = string.Empty;

                    GParam.theInst.SetTpTime(enumTpTime.TP1, ParameterGrid.T1Timeout);
                    GParam.theInst.SetTpTime(enumTpTime.TP2, ParameterGrid.T2Timeout);
                    GParam.theInst.SetTpTime(enumTpTime.TP3, ParameterGrid.T3Timeout);
                    GParam.theInst.SetTpTime(enumTpTime.TP4, ParameterGrid.T4Timeout);
                    GParam.theInst.SetTpTime(enumTpTime.TP5, ParameterGrid.T5Timeout);

                    if (bTRB1_SpeedOK)
                    {
                        GParam.theInst.SetRobot_MaintSpeed(0, ParameterGrid.TRB1MaintSpeed / 5);//0~100/5
                        GParam.theInst.SetRobot_RunSpeed(0, ParameterGrid.TRB1RunSpeed / 5);//0~100/5
                    }
                    /*if (bTRB2_SpeedOK)
                    {
                        GParam.theInst.SetRobot_MaintSpeed(1, ParameterGrid.TRB2MaintSpeed / 5);//0~100/5
                        GParam.theInst.SetRobot_RunSpeed(1, ParameterGrid.TRB2RunSpeed / 5);//0~100/5
                    }*/
                    //GParam.theInst.SetRFID_Bit(ParameterGrid.RFID_Bit);

                    GParam.theInst.SetPressure_Enable(ParameterGrid.EFEM_Pressure_Check);
                    GParam.theInst.SetPressure_Threshold(ParameterGrid.EFEM_Pressure_Threshold);

                    /*GParam.theInst.SetDIO5Pressure_Enable(ParameterGrid.Stocker_Pressure_Check);
                    GParam.theInst.SetDIO5Pressure_Threshold(ParameterGrid.Stocker_Pressure_Threshold);

                    GParam.theInst.SetFFUspeedEFEM(ParameterGrid.EFEM_FFU_Speed);
                    GParam.theInst.SetFFUspeedStocker(ParameterGrid.Stocker_FFU_Speed);
                    //EFEM FFU有兩站
                    for (int i = 0; i < ListFFU[0].AdressCount; i++)
                    {
                        ListFFU[0].SetSpeedSetting(i + 1, ParameterGrid.EFEM_FFU_Speed);

                        if (ListFFU[0].GetSpeedMax()[i] != 1300)
                        {
                            ListFFU[0].SetSpeedLimitMax(i + 1, 1300);//RS485
                            ListFFU[0].GetSpeedLimitMax(i + 1);//RS485
                        }

                        if (ListFFU[0].GetSpeedMin()[i] != 300)
                        {
                            ListFFU[0].SetSpeedLimitMin(i + 1, 300);//RS485
                            ListFFU[0].GetSpeedLimitMin(i + 1);//RS485
                        }
                    }
                    for (int i = 0; i < ListFFU[1].AdressCount; i++)
                    {
                        ListFFU[1].SetSpeedSetting(i + 1, ParameterGrid.Stocker_FFU_Speed);

                        if (ListFFU[1].GetSpeedMax()[i] != 1300)
                        {
                            ListFFU[1].SetSpeedLimitMax(i + 1, 1300);//RS485
                            ListFFU[1].GetSpeedLimitMax(i + 1);//RS485
                        }

                        if (ListFFU[1].GetSpeedMin()[i] != 300)
                        {
                            ListFFU[1].SetSpeedLimitMin(i + 1, 300);//RS485
                            ListFFU[1].GetSpeedLimitMin(i + 1);//RS485
                        }
                    }*/





                    GParam.theInst.SetIdleLogOutTime(ParameterGrid.IdleLogOutTime);
                    //GParam.theInst.SetEquipmentShowName(ParameterGrid.EquipmentShowName);
                    //GParam.theInst.SetDBAlarmlistUpdate = ParameterGrid.DBAlarmlistUpdate;

                    /*GParam.theInst.SetStkPassPurge(0, ParameterGrid.Stk1PassPurge);
                    GParam.theInst.SetStkPassPurge(1, ParameterGrid.Stk2PassPurge);*/

                    GParam.theInst.SetFoupArrivalIdleTimeout(ParameterGrid.FoupArrivalIdleTimeout);
                    GParam.theInst.SetFoupWaitTransferTimeout(ParameterGrid.FoupWaitTransferTimeout);
                    //GParam.theInst.SetAreaSelect((int)ParameterGrid.GradeAreaSelect);
                    GParam.theInst.SetOCRReadFailProcess(ParameterGrid.OCRReadFailProcess);
                    //GParam.theInst.SetSoftwareStartupTowerMapping(ParameterGrid.SoftwareStartupTowerMapping);
                    GParam.theInst.SetWaferIDFilterBit(ParameterGrid.WaferIDFilterBit);
                    /*GParam.theInst.SetOCR_ReadSucGetImage(ParameterGrid.OCRReadSaveImage);
                    GParam.theInst.SetTransferStockerWaferAngle(ParameterGrid.TransferStockerWaferAngle);
                    GParam.theInst.SetOCRWarningsAutoRestTime(ParameterGrid.OCRWarningsAutoRestTime);*/
                    GParam.theInst.SetSystemLanguage = ParameterGrid._SystemLanguage == 1 ? enumSystemLanguage.zh_CN : enumSystemLanguage.Default;
                    GParam.theInst.SetMotionEventManagerUrl(ParameterGrid.MotionEventManagerUrl);
                    //GParam.theInst.SetRobotAlignment_Enable(ParameterGrid.RobotAlignment_Enable);



                    //GParam.theInst.SetToEqNotchAngle(ParameterGrid.ToEqNotchAngle);

                    //GParam.theInst.SetWaferDiameter(ParameterGrid.GetWaferDiameter);
                    //GParam.theInst.SetWaferDiameterWaitTime(ParameterGrid.WaferDiameterWaitTime);
                    //GParam.theInst.SetWaferChipping(ParameterGrid.GetWaferChipping);

                    /*if (ParameterGrid.WaferChippingSpeed % 5 != 0 || ParameterGrid.WaferChippingSpeed < 1 || ParameterGrid.WaferChippingSpeed > 100)
                    {
                        new frmMessageBox("Get wafer chipping speed abnormal.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }
                    else
                    {
                        GParam.theInst.SetWaferChippingSpeed(ParameterGrid.WaferChippingSpeed / 5);
                    }
                    GParam.theInst.SetWaferChippingAngle(ParameterGrid.WaferChippingAngle);
                    GParam.theInst.SetWaferChippingWaitTime(ParameterGrid.WaferChippingWaitTime);*/
                    //GParam.theInst.SetWaferSurfaceChar(ParameterGrid.GetWaferSurfaceChar);
                    //GParam.theInst.SetWaferThicknessChar(ParameterGrid.GetWaferThicknessChar);
                    //GParam.theInst.SetTurnOnVacChar(ParameterGrid.GetTurnOnVacChar);
                    //GParam.theInst.SetEQRecipeListPath(ParameterGrid.GetEQRecipeListPath);

                    if (m_bIsRunMode == true)
                    {
                        foreach (I_Robot trb in ListTRB)
                        {
                            if (trb.Disable) continue;
                            if (trb.BodyNo == 1 && bTRB1_SpeedOK == false) continue;
                            if (trb.BodyNo == 2 && bTRB2_SpeedOK == false) continue;
                            trb.SSPD(GParam.theInst.GetRobot_RunSpeed(trb.BodyNo - 1));
                        }
                    }
                    else
                    {
                        foreach (I_Robot trb in ListTRB)
                        {
                            if (trb.Disable) continue;
                            if (trb.BodyNo == 1 && bTRB1_SpeedOK == false) continue;
                            if (trb.BodyNo == 2 && bTRB2_SpeedOK == false) continue;
                            trb.SSPD(GParam.theInst.GetRobot_MaintSpeed(trb.BodyNo - 1));
                        }
                    }

                    foreach (I_Loadport stg in ListSTG)
                    {
                        if (stg.Disable || stg.E84Object == null) continue;
                        stg.E84Object.SetTpTime = GParam.theInst.GetTpTime();
                        stg.FoupArrivalIdleTimeout = GParam.theInst.FoupArrivalIdleTimeout;//會依照設定變動
                        stg.FoupWaitTransferTimeout = GParam.theInst.FoupWaitTransferTimeout;//會依照設定變動
                    }

                    //GParam.theInst.SetOCRReadFailProcess(ParameterGrid.OCRReadFailProcess);

                    //_dbLog.SaveEventLog(string.Format("GUI Setting Save data."), _permission.UserID);
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        //========= 
        private void frmParameter_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible)
                {

                    ParameterGrid.T1Timeout = GParam.theInst.GetTpTime(enumTpTime.TP1);
                    ParameterGrid.T2Timeout = GParam.theInst.GetTpTime(enumTpTime.TP2);
                    ParameterGrid.T3Timeout = GParam.theInst.GetTpTime(enumTpTime.TP3);
                    ParameterGrid.T4Timeout = GParam.theInst.GetTpTime(enumTpTime.TP4);
                    ParameterGrid.T5Timeout = GParam.theInst.GetTpTime(enumTpTime.TP5);

                    ParameterGrid.TRB1MaintSpeed = GParam.theInst.GetRobot_MaintSpeed(0) * 5;
                    ParameterGrid.TRB1RunSpeed = GParam.theInst.GetRobot_RunSpeed(0) * 5;
                    /*ParameterGrid.TRB2MaintSpeed = GParam.theInst.GetRobot_MaintSpeed(1) * 5;
                    ParameterGrid.TRB2RunSpeed = GParam.theInst.GetRobot_RunSpeed(1) * 5;

                    ParameterGrid.Stk1PassPurge = GParam.theInst.GetStkPassPurge(0);
                    ParameterGrid.Stk2PassPurge = GParam.theInst.GetStkPassPurge(1);*/

                    //ParameterGrid.RFID_Bit = GParam.theInst.GetRFID_Bit;
                    ParameterGrid.EFEM_Pressure_Check = GParam.theInst.RC550Pressure_Enable;
                    ParameterGrid.EFEM_Pressure_Threshold = GParam.theInst.RC550Pressure_Threshold;
                    /*ParameterGrid.Stocker_Pressure_Check = GParam.theInst.RC550_5_Pressure_Enable;
                    ParameterGrid.Stocker_Pressure_Threshold = GParam.theInst.RC550_5_Pressure_Threshold;
                    ParameterGrid.EFEM_FFU_Speed = GParam.theInst.GetFFUspeedEFEM;
                    ParameterGrid.Stocker_FFU_Speed = GParam.theInst.GetFFUspeedStocker;*/
                    ParameterGrid.IdleLogOutTime = GParam.theInst.IdleLogOutTime;
                    //ParameterGrid.EquipmentShowName = GParam.theInst.EquipmentShowName;
                    //ParameterGrid.DBAlarmlistUpdate = GParam.theInst.GetDBAlarmlistUpdate;
                    ParameterGrid._SystemLanguage = GParam.theInst.SystemLanguage == enumSystemLanguage.zh_CN ? 1 : 0;
                    ParameterGrid.MotionEventManagerUrl = GParam.theInst.GetMotionEventManagerUrl();
                    //ParameterGrid.RobotAlignment_Enable = GParam.theInst.GetRobotAlignment_Enable();


                    ParameterGrid.FoupArrivalIdleTimeout = GParam.theInst.FoupArrivalIdleTimeout;
                    ParameterGrid.FoupWaitTransferTimeout = GParam.theInst.FoupWaitTransferTimeout;
                    //ParameterGrid.GradeAreaSelect = (enumGradeAreaSelect)GParam.theInst.AreaSelect;
                    ParameterGrid.OCRReadFailProcess = GParam.theInst.GetOCRReadFailProcess;
                    //ParameterGrid.SoftwareStartupTowerMapping = GParam.theInst.SoftwareStartupTowerMapping;
                    ParameterGrid.WaferIDFilterBit = GParam.theInst.WaferIDFilterBit;
                    //ParameterGrid.OCRReadSaveImage = GParam.theInst.GetOCR_ReadSucGetImage;
                    //ParameterGrid.TransferStockerWaferAngle = GParam.theInst.TransferStockerWaferAngle;
                    //ParameterGrid.OCRWarningsAutoRestTime = GParam.theInst.OCRWarningsAutoRestTime;

                    //ParameterGrid.ToEqNotchAngle = GParam.theInst.GetToEqNotchAngle;

                    //ParameterGrid.GetWaferDiameter = GParam.theInst.GetWaferDiameter;
                    //ParameterGrid.WaferDiameterWaitTime = GParam.theInst.WaferDiameterWaitTime;
                    //ParameterGrid.GetWaferChipping = GParam.theInst.GetWaferChipping;
                    /*ParameterGrid.WaferChippingAngle = GParam.theInst.WaferChippingAngle;
                    ParameterGrid.WaferChippingSpeed = GParam.theInst.WaferChippingSpeed * 5;
                    ParameterGrid.WaferChippingWaitTime = GParam.theInst.WaferChippingWaitTime;*/

                    //ParameterGrid.GetWaferSurfaceChar = GParam.theInst.GetWaferSurfaceChar;
                    //ParameterGrid.GetWaferThicknessChar = GParam.theInst.GetWaferThicknessChar;
                    //ParameterGrid.GetTurnOnVacChar = GParam.theInst.GetTurnOnVacChar;
                    //ParameterGrid.GetEQRecipeListPath = GParam.theInst.GetEQRecipeListPath;

                    propertyGridHost.SelectedObject = ParameterGrid;


                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        //========= 
    }

    internal class PropertyParameterCommunication
    {
        //bool m_bEQdisable = GParam.theInst.IsEquipmentDisable;


        public PropertyParameterCommunication()
        {
        }


        #region E84 TP TimeOut

        private int t1Timeout;
        [Category("E84 TP TimeOut")]
        [Browsable(true)]
        [OrderedDisplayName("TP1 Timeout (s)", 1)]
        [Description("The TP1 timeout is the transaction timer. This is the maximum amount of time between a primary message and the expected response before declaring the transaction closed. If the timer expires, an S9F9 error message is sent. The valid value range is 1 - 120 seconds. The typical value is 45 seconds.")]
        public int T1Timeout
        {
            get { return t1Timeout; }
            set
            {
                if (value < 1 || 120 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 120.");
                }
                t1Timeout = value;
            }
        }

        private int t2Timeout;
        [Category("E84 TP TimeOut")]
        [Browsable(true)]
        [OrderedDisplayName("TP2 Timeout (s)", 2)]
        [Description("The TP2 timeout is the connect separation timeout. This is the amount of time which must elapse between successive attempts to actively establish a connection. The valid value range is 1 - 240 seconds. The tyipical value is 5 seconds.")]
        public int T2Timeout
        {
            get { return t2Timeout; }
            set
            {
                if (value < 1 || 240 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 240.");
                }
                t2Timeout = value;
            }
        }

        private int t3Timeout;
        [Category("E84 TP TimeOut")]
        [Browsable(true)]
        [OrderedDisplayName("TP3 Timeout (s)", 3)]
        [Description("The TP3 timeout is the control transaction timeout. This is the maximum amount of time allowed between an HSMS-level control message and its response. If the timer expires, communications failure is declared. The valid value range is 1 - 240 seconds. The typical value is 5 seconds.")]
        public int T3Timeout
        {
            get { return t3Timeout; }
            set
            {
                if (value < 1 || 240 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 240.");
                }
                t3Timeout = value;
            }
        }

        private int t4Timeout;
        [Category("E84 TP TimeOut")]
        [Browsable(true)]
        [OrderedDisplayName("TP4 Timeout (s)", 4)]
        [Description("The TP4 timeout is the control transaction timeout. This is the maximum amount of time allowed between an HSMS-level control message and its response. If the timer expires, communications failure is declared. The valid value range is 1 - 240 seconds. The typical value is 5 seconds.")]
        public int T4Timeout
        {
            get { return t4Timeout; }
            set
            {
                if (value < 1 || 240 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 240.");
                }
                t4Timeout = value;
            }
        }

        private int t5Timeout;
        [Category("E84 TP TimeOut")]
        [Browsable(true)]
        [OrderedDisplayName("TP5 Timeout (s)", 5)]
        [Description("The TP5 timeout is the control transaction timeout. This is the maximum amount of time allowed between an HSMS-level control message and its response. If the timer expires, communications failure is declared. The valid value range is 1 - 240 seconds. The typical value is 5 seconds.")]
        public int T5Timeout
        {
            get { return t5Timeout; }
            set
            {
                if (value < 1 || 240 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 240.");
                }
                t5Timeout = value;
            }
        }

        #endregion

        #region Equipment Server Client        
        /*
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("TCP Type", 1)]
        [Description("")]
        [TypeConverter(typeof(TCPTypeEnumConvertor))]
        public enumTCPType EQ_TCPType { get; set; }
        #endregion
        #region Equipment IP       
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("IP", 2)]
        [Description("Equipment IP")]
        public string EQ_IP { get; set; }
        #endregion
        #region Equipment Port       
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Port", 3)]
        [Description("Equipment Port")]
        public int EQ_Port { get; set; }
        #endregion
        #region Equipment process timeout
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Process Timeout", 4)]
        [Description("Equipment Process Timeout")]
        public int EQ_ProcessTimeout { get; set; }
        #endregion
        #region Equipment ack timeout
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Ack Timeout", 5)]
        [Description("Equipment Ack Timeout")]
        public int EQ_AckTimeout { get; set; }
        #endregion
        #region Equipment Communication Folder         
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Communication Folder", 6)]
        [Description("Communication Folder")]
        public string EQCommFolder { get; set; }
        #endregion
        #region Equipment Communication Read File Name     
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Communication Read File Name", 7)]
        [Description("Communication Read File Name")]
        public string EQCommReadFileName { get; set; }
        #endregion
        #region Equipment Communication Write File Name      
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Communication Write File Name", 8)]
        [Description("Communication Write File Name")]
        public string EQCommWriteFileName { get; set; }
        #endregion
        #region Equipment Inspection Results Folder     
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Inspection Results Folder", 9)]
        [Description("Inspection Results Folder")]
        public string EQInspectionResultsFolder { get; set; }
        #endregion
        #region Equipment Delete Inspection Results File      
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Delete Inspection Results File", 10)]
        [Description("Delete Inspection Results File")]
        public bool EQDeleteInspectionResultsFile { get; set; }
        #endregion
        #region Equipment Recipe Folder     
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Recipe Folder", 11)]
        [Description("Recipe Folder")]
        public string EQRecipeFolder { get; set; }
        #endregion
        #region Equipment Communication Error File Name      
        [Category("Equipment")]
        [Browsable(true)]
        [OrderedDisplayName("Communication Error File Name", 12)]
        [Description("Communication Error File Name")]
        public string EQCommErrorFileName { get; set; }
        */
        #endregion

        #region Robot    

        [Category("Robot")]
        [Browsable(true)]
        [OrderedDisplayName("RobotA Run Mode Speed", 1)]
        [Description("When KeySwitch turns to RunMode")]
        [EditorAttribute(typeof(DefaultSpeedEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public int TRB1RunSpeed { get; set; }

        [Category("Robot")]
        [Browsable(true)]
        [OrderedDisplayName("RobotA Maint Speed", 2)]
        [Description("When KeySwitch turns to MaintMode,teaching,manual.")]
        [EditorAttribute(typeof(DefaultMaintSpeedEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public int TRB1MaintSpeed { get; set; }

        /*[Category("Robot")]
        [Browsable(true)]
        [OrderedDisplayName("RobotB Run Mode Speed", 3)]
        [Description("When KeySwitch turns to RunMode")]
        [EditorAttribute(typeof(DefaultSpeedEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public int TRB2RunSpeed { get; set; }

        [Category("Robot")]
        [Browsable(true)]
        [OrderedDisplayName("RobotB Maint Speed", 4)]
        [Description("When KeySwitch turns to MaintMode,teaching,manual.")]
        [EditorAttribute(typeof(DefaultMaintSpeedEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public int TRB2MaintSpeed { get; set; }*/

        #endregion

        #region System
        /*[Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("RFID Bit", 1)]
        [Description("RFID only takes the first few bits")]
        public int RFID_Bit { get; set; }*/

        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("EFEM Pressure Check", 1)]
        [Description("Pressure difference check.")]
        public bool EFEM_Pressure_Check { get; set; }
        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("EFEM Pressure Threshold(pa)", 2)]
        [Description("Pressure difference inside the machine.(1pa~3pa)")]
        public int EFEM_Pressure_Threshold { get; set; }

        /*[Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("Stocker Pressure Check", 4)]
        [Description("Pressure difference check.")]
        public bool Stocker_Pressure_Check { get; set; }
        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("Stocker Pressure Threshold(pa)", 5)]
        [Description("Pressure difference inside the machine.(1pa~3pa)")]
        public int Stocker_Pressure_Threshold { get; set; }

        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("EFEM FFU Speed(rpm)", 4)]
        [Description("EFEM FFU Speed.(300~1300rpm)")]
        public int EFEM_FFU_Speed { get; set; }
        
        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("Stocker FFU Speed(rpm)", 7)]
        [Description("Stocker FFU Speed.(300~1300rpm)")]
        public int Stocker_FFU_Speed { get; set; }*/

        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("Idle LogOut Time(ms)", 4)]
        [Description("Idle LogOut Time(ms)")]
        public int IdleLogOutTime { get; set; }


        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("SYSTEM Language", 5)]
        [Description("SYSTEM Language 0:English 1:中文")]
        public int _SystemLanguage { get; set; }

        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("MotionEventManager URL", 6)]
        [Description("MotionEventManager Base URL (e.g., http://localhost:61723)")]
        public string MotionEventManagerUrl { get; set; }

        //[Category("System")]
        //[Browsable(true)]
        //[OrderedDisplayName("Robot Alignment Enable", 7)]
        //[Description("Enable Robot Alignment function")]
        //public bool RobotAlignment_Enable { get; set; }

        /*[Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("Device Display Name", 5)]
        [Description("Device Display Name")]
        public string EquipmentShowName { get; set; }

        [Category("System")]
        [Browsable(true)]
        [OrderedDisplayName("DB Alarm List Update", 6)]
        [Description("Used to have new exception information, open this parameter when the next time to restart the software will automatically update the exception form, after the completion of the update will be closed parameters")]
        public bool DBAlarmlistUpdate { get; set; }*/

        #endregion

        #region CustomFunction
        [Category("Custom Function")]
        [Browsable(true)]
        [OrderedDisplayName("Foup Arrival Idle Timeout(s)", 1)]
        [Description("Carrier arrives at the device without docking timeout alarm (in seconds), if not enable it set -1")]
        public int FoupArrivalIdleTimeout { get; set; }

        [Category("Custom Function")]
        [Browsable(true)]
        [OrderedDisplayName("Foup Wait Transfer Timeout(s)", 2)]
        [Description("Carrier open without transfer timeout alarm (in seconds), if not enable it set -1")]
        public int FoupWaitTransferTimeout { get; set; }

        [Category("Custom Function")]
        [Browsable(true)]
        [OrderedDisplayName("OCR Read Fail Process", 3)]
        [Description("OCR Read Fail Process")]
        [TypeConverter(typeof(OCRReadFailProcessEnumConvertor))]
        public enumOCRReadFailProcess OCRReadFailProcess { get; set; }

        [Category("Custom Function")]
        [Browsable(true)]
        [OrderedDisplayName("WaferID Filter Bit", 4)]
        //[Description("Wafer reading filter bit")]
        [Description("WaferID Filter Bit")]
        public int WaferIDFilterBit { get; set; }

        #endregion

        public bool IsModified(PropertyParameterCommunication other)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this);
            foreach (PropertyDescriptor property in properties)
            {
                if (!property.IsBrowsable) continue;
                string s1 = property.GetValue(this).ToString();
                string s2 = property.GetValue(other).ToString();
                if (s1 != s2)
                {
                    return true;
                }
            }
            return false;
        }

        public class OrderedDisplayNameAttribute : DisplayNameAttribute
        {
            public OrderedDisplayNameAttribute(string displayName, int position)
            {
                base.DisplayNameValue = string.Format("{0,2:D}. {1}", position, displayName);
            }
        }


        //  Property Grid 轉換
        class TCPTypeEnumConvertor : EnumConverter
        {
            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                switch (value.ToString())
                {
                    case "None":
                        return enumTCPType.None;
                    case "Client":
                        return enumTCPType.Client;
                    case "Server":
                        return enumTCPType.Server;
                    default:
                        return null;
                }
            }

            public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                switch ((enumTCPType)value)
                {
                    case enumTCPType.None:
                        return "None";
                    case enumTCPType.Client:
                        return "Client";
                    case enumTCPType.Server:
                        return "Server";
                    default:
                        return null;
                }
            }

            public TCPTypeEnumConvertor(Type type) : base(type) { }
        }

        class GradeAreaSelectEnumConvertor : EnumConverter
        {
            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                switch (value.ToString())
                {
                    case "For 25 slot":
                        return enumGradeAreaSelect.For25slot;
                    case "For 50 slot":
                        return enumGradeAreaSelect.For50slot;
                    default:
                        return null;
                }
            }

            public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                switch ((enumGradeAreaSelect)value)
                {
                    case enumGradeAreaSelect.For25slot:
                        return "For 25 slot";
                    case enumGradeAreaSelect.For50slot:
                        return "For 50 slot";
                    default:
                        return null;
                }
            }

            public GradeAreaSelectEnumConvertor(Type type) : base(type) { }
        }

        class OCRReadFailProcessEnumConvertor : EnumConverter
        {
            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                switch (value.ToString())
                {
                    case "Continue":
                        return enumOCRReadFailProcess.Continue;
                    case "Abort":
                        return enumOCRReadFailProcess.Abort;
                    case "BackFoup":
                        return enumOCRReadFailProcess.BackFoup;
                    case "UserKeyIn":
                        return enumOCRReadFailProcess.UserKeyIn;
                    default:
                        return null;
                }
            }

            public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                switch ((enumOCRReadFailProcess)value)
                {
                    case enumOCRReadFailProcess.Continue:
                        return "Continue";
                    case enumOCRReadFailProcess.Abort:
                        return "Abort";
                    case enumOCRReadFailProcess.BackFoup:
                        return "BackFoup";
                    case enumOCRReadFailProcess.UserKeyIn:
                        return "UserKeyIn";
                    default:
                        return null;
                }
            }

            public OCRReadFailProcessEnumConvertor(Type type) : base(type)
            {
            }
        }

        class AngleEditor : System.Drawing.Design.UITypeEditor
        {
            public AngleEditor()
            {
            }

            // Indicates whether the UITypeEditor provides a form-based (modal) dialog,
            // drop down dialog, or no UI outside of the properties window.
            public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.DropDown;
            }

            // Displays the UI for value selection.
            public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value)
            {
                // Return the value if the value is not of type Int32, Double and Single.
                if (value.GetType() != typeof(double) && value.GetType() != typeof(float) && value.GetType() != typeof(int))
                    return value;

                // Uses the IWindowsFormsEditorService to display a
                // drop-down UI in the Properties window.
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null)
                {
                    // Display an angle selection control and retrieve the value.


                    GUINotchAngle angleControl = new GUINotchAngle();
                    angleControl._Angle = (double)value;

                    edSvc.DropDownControl(angleControl);

                    // Return the value in the appropraite data format.
                    if (value.GetType() == typeof(double))
                        return angleControl._Angle;
                    else if (value.GetType() == typeof(float))
                        return (float)angleControl._Angle;
                    else if (value.GetType() == typeof(int))
                        return (int)angleControl._Angle;
                }
                return value;
            }

            // Draws a representation of the property's value.
            public override void PaintValue(System.Drawing.Design.PaintValueEventArgs e)
            {
                int normalX = (e.Bounds.Width / 2);
                int normalY = (e.Bounds.Height / 2);

                double radius = Math.Sqrt(Math.Pow(normalX, 2) + Math.Pow(normalY, 2));

                // Fill background and ellipse and center point.
                e.Graphics.FillRectangle(new SolidBrush(Color.DarkBlue), e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
                e.Graphics.FillEllipse(new SolidBrush(Color.White), e.Bounds.X + 1, e.Bounds.Y + 1, e.Bounds.Width - 3, e.Bounds.Height - 3);
                e.Graphics.FillEllipse(new SolidBrush(Color.SlateGray), normalX + e.Bounds.X - 1, normalY + e.Bounds.Y - 1, 3, 3);

                // Draw line along the current angle.
                double angle = (((double)e.Value - 180) * Math.PI) / (double)180;
                e.Graphics.DrawLine(new Pen(new SolidBrush(Color.Red), 1),
                    e.Bounds.X + normalX,
                    e.Bounds.Y + normalY,
                    e.Bounds.X + (normalX + (int)((double)normalX * Math.Sin(angle))),
                    e.Bounds.Y + (normalY + (int)((double)normalY * Math.Cos(angle))));
            }

            // Indicates whether the UITypeEditor supports painting a
            // representation of a property's value.
            public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
            {
                return true;
            }
        }

        // Provides a user interface for adjusting an angle value.
        //internal class AngleControl : System.Windows.Forms.UserControl
        //{
        //    // Stores the angle.
        //    public double angle;
        //    // Stores the rotation offset.
        //    private int rotation = 0;
        //    // Control state tracking variables.
        //    private int dbx = -10;
        //    private int dby = -10;
        //    private int overButton = -1;

        //    public AngleControl(double initial_angle)
        //    {
        //        this.angle = initial_angle;
        //        this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        //    }

        //    protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        //    {
        //        // Set angle origin point at center of control.
        //        int originX = (this.Width / 2);
        //        int originY = (this.Height / 2);

        //        // Fill background and ellipse and center point.
        //        e.Graphics.FillRectangle(new SolidBrush(Color.DarkBlue), 0, 0, this.Width, this.Height);
        //        e.Graphics.FillEllipse(new SolidBrush(Color.White), 1, 1, this.Width - 3, this.Height - 3);
        //        e.Graphics.FillEllipse(new SolidBrush(Color.SlateGray), originX - 1, originY - 1, 3, 3);

        //        // Draw angle markers.
        //        int startangle = (270 - rotation) % 360;
        //        e.Graphics.DrawString(startangle.ToString(), new Font("Arial", 8), new SolidBrush(Color.DarkGray), (this.Width / 2) - 10, 10);
        //        startangle = (startangle + 90) % 360;
        //        e.Graphics.DrawString(startangle.ToString(), new Font("Arial", 8), new SolidBrush(Color.DarkGray), this.Width - 18, (this.Height / 2) - 6);
        //        startangle = (startangle + 90) % 360;
        //        e.Graphics.DrawString(startangle.ToString(), new Font("Arial", 8), new SolidBrush(Color.DarkGray), (this.Width / 2) - 6, this.Height - 18);
        //        startangle = (startangle + 90) % 360;
        //        e.Graphics.DrawString(startangle.ToString(), new Font("Arial", 8), new SolidBrush(Color.DarkGray), 10, (this.Height / 2) - 6);

        //        // Draw line along the current angle.
        //        double radians = Math.Round(((((angle + rotation) + 360) % 360) * Math.PI) / (double)180, 2, MidpointRounding.AwayFromZero);
        //        e.Graphics.DrawLine(new Pen(new SolidBrush(Color.Red), 1), originX, originY,
        //            originX + (int)((double)originX * (double)Math.Cos(radians)),
        //            originY + (int)((double)originY * (double)Math.Sin(radians)));

        //        // Output angle information.
        //        e.Graphics.FillRectangle(new SolidBrush(Color.Gray), this.Width - 84, 3, 82, 13);
        //        e.Graphics.DrawString("Angle: " + angle.ToString("F4"), new Font("Arial", 8), new SolidBrush(Color.Yellow), this.Width - 84, 2);
        //        // Draw square at mouse position of last angle adjustment.
        //        e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.Black), 1), dbx - 2, dby - 2, 4, 4);
        //        // Draw rotation adjustment buttons.
        //        if (overButton == 1)
        //        {
        //            e.Graphics.FillRectangle(new SolidBrush(Color.Green), this.Width - 28, this.Height - 14, 12, 12);
        //            e.Graphics.FillRectangle(new SolidBrush(Color.Gray), 2, this.Height - 13, 110, 12);
        //            e.Graphics.DrawString("Rotate 90 degrees left", new Font("Arial", 8), new SolidBrush(Color.White), 2, this.Height - 14);
        //        }
        //        else
        //        {
        //            e.Graphics.FillRectangle(new SolidBrush(Color.DarkGreen), this.Width - 28, this.Height - 14, 12, 12);
        //        }

        //        if (overButton == 2)
        //        {
        //            e.Graphics.FillRectangle(new SolidBrush(Color.Green), this.Width - 14, this.Height - 14, 12, 12);
        //            e.Graphics.FillRectangle(new SolidBrush(Color.Gray), 2, this.Height - 13, 116, 12);
        //            e.Graphics.DrawString("Rotate 90 degrees right", new Font("Arial", 8), new SolidBrush(Color.White), 2, this.Height - 14);
        //        }
        //        else
        //        {
        //            e.Graphics.FillRectangle(new SolidBrush(Color.DarkGreen), this.Width - 14, this.Height - 14, 12, 12);
        //        }

        //        e.Graphics.DrawEllipse(new Pen(new SolidBrush(Color.White), 1), this.Width - 11, this.Height - 11, 6, 6);
        //        e.Graphics.DrawEllipse(new Pen(new SolidBrush(Color.White), 1), this.Width - 25, this.Height - 11, 6, 6);
        //        if (overButton == 1)
        //            e.Graphics.FillRectangle(new SolidBrush(Color.Green), this.Width - 25, this.Height - 6, 4, 4);
        //        else
        //            e.Graphics.FillRectangle(new SolidBrush(Color.DarkGreen), this.Width - 25, this.Height - 6, 4, 4);
        //        if (overButton == 2)
        //            e.Graphics.FillRectangle(new SolidBrush(Color.Green), this.Width - 8, this.Height - 6, 4, 4);
        //        else
        //            e.Graphics.FillRectangle(new SolidBrush(Color.DarkGreen), this.Width - 8, this.Height - 6, 4, 4);
        //        e.Graphics.FillPolygon(new SolidBrush(Color.White), new Point[] { new Point(this.Width - 7, this.Height - 8), new Point(this.Width - 3, this.Height - 8), new Point(this.Width - 5, this.Height - 4) });
        //        e.Graphics.FillPolygon(new SolidBrush(Color.White), new Point[] { new Point(this.Width - 26, this.Height - 8), new Point(this.Width - 21, this.Height - 8), new Point(this.Width - 25, this.Height - 4) });
        //    }

        //    protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        //    {
        //        // Handle rotation adjustment button clicks.
        //        if (e.X >= this.Width - 28 && e.X <= this.Width - 2 && e.Y >= this.Height - 14 && e.Y <= this.Height - 2)
        //        {
        //            if (e.X <= this.Width - 16)
        //                rotation -= 90;
        //            else if (e.X >= this.Width - 14)
        //                rotation += 90;
        //            if (rotation < 0)
        //                rotation += 360;
        //            rotation = rotation % 360;
        //            dbx = -10;
        //            dby = -10;
        //        }
        //        else
        //        {
        //            UpdateAngle(e.X, e.Y);
        //        }

        //        this.Refresh();
        //    }

        //    protected override void OnMouseMove(System.Windows.Forms.MouseEventArgs e)
        //    {
        //        if (e.Button == MouseButtons.Left)
        //        {
        //            UpdateAngle(e.X, e.Y);
        //            overButton = -1;
        //        }
        //        else if (e.X >= this.Width - 28 && e.X <= this.Width - 16 && e.Y >= this.Height - 14 && e.Y <= this.Height - 2)
        //        {
        //            overButton = 1;
        //        }
        //        else if (e.X >= this.Width - 14 && e.X <= this.Width - 2 && e.Y >= this.Height - 14 && e.Y <= this.Height - 2)
        //        {
        //            overButton = 2;
        //        }
        //        else
        //        {
        //            overButton = -1;
        //        }

        //        this.Refresh();
        //    }

        //    private void UpdateAngle(int mx, int my)
        //    {
        //        // Store mouse coordinates.
        //        dbx = mx;
        //        dby = my;

        //        // Translate y coordinate input to GetAngle function to correct for ellipsoid distortion.
        //        double widthToHeightRatio = (double)this.Width / (double)this.Height;
        //        int tmy;
        //        if (my == 0)
        //            tmy = my;
        //        else if (my < this.Height / 2)
        //            tmy = (this.Height / 2) - (int)(((this.Height / 2) - my) * widthToHeightRatio);
        //        else
        //            tmy = (this.Height / 2) + (int)((double)(my - (this.Height / 2)) * widthToHeightRatio);

        //        // Retrieve updated angle based on rise over run.
        //        angle = (GetAngle(this.Width / 2, this.Height / 2, mx, tmy) - rotation) % 360;
        //    }

        //    private double GetAngle(int x1, int y1, int x2, int y2)
        //    {
        //        double degrees;

        //        // Avoid divide by zero run values.
        //        if (x2 - x1 == 0)
        //        {
        //            if (y2 > y1)
        //                degrees = 90;
        //            else
        //                degrees = 270;
        //        }
        //        else
        //        {
        //            // Calculate angle from offset.
        //            double riseoverrun = (double)(y2 - y1) / (double)(x2 - x1);
        //            double radians = Math.Atan(riseoverrun);
        //            degrees = radians * ((double)180 / Math.PI);

        //            // Handle quadrant specific transformations.
        //            if ((x2 - x1) < 0 || (y2 - y1) < 0)
        //                degrees += 180;
        //            if ((x2 - x1) > 0 && (y2 - y1) < 0)
        //                degrees -= 180;
        //            if (degrees < 0)
        //                degrees += 360;
        //        }
        //        return degrees;
        //    }
        //}

        class DefaultSpeedEditor : System.Drawing.Design.UITypeEditor
        {
            public DefaultSpeedEditor()
            {
            }

            // Indicates whether the UITypeEditor provides a form-based (modal) dialog,
            // drop down dialog, or no UI outside of the properties window.
            public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.DropDown;
            }

            // Displays the UI for value selection.
            public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value)
            {
                // Return the value if the value is not of type Int32, Double and Single.
                if (value.GetType() != typeof(double) && value.GetType() != typeof(float) && value.GetType() != typeof(int))
                    return value;

                // Uses the IWindowsFormsEditorService to display a
                // drop-down UI in the Properties window.
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null)
                {
                    // Display an angle selection control and retrieve the value.


                    GUISpeedSelect speedControl = new GUISpeedSelect((int)value);

                    edSvc.DropDownControl(speedControl);

                    // Return the value in the appropraite data format.
                    if (value.GetType() == typeof(double))
                        return speedControl.m_nSpeed5to100;
                    else if (value.GetType() == typeof(float))
                        return (float)speedControl.m_nSpeed5to100;
                    else if (value.GetType() == typeof(int))
                        return (int)speedControl.m_nSpeed5to100;
                }
                return value;
            }

            // Draws a representation of the property's value.
            public override void PaintValue(System.Drawing.Design.PaintValueEventArgs e)
            {
                int normalX = (e.Bounds.Width / 2);
                int normalY = (e.Bounds.Height / 2);

                double radius = Math.Sqrt(Math.Pow(normalX, 2) + Math.Pow(normalY, 2));

                //Fill background and ellipse and center point.
                e.Graphics.FillRectangle(new SolidBrush(Color.DarkBlue), e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
                //e.Graphics.FillEllipse(new SolidBrush(Color.White), e.Bounds.X + 1, e.Bounds.Y + 1, e.Bounds.Width - 3, e.Bounds.Height - 3);
                e.Graphics.FillEllipse(new SolidBrush(Color.SlateGray), normalX + e.Bounds.X - 1, normalY + e.Bounds.Y - 1, 3, 3);

                #region 畫扇形
                // Create rectangle for ellipse.
                Rectangle rect = new Rectangle(e.Bounds.X + 1, e.Bounds.Y + 1, e.Bounds.Width - 3, e.Bounds.Height - 3);
                // Create start and sweep angles.
                float startAngle = 150.0F;
                float sweepAngle = 240.0F;
                // Fill pie to screen.
                e.Graphics.FillPie(new SolidBrush(Color.White), rect, startAngle, sweepAngle);
                #endregion

                //  值換角度
                double dSpead = double.Parse(e.Value.ToString());//0~100
                if (dSpead == 0) dSpead = 100;
                dSpead = dSpead * 240 / 100;//0~100 to angle 0~240 

                //Draw line along the current angle.
                double angle = ((dSpead + 150) * Math.PI) / (double)180;
                e.Graphics.DrawLine(new Pen(new SolidBrush(Color.Red), 1),
                    e.Bounds.X + normalX,
                    e.Bounds.Y + normalY,
                    e.Bounds.X + (normalX + (int)((double)normalX * Math.Cos(angle))),
                    e.Bounds.Y + (normalY + (int)((double)normalY * Math.Sin(angle))));

            }

            public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
            {
                return true;
            }
        }

        class DefaultMaintSpeedEditor : System.Drawing.Design.UITypeEditor
        {
            public DefaultMaintSpeedEditor()
            {
            }

            // Indicates whether the UITypeEditor provides a form-based (modal) dialog,
            // drop down dialog, or no UI outside of the properties window.
            public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.DropDown;
            }

            // Displays the UI for value selection.
            public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value)
            {
                // Return the value if the value is not of type Int32, Double and Single.
                if (value.GetType() != typeof(double) && value.GetType() != typeof(float) && value.GetType() != typeof(int))
                    return value;

                // Uses the IWindowsFormsEditorService to display a
                // drop-down UI in the Properties window.
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null)
                {
                    // Display an angle selection control and retrieve the value.


                    GUIMaintSpeedSelect speedControl = new GUIMaintSpeedSelect((int)value);

                    edSvc.DropDownControl(speedControl);

                    // Return the value in the appropraite data format.
                    if (value.GetType() == typeof(double))
                        return speedControl.m_nSpeed5to100;
                    else if (value.GetType() == typeof(float))
                        return (float)speedControl.m_nSpeed5to100;
                    else if (value.GetType() == typeof(int))
                        return (int)speedControl.m_nSpeed5to100;
                }
                return value;
            }

            // Draws a representation of the property's value.
            public override void PaintValue(System.Drawing.Design.PaintValueEventArgs e)
            {
                int normalX = (e.Bounds.Width / 2);
                int normalY = (e.Bounds.Height / 2);

                double radius = Math.Sqrt(Math.Pow(normalX, 2) + Math.Pow(normalY, 2));

                //Fill background and ellipse and center point.
                e.Graphics.FillRectangle(new SolidBrush(Color.DarkBlue), e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
                //e.Graphics.FillEllipse(new SolidBrush(Color.White), e.Bounds.X + 1, e.Bounds.Y + 1, e.Bounds.Width - 3, e.Bounds.Height - 3);
                e.Graphics.FillEllipse(new SolidBrush(Color.SlateGray), normalX + e.Bounds.X - 1, normalY + e.Bounds.Y - 1, 3, 3);

                #region 畫扇形
                // Create rectangle for ellipse.
                Rectangle rect = new Rectangle(e.Bounds.X + 1, e.Bounds.Y + 1, e.Bounds.Width - 3, e.Bounds.Height - 3);
                // Create start and sweep angles.
                float startAngle = 150.0F;
                float sweepAngle = 240.0F;
                // Fill pie to screen.
                e.Graphics.FillPie(new SolidBrush(Color.White), rect, startAngle, sweepAngle);
                #endregion

                //  值換角度
                double dSpead = double.Parse(e.Value.ToString());//0~100
                if (dSpead == 0) dSpead = 100;
                dSpead = dSpead * 240 / 100;//0~100 to angle 0~240 

                //Draw line along the current angle.
                double angle = ((dSpead + 150) * Math.PI) / (double)180;
                e.Graphics.DrawLine(new Pen(new SolidBrush(Color.Red), 1),
                    e.Bounds.X + normalX,
                    e.Bounds.Y + normalY,
                    e.Bounds.X + (normalX + (int)((double)normalX * Math.Cos(angle))),
                    e.Bounds.Y + (normalY + (int)((double)normalY * Math.Sin(angle))));

            }

            public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
            {
                return true;
            }
        }

        private static System.Drawing.Image resizeImage(System.Drawing.Image imgToResize, Size size)
        {
            //Get the image current width  
            int sourceWidth = imgToResize.Width;
            //Get the image current height  
            int sourceHeight = imgToResize.Height;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            //Calulate  width with new desired size  
            nPercentW = ((float)size.Width / (float)sourceWidth);
            //Calculate height with new desired size  
            nPercentH = ((float)size.Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;
            //New Width  
            int destWidth = (int)(sourceWidth * nPercent);
            //New Height  
            int destHeight = (int)(sourceHeight * nPercent);
            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Draw image with new width and height  
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();
            return (System.Drawing.Image)b;
        }

    }
}
