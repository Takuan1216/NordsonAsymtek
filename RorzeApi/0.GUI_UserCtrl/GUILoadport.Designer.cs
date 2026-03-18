namespace RorzeApi.GUI
{
    partial class GUILoadport
    {
        /// <summary> 
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置 Managed 資源則為 true，否則為 false。</param>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GUILoadport));
            this.panelStage = new System.Windows.Forms.Panel();
            this.panelWaferData = new System.Windows.Forms.Panel();
            this.tlpWaferData = new System.Windows.Forms.TableLayoutPanel();
            this.chkFoupOn = new System.Windows.Forms.CheckBox();
            this.cbxViewSlot = new System.Windows.Forms.ComboBox();
            this.btnE84Mode = new System.Windows.Forms.Button();
            this.btnTitle = new System.Windows.Forms.Button();
            this.tlpRFID = new System.Windows.Forms.TableLayoutPanel();
            this.txtFoupID = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tlpRSV = new System.Windows.Forms.TableLayoutPanel();
            this.txtLoaderRSV = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tlpRecipeSelect = new System.Windows.Forms.TableLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.cbxRecipe = new System.Windows.Forms.ComboBox();
            this.tlpStatus = new System.Windows.Forms.TableLayoutPanel();
            this.lblLoaderStatus = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.tlpInfoPad = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblLoaderType = new System.Windows.Forms.Label();
            this.tlpProcessBtn = new System.Windows.Forms.TableLayoutPanel();
            this.btnProcessStart = new System.Windows.Forms.Button();
            this.tlpDockBtn = new System.Windows.Forms.TableLayoutPanel();
            this.btnDock = new System.Windows.Forms.Button();
            this.btnUnDock = new System.Windows.Forms.Button();
            this.tlpClampLock = new System.Windows.Forms.TableLayoutPanel();
            this.btnClampLock = new System.Windows.Forms.Button();
            this.panelStage.SuspendLayout();
            this.panelWaferData.SuspendLayout();
            this.tlpRFID.SuspendLayout();
            this.tlpRSV.SuspendLayout();
            this.tlpRecipeSelect.SuspendLayout();
            this.tlpStatus.SuspendLayout();
            this.tlpInfoPad.SuspendLayout();
            this.tlpProcessBtn.SuspendLayout();
            this.tlpDockBtn.SuspendLayout();
            this.tlpClampLock.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelStage
            // 
            this.panelStage.Controls.Add(this.panelWaferData);
            this.panelStage.Controls.Add(this.chkFoupOn);
            this.panelStage.Controls.Add(this.cbxViewSlot);
            this.panelStage.Controls.Add(this.btnE84Mode);
            this.panelStage.Controls.Add(this.btnTitle);
            this.panelStage.Controls.Add(this.tlpRFID);
            this.panelStage.Controls.Add(this.tlpRSV);
            this.panelStage.Controls.Add(this.tlpRecipeSelect);
            this.panelStage.Controls.Add(this.tlpStatus);
            this.panelStage.Controls.Add(this.tlpInfoPad);
            this.panelStage.Controls.Add(this.tlpProcessBtn);
            this.panelStage.Controls.Add(this.tlpDockBtn);
            this.panelStage.Controls.Add(this.tlpClampLock);
            resources.ApplyResources(this.panelStage, "panelStage");
            this.panelStage.Name = "panelStage";
            // 
            // panelWaferData
            // 
            this.panelWaferData.Controls.Add(this.tlpWaferData);
            resources.ApplyResources(this.panelWaferData, "panelWaferData");
            this.panelWaferData.Name = "panelWaferData";
            // 
            // tlpWaferData
            // 
            resources.ApplyResources(this.tlpWaferData, "tlpWaferData");
            this.tlpWaferData.Name = "tlpWaferData";
            // 
            // chkFoupOn
            // 
            resources.ApplyResources(this.chkFoupOn, "chkFoupOn");
            this.chkFoupOn.Name = "chkFoupOn";
            this.chkFoupOn.UseVisualStyleBackColor = true;
            this.chkFoupOn.CheckedChanged += new System.EventHandler(this.chkFoupOn_CheckedChanged);
            this.chkFoupOn.Click += new System.EventHandler(this.chkFoupOn_Click);
            // 
            // cbxViewSlot
            // 
            resources.ApplyResources(this.cbxViewSlot, "cbxViewSlot");
            this.cbxViewSlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxViewSlot.FormattingEnabled = true;
            this.cbxViewSlot.Items.AddRange(new object[] {
            resources.GetString("cbxViewSlot.Items"),
            resources.GetString("cbxViewSlot.Items1"),
            resources.GetString("cbxViewSlot.Items2")});
            this.cbxViewSlot.Name = "cbxViewSlot";
            // 
            // btnE84Mode
            // 
            this.btnE84Mode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(149)))), ((int)(((byte)(149)))));
            resources.ApplyResources(this.btnE84Mode, "btnE84Mode");
            this.btnE84Mode.ForeColor = System.Drawing.Color.Black;
            this.btnE84Mode.Name = "btnE84Mode";
            this.btnE84Mode.UseVisualStyleBackColor = false;
            this.btnE84Mode.Click += new System.EventHandler(this.btnE84Mode_Click);
            // 
            // btnTitle
            // 
            this.btnTitle.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            resources.ApplyResources(this.btnTitle, "btnTitle");
            this.btnTitle.ForeColor = System.Drawing.Color.White;
            this.btnTitle.Name = "btnTitle";
            this.btnTitle.Tag = "4";
            this.btnTitle.UseVisualStyleBackColor = false;
            // 
            // tlpRFID
            // 
            resources.ApplyResources(this.tlpRFID, "tlpRFID");
            this.tlpRFID.Controls.Add(this.txtFoupID, 1, 0);
            this.tlpRFID.Controls.Add(this.label11, 0, 0);
            this.tlpRFID.Name = "tlpRFID";
            // 
            // txtFoupID
            // 
            resources.ApplyResources(this.txtFoupID, "txtFoupID");
            this.txtFoupID.Name = "txtFoupID";
            this.txtFoupID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtFoupID_KeyDown);
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // tlpRSV
            // 
            resources.ApplyResources(this.tlpRSV, "tlpRSV");
            this.tlpRSV.Controls.Add(this.txtLoaderRSV, 1, 0);
            this.tlpRSV.Controls.Add(this.label2, 0, 0);
            this.tlpRSV.Name = "tlpRSV";
            // 
            // txtLoaderRSV
            // 
            resources.ApplyResources(this.txtLoaderRSV, "txtLoaderRSV");
            this.txtLoaderRSV.Name = "txtLoaderRSV";
            this.txtLoaderRSV.ReadOnly = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.BackColor = System.Drawing.SystemColors.Control;
            this.label2.Name = "label2";
            // 
            // tlpRecipeSelect
            // 
            resources.ApplyResources(this.tlpRecipeSelect, "tlpRecipeSelect");
            this.tlpRecipeSelect.Controls.Add(this.label5, 0, 0);
            this.tlpRecipeSelect.Controls.Add(this.cbxRecipe, 1, 0);
            this.tlpRecipeSelect.Name = "tlpRecipeSelect";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // cbxRecipe
            // 
            resources.ApplyResources(this.cbxRecipe, "cbxRecipe");
            this.cbxRecipe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxRecipe.FormattingEnabled = true;
            this.cbxRecipe.Name = "cbxRecipe";
            this.cbxRecipe.SelectionChangeCommitted += new System.EventHandler(this.cbxRecipe_SelectionChangeCommitted);
            // 
            // tlpStatus
            // 
            resources.ApplyResources(this.tlpStatus, "tlpStatus");
            this.tlpStatus.Controls.Add(this.lblLoaderStatus, 1, 0);
            this.tlpStatus.Controls.Add(this.label17, 0, 0);
            this.tlpStatus.Name = "tlpStatus";
            // 
            // lblLoaderStatus
            // 
            resources.ApplyResources(this.lblLoaderStatus, "lblLoaderStatus");
            this.lblLoaderStatus.Name = "lblLoaderStatus";
            // 
            // label17
            // 
            resources.ApplyResources(this.label17, "label17");
            this.label17.Name = "label17";
            // 
            // tlpInfoPad
            // 
            resources.ApplyResources(this.tlpInfoPad, "tlpInfoPad");
            this.tlpInfoPad.Controls.Add(this.label1, 0, 0);
            this.tlpInfoPad.Controls.Add(this.lblLoaderType, 1, 0);
            this.tlpInfoPad.Name = "tlpInfoPad";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // lblLoaderType
            // 
            resources.ApplyResources(this.lblLoaderType, "lblLoaderType");
            this.lblLoaderType.Name = "lblLoaderType";
            // 
            // tlpProcessBtn
            // 
            resources.ApplyResources(this.tlpProcessBtn, "tlpProcessBtn");
            this.tlpProcessBtn.Controls.Add(this.btnProcessStart, 0, 0);
            this.tlpProcessBtn.Name = "tlpProcessBtn";
            // 
            // btnProcessStart
            // 
            resources.ApplyResources(this.btnProcessStart, "btnProcessStart");
            this.btnProcessStart.Name = "btnProcessStart";
            this.btnProcessStart.UseVisualStyleBackColor = true;
            this.btnProcessStart.Click += new System.EventHandler(this.btnProcessStart_Click);
            // 
            // tlpDockBtn
            // 
            resources.ApplyResources(this.tlpDockBtn, "tlpDockBtn");
            this.tlpDockBtn.Controls.Add(this.btnDock, 0, 0);
            this.tlpDockBtn.Controls.Add(this.btnUnDock, 1, 0);
            this.tlpDockBtn.Name = "tlpDockBtn";
            // 
            // btnDock
            // 
            resources.ApplyResources(this.btnDock, "btnDock");
            this.btnDock.Name = "btnDock";
            this.btnDock.UseVisualStyleBackColor = true;
            this.btnDock.Click += new System.EventHandler(this.btnDock_Click);
            // 
            // btnUnDock
            // 
            resources.ApplyResources(this.btnUnDock, "btnUnDock");
            this.btnUnDock.Name = "btnUnDock";
            this.btnUnDock.UseVisualStyleBackColor = true;
            this.btnUnDock.Click += new System.EventHandler(this.btnUnDock_Click);
            // 
            // tlpClampLock
            // 
            resources.ApplyResources(this.tlpClampLock, "tlpClampLock");
            this.tlpClampLock.Controls.Add(this.btnClampLock, 0, 0);
            this.tlpClampLock.Name = "tlpClampLock";
            // 
            // btnClampLock
            // 
            resources.ApplyResources(this.btnClampLock, "btnClampLock");
            this.btnClampLock.Name = "btnClampLock";
            this.btnClampLock.UseVisualStyleBackColor = true;
            this.btnClampLock.Click += new System.EventHandler(this.btnClampLock_Click);
            // 
            // GUILoadport
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.panelStage);
            this.DoubleBuffered = true;
            this.Name = "GUILoadport";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.GUILoadport_Paint);
            this.panelStage.ResumeLayout(false);
            this.panelStage.PerformLayout();
            this.panelWaferData.ResumeLayout(false);
            this.tlpRFID.ResumeLayout(false);
            this.tlpRFID.PerformLayout();
            this.tlpRSV.ResumeLayout(false);
            this.tlpRSV.PerformLayout();
            this.tlpRecipeSelect.ResumeLayout(false);
            this.tlpRecipeSelect.PerformLayout();
            this.tlpStatus.ResumeLayout(false);
            this.tlpStatus.PerformLayout();
            this.tlpInfoPad.ResumeLayout(false);
            this.tlpInfoPad.PerformLayout();
            this.tlpProcessBtn.ResumeLayout(false);
            this.tlpDockBtn.ResumeLayout(false);
            this.tlpClampLock.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelStage;
        private System.Windows.Forms.CheckBox chkFoupOn;
        private System.Windows.Forms.Button btnTitle;
        private System.Windows.Forms.TableLayoutPanel tlpRFID;
        private System.Windows.Forms.TextBox txtFoupID;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TableLayoutPanel tlpRecipeSelect;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbxRecipe;
        private System.Windows.Forms.TableLayoutPanel tlpStatus;
        private System.Windows.Forms.Label lblLoaderStatus;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TableLayoutPanel tlpInfoPad;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblLoaderType;
        private System.Windows.Forms.TableLayoutPanel tlpDockBtn;
        private System.Windows.Forms.TableLayoutPanel tlpClampLock;
        private System.Windows.Forms.Button btnClampLock;
        private System.Windows.Forms.Panel panelWaferData;
        private System.Windows.Forms.TableLayoutPanel tlpWaferData;
        private System.Windows.Forms.TableLayoutPanel tlpRSV;
        private System.Windows.Forms.TextBox txtLoaderRSV;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tlpProcessBtn;
        private System.Windows.Forms.Button btnProcessStart;
        private System.Windows.Forms.Button btnDock;
        private System.Windows.Forms.Button btnUnDock;
        private System.Windows.Forms.ComboBox cbxViewSlot;
        private System.Windows.Forms.Button btnE84Mode;
    }
}
