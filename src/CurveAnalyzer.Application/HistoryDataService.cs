using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application;

public class HistoryDataService : IHistoryDataService
{
    private readonly IZcycRepository _repository;

    public HistoryDataService(IZcycRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<DateTime>> GetAvailableDatesAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllDatesAsync(cancellationToken);
    }

    public Task<ZcycData> GetDataForDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return _repository.GetByDateAsync(date, cancellationToken);
    }

    public Task<IReadOnlyList<double>> GetPeriodsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetPeriodsAsync(cancellationToken);
    }

    public async Task<bool> SaveDataAsync(ZcycData data, CancellationToken cancellationToken = default)
    {
        if (data.DataRow.Count == 0)
        {
            return false;
        }

        var curve = new YieldCurve(
            new TradingDate(data.Date),
            data.DataRow.Select(row => new YieldPoint(new TradingDate(data.Date), new CurvePeriod(row.Period), row.Value)));

        var newData = curve.Points.Select(point => new Zcyc
        {
            Tradedate = curve.TradingDate.Date,
            Period = point.Period.Value,
            Value = point.Value
        });

        return await _repository.AddRangeAsync(newData, cancellationToken);
    }

    public async Task<IReadOnlyList<Zcyc>> GetDataForPeriodAsync(double period, CancellationToken cancellationToken = default)
    {
        var data = await _repository.GetDataForPeriodAsync(period, cancellationToken);
        var series = new HistoricalSeries(
            new CurvePeriod(period),
            data.Select(point => new HistoricalPoint(new TradingDate(point.Tradedate), point.Value)));

        return series.Points
            .Select(point => new Zcyc
            {
                Tradedate = point.TradingDate.Date,
                Period = series.Period.Value,
                Value = point.Value
            })
            .ToList();
    }
}
