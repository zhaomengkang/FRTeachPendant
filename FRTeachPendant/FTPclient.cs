using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace FTPClient
{
    public class FtpClient : IDisposable
    {
        private readonly string _hostname;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        private string _serverPath;
        private bool _isConnected;

        private TcpClient _controlClient;
        private NetworkStream _controlStream;
        private StreamReader _reader;
        private StreamWriter _writer;

        public FtpClient(string hostname, string username, string password)
            : this(hostname, 21, username, password)
        {
        }

        public FtpClient(string hostname, int port, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                throw new ArgumentException("Hostname cannot be empty.", nameof(hostname));

            if (hostname.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                hostname = hostname.Substring(6);
            }

            _hostname = hostname.Trim().Trim('/');
            _port = port;
            _username = username ?? string.Empty;
            _password = password ?? string.Empty;
            _serverPath = "/";
            _isConnected = false;
        }

        public string ServerPath
        {
            get { return _serverPath; }
            set { _serverPath = NormalizePath(value); }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
        }

        public void Connect()
        {
            if (_isConnected)
                return;

            try
            {
                _controlClient = new TcpClient();
                _controlClient.Connect(_hostname, _port);
                _controlStream = _controlClient.GetStream();

                _reader = new StreamReader(_controlStream, Encoding.ASCII, false, 1024, true);
                _writer = new StreamWriter(_controlStream, Encoding.ASCII, 1024, true);
                _writer.NewLine = "\r\n";
                _writer.AutoFlush = true;

                FtpResponse response = ReadResponse();
                EnsureCode(response, 220);

                response = SendCommand("USER " + _username);
                if (response.Code == 331)
                {
                    response = SendCommand("PASS " + _password);
                    EnsureCode(response, 230);
                }
                else if (response.Code != 230)
                {
                    throw new Exception("FTP login failed: " + response.Message);
                }

                SendCommandExpectSuccess("TYPE I");
                _isConnected = true;
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_writer != null && _controlClient != null && _controlClient.Connected)
                {
                    try
                    {
                        SendCommand("QUIT");
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                if (_reader != null) _reader.Dispose();
                if (_writer != null) _writer.Dispose();
                if (_controlStream != null) _controlStream.Dispose();
                if (_controlClient != null) _controlClient.Close();

                _reader = null;
                _writer = null;
                _controlStream = null;
                _controlClient = null;
                _isConnected = false;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        public string GetCurrentPath()
        {
            EnsureConnected();
            return _serverPath;
        }

        public List<FtpFile> ListDirectory()
        {
            return ListDirectory(_serverPath);
        }

        public List<FtpFile> ListDirectory(string path)
        {
            EnsureConnected();

            path = NormalizePath(path);

            TcpClient dataClient = OpenPassiveDataClient();
            try
            {
                FtpResponse response = SendCommand("LIST " + path);
                EnsurePreliminary(response);

                List<FtpFile> files = new List<FtpFile>();

                using (NetworkStream dataStream = dataClient.GetStream())
                using (StreamReader dataReader = new StreamReader(dataStream, Encoding.ASCII))
                {
                    string line;
                    while ((line = dataReader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            files.Add(new FtpFile(line));
                        }
                    }
                }

                FtpResponse finalResponse = ReadResponse();
                EnsureSuccess(finalResponse);

                return files;
            }
            finally
            {
                dataClient.Close();
            }
        }

        public bool FileExists(string fileName)
        {
            EnsureConnected();

            string fullPath = BuildRemotePath(fileName);

            FtpResponse response = SendCommand("SIZE " + fullPath);
            if (response.Code == 213)
                return true;

            if (response.Code == 550)
                return false;

            throw new Exception("Failed to check whether the file exists: " + response.Message);
        }

        public void Upload(string localPath, string remotePath)
        {
            EnsureConnected();

            if (!File.Exists(localPath))
                throw new FileNotFoundException("Local file does not exist.", localPath);

            remotePath = BuildRemotePath(remotePath);

            TcpClient dataClient = OpenPassiveDataClient();
            try
            {
                FtpResponse response = SendCommand("STOR " + remotePath);
                EnsurePreliminary(response);

                using (FileStream sourceStream = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                using (NetworkStream dataStream = dataClient.GetStream())
                {
                    sourceStream.CopyTo(dataStream);
                }

                FtpResponse finalResponse = ReadResponse();
                EnsureSuccess(finalResponse);
            }
            finally
            {
                dataClient.Close();
            }
        }

        public void UploadByteArray(byte[] byteArray, string remotePath)
        {
            EnsureConnected();

            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));

            remotePath = BuildRemotePath(remotePath);

            TcpClient dataClient = OpenPassiveDataClient();
            try
            {
                FtpResponse response = SendCommand("STOR " + remotePath);
                EnsurePreliminary(response);

                using (NetworkStream dataStream = dataClient.GetStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Flush();
                }

                FtpResponse finalResponse = ReadResponse();
                EnsureSuccess(finalResponse);
            }
            finally
            {
                dataClient.Close();
            }
        }

        public void Download(string remotePath, string localPath)
        {
            EnsureConnected();

            remotePath = BuildRemotePath(remotePath);

            string dir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            TcpClient dataClient = OpenPassiveDataClient();
            try
            {
                FtpResponse response = SendCommand("RETR " + remotePath);
                EnsurePreliminary(response);

                using (NetworkStream dataStream = dataClient.GetStream())
                using (FileStream fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                {
                    dataStream.CopyTo(fileStream);
                }

                FtpResponse finalResponse = ReadResponse();
                EnsureSuccess(finalResponse);
            }
            finally
            {
                dataClient.Close();
            }
        }

        public void CreateDirectory(string directoryName)
        {
            EnsureConnected();

            string fullPath = BuildRemotePath(directoryName);
            FtpResponse response = SendCommand("MKD " + fullPath);

            if (response.Code != 257 && response.Code != 250)
                throw new Exception("Failed to create directory: " + response.Message);
        }

        public void DeleteFile(string filename)
        {
            EnsureConnected();

            string fullPath = BuildRemotePath(filename);
            FtpResponse response = SendCommand("DELE " + fullPath);
            EnsureSuccess(response);
        }

        public void DeleteDirectory(string directoryName)
        {
            EnsureConnected();

            string fullPath = BuildRemotePath(directoryName);
            FtpResponse response = SendCommand("RMD " + fullPath);
            EnsureSuccess(response);
        }

        public void ChangeDirectory(string path)
        {
            EnsureConnected();

            path = NormalizePath(path);
            FtpResponse response = SendCommand("CWD " + path);
            EnsureSuccess(response);
            _serverPath = path;
        }

        public long GetFileSize(string fileName)
        {
            EnsureConnected();

            string fullPath = BuildRemotePath(fileName);
            FtpResponse response = SendCommand("SIZE " + fullPath);

            if (response.Code != 213)
                throw new Exception("Failed to get file size: " + response.Message);

            long size;
            if (!long.TryParse(response.MessageText, out size))
                throw new Exception("Invalid file size format returned by the server: " + response.MessageText);

            return size;
        }

        public void Rename(string oldName, string newName)
        {
            EnsureConnected();

            string oldPath = BuildRemotePath(oldName);
            string newPath = BuildRemotePath(newName);

            FtpResponse response = SendCommand("RNFR " + oldPath);
            if (response.Code != 350)
                throw new Exception("Rename failed (RNFR): " + response.Message);

            response = SendCommand("RNTO " + newPath);
            EnsureSuccess(response);
        }

        private TcpClient OpenPassiveDataClient()
        {
            FtpResponse response = SendCommand("PASV");
            if (response.Code != 227)
                throw new Exception("Failed to enter passive mode: " + response.Message);

            PassiveEndpoint endpoint = ParsePassiveEndpoint(response.MessageText);
            TcpClient client = new TcpClient();
            client.Connect(endpoint.Host, endpoint.Port);
            return client;
        }

        private FtpResponse SendCommand(string command)
        {
            EnsureControlConnection();

            _writer.WriteLine(command);
            _writer.Flush();

            return ReadResponse();
        }

        private void SendCommandExpectSuccess(string command)
        {
            FtpResponse response = SendCommand(command);
            EnsureSuccess(response);
        }

        private FtpResponse ReadResponse()
        {
            EnsureControlConnection();

            string line = _reader.ReadLine();
            if (line == null)
                throw new IOException("The FTP server closed the connection.");

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(line);

            int code;
            if (line.Length < 3 || !int.TryParse(line.Substring(0, 3), out code))
                throw new Exception("Invalid FTP response: " + line);

            if (line.Length > 3 && line[3] == '-')
            {
                string endMark = code.ToString() + " ";
                while (true)
                {
                    string nextLine = _reader.ReadLine();
                    if (nextLine == null)
                        throw new IOException("The FTP server closed the connection.");

                    builder.AppendLine(nextLine);

                    if (nextLine.StartsWith(endMark))
                    {
                        line = nextLine;
                        break;
                    }
                }
            }

            string message = builder.ToString().TrimEnd();
            string messageText = line.Length > 4 ? line.Substring(4) : string.Empty;

            return new FtpResponse
            {
                Code = code,
                Message = message,
                MessageText = messageText
            };
        }

        private static PassiveEndpoint ParsePassiveEndpoint(string message)
        {
            int start = message.IndexOf('(');
            int end = message.IndexOf(')');

            if (start < 0 || end <= start)
                throw new Exception("Invalid PASV response format: " + message);

            string[] parts = message.Substring(start + 1, end - start - 1).Split(',');
            if (parts.Length != 6)
                throw new Exception("Invalid PASV response format: " + message);

            string host = parts[0] + "." + parts[1] + "." + parts[2] + "." + parts[3];
            int p1 = int.Parse(parts[4]);
            int p2 = int.Parse(parts[5]);
            int port = p1 * 256 + p2;

            return new PassiveEndpoint
            {
                Host = host,
                Port = port
            };
        }

        private string BuildRemotePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return _serverPath;

            path = path.Replace("\\", "/").Trim();

            if (path.StartsWith("/"))
                return path;

            if (_serverPath == "/")
                return "/" + path;

            return _serverPath.TrimEnd('/') + "/" + path;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            path = path.Replace("\\", "/").Trim();

            if (!path.StartsWith("/"))
                path = "/" + path;

            while (path.Contains("//"))
            {
                path = path.Replace("//", "/");
            }

            if (path.Length > 1)
                path = path.TrimEnd('/');

            return path;
        }

        private void EnsureConnected()
        {
            if (!_isConnected)
                throw new Exception("Not connected to the FTP server.");
        }

        private void EnsureControlConnection()
        {
            if (_controlClient == null || _controlStream == null || _reader == null || _writer == null)
                throw new Exception("The FTP control connection is unavailable.");
        }

        private static void EnsureCode(FtpResponse response, int expectedCode)
        {
            if (response.Code != expectedCode)
                throw new Exception("FTP error: " + response.Message);
        }

        private static void EnsurePreliminary(FtpResponse response)
        {
            if (response.Code != 150 && response.Code != 125)
                throw new Exception("FTP error: " + response.Message);
        }

        private static void EnsureSuccess(FtpResponse response)
        {
            if (response.Code < 200 || response.Code >= 300)
                throw new Exception("FTP error: " + response.Message);
        }
    }

    public class FtpResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string MessageText { get; set; }
    }

    public class PassiveEndpoint
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }

    public class FtpFile
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public FtpFileType Type { get; set; }
        public string Raw { get; set; }

        public FtpFile(string details)
        {
            Raw = details;
            Name = details;
            Size = 0;
            Type = FtpFileType.Unknown;

            if (string.IsNullOrWhiteSpace(details))
                return;

            string[] unixParts = details.Split(new[] { ' ' }, 9, StringSplitOptions.RemoveEmptyEntries);
            if (unixParts.Length >= 9)
            {
                Name = unixParts[8];

                long size;
                if (long.TryParse(unixParts[4], out size))
                    Size = size;

                char typeChar = unixParts[0][0];
                if (typeChar == 'd')
                    Type = FtpFileType.Directory;
                else if (typeChar == '-')
                    Type = FtpFileType.File;
                else
                    Type = FtpFileType.Unknown;

                return;
            }

            string[] winParts = details.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (winParts.Length >= 4)
            {
                Name = winParts[winParts.Length - 1];

                if (details.IndexOf("<DIR>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Type = FtpFileType.Directory;
                    Size = 0;
                }
                else
                {
                    Type = FtpFileType.File;
                    for (int i = winParts.Length - 2; i >= 0; i--)
                    {
                        long size;
                        if (long.TryParse(winParts[i], out size))
                        {
                            Size = size;
                            break;
                        }
                    }
                }
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