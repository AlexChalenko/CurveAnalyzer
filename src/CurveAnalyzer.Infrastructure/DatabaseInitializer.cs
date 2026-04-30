using Microsoft.EntityFrameworkCore;

namespace CurveAnalyzer.Infrastructure;

public sealed class DatabaseInitializer(MoexContext context) : IDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
