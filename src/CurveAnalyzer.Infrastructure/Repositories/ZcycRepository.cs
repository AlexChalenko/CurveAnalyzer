using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;
using Microsoft.EntityFrameworkCore;

namespace CurveAnalyzer.Infrastructure.Repositories;

public class ZcycRepository : IZcycRepository
{
    private readonly MoexContext _context;

    public ZcycRepository(MoexContext context)
    {
        _context = context;
    }

    public async Task<ZcycData> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var dbData = await _context.Zcycs
            .Where(r => r.Tradedate.Equals(date))
            .ToListAsync(cancellationToken);

        return new ZcycData
        {
            Date = date,
            DataRow = new(dbData.Select(r => new ZcycDataRow(r.Period, r.Value)))
        };
    }

    public async Task<IReadOnlyList<DateTime>> GetAllDatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Zcycs
            .Select(data => data.Tradedate)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<double>> GetPeriodsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Zcycs
            .Select(data => data.Period)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AddRangeAsync(IEnumerable<Zcyc> newData, CancellationToken cancellationToken = default)
    {
        await _context.Zcycs.AddRangeAsync(newData, cancellationToken);
        var res = await _context.SaveChangesAsync(cancellationToken);
        return res > 0;
    }

    public async Task<IReadOnlyList<Zcyc>> GetDataForPeriodAsync(double period, CancellationToken cancellationToken = default)
    {
        return await _context.Zcycs
            .Where(p => p.Period == period)
            .ToListAsync(cancellationToken);
    }
}
