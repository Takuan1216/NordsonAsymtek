using Rorze.Secs;
using RorzeApi.SECSGEM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RorzeApi
{
    public partial class frmSECSControl : Form
    {
        float frmX;//當前窗體的寬度
        float frmY;//當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        SGEM300 _Gem;
        SSECSParameter _parameter;

        public frmSECSControl(SGEM300 Gem, SSECSParameter Par)
        {
            InitializeComponent();

            _Gem = Gem;
            _parameter = Par;
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

        //========= 

        private void timer1_Tick(object sender, EventArgs e)
        {
            Color colorOn = Color.LightBlue;
            if (GParam.theInst.FreeStyle)
            {
                colorOn = GParam.theInst.ColorTitle;
            }

            btnSECSOn.BackColor = (_Gem.GetSECSDriver.GetSecsStarted()) ? colorOn : SystemColors.Control;
            btnSECSOff.BackColor = (!_Gem.GetSECSDriver.GetSecsStarted()) ? colorOn : SystemColors.Control;
            btnGoOffline.BackColor = (_Gem.GEMControlStatus != GEMControlStats.ONLINEREMOTE && _Gem.GEMControlStatus != GEMControlStats.ONLINELOCAL) ? colorOn : SystemColors.Control;
            btnGoLocal.BackColor = (_Gem.GEMControlStatus == GEMControlStats.ONLINELOCAL) ? colorOn : SystemColors.Control;
            btnGoRmote.BackColor = (_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE) ? colorOn : SystemColors.Control;

        }
        //========= 
        private void btnSECSOn_Click(object sender, EventArgs e)
        {
            if (!_Gem.GetSECSDriver.GetSecsStarted() && _Gem.SECSOpenbusy == false)
                _Gem.SECSStart();
            else
            {

            }
        }
        private void btnSECSOff_Click(object sender, EventArgs e)
        {
            if (_Gem.GetSECSDriver.GetSecsStarted() && _Gem.SECSClosebusy ==false)
                _Gem.SECSStop();
            else
            {

            }
        }
        private void btnGoOffline_Click(object sender, EventArgs e)
        {
            if (_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE || _Gem.GEMControlStatus == GEMControlStats.ONLINELOCAL)
                _Gem.SetGEMControlStatus = GEMControlStats.OFFLINE;
            else
            {

            }
        }
        private void btnGoOnline_Click(object sender, EventArgs e)
        {
            if (_Gem.GEMControlStatus != GEMControlStats.ONLINEREMOTE && _Gem.GEMControlStatus != GEMControlStats.ONLINELOCAL)
            {
                _Gem.SetGEMControlStatus = _parameter.GetSECSParameterConfig.OnlineSubStats;
            }
            else
            {

            }
        }
        private void btnGoLocal_Click(object sender, EventArgs e)
        {
            if (_Gem.GEMControlStatus == GEMControlStats.ONLINEREMOTE)
                _Gem.SetGEMControlStatus = GEMControlStats.ONLINELOCAL;
            else
            {

            }
        }
        private void btnGoRmote_Click(object sender, EventArgs e)
        {
            if (_Gem.GEMControlStatus == GEMControlStats.ONLINELOCAL)
                _Gem.SetGEMControlStatus = GEMControlStats.ONLINEREMOTE;
            else
            {

            }
        }
        //========= 
        private void frmSECSControl_VisibleChanged(object sender, EventArgs e)
        {
            timer1.Enabled = this.Visible;
        }

        //========= 
    }
}
