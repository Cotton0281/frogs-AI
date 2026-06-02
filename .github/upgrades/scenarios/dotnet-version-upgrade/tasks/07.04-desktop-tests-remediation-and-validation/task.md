# 07.04-desktop-tests-remediation-and-validation: Fix unit test compatibility and validate app+tests

# 07.04-desktop-tests-remediation-and-validation

## Objective
Resolve test-project compatibility issues and verify app/test build + test execution on upgraded framework.

## Scope
- `AI-Evlo-WPF.UnitTests` sources
- Solution-level validation for app + tests

## Steps
1. Fix test compile/runtime compatibility issues after app/package migration.
2. Build app and tests together with no warnings in touched projects.
3. Execute related tests and record outcomes/blockers.

**Done when**: app+test projects build cleanly on net10.0-windows and related test run results are captured.
