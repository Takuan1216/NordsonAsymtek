using RorzeApi._0.GUI_UserCtrl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RorzeApi
{
    public partial class frmStockSelectSlot : Form
    {
        private List<string> m_listSelect = new List<string>();

        public frmStockSelectSlot()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            //string
            m_listSelect.Clear();

            m_listSelect.Add(guiTowerSelectForDisplay1.GetSelect());
            m_listSelect.Add(guiTowerSelectForDisplay2.GetSelect());
            m_listSelect.Add(guiTowerSelectForDisplay3.GetSelect());
            m_listSelect.Add(guiTowerSelectForDisplay4.GetSelect());
            m_listSelect.Add(guiTowerSelectForDisplay5.GetSelect());
            m_listSelect.Add(guiTowerSelectForDisplay6.GetSelect());
            m_listSelect.Add(guiTowerSelectForDisplay7.GetSelect());
            m_listSelect.Add(guiTowerSelectForDisplay8.GetSelect());

            DialogResult = DialogResult.OK;
            Close();
        }

        public List<string> GetSelect()
        {
            return m_listSelect;
        }
    }
}
