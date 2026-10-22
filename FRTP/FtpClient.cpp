#include "pch.h"
#include "Util.h"
#include "FtpClient.h"

namespace
{
    std::vector<std::wstring> SplitMax(const std::wstring& s, size_t maxParts)
    {
        std::vector<std::wstring> parts;
        size_t i = 0;
        while (i < s.size())
        {
            while (i < s.size() && s[i] == L' ') ++i;
            if (i >= s.size()) break;
            if (parts.size() + 1 == maxParts)
            {
                parts.push_back(s.substr(i));
                break;
            }
            size_t e = s.find(L' ', i);
            if (e == std::wstring::npos) e = s.size();
            parts.push_back(s.substr(i, e - i));
            i = e;
        }
        return parts;
    }

    bool TryParseLL(const std::wstring& s, long long& v)
    {
        if (s.empty()) return false;
        wchar_t* end = nullptr;
        v = wcstoll(s.c_str(), &end, 10);
        return end && *end == L'\0';
    }

    void SendAll(SOCKET s, const char* data, size_t len)
    {
        while (len > 0)
        {
            int n = send(s, data, (int)(len > 65536 ? 65536 : len), 0);
            if (n <= 0) throw AppError(L"FTP data connection error.");
            data += n;
            len -= n;
        }
    }
}

FtpFile::FtpFile(const std::wstring& details)
{
    raw = details;
    name = details;
    if (Trim(details).empty()) return;

    auto unixParts = SplitMax(details, 9);
    if (unixParts.size() >= 9)
    {
        name = unixParts[8];
        long long sz;
        if (TryParseLL(unixParts[4], sz)) size = sz;
        wchar_t t = unixParts[0][0];
        type = t == L'd' ? FtpFileType::Directory : (t == L'-' ? FtpFileType::File : FtpFileType::Unknown);
        return;
    }

    auto winParts = SplitMax(details, (size_t)-1);
    if (winParts.size() >= 4)
    {
        name = winParts.back();
        if (ToUpper(details).find(L"<DIR>") != std::wstring::npos)
        {
            type = FtpFileType::Directory;
        }
        else
        {
            type = FtpFileType::File;
            for (int i = (int)winParts.size() - 2; i >= 0; --i)
            {
                long long sz;
                if (TryParseLL(winParts[i], sz)) { size = sz; break; }
            }
        }
    }
}

FtpClient::FtpClient(const std::wstring& host, const std::wstring& user, const std::wstring& pass, int port)
    : m_user(user), m_pass(pass), m_serverPath(L"/"), m_port(port)
{
    std::wstring h = Trim(host);
    if (h.empty())
        throw AppError(L"Hostname cannot be empty.");
    if (_wcsnicmp(h.c_str(), L"ftp://", 6) == 0) h = h.substr(6);
    while (!h.empty() && h.front() == L'/') h.erase(0, 1);
    while (!h.empty() && h.back() == L'/') h.pop_back();
    m_host = Trim(h);
}

FtpClient::~FtpClient() { Disconnect(); }

std::wstring FtpClient::Msg(const std::string& s) { return FromUtf8(s); }

std::wstring FtpClient::NormalizePath(const std::wstring& path)
{
    if (Trim(path).empty()) return L"/";
    std::wstring p = path;
    std::replace(p.begin(), p.end(), L'\\', L'/');
    p = Trim(p);
    if (p.empty() || p[0] != L'/') p = L"/" + p;
    size_t pos;
    while ((pos = p.find(L"//")) != std::wstring::npos) p.erase(pos, 1);
    if (p.size() > 1)
        while (p.size() > 1 && p.back() == L'/') p.pop_back();
    return p;
}

std::wstring FtpClient::BuildRemotePath(const std::wstring& path) const
{
    if (Trim(path).empty()) return m_serverPath;
    std::wstring p = path;
    std::replace(p.begin(), p.end(), L'\\', L'/');
    p = Trim(p);
    if (!p.empty() && p[0] == L'/') return p;
    if (m_serverPath == L"/") return L"/" + p;
    std::wstring base = m_serverPath;
    while (!base.empty() && base.back() == L'/') base.pop_back();
    return base + L"/" + p;
}

void FtpClient::EnsureConnected() const
{
    if (!m_connected) throw AppError(L"Not connected to the FTP server.");
}

void FtpClient::EnsureSuccess(const Response& r)
{
    if (r.code < 200 || r.code >= 300) throw AppError(L"FTP error: " + Msg(r.message));
}

