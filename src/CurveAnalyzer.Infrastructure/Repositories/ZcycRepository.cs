using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;
using Microsoft.EntityFrameworkCore;

namespace CurveAnalyzer.Infrastructure.Repositories;

public class ZcycRepository : IZcycRepository
{
    private readonly IDbContextFactory<MoexContext> _contextFactory;

    public ZcycRepository(IDbContextFactory<MoexContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ZcycData> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var dbData = await context.Zcycs
            .Where(r => r.Tradedate.Equals(date))
            .OrderBy(r => r.Period)
            .ToListAsync(cancellationToken);

        return new ZcycData
        {
            Date = date,
            DataRow = new(dbData.Select(r => new ZcycDataRow(r.Period, r.Value)))
        };
    }

    public async Task<IReadOnlyList<DateTime>> GetAllDatesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Zcycs
            .Select(data => data.Tradedate)
            .Distinct()
            .OrderBy(date => date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<double>> GetPeriodsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Zcycs
            .Select(data => data.Period)
            .Distinct()
            .OrderBy(period => period)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AddRangeAsync(IEnumerable<Zcyc> newData, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        await context.Zcycs.AddRangeAsync(newData, cancellationToken);
        var res = await context.SaveChangesAsync(cancellationToken);
        return res > 0;
    }

    public async Task<IReadOnlyList<Zcyc>> GetDataForPeriodAsync(double period, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Zcycs
            .Where(p => p.Period == period)
            .OrderBy(p => p.Tradedate)
            .ToListAsync(cancellationToken);
    }
}
