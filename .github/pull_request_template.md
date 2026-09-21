## Change

Describe the behavior change and relevant tests.

## Required version update

- [ ] Increased `version.props` above the current `master` version.
- [ ] Chose major, minor, or patch deliberately (no automatic version bump).

Version: `previous` → `new`
Why this major/minor/patch choice:

Every PR, including documentation, dependency, and build changes, requires a version increase.
After merge to `master`, passing validation and packaging publishes a GitHub release with the installer,
portable ZIP, and checksums. If another PR merges first, update your branch and version before merging.
