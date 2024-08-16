using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CurveAnalyzer.DataProviders;
using CurveAnalyzer.Interfaces;
using MoexData.Data;

namespace CurveAnalyzer.Data
{
    public class DataManager : IDataManager
    {
        private readonly IHistoryDataProvider _historyDataProvider;
        private readonly IDataProvider _onlineDataProvider;

        private DateTime _startDate;
        private DateTime _endDate;
        private DateTime _selectedDate;
        private string _status;

        private readonly CancellationToken token = CancellationToken.None;

        //private int progress;
        //private TimeSpan loadingTimeLeft;


        //public int Progress
        //{
        //    get { return progress; }
        //    set
        //    {
        //        if (SetProperty(ref progress, value))
        //        {
        //            ProgressBarVisibility = progress < 100
        //                ? Visibility.Visible
        //                : Visibility.Hidden;
        //            OnPropertyChanged(nameof(ProgressBarVisibility));
        //        }
        //    }
        //}

        //public TimeSpan LoadingTimeLeft { get => loadingTimeLeft; set => SetProperty(ref loadingTimeLeft, value); }


        public event EventHandler OnDataLoaded;

        public Visibility ProgressBarVisibility { get; set; } = Visibility.Hidden;

        public async Task<IEnumerable<Zcyc>> GetZcycForPeriodAsync(double period)
        {
            var historyTask = _historyDataProvider.GetDataForPeriod(period);
            var realtimeTask = _onlineDataProvider.GetDataForPeriod(period);

            await Task.WhenAll(historyTask, realtimeTask);

            if (historyTask.IsCompletedSuccessfully && realtimeTask.IsCompletedSuccessfully)
            {
                var res = historyTask.Result.Union(realtimeTask.Result).OrderBy(z => z.Tradedate);
                return res;
            }
            return [];
        }

        private Collection<double> Periods { get; } = [];

        public Collection<DateTime> BlackoutDates { get; private set; } = [];

        //public DateTime StartDate
        //{
        //    get { return _startDate; }
        //    set { SetProperty(ref _startDate, value); }
        //}

        //public DateTime EndDate
        //{
        //    get => _endDate;
        //    set => SetProperty(ref _endDate, value);
        //}

        //public DateTime SelectedDate
        //{
        //    get { return _selectedDate; }
        //    set { SetProperty(ref _selectedDate, value); }
        //}

        //public string Status
        //{
        //    get => _status;
        //    set => Application.Current.Dispatcher.Invoke(() => SetProperty(ref _status, value));
        //}

        //public (DateTime startDate, DateTime endDate) GetAvailableDates()
        //{
        //    return (StartDate, EndDate);
        //}

        public DataManager(IDataProvider onlineDataProvider, IHistoryDataProvider historyDataProvider)
        {
            _historyDataProvider = historyDataProvider;
            _onlineDataProvider = onlineDataProvider;

            onlineDataProvider = new OnlineDataProvider();
            //loadingProgress.ProgressChanged += LoadingProgressChanged;
        }

        public async Task UpdateDataASync(Progress<double> progress)
        {
            await UpdatePeriods();
            await LoadHistory(progress);
            //await GetBlackoutDates(dateRange);
        }

        private async Task LoadHistory(IProgress<double> progress)
        {
            var realtimeDates = await _onlineDataProvider.GetAvailableDates(token);
            bool todayDeleted = realtimeDates.ToList().Remove(DateTime.Today);
            var historyDates = await _historyDataProvider.GetAvailableDates(token);
            var newDates = historyDates.ToList().Count > 0 ?
                [.. realtimeDates.Except(historyDates).Where(date => date > historyDates.Max()).Order()] :
                realtimeDates;
            await DownloadAndSaveData(newDates, progress).ConfigureAwait(false);

            var blackoutDates = await GetBlackoutDates(realtimeDates.ToList());
            BlackoutDates = new Collection<DateTime>(blackoutDates);
        }

        private Task<List<DateTime>> GetBlackoutDates(List<DateTime> realtimeDates)
        {
            var tcs = new TaskCompletionSource<List<DateTime>>();

            _historyDataProvider.GetAvailableDates(token).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var historyDates = task.Result.ToList();

                    var today = DateTime.Today;
                    if (realtimeDates.Contains(today) && !historyDates.Contains(today))
                        historyDates.Add(today);

                    var result = realtimeDates.Except(historyDates).ToList();
                    tcs.TrySetResult(result);
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception);
                }
            }).ConfigureAwait(false);

            return tcs.Task;
        }

        private async Task DownloadAndSaveData(IEnumerable<DateTime> dates, IProgress<double> progress)
        {
            var allDates = dates.ToList();
            var startLoading = DateTime.Now;

            if (allDates.Count == 0)
            {
                progress.Report(1.0);
                return;
            }

            var inc = 1.0 / allDates.Count;
            var total = 0.0;

            //Parallel.ForEach(dates, async d =>
            //{
            //    var res = await _onlineDataProvider.GetDataForDate(d);
            //    Interlocked.Add
            //    total += inc;
            //    progress?.Report(total);
            //});

            foreach (var item in from date in dates select _onlineDataProvider.GetDataForDate(date))
            {
                //Task<ZcycData> firstFinishedTask = await item;
                var res = await item;
                if (res?.Date != null && res.DataRow.Count > 0)
                {
                    await _historyDataProvider.SaveData(res);
                }
                total += inc;
                progress?.Report(total);
            }
            //progress?.Report(total);
        }

        private async Task UpdatePeriods()
        {
            Periods.Clear();

            foreach (var period in await _onlineDataProvider.GetPeriods())
            {
                Periods.Add(period);
            }
        }

        public Task<ZcycData> GetDataAsync(DateTime value)
        {
            var tcs = new TaskCompletionSource<ZcycData>();

            _historyDataProvider.GetDataForDate(value).ContinueWith(async data =>
            {
                if (data.IsCompletedSuccessfully)
                {
                    ZcycData dataToPlot = data.Result;

                    if (dataToPlot.DataRow.Count == 0)
                        dataToPlot = await _onlineDataProvider.GetDataForDate(value);

                    tcs.SetResult(dataToPlot);
                }
                else if (data.Exception != null)
                {
                    tcs.SetException(data.Exception);
                }
                else
                {
                    tcs.SetException(new Exception("Task was canceled"));
                }
            });
            return tcs.Task;
        }

        public Task<IEnumerable<double>> GetPeriodsAsync()
        {
            return _historyDataProvider.GetPeriods();
        }

        public async Task<DateRange> GetAvailableDatesAsync()
        {
            var realtimeDates = await _onlineDataProvider.GetAvailableDates(token);
            var historyDates = await _historyDataProvider.GetAvailableDates(token);
            var allDates = realtimeDates.Union(historyDates).Distinct().ToList();
            return new DateRange(allDates.Min(), allDates.Max());
        }
    }
}
