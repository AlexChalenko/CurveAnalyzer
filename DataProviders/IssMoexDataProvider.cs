using System;
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
        private const string DatesPath = "https://iss.moex.com/iss/engines/stock/zcyc.xml?iss.only=yearyields.dates&iss.meta=off";

        public Task<ZcycData> ReadDataForDate(DateTime date)
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

        public async Task<DateRange> GetAvailableDates()
        {
            var tcs = new TaskCompletionSource<DateRange>();

            var settings = new XmlReaderSettings
            {
                Async = true
            };

            using XmlReader reader = XmlReader.Create(DatesPath, settings);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name.Equals("row"))
                        {
                            var date1 = DateTime.Parse(reader.GetAttribute(0));
                            var date2 = DateTime.Parse(reader.GetAttribute(1));
                            tcs.TrySetResult(new DateRange(date1, date2));
                        }
                        break;

                    case XmlNodeType.Text:
                        //Debug.WriteLine($"Text Node: {await reader.GetValueAsync()}");
                        break;

                    case XmlNodeType.EndElement:
                        //Debug.WriteLine($"End Element {reader.Name}");
                        break;

                    default:
                        //Debug.WriteLine($"Other node {reader.NodeType} with value {reader.Value}");
                        break;
                }
            }
            return await tcs.Task.ConfigureAwait(false);
        }

        public Task<bool> SaveData(ZcycData data) => Task.FromResult(true);
    }
}