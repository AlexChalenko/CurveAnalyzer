using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CurveAnalyzer.Application;
using CurveAnalyzer.Core;
using CurveAnalyzer.Presentation.WPF.Data;
using PresentationPeriods = CurveAnalyzer.Presentation.WPF.Data.Periods;

namespace CurveAnalyzer.Presentation.WPF.ViewModels;

public partial class SpreadChartViewModel : ObservableObject, IChartViewModel
{
    private readonly DataSyncService _dataService;
    private bool _initialized;

    [ObservableProperty]
    public partial ObservableCollection<double> PeriodsList { get; set; } = [];

    [ObservableProperty]
    public partial PresentationPeriods Periods { get; set; } = new();

    [ObservableProperty]
    public partial double PeriodFirst { get; set; }

    [ObservableProperty]
    public partial double PeriodSecond { get; set; }

    [ObservableProperty]
    public partial List<ZcycPoint> Values { get; set; } = [];

    public SpreadChartViewModel(DataSyncService dataService)
    {
        _dataService = dataService;
    }

    public async Task Initialize()
    {
        if (_initialized && PeriodsList.Count > 0)
        {
            return;
        }

        var periods = await _dataService.GetAvailablePeriodsAsync(CancellationToken.None);

        PeriodsList.Clear();
        foreach (var period in periods)
        {
            PeriodsList.Add(period);
        }

        _initialized = true;
    }

    private async Task LoadDataAsync()
    {
        var data1 = await _dataService.GetZcycForPeriodAsync(PeriodFirst, CancellationToken.None);
        var data2 = await _dataService.GetZcycForPeriodAsync(PeriodSecond, CancellationToken.None);

        var firstSeries = ToHistoricalSeries(PeriodFirst, data1);
        var secondSeries = ToHistoricalSeries(PeriodSecond, data2);
        var spread = SpreadCalculator.Calculate(firstSeries, secondSeries);

        Values = spread.Points
            .Select(point => new ZcycPoint(point.TradingDate.Date, point.Value))
            .ToList();
    }

    partial void OnPeriodFirstChanged(double value)
    {
        Periods.Period1 = value;
        QueueDataReload();
    }

    partial void OnPeriodSecondChanged(double value)
    {
        Periods.Period2 = value;
        QueueDataReload();
    }

    private void QueueDataReload()
    {
        if (!Periods.IsEmpty)
        {
            _ = LoadDataAsync();
        }
    }

    private static HistoricalSeries ToHistoricalSeries(double period, IEnumerable<Zcyc> points)
    {
        return new HistoricalSeries(
            new CurvePeriod(period),
            points.Select(point => new HistoricalPoint(new TradingDate(point.Tradedate), point.Value)));
    }
}
