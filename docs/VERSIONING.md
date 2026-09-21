# Version and release policy

`version.props` is the single source of truth for SoundAnchor's product version. All projects import
it through Directory.Build.props. Packaging reads it directly. There is no independent build-script,
installer, or tag version override.

**Every PR to master MUST increase the version**, including documentation, dependency, test, and
workflow changes. Developers decide the appropriate stable SemVer MAJOR.MINOR.PATCH:

- Major: incompatible changes.
- Minor: compatible features.
- Patch: compatible fixes or maintenance.

Versions contain exactly three numeric components without leading zeros. Prerelease/build suffixes
are not supported for these stable Windows releases. Each component is limited to 65534 by Windows
assembly and installer metadata. The initial versioning feature raises 0.1.0 to 0.2.0.

The **Semantic version** check rejects missing, malformed, unchanged, and decreasing versions against
the target branch. Up-to-date branch protection must require this check and **Build, test and package**
so concurrent PRs cannot merge the same version. Developer changes are never automatically bumped,
including Dependabot PRs. Update/rebase and choose another version if master advances first.

## Release on merge

A push to master validates against the exact previous master commit (works for merge, squash, and
rebase strategies), runs all tests, builds the installer and portable ZIP, and verifies installer
lifecycle. A final job confirms that this exact commit corresponds to a merged PR to master, then
publishes vMAJOR.MINOR.PATCH with those build artifacts and SHA-256 checksums. Direct pushes and manual
builds do not create releases. No manually pushed tag is needed.

The job checks out the event's exact commit, never a moving branch tip. Failed validation/tests stop
publication. Assets are staged in a draft before publication. Retries reuse a draft for the same
version/commit; an already published release for the same commit is a no-op. A version belonging to
another commit is rejected. Rerun the failed workflow to recover transient GitHub/upload failures.

## Hosting requirement

The user explicitly requested making this repository public so required checks can block merges.
The repository is now public. Keep the required checks configured on master; a failing or stale
version check must not be bypassed.

Required protection settings once supported: PR required, strict/up-to-date required checks named
`Semantic version` and `Build, test and package`, administrator enforcement, no force pushes/deletion.

Run `scripts/Test-Versioning.ps1` for parser, comparison, legacy migration, and real-Git baseline tests.

`scripts/Test-Releases.ps1` adds eight offline publication tests: direct-push suppression, successful
publication order, corrupted assets, conflicting tags, resuming untagged drafts, rejecting a different
commit's draft, published-release idempotence, and upload-failure recovery. No network calls are made.
