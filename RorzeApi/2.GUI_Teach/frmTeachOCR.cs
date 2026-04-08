using Rorze.Equipment.Unit;
using RorzeApi.Class;
using RorzeApi.GUI;
using RorzeComm;
using RorzeComm.Log;
using RorzeUnit.Class;
using RorzeUnit.Class.Aligner;
using RorzeUnit.Class.E84.Event;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Robot;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using static RorzeUnit.Class.SWafer;

namespace RorzeApi
{
    public partial class frmTeachOCR : Form
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
        private enumPosition ePos = enumPosition.UnKnow;
        private enumPosition ePosAln = enumPosition.UnKnow;

        //private int m_SelectPort = -1;
        private int m_nSelectSlot = -1;

        private float PoseW;

        private SProcessDB _accessDBlog;
        private Class.SPermission _userManager;   //  管理LOGIN使用者權限
        private string _strUserName;//登入者名稱
        private bool m_bIsRunMode = false;

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");


        private List<I_Robot> m_listTRB;
        private I_Robot m_robotSelect;

        private List<I_Loadport> m_listSTG;

        private List<I_Aligner> m_listALN;
        private I_Aligner m_aligner;

        private List<I_OCR> m_listOCR;

        private I_OCR m_SelectOCR;

        private List<I_E84> m_listE84;

        private SGroupRecipeManager m_dbGrouprecipe;
        private List<GUILoadport> m_guiloadportList = new List<GUILoadport>();

        private List<Button> m_btnSelectAlignerList = new List<Button>();
        private List<Button> m_btnSelectOCRList = new List<Button>();

        public frmTeachOCR(List<I_Robot> robotList, List<I_Loadport> loadportList, List<I_Aligner> alignerList, List<I_OCR> ocrList, List<I_E84> _E84List,
            SProcessDB db, Class.SPermission userManager, bool bIsRunMode)
        {
            InitializeComponent();

            this.Size = new Size(970/*840*/, 718/*700*/);

            //  消失頁籤
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);
            m_listTRB = robotList;
            m_listSTG = loadportList;
            m_listALN = alignerList;
            m_listOCR = ocrList;
            m_listE84 = _E84List;
            m_bIsRunMode = bIsRunMode;
            m_bSimulate = GParam.theInst.IsSimulate;
            _accessDBlog = db;
            _userManager = userManager;

            #region Select Aligner Button

            tlpSelectAligner.RowStyles.Clear();
            tlpSelectAligner.ColumnStyles.Clear();
            tlpSelectAligner.Dock = DockStyle.Fill;

