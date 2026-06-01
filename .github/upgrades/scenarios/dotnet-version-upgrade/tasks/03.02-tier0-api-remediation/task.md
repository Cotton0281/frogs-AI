# 03.02-tier0-api-remediation: Fix net10.0 API/source incompatibilities in Accord.Core

# 03.02-tier0-api-remediation

## Objective
Resolve net10.0 compile-time API/source incompatibilities in Accord.Core surfaced after project/package stabilization.

## Scope
- Serialization/remoting compatibility code in `Accord.Core`
- Language/compiler issues (e.g., reserved identifiers, ambiguous types)

## Steps
1. Fix `SYSLIB0011`/`SYSLIB0050` build blockers in formatter-based serialization code using conditional compilation or safe compatibility strategy while preserving existing target behavior.
2. Resolve `CS8981` (`cast` type naming conflict) without changing public behavior.
3. Resolve `CS0104` ambiguity for `OrderedDictionary<,>` by explicit qualification where needed.
4. Rebuild net10.0 and netstandard2.0 targets and remove warnings in touched files/projects.

**Done when**: `Accord.Core` compiles warning-free for the updated targets in scope.

## Research Notes
- Current net10.0 blockers are isolated to source/API issues in `Cast.cs`, `ExtensionMethods.cs`, `Serializer.cs`, and `Attributes/SurrogateSelectorAttribute.cs`.
- `cast` helper structs are internal to `Cast.cs`; renaming should be low-risk with local aliasing to preserve call sites.
- `OrderedDictionary<,>` ambiguity is in `ExtensionMethods.cs`; explicit namespace qualification is sufficient.
- `BinaryFormatter` and `SurrogateSelector` usage is present in serializer paths; target-specific suppression in legacy-compatible methods is the least invasive path for this migration stage.

## Execution Notes
- Renamed internal `cast` helper structs to `CastValue` to resolve `CS8981` without changing conversion behavior.
- Qualified `OrderedDictionary` usage in `ExtensionMethods.cs` with `Accord.Collections` to resolve `CS0104`.
- Disabled surrogate-selector path for `NET10_0_OR_GREATER` and excluded `SurrogateSelectorAttribute` from net10 compilation.
- Marked legacy formatter-based exception serialization constructors as `[Obsolete]` to satisfy `SYSLIB0051` guidance.
- Marked WebClient helper APIs as `[Obsolete]` to satisfy `SYSLIB0014` while preserving compatibility behavior in existing targets.
