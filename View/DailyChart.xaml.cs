using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            if (MainDatePicker.BlackoutDates != null && MainDatePicker.BlackoutDates.Count == 0 && e.AddedItems.Count > 0 && (DateTime)e.AddedItems[0] > DateTime.MinValue)
            {
                var dailyChartViewModel = DataContext as DailyChartViewModel;
                if (dailyChartViewModel != null)
                {
                    foreach (var newDate in dailyChartViewModel.DataManager.BlackoutDates)
                    {
                        MainDatePicker.BlackoutDates.Add(new CalendarDateRange(newDate));
                    }
                }
            }
        }
    }
}
