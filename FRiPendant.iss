#define MyAppName "FRiPendant"
#define MyAppVersion "2026.08.22"
#define MyAppPublisher "Zhao,Mengkang"
#define MyAppExeName "FRiPendant.exe"

[Setup]
AppId={{267B176E-6D8C-431C-B4FF-D1D9BDF12ADD}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

; Install the application for all users.
DefaultDirName={autopf}\FRiPendant
PrivilegesRequired=admin

DisableDirPage=yes
DisableProgramGroupPage=yes
DefaultGroupName=FRiPendant

UninstallDisplayIcon={app}\bin\{#MyAppExeName}

OutputDir=Release
OutputBaseFilename=FRiPendantInstallV{#MyAppVersion}

SolidCompression=yes
WizardStyle=classic
SetupIconFile=icon\FRiPendant.ico

; Try to close the running application before replacing files.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Main application files.
Source: "FRTeachPendant\bin\Release\*"; \
  DestDir: "{app}\bin"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

; OCX components and related UI files.
Source: "Support\UIF\*"; \
  DestDir: "{app}\Support\UIF"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

; KAREL project files.
Source: "KarelProject\release\*"; \
  DestDir: "{app}\bin\KAREL"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

; VC++ 2008 Redistributable installer.
Source: "Support\VC\VC2008\*"; \
  DestDir: "{app}\Support\VC2008"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

; VC++ 2013 Redistributable installer.
Source: "Support\VC\VC2013\*"; \
  DestDir: "{app}\Support\VC2013"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

; Microsoft Edge WebView2 Runtime installer.
Source: "Support\webview2\MicrosoftEdgeWebview2Setup.exe"; \
  DestDir: "{app}\Support\webview2"; \
  Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; \
  Filename: "{app}\bin\{#MyAppExeName}"

Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; \
  Filename: "{uninstallexe}"

Name: "{autodesktop}\{#MyAppName}"; \
  Filename: "{app}\bin\{#MyAppExeName}"

[Code]

const
  { OCX type library registry keys. }
  TypeLibKey1 = 'TypeLib\{34F4C4DB-A64B-4D87-99DA-042F7FB7DEBA}';
  TypeLibKey2 = 'TypeLib\{71060659-0E45-11D3-81B6-0000E206D650}';
  TypeLibKey3 = 'TypeLib\{F8A2CDB9-DC5A-49D2-90D1-559CAB110FFA}';

  { Minimum required major version for OCX components. }
  OcxVersionRequired = 10;

  { Microsoft Edge WebView2 Runtime client ID. }
  WebView2ClientId = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';

var
  OcxForceRegPage: TInputOptionWizardPage;

  { Indexes of the OCX checkboxes. }
  OcxCheckIndex1: Integer;
  OcxCheckIndex2: Integer;
  OcxCheckIndex3: Integer;

  { Indicates whether a fatal installation error occurred. }
  InstallationFailed: Boolean;


{ Returns the registered path of an OCX component. }
function GetRegisteredOcxPath(const TypeLibKey: String): String;
begin
  Result := '';

  if RegQueryStringValue(
       HKCR,
       TypeLibKey + '\1.0\0\win32',
       '',
       Result) then
  begin
    { Ignore registry entries that point to a missing file. }
    if not FileExists(Result) then
      Result := '';
  end;
end;


{ Returns True when the OCX is registered and the registered file exists. }
function IsOcxRegistered(const TypeLibKey: String): Boolean;
begin
  Result := GetRegisteredOcxPath(TypeLibKey) <> '';
end;


{ Reads the major version from a file version resource. }
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


{ Creates the OCX re-registration page when an older OCX version is detected. }
procedure InitializeWizard();
var
  MajorVersion: Cardinal;
  RegisteredPath: String;
  NeedOcxPage: Boolean;
begin
  InstallationFailed := False;

  OcxCheckIndex1 := -1;
  OcxCheckIndex2 := -1;
  OcxCheckIndex3 := -1;
  NeedOcxPage := False;

  { Check the first OCX component. }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey1);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  { Check the second OCX component. }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey2);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  { Check the third OCX component. }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey3);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  { Do not create the page when no old OCX version was found. }
  if not NeedOcxPage then
    Exit;

  OcxForceRegPage := CreateInputOptionPage(
    wpSelectComponents,
    'OCX Component Registration',
    'Some registered OCX components have older versions.',
    'Select the components that should be registered again.',
    False,
    False);

  { Add the first OCX option. }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey1);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
  begin
    OcxCheckIndex1 := OcxForceRegPage.Add(
      'Force register fripendant.ocx (current version: ' +
      IntToStr(MajorVersion) + '.x)');

    OcxForceRegPage.Values[OcxCheckIndex1] := True;
  end;

  { Add the second OCX option. }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey2);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
  begin
    OcxCheckIndex2 := OcxForceRegPage.Add(
      'Force register fripcontrols.ocx (current version: ' +
      IntToStr(MajorVersion) + '.x)');

    OcxForceRegPage.Values[OcxCheckIndex2] := True;
  end;

  { Add the third OCX option. }
  RegisteredPath := GetRegisteredOcxPath(TypeLibKey3);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
  begin
    OcxCheckIndex3 := OcxForceRegPage.Add(
      'Force register frtreeview.ocx (current version: ' +
      IntToStr(MajorVersion) + '.x)');

    OcxForceRegPage.Values[OcxCheckIndex3] := True;
  end;
