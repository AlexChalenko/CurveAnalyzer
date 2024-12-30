using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;

namespace CurveAnalyzer.Interfaces
{
    public abstract partial class ChartBase<T> : ObservableObject
    {
        private bool isBusy;
        private IRelayCommand[] commandsToUpdate;

        protected IDataManager _dataManager;

        protected ChartBase(IDataManager DataManager)
        {
            _dataManager = DataManager;
            PlotCommand = new AsyncRelayCommand<T>(Plot);
        }

        [ObservableProperty]
        private PlotModel mainChart = new();

        public IAsyncRelayCommand PlotCommand { get; }

        public virtual void Setup(IRelayCommand[] commandsToUpdate)
        {
            this.commandsToUpdate = commandsToUpdate;
            IsBusy = false;
        }

        public abstract Task Plot(T value);

        public virtual bool Validate(T value)
        {
            return true;
        }

        public virtual void Clear()
        {
            MainChart.Series.Clear();
            MainChart.InvalidatePlot(true);
        }

        public bool IsBusy
        {
            set
            {
                if (SetProperty(ref isBusy, value))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (commandsToUpdate.Length > 0)
                            foreach (var item in commandsToUpdate)
                                item.NotifyCanExecuteChanged();
                    });
                }
            }
            get => isBusy;
        }
    }
}
