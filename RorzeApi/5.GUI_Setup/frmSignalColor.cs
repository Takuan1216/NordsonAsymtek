using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using RorzeApi.Class;
using RorzeUnit.Class;
using RorzeApi.SECSGEM;
using System.Reflection;
using System.Collections.Generic;

namespace RorzeApi
{
    public partial class frmSignalColor : Form
    {


        private float X;//當前窗體的寬度
        private float Y;//當前窗體的高度

        private float frmX;//當前窗體的寬度
        private float frmY;//當前窗體的高度

        bool isLoaded;  // 是否已設定各控制的尺寸資料到Tag屬性

        private SPermission _permission;
        private SMainDB _dbMain;
        SGEM300 _Gem;

        enumSignalTowerColor m_eSelectColor = enumSignalTowerColor.None;

        Dictionary<Panel, enumSignalTowerColor> m_dicPanelColor = new Dictionary<Panel, enumSignalTowerColor>();
        Dictionary<Button, enumSignalTowerColor> m_dicButtonColor = new Dictionary<Button, enumSignalTowerColor>();

    
        public frmSignalColor(SPermission permission, SMainDB db, SGEM300 GEM)
        {
            InitializeComponent();

            PropertyInfo info = this.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            info.SetValue(tableLayoutPanel1, true, null);//雙緩衝減少form閃爍

            X = this.Width;//獲取窗體的寬度
            Y = this.Height;//獲取窗體的高度

            frmX = this.Width;
            frmY = this.Height;

            isLoaded = false;

            _dbMain = db;
            _permission = permission;
            _Gem = GEM;

            if (GParam.theInst.FreeStyle)
            {
                btnSave.Image = Properties.Resources._32_save_;
                btnTitle.BackColor = btnColor.BackColor = btnSelectColor.BackColor = GParam.theInst.ColorTitle; 
                btnTitle.ForeColor = btnColor.ForeColor = btnSelectColor.ForeColor = Color.Black;
            }

        }

        #region 畫面縮放
        public void SetGUISize(float frmWidth, float frmHeight)
        {
            frmX = frmWidth;
            frmY = frmHeight;
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
            if (isLoaded)
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
        }
        #endregion

        private void frmPermissionUser_Load(object sender, EventArgs e)
        {
            isLoaded = true;// 已設定各控制項的尺寸到Tag屬性中
            SetTag(this);//調用方法

            float tempX = frmX / X;
            float tempY = frmY / Y;
            SetControls(tempX, tempY, this);
            //-------------------------------------------------------------------
            m_dicPanelColor[pnlRed] = enumSignalTowerColor.Red;
            m_dicPanelColor[pnlRedBlinking] = enumSignalTowerColor.RedBlinking;
            m_dicPanelColor[pnlYellow] = enumSignalTowerColor.Yellow;
            m_dicPanelColor[pnlYellowBlinking] = enumSignalTowerColor.YellowBlinking;
            m_dicPanelColor[pnlGreen] = enumSignalTowerColor.Green;
            m_dicPanelColor[pnlGreenBlinking] = enumSignalTowerColor.GreenBlinking;
            m_dicPanelColor[pnlBlue] = enumSignalTowerColor.Blue;
            m_dicPanelColor[pnlBlueBlinking] = enumSignalTowerColor.BlueBlinking;
            m_dicPanelColor[pnlNoneColor] = enumSignalTowerColor.None;
            //-------------------------------------------------------------------
            m_dicButtonColor[btnLight_Err] = enumSignalTowerColor.None;
            m_dicButtonColor[btnLight_Maint] = enumSignalTowerColor.None;
            m_dicButtonColor[btnLight_LUReq] = enumSignalTowerColor.None;
            m_dicButtonColor[btnLight_Operation] = enumSignalTowerColor.None;
            m_dicButtonColor[btnLight_Idle] = enumSignalTowerColor.None;
            m_dicButtonColor[btnLight_Process] = enumSignalTowerColor.None;
            m_dicButtonColor[btnLight_OnlineLocal] = enumSignalTowerColor.None;
            m_dicButtonColor[btnLight_OnlineRemote] = enumSignalTowerColor.None;
            m_dicButtonColor[btnLight_Offline] = enumSignalTowerColor.None;

        }

