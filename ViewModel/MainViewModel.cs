using System.Windows.Input;
using System.Windows.Threading;
using CurveAnalyzer.Data;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace CurveAnalyzer.ViewModel
{
    internal class MainViewModel : ObservableObject
    {
        private string status;

        public DataManager DataManager { get; set; }
        public DailyChartViewModel DailyChartViewModel { get; }

        public WeeklyChartViewModel WeeklyChartViewModel { get; }

        public SpreadChartViewModel SpreadChartViewModel { get; }

        private ObservableObject currentView;

        public ObservableObject CurrentView
        {
            get => currentView;
            set => SetProperty(ref currentView, value);
        }

        public string Status
        {
            get => status;
            set
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    SetProperty(ref status, value);
                });
            }
        }

        public MainViewModel()
        {
            DataManager = new DataManager();
            DailyChartViewModel = new DailyChartViewModel(DataManager);
            WeeklyChartViewModel = new WeeklyChartViewModel(DataManager);
            SpreadChartViewModel = new SpreadChartViewModel(DataManager);

            CurrentView = DailyChartViewModel;

            DataManager.Initialize();
        }

        private ICommand weeklyChartSelect;
        public ICommand WeeklyChartSelect => weeklyChartSelect ??= new RelayCommand(PerformWeeklyChartSelect);

        private void PerformWeeklyChartSelect()
        {
            CurrentView = WeeklyChartViewModel;
        }

        private ICommand spreadChartSelect;
        public ICommand SpreadChartSelect => spreadChartSelect ??= new RelayCommand(PerformSpreadChartSelect);

        private void PerformSpreadChartSelect()
        {
            CurrentView = SpreadChartViewModel;
        }

        private ICommand dailyChartSelect;
        public ICommand DailyChartSelect => dailyChartSelect ??= new RelayCommand(PerformDailyChartSelect);

        private void PerformDailyChartSelect()
        {
            CurrentView = DailyChartViewModel;
        }
    }
}
