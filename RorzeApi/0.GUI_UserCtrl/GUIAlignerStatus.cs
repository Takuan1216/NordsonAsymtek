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
    public partial class GUIAlignerStatus : UserControl
    {
        Dictionary<string, string> m_DicAllLanguageTranfer = new Dictionary<string, string>();
        public GUIAlignerStatus()
        {
            InitializeComponent();
            m_DicAllLanguageTranfer.Add("NOTREADY", "未就绪");
            m_DicAllLanguageTranfer.Add("READY", "准备");
            m_DicAllLanguageTranfer.Add("ERROR", "错误");
            m_DicAllLanguageTranfer.Add("RUN", "执行");
            m_DicAllLanguageTranfer.Add("Disable", "禁用");
            m_DicAllLanguageTranfer.Add("Disconnect", "断开");
            m_DicAllLanguageTranfer.Add("Teach Pendant", "教导器");

            m_DicAllLanguageTranfer.Add("Robot ", "机器手");
            m_DicAllLanguageTranfer.Add("Loadport ", "晶圆装卸机");    
            m_DicAllLanguageTranfer.Add("Aligner ", "晶圆校准机");
            m_DicAllLanguageTranfer.Add("Buffer ", "暂存区");

        }

        int m_nBodyNo;
        public int BodyNo
        {
            get { return m_nBodyNo; }
            set
            {
                m_nBodyNo = value;
                lblUnitName.Text = GetLanguage("Aligner ") + (char)(64 + value);
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
                if (GetLanguage(GetEnumDescription(value)) == lblState.Text) { return; }
                lblState.Text = GetLanguage(GetEnumDescription(value));
                switch (value)
                {
                    case enumState.Error:
                        //lblAlignerBState.Text = "ERROR";
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Red;
                        break;
                    case enumState.Disable:
                        //lblAlignerBState.Text = "Disable";
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
                    case enumState.Idle:
                        //lblAlignerBState.Text = "IDLE";
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
                        //lblAlignerBState.Text = "RUN";
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Cyan;
                        break;
                    case enumState.Unknown:
                        //lblAlignerBState.Text = "NOTREADY";
                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Yellow;
                        break;
                }
                this.Refresh();
            }
        }
        public GUI.GUIAligner.enuWaferStatus SetWaferStatus
        {
            set
            {
                if (value == guiAligner.AlignerStatus) { return; }
                guiAligner.AlignerStatus = value;
                this.Refresh();
            }
            get { return guiAligner.AlignerStatus; }
        }


        bool m_bFreeStyle = false;
        Color m_cReady = Color.Green;
        public void SetFreeStyleColor(Color cTitle, Color cReady)
        {
            m_bFreeStyle = true;
            m_cReady = cReady;
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
