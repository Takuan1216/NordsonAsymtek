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
    public partial class GUIRobotStatus : UserControl
    {
        Dictionary<string, string> m_DicAllLanguageTranfer = new Dictionary<string, string>();
        public GUIRobotStatus()
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
                lblUnitName.Text = GetLanguage("Robot ") + (char)(64 + value);
            }
        }
        public string SetUpperWaferSlotNo
        {
            set
            {
                if (value == lblUpperSlotNo.Text) { return; }
                lblUpperSlotNo.Text = value;
                guiRobotArm1.SetWaferSlotNo = value;
                this.Refresh();
            }
        }
        public string SetLowerWaferSlotNo
        {
            set
            {
                if (value == lblLowerSlotNo.Text) { return; }
                lblLowerSlotNo.Text = value;
                guiRobotArm2.SetWaferSlotNo = value;
                this.Refresh();
            }
        }
        public string SetUpperWaferRecipe
        {
            set
            {
                if (value == lblUpperRecipe.Text) { return; }
                lblUpperRecipe.Text = value;
                this.Refresh();
            }
        }
        public string SetLowerWaferRecipe
        {
            set
            {
                if (value == lblLowerRecipe.Text) { return; }
                lblLowerRecipe.Text = value;
                this.Refresh();
            }
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
            Disconnect = 5,
            [Description("Teach Pendant")]
            TeachPendant = 6
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
                    case enumState.TeachPendant:

                        lblState.ForeColor = Color.Black;
                        lblState.BackColor = Color.Yellow;
                        break;
                }
                this.Refresh();
            }
        }
        public GUI.GUIRobotArm.enuWaferStatus SetUpperWaferStatus
        {
            set
            {
                if (value == guiRobotArm1.ArmStatus) { return; }
                guiRobotArm1.ArmStatus = value;
                this.Refresh();
            }
            get { return guiRobotArm1.ArmStatus; }
        }
        public GUI.GUIRobotArm.enuWaferStatus SetLowerWaferStatus
        {
            set
            {
                if (value == guiRobotArm2.ArmStatus) { return; }
                guiRobotArm2.ArmStatus = value;
                this.Refresh();
            }
            get { return guiRobotArm2.ArmStatus; }
        }


        public bool DisableUpper
        {
            set
            {
                guiRobotArm1.Visible = false;
                lblUpperSlotNo.Visible = false;
                lblUpperRecipe.Visible = false;
                lblUpperSlotNoTitle.Visible = false;
                lblUpperRecipeTitle.Visible = false;


            }
        }
        public bool DisableLower
        {
            set
            {
                guiRobotArm2.Visible = false;
                lblLowerSlotNo.Visible = false;
                lblLowerRecipe.Visible = false;
                lblLowerSlotNoTitle.Visible = false;
                lblLowerRecipeTitle.Visible = false;

                //int nCloumn = tableLayoutPanel4.ColumnCount;


                //for (int i = 0; i < nCloumn; i++)
                //{
                //    Control control1 = tableLayoutPanel4.GetControlFromPosition(i, 3);
                //    tableLayoutPanel4.Controls.Remove(control1);
                //    Control control2 = tableLayoutPanel4.GetControlFromPosition(i, 4);
                //    tableLayoutPanel4.Controls.Remove(control2);         
                //}
                //tableLayoutPanel4.RowStyles.RemoveAt(3);
                //tableLayoutPanel4.RowStyles.RemoveAt(4);
                //tableLayoutPanel4.RowStyles.RemoveAt(5);
                //tableLayoutPanel4.RowCount = tableLayoutPanel4.RowCount - 3;
            }
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
