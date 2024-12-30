using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CurveAnalyzer.DataProviders
{
    public class HistoryDataProvider : IHistoryDataProvider
    {
        public async Task<IEnumerable<DateTime>> GetAvailableDates(CancellationToken token)
        {

            try
            {
                using MoexContext db = new();
                return await db.Zcycs.Select(_ => _.Tradedate).Distinct().ToListAsync(token);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }

            return [];
        }

        public Task<ZcycData> GetDataForDate(DateTime date)
        {
            TaskCompletionSource<ZcycData> tcs = new();

            using MoexContext db = new();

            List<Zcyc> dbData = [.. db.Zcycs.Where(r => r.Tradedate.Equals(date))];

            ZcycData zData = new()
            {
                Date = date
            };

            if (dbData.Count > 0)
            {
                for (int i = 0; i < dbData.Count; i++)
                {
                    var dbDataRow = dbData[i];
                    zData.DataRow.Add(new ZcycDataRow
                    {
                        Period = dbDataRow.Period,
                        Value = dbDataRow.Value
                    });
                }
            }
            tcs.TrySetResult(zData);

            return tcs.Task;
        }

        public async Task<IEnumerable<double>> GetPeriods()
        {
            using MoexContext db = new();
            return await db.Zcycs.Select(_ => _.Period).Distinct().ToListAsync();
        }

        public async Task<bool> SaveData(ZcycData data) //todo add error checking
        {
            if (data.DataRow == null || data.DataRow.Count == 0)
            {
                return false;
            }

            using MoexContext db = new();

            var newData = data.DataRow.Select(r => new Zcyc
            {
                Tradedate = data.Date,
                Period = r.Period,
                Value = r.Value
            });

            await db.Zcycs.AddRangeAsync(newData);
            var res = await db.SaveChangesAsync();
            return res > 0;
        }

        public async Task<IEnumerable<Zcyc>> GetDataForPeriod(double period)
        {
            using MoexContext db = new();
            return await db.Zcycs.Where(p => p.Period == period).ToListAsync();
        }
    }
}
