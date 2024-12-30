namespace CurveAnalyzer.Core;
public class DateRange
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        Start = startDate;
        End = endDate;
    }

    public bool IsInRange(DateTime date)
    {
        return date >= Start && date <= End;
    }
}
