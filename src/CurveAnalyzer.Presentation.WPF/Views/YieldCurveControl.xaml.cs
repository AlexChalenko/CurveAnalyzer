using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Presentation.WPF.ViewModels;
using LiveChartsCore.SkiaSharpView;

namespace CurveAnalyzer.Presentation.WPF.Views;

public partial class YieldCurveControl : UserControl
{
    public YieldCurveControl()
    {
        InitializeComponent();
        DataContextChanged += YieldCurveControl_DataContextChanged;
    }

    private void YieldCurveControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not YieldCurveViewModel viewModel)
        {
            return;
        }

        YieldChart.YAxes =
        [
            new Axis
            {
                Name = "Доходность",
                Labeler = viewModel.Formatter
            }
        ];
    }
}
