using System.Text.Json.Serialization;

namespace CurveAnalyzer.Core;

public enum OfzYieldDirection
{
    NotComparable = 0,
    Up = 1,
    Down = 2,
    Unchanged = 3
}

public enum OfzDominantYieldDirection
{
    None = 0,
    Up = 1,
    Down = 2,
    Mixed = 3,
    Flat = 4
}

public enum MarketBreadthContributorReason
{
    TopTurnover = 0,
    YieldUp = 1,
    YieldDown = 2,
    WeakLiquidity = 3,
    HighActivity = 4
}

public sealed class OfzMarketBreadthOptions
{
    public double UnchangedYieldMoveThreshold { get; init; } = 0.01;
    public double BroadMoveShare { get; init; } = 0.6;
    public int MinimumComparableIssuesForBroadMove { get; init; } = 3;
    public double HighTop5Share { get; init; } = 0.75;
    public double HighTop10Share { get; init; } = 0.9;
    public double TypeDominanceShare { get; init; } = 0.5;
    public int MaxTopContributors { get; init; } = 10;
}

public sealed class MarketBreadthDay
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public int IssueCount { get; init; }
    public int ComparableIssueCount { get; init; }
    public int NotComparableIssueCount { get; init; }
    public int ActiveIssueCount { get; init; }
    public double? ActiveIssueShare { get; init; }
    public double? TotalValue { get; init; }
    public int? NumTrades { get; init; }
    public YieldDirectionBreakdown Direction { get; init; } = new();
    public TurnoverConcentration Concentration { get; init; } = new();
    public IReadOnlyList<TypeTurnoverShare> TypeShares { get; init; } = [];
    public IReadOnlyList<MarketBreadthContributor> TopContributors { get; init; } = [];
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
    public bool IsProvisional { get; init; }
}

public sealed class YieldDirectionBreakdown
{
    public int YieldUpCount { get; init; }
    public int YieldDownCount { get; init; }
    public int UnchangedCount { get; init; }
    public int NotComparableCount { get; init; }
    public OfzDominantYieldDirection DominantDirection { get; init; } = OfzDominantYieldDirection.None;
    public double? DominantDirectionShare { get; init; }
    public double? MedianYieldMove { get; init; }
}

public sealed class TurnoverConcentration
{
    public double? TotalValue { get; init; }
    public int IssueBaseCount { get; init; }
    public double? Top5Value { get; init; }
    public double? Top5Share { get; init; }
    public double? Top10Value { get; init; }
    public double? Top10Share { get; init; }
    public IReadOnlyList<MarketBreadthContributor> TopIssues { get; init; } = [];
    public bool IsHighConcentration { get; init; }
}

public sealed class TypeTurnoverShare
{
    public OfzCouponType CouponType { get; init; }
    public string CouponTypeMarker { get; init; } = string.Empty;
    public int IssueCount { get; init; }
    public int ActiveIssueCount { get; init; }
    public double? TotalValue { get; init; }
    public double? ValueShare { get; init; }
    public int? NumTrades { get; init; }
    public int MissingTypeCount { get; init; }
}

public sealed class MarketBreadthContributor
{
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public OfzCouponType? CouponType { get; init; }
    public string? CouponTypeMarker { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public double? Value { get; init; }
    public double? ValueShare { get; init; }
    public int? NumTrades { get; init; }
    public double? YieldMove { get; init; }
    public OfzYieldDirection YieldDirection { get; init; } = OfzYieldDirection.NotComparable;
    public double? ActivityScore { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket? LiquidityBucket { get; init; }
    public double? Spread { get; init; }
    public MarketBreadthContributorReason Reason { get; init; }
}
