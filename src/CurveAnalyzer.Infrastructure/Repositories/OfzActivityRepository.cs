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
        var existingIssueIds = await context.OfzIssues
            .Where(issue => secIds.Contains(issue.SecId))
            .Select(issue => issue.SecId)
            .ToListAsync(cancellationToken);
        var existingIssueIdSet = existingIssueIds.ToHashSet(StringComparer.Ordinal);

        foreach (var issue in distinctIssues)
        {
            if (existingIssueIdSet.Contains(issue.SecId))
            {
                context.OfzIssues.Update(issue);
            }
            else
            {
                await context.OfzIssues.AddAsync(issue, cancellationToken).ConfigureAwait(false);
            }
        }
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
