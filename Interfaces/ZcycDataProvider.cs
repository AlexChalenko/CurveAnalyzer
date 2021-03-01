using System;
using System.Threading.Tasks;
using CurveAnalyzer.Data;

namespace CurveAnalyzer.Interfaces
{
    public interface ZcycDataProvider
    {
        Task<DateRange> GetAvailableDates();
        Task<ZcycData> GetDataForDate(DateTime date);
        Task<bool> SaveData(ZcycData data);
    }
}
