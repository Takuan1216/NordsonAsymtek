using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RorzeApi.Class;
using RorzeUnit.Interface;
using RorzeUnit.Class;
using RorzeComm.Log;
using System.Collections.Generic;
using RorzeApi.GUI;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Robot.Enum;
using RorzeApi.SECSGEM;
using RorzeUnit.Class.E84.Event;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Data;
using static RorzeUnit.Class.SWafer;
using RorzeComm;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.Aligner;
using RorzeUnit.Class.Robot;
using RorzeUnit.Class.RC500.RCEnum;


namespace RorzeApi
{
    public partial class frmManual : Form
    {
        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制

        public delegate void DelegateMDITriggerShowMainform();
        public event DelegateMDITriggerShowMainform delegateMDITriggerShowMainform;

        public delegate void DelegateDemoStart(int sourceBodyNo, int targetBodyNo);
        public event DelegateDemoStart delegateDemoStart;

        public event EventHandler delegateCycleStart;

        #endregion

        float frmX;             //當前窗體的寬度
        float frmY;             //當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        List<I_Robot> ListTRB;
        List<I_Loadport> ListSTG;
        List<I_Aligner> ListALN;
        List<I_E84> ListE84;
        List<I_RC5X0_IO> ListDIO;
        List<I_Buffer> ListBUF;
        List<SSEquipment> ListEQM;

        I_Robot m_SelectTRB = null;
        I_Aligner m_SelectALN = null;

        private enumRC550Axis m_eXAX1 = enumRC550Axis.AXS1;

        private int m_nTower1to16;

        private int m_nStep = 1000;

        private SGEM300 m_Gem;
        private STransfer m_autoProcess;      //  自動傳片流程
        private SProcessDB _accessDBlog;
        private SPermission m_userManager;//  管理LOGIN使用者權限
        private SSSorterSQL m_DataBase;

