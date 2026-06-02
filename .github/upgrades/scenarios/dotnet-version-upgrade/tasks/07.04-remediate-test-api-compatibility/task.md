# 07.04-remediate-test-api-compatibility: Fix unit test project for net10 desktop compatibility

# 07.04-remediate-test-api-compatibility

## Objective
Update unit tests and test project compatibility for `net10.0-windows` after app migration changes.

## Scope
- `AI-Evlo-WPF.UnitTests` source and project configuration
- Test references and any framework/API assumptions broken by migration

## Steps
1. Build test project against updated app and resolve compile/runtime compatibility issues.
2. Adjust test code for API signature or framework behavior differences where needed.
3. Ensure test project builds cleanly with updated dependencies.

**Done when**: unit test project builds successfully on `net10.0-windows` and is ready to execute tests.
