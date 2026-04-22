using Rorze.Equipments.Unit;
using RorzeApi.Class;
using RorzeApi.GUI;
using RorzeComm.Log;
using RorzeUnit.Class;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.RC500.RCEnum;
using RorzeUnit.Class.Robot;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using static Rorze.Equipments.Unit.SRobot;
using static RorzeUnit.Class.SWafer;

namespace RorzeApi
{
    public partial class frmWaferRecover : Form
    {
        List<GUILoadport> m_guiloadportList = new List<GUILoadport>();
        List<I_Robot> ListTRB;
        List<I_Loadport> ListSTG;
        List<I_Aligner> ListALN;
        List<I_E84> ListE84;
        List<I_Buffer> ListBUF;
        List<I_OCR> ListOCR;
        List<SSEquipment> ListEQM;
        SSSorterSQL m_MySQL;
        Dictionary<enumUnit, bool> m_DicDoneOK = new Dictionary<enumUnit, bool>();
        SLogger m_logger = SLogger.GetLogger("ExecuteLog");
        private enumRC550Axis m_eXAX1 = enumRC550Axis.AXS1;


        public frmWaferRecover(List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN,
             List<I_E84> listE84, List<I_Buffer> listBUF, List<I_OCR> listOCR, SSSorterSQL mySQL, List<SSEquipment> listEQM)
        {
            InitializeComponent();

            ListTRB = listTRB;
            ListSTG = listSTG;
            ListALN = listALN;
            ListE84 = listE84;
            ListBUF = listBUF;
            ListOCR = listOCR;
            m_MySQL = mySQL;
            ListEQM = listEQM;

            tlpTRB1.Visible = ListTRB[0] != null && !ListTRB[0].Disable;
            tlpTRB2.Visible = ListTRB[1] != null && !ListTRB[1].Disable;
            tlpALN1.Visible = ListALN[0] != null && !ListALN[0].Disable;
            tlpALN2.Visible = ListALN[1] != null && !ListALN[1].Disable;

            tlpBUF1_1.Visible = ListBUF[0].IsSlotDisable(0) == false;
            tlpBUF1_2.Visible = ListBUF[0].IsSlotDisable(1) == false;
            tlpBUF1_3.Visible = ListBUF[0].IsSlotDisable(2) == false;
            tlpBUF1_4.Visible = ListBUF[0].IsSlotDisable(3) == false;

            tlpBUF2_1.Visible = ListBUF[1].IsSlotDisable(0) == false;
            tlpBUF2_2.Visible = ListBUF[1].IsSlotDisable(1) == false;
            tlpBUF2_3.Visible = ListBUF[1].IsSlotDisable(2) == false;
            tlpBUF2_4.Visible = ListBUF[1].IsSlotDisable(3) == false;

            tplEQ1.Visible = ListEQM[0] != null && ListEQM[0].Disable == false;
            tplEQ2.Visible = ListEQM[1] != null && ListEQM[1].Disable == false;
            tplEQ3.Visible = ListEQM[2] != null && ListEQM[2].Disable == false;
            tplEQ4.Visible = ListEQM[3] != null && ListEQM[3].Disable == false;

            //  Robot       
            for (int i = 0; i < ListTRB.Count; i++)
            {
                m_DicDoneOK[enumUnit.TRB1 + i] = ListTRB[i].Disable;
            }
            //  Loadport
            for (int i = 0; i < ListSTG.Count; i++)
            {
                m_DicDoneOK[enumUnit.STG1 + i] = ListSTG[i].Disable;
            }

            #region Loadport
            m_guiloadportList.Add(guiLoadport1);
            m_guiloadportList.Add(guiLoadport2);
            m_guiloadportList.Add(guiLoadport3);
            m_guiloadportList.Add(guiLoadport4);
            m_guiloadportList.Add(guiLoadport5);
            m_guiloadportList.Add(guiLoadport6);
            m_guiloadportList.Add(guiLoadport7);
            m_guiloadportList.Add(guiLoadport8);

            for (int i = 0; i < ListSTG.Count; i++)
            {
                if (GParam.theInst.FreeStyle)
                    m_guiloadportList[i].SetFreeStyleColor(
                        GParam.theInst.ColorTitle,//橘
                        Color.FromArgb(97, 162, 79),//ready綠
                        Color.FromArgb(98, 186, 166),//wafer
                        Color.FromArgb(135, 206, 250),//select藍
                        Color.FromArgb(151, 218, 203)
                         );

                m_guiloadportList[i].Simulate = GParam.theInst.IsSimulate;
                m_guiloadportList[i].BodyNo = i + 1;
                m_guiloadportList[i].Disable_E84 = true /*GParam.theInst.IsE84Disable(i)*/;
                m_guiloadportList[i].Disable_OCR = true/*GParam.theInst.IsOcrAllDisable()*/;
                m_guiloadportList[i].Disable_Recipe = true /*(GParam.theInst.IsOcrAllDisable() && GParam.theInst.GetEQRecipeCanSelect == false)*/;
                m_guiloadportList[i].Disable_RSV = true;
                m_guiloadportList[i].Disable_ClmpLock = true;
                m_guiloadportList[i].Disable_DockBtn = false;


                m_guiloadportList[i].Disable_ProcessBtn = true;
                m_guiloadportList[i].Visible = !ListSTG[i].Disable;
                m_guiloadportList[i].Enabled = !ListSTG[i].Disable;
                m_guiloadportList[i].ShowSelectColor = false;
                if (ListSTG[i].Disable)
                {
                    continue;//不需要註冊
                }


                ListSTG[i].OnFoupExistChenge += Loadport_FoupExistChenge;          //  更新UI  
                ListSTG[i].OnClmpComplete += Loadport_MappingComplete;             //  更新UI
                ListSTG[i].OnMappingComplete += Loadport_MappingComplete;          //  更新UI
                ListSTG[i].OnStatusMachineChange += Loadport_StatusMachineChange;  //  更新UI
                ListSTG[i].OnFoupIDChange += Loadport_FoupIDChange;                //  更新UI
                ListSTG[i].OnFoupTypeChange += Loadport_FoupTypeChange;            //  更新UI

                //ListSTG[i].OnUclmComplete += Loadport_OnUclmComplete;              //  更新UI
                //ListE84[i].OnAceessModeChange += Loadport_E84ModeChange;                //  更新UI
                ListSTG[i].OnTakeWaferInFoup += m_guiloadportList[i].TakeWaferInFoup;//wafer被塞進來
                ListSTG[i].OnTakeWaferOutFoup += m_guiloadportList[i].TakeWaferOutFoup;//wafer被拿走

                //m_guiloadportList[i].BtnClamp += btnClamp_Click;
                m_guiloadportList[i].BtnDock += btnDock_Click;
                m_guiloadportList[i].BtnUnDock += btnUnDock_Click;
                //m_guiloadportList[i].BtnE84Mode += btnE84Mode_Click;
                //m_guiloadportList[i].ChkRecipeSelect += chkRecipeSelect_Checked;
                //m_guiloadportList[i].BtnProcess += btnProcess_Click;
                m_guiloadportList[i].ChkFoupOn += chkFoupOn_Checked;
                //m_guiloadportList[i].FoupIDKeyDownEnter += txtLoaderFoupID_Enter;
                //m_guiloadportList[i].LotIDKeyDownEnter += txtLoaderLotID_Enter;
                m_guiloadportList[i].UseSelectWafer += GuiLoadport_UseSelectWafer;//選片功能
            }

            tabPageABCD.Text = "";
            if (ListSTG[0].Disable == false) tabPageABCD.Text += "A";
            if (ListSTG[1].Disable == false) tabPageABCD.Text += "B";
            if (ListSTG[2].Disable == false) tabPageABCD.Text += "C";
            if (ListSTG[3].Disable == false) tabPageABCD.Text += "D";

            tabPageEFGH.Text = "";
            if (ListSTG[4].Disable == false) tabPageEFGH.Text += "E";
            if (ListSTG[5].Disable == false) tabPageEFGH.Text += "F";
            if (ListSTG[6].Disable == false) tabPageEFGH.Text += "G";
            if (ListSTG[7].Disable == false) tabPageEFGH.Text += "H";

            //只有一頁
            if (tabPageEFGH.Text == "")
            {
                //  消失頁籤
                tabPageEFGH.Parent = null;
                tabCtrlStage.SizeMode = TabSizeMode.Fixed;
                tabCtrlStage.ItemSize = new System.Drawing.Size(0, 1);
                int nCount = tabPageABCD.Text.Length;
                if (nCount <= 1)
                {
                    foreach (GUILoadport item in m_guiloadportList)
                    {
                        item.Width = (flowLayoutPanel4.Width - 0) / 2;
                        item.Height = flowLayoutPanel4.Height;
                    }
                }
                else
                {
                    foreach (GUILoadport item in m_guiloadportList)
                    {
                        item.Width = (flowLayoutPanel4.Width - 0) / nCount;
                        item.Height = flowLayoutPanel4.Height;
                    }
                }
            }
            else//有第二頁
            {
                tabPageEFGH.Parent = tabCtrlStage;
                foreach (GUILoadport item in m_guiloadportList)
                {
                    item.Width = (flowLayoutPanel4.Width - 0) / 4;
                    item.Height = flowLayoutPanel4.Height;
                }
            }

            #endregion

            ClearAllSelect();

            ShowMessageBox(rtbMessage, "Please place FOUP and Docking first.");
            ShowMessageBox(rtbMessage, "Manually select delivery.");

            if (GParam.theInst.FreeStyle)
            {
                btnOK.Image = RorzeApi.Properties.Resources._32_next_;
                btnExit.Image = RorzeApi.Properties.Resources._32_cancel_;
                this.Icon = RorzeApi.Properties.Resources.bwbs_;
            }
            else
            {
                this.Icon = RorzeApi.Properties.Resources.R;
            }

        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }


        //  Loadport UI Button
        private void btnDock_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;
            I_Loadport stg = ListSTG[Loader.BodyNo - 1];
            if (stg.Disable || stg.FoupExist == false) return;

