using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Rorze.Equipments.Unit;
using RorzeApi.Class;
using RorzeComm.Log;
using RorzeUnit.Class;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.RC500.RCEnum;
using RorzeUnit.Class.Robot;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Interface;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using static Rorze.Equipments.Unit.SRobot;
using static RorzeUnit.Class.SWafer;

namespace RorzeApi
{
    public partial class frmTeachRobotMapping : Form
    {
        private int _nCurrStage = 0;        //  Stage number of Robot
        private int m_nStep = 1000;         //  Jog Step
        private float frmX;                 //  Zoom 放入窗體的寬度
        private float frmY;                 //  Zoom 放入窗體的高度
        private bool isLoaded;              //  Zoom 是否已設定各控制的尺寸資料到Tag屬性
        private bool bSelectUpArm;          //  Teaching Arm
        private bool _bSimulate;
        private bool _bRobotTeachStart;
        public bool RobotTeachStart { get { return _bRobotTeachStart; } set { _bRobotTeachStart = value; } }

        private string _strSelectStgName;

        private enumRbtAddress m_SelectUnit;
        private enumRC550Axis m_eXAX1 = enumRC550Axis.AXS1;

        private Class.SPermission _userManager;   //  管理LOGIN使用者權限
        private string _strUserName;//登入者名稱
        private bool m_bIsRunMode = false;
        private SProcessDB _accessDBlog;
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        private I_Robot m_robot;
        private List<I_Robot> m_robotList;
        private List<I_Loadport> ListSTG;
        private List<I_Aligner> m_alignerList;
        private List<I_Buffer> m_bufList;
        private List<SSEquipment> m_listEQM;
        private List<Button> m_btnSelectRobotList = new List<Button>();

        public frmTeachRobotMapping(
            List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN,
            List<I_Buffer> listBUF,
            SProcessDB db, SPermission userManager, bool bIsRunMode, List<SSEquipment> listEQM)
        {
            InitializeComponent();
            this.Size = new Size(970/*840*/, 718/*700*/);

            //  消失頁籤
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);

            m_robotList = listTRB;
            ListSTG = listSTG;
            m_alignerList = listALN;
            m_bufList = listBUF;

            m_listEQM = listEQM;
            _bSimulate = GParam.theInst.IsSimulate;
            m_bIsRunMode = bIsRunMode;
            _accessDBlog = db;
            _userManager = userManager;

            #region Select Robot Button
            tlpSelectRobot.RowStyles.Clear();
            tlpSelectRobot.ColumnStyles.Clear();
            tlpSelectRobot.RowCount = 1;
            tlpSelectRobot.ColumnCount = ListSTG.Count;
            tlpSelectRobot.Dock = DockStyle.Fill;
            for (int i = 0; i < m_robotList.Count; i++)
            {
                if (m_robotList[i].Disable) continue;
                if (GParam.theInst.GetRobot_AllowPort(m_robotList[i].BodyNo - 1).Contains('1') == false) continue;
                tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                Button btn = new Button();
                btn.Font = new Font("微軟正黑體", 18, FontStyle.Bold);
                btn.Text = GParam.theInst.GetLanguage("Robot" + (char)(64 + m_robotList[i].BodyNo));
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Click += btnSelectRobot_Click;
                m_btnSelectRobotList.Add(btn);
                tlpSelectRobot.Controls.Add(btn, m_btnSelectRobotList.Count - 1, 0);
            }
            tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            #endregion

            if (GParam.theInst.FreeStyle)
            {
                btnSave.Image = RorzeApi.Properties.Resources._48_save_;
                btnNext.Image = RorzeApi.Properties.Resources._32_next_;
                btnCancel.Image = RorzeApi.Properties.Resources._32_cancel_;

                btnRbArmFW.Image = btnRbZUp.Image = RorzeApi.Properties.Resources.Teachtop_;
                btnRbArmBW.Image = btnRbZDown.Image = RorzeApi.Properties.Resources.Teachdown_;
                btnRbRotFW.Image = RorzeApi.Properties.Resources.TeachCCW_;
                btnRbRotBW.Image = RorzeApi.Properties.Resources.TeachCW_;
                btnRbXFW.Image = RorzeApi.Properties.Resources.Teachforw_;
                btnRbXBW.Image = RorzeApi.Properties.Resources.Teachback_;
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
            if (isLoaded)
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
        }
        #endregion

        private void btnSelectRobot_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.Insert(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "frmTeachRobot", _strUserName, "Select Robot", btn.Name);
            cbxStage.SelectedIndex = -1;
            cbxType.SelectedIndex = -1;

            ChangeButtun(btnRobotWmap, false);
            ChangeButtun(btnTeachArm1, false);
            ChangeButtun(btnTeachArm2, false);
            ChangeButtun(btnSave, false);

            for (int i = 0; i < m_btnSelectRobotList.Count; i++)
            {
                if (m_btnSelectRobotList[i] == btn)
                {
                    if (GParam.theInst.FreeStyle)
                    {
                        m_btnSelectRobotList[i].BackColor = GParam.theInst.ColorTitle;
                    }
                    else
                    {
                        m_btnSelectRobotList[i].BackColor = Color.LightBlue;
                    }

                    string strName1 = GParam.theInst.GetLanguage("RobotA");
                    string strName2 = GParam.theInst.GetLanguage("RobotB");

                    if (strName1 == btn.Text)
                    {
                        m_robot = m_robotList[0]; // as SSRobotRR75x;
                    }
                    else if (strName2 == btn.Text)
                    {
                        m_robot = m_robotList[1]; // as SSRobotRR75x;
                    }
                    else
                        switch (btn.Text)
                        {
                            case "RobotA":
                            case "Robot A":
                                m_robot = m_robotList[0]; break; // as SSRobotRR75x; break;
                            case "RobotB":
                            case "Robot B":
                                m_robot = m_robotList[1]; break; // as SSRobotRR75x; break;
                            default:
                                m_robot = null; break;
                        }

                    if (m_robot == null)
                    {
                        new frmMessageBox("Robot isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }


                    cbxStage.Items.Clear();
                    foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_robot.BodyNo).ToArray())
                    {
                        I_Loadport stg = RbtAddressConvertLpObject(item.strDefineName);
                        if (stg == null) continue;
                        bool b = false;
                        switch (item.strDefineName)
                        {
                            case enumRbtAddress.STG1_12:
                            case enumRbtAddress.STG2_12:
                            case enumRbtAddress.STG3_12:
                            case enumRbtAddress.STG4_12:
                            case enumRbtAddress.STG5_12:
                            case enumRbtAddress.STG6_12:
                            case enumRbtAddress.STG7_12:
                            case enumRbtAddress.STG8_12:
                                b = (stg.Disable == false && m_robot.RobotHardwareAllow(SWafer.enumFromLoader.LoadportA + stg.BodyNo - 1)); break;
                            case enumRbtAddress.STG1_08:
                            case enumRbtAddress.STG2_08:
                            case enumRbtAddress.STG3_08:
                            case enumRbtAddress.STG4_08:
                            case enumRbtAddress.STG5_08:
                            case enumRbtAddress.STG6_08:
                            case enumRbtAddress.STG7_08:
                            case enumRbtAddress.STG8_08:
                                b = (stg.Disable == false && m_robot.RobotHardwareAllow(SWafer.enumFromLoader.LoadportA + stg.BodyNo - 1) && stg.UseAdapter); break;
                        }
                        if (b) cbxStage.Items.Add(item.strDisplayName);
                    }
                    if (cbxStage.Items.Count > 0) cbxStage.SelectedIndex = 0;

                    if (m_robot.XaxsDisable)
                    {
                        nudXAxisArm.Visible = lblXAxisArm.Visible = false;
                        gbRobotXControl.Visible = false;
                        lblTrackEncoder.Visible = lblXasis.Visible = lblXaxisUnits.Visible = false;
                    }
                    else
                    {
                        nudXAxisArm.Visible = lblXAxisArm.Visible = true;
                        gbRobotXControl.Visible = true;
                        lblTrackEncoder.Visible = lblXasis.Visible = lblXaxisUnits.Visible = true;
                    }

                    switch (m_robot.MappingType)
                    {
                        case enumDEQU_15_waferSearch.UpperFinger:
                        case enumDEQU_15_waferSearch.UpperWrist:
                            btnTeachArm1.Visible = true;
                            btnTeachArm2.Visible = false;
                            break;
                        case enumDEQU_15_waferSearch.LowerFinger:
                        case enumDEQU_15_waferSearch.LowerWrist:
                            btnTeachArm1.Visible = false;
                            btnTeachArm2.Visible = true;
                            break;
                        case enumDEQU_15_waferSearch.UpperLowerFinger:
                        case enumDEQU_15_waferSearch.UpperLowerWrist:
                        case enumDEQU_15_waferSearch.UpperFingerLowerWrist:
                        case enumDEQU_15_waferSearch.UpperWristLowerWrist:
                            btnTeachArm1.Visible = btnTeachArm2.Visible = true;
                            break;
                        default:
                            btnTeachArm1.Visible = btnTeachArm2.Visible = false;
                            break;
                    }



                    continue;
                }
                else
                    m_btnSelectRobotList[i].BackColor = System.Drawing.SystemColors.ControlLight;
            }
        }

