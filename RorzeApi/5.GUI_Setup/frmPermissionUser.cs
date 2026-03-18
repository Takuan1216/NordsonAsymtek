using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RorzeApi.Class;
using RorzeUnit.Class;
using RorzeApi.SECSGEM;
using RorzeUnit.Class.RFID;
using RorzeApi.GUI;

namespace RorzeApi
{
    public partial class frmPermissionUser : Form
    {
        float frmX;//當前窗體的寬度
        float frmY;//當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        SPermission _permission;
        SMainDB _dbMain;
        SGEM300 _Gem;
        SmartRFID _smartRFID;

        private bool TeachingF = false;
        private bool InitialenF = false;
        private bool MaintenanceF = false;
        private bool SetupF = false;
        private bool LogF = false;

        Color m_ButtonOn = SystemColors.ActiveCaption;

        public frmPermissionUser(SPermission permission, SMainDB db, SGEM300 GEM, SmartRFID smartRFID)
        {
            InitializeComponent();

            _dbMain = db;
            _permission = permission;
            _Gem = GEM;
            _smartRFID = smartRFID;

            m_ButtonOn = GParam.theInst.FreeStyle ? GParam.theInst.ColorButton : SystemColors.ActiveCaption;
            if (GParam.theInst.FreeStyle)
            {
                btnTeaching.Image = Properties.Resources._32_settings_;
                btnInitialen.Image = Properties.Resources._32_reset_;
                btnMaintenance.Image = Properties.Resources._32_support_;
                btnSetup.Image = Properties.Resources._32_document_;
                btnLog.Image = Properties.Resources._32_folder_;
                btnSave.Image = Properties.Resources._32_save_;
                btnDelete.Image = Properties.Resources._32_delete_;
            }

        }

        #region 畫面縮放
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
        private void cboUserID_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataSet dt = _dbMain.Reader(string.Format("Select * from UserLogin where UserName = '{0}'", cboUserID.Text));

