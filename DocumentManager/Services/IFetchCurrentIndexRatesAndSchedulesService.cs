namespace DocumentManager.Services;

public interface IFetchCurrentIndexRatesAndSchedulesService
{
    FetchCurrentIndexRatesAndSchedulesService.ScheduleResult GenerateProjectedSchedule(FetchCurrentIndexRatesAndSchedulesService.LoanTerms terms);

    Task<decimal?> GetLatestFromFredAsync(string fredSeriesId, string fredApiKey, CancellationToken ct = default);

    Task<decimal?> GetLatestSofrAsync(CancellationToken ct = default);
}