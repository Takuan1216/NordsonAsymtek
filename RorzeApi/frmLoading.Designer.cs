namespace RorzeApi
{
    partial class frmLoading
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmLoading));
            this.btnSkip = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.layoutMessage = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSystemMode = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSkip
            // 
            this.btnSkip.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            resources.ApplyResources(this.btnSkip, "btnSkip");
            this.btnSkip.Name = "btnSkip";
            this.btnSkip.UseVisualStyleBackColor = true;
            // 
            // progressBar1
            // 
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.Name = "progressBar1";
            // 
            // layoutMessage
            // 
            resources.ApplyResources(this.layoutMessage, "layoutMessage");
            this.layoutMessage.Name = "layoutMessage";
            this.layoutMessage.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.layoutMessage_ControlAdded);
            // 
            // lblSystemMode
            // 
            this.lblSystemMode.BackColor = System.Drawing.Color.DarkSlateGray;
            this.lblSystemMode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.lblSystemMode, "lblSystemMode");
            this.lblSystemMode.ForeColor = System.Drawing.Color.White;
            this.lblSystemMode.Name = "lblSystemMode";
            // 
            // frmLoading
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.layoutMessage);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btnSkip);
            this.Controls.Add(this.lblSystemMode);
            this.Name = "frmLoading";
            this.ResumeLayout(false);

        }

        #endregion
        public System.Windows.Forms.Button btnSkip;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.FlowLayoutPanel layoutMessage;
        private System.Windows.Forms.Label lblSystemMode;
    }
}