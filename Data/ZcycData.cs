using System;
using System.Collections.Generic;

namespace CurveAnalyzer.Data;

public class ZcycData
{
    public DateTime Date { get; set; }
    public List<ZcycDataRow> DataRow { get; set; }

    public ZcycData()
    {
        DataRow = [];
    }
}

public class ZcycDataRow
{
    public double Period { get; set; }
    public double Value { get; set; }
}
