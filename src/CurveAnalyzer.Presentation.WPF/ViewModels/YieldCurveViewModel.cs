using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Application;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.ViewModels;

public partial class YieldCurveViewModel : ObservableObject, IChartViewModel
{
    private readonly DataSyncService _dataSyncService;

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

    public YieldCurveViewModel(DataSyncService dataSyncService)
    {
        Formatter = value => value.ToString("0.00");
        EndDate = DateTime.Today;
        StartDate = DateTime.Today.AddYears(-1);
        _dataSyncService = dataSyncService;
    }

    public async Task Initialize()
    {
        var blackoutDates = await _dataSyncService.GetBlackoutDatesAsync(CancellationToken.None);

        BlackoutDates.Clear();
        foreach (var date in blackoutDates)
        {
            BlackoutDates.Add(date);
        }

        IsReady = true;
        SelectedDate = DateTime.Today;
    }

    [RelayCommand]
    private void ClearChart()
    {
        SelectedDate = null;
    }

    [RelayCommand]
    private void PlotPreviosDay()
    {
        if (!SelectedDate.HasValue)
        {
            return;
        }

        DateTime newDate = SelectedDate.Value;
        do
        {
            newDate = newDate.AddDays(-1);
        } while (BlackoutDates.Any(date => date.Date == newDate.Date));

        SelectedDate = newDate;
    }

    partial void OnSelectedDateChanged(DateTime? value)
    {
        _ = LoadSelectedDateAsync(value);
    }

    private async Task LoadSelectedDateAsync(DateTime? value)
    {
        if (!value.HasValue)
        {
            ZcycDatas.Clear();
            return;
        }

        var data = await _dataSyncService.GetYieldCurveForDateAsync(value.Value, CancellationToken.None);
        if (data.DataRow.Count > 0)
        {
            if (ZcycDatas.All(existing => existing.Date.Date != data.Date.Date))
            {
                ZcycDatas.Add(data);
            }

            return;
        }

        SelectedDate = value.Value.AddDays(-1);
    }
}
