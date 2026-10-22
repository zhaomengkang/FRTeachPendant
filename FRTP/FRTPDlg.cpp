#include "pch.h"
#include "FRTP.h"
#include "FRTPDlg.h"
#include "HelpDlg.h"
#include "InputDlg.h"
#include "MkWebClient.h"
#include "RobotChecker.h"
#include "FtpClient.h"
#include "FRLoadUserPC.h"
#include <atlhost.h>
#include <atlcomcli.h>

ATL::CAtlMfcModule _AtlModule;

namespace
{
    constexpr wchar_t kWindowTitle[] = L"FRTP V26.10.22";
    const COLORREF kGreen = RGB(173, 255, 47);   // GreenYellow
    const COLORREF kBlue = RGB(0, 0, 205);       // MediumBlue
    const UINT_PTR kTimerId = 1;
    const UINT kTimerMs = 300;

    struct KeyEntry { UINT id; int normal; int shifted; };
    const KeyEntry kKeys[] = {
#include "KeyTable.inc"
    };
    struct KeyBmp { UINT id; UINT bmp; };
    const KeyBmp kKeyBmps[] = {
#include "KeyBitmaps.inc"
    };

    HBITMAP CreateFittedBitmap(HBITMAP source, int maxWidth, int maxHeight)
    {
        BITMAP sourceInfo = {};
        if (!source || maxWidth <= 0 || maxHeight <= 0 ||
            GetObjectW(source, sizeof(sourceInfo), &sourceInfo) != sizeof(sourceInfo) ||
            sourceInfo.bmWidth <= 0 || sourceInfo.bmHeight <= 0)
            return nullptr;

        double scale = min(
            static_cast<double>(maxWidth) / sourceInfo.bmWidth,
            static_cast<double>(maxHeight) / sourceInfo.bmHeight);
        int width = max(1, static_cast<int>(sourceInfo.bmWidth * scale));
        int height = max(1, static_cast<int>(sourceInfo.bmHeight * scale));

        HDC screen = ::GetDC(nullptr);
        if (!screen) return nullptr;
        HDC sourceDc = CreateCompatibleDC(screen);
        HDC targetDc = CreateCompatibleDC(screen);
        HBITMAP fitted = CreateCompatibleBitmap(screen, width, height);
        if (!sourceDc || !targetDc || !fitted)
        {
            if (fitted) DeleteObject(fitted);
            if (sourceDc) DeleteDC(sourceDc);
            if (targetDc) DeleteDC(targetDc);
            ::ReleaseDC(nullptr, screen);
            return nullptr;
        }

        HGDIOBJ oldSource = SelectObject(sourceDc, source);
        HGDIOBJ oldTarget = SelectObject(targetDc, fitted);
        SetStretchBltMode(targetDc, HALFTONE);
        SetBrushOrgEx(targetDc, 0, 0, nullptr);
        BOOL copied = StretchBlt(targetDc, 0, 0, width, height,
                                 sourceDc, 0, 0, sourceInfo.bmWidth, sourceInfo.bmHeight, SRCCOPY);
        SelectObject(sourceDc, oldSource);
        SelectObject(targetDc, oldTarget);
        DeleteDC(sourceDc);
        DeleteDC(targetDc);
        ::ReleaseDC(nullptr, screen);
        if (!copied)
        {
            DeleteObject(fitted);
            return nullptr;
        }
        return fitted;
    }

    int ControllerType(const std::wstring& v)
    {
        if (v == L"R-30iA") return 1;
        if (v == L"R-30iB") return 2;
        if (v == L"R-30iB Plus") return 3;
        if (v == L"R-50iA") return 4;
        return 0;
    }
}

BEGIN_MESSAGE_MAP(CFRTPDlg, CDialog)
    ON_WM_CLOSE()
    ON_WM_DESTROY()
    ON_WM_TIMER()
    ON_WM_DROPFILES()
    ON_WM_CTLCOLOR()
    ON_MESSAGE(WM_APP_POST, &CFRTPDlg::OnPostedUi)
    ON_BN_CLICKED(IDC_BT_CONNECT, &CFRTPDlg::OnBnClickedConnect)
    ON_BN_CLICKED(IDC_BT_HELP, &CFRTPDlg::OnBnClickedHelp)
    ON_BN_CLICKED(IDC_BT_TBKEYSHOW, &CFRTPDlg::OnBnClickedKeyShow)
    ON_BN_CLICKED(IDC_BT_TOPMOST, &CFRTPDlg::OnBnClickedTopMost)
    ON_BN_CLICKED(IDC_BT_SHIFTL, &CFRTPDlg::OnBnClickedShift)
    ON_BN_CLICKED(IDC_BT_KEYBOARD, &CFRTPDlg::OnBnClickedKeyboard)
    ON_BN_CLICKED(IDC_BT_BACKUP, &CFRTPDlg::OnBnClickedBackup)
    ON_BN_CLICKED(IDC_BT_DOWNLOAD, &CFRTPDlg::OnBnClickedDownload)
    ON_CONTROL_RANGE(BN_CLICKED, IDC_KEY_FIRST, IDC_KEY_LAST, &CFRTPDlg::OnPendantKey)
END_MESSAGE_MAP()

