using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace CurveAnalyzer.Charts
{
    public class Periods : ObservableObject, IDataErrorInfo
    {
        private double period1;
        private double period2;

        public double Period1
        {
            get => period1;
            set => SetProperty(ref period1, value);
        }
        public double Period2
        {
            get => period2;
            set => SetProperty(ref period2, value);
        }

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
