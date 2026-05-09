using System.Text.Json;
using CurveAnalyzer.ApiServices.Data;
using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.ApiServices;

public sealed class OfzCashflowOnlineDataService(HttpClient httpClient) : IOfzCashflowDataService
{
    public async Task<IReadOnlyList<OfzCashflowEvent>> GetScheduleAsync(
        IReadOnlyCollection<string> secIds,
        CancellationToken cancellationToken = default)
    {
        if (secIds.Count == 0)
        {
            return [];
        }

        var loadedAt = DateTime.UtcNow;
        List<OfzCashflowEvent> events = [];

        foreach (var secId in NormalizeSecIds(secIds))
        {
            var url = CreateBondizationUrl(secId);
            await using var stream = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;

            AddRows(events, IssJsonTable.TryCreate(root, "coupons"), secId, loadedAt, OfzCashflowIssData.MapCoupon);
            AddRows(events, IssJsonTable.TryCreate(root, "amortizations"), secId, loadedAt, OfzCashflowIssData.MapAmortization);
            AddRows(events, IssJsonTable.TryCreate(root, "offers"), secId, loadedAt, OfzCashflowIssData.MapOffer);
            try
            {
                await AddSnapshotFallbackEventsAsync(events, secId, loadedAt, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Schedule rows are the primary source; snapshot fallback must not drop them.
            }
        }

        return Distinct(events);
    }

    private static void AddRows(
        ICollection<OfzCashflowEvent> events,
        IssJsonTable? table,
        string secId,
        DateTime loadedAt,
        Func<IssJsonTable, JsonElement, string, DateTime, OfzCashflowEvent?> map)
    {
        if (table is null)
        {
            return;
        }

        foreach (var row in table.Rows)
        {
            var cashflowEvent = map(table, row, secId, loadedAt);
            if (cashflowEvent is not null)
            {
                events.Add(cashflowEvent);
            }
        }
    }

    private static string CreateBondizationUrl(string secId)
    {
        return $"https://iss.moex.com/iss/statistics/engines/stock/markets/bonds/bondization/{Uri.EscapeDataString(secId)}.json" +
            "?iss.only=coupons,amortizations,offers" +
            "&iss.meta=off";
    }

    private async Task AddSnapshotFallbackEventsAsync(
        ICollection<OfzCashflowEvent> events,
        string secId,
        DateTime loadedAt,
        CancellationToken cancellationToken)
    {
        var url = CreateSnapshotUrl(secId);
        await using var stream = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var securities = IssJsonTable.TryCreate(document.RootElement, "securities");
        if (securities is null)
        {
            return;
        }

        foreach (var row in securities.Rows)
        {
            foreach (var cashflowEvent in OfzCashflowIssData.MapSnapshotFallbackEvents(securities, row, secId, loadedAt))
            {
                events.Add(cashflowEvent);
            }
        }
    }

    private static string CreateSnapshotUrl(string secId)
    {
        return $"https://iss.moex.com/iss/engines/stock/markets/bonds/boards/TQOB/securities/{Uri.EscapeDataString(secId)}.json" +
            "?iss.only=securities" +
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

    private static IReadOnlyList<OfzCashflowEvent> Distinct(IEnumerable<OfzCashflowEvent> events)
    {
        var distinctEvents = events
            .Where(item => !string.IsNullOrWhiteSpace(item.SecId))
            .Where(item => item.EventDate != default)
            .GroupBy(item => new { item.SecId, item.EventType, item.EventDate, item.SourceKind, item.SourceKey })
            .Select(group => group.OrderByDescending(item => item.LoadedAt).First())
            .ToList();
        var scheduleKeys = distinctEvents
            .Where(item => item.SourceKind == OfzCashflowSourceKind.Schedule)
            .Select(item => new { item.SecId, item.EventType, item.EventDate })
            .ToHashSet();

        return distinctEvents
            .Where(item =>
                item.SourceKind == OfzCashflowSourceKind.Schedule ||
                !scheduleKeys.Contains(new { item.SecId, item.EventType, item.EventDate }))
            .OrderBy(item => item.SecId, StringComparer.Ordinal)
            .ThenBy(item => item.EventDate)
            .ThenBy(item => item.EventType)
            .ThenBy(item => item.SourceKind)
            .ThenBy(item => item.SourceKey, StringComparer.Ordinal)
            .ToList();
    }
}
