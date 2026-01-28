using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocumentManager.Infrastructure;

public abstract class WorkerPoolBackgroundService<TJob> : BackgroundService
{
    private readonly IJobQueue<TJob> _queue;
    private readonly ILogger _logger;
    private readonly int _workers;

    protected WorkerPoolBackgroundService(IJobQueue<TJob> queue, ILogger logger, int workers)
    {
        _queue = queue;
        _logger = logger;
        _workers = Math.Max(1, workers);
    }

    protected abstract Task HandleAsync(TJob job, CancellationToken ct);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new Task[_workers];

        for (int i = 0; i < _workers; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await foreach (var job in _queue.DequeueAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        await HandleAsync(job, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "WorkerPool failed for {JobType}", typeof(TJob).Name);
                        await OnJobFailedAsync(job, ex, stoppingToken).ConfigureAwait(false);
                    }
                }
            }, stoppingToken);
        }

        return Task.WhenAll(tasks);
    }

    protected virtual Task OnJobFailedAsync(TJob job, Exception ex, CancellationToken ct)
        => Task.CompletedTask;
}
