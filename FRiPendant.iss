#define MyAppName "FRiPendant"
#define MyAppVersion "2026.07.22"
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

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"

[Code]
const
  TypeLibKey1 = 'TypeLib\{34F4C4DB-A64B-4D87-99DA-042F7FB7DEBA}';
  TypeLibKey2 = 'TypeLib\{71060659-0E45-11D3-81B6-0000E206D650}';
  TypeLibKey3 = 'TypeLib\{F8A2CDB9-DC5A-49D2-90D1-559CAB110FFA}';

function IsOCXRegistered(const TypeLibKey: String): Boolean;
var
  Win32Path: String;
begin
  if not RegKeyExists(HKCR, TypeLibKey) then
  begin
    Result := False;
    Exit;
  end;

  if not RegQueryStringValue(HKCR, TypeLibKey + '\1.0\0\win32', '', Win32Path) then
  begin
    Result := False;
    Exit;
  end;

  Result := (Win32Path <> '') and FileExists(Win32Path);
end;

procedure InstallVC2008IfNeeded();
var
  ResultCode: Integer;
  VCRedistExe: String;
  ExecResult: Boolean;
begin
  VCRedistExe := ExpandConstant('{app}\support\VC2008\vcredist_x86.exe');
  ExecResult := Exec(VCRedistExe, '/q /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if not ExecResult or (ResultCode <> 0) then
    MsgBox('VC++ 2008 Redistributable installation failed, error code: ' + IntToStr(ResultCode), mbError, MB_OK);
end;

procedure InstallVC2013IfNeeded();
var
  ResultCode: Integer;
  VCRedistExe: String;
  ExecResult: Boolean;
begin
  VCRedistExe := ExpandConstant('{app}\support\VC2013\vcredist_x86.exe');
  ExecResult := Exec(VCRedistExe, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if not ExecResult or (ResultCode <> 0) then
    MsgBox('VC++ 2013 Redistributable installation failed, error code: ' + IntToStr(ResultCode), mbError, MB_OK);
end;

procedure RegisterOCXIfNeeded(const OCXPath: String; const TypeLibKey: String);
var
  ResultCode: Integer;
begin
  if (not IsOCXRegistered(TypeLibKey)) or (not FileExists(OCXPath)) then
  begin
    if not Exec('regsvr32.exe', '/s "' + OCXPath + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      MsgBox('Failed to execute regsvr32 to register OCX: ' + OCXPath, mbError, MB_OK)
    else if ResultCode <> 0 then
      MsgBox('OCX registration failed with error code: ' + IntToStr(ResultCode), mbError, MB_OK);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  OCXPath1, OCXPath2, OCXPath3: String;
  VCPath: String;
begin
  if CurStep = ssPostInstall then
  begin
    InstallVC2008IfNeeded();
    InstallVC2013IfNeeded();

    OCXPath1 := ExpandConstant('{app}\support\UIF\fripendant.ocx');
    RegisterOCXIfNeeded(OCXPath1, TypeLibKey1);

    OCXPath2 := ExpandConstant('{app}\support\UIF\fripcontrols.ocx');
    RegisterOCXIfNeeded(OCXPath2, TypeLibKey2);

    OCXPath3 := ExpandConstant('{app}\support\UIF\frtreeview.ocx');
    RegisterOCXIfNeeded(OCXPath3, TypeLibKey3);

    WizardForm.StatusLabel.Caption := 'Cleaning up VC redistributable folder...';
    WizardForm.Refresh;
    Sleep(1000);

    VCPath := ExpandConstant('{app}\support\VC2008');
    if DirExists(VCPath) then
      DelTree(VCPath, True, True, True);

    VCPath := ExpandConstant('{app}\support\VC2013');
    if DirExists(VCPath) then
      DelTree(VCPath, True, True, True);

    WizardForm.StatusLabel.Caption := 'Installation completed.';
    WizardForm.Refresh;
    Sleep(1000);
  end;
end;

[Run]
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""FRiPendant"" program=""{app}\bin\{#MyAppExeName}"" dir=in"; Flags: runhidden waituntilterminated
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""FRiPendant"" dir=in action=allow program=""{app}\bin\{#MyAppExeName}"" enable=yes profile=domain,private,public"; Flags: runhidden waituntilterminated
Filename: "{app}\bin\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"