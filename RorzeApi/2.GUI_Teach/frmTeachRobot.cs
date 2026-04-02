using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RorzeUnit.Interface;
using RorzeUnit.Class;
using RorzeComm.Log;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Class.BarCode;
using RorzeUnit.Class.Loadport.Event;
using Rorze.Equipments.Unit;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.Robot;
using static Rorze.Equipments.Unit.SRobot;
using RorzeUnit.Class.RC500.RCEnum;
using static RorzeUnit.Class.SWafer;
using System.Runtime.CompilerServices;

namespace RorzeApi
{
    public partial class frmTeachRobot : Form
    {
        private int _nCurrStage = 0;        //  Stage number of Robot
        private RobPos _nCurPos;            //  使用ini紀錄Pos時使用
        private int m_nStep = 1000;         //  Jog Step   
        private float frmX;                 //  Zoom 放入窗體的寬度
        private float frmY;                 //  Zoom 放入窗體的高度
        private bool isLoaded = false;      //  Zoom 是否已設定各控制的尺寸資料到Tag屬性
        private bool bSelectUpArm;          //  Teaching Arm
        private bool m_bSimulate;
        private bool _bRobotTeachStart;
        public bool RobotTeachStart { get { return _bRobotTeachStart; } set { _bRobotTeachStart = value; } }

        private string _strSelectStgName;


        private enumRbtAddress m_SelectUnit;
        private enumPosition m_SelectIniUnit;
        private enumRC550Axis m_eXAX1 = enumRC550Axis.AXS1;

