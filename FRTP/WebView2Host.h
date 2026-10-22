#pragma once

// Thin wrapper around a WebView2 controller hosted inside an existing window.
class CWebView2Host
{
public:
    CWebView2Host() = default;
    ~CWebView2Host() { Close(); }

    // Starts asynchronous creation. onReady(true/false) is invoked on the UI thread.
    void Init(HWND parent, const CRect& bounds, const std::wstring& userDataFolder, double zoom,
              std::function<void(bool)> onReady = nullptr);
    void Navigate(const std::wstring& url);
    void SetBounds(const CRect& bounds);
    void SetVisible(bool visible);
    bool IsReady() const { return m_controller != nullptr; }
    void Close();

private:
    Microsoft::WRL::ComPtr<ICoreWebView2Environment> m_env;
    Microsoft::WRL::ComPtr<ICoreWebView2Controller> m_controller;
    Microsoft::WRL::ComPtr<ICoreWebView2> m_view;
    std::wstring m_pendingUrl;
    CRect m_bounds;
    bool m_visible = true;
    double m_zoom = 1.0;
    std::shared_ptr<bool> m_alive = std::make_shared<bool>(true);
};
