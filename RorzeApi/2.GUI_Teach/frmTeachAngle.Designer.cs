namespace RorzeApi
{
    partial class frmTeachAngle
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmTeachAngle));
            this.tmrUI = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.gpRobot = new System.Windows.Forms.GroupBox();
            this.tlpSelectRobot = new System.Windows.Forms.TableLayoutPanel();
            this.tabCtrlStage = new System.Windows.Forms.TabControl();
            this.tabPageABCD = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.guiLoadport1 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport2 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport3 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport4 = new RorzeApi.GUI.GUILoadport();
            this.tabPageEFGH = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.guiLoadport5 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport6 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport7 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport8 = new RorzeApi.GUI.GUILoadport();
            this.gpAligner = new System.Windows.Forms.GroupBox();
            this.tlpSelectAligner = new System.Windows.Forms.TableLayoutPanel();
            this.btnModify = new System.Windows.Forms.Button();
            this.btnTeach = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.AngleE = new System.Windows.Forms.TextBox();
            this.grpTeachingData = new System.Windows.Forms.GroupBox();
            this.tlpNotchAngleSub = new System.Windows.Forms.TableLayoutPanel();
            this.gbStep = new System.Windows.Forms.GroupBox();
            this.btnAlignerCW = new System.Windows.Forms.Button();
            this.rbStep30 = new System.Windows.Forms.RadioButton();
            this.btnAlignerCCW = new System.Windows.Forms.Button();
            this.rbStep10 = new System.Windows.Forms.RadioButton();
            this.rbStep01 = new System.Windows.Forms.RadioButton();
            this.rbStep05 = new System.Windows.Forms.RadioButton();
            this.rbStep001 = new System.Windows.Forms.RadioButton();
            this.gbInstruct = new System.Windows.Forms.GroupBox();
            this.rtbInstruct = new System.Windows.Forms.RichTextBox();
            this.tlpnlStep = new System.Windows.Forms.TableLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.grpModifyData = new System.Windows.Forms.GroupBox();
            this.tlpNotchAngleAll = new System.Windows.Forms.TableLayoutPanel();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.gpRobot.SuspendLayout();
            this.tabCtrlStage.SuspendLayout();
            this.tabPageABCD.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tabPageEFGH.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.gpAligner.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.grpTeachingData.SuspendLayout();
            this.gbStep.SuspendLayout();
            this.gbInstruct.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.grpModifyData.SuspendLayout();
            this.SuspendLayout();
            // 
            // tmrUI
            // 
            this.tmrUI.Interval = 500;
            this.tmrUI.Tick += new System.EventHandler(this.tmrUI_Tick);
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Controls.Add(this.gpRobot);
            this.tabPage1.Controls.Add(this.tabCtrlStage);
            this.tabPage1.Controls.Add(this.gpAligner);
            this.tabPage1.Controls.Add(this.btnModify);
            this.tabPage1.Controls.Add(this.btnTeach);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // gpRobot
            // 
            resources.ApplyResources(this.gpRobot, "gpRobot");
            this.gpRobot.Controls.Add(this.tlpSelectRobot);
            this.gpRobot.Name = "gpRobot";
            this.gpRobot.TabStop = false;
            // 
            // tlpSelectRobot
            // 
            resources.ApplyResources(this.tlpSelectRobot, "tlpSelectRobot");
            this.tlpSelectRobot.Name = "tlpSelectRobot";
            // 
            // tabCtrlStage
            // 
            resources.ApplyResources(this.tabCtrlStage, "tabCtrlStage");
            this.tabCtrlStage.Controls.Add(this.tabPageABCD);
            this.tabCtrlStage.Controls.Add(this.tabPageEFGH);
            this.tabCtrlStage.Multiline = true;
            this.tabCtrlStage.Name = "tabCtrlStage";
            this.tabCtrlStage.SelectedIndex = 0;
            // 
            // tabPageABCD
            // 
            resources.ApplyResources(this.tabPageABCD, "tabPageABCD");
            this.tabPageABCD.Controls.Add(this.flowLayoutPanel1);
            this.tabPageABCD.Name = "tabPageABCD";
            this.tabPageABCD.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport1);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport2);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport3);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport4);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // guiLoadport1
            // 
            resources.ApplyResources(this.guiLoadport1, "guiLoadport1");
            this.guiLoadport1.BodyNo = 1;
            this.guiLoadport1.Disable_ClmpLock = true;
            this.guiLoadport1.Disable_DockBtn = true;
            this.guiLoadport1.Disable_E84 = true;
            this.guiLoadport1.Disable_OCR = true;
            this.guiLoadport1.Disable_ProcessBtn = true;
            this.guiLoadport1.Disable_Recipe = true;
            this.guiLoadport1.Disable_RSV = true;
            this.guiLoadport1.Disable_SelectWafer = false;
            this.guiLoadport1.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport1.FoupID = "";
            this.guiLoadport1.KeepClamp = false;
            this.guiLoadport1.MapData = "0000000000000000000000000";
            this.guiLoadport1.Name = "guiLoadport1";
            this.guiLoadport1.Recipe = "";
            this.guiLoadport1.RSV = "";
            this.guiLoadport1.SelectForStocker = false;
            this.guiLoadport1.SelectWaferBySorterMode = false;
            this.guiLoadport1.ShowSelectColor = false;
            this.guiLoadport1.Simulate = false;
            this.guiLoadport1.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            // 
            // guiLoadport2
            // 
            resources.ApplyResources(this.guiLoadport2, "guiLoadport2");
            this.guiLoadport2.BodyNo = 2;
            this.guiLoadport2.Disable_ClmpLock = true;
            this.guiLoadport2.Disable_DockBtn = true;
            this.guiLoadport2.Disable_E84 = true;
            this.guiLoadport2.Disable_OCR = true;
            this.guiLoadport2.Disable_ProcessBtn = true;
            this.guiLoadport2.Disable_Recipe = true;
            this.guiLoadport2.Disable_RSV = true;
            this.guiLoadport2.Disable_SelectWafer = false;
            this.guiLoadport2.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport2.FoupID = "";
            this.guiLoadport2.KeepClamp = false;
            this.guiLoadport2.MapData = "0000000000000000000000000";
            this.guiLoadport2.Name = "guiLoadport2";
            this.guiLoadport2.Recipe = "";
            this.guiLoadport2.RSV = "";
            this.guiLoadport2.SelectForStocker = false;
            this.guiLoadport2.SelectWaferBySorterMode = false;
            this.guiLoadport2.ShowSelectColor = false;
            this.guiLoadport2.Simulate = false;
            this.guiLoadport2.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            // 
            // guiLoadport3
            // 
            resources.ApplyResources(this.guiLoadport3, "guiLoadport3");
            this.guiLoadport3.BodyNo = 3;
            this.guiLoadport3.Disable_ClmpLock = true;
            this.guiLoadport3.Disable_DockBtn = true;
            this.guiLoadport3.Disable_E84 = true;
            this.guiLoadport3.Disable_OCR = true;
            this.guiLoadport3.Disable_ProcessBtn = true;
            this.guiLoadport3.Disable_Recipe = true;
            this.guiLoadport3.Disable_RSV = true;
            this.guiLoadport3.Disable_SelectWafer = false;
            this.guiLoadport3.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport3.FoupID = "";
            this.guiLoadport3.KeepClamp = false;
            this.guiLoadport3.MapData = "0000000000000000000000000";
            this.guiLoadport3.Name = "guiLoadport3";
            this.guiLoadport3.Recipe = "";
            this.guiLoadport3.RSV = "";
            this.guiLoadport3.SelectForStocker = false;
            this.guiLoadport3.SelectWaferBySorterMode = false;
            this.guiLoadport3.ShowSelectColor = false;
            this.guiLoadport3.Simulate = false;
            this.guiLoadport3.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            // 
            // guiLoadport4
            // 
            resources.ApplyResources(this.guiLoadport4, "guiLoadport4");
            this.guiLoadport4.BodyNo = 4;
            this.guiLoadport4.Disable_ClmpLock = true;
            this.guiLoadport4.Disable_DockBtn = true;
            this.guiLoadport4.Disable_E84 = true;
            this.guiLoadport4.Disable_OCR = true;
            this.guiLoadport4.Disable_ProcessBtn = true;
            this.guiLoadport4.Disable_Recipe = true;
            this.guiLoadport4.Disable_RSV = true;
            this.guiLoadport4.Disable_SelectWafer = false;
            this.guiLoadport4.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport4.FoupID = "";
            this.guiLoadport4.KeepClamp = false;
            this.guiLoadport4.MapData = "0000000000000000000000000";
            this.guiLoadport4.Name = "guiLoadport4";
            this.guiLoadport4.Recipe = "";
            this.guiLoadport4.RSV = "";
            this.guiLoadport4.SelectForStocker = false;
            this.guiLoadport4.SelectWaferBySorterMode = false;
            this.guiLoadport4.ShowSelectColor = false;
            this.guiLoadport4.Simulate = false;
            this.guiLoadport4.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            // 
            // tabPageEFGH
            // 
            resources.ApplyResources(this.tabPageEFGH, "tabPageEFGH");
            this.tabPageEFGH.Controls.Add(this.flowLayoutPanel2);
            this.tabPageEFGH.Name = "tabPageEFGH";
            this.tabPageEFGH.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel2
            // 
            resources.ApplyResources(this.flowLayoutPanel2, "flowLayoutPanel2");
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport5);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport6);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport7);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport8);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            // 
            // guiLoadport5
            // 
            resources.ApplyResources(this.guiLoadport5, "guiLoadport5");
            this.guiLoadport5.BodyNo = 5;
            this.guiLoadport5.Disable_ClmpLock = true;
            this.guiLoadport5.Disable_DockBtn = true;
            this.guiLoadport5.Disable_E84 = true;
            this.guiLoadport5.Disable_OCR = true;
            this.guiLoadport5.Disable_ProcessBtn = true;
            this.guiLoadport5.Disable_Recipe = true;
            this.guiLoadport5.Disable_RSV = true;
            this.guiLoadport5.Disable_SelectWafer = false;
            this.guiLoadport5.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport5.FoupID = "";
            this.guiLoadport5.KeepClamp = false;
            this.guiLoadport5.MapData = "0000000000000000000000000";
            this.guiLoadport5.Name = "guiLoadport5";
            this.guiLoadport5.Recipe = "";
            this.guiLoadport5.RSV = "";
            this.guiLoadport5.SelectForStocker = false;
            this.guiLoadport5.SelectWaferBySorterMode = false;
            this.guiLoadport5.ShowSelectColor = false;
            this.guiLoadport5.Simulate = false;
            this.guiLoadport5.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            // 
            // guiLoadport6
            // 
            resources.ApplyResources(this.guiLoadport6, "guiLoadport6");
            this.guiLoadport6.BodyNo = 6;
            this.guiLoadport6.Disable_ClmpLock = true;
            this.guiLoadport6.Disable_DockBtn = true;
            this.guiLoadport6.Disable_E84 = true;
            this.guiLoadport6.Disable_OCR = true;
            this.guiLoadport6.Disable_ProcessBtn = true;
            this.guiLoadport6.Disable_Recipe = true;
            this.guiLoadport6.Disable_RSV = true;
            this.guiLoadport6.Disable_SelectWafer = false;
            this.guiLoadport6.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport6.FoupID = "";
            this.guiLoadport6.KeepClamp = false;
            this.guiLoadport6.MapData = "0000000000000000000000000";
            this.guiLoadport6.Name = "guiLoadport6";
            this.guiLoadport6.Recipe = "";
            this.guiLoadport6.RSV = "";
            this.guiLoadport6.SelectForStocker = false;
            this.guiLoadport6.SelectWaferBySorterMode = false;
            this.guiLoadport6.ShowSelectColor = false;
            this.guiLoadport6.Simulate = false;
            this.guiLoadport6.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            // 
            // guiLoadport7
            // 
            resources.ApplyResources(this.guiLoadport7, "guiLoadport7");
            this.guiLoadport7.BodyNo = 7;
            this.guiLoadport7.Disable_ClmpLock = true;
            this.guiLoadport7.Disable_DockBtn = true;
            this.guiLoadport7.Disable_E84 = true;
            this.guiLoadport7.Disable_OCR = true;
            this.guiLoadport7.Disable_ProcessBtn = true;
            this.guiLoadport7.Disable_Recipe = true;
            this.guiLoadport7.Disable_RSV = true;
            this.guiLoadport7.Disable_SelectWafer = false;
            this.guiLoadport7.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport7.FoupID = "";
            this.guiLoadport7.KeepClamp = false;
            this.guiLoadport7.MapData = "0000000000000000000000000";
            this.guiLoadport7.Name = "guiLoadport7";
            this.guiLoadport7.Recipe = "";
            this.guiLoadport7.RSV = "";
            this.guiLoadport7.SelectForStocker = false;
            this.guiLoadport7.SelectWaferBySorterMode = false;
            this.guiLoadport7.ShowSelectColor = false;
            this.guiLoadport7.Simulate = false;
            this.guiLoadport7.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            // 
            // guiLoadport8
            // 
            resources.ApplyResources(this.guiLoadport8, "guiLoadport8");
            this.guiLoadport8.BodyNo = 8;
            this.guiLoadport8.Disable_ClmpLock = true;
            this.guiLoadport8.Disable_DockBtn = true;
            this.guiLoadport8.Disable_E84 = true;
            this.guiLoadport8.Disable_OCR = true;
            this.guiLoadport8.Disable_ProcessBtn = true;
            this.guiLoadport8.Disable_Recipe = true;
            this.guiLoadport8.Disable_RSV = true;
            this.guiLoadport8.Disable_SelectWafer = false;
            this.guiLoadport8.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport8.FoupID = "";
            this.guiLoadport8.KeepClamp = false;
            this.guiLoadport8.MapData = "0000000000000000000000000";
            this.guiLoadport8.Name = "guiLoadport8";
            this.guiLoadport8.Recipe = "";
            this.guiLoadport8.RSV = "";
            this.guiLoadport8.SelectForStocker = false;
            this.guiLoadport8.SelectWaferBySorterMode = false;
            this.guiLoadport8.ShowSelectColor = false;
            this.guiLoadport8.Simulate = false;
            this.guiLoadport8.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            // 
            // gpAligner
            // 
            resources.ApplyResources(this.gpAligner, "gpAligner");
            this.gpAligner.Controls.Add(this.tlpSelectAligner);
            this.gpAligner.Name = "gpAligner";
            this.gpAligner.TabStop = false;
            // 
            // tlpSelectAligner
            // 
            resources.ApplyResources(this.tlpSelectAligner, "tlpSelectAligner");
            this.tlpSelectAligner.Name = "tlpSelectAligner";
            // 
            // btnModify
            // 
            resources.ApplyResources(this.btnModify, "btnModify");
            this.btnModify.Image = global::RorzeApi.Properties.Resources._48_edit_file;
            this.btnModify.Name = "btnModify";
            this.btnModify.UseVisualStyleBackColor = true;
            this.btnModify.Click += new System.EventHandler(this.btnModify_Click);
            // 
            // btnTeach
            // 
            resources.ApplyResources(this.btnTeach, "btnTeach");
            this.btnTeach.Image = global::RorzeApi.Properties.Resources._48_work;
            this.btnTeach.Name = "btnTeach";
            this.btnTeach.UseVisualStyleBackColor = true;
            this.btnTeach.Click += new System.EventHandler(this.btnTeach_Click);
            // 
            // tabPage2
            // 
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Controls.Add(this.grpTeachingData);
            this.tabPage2.Controls.Add(this.gbStep);
            this.tabPage2.Controls.Add(this.gbInstruct);
            this.tabPage2.Controls.Add(this.btnCancel);
            this.tabPage2.Controls.Add(this.btnNext);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.AngleE);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // AngleE
            // 
            resources.ApplyResources(this.AngleE, "AngleE");
            this.AngleE.Name = "AngleE";
            this.AngleE.ReadOnly = true;
            // 
            // grpTeachingData
            // 
            resources.ApplyResources(this.grpTeachingData, "grpTeachingData");
            this.grpTeachingData.Controls.Add(this.tlpNotchAngleSub);
            this.grpTeachingData.Name = "grpTeachingData";
            this.grpTeachingData.TabStop = false;
            // 
            // tlpNotchAngleSub
            // 
            resources.ApplyResources(this.tlpNotchAngleSub, "tlpNotchAngleSub");
            this.tlpNotchAngleSub.Name = "tlpNotchAngleSub";
            // 
            // gbStep
            // 
            resources.ApplyResources(this.gbStep, "gbStep");
            this.gbStep.Controls.Add(this.btnAlignerCW);
            this.gbStep.Controls.Add(this.rbStep30);
            this.gbStep.Controls.Add(this.btnAlignerCCW);
            this.gbStep.Controls.Add(this.rbStep10);
            this.gbStep.Controls.Add(this.rbStep01);
            this.gbStep.Controls.Add(this.rbStep05);
            this.gbStep.Controls.Add(this.rbStep001);
            this.gbStep.Name = "gbStep";
            this.gbStep.TabStop = false;
            // 
            // btnAlignerCW
            // 
            resources.ApplyResources(this.btnAlignerCW, "btnAlignerCW");
            this.btnAlignerCW.Image = global::RorzeApi.Properties.Resources.TeachCW;
            this.btnAlignerCW.Name = "btnAlignerCW";
            this.btnAlignerCW.UseVisualStyleBackColor = true;
            this.btnAlignerCW.Click += new System.EventHandler(this.btnAlignerRotFW_Click);
            // 
            // rbStep30
            // 
            resources.ApplyResources(this.rbStep30, "rbStep30");
            this.rbStep30.Checked = true;
            this.rbStep30.Name = "rbStep30";
            this.rbStep30.TabStop = true;
            this.rbStep30.Tag = "30";
            this.rbStep30.UseVisualStyleBackColor = true;
            this.rbStep30.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // btnAlignerCCW
            // 
            resources.ApplyResources(this.btnAlignerCCW, "btnAlignerCCW");
            this.btnAlignerCCW.Image = global::RorzeApi.Properties.Resources.TeachCCW;
            this.btnAlignerCCW.Name = "btnAlignerCCW";
            this.btnAlignerCCW.UseVisualStyleBackColor = true;
            this.btnAlignerCCW.Click += new System.EventHandler(this.btnAlignerRotBW_Click);
            // 
            // rbStep10
            // 
            resources.ApplyResources(this.rbStep10, "rbStep10");
            this.rbStep10.Name = "rbStep10";
            this.rbStep10.Tag = "10";
            this.rbStep10.UseVisualStyleBackColor = true;
            this.rbStep10.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // rbStep01
            // 
            resources.ApplyResources(this.rbStep01, "rbStep01");
            this.rbStep01.Name = "rbStep01";
            this.rbStep01.Tag = "1";
            this.rbStep01.UseVisualStyleBackColor = true;
            this.rbStep01.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // rbStep05
            // 
            resources.ApplyResources(this.rbStep05, "rbStep05");
            this.rbStep05.Name = "rbStep05";
            this.rbStep05.Tag = "5";
            this.rbStep05.UseVisualStyleBackColor = true;
            this.rbStep05.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // rbStep001
            // 
            resources.ApplyResources(this.rbStep001, "rbStep001");
            this.rbStep001.Name = "rbStep001";
            this.rbStep001.Tag = "0.1";
            this.rbStep001.UseVisualStyleBackColor = true;
            this.rbStep001.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // gbInstruct
            // 
            resources.ApplyResources(this.gbInstruct, "gbInstruct");
            this.gbInstruct.Controls.Add(this.rtbInstruct);
            this.gbInstruct.Controls.Add(this.tlpnlStep);
            this.gbInstruct.Name = "gbInstruct";
            this.gbInstruct.TabStop = false;
            // 
            // rtbInstruct
            // 
            resources.ApplyResources(this.rtbInstruct, "rtbInstruct");
            this.rtbInstruct.Name = "rtbInstruct";
            this.rtbInstruct.ReadOnly = true;
            // 
            // tlpnlStep
            // 
            resources.ApplyResources(this.tlpnlStep, "tlpnlStep");
            this.tlpnlStep.Name = "tlpnlStep";
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.Image = global::RorzeApi.Properties.Resources._32_cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnNext
            // 
            resources.ApplyResources(this.btnNext, "btnNext");
            this.btnNext.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnNext.Image = global::RorzeApi.Properties.Resources._32_next;
            this.btnNext.Name = "btnNext";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // tabPage3
            // 
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Controls.Add(this.btnBack);
            this.tabPage3.Controls.Add(this.btnSave);
            this.tabPage3.Controls.Add(this.grpModifyData);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // btnBack
            // 
            resources.ApplyResources(this.btnBack, "btnBack");
            this.btnBack.Image = global::RorzeApi.Properties.Resources._48_arrowback;
            this.btnBack.Name = "btnBack";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnSave
            // 
            resources.ApplyResources(this.btnSave, "btnSave");
            this.btnSave.Image = global::RorzeApi.Properties.Resources._48_save;
            this.btnSave.Name = "btnSave";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click_1);
            // 
            // grpModifyData
            // 
            resources.ApplyResources(this.grpModifyData, "grpModifyData");
            this.grpModifyData.Controls.Add(this.tlpNotchAngleAll);
            this.grpModifyData.Name = "grpModifyData";
            this.grpModifyData.TabStop = false;
            // 
            // tlpNotchAngleAll
            // 
            resources.ApplyResources(this.tlpNotchAngleAll, "tlpNotchAngleAll");
            this.tlpNotchAngleAll.Name = "tlpNotchAngleAll";
            // 
            // frmTeachAngle
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmTeachAngle";
            this.VisibleChanged += new System.EventHandler(this.frmTeachOCR_VisibleChanged);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.gpRobot.ResumeLayout(false);
            this.tabCtrlStage.ResumeLayout(false);
            this.tabPageABCD.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tabPageEFGH.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.gpAligner.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.grpTeachingData.ResumeLayout(false);
            this.gbStep.ResumeLayout(false);
            this.gbStep.PerformLayout();
            this.gbInstruct.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.grpModifyData.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer tmrUI;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox gbInstruct;
        private System.Windows.Forms.RichTextBox rtbInstruct;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnTeach;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox AngleE;
        private System.Windows.Forms.GroupBox gbStep;
        private System.Windows.Forms.RadioButton rbStep30;
        private System.Windows.Forms.RadioButton rbStep10;
        private System.Windows.Forms.RadioButton rbStep01;
        private System.Windows.Forms.RadioButton rbStep05;
        private System.Windows.Forms.RadioButton rbStep001;
        private System.Windows.Forms.Button btnAlignerCW;
        private System.Windows.Forms.Button btnAlignerCCW;
        private System.Windows.Forms.GroupBox grpTeachingData;
        private System.Windows.Forms.Button btnModify;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.GroupBox grpModifyData;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.TabControl tabCtrlStage;
        private System.Windows.Forms.TabPage tabPageABCD;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private GUI.GUILoadport guiLoadport1;
        private GUI.GUILoadport guiLoadport2;
        private GUI.GUILoadport guiLoadport3;
        private GUI.GUILoadport guiLoadport4;
        private System.Windows.Forms.TabPage tabPageEFGH;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private GUI.GUILoadport guiLoadport5;
        private GUI.GUILoadport guiLoadport6;
        private GUI.GUILoadport guiLoadport7;
        private GUI.GUILoadport guiLoadport8;
        private System.Windows.Forms.GroupBox gpAligner;
        private System.Windows.Forms.TableLayoutPanel tlpSelectAligner;
        private System.Windows.Forms.TableLayoutPanel tlpNotchAngleAll;
        private System.Windows.Forms.GroupBox gpRobot;
        private System.Windows.Forms.TableLayoutPanel tlpSelectRobot;
        private System.Windows.Forms.TableLayoutPanel tlpnlStep;
        private System.Windows.Forms.TableLayoutPanel tlpNotchAngleSub;
    }
}