using Rorze.Secs;
using RorzeApi.SECSGEM;
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
    public partial class frmSECSSetting : Form
    {
        float frmX;//當前窗體的寬度
        float frmY;//當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        PropertyGridHostCommunication HostproGrid = new PropertyGridHostCommunication();
        MainDB _DB;
        SSECSParameter _parameter;
        SGEM300 _Gem;
        frmMessageBox frm1;
        public frmSECSSetting(MainDB DB, SSECSParameter Parm, SGEM300 Gem)
        {
            InitializeComponent();

            _DB = DB;
            _parameter = Parm;
            _Gem = Gem;

            if (GParam.theInst.FreeStyle)
            {
                btnSave.Image = Properties.Resources._32_save_;
            }
        }

        #region Form Zoom
        public void SetGUISize(float frmWidth, float frmHeight)
        {
            if (isLoaded == false)
            {
                frmX = this.Width;  //獲取窗體的寬度
                frmY = this.Height; //獲取窗體的高度      
                isLoaded = true;    // 已設定各控制項的尺寸到Tag屬性中
                SetTag(this);       //調用方法
            }
            float tempX = frmWidth / frmX;  //計算比例
            float tempY = frmHeight / frmY; //計算比例
            SetControls(tempX, tempY, this);
        }
        private void SetTag(Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                con.Tag = con.Width + ":" + con.Height + ":" + con.Left + ":" + con.Top + ":" + con.Font.Size;
                if (con.Controls.Count > 0)
                    SetTag(con);
            }
        }
        private void SetControls(float newx, float newy, Control cons)
        {
            //遍歷窗體中的控制項，重新設置控制項的值
            foreach (Control con in cons.Controls)
            {
                string[] mytag = con.Tag.ToString().Split(new char[] { ':' });//獲取控制項的Tag屬性值，並分割後存儲字元串數組
                float a = System.Convert.ToSingle(mytag[0]) * newx;//根據窗體縮放比例確定控制項的值，寬度
                con.Width = (int)a;//寬度
                a = System.Convert.ToSingle(mytag[1]) * newy;//高度
                con.Height = (int)(a);
                a = System.Convert.ToSingle(mytag[2]) * newx;//左邊距離
                con.Left = (int)(a);
                a = System.Convert.ToSingle(mytag[3]) * newy;//上邊緣距離
                con.Top = (int)(a);
                Single currentSize = System.Convert.ToSingle(mytag[4]) * newy;//字體大小
                con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                if (con.Controls.Count > 0)
                {
                    SetControls(newx, newy, con);
                }
            }
        }
        #endregion

        //========= 
        private void _Gem_OnSECSClose(object sender, EventArgs e)
        {
            _Gem.SECSStart();
        }
        private void _Gem_OnSECSOpen(object sender, EventArgs e)
        {
            if (frm1 != null)
                CloseFrm(frm1);
        }
        //========= 
        private void buttonHostConfigSave_Click(object sender, EventArgs e)
        {
            if (_Gem.GEMControlStatus != GEMControlStats.OFFLINE && _Gem.GEMControlStatus != GEMControlStats.ATTEMTPONLINE && _Gem.GEMControlStatus != GEMControlStats.EQUIPMENTOFFLINE && _Gem.GEMControlStatus != GEMControlStats.HOSTOFFLINE)
            {
                frmMessageBox frm = new frmMessageBox(string.Format("Gem Status is error, Please Change to offline"), "Error.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK) return;
            }

            _parameter._SecsParameterConfig.OnlineSubStats = (HostproGrid.DefaultControlState == PropertyGridHostCommunication.defaulControlStat.ONLINELOCAL) ? GEMControlStats.ONLINELOCAL : GEMControlStats.ONLINEREMOTE;

            _parameter._SecsConnectConfig.LocalIP = HostproGrid.LocalAddress;
            _parameter._SecsConnectConfig.LocalPort = HostproGrid.LocalPort;
            _parameter._SecsConnectConfig.T3 = HostproGrid.T3Timeout;
            _parameter._SecsConnectConfig.T5 = HostproGrid.T5Timeout;
            _parameter._SecsConnectConfig.T6 = HostproGrid.T6Timeout;
            _parameter._SecsConnectConfig.T7 = HostproGrid.T7Timeout;
            _parameter._SecsConnectConfig.T8 = HostproGrid.T8Timeout;

            //GParam.theInst.SetWaferOutAction(HostproGrid.WaferOutAction);

            _Gem.GetSECSDriver.SetLocalIP(_parameter._SecsConnectConfig.LocalIP);
            _Gem.GetSECSDriver.SetLocalPort(_parameter._SecsConnectConfig.LocalPort);
            _Gem.GetSECSDriver.SetT3TimeOut(_parameter._SecsConnectConfig.T3);
            _Gem.GetSECSDriver.SetT5TimeOut(_parameter._SecsConnectConfig.T5);
            _Gem.GetSECSDriver.SetT6TimeOut(_parameter._SecsConnectConfig.T6);
            _Gem.GetSECSDriver.SetT7TimeOut(_parameter._SecsConnectConfig.T7);
            _Gem.GetSECSDriver.SetT8TimeOut(_parameter._SecsConnectConfig.T8);

            _Gem.GetSECSDriver.SaveConfig();



            //_Gem.SECSStart();

            _DB.SetSECSParameter(_parameter.GetSECSParameterConfig);
            _DB.SetSECSConnectParameter(_parameter.GetSecsConnectConfig);

            _Gem.SECSStop();

            frm1 = new frmMessageBox(string.Format("SECS/GEM Function Reset Now !! Please Wait few minutes...."), "INFO.", MessageBoxButtons.OK, MessageBoxIcon.Information, false);
            frm1.ShowDialog();
        }
        //========= 
        private void frmSECSSetting_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                _Gem.OnSECSClose += _Gem_OnSECSClose;
                _Gem.OnSECSOpen += _Gem_OnSECSOpen;
                HostproGrid.DefaultControlState = (_parameter.GetSECSParameterConfig.OnlineSubStats == GEMControlStats.ONLINELOCAL) ? PropertyGridHostCommunication.defaulControlStat.ONLINELOCAL : PropertyGridHostCommunication.defaulControlStat.ONLINEREMOTE;

                HostproGrid.LocalAddress = _parameter.GetSecsConnectConfig.LocalIP;
                HostproGrid.LocalPort = _parameter.GetSecsConnectConfig.LocalPort;
                HostproGrid.T3Timeout = _parameter.GetSecsConnectConfig.T3;
                HostproGrid.T5Timeout = _parameter.GetSecsConnectConfig.T5;
                HostproGrid.T6Timeout = _parameter.GetSecsConnectConfig.T6;
                HostproGrid.T7Timeout = _parameter.GetSecsConnectConfig.T7;
                HostproGrid.T8Timeout = _parameter.GetSecsConnectConfig.T8;

                //HostproGrid.WaferOutAction = GParam.theInst.WaferOutAction;

                propertyGridHost.SelectedObject = HostproGrid;
            }
            else
            {
                _Gem.OnSECSClose -= _Gem_OnSECSClose;
                _Gem.OnSECSOpen -= _Gem_OnSECSOpen;
            }
        }



        //========= 
        delegate void dlgCloseFrm(frmMessageBox from);
        public void CloseFrm(frmMessageBox from)
        {
            if (InvokeRequired)
            {
                dlgCloseFrm dlg = new dlgCloseFrm(CloseFrm);
                this.BeginInvoke(dlg, from);
            }
            else
            {
                from.Close();
            }
        }
    }
    internal class PropertyGridHostCommunication
    {
        public PropertyGridHostCommunication()
        {
        }

        public enum defaulControlStat
        { ONLINELOCAL = 1, ONLINEREMOTE = 2 }
        private defaulControlStat defaultControlState;

        [Category("GEM")]
        [Browsable(true)]
        [OrderedDisplayName("Default Control State", 1)]
        [Description("The default state of the GEM control state machine when equipment is started up.")]
        public defaulControlStat DefaultControlState
        {
            get { return defaultControlState; }
            set { defaultControlState = value; }
        }

        private string localAddress;
        [Category("HSMS")]
        [Browsable(true)]
        [OrderedDisplayName("IP Address", 1)]
        [Description("IP address for the HSMS connection.IP address must be localhost(127.0.0.1) if entity mode is PASSIVE.")]
        public string LocalAddress
        {
            get { return localAddress; }
            set { localAddress = value; }
        }

        private int localPort;
        [Category("HSMS")]
        [Browsable(true)]
        [OrderedDisplayName("Port Number", 2)]
        [Description("TCP port number that the host connect to. The valid value range is 0 - 65535. The typical port number is 5000.")]
        public int LocalPort
        {
            get { return localPort; }
            set
            {
                if (value < 0 || 65535 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 0 - 65535.");
                }
                localPort = value;
            }
        }

        private int t3Timeout;
        [Category("HSMS")]
        [Browsable(true)]
        [OrderedDisplayName("T3 Timeout (seconds)", 3)]
        [Description("The T3 timeout is the transaction timer. This is the maximum amount of time between a primary message and the expected response before declaring the transaction closed. If the timer expires, an S9F9 error message is sent. The valid value range is 1 - 120 seconds. The typical value is 45 seconds.")]
        public int T3Timeout
        {
            get { return t3Timeout; }
            set
            {
                if (value < 1 || 120 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 120.");
                }
                t3Timeout = value;
            }
        }

        private int t5Timeout;
        [Category("HSMS")]
        [Browsable(true)]
        [OrderedDisplayName("T5 Timeout (seconds)", 4)]
        [Description("The T5 timeout is the connect separation timeout. This is the amount of time which must elapse between successive attempts to actively establish a connection. The valid value range is 1 - 240 seconds. The tyipical value is 5 seconds.")]
        public int T5Timeout
        {
            get { return t5Timeout; }
            set
            {
                if (value < 1 || 240 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 240.");
                }
                t5Timeout = value;
            }
        }

        private int t6Timeout;
        [Category("HSMS")]
        [Browsable(true)]
        [OrderedDisplayName("T6 Timeout (seconds)", 5)]
        [Description("The T6 timeout is the control transaction timeout. This is the maximum amount of time allowed between an HSMS-level control message and its response. If the timer expires, communications failure is declared. The valid value range is 1 - 240 seconds. The typical value is 5 seconds.")]
        public int T6Timeout
        {
            get { return t6Timeout; }
            set
            {
                if (value < 1 || 240 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 240.");
                }
                t6Timeout = value;
            }
        }

        private int t7Timeout;
        [Category("HSMS")]
        [Browsable(true)]
        [OrderedDisplayName("T7 Timeout (seconds)", 6)]
        [Description("The T7 timeout is the NOT SELECTED timeout. This is the maximum amount of time a TCP/IP connection can remain in the NOT SELECTED state (no HSMS activity) before a communications failure is declared. The valid value range is 1 - 240 seconds. The typical value is 10 seconds.")]
        public int T7Timeout
        {
            get { return t7Timeout; }
            set
            {
                if (value < 1 || 240 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 240.");
                }
                t7Timeout = value;
            }
        }

        private int t8Timeout;
        [Category("HSMS")]
        [Browsable(true)]
        [OrderedDisplayName("T8 Timeout (seconds)", 7)]
        [Description("The T8 timeout is the network intercharacter timeout. This is the maximum amount of time allowed between successive bytes of a single HSMS message before a communications failure is declared. The valid value range is 1 - 120 seconds. The typical value is 5 seconds.")]
        public int T8Timeout
        {
            get { return t8Timeout; }
            set
            {
                if (value < 1 || 120 < value)
                {
                    throw new Exception("The input value is invalid. The valid value range is 1 - 120.");
                }
                t8Timeout = value;
            }
        }



        /*
        [Category("Custom Function")]
        [Browsable(true)]
        [OrderedDisplayName("Stocker Wafer Out Action", 1)]
        [Description("Stocker Wafer Out Action")]
        public enumWaferOutAction WaferOutAction { get; set; }
        */

        public bool IsModified(PropertyGridHostCommunication other)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this);
            foreach (PropertyDescriptor property in properties)
            {
                if (!property.IsBrowsable) continue;
                string s1 = property.GetValue(this).ToString();
                string s2 = property.GetValue(other).ToString();
                if (s1 != s2)
                {
                    return true;
                }
            }
            return false;
        }

        public class OrderedDisplayNameAttribute : DisplayNameAttribute
        {
            public OrderedDisplayNameAttribute(string displayName, int position)
            {
                base.DisplayNameValue = string.Format("{0,2:D}. {1}", position, displayName);
            }
        }
    }
}
