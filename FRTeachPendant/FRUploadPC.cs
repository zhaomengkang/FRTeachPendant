using FTPClient;
using System;
using System.IO;
using System.Windows.Forms;

namespace FRTeachPendant
{
    public class FRLoadUserPC
    {
        public static bool LoadMKWebServer(string hostname, int controllerType, string username, string password, Form mainForm)
        {
            bool resultOk = false;

            // Check whether the current version matches the controller
            string serverVersion = FRTeachPendant.MkWebClient.GetLibVersion("http://" + hostname);
            string formText = mainForm.Text;

            // Determine whether the versions match (case-insensitive)
            bool isVersionMatch = !string.IsNullOrEmpty(serverVersion) &&
                                  !string.IsNullOrEmpty(formText) &&
                                  formText.IndexOf(serverVersion, StringComparison.OrdinalIgnoreCase) >= 0;

            if (isVersionMatch)
            {
                return true;
            }

            // Get local application base path
            string appBasePath = AppDomain.CurrentDomain.BaseDirectory;

            // Select local KAREL folder by controller type
            string localKarelFolder;

            switch (controllerType)
            {
                case 1: // 7DA R-30iA Controller
                    localKarelFolder = Path.Combine(appBasePath, "KAREL", "7DA");
                    break;

                case 2: // 7DC R-30iB Controller
                    localKarelFolder = Path.Combine(appBasePath, "KAREL", "7DC");
                    break;

                case 3: // 7DF R-30iB Plus Controller
                    localKarelFolder = Path.Combine(appBasePath, "KAREL", "7DF");
                    break;

                case 4: // 7DH R-50iA Controller
                    localKarelFolder = Path.Combine(appBasePath, "KAREL", "7DH");
                    break;

                default:
                    throw new Exception("Invalid controller type");
            }

            string localFilePath = Path.Combine(localKarelFolder, "a_mkweb.pc");

            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException("Local file not found.", localFilePath);
            }

            DeletSimKey(hostname, username, password);

            FtpClient ftpClient = new FtpClient(hostname, username, password);
            try
            {
                ftpClient.Connect();

                if (ftpClient.IsConnected == true)
                {
                    ftpClient.ServerPath = "MDB:";
                    ftpClient.Upload(localFilePath, "A_MKWEB.PC");
                    resultOk = true;
                }
            }
            catch
            {
                resultOk = false;
                throw new Exception("Please input name and password");
            }
            finally
            {
                if (ftpClient.IsConnected)
                {
                    ftpClient.Disconnect();
                }
            }

            return resultOk;
        }

        public static bool DeletSimKey(string hostname, string username, string password)
        {
            bool resultOk = false;
            FtpClient ftpClient = new FtpClient(hostname, username, password);
            try
            {
                ftpClient.Connect();

                if (ftpClient.IsConnected == true)
                {
                    ftpClient.ServerPath = "MDB:";
                    ftpClient.DeleteFile("A_MKWEB.PC");
                    ftpClient.DeleteFile("A_MKWEB.VR");
                    resultOk = true;
                }
            }
            catch
            {
                resultOk = false;
            }
            finally
            {
                if (ftpClient.IsConnected)
                {
                    ftpClient.Disconnect();
                }
            }

            return resultOk;
        }
    }
}