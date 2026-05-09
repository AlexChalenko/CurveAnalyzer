using System.Net;
using CurveAnalyzer.ApiServices;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Infrastructure.Tests.ApiServices;

public sealed class OfzCashflowOnlineDataServiceTests
{
    [Fact]
    public async Task GetScheduleAsync_ParsesBondizationBlocks()
    {
        using var httpClient = new HttpClient(new StubHandler(_ => BondizationJson()));
        var service = new OfzCashflowOnlineDataService(httpClient);

        var events = await service.GetScheduleAsync(["SU26238RMFS4"], TestContext.Current.CancellationToken);

        Assert.Equal(3, events.Count);
        var coupon = Assert.Single(events, item => item.EventType == OfzCashflowEventType.Coupon);
        Assert.Equal("SU26238RMFS4", coupon.SecId);
        Assert.Equal(new DateTime(2026, 05, 20), coupon.EventDate);
        Assert.Equal(new DateTime(2026, 05, 19), coupon.RecordDate);
        Assert.Equal(12.34, coupon.Value);
        Assert.Equal(12.34, coupon.ValueRub);
        Assert.Equal(6.1, coupon.ValuePercent);
        Assert.Equal(OfzCashflowSourceKind.Schedule, coupon.SourceKind);

        var maturity = Assert.Single(events, item => item.EventType == OfzCashflowEventType.Maturity);
        Assert.Equal(new DateTime(2026, 06, 01), maturity.EventDate);
        Assert.Equal(100, maturity.ValuePercent);

        var offer = Assert.Single(events, item => item.EventType == OfzCashflowEventType.Offer);
        Assert.Equal(new DateTime(2026, 05, 25), offer.EventDate);
        Assert.Equal(new DateTime(2026, 05, 21), offer.StartDate);
        Assert.Equal(99.5, offer.Price);
        Assert.Equal("put", offer.OfferType);
    }

    [Fact]
    public async Task GetScheduleAsync_TreatsZeroCouponFieldsAsMissing()
    {
        using var httpClient = new HttpClient(new StubHandler(_ => ZeroCouponJson()));
        var service = new OfzCashflowOnlineDataService(httpClient);

        var events = await service.GetScheduleAsync(["SU29017RMFS8"], TestContext.Current.CancellationToken);

        var coupon = Assert.Single(events);
        Assert.Equal(OfzCashflowEventType.Coupon, coupon.EventType);
        Assert.Null(coupon.Value);
        Assert.Null(coupon.ValueRub);
        Assert.Null(coupon.ValuePercent);
    }

    [Fact]
    public async Task GetScheduleAsync_FillsRubCouponValueFromValueWhenValueRubMissing()
    {
        using var httpClient = new HttpClient(new StubHandler(_ => CouponWithoutValueRubJson()));
        var service = new OfzCashflowOnlineDataService(httpClient);

        var events = await service.GetScheduleAsync(["SU29009RMFS6"], TestContext.Current.CancellationToken);

        var coupon = Assert.Single(events);
        Assert.Equal(99.18, coupon.Value);
        Assert.Equal(99.18, coupon.ValueRub);
        Assert.Equal(19.89, coupon.ValuePercent);
    }

    [Fact]
    public async Task GetScheduleAsync_TreatsZeroSnapshotCouponFieldsAsMissing()
    {
        using var httpClient = new HttpClient(new StubHandler(uri =>
            uri.AbsoluteUri.Contains("bondization", StringComparison.Ordinal)
                ? EmptyBondizationJson()
                : ZeroSnapshotCouponJson()));
        var service = new OfzCashflowOnlineDataService(httpClient);

        var events = await service.GetScheduleAsync(["SU29017RMFS8"], TestContext.Current.CancellationToken);

        var coupon = Assert.Single(events, item => item.EventType == OfzCashflowEventType.Coupon);
        Assert.Equal(OfzCashflowSourceKind.Snapshot, coupon.SourceKind);
        Assert.Null(coupon.Value);
        Assert.Null(coupon.ValuePercent);
    }

    [Fact]
    public async Task GetScheduleAsync_FillsRubSnapshotCouponValueFromValue()
    {
        using var httpClient = new HttpClient(new StubHandler(uri =>
            uri.AbsoluteUri.Contains("bondization", StringComparison.Ordinal)
                ? EmptyBondizationJson()
                : SnapshotJson()));
        var service = new OfzCashflowOnlineDataService(httpClient);

        var events = await service.GetScheduleAsync(["SU26238RMFS4"], TestContext.Current.CancellationToken);

        var coupon = Assert.Single(events, item => item.EventType == OfzCashflowEventType.Coupon);
        Assert.Equal(12.34, coupon.Value);
        Assert.Equal(12.34, coupon.ValueRub);
    }

    [Fact]
    public async Task GetScheduleAsync_UrlEncodesSecId()
    {
        Uri? requestedUri = null;
        using var httpClient = new HttpClient(new StubHandler(uri =>
        {
            requestedUri = uri;
            return EmptyBondizationJson();
        }));
        var service = new OfzCashflowOnlineDataService(httpClient);

        await service.GetScheduleAsync(["TEST+ID"], TestContext.Current.CancellationToken);

        Assert.NotNull(requestedUri);
        Assert.Contains("TEST%2BID", requestedUri.AbsoluteUri);
    }

    [Fact]
    public async Task GetScheduleAsync_SkipsRowsWithoutDate()
    {
        using var httpClient = new HttpClient(new StubHandler(_ => MissingDateJson()));
        var service = new OfzCashflowOnlineDataService(httpClient);

        var events = await service.GetScheduleAsync(["SU26238RMFS4"], TestContext.Current.CancellationToken);

        Assert.Empty(events);
    }

