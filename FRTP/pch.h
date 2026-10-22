#pragma once

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN
#endif

#include "targetver.h"

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS
#define _AFX_ALL_WARNINGS

#include <afxwin.h>
#include <afxext.h>
#include <afxdisp.h>
#include <afxdtctl.h>
#include <afxcmn.h>
#include <afxdlgs.h>
#include <afxole.h>
#include <afxocc.h>
#include <afxcontrolbars.h>
#include <atlbase.h>

#include <winsock2.h>
#include <ws2tcpip.h>
#include <iphlpapi.h>
#include <icmpapi.h>
#include <winhttp.h>
#include <shellapi.h>
#include <shlobj.h>
#include <mshtmhst.h>
#include <exdisp.h>
#include <mshtml.h>

#include <wrl.h>
#include <WebView2.h>

#include <string>
#include <vector>
#include <map>
#include <set>
#include <functional>
#include <thread>
#include <mutex>
#include <atomic>
#include <algorithm>
#include <memory>
#include <cstdint>
