using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IOfzCashflowDataService
{
    Task<IReadOnlyList<OfzCashflowEvent>> GetScheduleAsync(
        IReadOnlyCollection<string> secIds,
        CancellationToken cancellationToken = default);
}
