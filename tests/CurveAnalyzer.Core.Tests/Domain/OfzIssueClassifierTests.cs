using CurveAnalyzer.Core;
using CurveAnalyzer.Core.Tests.Fixtures;

namespace CurveAnalyzer.Core.Tests.Domain;

public class OfzIssueClassifierTests
{
    [Theory]
    [MemberData(nameof(OfzIssueMetadataFixtures.KnownTypes), MemberType = typeof(OfzIssueMetadataFixtures))]
    public void Classify_ReturnsExpectedKnownType(
        OfzIssue issue,
        OfzCouponType expectedType,
        string expectedMarker,
        OfzClassificationReliability expectedReliability)
    {
        var classification = OfzIssueClassifier.Classify(issue);

        Assert.Equal(expectedType, classification.CouponType);
        Assert.Equal(expectedMarker, classification.CouponTypeMarker);
        Assert.Equal(expectedReliability, classification.Reliability);
        Assert.Equal(issue.ClassificationSource, classification.Source);
        Assert.False(string.IsNullOrWhiteSpace(classification.Evidence));
    }

    [Fact]
    public void Classify_ReturnsUnknownWhenEvidenceIsMissing()
    {
        var classification = OfzIssueClassifier.Classify(OfzIssueMetadataFixtures.Unknown);

        Assert.Equal(OfzCouponType.Unknown, classification.CouponType);
        Assert.Equal("Unknown", classification.CouponTypeMarker);
        Assert.Equal(OfzClassificationReliability.Unknown, classification.Reliability);
        Assert.Contains(classification.Limitations, item => item.Contains("No reliable", StringComparison.Ordinal));
    }

    [Fact]
    public void Classify_UsesMetadataFieldsSourceWhenReliableEvidenceHasNoStoredSource()
    {
        var issue = new OfzIssue
        {
            SecId = "SU26212RMFS9",
            ShortName = "ОФЗ-ПД 26212",
            FaceUnit = "SUR"
        };

        var classification = OfzIssueClassifier.Classify(issue);

        Assert.Equal(OfzCouponType.Fixed, classification.CouponType);
        Assert.Equal(OfzClassificationReliability.Reliable, classification.Reliability);
        Assert.Equal(OfzIssueClassificationSources.MetadataFields, classification.Source);
        Assert.Contains("source=metadata-fields", classification.Evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void Classify_ReturnsConflictWhenReliableEvidenceDisagrees()
    {
        var classification = OfzIssueClassifier.Classify(OfzIssueMetadataFixtures.Conflicting);

        Assert.Equal(OfzCouponType.Unknown, classification.CouponType);
        Assert.Equal("Unknown", classification.CouponTypeMarker);
        Assert.Equal(OfzClassificationReliability.Conflict, classification.Reliability);
        Assert.Contains(classification.Limitations, item => item.Contains("Conflicting", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("SU26238RMFS4", OfzCouponType.Fixed, "ОФЗ-ПД")]
    [InlineData("SU29019RMFS5", OfzCouponType.Floating, "ОФЗ-ПК")]
    [InlineData("SU52003RMFS9", OfzCouponType.InflationLinked, "ОФЗ-ИН")]
    [InlineData("SU46023RMFS6", OfzCouponType.Amortized, "ОФЗ-АД")]
    public void Classify_InfersLegacyCachedRowsFromIssueSeries(
        string secId,
        OfzCouponType expectedType,
        string expectedMarker)
    {
        var issue = new OfzIssue
        {
            SecId = secId,
            ShortName = secId,
            FaceUnit = "SUR"
        };

        var classification = OfzIssueClassifier.Classify(issue);

        Assert.Equal(expectedType, classification.CouponType);
        Assert.Equal(expectedMarker, classification.CouponTypeMarker);
        Assert.Equal(OfzClassificationReliability.Inferred, classification.Reliability);
        Assert.Equal(OfzIssueClassificationSources.SeriesFallback, classification.Source);
    }

    [Fact]
    public void Classify_DetectsIndexedNominalAndAmortizingFlags()
    {
        var inflationLinked = new OfzIssue
        {
            SecId = "SU52003RMFS9",
            ShortName = "ОФЗ-ИН 52003",
            FaceUnit = "SUR",
            BondType = "Линкер"
        };
        var amortized = new OfzIssue
        {
            SecId = "SU46023RMFS6",
            ShortName = "ОФЗ-АД 46023",
            FaceUnit = "SUR",
            BondType = "Амортизируемые облигации"
        };

        Assert.True(OfzIssueClassifier.Classify(inflationLinked).IsIndexedNominal);
        Assert.True(OfzIssueClassifier.Classify(amortized).IsAmortizing);
    }
}
