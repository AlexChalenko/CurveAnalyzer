using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
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
        public Task<ZcycData> GetDataForDate(DateTime date)
        {
            var tcs = new TaskCompletionSource<ZcycData>();

            using var db = new MoexContext();
            //db.Database.EnsureCreated();

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

        public Task<List<DateTime>> GetAvailableDates()
        {
            //DateTime startDate = DateTime.MinValue;
            //DateTime endDate = DateTime.Now.AddDays(-1);

            using var db = new MoexContext();
            //db.Database.EnsureCreated();

            List<DateTime> output = new();

            try
            {
                var r = db.Zcycs.Select(_ => _.Tradedate).Distinct().ToArray();
                output.AddRange(r);

                //long maxNum = db.Zcycs.Max(r => r.Num);
                //long minNum = db.Zcycs.Min(r => r.Num);
                //startDate = db.Zcycs.Where(r => r.Num == minNum).Select(r => r.Tradedate).FirstOrDefault();
                //endDate = db.Zcycs.Where(r => r.Num == maxNum).Select(r => r.Tradedate).FirstOrDefault();
                //var output = new Tuple<DateTime, DateTime>(startDate, endDate);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }

            return Task.FromResult(output);
            //return Task.FromResult(new DateRange(startDate, endDate));
        }

        public Task<bool> SaveData(ZcycData data) //todo add error checking
        {
            if (data.DataRow == null || data.DataRow.Count == 0)
            {
                return Task.FromResult(false);
            }

            using var db = new MoexContext();
            //db.Database.EnsureCreated();
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
    }
}
