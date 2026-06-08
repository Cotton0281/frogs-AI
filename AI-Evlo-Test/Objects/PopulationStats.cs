using System;
using System.Collections.Generic;
using ArtificialNeuralNetwork.Genes;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// One periodic time-series sample for a population.
    /// </summary>
    public struct PopulationSample
    {
        public int Cycle;
        public int Alive;
        public int TotalEver;   // cumulative members ever created (monotonic); used to derive deaths
        public double TopFitness;
        public double MeanFitness;
        public double MeanAge;
    }

    /// <summary>One golden-agent lifetime, recorded when a golden agent dies.</summary>
    public struct GoldenLifetimeSample
    {
        public int Cycle;       // simulation cycle at death
        public int Lifetime;    // how many cycles the golden agent lived
    }

    /// <summary>One contribution of a survivor brain into the golden average.</summary>
    public struct GoldenAverageEvent
    {
        public int Cycle;           // simulation cycle when the average happened
        public int AverageCount;    // resulting GoldenAveragedNetworkCount
        public string SurvivorId;   // contributing agent ID
        public int SurvivorCycles;  // contributing agent age
    }

    /// <summary>
    /// Runtime-only history collector for a single <see cref="Population"/>.
    ///
    /// It is <b>attached on demand</b>: the dashboard sets <c>Population.Stats</c> when it opens and
    /// clears it on close. While no dashboard is open the simulation only pays a single
    /// <c>pop.Stats?.</c> null-check per population per tick and records nothing, so a closed
    /// dashboard costs no compute and holds no buffers.
    ///
    /// All buffers are bounded ring buffers guarded by an internal lock so the simulation thread
    /// can write while the WinForms timer thread reads snapshots.
    /// </summary>
    public sealed class PopulationStats
    {
        private readonly object gate = new object();

        private readonly List<PopulationSample> samples = new List<PopulationSample>();
        private readonly List<GoldenLifetimeSample> goldenLifetimes = new List<GoldenLifetimeSample>();
        private readonly List<GoldenAverageEvent> goldenEvents = new List<GoldenAverageEvent>();

        private int lastSampleCycle;
        private bool hasSampled;

        /// <summary>How many simulation cycles between time-series samples.</summary>
        public int SampleIntervalCycles { get; set; } = 25;

        public int MaxSamples { get; set; } = 1000;
        public int MaxGoldenLifetimes { get; set; } = 300;
        public int MaxGoldenEvents { get; set; } = 500;

        /// <summary>
        /// Takes one population time-series sample if enough cycles have elapsed.
        /// Called from the simulation thread (already under simLock).
        /// </summary>
        public void SampleIfDue(Population population, int currentCycle)
        {
            if (population?.Members == null)
                return;
            // First call always samples; afterwards, throttle to one sample per interval.
            // (Guard against int overflow — do not subtract from an int.MinValue sentinel.)
            if (hasSampled && currentCycle - lastSampleCycle < SampleIntervalCycles)
                return;
            hasSampled = true;
            lastSampleCycle = currentCycle;

            int alive = 0;
            double top = 0, fitnessSum = 0, ageSum = 0;
            List<ISmartObject> members = population.Members;
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

            PopulationSample sample = new PopulationSample
            {
                Cycle = currentCycle,
                Alive = alive,
                TotalEver = population.TotalMembersCount,
                TopFitness = top,
                MeanFitness = alive > 0 ? fitnessSum / alive : 0,
                MeanAge = alive > 0 ? ageSum / alive : 0
            };

            lock (gate)
                Push(samples, sample, MaxSamples);
        }

        public void RecordGoldenAverage(int currentCycle, int averageCount, string survivorId, int survivorCycles)
        {
            GoldenAverageEvent ev = new GoldenAverageEvent
            {
                Cycle = currentCycle,
                AverageCount = averageCount,
                SurvivorId = survivorId,
                SurvivorCycles = survivorCycles
            };
            lock (gate)
                Push(goldenEvents, ev, MaxGoldenEvents);
        }

        public void RecordGoldenDeath(int currentCycle, int lifetimeCycles)
        {
            GoldenLifetimeSample s = new GoldenLifetimeSample { Cycle = currentCycle, Lifetime = lifetimeCycles };
            lock (gate)
                Push(goldenLifetimes, s, MaxGoldenLifetimes);
        }

        public PopulationSample[] SnapshotSamples()
        {
            lock (gate)
                return samples.ToArray();
        }

        public GoldenLifetimeSample[] SnapshotGoldenLifetimes()
        {
            lock (gate)
                return goldenLifetimes.ToArray();
        }

        public GoldenAverageEvent[] SnapshotGoldenEvents()
        {
            lock (gate)
                return goldenEvents.ToArray();
        }

        private static void Push<T>(List<T> buffer, T value, int max)
        {
            buffer.Add(value);
            if (buffer.Count > max)
                buffer.RemoveAt(0);
        }
    }
}
