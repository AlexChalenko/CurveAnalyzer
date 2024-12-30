using Microsoft.EntityFrameworkCore;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Infrastructure;

public class MoexContext : DbContext
{
    public DbSet<Zcyc> Zcycs { get; set; }

    public MoexContext(DbContextOptions<MoexContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Zcyc>()
            .HasKey(z => z.Num);
    }

}


