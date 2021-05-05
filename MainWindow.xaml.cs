using System;
using System.Windows;
using CurveAnalyzer.ViewModel;

namespace CurveAnalyzer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //DataContext = mainViewModel = new MainViewModel();
            InitializeComponent();

            ////var mainViewModel = (MainViewModel)DataContext;
            //mainViewModel.DataManager.BlackoutDates.CollectionChanged += BlackoutDates_CollectionChanged;

        }

        ////private void BlackoutDates_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        ////{
        ////    if (e.NewItems != null)
        ////    {
        ////        Application.Current.Dispatcher.Invoke(() =>
        ////        {
        ////            foreach (var newItem in e.NewItems)
        ////            {
        ////                if (newItem is DateTime newDate)
        ////                    MainDatePicker.BlackoutDates.Add(new System.Windows.Controls.CalendarDateRange(newDate));
        ////            }
        ////        });
        ////    }

        ////    //if (e.OldItems != null)
        ////    //{
        ////    //    foreach (var oldtem in e.OldItems)
        ////    //    {
        ////    //        if (oldtem is DateTime && MainDatePicker.BlackoutDates.IndexOf((DateTime)oldtem)>0)
        ////    //        {
        ////    //            MainDatePicker.BlackoutDates.Remove((new System.Windows.Controls.CalendarDateRange((DateTime)oldtem));
        ////    //        }
        ////    //    }
        ////    //}
        ////}
    }
}
