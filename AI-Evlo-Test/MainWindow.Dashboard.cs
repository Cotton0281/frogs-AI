using System;
using System.Collections.Generic;
using System.Linq;
using AI_Evlo_Test.Objects;

namespace AI_Evlo_Test
{
    // Population Dashboard host: opens a per-population WinForms dashboard, and bridges it to the
    // model. The dashboard only costs compute while it is open: opening it attaches a
    // PopulationStats collector to the population, closing it detaches the collector so the
    // simulation goes back to a single `pop.Stats?.` null-check per tick.
    public partial class MainWindow
    {
        private readonly Dictionary<string, PopulationDashboard> _dashboards =
            new Dictionary<string, PopulationDashboard>();

        private void ShowPopulationDashboard(Population population)
        {
            if (population == null)
                return;

            // Bring an existing dashboard to the front rather than opening a second one.
            if (_dashboards.TryGetValue(population.ID, out PopulationDashboard existing)
                && existing != null && !existing.IsDisposed)
            {
                existing.WindowState = System.Windows.Forms.FormWindowState.Normal;
                existing.BringToFront();
                existing.Activate();
                return;
            }

            // Attach the history collector so recording starts now.
            lock (simLock)
            {
                if (population.Stats == null)
                    population.Stats = new PopulationStats();

                // Baseline backfill: if the golden brain was already seeded before the dashboard
                // feature existed (e.g. a restored session), GoldenInitialGene is null and there is
                // nothing to diff against. The true first gene is unrecoverable, so we baseline from
                // the current golden brain — the diff then accumulates from now on.
                if (population.GoldenInitialGene == null && population.GoldenAgentGene != null)
                    population.GoldenInitialGene = Utils.CloneGene(population.GoldenAgentGene);
            }

            PopulationDashboard dashboard = new PopulationDashboard(
                $"Dashboard — {population.Name}",
                () => BuildDashboardSnapshot(population));

            dashboard.FormClosed += (s, e) =>
            {
                _dashboards.Remove(population.ID);
                // Stop recording (and free the buffers) once no dashboard is watching this population.
                lock (simLock)
                    population.Stats = null;
            };

            _dashboards[population.ID] = dashboard;
            dashboard.Show();
        }

        /// <summary>
        /// Re-anchors the brain-diff baseline: snapshots the current golden brain as the new
        /// "initial" gene so the dashboard diff starts fresh from now. Useful for populations whose
        /// true initial gene was never captured (e.g. golden brains seeded before this feature).
        /// </summary>
        private void ResetGoldenBaseline(Population population)
        {
            if (population == null)
                return;

            lock (simLock)
            {
                population.GoldenInitialGene = population.GoldenAgentGene != null
                    ? Utils.CloneGene(population.GoldenAgentGene)
                    : null;
                SaveSession();
            }

            Log($"Golden brain-diff baseline re-anchored for '{population.Name}'.");
        }

        /// <summary>
        /// Builds an immutable snapshot of the population under simLock. Genes are cloned so the
        /// dashboard timer thread never reads model state the simulation thread may be mutating.
        /// </summary>
        private PopulationDashboardSnapshot BuildDashboardSnapshot(Population population)
        {
            lock (simLock)
            {
                List<ISmartObject> members = population.Members ?? new List<ISmartObject>();
                int alive = 0;
                double top = 0, fitnessSum = 0, ageSum = 0;
                double[] fitnesses = new double[members.Count];
                for (int i = 0; i < members.Count; i++)
                {
                    ISmartObject m = members[i];
                    if (m == null)
                        continue;
                    alive++;
                    double fitness = m.Fitness;
                    fitnesses[i] = fitness;
                    fitnessSum += fitness;
                    if (fitness > top)
                        top = fitness;
                    ageSum += m.Cycles;
                }

                PopulationStats stats = population.Stats;
                return new PopulationDashboardSnapshot
                {
                    Name = population.Name,
                    Species = GetPopulationBeingName(population.Being),
                    SizeLimit = population.SizeLimit,
                    AliveCount = alive,
                    TotalEver = population.TotalMembersCount,
                    LifeCycles = population.LifeCycles,
                    TopFitness = top,
                    MeanFitness = alive > 0 ? fitnessSum / alive : 0,
                    MeanAge = alive > 0 ? ageSum / alive : 0,
                    ArchivedBestCount = population.lsBestGenes?.Count ?? 0,

                    GoldenEnabled = population.GoldenAgentEnabled,
                    GoldenAveragedCount = population.GoldenAveragedNetworkCount,
                    GoldenThreshold = population.GoldenThreshold,
                    GoldenRecordSurvivorCycles = population.GoldenRecordSurvivorCycles,
                    GoldenAlive = population.GoldenAgent != null,
                    GoldenAge = population.GoldenAgent?.Cycles ?? 0,

                    Series = stats?.SnapshotSamples() ?? Array.Empty<PopulationSample>(),
                    CurrentFitnesses = fitnesses,
                    GoldenLifetimes = stats?.SnapshotGoldenLifetimes() ?? Array.Empty<GoldenLifetimeSample>(),
                    GoldenEvents = stats?.SnapshotGoldenEvents() ?? Array.Empty<GoldenAverageEvent>(),

                    GoldenInitialGene = population.GoldenInitialGene != null
                        ? Utils.CloneGene(population.GoldenInitialGene)
                        : null,
                    GoldenCurrentGene = population.GoldenAgentGene != null
                        ? Utils.CloneGene(population.GoldenAgentGene)
                        : null
                };
            }
        }
    }
}
