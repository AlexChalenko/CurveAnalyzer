using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;
using Microsoft.EntityFrameworkCore;

namespace CurveAnalyzer.Infrastructure.Repositories;

public sealed class OfzActivityRepository(IDbContextFactory<MoexContext> contextFactory) : IOfzActivityRepository
{
    public async Task<IReadOnlySet<DateTime>> GetLoadedDatesAsync(
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var dates = await context.OfzActivityLoadStates
            .Where(state =>
                state.BoardId == boardId &&
                state.TradeDate >= startDate.Date &&
                state.TradeDate <= endDate.Date &&
                state.Status != OfzActivityLoadStatus.Failed &&
                !state.IsProvisional)
            .Select(state => state.TradeDate.Date)
            .ToListAsync(cancellationToken);

        return dates.ToHashSet();
    }

    public async Task SaveDailyDataAsync(OfzActivityDailyData data, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await SaveIssuesAsync(context, data.Issues, cancellationToken).ConfigureAwait(false);

        await context.OfzDailyTrades
            .Where(trade => trade.BoardId == data.BoardId && trade.TradeDate == data.TradeDate.Date)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await context.OfzActivityLoadStates
            .Where(state => state.BoardId == data.BoardId && state.TradeDate == data.TradeDate.Date)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await SaveLiquiditySnapshotsAsync(context, data, cancellationToken).ConfigureAwait(false);

        var trades = data.Trades
            .Where(trade => !string.IsNullOrWhiteSpace(trade.SecId))
            .GroupBy(trade => new { trade.BoardId, trade.SecId, trade.TradeDate })
            .Select(group => group.Last())
            .ToList();

        await context.OfzDailyTrades.AddRangeAsync(trades, cancellationToken).ConfigureAwait(false);
        await context.OfzActivityLoadStates.AddAsync(data.LoadState, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveLoadStateAsync(OfzActivityLoadState state, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await context.OfzActivityLoadStates
            .Where(existing => existing.BoardId == state.BoardId && existing.TradeDate == state.TradeDate.Date)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await context.OfzActivityLoadStates.AddAsync(state, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OfzDailyTrade>> GetTradesAsync(
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.OfzDailyTrades
            .Where(trade =>
                trade.BoardId == boardId &&
                trade.TradeDate >= startDate.Date &&
                trade.TradeDate <= endDate.Date)
            .OrderBy(trade => trade.SecId)
            .ThenBy(trade => trade.TradeDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfzDailyTrade>> GetIssueTradesAsync(
        string secId,
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secId))
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.OfzDailyTrades
            .AsNoTracking()
            .Where(trade =>
                trade.BoardId == boardId &&
                trade.SecId == secId &&
                trade.TradeDate >= startDate.Date &&
                trade.TradeDate <= endDate.Date)
            .OrderBy(trade => trade.TradeDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfzLiquiditySnapshot>> GetLiquiditySnapshotsAsync(
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.OfzLiquiditySnapshots
            .AsNoTracking()
            .Where(snapshot =>
                snapshot.BoardId == boardId &&
                snapshot.TradeDate >= startDate.Date &&
                snapshot.TradeDate <= endDate.Date)
            .OrderBy(snapshot => snapshot.SecId)
            .ThenBy(snapshot => snapshot.TradeDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfzLiquiditySnapshot>> GetIssueLiquiditySnapshotsAsync(
        string secId,
        DateTime startDate,
        DateTime endDate,
        string boardId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secId))
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.OfzLiquiditySnapshots
            .AsNoTracking()
            .Where(snapshot =>
                snapshot.BoardId == boardId &&
                snapshot.SecId == secId &&
                snapshot.TradeDate >= startDate.Date &&
                snapshot.TradeDate <= endDate.Date)
            .OrderBy(snapshot => snapshot.TradeDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfzDailyTrade>> GetBaselineTradesAsync(
        DateTime beforeDate,
        int recordsPerIssue,
        string boardId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var trades = await context.OfzDailyTrades
            .Where(trade => trade.BoardId == boardId && trade.TradeDate < beforeDate.Date)
            .OrderByDescending(trade => trade.TradeDate)
            .ToListAsync(cancellationToken);

        return trades
            .GroupBy(trade => trade.SecId, StringComparer.Ordinal)
            .SelectMany(group => group.Take(recordsPerIssue))
            .OrderBy(trade => trade.SecId, StringComparer.Ordinal)
            .ThenBy(trade => trade.TradeDate)
            .ToList();
    }

    public async Task<IReadOnlyList<OfzIssue>> GetIssuesAsync(
        IReadOnlyCollection<string> secIds,
        CancellationToken cancellationToken = default)
    {
        if (secIds.Count == 0)
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.OfzIssues
            .Where(issue => secIds.Contains(issue.SecId))
            .OrderBy(issue => issue.SecId)
            .ToListAsync(cancellationToken);
    }

    private async Task SaveIssuesAsync(
        MoexContext context,
        IEnumerable<OfzIssue> issues,
        CancellationToken cancellationToken)
    {
        var distinctIssues = issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToList();

        if (distinctIssues.Count == 0)
        {
            return;
        }

        var secIds = distinctIssues.Select(issue => issue.SecId).ToList();
        var existingIssues = await context.OfzIssues
            .Where(issue => secIds.Contains(issue.SecId))
            .ToDictionaryAsync(issue => issue.SecId, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        foreach (var issue in distinctIssues)
        {
            EnsureClassificationSource(issue);
            var classification = OfzIssueClassifier.Classify(issue, issue.ClassificationSource);
            var loadedAt = issue.MetadataLoadedAt ?? DateTime.UtcNow;

            if (existingIssues.TryGetValue(issue.SecId, out var existingIssue))
            {
                MergeIssueMetadata(existingIssue, issue);
                var hasIncomingClassification = classification.Reliability != OfzClassificationReliability.Unknown;
                var hasStoredClassification =
                    existingIssue.NormalizedCouponType.HasValue ||
                    existingIssue.ClassificationReliability.HasValue;

                if (hasIncomingClassification && ShouldReplaceClassification(existingIssue, classification))
                {
                    existingIssue.ApplyClassification(classification, loadedAt);
                }
                else if (!hasStoredClassification)
                {
                    var existingClassification = OfzIssueClassifier.Classify(
                        existingIssue,
                        existingIssue.ClassificationSource);

                    if (ShouldReplaceClassification(existingIssue, existingClassification))
                    {
                        existingIssue.ApplyClassification(existingClassification, loadedAt);
                    }
                }
            }
            else
            {
                issue.ApplyClassification(classification, loadedAt);
                await context.OfzIssues.AddAsync(issue, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void EnsureClassificationSource(OfzIssue issue)
    {
        if (string.IsNullOrWhiteSpace(issue.ClassificationSource))
        {
            issue.ClassificationSource = OfzIssueClassificationSources.Unknown;
        }
    }

    private static void MergeIssueMetadata(OfzIssue target, OfzIssue incoming)
    {
        target.ShortName = MergeRequiredText(target.ShortName, incoming.ShortName, incoming.SecId);
        target.SecName = MergeText(target.SecName, incoming.SecName);
        target.IssueName = MergeText(target.IssueName, incoming.IssueName);
        target.Isin = MergeText(target.Isin, incoming.Isin);
        target.MatDate = incoming.MatDate ?? target.MatDate;
        target.FaceValue = incoming.FaceValue ?? target.FaceValue;
        target.InitialFaceValue = incoming.InitialFaceValue ?? target.InitialFaceValue;
        target.FaceUnit = MergeText(target.FaceUnit, incoming.FaceUnit);
        target.CurrencyId = MergeText(target.CurrencyId, incoming.CurrencyId);
        target.CouponPercent = incoming.CouponPercent ?? target.CouponPercent;
        target.CouponValue = incoming.CouponValue ?? target.CouponValue;
        target.CouponPeriod = incoming.CouponPeriod ?? target.CouponPeriod;
        target.NextCouponDate = incoming.NextCouponDate ?? target.NextCouponDate;
        target.ListLevel = incoming.ListLevel ?? target.ListLevel;
        target.IssueSize = incoming.IssueSize ?? target.IssueSize;
        target.IssueSizePlaced = incoming.IssueSizePlaced ?? target.IssueSizePlaced;
        target.MetadataLoadedAt = incoming.MetadataLoadedAt ?? target.MetadataLoadedAt;
        target.BondType = MergeText(target.BondType, incoming.BondType);
        target.BondSubType = MergeText(target.BondSubType, incoming.BondSubType);
    }

    private static bool ShouldReplaceClassification(OfzIssue existingIssue, OfzIssueClassification classification)
    {
        var existingRank = GetReliabilityRank(existingIssue.ClassificationReliability);
        var newRank = GetReliabilityRank(classification.Reliability);

        return newRank > existingRank ||
            (newRank == existingRank && classification.Reliability != OfzClassificationReliability.Unknown) ||
            !existingIssue.NormalizedCouponType.HasValue && !existingIssue.ClassificationReliability.HasValue;
    }

    private static int GetReliabilityRank(OfzClassificationReliability? reliability)
    {
        return reliability switch
        {
            OfzClassificationReliability.Reliable => 4,
            OfzClassificationReliability.Inferred => 3,
            OfzClassificationReliability.Conflict => 2,
            OfzClassificationReliability.Unknown => 1,
            _ => 0
        };
    }

    private static string MergeRequiredText(string current, string incoming, string fallbackValue)
    {
        if (string.IsNullOrWhiteSpace(incoming))
        {
            return current;
        }

        var trimmed = incoming.Trim();
        if (trimmed.Equals(fallbackValue, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(current) &&
            !current.Equals(fallbackValue, StringComparison.Ordinal))
        {
            return current;
        }

        return trimmed;
    }

    private static string? MergeText(string? current, string? incoming)
    {
        return string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();
    }

    private static async Task SaveLiquiditySnapshotsAsync(
        MoexContext context,
        OfzActivityDailyData data,
        CancellationToken cancellationToken)
    {
        var snapshots = data.LiquiditySnapshots
            .Where(snapshot =>
                snapshot.BoardId == data.BoardId &&
                snapshot.TradeDate == data.TradeDate.Date &&
                !string.IsNullOrWhiteSpace(snapshot.SecId))
            .GroupBy(snapshot => new { snapshot.BoardId, snapshot.SecId, snapshot.TradeDate })
            .Select(group => group.Last())
            .ToList();

        if (snapshots.Count > 0)
        {
            await context.OfzLiquiditySnapshots
                .Where(snapshot => snapshot.BoardId == data.BoardId && snapshot.TradeDate == data.TradeDate.Date)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            await context.OfzLiquiditySnapshots.AddRangeAsync(snapshots, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!data.LoadState.IsProvisional)
        {
            await context.OfzLiquiditySnapshots
                .Where(snapshot =>
                    snapshot.BoardId == data.BoardId &&
                    snapshot.TradeDate == data.TradeDate.Date &&
                    snapshot.IsProvisional)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
