using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public sealed record OfzActivityDailyData(
    string BoardId,
    DateTime TradeDate,
    IReadOnlyList<OfzIssue> Issues,
    IReadOnlyList<OfzDailyTrade> Trades,
    OfzActivityLoadState LoadState);
