namespace RorzeApi
{
    partial class frmSubGroup
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSubGroup));
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabSubPage = new System.Windows.Forms.TabControl();
            this.tabSubPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(769, 0);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabSubPage
            // 
            this.tabSubPage.Controls.Add(this.tabPage1);
            this.tabSubPage.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabSubPage.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabSubPage.Location = new System.Drawing.Point(0, 0);
            this.tabSubPage.Multiline = true;
            this.tabSubPage.Name = "tabSubPage";
            this.tabSubPage.SelectedIndex = 0;
            this.tabSubPage.Size = new System.Drawing.Size(777, 25);
            this.tabSubPage.SizeMode = System.Windows.Forms.TabSizeMode.FillToRight;
            this.tabSubPage.TabIndex = 0;
            this.tabSubPage.SelectedIndexChanged += new System.EventHandler(this.tabSubPage_SelectedIndexChanged);
            // 
            // frmSubGroup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(777, 25);
            this.Controls.Add(this.tabSubPage);
            this.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "frmSubGroup";
            this.Text = "frmSubGroup";
            this.VisibleChanged += new System.EventHandler(this.frmSubGroup_VisibleChanged);
            this.tabSubPage.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabControl tabSubPage;

    }
}