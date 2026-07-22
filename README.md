# Software Help Documentation

Version V2026.07.22 · Author: Zhao Mengkang

## Version Information

- **Software Version:** V2026.07.22
- **Author:** Zhao Mengkang
- **WeChat:** Mengk_964210817
- **QQ:** 964210817
- **Email:** [zhaomengkang@hotmail.com](mailto:zhaomengkang@hotmail.com)
- **GitHub:** [zhaomengkang (赵梦康)](https://github.com/zhaomengkang)

## Software Update

- [TianYi Cloud Download](https://cloud.189.cn/t/biiEJzR3emAf)  
  `https://cloud.189.cn/t/biiEJzR3emAf`
- **Code:** `vpb5`

- [GitHub Releases](https://github.com/zhaomengkang/FRTeachPendant/releases)

## Open Source Project Address

- [zhaomengkang/FRTeachPendant: Fanuc Robot Remote TeachPendant](https://github.com/zhaomengkang/FRTeachPendant)

## Changelog

### 2026.07.22

- Fixed some bugs
- Removed unsupported risk items

### 2026.07.07

- Added support for R-30iA, R-30iB, R-30iB Plus, R-50iA controllers
- Fixed blurry screen display issue
- Redesigned FTP client, switched from webFTP to FTP
- Added program upload restriction: upload only allowed when teach pendant key switch is ON
- Changed the Help documentation display to HTML

### 2026.02.19

- Added password unlock key calculation tool
- Rewrote KAREL program
- Redesigned UI layout

### 2025.07.05

- Project launched for the first time; this tool supports remote teach pendant access

## Instructions

1. **Software Dependencies**
   - iPendant Controls dependency files:
     - `fripendant.ocx`
     - `frinppwnd.dll`
   - VC Runtime:
     - Microsoft Visual C++ 2008 Redistributable (X86)
2. **Password Protection**
   - If password protection is enabled, you must enter a username and password with **INSTALL** privilege level when connecting.

3. **401 (Unauthorized)**
   - KAREL resources need to be manually unlocked. On the teach pendant, go to MENU - Settings - Host Communications - HTTP, find KAREL, and unlock the resource.

4. **File Upload**
   - Drag files (PC / VR / TP / LS) to the **"Upload Area"** to upload them to the controller.

   > **Warning:** During this operation, the teach pendant key switch must be in **ON** position.

5. **Low Resolution Display Issue**
   - Low resolution may prevent the teach pendant screen from displaying properly. Please ensure a high resolution is used and display scaling is set to **100%**.

6. **Connection Display - Controller is Being Registered**
   - If you encounter this issue, please try the following solutions in order:
     1. Do not connect through a network switch. Connect the PC directly to the robot controller.
     2. A local firewall may be blocking the connection. Please disable the firewall.
     3. The screen resolution may be too low. Adjust it to an appropriate resolution.
     4. This application cannot be added to "Allow an app through firewall" during installation. Please add it manually.
     5. Microsoft Visual C++ 2008 (X86) may have been uninstalled. Please reinstall this tool.
     6. `fripendant.ocx` or `frinppwnd.dll` may have been deleted. Please reinstall this tool.


```Text
License
┌──────────────────────────────────────────────────────────────┐
│                    FRTeachPendant                            │
│                    Author: Zhao Mengkang                     │
│              Email: zhaomengkang@hotmail.com                 │
│           GitHub: https://github.com/zhaomengkang/           │
└──────────────────────────────────────────────────────────────┘

Copyright (c) 2025-2026 Zhao Mengkang

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

```
	