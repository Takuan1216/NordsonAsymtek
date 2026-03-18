using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RorzeApi
{
    public partial class frmSubGroup : Form
    {
        //public List<Form> SubPages = new List<Form>();
        private List<Form> _subPages = new List<Form>();
        public event EventHandler OnSelectedIndexChanged;
        public frmSubGroup()
        {
            InitializeComponent();
            
            //tabSubPage.TabPages.Clear();
            //this.TopMost = true;
            this.Click += new EventHandler(frmSubGroup_Click);
            tabSubPage.Click += new EventHandler(frmSubGroup_Click);
            tabSubPage.MouseUp += new MouseEventHandler(frmSubGroup_Click);
        }

        void frmSubGroup_Click(object sender, EventArgs e)
        {
            foreach (Form frm in _subPages)
            {
                if (frm.Visible)
                    frm.BringToFront();
            }
        }

        private void frmSubGroup_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.Dock = DockStyle.Top; //向上吸附
                //this.Width = tabSubPage.Width;
                this.Height = tabSubPage.Height;
                tabSubPage_SelectedIndexChanged(this, new EventArgs());
            }
            else
            {
                foreach (Form frm in _subPages)
                {
                    frm.Hide();
                }
            }
        }

        private void tabSubPage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OnSelectedIndexChanged != null) OnSelectedIndexChanged(this, new EventArgs());
            if (InvokeRequired)
            {
                EventHandler invoke = new EventHandler(tabSubPage_SelectedIndexChanged);
                this.ParentForm.BeginInvoke(invoke);
            }
            else
            {
                if (_subPages[tabSubPage.SelectedIndex].Visible) return;
                foreach (Form frm in _subPages)
                {
                    frm.MdiParent = this.ParentForm;
                    frm.Hide();
                }
                
                //_subPages[tabSubPage.SelectedIndex].Dock = DockStyle.Fill;
                _subPages[tabSubPage.SelectedIndex].Show();
                _subPages[tabSubPage.SelectedIndex].BringToFront();
                
            }
        }
        private bool _bFirstAdded = true;
        public void AddPage(Form frm)
        {
            if (_bFirstAdded)
            {
                tabSubPage.TabPages[0].Text = frm.Text;
                _bFirstAdded = false;
            }
            else
                tabSubPage.TabPages.Add(frm.Text);
            frm.Location = new Point(0, tabSubPage.Height);
            frm.Size = new Size(1010, 528 - tabSubPage.Height);
            //frm.Height = 510 - tabSubPage.Height;
            _subPages.Add(frm);
        }


    }
}
