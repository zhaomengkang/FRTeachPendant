#pragma once
#include <string>

namespace MkWebClient
{
    constexpr int IO_OperatorPanelOutput = 12;

    struct VarResponse
    {
        std::wstring program, var, value;
        int status = 0;
    };

    struct IoValueResponse
    {
        int ioType = 0, ioIndex = 0, value = 0, status = 0;
    };

    VarResponse WriteVar(const std::wstring& ip, const std::wstring& prog, const std::wstring& var,
                         const std::wstring& type, const std::wstring& value);
    VarResponse ReadVar(const std::wstring& ip, const std::wstring& prog, const std::wstring& var,
                        const std::wstring& type);
    std::wstring GetLibVersion(const std::wstring& ip, DWORD timeoutMs = 5000);
    // 0 = authorized, 1 = 401, 2 = 402, 3 = 403, -1 = other / error
    int CheckAuthStatus(const std::wstring& ip);
    int SimTpKey(const std::wstring& ip, const std::wstring& keyCodes);
    IoValueResponse GetIoValue(const std::wstring& ip, int ioType, int ioIndex);
}