            if (stg.StatusMachine == enumStateMachine.PS_Clamped ||
                stg.StatusMachine == enumStateMachine.PS_Arrived ||
                stg.StatusMachine == enumStateMachine.PS_UnDocked ||
                stg.StatusMachine == enumStateMachine.PS_ReadyToLoad ||
                stg.StatusMachine == enumStateMachine.PS_ReadyToUnload)
            {
                stg.CLMP();
            }
        }
        private void btnUnDock_Click(object sender, EventArgs e)
        {
            GUILoadport Loader = (GUILoadport)sender;
            I_Loadport stg = ListSTG[Loader.BodyNo - 1];
            if (stg.Disable || stg.FoupExist == false) return;

            if (stg.StatusMachine == enumStateMachine.PS_Docked ||
                stg.StatusMachine == enumStateMachine.PS_Stop ||
                stg.StatusMachine == enumStateMachine.PS_Complete)
            {
                stg.UCLM();
            }
        }
        private void chkFoupOn_Checked(object sender, EventArgs e)//Simulate
        {
            GUILoadport Loader = (GUILoadport)sender;
            try
            {
                ListSTG[Loader.BodyNo - 1].SimulateFoupOn(!ListSTG[Loader.BodyNo - 1].FoupExist);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        //  Loadport 註冊事件來更新 UI
        private void Loadport_FoupExistChenge(object sender, FoupExisteChangEventArgs e)
        {
            I_Loadport thenumUnit = sender as I_Loadport;
            if (thenumUnit.Disable) return;
            int index = thenumUnit.BodyNo - 1;
            m_guiloadportList[index].UpdataFoupExist(e.FoupExist);
        }
        private void Loadport_MappingComplete(object sender, LoadPortEventArgs e)
        {
            I_Loadport thenumUnit = sender as I_Loadport;
            if (thenumUnit.Disable) return;
            int index = thenumUnit.BodyNo - 1;
            string strMapData = e.MappingData;
            m_guiloadportList[index].UpdataMappingData(strMapData);
        }
        private void Loadport_StatusMachineChange(object sender, OccurStateMachineChangEventArgs e)
        {
            I_Loadport thenumUnit = sender as I_Loadport;
            if (thenumUnit.Disable) return;
            int index = thenumUnit.BodyNo - 1;
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
        private void Loadport_FoupIDChange(object sender, EventArgs e)
        {
            I_Loadport theLoadport = sender as I_Loadport;
            if (theLoadport.Disable) return;
            int nIndex = theLoadport.BodyNo - 1;
            m_guiloadportList[nIndex].FoupID = ListSTG[nIndex].FoupID;
        }
        private void Loadport_FoupTypeChange(object sender, string strName)
        {
            I_Loadport theLoadport = sender as I_Loadport;
            if (theLoadport.Disable) return;
            int nIndex = theLoadport.BodyNo - 1;
            m_guiloadportList[nIndex].InfoPadName = strName;
        }

        //  Next & Exit
        private void btnOk_Click(object sender, EventArgs e)
        {
            #region Interlock
            foreach (I_Robot item in ListTRB)
            {
                if (item == null || item.Disable) continue;

                if(item.UpperArmWafer != null)
                {
                    switch (item.UpperArmWafer.ToLoadport)
                    {
                        case SWafer.enumFromLoader.LoadportA: if (ListSTG[0].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportB: if (ListSTG[1].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportC: if (ListSTG[2].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportD: if (ListSTG[3].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportE: if (ListSTG[4].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportF: if (ListSTG[5].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportG: if (ListSTG[6].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportH: if (ListSTG[7].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    }
                }

                if (item.LowerArmWafer != null)
                {
                    switch (item.LowerArmWafer.ToLoadport)
                    {
                        case SWafer.enumFromLoader.LoadportA: if (ListSTG[0].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportB: if (ListSTG[1].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportC: if (ListSTG[2].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportD: if (ListSTG[3].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportE: if (ListSTG[4].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportF: if (ListSTG[5].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportG: if (ListSTG[6].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        case SWafer.enumFromLoader.LoadportH: if (ListSTG[7].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    }
                }
            }
         
            foreach (I_Aligner item in ListALN)
            {
                if (item == null || item.Disable || item.Wafer == null) continue;

                switch (item.Wafer.ToLoadport)
                {
                    case SWafer.enumFromLoader.LoadportA: if (ListSTG[0].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportB: if (ListSTG[1].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportC: if (ListSTG[2].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportD: if (ListSTG[3].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportE: if (ListSTG[4].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportF: if (ListSTG[5].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportG: if (ListSTG[6].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportH: if (ListSTG[7].StatusMachine != enumStateMachine.PS_Docked) return; break;
                }

            }

            foreach (I_Buffer item in ListBUF)
            {
                if (item.Disable) continue;

                for (int i = 0; i < item.HardwareSlot; i++)
                {
                    if (item.IsSlotDisable(i)) continue;
                    if (item.GetWafer(i) != null)
                    {
                        switch (item.GetWafer(i).ToLoadport)
                        {
                            case SWafer.enumFromLoader.LoadportA: if (ListSTG[0].StatusMachine != enumStateMachine.PS_Docked) return; break;
                            case SWafer.enumFromLoader.LoadportB: if (ListSTG[1].StatusMachine != enumStateMachine.PS_Docked) return; break;
                            case SWafer.enumFromLoader.LoadportC: if (ListSTG[2].StatusMachine != enumStateMachine.PS_Docked) return; break;
                            case SWafer.enumFromLoader.LoadportD: if (ListSTG[3].StatusMachine != enumStateMachine.PS_Docked) return; break;
                            case SWafer.enumFromLoader.LoadportE: if (ListSTG[4].StatusMachine != enumStateMachine.PS_Docked) return; break;
                            case SWafer.enumFromLoader.LoadportF: if (ListSTG[5].StatusMachine != enumStateMachine.PS_Docked) return; break;
                            case SWafer.enumFromLoader.LoadportG: if (ListSTG[6].StatusMachine != enumStateMachine.PS_Docked) return; break;
                            case SWafer.enumFromLoader.LoadportH: if (ListSTG[7].StatusMachine != enumStateMachine.PS_Docked) return; break;
                        }
                    }
                }
            }

            foreach (SSEquipment item in ListEQM)
            {
                if (item == null || item.Disable || item.Wafer == null) continue;
                switch (item.Wafer.ToLoadport)
                {
                    case SWafer.enumFromLoader.LoadportA: if (ListSTG[0].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportB: if (ListSTG[1].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportC: if (ListSTG[2].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportD: if (ListSTG[3].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportE: if (ListSTG[4].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportF: if (ListSTG[5].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportG: if (ListSTG[6].StatusMachine != enumStateMachine.PS_Docked) return; break;
                    case SWafer.enumFromLoader.LoadportH: if (ListSTG[7].StatusMachine != enumStateMachine.PS_Docked) return; break;
                }
            }

            if (m_QueSelectSlotNum.Count() > 0) { return; }

            #endregion

            btnOK.Enabled = false;
            btnExit.Enabled = false;
            tabCtrlStage.Enabled = false;
            btnUIPickWaferAllClear.Enabled = brnRecordSelection.Enabled = false;

            rtbMessage.Text = string.Empty;//清空
            ShowMessageBox(rtbMessage, "Wafer recovery start, please wait...");

            //收片回Loadport
            ListTRB[0].DoManualProcessing += (object Manual) =>
            {
                int nStgeIndx = -1;
                enumPosition ePos = enumPosition.UnKnow;
                I_Robot robotManual = Manual as I_Robot;

                if (robotManual.UpperArmWafer != null)
                {
                    SWafer waferData = robotManual.UpperArmWafer;

                    ePos = enumPosition.Loader1 + (int)(waferData.ToLoadport - enumFromLoader.LoadportA);

                    //robot stage index
                    nStgeIndx = robotManual.GetFromLoaderStagIndx(waferData.ToLoadport, waferData.ToSlot);
                    if (nStgeIndx == -1) return;
                    ShowMessageBox(rtbMessage, GParam.theInst.GetLanguage("wafer target:") + waferData.ToLoadport + " slot" + waferData.ToSlot);
                    //move standby
                    ShowMessageBox(rtbMessage, "Robot moving to standby position.");
                    robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, true, ePos, enumRobotArms.UpperArm, nStgeIndx, waferData.ToSlot);
                    //move robot 
                    ShowMessageBox(rtbMessage, "Robot upper arm place wafer.");
                    robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, enumRobotArms.UpperArm, ePos, nStgeIndx, waferData.ToSlot);
                }
                if (robotManual.LowerArmWafer != null)
                {
                    SWafer waferData = robotManual.LowerArmWafer;

                    ePos = enumPosition.Loader1 + (int)(waferData.ToLoadport - enumFromLoader.LoadportA);

                    //robot stage index
                    nStgeIndx = robotManual.GetFromLoaderStagIndx(waferData.ToLoadport, waferData.ToSlot);
                    if (nStgeIndx == -1) return;
                    ShowMessageBox(rtbMessage, GParam.theInst.GetLanguage("wafer target:") + waferData.ToLoadport + " slot" + waferData.ToSlot);
                    //move standby
                    ShowMessageBox(rtbMessage, "Robot moving to standby position.");
                    robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, true, ePos, enumRobotArms.LowerArm, nStgeIndx, waferData.ToSlot);
                    //move robot 
                    ShowMessageBox(rtbMessage, "Robot lower arm place wafer.");
                    robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, enumRobotArms.LowerArm, ePos, nStgeIndx, waferData.ToSlot);
                }

                for (int i = 0; i < ListALN.Count; i++)
                {
                    if (ListALN[i] == null || ListALN[i].Disable) continue;
                    if (ListALN[i].Wafer != null && robotManual.RobotHardwareAllow(ListALN[i].Wafer.ToLoadport))
                    {
                        SWafer waferData = ListALN[i].Wafer;
                        //robot stage index
                        nStgeIndx = robotManual.GetPositionStagIndx(waferData.Position, 1);//enumRbtAddress.ALN1
                        if (nStgeIndx == -1) return;

                        ShowMessageBox(rtbMessage, GParam.theInst.GetLanguage("wafer target:") + waferData.ToLoadport + " slot" + waferData.ToSlot);
                        //  Aligner VAC off
                        ShowMessageBox(rtbMessage, "Aligner unclamp!");
                        ListALN[i].ResetInPos();
                        ListALN[i].UclmW(3000);
                        ListALN[i].WaitInPos(30000);

                        //  Wafer Size 找用哪一隻手臂
                        enumRobotArms arm = enumRobotArms.Empty;
                        switch (waferData.WaferSize)
                        {
                            case enumWaferSize.Inch12:
                            case enumWaferSize.Inch08:
                                arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去LP取片
                                if (arm == enumRobotArms.Empty)
                                    arm = robotManual.GetAvailableArm(enumArmFunction.I);//找去LP取片
                                break;
                            case enumWaferSize.Frame:
                                arm = robotManual.GetAvailableArm(enumArmFunction.FRAME);//找去LP取片
                                break;
                            case enumWaferSize.Panel:
                                arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去LP取片
                                break;
                        }

                        switch (arm)    //過帳需要
                        {
                            case enumRobotArms.UpperArm: robotManual.PrepareUpperWafer = waferData; break;
                            case enumRobotArms.LowerArm: robotManual.PrepareLowerWafer = waferData; break;
                        }

                        if (arm != enumRobotArms.UpperArm && arm != enumRobotArms.LowerArm)
                        {
                            robotManual.TriggerSException(enumRobotError.InterlockStop);
                        }

                        //move standby
                        ShowMessageBox(rtbMessage, "Robot moving to standby position.");
                        robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, false, waferData.Position, arm, nStgeIndx, 1);
                        //move robot 
                        ShowMessageBox(rtbMessage, "Robot take wafer from aligner.");
                        robotManual.TakeWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, waferData.Position, nStgeIndx, 1);
                        //robot stage index
                        nStgeIndx = robotManual.GetPositionStagIndx(waferData.ToLoadport, waferData.ToSlot);
                        if (nStgeIndx == -1) return;
                        ePos = enumPosition.Loader1 + (int)(waferData.ToLoadport - enumFromLoader.LoadportA);
                        //move standby
                        ShowMessageBox(rtbMessage, "Robot moving to standby position.");
                        robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, true, ePos, arm, nStgeIndx, waferData.ToSlot);
                        //move robot
                        ShowMessageBox(rtbMessage, "Robot place wafer.");
                        robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, ePos, nStgeIndx, waferData.ToSlot);

                        waferData.ProcessStatus = SWafer.enumProcessStatus.Sleep;//畫面顯示
                    }
                }

                for (int i = 0; i < ListBUF.Count; i++)
                {
                    if (ListBUF[i].Disable) continue;
                    for (int j = 0; j < ListBUF[i].HardwareSlot; j++)//0,1,2,3
                    {
                        if (ListBUF[i].IsSlotDisable(j)) continue;
                        SWafer waferData = ListBUF[i].GetWafer(j);
                        if (waferData != null && robotManual.RobotHardwareAllow(waferData.ToLoadport))
                        {
                            //  Wafer Size 找用哪一隻手臂
                            enumRobotArms arm = enumRobotArms.Empty;
                            switch (waferData.WaferSize)
                            {
                                case enumWaferSize.Inch12:
                                case enumWaferSize.Inch08:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去LP取片
                                    if (arm == enumRobotArms.Empty)
                                        arm = robotManual.GetAvailableArm(enumArmFunction.I);//找去LP取片
                                    break;
                                case enumWaferSize.Frame:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.FRAME);//找去LP取片
                                    break;
                                case enumWaferSize.Panel:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);
                                    break;
                            }
                            if (arm != enumRobotArms.UpperArm && arm != enumRobotArms.LowerArm)
                            {
                                robotManual.TriggerSException(enumRobotError.InterlockStop);
                            }
                            //robot stage index
                            nStgeIndx = robotManual.GetPositionStagIndx(waferData.Position, j + 1);//enumRbtAddress.BUF1
                            if (nStgeIndx == -1) return;
                            //過帳需要
                            robotManual.PrepareUpperWafer = waferData;
                            ShowMessageBox(rtbMessage, GParam.theInst.GetLanguage("wafer target:") + waferData.ToLoadport + " slot" + waferData.ToSlot);
                            //move standby
                            ShowMessageBox(rtbMessage, "Robot moving to standby position.");
                            robotManual.MoveToStandbyByInterLockW(robotManual.GetAckTimeout, false, arm, nStgeIndx, j + 1);
                            //move Load 
                            ShowMessageBox(rtbMessage, "Robot upper arm take wafer from buffer.");
                            robotManual.TakeWaferByInterLockW(robotManual.GetAckTimeout, arm, nStgeIndx, j + 1);
                            //robot stage index
                            nStgeIndx = robotManual.GetPositionStagIndx(waferData.ToLoadport, waferData.ToSlot);
                            if (nStgeIndx == -1) return;
                            //move standby
                            ShowMessageBox(rtbMessage, "Robot moving to standby position.");
                            robotManual.MoveToStandbyByInterLockW(robotManual.GetAckTimeout, true, arm, nStgeIndx, waferData.ToSlot);
                            //move Load 
                            ShowMessageBox(rtbMessage, "Robot upper arm place wafer.");
                            robotManual.PutWaferByInterLockW(robotManual.GetAckTimeout, arm, nStgeIndx, waferData.ToSlot);

                            waferData.ProcessStatus = SWafer.enumProcessStatus.Sleep;//畫面顯示
                        }
                    }
                }

                //EQ
                for (int i = 0; i < ListEQM.Count; i++)
                {
                    if (ListEQM[i] == null || ListEQM[i].Disable) continue;
                    if (ListEQM[i].Wafer != null && robotManual.RobotHardwareAllow(ListEQM[i].Wafer.ToLoadport))
                    {
                        SWafer waferData = ListEQM[i].Wafer;
                        //robot stage index
                        nStgeIndx = robotManual.GetPositionStagIndx(waferData.Position, 1);//enumRbtAddress.ALN1
                        if (nStgeIndx == -1) return;

                        ShowMessageBox(rtbMessage, GParam.theInst.GetLanguage("wafer target:") + waferData.ToLoadport + " slot" + waferData.ToSlot);

                        //  Wafer Size 找用哪一隻手臂
                        enumRobotArms arm = enumRobotArms.Empty;
                        switch (waferData.WaferSize)
                        {
                            case enumWaferSize.Inch12:
                            case enumWaferSize.Inch08:
                                arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);//找去LP取片
                                if (arm == enumRobotArms.Empty)
                                    arm = robotManual.GetAvailableArm(enumArmFunction.I);//找去LP取片
                                break;
                            case enumWaferSize.Frame:
                                arm = robotManual.GetAvailableArm(enumArmFunction.FRAME);//找去LP取片
                                break;
                            case enumWaferSize.Panel:
                                arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);
                                break;
                        }

                        switch (arm)    //過帳需要
                        {
                            case enumRobotArms.UpperArm: robotManual.PrepareUpperWafer = waferData; break;
                            case enumRobotArms.LowerArm: robotManual.PrepareLowerWafer = waferData; break;
                        }

                        if (arm != enumRobotArms.UpperArm && arm != enumRobotArms.LowerArm)
                        {
                            robotManual.TriggerSException(enumRobotError.InterlockStop);
                        }

                        //move standby
                        ShowMessageBox(rtbMessage, "Robot moving to standby position.");
                        robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, false, waferData.Position, arm, nStgeIndx, 1);

                        //move robot 
                        ShowMessageBox(rtbMessage, "Robot take wafer from EQ.");
                        robotManual.TakeWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, waferData.Position, nStgeIndx, 1);
                        //robot stage index
                        nStgeIndx = robotManual.GetPositionStagIndx(waferData.ToLoadport, waferData.ToSlot);
                        if (nStgeIndx == -1) return;
                        ePos = enumPosition.Loader1 + (int)(waferData.ToLoadport - enumFromLoader.LoadportA);
                        //move standby
                        ShowMessageBox(rtbMessage, "Robot moving to standby position.");
                        robotManual.MoveToStandbyByInterLockW_ExtXaxis(robotManual.GetAckTimeout, true, ePos, arm, nStgeIndx, waferData.ToSlot);
                        //move robot 
                        ShowMessageBox(rtbMessage, "Robot place wafer.");
                        robotManual.PutWaferByInterLockW_ExtXaxis(robotManual.GetAckTimeout, arm, ePos, nStgeIndx, waferData.ToSlot);

                        waferData.ProcessStatus = SWafer.enumProcessStatus.Sleep;//畫面顯示
                    }

                }

                ListTRB[0].OnManualCompleted -= _robot_OnManualCompleted;
                ListTRB[0].OnManualCompleted += _robot_OnManualCompleted;
            };
            //片子放Buffer，而後再由RobotA回收
            ListTRB[1].DoManualProcessing += (object Manual) =>
            {
                int nStgeIndx = -1;
                I_Robot robotManual = Manual as I_Robot;
                if (robotManual.UpperArmWafer != null)
                {
                    SWafer waferData = robotManual.UpperArmWafer;
                    ShowMessageBox(rtbMessage, GParam.theInst.GetLanguage("wafer target:") + waferData.ToLoadport + " slot" + waferData.ToSlot);
                    for (int i = 0; i < ListBUF.Count; i++)
                    {
                        //判斷Buffer能用
                        if (ListBUF[i].Disable || robotManual.RobotHardwareAllow(enumPosition.BufferA + i) == false) continue;
                        int nBufferSlot = ListBUF[i].GetEmptySlot();
                        if (nBufferSlot <= 0) continue;
                        //robot stage index
                        nStgeIndx = robotManual.GetPositionStagIndx(enumPosition.BufferA + i, nBufferSlot);//enumRbtAddress.BUF1
                        if (nStgeIndx <= -1) return;
                        //move standby
                        ShowMessageBox(rtbMessage, "Robot moving to standby potion.");
                        robotManual.MoveToStandbyByInterLockW(robotManual.GetAckTimeout, false, enumRobotArms.UpperArm, nStgeIndx, nBufferSlot);
                        //move Unload 
                        ShowMessageBox(rtbMessage, "Robot upper arm place wafer.");
                        robotManual.PutWaferByInterLockW(robotManual.GetAckTimeout, enumRobotArms.UpperArm, nStgeIndx, nBufferSlot);
                        break;
                    }
                }
                if (robotManual.LowerArmWafer != null)
                {
                    SWafer waferData = robotManual.LowerArmWafer;
                    ShowMessageBox(rtbMessage, GParam.theInst.GetLanguage("wafer target:") + waferData.ToLoadport + " slot" + waferData.ToSlot);
                    for (int i = 0; i < ListBUF.Count; i++)
                    {
                        //判斷Buffer能用
                        if (ListBUF[i].Disable || robotManual.RobotHardwareAllow(enumPosition.BufferA + i) == false) continue;
                        int nBufferSlot = ListBUF[i].GetEmptySlot();
                        if (nBufferSlot <= 0) continue;
                        //robot stage index
                        nStgeIndx = robotManual.GetPositionStagIndx(enumPosition.BufferA + i, nBufferSlot);//enumRbtAddress.BUF1
                        if (nStgeIndx <= -1) return;
                        //move standby
                        ShowMessageBox(rtbMessage, "Robot moving to standby potion.");
                        robotManual.MoveToStandbyByInterLockW(robotManual.GetAckTimeout, false, enumRobotArms.LowerArm, nStgeIndx, nBufferSlot);
                        //move Unload 
                        ShowMessageBox(rtbMessage, "Robot lower arm place wafer.");
                        robotManual.PutWaferByInterLockW(robotManual.GetAckTimeout, enumRobotArms.LowerArm, nStgeIndx, nBufferSlot);
                        break;
                    }
                }
                //BUFFER需要TRB1回收
                for (int i = 0; i < ListBUF.Count; i++)
                {
                    if (ListBUF[i].Disable) continue;
                    for (int j = 0; j < ListBUF[i].HardwareSlot; j++)//0,1,2,3
                    {
                        if (ListBUF[i].IsSlotDisable(j)) continue;
                        SWafer waferData = ListBUF[i].GetWafer(j);
                        if (waferData != null && ListTRB[0].RobotHardwareAllow(waferData.ToLoadport))
                        {
                            //  Wafer Size 找用哪一隻手臂
                            enumRobotArms arm = enumRobotArms.Empty;
                            switch (waferData.WaferSize)
                            {
                                case enumWaferSize.Inch12:
                                case enumWaferSize.Inch08:
                                    arm = ListTRB[0].GetAvailableArm(enumArmFunction.NORMAL);//找去LP取片
                                    if (arm == enumRobotArms.Empty)
                                        arm = ListTRB[0].GetAvailableArm(enumArmFunction.I);//找去LP取片
                                    break;
                                case enumWaferSize.Frame:
                                    arm = ListTRB[0].GetAvailableArm(enumArmFunction.FRAME);//找去LP取片
                                    break;
                                case enumWaferSize.Panel:
                                    arm = robotManual.GetAvailableArm(enumArmFunction.NORMAL);
                                    break;
                            }
                            if (arm != enumRobotArms.UpperArm && arm != enumRobotArms.LowerArm)
                            {
                                ListTRB[0].TriggerSException(enumRobotError.InterlockStop);
                            }
                            //robot stage index
                            nStgeIndx = ListTRB[0].GetPositionStagIndx(waferData.Position, j + 1);//enumRbtAddress.BUF1
                            if (nStgeIndx == -1) return;

                            ShowMessageBox(rtbMessage, GParam.theInst.GetLanguage("wafer target:") + waferData.ToLoadport + " slot" + waferData.ToSlot);
                            //過帳需要
                            ListTRB[0].PrepareUpperWafer = waferData;
                            //move standby
                            ShowMessageBox(rtbMessage, "Robot moving to standby potion.");
                            ListTRB[0].MoveToStandbyByInterLockW(ListTRB[0].GetAckTimeout, false, arm, nStgeIndx, j + 1);
                            //move Load 
                            ShowMessageBox(rtbMessage, "Robot upper arm take wafer from buffer.");
                            ListTRB[0].TakeWaferByInterLockW(ListTRB[0].GetAckTimeout, arm, nStgeIndx, j + 1);
                            //robot stage index
                            nStgeIndx = ListTRB[0].GetPositionStagIndx(waferData.ToLoadport, waferData.ToSlot);
                            if (nStgeIndx == -1) return;
                            //move standby
                            ShowMessageBox(rtbMessage, "Robot moving to standby potion.");
                            ListTRB[0].MoveToStandbyByInterLockW(ListTRB[0].GetAckTimeout, true, arm, nStgeIndx, waferData.ToSlot);
                            //move unload 
                            ShowMessageBox(rtbMessage, "Robot upper arm place wafer.");
                            ListTRB[0].PutWaferByInterLockW(ListTRB[0].GetAckTimeout, arm, nStgeIndx, waferData.ToSlot);

                            waferData.ProcessStatus = SWafer.enumProcessStatus.Sleep;//畫面顯示
                        }
                    }
                }

                ListTRB[1].OnManualCompleted -= _robot_OnManualCompleted;
                ListTRB[1].OnManualCompleted += _robot_OnManualCompleted;
            };

            if (ListTRB[0].Disable == false) ListTRB[0].StartManualFunction();//RobotA先回收

        }
        private void btnExit_Click(object sender, EventArgs e)
        {

            for (int i = 0; i < ListSTG.Count; i++)
            {
                if (ListSTG[i].StatusMachine != enumStateMachine.PS_Docked &&
                    ListSTG[i].StatusMachine != enumStateMachine.PS_ReadyToLoad &&
                    ListSTG[i].StatusMachine != enumStateMachine.PS_Arrived &&
                    ListSTG[i].StatusMachine != enumStateMachine.PS_UnDocked &&
                    ListSTG[i].StatusMachine != enumStateMachine.PS_ReadyToUnload
                )
                {
                    new frmMessageBox("Please check loadport status.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
                else if (ListSTG[i].IsMoving)
                {
                    new frmMessageBox("The loaport is in moving.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning).ShowDialog();
                    return;
                }
            }


            Button btn = sender as Button;
            btn.Enabled = false;

            for (int i = 0; i < ListSTG.Count; i++)
            {
                ListSTG[i].OnFoupExistChenge -= Loadport_FoupExistChenge;          //  更新UI  
                ListSTG[i].OnClmpComplete -= Loadport_MappingComplete;             //  更新UI
                ListSTG[i].OnMappingComplete -= Loadport_MappingComplete;          //  更新UI
                ListSTG[i].OnStatusMachineChange -= Loadport_StatusMachineChange;  //  更新UI
                //ListSTG[i].OnAceessModeChange -= Loadport_E84ModeChange;                //  更新UI
                ListSTG[i].OnFoupIDChange -= Loadport_FoupIDChange;                //  更新UI
                ListSTG[i].OnFoupTypeChange -= Loadport_FoupTypeChange;            //  更新UI

                ListSTG[i].OnTakeWaferInFoup -= m_guiloadportList[i].TakeWaferInFoup;//wafer被塞進來
                ListSTG[i].OnTakeWaferOutFoup -= m_guiloadportList[i].TakeWaferOutFoup;//wafer被拿走
            }

            bool bDone = true;
            foreach (var item in m_DicDoneOK) bDone &= item.Value;

            //  使用者離開先回原點
            frmOrgn _frmOrgn = new frmOrgn(ListTRB, ListSTG, ListALN, ListBUF, ListEQM, GParam.theInst.IsSimulate);
            if (DialogResult.OK == _frmOrgn.ShowDialog() && bDone)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                new frmMessageBox("Robot Arm is not in a safe position and the system will shut down?", "Question", MessageBoxButtons.OK, MessageBoxIcon.Question).ShowDialog();
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                Environment.Exit(Environment.ExitCode);
            }

        }

        //  Robot
        private void _robot_OnManualCompleted(object sender, bool bSuc)
        {
            lock (this)
                try
                {
                    I_Robot theRobot = sender as I_Robot;
                    theRobot.OnManualCompleted -= _robot_OnManualCompleted;
                    m_DicDoneOK[enumUnit.TRB1 + theRobot.BodyNo - 1] = bSuc;
                    if (bSuc)
                    {
                        if (m_DicDoneOK[enumUnit.TRB1] == true && m_DicDoneOK[enumUnit.TRB2] == true)
                        {
                            ListTRB[0].ResetInPos();//2024.04.07
                            ListTRB[0].MoveToStandbyPosW(ListTRB[0].GetAckTimeout);//2024.04.07
                            ListTRB[0].WaitInPos(ListTRB[0].GetMotionTimeout);//2024.04.07
                            if (!ListTRB[0].ExtXaxisDisable)
                            {
                                ListTRB[0].TBL_560.ResetInPos();
                                RobPos pos = GParam.theInst.DicRobPos[enumPosition.HOME];
                                ListTRB[0].TBL_560.AxisMabsW(ListTRB[0].GetAckTimeout, m_eXAX1, pos.Pos_ARM1);
                                ListTRB[0].TBL_560.WaitInPos(ListTRB[0].GetMotionTimeout);
                            }

                            ShowMessageBox(rtbMessage, "The robot complete and loadport undocking.");
                            //  Loadport
                            for (int i = 0; i < ListSTG.Count; i++)
                            {
                                if (ListSTG[i].Disable == false && ListSTG[i].StatusMachine == enumStateMachine.PS_Docked)
                                {
                                    ListSTG[i].OnUclmComplete -= _loadport_OnUclmComplete;
                                    ListSTG[i].OnUclmComplete += _loadport_OnUclmComplete;
                                    WriteLog(string.Format("Loadport{0} undocking.", i + 1));
                                    ListSTG[i].UCLM();
                                }
                                else
                                {
                                    m_DicDoneOK[enumUnit.STG1 + ListSTG[i].BodyNo - 1] = true;
                                }
                            }
                        }
                        else if (m_DicDoneOK[enumUnit.TRB1] == false && m_DicDoneOK[enumUnit.TRB2] == true)
                        {
                            ShowMessageBox(rtbMessage, "RobotB recover complete and watting for Robot_A");
                        }
                        else if (m_DicDoneOK[enumUnit.TRB1] == true && m_DicDoneOK[enumUnit.TRB2] == false)
                        {
                            ShowMessageBox(rtbMessage, "RobotA recover complete and watting for Robot_B");
                            if (ListTRB[1].Disable == false) ListTRB[1].StartManualFunction();//RobotB後回收
                        }
                    }
                    else
                    {
                        if (m_DicDoneOK[enumUnit.TRB1] == false && m_DicDoneOK[enumUnit.TRB2] == true)
                        {
                            ShowMessageBox(rtbMessage, "RobotA recover fail and click [Exit] the system will shut down!!");
                        }
                        else if (m_DicDoneOK[enumUnit.TRB1] == true && m_DicDoneOK[enumUnit.TRB2] == false)
                        {
                            ShowMessageBox(rtbMessage, "RobotB recover fail and click [Exit] the system will shut down!!");
                        }
                        else
                        {
                            ShowMessageBox(rtbMessage, "Recover fail and click [Exit] the system will shut down!");
                        }
                        btnExit.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("[Exception]" + ex);
                }
        }
        //  Loadport
        private void _loadport_OnUclmComplete(object sender, LoadPortEventArgs e)
        {

            I_Loadport lp = sender as I_Loadport;
            lp.OnUclmComplete -= _loadport_OnUclmComplete;
            int nIndex = lp.BodyNo - 1;

            if (lp.Disable) return;
            if (!m_DicDoneOK[enumUnit.TRB1] || !m_DicDoneOK[enumUnit.TRB2]) return;

            lock (this)
            {
                WriteLog(string.Format("Loadport{0} undocking Success:{1}.", lp.BodyNo, e.Succeed));
                m_DicDoneOK[enumUnit.STG1 + nIndex] = e.Succeed;

                bool bFinish = true;
                foreach (var item in m_DicDoneOK)
                    bFinish &= item.Value;

                if (bFinish)
                {
                    ShowMessageBox(rtbMessage, "Recover completes the end of process!");
                    Action act = () =>
                    {
                        btnExit.Enabled = true;
                        btnExit.PerformClick();
                    };
                    Invoke(act);
                }
            }
        }

        private void frmWaferRecover_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible)
                {
                    foreach (I_Robot item in ListTRB)
                    {
                        if (item == null || item.Disable) continue;

                        item.ResetProcessCompleted();
                        item.SspdW(3000, 4);//test 20%
                        item.WaitProcessCompleted(3000);

                        item.RSTA(1);
                    }

                    foreach (GUILoadport item in m_guiloadportList)//更新grouprecipe list
                    {

                        int nIndex = item.BodyNo - 1;
                        if (ListSTG[nIndex].Disable == false)
                        {
                            //Loadport_E84ModeChange(ListE84[nIndex],
                            //    new E84ModeChangeEventArgs(m_guiloadportList[nIndex].E84Status == GUILoadport.enumE84Status.Auto));
                            Loadport_StatusMachineChange(ListSTG[nIndex],
                                new OccurStateMachineChangEventArgs(ListSTG[nIndex].StatusMachine));
                            Loadport_FoupExistChenge(ListSTG[nIndex],
                                new FoupExisteChangEventArgs(ListSTG[nIndex].FoupExist));
                            Loadport_FoupIDChange(ListSTG[nIndex], new EventArgs());     //初始值
                            Loadport_FoupTypeChange(ListSTG[nIndex], ListSTG[nIndex].FoupTypeName);   //初始值

                        }
                    }
                }
                else
                {

                }

                timer1.Enabled = this.Visible;
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            try
            {
                SWafer waferShow;
                //
                #region ==========Robot
                if (ListTRB[0] != null)
                {
                    //下arm
                    waferShow = ListTRB[0].LowerArmWafer;
                    if (waferShow != null)
                    {
                        //lower料帳資訊
                        //lblTRB1_LowerSlotNo.Text = string.Format("{0} ({1}\")", waferShow.Slot, waferShow.WaferSize);
                        lblTRB1_LowerSlotNo.Text = string.Format("{0}", waferShow.ToSlot, waferShow.WaferSize);
                        lblTRB1_LowerOwner.Text = waferShow.ToLoadport.ToString();
                        pbxTRB1_LowerWafer.Visible = true;
                    }
                    else
                    {
                        //lower料帳資訊
                        lblTRB1_LowerSlotNo.Text = "-";
                        lblTRB1_LowerOwner.Text = "-";
                        pbxTRB1_LowerWafer.Visible = false;
                    }

                    //上arm
                    waferShow = ListTRB[0].UpperArmWafer;
                    if (waferShow != null)
                    {
                        //upper料帳資訊       
                        lblTRB1_UpperSlotNo.Text = string.Format("{0}", waferShow.ToSlot, waferShow.WaferSize);
                        lblTRB1_UpperOwner.Text = waferShow.ToLoadport.ToString();
                        pbxTRB1_UpperWafer.Visible = true;
                    }
                    else
                    {
                        //upper料帳資訊
                        lblTRB1_UpperSlotNo.Text = "-";
                        lblTRB1_UpperOwner.Text = "-";
                        pbxTRB1_UpperWafer.Visible = false;
                    }
                }

                if (ListTRB[1] != null)
                {
                    //下arm
                    waferShow = ListTRB[1].LowerArmWafer;
                    if (waferShow != null)
                    {
                        //lower料帳資訊
                        //lblTRB2_LowerSlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                        lblTRB2_LowerSlotNo.Text = string.Format("{0}", waferShow.ToSlot, waferShow.WaferSize);
                        lblTRB2_LowerOwner.Text = waferShow.ToLoadport.ToString();
                        pbxTRB2_LowerWafer.Visible = true;
                    }
                    else
                    {
                        //lower料帳資訊
                        lblTRB2_LowerSlotNo.Text = "-";
                        lblTRB2_LowerOwner.Text = "-";
                        pbxTRB2_LowerWafer.Visible = false;
                    }

                    //上arm
                    waferShow = ListTRB[1].UpperArmWafer;
                    if (waferShow != null)
                    {
                        //upper料帳資訊
                        lblTRB2_UpperSlotNo.Text = string.Format("{0}", waferShow.ToSlot, waferShow.WaferSize);
                        lblTRB2_UpperOwner.Text = waferShow.ToLoadport.ToString();
                        pbxTRB2_UpperWafer.Visible = true;
                    }
                    else
                    {
                        //upper料帳資訊
                        lblTRB2_UpperSlotNo.Text = "-";
                        lblTRB2_UpperOwner.Text = "-";
                        pbxTRB2_UpperWafer.Visible = false;
                    }
                }
                #endregion
                //
                #region ==========Aligner
                if (ListALN[0] != null)
                {
                    waferShow = ListALN[0].Wafer;
                    if (waferShow != null)
                    {
                        lblALN1_SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                        lblALN1_Owner.Text = waferShow.ToLoadport.ToString();
                        pbxALN1_Wafer.Visible = true;
                    }
                    else
                    {
                        lblALN1_SlotNo.Text = "-";
                        lblALN1_Owner.Text = "-";
                        pbxALN1_Wafer.Visible = false;
                    }
                }
                if (ListALN[1] != null)
                {
                    waferShow = ListALN[1].Wafer;
                    if (waferShow != null)
                    {
                        lblALN2_SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                        lblALN2_Owner.Text = waferShow.ToLoadport.ToString();
                        pbxALN2_Wafer.Visible = true;
                    }
                    else
                    {
                        lblALN2_SlotNo.Text = "-";
                        lblALN2_Owner.Text = "-";
                        pbxALN2_Wafer.Visible = false;
                    }
                }
                #endregion
                //             
                #region ==========Loadport
                for (int nPort = 0; nPort < ListSTG.Count; nPort++)
                {
                    if (ListSTG[nPort].Disable) continue;

                    m_guiloadportList[nPort].KeepClamp = ListSTG[nPort].IsKeepClamp;

                    if (ListSTG[nPort].Waferlist == null) continue;

                    for (int nSlot = 1; nSlot <= ListSTG[nPort].Waferlist.Count; nSlot++)
                    {
                        /*SWafer*/
                        waferShow = ListSTG[nPort].Waferlist[nSlot - 1];

                        if (waferShow == null)//empty
                        {
                            m_guiloadportList[nPort].UpdataWaferStatus(nSlot, "");
                            m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.None, System.Drawing.SystemColors.Control);
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
                                    m_guiloadportList[nPort].UpdataWaferProcessStatus(nSlot, SWafer.enumProcessStatus.Processing, System.Drawing.SystemColors.Control);
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

                            m_guiloadportList[nPort].UpdataWaferStatus(nSlot, waferShow.WaferID_F, waferShow.WaferID_B, waferShow.WaferInforID_F, waferShow.WaferInforID_B, waferShow.Position);
                        }

                    }
                }
                #endregion              
                //
                #region ==========Buffer
                if (ListBUF[0].Disable == false)
                {
                    if (ListBUF[0].HardwareSlot > 0 && ListBUF[0].IsSlotDisable(0) == false)
                    {
                        waferShow = ListBUF[0].GetWafer(0);
                        if (waferShow != null)
                        {
                            lblBUF1_1SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                            lblBUF1_1Owner.Text = waferShow.ToLoadport.ToString();
                            pbxBUF1_1Wafer.Visible = true;
                        }
                        else
                        {
                            lblBUF1_1SlotNo.Text = "-";
                            lblBUF1_1Owner.Text = "-";
                            pbxBUF1_1Wafer.Visible = false;
                        }
                    }
                    if (ListBUF[0].HardwareSlot > 1 && ListBUF[0].IsSlotDisable(1) == false)
                    {
                        waferShow = ListBUF[0].GetWafer(1);
                        if (waferShow != null)
                        {
                            lblBUF1_2SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                            lblBUF1_2Owner.Text = waferShow.ToLoadport.ToString();
                            pbxBUF1_2Wafer.Visible = true;
                        }
                        else
                        {
                            lblBUF1_2SlotNo.Text = "-";
                            lblBUF1_2Owner.Text = "-";
                            pbxBUF1_2Wafer.Visible = false;
                        }
                    }
                    if (ListBUF[0].HardwareSlot > 2 && ListBUF[0].IsSlotDisable(2) == false)
                    {
                        waferShow = ListBUF[0].GetWafer(2);
                        if (waferShow != null)
                        {
                            lblBUF1_3SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                            lblBUF1_3Owner.Text = waferShow.ToLoadport.ToString();
                            pbxBUF1_3Wafer.Visible = true;
                        }
                        else
                        {
                            lblBUF1_3SlotNo.Text = "-";
                            lblBUF1_3Owner.Text = "-";
                            pbxBUF1_3Wafer.Visible = false;
                        }
                    }
                    if (ListBUF[0].HardwareSlot > 3 && ListBUF[0].IsSlotDisable(3) == false)
                    {
                        waferShow = ListBUF[0].GetWafer(3);
                        if (waferShow != null)
                        {
                            lblBUF1_4SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                            lblBUF1_4Owner.Text = waferShow.ToLoadport.ToString();
                            pbxBUF1_4Wafer.Visible = true;
                        }
                        else
                        {
                            lblBUF1_4SlotNo.Text = "-";
                            lblBUF1_4Owner.Text = "-";
                            pbxBUF1_4Wafer.Visible = false;
                        }
                    }
                }
                if (ListBUF[1].Disable == false)
                {
                    if (ListBUF[1].HardwareSlot > 0 && ListBUF[1].IsSlotDisable(0) == false)
                    {
                        waferShow = ListBUF[1].GetWafer(0);
                        if (waferShow != null)
                        {
                            lblBUF2_1SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                            lblBUF2_1Owner.Text = waferShow.ToLoadport.ToString();
                            pbxBUF2_1Wafer.Visible = true;
                        }
                        else
                        {
                            lblBUF2_1SlotNo.Text = "-";
                            lblBUF2_1Owner.Text = "-";
                            pbxBUF2_1Wafer.Visible = false;
                        }
                    }
                    if (ListBUF[1].HardwareSlot > 1 && ListBUF[1].IsSlotDisable(1) == false)
                    {
                        waferShow = ListBUF[1].GetWafer(1);
                        if (waferShow != null)
                        {
                            lblBUF2_2SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                            lblBUF2_2Owner.Text = waferShow.ToLoadport.ToString();
                            pbxBUF2_2Wafer.Visible = true;
                        }
                        else
                        {
                            lblBUF2_2SlotNo.Text = "-";
                            lblBUF2_2Owner.Text = "-";
                            pbxBUF2_2Wafer.Visible = false;
                        }
                    }
                    if (ListBUF[1].HardwareSlot > 2 && ListBUF[1].IsSlotDisable(2) == false)
                    {
                        waferShow = ListBUF[1].GetWafer(2);
                        if (waferShow != null)
                        {
                            lblBUF2_3SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                            lblBUF2_3Owner.Text = waferShow.ToLoadport.ToString();
                            pbxBUF2_3Wafer.Visible = true;
                        }
                        else
                        {
                            lblBUF2_3SlotNo.Text = "-";
                            lblBUF2_3Owner.Text = "-";
                            pbxBUF2_3Wafer.Visible = false;
                        }
                    }
                    if (ListBUF[1].HardwareSlot > 3 && ListBUF[1].IsSlotDisable(3) == false)
                    {
                        waferShow = ListBUF[1].GetWafer(3);
                        if (waferShow != null)
                        {
                            lblBUF2_4SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                            lblBUF2_4Owner.Text = waferShow.ToLoadport.ToString();
                            pbxBUF2_4Wafer.Visible = true;
                        }
                        else
                        {
                            lblBUF2_4SlotNo.Text = "-";
                            lblBUF2_4Owner.Text = "-";
                            pbxBUF2_4Wafer.Visible = false;
                        }
                    }
                }

                #endregion
                //

                #region ==========EQ
                if (ListEQM[0] != null)
                {
                    waferShow = ListEQM[0].Wafer;
                    if (waferShow != null)
                    {
                        lblEQ1SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                        lblEQ1Owner.Text = waferShow.ToLoadport.ToString();
                        pbxEQ1Wafer.Visible = true;
                    }
                    else
                    {
                        lblEQ1SlotNo.Text = "-";
                        lblEQ1Owner.Text = "-";
                        pbxEQ1Wafer.Visible = false;
                    }
                }

                if (ListEQM[1] != null)
                {
                    waferShow = ListEQM[1].Wafer;
                    if (waferShow != null)
                    {
                        lblEQ2SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                        lblEQ2Owner.Text = waferShow.ToLoadport.ToString();
                        pbxEQ2Wafer.Visible = true;
                    }
                    else
                    {
                        lblEQ2SlotNo.Text = "-";
                        lblEQ2Owner.Text = "-";
                        pbxEQ2Wafer.Visible = false;
                    }
                }

                if (ListEQM[2] != null)
                {
                    waferShow = ListEQM[2].Wafer;
                    if (waferShow != null)
                    {
                        lblEQ3SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                        lblEQ3Owner.Text = waferShow.ToLoadport.ToString();
                        pbxEQ3Wafer.Visible = true;
                    }
                    else
                    {
                        lblEQ3SlotNo.Text = "-";
                        lblEQ3Owner.Text = "-";
                        pbxEQ3Wafer.Visible = false;
                    }
                }

                if (ListEQM[3] != null)
                {
                    waferShow = ListEQM[3].Wafer;
                    if (waferShow != null)
                    {
                        lblEQ4SlotNo.Text = string.Format("{0} ({1}\")", waferShow.ToSlot, waferShow.WaferSize);
                        lblEQ4Owner.Text = waferShow.ToLoadport.ToString();
                        pbxEQ4Wafer.Visible = true;
                    }
                    else
                    {
                        lblEQ4SlotNo.Text = "-";
                        lblEQ4Owner.Text = "-";
                        pbxEQ4Wafer.Visible = false;
                    }
                }

                #endregion

                timer1.Enabled = true;
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }


        private delegate void ShowTexBox(RichTextBox MsgBox, string str);
        public void ShowMessageBox(RichTextBox MsgBox, string str)
        {


            if (InvokeRequired)
            {
                ShowTexBox dlg = new ShowTexBox(ShowMessageBox);
                this.BeginInvoke(dlg, MsgBox, str);
            }
            else
            {
                MsgBox.Text += GParam.theInst.GetLanguage(str) + "\r\n";
            }
        }

        #region 選片機制

        class clsSelectWaferInfo
        {
            private int m_nSourceLpBodyNo = -1;
            private int m_nTargetLpBodyNo = -1;
            private int m_nSourceSlotIdx = -1;
            private int m_nTargetSlotIdx = -1;
            private enumUnit m_eSourcenumUnit;
            public clsSelectWaferInfo(enumUnit _enumUnit, int lpBodyNo, int sourceSlotIdx, int targetSlotIdx = -1)
            {
                m_eSourcenumUnit = _enumUnit;
                m_nSourceLpBodyNo = lpBodyNo;
                m_nSourceSlotIdx = sourceSlotIdx;
                m_nTargetSlotIdx = targetSlotIdx;
            }
            public void SetTargetSlotIdx(int nIndex)
            {
                m_nTargetSlotIdx = nIndex;
                m_nSourceSlotIdx = nIndex;//RECOVER也不知道初始位置因此判斷與目標位置向同
            }
            public void SetTargetLpBodyNo(int nBodyNo)
            {
                m_nTargetLpBodyNo = nBodyNo;
                m_nSourceLpBodyNo = nBodyNo;//RECOVER也不知道初始位置因此判斷與目標位置向同
            }
            public int SourceLpBodyNo { get { return m_nSourceLpBodyNo; } }
            public int TargetLpBodyNo { get { return m_nTargetLpBodyNo; } }
            public int SourceSlotIdx { get { return m_nSourceSlotIdx; } }
            public int TargetSlotIdx { get { return m_nTargetSlotIdx; } }
            public enumUnit SourcenumUnit { get { return m_eSourcenumUnit; } }
        }
        ConcurrentQueue<clsSelectWaferInfo> m_QueSelectSlotNum = new ConcurrentQueue<clsSelectWaferInfo>();//只有選Wafer，不確定去哪裡
        ConcurrentQueue<clsSelectWaferInfo> m_QueWaferJob = new ConcurrentQueue<clsSelectWaferInfo>();//紀錄Wafer傳片紀錄，clear的時候清掉      
        //使用者選擇Wafer判斷邏輯
        private void GuiLoadport_UseSelectWafer(object sender, GUILoadport.EventArgs_SelectWafer e)//GUI MouseUp
        {
            try
            {
                GUILoadport guiLoadport = sender as GUILoadport;

                if (e.SelectSlotNum.Count() > 0)
                {
                    if (e.SelectSlotSts[0] == enumUIPickWaferStat.NoWafer)
                    {
                        if (m_QueSelectSlotNum.Count() > 0)
                        {
                            for (int i = 0; i < e.SelectSlotSts.Count(); i++)
                            {
                                clsSelectWaferInfo temp = null;
                                if (m_QueSelectSlotNum.TryPeek(out temp) == false) { return; }

                                if (temp.SourcenumUnit == enumUnit.TRB1)
                                {
                                    if (GParam.theInst.GetRobot_AllowPort(0)[guiLoadport.BodyNo - 1] != '1')
                                    {
                                        new frmMessageBox("This location is not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }

                                    //上arm = Wafer
                                    if (ListTRB[0].UpperArmWafer != null && ListTRB[0].UpperArmWafer.ToSlot == 0)
                                    {
                                        if (ListTRB[0].UpperArmFunc == enumArmFunction.NORMAL && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                                        {
                                            new frmMessageBox("The upper arm wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                            m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                            return;
                                        }
                                    }
                                    //下arm = Frame
                                    else if (ListTRB[0].LowerArmWafer != null && ListTRB[0].LowerArmWafer.ToSlot == 0)
                                    {
                                        if (ListTRB[0].LowerArmFunc == enumArmFunction.FRAME && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                        {
                                            new frmMessageBox("The lower arm wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                            m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                            return;
                                        }
                                    }
                                }
                                if (temp.SourcenumUnit == enumUnit.TRB2)
                                {
                                    /*針對2的Robot，TRB2只能回收C/Dport，但BWS不能回tower，一定要回Loadport
                                    if (GParam.theInst.GetRobot_AllowPort(1)[guiLoadport.BodyNo - 1] != '1')
                                    {
                                        new frmMessageBox("This location is not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }*/
                                }

                                if (temp.SourcenumUnit == enumUnit.ALN1) //Wafer
                                {
                                    if (GParam.theInst.GetAlignerMode(0) != enumAlignerType.TurnTable && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("Aligner1 wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                    else if (GParam.theInst.GetAlignerMode(0) == enumAlignerType.TurnTable && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("Aligner1 wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                }

                                if (temp.SourcenumUnit == enumUnit.ALN2) //Frame
                                {
                                    if (GParam.theInst.GetAlignerMode(1) != enumAlignerType.TurnTable && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("Aligner2 wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                    else if (GParam.theInst.GetAlignerMode(1) == enumAlignerType.TurnTable && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("Aligner2 wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                }

                                if (temp.SourcenumUnit == enumUnit.EQM1)
                                {
                                    if (ListEQM[0].Wafer.WaferSize == enumWaferSize.Inch12 && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("EQ wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                    else if (ListEQM[0].Wafer.WaferSize == enumWaferSize.Frame && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("EQ wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                }

                                if (temp.SourcenumUnit == enumUnit.EQM2)
                                {
                                    if (ListEQM[1].Wafer.WaferSize == enumWaferSize.Inch12 && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("EQ wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                    else if (ListEQM[1].Wafer.WaferSize == enumWaferSize.Frame && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("EQ wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                }

                                if (temp.SourcenumUnit == enumUnit.EQM3)
                                {
                                    if (ListEQM[2].Wafer.WaferSize == enumWaferSize.Inch12 && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() == SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("EQ wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                    else if (ListEQM[2].Wafer.WaferSize == enumWaferSize.Frame && ListSTG[guiLoadport.BodyNo - 1].GetCurrentLoadportWaferType() != SWafer.enumWaferSize.Frame)
                                    {
                                        new frmMessageBox("EQ wafer size is different from the target loadport..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error).ShowDialog();
                                        m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                        return;
                                    }
                                }

                                if (m_QueSelectSlotNum.TryDequeue(out temp))
                                {
                                    temp.SetTargetSlotIdx(e.SelectSlotNum[i]);//確認要送去哪個slot
                                    temp.SetTargetLpBodyNo(guiLoadport.BodyNo);//確認要送去哪個LP

                                    m_QueWaferJob.Enqueue(temp);

                                    string strFromName = (char)(64 + temp.SourceLpBodyNo) + (temp.SourceSlotIdx + 1).ToString("D2");
                                    m_guiloadportList[guiLoadport.BodyNo - 1].UserSelectPlaceWaferInLoadport(strFromName, temp.TargetSlotIdx);

                                    CheckSelect(temp, guiLoadport.BodyNo, e.SelectSlotNum[i] + 1);//ming
                                }
                                else
                                {
                                    m_guiloadportList[guiLoadport.BodyNo - 1].ResetSlotSelectFlag("", e.SelectSlotNum[i]);
                                }
                            }
                            if (m_QueSelectSlotNum.Count() == 0)
                            {
                                foreach (var lp in m_guiloadportList)
                                {
                                    lp.EnableUISelectPutWaferFlag(false);
                                }
                            }

                        }
                        else
                        {
                            foreach (var lp in m_guiloadportList)
                            {
                                lp.EnableUISelectPutWaferFlag(false);
                            }
                        }
                        CheckWaferOwner();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }

        }
        //判斷所有片都選好
        private void CheckWaferOwner()
        {
            bool bCheck = true;
            if (ListTRB[0] != null)
                if (ListTRB[0].UpperArmWafer != null && ListTRB[0].UpperArmWafer.ToLoadport == SWafer.enumFromLoader.UnKnow)
                {
                    bCheck = false;
                }
            if (ListTRB[0] != null)
                if (ListTRB[0].LowerArmWafer != null && ListTRB[0].LowerArmWafer.ToLoadport == SWafer.enumFromLoader.UnKnow)
                {
                    bCheck = false;
                }
            if (ListTRB[1] != null)
                if (ListTRB[1].UpperArmWafer != null && ListTRB[1].UpperArmWafer.ToLoadport == SWafer.enumFromLoader.UnKnow)
                {
                    bCheck = false;
                }
            if (ListTRB[1] != null)
                if (ListTRB[1].LowerArmWafer != null && ListTRB[1].LowerArmWafer.ToLoadport == SWafer.enumFromLoader.UnKnow)
                {
                    bCheck = false;
                }
            if (ListALN[0] != null)
                if (ListALN[0].Wafer != null && ListALN[0].Wafer.ToLoadport == SWafer.enumFromLoader.UnKnow)
                {
                    bCheck = false;
                }
            if (ListALN[1] != null)
                if (ListALN[1].Wafer != null && ListALN[1].Wafer.ToLoadport == SWafer.enumFromLoader.UnKnow)
                {
                    bCheck = false;
                }

            if (ListBUF[0].GetWafer(0) != null && ListBUF[0].GetWafer(0).ToLoadport == SWafer.enumFromLoader.UnKnow)
            {
                bCheck = false;
            }

            if (ListBUF[0].GetWafer(1) != null && ListBUF[0].GetWafer(1).ToLoadport == SWafer.enumFromLoader.UnKnow)
            {
                bCheck = false;
            }

            if (ListBUF[1].GetWafer(0) != null && ListBUF[1].GetWafer(0).ToLoadport == SWafer.enumFromLoader.UnKnow)
            {
                bCheck = false;
            }

            if (ListBUF[1].GetWafer(1) != null && ListBUF[1].GetWafer(1).ToLoadport == SWafer.enumFromLoader.UnKnow)
            {
                bCheck = false;
            }

            btnOK.Enabled = bCheck;
        }
        //判斷使用者選片
        private void CheckSelect(clsSelectWaferInfo temp, int loadportBodyNo, int loadportslot)
        {
            try
            {
                SWafer wafer = null;
                switch (temp.SourcenumUnit)
                {
                    case enumUnit.TRB1:
                        if (ListTRB[0].UpperArmWafer != null && ListTRB[0].UpperArmWafer.ToSlot == 0)
                        {
                            wafer = ListTRB[0].UpperArmWafer;
                        }
                        else if (ListTRB[0].LowerArmWafer != null && ListTRB[0].LowerArmWafer.ToSlot == 0)
                        {
                            wafer = ListTRB[0].LowerArmWafer;
                        }
                        break;
                    case enumUnit.TRB2:
                        if (ListTRB[1].UpperArmWafer != null && ListTRB[1].UpperArmWafer.ToSlot == 0)
                        {
                            wafer = ListTRB[1].UpperArmWafer;
                        }
                        else if (ListTRB[1].LowerArmWafer != null && ListTRB[1].LowerArmWafer.ToSlot == 0)
                        {
                            wafer = ListTRB[1].LowerArmWafer;
                        }
                        break;
                    case enumUnit.ALN1:
                        if (ListALN[0].Wafer != null)
                        {
                            wafer = ListALN[0].Wafer;
                        }
                        break;
                    case enumUnit.ALN2:
                        if (ListALN[1].Wafer != null)
                        {
                            wafer = ListALN[1].Wafer;
                        }
                        break;
                    case enumUnit.BUF1:
                        if (ListBUF[0].Disable == false)
                        {
                            for (int i = 0; i < ListBUF[0].HardwareSlot; i++)
                            {
                                if (ListBUF[0].IsSlotDisable(i)) continue;
                                if (ListBUF[0].GetWafer(i) == null) continue;
                                if (ListBUF[0].GetWafer(i).ToLoadport != enumFromLoader.UnKnow) continue;
                                wafer = ListBUF[0].GetWafer(i);
                                break;
                            }
                        }
                        break;
                    case enumUnit.BUF2:
                        if (ListBUF[1].Disable == false)
                        {
                            for (int i = 0; i < ListBUF[1].HardwareSlot; i++)
                            {
                                if (ListBUF[1].IsSlotDisable(i)) continue;
                                if (ListBUF[1].GetWafer(i) == null) continue;
                                if (ListBUF[1].GetWafer(i).ToLoadport != enumFromLoader.UnKnow) continue;
                                wafer = ListBUF[1].GetWafer(i);
                                break;
                            }
                        }
                        break;
                    case enumUnit.EQM1:
                        if (ListEQM[0].Wafer != null)
                        {
                            wafer = ListEQM[0].Wafer;
                        }
                        break;
                    case enumUnit.EQM2:
                        if (ListEQM[1].Wafer != null)
                        {
                            wafer = ListEQM[1].Wafer;
                        }
                        break;
                    case enumUnit.EQM3:
                        if (ListEQM[2].Wafer != null)
                        {
                            wafer = ListEQM[2].Wafer;
                        }
                        break;
                }

                if (wafer != null)
                {
                    //wafer.SetOwner(SWafer.enumFromLoader.LoadportA + loadportBodyNo - 1);
                    //wafer.SetSlot(loadportslot);
                    wafer.ToLoadport = SWafer.enumFromLoader.LoadportA + loadportBodyNo - 1;
                    wafer.ToSlot = loadportslot;
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
        //清除使用者選的片
        private void ClearAllSelect()
        {
            try
            {
                //
                for (int i = 0; i < ListSTG.Count; i++)
                {
                    if (ListSTG[i].StatusMachine == enumStateMachine.PS_Process) continue;
                    m_guiloadportList[i].ResetUpdateMappingData();
                }
                //
                while (true)
                {
                    clsSelectWaferInfo temp;
                    if (m_QueWaferJob.Count() > 0) m_QueWaferJob.TryDequeue(out temp);
                    else break;
                }
                //
                while (true)
                {
                    clsSelectWaferInfo temp;
                    if (m_QueSelectSlotNum.Count() > 0) m_QueSelectSlotNum.TryDequeue(out temp);
                    else break;
                }
                //              
                {
                    if (ListTRB[0] != null && ListTRB[0].UpperArmWafer != null)
                    {
                        ListTRB[0].UpperArmWafer.ToLoadport = SWafer.enumFromLoader.UnKnow;
                        ListTRB[0].UpperArmWafer.ToSlot = 0;

                        clsSelectWaferInfo temp = new clsSelectWaferInfo(enumUnit.TRB1, 1, -1);//slot 1 UpperArm
                        m_QueSelectSlotNum.Enqueue(temp);
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(true);
                        }
                    }

                    if (ListTRB[0] != null && ListTRB[0].LowerArmWafer != null)
                    {
                        ListTRB[0].LowerArmWafer.ToLoadport = SWafer.enumFromLoader.UnKnow;
                        ListTRB[0].LowerArmWafer.ToSlot = 0;

                        //ming test
                        clsSelectWaferInfo temp = new clsSelectWaferInfo(enumUnit.TRB1, 1, -1);//slot 1 LowerArm
                        m_QueSelectSlotNum.Enqueue(temp);
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(true);
                        }
                    }

                    if (ListTRB[1] != null && ListTRB[1].UpperArmWafer != null)
                    {
                        ListTRB[1].UpperArmWafer.ToLoadport = SWafer.enumFromLoader.UnKnow;
                        ListTRB[1].UpperArmWafer.ToSlot = 0;

                        clsSelectWaferInfo temp = new clsSelectWaferInfo(enumUnit.TRB2, 1, -1);//slot 1 UpperArm
                        m_QueSelectSlotNum.Enqueue(temp);
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(true);
                        }
                    }

                    if (ListTRB[1] != null && ListTRB[1].LowerArmWafer != null)
                    {
                        ListTRB[1].LowerArmWafer.ToLoadport = SWafer.enumFromLoader.UnKnow;
                        ListTRB[1].LowerArmWafer.ToSlot = 0;

                        //ming test
                        clsSelectWaferInfo temp = new clsSelectWaferInfo(enumUnit.TRB2, 1, -1);//slot 1 LowerArm
                        m_QueSelectSlotNum.Enqueue(temp);
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(true);
                        }
                    }

                    if (ListALN[0] != null && ListALN[0].Wafer != null)
                    {
                        ListALN[0].Wafer.ToLoadport = SWafer.enumFromLoader.UnKnow;
                        ListALN[0].Wafer.ToSlot = 0;

                        clsSelectWaferInfo temp = new clsSelectWaferInfo(enumUnit.ALN1, 1, -1);
                        m_QueSelectSlotNum.Enqueue(temp);
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(true);
                        }
                    }

                    if (ListALN[1] != null && ListALN[1].Wafer != null)
                    {
                        ListALN[1].Wafer.ToLoadport = SWafer.enumFromLoader.UnKnow;
                        ListALN[1].Wafer.ToSlot = 0;

                        clsSelectWaferInfo temp = new clsSelectWaferInfo(enumUnit.ALN2, 2, -1);
                        m_QueSelectSlotNum.Enqueue(temp);
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(true);
                        }
                    }

                    foreach (I_Buffer item in ListBUF)
                    {
                        if (item.Disable) continue;
                        enumUnit eUnit = enumUnit.BUF1 + (item.BodyNo - 1);
                        for (int i = 0; i < item.HardwareSlot; i++)
                        {
                            if (item.IsSlotDisable(i)) continue;
                            if (item.GetWafer(i) != null)
                            {
                                item.GetWafer(i).ToLoadport = SWafer.enumFromLoader.UnKnow;
                                item.GetWafer(i).ToSlot = 0;

                                clsSelectWaferInfo temp = new clsSelectWaferInfo(eUnit, 1, -1);
                                m_QueSelectSlotNum.Enqueue(temp);
                                foreach (var lp in m_guiloadportList)
                                {
                                    lp.EnableUISelectPutWaferFlag(true);
                                }
                            }
                        }
                    }

                    foreach (SSEquipment item in ListEQM)
                    {
                        if (item == null || item.Disable || item.Wafer == null) continue;

                        item.Wafer.ToLoadport = SWafer.enumFromLoader.UnKnow;
                        item.Wafer.ToSlot = 0;

                        clsSelectWaferInfo temp = new clsSelectWaferInfo(enumUnit.EQM1 + item._BodyNo - 1, item._BodyNo, -1);
                        m_QueSelectSlotNum.Enqueue(temp);
                        foreach (var lp in m_guiloadportList)
                        {
                            lp.EnableUISelectPutWaferFlag(true);
                        }

                    }
                }
                //
                btnOK.Enabled = false;
            }
            catch (Exception ex)
            {
                WriteLog("[Exception]" + ex);
            }
        }
        //點選清除按鈕
        private void btnUIPickWaferAllClear_Click(object sender, EventArgs e)
        {
            ClearAllSelect();
        }


        #endregion

        private void frmWaferRecover_Load(object sender, EventArgs e)
        {
            #region 讀取 db 更新UI 紀錄上一次Wafer的Source，將其植入swafer
            DataTable dt = m_MySQL.SelectUnitStatus();
            if (dt == null || dt.Rows.Count == 0) return;

            string[] strTRB1upper = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.TRB1Upper).Split('_');
            if (strTRB1upper.Length > 1 && WaferExist(SStockerSQL.enumUnit.TRB1Upper))
            {
                lblTRB1_UpperSlotNoRecord.Text = strTRB1upper[1];
                lblTRB1_UpperOwnerRecord.Text = strTRB1upper[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strTRB1upper[0], out eFromLoader))
                {
                    ListTRB[0].UpperArmWafer.SetOwner(eFromLoader);
                    ListTRB[0].UpperArmWafer.SetSlot(int.Parse(strTRB1upper[1]));
                }
            }

            string[] strTRB1Lower = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.TRB1Lower).Split('_');
            if (strTRB1Lower.Length > 1 && WaferExist(SStockerSQL.enumUnit.TRB1Lower))
            {
                lblTRB1_LowerSlotNoRecord.Text = strTRB1Lower[1];
                lblTRB1_LowerOwnerRecord.Text = strTRB1Lower[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strTRB1Lower[0], out eFromLoader))
                {
                    ListTRB[0].LowerArmWafer.SetOwner(eFromLoader);
                    ListTRB[0].LowerArmWafer.SetSlot(int.Parse(strTRB1Lower[1]));
                }
            }

            string[] strTRB2Upper = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.TRB2Upper).Split('_');
            if (strTRB2Upper.Length > 1 && WaferExist(SStockerSQL.enumUnit.TRB2Upper))
            {
                lblTRB2_UpperSlotNoRecord.Text = strTRB2Upper[1];
                lblTRB2_UpperOwnerRecord.Text = strTRB2Upper[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strTRB2Upper[0], out eFromLoader))
                {
                    ListTRB[1].UpperArmWafer.SetOwner(eFromLoader);
                    ListTRB[1].UpperArmWafer.SetSlot(int.Parse(strTRB2Upper[1]));
                }
            }

            string[] strTRB2Lower = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.TRB2Lower).Split('_');
            if (strTRB2Lower.Length > 1 && WaferExist(SStockerSQL.enumUnit.TRB2Lower))
            {
                lblTRB2_LowerSlotNoRecord.Text = strTRB2Lower[1];
                lblTRB2_LowerOwnerRecord.Text = strTRB2Lower[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strTRB2Lower[0], out eFromLoader))
                {
                    ListTRB[1].LowerArmWafer.SetOwner(eFromLoader);
                    ListTRB[1].LowerArmWafer.SetSlot(int.Parse(strTRB2Lower[1]));
                }
            }

            string[] strALN1 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.ALN1).Split('_');
            if (strALN1.Length > 1 && WaferExist(SStockerSQL.enumUnit.ALN1))
            {
                lblALN1_SlotNoRecord.Text = strALN1[1];
                lblALN1_OwnerRecord.Text = strALN1[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strALN1[0], out eFromLoader))
                {
                    ListALN[0].Wafer.SetOwner(eFromLoader);
                    ListALN[0].Wafer.SetSlot(int.Parse(strALN1[1]));
                }
            }

            string[] strALN2 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.ALN2).Split('_');
            if (strALN2.Length > 1 && WaferExist(SStockerSQL.enumUnit.ALN2))
            {
                lblALN2_SlotNoRecord.Text = strALN2[1];
                lblALN2_OwnerRecord.Text = strALN2[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strALN2[0], out eFromLoader))
                {
                    ListALN[1].Wafer.SetOwner(eFromLoader);
                    ListALN[1].Wafer.SetSlot(int.Parse(strALN2[1]));
                }
            }

            string[] strBUF1_1 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.BUF1_slot1).Split('_');
            if (strBUF1_1.Length > 1 && WaferExist(SStockerSQL.enumUnit.BUF1_slot1))
            {
                lblBUF1_1SlotNoRecord.Text = strBUF1_1[1];
                lblBUF1_1OwnerRecord.Text = strBUF1_1[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strBUF1_1[0], out eFromLoader))
                {
                    ListBUF[0].GetWafer(0).SetOwner(eFromLoader);
                    ListBUF[0].GetWafer(0).SetSlot(int.Parse(strBUF1_1[1]));
                }
            }

            string[] strBUF1_2 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.BUF1_slot2).Split('_');
            if (strBUF1_2.Length > 1 && WaferExist(SStockerSQL.enumUnit.BUF1_slot2))
            {
                lblBUF1_2SlotNoRecord.Text = strBUF1_2[1];
                lblBUF1_2OwnerRecord.Text = strBUF1_2[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strBUF1_2[0], out eFromLoader))
                {
                    ListBUF[0].GetWafer(1).SetOwner(eFromLoader);
                    ListBUF[0].GetWafer(1).SetSlot(int.Parse(strBUF1_2[1]));
                }
            }

            string[] strBUF2_1 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.BUF2_slot1).Split('_');
            if (strBUF2_1.Length > 1 && WaferExist(SStockerSQL.enumUnit.BUF2_slot1))
            {
                lblBUF2_1SlotNoRecord.Text = strBUF2_1[1];
                lblBUF2_1OwnerRecord.Text = strBUF2_1[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strBUF2_1[0], out eFromLoader))
                {
                    ListBUF[1].GetWafer(0).SetOwner(eFromLoader);
                    ListBUF[1].GetWafer(0).SetSlot(int.Parse(strBUF2_1[1]));
                }
            }

            string[] strBUF2_2 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.BUF2_slot2).Split('_');
            if (strBUF2_2.Length > 1 && WaferExist(SStockerSQL.enumUnit.BUF2_slot2))
            {
                lblBUF2_2SlotNoRecord.Text = strBUF2_2[1];
                lblBUF2_2OwnerRecord.Text = strBUF2_2[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strBUF2_2[0], out eFromLoader))
                {
                    ListBUF[1].GetWafer(1).SetOwner(eFromLoader);
                    ListBUF[1].GetWafer(1).SetSlot(int.Parse(strBUF2_2[1]));
                }
            }


            string[] strEQ1 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.EQM1).Split('_');
            if (strEQ1.Length > 1 && WaferExist(SStockerSQL.enumUnit.EQM1))
            {
                lblEQ1SlotNoRecord.Text = strEQ1[1];
                lblEQ1OwnerRecord.Text = strEQ1[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strEQ1[0], out eFromLoader))
                {
                    ListEQM[0].Wafer.SetOwner(eFromLoader);
                    ListEQM[0].Wafer.SetSlot(int.Parse(strEQ1[1]));
                }
            }
            string[] strEQ2 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.EQM2).Split('_');
            if (strEQ2.Length > 1 && WaferExist(SStockerSQL.enumUnit.EQM2))
            {
                lblEQ2SlotNoRecord.Text = strEQ2[1];
                lblEQ2OwnerRecord.Text = strEQ2[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strEQ2[0], out eFromLoader))
                {
                    ListEQM[1].Wafer.SetOwner(eFromLoader);
                    ListEQM[1].Wafer.SetSlot(int.Parse(strEQ2[1]));
                }
            }
            string[] strEQ3 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.EQM3).Split('_');
            if (strEQ3.Length > 1 && WaferExist(SStockerSQL.enumUnit.EQM3))
            {
                lblEQ3SlotNoRecord.Text = strEQ3[1];
                lblEQ3OwnerRecord.Text = strEQ3[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strEQ3[0], out eFromLoader))
                {
                    ListEQM[2].Wafer.SetOwner(eFromLoader);
                    ListEQM[2].Wafer.SetSlot(int.Parse(strEQ3[1]));
                }
            }
            string[] strEQ4 = m_MySQL.GetUnitStatus(SStockerSQL.enumUnit.EQM4).Split('_');
            if (strEQ4.Length > 1 && WaferExist(SStockerSQL.enumUnit.EQM4))
            {
                lblEQ4SlotNoRecord.Text = strEQ4[1];
                lblEQ4OwnerRecord.Text = strEQ4[0];
                enumFromLoader eFromLoader;
                if (Enum.TryParse(strEQ4[0], out eFromLoader))
                {
                    ListEQM[3].Wafer.SetOwner(eFromLoader);
                    ListEQM[3].Wafer.SetSlot(int.Parse(strEQ4[1]));
                }
            }
            #endregion

            //如果下arm有wafer且Turn table沒wafer，詢問是否要送去Turn table
            if (ListTRB[0].LowerArmWafer != null)
            {
                if (GParam.theInst.GetAlignerMode(0) == enumAlignerType.TurnTable && ListALN[0].WaferExists() == false)//如果ALN1是Turn table且沒wafer
                {
                    frmMessageBox frm = new frmMessageBox("Whether or not to send the wafer from the lower arm to ALN1 ?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (frm.ShowDialog() == DialogResult.Yes)
                    {
                        ListTRB[0].ResetProcessCompleted();
                        ListTRB[0].SspdW(3000, 4);//test 20%
                        ListTRB[0].WaitProcessCompleted(3000);

                        int nStgeIndx = GParam.theInst.GetDicPosRobot(ListTRB[0].BodyNo, enumPosition.AlignerA, false);

                        ListTRB[0].MoveToStandbyByInterLockW(ListTRB[0].GetAckTimeout, true, enumRobotArms.LowerArm, nStgeIndx, 1);
                        ListTRB[0].PutWaferByInterLockW(ListTRB[0].GetAckTimeout, enumRobotArms.LowerArm, nStgeIndx, 1);

                        ListALN[0].ResetInPos();
                        ListALN[0].ClmpW(3000);
                        ListALN[0].WaitInPos(30000);

                        SpinWait.SpinUntil(() => false, 1000);

                        ListALN[0].ResetInPos();
                        ListALN[0].UclmW(3000);
                        ListALN[0].WaitInPos(30000);

                        ClearAllSelect();
                    }
                }
                else if (GParam.theInst.GetAlignerMode(1) == enumAlignerType.TurnTable && ListALN[1].WaferExists() == false)//如果ALN2是Turn table且沒wafer
                {
                    frmMessageBox frm = new frmMessageBox("Whether or not to send the wafer from the lower arm to ALN2 ?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (frm.ShowDialog() == DialogResult.Yes)
                    {
                        ListTRB[0].ResetProcessCompleted();
                        ListTRB[0].SspdW(3000, 4);//test 20%
                        ListTRB[0].WaitProcessCompleted(3000);

                        int nStgeIndx = GParam.theInst.GetDicPosRobot(ListTRB[0].BodyNo, enumPosition.AlignerB, false);

                        ListTRB[0].MoveToStandbyByInterLockW(ListTRB[0].GetAckTimeout, true, enumRobotArms.LowerArm, nStgeIndx, 1);
                        ListTRB[0].PutWaferByInterLockW(ListTRB[0].GetAckTimeout, enumRobotArms.LowerArm, nStgeIndx, 1);

                        ListALN[1].ResetInPos();
                        ListALN[1].ClmpW(3000);
                        ListALN[1].WaitInPos(30000);

                        SpinWait.SpinUntil(() => false, 1000);

                        ListALN[1].ResetInPos();
                        ListALN[1].UclmW(3000);
                        ListALN[1].WaitInPos(30000);

                        ClearAllSelect();
                    }
                }
            }
        }

        private void brnRecordSelection_Click(object sender, EventArgs e)
        {
            ClearAllSelect();

            DataTable dt = m_MySQL.SelectUnitStatus();
            if (dt == null || dt.Rows.Count == 0) return;

            foreach (SStockerSQL.enumUnit item in Enum.GetValues(typeof(SStockerSQL.enumUnit)))
            {
                string[] strTRB1upper = m_MySQL.GetUnitStatus(item).Split('_');
                if (strTRB1upper.Length <= 1) continue;
                if (WaferExist(item) == false) continue;
                enumFromLoader eFromLoader = (enumFromLoader)Enum.Parse(typeof(enumFromLoader), strTRB1upper[0]);
                int nSlot = int.Parse(strTRB1upper[1]);
                switch (eFromLoader)
                {
                    case enumFromLoader.LoadportA:
                    case enumFromLoader.LoadportB:
                    case enumFromLoader.LoadportC:
                    case enumFromLoader.LoadportD:
                    case enumFromLoader.LoadportE:
                    case enumFromLoader.LoadportF:
                    case enumFromLoader.LoadportG:
                    case enumFromLoader.LoadportH:
                        int nLpIndx = eFromLoader - enumFromLoader.LoadportA;

                        if (ListSTG[nLpIndx].FoupExist == false) continue;
                        if (ListSTG[nLpIndx].StatusMachine != enumStateMachine.PS_Docked) continue;
                        if (ListSTG[nLpIndx].MappingData[nSlot - 1] != '0') continue;

                        List<enumUIPickWaferStat> listSelectTargetSlotSts = new List<enumUIPickWaferStat>();
                        List<int> listSelectTargetSlot = new List<int>();
                        listSelectTargetSlotSts.Add(enumUIPickWaferStat.NoWafer);
                        listSelectTargetSlot.Add(nSlot - 1);
                        GuiLoadport_UseSelectWafer(m_guiloadportList[nLpIndx], new GUILoadport.EventArgs_SelectWafer(listSelectTargetSlotSts, listSelectTargetSlot));
                        break;
                    default:
                        break;
                }
            }



        }

        private bool WaferExist(SStockerSQL.enumUnit eUnit)
        {
            bool b = false;
            switch (eUnit)
            {
                case SStockerSQL.enumUnit.TRB1Upper:
                    b = ListTRB[0].UpperArmWafer != null;
                    break;
                case SStockerSQL.enumUnit.TRB1Lower:
                    b = ListTRB[0].LowerArmWafer != null;
                    break;
                case SStockerSQL.enumUnit.TRB2Upper:
                    b = ListTRB[1].UpperArmWafer != null;
                    break;
                case SStockerSQL.enumUnit.TRB2Lower:
                    b = ListTRB[1].LowerArmWafer != null;
                    break;
                case SStockerSQL.enumUnit.ALN1:
                    b = ListALN[0].Wafer != null;
                    break;
                case SStockerSQL.enumUnit.ALN2:
                    b = ListALN[1].Wafer != null;
                    break;
                case SStockerSQL.enumUnit.BUF1_slot1:
                    if (ListBUF[0].HardwareSlot > 0)
                    {
                        b = ListBUF[0].GetWafer(0) != null;
                    }
                    break;
                case SStockerSQL.enumUnit.BUF1_slot2:
                    if (ListBUF[0].HardwareSlot > 1)
                    {
                        b = ListBUF[0].GetWafer(1) != null;
                    }
                    break;
                case SStockerSQL.enumUnit.BUF1_slot3:
                    if (ListBUF[0].HardwareSlot > 2)
                    {
                        b = ListBUF[0].GetWafer(2) != null;
                    }
                    break;
                case SStockerSQL.enumUnit.BUF1_slot4:
                    if (ListBUF[0].HardwareSlot > 3)
                    {
                        b = ListBUF[0].GetWafer(3) != null;
                    }
                    break;
                case SStockerSQL.enumUnit.BUF2_slot1:
                    if (ListBUF[1].HardwareSlot > 0)
                    {
                        b = ListBUF[1].GetWafer(0) != null;
                    }
                    break;
                case SStockerSQL.enumUnit.BUF2_slot2:
                    if (ListBUF[1].HardwareSlot > 1)
                    {
                        b = ListBUF[1].GetWafer(1) != null;
                    }
                    break;
                case SStockerSQL.enumUnit.BUF2_slot3:
                    if (ListBUF[1].HardwareSlot > 2)
                    {
                        b = ListBUF[1].GetWafer(2) != null;
                    }
                    break;
                case SStockerSQL.enumUnit.BUF2_slot4:
                    if (ListBUF[1].HardwareSlot > 3)
                    {
                        b = ListBUF[1].GetWafer(3) != null;
                    }
                    break;
                case SStockerSQL.enumUnit.EQM1:
                    b = ListEQM[0].Wafer != null;
                    break;
                case SStockerSQL.enumUnit.EQM2:
                    b = ListEQM[1].Wafer != null;
                    break;
                case SStockerSQL.enumUnit.EQM3:
                    b = ListEQM[2].Wafer != null;
                    break;
                default:
                    break;
            }
            return b;
        }


    }
}
