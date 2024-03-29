using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;

namespace CurveAnalyzer.Interfaces
{
    public abstract class ChartBase<T> : ObservableObject
    {
        private bool isBusy;
        private IRelayCommand[] commandsToUpdate;

        public IDataManager DataManager => App.Current.Services.GetService<IDataManager>();

        private PlotModel mainChart;

        public PlotModel MainChart
        {
            get => mainChart ??= new PlotModel();
            set => SetProperty(ref mainChart, value);
        }

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