        private void tmrUI_Tick(object sender, EventArgs e)
        {
            tbCurrentStep.Text = m_nStep.ToString();
            if (tabControl1.SelectedTab == tabPage2)
            {
                if (m_robot == null || false == m_robot.Connected) return;
                lblUpArmEncoder.Text = m_robot.UpperArm.Position.ToString();
                lblLowArmEncoder.Text = m_robot.LowerArm.Position.ToString();
                lblREncoder.Text = m_robot.Rotater.Position.ToString();
                lblLEncoder.Text = m_robot.Lifter.Position.ToString();
                if (m_robot.XaxsDisable == false) lblTrackEncoder.Text = m_robot.Traverse.Position.ToString();
            }

            if (tabControl2.SelectedTab == tabTypeEnable)
            {
                lblInfoType01.ForeColor = cbxInfoTypeEnable01.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType02.ForeColor = cbxInfoTypeEnable02.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType03.ForeColor = cbxInfoTypeEnable03.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType04.ForeColor = cbxInfoTypeEnable04.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType05.ForeColor = cbxInfoTypeEnable05.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType06.ForeColor = cbxInfoTypeEnable06.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType07.ForeColor = cbxInfoTypeEnable07.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType08.ForeColor = cbxInfoTypeEnable08.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType09.ForeColor = cbxInfoTypeEnable09.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType10.ForeColor = cbxInfoTypeEnable10.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType11.ForeColor = cbxInfoTypeEnable11.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType12.ForeColor = cbxInfoTypeEnable12.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType13.ForeColor = cbxInfoTypeEnable13.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType14.ForeColor = cbxInfoTypeEnable14.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType15.ForeColor = cbxInfoTypeEnable15.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType16.ForeColor = cbxInfoTypeEnable16.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType17.ForeColor = cbxInfoTypeEnable17.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType18.ForeColor = cbxInfoTypeEnable18.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType19.ForeColor = cbxInfoTypeEnable19.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;
                lblInfoType20.ForeColor = cbxInfoTypeEnable20.SelectedIndex == 1 ? Color.LimeGreen : Color.DimGray;

            }


        }

