#include "pch.h"
#include "Util.h"

std::string ToUtf8(const std::wstring& s)
{
    if (s.empty()) return std::string();
    int n = WideCharToMultiByte(CP_UTF8, 0, s.data(), (int)s.size(), nullptr, 0, nullptr, nullptr);
    std::string r(n, '\0');
    WideCharToMultiByte(CP_UTF8, 0, s.data(), (int)s.size(), &r[0], n, nullptr, nullptr);
    return r;
}

std::wstring FromUtf8(const std::string& s)
{
    if (s.empty()) return std::wstring();
    int n = MultiByteToWideChar(CP_UTF8, 0, s.data(), (int)s.size(), nullptr, 0);
    std::wstring r(n, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, s.data(), (int)s.size(), &r[0], n);
    return r;
}

std::wstring Trim(const std::wstring& s)
{
    size_t b = s.find_first_not_of(L" \t\r\n");
    if (b == std::wstring::npos) return std::wstring();
    size_t e = s.find_last_not_of(L" \t\r\n");
    return s.substr(b, e - b + 1);
}

std::wstring ToUpper(std::wstring s) { for (auto& c : s) c = (wchar_t)towupper(c); return s; }
std::wstring ToLower(std::wstring s) { for (auto& c : s) c = (wchar_t)towlower(c); return s; }

std::wstring UrlEncode(const std::wstring& s)
{
    std::string u = ToUtf8(s);
    std::wstring r;
    for (unsigned char c : u)
    {
        if (isalnum(c) || c == '-' || c == '_' || c == '.' || c == '~')
            r += (wchar_t)c;
        else
        {
            wchar_t buf[4];
            swprintf_s(buf, L"%%%02X", c);
            r += buf;
        }
    }
    return r;
}

std::wstring ExeDir()
{
    wchar_t buf[MAX_PATH * 2];
    GetModuleFileNameW(nullptr, buf, ARRAYSIZE(buf));
    std::wstring p(buf);
    size_t i = p.find_last_of(L"\\/");
    return i == std::wstring::npos ? p : p.substr(0, i);
}

std::wstring WebView2DataFolder()
{
    wchar_t buf[MAX_PATH] = {};
    SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, SHGFP_TYPE_CURRENT, buf);
    std::wstring dir = PathCombine2(PathCombine2(buf, L"FRTP"), L"WebView2");
    SHCreateDirectoryExW(nullptr, dir.c_str(), nullptr);
    return dir;
}

std::wstring PathCombine2(const std::wstring& a, const std::wstring& b)
{
    if (a.empty()) return b;
    if (a.back() == L'\\' || a.back() == L'/') return a + b;
    return a + L"\\" + b;
}

bool FileExists(const std::wstring& path)
{
    DWORD a = GetFileAttributesW(path.c_str());
    return a != INVALID_FILE_ATTRIBUTES && !(a & FILE_ATTRIBUTE_DIRECTORY);
}

std::wstring ReadTextFile(const std::wstring& path)
{
    HANDLE h = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, 0, nullptr);
    if (h == INVALID_HANDLE_VALUE) return std::wstring();
    DWORD sz = GetFileSize(h, nullptr), rd = 0;
    std::string buf(sz, '\0');
    ReadFile(h, &buf[0], sz, &rd, nullptr);
    CloseHandle(h);
    buf.resize(rd);
    if (buf.size() >= 3 && (unsigned char)buf[0] == 0xEF && (unsigned char)buf[1] == 0xBB && (unsigned char)buf[2] == 0xBF)
        buf.erase(0, 3);
    return FromUtf8(buf);
}

