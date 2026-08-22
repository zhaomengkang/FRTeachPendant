#define MyAppName "FRiPendant"
#define MyAppVersion "2026.08.22"
#define MyAppPublisher "Zhao,Mengkang"
#define MyAppExeName "FRiPendant.exe"

[Setup]
AppId={{267B176E-6D8C-431C-B4FF-D1D9BDF12ADD}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\FRiPendant
DisableDirPage=yes
UninstallDisplayIcon={app}\bin\{#MyAppExeName}
DefaultGroupName=FRiPendant
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=Release
OutputBaseFilename=FRiPendantInstallV{#MyAppVersion}
SolidCompression=yes
WizardStyle=classic
SetupIconFile=icon\FRiPendant.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "FRTeachPendant\bin\Release\*"; DestDir: "{app}\bin"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Support\UIF\*"; DestDir: "{app}\Support\UIF"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "KarelProject\release\*"; DestDir: "{app}\bin\KAREL"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Support\VC\VC2008\*"; DestDir: "{app}\Support\VC2008"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Support\VC\VC2013\*"; DestDir: "{app}\Support\VC2013"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Support\webview2\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{app}\Support\webview2"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"

[Code]

const
  { OCX type library registry keys }
  TypeLibKey1 = 'TypeLib\{34F4C4DB-A64B-4D87-99DA-042F7FB7DEBA}';
  TypeLibKey2 = 'TypeLib\{71060659-0E45-11D3-81B6-0000E206D650}';
  TypeLibKey3 = 'TypeLib\{F8A2CDB9-DC5A-49D2-90D1-559CAB110FFA}';

  { Minimum required major version for OCX components }
  OcxVersionRequired = 10;

  { Microsoft Edge WebView2 Runtime client ID }
  WebView2ClientId = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';

var
  OcxForceRegPage: TInputOptionWizardPage;
  OcxCheckIndex1: Integer;
  OcxCheckIndex2: Integer;
  OcxCheckIndex3: Integer;

  { Indicates whether a fatal installation error occurred }
  InstallationFailed: Boolean;


{-------------------------------------------------------------------------------
  Returns the registered file path of an OCX component.
-------------------------------------------------------------------------------}
function GetRegisteredOcxPath(const TypeLibKey: String): String;
begin
  Result := '';

  if RegQueryStringValue(
       HKCR,
       TypeLibKey + '\1.0\0\win32',
       '',
       Result) then
  begin
    { Ignore registry entries that point to a missing file }
    if not FileExists(Result) then
      Result := '';
  end;
end;


{-------------------------------------------------------------------------------
  Checks whether an OCX component is registered and its file exists.
-------------------------------------------------------------------------------}
function IsOcxRegistered(const TypeLibKey: String): Boolean;
begin
  Result := GetRegisteredOcxPath(TypeLibKey) <> '';
end;


{-------------------------------------------------------------------------------
  Reads the major version number from a file version resource.
-------------------------------------------------------------------------------}
function GetFileMajorVersion(
  const FilePath: String;
  var MajorVersion: Cardinal): Boolean;
var
  VersionString: String;
  MajorString: String;
  DotPosition: Integer;
  VersionNumber: Integer;
begin
  Result := False;
  MajorVersion := 0;

  if not FileExists(FilePath) then
    Exit;

  if not GetVersionNumbersString(FilePath, VersionString) then
    Exit;

  if VersionString = '' then
    Exit;

  DotPosition := Pos('.', VersionString);

  if DotPosition > 1 then
    MajorString := Copy(VersionString, 1, DotPosition - 1)
  else
    MajorString := VersionString;

  VersionNumber := StrToIntDef(MajorString, -1);

  if VersionNumber < 0 then
    Exit;

  MajorVersion := Cardinal(VersionNumber);
  Result := True;
end;


{-------------------------------------------------------------------------------
  Creates the OCX force-registration page only when an older OCX version
  is already registered on the computer.
-------------------------------------------------------------------------------}
procedure InitializeWizard();
var
  MajorVersion: Cardinal;
  RegisteredPath: String;
  NeedOcxPage: Boolean;
begin
  { Assume success until a fatal error occurs }
  InstallationFailed := False;

  OcxCheckIndex1 := -1;
  OcxCheckIndex2 := -1;
  OcxCheckIndex3 := -1;
  NeedOcxPage := False;

  { Check fripendant.ocx }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey1);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  { Check fripcontrols.ocx }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey2);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  { Check frtreeview.ocx }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey3);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  { Do not display the page if no outdated OCX was found }
  if not NeedOcxPage then
    Exit;

  OcxForceRegPage := CreateInputOptionPage(
    wpSelectComponents,
    'OCX Component Registration',
    'Some registered OCX components have older versions.',
    'Select the components that should be registered again.',
    False,
    False);

  { Add fripendant.ocx to the selection page }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey1);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
  begin
    { Use string concatenation instead of Format(..., [MajorVersion]) }
    OcxCheckIndex1 := OcxForceRegPage.Add(
      'Force register fripendant.ocx (current version: ' +
      IntToStr(MajorVersion) + '.x)');

    OcxForceRegPage.Values[OcxCheckIndex1] := True;
  end;

  { Add fripcontrols.ocx to the selection page }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey2);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
  begin
    { Use string concatenation instead of Format(..., [MajorVersion]) }
    OcxCheckIndex2 := OcxForceRegPage.Add(
      'Force register fripcontrols.ocx (current version: ' +
      IntToStr(MajorVersion) + '.x)');

    OcxForceRegPage.Values[OcxCheckIndex2] := True;
  end;

  { Add frtreeview.ocx to the selection page }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey3);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
  begin
    { Use string concatenation instead of Format(..., [MajorVersion]) }
    OcxCheckIndex3 := OcxForceRegPage.Add(
      'Force register frtreeview.ocx (current version: ' +
      IntToStr(MajorVersion) + '.x)');

    OcxForceRegPage.Values[OcxCheckIndex3] := True;
  end;
