# Population Dashboard

A per-population real-time dashboard, opened from the population-card right-click menu via the
**Dashboard** item (`MainWindow.Dashboard.cs` → `ShowPopulationDashboard`). One dashboard exists per
population (keyed by `Population.ID`); re-invoking the menu item brings the existing window to front.

## Zero cost when closed

The dashboard is **attach-on-demand**, so a closed dashboard uses no compute:

- Opening it sets `Population.Stats = new PopulationStats()` under `simLock`.
- Closing it (`FormClosed`) sets `Population.Stats = null` under `simLock` and drops the buffers.
- The simulation only ever touches the collector through `pop.Stats?.…`, so when nothing is attached
  the per-tick cost is a single null-check per population and nothing is recorded.

`PopulationStats` (`Objects/PopulationStats.cs`) holds bounded, lock-guarded ring buffers:

- periodic population samples (`SampleIfDue`, every `SampleIntervalCycles`, from `SimulationTick`),
- golden lifetimes (`RecordGoldenDeath`, from `DisposeObject` when a golden agent dies),
- golden merge events (`RecordGoldenAverage`, from `TryUpdateGoldenAverage`).

The simulation thread writes; the dashboard's WinForms timer reads `Snapshot*` copies. `MainWindow`
builds a fully-cloned `PopulationDashboardSnapshot` under `simLock` so the timer thread never reads
live model state.

## Tabs

### Population
Live line charts (population size; top vs mean fitness; mean longevity) plus a current fitness
histogram. Header tiles show alive/limit, total ever, top/mean fitness, mean age, life cycles, and
archived-best count.

### Golden Agent
Tiles for feature on/off, brains merged, threshold, record survivor, and the live golden agent's age.
A bar chart of golden longevity per life, a bar chart of cycles between merges (lower = more frequent
brain updates), and a newest-first feed of merge events.

### Brain Diff
Two `GeneDeltaNetworkView` panels side by side: the **initial** golden brain (`GoldenInitialGene`,
snapshotted when the first survivor seeded the golden average) and the **current** golden brain
(`GoldenAgentGene`).

- Colour encodes change since initial: **black = unchanged → red = the largest change** in the net.
  Both panels share the same normalisation so equal change reads the same red on both sides.
- Line/node thickness encodes the weight/bias magnitude in that panel's state, so the left panel
  shows the original brain's shape and the right the current one.
- Hover a node for its initial bias, current bias, and delta.

Alongside the panels: a "most-changed weights & biases" table (top 25 by |Δ|) and a "mean |change|
per layer" bar chart showing whether evolution concentrated change in the input, hidden, or output
layers.

### Baseline capture and re-anchoring

`GoldenInitialGene` is captured at the moment the first survivor seeds the golden brain, so fresh
runs diff against the true original. For golden brains that were already seeded before this feature
existed (restored sessions), the true initial is unrecoverable, so:

- opening the dashboard **backfills** `GoldenInitialGene` from the current golden brain if it is null
  (`MainWindow.Dashboard.cs` → `ShowPopulationDashboard`), and
- the population-card right-click menu has a **"Reset golden baseline"** item
  (`ResetGoldenBaseline`) that re-anchors `GoldenInitialGene` to the current golden brain on demand,
  so the diff starts fresh from now.

After a (back)fill or reset the diff starts all-grey and reddens as the brain diverges. The colour
ramp is normalised relatively (against the largest change in the net), so the most-changed elements
read as red even when absolute per-merge change is small.
