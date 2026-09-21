# Latest change: dark Fluent UI, clearer wording and optional code signing

The user reported four UI problems and asked about the installer's unknown-publisher warning. This
release (0.2.0 to **0.3.0**, minor: user-visible features, no behaviour change to enforcement):

- **Dark theme.** `App.xaml` sets `ThemeMode="Dark"`, which applies the WPF Fluent dark theme to
  every control and to the window title bar, and follows the user's Windows accent colour. Styles
  in application scope must stay untemplated: an implicit or explicit `Style` without a `Template`
  shadows the Fluent control style and drops that control back to the light classic chrome, so
  spacing now lives on the elements themselves and the primary button uses `AccentButtonStyle`.
- **One title.** The `SOUNDANCHOR` eyebrow above the heading duplicated the title bar and was
  clipped at the top of the window; it is gone, leaving a single heading with proper top spacing.
- **Clearer status lines.** The text under each selector no longer repeats Console/Multimedia role
  names and device names. It describes what SoundAnchor is doing in plain language, reporting the
  slot that needs attention most when Console and Multimedia disagree.
- **Sound control panel button** in a docked bottom bar with the existing actions, opening the
  classic Sound control panel (`control.exe mmsys.cpl,,0`), which still owns per-role defaults.
- **Optional Authenticode signing.** `Build-Packages.ps1 -SignToolCommand` signs the published
  executable, the setup and the uninstaller, using Inno Setup's `$f`/`$q` placeholders; CI passes a
  `SIGNTOOL_COMMAND` repository secret through when configured. Builds stay unsigned without it.
  [SIGNING.md](SIGNING.md) explains the warning and compares certificate options.

Verified on 2026-09-21 with .NET SDK 10.0.401 on Windows 11: clean Release build with zero warnings,
**22 unit/integration tests passed**, the **FlaUI desktop scenario passed** against the new layout,
31 versioning tests passed, and packaging produced 0.3.0 installer and portable packages. Signing was
smoke-tested with a temporary self-signed certificate: `SoundAnchor.exe`, `uninst.e32` and the setup
were all signed, `Get-AuthenticodeSignature` reported the expected signer, and the certificate was
then deleted. **Not verified:** signing with a real CA-issued certificate, and whether SmartScreen
stops warning — both need a purchased or granted certificate. `scripts/Test-Releases.ps1` still
requires PowerShell 7 (`pwsh`); under Windows PowerShell 5.1 it fails in `ConvertFrom-Json` property
access, on master as well as here, so CI remains its source of truth.

# SoundAnchor — continuation handoff

## Canonical locations

- Local repository: **C:\devl\repositories\sound-anchor** (the user's explicitly requested location).
- Public remote: **https://github.com/rm968211/sound-anchor**.
- Full approved scope: [PLAN.md](PLAN.md). Agent entry point: [../AGENTS.md](../AGENTS.md).
- Tested implementation commit: **87cfc4b** (plus this documentation-only follow-up).
- Successful CI: https://github.com/rm968211/sound-anchor/actions/runs/35622237568

The repository was moved in full from the generated Codex workspace. Do not recreate it there.
The user approved implementation, private repo creation, full test automation, installers/uninstall,
and Actions build artifacts. They requested frequent updates and durable context for future agents.

## Delivered

- Windows 11 x64 .NET 10 WPF settings/tray app with four device preferences covering all six roles.
- Event-driven correction, verification, bounded retries, health reconciliation, pause/resume,
  missing-device preservation, saved settings, startup option, single instance and resume handling.
- Native Core Audio enumeration/notifications and isolated undocumented IPolicyConfig setter.
- Simulated-audio demo/test mode, read-only native diagnostics, opt-in native hardware probe.
- Self-contained portable ZIP and per-user Inno installer with upgrade/uninstall and startup cleanup.
- Pinned GitHub Actions workflow, monthly Dependabot, installer lifecycle automation, draft-release
  creation configured for version tags. No version tag/release has been published yet.

## Verified on 2026-09-21

- Clean Release build, zero warnings/errors after relocation.
- **22 unit/integration tests passed**, no skipped tests. Policy and settings classes reached 100%
  line coverage; total Core line coverage was 75% (includes the demo backend and presentation summary).
- **1 FlaUI desktop scenario passed**: selects all four preferences, applies, pauses, simulates a
  switch, resumes, closes to tray, reopens, exits and verifies persistence after relaunch.
- Native enumeration succeeded with 85 endpoints and 6 defaults on the development machine.
- Native setter reassertion succeeded for **6/6 roles** without selecting a different device.
- Installer install, startup registration, launch, same-version upgrade, preference preservation,
  uninstall and startup cleanup passed locally and on GitHub's Windows runner.
- CI built and uploaded installer, portable ZIP, checksums, TRX reports and coverage.

The first CI run exposed a test-cleanup race that ran the uninstaller twice. Fixed in 87cfc4b;
the subsequent complete pipeline passed. Native testing also identified stale device properties;
enumeration now falls back to endpoint IDs when friendly names are unreadable. A startup UI fix
preserves saved selections when device enumeration is initially unavailable.

## Build outputs

Successful CI packages are downloaded to `artifacts/verified-packages` and SHA-256 verified.
Copies are provided in the original task's `outputs` directory for clickable delivery:
`C:\Users\rm968\Documents\Codex\2026-09-21\referenced-chatgpt-conversation-this-is-an\outputs`.
Repository source remains exclusively at the canonical C:\devl location.

`artifacts/packages` contains the earlier local build; prefer the verified CI packages because they
include the final startup UI fix. Generated artifacts and local toolchains are ignored by Git.

## Remaining acceptance work / limits

- Actual new USB/HDMI/Bluetooth connection tests, suspend/resume, audio-service/Explorer restart,
  scaling checks, and an actual cross-version upgrade need physical/manual validation.
- The opt-in hardware probe's real-switching mode is implemented but was not run. Only native
  enumeration and reassertion of already-selected defaults were run.
- Draft-release tag execution and code signing have not been exercised. Initial binaries are unsigned.
- Default restoration reacts after Windows changes devices; a brief interruption remains possible.
- Per-app routing, volume/mute/format controls, ARM64 and auto-updates are outside the first release.
- Installer preference-removal prompt is implemented; silent uninstall preservation is automated.

See [TESTING.md](TESTING.md) for commands and the full physical acceptance checklist. Do not report
simulations or native reassertion as proof of physical hot-plug switching.

## Local toolchain

.NET SDK 10.0.401 is at:
`C:\Users\rm968\Documents\Codex\2026-09-21\referenced-chatgpt-conversation-this-is-an\work\tools\dotnet`.
Set DOTNET_ROOT and prepend it to PATH. The NuGet cache and CLI home are adjacent `nuget` and
`cli-home` directories. GitHub CLI is `C:\Program Files\GitHub CLI\gh.exe`.
Inno compiler is `.tools/inno/ISCC.exe`; scripts can bootstrap its pinned, verified release.

The repository was originally created by the sandbox account. When using the host account, use a
process-scoped Git safe.directory for C:/devl/repositories/sound-anchor if necessary; never wildcard
global trust. The generated task's sandbox does not automatically grant writes to C:\devl, so use
the authorized filesystem approval mechanism or open this repository as a Codex project.

Useful commands: `scripts/Test.ps1`, `scripts/Build-Packages.ps1 -InnoCompiler .tools/inno/ISCC.exe`.
UI tests require an interactive desktop. Installer tests refuse existing installations and require
an explicit local switch; prefer disposable CI. No real enforcing app was left running or registered
to start at sign-in by development tests.
