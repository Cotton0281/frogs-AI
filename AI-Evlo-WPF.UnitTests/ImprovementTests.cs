using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using AI_Evlo_Test;
using AI_Evlo_Test.Objects;
using AI_Evlo_Test.Persistence;
using AI_Evlo_Test.ConfigLib;
using AI_Evlo_Test.Enumerators;

namespace AI_Evlo_WPF.UnitTests;

[TestClass]
public class ImprovementTests
{
    [TestMethod]
    public void SpatialPerception_MatchesFullScanIncludingTiesAndLargeObjects()
    {
        var random = new Random(123);
        var categories = Enum.GetValues<ObjectCategory>();
        var scene = Enumerable.Range(0, 1000).Select(i => new SensableSnapshot(i.ToString(),
            new Point(random.NextDouble() * 3000 - 500, random.NextDouble() * 2000 - 500),
            random.Next(10, 160), categories[i % categories.Length])).ToList();
        scene.Add(new SensableSnapshot("tie1", new Point(0, 0), 20, ObjectCategory.Bird));
        scene.Add(new SensableSnapshot("tie2", new Point(0, 0), 20, ObjectCategory.Frog));
        var index = new SpatialPerceptionIndex(); index.Rebuild(scene);
        var full = new RayPerception(centerRayMultiplier: 1.5);
        var indexed = new RayPerception(centerRayMultiplier: 1.5);
        var nearby = index.Query(new Point(0, 0), 375, new List<int>(), new List<SensableSnapshot>());
        Assert.AreNotSame(scene, nearby);
        Assert.IsLessThan(scene.Count, nearby.Count);
        for (int phase = 0; phase < 2; phase++)
        {
            if (phase == 1)
            {
                scene.Add(new SensableSnapshot("large", new Point(750, 0), 1100, ObjectCategory.Raft));
                index.Rebuild(scene);
                Assert.AreSame(scene, index.Query(new Point(0, 0), 375, new List<int>(), new List<SensableSnapshot>()));
            }
        for (int i = 0; i < 100; i++)
        {
            Point origin = i == 0 ? new Point(0, 0) : new Point(random.Next(-500, 2500), random.Next(-500, 1500));
            Vector facing = new Vector(Math.Cos(i), Math.Sin(i));
            var ignored = i % 2 == 0 ? new[] { ObjectCategory.Shark } : null;
            full.Update(origin, facing, scene, i.ToString(), ignored);
            indexed.Update(origin, facing, index, i.ToString(), ignored);
            CollectionAssert.AreEqual(full.Signals, indexed.Signals);
            for (int r = 0; r < full.RayCount; r++)
                for (int h = 0; h < RayPerception.HitsPerRay; h++)
                    Assert.AreEqual(full.HitLayers[r, h], indexed.HitLayers[r, h]);
        }
        }
        index.Rebuild(Array.Empty<SensableSnapshot>());
        indexed.Update(new Point(), new Vector(1, 0), index);
        Assert.IsTrue(indexed.Signals.All(s => s == 0));
    }

    [TestMethod]
    public void History_RetainsNewestValuesAcrossWrapAndResize()
    {
        var history = new BoundedHistory<int>();
        for (int i = 0; i < 10; i++) history.Add(i, 3);
        CollectionAssert.AreEqual(new[] { 7, 8, 9 }, history.Snapshot());
        history.Add(10, 2);
        CollectionAssert.AreEqual(new[] { 9, 10 }, history.Snapshot());
        history.Add(11, 4);
        CollectionAssert.AreEqual(new[] { 9, 10, 11 }, history.Snapshot());
        history.Add(12, 0);
        Assert.HasCount(0, history.Snapshot());
    }

    [TestMethod]
    public void GoldenEvents_RevisionAdvancesAfterBufferFills()
    {
        var stats = new PopulationStats { MaxGoldenEvents = 2 };
        for (int i = 0; i < 3; i++) stats.RecordGoldenAverage(i, i + 1, "survivor", 50);
        Assert.AreEqual(3L, stats.EventRevision);
        CollectionAssert.AreEqual(new[] { 1, 2 }, stats.SnapshotGoldenEvents().Select(e => e.Cycle).ToArray());
    }

