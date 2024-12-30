using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.ViewModel;

public partial class MainViewModel : ObservableObject
{

    [ObservableProperty]
    private ObservableObject _currentView;

    private IDataManager _dataManager;

    [ObservableProperty]
    private double _loadingHistoryProgress;

    [ObservableProperty]
    private string _status;

    private ChartViewModelBase<Periods> _spreadChartViewModel;
    private ChartViewModelBase<DateTime> _dailyChartViewModel;
    private ChartViewModelBase<double> _weeklyChartViewModel;

    public MainViewModel(IDataManager dataManager, DailyChartViewModel dailyChartViewModel, WeeklyChartViewModel weeklyChartViewModel, SpreadChartViewModel spreadChartViewModel)
    {
        _dataManager = dataManager;
        _dailyChartViewModel = dailyChartViewModel;
        _weeklyChartViewModel = weeklyChartViewModel;
        _spreadChartViewModel = spreadChartViewModel;

        CurrentView = _dailyChartViewModel;
    }

    public void Initialize()
    {
        var progress = new Progress<double>();

        progress.ProgressChanged += (s, e) =>
        {
            e *= 100;
            Status = $"Загрузка данных: {e:0.00}%";
            LoadingHistoryProgress = e;

            if (e == 100)
            {
                Status = "Данные загружены";
                //CurrentView.Plot(CurrentView.Parameter);
                LoadingHistoryProgress = 0.0;
            }
        };

        Task.Run(async () =>
        {
            await _dataManager.UpdateDataASync(progress);

            await _dailyChartViewModel.Setup();
            await _weeklyChartViewModel.Setup();
            await _spreadChartViewModel.Setup();
        });
    }


    [RelayCommand]
    private void DailyChartSelect()
    {
        CurrentView = _dailyChartViewModel;
    }

    [RelayCommand]
    private void SpreadChartSelect()
    {
        CurrentView = _spreadChartViewModel;
    }

    [RelayCommand]
    private void WeeklyChartSelect()
    {
        CurrentView = _weeklyChartViewModel;
    }
}
