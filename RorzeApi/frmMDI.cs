using RorzeApi.Class;
using RorzeApi.SECSGEM;
using RorzeComm.Log;
using RorzeUnit.Class;
using RorzeUnit.Class.Aligner;
using RorzeUnit.Class.BarCode;
using RorzeUnit.Class.Buffer;
using RorzeUnit.Class.E84;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.FFU;
using RorzeUnit.Class.ElectrostaticDetect;
using RorzeUnit.Class.Keyence_MP;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Loadport.Type;
using RorzeUnit.Class.OCR;
using RorzeUnit.Class.OCR.Enum;
using RorzeUnit.Class.RC500;
using RorzeUnit.Class.RFID;
using RorzeUnit.Class.Robot;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Class.SafetyIOStatus;
using RorzeUnit.Class.Camera;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using static RorzeUnit.Class.SWafer;
using System.Net.Sockets;
using System.Security.Cryptography;
using RorzeUnit.Class.Vibration;
using RorzeUnit.Class.ADAM;
using RorzeUnit.Class.EQ.Enum;


namespace RorzeApi
{
    public partial class frmMDI : Form
    {
        #region ======================== Enum =============================
        enum ePageSerial : int { None = -1, Status = 0, Teaching, Initialen, Mainten, Setup, Log, Secs }
        enum eStatusSec : int { Status = 0, None };
        enum eTeachingSec : int { Robot = 0, RobotDataCopy, RobotMapping, Loadport1, Loadport2, Loadport3, Loadport4, Loadport5, Loadport6, Loadport7, Loadport8, AlignerOCR, NotchAngle, None };
        enum eInitialationSec : int { Orgn = 0, None };
        enum eMaintenaceSec : int { IO = 0, Manual, EQ_Command, None };
        enum eStepupSec : int { Parameter = 0, SecsSetting, Permission, GroupRecipe, SignalTower, None };
        enum eLogSec : int { Alarm = 0, Event, Process, None };
        enum eAlarmSec : int { Current = 0, None };
        enum eSecsSec : int { Manual = 0, None };
        #endregion

        #region ======================== Moving Form ======================

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MOVE = 0xF010;
        private const int HTCAPTION = 0x0002;
        private void pnlMovingWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (m_bAutoZoom) return;
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }
        #endregion

