using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Application;
using CurveAnalyzer.Presentation.WPF.ViewModels;
using CurveAnalyzer.Presentation.WPF.Views;

namespace CurveAnalyzer.Presentation.WPF;

public partial class MainViewModel : ObservableObject
{
    private readonly DataSyncService _dataSyncService;
    private readonly YieldCurveViewModel _yieldCurveViewModel;
    private readonly RateChartViewModel _rateChartViewModel;
    private readonly SpreadChartViewModel _spreadChartViewModel;
    private readonly OfzActivityViewModel _ofzActivityViewModel;
    private readonly OfzActivityService _ofzActivityService;
    private readonly YieldCurveControl _yieldCurveControl;
    private readonly RateChartControl _rateChartControl;
    private readonly SpreadChartControl _spreadChartControl;
    private readonly OfzActivityControl _ofzActivityControl;

    [ObservableProperty]
    public partial object SelectedChartView { get; set; }

    [ObservableProperty]
    public partial double LoadingHistoryProgress { get; set; }

    [ObservableProperty]
    public partial string LoadingHistoryStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    public MainViewModel(
        DataSyncService dataSyncService,
        YieldCurveViewModel yieldCurveViewModel,
        RateChartViewModel rateChartViewModel,
        SpreadChartViewModel spreadChartViewModel,
        OfzActivityViewModel ofzActivityViewModel,
        OfzActivityService ofzActivityService,
        YieldCurveControl yieldCurveControl,
        RateChartControl rateChartControl,
        SpreadChartControl spreadChartControl,
        OfzActivityControl ofzActivityControl)
    {
        _dataSyncService = dataSyncService;
        _yieldCurveViewModel = yieldCurveViewModel;
        _rateChartViewModel = rateChartViewModel;
        _spreadChartViewModel = spreadChartViewModel;
        _ofzActivityViewModel = ofzActivityViewModel;
        _ofzActivityService = ofzActivityService;
        _yieldCurveControl = yieldCurveControl;
        _rateChartControl = rateChartControl;
        _spreadChartControl = spreadChartControl;
        _ofzActivityControl = ofzActivityControl;
        _yieldCurveControl.DataContext = _yieldCurveViewModel;
        _rateChartControl.DataContext = _rateChartViewModel;
        _spreadChartControl.DataContext = _spreadChartViewModel;
        _ofzActivityControl.DataContext = _ofzActivityViewModel;
        SelectedChartView = _yieldCurveControl;
    }

    public async Task Initialize()
    {
        IsLoading = true;

        try
        {
            LoadingHistoryStatusMessage = "Загрузка кривой доходности";
            var zcycProgress = new Progress<SyncProgress>(syncProgress =>
            {
                LoadingHistoryProgress = syncProgress.Ratio * 50;
            });

            await _dataSyncService.SyncDataAsync(zcycProgress, CancellationToken.None);

            LoadingHistoryStatusMessage = "Загрузка активности ОФЗ";
            var ofzProgress = new Progress<SyncProgress>(syncProgress =>
            {
                LoadingHistoryProgress = 50 + syncProgress.Ratio * 50;
            });

            await _ofzActivityService.WarmUpRecentHistoryAsync(ofzProgress, CancellationToken.None);

            await _yieldCurveViewModel.Initialize();
            ShowYieldCurve();
        }
        finally
        {
            LoadingHistoryProgress = 0;
            LoadingHistoryStatusMessage = string.Empty;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowYieldCurve()
    {
        SelectedChartView = _yieldCurveControl;
    }

    [RelayCommand]
    private void ShowRateChange()
    {
        SelectedChartView = _rateChartControl;
    }

    [RelayCommand]
    private void ShowSpreadChange()
    {
        SelectedChartView = _spreadChartControl;
    }

    [RelayCommand]
    private async Task ShowOfzActivity()
    {
        SelectedChartView = _ofzActivityControl;
        await _ofzActivityViewModel.Initialize();
    }
}
