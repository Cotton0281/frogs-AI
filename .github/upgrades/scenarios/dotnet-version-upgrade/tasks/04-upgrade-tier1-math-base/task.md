# 04-upgrade-tier1-math-base: Upgrade foundational math libraries

Upgrade Accord.Math.Core and Accord.Math with dependency-aware sequencing, including framework/package adjustments and required API compatibility fixes. This tier has shared dependencies for higher-level statistics/genetic/neuro components and therefore carries medium blast-radius risk if unstable.

Assessment flags include both source and behavioral changes in this layer, so runtime-sensitive areas must be validated with focused tests where available.

**Done when**: Tier-1 projects build warning-free with target framework changes applied and all direct dependents remain buildable.

## Scope Inventory
- Projects in scope: `Accord.Math.Core (NETStandard)` and `Accord.Math (NETStandard)`.
- Downstream dependents to validate after tier changes: `Accord.Genetic`, `Accord.Statistics`, `Accord.Neuro`.
- Distinct concerns:
  1. Add `net10.0` target to both tier-1 projects.
  2. Remove/re-scope framework-included threading package references in `Accord.Math` for modern target.
  3. Fix source incompatibilities in `LineSearchFailedException.cs` (legacy serialization/CAS attributes).
  4. Verify tier-1 builds cleanly and direct dependents remain buildable on current targets.

## Assessment Findings
- `Accord.Math.Core`: only `Project.0002` (target framework extension to net10.0).
- `Accord.Math`:
  - `Project.0002` (target framework extension to net10.0)
  - `NuGet.0003` for `System.Threading.Thread` and `System.Threading.Tasks` (framework-included for modern target)
  - `Api.0002` in `Optimization/Unconstrained/LineSearchFailedException.cs` (legacy serialization/CAS)
  - `Api.0003` behavioral note in `IO/NumPy/NpzFormat.Writer.cs` (`ZipArchive.CreateEntry`)
