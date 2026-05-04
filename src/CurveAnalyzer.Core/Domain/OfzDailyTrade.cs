namespace CurveAnalyzer.Core;

public class OfzDailyTrade
{
    public string BoardId { get; set; } = "TQOB";
    public string SecId { get; set; } = string.Empty;
    public DateTime TradeDate { get; set; }
    public int? NumTrades { get; set; }
    public double? Value { get; set; }
    public double? Volume { get; set; }
    public double? OpenPrice { get; set; }
    public double? LowPrice { get; set; }
    public double? HighPrice { get; set; }
    public double? ClosePrice { get; set; }
    public double? WeightedAveragePrice { get; set; }
    public double? YieldClose { get; set; }
    public double? YieldAtWeightedAveragePrice { get; set; }
    public double? Duration { get; set; }
    public double? Bid { get; set; }
    public double? Offer { get; set; }
    public double? Spread { get; set; }
    public double? HighBid { get; set; }
    public double? LowOffer { get; set; }
    public double? ZSpread { get; set; }
    public double? ZSpreadAtWeightedAveragePrice { get; set; }
    public double? ImpliedFloatingRate { get; set; }
    public double? ImpliedInflation { get; set; }
    public double? ImpliedCbrRate { get; set; }
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;

    public OfzIssue? Issue { get; set; }

    public double? PreferredYield => NormalizePositive(YieldAtWeightedAveragePrice) ?? NormalizePositive(YieldClose);

    public double? PreferredPrice => NormalizePositive(WeightedAveragePrice) ?? NormalizePositive(ClosePrice);

    private static double? NormalizePositive(double? value)
    {
        return value is > 0 ? value : null;
    }
}
