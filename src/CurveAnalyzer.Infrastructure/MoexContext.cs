using CurveAnalyzer.Core;
using Microsoft.EntityFrameworkCore;

namespace CurveAnalyzer.Infrastructure;

public class MoexContext : DbContext
{
    public DbSet<Zcyc> Zcycs { get; set; }

    public MoexContext(DbContextOptions<MoexContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var zcyc = modelBuilder.Entity<Zcyc>();

        zcyc.HasKey(z => z.Num);

        zcyc.HasIndex(z => new { z.Tradedate, z.Period })
            .HasDatabaseName("IX_Zcycs_Tradedate_Period");

        zcyc.HasIndex(z => new { z.Period, z.Tradedate })
            .HasDatabaseName("IX_Zcycs_Period_Tradedate");
    }
}
