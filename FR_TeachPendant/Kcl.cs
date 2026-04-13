using System;
using System.Net;

namespace FR_TeachPendant
{
    internal class Kcl
    {
        public static string WriteCMd(string ip, string cmd)
        {
            // 处理命令字符串：转为大写，并对空格和反斜杠进行URL编码
            cmd = cmd.ToUpper();
            cmd = cmd.Replace(" ", "%20");
            cmd = cmd.Replace("\\", "%5C");

            // 使用UriBuilder构建URL
            UriBuilder uriBuilder = new UriBuilder("http", ip);
            uriBuilder.Path = $"/KCL/{cmd}";
            string url = uriBuilder.ToString();

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    // 请求URL并返回内容
                    byte[] responseBytes = webClient.DownloadData(url);
                    // 将字节数组转换为字符串
                    string response = System.Text.Encoding.UTF8.GetString(responseBytes);

                    // 查找<body topmargin="0">和</body>之间的内容
                    string startTag = "<XMP>";
                    string endTag = "</XMP>";
                    int startIndex = response.IndexOf(startTag);
                    int endIndex = response.IndexOf(endTag);

                    // 如果找到这两个标签
                    if (startIndex != -1 && endIndex != -1)
                    {
                        startIndex += startTag.Length; // 跳过开始标签的长度
                        string bodyContent = response.Substring(startIndex, endIndex - startIndex);
                        return bodyContent.Trim();
                    }
                    else
                    {
                        throw new Exception("KCL Server not ready");
                    }
                }
            }
            catch (Exception ex)
            {
                // 在发生异常时，返回异常信息
                throw new Exception("KCL " + ex.Message);
            }
        }
    }
}