        private SGroupRecipeManager m_dbGrouprecipe;
        private List<GUILoadport> m_guiloadportList = new List<GUILoadport>();
        private bool m_bIsRunMode;
        public dlgb_v dlgIsTkeyOn { get; set; }

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        public frmManual(List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN, List<I_E84> e84List, List<I_RC5X0_IO> listDIO, List<I_Buffer> listBUF,
        SProcessDB db, SPermission userManager, bool bSimulate, SGEM300 Gem, SGroupRecipeManager grouprecipe, STransfer autoProcess, SSSorterSQL dataBase, bool bIsRunMode, List<SSEquipment> listEQM)
        {
            try
            {
                InitializeComponent();
                //  Unit
                ListTRB = listTRB;
                ListSTG = listSTG;
                ListALN = listALN;
                ListE84 = e84List;
                ListDIO = listDIO;
                ListBUF = listBUF;

                _accessDBlog = db;
                ListEQM = listEQM;

                m_userManager = userManager;

                m_Gem = Gem;
                cbxRobotArm.SelectedIndex = 0;
                cbxRobotStage.SelectedIndex = 0;
                m_dbGrouprecipe = grouprecipe;
                m_autoProcess = autoProcess;//cycle
                m_DataBase = dataBase;
                m_bIsRunMode = bIsRunMode;

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
                    m_guiloadportList[i].Disable_E84 = true /*GParam.theInst.IsE84Disable(i)*/;
                    m_guiloadportList[i].Disable_OCR = true /*GParam.theInst.IsOcrAllDisable()*/;
                    m_guiloadportList[i].Disable_Recipe = true/*(GParam.theInst.IsOcrAllDisable() && GParam.theInst.GetEQRecipeCanSelect == false)*/;
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
                    ListE84[i].OnAceessModeChange += OnLoadport_E84ModeChange;                //  更新UI
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
                    //m_guiloadportList.UseSelectWafer += GuiLoadport_UseSelectWafer;//選片功能
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
                    if (tabPageABCD.Text != "" && tabPageABCD.Text.Length == 4) tabPageABCD.Text = "EFEM";//長官建議顯示這樣比較適當

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
                            item.Height = flowLayoutPanel1.Height - 5;
                        }
                    }
                    else
                    {
                        foreach (GUILoadport item in m_guiloadportList)
                        {
                            item.Width = (flowLayoutPanel1.Width - 5) / nCount;
                            item.Height = flowLayoutPanel1.Height - 5;
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
                #region Select Robot Button
                //  消失頁籤
                tabControl1.SizeMode = TabSizeMode.Fixed;
                tabControl1.ItemSize = new Size(0, 1);

                tlpSelectRobot.RowStyles.Clear();
                tlpSelectRobot.ColumnStyles.Clear();
                tlpSelectRobot.Dock = DockStyle.Top;
                for (int i = 0; i < ListTRB.Count; i++)//移除第二支
                {
                    if (ListTRB[i].Disable) continue;
                    tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                    Button btn = new Button();
                    btn.Font = new Font("微軟正黑體", 14, FontStyle.Bold);
                    //btn.Text = "Robot " + (char)(64 + ListTRB[i].BodyNo);
                    btn.Text = GParam.theInst.GetLanguage("Robot" + (char)(64 + ListTRB[i].BodyNo));
                    btn.Dock = DockStyle.Fill;
                    btn.TextAlign = ContentAlignment.MiddleCenter;
                    //btn.Click += btnSelectRobot_Click;
                    btn.Click += (object sender, EventArgs e) =>
                    {
                        Button button = sender as Button;
                        foreach (Button item in tlpSelectRobot.Controls)
                        {
                            if (item == button)
                            {
                                item.BackColor = Color.LightBlue;

                                string strName1 = GParam.theInst.GetLanguage("RobotA");
                                string strName2 = GParam.theInst.GetLanguage("RobotB");

                                if (strName1 == btn.Text)
                                {
                                    tabControl1.SelectedTab = tabPage1;
                                    m_SelectTRB = ListTRB[0];
                                }
                                else if (strName2 == btn.Text)
                                {
                                    tabControl1.SelectedTab = tabPage2;
                                    m_SelectTRB = ListTRB[1];
                                }
                                else
                                    switch (button.Text)
                                    {
                                        case "Robot A": m_SelectTRB = ListTRB[0]; break;
                                        case "Robot B": m_SelectTRB = ListTRB[1]; break;
                                        default: m_SelectTRB = null; break;
                                    }

                                cbxRobotStage.Items.Clear();
                                cbxShutterDoor.Items.Clear();
                                if (m_SelectTRB != null)
                                    foreach (RorzePosition pos in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo).ToArray())
                                    {
                                        if (pos.Stge0to399 < 0) continue;

                                        switch (pos.strDefineName)
                                        {
                                            case enumRbtAddress.STG1_12: if (ListSTG[0] != null && ListSTG[0].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG2_12: if (ListSTG[1] != null && ListSTG[1].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG3_12: if (ListSTG[2] != null && ListSTG[2].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG4_12: if (ListSTG[3] != null && ListSTG[3].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG5_12: if (ListSTG[4] != null && ListSTG[4].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG6_12: if (ListSTG[5] != null && ListSTG[5].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG7_12: if (ListSTG[6] != null && ListSTG[6].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG8_12: if (ListSTG[7] != null && ListSTG[7].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.ALN1: if (ListALN[0] != null && ListALN[0].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.ALN2: if (ListALN[1] != null && ListALN[1].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.BUF1: if (ListBUF[0] != null && ListBUF[0].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.BUF2: if (ListBUF[1] != null && ListBUF[1].Disable == false) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG1_08: if (ListSTG[0] != null && ListSTG[0].Disable == false && ListSTG[0].UseAdapter) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG2_08: if (ListSTG[1] != null && ListSTG[1].Disable == false && ListSTG[1].UseAdapter) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG3_08: if (ListSTG[2] != null && ListSTG[2].Disable == false && ListSTG[2].UseAdapter) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG4_08: if (ListSTG[3] != null && ListSTG[3].Disable == false && ListSTG[3].UseAdapter) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG5_08: if (ListSTG[4] != null && ListSTG[4].Disable == false && ListSTG[4].UseAdapter) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG6_08: if (ListSTG[5] != null && ListSTG[5].Disable == false && ListSTG[5].UseAdapter) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG7_08: if (ListSTG[6] != null && ListSTG[6].Disable == false && ListSTG[6].UseAdapter) cbxRobotStage.Items.Add(pos.strDisplayName); break;
                                            case enumRbtAddress.STG8_08: if (ListSTG[7] != null && ListSTG[7].Disable == false && ListSTG[7].UseAdapter) cbxRobotStage.Items.Add(pos.strDisplayName); break;

                                            case enumRbtAddress.EQM1: if (ListEQM[0].Disable == false) { cbxRobotStage.Items.Add(pos.strDisplayName); cbxShutterDoor.Items.Add(pos.strDisplayName); } break;
                                            case enumRbtAddress.EQM2: if (ListEQM[1].Disable == false) { cbxRobotStage.Items.Add(pos.strDisplayName); cbxShutterDoor.Items.Add(pos.strDisplayName); } break;
                                            case enumRbtAddress.EQM3: if (ListEQM[2].Disable == false) { cbxRobotStage.Items.Add(pos.strDisplayName); cbxShutterDoor.Items.Add(pos.strDisplayName); } break;
                                            case enumRbtAddress.EQM4: if (ListEQM[3].Disable == false) { cbxRobotStage.Items.Add(pos.strDisplayName); cbxShutterDoor.Items.Add(pos.strDisplayName); } break;
                                        }
                                    }
                                if (cbxRobotStage.Items.Count != -1) cbxRobotStage.SelectedIndex = 0;
                                if (cbxShutterDoor.Items.Count > 0)
                                {
                                    cbxShutterDoor.SelectedIndex = 0;
                                }
                                else
                                {
                                    cbxShutterDoor.SelectedIndex = -1;
                                }
                            }
                            else
                            {
                                item.BackColor = System.Drawing.SystemColors.ControlLight;
                            }
                        }
                    };
                    tlpSelectRobot.Controls.Add(btn, tlpSelectRobot.Controls.Count, 0);
                }
                tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
                #endregion
                #region Select Aligner Button
                //  消失頁籤
                tabCtrAligner.SizeMode = TabSizeMode.Fixed;
                tabCtrAligner.ItemSize = new Size(0, 1);

                tlpSelectAligner.RowStyles.Clear();
                tlpSelectAligner.ColumnStyles.Clear();
                tlpSelectAligner.Dock = DockStyle.Top;
                for (int i = 0; i < ListALN.Count; i++)
                {
                    if (ListALN[i] == null || ListALN[i].Disable) continue;
                    tlpSelectAligner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                    Button btn = new Button();
                    btn.Font = new Font("微軟正黑體", 14, FontStyle.Bold);
                    btn.Text = GParam.theInst.GetLanguage("Aligner" + (char)(64 + ListALN[i].BodyNo));
                    btn.Dock = DockStyle.Fill;
                    btn.TextAlign = ContentAlignment.MiddleCenter;
                    btn.Click += (object sender, EventArgs e) =>
                    {
                        Button button = sender as Button;
                        foreach (Button item in tlpSelectAligner.Controls)
                        {
                            if (item == button)
                            {
                                item.BackColor = Color.LightBlue;
                                string strName1 = GParam.theInst.GetLanguage("AlignerA");
                                string strName2 = GParam.theInst.GetLanguage("AlignerB");
                                if (strName1 == btn.Text)
                                {
                                    m_SelectALN = ListALN[0];
                                    btnAlignerAlgn.Visible = true;
                                }
                                else if (strName2 == btn.Text)
                                {
                                    m_SelectALN = ListALN[1];
                                    btnAlignerAlgn.Visible = false;
                                }
                                else
                                    switch (button.Text)
                                    {
                                        case "Aligner A": m_SelectALN = ListALN[0]; break;
                                        case "Aligner B": m_SelectALN = ListALN[1]; break;
                                        default: m_SelectALN = null; break;
                                    }

                                if (m_SelectALN is SSAlignerPanelXYR)
                                {
                                    tabCtrAligner.Visible = true;
                                    btnAlignerAlgn.Visible = true;
                                    tabCtrAligner.SelectedTab = tabPagePanel;
                                    //PanelAligner
                                    guiNotchAngle1.OnAngleChange += btnAlignerRotAbs_Click;
                                }
                                else if (m_SelectALN is SSAlignerTurnTable)
                                {
                                    tabCtrAligner.Visible = true;
                                    btnAlignerAlgn.Visible = false;
                                    tabCtrAligner.SelectedTab = tabPageTurnTable;
                                }
                                else
                                {
                                    tabCtrAligner.Visible = false;
                                    btnAlignerAlgn.Visible = true;
                                }


                            }
                            else
                            {
                                item.BackColor = System.Drawing.SystemColors.ControlLight;
                            }
                        }
                    };
                    tlpSelectAligner.Controls.Add(btn, tlpSelectAligner.Controls.Count, 0);
                }
                tlpSelectAligner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));

                if (GParam.theInst.IsAllAlnDisable())
                {
                    gpbALN.Visible = false;
                }
                #endregion

                cbxCycleMode.Items.Clear();
                cbxCycleMode.Items.Add("EFEM");
                cbxCycleMode.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        #region ========== Form Zoom ===============
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
        #region ========== Loadport Button =========
        private void btnDock_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_autoProcess.IsCycle)
                {
                    new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                //TkeyOn
                if (dlgIsTkeyOn != null && dlgIsTkeyOn())
                {
                    new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }

                GUILoadport Loader = (GUILoadport)sender;

                if (!ListSTG[Loader.BodyNo - 1].FoupExist)
                {
                    new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "STG" + Loader.BodyNo, "Dock Click");
                if (ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Arrived ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_UnDocked ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToUnload)
                {
                    ListSTG[Loader.BodyNo - 1].CLMP();
                }
                else
                {
                    string strMsg = string.Format("Loadport satus {0} can't dock", ListSTG[Loader.BodyNo - 1].StatusMachine);
                    new frmMessageBox(strMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                }

            }
            catch (SException ex)
            {
                new frmMessageBox(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                WriteLog("[SException] " + ex);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        private void btnUnDock_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_autoProcess.IsCycle)
                {
                    new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                //TkeyOn
                if (dlgIsTkeyOn != null && dlgIsTkeyOn())
                {
                    new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
                GUILoadport Loader = (GUILoadport)sender;
                if (!ListSTG[Loader.BodyNo - 1].FoupExist)
                {
                    new frmMessageBox("Loadport has no foup.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "STG" + Loader.BodyNo, "UnDock Click");
                if (ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Docked ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Stop ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Complete ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped)//211227 增加Clamped
                {
                    ListSTG[Loader.BodyNo - 1].UCLM();
                }
                else
                {
                    string strMsg = string.Format("Loadport satus {0} can't undock", ListSTG[Loader.BodyNo - 1].StatusMachine);
                    new frmMessageBox(strMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                }
            }
            catch (SException ex)
            {
                new frmMessageBox(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                WriteLog("[SException] " + ex);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
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
                WriteLog("[Exception] " + ex);
            }
        }
        #endregion
        #region ========== Robot Button ============
        private void btnRbOrgn_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                if (robotManual.ExtXaxisDisable == false)
                {
                    robotManual.TBL_560.ResetProcessCompleted();
                    robotManual.TBL_560.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                    robotManual.TBL_560.WaitProcessCompleted(robotManual.GetAckTimeout);
                }

                robotManual.ResetProcessCompleted();
                robotManual.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                robotManual.WaitProcessCompleted(robotManual.GetAckTimeout);

                robotManual.ResetInPos();
                robotManual.OrgnW(robotManual.GetAckTimeout);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                if (robotManual.ExtXaxisDisable == false)
                {
                    robotManual.TBL_560.ResetInPos();
                    robotManual.TBL_560.ResetOrgnSinal();
                    robotManual.TBL_560.OrgnW(robotManual.GetAckTimeout, m_eXAX1);
                    robotManual.TBL_560.WaitInPos(300000);
                    robotManual.TBL_560.WaitOrgnCompleted(robotManual.GetAckTimeout);

                    robotManual.TBL_560.WtdtW(robotManual.GetAckTimeout);
                }
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }
        private void btnRbLoad_Click(object sender, EventArgs e)
        {
            #region Interlock       
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            int nStgeIndx = -1;
            SWafer targetWafer = null;
            //搜尋選擇的位置
            enumPosition ePosition = enumPosition.UnKnow;
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo).ToArray())
            {
                if (item.strDisplayName != cbxRobotStage.SelectedItem.ToString()) continue; //注意是顯示名稱相同
                switch (item.strDefineName)
                {
                    case enumRbtAddress.STG1_08:
                    case enumRbtAddress.STG1_12:
                        ePosition = enumPosition.Loader1;
                        break;
                    case enumRbtAddress.STG2_08:
                    case enumRbtAddress.STG2_12:
                        ePosition = enumPosition.Loader2;
                        break;
                    case enumRbtAddress.STG3_08:
                    case enumRbtAddress.STG3_12:
                        ePosition = enumPosition.Loader3;
                        break;
                    case enumRbtAddress.STG4_08:
                    case enumRbtAddress.STG4_12:
                        ePosition = enumPosition.Loader4;
                        break;
                    case enumRbtAddress.STG5_08:
                    case enumRbtAddress.STG5_12:
                        ePosition = enumPosition.Loader5;
                        break;
                    case enumRbtAddress.STG6_08:
                    case enumRbtAddress.STG6_12:
                        ePosition = enumPosition.Loader6;
                        break;
                    case enumRbtAddress.STG7_08:
                    case enumRbtAddress.STG7_12:
                        ePosition = enumPosition.Loader7;
                        break;
                    case enumRbtAddress.STG8_08:
                    case enumRbtAddress.STG8_12:
                        ePosition = enumPosition.Loader8;
                        break;
                    case enumRbtAddress.ALN1: ePosition = enumPosition.AlignerA; break;
                    case enumRbtAddress.ALN2: ePosition = enumPosition.AlignerB; break;
                    case enumRbtAddress.BUF1: ePosition = enumPosition.BufferA; break;
                    case enumRbtAddress.BUF2: ePosition = enumPosition.BufferB; break;
                    case enumRbtAddress.EQM1: ePosition = enumPosition.EQM1; break;
                    case enumRbtAddress.EQM2: ePosition = enumPosition.EQM2; break;
                    case enumRbtAddress.EQM3: ePosition = enumPosition.EQM3; break;
                    case enumRbtAddress.EQM4: ePosition = enumPosition.EQM4; break;
                }
                break;//找到了
            }
            //  防呆
            if (ePosition == enumPosition.UnKnow)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            switch (ePosition)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    {
                        I_Loadport stg = ListSTG[ePosition - enumPosition.Loader1];
                        if (stg.Disable)
                        {
                            new frmMessageBox("Loadport is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (stg.StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Please confirm the loadport dock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && stg.IsDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the loadport do not open door!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, stg.UseAdapter) + (int)stg.eFoupType;
                        targetWafer = stg.Waferlist[cbxRobotSlot.SelectedIndex];
                    }
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    {
                        I_Aligner aln = ListALN[ePosition - enumPosition.AlignerA];
                        if (aln == null || aln.Disable)
                        {
                            new frmMessageBox("Aligner is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = aln.Wafer;
                    }
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    {
                        I_Buffer buf = ListBUF[ePosition - enumPosition.BufferA];
                        if (buf.Disable)
                        {
                            new frmMessageBox("Buffer is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = buf.GetWafer(cbxRobotSlot.SelectedIndex);
                    }
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    {
                        SSEquipment eqm = ListEQM[ePosition - enumPosition.EQM1];
                        if (eqm == null || eqm.Disable)
                        {
                            new frmMessageBox("EQ is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && eqm.IsShutterDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the Shutter door is not open or not!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = eqm.Wafer;
                    }
                    break;
                default:
                    break;
            }
            //  防呆
            if (nStgeIndx == -1)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (targetWafer == null)
            {
                new frmMessageBox("The target has no wafer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                return;
            }
            //  load 手必須空，沒有WAFER
            enumRobotArms eRobotArms = (enumRobotArms)cbxRobotArm.SelectedIndex;
            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    if (m_SelectTRB.UpperArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    m_SelectTRB.PrepareUpperWafer = targetWafer;
                    break;
                case enumRobotArms.LowerArm:
                    if (m_SelectTRB.LowerArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    m_SelectTRB.PrepareLowerWafer = targetWafer;
                    break;
                case enumRobotArms.BothArms:
                    if (m_SelectTRB.UpperArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    if (m_SelectTRB.LowerArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    break;
                default:
                    break;
            }
            #endregion
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                robotManual.ResetProcessCompleted();
                robotManual.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                robotManual.WaitProcessCompleted(robotManual.GetAckTimeout);
                if (robotManual.ExtXaxisDisable == false)
                {
                    robotManual.TBL_560.ResetProcessCompleted();
                    robotManual.TBL_560.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                    robotManual.TBL_560.WaitProcessCompleted(robotManual.GetAckTimeout);
                }

                robotManual.ResetInPos();
                robotManual.MoveToStandbyPosW_Ext_Xaxis(robotManual.GetAckTimeout, false, ePosition, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                if (new frmMessageBox("Please check if the robot arm is extended?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    //if((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm)
                    robotManual.TakeWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, (enumRobotArms)cbxRobotArm.SelectedIndex, ePosition, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                    //else
                    //robotManual.TwoStepTakeWaferW(robotManual.GetAckTimeout, (enumRobotArms)cbxRobotArm.SelectedIndex, GParam.theInst.GetRobot_FrameTwoStepLoadArmBackPulse(m_SelectTRB.BodyNo - 1), nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                }
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }
        private void btnRbUnld_Click(object sender, EventArgs e)
        {
            #region Interlock   
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            int nStgeIndx = -1;
            SWafer targetWafer = null;
            SWafer armWafer = null;
            //  unld 手必須有WAFER
            enumRobotArms eRobotArms = (enumRobotArms)cbxRobotArm.SelectedIndex;
            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    if (m_SelectTRB.UpperArmWafer == null)
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    armWafer = m_SelectTRB.UpperArmWafer;
                    break;
                case enumRobotArms.LowerArm:
                    if (m_SelectTRB.LowerArmWafer == null)
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    armWafer = m_SelectTRB.LowerArmWafer;
                    break;
            }
            //搜尋選擇的位置
            enumPosition ePosition = enumPosition.UnKnow;
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo).ToArray())
            {
                if (item.strDisplayName != cbxRobotStage.SelectedItem.ToString()) continue;//注意是顯示名稱相同
                switch (item.strDefineName)
                {
                    case enumRbtAddress.STG1_08:
                    case enumRbtAddress.STG1_12:
                        ePosition = enumPosition.Loader1;
                        break;
                    case enumRbtAddress.STG2_08:
                    case enumRbtAddress.STG2_12:
                        ePosition = enumPosition.Loader2;
                        break;
                    case enumRbtAddress.STG3_08:
                    case enumRbtAddress.STG3_12:
                        ePosition = enumPosition.Loader3;
                        break;
                    case enumRbtAddress.STG4_08:
                    case enumRbtAddress.STG4_12:
                        ePosition = enumPosition.Loader4;
                        break;
                    case enumRbtAddress.STG5_08:
                    case enumRbtAddress.STG5_12:
                        ePosition = enumPosition.Loader5;
                        break;
                    case enumRbtAddress.STG6_08:
                    case enumRbtAddress.STG6_12:
                        ePosition = enumPosition.Loader6;
                        break;
                    case enumRbtAddress.STG7_08:
                    case enumRbtAddress.STG7_12:
                        ePosition = enumPosition.Loader7;
                        break;
                    case enumRbtAddress.STG8_08:
                    case enumRbtAddress.STG8_12:
                        ePosition = enumPosition.Loader8;
                        break;
                    case enumRbtAddress.ALN1: ePosition = enumPosition.AlignerA; break;
                    case enumRbtAddress.ALN2: ePosition = enumPosition.AlignerB; break;
                    case enumRbtAddress.BUF1: ePosition = enumPosition.BufferA; break;
                    case enumRbtAddress.BUF2: ePosition = enumPosition.BufferB; break;
                    case enumRbtAddress.EQM1: ePosition = enumPosition.EQM1; break;
                    case enumRbtAddress.EQM2: ePosition = enumPosition.EQM2; break;
                    case enumRbtAddress.EQM3: ePosition = enumPosition.EQM3; break;
                    case enumRbtAddress.EQM4: ePosition = enumPosition.EQM4; break;
                }
                break;//找到了
            }
            //  防呆
            if (ePosition == enumPosition.UnKnow)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            switch (ePosition)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    {
                        I_Loadport stg = ListSTG[ePosition - enumPosition.Loader1];
                        if (stg.Disable)
                        {
                            new frmMessageBox("Loadport is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (stg.StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Please confirm the loadport dock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && stg.IsDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the loadport do not open door!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, stg.UseAdapter) + (int)stg.eFoupType;
                        targetWafer = stg.Waferlist[cbxRobotSlot.SelectedIndex];
                        armWafer.ToLoadport = SWafer.enumFromLoader.LoadportA + stg.BodyNo - 1;
                        armWafer.ToSlot = cbxRobotSlot.SelectedIndex + 1;
                    }
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    {
                        I_Aligner aln = ListALN[ePosition - enumPosition.AlignerA];
                        if (aln == null || aln.Disable)
                        {
                            new frmMessageBox("Aligner is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = aln.Wafer;
                    }
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    {
                        I_Buffer buf = ListBUF[ePosition - enumPosition.BufferA];
                        if (buf.Disable)
                        {
                            new frmMessageBox("Buffer is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = buf.GetWafer(cbxRobotSlot.SelectedIndex);
                    }
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    {
                        SSEquipment eqm = ListEQM[ePosition - enumPosition.EQM1];
                        if (eqm == null || eqm.Disable)
                        {
                            new frmMessageBox("Equipment is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && eqm.IsShutterDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the Shutter door is not open or not!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = eqm.Wafer;
                    }
                    break;
                default:
                    break;
            }
            if (nStgeIndx == -1)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (targetWafer != null)//Processing 已經被傳出去
            {
                if (armWafer == targetWafer)
                {

                }
                else
                {
                    new frmMessageBox("The target has wafer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
            }
            #endregion

            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                robotManual.ResetProcessCompleted();
                robotManual.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                robotManual.WaitProcessCompleted(robotManual.GetAckTimeout);
                if (robotManual.ExtXaxisDisable == false)
                {
                    robotManual.TBL_560.ResetProcessCompleted();
                    robotManual.TBL_560.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                    robotManual.TBL_560.WaitProcessCompleted(robotManual.GetAckTimeout);
                }

                robotManual.ResetInPos();
                robotManual.MoveToStandbyPosW_Ext_Xaxis(robotManual.GetAckTimeout, true, ePosition, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);
                if (new frmMessageBox("Please check if the robot arm is extended?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, (enumRobotArms)cbxRobotArm.SelectedIndex, ePosition, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                    switch (ePosition)
                    {
                        case enumPosition.Loader1:
                        case enumPosition.Loader2:
                        case enumPosition.Loader3:
                        case enumPosition.Loader4:
                        case enumPosition.Loader5:
                        case enumPosition.Loader6:
                        case enumPosition.Loader7:
                        case enumPosition.Loader8:
                            armWafer.ProcessStatus = SWafer.enumProcessStatus.Sleep;
                            break;
                    }
                }
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }
        private void btnRbClmp_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;
                robotManual.ResetInPos();
                switch ((enumRobotArms)cbxRobotArm.SelectedIndex)
                {
                    case enumRobotArms.UpperArm:
                        robotManual.UpperArm.ClampW(robotManual.GetAckTimeout);
                        break;
                    case enumRobotArms.LowerArm:
                        robotManual.LowerArm.ClampW(robotManual.GetAckTimeout);
                        break;
                }
                robotManual.WaitInPos(5000);
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }
        private void btnRbUclm_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;
                robotManual.ResetInPos();
                switch ((enumRobotArms)cbxRobotArm.SelectedIndex)
                {
                    case enumRobotArms.UpperArm:
                        robotManual.UpperArm.UnClampW(robotManual.GetAckTimeout);
                        break;
                    case enumRobotArms.LowerArm:
                        robotManual.LowerArm.UnClampW(robotManual.GetAckTimeout);
                        break;
                }
                robotManual.WaitInPos(5000);
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }
        private void btnRbALLD_Click(object sender, EventArgs e)
        {
            #region Interlock       
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            int nStgeIndx = -1;
            SWafer targetWafer = null;
            //搜尋選擇的位置
            enumPosition ePosition = enumPosition.UnKnow;
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo).ToArray())
            {
                if (item.strDisplayName != cbxRobotStage.SelectedItem.ToString()) continue;//注意是顯示名稱相同
                switch (item.strDefineName)
                {
                    case enumRbtAddress.STG1_08:
                    case enumRbtAddress.STG1_12:
                        ePosition = enumPosition.Loader1;
                        break;
                    case enumRbtAddress.STG2_08:
                    case enumRbtAddress.STG2_12:
                        ePosition = enumPosition.Loader2;
                        break;
                    case enumRbtAddress.STG3_08:
                    case enumRbtAddress.STG3_12:
                        ePosition = enumPosition.Loader3;
                        break;
                    case enumRbtAddress.STG4_08:
                    case enumRbtAddress.STG4_12:
                        ePosition = enumPosition.Loader4;
                        break;
                    case enumRbtAddress.STG5_08:
                    case enumRbtAddress.STG5_12:
                        ePosition = enumPosition.Loader5;
                        break;
                    case enumRbtAddress.STG6_08:
                    case enumRbtAddress.STG6_12:
                        ePosition = enumPosition.Loader6;
                        break;
                    case enumRbtAddress.STG7_08:
                    case enumRbtAddress.STG7_12:
                        ePosition = enumPosition.Loader7;
                        break;
                    case enumRbtAddress.STG8_08:
                    case enumRbtAddress.STG8_12:
                        ePosition = enumPosition.Loader8;
                        break;
                    case enumRbtAddress.ALN1: ePosition = enumPosition.AlignerA; break;
                    case enumRbtAddress.ALN2: ePosition = enumPosition.AlignerB; break;
                    case enumRbtAddress.BUF1: ePosition = enumPosition.BufferA; break;
                    case enumRbtAddress.BUF2: ePosition = enumPosition.BufferB; break;
                    case enumRbtAddress.EQM1: ePosition = enumPosition.EQM1; break;
                    case enumRbtAddress.EQM2: ePosition = enumPosition.EQM2; break;
                    case enumRbtAddress.EQM3: ePosition = enumPosition.EQM3; break;
                    case enumRbtAddress.EQM4: ePosition = enumPosition.EQM4; break;
                }
                break;//找到了
            }
            //  防呆
            if (ePosition == enumPosition.UnKnow)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            switch (ePosition)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    {
                        I_Loadport stg = ListSTG[ePosition - enumPosition.Loader1];
                        if (stg.Disable)
                        {
                            new frmMessageBox("Loadport is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (stg.StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Please confirm the loadport dock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && stg.IsDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the loadport do not open door!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, stg.UseAdapter) + (int)stg.eFoupType;
                        targetWafer = stg.Waferlist[cbxRobotSlot.SelectedIndex];
                    }
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    {
                        I_Aligner aln = ListALN[ePosition - enumPosition.AlignerA];
                        if (aln == null || aln.Disable)
                        {
                            new frmMessageBox("Aligner is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = aln.Wafer;
                    }
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    {
                        I_Buffer buf = ListBUF[ePosition - enumPosition.BufferA];
                        if (buf.Disable)
                        {
                            new frmMessageBox("Buffer is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = buf.GetWafer(cbxRobotSlot.SelectedIndex);
                    }
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    {
                        SSEquipment eqm = ListEQM[ePosition - enumPosition.EQM1];
                        if (eqm == null || eqm.Disable)
                        {
                            new frmMessageBox("EQ is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && eqm.IsShutterDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the Shutter door is not open or not!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = eqm.Wafer;
                    }
                    break;
                default:
                    break;
            }
            //  防呆
            if (nStgeIndx == -1)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (targetWafer == null)
            {
                new frmMessageBox("The target has no wafer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                return;
            }
            //  load 手必須空，沒有WAFER
            enumRobotArms eRobotArms = (enumRobotArms)cbxRobotArm.SelectedIndex;
            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    if (m_SelectTRB.UpperArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    m_SelectTRB.PrepareUpperWafer = targetWafer;
                    break;
                case enumRobotArms.LowerArm:
                    if (m_SelectTRB.LowerArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    m_SelectTRB.PrepareLowerWafer = targetWafer;
                    break;
                case enumRobotArms.BothArms:
                    if (m_SelectTRB.UpperArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    if (m_SelectTRB.LowerArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    break;
                default:
                    break;
            }
            #endregion
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            bool bSelectUpArm = cbxRobotArm.SelectedIndex == 0;
            int nAlexAddress = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, enumRbtAddress.ALEX);
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                robotManual.ResetProcessCompleted();
                robotManual.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                robotManual.WaitProcessCompleted(robotManual.GetAckTimeout);

                robotManual.ResetInPos();
                robotManual.MoveToStandbyPosW_Ext_Xaxis(robotManual.GetAckTimeout, false, ePosition, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                if (new frmMessageBox("Please check if the robot arm is extended?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    robotManual.TakeWaferAlignmentByInterLockW_ExtXaxis(robotManual.GetAckTimeout, (enumRobotArms)cbxRobotArm.SelectedIndex, ePosition, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                }
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }
        private void btnRbALLD_ALEX_Click(object sender, EventArgs e)
        {
            #region Interlock       
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            int nStgeIndx = -1;
            SWafer targetWafer = null;
            //搜尋選擇的位置
            enumPosition ePosition = enumPosition.UnKnow;
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo).ToArray())
            {
                if (item.strDisplayName != cbxRobotStage.SelectedItem.ToString()) continue;//注意是顯示名稱相同
                switch (item.strDefineName)
                {
                    case enumRbtAddress.STG1_08:
                    case enumRbtAddress.STG1_12:
                        ePosition = enumPosition.Loader1;
                        break;
                    case enumRbtAddress.STG2_08:
                    case enumRbtAddress.STG2_12:
                        ePosition = enumPosition.Loader2;
                        break;
                    case enumRbtAddress.STG3_08:
                    case enumRbtAddress.STG3_12:
                        ePosition = enumPosition.Loader3;
                        break;
                    case enumRbtAddress.STG4_08:
                    case enumRbtAddress.STG4_12:
                        ePosition = enumPosition.Loader4;
                        break;
                    case enumRbtAddress.STG5_08:
                    case enumRbtAddress.STG5_12:
                        ePosition = enumPosition.Loader5;
                        break;
                    case enumRbtAddress.STG6_08:
                    case enumRbtAddress.STG6_12:
                        ePosition = enumPosition.Loader6;
                        break;
                    case enumRbtAddress.STG7_08:
                    case enumRbtAddress.STG7_12:
                        ePosition = enumPosition.Loader7;
                        break;
                    case enumRbtAddress.STG8_08:
                    case enumRbtAddress.STG8_12:
                        ePosition = enumPosition.Loader8;
                        break;
                    case enumRbtAddress.ALN1: ePosition = enumPosition.AlignerA; break;
                    case enumRbtAddress.ALN2: ePosition = enumPosition.AlignerB; break;
                    case enumRbtAddress.BUF1: ePosition = enumPosition.BufferA; break;
                    case enumRbtAddress.BUF2: ePosition = enumPosition.BufferB; break;
                    case enumRbtAddress.EQM1: ePosition = enumPosition.EQM1; break;
                    case enumRbtAddress.EQM2: ePosition = enumPosition.EQM2; break;
                    case enumRbtAddress.EQM3: ePosition = enumPosition.EQM3; break;
                    case enumRbtAddress.EQM4: ePosition = enumPosition.EQM4; break;
                    case enumRbtAddress.AOI: ePosition = enumPosition.AOI; break;
                }
                break;//找到了
            }
            //  防呆
            if (ePosition == enumPosition.UnKnow)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            switch (ePosition)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    {
                        I_Loadport stg = ListSTG[ePosition - enumPosition.Loader1];
                        if (stg.Disable)
                        {
                            new frmMessageBox("Loadport is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (stg.StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Please confirm the loadport dock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && stg.IsDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the loadport do not open door!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, stg.UseAdapter) + (int)stg.eFoupType;
                        targetWafer = stg.Waferlist[cbxRobotSlot.SelectedIndex];
                    }
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    {
                        I_Aligner aln = ListALN[ePosition - enumPosition.AlignerA];
                        if (aln == null || aln.Disable)
                        {
                            new frmMessageBox("Aligner is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = aln.Wafer;
                    }
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    {
                        I_Buffer buf = ListBUF[ePosition - enumPosition.BufferA];
                        if (buf.Disable)
                        {
                            new frmMessageBox("Buffer is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = buf.GetWafer(cbxRobotSlot.SelectedIndex);
                    }
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    {
                        SSEquipment eqm = ListEQM[ePosition - enumPosition.EQM1];
                        if (eqm == null || eqm.Disable)
                        {
                            new frmMessageBox("EQ is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && eqm.IsShutterDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the Shutter door is not open or not!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = eqm.Wafer;
                    }
                    break;
                default:
                    break;
            }
            //  防呆
            if (nStgeIndx == -1)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (targetWafer == null)
            {
                new frmMessageBox("The target has no wafer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                return;
            }
            //  load 手必須空，沒有WAFER
            enumRobotArms eRobotArms = (enumRobotArms)cbxRobotArm.SelectedIndex;
            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    if (m_SelectTRB.UpperArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    m_SelectTRB.PrepareUpperWafer = targetWafer;
                    break;
                case enumRobotArms.LowerArm:
                    if (m_SelectTRB.LowerArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    m_SelectTRB.PrepareLowerWafer = targetWafer;
                    break;
                case enumRobotArms.BothArms:
                    if (m_SelectTRB.UpperArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    if (m_SelectTRB.LowerArmWafer != null)//手要空
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                        return;
                    }
                    break;
                default:
                    break;
            }
            #endregion
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            bool bSelectUpArm = cbxRobotArm.SelectedIndex == 0;
            int nAlexAddress = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, enumRbtAddress.ALEX);
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                robotManual.ResetProcessCompleted();
                robotManual.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                robotManual.WaitProcessCompleted(robotManual.GetAckTimeout);

                robotManual.ResetInPos();
                robotManual.MoveToStandbyPosW_Ext_Xaxis(robotManual.GetAckTimeout, false, ePosition, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                if (new frmMessageBox("Please check if the robot arm is extended?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    robotManual.TakeWaferAlignmentByInterLockW_ExtXaxis(robotManual.GetAckTimeout, (enumRobotArms)cbxRobotArm.SelectedIndex, ePosition, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);

                    robotManual.ResetInPos();
                    robotManual.AlexW(robotManual.GetAckTimeout, eRobotArms, nAlexAddress, 1, 0, 0);
                    robotManual.WaitInPos(robotManual.GetMotionTimeout);
                }
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }
        private void btnRbALUL_Click(object sender, EventArgs e)
        {
            #region Interlock   
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            int nStgeIndx = -1;
            SWafer targetWafer = null;
            SWafer armWafer = null;
            //  unld 手必須有WAFER
            enumRobotArms eRobotArms = (enumRobotArms)cbxRobotArm.SelectedIndex;
            switch (eRobotArms)
            {
                case enumRobotArms.UpperArm:
                    if (m_SelectTRB.UpperArmWafer == null)
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    armWafer = m_SelectTRB.UpperArmWafer;
                    break;
                case enumRobotArms.LowerArm:
                    if (m_SelectTRB.LowerArmWafer == null)
                    {
                        new frmMessageBox("Please confirm the wafer on the robot", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    armWafer = m_SelectTRB.LowerArmWafer;
                    break;
            }
            //搜尋選擇的位置
            enumPosition ePosition = enumPosition.UnKnow;
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo).ToArray())
            {
                if (item.strDisplayName != cbxRobotStage.SelectedItem.ToString()) continue;//注意是顯示名稱相同
                switch (item.strDefineName)
                {
                    case enumRbtAddress.STG1_08:
                    case enumRbtAddress.STG1_12:
                        ePosition = enumPosition.Loader1;
                        break;
                    case enumRbtAddress.STG2_08:
                    case enumRbtAddress.STG2_12:
                        ePosition = enumPosition.Loader2;
                        break;
                    case enumRbtAddress.STG3_08:
                    case enumRbtAddress.STG3_12:
                        ePosition = enumPosition.Loader3;
                        break;
                    case enumRbtAddress.STG4_08:
                    case enumRbtAddress.STG4_12:
                        ePosition = enumPosition.Loader4;
                        break;
                    case enumRbtAddress.STG5_08:
                    case enumRbtAddress.STG5_12:
                        ePosition = enumPosition.Loader5;
                        break;
                    case enumRbtAddress.STG6_08:
                    case enumRbtAddress.STG6_12:
                        ePosition = enumPosition.Loader6;
                        break;
                    case enumRbtAddress.STG7_08:
                    case enumRbtAddress.STG7_12:
                        ePosition = enumPosition.Loader7;
                        break;
                    case enumRbtAddress.STG8_08:
                    case enumRbtAddress.STG8_12:
                        ePosition = enumPosition.Loader8;
                        break;
                    case enumRbtAddress.ALN1: ePosition = enumPosition.AlignerA; break;
                    case enumRbtAddress.ALN2: ePosition = enumPosition.AlignerB; break;
                    case enumRbtAddress.BUF1: ePosition = enumPosition.BufferA; break;
                    case enumRbtAddress.BUF2: ePosition = enumPosition.BufferB; break;
                    case enumRbtAddress.EQM1: ePosition = enumPosition.EQM1; break;
                    case enumRbtAddress.EQM2: ePosition = enumPosition.EQM2; break;
                    case enumRbtAddress.EQM3: ePosition = enumPosition.EQM3; break;
                    case enumRbtAddress.EQM4: ePosition = enumPosition.EQM4; break;
                }
                break;//找到了
            }
            //  防呆
            if (ePosition == enumPosition.UnKnow)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            switch (ePosition)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    {
                        I_Loadport stg = ListSTG[ePosition - enumPosition.Loader1];
                        if (stg.Disable)
                        {
                            new frmMessageBox("Loadport is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (stg.StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Please confirm the loadport dock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && stg.IsDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the loadport do not open door!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, stg.UseAdapter) + (int)stg.eFoupType;
                        targetWafer = stg.Waferlist[cbxRobotSlot.SelectedIndex];
                        armWafer.ToLoadport = SWafer.enumFromLoader.LoadportA + stg.BodyNo - 1;
                        armWafer.ToSlot = cbxRobotSlot.SelectedIndex + 1;
                    }
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    {
                        I_Aligner aln = ListALN[ePosition - enumPosition.AlignerA];
                        if (aln == null || aln.Disable)
                        {
                            new frmMessageBox("Aligner is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = aln.Wafer;
                    }
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    {
                        I_Buffer buf = ListBUF[ePosition - enumPosition.BufferA];
                        if (buf.Disable)
                        {
                            new frmMessageBox("Buffer is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = buf.GetWafer(cbxRobotSlot.SelectedIndex);
                    }
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    {
                        SSEquipment eqm = ListEQM[ePosition - enumPosition.EQM1];
                        if (eqm == null || eqm.Disable)
                        {
                            new frmMessageBox("Equipment is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && eqm.IsShutterDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the Shutter door is not open or not!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = eqm.Wafer;
                    }
                    break;
                default:
                    break;
            }
            if (nStgeIndx == -1)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (targetWafer != null)//Processing 已經被傳出去
            {
                if (armWafer == targetWafer)
                {

                }
                else
                {
                    new frmMessageBox("The target has wafer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
            }
            #endregion

            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            bool bSelectUpArm = cbxRobotArm.SelectedIndex == 0;
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                robotManual.ResetProcessCompleted();
                robotManual.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                robotManual.WaitProcessCompleted(robotManual.GetAckTimeout);

                robotManual.ResetInPos();
                robotManual.MoveToStandbyPosW_Ext_Xaxis(robotManual.GetAckTimeout, true, ePosition, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);
                if (new frmMessageBox("Please check if the robot arm is extended?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    robotManual.PutWaferAlignmentByInterLockW_ExtXaxis(robotManual.GetAckTimeout, (enumRobotArms)cbxRobotArm.SelectedIndex, ePosition, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                    switch (ePosition)
                    {
                        case enumPosition.Loader1:
                        case enumPosition.Loader2:
                        case enumPosition.Loader3:
                        case enumPosition.Loader4:
                        case enumPosition.Loader5:
                        case enumPosition.Loader6:
                        case enumPosition.Loader7:
                        case enumPosition.Loader8:
                            armWafer.ProcessStatus = SWafer.enumProcessStatus.Sleep;
                            break;
                    }
                }
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }

        private void btnRbFlipToFront_Click(object sender, EventArgs e)
        {
            #region Interlock   
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            int nStgeIndx = -1;
            SWafer targetWafer = null;
            SWafer armWafer = null;
            int nSide = 1; //翻成正面
            enumRobotArms eRobotArms = (enumRobotArms)cbxRobotArm.SelectedIndex;

            //搜尋選擇的位置
            enumPosition ePosition = enumPosition.UnKnow;
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo).ToArray())
            {
                if (item.strDisplayName != cbxRobotStage.SelectedItem.ToString()) continue;//注意是顯示名稱相同
                switch (item.strDefineName)
                {
                    case enumRbtAddress.STG1_08:
                    case enumRbtAddress.STG1_12:
                        ePosition = enumPosition.Loader1;
                        break;
                    case enumRbtAddress.STG2_08:
                    case enumRbtAddress.STG2_12:
                        ePosition = enumPosition.Loader2;
                        break;
                    case enumRbtAddress.STG3_08:
                    case enumRbtAddress.STG3_12:
                        ePosition = enumPosition.Loader3;
                        break;
                    case enumRbtAddress.STG4_08:
                    case enumRbtAddress.STG4_12:
                        ePosition = enumPosition.Loader4;
                        break;
                    case enumRbtAddress.STG5_08:
                    case enumRbtAddress.STG5_12:
                        ePosition = enumPosition.Loader5;
                        break;
                    case enumRbtAddress.STG6_08:
                    case enumRbtAddress.STG6_12:
                        ePosition = enumPosition.Loader6;
                        break;
                    case enumRbtAddress.STG7_08:
                    case enumRbtAddress.STG7_12:
                        ePosition = enumPosition.Loader7;
                        break;
                    case enumRbtAddress.STG8_08:
                    case enumRbtAddress.STG8_12:
                        ePosition = enumPosition.Loader8;
                        break;
                    case enumRbtAddress.ALN1: ePosition = enumPosition.AlignerA; break;
                    case enumRbtAddress.ALN2: ePosition = enumPosition.AlignerB; break;
                    case enumRbtAddress.BUF1: ePosition = enumPosition.BufferA; break;
                    case enumRbtAddress.BUF2: ePosition = enumPosition.BufferB; break;
                    case enumRbtAddress.EQM1: ePosition = enumPosition.EQM1; break;
                    case enumRbtAddress.EQM2: ePosition = enumPosition.EQM2; break;
                    case enumRbtAddress.EQM3: ePosition = enumPosition.EQM3; break;
                    case enumRbtAddress.EQM4: ePosition = enumPosition.EQM4; break;
                }
                break;//找到了
            }
            //  防呆
            if (ePosition == enumPosition.UnKnow)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            switch (ePosition)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    {
                        I_Loadport stg = ListSTG[ePosition - enumPosition.Loader1];
                        if (stg.Disable)
                        {
                            new frmMessageBox("Loadport is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (stg.StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Please confirm the loadport dock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && stg.IsDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the loadport do not open door!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, stg.UseAdapter) + (int)stg.eFoupType;
                        targetWafer = stg.Waferlist[cbxRobotSlot.SelectedIndex];
                        armWafer.ToLoadport = SWafer.enumFromLoader.LoadportA + stg.BodyNo - 1;
                        armWafer.ToSlot = cbxRobotSlot.SelectedIndex + 1;
                    }
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    {
                        I_Aligner aln = ListALN[ePosition - enumPosition.AlignerA];
                        if (aln == null || aln.Disable)
                        {
                            new frmMessageBox("Aligner is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = aln.Wafer;
                    }
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    {
                        I_Buffer buf = ListBUF[ePosition - enumPosition.BufferA];
                        if (buf.Disable)
                        {
                            new frmMessageBox("Buffer is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = buf.GetWafer(cbxRobotSlot.SelectedIndex);
                    }
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    {
                        SSEquipment eqm = ListEQM[ePosition - enumPosition.EQM1];
                        if (eqm == null || eqm.Disable)
                        {
                            new frmMessageBox("Equipment is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = eqm.Wafer;
                    }
                    break;
                default:
                    break;
            }
            if (nStgeIndx == -1)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            #endregion

            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            bool bSelectUpArm = cbxRobotArm.SelectedIndex == 0;
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                robotManual.ResetProcessCompleted();
                robotManual.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                robotManual.WaitProcessCompleted(robotManual.GetAckTimeout);

                robotManual.ResetInPos();
                robotManual.MoveToStandbyPosW_Ext_Xaxis(robotManual.GetAckTimeout, true, ePosition, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);
                if (new frmMessageBox("Please check if the robot arm is extended?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    robotManual.FlipByInterLockW(robotManual.GetAckTimeout, nSide, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                    robotManual.WaitInPos(5000);
                }
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }


        private void btnRBFlipToBack_Click(object sender, EventArgs e)
        {
            #region Interlock   
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectTRB == null)
            {
                new frmMessageBox("Please select Robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            int nStgeIndx = -1;
            SWafer targetWafer = null;
            SWafer armWafer = null;
            int nSide = 2; //翻成背面
            enumRobotArms eRobotArms = (enumRobotArms)cbxRobotArm.SelectedIndex;

            //搜尋選擇的位置
            enumPosition ePosition = enumPosition.UnKnow;
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo).ToArray())
            {
                if (item.strDisplayName != cbxRobotStage.SelectedItem.ToString()) continue;//注意是顯示名稱相同
                switch (item.strDefineName)
                {
                    case enumRbtAddress.STG1_08:
                    case enumRbtAddress.STG1_12:
                        ePosition = enumPosition.Loader1;
                        break;
                    case enumRbtAddress.STG2_08:
                    case enumRbtAddress.STG2_12:
                        ePosition = enumPosition.Loader2;
                        break;
                    case enumRbtAddress.STG3_08:
                    case enumRbtAddress.STG3_12:
                        ePosition = enumPosition.Loader3;
                        break;
                    case enumRbtAddress.STG4_08:
                    case enumRbtAddress.STG4_12:
                        ePosition = enumPosition.Loader4;
                        break;
                    case enumRbtAddress.STG5_08:
                    case enumRbtAddress.STG5_12:
                        ePosition = enumPosition.Loader5;
                        break;
                    case enumRbtAddress.STG6_08:
                    case enumRbtAddress.STG6_12:
                        ePosition = enumPosition.Loader6;
                        break;
                    case enumRbtAddress.STG7_08:
                    case enumRbtAddress.STG7_12:
                        ePosition = enumPosition.Loader7;
                        break;
                    case enumRbtAddress.STG8_08:
                    case enumRbtAddress.STG8_12:
                        ePosition = enumPosition.Loader8;
                        break;
                    case enumRbtAddress.ALN1: ePosition = enumPosition.AlignerA; break;
                    case enumRbtAddress.ALN2: ePosition = enumPosition.AlignerB; break;
                    case enumRbtAddress.BUF1: ePosition = enumPosition.BufferA; break;
                    case enumRbtAddress.BUF2: ePosition = enumPosition.BufferB; break;
                    case enumRbtAddress.EQM1: ePosition = enumPosition.EQM1; break;
                    case enumRbtAddress.EQM2: ePosition = enumPosition.EQM2; break;
                    case enumRbtAddress.EQM3: ePosition = enumPosition.EQM3; break;
                    case enumRbtAddress.EQM4: ePosition = enumPosition.EQM4; break;
                }
                break;//找到了
            }
            //  防呆
            if (ePosition == enumPosition.UnKnow)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            switch (ePosition)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    {
                        I_Loadport stg = ListSTG[ePosition - enumPosition.Loader1];
                        if (stg.Disable)
                        {
                            new frmMessageBox("Loadport is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (stg.StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Please confirm the loadport dock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        if (!GParam.theInst.IsSimulate && stg.IsDoorOpen == false)
                        {
                            new frmMessageBox("Please confirm the loadport do not open door!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, stg.UseAdapter) + (int)stg.eFoupType;
                        targetWafer = stg.Waferlist[cbxRobotSlot.SelectedIndex];
                        armWafer.ToLoadport = SWafer.enumFromLoader.LoadportA + stg.BodyNo - 1;
                        armWafer.ToSlot = cbxRobotSlot.SelectedIndex + 1;
                    }
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    {
                        I_Aligner aln = ListALN[ePosition - enumPosition.AlignerA];
                        if (aln == null || aln.Disable)
                        {
                            new frmMessageBox("Aligner is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = aln.Wafer;
                    }
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    {
                        I_Buffer buf = ListBUF[ePosition - enumPosition.BufferA];
                        if (buf.Disable)
                        {
                            new frmMessageBox("Buffer is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = buf.GetWafer(cbxRobotSlot.SelectedIndex);
                    }
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    {
                        SSEquipment eqm = ListEQM[ePosition - enumPosition.EQM1];
                        if (eqm == null || eqm.Disable)
                        {
                            new frmMessageBox("Equipment is disable!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                        nStgeIndx = GParam.theInst.GetDicPosRobot(m_SelectTRB.BodyNo, ePosition, false);
                        targetWafer = eqm.Wafer;
                    }
                    break;
                default:
                    break;
            }
            if (nStgeIndx == -1)
            {
                new frmMessageBox("Robot stage search fail!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            #endregion

            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "TRB" + m_SelectTRB.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            bool bSelectUpArm = cbxRobotArm.SelectedIndex == 0;
            m_SelectTRB.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                robotManual.ResetProcessCompleted();
                robotManual.SspdW(robotManual.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(robotManual.BodyNo - 1));
                robotManual.WaitProcessCompleted(robotManual.GetAckTimeout);

                robotManual.ResetInPos();
                robotManual.MoveToStandbyPosW_Ext_Xaxis(robotManual.GetAckTimeout, true, ePosition, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);
                if (new frmMessageBox("Please check if the robot arm is extended?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    robotManual.FlipByInterLockW(robotManual.GetAckTimeout, nSide, (enumRobotArms)cbxRobotArm.SelectedIndex, nStgeIndx, cbxRobotSlot.SelectedIndex + 1);
                }
            };
            m_SelectTRB.OnManualCompleted -= _robot_OnManualCompleted;
            m_SelectTRB.OnManualCompleted += _robot_OnManualCompleted;
            m_SelectTRB.StartManualFunction();
        }

        private void _robot_OnManualCompleted(object sender, bool bSuc)
        {
            I_Robot robotManual = sender as I_Robot;
            robotManual.OnManualCompleted -= _robot_OnManualCompleted;
            EnableControlButton(true);
        }
        private void cbxRobotStage_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbxRobotSlot.Items.Clear();
            if (m_SelectTRB == null) { return; }
            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo))
            {
                if (item.strDisplayName != cbxRobotStage.SelectedItem.ToString())
                    continue;
                //  找到位置
                switch (item.strDefineName)
                {
                    case enumRbtAddress.STG1_08:
                    case enumRbtAddress.STG1_12:
                        for (int i = 0; i < ListSTG[0].WaferTotal; i++)
                            cbxRobotSlot.Items.Add((i + 1).ToString("00"));

                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && ListSTG[0].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && ListSTG[0].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && ListSTG[0].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && ListSTG[0].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.STG2_08:
                    case enumRbtAddress.STG2_12:
                        for (int i = 0; i < ListSTG[1].WaferTotal; i++)
                            cbxRobotSlot.Items.Add((i + 1).ToString("00"));

                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && ListSTG[1].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && ListSTG[1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && ListSTG[1].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && ListSTG[1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.STG3_08:
                    case enumRbtAddress.STG3_12:
                        for (int i = 0; i < ListSTG[2].WaferTotal; i++)
                            cbxRobotSlot.Items.Add((i + 1).ToString("00"));

                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && ListSTG[2].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && ListSTG[2].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && ListSTG[2].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && ListSTG[2].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.STG4_08:
                    case enumRbtAddress.STG4_12:
                        for (int i = 0; i < ListSTG[3].WaferTotal; i++)
                            cbxRobotSlot.Items.Add((i + 1).ToString("00"));

                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && ListSTG[3].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && ListSTG[3].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && ListSTG[3].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && ListSTG[3].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.STG5_08:
                    case enumRbtAddress.STG5_12:
                        for (int i = 0; i < ListSTG[4].WaferTotal; i++)
                            cbxRobotSlot.Items.Add((i + 1).ToString("00"));

                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && ListSTG[4].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && ListSTG[4].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && ListSTG[4].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && ListSTG[4].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.STG6_08:
                    case enumRbtAddress.STG6_12:
                        for (int i = 0; i < ListSTG[5].WaferTotal; i++)
                            cbxRobotSlot.Items.Add((i + 1).ToString("00"));

                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && ListSTG[5].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && ListSTG[5].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && ListSTG[5].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && ListSTG[5].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.STG7_08:
                    case enumRbtAddress.STG7_12:
                        for (int i = 0; i < ListSTG[6].WaferTotal; i++)
                            cbxRobotSlot.Items.Add((i + 1).ToString("00"));

                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && ListSTG[6].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && ListSTG[6].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && ListSTG[6].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && ListSTG[6].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.STG8_08:
                    case enumRbtAddress.STG8_12:
                        for (int i = 0; i < ListSTG[7].WaferTotal; i++)
                            cbxRobotSlot.Items.Add((i + 1).ToString("00"));

                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && ListSTG[7].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && ListSTG[7].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && ListSTG[7].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && ListSTG[7].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)) //若arm type是跟loadport不同type
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.ALN1:
                        cbxRobotSlot.Items.Add("01");
                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(0) != enumAlignerType.TurnTable)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(0) == enumAlignerType.TurnTable)) //若arm type是跟aligner type不同
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(0) != enumAlignerType.TurnTable)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(0) == enumAlignerType.TurnTable)) //若arm type是跟aligner type不同
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.ALN2:
                        cbxRobotSlot.Items.Add("01");
                        if ((enumRobotArms)cbxRobotArm.SelectedIndex == enumRobotArms.UpperArm) //若arm選擇上arm
                        {
                            if ((m_SelectTRB.UpperArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(1) != enumAlignerType.TurnTable)
                                || (m_SelectTRB.UpperArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(1) == enumAlignerType.TurnTable)) //若arm type是跟aligner type不同
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }
                        else
                        {
                            if ((m_SelectTRB.LowerArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(1) != enumAlignerType.TurnTable)
                                || (m_SelectTRB.LowerArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(1) == enumAlignerType.TurnTable)) //若arm type是跟aligner type不同
                            {
                                btnRbLoad.Enabled = false;
                                btnRbUnld.Enabled = false;
                            }
                            else
                            {
                                btnRbLoad.Enabled = true;
                                btnRbUnld.Enabled = true;
                            }
                        }

                        break;
                    case enumRbtAddress.BarCode:
                    case enumRbtAddress.EQM1:
                    case enumRbtAddress.EQM2:
                    case enumRbtAddress.EQM3:
                    case enumRbtAddress.EQM4:
                        cbxRobotSlot.Items.Add("01");
                        btnRbLoad.Enabled = true;
                        btnRbUnld.Enabled = true;
                        break;
                    case enumRbtAddress.BUF1:
                        if (ListBUF[0].HardwareSlot > 0)
                        {
                            for (int i = 0; i < ListBUF[0].HardwareSlot; i++)
                            {
                                if (ListBUF[0].IsSlotDisable(i) == false)
                                    cbxRobotSlot.Items.Add((i + 1).ToString("00"));
                            }
                        }
                        break;
                    case enumRbtAddress.BUF2:
                        if (ListBUF[1].HardwareSlot > 0)
                        {
                            for (int i = 0; i < ListBUF[1].HardwareSlot; i++)
                            {
                                if (ListBUF[1].IsSlotDisable(i) == false)
                                    cbxRobotSlot.Items.Add((i + 1).ToString("00"));
                            }
                        }
                        break;
                }
            }
            if (cbxRobotSlot.Items.Count > 0)
                cbxRobotSlot.SelectedIndex = 0;
        }
        #endregion
        #region ========== Aligner Button ==========
        private void btnAlignerOrgn_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectALN == null)
            {
                new frmMessageBox("Please select Aligner.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "ALN" + m_SelectALN.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectALN.DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;

                alignerManual.ResetInPos();
                //alignerManual.OrgnW(3000);
                alignerManual.ORGN();
                alignerManual.WaitInPos(30000);
            };
            m_SelectALN.OnManualCompleted += _aligner_OnManualCompleted;
            m_SelectALN.StartManualFunction();
        }
        private void btnAlignerAlgn_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectALN == null)
            {
                new frmMessageBox("Please select Aligner.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "ALN" + m_SelectALN.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectALN.DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;

                if (alignerManual is SSAlignerPanelXYR)
                {
                    alignerManual.ResetInPos();
                    alignerManual.Algn1W(3000);
                    alignerManual.WaitInPos(30000);
                }
                else
                {
                    int nAngle = 0;

                    alignerManual.ResetInPos();
                    alignerManual.AlgnDW(3000, nAngle.ToString());
                    alignerManual.WaitInPos(30000);

                    alignerManual.ResetInPos();
                    alignerManual.UclmW(3000);
                    alignerManual.WaitInPos(30000);
                }
            };
            m_SelectALN.OnManualCompleted += _aligner_OnManualCompleted;
            m_SelectALN.StartManualFunction();
        }
        private void btnAlignerClmp_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectALN == null)
            {
                new frmMessageBox("Please select Aligner.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "ALN" + m_SelectALN.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectALN.DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;

                alignerManual.ResetInPos();
                alignerManual.ClmpW(3000);
                alignerManual.WaitInPos(30000);
            };
            m_SelectALN.OnManualCompleted += _aligner_OnManualCompleted;
            m_SelectALN.StartManualFunction();
        }
        private void btnAlignerUclm_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectALN == null)
            {
                new frmMessageBox("Please select Aligner.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Name, m_userManager.UserID, "ALN" + m_SelectALN.BodyNo, btn.Name + " Click");
            EnableControlButton(false);
            m_SelectALN.DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;

                alignerManual.ResetInPos();
                alignerManual.UclmW(3000);
                alignerManual.WaitInPos(30000);
            };
            m_SelectALN.OnManualCompleted += _aligner_OnManualCompleted;
            m_SelectALN.StartManualFunction();
        }
        private void _aligner_OnManualCompleted(object sender, bool bSuc)
        {
            I_Aligner alignerManual = sender as I_Aligner;
            alignerManual.OnManualCompleted -= _aligner_OnManualCompleted;
            EnableControlButton(true);
        }


        private void rbStep_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton btn = (RadioButton)sender;

            if (false == btn.Checked)
                return;

            try
            {
                int nTag = int.Parse(btn.Text.ToString());
                switch (nTag)
                {
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
            catch
            {

            }
        }

        private void btnCW_Click(object sender, EventArgs e)
        {
            try
            {
                EnableControlButton(false);

                m_SelectALN.ResetInPos();
                //m_SelectALN.RotStepW(3000, m_nStep);
                m_SelectALN.Rot1STEP(m_nStep);
                m_SelectALN.WaitInPos(100000);

                m_SelectALN.GposRW(3000); //  問位置
                txtTunTablePas.Text = m_SelectALN.Raxispos.ToString();

                EnableControlButton(true);
            }
            catch
            {
                EnableControlButton(true);
            }
        }

        private void btnCCW_Click(object sender, EventArgs e)
        {
            try
            {
                EnableControlButton(false);

                m_SelectALN.ResetInPos();
                //m_SelectALN.RotStepW(3000, -1 * m_nStep);
                m_SelectALN.Rot1STEP(-1 * m_nStep);
                m_SelectALN.WaitInPos(100000);

                m_SelectALN.GposRW(3000); //  問位置
                txtTunTablePas.Text = m_SelectALN.Raxispos.ToString();

                EnableControlButton(true);
            }
            catch
            {
                EnableControlButton(true);
            }
        }
        private void btn0Pos_Click(object sender, EventArgs e)
        {
            try
            {
                EnableControlButton(false);

                m_SelectALN.ResetInPos();
                m_SelectALN.ClmpW(3000);
                m_SelectALN.WaitInPos(30000);

                m_SelectALN.ResetInPos();
                //m_SelectALN.AlgnDW(3000, GParam.theInst.GetTunTable_angle_0(1).ToString());
                m_SelectALN.Rot1EXTD(GParam.theInst.GetTurnTable_angle_0(1));
                m_SelectALN.WaitInPos(100000);

                m_SelectALN.ResetInPos();
                m_SelectALN.UclmW(3000);
                m_SelectALN.WaitInPos(30000);

                m_SelectALN.GposRW(3000); //  問位置
                txtTunTablePas.Text = m_SelectALN.Raxispos.ToString();

                EnableControlButton(true);
            }
            catch
            {
                EnableControlButton(true);

            }
        }
        private void btn180Pos_Click(object sender, EventArgs e)
        {
            try
            {
                EnableControlButton(false);

                m_SelectALN.ResetInPos();
                m_SelectALN.ClmpW(3000);
                m_SelectALN.WaitInPos(30000);

                m_SelectALN.ResetInPos();
                //m_SelectALN.AlgnDW(3000, GParam.theInst.GetTunTable_angle_180(1).ToString());
                m_SelectALN.Rot1EXTD(GParam.theInst.GetTurnTable_angle_180(1));
                m_SelectALN.WaitInPos(100000);

                m_SelectALN.ResetInPos();
                m_SelectALN.UclmW(3000);
                m_SelectALN.WaitInPos(30000);

                m_SelectALN.GposRW(3000); //  問位置
                txtTunTablePas.Text = m_SelectALN.Raxispos.ToString();

                EnableControlButton(true);
            }
            catch
            {
                EnableControlButton(true);

            }
        }
        private void btnSet_Click(object sender, EventArgs e)
        {
            try
            {
                EnableControlButton(false);
                if (new frmMessageBox("Are you sure you want to set this position to the 0 degree position of the turntable?", "SURE", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    GParam.theInst.SetTurnTable_angle_0(1, int.Parse(txtTunTablePas.Text));
                    txtTunTablePas_0.Text = GParam.theInst.GetTurnTable_angle_0(1).ToString();
                }
                EnableControlButton(true);
            }
            catch
            {
                EnableControlButton(true);
            }
        }
        private void btn180Set_Click(object sender, EventArgs e)
        {
            try
            {
                EnableControlButton(false);
                if (new frmMessageBox("Are you sure you want to set this position to the 180 degree position of the turntable?", "SURE", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    GParam.theInst.SetTurnTable_angle_180(1, int.Parse(txtTunTablePas.Text));
                    txtTunTablePas_180.Text = GParam.theInst.GetTurnTable_angle_180(1).ToString();
                }
                EnableControlButton(true);
            }
            catch
            {
                EnableControlButton(true);
            }
        }
        private void btnReadBarcode_Click(object sender, EventArgs e)
        {
            txtBarcode.Text = m_SelectALN.BarcodeRead();
        }

        private void UpdataTurnTableStatus()
        {
            pbAir.Image = m_SelectALN.IsAirOK() ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
            pbWafer.Image = m_SelectALN.WaferExists() ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
            pbClamp.Image = m_SelectALN.IsClamp() ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
            pbUnclamp.Image = m_SelectALN.IsUnClamp() ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
            pbFan.Image = m_SelectALN.IsFanOK() ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
        }


        //  Button
        private void btnAlignerRotAbs_Click(object sender, double dAngle)
        {
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_SelectALN == null)
            {
                new frmMessageBox("Please select Aligner.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachOCR", m_userManager.UserID, "ALIGNER", "ABS Angle: " + (dAngle).ToString());

            EnableControlButton(false);
            m_SelectALN.DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;

                double n = dAngle;
                alignerManual.ResetInPos();
                alignerManual.AlgnDW(3000, n.ToString());
                alignerManual.WaitInPos(30000);

                alignerManual.ResetInPos();
                alignerManual.UclmW(3000);
                alignerManual.WaitInPos(30000);
            };
            m_SelectALN.OnManualCompleted += _aligner_OnManualCompleted;
            m_SelectALN.StartManualFunction();
        }

        #endregion
        #region ========== EQ Button ==========
        private void btn_shutterDoorOpen_Click(object sender, EventArgs e)
        {
            #region Interlock       
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            #endregion

            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo))
            {
                if (item.strDisplayName != cbxShutterDoor.SelectedItem?.ToString())
                    continue;

                int index = -1;
                switch (item.strDefineName)
                {
                    case enumRbtAddress.EQM1:
                        index = 0;
                        break;
                    case enumRbtAddress.EQM2:
                        index = 1;
                        break;
                    case enumRbtAddress.EQM3:
                        index = 2;
                        break;
                    case enumRbtAddress.EQM4:
                        index = 3;
                        break;
                }

                if (index >= 0 && index < ListEQM.Count)
                {
                    EnableControlButton(false);
                    var eqm = ListEQM[index];
                    eqm.OnSutterDoorOpenComplete -= _ShutterDoorOpenCompleted;
                    eqm.OnSutterDoorOpenComplete += _ShutterDoorOpenCompleted;
                    eqm.tShutterDoorOpenSetW();
                }
                else
                {

                }
                break; // 找到就可以跳出
            }
        }
        private void btn_shutterDoorClose_Click(object sender, EventArgs e)
        {
            #region Interlock       
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Running in cycles.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            #endregion

            foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(m_SelectTRB.BodyNo))
            {
                if (item.strDisplayName != cbxShutterDoor.SelectedItem?.ToString())
                    continue;

                int index = -1;
                switch (item.strDefineName)
                {
                    case enumRbtAddress.EQM1:
                        index = 0;
                        break;
                    case enumRbtAddress.EQM2:
                        index = 1;
                        break;
                    case enumRbtAddress.EQM3:
                        index = 2;
                        break;
                    case enumRbtAddress.EQM4:
                        index = 3;
                        break;
                }

                if (index >= 0 && index < ListEQM.Count)
                {
                    EnableControlButton(false);
                    var eqm = ListEQM[index];
                    eqm.OnSutterDoorCloseComplete -= _ShutterDoorCloseCompleted;
                    eqm.OnSutterDoorCloseComplete += _ShutterDoorCloseCompleted;
                    eqm.tShutterDoorCloseSetW();
                }
                break; // 找到就可以跳出
            }
        }
        private void _ShutterDoorOpenCompleted(object sender, bool bSuc)
        {
            var eqm = sender as SSEquipment;
            eqm.OnSutterDoorOpenComplete -= _ShutterDoorOpenCompleted;
            EnableControlButton(true);
        }
        private void _ShutterDoorCloseCompleted(object sender, bool bSuc)
        {
            var eqm = sender as SSEquipment;
            eqm.OnSutterDoorCloseComplete -= _ShutterDoorCloseCompleted;
            EnableControlButton(true);
        }
        #endregion
        private void tmrUI_Tick(object sender, EventArgs e)
        {
            tmrUI.Enabled = false;
            #region ========== Loadport ==========
            for (int i = 0; i < ListSTG.Count; i++)
            {
                if (ListSTG[i].Disable) continue;

                m_guiloadportList[i].KeepClamp = ListSTG[i].IsKeepClamp;

                if (ListSTG[i].Waferlist == null) continue;

                for (int nSlot = 1; nSlot <= ListSTG[i].Waferlist.Count; nSlot++)
                {
                    SWafer waferShow = ListSTG[i].Waferlist[nSlot - 1];

                    if (nSlot == 1)
                    {

                    }

                    if (waferShow == null)//empty
                    {
                        m_guiloadportList[i].UpdataWaferStatus(nSlot, "");
                        m_guiloadportList[i].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.None, SystemColors.Control);
                    }
                    else
                    {
                        switch (waferShow.ProcessStatus)
                        {
                            case SWafer.enumProcessStatus.Sleep:
                                m_guiloadportList[i].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Sleep, Color.LimeGreen);
                                break;
                            case SWafer.enumProcessStatus.WaitProcess:
                                m_guiloadportList[i].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.WaitProcess, Color.RoyalBlue);
                                break;
                            case SWafer.enumProcessStatus.Processing:
                                m_guiloadportList[i].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Processing, SystemColors.Control);
                                break;
                            case SWafer.enumProcessStatus.Processed:
                                {
                                    if (waferShow.WaferIDComparison == SWafer.enumWaferIDComparison.IDAbort)
                                        m_guiloadportList[i].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Abort, Color.Red);
                                    else
                                        m_guiloadportList[i].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Processed, Color.HotPink);
                                }
                                break;
                            case SWafer.enumProcessStatus.Error:
                                m_guiloadportList[i].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Error, Color.Red);
                                break;
                        }
                        m_guiloadportList[i].UpdataWaferStatus(nSlot, waferShow.WaferID_F, waferShow.WaferID_B, waferShow.WaferInforID_F, waferShow.WaferInforID_B, waferShow.Position);
                    }

                }
            }
            #endregion


            if (m_SelectALN != null && false == m_SelectALN.Disable && GParam.theInst.GetAlignerMode(m_SelectALN.BodyNo - 1) == enumAlignerType.TurnTable)
            {
                UpdataTurnTableStatus();
            }

            tmrUI.Enabled = true;
        }
        private void frmManual_Load(object sender, EventArgs e)
        {

            foreach (I_Aligner item in ListALN)
                if (item != null && item.Disable == false) gpbALN.Visible = true;

        }
        private void frmManual_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                tmrUI.Enabled = this.Visible;

                if (this.Visible)
                {
                    for (int i = 0; i < ListSTG.Count; i++)
                    {
                        if (ListSTG[i].Disable) continue;//220218 v1.003

                        ListSTG[i].OnUclmComplete -= OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        ListSTG[i].OnUclmComplete += OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        ListSTG[i].OnUclm1Complete -= OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖，勾住Foup
                        ListSTG[i].OnUclm1Complete += OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖，勾住Foup

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

                        ListE84[i].OnAceessModeChange -= OnLoadport_E84ModeChange;                //  更新UI
                        ListE84[i].OnAceessModeChange += OnLoadport_E84ModeChange;                //  更新UI
                    }

                    foreach (GUILoadport item in m_guiloadportList)//更新grouprecipe list
                    {
                        int nIndex = item.BodyNo - 1;
                        if (ListSTG[nIndex].Disable) continue;
                        //初始值
                        OnLoadport_FoupExistChenge(ListSTG[nIndex], new FoupExisteChangEventArgs(ListSTG[nIndex].FoupExist));
                        OnLoadport_MappingComplete(ListSTG[nIndex], new LoadPortEventArgs(ListSTG[nIndex].MappingData, ListSTG[nIndex].BodyNo, true));
                        OnLoadport_StatusMachineChange(ListSTG[nIndex], new OccurStateMachineChangEventArgs(ListSTG[nIndex].StatusMachine));
                        OnLoadport_FoupIDChange(ListSTG[nIndex], new EventArgs());
                        OnLoadport_FoupTypeChange(ListSTG[nIndex], ListSTG[nIndex].FoupTypeName);

                        OnLoadport_E84ModeChange(ListE84[nIndex], new E84ModeChangeEventArgs(m_guiloadportList[nIndex].E84Status == GUILoadport.enumE84Status.Auto));
                    }

                    //選片功能第一次要初始化
                    btnAlignFunction.PerformClick();
                    if (panelAlignFunction.Controls != null && panelAlignFunction.Controls.Count > 0)
                        ((Button)panelAlignFunction.Controls[0]).PerformClick();
                    btnRecipeFunction.PerformClick();
                    if (panelRecipeFunction.Controls != null && panelRecipeFunction.Controls.Count > 0)
                        ((Button)panelRecipeFunction.Controls[0]).PerformClick();

                    if (m_SelectALN != null && GParam.theInst.GetAlignerMode(m_SelectALN.BodyNo - 1) == enumAlignerType.TurnTable)
                    {
                        m_SelectALN.GposRW(3000); //  問位置
                        txtTunTablePas.Text = m_SelectALN.Raxispos.ToString();
                        txtTunTablePas_0.Text = GParam.theInst.GetTurnTable_angle_0(m_SelectALN.BodyNo - 1).ToString();
                        txtTunTablePas_180.Text = GParam.theInst.GetTurnTable_angle_180(m_SelectALN.BodyNo - 1).ToString();
                    }

                    if (ListDIO[1].GetGDIO_InputStatus(0, 0)) //is run mode
                    {
                        tabCtrlStage.Visible = false;

                        tabCtrlFunction.TabPages.Remove(tabPageRobot);
                        tabCtrlFunction.TabPages.Remove(tabPageAligner);
                    }

                    if (tlpSelectAligner.Controls.Count > 0)
                    {
                        if (tlpSelectAligner.Controls[0] is Button)
                        {
                            this.BeginInvoke(new Action(() => ((Button)tlpSelectAligner.Controls[0]).PerformClick()));
                        }
                    }
                }
                else
                {
                    foreach (I_Loadport item in ListSTG)
                    {
                        item.OnUclmComplete -= OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        item.OnUclm1Complete -= OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖
                        item.OnFoupExistChenge -= OnLoadport_FoupExistChenge;          //  更新UI
                        item.OnClmpComplete -= OnLoadport_MappingComplete;             //  更新UI
                        item.OnMappingComplete -= OnLoadport_MappingComplete;          //  更新UI
                        item.OnStatusMachineChange -= OnLoadport_StatusMachineChange;  //  更新UI
                        item.OnFoupIDChange -= OnLoadport_FoupIDChange;                //  更新UI
                        item.OnFoupTypeChange -= OnLoadport_FoupTypeChange;            //  更新UI                   
                    }
                    foreach (I_E84 item in ListE84)
                        item.OnAceessModeChange -= OnLoadport_E84ModeChange;                //  更新UI                  

                    //  回復到設定速度
                    foreach (SSRobotRR75x item in ListTRB)//進入畫面自動降maint速度
                    {
                        if (item.Disable) continue;
                        if (item.BodyNo == 2 && dlgIsTkeyOn != null && dlgIsTkeyOn()) continue;
                        item.ResetProcessCompleted();
                        int nSpeed = m_bIsRunMode ? GParam.theInst.GetRobot_RunSpeed(item.BodyNo - 1) : GParam.theInst.GetRobot_MaintSpeed(item.BodyNo - 1);
                        item.SspdW(item.GetAckTimeout, nSpeed);//  回原速度
                        item.WaitProcessCompleted(item.GetAckTimeout);

                        if (item.ExtXaxisDisable == false)
                        {
                            item.TBL_560.ResetProcessCompleted();
                            item.TBL_560.SspdW(item.GetAckTimeout, nSpeed);
                            item.TBL_560.WaitProcessCompleted(item.GetAckTimeout);
                        }
                    }





                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void EnableControlButton(bool bAct)
        {
            this.BeginInvoke(new Action(() =>
            {
                btnRbOrgn.Enabled = bAct;
                btnRbLoad.Enabled = bAct;
                btnRbUnld.Enabled = bAct;
                btnRbClmp.Enabled = bAct;
                btnRbUclm.Enabled = bAct;
                cbxRobotStage.Enabled = bAct;
                cbxRobotSlot.Enabled = bAct;
                cbxRobotArm.Enabled = bAct;

                btnAlignerOrgn.Enabled = bAct;
                btnAlignerAlgn.Enabled = bAct;
                btnAlignerClmp.Enabled = bAct;
                btnAlignerUclm.Enabled = bAct;

                btn_shutterDoorOpen.Enabled = bAct;
                btn_shutterDoorClose.Enabled = bAct;

                tabCtrlStage.Enabled = gpbTRB.Enabled = gpbALN.Enabled = bAct;

                gpbCycle.Enabled = bAct;

                guiNotchAngle1.Enabled = bAct;

            }));
            delegateMDILock?.Invoke(!bAct);//主畫面功能列鎖定

        }

        #region ========== Loadport 註冊事件來更新 UI ==========
        void OnLoadport_OnUclmComplete(object sender, LoadPortEventArgs e)
        {
            //I_Loadport loaderUnit = sender as I_Loadport;
            //EnableControlButton(true);
        }
        void OnLoadport_OnUclm1Complete(object sender, LoadPortEventArgs e)
        {
            //I_Loadport loaderUnit = sender as I_Loadport;
            //EnableControlButton(true);
        }

        void OnLoadport_FoupExistChenge(object sender, FoupExisteChangEventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int index = loaderUnit.BodyNo - 1;
            m_guiloadportList[index].UpdataFoupExist(e.FoupExist);
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
        #endregion

        #region =================== Cycle Function ====================
        bool m_bUseAign = false;
        string m_strRecipe = string.Empty;

        private void btnAlignFunction_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle == true) return;

            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }

            //create button            
            CreateAlignFunctionButton(enumTransferMode.All);
            //show
            showSubPanel(sender, panelAlignFunction);
        }
        private void btnRecipeFunction_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle == true) return;

            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }

            //create button          
            CreateRecipFunctionButton();
            //show
            showSubPanel(sender, panelRecipeFunction);
        }
        private void CreateRecipFunctionButton()
        {
            panelRecipeFunction.Controls.Clear();
            if (m_dbGrouprecipe.GetRecipeGroupList.Count < 1)
            {
                btnRecipeFunction.Enabled = false;
                return;
            }
            btnRecipeFunction.Enabled = true;
            //建立按鈕
            foreach (var item in m_dbGrouprecipe.GetRecipeGroupList.ToArray())
            {
                if (item.Value._M12 != "" && GParam.theInst.IsUnitDisable(enumUnit.OCRA1) && GParam.theInst.IsUnitDisable(enumUnit.OCRB1))
                {
                    continue;
                }
                if (item.Value._T7 != "" && GParam.theInst.IsUnitDisable(enumUnit.OCRA2) && GParam.theInst.IsUnitDisable(enumUnit.OCRB2))
                {
                    continue;
                }
                Button btn = new Button();
                btn.Text = item.Key;
                btn.BackColor = btnRecipeFunction.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;
                btn.Dock = DockStyle.Bottom;
                btn.Size = btnRecipeFunction.Size;
                btn.Click -= btnRecipeFunctionSelect_Click;
                btn.Click += btnRecipeFunctionSelect_Click;
                panelRecipeFunction.Controls.Add(btn);
            }
            panelRecipeFunction.AutoSize = true;
            //設定初始值
            m_strRecipe = btnRecipeFunction.Text = panelRecipeFunction.Controls[0].Text;
        }
        private void btnRecipeFunctionSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            m_strRecipe = btnRecipeFunction.Text = btn.Text;
            panelTransferMenu.Visible = false;
        }
        private void showSubPanel(object sender, Panel subMenu)
        {
            Button btn = sender as Button;
            int nCount = subMenu.Controls.Count;
            panelTransferMenu.Location = new Point(gpbCycle.Location.X + tlpTransferMenu.Location.X + btn.Location.X, gpbCycle.Location.Y + tlpTransferMenu.Location.Y + btn.Location.Y);
            panelTransferMenu.Width = btn.Width;
            panelTransferMenu.Height = btn.Height * nCount;
            panelTransferMenu.Visible = true;
            panelTransferMenu.BringToFront();
            //---------------------------------------
            foreach (Panel item in panelTransferMenu.Controls)
            {
                item.Visible = subMenu == item;
            }
        }
        private void CreateAlignFunctionButton(enumTransferMode eTransferFnc)
        {
            panelAlignFunction.Controls.Clear();

            bool bHardwareHasAlgn = false;
            foreach (I_Aligner aln in ListALN.ToArray()) { bHardwareHasAlgn |= (aln != null && aln.Disable == false); }

            string[] strArray;
            if (bHardwareHasAlgn == false)
            {
                strArray = new string[] { "No Aligner" };
            }
            else if (cbxCycleMode.SelectedIndex == 0)
            {
                strArray = new string[] { "No Aligner", "Aligner" };
            }
            else
            {
                strArray = new string[] { "Aligner" };
            }

            //建立按鈕          
            foreach (string item in strArray)
            {
                Button btn = new Button();
                btn.Text = GParam.theInst.GetLanguage(item);
                btn.BackColor = btnAlignFunction.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;
                btn.Dock = DockStyle.Bottom;
                btn.Size = btnAlignFunction.Size;
                btn.Click -= btnAlignFunctionSelect_Click;
                btn.Click += btnAlignFunctionSelect_Click;
                panelAlignFunction.Controls.Add(btn);
            }
            panelAlignFunction.AutoSize = true;
            //設定初始值
            btnAlignFunction.Text = panelAlignFunction.Controls[0].Text;
            m_bUseAign = (btnAlignFunction.Text == GParam.theInst.GetLanguage("Aligner"));
        }
        private void btnAlignFunctionSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            btnAlignFunction.Text = btn.Text;
            panelTransferMenu.Visible = false;
            m_bUseAign = (btn.Text == GParam.theInst.GetLanguage("Aligner"));
        }


        enum enumDemoStep { Dock, checkDock, Transfer, checkTransfer, Finish }
        enumDemoStep eDemoStep = enumDemoStep.Dock;

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (m_autoProcess.IsCycle == true) return;

            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }

            if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            foreach (I_Robot item in ListTRB)
            {
                if (item.Disable == false && item.IsMoving)
                {
                    frmMessageBox frmMbox = new frmMessageBox(string.Format("Robot Moving"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    frmMbox.ShowDialog();
                    return;
                }
            }

            //只要EQ能選擇Recipe或是有OCR就要考慮Grouprecipe
            if (GParam.theInst.IsAllOcrDisable() == false)
            {
                if (m_dbGrouprecipe.GetRecipeGroupList.ContainsKey(m_strRecipe) == false)
                {
                    new frmMessageBox(string.Format("Recipe is empty or wrong."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
            }

            bool bHardwareHasAlgn = false;//確認硬體
            foreach (I_Aligner aln in ListALN.ToArray())
            { bHardwareHasAlgn |= (aln != null && aln.Disable == false); }//確認硬體

            int FoupOnLoadportCount = 0;
            foreach (I_Loadport item in ListSTG.ToArray())
            {
                if (item.FoupExist)
                {
                    FoupOnLoadportCount++;
                }
            }
            if (FoupOnLoadportCount == 0)
            {
                new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            EnableControlButton(false);

            if (delegateCycleStart != null) delegateCycleStart(this, new EventArgs());


            Task.Run(() =>
            {
                eDemoStep = enumDemoStep.Dock;
                m_bUseAign = true; // 強制開啟
                m_autoProcess.SetCycleRunInfo(m_bUseAign, m_strRecipe);
                m_autoProcess.StartCycleRun();
                int nStartPort = 0;

                while (true)
                {
                    try
                    {
                        SpinWait.SpinUntil(() => false, 100);
                        switch (eDemoStep)
                        {
                            case enumDemoStep.Dock:
                                {
                                    foreach (var loader in ListSTG)
                                    {
                                        if (ListSTG[loader.BodyNo - 1].Disable == false && ListSTG[loader.BodyNo - 1].FoupExist)
                                        {
                                            if (ListSTG[loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped ||
                                                ListSTG[loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Arrived ||
                                                ListSTG[loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_UnDocked ||
                                                ListSTG[loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToUnload)
                                            {

                                                ListSTG[loader.BodyNo - 1].CLMP();
                                            }
                                        }
                                    }
                                    eDemoStep = enumDemoStep.checkDock;
                                    WriteLog("[Demo]:" + eDemoStep);
                                }
                                break;
                            case enumDemoStep.checkDock:
                                {
                                    bool bOK = true;
                                    foreach (var loader in ListSTG)
                                    {
                                        if (ListSTG[loader.BodyNo - 1].Disable == false &&
                                        ListSTG[loader.BodyNo - 1].FoupExist &&
                                        ListSTG[loader.BodyNo - 1].StatusMachine != enumStateMachine.PS_Docked)
                                            bOK = false;
                                    }
                                    if (bOK)
                                    {
                                        if (m_autoProcess.IsCycle)
                                        {
                                            eDemoStep = enumDemoStep.Transfer;
                                            WriteLog("[Demo]:" + eDemoStep);
                                        }
                                        else//被停掉
                                        {
                                            eDemoStep = enumDemoStep.Finish;
                                            WriteLog("[Demo]:" + eDemoStep);
                                        }
                                    }
                                }
                                break;
                            case enumDemoStep.Transfer:
                                {
                                    int nLoadportTotal = Enum.GetNames(typeof(enumLoadport)).Count();
                                    for (int i = 0; i < nLoadportTotal; i++)//搜尋 source loadport
                                    {
                                        I_Loadport source = ListSTG[(nStartPort + i) % nLoadportTotal];
                                        if (source.Disable || source.FoupExist == false) continue;

                                        bool bSourecHasWafer = source.MappingData.Contains('1');
                                        enumWaferSize eSourceWaferType = source.GetCurrentLoadportWaferType();
                                        //模式EFEM
                                        if (cbxCycleMode.SelectedIndex == 0 || cbxCycleMode.SelectedItem.ToString() == "EFEM")
                                        {
                                            if (bSourecHasWafer == false) continue;//沒有wafer跳下一個
                                            //EFEM A->B? A->A?
                                            for (int j = 0; j < nLoadportTotal; j++)//搜尋 target loadport
                                            {
                                                //I_Loadport target = source;
                                                I_Loadport target = ListSTG[(nStartPort + i + j + 1) % nLoadportTotal];
                                                if (target.Disable || target.FoupExist == false) continue;
                                                enumWaferSize eTargetWaferType = source.GetCurrentLoadportWaferType();
                                                if (source.MappingData != target.MappingData && eSourceWaferType == eTargetWaferType)//target loadport 滿足條件
                                                {
                                                    for (int k = 0; k < source.MappingData.Length; k++)//確認 mapping 結果能夠傳
                                                    {
                                                        if (source.MappingData[k] == '1' && target.MappingData[k] == '0')
                                                        {
                                                            nStartPort = source.BodyNo;//下一RUN從下一個LP開始
                                                            WriteLog(string.Format("[Demo]:{0} Loadport{1} to Loadport{2}", eDemoStep, source.BodyNo, target.BodyNo));
                                                            EnableControlButton(true);
                                                            delegateMDITriggerShowMainform?.Invoke();
                                                            SpinWait.SpinUntil(() => false, 100);//一定要等
                                                            delegateDemoStart?.Invoke(source.BodyNo, target.BodyNo);//兩port互傳
                                                            goto NextStep;
                                                        }
                                                    }
                                                }
                                                else
                                                if (source.BodyNo == target.BodyNo && bHardwareHasAlgn)//有Aligner能原去原回
                                                {
                                                    nStartPort = source.BodyNo;//下一RUN從下一個LP開始
                                                    WriteLog(string.Format("[Demo]:{0} Loadport{1} to Loadport{2}", eDemoStep, source.BodyNo, target.BodyNo));
                                                    EnableControlButton(true);
                                                    delegateMDITriggerShowMainform?.Invoke();
                                                    SpinWait.SpinUntil(() => false, 100);//一定要等
                                                    delegateDemoStart?.Invoke(source.BodyNo, target.BodyNo);//原去原回
                                                    goto NextStep;
                                                }

                                                /*if(i == nLoadportTotal - 1 && j == nLoadportTotal - 1)//找完
                                                {
                                                    goto NextStep;
                                                }*/
                                            }
                                        }
                                    }
                                    //找失敗
                                    eDemoStep = enumDemoStep.Finish;
                                    WriteLog("[Demo]:Conditions not met");
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        m_autoProcess.CycleEndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                        //btnAlignFunction.Enabled = true;
                                        //btnRecipeFunction.Enabled = true;
                                        //btnStart.Enabled = true;
                                        m_autoProcess.StopCycleRun();
                                        EnableControlButton(true);
                                        new frmMessageBox("Status cannot be executed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                    }));
                                    return;//END                                  
                                NextStep:

                                    eDemoStep = enumDemoStep.checkTransfer;
                                    WriteLog("[Demo]:" + eDemoStep);
                                    EnableControlButton(true);
                                }
                                break;
                            case enumDemoStep.checkTransfer:
                                {
                                    foreach (var loader in ListSTG)
                                    {
                                        if (ListSTG[loader.BodyNo - 1].Disable == false)
                                        {
                                            if (ListSTG[loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Process)
                                            {
                                                eDemoStep = enumDemoStep.Finish;
                                                WriteLog("[Demo]:" + eDemoStep);
                                                break;
                                            }
                                        }
                                    }
                                }
                                break;
                            case enumDemoStep.Finish:
                                {
                                    bool bOK = true;
                                    foreach (var loader in ListSTG)
                                    {
                                        if (ListSTG[loader.BodyNo - 1].Disable == false)
                                        {
                                            if (
                                            ListSTG[loader.BodyNo - 1].StatusMachine != enumStateMachine.PS_ReadyToUnload &&
                                            ListSTG[loader.BodyNo - 1].StatusMachine != enumStateMachine.PS_ReadyToLoad &&
                                            ListSTG[loader.BodyNo - 1].StatusMachine != enumStateMachine.PS_Docked &&
                                            ListSTG[loader.BodyNo - 1].StatusMachine != enumStateMachine.PS_Arrived)
                                            {
                                                bOK = false;
                                                break;
                                            }
                                        }

                                    }

                                    if (bOK && m_autoProcess.CheckPJCJDone())
                                    {
                                        eDemoStep = enumDemoStep.Dock;
                                        WriteLog("[Demo]:" + eDemoStep);
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            m_autoProcess.CycleEndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                            //if (m_autoProcess.IsCycle == false)
                                            //{
                                            //    btnAlignFunction.Enabled = true;
                                            //    btnRecipeFunction.Enabled = true;
                                            //    btnStart.Enabled = true;
                                            //}
                                        }));
                                        if (m_autoProcess.IsCycle == false)
                                        {
                                            WriteLog("[Demo]: Finish");
                                            return;//END
                                        }
                                    }

                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog("[Exception] " + ex);
                        return;//END
                    }
                }
            }

            );
        }





        #endregion

        private void frmManual_Shown(object sender, EventArgs e)
        {
            if (tlpSelectAligner.Controls.Count > 0)
            {
                if (tlpSelectAligner.Controls[0] is Button)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        tabCtrlFunction.SelectedTab = tabPageAligner;
                        ((Button)tlpSelectAligner.Controls[0]).PerformClick();
                    }));
                }
            }

            if (tlpSelectRobot.Controls.Count > 0)
            {
                if (tlpSelectRobot.Controls[0] is Button)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        tabCtrlFunction.SelectedTab = tabPageRobot;
                        ((Button)tlpSelectRobot.Controls[0]).PerformClick();
                    }));
                }

            }
        }
                
    }
}
