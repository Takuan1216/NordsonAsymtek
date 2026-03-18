namespace RorzeApi.GUI
{
    partial class GUIMaintSpeedSelect
    {
        /// <summary> 
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary> 
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.trbRobotSpeed = new System.Windows.Forms.TrackBar();
            this.lblRobotSpeed = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trbRobotSpeed)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Controls.Add(this.trbRobotSpeed, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblRobotSpeed, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(280, 59);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // trbRobotSpeed
            // 
            this.trbRobotSpeed.AutoSize = false;
            this.trbRobotSpeed.BackColor = System.Drawing.SystemColors.ControlLight;
            this.trbRobotSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trbRobotSpeed.Location = new System.Drawing.Point(4, 4);
            this.trbRobotSpeed.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.trbRobotSpeed.Maximum = 6;
            this.trbRobotSpeed.Minimum = 1;
            this.trbRobotSpeed.Name = "trbRobotSpeed";
            this.trbRobotSpeed.Size = new System.Drawing.Size(216, 51);
            this.trbRobotSpeed.TabIndex = 11;
            this.trbRobotSpeed.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.trbRobotSpeed.Value = 6;
            this.trbRobotSpeed.Scroll += new System.EventHandler(this.trbRobotSpeed_Scroll);
            this.trbRobotSpeed.MouseUp += new System.Windows.Forms.MouseEventHandler(this.trbRobotSpeed_MouseUp);
            // 
            // lblRobotSpeed
            // 
            this.lblRobotSpeed.AutoSize = true;
            this.lblRobotSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRobotSpeed.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRobotSpeed.Location = new System.Drawing.Point(228, 0);
            this.lblRobotSpeed.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblRobotSpeed.Name = "lblRobotSpeed";
            this.lblRobotSpeed.Size = new System.Drawing.Size(48, 59);
            this.lblRobotSpeed.TabIndex = 12;
            this.lblRobotSpeed.Text = "30";
            this.lblRobotSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // GUIMaintSpeedSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "GUIMaintSpeedSelect";
            this.Size = new System.Drawing.Size(280, 59);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trbRobotSpeed)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TrackBar trbRobotSpeed;
        private System.Windows.Forms.Label lblRobotSpeed;
    }
}
