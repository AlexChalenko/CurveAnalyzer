# Research: OFZ Analytics Summary

## Decision: Summary is computed from existing loaded data, not persisted

**Decision**: V1 builds `MarketSummary` in memory from Core collections already
contained in `OfzActivityLoadResult`: issues, trades, activity metrics,
historical liquidity metrics and snapshot liquidity metrics. UI/Application
unwrap Application data; Core builder does not depend on Application types.

**Rationale**: The current app already loads trades, activity metrics,
liquidity metrics, current snapshot liquidity and issue metadata for the
selected period. Persisting derived summary rows would add invalidation rules
for filters, current-day provisional data and snapshot-only observations before
there is a real external consumer.

**Alternatives considered**:

- New SQLite summary table: rejected because every filter/date/snapshot change
  would require cache invalidation and migrations.
- UI-only string generation in `OfzActivityViewModel`: rejected because future
  MCP/API reuse needs stable typed evidence, not only display text.

## Decision: Add a dedicated Core analyzer

**Decision**: Add `OfzMarketSummaryBuilder` and `OfzMarketSummary` domain
models in `CurveAnalyzer.Core`.

**Rationale**: `OfzActivityAnalyzer` already contains activity, liquidity,
scatter and detail transformations. A separate analyzer keeps the new ranking
and evidence rules readable while preserving the dependency direction: Core
does pure calculations, Presentation binds results.

**Alternatives considered**:

- Extend `OfzActivityAnalyzer`: rejected because the class is already broad and
  summary ranking is a separate read-model concern.
- Add an Application service only: rejected because unit tests for ranking and
  evidence should not need repository or async orchestration.

## Decision: Findings are typed and evidence-first

**Decision**: Each summary item uses a stable `SummaryFindingKind`, priority,
scope, display text and `FindingEvidence` object with numeric fields.

**Rationale**: The user must be able to verify every conclusion from the same
screen. Stable finding types also make the output suitable for future
structured export without parsing Russian display text.

**Alternatives considered**:

- Reuse `OfzActivityInsight` as-is: rejected because current insights are
  display-oriented and do not model data limitations or contract metadata
  explicitly enough.
- Free-form dictionaries: rejected because tests would not reliably catch
  missing evidence fields or stale scope.

## Decision: Missing, snapshot-only and provisional data are first-class

**Decision**: `DataLimitation` entries are attached to both the full summary and
individual findings when spread, bid/offer, baseline, current-day or snapshot
constraints affect interpretation.

**Rationale**: Existing liquidity work already distinguishes missing quotes,
snapshot-only data and provisional current-day data. The summary must preserve
those distinctions and never turn missing spread into `0`.

**Alternatives considered**:

- Hide findings with incomplete liquidity: rejected because "нет котировок при
  активной торговле" is itself a useful signal.
- Fill missing values with neutral defaults: rejected because it violates the
  existing missing-vs-zero rule and could produce misleading conclusions.

## Decision: Ranking uses deterministic categories, not recommendation logic

**Decision**: Findings are ranked by deterministic severity rules: market-wide
active dates, repeated issue activity, segment concentration, yield moves,
liquidity/spread limitations and weak liquidity signals.

**Rationale**: This matches the current analyzer style and keeps output
explainable. Text must describe observed market data only and avoid
recommendation language.

**Alternatives considered**:

- LLM-generated summaries: rejected for v1 because output would be harder to
  test and might introduce recommendation wording.
- Statistical model with opaque scoring: rejected because the current data set
  is small and explainability matters more than model complexity.

## Decision: Segment summaries use existing `OfzCouponType`

**Decision**: Segment-level aggregation groups by `OfzCouponType`: Fixed,
Floating, InflationLinked, Amortized, Currency and Unknown when present.

**Rationale**: Issue metadata already classifies ОФЗ-ПД/ПК/ИН/АД and currency
issues. Reusing that logic keeps summary filters consistent with the current
screen and avoids a second taxonomy.

**Alternatives considered**:

- Segment by maturity buckets first: useful later, but current user workflow is
  already built around OFZ type filters.
- Segment by market board: rejected for v1 because supported data path is TQOB
  OFZ activity.

## Decision: Contract is JSON-compatible documentation, not an API endpoint

**Decision**: Provide `contracts/market-summary.schema.json` as the structured
shape for future MCP/API use, but do not expose a server endpoint in v1.

**Rationale**: The spec explicitly excludes a new MCP server for v1 while asking
for a machine-readable summary contract. A JSON schema documents stable fields
without adding runtime surface area.

**Alternatives considered**:

- Start MCP server now: rejected as out of scope and premature while UI
  behavior is still evolving.
- No contract artifact: rejected because it would defer the key reuse decision
  and make future MCP work more expensive.
