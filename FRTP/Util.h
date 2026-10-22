#pragma once

#include <string>
#include <vector>
#include <functional>
#include <winsock2.h>

// Error type thrown by the FTP / loader code; carries a user-visible message.
struct AppError
{
    std::wstring msg;
    explicit AppError(std::wstring m) : msg(std::move(m)) {}
};

std::string ToUtf8(const std::wstring& s);
std::wstring FromUtf8(const std::string& s);
std::wstring Trim(const std::wstring& s);
std::wstring ToUpper(std::wstring s);
std::wstring ToLower(std::wstring s);
std::wstring UrlEncode(const std::wstring& s);
std::wstring ExeDir();
std::wstring WebView2DataFolder();
std::wstring PathCombine2(const std::wstring& a, const std::wstring& b);
bool FileExists(const std::wstring& path);
std::wstring ReadTextFile(const std::wstring& path);
bool WriteTextFile(const std::wstring& path, const std::wstring& text);

// Result of IcmpSendEcho style ping.
enum class PingResult { Ok, Failed, InvalidAddress };
PingResult PingHost(const std::wstring& host, DWORD timeoutMs);

// Resolves an IPv4 host and connects with a timeout. Returns INVALID_SOCKET on failure.
SOCKET ConnectTcp(const std::wstring& host, int port, DWORD timeoutMs);

// Minimal XML helpers (flat documents as sent by the KAREL server).
bool XmlParse(const std::string& xml, const std::string& rootName);
bool XmlChild(const std::string& xml, const std::string& tag, std::string& value);
std::string XmlDecode(const std::string& s);

// Marshals a callable to the UI thread of the main dialog.
#define WM_APP_POST (WM_APP + 1)
void PostToUi(HWND hwnd, std::function<void()> fn);
void RunPostedUi(LPARAM lp);
