using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Application;
using CurveAnalyzer.Presentation.WPF.ViewModels;

namespace CurveAnalyzer.Presentation.WPF;

public partial class MainViewModel : ObservableObject
{
    private readonly DataSyncService _dataSyncService;
    private readonly YieldCurveViewModel _yieldCurveViewModel;
    private readonly RateChartViewModel _rateChartViewModel;
    private readonly SpreadChartViewModel _spreadChartViewModel;

    [ObservableProperty]
    public partial IChartViewModel SelectedChart { get; set; }

    [ObservableProperty]
    public partial double LoadingHistoryProgress { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    public MainViewModel(
        DataSyncService dataSyncService,
        YieldCurveViewModel yieldCurveViewModel,
        RateChartViewModel rateChartViewModel,
        SpreadChartViewModel spreadChartViewModel)
    {
        _dataSyncService = dataSyncService;
        _yieldCurveViewModel = yieldCurveViewModel;
        _rateChartViewModel = rateChartViewModel;
        _spreadChartViewModel = spreadChartViewModel;
        SelectedChart = _yieldCurveViewModel;
    }

    public async Task Initialize()
    {
        IsLoading = true;

        try
        {
            var progress = new Progress<SyncProgress>(syncProgress =>
            {
                LoadingHistoryProgress = syncProgress.Ratio * 100;
            });

            await _dataSyncService.SyncDataAsync(progress, CancellationToken.None);
            await _yieldCurveViewModel.Initialize();
            ShowYieldCurve();
        }
        finally
        {
            LoadingHistoryProgress = 0;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowYieldCurve()
    {
        SelectedChart = _yieldCurveViewModel;
    }

    [RelayCommand]
    private void ShowRateChange()
    {
        SelectedChart = _rateChartViewModel;
    }

    [RelayCommand]
    private void ShowSpreadChange()
    {
        SelectedChart = _spreadChartViewModel;
    }
}
