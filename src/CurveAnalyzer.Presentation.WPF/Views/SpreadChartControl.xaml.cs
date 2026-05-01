using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Presentation.WPF.ViewModels;

namespace CurveAnalyzer.Presentation.WPF.Views
{
    public partial class SpreadChartControl : UserControl
    {
        public SpreadChartControl()
        {
            InitializeComponent();
            Loaded += SpreadChartControl_Loaded;
        }

        private async void SpreadChartControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SpreadChartViewModel viewModel)
            {
                await viewModel.Initialize();
            }
        }
    }
}
