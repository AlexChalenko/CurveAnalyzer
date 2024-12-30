using System.Xml.Serialization;

namespace CurveAnalyzer.ApiService.Data;


// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[System.Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false, ElementName = "document")]
public partial class IssData
{

    private documentData dataField;

    /// <remarks/>
    public documentData data
    {
        get => dataField;
        set => dataField = value;
    }
}

/// <remarks/>
[System.Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class documentData
{

    private documentDataRow[] rowsField;

    private string idField;

    /// <remarks/>
    [XmlArrayItem("row", IsNullable = false)]
    public documentDataRow[] rows
    {
        get => rowsField;
        set => rowsField = value;
    }

    /// <remarks/>
    [XmlAttribute()]
    public string id
    {
        get => idField;
        set => idField = value;
    }
}

/// <remarks/>
[System.Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class documentDataRow
{

    private System.DateTime tradedateField;

    private System.DateTime tradetimeField;

    private double periodField;

    private double valueField;

    /// <remarks/>
    [XmlAttribute(DataType = "date")]
    public System.DateTime Tradedate
    {
        get => tradedateField;
        set => tradedateField = value;
    }

    /// <remarks/>
    [XmlAttribute(DataType = "time")]
    public System.DateTime tradetime
    {
        get => tradetimeField;
        set => tradetimeField = value;
    }

    /// <remarks/>
    [XmlAttribute()]
    public double period
    {
        get => periodField;
        set => periodField = value;
    }

    /// <remarks/>
    [XmlAttribute()]
    public double value
    {
        get => valueField;
        set => valueField = value;
    }
}
