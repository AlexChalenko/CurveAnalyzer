using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Interfaces;

public interface IHistoryDataService : IDataService
{
    Task<bool> SaveData(ZcycData data);
}
