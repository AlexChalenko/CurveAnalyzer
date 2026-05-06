# Data Model: OFZ Market Breadth

## MarketBreadthDay

Сводка одного торгового дня в выбранном периоде и фильтре.

**Fields**:

- `TradeDate`: торговая дата.
- `IssueCount`: количество выпусков в базе расчета после фильтра.
- `ComparableIssueCount`: количество выпусков, для которых есть текущая и
  предыдущая доходность.
- `NotComparableIssueCount`: количество выпусков без корректной пары
  доходностей.
- `ActiveIssueCount`: количество выпусков с положительным оборотом или
  сделками.
- `ActiveIssueShare`: `ActiveIssueCount / IssueCount`, если база не пуста.
- `Direction`: `YieldDirectionBreakdown`.
- `Turnover`: общий оборот дня.
- `NumTrades`: суммарное количество сделок.
- `Concentration`: `TurnoverConcentration`.
- `TypeShares`: список `TypeTurnoverShare`.
- `TopContributors`: top issues по обороту дня.
- `Limitations`: ограничения данных для даты.
- `IsProvisional`: true для текущего неполного дня или snapshot-derived данных.

**Validation rules**:

- `ComparableIssueCount + NotComparableIssueCount <= IssueCount`.
- Direction counts суммируются в `ComparableIssueCount`.
- Nullable shares означают отсутствие базы, а не `0`.
- `IsProvisional = true` требует limitation `Provisional` или `SnapshotOnly`.

## YieldDirectionBreakdown

Распределение сравнимых выпусков по направлению изменения доходности.

**Fields**:

- `YieldUpCount`: выпусков с ростом доходности.
- `YieldDownCount`: выпусков со снижением доходности.
- `UnchangedCount`: выпусков без значимого изменения.
- `NotComparableCount`: выпусков без пары для сравнения.
- `DominantDirection`: `Up`, `Down`, `Mixed`, `Flat`, `None`.
- `DominantDirectionShare`: доля доминирующего направления среди сравнимых
  выпусков.
- `MedianYieldMove`: медианное изменение доходности по сравнимым выпускам.

**Validation rules**:

- Direction определяется только по предыдущей доступной торговой записи выпуска.
- Порог `unchanged` фиксируется в options и применяется симметрично.
- `NotComparableCount` не смешивается с `UnchangedCount`.

## TurnoverConcentration

Концентрация оборота по top-N выпускам за день.

**Fields**:

- `TotalValue`: общий оборот дня.
- `IssueBaseCount`: количество выпусков с оборотом в базе расчета.
- `Top5Value`, `Top5Share`: оборот и доля top-5 выпусков.
- `Top10Value`, `Top10Share`: оборот и доля top-10 выпусков.
- `TopIssues`: список `MarketBreadthContributor`.
- `IsHighConcentration`: true, если доля top-5/top-10 превышает threshold.

**Validation rules**:

- `Top5Share` и `Top10Share` считаются только от `TotalValue > 0`.
- Если выпусков меньше 5 или 10, используется доступное количество и явно
  сохраняется `IssueBaseCount`.
- Missing turnover не превращается в zero-contributor.

## TypeTurnoverShare

Вклад типа ОФЗ в оборот/активность.

**Fields**:

- `CouponType`, `CouponTypeMarker`: тип ОФЗ.
- `IssueCount`: выпусков типа в выбранной базе.
- `ActiveIssueCount`: активных выпусков типа.
- `TotalValue`: оборот типа.
- `ValueShare`: доля в общем обороте текущего набора.
- `NumTrades`: сделки типа.
- `MissingTypeCount`: сколько выпусков не удалось классифицировать.

**Validation rules**:

- В режиме конкретного типа все значения ограничены этим типом.
- Unknown type не теряется молча и попадает в `Unknown` или limitation.
- Валютные выпуски не смешиваются с рублевыми без явного `CouponType`.

## MarketBreadthContributor

Выпуск, который объясняет breadth day.

**Fields**:

- `SecId`, `ShortName`, `CouponType`, `CouponTypeMarker`.
- `TradeDate`.
- `Value`, `ValueShare`, `NumTrades`.
- `YieldMove`, `YieldDirection`.
- `ActivityScore`, `LiquidityScore`, `LiquidityBucket`, `Spread`.
- `Reason`: `TopTurnover`, `YieldUp`, `YieldDown`, `WeakLiquidity`,
  `HighActivity`.

**Validation rules**:

- `ValueShare` nullable, если нет общего оборота.
- `Reason` должен соответствовать evidence, из-за которого выпуск выбран.

## MarketBreadthFinding

Вывод по market breadth, входящий в общий `OfzMarketSummary`.

**Fields**:

- `Kind`: новый или расширенный kind, например `MarketBreadth`,
  `TurnoverConcentration`, `TypeShare`.
- `Scope`: `Market`, `Date`, `Segment`, `Issue`, `DataQuality`.
- `Evidence`: числовые поля breadth: counts, shares, concentration,
  direction.
- `DrillDown`: ссылка на дату или выпуск.

**Validation rules**:

- Текст не содержит recommendation language.
- Для current-day evidence присутствует provisional marker.
- Finding должен быть воспроизводим из `MarketBreadthDay`.

## Relationships

- `OfzMarketSummary` содержит коллекцию `MarketBreadthDay` и breadth findings.
- `MarketBreadthDay` содержит один `YieldDirectionBreakdown`, один
  `TurnoverConcentration`, несколько `TypeTurnoverShare` и contributors.
- `MarketBreadthContributor` ссылается на existing `OfzIssue`,
  `OfzDailyTrade`, `OfzActivityMetric` и optional `OfzLiquidityMetric`.

## State Transitions

1. `NoData`: после фильтра нет trades/activity metrics.
2. `InsufficientComparableData`: данные есть, но не хватает предыдущих
   доходностей.
3. `Ready`: breadth day имеет comparable data и turnover base.
4. `ReadyWithLimitations`: breadth day имеет result и limitations.
5. `Provisional`: день рассчитан по current-day/snapshot/incomplete данным.
