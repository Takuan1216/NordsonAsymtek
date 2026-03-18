using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RorzeUnit.Class;
using RorzeUnit.Interface;
using RorzeApi.Class;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit;

using RorzeApi.SECSGEM;
using RorzeApi.GUI;
using RorzeUnit.Class.EQ;
using RorzeComm.Log;
using RorzeUnit.Class.E84.Event;
using System.Threading;
using System.Collections.Concurrent;
using RorzeUnit.Class.EQ.Enum;
using System.Windows.Controls.Primitives;

namespace RorzeApi
{
    public partial class frmCycle : Form
    {
        #region ==========   delegate UI    ==========     
        public delegate void DelegateMDILock(bool bDisable);
        public event DelegateMDILock delegateMDILock;        // 安全機制
        #endregion

        float m_nFrmX;//當前窗體的寬度
        float m_nFrmY;//當前窗體的高度
        bool m_bFrmLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        private List<GUILoadport> m_guiloadportList = new List<GUILoadport>();
        //Unit
        private I_Robot m_robot;
        private List<I_Loadport> m_loadportList;
        private List<I_Aligner> m_alignerList;
        private List<I_E84> m_e84List;
        private List<I_OCR> m_ocrList;
        private I_RC5X0_IO m_rc550;
        private I_RC5X0_IO m_rc530_1, m_rc530_2;
        private SSEquipment m_equipment;
        //DB
        private SProcessDB m_dbProcess;
        private SGroupRecipeManager m_dbGrouprecipe;
        //Class
        private SAlarm m_alarm;
        private STransfer m_autoProcess;      //  自動傳片流程
        private PJCJManager m_JobControl;
        private SGEM300 m_Gem;
        //Logger
        private SLogger _executelog = SLogger.GetLogger("ExecuteLog");

        bool m_bBuildCJPJ = false;//建立料帳動作中, 避免重複建帳,

