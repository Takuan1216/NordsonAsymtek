namespace RorzeApi
{
    partial class frmCycle
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCycle));
            this.tmrUpdateUI = new System.Windows.Forms.Timer(this.components);
            this.panelLoadportGui = new System.Windows.Forms.Panel();
            this.tabCtrlStage = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.guiLoadport1 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport2 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport3 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport4 = new RorzeApi.GUI.GUILoadport();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.guiLoadport5 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport6 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport7 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport8 = new RorzeApi.GUI.GUILoadport();
            this.guiAlignerBStatus = new RorzeApi._0.GUI_UserCtrl.GUIAlignerStatus();
            this.guiAlignerAStatus = new RorzeApi._0.GUI_UserCtrl.GUIAlignerStatus();
            this.guiRobotStatus = new RorzeApi._0.GUI_UserCtrl.GUIRobotStatus();
            this.guiEquipmentStatus = new RorzeApi._0.GUI_UserCtrl.GUIEquipmentStatus();
            this.panelUnit = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.btnFinish = new System.Windows.Forms.Button();
            this.btnCycle = new System.Windows.Forms.Button();
            this.label26 = new System.Windows.Forms.Label();
            this.lblCycleCout = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.lblCycleEndTime = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblCycleStartTime = new System.Windows.Forms.Label();
            this.panel7 = new System.Windows.Forms.Panel();
            this.panelLoadportGui.SuspendLayout();
            this.tabCtrlStage.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.panelUnit.SuspendLayout();
            this.panel3.SuspendLayout();
            this.tableLayoutPanel8.SuspendLayout();
            this.panel7.SuspendLayout();
            this.SuspendLayout();
            // 
            // tmrUpdateUI
            // 
            this.tmrUpdateUI.Tick += new System.EventHandler(this.tmrUpdateUI_Tick);
            // 
            // panelLoadportGui
            // 
            this.panelLoadportGui.AutoScroll = true;
            this.panelLoadportGui.Controls.Add(this.tabCtrlStage);
            this.panelLoadportGui.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLoadportGui.Location = new System.Drawing.Point(0, 0);
            this.panelLoadportGui.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.panelLoadportGui.Name = "panelLoadportGui";
            this.panelLoadportGui.Size = new System.Drawing.Size(650, 700);
            this.panelLoadportGui.TabIndex = 113;
            // 
            // tabCtrlStage
            // 
            this.tabCtrlStage.Controls.Add(this.tabPage1);
            this.tabCtrlStage.Controls.Add(this.tabPage2);
            this.tabCtrlStage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabCtrlStage.Location = new System.Drawing.Point(0, 0);
            this.tabCtrlStage.Multiline = true;
            this.tabCtrlStage.Name = "tabCtrlStage";
            this.tabCtrlStage.SelectedIndex = 0;
            this.tabCtrlStage.Size = new System.Drawing.Size(650, 700);
            this.tabCtrlStage.TabIndex = 9;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.flowLayoutPanel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 23);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(642, 673);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "ABCD";
            this.tabPage1.ToolTipText = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport1);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport2);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport3);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport4);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(636, 667);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // guiLoadport1
            // 
            this.guiLoadport1.BodyNo = 1;
            this.guiLoadport1.Disable_ClmpLock = true;
            this.guiLoadport1.Disable_E84 = true;
            this.guiLoadport1.Disable_OCR = true;
            this.guiLoadport1.Disable_ProcessBtn = true;
            this.guiLoadport1.Disable_Recipe = true;
            this.guiLoadport1.Disable_RSV = true;
            this.guiLoadport1.Disable_SelectWafer = false;

            this.guiLoadport1.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport1.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guiLoadport1.FoupID = "";
            this.guiLoadport1.KeepClamp = false;
            this.guiLoadport1.Location = new System.Drawing.Point(0, 0);

            this.guiLoadport1.Margin = new System.Windows.Forms.Padding(0);
            this.guiLoadport1.Name = "guiLoadport1";
            this.guiLoadport1.Recipe = "";
            this.guiLoadport1.RSV = "";
            this.guiLoadport1.SelectWaferBySorterMode = false;
            this.guiLoadport1.Simulate = false;
            this.guiLoadport1.Size = new System.Drawing.Size(158, 667);
            this.guiLoadport1.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            this.guiLoadport1.TabIndex = 2;
            // 
            // guiLoadport2
            // 
            this.guiLoadport2.BodyNo = 2;
            this.guiLoadport2.Disable_ClmpLock = true;
            this.guiLoadport2.Disable_E84 = true;
            this.guiLoadport2.Disable_OCR = true;
            this.guiLoadport2.Disable_ProcessBtn = true;
            this.guiLoadport2.Disable_Recipe = true;
            this.guiLoadport2.Disable_RSV = true;
            this.guiLoadport2.Disable_SelectWafer = false;
   
            this.guiLoadport2.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport2.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guiLoadport2.FoupID = "";
            this.guiLoadport2.KeepClamp = false;
            this.guiLoadport2.Location = new System.Drawing.Point(158, 0);
   
            this.guiLoadport2.Margin = new System.Windows.Forms.Padding(0);
            this.guiLoadport2.Name = "guiLoadport2";
            this.guiLoadport2.Recipe = "";
            this.guiLoadport2.RSV = "";
            this.guiLoadport2.SelectWaferBySorterMode = false;
            this.guiLoadport2.Simulate = false;
            this.guiLoadport2.Size = new System.Drawing.Size(158, 667);
            this.guiLoadport2.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            this.guiLoadport2.TabIndex = 3;
            // 
            // guiLoadport3
            // 
            this.guiLoadport3.BodyNo = 3;
            this.guiLoadport3.Disable_ClmpLock = true;
            this.guiLoadport3.Disable_E84 = true;
            this.guiLoadport3.Disable_OCR = true;
            this.guiLoadport3.Disable_ProcessBtn = true;
            this.guiLoadport3.Disable_Recipe = true;
            this.guiLoadport3.Disable_RSV = true;
            this.guiLoadport3.Disable_SelectWafer = false;

            this.guiLoadport3.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport3.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guiLoadport3.FoupID = "";
            this.guiLoadport3.KeepClamp = false;
            this.guiLoadport3.Location = new System.Drawing.Point(316, 0);

            this.guiLoadport3.Margin = new System.Windows.Forms.Padding(0);
            this.guiLoadport3.Name = "guiLoadport3";
            this.guiLoadport3.Recipe = "";
            this.guiLoadport3.RSV = "";
            this.guiLoadport3.SelectWaferBySorterMode = false;
            this.guiLoadport3.Simulate = false;
            this.guiLoadport3.Size = new System.Drawing.Size(158, 667);
            this.guiLoadport3.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            this.guiLoadport3.TabIndex = 4;
            // 
            // guiLoadport4
            // 
            this.guiLoadport4.BodyNo = 4;
            this.guiLoadport4.Disable_ClmpLock = true;
            this.guiLoadport4.Disable_E84 = true;
            this.guiLoadport4.Disable_OCR = true;
            this.guiLoadport4.Disable_ProcessBtn = true;
            this.guiLoadport4.Disable_Recipe = true;
            this.guiLoadport4.Disable_RSV = true;
            this.guiLoadport4.Disable_SelectWafer = false;

            this.guiLoadport4.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport4.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guiLoadport4.FoupID = "";
            this.guiLoadport4.KeepClamp = false;
            this.guiLoadport4.Location = new System.Drawing.Point(474, 0);

            this.guiLoadport4.Margin = new System.Windows.Forms.Padding(0);
            this.guiLoadport4.Name = "guiLoadport4";
            this.guiLoadport4.Recipe = "";
            this.guiLoadport4.RSV = "";
            this.guiLoadport4.SelectWaferBySorterMode = false;
            this.guiLoadport4.Simulate = false;
            this.guiLoadport4.Size = new System.Drawing.Size(158, 667);
            this.guiLoadport4.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            this.guiLoadport4.TabIndex = 5;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.flowLayoutPanel2);
            this.tabPage2.Location = new System.Drawing.Point(4, 23);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(642, 673);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "EFGH";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport5);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport6);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport7);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport8);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(636, 667);
            this.flowLayoutPanel2.TabIndex = 2;
            // 
            // guiLoadport5
            // 
            this.guiLoadport5.BodyNo = 5;
            this.guiLoadport5.Disable_ClmpLock = true;
            this.guiLoadport5.Disable_E84 = true;
            this.guiLoadport5.Disable_OCR = true;
            this.guiLoadport5.Disable_ProcessBtn = true;
            this.guiLoadport5.Disable_Recipe = true;
            this.guiLoadport5.Disable_RSV = true;
            this.guiLoadport5.Disable_SelectWafer = false;
    
            this.guiLoadport5.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport5.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guiLoadport5.FoupID = "";
            this.guiLoadport5.KeepClamp = false;
            this.guiLoadport5.Location = new System.Drawing.Point(0, 0);

            this.guiLoadport5.Margin = new System.Windows.Forms.Padding(0);
            this.guiLoadport5.Name = "guiLoadport5";
            this.guiLoadport5.Recipe = "";
            this.guiLoadport5.RSV = "";
            this.guiLoadport5.SelectWaferBySorterMode = false;
            this.guiLoadport5.Simulate = false;
            this.guiLoadport5.Size = new System.Drawing.Size(158, 667);
            this.guiLoadport5.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            this.guiLoadport5.TabIndex = 2;
            // 
            // guiLoadport6
            // 
            this.guiLoadport6.BodyNo = 6;
            this.guiLoadport6.Disable_ClmpLock = true;
            this.guiLoadport6.Disable_E84 = true;
            this.guiLoadport6.Disable_OCR = true;
            this.guiLoadport6.Disable_ProcessBtn = true;
            this.guiLoadport6.Disable_Recipe = true;
            this.guiLoadport6.Disable_RSV = true;
            this.guiLoadport6.Disable_SelectWafer = false;

            this.guiLoadport6.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport6.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guiLoadport6.FoupID = "";
            this.guiLoadport6.KeepClamp = false;
            this.guiLoadport6.Location = new System.Drawing.Point(158, 0);

            this.guiLoadport6.Margin = new System.Windows.Forms.Padding(0);
            this.guiLoadport6.Name = "guiLoadport6";
            this.guiLoadport6.Recipe = "";
            this.guiLoadport6.RSV = "";
            this.guiLoadport6.SelectWaferBySorterMode = false;
            this.guiLoadport6.Simulate = false;
            this.guiLoadport6.Size = new System.Drawing.Size(158, 667);
            this.guiLoadport6.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            this.guiLoadport6.TabIndex = 3;
            // 
            // guiLoadport7
            // 
            this.guiLoadport7.BodyNo = 7;
            this.guiLoadport7.Disable_ClmpLock = true;
            this.guiLoadport7.Disable_E84 = true;
            this.guiLoadport7.Disable_OCR = true;
            this.guiLoadport7.Disable_ProcessBtn = true;
            this.guiLoadport7.Disable_Recipe = true;
            this.guiLoadport7.Disable_RSV = true;
            this.guiLoadport7.Disable_SelectWafer = false;
       
            this.guiLoadport7.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport7.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guiLoadport7.FoupID = "";
            this.guiLoadport7.KeepClamp = false;
            this.guiLoadport7.Location = new System.Drawing.Point(316, 0);
   
            this.guiLoadport7.Margin = new System.Windows.Forms.Padding(0);
            this.guiLoadport7.Name = "guiLoadport7";
            this.guiLoadport7.Recipe = "";
            this.guiLoadport7.RSV = "";
            this.guiLoadport7.SelectWaferBySorterMode = false;
            this.guiLoadport7.Simulate = false;
            this.guiLoadport7.Size = new System.Drawing.Size(158, 667);
            this.guiLoadport7.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            this.guiLoadport7.TabIndex = 4;
            // 
            // guiLoadport8
            // 
            this.guiLoadport8.BodyNo = 8;
            this.guiLoadport8.Disable_ClmpLock = true;
            this.guiLoadport8.Disable_E84 = true;
            this.guiLoadport8.Disable_OCR = true;
            this.guiLoadport8.Disable_ProcessBtn = true;
            this.guiLoadport8.Disable_Recipe = true;
            this.guiLoadport8.Disable_RSV = true;
            this.guiLoadport8.Disable_SelectWafer = false;
    
            this.guiLoadport8.E84Status = RorzeApi.GUI.GUILoadport.enumE84Status.Manual;
            this.guiLoadport8.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guiLoadport8.FoupID = "";
            this.guiLoadport8.KeepClamp = false;
            this.guiLoadport8.Location = new System.Drawing.Point(474, 0);
       
            this.guiLoadport8.Margin = new System.Windows.Forms.Padding(0);
            this.guiLoadport8.Name = "guiLoadport8";
            this.guiLoadport8.Recipe = "";
            this.guiLoadport8.RSV = "";
            this.guiLoadport8.SelectWaferBySorterMode = false;
            this.guiLoadport8.Simulate = false;
            this.guiLoadport8.Size = new System.Drawing.Size(158, 667);
            this.guiLoadport8.Status = RorzeApi.GUI.GUILoadport.enumLoadportStatus.Abort;
            this.guiLoadport8.TabIndex = 5;
            // 
            // guiAlignerBStatus
            // 
            this.guiAlignerBStatus.BodyNo = 2;
            this.guiAlignerBStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.guiAlignerBStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.guiAlignerBStatus.Font = new System.Drawing.Font("Calibri", 9F);
            this.guiAlignerBStatus.Location = new System.Drawing.Point(0, 451);
            this.guiAlignerBStatus.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.guiAlignerBStatus.Name = "guiAlignerBStatus";
            this.guiAlignerBStatus.Size = new System.Drawing.Size(320, 100);
            this.guiAlignerBStatus.TabIndex = 2;
            // 
            // guiAlignerAStatus
            // 
            this.guiAlignerAStatus.BodyNo = 1;
            this.guiAlignerAStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.guiAlignerAStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.guiAlignerAStatus.Font = new System.Drawing.Font("Calibri", 9F);
            this.guiAlignerAStatus.Location = new System.Drawing.Point(0, 351);
            this.guiAlignerAStatus.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.guiAlignerAStatus.Name = "guiAlignerAStatus";
            this.guiAlignerAStatus.Size = new System.Drawing.Size(320, 100);
            this.guiAlignerAStatus.TabIndex = 3;
            // 
            // guiRobotStatus
            // 
            this.guiRobotStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.guiRobotStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.guiRobotStatus.Font = new System.Drawing.Font("Calibri", 9F);
            this.guiRobotStatus.Location = new System.Drawing.Point(0, 173);
            this.guiRobotStatus.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.guiRobotStatus.Name = "guiRobotStatus";
            this.guiRobotStatus.Size = new System.Drawing.Size(320, 178);
            this.guiRobotStatus.TabIndex = 4;
            // 
            // guiEquipmentStatus
            // 
            this.guiEquipmentStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.guiEquipmentStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.guiEquipmentStatus.EQName = "YXLON";
            this.guiEquipmentStatus.Font = new System.Drawing.Font("Calibri", 9F);
            this.guiEquipmentStatus.Location = new System.Drawing.Point(0, 68);
            this.guiEquipmentStatus.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.guiEquipmentStatus.Name = "guiEquipmentStatus";
            this.guiEquipmentStatus.Size = new System.Drawing.Size(320, 105);
            this.guiEquipmentStatus.TabIndex = 316;
            // 
            // panelUnit
            // 
            this.panelUnit.Controls.Add(this.guiEquipmentStatus);
            this.panelUnit.Controls.Add(this.guiRobotStatus);
            this.panelUnit.Controls.Add(this.guiAlignerAStatus);
            this.panelUnit.Controls.Add(this.guiAlignerBStatus);
            this.panelUnit.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelUnit.Location = new System.Drawing.Point(650, 149);
            this.panelUnit.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.panelUnit.Name = "panelUnit";
            this.panelUnit.Size = new System.Drawing.Size(320, 551);
            this.panelUnit.TabIndex = 117;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.tableLayoutPanel8);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(320, 129);
            this.panel3.TabIndex = 0;
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.tableLayoutPanel8.ColumnCount = 4;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel8.Controls.Add(this.btnFinish, 2, 4);
            this.tableLayoutPanel8.Controls.Add(this.btnCycle, 0, 4);
            this.tableLayoutPanel8.Controls.Add(this.label26, 0, 0);
            this.tableLayoutPanel8.Controls.Add(this.lblCycleCout, 1, 3);
            this.tableLayoutPanel8.Controls.Add(this.label12, 0, 1);
            this.tableLayoutPanel8.Controls.Add(this.label17, 0, 2);
            this.tableLayoutPanel8.Controls.Add(this.lblCycleEndTime, 1, 2);
            this.tableLayoutPanel8.Controls.Add(this.label7, 0, 3);
            this.tableLayoutPanel8.Controls.Add(this.lblCycleStartTime, 1, 1);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel8.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 5;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this.tableLayoutPanel8.Size = new System.Drawing.Size(320, 129);
            this.tableLayoutPanel8.TabIndex = 313;
            // 
            // btnFinish
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.btnFinish, 2);
            this.btnFinish.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFinish.Font = new System.Drawing.Font("Calibri", 9F);
            this.btnFinish.Location = new System.Drawing.Point(162, 95);
            this.btnFinish.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnFinish.Name = "btnFinish";
            this.btnFinish.Size = new System.Drawing.Size(156, 31);
            this.btnFinish.TabIndex = 309;
            this.btnFinish.Text = "Finish";
            this.btnFinish.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btnFinish.UseVisualStyleBackColor = true;
            this.btnFinish.Click += new System.EventHandler(this.btnFinish_Click);
            // 
            // btnCycle
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.btnCycle, 2);
            this.btnCycle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCycle.Font = new System.Drawing.Font("Calibri", 9F);
            this.btnCycle.Location = new System.Drawing.Point(2, 95);
            this.btnCycle.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnCycle.Name = "btnCycle";
            this.btnCycle.Size = new System.Drawing.Size(156, 31);
            this.btnCycle.TabIndex = 310;
            this.btnCycle.Text = "Cycle Start";
            this.btnCycle.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btnCycle.UseVisualStyleBackColor = true;
            this.btnCycle.Click += new System.EventHandler(this.btnCycle_Click);
            // 
            // label26
            // 
            this.label26.BackColor = System.Drawing.Color.RoyalBlue;
            this.tableLayoutPanel8.SetColumnSpan(this.label26, 4);
            this.label26.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label26.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label26.ForeColor = System.Drawing.Color.Yellow;
            this.label26.Location = new System.Drawing.Point(1, 1);
            this.label26.Margin = new System.Windows.Forms.Padding(1);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(318, 21);
            this.label26.TabIndex = 320;
            this.label26.Text = "Cycle";
            this.label26.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCycleCout
            // 
            this.lblCycleCout.AutoSize = true;
            this.tableLayoutPanel8.SetColumnSpan(this.lblCycleCout, 3);
            this.lblCycleCout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCycleCout.Font = new System.Drawing.Font("Calibri", 9F);
            this.lblCycleCout.Location = new System.Drawing.Point(81, 70);
            this.lblCycleCout.Margin = new System.Windows.Forms.Padding(1);
            this.lblCycleCout.Name = "lblCycleCout";
            this.lblCycleCout.Size = new System.Drawing.Size(238, 21);
            this.lblCycleCout.TabIndex = 7;
            this.lblCycleCout.Text = "-";
            this.lblCycleCout.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label12.Font = new System.Drawing.Font("Calibri", 9F);
            this.label12.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label12.Location = new System.Drawing.Point(1, 24);
            this.label12.Margin = new System.Windows.Forms.Padding(1);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(78, 21);
            this.label12.TabIndex = 0;
            this.label12.Text = "Start :";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label17.Font = new System.Drawing.Font("Calibri", 9F);
            this.label17.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label17.Location = new System.Drawing.Point(1, 47);
            this.label17.Margin = new System.Windows.Forms.Padding(1);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(78, 21);
            this.label17.TabIndex = 0;
            this.label17.Text = "End :";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCycleEndTime
            // 
            this.lblCycleEndTime.AutoSize = true;
            this.tableLayoutPanel8.SetColumnSpan(this.lblCycleEndTime, 3);
            this.lblCycleEndTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCycleEndTime.Font = new System.Drawing.Font("Calibri", 9F);
            this.lblCycleEndTime.Location = new System.Drawing.Point(81, 47);
            this.lblCycleEndTime.Margin = new System.Windows.Forms.Padding(1);
            this.lblCycleEndTime.Name = "lblCycleEndTime";
            this.lblCycleEndTime.Size = new System.Drawing.Size(238, 21);
            this.lblCycleEndTime.TabIndex = 5;
            this.lblCycleEndTime.Text = "yyyy-MM-dd HH:mm:ss";
            this.lblCycleEndTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Font = new System.Drawing.Font("Calibri", 9F);
            this.label7.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label7.Location = new System.Drawing.Point(1, 70);
            this.label7.Margin = new System.Windows.Forms.Padding(1);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(78, 21);
            this.label7.TabIndex = 6;
            this.label7.Text = "Count :";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCycleStartTime
            // 
            this.lblCycleStartTime.AutoSize = true;
            this.tableLayoutPanel8.SetColumnSpan(this.lblCycleStartTime, 3);
            this.lblCycleStartTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCycleStartTime.Font = new System.Drawing.Font("Calibri", 9F);
            this.lblCycleStartTime.Location = new System.Drawing.Point(81, 24);
            this.lblCycleStartTime.Margin = new System.Windows.Forms.Padding(1);
            this.lblCycleStartTime.Name = "lblCycleStartTime";
            this.lblCycleStartTime.Size = new System.Drawing.Size(238, 21);
            this.lblCycleStartTime.TabIndex = 0;
            this.lblCycleStartTime.Text = "yyyy-MM-dd HH:mm:ss";
            this.lblCycleStartTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel7
            // 
            this.panel7.Controls.Add(this.panel3);
            this.panel7.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel7.Location = new System.Drawing.Point(650, 0);
            this.panel7.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(320, 143);
            this.panel7.TabIndex = 311;
            // 
            // frmCycle
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(970, 700);
            this.Controls.Add(this.panel7);
            this.Controls.Add(this.panelUnit);
            this.Controls.Add(this.panelLoadportGui);
            this.Font = new System.Drawing.Font("Calibri", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmCycle";
            this.StartPosition = System.Windows.Forms.FormStartPosition.WindowsDefaultBounds;
            this.Text = "Main";
            this.VisibleChanged += new System.EventHandler(this.frmMain_VisibleChanged);
            this.panelLoadportGui.ResumeLayout(false);
            this.tabCtrlStage.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.panelUnit.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.tableLayoutPanel8.ResumeLayout(false);
            this.tableLayoutPanel8.PerformLayout();
            this.panel7.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer tmrUpdateUI;
        private System.Windows.Forms.Panel panelLoadportGui;
        private System.Windows.Forms.TabControl tabCtrlStage;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private GUI.GUILoadport guiLoadport1;
        private GUI.GUILoadport guiLoadport2;
        private GUI.GUILoadport guiLoadport3;
        private GUI.GUILoadport guiLoadport4;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private GUI.GUILoadport guiLoadport5;
        private GUI.GUILoadport guiLoadport6;
        private GUI.GUILoadport guiLoadport7;
        private GUI.GUILoadport guiLoadport8;
        private _0.GUI_UserCtrl.GUIAlignerStatus guiAlignerBStatus;
        private _0.GUI_UserCtrl.GUIAlignerStatus guiAlignerAStatus;
        private _0.GUI_UserCtrl.GUIRobotStatus guiRobotStatus;
        private _0.GUI_UserCtrl.GUIEquipmentStatus guiEquipmentStatus;
        private System.Windows.Forms.Panel panelUnit;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        public System.Windows.Forms.Button btnFinish;
        public System.Windows.Forms.Button btnCycle;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.Label lblCycleCout;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label lblCycleEndTime;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblCycleStartTime;
        private System.Windows.Forms.Panel panel7;
    }
}