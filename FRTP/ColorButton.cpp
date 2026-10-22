#include "pch.h"
#include "ColorButton.h"

BEGIN_MESSAGE_MAP(CColorButton, CButton)
    ON_WM_ERASEBKGND()
END_MESSAGE_MAP()

CColorButton::CColorButton()
    : m_back(GetSysColor(COLOR_BTNFACE)), m_text(RGB(0, 0, 0))
{
}

void CColorButton::SetColors(COLORREF back, COLORREF text)
{
    m_back = back;
    m_text = text;
    if (GetSafeHwnd()) Invalidate();
}

void CColorButton::SetBackColor(COLORREF back)
{
    m_back = back;
    if (GetSafeHwnd()) Invalidate();
}

void CColorButton::SetGlyphFont(const wchar_t* face, int pointSize)
{
    m_font.DeleteObject();
    CClientDC dc(this);
    LOGFONTW lf = {};
    lf.lfHeight = -MulDiv(pointSize, dc.GetDeviceCaps(LOGPIXELSY), 72);
    lf.lfWeight = FW_NORMAL;
    wcscpy_s(lf.lfFaceName, face);
    m_hasFont = m_font.CreateFontIndirectW(&lf) != FALSE;
    if (GetSafeHwnd()) Invalidate();
}

void CColorButton::DrawItem(LPDRAWITEMSTRUCT lpDIS)
{
    CDC* pDC = CDC::FromHandle(lpDIS->hDC);
    CRect rc(lpDIS->rcItem);
    bool pressed = (lpDIS->itemState & ODS_SELECTED) != 0;
    bool disabled = (lpDIS->itemState & ODS_DISABLED) != 0;

    pDC->FillSolidRect(rc, m_back);
    pDC->DrawEdge(rc, pressed ? EDGE_SUNKEN : EDGE_RAISED, BF_RECT);

    CString text;
    GetWindowText(text);
    CFont* old = nullptr;
    if (m_hasFont) old = pDC->SelectObject(&m_font);
    else old = pDC->SelectObject(GetParent()->GetFont());

    pDC->SetBkMode(TRANSPARENT);
    pDC->SetTextColor(disabled ? GetSysColor(COLOR_GRAYTEXT) : m_text);
    CRect tr(rc);
    tr.DeflateRect(2, 2);
    if (pressed) tr.OffsetRect(1, 1);
    pDC->DrawTextW(text, tr, DT_CENTER | DT_VCENTER | DT_SINGLELINE | DT_NOPREFIX);

    if (lpDIS->itemState & ODS_FOCUS)
    {
        CRect fr(rc);
        fr.DeflateRect(3, 3);
        pDC->DrawFocusRect(fr);
    }
    pDC->SelectObject(old);
}