bool WriteTextFile(const std::wstring& path, const std::wstring& text)
{
    HANDLE h = CreateFileW(path.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return false;
    std::string u = ToUtf8(text);
    DWORD wr = 0;
    BOOL ok = WriteFile(h, u.data(), (DWORD)u.size(), &wr, nullptr);
    CloseHandle(h);
    return ok != FALSE;
}

static bool ResolveV4(const std::wstring& host, sockaddr_in& out)
{
    ADDRINFOW hints = {};
    hints.ai_family = AF_INET;
    hints.ai_socktype = SOCK_STREAM;
    ADDRINFOW* res = nullptr;
    if (GetAddrInfoW(host.c_str(), nullptr, &hints, &res) != 0 || !res)
        return false;
    out = *reinterpret_cast<sockaddr_in*>(res->ai_addr);
    FreeAddrInfoW(res);
    return true;
}

PingResult PingHost(const std::wstring& host, DWORD timeoutMs)
{
    sockaddr_in sa = {};
    if (host.empty() || !ResolveV4(host, sa))
        return PingResult::InvalidAddress;

    HANDLE h = IcmpCreateFile();
    if (h == INVALID_HANDLE_VALUE)
        return PingResult::Failed;

    char payload[32] = "FRTP ping";
    std::vector<char> reply(sizeof(ICMP_ECHO_REPLY) + sizeof(payload) + 8);
    DWORD n = IcmpSendEcho(h, sa.sin_addr.S_un.S_addr, payload, (WORD)sizeof(payload), nullptr,
                           reply.data(), (DWORD)reply.size(), timeoutMs);
    bool ok = false;
    if (n > 0)
        ok = reinterpret_cast<ICMP_ECHO_REPLY*>(reply.data())->Status == IP_SUCCESS;
    IcmpCloseHandle(h);
    return ok ? PingResult::Ok : PingResult::Failed;
}

SOCKET ConnectTcp(const std::wstring& host, int port, DWORD timeoutMs)
{
    sockaddr_in sa = {};
    if (!ResolveV4(host, sa))
        return INVALID_SOCKET;
    sa.sin_port = htons((u_short)port);

    SOCKET s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (s == INVALID_SOCKET)
        return s;

    u_long nb = 1;
    ioctlsocket(s, FIONBIO, &nb);
    int r = connect(s, (sockaddr*)&sa, sizeof(sa));
    if (r == SOCKET_ERROR && WSAGetLastError() == WSAEWOULDBLOCK)
    {
        fd_set wf, ef;
        FD_ZERO(&wf); FD_ZERO(&ef);
        FD_SET(s, &wf); FD_SET(s, &ef);
        timeval tv = { (long)(timeoutMs / 1000), (long)((timeoutMs % 1000) * 1000) };
        r = select(0, nullptr, &wf, &ef, &tv);
        if (r <= 0 || FD_ISSET(s, &ef) || !FD_ISSET(s, &wf))
        {
            closesocket(s);
            return INVALID_SOCKET;
        }
    }
    else if (r == SOCKET_ERROR)
    {
        closesocket(s);
        return INVALID_SOCKET;
    }
    nb = 0;
    ioctlsocket(s, FIONBIO, &nb);
    DWORD to = 15000;
    setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, (const char*)&to, sizeof(to));
    setsockopt(s, SOL_SOCKET, SO_SNDTIMEO, (const char*)&to, sizeof(to));
    return s;
}

std::string XmlDecode(const std::string& s)
{
    std::string r;
    for (size_t i = 0; i < s.size(); ++i)
    {
        if (s[i] == '&')
        {
            size_t e = s.find(';', i);
            if (e != std::string::npos && e - i <= 8)
            {
                std::string ent = s.substr(i + 1, e - i - 1);
                if (ent == "lt") { r += '<'; i = e; continue; }
                if (ent == "gt") { r += '>'; i = e; continue; }
                if (ent == "amp") { r += '&'; i = e; continue; }
                if (ent == "quot") { r += '"'; i = e; continue; }
                if (ent == "apos") { r += '\''; i = e; continue; }
                if (!ent.empty() && ent[0] == '#')
                {
                    int code = ent.size() > 1 && (ent[1] == 'x' || ent[1] == 'X') ? (int)strtol(ent.c_str() + 2, nullptr, 16)
                                                                                    : (int)strtol(ent.c_str() + 1, nullptr, 10);
                    std::wstring w(1, (wchar_t)code);
                    r += ToUtf8(w);
                    i = e;
                    continue;
                }
            }
        }
        r += s[i];
    }
    return r;
}

bool XmlParse(const std::string& xml, const std::string& rootName)
{
    if (xml.empty()) return false;
    size_t a = xml.find("<" + rootName);
    if (a == std::string::npos) return false;
    return xml.find("</" + rootName + ">", a) != std::string::npos || xml.find("/>", a) != std::string::npos;
}

bool XmlChild(const std::string& xml, const std::string& tag, std::string& value)
{
    std::string open = "<" + tag;
    size_t p = 0;
    while ((p = xml.find(open, p)) != std::string::npos)
    {
        size_t after = p + open.size();
        if (after < xml.size() && (xml[after] == '>' || xml[after] == ' ' || xml[after] == '/'))
            break;
        p = after;
    }
    if (p == std::string::npos) return false;
    size_t gt = xml.find('>', p);
    if (gt == std::string::npos) return false;
    if (xml[gt - 1] == '/') { value.clear(); return true; }
    size_t end = xml.find("</" + tag + ">", gt);
    if (end == std::string::npos) return false;
    value = XmlDecode(xml.substr(gt + 1, end - gt - 1));
    return true;
}

void PostToUi(HWND hwnd, std::function<void()> fn)
{
    auto* p = new std::function<void()>(std::move(fn));
    if (!::PostMessageW(hwnd, WM_APP_POST, 0, (LPARAM)p))
        delete p;
}

void RunPostedUi(LPARAM lp)
{
    std::unique_ptr<std::function<void()>> p(reinterpret_cast<std::function<void()>*>(lp));
    if (p && *p) (*p)();
}
