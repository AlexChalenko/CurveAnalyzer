using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Presentation.WPF.ViewModels;

namespace CurveAnalyzer.Presentation.WPF.Views;

public partial class OfzActivityControl : UserControl
{
    public OfzActivityControl()
    {
        InitializeComponent();
        SetActivityContent(ActivitySection.Overview);
    }

    private void ActivityMode_Checked(object sender, RoutedEventArgs e)
    {
        if (ActivityContentHost is null)
        {
            return;
        }

        var section = DetailModeToggle?.IsChecked == true
            ? ActivitySection.Detail
            : SignalsModeToggle?.IsChecked == true
                ? ActivitySection.Signals
                : ActivitySection.Overview;

        SetActivityContent(section);
    }

    private void SetActivityContent(ActivitySection section)
    {
        if (section != ActivitySection.Overview && DataContext is OfzActivityViewModel viewModel)
        {
            viewModel.ClearOverviewTransientSelection();
        }

        ActivityContentHost.Content = section switch
        {
            ActivitySection.Signals => new OfzActivitySignalsControl(),
            ActivitySection.Detail => new OfzActivityDetailControl(),
            _ => new OfzActivityOverviewControl()
        };
    }

    private enum ActivitySection
    {
        Overview,
        Signals,
        Detail
    }
}
