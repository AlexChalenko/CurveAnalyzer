using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CurveAnalyzer.Application;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.ViewModels;

public partial class RateChartViewModel(DataSyncService dataService) : ObservableObject, IChartViewModel
{
    private readonly DataSyncService _dataService = dataService;

    [ObservableProperty]
    public partial double SelectedPeriod { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Zcyc>? ZcycArray { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<double> Periods { get; set; } = [];

    public async Task Initialize()
    {
        var periods = await _dataService.GetAvailablePeriodsAsync(CancellationToken.None);

        Periods.Clear();
        foreach (var period in periods)
        {
            Periods.Add(period);
        }
    }

    partial void OnSelectedPeriodChanged(double value)
    {
        _ = LoadPeriodAsync(value);
    }

    private async Task LoadPeriodAsync(double value)
    {
        ZcycArray = await _dataService.GetZcycForPeriodAsync(value, CancellationToken.None);
    }
}