end;


{-------------------------------------------------------------------------------
  Marks the installation as failed.
-------------------------------------------------------------------------------}
procedure MarkInstallationAsFailed();
begin
  InstallationFailed := True;
end;


{-------------------------------------------------------------------------------
  Returns True if the installation completed successfully.
-------------------------------------------------------------------------------}
function IsInstallationSuccessful(): Boolean;
begin
  Result := not InstallationFailed;
end;


{-------------------------------------------------------------------------------
  Skips the Finished page after a fatal installation error.
-------------------------------------------------------------------------------}
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;

  { Do not show the final launch page after installation failure }
  if (PageID = wpFinished) and InstallationFailed then
    Result := True;
end;


{-------------------------------------------------------------------------------
  Returns True if the specified OCX checkbox is selected.
-------------------------------------------------------------------------------}
function IsForceChecked(CheckIndex: Integer): Boolean;
begin
  Result :=
    (OcxForceRegPage <> nil) and
    (CheckIndex >= 0) and
    OcxForceRegPage.Values[CheckIndex];
end;


{-------------------------------------------------------------------------------
  Cleans up files created during a failed installation.

  Important:
  OCX registration is intentionally NOT reversed.
  Registered OCX components remain registered.
-------------------------------------------------------------------------------}
procedure CleanupFailedInstallation();
var
  AppPath: String;
  DesktopShortcut: String;
  StartMenuShortcut: String;
  UninstallShortcut: String;
  ResultCode: Integer;
