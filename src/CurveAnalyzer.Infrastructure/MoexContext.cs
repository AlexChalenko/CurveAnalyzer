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
        modelBuilder.Entity<Zcyc>()
            .HasKey(z => z.Num);
    }

}

