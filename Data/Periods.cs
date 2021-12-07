using System.ComponentModel;

namespace CurveAnalyzer.Charts
{
    public struct Periods : IDataErrorInfo
    {
        public double Period1 { get; set; }
        public double Period2 { get; set; }

        public string Error => throw new System.NotImplementedException();
        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Period1):
                    case nameof(Period2):
                        if (Period1.Equals(Period2))
                            return "Period should be defferent";
                        break;

                    default:
                        break;
                }
                return string.Empty;
            }
        }
    }
}
