using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CurveAnalyzer.DataProviders;
using CurveAnalyzer.Interfaces;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using MoexData;

namespace CurveAnalyzer.Data
{
    public class DataManager : ObservableObject
    {
        private IDataProvider historyDataProvider;
        private IDataProvider onlineDataProvider;

        private DateTime startDate;
        private DateTime endDate;
        private DateTime selectedDate;
        private string status;
        private bool isBusy;

        public async Task<List<Zcyc>> GetAllDataForPeriod(double period)
        {
            var history = await historyDataProvider.GetDataForPeriod(period);
            var realtime = await onlineDataProvider.GetDataForPeriod(period);

            if (history != null && realtime != null)
            {
                var res = history.Union(realtime).ToList();
                return await Task.FromResult(res);
            }
            return await Task.FromResult<List<Zcyc>>(null);
        }

        public ObservableCollection<double> Periods { get; }

        public ObservableCollection<DateTime> BlackoutDates { get; }

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
            set
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetProperty(ref status, value);
                });
            }
        }

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                SetProperty(ref isBusy, value);
            }
        }

        internal async Task<ZcycData> GetData(DateTime value)
        {
            ZcycData dataToPlot = await historyDataProvider.GetDataForDate(value);

            if (dataToPlot.DataRow.Count == 0)
                dataToPlot = await onlineDataProvider.GetDataForDate(value);

            return dataToPlot;
        }

        public (DateTime startDate, DateTime endDate) GetAvailableDates()
        {
            return (StartDate, EndDate);
        }

        public DataManager()
        {
            historyDataProvider = new HistoryDataProvider();
            onlineDataProvider = new OnlineDataProvider();
            Periods = new ObservableCollection<double>();
            BlackoutDates = new();
        }

        public void Initialize()
        {
            updateHistory();
            Task.Run(() => updatePeriods());
        }

        private void updateHistory()
        {
            IsBusy = true;
            onlineDataProvider.GetAvailableDates().ContinueWith(async t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    var realtimeDates = t.Result;
                    bool todayDeleted = realtimeDates.Remove(DateTime.Today);
                    var historyDates = await historyDataProvider.GetAvailableDates().ConfigureAwait(false);
                    var newDates = historyDates.Count > 0 ? realtimeDates.Except(historyDates).Where(date => date > historyDates.Max()).ToList() : realtimeDates;
                    StartDate = historyDates.Count > 0 ? historyDates.Min() : realtimeDates.Min();
                    EndDate = todayDeleted ? DateTime.Today : realtimeDates.Max();
                    SelectedDate = EndDate;
                    await downloadAndSaveData(newDates);
                    BlackoutDates.Clear();
                    foreach (var date in await getBlackoutDates(realtimeDates))
                        BlackoutDates.Add(date);
                }
                IsBusy = false;
            });
        }

        async Task<List<DateTime>> getBlackoutDates(List<DateTime> realtimeDates)
        {
            var historyDates = await historyDataProvider.GetAvailableDates();
            var today = DateTime.Today;
            if (realtimeDates.Contains(today) && !historyDates.Contains(today))
                historyDates.Add(today);

            return await Task.FromResult(realtimeDates.Except(historyDates).ToList());
        }

        private async Task downloadAndSaveData(IEnumerable<DateTime> dates)
        {
            foreach (var item in from date in dates select onlineDataProvider.GetDataForDate(date))
            {
                Task<ZcycData> firstFinishedTask = await Task.WhenAny(item);
                var res = await firstFinishedTask;
                if (res?.Date != null && res.DataRow.Count > 0)
                {
                    Status = $"Загрузка данных за {res.Date}";
                    var result = await historyDataProvider.SaveData(res);
                    Debug.WriteLine($"******************** added {result} with date {res.Date}");
                }
            }
            Status = "Загрузка данных завершена";
        }

        private Task updatePeriods()
        {
            return onlineDataProvider.GetPeriods().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    foreach (var item in t.Result)
                        Periods.Add(item);
                }
            });
        }
    }
}
