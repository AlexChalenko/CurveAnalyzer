using CurveAnalyzer.ApiServices;

namespace CurveAnalyzer.Infrastructure.Tests.ApiServices;

public sealed class CbrKeyRateDataServiceTests
{
    [Fact]
    public void ParseKeyRateSoapResponse_ReadsRatesFromDailyInfoResponse()
    {
        var loadedAt = new DateTime(2026, 05, 08, 9, 0, 0, DateTimeKind.Utc);
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <KeyRateResponse xmlns="http://web.cbr.ru/">
                  <KeyRateResult>
                    <diffgram xmlns="urn:schemas-microsoft-com:xml-diffgram-v1">
                      <KeyRate xmlns="">
                        <KR>
                          <DT>2026-05-08T00:00:00+03:00</DT>
                          <Rate>14.50</Rate>
                        </KR>
                        <KR>
                          <DT>2026-05-07T00:00:00+03:00</DT>
                          <Rate>14.50</Rate>
                        </KR>
                      </KeyRate>
                    </diffgram>
                  </KeyRateResult>
                </KeyRateResponse>
              </soap:Body>
            </soap:Envelope>
            """;

        var rates = CbrKeyRateDataService.ParseKeyRateSoapResponse(xml, loadedAt);

        Assert.Collection(
            rates,
            first =>
            {
                Assert.Equal(new DateTime(2026, 05, 07), first.Date);
                Assert.Equal(14.50, first.Rate);
                Assert.Equal(loadedAt, first.LoadedAt);
            },
            second =>
            {
                Assert.Equal(new DateTime(2026, 05, 08), second.Date);
                Assert.Equal(14.50, second.Rate);
                Assert.Equal(loadedAt, second.LoadedAt);
            });
    }
}
