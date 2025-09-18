using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace WifiPasswordTool
{
    public partial class AboutDialog : Form
    {
        public AboutDialog()
        {
            InitializeComponent();
            LoadAboutInfo();
        }

        private void LoadAboutInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            
            lblVersion.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
            lblCopyright.Text = $"© {DateTime.Now.Year} WiFi Password Manager";
            lblDescription.Text = "Professional WiFi password viewer and management tool.\n\n" +
                                 "Features:\n" +
                                 "• View saved WiFi passwords\n" +
                                 "• Export passwords to file\n" +
                                 "• Delete individual or all profiles\n" +
                                 "• Secure password handling\n" +
                                 "• Modern, professional interface";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}