CFRTPDlg::CFRTPDlg(CWnd* pParent) : CDialog(IDD_FRTP_DIALOG, pParent)
{
    m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

CFRTPDlg::~CFRTPDlg() {}

void CFRTPDlg::SetStartupCredentials(const std::wstring& ip, const std::wstring& name, const std::wstring& pwd)
{
    m_ip = ip.c_str();
    m_user = name.c_str();
    m_pass = pwd.c_str();
    m_autoConnectOnStartup = true;
}

void CFRTPDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    DDX_Control(pDX, IDC_BT_CONNECT, m_btConnect);
    DDX_Control(pDX, IDC_BT_SHIFTL, m_btShift);
    DDX_Control(pDX, IDC_BT_TOPMOST, m_btTopMost);
    DDX_Control(pDX, IDC_BT_TBKEYSHOW, m_btKeyShow);
    DDX_Control(pDX, IDC_BT_UPLOADAREA, m_btUpload);
    DDX_Control(pDX, IDC_PB_BACKUP, m_pb);
    DDX_Control(pDX, IDC_PIC_TPSWITCH, m_picSwitch);
    DDX_Text(pDX, IDC_TB_ROBOTIP, m_ip);
    DDX_Text(pDX, IDC_TB_NAME, m_user);
    DDX_Text(pDX, IDC_TB_PASSWORD, m_pass);
    DDX_Check(pDX, IDC_CB_BFSELECT, m_exitUi);
    DDX_Check(pDX, IDC_CB_COMMENT, m_comment);
}

BOOL CFRTPDlg::OnInitDialog()
{
    if (!m_autoConnectOnStartup)
    {
        m_ip = L"127.0.0.1";
        m_user = L"anonymous";
        m_pass = L"anonymous@";
    }
    CDialog::OnInitDialog();
    SetWindowText(kWindowTitle);
    ScaleLayout();
    SetIcon(m_hIcon, TRUE);
    SetIcon(m_hIcon, FALSE);

    // allow drag & drop even when elevated
    ::ChangeWindowMessageFilterEx(m_hWnd, WM_DROPFILES, MSGFLT_ALLOW, nullptr);
    ::ChangeWindowMessageFilterEx(m_hWnd, 0x49, MSGFLT_ALLOW, nullptr);
    DragAcceptFiles(TRUE);

    m_btConnect.SetColors(RGB(255, 255, 255));
    m_btShift.SetColors(RGB(240, 240, 240));
    m_btTopMost.SetColors(RGB(240, 240, 240));
    m_btKeyShow.SetColors(RGB(240, 240, 240));
    m_btUpload.SetColors(RGB(240, 240, 240));
    m_btTopMost.SetGlyphFont(L"Segoe UI Symbol", 6);
    m_btKeyShow.SetGlyphFont(L"Segoe UI Symbol", 6);
    m_btTopMost.SetWindowText(L"\U0001F4CC");
    m_btKeyShow.SetWindowText(L"\u2328");

    for (const auto& kb : kKeyBmps)
    {
        HBITMAP source = (HBITMAP)::LoadImage(AfxGetInstanceHandle(), MAKEINTRESOURCE(kb.bmp), IMAGE_BITMAP, 0, 0, LR_CREATEDIBSECTION);
        CWnd* button = GetDlgItem(kb.id);
        if (!source || !button)
        {
            if (source) DeleteObject(source);
            Msg(L"Unable to load a teach-pendant key image.", L"FRTP", MB_ICONWARNING);
            continue;
        }
        CRect buttonBounds;
        button->GetClientRect(&buttonBounds);
        HBITMAP h = CreateFittedBitmap(source, buttonBounds.Width() - 4, buttonBounds.Height() - 4);
        DeleteObject(source);
        if (!h)
        {
            Msg(L"Unable to resize a teach-pendant key image.", L"FRTP", MB_ICONWARNING);
            continue;
        }
        auto bmp = std::make_unique<CBitmap>();
        bmp->Attach(h);
        ((CButton*)button)->SetBitmap(h);
        m_bitmaps.push_back(std::move(bmp));
    }
    {
        CRect switchBounds;
        m_picSwitch.GetClientRect(&switchBounds);
        HBITMAP sourceOff = (HBITMAP)::LoadImage(AfxGetInstanceHandle(), MAKEINTRESOURCE(IDB_TPENOFF), IMAGE_BITMAP, 0, 0, LR_CREATEDIBSECTION);
        HBITMAP sourceOn = (HBITMAP)::LoadImage(AfxGetInstanceHandle(), MAKEINTRESOURCE(IDB_TPENON), IMAGE_BITMAP, 0, 0, LR_CREATEDIBSECTION);
        HBITMAP off = CreateFittedBitmap(sourceOff, switchBounds.Width() - 2, switchBounds.Height() - 2);
        HBITMAP on = CreateFittedBitmap(sourceOn, switchBounds.Width() - 2, switchBounds.Height() - 2);
        if (sourceOff) DeleteObject(sourceOff);
        if (sourceOn) DeleteObject(sourceOn);
        if (off) m_bmpTpOff.Attach(off);
        if (on) m_bmpTpOn.Attach(on);
        if (!off || !on)
            Msg(L"Unable to resize the teach-pendant switch image.", L"FRTP", MB_ICONWARNING);
        SetTpSwitchImage(false);
    }

    m_pb.SetRange(0, 100);

    CollectKeyboardControls();
    CRect cr; GetClientRect(&cr);
    m_fullClientH = cr.Height();
    m_collapsedClientH = m_kbArea.top - 3;
    SetPanelEnabled(false);
    GetDlgItem(IDC_LB_HOSTNAME)->ShowWindow(SW_HIDE);

    m_tip.Create(this, TTS_ALWAYSTIP);
    m_tip.SetMaxTipWidth(300);
    struct { UINT id; const wchar_t* text; } tips[] = {
        { IDC_TB_ROBOTIP, L"Enter the IP address of the robot controller." },
        { IDC_BT_CONNECT, L"Connect to the robot controller" },
        { IDC_BT_TBKEYSHOW, L"Show/Hide the teach pendant keyboard" },
        { IDC_BT_TOPMOST, L"Keep the window always on top" },
        { IDC_BT_BACKUP, L"Back up robot files to a local folder" },
        { IDC_BT_KEYBOARD, L"Send a string to the robot controller" },
        { IDC_TB_NAME, L"Administrator username for password-protected functions (level: INSTALL)" },
        { IDC_TB_PASSWORD, L"Administrator password for password-protected functions" },
        { IDC_BT_HELP, L"Something went wrong? Click here for help" },
    };
    for (auto& t : tips) m_tip.AddTool(GetDlgItem(t.id), t.text);
    m_tip.Activate(TRUE);

    CRect echoArea;
    GetDlgItem(IDC_PN_CGTP)->GetWindowRect(&echoArea);
    ScreenToClient(&echoArea);
    echoArea.DeflateRect(2, 2);
    int echoWidth = min(640, echoArea.Width());
    int echoHeight = min(480, echoArea.Height());
    m_echoBounds.SetRect(
        echoArea.left + (echoArea.Width() - echoWidth) / 2,
        echoArea.top + (echoArea.Height() - echoHeight) / 2,
        echoArea.left + (echoArea.Width() - echoWidth) / 2 + echoWidth,
        echoArea.top + (echoArea.Height() - echoHeight) / 2 + echoHeight);

    CWnd* placeholder = GetDlgItem(IDC_EXPLORER_CGTP);
    placeholder->DestroyWindow();

    if (!m_ie.Create(m_hWnd, m_echoBounds, nullptr,
                     WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
                     0, IDC_EXPLORER_CGTP))
    {
        Msg(L"Unable to create the ATL ActiveX host window.", L"Internet Explorer", MB_ICONWARNING);
    }
    else
    {
        HRESULT hr = m_ie.CreateControl(L"Shell.Explorer.2");
        if (SUCCEEDED(hr))
            hr = m_ie.QueryControl(IID_IWebBrowser2, reinterpret_cast<void**>(&m_webBrowser));
        if (SUCCEEDED(hr) && m_webBrowser)
            hr = m_webBrowser->put_Visible(VARIANT_TRUE);
        if (FAILED(hr) || !m_webBrowser)
        {
            wchar_t message[128];
            swprintf_s(message, L"Unable to initialize the IE WebBrowser control (HRESULT 0x%08lX).", hr);
            Msg(message, L"Internet Explorer", MB_ICONWARNING);
        }
        else
        {
            m_webBrowser->put_Silent(VARIANT_FALSE);
            m_ie.ShowWindow(SW_SHOW);
        }
    }
    if (!m_autoConnectOnStartup)
        LoadIp();
    UpdateData(FALSE);
    m_btShift.SetBackColor(RGB(240, 240, 240));
    m_btKeyShow.SetBackColor(kGreen);
    if (m_autoConnectOnStartup)
        PostMessage(WM_COMMAND, MAKEWPARAM(IDC_BT_CONNECT, BN_CLICKED),
                    reinterpret_cast<LPARAM>(m_btConnect.GetSafeHwnd()));
    return TRUE;
}