begin
  AppPath := ExpandConstant('{app}');

  { Remove the firewall rule created by this installer }
  Exec(
    'netsh.exe',
    'advfirewall firewall delete rule name="FRiPendant"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);

  { Remove the desktop shortcut }
  DesktopShortcut := ExpandConstant(
    '{autodesktop}\{#MyAppName}.lnk');

  if FileExists(DesktopShortcut) then
    DeleteFile(DesktopShortcut);

  { Remove the Start Menu application shortcut }
  StartMenuShortcut := ExpandConstant(
    '{group}\{#MyAppName}.lnk');

  if FileExists(StartMenuShortcut) then
    DeleteFile(StartMenuShortcut);

  { Remove the Start Menu uninstall shortcut }
  UninstallShortcut := ExpandConstant(
    '{group}\{cm:UninstallProgram,{#MyAppName}}.lnk');

  if FileExists(UninstallShortcut) then
    DeleteFile(UninstallShortcut);

  { Remove all files and subdirectories copied to the installation directory }
  if DirExists(AppPath) then
    DelTree(AppPath, True, True, True);
end;


{-------------------------------------------------------------------------------
  Installs the Microsoft Visual C++ 2008 Redistributable package.
-------------------------------------------------------------------------------}
procedure InstallVC2008IfNeeded();
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  InstallerPath := ExpandConstant(
    '{app}\Support\VC2008\vcredist_x86.exe');

  { Continue if the installer is not included }
  if not FileExists(InstallerPath) then
    Exit;

  if not Exec(
       InstallerPath,
       '/q /norestart',
       '',
       SW_HIDE,
       ewWaitUntilTerminated,
       ResultCode) then
  begin
    MsgBox(
      'Failed to start the VC++ 2008 Redistributable installer.',
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  { 3010 means success with reboot required }
  if (ResultCode <> 0) and (ResultCode <> 3010) then
  begin
    MsgBox(
      'VC++ 2008 Redistributable installation failed. Error code: ' +
      IntToStr(ResultCode),
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;
end;


{-------------------------------------------------------------------------------
  Installs the Microsoft Visual C++ 2013 Redistributable package.
-------------------------------------------------------------------------------}
procedure InstallVC2013IfNeeded();
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  InstallerPath := ExpandConstant(
    '{app}\Support\VC2013\vcredist_x86.exe');

  { Continue if the installer is not included }
  if not FileExists(InstallerPath) then
    Exit;

  if not Exec(
       InstallerPath,
       '/install /quiet /norestart',
       '',
       SW_HIDE,
       ewWaitUntilTerminated,
       ResultCode) then
  begin
    MsgBox(
      'Failed to start the VC++ 2013 Redistributable installer.',
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  { 3010 means success with reboot required }
  if (ResultCode <> 0) and (ResultCode <> 3010) then
  begin
    MsgBox(
      'VC++ 2013 Redistributable installation failed. Error code: ' +
      IntToStr(ResultCode),
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;
end;


{-------------------------------------------------------------------------------
  Checks whether the Microsoft Edge WebView2 Runtime is installed.

  The following registry locations are checked:
  - 64-bit machine scope;
  - 32-bit machine scope;
  - Current-user scope.
-------------------------------------------------------------------------------}
function IsWebView2RuntimeInstalled(): Boolean;
var
  Version: String;
begin
  Result := False;

  { Check the 64-bit machine registry }
  if RegQueryStringValue(
       HKLM64,
       'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + WebView2ClientId,
       'pv',
       Version) then
  begin
    Result := (Version <> '') and (Version <> '0.0.0.0');

    if Result then
      Exit;
  end;

  { Check the 32-bit machine registry }
  if RegQueryStringValue(
       HKLM32,
       'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + WebView2ClientId,
       'pv',
       Version) then
  begin
    Result := (Version <> '') and (Version <> '0.0.0.0');

    if Result then
      Exit;
  end;

  { Check the current-user registry }
  if RegQueryStringValue(
       HKCU,
       'Software\Microsoft\EdgeUpdate\Clients\' + WebView2ClientId,
       'pv',
       Version) then
  begin
    Result := (Version <> '') and (Version <> '0.0.0.0');
  end;
end;


{-------------------------------------------------------------------------------
  Installs WebView2 if it is not already installed.

  Any WebView2 installation failure triggers cleanup of:
  - Application files;
  - Desktop shortcut;
  - Start Menu shortcuts;
  - Firewall rule.

  OCX registration is not undone.
-------------------------------------------------------------------------------}
procedure InstallWebView2IfNeeded();
var
  InstallerPath: String;
  ResultCode: Integer;
begin
  { Skip installation if a valid WebView2 Runtime is already installed }
  if IsWebView2RuntimeInstalled() then
    Exit;

  InstallerPath := ExpandConstant(
    '{app}\Support\webview2\MicrosoftEdgeWebview2Setup.exe');

  if not FileExists(InstallerPath) then
  begin
    MsgBox(
      'The WebView2 Runtime installer was not found:' + #13#10 +
      InstallerPath,
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  WizardForm.StatusLabel.Caption :=
    'Installing Microsoft Edge WebView2 Runtime...';
  WizardForm.Refresh;

  if not Exec(
       InstallerPath,
       '',
       '',
       SW_SHOWNORMAL,
       ewWaitUntilTerminated,
       ResultCode) then
  begin
    MsgBox(
      'Failed to start the WebView2 Runtime installer.',
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  { 3010 means success with reboot required }
  if (ResultCode <> 0) and (ResultCode <> 3010) then
  begin
    MsgBox(
      'WebView2 Runtime installation failed. Error code: ' +
      IntToStr(ResultCode),
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  { Verify that WebView2 was actually installed }
  if not IsWebView2RuntimeInstalled() then
  begin
    MsgBox(
      'WebView2 Runtime installation could not be verified.',
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;
end;


{-------------------------------------------------------------------------------
  Registers an OCX component when required.

  ForceRegister=True:
    Always register the OCX if the file exists.

  ForceRegister=False:
    Register only if the OCX is not currently registered or its file is missing.

  Important:
  OCX registration is not removed during failure cleanup.
-------------------------------------------------------------------------------}
procedure RegisterOcxIfNeeded(
  const OcxPath: String;
  const TypeLibKey: String;
  ForceRegister: Boolean);
var
  ResultCode: Integer;
  ShouldRegister: Boolean;
begin
  if ForceRegister then
    ShouldRegister := FileExists(OcxPath)
  else
    ShouldRegister :=
      (not IsOcxRegistered(TypeLibKey)) or
      (not FileExists(OcxPath));

  if not ShouldRegister then
    Exit;

  if not Exec(
       'regsvr32.exe',
       '/s "' + OcxPath + '"',
       '',
       SW_HIDE,
       ewWaitUntilTerminated,
       ResultCode) then
  begin
    MsgBox(
      'Failed to start regsvr32.exe for:' + #13#10 +
      OcxPath,
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  if ResultCode <> 0 then
  begin
    MsgBox(
      'OCX registration failed. Error code: ' +
      IntToStr(ResultCode) + #13#10 +
      OcxPath,
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;
end;


{-------------------------------------------------------------------------------
  Performs post-installation tasks:
  1. Install WebView2.
  2. Install VC++ 2008.
  3. Install VC++ 2013.
  4. Register OCX components.
  5. Remove temporary runtime installer files.
-------------------------------------------------------------------------------}
procedure CurStepChanged(CurStep: TSetupStep);
var
  OcxPath1: String;
  OcxPath2: String;
  OcxPath3: String;
  CleanupPath: String;
begin
  if CurStep <> ssPostInstall then
    Exit;

  { Install required runtime dependencies }
  InstallWebView2IfNeeded();
  InstallVC2008IfNeeded();
  InstallVC2013IfNeeded();

  { Register fripendant.ocx }
  OcxPath1 := ExpandConstant(
    '{app}\Support\UIF\fripendant.ocx');

  RegisterOcxIfNeeded(
    OcxPath1,
    TypeLibKey1,
    IsForceChecked(OcxCheckIndex1));

  { Register fripcontrols.ocx }
  OcxPath2 := ExpandConstant(
    '{app}\Support\UIF\fripcontrols.ocx');

  RegisterOcxIfNeeded(
    OcxPath2,
    TypeLibKey2,
    IsForceChecked(OcxCheckIndex2));

  { Register frtreeview.ocx }
  OcxPath3 := ExpandConstant(
    '{app}\Support\UIF\frtreeview.ocx');

  RegisterOcxIfNeeded(
    OcxPath3,
    TypeLibKey3,
    IsForceChecked(OcxCheckIndex3));

  { Remove temporary runtime installer files }
  WizardForm.StatusLabel.Caption :=
    'Cleaning up runtime installer files...';
  WizardForm.Refresh;

  CleanupPath := ExpandConstant('{app}\Support\VC2008');

  if DirExists(CleanupPath) then
    DelTree(CleanupPath, True, True, True);

  CleanupPath := ExpandConstant('{app}\Support\VC2013');

  if DirExists(CleanupPath) then
    DelTree(CleanupPath, True, True, True);

  CleanupPath := ExpandConstant('{app}\Support\webview2');

  if DirExists(CleanupPath) then
    DelTree(CleanupPath, True, True, True);

  WizardForm.StatusLabel.Caption :=
    'Installation completed.';
  WizardForm.Refresh;
end;


[Run]
; Remove an existing firewall rule before creating a new one
Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall delete rule name=""FRiPendant"" program=""{app}\bin\{#MyAppExeName}"" dir=in"; \
  Flags: runhidden waituntilterminated

; Allow FRiPendant to receive inbound network connections
Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall add rule name=""FRiPendant"" dir=in action=allow program=""{app}\bin\{#MyAppExeName}"" enable=yes profile=domain,private,public"; \
  Flags: runhidden waituntilterminated

; Show the launch option only after a successful installation
Filename: "{app}\bin\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent; \
  Check: IsInstallationSuccessful


[UninstallRun]
; Remove all firewall rules named FRiPendant during uninstallation
Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall delete rule name=""FRiPendant"""; \
  Flags: runhidden waituntilterminated; \
  RunOnceId: "RemoveFRiPendantFirewallRule"


[UninstallDelete]
; Remove the complete application directory during uninstallation
Type: filesandordirs; Name: "{app}"