using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IDataService
{
    Task<IEnumerable<DateTime>> GetAvailableDates(CancellationToken token);
    Task<ZcycData> GetDataForDate(DateTime date);
    Task<IEnumerable<double>> GetPeriods();
    Task<IEnumerable<Zcyc>> GetDataForPeriod(double period);
}
