#ifndef AppVersion
  #error "AppVersion must come from version.props via scripts/Build-Packages.ps1"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
AppId={{82B72C23-769B-48AD-8174-B5ACED8384E2}
AppName=SoundAnchor
AppVersion={#AppVersion}
AppPublisher=rm968211
AppPublisherURL=https://github.com/rm968211/sound-anchor
DefaultDirName={localappdata}\Programs\SoundAnchor
DefaultGroupName=SoundAnchor
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\packages
OutputBaseFilename=SoundAnchor-{#AppVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\SoundAnchor.exe
; Inno's Win32 resource updater rejects an oversized icon ("File is too large") well below what
; Explorer/taskbar happily display, so assets/icon.ico is kept compact (PIL-optimized, ~55 KB).
SetupIconFile=..\assets\icon.ico
WizardImageFile=..\assets\installer-wizard-large.bmp
WizardSmallImageFile=..\assets\installer-wizard-small.bmp
CloseApplications=yes
CloseApplicationsFilter=SoundAnchor.exe
RestartApplications=no
VersionInfoVersion={#AppVersion}.0
SetupLogging=yes
; Build-Packages.ps1 defines SignToolName only when a signing command is supplied, so unsigned
; builds keep working without an Authenticode certificate.
#ifdef SignToolName
SignTool={#SignToolName}
SignedUninstaller=yes
#endif

[Tasks]
Name: "startup"; Description: "Start SoundAnchor when I sign into Windows"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SoundAnchor"; Filename: "{app}\SoundAnchor.exe"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SoundAnchor"; ValueData: """{app}\SoundAnchor.exe"" --background"; Tasks: startup

[Run]
Filename: "{app}\SoundAnchor.exe"; Description: "Open SoundAnchor"; Flags: nowait postinstall skipifsilent

[Code]
var PurgePreferences: Boolean;

procedure StopSoundAnchor();
var ResultCode: Integer;
begin
  if FileExists(ExpandConstant('{app}\SoundAnchor.exe')) then
    Exec(ExpandConstant('{app}\SoundAnchor.exe'), '--exit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopSoundAnchor();
  Result := '';
end;

function InitializeUninstall(): Boolean;
begin
  PurgePreferences := False;
  if not UninstallSilent then
    PurgePreferences := MsgBox('Also remove saved device preferences and diagnostic logs?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var StartupValue: String;
begin
  if CurUninstallStep = usUninstall then begin
    StopSoundAnchor();
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'SoundAnchor', StartupValue) then
      if CompareText(StartupValue, '"' + ExpandConstant('{app}\SoundAnchor.exe') + '" --background') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'SoundAnchor');
  end;
  if (CurUninstallStep = usPostUninstall) and PurgePreferences then
    DelTree(ExpandConstant('{localappdata}\SoundAnchor'), True, True, True);
end;
