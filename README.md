# SoundAnchor

A Windows tray app that restores your preferred speakers and microphones whenever Windows changes
the default audio devices. Windows 11 x64; built with C#/.NET 10 and WPF.

## Use

Download the installer or portable ZIP from this repository's **Actions → Build, test and package**
artifacts, or from a draft/released version on the Releases page. Extract the portable ZIP before
running `SoundAnchor.exe`; a separate .NET installation is not required. Initial builds are unsigned.

Choose ordinary playback, communications playback, recording, and communications recording, then
click **Save and apply**. Each selection can also be left unmanaged. Ordinary playback and recording
cover both Console and Multimedia roles. The tray menu provides Settings, Pause/Resume, Restore now,
and Exit. Closing the window keeps the app running. Start at sign-in is optional.

- Manual Windows device changes are also reversed while protection is enabled. Pause first to make
  a temporary change, or change your preferences in SoundAnchor.
- Unplugging a preferred device preserves the preference. Windows may select a temporary replacement;
  SoundAnchor restores the original endpoint when it reconnects.
- A driver that changes an endpoint's identity requires reselection; the app does not guess by name.
- A brief interruption can happen before correction. Apps with explicit audio routing may ignore the
  Windows default. The app does not change volumes, mute, formats, or per-app routes.
- Exiting or uninstalling stops enforcement and leaves the current audio defaults in place.

## Build and test

Install a .NET 10 SDK and run in PowerShell from the repository root:

```powershell
./scripts/Test.ps1
./scripts/Install-InnoCompiler.ps1
./scripts/Build-Packages.ps1 -InnoCompiler .tools/inno/ISCC.exe
```

UI automation requires an interactive Windows desktop. `Test.ps1 -SkipUi` runs the unit and
integration suite without UI automation. Reports and coverage go to `artifacts/test-results`.
Packages go to `artifacts/packages`.

For a safe interactive preview:

```powershell
dotnet run --project src/SoundAnchor.App -- --demo
```

Demo mode uses simulated devices and separate preferences; it cannot change real audio defaults or
startup settings. `--data-dir <directory>` selects an isolated preferences folder for tests.
`--background` starts in the tray, and `--exit` asks the matching instance to exit.
`--diagnose <file.json>` writes a read-only native endpoint/default report and exits.

## Install, upgrade, uninstall

The per-user installer does not require administrator privileges. Run a newer installer to upgrade;
preferences are preserved. Uninstall from Windows Installed apps. The uninstaller removes its startup
registration and offers to remove saved preferences and logs. Silent uninstall retains preferences.
Portable users should turn off start at sign-in and exit before deleting the extracted directory.

Preferences/logs: `%LOCALAPPDATA%\SoundAnchor`. Demo data: `%LOCALAPPDATA%\SoundAnchor-Demo`.
Diagnostic logs are local and size-limited. There is no network service or telemetry in the app.

## Project documentation

- [Approved full plan](docs/PLAN.md)
- [Current handoff and verification](docs/HANDOFF.md)
- [Test suite and hardware checklist](docs/TESTING.md)
- [Agent instructions](AGENTS.md)

The audio setter uses the undocumented Windows `IPolicyConfig` COM interface; it is isolated in
`SoundAnchor.Windows`. Future Windows changes may require updating that adapter. Endpoint monitoring
uses Microsoft's documented `IMMNotificationClient` callbacks. See PLAN.md for upstream references.

## Releases

Pushes and pull requests build, test, and produce packages. Push a tag such as `v0.1.0` to create a
**draft** GitHub release with installer, portable ZIP, and SHA-256 checksums after all checks pass.
Review hardware validation before publishing. ARM64, signing, and automatic updates are future work.
