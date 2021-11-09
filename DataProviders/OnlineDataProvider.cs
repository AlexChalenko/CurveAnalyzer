using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using MoexData;

namespace CurveAnalyzer.DataProviders
{
    public class OnlineDataProvider : IDataProvider
    {
        private const string DataUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?date={0}&iss.only=yearyields&iss.meta=off";
        private const string DatesUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?iss.only=yearyields.dates&iss.meta=off";
        private const string PeriodsUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?iss.only=yearyields&yearyields.columns=period&iss.meta=off";

        private readonly CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

        private ZcycData todayZcycData;

        private readonly TimeSpan cachePeriod = TimeSpan.FromMinutes(5);
        private DateTime lastUpdateTime = DateTime.MinValue;

        public Task<List<DateTime>> GetAvailableDates(CancellationToken token)
        {
            TaskCompletionSource<List<DateTime>> tcs = new();

            var settings = new XmlReaderSettings
            {
                Async = true
            };

            if (token.IsCancellationRequested)
            {
                tcs.SetResult(new List<DateTime>());
            }
            else
            {
                using XmlReader reader = XmlReader.Create(DatesUrl, settings);
                try
                {
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (reader.Name.Equals("row", StringComparison.Ordinal))
                                {
                                    if (DateTime.TryParse(reader.GetAttribute(0), culture, DateTimeStyles.None, out var startDate)
                                        && DateTime.TryParse(reader.GetAttribute(1), culture, DateTimeStyles.None, out var endDate))
                                    {
                                        var range = Enumerable.Range(0, int.MaxValue)
                                            .Select(index => startDate.AddDays(index))
                                            .TakeWhile(date => date <= endDate);
                                        tcs.SetResult(range.ToList());
                                    }
                                    else
                                    {
                                        tcs.SetException(new Exception("Invalid dates"));
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            return tcs.Task;
        }

        public Task<ZcycData> GetDataForDate(DateTime date)
        {
            if (date.Equals(DateTime.Today) && DateTime.Now.Subtract(lastUpdateTime) < cachePeriod && todayZcycData != null)
            {
                return Task.FromResult(todayZcycData);
            }

            var tcs = new TaskCompletionSource<ZcycData>();
            var serializer = new XmlSerializer(typeof(IssData));
            string downloadingDate = date.ToString("yyyy-MM-dd");
            string url = string.Format(culture, DataUrl, downloadingDate);

            var zData = new ZcycData
            {
                Date = date
            };

            try
            {
                using XmlReader xmlReader = XmlReader.Create(url); //gets only 500 lines
                var issData = (IssData)serializer.Deserialize(xmlReader);
                if (issData == null || issData.data == null || issData.data.rows.Length == 0)
                {
                    tcs.SetResult(zData);
                }
                else
                {
                    foreach (var item in issData.data.rows)
                    {
                        zData.DataRow.Add(new ZcycDataRow
                        {
                            Period = item.period,
                            Value = item.value
                        });
                    }
                    if (date.Equals(DateTime.Today))
                    {
                        todayZcycData = zData;
                        lastUpdateTime = DateTime.Now;
                    }
                    tcs.SetResult(zData);
                }
            }
            catch (Exception) // on error return empty data
            {
                tcs.SetResult(zData);
            }
            return tcs.Task;
        }

        public Task<List<Zcyc>> GetDataForPeriod(double period)
        {
            var data = GetDataForDate(DateTime.Today);
            if (data.IsCompletedSuccessfully)
            {
                var dataForPeriod = data.Result.DataRow.Where(r => r.Period == period).ToList().FirstOrDefault();
                if (dataForPeriod != null)
                {
                    Zcyc zcycData = new()
                    {
                        Tradedate = DateTime.Today,
                        Period = dataForPeriod.Period,
                        Value = dataForPeriod.Value
                    };
                    return Task.FromResult(new List<Zcyc>() { zcycData });
                }
            }
            return Task.FromResult(new List<Zcyc>());
        }

        public async Task<List<double>> GetPeriods()
        {
            List<double> result = new();

            XmlReaderSettings settings = new()
            {
                Async = true
            };

            CultureInfo culture = new("en-US");

            using XmlReader reader = XmlReader.Create(PeriodsUrl, settings);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name.Equals("row") && double.TryParse(reader.GetAttribute(0), NumberStyles.Float, culture, out double period))
                            result.Add(period);
                        break;
                }
            }
            return result;
        }
    }
}
