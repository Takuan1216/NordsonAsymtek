using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RorzeApi.Class;
using RorzeComm.Log;
using RorzeUnit.Class;

namespace RorzeApi
{
    public partial class frmLogProcess : Form
    {
        float frmX;//當前窗體的寬度
        float frmY;//當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        private SProcessDB _dbLog;
        private SPermission _permission;
        enum LogType { Lot = 0, wafer, station }
        LogType CurrentType;

        LogType SetCurrentType
        {
            set
            {

                CurrentType = value;
                switch (CurrentType)
                {
                    case LogType.Lot:
                        //labPageType.Text = "Lot Type";
                        break;
                    case LogType.wafer:
                        //labPageType.Text = "Wafer Type";
                        break;
                    case LogType.station:
                        //labPageType.Text = "Wafer Station Type";

                        break;
                }
            }
        }

        DataSet dsGet = new DataSet();
        DataSet dsAlarm = new DataSet();
        int PageCount = 100;
        int CurrentPage = 0;
        int DayRange = 7;
        ToolStripButton NavClicked = new ToolStripButton();
        //20150428 Daniel
        //frmLogSubProcess _frmLogSubProcess; 
        //frmLogSubProcessDetial _frmLogSubProcessDetial;

        public frmLogProcess(SProcessDB dbLog, SPermission permission)
        {
            try
            {
                InitializeComponent();

                //_frmLogSubProcess = new frmLogSubProcess(dbLog);
                //_frmLogSubProcessDetial = new frmLogSubProcessDetial(dbLog);
                _dbLog = dbLog;
                _permission = permission;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
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


        //========= 
        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.DataSource = null;
                dataGridView1.Columns.Clear();
                string strSQL = string.Empty;

                //查詢格不為空時
                if (txtKeyWork.Text != string.Empty)
                {
                    //dsGet = _dbLog.GetProcessSearchLike(txtKeyWork.Text.Trim(), dtpStartTime.Value.ToString("yyyy/MM/dd"), dtpEndTime.Value.ToString("yyyy/MM/dd"));
                    //dsGet = _dbLog.GetProcessWaferSearchLike(txtKeyWork.Text.Trim(), dtpStartTime.Value.ToString("yyyy/MM/dd"), dtpEndTime.Value.ToString("yyyy/MM/dd"));
                }
                else
                {
                    //查詢為空時只做時間搜尋
                    //dsGet = _dbLog.SearchOccurTime(dtpStartTime.Value.ToString("yyyy/MM/dd"), dtpEndTime.Value.ToString("yyyy/MM/dd"));
                    //dsGet = _dbLog.GetProcessWaferTime(dtpStartTime.Value.ToString("yyyy/MM/dd"), dtpEndTime.Value.ToString("yyyy/MM/dd"));

                    //查詢格不為空時
                    dsGet = _dbLog.GetProcessLotInfoSearchLike(dtpStartTime.Value.ToString("yyyy/MM/dd"), dtpEndTime.Value.ToString("yyyy/MM/dd"));

                    if (dsGet.Tables.Count > 0)
                    {
                        if (dsGet.Tables[0].Rows.Count > 0)
                        {
                            bindingSource1.DataSource = dsGet.Tables[0].AsEnumerable().Take(dsGet.Tables[0].Rows.Count / PageCount + 1);
                            //資料筆數
                            this.labDatabaseLoadCount.Text = dsGet.Tables[0].Rows.Count.ToString();
                            //清空搜尋數值
                            txtKeyWork.Text = null;
                            NavClicked = new ToolStripButton("First");
                            DeterminePageBoundaries();
                        }
                    }       
                }
                SetCurrentType = LogType.Lot;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void btnExport_Click(object sender, EventArgs e)
        {
            //                var Db = _db.Reader(string.Format(@"
            //                    SELECT  
            //                          HistoryProcessLot.OPID, HistoryProcessLot.CJID, HistoryProcessLot.LotID,
            //                          HistoryProcessLot.RecipeName, HistoryProcessLot.RecipeTime, 
            //                          HistoryProcessLot.WaferCount, HistoryProcessLot.StartTime AS LotStartTime, 
            //                          HistoryProcessLot.EndTime AS LotEndTime, HistoryProcessWafer.ChamberNo, 
            //                          HistoryProcessWafer.SlotID, HistoryProcessWafer.StartTime, 
            //                          HistoryProcessWafer.EndTime
            //                    FROM  (HistoryProcessLot INNER JOIN
            //                          HistoryProcessWafer ON 
            //                          HistoryProcessLot.CJID = HistoryProcessWafer.CJID AND 
            //                          HistoryProcessLot.LotID = HistoryProcessWafer.LotID AND
            //                          HistoryProcessLot.StartTime < HistoryProcessWafer.StartTime AND
            //                          HistoryProcessLot.EndTime > HistoryProcessWafer.EndTime)                            
            //                    WHERE (HistoryProcessLot.StartTime >= #{0}#) AND 
            //                          (HistoryProcessLot.EndTime <= #{1}#)
            //                    ORDER BY  HistoryProcessLot.StartTime DESC"
            //                     , dtpStartTime.Value.ToString("yyyy/MM/dd 00:00:00")
            //                     , dtpEndTime.Value.ToString("yyyy/MM/dd 23:59:59")));

            //                if (csLogger.DataExport(Db.Tables[0]))
            //                {
            //                    frmMessageBox.Show("Info_MsgDBExportOK", "Notice", frmMessageBox.CYButtons.OK, frmMessageBox.CYIcon.Question, "");
            //                    csDB.WriteEventTable("Export Log Process - " + dtpStartTime.Value.ToString("yyyy/MM/dd HH:mm:ss") + " ~ " + dtpEndTime.Value.ToString("yyyy/MM/dd HH:mm:ss"));
            //                }
            try
            {
                //if (dsGet.Tables[0].Rows.Count > 0)
                //    _dbLog.DataExport(dsGet.Tables[0]);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }

        //========= 
        private void DeterminePageBoundaries()
        {
            try
            {
                int TotalRowCount = dsGet.Tables[0].Rows.Count;

                int pageRows = PageCount;
                int pages = 0;

                if (pageRows < TotalRowCount)
                {
                    if ((TotalRowCount % pageRows) > 0)
                        pages = ((TotalRowCount / pageRows) + 1);
                    else
                        pages = TotalRowCount / pageRows;
                }
                else
                {
                    pages = 1;
                    dataGridView1.DataSource = dsGet.Tables[0];
                }

                int LowerBoundary = 0;
                int UpperBoundary = 0;

                switch (NavClicked.Text)
                {
                    case "First":
                        //First clicked, the Current Page will always be 1
                        CurrentPage = 1;
                        //The LowerBoundary will thus be ((50 * 1) - (50 - 1)) = 1
                        LowerBoundary = ((pageRows * CurrentPage) - (pageRows - 1));

                        if (pageRows < TotalRowCount)
                            UpperBoundary = (pageRows * CurrentPage);
                        else
                            UpperBoundary = TotalRowCount;

                        dataGridView1.DataSource = dsGet.Tables[0].AsEnumerable().Skip(LowerBoundary - 1).Take(pageRows).CopyToDataTable();

                        break;

                    case "Last":
                        CurrentPage = pages;
                        LowerBoundary = ((pageRows * CurrentPage) - (pageRows - 1));

                        UpperBoundary = TotalRowCount;

                        dataGridView1.DataSource = dsGet.Tables[0].AsEnumerable().Skip(LowerBoundary - 1).Take(pageRows).CopyToDataTable();

                        break;

                    case "Next":
                        //Next clicked
                        if (CurrentPage != pages)
                            CurrentPage += 1;

                        LowerBoundary = ((pageRows * CurrentPage) - (pageRows - 1));

                        if (CurrentPage == pages)
                            UpperBoundary = TotalRowCount;
                        else
                            UpperBoundary = (pageRows * CurrentPage);

                        dataGridView1.DataSource = dsGet.Tables[0].AsEnumerable().Skip(LowerBoundary - 1).Take(pageRows).CopyToDataTable();

                        break;

                    case "Previous":
                        //Previous clicked
                        if (CurrentPage != 1)
                        {
                            CurrentPage -= 1;
                        }
                        LowerBoundary = ((pageRows * CurrentPage) - (pageRows - 1));

                        if (pageRows < TotalRowCount)
                            UpperBoundary = (pageRows * CurrentPage);
                        else
                            UpperBoundary = TotalRowCount;

                        dataGridView1.DataSource = dsGet.Tables[0].AsEnumerable().Skip(LowerBoundary - 1).Take(pageRows).CopyToDataTable();

                        break;

                    default:
                        //No button was clicked.
                        CurrentPage = 1;
                        LowerBoundary = ((pageRows * CurrentPage) - (pageRows - 1));

                        if (pageRows < TotalRowCount)
                            UpperBoundary = (pageRows * CurrentPage);
                        else
                            UpperBoundary = TotalRowCount;

                        dataGridView1.DataSource = dsGet.Tables[0].AsEnumerable().Skip(LowerBoundary - 1).Take(pageRows).CopyToDataTable();

                        break;
                }
                //取消行排序
                int[] Widths = new int[] { 145, 200, 200, 180, 180 };
                //取消行排序
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    //dataGridView1.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                    this.dataGridView1.Columns[i].Width = Widths[i];
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        //========= 

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DataGridViewRow dgvRow = dataGridView1.Rows[e.RowIndex];
                string SoruceCarrierID = string.Empty;
                string CJID = string.Empty;
                string PJID = string.Empty;
                string strSQL = string.Empty;
                string RecipeID = string.Empty;
                string PanelID = string.Empty;
                int slot = 0;
                int[] Widths;
                switch (CurrentType)
                {
                    case LogType.Lot:
                        SetCurrentType = LogType.wafer;

                        SoruceCarrierID = (string)dgvRow.Cells[0].Value;
                        CJID = (string)dgvRow.Cells[1].Value;
                        PJID = (string)dgvRow.Cells[2].Value;

                        dataGridView1.DataSource = null;
                        dataGridView1.Columns.Clear();

                        //查詢格不為空時
                        dsGet = _dbLog.GetProcessWafer(SoruceCarrierID, CJID, PJID);
                        dataGridView1.DataSource = dsGet.Tables[0];

                        Widths = new int[] { 160, 100, 50, 160, 160, 160, 160 };
                        for (int i = 0; i < dataGridView1.Columns.Count; i++)
                        {
                            //this.dataGridView1.Columns[i].Width = Widths[i];
                            this.dataGridView1.Columns[i].Width = dataGridView1.Width / dataGridView1.Columns.Count;
                        }
                        break;
                    case LogType.wafer:
                        SetCurrentType = LogType.station;

                        CJID = (string)dgvRow.Cells[1].Value;
                        PJID = (string)dgvRow.Cells[2].Value;
                        RecipeID = (string)dgvRow.Cells[3].Value;
                        slot = int.Parse((string)dgvRow.Cells[4].Value);

                        dataGridView1.DataSource = null;
                        dataGridView1.Columns.Clear();
                        //查詢格不為空時
                        dsGet = _dbLog.GetProcessWaferStation(CJID, PJID, RecipeID, slot);
                        dataGridView1.DataSource = dsGet.Tables[0];

                        Widths = new int[] { 200, 100, 100, 100, 450 };
                        for (int i = 0; i < dataGridView1.Columns.Count; i++)
                        {
                            this.dataGridView1.Columns[i].Width = Widths[i];
                            //this.dataGridView1.Columns[i].Width = dataGridView1.Width / dataGridView1.Columns.Count;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        //========= 
        private void bindingNavigatorMoveFirstItem_Click(object sender, EventArgs e)
        {
            try
            {
                NavClicked = sender as ToolStripButton;
                DeterminePageBoundaries();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void bindingNavigatorMovePreviousItem_Click(object sender, EventArgs e)
        {
            try
            {
                NavClicked = sender as ToolStripButton;
                DeterminePageBoundaries();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void bindingNavigatorMoveNextItem_Click(object sender, EventArgs e)
        {
            try
            {
                NavClicked = sender as ToolStripButton;
                DeterminePageBoundaries();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void bindingNavigatorMoveLastItem_Click(object sender, EventArgs e)
        {
            try
            {
                NavClicked = sender as ToolStripButton;
                DeterminePageBoundaries();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void bindingNavigatorPositionItem_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyValue == 13)
                {
                    int InputValue = 0;
                    string value = sender.ToString();
                    if (int.TryParse(value, out InputValue))
                    {
                        CurrentPage = int.Parse(value);
                        NavClicked = new ToolStripButton();
                        DeterminePageBoundaries();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }

        private void dtpStartTime_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                var Diff = DateTime.Parse(dtpEndTime.Text).Subtract(DateTime.Parse(dtpStartTime.Text));
                if (Diff.Days > DayRange)
                {
                    dtpEndTime.Text = DateTime.Parse(dtpStartTime.Text).AddDays(DayRange).ToString();
                }

                if (Diff.Days < 0)
                {
                    dtpEndTime.Text = DateTime.Parse(dtpStartTime.Text).AddDays(DayRange).ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void dtpEndTime_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                var Diff = DateTime.Parse(dtpEndTime.Text).Subtract(DateTime.Parse(dtpStartTime.Text));
                if (Diff.Days > DayRange)
                {
                    dtpStartTime.Text = DateTime.Parse(dtpEndTime.Text).AddDays(-DayRange).ToString();
                }

                if (Diff.Days < 0)
                {
                    dtpStartTime.Text = DateTime.Parse(dtpEndTime.Text).AddDays(-DayRange).ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        //========= 
        private void frmLogProcess_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible)
                {
                    dtpStartTime.Value = DateTime.Now.AddDays(-1);
                    txtKeyWork.Text = string.Empty;
                    dataGridView1.DataSource = null;
                    dataGridView1.Columns.Clear();
                    this.dataGridView1.AllowUserToAddRows = false;
                    this.btnSearch.PerformClick();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }



    }
}