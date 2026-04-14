using RorzeApi.Class;
using RorzeComm.Log;
using RorzeUnit.Class.ADAM;
using RorzeUnit.Class.ADAM.Event;
using RorzeUnit.Class.FFU;
using RorzeUnit.Class.SafetyIOStatus;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace RorzeApi
{
    public partial class frmIO : Form
    {
        float frmX;             //當前窗體的寬度
        float frmY;             //當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        bool _bSimulate;
        SPermission _permission;
        List<I_RC5X0_IO> RC5X0IOList;
        SafetyIOStatus m_SafetyIO;
        List<SSFFUCtrlParents> m_listFFU;
        List<ADAM6066> m_listADAM6066;

        SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        public frmIO(List<I_RC5X0_IO> dioList, List<ADAM6066> adam6066, SafetyIOStatus safetyIO, List<SSFFUCtrlParents> listFFU, SPermission permission)
        {
            try
            {
                InitializeComponent();
                _bSimulate = GParam.theInst.IsSimulate;
                _permission = permission;
                RC5X0IOList = dioList;
                m_SafetyIO = safetyIO;
                m_listADAM6066 = adam6066;
                m_listFFU = listFFU;
                foreach (ADAM6066 item in adam6066)
                {
                    OnOccurADAMIOChange(item, new IOAdam6066DataEventArgs(item.bDo, item.getInputValue()));
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
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

        //==================================== 
        private void dgvIO_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex != 0 || e.RowIndex < 0) return;//防呆
                if (_permission.MaintenanceEnable == false) return;
                //Check whether the status of the machine can be triggered
                switch (((DataGridView)sender).Name)
                {
                    case "dgvIN":
                        {
                            if (!_bSimulate) return;
                            int n1 = e.RowIndex / 8;
                            int n2 = e.RowIndex % 8;

                            int nBodyNoIndex = cbModules.Text.IndexOf("DIO");
                            string strBodyNo = cbModules.Text.Substring(nBodyNoIndex + 3, 1);
                            int nBodyNo = int.Parse(strBodyNo);

                            int nHCLID_Index = cbModules.Text.IndexOf("_");
                            string strHCLID_Index = cbModules.Text.Substring(nHCLID_Index + 1);
                            int nHCLID = int.Parse(strHCLID_Index);

                            I_RC5X0_IO dio = RC5X0IOList[nBodyNo];
                            dio.SetGDIO_InputStatus(nHCLID, e.RowIndex, !dio.GetGDIO_InputStatus(nHCLID, e.RowIndex));
                            break;
                        }
                    case "dgvOUT":
                        {
                            //IO point and get the current state of the ON OFF                        
                            int nBodyNoIndex = cbModules.Text.IndexOf("DIO");
                            string strBodyNo = cbModules.Text.Substring(nBodyNoIndex + 3, 1);
                            int nBodyNo = int.Parse(strBodyNo);

                            int nHCLID_Index = cbModules.Text.IndexOf("_");
                            string strHCLID_Index = cbModules.Text.Substring(nHCLID_Index + 1);
                            int nHCLID = int.Parse(strHCLID_Index);

                            I_RC5X0_IO dio = RC5X0IOList[nBodyNo];
                            bool bn = dio.GetGDIO_OutputStatus(nHCLID, e.RowIndex) == true ? false : true;
                            dio.SdobW(nHCLID, e.RowIndex, bn);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
        private void dgvIO_ADAM_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex != 0 || e.RowIndex < 0) return;//防呆
                if (_permission.MaintenanceEnable == false) return;
                //Check whether the status of the machine can be triggered
                switch (((DataGridView)sender).Name)
                {
                    case "dgvIN_ADAM":
                        {
                            if (!_bSimulate) return;
                            int n1 = e.RowIndex / 8;
                            int n2 = e.RowIndex % 8;

                            int nBodyNoIndex = ADAMcbModules.Text.IndexOf("IO");
                            string strBodyNo = ADAMcbModules.Text.Substring(nBodyNoIndex + 2, 1);
                            int nBodyNo = int.Parse(strBodyNo);

                            ADAM6066 ADAMdio = m_listADAM6066[nBodyNo];
                            ADAMdio.setInputValue(e.RowIndex, !ADAMdio.getInputValue(e.RowIndex));
                            break;
                        }
                    case "dgvOUT_ADAM":
                        {
                            //IO point and get the current state of the ON OFF                        
                            int nBodyNoIndex = ADAMcbModules.Text.IndexOf("IO");
                            string strBodyNo = ADAMcbModules.Text.Substring(nBodyNoIndex + 2, 1);
                            int nBodyNo = int.Parse(strBodyNo);

                            ADAM6066 ADAMdio = m_listADAM6066[nBodyNo];
                            bool bn = ADAMdio.getOutputValue(e.RowIndex) == true ? false : true;
                            ADAMdio.setOutputValue(e.RowIndex, bn);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
        //==================================== 
        private void OnOccurIOChange(object sender, RorzeUnit.Class.RC500.Event.NotifyGDIOEventArgs e)
        {
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    I_RC5X0_IO dio = sender as I_RC5X0_IO;
                    int nBodyNo = dio.BodyNo;
                    if (cbModules.Text.Contains(string.Format("DIO{0}_{1:D3}", nBodyNo, e.HCLID)))
                    {
                        for (int i = 0; i < dgvIN.RowCount; i++)
                        {
                            dgvIN.Rows[i].Cells[0].Value = e.Input[i] ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
                        }
                        for (int i = 0; i < dgvOUT.RowCount; i++)
                        {
                            dgvOUT.Rows[i].Cells[0].Value = e.Output[i] ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
        private void OnOccurADAMIOChange(object sender, RorzeUnit.Class.ADAM.Event.IOAdam6066DataEventArgs e)
        {
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    ADAM6066 ADAMdio = sender as ADAM6066;
                    int nBodyNo = ADAMdio._BodyNo;
                    if (ADAMcbModules.Text.Contains(string.Format("ADAMIO{0}", nBodyNo)))
                    {
                        for (int i = 0; i < dgvIN_ADAM.RowCount; i++)
                        {
                            dgvIN_ADAM.Rows[i].Cells[0].Value = e.Input[i] ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
                        }
                        for (int i = 0; i < dgvOUT_ADAM.RowCount; i++)
                        {
                            dgvOUT_ADAM.Rows[i].Cells[0].Value = e.Output[i] ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
        private void OnOccurPLCIOChange(object sender, RorzeUnit.Class.RC500.Event.NotifyGDIOEventArgs e)
        {
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    SafetyIOStatus dio = sender as SafetyIOStatus;

                    int i = e.HCLID;

                    for (int j = 0; j < e.Input.Length; j++)
                    {
                        Label label = tlpPLC.GetControlFromPosition(j, i) as Label;
                        if (label != null)
                            label.BackColor = e.Input[j] ? Color.Green : SystemColors.Control;
                    }

                }));
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
        //==================================== 
        private void FanBar_MouseUp(object sender, MouseEventArgs e)
        {
            FanBar.Enabled = false;
            try
            {
                Task.Run(() =>
                {
                    int nFFU = FanBar.Value;

                    if (RC5X0IOList[0].Disable == false)
                    {
                        RC5X0IOList[0].MoveW(FanBar.Value > 0 ? nFFU : 0);
                        GParam.theInst.SetFanDefaultSpeed(nFFU);
                    }

                    foreach (SSFFUCtrlParents ffu in m_listFFU)
                    {
                        if (ffu == null || ffu._Disable) continue;

                   
                        if (FanBar.Value > 0)
                        {
                            for(int i = 0; i < GParam.theInst.GetFfuFanCount(0); i++)
                            {
                                ffu.SetSpeedSetting(i + 1, FanBar.Value);
                            }

                            //ffu.SetOperationCtrl(1, FanBar.Value > 0);
                        }
                        else
                        {
                            //ffu.SetOperationCtrl(1, false);
                        }
                    }

                });

            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
            finally
            {
                FanBar.Enabled = true;
            }
        }
        private void FanBar_Scroll(object sender, EventArgs e)
        {
            try
            {
                int nFFU = FanBar.Value;
                txtFFU.Text = nFFU.ToString();
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
        //==================================== 
        private void frmIO_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = this.Visible;

                if (this.Visible)
                {
                    GParam.theInst.SetIO_Flog(true);    //  MDI tmrUI_Tick停止更新畫面

                    txtFFU.Text = GParam.theInst.GetFanDefaultSpeed.ToString();
                    FanBar.Value = GParam.theInst.GetFanDefaultSpeed;
                    RC5X0IOList[0].OnOccurGPRS += _rc550_0_OnOccurSensorChange;

                    if (m_SafetyIO != null)
                        if (m_SafetyIO._Disable)
                            tabPagePLC.Parent = null;
                        else if (tlpPLC.RowCount != m_SafetyIO._TotalModules)
                        {

                            tlpPLC.Dock = DockStyle.Fill;
                            tlpPLC.Controls.Clear();
                            tlpPLC.RowStyles.Clear();
                            tlpPLC.ColumnStyles.Clear();
                            tlpPLC.RowCount = m_SafetyIO._TotalModules;//用於表單建立層數
                            tlpPLC.ColumnCount = 8;//一個byte（位元組）由8個bit（位元）組成

                            for (int i = 0; i < tlpPLC.RowCount; i++)
                            {
                                tlpPLC.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                            }
                            for (int i = 0; i < tlpPLC.ColumnCount; i++)
                            {
                                tlpPLC.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                            }
                            for (int i = 0; i < tlpPLC.RowCount; i++)//0~19
                            {
                                for (int j = 0; j < tlpPLC.ColumnCount; j++)//0~7
                                {
                                    Label labelSlot = new Label();
                                    labelSlot.Text = GParam.theInst.GetPLC_DIName(i)[j];
                                    labelSlot.Dock = DockStyle.Fill;
                                    labelSlot.TextAlign = ContentAlignment.MiddleCenter;
                                    labelSlot.BorderStyle = BorderStyle.FixedSingle;
                                    labelSlot.Margin = new Padding(5);
                                    tlpPLC.Controls.Add(labelSlot, j, i);
                                }
                            }
                            m_SafetyIO.OnNotifyEvntGDIO -= OnOccurPLCIOChange;
                            m_SafetyIO.OnNotifyEvntGDIO += OnOccurPLCIOChange;
                        }


                }
                else
                {
                    GParam.theInst.SetIO_Flog(false);   //  MDI tmrUI_Tick繼續更新畫面
                    RC5X0IOList[0].OnOccurGPRS -= _rc550_0_OnOccurSensorChange;
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }

        void _rc550_0_OnOccurSensorChange(object sender, int[] nValue)
        {
            GPRS_Pa.Text = Convert.ToDouble(nValue[0]) / 1000 + " Kpa";
        }

        private void frmIO_Load(object sender, EventArgs e)
        {
            try
            {
                foreach (I_RC5X0_IO item in RC5X0IOList)
                {
                    if (item.Disable) continue;
                    item.OnNotifyEvntGDIO -= OnOccurIOChange;
                    item.OnNotifyEvntGDIO += OnOccurIOChange;

                    foreach (int nHCL in GParam.theInst.GetDIO_HCL(item.BodyNo))
                    {
                        cbModules.Items.Add(string.Format("DIO{0}_{1:D3}", item.BodyNo, nHCL));
                    }
                }
                if (cbModules.Items.Count > 0) { cbModules.SelectedIndex = 0; }

                if (RC5X0IOList[0].Disable == false)
                {
                    if (GParam.theInst.RC550ctrlFFU)
                    {
                        FanBar.Visible = txtFFU.Visible = gbxFFU.Visible = lblFFUrpm.Visible = true;
                        FanBar.Minimum = 300;
                        FanBar.Maximum = 1600;
                    }

                    if (GParam.theInst.RC550Pressure_Enable)
                    {
                        GPRS_Pa.Visible = true;
                    }
                    else
                    {
                        GPRS_Pa.Visible = false;
                    }
                }

                foreach (SSFFUCtrlParents ffu in m_listFFU)
                {
                    if (ffu == null || ffu._Disable) continue;

                    FanBar.Visible = txtFFU.Visible = gbxFFU.Visible = lblFFUrpm.Visible = true;

                    FanBar.Minimum = 0/*ffu.GetSpeedMin()[0]*/;
                    FanBar.Maximum = ffu.GetSpeedMax()[0];
                    break;
                }

                foreach (ADAM6066 item in m_listADAM6066)
                {
                    if (item.Disable) continue;
                    item.OnNotifyAdamIO -= OnOccurADAMIOChange;
                    item.OnNotifyAdamIO += OnOccurADAMIOChange;
                    foreach (int nHCL in GParam.theInst.GetAdamDIO_HCL(item._BodyNo))
                    {
                        ADAMcbModules.Items.Add(string.Format("ADAMIO{0}_{1:D3}", item._BodyNo, nHCL));
                    }
                }
                if (ADAMcbModules.Items.Count > 0) { ADAMcbModules.SelectedIndex = 0; }




            }
            catch (Exception ex) { WriteLog("<Exception>" + ex); }
        }

        private void cbModules_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //缺頁面切換的時候，畫面的燈號值要反應現值

                dgvIN.DataSource = null;
                dgvIN.ColumnCount = 3;                             // 定義所需要的行數
                dgvIN.Columns[1].Name = "Bit";
                dgvIN.Columns[2].Name = "Name";
                dgvIN.Dock = DockStyle.Fill;

                dgvOUT.DataSource = null;
                dgvOUT.ColumnCount = 3;                             // 定義所需要的行數
                dgvOUT.Columns[1].Name = "Bit";
                dgvOUT.Columns[2].Name = "Name";
                dgvOUT.Dock = DockStyle.Fill;

                int nBitNumber = 16;// input 16bit , output 16bit
                Bitmap iconOnOff = new Bitmap(Properties.Resources.LightOff);
                while (dgvIN.Rows.Count < nBitNumber)
                {
                    dgvIN.Rows.Add(new object[] { iconOnOff, "", "" });
                }
                while (dgvOUT.Rows.Count < nBitNumber)
                {
                    dgvOUT.Rows.Add(new object[] { iconOnOff, "", "" });
                }

                int nBodyNoIndex = cbModules.Text.IndexOf("DIO");
                string strBodyNo = cbModules.Text.Substring(nBodyNoIndex + 3, 1);
                int nBodyNo = int.Parse(strBodyNo);

                int nHCLID_Index = cbModules.Text.IndexOf("_");
                string strHCLID_Index = cbModules.Text.Substring(nHCLID_Index + 1);
                int nHCLID = int.Parse(strHCLID_Index);

                I_RC5X0_IO dio = RC5X0IOList[nBodyNo];
                for (int i = 0; i < nBitNumber; i++)
                {
                    dgvIN.Rows[i].Cells[1].Value = i;
                    dgvIN.Rows[i].Cells[2].Value = GParam.theInst.GetDIO_DIName(nBodyNo, nHCLID)[i];
                    dgvIN.Rows[i].Cells[0].Value = dio.GetGDIO_InputStatus(nHCLID, i) ?
                        Properties.Resources.LightGreen : Properties.Resources.LightOff;

                    dgvOUT.Rows[i].Cells[1].Value = i;
                    dgvOUT.Rows[i].Cells[2].Value = GParam.theInst.GetDIO_DOName(nBodyNo, nHCLID)[i];
                    dgvOUT.Rows[i].Cells[0].Value = dio.GetGDIO_OutputStatus(nHCLID, i) ?
                        Properties.Resources.LightGreen : Properties.Resources.LightOff;
                }

                dgvIN.Columns[0].Width = dgvIN.Width * 3 / 20;
                dgvIN.Columns["Bit"].Width = dgvIN.Width * 2 / 20;
                dgvIN.Columns["Name"].Width = dgvIN.Width * 15 / 20;

                dgvOUT.Columns[0].Width = dgvOUT.Width * 3 / 20;
                dgvOUT.Columns["Bit"].Width = dgvOUT.Width * 2 / 20;
                dgvOUT.Columns["Name"].Width = dgvOUT.Width * 15 / 20;

                //取消行排序
                for (int i = 0; i < dgvIN.Columns.Count; i++)
                {
                    dgvIN.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                //取消行排序
                for (int i = 0; i < dgvOUT.Columns.Count; i++)
                {
                    dgvOUT.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }

        private void ADAMcbModules_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //缺頁面切換的時候，畫面的燈號值要反應現值

                dgvIN_ADAM.DataSource = null;
                dgvIN_ADAM.ColumnCount = 3;                             // 定義所需要的行數
                dgvIN_ADAM.Columns[1].Name = "Bit";
                dgvIN_ADAM.Columns[2].Name = "Name";
                dgvIN_ADAM.Dock = DockStyle.Fill;

                dgvOUT_ADAM.DataSource = null;
                dgvOUT_ADAM.ColumnCount = 3;                             // 定義所需要的行數
                dgvOUT_ADAM.Columns[1].Name = "Bit";
                dgvOUT_ADAM.Columns[2].Name = "Name";
                dgvOUT_ADAM.Dock = DockStyle.Fill;

                int nBitNumber = 6;// input 6bit , output 6bit
                Bitmap iconOnOff = new Bitmap(Properties.Resources.LightOff);
                while (dgvIN_ADAM.Rows.Count < nBitNumber)
                {
                    dgvIN_ADAM.Rows.Add(new object[] { iconOnOff, "", "" });
                }
                while (dgvOUT_ADAM.Rows.Count < nBitNumber)
                {
                    dgvOUT_ADAM.Rows.Add(new object[] { iconOnOff, "", "" });
                }

                int nBodyNoIndex = ADAMcbModules.Text.IndexOf("IO");
                string strBodyNo = ADAMcbModules.Text.Substring(nBodyNoIndex + 2, 1);
                int nBodyNo = int.Parse(strBodyNo);
                int nHCLID = 0;

                ADAM6066 ADAMdio = m_listADAM6066[nBodyNo];
                for (int i = 0; i < nBitNumber; i++)
                {
                    if (ADAMdio.getInputValue(i))
                        dgvIN_ADAM.Rows[i].Cells[0].Value = Properties.Resources.LightGreen;
                    else
                        dgvIN_ADAM.Rows[i].Cells[0].Value = Properties.Resources.LightOff;

                    if (ADAMdio.getOutputValue(i))
                        dgvOUT_ADAM.Rows[i].Cells[0].Value = Properties.Resources.LightGreen;
                    else
                        dgvOUT_ADAM.Rows[i].Cells[0].Value = Properties.Resources.LightOff;

                    dgvIN_ADAM.Rows[i].Cells[1].Value = i;
                    dgvIN_ADAM.Rows[i].Cells[2].Value = GParam.theInst.GetAdamIO_DIName(nBodyNo, nHCLID)[i];

                    dgvOUT_ADAM.Rows[i].Cells[1].Value = i;
                    dgvOUT_ADAM.Rows[i].Cells[2].Value = GParam.theInst.GetAdamIO_DOName(nBodyNo, nHCLID)[i];
                }

                dgvIN_ADAM.Columns[0].Width = dgvIN_ADAM.Width * 3 / 20;
                dgvIN_ADAM.Columns["Bit"].Width = dgvIN_ADAM.Width * 2 / 20;
                dgvIN_ADAM.Columns["Name"].Width = dgvIN_ADAM.Width * 15 / 20;

                dgvOUT_ADAM.Columns[0].Width = dgvOUT_ADAM.Width * 3 / 20;
                dgvOUT_ADAM.Columns["Bit"].Width = dgvOUT_ADAM.Width * 2 / 20;
                dgvOUT_ADAM.Columns["Name"].Width = dgvOUT_ADAM.Width * 15 / 20;

                //取消行排序
                for (int i = 0; i < dgvIN_ADAM.Columns.Count; i++)
                {
                    dgvIN_ADAM.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                //取消行排序
                for (int i = 0; i < dgvOUT_ADAM.Columns.Count; i++)
                {
                    dgvOUT_ADAM.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPageRC5X0)
            {

            }
            else if (tabControl1.SelectedTab == tabPagePLC)
            {
                for (int i = 0; i < tlpPLC.RowCount; i++)//0~19
                {
                    for (int j = 0; j < tlpPLC.ColumnCount; j++)//0~7
                    {
                        Label label = tlpPLC.GetControlFromPosition(j, i) as Label;
                        if (label != null)
                            label.BackColor = m_SafetyIO.GetGDIO_InputStatus(i, j) ? Color.Green : SystemColors.Control;
                    }
                }
            }
        }



        private void timer1_Tick_1(object sender, EventArgs e)
        {
            lblFFUrpm.Text = "Speed(rpm) :";
            foreach (SSFFUCtrlParents ffu in m_listFFU)
            {
                if (ffu == null || ffu._Disable) continue;

                foreach (int item in ffu.GetSpeed())
                    lblFFUrpm.Text += " " + item + "";
                lblFFUrpm.Text += "\r\n";
            }

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < m_listADAM6066.Count; i++)
                {
                    int nBodyNo = i + 1;
                    ADAM6066 ADAMdio = m_listADAM6066[i];
                    if (ADAMcbModules.Text.Contains(string.Format("IO{0}", nBodyNo)))
                    {
                        for (int j = 0; j < dgvIN_ADAM.RowCount; j++)
                        {
                            dgvIN_ADAM.Rows[j].Cells[0].Value = ADAMdio.getInputValue(j) ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
                        }
                        for (int j = 0; j < dgvOUT_ADAM.RowCount; j++)
                        {
                            dgvOUT_ADAM.Rows[j].Cells[0].Value = ADAMdio.getOutputValue(j) ? Properties.Resources.LightGreen : Properties.Resources.LightOff;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
    }
}
