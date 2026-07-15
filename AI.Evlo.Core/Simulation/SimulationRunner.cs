using System.Diagnostics;

namespace AI.Evlo.Core.Simulation;

/// <summary>
/// Owns the lifetime of the single background thread that advances a simulation.
/// The supplied batch operation is never invoked concurrently.
/// </summary>
public sealed class SimulationRunner : IDisposable
{
    private readonly Action<int> executeBatch;
    private readonly Func<int> batchSize;
    private readonly Func<int> delayMilliseconds;
    private readonly object lifecycleGate = new();
    private Thread? thread;
    private CancellationTokenSource? cancellation;

    public SimulationRunner(Action<int> executeBatch, Func<int> batchSize, Func<int> delayMilliseconds)
    {
        this.executeBatch = executeBatch ?? throw new ArgumentNullException(nameof(executeBatch));
        this.batchSize = batchSize ?? throw new ArgumentNullException(nameof(batchSize));
        this.delayMilliseconds = delayMilliseconds ?? throw new ArgumentNullException(nameof(delayMilliseconds));
    }

    public bool IsRunning
    {
        get
        {
            lock (lifecycleGate)
                return thread?.IsAlive == true && cancellation?.IsCancellationRequested == false;
        }
    }

    public void Start()
    {
        lock (lifecycleGate)
        {
            if (thread?.IsAlive == true)
                return;

            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            CancellationToken token = cancellation.Token;
            thread = new Thread(() => Run(token))
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "SimulationLoop"
            };
            thread.Start();
        }
    }

    public bool Stop(TimeSpan? timeout = null)
    {
        Thread? activeThread;
        lock (lifecycleGate)
        {
            cancellation?.Cancel();
            activeThread = thread;
        }

        if (activeThread == null || !activeThread.IsAlive || ReferenceEquals(activeThread, Thread.CurrentThread))
            return true;

        return activeThread.Join(timeout ?? TimeSpan.FromSeconds(1));
    }

    public void Dispose()
    {
        Stop();
        lock (lifecycleGate)
        {
            cancellation?.Dispose();
            cancellation = null;
            thread = null;
        }
    }

    private void Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            executeBatch(Math.Max(1, batchSize()));
            int delay = Math.Max(0, delayMilliseconds());
            if (delay > 0 && token.WaitHandle.WaitOne(delay))
                break;
        }
    }
}
