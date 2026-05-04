# Data Model: анализ активности ОФЗ

## Entity: OfzIssue

Описывает выпуск ОФЗ или близкий гособлигационный выпуск из выбранного board.

### Fields

- `SecId` - уникальный код бумаги из ISS, обязательный.
- `ShortName` - краткое название выпуска, обязательное для отображения.
- `SecName` - полное название, опционально.
- `Isin` - ISIN, опционально.
- `MatDate` - дата погашения, опционально.
- `IssueName` - официальное имя выпуска из ISS description/snapshot,
  опционально.
- `FaceValue` - номинал, опционально.
- `InitialFaceValue` - начальный номинал, если доступен из ISS, опционально.
- `FaceUnit` - валюта номинала, опционально.
- `CurrencyId` - расчетная/торговая валюта из ISS, опционально.
- `CouponPercent` - купонная ставка, опционально.
- `CouponValue` - денежное значение ближайшего/текущего купона, опционально.
- `CouponPeriod` - длительность купонного периода из ISS, опционально.
- `NextCouponDate` - дата ближайшего купона, опционально.
- `BondType` - тип выпуска из ISS, опционально.
- `BondSubType` - подтип выпуска из ISS, опционально.
- `CouponType` - нормализованный тип выпуска: `Fixed`, `Floating`,
  `InflationLinked`, `Amortized`, `Currency`, `Unknown`.
- `IsRub` - расчетный признак рублевого выпуска.
- `IsStandardOfz` - расчетный признак стандартного рублевого ОФЗ.
- `CouponTypeMarker` - короткая UI-пометка типа выпуска.

### Validation Rules

- `SecId` не может быть пустым.
- `ShortName` не может быть пустым для записей, показываемых в UI.
- Валютные или нестандартные выпуски не удаляются молча; они должны иметь
  отображаемую пометку.
- `CouponType` рассчитывается из `BondType`, `BondSubType`, названий выпуска и
  валютных признаков; неизвестный тип не блокирует анализ активности.

### Relationships

- Один `OfzIssue` имеет много `OfzDailyTrade`.
- Один `OfzIssue` может иметь много `OfzActivityMetric` и
  `OfzActivityAnomaly`.

## Entity: OfzDailyTrade

Одна дневная запись торгов по выпуску.

### Fields

- `SecId` - код выпуска, обязательный.
- `TradeDate` - торговая дата, обязательная.
- `BoardId` - режим торгов, для MVP ожидается `TQOB`.
- `NumTrades` - количество сделок, опционально.
- `Value` - денежный оборот, опционально.
- `Volume` - объем в бумагах/лотах по ISS, опционально.
- `OpenPrice` - цена открытия, опционально.
- `LowPrice` - минимум дня, опционально.
- `HighPrice` - максимум дня, опционально.
- `ClosePrice` - цена закрытия, опционально.
- `WeightedAveragePrice` - средневзвешенная цена, опционально.
- `YieldClose` - доходность закрытия, опционально.
- `YieldAtWeightedAveragePrice` - доходность по средневзвешенной цене,
  опционально.
- `Duration` - дюрация из ISS, опционально.
- `ZSpread` - Z-spread, опционально.
- `ZSpreadAtWeightedAveragePrice` - Z-spread по средневзвешенной цене,
  опционально.
- `LoadedAt` - момент локальной загрузки записи.

### Validation Rules

- Пара `SecId` + `TradeDate` уникальна.
- Записи с пустым `Value` сохраняются как частичные, но не участвуют в
  уверенном расчете `ActivityScore`.
- `Value < 0`, `Volume < 0` и `NumTrades < 0` считаются некорректными
  значениями и не должны попадать в расчет.

### Relationships

- Принадлежит одному `OfzIssue`.
- Используется как вход для `OfzActivityMetric`.

## Entity: OfzActivityMetric

Расчетная метрика активности по выпуску и дате.

### Fields

- `SecId` - код выпуска.
- `TradeDate` - дата метрики.
- `Value` - текущий оборот.
- `BaselineMedianValue` - медиана оборота за baseline window.
- `BaselineDays` - количество предыдущих записей, использованных в baseline.
- `ActivityScore` - отношение `Value / BaselineMedianValue`.
- `NumTrades` - количество сделок текущего дня.
- `YieldValue` - доходность, использованная для расчета движения.
- `PreviousYieldValue` - предыдущая доступная доходность.
- `YieldMove` - изменение доходности относительно предыдущей записи.
- `Status` - `Ready`, `InsufficientBaseline`, `MissingValue`,
  `MissingBaseline`, `MissingYield`, `NoData`.

### Validation Rules

- `ActivityScore` рассчитывается только при положительном `Value`,
  положительной `BaselineMedianValue` и достаточном `BaselineDays`.
- `BaselineDays >= 10` требуется для статуса `Ready`.
- Если доходность отсутствует, score может быть рассчитан, но `YieldMove`
  остается пустым.

### Relationships

