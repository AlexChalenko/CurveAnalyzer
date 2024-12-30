using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Presentation.WPF.ViewModels;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using LiveCharts;
using CommunityToolkit.Diagnostics;

namespace CurveAnalyzer.Presentation.WPF.Views
{
    public partial class SpreadChartControl : UserControl
    {
        private SpreadChartViewModel? _spreadChartViewModel;

        public SpreadChartControl()
        {
            InitializeComponent();

            DataContextChanged += SpreadChartControl_DataContextChanged;
            Loaded += SpreadChartControl_Loaded;
        }

        private void SpreadChartControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SpreadChartViewModel spreadChartViewModel)
            {
                _spreadChartViewModel = spreadChartViewModel;
                _spreadChartViewModel.PropertyChanging += _spreadChartViewModel_PropertyChanging;
            }
        }

        private void _spreadChartViewModel_PropertyChanging(object? sender, System.ComponentModel.PropertyChangingEventArgs e)
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

        private void SpreadChartControl_Loaded(object sender, RoutedEventArgs e)
        {
            _spreadChartViewModel?.Initialize();
        }
    }
}
