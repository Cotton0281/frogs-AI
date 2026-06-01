# 06-upgrade-tier3-neuro-lib: Upgrade high-level neuro library

Upgrade Accord.Neuro with all required framework and package updates after its dependency chain is fully migrated. Validate compatibility against updated lower tiers and ensure no unresolved migration incidents remain in this project.

This task completes shared library migration and prepares the application tier for final upgrade.

**Done when**: Accord.Neuro builds successfully against upgraded dependencies and no tier-local upgrade blockers remain.

## Scope Inventory
- Project in scope: `Accord.Neuro (NETStandard)`.
- Dependencies already upgraded in lower tiers: `Accord.Core`, `Accord.Math.Core`, `Accord.Math`, `Accord.Genetic`, `Accord.Statistics`.
- Distinct concerns:
  1. Add `net10.0` target while preserving compatibility targets.
  2. Validate no tier-local compile blockers surface after dependency chain migration.

## Assessment Findings
- Only reported issue is `Project.0002` (target framework extension to include net10.0).
- No package incompatibility or API issue inventory flagged for this project in assessment.
