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

- **Frogs** swim in the water and must rest on **rafts** to regain HP; hungry frogs bite landed
  birds on rafts and sharks in water.
- **Birds** fly, land on rafts to rest, bite sharks while flying, and bite raft frogs while landed.
- **Sharks** move under water (rendered beneath rafts) and bite water frogs or flying birds when hungry.

On first launch the app restores the last saved session, or seeds a default scenario
(2 rafts, 50 frogs, 10 birds with random brains) and starts running. Sessions are saved to
`%AppData%\AI-Evlo\populations` on close.

> Note: an earlier stock-market / trading mode (`SmartTrader`, `StockMarket`) has been removed
> from the project. Ignore any references to trading in older notes.

### Core Object Hierarchy

```
IBasicObject → BasicObject (2D location, movement, collision, visual representation)
                    ↓
             ISmartObject → SmartObject (neural network, fitness, HP, perception)
                                ↓
                         Frog  (swimmer, rests on rafts, bites when hungry)
                         Bird  (flies, lands on rafts, bites sharks/frogs by state)
                         Shark (underwater predator, bites flying birds)
```

Species differences are expressed by overriding virtual hooks on `SmartObject`
(`EffectiveMaxHp`, `SenseCategory`, `IgnoredCategories`, `GetSpriteFrame`, `Act`,
`InteractWithRafts`) rather than `is Bird`/`is Shark` checks in the tick loop.

- **IBasicObject** (`Objects/BasicObjects.cs`): Location, inertia, face direction, movement,
  circular collision + elastic bounce helpers.
- **ISmartObject / SmartObject** (`Objects/SmartObject.cs`): Neural-network agent. `Act(double[])`
  runs inference and drives rotation/thrust.
- **RayPerception** (`Objects/RayPerception.cs`): ego-centric raycasting sensor feeding the NN.
- **Population** (`Objects/Population.cs`): container managing a group of agents of one
  `PopulationBeing`; archives top performers via `GenomeRecord` (`lsBestGenes`) for re-growth,
  and owns optional golden-agent state (`GoldenAgentGene`, `GoldenThreshold`,
  `GoldenAveragedNetworkCount`).

### Neural Network System

Configured via `NeuroNetStructure` (`Objects/Configs.cs`) with presets: `Small_1Lx9N()`,
`Mid_3Lx10N()`, `Big_5Lx20N()`. NN inputs = 1 scalar (HP deficit) + 12 rays × 2
(distance + object-type signal) = 25. Outputs = 2 (rotation, thrust).

Built with the `ArtificialNeuralNetwork` factory chain: `NeuralNetworkFactory` → `NeuronFactory`
→ `SomaFactory` + `AxonFactory` (Tanh) + `SynapseFactory`.

### Genetic Evolution

`EvolutionChember` (`Objects/EvolutionChember.cs`) handles mutation:
- `MutateNN()` / `MutateGenom()` mutate weights/biases by percentage or absolute count.
- Layer-aware indexing (Input/Hidden/Output) via `IndexGene`.
- Preserves topology, only modifies weights and biases.
- On death, an agent's genome may be archived in its population's `lsBestGenes`; depleted
  populations re-grow gradually by simulation ticks, not wall-clock time. Each population
  spawns one replacement after its natural survival interval (`MaxHp / base HP drain`),
  rotating through archived-best, mutated archived-best, live-best, mutated live-best, and
  random brains (`ReGrowPopulation` in `MainWindow.AgentFactory.cs`).
- Each population can also maintain one golden agent. The golden agent is a runtime-only live
  representative outside `Population.Members`; its brain is a running average of normal agents
  that pass `GoldenThreshold`. The threshold starts at species max HP divided by base HP drain
  and rises to half of the population record survivor. Long-lived agents can contribute again
  every 10% of their own golden contribution interval. Golden agents never average themselves.
  See `data/GoldenAgent.md`.

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
- `TwoTargets`: two rafts. Agents rest/regain HP on a raft; if at least one third of the frog population
  crowd one raft it sinks (stops giving HP) until the crowd thins.

## Key Dependencies

- **NeuralNetwork 7.4.0** (`ArtificialNeuralNetwork` namespace): neural network engine.
- **Newtonsoft.Json 13.0.x**: population serialization.

## Code Conventions

- Interfaces prefixed with `I` (ISmartObject, IBasicObject, IPopulation).
- Thread-safe event access using the `object objectLock` pattern.
- Random seeded with `DateTime.Now.DayOfYear * 1000 + DateTime.Now.Millisecond`.
- JSON serialization via Newtonsoft.Json attributes on domain objects.
- Extension methods in `Extentions/NeuralNetworkGene.cs` for neural-network gene manipulation.
- The per-tick loop uses `Parallel.ForEach` over agents; keep `Act`/perception free of UI calls.
  UI updates (sprites, transforms, ray visualizer) run only when `isHeadlessMode` is false.