        #region ======================== Zoom Form ========================
        private bool m_bAutoZoom = true;
        private float m_nFrmX;//當前窗體的寬度
        private float m_nFrmY;//當前窗體的高度
        private bool m_bFrmLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        private void setFrmSize()
        {
            //  ==========   Operation
            ((frmMain)_afrmOperation[0]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            //  ==========   Teaching
            ((frmTeachRobot)_afrmTeaching[(int)eTeachingSec.Robot]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachRobotDataCopy)_afrmTeaching[(int)eTeachingSec.RobotDataCopy]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachRobotMapping)_afrmTeaching[(int)eTeachingSec.RobotMapping]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachLoadport)_afrmTeaching[(int)eTeachingSec.Loadport1]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachLoadport)_afrmTeaching[(int)eTeachingSec.Loadport2]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachLoadport)_afrmTeaching[(int)eTeachingSec.Loadport3]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachLoadport)_afrmTeaching[(int)eTeachingSec.Loadport4]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachLoadport)_afrmTeaching[(int)eTeachingSec.Loadport5]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachLoadport)_afrmTeaching[(int)eTeachingSec.Loadport6]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachLoadport)_afrmTeaching[(int)eTeachingSec.Loadport7]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachLoadport)_afrmTeaching[(int)eTeachingSec.Loadport8]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachOCR)_afrmTeaching[(int)eTeachingSec.AlignerOCR]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmTeachAngle)_afrmTeaching[(int)eTeachingSec.NotchAngle]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            //  ==========  Maintan
            ((frmIO)_afrmMaintenance[(int)eMaintenaceSec.IO]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmManual)_afrmMaintenance[(int)eMaintenaceSec.Manual]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmEQCommand)_afrmMaintenance[(int)eMaintenaceSec.EQ_Command]).SetGUISize(panelChilForm.Width, panelChilForm.Height);


            //  ==========  Setup
            ((frmParameter)_afrmSetup[(int)eStepupSec.Parameter]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmSECSSetting)_afrmSetup[(int)eStepupSec.SecsSetting]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmPermissionUser)_afrmSetup[(int)eStepupSec.Permission]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmGroupRecipe)_afrmSetup[(int)eStepupSec.GroupRecipe]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmSignalColor)_afrmSetup[(int)eStepupSec.SignalTower]).SetGUISize(panelChilForm.Width, panelChilForm.Height);

            //  ==========  Log
            ((frmLogAlarm)_afrmLog[(int)eLogSec.Alarm]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmLogEvent)_afrmLog[(int)eLogSec.Event]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            ((frmLogProcess)_afrmLog[(int)eLogSec.Process]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            //  ==========  Alarm
            ((frmAlarmCurrent)_afrmAlarm[(int)eAlarmSec.Current]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
            //  ==========  Secs
            ((frmSECSControl)_afrmSecs[(int)eSecsSec.Manual]).SetGUISize(panelChilForm.Width, panelChilForm.Height);
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

        private string m_strbtnStatus;
        private string m_strbtnTeaching;
        private string m_strbtnInitialen;//INITIALIZATION
        private string m_strbtnMaintenance;//MAINTENANCE
        private string m_strbtnSetup;//PARAMETER
        private string m_strbtnLog;
        private string m_strbtnSecs;
        private string m_strbtnAlarm;
        private string m_strbtnExit;
        private string m_strbtnSignIn;

        #region ======================== Select Menu ======================
        private void Menu_MouseLeave(object sender, EventArgs e)
        {
            Point pt = panelMenu.PointToScreen(new Point(0, 0));
            Rectangle rectangle = new Rectangle(pt.X, pt.Y, panelMenu.Width, panelMenu.Height);
            bool bIn = rectangle.Contains(new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y));
            //滑鼠範圍外且目前不是隱藏
            if (bIn == false)
            {
                //功能列未選擇的隱藏
                for (int i = 0; i < _panelMenu.Count; i++)
                {
                    if ((int)m_pageCurr == i) continue;
                    _panelMenu[i].Visible = false;
                }
            }

            //滑鼠離開下方選單，但進入到選單的列頁不需要隱藏
            Point pt2 = panelCommandMenu.PointToScreen(new Point(0, 0));
            Rectangle rectangle2 = new Rectangle(pt2.X, pt2.Y, panelCommandMenu.Width, panelCommandMenu.Height);
            bool bIn2 = rectangle2.Contains(new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y));
            if (bIn2 == false)
            {
                //隱藏選單
                panelCommandMenu.Visible = false;
            }

        }
        private void Menu_MouseEnter(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            Point pt = panelMenu.PointToScreen(new Point(0, 0));
            Rectangle rectangle = new Rectangle(pt.X, pt.Y, panelMenu.Width, panelMenu.Height);
            bool bIn = rectangle.Contains(new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y));
            //滑鼠範圍顯示選單
            if (btn == btnStatus)
            {
                showSubPanel(sender, panelStatusMenu);
                m_pageCurr = ePageSerial.Status;
            }
            else if (btn == btnTeaching)
            {
                showSubPanel(sender, panelTeachingMenu);
                m_pageCurr = ePageSerial.Teaching;
            }
            else if (btn == btnInitialen)
            {
                showSubPanel(sender, panelInitialenMenu);
                m_pageCurr = ePageSerial.Initialen;
            }
            else if (btn == btnMaintenance)
            {
                showSubPanel(sender, panelMaintenaceMenu);
                m_pageCurr = ePageSerial.Mainten;
            }
            else if (btn == btnSetup)
            {
                showSubPanel(sender, panelSetupMenu);
                m_pageCurr = ePageSerial.Setup;
            }
            else if (btn == btnLog)
            {
                showSubPanel(sender, panelLogMenu);
                m_pageCurr = ePageSerial.Log;
            }
            else if (btn == btnSecs)
            {
                showSubPanel(sender, panelSecsMenu);
                m_pageCurr = ePageSerial.Secs;
            }
        }
        #endregion

        #region ======================== 滑鼠沒有動自動登出 =================
        //========== idle detect
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        private static long GetIdleTick()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = Marshal.SizeOf(lastInputInfo);
            if (!GetLastInputInfo(ref lastInputInfo)) return 0;



            return Environment.TickCount - (long)lastInputInfo.dwTime;
        }
        //for 自動登出API
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }
        #endregion

        // ======================== Object unit    
        List<I_Robot> ListTRB = new List<I_Robot>();
        List<I_Loadport> ListSTG = new List<I_Loadport>();
        List<I_Aligner> ListALN = new List<I_Aligner>();
        List<I_RC5X0_IO> ListDIO = new List<I_RC5X0_IO>();
        List<I_RC5X0_Motion> ListTBL = new List<I_RC5X0_Motion>();

        List<I_E84> ListE84 = new List<I_E84>();
        List<I_Buffer> ListBUF = new List<I_Buffer>();
        List<I_OCR> ListOCR = new List<I_OCR>();

        List<I_RFID> RFIDList = new List<I_RFID>();
        List<I_BarCode> ListBCR = new List<I_BarCode>();//ForLoadport
        List<SSCamera> ListCMP = new List<SSCamera>();
        List<SSFFUCtrlParents> ListFFU = new List<SSFFUCtrlParents>();

        SmartRFID m_SmartRFID;
        SafetyIOStatus m_SafetyIO;
        Keyence_MP m_KeyenceMP;

        List<SSEquipment> ListEQM = new List<SSEquipment>();
        List<ADAM6066> ListAdam = new List<ADAM6066>();

        // ======================== Object class
        sServer _Server;
        SSECSGEMUtilty _SECSUtilty;
        MainDB _DB;
        SSSorterSQL _DataBase;
        CEIDManager _CEIDControl;
        PJCJManager _JobControl;
        VIDManager _VIDControl;
        SSECSParameter _SECSparameter;
        SGEM300 _Gem;
        SGroupRecipeManager _grouprecipe;
        SPermission _userManager;   //  管理LOGIN使用者權限
        SAlarm _alarm;              //  管理收集所有單體異常
        STransfer _autoProcess;     //  自動傳片流程

        // ShutterDoor
        private readonly Dictionary<int, (int OpenDo, int CloseDo, int OpenDi, int CloseDi)> _shutterMap
            = new Dictionary<int, (int OpenDo, int CloseDo, int OpenDi, int CloseDi)>
    {
                { 1, (OpenDo: 1, CloseDo: 0, OpenDi: 1, CloseDi: 0)},
                { 2, (OpenDo: 3, CloseDo: 2, OpenDi: 3, CloseDi: 2)},
                { 3, (OpenDo: 5, CloseDo: 4, OpenDi: 5, CloseDi: 4)},
                { 4, (OpenDo: 7, CloseDo: 6, OpenDi: 7, CloseDi: 6)},
    };
        private readonly Dictionary<int, (int nAdam, int Di_RDYtoLoad, int Di_RDYtoUnload, int Do_RDYtoLoad, int Do_RDYtoUnload)> _EQ_SMEMA_Map
            = new Dictionary<int, (int nAdam, int Di_RDYtoLoad, int Di_RDYtoUnload, int Do_RDYtoLoad, int Do_RDYtoUnload)>
    {
                { 1, (nAdam: 0, Di_RDYtoLoad: 0, Di_RDYtoUnload: 1, Do_RDYtoLoad: 0, Do_RDYtoUnload: 1)},
                { 2, (nAdam: 0, Di_RDYtoLoad: 2, Di_RDYtoUnload: 3, Do_RDYtoLoad: 2, Do_RDYtoUnload: 3)},
                { 3, (nAdam: 1, Di_RDYtoLoad: 0, Di_RDYtoUnload: 1, Do_RDYtoLoad: 0, Do_RDYtoUnload: 1)},
                { 4, (nAdam: 1, Di_RDYtoLoad: 2, Di_RDYtoUnload: 3, Do_RDYtoLoad: 2, Do_RDYtoUnload: 3)},
    };
        private int shutterDoorTimeout = 5000;



        private int m_tryPing = 0;

        private bool _bLockMenu;

        #region Object Log
        private SLogger _errorLog = SLogger.GetLogger("Errorlog");
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[MDI] {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }
        #endregion
        #region Object Form     
        private ePageSerial m_pageCurr = ePageSerial.None; //彈跳出的Panel是哪個

        private ePageSerial m_pageSelect = ePageSerial.None;
        private ePageSerial m_pageSelectLast = ePageSerial.None;


        private List<Form> _afrmOperation = new List<Form>();
        private List<Form> _afrmTeaching = new List<Form>();
        private List<Form> _afrmInitialen = new List<Form>();
        private List<Form> _afrmMaintenance = new List<Form>();
        private List<Form> _afrmSetup = new List<Form>();
        private List<Form> _afrmLog = new List<Form>();
        private List<Form> _afrmAlarm = new List<Form>();
        private List<Form> _afrmSecs = new List<Form>();

        private Dictionary<string, Form> m_dicOperation = new Dictionary<string, Form>();
        private Dictionary<string, Form> m_dicTeaching = new Dictionary<string, Form>();
        private Dictionary<string, Form> m_dicInitialen = new Dictionary<string, Form>();
        private Dictionary<string, Form> m_dicMaintenance = new Dictionary<string, Form>();
        private Dictionary<string, Form> m_dicSetup = new Dictionary<string, Form>();
        private Dictionary<string, Form> m_dicLog = new Dictionary<string, Form>();
        private Dictionary<string, Form> m_dicAlarm = new Dictionary<string, Form>();
        private Dictionary<string, Form> m_dicSecs = new Dictionary<string, Form>();

        private List<Panel> _panelMenu = new List<Panel>();

        private Form activeForm = null;
        #endregion
        #region Object Database
        private SMainDB _dbMain = new SMainDB();
        private SProcessDB _dbProcess;

        private SAlarmListDB _dbAlarmList = new SAlarmListDB();
        #endregion

        public frmMDI()
        {
            CheckMaintenaceSW();
            Form.CheckForIllegalCrossThreadCalls = false;
            GParam.theInst.DoInit();//先讀ini

            #region 轉語系
            InitializeComponent();
            switch (GParam.theInst.SystemLanguage)
            {
                case enumSystemLanguage.Default:
                case enumSystemLanguage.zn_TW:
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zn-TW");
                    break;
                case enumSystemLanguage.zh_CN:
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
                    break;
                case enumSystemLanguage.en_US:
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
                    break;
            }
            this.Controls.Clear();
            InitializeComponent();
            #endregion

            m_strbtnStatus = btnStatus.Text;
            m_strbtnTeaching = btnTeaching.Text;
            m_strbtnInitialen = btnInitialen.Text;
            m_strbtnMaintenance = btnMaintenance.Text;
            m_strbtnSetup = btnSetup.Text;
            m_strbtnLog = btnLog.Text;
            m_strbtnSecs = btnSecs.Text;
            m_strbtnAlarm = btnAlarm.Text;
            m_strbtnExit = btnExit.Text;
            m_strbtnSignIn = btnSignIn.Text;

            lblVersionValue.Text = System.Windows.Forms.Application.ProductName + Application.ProductVersion;
            //panelChilForm.Dock = DockStyle.Fill;
            panelChilForm.Size = new Size(1024, 678);//裝子窗
            panelChilForm.Location = new Point(0, pnlHeader.Size.Height);

            //建置元件
            RunDiagnostics();

            #region 右側功能視窗

            if (GParam.theInst.FreeStyle)
            {
                btnStatus.Image = Properties.Resources._32_home_;
                btnTeaching.Image = Properties.Resources._32_settings_;
                btnInitialen.Image = Properties.Resources._32_reset_;
                btnMaintenance.Image = Properties.Resources._32_support_;
                btnSetup.Image = Properties.Resources._32_document_;
                btnLog.Image = Properties.Resources._32_folder_;
                btnSecs.Image = Properties.Resources._32_automation_;
                btnAlarm.Image = Properties.Resources._32_alarm_;
                btnExit.Image = Properties.Resources._32_logout_;

                btnSignIn.Image = Properties.Resources._32_login_;
                btnBuzzerOff.Image = Properties.Resources._32_mute_;
            }

            btnStatus.MouseEnter += Menu_MouseEnter;
            btnStatus.MouseLeave += Menu_MouseLeave;
            btnTeaching.MouseEnter += Menu_MouseEnter;
            btnTeaching.MouseLeave += Menu_MouseLeave;
            btnInitialen.MouseEnter += Menu_MouseEnter;
            btnInitialen.MouseLeave += Menu_MouseLeave;
            btnMaintenance.MouseEnter += Menu_MouseEnter;
            btnMaintenance.MouseLeave += Menu_MouseLeave;
            btnSetup.MouseEnter += Menu_MouseEnter;
            btnSetup.MouseLeave += Menu_MouseLeave;
            btnLog.MouseEnter += Menu_MouseEnter;
            btnLog.MouseLeave += Menu_MouseLeave;
            btnSecs.MouseEnter += Menu_MouseEnter;
            btnSecs.MouseLeave += Menu_MouseLeave;

            btnAlarm.MouseEnter += Menu_MouseEnter;
            btnAlarm.MouseLeave += Menu_MouseLeave;
            btnExit.MouseEnter += Menu_MouseEnter;
            btnExit.MouseLeave += Menu_MouseLeave;

            /*panelStatusMenu.MouseEnter += Menu_MouseEnter;
            panelStatusMenu.MouseLeave += Menu_MouseLeave;
            panelTeachingMenu.MouseEnter += Menu_MouseEnter;
            panelTeachingMenu.MouseLeave += Menu_MouseLeave;
            panelInitialenMenu.MouseEnter += Menu_MouseEnter;
            panelInitialenMenu.MouseLeave += Menu_MouseLeave;
            panelMaintenaceMenu.MouseEnter += Menu_MouseEnter;
            panelMaintenaceMenu.MouseLeave += Menu_MouseLeave;
            panelSetupMenu.MouseEnter += Menu_MouseEnter;
            panelSetupMenu.MouseLeave += Menu_MouseLeave;
            panelLogMenu.MouseEnter += Menu_MouseEnter;
            panelLogMenu.MouseLeave += Menu_MouseLeave;
            panelSecsMenu.MouseEnter += Menu_MouseEnter;
            panelSecsMenu.MouseLeave += Menu_MouseLeave;

            panelFeaturesMenu.MouseEnter += Menu_MouseEnter;
            panelFeaturesMenu.MouseLeave += Menu_MouseLeave;
            //pnlRorzeLogo.MouseEnter += Menu_MouseEnter;
            //pnlRorzeLogo.MouseLeave += Menu_MouseLeave;
            panelMenu.MouseEnter += Menu_MouseEnter;
            panelMenu.MouseLeave += Menu_MouseLeave;*/

            _panelMenu.Add(panelStatusMenu);
            _panelMenu.Add(panelTeachingMenu);
            _panelMenu.Add(panelInitialenMenu);
            _panelMenu.Add(panelMaintenaceMenu);
            _panelMenu.Add(panelSetupMenu);
            _panelMenu.Add(panelLogMenu);
            _panelMenu.Add(panelSecsMenu);
            //  Create Buttn and add to panel foreach Form
            createPanelBtn(_afrmOperation, ePageSerial.Status, panelStatusMenu);
            createPanelBtn(_afrmTeaching, ePageSerial.Teaching, panelTeachingMenu);
            createPanelBtn(_afrmInitialen, ePageSerial.Initialen, panelInitialenMenu);
            createPanelBtn(_afrmMaintenance, ePageSerial.Mainten, panelMaintenaceMenu);
            createPanelBtn(_afrmSetup, ePageSerial.Setup, panelSetupMenu);
            createPanelBtn(_afrmLog, ePageSerial.Log, panelLogMenu);
            createPanelBtn(_afrmSecs, ePageSerial.Secs, panelSecsMenu);
            //  add to dictionary foreach Form
            listAddToDic(m_dicOperation, _afrmOperation);
            listAddToDic(m_dicTeaching, _afrmTeaching);
            listAddToDic(m_dicInitialen, _afrmInitialen);
            listAddToDic(m_dicMaintenance, _afrmMaintenance);
            listAddToDic(m_dicSetup, _afrmSetup);
            listAddToDic(m_dicLog, _afrmLog);
            listAddToDic(m_dicAlarm, _afrmAlarm);
            listAddToDic(m_dicSecs, _afrmSecs);



            Menu_MouseLeave(this, new EventArgs());

            #endregion

            #region 處理卡片初始值，包含初始IO與初速度

            _alarm.AlarmBuzzerOff();
            //  FFU RPM
            FFU_InitialSpeed();
            //  MaintSwitch 判斷Robot的速度修改
            if (IsMaintMode_EFEM())
            {
                foreach (I_Robot trb in ListTRB)
                    if (trb.Disable == false) trb.SSPD(GParam.theInst.GetRobot_MaintSpeed(trb.BodyNo - 1));
                ListDIO[2].SdobW(0, 5, true); //  告訴EQ RTI的MaintMode開啟
            }
            else
            {
                foreach (I_Robot trb in ListTRB)
                    if (trb.Disable == false) trb.SSPD(GParam.theInst.GetRobot_RunSpeed(trb.BodyNo - 1));
                ListDIO[2].SdobW(0, 5, false);// 告訴EQ RTI為RunMode
            }


            //  SECS要不要開
            if (GParam.theInst.IsSecsEnable)
                _Gem.SECSStart();
            //else
            //    btnSecs.Visible = false;
            #endregion

            System.Threading.Tasks.Task.Run(() => { checkStatus(); });

            tmrUI.Enabled = true;

            m_bAutoZoom = GParam.theInst.EnableAutoZoom;

            this.Icon = RorzeApi.Properties.Resources.R;

            if (GParam.theInst.FreeStyle)
            {
                this.Icon = RorzeApi.Properties.Resources.bwbs_;

                panel1.BackColor = GParam.theInst.ColorTitle;

                labVersion.ForeColor = lblVersionValue.ForeColor = Color.Black;
                labTime.ForeColor = labTimes.ForeColor = Color.Black;
                label3.ForeColor = labPowerMode.ForeColor = Color.Black;

                lblShowName.BackColor = GParam.theInst.ColorTitle;//客戶要顯示的設備號
                lblShowName.ForeColor = Color.Black;//客戶要顯示的設備號           

                btnStatus.BackColor = GParam.theInst.ColorButton;
                btnTeaching.BackColor = GParam.theInst.ColorButton;
                btnInitialen.BackColor = GParam.theInst.ColorButton;
                btnMaintenance.BackColor = GParam.theInst.ColorButton;
                btnSetup.BackColor = GParam.theInst.ColorButton;
                btnLog.BackColor = GParam.theInst.ColorButton;
                btnSecs.BackColor = GParam.theInst.ColorButton;
                btnAlarm.BackColor = GParam.theInst.ColorButton;
                btnExit.BackColor = GParam.theInst.ColorButton;
                btnBuzzerOff.BackColor = GParam.theInst.ColorButton;
                btnSignIn.BackColor = GParam.theInst.ColorButton;

                btnStatus.ForeColor = btnTeaching.ForeColor = btnInitialen.ForeColor = btnMaintenance.ForeColor = btnSetup.ForeColor = btnLog.ForeColor = btnSecs.ForeColor = Color.Black;
                btnAlarm.ForeColor = btnExit.ForeColor = btnBuzzerOff.ForeColor = btnSignIn.ForeColor = Color.Black;

                panelMenu.BackColor = GParam.theInst.ColorButton;
                panelFeaturesMenu.BackColor = GParam.theInst.ColorButton;

                uicimStatus1.SetFreeStyleColor(GParam.theInst.ColorTitle);
            }
        }

        private void frmMDI_Load(object sender, EventArgs e)
        {
            this.Hide();
            if (false == m_bFrmLoaded)
            {
                m_bFrmLoaded = true;// 已設定各控制項的尺寸到Tag屬性中
                SetTag(this);//調用方法
                m_nFrmX = this.Width;//獲取窗體的寬度
                m_nFrmY = this.Height;//獲取窗體的高度
            }
            float tempX = SystemInformation.PrimaryMonitorSize.Width / m_nFrmX;
            float tempY = SystemInformation.PrimaryMonitorSize.Height / m_nFrmY;
            if (m_bAutoZoom) SetControls(tempX, tempY, this);                                    //縮放功能

            uicimStatus1.Visible = GParam.theInst.IsSecsEnable;
        }
        private void frmMDI_Shown(object sender, EventArgs e)
        {
            if (m_bAutoZoom) this.WindowState = FormWindowState.Maximized;                       //縮放功能
            if (m_bAutoZoom) setFrmSize();                                                       //縮放功能

            triggerSelectFirstPage();
        }

        #region ========== private function for menu ==========
        private void showSubPanel(object sender, Panel subMenu)
        {
            //---------------------------------------

            Button btn = sender as Button;
            int nCount = subMenu.Controls.Count;
            panelCommandMenu.Location = new Point(btn.Location.X, panelMenu.Location.Y - btn.Height * nCount);
            panelCommandMenu.Width = btn.Width;
            panelCommandMenu.Height = btn.Height * nCount;
            panelCommandMenu.Visible = true;

            //---------------------------------------

            for (int i = 0; i < _panelMenu.Count; i++)
            {
                if (subMenu == _panelMenu[i])
                {
                    _panelMenu[i].Visible = true;
                }
                else if ((int)m_pageCurr == i)
                {
                    _panelMenu[i].Visible = false;
                }
                else
                {
                    _panelMenu[i].Visible = false;
                }
            }
        }
        private void openChildForm(Form childForm)
        {
            if (activeForm == childForm && childForm.Visible)
                return;
            if (activeForm != null)
                activeForm.Hide();
            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            panelChilForm.Controls.Add(childForm);
            panelChilForm.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }
        private void cleanSubPanelBtnColor()            //  所有按鈕顏色變回灰底
        {
            foreach (Panel thePanel in _panelMenu)
            {
                foreach (Button Btn in thePanel.Controls)
                {
                    if (GParam.theInst.FreeStyle)
                    {
                        Btn.BackColor = Color.FromArgb(255, 204, 127);
                        Btn.ForeColor = Color.Black;
                    }
                    else
                    {
                        Btn.BackColor = Color.DimGray;
                        Btn.ForeColor = SystemColors.Control;
                    }
                }
            }

            foreach (Button theButton in panelFeaturesMenu.Controls)
            {
                if (GParam.theInst.FreeStyle)
                {
                    theButton.BackColor = GParam.theInst.ColorOrange1;//回原本顏色   
                }
            }



        }
        private void listAddToDic(Dictionary<string, Form> m_dic, List<Form> frmList)
        {
            foreach (Form frm in frmList)
            {
                m_dic.Add(frm.Text, frm);
            }
        }
        private void createPanelBtn(List<Form> frmList, ePageSerial page, Panel pnl)
        {
            pnl.Controls.Clear();

            int nCount = 0;

            for (int nfrm = frmList.Count; nfrm > 0; nfrm--)
            {
                if (frmList[nfrm - 1].Enabled)
                {
                    nCount++;
                    Button btn = new Button();
                    btn.Text = frmList[nfrm - 1].Text;
                    btn.Dock = DockStyle.Top;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.AutoSizeMode = AutoSizeMode.GrowOnly;
                    btn.Font = new Font("Verdana", 9);
                    btn.ForeColor = SystemColors.Control;

                    btn.UseVisualStyleBackColor = true;
                    btn.Height = 40;
                    if (GParam.theInst.FreeStyle)
                    {
                        btn.BackColor = Color.FromArgb(255, 204, 127);
                        btn.ForeColor = Color.Black;
                    }
                    else
                    {
                        btn.BackColor = Color.DimGray;
                        btn.ForeColor = SystemColors.Control;
                    }
                    btn.MouseEnter += Menu_MouseEnter;
                    btn.MouseLeave += Menu_MouseLeave;

                    switch (page)
                    {
                        case ePageSerial.Status:
                            btn.Click += btnStatusMenu_Click;
                            break;
                        case ePageSerial.Teaching:
                            btn.Click += btnTeachMenu_Click;
                            break;
                        case ePageSerial.Initialen:
                            btn.Click += btnInitialenMenu_Click;
                            break;
                        case ePageSerial.Mainten:
                            btn.Click += btnMaintenMenu_Click;
                            break;
                        case ePageSerial.Setup:
                            btn.Click += btnSetupMenu_Click;
                            break;
                        case ePageSerial.Log:
                            btn.Click += btnLogMenu_Click;
                            break;
                        case ePageSerial.Secs:
                            btn.Click += btnSecsMenu_Click;
                            break;
                    }
                    pnl.Controls.Add(btn);
                }
            }
            //建立Buttun & Panel
            pnl.Height = nCount * 40;
            //_panelMenu[(int)page].Dock = DockStyle.Top;
            pnl.AutoSize = true;
        }
        private void triggerSelectFirstPage()           //  觸發顯示第一頁面
        {
            #region 觸發顯示第一頁面
            Action act = () =>
            {
                btnStatus.PerformClick();
                //  Panel 最先加入的button會再Panel最後一個
                int N = _panelMenu[(int)ePageSerial.Status].Controls.Count;
                ((Button)_panelMenu[(int)ePageSerial.Status].Controls[N - 1]).PerformClick();
            };
            Invoke(act);
            #endregion
        }
        private void disableMenuToolBar(bool bDisable)  //  Interlock 旁邊功能列
        {
            _bLockMenu = bDisable;

            //  所有的Button鎖起來，只有當下的選擇的不鎖(因為是橘色)
            foreach (Panel thePanel in _panelMenu)
            {
                for (int i = 0; i < thePanel.Controls.Count; i++)
                {
                    if (GParam.theInst.FreeStyle)
                    {
                        if (thePanel.Controls[i].BackColor == Color.FromArgb(255, 204, 127))
                            thePanel.Controls[i].Enabled = !bDisable;
                    }
                    else
                    {
                        if (thePanel.Controls[i].BackColor == Color.DimGray)
                            thePanel.Controls[i].Enabled = !bDisable;
                    }
                }
            }
        }
        private void _userManager_OnLogin(object sender, EventArgs e)
        {
            string strName = _userManager.UserID;
            int nLevel = _userManager.Level;


            if (nLevel == 1)
            {
                this.btnTeaching.Enabled = true;
                this.btnInitialen.Enabled = true;
                this.btnMaintenance.Enabled = true;
                this.btnSetup.Enabled = true;
                this.btnLog.Enabled = true;
            }
            else
            {
                this.btnTeaching.Enabled = _userManager.TeachingEnable;
                this.btnInitialen.Enabled = _userManager.InitialenEnable;
                this.btnMaintenance.Enabled = _userManager.MaintenanceEnable;
                this.btnSetup.Enabled = _userManager.SetupEnable;
                this.btnLog.Enabled = _userManager.LogRecordEnable;
            }

            _dbProcess.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "MDI", _userManager.UserID, "System", "LOGIN");
        }

        #region Button Event
        private void btnOperation_Click(object sender, EventArgs e)
        {
            if (m_pageCurr != ePageSerial.Status || panelCommandMenu.Visible == false)
            {
                showSubPanel(sender, panelStatusMenu);
                m_pageCurr = ePageSerial.Status;
            }
            else
            {
                panelCommandMenu.Visible = false;
            }
        }
        private void btnTeaching_Click(object sender, EventArgs e)
        {
            if (m_pageCurr != ePageSerial.Teaching || panelCommandMenu.Visible == false)
            {
                showSubPanel(sender, panelTeachingMenu);
                m_pageCurr = ePageSerial.Teaching;
            }
            else
            {
                panelCommandMenu.Visible = false;
            }
        }
        private void btnORGN_Click(object sender, EventArgs e)
        {
            if (m_pageCurr != ePageSerial.Initialen || panelCommandMenu.Visible == false)
            {
                showSubPanel(sender, panelInitialenMenu);
                m_pageCurr = ePageSerial.Initialen;
            }
            else
            {
                panelCommandMenu.Visible = false;
            }
        }
        private void btnMaintenance_Click(object sender, EventArgs e)
        {
            if (m_pageCurr != ePageSerial.Mainten || panelCommandMenu.Visible == false)
            {
                showSubPanel(sender, panelMaintenaceMenu);
                m_pageCurr = ePageSerial.Mainten;
            }
            else
            {
                panelCommandMenu.Visible = false;
            }
            //  特殊功能只給我們軟體用
            foreach (Button btn in panelMaintenaceMenu.Controls)
            {
                if (btn.Text == "EQ Command")
                    btn.Visible = (_userManager.Level == 1);
            }
        }
        private void btnSetup_Click(object sender, EventArgs e)
        {
            if (m_pageCurr != ePageSerial.Setup || panelCommandMenu.Visible == false)
            {
                showSubPanel(sender, panelSetupMenu);
                m_pageCurr = ePageSerial.Setup;
            }
            else
            {
                panelCommandMenu.Visible = false;
            }
        }
        private void btnLog_Click(object sender, EventArgs e)
        {
            if (m_pageCurr != ePageSerial.Log || panelCommandMenu.Visible == false)
            {
                showSubPanel(sender, panelLogMenu);
                m_pageCurr = ePageSerial.Log;
            }
            else
            {
                panelCommandMenu.Visible = false;
            }
        }
        private void btnSecs_Click(object sender, EventArgs e)
        {
            if (m_pageCurr != ePageSerial.Secs || panelCommandMenu.Visible == false)
            {
                showSubPanel(sender, panelSecsMenu);
                m_pageCurr = ePageSerial.Secs;
            }
            else
            {
                panelCommandMenu.Visible = false;
            }
        }
        private void btnAlarm_Click(object sender, EventArgs e)
        {
            //  功能列 subPanel隱藏並且panel裡面的按鈕變成灰色
            cleanSubPanelBtnColor();
            openChildForm(_afrmAlarm[(int)eAlarmSec.Current]);
        }
        private void btnSignIn_Click(object sender, EventArgs e)
        {
            #region 觸發顯示第一頁面
            triggerSelectFirstPage();
            #endregion

            if (_userManager.IsLogin == false)
            {
                frmUserLogIn login = new frmUserLogIn(_userManager, _dbMain);
                // 由_userManager判斷登入成功

                bool bSucc = (DialogResult.OK == login.ShowDialog());
                if (bSucc)
                {
                    //btnSignIn.Text = "LOGOUT";
                    btnSignIn.Text = _userManager.UserID;
                    if (GParam.theInst.FreeStyle)
                        btnSignIn.BackColor = GParam.theInst.ColorTitle;
                    else
                        btnSignIn.BackColor = Color.Orange;
                }
            }
            else
            {
                _dbProcess.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "MDI", _userManager.UserID, "System", "LOGOUT");

                _userManager.Logout();

                this.btnTeaching.Enabled = false;
                this.btnInitialen.Enabled = false;
                this.btnMaintenance.Enabled = false;
                this.btnSetup.Enabled = false;
                this.btnLog.Enabled = false;

                btnSignIn.Text = m_strbtnSignIn;
                if (GParam.theInst.FreeStyle)
                    btnSignIn.BackColor = GParam.theInst.ColorButton;
                else
                    btnSignIn.BackColor = SystemColors.ActiveCaptionText;

            }
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsProcess())
                {
                    frmMessageBox frmMbox = new frmMessageBox(string.Format("Loader StatusMachine is PS_Process.Do you want to Exit?"), "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (frmMbox.ShowDialog() != System.Windows.Forms.DialogResult.Yes)
                    { return; }
                }

                if (_userManager.IsLogin == false && GParam.theInst.IsSimulate == false)
                {
                    new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }

                frmPassword myDlgPwd = new frmPassword();
                if (DialogResult.OK == myDlgPwd.ShowDialog() && myDlgPwd.GetPassWord == "1")
                {
                    if (GParam.theInst.IsSecsEnable)
                    {
                        if (_Gem.GetSECSDriver.GetSecsStarted())
                            _Gem.GetSECSDriver.SecsStop();
                    }

                    foreach (I_Loadport item in ListSTG)
                    {
                        GParam.theInst.SetSimulateGmap(item.BodyNo - 1, item.MappingData);
                    }

                    this.Close();
                    Environment.Exit(Environment.ExitCode);
                }
            }
            catch { }
        }
        private void btnBuzzerOff_Click(object sender, EventArgs e)
        {
            try
            {
                if (_userManager.IsLogin == false && GParam.theInst.IsSimulate == false)
                {
                    new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }

                _alarm.AlarmBuzzerOff();
            }
            catch
            {

            }
        }
        // ======================================================================
        private void btnStatusMenu_Click(object sender, EventArgs e)
        {
            cleanSubPanelBtnColor();  //  所有按鈕顏色變回灰底
            Button btn = (Button)sender;
            btn.BackColor = Color.DarkOrange;
            openChildForm(m_dicOperation[btn.Text]);
            if (m_pageCurr != ePageSerial.Status)
            {
                //  關閉上一次開啟的Panel       
                _panelMenu[(int)m_pageCurr].Visible = false;
            }
            panelCommandMenu.Visible = false;
            btnStatus.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorOrange3 : Color.Black;
            m_pageSelect = ePageSerial.Status;
        }
        private void btnTeachMenu_Click(object sender, EventArgs e)
        {
            #region 檢查異常           
            // 異常不應該切畫面
            if (_alarm != null && _alarm.IsAlarm() && _alarm.IsOnlyWarning() == false)
            {
                new frmMessageBox("Please reset eror. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            //  檢查 Secs ONLINEREMOTE          
            if (_Gem != null && _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                new frmMessageBox("Now control status is Online Remote. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            //  檢查傳送中不能切畫面
            if (IsProcess())
            {
                new frmMessageBox("_loader StatusMachine is PS_Process. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            // RC530 RunMode
            if (IsMaintMode_EFEM() == false)
            {
                new frmMessageBox("Is RunMode!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            #endregion

            cleanSubPanelBtnColor();
            Button btn = (Button)sender;
            btn.BackColor = Color.DarkOrange;
            openChildForm(m_dicTeaching[btn.Text]);
            if (m_pageCurr != ePageSerial.Teaching)
            {
                //  關閉上一次開啟的Panel             
                _panelMenu[(int)m_pageCurr].Visible = false;
            }
            panelCommandMenu.Visible = false;
            btnTeaching.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorOrange3 : Color.Black;
            m_pageSelect = ePageSerial.Teaching;
        }
        private void btnInitialenMenu_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            #region 檢查異常           
            // 異常不應該切畫面
            if (_alarm != null && _alarm.IsAlarm() && _alarm.IsOnlyWarning() == false)
            {
                new frmMessageBox("Please reset eror. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            //  檢查 Secs ONLINEREMOTE          
            if (_Gem != null && _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                new frmMessageBox("Now control status is Online Remote. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            //  檢查傳送中不能切畫面
            if (IsProcess())
            {
                new frmMessageBox("_loader StatusMachine is PS_Process. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            #endregion

            cleanSubPanelBtnColor();

            if (new frmMessageBox(string.Format("Are you sure you want to run the initialization?"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() == System.Windows.Forms.DialogResult.Yes)
            {
                m_pageSelect = ePageSerial.Initialen;
                this.Hide();

                frmOrgn _frmOrgn = new frmOrgn(ListTRB, ListSTG, ListALN, ListBUF, ListEQM, GParam.theInst.IsSimulate);
                bool bSucc = (DialogResult.OK == _frmOrgn.ShowDialog());
                if (bSucc)
                {
                    _JobControl.PJCJReset();//要清除上一次跑一半中斷的

                    try
                    {
                        ExecuteRecover();
                    }
                    catch (Exception ex) { WriteLog("<Exception> : " + ex); }
                    //  MaintSwitch 判斷Robot的速度修改
                    foreach (I_Robot trb in ListTRB)
                        if (trb.Disable == false)
                            trb.SSPD(IsMaintMode_EFEM() ? GParam.theInst.GetRobot_MaintSpeed(trb.BodyNo - 1) : GParam.theInst.GetRobot_RunSpeed(trb.BodyNo - 1));
                }
                else
                {
                    new frmMessageBox("Origin failed the system will shut down.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    Environment.Exit(Environment.ExitCode);
                }

                this.Show();
                triggerSelectFirstPage();//回到主畫面

            }

        }
        private void btnMaintenMenu_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            #region 檢查異常      
            if (btn.Text != GParam.theInst.GetLanguage("IO"))
            {

                //  檢查 Secs ONLINEREMOTE          
                if (_Gem != null && _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
                //  檢查傳送中不能切畫面
                if (IsProcess() && btn.Text != "GRPC")
                {
                    new frmMessageBox("_loader StatusMachine is PS_Process. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
                // 異常不應該切畫面
                if (_alarm != null && _alarm.IsAlarm() && _alarm.IsOnlyWarning() == false)
                {
                    new frmMessageBox("Please reset eror. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
            }
            #endregion

            cleanSubPanelBtnColor();

            btn.BackColor = Color.DarkOrange;
            openChildForm(m_dicMaintenance[btn.Text]);
            if (m_pageCurr != ePageSerial.Mainten)
            {
                _panelMenu[(int)m_pageCurr].Visible = false;
            }
            panelCommandMenu.Visible = false;
            btnMaintenance.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorOrange3 : Color.Black;
            m_pageSelect = ePageSerial.Mainten;
        }
        private void btnSetupMenu_Click(object sender, EventArgs e)
        {
            #region 檢查異常           
            // 異常不應該切畫面
            if (_alarm != null && _alarm.IsAlarm() && _alarm.IsOnlyWarning() == false)
            {
                new frmMessageBox("Please reset eror. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            //  檢查 Secs ONLINEREMOTE          
            if (_Gem != null && _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                new frmMessageBox("Now control status is Online Remote. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            //  檢查傳送中不能切畫面
            if (IsProcess())
            {
                new frmMessageBox("_loader StatusMachine is PS_Process. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            #endregion

            cleanSubPanelBtnColor();
            Button btn = (Button)sender;
            btn.BackColor = Color.DarkOrange;
            openChildForm(m_dicSetup[btn.Text]);
            if (m_pageCurr != ePageSerial.Setup)
            {
                _panelMenu[(int)m_pageCurr].Visible = false;
            }
            panelCommandMenu.Visible = false;
            btnSetup.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorOrange3 : Color.Black;
            m_pageSelect = ePageSerial.Mainten;
        }
        private void btnLogMenu_Click(object sender, EventArgs e)
        {
            #region 檢查異常           
            //// 異常不應該切畫面
            //if (_alarm != null && _alarm.IsAlarm() && _alarm.IsOnlyWarning() == false)
            //{
            //    new frmMessageBox("Please reset eror. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
            //    return;
            //}
            ////  檢查 Secs ONLINEREMOTE          
            //if (_Gem != null && _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            //{
            //    new frmMessageBox("Now control status is Online Remote. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
            //    return;
            //}
            ////  檢查傳送中不能切畫面
            //if (IsProcess())
            //{
            //    new frmMessageBox("_loader StatusMachine is PS_Process. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
            //    return;
            //}
            ////TkeyOn
            //if (IsTkeyOn_Stocker())
            //{
            //    new frmMessageBox("Is T-key ON!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
            //    return;
            //}
            #endregion

            cleanSubPanelBtnColor();
            Button btn = (Button)sender;
            btn.BackColor = Color.DarkOrange;
            openChildForm(m_dicLog[btn.Text]);
            if (m_pageCurr != ePageSerial.Log)
            {
                _panelMenu[(int)m_pageCurr].Visible = false;
            }
            panelCommandMenu.Visible = false;
            btnLog.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorOrange3 : Color.Black;
            m_pageSelect = ePageSerial.Log;
        }
        private void btnSecsMenu_Click(object sender, EventArgs e)
        {
            #region 檢查異常      
            if (_userManager.IsLogin == false && GParam.theInst.IsSimulate == false)
            {
                new frmMessageBox("Please login first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            // 異常不應該切畫面
            if (_alarm != null && _alarm.IsAlarm() && _alarm.IsOnlyWarning() == false)
            {
                new frmMessageBox("Please reset eror. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            //  檢查傳送中不能切畫面
            if (IsProcess())
            {
                new frmMessageBox("_loader StatusMachine is PS_Process. ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            #endregion

            cleanSubPanelBtnColor();
            Button btn = (Button)sender;
            btn.BackColor = Color.DarkOrange;
            openChildForm(m_dicSecs[btn.Text]);
            if (m_pageCurr != ePageSerial.Secs)
            {
                _panelMenu[(int)m_pageCurr].Visible = false;
            }
            panelCommandMenu.Visible = false;
            btnSecs.BackColor = GParam.theInst.FreeStyle ? GParam.theInst.ColorOrange3 : Color.Black;
            m_pageSelect = ePageSerial.Secs;
        }
        #endregion

        #endregion

        private void checkStatus()
        {
            for (int i = 0; i < Enum.GetNames(typeof(enumLoadport)).Count(); i++)//8
            {
                I_Loadport stg = ListSTG[i];
                I_E84 e84 = ListE84[i];

                if (false == stg.Disable && e84.Disable == false)
                {
                    ListSTG[i].SetFoupExistChenge();
                    ListE84[i].ClearSignal();
                }
            }
        }

        private void RunDiagnostics() //運行診斷
        {
            bool bSimulate = GParam.theInst.IsSimulate;

            frmLoading frmloading = new frmLoading();
            Thread thread = new Thread(new ThreadStart(() => { frmloading.ShowDialog(); }));
            thread.IsBackground = true;
            thread.Start();

            try
            {
                string str;
                #region =========================== 檢查Database ====================================
                {
                    frmloading.AddMessage("Constructing Database Object.");
                    _dbMain.Open();
                    WriteLog("Main Database is Connected!!");
                    _dbProcess = new SProcessDB(30);
                    _dbProcess.Open();
                    WriteLog("AccessDBlog Database is Connected!!");
                    _dbAlarmList.Open();
                    WriteLog("AlarmList Database is Connected!!");
                    SWafer._dbWafer.Open();
                }
                #endregion
                #region =========================== 檢查Permission Manager ==========================             
                {
                    str = "Constructing Permission Manager Object.";
                    frmloading.AddMessage(str);
                    _userManager = new SPermission(_dbMain);
                    _userManager.OnLogin -= _userManager_OnLogin;
                    _userManager.OnLogin += _userManager_OnLogin;
                    WriteLog(str);
                }
                #endregion
                #region =========================== 建構Recipe ======================================              
                {
                    str = "Constructing Group Recipe Manager Object.";
                    frmloading.AddMessage(str);
                    _grouprecipe = new SGroupRecipeManager(_dbMain);
                    WriteLog(str);
                }
                #endregion
                #region =========================== 建構Rorze Server ================================             
                {
                    string strIP = bSimulate ? "127.0.0.1" : GParam.theInst.GetServerIP;
                    int nPort = GParam.theInst.GetServerPort;
                    try
                    {
                        str = "Constructing Rorze Socket Server Object.";
                        frmloading.AddMessage(str);
                        _Server = new sServer(strIP, nPort);
                        WriteLog(str);
                    }
                    catch (SocketException ex)
                    {
                        string strFail = string.Format("<{0}>Socket Exception\rCode:{1}\r{2}\rIP:{3}-{4}", "Rorze Server", ex.SocketErrorCode, ex.Message, strIP, nPort);
                        new frmMessageBox(strFail, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        WriteLog(strFail);
                    }
                }
                #endregion

                #region =========================== 建構DIO =========================================               
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumIOModule)).Count(); i++)//6
                    {
                        str = "Constructing DIO" + i;
                        frmloading.AddMessage(str);
                        enumIOModuleType eType = GParam.theInst.GetDioType(i);
                        I_RC5X0_IO dio = null;
                        switch (eType)
                        {
                            case enumIOModuleType.RC530:
                                dio = new SSRC530_IO(GParam.theInst.GetDioIP(i), 12000, i, GParam.theInst.IsUnitDisable(enumUnit.DIO0 + i), bSimulate);
                                break;
                            case enumIOModuleType.RC550:
                                dio = new SSRC550_IO(GParam.theInst.GetDioIP(i), 12000, i, GParam.theInst.IsUnitDisable(enumUnit.DIO0 + i), bSimulate);
                                break;
                            default:
                                break;
                        }
                        ListDIO.Add(dio);
                        WriteLog(str);
                    }
                }
                #endregion             
                #region =========================== 建構E84 =========================================             
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumLoadport)).Count(); i++)//8
                    {
                        str = "Constructing E84_" + (i + 1);
                        frmloading.AddMessage(str);

                        I_E84 e84 = null;
                        switch (GParam.theInst.E84Type)
                        {
                            case enumE84Type.SB058:
                                e84 = new SB058_E84(i + 1, ListDIO[0], GParam.theInst.IsE84Disable(i), 400 + i * 2, bSimulate);
                                e84.SetTpTime = GParam.theInst.GetTpTime();
                                e84.dlgAreaTrigger += () => { return IsArea1Trigger(); };
                                break;
                            case enumE84Type.FITC:
                                e84 = new FITC_E84(i + 1, GParam.theInst.IsE84Disable(i), GParam.theInst.GetFITCComport(i), bSimulate, true);
                                e84.SetTpTime = GParam.theInst.GetTpTime();
                                e84.dlgAreaTrigger += () => { return IsArea1Trigger(); };
                                break;
                        }
                        ListE84.Add(e84);
                        WriteLog(str);
                    }
                }
                #endregion
                #region =========================== 建構OCR =========================================               
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumOCR)).Count(); i++)//4
                    {
                        str = "Constructing OCR" + (i + 1);
                        frmloading.AddMessage(str);

                        enumName eName = enumName.A1 + i;
                        string strIP = GParam.theInst.GetOcrIP(i);
                        bool bDisable = GParam.theInst.IsUnitDisable(enumUnit.OCRA1 + i);
                        enumOcrType eType = GParam.theInst.GetOcrType(i);
                        I_OCR ocr = null;
                        switch (eType)
                        {
                            case enumOcrType.IS1740:
                                ocr = new OCR_IS1740(eName, strIP, bDisable, bSimulate);
                                break;
                            case enumOcrType.WID120:
                                ocr = new HTTWID120(eName, strIP, bDisable, bSimulate);
                                break;
                            case enumOcrType.TZ0031:
                                ocr = new OCR_TZ(eName,i+1, strIP, bDisable, bSimulate);
                                break;
                            default:
                                break;
                        }
                        ListOCR.Add(ocr);
                        WriteLog(str);
                    }
                }
                #endregion
                #region =========================== 建構RFID ========================================
                for (int i = 0; i < 8; i++)
                {
                    int nComport = GParam.theInst.GetRfidComport(i);
                    str = string.Format("Constructing RFID{0} com{1}.", i + 1, nComport);
                    frmloading.AddMessage(str);
                    I_RFID rfid = null;
                    if (nComport > 0)//有設定才new
                    {

                        WriteLog(str);
                        switch (GParam.theInst.GetRFIDType)
                        {
                            case enumRFID.UNISON: rfid = new RFID_Unison(nComport, bSimulate); break;
                            case enumRFID.HEART: rfid = new RFID_Heart(nComport, bSimulate); break;
                            case enumRFID.OMRON: rfid = new RFID_Omron(nComport, bSimulate); break;
                            case enumRFID.BRILLIAN: rfid = new RFID_Brillian(nComport, bSimulate); break;
                        }
                        if (!bSimulate && !rfid.IsConnect())
                        {
                            string strFail = "[RFID comport " + nComport + "] failed to construct, please confirm whether the parameters are correct!!";
                            new frmMessageBox(strFail, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                            WriteLog(strFail);
                        }
                    }
                    RFIDList.Add(rfid);
                }
                #endregion
                #region =========================== 建構Smart RFID ==================================     
                {
                    string strIP = bSimulate ? "127.0.0.1" : GParam.theInst.GetSmartRFID_IP();
                    int nPort = GParam.theInst.GetSmartRFID_Port();
                    try
                    {
                        frmloading.AddMessage("Constructing Smart RFID");
                        sServer server = new sServer(strIP, nPort);

                        m_SmartRFID = new SmartRFID(GParam.theInst.GetSmartRFID_RfidIP(), bSimulate, GParam.theInst.GetSmartRFID_Disable(), server);

                        m_SmartRFID.OnReadID += (object sender, string strNmae) =>
                                                  {
                                                      if (m_pageCurr != ePageSerial.Setup)
                                                          this.BeginInvoke(new Action(() =>
                                                          {
                                                              if (_userManager.Login("smartrfid", strNmae.Trim()))
                                                              {
                                                                  btnSignIn.Text = _userManager.UserID;
                                                                  btnSignIn.BackColor = Color.Orange;
                                                              }

                                                          }));
                                                  };
                        if (m_SmartRFID._Disable == false)
                        {
                            server.ServerStart();
                        }
                    }
                    catch (SocketException ex)
                    {
                        string strFail = string.Format("<{0}>Socket Exception\rCode:{1}\r{2}\rIP:{3}-{4}", "Smart RFID", ex.SocketErrorCode, ex.Message, strIP, nPort);
                        new frmMessageBox(strFail, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        WriteLog(strFail);
                    }
                }
                #endregion
                #region =========================== 建構Safety IOStatus =============================           
                {
                    string strIP = bSimulate ? "127.0.0.1" : GParam.theInst.GetSafetyIOStatus_IP();
                    int nPort = GParam.theInst.GetSafetyIOStatus_Port();
                    try
                    {
                        //自動帶起Safety IOStatus
                        //ActiveSafetyIOStatus();
                        frmloading.AddMessage("Constructing Safety IOStatus");
                        sServer server = new sServer(strIP, nPort);

                        m_SafetyIO = new SafetyIOStatus(GParam.theInst.GetSafetyIOStatus_PlcIP(), bSimulate, GParam.theInst.GetSafetyIOStatus_Disable(), server);
                        if (m_SafetyIO._Disable == false)
                            server.ServerStart();
                    }
                    catch (SocketException ex)
                    {
                        string strFail = string.Format("<{0}>Socket Exception\rCode:{1}\r{2}\rIP:{3}-{4}", "PLC Safety", ex.SocketErrorCode, ex.Message, strIP, nPort);
                        new frmMessageBox(strFail, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        WriteLog(strFail);
                    }
                }
                #endregion
                #region =========================== 建構Keyence_MP ==================================
                {
                    frmloading.AddMessage("Constructing Keyence_MP");
                    m_KeyenceMP = new Keyence_MP(GParam.theInst.GetKeyenceMP_IP(), GParam.theInst.GetKeyenceMP_Port(), bSimulate, GParam.theInst.GetKeyenceMP_Disable());
                }
                #endregion
                #region =========================== 建構KeyenceDL_RS1A ==============================
                {
                    int nComport = GParam.theInst.GetKeyence_DL_RS1A_Comport();
                    Keyence_DL_RS1A DL_RS1A = new Keyence_DL_RS1A(nComport, bSimulate);
                    if (nComport > 0)//Disable判斷
                        DL_RS1A.StartCommunication();
                }
                #endregion
                #region =========================== 建構SIMCO =======================================
                {
                    string strIP = bSimulate ? "127.0.0.1" : GParam.theInst.GetSimco_IP();
                    int nPort = GParam.theInst.GetSimco_Port();
                    try
                    {
                        RorzeUnit.Class.SIMCO.Simco simco = new RorzeUnit.Class.SIMCO.Simco(strIP, nPort, bSimulate, GParam.theInst.GetSimco_Disable());
                    }
                    catch (SocketException ex)
                    {
                        string strFail = string.Format("<{0}>Socket Exception\rCode:{1}\r{2}\rIP:{3}-{4}", "SIMCO", ex.SocketErrorCode, ex.Message, strIP, nPort);
                        new frmMessageBox(strFail, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        WriteLog(strFail);
                    }
                }
                #endregion


                #region =========================== 建構FFU =========================================    
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumFFU)).Count(); i++)//2
                    {
                        str = "Constructing FFU_" + (i + 1);
                        frmloading.AddMessage(str);
                        enumFfuType eType = GParam.theInst.GetFfuType(i);
                        int nFanCount = GParam.theInst.GetFfuFanCount(i);
                        SSFFUCtrlParents ffu = null;

                        switch (eType)
                        {
                            case enumFfuType.TOPWELL:
                                ffu = new SSFFU_TOPWELL(i + 1, nFanCount, bSimulate, false, GParam.theInst.GetFfuComport(i));
                                break;
                            case enumFfuType.AirTech:
                                ffu = new SSFFU_AirTech(i + 1, nFanCount, bSimulate, false, GParam.theInst.GetFfuIP(i), 502);
                                System.Threading.Tasks.Task.Run(() => { ffu.ToConnect(); });
                                break;
                            default:
                                break;
                        }

                        ListFFU.Add(ffu);//EFEM
                    }
                }
                #endregion
                #region =========================== 建構BarCode =====================================            
                for (int i = 0; i < Enum.GetNames(typeof(enumBarcode)).Count(); i++)//8
                {
                    str = string.Format("Constructing BarCode{0}.", i + 1);
                    frmloading.AddMessage(str);
                    I_BarCode barcode = null;
                    switch (GParam.theInst.GetBarcodeType(i))
                    {
                        case enumBarcodeType.KeyenceSR2000:
                            barcode = new SR1000(GParam.theInst.GetBarcodeIP(i), 9004, false, bSimulate);
                            break;
                        case enumBarcodeType.CognexDM370:
                            barcode = new DM370(GParam.theInst.GetBarcodeIP(i), i + 1, false, bSimulate);
                            break;
                        case enumBarcodeType.KeyenceSR710:
                            int nComport = GParam.theInst.GetBarCodeComport(i);
                            barcode = new BarcodeReder(nComport);
                            break;
                    }
                    ListBCR.Add(barcode);
                }
                #endregion
                #region =========================== 建構Camera ======================================            
                for (int i = 0; i < Enum.GetNames(typeof(enumCamera)).Count(); i++)//2
                {
                    str = string.Format("Constructing Camera{0}.", i + 1);
                    frmloading.AddMessage(str);
                    SSCamera camera = null;
                    switch (GParam.theInst.GetCameraType(i))
                    {
                        case enumCameraType.NPD:
                            camera = new SSCamera(GParam.theInst.GetBarcodeIP(i), 12000, i + 1, false, bSimulate);
                            break;
                    }
                    ListCMP.Add(camera);
                }
                #endregion

                #region =========================== 建構BUF =========================================
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumBuffer)).Count(); i++)//2
                    {
                        str = "Constructing Buffer_" + (i + 1);
                        frmloading.AddMessage(str);


                        if (Enum.IsDefined(typeof(SWafer.enumWaferSize), GParam.theInst.GetBufferWaferType(i)) == false)
                        {
                            throw new Exception("Buffer Wafer Type Setting Error!");
                        }
                        SWafer.enumWaferSize eBufferWaferType = (SWafer.enumWaferSize)GParam.theInst.GetBufferWaferType(i);

                        I_Buffer buffer = new SSBuffer(i + 1, GParam.theInst.IsUnitDisable(enumUnit.BUF1 + i), bSimulate, GParam.theInst.GetBufEnableSlot(i), eBufferWaferType, 2);
                        SWafer.enumPosition ePosition = SWafer.enumPosition.BufferA + i;
                        buffer.OnAssignWaferData += (object sender, WaferDataEventArgs e) => { if (e.Wafer != null) e.Wafer.Position = ePosition; };
                        //註冊IO
                        if (buffer.Disable == false)
                        {
                            int nBody = buffer.BodyNo;
                            //注意sensor邏輯
                            for (int j = 0; j < GParam.theInst.GetBufEnableSlot(i).Length; j++)//j=0~3,slot1~4
                            {

                                if (GParam.theInst.GetBufEnableSlot(i)[j] == '0')
                                {
                                    continue;//disable不需要註冊
                                }

                                //bit 8 9 10 11    
                                //硬體上面那片是1下面是2
                                //軟體下面是slot1                                
                                /*int nBit = nBody == 1 ? 9 : 11;
                                nBit = nBit - j;*/

                                int nBit = GParam.theInst.GetBufferSlotRc530Bit(i)[j];//v1.006

                                buffer.dlgSlotWaferExist[j] += () =>
                                {
                                    if (ListDIO[4] != null && !ListDIO[4].Disable)
                                    {
                                        bool bValue = ListDIO[4].GetGDIO_InputStatus(0, nBit);//對IO表 有東西true                                    
                                        return bValue;
                                    }
                                    return false;
                                };
                            }


                            int nAround1Bit = GParam.theInst.GetBufferAroundRc530Bit(i)[0];//v1.006
                            int nAround2Bit = GParam.theInst.GetBufferAroundRc530Bit(i)[1];//v1.006
                            int nAround3Bit = GParam.theInst.GetBufferAroundRc530Bit(i)[2];//v1.006
                            int nAround4Bit = GParam.theInst.GetBufferAroundRc530Bit(i)[3];//v1.006

                            buffer.dlgAroundTrigger += () =>
                            {//注意sensor邏輯

                                /*if (ListDIO[4] != null && !ListDIO[4].Disable && nBody == 1)
                                {
                                    bool bValue1 = ListDIO[4].GetGDIO_InputStatus(0, 0) == false;//對IO表 false trigger
                                    bool bValue2 = ListDIO[4].GetGDIO_InputStatus(0, 1) == false;//對IO表
                                    bool bValue3 = ListDIO[4].GetGDIO_InputStatus(0, 2) == false;//對IO表
                                    bool bValue4 = ListDIO[4].GetGDIO_InputStatus(0, 3) == false;//對IO表
                                    return (bValue1 || bValue2 || bValue3 || bValue4);
                                }
                                else if (ListDIO[4] != null && !ListDIO[4].Disable && nBody == 2)
                                {
                                    bool bValue1 = ListDIO[4].GetGDIO_InputStatus(0, 4) == false;//對IO表 false trigger
                                    bool bValue2 = ListDIO[4].GetGDIO_InputStatus(0, 5) == false;//對IO表
                                    bool bValue3 = ListDIO[4].GetGDIO_InputStatus(0, 6) == false;//對IO表
                                    bool bValue4 = ListDIO[4].GetGDIO_InputStatus(0, 7) == false;//對IO表
                                    return (bValue1 || bValue2 || bValue3 || bValue4);
                                }                                
                                return false;
                                 */


                                if (ListDIO[4] != null && !ListDIO[4].Disable) return false;//v1.006
                                                                                            //對IO表 false trigger
                                if (SpinWait.SpinUntil(() => ListDIO[4].GetGDIO_InputStatus(0, nAround1Bit) &&
                                    ListDIO[4].GetGDIO_InputStatus(0, nAround2Bit) &&
                                    ListDIO[4].GetGDIO_InputStatus(0, nAround3Bit) &&
                                    ListDIO[4].GetGDIO_InputStatus(0, nAround4Bit), 1000))
                                {
                                    //四個都沒觸發
                                    return false;//v1.006
                                }
                                else
                                {
                                    //有人觸發超過1秒
                                    return true;//v1.006
                                }

                            };
                        }
                        ListBUF.Add(buffer);
                        WriteLog(str);
                    }
                }
                #endregion
                #region =========================== 建構Vibration====================================
                //固定使用Port = 6341 & 6342
                //SSVibration vibration = new SSVibration("127.0.0.1", 6341, 1, false, bSimulate);

                //vibration.OpenConnect();
                #endregion

                #region =========================== 建構TBL =========================================               
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumTBLModule)).Count(); i++)
                    {
                        str = "Constructing TBL" + i;
                        frmloading.AddMessage(str);
                        I_RC5X0_Motion tbl = null;
                        switch (GParam.theInst.GetTblType(i))
                        {
                            case enumTblType.RC560:
                                tbl = new SSRC560_Motion(GParam.theInst.GetTblIP(i), 12100, i + 1, GParam.theInst.IsUnitDisable(enumUnit.TBL1 + i), bSimulate);
                                break;
                            case enumTblType.RC550:
                                tbl = new SSRC550_Motion(GParam.theInst.GetTblIP(i), 12100, i + 1, GParam.theInst.IsUnitDisable(enumUnit.TBL1 + i), bSimulate);
                                break;
                        }
                        ListTBL.Add(tbl);
                        WriteLog(str);
                    }
                }
                #endregion
                #region =========================== 建構TRB =========================================             
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumRobot)).Count(); i++)//2
                    {

                        str = "Constructing Robot_" + (i + 1);
                        frmloading.AddMessage(str);
                        string strIP = GParam.theInst.GetTrbIP(i);
                        bool bDisable = GParam.theInst.IsUnitDisable(enumUnit.TRB1 + i);
                        bool bXaxsDisable = GParam.theInst.GetRobot_XaxsDisable(i);
                        bool bExtXaxsDisable = GParam.theInst.GetRobot_ExtXaxsDisable(i);
                        enumArmFunction eUpperArmFnc = GParam.theInst.GetRobot_UpperArmWaferType(i);
                        enumArmFunction eLowerArmFnc = GParam.theInst.GetRobot_LowerArmWaferType(i);
                        int nFrameArmBackPulse = GParam.theInst.GetRobot_FrameTwoStepLoadArmBackPulse(i);
                        bool bUseArmSameMovement = GParam.theInst.GetRobot_UseArmSameMovement(i);
                        string strAllowPort = GParam.theInst.GetRobot_AllowPort(i);
                        string strAllowAligner = GParam.theInst.GetRobot_AllowAligner(i);
                        string strAllowEquipment = GParam.theInst.GetRobot_AllowEquipment(i);

                        I_Robot robot;
                        I_RC5X0_Motion ExtXMotion;

                        if (bExtXaxsDisable)
                            ExtXMotion = null;
                        else //此案預設使用560當X軸軸卡
                            ExtXMotion = ListTBL[i];
                        if (GParam.theInst.GetTRB_TCPType(i) == enumTCPType.Client)
                        {
                            robot = new SSRobotRR75x(strIP, 12000, i + 1, bDisable, bSimulate, bXaxsDisable, eUpperArmFnc, eLowerArmFnc, nFrameArmBackPulse, bUseArmSameMovement, strAllowPort, strAllowAligner, strAllowEquipment, null, ExtXMotion as SSRC560_Motion, _Server);
                        }
                        else
                        {
                            robot = new SSRobotRR75x(strIP, 12000, i + 1, bDisable, bSimulate, bXaxsDisable, eUpperArmFnc, eLowerArmFnc, nFrameArmBackPulse, bUseArmSameMovement, strAllowPort, strAllowAligner, strAllowEquipment, null, ExtXMotion as SSRC560_Motion);
                        }
                        //Assign wafer data to upper arm after modify position info.
                        robot.OnAssignUpperArmWaferData += (object sender, WaferDataEventArgs e) => { e.Wafer.Position = SWafer.enumPosition.UpperArm; };
                        robot.OnAssignLowerArmWaferData += (object sender, WaferDataEventArgs e) => { e.Wafer.Position = SWafer.enumPosition.LowerArm; };

                        robot.OnLoadComplete += OnRobotLoadComplete;
                        robot.OnUnldComplete += OnRobotUnldComplete;
                        robot.OnLoadExchangeComplete += OnLoadExchangeComplete;

                        robot.GetFromLoaderStagIndx = (object oEnumFrom, int nSlot) =>
                        {
                            SWafer.enumFromLoader eFromLoader = (SWafer.enumFromLoader)oEnumFrom;

                            int nStgeIndx;
                            switch (eFromLoader)//改成指定目標
                            {
                                case SWafer.enumFromLoader.LoadportA:
                                case SWafer.enumFromLoader.LoadportB:
                                case SWafer.enumFromLoader.LoadportC:
                                case SWafer.enumFromLoader.LoadportD:
                                case SWafer.enumFromLoader.LoadportE:
                                case SWafer.enumFromLoader.LoadportF:
                                case SWafer.enumFromLoader.LoadportG:
                                case SWafer.enumFromLoader.LoadportH:
                                    {
                                        I_Loadport loadport = ListSTG[(eFromLoader - SWafer.enumFromLoader.LoadportA)];
                                        nStgeIndx = GParam.theInst.GetDicPosRobot(robot.BodyNo, eFromLoader, loadport.UseAdapter) + (int)loadport.eFoupType;
                                    }
                                    break;
                                default: nStgeIndx = -1; break;
                            }
                            return nStgeIndx;
                        };
                        robot.GetPositionStagIndx = (object oEnumPos, int nSlot) =>
                        {
                            SWafer.enumPosition ePosition = (SWafer.enumPosition)oEnumPos;

                            int nStgeIndx;
                            switch (ePosition)//改成指定目標
                            {
                                case SWafer.enumPosition.Loader1:
                                case SWafer.enumPosition.Loader2:
                                case SWafer.enumPosition.Loader3:
                                case SWafer.enumPosition.Loader4:
                                case SWafer.enumPosition.Loader5:
                                case SWafer.enumPosition.Loader6:
                                case SWafer.enumPosition.Loader7:
                                case SWafer.enumPosition.Loader8:
                                    {
                                        I_Loadport loadport = ListSTG[(ePosition - SWafer.enumPosition.Loader1)];
                                        nStgeIndx = GParam.theInst.GetDicPosRobot(robot.BodyNo, ePosition, loadport.UseAdapter) + (int)loadport.eFoupType;
                                    }
                                    break;
                                case SWafer.enumPosition.AlignerA:
                                case SWafer.enumPosition.AlignerB:
                                case SWafer.enumPosition.BufferA:
                                case SWafer.enumPosition.BufferB:
                                case SWafer.enumPosition.EQM1:
                                case SWafer.enumPosition.EQM2:
                                case SWafer.enumPosition.EQM3:
                                case SWafer.enumPosition.EQM4:
                                    {
                                        nStgeIndx = GParam.theInst.GetDicPosRobot(robot.BodyNo, ePosition);
                                    }
                                    break;
                                default: nStgeIndx = -1; break;
                            }
                            return nStgeIndx;
                        };

                        robot.DlgPanelMisalign = isPanelMisalign;


                        ListTRB.Add(robot);
                        WriteLog(str);
                    }
                }
                #endregion


                
                #region =========================== 建構STG =========================================             
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumLoadport)).Count(); i++)//8
                    {
                        str = "Constructing Loadport_" + (i + 1);
                        frmloading.AddMessage(str);
                        string LoadPort1WaferType = GParam.theInst.GetLoadportWaferType(i);
                        sServer server = GParam.theInst.GetSTG_TCPType(i) == enumTCPType.Client ? _Server : null;
                        //找LP要Robot mapping對應Robot的stage number
                        int[] nTrbMapStgNo0to399 = new int[2] { -1, -1 };
                        for (int j = 0; j < ListTRB.Count; j++)
                        {
                            if (GParam.theInst.GetRobot_AllowPort(j)[i] == '1')
                            {
                                nTrbMapStgNo0to399[0] = GParam.theInst.GetDicPosRobot(j + 1, SWafer.enumFromLoader.LoadportA + i, false);
                                nTrbMapStgNo0to399[1] = GParam.theInst.GetDicPosRobot(j + 1, SWafer.enumFromLoader.LoadportA + i, true);
                                break;
                            }
                        }

                        I_BarCode barcode = null;
                        if (GParam.theInst.GetStgBarcodeIndex(i) >= 0)
                            barcode = ListBCR[GParam.theInst.GetStgBarcodeIndex(i)];

                        I_Loadport loadport = null;
                        switch (GParam.theInst.GetLoadportMode(i))
                        {
                            case enumLoadportType.RV201:
                                loadport = new SSLoadPortRV201(ListTRB[0], ListE84[i], GParam.theInst.GetStgIP(i), 12000, i + 1, GParam.theInst.IsUnitDisable(enumUnit.STG1 + i), bSimulate, nTrbMapStgNo0to399, LoadPort1WaferType, barcode, server);
                                break;
                            case enumLoadportType.RB201:
                                loadport = new SSLoadPortRB201(ListTRB[0], ListE84[i], GParam.theInst.GetStgIP(i), 12100, i + 1, GParam.theInst.IsUnitDisable(enumUnit.STG1 + i), bSimulate, nTrbMapStgNo0to399, LoadPort1WaferType, barcode, server);
                                break;
                            case enumLoadportType.Other:
                                Dictionary<LoadPortGPIO.LoadPortDI, SSLoadPortRC550.IO_Identification> m_dicDI = new Dictionary<LoadPortGPIO.LoadPortDI, SSLoadPortRC550.IO_Identification>();
                                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresence] = new SSLoadPortRC550.IO_Identification(0, 404 + i, 0);//注意電控設計
                                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresenceleft] = new SSLoadPortRC550.IO_Identification(0, 404 + i, 1);//注意電控設計
                                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresenceright] = new SSLoadPortRC550.IO_Identification(0, 404 + i, 2);//注意電控設計
                                m_dicDI[LoadPortGPIO.LoadPortDI._DIPresencemiddle] = new SSLoadPortRC550.IO_Identification(0, 404 + i, 3);//注意電控設計
                                m_dicDI[LoadPortGPIO.LoadPortDI._DIProtrusion] = new SSLoadPortRC550.IO_Identification(0, 404 + i, 4);//注意電控設計
                                loadport = new SSLoadPortRC550(ListDIO[0], ListTRB[0], ListE84[i], i + 1, GParam.theInst.IsUnitDisable(enumUnit.STG1 + i), bSimulate, nTrbMapStgNo0to399, LoadPort1WaferType, m_dicDI, barcode);
                                break;
                            default:
                                break;
                        }
                        loadport.UpdateInfoPadEnable(GParam.theInst.GetFoupTypeEnableList(i));
                        loadport.UpdateTrbMapInfoEnable(GParam.theInst.GetTrbMapInfoEnableList(i));
                        SWafer.enumPosition ePosition = SWafer.enumPosition.Loader1 + i;
                        SWafer.enumFromLoader eFromLoader = SWafer.enumFromLoader.LoadportA + i;
                        loadport.OnAssignWaferData += (object sender, WaferDataEventArgs e) => { e.Wafer.Position = ePosition; };
                        loadport.OnWaferDataDelete += (object sender, SlotEventArgs e) => { SWafer.RemoveWaferData(eFromLoader, e.Slot); };
                        loadport.OnSimulateMapping += _loadport_OnSimulateMapping;
                        loadport.OnFoupExistChenge += _loadport_OnFoupExistChenge;
                        //========== loader interlock
                        loadport.dlgLoadInterlock += LoadportDockUndkInterlock;
                        loadport.dlgUnloadInterlock += LoadportDockUndkInterlock;

                        loadport.FoupArrivalIdleTimeout = GParam.theInst.FoupArrivalIdleTimeout;//會依照設定變動
                        loadport.FoupWaitTransferTimeout = GParam.theInst.FoupWaitTransferTimeout;//會依照設定變動

                        ListSTG.Add(loadport);
                        WriteLog(str);
                    }
                }
                #endregion
                #region =========================== 建構ALG =========================================             
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumAligner)).Count(); i++)//2
                    {
                        str = "Constructing Aligner_" + (i + 1);
                        frmloading.AddMessage(str);

                        I_BarCode barcode = null;
                        if (GParam.theInst.GetAlnBarcodeIndex(i) >= 0)
                            barcode = ListBCR[GParam.theInst.GetAlnBarcodeIndex(i)];

                        I_Aligner aligner = null;
                        switch (GParam.theInst.GetAlignerMode(i))
                        {
                            case enumAlignerType.RA320:
                                aligner = new SSAlignerRA320(GParam.theInst.GetAlnIP(i), 12000, i + 1, GParam.theInst.IsUnitDisable(enumUnit.ALN1 + i), bSimulate, GParam.theInst.GetAlignerUnClampLiftPinUp(i), barcode);
                                break;
                            case enumAlignerType.RA420:
                                aligner = new SSAlignerRA420(GParam.theInst.GetAlnIP(i), 12000, i + 1, GParam.theInst.IsUnitDisable(enumUnit.ALN1 + i), bSimulate, GParam.theInst.GetAlignerUnClampLiftPinUp(i), barcode);
                                break;
                            case enumAlignerType.TurnTable:
                                aligner = new SSAlignerTurnTable(GParam.theInst.GetAlnIP(i), 12000, i + 1, GParam.theInst.IsUnitDisable(enumUnit.ALN1 + i), bSimulate, barcode);
                                break;
                            case enumAlignerType.PanelXYR:
                                aligner = new SSAlignerPanelXYR(i + 1, GParam.theInst.IsUnitDisable(enumUnit.ALN1 + i), bSimulate);
                                break;
                            case enumAlignerType.TAL303:
                                aligner = new SSAlignerTAL303(GParam.theInst.GetAlnIP(i), 12100, i + 1, GParam.theInst.IsUnitDisable(enumUnit.ALN1 + i), bSimulate, GParam.theInst.GetAlignerUnClampLiftPinUp(i), barcode);
                                break;
                        }

                        if (aligner != null)
                        {
                            //Assign wafer data to Aligner after modify position info.
                            SWafer.enumPosition ePosition = SWafer.enumPosition.AlignerA + i;
                            aligner.OnAssignWaferData += (object sender, WaferDataEventArgs e) => { e.Wafer.Position = ePosition; };
                        }
                        ListALN.Add(aligner);
                        WriteLog(str);
                    }
                }
                #endregion
                #region =========================== 建構Adam6066 ========================================
                try
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumAdam)).Count(); i++)
                    {
                        str = "Constructing Adam_" + i;
                        frmloading.AddMessage(str);
                        ADAM6066 Adam6066;
                        if (GParam.theInst.IsSimulate)
                        {
                            Adam6066 = new ADAM6066(i, "127.0.0.1", GParam.theInst.AdamPort(i), GParam.theInst.AdamDisable(i), GParam.theInst.IsSimulate);
                        }
                        else
                        {
                            Adam6066 = new ADAM6066(i, GParam.theInst.AdamIP(i), GParam.theInst.AdamPort(i), GParam.theInst.AdamDisable(i), GParam.theInst.IsSimulate);
                        }
                        ListAdam.Add(Adam6066);
                        WriteLog(str);
                    }
                }
                catch (Exception ex)
                {
                    _errorLog.WriteLog("[ MDI ] Exception : {0}", ex);
                }
                #endregion

                #region =========================== 建構Equipment ====================================
                try
                {
                    for (int i = 0; i < Enum.GetNames(typeof(enumEQM)).Count(); i++)//4
                    {
                        str = "Constructing Equipment_" + (i + 1);
                        frmloading.AddMessage(str);
                        sServer EQserver = null;

                        if (GParam.theInst.EqmTCPType(i) == enumTCPType.Client)//  EQ client Rorze Server
                        {
                            if (GParam.theInst.EqmSimulate(i))
                            {
                                string ip = $"127.0.0.1";
                                EQserver = new sServer(ip, GParam.theInst.EqmPort(i));
                            }
                            else
                            {
                                EQserver = new sServer(GParam.theInst.EqmIP(i), GParam.theInst.EqmPort(i));
                            }
                        }
                        SSEquipment equipment;

                        equipment = new SSEquipment
                        (
                            i + 1,
                            GParam.theInst.EqmIP(i), GParam.theInst.EqmPort(i), ListDIO,
                            GParam.theInst.EqmDisable(i), GParam.theInst.EqmSimulate(i),
                            GParam.theInst.EqmName(i),
                            _grouprecipe,
                            EQserver
                        );

                        enumPosition ePosition = SWafer.enumPosition.EQM1 + i;
                        equipment.OnAssignWaferData += (object sender, WaferDataEventArgs e) => { e.Wafer.Position = ePosition; };
                        equipment.DlgWaferExist = EQ_WaferExist;//委派外層
                        equipment.DlgStageReady = EQ_StageReady;//委派外層
                        equipment.DlgShutterDoorOpen = IsShutterDoorOpen;
                        equipment.DlgShutterDoorClose = IsShutterDoorClose;

                        equipment.DlgSetDoorCloseW = ShutterDoorCloseW;
                        equipment.DlgSetDoorOpenW = ShutterDoorOpenW;
<<<<<<< HEAD
                        equipment.DlgSetDoorOpen = ShutterDoorOpen;
=======
                        //equipment.DlgSetDoorOpen = ShutterDoorOpen;
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
                        equipment.DlgSetRobotExtendIO = robotExtendCtrlIO;

                        equipment.DlgReadyUnload = EQ_ReadyToUnload;
                        equipment.DlgReadyLoad = EQ_ReadyToLoad;
                        equipment.DlgGetSMEMA = robotGetEQCtrlIO;
                        equipment.DlgPutSMEMA = robotPutEQCtrlIO;

                        ListEQM.Add(equipment);
                        WriteLog(str);

                        if (EQserver != null) EQserver.ServerStart();
                    }
                }
                catch (Exception ex)
                {
                    _errorLog.WriteLog("[ MDI ] Exception : {0}", ex);
                }
                #endregion =====================================================================

                #region =========================== 初始化 MotionEventManager ================================
                {
                    try
                    {
                        // 檢查是否停用 GRPC 功能
                        if (!GParam.theInst.GetGRPC_Disable() || !GParam.theInst.IsSimulate)
                        {
                            str = "Initializing Motion Event Manager...";
                            frmloading.AddMessage(str);

                            var motionManager = MotionEventManager.Instance;
                            motionManager.SetBaseUrl(GParam.theInst.GetMotionEventManagerUrl());
                            motionManager.EnableLogging(true);

                            // 註冊所有 Robot（TRB）
                            foreach (I_Robot robot in ListTRB)
                            {
                                if (robot != null)
                                {
                                    motionManager.RegisterUnit(robot, "Robot", r => r.BodyNo, "OnLoadComplete"); // LOAD
                                    motionManager.RegisterUnit(robot, "Robot", r => r.BodyNo, "OnUnldComplete"); // UNLD
                                    WriteLog($"[MotionEventManager] Registered Robot #{robot.BodyNo}");
                                }
                            }

                            // 註冊所有 Loadport（STG）
                            foreach (I_Loadport loadport in ListSTG)
                            {
                                if (loadport != null)
                                {

                                    motionManager.RegisterUnit(loadport, "Loadport", lp => lp.BodyNo, "OnClmpComplete");
                                    motionManager.RegisterUnit(loadport, "Loadport", lp => lp.BodyNo, "OnClmp1Complete");
                                    //motionManager.RegisterUnit(loadport, "Loadport", lp => lp.BodyNo, "OnUclmComplete");
                                    //motionManager.RegisterUnit(loadport, "Loadport", lp => lp.BodyNo, "OnUclm1Complete");
                                    WriteLog($"[MotionEventManager] Registered Loadport #{loadport.BodyNo}");
                                }
                            }

                            // 註冊所有 Aligner（ALG）
                            foreach (I_Aligner aligner in ListALN)
                            {
                                if (aligner != null)
                                {
                                    motionManager.RegisterUnit(aligner, "Aligner", aln => aln.BodyNo, "OnAligCompelet");
                                    WriteLog($"[MotionEventManager] Registered Aligner #{aligner.BodyNo}");
                                }
                            }

                            WriteLog("[MotionEventManager] All devices registered successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        _errorLog.WriteLog("[ MDI ] MotionEventManager initialization failed: {0}", ex);
                    }
                }
                #endregion

                try
                {
                    SystemContext.Instance.Initialize(ListTRB, ListSTG, ListALN, ListBUF, ListEQM, ListDIO, simulate:bSimulate); // HSC TEST
                }
                catch
                {

                }

                #region =========================== 建構alarm =======================================
                {
                    str = "Constructing Alarm Manager Object.";
                    frmloading.AddMessage(str);
                    _alarm = new SAlarm(_dbProcess, _dbAlarmList, ListTRB, ListSTG, ListE84, ListALN, ListDIO, ListBUF);
                    _alarm.NotifyCloseSoftware += CloseSoftware;
                    _alarm.dlgIsTransfer += () =>
                    {
                        foreach (I_E84 item in ListE84)
                        {
                            if (item.GetAutoMode)
                                return true;
                        }
                        return false;
                        //return IsProcess();
                    };

                    //註冊Database容量異常警報
                    WriteLog(str);
                }
                #endregion
                #region =========================== 建構StockerDB ===================================
                //DataBase initialize
                _DataBase = new SSSorterSQL(GParam.theInst.DBSever, GParam.theInst.DBName, GParam.theInst.DBUser, GParam.theInst.DBPassWord);

                if (_DataBase.Check() == false) throw new Exception("Error DataBase Parameter");
                #endregion

                #region =========================== 建構TransferProcess =============================      
                {
                    // DB initialize
                    _DB = new MainDB();
                    _DB.Open();
                    _DB.ResetAllAlarm();

                    // VID initialize
                    _VIDControl = new VIDManager(_DB);

                    //Job initialize
                    _JobControl = new PJCJManager();

                    //CEID initialize
                    _CEIDControl = new CEIDManager(_DB);

                    // Parameter initialize
                    _SECSparameter = new SSECSParameter(_DB); // load Config


                    str = "Constructing Transfer Manager Object.";
                    frmloading.AddMessage(str);
                    _autoProcess = new STransfer(ListTRB, ListSTG, ListALN, ListE84, ListOCR, ListBUF, _JobControl, _grouprecipe, _dbProcess, _alarm, _DataBase, _VIDControl, ListEQM, ListAdam);
                    WriteLog(str);
                }
                #endregion =====================================================================  

                #region =========================== 建構SECS ========================================      
                {

                    str = "Constructing SECS GEM Utilty Object.";
                    frmloading.AddMessage(str);
                    _SECSUtilty = new SSECSGEMUtilty(
                        ListTRB,
                        new Dictionary<int, I_Loadport>() { { 1, ListSTG[0] }, { 2, ListSTG[1] }, { 3, ListSTG[2] }, { 4, ListSTG[3] }, { 5, ListSTG[4] }, { 6, ListSTG[5] }, { 7, ListSTG[6] }, { 8, ListSTG[7] } },
                        new Dictionary<int, I_Aligner>() { { 1, ListALN[0] }, { 2, ListALN[1] }, },
                           _alarm, _grouprecipe, ListOCR,
                           _autoProcess, _DataBase
                        );


                    WriteLog(str);
                }
                #endregion =====================================================================

                #region =========================== 建構Gem300 ======================================
                {
                    str = "Constructing SGEM300 Manager Object.";
                    frmloading.AddMessage(str);
                    _Gem = new SGEM300(_DB, _SECSparameter, _CEIDControl, _JobControl, _VIDControl, _SECSUtilty, _autoProcess, _dbAlarmList, GParam.theInst.IsSecsEnable, _grouprecipe);
                    _autoProcess.SetSecsGem(_Gem);
                    WriteLog(str);
                }
                #endregion


            }
            catch (Exception ex)
            {
                frmloading.AddMessage("[ Exception ]" + ex);
                new frmMessageBox(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                WriteLog("<Exception> : " + ex);
                this.Close();
                Environment.Exit(Environment.ExitCode);
            }
            List<bool> eqmDisableList = new List<bool>()
            {
                ListEQM[0].Disable,
                ListEQM[1].Disable,
                ListEQM[2].Disable,
                ListEQM[3].Disable
            };
            #region 註冊interlock delegate
            foreach (I_Robot robot in ListTRB)
            {
                //I_Robot robot = ListTRB[0];
                foreach (RorzePosition item in GParam.theInst.GetLisPosRobot(robot.BodyNo).ToArray())
                {
                    if (item.Stge0to399 == -1) continue;
                    switch (item.strDefineName)//0~399
                    {
                        case enumRbtAddress.STG1_08:
                            if (robot.RobotHardwareAllow(enumPosition.Loader1))
                                for (int i = 0; i < 4; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader1_8Interlock);
                                }
                            break;
                        case enumRbtAddress.STG1_12:
                            if (robot.RobotHardwareAllow(enumPosition.Loader1))
                                for (int i = 0; i < 16; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader1_12Interlock);
                                }
                            break;
                        case enumRbtAddress.STG2_08:
                            if (robot.RobotHardwareAllow(enumPosition.Loader2))
                                for (int i = 0; i < 4; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader2_8Interlock);
                                }
                            break;
                        case enumRbtAddress.STG2_12:
                            if (robot.RobotHardwareAllow(enumPosition.Loader2))
                                for (int i = 0; i < 16; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader2_12Interlock);
                                }
                            break;
                        case enumRbtAddress.STG3_08:
                            if (robot.RobotHardwareAllow(enumPosition.Loader3))
                                for (int i = 0; i < 4; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader3_8Interlock);

                                }
                            break;
                        case enumRbtAddress.STG3_12:
                            if (robot.RobotHardwareAllow(enumPosition.Loader3))
                                for (int i = 0; i < 16; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader3_12Interlock);

                                }
                            break;
                        case enumRbtAddress.STG4_08:
                            if (robot.RobotHardwareAllow(enumPosition.Loader4))
                                for (int i = 0; i < 4; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader4_8Interlock);
                                }
                            break;
                        case enumRbtAddress.STG4_12:
                            if (robot.RobotHardwareAllow(enumPosition.Loader4))
                                for (int i = 0; i < 16; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader4_12Interlock);
                                }
                            break;
                        case enumRbtAddress.STG5_08:
                            if (robot.RobotHardwareAllow(enumPosition.Loader5))
                                for (int i = 0; i < 4; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader5_8Interlock);
                                }
                            break;
                        case enumRbtAddress.STG5_12:
                            if (robot.RobotHardwareAllow(enumPosition.Loader5))
                                for (int i = 0; i < 16; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader5_12Interlock);
                                }
                            break;
                        case enumRbtAddress.STG6_08:
                            if (robot.RobotHardwareAllow(enumPosition.Loader6))
                                for (int i = 0; i < 4; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader6_8Interlock);
                                }
                            break;
                        case enumRbtAddress.STG6_12:
                            if (robot.RobotHardwareAllow(enumPosition.Loader6))
                                for (int i = 0; i < 16; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader6_12Interlock);
                                }
                            break;
                        case enumRbtAddress.STG7_08:
                            if (robot.RobotHardwareAllow(enumPosition.Loader7))
                                for (int i = 0; i < 4; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader7_8Interlock);
                                }
                            break;
                        case enumRbtAddress.STG7_12:
                            if (robot.RobotHardwareAllow(enumPosition.Loader7))
                                for (int i = 0; i < 16; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader7_12Interlock);
                                }
                            break;
                        case enumRbtAddress.STG8_08:
                            if (robot.RobotHardwareAllow(enumPosition.Loader8))
                                for (int i = 0; i < 4; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader8_8Interlock);
                                }
                            break;
                        case enumRbtAddress.STG8_12:
                            if (robot.RobotHardwareAllow(enumPosition.Loader8))
                                for (int i = 0; i < 16; i++)
                                {
                                    robot.AddInterlock(item.Stge0to399 + i, Loader8_12Interlock);
                                }
                            break;
                        case enumRbtAddress.ALN1:
                            if (robot.RobotHardwareAllow(enumPosition.AlignerA))
                                switch (GParam.theInst.GetAlignerMode(0))
                                {
                                    case enumAlignerType.TurnTable:
                                        robot.AddInterlock(item.Stge0to399, Aligner1_Interlock_Turtable);
                                        break;
                                    case enumAlignerType.RA320:
                                    case enumAlignerType.RA420:
                                    case enumAlignerType.TAL303:
                                        if (robot.RobotHardwareAllow(enumPosition.AlignerA))
                                            robot.AddInterlock(item.Stge0to399, Aligner1_Interlock);
                                        break;
                                }
                            break;
                        case enumRbtAddress.ALN2:
                            if (robot.RobotHardwareAllow(enumPosition.BufferB))
                                switch (GParam.theInst.GetAlignerMode(1))
                                {
                                    case enumAlignerType.TurnTable:
                                        robot.AddInterlock(item.Stge0to399, Aligner2_Interlock_Turtable);
                                        break;
                                    case enumAlignerType.RA320:
                                    case enumAlignerType.RA420:
                                    case enumAlignerType.TAL303:
                                        robot.AddInterlock(item.Stge0to399, Aligner2_Interlock);
                                        break;
                                }
                            break;
                        case enumRbtAddress.BUF1:
                            if (robot.RobotHardwareAllow(enumPosition.BufferA))
                                robot.AddInterlock(item.Stge0to399, Buffer1_Interlock);
                            break;
                        case enumRbtAddress.BUF2:
                            if (robot.RobotHardwareAllow(enumPosition.BufferB))
                                robot.AddInterlock(item.Stge0to399, Buffer2_Interlock);
                            break;
                        case enumRbtAddress.EQM1:
                            {
                                robot.AddInterlock(item.Stge0to399, Equipment1_Interlock);
                            }
                            break;
                        case enumRbtAddress.EQM2:
                            {
                                robot.AddInterlock(item.Stge0to399, Equipment2_Interlock);
                            }
                            break;
                        case enumRbtAddress.EQM3:
                            {
                                robot.AddInterlock(item.Stge0to399, Equipment3_Interlock);
                            }
                            break;
                        case enumRbtAddress.EQM4:
                            {
                                robot.AddInterlock(item.Stge0to399, Equipment4_Interlock);
                            }
                            break;
                    }
                }
            }
            #endregion

            //確認AlarmList DB有OcurTime欄位
            _alarm.CheckColumn_OcurTime();

            if (GParam.theInst.GetDBAlarmlistUpdate)
            {
                WriteLog("UpdateAlarmList Start!!");
                _alarm.UpdataAlarmList();//將自定義的error加入到DB
                GParam.theInst.SetDBAlarmlistUpdate = false;
                WriteLog("UpdateAlarmList End!!");
            }

            // Rorze server start connect
            if (_Server != null) _Server.ServerStart();
            frmloading.Close();

            try
            {
                #region =========================== Unit Connect =======================================
                WriteLog("Construct Unit Connect is Form!!!");
                frmUnitConnect1 _frmUnitConnect = new frmUnitConnect1(ListTRB, ListTBL, ListSTG, ListALN, ListDIO, ListOCR, ListBCR, ListEQM);
                if (_frmUnitConnect.ShowDialog() != DialogResult.OK)
                {
                    throw new Exception("Unit connection failed");
                }
                SpinWait.SpinUntil(() => false, 100);
                #endregion



                frmMessageBox frm = new frmMessageBox("Execute all unit initialize?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (frm.ShowDialog() == DialogResult.Yes)
                {
                    #region =========================== Initialization Orgn ================================          
                    WriteLog("Execute Home Return start!!");
                    frmOrgn _frmOrgn = new frmOrgn(ListTRB, ListSTG, ListALN, ListBUF, ListEQM, bSimulate);
                    if (_frmOrgn.ShowDialog() != DialogResult.OK)
                    {
                        throw new Exception("Unit Home Return failed");
                    }
                    WriteLog("Execute Home Return complete!!");
                    #endregion

                    #region =========================== WaferRecover =======================================              
                    //測試
                    if (bSimulate)
                    {
                        System.Data.DataTable dt = _DataBase.SelectUnitStatus();
                        if (dt != null && dt.Rows.Count != 0)
                        {
                            string[] strTRB1upper = _DataBase.GetUnitStatus(SMySQL.enumUnit.TRB1Upper).Split('_');
                            if (strTRB1upper.Length > 1)
                            {
                                ListTRB[0].UpperArmWafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    ListTRB[0].UpperArmFunc == RorzeUnit.Class.Robot.Enum.enumArmFunction.FRAME ? SWafer.enumWaferSize.Frame : SWafer.enumWaferSize.Inch12,
                                    SWafer.enumPosition.UpperArm,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep);
                            }
                            string[] strTRB1Lower = _DataBase.GetUnitStatus(SMySQL.enumUnit.TRB1Lower).Split('_');
                            if (strTRB1Lower.Length > 1)
                            {
                                ListTRB[0].LowerArmWafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    ListTRB[0].LowerArmFunc == RorzeUnit.Class.Robot.Enum.enumArmFunction.FRAME ? SWafer.enumWaferSize.Frame : SWafer.enumWaferSize.Inch12,
                                    SWafer.enumPosition.LowerArm,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep);
                            }
                            string[] strTRB2Upper = _DataBase.GetUnitStatus(SMySQL.enumUnit.TRB2Upper).Split('_');
                            if (strTRB2Upper.Length > 1)
                            {
                                ListTRB[1].UpperArmWafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    ListTRB[1].UpperArmFunc == RorzeUnit.Class.Robot.Enum.enumArmFunction.FRAME ? SWafer.enumWaferSize.Frame : SWafer.enumWaferSize.Inch12,
                                    SWafer.enumPosition.UpperArm,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep);
                            }
                            string[] strTRB2Lower = _DataBase.GetUnitStatus(SMySQL.enumUnit.TRB2Lower).Split('_');
                            if (strTRB2Lower.Length > 1)
                            {
                                ListTRB[1].LowerArmWafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                     ListTRB[1].LowerArmFunc == RorzeUnit.Class.Robot.Enum.enumArmFunction.FRAME ? SWafer.enumWaferSize.Frame : SWafer.enumWaferSize.Inch12,
                                     SWafer.enumPosition.LowerArm,
                                     SWafer.enumFromLoader.UnKnow,
                                     GParam.theInst.EqmDisableArray,
                                     SWafer.enumProcessStatus.Sleep);
                            }
                            string[] strALN1 = _DataBase.GetUnitStatus(SMySQL.enumUnit.ALN1).Split('_');
                            if (strALN1.Length > 1)
                            {
                                I_Aligner aln = ListALN[0];
                                enumWaferSize wafersize = aln.WaferType;

                                aln.Wafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    wafersize,
                                    SWafer.enumPosition.AlignerA + aln.BodyNo - 1,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep);
                            }
                            string[] strALN2 = _DataBase.GetUnitStatus(SMySQL.enumUnit.ALN2).Split('_');
                            if (strALN2.Length > 1)
                            {
                                I_Aligner aln = ListALN[1];
                                enumWaferSize wafersize = aln.WaferType;

                                aln.Wafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    wafersize,
                                    SWafer.enumPosition.AlignerA + aln.BodyNo - 1,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep);
                            }
                            string[] strBUF1_1 = _DataBase.GetUnitStatus(SMySQL.enumUnit.BUF1_slot1).Split('_');
                            if (strBUF1_1.Length > 1)
                            {
                                I_Buffer buf = ListBUF[0];
                                buf.SetWafer(0, new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    buf.WaferType,
                                    SWafer.enumPosition.BufferA + buf.BodyNo - 1,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep));//新建
                            }
                            string[] strBUF1_2 = _DataBase.GetUnitStatus(SMySQL.enumUnit.BUF1_slot2).Split('_');
                            if (strBUF1_2.Length > 1)
                            {
                                I_Buffer buf = ListBUF[0];
                                buf.SetWafer(1, new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 2,
                                    buf.WaferType,
                                    SWafer.enumPosition.BufferA + buf.BodyNo - 1,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep));//新建
                            }
                            string[] strBUF2_1 = _DataBase.GetUnitStatus(SMySQL.enumUnit.BUF2_slot1).Split('_');
                            if (strBUF2_1.Length > 1)
                            {
                                I_Buffer buf = ListBUF[1];
                                buf.SetWafer(0, new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    buf.WaferType,
                                    SWafer.enumPosition.BufferA + buf.BodyNo - 1,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep));//新建
                            }
                            string[] strBUF2_2 = _DataBase.GetUnitStatus(SMySQL.enumUnit.BUF2_slot2).Split('_');
                            if (strBUF2_2.Length > 1)
                            {
                                I_Buffer buf = ListBUF[1];
                                buf.SetWafer(1, new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 2,
                                    buf.WaferType,
                                    SWafer.enumPosition.BufferA + buf.BodyNo - 1,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep));//新建
                            }
                        }
                    }
                    //========== WaferRecover
                    ExecuteRecover();
                    #endregion
                }


            }
            catch (Exception ex)
            {
                WriteLog("<Exception> : " + ex);
                new frmMessageBox(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();

                if (_Gem != null && _Gem.GetSECSDriver.GetSecsStarted()) _Gem.GetSECSDriver.SecsStop();
                this.Close();
                Environment.Exit(Environment.ExitCode);
            }

            #region =========================== Create forms (畫面建構一定要最後一動) 
            try
            {
                //  ==========   Operation
                frmMain fMain = new frmMain(ListTRB, ListSTG, ListALN, ListE84, ListDIO, ListBUF, ListOCR, ListFFU, _alarm, _JobControl, _Gem, _dbProcess, _grouprecipe, _autoProcess, _DataBase, _userManager, ListEQM) { Text = GParam.theInst.GetLanguage("Main") };
                _afrmOperation.Add(fMain);
                WriteLog("Create Operation Form is Complete!!");

                //  ==========   Teaching
                frmTeachRobot frmteachrobot = new frmTeachRobot(ListTRB, ListTBL, ListSTG, ListALN, ListBUF, _dbProcess, _userManager, !IsMaintMode_EFEM(), ListEQM) { Text = GParam.theInst.GetLanguage("Robot") };
                frmteachrobot.delegateMDILock += disableMenuToolBar;
                frmteachrobot.Enabled = false;
                foreach (I_Robot item in ListTRB) frmteachrobot.Enabled |= (item.Disable == false);
                _afrmTeaching.Add(frmteachrobot);
                WriteLog("Create TeachRobot Form is Complete!!");

                frmTeachRobotDataCopy frmteachrobotdatacopy = new frmTeachRobotDataCopy(ListTRB, ListSTG, _dbProcess, _userManager) { Text = GParam.theInst.GetLanguage("Robot Copy Data") };
                frmteachrobotdatacopy.delegateMDILock += disableMenuToolBar;
                frmteachrobotdatacopy.Enabled = false;
                foreach (I_Robot item in ListTRB) frmteachrobotdatacopy.Enabled |= (item.Disable == false);
                _afrmTeaching.Add(frmteachrobotdatacopy);
                WriteLog("Create TeachRobotDataCopy Form is Complete!!");

                frmTeachRobotMapping frmteachrobotmapping = new frmTeachRobotMapping(ListTRB, ListSTG, ListALN, ListBUF, _dbProcess, _userManager, !IsMaintMode_EFEM(), ListEQM) { Text = GParam.theInst.GetLanguage("Robot Mapping") };
                frmteachrobotmapping.delegateMDILock += disableMenuToolBar;
                frmteachrobotmapping.Enabled = false;
                foreach (I_Robot item in ListTRB) frmteachrobotmapping.Enabled |= (item.Disable == false && item.EnableMap);
                _afrmTeaching.Add(frmteachrobotmapping);
                WriteLog("Create TeachRobotMapping Form is Complete!!]");

                foreach (I_Loadport item in ListSTG)
                {
                    frmTeachLoadport formteachlp = new frmTeachLoadport(item, bSimulate, _dbProcess, _userManager) { Text = GParam.theInst.GetLanguage("Loadport") + (char)(64 + item.BodyNo) };
                    formteachlp.delegateMDILock += disableMenuToolBar;
                    _afrmTeaching.Add(formteachlp);
                    if (item.Disable || GParam.theInst.GetLoadportMode(item.BodyNo - 1) == enumLoadportType.Other) formteachlp.Enabled = false;
                    WriteLog("Create TeachLoadport" + (char)(64 + item.BodyNo) + " Form is Complete!!");
                }

                frmTeachOCR frmteachocr = new frmTeachOCR(ListTRB, ListSTG, ListALN, ListOCR, ListE84, _dbProcess, _userManager, !IsMaintMode_EFEM()) { Text = GParam.theInst.GetLanguage("ALN OCR") };
                frmteachocr.delegateMDILock += disableMenuToolBar;
                _afrmTeaching.Add(frmteachocr);
                if ((ListALN[0].Disable && ListALN[1].Disable) || (ListOCR[0].Disable && ListOCR[1].Disable && ListOCR[2].Disable && ListOCR[3].Disable)) frmteachocr.Enabled = false;
                WriteLog("Create TeachOCR Form is Complete!!");

                frmTeachRobotAlignment frmteachrobotalignment = new frmTeachRobotAlignment(ListTRB, ListSTG, ListALN, ListBUF, _dbProcess, _userManager, !IsMaintMode_EFEM(), ListEQM) { Text = GParam.theInst.GetLanguage("RB Align") };
                frmteachrobotalignment.delegateMDILock += disableMenuToolBar;
                _afrmTeaching.Add(frmteachrobotalignment);
                frmteachrobotalignment.Enabled = (ListTRB[0].EnableUpperAlignment || ListTRB[0].EnableLowerAlignment || GParam.theInst.IsSimulate);
                WriteLog("Create RobotAlignment Form is Complete!!");

                /*frmTeachAngle frmteachangle = new frmTeachAngle(ListTRB, ListSTG, ListALN, _dbProcess, _userManager, _grouprecipe, IsRunMode_EFEM()) { Text = GParam.theInst.GetLanguage("Notch") };
                frmteachangle.delegateMDILock += disableMenuToolBar;
                _afrmTeaching.Add(frmteachangle);
                //教導robot與aligner相對關係，只要有aligner
                frmteachangle.Enabled = false;
                foreach (I_Aligner aligner in ListALN)
                {
                    if (aligner.Disable == false && aligner.WaferType == RorzeUnit.Class.Aligner.Enum.enumAlignerWaferType.Wafer)
                        frmteachangle.Enabled = true;
                }
                WriteLog("Create TeachNotch Form is Complete!!");*/

                /*frmTeachStock frmteachstock = new frmTeachStock(ListTRB, ListSTG, ListALN, ListSTK, ListBUF, _dbEvnt, _userManager, IsRunMode_EFEM(), bSimulate) { Text = GParam.theInst.GetLanguage("Stocker") };
                frmteachstock.delegateMDILock += disableMenuToolBar;
                frmteachstock.dlgIsTkeyOn += () => { return IsTkeyOn_Stocker(); };
                frmteachstock.Enabled = GParam.theInst.IsAllTowerDisable() == false;
                _afrmTeaching.Add(frmteachstock);
                WriteLog("Create TeachStock Form is Complete!!");*/

                //  ==========  Initialen
                _afrmInitialen.Add(new frmOrgn(ListTRB, ListSTG, ListALN, ListBUF, ListEQM, bSimulate) { Text = GParam.theInst.GetLanguage("Origin") });
                WriteLog("Create Orgn Form is Complete!!");

                //_afrmInitialen.Add(new frmCompareStockMapData(ListSTK, _DataBase, bSimulate) { Text = GParam.theInst.GetLanguage("StockerMapping") });
                //WriteLog("Create frmCompareStockMapData Form is Complete!!");

                //  ==========  Maintan
                _afrmMaintenance.Add(new frmIO(ListDIO, ListAdam, m_SafetyIO, ListFFU, _userManager) { Text = GParam.theInst.GetLanguage("IO") });
                WriteLog("Create frmIO Form is Complete!!");

                _afrmMaintenance.Add(new frmManual(ListTRB, ListSTG, ListALN, ListE84, ListDIO, ListBUF, _dbProcess, _userManager, bSimulate, _Gem, _grouprecipe, _autoProcess, _DataBase, !IsMaintMode_EFEM(), ListEQM) { Text = GParam.theInst.GetLanguage("Maintain") });

                ((frmManual)_afrmMaintenance[(int)eMaintenaceSec.Manual]).delegateMDILock += disableMenuToolBar;
                ((frmManual)_afrmMaintenance[(int)eMaintenaceSec.Manual]).delegateMDITriggerShowMainform += triggerSelectFirstPage;
                ((frmManual)_afrmMaintenance[(int)eMaintenaceSec.Manual]).delegateDemoStart += ((frmMain)_afrmOperation[(int)eStatusSec.Status]).DoFirstCycleSetting1;
                ((frmManual)_afrmMaintenance[(int)eMaintenaceSec.Manual]).delegateCycleStart += ((frmMain)_afrmOperation[(int)eStatusSec.Status]).DoFirstCycleStart;

                //_afrmMaintenance.Add(new frmEQCommand(_equipment) { Text = "EQ Command" });
                //((frmEQCommand)_afrmMaintenance[(int)eMaintenaceSec.EQ_Command]).Enabled = GParam.theInst.IsEquipmentDisable == false;

                _afrmMaintenance.Add(new frmWebView { Text = GParam.theInst.GetLanguage("GRPC") });
                WriteLog("Create frmIO GRPC is Complete!!");


                WriteLog("Create Maintan Form is Complete!!");

                //  ==========  Setup
                _afrmSetup.Add(new frmParameter(ListTRB, ListSTG, _userManager, !IsMaintMode_EFEM()) { Text = GParam.theInst.GetLanguage("Parameter") });
                WriteLog("Create Parameter Form is Complete!!");

                _afrmSetup.Add(new frmSECSSetting(_DB, _SECSparameter, _Gem) { Text = GParam.theInst.GetLanguage("SECSSetting") });
                if (!GParam.theInst.IsSecsEnable) _afrmSetup[(int)eStepupSec.SecsSetting].Enabled = false;
                WriteLog("Create SECSSetting Form is Complete!!");

                _afrmSetup.Add(new frmPermissionUser(_userManager, _dbMain, _Gem, m_SmartRFID) { Text = GParam.theInst.GetLanguage("Permission") });
                WriteLog("Create PermissionUser Form is Complete!!");

                _afrmSetup.Add(new frmGroupRecipe(_grouprecipe, _userManager, ListEQM) { Text = GParam.theInst.GetLanguage("GroupRecipe") });
                if (GParam.theInst.IsAllOcrDisable()) _afrmSetup[(int)eStepupSec.GroupRecipe].Enabled = false;
                WriteLog("Create GroupRecipe Form is Complete!!");

                frmSignalColor frmsignalcolor = new frmSignalColor(_userManager, _dbMain, _Gem) { Text = GParam.theInst.GetLanguage("Signal Tower") };
                _afrmSetup.Add(frmsignalcolor);
                WriteLog("Create SignalTower Form is Complete!!");

                //  ==========  Log
                _afrmLog.Add(new frmLogAlarm(_dbProcess, _userManager) { Text = GParam.theInst.GetLanguage("Alarm") });
                WriteLog("Create AlarmLog Form is Complete!!");

                _afrmLog.Add(new frmLogEvent(_dbProcess, _userManager) { Text = GParam.theInst.GetLanguage("Event") });
                WriteLog("Create EventLog Form is Complete!!");

                _afrmLog.Add(new frmLogProcess(_dbProcess, _userManager) { Text = GParam.theInst.GetLanguage("Process") });
                WriteLog("Create ProcessLog Form is Complete!!");

                //客戶需求
                //_afrmLog.Add(new frmIO(ListDIO, _userManager) { Text = GParam.theInst.GetLanguage("IO") });


                //  ==========  Alarm
                _afrmAlarm.Add(new frmAlarmCurrent(_alarm, _userManager) { Text = GParam.theInst.GetLanguage("AlarmPrompt") });
                WriteLog("Create AlarmPrompt Form is Complete!!");

                //  ==========  Secs
                _afrmSecs.Add(new frmSECSControl(_Gem, _SECSparameter) { Text = GParam.theInst.GetLanguage("SECSControl") });
                if (GParam.theInst.IsSecsEnable == false)
                {
                    _afrmSecs[(int)eSecsSec.Manual].Enabled = false;
                    btnSecs.Visible = false;
                }
                WriteLog("Create SECSControl Form is Complete!!");

                //_frmDiagnstics.AddMessage("Adjustposition of all pages .");
                //_alarm.exeAlarmReset.Set();
            }
            catch (Exception ex)
            {
                WriteLog("<Exception> : " + ex);
                new frmMessageBox("Software create error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                if (_Gem != null && _Gem.GetSECSDriver.GetSecsStarted()) _Gem.GetSECSDriver.SecsStop();
                this.Close();
                Environment.Exit(Environment.ExitCode);
            }
            #endregion 


        }

        #region ========== delegate Unit between Unit ==========

        // robot interlock checking
        private bool Loader1_12Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[0];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;

                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //目標沒有Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //比對手臂TYPE與LOADPORT TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //檢查LP狀態
                if (enumRobotAction != enumRobotAction.Standby && lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)//條件"enumRobotAction != enumRobotAction.Standby"是為了讓robot可以來執行mapping
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                if (!GParam.theInst.IsSimulate && lp.GetYaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Yaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                if (!GParam.theInst.IsSimulate && lp.GetZaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Zaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                //檢查LP門IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //檢查LP移動中
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //檢查LP異常
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
                /*if (enumRobotAction != enumRobotAction.Standby)
                {
                    if (SpinWait.SpinUntil(() => IsXaxsInLpA(), 1000) == false)
                    {
                        robot.TriggerSException(enumRobotError.XaxisINP_NotDetect);
                        return true;//這行執行不到
                    }
                }*/
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<SException>>:{0}", ex);
                return true;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader2_12Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[1];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //目標沒有Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //比對手臂TYPE與LOADPORT TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //檢查LP狀態
                if (enumRobotAction != enumRobotAction.Standby && lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process) //條件"enumRobotAction != enumRobotAction.Standby"是為了讓robot可以來執行mapping
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                if (lp.GetYaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Yaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                if (lp.GetZaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Zaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                //檢查LP門IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //檢查LP移動中
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //檢查LP異常
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
                /*if (enumRobotAction != enumRobotAction.Standby)
                {
                    if (SpinWait.SpinUntil(() => IsXaxsInLpB(), 1000) == false)
                    {
                        robot.TriggerSException(enumRobotError.XaxisINP_NotDetect);
                        return true;//這行執行不到
                    }
                }*/
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<SException>>:{0}", ex);
                return true;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader3_12Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[2];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //目標沒有Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //比對手臂TYPE與LOADPORT TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //檢查LP狀態
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                if (lp.GetYaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Yaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                if (lp.GetZaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Zaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                //檢查LP門IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //檢查LP移動中
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //檢查LP異常
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
                /*if (enumRobotAction != enumRobotAction.Standby)
                {
                    if (SpinWait.SpinUntil(() => IsXaxsInLpC(), 1000) == false)
                    {
                        robot.TriggerSException(enumRobotError.XaxisINP_NotDetect);
                        return true;//這行執行不到
                    }
                }*/
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<SException>>:{0}", ex);
                return true;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader4_12Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[3];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //目標沒有Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //比對手臂TYPE與LOADPORT TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //檢查LP狀態
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                if (lp.GetYaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Yaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                if (lp.GetZaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Zaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                //檢查LP門IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //檢查LP移動中
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //檢查LP異常
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
                /*if (enumRobotAction != enumRobotAction.Standby)
                {
                    if (SpinWait.SpinUntil(() => IsXaxsInLpD(), 1000) == false)
                    {
                        robot.TriggerSException(enumRobotError.XaxisINP_NotDetect);
                        return true;//這行執行不到
                    }
                }*/
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<SException>>:{0}", ex);
                return true;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader5_12Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[4];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //目標沒有Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //比對手臂TYPE與LOADPORT TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //檢查LP狀態
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                if (lp.GetYaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Yaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                if (lp.GetZaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Zaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                //檢查LP門IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //檢查LP移動中
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //檢查LP異常
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader6_12Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[5];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //目標沒有Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //比對手臂TYPE與LOADPORT TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //檢查LP狀態
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                if (lp.GetYaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Yaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                if (lp.GetZaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Zaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                //檢查LP門IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //檢查LP移動中
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //檢查LP異常
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader7_12Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[6];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //目標沒有Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //比對手臂TYPE與LOADPORT TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //檢查LP狀態
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                if (lp.GetYaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Yaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                if (lp.GetZaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Zaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                //檢查LP門IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //檢查LP移動中
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //檢查LP異常
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader8_12Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[7];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //目標沒有Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //比對手臂TYPE與LOADPORT TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //檢查LP狀態
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                if (lp.GetYaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Yaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                if (lp.GetZaxispos != enumLoadPortPos.Dock)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!,Zaxis Incorrect.", lp.BodyNo);
                    return true;
                }
                //檢查LP門IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //檢查LP移動中
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //檢查LP異常
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader1_8Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[0];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //check robot finger TYPE match LOADPORT TYPE               
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //check LP type
                if (lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} wrong wafer type !!", lp.BodyNo);
                    return true;
                }
                //check adapter
                if (lp.UseAdapter == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} no detection adapter!!", lp.BodyNo);
                    return true;
                }
                //check LP status
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                //check LP door IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //check LP moving
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //check LP error
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader2_8Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[1];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //check robot finger TYPE match LOADPORT TYPE               
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //check LP type
                if (lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} wrong wafer type !!", lp.BodyNo);
                    return true;
                }
                //check adapter
                if (lp.UseAdapter == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} no detection adapter!!", lp.BodyNo);
                    return true;
                }
                //check LP status
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                //check LP door IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //check LP moving
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //check LP error
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader3_8Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[2];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //check robot finger TYPE match LOADPORT TYPE               
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //check LP type
                if (lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} wrong wafer type !!", lp.BodyNo);
                    return true;
                }
                //check adapter
                if (lp.UseAdapter == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} no detection adapter!!", lp.BodyNo);
                    return true;
                }
                //check LP status
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                //check LP door IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //check LP moving
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //check LP error
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader4_8Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[3];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //check robot finger TYPE match LOADPORT TYPE               
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //check LP type
                if (lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} wrong wafer type !!", lp.BodyNo);
                    return true;
                }
                //check adapter
                if (lp.UseAdapter == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} no detection adapter!!", lp.BodyNo);
                    return true;
                }
                //check LP status
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                //check LP door IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //check LP moving
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //check LP error
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader5_8Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[4];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //check robot finger TYPE match LOADPORT TYPE               
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //check LP type
                if (lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} wrong wafer type !!", lp.BodyNo);
                    return true;
                }
                //check adapter
                if (lp.UseAdapter == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} no detection adapter!!", lp.BodyNo);
                    return true;
                }
                //check LP status
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                //check LP door IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //check LP moving
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //check LP error
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader6_8Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[5];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //check robot finger TYPE match LOADPORT TYPE               
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //check LP type
                if (lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} wrong wafer type !!", lp.BodyNo);
                    return true;
                }
                //check adapter
                if (lp.UseAdapter == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} no detection adapter!!", lp.BodyNo);
                    return true;
                }
                //check LP status
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                //check LP door IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //check LP moving
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //check LP error
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader7_8Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[6];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //check robot finger TYPE match LOADPORT TYPE               
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //check LP type
                if (lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} wrong wafer type !!", lp.BodyNo);
                    return true;
                }
                //check adapter
                if (lp.UseAdapter == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} no detection adapter!!", lp.BodyNo);
                    return true;
                }
                //check LP status
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                //check LP door IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //check LP moving
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //check LP error
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Loader8_8Interlock(object trb, object oRobotAction, object oArm, object oSlot)  //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Loadport lp = ListSTG[7];
            lp.SetRobotExtend = true;
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (lp.Waferlist[nSlot - 1] == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //目標有Wafer
                        if (lp.Waferlist[nSlot - 1] != null)
                        {
                            if (lp.Waferlist[nSlot - 1].ProcessStatus != enumProcessStatus.Processing)
                            {
                                _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                                return true;
                            }
                        }
                        break;
                }
                //check robot finger TYPE match LOADPORT TYPE               
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }
                //check LP type
                if (lp.GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} wrong wafer type !!", lp.BodyNo);
                    return true;
                }
                //check adapter
                if (lp.UseAdapter == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} no detection adapter!!", lp.BodyNo);
                    return true;
                }
                //check LP status
                if (lp.StatusMachine != enumStateMachine.PS_Docked && lp.StatusMachine != enumStateMachine.PS_Process)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not docking!!", lp.BodyNo);
                    return true;
                }
                //check LP door IO
                if (!GParam.theInst.IsSimulate && lp.IsDoorOpen == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} do not open door!!", lp.BodyNo);
                    return true;
                }
                //check LP moving
                if (lp.IsMoving)
                {
                    _errorLog.WriteLog("[ Interlock ]:STG{0} is moving!!", lp.BodyNo);
                    return true;
                }
                //check LP error
                if (lp.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is error!!", lp.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Aligner1_Interlock(object trb, object oRobotAction, object oArm, object oSlot)    //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Aligner aln = ListALN[0];
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;

                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (aln.Wafer == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        if (aln.IsMoving)
                        {
                            _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is moving!!", aln.BodyNo));
                            return true;
                        }
                        //if (aln.WaferExists() == false) // 夾爪某些角度會遮到
                        //{
                        //    _errorLog.WriteLog("[ Interlock ]:The target no wafer by sensor!!");
                        //    return true;
                        //}
                        break;
                    case enumRobotAction.Unlaod:
                        //target has Wafer
                        if (aln.Wafer != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (aln.GPIO.DO_WorkDetection && !GParam.theInst.IsSimulate)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !! gpio output bit6");
                            return true;
                        }
                        if (aln.IsMoving)
                        {
                            _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is moving!!", aln.BodyNo));
                            return true;
                        }
                        break;
                }

                //比對手臂TYPE與Aligner TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) != enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) != enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }

                
                //Bit8:RA320 Lower limit of spindle pressure RA420 Substrate detection sensor
                if (enumRobotAction != enumRobotAction.Standby)
                {
                    if (GParam.theInst.IsSimulate == false && aln.GPIO.DI_VacPressureOut_1)//吸住怎麼取 BIT 8
                    {
                        _errorLog.WriteLog("[ Interlock ]:ALN{0} spindle is vac on!!", aln.BodyNo);
                        return true;
                    }
                    //check lift pin zaxis need fix
                    if (aln.IsZaxsInBottom() == false)//GPOS
                    {
                        _errorLog.WriteLog("[ Interlock ]:ALN{0} Zaxis/Raxis is not safety!!", aln.BodyNo);
                        return true;
                    }
                }
                
                if (aln.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is error!!", aln.BodyNo));
                    return true;
                }

            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Aligner1_Interlock_Turtable(object trb, object oRobotAction, object oArm, object oSlot)    //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Aligner aln = ListALN[0];
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (aln.Wafer == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        if (aln.WaferExists() == false && GParam.theInst.IsSimulate == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer by sensor!!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //target has Wafer
                        if (aln.Wafer != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (aln.WaferExists() == true && GParam.theInst.IsSimulate == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer by sensor!!");
                            return true;
                        }
                        break;
                }

                //比對手臂TYPE與Aligner TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) != enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) != enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }

                if (aln.IsMoving)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is moving!!", aln.BodyNo));
                    return true;
                }
                if (aln.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is error!!", aln.BodyNo));
                    return true;
                }

                //確認R軸位置，只有在0/180度時robot才能取放
                if ((aln.Raxispos - GParam.theInst.GetTurnTable_angle_0(aln.BodyNo - 1) > 100) && (aln.Raxispos - GParam.theInst.GetTurnTable_angle_180(aln.BodyNo - 1) > 100))
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is not at robot get/put position!!", aln.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Aligner2_Interlock(object trb, object oRobotAction, object oArm, object oSlot)    //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Aligner aln = ListALN[1];
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (aln.Wafer == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //target has Wafer
                        if (aln.Wafer != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (aln.GPIO.DO_WorkDetection && !GParam.theInst.IsSimulate)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !! gpio output bit6");
                            return true;
                        }
                        break;
                }

                //比對手臂TYPE與Aligner TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) != enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) != enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }

                //check lift pin zaxis    
                if (aln.IsZaxsInBottom() == false)//GPOS
                {
                    _errorLog.WriteLog("[ Interlock ]:ALN{0} Zaxis is not safety!!", aln.BodyNo);
                    return true;
                }
                //Bit8:RA320 Lower limit of spindle pressure RA420 Substrate detection sensor
                if (enumRobotAction != enumRobotAction.Standby)
                    if (GParam.theInst.IsSimulate == false && aln.GPIO.DI_VacPressureOut_1)//吸住怎麼取 BIT 8
                    {
                        _errorLog.WriteLog("[ Interlock ]:ALN{0} spindle is vac on!!", aln.BodyNo);
                        return true;
                    }
                if (aln.IsMoving)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is moving!!", aln.BodyNo));
                    return true;
                }
                if (aln.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is error!!", aln.BodyNo));
                    return true;
                }

            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Aligner2_Interlock_Turtable(object trb, object oRobotAction, object oArm, object oSlot)    //手臂進去前檢查
        {
            I_Robot robot = trb as I_Robot;
            I_Aligner aln = ListALN[1];
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (aln.Wafer == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        if (aln.WaferExists() == false && GParam.theInst.IsSimulate == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer by sensor!!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //target has Wafer
                        if (aln.Wafer != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (aln.WaferExists() == true && GParam.theInst.IsSimulate == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer by sensor!!");
                            return true;
                        }
                        break;
                }

                //比對手臂TYPE與Aligner TYPE一致
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                    case enumRobotArms.BothArms:
                        if (robot.UpperArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) != enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        if (robot.UpperArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong upper finger type !!");
                            return true;
                        }
                        break;
                    case enumRobotArms.LowerArm:
                        if (robot.LowerArmFunc == enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) != enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        if (robot.LowerArmFunc != enumArmFunction.FRAME && GParam.theInst.GetAlignerMode(aln.BodyNo - 1) == enumAlignerType.TurnTable)
                        {
                            _errorLog.WriteLog("[ Interlock ]:Wrong lower finger type !!");
                            return true;
                        }
                        break;
                }

                if (aln.IsMoving)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is moving!!", aln.BodyNo));
                    return true;
                }
                if (aln.IsError)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is error!!", aln.BodyNo));
                    return true;
                }

                //確認夾爪狀態
                if (aln.IsUnClamp() == false)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is not unclamp!!", aln.BodyNo));
                    return true;
                }

                bool isAln1DegreeOK = Math.Abs(aln.Raxispos - GParam.theInst.GetTurnTable_angle_0(aln.BodyNo - 1)) <= 50
                                            || Math.Abs(aln.Raxispos - GParam.theInst.GetTurnTable_angle_180(aln.BodyNo - 1)) <= 50;
                //確認R軸位置，只有在0/180度時robot才能取放
                if (isAln1DegreeOK == false)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:ALN{0} is not at robot get/put position!!", aln.BodyNo));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]:<<Exception>>:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Buffer1_Interlock(object trb, object oRobotAction, object oArm, object oSlot)    //手臂進去前檢查
        {
            I_Buffer buf = ListBUF[0];
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (buf.IsSlotDisable(nSlot - 1))
                        {
                            _errorLog.WriteLog("[ Interlock ]:slot disable !!");
                            return true;
                        }
                        if (buf.GetWafer(nSlot - 1) == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        //if (buf.AllowedLoad(nSlot - 1) == false && GParam.theInst.IsSimulate == false)
                        //{//注意sensor邏輯
                        //    _errorLog.WriteLog("[ Interlock ]:The target not allowed!!");
                        //    return true;
                        //}
                        if (SpinWait.SpinUntil(() => buf.AllowedLoad(nSlot - 1) || GParam.theInst.IsSimulate, 1000) == false)
                        {
                            //注意sensor邏輯
                            _errorLog.WriteLog("[ Interlock ]:The target not allowed!!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //target has Wafer
                        if (buf.IsSlotDisable(nSlot - 1))
                        {
                            _errorLog.WriteLog("[ Interlock ]:slot disable !!");
                            return true;
                        }
                        if (buf.GetWafer(nSlot - 1) != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        //if (buf.AllowedUnld(nSlot - 1) == false && GParam.theInst.IsSimulate == false)
                        //{//注意sensor邏輯
                        //    _errorLog.WriteLog("[ Interlock ]:The target not allowed!!");
                        //    return true;
                        //}
                        if (SpinWait.SpinUntil(() => buf.AllowedUnld(nSlot - 1) || GParam.theInst.IsSimulate, 3000) == false)
                        {
                            //注意sensor邏輯
                            _errorLog.WriteLog("[ Interlock ]:The target not allowed!!");
                            return true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog(string.Format("[ Interlock ]:<<Exception>>:{0}", ex));
                return true;
            }
            return false;
        }
        private bool Buffer2_Interlock(object trb, object oRobotAction, object oArm, object oSlot)    //手臂進去前檢查
        {
            I_Buffer buf = ListBUF[1];
            try
            {
                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                int nSlot = (int)oSlot;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        //target no Wafer
                        if (buf.IsSlotDisable(nSlot - 1))
                        {
                            _errorLog.WriteLog("[ Interlock ]:slot disable !!");
                            return true;
                        }
                        if (buf.GetWafer(nSlot - 1) == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        if (SpinWait.SpinUntil(() => buf.AllowedLoad(nSlot - 1) || GParam.theInst.IsSimulate, 1000) == false)
                        {
                            //注意sensor邏輯
                            _errorLog.WriteLog("[ Interlock ]:The target not allowed!!");
                            return true;
                        }
                        break;
                    case enumRobotAction.Unlaod:
                        //target has Wafer
                        if (buf.IsSlotDisable(nSlot - 1))
                        {
                            _errorLog.WriteLog("[ Interlock ]:slot disable !!");
                            return true;
                        }
                        if (buf.GetWafer(nSlot - 1) != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (SpinWait.SpinUntil(() => buf.AllowedUnld(nSlot - 1) || GParam.theInst.IsSimulate, 1000) == false)
                        {
                            //注意sensor邏輯
                            _errorLog.WriteLog("[ Interlock ]:The target not allowed!!");
                            return true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog(string.Format("[ Interlock ]:<<Exception>>:{0}", ex));
                return true;
            }
            return false;
        }
        private bool Equipment1_Interlock(object trb, object oRobotAction, object oArm, object oSlot) //手臂進去前檢查
        {
            SSEquipment equipment = ListEQM[0];
            try
            {
                //  check position detect
                if (equipment.IsReady == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ incorrect location,pls check stage position sensor!!");
                    return true;
                }

                //  check processing 
                if (equipment.IsProcessing)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ is processing!!");
                    return true;
                }
                if (equipment.SetDoorOpenW() == false)//會等門開好
                {
                    _errorLog.WriteLog("[ Interlock ]:Failed to open Shutter door!!");
                    return true;
                }

                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        if (!equipment.Simulate && equipment.IsWaferExist == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer exist IO signal!!");
                            return true;
                        }
                        if (equipment.Wafer == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        if (equipment.IsReadyUnload == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target is not Ready to Unload!!");
                            return true;
                        }
                        equipment.SetRobotGetSMEMA(true);
                        break;
                    case enumRobotAction.Unlaod:
                        if (!equipment.Simulate && equipment.IsWaferExist == true)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target wafer exist IO signal!!");
                            return true;
                        }
                        if (equipment.Wafer != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (equipment.IsReadyLoad == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target is not Ready to Load!!");
                            return true;
                        }
                        equipment.SetRobotPutSMEMA(true);
                        break;
                }
                equipment.SetRobotExtendIO(true);

            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]<<Exception>> Equipment:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Equipment2_Interlock(object trb, object oRobotAction, object oArm, object oSlot) //手臂進去前檢查
        {
            SSEquipment equipment = ListEQM[1];
            try
            {
                //  check position detect
                if (equipment.IsReady == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ incorrect location,pls check stage position sensor!!");
                    return true;
                }

                //  check vacuum off
                if (equipment.DlgVacuumOff != null && equipment.IsWaferExist == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ vacuum is on,pls check stage vacuum!!");
                    return true;
                }

                //  check processing 
                if (equipment.IsProcessing)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ is processing!!");
                    return true;
                }

                if (equipment.SetDoorOpenW() == false)//會等門開好
                {
                    _errorLog.WriteLog("[ Interlock ]:Failed to open Shutter door!!");
                    return true;
                }

                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        if (!equipment.Simulate && equipment.IsWaferExist == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer exist IO signal!!");
                            return true;
                        }
                        if (equipment.Wafer == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        if (equipment.IsReadyUnload == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target is not Ready to Unload!!");
                            return true;
                        }
                        equipment.SetRobotGetSMEMA(true);
                        break;
                    case enumRobotAction.Unlaod:
                        if (!equipment.Simulate && equipment.IsWaferExist == true)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target wafer exist IO signal!!");
                            return true;
                        }
                        if (equipment.Wafer != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (equipment.IsReadyLoad == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target is not Ready to Load!!");
                            return true;
                        }
                        equipment.SetRobotPutSMEMA(true);
                        break;
                }
                equipment.SetRobotExtendIO(true);

            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]<<Exception>> Equipment:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Equipment3_Interlock(object trb, object oRobotAction, object oArm, object oSlot) //手臂進去前檢查
        {
            SSEquipment equipment = ListEQM[2];
            try
            {
                //  check position detect
                if (equipment.IsReady == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ incorrect location,pls check stage position sensor!!");
                    return true;
                }

                //  check vacuum off
                if (equipment.DlgVacuumOff != null && equipment.IsWaferExist == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ vacuum is on,pls check stage vacuum!!");
                    return true;
                }

                //  check processing 
                if (equipment.IsProcessing)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ is processing!!");
                    return true;
                }
                if (equipment.SetDoorOpenW() == false)//會等門開好
                {
                    _errorLog.WriteLog("[ Interlock ]:Failed to open Shutter door!!");
                    return true;
                }

                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        if (!equipment.Simulate && equipment.IsWaferExist == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer exist IO signal!!");
                            return true;
                        }
                        if (equipment.Wafer == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        if (equipment.IsReadyUnload == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target is not Ready to Unload!!");
                            return true;
                        }
                        equipment.SetRobotGetSMEMA(true);
                        break;
                    case enumRobotAction.Unlaod:
                        if (!equipment.Simulate && equipment.IsWaferExist == true)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target wafer exist IO signal!!");
                            return true;
                        }
                        if (equipment.Wafer != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (equipment.IsReadyLoad == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target is not Ready to Load!!");
                            return true;
                        }
                        equipment.SetRobotPutSMEMA(true);
                        break;
                }                
                equipment.SetRobotExtendIO(true);

            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]<<Exception>> Equipment:{0}", ex);
                return true;
            }
            return false;
        }
        private bool Equipment4_Interlock(object trb, object oRobotAction, object oArm, object oSlot) //手臂進去前檢查
        {
            SSEquipment equipment = ListEQM[3];
            try
            {
                //  check position detect
                if (equipment.IsReady == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ incorrect location,pls check stage position sensor!!");
                    return true;
                }

                //  check vacuum off
                if (equipment.DlgVacuumOff != null && equipment.IsWaferExist == false)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ vacuum is on,pls check stage vacuum!!");
                    return true;
                }

                //  check processing 
                if (equipment.IsProcessing)
                {
                    _errorLog.WriteLog("[ Interlock ]:EQ is processing!!");
                    return true;
                }

                if (equipment.SetDoorOpenW() == false)//會等門開好
                {
                    _errorLog.WriteLog("[ Interlock ]:Failed to open Shutter door!!");
                    return true;
                }

                enumRobotAction enumRobotAction = (enumRobotAction)oRobotAction;
                enumRobotArms eArm = (enumRobotArms)oArm;
                switch (enumRobotAction)
                {
                    case enumRobotAction.Load:
                        if (!equipment.Simulate && equipment.IsWaferExist == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer exist IO signal!!");
                            return true;
                        }
                        if (equipment.Wafer == null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target no wafer !!");
                            return true;
                        }
                        if (equipment.IsReadyUnload == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target is not Ready to Unload!!");
                            return true;
                        }
                        equipment.SetRobotGetSMEMA(true);
                        break;
                    case enumRobotAction.Unlaod:
                        if (!equipment.Simulate && equipment.IsWaferExist == true)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target wafer exist IO signal!!");
                            return true;
                        }
                        if (equipment.Wafer != null)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target has wafer !!");
                            return true;
                        }
                        if (equipment.IsReadyLoad == false)
                        {
                            _errorLog.WriteLog("[ Interlock ]:The target is not Ready to Load!!");
                            return true;
                        }
                        equipment.SetRobotPutSMEMA(true);
                        break;
                }                
                equipment.SetRobotExtendIO(true);

            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ Interlock ]<<Exception>> Equipment:{0}", ex);
                return true;
            }
            return false;
        }

        /*private bool RbtLoadEQ_BeforeOK()     //手臂取EQ Wafer前要做的事情
        {
            try
            {
                //Lift pin 上升
                _equipment.SetEQ_LiftPinUp(true);

                //等待到位
                if ((SpinWait.SpinUntil(() => _equipment.DlgLeftLiftPinUp() && _equipment.DlgRightLiftPinUp(), 5000)) == false)
                {
                    throw new SException((int)(enumLoadPortError.InterlockStop), "RbtUnldEQ_AfterOK Timeout");
                }

                return true;
            }
            catch (SException ex) { WriteLog("<<SException>> RbtLoadEQ_Before:" + ex); }
            catch (Exception ex) { WriteLog("<<Exception>> RbtLoadEQ_Before:" + ex); }
            return false;//不OK
        }
        private bool RbtUnldEQ_BeforeOK()     //手臂放至EQ Wafer前要做的事情
        {
            try
            {
                //Lift pin 上升前要檢查Ready訊號是否On

                //Lift pin 上升
                _equipment.SetEQ_LiftPinUp(true);

                //等待到位
                if ((SpinWait.SpinUntil(() => _equipment.DlgLeftLiftPinUp() && _equipment.DlgRightLiftPinUp(), 5000)) == false)
                {
                    throw new SException((int)(enumLoadPortError.InterlockStop), "RbtUnldEQ_AfterOK Timeout");
                }

                return true;//OK
            }
            catch (SException ex) { WriteLog("<<SException>> RbtUnldEQ_Before:" + ex); }
            catch (Exception ex) { WriteLog("<<Exception>> RbtUnldEQ_Before:" + ex); }
            return false;//不OK
        }*/
        private bool RbtUnldEQ_AfterOK()      //手臂放至EQ Wafer後要做的事情
        {
            try
            {
                return true;//OK
            }
            catch (SException ex) { WriteLog("<<SException>> RbtUnldEQ_AfterOK:" + ex); }
            catch (Exception ex) { WriteLog("<<Exception>> RbtUnldEQ_AfterOK:" + ex); }
            return false;//不OK
        }
        //================================================================================
        private bool LoadportDockUndkInterlock(object sender)
        {
            I_Loadport lp = sender as I_Loadport;
            I_E84 e84 = ListE84[lp.BodyNo - 1];
            try
            {
                foreach (I_Robot trb in ListTRB)
                {
                    if (!trb.Disable && trb.IsError)
                    {
                        _errorLog.WriteLog(string.Format("[ Interlock ]:The robot is error STG{0} can not move!!", lp.BodyNo));
                        lp.TriggerSException(enumLoadPortError.RobotError);
                        return true;
                    }
                }
                if (lp.IsRobotExtend)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:The robot in STG{0} extand flag On!!", lp.BodyNo));
                    lp.TriggerSException(enumLoadPortError.Robot_Extended);
                    return true;
                }
                if (lp.IsInfoPadEnable() == false)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} info-pad is disable", lp.BodyNo));
                    lp.TriggerSException(enumLoadPortError.InfoPad_Disable);
                    return true;
                }
                if (lp.FoupExist == false)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]:The robot in STG{0} extand flag On!!", lp.BodyNo));
                    lp.TriggerSException(enumLoadPortError.Foup_Not_Exist);
                    return true;
                }
                if (e84.Disable == false && e84.isCs0On)
                {
                    _errorLog.WriteLog(string.Format("[ Interlock ]E84_{0} CS0 On", lp.BodyNo));
                    lp.TriggerSException(enumLoadPortError.E84_Handshake);
                    return true;
                }


                switch (GParam.theInst.GetLoadportMode(lp.BodyNo - 1))
                {
                    case enumLoadportType.RV201:
                        break;
                    case enumLoadportType.RB201:
                        break;
                    case enumLoadportType.Other:// RC550 IO組成的LP                        
                        if (lp.IsInfoPadTrbMapEnable() == false)
                        {
                            _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} info-pad mapping is disable", lp.BodyNo));
                            lp.TriggerSException(enumLoadPortError.InfoPad_Mapping_Disable);
                            return true;
                        }
                        if (lp.IsProtrude)
                        {
                            _errorLog.WriteLog(string.Format("[ Interlock ]:STG{0} is protrude", lp.BodyNo));
                            lp.TriggerSException(enumLoadPortError.Detecting_Protrude);
                            return true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog(string.Format("[ Interlock ]:<<Exception>>:{0}", ex));
                return true;
            }
            return false;
        }


        //  EQ有實體的訊號(rc530)
        private bool EQ_WaferExist(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            try
            {
                if (equipment.Simulate || GParam.theInst.IsSimulate || equipment.Disable)
                {
                    return equipment.Wafer != null;
                }
                else if (equipment._BodyNo == 1)
                {
                    bool bExist = ListDIO[2].GetGDIO_InputStatus(0, 0);
                    return bExist;
                }
                else if (equipment._BodyNo == 2)
                {
                    bool bExist = ListDIO[2].GetGDIO_InputStatus(0, 2);
                    return bExist;
                }
                else if (equipment._BodyNo == 3)
                {
                    bool bExist = ListDIO[2].GetGDIO_InputStatus(0, 4);
                    return bExist;
                }
                else if (equipment._BodyNo == 4)
                {
                    bool bExist = ListDIO[2].GetGDIO_InputStatus(0, 6);
                    return bExist;
                }
                else
                {
                    return true;
                }
            }
            catch (SException ex)
            {
                _errorLog.WriteLog(string.Format("[ MDI ]:<<SException>> EQ{0}: EQ_WaferExist:" + ex, equipment._BodyNo));
                return false;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog(string.Format("[ MDI ]:<<Exception>> EQ{0}: EQ_WaferExist:" + ex, equipment._BodyNo));
                return false;
            }
        }

        //  EQ有實體的訊號(rc530)
        private bool EQ_StageReady(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            try
            {
                if (equipment.Simulate || GParam.theInst.IsSimulate || equipment.Disable)
                {
                    return equipment.Wafer != null;
                }
                else if (equipment._BodyNo == 1)
                {
                    bool bExist = ListDIO[2].GetGDIO_InputStatus(0, 1);
                    return bExist;
                }
                else if (equipment._BodyNo == 2)
                {
                    bool bExist = ListDIO[2].GetGDIO_InputStatus(0, 3);
                    return bExist;
                }
                else if (equipment._BodyNo == 3)
                {
                    bool bExist = ListDIO[2].GetGDIO_InputStatus(0, 5);
                    return bExist;
                }
                else if (equipment._BodyNo == 4)
                {
                    bool bExist = ListDIO[2].GetGDIO_InputStatus(0, 7);
                    return bExist;
                }
                else
                {
                    return true;
                }
            }
            catch (SException ex)
            {
                _errorLog.WriteLog(string.Format("[ MDI ]:<<SException>> EQ{0}: EQ_StageReady:" + ex, equipment._BodyNo));
                return false;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog(string.Format("[ MDI ]:<<Exception>> EQ{0}: EQ_StageReady:" + ex, equipment._BodyNo));
                return false;
            }

        }
        private bool EQ_MaintDoorOpen(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            try
            {
                if (equipment.Simulate || GParam.theInst.IsSimulate || equipment.Disable)
                {
                    return false;
                }
                else if (equipment._BodyNo == 1)
                {
                    bool bOpen = ListDIO[2].GetGDIO_InputStatus(0, 8);
                    return bOpen;
                }
                else if (equipment._BodyNo == 2)
                {
                    bool bOpen = ListDIO[2].GetGDIO_InputStatus(0, 9);
                    return bOpen;
                }
                else if (equipment._BodyNo == 3)
                {
                    bool bOpen = ListDIO[2].GetGDIO_InputStatus(0, 10);
                    return bOpen;
                }
                else if (equipment._BodyNo == 4)
                {
                    bool bOpen = ListDIO[2].GetGDIO_InputStatus(0, 11);
                    return bOpen;
                }
                else
                {
                    return false;
                }
            }
            catch (SException ex)
            {
                _errorLog.WriteLog(string.Format("[ MDI ]:<<SException>> EQ{0}: EQ_MaintDoorOpen:" + ex, equipment._BodyNo));
                return false;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog(string.Format("[ MDI ]:<<Exception>> EQ{0}: EQ_MaintDoorOpen:" + ex, equipment._BodyNo));
                return false;
            }

        }
        private bool IsShutterDoorOpen(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            bool bOpen = false;
            (int OpenDo, int CloseDo, int OpenDi, int CloseDi) bits;

            if (!_shutterMap.TryGetValue(equipment._BodyNo, out bits))
            {
                return bOpen;
            }

            bOpen = ListDIO[4].GetGDIO_InputStatus(0, bits.OpenDi);
            return bOpen;
        }
        private bool IsShutterDoorClose(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            bool bClose = false;
            (int OpenDo, int CloseDo, int OpenDi, int CloseDi) bits;

            if (!_shutterMap.TryGetValue(equipment._BodyNo, out bits))
            {
                return bClose;
            }

            bClose = ListDIO[4].GetGDIO_InputStatus(0, bits.CloseDi);
            return bClose;
        }
        private bool isEQDetectFinger(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            bool bDetected = true;
            if (equipment._BodyNo == 1)
            {
                bDetected = !ListDIO[4].GetGDIO_InputStatus(0, 8);
                return bDetected;
            }
            else if (equipment._BodyNo == 2)
            {
                bDetected = !ListDIO[4].GetGDIO_InputStatus(0, 9);
                return bDetected;
            }
            else if (equipment._BodyNo == 3)
            {
                bDetected = !ListDIO[4].GetGDIO_InputStatus(0, 10);
                return bDetected;
            }
            else if (equipment._BodyNo == 4)
            {
                bDetected = !ListDIO[4].GetGDIO_InputStatus(0, 11);
                return bDetected;
            }
            return bDetected;
        }
        private bool isEFEMExtedToEQ(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            int bit;
            bool result;   // 最終要回傳的 "extend"
            bool raw;      // 從 DIO 讀回來的原始值

            // 處理 bodyNo 不合法
            switch (equipment._BodyNo)
            {
                case 1: bit = 0; break;
                case 2: bit = 1; break;
                case 3: bit = 2; break;
                case 4: bit = 3; break;
                default:
                    return true;
            }

            raw = ListDIO[2].GetGDIO_OutputStatus(0, bit);


            // EQIOSwitchToExtend:
            // 0: raw 表示 retracted(縮回)  => extend = !raw  (原本邏輯)
            // 1: raw 表示 extended(伸出)   => extend = raw
            if (GParam.theInst.EQIOSwitchToExtend == 0)
                result = !raw;
            else
                result = raw;

            return result;
        }

        private bool ShutterDoorOpenW(object sender)
