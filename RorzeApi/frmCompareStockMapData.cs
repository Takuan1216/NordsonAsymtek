using RorzeApi.Class;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace RorzeApi
{
    public partial class frmCompareStockMapData : Form
    {
        private bool m_bSimulate;
        private SStockerSQL m_DataBase;
        private List<I_Stock> m_ListSTK;

        DataTable dt;

        private enum enumStep { StockWamp, CheckData, Finish };

        private bool m_bCompareStockData = false;

        private List<Panel> m_DicPanel = new List<Panel>();
        private List<Label> m_DicLabel = new List<Label>();
        private List<TableLayoutPanel> m_TbLayout = new List<TableLayoutPanel>();
        private List<bool> m_DicMapOK = new List<bool>();

        private SInterruptOneThread _exeTowerMap;

        SLogger m_logger = SLogger.GetLogger("ExecuteLog");

        public frmCompareStockMapData(List<I_Stock> listSTK, SStockerSQL dataBase, bool bSimulate)
        {
            InitializeComponent();
            this.ControlBox = false;

            m_ListSTK = listSTK;
            m_DataBase = dataBase;
            m_bSimulate = bSimulate;

            m_DicPanel.Add(pnlTower01);
            m_DicPanel.Add(pnlTower02);
            m_DicPanel.Add(pnlTower03);
            m_DicPanel.Add(pnlTower04);
            m_DicPanel.Add(pnlTower05);
            m_DicPanel.Add(pnlTower06);
            m_DicPanel.Add(pnlTower07);
            m_DicPanel.Add(pnlTower08);
            m_DicPanel.Add(pnlTower09);
            m_DicPanel.Add(pnlTower10);
            m_DicPanel.Add(pnlTower11);
            m_DicPanel.Add(pnlTower12);
            m_DicPanel.Add(pnlTower13);
            m_DicPanel.Add(pnlTower14);
            m_DicPanel.Add(pnlTower15);
            m_DicPanel.Add(pnlTower16);

            m_DicLabel.Add(lbStatus_Tower01);
            m_DicLabel.Add(lbStatus_Tower02);
            m_DicLabel.Add(lbStatus_Tower03);
            m_DicLabel.Add(lbStatus_Tower04);
            m_DicLabel.Add(lbStatus_Tower05);
            m_DicLabel.Add(lbStatus_Tower06);
            m_DicLabel.Add(lbStatus_Tower07);
            m_DicLabel.Add(lbStatus_Tower08);
            m_DicLabel.Add(lbStatus_Tower09);
            m_DicLabel.Add(lbStatus_Tower10);
            m_DicLabel.Add(lbStatus_Tower11);
            m_DicLabel.Add(lbStatus_Tower12);
            m_DicLabel.Add(lbStatus_Tower13);
            m_DicLabel.Add(lbStatus_Tower14);
            m_DicLabel.Add(lbStatus_Tower15);
            m_DicLabel.Add(lbStatus_Tower16);

            m_TbLayout.Add(tbLayoutTower01);
            m_TbLayout.Add(tbLayoutTower02);
            m_TbLayout.Add(tbLayoutTower03);
            m_TbLayout.Add(tbLayoutTower04);
            m_TbLayout.Add(tbLayoutTower05);
            m_TbLayout.Add(tbLayoutTower06);
            m_TbLayout.Add(tbLayoutTower07);
            m_TbLayout.Add(tbLayoutTower08);
            m_TbLayout.Add(tbLayoutTower09);
            m_TbLayout.Add(tbLayoutTower10);
            m_TbLayout.Add(tbLayoutTower11);
            m_TbLayout.Add(tbLayoutTower12);
            m_TbLayout.Add(tbLayoutTower13);
            m_TbLayout.Add(tbLayoutTower14);
            m_TbLayout.Add(tbLayoutTower15);
            m_TbLayout.Add(tbLayoutTower16);

            //  消失頁籤
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);
            ConstructSlotStatus();

            //  Tower      
            foreach (I_Stock item in listSTK)
            {
                if (item.Disable) continue;
                item.OnMappingComplete -= _tower_OnMappingComplete;
                item.OnMappingComplete += _tower_OnMappingComplete;

                item.OnMappingError -= _tower_OnMappingError;
                item.OnMappingError += _tower_OnMappingError;
            }

            _exeTowerMap = new SInterruptOneThread(RunMap);

            if (GParam.theInst.FreeStyle)
            {
                btnAbort.BackColor = btnDB_Modify.BackColor = btnBack.BackColor =
                    btnStockerMapping.BackColor = btnUsingDataBase.BackColor = GParam.theInst.ColorButton;

                btnAbort.ForeColor = btnDB_Modify.ForeColor = btnBack.ForeColor =
                         btnStockerMapping.ForeColor = btnUsingDataBase.ForeColor = Color.Black;



                lblTitle.BackColor = label3.BackColor = GParam.theInst.ColorOrange3;
                lblTitle.ForeColor = label3.ForeColor = Color.Black/*GParam.theInst.ColorOrange5*/;

                btnUsingDataBase.Image = RorzeApi.Properties.Resources._32_done_;
                btnStockerMapping.Image = RorzeApi.Properties.Resources._32_close_;
                btnDB_Modify.Image = RorzeApi.Properties.Resources._32_next_;
                btnAbort.Image = RorzeApi.Properties.Resources._32_cancel_;

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

        private void _tower_OnMappingComplete(object sender, RorzeUnit.Class.Stock.Evnt.TowerEventArgs.TowerGMAP_EventArgs e)
        {
            I_Stock unit = sender as I_Stock;
            if (unit.Disable) return;

            int index = unit.BodyNo - 1;
            int nT = e.FaceIndx;

            //011 021 031 041
            //051 061 071 081
            //091 101 111 121
            //131 141 151 161

            //01 02 03 04
            //05 06 07 08
            //09 10 11 12
            //13 14 15 16       

            ChangeMessage(SystemColors.ControlText, m_DicLabel[index * 4 + nT], GParam.theInst.GetLanguage("Wafer Search Completed."));

            m_DicMapOK[index * 4 + nT] = true;

            m_TbLayout[index * 4 + nT].BackColor = GParam.theInst.ColorReadyGreen;

            string str = string.Format("{0}{1:D2} \t {2} = {3}", GParam.theInst.GetLanguage("Tower"), index * 4 + nT + 1, GParam.theInst.GetLanguage("Mapping Data"), unit.GetMapDataOneTower(nT));

            AddMessage(Color.Blue, str/*"Tower" + String.Format("{0:00}", (index * 4 + nT + 1)) + " Mapping Data=" + unit.GetMapDataOneTower(nT)*/);
        }

        private void _tower_OnMappingError(object sender, EventArgs e)
        {
            I_Stock unit = sender as I_Stock;
            if (unit.Disable) return;

            int index = unit.BodyNo - 1;

            for (int nT = 0; nT < 4; nT++)
            {
                ChangeMessage(SystemColors.ControlText, m_DicLabel[index * 4 + nT], GParam.theInst.GetLanguage("Wafer Search Error."));
                m_DicMapOK[index * 4 + nT] = false;
                m_TbLayout[index * 4 + nT].BackColor = Color.Red;
            }
        }

        private void RunMap()
        {
            try
            {
                //確認那些塔需要Mapping
                m_DicMapOK = new List<bool>();
                for (int i = 0; i < m_ListSTK.Count; i++)//0~3
                {
                    for (int j = 0; j < m_ListSTK[i].TowerCount; j++)//0~3
                    {
                        if (m_ListSTK[i].TowerEnable(j) == false)
                        {
                            m_DicMapOK.Add(true);
                            m_DicPanel[4 * i + j].Visible = false;
                        }
                        else
                        {
                            m_DicMapOK.Add(false);
                        }
                    }
                }
                //顯示目前DB記錄的資料
                this.BeginInvoke(new Action(() =>
                {
                    layoutMessage.Controls.Clear();
                    dt = m_DataBase.SelectSlotStatus();
                    for (int i = 0; i < GParam.theInst.TowerCount; i++)//16
                    {
                        if (m_DicMapOK[i] == true) continue;//需要mapping才要比對
                        string strMessage = "";
                        DataTable dtTower = m_DataBase.SelectSlotStatus(i + 1);
                        foreach (DataRow item in dtTower.Rows) { strMessage += item["WaferStatus"].ToString(); }
                        string str = string.Format("{0}{1:D2} \t {2} = {3}", GParam.theInst.GetLanguage("Tower"), i + 1, GParam.theInst.GetLanguage("DataBase Data"), strMessage);
                        AddMessage(Color.Green, str);
                    }
                }));

                enumStep step = enumStep.StockWamp;
                //if (new frmMessageBox("Do not perform a Stocker mapping to use the database records.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == System.Windows.Forms.DialogResult.Yes)
                //{
                //    for (int i = 0; i < GParam.theInst.TowerCount; i++)//16
                //    {
                //        string strMessage = "";
                //        DataTable dtTower = m_DataBase.SelectSlotStatus(i + 1);
                //        foreach (DataRow item in dtTower.Rows) { strMessage += item["WaferStatus"].ToString(); }
                //        //把DB的資料先寫給Mapping資料
                //        m_ListSTK[i / 4].SetMapDataTower(i % 4, strMessage);
                //    }
                //    this.DialogResult = DialogResult.OK;
                //    this.Close();
                //    return;
                //}

                while (step != enumStep.Finish)
                {
                    SpinWait.SpinUntil(() => false, 1);
                    switch (step)
                    {
                        case enumStep.StockWamp:
                            for (int i = 0; i < m_ListSTK.Count; i++)//0~3stk
                            {
                                if (m_ListSTK[i].Disable) continue;

                                int nTowerCount = m_ListSTK[i].TowerCount;

                                for (int j = 0; j < nTowerCount; j++)//0~3tower
                                {
                                    int nTower0to15 = i * nTowerCount + j;
                                    ChangeMessage(Color.Red, m_DicLabel[nTower0to15], GParam.theInst.GetLanguage("Search Stock Wafers..."));
                                    m_TbLayout[nTower0to15].BackColor = GParam.theInst.ColorWaitYellow;
                                }

                                m_ListSTK[i].WMAP();

                                SpinWait.SpinUntil(() => false, 100);
                            }
                            step = enumStep.CheckData;
                            break;
                        case enumStep.CheckData:
                            if (ConfirmWmapCompleted())//等完成
                            {
                                m_bCompareStockData = true;
                                foreach (I_Stock stk in m_ListSTK)
                                {
                                    if (stk.Disable) continue;

                                    for (int i = 0; i < stk.TowerCount; i++)//0~3四面塔
                                    {

                                        if (stk.TowerEnable(i) == false) continue;

                                        string strComparisonResults = string.Empty;
                                        int nTower1to16 = (stk.BodyNo - 1) * stk.TowerCount + i + 1;
                                        DataTable dbTower = m_DataBase.SelectSlotStatus(nTower1to16);
                                        string strGmap = stk.GetMapDataOneTower(i);
                                        for (int j = 0; j < stk.TheTowerSlotNumber; j++)//0~199 slot
                                        {
                                            string strWaferStatus = dbTower.Rows[j]["WaferStatus"].ToString();

                                            if (strGmap[j].ToString() == strWaferStatus)//與MAPPING匹配
                                            {
                                                strComparisonResults += strWaferStatus;
                                                if (strWaferStatus == "1")
                                                {
                                                    string strLotID = dbTower.Rows[j]["LotID"].ToString();
                                                    string strM12ID = dbTower.Rows[j]["M12ID"].ToString();
                                                    string strWaferID = dbTower.Rows[j]["WaferID"].ToString();
                                                    string strWaferGrade = dbTower.Rows[j]["WaferGrade"].ToString();
                                                    SWafer wafer = stk.GetWafer(nTower1to16, j + 1);
                                                    wafer.FoupID = strLotID;
                                                    wafer.WaferID_F = strM12ID;
                                                    wafer.WaferID_B = strWaferID;
                                                    wafer.GradeID = strWaferGrade;
                                                    wafer.LotID = strLotID;
                                                }
                                            }
                                            else//不匹配
                                            {
                                                strComparisonResults += '?';
                                                m_bCompareStockData = false;
                                            }
                                        }

                                        if (strComparisonResults.Contains('?'))
                                        {
                                            string str = string.Format("{0}{1:D2} \t {2} = {3}", GParam.theInst.GetLanguage("Tower"), nTower1to16, GParam.theInst.GetLanguage("Comparison"), strComparisonResults);
                                            AddCheckDataMessage(Color.Red, str);
                                        }
                                    }
                                }
                                UpDataSlotStatus();//tabpage2
                                CheckMappingData(false);//tabpage2
                                step = enumStep.Finish;
                            }
                            break;
                    }
                }



                if (m_bCompareStockData)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        btnDB_Modify.Enabled = true;//需要tabpage2修正
                        btnAbort.Enabled = true;
                        tlpModify.Visible = true;//要給人選要重新map或中止
                    }));
                }

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private bool ConfirmWmapCompleted()
        {
            bool bFinish = true;
            for (int i = 0; i < GParam.theInst.TowerCount; i++)
            {
                if (m_DicMapOK[i] == false) bFinish = false;
            }
            return bFinish;
        }

        //----------------------------------------------
        private void btnAbort_Click(object sender, EventArgs e)
        {
            //if (m_bSimulate)
            //    m_bCompareStockData = true;
            this.DialogResult = m_bCompareStockData ? DialogResult.OK : DialogResult.Cancel;
            this.Close();
        }
        private void frmCompareStockMapData_Load(object sender, EventArgs e)
        {
            lblTitle.Visible = panel1.Visible = false;

            //確認那些塔需要Mapping
            m_DicMapOK = new List<bool>();
            for (int i = 0; i < m_ListSTK.Count; i++)//0~3
            {
                for (int j = 0; j < m_ListSTK[i].TowerCount; j++)//0~3
                {
                    if (m_ListSTK[i].TowerEnable(j) == false)
                    {
                        m_DicMapOK.Add(true);
                        m_DicPanel[4 * i + j].Visible = false;
                    }
                    else
                    {
                        m_DicMapOK.Add(false);
                    }
                }
            }
            //顯示目前DB記錄的資料
            this.BeginInvoke(new Action(() =>
            {
                layoutMessage.Controls.Clear();
                dt = m_DataBase.SelectSlotStatus();
                for (int i = 0; i < GParam.theInst.TowerCount; i++)//16
                {
                    if (m_DicMapOK[i] == true) continue;//需要mapping才要比對
                    string strMessage = "";
                    DataTable dtTower = m_DataBase.SelectSlotStatus(i + 1);
                    foreach (DataRow item in dtTower.Rows) { strMessage += item["WaferStatus"].ToString(); }
                    string str = string.Format("{0}{1:D2} \t {2} = {3}", GParam.theInst.GetLanguage("Tower"), i + 1, GParam.theInst.GetLanguage("DataBase Data"), strMessage);
                    AddMessage(Color.Green, str);
                }

                if (GParam.theInst.SoftwareStartupTowerMapping)
                {
                    btnStockerMapping.PerformClick();
                }
            }));
        }

        //----------------------------------------------
        private delegate void UpdateUI(Color color, Label LB, string str);
        public void ChangeMessage(Color color, Label LB, string strMsg)
        {
            if (InvokeRequired)
            {
                UpdateUI dlg = new UpdateUI(ChangeMessage);
                this.BeginInvoke(dlg, color, LB, strMsg);
            }
            else
            {
                LB.Text = strMsg;
                LB.ForeColor = color;
                this.Refresh();
            }
        }

        private List<Label> lblList = new List<Label>();

        private delegate void dlgAddMessage(Color color, string strMsg);
        public void AddMessage(Color color, string strMsg)
        {
            if (InvokeRequired)
            {
                dlgAddMessage dlg = new dlgAddMessage(AddMessage);
                this.Invoke(dlg, color, strMsg);
            }
            else
            {
                Label lbl = new Label();
                lbl.Width = strMsg.Length * 8;
                //lbl.Dock = DockStyle.Top;
                //lbl.BackColor = Color.AliceBlue;
                lbl.Font = new Font("微軟正黑體", 9, FontStyle.Bold);
                lbl.Margin = new System.Windows.Forms.Padding(0);
                lbl.Text = strMsg;
                lbl.ForeColor = color;

                layoutMessage.Controls.Add(lbl);

                if (layoutMessage.VerticalScroll.Visible)
                    layoutMessage.VerticalScroll.Value = layoutMessage.VerticalScroll.Maximum;

                this.Refresh();
            }
        }
        public void AddCheckDataMessage(Color color, string strMsg)
        {
            //if (lblList.Count == GParam.theInst.SingleTowerCount -1)
            {
                if (InvokeRequired)
                {
                    dlgAddMessage dlg = new dlgAddMessage(AddCheckDataMessage);
                    this.BeginInvoke(dlg, color, strMsg);
                }
                else
                {
                    //richTextBox1.SelectionColor = color;
                    //richTextBox1.AppendText(strMsg + Environment.NewLine);

                    Label lbl = new Label();
                    lbl.Font = new Font("微軟正黑體", 9, FontStyle.Bold);
                    //lbl.Width = strMsg.Length * 8;
                    lbl.BackColor = Color.AliceBlue;
                    lbl.Dock = DockStyle.Fill;
                    lbl.Margin = new System.Windows.Forms.Padding(0);
                    lbl.Text = strMsg;
                    lbl.ForeColor = color;



                    lblList.Add(lbl);

                    layoutMessage.Controls.AddRange(lblList.ToArray());

                    lblList.Clear();

                    if (layoutMessage.VerticalScroll.Visible)
                        layoutMessage.VerticalScroll.Value = layoutMessage.VerticalScroll.Maximum;

                    this.Refresh();
                }
            }
        }

        #region ========== SlotStatus page ==========
        /// <summary>
        /// 建構DataGridView
        /// </summary>
        private void ConstructSlotStatus()
        {
            try
            {
                dgvSlotStatus.ColumnCount = GParam.theInst.TowerCount;

                for (int i = 0; i < dgvSlotStatus.ColumnCount; i++)
                {
                    dgvSlotStatus.Columns[i].HeaderText = string.Format("Tower{0:D2}", i + 1);
                    dgvSlotStatus.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                dgvSlotStatus.Rows.Clear();
                dgvSlotStatus.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

                for (int i = 0; i < GParam.theInst.SingleTowerCount; i++)
                {
                    dgvSlotStatus.Rows.Add();
                    dgvSlotStatus.Rows[i].Height = 77;
                }

                dgvSlotStatus.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                //dgvSlotStatus.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                //for (int k = 0; k < dgvSlotStatus.RowCount; k++)
                //{
                //    for (int j = 0; j < dgvSlotStatus.ColumnCount; j++)
                //    {
                //        dgvSlotStatus.Rows[k].Cells[j].Value = "T" + String.Format("{0:00}", j + 1) + "-S" + String.Format("{0:000}", dgvSlotStatus.RowCount - k);
                //    }
                //}
                dgvSlotStatus.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvSlotStatus.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopCenter;
                dgvSlotStatus.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }
            catch
            {

            }
        }
        /// <summary>
        /// 點選DataGridView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvSlotStatus_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                string[] strArray;
                if (dgvSlotStatus.Rows[dgvSlotStatus.CurrentRow.Index].Cells[dgvSlotStatus.CurrentCell.ColumnIndex].Style.BackColor == Color.LightBlue)
                {
                    strArray = dgvSlotStatus.Rows[dgvSlotStatus.CurrentRow.Index].Cells[dgvSlotStatus.CurrentCell.ColumnIndex].Value.ToString().Replace("\r\n", " ").Split(' ');

                    SlotName.Text = strArray[0].ToString();
                    WaferIDData.Text = strArray[1].ToString();
                    WaferGradeData.Text = strArray[2].ToString();
                    M12IDData.Text = strArray[3].ToString();
                    LotIDData.Text = strArray[4].ToString();
                    //VCLData.Text = strArray[5].ToString();
                    btnSlotStausUpdate.Enabled = true;
                }
                else
                {
                    strArray = dgvSlotStatus.Rows[dgvSlotStatus.CurrentRow.Index].Cells[dgvSlotStatus.CurrentCell.ColumnIndex].Value.ToString().Replace("\r\n", " ").Split(' ');

                    SlotName.Text = strArray[0].ToString();
                    WaferIDData.Text = "";
                    WaferGradeData.Text = "";
                    M12IDData.Text = "";
                    LotIDData.Text = "";
                    VCLData.Text = "";
                    btnSlotStausUpdate.Enabled = false;
                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 更新DataGridView
        /// </summary>
        private void UpDataSlotStatus()
        {
            try
            {
                DataTable dt = m_DataBase.SelectSlotStatus();

                for (int k = 0; k < dgvSlotStatus.RowCount; k++)
                {
                    for (int j = 0; j < dgvSlotStatus.ColumnCount; j++)
                    {
                        string strSlotName = "T" + String.Format("{0:00}", j + 1) + "-S" + String.Format("{0:000}", dgvSlotStatus.RowCount - k);

                        DataRow[] item = dt.Select("SlotName ='" + strSlotName + "'");

                        for (int ii = 0; ii < item.Length; ii++)
                        {
                            dgvSlotStatus.Rows[k].Cells[j].Value =
                                item[ii]["SlotName"].ToString() + "\r\n" +
                                item[ii]["WaferID"].ToString() + "\r\n" +
                                item[ii]["WaferGrade"].ToString() + "\r\n" +
                                item[ii]["M12ID"].ToString() + "\r\n" +
                                item[ii]["LotID"].ToString();
                            dgvSlotStatus.Rows[k].Cells[j].Style.BackColor = item[ii]["WaferStatus"].ToString() == "1" ? Color.LightBlue : Color.Empty;
                        }
                    }
                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 對帳將Mapping資料更新DB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMappingUpdate_Click(object sender, EventArgs e)
        {
            frmPassword myDlgPwd = new frmPassword();
            if (DialogResult.OK == myDlgPwd.ShowDialog() && myDlgPwd.GetPassWord == "1")
            {
                lsbMappingData.Items.Clear();
                CheckMappingData(true);
                UpDataSlotStatus();
            }
        }
        /// <summary>
        /// 修改單一內容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSlotStausUpdate_Click(object sender, EventArgs e)
        {
            //修改後要再MAPPING一次，因此這裡只改DB
            frmPassword myDlgPwd = new frmPassword();
            if (DialogResult.OK == myDlgPwd.ShowDialog() && myDlgPwd.GetPassWord == "1")
            {
                if (WaferIDData.Text == null || WaferIDData.Text == "")
                {
                    m_DataBase.UpdateSlotStatus(SlotName.Text);
                }
                else
                {
                    m_DataBase.UpdateSlotStatus(SlotName.Text, WaferIDData.Text, WaferGradeData.Text, M12IDData.Text, LotIDData.Text);
                }
                UpDataSlotStatus();
            }
        }
        /// <summary>
        /// Grade名稱搜尋在哪個SLOT
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSlotStatusQuery_Click(object sender, EventArgs e)
        {
            lsbSlotStatusGdnaQuerySlot.Items.Clear();
            string tmpStr = "";

            if (clbSlotStatusGdnaQuery.CheckedItems.Count < 1)
            {
                new frmMessageBox("Please select Grade first!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            DataGridStyleReset(dgvSlotStatus);        //DataGridView字型、顏色重置

            for (int i = 0; i < clbSlotStatusGdnaQuery.CheckedItems.Count; i++)
            {
                if (tmpStr == "")
                    tmpStr = "WaferGrade='" + clbSlotStatusGdnaQuery.CheckedItems[i] + "'";
                else
                    tmpStr = tmpStr + " or WaferGrade='" + clbSlotStatusGdnaQuery.CheckedItems[i] + "'";
            }

            DataTable dt = m_DataBase.SelectDistinctGradeSetList(SStockerSQL.enumGradeType.Status, tmpStr);

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dgvSlotStatus.RowCount; j++)
                    {
                        for (int k = 0; k < dgvSlotStatus.ColumnCount; k++)
                        {
                            if (dgvSlotStatus.Rows[j].Cells[k].Value.ToString().Substring(0, 8) == dt.Rows[i]["SlotName"].ToString())
                            {
                                dgvSlotStatus.Rows[j].Cells[k].Style.BackColor = Color.PaleGreen;
                                lsbSlotStatusGdnaQuerySlot.Items.Add(dt.Rows[i]["SlotName"]);
                            }
                        }
                    }
                }
                dgvSlotStatus.ClearSelection();
            }
            else
            {
                new frmMessageBox(string.Format("No result."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
            }
            dgvSlotStatus.Focus();
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
        }
        /// <summary>
        /// 移除選擇顏色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSlotGradeQueryReSet_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            DataGridStyleReset(dgvSlotStatus);        //DataGridView字型、顏色重置
        }
        /// <summary>
        /// 選擇匯入檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSlotStausInPath_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbSlotStausInPath;
            ListBox listBox = lsbSlotStausInData;
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                listBox.Items.Clear();
                tbx.Text = "";
                dialog.FileName = "";
                dialog.Filter = "文字檔 *.csv|*.csv";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    if (dialog.FileName == "") return;

                    tbx.Text = dialog.FileName;

                    FileStream filestream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read);
                    StreamReader sr = new StreamReader(filestream);
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        listBox.Items.Add(line);
                    }
                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 匯入csv檔
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSlotStausInData_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbSlotStausInPath;
            try
            {
                string strPath = tbx.Text;
                if (strPath == "")
                {
                    new frmMessageBox(string.Format("Please enter the file path."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                DataTable table = ConvertCsvToDataTable(strPath, true);

                if (m_DataBase.UpdateSlotStatus(table))//更新SQL
                {
                    UpDataSlotStatus(); //更新UI
                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 選擇匯出檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSlotStatusOutPath_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbSlotStatusOutPath;
            try
            {
                SaveFileDialog dialog = new SaveFileDialog();
                tbx.Text = "";
                dialog.FileName = "";
                dialog.Filter = "文字檔 *.csv|*.csv";
                dialog.ShowDialog();
                tbx.Text = dialog.FileName;
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 匯出TowerSlot所有資料
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSlotStatusOutFile_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbSlotStatusOutPath;
            DataTable dt = m_DataBase.SelectSlotStatus();
            try
            {
                string strPath = tbx.Text;
                if (strPath == "")
                {
                    new frmMessageBox(string.Format("Please enter the file path."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                ConvertDataTableTocsv(dt, strPath);
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }

        /// <summary>
        /// DataGridView字型、顏色重置
        /// </summary>
        /// <param name="objDGV"></param>
        private void DataGridStyleReset(DataGridView objDGV) //DataGridView字型、顏色重置
        {
            for (int i = 0; i < objDGV.Rows.Count; i++)
            {
                for (int j = 0; j < objDGV.Columns.Count; j++)
                {
                    //objDGV.Rows[i].Cells[j].Style.Font = new Font("微軟正黑體", 9);    //此行會造成很慢的狀態
                    objDGV.Rows[i].Cells[j].Style.BackColor = Color.White;
                }
            }
        }
        /// <summary>
        /// 匯出 CSV檔
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="path"></param>
        private void ConvertDataTableTocsv(DataTable dt, string path)
        {
            try
            {
                StreamWriter s = new StreamWriter(path, false);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    s.Write(dt.Columns[i]);
                    if (i < dt.Columns.Count - 1)
                    {
                        s.Write(",");
                    }
                }
                s.Write(s.NewLine);
                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (!Convert.IsDBNull(dr[i]))
                        {
                            string value = dr[i].ToString();
                            if (value.Contains(','))
                            {
                                value = String.Format("\"{0}\"", value);
                                s.Write(value);
                            }
                            else
                            {
                                s.Write(dr[i].ToString());
                            }
                        }
                        if (i < dt.Columns.Count - 1)
                        {
                            s.Write(",");
                        }
                    }
                    s.Write(s.NewLine);
                }
                s.Close();
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }

            new frmMessageBox(string.Format("Export Completion."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();

        }
        /// <summary>
        /// 匯入到 DATATABLE
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isFirstRowHeader"></param>
        /// <returns></returns>
        private DataTable ConvertCsvToDataTable(string path, bool isFirstRowHeader)
        {
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            string sql = @"SELECT * FROM [" + fileName + "]";

            using (OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                      ";Extended Properties=\"Text;HDR=" + header + "\""))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                DataTable dataTable = new DataTable();
                dataTable.Locale = CultureInfo.CurrentCulture;
                adapter.Fill(dataTable);
                return dataTable;
            }

        }
        #endregion


        /// <summary>
        /// 掃描STK的GMAP對照DB比對
        /// </summary>
        /// <param name="bGmapUpDataToDB"></param>
        private void CheckMappingData(bool bGmapUpDataToDB)
        {
            try
            {
                lsbMappingData.Items.Clear();
                foreach (I_Stock item in m_ListSTK)
                {
                    if (item.Disable) continue;
                    for (int i = 0; i < item.TowerCount; i++)
                    {
                        if (item.TowerEnable(i) == false) continue;

                        int nTower1to16 = item.TowerCount * (item.BodyNo - 1) + i + 1;

                        DataTable dt_OneTower = m_DataBase.SelectSlotStatus(nTower1to16);
                        for (int j = 0; j < item.TheTowerSlotNumber; j++)
                        {
                            char cGMAP = item.GetMapDataOneTower(i)[j];
                            char strDB = dt_OneTower.Rows[j]["WaferStatus"].ToString()[0];
                            if (cGMAP == strDB)
                            {
                                if (strDB == '1')
                                {
                                    SWafer wafer = item.GetWafer(nTower1to16, j + 1);
                                    wafer.FoupID = dt_OneTower.Rows[j]["LotID"].ToString();
                                    wafer.WaferID_F = dt_OneTower.Rows[j]["M12ID"].ToString();
                                    wafer.WaferID_B = dt_OneTower.Rows[j]["WaferID"].ToString();
                                    wafer.GradeID = dt_OneTower.Rows[j]["WaferGrade"].ToString();
                                }
                            }
                            else
                            {
                                string strSlotName = string.Format("T{0:D2}-S{1:D3}", nTower1to16, j + 1);
                                if (bGmapUpDataToDB)
                                {
                                    switch (cGMAP)
                                    {
                                        case '0':
                                            m_DataBase.UpdateSlotStatus(strSlotName);
                                            break;
                                        case '1':
                                            m_DataBase.UpdateSlotStatus(strSlotName, "UnKnow", "UnKnow", "UnKnow", "UnKnow");
                                            break;
                                    }
                                }
                                else
                                {
                                    string strMessage = strSlotName + " DataBase with MappingData are not Match";
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        lsbMappingData.Items.Add(strMessage);
                                        this.Refresh();
                                        btnMappingUpdate.Enabled = true;
                                    }));
                                }



                            }
                        }
                    }
                }
                /*DataTable dt = m_DataBase.SelectSlotStatus();
                for (int i = 0; i < m_ListSTK.Count; i++)
                {
                    if (m_ListSTK[i].Disable) continue;

                    for (int nT = 0; nT < m_ListSTK[i].TowerCount; nT++)
                    {
                        if (m_ListSTK[i].TowerEnable(nT) == false) continue;

                        for (int nSTC = 0; nSTC < GParam.theInst.SingleTowerCount; nSTC++)
                        {
                            string str1 = m_ListSTK[i].GetMapDataAll()[nT * GParam.theInst.SingleTowerCount + nSTC].ToString();
                            string str2 = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["WaferStatus"].ToString();

                            if (m_ListSTK[i].GetMapDataAll()[nT * GParam.theInst.SingleTowerCount + nSTC].ToString() == dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["WaferStatus"].ToString())
                            {
                                if (m_ListSTK[i].GetMapDataAll()[nT * GParam.theInst.SingleTowerCount + nSTC].ToString() == "1" && bUpData)
                                {
                                    m_ListSTK[i].GetWafer(((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1), nSTC + 1).FoupID = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["LotID"].ToString();
                                    m_ListSTK[i].GetWafer(((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1), nSTC + 1).WaferID_F = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["M12ID"].ToString();
                                    m_ListSTK[i].GetWafer(((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1), nSTC + 1).WaferID_B = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["WaferID"].ToString();
                                    m_ListSTK[i].GetWafer(((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1), nSTC + 1).GradeID = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["WaferGrade"].ToString();
                                }
                            }
                            else
                            {
                                string str = "Tower" + String.Format("{0:00}", ((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1)) + "-Slot" + String.Format("{0:000}", nSTC + 1) + " DataBase  with MappingData are not Match";
                                this.BeginInvoke(new Action(() =>
                                {
                                    lsbMappingData.Items.Add(str);
                                    this.Refresh();
                                    btnMappingUpdate.Enabled = true;
                                }));
                            }
                        }
                    }
                }*/
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        private void lsbMappingData_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                e.DrawBackground();
                Brush mybsh = Brushes.Black;
                // 判断是什么类型的标签
                if (lsbMappingData.Items[e.Index].ToString().IndexOf("not Match") != -1)
                {
                    mybsh = Brushes.Red;
                }
                // 焦点框
                e.DrawFocusRectangle();
                //文本 
                e.Graphics.DrawString(lsbMappingData.Items[e.Index].ToString(), e.Font, mybsh, e.Bounds, StringFormat.GenericDefault);
            }
        }

        /// <summary>
        /// 去第二頁
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDBCorrect_Click(object sender, EventArgs e)
        {
            tlpModify.Visible = false;
            //  消失頁籤
            TabControl_DB.SizeMode = TabSizeMode.Fixed;
            TabControl_DB.ItemSize = new Size(0, 1);
            tabControl1.SelectedTab = tabPage2;
        }
        /// <summary>
        /// 回第一頁
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBack_Click(object sender, EventArgs e)
        {
            btnDB_Modify.Enabled = btnAbort.Enabled = false;

            tabControl1.SelectedTab = tabPage1;

            //需要重新mapping       
            _exeTowerMap.Set();
        }



        /// <summary>
        /// 不要mapping用database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUsingDataBase_Click(object sender, EventArgs e)
        {
            lblTitle.Visible = panel1.Visible = true;
            tlpPassMapping.Visible = false;//按下後消失執行mapping選擇
            for (int i = 0; i < GParam.theInst.TowerCount; i++)//16
            {
                string strMessage = "";
                DataTable dtTower = m_DataBase.SelectSlotStatus(i + 1);
                foreach (DataRow item in dtTower.Rows) { strMessage += item["WaferStatus"].ToString(); }
                //把DB的資料先寫給Mapping資料
                m_ListSTK[i / 4].SetMapDataTower(i % 4, strMessage);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        /// <summary>
        /// 要執行Mapping
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStockerMapping_Click(object sender, EventArgs e)
        {
            lblTitle.Visible = panel1.Visible = true;
            tlpPassMapping.Visible = false;//按下後消失執行mapping選擇
            _exeTowerMap.Set();
        }
    }


}