void CFRTPDlg::ScaleLayout()
{
    CRect client;
    GetClientRect(&client);
    if (!m_scaledFont.CreatePointFont(72, L"MS Shell Dlg"))
    {
        Msg(L"Unable to create the scaled interface font.", L"FRTP", MB_ICONWARNING);
        return;
    }
    SetFont(&m_scaledFont, FALSE);

    for (CWnd* child = GetWindow(GW_CHILD); child;)
    {
        CWnd* next = child->GetNextWindow();
        CRect bounds;
        child->GetWindowRect(&bounds);
        ScreenToClient(&bounds);
        bounds.left = MulDiv(bounds.left, 4, 5);
        bounds.top = MulDiv(bounds.top, 4, 5);
        bounds.right = MulDiv(bounds.right, 4, 5);
        bounds.bottom = MulDiv(bounds.bottom, 4, 5);
        child->MoveWindow(&bounds, FALSE);
        child->SetFont(&m_scaledFont, FALSE);
        child = next;
    }

    CRect window;
    GetWindowRect(&window);
    int frameWidth = window.Width() - client.Width();
    int frameHeight = window.Height() - client.Height();
    SetWindowPos(nullptr, 0, 0,
                 MulDiv(client.Width(), 4, 5) + frameWidth,
                 MulDiv(client.Height(), 4, 5) + frameHeight,
                 SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
    CenterWindow();
}

BOOL CFRTPDlg::PreTranslateMessage(MSG* pMsg)
{
    if (m_tip.m_hWnd) m_tip.RelayEvent(pMsg);
    if (pMsg->message == WM_KEYDOWN && (pMsg->wParam == VK_ESCAPE || pMsg->wParam == VK_RETURN))
    {
        if (pMsg->wParam == VK_RETURN)
        {
            CWnd* f = GetFocus();
            if (f && f->GetDlgCtrlID() >= IDC_TB_ROBOTIP && f->GetDlgCtrlID() <= IDC_TB_PASSWORD)
                OnBnClickedConnect();
        }
        return TRUE;
    }
    return CDialog::PreTranslateMessage(pMsg);
}

HBRUSH CFRTPDlg::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
    return CDialog::OnCtlColor(pDC, pWnd, nCtlColor);
}

LRESULT CFRTPDlg::OnPostedUi(WPARAM, LPARAM lp)
{
    RunPostedUi(lp);
    return 0;
}

void CFRTPDlg::Msg(const std::wstring& text, const wchar_t* caption, UINT flags)
{
    ::MessageBoxW(m_hWnd, text.c_str(), caption, flags);
}

