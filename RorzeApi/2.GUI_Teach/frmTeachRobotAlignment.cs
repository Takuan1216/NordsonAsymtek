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
using RorzeUnit.Class.RC500.RCEnum;
using static RorzeUnit.Class.SWafer;
using RorzeUnit.Class.Robot;

namespace RorzeApi
{
    public partial class frmTeachRobotAlignment : Form
    {
        private int _nCurrStage = 0;        //  Stage number of Robot
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
        private enumRC550Axis m_eXAX1 = enumRC550Axis.AXS1;

        private Class.SPermission _userManager;   //  管理LOGIN使用者權限
        private string _strUserName;//登入者名稱
        private bool m_bIsRunMode = false;
        private SProcessDB _accessDBlog;
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        private I_Robot m_robot;
        private List<I_Robot> m_listTRB;
        private List<I_Loadport> m_listSTG;
        private List<I_Aligner> m_listALN;
        private List<I_Buffer> m_listBUF;
        private List<SSEquipment> m_listEQM;


        private List<Button> m_btnSelectRobotList = new List<Button>();

        public frmTeachRobotAlignment(
            List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN,
            List<I_Buffer> listBUF,
            SProcessDB db, Class.SPermission userManager, bool bIsRunMode, List<SSEquipment> listEQM)
        {
            InitializeComponent();
            this.Size = new Size(1024, 698);

            //  消失頁籤
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);

            m_listTRB = listTRB;
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
                    case enumRbtAddress.STG1_12: if (m_listSTG[0] != null && m_listSTG[0].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG2_12: if (m_listSTG[1] != null && m_listSTG[1].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG3_12: if (m_listSTG[2] != null && m_listSTG[2].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG4_12: if (m_listSTG[3] != null && m_listSTG[3].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG5_12: if (m_listSTG[4] != null && m_listSTG[4].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG6_12: if (m_listSTG[5] != null && m_listSTG[5].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG7_12: if (m_listSTG[6] != null && m_listSTG[6].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                    case enumRbtAddress.STG8_12: if (m_listSTG[7] != null && m_listSTG[7].Disable == false) cbxStage.Items.Add(item.strDisplayName); break;
                }
            }
            if (cbxStage.Items.Count > 0) cbxStage.SelectedIndex = 0;

            if (GParam.theInst.FreeStyle)
            {
                btnSave.Image = RorzeApi.Properties.Resources._48_save_;
                btnNext.Image = RorzeApi.Properties.Resources._32_next_;
                btnCancel.Image = RorzeApi.Properties.Resources._32_cancel_;
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
                        m_robot = m_listTRB[0]; // as SSRobotRR75x; ;
                    }
                    else if (strName2 == btn.Text)
                    {
                        m_robot = m_listTRB[1]; //as SSRobotRR75x; ;
                    }
                    else
                        switch (btn.Text)
                        {
                            case "RobotA":
                            case "Robot A":
                                m_robot = m_listTRB[0]; break; //as SSRobotRR75x; ; break;
                            case "RobotB":
                            case "Robot B":
                                m_robot = m_listTRB[1]; break; //as SSRobotRR75x; ; break;
                            default:
                                m_robot = null; break;
                        }

                    if (m_robot == null)
                    {
                        new frmMessageBox("Robot isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
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

            if (tabControl1.SelectedTab == tabPage2)
            {
                if (m_robot == null || false == m_robot.Connected) return;
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
        private void cboStage_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cbxType.SelectedIndex = -1;
            cbxType.Enabled = false;
            cbxJIGType.SelectedIndex = -1;
            cbxJIGType.Enabled = false;
            if (cbxStage.SelectedItem == null) return;
            _strSelectStgName = cbxStage.SelectedItem.ToString();

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
                            break;
                        case enumRbtAddress.STG2_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[1].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[1].eFoupType;
                            break;
                        case enumRbtAddress.STG2_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[1].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[1].eFoupType;
                            break;
                        case enumRbtAddress.STG3_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[2].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[2].eFoupType;
                            break;
                        case enumRbtAddress.STG3_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[2].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[2].eFoupType;
                            break;
                        case enumRbtAddress.STG4_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[3].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[3].eFoupType;
                            break;
                        case enumRbtAddress.STG4_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[3].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[3].eFoupType;
                            break;
                        case enumRbtAddress.STG5_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[4].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[4].eFoupType;
                            break;
                        case enumRbtAddress.STG5_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[4].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[4].eFoupType;
                            break;
                        case enumRbtAddress.STG6_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[5].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[5].eFoupType;
                            break;
                        case enumRbtAddress.STG6_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[5].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[5].eFoupType;
                            break;
                        case enumRbtAddress.STG7_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[6].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[6].eFoupType;
                            break;
                        case enumRbtAddress.STG7_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[6].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[6].eFoupType;
                            break;
                        case enumRbtAddress.STG8_08:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[7].GetDPRMData[i + 16][16]); }//16~31
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[7].eFoupType;
                            break;
                        case enumRbtAddress.STG8_12:
                            for (int i = 0; i < 16; i++) { cbxType.Items.Add(m_listSTG[7].GetDPRMData[i][16]); }//0~15
                            nStage = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, item.strDefineName) + (int)m_listSTG[7].eFoupType;
                            break;
                    }
                    break;//成功找到位置
                }
            }
            if (nStage == -1) return;
            _nCurrStage = nStage;
            ShowStageTeachingData(_nCurrStage);
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
            }

            if (_nCurrStage != -1)
                ShowStageTeachingData(_nCurrStage);
        }

