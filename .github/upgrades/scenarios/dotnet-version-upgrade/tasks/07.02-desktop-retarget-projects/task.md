# 07.02-desktop-retarget-projects: Retarget desktop app and test projects to net10.0-windows

# 07.02-desktop-retarget-projects

## Objective
Move app and unit test projects to modern Windows TFMs after package compatibility strategy is in place.

## Scope
- `AI-Evlo-Test/AI-Evlo-WPF.csproj`
- `AI-Evlo-WPF.UnitTests/AI-Evlo-WPF.UnitTests.csproj`

## Steps
1. Retarget app project to `net10.0-windows` and align desktop project properties.
2. Retarget unit test project to `net10.0-windows` and align project reference compatibility.
3. Build both projects to surface source-level migration blockers.

**Done when**: both projects target net10.0-windows and compile progresses past project/package-level blockers.
