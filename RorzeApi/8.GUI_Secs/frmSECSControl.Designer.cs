namespace RorzeApi
{
    partial class frmSECSControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSECSControl));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnGoRmote = new System.Windows.Forms.Button();
            this.btnGoOffline = new System.Windows.Forms.Button();
            this.btnGoOnline = new System.Windows.Forms.Button();
            this.btnGoLocal = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnSECSOff = new System.Windows.Forms.Button();
            this.btnSECSOn = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 100);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1024, 100);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "SECS Control Status";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.btnGoRmote, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnGoOffline, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnGoOnline, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnGoLocal, 2, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 18);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1018, 79);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // btnGoRmote
            // 
            this.btnGoRmote.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoRmote.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGoRmote.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGoRmote.Location = new System.Drawing.Point(765, 3);
            this.btnGoRmote.Name = "btnGoRmote";
            this.btnGoRmote.Size = new System.Drawing.Size(250, 73);
            this.btnGoRmote.TabIndex = 2;
            this.btnGoRmote.Text = "Go Remote";
            this.btnGoRmote.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnGoRmote.UseVisualStyleBackColor = true;
            this.btnGoRmote.Click += new System.EventHandler(this.btnGoRmote_Click);
            // 
            // btnGoOffline
            // 
            this.btnGoOffline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoOffline.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGoOffline.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGoOffline.Location = new System.Drawing.Point(3, 3);
            this.btnGoOffline.Name = "btnGoOffline";
            this.btnGoOffline.Size = new System.Drawing.Size(248, 73);
            this.btnGoOffline.TabIndex = 0;
            this.btnGoOffline.Text = "Go Offline";
            this.btnGoOffline.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnGoOffline.UseVisualStyleBackColor = true;
            this.btnGoOffline.Click += new System.EventHandler(this.btnGoOffline_Click);
            // 
            // btnGoOnline
            // 
            this.btnGoOnline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoOnline.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGoOnline.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGoOnline.Location = new System.Drawing.Point(257, 3);
            this.btnGoOnline.Name = "btnGoOnline";
            this.btnGoOnline.Size = new System.Drawing.Size(248, 73);
            this.btnGoOnline.TabIndex = 0;
            this.btnGoOnline.Text = "Go Online";
            this.btnGoOnline.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnGoOnline.UseVisualStyleBackColor = true;
            this.btnGoOnline.Click += new System.EventHandler(this.btnGoOnline_Click);
            // 
            // btnGoLocal
            // 
            this.btnGoLocal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoLocal.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGoLocal.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGoLocal.Location = new System.Drawing.Point(511, 3);
            this.btnGoLocal.Name = "btnGoLocal";
            this.btnGoLocal.Size = new System.Drawing.Size(248, 73);
            this.btnGoLocal.TabIndex = 1;
            this.btnGoLocal.Text = "Go Local";
            this.btnGoLocal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnGoLocal.UseVisualStyleBackColor = true;
            this.btnGoLocal.Click += new System.EventHandler(this.btnGoLocal_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.tableLayoutPanel2);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox4.Location = new System.Drawing.Point(0, 0);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(1024, 100);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "SECS ON/OFF";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.Controls.Add(this.btnSECSOff, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnSECSOn, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 18);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1018, 79);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // btnSECSOff
            // 
            this.btnSECSOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSECSOff.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSECSOff.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSECSOff.Location = new System.Drawing.Point(257, 3);
            this.btnSECSOff.Name = "btnSECSOff";
            this.btnSECSOff.Size = new System.Drawing.Size(248, 73);
            this.btnSECSOff.TabIndex = 0;
            this.btnSECSOff.Text = "SECS OFF";
            this.btnSECSOff.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnSECSOff.UseVisualStyleBackColor = true;
            this.btnSECSOff.Click += new System.EventHandler(this.btnSECSOff_Click);
            // 
            // btnSECSOn
            // 
            this.btnSECSOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSECSOn.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSECSOn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSECSOn.Location = new System.Drawing.Point(3, 3);
            this.btnSECSOn.Name = "btnSECSOn";
            this.btnSECSOn.Size = new System.Drawing.Size(248, 73);
            this.btnSECSOn.TabIndex = 0;
            this.btnSECSOn.Text = "SECS ON";
            this.btnSECSOn.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnSECSOn.UseVisualStyleBackColor = true;
            this.btnSECSOn.Click += new System.EventHandler(this.btnSECSOn_Click);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // frmSECSControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(1024, 678);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox4);
            this.Font = new System.Drawing.Font("Calibri", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmSECSControl";
            this.StartPosition = System.Windows.Forms.FormStartPosition.WindowsDefaultBounds;
            this.Text = "frmSECSControl";
            this.VisibleChanged += new System.EventHandler(this.frmSECSControl_VisibleChanged);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnGoRmote;
        private System.Windows.Forms.Button btnGoLocal;
        private System.Windows.Forms.Button btnGoOnline;
        private System.Windows.Forms.Button btnGoOffline;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button btnSECSOff;
        private System.Windows.Forms.Button btnSECSOn;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
    }
}