# 01-prerequisites-toolchain: Validate SDK/toolchain and upgrade prerequisites

Confirm that the local environment is ready for a net10.0 migration and that solution-level prerequisites are stable before any project edits. This includes validating .NET 10 SDK availability, ensuring global SDK pinning does not block restore/build, and confirming baseline restore/build tooling is usable on the working branch.

This task reduces early failure risk and ensures that later compile/runtime failures are actual migration issues rather than environment misconfiguration.

**Done when**: .NET 10 SDK and global.json compatibility are verified, prerequisite blockers are documented/resolved, and the branch is ready for upgrade edits.

## Research Notes

### Scope Inventory
- Projects affected: solution-level prerequisite validation only (no project file modifications).
- Distinct concerns: SDK availability, global.json compatibility, baseline build tool validation.
- Build tooling decision: use `msbuild.exe` for baseline validation because the solution contains .NET Framework and desktop project types.

### Findings
- `validate_dotnet_sdk_installation(net10.0)`: compatible SDK found.
- `validate_dotnet_sdk_in_globaljson(net10.0)`: no global.json detected, so no pinning conflict.
- `dotnet --info`: .NET SDK 10.0.300 and .NET 10 runtimes (including WindowsDesktop) are installed.
- Baseline full solution MSBuild fails in `AI-Evlo-WPF.UnitTests` with existing `CS0246` missing namespace/type references (`AI_Evlo_Test`, `CoordinatesUtil`) on current branch state.
- Baseline app project build succeeds: `AI-Evlo-Test/AI-Evlo-WPF.csproj` builds with MSBuild and outputs `ML-Evolutions.exe`.

### Prerequisite Outcome
- Prerequisites required for starting migration are satisfied.
- Known baseline blocker is documented (pre-existing unit test compile issues) and will be handled during later app/test upgrade tasks.
