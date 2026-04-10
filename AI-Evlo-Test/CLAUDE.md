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

No test framework is configured in this project.

## Architecture

This is a WPF desktop application that evolves neural-network-driven agents using genetic algorithms. It supports two modes: **creature navigation** (frogs navigating to targets) and **stock market trading** (agents trading equities).

### Core Object Hierarchy

```
IBasicObject → BasicObject (2D location, movement, visual representation)
                    ↓
             ISmartObject → SmartObject (adds neural network, fitness, HP, reproduction)
                                ↓
                         Frog (visual creature)
                         SmartTrader (trading agent with portfolio)
```

- **IBasicObject** (`Objects/BasicObjects.cs`): Location, inertia, face direction, trajectory tracking
- **ISmartObject** (`Objects/SmartObject.cs`): Neural network agent with `Act(double[] inputs)` for inference
- **Population / PopulationTraders** (`Objects/Population.cs`): Container managing groups of smart objects, tracks top performers via `GenomeRecord`

### Neural Network System

Configured via `NeuroNetStructure` (`Objects/Configs.cs`) with presets: `Small_1Lx9N()`, `Mid_3Lx10N()`, `Big_5Lx20N()`.

Built using Accord.NET factory chain: `NeuralNetworkFactory` → `NeuronFactory` → `SomaFactory` + `AxonFactory`(Tanh) + `SynapseFactory`.

### Genetic Evolution

`EvolutionChember` (`Objects/EvolutionChember.cs`) handles mutation:
- `MutateNN()` mutates network weights by percentage or absolute count
- Layer-aware mutation (Input/Hidden/Output layers separately)
- Preserves topology, only modifies weights and biases

### Trading System (`Objects/Market/`)

- **SmartTrader**: Neural network agent; `Money` is fitness metric; maintains `Portfolio` and `TradeOrders`
- **TradeOrder**: Supports Market/Limit/TrailLimit orders, Long/Short positions; `Process(price)` evaluates execution
- **Position**: Equity holding tracking symbol, shares, cost basis

### UI Layer (mixed WPF + WinForms)

| File | Purpose |
|------|---------|
| `MainWindow.xaml.cs` | Primary simulation loop (DispatcherTimer), object selection, population management |
| `StockMarket.cs` | Trading simulator with DataGrid + chart (WinForms) |
| `PopulationList.cs` | Population statistics viewer (WinForms DataGrid) |
| `VisualizeNetwork.cs` | Neural network topology visualization |
| `TradingStrategyGesign.cs` | Trading strategy designer (stub) |

### Environment Modes (`EEnvironmentType` in `Objects/Enumerators.cs`)

- `OneTarget` / `TwoTargets`: Creatures navigate to target(s)
- `StockMarket`: Agents trade equities using neural network decisions

## Key Dependencies

- **Accord.NET 3.8.0**: Neural networks, genetic algorithms, math
- **NeuralNetwork 7.4.0** + **NeuralNetworkVisualizer 1.2.0**: NN library and visualization
- **Newtonsoft.Json 13.0.3**: Serialization (populations, stock data)
- **Avapi / SimpleAlphaVantage**: Stock market data from AlphaVantage API

## Data Files

- `data/MSFT_daily.json`, `data/SPY_daily.json`: Historical stock prices in AlphaVantage JSON format

## Code Conventions

- Interfaces prefixed with `I` (ISmartObject, IBasicObject, IPopulation)
- Thread-safe event access using `object objectLock` pattern
- Random seeded with `DateTime.Now.DayOfYear * 1000 + DateTime.Now.Millisecond`
- JSON serialization via Newtonsoft.Json attributes on domain objects
- Extension methods in `Extentions/NeuralNetworkGene.cs` for neural network gene manipulation
