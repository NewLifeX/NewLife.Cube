using System.Collections.Concurrent;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Cube.Automation;

/// <summary>消费 queued 运行</summary>
public class AutomationWorker : IHostedService
{
    static readonly ConcurrentQueue<Int64> _queue = new();
    TimerX _timer;

    /// <summary>投递运行 Id</summary>
    public static void Post(Int64 runId)
    {
        if (runId <= 0) return;
        if (AutomationRuntime.Immediate)
        {
            var run = AutomationRun.FindById(runId);
            if (run != null) AutomationExecutor.Execute(run);
            return;
        }
        _queue.Enqueue(runId);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new TimerX(DoWork, null, 800, 500) { Async = true };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();
        return Task.CompletedTask;
    }

    void DoWork(Object state)
    {
        var n = 0;
        while (n++ < 20 && _queue.TryDequeue(out var id))
        {
            try
            {
                var run = AutomationRun.FindById(id);
                if (run != null && run.Status.EqualIgnoreCase("queued"))
                    AutomationExecutor.Execute(run);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
    }
}
