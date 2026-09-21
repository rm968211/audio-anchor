<p align="center">
  <a href="https://github.com/rm968211/audio-anchor/actions/workflows/build.yml"><img src="https://github.com/rm968211/audio-anchor/actions/workflows/build.yml/badge.svg" alt="Build status"></a>
  <a href="https://www.buymeacoffee.com/rm968211"><img src="https://img.shields.io/badge/Buy%20Me%20A%20Coffee-FFDD00?style=flat&logo=buymeacoffee&logoColor=black" alt="Buy Me A Coffee"></a>
</p>

<p align="center"><img src="assets/logo.png" width="160" alt="AudioAnchor logo"></p>

# AudioAnchor

Tired of Windows automatically changing your default audio devices when something is plugged in? AudioAnchor is a simple Windows tray app that restores your preferred speakers and microphones whenever Windows changes the default audio devices.

<p align="center">
  <img width="440" alt="AudioAnchor's settings window, showing protected playback and recording devices" src="https://github.com/user-attachments/assets/6d214017-f9c1-4df6-8c4f-0eafbb07c23a">
</p>

## Installation

1. Navigate to the [releases page](https://github.com/rm968211/audio-anchor/releases) and locate the latest version.
2. Download and run the `win-x64-Setup.exe` file. For those that prefer a portable version, download the portable `.zip`, extract, and run `AudioAnchor.exe`.

   <p align="center">
     <img width="720" alt="The GitHub releases page, with the win-x64-Setup.exe asset highlighted" src="https://github.com/user-attachments/assets/d31bd10c-49b7-4e66-929f-cf026ce71213">
   </p>

> [!NOTE]
> You may receive a popup saying "Windows protected your PC. Microsoft Defender SmartScreen prevented an unrecognized app from starting. Running this app might put your PC at risk." This is normal, you can click **More info**, then **Run anyway**.

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
dotnet run --project src/AudioAnchor.App -- --demo
```

Demo mode uses simulated devices and separate preferences; it cannot change real audio defaults or
startup settings. `--data-dir <directory>` selects an isolated preferences folder for tests.
`--background` starts in the tray, and `--exit` asks the matching instance to exit.
`--diagnose <file.json>` writes a read-only native endpoint/default report and exits.

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

AudioAnchor is source-available, not open source: the [PolyForm Noncommercial License
1.0.0](LICENSE.md) lets you use, modify, and redistribute it free of charge for any noncommercial
purpose: personal use, hobby projects, research, education, and nonprofit/government use are all
covered. Commercial use (offering it, or a derivative of it, as part of a paid product or service,
or otherwise for commercial advantage) requires a separate license from the copyright holder.

## AI disclosure

AI (Claude) was used in the creation of this project, including source code, tests, documentation,
and build/release automation, under the direction and review of the copyright holder.
