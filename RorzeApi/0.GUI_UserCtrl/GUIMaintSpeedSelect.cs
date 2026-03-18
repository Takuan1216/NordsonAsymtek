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
    public partial class GUIMaintSpeedSelect : UserControl
    {
        public int m_nSpeed5to100;

        public GUIMaintSpeedSelect(int nSpeed5to100)//
        {
            InitializeComponent();

            if (nSpeed5to100 / 5 == 0)
                m_nSpeed5to100 = 5;
            else if (nSpeed5to100 / 5 > 20)
                m_nSpeed5to100 = 100;
            else
                m_nSpeed5to100 = nSpeed5to100 / 5 * 5;



            trbRobotSpeed.Value = m_nSpeed5to100 / 5;
            lblRobotSpeed.Text = m_nSpeed5to100 == 0 ? "100" : (m_nSpeed5to100).ToString();
        }

        private void trbRobotSpeed_MouseUp(object sender, MouseEventArgs e)
        {
            //int _nSpeed = trbRobotSpeed.Value;
        }

        private void trbRobotSpeed_Scroll(object sender, EventArgs e)
        {
            m_nSpeed5to100 = trbRobotSpeed.Value * 5;//(1~20)*5
            lblRobotSpeed.Text = m_nSpeed5to100 == 0 ? "100" : (m_nSpeed5to100).ToString();
        }
    }
}
