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
    public partial class frmMessageBox : Form
    {
        private MessageBoxIcon _icon;
        public frmMessageBox(string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, bool selectWaferSize = false, bool UseButtons = true)
        {
            InitializeComponent();
            try
            {
                lblMessage.Text = GParam.theInst.GetLanguage(message);
                this.Text = GParam.theInst.GetLanguage(caption);
                _icon = icon;


                this.TopMost = true;

                if (UseButtons)
                {
                    switch (buttons)
                    {
                        case MessageBoxButtons.AbortRetryIgnore:
                            layoutButton2.ColumnCount = 3;
                            foreach (var item in layoutButton2.ColumnStyles)
                            {
                                (item as ColumnStyle).SizeType = SizeType.Absolute;
                                (item as ColumnStyle).Width = layoutButton2.Width / 3;
                            }
                            layoutButton2.Controls.Add(CreateButton("Abort", System.Windows.Forms.DialogResult.Abort));
                            layoutButton2.Controls.Add(CreateButton("Retry", System.Windows.Forms.DialogResult.Retry));
                            layoutButton2.Controls.Add(CreateButton("Ignore", System.Windows.Forms.DialogResult.Ignore));
                            break;
                        case MessageBoxButtons.OK:
                            layoutButton2.Controls.Add(CreateButton("OK", System.Windows.Forms.DialogResult.OK));
                            break;
                        case MessageBoxButtons.OKCancel:
                            if(selectWaferSize == false)
                            {
                                layoutButton2.Controls.Add(CreateButton("OK", System.Windows.Forms.DialogResult.OK));
                                layoutButton2.Controls.Add(CreateButton("Cancel", System.Windows.Forms.DialogResult.Cancel));
                            }
                            else
                            {

                                layoutButton2.Controls.Add(CreateButton("12 Inch", System.Windows.Forms.DialogResult.OK));
                                layoutButton2.Controls.Add(CreateButton("Frame", System.Windows.Forms.DialogResult.Cancel));
                            }
                            break;
                        case MessageBoxButtons.RetryCancel:
                            layoutButton2.Controls.Add(CreateButton("Retry", System.Windows.Forms.DialogResult.Retry));
                            layoutButton2.Controls.Add(CreateButton("Cancel", System.Windows.Forms.DialogResult.Cancel));
                            break;
                        case MessageBoxButtons.YesNo:
                            layoutButton2.Controls.Add(CreateButton("Yes", System.Windows.Forms.DialogResult.Yes));
                            layoutButton2.Controls.Add(CreateButton("No", System.Windows.Forms.DialogResult.No));
                            break;
                        case MessageBoxButtons.YesNoCancel:
                            layoutButton2.ColumnCount = 3;
                            foreach (var item in layoutButton2.ColumnStyles)
                            {
                                (item as ColumnStyle).SizeType = SizeType.Absolute;
                                (item as ColumnStyle).Width = layoutButton2.Width / 3;
                            }
                            layoutButton2.Controls.Add(CreateButton("Yes", System.Windows.Forms.DialogResult.Yes));
                            layoutButton2.Controls.Add(CreateButton("No", System.Windows.Forms.DialogResult.No));
                            layoutButton2.Controls.Add(CreateButton("Cancel", System.Windows.Forms.DialogResult.Cancel));
                            break;

                        default:
                            break;
                    }
                }

                //picIcon.Paint += new PaintEventHandler(picIcon_Paint);


                this.Height = tableLayoutPanel1.Height + layoutButton2.Height + 60;
            }
            catch
            {

            }
        }

        private Button CreateButton(string strName, DialogResult result)
        {
            Button btn = new Button();
            btn.Text = GParam.theInst.GetLanguage(strName);
            btn.Width = 150;
            btn.Height = 50;
            btn.Font = new System.Drawing.Font(this.Font.FontFamily, 12);
            btn.Margin = new System.Windows.Forms.Padding(5);
            btn.Anchor = AnchorStyles.None;
            btn.DialogResult = result;
            return btn;
        }
        private void picIcon_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Icon icon = SystemIcons.Warning;

                Rectangle r = new Rectangle(picIcon.Left, picIcon.Top, icon.Width, icon.Height);
                switch (_icon)
                {
                    case MessageBoxIcon.Error:
                        e.Graphics.DrawIcon(SystemIcons.Error, r /*picIcon.Left, picIcon.Top*/);
                        break;
                    case MessageBoxIcon.Information:
                        e.Graphics.DrawIcon(SystemIcons.Information, r/*picIcon.Left, picIcon.Top*/);
                        break;
                    case MessageBoxIcon.None:
                        break;
                    case MessageBoxIcon.Question:
                        e.Graphics.DrawIcon(SystemIcons.Question,r);
                        break;
                    case MessageBoxIcon.Warning:
                        e.Graphics.DrawIcon(SystemIcons.Warning, icon.Width, icon.Height);
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }
        }






    }
}
