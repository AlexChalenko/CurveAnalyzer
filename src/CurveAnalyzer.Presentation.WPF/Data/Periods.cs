using System.ComponentModel;
using System.IO.Packaging;

namespace CurveAnalyzer.Presentation.WPF.Data;

public class Periods : IDataErrorInfo
{
    private double period1;
    private double period2;
    private string _error = string.Empty;

    public string Error => _error;

    public double Period2 { get => period2; set => period2 = value; }
    public double Period1 { get => period1; set => period1 = value; }

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

    public bool IsEmpty => period1 == default || period2 == default;
    //partial void OnPeriod1Changed(double value)
    //{

    //}

    ////private void OnPeriod2Changed(double value)
    ////{
    ////}
}