        private Class.SPermission _userManager;   //  管理LOGIN使用者權限
        private string _strUserName;//登入者名稱
        private bool m_bIsRunMode = false;
        private SProcessDB _accessDBlog;
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[TeachingForm] : {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        private I_Robot m_robot;
        private List<I_Robot> m_listTRB;
        private List<I_RC5X0_Motion> m_listTBL;
        private List<I_Loadport> m_listSTG;
        private List<I_Aligner> m_listALN;
        private List<I_Buffer> m_listBUF;
        private List<SSEquipment> m_listEQM;

        private List<Button> m_btnSelectRobotList = new List<Button>();

        public frmTeachRobot(
            List<I_Robot> listTRB, List<I_RC5X0_Motion> listTBL, List<I_Loadport> listSTG, List<I_Aligner> listALN,
            List<I_Buffer> listBUF,
            SProcessDB db, Class.SPermission userManager, bool bIsRunMode, List<SSEquipment> listEQM)
        {
            InitializeComponent();
            this.Size = new Size(970/*840*/, 718/*700*/);

            //  消失頁籤
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);

            m_listTRB = listTRB;
            m_listTBL = listTBL;
            m_listSTG = listSTG;
            m_listALN = listALN;
            m_listBUF = listBUF;

            m_listEQM = listEQM;
            m_bSimulate = GParam.theInst.IsSimulate;
            m_bIsRunMode = bIsRunMode;
            _accessDBlog = db;
            _userManager = userManager;

            #region Select Robot Button
            tlpSelectRobot.RowStyles.Clear();
            tlpSelectRobot.ColumnStyles.Clear();
            tlpSelectRobot.RowCount = 1;
            tlpSelectRobot.ColumnCount = m_listTRB.Count;
            tlpSelectRobot.Dock = DockStyle.Fill;
            for (int i = 0; i < m_listTRB.Count; i++)//移除第二支
            {
                if (m_listTRB[i].Disable) continue;
                if (GParam.theInst.GetRobot_AllowPort(m_listTRB[i].BodyNo - 1).Contains('1') == false) continue;
                tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                Button btn = new Button();
                btn.Font = new Font("微軟正黑體", 18, FontStyle.Bold);
                btn.Text = GParam.theInst.GetLanguage("Robot" + (char)(64 + m_listTRB[i].BodyNo));
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Click += btnSelectRobot_Click;
                m_btnSelectRobotList.Add(btn);
                tlpSelectRobot.Controls.Add(btn, m_btnSelectRobotList.Count - 1, 0);
            }
            tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            #endregion

            //  顯示8"&12"的名稱，使用者按下Teaching要檢查adapter
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_listTRB[0].BodyNo).ToArray())//先寫死第一支Robot
            {


                switch (item.strDefineName)
                {
                    case enumRbtAddress.ALEX: cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG1_12: if (m_listSTG[0] != null && m_listSTG[0].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG2_12: if (m_listSTG[1] != null && m_listSTG[1].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG3_12: if (m_listSTG[2] != null && m_listSTG[2].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG4_12: if (m_listSTG[3] != null && m_listSTG[3].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG5_12: if (m_listSTG[4] != null && m_listSTG[4].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG6_12: if (m_listSTG[5] != null && m_listSTG[5].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG7_12: if (m_listSTG[6] != null && m_listSTG[6].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG8_12: if (m_listSTG[7] != null && m_listSTG[7].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.ALN1: if (m_listALN[0] != null && m_listALN[0].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.ALN2: if (m_listALN[1] != null && m_listALN[1].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.BUF1: if (m_listBUF[0] != null && m_listBUF[0].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.BUF2: if (m_listBUF[1] != null && m_listBUF[1].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.EQM1: if (m_listEQM[0] != null && m_listEQM[0].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.EQM2: if (m_listEQM[1] != null && m_listEQM[1].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.EQM3: if (m_listEQM[2] != null && m_listEQM[2].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.EQM4: if (m_listEQM[3] != null && m_listEQM[3].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.AOI: cbxStage.Items.Add(item.strDisplayName); break;
                        //case enumRbtAddress.STG1_08:
                        //    if (m_listSTG[0].Disable == false && m_listSTG[0].UseAdapter)
                        //        cbxStage.Items.Add(item.strDisplayName);
                        //    break;
                        //case enumRbtAddress.STG2_08:
                        //    if (m_listSTG[1].Disable == false && m_listSTG[1].UseAdapter)
                        //        cbxStage.Items.Add(item.strDisplayName);
                        //    break;
                        //case enumRbtAddress.STG3_08:
                        //    if (m_listSTG[2].Disable == false && m_listSTG[2].UseAdapter)
                        //        cbxStage.Items.Add(item.strDisplayName);
                        //    break;
                        //case enumRbtAddress.STG4_08:
                        //    if (m_listSTG[3].Disable == false && m_listSTG[3].UseAdapter)
                        //        cbxStage.Items.Add(item.strDisplayName);
                        //    break;
                        //case enumRbtAddress.STG5_08:
                        //    if (m_listSTG[4].Disable == false && m_listSTG[4].UseAdapter)
                        //        cbxStage.Items.Add(item.strDisplayName);
                        //    break;
                        //case enumRbtAddress.STG6_08:
                        //    if (m_listSTG[5].Disable == false && m_listSTG[5].UseAdapter)
                        //        cbxStage.Items.Add(item.strDisplayName);
                        //    break;
                        //case enumRbtAddress.STG7_08:
                        //    if (m_listSTG[6].Disable == false && m_listSTG[6].UseAdapter)
                        //        cbxStage.Items.Add(item.strDisplayName);
                        //    break;
                        //case enumRbtAddress.STG8_08:
                        //    if (m_listSTG[7].Disable == false && m_listSTG[7].UseAdapter)
                        //        cbxStage.Items.Add(item.strDisplayName);
                        //    break;
                }
            }
            if (cbxStage.Items.Count > 0) cbxStage.SelectedIndex = 0;

            if (GParam.theInst.FreeStyle)
            {
                btnSave.Image = RorzeApi.Properties.Resources._48_save_;
                btnNext.Image = RorzeApi.Properties.Resources._32_next_;
                btnCancel.Image = RorzeApi.Properties.Resources._32_cancel_;

                btnRbArmFW.Image = btnRbZUp.Image = RorzeApi.Properties.Resources.Teachtop_;
                btnRbArmBW.Image = btnRbZDown.Image = RorzeApi.Properties.Resources.Teachdown_;
                btnRbRotFW.Image = RorzeApi.Properties.Resources.TeachCCW_;
                btnRbRotBW.Image = RorzeApi.Properties.Resources.TeachCW_;
                btnExtXFW.Image = RorzeApi.Properties.Resources.Teachforw_;
                btnExtXBW.Image = RorzeApi.Properties.Resources.Teachback_;
            }
        }

        private void btnSelectRobot_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "frmTeachRobot", _strUserName, "Select Robot", btn.Name);
            cbxStage.SelectedIndex = -1;
            cbxType.SelectedIndex = -1;
            cbxType.Enabled = false;
            cbxJIGType.SelectedIndex = -1;
            cbxJIGType.Enabled = false;

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
                        m_robot = m_listTRB[0]; //as SSRobotRR75x;
                    }
                    else if (strName2 == btn.Text)
                    {
                        m_robot = m_listTRB[1]; //as SSRobotRR75x;
                    }
                    else
                        switch (btn.Text)
                        {
                            case "RobotA":
                            case "Robot A":
                                m_robot = m_listTRB[0]; break; //as SSRobotRR75x; break;
                            case "RobotB":
                            case "Robot B":
                                m_robot = m_listTRB[1]; break; //as SSRobotRR75x; break;
                            default:
                                m_robot = null; break;
                        }

                    if (m_robot == null)
                    {
                        new frmMessageBox("Robot isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }

                    if (m_robot.XaxsDisable)
                    {
                        nudXUpArmPos.Visible = lblXUpArmPos.Visible = false;
                        nudXLowArmPos.Visible = lblXLowArmPos.Visible = false;
                        gbRobotXControl.Visible = false;

                        lblTrackEncoder.Visible = lblXasis.Visible = lblXaxisUnits.Visible = false;
                    }
                    else
                    {
                        nudXUpArmPos.Visible = lblXUpArmPos.Visible = true;
                        nudXLowArmPos.Visible = lblXLowArmPos.Visible = true;
                        gbRobotXControl.Visible = true;

                        lblTrackEncoder.Visible = lblXasis.Visible = lblXaxisUnits.Visible = true;
                    }

                    continue;
                }
                else
                {
                    m_btnSelectRobotList[i].BackColor = System.Drawing.SystemColors.ControlLight;
                }
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
                lblExtXEncoder.Text = m_robot.TBL_560.GetPulse(m_eXAX1).ToString();
                if (m_robot.XaxsDisable == false) lblTrackEncoder.Text = m_robot.Traverse.Position.ToString();
            }
        }

        //  啟用 Teaching 畫面
        private void frmTeachRobot_VisibleChanged(object sender, EventArgs e)
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
                        m_btnSelectRobotList[0].PerformClick();
                }
                else
                {
                    foreach (I_Robot item in m_listTRB)
                    {
                        if (item.Disable) continue;

                        item.ResetProcessCompleted();
                        if (m_bIsRunMode == true)
                        {
                            item.SspdW(item.GetAckTimeout, GParam.theInst.GetRobot_RunSpeed(item.BodyNo - 1));//  回原速度
                            if (item.ExtXaxisDisable == false)
                            {
                                if (!m_listTBL[0].Disable)
                                {
                                    item.TBL_560.ResetProcessCompleted();
                                    item.TBL_560.SspdW(item.GetAckTimeout, GParam.theInst.GetRobot_RunSpeed(item.BodyNo - 1));
                                    item.TBL_560.WaitProcessCompleted(item.GetAckTimeout);
                                }
                            }
                        }
                        else
                        {
                            item.SspdW(item.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(item.BodyNo - 1));//  回原速度
                            if (item.ExtXaxisDisable == false)
                            {
                                if (!m_listTBL[0].Disable)
                                {
                                    item.TBL_560.ResetProcessCompleted();
                                    item.TBL_560.SspdW(item.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(item.BodyNo - 1));
                                    item.TBL_560.WaitProcessCompleted(item.GetAckTimeout);
                                }
                            }
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
        private void cboStage_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cbxType.SelectedIndex = -1;
            cbxType.Enabled = false;
            cbxJIGType.SelectedIndex = -1;
            cbxJIGType.Enabled = false;
            if (cbxStage.SelectedItem == null) return;
            _strSelectStgName = cbxStage.SelectedItem.ToString();
            enumPosition enuStage = enumPosition.UnKnow;

            int nStage = -1;
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_robot.BodyNo).ToArray())//搜尋所有點位
            {
                if (item.strDisplayName == _strSelectStgName)//注意是顯示名稱相同
                {
                    //  找到使用者選擇的點位
                    m_SelectUnit = item.strDefineName;
                    //  選的是Loadport
                    if (item.strDefineName.ToString().Contains("STG"))
                    {
                        cbxType.Items.Clear();
                        cbxType.Enabled = true;
                        cbxType.SelectedIndex = -1;
                        cbxJIGType.Enabled = true;
                        cbxJIGType.SelectedIndex = -1;
                    }
                    //  判斷選擇位置對應到Robot的地址 nStage
                    switch (item.strDefineName)
                    {
                        case enumRbtAddress.STG1_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[0].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[0].eFoupType;
                            break;
                        case enumRbtAddress.STG1_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[0].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[0].eFoupType;
                            enuStage = enumPosition.Loader1;
                            break;
                        case enumRbtAddress.STG2_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[1].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[1].eFoupType;
                            break;
                        case enumRbtAddress.STG2_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[1].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[1].eFoupType;
                            enuStage = enumPosition.Loader2;
                            break;
                        case enumRbtAddress.STG3_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[2].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[2].eFoupType;
                            break;
                        case enumRbtAddress.STG3_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[2].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[2].eFoupType;
                            enuStage = enumPosition.Loader3;
                            break;
                        case enumRbtAddress.STG4_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[3].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[3].eFoupType;
                            break;
                        case enumRbtAddress.STG4_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[3].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[3].eFoupType;
                            enuStage = enumPosition.Loader4;
                            break;
                        case enumRbtAddress.STG5_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[4].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[4].eFoupType;
                            break;
                        case enumRbtAddress.STG5_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[4].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[4].eFoupType;
                            enuStage = enumPosition.Loader5;
                            break;
                        case enumRbtAddress.STG6_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[5].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[5].eFoupType;
                            break;
                        case enumRbtAddress.STG6_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[5].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[5].eFoupType;
                            enuStage = enumPosition.Loader6;
                            break;
                        case enumRbtAddress.STG7_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[6].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[6].eFoupType;
                            break;
                        case enumRbtAddress.STG7_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[6].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[6].eFoupType;
                            enuStage = enumPosition.Loader7;
                            break;
                        case enumRbtAddress.STG8_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[7].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[7].eFoupType;
                            break;
                        case enumRbtAddress.STG8_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[7].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[7].eFoupType;
                            enuStage = enumPosition.Loader8;
                            break;
                        case enumRbtAddress.ALEX:
                            enuStage = enumPosition.ALEX;
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.BarCode:
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.ALN1:
                            enuStage = enumPosition.AlignerA;
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.ALN2:
                            enuStage = enumPosition.AlignerB;
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.BUF1:
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.BUF2:
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.EQM1:
                            enuStage = enumPosition.EQM1;
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.EQM2:
                            enuStage = enumPosition.EQM2;
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.EQM3:
                            enuStage = enumPosition.EQM3;
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.EQM4:
                            enuStage = enumPosition.EQM4;
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                        case enumRbtAddress.AOI:
                            enuStage = enumPosition.AOI;
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName);
                            break;
                    }
                    break;//成功找到位置
                }
            }
            if (nStage == -1) return;
            if (enuStage == enumPosition.UnKnow) return;
            if (enuStage != enumPosition.AOI)
            {
                nudAOIMoveDist.Visible = false;
            }
            else
            {
                nudAOIMoveDist.Visible = true;
            }
                m_SelectIniUnit = enuStage;
            _nCurrStage = nStage;
            ShowStageTeachingData(_nCurrStage);
            _robot_GetTeachDataFromIni();
        }
        //  選擇  FOUP TYPE
        private void cboType_SelectionChangeCommitted(object sender, EventArgs e)
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
                case enumRbtAddress.ALEX:
                case enumRbtAddress.BarCode:
                case enumRbtAddress.ALN1:
                case enumRbtAddress.ALN2:
                case enumRbtAddress.BUF1:
                case enumRbtAddress.BUF2:
                    _nCurrStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, m_SelectUnit);
                    break;
            }

            if (_nCurrStage != -1)
                ShowStageTeachingData(_nCurrStage);
        }
        //  Robot 執行 ExeGetTeachData 完成後觸發事件
        private void _robot_OnGetTeachDataCompleted(object sender, bool bSuc)
        {
            I_Robot trb = sender as I_Robot;
            trb.OnGetTeachDataCompleted -= _robot_OnGetTeachDataCompleted;//Done

            //if (m_bSimulate == false)
            {
                bool bEnableTwoStepLoad = false;
                if (m_robot.DEQUData != null && m_robot.DEQUData.Count() > 9)//TwoStepLoad DEQU[8] bit4
                {
                    int nSoftwareSwitch = int.Parse(m_robot.DEQUData[8]);
                    if ((nSoftwareSwitch & 0x08) == 0x08)//bit3
                    {
                        bEnableTwoStepLoad = true;
                    }
                }
                cbxArmSlightBackwardMovement.SelectedIndex = bEnableTwoStepLoad ? 1 : 0;


                nudXUpArmPos.Value = int.Parse(m_robot.DTRBData[0]);
                nudXLowArmPos.Value = int.Parse(m_robot.DTRBData[0]) + int.Parse(m_robot.DTRBData[1]);

                nudZUpArmPos.Value = int.Parse(m_robot.DTRBData[2]);
                nudZLowArmPos.Value = int.Parse(m_robot.DTRBData[2]) + int.Parse(m_robot.DTRBData[3]);

                nudRUpArmPos.Value = int.Parse(m_robot.DTRBData[4]);
                nudRLowArmPos.Value = int.Parse(m_robot.DTRBData[4]) + int.Parse(m_robot.DTRBData[5]);

                nudUpArmPos.Value = int.Parse(m_robot.DTRBData[6]);
                nudLowArmPos.Value = int.Parse(m_robot.DTRBData[6]) + int.Parse(m_robot.DTRBData[7]);

                nudClampOffset.Value = int.Parse(m_robot.DTRBData[9]);
                nudPickUp.Value = int.Parse(m_robot.DTRBData[10]);
                nudSlotPitch.Value = int.Parse(m_robot.DTRBData[11]);
                nudSlotNum.Value = int.Parse(m_robot.DTRBData[12]);

                nudTwoStepDtrb.Value = int.Parse(m_robot.DTRBData[15]);

                nudVacOnOH.Value = int.Parse(m_robot.DTRBData[17]);
                nudVacOnOS.Value = int.Parse(m_robot.DTRBData[18]);
                nudVacOnTimer.Value = int.Parse(m_robot.DTRBData[19]);
                nudVacOffOH.Value = int.Parse(m_robot.DTRBData[20]);
                nudVacOffOS.Value = int.Parse(m_robot.DTRBData[21]);
                nudVacOffTimer.Value = int.Parse(m_robot.DTRBData[22]);

                //nudExtZUpArmPos.Value = int.Parse(m_robot.DTRBData[23]);
                //nudExtZDownArmPos.Value = int.Parse(m_robot.DTRBData[24]);

                //方法一
                //nudUnldOffset.Value = int.Parse(m_robot.DTULData[6]) - int.Parse(m_robot.DTRBData[6]);
                //方案二
                nudUnldOffset.Value = int.Parse(m_robot.DTULData[7]);

                nudRetryArmSlightMovingQuantityDtul.Value = int.Parse(m_robot.DTULData[9]);//二段取片往後拉的距離，-5000往後拉5mm

                nudTwoStepDtul.Value = int.Parse(m_robot.DTULData[15]);//對應edge clamp
                nudTwoStepMoveQuantityDtul.Value = int.Parse(m_robot.DTULData[16]);//對應edge clamp


                int nValue = int.Parse(m_robot.DCFGData[5]);

                //if ((nValue & 0x0c) == 0x0c)//手冊DCFG Data Flag bit2&3對應 P2=8,P2=9
                if ((nValue & 0x300) == 0x300)//bit8 bit9    &0x0c
                    cbxInterpolationFunction.SelectedIndex = 1;
                else
                    cbxInterpolationFunction.SelectedIndex = 0;

                nudLoadOffset_UpArm.Value = int.Parse(m_robot.DCFGData[6]);//對應edge clamp
                nudLoadOffset_LowArm.Value = int.Parse(m_robot.DCFGData[7]);//對應edge clamp
                nudnIterpolationDegree.Value = int.Parse(m_robot.DCFGData[8]);
                nudnIterpolationDistence.Value = int.Parse(m_robot.DCFGData[9]);
            }

            ChangeButtun(btnTeachArm1, true);
            ChangeButtun(btnTeachArm2, true);
            ChangeButtun(btnSave, true);

        }
        //  Robot 執行 ExeSetTeachData 完成後觸發事件
        private void _robot_OnSetTeachDataCompleted(object sender, bool bSuc)
        {
            I_Robot trb = sender as I_Robot;
            trb.OnSetTeachDataCompleted -= _robot_OnSetTeachDataCompleted;//Done

            ShowStageTeachingData(_nCurrStage);

            if (RobotTeachStart)
            {
                RobotTeachStart = false;
            }

            m_robot.UpperArm.GetPos();
            m_robot.LowerArm.GetPos();
            m_robot.Rotater.GetPos();
            m_robot.Lifter.GetPos();
            //m_robot.Lifter2.GetPos();
            if (m_robot.XaxsDisable == false) m_robot.Traverse.GetPos();

            //ChangeButtun(btnSave, true);
            EnablePage1Button(true);
        }

        // Robot 讀取ini目前Teaching Data
        private void _robot_GetTeachDataFromIni()
        {

            ChangeButtun(btnTeachArm1, false);
            ChangeButtun(btnSave, false);

            _nCurPos = GParam.theInst.DicRobPos[m_SelectIniUnit];

            #region 更新顯示

            nudExtXUpArmPos.Value = _nCurPos.Pos_ARM1;
            nudExtXDownArmPos.Value = _nCurPos.Pos_ARM2;
            nudAOIMoveDist.Value = _nCurPos.Pos_AOIMovingDist;

            #endregion

            ChangeButtun(btnTeachArm1, true);
            ChangeButtun(btnSave, true);
        }
        // Robot 寫入ini目前Teaching Data
        private void _robot_SetTeachDataToIni()
        {
            _nCurPos = GParam.theInst.DicRobPos[m_SelectIniUnit];

            #region 設定現在參數

            _nCurPos.Pos_ARM1 = Convert.ToInt32(nudExtXUpArmPos.Value);

            _nCurPos.Pos_ARM2 = Convert.ToInt32(nudExtXDownArmPos.Value);

            _nCurPos.Pos_AOIMovingDist = Convert.ToInt32(nudAOIMoveDist.Value);

            #endregion
            GParam.theInst.SetDicRobPos(m_SelectIniUnit, _nCurPos);

        }

        //  清除介面顯示資料
        private void DataSetEnableControlButton(bool bAct)
        {
            if (bAct == false)
            {
                nudXUpArmPos.Value = 0;
                nudXLowArmPos.Value = 0;
                nudZUpArmPos.Value = 0;
                nudZLowArmPos.Value = 0;
                nudRUpArmPos.Value = 0;
                nudRLowArmPos.Value = 0;
                nudUpArmPos.Value = 0;
                nudLowArmPos.Value = 0;
                nudExtXUpArmPos.Value = 0;
                nudExtXDownArmPos.Value = 0;

                nudPickUp.Value = 7000;
                nudSlotPitch.Value = 10000;
                nudSlotNum.Value = 25;
                nudUnldOffset.Value = 1000;

                nudAOIMoveDist.Value = 0;
            }
        }
        //  叫 robot 取得資料
        private void ShowStageTeachingData(int nStage)
        {
            DataSetEnableControlButton(_nCurrStage != -1);
            if (m_robot == null || nStage <= 0) return;
            m_robot.OnGetTeachDataCompleted -= _robot_OnGetTeachDataCompleted;//Get
            m_robot.OnGetTeachDataCompleted += _robot_OnGetTeachDataCompleted;//Get
            m_robot.GetTeachData(nStage);
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
            if (m_bSimulate)
            {
                lblTrackEncoder.Text = "10000";
                lblLEncoder.Text = "10000";
                lblREncoder.Text = "20000";
                lblUpArmEncoder.Text = "30000";
                lblLowArmEncoder.Text = "30000";
                lblExtXEncoder.Text = "30000";
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
            if (cbxType.SelectedIndex == -1 || cbxJIGType.SelectedIndex == -1)// 沒有選擇Type
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
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_08) + 3)
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

            if (_nCurrStage == GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.ALN1) || _nCurrStage == GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.ALN2))
            {
                gpAligner.Visible = true;

                I_Aligner aln = m_listALN[0];
                if (_nCurrStage == GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.ALN2))
                {
                    aln = m_listALN[1];
                }
                if (GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                {
                    btnCCW.Visible = true;
                    btnCW.Visible = true;
                }
            }
            else if (_nCurrStage == GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.BarCode))
            {
                gpAligner.Visible = false;

            }
            else
            {
                gpAligner.Visible = false;
            }


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
            m_robot.TBL_560.GetPulse(m_eXAX1, true);
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
            _robot_SetTeachDataToIni();

            WriteLog($"Save Teaching Data: " +
                    $"\n Upper Arm = {_nCurPos.Pos_ARM1}" +
                    $"\n Lower Arm = {_nCurPos.Pos_ARM2}"
                    );

            bool bCheckLoadport = false;

            if (cbxType.SelectedIndex == -1 /*|| cboJIGType.SelectedIndex == -1*/)// 沒有選擇Type
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
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG1_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG2_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG3_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG4_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG5_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG6_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG7_08) + 3)
                 || (_nCurrStage >= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_08) && _nCurrStage <= GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.STG8_08) + 3)
                 )
                {
                    new frmMessageBox("Please select Foup type or Jig type", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                #endregion
            }
            else
                bCheckLoadport = true;

            EnablePage1Button(false);

            if (m_robot.Connected && m_bSimulate == false)
            {
                if (m_robot.DEQUData != null && m_robot.DEQUData.Count() > 9)//TwoStepLoad DEQU[8] bit4
                {
                    int nSoftwareSwitch = int.Parse(m_robot.DEQUData[8]);

                    bool bON = cbxArmSlightBackwardMovement.SelectedIndex != 0;

                    int nBit = 0x01 << 3;
                    if (bON)
                        nSoftwareSwitch = (nSoftwareSwitch | nBit);
                    else
                        nSoftwareSwitch = (nSoftwareSwitch & ~nBit);

                    m_robot.DEQUData[8] = nSoftwareSwitch.ToString();
                }

                m_robot.DTRBData[0] = nudXUpArmPos.Value.ToString();
                m_robot.DTRBData[1] = (nudXLowArmPos.Value - nudXUpArmPos.Value).ToString();
                m_robot.DTRBData[2] = nudZUpArmPos.Value.ToString();
                m_robot.DTRBData[3] = (nudZLowArmPos.Value - nudZUpArmPos.Value).ToString();
                m_robot.DTRBData[4] = nudRUpArmPos.Value.ToString();
                m_robot.DTRBData[5] = (nudRLowArmPos.Value - nudRUpArmPos.Value).ToString();
                m_robot.DTRBData[6] = nudUpArmPos.Value.ToString();
                m_robot.DTRBData[7] = (nudLowArmPos.Value - nudUpArmPos.Value).ToString();

                //m_robot.DTRBData[23] = nudExtZUpArmPos.Value.ToString();
                //m_robot.DTRBData[24] = nudExtZDownArmPos.Value.ToString();

                m_robot.DTRBData[9] = nudClampOffset.Value.ToString();//對應edge clamp
                m_robot.DTRBData[10] = nudPickUp.Value.ToString();
                m_robot.DTRBData[11] = nudSlotPitch.Value.ToString();
                m_robot.DTRBData[12] = nudSlotNum.Value.ToString();

                m_robot.DTRBData[15] = nudTwoStepDtrb.Value.ToString();

                m_robot.DTRBData[17] = nudVacOnOH.Value.ToString();
                m_robot.DTRBData[18] = nudVacOnOS.Value.ToString();
                m_robot.DTRBData[19] = nudVacOnTimer.Value.ToString();
                m_robot.DTRBData[20] = nudVacOffOH.Value.ToString();
                m_robot.DTRBData[21] = nudVacOffOS.Value.ToString();
                m_robot.DTRBData[22] = nudVacOffTimer.Value.ToString();

                //方案一
                //m_robot.DTULData[6] = (nudUnldOffset.Value + nudUpArmPos.Value).ToString();
                //方案二
                m_robot.DTULData[6] = "0";
                m_robot.DTULData[7] = nudUnldOffset.Value.ToString();

                m_robot.DTULData[9] = nudRetryArmSlightMovingQuantityDtul.Value.ToString();

                m_robot.DTULData[15] = nudTwoStepDtul.Value.ToString();//對應edge clamp
                m_robot.DTULData[16] = nudTwoStepMoveQuantityDtul.Value.ToString();//對應edge clamp

                int nValue = int.Parse(m_robot.DCFGData[5]);//取出原本的D值
                //int nV = 0x01 << 8 | 0x01 << 9;//要改的位置
                int nV = 0x01 << 2 | 0x01 << 3;//手冊DCFG Data Flag bit2&3對應 P2=8,P2=9
                if (cbxInterpolationFunction.SelectedIndex != 0)
                    nValue = (nValue | nV);
                else
                    nValue = (nValue & ~nV);

                m_robot.DCFGData[5] = nValue.ToString();
                m_robot.DCFGData[6] = nudLoadOffset_UpArm.Value.ToString();//對應edge clamp
                m_robot.DCFGData[7] = nudLoadOffset_LowArm.Value.ToString();//對應edge clamp
                m_robot.DCFGData[8] = nudnIterpolationDegree.Value.ToString();
                m_robot.DCFGData[9] = nudnIterpolationDistence.Value.ToString();
            }
            m_robot.OnSetTeachDataCompleted -= _robot_OnSetTeachDataCompleted;//Save
            m_robot.OnSetTeachDataCompleted += _robot_OnSetTeachDataCompleted;//Save
            m_robot.SetTeachData(_nCurrStage);

            #region _nCurrStage如果是Loadport則要多存位置檢測變形量位置
            if (bCheckLoadport == true)
            {
                //if (m_robot.Connected && m_bSimulate == false)
                //{
                //    m_robot.DTRBData[2] = (nudZUpArmPos.Value - GParam.theInst.GetArmDeformationPos(0)).ToString();
                //    m_robot.DTRBData[3] = ((nudZLowArmPos.Value - nudZUpArmPos.Value) - GParam.theInst.GetArmDeformationPos(1)).ToString();
                //    m_robot.DTRBData[6] = "0";
                //    m_robot.DTRBData[7] = "0";

                //    m_robot.DTRBData[9] = nudClampOffset.Value.ToString();//對應edge clamp
                //    m_robot.DTRBData[10] = "0";
                //}
                //m_robot.SetTeachData(_nCurrStage + 100);
            }
            #endregion
        }
        private void EnablePage1Button(bool bAct)
        {
            gbPosition.Enabled = bAct;
            tlpDTRB.Enabled = bAct;
            //ChangeButtun(btnSave, bAct);
            delegateMDILock?.Invoke(!bAct);
        }

        //  =====================================================================================================================
        private delegate void UpdateButtunUI(Button MsgBox, bool ret);
        public void ChangeButtun(Button MsgBox, bool ret)
        {
            if (InvokeRequired)
            {
                UpdateButtunUI dlg = new UpdateButtunUI(ChangeButtun);
                this.BeginInvoke(dlg, MsgBox, ret);
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
            ChangeButtun(btnRbXFW, bAct);
            ChangeButtun(btnRbXBW, bAct);
            ChangeButtun(btnExtXBW, bAct);
            ChangeButtun(btnExtXFW, bAct);
            ChangeButtun(btnCLMP, bAct);
            ChangeButtun(brnUCLM, bAct);

            ChangeButtun(btnAlignerClmp, bAct);
            ChangeButtun(btnAlignerUnClmp, bAct);

            ChangeButtun(btnCW, bAct);
            ChangeButtun(btnCCW, bAct);
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
                if (btn == btnRbXBW)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Y Axis FW Button Step[" + m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.Xax, m_nStep);
                }
                else if (btn == btnRbXFW)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Y Axis BW Button Step[" + -1 * m_nStep + "]");
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
                else if (btn == btnExtXFW)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Ext X Axis FW Button Step[" + m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.ExtX, m_nStep);
                }
                else if (btn == btnExtXBW)
                {
                    _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Ext X Axis BW Button Step[" + -1 * m_nStep + "]");
                    m_robot.STEP(enumRobotAxis.ExtX, -1 * m_nStep);
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
                        lblLEncoder.Text = (int.Parse(lblLEncoder.Text) + (-1 * m_nStep)).ToString();
                        break;
                    case "btnExtXFW":
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Ext X Axis FW Button Step[" + m_nStep + "]");
                        lblExtXEncoder.Text = (int.Parse(lblExtXEncoder.Text) + (m_nStep)).ToString();
                        break;
                    case "btnExtXBW":
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Ext X Axis BW Button Step[" + -1 * m_nStep + "]");
                        lblExtXEncoder.Text = (int.Parse(lblExtXEncoder.Text) + (-1 * m_nStep)).ToString();
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

        private enum eTeachStep { Prepare = 0, Home, Extd, Teach, End };
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
                case eTeachStep.Home:
                    //DoHomeTask();
                    DoXRZExtdTask();
                    break;
                case eTeachStep.Extd:
                    DoExtdTask(true);
                    break;
                case eTeachStep.Teach:
                    DoTeachTask();
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
                case eTeachStep.Home:
                    SkipHomeTask();
                    break;
                case eTeachStep.Extd:
                    DoExtdTask(false);
                    break;
                case eTeachStep.Teach:
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
            switch (cbxJIGType.SelectedIndex)
            {
                case 0://用治具
                    switch (m_SelectUnit)
                    {
                        case enumRbtAddress.STG1_08:
                        case enumRbtAddress.STG1_12:
                            m_listSTG[0].OnJigDockComplete -= FinishLoadportOnDock;
                            m_listSTG[0].OnJigDockComplete += FinishLoadportOnDock;
                            m_listSTG[0].JigDock();
                            break;
                        case enumRbtAddress.STG2_08:
                        case enumRbtAddress.STG2_12:
                            m_listSTG[1].OnJigDockComplete -= FinishLoadportOnDock;
                            m_listSTG[1].OnJigDockComplete += FinishLoadportOnDock;
                            m_listSTG[1].JigDock();
                            break;
                        case enumRbtAddress.STG3_08:
                        case enumRbtAddress.STG3_12:
                            m_listSTG[2].OnJigDockComplete -= FinishLoadportOnDock;
                            m_listSTG[2].OnJigDockComplete += FinishLoadportOnDock;
                            m_listSTG[2].JigDock();
                            break;
                        case enumRbtAddress.STG4_08:
                        case enumRbtAddress.STG4_12:
                            m_listSTG[3].OnJigDockComplete -= FinishLoadportOnDock;
                            m_listSTG[3].OnJigDockComplete += FinishLoadportOnDock;
                            m_listSTG[3].JigDock();
                            break;
                        case enumRbtAddress.STG5_08:
                        case enumRbtAddress.STG5_12:
                            m_listSTG[4].OnJigDockComplete -= FinishLoadportOnDock;
                            m_listSTG[4].OnJigDockComplete += FinishLoadportOnDock;
                            m_listSTG[4].JigDock();
                            break;
                        case enumRbtAddress.STG6_08:
                        case enumRbtAddress.STG6_12:
                            m_listSTG[5].OnJigDockComplete -= FinishLoadportOnDock;
                            m_listSTG[5].OnJigDockComplete += FinishLoadportOnDock;
                            m_listSTG[5].JigDock();
                            break;
                        case enumRbtAddress.STG7_08:
                        case enumRbtAddress.STG7_12:
                            m_listSTG[6].OnJigDockComplete -= FinishLoadportOnDock;
                            m_listSTG[6].OnJigDockComplete += FinishLoadportOnDock;
                            m_listSTG[6].JigDock();
                            break;
                        case enumRbtAddress.STG8_08:
                        case enumRbtAddress.STG8_12:
                            m_listSTG[7].OnJigDockComplete -= FinishLoadportOnDock;
                            m_listSTG[7].OnJigDockComplete += FinishLoadportOnDock;
                            m_listSTG[7].JigDock();
                            break;
                    }
                    break;
                case 1:
                    switch (m_SelectUnit)
                    {
                        case enumRbtAddress.STG1_08:
                        case enumRbtAddress.STG1_12:
                            m_listSTG[0].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[0].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[0].CLMP();
                            break;
                        case enumRbtAddress.STG2_08:
                        case enumRbtAddress.STG2_12:
                            m_listSTG[1].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[1].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[1].CLMP();
                            break;
                        case enumRbtAddress.STG3_08:
                        case enumRbtAddress.STG3_12:
                            m_listSTG[2].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[2].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[2].CLMP();
                            break;
                        case enumRbtAddress.STG4_08:
                        case enumRbtAddress.STG4_12:
                            m_listSTG[3].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[3].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[3].CLMP();
                            break;
                        case enumRbtAddress.STG5_08:
                        case enumRbtAddress.STG5_12:
                            m_listSTG[4].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[4].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[4].CLMP();
                            break;
                        case enumRbtAddress.STG6_08:
                        case enumRbtAddress.STG6_12:
                            m_listSTG[5].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[5].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[5].CLMP();
                            break;
                        case enumRbtAddress.STG7_08:
                        case enumRbtAddress.STG7_12:
                            m_listSTG[6].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[6].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[6].CLMP();
                            break;
                        case enumRbtAddress.STG8_08:
                        case enumRbtAddress.STG8_12:
                            m_listSTG[7].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[7].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[7].CLMP();
                            break;                      
                    }
                    break;
                default:
                    FinishLoadportOnDock(this, new LoadPortEventArgs("", 0, true));
                    break;
            }

            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_robot.BodyNo).ToArray())//搜尋EQ點位
            {
                if (item.strDisplayName == _strSelectStgName)//注意是顯示名稱相同
                {
                    //  找到使用者選擇的點位
                    m_SelectUnit = item.strDefineName;
                    switch (item.strDefineName)
                    {
                        case enumRbtAddress.EQM1:
                            m_listEQM[0].OnSutterDoorOpenComplete -= FinishShutterDoorOnOpen;
                            m_listEQM[0].OnSutterDoorOpenComplete += FinishShutterDoorOnOpen;
                            m_listEQM[0].tShutterDoorOpenSetW();
                            break;
                        case enumRbtAddress.EQM2:
                            m_listEQM[1].OnSutterDoorOpenComplete -= FinishShutterDoorOnOpen;
                            m_listEQM[1].OnSutterDoorOpenComplete += FinishShutterDoorOnOpen;
                            m_listEQM[1].tShutterDoorOpenSetW();
                            break;
                        case enumRbtAddress.EQM3:
                            m_listEQM[2].OnSutterDoorOpenComplete -= FinishShutterDoorOnOpen;
                            m_listEQM[2].OnSutterDoorOpenComplete += FinishShutterDoorOnOpen;
                            m_listEQM[2].tShutterDoorOpenSetW();
                            break;
                        case enumRbtAddress.EQM4:
                            m_listEQM[3].OnSutterDoorOpenComplete -= FinishShutterDoorOnOpen;
                            m_listEQM[3].OnSutterDoorOpenComplete += FinishShutterDoorOnOpen;
                            m_listEQM[3].tShutterDoorOpenSetW();
                            break;
                    }
                    break;
                }
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

        private void DoXRZExtdTask()
        {
            string str1 = GParam.theInst.GetLanguage("Are you sure moving robot to standby position of ");
            string str2 = string.Format(" {0}{1} {2}{3}", GParam.theInst.GetLanguage("stage:"), _nCurrStage, GParam.theInst.GetLanguage("name:"), _strSelectStgName);
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

                robotManual.ResetInPos();
                // robotManual.MoveToStandbyPosW(m_robot.GetAckTimeout, false, enumRobotArms.LowerArm, _nCurrStage, 1);;
                if (bSelectUpArm == false) // 下ARM
                {
                    //X axis
                    if (robotManual.XaxsDisable == false)
                    {
                        robotManual.ResetInPos();
                        robotManual.Traverse.AbsolutePosW(robotManual.GetAckTimeout, int.Parse(m_robot.DTRBData[0]) + int.Parse(m_robot.DTRBData[1]));
                        robotManual.WaitInPos(robotManual.GetMotionTimeout);
                    }

                    //Ext X axis
                    robotManual.TBL_560.ResetInPos();
                    robotManual.TBL_560.AxisMabsW(robotManual.GetAckTimeout, m_eXAX1, _nCurPos.Pos_ARM2);
                    robotManual.TBL_560.WaitInPos(robotManual.GetMotionTimeout);

                    //Z axis
                    robotManual.ResetInPos();
                    robotManual.Lifter.AbsolutePosW(robotManual.GetAckTimeout, int.Parse(m_robot.DTRBData[2]) + int.Parse(m_robot.DTRBData[3]));
                    robotManual.WaitInPos(robotManual.GetMotionTimeout);

                    ////Ext Z axis
                    //robotManual.ResetInPos();
                    //robotManual.Lifter2.AbsolutePosW(robotManual.GetAckTimeout, int.Parse(m_robot.DTRBData[24]));
                    //robotManual.WaitInPos(robotManual.GetMotionTimeout);

                    //R axis7
                    robotManual.ResetInPos();
                    robotManual.Rotater.AbsolutePosW(robotManual.GetAckTimeout, int.Parse(m_robot.DTRBData[4]) + int.Parse(m_robot.DTRBData[5]));
                    robotManual.WaitInPos(robotManual.GetMotionTimeout);
                }
                else // 上ARM
                {
                    //X axis
                    if (robotManual.XaxsDisable == false)
                    {
                        robotManual.ResetInPos();
                        robotManual.Traverse.AbsolutePosW(robotManual.GetAckTimeout, int.Parse(m_robot.DTRBData[0]));
                        robotManual.WaitInPos(robotManual.GetMotionTimeout);
                    }

                    //Ext X axis
                    robotManual.TBL_560.ResetInPos();
                    robotManual.TBL_560.AxisMabsW(robotManual.GetAckTimeout, m_eXAX1, _nCurPos.Pos_ARM1);
                    robotManual.TBL_560.WaitInPos(robotManual.GetMotionTimeout);

                    //Z axis
                    robotManual.ResetInPos();
                    robotManual.Lifter.AbsolutePosW(robotManual.GetAckTimeout, int.Parse(m_robot.DTRBData[2]));
                    robotManual.WaitInPos(robotManual.GetMotionTimeout);

                    ////Ext Z axis
                    //robotManual.ResetInPos();
                    //robotManual.Lifter2.AbsolutePosW(robotManual.GetAckTimeout, int.Parse(m_robot.DTRBData[23]));
                    //robotManual.WaitInPos(robotManual.GetMotionTimeout);

                    //R axis
                    robotManual.ResetInPos();
                    robotManual.Rotater.AbsolutePosW(robotManual.GetAckTimeout, int.Parse(m_robot.DTRBData[4]));
                    robotManual.WaitInPos(robotManual.GetMotionTimeout);
                }

                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                robotManual.UpperArm.GetPos();
                robotManual.LowerArm.GetPos();
                robotManual.Lifter.GetPos();
                //robotManual.Lifter2.GetPos();
                robotManual.Rotater.GetPos();
                robotManual.TBL_560.GetPulse(m_eXAX1);

                if (robotManual.XaxsDisable == false) robotManual.Traverse.GetPos();
            };
            m_robot.OnManualCompleted += FinishRobotHome;
            m_robot.StartManualFunction();
        }

        //  step2
        private void DoHomeTask()
        {
            string str1 = GParam.theInst.GetLanguage("Are you sure moving robot to standby position of ");
            string str2 = string.Format(" {0}{1} {2}{3}", GParam.theInst.GetLanguage("stage:"), _nCurrStage, GParam.theInst.GetLanguage("name:"), _strSelectStgName);
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

                robotManual.ResetInPos();

                if (bSelectUpArm == false)
                    robotManual.MoveToStandbyPosW(m_robot.GetAckTimeout, false, enumRobotArms.LowerArm, _nCurrStage, 1);
                else
                    robotManual.MoveToStandbyPosW(m_robot.GetAckTimeout, false, enumRobotArms.UpperArm, _nCurrStage, 1);

                robotManual.WaitInPos(80000);

                robotManual.UpperArm.GetPos();
                robotManual.LowerArm.GetPos();
                robotManual.Lifter.GetPos();
                robotManual.Rotater.GetPos();
                if (robotManual.XaxsDisable == false) robotManual.Traverse.GetPos();
            };
            m_robot.OnManualCompleted += FinishRobotHome;
            m_robot.StartManualFunction();
        }
        private void SkipHomeTask()
        {
            Cursor.Current = Cursors.WaitCursor;

            rtbInstruct.Clear();
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
            m_eStep = eTeachStep.Teach;
        }
        //  step3
        private void DoExtdTask(bool bMotion)
        {

            if (bMotion)
            {
                string strArm = bSelectUpArm ? "UpArm" : "LowArm";

                int nPulse = 0;
                if (m_robot.DTRBData != null)
                    nPulse = bSelectUpArm ? int.Parse(m_robot.DTRBData[6]) : int.Parse(m_robot.DTRBData[6]) + int.Parse(m_robot.DTRBData[7]);

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
                        /*robotManual.ResetInPos();
                        robotManual.MoveToStandbyPosW(m_robot.GetAckTimeout, false, enumRobotArms.LowerArm, _nCurrStage, 1);
                        robotManual.WaitInPos(30000);*/

                        //arm
                        robotManual.ResetInPos();
                        robotManual.LowerArm.AbsolutePosW(m_robot.GetAckTimeout, int.Parse(m_robot.DTRBData[6]) + int.Parse(m_robot.DTRBData[7]));
                        robotManual.WaitInPos(30000);
                    }
                    else
                    {
                        /*robotManual.ResetInPos();
                        robotManual.MoveToStandbyPosW(m_robot.GetAckTimeout, false, enumRobotArms.UpperArm, _nCurrStage, 1);
                        robotManual.WaitInPos(30000);*/

                        //arm
                        robotManual.ResetInPos();
                        robotManual.UpperArm.AbsolutePosW(m_robot.GetAckTimeout, int.Parse(m_robot.DTRBData[6]));
                        robotManual.WaitInPos(30000);
                    }

                    robotManual.UpperArm.GetPos();
                    robotManual.LowerArm.GetPos();
                    robotManual.Lifter.GetPos();
                    //robotManual.Lifter2.GetPos();
                    robotManual.Rotater.GetPos();
                    robotManual.TBL_560.GetPulse(m_eXAX1);
                    if (robotManual.XaxsDisable == false) robotManual.Traverse.GetPos();
                };
                m_robot.OnManualCompleted += FinishRobotExtd;
                m_robot.StartManualFunction();
            }
            else
            {
                FinishRobotExtd(this, true);
            }


        }
        //  step4
        private void DoTeachTask()
        {
            if (new frmMessageBox("Are you sure complete teaching ?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }

            I_Aligner aln = m_listALN[0];
            if (_nCurrStage == GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.ALN2))
            {
                aln = m_listALN[1];
            }
            if (GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
            {
                if (new frmMessageBox("Are you sure you want to set this position to the 0 degree position of the turntable?", "SURE", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    aln.GposRW(3000); //  問位置
                    GParam.theInst.SetTurnTable_angle_0(aln.BodyNo - 1, int.Parse(aln.Raxispos.ToString())); //設定0度
                    GParam.theInst.SetTurnTable_angle_180(aln.BodyNo - 1, int.Parse(aln.Raxispos.ToString()) + 180000); //設定180度
                }
            }

            EnableControlButton(false);

            Cursor.Current = Cursors.WaitCursor;

            if (bSelectUpArm)
            {
                nudXUpArmPos.Value = int.Parse(lblTrackEncoder.Text);
                nudZUpArmPos.Value = int.Parse(lblLEncoder.Text);
                nudRUpArmPos.Value = int.Parse(lblREncoder.Text);
                nudUpArmPos.Value = int.Parse(lblUpArmEncoder.Text);
                nudExtXUpArmPos.Value = int.Parse(lblExtXEncoder.Text);
            }
            else
            {
                nudXLowArmPos.Value = int.Parse(lblTrackEncoder.Text);
                nudZLowArmPos.Value = int.Parse(lblLEncoder.Text);
                nudRLowArmPos.Value = int.Parse(lblREncoder.Text);
                nudLowArmPos.Value = int.Parse(lblLowArmEncoder.Text);
                nudExtXDownArmPos.Value = int.Parse(lblExtXEncoder.Text);
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

            //Home
            m_robot.ResetInPos();
            m_robot.MoveToStandbyPosW(m_robot.GetAckTimeout);
            m_robot.WaitInPos(m_robot.GetMotionTimeout);

            if (!m_robot.ExtXaxisDisable)
            {
                m_robot.TBL_560.ResetInPos();
                RobPos pos = GParam.theInst.DicRobPos[enumPosition.HOME];
                m_robot.TBL_560.AxisMabsW(m_robot.GetAckTimeout, enumRC550Axis.AXS1, pos.Pos_ARM1);
                m_robot.TBL_560.WaitInPos(m_robot.GetMotionTimeout);
            }

            frmOrgn _frmOrgn = new frmOrgn(m_listTRB, m_listSTG, m_listALN, m_listBUF, m_listEQM, GParam.theInst.IsSimulate);
            bool bSucc = (DialogResult.OK == _frmOrgn.ShowDialog());

            m_robot.ResetProcessCompleted();
            m_robot.SspdW(m_robot.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(m_robot.BodyNo - 1));//進入就降到ModeSpeed
            m_robot.WaitProcessCompleted(m_robot.GetAckTimeout);

            if (m_robot is SSRobotRR75x && m_robot.ExtXaxisDisable == false)
            {
                m_robot.TBL_560.ResetProcessCompleted();
                m_robot.TBL_560.SspdW(m_robot.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(m_robot.BodyNo - 1));//進入就降到ModeSpeed
                m_robot.TBL_560.WaitProcessCompleted(m_robot.GetAckTimeout);
                m_robot.TBL_560.GetPulse(m_eXAX1, true);
            }

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
            switch (cbxJIGType.SelectedIndex)
            {
                case 0:
                    switch (m_SelectUnit)
                    {
                        case enumRbtAddress.STG1_08:
                        case enumRbtAddress.STG1_12:
                            m_listSTG[0].OnJigDockComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG2_08:
                        case enumRbtAddress.STG2_12:
                            m_listSTG[1].OnJigDockComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG3_08:
                        case enumRbtAddress.STG3_12:
                            m_listSTG[2].OnJigDockComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG4_08:
                        case enumRbtAddress.STG4_12:
                            m_listSTG[3].OnJigDockComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG5_08:
                        case enumRbtAddress.STG5_12:
                            m_listSTG[4].OnJigDockComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG6_08:
                        case enumRbtAddress.STG6_12:
                            m_listSTG[5].OnJigDockComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG7_08:
                        case enumRbtAddress.STG7_12:
                            m_listSTG[6].OnJigDockComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG8_08:
                        case enumRbtAddress.STG8_12:
                            m_listSTG[7].OnJigDockComplete -= FinishLoadportOnDock;
                            break;
                    }
                    break;
                case 1:
                    switch (m_SelectUnit)
                    {
                        case enumRbtAddress.STG1_08:
                        case enumRbtAddress.STG1_12:
                            m_listSTG[0].OnClmpComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG2_08:
                        case enumRbtAddress.STG2_12:
                            m_listSTG[1].OnClmpComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG3_08:
                        case enumRbtAddress.STG3_12:
                            m_listSTG[2].OnClmpComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG4_08:
                        case enumRbtAddress.STG4_12:
                            m_listSTG[3].OnClmpComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG5_08:
                        case enumRbtAddress.STG5_12:
                            m_listSTG[4].OnClmpComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG6_08:
                        case enumRbtAddress.STG6_12:
                            m_listSTG[5].OnClmpComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG7_08:
                        case enumRbtAddress.STG7_12:
                            m_listSTG[6].OnClmpComplete -= FinishLoadportOnDock;
                            break;
                        case enumRbtAddress.STG8_08:
                        case enumRbtAddress.STG8_12:
                            m_listSTG[7].OnClmpComplete -= FinishLoadportOnDock;
                            break;
                    }
                    break;
            }

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
                m_eStep = eTeachStep.Home;
            }
            EnableProcedureButton(true);
        }
        private void FinishShutterDoorOnOpen(object sender, bool bsucceed)
        {
            var eq = sender as SSEquipment;

            // 先退訂：用 sender 退訂最準，不用那串 switch
            if (eq != null)
                eq.OnSutterDoorOpenComplete -= FinishShutterDoorOnOpen;

            if (bsucceed)
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
                m_eStep = eTeachStep.Home;
            }
            else
            {
                MessageBox.Show(
                        eq.ErrorMSG,
                        "Shutter Door Open Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
            }
            EnableProcedureButton(true);
        }
        //  Robot Home Done
        private void FinishRobotHome(object sender, bool bSuc)
        {
            m_robot.OnManualCompleted -= FinishRobotHome;

            pnHome.BackColor = GParam.theInst.ColorReadyGreen;
            pnExtd.BackColor = GParam.theInst.ColorWaitYellow;
            Cursor.Current = Cursors.Default;
            rtbInstruct.Clear();
            rtbInstruct.AppendText("Does the arm extend directly?\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Does the arm extend directly?\r"));
            rtbInstruct.AppendText("If need move,press [Next] Button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If need move,press [Next] Button\r"));
            rtbInstruct.AppendText("If need skip this step,press [Cancel] Button\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If need skip this step,press [Cancel] Button\r"));

            m_eStep = eTeachStep.Extd;
            EnableProcedureButton(true);
        }
        //  Robot Extd Done
        private void FinishRobotExtd(object sender, bool bSuc)
        {
            m_robot.OnManualCompleted -= FinishRobotExtd;
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
            m_eStep = eTeachStep.Teach;
        }
        #endregion

        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion
        private void btnAlignerClmp_Click(object sender, EventArgs e)
        {
            /*m_listALN[0].DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;
                alignerManual.ResetInPos();
                alignerManual.ClmpW(3000, false);
                alignerManual.WaitInPos(30000);
            };
            m_listALN[0].OnManualCompleted -= _aligner_OnManualCompleted;
            m_listALN[0].OnManualCompleted += _aligner_OnManualCompleted;
            m_listALN[0].StartManualFunction();*/

            EnableControlButton(false);
            I_Aligner aln = m_listALN[0];
            if (cbxStage.Text == enumRbtAddress.ALN2.ToString())
            {
                aln = m_listALN[1];
            }

            aln.DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;
                alignerManual.ResetInPos();
                alignerManual.ClmpW(3000, false);
                alignerManual.WaitInPos(30000);
            };
            aln.OnManualCompleted -= _aligner_OnManualCompleted;
            aln.OnManualCompleted += _aligner_OnManualCompleted;
            aln.StartManualFunction();
        }
        private void btnAlignerUnClmp_Click(object sender, EventArgs e)
        {
            /*m_listALN[0].DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;
                alignerManual.ResetInPos();
                alignerManual.UclmW(3000);
                alignerManual.WaitInPos(30000);
            };
            m_listALN[0].OnManualCompleted += _aligner_OnManualCompleted;
            m_listALN[0].StartManualFunction();*/

            EnableControlButton(false);
            I_Aligner aln = m_listALN[0];
            if (cbxStage.Text == enumRbtAddress.ALN2.ToString())
            {
                aln = m_listALN[1];
            }

            aln.DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;
                alignerManual.ResetInPos();
                alignerManual.UclmW(3000);
                alignerManual.WaitInPos(30000);
            };
            aln.OnManualCompleted -= _aligner_OnManualCompleted;
            aln.OnManualCompleted += _aligner_OnManualCompleted;
            aln.StartManualFunction();
        }
        private void _aligner_OnManualCompleted(object sender, bool bSuc)
        {
            I_Aligner alignerManual = sender as I_Aligner;
            alignerManual.OnManualCompleted -= _aligner_OnManualCompleted;
            EnableControlButton(true);
            EnableProcedureButton(true);
        }

        private void btnCLMP_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            EnableControlButton(false);
            EnableProcedureButton(false);
            try
            {
                if (m_robot.Connected)
                {
                    if (bSelectUpArm == true)
                    {
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", btn.Name);
                        m_robot.ResetInPos();
                        m_robot.ClmpW(3000, 4);
                        m_robot.WaitInPos(m_robot.GetMotionTimeout);
                    }
                    else
                    {
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", btn.Name);
                        m_robot.ResetInPos();
                        m_robot.ClmpW(3000, 5);
                        m_robot.WaitInPos(m_robot.GetMotionTimeout);
                    }
                }
            }
            catch
            {
                new frmMessageBox("Clamp fail!", "Confirm", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
            }
            EnableControlButton(true);
            EnableProcedureButton(true);
        }
        private void brnUCLM_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            EnableControlButton(false);
            EnableProcedureButton(false);
            try
            {
                if (m_robot.Connected)
                {
                    if (bSelectUpArm == true)
                    {
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", btn.Name);
                        m_robot.ResetInPos();
                        m_robot.UclmW(3000, 1);
                        m_robot.WaitInPos(m_robot.GetMotionTimeout);
                    }
                    else
                    {
                        _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", btn.Name);
                        m_robot.ResetInPos();
                        m_robot.UclmW(3000, 2);
                        m_robot.WaitInPos(m_robot.GetMotionTimeout);
                    }
                }
            }
            catch
            {
                new frmMessageBox("Unclamp fail!", "Confirm", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
            }
            EnableControlButton(true);
            EnableProcedureButton(true);
        }

        private void btnCCW_Click(object sender, EventArgs e)
        {
            I_Aligner aln = m_listALN[0];
            if (_nCurrStage == GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.ALN2))
            {
                aln = m_listALN[1];
            }

            try
            {
                EnableControlButton(false);

                aln.ResetInPos();
                aln.Rot1STEP(m_nStep);
                aln.WaitInPos(100000);

                EnableControlButton(true);
            }
            catch
            {
                EnableControlButton(true);
            }
        }

        private void btnCW_Click(object sender, EventArgs e)
        {
            I_Aligner aln = m_listALN[0];
            if (_nCurrStage == GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.ALN2))
            {
                aln = m_listALN[1];
            }

            try
            {
                EnableControlButton(false);

                aln.ResetInPos();
                aln.Rot1STEP(-1 * m_nStep);
                aln.WaitInPos(100000);

                EnableControlButton(true);
            }
            catch
            {
                EnableControlButton(true);
            }
        }
    }
}
