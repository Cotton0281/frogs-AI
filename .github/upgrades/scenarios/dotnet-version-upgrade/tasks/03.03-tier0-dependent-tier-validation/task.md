# 03.03-tier0-dependent-tier-validation: Validate dependent libraries still build after tier-0 upgrade

# 03.03-tier0-dependent-tier-validation

## Objective
Validate that higher-tier dependent library projects still build on their current TFMs after tier-0 changes.

## Scope
- `Accord.Math.Core`, `Accord.Math`, `Accord.Genetic`, `Accord.Statistics`, `Accord.Neuro`

## Steps
1. Build each dependent project on current TFMs (with required solution properties such as `SolutionDir`).
2. Fix any regressions introduced by tier-0 changes that break dependent project compilation.
3. Record validation outcomes and unresolved blockers (if any) in progress details.

**Done when**: dependent projects build on existing TFMs or any blocking issue is fully documented for immediate follow-up.

## Research Notes
- Tier-0 (`Accord.Core`) now builds for `net10.0` and existing `netstandard2.0` compatibility target.
- This subtask validates dependent libraries remain buildable on their current targets after the tier-0 upgrade changes.
- Validation order follows dependency direction from lowest dependent upward: `Accord.Math.Core` → `Accord.Math` → `Accord.Genetic` / `Accord.Statistics` → `Accord.Neuro`.
