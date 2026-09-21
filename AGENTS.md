# AudioAnchor agent handoff

Read `docs/PLAN.md` before changing scope and `docs/HANDOFF.md` before continuing work.
The user approved implementation, a new GitHub repository under their authenticated account,
Windows installers with uninstall, GitHub Actions artifacts, and a full test automation suite.

- Build a Windows 11 x64, .NET 10 WPF tray application. Keep all six audio roles correct.
- Four user selections: ordinary playback, communications playback, ordinary recording,
  communications recording. Ordinary selections manage Console and Multimedia together.
- Use native Core Audio notifications; never block inside COM notification callbacks.
- Pause and unmanaged roles must never write defaults. Missing devices must not erase preferences.
- Do not identify devices by friendly name alone. Do not silently select replacements with a new ID.
- Test policy behavior independently of Windows; run integration, UI, and packaging tests too.
- Tests must not change the developer's real audio defaults or startup settings implicitly.
- Keep source, documentation, and automation in this repository. Never commit credentials or build outputs.
- Update HANDOFF with completed work, exact verification results, remaining gaps, and next steps.
- Do not claim physical USB/Bluetooth, sleep, or audio-service recovery is tested based on simulations.
- Every PR to master MUST increase version.props. The developer chooses major, minor, or patch.
- Merged PRs publish a GitHub release automatically after passing validation/tests. Initial binaries are unsigned.
- The repository is public at the user's explicit request. See docs/VERSIONING.md.
- Licensed under the PolyForm Noncommercial License 1.0.0 (see LICENSE.md) — source-available, not
  open source. Free for any noncommercial use; commercial use is reserved to rm968211, the sole
  copyright holder. Never suggest relicensing, dual-licensing, or adding an OSI-approved license
  without the user explicitly asking.

See `docs/TESTING.md` for commands and hardware checks.
