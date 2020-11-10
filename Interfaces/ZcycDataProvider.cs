using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CurveAnalyzer.Data;

namespace CurveAnalyzer.Interfaces
{
    public interface ZcycDataProvider
    {
        Task<DateRange> GetAvailableDates();
        Task<ZcycData> ReadDataForDay(DateTime date);
        Task<bool> SaveData(ZcycData data);
    }
}
