using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace FRTeachPendant
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
                        return "Unknown";

                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = timeoutMs;
                    stream.WriteTimeout = timeoutMs;

                    string welcome = ReadLine(stream, timeoutMs);
                    if (welcome == null || !welcome.StartsWith("220 "))
                        return "Unknown";

                    string version = ParseRobotVersion(welcome);
                    if (version == null)
                        return "Unknown";

                    byte[] quitBytes = Encoding.ASCII.GetBytes("QUIT\r\n");
                    stream.Write(quitBytes, 0, quitBytes.Length);

                    _ = ReadLine(stream, timeoutMs);
                    return version;
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string ParseRobotVersion(string welcomeMessage)
        {
            var match = Regex.Match(welcomeMessage, @"V\d+(\.\d+)?[A-Z]?/\d+");
            if (!match.Success)
                return null;
            string versionStr = match.Value; 
            var majorMatch = Regex.Match(versionStr, @"V(\d+)");
            if (!majorMatch.Success)
                return null;
            string majorVer = majorMatch.Groups[1].Value;

            switch (majorVer)
            {
                case "7":
                    return "R-30iA";
                case "8":
                    return "R-30iB";
                case "9":
                    return "R-30iB Plus";
                case "10":
                    return "R-50iA";
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