        //  啟用 Teaching 畫面
        private void Form1_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                tmrUI.Enabled = this.Visible;
                ChangeButtun(btnTeachArm1, false);
                ChangeButtun(btnTeachArm2, false);
                ChangeButtun(btnSave, false);
                _strUserName = _userManager.UserID;
                if (this.Visible)
                {
                    if (tabControl1.SelectedTab == tabPage1)
                    {
                        m_btnSelectRobotList[0].PerformClick();
                        SpinWait.SpinUntil(() => false, 10);
                        cbxStage_SelectionChangeCommitted(cbxStage, new EventArgs());
                    }
                }
                else
                {
                    foreach (I_Robot item in m_robotList)
                    {
                        if (item.Disable) continue;

                        item.ResetProcessCompleted();
                        if (m_bIsRunMode == true)
                        {
                            item.SspdW(item.GetAckTimeout, GParam.theInst.GetRobot_RunSpeed(item.BodyNo - 1));//  回原速度
                        }
                        else
                        {
                            item.SspdW(item.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(item.BodyNo - 1));//  回原速度
                        }
                        item.WaitProcessCompleted(item.GetAckTimeout);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }

        //  選擇  UNIT stg address 
        private void cbxStage_SelectionChangeCommitted(object sender, EventArgs e)
        {

            _strSelectStgName = cbxStage.SelectedItem.ToString();

            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_robot.BodyNo).ToArray())//搜尋所有點位
            {
                if (item.strDisplayName == _strSelectStgName)//注意是顯示名稱相同
                {
                    //  找到使用者選擇的點位
                    m_SelectUnit = item.strDefineName;
                    I_Loadport stg = RbtAddressConvertLpObject(item.strDefineName);
                    if (stg == null) continue;
                    //  選的是Loadport                  
                    cbxType.Items.Clear();
                    cbxType.Enabled = true;
                    cbxType.SelectedIndex = -1;
                    //  判斷選擇位置對應到Robot的地址 nStage               
                    switch (item.strDefineName)
                    {
                        case enumRbtAddress.STG1_08:
                        case enumRbtAddress.STG2_08:
                        case enumRbtAddress.STG3_08:
                        case enumRbtAddress.STG4_08:
                        case enumRbtAddress.STG5_08:
                        case enumRbtAddress.STG6_08:
                        case enumRbtAddress.STG7_08:
                        case enumRbtAddress.STG8_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(stg.GetDPRMData[i + 16][16]); }//16~31
                            break;
                        case enumRbtAddress.STG1_12:
                        case enumRbtAddress.STG2_12:
                        case enumRbtAddress.STG3_12:
                        case enumRbtAddress.STG4_12:
                        case enumRbtAddress.STG5_12:
                        case enumRbtAddress.STG6_12:
                        case enumRbtAddress.STG7_12:
                        case enumRbtAddress.STG8_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(stg.GetDPRMData[i][16]); }//0~15
                            break;
                    }
                    break;//成功找到位置
                }
            }

            ChangeButtun(btnRobotWmap, false);
            ChangeButtun(btnTeachArm1, false);
            ChangeButtun(btnTeachArm2, false);
            ChangeButtun(btnSave, false);

            ShowLoadportMapEnable();
        }
        //  選擇  FOUP TYPE
        private void cbxType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            switch (m_SelectUnit)
            {
                case enumRbtAddress.STG1_08:
                case enumRbtAddress.STG1_12:
                case enumRbtAddress.STG2_08:
                case enumRbtAddress.STG2_12:
                case enumRbtAddress.STG3_08:
                case enumRbtAddress.STG3_12:
                case enumRbtAddress.STG4_08:
                case enumRbtAddress.STG4_12:
                case enumRbtAddress.STG5_08:
                case enumRbtAddress.STG5_12:
                case enumRbtAddress.STG6_08:
                case enumRbtAddress.STG6_12:
                case enumRbtAddress.STG7_08:
                case enumRbtAddress.STG7_12:
                case enumRbtAddress.STG8_08:
                case enumRbtAddress.STG8_12:
                    _nCurrStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, m_SelectUnit) + cbxType.SelectedIndex;
                    break;
                case enumRbtAddress.BarCode:
                case enumRbtAddress.ALN1:
                case enumRbtAddress.ALN2:
                case enumRbtAddress.BUF1:
                case enumRbtAddress.BUF2:
                    _nCurrStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, m_SelectUnit);
                    break;
            }

            if (_nCurrStage != -1)
                ShowStageMappingData(_nCurrStage);
        }
        //  Robot 執行 ExeGetTeachData 完成後觸發事件
        private void _robot_OnGetTeachDataCompleted(object sender, bool bSuc)
        {
            I_Robot trb = sender as I_Robot;
            trb.OnGetTeachDataCompleted -= _robot_OnGetTeachDataCompleted;//Done

            //if (_bSimulate == false)
            {
                nudMargin.Value = int.Parse(m_robot.DMPRData[0]);
                nudZSpeed.Value = int.Parse(m_robot.DMPRData[1]);
                nudMinThickness.Value = int.Parse(m_robot.DMPRData[2]);
                nudMaxThickness.Value = int.Parse(m_robot.DMPRData[3]);
                nudCross.Value = int.Parse(m_robot.DMPRData[4]);

                nudFirstSlot.Value = int.Parse(m_robot.DMPRData[6]);//very bottom
                nudLastSlot.Value = int.Parse(m_robot.DMPRData[8]);//very top

                nudSlotNum.Value = int.Parse(m_robot.DMPRData[13]);

                nudZAxisTopArm.Value = int.Parse(m_robot.DMPRData[20]) + int.Parse(m_robot.DMPRData[22]);//Z軸搜尋終點點=起點+移動量

                nudXAxisArm.Value = int.Parse(m_robot.DMPRData[21]);

                nudZAxisBottomArm.Value = int.Parse(m_robot.DMPRData[22]);//Z軸搜尋起點

                nudRotArm.Value = int.Parse(m_robot.DMPRData[23]);
                nudArm.Value = int.Parse(m_robot.DMPRData[24]);
            }

            ChangeButtun(btnRobotWmap, true);
            ChangeButtun(btnTeachArm1, true);
            ChangeButtun(btnTeachArm2, true);
            ChangeButtun(btnSave, true);
        }
        //  Robot 執行 ExeSetTeachData 完成後觸發事件
        private void _robot_OnSetTeachDataCompleted(object sender, bool bSuc)
        {
            I_Robot trb = sender as I_Robot;
            trb.OnSetDmprDataCompleted -= _robot_OnSetTeachDataCompleted;//Done

            ShowStageMappingData(_nCurrStage);

            if (RobotTeachStart)
            {
                RobotTeachStart = false;
            }

            m_robot.UpperArm.GetPos();
            m_robot.LowerArm.GetPos();
            m_robot.Rotater.GetPos();
            m_robot.Lifter.GetPos();
            if (m_robot.XaxsDisable == false) m_robot.Traverse.GetPos();

            //ChangeButtun(btnSave, true);
            EnablePage1Button(true);
        }
        //  清除介面顯示資料
        private void DataSetEnableControlButton(bool bAct)
        {
            if (bAct == false)
            {
                nudZAxisTopArm.Value = 0;
                nudZAxisBottomArm.Value = 0;
                nudXAxisArm.Value = 0;
                nudRotArm.Value = 0;
                nudArm.Value = 0;

                //nudPickUp.Value = 7000;
                //nudSlotPitch.Value = 10000;
                nudFirstSlot.Value = 25;
                nudLastSlot.Value = 1000;
            }
        }
        //  叫 robot 取得資料
        private void ShowStageMappingData(int nStage)
        {
            DataSetEnableControlButton(nStage != -1);
            if (m_robot == null || nStage <= 0) return;
            m_robot.OnGetDmprDataCompleted -= _robot_OnGetTeachDataCompleted;
            m_robot.OnGetDmprDataCompleted += _robot_OnGetTeachDataCompleted;
            m_robot.GetDMPRData(nStage);

        }
        //  開始
        private void btnTeachArm1_Click(object sender, EventArgs e)
        {
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Start teach upper arm.");
            bSelectUpArm = true;
            TeachRobotStart();
        }
        private void btnTeachArm2_Click(object sender, EventArgs e)
        {
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Start teach lower arm.");
            bSelectUpArm = false;
            TeachRobotStart();
        }
        private void TeachRobotStart()
        {
            if (_bSimulate)
            {
                lblTrackEncoder.Text = "10000";
                lblLEncoder.Text = "10000";
                lblREncoder.Text = "20000";
                lblUpArmEncoder.Text = "30000";
                lblLowArmEncoder.Text = "30000";
            }

            if (m_robot == null)
            {
                frmMessageBox frm = new frmMessageBox("Please select robot?", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                frm.ShowDialog();
                return;
            }
            if (m_bIsRunMode)
            {
                new frmMessageBox("Is RunMode!!!Can't teaching.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (cbxStage.SelectedIndex == -1)
            {
                new frmMessageBox("Please select stage", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (cbxType.SelectedIndex == -1)// 沒有選擇Type
            {
                #region _nCurrStage如果是Loadport
                if (
                    (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_08) + 15)
                 )
                {
                    new frmMessageBox("Please select Foup type or Jig type", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                #endregion
            }
            if (m_robot.GetSpeed > 6 || m_robot.GetSpeed == 0)
            {
                frmMessageBox frm = new frmMessageBox("Mode speed is too large,sure to continue?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (frm.ShowDialog() == DialogResult.No)
                {
                    return;
                }
            }

            delegateMDILock?.Invoke(true);

            tabControl1.SelectedTab = tabPage2;

            EnableControlButton(false);
            pnPrepare.BackColor = GParam.theInst.ColorWaitYellow;
            pnHome.BackColor = SystemColors.Control;
            pnExtd.BackColor = SystemColors.Control;
            pnTeach.BackColor = SystemColors.Control;
            pnFinish.BackColor = SystemColors.Control;

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Excuting Origin,Please wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Excuting Origin,Please wait.\r"));
            m_eStep = eTeachStep.Prepare;
            Cursor.Current = Cursors.WaitCursor;
            DoOrgn();
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Save Data Fashion");

            if (m_bIsRunMode)
            {
                new frmMessageBox("Is RunMode!!!Can't save data.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if (cbxType.SelectedIndex == -1)// 沒有選擇Type
            {
                new frmMessageBox("Please select Foup type", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            ChangeButtun(btnRobotWmap, false);
            ChangeButtun(btnTeachArm1, false);
            ChangeButtun(btnTeachArm2, false);
            ChangeButtun(btnSave, false);

            if (m_robot.Connected && _bSimulate == false)
            {
                m_robot.DMPRData[0] = nudMargin.Value.ToString();
                m_robot.DMPRData[1] = nudZSpeed.Value.ToString();
                m_robot.DMPRData[2] = nudMinThickness.Value.ToString();
                m_robot.DMPRData[3] = nudMaxThickness.Value.ToString();
                m_robot.DMPRData[4] = nudCross.Value.ToString();
                m_robot.DMPRData[5] = "00";
                m_robot.DMPRData[6] = nudFirstSlot.Value.ToString();
                m_robot.DMPRData[8] = nudLastSlot.Value.ToString();
                m_robot.DMPRData[12] = "1";
                m_robot.DMPRData[13] = nudSlotNum.Value.ToString();

                m_robot.DMPRData[17] = "0";
                m_robot.DMPRData[18] = "0";

                m_robot.DMPRData[20] = (nudZAxisTopArm.Value - nudZAxisBottomArm.Value).ToString();
                m_robot.DMPRData[21] = nudXAxisArm.Value.ToString();
                m_robot.DMPRData[22] = nudZAxisBottomArm.Value.ToString();
                m_robot.DMPRData[23] = nudRotArm.Value.ToString();
                m_robot.DMPRData[24] = nudArm.Value.ToString();
            }

            SaveLoadportMapEnable();

            m_robot.OnSetDmprDataCompleted -= _robot_OnSetTeachDataCompleted;//Save
            m_robot.OnSetDmprDataCompleted += _robot_OnSetTeachDataCompleted;//Save
            m_robot.SetDMPRData(_nCurrStage);

        }
        private void EnablePage1Button(bool bAct)
        {
            gbPosition.Enabled = bAct;
            tlpDTRB.Enabled = bAct;
            //ChangeButtun(btnSave, bAct);
            delegateMDILock?.Invoke(!bAct);
        }



        //Robot mapping
        private void btnRobotWmap_Click(object sender, EventArgs e)
        {
            if (cbxStage.SelectedIndex == -1)
            {
                new frmMessageBox("Please select stage", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if (cbxType.SelectedIndex == -1)// 沒有選擇Type
            {
                #region _nCurrStage如果是Loadport
                if (
                    (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_12) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_12) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_08) + 15)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_08) + 15)
                 )
                {
                    new frmMessageBox("Please select Foup type or Jig type", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                #endregion
            }

            int nStgIndex = -1;
            switch (m_SelectUnit)
            {
                case enumRbtAddress.STG1_12:
                case enumRbtAddress.STG1_08:
                    nStgIndex = 0;
                    break;
                case enumRbtAddress.STG2_12:
                case enumRbtAddress.STG2_08:
                    nStgIndex = 1;
                    break;
                case enumRbtAddress.STG3_12:
                case enumRbtAddress.STG3_08:
                    nStgIndex = 2;
                    break;
                case enumRbtAddress.STG4_12:
                case enumRbtAddress.STG4_08:
                    nStgIndex = 3;
                    break;
                case enumRbtAddress.STG5_12:
                case enumRbtAddress.STG5_08:
                    nStgIndex = 4;
                    break;
                case enumRbtAddress.STG6_12:
                case enumRbtAddress.STG6_08:
                    nStgIndex = 5;
                    break;
                case enumRbtAddress.STG7_12:
                case enumRbtAddress.STG7_08:
                    nStgIndex = 6;
                    break;
                case enumRbtAddress.STG8_12:
                case enumRbtAddress.STG8_08:
                    nStgIndex = 7;
                    break;
            }
            if (nStgIndex == -1)
            {
                new frmMessageBox("Please select loadport", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            else if (ListSTG[nStgIndex].FoupExist == false)
            {
                new frmMessageBox("Foup Not Exist!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            ChangeButtun(btnRobotWmap, false);
            ChangeButtun(btnTeachArm1, false);
            ChangeButtun(btnTeachArm2, false);
            ChangeButtun(btnSave, false);
            delegateMDILock?.Invoke(true);

            if (ListSTG[nStgIndex].IsDoorOpen == false)
            {
                ListSTG[nStgIndex].ResetInPos();
                ListSTG[nStgIndex].ClmpW(3000);
                ListSTG[nStgIndex].WaitInPos(60000);
            }

            rtbMapInfo.Clear();

            m_robot.OnWmapFunctionCompleted -= _robot_OnWmapFunctionCompleted;
            m_robot.OnWmapFunctionCompleted += _robot_OnWmapFunctionCompleted;
            m_robot.WMAP(_nCurrStage);

            //ListSTG[nStgIndex].CLMP();


        }
        private void _robot_OnWmapFunctionCompleted(object sender, bool bSuc)

        {
            I_Robot trb = sender as I_Robot;
            trb.OnWmapFunctionCompleted -= _robot_OnWmapFunctionCompleted;//Done

            GetMappingInfomation();

            ChangeButtun(btnRobotWmap, true);
            ChangeButtun(btnTeachArm1, true);
            ChangeButtun(btnTeachArm2, true);
            ChangeButtun(btnSave, true);
            delegateMDILock?.Invoke(false);//解功能列
        }
        private void GetMappingInfomation()
        {
            int nBottomSlot, nTopSlot, nSlotNo, nSpace, nTop1, nBottom1, nSpace1, nBow = 0, nOffset;
            string strMap, strShow;

            try
            {
                //  GMAP
                strMap = m_robot.GetMappingData;
                strShow = "Mapping Data = " + strMap;
                rtbMapInfo.AppendText(strShow + "\n");
                //  DMPR
                nBottomSlot = int.Parse(m_robot.DMPRData[6]);
                nTopSlot = int.Parse(m_robot.DMPRData[8]);
                strShow = "Top:" + nTopSlot + "; Bottom:" + nBottomSlot;
                rtbMapInfo.AppendText(strShow + "\n");


                //  DPRM
                nSlotNo = int.Parse(m_robot.DMPRData[13]);
                nSpace = Math.Abs(nTopSlot - nBottomSlot) / (nSlotNo - 1);
                nOffset = 0;

                //  RCA2

                for (int i = 0; i < nSlotNo; i++)//從下往上MAPPING
                {


                    m_robot.Rca2W(3000, i * 2);
                    string[] theRCA2 = m_robot.GetRac2Data;

                    nBottom1 = int.Parse(theRCA2[0]);
                    nTop1 = int.Parse(theRCA2[1]);
                    nSpace1 = int.Parse(theRCA2[2]);

                    if (strMap[i] != '0')
                        nBow = ((nTop1 + nBottom1) / 2) - (nBottomSlot + i * nSpace);//實際值減去理論
                    else
                        nBow = 0;

                    if (nBow < 0)
                        strShow = string.Format("{0:D2}:{1:D8} - {2:D8} = {3:D8}\t {4:D7}\t{5}", i + 1, nTop1, nBottom1, nSpace1, nBow, strMap[i]);
                    else
                        strShow = string.Format("{0:D2}:{1:D8} - {2:D8} = {3:D8}\t {4:D8}\t{5}", i + 1, nTop1, nBottom1, nSpace1, nBow, strMap[i]);

                    rtbMapInfo.AppendText(strShow + "\n");

                }




            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }
        //Robot mapping enable ini
        private void SaveLoadportMapEnable()
        {
            I_Loadport stg = RbtAddressConvertLpObject(m_SelectUnit);
            if (stg == null) return;
            int nIndx = stg.BodyNo - 1;

            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 0, cbxInfoTypeEnable01.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 1, cbxInfoTypeEnable02.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 2, cbxInfoTypeEnable03.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 3, cbxInfoTypeEnable04.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 4, cbxInfoTypeEnable05.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 5, cbxInfoTypeEnable06.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 6, cbxInfoTypeEnable07.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 7, cbxInfoTypeEnable08.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 8, cbxInfoTypeEnable09.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 9, cbxInfoTypeEnable10.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 10, cbxInfoTypeEnable11.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 11, cbxInfoTypeEnable12.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 12, cbxInfoTypeEnable13.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 13, cbxInfoTypeEnable14.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 14, cbxInfoTypeEnable15.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 15, cbxInfoTypeEnable16.SelectedIndex == 1);

            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 16, cbxInfoTypeEnable17.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 17, cbxInfoTypeEnable18.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 18, cbxInfoTypeEnable19.SelectedIndex == 1);
            GParam.theInst.SetTrbMapInfoEnableList(nIndx, 19, cbxInfoTypeEnable20.SelectedIndex == 1);

        }
        private void ShowLoadportMapEnable()
        {
            I_Loadport stg = RbtAddressConvertLpObject(m_SelectUnit);
            if (stg == null) return;
            int nIndx = stg.BodyNo - 1;

            cbxInfoTypeEnable01.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 0) == false ? 0 : 1;
            cbxInfoTypeEnable02.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 1) == false ? 0 : 1;
            cbxInfoTypeEnable03.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 2) == false ? 0 : 1;
            cbxInfoTypeEnable04.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 3) == false ? 0 : 1;
            cbxInfoTypeEnable05.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 4) == false ? 0 : 1;
            cbxInfoTypeEnable06.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 5) == false ? 0 : 1;
            cbxInfoTypeEnable07.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 6) == false ? 0 : 1;
            cbxInfoTypeEnable08.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 7) == false ? 0 : 1;
            cbxInfoTypeEnable09.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 8) == false ? 0 : 1;
            cbxInfoTypeEnable10.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 9) == false ? 0 : 1;
            cbxInfoTypeEnable11.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 10) == false ? 0 : 1;
            cbxInfoTypeEnable12.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 11) == false ? 0 : 1;
            cbxInfoTypeEnable13.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 12) == false ? 0 : 1;
            cbxInfoTypeEnable14.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 13) == false ? 0 : 1;
            cbxInfoTypeEnable15.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 14) == false ? 0 : 1;
            cbxInfoTypeEnable16.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 15) == false ? 0 : 1;
            cbxInfoTypeEnable17.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 16) == false ? 0 : 1;
            cbxInfoTypeEnable18.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 17) == false ? 0 : 1;
            cbxInfoTypeEnable19.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 18) == false ? 0 : 1;
            cbxInfoTypeEnable20.SelectedIndex = GParam.theInst.GetTrbMapInfoEnableList(nIndx, 19) == false ? 0 : 1;
        }
        //  =====================================================================================================================
        private delegate void UpdateButtunUI(Button MsgBox, bool ret);
        public void ChangeButtun(Button MsgBox, bool ret)
        {
            if (InvokeRequired)
            {
                UpdateButtunUI dlg = new UpdateButtunUI(ChangeButtun);
                this.Invoke(dlg, MsgBox, ret);
            }
            else
            {
                if (MsgBox.Enabled != ret)
                {
                    MsgBox.Enabled = ret;
                    if (ret == true)
                        MsgBox.BackColor = Color.Transparent;
                    else
                        MsgBox.BackColor = Color.Gray;
                }
            }
        }
        //  Jog 按鈕解鎖
        private void EnableControlButton(bool bAct)
        {
            ChangeButtun(btnRbArmFW, bAct);
            ChangeButtun(btnRbArmBW, bAct);
            ChangeButtun(btnRbRotFW, bAct);
            ChangeButtun(btnRbRotBW, bAct);
            ChangeButtun(btnRbZUp, bAct);
            ChangeButtun(btnRbZDown, bAct);
            ChangeButtun(btnRbXBW, bAct);
            ChangeButtun(btnRbXFW, bAct);
        }
        //  Jog 按鈕動作
        private void btnJog_Click(object sender, EventArgs e)
        {
            if (Math.Abs(m_nStep) > 10000)
            {
                frmMessageBox frm = new frmMessageBox("Step value is too large,sure to continue?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (frm.ShowDialog() == DialogResult.No)
                {
                    return;
                }
            }

            EnableControlButton(false);
            EnableProcedureButton(false);
            Button btn = (Button)sender;
            if (m_robot.Connected)
            {
                m_robot.OnJobFunctionCompleted -= _robot_OnJobFunctionCompleted;//STEP
                m_robot.OnJobFunctionCompleted += _robot_OnJobFunctionCompleted;//STEP
                if (btn == btnRbXFW)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "X Axis FW Button Step[" + m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.Xax, m_nStep);
                }
                else if (btn == btnRbXBW)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "X Axis BW Button Step[" + -1 * m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.Xax, -1 * m_nStep);
                }
                else if (btn == btnRbZUp)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Z Axis Up Button Step[" + m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.Zax, m_nStep);
                }
                else if (btn == btnRbZDown)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Z Axis Down Button Step[" + -1 * m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.Zax, -1 * m_nStep);
                }
                else if (btn == btnRbRotFW)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "R Axis FW Button Step[" + m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.Rot, m_nStep);
                }
                else if (btn == btnRbRotBW)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "R Axis BW Button Step[" + -1 * m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.Rot, -1 * m_nStep);
                }
                else if (btn == btnRbArmFW)
                {
                    if (bSelectUpArm == false)
                    {
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "LowArm FW Button Step[" + m_nStep + "]");
                        m_robot.STEP(enumRobotAxis.Arm2, m_nStep);
                    }
                    else
                    {
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "UpArm FW Button Step[" + m_nStep + "]");
                        m_robot.STEP(enumRobotAxis.Arm1, m_nStep);
                    }
                }
                else if (btn == btnRbArmBW)
                {
                    if (bSelectUpArm == false)
                    {
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "LowArm BW Button Step[" + -1 * m_nStep + "]");
                        m_robot.STEP(enumRobotAxis.Arm2, -1 * m_nStep);
                    }
                    else
                    {
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "UpArm BW Button Step[" + -1 * m_nStep + "]");
                        m_robot.STEP(enumRobotAxis.Arm1, -1 * m_nStep);
                    }
                }
                else
                {
                    m_robot.OnJobFunctionCompleted -= _robot_OnJobFunctionCompleted;//STEP
                }
            }
            else
            {
                switch (btn.Name)
                {
                    case "btnRbXBW":
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "X Axis BW Button Step[" + m_nStep + "]");
                        lblTrackEncoder.Text = (int.Parse(lblTrackEncoder.Text) + (-1 * m_nStep)).ToString();
                        break;
                    case "btnRbXFW":
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "X Axis FW Button Step[" + -1 * m_nStep + "]");
                        lblTrackEncoder.Text = (int.Parse(lblTrackEncoder.Text) + (1 * m_nStep)).ToString();
                        break;
                    case "btnRbZUp":
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Z Axis Up Button Step[" + m_nStep + "]");
                        lblLEncoder.Text = (int.Parse(lblLEncoder.Text) + (m_nStep)).ToString();
                        break;
                    case "btnRbZDown":
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Z Axis Down Button Step[" + -1 * m_nStep + "]");
                        lblLEncoder.Text = (int.Parse(lblREncoder.Text) + (-1 * m_nStep)).ToString();
                        break;
                    case "btnRbRotFW":
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "R Axis FW Button Step[" + m_nStep + "]");
                        lblREncoder.Text = (int.Parse(lblREncoder.Text) + (m_nStep)).ToString();
                        break;
                    case "btnRbRotBW":
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "R Axis BW Button Step[" + -1 * m_nStep + "]");
                        lblREncoder.Text = (int.Parse(lblREncoder.Text) + (-1 * m_nStep)).ToString();
                        break;
                    case "btnRbArmFW":
                        if (bSelectUpArm == false)
                        {
                            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "LowArm FW Button Step[" + m_nStep + "]");
                            lblLowArmEncoder.Text = (int.Parse(lblLowArmEncoder.Text) + (m_nStep)).ToString();
                        }
                        else
                        {
                            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "UpArm FW Button Step[" + m_nStep + "]");
                            lblUpArmEncoder.Text = (int.Parse(lblUpArmEncoder.Text) + (m_nStep)).ToString();
                        }
                        break;
                    case "btnRbArmBW":
                        if (bSelectUpArm == false)
                        {
                            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "LowArm BW Button Step[" + -1 * m_nStep + "]");
                            lblLowArmEncoder.Text = (int.Parse(lblLowArmEncoder.Text) + (-1 * m_nStep)).ToString();
                        }
                        else
                        {
                            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "UpArm BW Button Step[" + -1 * m_nStep + "]");
                            lblUpArmEncoder.Text = (int.Parse(lblUpArmEncoder.Text) + (-1 * m_nStep)).ToString();
                        }
                        break;
                }
                EnableControlButton(true);
                EnableProcedureButton(true);
            }
        }
        //  Jog 完成按鈕解鎖
        private void _robot_OnJobFunctionCompleted(object sender, bool bSuc)
        {
            I_Robot trb = sender as I_Robot;
            trb.OnJobFunctionCompleted -= _robot_OnJobFunctionCompleted;//Done
            EnableControlButton(true);
            EnableProcedureButton(true);
        }
        //  Jog step的量更換
        private void rbStep_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton btn = (RadioButton)sender;

            if (false == btn.Checked) return;

            int nValue = 0;
            bool bsuc = int.TryParse(btn.Text.ToString(), out nValue);// Other 會失敗

            tbStep.Enabled = bsuc == false;//選擇Other

            switch (nValue)
            {
                case 0:

                    if (int.TryParse(tbStep.Text, out m_nStep))// tbStep填正常的
                    {

                    }
                    else
                    {
                        tbStep.Text = "2000";
                        m_nStep = 2000;
                    }

                    break;
                case 10:
                    m_nStep = 10;
                    break;
                case 50:
                    m_nStep = 50;
                    break;
                case 100:
                    m_nStep = 100;
                    break;
                case 500:
                    m_nStep = 500;
                    break;
                case 1000:
                    m_nStep = 1000;
                    break;
                case 5000:
                    m_nStep = 5000;
                    break;
                case 10000:
                    m_nStep = 10000;
                    break;
                case 50000:
                    m_nStep = 50000;
                    break;
            }
        }
        private void tbStep_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(tbStep.Text, out m_nStep))// tbStep填正常的
            {

            }
            else
            {
                tbStep.Text = "2000";
                m_nStep = 2000;
            }
        }

        #region 教導流程

        private enum eTeachStep { Prepare = 0, TopHome, TopExtd, TopTeach, BottomHome, BottomExtd, BottomTeach, End };
        eTeachStep m_eStep;

        private void EnableProcedureButton(bool bAct)
        {
            btnCancel.Enabled = bAct;
            btnNext.Enabled = bAct;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            EnableProcedureButton(false);
            switch (m_eStep)
            {
                case eTeachStep.Prepare:
                    DoPrepareTask();
                    break;
                case eTeachStep.TopHome:
                    DoTopHomeTask();
                    break;
                case eTeachStep.TopExtd:
                    DoTopExtdTask(true);
                    break;
                case eTeachStep.TopTeach:
                    DoTopTeachTask();
                    break;
                case eTeachStep.BottomHome:
                    DoBottomHomeTask();
                    break;
                case eTeachStep.BottomExtd:
                    DoBottomExtdTask(true);
                    break;
                case eTeachStep.BottomTeach:
                    DoBottomTeachTask();
                    break;
                case eTeachStep.End:
                    ProcessEnd();
                    break;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            EnableProcedureButton(false);
            switch (m_eStep)
            {
                case eTeachStep.Prepare:
                    SkipPrepareTask();
                    break;
                case eTeachStep.TopHome:
                    SkipTopHomeTask();
                    break;
                case eTeachStep.TopExtd:
                    DoTopExtdTask(false);
                    break;
                case eTeachStep.TopTeach:
                    SkipTeachTask();
                    break;
                case eTeachStep.BottomHome:
                    SkipBottomHomeTask();
                    break;
                case eTeachStep.BottomExtd:
                    DoBottomExtdTask(false);
                    break;
                case eTeachStep.BottomTeach:
                    SkipTeachTask();
                    break;
                case eTeachStep.End:
                    ProcessEnd();
                    break;
            }
        }

        private void ProcessEnd()
        {
            EnableControlButton(false);
            ChangeButtun(btnRobotWmap, true);
            ChangeButtun(btnTeachArm1, true);
            ChangeButtun(btnTeachArm2, true);
            ChangeButtun(btnSave, true);
            tabControl1.SelectedTab = tabPage1;
            delegateMDILock?.Invoke(false);
        }
        //  step1
        private void DoPrepareTask()
        {
            rtbInstruct.Clear();
            rtbInstruct.AppendText("Excuting Stage Clamp,Please Wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Excuting Stage Clamp,Please Wait.\r"));
            Cursor.Current = Cursors.WaitCursor;
            int nStgIndx = -1;
            switch (m_SelectUnit)
            {
                case enumRbtAddress.STG1_08:
                case enumRbtAddress.STG1_12:
                    nStgIndx = 0;
                    break;
                case enumRbtAddress.STG2_08:
                case enumRbtAddress.STG2_12:
                    nStgIndx = 1;
                    break;
                case enumRbtAddress.STG3_08:
                case enumRbtAddress.STG3_12:
                    nStgIndx = 2;
                    break;
                case enumRbtAddress.STG4_08:
                case enumRbtAddress.STG4_12:
                    nStgIndx = 3;
                    break;
                case enumRbtAddress.STG5_08:
                case enumRbtAddress.STG5_12:
                    nStgIndx = 4;
                    break;
                case enumRbtAddress.STG6_08:
                case enumRbtAddress.STG6_12:
                    nStgIndx = 5;
                    break;
                case enumRbtAddress.STG7_08:
                case enumRbtAddress.STG7_12:
                    nStgIndx = 6;
                    break;
                case enumRbtAddress.STG8_08:
                case enumRbtAddress.STG8_12:
                    nStgIndx = 7;
                    break;
            }
            if (nStgIndx != -1)
            {
                ListSTG[nStgIndx].OnJigDockComplete -= FinishLoadportOnDock;
                ListSTG[nStgIndx].OnJigDockComplete += FinishLoadportOnDock;
                ListSTG[nStgIndx].JigDock();
            }
        }
        private void SkipPrepareTask()
        {
            if (new frmMessageBox(GParam.theInst.GetLanguage("Abort teaching ? pos:") + cbxStage.Text, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }
            ProcessEnd();
        }
        //step2
        private void DoTopHomeTask()
        {
            string strX = "", strR = "", strZ_slotTop = "";
            if (m_robot.DMPRData != null)
            {
                strX = m_robot.DMPRData[21];
                strR = m_robot.DMPRData[23];
                strZ_slotTop = m_robot.DMPRData[8];
            }

            string str1 = GParam.theInst.GetLanguage("Are you sure moving robot to standby top position of ");
            string str2 = string.Format(" {0}{1} {2}{3}", GParam.theInst.GetLanguage("stage:"), _nCurrStage, GParam.theInst.GetLanguage("name:"), _strSelectStgName);
            string str3 = string.Format("\r\n(X:{0} R:{1} Z:{2})", strX, strR, strZ_slotTop);
            if (new frmMessageBox(str1 + str2 + str3, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }
            Cursor.Current = Cursors.WaitCursor;
            if (!RobotTeachStart)
                RobotTeachStart = true;

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Move to previous teaching position.Please Wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Move to previous teaching position.Please Wait.\r"));
            m_robot.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                //safety
                if (robotManual.GPIO.DO_LowerArmOrigin == false)
                {
                    robotManual.ResetInPos();
                    robotManual.LowerArm.AbsolutePosW(m_robot.GetAckTimeout, 0);
                    robotManual.WaitInPos(30000);
                }
                if (robotManual.GPIO.DO_UpperArmOrigin == false)
                {
                    robotManual.ResetInPos();
                    robotManual.UpperArm.AbsolutePosW(m_robot.GetAckTimeout, 0);
                    robotManual.WaitInPos(30000);
                }

                //x
                if (robotManual.XaxsDisable == false)
                {
                    robotManual.ResetInPos();
                    robotManual.Traverse.AbsolutePosW(m_robot.GetAckTimeout, int.Parse(strX)/*Decimal.ToInt32(nudXAxisArm.Value)*/);
                    robotManual.WaitInPos(30000);
                }

                //r
                robotManual.ResetInPos();
                robotManual.Rotater.AbsolutePosW(m_robot.GetAckTimeout, int.Parse(strR));
                robotManual.WaitInPos(30000);

                //z
                robotManual.ResetInPos();
                robotManual.Lifter.AbsolutePosW(m_robot.GetAckTimeout, int.Parse(strZ_slotTop));
                robotManual.WaitInPos(30000);

                robotManual.UpperArm.GetPos();
                robotManual.LowerArm.GetPos();
                robotManual.Lifter.GetPos();
                robotManual.Rotater.GetPos();
                if (robotManual.XaxsDisable == false) robotManual.Traverse.GetPos();
            };
            m_robot.OnManualCompleted += FinishRobotTopHome;
            m_robot.StartManualFunction();
        }
        private void SkipTopHomeTask()
        {
            Cursor.Current = Cursors.WaitCursor;

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Top Teach\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Top Teach\r"));
            rtbInstruct.AppendText("Use below button to excuting teaching\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Use below button to excuting teaching\r"));
            rtbInstruct.AppendText("If complete teaching , press 'Next' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If complete teaching , press 'Next' button\r"));
            rtbInstruct.AppendText("If cancel teaching , press 'Cancel' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If cancel teaching , press 'Cancel' button\r"));

            pnHome.BackColor = GParam.theInst.ColorReadyGreen;
            pnExtd.BackColor = GParam.theInst.ColorReadyGreen;
            pnTeach.BackColor = GParam.theInst.ColorWaitYellow;

            EnableControlButton(true);
            EnableProcedureButton(true);
            m_eStep = eTeachStep.TopTeach;
        }
        //step3
        private void DoTopExtdTask(bool bMotion)
        {
            if (bMotion)
            {
                string strArm = bSelectUpArm ? "UpArm" : "LowArm";

                int nPulse = 0;
                if (m_robot.DMPRData != null)
                    nPulse = int.Parse(m_robot.DMPRData[24]);


                string str1 = GParam.theInst.GetLanguage("Are you sure robot to stage ");
                string str2 = GParam.theInst.GetLanguage(" and Extd ");

                if (new frmMessageBox(string.Format("{0}[{1}]{2}{3}:{4}?", str1, cbxStage.Text, str2, strArm, nPulse), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
                {
                    EnableProcedureButton(true);
                    return;
                }

                Cursor.Current = Cursors.WaitCursor;

                if (!RobotTeachStart)
                    RobotTeachStart = true;

                rtbInstruct.Clear();
                rtbInstruct.AppendText("Please wait a moment with arm extended.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Please wait a moment with arm extended.\r"));

                m_robot.DoManualProcessing += (object Manual) =>
                {
                    I_Robot robotManual = Manual as I_Robot;

                    if (bSelectUpArm == false)
                    {
                        //arm
                        robotManual.ResetInPos();
                        robotManual.LowerArm.AbsolutePosW(m_robot.GetAckTimeout, nPulse);
                        robotManual.WaitInPos(30000);
                    }
                    else
                    {
                        //arm
                        robotManual.ResetInPos();
                        robotManual.UpperArm.AbsolutePosW(m_robot.GetAckTimeout, nPulse);
                        robotManual.WaitInPos(30000);
                    }

                    robotManual.UpperArm.GetPos();
                    robotManual.LowerArm.GetPos();
                    robotManual.Lifter.GetPos();
                    robotManual.Rotater.GetPos();
                    if (robotManual.XaxsDisable == false) robotManual.Traverse.GetPos();

                };
                m_robot.OnManualCompleted += FinishRobotTopExtd;
                m_robot.StartManualFunction();
            }
            else
            {
                FinishRobotTopExtd(this, true);
            }
        }
        //step4
        private void DoTopTeachTask()
        {
            if (new frmMessageBox("Are you sure complete teaching ?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }

            EnableControlButton(false);

            Cursor.Current = Cursors.WaitCursor;

            nudLastSlot.Value = int.Parse(lblLEncoder.Text);
            nudZAxisTopArm.Value = int.Parse(lblLEncoder.Text) + 3000;
            nudXAxisArm.Value = int.Parse(lblTrackEncoder.Text);
            nudRotArm.Value = int.Parse(lblREncoder.Text);

            if (bSelectUpArm == false)
            {
                nudArm.Value = int.Parse(lblLowArmEncoder.Text);
            }
            else
            {
                nudArm.Value = int.Parse(lblUpArmEncoder.Text);
            }

            pnHome.BackColor = GParam.theInst.ColorReadyGreen;
            pnExtd.BackColor = GParam.theInst.ColorReadyGreen;
            pnTeach.BackColor = GParam.theInst.ColorWaitYellow;

            rtbInstruct.Clear();

            rtbInstruct.AppendText("Bottom Teach\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Bottom Teach\r"));
            rtbInstruct.AppendText("Use below button to excuting teaching\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Use below button to excuting teaching\r"));
            rtbInstruct.AppendText("If complete teaching , press 'Next' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If complete teaching , press 'Next' button\r"));
            rtbInstruct.AppendText("If cancel teaching , press 'Cancel' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If cancel teaching , press 'Cancel' button\r"));

            EnableControlButton(false);
            EnableProcedureButton(true);
            m_eStep = eTeachStep.BottomHome;
        }

        //step5
        private void DoBottomHomeTask()
        {
            string strZ_slotBottom = "";
            if (m_robot.DMPRData != null)
            {
                strZ_slotBottom = m_robot.DMPRData[6];
            }

            string str1 = GParam.theInst.GetLanguage("Are you sure moving robot to standby bottom position of ");
            string str2 = string.Format(" {0}{1} {2}{3}", GParam.theInst.GetLanguage("stage:"), _nCurrStage, GParam.theInst.GetLanguage("name:"), _strSelectStgName);
            string str3 = string.Format("\r\n(Z:{0})", strZ_slotBottom);
            if (new frmMessageBox(str1 + str2, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }
            Cursor.Current = Cursors.WaitCursor;
            if (!RobotTeachStart)
                RobotTeachStart = true;

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Move to previous teaching position.Please Wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Move to previous teaching position.Please Wait.\r"));


            m_robot.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                if (bSelectUpArm == false)
                {
                    //arm
                    robotManual.ResetInPos();
                    robotManual.LowerArm.AbsolutePosW(m_robot.GetAckTimeout, 0);
                    robotManual.WaitInPos(30000);
                }
                else
                {
                    //arm
                    robotManual.ResetInPos();
                    robotManual.UpperArm.AbsolutePosW(m_robot.GetAckTimeout, 0);
                    robotManual.WaitInPos(30000);
                }
                //safety
                if (robotManual.GPIO.DO_LowerArmOrigin == false)
                {
                    robotManual.ResetInPos();
                    robotManual.LowerArm.AbsolutePosW(m_robot.GetAckTimeout, 0);
                    robotManual.WaitInPos(30000);
                }
                if (robotManual.GPIO.DO_UpperArmOrigin == false)
                {
                    robotManual.ResetInPos();
                    robotManual.UpperArm.AbsolutePosW(m_robot.GetAckTimeout, 0);
                    robotManual.WaitInPos(30000);
                }

                //z
                robotManual.ResetInPos();
                robotManual.Lifter.AbsolutePosW(m_robot.GetAckTimeout, int.Parse(strZ_slotBottom) /*Decimal.ToInt32(nudFirstSlot.Value)*/);
                robotManual.WaitInPos(30000);

                robotManual.UpperArm.GetPos();
                robotManual.LowerArm.GetPos();
                robotManual.Lifter.GetPos();
                robotManual.Rotater.GetPos();
                if (robotManual.XaxsDisable == false) robotManual.Traverse.GetPos();
            };
            m_robot.OnManualCompleted += FinishRobotBottomHome;
            m_robot.StartManualFunction();
        }
        private void SkipBottomHomeTask()
        {
            Cursor.Current = Cursors.WaitCursor;

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Bottom Teach\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Top Teach\r"));
            rtbInstruct.AppendText("Use below button to excuting teaching\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Use below button to excuting teaching\r"));
            rtbInstruct.AppendText("If complete teaching , press 'Next' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If complete teaching , press 'Next' button\r"));
            rtbInstruct.AppendText("If cancel teaching , press 'Cancel' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If cancel teaching , press 'Cancel' button\r"));

            pnHome.BackColor = GParam.theInst.ColorReadyGreen;
            pnExtd.BackColor = GParam.theInst.ColorReadyGreen;
            pnTeach.BackColor = GParam.theInst.ColorWaitYellow;

            EnableControlButton(true);
            EnableProcedureButton(true);
            m_eStep = eTeachStep.BottomTeach;
        }
        //step6
        private void DoBottomExtdTask(bool bMotion)
        {
            if (bMotion)
            {
                string strArm = bSelectUpArm ? "UpArm" : "LowArm";

                int nPulse = (int)nudArm.Value;

                string str1 = GParam.theInst.GetLanguage("Are you sure robot to stage ");
                string str2 = GParam.theInst.GetLanguage(" and Extd ");

                if (new frmMessageBox(string.Format("{0}[{1}]{2}{3}:{4}?", str1, cbxStage.Text, str2, strArm, nPulse), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
                {
                    EnableProcedureButton(true);
                    return;
                }

                Cursor.Current = Cursors.WaitCursor;

                if (!RobotTeachStart)
                    RobotTeachStart = true;

                rtbInstruct.Clear();
                rtbInstruct.AppendText("Please wait a moment with arm extended.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Please wait a moment with arm extended.\r"));

                m_robot.DoManualProcessing += (object Manual) =>
                {
                    I_Robot robotManual = Manual as I_Robot;

                    if (bSelectUpArm == false)
                    {
                        //arm
                        robotManual.ResetInPos();
                        robotManual.LowerArm.AbsolutePosW(m_robot.GetAckTimeout, nPulse);
                        robotManual.WaitInPos(30000);
                    }
                    else
                    {
                        //arm
                        robotManual.ResetInPos();
                        robotManual.UpperArm.AbsolutePosW(m_robot.GetAckTimeout, nPulse);
                        robotManual.WaitInPos(30000);
                    }

                    robotManual.UpperArm.GetPos();
                    robotManual.LowerArm.GetPos();
                    robotManual.Lifter.GetPos();
                    robotManual.Rotater.GetPos();
                    if (robotManual.XaxsDisable == false) robotManual.Traverse.GetPos();
                };
                m_robot.OnManualCompleted += FinishRobotBottomExtd;
                m_robot.StartManualFunction();
            }
            else
            {
                FinishRobotBottomExtd(this, true);
            }
        }
        //step7
        private void DoBottomTeachTask()
        {
            if (new frmMessageBox("Are you sure complete teaching ?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }

            EnableControlButton(false);

            Cursor.Current = Cursors.WaitCursor;

            if (bSelectUpArm == false)
            {
                nudFirstSlot.Value = int.Parse(lblLEncoder.Text);
                nudZAxisBottomArm.Value = int.Parse(lblLEncoder.Text) - 3000;
            }
            else
            {
                nudFirstSlot.Value = int.Parse(lblLEncoder.Text);
                nudZAxisBottomArm.Value = int.Parse(lblLEncoder.Text) - 3000;
            }

            DoOrgn();
            pnTeach.BackColor = GParam.theInst.ColorReadyGreen;
            pnFinish.BackColor = GParam.theInst.ColorWaitYellow;
            rtbInstruct.Clear();
            rtbInstruct.AppendText("That ends the teaching process.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("That ends the teaching process.\r"));
            rtbInstruct.AppendText("Excuting Origin,Please wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Excuting Origin,Please wait.\r"));
            m_eStep = eTeachStep.End;
        }
        //step8
        private void SkipTeachTask()
        {
            if (new frmMessageBox("Parameter will not be recorded. Are you sure you want to give up the teaching?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }

            DoOrgn();

            pnTeach.BackColor = GParam.theInst.ColorReadyGreen;
            pnFinish.BackColor = GParam.theInst.ColorWaitYellow;

            rtbInstruct.Clear();
            rtbInstruct.AppendText("That ends the teaching process.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("That ends the teaching process.\r"));
            rtbInstruct.AppendText("Excuting Origin,Please wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Excuting Origin,Please wait.\r"));
            m_eStep = eTeachStep.End;
        }

        //  Robot Origin
        private void DoOrgn()
        {
            EnableProcedureButton(false);

            if (!RobotTeachStart)
                RobotTeachStart = true;

            m_robot.ResetInPos();
            m_robot.MoveToStandbyPosW(m_robot.GetAckTimeout);
            m_robot.WaitInPos(m_robot.GetMotionTimeout);

            if (!m_robot.ExtXaxisDisable)
            {
                m_robot.TBL_560.ResetInPos();
                RobPos pos = GParam.theInst.DicRobPos[enumPosition.HOME];
                m_robot.TBL_560.AxisMabsW(m_robot.GetAckTimeout, m_eXAX1, pos.Pos_ARM1);
                m_robot.TBL_560.WaitInPos(m_robot.GetMotionTimeout);
            }

            frmOrgn _frmOrgn = new frmOrgn(m_robotList, ListSTG, m_alignerList, m_bufList, m_listEQM, GParam.theInst.IsSimulate);
            bool bSucc = (DialogResult.OK == _frmOrgn.ShowDialog());

            m_robot.ResetProcessCompleted();
            m_robot.SspdW(m_robot.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(m_robot.BodyNo - 1));//進入就降到ModeSpeed
            m_robot.WaitProcessCompleted(m_robot.GetAckTimeout);

            rtbInstruct.Clear();

            if (m_eStep == eTeachStep.Prepare)
            {
                rtbInstruct.AppendText("The teaching process to begin, please place the jig\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("The teaching process to begin, please place the jig\r"));
                rtbInstruct.AppendText("Please put jig complete,press [Next] Button\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Please put jig complete,press [Next] Button\r"));
                rtbInstruct.AppendText("If you need cancel teaching mode,press [Cancel] Button.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If you need cancel teaching mode,press [Cancel] Button.\r"));
            }

            Cursor.Current = Cursors.Default;
            EnableControlButton(false);
            EnableProcedureButton(true);
        }

        //  Loadport Clmp Done
        private void FinishLoadportOnDock(object sender, LoadPortEventArgs e)
        {
            I_Loadport stg = sender as I_Loadport;

            stg.OnJigDockComplete -= FinishLoadportOnDock;
            if (e.Succeed)
            {
                pnPrepare.BackColor = GParam.theInst.ColorReadyGreen;
                pnHome.BackColor = GParam.theInst.ColorWaitYellow;
                rtbInstruct.Clear();
                rtbInstruct.AppendText("Move to previous teaching position？\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Move to previous teaching position？\r"));
                rtbInstruct.AppendText("If need move,press [Next] Button\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If need move,press [Next] Button\r"));
                rtbInstruct.AppendText("If need skip this step,press [Cancel] Button\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If need skip this step,press [Cancel] Button\r"));
                Cursor.Current = Cursors.Default;
                m_eStep = eTeachStep.TopHome;
            }

            EnableProcedureButton(true);
        }
        //  Robot Home Done
        private void FinishRobotTopHome(object sender, bool bSuc)
        {
            m_robot.OnManualCompleted -= FinishRobotTopHome;
            if (bSuc)
            {
                pnHome.BackColor = GParam.theInst.ColorReadyGreen;
                pnExtd.BackColor = GParam.theInst.ColorWaitYellow;
                rtbInstruct.Clear();
                rtbInstruct.AppendText("Does the arm extend directly?\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Does the arm extend directly?\r"));
                rtbInstruct.AppendText("If need move,press [Next] Button\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If need move,press [Next] Button\r"));
                rtbInstruct.AppendText("If need skip this step,press [Cancel] Button\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If need skip this step,press [Cancel] Button\r"));
                Cursor.Current = Cursors.Default;
                m_eStep = eTeachStep.TopExtd;
            }
            EnableProcedureButton(true);
        }
        //  Robot Extd Done
        private void FinishRobotTopExtd(object sender, bool bSuc)
        {
            m_robot.OnManualCompleted -= FinishRobotTopExtd;
            rtbInstruct.Clear();
            rtbInstruct.AppendText("Use below button to excuting teaching\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Use below button to excuting teaching\r"));
            rtbInstruct.AppendText("If complete teaching , press 'Next' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If complete teaching , press 'Next' button\r"));
            rtbInstruct.AppendText("If cancel teaching , press 'Cancel' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If cancel teaching , press 'Cancel' button\r"));
            Cursor.Current = Cursors.Default;

            pnExtd.BackColor = GParam.theInst.ColorReadyGreen;
            pnTeach.BackColor = GParam.theInst.ColorWaitYellow;

            EnableControlButton(true);
            EnableProcedureButton(true);
            m_eStep = eTeachStep.TopTeach;
        }
        //  Robot Home Done
        private void FinishRobotBottomHome(object sender, bool bSuc)
        {
            m_robot.OnManualCompleted -= FinishRobotBottomHome;
            if (bSuc)
            {
                pnHome.BackColor = GParam.theInst.ColorReadyGreen;
                pnExtd.BackColor = GParam.theInst.ColorWaitYellow;
                rtbInstruct.Clear();
                rtbInstruct.AppendText("Does the arm extend directly?\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Does the arm extend directly?\r"));
                rtbInstruct.AppendText("If need move,press [Next] Button\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If need move,press [Next] Button\r"));
                rtbInstruct.AppendText("If need skip this step,press [Cancel] Button\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If need skip this step,press [Cancel] Button\r"));
                Cursor.Current = Cursors.Default;
                m_eStep = eTeachStep.BottomExtd;
            }
            EnableProcedureButton(true);
        }
        //  Robot Extd Done
        private void FinishRobotBottomExtd(object sender, bool bSuc)
        {
            m_robot.OnManualCompleted -= FinishRobotBottomExtd;
            rtbInstruct.Clear();
            rtbInstruct.AppendText("Use below button to excuting teaching\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Use below button to excuting teaching\r"));
            rtbInstruct.AppendText("If complete teaching , press 'Next' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If complete teaching , press 'Next' button\r"));
            rtbInstruct.AppendText("If cancel teaching , press 'Cancel' button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If cancel teaching , press 'Cancel' button\r"));
            Cursor.Current = Cursors.Default;

            pnExtd.BackColor = GParam.theInst.ColorReadyGreen;
            pnTeach.BackColor = GParam.theInst.ColorWaitYellow;

            EnableControlButton(true);
            EnableProcedureButton(true);
            m_eStep = eTeachStep.BottomTeach;
        }
        #endregion

        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion

        private void tlpDTRB_Paint(object sender, PaintEventArgs e)
        {

        }


        private I_Loadport RbtAddressConvertLpObject(enumRbtAddress eRbtAddress)
        {
            switch (eRbtAddress)
            {
                case enumRbtAddress.STG1_12:
                case enumRbtAddress.STG1_08:
                    return ListSTG[0];
                case enumRbtAddress.STG2_12:
                case enumRbtAddress.STG2_08:
                    return ListSTG[1];
                case enumRbtAddress.STG3_12:
                case enumRbtAddress.STG3_08:
                    return ListSTG[2];
                case enumRbtAddress.STG4_12:
                case enumRbtAddress.STG4_08:
                    return ListSTG[3];
                case enumRbtAddress.STG5_12:
                case enumRbtAddress.STG5_08:
                    return ListSTG[4];
                case enumRbtAddress.STG6_12:
                case enumRbtAddress.STG6_08:
                    return ListSTG[5];
                case enumRbtAddress.STG7_12:
                case enumRbtAddress.STG7_08:
                    return ListSTG[6];
                case enumRbtAddress.STG8_12:
                case enumRbtAddress.STG8_08:
                    return ListSTG[7];
                default:
                    return null;
            }
        }

        private void frmTeachRobotMapping_Load(object sender, EventArgs e)
        {

        }
    }
}
