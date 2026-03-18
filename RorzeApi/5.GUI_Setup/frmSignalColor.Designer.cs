namespace RorzeApi
{
    partial class frmSignalColor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSignalColor));
            this.btnSave = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.OfflineP = new System.Windows.Forms.Panel();
            this.label11 = new System.Windows.Forms.Label();
            this.btnLight_Offline = new System.Windows.Forms.Button();
            this.pnlOnlineRemote = new System.Windows.Forms.Panel();
            this.label10 = new System.Windows.Forms.Label();
            this.btnLight_OnlineRemote = new System.Windows.Forms.Button();
            this.pnlOnlineLocal = new System.Windows.Forms.Panel();
            this.label9 = new System.Windows.Forms.Label();
            this.btnLight_OnlineLocal = new System.Windows.Forms.Button();
            this.pnlProcessing = new System.Windows.Forms.Panel();
            this.label8 = new System.Windows.Forms.Label();
            this.btnLight_Process = new System.Windows.Forms.Button();
            this.pnlIdle = new System.Windows.Forms.Panel();
            this.label7 = new System.Windows.Forms.Label();
            this.btnLight_Idle = new System.Windows.Forms.Button();
            this.pnlOperator = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.btnLight_Operation = new System.Windows.Forms.Button();
            this.pnlLURequest = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.btnLight_LUReq = new System.Windows.Forms.Button();
            this.pnlMaintenance = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.btnLight_Maint = new System.Windows.Forms.Button();
            this.pnlErrorOccurring = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.btnLight_Err = new System.Windows.Forms.Button();
            this.btnTitle = new System.Windows.Forms.Button();
            this.btnSelectColor = new System.Windows.Forms.Button();
            this.btnColor = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.pnlRed = new System.Windows.Forms.Panel();
            this.pnlRedBlinking = new System.Windows.Forms.Panel();
            this.pnlYellow = new System.Windows.Forms.Panel();
            this.pnlYellowBlinking = new System.Windows.Forms.Panel();
            this.pnlGreen = new System.Windows.Forms.Panel();
            this.pnlGreenBlinking = new System.Windows.Forms.Panel();
            this.pnlBlue = new System.Windows.Forms.Panel();
            this.pnlBlueBlinking = new System.Windows.Forms.Panel();
            this.pnlNoneColor = new System.Windows.Forms.Panel();
            this.rtbInstruct = new System.Windows.Forms.RichTextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.panel2 = new System.Windows.Forms.Panel();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.OfflineP.SuspendLayout();
            this.pnlOnlineRemote.SuspendLayout();
            this.pnlOnlineLocal.SuspendLayout();
            this.pnlProcessing.SuspendLayout();
            this.pnlIdle.SuspendLayout();
            this.pnlOperator.SuspendLayout();
            this.pnlLURequest.SuspendLayout();
            this.pnlMaintenance.SuspendLayout();
            this.pnlErrorOccurring.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.btnSave, "btnSave");
            this.btnSave.Image = global::RorzeApi.Properties.Resources._32_save;
            this.btnSave.Name = "btnSave";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Controls.Add(this.rtbInstruct);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.OfflineP, 0, 9);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_Offline, 1, 9);
            this.tableLayoutPanel1.Controls.Add(this.pnlOnlineRemote, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_OnlineRemote, 1, 8);
            this.tableLayoutPanel1.Controls.Add(this.pnlOnlineLocal, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_OnlineLocal, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.pnlProcessing, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_Process, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.pnlIdle, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_Idle, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.pnlOperator, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_Operation, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.pnlLURequest, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_LUReq, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.pnlMaintenance, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_Maint, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.pnlErrorOccurring, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnLight_Err, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnTitle, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnSelectColor, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnColor, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 2, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // OfflineP
            // 
            this.OfflineP.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.OfflineP.Controls.Add(this.label11);
            resources.ApplyResources(this.OfflineP, "OfflineP");
            this.OfflineP.Name = "OfflineP";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // btnLight_Offline
            // 
            this.btnLight_Offline.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_Offline, "btnLight_Offline");
            this.btnLight_Offline.ForeColor = System.Drawing.Color.Black;
            this.btnLight_Offline.Name = "btnLight_Offline";
            this.btnLight_Offline.UseVisualStyleBackColor = false;
            this.btnLight_Offline.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // pnlOnlineRemote
            // 
            this.pnlOnlineRemote.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlOnlineRemote.Controls.Add(this.label10);
            resources.ApplyResources(this.pnlOnlineRemote, "pnlOnlineRemote");
            this.pnlOnlineRemote.Name = "pnlOnlineRemote";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // btnLight_OnlineRemote
            // 
            this.btnLight_OnlineRemote.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_OnlineRemote, "btnLight_OnlineRemote");
            this.btnLight_OnlineRemote.ForeColor = System.Drawing.Color.Black;
            this.btnLight_OnlineRemote.Name = "btnLight_OnlineRemote";
            this.btnLight_OnlineRemote.UseVisualStyleBackColor = false;
            this.btnLight_OnlineRemote.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // pnlOnlineLocal
            // 
            this.pnlOnlineLocal.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlOnlineLocal.Controls.Add(this.label9);
            resources.ApplyResources(this.pnlOnlineLocal, "pnlOnlineLocal");
            this.pnlOnlineLocal.Name = "pnlOnlineLocal";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // btnLight_OnlineLocal
            // 
            this.btnLight_OnlineLocal.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_OnlineLocal, "btnLight_OnlineLocal");
            this.btnLight_OnlineLocal.ForeColor = System.Drawing.Color.Black;
            this.btnLight_OnlineLocal.Name = "btnLight_OnlineLocal";
            this.btnLight_OnlineLocal.UseVisualStyleBackColor = false;
            this.btnLight_OnlineLocal.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // pnlProcessing
            // 
            this.pnlProcessing.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlProcessing.Controls.Add(this.label8);
            resources.ApplyResources(this.pnlProcessing, "pnlProcessing");
            this.pnlProcessing.Name = "pnlProcessing";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // btnLight_Process
            // 
            this.btnLight_Process.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_Process, "btnLight_Process");
            this.btnLight_Process.ForeColor = System.Drawing.Color.Black;
            this.btnLight_Process.Name = "btnLight_Process";
            this.btnLight_Process.UseVisualStyleBackColor = false;
            this.btnLight_Process.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // pnlIdle
            // 
            this.pnlIdle.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlIdle.Controls.Add(this.label7);
            resources.ApplyResources(this.pnlIdle, "pnlIdle");
            this.pnlIdle.Name = "pnlIdle";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // btnLight_Idle
            // 
            this.btnLight_Idle.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_Idle, "btnLight_Idle");
            this.btnLight_Idle.ForeColor = System.Drawing.Color.Black;
            this.btnLight_Idle.Name = "btnLight_Idle";
            this.btnLight_Idle.UseVisualStyleBackColor = false;
            this.btnLight_Idle.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // pnlOperator
            // 
            this.pnlOperator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlOperator.Controls.Add(this.label5);
            resources.ApplyResources(this.pnlOperator, "pnlOperator");
            this.pnlOperator.Name = "pnlOperator";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // btnLight_Operation
            // 
            this.btnLight_Operation.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_Operation, "btnLight_Operation");
            this.btnLight_Operation.ForeColor = System.Drawing.Color.Black;
            this.btnLight_Operation.Name = "btnLight_Operation";
            this.btnLight_Operation.UseVisualStyleBackColor = false;
            this.btnLight_Operation.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // pnlLURequest
            // 
            this.pnlLURequest.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlLURequest.Controls.Add(this.label3);
            resources.ApplyResources(this.pnlLURequest, "pnlLURequest");
            this.pnlLURequest.Name = "pnlLURequest";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // btnLight_LUReq
            // 
            this.btnLight_LUReq.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_LUReq, "btnLight_LUReq");
            this.btnLight_LUReq.ForeColor = System.Drawing.Color.Black;
            this.btnLight_LUReq.Name = "btnLight_LUReq";
            this.btnLight_LUReq.UseVisualStyleBackColor = false;
            this.btnLight_LUReq.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // pnlMaintenance
            // 
            this.pnlMaintenance.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlMaintenance.Controls.Add(this.label2);
            resources.ApplyResources(this.pnlMaintenance, "pnlMaintenance");
            this.pnlMaintenance.Name = "pnlMaintenance";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // btnLight_Maint
            // 
            this.btnLight_Maint.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_Maint, "btnLight_Maint");
            this.btnLight_Maint.ForeColor = System.Drawing.Color.Black;
            this.btnLight_Maint.Name = "btnLight_Maint";
            this.btnLight_Maint.UseVisualStyleBackColor = false;
            this.btnLight_Maint.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // pnlErrorOccurring
            // 
            this.pnlErrorOccurring.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlErrorOccurring.Controls.Add(this.label1);
            resources.ApplyResources(this.pnlErrorOccurring, "pnlErrorOccurring");
            this.pnlErrorOccurring.Name = "pnlErrorOccurring";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // btnLight_Err
            // 
            this.btnLight_Err.BackColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.btnLight_Err, "btnLight_Err");
            this.btnLight_Err.ForeColor = System.Drawing.Color.Black;
            this.btnLight_Err.Name = "btnLight_Err";
            this.btnLight_Err.UseVisualStyleBackColor = false;
            this.btnLight_Err.Click += new System.EventHandler(this.btnLight_Click);
            // 
            // btnTitle
            // 
            this.btnTitle.BackColor = System.Drawing.Color.DarkGreen;
            resources.ApplyResources(this.btnTitle, "btnTitle");
            this.btnTitle.ForeColor = System.Drawing.Color.Yellow;
            this.btnTitle.Name = "btnTitle";
            this.btnTitle.Tag = "4";
            this.btnTitle.UseVisualStyleBackColor = false;
            // 
            // btnSelectColor
            // 
            this.btnSelectColor.BackColor = System.Drawing.Color.DarkGreen;
            resources.ApplyResources(this.btnSelectColor, "btnSelectColor");
            this.btnSelectColor.ForeColor = System.Drawing.Color.Yellow;
            this.btnSelectColor.Name = "btnSelectColor";
            this.btnSelectColor.Tag = "4";
            this.btnSelectColor.UseVisualStyleBackColor = false;
            // 
            // btnColor
            // 
            this.btnColor.BackColor = System.Drawing.Color.DarkGreen;
            resources.ApplyResources(this.btnColor, "btnColor");
            this.btnColor.ForeColor = System.Drawing.Color.Yellow;
            this.btnColor.Name = "btnColor";
            this.btnColor.Tag = "4";
            this.btnColor.UseVisualStyleBackColor = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.pnlRed, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.pnlRedBlinking, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.pnlYellow, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.pnlYellowBlinking, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.pnlGreen, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.pnlGreenBlinking, 0, 5);
            this.tableLayoutPanel2.Controls.Add(this.pnlBlue, 0, 6);
            this.tableLayoutPanel2.Controls.Add(this.pnlBlueBlinking, 0, 7);
            this.tableLayoutPanel2.Controls.Add(this.pnlNoneColor, 0, 8);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel1.SetRowSpan(this.tableLayoutPanel2, 9);
            // 
            // pnlRed
            // 
            this.pnlRed.BackColor = System.Drawing.Color.Red;
            this.pnlRed.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlRed, "pnlRed");
            this.pnlRed.Name = "pnlRed";
            this.pnlRed.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // pnlRedBlinking
            // 
            this.pnlRedBlinking.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlRedBlinking, "pnlRedBlinking");
            this.pnlRedBlinking.Name = "pnlRedBlinking";
            this.pnlRedBlinking.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // pnlYellow
            // 
            this.pnlYellow.BackColor = System.Drawing.Color.Yellow;
            this.pnlYellow.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlYellow, "pnlYellow");
            this.pnlYellow.Name = "pnlYellow";
            this.pnlYellow.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // pnlYellowBlinking
            // 
            this.pnlYellowBlinking.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlYellowBlinking, "pnlYellowBlinking");
            this.pnlYellowBlinking.Name = "pnlYellowBlinking";
            this.pnlYellowBlinking.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // pnlGreen
            // 
            this.pnlGreen.BackColor = System.Drawing.Color.Green;
            this.pnlGreen.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlGreen, "pnlGreen");
            this.pnlGreen.Name = "pnlGreen";
            this.pnlGreen.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // pnlGreenBlinking
            // 
            this.pnlGreenBlinking.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlGreenBlinking, "pnlGreenBlinking");
            this.pnlGreenBlinking.Name = "pnlGreenBlinking";
            this.pnlGreenBlinking.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // pnlBlue
            // 
            this.pnlBlue.BackColor = System.Drawing.Color.Blue;
            this.pnlBlue.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlBlue, "pnlBlue");
            this.pnlBlue.Name = "pnlBlue";
            this.pnlBlue.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // pnlBlueBlinking
            // 
            this.pnlBlueBlinking.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlBlueBlinking, "pnlBlueBlinking");
            this.pnlBlueBlinking.Name = "pnlBlueBlinking";
            this.pnlBlueBlinking.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // pnlNoneColor
            // 
            this.pnlNoneColor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pnlNoneColor, "pnlNoneColor");
            this.pnlNoneColor.Name = "pnlNoneColor";
            this.pnlNoneColor.Click += new System.EventHandler(this.pnlSelectColor_Click);
            // 
            // rtbInstruct
            // 
            this.rtbInstruct.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.rtbInstruct, "rtbInstruct");
            this.rtbInstruct.Name = "rtbInstruct";
            this.rtbInstruct.ReadOnly = true;
            // 
            // timer1
            // 
            this.timer1.Interval = 250;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnSave);
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // frmSignalColor
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.panel2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmSignalColor";
            this.Load += new System.EventHandler(this.frmPermissionUser_Load);
            this.VisibleChanged += new System.EventHandler(this.frmPermissionUser_VisibleChanged);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.OfflineP.ResumeLayout(false);
            this.pnlOnlineRemote.ResumeLayout(false);
            this.pnlOnlineLocal.ResumeLayout(false);
            this.pnlProcessing.ResumeLayout(false);
            this.pnlIdle.ResumeLayout(false);
            this.pnlOperator.ResumeLayout(false);
            this.pnlLURequest.ResumeLayout(false);
            this.pnlMaintenance.ResumeLayout(false);
            this.pnlErrorOccurring.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnTitle;
        private System.Windows.Forms.Button btnColor;
        private System.Windows.Forms.Button btnSelectColor;
        private System.Windows.Forms.RichTextBox rtbInstruct;
        private System.Windows.Forms.Panel pnlMaintenance;
        private System.Windows.Forms.Button btnLight_Maint;
        private System.Windows.Forms.Panel pnlLURequest;
        private System.Windows.Forms.Button btnLight_LUReq;
        private System.Windows.Forms.Panel pnlOperator;
        private System.Windows.Forms.Button btnLight_Operation;
        private System.Windows.Forms.Panel pnlIdle;
        private System.Windows.Forms.Button btnLight_Idle;
        private System.Windows.Forms.Panel pnlProcessing;
        private System.Windows.Forms.Button btnLight_Process;
        private System.Windows.Forms.Panel pnlOnlineLocal;
        private System.Windows.Forms.Button btnLight_OnlineLocal;
        private System.Windows.Forms.Panel pnlOnlineRemote;
        private System.Windows.Forms.Button btnLight_OnlineRemote;
        private System.Windows.Forms.Panel OfflineP;
        private System.Windows.Forms.Button btnLight_Offline;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Panel pnlRed;
        private System.Windows.Forms.Panel pnlRedBlinking;
        private System.Windows.Forms.Panel pnlYellow;
        private System.Windows.Forms.Panel pnlYellowBlinking;
        private System.Windows.Forms.Panel pnlGreen;
        private System.Windows.Forms.Panel pnlGreenBlinking;
        private System.Windows.Forms.Panel pnlBlue;
        private System.Windows.Forms.Panel pnlBlueBlinking;
        private System.Windows.Forms.Panel pnlNoneColor;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Panel pnlErrorOccurring;
        private System.Windows.Forms.Button btnLight_Err;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label1;
    }
}