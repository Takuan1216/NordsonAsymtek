using RorzeUnit.Class;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace RorzeApi.GUI
{
    public partial class GUITower : UserControl
    {
        Dictionary<string, string> m_DicAllLanguageTranfer = new Dictionary<string, string>();
        public class LbWafer : Label
        {
            public int TowerFaceIndex = 0;
            public bool IsWaferOn = false;
            public enumUIPickWaferStat WaferSts = enumUIPickWaferStat.None;
            public int Slot = -1;//畫第幾片 1,2,3,4,5,6,7,8,9,10
            public bool SelectFlag = false;//反白select用
            public string ShowText = string.Empty;//顯示文字                 
            public double NotchAngle = 0.0;

            public string PrepareFromName;//選片顯示
            public string PrepareToName;//選片顯示
        }

        public event EventHandler<DataGridViewCellEventArgs> OnSlotLabelMouseEnter;
        public event EventHandler<DataGridViewCellEventArgs> OnSlotLabelMouseLeave;
        public event EventHandler<DataGridViewCellEventArgs> OnDataGridView1_CellClick;

        public enum enumUIGetPutFlag { None = 0, Get, Put };

        Color m_cWaferOn = Color.LimeGreen;//enumUIPickWaferStat.HasWafer
        Color cExeHasWafer = Color.LightGray;//enumUIPickWaferStat.ExeHasWafer
        Color m_cPutWafer = Color.PaleGreen;//enumUIPickWaferStat.PutWafer
        Color cPutWaferAndGet = Color.Blue;//enumUIPickWaferStat.PutWaferAndGet
        Color m_cExeHasWaferAndPut = Color.Salmon;//enumUIPickWaferStat.ExeHasWaferAndPut
        Color cNoWafer = Color.Transparent;//enumUIPickWaferStat.NoWafer
        Color cNullslot = Color.Silver;//只有13層的CAS 14~25反灰

        List<LbWafer> ListStockSlot = new List<LbWafer>();//800or1600片
        List<SWafer.enumProcessStatus> lstWaferProcessStatus = new List<SWafer.enumProcessStatus>();//800or1600片

        int m_nStockTowerTotal = 4; //Tower有4個面
        int m_nTowerSlotTotal = 200;

        public GUITower()
        {
            InitializeComponent();
            CreateStageGUI();
            m_DicAllLanguageTranfer.Add("Tower", "储存塔");
        }

        private void CreateStageGUI()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            for (int i = 0; i < m_nTowerSlotTotal; i++)//200or400
            {
                DataGridViewRow row = new DataGridViewRow();
                row.HeaderCell.Value = (m_nTowerSlotTotal - i).ToString("D3");
                dataGridView1.Rows.Add(row);
            }

            ListStockSlot.Clear();
            lstWaferProcessStatus.Clear();
            for (int i = 0; i < dataGridView1.ColumnCount; i++)//0,1,2,3
            {
                for (int j = 0; j < dataGridView1.RowCount; j++)//0,1,2,3,4...200or400
                {
                    dataGridView1.Rows[m_nTowerSlotTotal - j - 1].Cells[i].Value = "Slot" + (j + 1).ToString("D3");

                    LbWafer labelWaferStatus = new LbWafer();
                    labelWaferStatus.BackColor = SystemColors.Control;
                    labelWaferStatus.Dock = DockStyle.Fill;
                    labelWaferStatus.Margin = new Padding(0);
                    //labelWaferStatus.Name = string.Format("lbStg{0:D2}Slot{1:D3}", i, j);
                    labelWaferStatus.TowerFaceIndex = i;
                    labelWaferStatus.Slot = 1 + j;
                    labelWaferStatus.SelectFlag = false;
                    ListStockSlot.Add(labelWaferStatus);
                    lstWaferProcessStatus.Add(new SWafer.enumProcessStatus());
                }
            }
        }

        #region ========== select Wafer 方法 ==========

        /// <summary>
        /// User點選tower diaplay功能，UI對應顯示
        /// </summary>
        /// <param name="strStockSelect">count:800or1600</param>
        /// <remarks>一次800or1600片</remarks>
        public void PickPlaceForDisplay(string[] strStockSelect, double dAngle = -1)
        {
            string strSelect = "";
            foreach (string item in strStockSelect) strSelect += item;//0~799or1599
            string strMapData = "";
            foreach (string item in m_strStockMapData) strMapData += item;//0~799or1599

            for (int i = 0; i < m_nStockTowerTotal * m_nTowerSlotTotal; i++)//0~799or1599
            {
                if (strSelect[i] == '1' && strMapData[i] == '1')
                {
                    int nTowerNumber = m_nStockTowerTotal * (BodyNo - 1) + i / m_nTowerSlotTotal + 1;//1,2,...16
                    int nTowerSlot = i % m_nTowerSlotTotal + 1;//1,2,...200or400
                    string strToName = string.Format("T{0:D2}S{1:D3}", nTowerNumber, nTowerSlot);
                    ListStockSlot[i].WaferSts = enumUIPickWaferStat.ExeHasWaferAndPut;
                    ListStockSlot[i].SelectFlag = false;
                    ListStockSlot[i].NotchAngle = dAngle;
                    ListStockSlot[i].PrepareFromName = strToName;//選片顯示名稱
                    ListStockSlot[i].ShowText = "";
                }
            }
            dataGridView1.Refresh();
        }
        /// <summary>
        /// User選擇傳送，UI對應顯示，只更動單Slot
        /// </summary>
        /// <param name="strFromName"></param>
        /// <param name="nFaceIndx">第幾面</param>
        /// <param name="nTowerSlotIndx">第幾層</param>
        /// <param name="dAngle"></param>
        public void PlaceWaferInLoadport(string strFromName, int nFaceIndx, int nTowerSlotIndx, double dAngle = -1)
        {
            //sourceLp:如果之後有要UI顯示source的話可以用
            int nStockSoltIndx = nFaceIndx * m_nTowerSlotTotal + nTowerSlotIndx;//799or1599
            switch (ListStockSlot[nStockSoltIndx].WaferSts)
            {
                case enumUIPickWaferStat.NoWafer:
                    #region  NoWafer
                    {
                        ListStockSlot[nStockSoltIndx].WaferSts = enumUIPickWaferStat.PutWafer;
                        ListStockSlot[nStockSoltIndx].SelectFlag = false;
                        ListStockSlot[nStockSoltIndx].NotchAngle = dAngle;
                        ListStockSlot[nStockSoltIndx].PrepareFromName = strFromName;//選片顯示名稱
                    }
                    break;
                #endregion
                case enumUIPickWaferStat.ExeHasWafer:
                    #region  HasWafer
                    {
                        ListStockSlot[nStockSoltIndx].WaferSts = enumUIPickWaferStat.ExeHasWaferAndPut;
                        ListStockSlot[nStockSoltIndx].SelectFlag = false;
                        ListStockSlot[nStockSoltIndx].NotchAngle = dAngle;
                        ListStockSlot[nStockSoltIndx].PrepareFromName = strFromName;//選片顯示名稱
                    }
                    break;
                #endregion
                default:
                    {
                        ListStockSlot[nStockSoltIndx].PrepareFromName = "";
                    }
                    break;
            }
            dataGridView1.Refresh();
        }
        /// <summary>
        /// User選擇傳送，UI對應顯示，只更動單Slot
        /// </summary>
        /// <param name="strToName"></param>
        /// <param name="nFaceIndx">第幾面</param>
        /// <param name="nTowerSlotIndx">第幾層</param>
        public void ResetSlotSelectFlag(string strToName, int nFaceIndx, int nTowerSlotIndx)
        {
            int nStockSoltIndx = nFaceIndx * m_nTowerSlotTotal + nTowerSlotIndx;//799or1599
            ListStockSlot[nStockSoltIndx].SelectFlag = false;
            switch (ListStockSlot[nStockSoltIndx].WaferSts)
            {
                case enumUIPickWaferStat.HasWafer:
                    #region  HasWafer
                    {
                        ListStockSlot[nStockSoltIndx].WaferSts = enumUIPickWaferStat.ExeHasWafer;
                        ListStockSlot[nStockSoltIndx].PrepareToName = strToName;//選片顯示名稱
                        ListStockSlot[nStockSoltIndx].ShowText = "";
                    }
                    break;
                #endregion
                case enumUIPickWaferStat.PutWafer:
                    #region  PutWafer
                    {
                        ListStockSlot[nStockSoltIndx].WaferSts = enumUIPickWaferStat.PutWaferAndGet;
                        ListStockSlot[nStockSoltIndx].PrepareToName = strToName;//選片顯示名稱
                        ListStockSlot[nStockSoltIndx].ShowText = "";
                    }
                    break;
                    #endregion
            }
            ListStockSlot[nStockSoltIndx].NotchAngle = -1;
            dataGridView1.Refresh();
        }

        #endregion


        //-----------------------------------------------------------------------------
        private string[] m_strStockMapData = new string[4]
        {
            "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
            "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
            "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
            "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"
        };
        int m_nBodyNo = 1;
        public int BodyNo
        {
            get { return m_nBodyNo; }
            set
            {
                m_nBodyNo = value;
                //1-> 1,2,3,4 
                //2-> 5,6,7,8 
                //3-> 9,10,11,12 
                //4-> 13,14,15,16             
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    dataGridView1.Columns[i].HeaderText = GetLanguage("Tower") + (m_nStockTowerTotal * (value - 1) + 1 + i).ToString("D2");
                }
            }
        }

        bool m_bSimulate;
        public bool Simulate
        {
            get { return m_bSimulate; }
            set { m_bSimulate = value; }
        }

        /// <summary>
        /// 獲取Stock單Slot有無
        /// </summary>
        /// <param name="nStockSlotIndx">0~799or1599</param>
        /// <returns>'1' or '0' or '23456789'</returns>
        private char GetStockMapData(int nStockSlotIndx)
        {
            char strData;
            int nFaceIndx = nStockSlotIndx / m_nTowerSlotTotal;
            int nTowerSlotIndx = nStockSlotIndx % m_nTowerSlotTotal;
            lock (this) { strData = m_strStockMapData[nFaceIndx][nTowerSlotIndx]; }
            return strData;
        }
        /// <summary>
        /// 修改全部修改Map資料
        /// </summary>
        /// <param name="strMapData">800or1600</param>
        /// <remarks>4*(200or400)=800or1600</remarks>
        private void SetStockMapData(string strMapData)
        {
            lock (this)
            {
                if (strMapData.Length == m_nTowerSlotTotal * m_nStockTowerTotal)//4*(200or400)=800or1600
                {
                    for (int i = 0; i < m_nStockTowerTotal; i++)//0,1,2,3
                    {
                        //0:0~200, 1:200~400, 2:400~600,  3:600~800
                        //0:0~400, 1:400~800, 2:800~1200, 3:1200~1600
                        m_strStockMapData[i] = strMapData.Substring(i * m_nTowerSlotTotal, m_nTowerSlotTotal);
                    }
                }
            }
        }
        /// <summary>
        /// 修改單片Map資料
        /// </summary>
        /// <param name="nFaceIndex">0,1,2,3</param>
        /// <param name="nTowerSlotIndx">0,1,...199or399</param>
        /// <param name="bWaferOn"></param>
        private void SetStockMapData(int nFaceIndex, int nTowerSlotIndx, bool bWaferOn)
        {
            lock (this)
            {
                string strTowerMapData = m_strStockMapData[nFaceIndex];//取出資料
                strTowerMapData = strTowerMapData.Remove(nTowerSlotIndx, 1);//移除1個
                strTowerMapData = strTowerMapData.Insert(nTowerSlotIndx, bWaferOn ? "1" : "0");//加入1個
                m_strStockMapData[nFaceIndex] = strTowerMapData;
            }
        }
        /// <summary>
        /// Mapping 完成事件，Stock全部的wafer
        /// </summary>
        /// <param name="strMapData">800or1600</param>
        /// <remarks>4*(200or400)=800or1600 Stock全部的wafer</remarks>
        public void UpdataStockMappingData(string strMapData)//更新GUI Mapping 完成後 更新表單
        {
            SetStockMapData(strMapData);//儲存資料，用於clear選取紀錄
            for (int i = 0; i < strMapData.Length; i++)//0~799or1599
            {
                int nFaceIndx = i / m_nTowerSlotTotal;
                int nTowerSlot = i / m_nTowerSlotTotal + 1;
                ListStockSlot[i].SelectFlag = false;
                ListStockSlot[i].Enabled = true;

                switch (strMapData[i])
                {
                    case '0':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.None;
                        ListStockSlot[i].WaferSts = enumUIPickWaferStat.NoWafer;
                        ListStockSlot[i].IsWaferOn = false;
                        UpdataWaferStatus(nFaceIndx, nTowerSlot);//mapping updata
                        break;
                    case '1':
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Sleep;
                        ListStockSlot[i].WaferSts = enumUIPickWaferStat.HasWafer;
                        ListStockSlot[i].IsWaferOn = true;
                        break;
                    default:
                        lstWaferProcessStatus[i] = SWafer.enumProcessStatus.Error;
                        ListStockSlot[i].WaferSts = enumUIPickWaferStat.HasWafer;
                        ListStockSlot[i].IsWaferOn = true;
                        UpdataWaferStatus(nFaceIndx, nTowerSlot);//mapping updata
                        break;
                }
            }
            dataGridView1.Refresh();
        }
        /// <summary>
        /// 指定塔與slot更改單片狀態
        /// </summary>
        /// <param name="nFaceIndex">0,1,2,3</param>
        /// <param name="nTowerSlot">1,2,3...,200or400</param>
        /// <param name="strM12"></param>
        /// <param name="strT7"></param>
        /// <param name="Pos"></param>
        /// <remarks>FaceIndex:0,1,2,3|nTowerSlot:1,2,3...,200or400</remarks>
        public void UpdataWaferStatus(int nFaceIndex, int nTowerSlot, string strM12 = "", string strT7 = "", SWafer.enumPosition Pos = SWafer.enumPosition.UnKnow)//  更新GUI slot狀態_顯示文字
        {
            // 0~799or1599
            int nStockSlotIndx = nFaceIndex * (m_nTowerSlotTotal) + nTowerSlot - 1;
            switch (lstWaferProcessStatus[nStockSlotIndx])
            {
                case SWafer.enumProcessStatus.WaitProcess:
                    ListStockSlot[nStockSlotIndx].ShowText = "WaitProcess";
                    break;
                case SWafer.enumProcessStatus.Processing:
                    if (Pos == SWafer.enumPosition.AlignerA || Pos == SWafer.enumPosition.AlignerB)
                        ListStockSlot[nStockSlotIndx].ShowText = "Alignment";
                    else
                        ListStockSlot[nStockSlotIndx].ShowText = "Transfering";
                    break;
                case SWafer.enumProcessStatus.Processed:
                    ListStockSlot[nStockSlotIndx].ShowText = "Processed";
                    break;
                case SWafer.enumProcessStatus.Abort:
                    ListStockSlot[nStockSlotIndx].ShowText = "Abort";
                    break;
                case SWafer.enumProcessStatus.Error:
                    {
                        if (GetStockMapData(nStockSlotIndx) == '2') ListStockSlot[nStockSlotIndx].ShowText = "Thickness Wafer";
                        else if (GetStockMapData(nStockSlotIndx) == '3') ListStockSlot[nStockSlotIndx].ShowText = "Cross Wafer";
                        else if (GetStockMapData(nStockSlotIndx) == '4') ListStockSlot[nStockSlotIndx].ShowText = "Front Bow Wafer";
                        else if (GetStockMapData(nStockSlotIndx) == '7') ListStockSlot[nStockSlotIndx].ShowText = "Double Wafer";
                        else if (GetStockMapData(nStockSlotIndx) == '8') ListStockSlot[nStockSlotIndx].ShowText = "Thickness Wafer";
                        else if (GetStockMapData(nStockSlotIndx) == '9') ListStockSlot[nStockSlotIndx].ShowText = "Mapping Failure";
                        else ListStockSlot[nStockSlotIndx].ShowText = "Unknow";
                    }
                    break;
                default:
                    ListStockSlot[nStockSlotIndx].ShowText = "";
                    break;
            }
        }
        /// <summary>
        /// 指定塔與slot更改單片狀態
        /// </summary>
        /// <param name="nFaceIndex"></param>
        /// <param name="nTowerSlot">1~200/400</param>
        /// <param name="status"></param>
        /// <param name="cStatus"></param>
        public void UpdataWaferProcessStatus(int nFaceIndex, int nTowerSlot, SWafer.enumProcessStatus status, Color cStatus)
        {
            // 0~799or1599
            int nStockSlotIndx = nFaceIndex * (m_nTowerSlotTotal) + nTowerSlot - 1;
            //為了選擇WAFER變色，外層有Timer
            if (lstWaferProcessStatus[nStockSlotIndx] == status) return;
            lstWaferProcessStatus[nStockSlotIndx] = status;
            switch (status)
            {
                case SWafer.enumProcessStatus.None:
                    ListStockSlot[nStockSlotIndx].WaferSts = enumUIPickWaferStat.NoWafer;
                    break;
                case SWafer.enumProcessStatus.Sleep:
                    //ListStockSlot[nStockSlotIndx].BackColor = cWaferOn;
                    break;
                case SWafer.enumProcessStatus.WaitProcess:
                    //ListStockSlot[nStockSlotIndx].BackColor = cWaitProcess;
                    break;
                case SWafer.enumProcessStatus.Processing:
                    ListStockSlot[nStockSlotIndx].WaferSts = enumUIPickWaferStat.NoWafer;
                    break;
                case SWafer.enumProcessStatus.Processed:
                    ListStockSlot[nStockSlotIndx].WaferSts = enumUIPickWaferStat.HasWafer;
                    break;
                case SWafer.enumProcessStatus.Abort:
                case SWafer.enumProcessStatus.Error:
                    //ListStockSlot[nStockSlotIndx].BackColor = cAlarm;                  
                    break;
            }
            dataGridView1.Refresh();//更新UI
        }
        /// <summary>
        /// 更新全部層數
        /// </summary>
        public void ResetUpdateMappingData()
        {
            for (int i = 0; i < m_nStockTowerTotal; i++)//0,1,2,3
            {
                string strMappingData = m_strStockMapData[i];
                for (int j = 0; j < m_nTowerSlotTotal; j++)//200or400片
                {
                    int nStockSlotIndx = i * (m_nTowerSlotTotal) + j;

                    if (j >= strMappingData.Length)//超過層的反灰
                    {
                        lstWaferProcessStatus[nStockSlotIndx] = SWafer.enumProcessStatus.Sleep;
                        ListStockSlot[nStockSlotIndx].BackColor = cNullslot;
                        ListStockSlot[nStockSlotIndx].Enabled = false;
                        continue;
                    }
                    ListStockSlot[nStockSlotIndx].SelectFlag = false;
                    ListStockSlot[nStockSlotIndx].Enabled = true;
                    switch (strMappingData[j])
                    {
                        case '0':
                            lstWaferProcessStatus[nStockSlotIndx] = SWafer.enumProcessStatus.None;
                            ListStockSlot[nStockSlotIndx].WaferSts = enumUIPickWaferStat.NoWafer;
                            ListStockSlot[nStockSlotIndx].IsWaferOn = false;
                            UpdataWaferStatus(i, j + 1);
                            break;
                        case '1':
                            lstWaferProcessStatus[nStockSlotIndx] = SWafer.enumProcessStatus.Sleep;
                            ListStockSlot[nStockSlotIndx].WaferSts = enumUIPickWaferStat.HasWafer;
                            ListStockSlot[nStockSlotIndx].IsWaferOn = true;
                            break;
                        default:
                            lstWaferProcessStatus[nStockSlotIndx] = SWafer.enumProcessStatus.Error;
                            ListStockSlot[nStockSlotIndx].WaferSts = enumUIPickWaferStat.HasWafer;
                            ListStockSlot[nStockSlotIndx].IsWaferOn = true;
                            UpdataWaferStatus(i, j + 1);
                            break;
                    }
                }
            }
            dataGridView1.Refresh();
        }
        //-----------------------------------------------------------------------------
        /// <summary>
        /// Robot take wafer out.
        /// </summary>
        /// <param name="nStge">stage number 1~400</param>
        /// <param name="nSlot1to25">slot number 1~25</param>
        /// <remarks>nStge:1~400, slot:1~25</remarks>
        public void TakeWaferOutFoup(int nStge, int nSlot1to25)
        {
            try
            {
                int n1 = nStge - 10;//因為塔的編號起始是11開始
                int nFaceIndx;//0,1,2,3 判斷第幾面     
                int nFloorIndx;//25片一個stage，單面上第幾個stage 0,1,2...8or16
                if (m_nTowerSlotTotal / 25 > 10)
                {
                    nFaceIndx = (n1 / 20) % m_nStockTowerTotal;//tower一面有400片
                    nFloorIndx = n1 % 20 - 1;
                }
                else
                {
                    nFaceIndx = (n1 / 10) % m_nStockTowerTotal;//tower一面有200片
                    nFloorIndx = n1 % 10 - 1;
                }
                int nTowerSlotIndx = nSlot1to25 + nFloorIndx * 25 - 1;//0~199or399
                int nStockSlotIndx = nFaceIndx * (m_nTowerSlotTotal) + nTowerSlotIndx;//0~799or1599
                SetStockMapData(nFaceIndx, nTowerSlotIndx, false);//修改mapping data
                ListStockSlot[nStockSlotIndx].WaferSts = enumUIPickWaferStat.NoWafer;
                dataGridView1.Refresh();//更新UI
            }
            catch { }

        }
        /// <summary>
        /// Robot take wafer in.
        /// </summary>
        /// <param name="nStge">stage number 1~400</param>
        /// <param name="nSlot1to25">slot number 1~25</param>
        /// <remarks>nStge:1~400, slot:1~25</remarks>
        public void TakeWaferInFoup(int nStge, int nSlot1to25)
        {
            try
            {
                int n1 = nStge - 10;//因為塔的編號起始是11開始
                int nFaceIndx;//0,1,2,3 判斷第幾面     
                int nFloorIndx;//25片一個stage，單面上第幾個stage 0,1,2...8or16
                if (m_nTowerSlotTotal / 25 > 10)
                {
                    nFaceIndx = (n1 / 20) % m_nStockTowerTotal;//tower一面有400片
                    nFloorIndx = n1 % 20 - 1;
                }
                else
                {
                    nFaceIndx = (n1 / 10) % m_nStockTowerTotal;//tower一面有200片
                    nFloorIndx = n1 % 10 - 1;
                }
                int nTowerSlotIndx = nSlot1to25 + nFloorIndx * 25 - 1;//0~199or399
                int nStockSlotIndx = nFaceIndx * (m_nTowerSlotTotal) + nTowerSlotIndx;//0~799or1599
                SetStockMapData(nFaceIndx, nTowerSlotIndx, true);//修改mapping data
                ListStockSlot[nStockSlotIndx].WaferSts = enumUIPickWaferStat.HasWafer;
                dataGridView1.Refresh();//更新UI
            }
            catch { }
        }
        //-----------------------------------------------------------------------------
        private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {

            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;


            e.PaintBackground(e.ClipBounds, true);
            //e.PaintContent(e.ClipBounds);

            int nStockSlotIndx = m_nTowerSlotTotal * e.ColumnIndex + (m_nTowerSlotTotal - e.RowIndex - 1);
            LbWafer lb = ListStockSlot[nStockSlotIndx];
            Color temp = cNoWafer;
            bool hasWafer = true;
            switch (lb.WaferSts)
            {
                case enumUIPickWaferStat.NoWafer:
                case enumUIPickWaferStat.None:
                    {
                        hasWafer = false;
                    }
                    break;
                case enumUIPickWaferStat.HasWafer:
                    {
                        temp = m_cWaferOn;
                        lb.ShowText = string.Format("T{0:D2}S{1:D3}", lb.TowerFaceIndex + 1 + (BodyNo - 1) * 4, lb.Slot);
                    }
                    break;
                case enumUIPickWaferStat.PutWafer: { temp = m_cPutWafer; } break;
                case enumUIPickWaferStat.ExeHasWafer: { temp = cExeHasWafer; } break;
                case enumUIPickWaferStat.PutWaferAndGet: { temp = cPutWaferAndGet; } break;
                case enumUIPickWaferStat.ExeHasWaferAndPut: { temp = m_cExeHasWaferAndPut; } break;
                default: { temp = Color.Black; } break;
            }

            if (lstWaferProcessStatus[nStockSlotIndx] == SWafer.enumProcessStatus.Error)
            {
                hasWafer = true;
                temp = Color.Red;
            }

            if (lb.SelectFlag)
            {
                hasWafer = true;
                temp = Color.Blue;
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            e.Graphics.FillEllipse(new SolidBrush(temp), new Rectangle(e.CellBounds.X + 1, e.CellBounds.Y + 1, e.CellBounds.Width - 4, e.CellBounds.Height - 4));
            if (hasWafer)
            {
                e.Graphics.DrawEllipse(new Pen(Brushes.Black, 0.87f), new Rectangle(e.CellBounds.X + 1, e.CellBounds.Y + 1, e.CellBounds.Width - 4, e.CellBounds.Height - 4));
            }

            if (lb.ShowText != null && lb.ShowText != "")
            {
                //Tower需不需要顯示字?
                e.Graphics.DrawString(lb.ShowText, new Font("Calibri", (e.CellBounds.Height - 0) / 2), new SolidBrush(Color.Black), e.CellBounds.X + 6, e.CellBounds.Y + 2);
            }

            switch (lb.WaferSts)//寫傳送的源頭與哪來
            {
                case enumUIPickWaferStat.None:
                case enumUIPickWaferStat.NoWafer:
                    break;
                case enumUIPickWaferStat.HasWafer:
                    break;
                case enumUIPickWaferStat.ExeHasWafer:
                    {
                        e.Graphics.DrawString(lb.PrepareToName, new Font("Calibri", e.CellBounds.Height / 2), new SolidBrush(Color.Red), e.CellBounds.X + 4, e.CellBounds.Y + 0);
                    }
                    break;
                case enumUIPickWaferStat.PutWaferAndGet:
                    break;
                case enumUIPickWaferStat.PutWafer:
                case enumUIPickWaferStat.ExeHasWaferAndPut:
                    {
                        e.Graphics.DrawString(lb.PrepareFromName, new Font("Calibri", e.CellBounds.Height / 2), new SolidBrush(Color.Red), e.CellBounds.X + 4, e.CellBounds.Y + 0);

                        if (lb.NotchAngle > -1)
                        {
                            double[] dAngle = new double[] { 0, 45, 90, 135, 180, 225, 270, 315 };

                            Bitmap[] bitmapArray = new Bitmap[]
                            {
                                Properties.Resources.arrow_0_48,
                                Properties.Resources.arrow_45_48,
                                Properties.Resources.arrow_90_48,
                                Properties.Resources.arrow_135_48,
                                Properties.Resources.arrow_180_48,
                                Properties.Resources.arrow_225_48,
                                Properties.Resources.arrow_270_48,
                                Properties.Resources.arrow_315_48
                            };

                            for (int i = 0; i < dAngle.Length; i++)
                            {
                                if (Math.Abs(dAngle[i] - lb.NotchAngle) <= 22.5)
                                {
                                    Bitmap bmpTmp = new Bitmap(bitmapArray[i], lb.Height, lb.Height);//正方形大小
                                    double dXoffset = (lb.Width - bmpTmp.Width) /*/ 2*/;
                                    double dYoffset = (lb.Height - bmpTmp.Height) / 2;
                                    e.Graphics.DrawImage(bmpTmp, (int)dXoffset, (int)dYoffset);
                                    break;
                                }
                            }

                        }
                    }
                    break;
                default:
                    break;
            }

            e.Handled = true;

        }
        /// <summary>
        /// 顯示滑鼠目前停在地位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            DataGridViewCell dgvRowitem = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];//v1.001客戶想要顯示顏色
            dgvRowitem.Style.BackColor = Color.FromArgb(135, 206, 250);//v1.001客戶想要顯示顏色
            if (OnSlotLabelMouseEnter != null)
                OnSlotLabelMouseEnter(this, e);
        }
        /// <summary>
        /// 顯示滑鼠目前停在地位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            DataGridViewCell dgvRowitem = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];//v1.001客戶想要顯示顏色
            dgvRowitem.Style.BackColor = Color.White;//v1.001客戶想要顯示顏色
            if (OnSlotLabelMouseLeave != null)
                OnSlotLabelMouseLeave(this, e);
        }
        /// <summary>
        /// 點選slot位置由外部註冊判斷
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (OnDataGridView1_CellClick != null)
                OnDataGridView1_CellClick(this, e);
        }

        /*private void btnZoomIn_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Height = 24;
            }
        }
        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Height = dataGridView1.Height / 200;
            }
        }*/


        /// <summary>
        /// 外部設定硬體資訊
        /// </summary>
        /// <param name="nTowerCount"></param>
        /// <param name="nTowerSlotNumber"></param>
        public void SetHardwareParam(int nTowerCount, int nTowerSlotNumber)
        {
            m_nStockTowerTotal = nTowerCount;
            m_nTowerSlotTotal = nTowerSlotNumber;

            CreateStageGUI();

            m_strStockMapData = new string[m_nStockTowerTotal];

            string str = "";
            for (int i = 0; i < m_nTowerSlotTotal; i++)
            {
                str += '0';
            }
            for (int i = 0; i < m_nStockTowerTotal; i++)
            {
                m_strStockMapData[i] = str;
            }
        }

        public void SetFreeStyleColor(Color cWafer, Color cPutWafer)
        {
            m_cWaferOn = cWafer;
            m_cPutWafer = cPutWafer;
            m_cExeHasWaferAndPut = Color.FromArgb(255, 160, 122);//Color.LightSalmon
        }

        public string GetLanguage(string source)
        {
            string target = "";

            switch (GParam.theInst.SystemLanguage)
            {
                case enumSystemLanguage.Default:
                case enumSystemLanguage.zn_TW:
                    {
                        target = source;
                    }
                    break;
                case enumSystemLanguage.zh_CN:
                    {
                        if (m_DicAllLanguageTranfer.ContainsKey(source))
                        {
                            target = m_DicAllLanguageTranfer[source];
                        }
                        else
                        {
                            target = source;
                        }
                    }
                    break;
            }
            return target;
        }



    }
}

