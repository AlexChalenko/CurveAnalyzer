using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Fixtures;

public static class OfzIssueMetadataFixtures
{
    public static TheoryData<OfzIssue, OfzCouponType, string, OfzClassificationReliability> KnownTypes => new()
    {
        {
            new OfzIssue
            {
                SecId = "SU26238RMFS4",
                ShortName = "ОФЗ 26238",
                SecName = "ОФЗ-ПД 26238 15/05/2041",
                FaceUnit = "SUR",
                BondType = "Фикс с известным купоном",
                ClassificationSource = OfzIssueClassificationSources.History
            },
            OfzCouponType.Fixed,
            "ОФЗ-ПД",
            OfzClassificationReliability.Reliable
        },
        {
            new OfzIssue
            {
                SecId = "SU29019RMFS5",
                ShortName = "ОФЗ 29019",
                IssueName = "облигации федерального займа с переменным купонным доходом",
                FaceUnit = "SUR",
                BondType = "Флоатер",
                ClassificationSource = OfzIssueClassificationSources.History
            },
            OfzCouponType.Floating,
            "ОФЗ-ПК",
            OfzClassificationReliability.Reliable
        },
        {
            new OfzIssue
            {
                SecId = "SU52003RMFS9",
                ShortName = "ОФЗ 52003",
                SecName = "ОФЗ-ИН 52003",
                FaceUnit = "SUR",
                BondType = "Линкер",
                ClassificationSource = OfzIssueClassificationSources.History
            },
            OfzCouponType.InflationLinked,
            "ОФЗ-ИН",
            OfzClassificationReliability.Reliable
        },
        {
            new OfzIssue
            {
                SecId = "SU46023RMFS6",
                ShortName = "ОФЗ 46023",
                SecName = "ОФЗ-АД 46023",
                FaceUnit = "SUR",
                BondType = "Амортизируемые облигации",
                ClassificationSource = OfzIssueClassificationSources.History
            },
            OfzCouponType.Amortized,
            "ОФЗ-АД",
            OfzClassificationReliability.Reliable
        },
        {
            new OfzIssue
            {
                SecId = "RU000A10DQA8",
                ShortName = "ОФЗ 33 CNY",
                SecName = "ОФЗ 33 CNY",
                FaceUnit = "CNY",
                CurrencyId = "SUR",
                BondType = "Валютные облигации",
                ClassificationSource = OfzIssueClassificationSources.Snapshot
            },
            OfzCouponType.Currency,
            "Валютная",
            OfzClassificationReliability.Reliable
        }
    };

    public static OfzIssue Unknown => new()
    {
        SecId = "RU000UNKNOWN",
        ShortName = "UNKNOWN",
        FaceUnit = "SUR",
        ClassificationSource = OfzIssueClassificationSources.Snapshot
    };

    public static OfzIssue Conflicting => new()
    {
        SecId = "SU29019RMFS5",
        ShortName = "ОФЗ 29019",
        SecName = "ОФЗ-ПД 29019",
        IssueName = "облигации федерального займа с переменным купонным доходом",
        FaceUnit = "SUR",
        ClassificationSource = OfzIssueClassificationSources.History
    };
}
