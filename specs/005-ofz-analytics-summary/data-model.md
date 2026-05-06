# Data Model: OFZ Analytics Summary

## MarketSummary

Итоговая сводка за выбранный период и фильтры.

**Fields**:

- `StartDate`, `EndDate`: нормализованный период расчета.
- `GeneratedAt`: время построения сводки.
- `CouponTypeFilter`: выбранный фильтр типа ОФЗ или `null` для "Все".
- `InsightStartDate`, `InsightEndDate`: период, по которому строились выводы.
- `SignalScope`: режим сигналов (`AllDays` или `LastAvailableDay`).
- `Findings`: ранжированный список `SummaryFinding`.
- `Segments`: список `SegmentSummary`.
- `Limitations`: общие `DataLimitation` по данным.
- `SourceCounts`: количество issues, trades, activity metrics, liquidity
  metrics, snapshot metrics, dates.

**Validation rules**:

- `StartDate <= EndDate`.
- `Findings` не содержит stale-данные от старого фильтра.
- Если данных недостаточно, `Findings` может быть пустым, но `Limitations`
  должен объяснять причину.
- Missing liquidity/spread не сериализуется как `0`.

## SummaryFinding

Один объяснимый вывод в сводке.

**Fields**:

- `Id`: стабильный deterministic id внутри summary, например
  `market-active-date-2026-04-28`.
- `Kind`: тип вывода: `MarketActivity`, `SegmentConcentration`,
  `RepeatedIssue`, `YieldMove`, `WeakLiquidity`, `SpreadSignal`,
  `DataQuality`.
- `Priority`: число для сортировки, больше = важнее.
- `Scope`: область вывода: `Market`, `Segment`, `Issue`, `Date`,
  `Liquidity`, `Spread`, `DataQuality`.
- `Title`: короткий заголовок.
- `Text`: человекочитаемое объяснение без recommendation language.
- `Evidence`: `FindingEvidence`.
- `Limitations`: data limitations, применимые к этому выводу.
- `DrillDown`: ссылка на существующий UI context: date, secid, segment или
  table/chart target.

**Validation rules**:

- У каждого finding есть хотя бы одно числовое evidence поле или явная
  `DataLimitation`.
- `Text` не содержит `buy`, `sell`, `купить`, `продать`, `держать`,
  `рекоменд`.
- Если `Scope = Issue`, должен быть `SecId`.
- Если finding основан на current snapshot, должна быть limitation
  `SnapshotOnly` или `Provisional`.

## FindingEvidence

Числовые основания вывода.

**Fields**:

- `TradeDate`: дата события, если применимо.
- `SecId`, `ShortName`: выпуск, если применимо.
- `CouponType`, `CouponTypeMarker`: сегмент выпуска.
- `IssueCount`, `ActiveIssueCount`, `RankableIssueCount`: количества выпусков.
- `TotalValue`, `Value`: оборот в RUB или валюте инструмента.
- `TotalNumTrades`, `NumTrades`: количество сделок.
- `ActivityScore`, `MedianActivityScore`, `MaxActivityScore`: activity
  score-поля.
- `YieldMove`, `YieldValue`, `Duration`: доходность и дюрация.
- `Spread`, `SpreadSource`, `LiquidityScore`, `LiquidityBucket`,
  `LiquidityStatus`: liquidity/spread evidence.
- `ZSpread`, `ZSpreadBp`, `GSpreadBp`: spread context, если доступен.
- `IsSnapshot`, `IsProvisional`: флаги надежности источника.

**Validation rules**:

- Nullable numeric fields mean missing data, not zero.
- `SpreadSource = Missing` requires `Spread = null`.
- `IsProvisional = true` requires a matching data limitation.

## SegmentSummary

Агрегированная картина по типу ОФЗ.

**Fields**:

- `CouponType`, `CouponTypeMarker`: сегмент.
- `IssueCount`: количество выпусков в выбранном фильтре/периоде.
- `ActiveIssueCount`: количество выпусков с положительным оборотом.
- `RankableIssueCount`: количество выпусков с пригодным activity score.
- `TotalValue`, `TotalNumTrades`: агрегаты.
- `MedianActivityScore`, `MaxActivityScore`: activity context.
- `WeakLiquidityCount`, `MissingQuotesCount`, `SnapshotOnlyCount`: liquidity
  context.
- `TopIssues`: список `IssueFocus`.
- `Limitations`: ограничения данных сегмента.

**Validation rules**:

- Валютные выпуски не смешиваются с рублевыми без `CouponType = Currency`.
- `IssueCount` и агрегаты считаются только по текущему фильтру.

## IssueFocus

Выпуск, который объясняет вывод или является лидером сегмента.

**Fields**:

- `SecId`, `ShortName`, `CouponTypeMarker`, `DisplayMarker`.
- `TradeDate`: дата лучшего/важного наблюдения.
- `Value`, `NumTrades`, `ActivityScore`.
- `YieldMove`, `Duration`.
- `Spread`, `LiquidityScore`, `LiquidityBucket`, `LiquidityStatus`.
- `Reason`: `TopValue`, `TopScore`, `WeakLiquidity`, `YieldMove`,
  `RepeatedActivity`.

**Validation rules**:

- `Reason` должен соответствовать evidence, по которому выпуск выбран.
- Если выпуск попадает из snapshot liquidity, выставляется `IsSnapshot`.

## DataLimitation

Явное ограничение качества данных.

**Fields**:

- `Kind`: `NoData`, `InsufficientBaseline`, `MissingSpread`,
  `MissingQuotes`, `SnapshotOnly`, `Provisional`, `CurrencyMixed`,
  `ShortRange`.
- `Scope`: `Market`, `Segment`, `Issue`, `Date`, `Liquidity`, `Spread`.
- `Text`: короткое объяснение.
- `SecId`, `TradeDate`, `CouponType`: optional привязка.

**Validation rules**:

- Не используется для обычных нулевых значений.
- Для current-day данных должна явно отражать provisional nature.

## Relationships

- `MarketSummary` содержит много `SummaryFinding`, `SegmentSummary` и
  `DataLimitation`.
- `SummaryFinding` содержит один `FindingEvidence` и может ссылаться на
  `DataLimitation`.
- `SegmentSummary` содержит несколько `IssueFocus`.
- `IssueFocus` и `FindingEvidence` используют existing `OfzIssue`,
  `OfzActivityMetric` и `OfzLiquidityMetric` как source data, но не владеют ими.

## State Transitions

1. `NoData`: нет trades/metrics после фильтра.
2. `InsufficientData`: данные есть, но baseline/spread/yield недостаточны для
   надежных выводов.
3. `Ready`: summary содержит findings и evidence.
4. `ReadyWithLimitations`: summary содержит findings и limitations, например
   snapshot-only или provisional.
