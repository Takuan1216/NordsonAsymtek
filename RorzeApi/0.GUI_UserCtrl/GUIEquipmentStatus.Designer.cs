namespace RorzeApi._0.GUI_UserCtrl
{
    partial class GUIEquipmentStatus
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
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.lblEQName = new System.Windows.Forms.Label();
            this.lblState = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.guiEquipment = new RorzeApi.GUI.GUIEquipment();
            this.lblRecipe = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblSlotNo = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.tableLayoutPanel7.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 3;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.tableLayoutPanel7.Controls.Add(this.lblEQName, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.lblState, 1, 3);
            this.tableLayoutPanel7.Controls.Add(this.label14, 0, 3);
            this.tableLayoutPanel7.Controls.Add(this.guiEquipment, 2, 0);
            this.tableLayoutPanel7.Controls.Add(this.lblRecipe, 1, 2);
            this.tableLayoutPanel7.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.label5, 0, 1);
            this.tableLayoutPanel7.Controls.Add(this.lblSlotNo, 1, 1);
            this.tableLayoutPanel7.Controls.Add(this.label11, 0, 2);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(2, 4, 2, 4);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 4;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(270, 80);
            this.tableLayoutPanel7.TabIndex = 315;
            // 
            // lblEQName
            // 
            this.lblEQName.BackColor = System.Drawing.Color.White;
            this.lblEQName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEQName.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.lblEQName.Location = new System.Drawing.Point(55, 1);
            this.lblEQName.Margin = new System.Windows.Forms.Padding(1);
            this.lblEQName.Name = "lblEQName";
            this.lblEQName.Size = new System.Drawing.Size(111, 18);
            this.lblEQName.TabIndex = 316;
            this.lblEQName.Text = "Equipment";
            this.lblEQName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblState
            // 
            this.lblState.AutoSize = true;
            this.lblState.BackColor = System.Drawing.Color.Gainsboro;
            this.lblState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblState.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.lblState.Location = new System.Drawing.Point(55, 61);
            this.lblState.Margin = new System.Windows.Forms.Padding(1);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(111, 18);
            this.lblState.TabIndex = 317;
            this.lblState.Text = "-";
            this.lblState.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label14.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.label14.Location = new System.Drawing.Point(1, 61);
            this.label14.Margin = new System.Windows.Forms.Padding(1);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(52, 18);
            this.label14.TabIndex = 316;
            this.label14.Text = "State:";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // guiEquipment
            // 
            this.guiEquipment.Dock = System.Windows.Forms.DockStyle.Fill;
            this.guiEquipment.EquipmentStatus = RorzeApi.GUI.GUIEquipment.enuWaferStatus.s0_Idle;
            this.guiEquipment.Font = new System.Drawing.Font("Calibri", 9F);
            this.guiEquipment.Location = new System.Drawing.Point(171, 6);
            this.guiEquipment.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.guiEquipment.Name = "guiEquipment";
            this.tableLayoutPanel7.SetRowSpan(this.guiEquipment, 4);
            this.guiEquipment.Size = new System.Drawing.Size(95, 68);
            this.guiEquipment.TabIndex = 1;
            // 
            // lblRecipe
            // 
            this.lblRecipe.BackColor = System.Drawing.Color.White;
            this.lblRecipe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecipe.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.lblRecipe.Location = new System.Drawing.Point(55, 41);
            this.lblRecipe.Margin = new System.Windows.Forms.Padding(1);
            this.lblRecipe.Name = "lblRecipe";
            this.lblRecipe.Size = new System.Drawing.Size(111, 18);
            this.lblRecipe.TabIndex = 0;
            this.lblRecipe.Text = "-";
            this.lblRecipe.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.label1.Location = new System.Drawing.Point(1, 1);
            this.label1.Margin = new System.Windows.Forms.Padding(1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "Unit:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.label5.Location = new System.Drawing.Point(1, 21);
            this.label5.Margin = new System.Windows.Forms.Padding(1);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "Slot No:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSlotNo
            // 
            this.lblSlotNo.BackColor = System.Drawing.Color.Gainsboro;
            this.lblSlotNo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotNo.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.lblSlotNo.Location = new System.Drawing.Point(55, 21);
            this.lblSlotNo.Margin = new System.Windows.Forms.Padding(1);
            this.lblSlotNo.Name = "lblSlotNo";
            this.lblSlotNo.Size = new System.Drawing.Size(111, 18);
            this.lblSlotNo.TabIndex = 0;
            this.lblSlotNo.Text = "-";
            this.lblSlotNo.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label11.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.label11.Location = new System.Drawing.Point(1, 41);
            this.label11.Margin = new System.Windows.Forms.Padding(1);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(52, 18);
            this.label11.TabIndex = 0;
            this.label11.Text = "Recipe:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // GUIEquipmentStatus
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.tableLayoutPanel7);
            this.Font = new System.Drawing.Font("Calibri", 8.25F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "GUIEquipmentStatus";
            this.Size = new System.Drawing.Size(270, 80);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private GUI.GUIEquipment guiEquipment;
        private System.Windows.Forms.Label lblRecipe;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblSlotNo;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label lblEQName;
    }
}
