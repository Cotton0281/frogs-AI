# 07.02-resolve-desktop-package-compatibility: Remove/replace incompatible desktop app packages and legacy references

# 07.02-resolve-desktop-package-compatibility

## Objective
Resolve incompatible package/references in app and test projects after retargeting so restore/build can proceed into source-level fixes.

## Scope
- App package references (`NeuralNetwork`, `NeuralNetworkVisualizer`, legacy framework references)
- Test project incompatible package references and dependency alignment with app

## Steps
1. Remove or replace incompatible packages flagged by assessment for modern target.
2. Prune legacy framework references that are no longer valid under `net10.0-windows`.
3. Restore/build app and tests to verify package graph stability.

**Done when**: incompatible package blockers are removed/resolved and restore/build reaches source/API compilation stage.
