# Data Model: Индексный контекст ОФЗ

## OfzMarketIndexSeries

Описание индексного ряда, который используется как фон рынка ОФЗ.

**Fields**:

- `SecId`: код индекса, например `RGBI`, `RGBITR`, `RUGBICP3Y`.
- `ShortName`: короткое имя из источника, если доступно.
- `Name`: полное имя индекса, если доступно.
- `Role`: `WholeMarket`, `DurationSegment`.
- `ReturnKind`: `Price`, `TotalReturn`.
- `DurationBucket`: `All`, `UpTo1Y`, `OneToThreeY`, `ThreeToFiveY`,
  `SevenYPlus`, `Unknown`.
- `CurrencyId`: валюта ряда.
- `IsRequired`: true для `RGBI`/`RGBITR`, false для optional segment series.

**Validation rules**:

- `SecId` обязателен и нормализуется без пробелов.
- `RGBI` и `RGBITR` считаются required whole-market series.
- Unknown duration bucket не скрывает ряд, но добавляет limitation.

## OfzMarketIndexPoint

Дневная точка индексного ряда.

**Fields**:

- `SecId`: код индекса.
- `TradeDate`: торговая дата.
- `Close`: значение индекса на дату, nullable.
- `Open`, `High`, `Low`: optional значения, если доступны.
- `Value`: оборот или расчетная value/base из ISS, nullable.
- `Yield`: доходность индекса, nullable.
- `Duration`: duration индекса в днях, nullable.
- `CurrencyId`: валюта.
- `SourceKind`: `History` или `Snapshot`.
- `ObservedAt`: время наблюдения для snapshot/current values.
- `IsProvisional`: true для текущего неполного дня или snapshot-derived point.
- `Limitations`: ограничения данных на уровне точки.

**Validation rules**:

- `SecId + TradeDate + SourceKind` уникальны в cache.
- Missing `Close`, `Yield`, `Duration` хранится как null, не как `0`.
- `SourceKind = Snapshot` требует `IsProvisional = true`.
- Snapshot point не должен silently overwrite historical point без явного
  выбора более свежего status.

## OfzIndexContextDay

Сводка индексного фона за дату, сопоставленная с activity/breadth day.

**Fields**:

- `TradeDate`: дата контекста.
- `WholeMarketPoints`: `RGBI`/`RGBITR` points за дату.
- `SegmentPoints`: duration-segment points за дату.
- `DailyMoves`: изменения индексов относительно предыдущей торговой точки.
- `MarketDirection`: `Up`, `Down`, `Mixed`, `Flat`, `Unknown`.
- `HasMeaningfulMove`: true, если whole-market daily `Close` change по
  `RGBI`/`RGBITR` не меньше `0.25%` по модулю или `Yield` change не меньше
  `0.10 п.п.` по модулю.
- `IsProvisional`: true, если хотя бы одна used point предварительная.
- `Limitations`: missing required series, missing previous point, missing field,
  provisional state.

**Validation rules**:

- Daily move считается только по предыдущей торговой точке той же series.
- Если required series отсутствует, день остается в context с limitation.
- `MarketDirection` не выводится из missing values.

## OfzIndexDailyMove

Изменение одного индексного ряда за день.

**Fields**:

- `SecId`.
- `TradeDate`.
- `PreviousTradeDate`: предыдущая дата той же series.
- `Close`, `PreviousClose`.
- `Change`: `Close - PreviousClose`, nullable.
- `ChangePercent`: `Change / PreviousClose`, nullable.
- `YieldChange`: изменение `Yield`, nullable.
- `Direction`: `Up`, `Down`, `Flat`, `Unknown`.
- `IsMeaningful`: true, если `ChangePercent` не меньше `0.25%` по модулю или
  `YieldChange` не меньше `0.10 п.п.` по модулю.
- `Limitation`: почему изменение нельзя посчитать.

**Validation rules**:

- `ChangePercent` считается только при `PreviousClose > 0`.
- Missing previous point дает `Direction = Unknown`, а не `Flat`.
- Yield change не считается, если одна из доходностей отсутствует.

## OfzIndexSegmentContext

Контекст duration-сегментов за период или дату.

**Fields**:

- `Bucket`: duration bucket.
- `PriceSeriesSecId`, `TotalReturnSeriesSecId`.
- `PointCount`: количество доступных points.
- `LatestPoint`.
- `PeriodChange`, `PeriodChangePercent`.
- `LatestYield`, `LatestDuration`.
- `Limitations`: отсутствующий ряд, missing previous point, missing field.

**Validation rules**:

- Segment context может быть частичным: price есть, total-return отсутствует,
  или наоборот.
- Отсутствующий segment не скрывает остальные segments.
- Period change считается только при наличии первой и последней валидной точки.

## OfzIndexFinding

Вывод по index context, входящий в общий `OfzMarketSummary`.

**Fields**:

- `Kind`: например `IndexMove`, `ActivityWithIndexMove`,
  `ActivityWithoutIndexMove`, `SegmentIndexMove`, `IndexDataLimitation`.
- `Scope`: `Market`, `Date`, `Segment`, `DataQuality`.
- `TradeDate`: дата finding, если применимо.
- `Text`: пользовательский текст без recommendation language.
- `Evidence`: `SecId`, `Change`, `ChangePercent`, `Yield`, `Duration`,
  activity/breadth references.
- `DrillDown`: ссылка на index context day или series.
- `Limitations`: причины неполного evidence.

**Validation rules**:

- Finding должен быть воспроизводим из `OfzIndexContextDay`.
- Current-day finding обязан иметь provisional marker.
- Текст не содержит `купить`, `продать`, `держать`, `buy`, `sell`,
  `recommend`.

## Relationships

- `OfzMarketSummary` содержит коллекцию `OfzIndexContextDay`,
  `OfzIndexSegmentContext` и index-backed findings.
- `OfzIndexContextDay` строится из нескольких `OfzMarketIndexPoint`.
- `OfzIndexDailyMove` ссылается на текущую и предыдущую точку одной series.
- `OfzActivityService` загружает `OfzMarketIndexPoint` рядом с existing OFZ
  activity data и передает их в Core builder.

## State Transitions

1. `NoIndexData`: за период нет ни одной точки required series.
2. `PartialIndexData`: есть часть points или series, но не полный контекст.
3. `Ready`: есть required series и можно посчитать daily moves.
4. `ReadyWithLimitations`: контекст есть, но часть fields/segments отсутствует.
5. `Provisional`: context содержит current-day snapshot или неполный день.
