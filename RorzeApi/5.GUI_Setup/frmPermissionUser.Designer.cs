namespace RorzeApi
{
    partial class frmPermissionUser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmPermissionUser));
            this.btnSave = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.lblLastUser = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lblLastDate = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.masktxtRePwd = new System.Windows.Forms.MaskedTextBox();
            this.masktxtPwd = new System.Windows.Forms.MaskedTextBox();
            this.ckbShowPwd = new System.Windows.Forms.CheckBox();
            this.cboUserID = new System.Windows.Forms.ComboBox();
            this.gbTpTime = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnLog = new System.Windows.Forms.Button();
            this.btnInitialen = new System.Windows.Forms.Button();
            this.btnSetup = new System.Windows.Forms.Button();
            this.btnTeaching = new System.Windows.Forms.Button();
            this.btnMaintenance = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.txtNowUser = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.gbTpTime.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSave
            // 
            resources.ApplyResources(this.btnSave, "btnSave");
            this.btnSave.BackColor = System.Drawing.Color.White;
            this.btnSave.Image = global::RorzeApi.Properties.Resources._32_save;
            this.btnSave.Name = "btnSave";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnDelete
            // 
            resources.ApplyResources(this.btnDelete, "btnDelete");
            this.btnDelete.BackColor = System.Drawing.Color.White;
            this.btnDelete.Image = global::RorzeApi.Properties.Resources._32_delete;
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // lblLastUser
            // 
            resources.ApplyResources(this.lblLastUser, "lblLastUser");
            this.lblLastUser.Name = "lblLastUser";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // lblLastDate
            // 
            resources.ApplyResources(this.lblLastDate, "lblLastDate");
            this.lblLastDate.Name = "lblLastDate";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // masktxtRePwd
            // 
            resources.ApplyResources(this.masktxtRePwd, "masktxtRePwd");
            this.masktxtRePwd.BackColor = System.Drawing.Color.White;
            this.masktxtRePwd.Name = "masktxtRePwd";
            this.masktxtRePwd.PasswordChar = '*';
            // 
            // masktxtPwd
            // 
            resources.ApplyResources(this.masktxtPwd, "masktxtPwd");
            this.masktxtPwd.BackColor = System.Drawing.Color.White;
            this.masktxtPwd.Name = "masktxtPwd";
            this.masktxtPwd.PasswordChar = '*';
            // 
            // ckbShowPwd
            // 
            resources.ApplyResources(this.ckbShowPwd, "ckbShowPwd");
            this.ckbShowPwd.Name = "ckbShowPwd";
            this.ckbShowPwd.UseVisualStyleBackColor = true;
            this.ckbShowPwd.CheckedChanged += new System.EventHandler(this.ckbShowPwd_CheckedChanged);
            // 
            // cboUserID
            // 
            resources.ApplyResources(this.cboUserID, "cboUserID");
            this.cboUserID.BackColor = System.Drawing.Color.White;
            this.cboUserID.FormattingEnabled = true;
            this.cboUserID.Name = "cboUserID";
            this.cboUserID.SelectedIndexChanged += new System.EventHandler(this.cboUserID_SelectedIndexChanged);
            // 
            // gbTpTime
            // 
            resources.ApplyResources(this.gbTpTime, "gbTpTime");
            this.gbTpTime.Controls.Add(this.tableLayoutPanel2);
            this.gbTpTime.Name = "gbTpTime";
            this.gbTpTime.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.btnLog, 4, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnInitialen, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnSetup, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnTeaching, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnMaintenance, 2, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // btnLog
            // 
            resources.ApplyResources(this.btnLog, "btnLog");
            this.btnLog.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnLog.ForeColor = System.Drawing.Color.Black;
            this.btnLog.Image = global::RorzeApi.Properties.Resources._32_folder;
            this.btnLog.Name = "btnLog";
            this.btnLog.UseVisualStyleBackColor = false;
            this.btnLog.Click += new System.EventHandler(this.btnLog_Click);
            // 
            // btnInitialen
            // 
            resources.ApplyResources(this.btnInitialen, "btnInitialen");
            this.btnInitialen.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnInitialen.ForeColor = System.Drawing.Color.Black;
            this.btnInitialen.Image = global::RorzeApi.Properties.Resources._32_reset;
            this.btnInitialen.Name = "btnInitialen";
            this.btnInitialen.UseVisualStyleBackColor = false;
            this.btnInitialen.Click += new System.EventHandler(this.btnORGN_Click);
            // 
            // btnSetup
            // 
            resources.ApplyResources(this.btnSetup, "btnSetup");
            this.btnSetup.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnSetup.ForeColor = System.Drawing.Color.Black;
            this.btnSetup.Image = global::RorzeApi.Properties.Resources._32_document;
            this.btnSetup.Name = "btnSetup";
            this.btnSetup.UseVisualStyleBackColor = false;
            this.btnSetup.Click += new System.EventHandler(this.btnSetup_Click);
            // 
            // btnTeaching
            // 
            resources.ApplyResources(this.btnTeaching, "btnTeaching");
            this.btnTeaching.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnTeaching.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnTeaching.ForeColor = System.Drawing.Color.Black;
            this.btnTeaching.Image = global::RorzeApi.Properties.Resources._32_settings;
            this.btnTeaching.Name = "btnTeaching";
            this.btnTeaching.UseVisualStyleBackColor = false;
            this.btnTeaching.Click += new System.EventHandler(this.btnTeaching_Click);
            // 
            // btnMaintenance
            // 
            resources.ApplyResources(this.btnMaintenance, "btnMaintenance");
            this.btnMaintenance.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnMaintenance.ForeColor = System.Drawing.Color.Black;
            this.btnMaintenance.Image = global::RorzeApi.Properties.Resources._32_support;
            this.btnMaintenance.Name = "btnMaintenance";
            this.btnMaintenance.UseVisualStyleBackColor = false;
            this.btnMaintenance.Click += new System.EventHandler(this.btnMaintenance_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.tableLayoutPanel3);
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Controls.Add(this.gbTpTime);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // tableLayoutPanel3
            // 
            resources.ApplyResources(this.tableLayoutPanel3, "tableLayoutPanel3");
            this.tableLayoutPanel3.Controls.Add(this.lblLastDate, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.label6, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.lblLastUser, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtNowUser, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.ckbShowPwd, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.masktxtRePwd, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.masktxtPwd, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.cboUserID, 1, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // txtNowUser
            // 
            resources.ApplyResources(this.txtNowUser, "txtNowUser");
            this.txtNowUser.Name = "txtNowUser";
            this.txtNowUser.ReadOnly = true;
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Controls.Add(this.btnDelete);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Name = "panel1";
            // 
            // frmPermissionUser
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmPermissionUser";
            this.VisibleChanged += new System.EventHandler(this.frmPermissionUser_VisibleChanged);
            this.gbTpTime.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblLastUser;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblLastDate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.MaskedTextBox masktxtRePwd;
        private System.Windows.Forms.MaskedTextBox masktxtPwd;
        private System.Windows.Forms.CheckBox ckbShowPwd;
        private System.Windows.Forms.ComboBox cboUserID;
        private System.Windows.Forms.GroupBox gbTpTime;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.Button btnSetup;
        private System.Windows.Forms.Button btnMaintenance;
        public System.Windows.Forms.Button btnTeaching;
        private System.Windows.Forms.Button btnInitialen;
        private System.Windows.Forms.TextBox txtNowUser;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
    }
}