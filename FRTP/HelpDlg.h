#pragma once
#include "WebView2Host.h"

// Modeless help window showing res\Helper\help.html in WebView2.
class CHelpDlg : public CDialog
{
public:
    // Shows the help window, or activates it when already open.
    static void ShowHelp(CWnd* parent);

protected:
    explicit CHelpDlg(CWnd* parent = nullptr);
    BOOL OnInitDialog() override;
    void OnOK() override { DestroyWindow(); }
    void OnCancel() override { DestroyWindow(); }
    void PostNcDestroy() override;
    DECLARE_MESSAGE_MAP()
    afx_msg void OnClose();

private:
    CWebView2Host m_web;
    static CHelpDlg* s_instance;
};
