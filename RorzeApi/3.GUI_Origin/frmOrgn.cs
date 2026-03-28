using System;
using System.Windows.Forms;
using System.Threading;
using RorzeUnit.Interface;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit;
using System.Collections.Generic;
using RorzeUnit.Class;
using RorzeComm.Threading;
using RorzeComm.Log;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Reflection;
using System.Linq;
using static RorzeUnit.Class.SWafer;
using RorzeApi.Class;
using RorzeUnit.Class.EQ;
using System.Windows;

namespace RorzeApi
{
    public partial class frmOrgn : Form
    {
        private bool m_bSimulate;
        private bool m_bAbort = false;
        private SInterruptOneThread _exeAllOrng;

        private delegate void UpdateUI(Label LB, string str);
        public void ChangeMessage(Label LB, string strMsg)
        {
            if (InvokeRequired)
            {
                UpdateUI dlg = new UpdateUI(ChangeMessage);
                this.BeginInvoke(dlg, LB, strMsg);
            }
            else
            {
                // LB.AutoSize = true;
                // LB.Margin = new System.Windows.Forms.Padding(0);
                LB.Text = strMsg;
                // lbl.ForeColor = color;
                // layoutMessage.Controls.Add(lbl);
                this.Refresh();
            }
        }

        //  Unit
        private List<I_Robot> ListTRB;
        private List<I_Loadport> ListSTG;
        private List<I_Aligner> ListALN;
        private List<I_Buffer> ListBUF;
        private List<SSEquipment> ListEQM;
        //  Dictionary
        private Dictionary<enumUnit, bool> m_DicOrgnOK = new Dictionary<enumUnit, bool>();
        private Dictionary<enumUnit, Panel> m_DicPanel = new Dictionary<enumUnit, Panel>();
        private Dictionary<enumUnit, Label> m_DicLabel = new Dictionary<enumUnit, Label>();
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        public frmOrgn(List<I_Robot> listTRB, List<I_Loadport> listSTG, List<I_Aligner> listALN, List<I_Buffer> listBUF, List<SSEquipment> listEQM, bool Simulate)
        {
            try
            {
                InitializeComponent();
                if (GParam.theInst.FreeStyle)
                {
                    this.Icon = RorzeApi.Properties.Resources.bwbs_;
                }
                else
                {
                    this.Icon = RorzeApi.Properties.Resources.R;
                }
                ListTRB = listTRB;
                ListSTG = listSTG;
                ListALN = listALN;
                ListBUF = listBUF;
                ListEQM = listEQM;
                m_bSimulate = Simulate;

                this.ControlBox = false;

                Dictionary<enumUnit, string> dic = new Dictionary<enumUnit, string>()
                {
                    { enumUnit.TRB1,"RobotA" },
                    { enumUnit.TRB2,"RobotB" },
                    { enumUnit.STG1,"LoadportA" },
                    { enumUnit.STG2,"LoadportB" },
                    { enumUnit.STG3,"LoadportC" },
                    { enumUnit.STG4,"LoadportD" },
                    { enumUnit.STG5,"LoadportE" },
                    { enumUnit.STG6,"LoadportF" },
                    { enumUnit.STG7,"LoadportG" },
                    { enumUnit.STG8,"LoadportH" },
                    { enumUnit.ALN1,"AlignerA" },
                    { enumUnit.ALN2,"AlignerB" },
                    { enumUnit.EQM1,ListEQM[0]!=null?ListEQM[0]._Name : "Equipment1" },
                    { enumUnit.EQM2,ListEQM[1]!=null?ListEQM[1]._Name : "Equipment2" },
                    { enumUnit.EQM3,ListEQM[2]!=null?ListEQM[2]._Name : "Equipment3" },
                    { enumUnit.EQM4,ListEQM[3]!=null?ListEQM[3]._Name : "Equipment4" },
                };

                foreach (var item in dic)
                {
                    if (GParam.theInst.IsUnitDisable(item.Key)) continue;

                    Panel pnl = new Panel
                    {
                        BorderStyle = BorderStyle.FixedSingle,
                        Size = new System.Drawing.Size(570, 36)
                    };
                    TableLayoutPanel tlp = new TableLayoutPanel
                    {
                        //AutoSize = true,
                        Dock = DockStyle.Fill,
                        ColumnCount = 3,
                    };
                    Label lblName = new Label
                    {
                        Text = GParam.theInst.GetLanguage(item.Value),
                        Font = new Font("微軟正黑體", 16, System.Drawing.FontStyle.Bold),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    Label lblsub = new Label
                    {
                        Text = ":",
                        Font = new Font("微軟正黑體", 16, System.Drawing.FontStyle.Bold),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    Label lblContent = new Label
                    {
                        Text = GParam.theInst.GetLanguage("Waitting..."),
                        Font = new Font("微軟正黑體", 16, System.Drawing.FontStyle.Bold),
                        ForeColor = Color.Red,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5F));
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
                    tlp.Controls.Add(lblName, 0, 0);
                    tlp.Controls.Add(lblsub, 1, 0);
                    tlp.Controls.Add(lblContent, 2, 0);
                    pnl.Controls.Add(tlp);
                    flowLayoutPanel1.Controls.Add(pnl);

                    m_DicPanel.Add(item.Key, pnl);
                    m_DicOrgnOK.Add(item.Key, false);
                    m_DicLabel.Add(item.Key, lblContent);
                }

                //this.Size = new Size(this.Width, 39 + flowLayoutPanel1.Controls.Count * (36 + 6));
                this.Height = 39 + flowLayoutPanel1.Controls.Count * (36 + 6);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void RunAllOrgn()
        {
            try
            {
                GMotion.theInst.InitOrgnDone = false;
                for (int i = 0; i < ListTRB.Count; i++)
                {
                    if (ListTRB[i].Disable == false)
                    {
                        ChangeMessage(m_DicLabel[enumUnit.TRB1 + i], GParam.theInst.GetLanguage("Initializing..."));
                        ListTRB[i].ORGN();
                        WriteLog(string.Format("Robot{0} Origin.", i + 1));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        //  Robot
        private void _robot_OnORGNComplete(object sender, bool bSuc)
        {
            I_Robot theRobot = sender as I_Robot;
            int nIndex = theRobot.BodyNo - 1;
            theRobot.OnORGNComplete -= _robot_OnORGNComplete;

            lock (this)
            {
                WriteLog(string.Format("Robot{0} Origin Success:{1}.", theRobot.BodyNo, bSuc));
                ChangeMessage(m_DicLabel[enumUnit.TRB1 + nIndex], GParam.theInst.GetLanguage(bSuc ? "Done." : "Fail."));
                m_DicOrgnOK[enumUnit.TRB1 + nIndex] = bSuc;
                
                for (int i = 0; i < ListEQM.Count; i++)
                {
                    ListEQM[i].SetRobotExtendIO(false);
                }

                bool bOK = false;
                if (GParam.theInst.IsUnitDisable(enumUnit.TRB1))
                {
                    bOK = m_DicOrgnOK[enumUnit.TRB2];
                }
                else if (GParam.theInst.IsUnitDisable(enumUnit.TRB2))
                {
                    bOK = m_DicOrgnOK[enumUnit.TRB1];
                }
                else
                {
                    bOK = m_DicOrgnOK[enumUnit.TRB1] && m_DicOrgnOK[enumUnit.TRB2];
                }

                if (bOK)
                {
                    //  Loadport
                    for (int i = 0; i < ListSTG.Count; i++)
                    {
                        if (ListSTG[i] == null || ListSTG[i].Disable) continue;
                        ChangeMessage(m_DicLabel[enumUnit.STG1 + i], GParam.theInst.GetLanguage("Initializing..."));
                        ListSTG[i].ORGN();
                        WriteLog(string.Format("Loadport{0} Origin.", i + 1));
                    }
                    //  Alinger
                    for (int i = 0; i < ListALN.Count; i++)
                    {
                        if (ListALN[i] == null || ListALN[i].Disable) continue;

                        ChangeMessage(m_DicLabel[enumUnit.ALN1 + i], GParam.theInst.GetLanguage("Initializing..."));
                        ListALN[i].ORGN();
                        WriteLog(string.Format("Aligner{0} Origin.", i + 1));
                        break;
                    }
                    //  Equipment
                    for (int i = 0; i < ListEQM.Count; i++)
                    {
                        if (ListEQM[i] == null || ListEQM[i].Disable) continue;
                        ChangeMessage(m_DicLabel[enumUnit.EQM1 + i], GParam.theInst.GetLanguage("Initializing..."));
                        ListEQM[i].tOrgnSet();
                        WriteLog(string.Format("Equipment{0} Origin.", i + 1));
                    }





                }

                if (bSuc == false) this.ControlBox = true;
            }
        }
        //Loadport
        private void _loadport_OnORGNComplete(object sender, bool bSuc)
        {

            I_Loadport item = sender as I_Loadport;
            item.OnORGNComplete -= _loadport_OnORGNComplete;
            int nIndex = item.BodyNo - 1;
            WriteLog(string.Format("Loadport{0} Origin Success:{1}.", item.BodyNo, bSuc));
            ChangeMessage(m_DicLabel[enumUnit.STG1 + nIndex], GParam.theInst.GetLanguage(bSuc ? "Done." : "Fail."));
            m_DicOrgnOK[enumUnit.STG1 + nIndex] = bSuc;

            if (bSuc)
            {
                item.GetData();
                if (item.E84Object != null)
                    item.E84Object.SetAutoMode(false);
            }
            else
            {
                this.ControlBox = true;
            }
        }
        //Alinger
        private void _aligner_OnORGNComplete(object sender, bool bSuc)
        {
            I_Aligner item = sender as I_Aligner;
            item.OnORGNComplete -= _aligner_OnORGNComplete;
            int nIndex = item.BodyNo - 1;
            WriteLog(string.Format("Aligner{0} Origin Success:{1}.", item.BodyNo, bSuc));
            ChangeMessage(m_DicLabel[enumUnit.ALN1 + nIndex], GParam.theInst.GetLanguage(bSuc ? "Done." : "Fail."));
            m_DicOrgnOK[enumUnit.ALN1 + nIndex] = bSuc;

            if (bSuc == false) this.ControlBox = true;

            for (int i = 0; i < ListALN.Count; i++)//找下一個Aligner
            {
                if (item.BodyNo == (i + 1)) continue;
                if (ListALN[i] == null || ListALN[i].Disable || m_DicOrgnOK[enumUnit.ALN1 + i]) continue;

                ChangeMessage(m_DicLabel[enumUnit.ALN1 + i], GParam.theInst.GetLanguage("Initializing..."));
                ListALN[i].ORGN();
                WriteLog(string.Format("Aligner{0} Origin.", i + 1));
                break;
            }

        }
        //  Equipment
        private void _eq_OnORGNComplete(object sender, bool bSuc)
        {
            SSEquipment item = sender as SSEquipment;
            item.OnOrgnComplete -= _eq_OnORGNComplete;
            int nIndex = item._BodyNo - 1;
            WriteLog(string.Format("Equipment{0} Origin Success:{1}.", 1, bSuc));
            ChangeMessage(m_DicLabel[enumUnit.EQM1 + nIndex], GParam.theInst.GetLanguage(bSuc ? "Done." : "Fail."));
            m_DicOrgnOK[enumUnit.EQM1 + nIndex] = bSuc;
            if (bSuc == false) this.ControlBox = true;
        }


        private void m_tmr_Tick(object sender, EventArgs e)
        {
            m_tmr.Enabled = false;

            if (m_bAbort)
            {
                this.DialogResult = DialogResult.Abort;
                this.Close();
                return;
            }

            bool bFinish = true;
            foreach (var item in m_DicOrgnOK.ToArray())
                bFinish &= item.Value;

            if (bFinish)
            {
                //========== Robot
                foreach (I_Robot trb in ListTRB)
                {
                    if (trb == null) continue;
                    if (!m_bSimulate && trb.Disable == false)
                    {
                        //========== 檢查UpperArm
                        if (trb.GPIO.DI_UpperPresence1 && trb.GPIO.DI_UpperPresence2)
                        {
                            if (trb.UpperArmWafer == null)
                            {
                                //建空WAFER
                                trb.UpperArmWafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    trb.UpperArmFunc == RorzeUnit.Class.Robot.Enum.enumArmFunction.FRAME ? SWafer.enumWaferSize.Frame : SWafer.enumWaferSize.Inch12,
                                    SWafer.enumPosition.UpperArm,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep);
                            }
                        }
                        else
                        {
                            trb.UpperArmWafer = null;
                        }
                        //========== 檢查LowerArm
                        if (trb.GPIO.DI_LowerPresence1 && trb.GPIO.DI_LowerPresence2)
                        {
                            if (trb.LowerArmWafer == null)
                            {
                                trb.LowerArmWafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                    trb.LowerArmFunc == RorzeUnit.Class.Robot.Enum.enumArmFunction.FRAME ? SWafer.enumWaferSize.Frame : SWafer.enumWaferSize.Inch12,
                                    SWafer.enumPosition.LowerArm,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep);
                            }
                        }
                        else
                        {
                            trb.LowerArmWafer = null;
                        }
                    }
                }
                //========== Alugner
                foreach (I_Aligner aln in ListALN)
                {
                    if (aln == null) continue;
                    SWafer wafer = null;
                    if (aln.WaferExists())//有偵測到且沒有帳
                    {
                        if (aln.Wafer == null)
                        {
                            enumWaferSize wafersize = aln.WaferType;

                            wafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                wafersize,
                                SWafer.enumPosition.AlignerA + aln.BodyNo - 1,
                                SWafer.enumFromLoader.UnKnow,
                                GParam.theInst.EqmDisableArray,
                                SWafer.enumProcessStatus.Sleep);
                            aln.Wafer = wafer;//新建
                        }
                    }
                    else
                        aln.Wafer = null;//移除
                }
                //========== Buffer
                foreach (I_Buffer buf in ListBUF)
                {
                    if (buf == null) continue;
                    for (int i = 0; i < buf.HardwareSlot; i++)//0~3有四層
                    {
                        SWafer wafer = null;
                        if (buf.IsWaferDetectOn(i))
                        {
                            if (buf.GetWafer(i) == null)
                            {
                                wafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", i + 1,
                                    buf.WaferType,
                                    SWafer.enumPosition.BufferA + buf.BodyNo - 1,
                                    SWafer.enumFromLoader.UnKnow,
                                    GParam.theInst.EqmDisableArray,
                                    SWafer.enumProcessStatus.Sleep);
                                buf.SetWafer(i, wafer);//新建
                            }
                        }
                        else
                            buf.SetWafer(i, null);//移除
                    }
                }
                //========== Equipment
                foreach (SSEquipment item in ListEQM)
                {
                    if (item == null || item.Disable) continue;
                    SWafer wafer = null;
                    if (item.IsWaferExist)
                    {
                        if (item.Wafer == null)
                        {
                            enumWaferSize wafersize = enumWaferSize.Unknow;

                            //frmMessageBox messageBox = new frmMessageBox("Detect EQ has wafer.\nPlease select EQ wafer size", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, true);

                            //if (messageBox.ShowDialog() == DialogResult.OK)
                            //{
                            //    wafersize = enumWaferSize.Inch12;
                            //}
                            //else
                            //{
                            //    wafersize = enumWaferSize.Frame;
                            //}

                            wafersize = enumWaferSize.Panel;

                            string size = "Panel" /*= wafersize == enumWaferSize.Inch12 ? "Inch 12" : "Frame"*/;

                            WriteLog("User select EQ wafer size is " + size);

                            wafer = new SWafer("strFoupID", "strLotID", "strCJID", "strPJID", "", 1,
                                wafersize,
                                SWafer.enumPosition.EQM1 + item._BodyNo - 1,
                                SWafer.enumFromLoader.UnKnow,
                                GParam.theInst.EqmDisableArray,
                                SWafer.enumProcessStatus.Sleep);
                            item.Wafer = wafer;//新建
                        }
                    }
                    else
                    {
                        item.Wafer = null;//移除
                    }
                }

                GMotion.theInst.InitOrgnDone = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            m_tmr.Enabled = true;
        }
        private void frmOrgn_VisibleChanged(object sender, EventArgs e)
        {
            Form theForm = (Form)sender;
            m_tmr.Enabled = theForm.Visible;
        }
        private void frmOrgn_Load(object sender, EventArgs e)
        {
            foreach (var item in m_DicOrgnOK.ToArray())
            {
                if (GParam.theInst.IsUnitDisable(item.Key))
                    m_DicOrgnOK[item.Key] = true;
                else
                    m_DicOrgnOK[item.Key] = false;
            }

            foreach (var item in m_DicLabel)
            {
                if (item.Key == enumUnit.TRB1 || item.Key == enumUnit.TRB2)
                    item.Value.Text = GParam.theInst.GetLanguage("Ready to initial.");
                else
                    item.Value.Text = GParam.theInst.GetLanguage("Wait Robot.");
            }

            m_tmr.Interval = 750;
            m_tmr.Enabled = true;

            //Robot       
            for (int i = 0; i < ListTRB.Count; i++)
            {
                I_Robot item = ListTRB[i];
                if (item != null && item.Disable == false)
                {
                    item.OnORGNComplete -= _robot_OnORGNComplete;
                    item.OnORGNComplete += _robot_OnORGNComplete;
                }
            }
            //Loadport
            for (int i = 0; i < ListSTG.Count; i++)
            {
                I_Loadport item = ListSTG[i];
                if (item != null && item.Disable == false)
                {
                    item.OnORGNComplete -= _loadport_OnORGNComplete;
                    item.OnORGNComplete += _loadport_OnORGNComplete;
                }
            }
            //Alinger
            for (int i = 0; i < ListALN.Count; i++)
            {
                I_Aligner item = ListALN[i];
                if (item != null && item.Disable == false)
                {
                    item.OnORGNComplete -= _aligner_OnORGNComplete;
                    item.OnORGNComplete += _aligner_OnORGNComplete;
                }
            }
            //  Equipment
            for (int i = 0; i < ListEQM.Count; i++)
            {
                SSEquipment item = ListEQM[i];
                if (item == null || item.Disable) continue;
                item.OnOrgnComplete -= _eq_OnORGNComplete;
                item.OnOrgnComplete += _eq_OnORGNComplete;
            }
            _exeAllOrng = new SInterruptOneThread(RunAllOrgn);
            // ORGN Robot
            _exeAllOrng.Set();
        }
        private void frmOrgn_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Robot
            foreach (I_Robot item in ListTRB)
            {
                if (item == null || item.Disable) continue;
                item.OnORGNComplete -= _robot_OnORGNComplete;
            }
            //  Loadport
            foreach (I_Loadport item in ListSTG)
            {
                if (item == null || item.Disable) continue;
                item.OnORGNComplete -= _loadport_OnORGNComplete;
            }
            //  Alinger
            foreach (I_Aligner item in ListALN)
            {
                if (item == null || item.Disable) continue;
                item.OnORGNComplete -= _aligner_OnORGNComplete;
            }
            //  
            foreach (SSEquipment item in ListEQM)
            {
                if (item == null || item.Disable) continue;
                item.OnOrgnComplete -= _eq_OnORGNComplete;
            }
        }


    }
}
