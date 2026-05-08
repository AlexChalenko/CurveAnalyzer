using System.Text.Json;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.ApiServices.Data;

internal static class OfzIndexIssData
{
    public const string HistoryColumns =
        "SECID,TRADEDATE,SHORTNAME,NAME,OPEN,HIGH,LOW,CLOSE,VALUE,DURATION,YIELD,CURRENCYID,TRADE_SESSION_DATE,TRADESESSIONDATE,RECALC_DATE";

    public static OfzMarketIndexPoint? MapHistoryPoint(
        IssJsonTable table,
        JsonElement row,
        string fallbackSecId,
        DateTime loadedAt)
    {
        var secId = table.GetString(row, "SECID") ?? fallbackSecId;
        var tradeDate = table.GetDate(row, "TRADEDATE");
        if (string.IsNullOrWhiteSpace(secId) || !tradeDate.HasValue)
        {
            return null;
        }

        return new OfzMarketIndexPoint
        {
            SecId = secId,
            TradeDate = tradeDate.Value.Date,
            ShortName = table.GetString(row, "SHORTNAME"),
            Name = table.GetString(row, "NAME"),
            Open = table.GetDouble(row, "OPEN"),
            High = table.GetDouble(row, "HIGH"),
            Low = table.GetDouble(row, "LOW"),
            Close = table.GetDouble(row, "CLOSE"),
            Value = table.GetDouble(row, "VALUE"),
            Duration = NullIfZero(table.GetDouble(row, "DURATION")),
            Yield = NullIfZero(table.GetDouble(row, "YIELD")),
            CurrencyId = table.GetString(row, "CURRENCYID"),
            SourceKind = OfzMarketIndexSourceKind.History,
            TradeSessionDate = table.GetDate(row, "TRADE_SESSION_DATE") ?? table.GetDate(row, "TRADESESSIONDATE"),
            RecalcDate = table.GetDate(row, "RECALC_DATE"),
            LoadedAt = loadedAt
        };
    }

    public static OfzMarketIndexPoint? MapSnapshotPoint(
        IssJsonTable marketData,
        JsonElement marketDataRow,
        IssJsonTable? securities,
        string fallbackSecId,
        DateTime loadedAt)
    {
        var secId = marketData.GetString(marketDataRow, "SECID") ?? fallbackSecId;
        if (string.IsNullOrWhiteSpace(secId))
        {
            return null;
        }

        var securityRow = securities?.Rows
            .FirstOrDefault(row => string.Equals(securities.GetString(row, "SECID"), secId, StringComparison.Ordinal));
        var tradeDate =
            marketData.GetDate(marketDataRow, "TRADEDATE") ??
            marketData.GetDate(marketDataRow, "TRADINGSESSIONDATE") ??
            loadedAt.Date;

        return new OfzMarketIndexPoint
        {
            SecId = secId,
            TradeDate = tradeDate.Date,
            ShortName = GetSecurityString(securities, securityRow, "SHORTNAME") ?? secId,
            Name = GetSecurityString(securities, securityRow, "NAME") ??
                GetSecurityString(securities, securityRow, "SECNAME"),
            Open = Coalesce(
                marketData.GetDouble(marketDataRow, "OPENVALUE"),
                marketData.GetDouble(marketDataRow, "OPEN")),
            High = Coalesce(
                marketData.GetDouble(marketDataRow, "HIGHVALUE"),
                marketData.GetDouble(marketDataRow, "HIGH")),
            Low = Coalesce(
                marketData.GetDouble(marketDataRow, "LOWVALUE"),
                marketData.GetDouble(marketDataRow, "LOW")),
            Close = Coalesce(
                marketData.GetDouble(marketDataRow, "CURRENTVALUE"),
                marketData.GetDouble(marketDataRow, "LASTVALUE"),
                marketData.GetDouble(marketDataRow, "LAST")),
            Value = Coalesce(
                marketData.GetDouble(marketDataRow, "VALTODAY"),
                marketData.GetDouble(marketDataRow, "VALUE")),
            Duration = NullIfZero(marketData.GetDouble(marketDataRow, "DURATION")),
            Yield = NullIfZero(marketData.GetDouble(marketDataRow, "YIELD")),
            CurrencyId = GetSecurityString(securities, securityRow, "CURRENCYID") ??
                marketData.GetString(marketDataRow, "CURRENCYID"),
            SourceKind = OfzMarketIndexSourceKind.Snapshot,
            ObservedAt = loadedAt,
            TradeSessionDate = marketData.GetDate(marketDataRow, "TRADE_SESSION_DATE") ??
                marketData.GetDate(marketDataRow, "TRADINGSESSIONDATE"),
            LoadedAt = loadedAt,
            IsProvisional = true
        };
    }

    private static string? GetSecurityString(
        IssJsonTable? table,
        JsonElement? row,
        string column)
    {
        return table is null || row is null ? null : table.GetString(row.Value, column);
    }

    private static double? Coalesce(params double?[] values)
    {
        return values.FirstOrDefault(value => value.HasValue);
    }

    private static double? NullIfZero(double? value)
    {
        return value is 0 ? null : value;
    }
}
