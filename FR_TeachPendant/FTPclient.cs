using System;
using System.Collections.Generic;
using System.IO;
using System.Net;


namespace FTPClient
{
    public class FtpClient
    {
        private string _hostname;
        private string _username;
        private string _password;
        private string _serverPath;
        private bool _isConnected;

        public FtpClient(string hostname, string username, string password)
        {
            if (!hostname.StartsWith("ftp://"))
            {
                _hostname = "ftp://" + hostname;
            }
            else
            {
                _hostname = hostname;
            }
            _username = username;
            _password = password;
            _serverPath = "/";
            _isConnected = false;
        }

        public string ServerPath
        {
            get { return _serverPath; }
            set
            {
                if (!value.StartsWith("/"))
                {
                    _serverPath = "/" + value;
                }
                else
                {
                    _serverPath = value;
                }
            }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
        }

        public void Connect()
        {
            try
            {
                _isConnected = true;
                ListDirectory();
            }
            catch
            {
                _isConnected = false;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
        }

        public string GetCurrentPath()
        {
            return _serverPath;
        }

        public List<FtpFile> ListDirectory()
        {
            if (!_isConnected)
            {
                throw new Exception("Not connected to FTP server.");
            }

            List<FtpFile> files = new List<FtpFile>();
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_hostname + _serverPath);
            request.Credentials = new NetworkCredential(_username, _password);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    files.Add(new FtpFile(line));
                }
            }

            return files;
        }

        public bool FileExists(string fileName)
        {
            if (!_isConnected)
            {
                throw new Exception("Not connected to FTP server.");
            }

            bool exists = false;
            List<FtpFile> files = ListDirectory();

            foreach (FtpFile file in files)
            {
                if (file.Name == fileName)
                {
                    exists = true;
                    break;
                }
            }
            return exists;
        }
        public void Upload(string localPath, string remotePath)
        {
            if (!_isConnected)
            {
                throw new Exception("Not connected to FTP server.");
            }

            byte[] fileContents;
            using (FileStream sourceStream = new FileStream(localPath, FileMode.Open, FileAccess.Read))
            {
                fileContents = new byte[sourceStream.Length];
                sourceStream.Read(fileContents, 0, fileContents.Length);
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_hostname + _serverPath + "/" + remotePath);
            request.Credentials = new NetworkCredential(_username, _password);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.ContentLength = fileContents.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Upload File Complete, status: {response.StatusDescription}");
            }
        }

        public void UploadByteArray(byte[] byteArray, string remotePath)
        {
            if (!_isConnected)
            {
                throw new Exception("Not connected to FTP server.");
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_hostname + _serverPath + "/" + remotePath);
            request.Credentials = new NetworkCredential(_username, _password);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.ContentLength = byteArray.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(byteArray, 0, byteArray.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Upload Byte Array Complete, status: {response.StatusDescription}");
            }
        }

        public void Download(string remotePath, string localPath)
        {
            if (!_isConnected)
            {
                throw new Exception("Not connected to FTP server.");
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_hostname + _serverPath + "/" + remotePath);
            request.Credentials = new NetworkCredential(_username, _password);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (FileStream writer = new FileStream(localPath, FileMode.Create))
            {
                byte[] buffer = new byte[2048];
                int bytesRead;
                while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }

        public void CreateDirectory(string directoryName)
        {
            if (!_isConnected)
            {
                throw new Exception("Not connected to FTP server.");
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_hostname + _serverPath + "/" + directoryName);
            request.Credentials = new NetworkCredential(_username, _password);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Create Directory Complete, status: {response.StatusDescription}");
            }
        }

        public void DeleteFile(string filename)
        {
            if (!_isConnected)
            {
                throw new Exception("Not connected to FTP server.");
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_hostname + _serverPath + "/" + filename);
            request.Credentials = new NetworkCredential(_username, _password);
            request.Method = WebRequestMethods.Ftp.DeleteFile;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Delete File Complete, status: {response.StatusDescription}");
            }
        }

        public void DeleteDirectory(string directoryName)
        {
            if (!_isConnected)
            {
                throw new Exception("Not connected to FTP server.");
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_hostname + _serverPath + "/" + directoryName);
            request.Credentials = new NetworkCredential(_username, _password);
            request.Method = WebRequestMethods.Ftp.RemoveDirectory;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Delete Directory Complete, status: {response.StatusDescription}");
            }
        }
    }

    public class FtpFile
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public FtpFileType Type { get; set; }

        public FtpFile(string details)
        {
            string[] parts = details.Split(new[] { ' ' }, 9, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 9)
            {
                Name = parts[8];
                Size = long.Parse(parts[4]);
                Type = parts[0][0] == 'd' ? FtpFileType.Directory : FtpFileType.File;
            }
            else
            {
                Name = details;
                Size = 0;
                Type = FtpFileType.Unknown;
            }
        }
    }
    public enum FtpFileType
    {
        File,
        Directory,
        Unknown
    }
}

