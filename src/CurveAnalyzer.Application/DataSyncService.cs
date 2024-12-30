using CommunityToolkit.Mvvm.Messaging;
using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.Application;

public class DataSyncService
{
    private readonly IDataService _onlineDataService;
    private readonly IHistoryDataService _historyDataService;
    private readonly IMessenger _messenger;

    double _sycnProgress;

    public DataSyncService(IDataService onlineDataService, IHistoryDataService historyDataService, IMessenger messenger)
    {
        _onlineDataService = onlineDataService;
        _historyDataService = historyDataService;
        _messenger = messenger;
    }

    public async Task SyncDataAsync(IProgress<double> progress, CancellationToken token)
    {
        var onlineDates = await _onlineDataService.GetAvailableDates(token);
        var todayIsWorkingDay = onlineDates.Last().Date == DateTime.Today;
        if (todayIsWorkingDay)
        {
            onlineDates = onlineDates.ToArray()[..^1];
        }

        var historyDates = await _historyDataService.GetAvailableDates(token);
        var lastHistoryDate = historyDates.Any() ? historyDates.Max() : onlineDates.Min();
        bool needToUpdateHistory = lastHistoryDate < onlineDates.Last();

        if (needToUpdateHistory)
        {
            var datesToLoad = onlineDates.Where(d => d > lastHistoryDate).OrderBy(d => d).ToList();

            var totalCount = datesToLoad.Count;

            for (int i = 0; i < totalCount; i++)
            {
                var date = datesToLoad[i];
                var data = await _onlineDataService.GetDataForDate(date);
                if (data.DataRow.Count != 0)
                {
                    await SaveDataToHistory(data);
                }

                _sycnProgress = ((double)(i + 1)) / totalCount;
                progress.Report(_sycnProgress);
                _messenger.Send(new DownloadProgressMessage(_sycnProgress));
            }
        }

        _messenger.Send(new DownloadCompletedMessage());
    }

    private Task<bool> SaveDataToHistory(ZcycData data)
    {
        return _historyDataService.SaveData(data);
    }

    public async Task<ZcycData> GetYieldCurveForDateAsync(DateTime value)
    {
        var dataToPlot = await _historyDataService.GetDataForDate(value);
        var emptyData = dataToPlot.DataRow.Count == 0;

        if (emptyData)
        {
            dataToPlot = await _onlineDataService.GetDataForDate(value);
        }

        return dataToPlot;
    }

    public async Task<IEnumerable<DateTime>> GetBlackoutDatesAsync(CancellationToken token)
    {
        var realtimeDates = await _onlineDataService.GetAvailableDates(token);
        var historyDates = await _historyDataService.GetAvailableDates(token);

        var historyDatesList = historyDates.ToList();
        var today = DateTime.Today;
        if (realtimeDates.Contains(today) && !historyDates.Contains(today))
            historyDatesList.Add(today);

        return realtimeDates.Except(historyDatesList);
    }

    public Task<IEnumerable<double>> GetAvailablePeriodsAsync()
    {
        return _historyDataService.GetPeriods();
    }

    public async Task<IEnumerable<Zcyc>> GetZcycForPeriodAsync(double period)
    {
        var historyTask = _historyDataService.GetDataForPeriod(period);
        var realtimeTask = _onlineDataService.GetDataForPeriod(period);
        await Task.WhenAll(historyTask, realtimeTask);
        return historyTask.Result.Union(realtimeTask.Result).OrderBy(z => z.Tradedate);
    }
}
