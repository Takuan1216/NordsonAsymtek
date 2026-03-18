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
    public partial class frmPassword : Form
    {
        private string m_strKeyin;
        public frmPassword()
        {
            m_strKeyin = "";
            InitializeComponent();
        }
  
        private void btnNo1_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '1';
            btnOk.Enabled = true;
        }

        private void btnNo2_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '2';
            btnOk.Enabled = true;
        }

        private void btnNo3_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '3';
            btnOk.Enabled = true;
        }

        private void btnNo4_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '4';
            btnOk.Enabled = true;
        }

        private void btnNo5_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '5';
            btnOk.Enabled = true;
        }

        private void btnNo6_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '6';
            btnOk.Enabled = true;
        }

        private void btnNo7_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '7';
            btnOk.Enabled = true;
        }

        private void btnNo8_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '8';
            btnOk.Enabled = true;
        }

        private void btnNo9_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '9';
            btnOk.Enabled = true;
        }

        private void btnNo0_Click(object sender, EventArgs e)
        {
            tbPassword.Text += '*';
            m_strKeyin += '0';
            btnOk.Enabled = true;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            tbPassword.Text = "";
            m_strKeyin = "";
            btnOk.Enabled = false;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            int nLen = tbPassword.Text.Length;
            if (nLen > 1)
            {
                tbPassword.Text = tbPassword.Text.Substring(0, nLen - 1);
                m_strKeyin = m_strKeyin.Substring(0, nLen - 1);
            }
            else if (nLen == 1)
            {
                tbPassword.Text = "";
                m_strKeyin = "";
                btnOk.Enabled = false;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            //if (m_strKeyin == m_strPassword)
            {            
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            //else
            {
                //MessageBox.Show("密碼錯誤");
            }
        }

        public string GetPassWord { get { return m_strKeyin; } }

    }
}
