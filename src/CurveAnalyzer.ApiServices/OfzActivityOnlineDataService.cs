using System.Text.Json;
using CurveAnalyzer.ApiServices.Data;
using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.ApiServices;

public sealed class OfzActivityOnlineDataService(HttpClient httpClient) : IOfzActivityDataService
{
    private const string BoardId = "TQOB";
    private const string HistoryColumns =
        "BOARDID,TRADEDATE,SHORTNAME,SECID,NUMTRADES,VALUE,LOW,HIGH,CLOSE,WAPRICE,YIELDCLOSE,OPEN,VOLUME,MATDATE,DURATION,YIELDATWAP,BID,OFFER,SPREAD,HIGHBID,LOWOFFER,ZSPREAD,ZSPREADATWAPRICE,IRICPICLOSE,BEICLOSE,CBRCLOSE,COUPONPERCENT,COUPONVALUE,COUPONPERIOD,COUPONDATE,FACEVALUE,INITIALFACEVALUE,CURRENCYID,FACEUNIT,BONDTYPE,BONDSUBTYPE,SECNAME,ISSUENAME,LISTLEVEL,ISSUESIZE,ISSUESIZEPLACED";

    public async Task<OfzActivityDailyData> GetHistoryForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var requestedDate = date.Date;
        var loadedAt = DateTime.UtcNow;
        List<OfzIssue> issues = [];
        List<OfzDailyTrade> trades = [];

        var start = 0;
        var total = 0;

        do
        {
            var url = CreateHistoryUrl(requestedDate, start);
            await using var stream = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;

            var history = IssJsonTable.TryCreate(root, "history");
            if (history is not null)
            {
                foreach (var row in history.Rows)
                {
                    var secId = history.GetString(row, "SECID");
                    var tradeDate = history.GetDate(row, "TRADEDATE") ?? requestedDate;
                    if (string.IsNullOrWhiteSpace(secId))
                    {
                        continue;
                    }

                    issues.Add(MapIssueFromHistory(history, row, secId, loadedAt));
                    trades.Add(MapTradeFromHistory(history, row, secId, tradeDate, loadedAt));
                }
            }

            var cursor = IssCursor.From(root, "history.cursor");
            total = cursor.Total;
            start = cursor.PageSize > 0 ? cursor.NextStart : total;
        }
        while (start < total);

        var distinctTrades = trades
            .GroupBy(trade => new { trade.BoardId, trade.SecId, trade.TradeDate })
            .Select(group => group.Last())
            .ToList();

