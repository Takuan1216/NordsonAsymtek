using System;
using System.Drawing;
using System.Windows.Forms;
using RorzeApi.Class;

using System.Drawing.Drawing2D;
using RorzeUnit.Interface;
using RorzeUnit.Class.Robot.Enum;
using RorzeComm.Log;
using RorzeUnit.Class;
using RorzeUnit.Class.Robot;
using RorzeComm.Threading;
using System.Collections.Generic;
using System.Linq;


namespace RorzeApi
{
    public partial class frmTeachRobotDataCopy : Form
    {
        private float frmX;     //當前窗體的寬度
        private float frmY;     //當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        private List<I_Robot> m_robotList;
        private List<I_Loadport> m_loadportList;
        private I_Robot m_robot;


        private SProcessDB _accessDBlog;    // 紀錄操作按鈕
        private SPermission _userManager;   // 管理LOGIN使用者權限
        private string _strUserName;        // 登入者名稱

        private enumRbtAddress _eRbtAdrs;      // 要判斷選擇的位置
        private int _nSource = -1;
        private int _nPurpose = -1;

        private List<Button> m_btnSelectRobotList = new List<Button>();
        private List<Button> m_btnSelectLoadportList = new List<Button>();
        private List<Button> m_btnSourceList = new List<Button>();
        private List<Button> m_btnPurposeList = new List<Button>();

        public frmTeachRobotDataCopy(List<I_Robot> robotList, List<I_Loadport> loadportList, SProcessDB db, Class.SPermission userManager)
        {
            InitializeComponent();

            m_robotList = robotList;
            m_loadportList = loadportList;
            _accessDBlog = db;
            _userManager = userManager;

            #region Select InfoPad Button
            m_btnSourceList.Add(btnSourceFOUP1);
            m_btnSourceList.Add(btnSourceFOUP2);
            m_btnSourceList.Add(btnSourceFOUP3);
            m_btnSourceList.Add(btnSourceFOUP4);
            m_btnSourceList.Add(btnSourceFOUP5);
            m_btnSourceList.Add(btnSourceFOUP6);
            m_btnSourceList.Add(btnSourceFOUP7);
            m_btnSourceList.Add(btnSourceFOSB1);
            m_btnSourceList.Add(btnSourceFOSB2);
            m_btnSourceList.Add(btnSourceFOSB3);
            m_btnSourceList.Add(btnSourceFOSB4);
            m_btnSourceList.Add(btnSourceFOSB5);
            m_btnSourceList.Add(btnSourceOCP1);
            m_btnSourceList.Add(btnSourceOCP2);
            m_btnSourceList.Add(btnSourceOCP3);
            m_btnSourceList.Add(btnSourceFPO1);
            foreach (Button btn in m_btnSourceList) btn.Click += btnSource_Click;
            m_btnPurposeList.Add(btnPurposeFOUP1);
            m_btnPurposeList.Add(btnPurposeFOUP2);
            m_btnPurposeList.Add(btnPurposeFOUP3);
            m_btnPurposeList.Add(btnPurposeFOUP4);
            m_btnPurposeList.Add(btnPurposeFOUP5);
            m_btnPurposeList.Add(btnPurposeFOUP6);
            m_btnPurposeList.Add(btnPurposeFOUP7);
            m_btnPurposeList.Add(btnPurposeFOSB1);
            m_btnPurposeList.Add(btnPurposeFOSB2);
            m_btnPurposeList.Add(btnPurposeFOSB3);
            m_btnPurposeList.Add(btnPurposeFOSB4);
            m_btnPurposeList.Add(btnPurposeFOSB5);
            m_btnPurposeList.Add(btnPurposeOCP1);
            m_btnPurposeList.Add(btnPurposeOCP2);
            m_btnPurposeList.Add(btnPurposeOCP3);
            m_btnPurposeList.Add(btnPurposeFPO1);
            foreach (Button btn in m_btnPurposeList) btn.Click += btnPurpose_Click;
            #endregion

            #region Select Robot Button
            tlpSelectRobot.RowStyles.Clear();
            tlpSelectRobot.ColumnStyles.Clear();
            tlpSelectRobot.RowCount = 1;
            tlpSelectRobot.ColumnCount = m_robotList.Count;
            tlpSelectRobot.Dock = DockStyle.Fill;
            for (int i = 0; i < m_robotList.Count; i++)
            {
                if (m_robotList[i].Disable) continue;
                if (GParam.theInst.GetRobot_AllowPort(m_robotList[i].BodyNo - 1).Contains('1') == false) continue;
                tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                Button btn = new Button();
                btn.Font = new Font("微軟正黑體", 18, FontStyle.Bold);
                btn.Text = GParam.theInst.GetLanguage("Robot" + (char)(64 + m_robotList[i].BodyNo));
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Click += btnSelectRobot_Click;
                m_btnSelectRobotList.Add(btn);
                tlpSelectRobot.Controls.Add(btn, m_btnSelectRobotList.Count - 1, 0);
            }
            tlpSelectRobot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            #endregion

            #region Select Loadport Button
            tlpSelectLoadport.RowStyles.Clear();
            tlpSelectLoadport.ColumnStyles.Clear();
            tlpSelectLoadport.RowCount = 1;
            tlpSelectLoadport.ColumnCount = m_loadportList.Count;
            tlpSelectLoadport.Dock = DockStyle.Fill;
            for (int i = 0; i < m_loadportList.Count; i++)
            {
                if (m_loadportList[i].Disable) continue;
                tlpSelectLoadport.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                Button btn = new Button();
                btn.Font = new Font("微軟正黑體", 18, FontStyle.Bold);
                btn.Text = GParam.theInst.GetLanguage("Loadport" + (char)(64 + m_loadportList[i].BodyNo));
                btn.Dock = DockStyle.Fill;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Click += btnSelectLoadport_Click;
                m_btnSelectLoadportList.Add(btn);
                tlpSelectLoadport.Controls.Add(btn, m_btnSelectLoadportList.Count - 1, 0);
            }
            tlpSelectLoadport.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            #endregion

            if (GParam.theInst.FreeStyle)
            {
                btnSave.Image = RorzeApi.Properties.Resources._48_save_;
            }
        }

