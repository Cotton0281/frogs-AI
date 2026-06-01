# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0

## Source Control
- **Source Branch**: main
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: After Each Task

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: Bottom-Up

### Project Structure
- Project Approach: Multi-targeting
- Package Management: Per-Project (defer CPM to post-migration)

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages)
- Unsupported API Handling: Fix Inline
- Windows Native APIs: Windows Compatibility Pack

### Modernization
- Configuration Migration: Auto-migrate to .NET Core Configuration
- Nullable Reference Types: Leave Disabled

## Strategy
**Selected**: Bottom-Up (Dependency-First)
**Rationale**: The solution contains multiple .NET Framework projects plus dependent .NET Standard libraries, requiring dependency-ordered migration with validation checkpoints.

### Execution Constraints
- Strict dependency ordering: complete and validate lower dependency tiers before moving upward
- Keep migration changes scoped per task milestone and validate solution stability between milestones
- Resolve incompatible packages and unsupported APIs inline during each upgrade task to avoid deferred stub debt
- Preserve Windows desktop compatibility during migration using Windows compatibility support where required
- Perform full solution validation (build + tests) after migration tasks before closing the scenario

## Build Tool Decisions
- **AI-Evlo-Test/AI-Evlo-WPF.csproj**: msbuild.exe (desktop .NET Framework app baseline build validated)
- **AI-Evlo-WPF.sln**: msbuild.exe (mixed .NET Framework and desktop project stack)

## User Preferences
### Execution Style
- **Upgrade Scope**: Upgrade all projects in the solution

## Key Decisions Log
- Upgrade scope set to full-solution upgrade (all projects), not single-project upgrade.