            if (dt.Tables[0].Rows.Count > 0)
            {
                masktxtPwd.Text = dt.Tables[0].Rows[0]["UserPassword"].ToString();
                lblLastDate.Text = dt.Tables[0].Rows[0]["ModifyTime"].ToString();
                lblLastUser.Text = dt.Tables[0].Rows[0]["ModifyBy"].ToString();

                this.btnTeaching.BackColor = Convert.ToBoolean(dt.Tables[0].Rows[0]["Teaching"]) == true ? m_ButtonOn : SystemColors.ControlLight;
                this.TeachingF = Convert.ToBoolean(dt.Tables[0].Rows[0]["Teaching"]) == true ? true : false;

                this.btnInitialen.BackColor = Convert.ToBoolean(dt.Tables[0].Rows[0]["Initialen"]) == true ? m_ButtonOn : SystemColors.ControlLight;
                this.InitialenF = Convert.ToBoolean(dt.Tables[0].Rows[0]["Initialen"]) == true ? true : false;

                this.btnMaintenance.BackColor = Convert.ToBoolean(dt.Tables[0].Rows[0]["Maintenance"]) == true ? m_ButtonOn : SystemColors.ControlLight;
                this.MaintenanceF = Convert.ToBoolean(dt.Tables[0].Rows[0]["Maintenance"]) == true ? true : false;

                this.btnSetup.BackColor = Convert.ToBoolean(dt.Tables[0].Rows[0]["Setup"]) == true ? m_ButtonOn : SystemColors.ControlLight;
                this.SetupF = Convert.ToBoolean(dt.Tables[0].Rows[0]["Setup"]) == true ? true : false;

                this.btnLog.BackColor = Convert.ToBoolean(dt.Tables[0].Rows[0]["Log"]) == true ? m_ButtonOn : SystemColors.ControlLight;
                this.LogF = Convert.ToBoolean(dt.Tables[0].Rows[0]["Log"]) == true ? true : false;
            }
        }
        private void ckbShowPwd_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox ckbGet = sender as CheckBox;
            if (ckbGet.Checked)
            {
                masktxtPwd.PasswordChar = Convert.ToChar(0);
                masktxtRePwd.PasswordChar = Convert.ToChar(0);
            }
            else
            {
                masktxtPwd.PasswordChar = '*';
                masktxtRePwd.PasswordChar = '*';
            }
        }
        //========= 
        private void btnSave_Click(object sender, EventArgs e)
        {
            frmMessageBox frm;

            //if (_Gem != null && _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            //{
            //    frm = new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    frm.ShowDialog();
            //    return;
            //}
            string strUserID = cboUserID.Text.Trim();
            //檢查表單資料
            if (CheckData())
            {
                DataSet dtSelectUser = _dbMain.Reader(string.Format("Select * from UserLogin where UserName = '{0}'", strUserID));

                if (dtSelectUser.Tables[0].Rows.Count == 1)
                {
                    frmMessageBox frmMbox = new frmMessageBox(string.Format("Do you want to update {0}?", strUserID), "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (frmMbox.ShowDialog() == System.Windows.Forms.DialogResult.No) return;


                    _dbMain.SQLExec(
                        string.Format("Update UserLogin Set UserName='{0}', UserPassword='{1}', ModifyBy='{2}', ModifyTime='{3}', Teaching={4}, Initialen={5}, Maintenance={6}, Setup={7}, Log={8}   Where UserName='{9}'",
                                                     strUserID, masktxtPwd.Text, _permission.UserID, DateTime.Now, TeachingF, InitialenF, MaintenanceF, SetupF, LogF, strUserID));
                }
                else
                {
                    frmMessageBox frmMbox = new frmMessageBox(string.Format("Do you want to Add new UserName : {0} ?", strUserID), "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (frmMbox.ShowDialog() == System.Windows.Forms.DialogResult.No) return;

                    _dbMain.SQLExec(string.Format("Insert Into UserLogin (UserName, UserPassword, ModifyBy, ModifyTime, Teaching, Initialen, Maintenance, Setup, Log, UserLevel) Values ('{0}','{1}','{2}','{3}',{4},{5},{6},{7},{8},{9})",
                                                      strUserID, masktxtPwd.Text, _permission.UserID, DateTime.Now, TeachingF, InitialenF, MaintenanceF, SetupF, LogF, _permission.Level + 1));
                    InitialForm();
                }

            }
        }
        private void btnDelete_Click(object sender, EventArgs e)
        {
            frmMessageBox frm;


            //if (_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            //{
            //    frm = new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    frm.ShowDialog();
            //    return;
            //}
            string strUserID = cboUserID.Text.Trim();
            if (strUserID.ToLower().Contains("admin") || strUserID.ToLower().Contains("operator") || strUserID.ToLower().Contains("engineer"))
            {
                frmMessageBox frmMbox = new frmMessageBox(string.Format("This User can't Delete"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frmMbox.ShowDialog();
                return;
            }
            else if (strUserID == _permission.UserID)
            {
                frmMessageBox frmMbox = new frmMessageBox(string.Format("User is login..."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frmMbox.ShowDialog();
                return;
            }

            if (strUserID.Length > 0)
            {
                frmMessageBox frmMbox = new frmMessageBox(string.Format("Are you sure Delete User : {0}?", strUserID), "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (frmMbox.ShowDialog() == System.Windows.Forms.DialogResult.No) return;

                _dbMain.SQLExec("Delete From UserLogin Where UserName='" + strUserID + "'");
                //重新整理form
                InitialForm();
            }
        }
        private void btnTeaching_Click(object sender, EventArgs e)
        {
            if (TeachingF == false)
            {
                TeachingF = true;
                this.btnTeaching.BackColor = m_ButtonOn;
            }
            else
            {
                TeachingF = false;
                this.btnTeaching.BackColor = SystemColors.ControlLight;
            }
        }
        private void btnORGN_Click(object sender, EventArgs e)
        {
            if (InitialenF == false)
            {
                InitialenF = true;
                this.btnInitialen.BackColor = m_ButtonOn;
            }
            else
            {
                InitialenF = false;
                this.btnInitialen.BackColor = SystemColors.ControlLight;
            }
        }
        private void btnMaintenance_Click(object sender, EventArgs e)
        {
            if (MaintenanceF == false)
            {
                MaintenanceF = true;
                this.btnMaintenance.BackColor = m_ButtonOn;
            }
            else
            {
                MaintenanceF = false;
                this.btnMaintenance.BackColor = SystemColors.ControlLight;
            }
        }
        private void btnSetup_Click(object sender, EventArgs e)
        {
            if (SetupF == false)
            {
                SetupF = true;
                this.btnSetup.BackColor = m_ButtonOn;
            }
            else
            {
                SetupF = false;
                this.btnSetup.BackColor = SystemColors.ControlLight;
            }
        }
        private void btnLog_Click(object sender, EventArgs e)
        {
            if (LogF == false)
            {
                LogF = true;
                this.btnLog.BackColor = m_ButtonOn;
            }
            else
            {
                LogF = false;
                this.btnLog.BackColor = SystemColors.ControlLight;
            }
        }
        //========= 
        private bool CheckData()
        {
            string strNG = string.Empty;
            if (cboUserID.Text.Trim().Length <= 0) strNG = "User ID";
            if (masktxtPwd.Text.Length <= 0) strNG = "Password";
            if (masktxtRePwd.Text.Length <= 0) strNG = "Re-Password";
            //if (cboGroup.Text.Length <= 0) strNG = "Group ID";

            if (strNG.Length > 0)
            {
                frmMessageBox frmMbox = new frmMessageBox(string.Format("Please assign {0}. it cannot be empty!", strNG), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frmMbox.ShowDialog();
                return false;
            }
            else if (masktxtPwd.Text.CompareTo(masktxtRePwd.Text) != 0)
            {
                frmMessageBox frmMbox = new frmMessageBox(string.Format("Password and Re-Password not equal. Please check it!"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frmMbox.ShowDialog();
                return false;
            }
            else
                return true;
        }
        private void InitialForm()
        {

            //  目前使用者
            string strUser = _permission.UserID;
            //  目前使用者等級
            int nNowLevel = _permission.Level;
            //  目前使用者DataSet
            DataSet dtNowUser = _dbMain.Reader(string.Format("Select * from UserLogin where UserName = '{0}'", strUser));
            if (dtNowUser.Tables[0].Rows.Count > 0)
            {

                //  判斷比自己階級低的加入選擇使用者
                cboUserID.Text = string.Empty;
                cboUserID.Items.Clear();
                foreach (string userName in _permission.GetPermissionUser())
                {
                    DataSet dt = _dbMain.Reader(string.Format("Select * from UserLogin where UserName = '{0}'", userName));

                    if (dt.Tables[0].Rows.Count > 0)
                    {
                        int nLevel = (int)dt.Tables[0].Rows[0]["UserLevel"];

                        if (userName == strUser || nNowLevel < nLevel) //  權限最高是0，只顯示比自己低權限的
                            cboUserID.Items.Add(userName);
                    }
                }
                //  上次修改的時間
                lblLastDate.Text = dtNowUser.Tables[0].Rows[0]["ModifyTime"].ToString();
                //  上次修改的人
                lblLastUser.Text = dtNowUser.Tables[0].Rows[0]["ModifyBy"].ToString();
                //  現在使用者
                txtNowUser.Text = strUser;

                this.TeachingF = Convert.ToBoolean(dtNowUser.Tables[0].Rows[0]["Teaching"]) == true ? true : false;
                if (GParam.theInst.FreeStyle)
                    this.btnTeaching.BackColor = TeachingF ? GParam.theInst.ColorButton : SystemColors.ControlLight;
                else
                    this.btnTeaching.BackColor = TeachingF ? SystemColors.ActiveCaption : SystemColors.ControlLight;

                this.btnTeaching.Enabled = TeachingF;

                this.InitialenF = Convert.ToBoolean(dtNowUser.Tables[0].Rows[0]["Initialen"]) == true ? true : false;
                if (GParam.theInst.FreeStyle)
                    this.btnInitialen.BackColor = InitialenF ? GParam.theInst.ColorButton : SystemColors.ControlLight;
                else
                    this.btnInitialen.BackColor = InitialenF ? SystemColors.ActiveCaption : SystemColors.ControlLight;
                this.btnInitialen.Enabled = InitialenF;

                this.MaintenanceF = Convert.ToBoolean(dtNowUser.Tables[0].Rows[0]["Maintenance"]) == true ? true : false;
                if (GParam.theInst.FreeStyle)
                    this.btnMaintenance.BackColor = MaintenanceF ? GParam.theInst.ColorButton : SystemColors.ControlLight;
                else
                    this.btnMaintenance.BackColor = MaintenanceF ? SystemColors.ActiveCaption : SystemColors.ControlLight;
                this.btnMaintenance.Enabled = MaintenanceF;

                this.SetupF = Convert.ToBoolean(dtNowUser.Tables[0].Rows[0]["Setup"]) == true ? true : false;
                if (GParam.theInst.FreeStyle)
                    this.btnSetup.BackColor = SetupF ? GParam.theInst.ColorButton : SystemColors.ControlLight;
                else
                    this.btnSetup.BackColor = SetupF ? SystemColors.ActiveCaption : SystemColors.ControlLight;
                this.btnSetup.Enabled = SetupF;

                this.LogF = Convert.ToBoolean(dtNowUser.Tables[0].Rows[0]["Log"]) == true ? true : false;
                if (GParam.theInst.FreeStyle)
                    this.btnLog.BackColor = LogF ? GParam.theInst.ColorButton : SystemColors.ControlLight;
                else
                    this.btnLog.BackColor = LogF ? SystemColors.ActiveCaption : SystemColors.ControlLight;
                this.btnLog.Enabled = LogF;
            }
        }
        private void frmPermissionUser_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                InitialForm();
                _smartRFID.OnReadID -= SetSmartRFIDEvent;
                _smartRFID.OnReadID += SetSmartRFIDEvent;

            }
            else
            {
                _smartRFID.OnReadID -= SetSmartRFIDEvent;
                masktxtPwd.Text = "";
                masktxtRePwd.Text = "";
            }
        }

        private void SetSmartRFIDEvent(object sender, string strNmae)
        {
            Action act = () =>
            {
                masktxtPwd.Text = strNmae;
                masktxtRePwd.Text = strNmae;
            };
            BeginInvoke(act);
        }
    }
}
