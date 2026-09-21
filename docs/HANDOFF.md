# Latest change: up-to-date confirmation, tray About, a themed dialog to replace MessageBox

The user wanted visual confirmation from the manual "Check for update" tray item when there's
nothing new (previously silent either way, which read as broken when clicked), plus a tray "About"
item showing the version and other basics. This release (2.0.2 to **2.1.0**, minor: purely
additive UI, no behaviour change to enforcement):

- `CheckForUpdateAsync` takes a `manual` parameter. The automatic startup check still stays fully
  silent regardless of outcome (matches the existing design philosophy and demo mode's offline
  guarantee), but a manual click now always shows something: the existing banner if a newer
  release exists, or a new confirmation if not.
- New tray **About** item (`ShowAbout`, no `!demo` gating since it's non-network/non-destructive):
  version, tagline, a clickable link to the GitHub repo, and the license/copyright line.
- Removed the tray context menu's **"Restore now"** item. It predated this change and was
  unrelated to the About/up-to-date work, but the user flagged it during review of this branch:
  the window-level "Restore now" button was already removed earlier in the project, and this tray
  menu equivalent was the last remaining copy of that affordance. `EnforcementWorker.Request()`
  stays in place (still called from the constructor's initial enforcement pass), only the manual
  tray trigger is gone. Updated `docs/PLAN.md`'s tray-menu description to match.
- **First attempt used `MessageBox` for both the About content and the "up to date" confirmation,
  and the user caught two problems by eye:** the About window's background showed a visible blue
  tint instead of matching the app's neutral dark grey, and the embedded icon looked blurry.
  Root causes: (1) a `Window` with no explicit `Background` gets WPF Fluent's default translucent
  Mica-style backdrop, which was bleeding through the desktop wallpaper behind the window — fixed
  with `Background="{DynamicResource SolidBackgroundFillColorBaseBrush}"`, the Fluent token for an
  opaque app background, which disables the translucency; (2) the `Image` requested the icon at
  56×56, a size not embedded in `assets/icon.ico` (16/24/32/48/64/128/256), forcing a blurry
  rescale — changed to 64×64, an exact embedded frame. Screenshotted before and after both fixes
  to confirm.
- **Also discovered along the way:** `MessageBox` is a native Win32 dialog and does not follow
  WPF's `ThemeMode`, so it always renders in the system's light chrome regardless of the rest of
  the app being dark — visibly inconsistent once screenshotted. Added `InfoDialog`, a small themed
  window (same background fix, same fonts) as a MessageBox stand-in for this new confirmation.
  Existing `ShowError`/`MessageBox.Show` call sites elsewhere in the app were deliberately left
  alone — out of scope for this task, and replacing every MessageBox usage app-wide is a larger,
  separate decision the user hasn't asked for.
- Neither new window sets `Owner`: `App.xaml.cs` only calls `ShowSettings()` on startup when not
  launched with `--background`, so `MainWindow` may not have a window handle yet when a tray click
  fires, and WPF requires an owner to already have one.
- **Follow-up after the user looked at the actual running app on their own machine:** the 64x64
  `icon.ico` frame still looked "extremely blurry" for real, even though my own screenshot looked
  fine. This environment's display reports 100% DPI scaling, so it couldn't reproduce the bug, but
  the mechanism is real regardless: WPF re-rasterizes a fixed-size decoded ico frame to physical
  pixels at whatever the actual DPI scale factor is, and any scale other than exactly 100% blurs a
  frame that was already "exactly" the requested logical size. Fixed by embedding `assets/logo.png`
  (1254x1254) as a second `Resource` in the csproj and using that instead of `icon.ico` for this
  one on-screen `Image`, with `RenderOptions.BitmapScalingMode="HighQuality"`: downscaling from a
  source far larger than any plausible display size stays crisp under any DPI factor, so there's no
  "pick the right frame" problem to get wrong. `icon.ico` is unchanged for the exe/taskbar/title-bar
  icon and the tray icon, where multi-resolution ico selection is exactly the right tool.

Verified with .NET SDK 10.0.401 on Windows 11: clean Release build, zero warnings, all 39
unit/integration tests and the FlaUI desktop scenario pass (both new windows are demo-mode-safe
and untouched by the existing test, since they're only reachable via the tray menu). Both new
windows and the fixed styling were screenshotted directly rather than assumed correct: real tray
automation wasn't attempted (right-clicking a live system tray icon by screen coordinates is
fragile and was already judged not worth it earlier in this project), so a temporary env-var test
hook (`AA_TEST_SHOW_ABOUT`/`AA_TEST_SHOW_UPTODATE`, matching the pattern used earlier in this
project for the update-check banner) called `ShowAbout()`/showed the confirmation directly from
`MainWindow`'s constructor in demo mode; both env-var branches were removed before committing —
confirmed via `grep` that no trace of them remains in the committed code.

# Previous change: real screenshots, README installation points at audioanchor.io

This release (2.0.2 to **2.0.3**, patch: docs/assets only, no functional change):

- Replaced both README screenshots (previously GitHub user-attachment links to old, pre-rebrand
  captures) with two real screenshots the user took of the actual running app on their own machine:
  `assets/screenshot-app.png` (the settings window, real device names, protected/green state) and
  `assets/screenshot-tray.png` (the tray flyout with the AudioAnchor icon and its "protecting
  audio" tooltip). Now stored locally in the repo rather than linked to GitHub's user-attachments
  CDN, matching how `assets/logo.png` is already referenced.
- The tray screenshot also shows several unrelated third-party app icons and the user's desktop
  wallpaper bleeding through the flyout's translucent background. Flagged this before publishing
  since it reveals more than the screenshot's subject; the user explicitly chose to use it exactly
  as provided rather than have it cropped down to just the AudioAnchor icon and tooltip.
- Installation section rewritten from the previous "go to the releases page, find the right asset"
  walkthrough (with an annotated releases-page screenshot) to a single line pointing at
  [audioanchor.io](https://audioanchor.io) — the standalone landing site from the prior session
  (see `audioanchor-site` repo). The domain is not live yet (DNS not configured; the user said
  that's intentional and being revisited later), but the README now names the intended long-term
  download path regardless. The SmartScreen warning note is unchanged.

Verified: `README.md` renders correctly via GitHub's markdown render API (fetched with `gh api
markdown`), confirming both new local image paths and the audioanchor.io link resolve as expected
markup. No app code changed, so no build/test run for this docs-only change.

# Previous change: adopted the PolyForm Noncommercial License 1.0.0, removed em dashes, AI disclosure

The user does not want this repository open source: they want sole ownership, free use for
everyone else for noncommercial purposes, and commercial use reserved exclusively to them. This
release (2.0.1 to **2.0.2**, patch: legal/metadata only, no functional change):

- Added `LICENSE.md` at repo root with the verbatim official text of the PolyForm Noncommercial
  License 1.0.0 (fetched from polyformproject.org, not paraphrased), including the
  `Required Notice: Copyright rm968211 (...)` line the license itself requires be carried forward
  by redistributors. The user chose "rm968211" (their GitHub handle) as the copyright holder name
  when asked, over using a real legal name.
- Updated `Directory.Build.props` (`Authors`, new `Copyright` property, flows into every project's
  assembly attributes) and `installer/AudioAnchor.iss` (`AppPublisher`) from the old placeholder
  ("SoundAnchor contributors", later "AudioAnchor contributors" after the rename) to "rm968211",
  consistent with sole ownership.
- Added a plain-English `## License` section to README.md, and a durable note to AGENTS.md that
  this repo is source-available under PolyForm Noncommercial, not open source, and future agents
  should never suggest relicensing/dual-licensing without being asked.
- Per a follow-up request on this same branch: removed every em dash from README.md (replaced with
  commas, periods, or colons depending on the sentence), and added an `## AI disclosure` section at
  the end of README.md stating AI (Claude) was used in the project's creation under the copyright
  holder's direction and review.
- This branch was opened before the SoundAnchor→AudioAnchor rename and the README simplification
  both merged to master, which left it with real merge conflicts (README.md, Directory.Build.props,
  installer's `.iss`, version.props) — and, it turned out, silently stopped GitHub Actions from
  triggering `pull_request`-event runs on further pushes to this branch, since GitHub cannot compute
  a merge ref for a PR in a conflicting state. Rebasing/merging master into this branch resolved
  both the conflicts and the missing CI runs; see the "Merge order" note in prior entries below for
  why this branch was always going to need that regardless.

**Not done:** no Inno Setup `LicenseFile` acceptance page was added to the installer — the user
asked about the software's license, not for an install-time acceptance gate, so that would have
been scope creep. GitHub's own "License" sidebar detection was not independently verified (their
`licensee` matcher may or may not recognize PolyForm Noncommercial by fingerprint even though the
`LICENSE.md` file itself is the authoritative source regardless of what the sidebar shows).

# Previous change: manual update check, and a real SoundAnchor→AudioAnchor upgrade path

Added to the `simplify-readme` branch per the user's explicit instruction, alongside the README
fixes. This release (2.0.0 to **2.0.1**, patch — no behaviour change for users who were never on
SoundAnchor, additive elsewhere):

- **Tray "Check for update."** Previously the update banner only ever appeared from the one check
  done at startup — closing and reopening the window did nothing, so the only way to re-check was
  restarting the whole app. Added a tray context menu item, right under Settings, that shows the
  window and fires a fresh `CheckForUpdateAsync()`. Hidden entirely in demo mode (rather than
  disabled or silently no-op) since `_updateSource` is never constructed there, preserving demo's
  documented offline/network-free guarantee.
- **SoundAnchor → AudioAnchor upgrade path.** The rename PR predicted, but didn't fix, that an
  upgrade would leave an orphaned `SoundAnchor.exe`, an orphaned "SoundAnchor" startup registry
  value, and unmigrated preferences in `%LOCALAPPDATA%\SoundAnchor`. The user confirmed this is a
  real problem on their own machine (a genuine prior SoundAnchor install, startup-enabled, real
  Voicemeeter device preferences) and asked for it handled so 1.x→2.x doesn't error and preferences
  carry forward:
  - `AudioAnchor.Core.SettingsMigration.MigrateSettingsFile` (new, fully unit tested): copies
    `settings.json` from a legacy directory into the new one, only when the new directory doesn't
    exist yet — that existence check is itself the idempotency guard, no separate "migrated" marker
    needed.
  - `AudioAnchor.App.LegacyMigration` (new): calls the above with the real
    `%LOCALAPPDATA%\SoundAnchor` path, then separately migrates the startup registry value —
    deletes the legacy "SoundAnchor" Run value and, if it existed and no "AudioAnchor" value is
    already set, calls `StartupRegistration.Set(true)` so the carried-forward intent actually takes
    effect under the new name/path. Both steps are independently best-effort (`IOException`/
    `UnauthorizedAccessException`/`SecurityException` swallowed) so a failure in one never blocks
    the other or app startup.
  - Called from `App.xaml.cs` before `Directory.CreateDirectory(dataDirectory)`, gated `!demo`
    only — covers both the installer and portable-ZIP paths uniformly, since only app code runs
    for both (installer-side Pascal script cleanup can't reach portable users at all).
  - `installer/AudioAnchor.iss`: a `[Registry] ... Flags: deletevalue` entry unconditionally clears
    the legacy "SoundAnchor" Run value on every install (separate from and independent of the
    Tasks-gated "AudioAnchor" value creation), and `[InstallDelete]` removes the orphaned
    `{app}\SoundAnchor.exe`. A new `StopLegacySoundAnchor` procedure runs in `PrepareToInstall`
    alongside the existing `StopAudioAnchor`, since the old exe could still be running under its
    old name/mutex and would otherwise hold the file lock `[InstallDelete]` needs.
  - `installer/AudioAnchor.iss`'s `AppId` GUID was already unchanged from the rename PR — that's
    what makes Inno recognize this as an upgrade of the same product at all, rather than the two
    reported symptoms (errors, unrecognized app) the user was actually seeing.

Verified on 2026-09-21 with .NET SDK 10.0.401 on Windows 11: clean Release build, zero warnings,
**39 unit/integration tests pass** (4 new `SettingsMigrationTests`, including one against this
machine's actual real legacy `settings.json` — copied safely into an isolated temp destination via
a throwaway `dotnet run` probe, never loaded by a running app, so the real enforcement engine never
saw it and no real Windows audio default was touched). The FlaUI desktop scenario passed (one
retry — a single earlier failure was a 15s UI-automation timeout immediately after the packaging
build finished compiling, most likely system load; re-ran clean in 4s, unrelated to this change
since both features are demo-gated off). `Build-Packages.ps1` produced 2.0.1 installer/portable
packages with the new `[InstallDelete]`/`[Registry]` sections compiling and packaging successfully.

**Not verified:** the startup-registry migration path was NOT exercised live against this machine's
real "SoundAnchor" registry value or the real `%LOCALAPPDATA%\AudioAnchor` default location —
deliberately, since demo mode isn't involved in `LegacyMigration` and this machine's real legacy
settings.json has `Paused: false` with real device preferences, so letting the full app load it
non-demo would have driven the real `WindowsAudioBackend` to switch live Windows audio defaults,
which is exactly the kind of implicit change to the developer's real state this repo's own AGENTS.md
prohibits. Verified instead by code review (the registry operations are the same
`Registry.CurrentUser`/`StartupRegistration.Set` calls the existing, already-working Startup
checkbox feature uses) and a full real install/upgrade/uninstall cycle was not run end-to-end
against a real prior SoundAnchor installation — `scripts/Test-Installer.ps1`'s automated lifecycle
test still only exercises fresh-install/upgrade-same-version/uninstall, not upgrade-from-legacy-name;
extending it to simulate that would be a reasonable next step if this needs stronger CI coverage.

# Previous change: renamed SoundAnchor to AudioAnchor

The user discovered "SoundAnchor" is already the name of unrelated existing software and asked for
a full rename to **AudioAnchor**. This release (1.0.0 to **2.0.0**, major — chosen because renaming
changes the installed product's identity: a machine with an old SoundAnchor install left behind an
orphaned `SoundAnchor.exe` and an orphaned "SoundAnchor" startup registry value after upgrading,
since Inno's file/registry cleanup only removes what the *new* version references; not applicable
in practice yet since there are no real external installs, only the developer's own test installs,
but the version number should say so regardless):

- **GitHub repository renamed**: `rm968211/sound-anchor` → `rm968211/audio-anchor` via
  `gh repo rename`. GitHub auto-redirects the old clone URL, web URL, and existing release asset
  links indefinitely, so nothing that already links to the old name breaks.
- **Local working directory renamed** to match:
  `C:\devl\repositories\sound-anchor` → `C:\devl\repositories\audio-anchor`, plus
  `git remote set-url` and a `git config --global --add safe.directory` entry for the moved path
  (Windows/Git flags a directory as "dubious ownership" when it's moved, unrelated to the rename
  itself — a standard, safe trust declaration, not a security downgrade).
- **Every project folder and file renamed** via `git mv` (preserves history): `AudioAnchor.slnx`,
  `installer/AudioAnchor.iss`, and all six `src`/`tests` project folders plus their `.csproj` files
  (`AudioAnchor.App`, `.Core`, `.Windows`, `.Tests`, `.UiTests`, `.HardwareProbe`).
- **All source, docs, and scripts updated** with a scripted, case-aware replace (`SoundAnchor` →
  `AudioAnchor`, `soundanchor` → `audioanchor`, `SOUNDANCHOR` → `AUDIOANCHOR`, `sound-anchor` →
  `audio-anchor`) across every tracked non-binary file — namespaces, `x:Class`/XAML references,
  `AssemblyName`/`Authors`/`Copyright`, the installer's `AppName`/`AppPublisherURL`/
  `DefaultDirName`/`DefaultGroupName`/`OutputBaseFilename`, environment variable names
  (`SOUNDANCHOR_EXE`/`SOUNDANCHOR_TEST_RESULTS` → `AUDIOANCHOR_EXE`/`AUDIOANCHOR_TEST_RESULTS`),
  the tray/window/dialog text, the startup registry value name, the `%LOCALAPPDATA%` data folder
  names, and — critically — the GitHub owner/repo string `GitHubReleaseSource` uses to build the
  update-check URL (`MainWindow.xaml.cs`), so the update checker calls the renamed repository's real
  API endpoint rather than relying on GitHub's redirect for API calls.
- **`installer/AudioAnchor.iss`'s `AppId` GUID was deliberately left unchanged.** Inno matches
  "is this an upgrade of the same product" by `AppId`, not by `AppName`, so keeping the same GUID
  is what makes a future AudioAnchor release correctly upgrade-in-place over an AudioAnchor install
  made right after this rename, rather than installing side-by-side. It does NOT make upgrading
  cleanly over an *old SoundAnchor-named* install possible — see the orphaned-files note above.
- **`docs/PLAN.md` deliberately NOT renamed.** It's a frozen historical record of the original
  approved plan and is left exactly as written under the original name, consistent with how the
  file already treats its own superseded content; a new amendment note was added at its top instead
  (matching the existing "Current release-policy amendment" pattern in that file).
- **Two verbatim historical quotes in this file's own older entries were excluded from the rename**
  and manually restored: the literal filename `soundanchor logo.png` the user supplied on
  2026-09-21, and the literal `SOUNDANCHOR` XAML eyebrow text quoted while describing its removal —
  both describe exact artifacts as they existed at that point in time, not the current state.

**Not done / left to the user:** no attempt to migrate or clean up any pre-existing
`%LOCALAPPDATA%\SoundAnchor` data folder, `SoundAnchor.exe`, or "SoundAnchor" startup registry value
this development machine's earlier test installs may have left behind — there's no installed
product surface (SoundAnchor was never distributed beyond this developer's own testing) to make
migration logic worth building. Manually remove them if desired; a fresh AudioAnchor install/run
creates its own separate `%LOCALAPPDATA%\AudioAnchor` and does not read the old location.

**Known conflict, more serious than prior version-number conflicts:** `simplify-readme` and
`noncommercial-license` were both branched from the pre-rename master (1.0.0) and touch README.md,
AGENTS.md, docs/HANDOFF.md, installer/SoundAnchor.iss, and version.props under the *old* paths and
name. Recommend merging this rename PR first — then rebasing those two on top only needs a
version-number conflict resolution and a straightforward text merge (their content doesn't touch
file/folder paths, which this PR is the only one that moves). Merging either of them first instead
means this rename PR would need to redo the equivalent of their content changes against the new
file paths.

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
- **Installer wording.** The startup task's checkbox description changed from "Start AudioAnchor
  when I sign in" to "Start AudioAnchor when I sign into Windows," per the user's request. Only the
  installer task changed; the in-app checkbox (`MainWindow.xaml`'s `StartupCheck`) still reads
  "Start AudioAnchor when I sign in" — not asked to be changed.

Verified on 2026-09-21 with .NET SDK 10.0.401 on Windows 11: clean Release build with zero
warnings, all 35 unit/integration tests and the FlaUI desktop scenario pass. Screenshotted three
states: demo-mode protected (green card, demo notice still shows normally), a genuine no-warning
state using the real non-demo backend with an isolated empty data directory (confirmed the card now
hugs a single line of text — this is the exact bug reported, verified fixed; safe to run non-demo
here since first-run has no preferences, so the policy engine only reads current defaults and never
writes any), and demo-mode paused (amber card). Did not screenshot the error-red state (would need
a forced audio failure) or rebuild+reverify the installer wizard banners from the prior PR (unrelated
to this change, not touched).

# Previous change: applied the user's AudioAnchor logo everywhere

The user supplied `soundanchor logo.png`/`.ico` (a blue anchor with a sound waveform through the
shank) from their desktop and asked for it applied everywhere appropriate. This release (0.4.0 to
**0.5.0**, minor: user-visible branding, no behaviour change to enforcement):

- `assets/icon.ico` and `assets/logo.png` are the two canonical, source-controlled copies. The
  supplied `.ico` was 1.07 MB (uncompressed large frames) and made Inno Setup's resource updater
  fail with "File is too large" when used as `SetupIconFile`; it was re-encoded from the PNG with
  Pillow (`Image.save(..., sizes=[16..256])`) to the same 7 resolutions at ~56 KB, pixel-identical.
- `AudioAnchor.App.csproj` sets `<ApplicationIcon>` from `assets/icon.ico` (linked into the project
  as `Assets/icon.ico`) — this is the exe's Win32 icon resource, shown in Explorer, the taskbar, and
  Alt-Tab, and it flows through to the portable ZIP and the installed app automatically.
- `MainWindow.xaml` sets `Icon="Assets/icon.ico"` (title bar/taskbar), and the tray `NotifyIcon` now
  loads the same embedded resource via `Application.GetResourceStream` instead of
  `SystemIcons.Application`, disposed on exit alongside the other IDisposables.
- `installer/AudioAnchor.iss` sets `SetupIconFile` (the installer/uninstaller's own icon) and
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
- **Update check.** `AudioAnchor.Core.UpdateChecker`/`GitHubReleaseSource` query
  `api.github.com/repos/rm968211/audio-anchor/releases/latest` (which already excludes drafts and
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
  names and device names. It describes what AudioAnchor is doing in plain language, reporting the
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
smoke-tested with a temporary self-signed certificate: `AudioAnchor.exe`, `uninst.e32` and the setup
were all signed, `Get-AuthenticodeSignature` reported the expected signer, and the certificate was
then deleted. **Not verified:** signing with a real CA-issued certificate, and whether SmartScreen
stops warning — both need a purchased or granted certificate. `scripts/Test-Releases.ps1` still
requires PowerShell 7 (`pwsh`); under Windows PowerShell 5.1 it fails in `ConvertFrom-Json` property
access, on master as well as here, so CI remains its source of truth.

# AudioAnchor — continuation handoff

## Canonical locations

- Local repository: **C:\devl\repositories\audio-anchor** (the user's explicitly requested location).
- Public remote: **https://github.com/rm968211/audio-anchor**.
- Full approved scope: [PLAN.md](PLAN.md). Agent entry point: [../AGENTS.md](../AGENTS.md).
- Tested implementation commit: **87cfc4b** (plus this documentation-only follow-up).
- Successful CI: https://github.com/rm968211/audio-anchor/actions/runs/35622237568

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
process-scoped Git safe.directory for C:/devl/repositories/audio-anchor if necessary; never wildcard
global trust. The generated task's sandbox does not automatically grant writes to C:\devl, so use
the authorized filesystem approval mechanism or open this repository as a Codex project.

Useful commands: `scripts/Test.ps1`, `scripts/Build-Packages.ps1 -InnoCompiler .tools/inno/ISCC.exe`.
UI tests require an interactive desktop. Installer tests refuse existing installations and require
an explicit local switch; prefer disposable CI. No real enforcing app was left running or registered
to start at sign-in by development tests.
