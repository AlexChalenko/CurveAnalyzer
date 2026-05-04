namespace CurveAnalyzer.Application.Interfaces;

public interface IOfzActivityDataService
{
    Task<OfzActivityDailyData> GetHistoryForDateAsync(DateTime date, CancellationToken cancellationToken = default);

    Task<OfzActivityDailyData> GetCurrentSnapshotAsync(CancellationToken cancellationToken = default);
}
