using System.Text.Json;
using CurveAnalyzer.ApiServices.Data;
using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.ApiServices;

public sealed class OfzIndexOnlineDataService(HttpClient httpClient) : IOfzIndexDataService
{
    public async Task<IReadOnlyList<OfzMarketIndexPoint>> GetHistoryAsync(
        IReadOnlyCollection<string> secIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (secIds.Count == 0)
        {
            return [];
        }

        (startDate, endDate) = NormalizeRange(startDate, endDate);
        var loadedAt = DateTime.UtcNow;
        List<OfzMarketIndexPoint> points = [];

        foreach (var secId in NormalizeSecIds(secIds))
        {
            var start = 0;
            var total = 0;

            do
            {
                var url = CreateHistoryUrl(secId, startDate, endDate, start);
                await using var stream = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                var root = document.RootElement;
                var history = IssJsonTable.TryCreate(root, "history");

                if (history is not null)
                {
                    foreach (var row in history.Rows)
                    {
                        var point = OfzIndexIssData.MapHistoryPoint(history, row, secId, loadedAt);
                        if (point is not null)
                        {
                            points.Add(point);
                        }
                    }
                }

                var cursor = IssCursor.From(root, "history.cursor");
                total = cursor.Total;
                start = cursor.PageSize > 0 ? cursor.NextStart : total;
            }
            while (start < total);
        }

        return Distinct(points);
    }

    public async Task<IReadOnlyList<OfzMarketIndexPoint>> GetCurrentSnapshotAsync(
        IReadOnlyCollection<string> secIds,
        CancellationToken cancellationToken = default)
    {
        if (secIds.Count == 0)
        {
            return [];
        }

        var loadedAt = DateTime.UtcNow;
        List<OfzMarketIndexPoint> points = [];

        foreach (var secId in NormalizeSecIds(secIds))
        {
            var url = CreateSnapshotUrl(secId);
            await using var stream = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;
            var securities = IssJsonTable.TryCreate(root, "securities");
            var marketData = IssJsonTable.TryCreate(root, "marketdata");
            if (marketData is null)
            {
                continue;
            }

            foreach (var row in marketData.Rows)
            {
                var point = OfzIndexIssData.MapSnapshotPoint(marketData, row, securities, secId, loadedAt);
                if (point is not null)
                {
                    points.Add(point);
                }
            }
        }

        return Distinct(points);
    }

    private static string CreateHistoryUrl(string secId, DateTime startDate, DateTime endDate, int start)
    {
        return $"https://iss.moex.com/iss/history/engines/stock/markets/index/securities/{Uri.EscapeDataString(secId)}.json" +
            $"?from={startDate:yyyy-MM-dd}" +
            $"&till={endDate:yyyy-MM-dd}" +
            "&iss.only=history,history.cursor" +
            "&iss.meta=off" +
            $"&start={start}" +
            $"&history.columns={Uri.EscapeDataString(OfzIndexIssData.HistoryColumns)}";
    }

    private static string CreateSnapshotUrl(string secId)
    {
        return $"https://iss.moex.com/iss/engines/stock/markets/index/securities/{Uri.EscapeDataString(secId)}.json" +
            "?iss.only=securities,marketdata" +
            "&iss.meta=off";
    }

    private static IReadOnlyList<string> NormalizeSecIds(IEnumerable<string> secIds)
    {
        return secIds
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .Select(secId => secId.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(secId => secId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<OfzMarketIndexPoint> Distinct(IEnumerable<OfzMarketIndexPoint> points)
    {
        return points
            .Where(point => !string.IsNullOrWhiteSpace(point.SecId))
            .GroupBy(point => new { point.SecId, TradeDate = point.TradeDate.Date, point.SourceKind })
            .Select(group => group.OrderByDescending(point => point.LoadedAt).First())
            .OrderBy(point => point.SecId, StringComparer.Ordinal)
            .ThenBy(point => point.TradeDate)
            .ThenBy(point => point.SourceKind)
            .ToList();
    }

    private static (DateTime StartDate, DateTime EndDate) NormalizeRange(DateTime startDate, DateTime endDate)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;

        return startDate <= endDate
            ? (startDate, endDate)
            : (endDate, startDate);
    }
}
