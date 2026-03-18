using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RorzeUnit.Interface;
using RorzeUnit.Class;
using RorzeComm.Log;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Stock;
using RorzeComm;
using System.Runtime.InteropServices;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Class.EQ;

namespace RorzeApi
{
    public partial class frmTeachStock : Form
    {
        private float frmX;                 //  Zoom 放入窗體的寬度
        private float frmY;                 //  Zoom 放入窗體的高度
        private bool isLoaded = false;      //  Zoom 是否已設定各控制的尺寸資料到Tag屬性

        private Class.SPermission _userManager;   //  管理LOGIN使用者權限
        private string _strUserName;//登入者名稱
        private bool m_bIsRunMode = false;
        private SProcessDB _accessDBlog;
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        private I_Robot m_robot;
        private List<I_Robot> ListTRB;
        private List<I_Loadport> ListSTG;
        private List<I_Aligner> ListALN;
        private I_Stock m_stock;
        private List<I_Stock> ListSTK;
        private List<I_Buffer> ListBUF;
        SSEquipment _equipment;
        private bool m_bSimulate;
        private int m_nTower1to16;
        private int m_nArea1to8;
        private bool m_bTkeyOn;
        private enum eTeachStep { Prepare = 0, TkeyOn, TkeyOff, End };//  Page2 所有步驟    
        List<Label> lstLabStep = new List<Label>();             //  Page2 上面那排顯示做到第幾步驟的Lable
        private bool m_bBlink = false;                          //  Page2 上面那排顯示做到第幾步驟的Lable 畫面閃燈
        private eTeachStep m_eStep;                             //  Page2 做到哪一步驟

        public dlgb_v dlgIsTkeyOn { get; set; }

        public frmTeachStock(
            List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN, List<I_Stock> listSTK, List<I_Buffer> listBUF,
            SProcessDB db, Class.SPermission userManager, bool bIsRunMode, bool bSimulate, SSEquipment equipment)
        {
            InitializeComponent();
            this.Size = new Size(970, 718);
            ListTRB = listTRB;
            ListSTG = listSTG;
            ListALN = listALN;
            ListSTK = listSTK;
            ListBUF = listBUF;
            _equipment = equipment;
            _accessDBlog = db;
            _userManager = userManager;
            m_bIsRunMode = bIsRunMode;
            m_bSimulate = bSimulate;

            //  消失頁籤
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);

            #region Select Robot Button
            tlpSelectRobot.RowStyles.Clear();
            tlpSelectRobot.ColumnStyles.Clear();
            tlpSelectRobot.RowCount = 1;
            tlpSelectRobot.ColumnCount = 0;
            tlpSelectRobot.Dock = DockStyle.Fill;
            foreach (I_Robot item in ListTRB)
            {
                if (GParam.theInst.GetRobot_AllowTower(item.BodyNo - 1).Contains('1') == false) continue;
                tlpSelectTower.ColumnCount += 1;
                tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                Button btn = new Button();
                btn.Font = new Font("Calibri", 18, FontStyle.Bold);
                btn.Text = "Robot " + (char)(64 + item.BodyNo);
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Click += btnSelectRobot_Click;
                tlpSelectRobot.Controls.Add(btn);
            }
            tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            #endregion
            #region Select Tower Button
            tlpSelectTower.RowStyles.Clear();
            tlpSelectTower.ColumnStyles.Clear();
            tlpSelectTower.RowCount = 1;
            tlpSelectTower.ColumnCount = 0;
            tlpSelectTower.Dock = DockStyle.Fill;
            for (int i = 0; i < ListSTK.Count; i++)//四座塔
            {
                I_Stock stock = ListSTK[i];
                if (stock.Disable) continue;
                for (int j = 0; j < stock.TowerCount; j++)//塔四面
                {
                    if (stock.TowerEnable(j) == false) continue;
                    tlpSelectTower.ColumnCount += 1;
                    tlpSelectTower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                    Button btn = new Button();
                    btn.Font = new Font("Calibri", 18, FontStyle.Bold);
                    btn.Text = (i * stock.TowerCount + j + 1).ToString("D2");
                    btn.Dock = DockStyle.Fill;
                    btn.TextAlign = ContentAlignment.MiddleCenter;
                    btn.Click += btnSelectTower_Click;
                    tlpSelectTower.Controls.Add(btn);
                }
            }
            tlpSelectTower.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            #endregion
            #region Select Area Button          
            {
                I_Stock stock = ListSTK[0];//隨便挑一個塔            
                tlpSelectArea.RowStyles.Clear();
                tlpSelectArea.ColumnStyles.Clear();
                tlpSelectArea.RowCount = 1;
                tlpSelectArea.ColumnCount = 0;
                tlpSelectArea.Dock = DockStyle.Fill;
                for (int i = 0; i < stock.TheTowerSlotNumber / 25; i++)
                {
                    tlpSelectArea.ColumnCount += 1;
                    tlpSelectArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                    Button btn = new Button();
                    btn.Font = new Font("Calibri", 18, FontStyle.Bold);
                    btn.Text = (i + 1).ToString("D2");
                    btn.Dock = DockStyle.Fill;
                    btn.TextAlign = ContentAlignment.MiddleCenter;
                    btn.Click += btnSelectArea_Click;
                    tlpSelectArea.Controls.Add(btn);
                }
                tlpSelectArea.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            }
            #endregion

