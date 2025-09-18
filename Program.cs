using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;
using System.Diagnostics;
using System.Threading;

namespace WifiPasswordTool
{
    internal static class Program
    {
        private static Mutex mutex = null;
        private const string AppName = "WifiPasswordTool_SingleInstance";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Single instance check
            bool createdNew;
            mutex = new Mutex(true, AppName, out createdNew);
            
            if (!createdNew)
            {
                // Application is already running
                MessageBox.Show(
                    "WiFi Password Manager is already running.", 
                    "Application Already Running", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return;
            }

            // Set up global exception handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"A critical error occurred:\n\n{ex.Message}\n\nThe application will now close.", 
                    "Critical Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nPlease restart the application.", 
                "Application Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Warning);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"A fatal error occurred:\n\n{ex.Message}\n\nThe application will now terminate.", 
                    "Fatal Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        public static bool IsRunAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RestartAsAdministrator()
        {
            try
            {
                // Release the mutex before restarting
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                    mutex = null;
                }

                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Application.ExecutablePath;
                proc.Verb = "runas";
                
                // Start the new process
                Process.Start(proc);
                
                // Immediately exit this instance
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to restart as administrator:\n\n{ex.Message}", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }
    }
}
