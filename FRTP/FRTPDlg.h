#pragma once
#include <deque>
#include <condition_variable>
#include <atlhost.h>
#include "ColorButton.h"
#include "WebView2Host.h"
#include "Util.h"

class CFRTPDlg : public CDialog
{
public:
    explicit CFRTPDlg(CWnd* pParent = nullptr);
    ~CFRTPDlg() override;
    void SetStartupCredentials(const std::wstring& ip, const std::wstring& name, const std::wstring& pwd);

    enum { IDD = IDD_FRTP_DIALOG };

protected:
    void DoDataExchange(CDataExchange* pDX) override;
    BOOL OnInitDialog() override;
    BOOL PreTranslateMessage(MSG* pMsg) override;
    void OnOK() override {}
    void OnCancel() override {}

    DECLARE_MESSAGE_MAP()
    afx_msg void OnClose();
    afx_msg void OnDestroy();
    afx_msg void OnTimer(UINT_PTR id);
    afx_msg void OnDropFiles(HDROP hDrop);
    afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
    afx_msg LRESULT OnPostedUi(WPARAM, LPARAM lp);
    afx_msg void OnBnClickedConnect();
    afx_msg void OnBnClickedHelp();
    afx_msg void OnBnClickedKeyShow();
    afx_msg void OnBnClickedTopMost();
    afx_msg void OnBnClickedShift();
    afx_msg void OnBnClickedKeyboard();
    afx_msg void OnBnClickedBackup();
    afx_msg void OnBnClickedDownload();
    afx_msg void OnPendantKey(UINT id);

private:
    // controls bound to resource IDs
    CColorButton m_btConnect, m_btShift, m_btTopMost, m_btKeyShow, m_btUpload;
    CProgressCtrl m_pb;
    CAxWindow m_ie;
    CComPtr<IWebBrowser2> m_webBrowser;
    CStatic m_picSwitch;
    CFont m_scaledFont;
    CString m_ip, m_user, m_pass;
    bool m_autoConnectOnStartup = false;
    BOOL m_exitUi = FALSE;
    BOOL m_comment = TRUE;

    HICON m_hIcon = nullptr;
    std::vector<std::unique_ptr<CBitmap>> m_bitmaps;
    CBitmap m_bmpTpOn, m_bmpTpOff;
    std::vector<HWND> m_kbControls;
    CRect m_kbArea;
    CRect m_echoBounds;
    int m_fullClientH = 0;
    int m_collapsedClientH = 0;

    // state
    CToolTipCtrl m_tip;
    std::shared_ptr<class FtpClient> m_ftp;
    CWebView2Host m_edge;
    bool m_edgeInitialized = false;
    bool m_edgeFailedShown = false;
    bool m_connected = false;       // UI thread view
    std::atomic<bool> m_online{ false };   // seen by workers
    bool m_shift = false;
    bool m_topMost = false;
    bool m_kbVisible = true;
    bool m_busy = false;            // FTP operation running
    std::atomic<bool> m_pollBusy{ false };
    int m_session = 0;
    std::wstring m_robotType, m_robotName, m_robotIp, m_adminUser, m_adminPass;

    // key sender
    std::mutex m_keyMutex;
    std::condition_variable m_keyCv;
    std::deque<std::pair<std::wstring, int>> m_keyQueue;
    std::thread m_keyThread;
    bool m_keyStop = false;

    void StartKeyThread();
    void StopKeyThread();
    void QueueKey(int code);

    void LoadIp();
    void SaveIp();
    void ScaleLayout();
    void InitAll();
    void SetPanelEnabled(bool enable);
    void SetKeyboardVisible(bool visible);
    void SetRobotCfgVisible(bool visible);
    void CollectKeyboardControls();
    void SetTopMost(bool on);
    void DisableTopMostForDialog();
    void SetShift(bool on);
    void SetTpSwitchImage(bool on);
    void ShowBrowser(bool ie);
    void NavigateIe(const std::wstring& url);
    void SnapBrowserArea();
    void SetBusy(bool busy);
    void Msg(const std::wstring& text, const wchar_t* caption = L"FRTP", UINT flags = MB_OK);
    int Ask(const std::wstring& text, const wchar_t* caption, UINT flags);
    bool PickFolder(CString& path);

    void ConnectWorker(int session, std::wstring ip, std::wstring user, std::wstring pass, bool exitUi, std::wstring title);
    void PollRobot();
    void DoBackup(CString folder);
    void DoDownload(CString folder, std::wstring program);
    void DoUpload(std::vector<std::wstring> files);
    void RestartApp();
};
