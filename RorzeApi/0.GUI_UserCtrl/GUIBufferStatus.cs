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
    public partial class GUIBufferStatus : UserControl
    {
        Dictionary<string, string> m_DicAllLanguageTranfer = new Dictionary<string, string>();
        public GUIBufferStatus()
        {
            InitializeComponent();
            m_DicAllLanguageTranfer.Add("NOTREADY", "未就绪");
            m_DicAllLanguageTranfer.Add("READY", "准备");
            m_DicAllLanguageTranfer.Add("ERROR", "错误");
            m_DicAllLanguageTranfer.Add("RUN", "执行");
            m_DicAllLanguageTranfer.Add("Disable", "禁用");
            m_DicAllLanguageTranfer.Add("Disconnect", "断开");

            m_DicAllLanguageTranfer.Add("Robot ", "机器手");
            m_DicAllLanguageTranfer.Add("Loadport ", "晶圆装卸机");
            m_DicAllLanguageTranfer.Add("Aligner ", "晶圆校准机");
            m_DicAllLanguageTranfer.Add("Buffer ", "暂存区");
        }
        public enum enumState : int
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
                if (GetLanguage(GetEnumDescription(value)) == lblState.Text) { return; }
                lblState.Text = GetLanguage(GetEnumDescription(value));
                switch (value)
                {
                    case enumState.Idle:
                        if (m_bFreeStyle)
                        {
                            lblState.ForeColor = Color.Black;
                            lblState.BackColor = m_cReady;
                        }
                        else
                        {
                            lblState.ForeColor = Color.White;
                            lblState.BackColor = Color.Green;
                        }
                        break;
                    case enumState.Moving:
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Cyan;
                        break;
                    case enumState.Unknown:
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Yellow;
                        break;
                    case enumState.Error:
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Red;
                        break;
                }
                this.Refresh();
            }
        }
        public GUI.GUIEquipment.enuWaferStatus SetBuf1WaferStatus
        {
            set
            {
                if (value == guiSlot1Wafer.EquipmentStatus) { return; }
                guiSlot1Wafer.EquipmentStatus = value;
                this.Refresh();
            }
        }
        public string SetBuf1WaferSlotNo
        {
            set
            {
                if (value == guiSlot1Wafer.WaferSlotNo) { return; }
                guiSlot1Wafer.WaferSlotNo = value;
                this.Refresh();
            }
        }
        public string SetBuf1WaferRecipe
        {
            set
            {
                if (value == lblSlot1Recipe.Text) { return; }
                lblSlot1Recipe.Text = value;
                this.Refresh();
            }
        }
        public GUI.GUIEquipment.enuWaferStatus SetBuf2WaferStatus
        {
            set
            {
                if (value == guiSlot2Wafer.EquipmentStatus) { return; }
                guiSlot2Wafer.EquipmentStatus = value;
                this.Refresh();
            }
        }
        public string SetBuf2WaferSlotNo
        {
            set
            {
                if (value == guiSlot2Wafer.WaferSlotNo) { return; }
                guiSlot2Wafer.WaferSlotNo = value;
                this.Refresh();
            }
        }
        public string SetBuf2WaferRecipe
        {
            set
            {
                if (value == lblSlot2Recipe.Text) { return; }
                lblSlot2Recipe.Text = value;
                this.Refresh();
            }
        }
        public GUI.GUIEquipment.enuWaferStatus SetBuf3WaferStatus
        {
            set
            {
                if (value == guiSlot3Wafer.EquipmentStatus) { return; }
                guiSlot3Wafer.EquipmentStatus = value;
                this.Refresh();
            }
        }
        public string SetBuf3WaferSlotNo
        {
            set
            {
                if (value == guiSlot3Wafer.WaferSlotNo) { return; }
                guiSlot3Wafer.WaferSlotNo = value;
                this.Refresh();
            }
        }
        public string SetBuf3WaferRecipe
        {
            set
            {
                if (value == lblSlot3Recipe.Text) { return; }
                lblSlot3Recipe.Text = value;
                this.Refresh();
            }
        }
        public GUI.GUIEquipment.enuWaferStatus SetBuf4WaferStatus
        {
            set
            {
                if (value == guiSlot4Wafer.EquipmentStatus) { return; }
                guiSlot4Wafer.EquipmentStatus = value;
                this.Refresh();
            }
        }
        public string SetBuf4WaferSlotNo
        {
            set
            {
                if (value == guiSlot4Wafer.WaferSlotNo) { return; }
                guiSlot4Wafer.WaferSlotNo = value;
                this.Refresh();
            }
        }
        public string SetBuf4WaferRecipe
        {
            set
            {
                if (value == lblSlot4Recipe.Text) { return; }
                lblSlot4Recipe.Text = value;
                this.Refresh();
            }
        }

        bool m_bFreeStyle = false;
        Color m_cReady = Color.Green;
        public void SetFreeStyleColor(Color cTitle, Color cReady)
        {
            m_bFreeStyle = true;
            m_cReady = cReady;
        }


        int m_nBodyNo;
        public int BodyNo
        {
            get { return m_nBodyNo; }
            set
            {
                m_nBodyNo = value;
                lblUnitName.Text = GetLanguage("Buffer ") + (char)(64 + value);
            }
        }
        //int m_nSlot = 0;
        //public int SetSlot
        //{
        //    //get { return m_nSlot; }
        //    set
        //    {
        //        if (m_nSlot == value) return;

        //        //switch (value)
        //        //{
        //        //    case 1:
        //        //        tlpSlot1.Visible = true;
        //        //        tlpSlot4.Visible = tlpSlot3.Visible = tlpSlot2.Visible = false;
        //        //        break;
        //        //    case 2:
        //        //        tlpSlot1.Visible = tlpSlot2.Visible = true;
        //        //        tlpSlot4.Visible = tlpSlot3.Visible = false;
        //        //        break;
        //        //    case 3:
        //        //        tlpSlot1.Visible = tlpSlot2.Visible = tlpSlot3.Visible = true;
        //        //        tlpSlot4.Visible = false;
        //        //        break;
        //        //    case 4:
        //        //        tlpSlot1.Visible = tlpSlot2.Visible = tlpSlot3.Visible = tlpSlot4.Visible = true;
        //        //        break;
        //        //}
        //        //int nHeight = 0;
        //        //foreach (Control item in this.Controls)
        //        //{
        //        //    if (item.Visible)
        //        //        nHeight += item.Height;
        //        //}
        //        //if (nHeight != 0)
        //        //{
        //        //    m_nSlot = value;
        //        //    this.Height = nHeight - 1;
        //        //    this.Refresh();
        //        //}
        //    }
        //}
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

        string m_strSlot = "0000";
        public string SetHardwareSlot
        {
            get { return m_strSlot; }
            set
            {
                if (m_strSlot == value) return;

                m_strSlot = value;

                for (int i = 0; i < m_strSlot.Length; i++)
                {
                    char c = m_strSlot[i];
                    switch (i)
                    {
                        case 0:
                            tlpSlot1.Visible = (c == '1');
                            break;
                        case 1:
                            tlpSlot2.Visible = (c == '1');
                            break;
                        case 2:
                            tlpSlot3.Visible = (c == '1');
                            break;
                        case 3:
                            tlpSlot4.Visible = (c == '1');
                            break;
                    }                 
                }
             
                int nHeight = 0;
                foreach (Control item in this.Controls)
                {
                    if (item.Visible)
                        nHeight += item.Height;
                }
                if (nHeight != 0)
                {          
                    this.Height = nHeight - 1;
                    this.Refresh();
                }
            }
        }

    }
}
