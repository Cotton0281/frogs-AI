# 07-upgrade-desktop-app-and-tests: Upgrade WPF application and unit tests to net10.0-windows

Upgrade AI-Evlo-WPF and AI-Evlo-WPF.UnitTests to modern Windows-compatible TFMs, resolve incompatible packages, and apply inline API migration fixes for desktop framework usage. Include configuration migration to modern configuration patterns as applicable and maintain Windows desktop runtime compatibility.

This is the highest-risk task due to volume of API incompatibilities and desktop framework surface area. The work must leave both app and tests buildable together on updated frameworks.

**Done when**: App and test projects target modern frameworks, incompatible packages are resolved, builds are warning-free, and related tests pass.

## Scope Inventory
- Projects affected: `AI-Evlo-Test/AI-Evlo-WPF.csproj`, `AI-Evlo-WPF.UnitTests/AI-Evlo-WPF.UnitTests.csproj`.
- Distinct concerns:
  1. Retarget app and tests to `net10.0-windows` with desktop settings.
  2. Remove/replace incompatible packages (`NeuralNetwork`, `NeuralNetworkVisualizer`) and align package graph.
  3. Resolve desktop API compile breaks and update legacy config/runtime references for modern .NET.
  4. Revalidate tests and full app/test build integration.

## Assessment Findings
- App project: very high API break volume (`Api.0001` dominant), two incompatible packages, legacy configuration and mixed WPF/WinForms usage.
- Unit tests: target mismatch and one incompatible package plus many API compile impacts tied to app surface.
- App project already converted to SDK-style in earlier task; this task focuses on TFM/package/API migration.
