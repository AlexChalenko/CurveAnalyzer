using System.Collections.ObjectModel;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CurveAnalyzer.Application;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.ViewModels;

public partial class YieldCurveViewModel : ObservableObject
{
    public DateTime? EndDate { get; set; }
    public DateTime? StartDate { get; set; }

    [ObservableProperty]
    public partial Func<double, string> Formatter { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<DateTime> BlackoutDates { get; set; } = [];
    [ObservableProperty]
    public partial ObservableCollection<ZcycData> ZcycDatas { get; set; } = [];
    [ObservableProperty]
    public partial bool IsReady { get; set; }

    [ObservableProperty]
    public partial DateTime? SelectedDate { get; set; }

    private readonly DataSyncService _dataSyncService;

    public YieldCurveViewModel(DataSyncService dataSyncService, IMessenger messenger)
    {
        Formatter = value => value.ToString("0.00");

        EndDate = DateTime.Today;
        StartDate = DateTime.Today.AddYears(-1);
        _dataSyncService = dataSyncService;

        messenger.Register<DownloadCompletedMessage>(this, HandleDownloadComplete);

        IsReady = false;
    }

    private void HandleDownloadComplete(object recipient, DownloadCompletedMessage message)
    {
        Initialize();
    }

    public async Task Initialize()
    {
        var blackoutDates = await _dataSyncService.GetBlackoutDatesAsync(CancellationToken.None);

        foreach (var date in blackoutDates)
        {
            BlackoutDates.Add(date);
        }

        IsReady = true;

        App.Current.Dispatcher.Invoke(() =>
        {
            SelectedDate = DateTime.Today;
        });
    }

    [RelayCommand]
    private void ClearChart()
    {
        SelectedDate = null;
    }

    [RelayCommand]
    private void PlotPreviosDay()
    {
        if (SelectedDate.HasValue)
        {
            DateTime newDate = SelectedDate.Value;
            do
            {
                newDate = newDate.AddDays(-1);
            } while (BlackoutDates.Any((d) => d.Date == newDate));

            SelectedDate = newDate;
        }
    }

    partial void OnSelectedDateChanged(DateTime? value)
    {
        Guard.IsNotNull(_dataSyncService);

        if (value.HasValue)
        {
            _dataSyncService.GetYieldCurveForDateAsync(value.Value).ContinueWith(t =>
            {
                if (t.Result.DataRow.Count > 0)
                {
                    ZcycDatas.Add(t.Result);
                }
                else
                {
                    SelectedDate = SelectedDate!.Value.AddDays(-1);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        else
        {
            ZcycDatas.Clear();
        }
    }
}
