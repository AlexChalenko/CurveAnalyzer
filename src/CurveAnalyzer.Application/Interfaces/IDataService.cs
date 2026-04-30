using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IDataService
{
    Task<IReadOnlyList<DateTime>> GetAvailableDatesAsync(CancellationToken cancellationToken = default);
    Task<ZcycData> GetDataForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<double>> GetPeriodsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Zcyc>> GetDataForPeriodAsync(double period, CancellationToken cancellationToken = default);
}
