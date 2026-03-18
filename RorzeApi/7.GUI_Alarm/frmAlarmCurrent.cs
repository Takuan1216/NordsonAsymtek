using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using RorzeApi.Class;
using RorzeUnit.Interface;
using RorzeUnit;
using RorzeComm.Log;
using RorzeUnit.Class;
using System.Collections.Generic;

namespace RorzeApi
{
    public partial class frmAlarmCurrent : Form, IDisposable
    {
        float frmX;//當前窗體的寬度
        float frmY;//當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private SAlarm _alarm;
        private SPermission _userManager;   //  管理LOGIN使用者權限

        public frmAlarmCurrent(SAlarm alarm, SPermission userManager)
        {
            try
            {
                InitializeComponent();
                _userManager = userManager;
                _alarm = alarm;
                _alarm.OnAlarmOccurred += new SAlarm.AlarmEventHandler(_alarm_OnAlarm);
                _alarm.OnAlarmRemove += new SAlarm.AlarmEventHandler(_alarm_OnAlarmRset);

                //去搜尋一下已經發生過的異常
                if (_alarm.CurrentAlarm.Count > 0)
                {
                    foreach (SAlarm.CurrentAlarmItem item in _alarm.CurrentAlarm)
                    {
                        _alarm_OnAlarm(new AlarmEventArgs(item.AlarmID, item.Type, item.UnitType, item.AlarmMsg, item.CreateTime));
                    }
                }

                if (GParam.theInst.FreeStyle)
                {
                    btnBuzzer.Image = Properties.Resources._32_mute_;
                }
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
        private void btnBuzzer_Click(object sender, EventArgs e)
        {
            try
            {
                _alarm.AlarmBuzzerOff();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            try
            {

                if (_userManager.IsLogin == false)
                {
                    new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }

                btnReset.Enabled = false;
                _alarm.exeAlarmReset.Set();
                btnReset.Enabled = true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        //========= 
        private void frmAlarmCurrent_VisibleChanged(object sender, EventArgs e)
        {

        }

        //========= 
        delegate void UpdateTable(AlarmEventArgs args);
        void _alarm_OnAlarm(AlarmEventArgs args)
        {
            try
            {
                if (InvokeRequired)
                {
                    UpdateTable dlg = new UpdateTable(_alarm_OnAlarm);
                    this.BeginInvoke(dlg, args);
                }
                else
                {
                    string[] rowData = new string[] { args.CreateTime.ToString(), args.Type, args.UnitType, args.AlarmID.ToString("D9"), args.AlarmMsg };
                    dataGridView1.Rows.Add(rowData);
                    int[] Widths = new int[] { 180, 95, 95, 95, 390 };
                    //取消行排序
                    for (int i = 0; i < dataGridView1.Columns.Count; i++)
                    {
                        this.dataGridView1.Columns[i].Width = Widths[i];
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }
        void _alarm_OnAlarmRset(AlarmEventArgs args)
        {
            try
            {
                if (InvokeRequired)
                {
                    UpdateTable dlg = new UpdateTable(_alarm_OnAlarmRset);
                    this.BeginInvoke(dlg, args);
                }
                else
                {
                    foreach (DataGridViewRow item in this.dataGridView1.Rows)
                    {
                        if (item.Cells["AlarmCode"].Value.ToString() == args.AlarmID.ToString())
                        {
                            dataGridView1.Rows.RemoveAt(item.Index);
                        }
                    }

                    int[] Widths = new int[] { 180, 95, 95, 95, 390 };

                    //取消行排序
                    for (int i = 0; i < dataGridView1.Columns.Count; i++)
                    {
                        this.dataGridView1.Columns[i].Width = Widths[i];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(this.Name, ex);
            }
        }


    }
}