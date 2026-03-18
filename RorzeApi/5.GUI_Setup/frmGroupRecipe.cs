using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RorzeApi.Class;
using RorzeUnit.Class.EQ;

namespace RorzeApi
{
    public partial class frmGroupRecipe : Form
    {
        float frmX;//當前窗體的寬度
        float frmY;//當前窗體的高度
        bool isLoaded = false;

        SGroupRecipeManager _grouprecipe;
        List<SSEquipment> m_listEQM;
        SPermission _user;
        public frmGroupRecipe(SGroupRecipeManager grouprecipe, SPermission User, List<SSEquipment> listEQM)
        {
            InitializeComponent();
            _grouprecipe = grouprecipe;
            m_listEQM = listEQM;
            _user = User;


            /*
            TableLayoutPanel[] tlpEQ = new TableLayoutPanel[] { tlpEQ1, tlpEQ2, tlpEQ3, tlpEQ4 };
            Label[] lblEQ = new Label[] { lblEQ1Name, lblEQ2Name, lblEQ3Name, lblEQ4Name };
            for (int i = 0; i < m_listEQM.Count; i++)
            {
                SSEquipment equipment = m_listEQM[i];
                tlpEQ[i].Visible = equipment != null && equipment.Disable == false;

                lblEQ[i].Text = (equipment != null && equipment.Disable == false) ? equipment._Name : "Disable";
            }*/

            TableLayoutPanel tlpEQ = tlpEQ1;
            Label lblEQ = lblEQ1Name;
            tlpEQ.Visible = false;
            lblEQ.Text = "disable";
            for (int i = 0; i < m_listEQM.Count; i++)
            {
                SSEquipment equipment = m_listEQM[i];
                if (equipment != null && equipment.Disable == false)
                    tlpEQ.Visible = true;

                if(equipment != null && equipment.Disable == false)
                    lblEQ.Text = "EQ" ;
            }


            //沒提供此功能           
            tlpFront.Enabled = GParam.theInst.IsUnitDisable(enumUnit.OCRA1) == false || GParam.theInst.IsUnitDisable(enumUnit.OCRB1) == false;
            //沒提供此功能
            tlpBack.Enabled = GParam.theInst.IsUnitDisable(enumUnit.OCRA2) == false || GParam.theInst.IsUnitDisable(enumUnit.OCRB2) == false;

            if (GParam.theInst.FreeStyle)
            {
                btnSave.Image = Properties.Resources._32_save_;
                btnDelete.Image = Properties.Resources._32_delete_;
            }
        }

        #region Form Zoom
        public void SetGUISize(float frmWidth, float frmHeight)
        {
            if (isLoaded == false)
            {
                frmX = this.Width;  //獲取窗體的寬度
                frmY = this.Height; //獲取窗體的高度      
                isLoaded = true;    // 已設定各控制項的尺寸到Tag屬性中
                SetTag(this);       //調用方法
            }
            float tempX = frmWidth / frmX;  //計算比例
            float tempY = frmHeight / frmY; //計算比例
            SetControls(tempX, tempY, this);
        }
        private void SetTag(Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                con.Tag = con.Width + ":" + con.Height + ":" + con.Left + ":" + con.Top + ":" + con.Font.Size;
                if (con.Controls.Count > 0)
                    SetTag(con);
            }
        }
        private void SetControls(float newx, float newy, Control cons)
        {
            //遍歷窗體中的控制項，重新設置控制項的值
            foreach (Control con in cons.Controls)
            {
                string[] mytag = con.Tag.ToString().Split(new char[] { ':' });//獲取控制項的Tag屬性值，並分割後存儲字元串數組
                float a = System.Convert.ToSingle(mytag[0]) * newx;//根據窗體縮放比例確定控制項的值，寬度
                con.Width = (int)a;//寬度
                a = System.Convert.ToSingle(mytag[1]) * newy;//高度
                con.Height = (int)(a);
                a = System.Convert.ToSingle(mytag[2]) * newx;//左邊距離
                con.Left = (int)(a);
                a = System.Convert.ToSingle(mytag[3]) * newy;//上邊緣距離
                con.Top = (int)(a);
                Single currentSize = System.Convert.ToSingle(mytag[4]) * newy;//字體大小
                con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                if (con.Controls.Count > 0)
                {
                    SetControls(newx, newy, con);
                }
            }
        }
        #endregion

