namespace DocumentManager.Services;

public interface ISofrForwardCurveService
{
    Task<IReadOnlyList<decimal>> GetProjectedSofrCurveAsync(decimal spotSofrPercent, int resetsNeeded, CancellationToken ct = default);
}