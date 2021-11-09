using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using MoexData;

namespace CurveAnalyzer.DataProviders
{
    public class HistoryDataProvider : IHistoryDataProvider
    {
        public Task<List<DateTime>> GetAvailableDates(CancellationToken token)
        {
            using MoexContext db = new();
            List<DateTime> output = new();

            try
            {
                var r = db.Zcycs.Select(_ => _.Tradedate).Distinct().ToArray();
                output.AddRange(r);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }

            return Task.FromResult(output);
            //return Task.FromResult(new DateRange(startDate, endDate));
        }

        public Task<ZcycData> GetDataForDate(DateTime date)
        {
            TaskCompletionSource<ZcycData> tcs = new();

            using MoexContext db = new();

            List<Zcyc> dbData = db.Zcycs.Where(r => r.Tradedate.Equals(date)).ToList();

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

        public Task<List<double>> GetPeriods()
        {
            using MoexContext db = new();
            return Task.FromResult(db.Zcycs.Select(_ => _.Period).Distinct().ToList());
        }

        public Task<bool> SaveData(ZcycData data) //todo add error checking
        {
            if (data.DataRow == null || data.DataRow.Count == 0)
            {
                return Task.FromResult(false);
            }

            using MoexContext db = new();
            for (int i = 0; i < data.DataRow.Count; i++)
            {
                var row = data.DataRow[i];
                db.Zcycs.Add(new Zcyc
                {
                    Tradedate = data.Date,
                    Period = row.Period,
                    Value = row.Value
                });
            }

            try
            {
                var res = db.SaveChanges();
                return Task.FromResult(res > 0);
            }
            catch (Exception ex)
            {
                Debug.Print($"Saving data to database error: {ex.InnerException}");
                return Task.FromResult(false);
            }
        }

        Task<List<Zcyc>> IDataProvider.GetDataForPeriod(double period)
        {
            using MoexContext db = new();
            var r = db.Zcycs.Where(p => p.Period == period).ToList();
            return Task.FromResult(r);
        }
    }
}
