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
        // Dashboards currently open. Each one can switch which population it shows via its dropdown,
        // so they are tracked as a flat list rather than keyed by population.
        private readonly List<PopulationDashboard> _openDashboards = new List<PopulationDashboard>();

        // How many open dashboards are watching each population. A population needs its PopulationStats
        // collector attached while >=1 dashboard watches it; the count lets switching/closing free it
        // only once the last watcher is gone.
        private readonly Dictionary<string, int> _statsWatchCount = new Dictionary<string, int>();

        private void ShowPopulationDashboard(Population population)
        {
            if (population == null)
                return;

            // Bring an existing dashboard already showing this population to the front.
            foreach (PopulationDashboard open in _openDashboards)
            {
                if (open != null && !open.IsDisposed && open.CurrentPopulationId == population.ID)
                {
                    open.WindowState = System.Windows.Forms.FormWindowState.Normal;
                    open.BringToFront();
                    open.Activate();
                    return;
                }
            }

            AttachStats(population.ID);

            PopulationDashboard dashboard = new PopulationDashboard(
                $"Dashboard — {population.Name}",
                GetDashboardPopulationOptions,
                BuildDashboardSnapshotById,
                OnDashboardPopulationSwitched,
                population.ID);

            dashboard.FormClosed += (s, e) =>
            {
                _openDashboards.Remove(dashboard);
                // Stop recording for whichever population it was last watching.
                DetachStats(dashboard.CurrentPopulationId);
            };

            _openDashboards.Add(dashboard);
            dashboard.Show();
        }

        /// <summary>Lists the live populations for a dashboard's population dropdown.</summary>
        private List<DashboardPopulationOption> GetDashboardPopulationOptions()
        {
            List<DashboardPopulationOption> list = new List<DashboardPopulationOption>();
            lock (simLock)
            {
                foreach (Population p in lsPopulations)
                    list.Add(new DashboardPopulationOption
                    {
                        Id = p.ID,
                        Name = p.Name,
                        Species = GetPopulationBeingName(p.Being)
                    });
            }
            return list;
        }

        /// <summary>Builds a snapshot for the population with the given id, or null if it no longer exists.</summary>
        private PopulationDashboardSnapshot BuildDashboardSnapshotById(string populationId)
        {
            Population population;
            lock (simLock)
                population = lsPopulations.FirstOrDefault(p => p.ID == populationId);
            return population != null ? BuildDashboardSnapshot(population) : null;
        }

        /// <summary>A dashboard switched populations: attach the new collector before freeing the old.</summary>
        private void OnDashboardPopulationSwitched(string oldId, string newId)
        {
            if (oldId == newId)
                return;
            AttachStats(newId);
            DetachStats(oldId);
        }

        /// <summary>Attaches (ref-counted) the history collector to a population so recording starts.</summary>
        private void AttachStats(string populationId)
        {
            if (string.IsNullOrEmpty(populationId))
                return;
            lock (simLock)
            {
                _statsWatchCount.TryGetValue(populationId, out int count);
                _statsWatchCount[populationId] = count + 1;
                if (count > 0)
                    return; // already being recorded by another dashboard

                Population population = lsPopulations.FirstOrDefault(p => p.ID == populationId);
                if (population == null)
                    return;
                if (population.Stats == null)
                    population.Stats = new PopulationStats();

                // Baseline backfill: if the golden brain was already seeded before the dashboard
                // feature existed (e.g. a restored session), GoldenInitialGene is null and there is
                // nothing to diff against. The true first gene is unrecoverable, so we baseline from
                // the current golden brain — the diff then accumulates from now on.
                if (population.GoldenInitialGene == null && population.GoldenAgentGene != null)
                    population.GoldenInitialGene = Utils.CloneGene(population.GoldenAgentGene);
            }
        }

        /// <summary>Detaches (ref-counted) the collector, freeing it once the last dashboard stops watching.</summary>
        private void DetachStats(string populationId)
        {
            if (string.IsNullOrEmpty(populationId))
                return;
            lock (simLock)
            {
                if (!_statsWatchCount.TryGetValue(populationId, out int count))
                    return;
                count--;
                if (count > 0)
                {
                    _statsWatchCount[populationId] = count;
                    return;
                }

                _statsWatchCount.Remove(populationId);
                Population population = lsPopulations.FirstOrDefault(p => p.ID == populationId);
                if (population != null)
                    population.Stats = null;
            }
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
                double top = double.NegativeInfinity, fitnessSum = 0, ageSum = 0;
                for (int i = 0; i < members.Count; i++)
                {
                    ISmartObject m = members[i];
                    if (m == null)
                        continue;
                    alive++;
                    double fitness = m.Fitness;
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
                    TopFitness = alive > 0 ? top : 0,
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
