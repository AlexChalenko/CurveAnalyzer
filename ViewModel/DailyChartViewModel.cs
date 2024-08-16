using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.ViewModel
{
    public partial class DailyChartViewModel : ChartViewModelBase<DateTime>
    {
        [ObservableProperty]
        private ObservableCollection<DateTime> _blackoutDates = [];

        [ObservableProperty]
        private DateTime _startDate;

        [ObservableProperty]
        private DateTime _endDate;

        private IDataManager _dataManager;

        public string ButtonName { get; } = "Получить график";

        public DailyChartViewModel(DailyCurveChart dailyCurveChart, IDataManager dataManager) : base(dailyCurveChart, false)
        {
            _dataManager = dataManager;
        }

        [RelayCommand]
        private void PlotPreviosDay()
        {
            var date = Parameter;

            do
            {
                date = date.AddDays(-1);
            } while (_dataManager.BlackoutDates.Contains(date));

            Parameter = date;
            Chart.Clear();
            PlotChartCommand.Execute(date);
        }

        public override async Task Setup()
        {
            try
            {
                var allDates = await _dataManager.GetAvailableDatesAsync();
                StartDate = allDates.Start;
                EndDate = allDates.End;

                BlackoutDates.Clear();
                foreach (var date in _dataManager.BlackoutDates)
                {
                    BlackoutDates.Add(date);
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    Parameter = allDates.End;
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
