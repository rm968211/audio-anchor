# Current name

The product and repository were renamed from SoundAnchor to **AudioAnchor** after discovering
SoundAnchor was already the name of unrelated existing software. The GitHub repository moved to
`rm968211/audio-anchor`; every current source file, script, and document uses AudioAnchor. This
plan below is a frozen historical record of the original approval and is left exactly as written,
under the original name, for that reason.

# Current release-policy amendment

The user subsequently required a public repository, master as the default branch, a mandatory developer-selected version.props increase on every PR, and automatic published GitHub releases after merged PRs pass tests. docs/VERSIONING.md governs releases and supersedes the original private/draft/tag-only plan below.

# SoundAnchor — approved implementation plan

## Purpose and scope

Keep the user's chosen Windows default audio devices selected when new hardware appears or
Windows changes its selection. Implement a proper system tray application with a basic settings
UI. The user approved this plan and additionally requested appropriate unit tests, a complete test
automation suite, and persistent documentation for future agents. The GitHub repository is
`rm968211/sound-anchor`, private initially. Proposed product name: SoundAnchor.

## User selections and behavior

| Setting | Flow | Windows roles |
| --- | --- | --- |
| Playback | Render | Console and Multimedia |
| Communications playback | Render | Communications |
| Recording | Capture | Console and Multimedia |
| Communications recording | Capture | Communications |

Each selection can be unmanaged. Show preferred device, current defaults, and active, paused,
waiting, or error status. Save and apply explicitly; never silently adopt a new Windows default.
Persist exact endpoint identifiers plus display names. Duplicate names remain distinguishable.
Disconnected preferences stay saved; let Windows use an available device temporarily and restore
the preferred endpoint on its return. A changed endpoint identity requires explicit reselection.
Manual changes through Windows are also reversed while enabled; users pause enforcement or change
preferences within SoundAnchor when they intentionally want something else.

## Enforcement engine

Use IMMNotificationClient for default changes, added/removed devices, state changes, and relevant
property changes. Callbacks only queue work. A serialized worker coalesces event bursts, compares
all managed defaults, writes only mismatches, and verifies the result. Prevent self-generated
notifications from causing loops. Bound retries and rate-limit persistent conflicts. Reconcile on
startup, after resume, and with an occasional lightweight health check. Recover notification
registration after audio-service failures. Report errors without endless notifications.

Native Core Audio enumerates endpoints and current defaults. An isolated COM adapter uses the
widely used, undocumented IPolicyConfig.SetDefaultEndpoint operation. Validate its functionality
before relying on the complete UI. Compatibility failures must produce actionable errors.

Limits: this reacts after Windows changes defaults, so a brief interruption may occur. Explicit
per-application routing may ignore system defaults. No volume, mute, format, or per-app routing
changes are in scope. No device driver, background Windows service, or global registry audio hack.

## Application architecture and UI

C# / .NET 10 LTS / WPF, Windows 11 x64 initially. Separate Core policy/settings, Windows audio
interop, desktop app, and tests. A tray icon exposes Settings, Pause/Resume, Restore now, and Exit.
Closing the settings window hides it while enforcement continues. Optional start at sign-in,
single instance per user session, persisted preferences, bounded local diagnostic logs, and a
visible status panel. The app normally runs without administrator privileges. Exiting stops
enforcement and leaves the current Windows defaults in place. Paused state persists across launch.

## Packaging

Self-contained win-x64 publication includes .NET, distributed as a portable ZIP and an Inno Setup
installer EXE. Per-user installation, Start menu shortcut, optional startup registration, installed
apps entry, and upgrade support. Upgrades preserve preferences. Uninstall stops the app and removes
installed files and startup registration; offer to delete saved preferences and logs. Never delete
unrelated data. Initial downloads are unsigned and may display publisher warnings. Signing can be
added when a certificate is available. ARM64 and automatic updates are follow-up work.

## GitHub and CI/release automation

Create the private repo under the authenticated user's account. Pushes and pull requests restore,
build, run unit/integration/UI tests, publish the self-contained app, compile the installer, and
upload artifacts with test results and coverage. Package smoke tests install, upgrade, launch,
and uninstall in an isolated test path. Version tags create a draft release with the installer,
portable ZIP, and SHA-256 checksums. No release secrets are exposed to pull-request jobs.
Keep dependencies pinned and add dependency-update configuration.

## Test automation and acceptance criteria

1. Unit tests: six-role mapping, unmanaged and paused behavior, no-op convergence, unavailable
   endpoints, duplicate names, direction mismatch, verification failures, retry limits, and settings
   persistence/corruption handling.
2. Integration tests: simulated hardware/default notifications drive the actual worker; restore all
   six roles, survive disconnect/reconnect, event storms, transient failures, and restart persistence.
   Exercise COM enumeration read-only where endpoints exist; CI must not require physical hardware.
3. UI automation: launch the real WPF executable using an explicit simulated-audio test mode and
   isolated settings; choose devices, apply, pause/resume, persist/relaunch, close to tray and reopen.
4. Installer automation: install per user to a temporary directory, verify payload and uninstall
   registration, upgrade without losing preferences, launch, uninstall, and check startup cleanup.
5. Hardware checklist: actual USB/HDMI/Bluetooth connections, deliberate default switches, missing
   devices, reconnection, sign-in, sleep/resume, and audio-service restart. Restore any changed defaults
   after an explicitly enabled hardware test. Report which checks were actually executed.

Completion means builds and automated checks pass, repository and CI are operational, deliverables
are downloadable, and limitations/remaining physical tests are documented accurately. Unit tests
must cover meaningful behavior, not just mirror implementation. UI automation should use stable
automation IDs and avoid changing the real user's audio configuration.

## Implementation sequence

1. Repository/documentation/toolchain and native audio proof.
2. Policy engine, settings, and unit/integration tests.
3. Tray and settings UI, lifecycle/startup, UI automation.
4. Portable distribution, installer/uninstaller, packaging tests.
5. GitHub Actions, draft release artifacts, end-to-end verification, updated handoff.

## References

- https://learn.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nn-mmdeviceapi-immnotificationclient
- https://learn.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nn-mmdeviceapi-immdeviceenumerator
- https://github.com/amate/SetDefaultAudioDevice/blob/master/PolicyConfig.h
- https://dotnet.microsoft.com/en-us/platform/support/policy
- https://jrsoftware.org/isinfo.php
- https://github.com/FlaUI/FlaUI
