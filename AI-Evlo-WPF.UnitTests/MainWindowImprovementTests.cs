using Size = System.Windows.Size;
using CheckBox = System.Windows.Controls.CheckBox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AI_Evlo_Test;
using AI_Evlo_Test.Objects;
using AI_Evlo_Test.Enumerators;

namespace AI_Evlo_WPF.UnitTests;

[STATestClass]
public class MainWindowImprovementTests
{
    public TestContext TestContext { get; set; } = null!;
    [TestCleanup]
    public void RestoreDefaultMovementSettings()
    {
        SmartObject.MovementSettings = new MovementSettings();
        if (MainWindow.SessionDirectoryOverride != null)
            MainWindow.SaveMovementSettingsToPath(Path.Combine(MainWindow.SessionDirectoryOverride, "movement-settings.json"), SmartObject.MovementSettings);
    }

    private static object? Invoke(MainWindow window, string method, params object[] args) =>
        typeof(MainWindow).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);
    private static T Field<T>(MainWindow window, string name) =>
        (T)typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
    private static Population Seed(MainWindow window, int count, string name, PopulationBeing being)
    {
        var population = (Population)Invoke(window, "CreatePopulation", count, name, "Small", being)!;
        Invoke(window, "RegisterPopulation", population);
        return population;
    }

    [TestMethod]
    public void ScenarioApply_ReplacesPopulationSetAndStartsPausedWithFreshBrains()
    {
        var window = new MainWindow();
        try
        {
            Invoke(window, "InitTargets"); Seed(window, 3, "Old", PopulationBeing.Frog);
            var preset = AI_Evlo_Test.Persistence.ScenarioPreset.Capture("Bird trial", EEnvironmentType.OneTarget,
                new MovementSettings { BiteCooldownTicks = 8 }, new[] { new Population
                { Name = "New", SizeLimit = 5, Being = PopulationBeing.Bird,
                    NeuroNetTemplate = AI_Evlo_Test.ConfigLib.NeuroNetStructure.Small_1Lx9N() } });
            window.ApplyScenarioPreset(preset);
            Assert.HasCount(1, window.lsPopulations);
            Assert.AreEqual("New", window.lsPopulations[0].Name);
            Assert.HasCount(5, window.lsPopulations[0].Members);
            Assert.AreEqual(0, Field<int>(window, "CycleCount"));
            Assert.IsFalse(Field<AI.Evlo.Core.Simulation.SimulationRunner>(window, "simulationRunner").IsRunning);
            Assert.HasCount(1, Field<List<TargetObj>>(window, "Targets"));
            Assert.AreEqual(8, SmartObject.MovementSettings.BiteCooldownTicks);
            Assert.IsNull(window.lsPopulations[0].GoldenAgentGene);
            Invoke(window, "OnRendering", window, EventArgs.Empty);
        }
        finally { window.Close(); SmartObject.MovementSettings = new MovementSettings(); }
    }

    [TestMethod]
    public void ScenarioApply_InvalidPresetKeepsExistingPopulation()
    {
        var window = new MainWindow();
        try
        {
            var original = Seed(window, 1, "Existing", PopulationBeing.Frog);
            Assert.Throws<InvalidDataException>(() => window.ApplyScenarioPreset(new AI_Evlo_Test.Persistence.ScenarioPreset { Version = 7 }));
            Assert.AreSame(original, window.lsPopulations.Single());
        }
        finally { window.Close(); }
    }
    [TestMethod]
    public void Sidebar_EditorAndInspectorShareSpaceAndInvalidSizeIsRejected()
    {
        var window = new MainWindow();
        try
        {
            var editor = (Expander)window.FindName("populationEditor");
            var inspector = (Expander)window.FindName("inspectorPanel");
            editor.IsExpanded = true;
            Assert.IsFalse(inspector.IsExpanded);
            Invoke(window, "ShowAgentInspector");
            Assert.IsTrue(inspector.IsExpanded);
            Assert.IsFalse(editor.IsExpanded);
            ((System.Windows.Controls.TextBox)window.FindName("txtPopulationSize")).Text = "invalid";
            Invoke(window, "BtnNewPopulation_Click", window, new RoutedEventArgs());
            Assert.HasCount(0, window.lsPopulations);
            Assert.IsFalse(string.IsNullOrEmpty(((System.Windows.Controls.TextBlock)window.FindName("populationValidation")).Text));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void Dashboard_RefreshesEventsAfterCapacityAndSupportsAllTabs()
    {
        var snapshot = new PopulationDashboardSnapshot
        {
            Name = "Frogs", Species = "Frogs", SizeLimit = 10, EventRevision = 2,
            Series = new[] { new PopulationSample { Cycle = 100, Alive = 10 }, new PopulationSample { Cycle = 125, Alive = 9 } },
            GoldenEvents = new[] { new GoldenAverageEvent { Cycle = 10, AverageCount = 1 }, new GoldenAverageEvent { Cycle = 20, AverageCount = 2 } }
        };
        using var dashboard = new PopulationDashboard("Test", () => new List<DashboardPopulationOption>
        { new() { Id = "p", Name = "Frogs", Species = "Frogs" } }, _ => snapshot, (_, _) => { }, "p");
        var tabs = (System.Windows.Forms.TabControl)typeof(PopulationDashboard).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(dashboard)!;
        var refresh = typeof(PopulationDashboard).GetMethod("RenderCurrent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        tabs.SelectedIndex = 1;
        refresh.Invoke(dashboard, null);
        snapshot.EventRevision = 3;
        snapshot.GoldenEvents = new[] { new GoldenAverageEvent { Cycle = 20, AverageCount = 2 }, new GoldenAverageEvent { Cycle = 30, AverageCount = 3 } };
        refresh.Invoke(dashboard, null);
        var events = (System.Windows.Forms.ListBox)typeof(PopulationDashboard).GetField("lstEvents", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(dashboard)!;
        StringAssert.Contains(events.Items[0]!.ToString()!, "cycle 30");
        tabs.SelectedIndex = 2; refresh.Invoke(dashboard, null);
        tabs.SelectedIndex = 0; refresh.Invoke(dashboard, null);
    }

    [TestMethod]
    public void Rendering_UsesCapturedPositionsAndRemovesRetiredVisuals()
    {
        var window = new MainWindow();
        try
        {
            Invoke(window, "InitTargets");
            var population = Seed(window, 3, "Frogs", PopulationBeing.Frog);
            var agent = population.Members[0];
            agent.SetLocation(200, 200);
            var frame = Invoke(window, "CaptureWorldFrame")!;
            agent.SetLocation(400, 400);
            Invoke(window, "PaintWorldFrame", frame);
            Assert.AreEqual(184, Canvas.GetLeft(agent.VisibleShape), 0.001);
            var visual = agent.VisibleShape;
            Invoke(window, "DisposeObject", agent);
            Invoke(window, "PaintWorldFrame", Invoke(window, "CaptureWorldFrame")!);
            Assert.IsFalse(Field<Dictionary<FrameworkElement, ISmartObject>>(window, "shapeToObjectMap").ContainsKey(visual));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void Simulation_HeadlessTransitionsStopAndSaveCleanly()
    {
        var window = new MainWindow();
        try
        {
            Invoke(window, "InitTargets");
            Seed(window, 30, "Frogs", PopulationBeing.Frog);
            Seed(window, 5, "Birds", PopulationBeing.Bird);
            Invoke(window, "StartSimulation");
            Assert.IsTrue(SpinWait.SpinUntil(() => Field<int>(window, "CycleCount") > 2, 2000));
            var headless = (CheckBox)window.FindName("chkHeadless");
            headless.IsChecked = true;
            int cycle = Field<int>(window, "CycleCount");
            Assert.IsTrue(SpinWait.SpinUntil(() => Field<int>(window, "CycleCount") > cycle + 2, 2000));
            headless.IsChecked = false;
            Invoke(window, "StopSimulation");
            var runner = Field<AI.Evlo.Core.Simulation.SimulationRunner>(window, "simulationRunner");
            Assert.IsTrue(runner.Stop(TimeSpan.FromSeconds(2)));
            Invoke(window, "OnRendering", window, EventArgs.Empty);
            Assert.IsFalse(runner.IsRunning);
            int stopped = Field<int>(window, "CycleCount");
            Thread.Sleep(20);
            Assert.AreEqual(stopped, Field<int>(window, "CycleCount"));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [DataRow(900, 600, 96)]
    [DataRow(1280, 800, 96)]
    [DataRow(900, 600, 120)]
    [DataRow(900, 600, 144)]
    [DataRow(900, 600, 192)]
    public void Layout_RendersAtMinimumSizeAndScaledDpi(int width, int height, int dpi)
    {
        var window = new MainWindow();
        try
        {
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(width, height));
            root.Arrange(new Rect(0, 0, width, height));
            root.UpdateLayout();
            Invoke(window, "InitTargets");
            Seed(window, 50, "Frogs", PopulationBeing.Frog);
            Seed(window, 10, "Birds", PopulationBeing.Bird);
            Seed(window, 5, "Sharks", PopulationBeing.Shark);
            for (int i = 0; i < 10; i++) Invoke(window, "SimulationTick");
            Invoke(window, "OnRendering", window, EventArgs.Empty);
            Invoke(window, "UpdateLabbels");
            root.Measure(new Size(width, height)); root.Arrange(new Rect(0, 0, width, height)); root.UpdateLayout();
            var canvas = (Canvas)window.FindName("panlUniverseView");
            Assert.IsGreaterThan(280.0, canvas.ActualWidth);
            Assert.IsGreaterThan(200.0, canvas.ActualHeight);
            var list = (ScrollViewer)window.FindName("ScrollPopulations");
            Assert.IsGreaterThan(20.0, list.ActualHeight);
            Assert.IsGreaterThan(1, ((BitmapSource)FrogSheetCache.Frame(0)).PixelWidth);
            var bitmap = new RenderTargetBitmap(width * dpi / 96, height * dpi / 96, dpi, dpi, PixelFormats.Pbgra32);
            bitmap.Render(root);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../TestResults/ui-previews"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"main-{width}x{height}-{dpi}dpi.png");
            using (var stream = File.Create(path)) encoder.Save(stream);
            TestContext.AddResultFile(path);
            TestContext.WriteLine(path);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [TestCategory("Performance")]
    public void SimulationAndRenderingBenchmark_ReportsTickAndFrameCost()
    {
        foreach (int count in new[] { 100, 500, 1000 })
        {
            var window = new MainWindow();
            try
            {
                Invoke(window, "InitTargets"); Seed(window, count, "Frogs", PopulationBeing.Frog);
                for (int i = 0; i < 5; i++) Invoke(window, "SimulationTick");
                var timer = Stopwatch.StartNew();
                for (int i = 0; i < 20; i++) Invoke(window, "SimulationTick");
                double tickMs = timer.Elapsed.TotalMilliseconds / 20;
                Invoke(window, "PaintWorldFrame", Invoke(window, "CaptureWorldFrame")!);
                timer.Restart(); long allocated = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 20; i++) Invoke(window, "PaintWorldFrame", Invoke(window, "CaptureWorldFrame")!);
                TestContext.WriteLine($"{count} agents: model {tickMs:F2} ms/tick ({1000 / tickMs:F0} ticks/s); snapshot + visual updates {timer.Elapsed.TotalMilliseconds / 20:F2} ms/frame, {(GC.GetAllocatedBytesForCurrentThread() - allocated) / 20} B/frame (excludes display composition).");
            }
            finally { window.Close(); }
        }
    }
}