        //  Robot 執行 ExeGetTeachData 完成後觸發事件
        private void _robot_OnGetTeachDataCompleted(object sender, bool bSuc)
        {
            I_Robot trb = sender as I_Robot;
            trb.OnGetAlignmentDataCompleted -= _robot_OnGetTeachDataCompleted;//Done
            if (bSuc == false) return;

            nudDAPM_0_0.Value = int.Parse(m_robot.DAPMData[0][0]);
            nudDAPM_0_1.Value = int.Parse(m_robot.DAPMData[0][1]);
            nudDAPM_0_2.Value = int.Parse(m_robot.DAPMData[0][2]);
            nudDAPM_0_3.Value = int.Parse(m_robot.DAPMData[0][3]);
            nudDAPM_0_4.Value = int.Parse(m_robot.DAPMData[0][4]);
            nudDAPM_0_5.Value = int.Parse(m_robot.DAPMData[0][5]);
            nudDAPM_0_6.Value = int.Parse(m_robot.DAPMData[0][6]);
            nudDAPM_0_7.Value = int.Parse(m_robot.DAPMData[0][7]);
            nudDAPM_0_8.Value = int.Parse(m_robot.DAPMData[0][8]);
            nudDAPM_0_9.Value = int.Parse(m_robot.DAPMData[0][9]);
            nudDAPM_0_10.Value = int.Parse(m_robot.DAPMData[0][10]);
            nudDAPM_0_11.Value = int.Parse(m_robot.DAPMData[0][11]);
            nudDAPM_0_12.Value = int.Parse(m_robot.DAPMData[0][12]);

            nudDAPM_1_0.Value = int.Parse(m_robot.DAPMData[1][0]);
            nudDAPM_1_1.Value = int.Parse(m_robot.DAPMData[1][1]);
            nudDAPM_1_2.Value = int.Parse(m_robot.DAPMData[1][2]);
            nudDAPM_1_3.Value = int.Parse(m_robot.DAPMData[1][3]);
            nudDAPM_1_4.Value = int.Parse(m_robot.DAPMData[1][4]);
            nudDAPM_1_5.Value = int.Parse(m_robot.DAPMData[1][5]);
            nudDAPM_1_6.Value = int.Parse(m_robot.DAPMData[1][6]);
            nudDAPM_1_7.Value = int.Parse(m_robot.DAPMData[1][7]);
            nudDAPM_1_8.Value = int.Parse(m_robot.DAPMData[1][8]);
            nudDAPM_1_9.Value = int.Parse(m_robot.DAPMData[1][9]);
            nudDAPM_1_10.Value = int.Parse(m_robot.DAPMData[1][10]);
            nudDAPM_1_11.Value = int.Parse(m_robot.DAPMData[1][11]);
            nudDAPM_1_12.Value = int.Parse(m_robot.DAPMData[1][12]);

            nudDAPM_2_0.Value = int.Parse(m_robot.DAPMData[2][0]);
            nudDAPM_2_1.Value = int.Parse(m_robot.DAPMData[2][1]);
            nudDAPM_2_2.Value = int.Parse(m_robot.DAPMData[2][2]);
            nudDAPM_2_3.Value = int.Parse(m_robot.DAPMData[2][3]);
            nudDAPM_2_4.Value = int.Parse(m_robot.DAPMData[2][4]);
            nudDAPM_2_5.Value = int.Parse(m_robot.DAPMData[2][5]);
            nudDAPM_2_6.Value = int.Parse(m_robot.DAPMData[2][6]);
            nudDAPM_2_7.Value = int.Parse(m_robot.DAPMData[2][7]);
            nudDAPM_2_8.Value = int.Parse(m_robot.DAPMData[2][8]);
            nudDAPM_2_9.Value = int.Parse(m_robot.DAPMData[2][9]);
            nudDAPM_2_10.Value = int.Parse(m_robot.DAPMData[2][10]);
            nudDAPM_2_11.Value = int.Parse(m_robot.DAPMData[2][11]);
            nudDAPM_2_12.Value = int.Parse(m_robot.DAPMData[2][12]);


            if (m_robot.DEQUData != null && m_robot.DEQUData.Count() > 9)//TwoStepLoad DEQU[8] bit4
            {
                int nSoftwareSwitch = int.Parse(m_robot.DEQUData[9]);
                if ((nSoftwareSwitch & 0x30) == 0x30)//DEQU[9] bit4 bit5    
                    cbxDEQU9_BIT4and5.SelectedIndex = 1;
                else
                    cbxDEQU9_BIT4and5.SelectedIndex = 0;
            }

            if (m_robot.DCFGData != null)
            {
                int nValue = int.Parse(m_robot.DCFGData[5]);

                if ((nValue & 0xA0) == 0xA0)//DCFG[5] bit5 bit7 
                    cbxDCFG5_BIT5and7.SelectedIndex = 1;
                else
                    cbxDCFG5_BIT5and7.SelectedIndex = 0;
            }

            ChangeButtun(btnTeachArm1, true);
            ChangeButtun(btnTeachArm2, true);
            ChangeButtun(btnSave, true);

        }
        //  Robot 執行 ExeSetTeachData 完成後觸發事件
        private void _robot_OnSetTeachDataCompleted(object sender, bool bSuc)
        {
            I_Robot trb = sender as I_Robot;
            trb.OnSetAlignmentDataCompleted -= _robot_OnSetTeachDataCompleted;//Done

            ShowStageTeachingData(_nCurrStage);

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

            }
        }
        //  叫 robot 取得資料
        private void ShowStageTeachingData(int nStage)
        {
            DataSetEnableControlButton(_nCurrStage != -1);

            if (m_robot == null || nStage <= 0) return;

            m_robot.OnGetAlignmentDataCompleted -= _robot_OnGetTeachDataCompleted;//Get            
            m_robot.OnGetAlignmentDataCompleted += _robot_OnGetTeachDataCompleted;//Get            
            m_robot.GetDAPMData(nStage);
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

            EnablePage1Button(false);

            if (m_robot.Connected && m_bSimulate == false)
            {
                if (m_robot.DEQUData != null && m_robot.DEQUData.Count() > 9)//TwoStepLoad DEQU[8] bit4
                {
                    int nSoftwareSwitch = int.Parse(m_robot.DEQUData[9]);

                    int nBit = 0x01 << 4 | 0x01 << 5;
                    bool bON = cbxDEQU9_BIT4and5.SelectedIndex != 0;
                    if (bON)
                        nSoftwareSwitch = (nSoftwareSwitch | nBit);
                    else
                        nSoftwareSwitch = (nSoftwareSwitch & ~nBit);

                    m_robot.DEQUData[9] = nSoftwareSwitch.ToString();
                }

                if (m_robot.DCFGData != null)
                {
                    int nValue = int.Parse(m_robot.DCFGData[5]);//取出原本的D值           
                    int nV = 0x01 << 5 | 0x01 << 7;
                    if (cbxDCFG5_BIT5and7.SelectedIndex != 0)
                        nValue = (nValue | nV);
                    else
                        nValue = (nValue & ~nV);
                    m_robot.DCFGData[5] = nValue.ToString();
                }

                m_robot.DAPMData[0][0] = nudDAPM_0_0.Value.ToString();
                m_robot.DAPMData[0][1] = nudDAPM_0_1.Value.ToString();
                m_robot.DAPMData[0][2] = nudDAPM_0_2.Value.ToString();
                m_robot.DAPMData[0][3] = nudDAPM_0_3.Value.ToString();
                m_robot.DAPMData[0][4] = nudDAPM_0_4.Value.ToString();
                m_robot.DAPMData[0][5] = nudDAPM_0_5.Value.ToString();
                m_robot.DAPMData[0][6] = nudDAPM_0_6.Value.ToString();
                m_robot.DAPMData[0][7] = nudDAPM_0_7.Value.ToString();
                m_robot.DAPMData[0][8] = nudDAPM_0_8.Value.ToString();
                m_robot.DAPMData[0][9] = nudDAPM_0_9.Value.ToString();
                m_robot.DAPMData[0][10] = nudDAPM_0_10.Value.ToString();
                m_robot.DAPMData[0][11] = nudDAPM_0_11.Value.ToString();
                m_robot.DAPMData[0][12] = nudDAPM_0_12.Value.ToString();

                m_robot.DAPMData[1][0] = nudDAPM_1_0.Value.ToString();
                m_robot.DAPMData[1][1] = nudDAPM_1_1.Value.ToString();
                m_robot.DAPMData[1][2] = nudDAPM_1_2.Value.ToString();
                m_robot.DAPMData[1][3] = nudDAPM_1_3.Value.ToString();
                m_robot.DAPMData[1][4] = nudDAPM_1_4.Value.ToString();
                m_robot.DAPMData[1][5] = nudDAPM_1_5.Value.ToString();
                m_robot.DAPMData[1][6] = nudDAPM_1_6.Value.ToString();
                m_robot.DAPMData[1][7] = nudDAPM_1_7.Value.ToString();
                m_robot.DAPMData[1][8] = nudDAPM_1_8.Value.ToString();
                m_robot.DAPMData[1][9] = nudDAPM_1_9.Value.ToString();
                m_robot.DAPMData[1][10] = nudDAPM_1_10.Value.ToString();
                m_robot.DAPMData[1][11] = nudDAPM_1_11.Value.ToString();
                m_robot.DAPMData[1][12] = nudDAPM_1_12.Value.ToString();

                m_robot.DAPMData[2][0] = nudDAPM_2_0.Value.ToString();
                m_robot.DAPMData[2][1] = nudDAPM_2_1.Value.ToString();
                m_robot.DAPMData[2][2] = nudDAPM_2_2.Value.ToString();
                m_robot.DAPMData[2][3] = nudDAPM_2_3.Value.ToString();
                m_robot.DAPMData[2][4] = nudDAPM_2_4.Value.ToString();
                m_robot.DAPMData[2][5] = nudDAPM_2_5.Value.ToString();
                m_robot.DAPMData[2][6] = nudDAPM_2_6.Value.ToString();
                m_robot.DAPMData[2][7] = nudDAPM_2_7.Value.ToString();
                m_robot.DAPMData[2][8] = nudDAPM_2_8.Value.ToString();
                m_robot.DAPMData[2][9] = nudDAPM_2_9.Value.ToString();
                m_robot.DAPMData[2][10] = nudDAPM_2_10.Value.ToString();
                m_robot.DAPMData[2][11] = nudDAPM_2_11.Value.ToString();
                m_robot.DAPMData[2][12] = nudDAPM_2_12.Value.ToString();

            }
            m_robot.OnSetAlignmentDataCompleted -= _robot_OnSetTeachDataCompleted;//Save
            m_robot.OnSetAlignmentDataCompleted += _robot_OnSetTeachDataCompleted;//Save
            m_robot.SetDAPMData(_nCurrStage);


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

        #region 教導流程

        private enum eTeachStep { Prepare = 0, Alld, Alex, Teach, End };
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
                case eTeachStep.Alld:
                    DoAlldTask();
                    break;
                case eTeachStep.Alex:
                    DoAlexTask();
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
                case eTeachStep.Alld:
                    SkipHomeTask();
                    break;
                case eTeachStep.Alex:
                    //DoExtdTask(false);
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
            //用治具
			switch (cbxJIGType.SelectedIndex)
            {
                case 0:
            switch (m_SelectUnit)
            {
                case enumRbtAddress.STG1_08:
                case enumRbtAddress.STG1_12:
                    m_listSTG[0].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[0].OnClmpComplete += FinishLoadportOnDock;
                    m_listSTG[0].JigDock();
                    break;
                case enumRbtAddress.STG2_08:
                case enumRbtAddress.STG2_12:
                    m_listSTG[1].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[1].OnClmpComplete += FinishLoadportOnDock;
                    m_listSTG[1].JigDock();
                    break;
                case enumRbtAddress.STG3_08:
                case enumRbtAddress.STG3_12:
                    m_listSTG[2].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[2].OnClmpComplete += FinishLoadportOnDock;
                            m_listSTG[2].JigDock();
                    break;
                case enumRbtAddress.STG4_08:
                case enumRbtAddress.STG4_12:
                    m_listSTG[3].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[3].OnClmpComplete += FinishLoadportOnDock;
                    m_listSTG[3].JigDock();
                    break;
                case enumRbtAddress.STG5_08:
                case enumRbtAddress.STG5_12:
                    m_listSTG[4].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[4].OnClmpComplete += FinishLoadportOnDock;
                    m_listSTG[4].JigDock();
                    break;
                case enumRbtAddress.STG6_08:
                case enumRbtAddress.STG6_12:
                    m_listSTG[5].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[5].OnClmpComplete += FinishLoadportOnDock;
                    m_listSTG[5].JigDock();
                    break;
                case enumRbtAddress.STG7_08:
                case enumRbtAddress.STG7_12:
                    m_listSTG[6].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[6].OnClmpComplete += FinishLoadportOnDock;
                    m_listSTG[6].JigDock();
                    break;
                case enumRbtAddress.STG8_08:
                case enumRbtAddress.STG8_12:
                    m_listSTG[7].OnClmpComplete -= FinishLoadportOnDock;
                            m_listSTG[7].OnClmpComplete += FinishLoadportOnDock;
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


        //  step2
        private void DoAlldTask()
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
                robotManual.AlldW(robotManual.GetAckTimeout, bSelectUpArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm, _nCurrStage, 1, 0, 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                robotManual.WtdtW(robotManual.GetMotionTimeout);

                robotManual.GaldW(robotManual.GetAckTimeout, bSelectUpArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm);

                robotManual.ResetInPos();
                //robotManual.LoadW(robotManual.GetAckTimeout, bSelectUpArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm, _nCurrStage, 1);
                robotManual.AlldW(robotManual.GetAckTimeout, bSelectUpArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm, _nCurrStage, 1, 0, 0);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);

            };
            m_robot.OnManualCompleted += FinishAlld;
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


            EnableProcedureButton(true);
            m_eStep = eTeachStep.Teach;
        }

        //  step3
        private void DoAlexTask()
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

                int nAlexAddress = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, enumRbtAddress.ALEX);

