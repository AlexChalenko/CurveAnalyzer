using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CurveAnalyzer.Application;
using CurveAnalyzer.Presentation.WPF.ViewModels;

namespace CurveAnalyzer.Presentation.WPF;

public partial class MainViewModel : ObservableObject
{
    private readonly DataSyncService _dataSyncService;
    private readonly YieldCurveViewModel _yieldCurveViewModel;
    private readonly RateChartViewModel _rateChartViewModel;
    private readonly SpreadChartViewModel _spreadChartViewModel;
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private ObservableObject _selectedChart;

    [ObservableProperty]
    private double _loadingHistoryProgress;

    [ObservableProperty]
    private bool _isLoading =true;

    public MainViewModel(DataSyncService dataSyncService, YieldCurveViewModel yieldCurveViewModel, RateChartViewModel rateChartViewModel, SpreadChartViewModel spreadChartViewModel, IMessenger messenger)
    {
        _dataSyncService = dataSyncService;
        _yieldCurveViewModel = yieldCurveViewModel;
        _rateChartViewModel = rateChartViewModel;
        _spreadChartViewModel = spreadChartViewModel;
        _messenger = messenger;
        SelectedChart = _yieldCurveViewModel;

        _messenger.Register<DownloadProgressMessage>(this, DownloadProgressHandler);
        _messenger.Register<DownloadCompletedMessage>(this, OnDownloadComplete);
        //IsLoading = true;
    }

    private void OnDownloadComplete(object recipient, DownloadCompletedMessage message)
    {
        IsLoading = false;
    }

    private void DownloadProgressHandler(object recipient, DownloadProgressMessage message)
    {
        LoadingHistoryProgress = message.Value * 100;
    }

    public async Task Initialize()
    {
        var progress = new Progress<double>();

        progress.ProgressChanged += (s, e) =>
        {
            //e *= 100;
            ////Status = $"Загрузка данных: {e:0.00}%";
            //LoadingHistoryProgress = e;

            //if (e == 100)
            //{
            //    //  Status = "Данные загружены";
            //    //CurrentView.Plot(CurrentView.Parameter);
            //    LoadingHistoryProgress = 0.0;
            //}
        };

        await _dataSyncService.SyncDataAsync(progress, CancellationToken.None);
        ShowYieldCurveCommand.Execute(this);
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
