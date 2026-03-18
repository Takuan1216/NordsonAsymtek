using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using RorzeComm.Log;
using RorzeUnit.Class;
using RorzeApi.Class;

namespace RorzeApi
{
    public partial class frmLogAlarm : Form
    {
        float frmX;//當前窗體的寬度
        float frmY;//當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        private SProcessDB _accessDBlog;
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        int PageCount = 100;
        int CurrentPage = 1;
        int DayRange = 3;
        ToolStripButton NavClicked = new ToolStripButton();
        DataSet dsGet = new DataSet();

        private SPermission _permission;

        public frmLogAlarm(SProcessDB accessDBlog, SPermission permission)
        {
            try
            {
                InitializeComponent();

                _accessDBlog = accessDBlog;
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
        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                //if (dsGet.Tables[0].Rows.Count > 0)
                //    _db.DataExport(dsGet.Tables[0]);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                //查詢格不為空時
                //if (txtKeyWork.Text != string.Empty)
                //    dsGet = _db.SelectMessage(txtKeyWork.Text.Trim(), dtpStartTime.Value.ToString("yyyy/MM/dd"), dtpEndTime.Value.ToString("yyyy/MM/dd"));
                //else
                //查詢為空時只做時間搜尋
                dsGet = _accessDBlog.SelectAlarmLogOccurTime(dtpStartTime.Value.ToString("yyyy/MM/dd"), dtpEndTime.Value.ToString("yyyy/MM/dd"));

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
                    pages = 1;

                int LowerBoundary = 0;
                int UpperBoundary = 0;

                switch (NavClicked.Text)
                {
                    case "First":
                        CurrentPage = 1;
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
                        if (CurrentPage != 1)
                            CurrentPage -= 1;
                        LowerBoundary = ((pageRows * CurrentPage) - (pageRows - 1));

                        if (pageRows < TotalRowCount)
                            UpperBoundary = (pageRows * CurrentPage);
                        else
                            UpperBoundary = TotalRowCount;

                        dataGridView1.DataSource = dsGet.Tables[0].AsEnumerable().Skip(LowerBoundary - 1).Take(pageRows).CopyToDataTable();
                        break;
                    default:
                        LowerBoundary = ((pageRows * CurrentPage) - (pageRows - 1));

                        if (pageRows < TotalRowCount)
                            UpperBoundary = (pageRows * CurrentPage);
                        else
                            UpperBoundary = TotalRowCount;

                        dataGridView1.DataSource = dsGet.Tables[0].AsEnumerable().Skip(LowerBoundary - 1).Take(pageRows).CopyToDataTable();
                        break;
                }

                //取消行排序
                int[] Widths = new int[] { 180, 100, 100, 100, 400 };
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
        private void frmLogAlarm_VisibleChanged(object sender, EventArgs e)
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
                    bindingSource1.DataSource = null;
                    this.btnSearch.PerformClick();                
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }

        private void frmLogAlarm_Load(object sender, EventArgs e)
        {
            try
            {
                dtpStartTime.Value = DateTime.Now.AddDays(-1);

                this.btnSearch.PerformClick();
                this.dataGridView1.AllowUserToAddRows = false;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }


        //========= 
    }
}