using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IZcycRepository
{
    Task<ZcycData> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DateTime>> GetAllDatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<double>> GetPeriodsAsync(CancellationToken cancellationToken = default);
    Task<bool> AddRangeAsync(IEnumerable<Zcyc> newData, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Zcyc>> GetDataForPeriodAsync(double period, CancellationToken cancellationToken = default);
}
