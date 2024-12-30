using System.Collections.ObjectModel;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IDataManager
{
    Collection<DateTime> BlackoutDates { get; }
    Task UpdateDataASync(Progress<double> progress);
    Task<IEnumerable<double>> GetPeriodsAsync();
    Task<IEnumerable<Zcyc>> GetZcycForPeriodAsync(double period);
    Task<DateRange> GetAvailableDatesAsync();
    Task<ZcycData> GetDataAsync(DateTime value);

}
