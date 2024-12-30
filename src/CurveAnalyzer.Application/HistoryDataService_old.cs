using CurveAnalyzer.Core;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.Application;

public class HistoryDataService_old/* : IHistoryDataService*/
{
    ////private readonly MoexContext _moexContext;

    //public HistoryDataService_old(/*MoexContext moexContext*/)
    //{
    //    _moexContext = new MoexContext();
    //}

    //public async Task<IEnumerable<DateTime>> GetAvailableDates(CancellationToken token)
    //{
    //    return await _moexContext.Zcycs.Select(_ => _.Tradedate).Distinct().ToListAsync(token);
    //}

    //public Task<ZcycData> GetDataForDate(DateTime date)
    //{
    //    TaskCompletionSource<ZcycData> tcs = new();

    //    List<Zcyc> dbData = [.. _moexContext.Zcycs.Where(r => r.Tradedate.Equals(date))];

    //    ZcycData zData = new()
    //    {
    //        Date = date
    //    };

    //    if (dbData.Count > 0)
    //    {
    //        for (int i = 0; i < dbData.Count; i++)
    //        {
    //            var dbDataRow = dbData[i];
    //            zData.DataRow.Add(new ZcycDataRow(dbDataRow.Period, dbDataRow.Value));
    //        }
    //    }
    //    tcs.TrySetResult(zData);

    //    return tcs.Task;
    //}

    //public async Task<IEnumerable<double>> GetPeriods()
    //{
    //    return await _moexContext.Zcycs.Select(_ => _.Period).Distinct().ToListAsync();
    //}

    //public async Task<bool> SaveData(ZcycData data) //todo add error checking
    //{
    //    if (data.DataRow == null || data.DataRow.Count == 0)
    //    {
    //        return false;
    //    }

    //    var newData = data.DataRow.Select(r => new Zcyc
    //    {
    //        Tradedate = data.Date,
    //        Period = r.Period,
    //        Value = r.Value
    //    });

    //    await _moexContext.Zcycs.AddRangeAsync(newData);
    //    var res = await _moexContext.SaveChangesAsync();
    //    return res > 0;
    //}

    //public async Task<IEnumerable<Zcyc>> GetDataForPeriod(double period)
    //{
    //    using MoexContext db = new();
    //    return await db.Zcycs.Where(p => p.Period == period).ToListAsync();
    //}
}
