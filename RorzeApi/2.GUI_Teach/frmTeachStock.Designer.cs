namespace RorzeApi
{
    partial class frmTeachStock
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmTeachStock));
            this.btnTeach = new System.Windows.Forms.Button();
            this.tmrUI = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.gbxArea = new System.Windows.Forms.GroupBox();
            this.tlpSelectArea = new System.Windows.Forms.TableLayoutPanel();
            this.gbxTower = new System.Windows.Forms.GroupBox();
            this.tlpSelectTower = new System.Windows.Forms.TableLayoutPanel();
            this.gbxTRB = new System.Windows.Forms.GroupBox();
            this.tlpSelectRobot = new System.Windows.Forms.TableLayoutPanel();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pcbTkey = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbInstruct = new System.Windows.Forms.GroupBox();
            this.rtbInstruct = new System.Windows.Forms.RichTextBox();
            this.tlpnlStep = new System.Windows.Forms.TableLayoutPanel();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.gbxArea.SuspendLayout();
            this.gbxTower.SuspendLayout();
            this.gbxTRB.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pcbTkey)).BeginInit();
            this.tableLayoutPanel7.SuspendLayout();
            this.gbInstruct.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnTeach
            // 
            resources.ApplyResources(this.btnTeach, "btnTeach");
            this.btnTeach.Image = global::RorzeApi.Properties.Resources._48_work;
            this.btnTeach.Name = "btnTeach";
            this.btnTeach.UseVisualStyleBackColor = true;
            this.btnTeach.Click += new System.EventHandler(this.btnTeach_Click);
            // 
            // tmrUI
            // 
            this.tmrUI.Interval = 400;
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
            this.tabPage1.Controls.Add(this.btnTeach);
            this.tabPage1.Controls.Add(this.gbxArea);
            this.tabPage1.Controls.Add(this.gbxTower);
            this.tabPage1.Controls.Add(this.gbxTRB);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // gbxArea
            // 
            this.gbxArea.Controls.Add(this.tlpSelectArea);
            resources.ApplyResources(this.gbxArea, "gbxArea");
            this.gbxArea.Name = "gbxArea";
            this.gbxArea.TabStop = false;
            // 
            // tlpSelectArea
            // 
            resources.ApplyResources(this.tlpSelectArea, "tlpSelectArea");
            this.tlpSelectArea.Name = "tlpSelectArea";
            // 
            // gbxTower
            // 
            this.gbxTower.Controls.Add(this.tlpSelectTower);
            resources.ApplyResources(this.gbxTower, "gbxTower");
            this.gbxTower.Name = "gbxTower";
            this.gbxTower.TabStop = false;
            // 
            // tlpSelectTower
            // 
            resources.ApplyResources(this.tlpSelectTower, "tlpSelectTower");
            this.tlpSelectTower.Name = "tlpSelectTower";
            // 
            // gbxTRB
            // 
            this.gbxTRB.Controls.Add(this.tlpSelectRobot);
            resources.ApplyResources(this.gbxTRB, "gbxTRB");
            this.gbxTRB.Name = "gbxTRB";
            this.gbxTRB.TabStop = false;
            // 
            // tlpSelectRobot
            // 
            resources.ApplyResources(this.tlpSelectRobot, "tlpSelectRobot");
            this.tlpSelectRobot.Name = "tlpSelectRobot";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.tableLayoutPanel1);
            this.tabPage2.Controls.Add(this.tableLayoutPanel7);
            this.tabPage2.Controls.Add(this.gbInstruct);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.pcbTkey, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // pcbTkey
            // 
            resources.ApplyResources(this.pcbTkey, "pcbTkey");
            this.pcbTkey.Image = global::RorzeApi.Properties.Resources.LightOff;
            this.pcbTkey.Name = "pcbTkey";
            this.pcbTkey.TabStop = false;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tableLayoutPanel7
            // 
            resources.ApplyResources(this.tableLayoutPanel7, "tableLayoutPanel7");
            this.tableLayoutPanel7.Controls.Add(this.btnNext, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.btnCancel, 0, 0);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            // 
            // btnNext
            // 
            resources.ApplyResources(this.btnNext, "btnNext");
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
            // frmTeachStock
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmTeachStock";
            this.VisibleChanged += new System.EventHandler(this.frmTeachRobot_VisibleChanged);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.gbxArea.ResumeLayout(false);
            this.gbxTower.ResumeLayout(false);
            this.gbxTRB.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pcbTkey)).EndInit();
            this.tableLayoutPanel7.ResumeLayout(false);
            this.gbInstruct.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnTeach;
        private System.Windows.Forms.Timer tmrUI;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox gbInstruct;
        private System.Windows.Forms.RichTextBox rtbInstruct;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.TableLayoutPanel tlpnlStep;
        private System.Windows.Forms.GroupBox gbxTRB;
        private System.Windows.Forms.TableLayoutPanel tlpSelectRobot;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.GroupBox gbxTower;
        private System.Windows.Forms.TableLayoutPanel tlpSelectTower;
        private System.Windows.Forms.GroupBox gbxArea;
        private System.Windows.Forms.TableLayoutPanel tlpSelectArea;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.PictureBox pcbTkey;
        private System.Windows.Forms.Label label1;
    }
}