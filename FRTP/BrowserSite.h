#pragma once

// Control site for the IE WebBrowser control: suppresses the context menu, scroll bars,
// 3D border and browser accelerators (same behaviour as the WinForms WebBrowser settings).
class CBrowserSite : public COleControlSite, public IDocHostUIHandler
{
public:
    explicit CBrowserSite(COleControlContainer* pCtrlCont) : COleControlSite(pCtrlCont) {}

protected:
    LPUNKNOWN GetInterfaceHook(const void* iid) override;

    STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject) override;
    STDMETHOD_(ULONG, AddRef)() override;
    STDMETHOD_(ULONG, Release)() override;

    STDMETHOD(ShowContextMenu)(DWORD dwID, POINT* ppt, IUnknown* pcmdtReserved, IDispatch* pdispReserved) override;
    STDMETHOD(GetHostInfo)(DOCHOSTUIINFO* pInfo) override;
    STDMETHOD(ShowUI)(DWORD dwID, IOleInPlaceActiveObject* pActiveObject, IOleCommandTarget* pCommandTarget,
                      IOleInPlaceFrame* pFrame, IOleInPlaceUIWindow* pDoc) override;
    STDMETHOD(HideUI)(void) override;
    STDMETHOD(UpdateUI)(void) override;
    STDMETHOD(EnableModeless)(BOOL fEnable) override;
    STDMETHOD(OnDocWindowActivate)(BOOL fActivate) override;
    STDMETHOD(OnFrameWindowActivate)(BOOL fActivate) override;
    STDMETHOD(ResizeBorder)(LPCRECT prcBorder, IOleInPlaceUIWindow* pUIWindow, BOOL fRameWindow) override;
    STDMETHOD(TranslateAccelerator)(LPMSG lpMsg, const GUID* pguidCmdGroup, DWORD nCmdID) override;
    STDMETHOD(GetOptionKeyPath)(LPOLESTR* pchKey, DWORD dw) override;
    STDMETHOD(GetDropTarget)(IDropTarget* pDropTarget, IDropTarget** ppDropTarget) override;
    STDMETHOD(GetExternal)(IDispatch** ppDispatch) override;
    STDMETHOD(TranslateUrl)(DWORD dwTranslate, OLECHAR* pchURLIn, OLECHAR** ppchURLOut) override;
    STDMETHOD(FilterDataObject)(IDataObject* pDO, IDataObject** ppDORet) override;
};
