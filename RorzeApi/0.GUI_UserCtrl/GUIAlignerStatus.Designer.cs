namespace RorzeApi._0.GUI_UserCtrl
{
    partial class GUIAlignerStatus
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GUIAlignerStatus));
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.lblSlotNo = new System.Windows.Forms.Label();
            this.guiAligner = new RorzeApi.GUI.GUIAligner();
            this.label28 = new System.Windows.Forms.Label();
            this.lblRecipe = new System.Windows.Forms.Label();
            this.label33 = new System.Windows.Forms.Label();
            this.lblUnitName = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.lblState = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.tableLayoutPanel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.lblSlotNo, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.guiAligner, 2, 0);
            this.tableLayoutPanel4.Controls.Add(this.label28, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblRecipe, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.label33, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.lblUnitName, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.label25, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblState, 1, 3);
            this.tableLayoutPanel4.Controls.Add(this.label7, 0, 3);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // lblSlotNo
            // 
            this.lblSlotNo.BackColor = System.Drawing.Color.Gainsboro;
            resources.ApplyResources(this.lblSlotNo, "lblSlotNo");
            this.lblSlotNo.Name = "lblSlotNo";
            // 
            // guiAligner
            // 
            this.guiAligner.AlignerStatus = RorzeApi.GUI.GUIAligner.enuWaferStatus.s0_Idle;
            resources.ApplyResources(this.guiAligner, "guiAligner");
            this.guiAligner.Name = "guiAligner";
            this.tableLayoutPanel4.SetRowSpan(this.guiAligner, 4);
            // 
            // label28
            // 
            resources.ApplyResources(this.label28, "label28");
            this.label28.Name = "label28";
            // 
            // lblRecipe
            // 
            this.lblRecipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.lblRecipe, "lblRecipe");
            this.lblRecipe.Name = "lblRecipe";
            // 
            // label33
            // 
            resources.ApplyResources(this.label33, "label33");
            this.label33.Name = "label33";
            // 
            // lblUnitName
            // 
            this.lblUnitName.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.lblUnitName, "lblUnitName");
            this.lblUnitName.Name = "lblUnitName";
            // 
            // label25
            // 
            resources.ApplyResources(this.label25, "label25");
            this.label25.Name = "label25";
            // 
            // lblState
            // 
            this.lblState.BackColor = System.Drawing.Color.Gainsboro;
            resources.ApplyResources(this.lblState, "lblState");
            this.lblState.Name = "lblState";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // GUIAlignerStatus
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel4);
            this.Name = "GUIAlignerStatus";
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label lblSlotNo;
        private GUI.GUIAligner guiAligner;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.Label lblRecipe;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.Label lblUnitName;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblState;
    }
}
