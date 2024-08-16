using System.Threading.Tasks;
using CurveAnalyzer.Data;

namespace CurveAnalyzer.Interfaces
{
    public interface IHistoryDataProvider : IDataProvider
    {
        Task<bool> SaveData(ZcycData data);
    }
}
