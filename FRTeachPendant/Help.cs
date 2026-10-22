using Microsoft.Web.WebView2.Core;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FRTeachPendant
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();
            this.Load += Help_Load;
        }
        private async void InitWebView2()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MyWinFormsApp",
                    "WebView2");

                Directory.CreateDirectory(userDataFolder);

                var env = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: null);

                await wb_Helper.EnsureCoreWebView2Async(env);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "WebView2 Init failed");
            }
        }

        private async void Help_Load(object sender, EventArgs e)
        {
            InitWebView2();
            try
            {
            
                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "Helper", "help.html");

                if (!File.Exists(htmlPath))
                {
                    
                    return;
                }

                await wb_Helper.EnsureCoreWebView2Async(null);
                wb_Helper.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                wb_Helper.ZoomFactor = 0.7;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}