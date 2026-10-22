#pragma once
#include "Resource.h"

class CFRTPApp : public CWinApp
{
public:
    CFRTPApp() = default;
    BOOL InitInstance() override;
    int ExitInstance() override;
    DECLARE_MESSAGE_MAP()
};

extern CFRTPApp theApp;
