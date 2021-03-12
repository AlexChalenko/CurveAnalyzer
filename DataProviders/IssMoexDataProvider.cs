using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using MoexData;

namespace CurveAnalyzer.DataProviders
{
    public class IssMoexDataProvider : ZcycDataProvider
    {
        private const string DataUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?date={0}&iss.only=yearyields&iss.meta=off";
        private const string DatesUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?iss.only=yearyields.dates&iss.meta=off";
        private const string PeriodsUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?iss.only=yearyields&yearyields.columns=period&iss.meta=off";

        public Task<ZcycData> GetDataForDate(DateTime date)
        {
            var tcs = new TaskCompletionSource<ZcycData>();

            var serializer = new XmlSerializer(typeof(IssData));
            string downloadingDate = date.ToString("yyyy-MM-dd");
            string url = string.Format(DataUrl, downloadingDate);

            var zData = new ZcycData();
            zData.Date = date;

            try
            {
                using XmlReader xmlReader = XmlReader.Create(url); //gets only 500 lines
                var issData = (IssData)serializer.Deserialize(xmlReader);
                if (issData == null || issData.data == null || issData.data.rows.Length == 0)
                    tcs.TrySetResult(zData);

                foreach (var item in issData.data.rows)
                {
                    zData.DataRow.Add(new ZcycDataRow
                    {
                        Period = item.period,
                        Value = item.value
                    });
                }

                tcs.TrySetResult(zData);
            }
            catch (Exception) // on error return empty data
            {
                tcs.TrySetResult(zData);
            }
            return tcs.Task;
        }

        public async Task<List<DateTime>> GetAvailableDates()
        {
            var tcs = new TaskCompletionSource<List<DateTime>>();

            var settings = new XmlReaderSettings
            {
                Async = true
            };

            using XmlReader reader = XmlReader.Create(DatesUrl, settings);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name.Equals("row"))
                        {
                            var startDate = DateTime.Parse(reader.GetAttribute(0));
                            var endDate = DateTime.Parse(reader.GetAttribute(1));
                            var range = Enumerable.Range(0, int.MaxValue)
                                                  .Select(index => startDate.AddDays(index))
                                                  .TakeWhile(date => date <= endDate);
                            tcs.TrySetResult(range.ToList());
                        }
                        break;
                }
            }
            return await tcs.Task.ConfigureAwait(false);
        }

        public Task<bool> SaveData(ZcycData data) => Task.FromResult(true);

        public async Task<List<double>> GetPeriods()
        {
            List<double> result = new List<double>();
            var tcs = new TaskCompletionSource<List<double>>();

            var settings = new XmlReaderSettings
            {
                Async = true
            };

            using XmlReader reader = XmlReader.Create(PeriodsUrl, settings);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name.Equals("row") && double.TryParse(reader.GetAttribute(0), out double period))
                            result.Add(period);
                        break;
                }
            }
            tcs.TrySetResult(result);
            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
