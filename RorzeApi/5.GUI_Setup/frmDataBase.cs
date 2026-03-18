using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RorzeApi.Class;
using RorzeUnit.Class;
using RorzeComm.Log;
using System.Collections.Generic;
using RorzeApi.GUI;
using RorzeUnit.Interface;
using System.Runtime.CompilerServices;
using System.IO;
using System.Data.OleDb;
using System.Globalization;
using static System.Net.WebRequestMethods;



namespace RorzeApi
{
    public partial class frmDataBase : Form
    {
        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制

        public delegate void DelegateMDITriggerShowMainform();
        public event DelegateMDITriggerShowMainform delegateMDITriggerShowMainform;

        public delegate void DelegateDemoStart(int sourceBodyNo, int targetBodyNo);
        public event DelegateDemoStart delegateDemoStart;
        #endregion
        #region ==========   Form Zoom   =============
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

        float frmX;             //當前窗體的寬度
        float frmY;             //當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        private SLogger m_logger = SLogger.GetLogger("ExecuteLog");

        private List<I_Stock> m_ListSTK;
        private SStockerSQL m_DataBase;
        private SProcessDB _accessDBlog;
        private SPermission m_userManager;//  管理LOGIN使用者權限
        private string _strUserName;//登入者名稱

        public frmDataBase(List<I_Stock> listSTK, SStockerSQL dataBase, SProcessDB db, SPermission userManager)
        {
            try
            {
                InitializeComponent();

                m_ListSTK = listSTK;
                m_DataBase = dataBase;
                _accessDBlog = db;
                m_userManager = userManager;

                ConstructGradeSet();
                UpDataGradeName();//建構子

                ConstructTowerSet();
                ConstructAreaSet();

                ConstructSlotStatus();
                UpDataSlotStatus();

                Construct_dgvParticleSet();
                UpData_dgvParticleSet();

                CheckMappingData(false);

                if (GParam.theInst.TowerQuerySet == true) rdbTowerQueryEab.Checked = true; else rdbTowerQueryDsb.Checked = true;
                if (GParam.theInst.AreaQuerySet == true) rdbAreaQueryEab.Checked = true; else rdbAreaQueryDsb.Checked = true;

                if (GParam.theInst.GradeHLLimit == true) rdbHLLimitSetEab.Checked = true; else rdbHLLimitSetDsb.Checked = true;
                if (GParam.theInst.GradeRFID3 == true) rdbRFID3SetEab.Checked = true; else rdbRFID3SetDsb.Checked = true;
            }
            catch (Exception ex)
            {
                m_logger.WriteLog(this.Name, ex);
            }
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }
        private void frmDataBase_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible)
                {

                }
                else
                {

                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 選擇換頁籤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabDataBase_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabDataBase.SelectedTab == tabPageSlotStatus)
            {
                UpDataSlotStatus();
            }
            else if (tabDataBase.SelectedTab == tabPageGradeSet)
            {
                ConstructGradeSet();
            }
            else if (tabDataBase.SelectedTab == tabPageTowerSet)
            {

            }
            else if (tabDataBase.SelectedTab == tabPageAreaSet)
            {

            }
            else if (tabDataBase.SelectedTab == tabPageParticle)
            {
                UpData_dgvParticleSet();
            }
        }

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
                                    wafer.LotID = dt_OneTower.Rows[j]["LotID"].ToString();
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
                    if (m_ListSTK[i].Disable == false)
                    {
                        for (int nT = 0; nT < GParam.theInst.TowerCount / m_ListSTK.Count; nT++)
                        {
                            if (m_ListSTK[i].TowerEnable(nT) == false) continue;

                            for (int nSTC = 0; nSTC < GParam.theInst.SingleTowerCount; nSTC++)
                            {
                                if (m_ListSTK[i].GetMapDataAll()[nT * GParam.theInst.SingleTowerCount + nSTC].ToString() == dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["WaferStatus"].ToString())
                                {
                                    if (m_ListSTK[i].GetMapDataAll()[nT * GParam.theInst.SingleTowerCount + nSTC].ToString() == "1" && bUpData)
                                    {
                                        m_ListSTK[i].GetWafer(((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1), nSTC + 1).FoupID = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["LotID"].ToString();
                                        m_ListSTK[i].GetWafer(((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1), nSTC + 1).WaferID_F = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["M12ID"].ToString();
                                        m_ListSTK[i].GetWafer(((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1), nSTC + 1).WaferID_B = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["WaferID"].ToString();
                                        m_ListSTK[i].GetWafer(((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1), nSTC + 1).GradeID = dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["WaferGrade"].ToString();
                                    }
                                    //AddCheckDataMessage(Color.Green, "Tower" + String.Format("{0:00}", ((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1)) + "-Slot" + String.Format("{0:000}", nSTC + 1) + " DataBase  with MappingData are Match");
                                }
                                else
                                {
                                    AddMessage(lsbMappingData, "Tower" + String.Format("{0:00}", ((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT + 1)) + "-Slot" + String.Format("{0:000}", nSTC + 1) + " DataBase  with MappingData are not Match");
                                    btnMappingUpdate.Enabled = true;
                                }
                            }
                        }
                    }
                }*/
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }


        #region ========== TowerSet page==========
        /// <summary>
        /// 建構DataGridView
        /// </summary>
        private void ConstructTowerSet()
        {
            try
            {
                dgvTowerSet.ColumnCount = GParam.theInst.TowerCount;

                for (int i = 0; i < dgvTowerSet.ColumnCount; i++)
                {
                    dgvTowerSet.Columns[i].HeaderText = string.Format("Tower{0:D2}", i + 1);
                    dgvTowerSet.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                dgvTowerSet.Rows.Clear();
                dgvTowerSet.Rows.Add();

                dgvTowerSet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvTowerSet.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                dgvTowerSet.Rows[0].Height = dgvTowerSet.Height;

                int tmpNo = 0;
                for (int j = 0; j < dgvTowerSet.ColumnCount; j++)
                {
                    tmpNo = tmpNo + 1;
                    dgvTowerSet.Rows[0].Cells[j].Value = "Tower" + String.Format("{0:0}", tmpNo);
                }
                dgvTowerSet.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvTowerSet.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 選擇Tower
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvTowerSet_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            lsbTowerName.Items.Clear();
            lsbTowerSetData.Items.Clear();
            for (int i = 0; i < dgvTowerSet.SelectedCells.Count; i++)
            {
                lsbTowerName.Items.Add(dgvTowerSet.SelectedCells[i].Value);
            }

            if (dgvTowerSet.SelectedCells.Count == 1)
            {
                lsbTowerName.SelectedIndex = 0;
                labTowerSetTitle.Text = GParam.theInst.GetLanguage("Single-Tower setup");
                labTowerSetTitle.ForeColor = Color.Blue;
                btnTowerSetMGSL.Enabled = false;
                List<string> NameList = m_DataBase.LoadGradeSetListGetGrade(SStockerSQL.enumGradeType.Tower, lsbTowerName.SelectedItems[0].ToString());
                if (NameList.Count > 0)
                {
                    for (int i = 0; i < NameList.Count; i++)
                    {
                        lsbTowerSetData.Items.Add(NameList[i]);
                    }
                }
            }
            else if (dgvTowerSet.SelectedCells.Count > 1)
            {
                for (int i = 0; i < dgvTowerSet.SelectedCells.Count; i++)
                {
                    lsbTowerName.SelectedIndex = i;
                }
                labTowerSetTitle.Text = GParam.theInst.GetLanguage("Multi-Tower setting");
                labTowerSetTitle.ForeColor = Color.Red;
                btnTowerSetMGSL.Enabled = true;
            }
        }
        /// <summary>
        /// GradeName加入TowerSet中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerSetAdd_Click(object sender, EventArgs e)
        {
            if (lsbTowerName.Items.Count < 1)
            {
                new frmMessageBox("Please select the tower first! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if (lsbTowerGradeName.SelectedItems.Count < 1) //未選擇等級名稱不動作
            {
                new frmMessageBox("Please select the grade name first! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            int tmpChangeID = m_DataBase.SelectChangeLogMaxChangeID(); //取異動編號

            for (int i = 0; i < lsbTowerName.SelectedItems.Count; i++)
            {
                m_DataBase.InsertChangeLog(SStockerSQL.enumGradeType.Tower, lsbTowerName.SelectedItems[i].ToString(), tmpChangeID);
                if (lsbTowerGradeName.SelectedItems.Count > 0)
                {
                    //設定Tower Grade資料
                    for (int j = 0; j < lsbTowerGradeName.SelectedItems.Count; j++)
                    {
                        m_DataBase.InsertGradeNameToStocker(SStockerSQL.enumGradeType.Tower, lsbTowerName.SelectedItems[i].ToString(), lsbTowerGradeName.SelectedItems[j].ToString());
                        m_DataBase.InsertChangeHistoryLog(SStockerSQL.enumChangeType.Add, lsbTowerGradeName.SelectedItems[j].ToString(), tmpChangeID);
                    }
                }
            }

            lsbTowerSetData.Items.Clear();

            List<string> NameList = m_DataBase.LoadGradeSetListGetGrade(SStockerSQL.enumGradeType.Tower, lsbTowerName.SelectedItems[0].ToString());
            if (NameList.Count > 0)
            {
                for (int i = 0; i < NameList.Count; i++)
                {
                    lsbTowerSetData.Items.Add(NameList[i]);
                }
            }
        }
        /// <summary>
        /// GradeName移除TowerSet中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerSetDel_Click(object sender, EventArgs e)
        {
            if (lsbTowerName.Items.Count < 1)
            {
                new frmMessageBox("請先選擇塔柱! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if (lsbTowerSetData.SelectedItems.Count < 1) //未選擇等級名稱不動作
            {
                new frmMessageBox("請先選擇欲刪除的等級! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            int tmpChangeID = m_DataBase.SelectChangeLogMaxChangeID(); //取異動編號

            for (int i = 0; i < lsbTowerName.SelectedItems.Count; i++)
            {
                m_DataBase.InsertChangeLog(SStockerSQL.enumGradeType.Tower, lsbTowerName.SelectedItems[i].ToString(), tmpChangeID);
                if (lsbTowerSetData.SelectedItems.Count > 0)
                {
                    //設定Tower Grade資料
                    for (int j = 0; j < lsbTowerSetData.SelectedItems.Count; j++)
                    {
                        m_DataBase.DeleteGradeNameInStocker(SStockerSQL.enumGradeType.Tower, lsbTowerName.SelectedItems[i].ToString(), lsbTowerSetData.SelectedItems[j].ToString());
                        m_DataBase.InsertChangeHistoryLog(SStockerSQL.enumChangeType.Del, lsbTowerSetData.SelectedItems[j].ToString(), tmpChangeID);
                    }
                }
            }

            lsbTowerSetData.Items.Clear();

            List<string> NameList = m_DataBase.LoadGradeSetListGetGrade(SStockerSQL.enumGradeType.Tower, lsbTowerName.SelectedItems[0].ToString());
            if (NameList.Count > 0)
            {
                for (int i = 0; i < NameList.Count; i++)
                {
                    lsbTowerSetData.Items.Add(NameList[i]);
                }
            }
        }
        /// <summary>
        /// 查詢修改紀錄
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerLogCountQuery_Click(object sender, EventArgs e)
        {
            List<string> NameList = m_DataBase.SelectChangeLog(SStockerSQL.enumGradeType.Tower);
            lsbTowerChangeLog.Items.Clear();
            if (NameList.Count > 0)
            {
                for (int i = 0; i < NameList.Count; i++)
                {
                    lsbTowerChangeLog.Items.Add(NameList[i]);
                }
            }
        }
        /// <summary>
        /// Grade名稱搜尋在哪些Tower
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerSetQuery_Click(object sender, EventArgs e)
        {
            DataTable dt;
            string tmpStr = "";

            if (clbTowerSetGdnaQuery.CheckedItems.Count < 1)
            {
                new frmMessageBox("Please select the tower first! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            DataGridStyleReset(dgvTowerSet);        //DataGridView字型、顏色重置

            for (int i = 0; i < clbTowerSetGdnaQuery.CheckedItems.Count; i++)
            {
                if (tmpStr == "")
                    tmpStr = "TowerGrade='" + clbTowerSetGdnaQuery.CheckedItems[i] + "'";
                else
                    tmpStr = tmpStr + " or TowerGrade='" + clbTowerSetGdnaQuery.CheckedItems[i] + "'";
            }

            dt = m_DataBase.SelectDistinctGradeSetList(SStockerSQL.enumGradeType.Tower, tmpStr);

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dgvTowerSet.RowCount; j++)
                    {
                        for (int k = 0; k < dgvTowerSet.ColumnCount; k++)
                        {
                            if (dgvTowerSet.Rows[j].Cells[k].Value.ToString() == dt.Rows[i]["TowerName"].ToString())
                            {
                                //dgvTowerSet.Rows[j].Cells[k].Style.Font = new Font("微軟正黑體", 9, FontStyle.Bold);
                                dgvTowerSet.Rows[j].Cells[k].Style.BackColor = Color.PaleGreen;
                            }
                        }
                    }
                }
                dgvTowerSet.ClearSelection();
            }
            else
            {
                //MsgBox("查無符合等級區塊!")
                //System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default
                //Exit Sub
            }
            dgvTowerSet.Focus();
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
        }
        /// <summary>
        /// 移除選擇顏色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerSetQueryReSet_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            DataGridStyleReset(dgvTowerSet);        //DataGridView字型、顏色重置
        }
        /// <summary>
        /// 選擇匯入檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerInPath_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbTowerInPath;
            ListBox listBox = lsbTowerInData;
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
        /// 匯入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerInData_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbTowerInPath;
            try
            {
                string strPath = tbx.Text;
                if (strPath == "")
                {
                    new frmMessageBox(string.Format("Please enter the file path."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                DataTable table = ConvertCsvToDataTable(strPath, true);

                if (m_DataBase.InsertTowerSetData(table))//更新SQL
                {

                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 選擇匯出檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerOutPath_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbTowerOutPath;
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
        /// 匯出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTowerOutFile_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbTowerOutPath;
            DataTable dt = m_DataBase.SelectTowerSetData();
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
        #endregion

        #region ========== AreaSet page==========
        /// <summary>
        /// 建構DataGridView
        /// </summary>
        private void ConstructAreaSet()
        {
            try
            {
                dgvAreaSet.ColumnCount = GParam.theInst.TowerCount;

                for (int i = 0; i < dgvAreaSet.ColumnCount; i++)
                {
                    dgvAreaSet.Columns[i].HeaderText = string.Format("Tower{0:D2}", i + 1);
                    dgvAreaSet.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                dgvAreaSet.Rows.Clear();
                dgvAreaSet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvAreaSet.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

                int nAreaOnOneSideOfTower = GParam.theInst.SingleTowerCount / GParam.theInst.SingleAreaCount;
                for (int i = 0; i < nAreaOnOneSideOfTower; i++)
                {
                    dgvAreaSet.Rows.Add();
                    dgvAreaSet.Rows[i].Height = 38 /*77*/ /*dgvAreaSet.Height / 8*/;
                }


                int tmpNo = 0;

                for (int j = 0; j < dgvAreaSet.ColumnCount; j++)
                {
                    for (int k = dgvAreaSet.RowCount - 1; k >= 0; k--)
                    {
                        tmpNo = tmpNo + 1;
                        dgvAreaSet.Rows[k].Cells[j].Value = "Area" + String.Format("{0:0}", tmpNo);
                    }
                }
                dgvAreaSet.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvAreaSet.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 選擇Area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvAreaSet_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            lsbAreaName.Items.Clear();
            lsbAreaSetData.Items.Clear();
            for (int i = 0; i < dgvAreaSet.SelectedCells.Count; i++)
            {
                lsbAreaName.Items.Add(dgvAreaSet.SelectedCells[i].Value);
            }

            if (dgvAreaSet.SelectedCells.Count == 1)
            {
                lsbAreaName.SelectedIndex = 0;
                labAreaSetTitle.Text = GParam.theInst.GetLanguage("Single-Area setup");
                labAreaSetTitle.ForeColor = Color.Blue;
                btnAreaSetMGSL.Enabled = false;
                List<string> NameList = m_DataBase.LoadGradeSetListGetGrade(SStockerSQL.enumGradeType.Area, lsbAreaName.SelectedItems[0].ToString());
                if (NameList.Count > 0)
                {
                    for (int i = 0; i < NameList.Count; i++)
                    {
                        lsbAreaSetData.Items.Add(NameList[i]);
                    }
                }
            }
            else if (dgvAreaSet.SelectedCells.Count > 1)
            {
                for (int i = 0; i < dgvAreaSet.SelectedCells.Count; i++)
                {
                    lsbAreaName.SelectedIndex = i;
                }
                labAreaSetTitle.Text = GParam.theInst.GetLanguage("Multi-Area setting");
                labAreaSetTitle.ForeColor = Color.Red;
                btnAreaSetMGSL.Enabled = true;
                foreach (string item1 in lsbAreaName.Items)//選擇Area的名子
                {
                    //找Area已經設定那些GreadName
                    List<string> NameList = m_DataBase.LoadGradeSetListGetGrade(SStockerSQL.enumGradeType.Area, item1);
                    //把名子放在清單裡
                    foreach (string item2 in NameList)
                    {
                        if (lsbAreaSetData.Items.Contains(item2) == false)
                            lsbAreaSetData.Items.Add(item2);
                    }
                }
            }
        }
        /// <summary>
        /// GradeName加入AreaSet中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAreaSetAdd_Click(object sender, EventArgs e)
        {
            if (lsbAreaName.Items.Count < 1)
            {
                new frmMessageBox("Please select the area first! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if (lsbAreaGradeName.SelectedItems.Count < 1) //未選擇等級名稱不動作
            {
                new frmMessageBox("Please select the grade name first! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            int tmpChangeID = m_DataBase.SelectChangeLogMaxChangeID(); //取異動編號

            for (int i = 0; i < lsbAreaName.SelectedItems.Count; i++)
            {
                m_DataBase.InsertChangeLog(SStockerSQL.enumGradeType.Area, lsbAreaName.SelectedItems[i].ToString(), tmpChangeID);
                if (lsbAreaGradeName.SelectedItems.Count > 0)
                {
                    //Area Grade資料
                    for (int j = 0; j < lsbAreaGradeName.SelectedItems.Count; j++)
                    {
                        m_DataBase.InsertGradeNameToStocker(SStockerSQL.enumGradeType.Area, lsbAreaName.SelectedItems[i].ToString(), lsbAreaGradeName.SelectedItems[j].ToString());
                        m_DataBase.InsertChangeHistoryLog(SStockerSQL.enumChangeType.Add, lsbAreaGradeName.SelectedItems[j].ToString(), tmpChangeID);
                    }
                }
            }

            lsbAreaSetData.Items.Clear();

            List<string> NameList = m_DataBase.LoadGradeSetListGetGrade(SStockerSQL.enumGradeType.Area, lsbAreaName.SelectedItems[0].ToString());
            if (NameList.Count > 0)
            {
                for (int i = 0; i < NameList.Count; i++)
                {
                    lsbAreaSetData.Items.Add(NameList[i]);
                }
            }
        }
        /// <summary>
        /// GradeName移除AreaSet中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAreaSetDel_Click(object sender, EventArgs e)
        {
            if (lsbAreaName.Items.Count < 1)
            {
                new frmMessageBox("Please select the area first! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if (lsbAreaSetData.SelectedItems.Count < 1) //未選擇等級名稱不動作
            {
                new frmMessageBox("Please select the grade name first! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            int tmpChangeID = m_DataBase.SelectChangeLogMaxChangeID(); //取異動編號

            for (int i = 0; i < lsbAreaName.SelectedItems.Count; i++)
            {
                m_DataBase.InsertChangeLog(SStockerSQL.enumGradeType.Area, lsbAreaName.SelectedItems[i].ToString(), tmpChangeID);
                if (lsbAreaSetData.SelectedItems.Count > 0)
                {
                    //設定Area Grade資料
                    for (int j = 0; j < lsbAreaSetData.SelectedItems.Count; j++)
                    {
                        m_DataBase.DeleteGradeNameInStocker(SStockerSQL.enumGradeType.Area, lsbAreaName.SelectedItems[i].ToString(), lsbAreaSetData.SelectedItems[j].ToString());
                        m_DataBase.InsertChangeHistoryLog(SStockerSQL.enumChangeType.Del, lsbAreaSetData.SelectedItems[j].ToString(), tmpChangeID);
                    }
                }
            }

            lsbAreaSetData.Items.Clear();

            List<string> NameList = m_DataBase.LoadGradeSetListGetGrade(SStockerSQL.enumGradeType.Area, lsbAreaName.SelectedItems[0].ToString());
            if (NameList.Count > 0)
            {
                for (int i = 0; i < NameList.Count; i++)
                {
                    lsbAreaSetData.Items.Add(NameList[i]);
                }
            }
        }
        /// <summary>
        /// 查詢修改紀錄
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAreaLogCountQuery_Click(object sender, EventArgs e)
        {
            List<string> NameList = m_DataBase.SelectChangeLog(SStockerSQL.enumGradeType.Area);

            lsbAreaChangeLog.Items.Clear();

            if (NameList.Count > 0)
            {
                for (int i = 0; i < NameList.Count; i++)
                {
                    lsbAreaChangeLog.Items.Add(NameList[i]);
                }
            }
        }
        /// <summary>
        /// Grade名稱搜尋在哪些AreaSet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAreaSetQuery_Click(object sender, EventArgs e)
        {
            DataTable dt;
            string tmpStr = "";

            if (clbAreaSetGdnaQuery.CheckedItems.Count < 1)
            {
                new frmMessageBox("Please select the grade name first! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            DataGridStyleReset(dgvAreaSet);        //DataGridView字型、顏色重置

            for (int i = 0; i < clbAreaSetGdnaQuery.CheckedItems.Count; i++)
            {
                if (tmpStr == "")
                    tmpStr = "AreaGrade='" + clbAreaSetGdnaQuery.CheckedItems[i] + "'";
                else
                    tmpStr = tmpStr + " or AreaGrade='" + clbAreaSetGdnaQuery.CheckedItems[i] + "'";
            }

            dt = m_DataBase.SelectDistinctGradeSetList(SStockerSQL.enumGradeType.Area, tmpStr);

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dgvAreaSet.RowCount; j++)
                    {
                        for (int k = 0; k < dgvAreaSet.ColumnCount; k++)
                        {
                            if (dgvAreaSet.Rows[j].Cells[k].Value.ToString() == dt.Rows[i]["AreaName"].ToString())
                            {
                                dgvAreaSet.Rows[j].Cells[k].Style.Font = new Font("微軟正黑體", 9, FontStyle.Bold);
                                dgvAreaSet.Rows[j].Cells[k].Style.BackColor = Color.PaleGreen;
                            }
                        }
                    }
                }
                dgvAreaSet.ClearSelection();
            }
            else
            {
                //MsgBox("查無符合等級區塊!")
                //System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default
                //Exit Sub
            }
            dgvAreaSet.Focus();
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
        }
        /// <summary>
        /// 移除選擇顏色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAreaSetQueryReSet_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            DataGridStyleReset(dgvAreaSet);        //DataGridView字型、顏色重置
        }
        /// <summary>
        /// 選擇匯入檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnASInPath_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbASInPath;
            ListBox listBox = lsbASInData;
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
        /// 匯入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnASInData_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbASInPath;
            try
            {
                string strPath = tbx.Text;
                if (strPath == "")
                {
                    new frmMessageBox(string.Format("Please enter the file path."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                DataTable table = ConvertCsvToDataTable(strPath, true);

                if (m_DataBase.InsertAreaSetData(table))//更新SQL
                {

                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 選擇匯出檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnASOutPath_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbASOutPath;
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
        /// 匯出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnASOutFile_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbASOutPath;
            DataTable dt = m_DataBase.SelectAreaSetData();
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
        #endregion

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
                    //btnSlotStausUpdate.Enabled = false;
                    btnSlotStausUpdate.Enabled = true;
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
                /*DataTable dt = m_DataBase.SelectSlotStatus();
                for (int i = 0; i < m_ListSTK.Count; i++)
                {
                    if (m_ListSTK[i].Disable == false)
                    {
                        for (int nT = 0; nT < GParam.theInst.TowerCount / m_ListSTK.Count; nT++)
                        {
                            for (int nSTC = 0; nSTC < GParam.theInst.SingleTowerCount; nSTC++)
                            {
                                if (m_ListSTK[i].GetMapDataAll()[nT * GParam.theInst.SingleTowerCount + nSTC].ToString() == dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["WaferStatus"].ToString())
                                {

                                }
                                else
                                {
                                    switch (m_ListSTK[i].GetMapDataAll()[nT * GParam.theInst.SingleTowerCount + nSTC].ToString())
                                    {
                                        case "0":
                                            m_DataBase.UpdateSlotStatus(dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["SlotName"].ToString());
                                            break;
                                        case "1":
                                            m_DataBase.UpdateSlotStatus(dt.Rows[((i * GParam.theInst.TowerCount / m_ListSTK.Count) + nT) * GParam.theInst.SingleTowerCount + nSTC]["SlotName"].ToString(),
                                                "UnKnow", "UnKnow", "UnKnow", "UnKnow");
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }*/

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
            frmPassword myDlgPwd = new frmPassword();
            if (DialogResult.OK == myDlgPwd.ShowDialog() && myDlgPwd.GetPassWord == "1")
            {
                if (WaferIDData.Text == null || WaferIDData.Text == "")
                {
                    if (m_userManager.Level == 0)
                    {
                        m_DataBase.UpdateSlotStatus(SlotName.Text);
                        //改完DB後要去改SWafer
                        int nTower1to16 = int.Parse(SlotName.Text.Split('-')[0].Replace("T", ""));
                        int nSlot1to400 = int.Parse(SlotName.Text.Split('-')[1].Replace("S", ""));
                        int nFaceIndx = (nTower1to16 - 1) % 4;
                        m_ListSTK[nTower1to16 / 4].TakeWaferOut2(nFaceIndx, nSlot1to400);
                        //把DB資料撈出來給STK的GMAP
                        /*for (int i = 0; i < GParam.theInst.TowerCount; i++)//16
                        {
                            string strMessage = "";
                            DataTable dtTower = m_DataBase.SelectSlotStatus(i + 1);
                            foreach (DataRow item in dtTower.Rows) { strMessage += item["WaferStatus"].ToString(); }
                            //把DB的資料先寫給Mapping資料
                            m_ListSTK[i / 4].SetMapDataTower(i % 4, strMessage);
                        }*/
                    }
                    else
                    {
                        new frmMessageBox("Cannot delete wafer data, Please run the mapping again", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        //无法直接删除晶圆数据,请再次运行扫描
                        return;
                    }
                }
                else
                {
                    //要修改增加wafer的內部信息
                    m_DataBase.UpdateSlotStatus(SlotName.Text, WaferIDData.Text, WaferGradeData.Text, M12IDData.Text, LotIDData.Text);
                    //改完DB後要去改SWafer
                    int nTower1to16 = int.Parse(SlotName.Text.Split('-')[0].Replace("T", ""));
                    int nSlot1to400 = int.Parse(SlotName.Text.Split('-')[1].Replace("S", ""));
                    SWafer w = m_ListSTK[nTower1to16 / 4].GetWafer(nTower1to16, nSlot1to400);
                    w.WaferID_F = M12IDData.Text;
                    w.WaferID_B = WaferIDData.Text;
                    w.GradeID = WaferGradeData.Text;
                    w.LotID = LotIDData.Text;

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
        #endregion

        #region ========== GradeSet page ==========
        /// <summary>
        /// 建構DataGridView
        /// </summary>
        private void ConstructGradeSet()
        {
            Label43.Visible = mtbGSHLadd.Visible = Label44.Visible = mtbGSLLadd.Visible = GParam.theInst.GradeHLLimit;
            Label46.Visible = mtbGSHLupdate.Visible = Label45.Visible = mtbGSLLupdate.Visible = GParam.theInst.GradeHLLimit;
            Label50.Visible = cbbGSFIDadd.Visible = GParam.theInst.GradeRFID3;
            Label51.Visible = cbbGSFIDupdate.Visible = GParam.theInst.GradeRFID3;

            dgvGradeSet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGradeSet.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            dgvGradeSet.DataSource = m_DataBase.SelectGradeData();
            dgvGradeSet.ClearSelection();
            dgvGradeSet.Columns["No"].Visible = false;
            dgvGradeSet.Columns["HightLimit"].Visible = GParam.theInst.GradeHLLimit;
            dgvGradeSet.Columns["LowLimit"].Visible = GParam.theInst.GradeHLLimit;
            dgvGradeSet.Columns["FoupID3"].Visible = GParam.theInst.GradeRFID3;
        }
        /// <summary>
        /// 點選DataGridView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvGradeSet_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            txbGradeNo.Text = dgvGradeSet.Rows[e.RowIndex].Cells["No"].Value.ToString();
            txbGradeSetUpdate.Text = dgvGradeSet.Rows[e.RowIndex].Cells["GradeName"].Value.ToString();
            mtbGSHLupdate.Text = dgvGradeSet.Rows[e.RowIndex].Cells["HightLimit"].Value.ToString();
            mtbGSLLupdate.Text = dgvGradeSet.Rows[e.RowIndex].Cells["LowLimit"].Value.ToString();
            cbbGSFIDupdate.Text = dgvGradeSet.Rows[e.RowIndex].Cells["FoupID3"].Value.ToString();
        }
        /// <summary>
        /// GradeName有更動，需要更新畫面
        /// </summary>
        private void UpDataGradeName()
        {
            List<string> list = m_DataBase.SelectGradeNameList();
            lsbTowerGradeName.Items.Clear();
            lsbAreaGradeName.Items.Clear();
            clbTowerSetGdnaQuery.Items.Clear();
            clbAreaSetGdnaQuery.Items.Clear();
            clbSlotStatusGdnaQuery.Items.Clear();
            foreach (string item in list)
            {
                lsbTowerGradeName.Items.Add(item);
                lsbAreaGradeName.Items.Add(item);
                clbTowerSetGdnaQuery.Items.Add(item);
                clbAreaSetGdnaQuery.Items.Add(item);
                clbSlotStatusGdnaQuery.Items.Add(item);
            }
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGradeSetAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvGradeSet.RowCount >= 1000)
                {
                    new frmMessageBox("The maximum level setting is 1000 strokes, add failed!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                if (txbGradeSetAdd.Text == "")
                {
                    new frmMessageBox("Grade Name must not be blank!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                if (GParam.theInst.GradeHLLimitVisible == true)
                {
                    if (mtbGSHLadd.Text == "" && mtbGSLLadd.Text == "")
                    {
                        new frmMessageBox("The upper and lower limits cannot be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }

                    if (int.Parse(mtbGSHLadd.Text) <= 0)
                    {
                        new frmMessageBox("High level cannot be set to 0.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }

                    if (int.Parse(mtbGSLLadd.Text) < 0)
                    {
                        new frmMessageBox("The L-Limit must not be less than 0.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        return;
                    }
                }
                else
                {
                    mtbGSHLadd.Text = mtbGSLLadd.Text = "0";
                }

                DataTable dt = m_DataBase.SelectGradeData(txbGradeSetAdd.Text);
                if (dt == null || dt.Rows.Count != 0)
                {
                    new frmMessageBox("GradeName Repeat", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                m_DataBase.InsertGradeName(txbGradeSetAdd.Text, int.Parse(mtbGSHLadd.Text), int.Parse(mtbGSLLadd.Text), cbbGSFIDadd.Text);
                ConstructGradeSet();
                UpDataGradeName();//grade 新增
            }
            catch (Exception ex)
            {
                m_logger.WriteLog(this.Name, ex);
            }
        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGradeSetUpdate_Click(object sender, EventArgs e)
        {
            if (txbGradeSetUpdate.Text == "")
            {
                new frmMessageBox("Grade Name must not be blank!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if (GParam.theInst.GradeHLLimitVisible == true)
            {
                if (mtbGSHLupdate.Text == "" && mtbGSLLupdate.Text == "")
                {
                    new frmMessageBox("The upper and lower limits cannot be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                if (int.Parse(mtbGSHLupdate.Text) <= 0)
                {
                    new frmMessageBox("High level cannot be set to 0.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

                if (int.Parse(mtbGSLLupdate.Text) < 0)
                {
                    new frmMessageBox("The L-Limit must not be less than 0.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
            }
            m_DataBase.UpdateGradeName(int.Parse(txbGradeNo.Text), txbGradeSetUpdate.Text, int.Parse(mtbGSHLupdate.Text), int.Parse(mtbGSLLupdate.Text), cbbGSFIDupdate.Text);
            ConstructGradeSet();
            UpDataGradeName();//grade 修改
        }
        /// <summary>
        /// 刪除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGradeSetDel_Click(object sender, EventArgs e)
        {
            string str1 = GParam.theInst.GetLanguage("Make sure to delete the selected 『");

            if (dgvGradeSet.SelectedRows == null || dgvGradeSet.SelectedRows.Count == 0)
            {
                new frmMessageBox("Pls select GradeName!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }


            if (new frmMessageBox(str1 + dgvGradeSet.SelectedRows[0].Index + "』", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
            {
                m_DataBase.DeleteGradeName(int.Parse(txbGradeNo.Text));
                ConstructGradeSet();
                UpDataGradeName();//grade 刪除
            }
        }
        /// <summary>
        /// 重置選擇
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGradeSetReSet_Click(object sender, EventArgs e)
        {
            txbGradeSetAdd.Text = "";
            txbGradeNo.Text = "";
            txbGradeSetUpdate.Text = "";
            mtbGSHLadd.Text = "";
            mtbGSLLadd.Text = "";
            cbbGSFIDadd.Text = "";
            mtbGSHLupdate.Text = "";
            mtbGSLLupdate.Text = "";
            cbbGSFIDupdate.Text = "";
            if (dgvGradeSet.RowCount > 0)
            {
                dgvGradeSet.CurrentCell = dgvGradeSet.Rows[0].Cells[1];
                dgvGradeSet.ClearSelection();
            }
            else
            {
                btnGradeSetUpdate.Enabled = false;
                btnGradeSetDel.Enabled = false;
            }
        }
        /// <summary>
        /// 選擇匯入檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbOpenFilePath;
            ListBox listBox = lsbFromTxtData;
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
        /// 匯入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGradeSetTransDB_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbOpenFilePath;
            try
            {
                string strPath = tbx.Text;
                if (strPath == "")
                {
                    new frmMessageBox(string.Format("Please enter the file path."), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                DataTable table = ConvertCsvToDataTable(strPath, true);

                if (m_DataBase.UpdateGradeName(table))//更新SQL
                {
                    ConstructGradeSet(); //更新UI
                    UpDataGradeName();//grade 匯入
                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 選擇匯出檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTransFileOpenFile_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbTransFilePath;
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
        /// 匯出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGradeSetTransFile_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbTransFilePath;
            DataTable dt = m_DataBase.SelectGradeData();
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


        #endregion

        #region ========== Button ==========

        private void rdbTowerQueryEab_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbTowerQueryEab.Checked == true)
            {
                GParam.theInst.TowerQuerySet = true;
                m_DataBase.TowerQuerySet = true;
                ChangeButtun(rdbTowerQueryEab, true);
                ChangeButtun(rdbTowerQueryDsb, false);
            }
        }
        private void rdbTowerQueryDsb_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbTowerQueryDsb.Checked == true)
            {
                GParam.theInst.TowerQuerySet = false;
                m_DataBase.TowerQuerySet = false;
                ChangeButtun(rdbTowerQueryEab, false);
                ChangeButtun(rdbTowerQueryDsb, true);
            }
        }
        private void rdbAreaQueryEab_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbAreaQueryEab.Checked == true)
            {
                GParam.theInst.AreaQuerySet = true;
                m_DataBase.AreaQuerySet = true;
                ChangeButtun(rdbAreaQueryEab, true);
                ChangeButtun(rdbAreaQueryDsb, false);
            }
        }
        private void rdbAreaQueryDsb_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbAreaQueryDsb.Checked == true)
            {
                GParam.theInst.AreaQuerySet = false;
                m_DataBase.AreaQuerySet = false;
                ChangeButtun(rdbAreaQueryEab, false);
                ChangeButtun(rdbAreaQueryDsb, true);
            }
        }
        private void rdbHLLimitSetEab_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbHLLimitSetEab.Checked == true)
            {
                GParam.theInst.GradeHLLimit = true;
                ChangeButtun(rdbHLLimitSetEab, true);
                ChangeButtun(rdbHLLimitSetDsb, false);
            }
        }
        private void rdbHLLimitSetDsb_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbHLLimitSetDsb.Checked == true)
            {
                GParam.theInst.GradeHLLimit = false;
                ChangeButtun(rdbHLLimitSetEab, false);
                ChangeButtun(rdbHLLimitSetDsb, true);
            }
        }
        private void rdbRFID3SetEab_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbRFID3SetEab.Checked == true)
            {
                GParam.theInst.GradeRFID3 = false;
                ChangeButtun(rdbRFID3SetEab, true);
                ChangeButtun(rdbRFID3SetDsb, false);
            }
        }
        private void rdbRFID3SetDsb_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbRFID3SetDsb.Checked == true)
            {
                GParam.theInst.GradeRFID3 = false;
                ChangeButtun(rdbRFID3SetEab, false);
                ChangeButtun(rdbRFID3SetDsb, true);
            }
        }

        private void btnCleanGradeName_Click(object sender, EventArgs e)
        {
            if (m_userManager.Level != 0 && m_userManager.Level != 1)
            {
                new frmMessageBox("Insufficient authority.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (new frmMessageBox("The initialization action is irreversible, making sure to initialize?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
            {
                m_DataBase.TruncateGradeData();
            }
        }
        /// <summary>
        /// 清除 database slotstatus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCleanSlotStatus_Click(object sender, EventArgs e)
        {
            if (m_userManager.Level != 0 && m_userManager.Level != 1)
            {
                new frmMessageBox("Insufficient authority.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (new frmMessageBox("The initialization action is irreversible, making sure to initialize?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
            {
                m_DataBase.TruncateSlotStatus();
            }
        }
        /// <summary>
        /// 清除 database processstatus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCleanProcessStatus_Click(object sender, EventArgs e)
        {
            if (m_userManager.Level != 0 && m_userManager.Level != 1)
            {
                new frmMessageBox("Insufficient authority.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (new frmMessageBox("The initialization action is irreversible, making sure to initialize?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
            {
                m_DataBase.TruncateProcessStatus();
            }
        }
        /// <summary>
        /// 清除 database unitstatus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCleanUnitStatus_Click(object sender, EventArgs e)
        {
            if (m_userManager.Level != 0 && m_userManager.Level != 1)
            {
                new frmMessageBox("Insufficient authority.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (new frmMessageBox("The initialization action is irreversible, making sure to initialize?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
            {
                m_DataBase.TruncateUnitStatus();
            }
        }
        /// <summary>
        /// 移除重覆的名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGradeValueFix_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = m_DataBase.SelectGradeDataNameRepeat();

                if (dt == null || dt.Rows.Count <= 0)
                {
                    //沒有重複
                    return;
                }

                foreach (DataRow item in dt.Rows)
                {
                    string strGradeName = item[0].ToString();
                    string str1 = string.Format("{0}:{1} {2}:{3}", "Name", strGradeName, "repeated counts", item[1]);
                    string str2 = "Repeat data will keep the latest 1, sure to delete the old data?";
                    if (new frmMessageBox(str1 + "\r\n" + str2, "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                    {
                        m_DataBase.DeleteRepeatGradeName(strGradeName);
                    }
                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        #endregion

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



        private delegate void UpdateButtunUI(RadioButton MsgBox, bool ret);
        public void ChangeButtun(RadioButton MsgBox, bool ret)
        {
            if (InvokeRequired)
            {
                UpdateButtunUI dlg = new UpdateButtunUI(ChangeButtun);
                this.BeginInvoke(dlg, MsgBox, ret);
            }
            else
            {
                if (ret == true)
                {
                    MsgBox.ForeColor = Color.White;
                    MsgBox.BackColor = Color.Green;
                }
                else
                {
                    MsgBox.ForeColor = Color.Black;
                    MsgBox.BackColor = Color.Gainsboro;
                }
            }
        }

        private delegate void dlgAddMessage(ListBox box, string strMsg);
        public void AddMessage(ListBox box, string strMsg)
        {
            if (InvokeRequired)
            {
                dlgAddMessage dlg = new dlgAddMessage(AddMessage);
                this.Invoke(dlg, strMsg);
            }
            else
            {
                box.Items.Add(strMsg);
                this.Refresh();
            }
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



        #region ========== ParticleSet page==========
        /// <summary>
        /// 建構dgvParticleSet
        /// </summary>
        private void Construct_dgvParticleSet()
        {
            try
            {
                dgvParticleSet.ColumnCount = GParam.theInst.TowerCount;

                for (int i = 0; i < dgvParticleSet.ColumnCount; i++)
                {
                    dgvParticleSet.Columns[i].HeaderText = string.Format("Tower{0:D2}", i + 1);
                    dgvParticleSet.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                dgvParticleSet.Rows.Clear();
                dgvParticleSet.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

                for (int i = 0; i < GParam.theInst.SingleTowerCount; i++)
                {
                    dgvParticleSet.Rows.Add();
                    dgvParticleSet.Rows[i].Height = 30;
                }

                dgvParticleSet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                dgvParticleSet.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvParticleSet.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopCenter;
                dgvParticleSet.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 更新dgvParticleSet
        /// </summary>
        private void UpData_dgvParticleSet()
        {
            try
            {
                DataTable dt = m_DataBase.SelectSlotStatus();
                lsbParticle.Items.Clear();
                lsbDisable.Items.Clear();
                for (int i = 0; i < dgvParticleSet.RowCount; i++)
                {
                    for (int j = 0; j < dgvParticleSet.ColumnCount; j++)
                    {
                        string strSlotName = "T" + String.Format("{0:00}", j + 1) + "-S" + String.Format("{0:000}", dgvParticleSet.RowCount - i);

                        DataRow[] item = dt.Select("SlotName ='" + strSlotName + "'");

                        for (int ii = 0; ii < item.Length; ii++)
                        {
                            dgvParticleSet.Rows[i].Cells[j].Value =
                                item[ii]["SlotName"].ToString() + "\r\n" +
                                item[ii]["Particle"].ToString() + "/" + (item[ii]["Disable"].ToString() == "True" ? "Disable" : "Enable");

                            if (item[ii]["Disable"].ToString() == "True")
                            {
                                dgvParticleSet.Rows[i].Cells[j].Style.BackColor = Color.Red;
                                AddMessage(lsbDisable, item[ii]["SlotName"].ToString() + " Disable");
                            }
                            else if (item[ii]["Particle"].ToString() == "True")
                            {
                                dgvParticleSet.Rows[i].Cells[j].Style.BackColor = Color.LightBlue;
                                AddMessage(lsbParticle, item[ii]["SlotName"].ToString());
                            }
                            else
                            {
                                dgvParticleSet.Rows[i].Cells[j].Style.BackColor = Color.Empty;
                            }



                        }
                    }
                }
            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 點選dgvParticleSet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvParticleSet_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                string[] strArray;

                strArray = dgvParticleSet.Rows[dgvParticleSet.CurrentRow.Index].Cells[dgvParticleSet.CurrentCell.ColumnIndex].Value.ToString().Replace("\r\n", " ").Split(' ');

                lblParticleSetSelectName.Text = strArray[0].ToString();
                WaferIDData.Text = strArray[1].ToString();

                if (strArray[1].ToString().Contains("True"))
                    cbxParticleSetParticleTF.SelectedIndex = 0;
                else
                    cbxParticleSetParticleTF.SelectedIndex = 1;

                if (strArray[1].ToString().Contains("Disable"))
                    cbxParticleSetDisableTF.SelectedIndex = 0;
                else
                    cbxParticleSetDisableTF.SelectedIndex = 1;


            }
            catch (Exception ex) { WriteLog(this.Name + "::" + ex); }
        }
        /// <summary>
        /// 修改單一內容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnParticleSetSelectModify_Click_1(object sender, EventArgs e)
        {
            frmPassword myDlgPwd = new frmPassword();
            if (DialogResult.OK == myDlgPwd.ShowDialog() && myDlgPwd.GetPassWord == "1")
            {
                m_DataBase.UpdateSlotStatus(lblParticleSetSelectName.Text, cbxParticleSetParticleTF.SelectedIndex == 0, cbxParticleSetDisableTF.SelectedIndex == 0);
                UpData_dgvParticleSet();
            }
        }


        private void lsbParticle_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                e.DrawBackground();
                Brush mybsh = Brushes.Blue;
                // 焦点框
                e.DrawFocusRectangle();
                //文本 
                e.Graphics.DrawString(lsbParticle.Items[e.Index].ToString(), e.Font, mybsh, e.Bounds, StringFormat.GenericDefault);
            }
        }
        private void lsbDisable_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                e.DrawBackground();
                Brush mybsh = Brushes.Red;
                // 焦点框
                e.DrawFocusRectangle();
                //文本 
                e.Graphics.DrawString(lsbDisable.Items[e.Index].ToString(), e.Font, mybsh, e.Bounds, StringFormat.GenericDefault);
            }
        }

        #endregion

        #region ========== waferlog page ==========
        /// <summary>
        /// 查詢Query waferlog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnWaferLogQuery_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> strNameSubstitutionList = new Dictionary<string, string>();
            strNameSubstitutionList["WaferID"] = "T7";
            strNameSubstitutionList["M12ID"] = "M12";

            if (mtbWaferLogCount.Text == "")
            {
                new frmMessageBox("查詢筆數需大於0，請輸入查詢筆數!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            DataTable dt = m_DataBase.SelectWaferlog(dtpQueryDateStart.Value, dtpQueryDateEnd.Value, txbWaferLogQuerySource.Text,
                txbWaferLogQueryTarget.Text, txbWaferLogQueryActionType.Text, txbWaferLogQueryWaferID.Text, txbWaferLogQueryWaferGrade.Text,
                txbWaferLogQueryCommander.Text, txbWaferLogQueryM12ID.Text, txbWaferLogQueryLotID.Text, txbWaferLogQueryVCL.Text, mtbWaferLogCount.Text);
            dgvWaferLog.DataSource = dt;

            //dgvWaferLog.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //dgvWaferLog.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopCenter;
            //dgvWaferLog.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            dgvWaferLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvWaferLog.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        }
        /// <summary>
        /// 選擇匯出檔案的路徑名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnWaferLogTransFileOpen_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbWaferLogTransFilePath;
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
        /// 匯出waferlog所有資料
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnWaferLogTransFile_Click(object sender, EventArgs e)
        {
            TextBox tbx = txbWaferLogTransFilePath;
            DataTable dt = m_DataBase.SelectWaferlog(dtpQueryDateStart.Value, dtpQueryDateEnd.Value, txbWaferLogQuerySource.Text,
                 txbWaferLogQueryTarget.Text, txbWaferLogQueryActionType.Text, txbWaferLogQueryWaferID.Text, txbWaferLogQueryWaferGrade.Text,
                 txbWaferLogQueryCommander.Text, txbWaferLogQueryM12ID.Text, txbWaferLogQueryLotID.Text, txbWaferLogQueryVCL.Text, mtbWaferLogCount.Text);
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


        #endregion



        private void dgvAreaSet_CellStateChanged(object sender, DataGridViewCellStateChangedEventArgs e)
        {


            if (GParam.theInst.AreaSelect == 2) // 1: 25 slot 為一個Area . 2: 50 slot 為一個Area
            {
                int TempIndex = 0;
                // Need select 2 Area
                if (e.Cell.RowIndex == 0)
                {
                    TempIndex = 1;

                }
                else
                {
                    if ((e.Cell.RowIndex + 1) % 2 == 0) //偶數格
                    {
                        TempIndex = e.Cell.RowIndex - 1;

                    }
                    else // 奇數格
                    {
                        TempIndex = e.Cell.RowIndex + 1;
                    }


                }

                if (dgvAreaSet[e.Cell.ColumnIndex, TempIndex].Selected != e.Cell.Selected)
                    dgvAreaSet[e.Cell.ColumnIndex, TempIndex].Selected = e.Cell.Selected;
            }





        }

        private void lsbAreaName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (GParam.theInst.AreaSelect == 2) // 1: 25 slot 為一個Area . 2: 50 slot 為一個Area
            {
                ListBox Temp = (ListBox)sender;
                int AreaNo = 0;
                int TempAreaNo = 0;
                for (int i = 0; i < Temp.SelectedItems.Count; i++)
                {
                    if (int.TryParse(Temp.SelectedItems[i].ToString().Split('a')[1], out AreaNo))
                    {
                        if (AreaNo % 2 == 0)//偶數
                        {
                            TempAreaNo = AreaNo - 1;
                        }
                        else // 基數
                        {
                            TempAreaNo = AreaNo + 1;
                        }
                        for (int z = 0; z < lsbAreaName.Items.Count; z++)
                        {
                            if (lsbAreaName.Items[z].ToString() == ("Area" + TempAreaNo).ToString())
                            {
                                lsbAreaName.SelectedIndex = z;
                            }
                        }

                    }


                }





            }
        }
    }
}

