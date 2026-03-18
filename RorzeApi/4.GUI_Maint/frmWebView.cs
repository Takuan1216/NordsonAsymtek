using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace RorzeApi
{
    public partial class frmWebView : Form
    {
        private string _url = "http://127.0.0.1:5000";

        public frmWebView()
        {
            InitializeComponent();
            LoadUrlFromIni();
            InitializeWebView2();
        }

        private void LoadUrlFromIni()
        {
            try
            {
                string iniPath = Path.Combine(Application.StartupPath, "SettingFile", "Setting.ini");
                if (File.Exists(iniPath))
                {
                    CINIFile ini = new CINIFile(iniPath);
                    string baseUrl = ini.GetIni("GRPC", "BaseUrl", "http://127.0.0.1:5000");
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        _url = baseUrl;
                    }
                }
                txtUrl.Text = _url;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"讀取 INI 失敗: {ex.Message}");
            }
        }

        private void InitializeWebView2()
        {
            webView2 = new WebView2();
            webView2.Dock = DockStyle.Fill;
            webView2.Name = "webView2";
            panelWebView.Controls.Add(webView2);
        }

        private async void frmWebView_Load(object sender, EventArgs e)
        {
            try
            {
                await webView2.EnsureCoreWebView2Async(null);

                // 當網址變更時更新顯示
                webView2.CoreWebView2.SourceChanged += (s, args) =>
                {
                    if (webView2.CoreWebView2 != null)
                    {
                        txtUrl.Text = webView2.CoreWebView2.Source;
                    }
                };

                webView2.CoreWebView2.Navigate(_url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失敗: {ex.Message}\n\n請確認已安裝 WebView2 Runtime。",
                    "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            webView2?.Reload();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            if (webView2?.CoreWebView2 != null)
            {
                webView2.CoreWebView2.Navigate(_url);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (webView2 != null && webView2.CanGoBack)
                webView2.GoBack();
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (webView2 != null && webView2.CanGoForward)
                webView2.GoForward();
        }
    }
}
