using DocumentManager.Infrastructure;
using System.Threading.Channels;

public sealed class ChannelJobQueue<T> : IJobQueue<T>
{
    private readonly Channel<T> _channel;

    public ChannelJobQueue(int capacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait, // backpressure
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<T>(options);
    }

    public ValueTask EnqueueAsync(T job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public async IAsyncEnumerable<T> DequeueAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        while (await _channel.Reader.WaitToReadAsync(ct))
        {
            while (_channel.Reader.TryRead(out var item))
                yield return item;
        }
    }
}
