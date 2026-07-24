using System;
using System.Net.Http;
using System.Xml.Linq;
using System.Collections.Generic;

namespace FRTeachPendant
{
    public enum IOType
    {
        Any = 0,
        DigitalInput = 1,
        DigitalOutput = 2,
        AnalogInput = 3,
        AnalogOutput = 4,
        ToolOutput = 5,
        PLCInput = 6,
        PLCOutput = 7,
        RobotDigitalInput = 8,
        RobotDigitalOutput = 9,
        BrakeOutput = 10,
        OperatorPanelInput = 11,
        OperatorPanelOutput = 12,
        EmergencyStop = 13,
        TeachPendantInput = 14,
        TeachPendantOutput = 15,
        WeldInput = 16,
        WeldOutput = 17,
        GroupedInput = 18,
        GroupedOutput = 19,
        UserOperatorPanelInput = 20,
        UserOperatorPanelOutput = 21,
        LaserDIN = 22,
        LaserDOUT = 23,
        LaserAIN = 24,
        LaserAOUT = 25,
        WeldStickInput = 26,
        WeldStickOutput = 27,
        MemoryImageBoolean = 28,
        MemoryImageDin = 29,
        DummyBooleanPort = 30,
        DummyNumericPort = 31,
        ProcAxes = 32,
        InternalOperatorPanelInput = 33,
        InternalOperatorPanelOutput = 34,
        Flag = 35,
        Marker = 36,
        GroupedInput32 = 37,
        GroupedOutput32 = 38,
        BackupedInternalRelay = 41,
        NonBackupedInternalRelay = 42,
        BackupedInternalRegister = 43,
        NonBackupedInternalRegister = 44
    }

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
                return doc.Root?.Element("Version")?.Value ?? string.Empty;
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
                var node = doc.Root?.Element("SimKeyStatus");
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

        public static GetIoValueResponse GetIoValue(string ip, IOType ioType, int ioIndex)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["func_name"] = "GET_IO_VAL",
                ["io_type"] = ((int)ioType).ToString(),
                ["io_index"] = ioIndex.ToString()
            };

            var url = BuildUrl(ip, "KAREL/A_MKWEB", queryParams);
            var xml = GetString(url);

            return GetIoValueResponse.Parse(xml);
        }

        private static string BuildUrl(string ip, string path, Dictionary<string, string> queryParams)
        {
            var uriBuilder = new UriBuilder(ip);

            if (!uriBuilder.Path.EndsWith("/"))
                uriBuilder.Path += "/";

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
                var node = doc.Root?.Element("WriteVar");
                if (node == null)
                {
                    resp.Status = -998;
                    return resp;
                }

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
                var node = doc.Root?.Element("ReadVar");
                if (node == null)
                {
                    resp.Status = -998;
                    return resp;
                }

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
                var disablePasswordNode = doc.Root?.Element("DisablePassword");

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

    public class GetIoValueResponse
    {
        public int IOType { get; set; }
        public int IOIndex { get; set; }

        public int Value { get; set; }

        public int Status { get; set; }

        public static GetIoValueResponse Parse(string xml)
        {
            var resp = new GetIoValueResponse();

            try
            {
                var doc = XDocument.Parse(xml);
                var node = doc.Root?.Element("GetIOValue");

                if (node == null)
                {
                    resp.Status = -998;
                    return resp;
                }

                int.TryParse(node.Element("IOType")?.Value, out int ioType);
                int.TryParse(node.Element("IOIndex")?.Value, out int ioIndex);
                int.TryParse(node.Element("Value")?.Value, out int value);
                int.TryParse(node.Element("Status")?.Value, out int status);

                resp.IOType = ioType;
                resp.IOIndex = ioIndex;
                resp.Status = status;
                resp.Value = (status == 0) ? value : 0;
            }
            catch
            {
                resp.Status = -999;
            }

            return resp;
        }
    }
}