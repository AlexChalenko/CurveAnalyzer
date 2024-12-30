using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using CurveAnalyzer.ApiService.Data;
using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.ApiServices;

public class OnlineDataService : IDataService
{
    private const string DataUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?date={0}&iss.only=yearyields&iss.meta=off";
    private const string DatesUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?iss.only=yearyields.dates&iss.meta=off";
    private const string PeriodsUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?iss.only=yearyields&yearyields.columns=period&iss.meta=off";
    private const string SecuritiesUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?date={0}&iss.only=securities&iss.meta=off";

    // https://iss.moex.com/iss/history/engines/stock/zcyc

    private readonly CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

    private ZcycData? _todayZcycData;

    private readonly TimeSpan cachePeriod = TimeSpan.FromMinutes(5);
    private DateTime lastUpdateTime = DateTime.MinValue;
    private XmlReaderSettings _xmlReaderSettings => new XmlReaderSettings
    {
        Async = true
    };

    public Task<IEnumerable<DateTime>> GetAvailableDates(CancellationToken token)
    {
        TaskCompletionSource<IEnumerable<DateTime>> tcs = new();

        if (token.IsCancellationRequested)
        {
            tcs.SetResult([]);
        }
        else
        {
            try
            {
                using XmlReader reader = XmlReader.Create(DatesUrl, _xmlReaderSettings);
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
        if (date.Equals(DateTime.Today) && DateTime.Now.Subtract(lastUpdateTime) < cachePeriod && _todayZcycData != null)
        {
            return Task.FromResult(_todayZcycData);
        }

        var tcs = new TaskCompletionSource<ZcycData>();
        var serializer = new XmlSerializer(typeof(IssData));
        string downloadingDate = date.ToString("yyyy-MM-dd");
        string url = string.Format(culture, DataUrl, downloadingDate);

        try
        {
            using XmlReader xmlReader = XmlReader.Create(url); //gets only 500 lines
            if (serializer.Deserialize(xmlReader) is IssData { data: not null } issData)
            {
                var zData = new ZcycData()
                {
                    Date = date,
                    DataRow = issData.data.rows.Select(item => new ZcycDataRow(item.period, item.value)).ToList()
                };

                if (date.Equals(DateTime.Today))
                {
                    _todayZcycData = zData;
                    lastUpdateTime = DateTime.Now;
                }
                tcs.SetResult(zData);
            }
        }
        catch (Exception ex) // on error return empty data
        {
            tcs.SetException(ex);
        }
        return tcs.Task;
    }

    public async Task<IEnumerable<Zcyc>> GetDataForPeriod(double period)
    {
        var data = await GetDataForDate(DateTime.Today);
        return data.DataRow.Where(r => r.Period == period).Select(r => new Zcyc()
        {
            Tradedate = DateTime.Today,
            Period = r.Period,
            Value = r.Value
        });
    }

    public async Task<IEnumerable<double>> GetPeriods()
    {
        List<double> result = [];

        using XmlReader reader = XmlReader.Create(PeriodsUrl, _xmlReaderSettings);
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
