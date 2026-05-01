using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Presentation.WPF.ViewModels;

namespace CurveAnalyzer.Presentation.WPF.Views
{
    /// <summary>
    /// Interaction logic for RateChartControl.xaml
    /// </summary>
    public partial class RateChartControl : UserControl
    {
        public RateChartControl()
        {
            InitializeComponent();
            Loaded += RateChartControl_Loaded;
        }

        private async void RateChartControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RateChartViewModel viewModel)
            {
                await viewModel.Initialize();
            }
        }
    }
}
