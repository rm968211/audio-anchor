#ifndef AppVersion
  #error "AppVersion must come from version.props via scripts/Build-Packages.ps1"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
AppId={{82B72C23-769B-48AD-8174-B5ACED8384E2}
AppName=AudioAnchor
AppVersion={#AppVersion}
AppPublisher=rm968211
AppPublisherURL=https://github.com/rm968211/audio-anchor
DefaultDirName={localappdata}\Programs\AudioAnchor
DefaultGroupName=AudioAnchor
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\packages
OutputBaseFilename=AudioAnchor-{#AppVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\AudioAnchor.exe
; Inno's Win32 resource updater rejects an oversized icon ("File is too large") well below what
; Explorer/taskbar happily display, so assets/icon.ico is kept compact (PIL-optimized, ~55 KB).
SetupIconFile=..\assets\icon.ico
WizardImageFile=..\assets\installer-wizard-large.bmp
WizardSmallImageFile=..\assets\installer-wizard-small.bmp
CloseApplications=yes
CloseApplicationsFilter=AudioAnchor.exe
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
Name: "startup"; Description: "Start AudioAnchor when I sign into Windows"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AudioAnchor"; Filename: "{app}\AudioAnchor.exe"

[Registry]
; Renamed from SoundAnchor; unconditionally cleared on every install so an upgrading user never
; keeps a stale legacy startup entry, regardless of whether they recheck the task below this time.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "SoundAnchor"; Flags: deletevalue
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AudioAnchor"; ValueData: """{app}\AudioAnchor.exe"" --background"; Tasks: startup

[InstallDelete]
; Same AppId as the old SoundAnchor release, so Inno treats this as an upgrade in place, but the
; renamed exe means the old file is never overwritten by the new file set and is left orphaned.
Type: files; Name: "{app}\SoundAnchor.exe"

[Run]
Filename: "{app}\AudioAnchor.exe"; Description: "Open AudioAnchor"; Flags: nowait postinstall skipifsilent

[Code]
var PurgePreferences: Boolean;

procedure StopAudioAnchor();
var ResultCode: Integer;
begin
  if FileExists(ExpandConstant('{app}\AudioAnchor.exe')) then
    Exec(ExpandConstant('{app}\AudioAnchor.exe'), '--exit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure StopLegacySoundAnchor();
var ResultCode: Integer;
begin
  // Must exit before [InstallDelete] removes it, or the file-in-use delete fails.
  if FileExists(ExpandConstant('{app}\SoundAnchor.exe')) then
    Exec(ExpandConstant('{app}\SoundAnchor.exe'), '--exit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopAudioAnchor();
  StopLegacySoundAnchor();
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
    StopAudioAnchor();
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AudioAnchor', StartupValue) then
      if CompareText(StartupValue, '"' + ExpandConstant('{app}\AudioAnchor.exe') + '" --background') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AudioAnchor');
  end;
  if (CurUninstallStep = usPostUninstall) and PurgePreferences then
    DelTree(ExpandConstant('{localappdata}\AudioAnchor'), True, True, True);
end;