            //  顯示步驟的那條
            int nIdx = 0;
            foreach (string str in Enum.GetNames(typeof(eTeachStep)))
            {
                Label lb = new Label();
                lb.Font = new Font("Calibri", 9, FontStyle.Regular);
                lb.Text = str;
                lb.Dock = DockStyle.Fill;
                lb.TextAlign = ContentAlignment.MiddleCenter;
                tlpnlStep.Controls.Add(lb, nIdx, 0);
                lstLabStep.Add(lb);
                nIdx++;
            }

            if (GParam.theInst.FreeStyle)
            {
                btnNext.Image = RorzeApi.Properties.Resources._32_next_;
                btnCancel.Image = RorzeApi.Properties.Resources._32_cancel_;

                btnTeach.Image = RorzeApi.Properties.Resources._48_work;
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

        private void btnSelectRobot_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Text, _strUserName, "Select Robot", btn.Name);

            #region 按鈕顏色
            foreach (Button item in tlpSelectRobot.Controls)
            {
                if (item == btn)
                {
                    if (GParam.theInst.FreeStyle)
                    {
                        item.BackColor = GParam.theInst.ColorTitle;
                    }
                    else
                    {
                        item.BackColor = Color.LightBlue;
                    }

                    switch (btn.Text)
                    {
                        case "Robot A": m_robot = ListTRB[0]; break;
                        case "Robot B": m_robot = ListTRB[1]; break;
                        default: m_robot = null; break;
                    }
                }
                else
                {
                    item.BackColor = System.Drawing.SystemColors.ControlLight;
                }
            }
            #endregion  
        }
        private void btnSelectTower_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Text, _strUserName, "Select Tower", btn.Name);
            m_nTower1to16 = -1;
            #region 按鈕顏色
            foreach (Button item in tlpSelectTower.Controls)
            {
                if (item == btn)
                {
                    m_nTower1to16 = int.Parse(btn.Text);
                    m_stock = ListSTK[(m_nTower1to16 - 1) / 4];
                    if (GParam.theInst.FreeStyle)
                    {
                        item.BackColor = GParam.theInst.ColorTitle;
                    }
                    else
                    {
                        item.BackColor = Color.LightBlue;
                    }
                }
                else
                {
                    item.BackColor = System.Drawing.SystemColors.ControlLight;
                }
            }
            #endregion  
        }
        private void btnSelectArea_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Text, _strUserName, "Select Area", btn.Name);
            m_nArea1to8 = -1;
            #region 按鈕顏色
            foreach (Button item in tlpSelectArea.Controls)
            {
                if (item == btn)
                {
                    m_nArea1to8 = int.Parse(btn.Text);
                    if (GParam.theInst.FreeStyle)
                    {
                        item.BackColor = GParam.theInst.ColorTitle;
                    }
                    else
                    {
                        item.BackColor = Color.LightBlue;
                    }
                }
                else
                {
                    item.BackColor = System.Drawing.SystemColors.ControlLight;
                }
            }
            #endregion  
        }
        private void tmrUI_Tick(object sender, EventArgs e)
        {
            tmrUI.Enabled = false;

            //  閃燈
            foreach (object value in Enum.GetValues(typeof(eTeachStep)))
            {
                if ((int)value < (int)m_eStep)
                {
                    lstLabStep[(int)value].BackColor = Color.Orange;
                }
                else if ((int)value == (int)m_eStep)
                {
                    m_bBlink = (false == m_bBlink);
                    lstLabStep[(int)value].BackColor = m_bBlink ? Color.Orange : SystemColors.Control;
                }
                else
                {
                    lstLabStep[(int)value].BackColor = SystemColors.Control;
                }
            }

            if (dlgIsTkeyOn != null)
            {
                if (m_bTkeyOn != dlgIsTkeyOn())
                {
                    m_bTkeyOn = dlgIsTkeyOn();
                    pcbTkey.Image = m_bTkeyOn ? RorzeApi.Properties.Resources.LightGreen : RorzeApi.Properties.Resources.LightOff;
                }
            }



            tmrUI.Enabled = true;
        }
        private void frmTeachRobot_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                tmrUI.Enabled = this.Visible;
                _strUserName = _userManager.UserID;
                if (this.Visible)
                {
                    if (tabControl1.SelectedTab == tabPage1)
                    {
                        ((Button)tlpSelectRobot.Controls[0]).PerformClick();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }

        //  =====================================================================================================================
        private void btnTeach_Click(object sender, EventArgs e)
        {
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobot", _strUserName, "Robot", "Start teaching.");
            TeachRobotStart();
        }
        private void TeachRobotStart()
        {
            if (m_robot == null)
            {
                new frmMessageBox("Please select robot.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_bIsRunMode)
            {
                new frmMessageBox("Is RunMode!!!Can't teaching.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_nArea1to8 < 1)
            {
                new frmMessageBox("Please select Area!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }
            if (m_nTower1to16 < 1)
            {
                new frmMessageBox("Please select Tower!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            SWafer.enumPosition ePosition = SWafer.enumPosition.Tower01 + (m_nTower1to16 - 1);
            //stage index 0~399
            int nStge1to400 = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, ePosition) + m_nArea1to8;//1~400  

            string str = string.Format("{0} {1}:{2} {3}:{4} {5}:{6} ?",
                GParam.theInst.GetLanguage("Teaching"),
                GParam.theInst.GetLanguage("Tower"), ePosition,
                GParam.theInst.GetLanguage("Area"), m_nArea1to8,
                GParam.theInst.GetLanguage("No."), nStge1to400);

            if (new frmMessageBox(string.Format(str), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }

            m_eStep = eTeachStep.Prepare;
            bool bSuc = DoOrgn();
            if (bSuc)
            {
                rtbInstruct.Clear();
                rtbInstruct.AppendText("Execute the teaching process.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Execute the teaching process.\r"));

                rtbInstruct.AppendText("Button [Next] to start the process.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Button [Next] to start the process.\r"));

                rtbInstruct.AppendText("If cancel teaching , press 'Cancel' button\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If cancel teaching , press 'Cancel' button\r"));
                delegateMDILock?.Invoke(true);
                tabControl1.SelectedTab = tabPage2;
                EnableProcedureButton(true);
                Cursor.Current = Cursors.WaitCursor;
            }
        }

        #region 教導流程
        private void EnableProcedureButton(bool bAct)
        {
            btnCancel.Enabled = bAct;
            btnNext.Enabled = bAct;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            ExecutionStep(false);
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            ExecutionStep(true);
        }
        private void ExecutionStep(bool bCancel)
        {
            EnableProcedureButton(false);
            switch (m_eStep)
            {
                case eTeachStep.Prepare:
                    if (bCancel)
                        SkipPrepareTask();
                    else
                        DoPrepareTask();
                    break;
                case eTeachStep.TkeyOn:
                    if (bCancel)
                        SkipTkeyOnTask();
                    else
                        DoTkeyOnTask();
                    break;
                case eTeachStep.TkeyOff:
                    DoTkeyOffTask();
                    break;
                case eTeachStep.End:
                    ProcessEnd();
                    break;
            }
        }
        //  step1
        private void DoPrepareTask()
        {
            rtbInstruct.Clear();
            rtbInstruct.AppendText("Run Stocker to open the door.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Run Stocker to open the door.\r"));
            Cursor.Current = Cursors.WaitCursor;

            SWafer.enumPosition ePosition = SWafer.enumPosition.Tower01 + (m_nTower1to16 - 1);
            //stage index 0~399
            int nStgeIndx = GParam.theInst.GetDicPosRobot(m_robot.BodyNo, ePosition) + m_nArea1to8;//10,11,12,13,14,15,16,17  
            m_stock.OnHOMEComplete -= DoPrepareComplete;
            m_stock.OnHOMEComplete += DoPrepareComplete;
            m_stock.HOME(nStgeIndx + 1);
        }
        private void DoPrepareComplete(object sender, bool bSuc)
        {
            I_Stock stock = (I_Stock)sender;
            stock.OnHOMEComplete -= DoPrepareComplete;
            rtbInstruct.Clear();
            if (bSuc)
            {
                rtbInstruct.AppendText("Please turn on the T-key.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Please turn on the T-key.\r"));

                rtbInstruct.AppendText("Press [Next] to continue.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Press [Next] to continue.\r"));

                rtbInstruct.AppendText("If you need cancel teaching mode,press [Cancel] Button.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If you need cancel teaching mode,press [Cancel] Button.\r"));

                Cursor.Current = Cursors.Default;
                m_eStep = eTeachStep.TkeyOn;
            }
            else
            {
                rtbInstruct.AppendText("Failed to open the door.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Failed to open the door.\r"));

                rtbInstruct.AppendText("Press [Next] to retry.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Press [Next] to retry.\r"));

                rtbInstruct.AppendText("If you need cancel teaching mode,press [Cancel] Button.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If you need cancel teaching mode,press [Cancel] Button.\r"));
            }
            EnableProcedureButton(true);
        }
        private void SkipPrepareTask()
        {
            if (new frmMessageBox(string.Format("Abort teaching ?"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }
            ProcessEnd();
        }
        //  step2
        private void DoTkeyOnTask()
        {
            if (m_bSimulate == false)
                if (dlgIsTkeyOn == null || dlgIsTkeyOn() == false)
                {
                    new frmMessageBox("Please turn on the T-key.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    EnableProcedureButton(true);
                    return;
                }

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Perform robot mode switching.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Perform robot mode switching.\r"));
            Cursor.Current = Cursors.WaitCursor;

            m_robot.OnMODEComplete += DoTkeyOnComplete;
            m_robot.MODE(3);
        }
        private void DoTkeyOnComplete(object sender, bool bSuc)
        {
            I_Robot robot = (I_Robot)sender;
            robot.OnMODEComplete -= DoTkeyOnComplete;
            rtbInstruct.Clear();
            if (bSuc)
            {
                rtbInstruct.AppendText("Start operating Teaching Pendant.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Start operating Teaching Pendant.\r"));

                rtbInstruct.AppendText("When the teaching is complete close the T-key and press the [Next] button to continue.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("When the teaching is complete close the T-key and press the [Next] button to continue.\r"));

                rtbInstruct.AppendText("If you need cancel teaching mode,press [Cancel] Button.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If you need cancel teaching mode,press [Cancel] Button.\r"));
                Cursor.Current = Cursors.Default;
                m_eStep = eTeachStep.TkeyOff;
            }
            else
            {
                rtbInstruct.AppendText("Switching failed.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Switching failed.\r"));

                rtbInstruct.AppendText("Press [Next] to continue.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Press [Next] to continue.\r"));

                rtbInstruct.AppendText("If you need cancel teaching mode,press [Cancel] Button.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("If you need cancel teaching mode,press [Cancel] Button.\r"));
            }
            EnableProcedureButton(true);
        }
        private void SkipTkeyOnTask()
        {
            if (new frmMessageBox(string.Format("Abort teaching ?"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes)
            {
                EnableProcedureButton(true);
                return;
            }

            rtbInstruct.Clear();

            rtbInstruct.AppendText("Turn T-key off.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Turn T-key off.\r"));

            rtbInstruct.AppendText("Press [Next] to continue.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Press [Next] to continue.\r"));

            rtbInstruct.AppendText("If you need cancel teaching mode,press [Cancel] Button.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("If you need cancel teaching mode,press [Cancel] Button.\r"));
            Cursor.Current = Cursors.WaitCursor;

            EnableProcedureButton(true);
            m_eStep = eTeachStep.TkeyOff;
        }
        //  step3
        private void DoTkeyOffTask()
        {
            if (dlgIsTkeyOn == null || dlgIsTkeyOn() == true)
            {
                new frmMessageBox("Please turn off the T-key.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                EnableProcedureButton(true);
                return;
            }

            rtbInstruct.Clear();
            rtbInstruct.AppendText("Excuting Origin,Please wait.\r");
            rtbInstruct.AppendText(GParam.theInst.GetLanguage("Excuting Origin,Please wait.\r"));
            Cursor.Current = Cursors.WaitCursor;

            m_robot.DoManualProcessing += (object Manual) =>
            {
                I_Robot robotManual = Manual as I_Robot;
                //Mode
                robotManual.ModeW(robotManual.GetAckTimeout, 1);
                //Home
                robotManual.ResetInPos();
                robotManual.MoveToStandbyPosW(robotManual.GetAckTimeout);
                robotManual.WaitInPos(robotManual.GetMotionTimeout);

                frmOrgn _frmOrgn = new frmOrgn(ListTRB, ListSTG, ListALN, ListSTK, ListBUF, _equipment, m_bSimulate);
                bool bSucc = (DialogResult.OK == _frmOrgn.ShowDialog());
                if (bSucc == false)
                {
                    throw new RorzeUnit.Class.SException((int)enumRobotError.AckTimeout, string.Format("Failed to execute origin."));
                }

            };
            m_robot.OnManualCompleted += DoTkeyOffComplete;
            m_robot.StartManualFunction();
        }
        private void DoTkeyOffComplete(object sender, bool bSuc)
        {
            I_Robot robot = (I_Robot)sender;
            robot.OnManualCompleted -= DoTkeyOffComplete;

            rtbInstruct.Clear();
            if (bSuc)
            {
                rtbInstruct.AppendText("Completion of the origin.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Completion of the origin.\r"));

                rtbInstruct.AppendText("Press the [Next] or [Cancel] button.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Press the [Next] or [Cancel] button.\r"));

                Cursor.Current = Cursors.Default;
                m_eStep = eTeachStep.End;
            }
            else
            {
                rtbInstruct.AppendText("Failed at the origin.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Failed at the origin.\r"));

                rtbInstruct.AppendText("Press [Next] or [Cancel] to retry.\r");
                rtbInstruct.AppendText(GParam.theInst.GetLanguage("Press [Next] or [Cancel] to retry.\r"));
            }
            EnableProcedureButton(true);
        }
        //  step4
        private void ProcessEnd()
        {
            tabControl1.SelectedTab = tabPage1;
            delegateMDILock?.Invoke(false);
        }


        //  Robot Origin
        private bool DoOrgn()
        {
            EnableProcedureButton(false);

            //Home
            m_robot.ResetInPos();
            m_robot.MoveToStandbyPosW(m_robot.GetAckTimeout);
            m_robot.WaitInPos(m_robot.GetMotionTimeout);

            frmOrgn _frmOrgn = new frmOrgn(ListTRB, ListSTG, ListALN, ListSTK, ListBUF, _equipment, m_bSimulate);
            bool bSucc = (DialogResult.OK == _frmOrgn.ShowDialog());
            return bSucc;
        }


        #endregion

        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion

        private delegate void UpdateButtunUI(Button MsgBox, bool ret);
        public void ChangeButtun(Button MsgBox, bool ret)
        {
            if (InvokeRequired)
            {
                UpdateButtunUI dlg = new UpdateButtunUI(ChangeButtun);
                this.BeginInvoke(dlg, MsgBox, ret);
            }
            else
            {
                if (MsgBox.Enabled != ret)
                {
                    MsgBox.Enabled = ret;
                    if (ret == true)
                        MsgBox.BackColor = Color.Transparent;
                    else
                        MsgBox.BackColor = Color.Gray;
                }
            }
        }


    }
}
