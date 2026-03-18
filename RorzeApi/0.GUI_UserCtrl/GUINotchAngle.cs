using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RorzeApi.GUI
{
    public partial class GUINotchAngle : UserControl
    {


        public enum enumType { Wafer, Panel }

        public event EventHandler<double> OnAngleChange;//Button

        List<Button> lstButton = new List<Button>();

        enumType m_eType = enumType.Wafer;

        double m_dAngle = -1;

        public GUINotchAngle()
        {
            InitializeComponent();
            lstButton.Add(btn0);
            lstButton.Add(btn45);
            lstButton.Add(btn90);
            lstButton.Add(btn135);
            lstButton.Add(btn180);
            lstButton.Add(btn225);
            lstButton.Add(btn270);
            lstButton.Add(btn315);

            lstButton.Add(btn0_panel);
            lstButton.Add(btn90_panel);
            lstButton.Add(btn180_panel);
            lstButton.Add(btn270_panel);

            //隱藏page,消失頁籤
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.Appearance = TabAppearance.FlatButtons;
        }


        public enumType _Type
        {
            get { return m_eType; }
            set
            {
                if (m_eType != value)
                {
                    m_eType = value;

                    switch (m_eType)
                    {
                        case enumType.Wafer:
                            tabControl1.SelectedTab = tabPageWafer;
                            break;
                        case enumType.Panel:
                            tabControl1.SelectedTab = tabPagePanel;
                            break;
                    }

                }
            }
        }


        public double _Angle
        {
            get { return m_dAngle; }
            set
            {
                if (m_dAngle != value)
                {
                    m_dAngle = value;
                    foreach (Button bt in lstButton)
                        bt.BackColor = (bt.Text == ((int)value).ToString()) ? m_cSelect : SystemColors.Control;

                    tbxCustomize.Text = m_dAngle.ToString();
                    OnAngleChange?.Invoke(null, m_dAngle);

                }
            }
        }


        private void btnAngle_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;

            if (tbxCustomize.Visible)
            {
                tbxCustomize.Visible = false;
            }

            _Angle = double.Parse(btn.Text);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            tbxCustomize.Visible = !tbxCustomize.Visible;

            _Angle = 0;
        }

        private void tbxCustomize_KeyUp(object sender, KeyEventArgs e)
        {
            double dAngle;
            if (double.TryParse(tbxCustomize.Text, out dAngle) == false)
            {
                dAngle = 0;
            }

            if (dAngle < 0 || dAngle > 359)
            {
                dAngle = 0;
            }

            _Angle = dAngle;
        }

        public void EventTriggerBtn(int angle)
        {
            switch (angle)
            {
                case 0:
                    btn0.PerformClick();
                    break;
                case 45:
                    btn45.PerformClick();
                    break;
                case 90:
                    btn90.PerformClick();
                    break;
                case 135:
                    btn135.PerformClick();
                    break;
                case 180:
                    btn180.PerformClick();
                    break;
                case 225:
                    btn225.PerformClick();
                    break;
                case 270:
                    btn270.PerformClick();
                    break;
                case 315:
                    btn315.PerformClick();
                    break;
                default:
                    break;

            }
        }

        bool m_bFreeStyle;
        Color m_cSelect = Color.LightBlue;
        public void SetFreeStyleColor(Color cSelect)
        {
            m_bFreeStyle = true;
            m_cSelect = cSelect;
            foreach (Button bt in lstButton)
                bt.BackColor = (bt.Text == ((int)0).ToString()) ? m_cSelect : SystemColors.Control;

            btn0.Image = RorzeApi.Properties.Resources.circled_arrow_0_48_;
            btn45.Image = RorzeApi.Properties.Resources.circled_arrow_45_48_;
            btn90.Image = RorzeApi.Properties.Resources.circled_arrow_90_48_;
            btn135.Image = RorzeApi.Properties.Resources.circled_arrow_135_48_;
            btn180.Image = RorzeApi.Properties.Resources.circled_arrow_180_48_;
            btn225.Image = RorzeApi.Properties.Resources.circled_arrow_225_48_;
            btn270.Image = RorzeApi.Properties.Resources.circled_arrow_270_48_;
            btn315.Image = RorzeApi.Properties.Resources.circled_arrow_315_48_;
            pictureBox1.Image = RorzeApi.Properties.Resources.FoupTopView_;


        }

    }
}