end;


{ Marks the installation as failed. }
procedure MarkInstallationAsFailed();
begin
  InstallationFailed := True;
end;


{ Returns True only when no fatal installation error occurred. }
function IsInstallationSuccessful(): Boolean;
begin
  Result := not InstallationFailed;
end;


{ Hides the Finished page after a fatal installation error. }
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;

  if (PageID = wpFinished) and InstallationFailed then
    Result := True;
end;


{ Returns True when the specified OCX option is selected. }
function IsForceChecked(CheckIndex: Integer): Boolean;
begin
  Result :=
    (OcxForceRegPage <> nil) and
    (CheckIndex >= 0) and
    OcxForceRegPage.Values[CheckIndex];
end;


{ Removes files, shortcuts, and firewall rules after a failed installation. }
procedure CleanupFailedInstallation();
var
  AppPath: String;
  DesktopShortcut: String;
  StartMenuShortcut: String;
  UninstallShortcut: String;
  ResultCode: Integer;
begin
  AppPath := ExpandConstant('{app}');

  { Remove the firewall rule created by the installer. }
  Exec(
    'netsh.exe',
    'advfirewall firewall delete rule name="FRiPendant"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);

  { Remove the desktop shortcut. }
  DesktopShortcut := ExpandConstant(
    '{autodesktop}\{#MyAppName}.lnk');

  if FileExists(DesktopShortcut) then
    DeleteFile(DesktopShortcut);

  { Remove the Start Menu shortcut. }
  StartMenuShortcut := ExpandConstant(
    '{group}\{#MyAppName}.lnk');

  if FileExists(StartMenuShortcut) then
    DeleteFile(StartMenuShortcut);

  { Remove the uninstaller shortcut. }
  UninstallShortcut := ExpandConstant(
    '{group}\{cm:UninstallProgram,{#MyAppName}}.lnk');

  if FileExists(UninstallShortcut) then
    DeleteFile(UninstallShortcut);

  { Remove the complete application directory. }
  if DirExists(AppPath) then
    DelTree(AppPath, True, True, True);
end;


{ Returns True for successful or acceptable runtime installer results. }
function IsSuccessfulRuntimeResult(ResultCode: Integer): Boolean;
begin
  Result :=
    (ResultCode = 0) or
    (ResultCode = 3010) or
    (ResultCode = 1638) or
    (ResultCode = 5100);
end;


{ Installs VC++ 2008 every time without checking the registry. }
procedure InstallVC2008();
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  InstallerPath := ExpandConstant(
    '{app}\Support\VC2008\vcredist_x86.exe');

  if not FileExists(InstallerPath) then
  begin
    MsgBox(
      'The VC++ 2008 Redistributable installer was not found:' + #13#10 + InstallerPath,
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  WizardForm.StatusLabel.Caption :=
    'Installing Microsoft Visual C++ 2008 Redistributable...';
  WizardForm.Refresh;

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

  if not IsSuccessfulRuntimeResult(ResultCode) then
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


{ Installs VC++ 2013 every time without checking the registry. }
procedure InstallVC2013();
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  InstallerPath := ExpandConstant(
    '{app}\Support\VC2013\vcredist_x86.exe');

  if not FileExists(InstallerPath) then
  begin
    MsgBox(
      'The VC++ 2013 Redistributable installer was not found:' + #13#10 + InstallerPath,
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  WizardForm.StatusLabel.Caption :=
    'Installing Microsoft Visual C++ 2013 Redistributable...';
  WizardForm.Refresh;

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

  if not IsSuccessfulRuntimeResult(ResultCode) then
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


{ Checks whether Microsoft Edge WebView2 Runtime is installed. }
function IsWebView2RuntimeInstalled(): Boolean;
var
  Version: String;
begin
  Result := False;

  { Check the 64-bit machine registry. }
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

  { Check the 32-bit machine registry. }
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

  { Check the current-user registry. }
  if RegQueryStringValue(
       HKCU,
       'Software\Microsoft\EdgeUpdate\Clients\' + WebView2ClientId,
       'pv',
       Version) then
    Result := (Version <> '') and (Version <> '0.0.0.0');
end;


{ Installs and verifies WebView2 when it is not installed. }
procedure InstallWebView2IfNeeded();
var
  InstallerPath: String;
  ResultCode: Integer;
begin
  if IsWebView2RuntimeInstalled() then
    Exit;

  InstallerPath := ExpandConstant(
    '{app}\Support\webview2\MicrosoftEdgeWebview2Setup.exe');

  if not FileExists(InstallerPath) then
  begin
    MsgBox(
      'The WebView2 Runtime installer was not found:' + #13#10 + InstallerPath,
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

  { 0 means success; 3010 means success with reboot required. }
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

  { Verify that WebView2 was actually installed. }
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


{ Registers an OCX when registration is missing or force registration is selected. }
procedure RegisterOcxIfNeeded(
  const OcxPath: String;
  const TypeLibKey: String;
  ForceRegister: Boolean);
var
  ResultCode: Integer;
  ShouldRegister: Boolean;
begin
  if not FileExists(OcxPath) then
  begin
    MsgBox(
      'The OCX file was not found:' + #13#10 + OcxPath,
      mbError,
      MB_OK);

    MarkInstallationAsFailed();
    CleanupFailedInstallation();
    Abort;
  end;

  { Register when forced or when the OCX is not currently registered. }
  ShouldRegister :=
    ForceRegister or
    (not IsOcxRegistered(TypeLibKey));

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


{ Executes runtime installation, OCX registration, and cleanup. }
procedure CurStepChanged(CurStep: TSetupStep);
var
  OcxPath1: String;
  OcxPath2: String;
  OcxPath3: String;
  CleanupPath: String;
begin
  if CurStep <> ssPostInstall then
    Exit;

  { Always install VC++ 2008 and VC++ 2013. }
  InstallVC2008();
  InstallVC2013();

  { Register the first OCX component. }
  OcxPath1 := ExpandConstant(
    '{app}\Support\UIF\fripendant.ocx');

  RegisterOcxIfNeeded(
    OcxPath1,
    TypeLibKey1,
    IsForceChecked(OcxCheckIndex1));

  { Register the second OCX component. }
  OcxPath2 := ExpandConstant(
    '{app}\Support\UIF\fripcontrols.ocx');

  RegisterOcxIfNeeded(
    OcxPath2,
    TypeLibKey2,
    IsForceChecked(OcxCheckIndex2));

  { Register the third OCX component. }
  OcxPath3 := ExpandConstant(
    '{app}\Support\UIF\frtreeview.ocx');

  RegisterOcxIfNeeded(
    OcxPath3,
    TypeLibKey3,
    IsForceChecked(OcxCheckIndex3));
    
  { Install WebView2 only when it is missing. }
  InstallWebView2IfNeeded();

  WizardForm.StatusLabel.Caption :=
    'Cleaning up runtime installer files...';
  WizardForm.Refresh;

  { Remove VC++ 2008 installer files after installation. }
  CleanupPath := ExpandConstant('{app}\Support\VC2008');

  if DirExists(CleanupPath) then
    DelTree(CleanupPath, True, True, True);

  { Remove VC++ 2013 installer files after installation. }
  CleanupPath := ExpandConstant('{app}\Support\VC2013');

  if DirExists(CleanupPath) then
    DelTree(CleanupPath, True, True, True);

  { Remove WebView2 installer files after installation. }
  CleanupPath := ExpandConstant('{app}\Support\webview2');

  if DirExists(CleanupPath) then
    DelTree(CleanupPath, True, True, True);

  WizardForm.StatusLabel.Caption :=
    'Installation completed.';
  WizardForm.Refresh;
end;


[Run]
; Remove any existing firewall rule before creating a new one.
Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall delete rule name=""FRiPendant"" program=""{app}\bin\{#MyAppExeName}"" dir=in"; \
  Flags: runhidden waituntilterminated

; Allow FRiPendant to receive inbound network connections.
Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall add rule name=""FRiPendant"" dir=in action=allow program=""{app}\bin\{#MyAppExeName}"" enable=yes profile=domain,private,public"; \
  Flags: runhidden waituntilterminated

; Launch the application only after a successful installation.
Filename: "{app}\bin\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent; \
  Check: IsInstallationSuccessful


[UninstallRun]
; Remove all firewall rules created for FRiPendant.
Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall delete rule name=""FRiPendant"""; \
  Flags: runhidden waituntilterminated; \
  RunOnceId: "RemoveFRiPendantFirewallRule"


[UninstallDelete]
; Remove the complete application directory during uninstallation.
Type: filesandordirs; Name: "{app}"