using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Domain;

public class CurvePeriodTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsInvalidValue(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CurvePeriod(value));
    }

    [Fact]
    public void Constructor_AcceptsPositiveFiniteValue()
    {
        var period = new CurvePeriod(2.5);

        Assert.Equal(2.5, period.Value);
    }
}
