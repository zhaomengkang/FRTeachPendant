#include "pch.h"
#include "WebView2Host.h"

using namespace Microsoft::WRL;

void CWebView2Host::Init(HWND parent, const CRect& bounds, const std::wstring& userDataFolder, double zoom,
                         std::function<void(bool)> onReady)
{
    m_bounds = bounds;
    m_zoom = zoom;
    auto alive = m_alive;

    HRESULT hr = CreateCoreWebView2EnvironmentWithOptions(
        nullptr, userDataFolder.c_str(), nullptr,
        Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>(
            [this, alive, parent, onReady](HRESULT res, ICoreWebView2Environment* env) -> HRESULT
            {
                if (!*alive) return S_OK;
                if (FAILED(res) || !env)
                {
                    if (onReady) onReady(false);
                    return S_OK;
                }
                m_env = env;
                HRESULT h2 = env->CreateCoreWebView2Controller(
                    parent,
                    Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>(
                        [this, alive, onReady](HRESULT r2, ICoreWebView2Controller* ctl) -> HRESULT
                        {
                            if (!*alive) return S_OK;
                            if (FAILED(r2) || !ctl)
                            {
                                if (onReady) onReady(false);
                                return S_OK;
                            }
                            m_controller = ctl;
                            HRESULT setupResult = m_controller->get_CoreWebView2(&m_view);
                            if (SUCCEEDED(setupResult) && !m_view)
                                setupResult = E_FAIL;
                            if (SUCCEEDED(setupResult))
                                setupResult = m_controller->put_Bounds(m_bounds);
                            if (SUCCEEDED(setupResult))
                                setupResult = m_controller->put_ZoomFactor(m_zoom);
                            if (SUCCEEDED(setupResult))
                                setupResult = m_controller->put_IsVisible(m_visible ? TRUE : FALSE);
                            if (FAILED(setupResult))
                            {
                                m_controller->Close();
                                m_controller.Reset();
                                m_view.Reset();
                                if (onReady) onReady(false);
                                return S_OK;
                            }
                            if (!m_pendingUrl.empty() && m_view)
                            {
                                setupResult = m_view->Navigate(m_pendingUrl.c_str());
                                if (FAILED(setupResult))
                                {
                                    m_controller->Close();
                                    m_controller.Reset();
                                    m_view.Reset();
                                    if (onReady) onReady(false);
                                    return S_OK;
                                }
                            }
                            if (onReady) onReady(true);
                            return S_OK;
                        }).Get());
                if (FAILED(h2) && onReady) onReady(false);
                return S_OK;
            }).Get());

    if (FAILED(hr) && onReady)
        onReady(false);
}

void CWebView2Host::Navigate(const std::wstring& url)
{
    m_pendingUrl = url;
    if (m_view)
        m_view->Navigate(url.c_str());
}

void CWebView2Host::SetBounds(const CRect& bounds)
{
    m_bounds = bounds;
    if (m_controller)
        m_controller->put_Bounds(bounds);
}

void CWebView2Host::SetVisible(bool visible)
{
    m_visible = visible;
    if (m_controller)
        m_controller->put_IsVisible(visible ? TRUE : FALSE);
}

void CWebView2Host::Close()
{
    *m_alive = false;
    m_alive = std::make_shared<bool>(true);
    if (m_controller)
    {
        m_controller->Close();
        m_controller.Reset();
    }
    m_view.Reset();
    m_env.Reset();
}