<<<<<<< HEAD
        {
            SSEquipment equipment = sender as SSEquipment;
            string ErrorMSG = "";
            (int OpenDo, int CloseDo, int OpenDi, int CloseDi) bits;

            if (!_shutterMap.TryGetValue(equipment._BodyNo, out bits))
            {
                ErrorMSG = $"ShutterDoorOpenW: shutterMap not found for BodyNo={equipment._BodyNo}.";
                return false;
            }

            if (isEQDetectFinger(equipment))
            {
                ErrorMSG = "ShutterDoorOpenW: EQ detect finger interlock.";
                return false;
            }

            if (isEFEMExtedToEQ(equipment))
            {
                ErrorMSG = "ShutterDoorOpenW: EFEM extended to EQ interlock.";
                return false;
            }

            if (IsShutterDoorOpen(equipment))
                return true;


            try
            {
                ShutterDoorOpen(equipment);

                bool ok = SpinWait.SpinUntil(() => IsShutterDoorOpen(equipment), shutterDoorTimeout);
                if (!ok)
                    ErrorMSG = "ShutterDoorOpenW: timeout waiting door open DI.";

                return ok;
            }
            catch (Exception ex)
            {
                ErrorMSG = $"ShutterDoorOpenW exception: {ex.Message}";
                _errorLog?.WriteLog("[ EQ ] <<Exception>> ShutterDoorOpenW:" + ex);

                return false;
            }
            finally
            {
                try
                {
                    ListDIO[4].SdobW(0, bits.OpenDo, false);
                }
                catch (Exception ex)
                {
                    _errorLog?.WriteLog("[ EQ ] <<Exception>> ShutterDoorOpenW finally:" + ex);
                }
            }
        }
        private bool ShutterDoorOpen(object sender)
