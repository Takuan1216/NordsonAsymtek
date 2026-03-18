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
    public partial class GUIAligner : UserControl
    {
        public bool haveWafer = false;
        private Rectangle _rectDisable;
        public enum enuWaferStatus : int { s0_Idle, s1_HaveWafer, s2_TunTableNoFrame, s3_TunTableHasFrame, s4_PanelAlignerHasPanel }
        private Dictionary<enuWaferStatus, Image> _dicColorWafer = new Dictionary<enuWaferStatus, Image>()
        {
            {enuWaferStatus.s0_Idle, RorzeApi.Properties.Resources.PanelAlignerNoPanel}, // HSC UI
            {enuWaferStatus.s1_HaveWafer,  RorzeApi.Properties.Resources.AllignerHasWafer}, 
            {enuWaferStatus.s2_TunTableNoFrame,  RorzeApi.Properties.Resources.TunTableNoFrame},
            {enuWaferStatus.s3_TunTableHasFrame,  RorzeApi.Properties.Resources.TunTableHasFrame},
            {enuWaferStatus.s4_PanelAlignerHasPanel,  RorzeApi.Properties.Resources.PanelAlignerHasPanel},// HSC UI
        };

        public GUIAligner()
        {
            InitializeComponent();
            this.Paint += GUIAligner_Paint;
            _rectDisable = new Rectangle(_dicColorWafer[enuWaferStatus.s0_Idle].Width / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Height / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Width / 10000,
                _dicColorWafer[enuWaferStatus.s0_Idle].Height / 10000
                );
        }
        private enuWaferStatus _alignerStatus = enuWaferStatus.s0_Idle;
        public enuWaferStatus AlignerStatus
        {
            get { return _alignerStatus; }
            set
            {
                if (_alignerStatus == value) return;
                _alignerStatus = value;
                this.Refresh();
            }
        }


        private void GUIAligner_Paint(object sender, PaintEventArgs e)
        {
            //e.Graphics.DrawImage(_dicColorWafer[AlignerStatus], 0, 0);
            e.Graphics.DrawImage(_dicColorWafer[AlignerStatus],
                        new Rectangle(0, 0, this.Width, this.Height),
                        new Rectangle(0, 0, _dicColorWafer[AlignerStatus].Width, _dicColorWafer[AlignerStatus].Height),
                           GraphicsUnit.Pixel);
        }
    }
}
