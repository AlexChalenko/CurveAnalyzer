# Data Model: Внешние факторы активности ОФЗ

## ExternalFactorContext

Контейнер факторного слоя в `OfzMarketSummary`.

Поля:

- `StartDate`, `EndDate`: date-only границы summary period.
- `InsightStartDate`, `InsightEndDate`: date-only окно выводов.
- `FactorSeries`: normalized factor rows.
- `Links`: связи факторных наблюдений с активностью/доходностью.
- `Findings`: generated factor findings или ссылки на summary findings.
- `Limitations`: missing/short/provisional source limitations.
- `ObservationCount`: число usable observations.
- `MissingFactorCount`: число факторов, помеченных missing.

## ExternalFactorSeries

Один факторный ряд.

Поля:

- `Kind`: `PolicyRate`, `MarketIndex`, `DerivedOfzMetric`, `Currency`, `Commodity`.
- `Code`: stable id (`cbr_key_rate`, `RGBI`, `implied_inflation`).
- `Label`: отображаемое имя.
- `Unit`: `%`, `index`, `RUB`, etc.
- `Scope`: `Market`, `Segment`, `Issue`, `Derived`.
- `Source`: `CbrKeyRate`, `MoexIndex`, `OfzDerived`, `Missing`.
- `Availability`: `Historical`, `SnapshotOnly`, `Provisional`, `Missing`,
  `InsufficientHistory`.
- `Observations`: ordered normalized observations.
- `LatestObservation`: latest observation on or before `EndDate`.
- `Limitations`: row-level limitations.

## ExternalFactorObservation

Нормализованная точка ряда.

Поля:

- `TradeDate`: date-only observation date.
- `Value`: nullable numeric value.
- `DailyChange`: nullable absolute change.
- `DailyChangePercent`: nullable percent change.
- `Direction`: normalized direction if available.
- `IsMeaningful`: meaningful-move marker if available.
- `PreviousTradeDate`: date-only previous observation date.
- `SourceLabel`: `history`, `snapshot`, `ЦБ`, `derived`, `missing`.
- `IsSnapshot`, `IsProvisional`: source flags.

## ExternalFactorActivityLink

Связь factor observation с активностью или доходностью.

Поля:

- `TradeDate`: date-only activity date.
- `FactorCode`: source factor code.
- `FactorObservationDate`: date-only used factor date.
- `LinkKind`: `ActivityWithFactorMove`, `ActivityWithoutFactorMove`,
  `YieldMoveWithFactorMove`, `MissingFactor`.
- `ActivityValue`, `ActivityScore`, `YieldMove`: selected evidence values.
- `Text`: short neutral explanation for UI/JSON.
- `Limitations`: inherited limitations.

## State rules

- `ExternalFactorContext` may be present with zero usable observations if all
  factors are missing, but it must include limitations.
- `Value = null` means unavailable; `0` is a real value only when source value
  is actually zero.
- Policy-rate observation date may be earlier than activity trade date; index
  observation date should match trade date for a direct link.
- Provisional source data marks the series, observation, link and finding.
