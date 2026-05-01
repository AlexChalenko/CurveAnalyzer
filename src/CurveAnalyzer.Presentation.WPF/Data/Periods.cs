using System.ComponentModel;

namespace CurveAnalyzer.Presentation.WPF.Data;

public class Periods : IDataErrorInfo
{
    private double _period1;
    private double _period2;
    private string _error = string.Empty;

    public string Error => _error;

    public double Period2 { get => _period2; set => _period2 = value; }
    public double Period1 { get => _period1; set => _period1 = value; }

    public string this[string columnName]
    {
        get
        {
            switch (columnName)
            {
                case nameof(Period1):
                case nameof(Period2):
                    if (Period1.Equals(Period2))
                    {
                        _error = "Period should be defferent";
                    }
                    else
                    {
                        _error = string.Empty;
                    }
                    return Error;
                default:
                    break;
            }
            return string.Empty;
        }
    }

    public bool IsEmpty => _period1 == default || _period2 == default;
}
