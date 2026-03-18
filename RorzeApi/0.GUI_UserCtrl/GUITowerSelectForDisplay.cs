using RorzeUnit.Class;
using RorzeUnit.Class.Loadport.Event;
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
    public partial class GUITowerSelectForDisplay : UserControl
    {

        List<CheckBox> ListCheckBox = new List<CheckBox>();

        public GUITowerSelectForDisplay()
        {
            InitializeComponent();
        }

        int m_nBodyNo;
        public int BodyNo
        {
            get { return m_nBodyNo; }
            set
            {
                m_nBodyNo = value;
                gbxName.Text = "Tower " + value;
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach(CheckBox item in tableLayoutPanel1.Controls)
            {
                item.Checked = true;
            }
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            foreach (CheckBox item in tableLayoutPanel1.Controls)
            {
                item.Checked = false;
            }
        }


        public string GetSelect()
        {
            //1~200/400

            string str = "";
            //str += cbxSlotArea1.Checked ? "1111111111111111111111111" : "0000000000000000000000000";
            //str += cbxSlotArea2.Checked ? "1111111111111111111111111" : "0000000000000000000000000";
            //str += cbxSlotArea3.Checked ? "1111111111111111111111111" : "0000000000000000000000000";
            //str += cbxSlotArea4.Checked ? "1111111111111111111111111" : "0000000000000000000000000";
            //str += cbxSlotArea5.Checked ? "1111111111111111111111111" : "0000000000000000000000000";
            //str += cbxSlotArea6.Checked ? "1111111111111111111111111" : "0000000000000000000000000";
            //str += cbxSlotArea7.Checked ? "1111111111111111111111111" : "0000000000000000000000000";
            //str += cbxSlotArea8.Checked ? "1111111111111111111111111" : "0000000000000000000000000";

            foreach(CheckBox item in ListCheckBox)
                str += item.Checked ? "1111111111111111111111111" : "0000000000000000000000000";

            return str;
        }


        int m_nSlot = 0;
        public int SetSlot
        {
            get { return m_nSlot; }
            set
            {
                if (m_nSlot == value) return;
                m_nSlot = value;
                TableLayoutPanel tlp = tableLayoutPanel1;

                tableLayoutPanel1.Controls.Clear();

                tlp.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tlp, true, null);

                tlp.SuspendLayout();
                tlp.RowStyles.Clear();
                tlp.ColumnStyles.Clear();

                tlp.RowCount = m_nSlot / 25 + 1;//用於表單建立層數
                tlp.ColumnCount = 1;

                for (int i = 0; i < tlp.RowCount - 1; i++)
                {
                    tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                }
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));

                tlp.AutoSize = true;
                tlp.Dock = DockStyle.Fill;
                tlp.Margin = new Padding(0);
                tlp.Padding = new Padding(0);

                ListCheckBox.Clear();
                for (int i = 0; i < tlp.RowCount - 1; i++)//注意建立順序 16 15 14 13 12 11 10
                {
                    CheckBox cbx = new CheckBox();
                    cbx.Text = string.Format("Slot {0:D3}~{1:D3}", (m_nSlot / 25 - i - 1) * 25 + 1, (m_nSlot / 25 - i) * 25);

                    ListCheckBox.Insert(0, cbx);

                    tlp.Controls.Add(cbx, 0, i);
                }
                tlp.ResumeLayout();


            }
        }
    }
}
