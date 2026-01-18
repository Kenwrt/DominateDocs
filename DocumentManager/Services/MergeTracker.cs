namespace DocumentManager.Services;

public sealed class MergeTracker
{
    private readonly TaskCompletionSource<Dictionary<Guid, byte[]>> _tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly object _lock = new();
    private int _remaining;
    private readonly Dictionary<Guid, byte[]> _mergedDocs = new(); // DocumentId -> bytes

    public MergeTracker(int expectedCount)
    {
        _remaining = expectedCount;
        if (expectedCount == 0)
            _tcs.TrySetResult(new Dictionary<Guid, byte[]>());
    }

    public Task<Dictionary<Guid, byte[]>> WhenAllMerged => _tcs.Task;

    public void CompleteOne(Guid documentId, byte[] mergedBytes)
    {
        lock (_lock)
        {
            if (_tcs.Task.IsCompleted) return; // ignore late/double calls

            _mergedDocs[documentId] = mergedBytes;
            _remaining--;

            if (_remaining <= 0)
                _tcs.TrySetResult(new Dictionary<Guid, byte[]>(_mergedDocs));
        }
    }

    public void Fail(Exception ex) => _tcs.TrySetException(ex);

    public void Cancel() => _tcs.TrySetCanceled();
}