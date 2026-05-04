using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application;

public sealed record OfzActivityLoadResult(
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<OfzIssue> Issues,
    IReadOnlyList<OfzDailyTrade> Trades,
    IReadOnlyList<OfzActivityMetric> Metrics);
