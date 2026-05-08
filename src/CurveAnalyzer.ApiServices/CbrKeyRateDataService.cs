using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.ApiServices;

public sealed class CbrKeyRateDataService(HttpClient httpClient) : ICbrKeyRateDataService
{
    private static readonly Uri Endpoint = new("https://www.cbr.ru/DailyInfoWebServ/DailyInfo.asmx");

    public async Task<IReadOnlyList<CbrKeyRate>> GetKeyRatesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;
        if (startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("SOAPAction", "\"http://web.cbr.ru/KeyRate\"");
        request.Content = new StringContent(CreateSoapBody(startDate, endDate), Encoding.UTF8, "text/xml");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml")
        {
            CharSet = Encoding.UTF8.WebName
        };

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParseKeyRateSoapResponse(xml, DateTime.UtcNow);
    }

    public static IReadOnlyList<CbrKeyRate> ParseKeyRateSoapResponse(string xml, DateTime loadedAt)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return [];
        }

        var document = XDocument.Parse(xml);
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "KR")
            .Select(element => ParseKeyRateRow(element, loadedAt))
            .Where(rate => rate is not null)
            .Select(rate => rate!)
            .GroupBy(rate => rate.Date)
            .Select(group => group.OrderByDescending(rate => rate.LoadedAt).First())
            .OrderBy(rate => rate.Date)
            .ToList();
    }

    private static CbrKeyRate? ParseKeyRateRow(XElement row, DateTime loadedAt)
    {
        var dateText = row.Elements().FirstOrDefault(element => element.Name.LocalName == "DT")?.Value;
        var rateText = row.Elements().FirstOrDefault(element => element.Name.LocalName == "Rate")?.Value;

        if (!DateTimeOffset.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ||
            !double.TryParse(rateText, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) ||
            !double.IsFinite(rate))
        {
            return null;
        }

        return new CbrKeyRate
        {
            Date = date.Date,
            Rate = rate,
            LoadedAt = loadedAt
        };
    }

    private static string CreateSoapBody(DateTime startDate, DateTime endDate)
    {
        var fromDate = startDate.ToString("yyyy-MM-dd'T'00:00:00", CultureInfo.InvariantCulture);
        var toDate = endDate.ToString("yyyy-MM-dd'T'00:00:00", CultureInfo.InvariantCulture);

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <KeyRate xmlns="http://web.cbr.ru/">
                  <fromDate>{fromDate}</fromDate>
                  <ToDate>{toDate}</ToDate>
                </KeyRate>
              </soap:Body>
            </soap:Envelope>
            """;
    }
}
