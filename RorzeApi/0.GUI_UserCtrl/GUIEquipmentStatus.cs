using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RorzeApi._0.GUI_UserCtrl
{
    public partial class GUIEquipmentStatus : UserControl
    {
        public GUIEquipmentStatus()
        {
            InitializeComponent();
        }
        public string EQName
        {
            get { return lblEQName.Text; }
            set
            {
                if (value == lblEQName.Text) { return; }
                lblEQName.Text = value;
                this.Refresh();
            }
        }
        public string SetWaferSlotNo
        {
            set
            {
                if (value == lblSlotNo.Text) { return; }
                lblSlotNo.Text = value;
                this.Refresh();
            }
        }   
        public string SetWaferRecipe
        {
            set
            {
                if (value == lblRecipe.Text) { return; }
                lblRecipe.Text = value;
                this.Refresh();
            }
        }
        public enum enumState
        {
            [Description("NOTREADY")]
            Unknown = 0,
            [Description("READY")]
            Idle = 1,
            [Description("ERROR")]
            Error = 2,
            [Description("RUN")]
            Moving = 3,
            [Description("Disable")]
            Disable = 4,
            [Description("Disconnect")]
            Disconnect = 5
        }
        private string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if ((attributes != null) && (attributes.Length > 0))
                return attributes[0].Description;
            else
                return value.ToString();
        }
        public enumState SetStatus
        {
            set
            {
                if (GetEnumDescription(value) == lblState.Text) { return; }
                lblState.Text = GetEnumDescription(value);
                switch (value)
                {
                    case enumState.Idle:
                        //lblState.Text = "READY";
                        lblState.ForeColor = Color.White;
                        lblState.BackColor = Color.Green;
                        break;
                    case enumState.Moving:
                        //lblState.Text = "RUN";
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Cyan;
                        break;
                    case enumState.Unknown:
                        //lblState.Text = "NOTREADY";
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Yellow;
                        break;
                    case enumState.Error:
                        //lblState.Text = "ERROR";
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Red;
                        break;
                }
                this.Refresh();
            }
        }
        public GUI.GUIEquipment.enuWaferStatus SetWaferStatus
        {
            set
            {
                if (value == guiEquipment.EquipmentStatus) { return; }
                guiEquipment.EquipmentStatus = value;
                this.Refresh();
            }
        }
      






    }
}
