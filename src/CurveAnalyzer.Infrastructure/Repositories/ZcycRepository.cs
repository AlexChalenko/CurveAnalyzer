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

    public Task<ZcycData> GetByDateAsync(DateTime date)
    {
        List<Zcyc> dbData = [.. _context.Zcycs.Where(r => r.Tradedate.Equals(date))];

        return Task.FromResult(
            new ZcycData
            {
                Date = date,
                DataRow = new(dbData.Select(r => new ZcycDataRow(r.Period, r.Value)))
            });
    }

    public Task<IEnumerable<DateTime>> GetAllDatesAsync()
    {
        return Task.FromResult(_context.Zcycs.Select(_ => _.Tradedate).Distinct().AsEnumerable());
    }

    public Task<IEnumerable<double>> GetPeriodsAsync()
    {
        return Task.FromResult(_context.Zcycs.Select(_ => _.Period).Distinct().AsEnumerable());
    }

    public async Task<bool> AddRangeAsync(IEnumerable<Zcyc> newData)
    {
        await _context.Zcycs.AddRangeAsync(newData);
        var res = await _context.SaveChangesAsync();
        return res > 0;
    }

    public Task<IEnumerable<Zcyc>> GetDataForPeriod(double period)
    {
        return Task.FromResult(_context.Zcycs.Where(p => p.Period == period).AsEnumerable());
    }
}
