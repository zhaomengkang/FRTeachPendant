#include "pch.h"
#include "FRTP.h"
#include "FRTPDlg.h"
#include "HelpDlg.h"
#include "Util.h"
#include <atlhost.h>

#pragma comment(linker, "\"/manifestdependency:type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")

BEGIN_MESSAGE_MAP(CFRTPApp, CWinApp)
END_MESSAGE_MAP()

CFRTPApp theApp;

namespace
{
    struct CommandLineOptions
    {
        bool showHelp = false;
        bool invalid = false;
        bool hasCredentials = false;
        std::wstring ip;
        std::wstring name;
        std::wstring pwd;
    };

    bool ParseStartupArguments(CommandLineOptions& options)
    {
        int argc = 0;
        LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
        if (!argv)
            return false;

        for (int i = 1; i < argc; ++i)
        {
            if (_wcsicmp(argv[i], L"-h") == 0 || _wcsicmp(argv[i], L"--help") == 0)
            {
                options.showHelp = true;
                break;
            }
        }

        if (!options.showHelp && argc > 1)
        {
            if (argc == 4)
            {
                options.ip = argv[1];
                options.name = argv[2];
                options.pwd = argv[3];
                options.hasCredentials = true;
            }
            else
            {
                options.invalid = true;
            }
        }

        LocalFree(argv);
        return true;
    }

    void ShowCommandLineHelp(bool invalid)
    {
        std::wstring message;
        if (invalid)
            message = L"Invalid command-line arguments.\r\n\r\n";

        message +=
            L"Usage:\r\n"
            L"  FRTP.exe <IP> <Name> <Pwd>\r\n"
            L"  FRTP.exe -h\r\n\r\n"
            L"Arguments:\r\n"
            L"  IP       Robot controller IP address\r\n"
            L"  Name     Controller username\r\n"
            L"  Pwd      Controller password\r\n\r\n"
            L"Providing all three arguments starts FRTP and connects automatically.\r\n"
            L"Run without arguments to open FRTP without connecting.\r\n"
            L"Use -h or --help to display this help.\r\n\r\n"
            L"Security: command-line passwords may be visible in process listings and shell history.";

        MessageBoxW(nullptr, message.c_str(), L"FRTP Command-Line Help", MB_OK | (invalid ? MB_ICONWARNING : MB_ICONINFORMATION));
    }

    bool IsOcxRegistered()
    {
        HKEY k;
        if (RegOpenKeyExW(HKEY_CLASSES_ROOT, L"TypeLib\\{34F4C4DB-A64B-4D87-99DA-042F7FB7DEBA}", 0, KEY_READ, &k) == ERROR_SUCCESS)
        {
            RegCloseKey(k);
            return true;
        }
        return false;
    }

    bool IsAdmin()
    {
        BOOL admin = FALSE;
        PSID grp = nullptr;
        SID_IDENTIFIER_AUTHORITY nt = SECURITY_NT_AUTHORITY;
        if (AllocateAndInitializeSid(&nt, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &grp))
        {
            CheckTokenMembership(nullptr, grp, &admin);
            FreeSid(grp);
        }
        return admin != FALSE;
    }

    std::wstring FindOcx()
    {
        std::wstring dir = ExeDir();
        for (const std::wstring& p : { PathCombine2(dir, L"fripendant.ocx"),
                                        PathCombine2(dir, L"..\\Support\\UIF\\fripendant.ocx") })
            if (FileExists(p)) return p;
        return L"";
    }

    bool RegisterOcx()
    {
        std::wstring ocx = FindOcx();
        if (ocx.empty()) return false;
        std::wstring args = L"/s \"" + ocx + L"\"";
        SHELLEXECUTEINFOW sei = { sizeof(sei) };
        sei.fMask = SEE_MASK_NOCLOSEPROCESS;
        sei.lpFile = L"regsvr32.exe";
        sei.lpParameters = args.c_str();
        sei.nShow = SW_HIDE;
        if (!ShellExecuteExW(&sei) || !sei.hProcess) return false;
        WaitForSingleObject(sei.hProcess, 30000);
        DWORD code = 1;
        GetExitCodeProcess(sei.hProcess, &code);
        CloseHandle(sei.hProcess);
        return code == 0;
    }

    // Returns false when the app should exit.
    bool EnsureOcx()
    {
        if (IsOcxRegistered()) return true;
        if (MessageBoxW(nullptr, L"The OCX file is not registered. Would you like to register it now?", L"Notice",
                        MB_YESNO | MB_ICONQUESTION) != IDYES)
        {
            MessageBoxW(nullptr, L"If the OCX file is not registered, the teach pendant screen may not display properly. Only backup/upload functions will be available.",
                        L"Notice", MB_OK | MB_ICONWARNING);
            return true;
        }
        if (!IsAdmin())
        {
            wchar_t path[MAX_PATH];
            GetModuleFileNameW(nullptr, path, MAX_PATH);
            HINSTANCE h = ShellExecuteW(nullptr, L"runas", path, nullptr, nullptr, SW_SHOWNORMAL);
            if ((INT_PTR)h <= 32)
                MessageBoxW(nullptr, L"Unable to run the program as administrator. Please right-click the program and select 'Run as administrator'.",
                            L"Error", MB_OK | MB_ICONERROR);
            return false;
        }
        if (!RegisterOcx())
        {
            MessageBoxW(nullptr, L"Failed to register the OCX file. Please install the Microsoft Visual C++ 2008 Redistributable (x86) and try again.",
                        L"Error", MB_OK | MB_ICONERROR);
            return false;
        }
        MessageBoxW(nullptr, L"The OCX file was registered successfully. Please run the application again.", L"Success",
                    MB_OK | MB_ICONINFORMATION);
        return false;
    }
}

BOOL CFRTPApp::InitInstance()
{
    INITCOMMONCONTROLSEX icc = { sizeof(icc), ICC_WIN95_CLASSES };
    InitCommonControlsEx(&icc);
    CWinApp::InitInstance();

    CommandLineOptions options;
    if (!ParseStartupArguments(options))
    {
        MessageBoxW(nullptr, L"Unable to parse the command line.", L"FRTP", MB_OK | MB_ICONERROR);
        return FALSE;
    }
    if (options.showHelp || options.invalid)
    {
        ShowCommandLineHelp(options.invalid);
        return FALSE;
    }

    if (!AfxOleInit())
    {
        AfxMessageBox(IDP_OLE_INIT_FAILED);
        return FALSE;
    }
    AfxEnableControlContainer();
    if (!AtlAxWinInit())
    {
        AfxMessageBox(L"ATL ActiveX host initialization failed.", MB_OK | MB_ICONERROR);
        return FALSE;
    }

    WSADATA wsa;
    WSAStartup(MAKEWORD(2, 2), &wsa);

    if (!EnsureOcx()) return FALSE;

    CFRTPDlg dlg;
    if (options.hasCredentials)
        dlg.SetStartupCredentials(options.ip, options.name, options.pwd);
    m_pMainWnd = &dlg;
    dlg.DoModal();
    return FALSE;
}

int CFRTPApp::ExitInstance()
{
    WSACleanup();
    return CWinApp::ExitInstance();
}
