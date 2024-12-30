using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.Application;

public class HistoryDataService : IHistoryDataService
{
    private readonly IZcycRepository _repository;

    public HistoryDataService(IZcycRepository repository)
    {
        _repository = repository;
    }

    public  Task<IEnumerable<DateTime>> GetAvailableDates(CancellationToken token)
    {
        return _repository.GetAllDatesAsync();
    }

    public Task<ZcycData> GetDataForDate(DateTime date)
    {
        return _repository.GetByDateAsync(date);
    }

    public Task<IEnumerable<double>> GetPeriods()
    {
        return _repository.GetPeriodsAsync();
    }

    public async Task<bool> SaveData(ZcycData data) //todo add error checking
    {
        if (data.DataRow == null || data.DataRow.Count == 0)
        {
            return false;
        }

        var newData = data.DataRow.Select(r => new Zcyc
        {
            Tradedate = data.Date,
            Period = r.Period,
            Value = r.Value
        });

        return await _repository.AddRangeAsync(newData);
    }

    public Task<IEnumerable<Zcyc>> GetDataForPeriod(double period)
    {
        return _repository.GetDataForPeriod(period);
    }
}