- Рассчитывается на основе последовательности `OfzDailyTrade` одного выпуска.
- Может стать частью `OfzActivityAnomaly`.

## Entity: OfzActivityAnomaly

Ранжированная запись, показываемая пользователю как всплеск активности.

### Fields

- `SecId` - код выпуска.
- `ShortName` - краткое название для UI.
- `TradeDate` - дата аномалии.
- `Value` - оборот.
- `NumTrades` - количество сделок.
- `ActivityScore` - относительная сила всплеска.
- `YieldMove` - изменение доходности, если доступно.
- `Duration` - дюрация, если доступна.
- `FaceUnit` - валюта номинала/пометка выпуска.
- `Rank` - позиция в рейтинге выбранного диапазона.
- `Status` - состояние расчетной метрики.

### Validation Rules

- В рейтинг аномалий попадают только записи со статусом `Ready`, если
  пользователь не включил просмотр недостаточной базы.
- Сортировка по умолчанию: `ActivityScore` по убыванию, затем `Value` по
  убыванию.

## Entity: OfzActivityHeatmapCell

Ячейка карты активности для одного выпуска и одной даты.

### Fields

- `SecId`
- `ShortName`
- `TradeDate`
- `ActivityScore`
- `ScoreBucket` - нормализованный bucket цвета.
- `Value`
- `YieldMove`
- `Status`

### Validation Rules

- Пустая или недостаточная метрика должна иметь отдельный визуальный bucket, а
  не имитировать нулевую активность.
- `NoData` используется для матричных ячеек, где за торговую дату нет записи
  по конкретному выпуску.

## Entity: OfzActivityLoadState

Отслеживает локальную загрузку дневной истории.

### Fields

- `TradeDate` - дата загрузки.
- `BoardId` - режим торгов.
- `Status` - `Loaded`, `NoData`, `Failed`.
- `RowsLoaded` - количество записей.
- `LoadedAt` - время последней попытки.
- `ErrorMessage` - краткая ошибка для диагностики, опционально.

### Validation Rules

- Повторная загрузка даты должна быть идемпотентной.
- Ошибка одной даты не должна блокировать просмотр уже сохраненных дат.

## Derived Views

### Top anomalies

Выборка `OfzActivityAnomaly` за диапазон дат, отсортированная по
`ActivityScore`.

### Issue detail series

Последовательность `OfzDailyTrade` одного `SecId` за диапазон дат с полями
оборота, количества сделок, цены и доходности.

`OfzIssueDetail` содержит header выпуска (`SecId`, `ShortName`,
`DisplayMarker`, `MatDate`) и упорядоченные `OfzIssueDetailPoint`.
`OfzIssueDetailPoint` использует `PreferredPrice` и `PreferredYield`, поэтому
отсутствие доходности не скрывает оборот, сделки и цену.

### Activity index

Дневная агрегация по всем выбранным выпускам: сумма `Value`, сумма
`NumTrades`, количество выпусков с положительным оборотом, количество
rankable-выпусков и медианный `ActivityScore` по rankable-метрикам.
`ActivityIndex` вычисляется как `ActiveIssueCount * MedianActivityScore` и
равен `0`, если rankable score за дату отсутствует.

### Duration/yield scatter

`OfzDurationYieldScatterPoint` строится из `OfzActivityMetric` и `OfzIssue`.
В диапазонном режиме остается одна лучшая точка на `SecId`: максимальный
`ActivityScore`, затем максимальный `Value`, затем более поздняя дата.

Точка содержит `SecId`, `ShortName`, `DisplayMarker`, `CouponType`,
`CouponTypeMarker`, `TradeDate`, `DurationDays`, `DurationYears`, `Yield`,
`Value`, `NumTrades`, `ActivityScore`, `ScoreBucket` и `Status`.

В scatter попадают только записи с положительными `Value`, `Duration`,
`YieldValue` и rankable `ActivityScore`. `YieldMove` не требуется: запись со
статусом `MissingYield` может попасть в scatter, если текущая доходность есть,
но предыдущей доходности для изменения не было.

### Activity insights

`OfzActivityInsight` содержит объяснимое наблюдение по выбранному диапазону:

- `Kind` - тип вывода: `MarketWideActivity`, `RepeatedIssueActivity`,
  `YieldMoveActivity`, `BlockLikeActivity`, `TradeCountActivity`,
  `CouponTypeConcentration`.
- `Severity` - относительная важность вывода.
- `Title` - короткий заголовок для UI.
- `Text` - человекочитаемое описание без рекомендаций к покупке/продаже.
- `TradeDate` - дата, если вывод относится к конкретному дню.
- `SecId` / `ShortName` - выпуск, если вывод относится к конкретной бумаге.
- `CouponType` - тип выпуска, если вывод относится к сегменту.
- `Score`, `Value`, `NumTrades`, `YieldMove` - ключевые evidence values,
  если применимы.

Выводы строятся детерминированными правилами поверх `OfzActivityMetric` и
`OfzIssue`; UI не генерирует текстовые гипотезы самостоятельно.
