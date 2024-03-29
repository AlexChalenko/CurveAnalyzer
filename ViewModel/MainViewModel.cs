using System;
using System.Windows.Input;
using System.Windows.Threading;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace CurveAnalyzer.ViewModel
{
    internal class MainViewModel : ObservableObject
    {
        private ObservableObject currentView;
        private ICommand dailyChartSelect;
        private ICommand spreadChartSelect;
        private string status;

        private ICommand weeklyChartSelect;
        public MainViewModel()
        {
            DataManager = App.Current.Services.GetService<IDataManager>();
            DailyChartViewModel = new DailyChartViewModel();
            WeeklyChartViewModel = new WeeklyChartViewModel();
            SpreadChartViewModel = new SpreadChartViewModel();

            CurrentView = DailyChartViewModel;
        }

        public ObservableObject CurrentView
        {
            get => currentView;
            set => SetProperty(ref currentView, value);
        }

        public ICommand DailyChartSelect => dailyChartSelect ??= new RelayCommand(PerformDailyChartSelect);
        public ICommand SpreadChartSelect => spreadChartSelect ??= new RelayCommand(PerformSpreadChartSelect);
        public ICommand WeeklyChartSelect => weeklyChartSelect ??= new RelayCommand(PerformWeeklyChartSelect);
        public ChartViewModelBase<Periods> SpreadChartViewModel { get; }
        public ChartViewModelBase<DateTime> DailyChartViewModel { get; }
        public ChartViewModelBase<double> WeeklyChartViewModel { get; }
        public IDataManager DataManager { get; set; }

        public string Status
        {
            get => status;
            set => Dispatcher.CurrentDispatcher.Invoke(() => SetProperty(ref status, value));
        }

        private void PerformDailyChartSelect()
        {
            CurrentView = DailyChartViewModel;
        }

        private void PerformSpreadChartSelect()
        {
            CurrentView = SpreadChartViewModel;
        }

        private void PerformWeeklyChartSelect()
        {
            CurrentView = WeeklyChartViewModel;
        }
    }
}
