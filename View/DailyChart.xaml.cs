using System;
using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Interfaces;
using CurveAnalyzer.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer.View
{
    /// <summary>
    /// Interaction logic for DailyChart.xaml
    /// </summary>
    public partial class DailyChart : UserControl
    {
        private readonly IDataManager _dataManager;

        public DailyChart()
        {
            _dataManager = App.Current.Services.GetRequiredService<IDataManager>();
            InitializeComponent();

            DataContextChanged += DailyChart_DataContextChanged;
        }

        private void DailyChart_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DailyChartViewModel dailyChartViewModel)
            {
                dailyChartViewModel.BlackoutDates.CollectionChanged += BlackoutDates_CollectionChanged;
                foreach (var date in dailyChartViewModel.BlackoutDates)
                {
                    MainDatePicker.BlackoutDates.Add(new CalendarDateRange(date));
                }
            }
        }

        private void BlackoutDates_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is DateTime newDate)
                    {
                        App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MainDatePicker.BlackoutDates.Add(new CalendarDateRange(newDate));
                        });
                    }
                }
            }
        }
    }
}
