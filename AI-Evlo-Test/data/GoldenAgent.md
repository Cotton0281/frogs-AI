# Golden Agent

## Overview

Each population can maintain one **golden agent**. It is a live agent of the same species as the population, but it is tracked separately from the regular population members.

The golden agent exists to represent a running average of the neural networks from unusually successful survivors. It is visible on the simulation canvas with a golden tint and a glow, and it respawns immediately when it dies while the feature is enabled.

## Population Ownership

Golden-agent state is stored on `Population`:

- `GoldenAgentEnabled`: whether the feature is active. It is enabled by default.
- `GoldenAgent`: runtime-only live golden agent instance.
- `GoldenAgentGene`: persisted averaged neural-network gene.
- `GoldenAveragedNetworkCount`: number of network contributions already averaged into `GoldenAgentGene`.
- `GoldenRecordSurvivorCycles`: longest survivor cycle count observed for the population.
- `GoldenThreshold`: current qualifying cycle count for survivor contributions.

The golden agent is added to the global simulation object list so it moves, senses, loses HP, hunts, rests, and dies like any other agent. It is **not** added to `Population.Members`, so it does not count toward population size, regrowth, normal live/lost counts, or best-gene archive size.

## GoldenThreshold

`GoldenThreshold` is the minimum lifetime in cycles before a normal survivor can contribute its neural network to the golden average.

The initial threshold is based on the species HP pool divided by that species' base per-tick HP drain:

| Species | Formula | Default Value |
|---------|---------|---------------|
| Frog | `SmartObject.MaxHp / 0.35` | about 857 cycles |
| Shark | `Shark.SharkMaxHp / 0.4` | 3750 cycles |
| Bird | `Bird.BirdMaxHp / 0.45` | about 3333 cycles |

The threshold never drops below the initial species value.

When a population records a stronger survivor, `GoldenRecordSurvivorCycles` is updated. If half of that record is greater than the current threshold, `GoldenThreshold` rises to that half-record value.

Example:

- A shark population starts at `1500 / 0.4 = 3750`.
- A shark survives 20000 cycles.
- The population record becomes 20000.
- `GoldenThreshold` becomes `20000 / 2 = 10000`.

## Averaging Rule

The first qualifying survivor copies its neural network into `GoldenAgentGene`.

Every later qualifying contribution updates each matching weight and bias with an incremental average:

```csharp
average = average + (newValue - average) / (count + 1);
count++;
```

The averaging is performed on `NeuralNetworkGene` snapshots. It includes:

- input-layer outgoing weights
- hidden-layer biases
- hidden-layer outgoing weights
- output-layer biases
- output-layer outgoing weights, if any

Activation and summation function types are preserved from the existing golden gene. A survivor with a different topology is skipped, because its layers, neurons, or weight counts cannot be averaged safely.

## Repeated Contributions

A survivor can contribute more than once while it remains alive.

On first contribution, the survivor stores:

- `GoldenAverageIntervalTicks = ceil(GoldenThreshold * 0.1)`
- `NextGoldenAverageCycle = current cycles + GoldenAverageIntervalTicks`

After that, the same survivor contributes again each time it reaches `NextGoldenAverageCycle`, then the next milestone is advanced by the same interval.

Example:

- `GoldenThreshold = 10000`
- survivor first contributes at 10000 cycles
- interval is 1000 cycles
- it contributes again at 11000, 12000, 13000, and so on

This gives exceptionally long-lived agents more influence without making a single survivor replace the golden network outright.

## Self-Exclusion

Golden agents never average themselves. `TryAverageGoldenBrain` rejects the contribution when:

- the candidate is the population's `GoldenAgent`
- the candidate has `IsGoldenAgent == true`
- the feature is disabled
- the candidate has no neural network
- the candidate has not reached its current contribution milestone

## Runtime Behaviour

When the golden feature is enabled:

- one golden agent is spawned for each population
- it uses `GoldenAgentGene` when one exists
- otherwise it starts with a fresh network from the population template
- it respawns immediately after death
- it keeps the same species behaviour as regular population members

When the feature is disabled from the population-card context menu:

- the live golden agent is removed from the field
- normal survivor averaging stops
- persisted golden brain/count data is kept so re-enabling can continue from the same state

Changing a population's species or brain template rebuilds that population and resets the golden brain because old averaged genes may have a different topology.

## UI and Logging

Golden agents are tinted golden yellow. Animated species keep their normal sprite animation, but each frame is passed through `GoldenTintCache`.

The population card shows:

```text
golden <average-count> / T<threshold>
```

When a golden agent is selected, the selected-agent panel shows:

- the golden average count
- the current `GoldenThreshold`

Every time the golden neural network is updated, the log records:

- population name
- average count
- `GoldenThreshold`
- source agent ID
- source cycle count

## Tests

Golden-agent behaviour is covered in `AI-Evlo-WPF.UnitTests/Objects/PopulationTests.cs`:

- first qualifying survivor copies its network
- later survivors incrementally average weights and biases
- disabled feature blocks averaging
- dynamic `GoldenThreshold` uses species HP divided by drain
- record survivors raise `GoldenThreshold`
- the same survivor contributes again at 10% intervals
- golden agents do not average themselves
- topology mismatches do not change the golden average
