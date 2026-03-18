namespace RorzeApi
{
    partial class frmTransferUndo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmTransferUndo));
            this.m_tmr = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblStockerSlot_Pos = new System.Windows.Forms.Label();
            this.lblStockerSlot_Front = new System.Windows.Forms.Label();
            this.lblStockerSlot_FrontID_value = new System.Windows.Forms.Label();
            this.lblStockerSlot_BackID_value = new System.Windows.Forms.Label();
            this.lblStockerSlot_Grade = new System.Windows.Forms.Label();
            this.lblStockerSlot_Grade_value = new System.Windows.Forms.Label();
            this.lblStockerSlot_LotID = new System.Windows.Forms.Label();
            this.lblStockerSlot_LotID_value = new System.Windows.Forms.Label();
            this.rtbMessage = new System.Windows.Forms.RichTextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.chkLoadportAFoupOn = new System.Windows.Forms.CheckBox();
            this.chkLoadportBFoupOn = new System.Windows.Forms.CheckBox();
            this.chkLoadportCFoupOn = new System.Windows.Forms.CheckBox();
            this.chkLoadportDFoupOn = new System.Windows.Forms.CheckBox();
            this.dgvUndoData = new System.Windows.Forms.DataGridView();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageFirst = new System.Windows.Forms.TabPage();
            this.tlpSimulateFoupOn = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.label25 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblSTG2_Rfid = new System.Windows.Forms.Label();
            this.lblSTG3_Rfid = new System.Windows.Forms.Label();
            this.lblSTG4_Rfid = new System.Windows.Forms.Label();
            this.lblSTG1_Rfid = new System.Windows.Forms.Label();
            this.label28 = new System.Windows.Forms.Label();
            this.lblSTG1_RfidRecord = new System.Windows.Forms.Label();
            this.lblSTG2_RfidRecord = new System.Windows.Forms.Label();
            this.lblSTG3_RfidRecord = new System.Windows.Forms.Label();
            this.lblSTG4_RfidRecord = new System.Windows.Forms.Label();
            this.btnSTG1read = new System.Windows.Forms.Button();
            this.btnSTG2read = new System.Windows.Forms.Button();
            this.btnSTG3read = new System.Windows.Forms.Button();
            this.btnSTG4read = new System.Windows.Forms.Button();
            this.tabPageSecond = new System.Windows.Forms.TabPage();
            this.pnlStocker = new System.Windows.Forms.Panel();
            this.guiTower4 = new RorzeApi.GUI.GUITower();
            this.guiTower3 = new RorzeApi.GUI.GUITower();
            this.guiTower2 = new RorzeApi.GUI.GUITower();
            this.guiTower1 = new RorzeApi.GUI.GUITower();
            this.tabCtrlStage = new System.Windows.Forms.TabControl();
            this.tabPageABCD = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.guiLoadport1 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport2 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport3 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport4 = new RorzeApi.GUI.GUILoadport();
            this.tabPageEFGH = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel6 = new System.Windows.Forms.FlowLayoutPanel();
            this.guiLoadport5 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport6 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport7 = new RorzeApi.GUI.GUILoadport();
            this.guiLoadport8 = new RorzeApi.GUI.GUILoadport();
            this.label11 = new System.Windows.Forms.Label();
            this.panel3.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUndoData)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPageFirst.SuspendLayout();
            this.tlpSimulateFoupOn.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tabPageSecond.SuspendLayout();
            this.pnlStocker.SuspendLayout();
            this.tabCtrlStage.SuspendLayout();
            this.tabPageABCD.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.tabPageEFGH.SuspendLayout();
            this.flowLayoutPanel6.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_tmr
            // 
            this.m_tmr.Tick += new System.EventHandler(this.m_tmr_Tick);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.DarkSlateGray;
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            resources.ApplyResources(this.button1, "button1");
            this.button1.ForeColor = System.Drawing.Color.Yellow;
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = false;
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.tableLayoutPanel1);
            this.panel3.Controls.Add(this.rtbMessage);
            this.panel3.Controls.Add(this.btnOK);
            this.panel3.Controls.Add(this.btnExit);
            resources.ApplyResources(this.panel3, "panel3");
            this.panel3.Name = "panel3";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.lblStockerSlot_Pos, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStockerSlot_Front, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStockerSlot_FrontID_value, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStockerSlot_BackID_value, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStockerSlot_Grade, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStockerSlot_Grade_value, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStockerSlot_LotID, 6, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStockerSlot_LotID_value, 7, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // lblStockerSlot_Pos
            // 
            resources.ApplyResources(this.lblStockerSlot_Pos, "lblStockerSlot_Pos");
            this.lblStockerSlot_Pos.Name = "lblStockerSlot_Pos";
            // 
            // lblStockerSlot_Front
            // 
            resources.ApplyResources(this.lblStockerSlot_Front, "lblStockerSlot_Front");
            this.lblStockerSlot_Front.Name = "lblStockerSlot_Front";
            // 
            // lblStockerSlot_FrontID_value
            // 
            resources.ApplyResources(this.lblStockerSlot_FrontID_value, "lblStockerSlot_FrontID_value");
            this.lblStockerSlot_FrontID_value.Name = "lblStockerSlot_FrontID_value";
            // 
            // lblStockerSlot_BackID_value
            // 
            resources.ApplyResources(this.lblStockerSlot_BackID_value, "lblStockerSlot_BackID_value");
            this.lblStockerSlot_BackID_value.Name = "lblStockerSlot_BackID_value";
            // 
            // lblStockerSlot_Grade
            // 
            resources.ApplyResources(this.lblStockerSlot_Grade, "lblStockerSlot_Grade");
            this.lblStockerSlot_Grade.Name = "lblStockerSlot_Grade";
            // 
            // lblStockerSlot_Grade_value
            // 
            resources.ApplyResources(this.lblStockerSlot_Grade_value, "lblStockerSlot_Grade_value");
            this.lblStockerSlot_Grade_value.Name = "lblStockerSlot_Grade_value";
            // 
            // lblStockerSlot_LotID
            // 
            resources.ApplyResources(this.lblStockerSlot_LotID, "lblStockerSlot_LotID");
            this.lblStockerSlot_LotID.Name = "lblStockerSlot_LotID";
            // 
            // lblStockerSlot_LotID_value
            // 
            resources.ApplyResources(this.lblStockerSlot_LotID_value, "lblStockerSlot_LotID_value");
            this.lblStockerSlot_LotID_value.Name = "lblStockerSlot_LotID_value";
            // 
            // rtbMessage
            // 
            resources.ApplyResources(this.rtbMessage, "rtbMessage");
            this.rtbMessage.Name = "rtbMessage";
            this.rtbMessage.ReadOnly = true;
            this.rtbMessage.TabStop = false;
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Image = global::RorzeApi.Properties.Resources._32_next;
            this.btnOK.Name = "btnOK";
            this.btnOK.Tag = "0";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnExit
            // 
            resources.ApplyResources(this.btnExit, "btnExit");
            this.btnExit.Image = global::RorzeApi.Properties.Resources._32_cancel;
            this.btnExit.Name = "btnExit";
            this.btnExit.Tag = "0";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // chkLoadportAFoupOn
            // 
            resources.ApplyResources(this.chkLoadportAFoupOn, "chkLoadportAFoupOn");
            this.chkLoadportAFoupOn.Name = "chkLoadportAFoupOn";
            this.chkLoadportAFoupOn.UseVisualStyleBackColor = true;
            this.chkLoadportAFoupOn.Click += new System.EventHandler(this.chkLoadportAFoupOn_Click);
            // 
            // chkLoadportBFoupOn
            // 
            resources.ApplyResources(this.chkLoadportBFoupOn, "chkLoadportBFoupOn");
            this.chkLoadportBFoupOn.Name = "chkLoadportBFoupOn";
            this.chkLoadportBFoupOn.UseVisualStyleBackColor = true;
            this.chkLoadportBFoupOn.Click += new System.EventHandler(this.chkLoadportBFoupOn_Click);
            // 
            // chkLoadportCFoupOn
            // 
            resources.ApplyResources(this.chkLoadportCFoupOn, "chkLoadportCFoupOn");
            this.chkLoadportCFoupOn.Name = "chkLoadportCFoupOn";
            this.chkLoadportCFoupOn.UseVisualStyleBackColor = true;
            this.chkLoadportCFoupOn.Click += new System.EventHandler(this.chkLoadportCFoupOn_Click);
            // 
            // chkLoadportDFoupOn
            // 
            resources.ApplyResources(this.chkLoadportDFoupOn, "chkLoadportDFoupOn");
            this.chkLoadportDFoupOn.Name = "chkLoadportDFoupOn";
            this.chkLoadportDFoupOn.UseVisualStyleBackColor = true;
            this.chkLoadportDFoupOn.Click += new System.EventHandler(this.chkLoadportDFoupOn_Click);
            // 
            // dgvUndoData
            // 
            this.dgvUndoData.AllowUserToAddRows = false;
            this.dgvUndoData.AllowUserToDeleteRows = false;
            this.dgvUndoData.AllowUserToResizeColumns = false;
            this.dgvUndoData.AllowUserToResizeRows = false;
            this.dgvUndoData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this.dgvUndoData, "dgvUndoData");
            this.dgvUndoData.Name = "dgvUndoData";
            this.dgvUndoData.ReadOnly = true;
            this.dgvUndoData.RowTemplate.Height = 24;
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabPageFirst);
            this.tabControl1.Controls.Add(this.tabPageSecond);
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPageFirst
            // 
            this.tabPageFirst.Controls.Add(this.tlpSimulateFoupOn);
            this.tabPageFirst.Controls.Add(this.panel1);
            this.tabPageFirst.Controls.Add(this.dgvUndoData);
            this.tabPageFirst.Controls.Add(this.button1);
            resources.ApplyResources(this.tabPageFirst, "tabPageFirst");
            this.tabPageFirst.Name = "tabPageFirst";
            this.tabPageFirst.UseVisualStyleBackColor = true;
            // 
            // tlpSimulateFoupOn
            // 
            resources.ApplyResources(this.tlpSimulateFoupOn, "tlpSimulateFoupOn");
            this.tlpSimulateFoupOn.Controls.Add(this.chkLoadportDFoupOn, 3, 0);
            this.tlpSimulateFoupOn.Controls.Add(this.chkLoadportCFoupOn, 2, 0);
            this.tlpSimulateFoupOn.Controls.Add(this.chkLoadportBFoupOn, 1, 0);
            this.tlpSimulateFoupOn.Controls.Add(this.chkLoadportAFoupOn, 0, 0);
            this.tlpSimulateFoupOn.Name = "tlpSimulateFoupOn";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.tableLayoutPanel4);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.label25, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.label4, 3, 0);
            this.tableLayoutPanel4.Controls.Add(this.label3, 6, 0);
            this.tableLayoutPanel4.Controls.Add(this.label1, 9, 0);
            this.tableLayoutPanel4.Controls.Add(this.label7, 3, 1);
            this.tableLayoutPanel4.Controls.Add(this.label6, 6, 1);
            this.tableLayoutPanel4.Controls.Add(this.label5, 9, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblSTG2_Rfid, 4, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblSTG3_Rfid, 7, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblSTG4_Rfid, 10, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblSTG1_Rfid, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.label28, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblSTG1_RfidRecord, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblSTG2_RfidRecord, 4, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblSTG3_RfidRecord, 7, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblSTG4_RfidRecord, 10, 0);
            this.tableLayoutPanel4.Controls.Add(this.btnSTG1read, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.btnSTG2read, 4, 2);
            this.tableLayoutPanel4.Controls.Add(this.btnSTG3read, 7, 2);
            this.tableLayoutPanel4.Controls.Add(this.btnSTG4read, 10, 2);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // label25
            // 
            resources.ApplyResources(this.label25, "label25");
            this.label25.BackColor = System.Drawing.SystemColors.Control;
            this.label25.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label25.Name = "label25";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.BackColor = System.Drawing.SystemColors.Control;
            this.label4.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label4.Name = "label4";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.BackColor = System.Drawing.SystemColors.Control;
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Name = "label3";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.BackColor = System.Drawing.SystemColors.Control;
            this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label1.Name = "label1";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.BackColor = System.Drawing.SystemColors.Control;
            this.label7.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label7.Name = "label7";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.BackColor = System.Drawing.SystemColors.Control;
            this.label6.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label6.Name = "label6";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.BackColor = System.Drawing.SystemColors.Control;
            this.label5.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label5.Name = "label5";
            // 
            // lblSTG2_Rfid
            // 
            this.lblSTG2_Rfid.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.lblSTG2_Rfid, "lblSTG2_Rfid");
            this.lblSTG2_Rfid.ForeColor = System.Drawing.Color.Crimson;
            this.lblSTG2_Rfid.Name = "lblSTG2_Rfid";
            // 
            // lblSTG3_Rfid
            // 
            this.lblSTG3_Rfid.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.lblSTG3_Rfid, "lblSTG3_Rfid");
            this.lblSTG3_Rfid.ForeColor = System.Drawing.Color.Crimson;
            this.lblSTG3_Rfid.Name = "lblSTG3_Rfid";
            // 
            // lblSTG4_Rfid
            // 
            this.lblSTG4_Rfid.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.lblSTG4_Rfid, "lblSTG4_Rfid");
            this.lblSTG4_Rfid.ForeColor = System.Drawing.Color.Crimson;
            this.lblSTG4_Rfid.Name = "lblSTG4_Rfid";
            // 
            // lblSTG1_Rfid
            // 
            this.lblSTG1_Rfid.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.lblSTG1_Rfid, "lblSTG1_Rfid");
            this.lblSTG1_Rfid.ForeColor = System.Drawing.Color.Crimson;
            this.lblSTG1_Rfid.Name = "lblSTG1_Rfid";
            // 
            // label28
            // 
            resources.ApplyResources(this.label28, "label28");
            this.label28.BackColor = System.Drawing.SystemColors.Control;
            this.label28.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label28.Name = "label28";
            // 
            // lblSTG1_RfidRecord
            // 
            this.lblSTG1_RfidRecord.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.lblSTG1_RfidRecord, "lblSTG1_RfidRecord");
            this.lblSTG1_RfidRecord.Name = "lblSTG1_RfidRecord";
            // 
            // lblSTG2_RfidRecord
            // 
            this.lblSTG2_RfidRecord.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.lblSTG2_RfidRecord, "lblSTG2_RfidRecord");
            this.lblSTG2_RfidRecord.Name = "lblSTG2_RfidRecord";
            // 
            // lblSTG3_RfidRecord
            // 
            this.lblSTG3_RfidRecord.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.lblSTG3_RfidRecord, "lblSTG3_RfidRecord");
            this.lblSTG3_RfidRecord.Name = "lblSTG3_RfidRecord";
            // 
            // lblSTG4_RfidRecord
            // 
            this.lblSTG4_RfidRecord.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.lblSTG4_RfidRecord, "lblSTG4_RfidRecord");
            this.lblSTG4_RfidRecord.Name = "lblSTG4_RfidRecord";
            // 
            // btnSTG1read
            // 
            resources.ApplyResources(this.btnSTG1read, "btnSTG1read");
            this.btnSTG1read.Name = "btnSTG1read";
            this.btnSTG1read.UseVisualStyleBackColor = true;
            this.btnSTG1read.Click += new System.EventHandler(this.btnSTG1read_Click);
            // 
            // btnSTG2read
            // 
            resources.ApplyResources(this.btnSTG2read, "btnSTG2read");
            this.btnSTG2read.Name = "btnSTG2read";
            this.btnSTG2read.UseVisualStyleBackColor = true;
            this.btnSTG2read.Click += new System.EventHandler(this.btnSTG2read_Click);
            // 
            // btnSTG3read
            // 
            resources.ApplyResources(this.btnSTG3read, "btnSTG3read");
            this.btnSTG3read.Name = "btnSTG3read";
            this.btnSTG3read.UseVisualStyleBackColor = true;
            this.btnSTG3read.Click += new System.EventHandler(this.btnSTG3read_Click);
            // 
            // btnSTG4read
            // 
            resources.ApplyResources(this.btnSTG4read, "btnSTG4read");
            this.btnSTG4read.Name = "btnSTG4read";
            this.btnSTG4read.UseVisualStyleBackColor = true;
            this.btnSTG4read.Click += new System.EventHandler(this.btnSTG4read_Click);
            // 
            // tabPageSecond
            // 
            this.tabPageSecond.Controls.Add(this.pnlStocker);
            this.tabPageSecond.Controls.Add(this.tabCtrlStage);
            resources.ApplyResources(this.tabPageSecond, "tabPageSecond");
            this.tabPageSecond.Name = "tabPageSecond";
            this.tabPageSecond.UseVisualStyleBackColor = true;
            // 
            // pnlStocker
            // 
            resources.ApplyResources(this.pnlStocker, "pnlStocker");
            this.pnlStocker.Controls.Add(this.guiTower4);
            this.pnlStocker.Controls.Add(this.guiTower3);
            this.pnlStocker.Controls.Add(this.guiTower2);
            this.pnlStocker.Controls.Add(this.guiTower1);
            this.pnlStocker.Name = "pnlStocker";
            // 
            // guiTower4
            // 
            this.guiTower4.BodyNo = 4;
            resources.ApplyResources(this.guiTower4, "guiTower4");
            this.guiTower4.Name = "guiTower4";
            this.guiTower4.Simulate = false;
            // 
            // guiTower3
            // 
            this.guiTower3.BodyNo = 3;
            resources.ApplyResources(this.guiTower3, "guiTower3");
            this.guiTower3.Name = "guiTower3";
            this.guiTower3.Simulate = false;
            // 
            // guiTower2
            // 
            this.guiTower2.BodyNo = 2;
            resources.ApplyResources(this.guiTower2, "guiTower2");
            this.guiTower2.Name = "guiTower2";
            this.guiTower2.Simulate = false;
            // 
            // guiTower1
            // 
            this.guiTower1.BodyNo = 1;
            resources.ApplyResources(this.guiTower1, "guiTower1");
            this.guiTower1.Name = "guiTower1";
            this.guiTower1.Simulate = false;
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
            this.tabPageABCD.Controls.Add(this.flowLayoutPanel4);
            resources.ApplyResources(this.tabPageABCD, "tabPageABCD");
            this.tabPageABCD.Name = "tabPageABCD";
            this.tabPageABCD.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.guiLoadport1);
            this.flowLayoutPanel4.Controls.Add(this.guiLoadport2);
            this.flowLayoutPanel4.Controls.Add(this.guiLoadport3);
            this.flowLayoutPanel4.Controls.Add(this.guiLoadport4);
            resources.ApplyResources(this.flowLayoutPanel4, "flowLayoutPanel4");
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            // 
            // guiLoadport1
            // 
            this.guiLoadport1.BodyNo = 1;
            this.guiLoadport1.Disable_ClmpLock = true;
            this.guiLoadport1.Disable_DockBtn = true;
            this.guiLoadport1.Disable_E84 = true;
            this.guiLoadport1.Disable_OCR = true;
            this.guiLoadport1.Disable_ProcessBtn = true;
            this.guiLoadport1.Disable_Recipe = true;
            this.guiLoadport1.Disable_RSV = true;
            this.guiLoadport1.Disable_SelectWafer = true;
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
            this.guiLoadport2.BodyNo = 2;
            this.guiLoadport2.Disable_ClmpLock = true;
            this.guiLoadport2.Disable_DockBtn = true;
            this.guiLoadport2.Disable_E84 = true;
            this.guiLoadport2.Disable_OCR = true;
            this.guiLoadport2.Disable_ProcessBtn = true;
            this.guiLoadport2.Disable_Recipe = true;
            this.guiLoadport2.Disable_RSV = true;
            this.guiLoadport2.Disable_SelectWafer = true;
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
            this.guiLoadport3.BodyNo = 3;
            this.guiLoadport3.Disable_ClmpLock = true;
            this.guiLoadport3.Disable_DockBtn = true;
            this.guiLoadport3.Disable_E84 = true;
            this.guiLoadport3.Disable_OCR = true;
            this.guiLoadport3.Disable_ProcessBtn = true;
            this.guiLoadport3.Disable_Recipe = true;
            this.guiLoadport3.Disable_RSV = true;
            this.guiLoadport3.Disable_SelectWafer = true;
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
            this.guiLoadport4.BodyNo = 4;
            this.guiLoadport4.Disable_ClmpLock = true;
            this.guiLoadport4.Disable_DockBtn = true;
            this.guiLoadport4.Disable_E84 = true;
            this.guiLoadport4.Disable_OCR = true;
            this.guiLoadport4.Disable_ProcessBtn = true;
            this.guiLoadport4.Disable_Recipe = true;
            this.guiLoadport4.Disable_RSV = true;
            this.guiLoadport4.Disable_SelectWafer = true;
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
            this.tabPageEFGH.Controls.Add(this.flowLayoutPanel6);
            resources.ApplyResources(this.tabPageEFGH, "tabPageEFGH");
            this.tabPageEFGH.Name = "tabPageEFGH";
            this.tabPageEFGH.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel6
            // 
            this.flowLayoutPanel6.Controls.Add(this.guiLoadport5);
            this.flowLayoutPanel6.Controls.Add(this.guiLoadport6);
            this.flowLayoutPanel6.Controls.Add(this.guiLoadport7);
            this.flowLayoutPanel6.Controls.Add(this.guiLoadport8);
            resources.ApplyResources(this.flowLayoutPanel6, "flowLayoutPanel6");
            this.flowLayoutPanel6.Name = "flowLayoutPanel6";
            // 
            // guiLoadport5
            // 
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
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.LightBlue;
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // frmTransferUndo
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ControlBox = false;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.panel3);
            this.Name = "frmTransferUndo";
            this.Load += new System.EventHandler(this.frmTransferUndo_Load);
            this.VisibleChanged += new System.EventHandler(this.frmTransferUndo_VisibleChanged);
            this.panel3.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUndoData)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPageFirst.ResumeLayout(false);
            this.tlpSimulateFoupOn.ResumeLayout(false);
            this.tlpSimulateFoupOn.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.tabPageSecond.ResumeLayout(false);
            this.pnlStocker.ResumeLayout(false);
            this.tabCtrlStage.ResumeLayout(false);
            this.tabPageABCD.ResumeLayout(false);
            this.flowLayoutPanel4.ResumeLayout(false);
            this.tabPageEFGH.ResumeLayout(false);
            this.flowLayoutPanel6.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer m_tmr;
        public System.Windows.Forms.Button button1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.RichTextBox rtbMessage;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.DataGridView dgvUndoData;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageSecond;
        private System.Windows.Forms.TabPage tabPageFirst;
        private System.Windows.Forms.TabControl tabCtrlStage;
        private System.Windows.Forms.TabPage tabPageABCD;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private GUI.GUILoadport guiLoadport1;
        private GUI.GUILoadport guiLoadport2;
        private GUI.GUILoadport guiLoadport3;
        private GUI.GUILoadport guiLoadport4;
        private System.Windows.Forms.TabPage tabPageEFGH;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel6;
        private GUI.GUILoadport guiLoadport5;
        private GUI.GUILoadport guiLoadport6;
        private GUI.GUILoadport guiLoadport7;
        private GUI.GUILoadport guiLoadport8;
        private System.Windows.Forms.CheckBox chkLoadportBFoupOn;
        private System.Windows.Forms.CheckBox chkLoadportAFoupOn;
        private System.Windows.Forms.CheckBox chkLoadportCFoupOn;
        private System.Windows.Forms.CheckBox chkLoadportDFoupOn;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblSTG2_Rfid;
        private System.Windows.Forms.Label lblSTG3_Rfid;
        private System.Windows.Forms.Label lblSTG4_Rfid;
        private System.Windows.Forms.Label lblSTG1_Rfid;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.Label lblSTG1_RfidRecord;
        private System.Windows.Forms.Label lblSTG2_RfidRecord;
        private System.Windows.Forms.Label lblSTG3_RfidRecord;
        private System.Windows.Forms.Label lblSTG4_RfidRecord;
        private System.Windows.Forms.Panel pnlStocker;
        private GUI.GUITower guiTower4;
        private GUI.GUITower guiTower3;
        private GUI.GUITower guiTower2;
        private GUI.GUITower guiTower1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblStockerSlot_Pos;
        private System.Windows.Forms.Label lblStockerSlot_Front;
        private System.Windows.Forms.Label lblStockerSlot_FrontID_value;
        private System.Windows.Forms.Label lblStockerSlot_BackID_value;
        private System.Windows.Forms.Label lblStockerSlot_Grade;
        private System.Windows.Forms.Label lblStockerSlot_Grade_value;
        private System.Windows.Forms.Label lblStockerSlot_LotID;
        private System.Windows.Forms.Label lblStockerSlot_LotID_value;
        private System.Windows.Forms.TableLayoutPanel tlpSimulateFoupOn;
        private System.Windows.Forms.Button btnSTG1read;
        private System.Windows.Forms.Button btnSTG2read;
        private System.Windows.Forms.Button btnSTG3read;
        private System.Windows.Forms.Button btnSTG4read;
    }
}