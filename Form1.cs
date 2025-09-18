using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Principal;

namespace WifiPasswordTool
{
    public partial class Form1 : Form
    {
        private List<WifiProfile> wifiProfiles = new List<WifiProfile>();
        private bool isPasswordVisible = false;
        private bool isRestartingAsAdmin = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Check admin privileges first, before loading anything
            if (!CheckAdministratorPrivileges())
            {
                return; // Don't continue loading if restarting as admin
            }
            
            LoadWifiProfiles();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Don't process shortcuts if restarting as admin
            if (isRestartingAsAdmin) return true;
            
            // Handle keyboard shortcuts
            switch (keyData)
            {
                case Keys.F5:
                    btnRefresh_Click(null, null);
                    return true;
                case Keys.Control | Keys.P:
                    btnShowPassword_Click(null, null);
                    return true;
                case Keys.Control | Keys.E:
                    btnExport_Click(null, null);
                    return true;
                case Keys.Delete:
                    if (listViewWifi.SelectedItems.Count > 0)
                        btnDeleteSelected_Click(null, null);
                    return true;
                case Keys.Control | Keys.Shift | Keys.Delete:
                    btnDeleteAll_Click(null, null);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool CheckAdministratorPrivileges()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                var result = MessageBox.Show(
                    "This application requires administrator privileges to view and manage WiFi passwords.\n\n" +
                    "Would you like to restart the application as an administrator?",
                    "Administrator Rights Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                    
                if (result == DialogResult.Yes)
                {
                    isRestartingAsAdmin = true;
                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                    
                    // Use BeginInvoke to ensure the UI is updated before restarting
                    this.BeginInvoke(new Action(() =>
                    {
                        Program.RestartAsAdministrator();
                    }));
                    
                    return false; // Don't continue with form loading
                }
                else
                {
                    // User declined admin privileges, close the application
                    this.BeginInvoke(new Action(() =>
                    {
                        Application.Exit();
                    }));
                    return false;
                }
            }
            
            return true; // Has admin privileges, continue normally
        }

