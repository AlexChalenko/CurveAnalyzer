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

        public ObservableCollection<double> Periods { get; private set; }

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
            updateHistory();
            Task.Run(() => updatePeriods());
            //Task.Run(async () =>todayZcycData = await onlineDataProvider.GetDataForDate(DateTime.Today));
        }

        private void updateHistory()
        {
            onlineDataProvider.GetAvailableDates().ContinueWith(async t =>
            {
                var realtimeDates = t.Result;
                bool todayDeleted = realtimeDates.Remove(DateTime.Today);
                var historyDates = await historyDataProvider.GetAvailableDates().ConfigureAwait(false);
                var newDates = historyDates.Count > 0 ? realtimeDates.Except(historyDates).Where(date => date > historyDates.Max()).ToList() : realtimeDates;
                StartDate = historyDates.Count > 0 ? historyDates.Min() : realtimeDates.Min();
                EndDate = todayDeleted ? DateTime.Today : realtimeDates.Max();
                SelectedDate = EndDate;
                await downloadAndSaveData(newDates);
            });
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
