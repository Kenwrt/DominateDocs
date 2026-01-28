using System.Threading.Channels;

namespace DocumentManager.Infrastructure;

public sealed class ChannelJobQueue<T> : IJobQueue<T>
{
    private readonly Channel<T> _channel;

    public ChannelJobQueue(int capacity)
    {
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(T job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public async IAsyncEnumerable<T> DequeueAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        while (await _channel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var item))
                yield return item;
        }
    }
}
