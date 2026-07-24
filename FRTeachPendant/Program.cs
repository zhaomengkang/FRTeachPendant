using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Windows.Forms;

namespace FRTeachPendant
{
    internal static class Program
    {
        /// <summary>
        /// Main entry point of the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region Check whether the OCX is registered

            // Specify the registry path and value name to check
            string typeLibGuid = "{34F4C4DB-A64B-4D87-99DA-042F7FB7DEBA}";
            string registryPath = $@"TypeLib\{typeLibGuid}";

            // Check whether the registry key exists
            if (CheckRegistryKeyExists(registryPath) == false)
            {
                // Ask the user whether to register now
                DialogResult result = MessageBox.Show(
                    "The OCX file is not registered. Would you like to register it now?",
                    "Notice",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    // User chose not to register
                    MessageBox.Show(
                        "If the OCX file is not registered, the teach pendant screen may not display properly. Only backup/upload functions will be available.",
                        "Notice",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    goto RunApplication;
                }

                #region Elevation

                // Get the current Windows user
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                // Check whether the current user is an administrator
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    // If already running as administrator, register directly
                    goto RegisterOCX;
                }
                else
                {
                    // Create startup info
                    ProcessStartInfo processStartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = Application.ExecutablePath,
                        Verb = "runas"
                    };

                    try
                    {
                        Process.Start(processStartInfo);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(
                            "Unable to run the program as administrator. Please right-click the program and select 'Run as administrator'.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    Application.Exit();
                    return;
                }

            #endregion

            #region Register OCX file

            RegisterOCX:
                // Get the directory of the current executable
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string directory = Path.GetDirectoryName(exePath);

                // Build the full path of the OCX file
                string ocxPath = Path.Combine(directory, "fripendant.ocx");

                // Create a Process object to call regsvr32
                Process p = new Process();
                p.StartInfo.FileName = "regsvr32.exe";
                p.StartInfo.Arguments = "/s " + "\"" + ocxPath + "\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Verb = "runas";
                p.Start();

                // Read regsvr32 output
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                // Check the exit code of regsvr32
                if (p.ExitCode != 0)
                {
                    MessageBox.Show(
                        "Failed to register the OCX file. The teach pendant screen will not display properly.\r\n" +
                        "Please install the VC runtime from the Help page later.\r\n" +
                        "Please make sure the following VC runtime is installed:\r\n" +
                        "- Microsoft Visual C++ 2008 Redistributable - 9.0.30729\r\n",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    Application.Run(new Help());
                    return;
                }
                else
                {
                    MessageBox.Show(
                        "The OCX file was registered successfully. Please run the application again.",
                        "Notice",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    Application.Exit();
                    return;
                }

                #endregion
            }

        #endregion

        RunApplication:
            Application.EnableVisualStyles();
            Application.Run(new mainForm());
        }

        static bool CheckRegistryKeyExists(string path)
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(path, false))
                {
                    return key != null;
                }
            }
            catch (SecurityException)
            {
                // The current user may not have sufficient permission to access the registry key
                Console.WriteLine("Insufficient permission to check the registry key.");
                return false;
            }
        }
    }
}