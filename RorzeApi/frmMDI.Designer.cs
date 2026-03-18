namespace RorzeApi
{
    partial class frmMDI
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置 Managed 資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器
        /// 修改這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMDI));
            this.tmrUI = new System.Windows.Forms.Timer(this.components);
            this.panelSecsMenu = new System.Windows.Forms.Panel();
            this.panelLogMenu = new System.Windows.Forms.Panel();
            this.panelSetupMenu = new System.Windows.Forms.Panel();
            this.panelMaintenaceMenu = new System.Windows.Forms.Panel();
            this.panelInitialenMenu = new System.Windows.Forms.Panel();
            this.panelTeachingMenu = new System.Windows.Forms.Panel();
            this.panelStatusMenu = new System.Windows.Forms.Panel();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnBuzzerOff = new System.Windows.Forms.Button();
            this.btnSignIn = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pnlShowName = new System.Windows.Forms.Panel();
            this.lblShowName = new System.Windows.Forms.Label();
            this.labTime = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.labPowerMode = new System.Windows.Forms.Label();
            this.lblVersionValue = new System.Windows.Forms.Label();
            this.labVersion = new System.Windows.Forms.Label();
            this.labTimes = new System.Windows.Forms.Label();
            this.panelCommandMenu = new System.Windows.Forms.Panel();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnAlarm = new System.Windows.Forms.Button();
            this.panelMenu = new System.Windows.Forms.Panel();
            this.panelFeaturesMenu = new System.Windows.Forms.Panel();
            this.btnSecs = new System.Windows.Forms.Button();
            this.btnLog = new System.Windows.Forms.Button();
            this.btnSetup = new System.Windows.Forms.Button();
            this.btnMaintenance = new System.Windows.Forms.Button();
            this.btnInitialen = new System.Windows.Forms.Button();
            this.btnTeaching = new System.Windows.Forms.Button();
            this.btnStatus = new System.Windows.Forms.Button();
            this.panelChilForm = new System.Windows.Forms.Panel();
            this.lblUserName = new System.Windows.Forms.Label();
            this.userLight1 = new RorzeApi.GUI.GUILight();
            this.uicimStatus1 = new NewGem300Server_OOP.GUI.UICIMStatus();
            this.pnlHeader.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.pnlShowName.SuspendLayout();
            this.panelCommandMenu.SuspendLayout();
            this.panelMenu.SuspendLayout();
            this.panelFeaturesMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // tmrUI
            // 
            this.tmrUI.Interval = 500;
            this.tmrUI.Tick += new System.EventHandler(this.tmrUI_Tick);
            // 
            // panelSecsMenu
            // 
            resources.ApplyResources(this.panelSecsMenu, "panelSecsMenu");
            this.panelSecsMenu.Name = "panelSecsMenu";
            // 
            // panelLogMenu
            // 
            resources.ApplyResources(this.panelLogMenu, "panelLogMenu");
            this.panelLogMenu.Name = "panelLogMenu";
            // 
            // panelSetupMenu
            // 
            resources.ApplyResources(this.panelSetupMenu, "panelSetupMenu");
            this.panelSetupMenu.Name = "panelSetupMenu";
            // 
            // panelMaintenaceMenu
            // 
            resources.ApplyResources(this.panelMaintenaceMenu, "panelMaintenaceMenu");
            this.panelMaintenaceMenu.Name = "panelMaintenaceMenu";
            // 
            // panelInitialenMenu
            // 
            resources.ApplyResources(this.panelInitialenMenu, "panelInitialenMenu");
            this.panelInitialenMenu.Name = "panelInitialenMenu";
            // 
            // panelTeachingMenu
            // 
            resources.ApplyResources(this.panelTeachingMenu, "panelTeachingMenu");
            this.panelTeachingMenu.Name = "panelTeachingMenu";
            // 
            // panelStatusMenu
            // 
            resources.ApplyResources(this.panelStatusMenu, "panelStatusMenu");
            this.panelStatusMenu.Name = "panelStatusMenu";
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.pnlHeader.Controls.Add(this.panel1);
            resources.ApplyResources(this.pnlHeader, "pnlHeader");
            this.pnlHeader.Name = "pnlHeader";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.DarkSlateGray;
            this.panel1.Controls.Add(this.lblUserName);
            this.panel1.Controls.Add(this.tableLayoutPanel2);
            this.panel1.Controls.Add(this.tableLayoutPanel1);
            this.panel1.Controls.Add(this.userLight1);
            this.panel1.Controls.Add(this.uicimStatus1);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pnlMovingWindow_MouseDown);
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.btnBuzzerOff, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnSignIn, 1, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // btnBuzzerOff
            // 
            this.btnBuzzerOff.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.btnBuzzerOff.FlatAppearance.BorderSize = 0;
            resources.ApplyResources(this.btnBuzzerOff, "btnBuzzerOff");
            this.btnBuzzerOff.ForeColor = System.Drawing.SystemColors.Control;
            this.btnBuzzerOff.Image = global::RorzeApi.Properties.Resources._32_mute;
            this.btnBuzzerOff.Name = "btnBuzzerOff";
            this.btnBuzzerOff.UseVisualStyleBackColor = false;
            this.btnBuzzerOff.Click += new System.EventHandler(this.btnBuzzerOff_Click);
            // 
            // btnSignIn
            // 
            this.btnSignIn.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnSignIn, "btnSignIn");
            this.btnSignIn.FlatAppearance.BorderSize = 0;
            this.btnSignIn.ForeColor = System.Drawing.SystemColors.Control;
            this.btnSignIn.Image = global::RorzeApi.Properties.Resources._32_login;
            this.btnSignIn.Name = "btnSignIn";
            this.btnSignIn.UseVisualStyleBackColor = false;
            this.btnSignIn.Click += new System.EventHandler(this.btnSignIn_Click);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.pnlShowName, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.labTime, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labPowerMode, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblVersionValue, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.labVersion, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.labTimes, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.tableLayoutPanel1_MouseClick);
            // 
            // pnlShowName
            // 
            this.pnlShowName.BackColor = System.Drawing.Color.Black;
            this.pnlShowName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlShowName.Controls.Add(this.lblShowName);
            resources.ApplyResources(this.pnlShowName, "pnlShowName");
            this.pnlShowName.Name = "pnlShowName";
            this.tableLayoutPanel1.SetRowSpan(this.pnlShowName, 3);
            // 
            // lblShowName
            // 
            this.lblShowName.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.lblShowName, "lblShowName");
            this.lblShowName.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lblShowName.Name = "lblShowName";
            // 
            // labTime
            // 
            resources.ApplyResources(this.labTime, "labTime");
            this.labTime.ForeColor = System.Drawing.SystemColors.Control;
            this.labTime.Name = "labTime";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.ForeColor = System.Drawing.SystemColors.Control;
            this.label3.Name = "label3";
            // 
            // labPowerMode
            // 
            resources.ApplyResources(this.labPowerMode, "labPowerMode");
            this.labPowerMode.ForeColor = System.Drawing.SystemColors.Control;
            this.labPowerMode.Name = "labPowerMode";
            // 
            // lblVersionValue
            // 
            resources.ApplyResources(this.lblVersionValue, "lblVersionValue");
            this.lblVersionValue.ForeColor = System.Drawing.SystemColors.Control;
            this.lblVersionValue.Name = "lblVersionValue";
            // 
            // labVersion
            // 
            resources.ApplyResources(this.labVersion, "labVersion");
            this.labVersion.ForeColor = System.Drawing.SystemColors.Control;
            this.labVersion.Name = "labVersion";
            // 
            // labTimes
            // 
            resources.ApplyResources(this.labTimes, "labTimes");
            this.labTimes.ForeColor = System.Drawing.SystemColors.Control;
            this.labTimes.Name = "labTimes";
            // 
            // panelCommandMenu
            // 
            this.panelCommandMenu.AllowDrop = true;
            this.panelCommandMenu.BackColor = System.Drawing.Color.SlateGray;
            this.panelCommandMenu.Controls.Add(this.panelSecsMenu);
            this.panelCommandMenu.Controls.Add(this.panelLogMenu);
            this.panelCommandMenu.Controls.Add(this.panelSetupMenu);
            this.panelCommandMenu.Controls.Add(this.panelMaintenaceMenu);
            this.panelCommandMenu.Controls.Add(this.panelInitialenMenu);
            this.panelCommandMenu.Controls.Add(this.panelTeachingMenu);
            this.panelCommandMenu.Controls.Add(this.panelStatusMenu);
            resources.ApplyResources(this.panelCommandMenu, "panelCommandMenu");
            this.panelCommandMenu.Name = "panelCommandMenu";
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnExit, "btnExit");
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.ForeColor = System.Drawing.SystemColors.Control;
            this.btnExit.Image = global::RorzeApi.Properties.Resources._32_logout;
            this.btnExit.Name = "btnExit";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnAlarm
            // 
            this.btnAlarm.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnAlarm, "btnAlarm");
            this.btnAlarm.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.btnAlarm.FlatAppearance.BorderSize = 0;
            this.btnAlarm.ForeColor = System.Drawing.SystemColors.Control;
            this.btnAlarm.Image = global::RorzeApi.Properties.Resources._32_alarm;
            this.btnAlarm.Name = "btnAlarm";
            this.btnAlarm.UseVisualStyleBackColor = false;
            this.btnAlarm.Click += new System.EventHandler(this.btnAlarm_Click);
            // 
            // panelMenu
            // 
            this.panelMenu.AllowDrop = true;
            this.panelMenu.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panelMenu.Controls.Add(this.btnAlarm);
            this.panelMenu.Controls.Add(this.panelFeaturesMenu);
            this.panelMenu.Controls.Add(this.btnExit);
            resources.ApplyResources(this.panelMenu, "panelMenu");
            this.panelMenu.Name = "panelMenu";
            // 
            // panelFeaturesMenu
            // 
            resources.ApplyResources(this.panelFeaturesMenu, "panelFeaturesMenu");
            this.panelFeaturesMenu.BackColor = System.Drawing.Color.Black;
            this.panelFeaturesMenu.Controls.Add(this.btnSecs);
            this.panelFeaturesMenu.Controls.Add(this.btnLog);
            this.panelFeaturesMenu.Controls.Add(this.btnSetup);
            this.panelFeaturesMenu.Controls.Add(this.btnMaintenance);
            this.panelFeaturesMenu.Controls.Add(this.btnInitialen);
            this.panelFeaturesMenu.Controls.Add(this.btnTeaching);
            this.panelFeaturesMenu.Controls.Add(this.btnStatus);
            this.panelFeaturesMenu.Name = "panelFeaturesMenu";
            // 
            // btnSecs
            // 
            this.btnSecs.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnSecs, "btnSecs");
            this.btnSecs.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.btnSecs.FlatAppearance.BorderSize = 0;
            this.btnSecs.ForeColor = System.Drawing.SystemColors.Control;
            this.btnSecs.Image = global::RorzeApi.Properties.Resources._32_automation;
            this.btnSecs.Name = "btnSecs";
            this.btnSecs.UseVisualStyleBackColor = false;
            this.btnSecs.Click += new System.EventHandler(this.btnSecs_Click);
            // 
            // btnLog
            // 
            this.btnLog.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnLog, "btnLog");
            this.btnLog.FlatAppearance.BorderSize = 0;
            this.btnLog.ForeColor = System.Drawing.SystemColors.Control;
            this.btnLog.Image = global::RorzeApi.Properties.Resources._32_folder;
            this.btnLog.Name = "btnLog";
            this.btnLog.UseVisualStyleBackColor = false;
            this.btnLog.Click += new System.EventHandler(this.btnLog_Click);
            // 
            // btnSetup
            // 
            this.btnSetup.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnSetup, "btnSetup");
            this.btnSetup.FlatAppearance.BorderSize = 0;
            this.btnSetup.ForeColor = System.Drawing.SystemColors.Control;
            this.btnSetup.Image = global::RorzeApi.Properties.Resources._32_document;
            this.btnSetup.Name = "btnSetup";
            this.btnSetup.UseVisualStyleBackColor = false;
            this.btnSetup.Click += new System.EventHandler(this.btnSetup_Click);
            // 
            // btnMaintenance
            // 
            this.btnMaintenance.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnMaintenance, "btnMaintenance");
            this.btnMaintenance.FlatAppearance.BorderSize = 0;
            this.btnMaintenance.ForeColor = System.Drawing.SystemColors.Control;
            this.btnMaintenance.Image = global::RorzeApi.Properties.Resources._32_support;
            this.btnMaintenance.Name = "btnMaintenance";
            this.btnMaintenance.UseVisualStyleBackColor = false;
            this.btnMaintenance.Click += new System.EventHandler(this.btnMaintenance_Click);
            // 
            // btnInitialen
            // 
            this.btnInitialen.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnInitialen, "btnInitialen");
            this.btnInitialen.FlatAppearance.BorderSize = 0;
            this.btnInitialen.ForeColor = System.Drawing.SystemColors.Control;
            this.btnInitialen.Image = global::RorzeApi.Properties.Resources._32_reset;
            this.btnInitialen.Name = "btnInitialen";
            this.btnInitialen.UseVisualStyleBackColor = false;
            this.btnInitialen.Click += new System.EventHandler(this.btnORGN_Click);
            // 
            // btnTeaching
            // 
            this.btnTeaching.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnTeaching, "btnTeaching");
            this.btnTeaching.FlatAppearance.BorderSize = 0;
            this.btnTeaching.ForeColor = System.Drawing.SystemColors.Control;
            this.btnTeaching.Image = global::RorzeApi.Properties.Resources._32_settings;
            this.btnTeaching.Name = "btnTeaching";
            this.btnTeaching.UseVisualStyleBackColor = false;
            this.btnTeaching.Click += new System.EventHandler(this.btnTeaching_Click);
            // 
            // btnStatus
            // 
            this.btnStatus.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnStatus, "btnStatus");
            this.btnStatus.FlatAppearance.BorderSize = 0;
            this.btnStatus.ForeColor = System.Drawing.SystemColors.Control;
            this.btnStatus.Image = global::RorzeApi.Properties.Resources._32_home;
            this.btnStatus.Name = "btnStatus";
            this.btnStatus.UseVisualStyleBackColor = false;
            this.btnStatus.Click += new System.EventHandler(this.btnOperation_Click);
            // 
            // panelChilForm
            // 
            this.panelChilForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(65)))));
            this.panelChilForm.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.panelChilForm, "panelChilForm");
            this.panelChilForm.Name = "panelChilForm";
            // 
            // lblUserName
            // 
            resources.ApplyResources(this.lblUserName, "lblUserName");
            this.lblUserName.ForeColor = System.Drawing.Color.White;
            this.lblUserName.Name = "lblUserName";
            // 
            // userLight1
            // 
            this.userLight1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.userLight1, "userLight1");
            this.userLight1.LightBlueStatus = RorzeApi.GUI.GUILight.enuStatus.eOn;
            this.userLight1.LightGreenStatus = RorzeApi.GUI.GUILight.enuStatus.eOn;
            this.userLight1.LightRedStatus = RorzeApi.GUI.GUILight.enuStatus.eOn;
            this.userLight1.LightYellowStatus = RorzeApi.GUI.GUILight.enuStatus.eOn;
            this.userLight1.Name = "userLight1";
            // 
            // uicimStatus1
            // 
            this.uicimStatus1.BackColor = System.Drawing.Color.Silver;
            this.uicimStatus1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.uicimStatus1, "uicimStatus1");
            this.uicimStatus1.iConn = Rorze.Secs.GEMCommStats.DISABLE;
            this.uicimStatus1.icontrol = Rorze.Secs.GEMControlStats.OFFLINE;
            this.uicimStatus1.iProcessStats = Rorze.Secs.GEMProcessStats.Init;
            this.uicimStatus1.Name = "uicimStatus1";
            // 
            // frmMDI
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            resources.ApplyResources(this, "$this");
            this.ControlBox = false;
            this.Controls.Add(this.panelCommandMenu);
            this.Controls.Add(this.panelChilForm);
            this.Controls.Add(this.panelMenu);
            this.Controls.Add(this.pnlHeader);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.IsMdiContainer = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmMDI";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMDI_FormClosing);
            this.Load += new System.EventHandler(this.frmMDI_Load);
            this.Shown += new System.EventHandler(this.frmMDI_Shown);
            this.pnlHeader.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.pnlShowName.ResumeLayout(false);
            this.panelCommandMenu.ResumeLayout(false);
            this.panelMenu.ResumeLayout(false);
            this.panelFeaturesMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer tmrUI;
        private System.Windows.Forms.Panel panelSecsMenu;
        private System.Windows.Forms.Panel panelLogMenu;
        private System.Windows.Forms.Panel panelSetupMenu;
        private System.Windows.Forms.Panel panelMaintenaceMenu;
        private System.Windows.Forms.Panel panelInitialenMenu;
        private System.Windows.Forms.Panel panelTeachingMenu;
        private System.Windows.Forms.Panel panelStatusMenu;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Panel panelCommandMenu;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        public System.Windows.Forms.Button btnBuzzerOff;
        public System.Windows.Forms.Button btnSignIn;
        private GUI.GUILight userLight1;
        public System.Windows.Forms.Button btnExit;
        public System.Windows.Forms.Button btnAlarm;
        private System.Windows.Forms.Panel panelMenu;
        private System.Windows.Forms.Panel panelFeaturesMenu;
        public System.Windows.Forms.Button btnSecs;
        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.Button btnSetup;
        private System.Windows.Forms.Button btnMaintenance;
        private System.Windows.Forms.Button btnInitialen;
        private System.Windows.Forms.Button btnTeaching;
        private System.Windows.Forms.Button btnStatus;
        private NewGem300Server_OOP.GUI.UICIMStatus uicimStatus1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labTime;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labPowerMode;
        private System.Windows.Forms.Label lblVersionValue;
        private System.Windows.Forms.Label labVersion;
        private System.Windows.Forms.Label labTimes;
        private System.Windows.Forms.Panel pnlShowName;
        private System.Windows.Forms.Label lblShowName;
        private System.Windows.Forms.Panel panelChilForm;
        private System.Windows.Forms.Label lblUserName;
    }
}

