using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CurveAnalyzer.DataProviders;
using CurveAnalyzer.Interfaces;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using MoexData;

namespace CurveAnalyzer.Data
{
    public class DataManager : ObservableObject, IDataManager
    {
        private readonly IHistoryDataProvider historyDataProvider;
        private readonly IDataProvider onlineDataProvider;
        private DateTime startDate;
        private DateTime endDate;
        private DateTime selectedDate;
        private string status;
        private bool isBusy;

        private readonly CancellationToken token = CancellationToken.None;

        private int progress;
        private TimeSpan loadingTimeLeft;


        public int Progress
        {
            get { return progress; }
            set
            {
                if (SetProperty(ref progress, value))
                {
                    ProgressBarVisibility = progress < 100
                        ? Visibility.Visible
                        : Visibility.Hidden;
                    OnPropertyChanged(nameof(ProgressBarVisibility));
                }
            }
        }

        public TimeSpan LoadingTimeLeft { get => loadingTimeLeft; set => SetProperty(ref loadingTimeLeft, value); }

        private readonly Progress<int> loadingProgress = new();

        public event EventHandler OnDataLoaded;

        public Visibility ProgressBarVisibility { get; set; } = Visibility.Hidden;

        public async Task<List<Zcyc>> GetAllDataForPeriod(double period)
        {
            var history = await historyDataProvider.GetDataForPeriod(period).ConfigureAwait(false);
            var realtime = await onlineDataProvider.GetDataForPeriod(period).ConfigureAwait(false);

            if (history != null && realtime != null)
            {
                var res = history.Union(realtime).ToList();
                return await Task.FromResult(res).ConfigureAwait(false);
            }
            return await Task.FromResult<List<Zcyc>>(null).ConfigureAwait(false);
        }

        public Collection<double> Periods { get; } = new ObservableCollection<double>();

        public Collection<DateTime> BlackoutDates { get; } = new ObservableCollection<DateTime>();

        public DateTime StartDate
        {
            get { return startDate; }
            set { SetProperty(ref startDate, value); }
        }

        public DateTime EndDate
        {
            get => endDate;
            set => SetProperty(ref endDate, value);
        }

        public DateTime SelectedDate
        {
            get { return selectedDate; }
            set { SetProperty(ref selectedDate, value); }
        }

        public string Status
        {
            get => status;
            set => Application.Current.Dispatcher.Invoke(() => SetProperty(ref status, value));
        }

        public bool IsBusy
        {
            get => isBusy;
            set => SetProperty(ref isBusy, value);
        }

        public (DateTime startDate, DateTime endDate) GetAvailableDates()
        {
            return (StartDate, EndDate);
        }

        public DataManager()
        {
            historyDataProvider = new HistoryDataProvider();
            onlineDataProvider = new OnlineDataProvider();
            loadingProgress.ProgressChanged += LoadingProgressChanged;

            updateHistory();
            updatePeriods();
        }


        private void LoadingProgressChanged(object sender, int e)
        {
            Progress = e;
        }

        private void updateHistory()
        {
            IsBusy = true;
            _ = onlineDataProvider.GetAvailableDates(token).ContinueWith(async t =>
              {
                  if (t.IsCompletedSuccessfully)
                  {
                      var realtimeDates = t.Result;
                      bool todayDeleted = realtimeDates.Remove(DateTime.Today);
                      var historyDates = await historyDataProvider.GetAvailableDates(token).ConfigureAwait(false);
                      var newDates = historyDates?.Count > 0 ? realtimeDates.Except(historyDates).Where(date => date > historyDates.Max()).ToList() : realtimeDates;
                      StartDate = historyDates?.Count > 0 ? historyDates.Min() : realtimeDates.Min();
                      EndDate = todayDeleted ? DateTime.Today : realtimeDates.Max();
                     // SelectedDate = EndDate;
                      await downloadAndSaveData(newDates, loadingProgress).ConfigureAwait(false);
                      BlackoutDates.Clear();
                      _ = getBlackoutDates(realtimeDates).ContinueWith(dates =>
                        {
                            if (dates.IsCompletedSuccessfully)
                            {
                                foreach (var date in dates.Result)
                                    BlackoutDates.Add(date);
                            }
                            else
                            {
                                Status = dates.Exception == null ? "Error loading Blackout dates..." : $"Error loading Blackout dates: {dates.Exception}";
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                //it will update BlackoutDays in DailyChart.xaml
                                SelectedDate = StartDate;
                                SelectedDate = EndDate;
                                OnDataLoaded?.Invoke(this, EventArgs.Empty);
                            });
                        });
                  }
                  else
                  {
                      Status = t.Exception == null ? "Error loading available dates ..." : $"Error loading available dates {t.Exception}";
                  }
                  IsBusy = false;
              });
        }

        private Task<List<DateTime>> getBlackoutDates(List<DateTime> realtimeDates)
        {
            var tcs = new TaskCompletionSource<List<DateTime>>();

            historyDataProvider.GetAvailableDates(token).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var historyDates = task.Result;

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

        private async Task downloadAndSaveData(IEnumerable<DateTime> dates, IProgress<int> progress)
        {
            var allDates = dates.ToList();
            var startLoading = DateTime.Now;

            foreach (var item in from date in dates select onlineDataProvider.GetDataForDate(date))
            {
                Task<ZcycData> firstFinishedTask = await Task.WhenAny(item).ConfigureAwait(false);
                var res = await firstFinishedTask.ConfigureAwait(false);
                if (res?.Date != null && res.DataRow.Count > 0)
                {
                    Status = $"Загрузка данных за {res.Date}";
                    var result = await historyDataProvider.SaveData(res).ConfigureAwait(false);
                    var loadedQty = allDates.IndexOf(res.Date) + 1;
                    var tempQty = (DateTime.Now - startLoading) / loadedQty;
                    LoadingTimeLeft = (allDates.Count - loadedQty) * tempQty;
                    progress?.Report((int)((float)loadedQty / allDates.Count * 100));
                }
            }
            progress?.Report(100);
            Status = "Загрузка данных завершена";
        }

        private void updatePeriods()
        {
            onlineDataProvider.GetPeriods().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    t.Result.ForEach(d => Periods.Add(d));
                }
            }).ConfigureAwait(false);
        }

        public Task<ZcycData> GetData(DateTime value)
        {
            var tcs = new TaskCompletionSource<ZcycData>();

            historyDataProvider.GetDataForDate(value).ContinueWith(async data =>
            {
                if (data.IsCompletedSuccessfully)
                {
                    ZcycData dataToPlot = data.Result;

                    if (dataToPlot.DataRow.Count == 0)
                        dataToPlot = await onlineDataProvider.GetDataForDate(value).ConfigureAwait(false);

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
    }
}
