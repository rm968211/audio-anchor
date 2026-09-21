# Latest change: adopted the PolyForm Noncommercial License 1.0.0

The user does not want this repository open source: they want sole ownership, free use for
everyone else for noncommercial purposes, and commercial use reserved exclusively to them. This
release (1.0.0 to **1.0.1**, patch: legal/metadata only, no functional change):

- Added `LICENSE.md` at repo root with the verbatim official text of the PolyForm Noncommercial
  License 1.0.0 (fetched from polyformproject.org, not paraphrased), including the
  `Required Notice: Copyright rm968211 (...)` line the license itself requires be carried forward
  by redistributors. The user chose "rm968211" (their GitHub handle) as the copyright holder name
  when asked, over using a real legal name.
- Updated `Directory.Build.props` (`Authors`, new `Copyright` property, flows into every project's
  assembly attributes) and `installer/SoundAnchor.iss` (`AppPublisher`) from the old placeholder
  "SoundAnchor contributors" to "rm968211", consistent with sole ownership.
- Added a plain-English `## License` section to README.md plus a one-line mention under the app
  description, and a `LICENSE.md` link in the project documentation list.
- Added a durable note to AGENTS.md: this repo is source-available under PolyForm Noncommercial,
  not open source, and future agents should never suggest relicensing/dual-licensing without being
  asked.

**Not done:** no Inno Setup `LicenseFile` acceptance page was added to the installer — the user
asked about the software's license, not for an install-time acceptance gate, so that would have
been scope creep. GitHub's own "License" sidebar detection was not independently verified (their
`licensee` matcher may or may not recognize PolyForm Noncommercial by fingerprint even though the
`LICENSE.md` file itself is the authoritative source regardless of what the sidebar shows).

**Known conflict:** this PR and the concurrent `simplify-readme` PR both bump `version.props` to
1.0.1 from the same 1.0.0 base. Whichever merges second must rebase and choose a new version before
merge — normal under this repo's concurrent-PR policy, not an error.

# Previous change: 1.0 — status card fix, installer wording, protection color

