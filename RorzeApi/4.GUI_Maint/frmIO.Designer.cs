namespace RorzeApi
{
    partial class frmIO
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmIO));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
            this.dataGridViewImageColumn2 = new System.Windows.Forms.DataGridViewImageColumn();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageRC5X0 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvIN = new System.Windows.Forms.DataGridView();
            this.dataGridViewImageColumn3 = new System.Windows.Forms.DataGridViewImageColumn();
            this.dgvOUT = new System.Windows.Forms.DataGridView();
            this.ONOFFy = new System.Windows.Forms.DataGridViewImageColumn();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.GPRS_Pa = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cbModules = new System.Windows.Forms.ComboBox();
            this.gbxFFU = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.txtFFU = new System.Windows.Forms.TextBox();
            this.FanBar = new System.Windows.Forms.TrackBar();
            this.lblFFUrpm = new System.Windows.Forms.Label();
            this.tabPageAdam = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvIN_ADAM = new System.Windows.Forms.DataGridView();
            this.dataGridViewImageColumn4 = new System.Windows.Forms.DataGridViewImageColumn();
            this.dgvOUT_ADAM = new System.Windows.Forms.DataGridView();
            this.dataGridViewImageColumn5 = new System.Windows.Forms.DataGridViewImageColumn();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.ADAMcbModules = new System.Windows.Forms.ComboBox();
            this.tabPagePLC = new System.Windows.Forms.TabPage();
            this.tlpPLC = new System.Windows.Forms.TableLayoutPanel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPageRC5X0.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvIN)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOUT)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.gbxFFU.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FanBar)).BeginInit();
            this.tabPageAdam.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvIN_ADAM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOUT_ADAM)).BeginInit();
            this.tableLayoutPanel5.SuspendLayout();
            this.tabPagePLC.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridViewImageColumn1
            // 
            resources.ApplyResources(this.dataGridViewImageColumn1, "dataGridViewImageColumn1");
            this.dataGridViewImageColumn1.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
            this.dataGridViewImageColumn1.ReadOnly = true;
            this.dataGridViewImageColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // dataGridViewImageColumn2
            // 
            resources.ApplyResources(this.dataGridViewImageColumn2, "dataGridViewImageColumn2");
            this.dataGridViewImageColumn2.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dataGridViewImageColumn2.Name = "dataGridViewImageColumn2";
            this.dataGridViewImageColumn2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageRC5X0);
            this.tabControl1.Controls.Add(this.tabPageAdam);
            this.tabControl1.Controls.Add(this.tabPagePLC);
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabPageRC5X0
            // 
            this.tabPageRC5X0.Controls.Add(this.tableLayoutPanel1);
            this.tabPageRC5X0.Controls.Add(this.tableLayoutPanel2);
            resources.ApplyResources(this.tabPageRC5X0, "tabPageRC5X0");
            this.tabPageRC5X0.Name = "tabPageRC5X0";
            this.tabPageRC5X0.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.dgvIN, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.dgvOUT, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // dgvIN
            // 
            this.dgvIN.AllowUserToAddRows = false;
            this.dgvIN.AllowUserToDeleteRows = false;
            this.dgvIN.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dgvIN.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dgvIN.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvIN.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewImageColumn3});
            resources.ApplyResources(this.dgvIN, "dgvIN");
            this.dgvIN.GridColor = System.Drawing.SystemColors.ControlLight;
            this.dgvIN.MultiSelect = false;
            this.dgvIN.Name = "dgvIN";
            this.dgvIN.ReadOnly = true;
            this.dgvIN.RowHeadersVisible = false;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvIN.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvIN.RowTemplate.Height = 23;
            this.dgvIN.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvIN.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvIO_CellDoubleClick);
            // 
            // dataGridViewImageColumn3
            // 
            resources.ApplyResources(this.dataGridViewImageColumn3, "dataGridViewImageColumn3");
            this.dataGridViewImageColumn3.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dataGridViewImageColumn3.Name = "dataGridViewImageColumn3";
            this.dataGridViewImageColumn3.ReadOnly = true;
            this.dataGridViewImageColumn3.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // dgvOUT
            // 
            this.dgvOUT.AllowUserToAddRows = false;
            this.dgvOUT.AllowUserToDeleteRows = false;
            this.dgvOUT.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dgvOUT.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dgvOUT.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOUT.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ONOFFy});
            resources.ApplyResources(this.dgvOUT, "dgvOUT");
            this.dgvOUT.GridColor = System.Drawing.SystemColors.ControlLight;
            this.dgvOUT.MultiSelect = false;
            this.dgvOUT.Name = "dgvOUT";
            this.dgvOUT.ReadOnly = true;
            this.dgvOUT.RowHeadersVisible = false;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvOUT.RowsDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvOUT.RowTemplate.Height = 24;
            this.dgvOUT.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvOUT.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvIO_CellDoubleClick);
            // 
            // ONOFFy
            // 
            resources.ApplyResources(this.ONOFFy, "ONOFFy");
            this.ONOFFy.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.ONOFFy.Name = "ONOFFy";
            this.ONOFFy.ReadOnly = true;
            this.ONOFFy.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.GPRS_Pa, 9, 0);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.cbModules, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.gbxFFU, 4, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // GPRS_Pa
            // 
            resources.ApplyResources(this.GPRS_Pa, "GPRS_Pa");
            this.GPRS_Pa.Name = "GPRS_Pa";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // cbModules
            // 
            resources.ApplyResources(this.cbModules, "cbModules");
            this.cbModules.BackColor = System.Drawing.SystemColors.ControlLight;
            this.tableLayoutPanel2.SetColumnSpan(this.cbModules, 2);
            this.cbModules.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbModules.FormattingEnabled = true;
            this.cbModules.Name = "cbModules";
            this.cbModules.SelectedIndexChanged += new System.EventHandler(this.cbModules_SelectedIndexChanged);
            // 
            // gbxFFU
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.gbxFFU, 3);
            this.gbxFFU.Controls.Add(this.tableLayoutPanel3);
            resources.ApplyResources(this.gbxFFU, "gbxFFU");
            this.gbxFFU.Name = "gbxFFU";
            this.gbxFFU.TabStop = false;
            // 
            // tableLayoutPanel3
            // 
            resources.ApplyResources(this.tableLayoutPanel3, "tableLayoutPanel3");
            this.tableLayoutPanel3.Controls.Add(this.txtFFU, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.FanBar, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.lblFFUrpm, 0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            // 
            // txtFFU
            // 
            resources.ApplyResources(this.txtFFU, "txtFFU");
            this.txtFFU.Name = "txtFFU";
            // 
            // FanBar
            // 
            resources.ApplyResources(this.FanBar, "FanBar");
            this.FanBar.BackColor = System.Drawing.SystemColors.ControlLight;
            this.FanBar.LargeChange = 10;
            this.FanBar.Maximum = 1650;
            this.FanBar.Name = "FanBar";
            this.FanBar.TickFrequency = 100;
            this.FanBar.Scroll += new System.EventHandler(this.FanBar_Scroll);
            this.FanBar.MouseUp += new System.Windows.Forms.MouseEventHandler(this.FanBar_MouseUp);
            // 
            // lblFFUrpm
            // 
            resources.ApplyResources(this.lblFFUrpm, "lblFFUrpm");
            this.tableLayoutPanel3.SetColumnSpan(this.lblFFUrpm, 2);
            this.lblFFUrpm.Name = "lblFFUrpm";
            // 
            // tabPageAdam
            // 
            this.tabPageAdam.Controls.Add(this.tableLayoutPanel4);
            this.tabPageAdam.Controls.Add(this.tableLayoutPanel5);
            resources.ApplyResources(this.tabPageAdam, "tabPageAdam");
            this.tabPageAdam.Name = "tabPageAdam";
            this.tabPageAdam.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.dgvIN_ADAM, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.dgvOUT_ADAM, 1, 0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // dgvIN_ADAM
            // 
            this.dgvIN_ADAM.AllowUserToAddRows = false;
            this.dgvIN_ADAM.AllowUserToDeleteRows = false;
            this.dgvIN_ADAM.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dgvIN_ADAM.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dgvIN_ADAM.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvIN_ADAM.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewImageColumn4});
            this.dgvIN_ADAM.GridColor = System.Drawing.SystemColors.ControlLight;
            resources.ApplyResources(this.dgvIN_ADAM, "dgvIN_ADAM");
            this.dgvIN_ADAM.MultiSelect = false;
            this.dgvIN_ADAM.Name = "dgvIN_ADAM";
            this.dgvIN_ADAM.ReadOnly = true;
            this.dgvIN_ADAM.RowHeadersVisible = false;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvIN_ADAM.RowsDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvIN_ADAM.RowTemplate.Height = 23;
            this.dgvIN_ADAM.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvIN_ADAM.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvIO_ADAM_CellDoubleClick);
            // 
            // dataGridViewImageColumn4
            // 
            resources.ApplyResources(this.dataGridViewImageColumn4, "dataGridViewImageColumn4");
            this.dataGridViewImageColumn4.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dataGridViewImageColumn4.Name = "dataGridViewImageColumn4";
            this.dataGridViewImageColumn4.ReadOnly = true;
            this.dataGridViewImageColumn4.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // dgvOUT_ADAM
            // 
            this.dgvOUT_ADAM.AllowUserToAddRows = false;
            this.dgvOUT_ADAM.AllowUserToDeleteRows = false;
            resources.ApplyResources(this.dgvOUT_ADAM, "dgvOUT_ADAM");
            this.dgvOUT_ADAM.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dgvOUT_ADAM.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dgvOUT_ADAM.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOUT_ADAM.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewImageColumn5});
            this.dgvOUT_ADAM.GridColor = System.Drawing.SystemColors.ControlLight;
            this.dgvOUT_ADAM.MultiSelect = false;
            this.dgvOUT_ADAM.Name = "dgvOUT_ADAM";
            this.dgvOUT_ADAM.ReadOnly = true;
            this.dgvOUT_ADAM.RowHeadersVisible = false;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvOUT_ADAM.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvOUT_ADAM.RowTemplate.Height = 24;
            this.dgvOUT_ADAM.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvOUT_ADAM.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvIO_ADAM_CellDoubleClick);
            // 
            // dataGridViewImageColumn5
            // 
            resources.ApplyResources(this.dataGridViewImageColumn5, "dataGridViewImageColumn5");
            this.dataGridViewImageColumn5.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.dataGridViewImageColumn5.Name = "dataGridViewImageColumn5";
            this.dataGridViewImageColumn5.ReadOnly = true;
            this.dataGridViewImageColumn5.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // tableLayoutPanel5
            // 
            resources.ApplyResources(this.tableLayoutPanel5, "tableLayoutPanel5");
            this.tableLayoutPanel5.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.ADAMcbModules, 1, 0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // ADAMcbModules
            // 
            resources.ApplyResources(this.ADAMcbModules, "ADAMcbModules");
            this.ADAMcbModules.BackColor = System.Drawing.SystemColors.ControlLight;
            this.tableLayoutPanel5.SetColumnSpan(this.ADAMcbModules, 2);
            this.ADAMcbModules.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ADAMcbModules.FormattingEnabled = true;
            this.ADAMcbModules.Name = "ADAMcbModules";
            this.ADAMcbModules.SelectedIndexChanged += new System.EventHandler(this.ADAMcbModules_SelectedIndexChanged);
            // 
            // tabPagePLC
            // 
            this.tabPagePLC.Controls.Add(this.tlpPLC);
            resources.ApplyResources(this.tabPagePLC, "tabPagePLC");
            this.tabPagePLC.Name = "tabPagePLC";
            this.tabPagePLC.UseVisualStyleBackColor = true;
            // 
            // tlpPLC
            // 
            resources.ApplyResources(this.tlpPLC, "tlpPLC");
            this.tlpPLC.Name = "tlpPLC";
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick_1);
            // 
            // timer2
            // 
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // frmIO
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmIO";
            this.ShowInTaskbar = false;
            this.Load += new System.EventHandler(this.frmIO_Load);
            this.VisibleChanged += new System.EventHandler(this.frmIO_VisibleChanged);
            this.tabControl1.ResumeLayout(false);
            this.tabPageRC5X0.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvIN)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOUT)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.gbxFFU.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FanBar)).EndInit();
            this.tabPageAdam.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvIN_ADAM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOUT_ADAM)).EndInit();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.tabPagePLC.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageRC5X0;
        private System.Windows.Forms.TabPage tabPagePLC;
        private System.Windows.Forms.TableLayoutPanel tlpPLC;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TabPage tabPageAdam;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox ADAMcbModules;        
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.DataGridView dgvIN_ADAM;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn4;
        private System.Windows.Forms.DataGridView dgvOUT_ADAM;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn5;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.DataGridView dgvIN;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn3;
        private System.Windows.Forms.DataGridView dgvOUT;
        private System.Windows.Forms.DataGridViewImageColumn ONOFFy;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label GPRS_Pa;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbModules;
        private System.Windows.Forms.GroupBox gbxFFU;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TextBox txtFFU;
        private System.Windows.Forms.TrackBar FanBar;
        private System.Windows.Forms.Label lblFFUrpm;
    }
}