        return new OfzActivityDailyData(
            BoardId,
            requestedDate,
            DistinctIssues(issues),
            distinctTrades,
            new OfzActivityLoadState
            {
                BoardId = BoardId,
                TradeDate = requestedDate,
                Status = distinctTrades.Count == 0 ? OfzActivityLoadStatus.NoData : OfzActivityLoadStatus.Loaded,
                RowsLoaded = distinctTrades.Count,
                LoadedAt = loadedAt
            });
    }

    public async Task<OfzActivityDailyData> GetCurrentSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var tradeDate = DateTime.Today;
        var loadedAt = DateTime.UtcNow;
        var url = "https://iss.moex.com/iss/engines/stock/markets/bonds/boards/TQOB/securities.json?iss.only=securities,marketdata,marketdata_yields&iss.meta=off";

        await using var stream = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;

        var issuesBySecId = new Dictionary<string, OfzIssue>(StringComparer.Ordinal);
        var securities = IssJsonTable.TryCreate(root, "securities");
        if (securities is not null)
        {
            foreach (var row in securities.Rows)
            {
                var secId = securities.GetString(row, "SECID");
                if (string.IsNullOrWhiteSpace(secId))
                {
                    continue;
                }

                issuesBySecId[secId] = MapIssueFromSecurities(securities, row, secId, loadedAt);
            }
        }

        var yieldDataBySecId = MapYieldDataBySecId(root);
        List<OfzDailyTrade> trades = [];
        List<OfzLiquiditySnapshot> liquiditySnapshots = [];
        var marketData = IssJsonTable.TryCreate(root, "marketdata");
        if (marketData is not null)
        {
            foreach (var row in marketData.Rows)
            {
                var secId = marketData.GetString(row, "SECID");
                if (string.IsNullOrWhiteSpace(secId))
                {
                    continue;
                }

                if (!issuesBySecId.ContainsKey(secId))
                {
                    issuesBySecId[secId] = new OfzIssue
                    {
                        SecId = secId,
                        ShortName = secId,
                        MetadataLoadedAt = loadedAt,
                        ClassificationSource = OfzIssueClassificationSources.Snapshot
                    };
                }

                trades.Add(MapTradeFromMarketData(marketData, row, secId, tradeDate, loadedAt));
                yieldDataBySecId.TryGetValue(secId, out var yieldData);
                liquiditySnapshots.Add(MapLiquiditySnapshotFromMarketData(marketData, row, secId, tradeDate, loadedAt, yieldData));
            }
        }

        return new OfzActivityDailyData(
            BoardId,
            tradeDate,
            issuesBySecId.Values.OrderBy(issue => issue.SecId, StringComparer.Ordinal).ToList(),
            trades,
            new OfzActivityLoadState
            {
                BoardId = BoardId,
                TradeDate = tradeDate,
                IsProvisional = true,
                Status = trades.Count == 0 ? OfzActivityLoadStatus.NoData : OfzActivityLoadStatus.Loaded,
                RowsLoaded = trades.Count,
                LoadedAt = loadedAt
            })
        {
            LiquiditySnapshots = liquiditySnapshots
        };
    }

    private static string CreateHistoryUrl(DateTime date, int start)
    {
        return "https://iss.moex.com/iss/history/engines/stock/markets/bonds/boards/TQOB/securities.json" +
            $"?date={date:yyyy-MM-dd}" +
            "&iss.only=history,history.cursor" +
            "&iss.meta=off" +
            $"&start={start}" +
            $"&history.columns={Uri.EscapeDataString(HistoryColumns)}";
    }

    private static OfzIssue MapIssueFromHistory(IssJsonTable table, JsonElement row, string secId, DateTime loadedAt)
    {
        return new OfzIssue
        {
            SecId = secId,
            ShortName = table.GetString(row, "SHORTNAME") ?? secId,
            SecName = table.GetString(row, "SECNAME"),
            IssueName = table.GetString(row, "ISSUENAME"),
            MatDate = table.GetDate(row, "MATDATE"),
            FaceValue = table.GetDouble(row, "FACEVALUE"),
            InitialFaceValue = table.GetDouble(row, "INITIALFACEVALUE"),
            FaceUnit = table.GetString(row, "FACEUNIT"),
            CurrencyId = table.GetString(row, "CURRENCYID"),
            CouponPercent = table.GetDouble(row, "COUPONPERCENT"),
            CouponValue = table.GetDouble(row, "COUPONVALUE"),
            CouponPeriod = table.GetInt(row, "COUPONPERIOD"),
            NextCouponDate = table.GetDate(row, "COUPONDATE"),
            ListLevel = table.GetInt(row, "LISTLEVEL"),
            IssueSize = table.GetDouble(row, "ISSUESIZE"),
            IssueSizePlaced = table.GetDouble(row, "ISSUESIZEPLACED"),
            MetadataLoadedAt = loadedAt,
            ClassificationSource = OfzIssueClassificationSources.History,
            BondType = table.GetString(row, "BONDTYPE"),
            BondSubType = table.GetString(row, "BONDSUBTYPE")
        };
    }

    private static OfzIssue MapIssueFromSecurities(IssJsonTable table, JsonElement row, string secId, DateTime loadedAt)
    {
        return new OfzIssue
        {
            SecId = secId,
            ShortName = table.GetString(row, "SHORTNAME") ?? secId,
            SecName = table.GetString(row, "SECNAME"),
            IssueName = table.GetString(row, "ISSUENAME"),
            Isin = table.GetString(row, "ISIN"),
            MatDate = table.GetDate(row, "MATDATE"),
            FaceValue = table.GetDouble(row, "FACEVALUE"),
            InitialFaceValue = table.GetDouble(row, "INITIALFACEVALUE"),
            FaceUnit = table.GetString(row, "FACEUNIT"),
            CurrencyId = table.GetString(row, "CURRENCYID"),
            CouponPercent = table.GetDouble(row, "COUPONPERCENT"),
            CouponValue = table.GetDouble(row, "COUPONVALUE"),
            CouponPeriod = table.GetInt(row, "COUPONPERIOD"),
            NextCouponDate = table.GetDate(row, "NEXTCOUPON") ?? table.GetDate(row, "COUPONDATE"),
            ListLevel = table.GetInt(row, "LISTLEVEL"),
            IssueSize = table.GetDouble(row, "ISSUESIZE"),
            IssueSizePlaced = table.GetDouble(row, "ISSUESIZEPLACED"),
            MetadataLoadedAt = loadedAt,
            ClassificationSource = OfzIssueClassificationSources.Snapshot,
            BondType = table.GetString(row, "BONDTYPE"),
            BondSubType = table.GetString(row, "BONDSUBTYPE")
        };
    }

    private static OfzDailyTrade MapTradeFromHistory(
        IssJsonTable table,
        JsonElement row,
        string secId,
        DateTime tradeDate,
        DateTime loadedAt)
    {
        return new OfzDailyTrade
        {
            BoardId = table.GetString(row, "BOARDID") ?? BoardId,
            SecId = secId,
            TradeDate = tradeDate.Date,
            NumTrades = table.GetInt(row, "NUMTRADES"),
            Value = table.GetDouble(row, "VALUE"),
            Volume = table.GetDouble(row, "VOLUME"),
            OpenPrice = table.GetDouble(row, "OPEN"),
            LowPrice = table.GetDouble(row, "LOW"),
            HighPrice = table.GetDouble(row, "HIGH"),
            ClosePrice = table.GetDouble(row, "CLOSE"),
            WeightedAveragePrice = table.GetDouble(row, "WAPRICE"),
            YieldClose = NullIfZero(table.GetDouble(row, "YIELDCLOSE")),
            YieldAtWeightedAveragePrice = NullIfZero(table.GetDouble(row, "YIELDATWAP")),
            Duration = NullIfZero(table.GetDouble(row, "DURATION")),
            Bid = table.GetDouble(row, "BID"),
            Offer = table.GetDouble(row, "OFFER"),
            Spread = table.GetDouble(row, "SPREAD"),
            HighBid = table.GetDouble(row, "HIGHBID"),
            LowOffer = table.GetDouble(row, "LOWOFFER"),
            ZSpread = table.GetDouble(row, "ZSPREAD"),
            ZSpreadAtWeightedAveragePrice = table.GetDouble(row, "ZSPREADATWAPRICE"),
            ImpliedFloatingRate = table.GetDouble(row, "IRICPICLOSE"),
            ImpliedInflation = table.GetDouble(row, "BEICLOSE"),
            ImpliedCbrRate = table.GetDouble(row, "CBRCLOSE"),
            LoadedAt = loadedAt
        };
    }

    private static OfzDailyTrade MapTradeFromMarketData(
        IssJsonTable table,
        JsonElement row,
        string secId,
        DateTime tradeDate,
        DateTime loadedAt)
    {
        return new OfzDailyTrade
        {
            BoardId = table.GetString(row, "BOARDID") ?? BoardId,
            SecId = secId,
            TradeDate = tradeDate.Date,
            NumTrades = table.GetInt(row, "NUMTRADES"),
            Value = table.GetDouble(row, "VALTODAY"),
            Volume = table.GetDouble(row, "VOLTODAY"),
            ClosePrice = table.GetDouble(row, "LAST"),
            WeightedAveragePrice = table.GetDouble(row, "WAPRICE"),
            YieldClose = NullIfZero(table.GetDouble(row, "YIELD")),
            YieldAtWeightedAveragePrice = NullIfZero(table.GetDouble(row, "YIELDATWAPRICE")),
            Duration = NullIfZero(table.GetDouble(row, "DURATION")),
            ZSpread = table.GetDouble(row, "ZSPREAD"),
            ZSpreadAtWeightedAveragePrice = table.GetDouble(row, "ZSPREADATWAPRICE"),
            ImpliedFloatingRate = table.GetDouble(row, "IRICPICLOSE"),
            ImpliedInflation = table.GetDouble(row, "BEICLOSE"),
            ImpliedCbrRate = table.GetDouble(row, "CBRCLOSE"),
            LoadedAt = loadedAt
        };
    }

    private static Dictionary<string, CurrentYieldData> MapYieldDataBySecId(JsonElement root)
    {
        var result = new Dictionary<string, CurrentYieldData>(StringComparer.Ordinal);
        var marketDataYields = IssJsonTable.TryCreate(root, "marketdata_yields");
        if (marketDataYields is null)
        {
            return result;
        }

        foreach (var row in marketDataYields.Rows)
        {
            var secId = marketDataYields.GetString(row, "SECID");
            if (string.IsNullOrWhiteSpace(secId))
            {
                continue;
            }

            result[secId] = new CurrentYieldData(
                NullIfZero(marketDataYields.GetDouble(row, "EFFECTIVEYIELD")),
                NullIfZero(marketDataYields.GetDouble(row, "EFFECTIVEYIELDWAPRICE")),
                NullIfZero(marketDataYields.GetDouble(row, "DURATION")),
                NullIfZero(marketDataYields.GetDouble(row, "DURATIONWAPRICE")),
                marketDataYields.GetDouble(row, "ZSPREADBP"),
                marketDataYields.GetDouble(row, "GSPREADBP"));
        }

        return result;
    }

    private static OfzLiquiditySnapshot MapLiquiditySnapshotFromMarketData(
        IssJsonTable table,
        JsonElement row,
        string secId,
        DateTime tradeDate,
        DateTime observedAt,
        CurrentYieldData yieldData)
    {
        return new OfzLiquiditySnapshot
        {
            BoardId = table.GetString(row, "BOARDID") ?? BoardId,
            SecId = secId,
            TradeDate = tradeDate.Date,
            ObservedAt = observedAt,
            Bid = table.GetDouble(row, "BID"),
            Offer = table.GetDouble(row, "OFFER"),
            Spread = table.GetDouble(row, "SPREAD"),
            BidDepth = table.GetDouble(row, "BIDDEPTH"),
            OfferDepth = table.GetDouble(row, "OFFERDEPTH"),
            BidDepthTotal = table.GetDouble(row, "BIDDEPTHT"),
            OfferDepthTotal = table.GetDouble(row, "OFFERDEPTHT"),
            NumBids = table.GetInt(row, "NUMBIDS"),
            NumOffers = table.GetInt(row, "NUMOFFERS"),
            ValueToday = table.GetDouble(row, "VALTODAY"),
            VolumeToday = table.GetDouble(row, "VOLTODAY"),
            NumTrades = table.GetInt(row, "NUMTRADES"),
            EffectiveYield = yieldData.EffectiveYield,
            EffectiveYieldAtWeightedAveragePrice = yieldData.EffectiveYieldAtWeightedAveragePrice,
            Duration = yieldData.Duration,
            DurationAtWeightedAveragePrice = yieldData.DurationAtWeightedAveragePrice,
            ZSpread = table.GetDouble(row, "ZSPREAD"),
            ZSpreadAtWeightedAveragePrice = table.GetDouble(row, "ZSPREADATWAPRICE"),
            ZSpreadBp = yieldData.ZSpreadBp,
            GSpreadBp = yieldData.GSpreadBp,
            ImpliedFloatingRate = table.GetDouble(row, "IRICPICLOSE"),
            ImpliedInflation = table.GetDouble(row, "BEICLOSE"),
            ImpliedCbrRate = table.GetDouble(row, "CBRCLOSE"),
            IsProvisional = true
        };
    }

    private static IReadOnlyList<OfzIssue> DistinctIssues(IEnumerable<OfzIssue> issues)
    {
        return issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToList();
    }

    private static double? NullIfZero(double? value)
    {
        return value is 0 ? null : value;
    }

    private readonly record struct CurrentYieldData(
        double? EffectiveYield,
        double? EffectiveYieldAtWeightedAveragePrice,
        double? Duration,
        double? DurationAtWeightedAveragePrice,
        double? ZSpreadBp,
        double? GSpreadBp);
}