        //========= 
        private void cboUserID_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void ckbShowPwd_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox ckbGet = sender as CheckBox;

        }
        //========= 
        private void btnSave_Click(object sender, EventArgs e)
        {
            frmMessageBox frm;

            if (_Gem != null && _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                frm = new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtErrorOccurring, m_dicButtonColor[btnLight_Err]);
            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtMaintenance, m_dicButtonColor[btnLight_Maint]);
            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtLoadUnLoadRequest, m_dicButtonColor[btnLight_LUReq]);
            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtOperator, m_dicButtonColor[btnLight_Operation]);
            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtIdle, m_dicButtonColor[btnLight_Idle]);
            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtProcessing, m_dicButtonColor[btnLight_Process]);
            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtOnlineLocal, m_dicButtonColor[btnLight_OnlineLocal]);
            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtOnlineRemote, m_dicButtonColor[btnLight_OnlineRemote]);
            GParam.theInst.SetSignalTowerColor(enumSignalTowerColorSetting.AtOffline, m_dicButtonColor[btnLight_Offline]);
        }


        //========= 
        private bool CheckData()
        {
            string strNG = string.Empty;

            //if (cboGroup.Text.Length <= 0) strNG = "Group ID";

            if (strNG.Length > 0)
            {
                frmMessageBox frmMbox = new frmMessageBox(string.Format("Please assign {0}. it cannot be empty!", strNG), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frmMbox.ShowDialog();
                return false;
            }
            else
                return true;
        }
        private void InitialForm()
        {

            //  目前使用者
            string strUser = _permission.UserID;
            //  目前使用者等級
            int nNowLevel = _permission.Level;
            //  目前使用者DataSet
            DataSet dtNowUser = _dbMain.Reader(string.Format("Select * from UserLogin where UserName = '{0}'", strUser));
            if (dtNowUser.Tables[0].Rows.Count > 0)
            {

                //  判斷比自己階級低的加入選擇使用者

                foreach (string userName in _permission.GetPermissionUser())
                {
                    DataSet dt = _dbMain.Reader(string.Format("Select * from UserLogin where UserName = '{0}'", userName));

                    if (dt.Tables[0].Rows.Count > 0)
                    {
                        int nLevel = (int)dt.Tables[0].Rows[0]["UserLevel"];

                    }
                }

            }
        }
        private void frmPermissionUser_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                m_dicButtonColor[btnLight_Err] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtErrorOccurring);
                m_dicButtonColor[btnLight_Maint] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtMaintenance);
                m_dicButtonColor[btnLight_LUReq] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtLoadUnLoadRequest);
                m_dicButtonColor[btnLight_Operation] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOperator);
                m_dicButtonColor[btnLight_Idle] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtIdle);
                m_dicButtonColor[btnLight_Process] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtProcessing);
                m_dicButtonColor[btnLight_OnlineLocal] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOnlineLocal);
                m_dicButtonColor[btnLight_OnlineRemote] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOnlineRemote);
                m_dicButtonColor[btnLight_Offline] = GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOffline);
            }
            else
            {

            }

            timer1.Enabled = this.Visible;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {

            pnlRedBlinking.BackColor = pnlRedBlinking.BackColor == Color.Red ? SystemColors.Control : Color.Red;
            pnlYellowBlinking.BackColor = pnlYellowBlinking.BackColor == Color.Yellow ? SystemColors.Control : Color.Yellow;
            pnlGreenBlinking.BackColor = pnlGreenBlinking.BackColor == Color.Green ? SystemColors.Control : Color.Green;
            pnlBlueBlinking.BackColor = pnlBlueBlinking.BackColor == Color.Blue ? SystemColors.Control : Color.Blue;

            foreach (var item in m_dicButtonColor)
            {
                switch (item.Value)
                {
                    case enumSignalTowerColor.Red:
                        item.Key.BackColor = Color.Red;
                        break;
                    case enumSignalTowerColor.RedBlinking:
                        item.Key.BackColor = item.Key.BackColor == Color.Red ? SystemColors.Control : Color.Red;
                        break;
                    case enumSignalTowerColor.Yellow:
                        item.Key.BackColor = Color.Yellow;
                        break;
                    case enumSignalTowerColor.YellowBlinking:
                        item.Key.BackColor = item.Key.BackColor == Color.Yellow ? SystemColors.Control : Color.Yellow;
                        break;
                    case enumSignalTowerColor.Green:
                        item.Key.BackColor = Color.Green;
                        break;
                    case enumSignalTowerColor.GreenBlinking:
                        item.Key.BackColor = item.Key.BackColor == Color.Green ? SystemColors.Control : Color.Green;
                        break;
                    case enumSignalTowerColor.Blue:
                        item.Key.BackColor = Color.Blue;
                        break;
                    case enumSignalTowerColor.BlueBlinking:
                        item.Key.BackColor = item.Key.BackColor == Color.Blue ? SystemColors.Control : Color.Blue;
                        break;
                    case enumSignalTowerColor.None:
                        item.Key.BackColor = SystemColors.Control;
                        break;

                }
            }


        }
        private void pnlSelectColor_Click(object sender, EventArgs e)
        {
            Panel pnl = sender as Panel;
            if (m_dicPanelColor.ContainsKey(pnl))
            {
                m_eSelectColor = m_dicPanelColor[pnl];
            }
        }
        private void btnLight_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (m_dicButtonColor.ContainsKey(btn))
            {
                m_dicButtonColor[btn] = m_eSelectColor;
            }

        }
    }
}
