using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using MoexData;

namespace CurveAnalyzer.DataProviders
{
    public class SQLiteDataProvider : ZcycDataProvider
    {
        public Task<ZcycData> ReadDataForDay(DateTime date)
        {
            var tcs = new TaskCompletionSource<ZcycData>();

            using var db = new MoexContext();

            var dbData = db.Zcycs.Where(r => r.Tradedate.Equals(date)).ToList();
            var zData = new ZcycData();
            zData.Date = date;
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

        public Task<DateRange> GetAvailableDates()
        {
            using var db = new MoexContext();
            long maxNum = db.Zcycs.Max(r => r.Num);
            long minNum = db.Zcycs.Min(r => r.Num);
            var startDate = db.Zcycs.Where(r => r.Num == minNum).Select(r => r.Tradedate).FirstOrDefault();
            var endDate = db.Zcycs.Where(r => r.Num == maxNum).Select(r => r.Tradedate).FirstOrDefault();
            var output = new Tuple<DateTime, DateTime>(startDate, endDate);
            return Task.FromResult(new DateRange(startDate, endDate));
        }

        public async Task<bool> SaveData(ZcycData data) //todo add error checking
        {
            if (data.Date == null || data.DataRow == null || data.DataRow.Count == 0)
            {
                return await Task.FromResult(false);
            }

            using var db = new MoexContext();
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
            var res = await db.SaveChangesAsync().ConfigureAwait(false);
            return await Task.FromResult(res > 0);
        }
    }
}
