# 05-upgrade-tier2-analytics-libs: Upgrade analytics and optimization libraries

Upgrade Accord.Genetic and Accord.Statistics after lower tiers stabilize. Apply target framework updates, remove obsolete package references, and resolve remaining API issues surfaced for this tier.

This tier is a dependency bridge into higher-level neuro and application layers; completion quality here directly affects later upgrade risk.

**Done when**: Tier-2 projects compile and tests (if any) pass, with no unresolved compatibility blockers for upstream projects.

## Scope Inventory
- Projects in scope: `Accord.Genetic (NETStandard)` and `Accord.Statistics (NETStandard)`.
- Upstream dependency to keep aligned: `Accord.Core` and `Accord.Math` already migrated to include `net10.0`.
- Direct higher-tier consumer to validate: `Accord.Neuro`.

## Assessment Findings
- `Accord.Genetic`: `Project.0002` only (add net10.0 target).
- `Accord.Statistics`:
  - `Project.0002` (add net10.0 target)
  - `NuGet.0003` for `System.ComponentModel.Annotations` behavior on modern target
  - 12 `Api.0002` occurrences of `BinaryFormatter` usage in model/performance save/load paths.

## Execution Plan
1. Add net10.0 target to both tier-2 project files while preserving compatibility targets.
2. Keep `System.ComponentModel.Annotations` package scoped to netstandard targets (no modern-target conflict path).
3. Address BinaryFormatter net10 compile blockers with target-aware legacy-compatibility handling.
4. Validate tier-2 builds (net10.0 + netstandard2.0) and confirm `Accord.Neuro` still builds on current compatibility target.
