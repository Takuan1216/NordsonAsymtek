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
    public partial class GUILight : UserControl
    {
        public enum enuStatus : int { eOff, eOn }

        public GUILight()
        {
            InitializeComponent();


        }

        private enuStatus _redStatus = enuStatus.eOn;
        private enuStatus _yellowStatus = enuStatus.eOn;
        private enuStatus _greenStatus = enuStatus.eOn;
        private enuStatus _blueStatus = enuStatus.eOn;

        public enuStatus LightRedStatus
        {
            get { return _redStatus; }
            set
            {
                if (_redStatus == value) return;
                _redStatus = value;
                pictureBoxRead.Image = _redStatus == enuStatus.eOn ? RorzeApi.Properties.Resources.icons8_red_square_17 : null;
                this.panelRed.BackColor = _redStatus == enuStatus.eOn ? Color.Red : Color.Silver;
                this.Refresh();
            }
        }
        public enuStatus LightYellowStatus
        {
            get { return _yellowStatus; }
            set
            {
                if (_yellowStatus == value) return;
                _yellowStatus = value;
                pictureBoxYellow.Image = _yellowStatus == enuStatus.eOn ? RorzeApi.Properties.Resources.icons8_yellow_square_17 : null;
                this.panelYellow.BackColor = _yellowStatus == enuStatus.eOn ? Color.Yellow : Color.Silver;
                this.Refresh();
            }
        }
        public enuStatus LightGreenStatus
        {
            get { return _greenStatus; }
            set
            {
                if (_greenStatus == value) return;
                _greenStatus = value;
                pictureBoxGreen.Image = _greenStatus == enuStatus.eOn ? RorzeApi.Properties.Resources.icons8_green_square_17 : null;
                this.panelGreen.BackColor = _greenStatus == enuStatus.eOn ? Color.Green : Color.Silver;
                this.Refresh();
            }
        }
        public enuStatus LightBlueStatus
        {
            get { return _blueStatus; }
            set
            {
                if (_blueStatus == value) return;
                _blueStatus = value;
                pictureBoxBlue.Image = _blueStatus == enuStatus.eOn ? RorzeApi.Properties.Resources.icons8_blue_square_17 : null;
                this.panelBlue.BackColor = _blueStatus == enuStatus.eOn ? Color.Blue : Color.Silver;
                this.Refresh();
            }
        }
    }
}
