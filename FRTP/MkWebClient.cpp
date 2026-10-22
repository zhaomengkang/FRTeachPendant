#include "pch.h"
#include "Util.h"
#include "MkWebClient.h"

namespace
{
    // Performs a GET. Returns the HTTP status (0 on transport error); body filled on 2xx.
    DWORD HttpGet(const std::wstring& ip, const std::wstring& query, std::string* body, DWORD timeoutMs)
    {
        std::wstring host = Trim(ip);
        // Tolerate "http://host" input
        if (_wcsnicmp(host.c_str(), L"http://", 7) == 0) host = host.substr(7);
        while (!host.empty() && host.back() == L'/') host.pop_back();

        HINTERNET ses = WinHttpOpen(L"FRTP/1.0", WINHTTP_ACCESS_TYPE_NO_PROXY, nullptr, nullptr, 0);
        if (!ses) return 0;
        WinHttpSetTimeouts(ses, 3000, 3000, (int)timeoutMs, (int)timeoutMs);

        DWORD status = 0;
        std::wstring hostOnly = host;
        INTERNET_PORT port = INTERNET_DEFAULT_HTTP_PORT;
        size_t colon = host.rfind(L':');
        if (colon != std::wstring::npos)
        {
            port = (INTERNET_PORT)_wtoi(host.c_str() + colon + 1);
            hostOnly = host.substr(0, colon);
        }

        HINTERNET con = WinHttpConnect(ses, hostOnly.c_str(), port, 0);
        HINTERNET req = con ? WinHttpOpenRequest(con, L"GET", (L"/KAREL/A_MKWEB?" + query).c_str(), nullptr,
                                                 WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, 0) : nullptr;
        if (req && WinHttpSendRequest(req, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0) &&
            WinHttpReceiveResponse(req, nullptr))
        {
            DWORD sz = sizeof(status);
            WinHttpQueryHeaders(req, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, WINHTTP_HEADER_NAME_BY_INDEX,
                                &status, &sz, WINHTTP_NO_HEADER_INDEX);
            if (body && status >= 200 && status < 300)
            {
                DWORD avail = 0;
                while (WinHttpQueryDataAvailable(req, &avail) && avail > 0)
                {
                    std::string chunk(avail, '\0');
                    DWORD rd = 0;
                    if (!WinHttpReadData(req, &chunk[0], avail, &rd) || rd == 0) break;
                    body->append(chunk, 0, rd);
                }
            }
        }
        if (req) WinHttpCloseHandle(req);
        if (con) WinHttpCloseHandle(con);
        WinHttpCloseHandle(ses);
        return status;
    }

    std::string GetString(const std::wstring& ip, const std::wstring& query, DWORD timeoutMs = 5000)
    {
        std::string body;
        DWORD st = HttpGet(ip, query, &body, timeoutMs);
        if (st < 200 || st >= 300) return std::string();
        return body;
    }

    std::string Node(const std::string& xml, const char* name, bool& found)
    {
        std::string v;
        found = XmlParse(xml, "mklib") && XmlChild(xml, name, v);
        return v;
    }

    int ToInt(const std::string& s)
    {
        return atoi(ToUtf8(Trim(FromUtf8(s))).c_str());
    }

    MkWebClient::VarResponse ParseVar(const std::string& xml, const char* node)
    {
        MkWebClient::VarResponse r;
        if (!XmlParse(xml, "mklib")) { r.status = -999; return r; }
        bool found;
        std::string inner = Node(xml, node, found);
        if (!found) { r.status = -998; return r; }
        std::string v;
        if (XmlChild(inner, "Program", v)) r.program = FromUtf8(v);
        if (XmlChild(inner, "Var", v)) r.var = FromUtf8(v);
        if (XmlChild(inner, "Value", v)) r.value = FromUtf8(v);
        if (XmlChild(inner, "Status", v)) r.status = ToInt(v);
        return r;
    }
}

namespace MkWebClient
{
    VarResponse WriteVar(const std::wstring& ip, const std::wstring& prog, const std::wstring& var,
                         const std::wstring& type, const std::wstring& value)
    {
        std::wstring q = L"func_name=WRITE_VAR&prog_name=" + UrlEncode(prog) + L"&var_name=" + UrlEncode(var) +
                         L"&var_type=" + UrlEncode(type) + L"&var_value=" + UrlEncode(value);
        return ParseVar(GetString(ip, q), "WriteVar");
    }

    VarResponse ReadVar(const std::wstring& ip, const std::wstring& prog, const std::wstring& var,
                        const std::wstring& type)
    {
        std::wstring q = L"func_name=READ_VAR&prog_name=" + UrlEncode(prog) + L"&var_name=" + UrlEncode(var) +
                         L"&var_type=" + UrlEncode(type);
        return ParseVar(GetString(ip, q), "ReadVar");
    }

    std::wstring GetLibVersion(const std::wstring& ip, DWORD timeoutMs)
    {
        std::string xml = GetString(ip, L"func_name=LIB_VER", timeoutMs);
        bool found;
        std::string v = Node(xml, "Version", found);
        return found ? FromUtf8(v) : std::wstring();
    }

    int CheckAuthStatus(const std::wstring& ip)
    {
        DWORD st = HttpGet(ip, L"func_name=LIB_VER", nullptr, 5000);
        if (st >= 200 && st < 300) return 0;
        if (st == 401) return 1;
        if (st == 402) return 2;
        if (st == 403) return 3;
        return -1;
    }

    int SimTpKey(const std::wstring& ip, const std::wstring& keyCodes)
    {
        std::string xml = GetString(ip, L"func_name=SIM_KEY&key_codes=" + UrlEncode(keyCodes));
        if (!XmlParse(xml, "mklib")) return -999;
        bool found;
        std::string v = Node(xml, "SimKeyStatus", found);
        if (!found) return -999;
        std::string t = ToUtf8(Trim(FromUtf8(v)));
        char* end = nullptr;
        long n = strtol(t.c_str(), &end, 10);
        return (end && end != t.c_str() && *end == '\0') ? (int)n : -999;
    }

    IoValueResponse GetIoValue(const std::wstring& ip, int ioType, int ioIndex)
    {
        IoValueResponse r;
        std::wstring q = L"func_name=GET_IO_VAL&io_type=" + std::to_wstring(ioType) + L"&io_index=" + std::to_wstring(ioIndex);
        std::string xml = GetString(ip, q);
        if (!XmlParse(xml, "mklib")) { r.status = -999; return r; }
        bool found;
        std::string inner = Node(xml, "GetIOValue", found);
        if (!found) { r.status = -998; return r; }
        std::string v;
        int value = 0;
        if (XmlChild(inner, "IOType", v)) r.ioType = ToInt(v);
        if (XmlChild(inner, "IOIndex", v)) r.ioIndex = ToInt(v);
        if (XmlChild(inner, "Value", v)) value = ToInt(v);
        if (XmlChild(inner, "Status", v)) r.status = ToInt(v);
        r.value = r.status == 0 ? value : 0;
        return r;
    }
}
