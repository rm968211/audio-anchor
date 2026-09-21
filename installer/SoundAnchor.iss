#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
AppId={{82B72C23-769B-48AD-8174-B5ACED8384E2}
AppName=SoundAnchor
AppVersion={#AppVersion}
AppPublisher=SoundAnchor contributors
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
CloseApplications=yes
CloseApplicationsFilter=SoundAnchor.exe
RestartApplications=no
VersionInfoVersion={#AppVersion}.0
SetupLogging=yes

[Tasks]
Name: "startup"; Description: "Start SoundAnchor when I sign in"; Flags: unchecked

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
