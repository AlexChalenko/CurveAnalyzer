namespace CurveAnalyzer.ApiServices.Data
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("document")]
    public class Document
    {
        [XmlElement("data")]
        public SecuritiesData Data { get; set; }
    }

    public class SecuritiesData
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("rows")]
        public SecuritiesRows Rows { get; set; }
    }

    public class SecuritiesRows
    {
        [XmlElement("row")]
        public List<Security> Securities { get; set; }
    }

    public class Security
    {
        [XmlAttribute("tradedate")]
        public DateTime TradeDate { get; set; }

        [XmlAttribute("tradetime")]
        public string TradeTime { get; set; }

        [XmlAttribute("secid")]
        public string SecurityId { get; set; }

        [XmlAttribute("benchmark")]
        public int Benchmark { get; set; }

        [XmlAttribute("expdate")]
        public DateTime ExpDate { get; set; }

        [XmlAttribute("updatetime")]
        public string UpdateTime { get; set; }

        [XmlAttribute("bidprice")]
        public double BidPrice { get; set; }

        [XmlAttribute("bidyield")]
        public double BidYield { get; set; }

        [XmlAttribute("askprice")]
        public double AskPrice { get; set; }

        [XmlAttribute("askyield")]
        public double AskYield { get; set; }

        [XmlAttribute("trdprice")]
        public double TradePrice { get; set; }

        [XmlAttribute("trdyield")]
        public double TradeYield { get; set; }

        [XmlAttribute("clcprice")]
        public double ClosingPrice { get; set; }

        [XmlAttribute("crtprice")]
        public double CurrentPrice { get; set; }

        [XmlAttribute("clcyield")]
        public double ClosingYield { get; set; }

        [XmlAttribute("correction")]
        public double Correction { get; set; }

        [XmlAttribute("crtyield")]
        public double CurrentYield { get; set; }

        [XmlAttribute("crtduration")]
        public int CurrentDuration { get; set; }

        [XmlAttribute("bidduration")]
        public int BidDuration { get; set; }

        [XmlAttribute("askduration")]
        public int AskDuration { get; set; }

        [XmlAttribute("shortname")]
        public string ShortName { get; set; }
    }

}
