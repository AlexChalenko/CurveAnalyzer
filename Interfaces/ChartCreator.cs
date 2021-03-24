using System.Windows;
using CurveAnalyzer.Data;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;

namespace CurveAnalyzer.Interfaces
{
    public abstract class ChartCreator<T> : ObservableObject
    {
        private bool isBusy;
        IRelayCommand[] updateCommands;

        public ChartCreator()
        {
            MainChart = new PlotModel();
        }

        private PlotModel mainChart;

        public PlotModel MainChart
        {
            get { return mainChart; }
            set { SetProperty(ref mainChart, value); }
        }

        public virtual void Setup(IRelayCommand[] commandsToUpdate)
        {
            this.updateCommands = commandsToUpdate;
            IsBusy = false;
        }

        public abstract void Plot(DataManager dataManager, T value);

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
                if (value != isBusy)
                {
                    isBusy = value;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (updateCommands.Length > 0)
                            foreach (var item in updateCommands)
                                item.NotifyCanExecuteChanged();
                    });
                }
            }
            get => isBusy;
        }
    }
}
