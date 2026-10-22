#pragma once
#include <string>

namespace FRLoadUserPC
{
    // Uploads the KAREL server program if the controller's library version differs from the app title.
    // Throws AppError on failure.
    bool LoadMKWebServer(const std::wstring& host, int controllerType, const std::wstring& user,
                         const std::wstring& pass, const std::wstring& appTitle);
    bool DeleteSimKey(const std::wstring& host, const std::wstring& user, const std::wstring& pass);
}
