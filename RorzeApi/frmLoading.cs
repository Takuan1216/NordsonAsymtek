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
    public partial class frmLoading : Form
    {
        public frmLoading()
        {
            InitializeComponent();

            progressBar1.Maximum = 62;//設置最大長度值
            progressBar1.Value = 0;//設置當前值
            progressBar1.Step = 1;//設置沒次增長多少
            this.ControlBox = false;
            if (GParam.theInst.FreeStyle)
            {         
                lblSystemMode.BackColor= GParam.theInst.ColorTitle;
                lblSystemMode.ForeColor = Color.Black;
                this.Icon = RorzeApi.Properties.Resources.bwbs_;
            }
            else
            {
                this.Icon = RorzeApi.Properties.Resources.R;
            }
            lblSystemMode.Text = GParam.theInst.EquipmentShowName;
        }

        public void AddMessage(string strMsg, bool bFinish = true)
        {
            if (bFinish)
                AddMessage(Color.Black, strMsg);
            else
                AddMessage(Color.Red, strMsg);
        }

        delegate void dlgAddMessage(Color color, string strMsg);
        public void AddMessage(Color color, string strMsg)
        {
            if (InvokeRequired)
            {
                dlgAddMessage dlg = new dlgAddMessage(AddMessage);
                this.Invoke(dlg, color, strMsg);
            }
            else
            {
                Label lbl = new Label();
                lbl.AutoSize = true;
                lbl.Margin = new System.Windows.Forms.Padding(0);
                lbl.Text = strMsg;
                lbl.ForeColor = color;
                layoutMessage.Controls.Add(lbl);

                if (progressBar1.Value != progressBar1.Maximum)
                    progressBar1.Value += progressBar1.Step;

                this.Refresh();
            }
        }

        delegate void dlgInvoke();
        public void AllowIgnore()
        {
            if (InvokeRequired)
            {
                dlgInvoke dlg = new dlgInvoke(AllowIgnore);
                this.Invoke(dlg);
            }
            else
            {
                btnSkip.Visible = true;
                this.Refresh();
            }
        }
        private void layoutMessage_ControlAdded(object sender, ControlEventArgs e)
        {
            if (layoutMessage.VerticalScroll.Visible)
                layoutMessage.VerticalScroll.Value = layoutMessage.VerticalScroll.Maximum;
        }
    }
}
