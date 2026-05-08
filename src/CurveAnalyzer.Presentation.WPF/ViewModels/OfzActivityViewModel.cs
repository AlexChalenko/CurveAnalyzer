using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Application;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.ViewModels;

public partial class OfzActivityViewModel(OfzActivityService activityService) : ObservableObject, IChartViewModel
{
    private const string BreadthDayPlaceholder = "Выберите строку во вкладке \"Ширина\" или вывод по ширине рынка.";
    private const string IndexContextDayPlaceholder = "Выберите строку во вкладке \"Индекс\" или индексный вывод.";

    private bool _initialized;
    private int _detailSelectionVersion;
    private OfzActivityLoadResult? _currentResult;
    private static readonly JsonSerializerOptions SummaryJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

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
    public partial OfzMarketSummary? MarketSummary { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzSummaryFinding> SummaryFindings { get; set; } = [];

    [ObservableProperty]
    public partial OfzSummaryFinding? SelectedSummaryFinding { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzSegmentSummary> SegmentSummaries { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<OfzDataLimitation> SummaryLimitations { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<OfzIndexContextDay> IndexContextDays { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<OfzIndexSegmentContext> IndexSegments { get; set; } = [];

    [ObservableProperty]
    public partial OfzIndexContextDay? SelectedIndexContextDay { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzIndexContextPoint> SelectedIndexContextPoints { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<OfzDataLimitation> SelectedIndexLimitations { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<MarketBreadthContributor> SelectedBreadthContributors { get; set; } = [];

    [ObservableProperty]
    public partial MarketBreadthDay? SelectedBreadthDay { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OfzDataLimitation> SelectedBreadthLimitations { get; set; } = [];

    [ObservableProperty]
    public partial string StructuredSummaryJson { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedSummaryEvidenceText { get; set; } = "Вывод не выбран";

    [ObservableProperty]
    public partial string SelectedBreadthDaySummary { get; set; } = BreadthDayPlaceholder;

    [ObservableProperty]
    public partial string IndexContextStatusMessage { get; set; } = "Индексный контекст не рассчитан";

    [ObservableProperty]
    public partial string SelectedIndexContextDaySummary { get; set; } = IndexContextDayPlaceholder;

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
    public partial bool HasMarketSummary { get; set; }

    [ObservableProperty]
    public partial bool HasSegmentSummaries { get; set; }

    [ObservableProperty]
    public partial bool HasSummaryLimitations { get; set; }

    [ObservableProperty]
    public partial bool HasIndexContext { get; set; }

    [ObservableProperty]
    public partial bool HasIndexSegments { get; set; }

    [ObservableProperty]
    public partial bool HasSelectedIndexContextPoints { get; set; }

    [ObservableProperty]
    public partial bool HasSelectedIndexLimitations { get; set; }

    [ObservableProperty]
    public partial bool HasSelectedBreadthContributors { get; set; }

    [ObservableProperty]
    public partial bool HasSelectedBreadthLimitations { get; set; }

    [ObservableProperty]
    public partial bool HasStructuredSummaryJson { get; set; }

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

    public void ClearOverviewTransientSelection()
    {
        ClearSelectedBreadthDay();
        ClearSelectedIndexContextDay();
    }

    [RelayCommand(CanExecute = nameof(CanCopySummaryJson))]
    private void CopySummaryJson()
    {
        if (string.IsNullOrWhiteSpace(StructuredSummaryJson))
        {
            return;
        }

        Clipboard.SetText(StructuredSummaryJson);
        StatusMessage = "Structured summary JSON скопирован";
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

    partial void OnSelectedSummaryFindingChanged(OfzSummaryFinding? value)
    {
        ClearSelectedBreadthDay();
        ClearSelectedIndexContextDay();
        SelectedSummaryEvidenceText = FormatSummaryEvidence(value);

        if (value is null)
        {
            return;
        }

        if (value.DrillDown?.Target == OfzSummaryDrillDownTarget.MarketBreadthDay &&
            value.DrillDown.TradeDate is DateTime breadthDate)
        {
            FocusMarketBreadthDay(breadthDate, value);
        }

        if (value.DrillDown?.Target == OfzSummaryDrillDownTarget.SegmentDetail &&
            value.DrillDown.CouponType is OfzCouponType couponType)
        {
            FocusSegment(couponType, value);
        }

        if (value.DrillDown?.Target == OfzSummaryDrillDownTarget.IndexContextDay &&
            value.DrillDown.TradeDate is DateTime indexDate)
        {
            FocusIndexContextDay(indexDate, value);
        }

        var secId = value.DrillDown?.SecId ?? value.Evidence.SecId;
        if (!string.IsNullOrWhiteSpace(secId))
        {
            _ = LoadIssueDetailsAsync(secId);
        }

        if (value.DrillDown?.TradeDate is DateTime tradeDate)
        {
            SelectedHeatmapSummary = $"{tradeDate:yyyy-MM-dd}: {value.Title}. {value.Text}";
        }
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

    partial void OnSelectedIndexContextDayChanged(OfzIndexContextDay? value)
    {
        ClearSelectedIndexContextDayDetails();

        if (value is not null)
        {
            ApplySelectedIndexContextDay(value);
        }
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
        SummaryFindings.Clear();
        SegmentSummaries.Clear();
        SummaryLimitations.Clear();
        IndexContextDays.Clear();
        IndexSegments.Clear();
        ClearSelectedBreadthDay();
        ClearSelectedIndexContextDay();
        MarketSummary = null;
        StructuredSummaryJson = string.Empty;
        SelectedSummaryFinding = null;
        SelectedSummaryEvidenceText = "Вывод не выбран";
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
        HasMarketSummary = false;
        HasSegmentSummaries = false;
        HasSummaryLimitations = false;
        HasIndexContext = false;
        HasIndexSegments = false;
        HasSelectedBreadthContributors = false;
        HasSelectedBreadthLimitations = false;
        HasSelectedIndexContextPoints = false;
        HasSelectedIndexLimitations = false;
        HasStructuredSummaryJson = false;
        CopySummaryJsonCommand.NotifyCanExecuteChanged();
        HasActivityIndex = false;
        HasDurationYieldScatter = false;
        ActivityIndexStatusMessage = "Индекс не рассчитан";
        DurationYieldScatterStatusMessage = "Scatter не рассчитан";
        IndexContextStatusMessage = "Индексный контекст не рассчитан";
        ClearIssueDetail();
    }

    private void ApplyCurrentFilter()
    {
        if (_currentResult is null)
        {
            return;
        }

        var selectedSecId = GetSelectedIssueSecId();
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
        var trades = GetFilteredTrades(_currentResult, issues);
        var signalLiquidityMetrics = GetSignalLiquidityMetrics(liquidityMetrics);
        var insightMetrics = GetInsightMetrics(signalMetrics, _currentResult);
        var insightLiquidityMetrics = GetInsightLiquidityMetrics(signalLiquidityMetrics, _currentResult);
        var insightRange = GetInsightDateRange(_currentResult);
        var marketSummary = OfzMarketSummaryBuilder.Build(new OfzMarketSummaryInput
        {
            StartDate = _currentResult.StartDate,
            EndDate = _currentResult.EndDate,
            InsightStartDate = insightRange.StartDate,
            InsightEndDate = insightRange.EndDate,
            CouponTypeFilter = SelectedCouponTypeFilter?.CouponType,
            SignalScope = SelectedSignalScopeFilter?.LastAvailableDayOnly == true
                ? OfzSummarySignalScope.LastAvailableDay
                : OfzSummarySignalScope.AllDays,
            Issues = issues,
            Trades = trades,
            ActivityMetrics = metrics,
            LiquidityMetrics = liquidityMetrics,
            CbrKeyRates = _currentResult.CbrKeyRates,
            IndexPoints = _currentResult.IndexPoints
        });
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
        ApplyMarketSummary(marketSummary);
        ApplyActivityIndex(activityIndex);
        ApplyDurationYieldScatter(scatterPoints);
        RestoreIssueSelection(selectedSecId, issues);

        HasAnomalies = TopAnomalies.Count > 0;
        HasWeakLiquidity = WeakLiquidityItems.Count > 0;
        var filterLabel = SelectedCouponTypeFilter?.DisplayName ?? "Все";
        var insightPeriodLabel = SelectedInsightPeriodFilter?.DisplayName ?? "Весь диапазон";
        var signalScopeLabel = SelectedSignalScopeFilter?.DisplayName ?? "Все дни";
        StatusMessage = HasAnomalies || HasHeatmap || HasMarketSummary || HasActivityIndex || HasDurationYieldScatter
            ? $"Найдено всплесков: {TopAnomalies.Count}; weak liquidity: {WeakLiquidityItems.Count}; heatmap: {HeatmapRows.Count} выпусков; выводов: {SummaryFindings.Count}; дней breadth в выводах: {marketSummary.BreadthDays.Count}; index context: {IndexContextDays.Count}; index: {ActivityIndex.Count}; scatter: {DurationYieldScatterPoints.Count}; тип: {filterLabel}; сигналы: {signalScopeLabel}; период выводов: {insightPeriodLabel}"
            : $"Нет записей с достаточной baseline; тип: {filterLabel}; сигналы: {signalScopeLabel}; период выводов: {insightPeriodLabel}";
    }

    private string? GetSelectedIssueSecId()
    {
        if (!string.IsNullOrWhiteSpace(SelectedIssueDetail?.SecId))
        {
            return SelectedIssueDetail.SecId;
        }

        if (!string.IsNullOrWhiteSpace(SelectedTopAnomaly?.SecId))
        {
            return SelectedTopAnomaly.SecId;
        }

        if (!string.IsNullOrWhiteSpace(SelectedWeakLiquidityItem?.SecId))
        {
            return SelectedWeakLiquidityItem.SecId;
        }

        return !string.IsNullOrWhiteSpace(SelectedHeatmapCell?.SecId)
            ? SelectedHeatmapCell.SecId
            : null;
    }

    private void RestoreIssueSelection(string? preferredSecId, IReadOnlyList<OfzIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(preferredSecId))
        {
            var matchingAnomaly = TopAnomalies.FirstOrDefault(item =>
                string.Equals(item.SecId, preferredSecId, StringComparison.Ordinal));
            if (matchingAnomaly is not null)
            {
                SelectedTopAnomaly = matchingAnomaly;
                return;
            }

            var matchingWeakLiquidity = WeakLiquidityItems.FirstOrDefault(item =>
                string.Equals(item.SecId, preferredSecId, StringComparison.Ordinal));
            if (matchingWeakLiquidity is not null)
            {
                SelectedWeakLiquidityItem = matchingWeakLiquidity;
                return;
            }

            var matchingIssue = issues.FirstOrDefault(issue =>
                string.Equals(issue.SecId, preferredSecId, StringComparison.Ordinal));
            if (matchingIssue is not null)
            {
                _ = LoadIssueDetailsAsync(preferredSecId);
                return;
            }
        }

        if (TopAnomalies.FirstOrDefault() is { } firstAnomaly)
        {
            SelectedTopAnomaly = firstAnomaly;
            return;
        }

        if (WeakLiquidityItems.FirstOrDefault() is { } firstWeakLiquidity)
        {
            SelectedWeakLiquidityItem = firstWeakLiquidity;
        }
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

    private IReadOnlyList<OfzDailyTrade> GetFilteredTrades(
        OfzActivityLoadResult result,
        IReadOnlyList<OfzIssue> issues)
    {
        var selectedType = SelectedCouponTypeFilter?.CouponType;
        if (!selectedType.HasValue)
        {
            return result.Trades;
        }

        var secIds = issues
            .Select(issue => issue.SecId)
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .ToHashSet(StringComparer.Ordinal);

        return result.Trades
            .Where(trade => secIds.Contains(trade.SecId))
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

    private (DateTime StartDate, DateTime EndDate) GetInsightDateRange(OfzActivityLoadResult result)
    {
        var startDate = result.StartDate.Date;
        var endDate = result.EndDate.Date;
        if (startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var days = SelectedInsightPeriodFilter?.Days;
        if (!days.HasValue)
        {
            return (startDate, endDate);
        }

        var insightStartDate = endDate.AddDays(-(days.Value - 1));
        if (insightStartDate < startDate)
        {
            insightStartDate = startDate;
        }

        return (insightStartDate, endDate);
    }

    private IReadOnlyList<OfzActivityMetric> GetInsightMetrics(
        IReadOnlyList<OfzActivityMetric> metrics,
        OfzActivityLoadResult result)
    {
        var (insightStartDate, insightEndDate) = GetInsightDateRange(result);

        return metrics
            .Where(metric => metric.TradeDate.Date >= insightStartDate && metric.TradeDate.Date <= insightEndDate)
            .ToList();
    }

    private IReadOnlyList<OfzLiquidityMetric> GetInsightLiquidityMetrics(
        IReadOnlyList<OfzLiquidityMetric> metrics,
        OfzActivityLoadResult result)
    {
        var (insightStartDate, insightEndDate) = GetInsightDateRange(result);

        return metrics
            .Where(metric => metric.TradeDate.Date >= insightStartDate && metric.TradeDate.Date <= insightEndDate)
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

    private void ApplyMarketSummary(OfzMarketSummary summary)
    {
        MarketSummary = summary;

        foreach (var finding in summary.Findings)
        {
            SummaryFindings.Add(finding);
        }

        foreach (var segment in summary.Segments)
        {
            SegmentSummaries.Add(segment);
        }

        foreach (var limitation in summary.Limitations)
        {
            SummaryLimitations.Add(limitation);
        }

        foreach (var day in summary.IndexContextDays.OrderByDescending(day => day.TradeDate))
        {
            IndexContextDays.Add(day);
        }

        foreach (var segment in summary.IndexSegments.OrderBy(segment => segment.Bucket))
        {
            IndexSegments.Add(segment);
        }

        StructuredSummaryJson = JsonSerializer.Serialize(summary, SummaryJsonOptions);
        HasStructuredSummaryJson = !string.IsNullOrWhiteSpace(StructuredSummaryJson);
        HasMarketSummary = SummaryFindings.Count > 0 || summary.Limitations.Count > 0 || IndexContextDays.Count > 0;
        HasSegmentSummaries = SegmentSummaries.Count > 0;
        HasSummaryLimitations = SummaryLimitations.Count > 0;
        HasIndexContext = IndexContextDays.Any(day => day.Points.Count > 0);
        HasIndexSegments = IndexSegments.Count > 0;
        IndexContextStatusMessage = HasIndexContext
            ? $"{IndexContextDays.Count} дней; точек {summary.SourceCounts.IndexPoints}; source history/snapshot сохраняется в evidence"
            : "Индексный контекст отсутствует: значения не заменяются нулями";
        SelectedSummaryFinding = SummaryFindings.FirstOrDefault();
        SelectedIndexContextDay = IndexContextDays.FirstOrDefault(day => day.Points.Count > 0) ?? IndexContextDays.FirstOrDefault();
        CopySummaryJsonCommand.NotifyCanExecuteChanged();
    }

    private void FocusSegment(OfzCouponType couponType, OfzSummaryFinding finding)
    {
        var segment = SegmentSummaries.FirstOrDefault(item => item.CouponType == couponType);
        var marker = segment?.CouponTypeMarker ?? finding.Evidence.CouponTypeMarker ?? couponType.ToString();
        SelectedHeatmapSummary = segment is null
            ? $"Сегмент {marker}: {finding.Text}"
            : $"Сегмент {marker}: активных {segment.ActiveIssueCount}, оборот {segment.TotalValue:N0} {segment.TotalValueUnitMarker}, weak liquidity {segment.WeakLiquidityCount}";

        var filter = CouponTypeFilters.FirstOrDefault(item => item.CouponType == couponType);
        if (filter is not null && SelectedCouponTypeFilter?.CouponType != filter.CouponType)
        {
            SelectedCouponTypeFilter = filter;
        }
    }

    partial void OnSelectedBreadthDayChanged(MarketBreadthDay? value)
    {
        ClearSelectedBreadthDayDetails();

        if (value is not null)
        {
            ApplySelectedBreadthDay(value);
        }
    }

    private void FocusMarketBreadthDay(DateTime tradeDate, OfzSummaryFinding finding)
    {
        var day = MarketSummary?.BreadthDays.FirstOrDefault(item => item.TradeDate.Date == tradeDate.Date);
        if (day is null)
        {
            SelectedBreadthDaySummary = $"{tradeDate:yyyy-MM-dd}: {finding.Text}";
            return;
        }

        ApplySelectedBreadthDay(day);
    }

    private void FocusIndexContextDay(DateTime tradeDate, OfzSummaryFinding finding)
    {
        var day = IndexContextDays.FirstOrDefault(item => item.TradeDate.Date == tradeDate.Date);
        if (day is null)
        {
            SelectedIndexContextDaySummary = $"{tradeDate:yyyy-MM-dd}: {finding.Text}";
            return;
        }

        SelectedIndexContextDay = day;
    }

    private void ApplySelectedBreadthDay(MarketBreadthDay day)
    {
        SelectedBreadthDaySummary =
            $"{day.TradeDate:yyyy-MM-dd}: активных {day.ActiveIssueCount}/{day.IssueCount}, " +
            $"сравнимых {day.ComparableIssueCount}, рост {day.Direction.YieldUpCount}, " +
            $"снижение {day.Direction.YieldDownCount}, без изм. {day.Direction.UnchangedCount}, " +
            $"top-5 {FormatPercent(day.Concentration.Top5Share)}";

        foreach (var contributor in day.TopContributors
            .OrderByDescending(item => item.Value ?? 0)
            .ThenBy(item => item.ShortName, StringComparer.CurrentCulture)
            .ThenBy(item => item.SecId, StringComparer.Ordinal))
        {
            SelectedBreadthContributors.Add(contributor);
        }

        foreach (var limitation in day.Limitations)
        {
            SelectedBreadthLimitations.Add(limitation);
        }

        HasSelectedBreadthContributors = SelectedBreadthContributors.Count > 0;
        HasSelectedBreadthLimitations = SelectedBreadthLimitations.Count > 0;
    }

    private void ClearSelectedBreadthDay()
    {
        SelectedBreadthDay = null;
        ClearSelectedBreadthDayDetails();
    }

    private void ClearSelectedBreadthDayDetails()
    {
        SelectedBreadthContributors.Clear();
        SelectedBreadthLimitations.Clear();
        SelectedBreadthDaySummary = BreadthDayPlaceholder;
        HasSelectedBreadthContributors = false;
        HasSelectedBreadthLimitations = false;
    }

    private void ApplySelectedIndexContextDay(OfzIndexContextDay day)
    {
        var price = day.PriceIndexPoint;
        var totalReturn = day.TotalReturnIndexPoint;
        SelectedIndexContextDaySummary =
            $"{day.TradeDate:yyyy-MM-dd}: RGBI {FormatIndexPoint(price)}, RGBITR {FormatIndexPoint(totalReturn)}, направление {day.MarketDirection}";

        foreach (var point in day.Points
            .OrderBy(point => point.Role)
            .ThenBy(point => point.DurationBucket)
            .ThenBy(point => point.ReturnKind)
            .ThenBy(point => point.SecId, StringComparer.Ordinal))
        {
            SelectedIndexContextPoints.Add(point);
        }

        foreach (var limitation in day.Limitations)
        {
            SelectedIndexLimitations.Add(limitation);
        }

        HasSelectedIndexContextPoints = SelectedIndexContextPoints.Count > 0;
        HasSelectedIndexLimitations = SelectedIndexLimitations.Count > 0;
    }

    private void ClearSelectedIndexContextDay()
    {
        SelectedIndexContextDay = null;
        ClearSelectedIndexContextDayDetails();
    }

    private void ClearSelectedIndexContextDayDetails()
    {
        SelectedIndexContextPoints.Clear();
        SelectedIndexLimitations.Clear();
        SelectedIndexContextDaySummary = IndexContextDayPlaceholder;
        HasSelectedIndexContextPoints = false;
        HasSelectedIndexLimitations = false;
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

    private bool CanCopySummaryJson()
    {
        return HasStructuredSummaryJson;
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

    private static string FormatSummaryEvidence(OfzSummaryFinding? finding)
    {
        if (finding is null)
        {
            return "Вывод не выбран";
        }

        var evidence = finding.Evidence;
        List<string> parts = [];

        if (evidence.TradeDate.HasValue)
        {
            parts.Add($"дата {evidence.TradeDate:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(evidence.ShortName) || !string.IsNullOrWhiteSpace(evidence.SecId))
        {
            parts.Add($"{evidence.ShortName ?? evidence.SecId} ({evidence.SecId ?? "secid n/a"})");
        }

        if (!string.IsNullOrWhiteSpace(evidence.CouponTypeMarker))
        {
            parts.Add(evidence.CouponTypeMarker);
        }

        var activeIssueCountLabel = finding.Kind == OfzSummaryFindingKind.RepeatedIssue
            ? "дат активности"
            : "выпусков";
        AddNumber(parts, activeIssueCountLabel, evidence.ActiveIssueCount, "N0");
        AddNumber(parts, "сравнимых выпусков", evidence.ComparableIssueCount, "N0");
        AddNumber(parts, "без пары доходности", evidence.NotComparableIssueCount, "N0");
        AddNumber(parts, "оборот", evidence.TotalValue ?? evidence.Value, "N0");
        AddNumber(parts, "сделок", evidence.TotalNumTrades ?? evidence.NumTrades, "N0");
        AddPercent(parts, "доля активных", evidence.ActiveIssueShare);
        AddNumber(parts, "score", evidence.ActivityScore ?? evidence.MaxActivityScore ?? evidence.MedianActivityScore, "N2");
        AddNumber(parts, "yield Δ", evidence.YieldMove, "N2");
        AddNumber(parts, "yield up", evidence.YieldUpCount, "N0");
        AddNumber(parts, "yield down", evidence.YieldDownCount, "N0");
        AddNumber(parts, "yield flat", evidence.UnchangedCount, "N0");

        if (evidence.DominantDirection.HasValue)
        {
            parts.Add($"dominant {evidence.DominantDirection.Value}");
        }

        AddPercent(parts, "dominant share", evidence.DominantDirectionShare);
        AddNumber(parts, "median yield Δ", evidence.MedianYieldMove, "N2");
        AddNumber(parts, "turnover base", evidence.IssueBaseCount, "N0");
        AddPercent(parts, "top-5 share", evidence.Top5Share);
        AddPercent(parts, "top-10 share", evidence.Top10Share);
        AddPercent(parts, "type share", evidence.ValueShare);
        AddNumber(parts, "unknown type", evidence.MissingTypeCount, "N0");
        AddNumber(parts, "spread", evidence.Spread, "N3");
        AddNumber(parts, "liquidity", evidence.LiquidityScore, "N2");

        if (!string.IsNullOrWhiteSpace(evidence.IndexSecId))
        {
            parts.Add($"index {evidence.IndexSecId}");
        }

        AddNumber(parts, "index close", evidence.IndexClose, "N2");
        AddPercent(parts, "index Δ", evidence.IndexDailyChangePercent);
        AddNumber(parts, "index yield", evidence.IndexYield, "N2");
        AddNumber(parts, "index yield Δ", evidence.IndexYieldChange, "N2");
        AddNumber(parts, "index duration", evidence.IndexDuration, "N2");

        if (evidence.IndexPreviousTradeDate.HasValue)
        {
            parts.Add($"index prev {evidence.IndexPreviousTradeDate:yyyy-MM-dd}");
        }

        if (evidence.IndexDirection.HasValue)
        {
            parts.Add($"index direction {evidence.IndexDirection.Value}");
        }

        if (evidence.IndexMoveIsMeaningful)
        {
            parts.Add("index move meaningful");
        }

        if (evidence.LiquidityBucket.HasValue)
        {
            parts.Add($"bucket {evidence.LiquidityBucket.Value}");
        }

        if (evidence.LiquidityStatus.HasValue)
        {
            parts.Add($"status {evidence.LiquidityStatus.Value}");
        }

        if (evidence.IsSnapshot)
        {
            parts.Add("snapshot");
        }

        if (evidence.IsProvisional)
        {
            parts.Add("предварительно");
        }

        if (finding.Limitations.Count > 0)
        {
            parts.Add("ограничения: " + string.Join("; ", finding.Limitations.Select(limitation => limitation.Text)));
        }

        return parts.Count == 0 ? finding.Text : string.Join("; ", parts);
    }

    private static void AddNumber(List<string> parts, string label, double? value, string format)
    {
        if (value.HasValue)
        {
            parts.Add($"{label} {value.Value.ToString(format)}");
        }
    }

    private static void AddNumber(List<string> parts, string label, int? value, string format)
    {
        if (value.HasValue)
        {
            parts.Add($"{label} {value.Value.ToString(format)}");
        }
    }

    private static void AddPercent(List<string> parts, string label, double? value)
    {
        if (value.HasValue)
        {
            parts.Add($"{label} {value.Value:P0}");
        }
    }

    private static string FormatPercent(double? value)
    {
        return value.HasValue ? value.Value.ToString("P0") : "n/a";
    }

    private static string FormatSignedPercent(double? value)
    {
        return value.HasValue ? value.Value.ToString("+0.00%;-0.00%;0.00%") : "n/a";
    }

    private static string FormatIndexPoint(OfzIndexContextPoint? point)
    {
        return point is null
            ? "n/a"
            : $"{FormatNullable(point.Close, "N2")} ({FormatSignedPercent(point.DailyChangePercent)})";
    }

    private static string FormatNullable(double? value, string format)
    {
        return value.HasValue && double.IsFinite(value.Value)
            ? value.Value.ToString(format)
            : "n/a";
    }

    private static IEnumerable<OfzCouponTypeFilter> CreateCouponTypeFilters()
    {
        yield return new OfzCouponTypeFilter("Все", null);
        yield return new OfzCouponTypeFilter("ОФЗ-ПД", OfzCouponType.Fixed);
        yield return new OfzCouponTypeFilter("ОФЗ-ПК", OfzCouponType.Floating);
        yield return new OfzCouponTypeFilter("ОФЗ-ИН", OfzCouponType.InflationLinked);
        yield return new OfzCouponTypeFilter("ОФЗ-АД", OfzCouponType.Amortized);
        yield return new OfzCouponTypeFilter("Валютные", OfzCouponType.Currency);
        yield return new OfzCouponTypeFilter("Unknown", OfzCouponType.Unknown);
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
