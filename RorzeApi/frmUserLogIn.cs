using RorzeApi.Class;
using RorzeUnit.Class;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RorzeApi
{
    public partial class frmUserLogIn : Form
    {
        //event handler
        public class OnLonInEventArgs : EventArgs
        {
            public string UserID { get; set; }
            public OnLonInEventArgs(string ID)
            {
                UserID = ID;
            }
        }
        public delegate void OnLonInEventHandler(object sender, OnLonInEventArgs e);

        private SPermission _permission;
        private SMainDB _dbMain;

        //public event OnLonInEventHandler OnLogIn;

        public frmUserLogIn(SPermission permission, SMainDB db)
        {
            InitializeComponent();

        
            _dbMain = db;
            _permission = permission;

            if (GParam.theInst.FreeStyle)
            {
                btnOk.Image = Properties.Resources._64_password_;
                this.Icon = RorzeApi.Properties.Resources.bwbs_;
            }
            else
            {
                this.Icon = RorzeApi.Properties.Resources.R;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {          

            bool bSuc = _permission.Login(cboUserID.Text, tbPassword.Text);

            if (bSuc/*m_strPassword == tbPassword.Text*/)
            {
                this.DialogResult = DialogResult.OK;

                //if (OnLogIn != null)
                //    OnLogIn(this, new OnLonInEventArgs(cboUserID.Text));
                this.Close();
            }
            else
            {
                frmMessageBox frmMbox = new frmMessageBox("Wrong password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frmMbox.ShowDialog();
            }


        }
        private void frmUserLogIn_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                cboUserID.Items.Clear();
                foreach (string item in _permission.GetPermissionUser())
                {
                    if (item == "Super")
                        continue;

                    cboUserID.Items.Add(item);
                }
            }
        }
        private void tbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnOk.PerformClick();
            }
        }
        //  隱藏最高權限的使用者，要按下CTRL開啟
        private void label2_Click(object sender, EventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                cboUserID.Items.Clear();
                foreach (string item in _permission.GetPermissionUser())
                {
                    cboUserID.Items.Add(item);
                }
            }
        }

        private void tbPassword_Click(object sender, EventArgs e)
        {
            frmPassword myPodPwd = new frmPassword();
            if (DialogResult.OK == myPodPwd.ShowDialog())
            {
                tbPassword.Text = myPodPwd.GetPassWord;
                btnOk.PerformClick();
            }
        }
    }
}
