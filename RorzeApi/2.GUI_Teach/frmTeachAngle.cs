using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using RorzeUnit.Interface;
using RorzeUnit.Class;
using RorzeComm.Log;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeApi.GUI;
using System.Collections.Concurrent;
using RorzeApi.Class;
using RorzeUnit.Class.E84.Event;
using System.Reflection;
using Rorze.Equipments.Unit;
using static System.Windows.Forms.AxHost;
using RorzeUnit.Class.Robot;

namespace RorzeApi
{
    public partial class frmTeachAngle : Form
    {
        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion

        private enum eTeachStep { Prepare = 0, Teach, End };    //  Page2 所有步驟    
        List<Label> lstLabStep = new List<Label>();             //  Page2 上面那排顯示做到第幾步驟的Lable
        private bool m_bBlink = false;                          //  Page2 上面那排顯示做到第幾步驟的Lable 畫面閃燈
        private eTeachStep m_eStep;                             //  Page2 做到哪一步驟

        private int m_nStep = 30000;        //  Jog Step   
        private float frmX;                 //  Zoom 放入窗體的寬度
        private float frmY;                 //  Zoom 放入窗體的高度
        private bool isLoaded = false;      //  Zoom 是否已設定各控制的尺寸資料到Tag屬性
        private bool m_bSimulate;

        //  ROBOT ADDRESS 0~399
        private int m_nSTG_address = -1;// 10 30 50 70
        private int m_nALN_address = -1;// 5

        //private int m_SelectPort = -1;
        private int m_SelectWafer = -1;

        private float PoseW;

        private SProcessDB _accessDBlog;
        private SPermission _userManager;   //  管理LOGIN使用者權限
        private string _strUserName;//登入者名稱
        private bool m_bIsRunMode = false;

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        private List<I_Robot> ListTRB = new List<I_Robot>();
        private I_Robot m_robotSelect;//要teaching誰
        private I_Robot m_robotLoadUnld;//要誰取片出來
        private List<I_Loadport> ListSTG = new List<I_Loadport>();
        private I_Loadport m_loadport;
        private List<I_Aligner> ListALN = new List<I_Aligner>();
        private I_Aligner m_aligner;

        private enumNotchAngle m_eNotchAngle;

        private SGroupRecipeManager m_dbGrouprecipe;
        private List<GUILoadport> m_guiloadportList = new List<GUILoadport>();
        private List<Button> m_btnSelectAlignerList = new List<Button>();
        private List<Button> m_btnSelectRobotList = new List<Button>();

        private Dictionary<enumNotchAngle, TextBox> DicAngleSub = new Dictionary<enumNotchAngle, TextBox>();
        private Dictionary<enumNotchAngle, TextBox> DicAngleAll = new Dictionary<enumNotchAngle, TextBox>();


        public frmTeachAngle(List<I_Robot> robotList, List<I_Loadport> loadportList, List<I_Aligner> alignerList,
            SProcessDB db, SPermission userManager, SGroupRecipeManager grouprecipe, bool bIsRunMode)
        {
            InitializeComponent();
            this.Size = new Size(970, 718);
            //  消失頁籤
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);

            ListTRB = robotList;
            ListSTG = loadportList;
            ListALN = alignerList;

            m_bIsRunMode = bIsRunMode;
            m_bSimulate = GParam.theInst.IsSimulate;
            _accessDBlog = db;
            _userManager = userManager;
            m_dbGrouprecipe = grouprecipe;

            #region Select Aligner Button

            tlpSelectAligner.RowStyles.Clear();
            tlpSelectAligner.ColumnStyles.Clear();
            tlpSelectAligner.Dock = DockStyle.Fill;

