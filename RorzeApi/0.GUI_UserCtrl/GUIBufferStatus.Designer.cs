namespace RorzeApi._0.GUI_UserCtrl
{
    partial class GUIBufferStatus
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GUIBufferStatus));
            this.lblState = new System.Windows.Forms.Label();
            this.lblUnitName = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.lblSlot4Recipe = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tlpSlot4 = new System.Windows.Forms.TableLayoutPanel();
            this.guiSlot4Wafer = new RorzeApi.GUI.GUIEquipment();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.guiSlot1Wafer = new RorzeApi.GUI.GUIEquipment();
            this.lblSlot1Recipe = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.tlpSlot1 = new System.Windows.Forms.TableLayoutPanel();
            this.guiSlot2Wafer = new RorzeApi.GUI.GUIEquipment();
            this.lblSlot2Recipe = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.tlpSlot2 = new System.Windows.Forms.TableLayoutPanel();
            this.label7 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.guiSlot3Wafer = new RorzeApi.GUI.GUIEquipment();
            this.lblSlot3Recipe = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.tlpSlot3 = new System.Windows.Forms.TableLayoutPanel();
            this.tlpSlot4.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tlpSlot1.SuspendLayout();
            this.tlpSlot2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tlpSlot3.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblState
            // 
            this.lblState.BackColor = System.Drawing.Color.Gainsboro;
            resources.ApplyResources(this.lblState, "lblState");
            this.lblState.Name = "lblState";
            // 
            // lblUnitName
            // 
            this.lblUnitName.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.lblUnitName, "lblUnitName");
            this.lblUnitName.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblUnitName.Name = "lblUnitName";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // lblSlot4Recipe
            // 
            this.lblSlot4Recipe.BackColor = System.Drawing.Color.Gainsboro;
            resources.ApplyResources(this.lblSlot4Recipe, "lblSlot4Recipe");
            this.lblSlot4Recipe.Name = "lblSlot4Recipe";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // tlpSlot4
            // 
            resources.ApplyResources(this.tlpSlot4, "tlpSlot4");
            this.tlpSlot4.Controls.Add(this.label4, 0, 0);
            this.tlpSlot4.Controls.Add(this.lblSlot4Recipe, 1, 0);
            this.tlpSlot4.Controls.Add(this.guiSlot4Wafer, 2, 0);
            this.tlpSlot4.Name = "tlpSlot4";
            // 
            // guiSlot4Wafer
            // 
            this.guiSlot4Wafer.EquipmentStatus = RorzeApi.GUI.GUIEquipment.enuWaferStatus.s0_Idle;
            resources.ApplyResources(this.guiSlot4Wafer, "guiSlot4Wafer");
            this.guiSlot4Wafer.Name = "guiSlot4Wafer";
            this.guiSlot4Wafer.WaferSlotNo = null;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.lblUnitName, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label10, 0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // guiSlot1Wafer
            // 
            this.guiSlot1Wafer.EquipmentStatus = RorzeApi.GUI.GUIEquipment.enuWaferStatus.s0_Idle;
            resources.ApplyResources(this.guiSlot1Wafer, "guiSlot1Wafer");
            this.guiSlot1Wafer.Name = "guiSlot1Wafer";
            this.guiSlot1Wafer.WaferSlotNo = null;
            // 
            // lblSlot1Recipe
            // 
            this.lblSlot1Recipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.lblSlot1Recipe, "lblSlot1Recipe");
            this.lblSlot1Recipe.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblSlot1Recipe.Name = "lblSlot1Recipe";
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            // 
            // tlpSlot1
            // 
            resources.ApplyResources(this.tlpSlot1, "tlpSlot1");
            this.tlpSlot1.Controls.Add(this.label14, 0, 0);
            this.tlpSlot1.Controls.Add(this.lblSlot1Recipe, 1, 0);
            this.tlpSlot1.Controls.Add(this.guiSlot1Wafer, 2, 0);
            this.tlpSlot1.Name = "tlpSlot1";
            // 
            // guiSlot2Wafer
            // 
            this.guiSlot2Wafer.EquipmentStatus = RorzeApi.GUI.GUIEquipment.enuWaferStatus.s0_Idle;
            resources.ApplyResources(this.guiSlot2Wafer, "guiSlot2Wafer");
            this.guiSlot2Wafer.Name = "guiSlot2Wafer";
            this.guiSlot2Wafer.WaferSlotNo = null;
            // 
            // lblSlot2Recipe
            // 
            this.lblSlot2Recipe.BackColor = System.Drawing.Color.Gainsboro;
            resources.ApplyResources(this.lblSlot2Recipe, "lblSlot2Recipe");
            this.lblSlot2Recipe.Name = "lblSlot2Recipe";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // tlpSlot2
            // 
            resources.ApplyResources(this.tlpSlot2, "tlpSlot2");
            this.tlpSlot2.Controls.Add(this.label11, 0, 0);
            this.tlpSlot2.Controls.Add(this.lblSlot2Recipe, 1, 0);
            this.tlpSlot2.Controls.Add(this.guiSlot2Wafer, 2, 0);
            this.tlpSlot2.Name = "tlpSlot2";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.lblState, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // guiSlot3Wafer
            // 
            this.guiSlot3Wafer.EquipmentStatus = RorzeApi.GUI.GUIEquipment.enuWaferStatus.s0_Idle;
            resources.ApplyResources(this.guiSlot3Wafer, "guiSlot3Wafer");
            this.guiSlot3Wafer.Name = "guiSlot3Wafer";
            this.guiSlot3Wafer.WaferSlotNo = null;
            // 
            // lblSlot3Recipe
            // 
            this.lblSlot3Recipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.lblSlot3Recipe, "lblSlot3Recipe");
            this.lblSlot3Recipe.Name = "lblSlot3Recipe";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // tlpSlot3
            // 
            resources.ApplyResources(this.tlpSlot3, "tlpSlot3");
            this.tlpSlot3.Controls.Add(this.label8, 0, 0);
            this.tlpSlot3.Controls.Add(this.lblSlot3Recipe, 1, 0);
            this.tlpSlot3.Controls.Add(this.guiSlot3Wafer, 2, 0);
            this.tlpSlot3.Name = "tlpSlot3";
            // 
            // GUIBufferStatus
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.tlpSlot1);
            this.Controls.Add(this.tlpSlot2);
            this.Controls.Add(this.tlpSlot3);
            this.Controls.Add(this.tlpSlot4);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Name = "GUIBufferStatus";
            this.tlpSlot4.ResumeLayout(false);
            this.tlpSlot4.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tlpSlot1.ResumeLayout(false);
            this.tlpSlot1.PerformLayout();
            this.tlpSlot2.ResumeLayout(false);
            this.tlpSlot2.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tlpSlot3.ResumeLayout(false);
            this.tlpSlot3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.Label lblUnitName;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label lblSlot4Recipe;
        private System.Windows.Forms.Label label4;
        private GUI.GUIEquipment guiSlot4Wafer;
        private System.Windows.Forms.TableLayoutPanel tlpSlot4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private GUI.GUIEquipment guiSlot1Wafer;
        private System.Windows.Forms.TableLayoutPanel tlpSlot1;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label lblSlot1Recipe;
        private GUI.GUIEquipment guiSlot2Wafer;
        private System.Windows.Forms.TableLayoutPanel tlpSlot2;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label lblSlot2Recipe;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private GUI.GUIEquipment guiSlot3Wafer;
        private System.Windows.Forms.Label lblSlot3Recipe;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TableLayoutPanel tlpSlot3;
    }
}
