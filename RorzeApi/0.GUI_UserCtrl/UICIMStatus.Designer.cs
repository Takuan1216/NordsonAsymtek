namespace NewGem300Server_OOP.GUI
{
    partial class UICIMStatus
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UICIMStatus));
            this.lblONL = new System.Windows.Forms.Label();
            this.lblConn = new System.Windows.Forms.Label();
            this.labControlStats = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labCommStats = new System.Windows.Forms.Label();
            this.lblProcessStats = new System.Windows.Forms.Label();
            this.labProcessStat = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblONL
            // 
            this.lblONL.BackColor = System.Drawing.Color.Red;
            resources.ApplyResources(this.lblONL, "lblONL");
            this.lblONL.Name = "lblONL";
            // 
            // lblConn
            // 
            this.lblConn.BackColor = System.Drawing.Color.Red;
            resources.ApplyResources(this.lblConn, "lblConn");
            this.lblConn.Name = "lblConn";
            this.lblConn.Click += new System.EventHandler(this.lblConn_Click);
            // 
            // labControlStats
            // 
            this.labControlStats.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(this.labControlStats, "labControlStats");
            this.labControlStats.ForeColor = System.Drawing.Color.White;
            this.labControlStats.Name = "labControlStats";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.lblConn, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.labCommStats, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labControlStats, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblONL, 1, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // labCommStats
            // 
            this.labCommStats.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(this.labCommStats, "labCommStats");
            this.labCommStats.ForeColor = System.Drawing.Color.White;
            this.labCommStats.Name = "labCommStats";
            // 
            // lblProcessStats
            // 
            this.lblProcessStats.BackColor = System.Drawing.Color.Red;
            resources.ApplyResources(this.lblProcessStats, "lblProcessStats");
            this.lblProcessStats.Name = "lblProcessStats";
            // 
            // labProcessStat
            // 
            this.labProcessStat.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(this.labProcessStat, "labProcessStat");
            this.labProcessStat.ForeColor = System.Drawing.Color.White;
            this.labProcessStat.Name = "labProcessStat";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.lblProcessStats, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.labProcessStat, 0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // UICIMStatus
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.Silver;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Name = "UICIMStatus";
            this.Load += new System.EventHandler(this.UICIMStatus_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label lblONL;
        private System.Windows.Forms.Label lblConn;
        private System.Windows.Forms.Label labControlStats;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblProcessStats;
        private System.Windows.Forms.Label labCommStats;
        private System.Windows.Forms.Label labProcessStat;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
    }
}
