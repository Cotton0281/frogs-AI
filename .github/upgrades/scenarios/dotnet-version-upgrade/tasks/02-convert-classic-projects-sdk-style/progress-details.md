# Progress Details — 02-convert-classic-projects-sdk-style

## Summary
Converted the only non-SDK project (`AI-Evlo-Test/AI-Evlo-WPF.csproj`) to SDK-style while keeping it on its original target framework (`net481`) and validated project build stability.

## What Changed
- Ran `convert_project_to_sdk_style` for `AI-Evlo-Test/AI-Evlo-WPF.csproj`.
- Kept framework unchanged (`net481`) and restored required desktop/framework reference:
  - Added `<Reference Include="System.Web" />` in the converted project to satisfy XAML compile-time metadata resolution.
- Removed legacy `AI-Evlo-Test/packages.config` after PackageReference migration.
- Cleaned project-local build warnings in modified project files:
  - Removed unused local variable in `PopulationList.cs`
  - Removed unused designer field in `PopulationList.Designer.cs`
  - Added event invocation helper in `EvolutionChember.cs` so event is referenced without changing behavior
  - Removed legacy binding redirects in `App.config` that produced full-trust warning (`MSB3111`)
- Enriched `tasks/02-convert-classic-projects-sdk-style/task.md` with scope inventory and assessment-backed research notes.

## Validation Performed
- Built converted project with MSBuild at original framework:
  - `msbuild AI-Evlo-Test/AI-Evlo-WPF.csproj /restore /t:Build /p:Configuration=Release`
  - Result: success, output at `bin/Release/net481/ML-Evolutions.exe`
- Confirmed `packages.config` removal for converted project.

## Notes / Known Baseline Issues
- Full solution build remains blocked by existing cross-project compatibility mismatch (`AI-Evlo-WPF.UnitTests` targets `net472` while app is `net481`), producing `NU1201` during restore/build. This is outside SDK-format conversion scope and will be handled in the app/test upgrade task.

## Done-When Verification
- All required non-SDK projects converted: ✅ (one project in scope)
- Build at original TFM succeeds: ✅
- Conversion artifacts stable for downstream TFM upgrade: ✅