        private void btnSelectRobot_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobotDataCopy", _strUserName, "Select Robot", btn.Name);
            ClearSelectionSource();
            ClearSelectionPurpose();
            btnSave.Enabled = false;
            btnSave.BackColor = Color.Gray;

            for (int i = 0; i < m_btnSelectRobotList.Count; i++)
            {
                if (m_btnSelectRobotList[i] == btn)
                {
                    if (GParam.theInst.FreeStyle)
                    {
                        m_btnSelectRobotList[i].BackColor = GParam.theInst.ColorTitle;
                    }
                    else
                    {
                        m_btnSelectRobotList[i].BackColor = Color.LightBlue;
                    }

                    string strName1 = GParam.theInst.GetLanguage("RobotA");
                    string strName2 = GParam.theInst.GetLanguage("RobotB");

                    if (strName1 == btn.Text)
                    {
                        m_robot = m_robotList[0];
                    }
                    else if (strName2 == btn.Text)
                    {
                        m_robot = m_robotList[1];
                    }
                    else
                        switch (btn.Text)
                        {
                            case "RobotA":
                            case "Robot A":
                                m_robot = m_robotList[0]; break;
                            case "RobotB":
                            case "Robot B":
                                m_robot = m_robotList[1]; break;
                            default:
                                m_robot = null; break;
                        }

                    if (m_robot == null)
                    {
                        new frmMessageBox("Robot isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    }
                    else
                    {
                        m_robot = m_robotList[i];
                    }
                    continue;
                }
                else
                    m_btnSelectRobotList[i].BackColor = System.Drawing.SystemColors.ControlLight;
            }
        }
        private void btnSelectLoadport_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobotDataCopy", _strUserName, "Select Loadport", btn.Name);
            ClearSelectionSource();
            ClearSelectionPurpose();
            btnSave.Enabled = false;
            btnSave.BackColor = Color.Gray;

            for (int i = 0; i < m_btnSelectLoadportList.Count; i++)
            {
                if (m_btnSelectLoadportList[i] == btn)
                {
                    if (GParam.theInst.FreeStyle)
                    {
                        m_btnSelectLoadportList[i].BackColor = GParam.theInst.ColorTitle;
                    }
                    else
                    {
                        m_btnSelectLoadportList[i].BackColor = Color.LightBlue;
                    }

                    string strName1 = GParam.theInst.GetLanguage("LoadportA");
                    string strName2 = GParam.theInst.GetLanguage("LoadportB");
                    string strName3 = GParam.theInst.GetLanguage("LoadportC");
                    string strName4 = GParam.theInst.GetLanguage("LoadportD");
                    string strName5 = GParam.theInst.GetLanguage("LoadportE");
                    string strName6 = GParam.theInst.GetLanguage("LoadportF");
                    string strName7 = GParam.theInst.GetLanguage("LoadportG");
                    string strName8 = GParam.theInst.GetLanguage("LoadportH");

                    if (strName1 == btn.Text)
                        _eRbtAdrs = enumRbtAddress.STG1_12;
                    else if (strName2 == btn.Text)
                        _eRbtAdrs = enumRbtAddress.STG2_12;
                    else if (strName3 == btn.Text)
                        _eRbtAdrs = enumRbtAddress.STG3_12;
                    else if (strName4 == btn.Text)
                        _eRbtAdrs = enumRbtAddress.STG4_12;
                    else if (strName5 == btn.Text)
                        _eRbtAdrs = enumRbtAddress.STG5_12;
                    else if (strName6 == btn.Text)
                        _eRbtAdrs = enumRbtAddress.STG6_12;
                    else if (strName7 == btn.Text)
                        _eRbtAdrs = enumRbtAddress.STG7_12;
                    else if (strName8 == btn.Text)
                        _eRbtAdrs = enumRbtAddress.STG8_12;
                    else
                        switch (btn.Text)
                        {
                            case "Loadport A": _eRbtAdrs = enumRbtAddress.STG1_12; break;
                            case "Loadport B": _eRbtAdrs = enumRbtAddress.STG2_12; break;
                            case "Loadport C": _eRbtAdrs = enumRbtAddress.STG3_12; break;
                            case "Loadport D": _eRbtAdrs = enumRbtAddress.STG4_12; break;
                            case "Loadport E": _eRbtAdrs = enumRbtAddress.STG5_12; break;
                            case "Loadport F": _eRbtAdrs = enumRbtAddress.STG6_12; break;
                            case "Loadport G": _eRbtAdrs = enumRbtAddress.STG7_12; break;
                            case "Loadport H": _eRbtAdrs = enumRbtAddress.STG8_12; break;
                            default:
                                new frmMessageBox("Loadport isn't find.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                                break;
                        }
                    continue;
                }
                else
                    m_btnSelectLoadportList[i].BackColor = System.Drawing.SystemColors.ControlLight;
            }
        }
        private void btnSource_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobotDataCopy", _strUserName, "Robot", btn.Name);
            for (int i = 0; i < m_btnSourceList.Count; i++)
            {
                if (m_btnSourceList[i] == btn)
                {
                    int n = (int)_eRbtAdrs + i;//0~399
                    if (n == _nPurpose) return;
                    _nSource = (int)_eRbtAdrs + i;//0~399
                    m_btnSourceList[i].BackColor = Color.LightBlue;
                    continue;
                }
                else
                    m_btnSourceList[i].BackColor = System.Drawing.SystemColors.ControlLight;
            }

            if (_nSource != -1 && _nPurpose != -1)
            {
                btnSave.Enabled = true;
                btnSave.BackColor = Color.White;
            }
            ShowStageTeachingData(_nSource);
        }
        private void btnPurpose_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobotDataCopy", _strUserName, "Robot", btn.Name);
            for (int i = 0; i < m_btnPurposeList.Count; i++)
            {
                if (m_btnPurposeList[i] == btn)
                {
                    int n = (int)_eRbtAdrs + i;//0~399
                    if (_nSource == n) return;
                    _nPurpose = (int)_eRbtAdrs + i;//0~399
                    m_btnPurposeList[i].BackColor = Color.LightBlue;
                    continue;
                }
                else
                    m_btnPurposeList[i].BackColor = System.Drawing.SystemColors.ControlLight;
            }

