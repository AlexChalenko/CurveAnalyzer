namespace CurveAnalyzer.Core;

internal sealed class CbrKeyRateLookup
{
    private readonly CbrKeyRate[] keyRates;

    private CbrKeyRateLookup(IEnumerable<CbrKeyRate>? keyRates)
    {
        this.keyRates = keyRates?
            .Where(rate => double.IsFinite(rate.Rate))
            .GroupBy(rate => rate.Date.Date)
            .Select(group => group.OrderByDescending(rate => rate.LoadedAt).First())
            .OrderBy(rate => rate.Date)
            .ToArray() ?? [];
    }

    public static CbrKeyRateLookup Create(IEnumerable<CbrKeyRate>? keyRates) => new(keyRates);

    public CbrKeyRate? GetLatestOnOrBefore(DateTime date)
    {
        if (keyRates.Length == 0)
        {
            return null;
        }

        var target = date.Date;
        var left = 0;
        var right = keyRates.Length - 1;
        var bestIndex = -1;

        while (left <= right)
        {
            var middle = left + ((right - left) / 2);
            var candidateDate = keyRates[middle].Date.Date;
            if (candidateDate <= target)
            {
                bestIndex = middle;
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        return bestIndex >= 0 ? keyRates[bestIndex] : null;
    }
}
