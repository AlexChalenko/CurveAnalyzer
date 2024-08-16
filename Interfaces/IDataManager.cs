using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CurveAnalyzer.Data;
using MoexData.Data;

namespace CurveAnalyzer.Interfaces
{
    public interface IDataManager
    {
        Collection<DateTime> BlackoutDates { get; }

        //DateRange DateRange { get; }
        //DateTime SelectedDate { get; set; }
        //bool IsBusy { get; set; }
        //TimeSpan LoadingTimeLeft { get; set; }
        //ICollection<double> Periods { get; }
        //int Progress { get; set; }
        //Visibility ProgressBarVisibility { get; set; }
        //string Status { get; set; }

        Task UpdateDataASync(Progress<double> progress);
        Task<IEnumerable<double>> GetPeriodsAsync();
        Task<IEnumerable<Zcyc>> GetZcycForPeriodAsync(double period);
        Task<DateRange> GetAvailableDatesAsync();
        Task<ZcycData> GetDataAsync(DateTime value);

    }
}