void FtpClient::EnsurePreliminary(const Response& r)
{
    if (r.code != 150 && r.code != 125) throw AppError(L"FTP error: " + Msg(r.message));
}

bool FtpClient::ReadLine(std::string& line)
{
    for (;;)
    {
        size_t nl = m_rx.find('\n');
        if (nl != std::string::npos)
        {
            line = m_rx.substr(0, nl);
            if (!line.empty() && line.back() == '\r') line.pop_back();
            m_rx.erase(0, nl + 1);
            return true;
        }
        char buf[2048];
        int n = recv(m_ctrl, buf, sizeof(buf), 0);
        if (n <= 0) return false;
        m_rx.append(buf, n);
    }
}

FtpClient::Response FtpClient::ReadResponse()
{
    if (m_ctrl == INVALID_SOCKET) throw AppError(L"The FTP control connection is unavailable.");
    std::string line;
    if (!ReadLine(line)) throw AppError(L"The FTP server closed the connection.");

    std::string all = line;
    int code = 0;
    if (line.size() < 3 || !isdigit((unsigned char)line[0]) || !isdigit((unsigned char)line[1]) || !isdigit((unsigned char)line[2]))
        throw AppError(L"Invalid FTP response: " + Msg(line));
    code = atoi(line.substr(0, 3).c_str());

    if (line.size() > 3 && line[3] == '-')
    {
        std::string endMark = line.substr(0, 3) + " ";
        for (;;)
        {
            std::string next;
            if (!ReadLine(next)) throw AppError(L"The FTP server closed the connection.");
            all += "\r\n" + next;
            if (next.compare(0, endMark.size(), endMark) == 0) { line = next; break; }
        }
    }
    Response r;
    r.code = code;
    r.message = all;
    r.text = line.size() > 4 ? line.substr(4) : std::string();
    return r;
}

FtpClient::Response FtpClient::SendCommand(const std::string& cmd)
{
    if (m_ctrl == INVALID_SOCKET) throw AppError(L"The FTP control connection is unavailable.");
    std::string c = cmd + "\r\n";
    SendAll(m_ctrl, c.data(), c.size());
    return ReadResponse();
}

void FtpClient::Connect()
{
    if (m_connected) return;
    try
    {
        m_ctrl = ConnectTcp(m_host, m_port, 5000);
        if (m_ctrl == INVALID_SOCKET)
            throw AppError(L"Unable to connect to the FTP server.");
        m_rx.clear();

        Response r = ReadResponse();
        if (r.code != 220) throw AppError(L"FTP error: " + Msg(r.message));

        r = SendCommand("USER " + ToUtf8(m_user));
        if (r.code == 331)
        {
            r = SendCommand("PASS " + ToUtf8(m_pass));
            if (r.code != 230) throw AppError(L"FTP error: " + Msg(r.message));
        }
        else if (r.code != 230)
            throw AppError(L"FTP login failed: " + Msg(r.message));

        EnsureSuccess(SendCommand("TYPE I"));
        m_connected = true;
    }
    catch (...)
    {
        Disconnect();
        throw;
    }
}

void FtpClient::Disconnect()
{
    if (m_ctrl != INVALID_SOCKET)
    {
        try { SendCommand("QUIT"); } catch (...) {}
        closesocket(m_ctrl);
        m_ctrl = INVALID_SOCKET;
    }
    m_connected = false;
    m_rx.clear();
}

SOCKET FtpClient::OpenPassive()
{
    Response r = SendCommand("PASV");
    if (r.code != 227) throw AppError(L"Failed to enter passive mode: " + Msg(r.message));
    size_t a = r.text.find('('), b = r.text.find(')');
    if (a == std::string::npos || b == std::string::npos || b <= a)
        throw AppError(L"Invalid PASV response format: " + Msg(r.message));
    int n[6];
    if (sscanf_s(r.text.substr(a + 1, b - a - 1).c_str(), "%d,%d,%d,%d,%d,%d", &n[0], &n[1], &n[2], &n[3], &n[4], &n[5]) != 6)
        throw AppError(L"Invalid PASV response format: " + Msg(r.message));
    std::wstring host = std::to_wstring(n[0]) + L"." + std::to_wstring(n[1]) + L"." + std::to_wstring(n[2]) + L"." + std::to_wstring(n[3]);
    SOCKET s = ConnectTcp(host, n[4] * 256 + n[5], 5000);
    if (s == INVALID_SOCKET)
        throw AppError(L"Unable to open the FTP data connection.");
    return s;
}

