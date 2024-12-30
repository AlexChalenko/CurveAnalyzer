using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IZcycRepository
{
    Task<ZcycData> GetByDateAsync(DateTime date);
    Task<IEnumerable<DateTime>> GetAllDatesAsync();
    Task<IEnumerable<double>> GetPeriodsAsync();
    Task<bool> AddRangeAsync(IEnumerable<Zcyc> newData);
    Task<IEnumerable<Zcyc>> GetDataForPeriod(double period);
}