            if (_nSource != -1 && _nPurpose != -1)
            {
                btnSave.Enabled = true;
                btnSave.BackColor = Color.White;
            }
            ShowStageTeachingData(_nSource);
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
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_nSource == -1 || _nPurpose == -1) return;

            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobotDataCopy", _strUserName, "Robot", "Save Data Fashion");

            if (m_robot != null && m_robot.Connected)
            {
                m_robot.OnSetTeachDataCompleted -= _robot_OnSetTeachDataCompleted;//save
                m_robot.OnSetTeachDataCompleted += _robot_OnSetTeachDataCompleted;//save
                m_robot.SetTeachData(_nPurpose);
            }

            ClearSelectionSource();
            ClearSelectionPurpose();

            btnSave.Enabled = false;
            tlpSource.Enabled = false;
            tlpPurpose.Enabled = false;
            btnSave.BackColor = Color.Gray;
            _accessDBlog.InsertEvntLog(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "TeachRobotDataCopy", _strUserName, "Robot", "Save Done");
        }


        //========= 
        private void ShowStageTeachingData(int nStage)
        {
            if (m_robot == null) return;
            if (nStage <= 0) return;
            m_robot.OnGetTeachDataCompleted -= _robot_OnGetTeachDataCompleted;//GET
            m_robot.OnGetTeachDataCompleted += _robot_OnGetTeachDataCompleted;//GET
            m_robot.GetTeachData(nStage);

            EnableButton(false);
        }

        private void ClearSelectionSource()
        {
            foreach (Button btn in m_btnSourceList)
                btn.BackColor = System.Drawing.SystemColors.ControlLight;
            _nSource = -1;
        }
        private void ClearSelectionPurpose()
        {
            foreach (Button btn in m_btnPurposeList)
                btn.BackColor = System.Drawing.SystemColors.ControlLight;
            _nPurpose = -1;
        }
        //========= 
        private void frmRobotDataCopy_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible)
                {
                    _strUserName = _userManager.UserID;

                    m_btnSelectRobotList[0].PerformClick();
                    m_btnSelectLoadportList[0].PerformClick();
                }           
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }

        }

        //========= 
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
                    MsgBox.Enabled = ret;
            }
        }

        //=========
        private void _robot_OnGetTeachDataCompleted(object sender, bool bSuc) //  Robot 執行 ExeGetTeachData 完成後觸發事件
        {
            I_Robot trb = sender as I_Robot;
            trb.OnGetTeachDataCompleted -= _robot_OnGetTeachDataCompleted;//done
            EnableButton(true);
        }
        private void _robot_OnSetTeachDataCompleted(object sender, bool bSuc) //  Robot 執行 ExeSetTeachData 完成後觸發事件
        {
            I_Robot trb = sender as I_Robot;
            trb.OnSetTeachDataCompleted -= _robot_OnSetTeachDataCompleted;//done
            EnableButton(true);
        }
        private void EnableButton(bool bAct)
        {
            gpbSTGselect.Enabled = bAct;
            tlpSource.Enabled = bAct;
            tlpPurpose.Enabled = bAct;
            btnSave.Enabled = bAct;
            delegateMDILock?.Invoke(!bAct);
        }

        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion


    }
}
