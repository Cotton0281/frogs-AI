# 01-prerequisites-toolchain: Validate SDK/toolchain and upgrade prerequisites

Confirm that the local environment is ready for a net10.0 migration and that solution-level prerequisites are stable before any project edits. This includes validating .NET 10 SDK availability, ensuring global SDK pinning does not block restore/build, and confirming baseline restore/build tooling is usable on the working branch.

This task reduces early failure risk and ensures that later compile/runtime failures are actual migration issues rather than environment misconfiguration.

**Done when**: .NET 10 SDK and global.json compatibility are verified, prerequisite blockers are documented/resolved, and the branch is ready for upgrade edits.
