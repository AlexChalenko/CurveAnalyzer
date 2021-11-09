using System;
using System.Windows.Controls;
using CurveAnalyzer.ViewModel;

namespace CurveAnalyzer.View
{
    /// <summary>
    /// Interaction logic for DailyChart.xaml
    /// </summary>
    public partial class DailyChart : UserControl
    {
        public DailyChart()
        {
            InitializeComponent();
        }

        private void MainDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainDatePicker.BlackoutDates?.Count == 0 && e.AddedItems.Count > 0 && (DateTime)e.AddedItems[0] > DateTime.MinValue && DataContext is DailyChartViewModel dailyChartViewModel)
            {
                foreach (var newDate in dailyChartViewModel.Chart.DataManager.BlackoutDates)
                {
                    MainDatePicker.BlackoutDates.Add(new CalendarDateRange(newDate));
                }
            }
        }
    }
}