namespace
{
    struct SockGuard
    {
        SOCKET s;
        explicit SockGuard(SOCKET x) : s(x) {}
        ~SockGuard() { if (s != INVALID_SOCKET) closesocket(s); }
        void Close() { if (s != INVALID_SOCKET) { closesocket(s); s = INVALID_SOCKET; } }
    };
}

std::vector<FtpFile> FtpClient::ListDirectory() { return ListDirectory(m_serverPath); }

std::vector<FtpFile> FtpClient::ListDirectory(const std::wstring& path)
{
    EnsureConnected();
    std::wstring p = NormalizePath(path);
    SockGuard data(OpenPassive());
    EnsurePreliminary(SendCommand("LIST " + ToUtf8(p)));

    std::string all;
    char buf[8192];
    int n;
    while ((n = recv(data.s, buf, sizeof(buf), 0)) > 0)
        all.append(buf, n);
    data.Close();
    EnsureSuccess(ReadResponse());

    std::vector<FtpFile> files;
    size_t i = 0;
    while (i < all.size())
    {
        size_t e = all.find('\n', i);
        std::string line = all.substr(i, e == std::string::npos ? std::string::npos : e - i);
        i = e == std::string::npos ? all.size() : e + 1;
        if (!line.empty() && line.back() == '\r') line.pop_back();
        std::wstring w = FromUtf8(line);
        if (!Trim(w).empty())
            files.emplace_back(w);
    }
    return files;
}

void FtpClient::Upload(const std::wstring& localPath, const std::wstring& remotePath)
{
    EnsureConnected();
    HANDLE f = CreateFileW(localPath.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, 0, nullptr);
    if (f == INVALID_HANDLE_VALUE)
        throw AppError(L"Local file does not exist: " + localPath);
    std::string content;
    DWORD sz = GetFileSize(f, nullptr), rd = 0;
    content.resize(sz);
    BOOL ok = sz == 0 ? TRUE : ReadFile(f, &content[0], sz, &rd, nullptr);
    CloseHandle(f);
    if (!ok) throw AppError(L"Unable to read local file: " + localPath);
    UploadBytes(content, remotePath);
}

void FtpClient::UploadBytes(const std::string& data, const std::wstring& remotePath)
{
    EnsureConnected();
    std::wstring rp = BuildRemotePath(remotePath);
    SockGuard ds(OpenPassive());
    EnsurePreliminary(SendCommand("STOR " + ToUtf8(rp)));
    SendAll(ds.s, data.data(), data.size());
    ds.Close();
    EnsureSuccess(ReadResponse());
}

void FtpClient::Download(const std::wstring& remotePath, const std::wstring& localPath)
{
    EnsureConnected();
    std::wstring rp = BuildRemotePath(remotePath);

    size_t slash = localPath.find_last_of(L"\\/");
    if (slash != std::wstring::npos)
    {
        std::wstring dir = localPath.substr(0, slash);
        if (!dir.empty() && GetFileAttributesW(dir.c_str()) == INVALID_FILE_ATTRIBUTES)
            SHCreateDirectoryExW(nullptr, dir.c_str(), nullptr);
    }

    SockGuard ds(OpenPassive());
    EnsurePreliminary(SendCommand("RETR " + ToUtf8(rp)));

    HANDLE f = CreateFileW(localPath.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS,
                           FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, nullptr);
    if (f == INVALID_HANDLE_VALUE)
        throw AppError(L"Unable to create local file: " + localPath);
    std::vector<char> buf(256 * 1024);
    int n;
    bool wok = true;
    while ((n = recv(ds.s, buf.data(), static_cast<int>(buf.size()), 0)) > 0)
    {
        DWORD wr = 0;
        if (!WriteFile(f, buf.data(), n, &wr, nullptr) || wr != (DWORD)n) { wok = false; break; }
    }
    CloseHandle(f);
    ds.Close();
    if (!wok) throw AppError(L"Unable to write local file: " + localPath);
    EnsureSuccess(ReadResponse());
}

void FtpClient::DeleteFile(const std::wstring& name)
{
    EnsureConnected();
    EnsureSuccess(SendCommand("DELE " + ToUtf8(BuildRemotePath(name))));
}
