using System;
using System.Net;

namespace FR_TeachPendant
{
    public class Rpc
    {
        public static string WriteCMd(string ip, string cmd)
        {
            // 处理命令字符串：转为大写，并对空格和反斜杠进行URL编码
            cmd = cmd.ToUpper();
            cmd = cmd.Replace(" ", "%20");
            cmd = cmd.Replace("\\", "%5C");

            // 使用UriBuilder构建URL
            UriBuilder uriBuilder = new UriBuilder("http", ip);
            uriBuilder.Path = $"/COMET/{cmd}";
            string url = uriBuilder.ToString();
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    // 请求URL并返回内容
                    byte[] responseBytes = webClient.DownloadData(url);
                    // 将字节数组转换为字符串
                    string response = System.Text.Encoding.UTF8.GetString(responseBytes);
                    string bodyContent = response;
                    return bodyContent.Trim();
                }
            }
            catch 
            {
                return ("");
            }
        }
    }
}
