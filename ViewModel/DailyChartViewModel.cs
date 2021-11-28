using System;
using System.Windows.Input;
using CurveAnalyzer.Charts;
using Microsoft.Toolkit.Mvvm.Input;

namespace CurveAnalyzer.ViewModel
{
    public class DailyChartViewModel : ChartViewModelBase<DateTime>
    {
        public DailyChartViewModel() : base(new DailyCurveChart())
        {
            Chart.DataManager.OnDataLoaded += (s, e) =>
            {
                Parameter = Chart.DataManager.EndDate;
            };
        }

        public override DateTime Parameter
        {
            get => base.Parameter;
            set
            {
                if (base.Parameter != value)
                {
                    Chart.DataManager.SelectedDate = Parameter;
                }
                base.Parameter = value;
            }
        }

        public override void PlotChart(DateTime parameter)
        {
            Chart.Plot(parameter);
        }

        public string ButtonName { get; } = "Получить данные";

        private RelayCommand plotPreviosDayCommand;
        public ICommand PlotPreviosDayCommand => plotPreviosDayCommand ??= new RelayCommand(PlotPreviosDay);

        private void PlotPreviosDay()
        {
            var date = Parameter;

            do
            {
                date = date.AddDays(-1);
            } while (Chart.DataManager.BlackoutDates.Contains(date));

            Parameter = date;
            Chart.Clear();
            PlotChart(date);
        }
    }
}
