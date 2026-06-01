# 07-upgrade-desktop-app-and-tests: Upgrade WPF application and unit tests to net10.0-windows

Upgrade AI-Evlo-WPF and AI-Evlo-WPF.UnitTests to modern Windows-compatible TFMs, resolve incompatible packages, and apply inline API migration fixes for desktop framework usage. Include configuration migration to modern configuration patterns as applicable and maintain Windows desktop runtime compatibility.

This is the highest-risk task due to volume of API incompatibilities and desktop framework surface area. The work must leave both app and tests buildable together on updated frameworks.

**Done when**: App and test projects target modern frameworks, incompatible packages are resolved, builds are warning-free, and related tests pass.
