using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Diagnostics;
using CurveAnalyzer.Presentation.WPF.ViewModels;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace CurveAnalyzer.Presentation.WPF.Views
{
    public partial class SpreadChartControl : UserControl
    {
        private SpreadChartViewModel? _spreadChartViewModel;

        public SpreadChartControl(/*SpreadChartViewModel spreadChartViewModel*/)
        {
            InitializeComponent();
            DataContextChanged += SpreadChartControl_DataContextChanged;
            //DataContext = _spreadChartViewModel = spreadChartViewModel;
            Loaded += SpreadChartControl_Loaded;
        }

        private void SpreadChartControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SpreadChartViewModel spreadChartViewModel)
            {
                _spreadChartViewModel = spreadChartViewModel;
                _spreadChartViewModel.PropertyChanged += _spreadChartViewModel_PropertyChanged;
            }
        }

        private void _spreadChartViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_spreadChartViewModel.Values))
            {
                Guard.IsNotNull(_spreadChartViewModel);

                var seriesCollection = new SeriesCollection
                {
                    new LineSeries()
                    {
                        Values = new ChartValues<DateTimePoint>(_spreadChartViewModel.Values.Select(d => new DateTimePoint(d.date, d.Value)))
                    }
                };

                SpreadChart.Series = seriesCollection;
            }
        }

        private async void SpreadChartControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_spreadChartViewModel != null)
            {
                await _spreadChartViewModel.Initialize();
            }
        }
    }
}
