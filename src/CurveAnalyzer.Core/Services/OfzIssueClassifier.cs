namespace CurveAnalyzer.Core;

public static class OfzIssueClassifier
{
    private static readonly string[] RubleCurrencyCodes = ["RUB", "RUR", "SUR"];

    public static OfzIssueClassification Classify(OfzIssue issue, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(issue);

        var sourceLabel = NormalizeSource(source ?? issue.ClassificationSource);
        var nominalCurrency = NormalizeCurrency(FirstNonEmpty(issue.FaceUnit, issue.CurrencyId));
        var text = BuildSearchText(issue);
        var reliable = new List<Candidate>();

        var hasReliableCurrencyNominal = !IsRubleCode(nominalCurrency) && nominalCurrency != "Unknown";
        if (hasReliableCurrencyNominal)
        {
            reliable.Add(new Candidate(OfzCouponType.Currency, "currency-nominal", $"currency={nominalCurrency}"));
        }
        else if (ContainsAny(text, "валют", "cny", "usd", "eur"))
        {
            var textCurrency = ExtractTextCurrency(text) ?? nominalCurrency;
            reliable.Add(new Candidate(OfzCouponType.Currency, "currency-text", $"currency={textCurrency}"));
            if (nominalCurrency == "Unknown" && textCurrency != "Unknown")
            {
                nominalCurrency = textCurrency;
            }
        }

        if (ContainsAny(text, "линкер", "индексируем", "офз-ин", "ofz-in"))
        {
            reliable.Add(new Candidate(OfzCouponType.InflationLinked, "source-linker", "marker=ОФЗ-ИН"));
        }

        if (ContainsAny(text, "амортиз", "офз-ад", "ofz-ad"))
        {
            reliable.Add(new Candidate(OfzCouponType.Amortized, "source-amortized", "marker=ОФЗ-АД"));
        }

        if (ContainsAny(text, "флоат", "переменн", "офз-пк", "ofz-pk"))
        {
            reliable.Add(new Candidate(OfzCouponType.Floating, "source-floating", "marker=ОФЗ-ПК"));
        }

        if (ContainsAny(text, "фикс", "постоянн", "офз-пд", "ofz-pd"))
        {
            reliable.Add(new Candidate(OfzCouponType.Fixed, "source-fixed", "marker=ОФЗ-ПД"));
        }

        var reliableTypes = reliable.Select(candidate => candidate.CouponType).Distinct().ToArray();
        if (reliableTypes.Length > 0)
        {
            var reliableSource = sourceLabel.Equals(OfzIssueClassificationSources.Unknown, StringComparison.OrdinalIgnoreCase)
                ? OfzIssueClassificationSources.MetadataFields
                : sourceLabel;

            if (hasReliableCurrencyNominal)
            {
                return CreateClassification(
                    OfzCouponType.Currency,
                    OfzClassificationReliability.Reliable,
                    reliableSource,
                    nominalCurrency,
                    reliable,
                    []);
            }

            if (reliableTypes.Length == 1)
            {
                return CreateClassification(
                    reliableTypes[0],
                    OfzClassificationReliability.Reliable,
                    reliableSource,
                    nominalCurrency,
                    reliable,
                    []);
            }

            return CreateClassification(
                OfzCouponType.Unknown,
                OfzClassificationReliability.Conflict,
                reliableSource,
                nominalCurrency,
                reliable,
                ["Conflicting OFZ type evidence."]);
        }

        var inferred = InferFromSeries(text);
        if (inferred is not null)
        {
            return CreateClassification(
                inferred.CouponType,
                OfzClassificationReliability.Inferred,
                OfzIssueClassificationSources.SeriesFallback,
                nominalCurrency,
                [inferred],
                ["Classification inferred from OFZ issue series."]);
        }

        return CreateClassification(
            OfzCouponType.Unknown,
            OfzClassificationReliability.Unknown,
            sourceLabel,
            nominalCurrency,
            [],
            ["No reliable OFZ type evidence."]);
    }

