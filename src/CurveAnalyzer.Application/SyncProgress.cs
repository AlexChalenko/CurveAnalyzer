namespace CurveAnalyzer.Application;

public readonly record struct SyncProgress(int CompletedCount, int TotalCount, DateTime? CurrentDate)
{
    public double Ratio => TotalCount <= 0
        ? 1
        : Math.Clamp((double)CompletedCount / TotalCount, 0, 1);

    public static SyncProgress Completed { get; } = new(0, 0, null);
}
