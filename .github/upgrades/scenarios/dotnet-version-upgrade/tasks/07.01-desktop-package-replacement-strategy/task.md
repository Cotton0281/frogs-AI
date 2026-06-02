# 07.01-desktop-package-replacement-strategy: Define and apply replacement strategy for incompatible neural-network packages

# 07.01-desktop-package-replacement-strategy

## Objective
Resolve `NeuralNetwork` and `NeuralNetworkVisualizer` incompatibility for net10.0-windows by determining and applying a concrete replacement strategy that leaves app/test projects in a compilable dependency state.

## Scope
- `AI-Evlo-Test/AI-Evlo-WPF.csproj`
- `AI-Evlo-WPF.UnitTests/AI-Evlo-WPF.UnitTests.csproj`
- Neural-network/visualizer package references and direct consumers

## Steps
1. Inventory all app/test source files depending on incompatible package namespaces/types.
2. Choose replacement path for this repo context (inline replacement/in-tree adaptation) and update project references/packages accordingly.
3. Ensure no unresolved package restore incompatibility remains for target framework migration work.

**Done when**: incompatible package references are removed/replaced and restore succeeds for app/test projects in migration branch context.
