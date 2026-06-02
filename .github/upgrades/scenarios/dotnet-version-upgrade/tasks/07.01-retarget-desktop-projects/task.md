# 07.01-retarget-desktop-projects: Retarget app and test projects to net10.0-windows desktop TFMs

# 07.01-retarget-desktop-projects

## Objective
Retarget the desktop application and unit test projects from .NET Framework to modern Windows desktop TFMs (`net10.0-windows`) while preserving desktop capabilities and project wiring.

## Scope
- `AI-Evlo-Test/AI-Evlo-WPF.csproj`
- `AI-Evlo-WPF.UnitTests/AI-Evlo-WPF.UnitTests.csproj`

## Steps
1. Update target framework properties to `net10.0-windows` for both projects.
2. Ensure required desktop settings (`UseWPF`, `UseWindowsForms`, and related SDK behaviors) remain valid for modern target.
3. Run first-pass project builds to capture post-retarget compile/package blockers for follow-up subtasks.

**Done when**: both project files are retargeted and first-pass build diagnostics are captured for package/API remediation.