int CFRTPDlg::Ask(const std::wstring& text, const wchar_t* caption, UINT flags)
{
    return ::MessageBoxW(m_hWnd, text.c_str(), caption, flags);
}

// ---------------------------------------------------------------- IP file

void CFRTPDlg::LoadIp()
{
    std::wstring path = PathCombine2(ExeDir(), L"robot_ip.txt");
    if (!FileExists(path)) return;
    std::wstring ip = Trim(ReadTextFile(path));
    if (!ip.empty()) m_ip = ip.c_str();
}

void CFRTPDlg::SaveIp()
{
    UpdateData(TRUE);
    WriteTextFile(PathCombine2(ExeDir(), L"robot_ip.txt"), std::wstring(m_ip));
}

// ---------------------------------------------------------------- panel / layout

void CFRTPDlg::CollectKeyboardControls()
{
    CRect area;
    GetDlgItem(IDC_AREA_KEYBOARD)->GetWindowRect(&area);
    ScreenToClient(&area);
    m_kbArea = area;
    m_kbControls.clear();
    for (CWnd* w = GetWindow(GW_CHILD); w; w = w->GetWindow(GW_HWNDNEXT))
    {
        int id = w->GetDlgCtrlID();
        if (id == IDC_AREA_KEYBOARD || id == IDC_PB_BACKUP) continue;
        CRect r; w->GetWindowRect(&r); ScreenToClient(&r);
        CRect inter;
        if (inter.IntersectRect(&r, &area) && inter == r)
            m_kbControls.push_back(w->m_hWnd);
    }
}

void CFRTPDlg::SetPanelEnabled(bool enable)
{
    for (HWND h : m_kbControls) ::EnableWindow(h, enable);
}

