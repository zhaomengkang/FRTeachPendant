#pragma once

// Owner-draw push button with a configurable background / text colour.
// Place a button with the "Owner Draw" property set in the resource editor and bind it with DDX_Control.
class CColorButton : public CButton
{
public:
    CColorButton();

    void SetColors(COLORREF back, COLORREF text = RGB(0, 0, 0));
    void SetBackColor(COLORREF back);
    void SetGlyphFont(const wchar_t* face, int pointSize);

    COLORREF GetBackColor() const { return m_back; }

protected:
    void DrawItem(LPDRAWITEMSTRUCT lpDIS) override;
    DECLARE_MESSAGE_MAP()
    afx_msg BOOL OnEraseBkgnd(CDC*) { return TRUE; }

private:
    COLORREF m_back;
    COLORREF m_text;
    CFont m_font;
    bool m_hasFont = false;
};