Follow-up to the logo PR, added to the same branch before merge. This release (0.5.0 to **1.0.0**,
the user's explicit choice — not a semver-meaning bump, just the version they asked for):

- **Fixed a layout bug.** `WarningText` (the small line under the main status message — used for
  the demo-mode notice, startup-read errors, and device-enumeration errors) always reserved a
  blank line's height even when empty, because an empty `TextBlock` still occupies its line box.
  The user saw this as dead space in a plain, no-warning install. Fixed with a `SetWarning(string?)`
  helper that sets `Visibility.Collapsed` when there's nothing to show, replacing the three call
  sites that used to write `WarningText.Text` directly.
- **Status card color.** The status card's background now tints to match its own message, using
  the exact same precedence `EnforcementReport.Summary` already uses (error → red, paused/waiting →
  amber, protected → green, nothing configured → the default neutral card). Colors are static,
  frozen `SolidColorBrush`es chosen independent of the Fluent accent color, so the signal reads the
  same regardless of the user's Windows accent.
- **Installer wording.** The startup task's checkbox description changed from "Start SoundAnchor
  when I sign in" to "Start SoundAnchor when I sign into Windows," per the user's request. Only the
  installer task changed; the in-app checkbox (`MainWindow.xaml`'s `StartupCheck`) still reads
  "Start SoundAnchor when I sign in" — not asked to be changed.

Verified on 2026-09-21 with .NET SDK 10.0.401 on Windows 11: clean Release build with zero
warnings, all 35 unit/integration tests and the FlaUI desktop scenario pass. Screenshotted three
states: demo-mode protected (green card, demo notice still shows normally), a genuine no-warning
state using the real non-demo backend with an isolated empty data directory (confirmed the card now
hugs a single line of text — this is the exact bug reported, verified fixed; safe to run non-demo
here since first-run has no preferences, so the policy engine only reads current defaults and never
writes any), and demo-mode paused (amber card). Did not screenshot the error-red state (would need
a forced audio failure) or rebuild+reverify the installer wizard banners from the prior PR (unrelated
to this change, not touched).

# Previous change: applied the user's SoundAnchor logo everywhere

The user supplied `soundanchor logo.png`/`.ico` (a blue anchor with a sound waveform through the
shank) from their desktop and asked for it applied everywhere appropriate. This release (0.4.0 to
**0.5.0**, minor: user-visible branding, no behaviour change to enforcement):

- `assets/icon.ico` and `assets/logo.png` are the two canonical, source-controlled copies. The
  supplied `.ico` was 1.07 MB (uncompressed large frames) and made Inno Setup's resource updater
  fail with "File is too large" when used as `SetupIconFile`; it was re-encoded from the PNG with
  Pillow (`Image.save(..., sizes=[16..256])`) to the same 7 resolutions at ~56 KB, pixel-identical.
- `SoundAnchor.App.csproj` sets `<ApplicationIcon>` from `assets/icon.ico` (linked into the project
  as `Assets/icon.ico`) — this is the exe's Win32 icon resource, shown in Explorer, the taskbar, and
  Alt-Tab, and it flows through to the portable ZIP and the installed app automatically.
- `MainWindow.xaml` sets `Icon="Assets/icon.ico"` (title bar/taskbar), and the tray `NotifyIcon` now
  loads the same embedded resource via `Application.GetResourceStream` instead of
  `SystemIcons.Application`, disposed on exit alongside the other IDisposables.
- `installer/SoundAnchor.iss` sets `SetupIconFile` (the installer/uninstaller's own icon) and
  `WizardImageFile`/`WizardSmallImageFile`, two Pillow-generated BMP banners
  (`assets/installer-wizard-large.bmp` 164×314, `-small.bmp` 55×58, white background, logo
  centered) built once from the same source PNG — regenerate them from the PNG rather than
  hand-editing if the logo ever changes.
- `README.md` shows the full-resolution PNG centered at the top, and documents `assets/` as the
  single source of truth for every surface above.

Verified on 2026-09-21 with .NET SDK 10.0.401 on Windows 11: clean Release build with zero
warnings, all 35 unit/integration tests and the FlaUI desktop scenario still pass (unaffected by
this change), and `Build-Packages.ps1` produced 0.5.0 packages. Confirmed visually: the built exe's
extracted icon and the running window's title-bar icon both show the anchor logo; the installer's
own exe icon (`Get-AuthenticodeSignature`-style `ExtractAssociatedIcon`) shows the anchor; and the
small wizard banner renders correctly top-right on the installer's "Select Additional Tasks" page.
**Not independently screenshotted:** the large `WizardImageFile` banner and the tray icon pixels —
Inno 6 hides the Welcome/Finished pages by default (`DisableWelcomePage` defaults to `yes`, and the
Select Destination page auto-hides with one obvious default), so reaching them requires completing
a real per-user install, which was intentionally not done to avoid writing the real startup
registry entry from the installer's default-checked "start at sign-in" task; the tray icon uses the
identical embedded resource and a standard `System.Drawing.Icon(Stream, Size)` load already proven
via the title-bar icon and the exe's extracted icon, so it was not separately screenshotted. A
**manual follow-up** the user can do outside this repo: GitHub's repository "social preview" image
(Settings → General → Social preview) has no public API and must be uploaded through the web UI —
`assets/logo.png` is the file to use there.

# Previous change: removed the window Restore button, added a startup update check

The user felt the window's **Restore now** button was redundant — enforcement already reacts to
device-change notifications, a 15-second health check, and system resume, so a manual re-trigger
added nothing the tray menu's own **Restore now** item didn't already cover — and asked for an
in-app notice when a newer GitHub release exists. This release (0.3.0 to **0.4.0**, minor:
user-visible features, no behaviour change to enforcement):

- **Removed the window's Restore now button** (`RestoreButton`/`RestoreClicked`) from the bottom
  action bar. The tray context menu keeps its own **Restore now** item; `EnforcementWorker.Refresh()`
  is unchanged and still runs from the periodic health check and on system resume.
- **Update check.** `SoundAnchor.Core.UpdateChecker`/`GitHubReleaseSource` query
  `api.github.com/repos/rm968211/sound-anchor/releases/latest` (which already excludes drafts and
  prereleases) once at startup, outside demo mode only, with a 5-second timeout and every failure
  mode (offline, rate-limited, malformed body) swallowed rather than surfaced. A newer version shows
  an accent banner above the status card with a **View release** button that opens the release page.
  The current version is read from the build's `AssemblyInformationalVersion` (set from
  `version.props`), so a locally built binary ahead of the last release shows no banner.

Verified on 2026-09-21 with .NET SDK 10.0.401 on Windows 11: clean Release build with zero warnings,
**35 unit/integration tests passed** (13 new `UpdateCheckerTests`, parsing and swallowed-failure
cases), the **FlaUI desktop scenario passed** against the trimmed action bar. The live update path
was also verified end to end against the real repository with a throwaway console probe (not
committed): an older current version correctly resolved `0.3.0` with its release URL, and current
(`0.3.0`) and ahead-of-latest (`9.9.9`) versions both correctly returned no update. The banner's
visual layout was confirmed with a temporary env-var override forcing the check in demo mode,
removed before commit. **Not verified:** behavior when GitHub is genuinely rate-limiting or when
DNS/network is fully unavailable on a locked-down machine — the exception filter in
`UpdateChecker.CheckAsync` covers `HttpRequestException`/`TaskCanceledException`/`JsonException`,
which should cover both, but only real request failures and malformed JSON were exercised.

# Previous change: dark Fluent UI, clearer wording and optional code signing

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
