#pragma once

// "Input Data" dialog used to send a text string to the controller.
class CInputDlg : public CDialog
{
public:
    explicit CInputDlg(CWnd* parent = nullptr);
    CString m_text;

protected:
    void DoDataExchange(CDataExchange* pDX) override;
    BOOL OnInitDialog() override;
};
