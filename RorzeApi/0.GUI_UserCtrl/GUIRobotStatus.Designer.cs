namespace RorzeApi._0.GUI_UserCtrl
{
    partial class GUIRobotStatus
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GUIRobotStatus));
            this.lblState = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblUpperRecipe = new System.Windows.Forms.Label();
            this.lblUpperSlotNo = new System.Windows.Forms.Label();
            this.lblUpperSlotNoTitle = new System.Windows.Forms.Label();
            this.lblUpperRecipeTitle = new System.Windows.Forms.Label();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.lblLowerSlotNo = new System.Windows.Forms.Label();
            this.lblUnitName = new System.Windows.Forms.Label();
            this.lblLowerRecipe = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.lblLowerRecipeTitle = new System.Windows.Forms.Label();
            this.lblLowerSlotNoTitle = new System.Windows.Forms.Label();
            this.guiRobotArm1 = new RorzeApi.GUI.GUIRobotArm();
            this.guiRobotArm2 = new RorzeApi.GUI.GUIRobotArm();
            this.tableLayoutPanel4.SuspendLayout();
            this.SuspendLayout();
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
            // lblUpperRecipe
            // 
            this.lblUpperRecipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.lblUpperRecipe, "lblUpperRecipe");
            this.lblUpperRecipe.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblUpperRecipe.Name = "lblUpperRecipe";
            // 
            // lblUpperSlotNo
            // 
            this.lblUpperSlotNo.BackColor = System.Drawing.Color.Gainsboro;
            resources.ApplyResources(this.lblUpperSlotNo, "lblUpperSlotNo");
            this.lblUpperSlotNo.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblUpperSlotNo.Name = "lblUpperSlotNo";
            // 
            // lblUpperSlotNoTitle
            // 
            resources.ApplyResources(this.lblUpperSlotNoTitle, "lblUpperSlotNoTitle");
            this.lblUpperSlotNoTitle.Name = "lblUpperSlotNoTitle";
            // 
            // lblUpperRecipeTitle
            // 
            resources.ApplyResources(this.lblUpperRecipeTitle, "lblUpperRecipeTitle");
            this.lblUpperRecipeTitle.Name = "lblUpperRecipeTitle";
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.lblState, 1, 5);
            this.tableLayoutPanel4.Controls.Add(this.lblLowerSlotNo, 1, 3);
            this.tableLayoutPanel4.Controls.Add(this.label7, 0, 5);
            this.tableLayoutPanel4.Controls.Add(this.lblUpperRecipe, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.lblUnitName, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblLowerRecipe, 1, 4);
            this.tableLayoutPanel4.Controls.Add(this.label10, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblUpperSlotNoTitle, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblUpperSlotNo, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.lblLowerRecipeTitle, 0, 4);
            this.tableLayoutPanel4.Controls.Add(this.lblUpperRecipeTitle, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.lblLowerSlotNoTitle, 0, 3);
            this.tableLayoutPanel4.Controls.Add(this.guiRobotArm1, 2, 0);
            this.tableLayoutPanel4.Controls.Add(this.guiRobotArm2, 2, 3);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // lblLowerSlotNo
            // 
            this.lblLowerSlotNo.BackColor = System.Drawing.Color.Gainsboro;
            resources.ApplyResources(this.lblLowerSlotNo, "lblLowerSlotNo");
            this.lblLowerSlotNo.Name = "lblLowerSlotNo";
            // 
            // lblUnitName
            // 
            this.lblUnitName.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.lblUnitName, "lblUnitName");
            this.lblUnitName.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblUnitName.Name = "lblUnitName";
            // 
            // lblLowerRecipe
            // 
            this.lblLowerRecipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.lblLowerRecipe, "lblLowerRecipe");
            this.lblLowerRecipe.Name = "lblLowerRecipe";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // lblLowerRecipeTitle
            // 
            resources.ApplyResources(this.lblLowerRecipeTitle, "lblLowerRecipeTitle");
            this.lblLowerRecipeTitle.Name = "lblLowerRecipeTitle";
            // 
            // lblLowerSlotNoTitle
            // 
            resources.ApplyResources(this.lblLowerSlotNoTitle, "lblLowerSlotNoTitle");
            this.lblLowerSlotNoTitle.Name = "lblLowerSlotNoTitle";
            // 
            // guiRobotArm1
            // 
            this.guiRobotArm1.ArmStatus = RorzeApi.GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
            resources.ApplyResources(this.guiRobotArm1, "guiRobotArm1");
            this.guiRobotArm1.Name = "guiRobotArm1";
            this.tableLayoutPanel4.SetRowSpan(this.guiRobotArm1, 3);
            // 
            // guiRobotArm2
            // 
            this.guiRobotArm2.ArmStatus = RorzeApi.GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
            resources.ApplyResources(this.guiRobotArm2, "guiRobotArm2");
            this.guiRobotArm2.Name = "guiRobotArm2";
            this.tableLayoutPanel4.SetRowSpan(this.guiRobotArm2, 3);
            // 
            // GUIRobotStatus
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel4);
            this.Name = "GUIRobotStatus";
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label lblUpperRecipe;
        private System.Windows.Forms.Label lblUpperSlotNo;
        private System.Windows.Forms.Label lblUpperSlotNoTitle;
        private System.Windows.Forms.Label lblUpperRecipeTitle;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label lblUnitName;
        private System.Windows.Forms.Label label10;
        private GUI.GUIRobotArm guiRobotArm1;
        private System.Windows.Forms.Label lblLowerSlotNo;
        private System.Windows.Forms.Label lblLowerRecipe;
        private System.Windows.Forms.Label lblLowerRecipeTitle;
        private System.Windows.Forms.Label lblLowerSlotNoTitle;
        private GUI.GUIRobotArm guiRobotArm2;
    }
}
