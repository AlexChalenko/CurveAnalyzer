using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application.Interfaces;

public interface IHistoryDataService : IDataService
{
    Task<bool> SaveDataAsync(ZcycData data, CancellationToken cancellationToken = default);
}
