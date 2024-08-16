using System;
using System.Globalization;

namespace CurveAnalyzer.Tools
{
    public static class MyExtensions
    {
        public static int YearAndWeekToNumber(this DateTime date)
        {
            CultureInfo culInfo = CultureInfo.CurrentCulture;
            return (date.Year * 100) + culInfo.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
