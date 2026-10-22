#pragma once
#include <string>

namespace RobotChecker
{
    // Returns "R-30iA", "R-30iB", "R-30iB Plus", "R-50iA" or "Unknown".
    std::wstring GetRobotVersion(const std::wstring& ip, int port = 21, DWORD timeoutMs = 5000);
}
