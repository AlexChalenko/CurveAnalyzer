using System.Windows;
using System.Windows.Controls;

namespace CurveAnalyzer.Presentation.WPF.Views;

public partial class OfzActivityDetailControl : UserControl
{
    public OfzActivityDetailControl()
    {
        InitializeComponent();
        SetDetailContent(showIssue: true);
    }

    private void DetailMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DetailContentHost is null)
        {
            return;
        }

        SetDetailContent(IssueDetailToggle?.IsChecked == true);
    }

    private void SetDetailContent(bool showIssue)
    {
        DetailContentHost.Content = showIssue
            ? new OfzActivityIssueDetailControl()
            : new OfzActivitySegmentsDetailControl();
    }
}
