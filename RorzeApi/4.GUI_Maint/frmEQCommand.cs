
using RorzeComm;
using RorzeComm.Log;
using RorzeUnit;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.EQ.Enum;
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
    public partial class frmEQCommand : Form
    {
        float frmX;//當前窗體的寬度
        float frmY;//當前窗體的高度
        bool isLoaded = false;  // 是否已設定各控制的尺寸資料到Tag屬性

        SSEquipment _equipment;
        Dictionary<enumSendCmd, TextBox> dicTextData = new Dictionary<enumSendCmd, TextBox>();
        SLogger _logger = SLogger.GetLogger("ExecuteLog");

        public frmEQCommand(SSEquipment equipment)
        {
            InitializeComponent();

            _equipment = equipment;
            _equipment.OnReadData += WritelsbContent;

            //  建立tableLayout
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Padding = new Padding(0);
            tableLayoutPanel1.RowCount = 10;
            tableLayoutPanel1.ColumnCount = ((Enum.GetNames(typeof(enumSendCmd)).Count() / 10) + 1) * 2;
            for (int i = 0; i < tableLayoutPanel1.RowCount; i++)
            {
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            }
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));

            for (int i = 0; i < tableLayoutPanel1.ColumnCount; i++)
            {
                if (i % 2 == 0)
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
                else
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            }
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));

            //  tableLayout中塞入 label 與 textbox
            for (enumSendCmd i = 0; i < (enumSendCmd)Enum.GetNames(typeof(enumSendCmd)).Count(); i++)
            {
                Label lbl = new Label();
                lbl.Text = i.ToString() + " :";
                lbl.Dock = DockStyle.Fill;
                lbl.TextAlign = ContentAlignment.MiddleRight;

                //不顯示none
                if (i == enumSendCmd.Unknow)
                {
                    continue;
                }

                tableLayoutPanel1.Controls.Add(lbl, ((int)i / 10) * 2, (int)i % 10);

                TextBox txt = new TextBox();
                txt.Dock = DockStyle.Fill;

                //  TextBox用來寫參數
                txt.ReadOnly = true;
                txt.Anchor = ((AnchorStyles)(AnchorStyles.Left | AnchorStyles.Right));//看起來至中


                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.KeyDown += textBox_KeyDown; //   註冊事件
                tableLayoutPanel1.Controls.Add(txt, ((int)i / 10) * 2 + 1, (int)i % 10);

                dicTextData.Add(i, txt);
            }





        }
        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    TextBox txt = sender as TextBox;
                    foreach (var item in dicTextData)
                    {
                        if (txt != item.Value) continue;
                        gpbCommand.Enabled = false;

                        switch (item.Key)
                        {
                            case enumSendCmd.Hello:
                                _equipment.HelloW();
                                break;
                            case enumSendCmd.RecipeList:
                                _equipment.GetRecipeListW();
                                break;
                            case enumSendCmd.PrepareToReceiveWafer:
                                _equipment.PrepareToReceiveWaferW();
                                break;
                            case enumSendCmd.PutWaferFinish:
                                _equipment.PutWaferFinishW();
                                break;
                            case enumSendCmd.ProcessWafer:
                                _equipment.ProcessWaferW();
                                break;
                            case enumSendCmd.GetWaferFinish:
                                _equipment.GetWaferFinishW();
                                break;
                            case enumSendCmd.Stop:
                                _equipment.StopW();
                                break;
                            case enumSendCmd.Retry:
                                _equipment.RetryW();
                                break;
                            case enumSendCmd.Abort:
                                _equipment.AbortW();
                                break;
                            case enumSendCmd.Status:
                                _equipment.StatusW();
                                break;
                            case enumSendCmd.UnloadWafer:
                                _equipment.UnloadWaferW();
                                break;
                            case enumSendCmd.Alarm:
                                _equipment.AlarmW();
                                break;
                            case enumSendCmd.SoftwareVersion:
                                _equipment.SoftwareVersionW();
                                break;
                            case enumSendCmd.MachineType:
                                _equipment.MachineTypeW();
                                break;
                            case enumSendCmd.ModeLock:
                                _equipment.ModeLockW("1");
                                break;
                            case enumSendCmd.EQToSafePosition:
                                _equipment.EQToSafePosition();
                                break;
                            default:
                                break;
                        }

                        gpbCommand.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                frmMessageBox frm = new frmMessageBox(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                frm.ShowDialog();
                gpbCommand.Enabled = true;
            }
        }
        private void WritelsbContent(object sender, MessageEventArgs e)
        {
            if (this.Visible == false) return;
            Action act = () =>
            {
                foreach (string str in e.Message)
                {
                    lsbContent.Items.Add(str);
                }
            };
            BeginInvoke(act);
        }
        private void btnClear_Click(object sender, EventArgs e)
        {
            lsbContent.Items.Clear();
            gpbCommand.Enabled = true;
        }

        #region Form Zoom
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
            if (isLoaded)
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
        }
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
        #endregion


    }
}
