using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Application;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.ViewModels;

public partial class OfzActivityViewModel(OfzActivityService activityService) : ObservableObject, IChartViewModel
{
    private bool _initialized;
    private int _detailSelectionVersion;
    private OfzActivityLoadResult? _currentResult;

    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzCouponTypeFilter> CouponTypeFilters { get; set; } = new(CreateCouponTypeFilters());

    [ObservableProperty]
    public partial OfzCouponTypeFilter? SelectedCouponTypeFilter { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzInsightPeriodFilter> InsightPeriodFilters { get; set; } = new(CreateInsightPeriodFilters());

    [ObservableProperty]
    public partial OfzInsightPeriodFilter? SelectedInsightPeriodFilter { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzSignalScopeFilter> SignalScopeFilters { get; set; } = new(CreateSignalScopeFilters());

    [ObservableProperty]
    public partial OfzSignalScopeFilter? SelectedSignalScopeFilter { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzActivityAnomaly> TopAnomalies { get; set; } = [];

    [ObservableProperty]
    public partial OfzActivityAnomaly? SelectedTopAnomaly { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzWeakLiquidityItem> WeakLiquidityItems { get; set; } = [];

    [ObservableProperty]
    public partial OfzWeakLiquidityItem? SelectedWeakLiquidityItem { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DateTime> HeatmapDates { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<OfzActivityHeatmapRow> HeatmapRows { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<OfzActivityInsight> Insights { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<OfzActivityIndexPoint> ActivityIndex { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<OfzDurationYieldScatterPoint> DurationYieldScatterPoints { get; set; } = [];

    [ObservableProperty]
    public partial OfzActivityHeatmapCell? SelectedHeatmapCell { get; set; }

    [ObservableProperty]
    public partial string SelectedHeatmapSummary { get; set; } = "Ячейка не выбрана";

    [ObservableProperty]
    public partial OfzIssueDetail? SelectedIssueDetail { get; set; }

    [ObservableProperty]
    public partial OfzIssueLiquidityProfile? SelectedIssueLiquidityProfile { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingIssueDetail { get; set; }

    [ObservableProperty]
    public partial string IssueDetailStatusMessage { get; set; } = "Выпуск не выбран";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial double LoadingProgress { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Готово";

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool HasAnomalies { get; set; }

    [ObservableProperty]
    public partial bool HasWeakLiquidity { get; set; }

    [ObservableProperty]
    public partial bool HasHeatmap { get; set; }

    [ObservableProperty]
    public partial bool HasInsights { get; set; }

    [ObservableProperty]
    public partial bool HasActivityIndex { get; set; }

    [ObservableProperty]
    public partial bool HasDurationYieldScatter { get; set; }

    [ObservableProperty]
    public partial string ActivityIndexStatusMessage { get; set; } = "Индекс не рассчитан";

    [ObservableProperty]
    public partial string DurationYieldScatterStatusMessage { get; set; } = "Scatter не рассчитан";

    public Task Initialize()
    {
        if (_initialized)
        {
            return Task.CompletedTask;
        }

        EndDate = DateTime.Today;
        StartDate = DateTime.Today.AddDays(-45);
        SelectedCouponTypeFilter ??= CouponTypeFilters.FirstOrDefault();
        SelectedInsightPeriodFilter ??= InsightPeriodFilters.FirstOrDefault();
        SelectedSignalScopeFilter ??= SignalScopeFilters.FirstOrDefault();
        StatusMessage = "Выберите диапазон";
        _initialized = true;

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task WarmUpRecentHistoryAsync()
    {
        return RunWithProgressAsync(
            "Загрузка недавней истории",
            (progress, cancellationToken) => activityService.WarmUpRecentHistoryAsync(progress, cancellationToken),
            updateRows: false);
    }

    [RelayCommand]
    private Task LoadActivityAsync()
    {
        return RunWithProgressAsync(
            "Загрузка активности",
            async (progress, cancellationToken) =>
            {
                ClearResults();

                if (!StartDate.HasValue || !EndDate.HasValue)
                {
                    ErrorMessage = "Укажите даты диапазона";
                    return;
                }

                var result = await activityService
                    .LoadActivityAsync(StartDate.Value, EndDate.Value, progress, cancellationToken)
                    .ConfigureAwait(true);
                _currentResult = result;
                ApplyCurrentFilter();
            },
            updateRows: true);
    }

    [RelayCommand]
    private void SelectHeatmapCell(OfzActivityHeatmapCell? cell)
    {
        if (cell is null)
        {
            return;
        }

        SelectedHeatmapCell = cell;
        SelectedHeatmapSummary = FormatHeatmapCell(cell);
        _ = LoadIssueDetailsAsync(cell.SecId);
    }

    partial void OnSelectedTopAnomalyChanged(OfzActivityAnomaly? value)
    {
        if (value is null)
        {
            return;
        }

        _ = LoadIssueDetailsAsync(value.SecId);
    }

    partial void OnSelectedWeakLiquidityItemChanged(OfzWeakLiquidityItem? value)
    {
        if (value is null)
        {
            return;
        }

        _ = LoadIssueDetailsAsync(value.SecId);
    }

    partial void OnSelectedCouponTypeFilterChanged(OfzCouponTypeFilter? value)
    {
        if (_currentResult is null || IsLoading)
        {
            return;
        }

        ApplyCurrentFilter();
    }

    partial void OnSelectedInsightPeriodFilterChanged(OfzInsightPeriodFilter? value)
    {
        if (_currentResult is null || IsLoading)
        {
            return;
        }

        ApplyCurrentFilter();
    }

    partial void OnSelectedSignalScopeFilterChanged(OfzSignalScopeFilter? value)
    {
        if (_currentResult is null || IsLoading)
        {
            return;
        }

        ApplyCurrentFilter();
    }

    private void ClearResults()
    {
        _currentResult = null;
        ClearViewResults();
    }

    private void ClearViewResults()
    {
        TopAnomalies.Clear();
        WeakLiquidityItems.Clear();
        Insights.Clear();
        ActivityIndex = [];
        DurationYieldScatterPoints = [];
        SelectedTopAnomaly = null;
        SelectedWeakLiquidityItem = null;
        HeatmapDates.Clear();
        HeatmapRows.Clear();
        SelectedHeatmapCell = null;
        SelectedHeatmapSummary = "Ячейка не выбрана";
        HasAnomalies = false;
        HasWeakLiquidity = false;
        HasHeatmap = false;
        HasInsights = false;
        HasActivityIndex = false;
        HasDurationYieldScatter = false;
        ActivityIndexStatusMessage = "Индекс не рассчитан";
        DurationYieldScatterStatusMessage = "Scatter не рассчитан";
        ClearIssueDetail();
    }

    private void ApplyCurrentFilter()
    {
        if (_currentResult is null)
        {
            return;
        }

        ClearViewResults();

        var (issues, metrics) = GetFilteredActivityData(_currentResult);
        var signalMetrics = GetSignalMetrics(metrics);
        var anomalies = OfzActivityAnalyzer.GetTopAnomalies(signalMetrics, issues, topCount: 50);
        var heatmapCells = OfzActivityAnalyzer.BuildHeatmapCells(
            metrics,
            issues,
            _currentResult.StartDate,
            _currentResult.EndDate);
        var liquidityMetrics = GetFilteredLiquidityMetrics(_currentResult, issues);
        var signalLiquidityMetrics = GetSignalLiquidityMetrics(liquidityMetrics);
        var insightMetrics = GetInsightMetrics(signalMetrics, _currentResult);
        var insightLiquidityMetrics = GetInsightLiquidityMetrics(signalLiquidityMetrics, _currentResult);
        var insights = OfzActivityAnalyzer.BuildActivityInsights(insightMetrics, issues)
            .Concat(OfzActivityAnalyzer.BuildLiquidityInsights(insightLiquidityMetrics, issues))
            .OrderByDescending(insight => insight.Severity)
            .ThenBy(insight => insight.Kind)
            .ToList();
        var activityIndex = OfzActivityAnalyzer.BuildActivityIndex(signalMetrics);
        var scatterPoints = OfzActivityAnalyzer.BuildDurationYieldScatter(signalMetrics, issues, signalLiquidityMetrics);
        var weakLiquidityItems = OfzActivityAnalyzer.GetWeakLiquidityRankings(signalLiquidityMetrics, issues);

        foreach (var anomaly in anomalies)
        {
            TopAnomalies.Add(anomaly);
        }

        foreach (var weakLiquidityItem in weakLiquidityItems)
        {
            WeakLiquidityItems.Add(weakLiquidityItem);
        }

        ApplyHeatmap(heatmapCells);
        ApplyInsights(insights);
        ApplyActivityIndex(activityIndex);
        ApplyDurationYieldScatter(scatterPoints);

        HasAnomalies = TopAnomalies.Count > 0;
        HasWeakLiquidity = WeakLiquidityItems.Count > 0;
        var filterLabel = SelectedCouponTypeFilter?.DisplayName ?? "Все";
        var insightPeriodLabel = SelectedInsightPeriodFilter?.DisplayName ?? "Весь диапазон";
        var signalScopeLabel = SelectedSignalScopeFilter?.DisplayName ?? "Все дни";
        StatusMessage = HasAnomalies || HasHeatmap || HasInsights || HasActivityIndex || HasDurationYieldScatter
            ? $"Найдено всплесков: {TopAnomalies.Count}; weak liquidity: {WeakLiquidityItems.Count}; heatmap: {HeatmapRows.Count} выпусков; выводов: {Insights.Count}; index: {ActivityIndex.Count}; scatter: {DurationYieldScatterPoints.Count}; тип: {filterLabel}; сигналы: {signalScopeLabel}; период выводов: {insightPeriodLabel}"
            : $"Нет записей с достаточной baseline; тип: {filterLabel}; сигналы: {signalScopeLabel}; период выводов: {insightPeriodLabel}";
    }

    private (IReadOnlyList<OfzIssue> Issues, IReadOnlyList<OfzActivityMetric> Metrics) GetFilteredActivityData(
        OfzActivityLoadResult result)
    {
        var selectedType = SelectedCouponTypeFilter?.CouponType;
        if (!selectedType.HasValue)
        {
            return (result.Issues, result.Metrics);
        }

        var issues = result.Issues
            .Where(issue => issue.CouponType == selectedType.Value)
            .ToList();
        var secIds = issues
            .Select(issue => issue.SecId)
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .ToHashSet(StringComparer.Ordinal);
        var metrics = result.Metrics
            .Where(metric => secIds.Contains(metric.SecId))
            .ToList();

        return (issues, metrics);
    }

    private IReadOnlyList<OfzLiquidityMetric> GetFilteredLiquidityMetrics(
        OfzActivityLoadResult result,
        IReadOnlyList<OfzIssue> issues)
    {
        var metrics = result.LiquidityMetrics
            .Concat(result.SnapshotLiquidityMetrics);
        var selectedType = SelectedCouponTypeFilter?.CouponType;
        if (!selectedType.HasValue)
        {
            return metrics.ToList();
        }

        var secIds = issues
            .Select(issue => issue.SecId)
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .ToHashSet(StringComparer.Ordinal);

        return metrics
            .Where(metric => secIds.Contains(metric.SecId))
            .ToList();
    }

    private IReadOnlyList<OfzActivityMetric> GetSignalMetrics(IReadOnlyList<OfzActivityMetric> metrics)
    {
        if (SelectedSignalScopeFilter?.LastAvailableDayOnly != true)
        {
            return metrics;
        }

        var lastDate = metrics
            .Select(metric => metric.TradeDate.Date)
            .DefaultIfEmpty()
            .Max();
        if (lastDate == default)
        {
            return [];
        }

        return metrics
            .Where(metric => metric.TradeDate.Date == lastDate)
            .ToList();
    }

    private IReadOnlyList<OfzLiquidityMetric> GetSignalLiquidityMetrics(IReadOnlyList<OfzLiquidityMetric> metrics)
    {
        if (SelectedSignalScopeFilter?.LastAvailableDayOnly != true)
        {
            return metrics;
        }

        var lastDate = metrics
            .Select(metric => metric.TradeDate.Date)
            .DefaultIfEmpty()
            .Max();
        if (lastDate == default)
        {
            return [];
        }

        return metrics
            .Where(metric => metric.TradeDate.Date == lastDate)
            .ToList();
    }

    private IReadOnlyList<OfzActivityMetric> GetInsightMetrics(
        IReadOnlyList<OfzActivityMetric> metrics,
        OfzActivityLoadResult result)
    {
        var days = SelectedInsightPeriodFilter?.Days;
        if (!days.HasValue)
        {
            return metrics;
        }

        var startDate = result.StartDate.Date;
        var endDate = result.EndDate.Date;
        if (startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var insightStartDate = endDate.AddDays(-(days.Value - 1));
        if (insightStartDate < startDate)
        {
            insightStartDate = startDate;
        }

        return metrics
            .Where(metric => metric.TradeDate.Date >= insightStartDate && metric.TradeDate.Date <= endDate)
            .ToList();
    }

    private IReadOnlyList<OfzLiquidityMetric> GetInsightLiquidityMetrics(
        IReadOnlyList<OfzLiquidityMetric> metrics,
        OfzActivityLoadResult result)
    {
        var days = SelectedInsightPeriodFilter?.Days;
        if (!days.HasValue)
        {
            return metrics;
        }

        var startDate = result.StartDate.Date;
        var endDate = result.EndDate.Date;
        if (startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var insightStartDate = endDate.AddDays(-(days.Value - 1));
        if (insightStartDate < startDate)
        {
            insightStartDate = startDate;
        }

        return metrics
            .Where(metric => metric.TradeDate.Date >= insightStartDate && metric.TradeDate.Date <= endDate)
            .ToList();
    }

    private void ApplyHeatmap(IReadOnlyList<OfzActivityHeatmapCell> cells)
    {
        foreach (var date in cells.Select(cell => cell.TradeDate.Date).Distinct().OrderBy(date => date))
        {
            HeatmapDates.Add(date);
        }

        foreach (var group in cells
            .GroupBy(cell => cell.SecId, StringComparer.Ordinal)
            .OrderBy(group => group.First().ShortName, StringComparer.CurrentCulture)
            .ThenBy(group => group.Key, StringComparer.Ordinal))
        {
            var orderedCells = group
                .OrderBy(cell => cell.TradeDate)
                .ToList();
            HeatmapRows.Add(new OfzActivityHeatmapRow(
                group.Key,
                orderedCells.First().ShortName,
                orderedCells));
        }

        HasHeatmap = HeatmapDates.Count > 0 && HeatmapRows.Count > 0;
    }

    private void ApplyInsights(IReadOnlyList<OfzActivityInsight> insights)
    {
        foreach (var insight in insights)
        {
            Insights.Add(insight);
        }

        HasInsights = Insights.Count > 0;
    }

    private void ApplyActivityIndex(IReadOnlyList<OfzActivityIndexPoint> points)
    {
        ActivityIndex = new ObservableCollection<OfzActivityIndexPoint>(points);
        HasActivityIndex = ActivityIndex.Count > 0;
        ActivityIndexStatusMessage = HasActivityIndex
            ? $"{ActivityIndex.Count} дат; индекс = active issues * median score"
            : "Нет дат с положительным оборотом для выбранного типа";
    }

    private void ApplyDurationYieldScatter(IReadOnlyList<OfzDurationYieldScatterPoint> points)
    {
        DurationYieldScatterPoints = new ObservableCollection<OfzDurationYieldScatterPoint>(points);
        HasDurationYieldScatter = DurationYieldScatterPoints.Count > 0;
        DurationYieldScatterStatusMessage = HasDurationYieldScatter
            ? $"{DurationYieldScatterPoints.Count} точек; X = дюрация, Y = доходность; заливка/размер = score, обводка/tooltip = liquidity"
            : "Нет точек: нужны Duration и текущая доходность; у ОФЗ-ПК эти поля часто отсутствуют";
    }

    private async Task LoadIssueDetailsAsync(string secId)
    {
        if (string.IsNullOrWhiteSpace(secId) || !StartDate.HasValue || !EndDate.HasValue)
        {
            return;
        }

        var version = ++_detailSelectionVersion;
        IsLoadingIssueDetail = true;
        IssueDetailStatusMessage = $"Загрузка {secId}";

        try
        {
            var detailTask = activityService
                .GetIssueDetailsAsync(secId, StartDate.Value, EndDate.Value, CancellationToken.None);
            var liquidityProfileTask = activityService
                .GetIssueLiquidityProfileAsync(secId, StartDate.Value, EndDate.Value, CancellationToken.None);

            await Task.WhenAll(detailTask, liquidityProfileTask).ConfigureAwait(true);
            var detail = await detailTask.ConfigureAwait(true);
            var liquidityProfile = await liquidityProfileTask.ConfigureAwait(true);

            if (version != _detailSelectionVersion)
            {
                return;
            }

            SelectedIssueDetail = detail;
            SelectedIssueLiquidityProfile = liquidityProfile;
            IssueDetailStatusMessage = detail.HasPoints
                ? $"{detail.ShortName} ({detail.SecId}): {detail.Points.Count} записей"
                : $"{secId}: нет записей за диапазон";
        }
        catch (Exception ex)
        {
            if (version != _detailSelectionVersion)
            {
                return;
            }

            SelectedIssueDetail = null;
            SelectedIssueLiquidityProfile = null;
            IssueDetailStatusMessage = $"Ошибка детализации: {ex.Message}";
        }
        finally
        {
            if (version == _detailSelectionVersion)
            {
                IsLoadingIssueDetail = false;
            }
        }
    }

    private void ClearIssueDetail()
    {
        _detailSelectionVersion++;
        SelectedIssueDetail = null;
        SelectedIssueLiquidityProfile = null;
        IsLoadingIssueDetail = false;
        IssueDetailStatusMessage = "Выпуск не выбран";
    }

    private async Task RunWithProgressAsync(
        string operationName,
        Func<IProgress<SyncProgress>, CancellationToken, Task> operation,
        bool updateRows)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        LoadingProgress = 0;
        ErrorMessage = null;
        StatusMessage = operationName;

        var progress = new Progress<SyncProgress>(syncProgress =>
        {
            LoadingProgress = syncProgress.Ratio * 100;
            if (syncProgress.CurrentDate.HasValue)
            {
                StatusMessage = $"{operationName}: {syncProgress.CurrentDate:yyyy-MM-dd}";
            }
        });

        try
        {
            await operation(progress, CancellationToken.None).ConfigureAwait(true);

            if (!updateRows)
            {
                StatusMessage = "История загружена";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Ошибка загрузки";
        }
        finally
        {
            LoadingProgress = 0;
            IsLoading = false;
        }
    }

    private static string FormatHeatmapCell(OfzActivityHeatmapCell cell)
    {
        var score = cell.ActivityScore.HasValue ? $"{cell.ActivityScore.Value:N2}" : "n/a";
        var value = cell.Value.HasValue ? $"{cell.Value.Value:N0}" : "n/a";
        var yieldMove = cell.YieldMove.HasValue ? $"{cell.YieldMove.Value:N2}" : "n/a";

        return $"{cell.TradeDate:yyyy-MM-dd} {cell.ShortName} ({cell.SecId}): score {score}, оборот {value}, yield Δ {yieldMove}, {cell.Status}";
    }

    private static IEnumerable<OfzCouponTypeFilter> CreateCouponTypeFilters()
    {
        yield return new OfzCouponTypeFilter("Все", null);
        yield return new OfzCouponTypeFilter("ОФЗ-ПД", OfzCouponType.Fixed);
        yield return new OfzCouponTypeFilter("ОФЗ-ПК", OfzCouponType.Floating);
        yield return new OfzCouponTypeFilter("ОФЗ-ИН", OfzCouponType.InflationLinked);
        yield return new OfzCouponTypeFilter("ОФЗ-АД", OfzCouponType.Amortized);
        yield return new OfzCouponTypeFilter("Валютные", OfzCouponType.Currency);
        yield return new OfzCouponTypeFilter("Тип n/a", OfzCouponType.Unknown);
    }

    private static IEnumerable<OfzInsightPeriodFilter> CreateInsightPeriodFilters()
    {
        yield return new OfzInsightPeriodFilter("7 дней", 7);
        yield return new OfzInsightPeriodFilter("14 дней", 14);
        yield return new OfzInsightPeriodFilter("Весь диапазон", null);
    }

    private static IEnumerable<OfzSignalScopeFilter> CreateSignalScopeFilters()
    {
        yield return new OfzSignalScopeFilter("Все дни", false);
        yield return new OfzSignalScopeFilter("Последний день", true);
    }

}

public sealed class OfzCouponTypeFilter(string displayName, OfzCouponType? couponType)
{
    public string DisplayName { get; } = displayName;
    public OfzCouponType? CouponType { get; } = couponType;
}

public sealed class OfzInsightPeriodFilter(string displayName, int? days)
{
    public string DisplayName { get; } = displayName;
    public int? Days { get; } = days;
}

public sealed class OfzSignalScopeFilter(string displayName, bool lastAvailableDayOnly)
{
    public string DisplayName { get; } = displayName;
    public bool LastAvailableDayOnly { get; } = lastAvailableDayOnly;
}

public sealed class OfzActivityHeatmapRow(
    string secId,
    string shortName,
    IEnumerable<OfzActivityHeatmapCell> cells)
{
    public string SecId { get; } = secId;
    public string ShortName { get; } = shortName;
    public ObservableCollection<OfzActivityHeatmapCell> Cells { get; } = new(cells);
}