        #region Form Zoom
        public void SetGUISize(float frmWidth, float frmHeight)
        {
            if (m_bFrmLoaded == false)
            {
                m_nFrmX = this.Width;  //獲取窗體的寬度
                m_nFrmY = this.Height; //獲取窗體的高度      
                m_bFrmLoaded = true;    // 已設定各控制項的尺寸到Tag屬性中
                SetTag(this);       //調用方法
            }
            float tempX = frmWidth / m_nFrmX;  //計算比例
            float tempY = frmHeight / m_nFrmY; //計算比例
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

        public frmCycle(I_Robot robot, List<I_Loadport> loadportList, List<I_Aligner> alignerList, List<I_OCR> ocrList, List<I_E84> e84List,
            I_RC5X0_IO rc550, I_RC5X0_IO rc530_1, I_RC5X0_IO rc530_2,
             SAlarm alarm, PJCJManager Job, SGEM300 Gem, SProcessDB dbProcess, SGroupRecipeManager grouprecipe, SSEquipment equipment, STransfer autoProcess)
        {
            try
            {
                InitializeComponent();
                //  Unit
                m_robot = robot;
                m_loadportList = loadportList;
                m_alignerList = alignerList;
                m_ocrList = ocrList;
                m_e84List = e84List;
                m_rc550 = rc550;
                m_rc530_1 = rc530_1;
                m_rc530_2 = rc530_2;
                m_equipment = equipment;
                //  DB
                m_dbProcess = dbProcess;
                m_dbGrouprecipe = grouprecipe;
                //  Class
                m_alarm = alarm;
                m_autoProcess = autoProcess;
                m_JobControl = Job;
                m_Gem = Gem;

                m_robot.LowerArmWaferChange += Robot_LowerArmWaferChange;       //  更新UI
                m_robot.UpperArmWaferChange += Robot_UpperArmWaferChange;       //  更新UI
                m_alignerList[0].Aligner_WaferChange += AlingerA_WaferChange;   //  更新UI
                m_alignerList[1].Aligner_WaferChange += AlingerB_WaferChange;   //  更新UI
                m_equipment.EQ_WaferChange += Equipment_WaferChange;

                #region Loadport
                m_guiloadportList.Add(guiLoadport1);
                m_guiloadportList.Add(guiLoadport2);
                m_guiloadportList.Add(guiLoadport3);
                m_guiloadportList.Add(guiLoadport4);
                m_guiloadportList.Add(guiLoadport5);
                m_guiloadportList.Add(guiLoadport6);
                m_guiloadportList.Add(guiLoadport7);
                m_guiloadportList.Add(guiLoadport8);

                for (int i = 0; i < m_loadportList.Count; i++)
                {
                    m_guiloadportList[i].Simulate = GParam.theInst.IsSimulate;
                    m_guiloadportList[i].BodyNo = i + 1;
                    m_guiloadportList[i].Disable_E84 = GParam.theInst.IsE84Disable(i);
                    m_guiloadportList[i].Disable_OCR = GParam.theInst.IsOcrAllDisable();
                    m_guiloadportList[i].Disable_Recipe = (GParam.theInst.IsOcrAllDisable() && GParam.theInst.GetEQRecipeCanSelect == false);
                    m_guiloadportList[i].Disable_RSV = true;
                    m_guiloadportList[i].Disable_ClmpLock = true;
    
                    m_guiloadportList[i].Disable_ProcessBtn = false;
                    m_guiloadportList[i].Visible = GParam.theInst.IsStageVisible(i);
                    m_guiloadportList[i].Enabled = !GParam.theInst.IsStageDisable(i);

                    if (GParam.theInst.IsStageVisible(i) == false || GParam.theInst.IsStageDisable(i))
                    {
                        continue;//不需要註冊
                    }

                    m_loadportList[i].OnFoupExistChenge += OnLoadport_FoupExistChenge;          //  更新UI  
                    m_loadportList[i].OnClmpComplete += OnLoadport_MappingComplete;             //  更新UI
                    m_loadportList[i].OnMappingComplete += OnLoadport_MappingComplete;          //  更新UI
                    m_loadportList[i].OnStatusMachineChange += OnLoadport_StatusMachineChange;  //  更新UI
                    m_loadportList[i].OnFoupIDChange += OnLoadport_FoupIDChange;                //  更新UI
                    m_loadportList[i].OnFoupTypeChange += OnLoadport_FoupTypeChange;            //  更新UI

                    //m_loadportList[i].OnUclmComplete += OnLoadport_OnUclmComplete;              //  更新UI
                    m_e84List[i].OnAceessModeChange += OnLoadport_E84ModeChange;                //  更新UI
                    m_loadportList[i].OnTakeWaferInFoup += m_guiloadportList[i].TakeWaferInFoup;//wafer被塞進來
                    m_loadportList[i].OnTakeWaferOutFoup += m_guiloadportList[i].TakeWaferOutFoup;//wafer被拿走            

                    //m_guiloadportList[i].BtnClamp += btnClamp_Click;
                    m_guiloadportList[i].BtnDock += btnDock_Click;
                    m_guiloadportList[i].BtnUnDock += btnUnDock_Click;
                    m_guiloadportList[i].BtnE84Mode += btnE84Mode_Click;
                    m_guiloadportList[i].ChkRecipeSelect += chkRecipeSelect_Checked;
                    m_guiloadportList[i].BtnProcess += btnProcess_Click;
                    m_guiloadportList[i].ChkFoupOn += chkFoupOn_Checked;
                    //m_guiloadportList[i].FoupIDKeyDownEnter += txtLoaderFoupID_Enter;
                    //m_guiloadportList[i].LotIDKeyDownEnter += txtLoaderLotID_Enter;
                    //m_guiloadportList[i].UseSelectWafer += GuiLoadport_UseSelectWafer;//選片功能
                }

                tabPage1.Text = "";
                if (GParam.theInst.IsStageVisible(0)) tabPage1.Text += "A";
                if (GParam.theInst.IsStageVisible(1)) tabPage1.Text += "B";
                if (GParam.theInst.IsStageVisible(2)) tabPage1.Text += "C";
                if (GParam.theInst.IsStageVisible(3)) tabPage1.Text += "D";
                tabPage2.Text = "";
                if (GParam.theInst.IsStageVisible(4)) tabPage2.Text += "E";
                if (GParam.theInst.IsStageVisible(5)) tabPage2.Text += "F";
                if (GParam.theInst.IsStageVisible(6)) tabPage2.Text += "G";
                if (GParam.theInst.IsStageVisible(7)) tabPage2.Text += "H";

                //只有一頁
                if (tabPage2.Text == "")
                {
                    //  消失頁籤
                    tabPage2.Parent = null;
                    //tabCtrlStage.SizeMode = TabSizeMode.Fixed;
                    //tabCtrlStage.ItemSize = new Size(0, 1);
                    int nCount = tabPage1.Text.Length;
                    if (nCount <= 1)
                    {
                        foreach (GUILoadport item in m_guiloadportList)
                        {
                            item.Width = (flowLayoutPanel1.Width - 0) / 2;
                            item.Height = flowLayoutPanel1.Height - 5;
                        }
                    }
                    else
                    {
                        foreach (GUILoadport item in m_guiloadportList)
                        {
                            item.Width = (flowLayoutPanel1.Width - 0) / nCount;
                            item.Height = flowLayoutPanel1.Height - 5;
                        }
                    }
                }
                else//有第二頁
                {
                    tabPage2.Parent = tabCtrlStage;
                    foreach (GUILoadport item in m_guiloadportList)
                    {
                        item.Width = (flowLayoutPanel1.Width - 0) / 4;
                        item.Height = flowLayoutPanel1.Height - 5;
                    }
                }
                #endregion

                //隱藏元件
                guiAlignerAStatus.Visible = !m_alignerList[0].Disable;
                guiAlignerBStatus.Visible = !m_alignerList[1].Disable;
                guiEquipmentStatus.Visible = !m_equipment.Disable;

                lblCycleStartTime.Text = "";
                lblCycleEndTime.Text = "";
            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }


        //========== 建帳    
        private void CreateLot(int BodyNo, I_Loadport selectLoad, string strFoupID, string strOPID, string strWafer, string strRecipe)
        {
            lock (this)
            {
                int index = BodyNo - 1;
                try
                {
                    SpinWait.SpinUntil(() => false, 1000);//因為可能會同時建帳，避免CJID重複
                    #region 檢查
                    //=========== 避免重複建帳
                    if (m_bBuildCJPJ)
                    {
                        _executelog.WriteLog("<<<Error>>> Trigger to create wafer data two times at loader[{0}].", index + 1);
                        return;
                    }
                    m_bBuildCJPJ = true;

                    //=========== check condition
                    frmMessageBox frm;
                    if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                    {
                        frm = new frmMessageBox("Now control status is Online Remote , Not process now", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        frm.ShowDialog();
                        return;
                    }

                    if (m_alarm.ToolInitialing)
                    {
                        frm = new frmMessageBox("Tool initialing please wait it complete or abort it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        frm.ShowDialog();
                        return;
                    }

                    if (m_alarm.CurrentAlarm != null && m_alarm.CurrentAlarm.Count > 0)
                    {
                        frm = new frmMessageBox("There are uncleared abnormalities, please confirm the machine status first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        frm.ShowDialog();
                        return;
                    }

                    if (selectLoad.StatusMachine == enumStateMachine.PS_Process) //沒有dock視為沒有載貨完成
                    {
                        frm = new frmMessageBox(string.Format("Loader#{0} is process.", BodyNo), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        frm.ShowDialog();
                        return;
                    }

                    if (selectLoad.StatusMachine != enumStateMachine.PS_Docked) //沒有dock視為沒有載貨完成
                    {
                        frm = new frmMessageBox(string.Format("Loader#{0} not load Foup (or Cassette) yet.", BodyNo), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        frm.ShowDialog();
                        return;
                    }

                    if (strFoupID.Trim().Length <= 0)
                    {
                        frm = new frmMessageBox(string.Format("Loader#{0} FoupID is empty or wrong.", BodyNo), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        frm.ShowDialog();
                        return;
                    }

                    //  Check select wafer
                    if (strWafer == string.Empty || !strWafer.Contains('1'))
                    {
                        new frmMessageBox("Please select wafer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        return;
                    }

                    //  Check mapping error
                    for (int i = 0; i < strWafer.Length; i++)
                    {
                        if (strWafer[i] != '0' && strWafer[i] != '1')
                        {
                            new frmMessageBox("Please check mapping data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                            return;
                        }
                    }
                    //check wafer data has wafer
                    if (selectLoad.MappingData == string.Empty || !selectLoad.MappingData.Contains('1'))
                    {
                        frm = new frmMessageBox(string.Format("Loader#{0} not find Mapping data.", BodyNo), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        frm.ShowDialog();
                        return;
                    }

                    if (m_equipment.Disable == false)
                    {

                        if (GParam.theInst.IsOcrAllDisable() == false || GParam.theInst.GetEQRecipeCanSelect)
                            if (strRecipe.Trim().Length <= 0)
                            {
                                frm = new frmMessageBox(string.Format("Loader#{0} Recipe is empty or wrong.", BodyNo), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                frm.ShowDialog();
                                return;
                            }

                        if (m_equipment.IsPoisitionDetect != null && m_equipment.IsPoisitionDetect() == false)
                        {
                            new frmMessageBox(string.Format("Equipment incorrect location, position detect sensor not yet."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                            return;
                        }
                        if (m_equipment.FilePathCorrect == false)
                        {
                            new frmMessageBox(string.Format("Equipment file path isn't correct."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                            return;
                        }
                        if (m_equipment.ReadFileExist == false)
                        {
                            new frmMessageBox(string.Format("Equipment read file isn't exist."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                            return;
                        }
                        if (m_equipment.IsWaferExist != null && m_equipment.IsWaferExist())
                        {
                            new frmMessageBox(string.Format("Equipment incorrect status, check exist wafer."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                            return;
                        }
                        //if (m_equipment.IsStandy() == false)
                        //{
                        //    new frmMessageBox(string.Format("Equipment is not in standy."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        //    return;
                        //}
                        //if (m_robot.GetEQExtendFlag())
                        //{
                        //    new frmMessageBox(string.Format("Robot extend Equipment flag On."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                        //    return;
                        //}
                    }
                    else
                    {
                        strRecipe = "Test";
                    }

                    //檢查目前lot是否已經開始生產
                    foreach (var lot in selectLoad.Lots)
                    {
                        foreach (var wafer in lot.Wafers.Values)
                        {
                            if (wafer.ProcessStatus == SWafer.enumProcessStatus.WaitProcess) //已經生產中
                            {
                                frm = new frmMessageBox(string.Format("Loader#{0} Lot [{1}] is processing already.", BodyNo, wafer.LotID), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                frm.ShowDialog();
                                return;
                            }
                        }
                    }

                    #endregion

                    if (GMotion.theInst.CycleRunning == false)//在cycle不用問
                        if (new frmMessageBox(string.Format("Are you want to Loader#{0} Process start?", BodyNo), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes) return;

                    if (selectLoad.Lots.Count > 0)
                    {
                        _executelog.WriteLog("<<<Error>>> ready to process but loader has wafer dta . loader{0}", selectLoad.BodyNo);

                        foreach (var lot in selectLoad.Lots)
                        {
                            foreach (SWafer wafer in lot.Wafers.Values)
                                _executelog.WriteLog("<<<Error>>> create load port data abnormal and delete wafer data. [{0}]", wafer.ToString());
                        }
                        //delete all of lots.
                        selectLoad.Lots.Clear();
                    }

                    //=========== 資料準備
                    DateTime dt = DateTime.Now;

                    //建立Lot
                    string strLotID = string.Format("{0}.{1}", strFoupID, selectLoad.Lots.Count);
                    string strCJID = "CJID-" + dt.ToString("yyyyMMddHHmmssfff");
                    string strPJID = "PJID-" + dt.ToString("yyyyMMddHHmmssfff");
                    selectLoad.CJID = strCJID;
                    selectLoad.ExcutPJID = strPJID;
                    selectLoad.FoupID = strFoupID;
                    selectLoad.Lots.Add(new SLot(strFoupID, strLotID, strPJID));
              

                    // SECS 建立帳料
                    if (m_JobControl.CJlist.ContainsKey(strCJID)) m_JobControl.CJlist.Remove(strCJID);

                    m_JobControl.CJlist.Add(strCJID, new SControlJobObject(strCJID));

                    if (m_JobControl.PJlist.ContainsKey(strPJID)) m_JobControl.PJlist.Remove(strPJID);

                    m_JobControl.PJlist.Add(strPJID, new SProcessJobObject(strPJID));

                    m_dbProcess.CreateProcessLotInfo(DateTime.Now, selectLoad.FoupID, selectLoad.CJID, selectLoad.ExcutPJID);

                    //建立帳
                    for (int nCnt = 0; nCnt < selectLoad.WaferTotal; nCnt++)
                    {
                        if (selectLoad.MappingData[nCnt] == '1' && strWafer[nCnt] == '1')
                        {
                            selectLoad.NeedTranferCount++;

                            #region 取出 Waferlist的wafer放入lot
                            SWafer theWafer = selectLoad.Waferlist[nCnt];
                            theWafer.FoupID = strFoupID;
                            theWafer.LotID = strLotID;
                            theWafer.CJID = strCJID;
                            theWafer.PJID = strPJID;
                            theWafer.RecipeID = strRecipe;
                            theWafer.Slot = nCnt + 1;
                            theWafer.ToSlot = nCnt + 1;//新增同位置


                            theWafer.Position = BodyNo == 1 ? SWafer.enumPosition.Loader1 : BodyNo == 2 ? SWafer.enumPosition.Loader2 : BodyNo == 3 ? SWafer.enumPosition.Loader3 : SWafer.enumPosition.Loader4;

                            theWafer.ProcessStatus = SWafer.enumProcessStatus.Sleep;
                            //theWafer.WaferID_F = string.Format("STG{0}_SLOT:{1}", BodyNo, nCnt + 1)  /*string.Empty*/;//M12
                            //theWafer.WaferID_B = string.Format("STG{0}_SLOT:{1}", BodyNo, nCnt + 1);//T7
                            #endregion

                            selectLoad.Lots[0].Wafers.Add(nCnt + 1, theWafer);

                            // SECS 加入帳料
                            //m_JobControl.PJlist[strPJID].AssginSourceTransInfo(selectLoad.FoupID, nCnt + 1, selectLoad.BodyNo);
                            //m_JobControl.PJlist[strPJID].SourceTransInfo[selectLoad.FoupID].AssignInfo(nCnt + 1, nCnt + 1);
                            //m_JobControl.PJlist[strPJID].SourceTransInfo[selectLoad.FoupID].TargetCarrierID = selectLoad.FoupID;
                            //m_JobControl.PJlist[strPJID].SourceTransInfo[selectLoad.FoupID].TargetBodyNo = selectLoad.BodyNo;
                        }
                    }

                    // SECS CJ Assgin PJ
                    m_JobControl.CJlist[strCJID].AssignPJ(1, m_JobControl.PJlist[strPJID]);
                    m_Gem.MaunalProcessRegistCJ(strCJID);
                    m_JobControl.CJlist[strCJID].Status = SECSGEM.JobStatus.QUEUED;
                    m_JobControl.CJlist[strCJID].Status = SECSGEM.JobStatus.Select;
                    m_JobControl.CJlist[strCJID].Status = SECSGEM.JobStatus.EXECUTING;

                    m_JobControl.PJlist[strPJID].Status = SECSGEM.JobStatus.FunctionSetup;
                    m_JobControl.PJlist[strPJID].Status = SECSGEM.JobStatus.EXECUTING;

                    //assign所有wafer開始生產
                    foreach (var lot in selectLoad.Lots)
                    {
                        foreach (var wafer in lot.Wafers.Values)
                        {
                            wafer.ProcessStatus = SWafer.enumProcessStatus.WaitProcess;
                        }
                    }

                    // Create Wafer End && Start process
                    selectLoad.StatusMachine = enumStateMachine.PS_Process;
                    m_Gem.CurrntGEMProcessStats = Rorze.Secs.GEMProcessStats.Executing;
                }
                catch (Exception ex)
                {
                    _executelog.WriteLog(this.Name, ex);
                }
            }
        }


        //========== 基底類別事件處理常式
        private void DoCycleRunWhenLoaderLoad(object sender, LoadPortEventArgs e)
        {
            try
            {
                I_Loadport loaderUnit = sender as I_Loadport;
                //string strLotId = DateTime.Now.ToString("yyyyMMddHHmmss");
                //string strFoupId = loaderUnit.FoupID;
                //string strRecieId = "";
                //string strCJID = "CJID-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                //string strPJID = "PJID-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                if (loaderUnit != null)
                {
                    int index = loaderUnit.BodyNo - 1;

                    string strRecipe = m_guiloadportList[index].Recipe;

                    CreateLot(loaderUnit.BodyNo, m_loadportList[index], m_loadportList[index].FoupID, "", m_loadportList[index].MappingData, strRecipe);

                    m_bBuildCJPJ = false; //建帳結束

                    m_autoProcess.AutoProcessStart(this, new EventArgs());//自動開始
                }
                else _executelog.WriteLog("[Cycle run] function function was failure due to loader#[{0}] object convert is worng.", e.BodyNo);
            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }
        private void DoCycleRunWhenUnload(object sender, LoadPortEventArgs e)
        {
            try
            {
                DateTime dt = DateTime.Now;
                lblCycleEndTime.Text = dt.ToString("yyyy-MM-dd-HH:mm:ss");

                I_Loadport loaderUnit = sender as I_Loadport;

                if (loaderUnit != null)
                {
                    _executelog.WriteLog("[Cycle run] Do loader#[{0}] load for cycle running.", e.BodyNo);
                    //clean wafer data before load cassette.
                    if (loaderUnit.Lots.Count > 0)
                    {
                        _executelog.WriteLog("<<<Warning>>> Wafer data donot be cleaned when cycle load cassette.");
                        loaderUnit.Lots.Clear();
                    }

                    loaderUnit.CLMP();
                }
                else _executelog.WriteLog("[Cycle run] function function was failure due to loader#[{0}] object convert is worng.", e.BodyNo);
            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }

        #region ========== Button ==========
        private void btnDock_Click(object sender, EventArgs e)
        {

            try
            {

                GUILoadport Loader = (GUILoadport)sender;

                if (!m_loadportList[Loader.BodyNo - 1].FoupExist)
                {
                    new frmMessageBox("Loadport has no foup.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }

                if (m_loadportList[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Clamped ||
                    m_loadportList[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Arrived ||
                    m_loadportList[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_UnDocked ||
                    m_loadportList[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_ReadyToUnload)
                {
                    m_loadportList[Loader.BodyNo - 1].CLMP();
                }
                else
                {
                    string strMsg = string.Format("Loadport satus {0} can't dock", m_loadportList[Loader.BodyNo - 1].StatusMachine);
                    new frmMessageBox(strMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                }

            }
            catch (SException ex)
            {
                new frmMessageBox(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                _executelog.WriteLog(this.Name, ex);
            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }


        }
        private void btnUnDock_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;

            if (!m_loadportList[Loader.BodyNo - 1].FoupExist)
            {
                new frmMessageBox("Loadport has no foup.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }
            if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }


            if (m_loadportList[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Docked ||
                m_loadportList[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Stop ||
                m_loadportList[Loader.BodyNo - 1].StatusMachine == enumStateMachine.PS_Complete)
            {
                m_loadportList[Loader.BodyNo - 1].UCLM();
            }
            else
            {
                string strMsg = string.Format("Loadport satus {0} can't undock", m_loadportList[Loader.BodyNo - 1].StatusMachine);
                new frmMessageBox(strMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
            }
        }
        private void btnProcess_Click(object sender, string strSelectWafer)
        {
            try
            {
                GUILoadport Loader = (GUILoadport)sender;

                if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
                {
                    new frmMessageBox("Now control status is Online Remote ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }

                int index = Loader.BodyNo - 1;

                string strRecipe = m_guiloadportList[index].Recipe;

                CreateLot(Loader.BodyNo, m_loadportList[index], m_loadportList[index].FoupID, "", strSelectWafer, strRecipe);

                m_bBuildCJPJ = false; //建帳結束

                m_autoProcess.AutoProcessStart(this, new EventArgs());//自動開始
            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }
        private void btnE84Mode_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;

            if (GParam.theInst.IsE84Disable(Loader.BodyNo - 1)) return;

            if (m_e84List[Loader.BodyNo - 1] == null) return;

            if (m_e84List[Loader.BodyNo - 1].GetAutoMode == false)
                m_e84List[Loader.BodyNo - 1].SetAutoMode(true);
            else
                m_e84List[Loader.BodyNo - 1].SetAutoMode(false);
        }
        private void chkFoupOn_Checked(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;

            try
            {
                if (m_loadportList[Loader.BodyNo - 1].SimulateCheckFoupExist == false)
                {
                    m_loadportList[Loader.BodyNo - 1].SimulateCheckFoupExist = true;
                    if (m_e84List[Loader.BodyNo - 1].GetAutoMode)  // E84 Auto 不Check  
                    {
                        if (m_e84List[Loader.BodyNo - 1].isCs0On || m_e84List[Loader.BodyNo - 1].isValidOn) return;
                    }
                    m_loadportList[Loader.BodyNo - 1].CheckFoupExist();
                }
                else
                {
                    m_loadportList[Loader.BodyNo - 1].SimulateCheckFoupExist = false;
                    m_loadportList[Loader.BodyNo - 1].CheckFoupExist();
                }
            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }


        private void btnCycle_Click(object sender, EventArgs e)
        {
            frmMessageBox frm;
            if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                frm = new frmMessageBox("Now control status is Online Remote , Not process now", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                return;
            }

            foreach (I_Loadport lp in m_loadportList)
                if (lp.Finish)
                {
                    new frmMessageBox("Cannot set cycle-run due to FINIH be set now!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }

            if (m_equipment.Disable == false)
            {
                //if (m_equipment.IsDoorOpen != null && m_equipment.IsDoorOpen() == false)
                //{
                //    new frmMessageBox(string.Format("Equipment door isn't open yet."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                //    return;
                //}
                if (m_equipment.IsPoisitionDetect != null && m_equipment.IsPoisitionDetect() == false)
                {
                    new frmMessageBox(string.Format("Equipment incorrect location, position detect sensor not yet."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
                if (m_equipment.FilePathCorrect == false)
                {
                    new frmMessageBox(string.Format("Equipment file path isn't correct."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
                if (m_equipment.ReadFileExist == false && GMotion.theInst.CycleRunning == false)
                {
                    new frmMessageBox(string.Format("Equipment read file isn't exist."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
                if (m_equipment.IsWaferExist != null && m_equipment.IsWaferExist() && GMotion.theInst.CycleRunning == false)
                {
                    new frmMessageBox(string.Format("Equipment incorrect status, check exist wafer."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                    return;
                }
                //if (m_equipment.IsStandy() == false && GMotion.theInst.CycleRunning == false)
                //{
                //    new frmMessageBox(string.Format("Equipment is not in standy."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                //    return;
                //}
                //if (m_robot.GetEQExtendFlag())
                //{
                //    new frmMessageBox(string.Format("Robot extend Equipment flag On."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                //    return;
                //}
            }

            if (new frmMessageBox(string.Format("Are you want to {0} cycle run processing?", GMotion.theInst.CycleRunning ? "RESET" : "SET"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes) return;

            try
            {
                GMotion.theInst.CycleRunning = !GMotion.theInst.CycleRunning;
                if (GMotion.theInst.CycleRunning)
                {
                    //=========== 資料準備
                    DateTime dt = DateTime.Now;

                    lblCycleStartTime.Text = dt.ToString("yyyy-MM-dd-HH:mm:ss");
                    GMotion.theInst.CycleCount = 0;


                    foreach (I_Loadport theLoadport in m_loadportList)
                    {
                        if (theLoadport.Disable) continue;

                        theLoadport.OnUclmComplete -= DoCycleRunWhenUnload;
                        theLoadport.OnUclmComplete += DoCycleRunWhenUnload;
                        theLoadport.OnClmpComplete -= DoCycleRunWhenLoaderLoad;
                        theLoadport.OnClmpComplete += DoCycleRunWhenLoaderLoad;

                        if (theLoadport.StatusMachine == enumStateMachine.PS_Clamped ||
                            theLoadport.StatusMachine == enumStateMachine.PS_Arrived ||
                            theLoadport.StatusMachine == enumStateMachine.PS_UnDocked ||
                            theLoadport.StatusMachine == enumStateMachine.PS_ReadyToUnload)
                        {
                            theLoadport.CLMP();
                        }
                        else if (theLoadport.StatusMachine == enumStateMachine.PS_Docked ||
                            theLoadport.StatusMachine == enumStateMachine.PS_Stop ||
                            theLoadport.StatusMachine == enumStateMachine.PS_Complete)
                        {
                            theLoadport.UCLM();
                        }

                    }

                    if (delegateMDILock != null) { delegateMDILock(true); }

                }
                else
                {
                    DateTime dt = DateTime.Now;
                    lblCycleEndTime.Text = dt.ToString("yyyy-MM-dd-HH:mm:ss");

                    m_loadportList[0].OnUclmComplete -= DoCycleRunWhenUnload;
                    m_loadportList[1].OnUclmComplete -= DoCycleRunWhenUnload;
                    m_loadportList[2].OnUclmComplete -= DoCycleRunWhenUnload;
                    m_loadportList[3].OnUclmComplete -= DoCycleRunWhenUnload;

                    m_loadportList[0].OnClmpComplete -= DoCycleRunWhenLoaderLoad;
                    m_loadportList[1].OnClmpComplete -= DoCycleRunWhenLoaderLoad;
                    m_loadportList[2].OnClmpComplete -= DoCycleRunWhenLoaderLoad;
                    m_loadportList[3].OnClmpComplete -= DoCycleRunWhenLoaderLoad;

                    if (delegateMDILock != null) { delegateMDILock(false); }

                }

            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }
        private void btnFinish_Click(object sender, EventArgs e)
        {
            if (m_Gem.GEMControlStatus == Rorze.Secs.GEMControlStats.ONLINEREMOTE)
            {
                new frmMessageBox("Now control status is Online Remote , Not process now", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                return;
            }

            if (GMotion.theInst.CycleRunning)
            {
                new frmMessageBox("Cannot set finish due to Cycle-Run be set!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                return;
            }

            if (m_loadportList[0].Finish || m_loadportList[1].Finish || m_loadportList[2].Finish || m_loadportList[3].Finish)
            {
                if (new frmMessageBox("Are you want to CANCEL finish function and keep prcessing wafer?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes) return;
                m_loadportList[0].Finish = false;
                m_loadportList[1].Finish = false;
                m_loadportList[2].Finish = false;
                m_loadportList[3].Finish = false;
            }
            else
            {
                if (new frmMessageBox("Are you want to FINISH current wafer which in chamber and stop to process wafer from load port?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question).ShowDialog() != System.Windows.Forms.DialogResult.Yes) return;
                if ((m_loadportList[0].StatusMachine == enumStateMachine.PS_Process)) m_loadportList[0].Finish = true;
                if ((m_loadportList[1].StatusMachine == enumStateMachine.PS_Process)) m_loadportList[1].Finish = true;
                if ((m_loadportList[2].StatusMachine == enumStateMachine.PS_Process)) m_loadportList[2].Finish = true;
                if ((m_loadportList[3].StatusMachine == enumStateMachine.PS_Process)) m_loadportList[3].Finish = true;
            }


        }
        private void chkRecipeSelect_Checked(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;
            int index = Loader.BodyNo - 1;
            try
            {
                string strRecipe = m_guiloadportList[index].Recipe;
                //確認有這個recipe
                if (m_dbGrouprecipe.GetRecipeGroupList.ContainsKey(strRecipe) == false) return;

                GParam.theInst.SetDefaultRecipe(strRecipe);//紀錄使用哪一筆
                SGroupRecipe RecipeContent = m_dbGrouprecipe.GetRecipeGroupList[strRecipe];
            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }

        #endregion

        //========== 畫面事件處理常式
        private void tmrUpdateUI_Tick(object sender, EventArgs e)
        {
            try
            {
                tmrUpdateUI.Enabled = false;
                btnCycle.BackColor = GMotion.theInst.CycleRunning == true ? Color.LightBlue : SystemColors.Control;
                lblCycleCout.Text = GMotion.theInst.CycleCount.ToString();
                btnFinish.BackColor = m_loadportList[0].Finish == true || m_loadportList[1].Finish == true || m_loadportList[2].Finish == true || m_loadportList[3].Finish == true ? Color.LightBlue : SystemColors.Control;

                #region 更新 UserCtrl Status

                if (m_robot != null)
                {
                    if (!m_robot.Connected && !GParam.theInst.IsSimulate)
                    {
                        guiRobotStatus.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disconnect;
                    }
                    else if (m_robot.Disable)
                    {
                        guiRobotStatus.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disable;
                    }
                    else if (m_robot.IsMoving)
                    {
                        guiRobotStatus.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Moving;
                    }
                    else if (m_robot.IsError)
                    {
                        guiRobotStatus.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Error;
                    }
                    else if (m_robot.IsOrgnComplete)
                    {
                        guiRobotStatus.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Idle;
                    }
                    else
                    {
                        if (!m_robot.Connected)
                        {
                            guiRobotStatus.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Disconnect;
                        }
                        guiRobotStatus.SetStatus = _0.GUI_UserCtrl.GUIRobotStatus.enumState.Unknown;
                    }
                }

                if (m_alignerList.Count > 1)
                {
                    if (m_alignerList[0].Disable)
                    {
                        guiAlignerAStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Disable;
                    }
                    else if (m_alignerList[0].IsMoving)
                    {
                        guiAlignerAStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Moving;
                    }
                    else if (m_alignerList[0].IsError)
                    {
                        guiAlignerAStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Error;
                    }
                    else if (m_alignerList[0].IsOrgnComplete)
                    {
                        guiAlignerAStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Idle;
                    }
                    else
                    {
                        guiAlignerAStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Unknown;
                    }
                }

                if (m_alignerList.Count > 2)
                {
                    if (m_alignerList[1].Disable)
                    {
                        guiAlignerBStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Disable;
                    }
                    else if (m_alignerList[1].IsMoving)
                    {
                        guiAlignerBStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Moving;
                    }
                    else if (m_alignerList[1].IsError)
                    {
                        guiAlignerBStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Error;
                    }
                    else if (m_alignerList[1].IsOrgnComplete)
                    {
                        guiAlignerBStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Idle;
                    }
                    else
                    {
                        guiAlignerBStatus.SetStatus = _0.GUI_UserCtrl.GUIAlignerStatus.enumState.Unknown;
                    }
                }

                if (m_equipment != null)
                {

                    if (m_equipment.Disable)
                    {
                        guiEquipmentStatus.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Disable;
                    }
                    else if (m_equipment.IsProcessing)
                    {
                        guiEquipmentStatus.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Moving;
                    }
                    else if (m_equipment.IsError)
                    {
                        guiEquipmentStatus.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Error;
                    }
                    else if (/*m_equipment.IsDoorOpen != null && m_equipment.IsDoorOpen() &&*/
                             m_equipment.IsPoisitionDetect != null && m_equipment.IsPoisitionDetect())
                    {
                        guiEquipmentStatus.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Idle;
                    }
                    else
                    {
                        guiEquipmentStatus.SetStatus = _0.GUI_UserCtrl.GUIEquipmentStatus.enumState.Unknown;
                    }
                }

                #endregion

                #region ==========Loadport
                for (int nPort = 0; nPort < m_loadportList.Count; nPort++)
                {
                    if (m_loadportList[nPort].Disable) continue;

                    m_guiloadportList[nPort].KeepClamp = m_loadportList[nPort].IsKeepClamp;

                    if (m_loadportList[nPort].Waferlist == null) continue;

                    for (int nSlot = 1; nSlot <= m_loadportList[nPort].Waferlist.Count; nSlot++)
                    {
                        SWafer waferShow = m_loadportList[nPort].Waferlist[nSlot - 1];

                        if (waferShow == null)//empty
                        {
                            m_guiloadportList[nPort].UpdataWaferStatus(nSlot, "");
                            m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.None, SystemColors.Control);
                        }
                        else
                        {
                            switch (waferShow.ProcessStatus)
                            {
                                case SWafer.enumProcessStatus.Sleep:
                                    m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Sleep, Color.LimeGreen);
                                    break;
                                case SWafer.enumProcessStatus.WaitProcess:
                                    m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.WaitProcess, Color.RoyalBlue);
                                    break;
                                case SWafer.enumProcessStatus.Processing:
                                    m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Processing, SystemColors.Control);
                                    break;
                                case SWafer.enumProcessStatus.Processed:
                                    {
                                        if (waferShow.WaferIDComparison == SWafer.enumWaferIDComparison.IDAbort)
                                            m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Abort, Color.Red);
                                        else
                                            m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Processed, Color.HotPink);
                                    }
                                    break;
                                case SWafer.enumProcessStatus.Error:
                                    m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Error, Color.Red);
                                    break;
                            }

                            m_guiloadportList[nPort].UpdataWaferStatus(nSlot, waferShow.WaferID_F, waferShow.WaferID_B, waferShow.Position);
                        }

                    }
                }
                #endregion



                tmrUpdateUI.Enabled = true;
            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }
        private void frmMain_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                tmrUpdateUI.Enabled = this.Visible;

                if (this.Visible)
                {
                    string strDefaultRecipe = GParam.theInst.GetDefaultRecipe;
                    foreach (GUILoadport item in m_guiloadportList)//更新grouprecipe list
                    {
                        if (m_dbGrouprecipe != null)
                        {
                            item.SetRecipList(m_dbGrouprecipe.GetRecipeGroupList.Keys.ToArray(), strDefaultRecipe);
                        }

                        int nIndex = item.BodyNo - 1;
                        if (m_loadportList[nIndex].Disable == false)
                        {
                            //初始值
                            OnLoadport_FoupExistChenge(m_loadportList[nIndex], new FoupExisteChangEventArgs(m_loadportList[nIndex].FoupExist));
                            OnLoadport_MappingComplete(m_loadportList[nIndex], new LoadPortEventArgs(m_loadportList[nIndex].MappingData, m_loadportList[nIndex].BodyNo));
                            OnLoadport_StatusMachineChange(m_loadportList[nIndex], new OccurStateMachineChangEventArgs(m_loadportList[nIndex].StatusMachine));
                            OnLoadport_FoupIDChange(m_loadportList[nIndex], new EventArgs());
                            OnLoadport_FoupTypeChange(m_loadportList[nIndex], m_loadportList[nIndex].FoupTypeName);
                          
                            OnLoadport_E84ModeChange(m_e84List[nIndex], new E84ModeChangeEventArgs(m_guiloadportList[nIndex].E84Status == GUILoadport.enumE84Status.Auto));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _executelog.WriteLog(this.Name, ex);
            }
        }
        //  更新UI
        private void Robot_UpperArmWaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            if (m_robot == null) return;
            //上arm upper料帳資訊
            SWafer waferShow = e.Wafer;
            if (waferShow != null)
            {
                guiRobotStatus.SetUpperWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);

                guiRobotStatus.SetUpperWaferRecipe = waferShow.RecipeID;
                guiRobotStatus.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s1_HaveWafer;
            }
            else
            {
                guiRobotStatus.SetUpperWaferSlotNo = "-";

                guiRobotStatus.SetUpperWaferRecipe = "-";
                guiRobotStatus.SetUpperWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
            }
        }
        private void Robot_LowerArmWaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            if (m_robot == null) return;
            //下arm lower料帳資訊
            SWafer waferShow = e.Wafer;
            if (waferShow != null)
            {
                guiRobotStatus.SetLowerWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);

                guiRobotStatus.SetLowerWaferRecipe = waferShow.RecipeID;
                guiRobotStatus.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s1_HaveWafer;
            }
            else
            {
                guiRobotStatus.SetLowerWaferSlotNo = "-";

                guiRobotStatus.SetLowerWaferRecipe = "-";
                guiRobotStatus.SetLowerWaferStatus = GUI.GUIRobotArm.enuWaferStatus.s0_Idle;
            }
        }
        private void AlingerA_WaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            if (m_alignerList[0].Disable) return;
            SWafer waferShow = m_alignerList[0].Wafer;
            if (waferShow != null)
            {
                guiAlignerAStatus.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);

                guiAlignerAStatus.SetWaferRecipe = waferShow.RecipeID;
                guiAlignerAStatus.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s1_HaveWafer;
            }
            else
            {
                guiAlignerAStatus.SetWaferSlotNo = "-";

                guiAlignerAStatus.SetWaferRecipe = "-";
                guiAlignerAStatus.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s0_Idle;
            }

        }
        private void AlingerB_WaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            if (m_alignerList[1].Disable) return;
            SWafer waferShow = m_alignerList[1].Wafer;
            if (waferShow != null)
            {
                guiAlignerBStatus.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);

                guiAlignerBStatus.SetWaferRecipe = waferShow.RecipeID;
                guiAlignerBStatus.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s1_HaveWafer;
            }
            else
            {
                guiAlignerBStatus.SetWaferSlotNo = "-";

                guiAlignerBStatus.SetWaferRecipe = "-";
                guiAlignerBStatus.SetWaferStatus = GUI.GUIAligner.enuWaferStatus.s0_Idle;
            }
        }
        private void Equipment_WaferChange(object sender, RorzeUnit.Event.WaferDataEventArgs e)
        {
            if (m_equipment == null || m_equipment.Disable) return;
            SWafer waferShow = m_equipment.Wafer;
            if (waferShow != null)
            {
                guiEquipmentStatus.SetWaferSlotNo = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);

                guiEquipmentStatus.SetWaferRecipe = waferShow.RecipeID;

                if (waferShow.WaferSize == SWafer.enumWaferSize.Frame)
                    guiEquipmentStatus.SetWaferStatus = GUIEquipment.enuWaferStatus.s2_HaveFRAME;

                else
                    guiEquipmentStatus.SetWaferStatus = GUIEquipment.enuWaferStatus.s1_HaveWafer;

            }
            else
            {
                guiEquipmentStatus.SetWaferSlotNo = "-";

                guiEquipmentStatus.SetWaferRecipe = "-";
                guiEquipmentStatus.SetWaferStatus = GUI.GUIEquipment.enuWaferStatus.s0_Idle;
            }
        }
        //  Loadport 註冊事件來更新 UI
        void OnLoadport_OnUclmComplete(object sender, LoadPortEventArgs e)
        {
            //I_Loadport loaderUnit = sender as I_Loadport;
            //EnableControlButton(true);
        }
        void OnLoadport_OnUclm1Complete(object sender, LoadPortEventArgs e)
        {
            //I_Loadport loaderUnit = sender as I_Loadport;
            //EnableControlButton(true);
        }
        void OnLoadport_FoupExistChenge(object sender, FoupExisteChangEventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int index = loaderUnit.BodyNo - 1;
            m_guiloadportList[index].UpdataFoupExist(e.FoupExist);
        }
        void OnLoadport_MappingComplete(object sender, LoadPortEventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int index = loaderUnit.BodyNo - 1;
            string strMapData = e.MappingData;
            m_guiloadportList[index].UpdataMappingData(strMapData);
        }
        void OnLoadport_StatusMachineChange(object sender, OccurStateMachineChangEventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int index = loaderUnit.BodyNo - 1;
            enumStateMachine eStatus = e.StatusMachine;
            switch (eStatus)
            {
                case enumStateMachine.PS_Abort:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Abort;
                    break;
                case enumStateMachine.PS_Arrived:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Arrived;
                    break;
                case enumStateMachine.PS_Clamped:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Clamped;
                    break;
                case enumStateMachine.PS_Complete:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Complete;
                    break;
                case enumStateMachine.PS_Disable:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Disable;
                    break;
                case enumStateMachine.PS_Docked:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Docked;
                    break;
                case enumStateMachine.PS_Docking:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Docking;
                    break;
                case enumStateMachine.PS_Error:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Error;
                    break;
                case enumStateMachine.PS_FoupOn:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.FoupOn;
                    break;
                case enumStateMachine.PS_FuncSetup:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.FuncSetup;
                    break;
                case enumStateMachine.PS_FuncSetupNG:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.FuncSetupNG;
                    break;
                case enumStateMachine.PS_Process:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Process;
                    break;
                case enumStateMachine.PS_ReadyToLoad:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.ReadyToLoad;
                    break;
                case enumStateMachine.PS_ReadyToUnload:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.ReadyToUnload;
                    break;
                case enumStateMachine.PS_Removed:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Removed;
                    break;
                case enumStateMachine.PS_Stop:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Stop;
                    break;
                case enumStateMachine.PS_UnClamped:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.UnClamped;
                    break;
                case enumStateMachine.PS_UnDocked:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.UnDocked;
                    break;
                case enumStateMachine.PS_UnDocking:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.UnDocking;
                    break;
                case enumStateMachine.PS_Unknown:
                    m_guiloadportList[index].Status = GUILoadport.enumLoadportStatus.Unknown;
                    break;
            }

        }
        void OnLoadport_FoupIDChange(object sender, EventArgs e)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int nIndex = loaderUnit.BodyNo - 1;
            m_guiloadportList[nIndex].FoupID = loaderUnit.FoupID;
        }
        void OnLoadport_FoupTypeChange(object sender, string strName)
        {
            I_Loadport loaderUnit = sender as I_Loadport;
            if (loaderUnit.Disable) return;
            int nIndex = loaderUnit.BodyNo - 1;

            m_guiloadportList[nIndex].InfoPadName = strName;

        }

        void OnLoadport_E84ModeChange(object sender, E84ModeChangeEventArgs e)
        {
            I_E84 thenumUnit = sender as I_E84;
            if (thenumUnit.Disable) return;
            int index = thenumUnit.BodyNo - 1;
            m_guiloadportList[index].E84Status = e.Auto ? GUILoadport.enumE84Status.Auto : GUILoadport.enumE84Status.Manual;
        }



    }
}
