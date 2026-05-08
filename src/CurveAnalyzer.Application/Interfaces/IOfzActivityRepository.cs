using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IOfzActivityRepository
{
    Task<IReadOnlySet<DateTime>> GetLoadedDatesAsync(
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default);

    Task SaveDailyDataAsync(OfzActivityDailyData data, CancellationToken cancellationToken = default);

    Task SaveLoadStateAsync(OfzActivityLoadState state, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfzDailyTrade>> GetTradesAsync(
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfzDailyTrade>> GetIssueTradesAsync(
        string secId,
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfzLiquiditySnapshot>> GetLiquiditySnapshotsAsync(
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfzLiquiditySnapshot>> GetIssueLiquiditySnapshotsAsync(
        string secId,
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfzDailyTrade>> GetBaselineTradesAsync(
        DateTime beforeDate,
        int recordsPerIssue,
        string boardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfzIssue>> GetIssuesAsync(
        IReadOnlyCollection<string> secIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CbrKeyRate>> GetCbrKeyRatesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task SaveCbrKeyRatesAsync(
        IEnumerable<CbrKeyRate> keyRates,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfzMarketIndexPoint>> GetMarketIndexPointsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task SaveMarketIndexPointsAsync(
        IEnumerable<OfzMarketIndexPoint> points,
        CancellationToken cancellationToken = default);
}
