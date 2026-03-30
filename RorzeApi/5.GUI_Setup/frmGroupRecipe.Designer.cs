namespace RorzeApi
{
    partial class frmGroupRecipe
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmGroupRecipe));
            this.btnSave = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.cbxRecipeList = new System.Windows.Forms.ComboBox();
            this.btn = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tlpEQ4 = new System.Windows.Forms.TableLayoutPanel();
            this.lblEQ4Name = new System.Windows.Forms.Label();
            this.btnEQ4Recipe = new System.Windows.Forms.Button();
            this.lblEQ4Recipe = new System.Windows.Forms.Label();
            this.cbxEQ4Recipe = new System.Windows.Forms.ComboBox();
            this.tlpEQ3 = new System.Windows.Forms.TableLayoutPanel();
            this.lblEQ3Name = new System.Windows.Forms.Label();
            this.btnEQ3Recipe = new System.Windows.Forms.Button();
            this.lblEQ3Recipe = new System.Windows.Forms.Label();
            this.cbxEQ3Recipe = new System.Windows.Forms.ComboBox();
            this.tlpEQ2 = new System.Windows.Forms.TableLayoutPanel();
            this.lblEQ2Name = new System.Windows.Forms.Label();
            this.btnEQ2Recipe = new System.Windows.Forms.Button();
            this.lblEQ2Recipe = new System.Windows.Forms.Label();
            this.cbxEQ2Recipe = new System.Windows.Forms.ComboBox();
            this.tlpEQ1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblEQName = new System.Windows.Forms.Label();
            this.btnEQRecipe = new System.Windows.Forms.Button();
            this.lblEQRecipe = new System.Windows.Forms.Label();
            this.cbxEQRecipe = new System.Windows.Forms.ComboBox();
            this.tlpBack = new System.Windows.Forms.TableLayoutPanel();
            this.label7 = new System.Windows.Forms.Label();
            this.btnOCR_Back_Recip = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.cbxOCR_Back_Recip = new System.Windows.Forms.ComboBox();
            this.tlpFront = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.btnOCR_Front_Recipe = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.cbxOCR_Front_Recipe = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.lblLastModfiyUser = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblLastModifyDate = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btn.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tlpEQ4.SuspendLayout();
            this.tlpEQ3.SuspendLayout();
            this.tlpEQ2.SuspendLayout();
            this.tlpEQ1.SuspendLayout();
            this.tlpBack.SuspendLayout();
            this.tlpFront.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.groupBox2.SuspendLayout();
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
            // btnDelete
            // 
            this.btnDelete.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.btnDelete, "btnDelete");
            this.btnDelete.Image = global::RorzeApi.Properties.Resources._32_delete;
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // cbxRecipeList
            // 
            this.cbxRecipeList.FormattingEnabled = true;
            resources.ApplyResources(this.cbxRecipeList, "cbxRecipeList");
            this.cbxRecipeList.Name = "cbxRecipeList";
            this.cbxRecipeList.SelectedIndexChanged += new System.EventHandler(this.cbxRecipeList_SelectedIndexChanged);
            // 
            // btn
            // 
            this.btn.Controls.Add(this.tableLayoutPanel1);
            resources.ApplyResources(this.btn, "btn");
            this.btn.Name = "btn";
            this.btn.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.tlpEQ4, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.tlpEQ3, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.tlpEQ2, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.tlpEQ1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tlpBack, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tlpFront, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // tlpEQ4
            // 
            resources.ApplyResources(this.tlpEQ4, "tlpEQ4");
            this.tlpEQ4.Controls.Add(this.lblEQ4Name, 0, 0);
            this.tlpEQ4.Controls.Add(this.btnEQ4Recipe, 1, 0);
            this.tlpEQ4.Controls.Add(this.lblEQ4Recipe, 2, 0);
            this.tlpEQ4.Controls.Add(this.cbxEQ4Recipe, 3, 0);
            this.tlpEQ4.Name = "tlpEQ4";
            // 
            // lblEQ4Name
            // 
            resources.ApplyResources(this.lblEQ4Name, "lblEQ4Name");
            this.lblEQ4Name.Name = "lblEQ4Name";
            // 
            // btnEQ4Recipe
            // 
            this.btnEQ4Recipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.btnEQ4Recipe, "btnEQ4Recipe");
            this.btnEQ4Recipe.Name = "btnEQ4Recipe";
            this.btnEQ4Recipe.UseVisualStyleBackColor = false;
            this.btnEQ4Recipe.Click += new System.EventHandler(this.btnText);
            // 
            // lblEQ4Recipe
            // 
            resources.ApplyResources(this.lblEQ4Recipe, "lblEQ4Recipe");
            this.lblEQ4Recipe.Name = "lblEQ4Recipe";
            // 
            // cbxEQ4Recipe
            // 
            resources.ApplyResources(this.cbxEQ4Recipe, "cbxEQ4Recipe");
            this.tlpEQ4.SetColumnSpan(this.cbxEQ4Recipe, 2);
            this.cbxEQ4Recipe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxEQ4Recipe.FormattingEnabled = true;
            this.cbxEQ4Recipe.Name = "cbxEQ4Recipe";
            // 
            // tlpEQ3
            // 
            resources.ApplyResources(this.tlpEQ3, "tlpEQ3");
            this.tlpEQ3.Controls.Add(this.lblEQ3Name, 0, 0);
            this.tlpEQ3.Controls.Add(this.btnEQ3Recipe, 1, 0);
            this.tlpEQ3.Controls.Add(this.lblEQ3Recipe, 2, 0);
            this.tlpEQ3.Controls.Add(this.cbxEQ3Recipe, 3, 0);
            this.tlpEQ3.Name = "tlpEQ3";
            // 
            // lblEQ3Name
            // 
            resources.ApplyResources(this.lblEQ3Name, "lblEQ3Name");
            this.lblEQ3Name.Name = "lblEQ3Name";
            // 
            // btnEQ3Recipe
            // 
            this.btnEQ3Recipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.btnEQ3Recipe, "btnEQ3Recipe");
            this.btnEQ3Recipe.Name = "btnEQ3Recipe";
            this.btnEQ3Recipe.UseVisualStyleBackColor = false;
            this.btnEQ3Recipe.Click += new System.EventHandler(this.btnText);
            // 
            // lblEQ3Recipe
            // 
            resources.ApplyResources(this.lblEQ3Recipe, "lblEQ3Recipe");
            this.lblEQ3Recipe.Name = "lblEQ3Recipe";
            // 
            // cbxEQ3Recipe
            // 
            resources.ApplyResources(this.cbxEQ3Recipe, "cbxEQ3Recipe");
            this.tlpEQ3.SetColumnSpan(this.cbxEQ3Recipe, 2);
            this.cbxEQ3Recipe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxEQ3Recipe.FormattingEnabled = true;
            this.cbxEQ3Recipe.Name = "cbxEQ3Recipe";
            // 
            // tlpEQ2
            // 
            resources.ApplyResources(this.tlpEQ2, "tlpEQ2");
            this.tlpEQ2.Controls.Add(this.lblEQ2Name, 0, 0);
            this.tlpEQ2.Controls.Add(this.btnEQ2Recipe, 1, 0);
            this.tlpEQ2.Controls.Add(this.lblEQ2Recipe, 2, 0);
            this.tlpEQ2.Controls.Add(this.cbxEQ2Recipe, 3, 0);
            this.tlpEQ2.Name = "tlpEQ2";
            // 
            // lblEQ2Name
            // 
            resources.ApplyResources(this.lblEQ2Name, "lblEQ2Name");
            this.lblEQ2Name.Name = "lblEQ2Name";
            // 
            // btnEQ2Recipe
            // 
            this.btnEQ2Recipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.btnEQ2Recipe, "btnEQ2Recipe");
            this.btnEQ2Recipe.Name = "btnEQ2Recipe";
            this.btnEQ2Recipe.UseVisualStyleBackColor = false;
            this.btnEQ2Recipe.Click += new System.EventHandler(this.btnText);
            // 
            // lblEQ2Recipe
            // 
            resources.ApplyResources(this.lblEQ2Recipe, "lblEQ2Recipe");
            this.lblEQ2Recipe.Name = "lblEQ2Recipe";
            // 
            // cbxEQ2Recipe
            // 
            resources.ApplyResources(this.cbxEQ2Recipe, "cbxEQ2Recipe");
            this.tlpEQ2.SetColumnSpan(this.cbxEQ2Recipe, 2);
            this.cbxEQ2Recipe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxEQ2Recipe.FormattingEnabled = true;
            this.cbxEQ2Recipe.Name = "cbxEQ2Recipe";
            // 
            // tlpEQ1
            // 
            resources.ApplyResources(this.tlpEQ1, "tlpEQ1");
            this.tlpEQ1.Controls.Add(this.lblEQName, 0, 0);
            this.tlpEQ1.Controls.Add(this.btnEQRecipe, 1, 0);
            this.tlpEQ1.Controls.Add(this.lblEQRecipe, 2, 0);
            this.tlpEQ1.Controls.Add(this.cbxEQRecipe, 3, 0);
            this.tlpEQ1.Name = "tlpEQ1";
            // 
            // lblEQName
            // 
            resources.ApplyResources(this.lblEQName, "lblEQName");
            this.lblEQName.Name = "lblEQName";
            // 
            // btnEQRecipe
            // 
            this.btnEQRecipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.btnEQRecipe, "btnEQRecipe");
            this.btnEQRecipe.Name = "btnEQRecipe";
            this.btnEQRecipe.UseVisualStyleBackColor = false;
            this.btnEQRecipe.Click += new System.EventHandler(this.btnText);
            // 
            // lblEQRecipe
            // 
            resources.ApplyResources(this.lblEQRecipe, "lblEQRecipe");
            this.lblEQRecipe.Name = "lblEQRecipe";
            // 
            // cbxEQRecipe
            // 
            resources.ApplyResources(this.cbxEQRecipe, "cbxEQRecipe");
            this.tlpEQ1.SetColumnSpan(this.cbxEQRecipe, 2);
            this.cbxEQRecipe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxEQRecipe.FormattingEnabled = true;
            this.cbxEQRecipe.Name = "cbxEQRecipe";
            // 
            // tlpBack
            // 
            resources.ApplyResources(this.tlpBack, "tlpBack");
            this.tlpBack.Controls.Add(this.label7, 0, 0);
            this.tlpBack.Controls.Add(this.btnOCR_Back_Recip, 1, 0);
            this.tlpBack.Controls.Add(this.label9, 2, 0);
            this.tlpBack.Controls.Add(this.cbxOCR_Back_Recip, 3, 0);
            this.tlpBack.Name = "tlpBack";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // btnOCR_Back_Recip
            // 
            this.btnOCR_Back_Recip.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.btnOCR_Back_Recip, "btnOCR_Back_Recip");
            this.btnOCR_Back_Recip.Name = "btnOCR_Back_Recip";
            this.btnOCR_Back_Recip.UseVisualStyleBackColor = false;
            this.btnOCR_Back_Recip.Click += new System.EventHandler(this.btnText);
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // cbxOCR_Back_Recip
            // 
            resources.ApplyResources(this.cbxOCR_Back_Recip, "cbxOCR_Back_Recip");
            this.tlpBack.SetColumnSpan(this.cbxOCR_Back_Recip, 2);
            this.cbxOCR_Back_Recip.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxOCR_Back_Recip.FormattingEnabled = true;
            this.cbxOCR_Back_Recip.Name = "cbxOCR_Back_Recip";
            // 
            // tlpFront
            // 
            resources.ApplyResources(this.tlpFront, "tlpFront");
            this.tlpFront.Controls.Add(this.label4, 0, 0);
            this.tlpFront.Controls.Add(this.btnOCR_Front_Recipe, 1, 0);
            this.tlpFront.Controls.Add(this.label8, 2, 0);
            this.tlpFront.Controls.Add(this.cbxOCR_Front_Recipe, 3, 0);
            this.tlpFront.Name = "tlpFront";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // btnOCR_Front_Recipe
            // 
            this.btnOCR_Front_Recipe.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.btnOCR_Front_Recipe, "btnOCR_Front_Recipe");
            this.btnOCR_Front_Recipe.Name = "btnOCR_Front_Recipe";
            this.btnOCR_Front_Recipe.Tag = "1";
            this.btnOCR_Front_Recipe.UseVisualStyleBackColor = false;
            this.btnOCR_Front_Recipe.Click += new System.EventHandler(this.btnText);
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // cbxOCR_Front_Recipe
            // 
            resources.ApplyResources(this.cbxOCR_Front_Recipe, "cbxOCR_Front_Recipe");
            this.tlpFront.SetColumnSpan(this.cbxOCR_Front_Recipe, 2);
            this.cbxOCR_Front_Recipe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxOCR_Front_Recipe.FormattingEnabled = true;
            this.cbxOCR_Front_Recipe.Name = "cbxOCR_Front_Recipe";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel2);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.lblLastModfiyUser, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label5, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblLastModifyDate, 1, 1);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // lblLastModfiyUser
            // 
            resources.ApplyResources(this.lblLastModfiyUser, "lblLastModfiyUser");
            this.lblLastModfiyUser.Name = "lblLastModfiyUser";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // lblLastModifyDate
            // 
            resources.ApplyResources(this.lblLastModifyDate, "lblLastModifyDate");
            this.lblLastModifyDate.Name = "lblLastModifyDate";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.groupBox1);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.btn);
            this.groupBox2.Controls.Add(this.cbxRecipeList);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnDelete);
            this.panel2.Controls.Add(this.btnSave);
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // frmGroupRecipe
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.panel2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmGroupRecipe";
            this.VisibleChanged += new System.EventHandler(this.frmGroupRecipe_VisibleChanged);
            this.btn.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tlpEQ4.ResumeLayout(false);
            this.tlpEQ4.PerformLayout();
            this.tlpEQ3.ResumeLayout(false);
            this.tlpEQ3.PerformLayout();
            this.tlpEQ2.ResumeLayout(false);
            this.tlpEQ2.PerformLayout();
            this.tlpEQ1.ResumeLayout(false);
            this.tlpEQ1.PerformLayout();
            this.tlpBack.ResumeLayout(false);
            this.tlpBack.PerformLayout();
            this.tlpFront.ResumeLayout(false);
            this.tlpFront.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.ComboBox cbxRecipeList;
        private System.Windows.Forms.GroupBox btn;
        private System.Windows.Forms.Button btnOCR_Back_Recip;
        private System.Windows.Forms.Button btnEQRecipe;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cbxOCR_Back_Recip;
        private System.Windows.Forms.ComboBox cbxEQRecipe;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblEQRecipe;
        private System.Windows.Forms.Label lblEQName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblLastModifyDate;
        private System.Windows.Forms.Label lblLastModfiyUser;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TableLayoutPanel tlpEQ1;
        private System.Windows.Forms.TableLayoutPanel tlpBack;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tlpFront;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnOCR_Front_Recipe;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cbxOCR_Front_Recipe;
        private System.Windows.Forms.TableLayoutPanel tlpEQ3;
        private System.Windows.Forms.Label lblEQ3Name;
        private System.Windows.Forms.Button btnEQ3Recipe;
        private System.Windows.Forms.Label lblEQ3Recipe;
        private System.Windows.Forms.ComboBox cbxEQ3Recipe;
        private System.Windows.Forms.TableLayoutPanel tlpEQ2;
        private System.Windows.Forms.Label lblEQ2Name;
        private System.Windows.Forms.Button btnEQ2Recipe;
        private System.Windows.Forms.Label lblEQ2Recipe;
        private System.Windows.Forms.ComboBox cbxEQ2Recipe;
        private System.Windows.Forms.TableLayoutPanel tlpEQ4;
        private System.Windows.Forms.Label lblEQ4Name;
        private System.Windows.Forms.Button btnEQ4Recipe;
        private System.Windows.Forms.Label lblEQ4Recipe;
        private System.Windows.Forms.ComboBox cbxEQ4Recipe;
        private System.Windows.Forms.Button button1;
    }
}