    public static string GetCouponTypeMarker(OfzCouponType couponType)
    {
        return couponType switch
        {
            OfzCouponType.Fixed => "ОФЗ-ПД",
            OfzCouponType.Floating => "ОФЗ-ПК",
            OfzCouponType.InflationLinked => "ОФЗ-ИН",
            OfzCouponType.Amortized => "ОФЗ-АД",
            OfzCouponType.Currency => "Валютная",
            _ => "Unknown"
        };
    }

    private static OfzIssueClassification CreateClassification(
        OfzCouponType couponType,
        OfzClassificationReliability reliability,
        string source,
        string nominalCurrency,
        IReadOnlyCollection<Candidate> candidates,
        IReadOnlyList<string> limitations)
    {
        return new OfzIssueClassification
        {
            CouponType = couponType,
            CouponTypeMarker = GetCouponTypeMarker(couponType),
            Reliability = reliability,
            Source = source,
            Evidence = BuildEvidence(candidates, source),
            Limitations = limitations,
            IsIndexedNominal = couponType == OfzCouponType.InflationLinked ? true : null,
            IsAmortizing = couponType == OfzCouponType.Amortized ? true : null,
            NominalCurrency = nominalCurrency
        };
    }

    private static Candidate? InferFromSeries(string text)
    {
        if (ContainsAny(text, "su520", "офз 520", "ofz 520"))
        {
            return new Candidate(OfzCouponType.InflationLinked, "series-su520", "series=520");
        }

        if (ContainsAny(text, "su460", "офз 460", "ofz 460"))
        {
            return new Candidate(OfzCouponType.Amortized, "series-su460", "series=460");
        }

        if (ContainsAny(text, "su290", "офз 290", "ofz 290"))
        {
            return new Candidate(OfzCouponType.Floating, "series-su290", "series=290");
        }

        if (ContainsAny(text, "su262", "офз 262", "ofz 262"))
        {
            return new Candidate(OfzCouponType.Fixed, "series-su262", "series=262");
        }

        return null;
    }

    private static string BuildSearchText(OfzIssue issue)
    {
        return string.Join(
                ' ',
                new[]
                {
                    issue.SecId,
                    issue.BondType,
                    issue.BondSubType,
                    issue.ShortName,
                    issue.SecName,
                    issue.IssueName,
                    issue.FaceUnit,
                    issue.CurrencyId
                }.Where(value => !string.IsNullOrWhiteSpace(value)))
            .ToLowerInvariant();
    }

    private static string BuildEvidence(IReadOnlyCollection<Candidate> candidates, string source)
    {
        if (candidates.Count == 0)
        {
            return $"source={source}; rules=none";
        }

        var rules = string.Join(",", candidates.Select(candidate => candidate.RuleId).Distinct(StringComparer.Ordinal));
        var details = string.Join(",", candidates.Select(candidate => candidate.Detail).Where(detail => !string.IsNullOrWhiteSpace(detail)).Distinct(StringComparer.Ordinal));
        return string.IsNullOrWhiteSpace(details)
            ? $"source={source}; rules={rules}"
            : $"source={source}; rules={rules}; {details}";
    }

    private static string NormalizeSource(string? source)
    {
        return string.IsNullOrWhiteSpace(source)
            ? OfzIssueClassificationSources.Unknown
            : source.Trim();
    }

    private static string NormalizeCurrency(string? value)
    {
        var normalized = FirstNonEmpty(value)?.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Unknown";
        }

        return normalized == "SUR" || normalized == "RUR" ? "RUB" : normalized;
    }

    private static string? ExtractTextCurrency(string text)
    {
        if (text.Contains("cny", StringComparison.OrdinalIgnoreCase))
        {
            return "CNY";
        }

        if (text.Contains("usd", StringComparison.OrdinalIgnoreCase))
        {
            return "USD";
        }

        if (text.Contains("eur", StringComparison.OrdinalIgnoreCase))
        {
            return "EUR";
        }

        return null;
    }

    private static bool IsRubleCode(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            RubleCurrencyCodes.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private sealed record Candidate(OfzCouponType CouponType, string RuleId, string Detail);
}