        private void cbxRecipeList_SelectedIndexChanged(object sender, EventArgs e)
        {

            bool findM12 = false;
            bool findT7 = false;
            string ErrorStr = string.Empty;
            frmMessageBox frm;
            // cbxRecipeList.Text = "";
            cbxEQ1Recipe.Text = "";
            cbxOCR_Front_Recipe.Text = "";
            cbxOCR_Back_Recip.Text = "";
            btnEQ1Recipe.BackColor = Color.White;
            btnOCR_Front_Recipe.BackColor = Color.White;
            btnOCR_Back_Recip.BackColor = Color.White;
            cbxEQ1Recipe.Enabled = false;
            cbxOCR_Front_Recipe.Enabled = false;
            cbxOCR_Back_Recip.Enabled = false;

            lblLastModfiyUser.Text = "";
            lblLastModifyDate.Text = "";
            if (_grouprecipe.GetRecipeGroupList.ContainsKey(cbxRecipeList.Text))
            {
                bool[] listEQEnable = _grouprecipe.GetRecipeGroupList[cbxRecipeList.Text].GetEQ_ProcessEnable();
                string[] listEQRecipe = _grouprecipe.GetRecipeGroupList[cbxRecipeList.Text].GetEQ_Recipe();

                Label[] lblEQName = new Label[] { lblEQ1Name, lblEQ2Name, lblEQ3Name, lblEQ4Name };
                Button[] btnEQrecipe = new Button[] { btnEQ1Recipe, btnEQ2Recipe, btnEQ3Recipe, btnEQ4Recipe };
                ComboBox[] cbxEQrecipe = new ComboBox[] { cbxEQ1Recipe, cbxEQ2Recipe, cbxEQ3Recipe, cbxEQ4Recipe };
                bool[] bFindEQrecipe = new bool[] { false, false, false, false };
                for (int i = 0; i < listEQRecipe.Length; i++)
                {
                    SSEquipment equipment = m_listEQM[i];

                    if (equipment == null || equipment.Disable)
                    {
                        continue;
                    }

                    if (listEQEnable != null && listEQEnable.Length > i)
                        btnEQrecipe[i].BackColor = listEQEnable[i] ? Color.LightBlue : Color.White;


                    foreach (string item in equipment.RecipeList())
                    {
                        if (item == listEQRecipe[0])//找對應清單內哪一個
                        {

                            cbxEQrecipe[i].Enabled = true;
                            cbxEQrecipe[i].Text = item;
                            bFindEQrecipe[i] = true;
                            break;
                        }
                    }
                }

                if (_grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._M12 != "")
                {
                    for (int i = 0; i < GParam.theInst.GetOCRRecipeIniFile(true).Count; i++)
                    {
                        if (_grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._M12 == GParam.theInst.GetOCRRecipeIniFile(true)[i].Name)
                        {
                            btnOCR_Front_Recipe.BackColor = Color.LightBlue;
                            cbxOCR_Front_Recipe.Enabled = true;
                            cbxOCR_Front_Recipe.Text = _grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._M12;
                            findM12 = true;
                            break;
                        }

                    }
                }
                else
                    findM12 = true;

                if (_grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._T7 != "")
                {
                    for (int i = 0; i < GParam.theInst.GetOCRRecipeIniFile(false).Count; i++)
                    {
                        if (_grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._T7 == GParam.theInst.GetOCRRecipeIniFile(false)[i].Name)
                        {
                            btnOCR_Back_Recip.BackColor = Color.LightBlue;
                            cbxOCR_Back_Recip.Enabled = true;
                            cbxOCR_Back_Recip.Text = _grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._T7;
                            findT7 = true;
                            break;
                        }
                    }
                }
                else
                    findT7 = true;

                #region 檢查
                foreach (SSEquipment item in m_listEQM)
                {
                    if (item == null || item.Disable || item.RecipeList().Count == 0)
                        continue;
                    int nIndex = item._BodyNo - 1;

                    if (!bFindEQrecipe[nIndex])
                        ErrorStr += item._Name + ",";
                }

                if (!findM12)
                    ErrorStr += _grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._M12 + ",";
                if (!findT7)
                    ErrorStr += _grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._T7 + ",";

                if (ErrorStr != string.Empty)
                {
                    frm = new frmMessageBox(string.Format("Not find sub recipe, {0} Please Check it ", ErrorStr), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    frm.ShowDialog();
                }
                #endregion

                lblLastModfiyUser.Text = _grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._HistoryUser;
                lblLastModifyDate.Text = _grouprecipe.GetRecipeGroupList[cbxRecipeList.Text]._HistoryTime.ToString("yyyy/MM/dd HH:mm:ss");
            }
        }


        private void btnText(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            btn.BackColor = btn.BackColor == Color.White ? Color.LightBlue : Color.White;

            if (btn == btnEQ1Recipe)
            {
                cbxEQ1Recipe.Enabled = (btn.BackColor == Color.White) ? false : true;
                cbxEQ1Recipe.SelectedIndex = -1;
            }
            if (btn == btnEQ2Recipe)
            {
                cbxEQ2Recipe.Enabled = (btn.BackColor == Color.White) ? false : true;
                cbxEQ2Recipe.SelectedIndex = -1;
            }
            if (btn == btnEQ3Recipe)
            {
                cbxEQ3Recipe.Enabled = (btn.BackColor == Color.White) ? false : true;
                cbxEQ3Recipe.SelectedIndex = -1;
            }
            if (btn == btnEQ4Recipe)
            {
                cbxEQ4Recipe.Enabled = (btn.BackColor == Color.White) ? false : true;
                cbxEQ4Recipe.SelectedIndex = -1;
            }
            else if (btn == btnOCR_Front_Recipe)
            {
                cbxOCR_Front_Recipe.Enabled = (btn.BackColor == Color.White) ? false : true;
                cbxOCR_Front_Recipe.SelectedIndex = -1;
            }
            else if (btn == btnOCR_Back_Recip)
            {
                cbxOCR_Back_Recip.Enabled = (btn.BackColor == Color.White) ? false : true;
                cbxOCR_Back_Recip.SelectedIndex = -1;
            }
        }

        private void frmGroupRecipe_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                cbxRecipeList.Items.Clear();
                cbxEQ1Recipe.Items.Clear();
                cbxOCR_Front_Recipe.Items.Clear();
                cbxOCR_Back_Recip.Items.Clear();

                foreach (string RecipeGroup in _grouprecipe.GetRecipeGroupList.Keys)
                    cbxRecipeList.Items.Add(RecipeGroup);

                /*ComboBox[] cbxEQ = new ComboBox[] { cbxEQ1Recipe, cbxEQ2Recipe, cbxEQ3Recipe, cbxEQ4Recipe };
                Label[] lblEQrecipe = new Label[] { lblEQ1Recipe, lblEQ2Recipe, lblEQ3Recipe, lblEQ4Recipe };
                Button[] btnEQrecipe = new Button[] { btnEQ1Recipe, btnEQ2Recipe, btnEQ3Recipe, btnEQ4Recipe };
                for (int i = 0; i < m_listEQM.Count; i++)
                {
                    SSEquipment equipment = m_listEQM[i];
                    if (equipment == null || equipment.Disable || equipment.RecipeList().Count == 0)
                    {
                        cbxEQ[i].Visible = false;
                        lblEQrecipe[i].Text = "NoRecipe";

                    }
                    else
                    {
                        foreach (string EQRecipe in equipment.RecipeList())
                            cbxEQ[i].Items.Add(EQRecipe);
                    }
                }*/

                ComboBox cbxEQ = cbxEQ1Recipe;
                Label lblEQrecipe = lblEQ1Recipe; 
                Button btnEQrecipe = btnEQ1Recipe;
                cbxEQ.Visible = false;
                lblEQrecipe.Text = "NoRecipe";
                for (int i = 0; i < m_listEQM.Count; i++)
                {
                    SSEquipment equipment = m_listEQM[i];
                    
                    if (equipment == null || equipment.Disable || equipment.RecipeList().Count == 0)
                    {
                        
                    }
                    else
                    {
                        foreach (string EQRecipe in equipment.RecipeList())
                            if (!cbxEQ.Items.Contains(EQRecipe))
                            {
                                cbxEQ.Visible = true;
                                lblEQrecipe.Text = "Recipe";
                                cbxEQ.Items.Add(EQRecipe);
                            }
                    }
                }

                for (int i = 0; i < GParam.theInst.GetOCRRecipeIniFile(true).Count; i++)
                {
                    if (GParam.theInst.GetOCRRecipeIniFile(true)[i].Stored == 1)
                        cbxOCR_Front_Recipe.Items.Add(GParam.theInst.GetOCRRecipeIniFile(true)[i].Name);
                }
                for (int i = 0; i < GParam.theInst.GetOCRRecipeIniFile(false).Count; i++)
                {
                    if (GParam.theInst.GetOCRRecipeIniFile(false)[i].Stored == 1)
                        cbxOCR_Back_Recip.Items.Add(GParam.theInst.GetOCRRecipeIniFile(false)[i].Name);
                }

                cbxRecipeList.Text = "";
                cbxEQ1Recipe.Text = "";
                cbxOCR_Front_Recipe.Text = "";
                cbxOCR_Back_Recip.Text = "";
                btnEQ1Recipe.BackColor = Color.White;
                btnOCR_Front_Recipe.BackColor = Color.White;
                btnOCR_Back_Recip.BackColor = Color.White;
                cbxEQ1Recipe.Enabled = false;
                cbxOCR_Front_Recipe.Enabled = false;
                cbxOCR_Back_Recip.Enabled = false;


                lblLastModfiyUser.Text = "";
                lblLastModifyDate.Text = "";
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            frmMessageBox frm;

            if (cbxRecipeList.Text == "")
            {
                frm = new frmMessageBox(string.Format("Need selete Group Recipe , Please check it"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            if (btnEQ1Recipe.BackColor == Color.LightBlue
                && cbxEQ1Recipe.Visible && cbxEQ1Recipe.Text == "")
            {
                frm = new frmMessageBox(string.Format("Need selete EQ1 recipe , Please check it"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            if (btnEQ2Recipe.BackColor == Color.LightBlue
                && cbxEQ2Recipe.Visible && cbxEQ2Recipe.Text == "")
            {
                frm = new frmMessageBox(string.Format("Need selete EQ2 recipe , Please check it"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            if (btnEQ3Recipe.BackColor == Color.LightBlue
                && cbxEQ3Recipe.Visible && cbxEQ3Recipe.Text == "")
            {
                frm = new frmMessageBox(string.Format("Need selete EQ3 recipe , Please check it"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            if (btnEQ4Recipe.BackColor == Color.LightBlue
                && cbxEQ4Recipe.Visible && cbxEQ4Recipe.Text == "")
            {
                frm = new frmMessageBox(string.Format("Need selete EQ4 recipe , Please check it"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            if (btnOCR_Front_Recipe.BackColor == Color.LightBlue &&
                cbxOCR_Front_Recipe.Text == "")
            {
                frm = new frmMessageBox(string.Format("M12 Recipe not select  , Please check it"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            if (btnOCR_Back_Recip.BackColor == Color.LightBlue
               && cbxOCR_Back_Recip.Text == "")
            {
                frm = new frmMessageBox(string.Format("T7 Recipe not select , Please check it"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            if (_grouprecipe.GetRecipeGroupList.ContainsKey(cbxRecipeList.Text)) // modify 
            {
                if (new frmMessageBox(string.Format("Do you want to modify {0} Group Recipe", cbxRecipeList.Text), "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    List<bool> listEQ_Enable = new List<bool> { btnEQ1Recipe.BackColor == Color.LightBlue, btnEQ2Recipe.BackColor == Color.LightBlue, btnEQ3Recipe.BackColor == Color.LightBlue, btnEQ4Recipe.BackColor == Color.LightBlue };
                    List<string> listEQ_Recipe = new List<string>() { cbxEQ1Recipe.Text, cbxEQ2Recipe.Text, cbxEQ3Recipe.Text, cbxEQ4Recipe.Text };
                    _grouprecipe.ModifyRecipe(cbxRecipeList.Text, listEQ_Enable, listEQ_Recipe, (btnOCR_Front_Recipe.BackColor == Color.LightBlue) ? cbxOCR_Front_Recipe.Text : "", (btnOCR_Back_Recip.BackColor == Color.LightBlue) ? cbxOCR_Back_Recip.Text : "", _user.UserID);
                }

            }
            else // create 
            {
                if (new frmMessageBox(string.Format("Do you want to create {0} Group Recipe", cbxRecipeList.Text), "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
                {
                    List<bool> listEQ_Enable = new List<bool> { btnEQ1Recipe.BackColor == Color.LightBlue, btnEQ2Recipe.BackColor == Color.LightBlue, btnEQ3Recipe.BackColor == Color.LightBlue, btnEQ4Recipe.BackColor == Color.LightBlue };
                    List<string> listEQ_Recipe = new List<string>() { cbxEQ1Recipe.Text, cbxEQ2Recipe.Text, cbxEQ3Recipe.Text, cbxEQ4Recipe.Text };

                    _grouprecipe.ModifyRecipe(cbxRecipeList.Text, listEQ_Enable, listEQ_Recipe, (btnOCR_Front_Recipe.BackColor == Color.LightBlue) ? cbxOCR_Front_Recipe.Text : "", (btnOCR_Back_Recip.BackColor == Color.LightBlue) ? cbxOCR_Back_Recip.Text : "", _user.UserID);
                    cbxRecipeList.Items.Clear();
                    foreach (string RecipeGroup in _grouprecipe.GetRecipeGroupList.Keys)
                        cbxRecipeList.Items.Add(RecipeGroup);

                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            frmMessageBox frm;
            if (cbxRecipeList.Text == "")
            {
                frm = new frmMessageBox(string.Format("Need selete Group Recipe , Please check it"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }
            if (new frmMessageBox(string.Format("Do you want to Delete {0} Group Recipe", cbxRecipeList.Text), "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == DialogResult.Yes)
            {
                _grouprecipe.DeleteRecipe(cbxRecipeList.Text);
                cbxRecipeList.Items.Clear();
                foreach (string RecipeGroup in _grouprecipe.GetRecipeGroupList.Keys)
                    cbxRecipeList.Items.Add(RecipeGroup);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            m_listEQM[0].GetRecipeListW();
        }
    }
}
