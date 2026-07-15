using AI.Evlo.Core.Simulation;
using System.Threading;

namespace AI_Evlo_WPF.UnitTests.Simulation;

[TestClass]
public class SimulationRunnerTests
{
    [TestMethod]
    public void Start_ExecutesBatchesUntilStopped()
    {
        using var executed = new ManualResetEventSlim();
        int ticks = 0;
        using var runner = new SimulationRunner(
            batch =>
            {
                if (Interlocked.Add(ref ticks, batch) >= 3)
                    executed.Set();
            },
            () => 3,
            () => 1);

        runner.Start();

        Assert.IsTrue(executed.Wait(TimeSpan.FromSeconds(2)));
        Assert.IsTrue(runner.Stop());
        Assert.IsFalse(runner.IsRunning);
        Assert.IsGreaterThanOrEqualTo(3, ticks);
    }

    [TestMethod]
    public void Start_WhenAlreadyRunning_DoesNotCreateConcurrentLoop()
    {
        using var gate = new ManualResetEventSlim();
        int concurrent = 0;
        int maximumConcurrent = 0;
        using var runner = new SimulationRunner(
            _ =>
            {
                int active = Interlocked.Increment(ref concurrent);
                InterlockedExtensions.Max(ref maximumConcurrent, active);
                gate.Wait(TimeSpan.FromMilliseconds(20));
                Interlocked.Decrement(ref concurrent);
            },
            () => 1,
            () => 0);

        runner.Start();
        runner.Start();
        Thread.Sleep(30);
        gate.Set();
        runner.Stop();

        Assert.AreEqual(1, maximumConcurrent);
    }
}

internal static class InterlockedExtensions
{
    internal static void Max(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value &&
               Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }
}
