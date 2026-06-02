# 07.03-remediate-app-api-compatibility: Fix app project source/API incompatibilities for net10 desktop

# 07.03-remediate-app-api-compatibility

## Objective
Resolve application source/API incompatibilities introduced by migration to modern Windows desktop target.

## Scope
- App source files with compile errors after retarget/package remediation
- Legacy desktop API usage requiring modern equivalents or code adjustments

## Steps
1. Build app project and iterate through compile errors/warnings until warning-free in touched scope.
2. Apply code fixes for unsupported/changed APIs and desktop behavioral differences.
3. Validate app project build success on `net10.0-windows`.

**Done when**: app compiles cleanly on `net10.0-windows` with no unresolved task-scope blockers.
