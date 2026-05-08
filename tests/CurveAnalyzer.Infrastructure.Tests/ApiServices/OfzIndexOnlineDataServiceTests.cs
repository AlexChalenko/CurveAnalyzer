using System.Net;
using CurveAnalyzer.ApiServices;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Infrastructure.Tests.ApiServices;

public sealed class OfzIndexOnlineDataServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_ParsesNullableIndexHistoryFields()
    {
        using var httpClient = new HttpClient(new StubHandler(_ => HistoryJson()));
        var service = new OfzIndexOnlineDataService(httpClient);

        var points = await service.GetHistoryAsync(
            ["RGBI"],
            new DateTime(2026, 04, 01),
            new DateTime(2026, 04, 02),
            TestContext.Current.CancellationToken);

        var point = Assert.Single(points);
        Assert.Equal("RGBI", point.SecId);
        Assert.Equal(new DateTime(2026, 04, 02), point.TradeDate);
        Assert.Equal(100.42, point.Close);
        Assert.Null(point.Yield);
        Assert.Equal(910.5, point.Duration);
        Assert.Equal(OfzMarketIndexSourceKind.History, point.SourceKind);
        Assert.False(point.IsProvisional);
    }

    [Fact]
    public async Task GetHistoryAsync_UrlEncodesPlusInSegmentSecId()
    {
        Uri? requestedUri = null;
        using var httpClient = new HttpClient(new StubHandler(uri =>
        {
            requestedUri = uri;
            return HistoryJson("RUGBICP7Y+");
        }));
        var service = new OfzIndexOnlineDataService(httpClient);

        await service.GetHistoryAsync(
            ["RUGBICP7Y+"],
            new DateTime(2026, 04, 01),
            new DateTime(2026, 04, 02),
            TestContext.Current.CancellationToken);

        Assert.NotNull(requestedUri);
        Assert.Contains("RUGBICP7Y%2B", requestedUri.AbsoluteUri);
    }

    [Fact]
    public async Task GetCurrentSnapshotAsync_ParsesSnapshotWithoutZeroSubstitution()
    {
        using var httpClient = new HttpClient(new StubHandler(_ => SnapshotJson()));
        var service = new OfzIndexOnlineDataService(httpClient);

        var points = await service.GetCurrentSnapshotAsync(["RGBI"], TestContext.Current.CancellationToken);

        var point = Assert.Single(points);
        Assert.Equal("RGBI", point.SecId);
        Assert.Equal(new DateTime(2026, 04, 02), point.TradeDate);
        Assert.Equal(101.12, point.Close);
        Assert.Null(point.Duration);
        Assert.Equal(OfzMarketIndexSourceKind.Snapshot, point.SourceKind);
        Assert.True(point.IsProvisional);
        Assert.NotNull(point.ObservedAt);
    }

    private static string HistoryJson(string secId = "RGBI")
    {
        return $$"""
        {
          "history": {
            "columns": ["SECID","TRADEDATE","SHORTNAME","NAME","OPEN","HIGH","LOW","CLOSE","VALUE","DURATION","YIELD","CURRENCYID"],
            "data": [["{{secId}}","2026-04-02","{{secId}}","Index {{secId}}",99.8,100.7,99.7,100.42,123456789,910.5,null,"RUB"]]
          },
          "history.cursor": {
            "columns": ["INDEX","TOTAL","PAGESIZE"],
            "data": [[0,1,100]]
          }
        }
        """;
    }

    private static string SnapshotJson()
    {
        return """
        {
          "securities": {
            "columns": ["SECID","SHORTNAME","NAME","CURRENCYID"],
            "data": [["RGBI","RGBI","Index RGBI","RUB"]]
          },
          "marketdata": {
            "columns": ["SECID","TRADINGSESSIONDATE","CURRENTVALUE","OPENVALUE","HIGHVALUE","LOWVALUE","VALTODAY","DURATION","YIELD"],
            "data": [["RGBI","2026-04-02",101.12,100.00,101.20,99.90,987654321,0,14.25]]
          }
        }
        """;
    }

    private sealed class StubHandler(Func<Uri, string> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var json = responseFactory(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }
}
