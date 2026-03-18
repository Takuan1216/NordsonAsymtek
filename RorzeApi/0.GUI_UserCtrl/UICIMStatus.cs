using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rorze.Secs;

namespace NewGem300Server_OOP.GUI
{
    public partial class UICIMStatus : UserControl
    {

        private GEMCommStats blnConn = GEMCommStats.DISABLE;
        private GEMControlStats blncontrol = GEMControlStats.OFFLINE;
        private GEMProcessStats blnProcess = GEMProcessStats.Init;
        public enum UIGEMControlStats
        { OFFLINE = 0, EQUIPMENTOFFLINE = 1, ATTEMTPONLINE = 2, HOSTOFFLINE = 3, ONLINELOCAL = 4, ONLINEREMOTE = 5 }
        public enum UIMCommStats
        { Disable = 0, NOTCOMMUNICATION = 1, COMMUNICATION = 2 }

        public UICIMStatus()
        {
            InitializeComponent();
            //207, 70
            tableLayoutPanel2.Visible = false;
        }

        public GEMCommStats iConn
        {
            get
            {
                return blnConn;
            }
            set
            {
                blnConn = value;

                switch (blnConn)
                {
                    case GEMCommStats.DISABLE:
                        if (m_bFreeStyle)
                        {
                            this.lblConn.BackColor = Color.FromArgb(149,149,149);
                            this.lblConn.Text = "Disable";
                        }
                        else
                        {
                            this.lblConn.BackColor = Color.Red;
                            this.lblConn.Text = "Disable";
                        }
                        break;
                    case GEMCommStats.COMMUNICATION:
                        this.lblConn.BackColor = Color.Lime;
                        this.lblConn.Text = "Connect";
                        break;
                    case GEMCommStats.NOTCOMMUNICATION:
                        this.lblConn.BackColor = Color.Yellow;
                        this.lblConn.Text = "Not Connect";
                        break;

                }

            }
        }

        public GEMControlStats icontrol
        {
            get
            {
                return blncontrol;
            }
            set
            {
                blncontrol = value;
                switch (blncontrol)
                {
                    case GEMControlStats.OFFLINE:
                    case GEMControlStats.HOSTOFFLINE:
                    case GEMControlStats.EQUIPMENTOFFLINE:
                        if (m_bFreeStyle)
                        {
                            lblONL.BackColor = Color.FromArgb(149,149,149);
                            lblONL.Text = "OFFLINE";
                        }
                        else
                        {
                            lblONL.BackColor = Color.Red;
                            lblONL.Text = "OFFLINE";
                        }
                        break;

                    case GEMControlStats.ATTEMTPONLINE:
                        lblONL.BackColor = SystemColors.Control;
                        lblONL.Text = "ATTEMTPONLINE";
                        break;
                    case GEMControlStats.ONLINELOCAL:
                        lblONL.BackColor = Color.Yellow;
                        lblONL.Text = "ONLINELOCAL";
                        break;

                    case GEMControlStats.ONLINEREMOTE:
                        lblONL.BackColor = Color.Lime;
                        lblONL.Text = "ONLINEREMOTE";

                        break;

                }


            }

        }

        public GEMProcessStats iProcessStats
        {
            get { return blnProcess; }
            set
            {
                blnProcess = value;
                switch (blnProcess)
                {
                    case GEMProcessStats.Init:
                        lblProcessStats.BackColor = Color.Red;
                        lblProcessStats.Text = "Inital";
                        break;
                    case GEMProcessStats.Idle:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Idle";
                        break;
                    case GEMProcessStats.FOUPClamp:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Foup Clamp";
                        break;
                    case GEMProcessStats.FOUPDocking:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Foup Docking";
                        break;
                    case GEMProcessStats.FOUPReady:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Foup Ready";
                        break;
                    case GEMProcessStats.ComMID:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Compare MID";
                        break;
                    case GEMProcessStats.FunctionSetup:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Function Setup";
                        break;
                    case GEMProcessStats.FunctionSetupFail:
                        lblProcessStats.BackColor = Color.Red;
                        lblProcessStats.Text = "Function Setup Fail";
                        break;
                    case GEMProcessStats.Stop:
                        lblProcessStats.BackColor = Color.Red;
                        lblProcessStats.Text = "Stop";
                        break;
                    case GEMProcessStats.Pause:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Stop";
                        break;
                    case GEMProcessStats.Resume:
                        lblProcessStats.BackColor = Color.Lime;
                        lblProcessStats.Text = "Resume";
                        break;
                    case GEMProcessStats.Executing:
                        lblProcessStats.BackColor = Color.Lime;
                        lblProcessStats.Text = "Executing";
                        break;
                    case GEMProcessStats.Finish:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Finish";
                        break;
                    case GEMProcessStats.FOUPUnDock:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Foup UnDock";
                        break;
                    case GEMProcessStats.FOUPUnClamp:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "Foup UnClamp";
                        break;
                    case GEMProcessStats.PodReadyToMoveOut:
                        lblProcessStats.BackColor = Color.Yellow;
                        lblProcessStats.Text = "ReadyToMoveOut";
                        break;

                }
            }

        }


        private void lblConn_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pnlEQ3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void UICIMStatus_Load(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }


        bool m_bFreeStyle = false;
        public void SetFreeStyleColor(Color c)
        {
            m_bFreeStyle = true;

            labCommStats.ForeColor = Color.Black;
            labControlStats.ForeColor = Color.Black;

            labCommStats.BackColor = c;
            labControlStats.BackColor = c;

            lblConn.ForeColor = Color.Black;
            lblONL.ForeColor = Color.Black;

            lblConn.BackColor = Color.FromArgb(149,149,149);
            lblONL.BackColor = Color.FromArgb(149,149,149);
        }

    }
}
