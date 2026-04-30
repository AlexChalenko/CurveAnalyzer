using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application;

public class DataSyncService
{
    private readonly IDataService _onlineDataService;
    private readonly IHistoryDataService _historyDataService;

    public DataSyncService(IDataService onlineDataService, IHistoryDataService historyDataService)
    {
        _onlineDataService = onlineDataService;
        _historyDataService = historyDataService;
    }

    public async Task SyncDataAsync(IProgress<SyncProgress>? progress, CancellationToken token)
    {
        var onlineDates = (await _onlineDataService.GetAvailableDatesAsync(token).ConfigureAwait(false)).OrderBy(date => date).ToList();
        if (onlineDates.Count == 0)
        {
            progress?.Report(SyncProgress.Completed);
            return;
        }

        var todayIsWorkingDay = onlineDates.Last().Date == DateTime.Today;
        if (todayIsWorkingDay)
        {
            onlineDates = onlineDates[..^1];
        }

        if (onlineDates.Count == 0)
        {
            progress?.Report(SyncProgress.Completed);
            return;
        }

        var historyDates = await _historyDataService.GetAvailableDatesAsync(token).ConfigureAwait(false);
        var lastHistoryDate = historyDates.Count > 0 ? historyDates.Max() : DateTime.MinValue;
        bool needToUpdateHistory = lastHistoryDate < onlineDates.Last();

        if (needToUpdateHistory)
        {
            var datesToLoad = onlineDates.Where(d => d > lastHistoryDate).OrderBy(d => d).ToList();

            var totalCount = datesToLoad.Count;

            for (int i = 0; i < totalCount; i++)
            {
                var date = datesToLoad[i];
                var data = await _onlineDataService.GetDataForDateAsync(date, token).ConfigureAwait(false);
                if (data.DataRow.Count != 0)
                {
                    await SaveDataToHistory(data, token).ConfigureAwait(false);
                }

                progress?.Report(new SyncProgress(i + 1, totalCount, date));
            }
        }

        progress?.Report(SyncProgress.Completed);
    }

    private Task<bool> SaveDataToHistory(ZcycData data, CancellationToken cancellationToken)
    {
        return _historyDataService.SaveDataAsync(data, cancellationToken);
    }

    public async Task<ZcycData> GetYieldCurveForDateAsync(DateTime value, CancellationToken cancellationToken = default)
    {
        var dataToPlot = await _historyDataService.GetDataForDateAsync(value, cancellationToken).ConfigureAwait(false);
        var emptyData = dataToPlot.DataRow.Count == 0;

        if (emptyData)
        {
            dataToPlot = await _onlineDataService.GetDataForDateAsync(value, cancellationToken).ConfigureAwait(false);
        }

        return dataToPlot;
    }

    public async Task<IEnumerable<DateTime>> GetBlackoutDatesAsync(CancellationToken token)
    {
        var realtimeDates = await _onlineDataService.GetAvailableDatesAsync(token).ConfigureAwait(false);
        var historyDates = await _historyDataService.GetAvailableDatesAsync(token).ConfigureAwait(false);

        var historyDatesList = historyDates.ToList();
        var today = DateTime.Today;
        if (realtimeDates.Contains(today) && !historyDates.Contains(today))
            historyDatesList.Add(today);

        return realtimeDates.Except(historyDatesList);
    }

    public Task<IReadOnlyList<double>> GetAvailablePeriodsAsync(CancellationToken cancellationToken = default)
    {
        return _historyDataService.GetPeriodsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Zcyc>> GetZcycForPeriodAsync(double period, CancellationToken cancellationToken = default)
    {
        var historyTask = _historyDataService.GetDataForPeriodAsync(period, cancellationToken);
        var realtimeTask = _onlineDataService.GetDataForPeriodAsync(period, cancellationToken);
        await Task.WhenAll(historyTask, realtimeTask).ConfigureAwait(false);

        var historyData = await historyTask.ConfigureAwait(false);
        var realtimeData = await realtimeTask.ConfigureAwait(false);

        return historyData
            .Union(realtimeData)
            .OrderBy(z => z.Tradedate)
            .ToList();
    }
}