=======
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
        {
            SSEquipment equipment = sender as SSEquipment;
            string ErrorMSG = "";
            (int OpenDo, int CloseDo, int OpenDi, int CloseDi) bits;

            if (!_shutterMap.TryGetValue(equipment._BodyNo, out bits))
            {
                ErrorMSG = $"ShutterDoorOpenW: shutterMap not found for BodyNo={equipment._BodyNo}.";
                return false;
            }

            if (isEQDetectFinger(equipment) && !GParam.theInst.IsSimulate)
            {
                ErrorMSG = $"ShutterDoorOpenW: EQ detect finger interlock for BodyNo={equipment._BodyNo}.";
                return false;
            }

            //if (isEFEMExtedToEQ(equipment))
            //{
            //    ErrorMSG = "ShutterDoorOpenW: EFEM extended to EQ interlock.";
            //    return false;
            //}

            if (IsShutterDoorOpen(equipment))
                return true;


            try
            {
                if (GParam.theInst.IsSimulate) return true;
                ListDIO[4].SdobW(0, bits.CloseDo, false);
                Thread.Sleep(100);
                ListDIO[4].SdobW(0, bits.OpenDo, true);
<<<<<<< HEAD
                return true;
=======

                bool ok = SpinWait.SpinUntil(() => IsShutterDoorOpen(equipment), shutterDoorTimeout);
                if (!ok)
                    ErrorMSG = $"ShutterDoorOpenW: timeout waiting door open DI for BodyNo={equipment._BodyNo}..";

                return ok;
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
            }
            catch (SException ex)
            {
                ErrorMSG = $"ShutterDoorOpenW exception: {ex.Message}";
                _errorLog?.WriteLog(string.Format("EQ{0}: [ EQ ] <<SException>> ShutterDoorOpenW:" + ex, equipment._BodyNo));

                return false;
            }
            catch (Exception ex)
            {
                ErrorMSG = $"ShutterDoorOpenW exception: {ex.Message}";
                _errorLog?.WriteLog(string.Format("EQ{0}: [ EQ ] <<Exception>> ShutterDoorOpenW:" + ex, equipment._BodyNo));

                return false;
            }
            finally
            {
                try
                {
                    ListDIO[4].SdobW(0, bits.OpenDo, false);
                }
                catch (Exception ex)
                {
                    _errorLog?.WriteLog(string.Format("EQ{0} [ EQ ] <<Exception>> ShutterDoorOpenW finally:" + ex, equipment._BodyNo));
                }
            }
        }
