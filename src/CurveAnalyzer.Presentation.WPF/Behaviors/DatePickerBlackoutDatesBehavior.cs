using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace CurveAnalyzer.Presentation.WPF.Behaviors;

public static class DatePickerBlackoutDatesBehavior
{
    public static readonly DependencyProperty DatesProperty =
        DependencyProperty.RegisterAttached(
            "Dates",
            typeof(IEnumerable<DateTime>),
            typeof(DatePickerBlackoutDatesBehavior),
            new PropertyMetadata(null, OnDatesChanged));

    private static readonly DependencyProperty CollectionChangedHandlerProperty =
        DependencyProperty.RegisterAttached(
            "CollectionChangedHandler",
            typeof(NotifyCollectionChangedEventHandler),
            typeof(DatePickerBlackoutDatesBehavior),
            new PropertyMetadata(null));

    public static void SetDates(DependencyObject element, IEnumerable<DateTime>? value)
    {
        element.SetValue(DatesProperty, value);
    }

    public static IEnumerable<DateTime>? GetDates(DependencyObject element)
    {
        return (IEnumerable<DateTime>?)element.GetValue(DatesProperty);
    }

    private static void OnDatesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not DatePicker datePicker)
        {
            return;
        }

        if (e.OldValue is INotifyCollectionChanged oldCollection
            && GetCollectionChangedHandler(datePicker) is { } oldHandler)
        {
            oldCollection.CollectionChanged -= oldHandler;
        }

        NotifyCollectionChangedEventHandler? newHandler = null;
        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newHandler = (_, args) => ApplyCollectionChange(datePicker, args);
            newCollection.CollectionChanged += newHandler;
        }

        SetCollectionChangedHandler(datePicker, newHandler);
        ReloadDates(datePicker, e.NewValue as IEnumerable<DateTime>);
    }

    private static void ApplyCollectionChange(DatePicker datePicker, NotifyCollectionChangedEventArgs e)
    {
        if (!datePicker.Dispatcher.CheckAccess())
        {
            _ = datePicker.Dispatcher.BeginInvoke(() => ApplyCollectionChange(datePicker, e));
            return;
        }

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            AddDates(datePicker, e.NewItems.OfType<DateTime>());
            return;
        }

        ReloadDates(datePicker, GetDates(datePicker));
    }

    private static void ReloadDates(DatePicker datePicker, IEnumerable<DateTime>? dates)
    {
        if (!datePicker.Dispatcher.CheckAccess())
        {
            _ = datePicker.Dispatcher.BeginInvoke(() => ReloadDates(datePicker, dates));
            return;
        }

        datePicker.BlackoutDates.Clear();

        if (dates != null)
        {
            AddDates(datePicker, dates);
        }
    }

    private static void AddDates(DatePicker datePicker, IEnumerable<DateTime> dates)
    {
        foreach (var date in dates)
        {
            datePicker.BlackoutDates.Add(new CalendarDateRange(date));
        }
    }

    private static void SetCollectionChangedHandler(
        DependencyObject element,
        NotifyCollectionChangedEventHandler? value)
    {
        element.SetValue(CollectionChangedHandlerProperty, value);
    }

    private static NotifyCollectionChangedEventHandler? GetCollectionChangedHandler(DependencyObject element)
    {
        return (NotifyCollectionChangedEventHandler?)element.GetValue(CollectionChangedHandlerProperty);
    }
}
