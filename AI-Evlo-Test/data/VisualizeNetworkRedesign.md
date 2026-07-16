# Population Neural-Network Designer

## Outcome

`VisualizeNetwork` is now a population-level neural-network workspace. Open it from a population card's right-click menu with **Neural Network Designer**. The old selected-agent **Show Net** button is removed.

The designer can switch among active populations and the special brains retained for each population:

- Golden agent
- Fallback spawn brain
- Longest-lived live agent
- Highest-fitness live agent
- Best archived genome

The displayed network is a cloned snapshot captured under `simLock`, so visualization never races the simulation.

The population list is polled independently of live graph refresh. Adding, deleting, or renaming a population in the main window updates the designer dropdown even when **Live** is unchecked.

## Population parameter controls

**Zero unlocked layers** sets incoming weights and biases in every unlocked destination layer to zero, clears `lsBestGenes`, and creates one zeroed archived seed for later regrowth. **Randomize** creates one canonical network, assigns independent values in `[-1, 1]` to its unlocked parameters, copies that same network to every live agent and the Golden brain, clears `lsBestGenes`, and stores the canonical network as its only new archived seed. Parentless regrowth clones the current best archived gene; only populations with no archived gene create a genuinely new random brain. Locked layers and `GoldenInitialGene`—the historical dashboard baseline—remain unchanged. The designer asks for confirmation before either destructive operation.

**Mutation Rate** is a persisted population setting from 1–20 and means an absolute parameter count, not a percentage. Automated mutated offspring use this count and select only weights or biases owned by unlocked destination layers. **Mutate displayed** applies the same count and lock rules to the special-agent brain currently selected in the designer. A newly added residual layer remains the only mutation target when all earlier hidden layers and the output layer are locked.

## Population topology and locks

`Population.LayerLocks` owns one lock per destination layer: every hidden layer followed by the output layer. A locked destination protects both its bias values and every incoming weight, even though an incoming weight is physically stored on the preceding neuron's axon. Mutation still computes its requested mutation count from the full genome, then redirects those mutations to unlocked targets. When no targets are unlocked, mutation performs no changes.

All lock state, custom layer definitions, auto-growth state, survival record, and the next milestone are persisted in `session.json`. Old sessions without these fields migrate to dense layer definitions with all layers unlocked.

## Identity-preserving growth

A conventional tanh layer cannot be inserted as an exact identity layer for every possible signal. The implementation therefore adds a same-width residual layer:

`y = x + tanh(Wx + b)`

The new branch starts with `W = 0` and `b = 0`, so `y = x` exactly. Existing outgoing weights are moved from the previous hidden layer to the residual layer, while the previous layer's new outgoing branch weights are zeroed. This preserves every agent's output until the new branch mutates.

Population growth is atomic. Candidate runtime networks and stored genes are validated and rebuilt first; live agents, the golden runtime agent, golden/current baseline genes, and archived genomes are committed only after all candidates succeed. The population template is then updated so future offspring use the same topology.

## Automatic growth

The population setting **Auto grow NN** schedules its first achievement at twice the species' natural survival duration. Later milestones double. On reaching a milestone:

1. Existing hidden and output layers are locked.
2. One zero-initialized residual layer is appended and left unlocked.
3. The entire population is migrated atomically.
4. The next doubling milestone is scheduled strictly above the survival record.

The survival record is tracked independently of whether the golden-agent feature is enabled. This policy is isolated in `PopulationAutoGrowthPolicy` so a future settings UI can replace the milestone rule without coupling it to rendering or mutation.

## Rendering-control research

[Westermo GraphX](https://github.com/westermo/GraphX) was the strongest free third-party candidate: it is Apache-2.0 and its current [Westermo.GraphX.Controls NuGet package](https://www.nuget.org/packages/Westermo.GraphX.Controls/) targets .NET 10 WPF. It remains a good option if the application later needs arbitrary graph editing or automatic graph-layout algorithms.

For the current dense, strictly layered networks, the designer keeps a custom immediate-mode WPF renderer. It avoids thousands of WPF child elements, gives predictable layer columns, makes the population lock overlay direct, and adds no package dependency. It supports pan, zoom, fit-to-view, node tooltips, and a minimum-absolute-weight filter for dense graphs.

## Verification contract

Automated tests cover:

- exact output preservation after residual insertion;
- zero initialization of residual branches;
- atomic migration of live, golden, and archived brains;
- rollback when a stored brain has incompatible topology;
- semantic layer-lock enforcement and all-layers-locked behavior;
- survival-triggered growth and milestone scheduling;
- session round-trip of residual topology, locks, and auto-growth state;
- the WPF designer/renderer public seams.

The lock images are reproducible with `tools/GenerateLayerLockIcons.ps1` and embedded as WPF resources.
