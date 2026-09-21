<p align="center"><img src="assets/logo.png" width="160" alt="SoundAnchor logo"></p>

# SoundAnchor

A Windows tray app that restores your preferred speakers and microphones whenever Windows changes
the default audio devices. Windows 11 x64; built with C#/.NET 10 and WPF.

Free to use, modify, and share for noncommercial purposes under the [PolyForm Noncommercial License
1.0.0](LICENSE.md). Commercial use is reserved to the copyright holder.

## Use

Download the installer or portable ZIP from this repository's **Actions → Build, test and release**
artifacts, or from the Releases page. Extract the portable ZIP before
running `SoundAnchor.exe`; a separate .NET installation is not required.

Builds are unsigned, so Windows reports an unknown publisher during installation. Verify the
download against `SHA256SUMS.txt` and choose **More info → Run anyway**, or see
[code signing](docs/SIGNING.md) for how to publish signed packages.

Choose ordinary playback, communications playback, recording, and communications recording, then
click **Save and apply**. Each selection can also be left unmanaged. Ordinary playback and recording
cover both Console and Multimedia roles. The line under each selector says what SoundAnchor is doing
with that choice. **Sound control panel** opens the classic Windows Sound dialog; pause protection
first if a change made there should stick. The tray menu provides Settings, Pause/Resume, Restore
now, and Exit. Closing the window keeps the app running. Start at sign-in is optional. The window
uses the dark Fluent theme and your Windows accent colour.

On startup, the app checks this repository's latest GitHub release and shows a banner in the window
if a newer stable version is available; **View release** opens its release page. The check is a
single anonymous request to GitHub's public releases API, no other data is sent, and it never
blocks startup or the audio enforcement path. It only runs outside demo mode, and a failed or
offline check is silently skipped.

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
Diagnostic logs are local and size-limited. There is no telemetry in the app. The only outbound
network call is the startup check against GitHub's public releases API described above.

## Branding

`assets/icon.ico` is the single source-of-truth application icon: the exe's Win32 icon resource,
the window/taskbar/Alt-Tab icon, the tray icon, and the installer's icon all reference this one
file, so replacing it updates every surface at once. `assets/logo.png` is the same mark at full
resolution for documentation. `assets/installer-wizard-large.bmp` and `-small.bmp` are pre-rendered
Inno Setup wizard banners generated from the logo; regenerate them (`Image.save(..., sizes=...)`
with Pillow) rather than hand-editing, since Inno Setup's resource updater rejects an oversized
`SetupIconFile` (see the comment in `installer/SoundAnchor.iss`).

## Project documentation

- [Approved full plan](docs/PLAN.md)
- [Current handoff and verification](docs/HANDOFF.md)
- [Test suite and hardware checklist](docs/TESTING.md)
- [Code signing and the unknown publisher warning](docs/SIGNING.md)
- [Agent instructions](AGENTS.md)
- [License](LICENSE.md)

The audio setter uses the undocumented Windows `IPolicyConfig` COM interface; it is isolated in
`SoundAnchor.Windows`. Future Windows changes may require updating that adapter. Endpoint monitoring
uses Microsoft's documented `IMMNotificationClient` callbacks. See PLAN.md for upstream references.

## Releases

Every PR to master MUST increase the stable SemVer version in `version.props`. The developer decides
whether major, minor, or patch is appropriate. This includes dependency, documentation, and build PRs.
The manifest drives application metadata, package filenames, installer version, and release tag.

After a PR merges to master, passing version validation, tests, and packaging automatically publishes
a GitHub release with the installer, portable ZIP, and checksums. Direct pushes do not publish releases.
Required checks block merges with an unchanged, invalid, or decreasing version. See
[versioning and release policy](docs/VERSIONING.md). Released binaries are unsigned until a signing
certificate is configured; see [code signing](docs/SIGNING.md).

## License

SoundAnchor is source-available, not open source: the [PolyForm Noncommercial License
1.0.0](LICENSE.md) lets you use, modify, and redistribute it free of charge for any noncommercial
purpose: personal use, hobby projects, research, education, and nonprofit/government use are all
covered. Commercial use (offering it, or a derivative of it, as part of a paid product or service,
or otherwise for commercial advantage) requires a separate license from the copyright holder.

## AI disclosure

AI (Claude) was used in the creation of this project, including source code, tests, documentation,
and build/release automation, under the direction and review of the copyright holder.