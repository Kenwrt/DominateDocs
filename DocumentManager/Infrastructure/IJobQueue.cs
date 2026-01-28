namespace DocumentManager.Infrastructure;

public interface IJobQueue<T>
{
    ValueTask EnqueueAsync(T job, CancellationToken ct = default);
    IAsyncEnumerable<T> DequeueAllAsync(CancellationToken ct);
}
