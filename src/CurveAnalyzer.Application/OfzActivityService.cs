using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application;

public sealed class OfzActivityService(
    IOfzActivityDataService dataService,
    IOfzActivityRepository repository,
    ICbrKeyRateDataService cbrKeyRateDataService,
    IOfzIndexDataService indexDataService,
    IOfzCashflowDataService cashflowDataService)
{
    private const string BoardId = "TQOB";
    private const int WarmUpTradingDays = 252;
    private const int BaselineCalendarLookbackDays = 60;
    private const int CbrKeyRateCalendarLookbackDays = 370;
    private const int IndexContextCalendarLookbackDays = 60;
    private const int CashflowNearWindowDays = 3;
    private const int CashflowLookAheadDays = 370;
    private const int RefreshableRecentCalendarDays = 7;

    public async Task WarmUpRecentHistoryAsync(
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var dates = GetRecentWeekdays(DateTime.Today, WarmUpTradingDays);
        await EnsureDatesLoadedAsync(dates, progress, cancellationToken).ConfigureAwait(false);

        if (dates.Count > 0)
        {
            await EnsureCbrKeyRatesLoadedAsync(dates.First(), dates.Last(), cancellationToken).ConfigureAwait(false);
            await EnsureMarketIndexPointsLoadedAsync(dates.First(), dates.Last(), cancellationToken).ConfigureAwait(false);
        }
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
        var liquiditySnapshots = await repository
            .GetLiquiditySnapshotsAsync(startDate, endDate, BoardId, cancellationToken)
            .ConfigureAwait(false);
        var cbrKeyRates = await EnsureCbrKeyRatesLoadedAsync(startDate, endDate, cancellationToken)
            .ConfigureAwait(false);
        var indexPoints = await EnsureMarketIndexPointsLoadedAsync(startDate, endDate, cancellationToken)
            .ConfigureAwait(false);

        var allTrades = baselineTrades
            .Concat(rangeTrades)
            .OrderBy(trade => trade.SecId, StringComparer.Ordinal)
            .ThenBy(trade => trade.TradeDate)
            .ToList();

        var metrics = OfzActivityAnalyzer.CalculateMetrics(allTrades)
            .Where(metric => metric.TradeDate >= startDate && metric.TradeDate <= endDate)
            .ToList();
        var liquidityMetrics = OfzActivityAnalyzer.CalculateLiquidityMetrics(rangeTrades);
        var snapshotLiquidityMetrics = OfzActivityAnalyzer.CalculateSnapshotLiquidityMetrics(liquiditySnapshots);

        var secIds = allTrades
            .Select(trade => trade.SecId)
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var issues = await repository.GetIssuesAsync(secIds, cancellationToken).ConfigureAwait(false);
        var activeSecIds = rangeTrades
            .Select(trade => trade.SecId)
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var cashflowEvents = await EnsureCashflowEventsLoadedAsync(activeSecIds, startDate, endDate, cancellationToken)
            .ConfigureAwait(false);

        return new OfzActivityLoadResult(startDate, endDate, issues, rangeTrades, metrics)
        {
            LiquiditySnapshots = liquiditySnapshots,
            LiquidityMetrics = liquidityMetrics,
            SnapshotLiquidityMetrics = snapshotLiquidityMetrics,
            CbrKeyRates = cbrKeyRates,
            IndexPoints = indexPoints,
            CashflowEvents = cashflowEvents,
            CashflowDataLoaded = activeSecIds.Count > 0
        };
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

    public async Task<IReadOnlyList<OfzWeakLiquidityItem>> GetWeakLiquidityAsync(
        DateTime startDate,
        DateTime endDate,
        int topCount = 50,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await LoadActivityAsync(startDate, endDate, progress, cancellationToken).ConfigureAwait(false);
        var metrics = result.LiquidityMetrics
            .Concat(result.SnapshotLiquidityMetrics)
            .ToList();

        return OfzActivityAnalyzer.GetWeakLiquidityRankings(metrics, result.Issues, topCount);
    }

    public async Task<IReadOnlyList<OfzActivityInsight>> GetLiquidityInsightsAsync(
        DateTime startDate,
        DateTime endDate,
        int maxInsights = 4,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await LoadActivityAsync(startDate, endDate, progress, cancellationToken).ConfigureAwait(false);
        var metrics = result.LiquidityMetrics
            .Concat(result.SnapshotLiquidityMetrics)
            .ToList();

        return OfzActivityAnalyzer.BuildLiquidityInsights(metrics, result.Issues, maxInsights);
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
        return OfzActivityAnalyzer.BuildDurationYieldScatter(
            result.Metrics,
            result.Issues,
            result.LiquidityMetrics,
            tradeDate);
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
        var snapshots = await repository
            .GetIssueLiquiditySnapshotsAsync(secId, startDate, endDate, BoardId, cancellationToken)
            .ConfigureAwait(false);
        var currentSnapshot = snapshots
            .OrderByDescending(snapshot => snapshot.TradeDate)
            .ThenByDescending(snapshot => snapshot.ObservedAt)
            .FirstOrDefault();
        var cbrKeyRates = await EnsureCbrKeyRatesLoadedAsync(startDate, endDate, cancellationToken)
            .ConfigureAwait(false);

        return OfzActivityAnalyzer.BuildIssueDetail(secId, trades, issues.FirstOrDefault(), currentSnapshot, cbrKeyRates);
    }

    public async Task<OfzIssueLiquidityProfile> GetIssueLiquidityProfileAsync(
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
        var snapshots = await repository
            .GetIssueLiquiditySnapshotsAsync(secId, startDate, endDate, BoardId, cancellationToken)
            .ConfigureAwait(false);
        var issues = await repository
            .GetIssuesAsync([secId], cancellationToken)
            .ConfigureAwait(false);
        var currentSnapshot = snapshots
            .OrderByDescending(snapshot => snapshot.TradeDate)
            .ThenByDescending(snapshot => snapshot.ObservedAt)
            .FirstOrDefault();

        return OfzActivityAnalyzer.BuildIssueLiquidityProfile(
            secId,
            trades,
            currentSnapshot,
            issues.FirstOrDefault());
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

    private async Task<IReadOnlyList<CbrKeyRate>> EnsureCbrKeyRatesLoadedAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var keyRateStartDate = startDate.Date.AddDays(-CbrKeyRateCalendarLookbackDays);
        var keyRateEndDate = endDate.Date;
        var cachedRates = await repository
            .GetCbrKeyRatesAsync(keyRateStartDate, keyRateEndDate, cancellationToken)
            .ConfigureAwait(false);

        if (!ShouldRefreshCbrKeyRates(cachedRates, keyRateStartDate, keyRateEndDate))
        {
            return cachedRates;
        }

        try
        {
            var loadedRates = await cbrKeyRateDataService
                .GetKeyRatesAsync(keyRateStartDate, keyRateEndDate, cancellationToken)
                .ConfigureAwait(false);

            await repository.SaveCbrKeyRatesAsync(loadedRates, cancellationToken).ConfigureAwait(false);

            return await repository
                .GetCbrKeyRatesAsync(keyRateStartDate, keyRateEndDate, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return cachedRates;
        }
    }

    private async Task<IReadOnlyList<OfzMarketIndexPoint>> EnsureMarketIndexPointsLoadedAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var indexStartDate = startDate.Date.AddDays(-IndexContextCalendarLookbackDays);
        var indexEndDate = endDate.Date;
        var cachedPoints = await repository
            .GetMarketIndexPointsAsync(indexStartDate, indexEndDate, cancellationToken)
            .ConfigureAwait(false);

        if (!ShouldRefreshMarketIndexPoints(cachedPoints, indexStartDate, indexEndDate))
        {
            return cachedPoints;
        }

        var secIds = OfzIndexContextBuilder.DefaultSeries
            .Select(series => series.SecId)
            .ToArray();

        try
        {
            var historyPoints = await indexDataService
                .GetHistoryAsync(secIds, indexStartDate, indexEndDate, cancellationToken)
                .ConfigureAwait(false);
            await repository.SaveMarketIndexPointsAsync(historyPoints, cancellationToken).ConfigureAwait(false);

            if (indexEndDate >= DateTime.Today.AddDays(-RefreshableRecentCalendarDays))
            {
                var snapshotPoints = await indexDataService
                    .GetCurrentSnapshotAsync(secIds, cancellationToken)
                    .ConfigureAwait(false);
                await repository.SaveMarketIndexPointsAsync(snapshotPoints, cancellationToken).ConfigureAwait(false);
            }

            return await repository
                .GetMarketIndexPointsAsync(indexStartDate, indexEndDate, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return cachedPoints;
        }
    }

    private async Task<IReadOnlyList<OfzCashflowEvent>> EnsureCashflowEventsLoadedAsync(
        IReadOnlyCollection<string> secIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        if (secIds.Count == 0)
        {
            return [];
        }

        var cashflowStartDate = startDate.Date.AddDays(-CashflowNearWindowDays);
        var cashflowEndDate = endDate.Date.AddDays(CashflowLookAheadDays);
        var cachedEvents = await repository
            .GetCashflowEventsAsync(secIds, cashflowStartDate, cashflowEndDate, cancellationToken)
            .ConfigureAwait(false);

        if (!ShouldRefreshCashflowEvents(secIds, cachedEvents))
        {
            return cachedEvents;
        }

        try
        {
            var loadedEvents = await cashflowDataService
                .GetScheduleAsync(secIds, cancellationToken)
                .ConfigureAwait(false);
            await repository.SaveCashflowEventsAsync(loadedEvents, cancellationToken).ConfigureAwait(false);

            return await repository
                .GetCashflowEventsAsync(secIds, cashflowStartDate, cashflowEndDate, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return cachedEvents;
        }
    }

    private static bool ShouldRefreshCbrKeyRates(
        IReadOnlyList<CbrKeyRate> cachedRates,
        DateTime startDate,
        DateTime endDate)
    {
        if (cachedRates.Count == 0)
        {
            return true;
        }

        var earliestLoaded = cachedRates.Min(rate => rate.Date.Date);
        var latestLoaded = cachedRates.Max(rate => rate.Date.Date);
        var latestRequired = GetLastWeekdayOnOrBefore(endDate.Date <= DateTime.Today ? endDate.Date : DateTime.Today);

        return earliestLoaded > startDate.AddDays(7) || latestLoaded < latestRequired;
    }

    private static bool ShouldRefreshMarketIndexPoints(
        IReadOnlyList<OfzMarketIndexPoint> cachedPoints,
        DateTime startDate,
        DateTime endDate)
    {
        if (cachedPoints.Count == 0)
        {
            return true;
        }

        var requiredSecIds = OfzIndexContextBuilder.DefaultSeries
            .Where(series => series.IsRequired)
            .Select(series => series.SecId)
            .ToArray();
        var cachedSecIds = cachedPoints
            .Select(point => point.SecId)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (requiredSecIds.Any(secId => !cachedSecIds.Contains(secId)))
        {
            return true;
        }

        var earliestLoaded = cachedPoints.Min(point => point.TradeDate.Date);
        var latestLoaded = cachedPoints.Max(point => point.TradeDate.Date);
        var latestRequired = GetLastWeekdayOnOrBefore(endDate.Date <= DateTime.Today ? endDate.Date : DateTime.Today);

        return earliestLoaded > startDate.AddDays(7) ||
            latestLoaded < latestRequired.AddDays(-RefreshableRecentCalendarDays);
    }

    private static bool ShouldRefreshCashflowEvents(
        IReadOnlyCollection<string> secIds,
        IReadOnlyList<OfzCashflowEvent> cachedEvents)
    {
        if (cachedEvents.Count == 0)
        {
            return true;
        }

        var cachedSecIds = cachedEvents
            .Select(item => item.SecId)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        return secIds.Any(secId => !cachedSecIds.Contains(secId));
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

    private static DateTime GetLastWeekdayOnOrBefore(DateTime date)
    {
        var candidate = date.Date;
        while (!IsWeekday(candidate))
        {
            candidate = candidate.AddDays(-1);
        }

        return candidate;
    }

    private static bool IsRefreshableDate(DateTime date)
    {
        var tradeDate = date.Date;
        var today = DateTime.Today;

        return tradeDate <= today && tradeDate >= today.AddDays(-RefreshableRecentCalendarDays);
    }
}
