using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IOfzIndexDataService
{
    Task<IReadOnlyList<OfzMarketIndexPoint>> GetHistoryAsync(
        IReadOnlyCollection<string> secIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfzMarketIndexPoint>> GetCurrentSnapshotAsync(
        IReadOnlyCollection<string> secIds,
        CancellationToken cancellationToken = default);
}