        private async void LoadWifiProfiles()
        {
            if (isRestartingAsAdmin) return;
            
            try
            {
                SetStatus("Loading WiFi profiles...");
                ShowProgress(true);
                
                wifiProfiles.Clear();
                listViewWifi.Items.Clear();

                await Task.Run(() =>
                {
                    // Get all WiFi profiles
                    var profiles = GetWifiProfiles();
                    
                    foreach (var profile in profiles)
                    {
                        var password = GetWifiPassword(profile);
                        var security = GetWifiSecurity(profile);
                        
                        wifiProfiles.Add(new WifiProfile
                        {
                            SSID = profile,
                            Password = password,
                            Security = security,
                            ConnectionType = "Saved Profile"
                        });
                    }
                });

                if (isRestartingAsAdmin) return; // Check again after async operation

                // Update UI on main thread
                foreach (var profile in wifiProfiles)
                {
                    var item = new ListViewItem(profile.SSID);
                    item.SubItems.Add(isPasswordVisible ? profile.Password : new string('•', Math.Max(profile.Password.Length, 8)));
                    item.SubItems.Add(profile.Security);
                    item.SubItems.Add(profile.ConnectionType);
                    item.Tag = profile;
                    listViewWifi.Items.Add(item);
                }

                SetStatus($"Loaded {wifiProfiles.Count} WiFi profiles");
                ShowProgress(false);
            }
            catch (Exception ex)
            {
                if (!isRestartingAsAdmin)
                {
                    MessageBox.Show($"Error loading WiFi profiles: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetStatus("Error loading profiles");
                    ShowProgress(false);
                }
            }
        }

        private List<string> GetWifiProfiles()
        {
            var profiles = new List<string>();
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show profiles",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("All User Profile") || line.Contains("User Profile"))
                    {
                        var match = Regex.Match(line, @":\s*(.+)");
                        if (match.Success)
                        {
                            var profileName = match.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(profileName))
                            {
                                profiles.Add(profileName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve WiFi profiles: {ex.Message}");
            }

            return profiles;
        }

        private string GetWifiPassword(string profileName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"wlan show profile name=\"{profileName}\" key=clear",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("Key Content"))
                    {
                        var match = Regex.Match(line, @":\s*(.+)");
                        if (match.Success)
                        {
                            return match.Groups[1].Value.Trim();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore individual profile errors
            }

            return "N/A";
        }

        private string GetWifiSecurity(string profileName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"wlan show profile name=\"{profileName}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("Security key") || line.Contains("Authentication"))
                    {
                        var match = Regex.Match(line, @":\s*(.+)");
                        if (match.Success)
                        {
                            return match.Groups[1].Value.Trim();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore individual profile errors
            }

            return "Unknown";
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (!isRestartingAsAdmin)
                LoadWifiProfiles();
        }

        private void btnShowPassword_Click(object sender, EventArgs e)
        {
            if (isRestartingAsAdmin) return;
            
            isPasswordVisible = !isPasswordVisible;
            btnShowPassword.Text = isPasswordVisible ? "🙈 Hide Password" : "👁 Show Password";
            showPasswordsToolStripMenuItem.Text = isPasswordVisible ? "Hide &Passwords" : "Show &Passwords";
            
            // Update password column visibility
            for (int i = 0; i < listViewWifi.Items.Count; i++)
            {
                var profile = (WifiProfile)listViewWifi.Items[i].Tag;
                listViewWifi.Items[i].SubItems[1].Text = isPasswordVisible ? 
                    profile.Password : 
                    new string('•', Math.Max(profile.Password.Length, 8));
            }
        }

        private async void btnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (isRestartingAsAdmin) return;
            
            if (listViewWifi.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a WiFi profile to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the selected WiFi profile(s)?\n\nThis action cannot be undone.",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                SetStatus("Deleting selected profiles...");
                ShowProgress(true);

                await Task.Run(() =>
                {
                    foreach (ListViewItem item in listViewWifi.SelectedItems)
                    {
                        var profile = (WifiProfile)item.Tag;
                        DeleteWifiProfile(profile.SSID);
                    }
                });

                LoadWifiProfiles();
            }
        }

        private async void btnDeleteAll_Click(object sender, EventArgs e)
        {
            if (isRestartingAsAdmin) return;
            
            if (wifiProfiles.Count == 0)
            {
                MessageBox.Show("No WiFi profiles to delete.", "No Profiles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"⚠️ WARNING: This will delete ALL {wifiProfiles.Count} saved WiFi profiles!\n\n" +
                "You will need to re-enter passwords for all networks.\n" +
                "This action cannot be undone.\n\n" +
                "Are you absolutely sure you want to continue?",
                "Delete All WiFi Profiles",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var confirmResult = MessageBox.Show(
                    "This is your final confirmation.\n\n" +
                    "Click YES to permanently delete all WiFi profiles.",
                    "Final Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Stop);

                if (confirmResult == DialogResult.Yes)
                {
                    SetStatus("Deleting all WiFi profiles...");
                    ShowProgress(true);

                    await Task.Run(() =>
                    {
                        foreach (var profile in wifiProfiles)
                        {
                            DeleteWifiProfile(profile.SSID);
                        }
                    });

                    LoadWifiProfiles();
                    MessageBox.Show("All WiFi profiles have been deleted successfully.", "Operation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DeleteWifiProfile(string profileName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"wlan delete profile name=\"{profileName}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete profile '{profileName}': {ex.Message}");
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (isRestartingAsAdmin) return;
            
            if (wifiProfiles.Count == 0)
            {
                MessageBox.Show("No WiFi profiles to export.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                saveDialog.Title = "Export WiFi Profiles";
                saveDialog.FileName = $"WiFi_Profiles_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ExportToFile(saveDialog.FileName);
                        MessageBox.Show($"WiFi profiles exported successfully to:\n{saveDialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SetStatus($"Exported {wifiProfiles.Count} profiles to file");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting profiles: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportToFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            var content = new StringBuilder();

            if (extension == ".csv")
            {
                content.AppendLine("Network Name (SSID),Password,Security Type,Connection Type");
                foreach (var profile in wifiProfiles)
                {
                    content.AppendLine($"\"{profile.SSID}\",\"{profile.Password}\",\"{profile.Security}\",\"{profile.ConnectionType}\"");
                }
            }
            else
            {
                content.AppendLine("WiFi Password Export");
                content.AppendLine($"Generated on: {DateTime.Now}");
                content.AppendLine($"Total profiles: {wifiProfiles.Count}");
                content.AppendLine(new string('=', 50));
                content.AppendLine();

                foreach (var profile in wifiProfiles)
                {
                    content.AppendLine($"Network Name: {profile.SSID}");
                    content.AppendLine($"Password: {profile.Password}");
                    content.AppendLine($"Security: {profile.Security}");
                    content.AppendLine($"Type: {profile.ConnectionType}");
                    content.AppendLine(new string('-', 30));
                }
            }

            File.WriteAllText(fileName, content.ToString(), Encoding.UTF8);
        }

        private void listViewWifi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isRestartingAsAdmin)
            {
                btnShowPassword.Enabled = listViewWifi.Items.Count > 0;
                btnDeleteSelected.Enabled = listViewWifi.SelectedItems.Count > 0;
                deleteSelectedToolStripMenuItem.Enabled = listViewWifi.SelectedItems.Count > 0;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isRestartingAsAdmin) return;
            
            using (var aboutDialog = new AboutDialog())
            {
                aboutDialog.ShowDialog(this);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (isRestartingAsAdmin) return;
            
            try
            {
                Process.Start("https://ko-fi.com/root");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetStatus), message);
                return;
            }
            if (!isRestartingAsAdmin)
                toolStripStatusLabel.Text = message;
        }

        private void ShowProgress(bool show)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(ShowProgress), show);
                return;
            }
            if (!isRestartingAsAdmin)
                progressBar.Visible = show;
        }
    }

    public class WifiProfile
    {
        public string SSID { get; set; }
        public string Password { get; set; }
        public string Security { get; set; }
        public string ConnectionType { get; set; }
    }
}
