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
  TypeLibKey1 = 'TypeLib\{34F4C4DB-A64B-4D87-99DA-042F7FB7DEBA}';
  TypeLibKey2 = 'TypeLib\{71060659-0E45-11D3-81B6-0000E206D650}';
  TypeLibKey3 = 'TypeLib\{F8A2CDB9-DC5A-49D2-90D1-559CAB110FFA}';

  OcxVersionRequired = 10;

  WebView2ClientId = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';

  VC2008ProductCode = '{9BE518E6-ECC6-35A9-88E4-87755C07200F}';
  VC2013ProductCode = '{13A4EE12-23EA-3371-91EE-EFB36DDFFF3E}';

var
  OcxForceRegPage: TInputOptionWizardPage;
  OcxCheckIndex1: Integer;
  OcxCheckIndex2: Integer;
  OcxCheckIndex3: Integer;
  InstallationFailed: Boolean;


function GetRegisteredOcxPath(const TypeLibKey: String): String;
begin
  Result := '';

  if RegQueryStringValue(
       HKCR,
       TypeLibKey + '\1.0\0\win32',
       '',
       Result) then
  begin
    if not FileExists(Result) then
      Result := '';
  end;
end;


function IsOcxRegistered(const TypeLibKey: String): Boolean;
begin
  Result := GetRegisteredOcxPath(TypeLibKey) <> '';
end;


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

  RegisteredPath := GetRegisteredOcxPath(TypeLibKey1);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  RegisteredPath := GetRegisteredOcxPath(TypeLibKey2);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  RegisteredPath := GetRegisteredOcxPath(TypeLibKey3);

  if (RegisteredPath <> '') and
     GetFileMajorVersion(RegisteredPath, MajorVersion) and
     (MajorVersion < OcxVersionRequired) then
    NeedOcxPage := True;

  if not NeedOcxPage then
    Exit;

  OcxForceRegPage := CreateInputOptionPage(
    wpSelectComponents,
    'OCX Component Registration',
    'Some registered OCX components have older versions.',
    'Select the components that should be registered again.',
    False,
    False);

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


procedure MarkInstallationAsFailed();
begin
  InstallationFailed := True;
end;


function IsInstallationSuccessful(): Boolean;
begin
  Result := not InstallationFailed;
end;


function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;

  if (PageID = wpFinished) and InstallationFailed then
    Result := True;
end;


function IsForceChecked(CheckIndex: Integer): Boolean;
begin
  Result :=
    (OcxForceRegPage <> nil) and
    (CheckIndex >= 0) and
    OcxForceRegPage.Values[CheckIndex];
end;


procedure CleanupFailedInstallation();
var
  AppPath: String;
  DesktopShortcut: String;
  StartMenuShortcut: String;
  UninstallShortcut: String;
  ResultCode: Integer;
begin
  AppPath := ExpandConstant('{app}');

  Exec(
    'netsh.exe',
    'advfirewall firewall delete rule name="FRiPendant"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);

  DesktopShortcut := ExpandConstant(
    '{autodesktop}\{#MyAppName}.lnk');

  if FileExists(DesktopShortcut) then
    DeleteFile(DesktopShortcut);

  StartMenuShortcut := ExpandConstant(
    '{group}\{#MyAppName}.lnk');

  if FileExists(StartMenuShortcut) then
    DeleteFile(StartMenuShortcut);

  UninstallShortcut := ExpandConstant(
    '{group}\{cm:UninstallProgram,{#MyAppName}}.lnk');

  if FileExists(UninstallShortcut) then
    DeleteFile(UninstallShortcut);

  if DirExists(AppPath) then
    DelTree(AppPath, True, True, True);
end;


function IsProductInstalled(const ProductCode: String): Boolean;
var
  DisplayVersion: String;
begin
  Result := False;

  if RegQueryStringValue(
       HKLM32,
       'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' +
       ProductCode,
       'DisplayVersion',
       DisplayVersion) then
  begin
    Result := DisplayVersion <> '';
    if Result then
      Exit;
  end;

  if RegQueryStringValue(
       HKLM64,
       'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' +
       ProductCode,
       'DisplayVersion',
       DisplayVersion) then
  begin
    Result := DisplayVersion <> '';
    if Result then
      Exit;
  end;

  if RegQueryStringValue(
       HKCU,
       'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
       ProductCode,
       'DisplayVersion',
       DisplayVersion) then
    Result := DisplayVersion <> '';
end;


function IsVC2008Installed(): Boolean;
begin
  Result := IsProductInstalled(VC2008ProductCode);
end;


function IsVC2013Installed(): Boolean;
begin
  Result := IsProductInstalled(VC2013ProductCode);
end;


function IsSuccessfulRuntimeResult(ResultCode: Integer): Boolean;
begin
  Result :=
    (ResultCode = 0) or
    (ResultCode = 3010) or
    (ResultCode = 1638) or
    (ResultCode = 5100);
end;


procedure InstallVC2008IfNeeded();
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  if IsVC2008Installed() then
    Exit;

  InstallerPath := ExpandConstant(
    '{app}\Support\VC2008\vcredist_x86.exe');

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


procedure InstallVC2013IfNeeded();
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  if IsVC2013Installed() then
    Exit;

  InstallerPath := ExpandConstant(
    '{app}\Support\VC2013\vcredist_x86.exe');

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


function IsWebView2RuntimeInstalled(): Boolean;
var
  Version: String;
begin
  Result := False;

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

  if RegQueryStringValue(
       HKCU,
       'Software\Microsoft\EdgeUpdate\Clients\' + WebView2ClientId,
       'pv',
       Version) then
    Result := (Version <> '') and (Version <> '0.0.0.0');
end;


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


procedure CurStepChanged(CurStep: TSetupStep);
var
  OcxPath1: String;
  OcxPath2: String;
  OcxPath3: String;
  CleanupPath: String;
begin
  if CurStep <> ssPostInstall then
    Exit;

  InstallWebView2IfNeeded();
  InstallVC2008IfNeeded();
  InstallVC2013IfNeeded();

  OcxPath1 := ExpandConstant(
    '{app}\Support\UIF\fripendant.ocx');

  RegisterOcxIfNeeded(
    OcxPath1,
    TypeLibKey1,
    IsForceChecked(OcxCheckIndex1));

  OcxPath2 := ExpandConstant(
    '{app}\Support\UIF\fripcontrols.ocx');

  RegisterOcxIfNeeded(
    OcxPath2,
    TypeLibKey2,
    IsForceChecked(OcxCheckIndex2));

  OcxPath3 := ExpandConstant(
    '{app}\Support\UIF\frtreeview.ocx');

  RegisterOcxIfNeeded(
    OcxPath3,
    TypeLibKey3,
    IsForceChecked(OcxCheckIndex3));

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
Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall delete rule name=""FRiPendant"" program=""{app}\bin\{#MyAppExeName}"" dir=in"; \
  Flags: runhidden waituntilterminated

Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall add rule name=""FRiPendant"" dir=in action=allow program=""{app}\bin\{#MyAppExeName}"" enable=yes profile=domain,private,public"; \
  Flags: runhidden waituntilterminated

Filename: "{app}\bin\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent; \
  Check: IsInstallationSuccessful


[UninstallRun]
Filename: "netsh.exe"; \
  Parameters: "advfirewall firewall delete rule name=""FRiPendant"""; \
  Flags: runhidden waituntilterminated; \
  RunOnceId: "RemoveFRiPendantFirewallRule"


[UninstallDelete]
Type: filesandordirs; Name: "{app}"
