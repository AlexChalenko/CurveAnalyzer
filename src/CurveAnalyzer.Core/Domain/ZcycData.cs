namespace CurveAnalyzer.Core;

public record ZcycData(DateTime Date, List<ZcycDataRow> DataRow)
{
    public ZcycData() : this(default, [])
    {
    }
}
