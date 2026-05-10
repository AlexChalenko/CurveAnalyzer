# Data Model: Quality gates для OFZ summary/export

Feature 013 не добавляет production data model. Ниже описаны test/design
понятия, которые помогают держать quality gates понятными и устойчивыми.

## RepresentativeSummaryScenario

Synthetic input для `OfzMarketSummaryBuilder`, покрывающий полный export
contract.

Fields:

- `StartDate`, `EndDate`: полный период расчета.
- `Issues`: минимум один ОФЗ-ПД, один ОФЗ-ПК и/или ОФЗ-ИН для coverage type
  filters/special metrics.
- `Trades`: historical trades with yield/duration/value.
- `ActivityMetrics`: active days, yield moves, repeated issue evidence.
- `LiquidityMetrics`: at least one ready liquidity point and one missing/spread
  edge if needed.
- `IndexPoints`: RGBI/RGBITR and optional duration segment points.
- `CashflowEvents`: coupon/amortization/maturity sample near activity date.
- `CbrKeyRates`: key-rate history for external factor context.

Validation:

- Must build deterministic summary without network or file I/O.
- Must include enough data for default `MaxFindings` behavior.

## SummaryQualityGate

Набор assertions для `OfzMarketSummary` object и serialized JSON.

Fields:

- `RequiredSections`: JSON property names required in representative export.
- `RequiredSourceCounts`: source count property names and expected ranges.
- `RequiredFindingKinds`: finding kinds that must appear when matching evidence
  exists.
- `NoRecommendationTerms`: forbidden terms checked in findings/limitations.
- `DateOnlyPaths`: representative JSON paths where date-only format is required.

Validation:

- Fails with clear assertion message naming the missing contract surface.
- Does not assert full JSON snapshot.

## ContractSectionExpectation

Ожидание для одной секции summary JSON.

Fields:

- `SectionName`: top-level JSON property.
- `RequiredWhen`: condition under representative input.
- `MinimumItemCount`: optional minimum item count for arrays.
- `SourceCountName`: optional source count that must match or be positive.

Validation:

- Missing section fails even if tests for individual builder components pass.
- Empty section fails only when representative scenario promised data.

## MissingDataExpectation

Focused expectation for no-fake-zero/no-fake-n/a behavior.

Fields:

- `SectionName`: special/external/liquidity/index section.
- `ValuePath`: nullable value that must stay `null` when missing.
- `LimitationKind`: expected limitation kind.
- `Availability`: expected availability marker.

Validation:

- Missing value must not be serialized as numeric `0`.
- Limitation must explain the data quality boundary.
