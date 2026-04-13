using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace FR_TeachPendant
{
    internal class RobotChecker
    {
        public static string GetRobotVersion(string ip, int port = 21, int timeoutMs = 5000)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var ar = client.BeginConnect(ip, port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(timeoutMs))
                    {
                        client.EndConnect(ar);
                    }
                   
                    if (!client.Connected)
                        return null;

                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = timeoutMs;
                    stream.WriteTimeout = timeoutMs;

                    string welcome = ReadLine(stream, timeoutMs);
                    if (welcome == null || !welcome.StartsWith("220 "))
                        return null;

                    string version = ParseRobotVersion(welcome);
                    if (version == null)
                        return null;

                    byte[] quitBytes = Encoding.ASCII.GetBytes("QUIT\r\n");
                    stream.Write(quitBytes, 0, quitBytes.Length);

                    // 读一下服务端的响应
                    _ = ReadLine(stream, timeoutMs);
                    return version;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string ParseRobotVersion(string welcomeMessage)
        {
            // 先提取版本号 格式如 V9.40P/79
            var match = Regex.Match(welcomeMessage, @"V\d+(\.\d+)?[A-Z]?/\d+");
            if (!match.Success)
                return null;
            string versionStr = match.Value; // e.g. "V9.40P/79"
            // 解析第一个数字字符，判断型号
            char majorVer = versionStr.Length > 1 ? versionStr[1] : '\0';
            switch (majorVer)
            {
                case '9':
                    return "R-30iB Plus";
                case '8':
                    return "R-30iB";
                default:
                    return null;
            }
        }
        private static string ReadLine(NetworkStream stream, int timeoutMs)
        {
            var buffer = new byte[1024];
            var sb = new StringBuilder();
            DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);

            while (DateTime.Now < deadline)
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    string str = sb.ToString();
                    int idx = str.IndexOf("\r\n");
                    if (idx >= 0)
                    {
                        return str.Substring(0, idx);
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(50);
                }
            }

            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}