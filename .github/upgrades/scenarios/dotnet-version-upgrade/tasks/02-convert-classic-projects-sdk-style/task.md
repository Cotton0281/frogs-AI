# 02-convert-classic-projects-sdk-style: Convert non-SDK project files before TFM changes

Convert the classic project in scope to SDK-style format while keeping current framework behavior intact. This isolates structural project system migration from framework/API migration and avoids conflating failure modes.

The task includes validating that conversion is behavior-preserving at the original target framework and that package reference format is normalized for downstream upgrade work.

**Done when**: All required non-SDK projects are converted, build at original TFMs succeeds, and conversion artifacts are stable for TFM upgrades.

## Scope Inventory
- Projects affected: `AI-Evlo-Test/AI-Evlo-WPF.csproj` (only non-SDK project identified in assessment).
- Distinct concerns: SDK-style project conversion only (no TFM change), package reference normalization from legacy format if needed, and post-conversion build stability at current framework.
- Change signals from assessment: `Project.0001` (non-SDK project requires conversion), plus downstream `Project.0002` and compatibility issues to be handled in later tasks.

## Research Notes
- Topological order was retrieved to ensure dependency-aware processing; this task targets only one classic project, so no decomposition is required.
- Assessment confirms project metadata: `AI-Evlo-WPF.csproj` is `Sdk Style = False`, `Current target framework = net481`, `Project Kind = ClassicWinForms`.
- This task will execute conversion with `convert_project_to_sdk_style` and then validate build behavior with MSBuild at original framework settings.
- Build tool choice for validation remains `msbuild.exe` due to mixed .NET Framework + desktop stack and existing cached build tool decisions.