<<<<<<< HEAD
=======
        //private bool ShutterDoorOpen(object sender)
        //{
        //    SSEquipment equipment = sender as SSEquipment;
        //    string ErrorMSG = "";
        //    (int OpenDo, int CloseDo, int OpenDi, int CloseDi) bits;

        //    if (!_shutterMap.TryGetValue(equipment._BodyNo, out bits))
        //    {
        //        ErrorMSG = $"ShutterDoorOpen: shutterMap not found for BodyNo={equipment._BodyNo}.";
        //        return false;
        //    }

        //    if (isEQDetectFinger(equipment))
        //    {
        //        ErrorMSG = "ShutterDoorOpen: EQ detect finger interlock.";
        //        return false;
        //    }

        //    if (isEFEMExtedToEQ(equipment))
        //    {
        //        ErrorMSG = "ShutterDoorOpen: EFEM extended to EQ interlock.";
        //        return false;
        //    }

        //    if (IsShutterDoorOpen(equipment))
        //        return true;


        //    try
        //    {
        //        ListDIO[4].SdobW(0, bits.CloseDo, false);
        //        Thread.Sleep(100);
        //        ListDIO[4].SdobW(0, bits.OpenDo, true);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMSG = $"ShutterDoorOpen exception: {ex.Message}";
        //        _errorLog?.WriteLog("[ EQ ] <<Exception>> ShutterDoorOpen:" + ex);

        //        return false;
        //    }
        //    finally
        //    {
        //        try
        //        {
        //            ListDIO[4].SdobW(0, bits.OpenDo, false);
        //        }
        //        catch (Exception ex)
        //        {
        //            _errorLog?.WriteLog("[ EQ ] <<Exception>> ShutterDoorOpen finally:" + ex);
        //        }
        //    }
        //}
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
        private bool ShutterDoorCloseW(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            string ErrorMSG = "";
            (int OpenDo, int CloseDo, int OpenDi, int CloseDi) bits;

            if (!_shutterMap.TryGetValue(equipment._BodyNo, out bits))
            {
                ErrorMSG = $"ShutterDoorCloseW: shutterMap not found for BodyNo={equipment._BodyNo}.";
                return false;
            }

            if (isEQDetectFinger(equipment))
            {
<<<<<<< HEAD
                ErrorMSG = "ShutterDoorCloseW: EQ detect finger interlock.";
=======
                ErrorMSG = $"ShutterDoorCloseW: EQ detect finger interlock for BodyNo={equipment._BodyNo}.";
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
                return false;
            }

            if (isEFEMExtedToEQ(equipment))
            {
<<<<<<< HEAD
                ErrorMSG = "ShutterDoorCloseW: EFEM extended to EQ interlock.";
=======
                ErrorMSG = $"ShutterDoorCloseW: EFEM extended to EQ interlock for BodyNo={equipment._BodyNo}.";
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
                return false;
            }

            if (IsShutterDoorClose(equipment))
                return true;


            try
            {
                ListDIO[4].SdobW(0, bits.OpenDo, false);
                Thread.Sleep(100);
                ListDIO[4].SdobW(0, bits.CloseDo, true);

                Stopwatch sw = Stopwatch.StartNew();

                while (!IsShutterDoorClose(equipment))
                {
                    if (sw.ElapsedMilliseconds >= shutterDoorTimeout)
                    {
<<<<<<< HEAD
                        ErrorMSG = "ShutterDoorCloseW: timeout waiting door close DI.";
=======
                        ErrorMSG = $"ShutterDoorCloseW: timeout waiting door close DI  for BodyNo={equipment._BodyNo}.";
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
                        _errorLog?.WriteLog("[ EQ ] " + ErrorMSG);
                        return false;
                    }

                    // 例：危險狀態解除時做一次額外處理
<<<<<<< HEAD
                    if (!isEQDetectFinger(equipment))
=======
                    if (isEQDetectFinger(equipment))
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
                    {
                        equipment.TriggerSException(enumEQError.ShutterDoor1_protrude_sensor_detect + (equipment._BodyNo - 1));
                    }

                    Thread.Sleep(100);
                }
                return true;
            }
            catch (SException ex)
            {
                ErrorMSG = $"ShutterDoorCloseW exception: {ex.Message}";
<<<<<<< HEAD
                _errorLog?.WriteLog("[ EQ ] <<Exception>> ShutterDoorCloseW:" + ex);
=======
                _errorLog?.WriteLog(string.Format("EQ{0}: [ EQ ] <<SException>> ShutterDoorCloseW:" + ex, equipment._BodyNo));
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger

                return false;
            }
            catch (Exception ex)
            {
                ErrorMSG = $"ShutterDoorCloseW exception: {ex.Message}";
<<<<<<< HEAD
                _errorLog?.WriteLog("[ EQ ] <<Exception>> ShutterDoorCloseW:" + ex);
=======
                _errorLog?.WriteLog(string.Format("EQ{0}: [ EQ ] <<Exception>> ShutterDoorCloseW:" + ex, equipment._BodyNo));
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger

                return false;
            }
            
            finally
            {
                try
                {
                    ListDIO[4].SdobW(0, bits.CloseDo, false);
                }
                catch (Exception ex)
                {
<<<<<<< HEAD
                    _errorLog?.WriteLog("[ EQ ] <<Exception>> ShutterDoorCloseW finally error:" + ex);
=======
                    _errorLog?.WriteLog(string.Format("EQ{0} [ EQ ] <<Exception>> ShutterDoorCloseW finally:" + ex, equipment._BodyNo));
>>>>>>> debug/Shutterdoor-close-sensor-check-alarm-trigger
                }
            }
        }
        private bool robotExtendCtrlIO(object sender, bool bExtend)
        {
            SSEquipment equipment = sender as SSEquipment;
            int bit;
            // 處理 bodyNo 不合法
            switch (equipment._BodyNo)
            {
                case 1: bit = 0; break;
                case 2: bit = 1; break;
                case 3: bit = 2; break;
                case 4: bit = 3; break;
                default:
                    return false;
            }
            if (GParam.theInst.EQIOSwitchToExtend == 0)
            {
                ListDIO[2].SdobW(0, bit, !bExtend);
                ListDIO[2].GdioW(0, bit);
            }                
            else
                ListDIO[2].SdobW(0, bit, bExtend);
            return true;
        }

        //adam6066 傳送片交握訊號
        private bool EQ_ReadyToUnload(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            try
            {
                if (!_EQ_SMEMA_Map.TryGetValue(equipment._BodyNo, out var bits))
                {
                    _errorLog.WriteLog($"[ MDI ]: EQ_ReadyToUnload: Cannot find SMEMA parameter, eqNo={equipment._BodyNo}");
                    throw new Exception("EQ_ReadyToUnload: Cannot find SMEMA parameter, eqNo={equipment._BodyNo}");
                }
                return ListAdam[bits.nAdam].getInputValue(bits.Di_RDYtoUnload);
            }   
            catch (SException ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<SException>> EQ_ReadyToUnload:" + ex);
                return false;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> EQ_ReadyToUnload:" + ex);
                return false;
            }
        }

        private bool EQ_ReadyToLoad(object sender)
        {
            SSEquipment equipment = sender as SSEquipment;
            try
            {
                if (!_EQ_SMEMA_Map.TryGetValue(equipment._BodyNo, out var bits))
                {
                    _errorLog.WriteLog($"[ MDI ]: EQ_ReadyToLoad: Cannot find SMEMA parameter, eqNo={equipment._BodyNo}");
                    throw new Exception("EQ_ReadyToLoad: Cannot find SMEMA parameter, eqNo={equipment._BodyNo}!");
                }
                return ListAdam[bits.nAdam].getInputValue(bits.Di_RDYtoLoad);
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<SException>> EQ_ReadyToLoad:" + ex);
                return false;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> EQ_ReadyToUnload:" + ex);
                return false;
            }
        }

        private bool robotGetEQCtrlIO(object sender, bool bExtend)
        {
            SSEquipment equipment = sender as SSEquipment;
            try
            {
                if (!_EQ_SMEMA_Map.TryGetValue(equipment._BodyNo, out var bits))
                {
                    _errorLog.WriteLog($"[ MDI ]: robotGetEQCtrlIO: Cannot find SMEMA parameter, eqNo={equipment._BodyNo}");
                    throw new Exception("robotGetEQCtrlIO: Cannot find SMEMA parameter!");
                }
                ListAdam[bits.nAdam].setOutputValue(bits.Do_RDYtoUnload, bExtend);
                return true;
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<SException>> robotGetEQCtrlIO:" + ex);
                return false;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> robotGetEQCtrlIO:" + ex);
                return false;
            }

        }
        private bool robotPutEQCtrlIO(object sender, bool bExtend)
        {
            SSEquipment equipment = sender as SSEquipment;
            try
            {
                if (!_EQ_SMEMA_Map.TryGetValue(equipment._BodyNo, out var bits))
                {
                    _errorLog.WriteLog($"[ MDI ]: robotPutEQCtrlIO: Cannot find SMEMA parameter, eqNo={equipment._BodyNo}");
                    throw new Exception("robotPutEQCtrlIO: Cannot find SMEMA parameter!");
                }
                ListAdam[bits.nAdam].setOutputValue(bits.Do_RDYtoLoad, bExtend);
                return true;
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<SException>> robotPutEQCtrlIO:" + ex);
                return false;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> robotPutEQCtrlIO:" + ex);
                return false;
            }

        }


        private bool isPanelMisalign(enumPosition sender)
        {    
            try
            {
                enumPosition enumPos = sender;
                int bit;

                // 處理 bodyNo 不合法
                switch (enumPos)
                {
                    case enumPosition.EQM1: bit = 9; break;
                    case enumPosition.EQM2: bit = 10; break;
                    case enumPosition.EQM3: bit = 11; break;
                    case enumPosition.EQM4: bit = 12; break;
                    default:
                        return true;
                }

                return ListDIO[3].GetGDIO_InputStatus(0, bit);
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<SException>> isPanelMisalign:" + ex);
                return true;
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> isPanelMisalign:" + ex);
                return true;
            }

        }


        //  EQ有實體的訊號(rc530)控制手臂伸出
        private void SetEQ_ExtendFlag(bool bValue)
        {
            try
            {
                //if (_rc530_2 != null && !_rc530_2.Disable)
                //{
                //    bool bOn = _rc530_2.GetGDIO_OutputStatus(0, 0);
                //    bool bExtend = (bOn == false);//電控設計 off extend
                //    if (bExtend != bValue)
                //    {
                //        _rc530_2.SdobW(0, 0, !bValue);//value change
                //    }
                //}
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<SException>> EQ_ExtendFlag:" + ex);
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> EQ_ExtendFlag:" + ex);
            }
        }
        private bool GetEQ_ExtendFlag()
        {
            bool bExtend = true;
            try
            {
                //if (_rc530_2 != null && !_rc530_2.Disable)
                //{
                //    bool bOn = _rc530_2.GetGDIO_OutputStatus(0, 0);
                //    bExtend = (bOn == false);//電控設計 off extend                  
                //}
            }
            catch (SException ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<SException>> GetEQ_ExtendFlag:" + ex);
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> GetEQ_ExtendFlag:" + ex);
            }
            return bExtend;
        }


        #endregion

        //====================      拉到外層方便直接改
        void _loadport_OnSimulateMapping(object sender, EventArgs e)
        {
            //取得load port
            I_Loadport loaderUnit = sender as I_Loadport;

            string strOldMappingData = loaderUnit.MappingData;

            if (strOldMappingData == string.Empty)//2022.07.08 
            {
                loaderUnit.SimulateMappingData = GParam.theInst.GetSimulateGmap(loaderUnit.BodyNo - 1);
            }
            else
            {
                loaderUnit.SimulateMappingData = strOldMappingData;
            }

        }
        void _loadport_OnFoupExistChenge(object sender, FoupExisteChangEventArgs e)//讀RFID
        {
            try
            {
                //取得load port
                I_Loadport loaderUnit = sender as I_Loadport;

                _SECSUtilty.FoupExistChenge(loaderUnit, e);

                string strFoupID = "ReadFail";

                if ((loaderUnit.FoupExist) == false)
                {
                    //可清掉Warning
                    _alarm.ResetLPWarning(loaderUnit.BodyNo);
                    return;
                }

                if (GParam.theInst.IsSimulate)
                {
                    frmTextbox myTextbox = new frmTextbox();
                    myTextbox.Text = "Simulate Carrier ID";
                    int x = System.Windows.Forms.Cursor.Position.X;
                    int y = System.Windows.Forms.Cursor.Position.Y;
                    myTextbox.Location = new Point(x, y);

                    if (DialogResult.OK == myTextbox.ShowDialog())
                    {
                        strFoupID = myTextbox.GetTextboxString();
                    }
                    else
                    {
                        strFoupID = "TEST_" + loaderUnit.BodyNo;
                    }
                }

                if (GParam.theInst.GetSystemType == enumSystemType.ActiveEFEM ||
                    GParam.theInst.GetSystemType == enumSystemType.Sorter)
                {
                    if (!GParam.theInst.IsSimulate)
                    {
                        strFoupID = "ReadFail";

                        if (RFIDList[loaderUnit.BodyNo - 1] != null)
                        {
                            for (int i = 0; i < 3; i++)//retry
                            {
                                strFoupID = RFIDList[loaderUnit.BodyNo - 1].ReadMID();

                                //讀成功 211224 Ming
                                if (strFoupID != "ReadFail" && GParam.theInst.GetRFID_Bit != -1)
                                {
                                    if (strFoupID.Length < GParam.theInst.GetRFID_Bit)//user設定長度，不足補
                                        strFoupID = strFoupID.PadRight(GParam.theInst.GetRFID_Bit, ' ');
                                    else//user設定長度，超過砍
                                        strFoupID = strFoupID.Substring(0, GParam.theInst.GetRFID_Bit);
                                    break;
                                }
                            }
                        }
                        if (strFoupID == "ReadFail" && loaderUnit.IsBarcodeEnable)
                        {
                            strFoupID = loaderUnit.BarcodeRead();

                            if (strFoupID != "")
                                strFoupID = strFoupID.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");
                            else
                                strFoupID = "ReadFail";
                        }

                    }


                    ListSTG[loaderUnit.BodyNo - 1].FoupID = strFoupID;//讀失敗字串是ReadFail

                    _SECSUtilty.ReadIDcomplete(loaderUnit, e);

                    if (_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE) // Remote need Clamp
                        ListSTG[loaderUnit.BodyNo - 1].CLMP1();
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> _loadport_OnFoupExistChenge:{0}", ex);
            }
        }

        //==================== 
        private void tmrUI_Tick(object sender, EventArgs e)
        {
            try
            {



                labTimes.Text = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss");
                labPowerMode.Text = IsMaintMode_EFEM() ? "EFEM Maint" : "EFEM Run";
                if (GParam.theInst.EquipmentShowName == "" && pnlShowName.Visible)
                {
                    pnlShowName.Visible = false;
                }
                else if (GParam.theInst.EquipmentShowName != "")
                {
                    pnlShowName.Visible = true;
                    lblShowName.Text = GParam.theInst.EquipmentShowName;
                }

                DoPingIP();

                #region userCtrl Signal tower
                //Red
                if (IsSignalTowerOn(enumSignalTowerColor.RedBlinking))
                {
                    userLight1.LightRedStatus = userLight1.LightRedStatus == GUI.GUILight.enuStatus.eOn ? GUI.GUILight.enuStatus.eOff : GUI.GUILight.enuStatus.eOn;
                }
                else if (IsSignalTowerOn(enumSignalTowerColor.Red))
                {
                    userLight1.LightRedStatus = GUI.GUILight.enuStatus.eOn;
                }
                else
                {
                    userLight1.LightRedStatus = GUI.GUILight.enuStatus.eOff;
                }
                //Yellow
                if (IsSignalTowerOn(enumSignalTowerColor.YellowBlinking))
                {
                    userLight1.LightYellowStatus = userLight1.LightYellowStatus == GUI.GUILight.enuStatus.eOn ? GUI.GUILight.enuStatus.eOff : GUI.GUILight.enuStatus.eOn;
                }
                else if (IsSignalTowerOn(enumSignalTowerColor.Yellow))
                {
                    userLight1.LightYellowStatus = GUI.GUILight.enuStatus.eOn;
                }
                else
                {
                    userLight1.LightYellowStatus = GUI.GUILight.enuStatus.eOff;
                }
                //Green
                if (IsSignalTowerOn(enumSignalTowerColor.GreenBlinking))
                {
                    userLight1.LightGreenStatus = userLight1.LightGreenStatus == GUI.GUILight.enuStatus.eOn ? GUI.GUILight.enuStatus.eOff : GUI.GUILight.enuStatus.eOn;
                }
                else if (IsSignalTowerOn(enumSignalTowerColor.Green))
                {
                    userLight1.LightGreenStatus = GUI.GUILight.enuStatus.eOn;
                }
                else
                {
                    userLight1.LightGreenStatus = GUI.GUILight.enuStatus.eOff;
                }
                //Bule
                if (IsSignalTowerOn(enumSignalTowerColor.BlueBlinking))
                {
                    userLight1.LightBlueStatus = userLight1.LightBlueStatus == GUI.GUILight.enuStatus.eOn ? GUI.GUILight.enuStatus.eOff : GUI.GUILight.enuStatus.eOn;
                }
                else if (IsSignalTowerOn(enumSignalTowerColor.Blue))
                {
                    userLight1.LightBlueStatus = GUI.GUILight.enuStatus.eOn;
                }
                else
                {
                    userLight1.LightBlueStatus = GUI.GUILight.enuStatus.eOff;
                }
                #endregion

                if (!GParam.theInst.GetIO_Flog())
                {
                    #region Alarm Light
                    if (_alarm != null)
                    {
                        if (_alarm.CurrentAlarm != null && _alarm.IsAlarm())
                        {
                            btnAlarm.BackColor = btnAlarm.BackColor == Color.Pink ? SystemColors.ActiveCaptionText : Color.Pink;

                            if (_alarm.IsOnlyWarning())//如果是有警告
                            {
                                //不停機直接叫
                                _alarm.AlarmBuzzer2On();
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtErrorOccurring), true);
                            }
                            else if (activeForm != _afrmAlarm[0] && _afrmAlarm[0].Visible == false)//發生異常強制切換到異常頁面
                            {
                                btnAlarm.PerformClick();//切換頁面
                                                        //  燈塔亮燈On 蜂鳴器吵你                               
                                _alarm.AlarmBuzzerOn();
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtErrorOccurring), true);
                            }
                            else
                            {
                                //  燈塔亮燈On 蜂鳴器吵你                     
                                _alarm.AlarmBuzzerOn();
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtErrorOccurring), true);
                            }
                        }
                        else
                        {
                            if (GParam.theInst.FreeStyle)
                                btnAlarm.BackColor = GParam.theInst.ColorButton;
                            else
                                btnAlarm.BackColor = SystemColors.ActiveCaptionText;



                            _alarm.AlarmBuzzerOff();
                            CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtErrorOccurring), false);
                        }
                    }
                    #endregion

                    #region Maintenance/Operator Light
                    if (m_pageSelectLast != m_pageSelect)
                    {
                        m_pageSelectLast = m_pageSelect;
                        switch (m_pageSelectLast)
                        {
                            case ePageSerial.Status:
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOperator), false);
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtMaintenance), false);
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtIdle), IsProcess() == false);
                                break;
                            case ePageSerial.Teaching:
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtIdle), false);
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOperator), false);
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtMaintenance), true);
                                break;
                            case ePageSerial.Initialen:
                            case ePageSerial.Mainten:
                            case ePageSerial.Setup:
                            case ePageSerial.Log:
                            case ePageSerial.Secs:
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtIdle), false);
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtMaintenance), false);
                                CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOperator), true);
                                break;
                        }
                    }
                    #endregion

                    #region E84 Light

                    bool bE84transfer = false;
                    foreach (I_E84 e84 in ListE84)
                        bE84transfer |= e84.isCs0On;

                    bool bReadyToUnload = false;
                    foreach (I_Loadport item in ListSTG)
                        bReadyToUnload |= item.StatusMachine == enumStateMachine.PS_ReadyToUnload;

                    if (bReadyToUnload)
                    {
                        CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtLoadUnLoadRequest), true);
                    }
                    else
                    {
                        CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtLoadUnLoadRequest), false);
                    }
                    #endregion

                    #region Processing Light
                    CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtProcessing), IsProcess());
                    #endregion

                    #region  SECS Online Light
                    if (_Gem != null)
                    {
                        CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOnlineLocal), _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINELOCAL);
                        CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOnlineRemote), _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE);
                        CtrlSignalTower(GParam.theInst.GetSignalTowerColor(enumSignalTowerColorSetting.AtOffline), _Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.OFFLINE);
                    }
                    #endregion

                    #region  Idle Light Yellow bit2 / Maint Manual Light Red bit1                
                    //switch (_pageCurr)
                    //{
                    //    case ePageSerial.Status:
                    //        if (_rc530_1.GetGDIO_OutputStatus(0, 0) == true)
                    //        {
                    //            _rc530_1.SdobW(0, 0, false);//紅燈    
                    //        }
                    //        if (IsProcess() == false)
                    //        {
                    //            if (_rc530_1.GetGDIO_OutputStatus(0, 1) == false)
                    //                _rc530_1.SdobW(0, 1, true);//黃燈  
                    //        }
                    //        else
                    //        {
                    //            if (_rc530_1.GetGDIO_OutputStatus(0, 1) == true)
                    //                _rc530_1.SdobW(0, 1, false);//黃燈  
                    //        }
                    //        break;
                    //    case ePageSerial.Teaching:
                    //    case ePageSerial.Initialen:
                    //    case ePageSerial.Mainten:
                    //    case ePageSerial.Setup:
                    //    case ePageSerial.Log:
                    //    case ePageSerial.Secs:
                    //        if (_rc530_1.GetGDIO_OutputStatus(0, 0) == false)
                    //            _rc530_1.SdobW(0, 0, true);//紅燈
                    //        break;
                    //}
                    #endregion
                }

                #region 自動登出           
                if (_userManager.IsLogin && _bLockMenu == false)
                {
                    long nElapseTime = GetIdleTick();

                    int nTimesUp = GParam.theInst.IdleLogOutTime;

                    if (nTimesUp > 0)
                    {
                        lblUserName.Text = "User :" + _userManager.UserID;

                        btnSignIn.Text = /*_userManager.UserID + "\r\n" +*/ ((nTimesUp - nElapseTime) / 1000).ToString();


                        if ((nTimesUp - nElapseTime) <= 0)
                        {
                            btnSignIn.PerformClick();
                        }
                    }
                }
                else
                {
                    lblUserName.Text = "User : none";
                }
                #endregion

                UpdateGemStatus();//左上的SECS

                CheckMaintenaceSW();//怕有人開Maint tool
            }
            catch (Exception ex)
            {
                SLogger.GetLogger("ExecuteLog").WriteLog(ex);
            }
        }
        private void DoPingIP()
        {
            if (GParam.theInst.IsSimulate == false && ListDIO[1].Disable == false)
            {
                if (m_tryPing >= 2) return;//只要報一次

                //PingReply reply;
                //reply = ping.Send(GParam.theInst.GetRC530IP(0), 500);

                if (ListDIO[1].Connected == false)
                {
                    m_tryPing++;
                    //if (m_tryPing == 2)//只要報一次
                    {
                        //tmrUI.Enabled = false;

                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.System_EMO_is_turned_on_and_the_system_will_shut_down);//EMO


                        //return;
                    }
                }
                else
                {
                    m_tryPing = 0;
                }
            }
        }
        private void UpdateGemStatus()
        {
            try
            {
                if (_Gem != null)   // Update Gem status
                {
                    if (GParam.theInst.IsSecsEnable)
                    {
                        uicimStatus1.iConn = _Gem.GetGEMCommStats;
                        uicimStatus1.icontrol = _Gem.GEMControlStatus;
                        uicimStatus1.iProcessStats = _Gem.CurrntGEMProcessStats;
                    }
                }
            }
            catch (Exception ex)
            {
                _errorLog.WriteLog("[ MDI ]:<<Exception>> UpdateGemStatus:{0}", ex);
            }
        }

        //  預計某些異常(EMO)發生要關閉軟體
        private void CloseSoftware(object sender, EventArgs e)
        {
            frmMessageBox frmMbox = new frmMessageBox("The system will shut down.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Question);
            frmMbox.TopMost = true;
            frmMbox.Focus();
            frmMbox.ShowDialog();

            if (_Gem != null && _Gem.GetSECSDriver.GetSecsStarted())
                _Gem.GetSECSDriver.SecsStop();

            this.Close();
            Environment.Exit(Environment.ExitCode);
        }
        //  判斷正在傳送
        private bool IsProcess()
        {
            bool bProcess = false;
            foreach (I_Loadport lp in ListSTG)
                bProcess |= lp.StatusMachine == enumStateMachine.PS_Process;
            return bProcess;
        }
        private void CheckMaintenaceSW()
        {
            try
            {
                string[] str = new string[] { "trb57", "stg54", "stg65", "RA320_Mnt", "mio_mnt", "rc530m", "WaferStocker" };
                foreach (string name in str)
                {
                    Process[] proc = Process.GetProcessesByName(name);
                    if (proc.Length > 0)
                    {
                        SLogger.GetLogger("ExecuteLog").WriteLog(string.Format("Detection software [{0}] is turned on!!!", name));
                        //new frmMessageBox(string.Format("Detection software [{0}] is turned on!!!", name), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                        foreach (Process pc in proc)
                        {
                            pc.Kill();
                        }
                        //new frmMessageBox("For the safety of the machine, please close and restart the main program!!!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                SLogger.GetLogger("ExecuteLog").WriteLog(ex);
            }
        }


        #region Robot delegate wafer data transfer
        private void OnRobotLoadComplete(object sender, RorzeUnit.Class.Robot.Event.LoadUnldEventArgs e)
        {
            I_Robot robot = sender as I_Robot;
            enumRobotArms eArm = e.Arm;
            int nStage = e.StgeIndx;//nStage0~399
            int nRobotSlot = e.Slot;

            enumPosition ePos = enumPosition.UnKnow;
            #region robot wafer data
            switch (eArm)
            {
                case enumRobotArms.UpperArm:
                    ePos = robot.PrepareUpperWafer.Position;
                    robot.UpperArmWafer = robot.PrepareUpperWafer;
                    robot.UpperArmWafer.ProcessStatus = enumProcessStatus.Processing;

                    if (robot.BodyNo == 1)
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB1Upper, robot.UpperArmWafer);//Load完成更新DB add
                    else if (robot.BodyNo == 2)
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB2Upper, robot.UpperArmWafer);//Load完成更新DB add
                    break;
                case enumRobotArms.LowerArm:
                    ePos = robot.PrepareLowerWafer.Position;
                    robot.LowerArmWafer = robot.PrepareLowerWafer;
                    robot.LowerArmWafer.ProcessStatus = enumProcessStatus.Processing;

                    if (robot.BodyNo == 1)
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB1Lower, robot.LowerArmWafer);//Load完成更新DB add
                    else if (robot.BodyNo == 2)
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB2Lower, robot.LowerArmWafer);//Load完成更新DB add
                    break;
                case enumRobotArms.BothArms:
                    ePos = robot.PrepareUpperWafer.Position;
                    robot.UpperArmWafer = robot.PrepareUpperWafer;
                    robot.LowerArmWafer = robot.PrepareLowerWafer;
                    robot.UpperArmWafer.ProcessStatus = enumProcessStatus.Processing;
                    robot.LowerArmWafer.ProcessStatus = enumProcessStatus.Processing;

                    if (robot.BodyNo == 1)
                    {
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB1Upper, robot.UpperArmWafer);//Load完成更新DB add
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB1Lower, robot.LowerArmWafer);//Load完成更新DB add
                    }
                    else if (robot.BodyNo == 2)
                    {
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB2Upper, robot.UpperArmWafer);//Load完成更新DB add
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB2Lower, robot.LowerArmWafer);//Load完成更新DB add
                    }
                    break;
            }
            #endregion

            #region robot load完成，將wafer data資料移除   
            if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.BUF1))
            {
                ListBUF[0].SetWafer(nRobotSlot - 1, null);
                _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF1_slot1 + nRobotSlot - 1);//Load完成更新DB remove
                if (eArm == enumRobotArms.BothArms)
                {
                    ListBUF[0].SetWafer(nRobotSlot - 2, null);
                    _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF1_slot1 + nRobotSlot - 2);//Load完成更新DB remove   
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.BUF2))
            {
                ListBUF[1].SetWafer(nRobotSlot - 1, null);
                _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF2_slot1 + nRobotSlot - 1);//Load完成更新DB remove
                if (eArm == enumRobotArms.BothArms)
                {
                    ListBUF[1].SetWafer(nRobotSlot - 2, null);
                    _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF2_slot1 + nRobotSlot - 2);//Load完成更新DB remove   
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.ALN1))
            {
                ListALN[0].Wafer = null;
                _DataBase.UpdateUnitStatus(SMySQL.enumUnit.ALN1);//Load完成更新DB remove
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.ALN2))
            {
                ListALN[1].Wafer = null;
                _DataBase.UpdateUnitStatus(SMySQL.enumUnit.ALN2);//Load完成更新DB remove
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.EQM1))
            {
                ListEQM[0].Wafer = null;
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.EQM2))
            {
                ListEQM[1].Wafer = null;
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.EQM3))
            {
                ListEQM[2].Wafer = null;
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.EQM4))
            {
                ListEQM[3].Wafer = null;
            }
            #endregion

            switch (ePos)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    ListSTG[ePos - enumPosition.Loader1].SetRobotExtend = false;
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    ListALN[ePos - enumPosition.AlignerA].SetRobotExtend = false;
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    ListBUF[ePos - enumPosition.BufferA].SetRobotExtend = false;
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    ListEQM[ePos - enumPosition.EQM1].SetRobotExtendIO(false);
                    ListEQM[ePos - enumPosition.EQM1].SetRobotGetSMEMA(false);
                    ListEQM[ePos - enumPosition.EQM1].tShutterDoorCloseSetW();//用一次緒關門，不卡robot動作
                    break;

            }
        }
        private void OnRobotUnldComplete(object sender, RorzeUnit.Class.Robot.Event.LoadUnldEventArgs e)
        {
            I_Robot robot = sender as I_Robot;
            enumRobotArms eArm = e.Arm;
            int nStage = e.StgeIndx;//nStage0~399
            int nRobotSlot = e.Slot;

            #region 處理wafer資料
            if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.BUF1))
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                        ListBUF[0].SetWafer(nRobotSlot - 1, robot.UpperArmWafer);
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF1_slot1 + nRobotSlot - 1, robot.UpperArmWafer);//Unld完成更新DB add
                        break;
                    case enumRobotArms.LowerArm:
                        ListBUF[0].SetWafer(nRobotSlot - 1, robot.LowerArmWafer);
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF1_slot1 + nRobotSlot - 1, robot.LowerArmWafer);//Unld完成更新DB add   
                        break;
                    case enumRobotArms.BothArms:
                        ListBUF[0].SetWafer(nRobotSlot - 1, robot.UpperArmWafer);
                        ListBUF[0].SetWafer(nRobotSlot - 2, robot.LowerArmWafer);
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF1_slot1 + nRobotSlot - 1, robot.UpperArmWafer);//Unld完成更新DB add
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF1_slot1 + nRobotSlot - 2, robot.LowerArmWafer);//Unld完成更新DB add
                        break;
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.BUF2))
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                        ListBUF[1].SetWafer(nRobotSlot - 1, robot.UpperArmWafer);
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF2_slot1 + nRobotSlot - 1, robot.UpperArmWafer);//Unld完成更新DB add
                        break;
                    case enumRobotArms.LowerArm:
                        ListBUF[1].SetWafer(nRobotSlot - 1, robot.LowerArmWafer);
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF2_slot1 + nRobotSlot - 1, robot.LowerArmWafer);//Unld完成更新DB add                   
                        break;
                    case enumRobotArms.BothArms:
                        ListBUF[1].SetWafer(nRobotSlot - 1, robot.UpperArmWafer);
                        ListBUF[1].SetWafer(nRobotSlot - 2, robot.LowerArmWafer);
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF2_slot1 + nRobotSlot - 1, robot.UpperArmWafer);//Unld完成更新DB add
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.BUF2_slot1 + nRobotSlot - 2, robot.LowerArmWafer);//Unld完成更新DB add
                        break;
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.ALN1))
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                        ListALN[0].Wafer = robot.UpperArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.ALN1, robot.UpperArmWafer);//Unld完成更新DB add
                        break;
                    case enumRobotArms.LowerArm:
                        ListALN[0].Wafer = robot.LowerArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.ALN1, robot.LowerArmWafer);//Unld完成更新DB add
                        break;
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.ALN2))
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                        ListALN[1].Wafer = robot.UpperArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.ALN2, robot.UpperArmWafer);//Unld完成更新DB add         
                        break;
                    case enumRobotArms.LowerArm:
                        ListALN[1].Wafer = robot.LowerArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.ALN2, robot.LowerArmWafer);//Unld完成更新DB add       
                        break;
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.EQM1))
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                        ListEQM[0].Wafer = robot.UpperArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.EQM1, robot.UpperArmWafer);//Unld完成更新DB add         
                        break;
                    case enumRobotArms.LowerArm:
                        ListEQM[0].Wafer = robot.LowerArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.EQM1, robot.LowerArmWafer);//Unld完成更新DB add       
                        break;
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.EQM2))
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                        ListEQM[1].Wafer = robot.UpperArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.EQM2, robot.UpperArmWafer);//Unld完成更新DB add         
                        break;
                    case enumRobotArms.LowerArm:
                        ListEQM[1].Wafer = robot.LowerArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.EQM2, robot.LowerArmWafer);//Unld完成更新DB add       
                        break;
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.EQM3))
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                        ListEQM[2].Wafer = robot.UpperArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.EQM3, robot.UpperArmWafer);//Unld完成更新DB add         
                        break;
                    case enumRobotArms.LowerArm:
                        ListEQM[2].Wafer = robot.LowerArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.EQM3, robot.LowerArmWafer);//Unld完成更新DB add       
                        break;
                }
            }
            else if (nStage == GParam.theInst.GetDicPosRobot(robot.BodyNo, enumRbtAddress.EQM4))
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:
                        ListEQM[3].Wafer = robot.UpperArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.EQM4, robot.UpperArmWafer);//Unld完成更新DB add         
                        break;
                    case enumRobotArms.LowerArm:
                        ListEQM[3].Wafer = robot.LowerArmWafer;
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.EQM4, robot.LowerArmWafer);//Unld完成更新DB add       
                        break;
                }
            }
            else//loadport
            {
                switch (eArm)
                {
                    case enumRobotArms.UpperArm:

                        //SWafer waferData = robot.UpperArmWafer;
                        ////source
                        //enumFromLoader eFromLoadport = waferData.Owner;
                        //int nFromSlot = waferData.Slot;
                        ////target
                        //enumFromLoader eToLoadport = waferData.ToLoadport;
                        //int nToSlot = waferData.ToSlot;

                        //if (eFromLoadport == eToLoadport && nFromSlot != nToSlot)
                        //{
                        //    int nIdx = eFromLoadport - SWafer.enumFromLoader.LoadportA;
                        //    if (ListSTG[nIdx].Waferlist[nToSlot - 1] != null)
                        //    {
                        //        //這一片是在跟另一片交換位置                          
                        //        ListSTG[nIdx].TakeWaferSlotExchange(nFromSlot, nToSlot);
                        //    }

                        //}



                        LPtoLP_WafterDataTransfer(nStage, nRobotSlot, robot.UpperArmWafer);
                        break;
                    case enumRobotArms.LowerArm:
                        LPtoLP_WafterDataTransfer(nStage, nRobotSlot, robot.LowerArmWafer);
                        break;
                    case enumRobotArms.BothArms:
                        LPtoLP_WafterDataTransfer(nStage, nRobotSlot, robot.UpperArmWafer);
                        LPtoLP_WafterDataTransfer(nStage, nRobotSlot, robot.LowerArmWafer);
                        break;
                }
            }
            #endregion

            enumPosition ePos = enumPosition.UnKnow;
            #region robot wafer data remove
            switch (eArm)
            {
                case enumRobotArms.UpperArm:
                    ePos = robot.UpperArmWafer.Position;
                    robot.UpperArmWafer = null;
                    if (robot.BodyNo == 1)
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB1Upper);//Unld完成更新DB remove
                    else if (robot.BodyNo == 2)
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB2Upper);//Unld完成更新DB remove                
                    break;
                case enumRobotArms.LowerArm:
                    ePos = robot.LowerArmWafer.Position;
                    robot.LowerArmWafer = null;
                    if (robot.BodyNo == 1)
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB1Lower);//Unld完成更新DB remove
                    else if (robot.BodyNo == 2)
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB2Lower);//Unld完成更新DB remove           
                    break;
                case enumRobotArms.BothArms:
                    ePos = robot.UpperArmWafer.Position;
                    robot.UpperArmWafer = robot.LowerArmWafer = null;
                    if (robot.BodyNo == 1)
                    {
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB1Upper);//Unld完成更新DB remove
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB1Lower);//Unld完成更新DB remove
                    }
                    else if (robot.BodyNo == 2)
                    {
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB2Upper);//Unld完成更新DB remove      
                        _DataBase.UpdateUnitStatus(SMySQL.enumUnit.TRB2Lower);//Unld完成更新DB remove
                    }
                    break;
            }
            #endregion

            switch (ePos)
            {
                case enumPosition.Loader1:
                case enumPosition.Loader2:
                case enumPosition.Loader3:
                case enumPosition.Loader4:
                case enumPosition.Loader5:
                case enumPosition.Loader6:
                case enumPosition.Loader7:
                case enumPosition.Loader8:
                    ListSTG[ePos - enumPosition.Loader1].SetRobotExtend = false;
                    break;
                case enumPosition.AlignerA:
                case enumPosition.AlignerB:
                    ListALN[ePos - enumPosition.AlignerA].SetRobotExtend = false;
                    break;
                case enumPosition.BufferA:
                case enumPosition.BufferB:
                    ListBUF[ePos - enumPosition.BufferA].SetRobotExtend = false;
                    break;
                case enumPosition.EQM1:
                case enumPosition.EQM2:
                case enumPosition.EQM3:
                case enumPosition.EQM4:
                    ListEQM[ePos - enumPosition.EQM1].SetRobotExtendIO(false);
                    ListEQM[ePos - enumPosition.EQM1].SetRobotPutSMEMA(false);
                    ListEQM[ePos - enumPosition.EQM1].tShutterDoorCloseSetW(); //用一次緒關門，不卡robot動作
                    break;

            }
        }
        private void OnLoadExchangeComplete(object sender, RorzeUnit.Class.Robot.Event.LoadUnldEventArgs e)
        {
            enumRobotArms eLoadArm = e.Arm, eUnldArm;
            switch (e.Arm)
            {
                case enumRobotArms.UpperArm: eUnldArm = enumRobotArms.LowerArm; break;
                case enumRobotArms.LowerArm: eUnldArm = enumRobotArms.UpperArm; break;
                default: throw new Exception("Exchange Arm Fail!");
            }

            OnRobotLoadComplete(sender, new RorzeUnit.Class.Robot.Event.LoadUnldEventArgs(eLoadArm, e.StgeIndx, 1));
            OnRobotUnldComplete(sender, new RorzeUnit.Class.Robot.Event.LoadUnldEventArgs(eUnldArm, e.StgeIndx, 1));
        }
        /// <summary>
        /// wafer transfer LP to LP.
        /// </summary>
        /// <param name="nRobotStgeIndx">0~399</param>
        /// <param name="nRobotSlot">1~25</param>
        /// <param name="waferData">wafer</param>
        /// <remarks>nRobotStageIndx:0~399, slot:1~25</remarks>
        private void LPtoLP_WafterDataTransfer(int nRobotStgeIndx, int nRobotSlot, SWafer waferData)//nStage0~399
        {
            //source
            enumFromLoader eFromLoadport = waferData.Owner;
            int nFromSlot = waferData.Slot;
            //target
            enumFromLoader eToLoadport = waferData.ToLoadport;
            int nToSlot = waferData.ToSlot;

            //當下JOB傳到目標的紀錄DB(UDO使用)
            _DataBase.InsertWaferTransfer(waferData);
            _DataBase.InsertWaferlog(waferData);
            switch (eFromLoadport)//原本位置的資料->移除
            {
                case SWafer.enumFromLoader.LoadportA:
                case SWafer.enumFromLoader.LoadportB:
                case SWafer.enumFromLoader.LoadportC:
                case SWafer.enumFromLoader.LoadportD:
                case SWafer.enumFromLoader.LoadportE:
                case SWafer.enumFromLoader.LoadportF:
                case SWafer.enumFromLoader.LoadportG:
                case SWafer.enumFromLoader.LoadportH:
                    int nIdx = eFromLoadport - SWafer.enumFromLoader.LoadportA;

                    if (eFromLoadport == eToLoadport && nFromSlot != nToSlot)
                    {
                        //這種情況是兩片互換，目標已經有片，且還沒互換過
                        if (ListSTG[nIdx].Waferlist[nToSlot - 1] != null)
                        {
                            //這一片是在跟另一片交換位置   
                            if (ListSTG[nIdx].Waferlist[nToSlot - 1] != waferData)
                                ListSTG[nIdx].TakeWaferSlotExchange(nFromSlot, nToSlot);
                            break;
                        }
                    }

                    ListSTG[nIdx].TakeWaferOutFoup(nFromSlot - 1);

                    if (GParam.theInst.IsSimulate)
                        GParam.theInst.SetSimulateGmap(nIdx, ListSTG[nIdx].MappingData);
                    break;
            }

            System.Threading.SpinWait.SpinUntil(() => false, 100);

            switch (eToLoadport)//目標位置的資料->加入
            {
                case SWafer.enumFromLoader.LoadportA:
                case SWafer.enumFromLoader.LoadportB:
                case SWafer.enumFromLoader.LoadportC:
                case SWafer.enumFromLoader.LoadportD:
                case SWafer.enumFromLoader.LoadportE:
                case SWafer.enumFromLoader.LoadportF:
                case SWafer.enumFromLoader.LoadportG:
                case SWafer.enumFromLoader.LoadportH:
                    int nIdx = eToLoadport - SWafer.enumFromLoader.LoadportA;
                    ListSTG[nIdx].AssignWafer(waferData.LotID, nToSlot, waferData);
                    ListSTG[nIdx].TakeWaferInFoup(nToSlot - 1, waferData);
                    if (GParam.theInst.IsSimulate)
                        GParam.theInst.SetSimulateGmap(nIdx, ListSTG[nIdx].MappingData);
                    break;
            }



        }
        #endregion


        private void FFU_InitialSpeed()
        {
            I_RC5X0_IO rc550 = ListDIO[0];
            if (rc550.Disable == false && GParam.theInst.RC550ctrlFFU)
            {
                if (GParam.theInst.GetFanDefaultSpeed == 0)
                    rc550.StopW(0);
                else
                    rc550.MoveW(GParam.theInst.GetFanDefaultSpeed);
            }
        }
        private bool IsMaintMode_EFEM()
        {
            IO_Signal_Information io_id = GParam.theInst.GetIO_Information(enumIO_Signal.IsMaint);
            bool bOn = ListDIO[io_id._DioBodyNo].GetGDIO_InputStatus(0, io_id._Bit);
            if (io_id._NormalOff == false)
            {
                bOn = !bOn;
            }
            return bOn || GParam.theInst.IsSimulate;//電控設計名稱IsMaint
        }

        private bool IsArea1Trigger()
        {
            IO_Signal_Information io_id = GParam.theInst.GetIO_Information(enumIO_Signal.LightCurtain1);
            bool bOn = ListDIO[io_id._DioBodyNo].GetGDIO_InputStatus(0, io_id._Bit);
            if (io_id._NormalOff == false)
            {
                bOn = !bOn;
            }
            return bOn;
        }
        private bool IsSignalTowerOn(enumSignalTowerColor eSignalTowerColor)
        {
            bool bOn = false;
            switch (eSignalTowerColor)
            {
                case enumSignalTowerColor.Red:
                    bOn = ListDIO[1].GetGDIO_OutputStatus(0, 0);
                    break;
                case enumSignalTowerColor.RedBlinking:
                    bOn = ListDIO[1].GetGDIO_OutputStatus(0, 4);
                    break;
                case enumSignalTowerColor.Yellow:
                    bOn = ListDIO[1].GetGDIO_OutputStatus(0, 1);
                    break;
                case enumSignalTowerColor.YellowBlinking:
                    bOn = ListDIO[1].GetGDIO_OutputStatus(0, 5);
                    break;
                case enumSignalTowerColor.Green:
                    bOn = ListDIO[1].GetGDIO_OutputStatus(0, 2);
                    break;
                case enumSignalTowerColor.GreenBlinking:
                    bOn = ListDIO[1].GetGDIO_OutputStatus(0, 6);
                    break;
                case enumSignalTowerColor.Blue:
                    bOn = ListDIO[1].GetGDIO_OutputStatus(0, 3);
                    break;
                case enumSignalTowerColor.BlueBlinking:
                    bOn = ListDIO[1].GetGDIO_OutputStatus(0, 7);
                    break;
                default:
                    break;
            }
            return bOn;
        }
        private void CtrlSignalTower(enumSignalTowerColor eSignalTowerColor, bool on)
        {
            try
            {
                if (GParam.theInst.IsSimulate == false && ListDIO[1].Connected == false) return;
                if (ListDIO[1].Disable) return;
                bool bNowOutputOn = IsSignalTowerOn(eSignalTowerColor);
                if (bNowOutputOn == on) return;
                switch (eSignalTowerColor)
                {
                    case enumSignalTowerColor.Red:
                        ListDIO[1].SdobW(0, 0, on);
                        break;
                    case enumSignalTowerColor.RedBlinking:
                        ListDIO[1].SdobW(0, 4, on);
                        break;
                    case enumSignalTowerColor.Yellow:
                        ListDIO[1].SdobW(0, 1, on);
                        break;
                    case enumSignalTowerColor.YellowBlinking:
                        ListDIO[1].SdobW(0, 5, on);
                        break;
                    case enumSignalTowerColor.Green:
                        ListDIO[1].SdobW(0, 2, on);
                        break;
                    case enumSignalTowerColor.GreenBlinking:
                        ListDIO[1].SdobW(0, 6, on);
                        break;
                    case enumSignalTowerColor.Blue:
                        ListDIO[1].SdobW(0, 3, on);
                        break;
                    case enumSignalTowerColor.BlueBlinking:
                        ListDIO[1].SdobW(0, 7, on);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog("<Exception> : " + ex);
            }
        }




        private void ExecuteRecover()
        {
            try
            {
                //DB確認有帳要寫入
                System.Data.DataTable dt = _DataBase.SelectUnitStatus();
                if (dt != null && dt.Rows.Count != 0)
                {
                    string[] strTRB1upper = _DataBase.GetUnitStatus(SMySQL.enumUnit.TRB1Upper).Split('_');
                    if (strTRB1upper.Length > 1 && ListTRB[0].UpperArmWafer != null)
                    {
                        SWafer w = ListTRB[0].UpperArmWafer;
                        w.FoupID = strTRB1upper[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.TRB1Upper).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.TRB1Upper).Split('_')[1];

                    }
                    string[] strTRB1Lower = _DataBase.GetUnitStatus(SMySQL.enumUnit.TRB1Lower).Split('_');
                    if (strTRB1Lower.Length > 1 && ListTRB[0].LowerArmWafer != null)
                    {
                        SWafer w = ListTRB[0].LowerArmWafer;
                        w.FoupID = strTRB1Lower[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.TRB1Lower).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.TRB1Lower).Split('_')[1];

                    }
                    string[] strTRB2Upper = _DataBase.GetUnitStatus(SMySQL.enumUnit.TRB2Upper).Split('_');
                    if (strTRB2Upper.Length > 1 && ListTRB[1].UpperArmWafer != null)
                    {
                        SWafer w = ListTRB[1].UpperArmWafer;
                        w.FoupID = strTRB2Upper[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.TRB2Upper).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.TRB2Upper).Split('_')[1];

                    }
                    string[] strTRB2Lower = _DataBase.GetUnitStatus(SMySQL.enumUnit.TRB2Lower).Split('_');
                    if (strTRB2Lower.Length > 1 && ListTRB[1].LowerArmWafer != null)
                    {
                        SWafer w = ListTRB[1].LowerArmWafer;
                        w.FoupID = strTRB2Lower[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.TRB2Lower).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.TRB2Lower).Split('_')[1];

                    }
                    string[] strALN1 = _DataBase.GetUnitStatus(SMySQL.enumUnit.ALN1).Split('_');
                    if (strALN1.Length > 1 && ListALN[0].Wafer != null)
                    {
                        SWafer w = ListALN[0].Wafer;
                        w.FoupID = strALN1[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.ALN1).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.ALN1).Split('_')[1];

                    }
                    string[] strALN2 = _DataBase.GetUnitStatus(SMySQL.enumUnit.ALN2).Split('_');
                    if (strALN2.Length > 1 && ListALN[1].Wafer != null)
                    {
                        SWafer w = ListALN[1].Wafer;
                        w.FoupID = strALN2[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.ALN2).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.ALN2).Split('_')[1];

                    }
                    string[] strBUF1_1 = _DataBase.GetUnitStatus(SMySQL.enumUnit.BUF1_slot1).Split('_');
                    if (strBUF1_1.Length > 1 && ListBUF[0].GetWafer(0) != null)
                    {
                        SWafer w = ListBUF[0].GetWafer(0);
                        w.FoupID = strBUF1_1[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.BUF1_slot1).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.BUF1_slot1).Split('_')[1];

                    }
                    string[] strBUF1_2 = _DataBase.GetUnitStatus(SMySQL.enumUnit.BUF1_slot2).Split('_');
                    if (strBUF1_2.Length > 1 && ListBUF[0].GetWafer(1) != null)
                    {
                        SWafer w = ListBUF[0].GetWafer(1);
                        w.FoupID = strBUF1_2[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.BUF1_slot2).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.BUF1_slot2).Split('_')[1];

                    }
                    string[] strBUF2_1 = _DataBase.GetUnitStatus(SMySQL.enumUnit.BUF2_slot1).Split('_');
                    if (strBUF2_1.Length > 1 && ListBUF[1].GetWafer(0) != null)
                    {
                        SWafer w = ListBUF[1].GetWafer(0);
                        w.FoupID = strBUF2_1[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.BUF2_slot1).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.BUF2_slot1).Split('_')[1];

                    }
                    string[] strBUF2_2 = _DataBase.GetUnitStatus(SMySQL.enumUnit.BUF2_slot2).Split('_');
                    if (strBUF2_2.Length > 1 && ListBUF[1].GetWafer(1) != null)
                    {
                        SWafer w = ListBUF[1].GetWafer(1);
                        w.FoupID = strBUF2_2[2];
                        w.WaferID_F = _DataBase.GetUnitWaferID(SMySQL.enumUnit.BUF2_slot2).Split('_')[0];
                        w.WaferID_B = _DataBase.GetUnitWaferID(SMySQL.enumUnit.BUF2_slot2).Split('_')[1];

                    }
                }
            }
            catch (Exception ex) { WriteLog("<Exception> : " + ex); }

            bool bNeed = false;

            foreach (I_Robot item in ListTRB)//2
            {
                if (item == null || item.Disable) continue;
                if (item.UpperArmWafer != null || item.LowerArmWafer != null)
                    bNeed = true;
            }

            foreach (I_Aligner item in ListALN)
            {
                if (item == null || item.Disable) continue;
                if (item.Wafer != null || item.WaferExists())
                    bNeed = true;
            }

            foreach (I_Buffer item in ListBUF)
            {
                if (item == null || item.Disable) continue;
                if (item.AnySlotHasWafer())
                    bNeed = true;
            }

            foreach (SSEquipment item in ListEQM)
            {
                if (item == null || item.Disable) continue;
                if (item.Wafer != null || item.IsWaferExist)
                    bNeed = true;
            }

            if (bNeed)
            {
                WriteLog("Execute WaferRecove is Start!!");
                frmWaferRecover waferRecover = new frmWaferRecover(ListTRB, ListSTG, ListALN, ListE84, ListBUF, ListOCR, _DataBase, ListEQM);
                if (waferRecover.ShowDialog() != DialogResult.OK)
                {
                    throw new Exception("Wafer recover failed");
                }
                _JobControl.PJCJReset();//要清除上一次跑一半中斷的
            }

        }

        private void tableLayoutPanel1_MouseClick(object sender, MouseEventArgs e)
        {
            this.Location = new Point(0, 0);
        }

        private void frmMDI_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;//window清單案右鍵關閉，不要關掉
        }



        /*private void ActiveSafetyIOStatus()
        {
            try
            {
                Process[] proc = null;
                string strPath = string.Empty, strFile = string.Empty;

                proc = Process.GetProcessesByName("SafetyIOStatus");//TeachTool
                strPath = System.Environment.CurrentDirectory + "\\SafetyIOStatus";
                strFile = "TeachTool.exe";

                if (Directory.Exists(strPath) == false || GParam.theInst.IsSimulate)
                {
                    return;
                }

                if (proc.Length != 0)//判斷程式已經開啟
                {
                    foreach (Process pc in proc) { pc.Kill(); }
                }

                WriteLog("<Active RobotTool> : " + Path.Combine(strPath, strFile));

                var startInfo = new ProcessStartInfo();
                startInfo.WorkingDirectory = strPath;
                startInfo.FileName = strFile;
                Process.Start(startInfo);
                SpinWait.SpinUntil(() => false, 1000);

            }
            catch (Exception ex)
            {
                WriteLog("<Exception> : " + ex);
            }
        }*/

    }
}
