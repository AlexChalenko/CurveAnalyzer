using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application;

public sealed class OfzActivityService(
    IOfzActivityDataService dataService,
    IOfzActivityRepository repository)
{
    private const string BoardId = "TQOB";
    private const int WarmUpTradingDays = 252;
    private const int BaselineCalendarLookbackDays = 60;
    private const int RefreshableRecentCalendarDays = 7;

    public Task WarmUpRecentHistoryAsync(
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var dates = GetRecentWeekdays(DateTime.Today, WarmUpTradingDays);
        return EnsureDatesLoadedAsync(dates, progress, cancellationToken);
    }

    public async Task<OfzActivityLoadResult> LoadActivityAsync(
        DateTime startDate,
        DateTime endDate,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        (startDate, endDate) = NormalizeRange(startDate, endDate);

        var syncStartDate = startDate.AddDays(-BaselineCalendarLookbackDays);
        var datesToEnsure = GetWeekdays(syncStartDate, endDate);
        await EnsureDatesLoadedAsync(datesToEnsure, progress, cancellationToken).ConfigureAwait(false);

        var baselineTrades = await repository
            .GetBaselineTradesAsync(startDate, OfzActivityAnalyzer.DefaultBaselineWindow, BoardId, cancellationToken)
            .ConfigureAwait(false);
        var rangeTrades = await repository
            .GetTradesAsync(startDate, endDate, BoardId, cancellationToken)
            .ConfigureAwait(false);

        var allTrades = baselineTrades
            .Concat(rangeTrades)
            .OrderBy(trade => trade.SecId, StringComparer.Ordinal)
            .ThenBy(trade => trade.TradeDate)
            .ToList();

        var metrics = OfzActivityAnalyzer.CalculateMetrics(allTrades)
            .Where(metric => metric.TradeDate >= startDate && metric.TradeDate <= endDate)
            .ToList();

        var secIds = allTrades
            .Select(trade => trade.SecId)
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var issues = await repository.GetIssuesAsync(secIds, cancellationToken).ConfigureAwait(false);

        return new OfzActivityLoadResult(startDate, endDate, issues, rangeTrades, metrics);
    }

    public async Task<IReadOnlyList<OfzActivityAnomaly>> GetTopAnomaliesAsync(
        DateTime startDate,
        DateTime endDate,
        int topCount = 50,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await LoadActivityAsync(startDate, endDate, progress, cancellationToken).ConfigureAwait(false);
        return OfzActivityAnalyzer.GetTopAnomalies(result.Metrics, result.Issues, topCount);
    }

    public async Task<IReadOnlyList<OfzActivityHeatmapCell>> GetHeatmapCellsAsync(
        DateTime startDate,
        DateTime endDate,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await LoadActivityAsync(startDate, endDate, progress, cancellationToken).ConfigureAwait(false);
        return OfzActivityAnalyzer.BuildHeatmapCells(result.Metrics, result.Issues, result.StartDate, result.EndDate);
    }

    public async Task<IReadOnlyList<OfzActivityIndexPoint>> GetActivityIndexAsync(
        DateTime startDate,
        DateTime endDate,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await LoadActivityAsync(startDate, endDate, progress, cancellationToken).ConfigureAwait(false);
        return OfzActivityAnalyzer.BuildActivityIndex(result.Metrics);
    }

    public async Task<IReadOnlyList<OfzDurationYieldScatterPoint>> GetDurationYieldScatterAsync(
        DateTime startDate,
        DateTime endDate,
        DateTime? tradeDate = null,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await LoadActivityAsync(startDate, endDate, progress, cancellationToken).ConfigureAwait(false);
        return OfzActivityAnalyzer.BuildDurationYieldScatter(result.Metrics, result.Issues, tradeDate);
    }

    public async Task<OfzIssueDetail> GetIssueDetailsAsync(
        string secId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secId))
        {
            throw new ArgumentException("SECID is required.", nameof(secId));
        }

        (startDate, endDate) = NormalizeRange(startDate, endDate);

        var trades = await repository
            .GetIssueTradesAsync(secId, startDate, endDate, BoardId, cancellationToken)
            .ConfigureAwait(false);
        var issues = await repository
            .GetIssuesAsync([secId], cancellationToken)
            .ConfigureAwait(false);

        return OfzActivityAnalyzer.BuildIssueDetail(secId, trades, issues.FirstOrDefault());
    }

    private async Task EnsureDatesLoadedAsync(
        IReadOnlyList<DateTime> dates,
        IProgress<SyncProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (dates.Count == 0)
        {
            progress?.Report(SyncProgress.Completed);
            return;
        }

        var loadedDates = await repository
            .GetLoadedDatesAsync(dates.First(), dates.Last(), BoardId, cancellationToken)
            .ConfigureAwait(false);
        var missingDates = dates
            .Where(date => IsRefreshableDate(date) || !loadedDates.Contains(date.Date))
            .ToList();

        if (missingDates.Count == 0)
        {
            progress?.Report(SyncProgress.Completed);
            return;
        }

        for (var index = 0; index < missingDates.Count; index++)
        {
            var date = missingDates[index];
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var data = date.Date == DateTime.Today
                    ? await dataService.GetCurrentSnapshotAsync(cancellationToken).ConfigureAwait(false)
                    : await dataService.GetHistoryForDateAsync(date, cancellationToken).ConfigureAwait(false);

                await repository.SaveDailyDataAsync(data, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await repository
                    .SaveLoadStateAsync(new OfzActivityLoadState
                    {
                        BoardId = BoardId,
                        TradeDate = date.Date,
                        Status = OfzActivityLoadStatus.Failed,
                        LoadedAt = DateTime.UtcNow,
                        ErrorMessage = ex.Message
                    }, cancellationToken)
                    .ConfigureAwait(false);
            }

            progress?.Report(new SyncProgress(index + 1, missingDates.Count, date));
        }

        progress?.Report(SyncProgress.Completed);
    }

    private static (DateTime StartDate, DateTime EndDate) NormalizeRange(DateTime startDate, DateTime endDate)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;

        return startDate <= endDate
            ? (startDate, endDate)
            : (endDate, startDate);
    }

    private static IReadOnlyList<DateTime> GetRecentWeekdays(DateTime endDate, int count)
    {
        List<DateTime> dates = [];
        var date = endDate.Date;

        while (dates.Count < count)
        {
            if (IsWeekday(date))
            {
                dates.Add(date);
            }

            date = date.AddDays(-1);
        }

        dates.Reverse();
        return dates;
    }

    private static IReadOnlyList<DateTime> GetWeekdays(DateTime startDate, DateTime endDate)
    {
        List<DateTime> dates = [];

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (IsWeekday(date))
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    private static bool IsWeekday(DateTime date)
    {
        return date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
    }

    private static bool IsRefreshableDate(DateTime date)
    {
        var tradeDate = date.Date;
        var today = DateTime.Today;

        return tradeDate <= today && tradeDate >= today.AddDays(-RefreshableRecentCalendarDays);
    }
}
