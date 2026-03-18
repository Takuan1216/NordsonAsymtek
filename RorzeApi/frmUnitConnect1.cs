using System;
using System.Drawing;
using System.Windows.Forms;
using RorzeUnit.Interface;
using System.Collections.Generic;
using System.Linq;
using RorzeComm.Log;
using System.Runtime.CompilerServices;
using RorzeUnit.Class.EQ;

namespace RorzeApi
{
    public partial class frmUnitConnect1 : Form
    {
        private bool m_bAbort = false;
        private bool m_bBlink = false;
        private bool m_bSimulate = false;

        private List<I_Robot> ListTRB;
        private List<I_Loadport> ListSTG;
        private List<I_Aligner> ListALN;
        private List<I_RC5X0_IO> ListDIO;
        private List<I_OCR> ListOCR;
        private List<I_BarCode> ListBCR;
        private List<SSEquipment> ListEQM;

        //  Dictionary
        private Dictionary<enumUnit, Panel> m_DicPanel = new Dictionary<enumUnit, Panel>();
        private Dictionary<enumUnit, Label> m_DicLabel = new Dictionary<enumUnit, Label>();
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        public frmUnitConnect1(
            List<I_Robot> listTRB,
            List<I_Loadport> listSTG,
            List<I_Aligner> listALN,
            List<I_RC5X0_IO> listDIO,
            List<I_OCR> listOCR,
            List<I_BarCode> listBCR,
            List<SSEquipment> listEQM)
        {
            try
            {
                InitializeComponent();

                this.Icon = GParam.theInst.FreeStyle ? RorzeApi.Properties.Resources.bwbs_ : RorzeApi.Properties.Resources.R;

                m_bSimulate = GParam.theInst.IsSimulate;
                ListTRB = listTRB;
                ListSTG = listSTG;
                ListALN = listALN;
                ListDIO = listDIO;
                ListOCR = listOCR;
                ListBCR = listBCR;
                ListEQM = listEQM;
                this.ControlBox = false;

                ///需要判斷連線的加入Dictionary
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
                    { enumUnit.DIO0,"IO card0" },
                    { enumUnit.DIO1,"IO card1" },
                    { enumUnit.DIO2,"IO card2" },
                    { enumUnit.DIO3,"IO card3" },
                    { enumUnit.DIO4,"IO card4" },
                    { enumUnit.DIO5,"IO card5" },
                    { enumUnit.BCR1,"Barcode1" },
                    { enumUnit.BCR2,"Barcode2" },
                    { enumUnit.BCR3,"Barcode3" },
                    { enumUnit.BCR4,"Barcode4" },
                    { enumUnit.BCR5,"Barcode5" },
                    { enumUnit.BCR6,"Barcode6" },
                    { enumUnit.BCR7,"Barcode7" },
                    { enumUnit.BCR8,"Barcode8" },
                    { enumUnit.EQM1,ListEQM[0]!=null?ListEQM[0]._Name : "Equipment1" },
                    { enumUnit.EQM2,ListEQM[1]!=null?ListEQM[1]._Name : "Equipment2" },
                    { enumUnit.EQM3,ListEQM[2]!=null?ListEQM[2]._Name : "Equipment3" },
                    { enumUnit.EQM4,ListEQM[3]!=null?ListEQM[3]._Name : "Equipment4" },
                };

                //產風畫面元件
                foreach (var item in dic)
                {
                    if (GParam.theInst.IsUnitDisable(item.Key)) continue;

                    Panel pnl = new Panel
                    {
                        BorderStyle = BorderStyle.FixedSingle,
                        Size = new Size(flowLayoutPanel1.Width - 6, 36),
                        BackColor = Color.Yellow,
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
                        Font = new Font("微軟正黑體", 16, FontStyle.Bold),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    Label lblsub = new Label
                    {
                        Text = ":",
                        Font = new Font("微軟正黑體", 16, FontStyle.Bold),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    Label lblContent = new Label
                    {
                        Text = GParam.theInst.GetLanguage("Waitting"),
                        Font = new Font("微軟正黑體", 16, FontStyle.Bold),
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

                    m_DicPanel.Add(item.Key, pnl);
                    m_DicLabel.Add(item.Key, lblContent);
                    flowLayoutPanel1.Controls.Add(pnl);
                }

                this.Size = new Size(this.Width, 39 + flowLayoutPanel1.Controls.Count * (36 + 6) + btnExit.Height);

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            frmPassword myPodPwd = new frmPassword();
            if (DialogResult.OK == myPodPwd.ShowDialog() && myPodPwd.GetPassWord == "1")
            {
                m_bAbort = true;
            }
        }
        private void m_tmr_Tick(object sender, EventArgs e)
        {
            m_tmr.Enabled = false;
            try
            {
                if (m_bAbort)
                {
                    m_tmr.Enabled = false;
                    this.DialogResult = DialogResult.Abort;
                    this.Close();
                    return;
                }

                m_bBlink = (false == m_bBlink);
                lbTitle.Text = m_bBlink ? "" : GParam.theInst.GetLanguage("Check Connection!!");

                bool bConnected = IsAllConnect();
                if (bConnected)
                {
                    this.DialogResult = DialogResult.OK;
                    System.Threading.SpinWait.SpinUntil(() => false, 100);
                    this.Close();
                    return;
                }

                m_tmr.Enabled = true;
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
                m_tmr.Enabled = false;
            }
        }
        private void RunConnect()
        {
            try
            {
                if (m_bSimulate == false)
                {
                    foreach (I_Robot item in ListTRB)
                    {
                        int nIndx = item.BodyNo - 1;
                        if (item.Disable == false && GParam.theInst.GetTRB_TCPType(nIndx) == enumTCPType.Server)
                        {
                            item.Open();//client 去連 server
                        }
                    }

                    foreach (I_Loadport item in ListSTG)
                    {
                        int nIndx = item.BodyNo - 1;
                        if (item.Disable == false && GParam.theInst.GetSTG_TCPType(nIndx) == enumTCPType.Server)
                        {
                            item.Open();//client 去連 server
                        }
                        item.BarcodeOpen();//Client to server
                    }

                    foreach (I_RC5X0_IO item in ListDIO)
                    {
                        if (item.Disable == false) item.Open();//Client to server
                    }


                    foreach (I_Aligner item in ListALN)
                    {
                        if (item != null && item.Disable == false)
                        {
                            item.Open();//Client to server
                            item.BarcodeOpen();//Client to server
                        }
                    }

               
                }

                foreach (SSEquipment item in ListEQM)
                {
                    if (item == null || item.Disable) continue;
                    item.Open();//EQ要分開看，因為要考慮全部模擬只有EQ實際
                }

            }
            catch (Exception ex) { WriteLog("[Exception] " + ex); }
        }
        private bool IsAllConnect()
        {
            bool bAllConnect = true;
            try
            {
                foreach (var item in m_DicPanel.ToArray())
                {
                    Label lbl = m_DicLabel[item.Key];
                    if (GParam.theInst.IsUnitDisable(item.Key))
                    {
                        item.Value.BackColor = Color.Gray;
                        lbl.Text = GParam.theInst.GetLanguage("Disable!!");
                    }
                    else if (CheckUnitConnected(item.Key) && item.Value.BackColor == Color.Yellow)
                    {
                        if (m_bSimulate)
                        {
                            switch (item.Key)
                            {
                                case enumUnit.DIO1:
                                    for (int i = 0; i < 16; i++)
                                    {
                                        bool b = ListDIO[1].GetGDIO_InputStatus(0, i);
                                        bool bAbnormal = GParam.theInst.Check_DIO_IO_Abnormal(1, i, b);
                                        if (bAbnormal) ListDIO[1].SetGDIO_InputStatus(0, i, !b);
                                    }
                                    break;
                                case enumUnit.DIO2:
                                    for (int i = 0; i < 16; i++)
                                    {
                                        bool b = ListDIO[2].GetGDIO_InputStatus(0, i);
                                        bool bAbnormal = GParam.theInst.Check_DIO_IO_Abnormal(2, i, b);
                                        if (bAbnormal) ListDIO[2].SetGDIO_InputStatus(0, i, !b);
                                    }
                                    break;
                            }
                        }

                        item.Value.BackColor = GParam.theInst.ColorReadyGreen;
                        lbl.Text = GParam.theInst.GetLanguage("Connected!!");
                    }
                    if (item.Value.BackColor == Color.Yellow)
                        bAllConnect = false;
                }
            }
            catch (Exception ex) { WriteLog("[Exception] " + ex); }
            return bAllConnect;
        }
        private void frmUnitConnect_Load(object sender, EventArgs e)
        {
            m_tmr.Interval = 750;
            m_tmr.Enabled = true;

            System.Threading.Tasks.Task.Run(() => { RunConnect(); });
        }
        private bool CheckUnitConnected(enumUnit eUnit)
        {
            bool bConnected = false;
            switch (eUnit)
            {
                case enumUnit.TRB1: bConnected = ListTRB[0].Connected; break;
                case enumUnit.TRB2: bConnected = ListTRB[1].Connected; break;
                case enumUnit.ALN1: bConnected = ListALN[0].Connected; break;
                case enumUnit.ALN2: bConnected = ListALN[1].Connected; break;
                case enumUnit.STG1: bConnected = ListSTG[0].Connected; break;
                case enumUnit.STG2: bConnected = ListSTG[1].Connected; break;
                case enumUnit.STG3: bConnected = ListSTG[2].Connected; break;
                case enumUnit.STG4: bConnected = ListSTG[3].Connected; break;
                case enumUnit.STG5: bConnected = ListSTG[4].Connected; break;
                case enumUnit.STG6: bConnected = ListSTG[5].Connected; break;
                case enumUnit.STG7: bConnected = ListSTG[6].Connected; break;
                case enumUnit.STG8: bConnected = ListSTG[7].Connected; break;
                case enumUnit.DIO0: bConnected = ListDIO[0].Connected; break;
                case enumUnit.DIO1: bConnected = ListDIO[1].Connected; break;
                case enumUnit.DIO2: bConnected = ListDIO[2].Connected; break;
                case enumUnit.DIO3: bConnected = ListDIO[3].Connected; break;
                case enumUnit.DIO4: bConnected = ListDIO[4].Connected; break;
                case enumUnit.DIO5: bConnected = ListDIO[5].Connected; break;
                case enumUnit.OCRA1: bConnected = ListOCR[0].Connected; break;
                case enumUnit.OCRA2: bConnected = ListOCR[1].Connected; break;
                case enumUnit.OCRB1: bConnected = ListOCR[2].Connected; break;
                case enumUnit.OCRB2: bConnected = ListOCR[3].Connected; break;
                case enumUnit.BCR1: bConnected = ListBCR[0].Connected; break;
                case enumUnit.BCR2: bConnected = ListBCR[1].Connected; break;
                case enumUnit.BCR3: bConnected = ListBCR[2].Connected; break;
                case enumUnit.BCR4: bConnected = ListBCR[3].Connected; break;
                case enumUnit.BCR5: bConnected = ListBCR[4].Connected; break;
                case enumUnit.BCR6: bConnected = ListBCR[5].Connected; break;
                case enumUnit.BCR7: bConnected = ListBCR[6].Connected; break;
                case enumUnit.BCR8: bConnected = ListBCR[7].Connected; break;
                case enumUnit.EQM1:
                    {
                        bConnected = ListEQM[0].Connected || ListEQM[0].Simulate;
                    }
                    return bConnected;//EQ要分開看，因為要考慮全部模擬只有EQ實際
                case enumUnit.EQM2:
                    {
                        bConnected = ListEQM[1].Connected || ListEQM[1].Simulate;
                    }
                    return bConnected;//EQ要分開看，因為要考慮全部模擬只有EQ實際
                case enumUnit.EQM3:
                    {
                        bConnected = ListEQM[2].Connected || ListEQM[2].Simulate;
                    }
                    return bConnected;//EQ要分開看，因為要考慮全部模擬只有EQ實際
            }
            return bConnected || m_bSimulate;
        }


    }
}
