using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using RorzeUnit.Interface;
using System.IO;
using System.Diagnostics;
using RorzeUnit.Class.EQ;
using System.Collections.Generic;

namespace RorzeApi
{
    public partial class frmUnitConnect : Form
    {
        private bool m_bAbort = false;
        private bool m_bBlink = false;
        private bool m_bSimulate = false;

        private List<I_Robot> ListTRB;
        private List<I_Loadport> ListSTG;
        private List<I_Aligner> ListALN;
        private List<I_RC5X0_IO> ListDIO;
        private List<I_OCR> ListOCR;
        private SSEquipment m_equipment;

        //  Dictionary
        private Dictionary<enumUnit, bool> m_DicConnect = new Dictionary<enumUnit, bool>();
        private Dictionary<enumUnit, TableLayoutPanel> m_DicPanel = new Dictionary<enumUnit, TableLayoutPanel>();
        private Dictionary<enumUnit, Label> m_DicLabel = new Dictionary<enumUnit, Label>();


        public frmUnitConnect(
            List<I_Robot> listTRB,
            List<I_Loadport> listSTG,
            List<I_Aligner> listALN,
            List<I_RC5X0_IO> listDIO,
            List<I_OCR> listOCR,
            SSEquipment equipment)
        {
            try
            {
                InitializeComponent();

                m_bSimulate = GParam.theInst.IsSimulate;
                ListTRB = listTRB;
                ListSTG = listSTG;
                ListALN = listALN;
                ListDIO = listDIO;
                ListOCR = listOCR;
                m_equipment = equipment;

                m_DicPanel.Add(enumUnit.TRB1, tlpTRB1);
                m_DicPanel.Add(enumUnit.TRB2, tlpTRB2);
                m_DicPanel.Add(enumUnit.ALN1, tlpALN1);
                m_DicPanel.Add(enumUnit.ALN2, tlpALN2);
                m_DicPanel.Add(enumUnit.STG1, tlpSTG1);
                m_DicPanel.Add(enumUnit.STG2, tlpSTG2);
                m_DicPanel.Add(enumUnit.STG3, tlpSTG3);
                m_DicPanel.Add(enumUnit.STG4, tlpSTG4);
                m_DicPanel.Add(enumUnit.STG5, tlpSTG5);
                m_DicPanel.Add(enumUnit.STG6, tlpSTG6);
                m_DicPanel.Add(enumUnit.STG7, tlpSTG7);
                m_DicPanel.Add(enumUnit.STG8, tlpSTG8);
                m_DicPanel.Add(enumUnit.DIO0, tlpDIO0);
                m_DicPanel.Add(enumUnit.DIO1, tlpDIO1);
                m_DicPanel.Add(enumUnit.DIO2, tlpDIO2);
                m_DicPanel.Add(enumUnit.EQPT, tlpEQPT);
                m_DicPanel.Add(enumUnit.BUFR, tlpBUFR);
                m_DicPanel.Add(enumUnit.OCRA1, tlpOCRA1);
                m_DicPanel.Add(enumUnit.OCRA2, tlpOCRA2);
                m_DicPanel.Add(enumUnit.OCRB1, tlpOCRB1);
                m_DicPanel.Add(enumUnit.OCRB2, tlpOCRB2);

                m_DicLabel.Add(enumUnit.TRB1, lblTRB1);
                m_DicLabel.Add(enumUnit.TRB2, lblTRB2);
                m_DicLabel.Add(enumUnit.ALN1, lblALN1);
                m_DicLabel.Add(enumUnit.ALN2, lblALN2);
                m_DicLabel.Add(enumUnit.STG1, lblSTG1);
                m_DicLabel.Add(enumUnit.STG2, lblSTG2);
                m_DicLabel.Add(enumUnit.STG3, lblSTG3);
                m_DicLabel.Add(enumUnit.STG4, lblSTG4);
                m_DicLabel.Add(enumUnit.STG5, lblSTG5);
                m_DicLabel.Add(enumUnit.STG6, lblSTG6);
                m_DicLabel.Add(enumUnit.STG7, lblSTG7);
                m_DicLabel.Add(enumUnit.STG8, lblSTG8);
                m_DicLabel.Add(enumUnit.EQPT, lblEQPT);
                m_DicLabel.Add(enumUnit.BUFR, lblBUFR);
                m_DicLabel.Add(enumUnit.DIO0, lblDIO0);
                m_DicLabel.Add(enumUnit.DIO1, lblDIO1);
                m_DicLabel.Add(enumUnit.DIO2, lblDIO2);
                m_DicLabel.Add(enumUnit.OCRA1, lblOCRA1);
                m_DicLabel.Add(enumUnit.OCRA2, lblOCRA2);
                m_DicLabel.Add(enumUnit.OCRB1, lblOCRB1);
                m_DicLabel.Add(enumUnit.OCRB2, lblOCRB2);

                InitPanel();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        private void InitPanel()
        {
            for (enumUnit i = 0; i < enumUnit.Total; i++)
            {
                if (m_DicPanel.ContainsKey(i))
                    m_DicPanel[i].Visible = GParam.theInst.IsUnitVisible(i);

                m_DicConnect[i] = GParam.theInst.IsUnitDisable(i);
            }

            m_tmr.Interval = 750;
            m_tmr.Enabled = true;
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            frmPassword myPodPwd = new frmPassword();
            myPodPwd.setPassword("1");
            if (DialogResult.OK == myPodPwd.ShowDialog())
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
                lbTitle.Text = m_bBlink ? "" : "Check Connection!!";

                bool bConnected = IsAllConnect();
                if (bConnected)
                {
                    this.DialogResult = DialogResult.OK;
                    SpinWait.SpinUntil(() => false, 1000);
                    this.Close();
                    return;
                }

                m_tmr.Enabled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                m_tmr.Enabled = false;
            }
        }
        private void RunConnect()
        {
            try
            {
                if (m_bSimulate == false)
                {                   

                    for (int i = 0; i < (int)enumRobot.Total; i++)
                    {
                        if (ListTRB[i].Disable == false && GParam.theInst.GetTRB_TCPType(i) == enumTCPType.Server)
                        {
                            ListTRB[i].Open();//client 去連 server
                        }
                    }

                    for (int i = 0; i < (int)enumLoadport.Total; i++)
                    {
                        if (ListSTG[i].Disable == false && GParam.theInst.GetSTG_TCPType(i) == enumTCPType.Server)
                        {
                            ListSTG[i].Open();//client 去連 server
                        }
                    }
                    if (ListALN[0].Disable == false) { ListALN[0].Open(); }//Client to server
                    if (ListALN[1].Disable == false) { ListALN[1].Open(); }//Client to server
                    if (ListDIO[0].Disable == false) { ListDIO[0].Open(); }//Client to server
                    if (ListDIO[1].Disable == false) { ListDIO[1].Open(); }//Client to server
                    if (ListDIO[2].Disable == false) { ListDIO[2].Open(); }//Client to server
                }

                //  EQ TCPIP
                if (m_equipment.Simulate == false && m_equipment.Disable == false && GParam.theInst.GetEQTCPtype != enumTCPType.None)
                {
                    System.Threading.Tasks.Task.Run(() => { m_equipment.Open(); });
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        private bool IsAllConnect()
        {
            bool bAllConnect = true;
            try
            {
                for (enumUnit i = 0; i < enumUnit.Total; i++)
                {
                    if (GParam.theInst.IsUnitDisable(i))
                    {
                        m_DicPanel[i].BackColor = Color.Gray;
                        m_DicLabel[i].Text = "Disable!!";
                    }
                    else if (m_DicConnect[i] == false)
                    {
                        if (CheckUnitConnected(i) || m_bSimulate)
                        {
                            m_DicPanel[i].BackColor = Color.DarkGreen;
                            m_DicLabel[i].Text = "Connected!!";
                            m_DicConnect[i] = true;

                            if (m_bSimulate && i == enumUnit.DIO1)
                            {
                                ListDIO[1].SetGDIO_InputStatus(0, 2, true);//負壓
                                ListDIO[1].SetGDIO_InputStatus(0, 3, true);//正壓

                                ListDIO[1].SetGDIO_InputStatus(0, 5, true);//正壓
                                ListDIO[1].SetGDIO_InputStatus(0, 6, true);//靜電

                                ListDIO[1].SetGDIO_InputStatus(0, 8, true);//光閘
                            }
                        }
                    }
                }

                foreach (var item in m_DicConnect)
                    bAllConnect &= item.Value;
                if (bAllConnect)
                {

                }

            }
            catch (Exception ex)
            {
                bAllConnect = false;
                Console.WriteLine("{0} Exception caught.", ex);
            }
            return bAllConnect;
        }
        private void frmUnitConnect_Load(object sender, EventArgs e)
        {
            System.Threading.Tasks.Task.Run(() => { RunConnect(); });
        }
        private void frmUnitConnect_VisibleChanged(object sender, EventArgs e)
        {
            Form theForm = (Form)sender;
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
                case enumUnit.EQPT: bConnected = m_equipment.Connected; break;
                case enumUnit.BUFR: bConnected = GParam.theInst.GetBufferEnableSlot > 0; break;
                case enumUnit.OCRA1: bConnected = ListOCR[0].Connected; break;
                case enumUnit.OCRA2: bConnected = ListOCR[0].Connected; break;
                case enumUnit.OCRB1: bConnected = ListOCR[0].Connected; break;
                case enumUnit.OCRB2: bConnected = ListOCR[0].Connected; break;
            }
            return bConnected;
        }


    }
}