            foreach (I_Aligner item in m_listALN)
            {
                if (item == null || item.Disable) continue;
                if (m_listOCR[2 * (item.BodyNo - 1)].Disable && m_listOCR[2 * (item.BodyNo - 1) + 1].Disable) continue;

                tlpSelectAligner.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

                Button btn = new Button();
                btn.Font = new Font("微軟正黑體", 18, FontStyle.Bold);
                btn.Text = GParam.theInst.GetLanguage("Aligner" + (char)(64 + item.BodyNo));
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleCenter;

                btn.Click += btnSelectAligner_Click;

                m_btnSelectAlignerList.Add(btn);
                tlpSelectAligner.Controls.Add(btn, 0, m_btnSelectAlignerList.Count - 1);//注意
            }
            tlpSelectAligner.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));

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



            for (int i = 0; i < m_listSTG.Count; i++)
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
                m_guiloadportList[i].Visible = !m_listSTG[i].Disable;
                m_guiloadportList[i].Enabled = !m_listSTG[i].Disable;

                if (m_listSTG[i] == null || m_listSTG[i].Disable)
                {
                    continue;//不需要註冊
                }

                m_listSTG[i].OnFoupExistChenge += OnLoadport_FoupExistChenge;          //  更新UI  
                m_listSTG[i].OnClmpComplete += OnLoadport_MappingComplete;             //  更新UI
                m_listSTG[i].OnMappingComplete += OnLoadport_MappingComplete;          //  更新UI
                m_listSTG[i].OnStatusMachineChange += OnLoadport_StatusMachineChange;  //  更新UI
                m_listSTG[i].OnFoupIDChange += OnLoadport_FoupIDChange;                //  更新UI
                m_listSTG[i].OnFoupTypeChange += OnLoadport_FoupTypeChange;            //  更新UI

                //m_listSTG[i].OnUclmComplete += OnLoadport_OnUclmComplete;              //  更新UI
                m_listE84[i].OnAceessModeChange += OnLoadport_E84ModeChange;                //  更新UI
                m_listSTG[i].OnTakeWaferInFoup += m_guiloadportList[i].TakeWaferInFoup;//wafer被塞進來
                m_listSTG[i].OnTakeWaferOutFoup += m_guiloadportList[i].TakeWaferOutFoup;//wafer被拿走              

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
            if (m_listSTG[0].Disable == false) tabPageABCD.Text += "A";
            if (m_listSTG[1].Disable == false) tabPageABCD.Text += "B";
            if (m_listSTG[2].Disable == false) tabPageABCD.Text += "C";
            if (m_listSTG[3].Disable == false) tabPageABCD.Text += "D";

            tabPageEFGH.Text = "";
            if (m_listSTG[4].Disable == false) tabPageEFGH.Text += "E";
            if (m_listSTG[5].Disable == false) tabPageEFGH.Text += "F";
            if (m_listSTG[6].Disable == false) tabPageEFGH.Text += "G";
            if (m_listSTG[7].Disable == false) tabPageEFGH.Text += "H";

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

            //隱藏page,消失頁籤
            tabCtrlAngle.SizeMode = TabSizeMode.Fixed;
            tabCtrlAngle.ItemSize = new Size(0, 1);
            tabCtrlAngle.Appearance = TabAppearance.FlatButtons;
            //PanelAligner
            guiNotchAngle1.OnAngleChange += btnAlignerRotAbs_Click;

            if (GParam.theInst.FreeStyle)
            {
                btnNext.Image = RorzeApi.Properties.Resources._32_next_;
                btnCancel.Image = RorzeApi.Properties.Resources._32_cancel_;
                btnSave.Image = RorzeApi.Properties.Resources._48_save_;

                btnTeach.Image = RorzeApi.Properties.Resources._48_work_;
            }
        }

        //選擇Aligner
        private void btnSelectAligner_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "frmTeachRobot", _strUserName, "Select Robot", btn.Name);

            gpRecipeNumber.Visible = false;
            tbRecipeName.Text = string.Empty;
            cbRecipeNumber.SelectedIndex = -1;
            for (int i = 0; i < m_btnSelectAlignerList.Count; i++)
            {


                if (m_btnSelectAlignerList[i] == btn)
                {
                    m_btnSelectAlignerList[i].BackColor = Color.LightBlue;

                    string strName1 = GParam.theInst.GetLanguage("AlignerA");
                    string strName2 = GParam.theInst.GetLanguage("AlignerB");
                    if (strName1 == btn.Text)
                    {
                        m_aligner = m_listALN[0];
                    }
                    else if (strName2 == btn.Text)
                    {
                        m_aligner = m_listALN[1];
                    }
                    else
                        switch (btn.Text)
                        {
                            case "AlignerA":
                            case "Aligner A":
                                m_aligner = m_listALN[0];
                                break;
                            case "AlignerB":
                            case "Aligner B":
                                m_aligner = m_listALN[1];
                                break;
                            default:
                                m_aligner = null;
                                break;
                        }

                    if (m_aligner == null)
                    {
                        new frmMessageBox("Aligner isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }
                    else
                    {


                        #region Select OCR Button
                        m_btnSelectOCRList.Clear();
                        tlpSelectOCR.Controls.Clear();
                        tlpSelectOCR.RowStyles.Clear();
                        tlpSelectOCR.ColumnStyles.Clear();
                        tlpSelectOCR.Dock = DockStyle.Fill;
                        for (int j = 0; j < 2; j++)
                        {
                            if (m_listOCR[j + 2 * (m_aligner.BodyNo - 1)].Disable) continue;
                            tlpSelectOCR.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
                            Button btn1 = new Button();
                            btn1.Font = new Font("Calibri", 18, FontStyle.Bold);
                            btn1.Text = "OCR " + (char)(64 + m_aligner.BodyNo) + (j + 1);
                            btn1.Dock = DockStyle.Fill;
                            btn1.TextAlign = ContentAlignment.MiddleCenter;
                            btn1.Click += btnSelectOCR_Click;
                            m_btnSelectOCRList.Add(btn1);
                            tlpSelectOCR.Controls.Add(btn1, 0, m_btnSelectOCRList.Count - 1);//注意
                        }
                        tlpSelectOCR.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
                        #endregion
                    }
                    continue;
                }
                else
                    m_btnSelectAlignerList[i].BackColor = System.Drawing.SystemColors.ControlLight;
            }

        }
        //選擇OCR
        private void btnSelectOCR_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "frmTeachRobot", _strUserName, "Select Robot", btn.Name);


            for (int i = 0; i < m_btnSelectOCRList.Count; i++)
            {

                bool isFront = false;
                if (m_btnSelectOCRList[i] == btn)
                {
                    m_btnSelectOCRList[i].BackColor = Color.LightBlue;

                    switch (btn.Text)
                    {
                        case "OCR A1":
                            m_SelectOCR = m_listOCR[0];
                            isFront = true;
                            break;
                        case "OCR A2":
                            m_SelectOCR = m_listOCR[1];
                            isFront = false;
                            break;
                        case "OCR B1":
                            m_SelectOCR = m_listOCR[2];
                            isFront = true;
                            break;
                        case "OCR B2":
                            m_SelectOCR = m_listOCR[3];
                            isFront = false;
                            break;
                        default: m_SelectOCR = null; break;
                    }

                    if (m_SelectOCR == null)
                    {
                        new frmMessageBox("OCR isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }
                    else
                    {

                        cbRecipeNumber.Items.Clear();
                        for (int n = 0; n < GParam.theInst.GetOCRRecipeMax; n++)
                            cbRecipeNumber.Items.Add("Recipe" + GParam.theInst.GetOCRRecipeIniFile(isFront)[n].Number.ToString("00"));

                        cbRecipeNumber.SelectedIndex = 0;

                        if (gpRecipeNumber.Visible == false)
                            gpRecipeNumber.Visible = true;
                    }
                    continue;
                }
                else
                    m_btnSelectOCRList[i].BackColor = System.Drawing.SystemColors.ControlLight;
            }

        }
        //選擇OcrType
        private void cbRecipeNumber_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isFront = m_SelectOCR.IsFront;

            if (cbRecipeNumber.SelectedIndex == -1) return;

            tbRecipeName.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Name;
        }
        private void cbRecipeNumber_SelectionChangeCommitted(object sender, EventArgs e)
        {
            bool isFront = m_SelectOCR.IsFront;

            if (cbRecipeNumber.SelectedIndex == -1) return;

            tbRecipeName.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Name;
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
                            {
                                lp.ResetAllSelectSlot();
                                ClearSelectWafer();
                            }
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

                    guiLoadport.ResetSlotSelectFlag("", temp1.SourceSlotIdx);
                    guiLoadport.UserSelectPlaceWaferInLoadport("", temp1.SourceSlotIdx, -1);

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

            for (int i = 0; i < m_listSTG.Count; i++)
            {
                if (m_listSTG[i].StatusMachine == enumStateMachine.PS_Process) continue;
                m_guiloadportList[i].EnableUISelectPutWaferFlag(false);
                m_guiloadportList[i].ResetUpdateMappingData();
            }
        }

        private void btnUIPickWaferAllClear_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_listSTG.Count; i++)
            {
                if (m_listSTG[i].StatusMachine == enumStateMachine.PS_Process) continue;
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
                    for (int i = 0; i < m_listSTG.Count; i++)
                    {
                        if (m_listSTG[i].Disable) continue;//220218 v1.003

                        m_listSTG[i].OnUclmComplete -= OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        m_listSTG[i].OnUclmComplete += OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        m_listSTG[i].OnUclm1Complete -= OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖
                        m_listSTG[i].OnUclm1Complete += OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖

                        m_listSTG[i].OnFoupExistChenge -= OnLoadport_FoupExistChenge;          //  更新UI
                        m_listSTG[i].OnFoupExistChenge += OnLoadport_FoupExistChenge;          //  更新UI

                        m_listSTG[i].OnClmpComplete -= OnLoadport_MappingComplete;             //  更新UI
                        m_listSTG[i].OnClmpComplete += OnLoadport_MappingComplete;             //  更新UI

                        m_listSTG[i].OnMappingComplete -= OnLoadport_MappingComplete;          //  更新UI
                        m_listSTG[i].OnMappingComplete += OnLoadport_MappingComplete;          //  更新UI

                        m_listSTG[i].OnStatusMachineChange -= OnLoadport_StatusMachineChange;  //  更新UI
                        m_listSTG[i].OnStatusMachineChange += OnLoadport_StatusMachineChange;  //  更新UI

                        m_listSTG[i].OnFoupIDChange -= OnLoadport_FoupIDChange;                //  更新UI
                        m_listSTG[i].OnFoupIDChange += OnLoadport_FoupIDChange;                //  更新UI

                        m_listSTG[i].OnFoupTypeChange -= OnLoadport_FoupTypeChange;            //  更新UI
                        m_listSTG[i].OnFoupTypeChange += OnLoadport_FoupTypeChange;            //  更新UI

                        m_listE84[i].OnAceessModeChange -= OnLoadport_E84ModeChange;                //  更新UI
                        m_listE84[i].OnAceessModeChange += OnLoadport_E84ModeChange;                //  更新UI
                    }

                    foreach (GUILoadport item in m_guiloadportList)//更新grouprecipe list
                    {
                        if (m_dbGrouprecipe != null)
                        {
                            item.SetRecipList(m_dbGrouprecipe.GetRecipeGroupList.Keys.ToArray(), "");
                        }

                        int nIndex = item.BodyNo - 1;
                        if (m_listSTG[nIndex].Disable == false)
                        {
                            //初始值
                            OnLoadport_FoupExistChenge(m_listSTG[nIndex], new FoupExisteChangEventArgs(m_listSTG[nIndex].FoupExist));
                            OnLoadport_MappingComplete(m_listSTG[nIndex], new LoadPortEventArgs(m_listSTG[nIndex].MappingData, m_listSTG[nIndex].BodyNo, true));
                            OnLoadport_StatusMachineChange(m_listSTG[nIndex], new OccurStateMachineChangEventArgs(m_listSTG[nIndex].StatusMachine));
                            OnLoadport_FoupIDChange(m_listSTG[nIndex], new EventArgs());
                            OnLoadport_FoupTypeChange(m_listSTG[nIndex], m_listSTG[nIndex].FoupTypeName);

                            OnLoadport_E84ModeChange(m_listE84[nIndex], new E84ModeChangeEventArgs(m_guiloadportList[nIndex].E84Status == GUILoadport.enumE84Status.Auto));
                        }
                    }

                    if (tabControl1.SelectedTab == tabPage1)
                    {
                        m_btnSelectAlignerList[0].PerformClick();
                    }
                }
                else
                {
                    for (int i = 0; i < m_listSTG.Count; i++)
                    {
                        m_listSTG[i].OnUclmComplete -= OnLoadport_OnUclmComplete;//220218 v1.003 Uudock解鎖
                        m_listSTG[i].OnUclm1Complete -= OnLoadport_OnUclm1Complete;//220218 v1.003 Uudock解鎖
                        m_listSTG[i].OnFoupExistChenge -= OnLoadport_FoupExistChenge;          //  更新UI
                        m_listSTG[i].OnClmpComplete -= OnLoadport_MappingComplete;             //  更新UI
                        m_listSTG[i].OnMappingComplete -= OnLoadport_MappingComplete;          //  更新UI
                        m_listSTG[i].OnStatusMachineChange -= OnLoadport_StatusMachineChange;  //  更新UI
                        m_listSTG[i].OnFoupIDChange -= OnLoadport_FoupIDChange;                //  更新UI
                        m_listSTG[i].OnFoupTypeChange -= OnLoadport_FoupTypeChange;            //  更新UI

                        m_listE84[i].OnAceessModeChange -= OnLoadport_E84ModeChange;                //  更新UI
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

            if (m_listSTG[Loader.BodyNo - 1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
            {
                MessageBox.Show("The loadport is frame type!\nCan not use in OCR teaching!");
                return;
            }

            if (!m_listSTG[Loader.BodyNo - 1].FoupExist) return;

            if (m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped ||
                m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Arrived ||
                m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_UnDocked ||
                m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToLoad ||
                m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToUnload)
            {
                m_listSTG[Loader.BodyNo - 1].CLMP();
            }
            else if (m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Docked)
            {
                m_listSTG[Loader.BodyNo - 1].WMAP();
            }
        }
        private void btnUnDock_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;

            if (!m_listSTG[Loader.BodyNo - 1].FoupExist) return;

            if (m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Docked ||
                m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Stop ||
                m_listSTG[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Complete)
            {
                m_listSTG[Loader.BodyNo - 1].UCLM();
            }
        }
        private void chkFoupOn_Checked(object sender, EventArgs e)//Simulate
        {
            GUILoadport Loader = (GUILoadport)sender;
            try
            {
                m_listSTG[Loader.BodyNo - 1].SimulateFoupOn(!m_listSTG[Loader.BodyNo - 1].FoupExist);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void btnTeach_Click(object sender, EventArgs e)
        {
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachOCR", _strUserName, "OCR", "Teach start");

            if (cbRecipeNumber.SelectedIndex == -1)
            {
                frmMessageBox frm = new frmMessageBox("Please select recipe number", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            if (m_aligner == null)
            {
                frmMessageBox frm = new frmMessageBox("The Aligner isn't find.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }
            else
            {
                switch (m_aligner.WaferType)
                {
                    case SWafer.enumWaferSize.Inch12:
                    case SWafer.enumWaferSize.Inch08:
                    case SWafer.enumWaferSize.Inch06:
                        tabCtrlAngle.SelectedTab = tabPageWafer;
                        break;
                    case SWafer.enumWaferSize.Frame:

                        break;
                    case SWafer.enumWaferSize.Panel:
                        tabCtrlAngle.SelectedTab = tabPagePanel;
                        break;
                    default:
                        break;
                }
            }

            if (m_aligner.Disable)
            {
                frmMessageBox frm = new frmMessageBox("The Aligner is disable.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            if (m_SelectOCR == null)
            {
                frmMessageBox frm = new frmMessageBox("The OCR isn't find.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            if (m_SelectOCR.Disable)
            {
                frmMessageBox frm = new frmMessageBox("The OCR is disable", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            //  判斷選擇的WAFER

            m_nSTG_address = m_nSelectSlot = -1;

            clsSelectWaferInfo temp1 = null;
            if (m_QueSelectSlotNum.TryPeek(out temp1) == false)
            {
                frmMessageBox frm = new frmMessageBox("Pls select wafer", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            I_Loadport loader = m_listSTG[temp1.SourceLpBodyNo - 1];
            if (loader.Disable)
            {
                frmMessageBox frm = new frmMessageBox("Loadport disable", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }
            if (loader.StatusMachine != enumStateMachine.PS_Docked)
            {
                frmMessageBox frm = new frmMessageBox("Loadport isn't docked", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }
            if (loader.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
            {
                frmMessageBox frm = new frmMessageBox("Loadport is frame type", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            #region 判斷Loadport對應的Robot
            foreach (I_Robot robot in m_listTRB.ToArray())
            {
                if (robot.RobotHardwareAllow(SWafer.enumPosition.Loader1 + loader.BodyNo - 1))
                {
                    m_robotSelect = robot;
                    break;
                }
            }
            if (m_robotSelect == null)
            {
                frmMessageBox frm = new frmMessageBox("No Robot available.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                frm.ShowDialog();
                return;
            }
            #endregion

            #region 判斷Robot放Aligner
            if (m_robotSelect.RobotHardwareAllow(SWafer.enumPosition.AlignerA + m_aligner.BodyNo - 1) == false)
            {
                frmMessageBox frm = new frmMessageBox("Aligner cannot be used on robots.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                frm.ShowDialog();
                return;
            }
            m_nALN_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.ALN1 + m_aligner.BodyNo - 1);
            ePosAln = SWafer.enumPosition.AlignerA + m_aligner.BodyNo - 1;
            #endregion

            #region 判斷 loadport 對應到 robot address
            switch (temp1.SourceLpBodyNo)
            {
                case 1://BodyNo
                    {
                        if (loader.UseAdapter)
                        {
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG1_08) + (int)loader.eFoupType;
                            ePos = enumPosition.Loader1;
                        }

                        else
                        {
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG1_12) + (int)loader.eFoupType;
                            ePos = enumPosition.Loader1;
                        } 
                    }
                    break;
                case 2://BodyNo
                    {
                        if (loader.UseAdapter)
                        {
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG2_08) + (int)loader.eFoupType;
                            ePos = enumPosition.Loader2;
                        }

                        else
                        {
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG2_12) + (int)loader.eFoupType;
                            ePos = enumPosition.Loader2;
                        } 
                    }
                    break;
                case 3://BodyNo
                    {
                        if (loader.UseAdapter)
                        {
                            ePos = enumPosition.Loader3;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG3_08) + (int)loader.eFoupType;
                        }
                            
                        else
                        {
                            ePos = enumPosition.Loader3;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG3_12) + (int)loader.eFoupType;
                        }
                    }
                    break;
                case 4://BodyNo
                    {
                        if (loader.UseAdapter)
                        {
                            ePos = enumPosition.Loader4;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG4_08) + (int)loader.eFoupType;
                        }

                        else
                        {
                            ePos = enumPosition.Loader4;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG4_12) + (int)loader.eFoupType;
                        }       
                    }
                    break;
                case 5://BodyNo
                    {
                        if (loader.UseAdapter)
                        {
                            ePos = enumPosition.Loader5;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG5_08) + (int)loader.eFoupType;
                        }
                            
                        else
                        {
                            ePos = enumPosition.Loader5;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG5_12) + (int)loader.eFoupType;
                        } 
                    }
                    break;
                case 6://BodyNo
                    {
                        if (loader.UseAdapter)
                        {
                            ePos = enumPosition.Loader6;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG6_08) + (int)loader.eFoupType;
                        }

                        else
                        {
                            ePos = enumPosition.Loader6;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG6_12) + (int)loader.eFoupType;
                        }   
                    }
                    break;
                case 7://BodyNo
                    {
                        if (loader.UseAdapter)
                        {
                            ePos = enumPosition.Loader7;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG7_08) + (int)loader.eFoupType;
                        }
                        else
                        {
                            ePos = enumPosition.Loader7;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG7_12) + (int)loader.eFoupType;
                        }   
                    }
                    break;
                case 8://BodyNo
                    {
                        if (loader.UseAdapter)
                        {
                            ePos = enumPosition.Loader8;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG8_08) + (int)loader.eFoupType;
                        }
                        else
                        {
                            ePos = enumPosition.Loader8;
                            m_nSTG_address = GParam.theInst.GetDicPosRobot(m_robotSelect.BodyNo, enumRbtAddress.STG8_12) + (int)loader.eFoupType;
                        }                            
                    }
                    break;
                default: m_nSTG_address = -1; break;
            }
            #endregion


            SWafer waferData = loader.Waferlist[temp1.SourceSlotIdx];//Wafer資料
            m_nSelectSlot = temp1.SourceSlotIdx + 1;//    Wafer slot

            //過帳需要
            m_robotSelect.PrepareUpperWafer = waferData;

            if (m_nSelectSlot == -1 || m_nSTG_address == -1 || waferData == null)
            {
                frmMessageBox frm = new frmMessageBox("Pls select wafer", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            switch (waferData.Owner)
            {
                case SWafer.enumFromLoader.LoadportA: if (m_listSTG[0].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportB: if (m_listSTG[1].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportC: if (m_listSTG[2].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportD: if (m_listSTG[3].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportE: if (m_listSTG[4].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportF: if (m_listSTG[5].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportG: if (m_listSTG[6].StatusMachine != enumStateMachine.PS_Docked) return; break;
                case SWafer.enumFromLoader.LoadportH: if (m_listSTG[7].StatusMachine != enumStateMachine.PS_Docked) return; break;
            }
            //  主選單上鎖
            delegateMDILock?.Invoke(true);
            //  切到第二頁
            tabControl1.SelectedTab = tabPage2;
            TeachOCRStart();

        }
        //   開始Teaching
        private void TeachOCRStart()
        {
            EnableProcedureButton1(true);
            EnableControlButton1(false);

            m_robotSelect.ResetProcessCompleted();
            m_robotSelect.SspdW(m_robotSelect.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(m_robotSelect.BodyNo - 1));//進入就降到ModeSpeed
            m_robotSelect.WaitProcessCompleted(m_robotSelect.GetAckTimeout);

            textWaferID.Text = string.Empty;

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Button [Next] to start the process.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Button [Next] to start the process.\r"));

            bool isFront = m_SelectOCR.IsFront;

            if (m_aligner.BodyNo == 1)
                AngleE.Text = string.Format("{0}", GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Angle_A);                     //取得OCR Recipe角度位置
            else if (m_aligner.BodyNo == 2)
                AngleE.Text = string.Format("{0}", GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Angle_B);

            WaferIDLengthE.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].WaferIDLength.ToString();
            LotIDFirstPositionE.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].LotIDFirstPosition.ToString();        //取得LotID第一個位置
            LotIDLengthE.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].LotIDLength.ToString();                      //取得LotID長度
            WaferNoFirstPositionE.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].WaferNoFirstPosition.ToString();    //取得WaferNo第一個位置

            m_eStep = eTeachStep.Prepare;
            Cursor.Current = Cursors.WaitCursor;
        }

        #region ========== tabpage2 teaching
        //  Button
        private void btnSave_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachOCR", _strUserName, "OCR", btn.Name);

            EnableProcedureButton1(false);

            bool bFind = false;
            foreach (string item in m_SelectOCR.getRecipt())
            {
                if (item.Contains(tbRecipeName.Text))
                {
                    bFind = true;
                    break;
                }
            }
            if (bFind == false)
            {
                frmMessageBox frm = new frmMessageBox(tbRecipeName.Text + GParam.theInst.GetLanguage("Name not found in OCR"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frm.ShowDialog();
                return;
            }

            bool isFront = m_SelectOCR.IsFront;
            //bool isFront = (_strSelectStgName.IndexOf("Front") >= 0);

            GParam.theInst.SetRecipeName(cbRecipeNumber.SelectedIndex, tbRecipeName.Text, isFront);
            if (m_aligner.BodyNo == 1)
                GParam.theInst.SetRecipeAngle_A(cbRecipeNumber.SelectedIndex, double.Parse(AngleE.Text), isFront);
            else if (m_aligner.BodyNo == 2)
                GParam.theInst.SetRecipeAngle_B(cbRecipeNumber.SelectedIndex, double.Parse(AngleE.Text), isFront);

            GParam.theInst.SetRecipeWaferIDLength(cbRecipeNumber.SelectedIndex, int.Parse(WaferIDLengthE.Text), isFront);
            GParam.theInst.SetRecipeLotIDFirstPosition(cbRecipeNumber.SelectedIndex, int.Parse(LotIDFirstPositionE.Text), isFront);
            GParam.theInst.SetRecipeLotIDLength(cbRecipeNumber.SelectedIndex, int.Parse(LotIDLengthE.Text), isFront);
            GParam.theInst.SetRecipeWaferNoFirstPosition(cbRecipeNumber.SelectedIndex, int.Parse(WaferNoFirstPositionE.Text), isFront);
            GParam.theInst.SetRecipeStored(cbRecipeNumber.SelectedIndex, 1, isFront);

            GParam.theInst.LoadOCRRecipeIniFile();  //  有更新重新讀取

            Cursor.Current = Cursors.WaitCursor;
            EnableProcedureButton1(true);
        }
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
                    SkipPrepareTask();
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

        //  Button
        private void btnAlignerRotAbs_Click(object sender, double dAngle)
        {

            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachOCR", _strUserName, "ALIGNER", "ABS Angle: " + (dAngle).ToString());

            EnableProcedureButton1(false);
            EnableControlButton1(false);
            m_aligner.DoManualProcessing += (object Manual) =>
            {
                I_Aligner alignerManual = Manual as I_Aligner;

                double n = dAngle;
                alignerManual.ResetInPos();
                alignerManual.AlgnDW(3000, n.ToString());
                alignerManual.WaitInPos(30000);



                this.BeginInvoke(new Action(() =>
                {
                    //PosW目前Aligner 動作的角度位置(pulse)
                    PoseW = (float)n; //Aligner角度
                    AngleE.Text = PoseW.ToString();

                }));


            };
            m_aligner.OnManualCompleted -= _aligner_AlgnCompleted;
            m_aligner.OnManualCompleted += _aligner_AlgnCompleted;
            m_aligner.StartManualFunction();
        }




        //  Button
        private void btnRead_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachOCR", _strUserName, "OCR", btn.Name);

            EnableProcedureButton1(false);
            EnableControlButton1(false);

            if (m_SelectOCR != null)
            {
                bool bSucc = false;

                while (true)
                {
                    if (false == m_SelectOCR.OffLine())
                        break;
                    if (m_SelectOCR.SetRecipe(tbRecipeName.Text) == false)
                        break;
                    if (false == m_SelectOCR.OnLine())
                        break;
                    string strResult = string.Empty;

                    if (m_SelectOCR.Read(ref strResult, true) == false)//TEACHING
                        break;
                    textWaferID.Text = strResult;


                    WaferIDLengthE.Text = strResult.Length.ToString(); //  Wafer ID Length由讀到的條碼更新

                    frmMessageBox frm = new frmMessageBox(GParam.theInst.GetLanguage("WaferID : ") + strResult, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    frm.ShowDialog();

                    bSucc = true;
                    break;
                }
                if (false == bSucc)
                {
                    frmMessageBox frm = new frmMessageBox("OCR Read failure!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    frm.ShowDialog();
                }
            }

            EnableProcedureButton1(true);
            EnableControlButton1(true);
        }
        //  Button
        private void btnTop_Click(object sender, EventArgs e)
        {
            ActiveInSight();
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
            ChangeButtun(btnTop, bAct);
            ChangeButtun(btnSave, bAct);
            ChangeButtun(btnRead, bAct);
            ChangeButtun(btnAlignerCW, bAct);
            ChangeButtun(btnAlignerCCW, bAct);

            BeginInvoke(new Action(() => gbStep.Enabled = bAct));
        }
        //  Step1 Next
        private void DoPrepareTask()    //  手臂 Load Unld 
        {
            m_robotSelect.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;

                //robotManual.ResetInPos();
                //robotManual.LoadW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, m_nSTG_address, m_nSelectSlot);
                //robotManual.WaitInPos(robotManual.GetMotionTimeout);
                robotManual.TakeWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, enumRobotArms.UpperArm, ePos, m_nSTG_address, m_nSelectSlot);

                if (m_aligner is SSAlignerPanelXYR || m_aligner is SSAlignerTAL303)
                {
                    m_aligner.ResetInPos();
                    m_aligner.ORGN();
                    m_aligner.WaitInPos(10000);
                }

                //robotManual.ResetInPos();
                //robotManual.UnldW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, m_nALN_address, 1);
                //robotManual.WaitInPos(robotManual.GetMotionTimeout);
                robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, enumRobotArms.UpperArm, ePosAln, m_nALN_address, 1);

                bool isFront = m_SelectOCR.IsFront;

                string strAngle = "0";
                //if (m_aligner.BodyNo == 1)
                //    strAngle = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Angle_A.ToString();
                //else if (m_aligner.BodyNo == 2)
                //    strAngle = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Angle_B.ToString();
                m_aligner.ResetInPos();
                m_aligner.AlgnDW(3000, strAngle);
                m_aligner.WaitInPos(10000);

                if (m_aligner is SSAlignerPanelXYR)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        //PosW目前Aligner 動作的角度位置(pulse)
                        PoseW = float.Parse(strAngle); //Aligner角度
                        AngleE.Text = PoseW.ToString();

                    }));
                }
                else
                {
                    m_aligner.GposRW(3000); //  問位置
                }

            };
            m_robotSelect.OnManualCompleted -= FinishRobotTack;
            m_robotSelect.OnManualCompleted += FinishRobotTack;

            m_robotSelect.StartManualFunction();

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Executing Robot move wafer to Aligner,please Wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Executing Robot move wafer to Aligner,please Wait.\r"));
            Cursor.Current = Cursors.WaitCursor;
        }
        //  Step1 Finish
        private void FinishRobotTack(object sender, bool bSuc)
        {
            m_robotSelect.OnManualCompleted -= FinishRobotTack;

            rtbInstruct.Clear();
            rtbInstruct.AppendText(bSuc ? "Start Teaching OCR, click[Next] when finished\r" : "Executing Robot move wafer to Alinger fail,click[Next] when retry\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage(bSuc ? "Start Teaching OCR, click[Next] when finished\r" : "Executing Robot move wafer to Alinger fail,click[Next] when retry\r"));


            m_eStep = eTeachStep.Teach;


            int PosW = m_aligner.Raxispos;
            if (PosW == 0)
                PosW = 360000;

            bool isFront = m_SelectOCR.IsFront;

            //PosW目前Aligner 動作的角度位置(pulse)
            //PoseW = ((float)PosW) / 1000; //Aligner角度
            //if (m_bSimulate == false)
            //{
            //    AngleE.Text = PoseW.ToString();
            //}
            //else
            {
                if (m_aligner.BodyNo == 1)
                    AngleE.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Angle_A.ToString();
                else if (m_aligner.BodyNo == 2)
                    AngleE.Text = GParam.theInst.GetOCRRecipeIniFile(isFront)[cbRecipeNumber.SelectedIndex].Angle_B.ToString();

            }
            EnableControlButton1(true);
            if (bSuc) EnableProcedureButton1(true);//給他按NEXT
            Cursor.Current = Cursors.Default;
        }
        //  Step1 Cancel
        private void SkipPrepareTask()
        {
            if (new frmMessageBox(GParam.theInst.GetLanguage("Abort teaching ? Recipe:") + m_SelectOCR.Name, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton1(true);
                return;
            }

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Teaching OCR is over, please click [Next]\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Teaching OCR is over, please click [Next]\r"));
            m_eStep = eTeachStep.End;

            EnableProcedureButton1(true);
            Cursor.Current = Cursors.Default;
        }
        //  Step2 Next
        private void DoTeachTask(bool bCancel)      //  回收wafer
        {
            if (bCancel)
            {
                if (new frmMessageBox("Parameter will not be recorded. Are you sure you want to give up the teaching?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != DialogResult.Yes)
                {
                    EnableProcedureButton1(true);
                    return;
                }
            }
            else
            {
                if (new frmMessageBox(GParam.theInst.GetLanguage("Are you sure complete teaching ?") + m_SelectOCR.Name, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
                {
                    EnableProcedureButton1(true);
                    return;
                }

                btnSave.PerformClick();//Save
            }

            EnableControlButton1(false);



            rtbInstruct.Clear();
            rtbInstruct.AppendText("Please wait for wafer recover\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Please wait for wafer recover\r"));

            m_robotSelect.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;
                //過帳需要
                SWafer waferData = m_aligner.Wafer;//Wafer資料
                robotManual.PrepareUpperWafer = waferData;

                m_aligner.ResetInPos();
                m_aligner.UclmW(3000);
                m_aligner.WaitInPos(10000);

                //robotManual.ResetInPos();
                //robotManual.LoadW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, m_nALN_address, 1);
                //robotManual.WaitInPos(robotManual.GetMotionTimeout);
                robotManual.TakeWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, enumRobotArms.UpperArm, ePosAln, m_nALN_address, 1);

                //robotManual.ResetInPos();
                //robotManual.UnldW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, m_nSTG_address, m_nSelectSlot);
                //robotManual.WaitInPos(robotManual.GetMotionTimeout);
                robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, enumRobotArms.UpperArm, ePos, m_nSTG_address, m_nSelectSlot);

                waferData.ProcessStatus = SWafer.enumProcessStatus.Sleep;
            };
            m_robotSelect.OnManualCompleted -= FinishRobotPut;
            m_robotSelect.OnManualCompleted += FinishRobotPut;
            m_robotSelect.StartManualFunction();
            Cursor.Current = Cursors.WaitCursor;
        }
        //  Step2 Finish
        private void FinishRobotPut(object sender, bool bSuc)
        {
            m_robotSelect.OnManualCompleted -= FinishRobotPut;

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Teaching OCR is over, please click [Next]\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Teaching OCR is over, please click [Next]\r"));
            m_eStep = eTeachStep.End;

            EnableProcedureButton1(true);
            Cursor.Current = Cursors.Default;
        }

        //  Step3
        private void ProcessEnd()
        {

            m_robotSelect.ResetProcessCompleted();
            if (m_bIsRunMode == true)
                m_robotSelect.SspdW(m_robotSelect.GetAckTimeout, GParam.theInst.GetRobot_RunSpeed(m_robotSelect.BodyNo - 1)); //  切回原速度
            else
                m_robotSelect.SspdW(m_robotSelect.GetAckTimeout, GParam.theInst.GetRobot_MaintSpeed(m_robotSelect.BodyNo - 1)); //  切回原速度
            m_robotSelect.WaitProcessCompleted(m_robotSelect.GetAckTimeout);

            EnableControlButton1(false);
            tabControl1.SelectedTab = tabPage1;

            if (delegateMDILock != null)//主選單上鎖
                delegateMDILock(false);
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
            for (int nPort = 0; nPort < m_listSTG.Count; nPort++)
            {
                if (m_listSTG[nPort].Disable) continue;

                m_guiloadportList[nPort].KeepClamp = m_listSTG[nPort].IsKeepClamp;

                if (m_listSTG[nPort].Waferlist == null) continue;

                for (int nSlot = 1; nSlot <= m_listSTG[nPort].Waferlist.Count; nSlot++)
                {
                    SWafer waferShow = m_listSTG[nPort].Waferlist[nSlot - 1];

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

            if (e.FoupExist == false) btnTeach.Enabled = false; //  沒有FOUP不能按開始
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

        private void _aligner_AlgnCompleted(object sender, bool bSuc)
        {

            EnableProcedureButton1(true);
            EnableControlButton1(true);
            Cursor.Current = Cursors.WaitCursor;
        }


        #region ========== 開外部程式 In-Sight Explorer==========
        private void ActiveInSight()
        {
            int nIndx = (m_aligner.BodyNo - 1) * 2 + (m_SelectOCR.IsFront ? 1 : 0);
            enumOcrType eType = GParam.theInst.GetOcrType(nIndx);
            try
            {
                Process[] proc = null;
                string strPath = string.Empty, strFile = string.Empty;
                switch (eType)
                {
                    case enumOcrType.IS1740:
                        proc = Process.GetProcessesByName("In-Sight Explorer");

                        strPath = "C:\\Program Files (x86)\\Cognex\\In-Sight\\In-Sight Explorer Wafer 4.5.0";
                        strFile = "In-Sight Explorer.exe";
                        if (Directory.Exists(strPath) == false)
                        {
                            strPath = @"C:\Program Files (x86)\Cognex\In-Sight\In-Sight Explorer Wafer 4.5.2";
                        }
                        //C:\Program Files (x86)\Cognex\In-Sight\In-Sight Explorer Wafer 4.5.2\In-Sight Explorer.exe
                        break;
                    case enumOcrType.WID120:
                        proc = Process.GetProcessesByName("javaw");
                        //"C:\Program Files (x86)\IOSS\WID120\jre\bin\javaw.exe" -jar -Dsun.java2d.dpiaware=false "C:\Program Files (x86)\IOSS\WID120\WID120.jar"         
                        strPath = @"C:\Program Files (x86)\IOSS\WID120\jre\bin";
                        strFile = "javaw.exe";
                        break;
                    default:
                        return;
                }

                //判斷程式已經開啟
                if (proc.Length == 0)
                {

                    if (Directory.Exists(strPath) == false)
                    {
                        new frmMessageBox(strPath + " is not a valid file or directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        return;
                    }

                    switch (eType)
                    {
                        case enumOcrType.IS1740:
                            var startInfo = new ProcessStartInfo();
                            startInfo.WorkingDirectory = strPath;
                            startInfo.FileName = strFile;
                            Process.Start(startInfo);
                            break;
                        case enumOcrType.WID120:
                            // 設定 Java 軟體的路徑
                            string javaPath = @"C:\Program Files (x86)\IOSS\WID120\jre\bin\javaw.exe";
                            // 設定 Java 軟體的命令行參數
                            string javaArguments = "-jar \"-Dsun.java2d.dpiaware=false\" " + "\"C:\\Program Files (x86)\\IOSS\\WID120\\WID120.jar\"";
                            // 建立 ProcessStartInfo 物件
                            ProcessStartInfo startInfo2 = new ProcessStartInfo();
                            startInfo2.WorkingDirectory = @"C:\Program Files (x86)\IOSS\WID120";
                            startInfo2.FileName = javaPath;
                            startInfo2.Arguments = javaArguments;
                            // 啟動 Java 軟體
                            Process.Start(startInfo2);
                            break;
                    }
                    SpinWait.SpinUntil(() => false, 1000);
                }
                else
                {
                    bringToFront(proc[0].MainWindowTitle);
                    //bringToFront("In-Sight Explorer - admin - [OCRA2 - Wafer ID View]"); //填入視窗的Title
                }
            }
            catch (Exception ex)
            {

                switch (eType)
                {
                    case enumOcrType.IS1740:
                        new frmMessageBox("[In-Sight Explorer]" + GParam.theInst.GetLanguage("Open fail,Please start manually"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        break;
                    case enumOcrType.WID120:
                        new frmMessageBox("[WID120]" + GParam.theInst.GetLanguage("Open fail,Please start manually"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        break;
                    default:
                        new frmMessageBox(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        break;
                }

            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

        public static void bringToFront(string title)
        {
            // Get a handle to the Calculator application.
            IntPtr handle = FindWindow(null, title);
            // Verify that Calculator is a running process.
            if (handle == IntPtr.Zero)
            {
                return;
            }
            BringWindowToTop(handle); // 將視窗浮在最上層
            ShowWindow(handle, 3); // 將視窗最大化
        }

        #endregion


    }
}