    [TestMethod]
    public void Csv_QuotesNamesAndUsesInvariantNumbers()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            string csv = PopulationCsv.Export(new PopulationDashboardSnapshot
            {
                Name = "Frogs, \"A\"", Species = "Frogs", RecordingStartedCycle = 100,
                Series = new[] { new PopulationSample { Cycle = 125, Alive = 2, MeanFitness = 1.5 } }
            });
            StringAssert.Contains(csv, "\"Frogs, \"\"A\"\"\"");
            StringAssert.Contains(csv, ",100,125,2,0,0,1.5,0");
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [TestMethod]
    public void Scenario_CapturesSettingsWithoutLearnedBrainsOrRuntimeHistory()
    {
        var population = new Population
        {
            Name = "Frogs", SizeLimit = 10, Being = PopulationBeing.Frog,
            NeuroNetTemplate = NeuroNetStructure.Small_1Lx9N(),
            MutationRate = 3, SpawnDelay = false, PauseMutation = true, Stats = new PopulationStats(),
            SurvivalRecordCycles = 500
        };
        var preset = ScenarioPreset.Capture("Trial", EEnvironmentType.TwoTargets, new MovementSettings(), new[] { population });
        preset.Validate();
        var copy = preset.Populations.Single();
        Assert.AreEqual(3, copy.MutationRate);
        Assert.IsFalse(copy.SpawnDelay);
        Assert.IsTrue(copy.PauseMutation);
        Assert.AreEqual(0, copy.SurvivalRecordCycles);
        Assert.IsNull(copy.GoldenAgentGene);
        Assert.IsNull(copy.Stats);
        Assert.HasCount(0, copy.Members);
        population.NeuroNetTemplate.HiddenLayers = 7;
        Assert.AreEqual(1, copy.NeuroNetTemplate.HiddenLayers);
    }

    [TestMethod]
    public void Scenario_RejectsInvalidSettingsBeforeChangingTheWorld()
    {
        var preset = new ScenarioPreset { Version = 2 };
        Assert.Throws<System.IO.InvalidDataException>(() => preset.Validate());
        preset.Version = 1;
        preset.Populations.Add(new Population { Name = "bad", SizeLimit = 0 });
        Assert.Throws<System.IO.InvalidDataException>(() => preset.Validate());
    }

    [TestMethod]
    public void Scenario_FileRoundTripRetainsSettingsAndClearsEvolution()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".scenario.json");
        try
        {
            var p = new Population { Name = "Birds", Being = PopulationBeing.Bird, SizeLimit = 5,
                NeuroNetTemplate = NeuroNetStructure.Mid_3Lx10N(), AutoGrowNeuralNetwork = true,
                LayerLocks = new List<bool> { true, false, false, true } };
            var preset = ScenarioPreset.Capture("Bird trial", EEnvironmentType.TwoTargets, new MovementSettings { BiteCooldownTicks = 8 }, new[] { p });
            // Even a manually edited preset cannot smuggle runtime evolution into a fresh run.
            preset.Populations[0].SurvivalRecordCycles = 900;
            preset.Save(path);
            var loaded = ScenarioPreset.Load(path);
            Assert.AreEqual("Bird trial", loaded.Name);
            Assert.AreEqual("Medium", loaded.Populations[0].NeuroNetTemplate.Id);
            Assert.AreEqual(8, loaded.Movement.BiteCooldownTicks);
            Assert.IsTrue(loaded.Populations[0].AutoGrowNeuralNetwork);
            CollectionAssert.AreEqual(p.LayerLocks, loaded.Populations[0].LayerLocks);
            Assert.AreEqual(0, loaded.Populations[0].SurvivalRecordCycles);
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("Performance")]
    public void PerceptionBenchmark_ReportsFullScanAndIndexedCost()
    {
        foreach (int count in new[] { 100, 500, 1000 })
        {
            var random = new Random(77);
            var scene = Enumerable.Range(0, count).Select(i => new SensableSnapshot(i.ToString(),
                new Point(random.NextDouble() * 2000, random.NextDouble() * 1200), 32,
                (ObjectCategory)new[] { 1, 5, 6 }[i % 3])).ToArray();
            var full = Enumerable.Range(0, count).Select(_ => new RayPerception()).ToArray();
            var indexed = Enumerable.Range(0, count).Select(_ => new RayPerception()).ToArray();
            var index = new SpatialPerceptionIndex();
            index.Rebuild(scene);
            for (int i = 0; i < count; i++)
            {
                full[i].Update(scene[i].Location, new Vector(1, 0), scene, scene[i].Id);
                indexed[i].Update(scene[i].Location, new Vector(1, 0), index, scene[i].Id);
            }
            const int ticks = 60;
            var timer = Stopwatch.StartNew();
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 0; tick < ticks; tick++)
                for (int i = 0; i < count; i++) full[i].Update(scene[i].Location, new Vector(1, 0), scene, scene[i].Id);
            double baseline = timer.Elapsed.TotalMilliseconds / ticks;
            long baselineBytes = (GC.GetAllocatedBytesForCurrentThread() - bytes) / ticks;
            timer.Restart(); bytes = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 0; tick < ticks; tick++)
            {
                index.Rebuild(scene);
                for (int i = 0; i < count; i++) indexed[i].Update(scene[i].Location, new Vector(1, 0), index, scene[i].Id);
            }
            TestContext.WriteLine($"{count} agents: full scan {baseline:F2} ms/tick, {baselineBytes} B/tick; indexed {timer.Elapsed.TotalMilliseconds / ticks:F2} ms/tick, {(GC.GetAllocatedBytesForCurrentThread() - bytes) / ticks} B/tick");
        }
    }
}
