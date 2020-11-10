using System;

namespace CurveAnalyzer
{
    public class DateRange
    {
        public DateTime StartDate;
        public DateTime EndDate;

        public DateRange(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        public bool IsInRange(DateTime date)
        {
            return date >= StartDate && date <= EndDate;
        }
    }
}
