# Handoff — SoundAnchor

## Location and authorization

Canonical local repository: **C:\devl\repositories\sound-anchor**.
Private remote: **https://github.com/rm968211/sound-anchor**.
The user explicitly requested this location after initial creation in a generated Codex workspace.
The repository was moved in full, including `.git`. Continue work here.

The user approved the full plan in PLAN.md, implementation, private repo creation, installer/uninstall,
Actions artifacts, appropriate unit testing and a complete automation suite. They asked for frequent
updates and noted limited credits. Preserve concrete progress in commits and this document.

## Latest verification (2026-09-21)

- Initial implementation committed and pushed as 90829c2.
- After relocation: clean Release build, 22 unit/integration tests and 1 UI scenario passed.
- Native enumeration passed: 85 endpoints and 6 defaults on the development machine.
- Native setter reassertion passed for all six roles; no different audio device was selected.
- Installer EXE and portable ZIP compiled successfully; lifecycle and hosted CI checks in progress.
- Added startup-recovery UI fix so delayed audio enumeration retains saved selections.

## Implemented

- Core six-role policy, event-driven serialized worker, retries, health checks, pause, settings/backup.
- Native Windows Core Audio enumeration/defaults/notifications and isolated IPolicyConfig setter.
- WPF settings window, tray, startup toggle, per-session single instance, resume handling, demo mode.
- Unit/integration suite, FlaUI desktop automation, Inno installer and lifecycle test script.
- Build/package scripts and CI/release workflow are being finalized.

## Verification performed before relocation

- Release solution build: succeeded with zero warnings/errors.
- Unit/integration suite: **22 passed**, no skipped tests; coverage generated.
- Desktop suite: **1 passed**, full select/apply/pause/simulate/reopen/relaunch scenario (~34 seconds).
- Native diagnostic exposed an incorrect collection GUID, fixed and rebuilt.
- A second native run exposed unreadable friendly-name properties on stale endpoints. Source fixed
  to fall back to the endpoint ID; this fix still needs rebuilding and native verification.
- No real audio defaults have been switched during the initial verification.

## Outstanding checks

Rebuild after remaining edits and relocation. Run native diagnostics and optionally the native
setter probe. Build installer/portable packages, run installer automation, push commits, and inspect
the first GitHub Actions run. Physical USB/HDMI/Bluetooth, sleep/resume, audio-service restart,
Explorer restart, and actual cross-version upgrade remain hardware/manual acceptance items.
Update this file after each completed check; do not call unrun checks passed.

## Toolchain / previous interruption

The parent Codex workspace contains `.NET SDK 10.0.401` under
`C:\Users\rm968\Documents\Codex\2026-09-21\referenced-chatgpt-conversation-this-is-an\work\tools\dotnet`.
Its NuGet package cache is the adjacent `nuget` folder. Use DOTNET_ROOT and PATH to select it.
GitHub CLI: `C:\Program Files\GitHub CLI\gh.exe`. Host PATH can be stale.
Git objects were created by the sandbox account. Use a process-scoped `safe.directory` Git config
for the canonical path when running as the host user; avoid global wildcard trust.

An earlier automatic approval check failed because of exhausted workspace credits; it did not
determine the installer action was unsafe. Later repository relocation and network checks succeeded.
The official Inno 7.1.0 x64 installer was downloaded, checksum recorded, signature valid (Pyrsys B.V.);
compiler setup was not completed at that interruption. CI bootstrap verifies the fixed SHA-256.
