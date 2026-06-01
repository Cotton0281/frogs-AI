# 03.01-tier0-project-and-package-stabilization: Stabilize tier-0 project file and package graph for net10.0 build entry

# 03.01-tier0-project-and-package-stabilization

## Objective
Stabilize `Accord.Core (NETStandard).csproj` so the project restores and compiles far enough on `net10.0` to expose only source/API migration errors.

## Scope
- `Accord.NET-3.8.0/Accord.Core/Accord.Core (NETStandard).csproj`
- Package/reference conditions related to netstandard and net10.0 compatibility

## Steps
1. Keep multi-targeting (`net10.0;netstandard2.0;netstandard1.4`) in place for transition.
2. Normalize package/reference conditions so DataAnnotations and framework-included functionality do not conflict on net10.0.
3. Keep vulnerable transitive package remediation for netstandard1.4 path (`System.Net.Http`, `System.Text.RegularExpressions`) with supported versions.
4. Build project on net10.0 and netstandard2.0 and capture remaining compiler/API errors for next subtask.

**Done when**: net10.0 restore succeeds and remaining failures are source/API-level (not package/reference-graph failures).

## Research Notes
- `Accord.Core (NETStandard).csproj` now multi-targets `net10.0;netstandard2.0;netstandard1.4`.
- Initial net10.0 build failures included package-vulnerability restore blockers (`NU1903`) and reference graph noise from legacy DataAnnotations reference path.
- Vulnerability blockers for netstandard1.4 compatibility path are already pinned to supported versions:
  - `System.Net.Http` 4.3.4
  - `System.Text.RegularExpressions` 4.3.1
- To isolate source/API migration work for the next subtask, this subtask removes conflicting legacy `System.ComponentModel.DataAnnotations` assembly reference behavior for non-netstandard targets while preserving netstandard package references.