    [Fact]
    public async Task GetScheduleAsync_AddsSnapshotFallbackEvents()
    {
        using var httpClient = new HttpClient(new StubHandler(uri =>
            uri.AbsoluteUri.Contains("bondization", StringComparison.Ordinal)
                ? EmptyBondizationJson()
                : SnapshotJson()));
        var service = new OfzCashflowOnlineDataService(httpClient);

        var events = await service.GetScheduleAsync(["SU26238RMFS4"], TestContext.Current.CancellationToken);

        Assert.Contains(events, item =>
            item.EventType == OfzCashflowEventType.Buyback &&
            item.EventDate == new DateTime(2026, 07, 01) &&
            item.SourceKind == OfzCashflowSourceKind.Snapshot &&
            item.IsProvisional);
        Assert.Contains(events, item =>
            item.EventType == OfzCashflowEventType.CallOption &&
            item.EventDate == new DateTime(2026, 08, 01));
        Assert.Contains(events, item =>
            item.EventType == OfzCashflowEventType.PutOption &&
            item.EventDate == new DateTime(2026, 09, 01));
    }

    private static string BondizationJson()
    {
        return """
        {
          "coupons": {
            "columns": ["isin","name","issuevalue","coupondate","recorddate","startdate","initialfacevalue","facevalue","faceunit","value","valueprc","value_rub","secid","primary_boardid"],
            "data": [["RU000A0","ОФЗ 26238",1000,"2026-05-20","2026-05-19","2026-02-20",1000,1000,"SUR",12.34,6.1,0,"SU26238RMFS4","TQOB"]]
          },
          "amortizations": {
            "columns": ["isin","name","issuevalue","amortdate","facevalue","initialfacevalue","faceunit","valueprc","value","value_rub","data_source","secid","primary_boardid"],
            "data": [["RU000A0","ОФЗ 26238",1000,"2026-06-01",0,1000,"SUR",100,1000,1000,"maturity","SU26238RMFS4","TQOB"]]
          },
          "offers": {
            "columns": ["isin","name","issuevalue","offerdate","offerdatestart","offerdateend","facevalue","faceunit","price","value","agent","offertype","secid","primary_boardid"],
            "data": [["RU000A0","ОФЗ 26238",1000,"2026-05-25","2026-05-21","2026-05-24",1000,"SUR",99.5,null,"agent","put","SU26238RMFS4","TQOB"]]
          }
        }
        """;
    }

    private static string EmptyBondizationJson()
    {
        return """
        {
          "coupons": { "columns": ["secid","coupondate"], "data": [] },
          "amortizations": { "columns": ["secid","amortdate"], "data": [] },
          "offers": { "columns": ["secid","offerdate"], "data": [] }
        }
        """;
    }

    private static string ZeroCouponJson()
    {
        return """
        {
          "coupons": {
            "columns": ["isin","name","issuevalue","coupondate","recorddate","startdate","initialfacevalue","facevalue","faceunit","value","valueprc","value_rub","secid","primary_boardid"],
            "data": [["RU000A0","ОФЗ-ПК 29017",1000,"2026-06-03",null,"2026-05-27",1000,1000,"RUB",0,null,0,"SU29017RMFS8","TQOB"]]
          },
          "amortizations": { "columns": ["secid","amortdate"], "data": [] },
          "offers": { "columns": ["secid","offerdate"], "data": [] }
        }
        """;
    }

    private static string CouponWithoutValueRubJson()
    {
        return """
        {
          "coupons": {
            "columns": ["isin","name","issuevalue","coupondate","recorddate","startdate","initialfacevalue","facevalue","faceunit","value","valueprc","value_rub","secid","primary_boardid"],
            "data": [["RU000A0","ОФЗ-ПК 29009",1000,"2026-05-13",null,"2026-02-12",1000,1000,"RUB",99.18,19.89,null,"SU29009RMFS6","TQOB"]]
          },
          "amortizations": { "columns": ["secid","amortdate"], "data": [] },
          "offers": { "columns": ["secid","offerdate"], "data": [] }
        }
        """;
    }

    private static string MissingDateJson()
    {
        return """
        {
          "coupons": { "columns": ["secid","coupondate"], "data": [["SU26238RMFS4", null]] }
        }
        """;
    }

    private static string SnapshotJson()
    {
        return """
        {
          "securities": {
            "columns": ["SECID","SHORTNAME","NEXTCOUPON","COUPONVALUE","COUPONPERCENT","MATDATE","OFFERDATE","BUYBACKDATE","CALLOPTIONDATE","PUTOPTIONDATE","FACEVALUE","FACEUNIT"],
            "data": [["SU26238RMFS4","ОФЗ 26238","2026-05-20",12.34,6.1,"2026-06-01","2026-05-25","2026-07-01","2026-08-01","2026-09-01",1000,"SUR"]]
          }
        }
        """;
    }

    private static string ZeroSnapshotCouponJson()
    {
        return """
        {
          "securities": {
            "columns": ["SECID","SHORTNAME","NEXTCOUPON","COUPONVALUE","COUPONPERCENT","MATDATE","OFFERDATE","BUYBACKDATE","CALLOPTIONDATE","PUTOPTIONDATE","FACEVALUE","FACEUNIT"],
            "data": [["SU29017RMFS8","ОФЗ-ПК 29017","2026-06-03",0,null,null,null,null,null,null,1000,"RUB"]]
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
