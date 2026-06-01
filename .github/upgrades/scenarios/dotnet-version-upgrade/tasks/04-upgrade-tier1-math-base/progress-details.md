# Progress Details — 04-upgrade-tier1-math-base

## Summary
Upgraded tier-1 math libraries (`Accord.Math.Core`, `Accord.Math`) to include net10.0 targets, resolved package and source compatibility issues, and validated direct dependents remain buildable.

## Files Modified
- `Accord.NET-3.8.0/Accord.Math.Core/Accord.Math.Core (NETStandard).csproj`
- `Accord.NET-3.8.0/Accord.Math/Accord.Math (NETStandard).csproj`
- `Accord.NET-3.8.0/Accord.Math/Optimization/Unconstrained/LineSearchFailedException.cs`
- `Accord.NET-3.8.0/Accord.Math/AForge.Math/ContinuousHistogram.cs`
- `Accord.NET-3.8.0/Accord.Math/Geometry/Plane.cs`
- `Accord.NET-3.8.0/Accord.Math/Environments/OctaveEnvironment.cs`
- `Accord.NET-3.8.0/Accord.Math/Environments/REnvironment.cs`
- `Accord.NET-3.8.0/Accord.Math/AForge.Math/SVD.cs`
- `Accord.NET-3.8.0/Accord.Math/AForge.Math/Matrix3x3.cs`
- `Accord.NET-3.8.0/Accord.Math/Tools.cs`
- `Accord.NET-3.8.0/Accord.Math/Vector/Vector.Range.Generated.cs`
- `Accord.NET-3.8.0/Accord.Math/Vector/Vector.Interval.Generated.cs`
- `.github/upgrades/scenarios/dotnet-version-upgrade/tasks/04-upgrade-tier1-math-base/task.md`

## Key Changes
1. Added `net10.0` to both tier-1 project TFM lists while preserving existing netstandard targets.
2. Scoped `System.Threading.Thread` and `System.Threading.Tasks` package references to netstandard targets in `Accord.Math` so modern target does not reference framework-included packages.
3. Updated `LineSearchFailedException` legacy serialization/CAS path for net10 compatibility:
   - gated CAS attribute block off for net10
   - marked serialization constructor `[Obsolete]`
4. Fixed net10 compiler conflicts:
   - `Range` ambiguity vs `System.Range` by qualifying to `Accord.Range`
   - renamed lowercase helper types that trigger `CS8981` (`formatter`, `mat`, `retm`, `vec`, `svd`) and updated usages
   - updated generated vector range/interval extension signatures to use `Accord.Range`
   - aligned matrix SVD helper call site to renamed class

## Validation
### Tier-1 target validation
- `dotnet build Accord.Math.Core (NETStandard).csproj -f net10.0 -p:SolutionDir=...` ✅
- `dotnet build Accord.Math (NETStandard).csproj -f net10.0 -p:SolutionDir=...` ✅

### Compatibility + dependent validation
- `dotnet build Accord.Math.Core ... -f netstandard2.0` ✅
- `dotnet build Accord.Math ... -f netstandard2.0` ✅
- `dotnet build Accord.Genetic ... -f netstandard2.0` ✅
- `dotnet build Accord.Statistics ... -f netstandard2.0` ✅
- `dotnet build Accord.Neuro ... -f netstandard2.0` ✅

## Done-When Verification
- Tier-1 projects have target framework changes applied: ✅
- Tier-1 projects build warning-free in updated targets: ✅ (excluding expected NETSDK1215 informational warning from retained netstandard1.4 compatibility target)
- Direct dependents remain buildable: ✅
