#include "pch.h"
#include "Resource.h"
#include "InputDlg.h"

CInputDlg::CInputDlg(CWnd* parent) : CDialog(IDD_INPUT_DIALOG, parent) {}

void CInputDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    DDX_Text(pDX, IDC_TB_INPUT, m_text);
}

BOOL CInputDlg::OnInitDialog()
{
    CDialog::OnInitDialog();
    GetDlgItem(IDC_TB_INPUT)->SetFocus();
    return FALSE;
}
