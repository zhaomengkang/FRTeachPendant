using System;
using System.Net.Http;
using System.Xml.Linq;
using System.Collections.Generic;

namespace FR_TeachPendant
{
    public static class MkWebClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static WriteVarResponse WriteVar(string ip, string progName, string varName, string varType, string varValue)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["func_name"] = "WRITE_VAR",
                ["prog_name"] = progName,
                ["var_name"] = varName,
                ["var_type"] = varType,
                ["var_value"] = varValue
            };

            var url = BuildUrl(ip, "KAREL/A_MKWEB", queryParams);
            var responseXml = GetString(url);
            return WriteVarResponse.Parse(responseXml);
        }

        public static ReadVarResponse ReadVar(string ip, string progName, string varName, string varType)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["func_name"] = "READ_VAR",
                ["prog_name"] = progName,
                ["var_name"] = varName,
                ["var_type"] = varType
            };

            var url = BuildUrl(ip, "KAREL/A_MKWEB", queryParams);
            var responseXml = GetString(url);
            return ReadVarResponse.Parse(responseXml);
        }

        public static string GetLibVersion(string ip)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["func_name"] = "LIB_VER"
            };
            var url = BuildUrl(ip, "KAREL/A_MKWEB", queryParams);
            var xml = GetString(url);
            try
            {
                var doc = XDocument.Parse(xml);
                return doc.Root.Element("Version")?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static int SimTpKey(string ip, string keyCodes)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["func_name"] = "SIM_KEY",
                ["key_codes"] = keyCodes
            };
            var url = BuildUrl(ip, "KAREL/A_MKWEB", queryParams);
            var xml = GetString(url);

            try
            {
                var doc = XDocument.Parse(xml);
                var node = doc.Root.Element("SimKeyStatus");
                if (node == null)
                    return -999;
                if (int.TryParse(node.Value, out int status))
                    return status;
                return -999;
            }
            catch
            {
                return -999;
            }
        }

        public static DisablePassResponse DisablePass(string ip, string passId)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["func_name"] = "DISABL_PASS",
                ["pass_id"] = passId
            };
            var url = BuildUrl(ip, "KAREL/A_MKWEB", queryParams);
            var xml = GetString(url);

            return DisablePassResponse.Parse(xml);
        }

        private static string BuildUrl(string ip, string path, Dictionary<string, string> queryParams)
        {
            var uriBuilder = new UriBuilder(ip);
            if (!uriBuilder.Path.EndsWith("/")) uriBuilder.Path += "/";
            uriBuilder.Path += path.TrimStart('/');

            var queryList = new List<string>();
            foreach (var kvp in queryParams)
            {
                var key = Uri.EscapeDataString(kvp.Key);
                var value = Uri.EscapeDataString(kvp.Value ?? "");
                queryList.Add($"{key}={value}");
            }
            uriBuilder.Query = string.Join("&", queryList);
            return uriBuilder.ToString();
        }

        private static string GetString(string url)
        {
            var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
    }

    public class VarResponse
    {
        public string Program { get; set; }
        public string Var { get; set; }
        public string Value { get; set; }
        public int Status { get; set; }
    }

    public class WriteVarResponse : VarResponse
    {
        public static WriteVarResponse Parse(string xml)
        {
            var resp = new WriteVarResponse();
            try
            {
                var doc = XDocument.Parse(xml);
                var node = doc.Root.Element("WriteVar");
                if (node == null)
                    return resp;

                resp.Program = node.Element("Program")?.Value ?? "";
                resp.Var = node.Element("Var")?.Value ?? "";
                resp.Value = node.Element("Value")?.Value ?? "";
                int.TryParse(node.Element("Status")?.Value, out int status);
                resp.Status = status;
            }
            catch
            {
                resp.Status = -999;
            }

            return resp;
        }
    }

    public class ReadVarResponse : VarResponse
    {
        public static ReadVarResponse Parse(string xml)
        {
            var resp = new ReadVarResponse();
            try
            {
                var doc = XDocument.Parse(xml);
                var node = doc.Root.Element("ReadVar");
                if (node == null)
                    return resp;

                resp.Program = node.Element("Program")?.Value ?? "";
                resp.Var = node.Element("Var")?.Value ?? "";
                resp.Value = node.Element("Value")?.Value ?? "";
                int.TryParse(node.Element("Status")?.Value, out int status);
                resp.Status = status;
            }
            catch
            {
                resp.Status = -999;
            }

            return resp;
        }
    }

    public class DisablePassResponse
    {
        public string PasswordID { get; set; }
        public string ReleaseKey { get; set; }
        public int Status { get; set; }

        public static DisablePassResponse Parse(string xml)
        {
            var resp = new DisablePassResponse();
            try
            {
                var doc = XDocument.Parse(xml);
                var disablePasswordNode = doc.Root.Element("DisablePassword");
                if (disablePasswordNode != null)
                {
                    resp.PasswordID = disablePasswordNode.Element("PasswordID")?.Value ?? "";
                    resp.ReleaseKey = disablePasswordNode.Element("ReleaseKey")?.Value ?? "";
                    int.TryParse(disablePasswordNode.Element("Status")?.Value, out int status);
                    resp.Status = status;
                }
                else
                {
                    resp.Status = -998;
                }
            }
            catch
            {
                resp.Status = -999;
            }
            return resp;
        }
    }
}