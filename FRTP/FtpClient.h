#pragma once
#include <string>
#include <vector>
#include <winsock2.h>

enum class FtpFileType { File, Directory, Unknown };

struct FtpFile
{
    std::wstring name;
    long long size = 0;
    FtpFileType type = FtpFileType::Unknown;
    std::wstring raw;

    explicit FtpFile(const std::wstring& details);
};

class FtpClient
{
public:
    FtpClient(const std::wstring& host, const std::wstring& user, const std::wstring& pass, int port = 21);
    ~FtpClient();
    FtpClient(const FtpClient&) = delete;
    FtpClient& operator=(const FtpClient&) = delete;

    void SetServerPath(const std::wstring& path) { m_serverPath = NormalizePath(path); }
    const std::wstring& ServerPath() const { return m_serverPath; }
    bool IsConnected() const { return m_connected; }

    void Connect();
    void Disconnect();

    std::vector<FtpFile> ListDirectory();
    std::vector<FtpFile> ListDirectory(const std::wstring& path);
    void Upload(const std::wstring& localPath, const std::wstring& remotePath);
    void UploadBytes(const std::string& data, const std::wstring& remotePath);
    void Download(const std::wstring& remotePath, const std::wstring& localPath);
    void DeleteFile(const std::wstring& name);

private:
    struct Response { int code = 0; std::string message, text; };

    SOCKET OpenPassive();
    Response SendCommand(const std::string& cmd);
    Response ReadResponse();
    bool ReadLine(std::string& line);
    std::wstring BuildRemotePath(const std::wstring& path) const;
    static std::wstring NormalizePath(const std::wstring& path);
    void EnsureConnected() const;
    static void EnsureSuccess(const Response& r);
    static void EnsurePreliminary(const Response& r);
    static std::wstring Msg(const std::string& s);

    std::wstring m_host, m_user, m_pass, m_serverPath;
    int m_port;
    bool m_connected = false;
    SOCKET m_ctrl = INVALID_SOCKET;
    std::string m_rx;
};
