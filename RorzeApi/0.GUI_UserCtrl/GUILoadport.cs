using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RorzeUnit.Class;
using RorzeUnit.Class.Loadport.Event;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;

namespace RorzeApi.GUI
{
    public partial class GUILoadport : UserControl
    {
        Dictionary<string, string> m_DicAllLanguageTranfer = new Dictionary<string, string>();

        #region Enum
        public enum enumE84Status : int { Manual = 0, Auto }
        public enum enumLoadportStatus : int
        {
            [Description("Abort")]
            Abort = 0,
            [Description("Arrived")]
            Arrived,
            [Description("Clamped")]
            Clamped,
            [Description("Complete")]
            Complete,
            [Description("Disable")]
            Disable,
            [Description("Docked")]
            Docked,
            [Description("Docking")]
            Docking,
            [Description("Error")]
            Error,
            [Description("FoupOn")]
            FoupOn,
            [Description("FuncSetup")]
            FuncSetup,
            [Description("FuncSetupNG")]
            FuncSetupNG,
            [Description("Process")]
            Process,
            [Description("ReadyToLoad")]
            ReadyToLoad,
            [Description("ReadyToUnload")]
            ReadyToUnload,
            [Description("Removed")]
            Removed,
            [Description("Stop")]
            Stop,
            [Description("UnClamped")]
            UnClamped,
            [Description("UnDocked")]
            UnDocked,
            [Description("UnDocking")]
            UnDocking,
            [Description("Unknown")]
            Unknown
        }
        public enum enumUIGetPutFlag { None = 0, Get, Put };
        public enum enumType { Wafer, Panel }
        #endregion

        #region delegate EventHandler
        public event EventHandler BtnClamp;//Button
        public event EventHandler BtnDock;//Button
        public event EventHandler BtnUnDock;//Button
        public event EventHandler BtnE84Mode;//Button
        public event EventHandler ChkFoupOn;//Button(模擬)
        public event EventHandler ChkRecipeSelect;//Combobox
        public event EventHandler FoupIDKeyDownEnter;//Textbox
        //public event EventHandler LotIDKeyDownEnter;//Textbox
        public event EventHandler BtnProcess;//Button
        #endregion

        public class LbWafer : Label
        {
            public bool IsWaferOn = false;
            public enumUIPickWaferStat WaferSts = enumUIPickWaferStat.None;
            public int SlotIndex = -1;//畫第幾片 0,1,2,3,4,5,6
            public bool SelectFlag = false;//反白select用
            public string ShowText = string.Empty;//顯示文字   
            public double NotchAngle = 0.0;

            public string PrepareFromName;//選片顯示
            public string PrepareToName;//選片顯示

            public Color ShowTextColor = Color.Black;//顯示文字   

        }
        private enumUIGetPutFlag m_GetPutWafer = enumUIGetPutFlag.None;
        private bool m_IsWaferInUISelectList = false;

        private float X;//當前窗體的寬度
        private float Y;//當前窗體的高度

        private string _strMapData = "0000000000000000000000000";

        public string MapData
        {
            get
            {
                string strData;
                lock (this) { strData = _strMapData; }
                return strData;
            }
            set
            {
                lock (this) { _strMapData = value; }
            }
        }


        private bool isLoaded = false;
        private int _nWaferCount = 20;//用於表單建立層數
        public int WaferCount
        {
            get
            {
                return _strMapData.Length;//要看mapping的數量 
            }
        }

        private List<LbWafer> lstSlot = new List<LbWafer>();
        private List<SWafer.enumProcessStatus> lstWaferProcessStatus = new List<SWafer.enumProcessStatus>();
        public List<LbWafer> LstSlot { get { return lstSlot; } }

        Rectangle m_rectangleSelect;
        Point m_pointDown = Point.Empty;

        Color m_cWaferOn = Color.LimeGreen;//enumUIPickWaferStat.HasWafer
        Color cExeHasWafer = Color.LightGray;//enumUIPickWaferStat.ExeHasWafer
        Color m_cPutWafer = Color.PaleGreen;//enumUIPickWaferStat.PutWafer
        Color cPutWaferAndGet = Color.Blue;//enumUIPickWaferStat.PutWaferAndGet
        Color m_cExeHasWaferAndPut = Color.Salmon;//enumUIPickWaferStat.ExeHasWaferAndPut
        Color cNoWafer = SystemColors.Control;//enumUIPickWaferStat.NoWafer

        Color cNullslot = Color.Silver;//只有13層的CAS 14~25反灰      

        public GUILoadport()
        {
            InitializeComponent();

            X = this.Width;//獲取窗體的寬度
            Y = this.Height;//獲取窗體的高度

            _nWaferCount = 20;

            btnTitle.Text = "Loadport " + (char)(64 + BodyNo);

            cbxViewSlot.SelectedIndex = 0;

            CreateStageGUI();

            //m_DicAllLanguageTranfer.Add("NOTREADY", "未就绪");
            //m_DicAllLanguageTranfer.Add("READY", "准备");
            //m_DicAllLanguageTranfer.Add("ERROR", "错误");
            //m_DicAllLanguageTranfer.Add("RUN", "执行");
            //m_DicAllLanguageTranfer.Add("Disable", "禁用");
            //m_DicAllLanguageTranfer.Add("Disconnect", "断开"); 

            m_DicAllLanguageTranfer.Add("Loadport ", "晶圆装卸机");
            m_DicAllLanguageTranfer.Add("Abort", "中止");
            m_DicAllLanguageTranfer.Add("Arrived", "到达");
            m_DicAllLanguageTranfer.Add("Clamped", "勾住");
            m_DicAllLanguageTranfer.Add("Complete", "完成");
            m_DicAllLanguageTranfer.Add("Disable", "禁用");
            m_DicAllLanguageTranfer.Add("Docked", "开门");
            m_DicAllLanguageTranfer.Add("Docking", "开门中");
            m_DicAllLanguageTranfer.Add("Error", "错误");
            m_DicAllLanguageTranfer.Add("FoupOn", "晶圆盒");
            m_DicAllLanguageTranfer.Add("FuncSetup", "FuncSetup");
            m_DicAllLanguageTranfer.Add("FuncSetupNG", "FuncSetupNG");
            m_DicAllLanguageTranfer.Add("Process", "处理");
            m_DicAllLanguageTranfer.Add("ReadyToLoad", "准备加载");
            m_DicAllLanguageTranfer.Add("ReadyToUnload", "准备卸载");
            m_DicAllLanguageTranfer.Add("Removed", "离开");
            m_DicAllLanguageTranfer.Add("Stop", "停止");
            m_DicAllLanguageTranfer.Add("UnClamped", "松开");
            m_DicAllLanguageTranfer.Add("UnDocked", "关门");
            m_DicAllLanguageTranfer.Add("UnDocking", "关门中");
            m_DicAllLanguageTranfer.Add("Unknown", "未知");
            m_DicAllLanguageTranfer.Add("E84 Auto", "E84 自动");
            m_DicAllLanguageTranfer.Add("E84 Manual", "E84 手动");
        }
        private void CreateStageGUI()
        {
            TableLayoutPanel tlp = tlpWaferData/*new TableLayoutPanel()*/;

            tlp.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tlp, true, null);

