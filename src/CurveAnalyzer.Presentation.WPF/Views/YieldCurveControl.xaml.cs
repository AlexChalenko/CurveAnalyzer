using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Core;
using CurveAnalyzer.Presentation.WPF.ViewModels;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace CurveAnalyzer.Presentation.WPF.Views;

public partial class YieldCurveControl : UserControl
{
    private YieldCurveViewModel? _viewModel;

    public YieldCurveControl()
    {
        InitializeComponent();
        DataContextChanged += YieldCurveControl_DataContextChanged;
    }

    private void YieldCurveControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is YieldCurveViewModel model)
        {
            _viewModel = model;
            _viewModel.BlackoutDates.CollectionChanged += BlackoutDates_CollectionChanged;
            _viewModel.ZcycDatas.CollectionChanged += ZcycDatas_CollectionChanged;
            //_viewModel.Initialize();
        }
    }

    private void ZcycDatas_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
        {
            YieldChart.Series.Clear();
        }
        else
        {
            for (int i = 0; i < e.NewItems.Count; i++)
            {
                if (e.NewItems[i] is ZcycData zcyc)
                {
                    var series = new LineSeries()
                    {
                        Title = zcyc.Date.ToShortDateString(),
                        Values = new ChartValues<ObservablePoint>(zcyc.DataRow.Select(d => new ObservablePoint(d.Period, d.Value)))
                    };
                    YieldChart.Series.Add(series);
                }
            }
        }
    }

    private void BlackoutDates_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
        {
            return;
        }

        foreach (var item in e.NewItems)
        {
            if (item is DateTime date)
            {
                App.Current.Dispatcher.BeginInvoke(() =>
                {
                    MainDatePicker.BlackoutDates.Add(new CalendarDateRange(date));
                });
            }
        }
    }
}
