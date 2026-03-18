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
    public partial class GUIEquipment : UserControl
    {
        public bool haveWafer = false;
        private Rectangle _rectDisable;
        public enum enuWaferStatus : int { s0_Idle, s1_HaveWafer, s2_HaveFRAME, s3_HavePanel }
        private Dictionary<enuWaferStatus, Image> _dicColorWafer = new Dictionary<enuWaferStatus, Image>()
        {
            {enuWaferStatus.s0_Idle, RorzeApi.Properties.Resources.RorzeNoPanel}, // HSC GUI
            {enuWaferStatus.s1_HaveWafer,  RorzeApi.Properties.Resources.Wafer},
            {enuWaferStatus.s2_HaveFRAME,  RorzeApi.Properties.Resources.Frame},
            {enuWaferStatus.s3_HavePanel,  RorzeApi.Properties.Resources.RorzePanel},
        };

        public GUIEquipment()
        {
            InitializeComponent();
            this.Paint += GUIEquipment_Paint;
            _rectDisable = new Rectangle(_dicColorWafer[enuWaferStatus.s0_Idle].Width / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Height / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Width / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Height / 10000
                );
        }
        private enuWaferStatus _equipmentStatus = enuWaferStatus.s0_Idle;
        public enuWaferStatus EquipmentStatus
        {
            get { return _equipmentStatus; }
            set
            {
                if (_equipmentStatus == value) return;
                _equipmentStatus = value;
                this.Refresh();
            }
        }


        private void GUIEquipment_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(_dicColorWafer[EquipmentStatus],
             new Rectangle(0, 0, this.Width, this.Height),
             new Rectangle(0, 0, _dicColorWafer[EquipmentStatus].Width, _dicColorWafer[EquipmentStatus].Height),
                GraphicsUnit.Pixel);
            e.Graphics.DrawString(m_strSlotNo, new Font("Calibri", 8), new SolidBrush(Color.Black), 14, 2);
        }
        private string m_strSlotNo;
        public string WaferSlotNo
        {
            get { return m_strSlotNo; }
            set
            {
                if (value == m_strSlotNo) { return; }
                m_strSlotNo = value;
                this.Refresh();
            }
        }
    }
}