            tlp.SuspendLayout();
            tlp.RowStyles.Clear();
            tlp.ColumnStyles.Clear();

            tlp.RowCount = _nWaferCount + 1;//用於表單建立層數
            tlp.ColumnCount = 2;

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < tlp.RowCount - 1; i++)
            {
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            }
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));

            tlp.AutoSize = true;
            tlp.Dock = DockStyle.Fill;
            tlp.Margin = new Padding(0);
            tlp.Padding = new Padding(0);
            //tlp.CellBorderStyle = TableLayoutPanelCellBorderStyle.Outset;

            for (int i = 0; i < tlp.RowCount - 1; i++)//注意建立順序 25 24 23 22 21...
            {
                Label labelSlot = new Label();
                labelSlot.Text = (_nWaferCount - i).ToString("D2");
                labelSlot.Dock = DockStyle.Fill;
                labelSlot.TextAlign = ContentAlignment.MiddleCenter;
                labelSlot.BorderStyle = BorderStyle.FixedSingle;
                labelSlot.Margin = new Padding(0);

                LbWafer labelWaferStatus = new LbWafer();
                labelWaferStatus.BackColor = SystemColors.Control;
                labelWaferStatus.Dock = DockStyle.Fill;
                labelWaferStatus.BorderStyle = BorderStyle.FixedSingle;

                labelWaferStatus.MouseDown += new MouseEventHandler(this._MouseDown);
                labelWaferStatus.MouseMove += new MouseEventHandler(this._MouseMove);
                labelWaferStatus.MouseUp += new MouseEventHandler(this._MouseUp);
                labelWaferStatus.Name = string.Format("lbWaferSts{0:D2}", _nWaferCount - 1 - i);
                labelWaferStatus.SlotIndex = _nWaferCount - 1 - i;
                labelWaferStatus.SelectFlag = false;
                labelWaferStatus.Paint += new PaintEventHandler(OnPaint);

                lstSlot.Insert(0, labelWaferStatus);
                lstWaferProcessStatus.Insert(0, new SWafer.enumProcessStatus());


                tlp.Controls.Add(labelSlot, 0, i);
                tlp.Controls.Add(labelWaferStatus, 1, i);
            }
            tlp.ResumeLayout();
        }
        public void SetSize(float frmX, float frmY)
        {
            isLoaded = true;// 已設定各控制項的尺寸到Tag屬性中
            SetTag(this);//調用方法


            float tempX = frmX / X;
            float tempY = frmY / Y;
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
        private string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if ((attributes != null) && (attributes.Length > 0))
                return attributes[0].Description;
            else
                return value.ToString();
        }

        #region ========== Button Operating Event=========
        private void btnDock_Click(object sender, EventArgs e)
        {
            BtnDock?.Invoke(this, new EventArgs());
        }
        private void btnUnDock_Click(object sender, EventArgs e)
        {
            BtnUnDock?.Invoke(this, new EventArgs());
        }
        private void btnE84Mode_Click(object sender, EventArgs e)
        {
            BtnE84Mode?.Invoke(this, new EventArgs());
        }
        private void btnProcessStart_Click(object sender, EventArgs e)
        {
            BtnProcess?.Invoke(this, new EventArgs());
        }
        private void chkFoupOn_CheckedChanged(object sender, EventArgs e)
        {
            //if (ChkFoupOn != null && Simulate)
            //    ChkFoupOn(this, new EventArgs());
        }
        private void chkFoupOn_Click(object sender, EventArgs e)
        {
            if (ChkFoupOn != null && Simulate)
                ChkFoupOn(this, new EventArgs());
        }
        private void cbxRecipe_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbxRecipe.SelectedItem == null) return;//剛開啟程式找不到預設的RECIPE
            Recipe = cbxRecipe.SelectedItem.ToString();
            ChkRecipeSelect?.Invoke(this, new EventArgs());
        }
        private void txtFoupID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FoupID = txtFoupID.Text;
                if (FoupIDKeyDownEnter != null) FoupIDKeyDownEnter(this, new EventArgs());
            }
        }
        private void btnClampLock_Click(object sender, EventArgs e)
        {
            if (BtnClamp != null) BtnClamp(this, new EventArgs());

            foreach (Label lb in lstSlot)
            {
                lb.BackColor = lb.BackColor;
            }
        }
        #endregion

        #region ========== select Wafer 方法 ==========
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            if (Disable_SelectWafer) return;
            if (Status == enumLoadportStatus.Process) return;
            if (Status != enumLoadportStatus.Docked) return;

            m_pointDown = MousePosition;//紀錄滑鼠點的位置

            LbWafer theLabel = (LbWafer)sender;
            if (theLabel.Enabled == false) return;

            if (SelectWaferBySorterMode == false)//true 跟Sorter一樣
            {
                if (theLabel.WaferSts == enumUIPickWaferStat.ExeHasWafer || theLabel.WaferSts == enumUIPickWaferStat.PutWafer)
                {
                    return;
                }
            }
            else
            {
                if (theLabel.WaferSts == enumUIPickWaferStat.PutWafer)
                {
                    return;
                }
            }

            //#region  ========== 取得當前label index ==========
            //int idx = -1;
            //Int32.TryParse(theLabel.Name.Substring(10, 2), out idx);//theLabel.Name = lbWaferStsXX
            //#endregion

            if (theLabel.WaferSts == enumUIPickWaferStat.HasWafer || theLabel.WaferSts == enumUIPickWaferStat.PutWafer)
            {
                #region  ========== 如果Slot 是 "有片" or "被放過片" ==========
                m_GetPutWafer = enumUIGetPutFlag.Get;

                #region ========== 如果按下ctrl或許可以不要下面那個 ===========
                //尚待優化
                #endregion

                #region ========== Reset 曾經選過的Slot ===========
                foreach (LbWafer item in lstSlot)
                {
                    item.SelectFlag = false;
                    item.Refresh();
                }
                #endregion

                theLabel.SelectFlag = true;

                #endregion
            }
            else if (theLabel.WaferSts == enumUIPickWaferStat.NoWafer || theLabel.WaferSts == enumUIPickWaferStat.ExeHasWafer)
            {
                #region  ========== 如果Slot 是 "無片" or "曾經有片(有片但被取走ㄌ)" ==========
                if (m_IsWaferInUISelectList == true)//如果上一次有人選片 
                {
                    m_GetPutWafer = enumUIGetPutFlag.Put;
                    theLabel.SelectFlag = true;
                }
                else if (SelectForStocker)//stocker mode
                {
                    #region ========== Reset 曾經選過的Slot ===========
                    foreach (LbWafer item in lstSlot)
                    {
                        item.SelectFlag = false;
                        item.Refresh();
                    }
                    #endregion

                    m_GetPutWafer = enumUIGetPutFlag.Put;
                    theLabel.SelectFlag = true;//測試寫寫看
                }
                else
                {
                    return;
                }
                #endregion
            }
            else
            {
                m_GetPutWafer = enumUIGetPutFlag.None;
                theLabel.SelectFlag = false;
            }

            theLabel.Refresh();
        }
        private void _MouseMove(object sender, MouseEventArgs e)
        {
            if (Disable_SelectWafer) return;
            if (Status == enumLoadportStatus.Process) return;
            if (Status != enumLoadportStatus.Docked) return;

            if (e.Button == MouseButtons.Left)
            {
                //  建立滑鼠選擇矩形
                Point pointNow = MousePosition;
                int x = (m_pointDown.X < pointNow.X) ? m_pointDown.X : pointNow.X;
                int y = (m_pointDown.Y < pointNow.Y) ? m_pointDown.Y : pointNow.Y;
                int width = Math.Abs(pointNow.X - m_pointDown.X);
                int hight = Math.Abs(pointNow.Y - m_pointDown.Y);
                m_rectangleSelect = new Rectangle(x, y, width, hight);//建立滑鼠拖拉矩形

                for (int i = 0; i < lstSlot.Count; i++)
                {
                    LbWafer theLabel = lstSlot[i];
                    Point pLabel = theLabel.PointToScreen(new Point(0, 0));
                    Rectangle rectangle2 = new Rectangle(pLabel.X, pLabel.Y, theLabel.Width, theLabel.Height);
                    bool inRegion1 = m_rectangleSelect.IntersectsWith(rectangle2);

                    if (theLabel.Enabled == false) continue;  //    13 or 25 slot

                    if (SelectWaferBySorterMode == false)//true 跟Sorter一樣
                    {
                        if (theLabel.WaferSts == enumUIPickWaferStat.ExeHasWafer || theLabel.WaferSts == enumUIPickWaferStat.PutWafer)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (theLabel.WaferSts == enumUIPickWaferStat.PutWafer)
                        {
                            continue;
                        }
                    }

                    if (m_GetPutWafer == enumUIGetPutFlag.Get)
                    {
                        if (inRegion1 &&
                            (theLabel.WaferSts == enumUIPickWaferStat.HasWafer || theLabel.WaferSts == enumUIPickWaferStat.PutWafer))
                        {
                            theLabel.SelectFlag = true;
                        }
                        else
                        {
                            theLabel.SelectFlag = false;
                        }
                    }
                    else if (m_GetPutWafer == enumUIGetPutFlag.Put)
                    {
                        if (inRegion1 &&
                            (theLabel.WaferSts == enumUIPickWaferStat.NoWafer || theLabel.WaferSts == enumUIPickWaferStat.ExeHasWafer))
                        {
                            if (m_IsWaferInUISelectList == true)//如果上一次有人選片  
                            {
                                theLabel.SelectFlag = true;
                            }
                            else if (SelectForStocker)//stocker mode 可以選空位
                            {
                                theLabel.SelectFlag = true;//測試寫寫看
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            //theLabel.SelectFlag = false;
                        }
                    }
                    else
                    {
                        theLabel.SelectFlag = false;
                    }
                    theLabel.Refresh();
                }

            }
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (Disable_SelectWafer) return;
            if (Status == enumLoadportStatus.Process) return;
            if (Status != enumLoadportStatus.Docked) return;

            Point pointNow = MousePosition;
            int x = (m_pointDown.X < pointNow.X) ? m_pointDown.X : pointNow.X;//選小的
            int y = (m_pointDown.Y < pointNow.Y) ? m_pointDown.Y : pointNow.Y;//選小的
            int width = Math.Abs(pointNow.X - m_pointDown.X);
            int hight = Math.Abs(pointNow.Y - m_pointDown.Y);
            m_rectangleSelect = new Rectangle(x, y, width, hight);//建立滑鼠拖拉矩形

            List<enumUIPickWaferStat> listSelectSlotSts = new List<enumUIPickWaferStat>();
            List<int> listSelectSlot = new List<int>();

            if (SelectForStocker)//stocker mode
            {
                //Stocker不要塞東西出去
            }
            else
            {

                for (int i = 0; i < lstSlot.Count(); i++)
                {
                    if (lstSlot[i].SelectFlag)
                    {
                        if (m_GetPutWafer == enumUIGetPutFlag.Get)
                        {
                            if (lstSlot[i].WaferSts == enumUIPickWaferStat.HasWafer || lstSlot[i].WaferSts == enumUIPickWaferStat.PutWafer)
                            {
                                listSelectSlotSts.Add(lstSlot[i].WaferSts);
                                listSelectSlot.Add(i);
                            }
                        }
                        else if (m_GetPutWafer == enumUIGetPutFlag.Put)
                        {
                            if (m_IsWaferInUISelectList == true)//如果上一次有人選片  
                            {
                                if (lstSlot[i].WaferSts == enumUIPickWaferStat.NoWafer || lstSlot[i].WaferSts == enumUIPickWaferStat.ExeHasWafer)
                                {
                                    listSelectSlotSts.Add(lstSlot[i].WaferSts);
                                    listSelectSlot.Add(i);
                                }
                            }
                            else
                            {
                                return;
                            }
                        }

                    }
                }

                //Y方向是上下，為了判斷使用者選的順序
                if (m_pointDown.Y < pointNow.Y)
                {
                    listSelectSlotSts.Reverse();
                    listSelectSlot.Reverse();
                }
            }

            UseSelectWafer?.Invoke(this, new EventArgs_SelectWafer(listSelectSlotSts, listSelectSlot));


        }
        public void UserSelectPlaceWaferInLoadport(string strFromName, int nSlotIndx, double dAngle = -1)
        {
            //sourceLp:如果之後有要UI顯示source的話可以用
            switch (lstSlot[nSlotIndx].WaferSts)
            {
                case enumUIPickWaferStat.NoWafer:
                    #region  NoWafer
                    {
                        lstSlot[nSlotIndx].WaferSts = enumUIPickWaferStat.PutWafer;
                        lstSlot[nSlotIndx].SelectFlag = false;
                        lstSlot[nSlotIndx].NotchAngle = dAngle;
                        lstSlot[nSlotIndx].PrepareFromName = strFromName;//選片顯示名稱
                    }
                    break;
                #endregion
                case enumUIPickWaferStat.ExeHasWafer:
                    #region  HasWafer
                    {
                        lstSlot[nSlotIndx].WaferSts = enumUIPickWaferStat.ExeHasWaferAndPut;
                        lstSlot[nSlotIndx].SelectFlag = false;
                        lstSlot[nSlotIndx].NotchAngle = dAngle;
                        lstSlot[nSlotIndx].PrepareFromName = strFromName;//選片顯示名稱
                    }
                    break;
                #endregion
                default:
                    {
                        lstSlot[nSlotIndx].PrepareFromName = "";
                    }
                    break;
            }
            lstSlot[nSlotIndx].Refresh();
        }
        public void ResetSlotSelectFlag(string strToName, int nSlotIndx)
        {
            lstSlot[nSlotIndx].SelectFlag = false;
            switch (lstSlot[nSlotIndx].WaferSts)
            {
                case enumUIPickWaferStat.HasWafer:
                    #region  HasWafer
                    {
                        lstSlot[nSlotIndx].WaferSts = enumUIPickWaferStat.ExeHasWafer;
                        lstSlot[nSlotIndx].PrepareToName = strToName;//選片顯示名稱
                    }
                    break;
                #endregion
                case enumUIPickWaferStat.PutWafer:
                    #region  PutWafer
                    {
                        lstSlot[nSlotIndx].WaferSts = enumUIPickWaferStat.PutWaferAndGet;
                        lstSlot[nSlotIndx].PrepareToName = strToName;//選片顯示名稱
                    }
                    break;
                #endregion
                default:
                    lstSlot[nSlotIndx].PrepareToName = "";
                    break;
            }
            lstSlot[nSlotIndx].NotchAngle = -1;
            lstSlot[nSlotIndx].Refresh();
        }
        public void ResetAllSelectSlot()
        {
            foreach (LbWafer lb in lstSlot)
            {
                lb.SelectFlag = false;
                lb.Refresh();
            }
            //for (int i = 0; i < lstSlot.Count; i++)
            //{
            //    lstSlot[i].SelectFlag = false;
            //    lstSlot[i].Refresh();
            //}
        }
        public void EnableUISelectPutWaferFlag(bool enable)
        {
            m_IsWaferInUISelectList = enable;
        }
        protected void OnPaint(object sender, PaintEventArgs e)
        {
            LbWafer lb = (LbWafer)sender;

            int idx = -1;
            Int32.TryParse(lb.Name.Substring(10, 2), out idx);//theLabel.Name = lbWaferStsXX

            //注意你只能改變一次BackColor，否則會進無窮Paint迴圈
            if (lb.SelectFlag)
            {
                if (m_bFreeStyle)
                    lb.BackColor = ShowSelectColor ? m_cSelect : Color.Transparent;
                else
                    lb.BackColor = ShowSelectColor ? Color.Blue : Color.Transparent;
            }
            else
            {
                if (lb.Enabled == false)
                {

                }
                else
                {
                    lb.BackColor = Color.Transparent;
                }
            }

            Color temp = Color.Transparent;
            bool hasWafer = true;
            switch (lb.WaferSts)
            {
                case enumUIPickWaferStat.NoWafer:
                case enumUIPickWaferStat.None:
                    hasWafer = false;
                    break;
                case enumUIPickWaferStat.HasWafer:
                    {
                        temp = m_cWaferOn;
                    }
                    break;
                case enumUIPickWaferStat.PutWafer:
                    {
                        temp = m_cPutWafer;
                    }
                    break;
                case enumUIPickWaferStat.ExeHasWafer:
                    {
                        temp = cExeHasWafer;
                    }
                    break;
                case enumUIPickWaferStat.PutWaferAndGet:
                    {
                        temp = cPutWaferAndGet;
                    }
                    break;
                case enumUIPickWaferStat.ExeHasWaferAndPut:
                    {
                        temp = m_cExeHasWaferAndPut;
                    }
                    break;
                default:
                    {
                        temp = Color.Black;
                    }
                    break;
            }

            if (lstWaferProcessStatus[idx] == SWafer.enumProcessStatus.Error)
            {
                hasWafer = true;
                temp = Color.Red;
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            e.Graphics.FillRectangle/*.FillEllipse*/(new SolidBrush(temp), new Rectangle(1, 1, lb.Width - 5, lb.Height - 5));
            if (hasWafer)
            {
                e.Graphics.DrawRectangle/*.DrawEllipse*/(new Pen(Brushes.Black, 0.87f), new Rectangle(1, 1, lb.Width - 5, lb.Height - 5));
            }

            e.Graphics.DrawString(lb.ShowText, new Font("Consolas", lb.Height / 2), new SolidBrush(lb.ShowTextColor), lb.Width / 3, 2);


            if (idx == 1 && lb.WaferSts != enumUIPickWaferStat.ExeHasWafer && lb.WaferSts != enumUIPickWaferStat.PutWafer)
            {

            }

            switch (lb.WaferSts)
            {
                case enumUIPickWaferStat.None:
                case enumUIPickWaferStat.NoWafer:
                    break;
                case enumUIPickWaferStat.HasWafer:
                    break;
                case enumUIPickWaferStat.ExeHasWafer:
                    {
                        if (lb.PrepareToName != null && lb.PrepareToName != "" && lb.PrepareToName.Length <= 3)
                            e.Graphics.DrawString(lb.PrepareToName, new Font("Consolas", lb.Height / 2), new SolidBrush(Color.Red), lb.Width / 8, 2);
                        else
                            e.Graphics.DrawString(lb.PrepareToName, new Font("Consolas", lb.Height / 2), new SolidBrush(Color.Red), 0, 2);
                    }
                    break;
                case enumUIPickWaferStat.PutWaferAndGet:
                    break;
                case enumUIPickWaferStat.PutWafer:
                case enumUIPickWaferStat.ExeHasWaferAndPut:
                    {
                        if (lb.PrepareFromName != null && lb.PrepareFromName != "" && lb.PrepareFromName.Length <= 3)
                            e.Graphics.DrawString(lb.PrepareFromName, new Font("Consolas", lb.Height / 2), new SolidBrush(Color.Red), lb.Width / 8, 2);
                        else
                            e.Graphics.DrawString(lb.PrepareFromName, new Font("Consolas", lb.Height / 2), new SolidBrush(Color.Red), 0, 2);

                        if (lb.NotchAngle > -1)
                        {
                            double[] dAngle = new double[] { 0, 45, 90, 135, 180, 225, 270, 315 };

                            Bitmap[] bitmapArray = new Bitmap[]
                            {
                                Properties.Resources.arrow_0_48,
                                Properties.Resources.arrow_45_48,
                                Properties.Resources.arrow_90_48,
                                Properties.Resources.arrow_135_48,
                                Properties.Resources.arrow_180_48,
                                Properties.Resources.arrow_225_48,
                                Properties.Resources.arrow_270_48,
                                Properties.Resources.arrow_315_48
                            };

                            if (_Type == enumType.Panel)//panel定義與wafer不相同，視覺響應
                            {
                                bitmapArray = new Bitmap[]
                                {
                                    Properties.Resources.arrow_45_48,
                                    Properties.Resources.arrow_45_48,
                                    Properties.Resources.arrow_315_48,
                                    Properties.Resources.arrow_315_48,
                                    Properties.Resources.arrow_225_48,
                                    Properties.Resources.arrow_225_48,
                                    Properties.Resources.arrow_135_48,
                                    Properties.Resources.arrow_135_48,                                         
                                };
                            }



                            for (int i = 0; i < dAngle.Length; i++)
                            {
                                if (Math.Abs(dAngle[i] - lb.NotchAngle) <= 22.5)
                                {
                                    Bitmap bmpTmp = new Bitmap(bitmapArray[i], lb.Height, lb.Height);//正方形大小
                                    double dXoffset = (lb.Width - bmpTmp.Width) /*/ 2*/;
                                    double dYoffset = (lb.Height - bmpTmp.Height) / 2;
                                    e.Graphics.DrawImage(bmpTmp, (int)dXoffset, (int)dYoffset);
                                    break;
                                }
                            }

                        }
                    }
                    break;
                default:
                    break;
            }




        }

        public void SelectWaferByCycle(bool bWaferOut, int nCount)
        {
            for (int i = 0; i < lstSlot.Count; i++)
            {
                LbWafer theLabel = lstSlot[i];
                if (theLabel.Enabled == false) continue;//cassette只有13層


                //改變屬性
                m_GetPutWafer = bWaferOut ? enumUIGetPutFlag.Get : enumUIGetPutFlag.Put;

                if (bWaferOut == theLabel.IsWaferOn)//出片要有Wafer
                {

                    theLabel.SelectFlag = true;//有片要送出
                    nCount--;
                    if (nCount == 0) { break; }//片子分配完了
                }

            }
        }

        #endregion

        #region ========== public ==========


        //  更新GUI Recipe清單
        public void SetRecipList(string[] strArry, string strDefaultRecipe)
        {
            if (strArry == null) return;

            cbxRecipe.Items.Clear();

            foreach (string str in strArry)
            {
                cbxRecipe.Items.Add(str);
                if (str == strDefaultRecipe)
                    cbxRecipe.SelectedIndex = cbxRecipe.Items.IndexOf(strDefaultRecipe);
            }
            if (cbxRecipe.Items.Count > 0 && cbxRecipe.SelectedIndex == -1) cbxRecipe.SelectedIndex = 0;
            cbxRecipe_SelectionChangeCommitted(cbxRecipe, new EventArgs());
        }
        //  更新GUI slot狀態_顏色
        public void UpdataWaferProcessStatus(int nSlot, SWafer.enumProcessStatus status, Color cStatus)
        {
            //為了選擇WAFER變色，外層有Timer
            if (lstWaferProcessStatus[nSlot - 1] == status) return;

            lstWaferProcessStatus[nSlot - 1] = status;

            switch (status)
            {
                case SWafer.enumProcessStatus.None:
                    lstSlot[nSlot - 1].WaferSts = enumUIPickWaferStat.NoWafer;
                    break;
                case SWafer.enumProcessStatus.Sleep:
                    lstSlot[nSlot - 1].WaferSts = enumUIPickWaferStat.HasWafer;
                    break;
                case SWafer.enumProcessStatus.WaitProcess:
                    break;
                case SWafer.enumProcessStatus.Processing:
                    lstSlot[nSlot - 1].WaferSts = enumUIPickWaferStat.NoWafer;
                    break;
                case SWafer.enumProcessStatus.Processed:
                    lstSlot[nSlot - 1].WaferSts = enumUIPickWaferStat.HasWafer;
                    break;
                case SWafer.enumProcessStatus.Abort:
                case SWafer.enumProcessStatus.Error:
                    break;
            }
            lstSlot[nSlot - 1].Refresh();
        }
        //  更新GUI slot狀態_顯示文字
        public void UpdataWaferStatus(int nSlot, string strM12 = "", string strT7 = "", string strM12ByHost = "", string strT7ByHost = "", SWafer.enumPosition Pos = SWafer.enumPosition.UnKnow)
        {
            lstSlot[nSlot - 1].ShowTextColor = Color.Black;

            if (cbxViewSlot.SelectedIndex == 0)
            {
                switch (lstWaferProcessStatus[nSlot - 1])
                {
                    case SWafer.enumProcessStatus.WaitProcess:
                        lstSlot[nSlot - 1].ShowText = "WaitProcess";
                        break;
                    case SWafer.enumProcessStatus.Processing:

                        if (Pos == SWafer.enumPosition.AlignerA)
                            lstSlot[nSlot - 1].ShowText = "Alignment";
                        else if (Pos == SWafer.enumPosition.AlignerB)
                            lstSlot[nSlot - 1].ShowText = "Alignment";
                        else if (Pos == SWafer.enumPosition.EQM1 || Pos == SWafer.enumPosition.EQM2 || Pos == SWafer.enumPosition.EQM3 || Pos == SWafer.enumPosition.EQM4)
                            lstSlot[nSlot - 1].ShowText = "Processing";
                        else if (Pos == SWafer.enumPosition.AOI)
                            lstSlot[nSlot - 1].ShowText = "Processing";
                        else
                            lstSlot[nSlot - 1].ShowText = "Transfering";
                        break;
                    case SWafer.enumProcessStatus.Processed:
                        lstSlot[nSlot - 1].ShowText = "Processed";
                        break;
                    case SWafer.enumProcessStatus.Abort:
                        lstSlot[nSlot - 1].ShowText = "Abort";
                        break;
                    case SWafer.enumProcessStatus.Error:
                        if (MapData != null && MapData != "" && MapData[nSlot - 1] == '2') lstSlot[nSlot - 1].ShowText = "Thickness Wafer";
                        else if (MapData != null && MapData != "" && MapData[nSlot - 1] == '3') lstSlot[nSlot - 1].ShowText = "Cross Wafer";
                        else if (MapData != null && MapData != "" && MapData[nSlot - 1] == '4') lstSlot[nSlot - 1].ShowText = "Front Bow Wafer";
                        else if (MapData != null && MapData != "" && MapData[nSlot - 1] == '7') lstSlot[nSlot - 1].ShowText = "Double Wafer";
                        else if (MapData != null && MapData != "" && MapData[nSlot - 1] == '8') lstSlot[nSlot - 1].ShowText = "Thickness Wafer";
                        else if (MapData != null && MapData != "" && MapData[nSlot - 1] == '9') lstSlot[nSlot - 1].ShowText = "Mapping Failure";
                        else lstSlot[nSlot - 1].ShowText = "Error";
                        break;
                    default:
                        lstSlot[nSlot - 1].ShowText = "";
                        break;
                }
            }
            else if (cbxViewSlot.SelectedIndex == 1)
            {
                if (strM12ByHost == string.Empty || strM12 == strM12ByHost)//offline只顯示OCR讀到的
                {
                    lstSlot[nSlot - 1].ShowText = strM12;
                }
                else if (strM12 != strM12ByHost)
                {
                    lstSlot[nSlot - 1].ShowText = strM12ByHost;
                    lstSlot[nSlot - 1].ShowTextColor = Color.Red;
                }
            }
            else if (cbxViewSlot.SelectedIndex == 2)
            {
                if (strT7ByHost == string.Empty || strT7 == strT7ByHost)//offline只顯示OCR讀到的
                {
                    lstSlot[nSlot - 1].ShowText = strT7;
                }
                else if (strT7 != strT7ByHost)
                {
                    lstSlot[nSlot - 1].ShowText = strT7ByHost;
                    lstSlot[nSlot - 1].ShowTextColor = Color.Red;
                }
            }
            else
            {
                lstSlot[nSlot - 1].ShowText = "";
            }
            lstSlot[nSlot - 1].Refresh();
        }
        //  更新GUI Mapping 完成後 更新表單   
        public void UpdataMappingData(string strMappingData)
        {
            MapData = strMappingData;
            for (int i = 0; i < lstSlot.Count; i++)//1~25
            {
                if (i >= strMappingData.Length)//只有13層的CAS 14~25反灰
                {
                    lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Sleep;
                    lstSlot[i].BackColor = cNullslot;
                    lstSlot[i].Enabled = false;
                    continue;
                }
                lstSlot[i].SelectFlag = false;
                lstSlot[i].Enabled = true;

                switch (strMappingData[i])
                {
                    case '0':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.None;
                        lstSlot[i].WaferSts = enumUIPickWaferStat.NoWafer;
                        lstSlot[i].IsWaferOn = false;
                        UpdataWaferStatus(i + 1);
                        break;
                    case '1':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Sleep;
                        lstSlot[i].WaferSts = enumUIPickWaferStat.HasWafer;
                        lstSlot[i].IsWaferOn = true;
                        break;
                    case '2':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Error;
                        lstSlot[i].WaferSts = enumUIPickWaferStat.None;
                        lstSlot[i].IsWaferOn = true;
                        UpdataWaferStatus(i + 1);
                        break;
                    case '3':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Error;
                        lstSlot[i].WaferSts = enumUIPickWaferStat.None;
                        lstSlot[i].IsWaferOn = true;
                        UpdataWaferStatus(i + 1);
                        break;
                    case '4':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Error;
                        lstSlot[i].WaferSts = enumUIPickWaferStat.None;
                        lstSlot[i].IsWaferOn = true;
                        UpdataWaferStatus(i + 1);
                        break;
                    case '7':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Error;
                        lstSlot[i].WaferSts = enumUIPickWaferStat.None;
                        lstSlot[i].IsWaferOn = true;
                        UpdataWaferStatus(i + 1);
                        break;
                    case '8':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Error;
                        lstSlot[i].WaferSts = enumUIPickWaferStat.None;
                        lstSlot[i].IsWaferOn = true;
                        UpdataWaferStatus(i + 1);
                        break;
                    case '9':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Error;
                        lstSlot[i].WaferSts = enumUIPickWaferStat.None;
                        lstSlot[i].IsWaferOn = true;
                        UpdataWaferStatus(i + 1);
                        break;
                    default:
                        UpdataWaferStatus(i + 1);
                        break;
                }
                lstSlot[i].Refresh();
            }
        }

        public void ResetUpdateMappingData()
        {
            UpdataMappingData(MapData);
        }
        //  更新GUI Foup 放上去或拿走後 更新表單   
        public void UpdataFoupExist(bool bFoupExist)
        {

            //txtFoupID.Enabled = bFoupExist;
            //txtLotID.Enabled = bFoupExist;

            tlpWaferData.Enabled = bFoupExist;
            btnProcessStart.Enabled = bFoupExist;
            btnDock.Enabled = bFoupExist;
            btnUnDock.Enabled = bFoupExist;

            chkFoupOn.Checked = bFoupExist;

            if (bFoupExist == false)
            {
                for (int nSlot = 0; nSlot < lstSlot.Count; nSlot++)
                {
                    lstSlot[nSlot].Text = "";
                    lstSlot[nSlot].ShowText = "";
                    lstSlot[nSlot].BackColor = cNoWafer;
                    lstSlot[nSlot].WaferSts = enumUIPickWaferStat.None;
                    lstWaferProcessStatus[nSlot] = SWafer.enumProcessStatus.None;
                    lstSlot[nSlot].Refresh();
                }

                _strMapData = "0000000000000000000000000";



            }




        }

        public void TakeWaferOutFoup(int nSlot)
        {
            try
            {
                int nIndex = nSlot - 1;
                string str = MapData;
                str = str.Remove(nIndex, 1);
                str = str.Insert(nIndex, "0");
                lstSlot[nIndex].WaferSts = enumUIPickWaferStat.NoWafer;
                MapData = str;
                lstSlot[nIndex].Refresh();//20240329 STAR
            }
            catch { }
        }
        public void TakeWaferInFoup(int nSlot)
        {
            try
            {
                int nIndex = nSlot - 1;
                string str = MapData;
                str = str.Remove(nIndex, 1);
                str = str.Insert(nIndex, "1");
                lstSlot[nIndex].WaferSts = enumUIPickWaferStat.HasWafer;
                MapData = str;
                lstSlot[nIndex].Refresh();//20240329 STAR
            }
            catch { }
        }


        public List<int> GetSelectSlotIndxForStocker()
        {
            List<enumUIPickWaferStat> listSelectSlotSts = new List<enumUIPickWaferStat>();
            List<int> listSelectSlot = new List<int>();

            for (int i = 0; i < lstSlot.Count(); i++)
            {
                if (lstSlot[i].SelectFlag == false) continue;

                if (m_GetPutWafer == enumUIGetPutFlag.Get)
                {
                    if (lstSlot[i].WaferSts == enumUIPickWaferStat.HasWafer)
                    {
                        listSelectSlotSts.Add(lstSlot[i].WaferSts);
                        listSelectSlot.Add(i);
                    }
                }
                else if (m_GetPutWafer == enumUIGetPutFlag.Put)
                {
                    if (lstSlot[i].WaferSts == enumUIPickWaferStat.NoWafer)
                    {
                        listSelectSlotSts.Add(lstSlot[i].WaferSts);
                        listSelectSlot.Add(i);
                    }
                }
            }
            return listSelectSlot;
        }


        #endregion

        #region ========== property ==========
        //-----------------------------------------------------------------------------
        bool m_bSimulate;
        public bool Simulate
        {
            get { return m_bSimulate; }
            set
            {
                m_bSimulate = value;
                chkFoupOn.Visible = value;
            }
        }
        //-----------------------------------------------------------------------------
        int m_nBodyNo;
        public int BodyNo
        {
            get { return m_nBodyNo; }
            set
            {
                m_nBodyNo = value;
                btnTitle.Text = GetLanguage("Loadport ") + (char)(64 + value);
            }
        }
        //-----------------------------------------------------------------------------
        string m_strFoupID = "";
        public string FoupID
        {
            get { return m_strFoupID; }
            set
            {
                if (m_strFoupID == value) return;
                m_strFoupID = value;
                txtFoupID.Text = m_strFoupID;
                this.Refresh();
            }
        }
        //-----------------------------------------------------------------------------
        string m_strRSV = "";
        public string RSV
        {
            get { return m_strRSV; }
            set
            {
                if (m_strRSV == value) return;
                m_strRSV = value;
                txtLoaderRSV.Text = m_strRSV;
                this.Refresh();
            }
        }
        //-----------------------------------------------------------------------------
        bool m_bKeepClamp = false;
        public bool KeepClamp
        {
            get { return m_bKeepClamp; }
            set
            {
                if (m_bKeepClamp == value) return;
                m_bKeepClamp = value;
                btnClampLock.Image = value ? Properties.Resources._32_lock : null;
            }
        }
        //-----------------------------------------------------------------------------
        enumLoadportStatus m_loadportStatus = enumLoadportStatus.Unknown;
        public enumLoadportStatus Status
        {
            get { return m_loadportStatus; }
            set
            {
                if (m_loadportStatus == value) return;
                m_loadportStatus = value;

                if (lblLoaderStatus.InvokeRequired)
                {
                    lblLoaderStatus.BeginInvoke(new Action(() => lblLoaderStatus.Text = GetLanguage(GetEnumDescription(m_loadportStatus))));
                }
                else
                {
                    lblLoaderStatus.Text = GetLanguage(GetEnumDescription(m_loadportStatus));
                }

                //因為可以人工KeyIn，只有在以下兩種狀況可以給他手工輸入
                bool b = m_loadportStatus == enumLoadportStatus.Arrived ||
                         m_loadportStatus == enumLoadportStatus.ReadyToUnload ||
                         m_loadportStatus == enumLoadportStatus.Clamped ||
                         m_loadportStatus == enumLoadportStatus.Docked;
                txtFoupID.Enabled = b;

                cbxRecipe.Enabled = b;//220708

                switch (m_loadportStatus)
                {
                    case enumLoadportStatus.Abort: break;

                    case enumLoadportStatus.Clamped: break;
                    case enumLoadportStatus.Complete: break;
                    case enumLoadportStatus.Disable: break;

                    case enumLoadportStatus.Docked:
                        if (m_bFreeStyle)
                        {
                            lblLoaderStatus.ForeColor = Color.Black;
                            lblLoaderStatus.BackColor = m_cReady;
                        }
                        else
                        {
                            lblLoaderStatus.ForeColor = Color.White;
                            lblLoaderStatus.BackColor = Color.Green;
                        }
                        break;
                    case enumLoadportStatus.Docking:
                        lblLoaderStatus.ForeColor = Color.Black;
                        lblLoaderStatus.BackColor = Color.Cyan;
                        break;
                    case enumLoadportStatus.Error:
                        lblLoaderStatus.ForeColor = Color.Black;
                        lblLoaderStatus.BackColor = Color.Red;
                        break;
                    case enumLoadportStatus.FoupOn: break;
                    case enumLoadportStatus.FuncSetup: break;
                    case enumLoadportStatus.FuncSetupNG: break;
                    //case enumLoadportStatus.Process: break;
                    case enumLoadportStatus.ReadyToLoad:
                    case enumLoadportStatus.Arrived:
                        if (m_bFreeStyle)
                        {
                            lblLoaderStatus.ForeColor = Color.Black;
                            lblLoaderStatus.BackColor = m_cReady;
                        }
                        else
                        {
                            lblLoaderStatus.ForeColor = Color.White;
                            lblLoaderStatus.BackColor = Color.Green;
                        }
                        break;
                    case enumLoadportStatus.ReadyToUnload: break;
                    case enumLoadportStatus.Removed: break;
                    case enumLoadportStatus.Stop: break;
                    case enumLoadportStatus.UnClamped: break;
                    case enumLoadportStatus.UnDocked: break;
                    case enumLoadportStatus.UnDocking:
                        lblLoaderStatus.ForeColor = Color.Black;
                        lblLoaderStatus.BackColor = Color.Cyan;
                        break;
                    case enumLoadportStatus.Unknown:
                    case enumLoadportStatus.Process:
                        lblLoaderStatus.ForeColor = Color.Black;
                        lblLoaderStatus.BackColor = Color.Yellow;
                        break;
                }

                this.Refresh();
            }
        }
        //-----------------------------------------------------------------------------
        enumE84Status m_e84Status = enumE84Status.Manual;
        public enumE84Status E84Status
        {
            get { return m_e84Status; }
            set
            {
                if (m_e84Status == value) return;
                m_e84Status = value;

                if (m_e84Status == enumE84Status.Auto)
                {
                    btnE84Mode.Text = GetLanguage("E84 Auto");
                    btnE84Mode.BackColor = Color.Gold;
                    btnE84Mode.ForeColor = Color.Black;
                }
                else
                {
                    btnE84Mode.Text = GetLanguage("E84 Manual");
                    btnE84Mode.BackColor = Color.FromArgb(149, 149, 149)/*Color.RoyalBlue*/;
                    btnE84Mode.ForeColor = Color.Black/*Color.White*/;
                }

                this.Refresh();
            }
        }
        //-----------------------------------------------------------------------------        
        public string InfoPadName
        {
            set
            {
                if (lblLoaderType.Text == value) return;
                lblLoaderType.Text = value;
                this.Refresh();
            }
        }
        //-----------------------------------------------------------------------------
        enumType m_eType = enumType.Wafer;
        public enumType _Type
        {
            get { return m_eType; }
            set
            {
                if (m_eType == value) return;
                m_eType = value;
            }
        }
        //-----------------------------------------------------------------------------

        public bool Disable_OCR { get { return !cbxViewSlot.Visible; } set { cbxViewSlot.Visible = !value; } }
        public bool Disable_E84 { get { return !btnE84Mode.Visible; } set { btnE84Mode.Visible = !value; } }
        public bool Disable_Recipe { get { return !tlpRecipeSelect.Visible; } set { tlpRecipeSelect.Visible = !value; } }
        public bool Disable_RSV { get { return !tlpRSV.Visible; } set { tlpRSV.Visible = !value; } }
        public bool Disable_ClmpLock { get { return !tlpClampLock.Visible; } set { tlpClampLock.Visible = !value; } }
        public bool Disable_ProcessBtn { get { return !tlpProcessBtn.Visible; } set { tlpProcessBtn.Visible = !value; } }
        public bool Disable_DockBtn { get { return !tlpDockBtn.Visible; } set { tlpDockBtn.Visible = !value; } }
        public bool Disable_SelectWafer { get; set; }
        public bool SelectWaferBySorterMode { get; set; }//true 跟Sorter一樣
        public bool ShowSelectColor { get; set; }//有時候介面上不想看見選取的背景藍色
        public bool SelectForStocker { get; set; }//先選擇位置然後透過stock指定wafer

        //-----------------------------------------------------------------------------
        string m_strRecipe = "";
        public string Recipe
        {
            get { return m_strRecipe; }
            set
            {
                if (m_strRecipe == value) return;
                m_strRecipe = value;

                for (int i = 0; i < cbxRecipe.Items.Count; i++)
                {
                    if (cbxRecipe.Items[i].ToString() == m_strRecipe)
                    {
                        cbxRecipe.SelectedIndex = i;
                        break;
                    }
                }

                this.Refresh();
            }
        }


        #endregion



        //event arguments
        public class EventArgs_SelectWafer : EventArgs
        {
            public EventArgs_SelectWafer()
            {
                SelectSlotSts = new List<enumUIPickWaferStat>();
                SelectSlotNum = new List<int>();
            }

            public EventArgs_SelectWafer(List<enumUIPickWaferStat> listSelectSlotSts, List<int> listSelectSlotNum)
            {
                SelectSlotSts = listSelectSlotSts;
                SelectSlotNum = listSelectSlotNum;
            }
            public List<enumUIPickWaferStat> SelectSlotSts { get; set; }
            public List<int> SelectSlotNum { get; set; }
        }
        //event handler
        public delegate void EventHandler_SelectWafer(object sender, EventArgs_SelectWafer e);

        public event EventHandler_SelectWafer UseSelectWafer;

        public void DrawEllipseFloat(object sender, PaintEventArgs e)
        {
            Label lb = sender as Label;

            // Create pen.
            Pen redkPen = new Pen(Color.Red, 1);
            // Create location and size of ellipse.
            float x = 0.0F;
            float y = 0.0F;
            //float width = 100.0F;
            //float height = 100.0F;

            //int normalX = (lb.Width / 2);
            //int normalY = (lb.Height / 2);

            // Draw ellipse to screen.
            e.Graphics.DrawEllipse(redkPen, x + 3, y + 3, lb.Width - 3, lb.Height - 3);
        }

        private void GUILoadport_Paint(object sender, PaintEventArgs e)
        {

        }

        public void SetcbxViewSlot(int n)
        {
            cbxViewSlot.SelectedIndex = n;
        }



        bool m_bFreeStyle = false;
        Color m_cReady = Color.Green;
        Color m_cSelect = Color.Blue;
        public void SetFreeStyleColor(Color cTitle, Color cReady, Color cWafer, Color cSelect, Color cPutWafer)
        {
            m_bFreeStyle = true;

            btnTitle.ForeColor = Color.Black;
            btnTitle.BackColor = cTitle;

            m_cReady = cReady;

            m_cWaferOn = cWafer;
            m_cSelect = cSelect;
            m_cPutWafer = cPutWafer;

            m_cExeHasWaferAndPut = Color.FromArgb(255, 160, 122);//Color.LightSalmon
        }

        public string GetLanguage(string source)
        {
            string target = "";

            switch (GParam.theInst.SystemLanguage)
            {
                case enumSystemLanguage.Default:
                case enumSystemLanguage.zn_TW:
                    {
                        target = source;
                    }
                    break;
                case enumSystemLanguage.zh_CN:
                    {
                        if (m_DicAllLanguageTranfer.ContainsKey(source))
                        {
                            target = m_DicAllLanguageTranfer[source];
                        }
                        else
                        {
                            target = source;
                        }
                    }
                    break;
            }
            return target;
        }
    }
}