void CFRTPDlg::SetKeyboardVisible(bool visible)
{
    m_kbVisible = visible;
    for (HWND h : m_kbControls) ::ShowWindow(h, visible ? SW_SHOW : SW_HIDE);
    if (!visible) m_pb.ShowWindow(SW_HIDE);
    int clientH = visible ? m_fullClientH : m_collapsedClientH;
    CRect wr, cr; GetWindowRect(&wr); GetClientRect(&cr);
    int nonClient = wr.Height() - cr.Height();
    SetWindowPos(nullptr, 0, 0, wr.Width(), clientH + nonClient, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
    m_btKeyShow.SetBackColor(visible ? kGreen : RGB(240, 240, 240));
}

void CFRTPDlg::SetRobotCfgVisible(bool visible)
{
    for (UINT id : { IDC_TB_ROBOTIP, IDC_TB_NAME, IDC_TB_PASSWORD, IDC_LB_NAME, IDC_LB_PASSWORD, IDC_CB_BFSELECT })
        GetDlgItem(id)->ShowWindow(visible ? SW_SHOW : SW_HIDE);
    GetDlgItem(IDC_LB_HOSTNAME)->ShowWindow(visible ? SW_HIDE : SW_SHOW);
}

void CFRTPDlg::OnBnClickedKeyShow()
{
    SetKeyboardVisible(!m_kbVisible);
}

void CFRTPDlg::SetTopMost(bool on)
{
    m_topMost = on;
    SetWindowPos(on ? &wndTopMost : &wndNoTopMost, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    m_btTopMost.SetBackColor(on ? kGreen : RGB(240, 240, 240));
}

void CFRTPDlg::DisableTopMostForDialog()
{
    if (m_topMost) SetTopMost(false);
}

void CFRTPDlg::OnBnClickedTopMost()
{
    SetTopMost(!m_topMost);
}

void CFRTPDlg::SetShift(bool on)
{
    m_shift = on;
    m_btShift.SetColors(on ? kBlue : RGB(240, 240, 240), on ? RGB(255, 255, 255) : RGB(0, 0, 0));
}

void CFRTPDlg::OnBnClickedShift()
{
    SetShift(!m_shift);
}

void CFRTPDlg::SetTpSwitchImage(bool on)
{
    CBitmap& b = on ? m_bmpTpOn : m_bmpTpOff;
    if (b.GetSafeHandle()) m_picSwitch.SetBitmap((HBITMAP)b.GetSafeHandle());
}

void CFRTPDlg::OnBnClickedHelp()
{
    CHelpDlg::ShowHelp(this);
}

// ---------------------------------------------------------------- browsers

void CFRTPDlg::NavigateIe(const std::wstring& url)
{
    if (!m_webBrowser) return;
    CComVariant target(url.c_str());
    CComVariant empty;
    HRESULT hr = m_webBrowser->Navigate2(&target, &empty, &empty, &empty, &empty);
    if (FAILED(hr))
    {
        wchar_t message[128];
        swprintf_s(message, L"IE Navigate2 failed (HRESULT 0x%08lX).", hr);
        Msg(message, L"Internet Explorer", MB_ICONWARNING);
    }
}

void CFRTPDlg::ShowBrowser(bool ie)
{
    if (m_ie.IsWindow())
    {
        if (ie)
        {
            m_ie.SetWindowPos(HWND_TOP, 0, 0, 0, 0,
                              SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            m_ie.UpdateWindow();
        }
        else
        {
            m_ie.ShowWindow(SW_HIDE);
        }
    }
    if (CWnd* webViewHost = GetDlgItem(IDC_WEBVIEW_CGTP))
        webViewHost->ShowWindow(ie ? SW_HIDE : SW_SHOW);
    m_edge.SetVisible(!ie);
}

// ---------------------------------------------------------------- connect / disconnect

void CFRTPDlg::InitAll()
{
    KillTimer(kTimerId);
    m_session++;
    m_ftp.reset();
    m_connected = false;
    m_online = false;
    SetPanelEnabled(false);
    DragAcceptFiles(FALSE);
    CString t; m_btConnect.GetWindowText(t);
    m_btConnect.SetWindowText(L"Connect");
    m_btConnect.SetBackColor(RGB(255, 255, 255));
    GetDlgItem(IDC_LB_HOSTNAME)->SetWindowText(L"");
    NavigateIe(L"about:blank");
    if (m_edgeInitialized)
    {
        m_edge.Navigate(L"about:blank");
    }
    ShowBrowser(true);
    SetRobotCfgVisible(true);
    GetDlgItem(IDC_TB_ROBOTIP)->EnableWindow(TRUE);
    GetDlgItem(IDC_BT_CONNECT)->EnableWindow(TRUE);
    m_pb.ShowWindow(SW_HIDE);
    SetBusy(false);
    SetTpSwitchImage(false);
    if (m_kbVisible) { /* keyboard stays visible but disabled */ }
}

void CFRTPDlg::SetBusy(bool busy)
{
    m_busy = busy;
    for (UINT id : { IDC_BT_BACKUP, IDC_BT_DOWNLOAD })
        if (CWnd* w = GetDlgItem(id)) w->EnableWindow(!busy && m_connected);
}

void CFRTPDlg::OnBnClickedConnect()
{
    CString text; m_btConnect.GetWindowText(text);
    if (text == L"Disconnect") { InitAll(); return; }
    if (text != L"Connect") return;

    UpdateData(TRUE);
    std::wstring ip = Trim(std::wstring(m_ip));
    PingResult pr = PingHost(ip, 100);
    if (pr == PingResult::InvalidAddress)
    {
        InitAll();
        Msg(L"ERROR: iP Address invalid.\n Response: " + ip, L"FRTP", MB_ICONERROR);
        return;
    }
    if (pr == PingResult::Failed)
    {
        InitAll();
        Msg(L"ERROR: Ping failed", L"FRTP", MB_ICONERROR);
        return;
    }
    m_btConnect.SetWindowText(L"Connecting");
    m_btConnect.EnableWindow(FALSE);
    GetDlgItem(IDC_TB_ROBOTIP)->EnableWindow(FALSE);

    CString title; GetWindowText(title);
    int session = ++m_session;
    std::wstring user(m_user), pass(m_pass);
    bool exitUi = m_exitUi != FALSE;
    std::thread(&CFRTPDlg::ConnectWorker, this, session, ip, user, pass, exitUi, std::wstring(title)).detach();
}

void CFRTPDlg::ConnectWorker(int session, std::wstring ip, std::wstring user, std::wstring pass, bool exitUi, std::wstring title)
{
    HWND hwnd = m_hWnd;
    auto fail = [=](std::wstring text, std::wstring caption, UINT flags) {
        PostToUi(hwnd, [=] {
            if (session != m_session) return;
            InitAll();
            Msg(text, caption.c_str(), flags);
        });
    };

    std::wstring robot = RobotChecker::GetRobotVersion(ip);
    int type = ControllerType(robot);
    if (!type)
    {
        fail(L"Not the FANUC R-30iA,R-30iB,R-30iB Plus,R-50iA  Controller.\n", L"FRTP", MB_ICONERROR);
        return;
    }
    try
    {
        if (!FRLoadUserPC::LoadMKWebServer(ip, type, user, pass, title))
        {
            fail(L"Karel Program Load Error\n", L"FRTP", MB_ICONERROR);
            return;
        }
    }
    catch (const AppError& e)
    {
        fail(L"ERROR: " + e.msg, L"FRTP", MB_ICONERROR);
        return;
    }

    int auth = MkWebClient::CheckAuthStatus(ip);
    if (auth != 0)
    {
        std::wstring text, cap;
        switch (auth)
        {
        case 1: text = L"401 Unauthorized: Please unlock KAREL resources on the teach pendant:\nMENU - SETUP - HostComm - HTTP"; cap = L"Authorization Required"; break;
        case 2: text = L"402 Payment Required: HTTP access requires payment or license"; cap = L"Payment Required"; break;
        case 3: text = L"403 Forbidden: Please unlock KAREL resources on the teach pendant:\nMENU - SETUP - HostComm - HTTP"; cap = L"Forbidden"; break;
        default: text = L"HTTP access blocked or an error occurred. Please check network and controller settings."; cap = L"Authorization Required"; break;
        }
        PostToUi(hwnd, [=] {
            if (session != m_session) return;
            DisableTopMostForDialog();
            InitAll();
            Msg(text, cap.c_str(), MB_ICONWARNING);
        });
        return;
    }

    std::wstring name;
    name = MkWebClient::ReadVar(ip, L"*SYSTEM*", L"$HOSTNAME", L"STR").value;
    name = Trim(name);

    if (exitUi)
    {
        MkWebClient::SimTpKey(ip, L"145");
        Sleep(50);
        for (int i = 0; i < 2; i++)
        {
            MkWebClient::SimTpKey(ip, L"240");
            MkWebClient::SimTpKey(ip, L"145");
            Sleep(50);
        }
    }

    std::shared_ptr<FtpClient> ftp;
    try
    {
        ftp = std::make_shared<FtpClient>(ip, user, pass);
        ftp->Connect();
    }
    catch (const AppError& e)
    {
        fail(L"ERROR: " + e.msg, L"FRTP", MB_ICONERROR);
        return;
    }

    PostToUi(hwnd, [=] {
        if (session != m_session) return;
        m_robotType = robot;
        m_robotName = name;
        m_robotIp = ip;
        m_adminUser = user;
        m_adminPass = pass;
        m_ftp = ftp;
        m_connected = true;
        m_online = true;

        bool ie = type != 4;
        std::wstring url = L"http://" + ip + L":80/frh/cgtp/echo.htm";
        if (ie)
        {
            if (m_edgeInitialized) m_edge.Navigate(L"about:blank");
            ShowBrowser(true);
            NavigateIe(url);
        }
        else
        {
            if (!m_edgeInitialized)
            {
                CWnd* webViewHost = GetDlgItem(IDC_WEBVIEW_CGTP);
                if (!webViewHost || !::IsWindow(webViewHost->GetSafeHwnd()))
                {
                    InitAll();
                    Msg(L"Unable to find the WebView2 display area.", L"WebView2", MB_ICONERROR);
                    return;
                }
                CRect webViewBounds;
                webViewHost->GetClientRect(&webViewBounds);
                if (webViewBounds.IsRectEmpty())
                {
                    InitAll();
                    Msg(L"The WebView2 display area has no size.", L"WebView2", MB_ICONERROR);
                    return;
                }
                m_edgeInitialized = true;
                m_edge.Init(webViewHost->GetSafeHwnd(), webViewBounds, WebView2DataFolder(), 0.5,
                    [this, session](bool ok)
                    {
                        if (!ok)
                        {
                            m_edgeInitialized = false;
                            if (session == m_session && m_connected && !m_edgeFailedShown)
                            {
                                m_edgeFailedShown = true;
                                Msg(L"Unable to initialize the Edge WebView2 browser. Verify that the WebView2 Runtime is installed, then reconnect.",
                                    L"WebView2", MB_ICONERROR);
                            }
                            return;
                        }
                        m_edgeFailedShown = false;
                    });
            }
            NavigateIe(L"about:blank");
            ShowBrowser(false);
            m_edge.Navigate(url);
        }

        SetTimer(kTimerId, kTimerMs, nullptr);
        m_btConnect.EnableWindow(TRUE);
        m_btConnect.SetWindowText(L"Disconnect");
        m_btConnect.SetBackColor(kGreen);
        GetDlgItem(IDC_LB_HOSTNAME)->SetWindowText((L"HostName:" + name).c_str());
        SetPanelEnabled(true);
        SetRobotCfgVisible(false);
        DragAcceptFiles(TRUE);
        SetBusy(false);
        SaveIp();
        StartKeyThread();
    });
}

// ---------------------------------------------------------------- polling

void CFRTPDlg::OnTimer(UINT_PTR id)
{
    if (id == kTimerId) PollRobot();
    CDialog::OnTimer(id);
}

void CFRTPDlg::PollRobot()
{
    if (!m_connected || m_pollBusy.exchange(true)) return;
    HWND hwnd = m_hWnd;
    int session = m_session;
    std::wstring ip = m_robotIp;
    std::thread([=] {
        int ok = 0;
        for (int i = 0; i < 3; i++)
        {
            if (PingHost(ip, 1000) == PingResult::Ok) ok++;
            Sleep(100);
        }
        bool alive = ok >= 2 && !MkWebClient::GetLibVersion(ip, 1500).empty();
        if (!alive)
        {
            PostToUi(hwnd, [=] {
                m_pollBusy = false;
                if (session == m_session) RestartApp();
            });
            return;
        }
        int v = 0;
        try { auto r = MkWebClient::GetIoValue(ip, MkWebClient::IO_OperatorPanelOutput, 7); v = r.status == 0 ? r.value : 0; }
        catch (...) { v = 0; }
        PostToUi(hwnd, [=] {
            m_pollBusy = false;
            if (session == m_session) SetTpSwitchImage(v != 0);
        });
    }).detach();
}

void CFRTPDlg::RestartApp()
{
    wchar_t path[MAX_PATH];
    ::GetModuleFileNameW(nullptr, path, MAX_PATH);
    InitAll();
    ::ShellExecuteW(nullptr, L"open", path, nullptr, nullptr, SW_SHOWNORMAL);
    EndDialog(IDCANCEL);
}

// ---------------------------------------------------------------- keys

void CFRTPDlg::StartKeyThread()
{
    StopKeyThread();
    m_keyStop = false;
    m_keyThread = std::thread([this] {
        for (;;)
        {
            std::pair<std::wstring, int> item;
            {
                std::unique_lock<std::mutex> lk(m_keyMutex);
                m_keyCv.wait(lk, [this] { return m_keyStop || !m_keyQueue.empty(); });
                if (m_keyStop) return;
                item = m_keyQueue.front();
                m_keyQueue.pop_front();
            }
            MkWebClient::SimTpKey(item.first, std::to_wstring(item.second));
        }
    });
}

void CFRTPDlg::StopKeyThread()
{
    {
        std::lock_guard<std::mutex> lk(m_keyMutex);
        m_keyStop = true;
        m_keyQueue.clear();
    }
    m_keyCv.notify_all();
    if (m_keyThread.joinable()) m_keyThread.join();
}

void CFRTPDlg::QueueKey(int code)
{
    if (!m_online) return;
    {
        std::lock_guard<std::mutex> lk(m_keyMutex);
        m_keyQueue.emplace_back(m_robotIp, code);
    }
    m_keyCv.notify_one();
}

void CFRTPDlg::OnPendantKey(UINT id)
{
    for (const auto& k : kKeys)
    {
        if (k.id != id) continue;
        int code = (m_shift && k.shifted != -1) ? k.shifted : k.normal;
        QueueKey(code);
        return;
    }
}

void CFRTPDlg::OnBnClickedKeyboard()
{
    if (!m_connected) return;
    DisableTopMostForDialog();
    CInputDlg dlg(this);
    if (dlg.DoModal() != IDOK) return;
    std::string bytes = ToUtf8(std::wstring(dlg.m_text));
    for (unsigned char c : bytes) QueueKey(c);
}

// ---------------------------------------------------------------- folder picker

bool CFRTPDlg::PickFolder(CString& path)
{
    CFolderPickerDialog dlg(nullptr, 0, this);
    if (dlg.DoModal() != IDOK) return false;
    path = dlg.GetPathName();
    return !path.IsEmpty();
}

// ---------------------------------------------------------------- backup

void CFRTPDlg::OnBnClickedBackup()
{
    if (!m_connected || m_busy) return;
    DisableTopMostForDialog();
    CString folder;
    if (!PickFolder(folder)) return;
    DoBackup(folder);
}

void CFRTPDlg::DoBackup(CString folder)
{
    UpdateData(TRUE);
    bool comment = m_comment != FALSE;
    SetBusy(true);
    m_pb.SetPos(0);
    m_pb.ShowWindow(SW_SHOW);

    HWND hwnd = m_hWnd;
    int session = m_session;
    auto ftp = m_ftp;
    std::wstring ip = m_robotIp, robot = m_robotType, name = m_robotName;
    std::wstring root(folder);

    std::thread([=] {
        std::wstring currentFile;
        auto progress = [=](int p) {
            PostToUi(hwnd, [=] { if (session == m_session) m_pb.SetPos(p); });
        };
        auto finish = [=](std::wstring text, std::wstring cap, UINT flags) {
            PostToUi(hwnd, [=] {
                if (session != m_session) return;
                m_pb.ShowWindow(SW_HIDE);
                SetBusy(false);
                Msg(text, cap.c_str(), flags);
            });
        };
        try
        {
            std::wstring safe = name;
            for (auto& c : safe)
                if (wcschr(L"\\/:*?\"<>|", c) || c < 32) c = L'_';
            SYSTEMTIME st; GetLocalTime(&st);
            wchar_t stamp[32];
            swprintf_s(stamp, L"%04d%02d%02d%02d%02d", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute);
            std::wstring dir = PathCombine2(root, safe + L"_Backup_" + stamp);
            SHCreateDirectoryExW(nullptr, dir.c_str(), nullptr);

            std::wstring v = comment ? L"1" : L"0";

            MkWebClient::WriteVar(ip, L"*SYSTEM*", L"$MNDSP_CMNT", L"INT", v);
            MkWebClient::WriteVar(ip, L"*SYSTEM*", L"$UI_CONFIG.$IOSTAT_INST", L"INT", v);

            ftp->SetServerPath(L"MDB:");
            auto list = ftp->ListDirectory();
            std::vector<std::wstring> tpNames;
            size_t total = list.size(), processed = 0;
            for (auto& f : list)
            {
                processed++;
                std::wstring up = ToUpper(f.name);
                if (f.type != FtpFileType::File || up == L"A_MKWEB.PC" || (!f.name.empty() && f.name[0] == L'-'))
                {
                    progress((int)(processed * 100 / total));
                    continue;
                }
                currentFile = f.name;
                ftp->Download(f.name, PathCombine2(dir, f.name));
                if (up.size() > 3 && up.substr(up.size() - 3) == L".TP")
                    tpNames.push_back(f.name.substr(0, f.name.size() - 3));
                progress((int)(processed * 100 / total));
            }

            ftp->SetServerPath(L"MD:");
            size_t tpTotal = tpNames.size(), tpProcessed = 0;
            for (auto& n : tpNames)
            {
                std::wstring fn = n + L".ls";
                currentFile = fn;
                ftp->Download(fn, PathCombine2(dir, fn));
                tpProcessed++;
                progress((int)((processed + tpProcessed) * 100 / (total + tpTotal)));
            }
            ftp->SetServerPath(L"MDB:");
            finish(L"Backup completed!", L"Prompt", MB_ICONINFORMATION);
        }
        catch (const AppError& e)
        {
            finish(L"An error occurred: " + e.msg + L", file: " + currentFile, L"Error", MB_ICONERROR);
        }
    }).detach();
}

// ---------------------------------------------------------------- download

void CFRTPDlg::OnBnClickedDownload()
{
    if (!m_connected || m_busy) return;
    DisableTopMostForDialog();

    MkWebClient::VarResponse r = MkWebClient::ReadVar(m_robotIp, L"*SYSTEM*", L"$TP_DEFPROG", L"STR");
    std::wstring prog = Trim(r.value);
    if (prog.empty())
    {
        Msg(L"Program name not specified:$TP_DEFPROG", L"Error", MB_ICONERROR);
        return;
    }
    CString folder;
    if (!PickFolder(folder)) return;
    DoDownload(folder, prog);
}

void CFRTPDlg::DoDownload(CString folder, std::wstring program)
{
    SetBusy(true);
    m_pb.SetPos(0);
    m_pb.ShowWindow(SW_SHOW);
    HWND hwnd = m_hWnd;
    int session = m_session;
    auto ftp = m_ftp;
    std::wstring dest(folder);

    std::thread([=] {
        auto finish = [=](std::wstring text, std::wstring cap, UINT flags) {
            PostToUi(hwnd, [=] {
                if (session != m_session) return;
                m_pb.ShowWindow(SW_HIDE);
                SetBusy(false);
                Msg(text, cap.c_str(), flags);
            });
        };
        auto baseName = [](const std::wstring& n) {
            size_t p = n.find_last_of(L'.');
            return p == std::wstring::npos ? n : n.substr(0, p);
        };
        try
        {
            std::wstring target = ToUpper(program);
            ftp->SetServerPath(L"MDB:");
            std::vector<std::wstring> mdb, md;
            for (auto& f : ftp->ListDirectory())
                if (f.type == FtpFileType::File && ToUpper(baseName(f.name)) == target) mdb.push_back(f.name);
            if (mdb.empty())
            {
                ftp->SetServerPath(L"MDB:");
                finish(L"MDB: File with program name '" + program + L"' was not found.", L"Prompt", MB_ICONINFORMATION);
                return;
            }
            ftp->SetServerPath(L"MD:");
            for (auto& f : ftp->ListDirectory())
            {
                if (f.type != FtpFileType::File || ToUpper(baseName(f.name)) != target) continue;
                bool dup = false;
                for (auto& m : mdb) if (ToUpper(m) == ToUpper(f.name)) dup = true;
                if (!dup) md.push_back(f.name);
            }
            size_t total = mdb.size() + md.size(), done = 0;
            ftp->SetServerPath(L"MDB:");
            for (auto& n : mdb)
            {
                ftp->Download(n, PathCombine2(dest, n));
                done++;
                int p = (int)(done * 100 / total);
                PostToUi(hwnd, [=] { if (session == m_session) m_pb.SetPos(p); });
            }
            ftp->SetServerPath(L"MD:");
            for (auto& n : md)
            {
                ftp->Download(n, PathCombine2(dest, n));
                done++;
                int p = (int)(done * 100 / total);
                PostToUi(hwnd, [=] { if (session == m_session) m_pb.SetPos(p); });
            }
            ftp->SetServerPath(L"MDB:");
            finish(L"Program '" + program + L"' Download completed.\nTotal downloaded: " + std::to_wstring(total) + L" files\n",
                   L"Download completed", MB_ICONINFORMATION);
        }
        catch (const AppError& e)
        {
            finish(L"Error occurred during download:" + e.msg, L"Error", MB_ICONERROR);
        }
    }).detach();
}

// ---------------------------------------------------------------- upload

void CFRTPDlg::OnDropFiles(HDROP hDrop)
{
    std::vector<std::wstring> files;
    UINT n = DragQueryFileW(hDrop, 0xFFFFFFFF, nullptr, 0);
    for (UINT i = 0; i < n; i++)
    {
        UINT len = DragQueryFileW(hDrop, i, nullptr, 0);
        std::wstring s(len + 1, L'\0');
        DragQueryFileW(hDrop, i, &s[0], len + 1);
        s.resize(len);
        files.push_back(s);
    }
    POINT pt; DragQueryPoint(hDrop, &pt);
    DragFinish(hDrop);

    if (!m_connected || m_busy) return;
    CRect r; m_btUpload.GetWindowRect(&r); ScreenToClient(&r);
    if (!r.PtInRect(pt)) return;
    DoUpload(files);
}

void CFRTPDlg::DoUpload(std::vector<std::wstring> files)
{
    DisableTopMostForDialog();
    auto io = MkWebClient::GetIoValue(m_robotIp, MkWebClient::IO_OperatorPanelOutput, 7);
    if (io.value == 0)
    {
        Msg(L"Program upload not allowed.\r\n Please turn the teach pendant mode switch to ON.", L"Notify", MB_ICONINFORMATION);
        return;
    }
    std::vector<std::wstring> valid;
    for (auto& f : files)
    {
        size_t p = f.find_last_of(L'.');
        std::wstring ext = p == std::wstring::npos ? L"" : ToUpper(f.substr(p));
        if (ext == L".TP" || ext == L".PC" || ext == L".VR" || ext == L".LS")
            valid.push_back(f);
        else
        {
            size_t s = f.find_last_of(L"\\/");
            Msg(L"Uploading this file type is not allowed: " + ext + L" (file: " + f.substr(s == std::wstring::npos ? 0 : s + 1) + L")",
                L"File Type Error", MB_ICONWARNING);
        }
    }
    m_ftp->SetServerPath(L"MDB:");
    if (valid.empty())
    {
        Msg(L"There are no files allowed to be uploaded!", L"Operation Canceled", MB_ICONINFORMATION);
        return;
    }
    if (Ask(L"Do you want to upload " + std::to_wstring(valid.size()) + L" files to the FTP server?", L"Confirm Upload",
            MB_YESNO | MB_ICONQUESTION) != IDYES)
    {
        Msg(L"File upload has been canceled!", L"Operation Canceled", MB_ICONINFORMATION);
        return;
    }
    SetBusy(true);
    HWND hwnd = m_hWnd;
    int session = m_session;
    auto ftp = m_ftp;
    std::thread([=] {
        std::wstring text, cap;
        UINT flags;
        try
        {
            for (auto& f : valid)
            {
                size_t s = f.find_last_of(L"\\/");
                ftp->Upload(f, f.substr(s == std::wstring::npos ? 0 : s + 1));
            }
            text = L"Successfully uploaded " + std::to_wstring(valid.size()) + L" files to the FTP server!";
            cap = L"Upload Successful"; flags = MB_ICONINFORMATION;
        }
        catch (const AppError& e)
        {
            text = L"An error occurred while uploading files: " + e.msg;
            cap = L"Upload Error"; flags = MB_ICONERROR;
        }
        PostToUi(hwnd, [=] {
            if (session != m_session) return;
            SetBusy(false);
            Msg(text, cap.c_str(), flags);
        });
    }).detach();
}

// ---------------------------------------------------------------- shutdown

void CFRTPDlg::OnClose()
{
    InitAll();
    EndDialog(IDCANCEL);
}

void CFRTPDlg::OnDestroy()
{
    KillTimer(kTimerId);
    StopKeyThread();
    if (m_webBrowser)
    {
        m_webBrowser->Stop();
        m_webBrowser.Release();
    }
    m_edge.Close();
    CDialog::OnDestroy();
}
