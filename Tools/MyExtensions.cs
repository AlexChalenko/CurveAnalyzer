using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurveAnalyzer.Tools
{
    public static class MyExtensions
    {
        public static int YearAndWeekToNumber(this DateTime date)
        {
            CultureInfo ciCurr = CultureInfo.CurrentCulture;
            return date.Year * 100 + ciCurr.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
