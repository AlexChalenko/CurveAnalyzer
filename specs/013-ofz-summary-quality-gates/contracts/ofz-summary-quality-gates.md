# Contract: OFZ Summary Quality Gates

## Scope

Quality gates validate `OfzMarketSummary` and serialized summary JSON for a
representative OFZ analytics scenario. They do not validate UI layout, network
loading or persistence.

## Required Top-Level JSON Contract

Representative export must include:

- `schemaVersion = "1.6"`
- `startDate`, `endDate`, `generatedAt`
- `findings`
- `segments`
- `breadthDays`
- `indexContextDays`
- `indexSegments`
- `cashflowContext`
- `seasonalityContext`
- `externalFactorsContext`
- `specialMetrics`
- `limitations`
- `sourceCounts`

## Required Source Counts

The representative scenario must assert these counters exist and are coherent:

- `issues`
- `trades`
- `activityMetrics`
- `liquidityMetrics`
- `cashflowEvents`
- `cashflowIssues`
- `seasonalityObservations`
- `seasonalityFindings`
- `externalFactorObservations`
- `externalFactorSeries`
- `externalFactorLinks`
- `indexPoints`
- `indexContextDays`
- `dates`

## Required Finding Coverage

When the representative input contains matching evidence, final top-level
`findings` must include:

- at least one activity/issue finding (`MarketActivity`, `YieldMove` or
  `RepeatedIssue`);
- at least one breadth/concentration finding;
- at least one context finding for external factors when
  `externalFactorsContext.links` is non-empty.

The gate should not require every possible finding kind, because ranking can
legitimately change as analytics evolve.

## Date-Only Fields

Representative JSON must serialize trade/event dates as `yyyy-MM-dd` without
time component for:

- top-level `startDate`, `endDate`, `insightStartDate`, `insightEndDate`;
- `breadthDays[*].tradeDate`;
- `indexContextDays[*].tradeDate`;
- `cashflowContext.events[*].eventDate`;
- `seasonalityContext.findings[*].tradeDate`;
- `externalFactorsContext.factorSeries[*].observations[*].tradeDate`;
- `findings[*].evidence.tradeDate` when present.

## Missing-Data Policy

Quality gates must fail if:

- missing special/factor values are serialized as numeric `0`;
- missing source data is present without a limitation;
- snapshot/provisional values lose their `isSnapshot`/`isProvisional` markers
  in sections that expose those markers.

## Forbidden Wording

Findings and limitations must not use investment recommendation or causality
language such as:

- `купить`
- `продать`
- `рекоменд`
- `прогноз`
- `из-за`
- `потому что рынок`

Neutral wording such as `на фоне`, `совпало с`, `локальная активность` is
allowed.
