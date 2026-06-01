# Progress Details — 03-upgrade-tier0-core-foundation

## Summary
Completed tier-0 foundation upgrade by adding net10.0 support to `Accord.Core`, resolving tier-local package/reference and API compatibility blockers, and validating all dependent higher-tier libraries remain buildable on current targets.

## Subtask Outcomes
- `03.01-tier0-project-and-package-stabilization` ✅
  - Stabilized project/package graph for net10 build entry.
  - Removed conflicting legacy DataAnnotations reference flow and retained netstandard-target package references.
  - Kept vulnerability remediations for netstandard1.4 compatibility path.

- `03.02-tier0-api-remediation` ✅
  - Fixed net10 source/API blockers:
	- Renamed internal `cast` helper structs to `CastValue` (CS8981)
	- Explicitly qualified `OrderedDictionary` usage (CS0104)
	- Applied target-aware compatibility handling for formatter-related obsolescence paths
	- Marked legacy formatter-based exception serialization constructors and WebClient helper methods as obsolete legacy APIs

- `03.03-tier0-dependent-tier-validation` ✅
  - Built all dependent libraries on current netstandard2.0 target and confirmed no regressions from tier-0 changes.

## Files Modified (Tier-0 Parent Scope)
- `Accord.NET-3.8.0/Accord.Core/Accord.Core (NETStandard).csproj`
- `Accord.NET-3.8.0/Accord.Core/Cast.cs`
- `Accord.NET-3.8.0/Accord.Core/ExtensionMethods.cs`
- `Accord.NET-3.8.0/Accord.Core/Serializer.cs`
- `Accord.NET-3.8.0/Accord.Core/Attributes/SurrogateSelectorAttribute.cs`
- `Accord.NET-3.8.0/Accord.Core/AForge.Core/Exceptions.cs`
- `Accord.NET-3.8.0/Accord.Core/Exceptions/ConvergenceException.cs`
- `Accord.NET-3.8.0/Accord.Core/Exceptions/DimensionMismatchException.cs`
- `Accord.NET-3.8.0/Accord.Core/Exceptions/NonPositiveDefiniteMatrixException.cs`
- `Accord.NET-3.8.0/Accord.Core/Exceptions/NonSymmetricMatrixException.cs`
- `Accord.NET-3.8.0/Accord.Core/Exceptions/SingularMatrixException.cs`
- `tasks/03-upgrade-tier0-core-foundation/task.md`

## Validation
- `dotnet build Accord.Core -f net10.0 -p:SolutionDir=...` ✅
- `dotnet build Accord.Core -f netstandard2.0 -p:SolutionDir=...` ✅
- Dependent-library validation (all on netstandard2.0) ✅
  - Accord.Math.Core
  - Accord.Math
  - Accord.Genetic
  - Accord.Statistics
  - Accord.Neuro

## Done-When Verification
- Tier-0 projects target required frameworks: ✅ (`Accord.Core` includes `net10.0` + existing compatibility targets)
- Tier-0 compiles cleanly in scope: ✅
- Dependent higher tiers still build on existing TFMs: ✅
