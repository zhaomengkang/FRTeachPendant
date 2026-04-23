# FR_TeachPendantV2026.02.19 Tool Introduction

## Software View
<img width="437" height="500" alt="image" src="https://github.com/user-attachments/assets/2f88398f-fecd-43e8-ab5e-f7585257a401" />

## User Guide

### 1. Software Dependencies
- **iPendant Controls**
- **VC Runtime Libraries**
  - Microsoft Visual C++ 2008 Redistributable (X86)
  - Microsoft Visual C++ 2013 Redistributable (X86)

### 2. Password Protection
If password protection is enabled, you will need to enter the **installation-level username and password** when connecting.

### 3. Drag and Drop Files to the “Upload Area”
You can drag files such as **PC, VR, TP, and LS** into the **“Upload Area”** to upload programs to the controller.  
> **Note:** This operation should be performed in **T1 mode**.

### 4. Low Resolution May Prevent the Teach Pendant Screen from Displaying Properly
Please ensure that you are using a **high-resolution display** and that the **screen scaling is set to 100%**.

### 5. Connection Status: “Controller is Registering”
If the connection status shows **“Controller is registering”**, try the following:

1. **Do not connect through a switch** — connect the PC directly to the robot controller.
2. **Check the local firewall settings** — disable the firewall if it is blocking the connection.
3. **Adjust the screen resolution** — a low resolution may cause display issues.
4. **Manually allow the application through the firewall** — this app cannot be added automatically to the “Allow an app through Windows Firewall” list during installation.
5. **Reinstall Microsoft Visual C++ 2008 (X86)** if it has been uninstalled.
6. **Reinstall this tool** if `fripendant.ocx` or `frinppwnd.dll` has been deleted.
