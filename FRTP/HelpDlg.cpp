#include "pch.h"
#include "Resource.h"
#include "Util.h"
#include "HelpDlg.h"

CHelpDlg* CHelpDlg::s_instance = nullptr;

BEGIN_MESSAGE_MAP(CHelpDlg, CDialog)
    ON_WM_CLOSE()
END_MESSAGE_MAP()

CHelpDlg::CHelpDlg(CWnd* parent) : CDialog(IDD_HELP_DIALOG, parent) {}

void CHelpDlg::ShowHelp(CWnd* parent)
{
    if (s_instance && ::IsWindow(s_instance->GetSafeHwnd()))
    {
        s_instance->ShowWindow(SW_RESTORE);
        s_instance->SetForegroundWindow();
        return;
    }
    s_instance = new CHelpDlg(parent);
    if (!s_instance->Create(IDD_HELP_DIALOG, parent))
    {
        delete s_instance;
        s_instance = nullptr;
        return;
    }
    s_instance->ShowWindow(SW_SHOW);
}

BOOL CHelpDlg::OnInitDialog()
{
    CDialog::OnInitDialog();

    CRect rc;
    GetDlgItem(IDC_WEBVIEW_HELP)->GetWindowRect(rc);
    ScreenToClient(rc);

    std::wstring folder = WebView2DataFolder();
    m_web.Init(GetSafeHwnd(), rc, folder, 0.7, [this](bool ok)
    {
        if (!ok)
        {
            ::MessageBoxW(GetSafeHwnd(), L"WebView2 runtime is not available.", L"WebView2 Init failed", MB_OK | MB_ICONERROR);
            return;
        }
        std::wstring html = PathCombine2(PathCombine2(PathCombine2(ExeDir(), L"res"), L"Helper"), L"help.html");
        if (FileExists(html))
        {
            std::wstring url = L"file:///" + html;
            std::replace(url.begin(), url.end(), L'\\', L'/');
            m_web.Navigate(url);
        }
    });
    return TRUE;
}

void CHelpDlg::OnClose() { DestroyWindow(); }

void CHelpDlg::PostNcDestroy()
{
    m_web.Close();
    s_instance = nullptr;
    delete this;
}