            for (int i = 0; i < ListALN.Count; i++)
            {
                if (ListALN[i].Disable) continue;

                tlpSelectAligner.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

                Button btn = new Button();
                btn.Font = new Font("微軟正黑體", 18, FontStyle.Bold);
                btn.Text = GParam.theInst.GetLanguage("Aligner" + (char)(64 + ListALN[i].BodyNo));
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleCenter;

                btn.Click += btnSelectAligner_Click;

                m_btnSelectAlignerList.Add(btn);
                tlpSelectAligner.Controls.Add(btn, 0, m_btnSelectAlignerList.Count - 1);//注意
            }
            tlpSelectAligner.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));

            #endregion          

            #region Select Robot Button

            tlpSelectRobot.RowStyles.Clear();
            tlpSelectRobot.ColumnStyles.Clear();
            tlpSelectRobot.Dock = DockStyle.Fill;

            for (int i = 0; i < ListTRB.Count; i++)
            {
                if (ListTRB[i].Disable) continue;

                if (ListTRB[i].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false &&
                    ListTRB[i].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false)
                {
                    continue;
                }
                tlpSelectRobot.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

                Button btn = new Button();
                btn.Font = new Font("微軟正黑體", 18, FontStyle.Bold);
                btn.Text = GParam.theInst.GetLanguage("Robot" + (char)(64 + ListTRB[i].BodyNo));
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleCenter;

                btn.Click += btnSelectRobot_Click;

                m_btnSelectRobotList.Add(btn);
                tlpSelectRobot.Controls.Add(btn, 0, m_btnSelectRobotList.Count - 1);//注意
            }
            tlpSelectRobot.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));

            #endregion          

            #region Loadport
            m_guiloadportList.Add(guiLoadport1);
            m_guiloadportList.Add(guiLoadport2);
            m_guiloadportList.Add(guiLoadport3);
            m_guiloadportList.Add(guiLoadport4);
            m_guiloadportList.Add(guiLoadport5);
            m_guiloadportList.Add(guiLoadport6);
            m_guiloadportList.Add(guiLoadport7);
            m_guiloadportList.Add(guiLoadport8);


            for (int i = 0; i < ListSTG.Count; i++)
            {
                if (GParam.theInst.FreeStyle)
                    m_guiloadportList[i].SetFreeStyleColor(
                        GParam.theInst.ColorTitle,//橘
                        Color.FromArgb(97, 162, 79),//ready綠
                        Color.FromArgb(98, 186, 166),//wafer
                        Color.FromArgb(135, 206, 250),//select藍
                        Color.FromArgb(151, 218, 203)
                         );

                m_guiloadportList[i].Simulate = GParam.theInst.IsSimulate;
                m_guiloadportList[i].BodyNo = i + 1;
                m_guiloadportList[i].Disable_E84 = true;
                m_guiloadportList[i].Disable_OCR = GParam.theInst.IsAllOcrDisable();
                m_guiloadportList[i].Disable_Recipe = true;
                m_guiloadportList[i].Disable_RSV = true;
                m_guiloadportList[i].Disable_ClmpLock = true;
                m_guiloadportList[i].Disable_DockBtn = false;

                m_guiloadportList[i].Disable_ProcessBtn = true;
                m_guiloadportList[i].Visible = !ListSTG[i].Disable;
                m_guiloadportList[i].Enabled = !ListSTG[i].Disable;

                if (ListSTG[i].Disable)
                {
                    continue;//不需要註冊
                }

                ListSTG[i].OnFoupExistChenge += OnLoadport_FoupExistChenge;          //  更新UI  
                ListSTG[i].OnClmpComplete += OnLoadport_MappingComplete;             //  更新UI
                ListSTG[i].OnMappingComplete += OnLoadport_MappingComplete;          //  更新UI
                ListSTG[i].OnStatusMachineChange += OnLoadport_StatusMachineChange;  //  更新UI
                ListSTG[i].OnFoupIDChange += OnLoadport_FoupIDChange;                //  更新UI
                ListSTG[i].OnFoupTypeChange += OnLoadport_FoupTypeChange;            //  更新UI

                //ListSTG[i].OnUclmComplete += OnLoadport_OnUclmComplete;              //  更新UI
                //m_e84List[i].OnAceessModeChange += OnLoadport_E84ModeChange;                //  更新UI
                ListSTG[i].OnTakeWaferInFoup += m_guiloadportList[i].TakeWaferInFoup;//wafer被塞進來
                ListSTG[i].OnTakeWaferOutFoup += m_guiloadportList[i].TakeWaferOutFoup;//wafer被拿走              

                //m_guiloadportList[i].BtnClamp += btnClamp_Click;
                m_guiloadportList[i].BtnDock += btnDock_Click;
                m_guiloadportList[i].BtnUnDock += btnUnDock_Click;
                //m_guiloadportList[i].BtnE84Mode += btnE84Mode_Click;
                //m_guiloadportList[i].ChkRecipeSelect += chkRecipeSelect_Checked;
                //m_guiloadportList[i].BtnProcess += btnProcess_Click;
                m_guiloadportList[i].ChkFoupOn += chkFoupOn_Checked;
                //m_guiloadportList[i].FoupIDKeyDownEnter += txtLoaderFoupID_Enter;
                //m_guiloadportList[i].LotIDKeyDownEnter += txtLoaderLotID_Enter;
                m_guiloadportList[i].UseSelectWafer += GuiLoadport_UseSelectWafer;//選片功能

            }


            tabPageABCD.Text = "";
            if (ListSTG[0].Disable == false) tabPageABCD.Text += "A";
            if (ListSTG[1].Disable == false) tabPageABCD.Text += "B";
            if (ListSTG[2].Disable == false) tabPageABCD.Text += "C";
            if (ListSTG[3].Disable == false) tabPageABCD.Text += "D";

            tabPageEFGH.Text = "";
            if (ListSTG[4].Disable == false) tabPageEFGH.Text += "E";
            if (ListSTG[5].Disable == false) tabPageEFGH.Text += "F";
            if (ListSTG[6].Disable == false) tabPageEFGH.Text += "G";
            if (ListSTG[7].Disable == false) tabPageEFGH.Text += "H";

            //只有一頁
            if (tabPageEFGH.Text == "")
            {
                tabPageABCD.Text = "EFEM";
                //  消失頁籤
                tabPageEFGH.Parent = null;
                //tabCtrlStage.SizeMode = TabSizeMode.Fixed;
                //tabCtrlStage.ItemSize = new Size(0, 1);

                int nCount = tabPageABCD.Text.Length;
                if (nCount <= 1)
                {
                    foreach (GUILoadport item in m_guiloadportList)
                    {
                        item.Width = (flowLayoutPanel1.Width - 0) / 2;
                        item.Height = flowLayoutPanel1.Height - 5/*+ 20*/;
                    }
                }
                else
                {
                    foreach (GUILoadport item in m_guiloadportList)
                    {
                        item.Width = (flowLayoutPanel1.Width - 0) / nCount;
                        item.Height = flowLayoutPanel1.Height - 5/*+ 20*/;
                    }
                }
            }
            else//有第二頁
            {
                tabPageEFGH.Parent = tabCtrlStage;
                foreach (GUILoadport item in m_guiloadportList)
                {
                    item.Width = (flowLayoutPanel1.Width - 0) / 4;
                    item.Height = flowLayoutPanel1.Height - 5;
                }
            }
            #endregion

            //  顯示步驟的那條
            int nIdx = 0;
            foreach (string str in Enum.GetNames(typeof(eTeachStep)))
            {
                Label lb = new Label();
                lb.Font = new Font("Calibri", 9, FontStyle.Regular);
                lb.Text = str;
                lb.Dock = DockStyle.Fill;
                lb.TextAlign = ContentAlignment.MiddleCenter;
                tlpnlStep.Controls.Add(lb, nIdx, 0);
                lstLabStep.Add(lb);
                nIdx++;
            }


            int nTotalCount = Enum.GetNames(typeof(enumNotchAngle)).Count();

            foreach (enumNotchAngle eType in Enum.GetValues(typeof(enumNotchAngle)))
            {
                switch (eType)
                {
                    case enumNotchAngle.ALN1_RB1_STG1:
                        if (ListALN[0].Disable || ListTRB[0].Disable || ListSTG[0].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB1_STG2:
                        if (ListALN[0].Disable || ListTRB[0].Disable || ListSTG[1].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB1_STG3:
                        if (ListALN[0].Disable || ListTRB[0].Disable || ListSTG[2].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB1_STG4:
                        if (ListALN[0].Disable || ListTRB[0].Disable || ListSTG[3].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB1_STG5:
                        if (ListALN[0].Disable || ListTRB[0].Disable || ListSTG[4].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB1_STG6:
                        if (ListALN[0].Disable || ListTRB[0].Disable || ListSTG[5].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB1_STG7:
                        if (ListALN[0].Disable || ListTRB[0].Disable || ListSTG[6].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB1_STG8:
                        if (ListALN[0].Disable || ListTRB[0].Disable || ListSTG[7].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB2_STG1:
                        if (ListALN[0].Disable || ListTRB[1].Disable || ListSTG[0].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB2_STG2:
                        if (ListALN[0].Disable || ListTRB[1].Disable || ListSTG[1].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB2_STG3:
                        if (ListALN[0].Disable || ListTRB[1].Disable || ListSTG[2].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB2_STG4:
                        if (ListALN[0].Disable || ListTRB[1].Disable || ListSTG[3].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB2_STG5:
                        if (ListALN[0].Disable || ListTRB[1].Disable || ListSTG[4].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB2_STG6:
                        if (ListALN[0].Disable || ListTRB[1].Disable || ListSTG[5].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB2_STG7:
                        if (ListALN[0].Disable || ListTRB[1].Disable || ListSTG[6].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.ALN1_RB2_STG8:
                        if (ListALN[0].Disable || ListTRB[1].Disable || ListSTG[7].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerA) == false) continue;
                        break;
                    case enumNotchAngle.RB1_RB2:
                        break;
                    case enumNotchAngle.ALN2_RB1_STG1:
                        if (ListALN[1].Disable || ListTRB[0].Disable || ListSTG[0].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB1_STG2:
                        if (ListALN[1].Disable || ListTRB[0].Disable || ListSTG[1].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB1_STG3:
                        if (ListALN[1].Disable || ListTRB[0].Disable || ListSTG[2].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB1_STG4:
                        if (ListALN[1].Disable || ListTRB[0].Disable || ListSTG[3].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB1_STG5:
                        if (ListALN[1].Disable || ListTRB[0].Disable || ListSTG[4].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB1_STG6:
                        if (ListALN[1].Disable || ListTRB[0].Disable || ListSTG[5].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB1_STG7:
                        if (ListALN[1].Disable || ListTRB[0].Disable || ListSTG[6].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB1_STG8:
                        if (ListALN[1].Disable || ListTRB[0].Disable || ListSTG[7].Disable) continue;
                        if (ListTRB[0].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB2_STG1:
                        if (ListALN[1].Disable || ListTRB[1].Disable || ListSTG[0].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB2_STG2:
                        if (ListALN[1].Disable || ListTRB[1].Disable || ListSTG[1].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB2_STG3:
                        if (ListALN[1].Disable || ListTRB[1].Disable || ListSTG[2].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB2_STG4:
                        if (ListALN[1].Disable || ListTRB[1].Disable || ListSTG[3].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB2_STG5:
                        if (ListALN[1].Disable || ListTRB[1].Disable || ListSTG[4].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB2_STG6:
                        if (ListALN[1].Disable || ListTRB[1].Disable || ListSTG[5].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB2_STG7:
                        if (ListALN[1].Disable || ListTRB[1].Disable || ListSTG[6].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.ALN2_RB2_STG8:
                        if (ListALN[1].Disable || ListTRB[1].Disable || ListSTG[7].Disable) continue;
                        if (ListTRB[1].RobotHardwareAllow(SWafer.enumPosition.AlignerB) == false) continue;
                        break;
                    case enumNotchAngle.RB2_RB1:
                        break;
                    default:
                        continue;
                }

                int n = (int)eType;

                //tabPage2 建立所有角度參數列表
                Label lbl = new Label();
                lbl.Margin = new System.Windows.Forms.Padding(3);
                lbl.Dock = DockStyle.Fill;
                lbl.Text = GetEnumDescription(eType);
                lbl.TextAlign = ContentAlignment.MiddleRight;
                tlpNotchAngleSub.Controls.Add(lbl, 0 + n / (nTotalCount / 2) * 2, n % (nTotalCount / 2));
                TextBox txt = new TextBox();
                txt.Dock = DockStyle.Fill;
                txt.Anchor = (AnchorStyles.Left | AnchorStyles.Right);
                tlpNotchAngleSub.Controls.Add(txt, 1 + n / (nTotalCount / 2) * 2, n % (nTotalCount / 2));

                DicAngleSub.Add(eType, txt);

                //tabPage3 建立所有角度參數列表
                Label lbl2 = new Label();
                lbl2.Margin = new System.Windows.Forms.Padding(3);
                lbl2.Dock = DockStyle.Fill;
                lbl2.Text = GetEnumDescription(eType);
                lbl2.TextAlign = ContentAlignment.MiddleRight;
                tlpNotchAngleAll.Controls.Add(lbl2, 0 + n / (nTotalCount / 2) * 2, n % (nTotalCount / 2));

                TextBox txt2 = new TextBox();
                txt2.Dock = DockStyle.Fill;
                txt2.Anchor = (AnchorStyles.Left | AnchorStyles.Right);
                txt2.Text = GParam.theInst.GetNotchData(eType).ToString();
                tlpNotchAngleAll.Controls.Add(txt2, 1 + n / (nTotalCount / 2) * 2, n % (nTotalCount / 2));

                DicAngleAll.Add(eType, txt2);
            }

            if (GParam.theInst.FreeStyle)
            {
                btnNext.Image = RorzeApi.Properties.Resources._32_next_;
                btnCancel.Image = RorzeApi.Properties.Resources._32_cancel_;
                btnSave.Image = RorzeApi.Properties.Resources._48_save_;
                btnBack.Image = RorzeApi.Properties.Resources._48_arrowback_;

                btnModify.Image = RorzeApi.Properties.Resources._48_edit_file_;
                btnTeach.Image = RorzeApi.Properties.Resources._48_work_;
            }

        }

        //選擇Aligner
        private void btnSelectAligner_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "frmTeachAngle", _strUserName, "Select Robot", btn.Name);

            for (int i = 0; i < m_btnSelectAlignerList.Count; i++)
            {


                if (m_btnSelectAlignerList[i] == btn)
                {
                    m_btnSelectAlignerList[i].BackColor = Color.LightBlue;

                    string strName1 = GParam.theInst.GetLanguage("AlignerA");
                    string strName2 = GParam.theInst.GetLanguage("AlignerB");
                    if (strName1 == btn.Text)
                    {
                        m_aligner = ListALN[0];
                    }
                    else if (strName2 == btn.Text)
                    {
                        m_aligner = ListALN[1];
                    }
                    else
                        switch (btn.Text)
                        {
                            case "AlignerA":
                            case "Aligner A":
                                m_aligner = ListALN[0];
                                break;
                            case "AlignerB":
                            case "Aligner B":
                                m_aligner = ListALN[1];
                                break;
                            default:
                                m_aligner = null;
                                break;
                        }

                    if (m_aligner == null)
                    {
                        new frmMessageBox("The Aligner isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }
                    else
                    {

                    }
                    continue;
                }
                else
                    m_btnSelectAlignerList[i].BackColor = System.Drawing.SystemColors.ControlLight;
            }

        }
        //選擇Robot
        private void btnSelectRobot_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "frmTeachAngle", _strUserName, "Select Robot", btn.Name);

            for (int i = 0; i < m_btnSelectRobotList.Count; i++)
            {

                if (m_btnSelectRobotList[i] == btn)
                {
                    m_btnSelectRobotList[i].BackColor = Color.LightBlue;

                    string strName1 = GParam.theInst.GetLanguage("RobotA");
                    string strName2 = GParam.theInst.GetLanguage("RobotB");

                    if (strName1 == btn.Text)
                    {
                        m_robotSelect = ListTRB[0];
                    }
                    else if (strName2 == btn.Text)
                    {
                        m_robotSelect = ListTRB[1];
                    }
                    else
                        switch (btn.Text)
                        {
                            case "RobotA":
                            case "Robot A":
                                m_robotSelect = ListTRB[0];

                                break;
                            case "RobotB":
                            case "Robot B":
                                m_robotSelect = ListTRB[1];

                                break;
                            default:
                                m_robotSelect = null;
                                break;
                        }

                    if (m_robotSelect == null)
                    {
                        new frmMessageBox("Robot isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }
                    else
                    {

                    }
                    continue;
                }
                else
                    m_btnSelectRobotList[i].BackColor = System.Drawing.SystemColors.ControlLight;
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

        #region 選片功能

        class clsSelectWaferInfo
        {
            private int m_nSourceLpBodyNo = -1;
            private int m_nTargetLpBodyNo = -1;
            private int m_nSourceSlotIdx = -1;
            private int m_nTargetSlotIdx = -1;
            public clsSelectWaferInfo(int lpBodyNo, int sourceSlot, int targetSlot = -1)
            {
                m_nSourceLpBodyNo = lpBodyNo;
                m_nSourceSlotIdx = sourceSlot;
                m_nTargetSlotIdx = targetSlot;
            }
            public void SetTargetSlotIdx(int nIndex)
            {
                m_nTargetSlotIdx = nIndex;
            }
            public void SetTargetLpBodyNo(int nBodyNo)
            {
                m_nTargetLpBodyNo = nBodyNo;
            }
            public int SourceLpBodyNo { get { return m_nSourceLpBodyNo; } }
            public int TargetLpBodyNo { get { return m_nTargetLpBodyNo; } }
            public int SourceSlotIdx { get { return m_nSourceSlotIdx; } }
            public int TargetSlotIdx { get { return m_nTargetSlotIdx; } }
        }

        ConcurrentQueue<clsSelectWaferInfo> m_QueSelectSlotNum = new ConcurrentQueue<clsSelectWaferInfo>();//只有選Wafer，不確定去哪裡
        ConcurrentQueue<clsSelectWaferInfo> m_QueWaferJob = new ConcurrentQueue<clsSelectWaferInfo>();//紀錄Wafer傳片紀錄，clear的時候清掉      
        //使用者選擇Wafer判斷邏輯
        private void GuiLoadport_UseSelectWafer(object sender, GUILoadport.EventArgs_SelectWafer e)//GUI MouseUp
        {
            GUILoadport guiLoadport = sender as GUILoadport;

            if (e.SelectSlotNum.Count() > 0)
            {
                if (e.SelectSlotSts[0] == enumUIPickWaferStat.HasWafer ||
                    e.SelectSlotSts[0] == enumUIPickWaferStat.PutWafer)
                {
                    //只要是選片，若是清單還有Wafer，消除全部SelectFlag狀態，等於重新開始
                    if (m_QueSelectSlotNum.Count() > 0)
                    {
                        while (m_QueSelectSlotNum.Count() > 0)
                        {
                            clsSelectWaferInfo temp = null;
                            m_QueSelectSlotNum.TryDequeue(out temp);
                        }
                        foreach (var lp in m_guiloadportList)
                        {
                            if (lp.BodyNo != guiLoadport.BodyNo)
                                lp.ResetAllSelectSlot();
                        }
                    }

                    for (int i = 0; i < e.SelectSlotSts.Count(); i++)
                    {
                        int lp = guiLoadport.BodyNo;
                        int slot = e.SelectSlotNum[i];
                        clsSelectWaferInfo temp = new clsSelectWaferInfo(lp, slot);
                        m_QueSelectSlotNum.Enqueue(temp);


                    }

                    clsSelectWaferInfo temp1 = null;
                    m_QueSelectSlotNum.TryPeek(out temp1);

                    btnTeach.Text = GParam.theInst.GetLanguage("Loadport") + (char)(64 + guiLoadport.BodyNo) + "\r" + GParam.theInst.GetLanguage("Slot") + (temp1.SourceSlotIdx + 1) + " "
                        + GParam.theInst.GetLanguage("Teaching");

                    btnTeach.Enabled = true;

                    /*if (m_eTransferMode == enumTransferMode.Random)//如果是任意傳送才需要
                    {
                        foreach (var lp in m_guiloadportList)
                        {
                            if (m_QueSelectSlotNum.Count() > 0)
                                lp.EnableUISelectFlag(true);
                            else
                                lp.EnableUISelectFlag(false);
                        }
                    }*/


                }
                else if (e.SelectSlotSts[0] == enumUIPickWaferStat.NoWafer ||
                         e.SelectSlotSts[0] == enumUIPickWaferStat.ExeHasWafer)
                {
                    if (m_QueSelectSlotNum.Count() > 0)
                    {
                        /*for (int i = 0; i < e.SelectSlotSts.Count(); i++)
                        {
                            clsSelectWaferInfo temp = null;
                            if (m_QueSelectSlotNum.TryDequeue(out temp))
                            {
                                temp.SetTargetSlotIdx(e.SelectSlotNum[i]);
                                temp.SetTargetLpBodyNo(guiLoadport.BodyNo);

                                m_QueWaferJob.Enqueue(temp);

                                m_guiloadportList[guiLoadport.BodyNo - 1].PlaceWaferInLoadport(temp.SourceLpBodyNo, temp.SourceSlotIdx, temp.TargetSlotIdx);
                                m_guiloadportList[temp.SourceLpBodyNo - 1].ResetSlotSelectFlag(temp.SourceSlotIdx);

                            }
                            else
                                m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag(e.SelectSlotNum[i]);

                        }
                        if (m_QueSelectSlotNum.Count() == 0)
                        {
                            foreach (var lp in m_guiloadportList)
                            {
                                lp.EnableUISelectFlag(false);
                            }
                        }*/
                    }
                    else
                    {
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(false);
                        }
                    }
                }
                else
                {
                }
            }
        }

        private void btnUIPickWaferAllClear_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < ListSTG.Count; i++)
            {
                if (ListSTG[i].StatusMachine == enumStateMachine.PS_Process) continue;
                m_guiloadportList[i].ResetUpdateMappingData();
            }
            while (true)
            {
                clsSelectWaferInfo temp = new clsSelectWaferInfo(0, 0);
                if (m_QueWaferJob.Count() > 0)
                    m_QueWaferJob.TryDequeue(out temp);
                else
                    break;
            }
        }

        #endregion

        //啟用 Teaching 畫面
        private void frmTeachOCR_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                tmrUI.Enabled = this.Visible;

                _strUserName = _userManager.UserID;

                if (this.Visible)
                {

                    for (int i = 0; i < ListSTG.Count; i++)
                    {
                        if (ListSTG[i].Disable) continue;//220218 v1.003

                        ListSTG[i].OnUclmComplete -= OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        ListSTG[i].OnUclmComplete += OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        ListSTG[i].OnUclm1Complete -= OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖
                        ListSTG[i].OnUclm1Complete += OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖

                        ListSTG[i].OnFoupExistChenge -= OnLoadport_FoupExistChenge;          //  更新UI
                        ListSTG[i].OnFoupExistChenge += OnLoadport_FoupExistChenge;          //  更新UI

                        ListSTG[i].OnClmpComplete -= OnLoadport_MappingComplete;             //  更新UI
                        ListSTG[i].OnClmpComplete += OnLoadport_MappingComplete;             //  更新UI

                        ListSTG[i].OnMappingComplete -= OnLoadport_MappingComplete;          //  更新UI
                        ListSTG[i].OnMappingComplete += OnLoadport_MappingComplete;          //  更新UI

                        ListSTG[i].OnStatusMachineChange -= OnLoadport_StatusMachineChange;  //  更新UI
                        ListSTG[i].OnStatusMachineChange += OnLoadport_StatusMachineChange;  //  更新UI

                        ListSTG[i].OnFoupIDChange -= OnLoadport_FoupIDChange;                //  更新UI
                        ListSTG[i].OnFoupIDChange += OnLoadport_FoupIDChange;                //  更新UI

                        ListSTG[i].OnFoupTypeChange -= OnLoadport_FoupTypeChange;            //  更新UI
                        ListSTG[i].OnFoupTypeChange += OnLoadport_FoupTypeChange;            //  更新UI

                        //m_e84List[i].OnAceessModeChange -= OnLoadport_E84ModeChange;                //  更新UI
                        //m_e84List[i].OnAceessModeChange += OnLoadport_E84ModeChange;                //  更新UI
                    }

                    foreach (GUILoadport item in m_guiloadportList)//更新grouprecipe list
                    {
                        //if (m_dbGrouprecipe != null)
                        //{
                        //    item.SetRecipList(m_dbGrouprecipe.GetRecipeGroupList.Keys.ToArray(), GParam.theInst.GetDefaultRecipe);
                        //}

                        int nIndex = item.BodyNo - 1;
                        if (ListSTG[nIndex].Disable == false)
                        {
                            //初始值
                            OnLoadport_FoupExistChenge(ListSTG[nIndex], new FoupExisteChangEventArgs(ListSTG[nIndex].FoupExist));
                            OnLoadport_MappingComplete(ListSTG[nIndex], new LoadPortEventArgs(ListSTG[nIndex].MappingData, ListSTG[nIndex].BodyNo, true));
                            OnLoadport_StatusMachineChange(ListSTG[nIndex], new OccurStateMachineChangEventArgs(ListSTG[nIndex].StatusMachine));
                            OnLoadport_FoupIDChange(ListSTG[nIndex], new EventArgs());
                            OnLoadport_FoupTypeChange(ListSTG[nIndex], ListSTG[nIndex].FoupTypeName);

                            //OnLoadport_E84ModeChange(m_e84List[nIndex], new E84ModeChangeEventArgs(m_guiloadportList[nIndex].E84Status == GUILoadport.enumE84Status.Auto));
                        }
                    }

                    if (tabControl1.SelectedTab == tabPage1)
                    {
                        m_btnSelectAlignerList[0].PerformClick();
                        m_btnSelectRobotList[0].PerformClick();
                    }
                }
                else
                {
                    for (int i = 0; i < ListSTG.Count; i++)
                    {
                        ListSTG[i].OnUclmComplete -= OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        ListSTG[i].OnUclm1Complete -= OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖
                        ListSTG[i].OnFoupExistChenge -= OnLoadport_FoupExistChenge;          //  更新UI
                        ListSTG[i].OnClmpComplete -= OnLoadport_MappingComplete;             //  更新UI
                        ListSTG[i].OnMappingComplete -= OnLoadport_MappingComplete;          //  更新UI
                        ListSTG[i].OnStatusMachineChange -= OnLoadport_StatusMachineChange;  //  更新UI
                        ListSTG[i].OnFoupIDChange -= OnLoadport_FoupIDChange;                //  更新UI
                        ListSTG[i].OnFoupTypeChange -= OnLoadport_FoupTypeChange;            //  更新UI

                        //m_e84List[i].OnAceessModeChange -= OnLoadport_E84ModeChange;                //  更新UI
                    }


                }

            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }

        //========== 按鈕
        private void btnDock_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;

            if (!ListSTG[Loader.BodyNo - 1].FoupExist) return;

            if (ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Arrived ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_UnDocked ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToLoad ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToUnload)
            {
                ListSTG[Loader.BodyNo - 1].CLMP();
            }
            else if (ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Docked)
            {
                //ListSTG[Loader.BodyNo - 1].WMAP();
                string strMsg = string.Format("Loadport satus {0} can't dock", ListSTG[Loader.BodyNo - 1].StatusMachine);
                new frmMessageBox(strMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
            }
        }
        private void btnUnDock_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;

            if (!ListSTG[Loader.BodyNo - 1].FoupExist) return;

            if (ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Docked ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Stop ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Complete)
            {
                ListSTG[Loader.BodyNo - 1].UCLM();
            }
        }
        private void chkFoupOn_Checked(object sender, EventArgs e)//Simulate
        {
            GUILoadport Loader = (GUILoadport)sender;
            try
            {
                ListSTG[Loader.BodyNo - 1].SimulateFoupOn(!ListSTG[Loader.BodyNo - 1].FoupExist);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void btnTeach_Click(object sender, EventArgs e)
        {
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "Teach Angle", _strUserName, "OCR", "Teach start");

            if (m_aligner == null)
            {
                frmMessageBox frm = new frmMessageBox("Please select Aligner.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                frm.ShowDialog();
                return;
            }
            if (m_aligner.Disable)
            {
                frmMessageBox frm = new frmMessageBox("The Aligner is disable.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                frm.ShowDialog();
                return;
            }

            //  判斷選擇的WAFER
            SWafer waferData = null;
            m_nSTG_address = m_SelectWafer = -1;

            //clsSelectWaferInfo temp1 = null;
            clsSelectWaferInfo temp1 = null;
            if (m_QueSelectSlotNum.TryPeek(out temp1) == false)
            {
                frmMessageBox frm = new frmMessageBox("Pls select wafer", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            m_loadport = ListSTG[temp1.SourceLpBodyNo - 1];
            if (m_loadport.Disable)
            {
                frmMessageBox frm = new frmMessageBox("Loadport disable", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }
            if (m_loadport.StatusMachine != enumStateMachine.PS_Docked)
            {
                frmMessageBox frm = new frmMessageBox("Loadport isn't docked", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }
            if (m_loadport.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
            {
                frmMessageBox frm = new frmMessageBox("Loadport is frame type", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            #region 判斷Loadport對應能取放的Robot
            foreach (I_Robot robot in ListTRB.ToArray())
            {
                if (robot.RobotHardwareAllow(SWafer.enumPosition.Loader1 + m_loadport.BodyNo - 1))
                {
                    m_robotLoadUnld = robot;
                    break;
                }
            }
            if (m_robotLoadUnld == null)
            {
                frmMessageBox frm = new frmMessageBox("No Robot available", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                frm.ShowDialog();
                return;
            }
            #endregion

            #region 判斷Robot放Aligner
            if (m_robotLoadUnld.RobotHardwareAllow(SWafer.enumPosition.AlignerA + m_aligner.BodyNo - 1) == false)
            {
                frmMessageBox frm = new frmMessageBox("Aligner available for robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                frm.ShowDialog();
                return;
            }
            m_nALN_address = GParam.theInst.GetDicPosRobot(m_robotLoadUnld.BodyNo, enumRbtAddress.ALN1 + m_aligner.BodyNo - 1);
            #endregion

            #region 判斷 loadport 對應到 robot address
            SWafer.enumPosition ePosition = SWafer.enumPosition.Loader1 + m_loadport.BodyNo - 1;
            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotLoadUnld.BodyNo, ePosition, m_loadport.UseAdapter) + (int)m_loadport.eFoupType;
            #endregion

            m_eNotchAngle = enumNotchAngle.Total;
            string strAln = "Aligner" + (char)(64 + m_aligner.BodyNo);//這有不能翻譯
            string strRbt = "Robot" + (char)(64 + m_robotSelect.BodyNo);//這有不能翻譯
            string strStg = "LoadPort" + (char)(64 + m_loadport.BodyNo);//這有不能翻譯
            foreach (enumNotchAngle eType in Enum.GetValues(typeof(enumNotchAngle)))
            {
                if (GetEnumDescription(eType).Contains(strAln)
                 && GetEnumDescription(eType).Contains(strRbt)
                 && GetEnumDescription(eType).Contains(strStg))
                {
                    m_eNotchAngle = eType;
                    break;
                }
            }

            waferData = m_loadport.Waferlist[temp1.SourceSlotIdx];//Wafer資料
            m_SelectWafer = temp1.SourceSlotIdx + 1;//    Wafer slot

            if (m_SelectWafer == -1 || m_nSTG_address == -1 || waferData == null)
            {
                frmMessageBox frm = new frmMessageBox("Pls select wafer", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            switch (waferData.Owner)
            {
                case SWafer.enumFromLoader.LoadportA: if (ListSTG[0].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportB: if (ListSTG[1].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportC: if (ListSTG[2].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportD: if (ListSTG[3].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportE: if (ListSTG[4].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportF: if (ListSTG[5].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportG: if (ListSTG[6].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportH: if (ListSTG[7].StatusMachine != enumStateMachine.PS_Docked) return; break;
            }

            //  主選單上鎖
            delegateMDILock?.Invoke(true);
            //  切到第二頁  
            tabControl1.SelectedTab = tabPage2;
            TeachOCRStart();

        }
        private string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if ((attributes != null) && (attributes.Length > 0))
                return attributes[0].Description;
            else
                return value.ToString();
        }

        //   開始Teaching
        private void TeachOCRStart()
        {
            EnableProcedureButton1(true);
            EnableControlButton1(false);

            string strAln = "Aligner" + (char)(64 + m_aligner.BodyNo);
            string strRbt = "Robot" + (char)(64 + m_robotSelect.BodyNo);
            string strStg = "LoadPort" + (char)(64 + m_loadport.BodyNo);//這有不能翻譯
            foreach (enumNotchAngle eType in Enum.GetValues(typeof(enumNotchAngle)))
            {
                if (DicAngleSub.ContainsKey(eType) == false) continue;

                TextBox txt = DicAngleSub[eType];
                txt.Text = GParam.theInst.GetNotchData(eType).ToString();//開始教導更新現在的角度

                if (GetEnumDescription(eType).Contains(strAln)
                 && GetEnumDescription(eType).Contains(strRbt)
                 && GetEnumDescription(eType).Contains(strStg))
                {
                    txt.Enabled = true;
                }
                else
                {
                    txt.Enabled = false;
                }
            }

            foreach (I_Robot item in ListTRB)
            {
                if (item.Disable) continue;
                item.ResetProcessCompleted();
                item.SspdW(item.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(item.BodyNo - 1));//開始切Maint速度
                item.WaitProcessCompleted(item.GetAckTimeout);
            }

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Button [Next] to start the process\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Button [Next] to start the process\r"));

            m_eStep = eTeachStep.Prepare;
            Cursor.Current = Cursors.WaitCursor;
        }

        #region ========== tabpage2 teaching
        //  Button
        private void btnNext_Click(object sender, EventArgs e)
        {
            EnableProcedureButton1(false);
            switch (m_eStep)
            {
                case eTeachStep.Prepare:
                    DoPrepareTask();
                    break;
                case eTeachStep.Teach:
                    DoTeachTask(false);
                    break;
                case eTeachStep.End:
                    ProcessEnd();
                    break;
            }
        }
        //  Button
        private void btnCancel_Click(object sender, EventArgs e)
        {
            EnableProcedureButton1(false);
            switch (m_eStep)
            {
                case eTeachStep.Prepare:
                    DoTeachTask(true);
                    break;
                case eTeachStep.Teach:
                    DoTeachTask(true);
                    break;
                case eTeachStep.End:
                    ProcessEnd();
                    break;
            }
        }
        //  Button
        private void btnAlignerRotBW_Click(object sender, EventArgs e)//CCW
        {
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachOCR", _strUserName, "OCR", btn.Name + " " + m_nStep.ToString());

            EnableProcedureButton1(false);
            EnableControlButton1(false);
            m_aligner.OnRot1StepComplete -= _aligner_StepComplete;
            m_aligner.OnRot1StepComplete += _aligner_StepComplete;
            m_aligner.Rot1STEP(m_nStep);
        }
        //  Button
        private void btnAlignerRotFW_Click(object sender, EventArgs e)//CW
        {
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachOCR", _strUserName, "OCR", btn.Name + " " + (-1 * m_nStep).ToString());

            EnableProcedureButton1(false);
            EnableControlButton1(false);
            m_aligner.OnRot1StepComplete -= _aligner_StepComplete;
            m_aligner.OnRot1StepComplete += _aligner_StepComplete;
            m_aligner.Rot1STEP(-1 * m_nStep);
        }
        //  Intetlock
        private void EnableProcedureButton1(bool bAct)
        {
            ChangeButtun(btnCancel, bAct);
            ChangeButtun(btnNext, bAct);
        }
        //  Intetlock
        private void EnableControlButton1(bool bAct)
        {
            ChangeButtun(btnAlignerCW, bAct);
            ChangeButtun(btnAlignerCCW, bAct);
        }

        //  Step1 Next
        private void DoPrepareTask()    //  手臂 Load Unld 
        {
            m_robotLoadUnld.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                //過帳需要
                SWafer waferData = m_loadport.Waferlist[(m_SelectWafer - 1)];//Wafer資料
                robotManual.PrepareUpperWafer = waferData;

                if (waferData.Position == SWafer.enumPosition.UpperArm)
                {
                    //retry next load ok
                }
                else if (waferData.Position == SWafer.enumPosition.AlignerA && m_aligner.BodyNo == 1)
                {
                    //retry next load ok & unld aln ok
                }
                else if (waferData.Position == SWafer.enumPosition.AlignerB && m_aligner.BodyNo == 2)
                {
                    //retry next load ok & unld aln ok
                }
                else
                {
                    // LOAD STG
                    robotManual.TakeWaferByInterLockW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, m_nSTG_address, m_SelectWafer);
                }

                if (m_aligner.WaferExists())
                {

                }
                else
                {
                    // UNLD ALN
                    robotManual.PutWaferByInterLockW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, m_nALN_address, 1);
                }

                int nAngleTxt = GParam.theInst.GetNotchData(m_eNotchAngle);

                int nAngleFingerAndLoadport = 0;//補間有開啟要算這個角度

                robotManual.GtdtW(3000, enumRobotDataType.DCFG, m_nSTG_address);
                int nValue = int.Parse(robotManual.DCFGData[5]);
                if ((nValue & 0x0c) == 0x0c)//bit8 bit9
                {
                    //表示補間參數開啟
                    /*step1: 計算LP垂直面與手臂伸出的夾角
                      手臂教導的旋轉角度(dtrb) + 手臂原點與X軸夾角(90 - 70) + LP垂直面與手臂伸出的夾角 = 90度
                      90 - dtrb - 20 = LP垂直面與手臂伸出的夾角
                      step2: 計算最終角度
                      最終角度 = aligner轉notch對到finger角度 + LP垂直面與手臂伸出的夾角
                    */
                    robotManual.GtdtW(3000, enumRobotDataType.DTRB, m_nSTG_address);

                    int nRobotRotation = int.Parse(robotManual.DTRBData[4]);

                    if (m_bSimulate) nRobotRotation = 70000;

                    nAngleFingerAndLoadport = 90000 - nRobotRotation - GParam.theInst.GetRobot_AngleBetweenOrgnAndXaxis(robotManual.BodyNo - 1);
                }

                int nAngle = nAngleTxt - nAngleFingerAndLoadport;

                //robot dtrb 
                while (nAngle > 360000)
                {
                    nAngle -= 360000;
                }


                robotManual.MoveToStandbyByInterLockW(robotManual.GetAckTimeout, false, enumRobotArms.UpperArm, m_nALN_address, 1);

                m_aligner.ResetInPos();
                m_aligner.AlgnDW(3000, (((float)nAngle) / 1000).ToString());
                m_aligner.WaitInPos(10000);

                m_aligner.GposRW(3000); //  問位置

                int PosW = m_aligner.Raxispos;
                if (PosW == 0) PosW = 360000;
                PoseW = ((float)PosW) / 1000; //Aligner角度
                AngleE.Text = PoseW.ToString();

                m_aligner.ResetInPos();
                m_aligner.ClmpW(3000);//Lift pin down
                m_aligner.WaitInPos(10000);

                robotManual.ResetInPos();
                robotManual.ExtdW(robotManual.GetAckTimeout, 1, enumRobotArms.UpperArm, m_nALN_address, 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);
            };

            m_robotLoadUnld.OnManualCompleted -= FinishPrepareTask;
            m_robotLoadUnld.OnManualCompleted += FinishPrepareTask;

            m_robotLoadUnld.StartManualFunction();
            rtbInstruct.Clear();
            rtbInstruct.AppendText("Executing Robot move wafer to Alinger,please Wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Executing Robot move wafer to Alinger,please Wait.\r"));
            Cursor.Current = Cursors.WaitCursor;
        }
        //  Step1 Finish
        private void FinishPrepareTask(object sender, bool bSuc)
        {
            m_robotLoadUnld.OnManualCompleted -= FinishPrepareTask;

            rtbInstruct.Clear();
            rtbInstruct.AppendText(bSuc ? "Start Teaching Notch Angle, click[Next] when finished\r" : "Executing Robot move wafer to Alinger fail,click[Next] when retry\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage(bSuc ? "Start Teaching Notch Angle, click[Next] when finished\r" : "Executing Robot move wafer to Alinger fail,click[Next] when retry\r"));

            if (bSuc)
            {
                m_eStep = eTeachStep.Teach;

                int PosW = m_aligner.Raxispos;

                if (PosW == 0)
                    PosW = 360000;

                //PosW目前Aligner 動作的角度位置(pulse)
                PoseW = ((float)PosW) / 1000; //Aligner角度

                if (m_bSimulate == false)
                {
                    AngleE.Text = PoseW.ToString();
                }
                else
                {
                    //if (m_aligner.BodyNo == 1)
                    //    AngleE.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Angle_A.ToString();
                    //else if (m_aligner.BodyNo == 2)
                    //    AngleE.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Angle_B.ToString();

                }
            }
            EnableControlButton1(true);
            EnableProcedureButton1(true);//給他按NEXT
            Cursor.Current = Cursors.Default;
        }
        //  Step1 Cancel
        private void SkipPrepareTask()
        {

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Teaching is over, please click Next\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Teaching is over, please click Next\r"));
            m_eStep = eTeachStep.End;

            EnableProcedureButton1(true);
            Cursor.Current = Cursors.Default;
        }

        //  Step2 Next
        private void DoTeachTask(bool bCancel)      //  回收wafer
        {
            if (bCancel)
                if (new frmMessageBox("Parameter will not be recorded. Are you sure you want to give up the teaching?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != DialogResult.Yes)
                {
                    EnableProcedureButton1(true);
                    return;
                }

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Please wait for wafer recover\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Please wait for wafer recover\r"));

            m_robotLoadUnld.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                if (bCancel == false)
                {
                    int nAngleFingerAndLoadport = 0;//補間有開啟要算這個角度
                    robotManual.GtdtW(3000, enumRobotDataType.DCFG, m_nSTG_address);
                    int nValue = int.Parse(robotManual.DCFGData[5]);
                    if ((nValue & 0x0c) == 0x0c)//bit8 bit9
                    {
                        //表示補間參數開啟
                        /*step1: 計算LP垂直面與手臂伸出的夾角
                          手臂教導的旋轉角度(dtrb) + 手臂原點與X軸夾角(90 - 70) + LP垂直面與手臂伸出的夾角 = 90度
                          90 - dtrb - 20 = LP垂直面與手臂伸出的夾角
                          step2: 計算最終角度
                          最終角度 = aligner轉notch對到finger角度 + LP垂直面與手臂伸出的夾角
                       */

                        robotManual.GtdtW(3000, enumRobotDataType.DTRB, m_nSTG_address);

                        int nRobotRotation = int.Parse(robotManual.DTRBData[4]);

                        if (nRobotRotation > 180000)
                            nRobotRotation -= 180000;

                        if (m_bSimulate) nRobotRotation = 70000;

                        nAngleFingerAndLoadport = 90000 - nRobotRotation - GParam.theInst.GetRobot_AngleBetweenOrgnAndXaxis(robotManual.BodyNo - 1);

                    }

                    m_aligner.GposRW(3000); //  問位置

                    int nAngle = m_aligner.Raxispos;

                    int nAngleTxt = nAngle + nAngleFingerAndLoadport;

                    GParam.theInst.SetNotchData(m_eNotchAngle, nAngleTxt);//結果儲存
                }

                //過帳需要
                SWafer waferData = m_loadport.Waferlist[(m_SelectWafer - 1)];//Wafer資料
                //SWafer waferData = m_aligner.Wafer;//Wafer資料
                robotManual.PrepareUpperWafer = waferData;

                //使用的robot先收回
                robotManual.MoveToStandbyByInterLockW(robotManual.GetAckTimeout, false, enumRobotArms.UpperArm, m_nALN_address, 1);

                m_aligner.ResetInPos();
                m_aligner.UclmW(3000);
                m_aligner.WaitInPos(10000);

                if ((waferData.Position == SWafer.enumPosition.AlignerA && m_aligner.BodyNo == 1) ||
                    (waferData.Position == SWafer.enumPosition.AlignerB && m_aligner.BodyNo == 2))
                {
                    robotManual.TakeWaferByInterLockW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, m_nALN_address, 1);
                }

                if (waferData.Position == SWafer.enumPosition.UpperArm)
                {
                    robotManual.PutWaferByInterLockW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, m_nSTG_address, m_SelectWafer);
                }

                waferData.ProcessStatus = SWafer.enumProcessStatus.Sleep;
            };
            m_robotLoadUnld.OnManualCompleted -= FinishRobotPut;
            m_robotLoadUnld.OnManualCompleted += FinishRobotPut;
            m_robotLoadUnld.StartManualFunction();
            Cursor.Current = Cursors.WaitCursor;
        }
        //  Step2 Finish
        private void FinishRobotPut(object sender, bool bSuc)
        {
            m_robotLoadUnld.OnManualCompleted -= FinishRobotPut;

            rtbInstruct.Clear();
            if (bSuc)
            {
                rtbInstruct.AppendText("Teaching is over, please click Next\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Teaching is over, please click Next\r"));
                m_eStep = eTeachStep.End;
            }
            EnableProcedureButton1(true);
            Cursor.Current = Cursors.Default;
        }

        //  Step3
        private void ProcessEnd()
        {
            foreach (I_Robot item in ListTRB)//進入畫面自動降maint速度
            {
                if (item.Disable) continue;
                item.ResetProcessCompleted();
                int nSpeed = m_bIsRunMode ? GParam.theInst.GetRobot_RunSpeed(item.BodyNo - 1) : GParam.theInst.GetRobot_MaintSpeed(item.BodyNo - 1);
                item.SspdW(item.GetAckTimeout, nSpeed);//  回原速度
                item.WaitProcessCompleted(item.GetAckTimeout);
            }

            EnableControlButton1(false);
            tabControl1.SelectedTab = tabPage1;

            delegateMDILock?.Invoke(false);
        }
        //  Jog step的量更換
        private void rbStep_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton btn = (RadioButton)sender;

            if (false == btn.Checked)
                return;

            m_nStep = (int)(float.Parse(btn.Text.ToString()) * 1000);
        }
        #endregion

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
        private void tmrUI_Tick(object sender, EventArgs e)
        {
            tmrUI.Enabled = false;

            #region ==========Loadport
            for (int nPort = 0; nPort < ListSTG.Count; nPort++)
            {
                if (ListSTG[nPort].Disable) continue;

                m_guiloadportList[nPort].KeepClamp = ListSTG[nPort].IsKeepClamp;

                if (ListSTG[nPort].Waferlist == null) continue;

                for (int nSlot = 1; nSlot <= ListSTG[nPort].Waferlist.Count; nSlot++)
                {
                    SWafer waferShow = ListSTG[nPort].Waferlist[nSlot - 1];

                    if (waferShow == null)//empty
                    {
                        m_guiloadportList[nPort].UpdataWaferStatus(nSlot, "");
                        m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.None, SystemColors.Control);
                    }
                    else
                    {
                        switch (waferShow.ProcessStatus)
                        {
                            case SWafer.enumProcessStatus.Sleep:
                                m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Sleep, Color.LimeGreen);
                                break;
                            case SWafer.enumProcessStatus.WaitProcess:
                                m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.WaitProcess, Color.RoyalBlue);
                                break;
                            case SWafer.enumProcessStatus.Processing:
                                m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Processing, SystemColors.Control);
                                break;
                            case SWafer.enumProcessStatus.Processed:
                                {
                                    if (waferShow.WaferIDComparison == SWafer.enumWaferIDComparison.IDAbort)
                                        m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Abort, Color.Red);
                                    else
                                        m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Processed, Color.HotPink);
                                }
                                break;
                            case SWafer.enumProcessStatus.Error:
                                m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Error, Color.Red);
                                break;
                        }

                        m_guiloadportList[nPort].UpdataWaferStatus(nSlot, waferShow.WaferID_F, waferShow.WaferID_B, waferShow.WaferInforID_F, waferShow.WaferInforID_B, waferShow.Position);
                    }

                }
            }
            #endregion

            //  閃燈
            foreach (object value in Enum.GetValues(typeof(eTeachStep)))
            {
                if ((int)value < (int)m_eStep)
                {
                    lstLabStep[(int)value].BackColor = Color.Orange;
                }
                else if ((int)value == (int)m_eStep)
                {
                    m_bBlink = (false == m_bBlink);
                    lstLabStep[(int)value].BackColor = m_bBlink ? Color.Orange : SystemColors.Control;
                }
                else
                {
                    lstLabStep[(int)value].BackColor = SystemColors.Control;
                }
            }


            tmrUI.Enabled = true;
        }

        //  Loadport 註冊事件來更新 UI
        void OnLoadport_OnUclmComplete(object sender, LoadPortEventArgs e)
        {
            //I_Loadport loaderUnit = sender as I_Loadport;           
        }
        void OnLoadport_OnUclm1Complete(object sender, LoadPortEventArgs e)
        {
            //I_Loadport loaderUnit = sender as I_Loadport;           
        }
        void OnLoadport_FoupExistChenge(object sender, FoupExisteChangEventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int index = loaderUnit.BodyNo - 1;
            m_guiloadportList[index].UpdataFoupExist(e.FoupExist);

            if (e.FoupExist == false)
                btnTeach.Enabled = false; //  沒有FOUP不能按開始
        }
        void OnLoadport_MappingComplete(object sender, LoadPortEventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int index = loaderUnit.BodyNo - 1;
            string strMapData = e.MappingData;
            m_guiloadportList[index].UpdataMappingData(strMapData);
        }
        void OnLoadport_StatusMachineChange(object sender, OccurStateMachineChangEventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int index = loaderUnit.BodyNo - 1;
            enumStateMachine eStatus = e.StatusMachine;
            switch (eStatus)
            {
                case enumStateMachine.PS_Abort:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Abort;
                    break;
                case enumStateMachine.PS_Arrived:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Arrived;
                    break;
                case enumStateMachine.PS_Clamped:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Clamped;
                    break;
                case enumStateMachine.PS_Complete:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Complete;
                    break;
                case enumStateMachine.PS_Disable:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Disable;
                    break;
                case enumStateMachine.PS_Docked:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Docked;
                    break;
                case enumStateMachine.PS_Docking:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Docking;
                    break;
                case enumStateMachine.PS_Error:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Error;
                    break;
                case enumStateMachine.PS_FoupOn:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.FoupOn;
                    break;
                case enumStateMachine.PS_FuncSetup:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.FuncSetup;
                    break;
                case enumStateMachine.PS_FuncSetupNG:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.FuncSetupNG;
                    break;
                case enumStateMachine.PS_Process:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Process;
                    break;
                case enumStateMachine.PS_ReadyToLoad:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.ReadyToLoad;
                    break;
                case enumStateMachine.PS_ReadyToUnload:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.ReadyToUnload;
                    break;
                case enumStateMachine.PS_Removed:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Removed;
                    break;
                case enumStateMachine.PS_Stop:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Stop;
                    break;
                case enumStateMachine.PS_UnClamped:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.UnClamped;
                    break;
                case enumStateMachine.PS_UnDocked:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.UnDocked;
                    break;
                case enumStateMachine.PS_UnDocking:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.UnDocking;
                    break;
                case enumStateMachine.PS_Unknown:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Unknown;
                    break;
            }

        }
        void OnLoadport_FoupIDChange(object sender, EventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int nIndex = loaderUnit.BodyNo - 1;
            m_guiloadportList[nIndex].FoupID = loaderUnit.FoupID;
        }
        void OnLoadport_FoupTypeChange(object sender, string strName)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int nIndex = loaderUnit.BodyNo - 1;

            m_guiloadportList[nIndex].InfoPadName = strName;

        }
        void OnLoadport_E84ModeChange(object sender, E84ModeChangeEventArgs e)
        {
            I_E84 thenumUnit = sender as I_E84;
            if (thenumUnit.Disable) return;
            int index = thenumUnit.BodyNo - 1;
            m_guiloadportList[index].E84Status = e.Auto ? GUILoadport.enumE84Status.Auto : GUILoadport.enumE84Status.Manual;
        }


        private void _aligner_StepComplete(object sender, bool bSuc)
        {
            m_aligner.OnRot1StepComplete -= _aligner_StepComplete;

            int PosW = m_aligner.Raxispos;

            if (PosW == 0)
                PosW = 360000;
            //PosW目前Aligner 動作的角度位置(pulse)

            PoseW = ((float)PosW) / 1000; //Aligner角度

            AngleE.Text = PoseW.ToString();

            EnableProcedureButton1(true);
            EnableControlButton1(true);
            Cursor.Current = Cursors.WaitCursor;
        }




        #region tabPage3
        private void btnModify_Click(object sender, EventArgs e)
        {
            foreach (enumNotchAngle eType in Enum.GetValues(typeof(enumNotchAngle)))
            {
                if (DicAngleAll.ContainsKey(eType) == false) continue;
                if (eType == enumNotchAngle.Total) continue;
                DicAngleAll[eType].Text = GParam.theInst.GetNotchData(eType).ToString();
            }


            tabControl1.SelectedTab = tabPage3;
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage1;
        }
        private void btnSave_Click_1(object sender, EventArgs e)
        {
            SaveData();
        }
        private void SaveData()
        {
            btnSave.Enabled = btnBack.Enabled = false;
            grpModifyData.Enabled = false;
            foreach (enumNotchAngle eType in Enum.GetValues(typeof(enumNotchAngle)))
            {
                if (eType == enumNotchAngle.Total) continue;
                if (DicAngleAll.ContainsKey(eType) == false) continue;

                string strValue = DicAngleAll[eType].Text;
                int nValue;
                if (int.TryParse(strValue, out nValue) == false) continue;
                GParam.theInst.SetNotchData(eType, nValue);
            }
            btnSave.Enabled = btnBack.Enabled = true;
            grpModifyData.Enabled = true;
        }
        #endregion


    }
}
