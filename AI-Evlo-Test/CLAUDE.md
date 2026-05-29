# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Restore NuGet packages
nuget restore AI-Evlo-WPF.sln

# Build (from solution root: D:\projects\AIlib\AI-Evlo-WPF)
msbuild AI-Evlo-WPF.sln /p:Configuration=Release /p:Platform="Any CPU"

# Output executable
bin\Release\ML-Evolutions.exe
```

**Target**: .NET Framework 4.7.2 | **Output**: WinExe | **Assembly Name**: ML-Evolutions | **Root Namespace**: AI_Evlo_Test

### Tests

MSTest suite in the sibling project `AI-Evlo-WPF.UnitTests` (`bin\Debug\net472\AI-Evlo-WPF.UnitTests.dll`).
Run with `vstest.console.exe <dll>`. Note: a number of tests fail independently of app
correctness — several `SmartObjectTests.Act_*` call `Assert.IsGreaterThan/IsLessThan` with the
arguments in `(actual, expected)` order, but the current MSTest overload is
`(lowerBound, value)`, so they assert backwards; the `VisualizeNetwork` `ShowNNet_*` tests need a
rendering host. Treat the current pass count as the regression baseline rather than expecting 100% green.

## Architecture

This is a WPF desktop application that evolves neural-network-driven agents using a genetic
algorithm. The simulation is an **ecosystem** rendered on a canvas:

- **Frogs** swim in the water and must rest on **rafts** to regain HP; they lose HP in open water.
- **Birds** are predators that fly, land on rafts, and eat frogs that are sitting on a raft.
- **Sharks** are predators that move under water (rendered beneath rafts) and eat frogs in open
  water — but cannot eat frogs that are on a raft.

On first launch the app restores the last saved session, or seeds a default scenario
(2 rafts, 50 frogs, 10 birds with random brains) and starts running. Sessions are saved to
`%AppData%\AI-Evlo\populations` on close.

> Note: an earlier stock-market / trading mode (`SmartTrader`, `StockMarket`) has been removed
> from the project. Ignore any references to trading in older notes.

### Core Object Hierarchy

```
IBasicObject → BasicObject (2D location, movement, collision, visual representation)
                    ↓
             ISmartObject → SmartObject (neural network, fitness, HP, stamina, perception)
                                ↓
                         Frog  (swimmer, rests on rafts)
                         Bird  (flying predator, eats frogs on rafts)
                         Shark (underwater predator, eats frogs in open water)
```

Species differences are expressed by overriding virtual hooks on `SmartObject`
(`EffectiveMaxHp`, `SenseCategory`, `IgnoredCategories`, `GetSpriteFrame`, `Act`,
`InteractWithRafts`) rather than `is Bird`/`is Shark` checks in the tick loop.

- **IBasicObject** (`Objects/BasicObjects.cs`): Location, inertia, face direction, movement,
  circular collision + elastic bounce helpers.
- **ISmartObject / SmartObject** (`Objects/SmartObject.cs`): Neural-network agent. `Act(double[])`
  runs inference and drives rotation/thrust; stamina drains with effort and scales movement.
- **RayPerception** (`Objects/RayPerception.cs`): ego-centric raycasting sensor feeding the NN.
- **Population** (`Objects/Population.cs`): container managing a group of agents of one
  `PopulationBeing`; archives top performers via `GenomeRecord` (`lsBestGenes`) for re-growth.

### Neural Network System

Configured via `NeuroNetStructure` (`Objects/Configs.cs`) with presets: `Small_1Lx9N()`,
`Mid_3Lx10N()`, `Big_5Lx20N()`. NN inputs = 2 scalars (HP deficit, stamina deficit) + 12 rays × 2
(distance + object-type signal) = 26. Outputs = 2 (rotation, thrust).

Built with the `ArtificialNeuralNetwork` factory chain: `NeuralNetworkFactory` → `NeuronFactory`
→ `SomaFactory` + `AxonFactory` (Tanh) + `SynapseFactory`.

### Genetic Evolution

`EvolutionChember` (`Objects/EvolutionChember.cs`) handles mutation:
- `MutateNN()` / `MutateGenom()` mutate weights/biases by percentage or absolute count.
- Layer-aware indexing (Input/Hidden/Output) via `IndexGene`.
- Preserves topology, only modifies weights and biases.
- On death, an agent's genome may be archived in its population's `lsBestGenes`; depleted
  populations re-grow from that archive (`ReGrowPopulation` in `MainWindow.AgentFactory.cs`).

### UI Layer (mixed WPF + WinForms)

| File | Purpose |
|------|---------|
| `MainWindow.xaml.cs` | Window, controls, selection, population management, save/load |
| `MainWindow.Simulation.cs` | Per-tick loop: movement, perception snapshot, environment effects |
| `MainWindow.AgentFactory.cs` | Agent/shape creation, offspring, disposal, population re-growth |
| `PopulationList.cs` | Population statistics viewer (WinForms DataGrid) |
| `VisualizeNetwork.cs` | Neural-network topology visualization (WinForms) |

### Environment Modes (`EEnvironmentType` in `Objects/Enumerators.cs`)

- `OneTarget`: a single moving "food" target; agents gain HP while on it, lose HP off it.
- `TwoTargets`: two rafts. Agents rest/regain HP on a raft; if more than half of all agents
  crowd one raft it sinks (stops giving HP) until the crowd thins.

## Key Dependencies

- **NeuralNetwork 7.4.0** (`ArtificialNeuralNetwork` namespace): neural network engine.
- **NeuralNetworkVisualizer 1.2.0**: WinForms NN topology visualization control.
- **Newtonsoft.Json 13.0.x**: population serialization.

## Code Conventions

- Interfaces prefixed with `I` (ISmartObject, IBasicObject, IPopulation).
- Thread-safe event access using the `object objectLock` pattern.
- Random seeded with `DateTime.Now.DayOfYear * 1000 + DateTime.Now.Millisecond`.
- JSON serialization via Newtonsoft.Json attributes on domain objects.
- Extension methods in `Extentions/NeuralNetworkGene.cs` for neural-network gene manipulation.
- The per-tick loop uses `Parallel.ForEach` over agents; keep `Act`/perception free of UI calls.
  UI updates (sprites, transforms, ray visualizer) run only when `isHeadlessMode` is false.