                robotManual.ResetInPos();
                robotManual.AlexW(robotManual.GetAckTimeout, bSelectUpArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm, nAlexAddress, 1, 0, 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                robotManual.WtdtW(robotManual.GetMotionTimeout);

                robotManual.ResetInPos();
                robotManual.UnldW(robotManual.GetAckTimeout, bSelectUpArm ? enumRobotArms.UpperArm : enumRobotArms.LowerArm, _nCurrStage, 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);
            };
            m_robot.OnManualCompleted += FinishAlex;
            m_robot.StartManualFunction();




        }

        //  step4
        private void DoTeachTask()
        {
            if (new frmMessageBox("Are you sure complete teaching ?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;

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
                m_robot.TBL_560.AxisMabsW(m_robot.GetAckTimeout, m_eXAX1, pos.Pos_ARM1);
                m_robot.TBL_560.WaitInPos(m_robot.GetMotionTimeout);
            }

            frmOrgn _frmOrgn = new frmOrgn(m_listTRB, m_listSTG, m_listALN, m_listBUF, m_listEQM, GParam.theInst.IsSimulate);
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
                m_eStep = eTeachStep.Alld;
            }
            EnableProcedureButton(true);
        }
        //  Robot Home Done
        private void FinishAlld(object sender, bool bSuc)
        {
            m_robot.OnManualCompleted -= FinishAlld;

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

            m_eStep = eTeachStep.Alex;
            EnableProcedureButton(true);
        }
        //  Robot Extd Done
        private void FinishAlex(object sender, bool bSuc)
        {
            m_robot.OnManualCompleted -= FinishAlex;
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

            EnableProcedureButton(true);
            m_eStep = eTeachStep.Teach;
        }
        #endregion

        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion




    }
}
