# Progress Details — 03.01-tier0-project-and-package-stabilization

## Summary
Stabilized tier-0 project/package configuration so net10.0 restore succeeds and remaining failures are isolated to source/API migration items.

## Changes Applied
- Updated `Accord.NET-3.8.0/Accord.Core/Accord.Core (NETStandard).csproj`:
  - Kept multi-targeting: `net10.0;netstandard2.0;netstandard1.4`
  - Removed legacy `Choose/Otherwise` DataAnnotations reference path that caused framework-reference conflicts on modern target.
  - Kept package-based DataAnnotations only for netstandard targets.
  - Retained netstandard1.4 vulnerability remediations:
	- `System.Net.Http` 4.3.4
	- `System.Text.RegularExpressions` 4.3.1
- Added research notes in task file documenting stabilization rationale.

## Validation
- `dotnet build ...Accord.Core (NETStandard).csproj -f netstandard2.0 -p:SolutionDir=...` ✅ success
- `dotnet build ...Accord.Core (NETStandard).csproj -f net10.0 -p:SolutionDir=...` ✅ restore/package graph succeeded; remaining failures are source/API-level only

## Remaining Errors (for next subtask)
- `CS8981` (`cast` type naming)
- `CS0104` (`OrderedDictionary<,>` ambiguity)
- `SYSLIB0011` / `SYSLIB0050` (BinaryFormatter/SurrogateSelector obsolescence treated as errors)

## Done-When Verification
- net10.0 restore succeeds: ✅
- remaining failures are source/API-level, not package/reference-graph failures: ✅
