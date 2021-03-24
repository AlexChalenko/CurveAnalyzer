using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CurveAnalyzer.Data;
using MoexData;

namespace CurveAnalyzer.Interfaces
{
    internal interface IDataProvider
    {
        Task<List<DateTime>> GetAvailableDates();

        Task<ZcycData> GetDataForDate(DateTime date);

        Task<List<double>> GetPeriods();

        Task<bool> SaveData(ZcycData data);

        Task<List<Zcyc>> GetDataForPeriod(double period);
    }
}
