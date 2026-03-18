using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RorzeApi.GUI
{
    public partial class GUIRobotArm : UserControl
    {
        public bool haveWafer = false;
        private Rectangle _rectDisable;
        public enum enuWaferStatus : int { s0_Idle, s1_HaveWafer, s2_FingerNoFrame, s3_FingerHaveFrame, s4_FingerNoWafer_I, s5_FingerHaveWafer_I, s6_FingerHavePanel }
        private Dictionary<enuWaferStatus, Image> _dicColorWafer = new Dictionary<enuWaferStatus, Image>()
        {
            {enuWaferStatus.s0_Idle, RorzeApi.Properties.Resources. FingerNoPanel}, // HSC UI
            {enuWaferStatus.s1_HaveWafer,  RorzeApi.Properties.Resources.FingerHasWafer},
            {enuWaferStatus.s2_FingerNoFrame,  RorzeApi.Properties.Resources.FingerNoFrame},
            {enuWaferStatus.s3_FingerHaveFrame,  RorzeApi.Properties.Resources.FingerHasFrame},
            {enuWaferStatus.s4_FingerNoWafer_I,  RorzeApi.Properties.Resources.FingerNoWafer_I},
            {enuWaferStatus.s5_FingerHaveWafer_I,  RorzeApi.Properties.Resources.FingerHasWafer},
            {enuWaferStatus.s6_FingerHavePanel,  RorzeApi.Properties.Resources.FingerHasPanel}, // HSC UI
        };

        public GUIRobotArm()
        {
            InitializeComponent();
            this.Paint += GUIRobotArm_Paint;
            _rectDisable = new Rectangle(
                _dicColorWafer[enuWaferStatus.s0_Idle].Width / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Height / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Width / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Height / 10000
                );

        }
        private enuWaferStatus _armStatus = enuWaferStatus.s0_Idle;
        public enuWaferStatus ArmStatus
        {
            get { return _armStatus; }
            set
            {
                if (_armStatus == value) return;
                _armStatus = value;
                this.Refresh();
            }
        }
        private void GUIRobotArm_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(_dicColorWafer[ArmStatus], 
                new Rectangle(0, 0, this.Width, this.Height),
                new Rectangle(0, 0, _dicColorWafer[ArmStatus].Width, _dicColorWafer[ArmStatus].Height),
                GraphicsUnit.Pixel);
            //e.Graphics.DrawString(m_strSlotNo, new Font("Calibri", 8), new SolidBrush(Color.Black), 0, 2);
        }
        private string m_strSlotNo;
        public string SetWaferSlotNo
        {
            set
            {
                if (value == m_strSlotNo) { return; }
                m_strSlotNo = value;
                this.Refresh();
            }
        }
    }
}
