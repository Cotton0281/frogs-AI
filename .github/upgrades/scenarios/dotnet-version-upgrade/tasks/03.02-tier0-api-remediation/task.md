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
