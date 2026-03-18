using Advantech.Adam;
using RorzeApi.Class;
using RorzeApi.GUI;
using RorzeApi.SECSGEM;
using RorzeComm.Log;
using RorzeUnit.Class;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace RorzeApi
{
    public partial class frmTransferUndo : Form
    {
        private enum eTransferUndoStep { CheckRFID = 0, Start, End };

        List<GUILoadport> m_guiloadportList = new List<GUILoadport>();
        List<GUITower> m_guiTowerList = new List<GUITower>();
        List<I_Robot> ListTRB;
        List<I_Loadport> ListSTG;
        List<I_Aligner> ListALN;
        List<I_E84> ListE84;
        List<I_Buffer> ListBUF;

        List<I_RFID> ListRFID;

        SStockerSQL m_MySQL;
        STransfer m_Transfer;
        SAlarm m_Alarm;
        SGEM300 m_Gem;
        PJCJManager m_JobControl;
        SProcessDB m_dbProcess;
        SGroupRecipeManager m_dbGrouprecipe;
        SLogger m_logger = SLogger.GetLogger("ExecuteLog");

        private bool m_bSimulate = false;


        eTransferUndoStep m_eStep;
        List<Label> ListlblRfidRecord = new List<Label>();
        List<Label> ListlblRfid = new List<Label>();


        public frmTransferUndo(List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN, List<I_E84> listE84, List<I_Buffer> listBUF, List<I_Stock> listSTK, List<I_RFID> listRFID,
            SStockerSQL mySQL, STransfer transfer, SAlarm alarm, SGEM300 gem, PJCJManager jobControl, SProcessDB dbProcess, SGroupRecipeManager dbGrouprecipe, bool bSimulate)
        {
            InitializeComponent();

            ListTRB = listTRB;
            ListSTG = listSTG;
            ListALN = listALN;
            ListE84 = listE84;
            ListBUF = listBUF;
            ListSTK = listSTK;
            ListRFID = listRFID;
            m_MySQL = mySQL;
            m_Transfer = transfer;
            m_Alarm = alarm;
            m_Gem = gem;
            m_JobControl = jobControl;
            m_dbProcess = dbProcess;
            m_dbGrouprecipe = dbGrouprecipe;
            m_bSimulate = bSimulate;

            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);

            //STG
            ListlblRfidRecord.Add(lblSTG1_RfidRecord);
            ListlblRfidRecord.Add(lblSTG2_RfidRecord);
            ListlblRfidRecord.Add(lblSTG3_RfidRecord);
            ListlblRfidRecord.Add(lblSTG4_RfidRecord);

            ListlblRfid.Add(lblSTG1_Rfid);
            ListlblRfid.Add(lblSTG2_Rfid);
            ListlblRfid.Add(lblSTG3_Rfid);
            ListlblRfid.Add(lblSTG4_Rfid);

            tabControl1.SelectedTab = tabPageFirst;

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
                m_guiloadportList[i].Disable_OCR = true/*GParam.theInst.IsOcrAllDisable()*/;
                m_guiloadportList[i].Disable_Recipe = true /*(GParam.theInst.IsOcrAllDisable() && GParam.theInst.GetEQRecipeCanSelect == false)*/;
                m_guiloadportList[i].Disable_RSV = true;
                m_guiloadportList[i].Disable_ClmpLock = true;
                m_guiloadportList[i].Disable_DockBtn = true;

                m_guiloadportList[i].Disable_ProcessBtn = true;
                m_guiloadportList[i].Visible = !ListSTG[i].Disable;
                m_guiloadportList[i].Enabled = !ListSTG[i].Disable;
                m_guiloadportList[i].ShowSelectColor = false;
                if (ListSTG[i].Disable)
                {
                    continue;//不需要註冊
                }

                ListSTG[i].OnFoupExistChenge += Loadport_FoupExistChenge;          //  更新UI  
                ListSTG[i].OnClmpComplete += Loadport_MappingComplete;             //  更新UI
                ListSTG[i].OnMappingComplete += Loadport_MappingComplete;          //  更新UI
                ListSTG[i].OnStatusMachineChange += Loadport_StatusMachineChange;  //  更新UI
                ListSTG[i].OnFoupIDChange += Loadport_FoupIDChange;                //  更新UI
                ListSTG[i].OnFoupTypeChange += Loadport_FoupTypeChange;            //  更新UI

                //ListSTG[i].OnUclmComplete += Loadport_OnUclmComplete;              //  更新UI
                //ListE84[i].OnAceessModeChange += Loadport_E84ModeChange;                //  更新UI
                ListSTG[i].OnTakeWaferInFoup += m_guiloadportList[i].TakeWaferInFoup;//wafer被塞進來
                ListSTG[i].OnTakeWaferOutFoup += m_guiloadportList[i].TakeWaferOutFoup;//wafer被拿走

                //m_guiloadportList[i].BtnClamp += btnClamp_Click;
                //m_guiloadportList[i].BtnDock += btnDock_Click;
                //m_guiloadportList[i].BtnUnDock += btnUnDock_Click;
                //m_guiloadportList[i].BtnE84Mode += btnE84Mode_Click;
                //m_guiloadportList[i].ChkRecipeSelect += chkRecipeSelect_Checked;
                //m_guiloadportList[i].BtnProcess += btnProcess_Click;
                //m_guiloadportList[i].ChkFoupOn += chkFoupOn_Checked;
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
                //  消失頁籤
                tabPageEFGH.Parent = null;
                int nCount = tabPageABCD.Text.Length;
                if (nCount <= 1)
                {
                    foreach (GUILoadport item in m_guiloadportList)
                    {
                        item.Width = (flowLayoutPanel4.Width - 0) / 2;
                        item.Height = flowLayoutPanel4.Height;
                    }
                }
                else
                {
                    foreach (GUILoadport item in m_guiloadportList)
                    {
                        item.Width = (flowLayoutPanel4.Width - 0) / nCount;
                        item.Height = flowLayoutPanel4.Height;
                    }
                }
            }
            else//有第二頁
            {
                tabPageEFGH.Parent = tabCtrlStage;
                foreach (GUILoadport item in m_guiloadportList)
                {
                    item.Width = (flowLayoutPanel4.Width - 0) / 4;
                    item.Height = flowLayoutPanel4.Height;
                }
            }

            #endregion

            #region Tower

            m_guiTowerList.Add(guiTower1);
            m_guiTowerList.Add(guiTower2);
            m_guiTowerList.Add(guiTower3);
            m_guiTowerList.Add(guiTower4);

            for (int i = 0; i < ListSTK.Count; i++)
            {
                I_Stock tower = ListSTK[i];

                m_guiTowerList[i].Simulate = GParam.theInst.IsSimulate;
                m_guiTowerList[i].BodyNo = i + 1;
                m_guiTowerList[i].Visible = tower.Disable == false;

                m_guiTowerList[i].OnSlotLabelMouseEnter += OnSlotLabelMouseEnter;//UI事件
                m_guiTowerList[i].OnSlotLabelMouseLeave += OnSlotLabelMouseLeave;//UI事件
                //m_guiTowerList[i].OnDataGridView1_CellClick += GuiTowerDataGridView_CellClick;//UI事件
                m_guiTowerList[i].SetHardwareParam(tower.TowerCount, tower.TheTowerSlotNumber);

                if (GParam.theInst.FreeStyle)
                    m_guiTowerList[i].SetFreeStyleColor(
                        Color.FromArgb(98, 186, 166),//wafer                        
                        Color.FromArgb(151, 218, 203));

                tower.OnMappingCompleteAll += OnStock_MappingComplete;//  更新UI   

                tower.OnTakeWaferInFoup += m_guiTowerList[i].TakeWaferInFoup;//wafer被塞進來
                tower.OnTakeWaferOutFoup += m_guiTowerList[i].TakeWaferOutFoup;//wafer被拿走                 


            }
            #endregion

            tlpSimulateFoupOn.Visible = m_bSimulate;
            m_tmr.Enabled = true;

            if (GParam.theInst.FreeStyle)
            {

                button1.BackColor = GParam.theInst.ColorTitle;
                button1.ForeColor = Color.Black;
                btnOK.Image = RorzeApi.Properties.Resources._32_next_;
                btnExit.Image = RorzeApi.Properties.Resources._32_cancel_;
                this.Icon = RorzeApi.Properties.Resources.bwbs_;
            }
            else
            {
                this.Icon = RorzeApi.Properties.Resources.R;
            }
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }

        private void InvokeAddMessage(object sender, string strMsg)
        {
            if (this.InvokeRequired) // 若非同執行緒
            {
                EventHandler<string> del = new EventHandler<string>(InvokeAddMessage); //利用委派執行
                this.BeginInvoke(del, sender, strMsg);
            }
            else
            {
                RichTextBox tbx = sender as RichTextBox;
                tbx.Text = strMsg;
            }
        }

        private void m_tmr_Tick(object sender, EventArgs e)
        {
            m_tmr.Enabled = false;

            #region ==========Loadport ==========
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

            m_tmr.Enabled = true;
        }
        private void frmTransferUndo_Load(object sender, EventArgs e)
        {
            //更新stocker的wafer資料
            for (int i = 0; i < ListSTK.Count; i++)
            {
                if (ListSTK[i].Disable) continue;
                OnStock_MappingComplete(ListSTK[i], ListSTK[i].GetMapDataAll());
            }

            DataTable dt = m_MySQL.SelectWaferTransferWithoutPtoP_TtoT();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string strSource = dt.Rows[i]["Source"].ToString();
                string strTarget = dt.Rows[i]["Target"].ToString();
                m_strGradeName = dt.Rows[i]["WaferGrade"].ToString();//紀錄沒做完的Grade

                string[] strASource = strSource.Split(new char[] { '-' });
                string[] strATarget = strTarget.Split(new char[] { '-' });

                if (strASource[0] != string.Empty && strASource[0][0] == 'P')
                {
                    if (strATarget[0] != string.Empty && strATarget[0][0] == 'T')
                    {
                        //LPtoTower
                        int nBody = int.Parse(strASource[0].Replace('P', ' '));
                        if (strASource[2] != "strFoupID")//怕被RECOVER搞錯
                            ListlblRfidRecord[nBody - 1].Text = strASource[2];
                    }
                }
                if (strASource[0] != string.Empty && strASource[0][0] == 'T')
                {
                    if (strATarget[0] != string.Empty && strATarget[0][0] == 'P')
                    {
                        //TowerToLP
                        int nBody = int.Parse(strATarget[0].Replace('P', ' '));
                        if (strATarget[2] != "strFoupID")//怕被RECOVER搞錯
                            ListlblRfidRecord[nBody - 1].Text = strATarget[2];
                    }
                }
            }

            dgvUndoData.DataSource = dt;
            dgvUndoData.Columns["No"].Visible = false;
            dgvUndoData.Columns["Commander"].Visible = false;
            dgvUndoData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvUndoData.RowHeadersVisible = false;

            rtbMessage.AppendText("Please place FOUP first. Confirm RFID match!!\r");
            rtbMessage.AppendText("請放置晶圓盒，確認信息條碼匹配!!\r");
        }
        private void frmTransferUndo_VisibleChanged(object sender, EventArgs e)
        {
            m_tmr.Enabled = this.Visible;
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            btnOK.Enabled = btnExit.Enabled = false;

            switch (m_eStep)
            {
                case eTransferUndoStep.CheckRFID:
                    DoCheckRFID();
                    break;
                case eTransferUndoStep.Start:
                    DoStart();
                    break;
                case eTransferUndoStep.End:
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    break;
            }
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            switch (m_eStep)
            {
                case eTransferUndoStep.CheckRFID:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
                case eTransferUndoStep.Start:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
                case eTransferUndoStep.End:
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    break;
            }
        }
        private void DoCheckRFID()
        {
            try
            {
                rtbMessage.Clear();
                rtbMessage.AppendText("Confirm RFID, Please Wait.\r");
                rtbMessage.AppendText("確認RFID匹配.\r");

                for (int i = 0; i < ListSTG.Count; i++)
                {
                    I_Loadport stg = ListSTG[i];
                    if (stg.Disable) continue;

                    if (ListlblRfidRecord[i].Text == null || ListlblRfidRecord[i].Text == string.Empty) continue;

                    if (ListlblRfid[i].Text != ListlblRfidRecord[i].Text)
                    {
                        rtbMessage.Clear();
                        rtbMessage.AppendText("Loadport doesn't match, can't run.\r");
                        rtbMessage.AppendText("Loadport 不匹配,無法執行.\r");

                        rtbMessage.AppendText(" Please restart the software.\r");
                        rtbMessage.AppendText(" 請重新啟動軟體.\r");

                        btnOK.Enabled = btnExit.Enabled = true;
                        return;
                    }
                }

                rtbMessage.Clear();
                rtbMessage.AppendText("Excuting loadport docking,Please Wait\r");
                rtbMessage.AppendText("執行載具平台開門\r");

                for (int i = 0; i < ListSTG.Count; i++)
                {
                    I_Loadport stg = ListSTG[i];
                    if (stg.Disable || stg.FoupExist == false) continue;

                    ListSTG[i].OnClmpComplete += DoCheckRFIDComplete;

                    GUILoadport gui = m_guiloadportList[i];
                    btnDock_Click(gui, null);
                }

            }
            catch (Exception ex) { WriteLog("<Exception>" + ex); }

        }
        private void DoCheckRFIDComplete(object sender, LoadPortEventArgs e)
        {
            try
            {
                I_Loadport item = (I_Loadport)sender;
                item.OnClmpComplete -= DoCheckRFIDComplete;//註銷註冊
                lock (this)
                {
                    //檢查Loadport都完成Docking
                    for (int i = 0; i < ListSTG.Count; i++)
                    {
                        I_Loadport stg = ListSTG[i];
                        if (stg.Disable) continue;
                        if (stg.FoupExist && stg.StatusMachine != enumStateMachine.PS_Docked)
                            return;//有未完成
                    }

                    //切換到第二頁
                    this.BeginInvoke((EventHandler)delegate
                    {
                        tabControl1.SelectedTab = tabPageSecond;
                    });
                    SpinWait.SpinUntil(() => false, 500);

                    rtbMessage.Clear();
                    rtbMessage.AppendText("Creating a Wafer Sort.\t\t\t\t\t建立晶圓排序.\r");

                    //分配Wafer
                    m_eTransferMode = enumTransferMode.Stock;
                    m_eTransferModeType = enumTransferModeType.WaferInOut;
                    DataTable dt = m_MySQL.SelectWaferTransferWithoutPtoP_TtoT();

                    string strErrMsg = string.Empty;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string strSource = dt.Rows[i]["Source"].ToString();
                        string strTarget = dt.Rows[i]["Target"].ToString();
                        string[] strASource = strSource.Split(new char[] { '-' });
                        string[] strATarget = strTarget.Split(new char[] { '-' });

                        string strSTG = string.Empty, strSTK = string.Empty;
                        int nStgBody, nStgSlot, nTower1to16, nTowerSlot1to200;

                        if (strASource[0] != string.Empty && strASource[0][0] == 'P')
                        {
                            nStgBody = int.Parse(strASource[0].Replace('P', ' '));
                            nStgSlot = int.Parse(strASource[1].Replace('S', ' '));
                            strSTG = strSource;
                            rtbMessage.AppendText(string.Format("Check Loadport{0} Slot{1:D2}.\t\t\t\t\t", nStgBody, nStgSlot));
                            rtbMessage.AppendText(string.Format("檢查 晶圓加載{0} 層{1:D2}.\r", nStgBody, nStgSlot));
                            if (ListSTG[nStgBody - 1].MappingData[nStgSlot - 1] != '0')//目標應該空
                            {
                                strErrMsg = "The target presence wafer can't perform the undo.\t\t目標存在晶圓無法執行撤回.\r";
                                break;
                            }
                            if (strATarget[0] != string.Empty && strATarget[0][0] == 'T')
                            {
                                nTower1to16 = int.Parse(strATarget[0].Replace('T', ' '));
                                nTowerSlot1to200 = int.Parse(strATarget[1].Replace('S', ' '));
                                strSTK = strTarget;
                                rtbMessage.AppendText(string.Format("Check Tower{0} Slot{1:D2}.\t\t\t\t\t", nTower1to16, nTowerSlot1to200));
                                rtbMessage.AppendText(string.Format("檢查 塔{0} 層{1:D2}.\r", nTower1to16, nTowerSlot1to200));

                                int nStkIndx = (nTower1to16 - 1) / 4;
                                int nStkFace0to3 = (nTower1to16 - 1) % 4;
                                if (ListSTK[nStkIndx].GetMapDataOneTower(nStkFace0to3)[nTowerSlot1to200 - 1] != '1')//目標應該有
                                {
                                    strErrMsg = "The target doesn't exist wafer can't perform the undo.\t目標不存在晶圓無法執行撤回.\r";
                                    break;
                                }
                            }
                        }
                        else if (strASource[0] != string.Empty && strASource[0][0] == 'T')
                        {
                            //Wafer Out(Tower -> FOUP)
                            nTower1to16 = int.Parse(strASource[0].Replace('T', ' '));
                            nTowerSlot1to200 = int.Parse(strASource[1].Replace('S', ' '));
                            strSTK = strSource;
                            rtbMessage.AppendText(string.Format("Check Tower{0} Slot{1:D2}.\t\t\t\t\t", nTower1to16, nTowerSlot1to200));
                            rtbMessage.AppendText(string.Format("檢查 塔{0} 層{1:D2}.\r", nTower1to16, nTowerSlot1to200));

                            int nStkIndx = (nTower1to16 - 1) / 4;
                            int nStkFace0to3 = (nTower1to16 - 1) % 4;
                            if (ListSTK[nStkIndx].GetMapDataOneTower(nStkFace0to3)[nTowerSlot1to200 - 1] != '0')//目標應該空
                            {
                                strErrMsg = "The target presence wafer can't perform the undo.\t\t目標存在晶圓無法執行撤回.\r";
                                break;
                            }
                            if (strATarget[0] != string.Empty && strATarget[0][0] == 'P')
                            {
                                nStgBody = int.Parse(strATarget[0].Replace('P', ' '));
                                nStgSlot = int.Parse(strATarget[1].Replace('S', ' '));
                                strSTG = strTarget;
                                rtbMessage.AppendText(string.Format("Check Loadport{0} Slot{1:D2}.\t\t\t\t\t", nStgBody, nStgSlot));
                                rtbMessage.AppendText(string.Format("檢查 晶圓加載{0} 層{1:D2}.\r", nStgBody, nStgSlot));
                                if (ListSTG[nStgBody - 1].MappingData[nStgSlot - 1] != '1')//目標應該有
                                {
                                    strErrMsg = "The target doesn't exist wafer can't perform the undo.\t目標不存在晶圓無法執行撤回.\r";
                                    break;
                                }
                                else
                                {
                                    //FOUP內Wafer資料寫入，等等要送回Tower
                                    SWafer w = ListSTG[nStgBody - 1].Waferlist[nStgSlot - 1];
                                    w.WaferID_B = dt.Rows[i]["WaferID"].ToString();
                                    w.GradeID = dt.Rows[i]["WaferGrade"].ToString();
                                    w.LotID = dt.Rows[i]["LotID"].ToString();
                                }
                            }
                        }

                        if (strSTG == string.Empty || strSTK == string.Empty)
                        {
                            strErrMsg = "Conditions not met, can't perform the undo.\t\t\t條件未滿足無法執行撤回.\r";
                            break;
                        }
                        if (AssignmentTransferForTower(strSTG, strSTK) == false)
                        {
                            strErrMsg = "Sort Failure.\t\t\t\t\t\t排序失敗.\r";
                            break;
                        }
                        SpinWait.SpinUntil(() => false, 100);
                    }

                    if (strErrMsg != string.Empty)
                    {
                        this.BeginInvoke((EventHandler)delegate
                        {
                            rtbMessage.AppendText(strErrMsg);
                            rtbMessage.AppendText("Please restart the software.\t\t\t\t請重新啟動軟體.\r");
                            btnExit.Enabled = true;
                        });
                        return;
                    }
                    else
                    {
                        rtbMessage.AppendText("Press the Next button to start the process.\t\t\t按下Next按鈕開始流程.\r");
                    }
                    m_eStep = eTransferUndoStep.Start;
                }
                btnOK.Enabled = btnExit.Enabled = true;
            }
            catch (Exception ex) { WriteLog("<Exception>" + ex); }
        }
        private void DoStart()
        {
            try
            {
                rtbMessage.Clear();
                rtbMessage.AppendText("Transmitting.\r");
                rtbMessage.AppendText("傳送中.\r");

                foreach (I_Loadport stg in ListSTG)
                {
                    if (stg.Disable) continue;
                    if (stg.FoupExist && stg.StatusMachine == enumStateMachine.PS_Docked)
                    {
                        stg.OnUclmComplete += DoStartComplete;//註冊
                    }
                }

                if (btnStart_Click(null, null) == false)
                {
                    rtbMessage.Clear();
                    rtbMessage.AppendText(" Please restart the software.\r");
                    rtbMessage.AppendText(" 請重新啟動軟體.\r");
                }
            }
            catch (Exception ex) { WriteLog("<Exception>" + ex); }
        }
        private void DoStartComplete(object sender, LoadPortEventArgs e)
        {
            I_Loadport item = (I_Loadport)sender;
            item.OnUclmComplete -= DoStartComplete;//註銷註冊
            lock (this)
            {
                //檢查Loadport都完成Undocking
                foreach (I_Loadport stg in ListSTG)
                {
                    if (stg.Disable) continue;

                    if (stg.FoupExist && stg.StatusMachine == enumStateMachine.PS_Process)
                    {
                        return;//有未完成
                    }
                    if (stg.FoupExist && stg.StatusMachine != enumStateMachine.PS_ReadyToUnload && stg.StatusMachine != enumStateMachine.PS_Docked)
                    {
                        return;//有未完成
                    }
                }
                rtbMessage.Clear();
                rtbMessage.AppendText("Transmission complete.\r");
                rtbMessage.AppendText("傳送完成.\r");
                rtbMessage.AppendText("Press the Next button to continue.\r");
                rtbMessage.AppendText("按下Next按鈕繼續.\r");
                m_eStep = eTransferUndoStep.End;
            }
            btnOK.Enabled = btnExit.Enabled = true;
        }

        //  Loadport 註冊事件來更新 UI
        private void Loadport_FoupExistChenge(object sender, FoupExisteChangEventArgs e)
        {
            I_Loadport thenumUnit = sender as I_Loadport;
            if (thenumUnit.Disable) return;
            int index = thenumUnit.BodyNo - 1;
            m_guiloadportList[index].UpdataFoupExist(e.FoupExist);
        }
        private void Loadport_MappingComplete(object sender, LoadPortEventArgs e)
        {
            I_Loadport thenumUnit = sender as I_Loadport;
            if (thenumUnit.Disable) return;
            int index = thenumUnit.BodyNo - 1;
            string strMapData = e.MappingData;
            m_guiloadportList[index].UpdataMappingData(strMapData);
        }
        private void Loadport_StatusMachineChange(object sender, OccurStateMachineChangEventArgs e)
        {
            I_Loadport thenumUnit = sender as I_Loadport;
            if (thenumUnit.Disable) return;
            int index = thenumUnit.BodyNo - 1;
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
        private void Loadport_FoupIDChange(object sender, EventArgs e)
        {
            I_Loadport theLoadport = sender as I_Loadport;
            if (theLoadport.Disable) return;
            int nIndex = theLoadport.BodyNo - 1;
            m_guiloadportList[nIndex].FoupID = ListSTG[nIndex].FoupID;


            ListlblRfid[nIndex].Text = ListSTG[nIndex].FoupID;

            if (ListlblRfid[nIndex].Text == ListlblRfidRecord[nIndex].Text)
                ListlblRfid[nIndex].ForeColor = Color.SeaGreen;
            else
                ListlblRfid[nIndex].ForeColor = Color.Crimson;

        }
        private void Loadport_FoupTypeChange(object sender, string strName)
        {
            I_Loadport theLoadport = sender as I_Loadport;
            if (theLoadport.Disable) return;
            int nIndex = theLoadport.BodyNo - 1;
            m_guiloadportList[nIndex].InfoPadName = strName;
        }

        //  Loadport UI Button
        private void btnDock_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;
            I_Loadport stg = ListSTG[Loader.BodyNo - 1];
            if (stg.Disable || stg.FoupExist == false) return;

            if (stg.StatusMachine == enumStateMachine.PS_Clamped ||
                stg.StatusMachine == enumStateMachine.PS_Arrived ||
                stg.StatusMachine == enumStateMachine.PS_UnDocked ||
                stg.StatusMachine == enumStateMachine.PS_ReadyToLoad ||
                stg.StatusMachine == enumStateMachine.PS_ReadyToUnload)
            {
                stg.CLMP();
            }
            else if (stg.StatusMachine == enumStateMachine.PS_Docked)
            {
                stg.WMAP();
            }
        }
        private void btnUnDock_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;
            I_Loadport stg = ListSTG[Loader.BodyNo - 1];
            if (stg.Disable || stg.FoupExist == false) return;

            if (stg.StatusMachine == enumStateMachine.PS_Docked ||
                stg.StatusMachine == enumStateMachine.PS_Stop ||
                stg.StatusMachine == enumStateMachine.PS_Complete)
            {
                stg.UCLM();
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
        private void chkLoadportAFoupOn_Click(object sender, EventArgs e)//Simulate
        {
            chkFoupOn_Checked(guiLoadport1, e);
        }
        private void chkLoadportBFoupOn_Click(object sender, EventArgs e)//Simulate
        {
            chkFoupOn_Checked(guiLoadport2, e);

        }
        private void chkLoadportCFoupOn_Click(object sender, EventArgs e)//Simulate
        {
            chkFoupOn_Checked(guiLoadport3, e);

        }
        private void chkLoadportDFoupOn_Click(object sender, EventArgs e)//Simulate
        {
            chkFoupOn_Checked(guiLoadport4, e);
        }

        #region Loadport 選片機制


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

            if (e.SelectSlotNum.Count() <= 0)//Stock只是選LP位置還不知道送去哪個Tower位置
            {
                if (m_eTransferMode == enumTransferMode.Stock && m_eTransferModeType == enumTransferModeType.WaferInOut)//BWS
                {
                    //cbxGradeName.SelectedIndex = -1;
                    //dgvGrade.DataSource = null;
                    foreach (var lp in m_guiloadportList)
                    {
                        if (lp.BodyNo == guiLoadport.BodyNo)
                        {
                            List<int> listSetectIndx = lp.GetSelectSlotIndxForStocker();
                            if (listSetectIndx != null && listSetectIndx.Count != 0)//有選擇
                            {
                                //lblWaferIDSearch.Visible = lp.MapData[listSetectIndx[0]] == '0';//選擇的是空位
                                //tbxWaferIDSearch.Visible = lp.MapData[listSetectIndx[0]] == '0';//選擇的是空位
                                //tlpGradeSelect.Visible = true;//Stocker
                            }
                            continue;
                        }
                        lp.ResetAllSelectSlot();
                    }
                }
                return;//防呆
            }

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
                    clsSelectWaferInfo temp = new clsSelectWaferInfo(lp, -1, slot);
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

        private void ClearSelectWafer()
        {
            for (int i = 0; i < ListSTG.Count; i++)
            {
                if (ListSTG[i].StatusMachine == enumStateMachine.PS_Process) continue;
                m_guiloadportList[i].EnableUISelectPutWaferFlag(false);
                m_guiloadportList[i].ResetUpdateMappingData();
            }

            for (int i = 0; i < ListSTK.Count; i++)
            {
                if (ListSTK[i].StatusMachine == RorzeUnit.Class.Stock.Enum.enumStateMachine.PS_Process) continue;
                //m_guiTowerList[i].EnableUISelectPutWaferFlag(false);
                m_guiTowerList[i].ResetUpdateMappingData();
            }

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

            //dgvGrade.DataSource = null;//Grade
            //cbxGradeName.SelectedIndex = -1;//Grade
            //tbxWaferIDSearch.Text = string.Empty;
            //tlpGradeSelect.Visible = false;//清除選擇
        }
        private void btnUIPickWaferAllClear_Click(object sender, EventArgs e)
        {
            ClearSelectWafer();
        }


        #endregion

        //Tower
        void OnStock_MappingComplete(object sender, string strMappingData)//800or1600片更新
        {
            I_Stock unit = sender as I_Stock;
            if (unit.Disable) return;
            GUITower tower = m_guiTowerList[unit.BodyNo - 1];
            tower.UpdataStockMappingData(strMappingData);
        }
        void OnSlotLabelMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            GUITower tower = sender as GUITower;
            int nBody = tower.BodyNo;
            I_Stock stock = ListSTK[nBody - 1];
            int nFaceIndx0to3 = e.ColumnIndex;
            int nTower1to16 = nFaceIndx0to3 + stock.TowerCount * (nBody - 1) + 1;
            int nTowerSlot = stock.TheTowerSlotNumber - e.RowIndex;
            string strName = string.Format("T{0:D2} S{1:D3}", nTower1to16, nTowerSlot);
            SWafer wafer = ListSTK[nBody - 1].GetWafer(nTower1to16, nTowerSlot);

            lblStockerSlot_Pos.Text = strName;
            if (wafer != null)
            {
                lblStockerSlot_FrontID_value.Text = wafer.WaferID_F;
                lblStockerSlot_BackID_value.Text = wafer.WaferID_B;
                lblStockerSlot_Grade_value.Text = wafer.GradeID;
                lblStockerSlot_LotID_value.Text = wafer.LotID;
            }

        }
        void OnSlotLabelMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            //GUITower tower = sender as GUITower;
            //tower.SelectSlot(nIndex0to799, false);
        }


        /// <summary>
        /// 分配Tower與Loadport傳送資訊
        /// </summary>
        /// <param name="strTowerPos">ex:T01-S195</param>
        /// <returns></returns>
        private bool AssignmentTransferForTower(string strSTG, string strTowerPos)//範例:P01-02   T01-S195
        {
            bool bSuccess = false;//分配成功
            try
            {
                bool bShowNotchAngle = (m_eTransferMode == enumTransferMode.Notch);

                //要分配去Grade
                int nStg1to8 = int.Parse(strSTG.Split('-')[0].Replace("P", ""));
                int nStgSlot1to25 = int.Parse(strSTG.Split('-')[1].Replace("S", ""));

                I_Loadport lp = ListSTG[nStg1to8 - 1];
                GUILoadport guiLP = m_guiloadportList[nStg1to8 - 1];
                bool bSelectWaferExist = lp.MappingData[nStgSlot1to25 - 1] != '0';//選擇的位置有沒有wafer

                //範例T01-S195
                int nTower1to16 = int.Parse(strTowerPos.Split('-')[0].Replace("T", ""));
                int nSlot1to200 = int.Parse(strTowerPos.Split('-')[1].Replace("S", ""));
                GUITower guiTower = m_guiTowerList[(nTower1to16 - 1) / 4];

                clsSelectWaferInfo temp;
                //LP送去Tower
                if (bSelectWaferExist)
                {
                    temp = new clsSelectWaferInfo(lp.BodyNo, -1, nStgSlot1to25 - 1);//注意第0個
                    temp.SetTargetSlotIdx(nSlot1to200 - 1);
                    temp.SetTargetTowerNum(nTower1to16);
                    temp.SetGradeName(m_strGradeName);

                    string strFromName = (char)(64 + temp.SourceLpBodyNo) + (temp.SourceSlotIdx + 1).ToString("D2");
                    string strToName = string.Format("T{0:D2}S{1:D3}", temp.TargetTowerNum, temp.TargetSlotIdx + 1);

                    guiLP.ResetSlotSelectFlag(strToName, temp.SourceSlotIdx);

                    guiTower.PlaceWaferInLoadport(strFromName, (nTower1to16 - 1) % 4, temp.TargetSlotIdx, bShowNotchAngle ? m_dNotchAngle : -1);
                }
                //Tower送去LP
                else
                {
                    temp = new clsSelectWaferInfo(-1, nTower1to16, nSlot1to200 - 1);
                    temp.SetTargetSlotIdx(nStgSlot1to25 - 1);//注意第0個
                    temp.SetTargetLpBodyNo(lp.BodyNo);

                    string strFromName = string.Format("T{0:D2}S{1:D3}", temp.SourceTowerNum, temp.SourceSlotIdx + 1);
                    string strToName = (char)(64 + temp.TargetLpBodyNo) + (temp.TargetSlotIdx + 1).ToString("D2");

                    guiTower.ResetSlotSelectFlag(strToName, (nTower1to16 - 1) % 4, temp.SourceSlotIdx);

                    guiLP.UserSelectPlaceWaferInLoadport(strFromName, temp.TargetSlotIdx, bShowNotchAngle ? m_dNotchAngle : -1);
                }
                if (bShowNotchAngle) temp.SetNotchAngle(m_dNotchAngle);
                m_QueWaferJob.Enqueue(temp);//job
                bSuccess = true;
            }
            catch (Exception ex) { WriteLog("<Exception>" + ex); }
            return bSuccess;
        }

        enumTransferMode m_eTransferMode = enumTransferMode.Stock;
        enumTransferModeType m_eTransferModeType = enumTransferModeType.WaferInOut;
        double m_dNotchAngle = 0;
        string m_strGradeName;
        string m_strRecipe = string.Empty;
        private bool btnStart_Click(object sender, EventArgs e)
        {
            bool bSuccess = false;
            try
            {
                m_Transfer.InitalStopFlag();
                clsSelectWaferInfo selectInfo = null;
                if (false == m_QueWaferJob.TryPeek(out selectInfo))
                {
                    new frmMessageBox(string.Format("Please select wafer."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return false;
                }
                ConcurrentQueue<clsSelectWaferInfo> SelectWaferInfoQueue = m_QueWaferJob;

                #region  ============ Safety ============    

                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return false;
                }

                if (m_Alarm.CurrentAlarm != null && m_Alarm.CurrentAlarm.Count > 0)
                {
                    if (m_Alarm.IsOnlyWarning() == false)//如果是有警告
                    {
                        new frmMessageBox("There are uncleared abnormalities, please confirm the machine status first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return false;
                    }
                }

                foreach (I_Robot rb in ListTRB)
                {
                    if (rb != null && rb.Disable == false && rb.IsOrgnComplete == false)
                    {
                        new frmMessageBox(string.Format("Robot is not orgned."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return false;
                    }
                }

                foreach (clsSelectWaferInfo clsSelectWaferInfo in SelectWaferInfoQueue.ToArray())
                {
                    if (clsSelectWaferInfo.SourceLpBodyNo > 0)
                    {
                        if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].FoupExist == false)
                        {
                            new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return false;
                        }
                        if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].StatusMachine == enumStateMachine.PS_Process)
                        {
                            new frmMessageBox("Loadport StatusMachine is PS_Process.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return false;
                        }
                        if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Loadport StatusMachine is not PS_Docked.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return false;
                        }
                        if (ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].FoupID.Trim().Length <= 0)
                        {
                            new frmMessageBox("Loadport foup id is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return false;
                        }
                        foreach (char item in ListSTG[clsSelectWaferInfo.SourceLpBodyNo - 1].MappingData)
                        {
                            if (item != '0' && item != '1')
                            {
                                new frmMessageBox("Loadport mapping error.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return false;
                            }
                        }
                    }

                    if (clsSelectWaferInfo.TargetLpBodyNo > 0)
                    {
                        if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].FoupExist == false)
                        {
                            new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return false;
                        }
                        else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].StatusMachine == enumStateMachine.PS_Process)
                        {
                            new frmMessageBox("Loadport StatusMachine is PS_Process.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return false;
                        }
                        else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].StatusMachine != enumStateMachine.PS_Docked)
                        {
                            new frmMessageBox("Loadport StatusMachine is not PS_Docked.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return false;
                        }
                        else if (ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].FoupID.Trim().Length <= 0)
                        {
                            new frmMessageBox("Loadport foup id is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                            return false;
                        }
                        foreach (char item in ListSTG[clsSelectWaferInfo.TargetLpBodyNo - 1].MappingData)
                        {
                            if (item != '0' && item != '1')
                            {
                                new frmMessageBox("Loadport mapping error.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                return false;
                            }
                        }
                    }
                }

                //只要EQ能選擇Recipe或是有OCR就要考慮Grouprecipe
                if (GParam.theInst.IsAllOcrDisable() == false)
                {
                    m_strRecipe = "NoOCR";
                    if (m_dbGrouprecipe.GetRecipeGroupList.ContainsKey(m_strRecipe) == false)
                    {
                        new frmMessageBox(string.Format("Recipe is empty or wrong."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return false;
                    }
                }

                #endregion

                bSuccess = m_Transfer.CreateJob(ref m_QueWaferJob, false, m_strRecipe, m_strGradeName);//RecoverUndo

                ClearSelectWafer();
            }
            catch (Exception ex) { WriteLog("<Exception>" + ex); }
            return bSuccess;
        }

        private void btnSTG1read_Click(object sender, EventArgs e)
        {
            string strFoupID = "ReadFail";
            if (GParam.theInst.IsSimulate == false)
                strFoupID = ListRFID[0].ReadMID();
            ListSTG[0].FoupID = strFoupID;//讀失敗字串是ReadFail
        }

        private void btnSTG2read_Click(object sender, EventArgs e)
        {
            string strFoupID = "ReadFail";
            if (GParam.theInst.IsSimulate == false)
                strFoupID = ListRFID[1].ReadMID();
            ListSTG[1].FoupID = strFoupID;//讀失敗字串是ReadFail
        }

        private void btnSTG3read_Click(object sender, EventArgs e)
        {
            string strFoupID = "ReadFail";
            if (GParam.theInst.IsSimulate == false)
                strFoupID = ListRFID[2].ReadMID();
            ListSTG[2].FoupID = strFoupID;//讀失敗字串是ReadFail
        }

        private void btnSTG4read_Click(object sender, EventArgs e)
        {
            string strFoupID = "ReadFail";
            if (GParam.theInst.IsSimulate == false)
                strFoupID = ListRFID[3].ReadMID();
            ListSTG[3].FoupID = strFoupID;//讀失敗字串是ReadFail
        }
    }
}
