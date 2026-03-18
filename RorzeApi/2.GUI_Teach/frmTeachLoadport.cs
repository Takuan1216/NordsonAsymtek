using System;
using System.Drawing;
using System.Windows.Forms;
using RorzeApi.Class;

using System.Drawing.Drawing2D;
using RorzeUnit.Interface;
using RorzeUnit.Class.Robot.Enum;
using RorzeComm.Log;
using RorzeUnit.Class;
using RorzeUnit.Class.Robot;
using System.Runtime.CompilerServices;

namespace RorzeApi
{
    public partial class frmTeachLoadport : Form
    {
        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion


        private float frmX;     //當前窗體的寬度
        private float frmY;     //當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }
        private I_Loadport _load;

        private bool _bSimulate;

        private SProcessDB _accessDBlog;
        private SPermission _userManager;   //  管理LOGIN使用者權限
        private string _strUserName;        //  登入者名稱

        private ComboBox[] m_cbFoupType = new ComboBox[16];
        private CheckBox[] m_cbkTypeEnable = new CheckBox[16];

        public frmTeachLoadport(I_Loadport load, bool simulate, SProcessDB db, SPermission userManager)
        {
            InitializeComponent();

            _load = load;
            _bSimulate = simulate;

            _accessDBlog = db;
            _userManager = userManager;

            DataGridViewRowCollection myRows = dgvStage.Rows;
            while (myRows.Count < 10)
            {
                myRows.Add(new object[] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" });
            }

            dgvStage.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvStage.Rows[0].HeaderCell.Value = "Carrier ID";
            dgvStage.Rows[1].HeaderCell.Value = "Clmp parm";
            dgvStage.Rows[2].HeaderCell.Value = "Map flag";
            dgvStage.Rows[3].HeaderCell.Value = "Clmp flag";
            dgvStage.Rows[4].HeaderCell.Value = "Slot No";
            dgvStage.Rows[5].HeaderCell.Value = "Slot pitch";
            dgvStage.Rows[6].HeaderCell.Value = "Thick Min";
            dgvStage.Rows[7].HeaderCell.Value = "Thick Max";
            dgvStage.Rows[8].HeaderCell.Value = "Height Offset";
            dgvStage.Rows[9].HeaderCell.Value = "Front Bow";

            m_cbFoupType[0] = cbDcst00;
            m_cbFoupType[1] = cbDcst01;
            m_cbFoupType[2] = cbDcst02;
            m_cbFoupType[3] = cbDcst03;
            m_cbFoupType[4] = cbDcst04;
            m_cbFoupType[5] = cbDcst05;
            m_cbFoupType[6] = cbDcst06;
            m_cbFoupType[7] = cbDcst07;
            m_cbFoupType[8] = cbDcst08;
            m_cbFoupType[9] = cbDcst09;
            m_cbFoupType[10] = cbDcst10;
            m_cbFoupType[11] = cbDcst11;
            m_cbFoupType[12] = cbDcst12;
            m_cbFoupType[13] = cbDcst13;
            m_cbFoupType[14] = cbDcst14;
            m_cbFoupType[15] = cbDcst15;

            m_cbkTypeEnable[0] = chkEnable00;
            m_cbkTypeEnable[1] = chkEnable01;
            m_cbkTypeEnable[2] = chkEnable02;
            m_cbkTypeEnable[3] = chkEnable03;
            m_cbkTypeEnable[4] = chkEnable04;
            m_cbkTypeEnable[5] = chkEnable05;
            m_cbkTypeEnable[6] = chkEnable06;
            m_cbkTypeEnable[7] = chkEnable07;
            m_cbkTypeEnable[8] = chkEnable08;
            m_cbkTypeEnable[9] = chkEnable09;
            m_cbkTypeEnable[10] = chkEnable10;
            m_cbkTypeEnable[11] = chkEnable11;
            m_cbkTypeEnable[12] = chkEnable12;
            m_cbkTypeEnable[13] = chkEnable13;
            m_cbkTypeEnable[14] = chkEnable14;
            m_cbkTypeEnable[15] = chkEnable15;

            if (GParam.theInst.FreeStyle)
            {
                btnSet.Image = RorzeApi.Properties.Resources._32_save_;
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

        //  去Loadport讀資料
        private void ReadLoadportData()
        {
            EnableButtom(false);
            _load.OnGetDataComplete -= _load_OnGetDataComplete;
            _load.OnGetDataComplete += _load_OnGetDataComplete;
            _load.GetData();
        }
        //  讀取結果
        private void _load_OnGetDataComplete(object sender, bool bSuc)
        {
            _load.OnGetDataComplete -= _load_OnGetDataComplete;
            if (bSuc)
                AnalysisData();
            EnableButtom(true);
        }
        //  讀取分析
        private void AnalysisData()
        {
            dgvStage.SuspendLayout();

            //  DPRM
            cbCarrierID.Items.Clear();
            for (int i = 0; i < 16; i++)
            {
                cbCarrierID.Items.Add(_load.GetDPRMData[i][16]);

                dgvStage.Rows[0].Cells[i].Value = _load.GetDPRMData[i][16];

                for (int j = 0; j < 9; j++)
                {
                    dgvStage.Rows[j + 1].Cells[i].Value = _load.GetDPRMData[i][j];
                }
            }

            //取消行排序
            for (int i = 0; i < dgvStage.Columns.Count; i++)
            {
                //dgvStage.Columns[i].Width = dgvStage.Width / (dgvStage.Columns.Count + 1);
            }

            //  DPRM
            tbDmpr06.Text = _load.GetDMPRData[6];
            tbDmpr08.Text = _load.GetDMPRData[8];
            //  DCST
            for (int i = 0; i < m_cbFoupType.Length; i++)
            {
                m_cbFoupType[i].Items.Clear();

                for (int n = 0; n < 16; n++)
                {
                    m_cbFoupType[i].Items.Add(_load.GetDPRMData[n][16]);
                    if (_load.GetDCSTData[i] == _load.GetDPRMData[n][16].Replace("\"", ""))
                        m_cbFoupType[i].SelectedIndex = n;
                }

                m_cbFoupType[i].Items.Add("AUTO");
                if (_load.GetDCSTData[i].Contains("AUTO"))
                    m_cbFoupType[i].SelectedIndex = m_cbFoupType[i].Items.Count - 1;

                m_cbkTypeEnable[i].Text = ((RorzeUnit.Class.Loadport.Enum.enumFoupType)i).ToString() + " Enable";

                m_cbkTypeEnable[i].Checked = GParam.theInst.GetFoupTypeEnableList(_load.BodyNo - 1, i);
            }

            dgvStage.ResumeLayout();
        }
        //  Interlock
        private void EnableButtom(bool bAct)
        {
            gpbDCST.Enabled = gpbDMPR.Enabled = bAct;
            btnSet.Enabled = bAct;
            tlpTypeEnable.Enabled = bAct;

            gpbMapping.Enabled = bAct;
            if (delegateMDILock != null)
                delegateMDILock(!bAct);
        }


        private void GetMappingInfomation()
        {
            int nBottom, nTop, nSlotNo, nSpace, nTop1, nBottom1, nSpace1, nBow, nOffset;
            string strMap, strShow;

            rtbMapInfo.Clear();
            _load.GmapW(2000);

            //  GMAP         
            strMap = _load.MappingData;
            strShow = "Mapping Data = " + _load.MappingData;
            rtbMapInfo.AppendText(strShow + "\n");
            //  DMPR
            strShow = "Top:" + _load.GetDMPRData[6] + "; Bottom:" + _load.GetDMPRData[8];
            rtbMapInfo.AppendText(strShow + "\n");
            nTop = int.Parse(_load.GetDMPRData[6]);
            nBottom = int.Parse(_load.GetDMPRData[8]);
            //  DPRM
            nSlotNo = 25;
            nSpace = 10000;
            nOffset = 0;
            for (int i = 0; i < 16; i++)
            {
                if (_load.GetDPRMData[i][16].IndexOf(cbCarrierID.Text) >= 0)
                {
                    nSlotNo = int.Parse(_load.GetDPRMData[i][3]);
                    nSpace = int.Parse(_load.GetDPRMData[i][4]);
                    nOffset = int.Parse(_load.GetDPRMData[i][7]);
                    break;
                }
            }
            strShow = string.Format("Slot Num:{0} , Slot Pitch:{1} , Offset:{2}", nSlotNo, nSpace, nOffset);
            rtbMapInfo.AppendText(strShow + "\n");
            //  參考BCB
            nTop += nOffset;
            nBottom += nOffset;
            //  RCA2
            for (int i = 0; i < nSlotNo; i++)
            {
                _load.Rca2W(3000, i * 2);
                string[] theRCA2 = _load.GetRac2Data;

                nTop1 = int.Parse(theRCA2[0]);
                nBottom1 = int.Parse(theRCA2[1]);
                nSpace1 = int.Parse(theRCA2[2]);

                if (strMap[nSlotNo - 1 - i] == '0')//沒有WAFER不用特別讀
                {
                    nBow = 0;
                }
                else
                {
                    nBow = ((nTop1 + nBottom1) / 2) - (nBottom - (nSlotNo - 1 - i) * nSpace);
                }

                if (nBow < 0)
                    strShow = string.Format("{0:D2} {1:D8} - {2:D8} = {3:D8} {4:D8} {5}", nSlotNo - i, nTop1, nBottom1, nSpace1, nBow, strMap[nSlotNo - 1 - i]);
                else
                    strShow = string.Format("{0:D2} {1:D8} - {2:D8} = {3:D8} +{4:D8} {5}", nSlotNo - i, nTop1, nBottom1, nSpace1, nBow, strMap[nSlotNo - 1 - i]);
                rtbMapInfo.AppendText(strShow + "\n");
            }
        }

        //=========        
        private void frmTeachLoadport_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                _strUserName = _userManager.UserID;

                if (this.Visible)
                {
                    ReadLoadportData();
                }
                else
                {
                    _load.SwidW(3000, "AUTO");
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        //========= 
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
                    MsgBox.Enabled = ret;
            }
        }

        #region Button
        private void cbCarrierID_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbCarrierID.SelectedItem == null)
            {
                new frmMessageBox("Foup Type is abnormal.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
                return;
            }
            EnableButtom(false);
            _load.SwidW(3000, cbCarrierID.SelectedItem.ToString());
            EnableButtom(true);
        }
        private void btnSet_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachLoadport" + _load.BodyNo, _strUserName, "Loadport" + _load.BodyNo, btn.Name);

            EnableButtom(false);
            //  DPRM
            string[] strDprm /*= new string[17]*/;
            for (int i = 0; i < 16; i++)
            {
                strDprm = _load.GetDPRMData[i];//   先撈loadport data
                strDprm[16] = dgvStage.Rows[0].Cells[i].Value.ToString();
                for (int j = 0; j < 9; j++)
                {
                    strDprm[j] = dgvStage.Rows[j + 1].Cells[i].Value.ToString();
                }
                //STG1.DPRM.STDT[0]=0,2,0,25,10000,200,1300,0,1000,0,1,0,0,0,0,0,FUP1
                string strContent = string.Join(",", strDprm);
                _load.SetDprmW(3000, i, strContent);
            }
            // DMPR
            _load.SetDmprW(3000, 6, tbDmpr06.Text);//very top
            _load.SetDmprW(3000, 8, tbDmpr08.Text);//vert bottom

            // infor pad
            string[] strDcst = new string[16];
            for (int i = 0; i < strDcst.Length; i++)
            {
                strDcst[i] = m_cbFoupType[i].SelectedItem.ToString();
                GParam.theInst.SetFoupTypeEnableList(_load.BodyNo - 1, i, m_cbkTypeEnable[i].Checked);
            }
            _load.UpdateInfoPadEnable(GParam.theInst.GetFoupTypeEnableList(_load.BodyNo - 1));

            // STG1.DCST.STDT=FUP1,FUP2,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO,AUTO
            string strDCSTContent = string.Join(",", strDcst);
            _load.SetDCSTW(3000, strDCSTContent);

            _load.WtdtW(10000);

            new frmMessageBox("Saving data completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information).ShowDialog();
            EnableButtom(true);
        }
        private void btnLoad_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachLoadport" + _load.BodyNo, _strUserName, "Loadport" + _load.BodyNo, btn.Name);

            if (cbCarrierID.SelectedIndex == -1)
            {
                new frmMessageBox("Please choose type!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (false == _load.FoupExist)
            {
                new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            EnableButtom(false);
            _load.OnClmpComplete -= Loadport_MappingComplete;
            _load.OnClmpComplete += Loadport_MappingComplete;
            _load.CLMP();
        }
        private void btnUnld_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachLoadport" + _load.BodyNo, _strUserName, "Loadport" + _load.BodyNo, btn.Name);

            EnableButtom(false);
            _load.OnUclmComplete -= Loadport_UclmComplete;
            _load.OnUclmComplete += Loadport_UclmComplete;
            _load.UCLM();
        }
        private void btnMap_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachLoadport" + _load.BodyNo, _strUserName, "Loadport" + _load.BodyNo, btn.Name);

            if (false == _load.FoupExist)
            {
                new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            EnableButtom(false);
            _load.OnMappingComplete -= Loadport_MappingComplete;
            _load.OnMappingComplete += Loadport_MappingComplete;
            _load.WMAP();
        }
        #endregion

        public void Loadport_MappingComplete(object sender, RorzeUnit.Class.Loadport.Event.LoadPortEventArgs e)
        {

            _load.OnMappingComplete -= Loadport_MappingComplete;
            _load.OnClmpComplete -= Loadport_MappingComplete;

            I_Loadport thenumUnit = sender as I_Loadport;
            int index = thenumUnit.BodyNo - 1;
            if (thenumUnit.Disable) return;
            string strMapData = e.MappingData;

            GetMappingInfomation();

            EnableButtom(true);
        }
        public void Loadport_UclmComplete(object sender, RorzeUnit.Class.Loadport.Event.LoadPortEventArgs e)
        {
            _load.OnUclmComplete -= Loadport_UclmComplete;

            rtbMapInfo.Clear();

            EnableButtom(true);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
