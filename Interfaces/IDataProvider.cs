using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CurveAnalyzer.Data;
using CurveAnalyzer.Data;

namespace CurveAnalyzer.Interfaces
{
    public interface IDataProvider
    {
        Task<IEnumerable<DateTime>> GetAvailableDates(CancellationToken token);
        Task<ZcycData> GetDataForDate(DateTime date);
        Task<IEnumerable<double>> GetPeriods();
        Task<IEnumerable<Zcyc>> GetDataForPeriod(double period);
    }
}
