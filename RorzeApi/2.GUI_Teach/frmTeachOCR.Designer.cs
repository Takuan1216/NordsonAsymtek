namespace RorzeApi
{
    partial class frmTeachOCR
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmTeachOCR));
            this.tmrUI = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.tlpSelectOCR = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.tlpSelectAligner = new System.Windows.Forms.TableLayoutPanel();
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
            this.gpRecipeNumber = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.tbRecipeName = new System.Windows.Forms.TextBox();
            this.cbRecipeNumber = new System.Windows.Forms.ComboBox();
            this.btnTeach = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.gbStep = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.tabCtrlAngle = new System.Windows.Forms.TabControl();
            this.tabPageWafer = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.rbStep001 = new System.Windows.Forms.RadioButton();
            this.btnAlignerCW = new System.Windows.Forms.Button();
            this.btnAlignerCCW = new System.Windows.Forms.Button();
            this.rbStep90 = new System.Windows.Forms.RadioButton();
            this.rbStep50 = new System.Windows.Forms.RadioButton();
            this.rbStep30 = new System.Windows.Forms.RadioButton();
            this.rbStep10 = new System.Windows.Forms.RadioButton();
            this.rbStep05 = new System.Windows.Forms.RadioButton();
            this.rbStep01 = new System.Windows.Forms.RadioButton();
            this.rbStep005 = new System.Windows.Forms.RadioButton();
            this.tabPagePanel = new System.Windows.Forms.TabPage();
            this.guiNotchAngle1 = new RorzeApi.GUI.GUINotchAngle();
            this.AngleE = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.textWaferID = new System.Windows.Forms.TextBox();
            this.btnTop = new System.Windows.Forms.Button();
            this.btnRead = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.WaferIDLengthE = new System.Windows.Forms.TextBox();
            this.WaferNoFirstPositionE = new System.Windows.Forms.TextBox();
            this.LotIDLengthE = new System.Windows.Forms.TextBox();
            this.LotIDFirstPositionE = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.gbInstruct = new System.Windows.Forms.GroupBox();
            this.rtbInstruct = new System.Windows.Forms.RichTextBox();
            this.tlpnlStep = new System.Windows.Forms.TableLayoutPanel();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.tabCtrlStage.SuspendLayout();
            this.tabPageABCD.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tabPageEFGH.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.gpRecipeNumber.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.gbStep.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tabCtrlAngle.SuspendLayout();
            this.tabPageWafer.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.tabPagePanel.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.gbInstruct.SuspendLayout();
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
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox8);
            this.tabPage1.Controls.Add(this.groupBox7);
            this.tabPage1.Controls.Add(this.tabCtrlStage);
            this.tabPage1.Controls.Add(this.gpRecipeNumber);
            this.tabPage1.Controls.Add(this.btnTeach);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.tlpSelectOCR);
            resources.ApplyResources(this.groupBox8, "groupBox8");
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.TabStop = false;
            // 
            // tlpSelectOCR
            // 
            resources.ApplyResources(this.tlpSelectOCR, "tlpSelectOCR");
            this.tlpSelectOCR.Name = "tlpSelectOCR";
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.tlpSelectAligner);
            resources.ApplyResources(this.groupBox7, "groupBox7");
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.TabStop = false;
            // 
            // tlpSelectAligner
            // 
            resources.ApplyResources(this.tlpSelectAligner, "tlpSelectAligner");
            this.tlpSelectAligner.Name = "tlpSelectAligner";
            // 
            // tabCtrlStage
            // 
            this.tabCtrlStage.Controls.Add(this.tabPageABCD);
            this.tabCtrlStage.Controls.Add(this.tabPageEFGH);
            resources.ApplyResources(this.tabCtrlStage, "tabCtrlStage");
            this.tabCtrlStage.Multiline = true;
            this.tabCtrlStage.Name = "tabCtrlStage";
            this.tabCtrlStage.SelectedIndex = 0;
            // 
            // tabPageABCD
            // 
            this.tabPageABCD.Controls.Add(this.flowLayoutPanel1);
            resources.ApplyResources(this.tabPageABCD, "tabPageABCD");
            this.tabPageABCD.Name = "tabPageABCD";
            this.tabPageABCD.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport1);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport2);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport3);
            this.flowLayoutPanel1.Controls.Add(this.guiLoadport4);
            resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // guiLoadport1
            // 
            this.guiLoadport1._Type = RorzeApi.GUI.GUILoadport.enumType.Wafer;
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
            resources.ApplyResources(this.guiLoadport1, "guiLoadport1");
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
            this.guiLoadport2._Type = RorzeApi.GUI.GUILoadport.enumType.Wafer;
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
            resources.ApplyResources(this.guiLoadport2, "guiLoadport2");
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
            this.guiLoadport3._Type = RorzeApi.GUI.GUILoadport.enumType.Wafer;
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
            resources.ApplyResources(this.guiLoadport3, "guiLoadport3");
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
            this.guiLoadport4._Type = RorzeApi.GUI.GUILoadport.enumType.Wafer;
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
            resources.ApplyResources(this.guiLoadport4, "guiLoadport4");
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
            this.tabPageEFGH.Controls.Add(this.flowLayoutPanel2);
            resources.ApplyResources(this.tabPageEFGH, "tabPageEFGH");
            this.tabPageEFGH.Name = "tabPageEFGH";
            this.tabPageEFGH.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport5);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport6);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport7);
            this.flowLayoutPanel2.Controls.Add(this.guiLoadport8);
            resources.ApplyResources(this.flowLayoutPanel2, "flowLayoutPanel2");
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            // 
            // guiLoadport5
            // 
            this.guiLoadport5._Type = RorzeApi.GUI.GUILoadport.enumType.Wafer;
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
            resources.ApplyResources(this.guiLoadport5, "guiLoadport5");
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
            this.guiLoadport6._Type = RorzeApi.GUI.GUILoadport.enumType.Wafer;
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
            resources.ApplyResources(this.guiLoadport6, "guiLoadport6");
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
            this.guiLoadport7._Type = RorzeApi.GUI.GUILoadport.enumType.Wafer;
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
            resources.ApplyResources(this.guiLoadport7, "guiLoadport7");
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
            this.guiLoadport8._Type = RorzeApi.GUI.GUILoadport.enumType.Wafer;
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
            resources.ApplyResources(this.guiLoadport8, "guiLoadport8");
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
            // gpRecipeNumber
            // 
            this.gpRecipeNumber.Controls.Add(this.tableLayoutPanel4);
            resources.ApplyResources(this.gpRecipeNumber, "gpRecipeNumber");
            this.gpRecipeNumber.Name = "gpRecipeNumber";
            this.gpRecipeNumber.TabStop = false;
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.tbRecipeName, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.cbRecipeNumber, 0, 0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // tbRecipeName
            // 
            resources.ApplyResources(this.tbRecipeName, "tbRecipeName");
            this.tbRecipeName.Name = "tbRecipeName";
            // 
            // cbRecipeNumber
            // 
            resources.ApplyResources(this.cbRecipeNumber, "cbRecipeNumber");
            this.cbRecipeNumber.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRecipeNumber.FormattingEnabled = true;
            this.cbRecipeNumber.Name = "cbRecipeNumber";
            this.cbRecipeNumber.SelectedIndexChanged += new System.EventHandler(this.cbRecipeNumber_SelectedIndexChanged);
            this.cbRecipeNumber.SelectionChangeCommitted += new System.EventHandler(this.cbRecipeNumber_SelectionChangeCommitted);
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
            this.tabPage2.Controls.Add(this.tableLayoutPanel7);
            this.tabPage2.Controls.Add(this.groupBox6);
            this.tabPage2.Controls.Add(this.gbInstruct);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.Controls.Add(this.btnNext, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.btnCancel, 0, 0);
            resources.ApplyResources(this.tableLayoutPanel7, "tableLayoutPanel7");
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
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
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.Image = global::RorzeApi.Properties.Resources._32_cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.gbStep);
            this.groupBox6.Controls.Add(this.groupBox2);
            this.groupBox6.Controls.Add(this.groupBox5);
            this.groupBox6.Controls.Add(this.groupBox3);
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            // 
            // gbStep
            // 
            this.gbStep.Controls.Add(this.tableLayoutPanel2);
            resources.ApplyResources(this.gbStep, "gbStep");
            this.gbStep.Name = "gbStep";
            this.gbStep.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.label5, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tabCtrlAngle, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.AngleE, 1, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // tabCtrlAngle
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.tabCtrlAngle, 3);
            this.tabCtrlAngle.Controls.Add(this.tabPageWafer);
            this.tabCtrlAngle.Controls.Add(this.tabPagePanel);
            resources.ApplyResources(this.tabCtrlAngle, "tabCtrlAngle");
            this.tabCtrlAngle.Name = "tabCtrlAngle";
            this.tableLayoutPanel2.SetRowSpan(this.tabCtrlAngle, 4);
            this.tabCtrlAngle.SelectedIndex = 0;
            // 
            // tabPageWafer
            // 
            this.tabPageWafer.Controls.Add(this.tableLayoutPanel5);
            resources.ApplyResources(this.tabPageWafer, "tabPageWafer");
            this.tabPageWafer.Name = "tabPageWafer";
            this.tabPageWafer.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel5
            // 
            resources.ApplyResources(this.tableLayoutPanel5, "tableLayoutPanel5");
            this.tableLayoutPanel5.Controls.Add(this.rbStep001, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.btnAlignerCW, 2, 2);
            this.tableLayoutPanel5.Controls.Add(this.btnAlignerCCW, 0, 2);
            this.tableLayoutPanel5.Controls.Add(this.rbStep90, 3, 1);
            this.tableLayoutPanel5.Controls.Add(this.rbStep50, 2, 1);
            this.tableLayoutPanel5.Controls.Add(this.rbStep30, 1, 1);
            this.tableLayoutPanel5.Controls.Add(this.rbStep10, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.rbStep05, 3, 0);
            this.tableLayoutPanel5.Controls.Add(this.rbStep01, 2, 0);
            this.tableLayoutPanel5.Controls.Add(this.rbStep005, 1, 0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            // 
            // rbStep001
            // 
            resources.ApplyResources(this.rbStep001, "rbStep001");
            this.rbStep001.Name = "rbStep001";
            this.rbStep001.Tag = "0.1";
            this.rbStep001.UseVisualStyleBackColor = true;
            this.rbStep001.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // btnAlignerCW
            // 
            this.tableLayoutPanel5.SetColumnSpan(this.btnAlignerCW, 2);
            resources.ApplyResources(this.btnAlignerCW, "btnAlignerCW");
            this.btnAlignerCW.Image = global::RorzeApi.Properties.Resources.TeachCW;
            this.btnAlignerCW.Name = "btnAlignerCW";
            this.tableLayoutPanel5.SetRowSpan(this.btnAlignerCW, 2);
            this.btnAlignerCW.UseVisualStyleBackColor = true;
            this.btnAlignerCW.Click += new System.EventHandler(this.btnAlignerRotFW_Click);
            // 
            // btnAlignerCCW
            // 
            this.tableLayoutPanel5.SetColumnSpan(this.btnAlignerCCW, 2);
            resources.ApplyResources(this.btnAlignerCCW, "btnAlignerCCW");
            this.btnAlignerCCW.Image = global::RorzeApi.Properties.Resources.TeachCCW;
            this.btnAlignerCCW.Name = "btnAlignerCCW";
            this.tableLayoutPanel5.SetRowSpan(this.btnAlignerCCW, 2);
            this.btnAlignerCCW.UseVisualStyleBackColor = true;
            this.btnAlignerCCW.Click += new System.EventHandler(this.btnAlignerRotBW_Click);
            // 
            // rbStep90
            // 
            resources.ApplyResources(this.rbStep90, "rbStep90");
            this.rbStep90.Name = "rbStep90";
            this.rbStep90.Tag = "90";
            this.rbStep90.UseVisualStyleBackColor = true;
            this.rbStep90.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // rbStep50
            // 
            resources.ApplyResources(this.rbStep50, "rbStep50");
            this.rbStep50.Name = "rbStep50";
            this.rbStep50.Tag = "50";
            this.rbStep50.UseVisualStyleBackColor = true;
            this.rbStep50.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
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
            // rbStep10
            // 
            resources.ApplyResources(this.rbStep10, "rbStep10");
            this.rbStep10.Name = "rbStep10";
            this.rbStep10.Tag = "10";
            this.rbStep10.UseVisualStyleBackColor = true;
            this.rbStep10.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // rbStep05
            // 
            resources.ApplyResources(this.rbStep05, "rbStep05");
            this.rbStep05.Name = "rbStep05";
            this.rbStep05.Tag = "5";
            this.rbStep05.UseVisualStyleBackColor = true;
            this.rbStep05.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // rbStep01
            // 
            resources.ApplyResources(this.rbStep01, "rbStep01");
            this.rbStep01.Name = "rbStep01";
            this.rbStep01.Tag = "1";
            this.rbStep01.UseVisualStyleBackColor = true;
            this.rbStep01.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // rbStep005
            // 
            resources.ApplyResources(this.rbStep005, "rbStep005");
            this.rbStep005.Name = "rbStep005";
            this.rbStep005.Tag = "0.5";
            this.rbStep005.UseVisualStyleBackColor = true;
            this.rbStep005.CheckedChanged += new System.EventHandler(this.rbStep_CheckedChanged);
            // 
            // tabPagePanel
            // 
            this.tabPagePanel.Controls.Add(this.guiNotchAngle1);
            resources.ApplyResources(this.tabPagePanel, "tabPagePanel");
            this.tabPagePanel.Name = "tabPagePanel";
            this.tabPagePanel.UseVisualStyleBackColor = true;
            // 
            // guiNotchAngle1
            // 
            this.guiNotchAngle1._Angle = -1D;
            this.guiNotchAngle1._Type = RorzeApi.GUI.GUINotchAngle.enumType.Panel;
            resources.ApplyResources(this.guiNotchAngle1, "guiNotchAngle1");
            this.guiNotchAngle1.Name = "guiNotchAngle1";
            // 
            // AngleE
            // 
            resources.ApplyResources(this.AngleE, "AngleE");
            this.AngleE.Name = "AngleE";
            this.AngleE.ReadOnly = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnSave);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.btnSave, "btnSave");
            this.btnSave.Image = global::RorzeApi.Properties.Resources._48_save;
            this.btnSave.Name = "btnSave";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.tableLayoutPanel3);
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // tableLayoutPanel3
            // 
            resources.ApplyResources(this.tableLayoutPanel3, "tableLayoutPanel3");
            this.tableLayoutPanel3.Controls.Add(this.textWaferID, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.btnTop, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.btnRead, 1, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            // 
            // textWaferID
            // 
            resources.ApplyResources(this.textWaferID, "textWaferID");
            this.textWaferID.Name = "textWaferID";
            this.textWaferID.ReadOnly = true;
            // 
            // btnTop
            // 
            this.btnTop.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.btnTop, "btnTop");
            this.btnTop.Name = "btnTop";
            this.tableLayoutPanel3.SetRowSpan(this.btnTop, 2);
            this.btnTop.UseVisualStyleBackColor = false;
            this.btnTop.Click += new System.EventHandler(this.btnTop_Click);
            // 
            // btnRead
            // 
            this.btnRead.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.btnRead, "btnRead");
            this.btnRead.Name = "btnRead";
            this.btnRead.UseVisualStyleBackColor = false;
            this.btnRead.Click += new System.EventHandler(this.btnRead_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.tableLayoutPanel1);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.WaferIDLengthE, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.WaferNoFirstPositionE, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.LotIDLengthE, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.LotIDFirstPositionE, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // WaferIDLengthE
            // 
            resources.ApplyResources(this.WaferIDLengthE, "WaferIDLengthE");
            this.WaferIDLengthE.Name = "WaferIDLengthE";
            // 
            // WaferNoFirstPositionE
            // 
            resources.ApplyResources(this.WaferNoFirstPositionE, "WaferNoFirstPositionE");
            this.WaferNoFirstPositionE.Name = "WaferNoFirstPositionE";
            // 
            // LotIDLengthE
            // 
            resources.ApplyResources(this.LotIDLengthE, "LotIDLengthE");
            this.LotIDLengthE.Name = "LotIDLengthE";
            // 
            // LotIDFirstPositionE
            // 
            resources.ApplyResources(this.LotIDFirstPositionE, "LotIDFirstPositionE");
            this.LotIDFirstPositionE.Name = "LotIDFirstPositionE";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // gbInstruct
            // 
            this.gbInstruct.Controls.Add(this.rtbInstruct);
            this.gbInstruct.Controls.Add(this.tlpnlStep);
            resources.ApplyResources(this.gbInstruct, "gbInstruct");
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
            // frmTeachOCR
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmTeachOCR";
            this.VisibleChanged += new System.EventHandler(this.frmTeachOCR_VisibleChanged);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox8.ResumeLayout(false);
            this.groupBox7.ResumeLayout(false);
            this.tabCtrlStage.ResumeLayout(false);
            this.tabPageABCD.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tabPageEFGH.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.gpRecipeNumber.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.gbStep.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tabCtrlAngle.ResumeLayout(false);
            this.tabPageWafer.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.tabPagePanel.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.gbInstruct.ResumeLayout(false);
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
        private System.Windows.Forms.GroupBox gpRecipeNumber;
        private System.Windows.Forms.ComboBox cbRecipeNumber;
        private System.Windows.Forms.TextBox tbRecipeName;
        private System.Windows.Forms.TextBox AngleE;
        private System.Windows.Forms.GroupBox gbStep;
        private System.Windows.Forms.RadioButton rbStep30;
        private System.Windows.Forms.RadioButton rbStep10;
        private System.Windows.Forms.RadioButton rbStep01;
        private System.Windows.Forms.RadioButton rbStep05;
        private System.Windows.Forms.RadioButton rbStep001;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox WaferNoFirstPositionE;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox LotIDLengthE;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox LotIDFirstPositionE;
        private System.Windows.Forms.Button btnAlignerCW;
        private System.Windows.Forms.Button btnAlignerCCW;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRead;
        private System.Windows.Forms.TextBox textWaferID;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button btnTop;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox WaferIDLengthE;
        private System.Windows.Forms.TableLayoutPanel tlpnlStep;
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
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.TableLayoutPanel tlpSelectAligner;
        private System.Windows.Forms.GroupBox groupBox8;
        private System.Windows.Forms.TableLayoutPanel tlpSelectOCR;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TabControl tabCtrlAngle;
        private System.Windows.Forms.TabPage tabPageWafer;
        private System.Windows.Forms.TabPage tabPagePanel;
        private GUI.GUINotchAngle guiNotchAngle1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.RadioButton rbStep50;
        private System.Windows.Forms.RadioButton rbStep90;
        private System.Windows.Forms.RadioButton rbStep005;
    }
}