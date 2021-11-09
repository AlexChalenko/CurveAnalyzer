using System.Threading.Tasks;
using CurveAnalyzer.Data;

namespace CurveAnalyzer.Interfaces
{
    internal interface IHistoryDataProvider : IDataProvider
    {
        Task<bool> SaveData(ZcycData data);
    }
}
