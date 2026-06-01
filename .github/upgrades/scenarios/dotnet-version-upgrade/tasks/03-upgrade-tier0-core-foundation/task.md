# 03-upgrade-tier0-core-foundation: Upgrade core foundation library tier

Upgrade the lowest dependency layer starting with Accord.Core to add net10.0 support while retaining compatibility for dependent projects still migrating. Address package removals that are framework-included and apply required source-level API updates identified by assessment.

This tier forms the base for all higher libraries, so it must be clean and validated before continuing.

**Done when**: Tier-0 projects target the required frameworks, compile cleanly, and dependent higher tiers still build on existing TFMs.

## Scope Inventory
- Projects affected: `Accord.NET-3.8.0/Accord.Core/Accord.Core (NETStandard).csproj`.
- Dependent projects to validate after tier change: `Accord.Math.Core`, `Accord.Math`, `Accord.Genetic`, `Accord.Statistics`, `Accord.Neuro`.
- Distinct concerns: add net10.0 multi-targeting to tier-0 library, keep existing netstandard targets for in-flight dependency migration, and validate dependent project buildability on current TFMs.

## Assessment Findings
- `Project.0002`: target frameworks must include net10.0 for this project.
- `NuGet.0003`: `System.ComponentModel.Annotations` package functionality is framework-included on modern target; current conditional package usage should stay scoped to netstandard targets.
- `Api.0002` incidents are concentrated in serialization/remoting paths (`BinaryFormatter`, `SurrogateSelector`, serialization exception constructors) and may require conditional handling for net10.0.

## Execution Plan
1. Update `TargetFrameworks` to include `net10.0` while preserving existing netstandard targets.
2. Build `Accord.Core` on net10.0 and existing primary compatibility target.
3. Validate dependent higher-tier libraries still build on their current TFMs after tier-0 changes.

## Build Findings After Initial Attempt
- Restore/build for `net10.0` initially failed on vulnerability warnings treated as errors (`NU1903`) from transitive packages. Added explicit patched references for `netstandard1.4` compatibility path: `System.Net.Http 4.3.4`, `System.Text.RegularExpressions 4.3.1`.
- Current `net10.0` compile still fails with multiple API/compatibility issues that must be remediated before tier completion:
  - `SYSLIB0011`/`SYSLIB0050` in serialization paths (`BinaryFormatter`, `SurrogateSelector`)
  - `CS8981` for type name `cast`
  - `CS0104` ambiguity with `OrderedDictionary<,>`
  - DataAnnotations reference resolution warnings from legacy conditional reference path

Given multiple distinct concerns, this task will be decomposed into focused subtasks (project/package stabilization, API remediation, then tier validation).
