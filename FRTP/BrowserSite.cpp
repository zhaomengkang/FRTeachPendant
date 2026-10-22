#include "pch.h"
#include "BrowserSite.h"

LPUNKNOWN CBrowserSite::GetInterfaceHook(const void* iid)
{
    if (*(const IID*)iid == IID_IDocHostUIHandler)
        return static_cast<IDocHostUIHandler*>(this);
    return nullptr;
}

STDMETHODIMP CBrowserSite::QueryInterface(REFIID riid, void** ppvObject) { return ExternalQueryInterface(&riid, ppvObject); }
STDMETHODIMP_(ULONG) CBrowserSite::AddRef() { return ExternalAddRef(); }
STDMETHODIMP_(ULONG) CBrowserSite::Release() { return ExternalRelease(); }

STDMETHODIMP CBrowserSite::ShowContextMenu(DWORD, POINT*, IUnknown*, IDispatch*) { return S_OK; }

STDMETHODIMP CBrowserSite::GetHostInfo(DOCHOSTUIINFO* pInfo)
{
    pInfo->cbSize = sizeof(DOCHOSTUIINFO);
    pInfo->dwFlags = DOCHOSTUIFLAG_NO3DBORDER | DOCHOSTUIFLAG_SCROLL_NO | DOCHOSTUIFLAG_DISABLE_HELP_MENU;
    pInfo->dwDoubleClick = DOCHOSTUIDBLCLK_DEFAULT;
    return S_OK;
}

STDMETHODIMP CBrowserSite::ShowUI(DWORD, IOleInPlaceActiveObject*, IOleCommandTarget*, IOleInPlaceFrame*, IOleInPlaceUIWindow*) { return S_OK; }
STDMETHODIMP CBrowserSite::HideUI() { return S_OK; }
STDMETHODIMP CBrowserSite::UpdateUI() { return S_OK; }
STDMETHODIMP CBrowserSite::EnableModeless(BOOL) { return S_OK; }
STDMETHODIMP CBrowserSite::OnDocWindowActivate(BOOL) { return S_OK; }
STDMETHODIMP CBrowserSite::OnFrameWindowActivate(BOOL) { return S_OK; }
STDMETHODIMP CBrowserSite::ResizeBorder(LPCRECT, IOleInPlaceUIWindow*, BOOL) { return S_OK; }

STDMETHODIMP CBrowserSite::TranslateAccelerator(LPMSG lpMsg, const GUID*, DWORD)
{
    if (lpMsg && (lpMsg->message == WM_KEYDOWN || lpMsg->message == WM_SYSKEYDOWN))
    {
        bool ctrl = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
        if (ctrl || lpMsg->wParam == VK_F5)
            return S_OK; // swallow browser shortcuts
    }
    return S_FALSE;
}

STDMETHODIMP CBrowserSite::GetOptionKeyPath(LPOLESTR*, DWORD) { return E_NOTIMPL; }
STDMETHODIMP CBrowserSite::GetDropTarget(IDropTarget*, IDropTarget**) { return E_NOTIMPL; }
STDMETHODIMP CBrowserSite::GetExternal(IDispatch** ppDispatch) { if (ppDispatch) *ppDispatch = nullptr; return E_NOTIMPL; }
STDMETHODIMP CBrowserSite::TranslateUrl(DWORD, OLECHAR*, OLECHAR**) { return E_NOTIMPL; }
STDMETHODIMP CBrowserSite::FilterDataObject(IDataObject*, IDataObject**) { return E_NOTIMPL; }
