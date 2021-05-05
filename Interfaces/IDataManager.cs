using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using MoexData;

namespace CurveAnalyzer.Interfaces
{
    public interface IDataManager
    {
        ObservableCollection<DateTime> BlackoutDates { get; }
        DateTime EndDate { get; set; }
        bool IsBusy { get; set; }
        TimeSpan LoadingTimeLeft { get; set; }
        ObservableCollection<double> Periods { get; }
        int Progress { get; set; }
        Visibility ProgressBarVisibility { get; set; }
        DateTime SelectedDate { get; set; }
        DateTime StartDate { get; set; }
        string Status { get; set; }
        Task<List<Zcyc>> GetAllDataForPeriod(double period);
        (DateTime startDate, DateTime endDate) GetAvailableDates();
    }
}
