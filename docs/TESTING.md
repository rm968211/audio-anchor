# Testing AudioAnchor

## Automated layers

`scripts/Test.ps1` restores/builds the solution and runs:

- **Unit tests**: all six roles, pause/unmanaged no-write behavior, convergence, exact endpoint IDs,
  missing devices and wrong flow, partial failure isolation, read-back verification, persisted pause,
  invalid/future configuration backup and recovery.
- **Integration tests**: the actual asynchronous worker with a fake audio backend; notifications,
  self-generated events, 10,000-event bursts, disconnect/reconnect, pause/resume, bounded retries,
  and recovery through the health check without a notification.
- **Desktop automation**: FlaUI/UIA3 drives the real WPF executable in demo mode with isolated data.
  Selects all four preferences, applies them, verifies all six defaults, simulates a Windows switch,
  pauses/resumes, closes to the tray, reopens through single-instance IPC, exits, and checks persistence
  after relaunch. A failure screenshot is saved where possible. Requires an unlocked interactive desktop.

Unit/integration TRX and Cobertura coverage are written under `artifacts/test-results`.
UI reports use `ui.trx`. UI tests never connect to native audio or modify startup registration.
Do not interpret simulated tests as proof that physical hardware or sleep/resume works.

## Native diagnostics

Run the built application with `--diagnose <absolute-output.json>` for read-only enumeration and
notification registration. It reports all endpoints and all six defaults without changing them.

`dotnet run --project tests/AudioAnchor.HardwareProbe -- --verify-current` reasserts each existing
default to itself and verifies the native setter ABI without selecting different devices.

`dotnet run --project tests/AudioAnchor.HardwareProbe -- --exercise-switching` is an **opt-in test that
temporarily changes real defaults**. Close AudioAnchor and other enforcing utilities first. It takes a
snapshot, changes each testable role to an alternative active device, lets the worker restore the
original, and restores the snapshot in a finally block. Requires at least two active endpoints per
tested direction. A missing direction is reported as untested, not passed. Do not disconnect hardware
during the test. It does not survive forced process termination, so its saved snapshot is retained.

## Installer lifecycle

After packaging, `scripts/Test-Installer.ps1 -Installer <setup.exe>` runs automatically on GitHub's
disposable Windows runner. It checks install, payload, uninstall registration, startup command,
launch/exit, upgrade preservation, removal, and startup cleanup. Logs are uploaded with test results.
The launcher uses demo mode and unique data. The script refuses to replace an existing installation
or startup registration. On a local disposable Windows account, explicitly pass
`-AllowInstalledAppChanges`. Never run it against an existing user installation.

## Physical Windows 11 acceptance checklist

Record Windows build, app version, hardware, expected/observed behavior, and measured correction time:

- Select different communications and ordinary devices; inspect all six roles.
- Change defaults manually in Windows; confirm correction for render and capture.
- Attach a previously unseen USB headset, HDMI monitor/dock, or capture card.
- Disconnect the preferred endpoint; confirm waiting state, then reconnect it.
- Pair/reconnect Bluetooth and change headset profiles; record endpoint identity changes.
- Suspend/resume and sign out/in with startup enabled.
- Restart Windows Audio services in a disposable test environment; confirm reconnection.
- Run a competing audio utility; verify bounded activity and visible failures.
- Verify tray menu and double-click behavior after Explorer restarts.
- Install an older version, save real preferences, upgrade to a newer version, verify preservation.
- Uninstall with both preference-preservation and preference-removal choices.
- Check UI at 100%, 150%, and 200% scaling and with keyboard-only navigation.

Native hardware tests require access to the actual desktop audio session. Hosted CI may have no active
audio devices; those cases cannot count as successful switching tests.
