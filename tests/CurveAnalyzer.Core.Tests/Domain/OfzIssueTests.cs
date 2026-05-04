using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Domain;

public class OfzIssueTests
{
    [Fact]
    public void CouponType_ClassifiesFixedCouponOfz()
    {
        var issue = new OfzIssue
        {
            SecId = "SU26238RMFS4",
            ShortName = "ОФЗ 26238",
            SecName = "ОФЗ-ПД 26238 15/05/2041",
            FaceUnit = "SUR",
            BondType = "Фикс с известным купоном"
        };

        Assert.Equal(OfzCouponType.Fixed, issue.CouponType);
        Assert.Equal("ОФЗ-ПД", issue.CouponTypeMarker);
        Assert.Equal("RUB / ОФЗ-ПД", issue.DisplayMarker);
    }

    [Fact]
    public void CouponType_ClassifiesFloatingCouponOfz()
    {
        var issue = new OfzIssue
        {
            SecId = "SU29019RMFS5",
            ShortName = "ОФЗ 29019",
            IssueName = "облигации федерального займа с переменным купонным доходом",
            FaceUnit = "SUR",
            BondType = "Флоатер"
        };

        Assert.Equal(OfzCouponType.Floating, issue.CouponType);
        Assert.Equal("ОФЗ-ПК", issue.CouponTypeMarker);
    }

    [Theory]
    [InlineData("SU52003RMFS9", "ОФЗ-ИН 52003", "Линкер/облигации с индексируемым", "ОФЗ-ИН", OfzCouponType.InflationLinked)]
    [InlineData("SU46023RMFS6", "ОФЗ-АД 46023", "Амортизируемые облигации", "ОФЗ-АД", OfzCouponType.Amortized)]
    public void CouponType_ClassifiesNonStandardRubleOfz(
        string secId,
        string secName,
        string bondType,
        string marker,
        OfzCouponType expectedType)
    {
        var issue = new OfzIssue
        {
            SecId = secId,
            ShortName = secName,
            SecName = secName,
            FaceUnit = "SUR",
            BondType = bondType
        };

        Assert.Equal(expectedType, issue.CouponType);
        Assert.Equal(marker, issue.CouponTypeMarker);
    }

    [Fact]
    public void CouponType_ClassifiesCurrencyOfzByFaceUnit()
    {
        var issue = new OfzIssue
        {
            SecId = "RU000A10DQA8",
            ShortName = "ОФЗ 33 CNY",
            FaceUnit = "CNY",
            CurrencyId = "SUR",
            BondType = "Валютные облигации"
        };

        Assert.Equal(OfzCouponType.Currency, issue.CouponType);
        Assert.Equal("CNY / Валютная", issue.DisplayMarker);
    }

    [Theory]
    [InlineData("SU26238RMFS4", "ОФЗ 26238", OfzCouponType.Fixed, "ОФЗ-ПД")]
    [InlineData("SU29019RMFS5", "ОФЗ 29019", OfzCouponType.Floating, "ОФЗ-ПК")]
    [InlineData("SU52003RMFS9", "ОФЗ 52003", OfzCouponType.InflationLinked, "ОФЗ-ИН")]
    [InlineData("SU46023RMFS6", "ОФЗ 46023", OfzCouponType.Amortized, "ОФЗ-АД")]
    public void CouponType_ClassifiesOldCachedRowsByIssueSeries(
        string secId,
        string shortName,
        OfzCouponType expectedType,
        string expectedMarker)
    {
        var issue = new OfzIssue
        {
            SecId = secId,
            ShortName = shortName,
            FaceUnit = "SUR"
        };

        Assert.Equal(expectedType, issue.CouponType);
        Assert.Equal(expectedMarker, issue.CouponTypeMarker);
    }
}
