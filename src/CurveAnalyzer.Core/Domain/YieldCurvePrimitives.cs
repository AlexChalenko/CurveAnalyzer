namespace CurveAnalyzer.Core;

public readonly record struct TradingDate : IComparable<TradingDate>
{
    public DateTime Date { get; }

    public TradingDate(DateTime date)
    {
        Date = date.Date;
    }

    public int CompareTo(TradingDate other)
    {
        return Date.CompareTo(other.Date);
    }
}

public readonly record struct CurvePeriod : IComparable<CurvePeriod>
{
    public double Value { get; }

    public CurvePeriod(double value)
    {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Curve period must be a positive finite value.");
        }

        Value = value;
    }

    public int CompareTo(CurvePeriod other)
    {
        return Value.CompareTo(other.Value);
    }
}

public readonly record struct YieldPoint(TradingDate TradingDate, CurvePeriod Period, double Value);
