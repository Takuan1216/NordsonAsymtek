using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RorzeUnit.Class;
using RorzeUnit.Interface;
using RorzeApi.Class;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Loadport.Enum;
using RorzeApi.SECSGEM;
using RorzeApi.GUI;
using RorzeComm.Log;
using RorzeUnit.Class.E84.Event;
using System.Collections.Concurrent;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using RorzeUnit.Class.Robot.Enum;
using System.Data;
using System.Windows.Documents;
using RorzeUnit.Class.CIPC;
using RorzeUnit.Class.FFU;
using RorzeComm;
using RorzeComm.Threading;
using RorzeApi._0.GUI_UserCtrl;
using RorzeUnit.Class.EQ;
using System.Diagnostics;
using System.Collections;

namespace RorzeApi
{
    public partial class frmMain : Form
    {
        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion

        enumTransferMode m_eTransferMode = enumTransferMode.Display;
        enumTransferModeType m_eTransferModeType;
        bool m_bNoAign = false;
        string m_strRecipe = String.Empty;

        float m_nFrmX;//當前窗體的寬度
        float m_nFrmY;//當前窗體的高度
        bool m_bFrmLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性


        List<GUILoadport> m_guiloadportList = new List<GUILoadport>();

        //Unit
        List<I_Robot> ListTRB;
        List<I_Loadport> ListSTG;
        List<I_Aligner> ListALN;
        List<I_E84> ListE84;
        List<I_RC5X0_IO> ListDIO;
        List<I_Buffer> ListBUF;
        List<I_OCR> ListOCR;
        List<SSEquipment> ListEQM;
        List<SSFFUCtrlParents> ListFFU;
        //DB
        SProcessDB m_dbProcess;
        SGroupRecipeManager m_dbGrouprecipe;
        //Class
        SAlarm m_alarm;
        STransfer m_autoProcess;      //  自動傳片流程
        PJCJManager m_JobControl;
        SGEM300 m_Gem;
        SSSorterSQL m_DataBase;

        SInterruptOneThread _exeUpdatePJDataViwe;
        SInterruptOneThread _exeUpdateCJDataViwe;

        string m_strUserSelectRecordFilePath = Path.Combine(Application.StartupPath, "UserSelectRecord");
        List<string> m_listUserSelectRecordName = new List<string>();

        public dlgb_v dlgIsTkeyOn { get; set; }

        double m_dNotchAngle = 0;

        #region Form Zoom
        public void SetGUISize(float frmWidth, float frmHeight)
        {
            if (m_bFrmLoaded == false)
            {
                m_nFrmX = this.Width;  //獲取窗體的寬度
                m_nFrmY = this.Height; //獲取窗體的高度      
                m_bFrmLoaded = true;    // 已設定各控制項的尺寸到Tag屬性中
                SetTag(this);       //調用方法
            }
            float tempX = frmWidth / m_nFrmX;  //計算比例
            float tempY = frmHeight / m_nFrmY; //計算比例
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

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");


        private SPermission _userManager;   //  管理LOGIN使用者權限

        delegate void dlgUpdateDataViwe(DataGridView GridView, DataSet DataSource);
        public void ShowDataGridView(DataGridView GridView, DataSet DataSource)
        {
            if (InvokeRequired)
            {
                dlgUpdateDataViwe dlg = new dlgUpdateDataViwe(ShowDataGridView);
                this.Invoke(dlg, GridView, DataSource);
            }
            else
            {
                GridView.DataSource = DataSource.Tables[0];
            }
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        public frmMain(List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN, List<I_E84> e84List, List<I_RC5X0_IO> listDIO, List<I_Buffer> listBUF, List<I_OCR> listOCR, List<SSFFUCtrlParents> listFFU,
                       SAlarm alarm, PJCJManager Job, SGEM300 Gem, SProcessDB dbProcess, SGroupRecipeManager grouprecipe, STransfer autoProcess, SSSorterSQL dataBase, SPermission userManager, List<SSEquipment> listEQM)
        {
            try
            {
                InitializeComponent();


                lblEFEM_Pa.Visible = GParam.theInst.RC550Pressure_Enable;
                //  Unit
                ListTRB = listTRB;
                ListSTG = listSTG;
                ListALN = listALN;
                ListE84 = e84List;
                ListDIO = listDIO;
                ListBUF = listBUF;
                ListOCR = listOCR;

                ListFFU = listFFU;
                ListEQM = listEQM;
                //  DB
                m_dbProcess = dbProcess;
                m_dbGrouprecipe = grouprecipe;
                //  Class
                m_alarm = alarm;
                m_autoProcess = autoProcess;
                m_JobControl = Job;
                m_Gem = Gem;
                m_DataBase = dataBase;
                _userManager = userManager;

                #region Robot
                if (ListTRB[0] != null && ListTRB[0].Disable == false)
                {
                    switch (ListTRB[0].UpperArmFunc)
                    {
                        case enumArmFunction.NONE:
                            guiTRB1_Status.DisableUpper = true;
                            break;
                        case enumArmFunction.NORMAL:
                            guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
                            break;
                        case enumArmFunction.I:
                            guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s4_FingerNoWafer_I;
                            break;
                        case enumArmFunction.FRAME:
                            guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s2_FingerNoFrame;
                            break;
                        default:
                            break;
                    }
                    switch (ListTRB[0].LowerArmFunc)
                    {
                        case enumArmFunction.NONE:
                            guiTRB1_Status.DisableLower = true;
                            break;
                        case enumArmFunction.NORMAL:
                            guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
                            break;
                        case enumArmFunction.I:
                            guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s4_FingerNoWafer_I;
                            break;
                        case enumArmFunction.FRAME:
                            guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s2_FingerNoFrame;
                            break;
                        default:
                            break;
                    }
                    ListTRB[0].LowerArmWaferChange += RobotA_LowerArmWaferChange;       //  更新UI
                    ListTRB[0].UpperArmWaferChange += RobotA_UpperArmWaferChange;       //  更新UI
                }
                if (ListTRB[1] != null && ListTRB[1].Disable == false)
                {
                    switch (ListTRB[1].UpperArmFunc)
                    {
                        case enumArmFunction.NONE:
                            guiTRB2_Status.DisableUpper = true;
                            break;
                        case enumArmFunction.NORMAL:
                            guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
                            break;
                        case enumArmFunction.I:
                            guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s4_FingerNoWafer_I;
                            break;
                        case enumArmFunction.FRAME:
                            guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s2_FingerNoFrame;
                            break;
                        default:
                            break;
                    }
                    switch (ListTRB[1].LowerArmFunc)
                    {
                        case enumArmFunction.NONE:
                            guiTRB2_Status.DisableLower = true;
                            break;
                        case enumArmFunction.NORMAL:
                            guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
                            break;
                        case enumArmFunction.I:
                            guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s4_FingerNoWafer_I;
                            break;
                        case enumArmFunction.FRAME:
                            guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s2_FingerNoFrame;
                            break;
                        default:
                            break;
                    }
                    ListTRB[1].LowerArmWaferChange += RobotB_LowerArmWaferChange;       //  更新UI
                    ListTRB[1].UpperArmWaferChange += RobotB_UpperArmWaferChange;       //  更新UI
                }
                if (ListALN[0] != null && ListALN[0].Disable == false)
                {
                    ListALN[0].Aligner_WaferChange += AlingerA_WaferChange;//  更新UI

                    if (ListALN[0].WaferType == SWafer.enumWaferSize.Frame)
                        guiALN1_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s2_TunTableNoFrame;
                    else
                        guiALN1_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s0_Idle;

                }
                if (ListALN[1] != null && ListALN[1].Disable == false)
                {
                    ListALN[1].Aligner_WaferChange += AlingerB_WaferChange;//  更新UI

                    if (ListALN[1].WaferType == SWafer.enumWaferSize.Frame)
                        guiALN2_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s2_TunTableNoFrame;
                    else
                        guiALN2_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s0_Idle;
                }

                if (ListBUF[0] != null && ListBUF[0].Disable == false) ListBUF[0].OnAssignWaferData += BufferA_OnAssignWaferData;
                if (ListBUF[1] != null && ListBUF[1].Disable == false) ListBUF[1].OnAssignWaferData += BufferB_OnAssignWaferData;
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
                    m_guiloadportList[i].Disable_E84 = GParam.theInst.IsE84Disable(i);
                    m_guiloadportList[i].Disable_OCR = true /*GParam.theInst.IsOcrAllDisable()*/;
                    m_guiloadportList[i].Disable_Recipe = true /*(GParam.theInst.IsOcrAllDisable() && GParam.theInst.GetEQRecipeCanSelect == false)*/;
                    m_guiloadportList[i].Disable_RSV = true;
                    m_guiloadportList[i].Disable_ClmpLock = true;
                    m_guiloadportList[i].Disable_DockBtn = false;

                    m_guiloadportList[i].Disable_ProcessBtn = true;
                    m_guiloadportList[i].Visible = !ListSTG[i].Disable;
                    m_guiloadportList[i].Enabled = !ListSTG[i].Disable;

                    if (ListSTG[i] == null || ListSTG[i].Disable)
                    {
                        continue;//不需要註冊
                    }

                    ListSTG[i].OnFoupExistChenge += OnLoadport_FoupExistChenge;          //  更新UI  
                    ListSTG[i].OnClmpComplete += OnLoadport_MappingComplete;             //  更新UI
                    ListSTG[i].OnMappingComplete += OnLoadport_MappingComplete;          //  更新UI
                    ListSTG[i].OnStatusMachineChange += OnLoadport_StatusMachineChange;  //  更新UI
                    ListSTG[i].OnFoupIDChange += OnLoadport_FoupIDChange;                //  更新UI
                    ListSTG[i].OnFoupTypeChange += OnLoadport_FoupTypeChange;            //  更新UI

                    ListSTG[i].OnUclmComplete += OnLoadport_OnUclmComplete;              //  更新UI
                    ListE84[i].OnAceessModeChange += OnLoadport_E84ModeChange;                //  更新UI
                    ListSTG[i].OnTakeWaferInFoup += m_guiloadportList[i].TakeWaferInFoup;//wafer被塞進來
                    ListSTG[i].OnTakeWaferOutFoup += m_guiloadportList[i].TakeWaferOutFoup;//wafer被拿走

                    m_guiloadportList[i].BtnClamp += btnClamp_Click;
                    m_guiloadportList[i].BtnDock += btnDock_Click;
                    m_guiloadportList[i].BtnUnDock += btnUnDock_Click;
                    m_guiloadportList[i].BtnE84Mode += btnE84Mode_Click;
                    m_guiloadportList[i].ChkRecipeSelect += chkRecipeSelect_Checked;
                    m_guiloadportList[i].BtnProcess += btnStart_Click;
                    m_guiloadportList[i].ChkFoupOn += chkFoupOn_Checked;
                    m_guiloadportList[i].FoupIDKeyDownEnter += txtLoaderFoupID_Enter;

                    m_guiloadportList[i].UseSelectWafer += GuiLoadport_UseSelectWafer;//選片功能

                    m_guiloadportList[i].SelectWaferBySorterMode = GParam.theInst.GetSystemType == enumSystemType.Sorter;
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

                    tabPageEFGH.Parent = null;
                    //  消失頁籤
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
                            item.Width = (flowLayoutPanel1.Width - 5) / nCount;
                            item.Height = flowLayoutPanel1.Height - 5/*+ 20*/;
                        }
                    }
                }
                else//有第二頁
                {
                    tabPageEFGH.Parent = tabCtrlStage;
                    foreach (GUILoadport item in m_guiloadportList)
                    {
                        item.Width = (flowLayoutPanel1.Width - 5) / 4;
                        item.Height = flowLayoutPanel1.Height - 5;
                    }
                }
                #endregion                               

                //隱藏元件
                guiTRB1_Status.Visible = (ListTRB[0] != null && ListTRB[0].Disable == false);
                if (GParam.theInst.FreeStyle) guiTRB1_Status.SetFreeStyleColor(GParam.theInst.ColorTitle, Color.FromArgb(97, 162, 79));

                guiTRB2_Status.Visible = (ListTRB[0] != null && ListTRB[1].Disable == false);
                if (GParam.theInst.FreeStyle) guiTRB2_Status.SetFreeStyleColor(GParam.theInst.ColorTitle, Color.FromArgb(97, 162, 79));

                guiALN1_Status.Visible = (ListALN[0] != null && ListALN[0].Disable == false);
                if (GParam.theInst.FreeStyle) guiALN1_Status.SetFreeStyleColor(GParam.theInst.ColorTitle, Color.FromArgb(97, 162, 79));

                guiALN2_Status.Visible = (ListALN[1] != null && ListALN[1].Disable == false);
                if (GParam.theInst.FreeStyle) guiALN2_Status.SetFreeStyleColor(GParam.theInst.ColorTitle, Color.FromArgb(97, 162, 79));

                guiEquipmentStatus1.Visible = (ListEQM[0] != null && ListEQM[0].Disable == false);
                guiEquipmentStatus1.EQName = ListEQM[0] != null ? ListEQM[0]._Name : "Equipment";
                if (ListEQM[0].Disable == false) ListEQM[0].EQ_WaferChange += Equipment_WaferChange;              //  更新UI

                guiEquipmentStatus2.Visible = (ListEQM[1] != null && ListEQM[1].Disable == false);
                guiEquipmentStatus2.EQName = ListEQM[1] != null ? ListEQM[1]._Name : "Equipment";
                if (ListEQM[1].Disable == false) ListEQM[1].EQ_WaferChange += Equipment_WaferChange;              //  更新UI

                guiEquipmentStatus3.Visible = (ListEQM[2] != null && ListEQM[2].Disable == false);
                guiEquipmentStatus3.EQName = ListEQM[2] != null ? ListEQM[2]._Name : "Equipment";
                if (ListEQM[2].Disable == false) ListEQM[2].EQ_WaferChange += Equipment_WaferChange;              //  更新UI

                guiEquipmentStatus4.Visible = (ListEQM[3] != null && ListEQM[3].Disable == false);
                guiEquipmentStatus4.EQName = ListEQM[3] != null ? ListEQM[3]._Name : "Equipment";
                if (ListEQM[3].Disable == false) ListEQM[3].EQ_WaferChange += Equipment_WaferChange;              //  更新UI

                //-----------------------------------------------------
                if (GParam.theInst.IsUnitDisable(enumUnit.BUF1))
                    guiBUF1_Status.Visible = false;

                if (GParam.theInst.FreeStyle) guiBUF1_Status.SetFreeStyleColor(GParam.theInst.ColorTitle, GParam.theInst.ColorReadyGreen);
                //-----------------------------------------------------
                if (GParam.theInst.IsUnitDisable(enumUnit.BUF2))
                    guiBUF2_Status.Visible = false;

                if (GParam.theInst.FreeStyle) guiBUF2_Status.SetFreeStyleColor(GParam.theInst.ColorTitle, GParam.theInst.ColorReadyGreen);
                //-----------------------------------------------------
                if (GParam.theInst.GetEFEM_FFUComport() == 0)
                {
                    tabPageFFU.Parent = null;
                }

                gbSelectRecord.Visible = GParam.theInst.EnableRandomSelectWafer;


                foreach (Control item in tlpTransferMenu.Controls)
                {
                    if (item is Label && (Label)item == label13)
                    {
                        continue;
                    }
                    if (item is Button && (Button)item == btnRecipeFunction)
                    {
                        continue;
                    }
                    item.Visible = GParam.theInst.EnableRandomSelectWafer;
                }

                cbxViewSlotInfo.SelectedIndex = 0;

                guiNotchAngle1.OnAngleChange += (object sender, double dAngle) => { m_dNotchAngle = dAngle; };

                //隱藏page,消失頁籤
                tabCtrlTransferFnc.SizeMode = TabSizeMode.Fixed;
                tabCtrlTransferFnc.ItemSize = new Size(0, 1);
                tabCtrlTransferFnc.Appearance = TabAppearance.FlatButtons;
                //隱藏page,消失頁籤
                tabCtrlTransferSub.SizeMode = TabSizeMode.Fixed;
                tabCtrlTransferSub.ItemSize = new Size(0, 1);
                tabCtrlTransferSub.Appearance = TabAppearance.FlatButtons;

                tabCtrlStage.TabPages.Remove(tabPage1);

                if (GParam.theInst.FreeStyle)
                {
                    btnTransferShow.BackColor = GParam.theInst.ColorTitle;


                    guiNotchAngle1.SetFreeStyleColor(GParam.theInst.ColorButton);

                    btnTransferFunction.BackColor = GParam.theInst.ColorButton;
                    btnTransferFunctionType.BackColor = GParam.theInst.ColorButton;
                    btnTransferSource.BackColor = btnTransferTarget.BackColor = GParam.theInst.ColorButton;
                    btnAlignFunction.BackColor = GParam.theInst.ColorButton;
                    btnRecipeFunction.BackColor = GParam.theInst.ColorButton;
                }

                _exeUpdatePJDataViwe = new SInterruptOneThread(RunUpdatePJDataViwe);
                _exeUpdateCJDataViwe = new SInterruptOneThread(RunUpdateCJDataViwe);

                m_Gem.OnProcessJobUpdate += M_Gem_OnProcessJobUpdate;
                m_Gem.OnControlJobUpdate += M_Gem_OnControlJobUpdate;

                DGVPJlist.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                DGVCJlist.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }


        ConcurrentQueue<clsSelectWaferInfo> m_QueSelectSlotNum = new ConcurrentQueue<clsSelectWaferInfo>();//只有選Wafer，不確定去哪裡
        ConcurrentQueue<clsSelectWaferInfo> m_QueWaferJob = new ConcurrentQueue<clsSelectWaferInfo>();//紀錄Wafer傳片紀錄，clear的時候清掉      

        /// <summary>
        /// GUI Loadport點選slot位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GuiLoadport_UseSelectWafer(object sender, GUILoadport.EventArgs_SelectWafer e)//GUI MouseUp
        {
            GUILoadport guiLoadport = sender as GUILoadport;
            bool bIsReturnSameSlot = m_eTransferMode == enumTransferMode.Display || m_eTransferMode == enumTransferMode.Notch;//原去原回
            bool bShowNotchAngle = m_eTransferMode == enumTransferMode.Notch;

            if (e.SelectSlotNum.Count() <= 0) return;

            // Has Wafer
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

                if (m_eTransferMode == enumTransferMode.Random)//如果是任意傳送才需要
                {
                    foreach (var lp in m_guiloadportList)
                    {
                        bool bSelectWafer = m_QueSelectSlotNum.Count() > 0;
                        lp.EnableUISelectPutWaferFlag(bSelectWafer);
                    }
                }
            }
            // No Wafer
            if (e.SelectSlotSts[0] == enumUIPickWaferStat.NoWafer ||
                e.SelectSlotSts[0] == enumUIPickWaferStat.ExeHasWafer || bIsReturnSameSlot)
            {
                //剛選擇的片子判斷要放那些空位
                if (m_QueSelectSlotNum.Count() > 0)
                {
                    for (int i = 0; i < e.SelectSlotSts.Count(); i++)
                    {
                        clsSelectWaferInfo temp;
                        if (m_QueSelectSlotNum.TryDequeue(out temp))
                        {
                            temp.SetTargetSlotIdx(e.SelectSlotNum[i]);
                            temp.SetTargetLpBodyNo(guiLoadport.BodyNo);
                            if (bShowNotchAngle) temp.SetNotchAngle(m_dNotchAngle);

                            m_QueWaferJob.Enqueue(temp);

                            string strFromName = (char)(64 + temp.SourceLpBodyNo) + (temp.SourceSlotIdx + 1).ToString("D2");
                            string strToName = (char)(64 + temp.TargetLpBodyNo) + (temp.TargetSlotIdx + 1).ToString("D2");

                            m_guiloadportList[temp.SourceLpBodyNo - 1].ResetSlotSelectFlag(strToName, temp.SourceSlotIdx);
                            m_guiloadportList[guiLoadport.BodyNo - 1].UserSelectPlaceWaferInLoadport(strFromName, temp.TargetSlotIdx, bShowNotchAngle ? m_dNotchAngle : -1);
                        }
                        else
                        {
                            m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                        }
                    }
                    if (m_QueSelectSlotNum.Count() == 0)
                    {
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(false);
                        }
                    }
                }
                else
                {
                    //選擇目標空，上一次並未選片
                    foreach (var lp in m_guiloadportList)
                    {
                        lp.EnableUISelectPutWaferFlag(false);
                    }
                }
            }

        }

        #region ========== Button ==========
        private void btnClamp_Click(object sender, EventArgs e)
        {
            try
            {
                if (_userManager.IsLogin == false && GParam.theInst.IsSimulate == false)
                {
                    new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }

                GUILoadport Loader = (GUILoadport)sender;

                //if (!ListSTG[Loader.BodyNo - 1].FoupExist)
                //{
                //    new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                //    return;
                //}
                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                if (m_autoProcess.IsCycle)
                {
                    new frmMessageBox("Is in cycle run. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }


                if (ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Arrived ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_UnDocked ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToUnload ||
                ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToLoad)
                {
                    ListSTG[Loader.BodyNo - 1].KeepClamp(!ListSTG[Loader.BodyNo - 1].IsKeepClamp);
                }
                else
                {
                    string strMsg = string.Format(GParam.theInst.GetLanguage("Loadport satus {0} can't clamp"), ListSTG[Loader.BodyNo - 1].StatusMachine);
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
        private void btnDock_Click(object sender, EventArgs e)
        {
            try
            {
                if (_userManager.IsLogin == false && GParam.theInst.IsSimulate == false)
                {
                    new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
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
                if (m_autoProcess.IsCycle)
                {
                    new frmMessageBox("Is in cycle run. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                if (ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Arrived ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_UnDocked ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToUnload)
                {
                    ListSTG[Loader.BodyNo - 1].CLMP(true); // Need Check Foup Type
                }
                else
                {
                    string strMsg = string.Format(GParam.theInst.GetLanguage("Loadport satus {0} can't dock"), ListSTG[Loader.BodyNo - 1].StatusMachine);
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

        private void btnDock_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (_userManager.IsLogin == false && m_autoProcess.IsCycle == false && GParam.theInst.IsSimulate == false)
                {
                    new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                    return;
                if (m_autoProcess.IsCycle)
                    return;

                foreach (I_Loadport lp in ListSTG)
                {
                    if (lp.Disable) continue;
                    if (lp.FoupExist == false) continue;
                    if (lp.StatusMachine == enumStateMachine.PS_Clamped ||
                        lp.StatusMachine == enumStateMachine.PS_Arrived ||
                        lp.StatusMachine == enumStateMachine.PS_UnDocked ||
                        lp.StatusMachine == enumStateMachine.PS_ReadyToUnload)
                    {
                        lp.CLMP(true); // Need Check Foup Type
                    }

                }
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
                if (_userManager.IsLogin == false && m_autoProcess.IsCycle == false && GParam.theInst.IsSimulate == false)
                {
                    new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
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
                if (m_autoProcess.IsCycle)
                {
                    new frmMessageBox("Is in cycle run. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }


                if (ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Docked ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Stop ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Complete ||
                    ListSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped)//211227 增加Clamped
                {
                    ListSTG[Loader.BodyNo - 1].UCLM();
                }
                else
                {
                    string strMsg = string.Format(GParam.theInst.GetLanguage("Loadport satus {0} can't undock"), ListSTG[Loader.BodyNo - 1].StatusMachine);
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

        private void btnE84Mode_Click(object sender, EventArgs e)
        {
            if (_userManager.IsLogin == false && m_autoProcess.IsCycle == false)
            {
                new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            if (m_autoProcess.IsCycle)
            {
                new frmMessageBox("Is in cycle run. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            GUILoadport Loader = (GUILoadport)sender;

            if (GParam.theInst.IsE84Disable(Loader.BodyNo - 1)) return;

            if (ListE84[Loader.BodyNo - 1] == null) return;

            if (ListE84[Loader.BodyNo - 1].GetAutoMode == false)
                ListE84[Loader.BodyNo - 1].SetAutoMode(true);
            else
                ListE84[Loader.BodyNo - 1].SetAutoMode(false);
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
        private void chkRecipeSelect_Checked(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;
            int index = Loader.BodyNo - 1;
            try
            {
                //string strRecipe = m_guiloadportList[index].Recipe;
                ////確認有這個recipe
                //if (m_dbGrouprecipe.GetRecipeGroupList.ContainsKey(strRecipe) == false) return;

                //lblGoupRecipeName.Text = strRecipe;
                //GParam.theInst.SetDefaultRecipe(strRecipe);//紀錄使用哪一筆
                //SGroupRecipe RecipeContent = m_dbGrouprecipe.GetRecipeGroupList[strRecipe];

                //if (RecipeContent.EQRecipe != "")
                //{
                //    foreach (string EQRecipe in m_equipment.RecipeList())
                //    {
                //        if (EQRecipe == RecipeContent.EQRecipe)
                //        {
                //            txtEQRecipeSelect.BackColor = Color.LightBlue;
                //            txtEQRecipeSelect.Text = RecipeContent.EQRecipe;
                //            break;
                //        }
                //    }
                //}
                //else
                //{
                //    txtEQRecipeSelect.BackColor = SystemColors.Control;
                //    txtEQRecipeSelect.Text = "";
                //}

                //if (RecipeContent.Front_Recipe != "")
                //{
                //    for (int i = 0; i < GParam.theInst.GetOCRRecipeIniFile(true).Count; i++)
                //    {
                //        if (RecipeContent.Front_Recipe == GParam.theInst.GetOCRRecipeIniFile(true)[i].Name)
                //        {
                //            lblOCR_FrontUse.BackColor = Color.LightBlue;
                //            txtFrontRecipeSelect.BackColor = Color.LightBlue;
                //            txtFrontRecipeSelect.Text = RecipeContent.Front_Recipe;
                //            break;
                //        }
                //    }
                //}
                //else
                //{
                //    lblOCR_FrontUse.BackColor = SystemColors.Control;
                //    txtFrontRecipeSelect.BackColor = SystemColors.Control;
                //    txtFrontRecipeSelect.Text = "";
                //}

                //if (RecipeContent.Back_Recipe != "")
                //{
                //    for (int i = 0; i < GParam.theInst.GetOCRRecipeIniFile(false).Count; i++)
                //    {
                //        if (RecipeContent.Back_Recipe == GParam.theInst.GetOCRRecipeIniFile(false)[i].Name)
                //        {
                //            lblOCR_BackUse.BackColor = Color.LightBlue;
                //            txtBackRecipeSelect.BackColor = Color.LightBlue;
                //            txtBackRecipeSelect.Text = RecipeContent.Back_Recipe;
                //            break;
                //        }
                //    }
                //}
                //else
                //{
                //    lblOCR_BackUse.BackColor = SystemColors.Control;
                //    txtBackRecipeSelect.BackColor = SystemColors.Control;
                //    txtBackRecipeSelect.Text = "";
                //}


            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        private void txtLoaderFoupID_Enter(object sender, EventArgs e)//手動key Foup ID
        {
            GUILoadport Loader = (GUILoadport)sender;
            int index = Loader.BodyNo - 1;

            if (!ListSTG[index].FoupExist)
            {
                new frmMessageBox("Loadport has no foup.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                Loader.FoupID = string.Empty;
                return;
            }
            if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if ((ListSTG[index].StatusMachine == enumStateMachine.PS_Clamped ||
                 ListSTG[index].StatusMachine == enumStateMachine.PS_Arrived ||
                 ListSTG[index].StatusMachine == enumStateMachine.PS_ReadyToUnload)
                 && ListSTG[index].FoupExist)
            {

                if (new frmMessageBox(string.Format("Are you want to change Loader#{0} Foup ID ", Loader.BodyNo), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
                {
                    Loader.FoupID = ListSTG[index].FoupID;
                    return;
                }
                ListSTG[index].FoupID = Loader.FoupID;
            }
            else
            {
                new frmMessageBox("Loadport status isn't Arrived or ReadyToUnload.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
        }

        #endregion

        //========== 畫面事件處理常式
        private void tmrUpdateUI_Tick(object sender, EventArgs e)
        {
            try
            {
                //return;
                tmrUpdateUI.Enabled = false;

                //注意這裡先寫死兩層
                //guiBUF1_Status.SetSlot = 2;//因為更換語系，一定要放這裡
                //guiBUF2_Status.SetSlot = 2;//因為更換語系，一定要放這裡

                #region 更新 UserCtrl Status

                if (ListTRB.Count > 0 && ListTRB[0] != null)
                {
                    if (!ListTRB[0].Connected && !GParam.theInst.IsSimulate)
                    {
                        guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disconnect;
                    }
                    else if (ListTRB[0].Disable)
                    {
                        guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disable;
                    }
                    else if (ListTRB[0].IsMoving)
                    {
                        guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Moving;
                    }
                    else if (ListTRB[0].IsError)
                    {
                        guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Error;
                    }
                    else if (ListTRB[0].StatMode == RorzeUnit.Class.Robot.Enum.enumRobotMode.TeachingPendent)
                    {
                        guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.TeachPendant;
                    }
                    else if (ListTRB[0].IsOrgnComplete)
                    {
                        guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Idle;
                    }
                    else
                    {
                        if (!ListTRB[0].Connected)
                        {
                            guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disconnect;
                        }
                        else
                            guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Unknown;
                    }
                }

                if (ListTRB.Count > 1 && ListTRB[1] != null)
                {
                    if (!ListTRB[1].Connected && !GParam.theInst.IsSimulate)
                    {
                        guiTRB2_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disconnect;
                    }
                    else if (ListTRB[1].Disable)
                    {
                        guiTRB2_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disable;
                    }
                    else if (ListTRB[1].IsMoving)
                    {
                        guiTRB2_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Moving;
                    }
                    else if (ListTRB[1].IsError)
                    {
                        guiTRB2_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Error;
                    }
                    else if (ListTRB[0].StatMode == RorzeUnit.Class.Robot.Enum.enumRobotMode.TeachingPendent)
                    {
                        guiTRB1_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.TeachPendant;
                    }
                    else if (ListTRB[1].IsOrgnComplete)
                    {
                        guiTRB2_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Idle;
                    }
                    else
                    {
                        if (!ListTRB[1].Connected)
                        {
                            guiTRB2_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disconnect;
                        }
                        else
                            guiTRB2_Status.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Unknown;
                    }
                }

                if (ListALN.Count > 0 && ListALN[0] != null)
                {
                    if (ListALN[0].Disable)
                    {
                        guiALN1_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Disable;
                    }
                    else if (ListALN[0].IsMoving)
                    {
                        guiALN1_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Moving;
                    }
                    else if (ListALN[0].IsError)
                    {
                        guiALN1_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Error;
                    }
                    else if (ListALN[0].IsOrgnComplete)
                    {
                        guiALN1_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Idle;
                    }
                    else
                    {
                        guiALN1_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Unknown;
                    }
                }

                if (ListALN.Count > 1 && ListALN[1] != null)
                {
                    if (ListALN[1].Disable)
                    {
                        guiALN2_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Disable;
                    }
                    else if (ListALN[1].IsMoving)
                    {
                        guiALN2_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Moving;
                    }
                    else if (ListALN[1].IsError)
                    {
                        guiALN2_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Error;
                    }
                    else if (ListALN[1].IsOrgnComplete)
                    {
                        guiALN2_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Idle;
                    }
                    else
                    {
                        guiALN2_Status.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Unknown;
                    }
                }

                if (ListBUF.Count > 0)
                    if (ListBUF[0] == null || ListBUF[0].Disable)
                    {
                        guiBUF1_Status.SetStatus = _0.GUI_UserCtrl.GUIBufferStatus.enumState.Disable;
                    }
                    else
                    {
                        guiBUF1_Status.SetStatus = _0.GUI_UserCtrl.GUIBufferStatus.enumState.Idle;
                    }
                if (ListBUF.Count > 1)
                    if (ListBUF[1] == null || ListBUF[1].Disable)
                    {
                        guiBUF2_Status.SetStatus = _0.GUI_UserCtrl.GUIBufferStatus.enumState.Disable;
                    }
                    else
                    {
                        guiBUF2_Status.SetStatus = _0.GUI_UserCtrl.GUIBufferStatus.enumState.Idle;
                    }

                #endregion

                #region ========== Loadport ==========
                for (int nPort = 0; nPort < ListSTG.Count; nPort++)
                {
                    if (ListSTG[nPort].Disable) continue;

                    m_guiloadportList[nPort].KeepClamp = ListSTG[nPort].IsKeepClamp;

                    if (ListSTG[nPort].Waferlist == null) continue;

                    List<SWafer> wafer = ListSTG[nPort].Waferlist.ToList();

                    if (wafer == null) continue;

                    for (int nSlot = 1; nSlot <= wafer.Count; nSlot++)
                    {

                        SWafer waferShow = wafer[nSlot - 1];

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


                if (ListEQM.Count > 0 && ListEQM[0] != null)
                {
                    if (ListEQM[0].Disable)
                    {
                        guiEquipmentStatus1.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Disable;
                    }
                    else if (ListEQM[0].IsProcessing)
                    {
                        guiEquipmentStatus1.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Moving;
                    }
                    else if (ListEQM[0].IsError)
                    {
                        guiEquipmentStatus1.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Error;
                    }
                    else if (ListEQM[0].IsReady)
                    {
                        guiEquipmentStatus1.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Idle;
                    }
                    else
                    {
                        guiEquipmentStatus1.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Unknown;
                    }
                }

                if (ListEQM.Count > 1 && ListEQM[1] != null)
                {
                    if (ListEQM[1].Disable)
                    {
                        guiEquipmentStatus2.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Disable;
                    }
                    else if (ListEQM[1].XYZ_Status == RorzeUnit.Class.EQ.Enum.enumMachineStatus.ACTION)
                    {
                        guiEquipmentStatus2.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Moving;
                    }
                    else if (ListEQM[1].XYZ_Status == RorzeUnit.Class.EQ.Enum.enumMachineStatus.ALARM)
                    {
                        guiEquipmentStatus2.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Error;
                    }
                    else if (ListEQM[1].XYZ_Status == RorzeUnit.Class.EQ.Enum.enumMachineStatus.IDLE)
                    {
                        guiEquipmentStatus2.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Idle;
                    }
                    else
                    {
                        guiEquipmentStatus2.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Unknown;
                    }
                }

                if (ListEQM.Count > 2 && ListEQM[2] != null)
                {
                    if (ListEQM[2].Disable)
                    {
                        guiEquipmentStatus3.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Disable;
                    }
                    else if (ListEQM[2].IsProcessing)
                    {
                        guiEquipmentStatus3.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Moving;
                    }
                    else if (ListEQM[2].IsError)
                    {
                        guiEquipmentStatus3.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Error;
                    }
                    else if (ListEQM[2].IsReady)
                    {
                        guiEquipmentStatus3.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Idle;
                    }
                    else
                    {
                        guiEquipmentStatus3.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Unknown;
                    }
                }

                if (ListEQM.Count > 3 && ListEQM[3] != null)
                {
                    if (ListEQM[3].Disable)
                    {
                        guiEquipmentStatus4.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Disable;
                    }
                    else if (ListEQM[3].IsProcessing)
                    {
                        guiEquipmentStatus4.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Moving;
                    }
                    else if (ListEQM[3].IsError)
                    {
                        guiEquipmentStatus4.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Error;
                    }
                    else if (ListEQM[3].IsReady)
                    {
                        guiEquipmentStatus4.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Idle;
                    }
                    else
                    {
                        guiEquipmentStatus4.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Unknown;
                    }
                }

                if (lblEFEM_Pa.Visible && ListDIO.Count > 0 && ListDIO[0].Disable == false)
                {
                    double dValue = ((double)ListDIO[0].GetSenGprs[0]) / 1000;
                    string str = GParam.theInst.GetLanguage("Differential pressure(Pa):");
                    lblEFEM_Pa.Text = str + dValue.ToString();
                    if (dValue < GParam.theInst.RC550Pressure_Threshold && GParam.theInst.RC550Pressure_Threshold > 0)
                    {
                        lblEFEM_Pa.ForeColor = Color.Red;
                    }
                    else
                    {
                        lblEFEM_Pa.ForeColor = Color.Black;
                    }
                }


                switch (GMotion.theInst.eTransfeStatus)
                {
                    case enumTransfeStatus.Idle:
                        lblTransferStatus.Text = GParam.theInst.GetLanguage("IDLE");
                        break;
                    case enumTransfeStatus.Transfe:
                        lblTransferStatus.Text = GParam.theInst.GetLanguage(m_autoProcess.IsCycle ? "CYCLE" : "TRANSFER");
                        break;
                    case enumTransfeStatus.Abort:
                        lblTransferStatus.Text = GParam.theInst.GetLanguage("ABORT");
                        break;
                    case enumTransfeStatus.Stop:
                        lblTransferStatus.Text = GParam.theInst.GetLanguage("STOP");
                        break;
                    case enumTransfeStatus.Pause:
                        lblTransferStatus.Text = GParam.theInst.GetLanguage("PAUSE");
                        break;
                }

                if (m_autoProcess.IsCycle)
                {

                    if (GParam.theInst.EnableRandomSelectWafer == false)
                    {
                        if (gbSelectRecord.Visible == true) gbSelectRecord.Visible = false;//沒有選片功能
                    }
                    else
                    {
                        if (gbSelectRecord.Visible == true) gbSelectRecord.Visible = !gbSelectRecord.Visible;
                    }

                    lblCycleStartTime.Text = m_autoProcess.CycleStartTime;

                    if (lblCycleEndTime.Text != m_autoProcess.CycleEndTime)
                    {
                        DateTime dtEnd, dtStart;
                        if (DateTime.TryParseExact(m_autoProcess.CycleEndTime, "yyyy/MM/dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out dtEnd))
                        {
                            if (DateTime.TryParseExact(m_autoProcess.CycleStartTime, "yyyy/MM/dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out dtStart))
                            {
                                TimeSpan ts = dtEnd - dtStart;
                                double dSeconds = ts.TotalSeconds;
                                if (dSeconds == 0)
                                {
                                    lblCycleWPH.Text = "0";
                                }
                                else
                                {
                                    double dPiece = dSeconds / m_autoProcess.CycleCount;//一片花費時間
                                    double d25Slost = dPiece * 25 + 20;//25片加上dock/undock時間
                                    double dWPH = 60 * 60 / d25Slost * 25;//一小時可以做幾組
                                    lblCycleWPH.Text = dWPH.ToString("0.00");
                                }
                            }
                        }
                    }

                    lblCycleEndTime.Text = m_autoProcess.CycleEndTime;
                    lblCycleCount.Text = m_autoProcess.CycleCount.ToString();
                }
                else
                {

                    if (GParam.theInst.EnableRandomSelectWafer == false)
                    {
                        if (gbSelectRecord.Visible == true) gbSelectRecord.Visible = false;//沒有選片功能
                    }
                    else
                    {
                        if (gbSelectRecord.Visible == false) gbSelectRecord.Visible = !gbSelectRecord.Visible;
                    }
                }

                if (m_autoProcess.IsPrepareForEnd())
                {
                    btnStop.Text = "Waiting for end";
                    if (btnStop.ForeColor == Color.DeepSkyBlue)
                        btnStop.ForeColor = SystemColors.ControlText;
                    else
                        btnStop.ForeColor = Color.DeepSkyBlue;
                }
                else
                {
                    if (btnStop.ForeColor != SystemColors.ControlText)
                        btnStop.ForeColor = SystemColors.ControlText;


                    string btnStopText = GParam.theInst.GetLanguage("Stop");
                    if (btnStop.Text != btnStopText)
                        btnStop.Text = btnStopText;
                }

                if (tabCtrlStage.SelectedTab == tabPageFFU)
                {
                    if (ListFFU[0]._Disable == false)
                    {
                        Label[] lp_name = new Label[] { lblEFEM_FFU1_Name, lblEFEM_FFU2_Name };
                        Label[] lp_value = new Label[] { lblEFEM_FFU1, lblEFEM_FFU2 };
                        for (int i = 0; i < lp_name.Count(); i++)
                        {
                            lp_name[i].Text = string.Format("FFU({0}~{1})", ListFFU[0].GetSpeedMin()[i], ListFFU[0].GetSpeedMax()[i]);
                            int nValue1 = ListFFU[0].GetSpeed()[i];
                            lp_value[i].Text = nValue1.ToString();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
            tmrUpdateUI.Enabled = true;
        }
        private void frmMain_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                tmrUpdateUI.Enabled = this.Visible;

                if (this.Visible)
                {


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
                            //OnLoadport_MappingComplete(ListSTG[nIndex], new LoadPortEventArgs(ListSTG[nIndex].MappingData, ListSTG[nIndex].BodyNo, true));
                            OnLoadport_StatusMachineChange(ListSTG[nIndex], new OccurStateMachineChangEventArgs(ListSTG[nIndex].StatusMachine));
                            OnLoadport_FoupIDChange(ListSTG[nIndex], new EventArgs());
                            OnLoadport_FoupTypeChange(ListSTG[nIndex], ListSTG[nIndex].FoupTypeName);
                            OnLoadport_E84ModeChange(ListE84[nIndex], new E84ModeChangeEventArgs(m_guiloadportList[nIndex].E84Status == GUILoadport.enumE84Status.Auto));
                        }
                    }

                    if (m_autoProcess.IsCycle == false)
                    {
                        //選片功能第一次要初始化
                        btnTransferFunction.PerformClick();
                        if (panelTransferFunction.Controls != null && panelTransferFunction.Controls.Count > 0)
                            ((Button)panelTransferFunction.Controls[0]).PerformClick();
                    }

                    ReloadUserSelectRecordFile();
                    cmbUserSelectRecord.Items.Clear();
                    foreach (var item in m_listUserSelectRecordName.ToArray())
                    {
                        cmbUserSelectRecord.Items.Add(item);
                    }

                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        private void frmMain_Load(object sender, EventArgs e)
        {

            if (ListFFU[0] != null && ListFFU[0]._Disable)
            {
                Label[] lp_name = new Label[] { lblEFEM_FFU1_Name, lblEFEM_FFU2_Name };
                Label[] lp_value = new Label[] { lblEFEM_FFU1, lblEFEM_FFU2 };
                for (int i = 0; i < lp_name.Count(); i++)
                {
                    lp_name[i].Visible = lp_value[i].Visible = false;
                }
            }

            foreach (I_Aligner item in ListALN)
            {
                if (item == null || item.Disable) continue;

                switch (item.WaferType)
                {
                    case SWafer.enumWaferSize.Inch12:
                    case SWafer.enumWaferSize.Inch08:
                    case SWafer.enumWaferSize.Inch06:
                        guiNotchAngle1._Type = GUINotchAngle.enumType.Wafer;
                        break;
                    case SWafer.enumWaferSize.Frame:

                        break;
                    case SWafer.enumWaferSize.Panel:
                        guiNotchAngle1._Type = GUINotchAngle.enumType.Panel;
                        break;
                    default:
                        break;
                }
                break;
            }
                  
            cbxViewSlotInfo.Visible = true/* (GParam.theInst.IsOcrAllDisable() == false) &&*/ ;

        }
        //  更新UI
        void RobotA_UpperArmWaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            I_Robot theRobot = sender as I_Robot;
            if (theRobot.Disable) return;
            enumArmFunction eArmFunction = theRobot.UpperArmFunc;
            //上arm upper料帳資訊
            SWafer waferShow = e.Wafer;
            if (waferShow != null)
            {
                guiTRB1_Status.SetUpperWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);
                guiTRB1_Status.SetUpperWaferRecipe = waferShow.RecipeID;
                if (waferShow.WaferSize == SWafer.enumWaferSize.Panel)
                    guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s6_FingerHavePanel;
                else
                {
                    switch (eArmFunction)
                    {
                        case enumArmFunction.NONE:
                        case enumArmFunction.NORMAL:
                            guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s1_HaveWafer;
                            break;
                        case enumArmFunction.I:
                            guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s5_FingerHaveWafer_I;
                            break;
                        case enumArmFunction.FRAME:
                            guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s3_FingerHaveFrame;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                guiTRB1_Status.SetUpperWaferSlotNo = "-";
                guiTRB1_Status.SetUpperWaferRecipe = "-";
                switch (eArmFunction)
                {
                    case enumArmFunction.NONE:
                    case enumArmFunction.NORMAL:
                        guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
                        break;
                    case enumArmFunction.I:
                        guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s4_FingerNoWafer_I;
                        break;
                    case enumArmFunction.FRAME:
                        guiTRB1_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s2_FingerNoFrame;
                        break;
                    default:
                        break;
                }
            }
        }
        void RobotA_LowerArmWaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            I_Robot theRobot = sender as I_Robot;
            if (theRobot.Disable) return;
            enumArmFunction eArmFunction = theRobot.LowerArmFunc;
            //下arm lower料帳資訊
            SWafer waferShow = e.Wafer;
            if (waferShow != null)
            {
                guiTRB1_Status.SetLowerWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);
                guiTRB1_Status.SetLowerWaferRecipe = waferShow.RecipeID;
                if (waferShow.WaferSize == SWafer.enumWaferSize.Panel)
                    guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s6_FingerHavePanel;
                else
                {
                    switch (eArmFunction)
                    {
                        case enumArmFunction.NONE:
                        case enumArmFunction.NORMAL:
                            guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s1_HaveWafer;
                            break;
                        case enumArmFunction.I:
                            guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s5_FingerHaveWafer_I;
                            break;
                        case enumArmFunction.FRAME:
                            guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s3_FingerHaveFrame;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                guiTRB1_Status.SetLowerWaferSlotNo = "-";
                guiTRB1_Status.SetLowerWaferRecipe = "-";
                switch (eArmFunction)
                {
                    case enumArmFunction.NONE:
                    case enumArmFunction.NORMAL:
                        guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
                        break;
                    case enumArmFunction.I:
                        guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s4_FingerNoWafer_I;
                        break;
                    case enumArmFunction.FRAME:
                        guiTRB1_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s2_FingerNoFrame;
                        break;
                    default:
                        break;
                }
            }
        }
        void RobotB_UpperArmWaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            I_Robot theRobot = sender as I_Robot;
            if (theRobot.Disable) return;
            enumArmFunction eArmFunction = theRobot.UpperArmFunc;
            //上arm upper料帳資訊
            SWafer waferShow = e.Wafer;
            if (waferShow != null)
            {
                guiTRB2_Status.SetUpperWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);

                guiTRB2_Status.SetUpperWaferRecipe = waferShow.RecipeID;
                if (waferShow.WaferSize == SWafer.enumWaferSize.Panel)
                    guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s6_FingerHavePanel;
                else
                {
                    switch (eArmFunction)
                    {
                        case enumArmFunction.NONE:
                        case enumArmFunction.NORMAL:
                            guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s1_HaveWafer;
                            break;
                        case enumArmFunction.I:
                            guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s5_FingerHaveWafer_I;
                            break;
                        case enumArmFunction.FRAME:
                            guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s3_FingerHaveFrame;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                guiTRB2_Status.SetUpperWaferSlotNo = "-";
                guiTRB2_Status.SetUpperWaferRecipe = "-";
                switch (eArmFunction)
                {
                    case enumArmFunction.NONE:
                    case enumArmFunction.NORMAL:
                        guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
                        break;
                    case enumArmFunction.I:
                        guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s4_FingerNoWafer_I;
                        break;
                    case enumArmFunction.FRAME:
                        guiTRB2_Status.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s2_FingerNoFrame;
                        break;
                    default:
                        break;
                }
            }
        }
        void RobotB_LowerArmWaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            I_Robot theRobot = sender as I_Robot;
            if (theRobot.Disable) return;
            enumArmFunction eArmFunction = theRobot.LowerArmFunc;
            //下arm lower料帳資訊
            SWafer waferShow = e.Wafer;
            if (waferShow != null)
            {
                guiTRB2_Status.SetLowerWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);
                guiTRB2_Status.SetLowerWaferRecipe = waferShow.RecipeID;
                if (waferShow.WaferSize == SWafer.enumWaferSize.Panel)
                    guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s6_FingerHavePanel;
                else
                {
                    switch (eArmFunction)
                    {
                        case enumArmFunction.NONE:
                        case enumArmFunction.NORMAL:
                            guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s1_HaveWafer;
                            break;
                        case enumArmFunction.I:
                            guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s5_FingerHaveWafer_I;
                            break;
                        case enumArmFunction.FRAME:
                            guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s3_FingerHaveFrame;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                guiTRB2_Status.SetLowerWaferSlotNo = "-";
                guiTRB2_Status.SetLowerWaferRecipe = "-";
                switch (eArmFunction)
                {
                    case enumArmFunction.NONE:
                    case enumArmFunction.NORMAL:
                        guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
                        break;
                    case enumArmFunction.I:
                        guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s4_FingerNoWafer_I;
                        break;
                    case enumArmFunction.FRAME:
                        guiTRB2_Status.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s2_FingerNoFrame;
                        break;
                    default:
                        break;
                }
            }
        }
        void AlingerA_WaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            I_Aligner aligner = sender as I_Aligner;
            if (aligner.Disable) return;
            SWafer waferShow = aligner.Wafer;
            if (waferShow != null)
            {
                guiALN1_Status.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);

                guiALN1_Status.SetWaferRecipe = waferShow.RecipeID;
                if (aligner.WaferType == SWafer.enumWaferSize.Frame)
                    guiALN1_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s3_TunTableHasFrame;
                else if (aligner.WaferType == SWafer.enumWaferSize.Panel)
                    guiALN1_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s4_PanelAlignerHasPanel;
                else
                    guiALN1_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s1_HaveWafer;
            }
            else
            {
                guiALN1_Status.SetWaferSlotNo = "-";
                guiALN1_Status.SetWaferRecipe = "-";
                if (aligner.WaferType == SWafer.enumWaferSize.Frame)
                    guiALN1_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s2_TunTableNoFrame;
                else
                    guiALN1_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s0_Idle;
            }

        }
        void AlingerB_WaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            I_Aligner aligner = sender as I_Aligner;
            if (aligner.Disable) return;
            SWafer waferShow = aligner.Wafer;
            if (waferShow != null)
            {
                guiALN2_Status.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);

                guiALN2_Status.SetWaferRecipe = waferShow.RecipeID;
                if (aligner.WaferType == SWafer.enumWaferSize.Frame)
                    guiALN2_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s3_TunTableHasFrame;
                else if (aligner.WaferType == SWafer.enumWaferSize.Panel)
                    guiALN2_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s4_PanelAlignerHasPanel;
                else
                    guiALN2_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s1_HaveWafer;
            }
            else
            {
                guiALN2_Status.SetWaferSlotNo = "-";

                guiALN2_Status.SetWaferRecipe = "-";
                if (aligner.WaferType == SWafer.enumWaferSize.Frame)
                    guiALN2_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s2_TunTableNoFrame;
                else
                    guiALN2_Status.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s0_Idle;
            }
        }
        void BufferA_OnAssignWaferData(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            I_Buffer buffer = sender as I_Buffer;
            if (buffer.Disable) return;
            if (e.Wafer != null)
            {
                switch (e.Slot)
                {
                    case 1:
                        guiBUF1_Status.SetBuf1WaferSlotNo = string.Format("{0} ({1}\")", e.Wafer.Slot, e.Wafer.WaferSize);
                        guiBUF1_Status.SetBuf1WaferRecipe = e.Wafer.RecipeID;
                        guiBUF1_Status.SetBuf1WaferStatus = GUI.GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        break;
                    case 2:
                        guiBUF1_Status.SetBuf2WaferSlotNo = string.Format("{0} ({1}\")", e.Wafer.Slot, e.Wafer.WaferSize);
                        guiBUF1_Status.SetBuf2WaferRecipe = e.Wafer.RecipeID;
                        guiBUF1_Status.SetBuf2WaferStatus = GUI.GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        break;
                    case 3:
                        guiBUF1_Status.SetBuf3WaferSlotNo = string.Format("{0} ({1}\")", e.Wafer.Slot, e.Wafer.WaferSize);
                        guiBUF1_Status.SetBuf3WaferRecipe = e.Wafer.RecipeID;
                        guiBUF1_Status.SetBuf3WaferStatus = GUI.GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        break;
                    case 4:
                        guiBUF1_Status.SetBuf4WaferSlotNo = string.Format("{0} ({1}\")", e.Wafer.Slot, e.Wafer.WaferSize);
                        guiBUF1_Status.SetBuf4WaferRecipe = e.Wafer.RecipeID;
                        guiBUF1_Status.SetBuf4WaferStatus = GUI.GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        break;
                }
            }
            else
            {
                switch (e.Slot)
                {
                    case 1:
                        guiBUF1_Status.SetBuf1WaferSlotNo = "-";
                        guiBUF1_Status.SetBuf1WaferRecipe = "-";
                        guiBUF1_Status.SetBuf1WaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 2:
                        guiBUF1_Status.SetBuf2WaferSlotNo = "-";
                        guiBUF1_Status.SetBuf2WaferRecipe = "-";
                        guiBUF1_Status.SetBuf2WaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 3:
                        guiBUF1_Status.SetBuf3WaferSlotNo = "-";
                        guiBUF1_Status.SetBuf3WaferRecipe = "-";
                        guiBUF1_Status.SetBuf3WaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 4:
                        guiBUF1_Status.SetBuf4WaferSlotNo = "-";
                        guiBUF1_Status.SetBuf4WaferRecipe = "-";
                        guiBUF1_Status.SetBuf4WaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                }
            }

        }
        void BufferB_OnAssignWaferData(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            I_Buffer buffer = sender as I_Buffer;
            if (buffer.Disable) return;
            if (e.Wafer != null)
            {
                switch (e.Slot)
                {
                    case 1:
                        guiBUF2_Status.SetBuf1WaferSlotNo = string.Format("{0} ({1}\")", e.Wafer.Slot, e.Wafer.WaferSize);
                        guiBUF2_Status.SetBuf1WaferRecipe = e.Wafer.RecipeID;
                        guiBUF2_Status.SetBuf1WaferStatus = GUI.GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        break;
                    case 2:
                        guiBUF2_Status.SetBuf2WaferSlotNo = string.Format("{0} ({1}\")", e.Wafer.Slot, e.Wafer.WaferSize);
                        guiBUF2_Status.SetBuf2WaferRecipe = e.Wafer.RecipeID;
                        guiBUF2_Status.SetBuf2WaferStatus = GUI.GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        break;
                    case 3:
                        guiBUF2_Status.SetBuf3WaferSlotNo = string.Format("{0} ({1}\")", e.Wafer.Slot, e.Wafer.WaferSize);
                        guiBUF2_Status.SetBuf3WaferRecipe = e.Wafer.RecipeID;
                        guiBUF2_Status.SetBuf3WaferStatus = GUI.GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        break;
                    case 4:
                        guiBUF2_Status.SetBuf4WaferSlotNo = string.Format("{0} ({1}\")", e.Wafer.Slot, e.Wafer.WaferSize);
                        guiBUF2_Status.SetBuf4WaferRecipe = e.Wafer.RecipeID;
                        guiBUF2_Status.SetBuf4WaferStatus = GUI.GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        break;
                }
            }
            else
            {
                switch (e.Slot)
                {
                    case 1:
                        guiBUF2_Status.SetBuf1WaferSlotNo = "-";
                        guiBUF2_Status.SetBuf1WaferRecipe = "-";
                        guiBUF2_Status.SetBuf1WaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 2:
                        guiBUF2_Status.SetBuf2WaferSlotNo = "-";
                        guiBUF2_Status.SetBuf2WaferRecipe = "-";
                        guiBUF2_Status.SetBuf2WaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 3:
                        guiBUF2_Status.SetBuf3WaferSlotNo = "-";
                        guiBUF2_Status.SetBuf3WaferRecipe = "-";
                        guiBUF2_Status.SetBuf3WaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 4:
                        guiBUF2_Status.SetBuf4WaferSlotNo = "-";
                        guiBUF2_Status.SetBuf4WaferRecipe = "-";
                        guiBUF2_Status.SetBuf4WaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                }
            }

        }
        void Equipment_WaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            SSEquipment equipment = sender as SSEquipment;
            if (equipment == null || equipment.Disable) return;
            SWafer waferShow = equipment.Wafer;

            if (waferShow != null)
            {
                switch (equipment._BodyNo)
                {
                    case 1:
                        {
                            guiEquipmentStatus1.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);
                            guiEquipmentStatus1.SetWaferRecipe = waferShow.RecipeID;
                            if (waferShow.WaferSize == SWafer.enumWaferSize.Frame)
                                guiEquipmentStatus1.SetWaferStatus = GUIEquipment.enuWaferStatus.s2_HaveFRAME;
                            else if (waferShow.WaferSize == SWafer.enumWaferSize.Panel)
                                guiEquipmentStatus1.SetWaferStatus = GUIEquipment.enuWaferStatus.s3_HavePanel;
                            else
                                guiEquipmentStatus1.SetWaferStatus = GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        }
                        break;
                    case 2:
                        {
                            guiEquipmentStatus2.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);
                            guiEquipmentStatus2.SetWaferRecipe = waferShow.RecipeID;
                            if (waferShow.WaferSize == SWafer.enumWaferSize.Frame)
                                guiEquipmentStatus2.SetWaferStatus = GUIEquipment.enuWaferStatus.s2_HaveFRAME;
                            else if (waferShow.WaferSize == SWafer.enumWaferSize.Panel)
                                guiEquipmentStatus2.SetWaferStatus = GUIEquipment.enuWaferStatus.s3_HavePanel;
                            else
                                guiEquipmentStatus2.SetWaferStatus = GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        }
                        break;
                    case 3:
                        {
                            guiEquipmentStatus3.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);
                            guiEquipmentStatus3.SetWaferRecipe = waferShow.RecipeID;
                            if (waferShow.WaferSize == SWafer.enumWaferSize.Frame)
                                guiEquipmentStatus3.SetWaferStatus = GUIEquipment.enuWaferStatus.s2_HaveFRAME;
                            else if (waferShow.WaferSize == SWafer.enumWaferSize.Panel)
                                guiEquipmentStatus3.SetWaferStatus = GUIEquipment.enuWaferStatus.s3_HavePanel;
                            else
                                guiEquipmentStatus3.SetWaferStatus = GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        }
                        break;
                    case 4:
                        {
                            guiEquipmentStatus4.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);
                            guiEquipmentStatus4.SetWaferRecipe = waferShow.RecipeID;
                            if (waferShow.WaferSize == SWafer.enumWaferSize.Frame)
                                guiEquipmentStatus4.SetWaferStatus = GUIEquipment.enuWaferStatus.s2_HaveFRAME;
                            else if (waferShow.WaferSize == SWafer.enumWaferSize.Panel)
                                guiEquipmentStatus4.SetWaferStatus = GUIEquipment.enuWaferStatus.s3_HavePanel;
                            else
                                guiEquipmentStatus4.SetWaferStatus = GUIEquipment.enuWaferStatus.s1_HaveWafer;
                        }
                        break;

                }


            }
            else
            {
                switch (equipment._BodyNo)
                {
                    case 1:
                        guiEquipmentStatus1.SetWaferSlotNo = "-";
                        guiEquipmentStatus1.SetWaferRecipe = "-";
                        guiEquipmentStatus1.SetWaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 2:
                        guiEquipmentStatus2.SetWaferSlotNo = "-";
                        guiEquipmentStatus2.SetWaferRecipe = "-";
                        guiEquipmentStatus2.SetWaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 3:
                        guiEquipmentStatus3.SetWaferSlotNo = "-";
                        guiEquipmentStatus3.SetWaferRecipe = "-";
                        guiEquipmentStatus3.SetWaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                    case 4:
                        guiEquipmentStatus4.SetWaferSlotNo = "-";
                        guiEquipmentStatus4.SetWaferRecipe = "-";
                        guiEquipmentStatus4.SetWaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
                        break;
                }
            }
        }

        //  Loadport 註冊事件來更新 UI
        void OnLoadport_OnUclmComplete(object sender, LoadPortEventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            //EnableControlButton(true);
            int index = loaderUnit.BodyNo - 1;
            m_guiloadportList[index].ResetUpdateMappingData();
            ClearSelectWafer();
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

            switch (loaderUnit.GetCurrentLoadportWaferType())
            {               
                case SWafer.enumWaferSize.Inch12:                 
                case SWafer.enumWaferSize.Inch08:                 
                case SWafer.enumWaferSize.Inch06:
                    m_guiloadportList[index]._Type = GUILoadport.enumType.Wafer;
                    break;
                case SWafer.enumWaferSize.Frame:
                    break;
                case SWafer.enumWaferSize.Panel:
                    m_guiloadportList[index]._Type = GUILoadport.enumType.Panel;
                    break;              
            }



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

        //========== 建帳     
        private void cbxViewSlotInfo_SelectionChangeCommitted(object sender, EventArgs e)
        {
            foreach (GUILoadport item in m_guiloadportList)
            {
                item.SetcbxViewSlot(cbxViewSlotInfo.SelectedIndex);
            }
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            lock (this)
            {
                try
                {
                    #region Check Interlock
                    // Login
                    if (_userManager.IsLogin == false && m_autoProcess.IsCycle == false && GParam.theInst.IsSimulate == false)
                    {
                        new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        return;
                    }
                    // TkeyOn
                    if (dlgIsTkeyOn != null && dlgIsTkeyOn())
                    {
                        new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        return;
                    }
                    // PIN SAFETY
                    if (ListTRB[1].Disable == false && ListTRB[1].PinSafety == false)
                    {
                        new frmMessageBox("Please check pin safety.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        return;
                    }
                    // Need Origin
                    if (GMotion.theInst.InitOrgnDone == false)
                    {
                        new frmMessageBox("Please run the initialization first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        return;
                    }
                    // Need Aligner
                    if (m_eTransferMode == enumTransferMode.Display && m_bNoAign == true)
                    {
                        new frmMessageBox(string.Format("Display need alignment."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    #endregion

                    m_autoProcess.InitalStopFlag();
                    clsSelectWaferInfo selectInfo = null;
                    if (false == m_QueWaferJob.TryPeek(out selectInfo))
                    {
                        new frmMessageBox(string.Format("Please select wafer."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    ConcurrentQueue<clsSelectWaferInfo> SelectWaferInfoQueue = m_QueWaferJob;

                    #region  ============ Safety ============
                    if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                    {
                        new frmMessageBox("Now control status is Online Remote ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    //=========== 不能有異常
                    if (m_alarm.CurrentAlarm != null && m_alarm.CurrentAlarm.Count > 0)
                    {
                        if (m_alarm.IsOnlyWarning() == false)//如果是有警告
                        {
                            new frmMessageBox("There are uncleared abnormalities, please confirm the machine status first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                    }
                    //=========== 檢查Robot狀態
                    foreach (I_Robot rb in ListTRB)
                    {
                        if (rb != null && rb.Disable == false && rb.IsOrgnComplete == false)
                        {
                            new frmMessageBox(string.Format("Robot is not orgned."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                    }
                    //=========== 檢查Loadport狀態
                    foreach (clsSelectWaferInfo clsSelectWaferInfo in SelectWaferInfoQueue.ToArray())
                    {
                        if (clsSelectWaferInfo.SourceLpBodyNo > 0)
                        {
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].FoupExist == false)
                            {
                                new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return;
                            }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].StatusMachine == enumStateMachine.PS_Process)
                            {
                                new frmMessageBox("Loadport StatusMachine is PS_Process.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return;
                            }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].StatusMachine != enumStateMachine.PS_Docked)
                            {
                                new frmMessageBox("Loadport StatusMachine is not PS_Docked.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return;
                            }
                            if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].FoupID.Trim().Length <= 0)
                            {
                                new frmMessageBox("Loadport foup id is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return;
                            }
                            foreach (char item in ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].MappingData)
                            {
                                if (item != '0' && item != '1')
                                {
                                    new frmMessageBox("Loadport mapping error.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                    return;
                                }
                            }
                        }

                        if (clsSelectWaferInfo.TargetLpBodyNo > 0)
                        {
                            if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].FoupExist == false)
                            {
                                new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return;
                            }
                            else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].StatusMachine == enumStateMachine.PS_Process)
                            {
                                new frmMessageBox("Loadport StatusMachine is PS_Process.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return;
                            }
                            else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].StatusMachine != enumStateMachine.PS_Docked)
                            {
                                new frmMessageBox("Loadport StatusMachine is not PS_Docked.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return;
                            }
                            else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].FoupID.Trim().Length <= 0)
                            {
                                new frmMessageBox("Loadport foup id is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return;
                            }
                            foreach (char item in ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].MappingData)
                            {
                                if (item != '0' && item != '1')
                                {
                                    new frmMessageBox("Loadport mapping error.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                    return;
                                }
                            }
                        }
                    }
                    //=========== 只要EQ能選擇Recipe或是有OCR就要考慮Grouprecipe
                    if (GParam.theInst.IsAllOcrDisable() == false)
                    {
                        if (m_dbGrouprecipe.GetRecipeGroupList.ContainsKey(m_strRecipe) == false)
                        {
                            new frmMessageBox(string.Format("Recipe is empty or wrong."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return;
                        }
                    }
                    #endregion

                    //手動要再次確認
                    if (m_autoProcess.IsCycle == false)
                    {
                        if (new frmMessageBox(string.Format("Are you want to Process start?", selectInfo.SourceLpBodyNo), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
                        {
                            ClearSelectWafer();
                            return;
                        }
                    }

                    if (m_autoProcess.CreateJob(ref m_QueWaferJob, m_bNoAign, m_strRecipe) == false)//Main
                    {
                        new frmMessageBox(string.Format("Create TransferJob fail!!!"), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }

                    if (tabCtrlTransferFnc.SelectedTab != tabPageUnit)
                        btnTransferShow.PerformClick();

                    //ClearSelectWafer();//正常啟動不用清除了，為了維持UI顯示
                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                }
            }
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_userManager.IsLogin == false)
            {
                new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            //TkeyOn
            if (dlgIsTkeyOn != null && dlgIsTkeyOn())
            {
                new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }

            if (GMotion.theInst.eTransfeStatus != enumTransfeStatus.Transfe) return;

            if (m_JobControl.CJlist.Count == 0) return;//沒有帳料不用停止

            bool bCycle = m_autoProcess.IsCycle;

            m_autoProcess.PrepareForEnd();

            if (bCycle)
            {
                new frmMessageBox(
                    string.Format("Cycle Results\nStart:{0}\nEnd:{1}\nCount:{2}\nWPH:{3}", lblCycleStartTime.Text, lblCycleEndTime.Text, lblCycleCount.Text, lblCycleWPH.Text),
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information).ShowDialog();
            }
        }

        #region ========= Create Select Menu Wafer Function =========    
        private void AnalyzeModeToCombineJob()
        {
            ClearSelectWafer();
            switch (m_eTransferMode)
            {
                case enumTransferMode.All:
                    {
                        if (panelTransferTarget.Controls.Count == 0 || panelTransferSource.Controls.Count == 0)
                            return;
                        string sourceLP = btnTransferSource.Text;
                        string targetLP = btnTransferTarget.Text;
                        char cSourceLP = sourceLP[0];
                        char cTargetLP = targetLP[0];
                        enumLoadport sourceLPIdx = (enumLoadport)(char.ToUpper(cSourceLP) - 65);
                        enumLoadport targetLPIdx = (enumLoadport)(char.ToUpper(cTargetLP) - 65);
                        if (ListSTG[(int)sourceLPIdx].StatusMachine != enumStateMachine.PS_Docked
                         || ListSTG[(int)targetLPIdx].StatusMachine != enumStateMachine.PS_Docked)
                            return;
                        FunctionAll(sourceLPIdx, targetLPIdx);
                    }
                    break;
                case enumTransferMode.Pack:
                    {
                        FunctionPack();
                    }
                    break;
                default:
                    break;
            }
        }
        private void FunctionAll(enumLoadport sourceLP, enumLoadport targetLP)
        {
            switch (m_eTransferModeType)
            {
                case enumTransferModeType.SameSlot:
                    #region  SameSlot
                    {
                        for (int i = 0; i < m_guiloadportList[(int)sourceLP].WaferCount; i++)
                        {
                            if (m_guiloadportList[(int)sourceLP].LstSlot[i].IsWaferOn && m_guiloadportList[(int)sourceLP].LstSlot[i].WaferSts == enumUIPickWaferStat.HasWafer)
                            {
                                if (m_guiloadportList[(int)targetLP].LstSlot[i].IsWaferOn == false && m_guiloadportList[(int)targetLP].LstSlot[i].WaferSts == enumUIPickWaferStat.NoWafer)
                                {
                                    List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                    List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                    List<int> listSelectSourceSlot = new List<int>();
                                    List<int> listSelectTargetSlot = new List<int>();
                                    listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                    listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                    listSelectSourceSlot.Add(i);
                                    listSelectTargetSlot.Add(i);
                                    GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                    GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                }
                            }
                        }
                    }
                    break;
                #endregion
                case enumTransferModeType.FromTop:
                    #region  FromTop 從來源slot1開始看，有片，從目標slot25開始看，沒片
                    {
                        for (int i = 0; i < m_guiloadportList[(int)sourceLP].WaferCount; i++)
                        {
                            if (m_guiloadportList[(int)sourceLP].LstSlot[i].IsWaferOn)
                            {
                                for (int j = m_guiloadportList[(int)targetLP].WaferCount - 1; j >= 0; j--)
                                {
                                    //倒著檢查
                                    if (m_guiloadportList[(int)targetLP].LstSlot[j].WaferSts == enumUIPickWaferStat.NoWafer)
                                    {
                                        List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                        List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                        List<int> listSelectSourceSlot = new List<int>();
                                        List<int> listSelectTargetSlot = new List<int>();
                                        listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                        listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                        listSelectSourceSlot.Add(i);
                                        listSelectTargetSlot.Add(j);
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
                #endregion
                case enumTransferModeType.FromTop_S:
                    #region  FromTop_translation
                    {
                        for (int i = m_guiloadportList[(int)sourceLP].WaferCount - 1; i >= 0; i--)
                        {
                            if (m_guiloadportList[(int)sourceLP].LstSlot[i].IsWaferOn)
                            {
                                for (int j = m_guiloadportList[(int)targetLP].WaferCount - 1; j >= 0; j--)
                                {
                                    //倒著檢查
                                    if (m_guiloadportList[(int)targetLP].LstSlot[j].WaferSts == enumUIPickWaferStat.NoWafer)
                                    {
                                        List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                        List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                        List<int> listSelectSourceSlot = new List<int>();
                                        List<int> listSelectTargetSlot = new List<int>();
                                        listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                        listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                        listSelectSourceSlot.Add(i);
                                        listSelectTargetSlot.Add(j);
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                        break;
                                    }
                                }

                            }
                        }

                        if (m_eTransferModeType == enumTransferModeType.FromTop_S)
                        {
                            List<clsSelectWaferInfo> temp = new List<clsSelectWaferInfo>();
                            while (m_QueWaferJob.Count() > 0)
                            {
                                clsSelectWaferInfo temp2 = null;
                                m_QueWaferJob.TryDequeue(out temp2);
                                temp.Add(temp2);
                            }
                            for (int i = 0; i < temp.Count(); i++)
                            {
                                m_QueWaferJob.Enqueue(temp[temp.Count() - 1 - i]);
                            }
                        }
                    }
                    break;
                #endregion
                case enumTransferModeType.FromBottom_S:
                    #region  FromBottom_translation
                    {
                        for (int i = 0; i < m_guiloadportList[(int)sourceLP].WaferCount; i++)
                        {
                            if (m_guiloadportList[(int)sourceLP].LstSlot[i].IsWaferOn)
                            {
                                for (int j = 0; j < m_guiloadportList[(int)targetLP].WaferCount; j++)
                                {
                                    if (m_guiloadportList[(int)targetLP].LstSlot[j].WaferSts == enumUIPickWaferStat.NoWafer)
                                    {
                                        List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                        List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                        List<int> listSelectSourceSlot = new List<int>();
                                        List<int> listSelectTargetSlot = new List<int>();
                                        listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                        listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                        listSelectSourceSlot.Add(i);
                                        listSelectTargetSlot.Add(j);
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
                #endregion
                case enumTransferModeType.FromBottom:
                    #region  FromBottom
                    {
                        for (int i = m_guiloadportList[(int)sourceLP].WaferCount - 1; i >= 0; i--)
                        //for (int i = 0; i < m_guiloadportList[(int)sourceLP].WaferCount; i++)
                        {
                            if (m_guiloadportList[(int)sourceLP].LstSlot[i].IsWaferOn)
                            {
                                for (int j = 0; j < m_guiloadportList[(int)targetLP].WaferCount; j++)
                                {
                                    if (m_guiloadportList[(int)targetLP].LstSlot[j].WaferSts == enumUIPickWaferStat.NoWafer)
                                    {
                                        List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                        List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                        List<int> listSelectSourceSlot = new List<int>();
                                        List<int> listSelectTargetSlot = new List<int>();
                                        listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                        listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                        listSelectSourceSlot.Add(i);
                                        listSelectTargetSlot.Add(j);
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                        break;
                                    }
                                }
                            }
                        }
                        //List<clsSelectWaferInfo> temp = new List<clsSelectWaferInfo>();
                        //while (m_QueWaferJob.Count() > 0)
                        //{
                        //    clsSelectWaferInfo temp2 = null;
                        //    m_QueWaferJob.TryDequeue(out temp2);
                        //    temp.Add(temp2);
                        //}
                        //for (int i = 0; i < temp.Count(); i++)
                        //{
                        //    m_QueWaferJob.Enqueue(temp[temp.Count() - 1 - i]);
                        //}
                    }
                    break;
                #endregion
                case enumTransferModeType.Match:
                    #region  Match
                    {
                        //希望送去的目標一定要空的FOUP
                        int nFoupWafer = 0;
                        int nFoupSlotEmpty = 0;
                        foreach (char c in ListSTG[(int)targetLP].MappingData)
                        {
                            if (c == '1') { nFoupWafer++; }
                            else if (c == '0') { nFoupSlotEmpty++; }
                        }

                        if (nFoupSlotEmpty == m_guiloadportList[(int)sourceLP].WaferCount)
                        {
                            for (int i = 0; i < m_guiloadportList[(int)sourceLP].WaferCount; i++)
                            {
                                if (m_guiloadportList[(int)sourceLP].LstSlot[i].IsWaferOn && m_guiloadportList[(int)sourceLP].LstSlot[i].WaferSts == enumUIPickWaferStat.HasWafer)
                                {
                                    if (m_guiloadportList[(int)targetLP].LstSlot[i].IsWaferOn == false && m_guiloadportList[(int)targetLP].LstSlot[i].WaferSts == enumUIPickWaferStat.NoWafer)
                                    {
                                        List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                        List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                        List<int> listSelectSourceSlot = new List<int>();
                                        List<int> listSelectTargetSlot = new List<int>();
                                        listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                        listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                        listSelectSourceSlot.Add(i);
                                        listSelectTargetSlot.Add(i);
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                        GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                    }
                                }
                            }
                        }
                    }
                    break;
                #endregion
                default:
                    break;
            }
        }
        private void FunctionPack()
        {
            switch (m_eTransferModeType)
            {
                case enumTransferModeType.FromTop:
                    #region
                    {
                        for (int i = 0; i < ListSTG.Count; i++)
                        {
                            if (ListSTG[i].Disable || ListSTG[i].StatusMachine != enumStateMachine.PS_Docked) continue;
                            int lastSlotIdx = m_guiloadportList[i].WaferCount;

                            for (int j = 0; j < m_guiloadportList[i].WaferCount; j++)
                            {
                                if (m_guiloadportList[i].LstSlot[j].IsWaferOn && m_guiloadportList[i].LstSlot[j].WaferSts == enumUIPickWaferStat.HasWafer)
                                {
                                    for (int k = lastSlotIdx - 1; k >= 0; k--)
                                    {
                                        if ((m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.NoWafer
                                          || m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.ExeHasWafer)
                                          && j < k)
                                        {
                                            List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                            List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                            List<int> listSelectSourceSlot = new List<int>();
                                            List<int> listSelectTargetSlot = new List<int>();
                                            listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                            if (m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.NoWafer)
                                                listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                            else if (m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.ExeHasWafer)
                                                listSelectTargetSlotSts.Add(enumUIPickWaferStat.ExeHasWafer);
                                            listSelectSourceSlot.Add(j);
                                            listSelectTargetSlot.Add(k);
                                            GuiLoadport_UseSelectWafer(m_guiloadportList[i], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                            GuiLoadport_UseSelectWafer(m_guiloadportList[i], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                            lastSlotIdx = k;
                                            break;
                                        }
                                    }
                                }
                            }



                        }
                    }
                    break;
                #endregion
                case enumTransferModeType.FromTop_S:
                    #region
                    {
                        for (int i = 0; i < ListSTG.Count; i++)
                        {
                            if (ListSTG[i].Disable || ListSTG[i].StatusMachine != enumStateMachine.PS_Docked) continue;
                            int lastSlotIdx = m_guiloadportList[i].WaferCount;
                            for (int j = m_guiloadportList[i].WaferCount - 1; j >= 0; j--)
                            {
                                if (m_guiloadportList[i].LstSlot[j].IsWaferOn && m_guiloadportList[i].LstSlot[j].WaferSts == enumUIPickWaferStat.HasWafer)
                                {
                                    for (int k = lastSlotIdx - 1; k >= 0; k--)
                                    {
                                        if ((m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.NoWafer || m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.ExeHasWafer) &&
                                            j < k)
                                        {
                                            List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                            List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                            List<int> listSelectSourceSlot = new List<int>();
                                            List<int> listSelectTargetSlot = new List<int>();
                                            listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                            listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                            listSelectSourceSlot.Add(j);
                                            listSelectTargetSlot.Add(k);
                                            GuiLoadport_UseSelectWafer(m_guiloadportList[i], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                            GuiLoadport_UseSelectWafer(m_guiloadportList[i], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                            lastSlotIdx = k;
                                            break;
                                        }
                                    }

                                }
                            }

                        }
                    }
                    break;
                #endregion
                case enumTransferModeType.FromBottom:
                    #region
                    {
                        for (int i = 0; i < ListSTG.Count; i++)
                        {
                            if (ListSTG[i].Disable || ListSTG[i].StatusMachine != enumStateMachine.PS_Docked) continue;
                            for (int j = m_guiloadportList[i].WaferCount - 1; j >= 0; j--)
                            {
                                if (m_guiloadportList[i].LstSlot[j].IsWaferOn && m_guiloadportList[i].LstSlot[j].WaferSts == enumUIPickWaferStat.HasWafer)
                                {
                                    for (int k = 0; k < m_guiloadportList[i].WaferCount; k++)
                                    {
                                        if ((m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.NoWafer || m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.ExeHasWafer) &&
                                            j > k)
                                        {
                                            List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                            List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                            List<int> listSelectSourceSlot = new List<int>();
                                            List<int> listSelectTargetSlot = new List<int>();

                                            listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);

                                            if (m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.NoWafer)
                                                listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                            else if (m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.ExeHasWafer)
                                                listSelectTargetSlotSts.Add(enumUIPickWaferStat.ExeHasWafer);

                                            listSelectSourceSlot.Add(j);
                                            listSelectTargetSlot.Add(k);
                                            GuiLoadport_UseSelectWafer(m_guiloadportList[i], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                            GuiLoadport_UseSelectWafer(m_guiloadportList[i], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                            break;
                                        }
                                    }

                                }
                            }
                        }
                    }
                    break;
                #endregion
                case enumTransferModeType.FromBottom_S:
                    #region
                    {
                        for (int i = 0; i < ListSTG.Count; i++)
                        {
                            if (ListSTG[i].Disable || ListSTG[i].StatusMachine != enumStateMachine.PS_Docked) continue;
                            int lastSlotIdx = m_guiloadportList[i].WaferCount;
                            for (int j = 0; j < m_guiloadportList[i].WaferCount; j++)
                            {
                                if (m_guiloadportList[i].LstSlot[j].IsWaferOn && m_guiloadportList[i].LstSlot[j].WaferSts == enumUIPickWaferStat.HasWafer)
                                {
                                    for (int k = 0; k < m_guiloadportList[i].WaferCount; k++)
                                    {
                                        if ((m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.NoWafer || m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.ExeHasWafer) &&
                                            j > k)
                                        {
                                            List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                            List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                            List<int> listSelectSourceSlot = new List<int>();
                                            List<int> listSelectTargetSlot = new List<int>();

                                            listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);

                                            if (m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.NoWafer)
                                                listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                            else if (m_guiloadportList[i].LstSlot[k].WaferSts == enumUIPickWaferStat.ExeHasWafer)
                                                listSelectTargetSlotSts.Add(enumUIPickWaferStat.ExeHasWafer);

                                            listSelectSourceSlot.Add(j);
                                            listSelectTargetSlot.Add(k);
                                            GuiLoadport_UseSelectWafer(m_guiloadportList[i], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                            GuiLoadport_UseSelectWafer(m_guiloadportList[i], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                                            lastSlotIdx = k;
                                            break;
                                        }
                                    }

                                }
                            }

                        }
                    }
                    break;
                    #endregion
            }
        }
        private void ClearSelectWafer()
        {
            while (true)
            {
                clsSelectWaferInfo temp;
                if (m_QueWaferJob.Count() > 0) m_QueWaferJob.TryDequeue(out temp);
                else break;
            }

            while (true)
            {
                clsSelectWaferInfo temp;
                if (m_QueSelectSlotNum.Count() > 0) m_QueSelectSlotNum.TryDequeue(out temp);
                else break;
            }

            for (int i = 0; i < ListSTG.Count; i++)
            {
                if (ListSTG[i].StatusMachine == enumStateMachine.PS_Process) continue;
                m_guiloadportList[i].EnableUISelectPutWaferFlag(false);
                m_guiloadportList[i].ResetUpdateMappingData();
            }

        }
        private void btnUIPickWaferAllClear_Click(object sender, EventArgs e)
        {
            if (_userManager.IsLogin == false && GParam.theInst.IsSimulate == false)
            {
                new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }

            ClearSelectWafer();
        }
        //==================================================================================================
        Dictionary<string, Button> m_DicPanelTransferFunctionTypeButton = new Dictionary<string, Button>();
        private void btnTransferFunction_Click(object sender, EventArgs e)
        {
            //create button
            CreateTransferFunctionButton();
            //show
            showSubPanel(sender, panelTransferFunction);
        }
        private void CreateTransferFunctionButton()
        {
            bool bHardwareHasAlgn = false;
            bool bHardwareTurntable = false;
            foreach (I_Aligner aln in ListALN.ToArray())
            {
                bHardwareHasAlgn |= (aln != null && aln.Disable == false);
                bHardwareTurntable |= (aln != null && aln.Disable == false && aln.WaferType == SWafer.enumWaferSize.Frame);
            }


            panelTransferFunction.Controls.Clear();
            m_DicPanelTransferFunctionTypeButton.Clear();
            foreach (enumTransferMode enumType in Enum.GetValues(typeof(enumTransferMode)))
            {
                if (enumType == enumTransferMode.Notch && bHardwareHasAlgn == false) continue;
                if (enumType == enumTransferMode.Notch && bHardwareTurntable == true) continue;
                if (enumType == enumTransferMode.Display && bHardwareHasAlgn == false) continue;

                Button btn = new Button();
                btn.Text = GParam.theInst.GetLanguage(GetEnumDescription(enumType));
                if (GParam.theInst.FreeStyle)
                    btn.BackColor = btnTransferFunction.Text == btn.Text ? GParam.theInst.ColorButton/*ColorTitle*/ : Color.Transparent/*GParam.theInst.ColorButton*/;
                else
                    btn.BackColor = btnTransferFunction.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;

                btn.Dock = DockStyle.Bottom;
                btn.Size = btnTransferFunction.Size;
                btn.Click -= btnTransferFunctionSelect_Click;
                btn.Click += btnTransferFunctionSelect_Click;
                m_DicPanelTransferFunctionTypeButton.Add(btn.Text, btn);
                panelTransferFunction.Controls.Add(btn);
            }
            panelTransferFunction.AutoSize = true;
        }
        private void btnTransferFunctionSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            btnTransferFunction.Text = GParam.theInst.GetLanguage(btn.Text);
            panelTransferMenu.Visible = false;
            foreach (Button btn1 in panelTransferFunction.Controls)
            {
                if (btn1 == btn)//按下去的按鈕
                {
                    bool bFind = false;
                    foreach (enumTransferMode enumType in Enum.GetValues(typeof(enumTransferMode)))
                    {
                        string strName = GetEnumDescription(enumType);
                        if (GParam.theInst.GetLanguage(strName) == btn.Text)//找到匹配
                        {
                            m_eTransferMode = enumType;
                            bFind = true;
                            break;
                        }
                    }
                    if (bFind == false) return;

                    CreateTransferFunctionTypeButton(m_eTransferMode);
                    CreateAlignFunctionButton(m_eTransferMode);
                    CreateTransferSourceButton(m_eTransferMode);
                    CreateTransferTargetButton(m_eTransferMode);
                    CreateRecipFunctionButton();

                    switch (m_eTransferMode)
                    {
                        case enumTransferMode.Notch:
                            guiNotchAngle1.Visible = true;
                            tabCtrlTransferSub.SelectedTab = tabPageNotchFnc;//切頁籤
                            break;
                        default:
                            guiNotchAngle1.Visible = false;
                            break;
                    }

                    foreach (GUILoadport item in m_guiloadportList)//啟用對應UI選取wafer
                    {
                        if (item.Visible == false) continue;
                        item.ShowSelectColor = (m_eTransferMode == enumTransferMode.Notch
                            || m_eTransferMode == enumTransferMode.Display || m_eTransferMode == enumTransferMode.Random);
                        item.SelectForStocker = false;//Stocker
                        item.SelectWaferBySorterMode = true;//Stocker
                    }
                    btn1.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorButton : Color.SteelBlue;//選擇變顏色
                }
                else//沒選擇按鈕重置
                {
                    btn1.BackColor = SystemColors.ActiveCaption;//沒選擇回原色              
                }
            }
            ClearSelectWafer();//使用者選擇Wafer清除
        }
        //==================================================================================================
        private void btnTransferFunctionType_Click(object sender, EventArgs e)
        {
            //create button       
            CreateTransferFunctionTypeButton(m_eTransferMode);
            //show
            showSubPanel(sender, panelTransferFunctionType);
        }
        private void CreateTransferFunctionTypeButton(enumTransferMode eTransferFnc)
        {
            bool bHardwareHasAlgn = false;
            foreach (I_Aligner aln in ListALN.ToArray()) { bHardwareHasAlgn |= (aln != null && aln.Disable == false); }
            panelTransferFunctionType.Controls.Clear();
            m_DicPanelTransferFunctionTypeButton.Clear();
            //建立按鈕      
            switch (eTransferFnc)
            {
                case enumTransferMode.All:
                    foreach (enumTransferModeType enumType in Enum.GetValues(typeof(enumTransferModeType)))
                    {
                        if (enumType == enumTransferModeType.Match && bHardwareHasAlgn == false && GParam.theInst.IsAllOcrDisable()) continue;//沒有Aligner&OCR
                        if (enumType == enumTransferModeType.Match && GParam.theInst.IsSimulate == false) continue;//暫時不支援
                        Button btn = new Button { Text = GParam.theInst.GetLanguage(GetEnumDescription(enumType)) };
                        if (GParam.theInst.FreeStyle)
                            btn.BackColor = btnTransferFunctionType.Text == btn.Text ? GParam.theInst.ColorButton/*ColorTitle*/ : Color.Transparent/*GParam.theInst.ColorButton*/;
                        else
                            btn.BackColor = btnTransferFunctionType.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;
                        btn.Dock = DockStyle.Bottom;
                        btn.Size = btnTransferFunctionType.Size;
                        btn.Click -= btnTransferFunctionTypeSelect_Click;
                        btn.Click += btnTransferFunctionTypeSelect_Click;
                        panelTransferFunctionType.Controls.Add(btn);
                        m_DicPanelTransferFunctionTypeButton.Add(btn.Text, btn);
                    }
                    btnTransferFunctionType.Enabled = true;
                    break;
                case enumTransferMode.Pack:
                    foreach (enumTransferModeType enumType in Enum.GetValues(typeof(enumTransferModeType)))
                    {
                        if (enumType == enumTransferModeType.SameSlot || enumType == enumTransferModeType.Match) continue;
                        Button btn = new Button { Text = GParam.theInst.GetLanguage(GetEnumDescription(enumType)) };
                        if (GParam.theInst.FreeStyle)
                            btn.BackColor = btnTransferFunctionType.Text == btn.Text ? GParam.theInst.ColorButton/*ColorTitle*/ : Color.Transparent/*GParam.theInst.ColorButton*/;
                        else
                            btn.BackColor = btnTransferFunctionType.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;
                        btn.Dock = DockStyle.Bottom;
                        btn.Size = btnTransferFunctionType.Size;
                        btn.Click -= btnTransferFunctionTypeSelect_Click;
                        btn.Click += btnTransferFunctionTypeSelect_Click;
                        panelTransferFunctionType.Controls.Add(btn);
                    }
                    btnTransferFunctionType.Enabled = true;
                    break;
                default:
                    btnTransferFunctionType.Enabled = false;
                    break;
            }
            panelTransferFunctionType.AutoSize = true;
            //初始化，針對All / Pack / Stock對應選擇FunctionType
            btnTransferFunctionType.Text = GParam.theInst.GetLanguage("FunctionType");
            btnTransferFunctionType.BackColor = Color.Transparent;


        }
        private void btnTransferFunctionTypeSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            btnTransferFunctionType.Text = GParam.theInst.GetLanguage(btn.Text);
            panelTransferMenu.Visible = false;
            foreach (enumTransferModeType enumType in Enum.GetValues(typeof(enumTransferModeType)))
            {
                if (btn.Text == GParam.theInst.GetLanguage(GetEnumDescription(enumType)))
                {
                    m_eTransferModeType = enumType;
                    AnalyzeModeToCombineJob();
                    break;
                }
            }
            btnTransferFunctionType.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorButton : Color.Transparent;
        }
        //==================================================================================================
        private void btnAlignFunction_Click_1(object sender, EventArgs e)
        {
            //create button            
            CreateAlignFunctionButton(m_eTransferMode);
            //show
            showSubPanel(sender, panelAlignFunction);
        }
        private void CreateAlignFunctionButton(enumTransferMode eTransferFnc)
        {
            panelAlignFunction.Controls.Clear();
            m_DicPanelTransferFunctionTypeButton.Clear();
            bool bHardwareHasAlgn = false;
            foreach (I_Aligner aln in ListALN.ToArray()) { bHardwareHasAlgn |= (aln != null && aln.Disable == false); }

            string[] strArray;
            if (bHardwareHasAlgn == false)
            {
                strArray = new string[] { "No Aligner" };
            }
            else if (eTransferFnc == enumTransferMode.Notch || eTransferFnc == enumTransferMode.Display)
            {
                strArray = new string[] { "Aligner" };
            }
            else
            {
                strArray = new string[] { /*"No Aligner",*/ "Aligner" };
            }
            //建立按鈕          
            foreach (string item in strArray)
            {
                Button btn = new Button();
                btn.Text = GParam.theInst.GetLanguage(item);
                if (GParam.theInst.FreeStyle)
                    btn.BackColor = btnAlignFunction.Text == btn.Text ? GParam.theInst.ColorButton/*ColorTitle*/ : Color.Transparent/*GParam.theInst.ColorButton*/;
                else
                    btn.BackColor = btnAlignFunction.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;
                btn.Dock = DockStyle.Bottom;
                btn.Size = btnAlignFunction.Size;
                btn.Click -= btnAlignFunctionSelect_Click;
                btn.Click += btnAlignFunctionSelect_Click;
                panelAlignFunction.Controls.Add(btn);
                m_DicPanelTransferFunctionTypeButton.Add(btn.Text, btn);
            }
            panelAlignFunction.AutoSize = true;
            //設定初始值
            btnAlignFunction.Text = panelAlignFunction.Controls[0].Text;
            m_bNoAign = (btnAlignFunction.Text == GParam.theInst.GetLanguage("No Aligner"));
        }
        private void btnAlignFunctionSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            btnAlignFunction.Text = GParam.theInst.GetLanguage(btn.Text);
            panelTransferMenu.Visible = false;
            m_bNoAign = (btn.Text == GParam.theInst.GetLanguage("No Aligner"));
        }
        //==================================================================================================
        private void btnRecipeFunction_Click_1(object sender, EventArgs e)
        {
            //create button          
            CreateRecipFunctionButton();
            //show
            showSubPanel(sender, panelRecipeFunction);
        }

        private void CreateRecipFunctionButton()
        {
            panelRecipeFunction.Controls.Clear();
            m_DicPanelTransferFunctionTypeButton.Clear();
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
                if (GParam.theInst.FreeStyle)
                    btn.BackColor = btnRecipeFunction.Text == btn.Text ? GParam.theInst.ColorButton/*ColorTitle*/ : Color.Transparent/*GParam.theInst.ColorButton*/;
                else
                    btn.BackColor = btnRecipeFunction.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;
                btn.Dock = DockStyle.Bottom;
                btn.Size = btnRecipeFunction.Size;
                btn.Click -= btnRecipeFunctionSelect_Click;
                btn.Click += btnRecipeFunctionSelect_Click;
                panelRecipeFunction.Controls.Add(btn);
                m_DicPanelTransferFunctionTypeButton.Add(btn.Text, btn);
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
        //==================================================================================================
        private void btnTransferSource_Click(object sender, EventArgs e)
        {
            //create button
            CreateTransferSourceButton(m_eTransferMode);
            //show
            showSubPanel(sender, panelTransferSource);
        }
        private void CreateTransferSourceButton(enumTransferMode eTransferFnc)
        {
            panelTransferSource.Controls.Clear();
            m_DicPanelTransferFunctionTypeButton.Clear();
            if (ListSTG.Count < 2 || eTransferFnc != enumTransferMode.All)
            {
                btnTransferSource.Enabled = false;
                btnTransferSource.BackColor = Color.Transparent;
                return;
            }
            btnTransferSource.Enabled = true;
            btnTransferSource.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorButton : Color.Transparent;
            //建立按鈕
            foreach (I_Loadport stg in ListSTG.ToArray())
            {
                if (stg.Disable) continue;
                Button btn = new Button();
                btn.Text = Encoding.ASCII.GetString(new Byte[] { (byte)(65 + stg.BodyNo - 1) });
                if (GParam.theInst.FreeStyle)
                    btn.BackColor = btnTransferSource.Text == btn.Text ? GParam.theInst.ColorButton/*ColorTitle*/ : Color.Transparent/*GParam.theInst.ColorButton*/;
                else
                    btn.BackColor = btnTransferSource.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;
                btn.Dock = DockStyle.Bottom;
                btn.Size = btnTransferSource.Size;
                btn.Click -= btnTransferSourceSelect_Click;
                btn.Click += btnTransferSourceSelect_Click;
                panelTransferSource.Controls.Add(btn);
                m_DicPanelTransferFunctionTypeButton.Add(btn.Text, btn);
            }
            panelTransferSource.AutoSize = true;
            //設定初始值
            foreach (Button item in panelTransferSource.Controls)
            {
                if (btnTransferTarget.Text != item.Text)
                {
                    btnTransferSource.Text = item.Text;
                    break;
                }
            }
        }
        private void btnTransferSourceSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn.Text == btnTransferTarget.Text)//選擇Source與Target相同
            {
                foreach (Button item in panelTransferTarget.Controls)
                {
                    if (btn.Text != item.Text)
                    {
                        btnTransferTarget.Text = item.Text;
                        break;
                    }
                }
            }
            btnTransferSource.Text = btn.Text;
            panelTransferMenu.Visible = false;
            AnalyzeModeToCombineJob();
        }
        //==================================================================================================
        private void btnTransferTarget_Click(object sender, EventArgs e)
        {
            //create button
            CreateTransferTargetButton(m_eTransferMode);
            //show
            showSubPanel(sender, panelTransferTarget);
        }
        private void CreateTransferTargetButton(enumTransferMode eTransferFnc)
        {
            panelTransferTarget.Controls.Clear();
            m_DicPanelTransferFunctionTypeButton.Clear();
            if (ListSTG.Count < 2 || eTransferFnc != enumTransferMode.All)
            {
                btnTransferTarget.Enabled = false;
                btnTransferTarget.BackColor = Color.Transparent;
                return;
            }
            btnTransferTarget.Enabled = true;
            btnTransferTarget.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorButton : Color.Transparent;
            //建立按鈕
            foreach (I_Loadport stg in ListSTG.ToArray())
            {
                if (stg.Disable) continue;
                Button btn = new Button();
                btn.Text = Encoding.ASCII.GetString(new Byte[] { (byte)(65 + stg.BodyNo - 1) });
                if (GParam.theInst.FreeStyle)
                    btn.BackColor = btnTransferTarget.Text == btn.Text ? GParam.theInst.ColorButton/*ColorTitle*/ : Color.Transparent/*GParam.theInst.ColorButton*/;
                else
                    btn.BackColor = btnTransferTarget.Text == btn.Text ? Color.SteelBlue : SystemColors.ActiveCaption;
                btn.Dock = DockStyle.Bottom;
                btn.Size = btnTransferSource.Size;
                btn.Click -= btnTransferTargetSelect_Click;
                btn.Click += btnTransferTargetSelect_Click;
                panelTransferTarget.Controls.Add(btn);
                m_DicPanelTransferFunctionTypeButton.Add(btn.Text, btn);
            }
            panelTransferTarget.AutoSize = true;
            //設定初始值
            foreach (Button item in panelTransferSource.Controls)
            {
                if (btnTransferSource.Text != item.Text)
                {
                    btnTransferTarget.Text = item.Text;
                    break;
                }
            }
        }
        private void btnTransferTargetSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn.Text == btnTransferSource.Text)//選擇Source與Target相同
            {
                foreach (Button item in panelTransferSource.Controls)
                {
                    if (btn.Text != item.Text)
                    {
                        btnTransferSource.Text = item.Text;
                        break;
                    }
                }
            }
            btnTransferTarget.Text = btn.Text;
            panelTransferMenu.Visible = false;
            AnalyzeModeToCombineJob();
        }
        //==================================================================================================
        private void showSubPanel(object sender, Panel subMenu)
        {
            Button btn = sender as Button;
            int nCount = subMenu.Controls.Count;

            panelTransferMenu.Location = new Point
                (
                 tlpTransferMenu.Location.X + btn.Location.X,
                 tlpTransferMenu.Location.Y + btn.Location.Y
                );
            panelTransferMenu.Width = btn.Width;
            panelTransferMenu.Height = btn.Height * nCount;
            panelTransferMenu.Visible = true;

            //---------------------------------------
            foreach (Panel item in panelTransferMenu.Controls)
            {
                item.Visible = (subMenu == item);
            }
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
        #endregion


        public void DoFirstCycleStart(object sender, EventArgs e)
        {

        }
        public void DoFirstCycleSetting1(int sourceBodyNo, int targetBodyNo)//注意補上TOWER的CYCLE
        {
            Action act = () =>
            {
                #region =========== click Button =============
                //選擇ALL或Display
                btnTransferFunction_Click(btnTransferFunction, null);
                foreach (Button btn in panelTransferFunction.Controls)
                {
                    if (sourceBodyNo == targetBodyNo)
                    {
                        if (btn.Text == GParam.theInst.GetLanguage(GetEnumDescription(enumTransferMode.Display)))
                            btnTransferFunctionSelect_Click(btn, null);
                    }
                    else
                    {
                        if (btn.Text == GParam.theInst.GetLanguage(GetEnumDescription(enumTransferMode.All)))
                            btnTransferFunctionSelect_Click(btn, null);
                    }
                }

                //選擇Aligner
                btnAlignFunction_Click_1(btnAlignFunction, null);
                foreach (Button btn in panelAlignFunction.Controls)
                {
                    if (m_autoProcess.IsCycleDoAlign)
                    {
                        if (btn.Text == GParam.theInst.GetLanguage("Aligner")) btnAlignFunctionSelect_Click(btn, null);
                    }
                    else
                    {
                        if (btn.Text == GParam.theInst.GetLanguage("No Aligner")) btnAlignFunctionSelect_Click(btn, null);
                    }
                }

                //GroupRecipe
                btnRecipeFunction_Click_1(btnRecipeFunction, null);
                foreach (Button btn in panelRecipeFunction.Controls)
                {
                    if (btn.Text == m_autoProcess.CycleRcpName)
                    {
                        btnRecipeFunctionSelect_Click(btn, null);
                    }
                }

                switch (m_eTransferMode)
                {
                    case enumTransferMode.Display:
                        {
                            string str = ListSTG[sourceBodyNo - 1].MappingData;
                            List<enumUIPickWaferStat> listSelectSlotSts = new List<enumUIPickWaferStat>();
                            List<int> listSelectSlot = new List<int>();
                            for (int i = 0; i < str.Length; i++)
                            {
                                if (str[i] == '1')
                                {
                                    listSelectSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                    listSelectSlot.Add(i);
                                }
                            }
                            GuiLoadport_UseSelectWafer(m_guiloadportList[sourceBodyNo - 1], new GUILoadport.EventArgs_SelectWafer(listSelectSlotSts, listSelectSlot));
                        }
                        break;
                    case enumTransferMode.Notch:
                        break;
                    case enumTransferMode.Random:
                        break;
                    case enumTransferMode.All:
                        {
                            //選擇SameSlot    
                            btnTransferFunctionType_Click(btnTransferFunctionType, null);
                            foreach (Button btn in panelTransferFunctionType.Controls)
                            {
                                if (btn.Text == GetEnumDescription(enumTransferModeType.SameSlot))
                                {
                                    btnTransferFunctionTypeSelect_Click(btn, null);
                                }
                            }

                            //source
                            btnTransferSource_Click(btnTransferSource, null);
                            foreach (Button btn in panelTransferSource.Controls)
                            {
                                if (btn.Text == Encoding.ASCII.GetString(new Byte[] { (byte)(65 + sourceBodyNo - 1) }))
                                {
                                    btnTransferSourceSelect_Click(btn, null);
                                }
                            }

                            //target
                            btnTransferTarget_Click(btnTransferTarget, null);
                            foreach (Button btn in panelTransferTarget.Controls)
                            {
                                if (btn.Text == Encoding.ASCII.GetString(new Byte[] { (byte)(65 + targetBodyNo - 1) }))
                                {
                                    btnTransferTargetSelect_Click(btn, null);
                                }
                            }
                        }
                        break;
                    case enumTransferMode.Pack:
                        break;
                    default:
                        break;
                }

                #endregion

                btnStart.PerformClick();
            };
            BeginInvoke(act);
        }

        #region  ================= Set and Save User Select Record Function =================

        private void btnSelectTransferRecipe_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                if (m_autoProcess.IsCycle)
                {
                    new frmMessageBox("Is in cycle run. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                if (m_QueWaferJob.Count() > 0)
                    return;

                WriteLog("btnSelectTransferRecipe_Click");

                ReloadUserSelectRecordFile();
                string str = Path.Combine(m_strUserSelectRecordFilePath, cmbUserSelectRecord.SelectedItem.ToString());
                List<string> lines = new List<string>();

                using (StreamReader reader = new StreamReader(str))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }

                if (lines.Count() < 1)
                    return;

                string[] transferFunction = lines[0].Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (transferFunction.Count() != 1)
                {
                    new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                enumTransferMode mode = enumTransferMode.All;
                if (Enum.TryParse(transferFunction[0], out mode) == false)
                    return;

                Action act = () =>
                {
                    CreateTransferFunctionButton();
                    showSubPanel(btnTransferFunction, panelTransferFunction);
                    Button temp = m_DicPanelTransferFunctionTypeButton[mode.ToString()];
                    btnTransferFunctionSelect_Click(temp, null);
                };
                Invoke(act);

                if (mode == enumTransferMode.All || mode == enumTransferMode.Pack)
                {
                    string[] transferFunctionType = lines[1].Split(new char[2] { '-', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (transferFunctionType.Count() != 3)
                    {
                        new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    enumTransferModeType type = enumTransferModeType.SameSlot;

                    //第一個要判斷是否為enumTransferModeType，後面如果是要馬ABCD...要馬None
                    if (Enum.TryParse(transferFunctionType[0], out type) &&
                        transferFunctionType[1].Length == 1 &&
                        transferFunctionType[2].Length == 1)
                    {
                        Action act2 = () =>
                        {
                            CreateTransferFunctionTypeButton(mode);
                            showSubPanel(btnTransferFunctionType, panelTransferFunction);
                            Button temp = m_DicPanelTransferFunctionTypeButton[type.ToString()];
                            btnTransferFunctionTypeSelect_Click(temp, null);

                            CreateTransferSourceButton(enumTransferMode.All);
                            showSubPanel(btnTransferSource, panelTransferFunction);
                            temp = m_DicPanelTransferFunctionTypeButton[transferFunctionType[1]];
                            btnTransferSourceSelect_Click(temp, null);

                            CreateTransferTargetButton(enumTransferMode.All);
                            showSubPanel(btnTransferTarget, panelTransferFunction);
                            temp = m_DicPanelTransferFunctionTypeButton[transferFunctionType[2]];
                            btnTransferTargetSelect_Click(temp, null);


                        };
                        Invoke(act2);
                    }
                    else
                    {
                        return;
                    }
                }
                //要補一個控制guiNotchAngle1的事件?
                string[] transferAligner = lines[2].Split(new char[3] { ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (transferAligner.Count() != 2)
                {
                    new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                Action act3 = () =>
                {
                    CreateAlignFunctionButton(mode);
                    showSubPanel(btnAlignFunction, panelTransferFunction);
                    Button temp = m_DicPanelTransferFunctionTypeButton[transferAligner[0]];
                    btnAlignFunctionSelect_Click(temp, null);

                    int angle = -1;
                    Int32.TryParse(transferAligner[1], out angle);

                    guiNotchAngle1.EventTriggerBtn(angle);
                };
                Invoke(act3);

                for (int i = 3; i < lines.Count; i++)
                {
                    string[] transferInfo = lines[i].Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (transferInfo.Count() != 2)
                    {
                        new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    string[] sourceInfo = transferInfo[0].Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] targetInfo = transferInfo[1].Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sourceInfo.Count() != 2 && targetInfo.Count() != 2)
                    {
                        new frmMessageBox("Recipe infomation has error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }

                    enumLoadport sourceLP /*= enumLoadport.Total*/;
                    enumLoadport targetLP /*= enumLoadport.Total*/;
                    if (Enum.TryParse(sourceInfo[0], out sourceLP) == false || Enum.TryParse(targetInfo[0], out targetLP) == false)
                    {
                        new frmMessageBox("Recipe infomation has error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    int sourceSlot = -1;
                    int targetSlot = -1;

                    if (Int32.TryParse(sourceInfo[1], out sourceSlot) == false || Int32.TryParse(targetInfo[1], out targetSlot) == false)
                    {
                        new frmMessageBox("Recipe infomation has error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }


                    if (mode == enumTransferMode.All || mode == enumTransferMode.Random)
                    {
                        if ((m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].IsWaferOn && m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].WaferSts == enumUIPickWaferStat.HasWafer) ||
                        (m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].IsWaferOn == false && m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].WaferSts == enumUIPickWaferStat.PutWafer))
                        {
                            if ((m_guiloadportList[(int)targetLP].LstSlot[targetSlot - 1].IsWaferOn == false && m_guiloadportList[(int)targetLP].LstSlot[targetSlot - 1].WaferSts == enumUIPickWaferStat.NoWafer) ||
                                (m_guiloadportList[(int)targetLP].LstSlot[targetSlot - 1].IsWaferOn == true && m_guiloadportList[(int)targetLP].LstSlot[targetSlot - 1].WaferSts == enumUIPickWaferStat.ExeHasWafer))
                            {
                                List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                List<int> listSelectSourceSlot = new List<int>();
                                List<int> listSelectTargetSlot = new List<int>();
                                listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                listSelectSourceSlot.Add(sourceSlot - 1);
                                listSelectTargetSlot.Add(targetSlot - 1);
                                GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                            }
                        }
                    }
                    else if (mode == enumTransferMode.Display || mode == enumTransferMode.Notch || mode == enumTransferMode.Pack)
                    {
                        if (m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].IsWaferOn && m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].WaferSts == enumUIPickWaferStat.HasWafer)
                        {
                            List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                            List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                            List<int> listSelectSourceSlot = new List<int>();
                            List<int> listSelectTargetSlot = new List<int>();
                            listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                            listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                            listSelectSourceSlot.Add(sourceSlot - 1);
                            listSelectTargetSlot.Add(targetSlot - 1);
                            GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                            GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void btnSaveTransferRecipe_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                if (m_autoProcess.IsCycle)
                {
                    new frmMessageBox("Is in cycle run. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                if (m_QueWaferJob.Count() < 1)
                    return;

                string transferFunction = btnTransferFunction.Text.ToString();

                string transferFunctionType = btnTransferFunctionType.Enabled ? btnTransferFunctionType.Text.ToString() : "None";
                string transferSourceLP = btnTransferSource.Enabled ? btnTransferSource.Text.ToString() : "None";
                string transferTargetLP = btnTransferTarget.Enabled ? btnTransferTarget.Text.ToString() : "None";

                string alignerFunction = btnAlignFunction.Text.ToString();
                enumTransferMode mode = enumTransferMode.All;
                if (Enum.TryParse(transferFunction, out mode) == false)
                    return;

                List<clsSelectWaferInfo> waferSelectList = m_QueWaferJob.ToArray().ToList();
                List<string> writeToRcpList = new List<string>();

                writeToRcpList.Add(transferFunction + ";");
                writeToRcpList.Add(transferFunctionType + "-" + transferSourceLP + "-" + transferTargetLP + ";");
                writeToRcpList.Add(string.Format("{0}-{1};", alignerFunction, m_dNotchAngle));

                foreach (var item in waferSelectList)
                {
                    string temp = string.Format("STG{0}-{1:00},STG{2}-{3:00};", item.SourceLpBodyNo, item.SourceSlotIdx + 1, item.TargetLpBodyNo, item.TargetSlotIdx + 1);
                    writeToRcpList.Add(temp);
                }
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Select Record(*.rcp)|*.rcp";
                    dialog.InitialDirectory = Path.Combine(Application.StartupPath, "UserSelectRecord");
                    dialog.RestoreDirectory = true;
                    dialog.Title = "Save Select Record File";
                    dialog.FileName = "Default.rcp"; // 預設檔案名稱

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = dialog.FileName;
                        using (StreamWriter writer = new StreamWriter(filePath))
                        {
                            foreach (string line in writeToRcpList)
                            {
                                writer.WriteLine(line);
                            }
                        }
                    }
                }

                ReloadUserSelectRecordFile();
                cmbUserSelectRecord.Items.Clear();
                foreach (var item in m_listUserSelectRecordName.ToArray())
                {
                    cmbUserSelectRecord.Items.Add(item);
                }

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void ReloadUserSelectRecordFile()
        {
            m_listUserSelectRecordName.Clear();
            string[] temp = Directory.GetFiles(m_strUserSelectRecordFilePath, "*.rcp");
            foreach (var item in temp)
            {
                string fileName = Path.GetFileName(item);
                m_listUserSelectRecordName.Add(fileName);
            }
        }

        private void cmbUserSelectRecord_SelectionChangeCommitted(object sender, EventArgs e)
        {
            try
            {
                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                if (m_autoProcess.IsCycle)
                {
                    new frmMessageBox("Is in cycle run. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                //if (m_QueWaferJob.Count() > 0)
                //    return;

                WriteLog("btnSelectTransferRecipe_Click");
                ClearSelectWafer();

                #region 讀文件內容
                ReloadUserSelectRecordFile();
                string str = Path.Combine(m_strUserSelectRecordFilePath, cmbUserSelectRecord.SelectedItem.ToString());
                List<string> lines = new List<string>();
                using (StreamReader reader = new StreamReader(str))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
                if (lines.Count() < 1)
                    return;
                #endregion

                #region 解析第四行開始 loadport 檢查狀態
                for (int i = 3; i < lines.Count; i++)
                {
                    string[] transferInfo = lines[i].Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (transferInfo.Count() != 2)
                    {
                        new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    string[] sourceInfo = transferInfo[0].Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] targetInfo = transferInfo[1].Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sourceInfo.Count() != 2 || targetInfo.Count() != 2)
                    {
                        new frmMessageBox("Recipe infomation has error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }

                    enumLoadport sourceLP, targetLP;
                    if (Enum.TryParse(sourceInfo[0], out sourceLP) == false || Enum.TryParse(targetInfo[0], out targetLP) == false)
                    {
                        new frmMessageBox("Recipe infomation has error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }

                    if (ListSTG[(int)sourceLP].StatusMachine != enumStateMachine.PS_Docked ||
                        ListSTG[(int)targetLP].StatusMachine != enumStateMachine.PS_Docked)
                    {
                        new frmMessageBox("Loadport is not docked", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                }
                #endregion

                #region 解析第一行 傳送模式 並觸發選擇按鈕
                string[] transferFunction = lines[0].Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (transferFunction.Count() != 1)
                {
                    new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                enumTransferMode mode = enumTransferMode.All;
                if (Enum.TryParse(transferFunction[0], out mode) == false)
                    return;

                //觸發模式按鈕
                Action act = () =>
                {
                    CreateTransferFunctionButton();
                    showSubPanel(btnTransferFunction, panelTransferFunction);
                    Button temp = m_DicPanelTransferFunctionTypeButton[mode.ToString()];
                    btnTransferFunctionSelect_Click(temp, null);
                };
                Invoke(act);
                #endregion

                #region 解析第二行 傳送模式 子模式 並觸發選擇按鈕
                if (mode == enumTransferMode.All || mode == enumTransferMode.Pack)
                {
                    string[] transferFunctionType = lines[1].Split(new char[2] { '-', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (transferFunctionType.Count() != 3)
                    {
                        new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    enumTransferModeType type = enumTransferModeType.SameSlot;

                    //第一個要判斷是否為enumTransferModeType，後面如果是要馬ABCD...要馬None
                    if (Enum.TryParse(transferFunctionType[0], out type) &&
                        transferFunctionType[1].Length == 1 &&
                        transferFunctionType[2].Length == 1)
                    {
                        Action act2 = () =>
                        {
                            CreateTransferFunctionTypeButton(mode);
                            showSubPanel(btnTransferFunctionType, panelTransferFunction);
                            Button temp = m_DicPanelTransferFunctionTypeButton[type.ToString()];
                            btnTransferFunctionTypeSelect_Click(temp, null);

                            CreateTransferSourceButton(enumTransferMode.All);
                            showSubPanel(btnTransferSource, panelTransferFunction);
                            temp = m_DicPanelTransferFunctionTypeButton[transferFunctionType[1]];
                            btnTransferSourceSelect_Click(temp, null);

                            CreateTransferTargetButton(enumTransferMode.All);
                            showSubPanel(btnTransferTarget, panelTransferFunction);
                            temp = m_DicPanelTransferFunctionTypeButton[transferFunctionType[2]];
                            btnTransferTargetSelect_Click(temp, null);
                        };
                        Invoke(act2);
                    }
                    else
                    {
                        return;
                    }
                }
                #endregion

                #region 解析第三行 是否啟用Aligner 並觸發按鈕
                //要補一個控制guiNotchAngle1的事件?
                string[] transferAligner = lines[2].Split(new char[3] { ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (transferAligner.Count() != 2)
                {
                    new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                Action act3 = () =>
                {
                    CreateAlignFunctionButton(mode);
                    showSubPanel(btnAlignFunction, panelTransferFunction);
                    Button temp = m_DicPanelTransferFunctionTypeButton[transferAligner[0]];
                    btnAlignFunctionSelect_Click(temp, null);

                    int angle = -1;
                    Int32.TryParse(transferAligner[1], out angle);

                    guiNotchAngle1.EventTriggerBtn(angle);
                };
                Invoke(act3);
                #endregion



                for (int i = 3; i < lines.Count; i++)
                {
                    string[] transferInfo = lines[i].Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (transferInfo.Count() != 2)
                    {
                        new frmMessageBox("Recipe Info Has Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    string[] sourceInfo = transferInfo[0].Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] targetInfo = transferInfo[1].Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sourceInfo.Count() != 2 || targetInfo.Count() != 2)
                    {
                        new frmMessageBox("Recipe infomation has error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }

                    enumLoadport sourceLP /*= enumLoadport.Total*/;
                    enumLoadport targetLP /*= enumLoadport.Total*/;
                    if (Enum.TryParse(sourceInfo[0], out sourceLP) == false || Enum.TryParse(targetInfo[0], out targetLP) == false)
                    {
                        new frmMessageBox("Recipe infomation has error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                    int sourceSlot = -1;
                    int targetSlot = -1;

                    if (Int32.TryParse(sourceInfo[1], out sourceSlot) == false || Int32.TryParse(targetInfo[1], out targetSlot) == false)
                    {
                        new frmMessageBox("Recipe infomation has error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }


                    if (mode == enumTransferMode.All || mode == enumTransferMode.Random)
                    {
                        if ((m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].IsWaferOn && m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].WaferSts == enumUIPickWaferStat.HasWafer) ||
                        (m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].IsWaferOn == false && m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].WaferSts == enumUIPickWaferStat.PutWafer))
                        {
                            if ((m_guiloadportList[(int)targetLP].LstSlot[targetSlot - 1].IsWaferOn == false && m_guiloadportList[(int)targetLP].LstSlot[targetSlot - 1].WaferSts == enumUIPickWaferStat.NoWafer) ||
                                (m_guiloadportList[(int)targetLP].LstSlot[targetSlot - 1].IsWaferOn == true && m_guiloadportList[(int)targetLP].LstSlot[targetSlot - 1].WaferSts == enumUIPickWaferStat.ExeHasWafer))
                            {
                                List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                                List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                                List<int> listSelectSourceSlot = new List<int>();
                                List<int> listSelectTargetSlot = new List<int>();
                                listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                                listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                                listSelectSourceSlot.Add(sourceSlot - 1);
                                listSelectTargetSlot.Add(targetSlot - 1);
                                GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                                GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                            }
                        }
                    }
                    else if (mode == enumTransferMode.Display || mode == enumTransferMode.Notch || mode == enumTransferMode.Pack)
                    {
                        if (m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].IsWaferOn && m_guiloadportList[(int)sourceLP].LstSlot[sourceSlot - 1].WaferSts == enumUIPickWaferStat.HasWafer)
                        {
                            List<enumUIPickWaferStat> listSelectSourceSlotSts = new List<enumUIPickWaferStat>();
                            List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                            List<int> listSelectSourceSlot = new List<int>();
                            List<int> listSelectTargetSlot = new List<int>();
                            listSelectSourceSlotSts.Add(enumUIPickWaferStat.HasWafer);
                            listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                            listSelectSourceSlot.Add(sourceSlot - 1);
                            listSelectTargetSlot.Add(targetSlot - 1);
                            GuiLoadport_UseSelectWafer(m_guiloadportList[(int)sourceLP], new GUILoadport.EventArgs_SelectWafer(listSelectSourceSlotSts, listSelectSourceSlot));
                            GuiLoadport_UseSelectWafer(m_guiloadportList[(int)targetLP], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }


        #endregion



        /// <summary>
        /// 隱藏選片功能，可以看Unit狀態
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTransferShow_Click(object sender, EventArgs e)
        {
            if (_userManager.IsLogin == false && m_autoProcess.IsCycle == false && GParam.theInst.IsSimulate == false)
            {
                new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }

            if (m_autoProcess.IsCycle)
            {
                if (tabCtrlTransferFnc.SelectedTab == tabPageCycle || tabCtrlTransferFnc.SelectedTab == tabPageTransferFnc)
                {
                    //btnTransferShow.Dock = DockStyle.Bottom;
                    btnTransferShow.Text = GParam.theInst.GetLanguage("↓Tool");
                    tabCtrlTransferFnc.SelectedTab = tabPageUnit;
                }
                else if (tabCtrlTransferFnc.SelectedTab == tabPageUnit)
                {
                    //btnTransferShow.Dock = DockStyle.Top;
                    btnTransferShow.Text = GParam.theInst.GetLanguage("↑Tool");
                    tabCtrlTransferFnc.SelectedTab = tabPageCycle;
                }
            }
            else
            {
                if (tabCtrlTransferFnc.SelectedTab == tabPageTransferFnc || tabCtrlTransferFnc.SelectedTab == tabPageCycle)
                {
                    //btnTransferShow.Dock = DockStyle.Bottom;
                    btnTransferShow.Text = GParam.theInst.GetLanguage("↓Tool");
                    tabCtrlTransferFnc.SelectedTab = tabPageUnit;
                }
                else if (tabCtrlTransferFnc.SelectedTab == tabPageUnit)
                {
                    //btnTransferShow.Dock = DockStyle.Top;
                    btnTransferShow.Text = GParam.theInst.GetLanguage("↑Tool");
                    tabCtrlTransferFnc.SelectedTab = tabPageTransferFnc;
                }
            }


        }


        void RunUpdatePJDataViwe()
        {
            ShowDataGridView(DGVPJlist, m_Gem.PJListForDataSet());
        }
        void RunUpdateCJDataViwe()
        {
            ShowDataGridView(DGVCJlist, m_Gem.CJListForDataSet());
        }
        private void M_Gem_OnControlJobUpdate(object sender, EventArgs e)
        {
            if (!this.Visible || !DGVCJlist.Visible) return;
            _exeUpdateCJDataViwe.Set();
        }
        private void M_Gem_OnProcessJobUpdate(object sender, EventArgs e)
        {
            if (!this.Visible || !DGVPJlist.Visible) return;
            _exeUpdatePJDataViwe.Set();
        }
        private void DGVPJlist_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
                UpdatePJdataList();
        }
        private void DGVCJlist_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
                UpdateCJdataList();
        }
        void UpdatePJdataList()
        {
            DGVPJlist.DataSource = m_Gem.PJListForDataSet().Tables[0];
        }
        void UpdateCJdataList()
        {
            DGVCJlist.DataSource = m_Gem.CJListForDataSet().Tables[0];
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            //-----------------------------------------------------
            if (GParam.theInst.IsUnitDisable(enumUnit.BUF1) == false)
                guiBUF1_Status.SetHardwareSlot = GParam.theInst.GetBufEnableSlot(0);

            //-----------------------------------------------------
            if (GParam.theInst.IsUnitDisable(enumUnit.BUF2) == false)
                guiBUF2_Status.SetHardwareSlot = GParam.theInst.GetBufEnableSlot(1);

            //-----------------------------------------------------
        }
    }
}
