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
            DataContextChanged += DailyChart_DataContextChanged;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            DataContextChanged -= DailyChart_DataContextChanged;
            Dispatcher.ShutdownStarted -= Dispatcher_ShutdownStarted;
        }

        private void DailyChart_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DailyChartViewModel dailyChartViewModel)
            {
                dailyChartViewModel.DataManager.BlackoutDates.CollectionChanged += BlackoutDates_CollectionChanged;
            }
        }

        private void BlackoutDates_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is null)
                return;

            Dispatcher.Invoke(() =>
             {
                 foreach (var newItem in e.NewItems)
                 {
                     if (newItem is DateTime newDate)
                         MainDatePicker.BlackoutDates.Add(new System.Windows.Controls.CalendarDateRange(newDate));
                 }
             });
        }
    }
}
