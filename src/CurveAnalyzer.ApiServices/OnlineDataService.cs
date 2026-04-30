using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using CurveAnalyzer.ApiServices.Data;
using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.ApiServices;

public class OnlineDataService(HttpClient httpClient) : IDataService
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
        Async = true,
        CloseInput = true
    };

    public async Task<IReadOnlyList<DateTime>> GetAvailableDatesAsync(CancellationToken cancellationToken = default)
    {
        using XmlReader reader = await CreateXmlReaderAsync(DatesUrl, cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType != XmlNodeType.Element ||
                !reader.Name.Equals("row", StringComparison.Ordinal))
            {
                continue;
            }

            if (DateTime.TryParse(reader.GetAttribute(0), culture, DateTimeStyles.None, out var startDate)
                && DateTime.TryParse(reader.GetAttribute(1), culture, DateTimeStyles.None, out var endDate))
            {
                return Enumerable.Range(0, int.MaxValue)
                    .Select(index => startDate.AddDays(index))
                    .TakeWhile(date => date <= endDate)
                    .ToList();
            }

            throw new InvalidOperationException("Invalid dates");
        }

        return [];
    }

    public async Task<ZcycData> GetDataForDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (date.Equals(DateTime.Today) && DateTime.Now.Subtract(lastUpdateTime) < cachePeriod && _todayZcycData != null)
        {
            return _todayZcycData;
        }

        var serializer = new XmlSerializer(typeof(IssData));
        string downloadingDate = date.ToString("yyyy-MM-dd");
        string url = string.Format(culture, DataUrl, downloadingDate);

        try
        {
            using XmlReader xmlReader = await CreateXmlReaderAsync(url, cancellationToken).ConfigureAwait(false);
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
                return zData;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        return new ZcycData(date, []);
    }

    public async Task<IReadOnlyList<Zcyc>> GetDataForPeriodAsync(double period, CancellationToken cancellationToken = default)
    {
        var data = await GetDataForDateAsync(DateTime.Today, cancellationToken).ConfigureAwait(false);
        return data.DataRow
            .Where(r => r.Period == period)
            .Select(r => new Zcyc()
            {
                Tradedate = DateTime.Today,
                Period = r.Period,
                Value = r.Value
            })
            .ToList();
    }

    public async Task<IReadOnlyList<double>> GetPeriodsAsync(CancellationToken cancellationToken = default)
    {
        List<double> result = [];

        using XmlReader reader = await CreateXmlReaderAsync(PeriodsUrl, cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

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

    private async Task<XmlReader> CreateXmlReaderAsync(string url, CancellationToken cancellationToken)
    {
        var payload = await httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
        return XmlReader.Create(new MemoryStream(payload), _xmlReaderSettings);
    }
}
