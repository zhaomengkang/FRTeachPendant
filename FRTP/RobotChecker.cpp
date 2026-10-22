#include "pch.h"
#include <regex>
#include "Util.h"
#include "RobotChecker.h"

namespace
{
    bool ReadFirstLine(SOCKET s, DWORD timeoutMs, std::string& line)
    {
        DWORD start = GetTickCount();
        std::string sb;
        while (GetTickCount() - start < timeoutMs)
        {
            fd_set rf;
            FD_ZERO(&rf);
            FD_SET(s, &rf);
            timeval tv = { 0, 50 * 1000 };
            int r = select(0, &rf, nullptr, nullptr, &tv);
            if (r > 0)
            {
                char buf[1024];
                int n = recv(s, buf, sizeof(buf), 0);
                if (n <= 0) break;
                sb.append(buf, n);
                size_t idx = sb.find("\r\n");
                if (idx != std::string::npos)
                {
                    line = sb.substr(0, idx);
                    return true;
                }
            }
            else if (r < 0)
                break;
        }
        line = sb;
        return !sb.empty();
    }

    std::wstring ParseVersion(const std::string& welcome)
    {
        std::smatch m;
        if (!std::regex_search(welcome, m, std::regex("V\\d+(\\.\\d+)?[A-Z]?/\\d+")))
            return L"Unknown";
        std::string v = m.str();
        std::smatch mm;
        if (!std::regex_search(v, mm, std::regex("V(\\d+)")))
            return L"Unknown";
        std::string major = mm[1].str();
        if (major == "7") return L"R-30iA";
        if (major == "8") return L"R-30iB";
        if (major == "9") return L"R-30iB Plus";
        if (major == "10") return L"R-50iA";
        return L"Unknown";
    }
}

namespace RobotChecker
{
    std::wstring GetRobotVersion(const std::wstring& ip, int port, DWORD timeoutMs)
    {
        SOCKET s = ConnectTcp(ip, port, timeoutMs);
        if (s == INVALID_SOCKET)
            return L"Unknown";

        std::wstring result = L"Unknown";
        std::string welcome;
        if (ReadFirstLine(s, timeoutMs, welcome) && welcome.compare(0, 4, "220 ") == 0)
        {
            result = ParseVersion(welcome);
            if (result != L"Unknown")
            {
                send(s, "QUIT\r\n", 6, 0);
                std::string dummy;
                ReadFirstLine(s, 1000, dummy);
            }
        }
        closesocket(s);
        return result;
    }
}
