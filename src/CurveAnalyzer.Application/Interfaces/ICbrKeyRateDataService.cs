using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface ICbrKeyRateDataService
{
    Task<IReadOnlyList<CbrKeyRate>> GetKeyRatesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
