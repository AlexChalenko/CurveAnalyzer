# Data Model: Сезонность активности ОФЗ

## OfzSeasonalityBucketKind

Тип сезонного bucket.

**Values**:

- `Weekday`: день недели.
- `Month`: календарный месяц.

## OfzSeasonalityObservation

Дневной агрегат активности выбранного набора выпусков.

**Fields**:

- `TradeDate`: дата торгов.
- `BucketKind`: `Weekday` или `Month`.
- `BucketKey`: стабильный ключ (`Monday`, `January`, `2026-05` не нужен).
- `BucketLabel`: отображаемое имя bucket.
- `TotalValue`: суммарный оборот дня, nullable.
- `TotalNumTrades`: суммарное число сделок, nullable.
- `ActiveIssueCount`: число выпусков с активностью.
- `ComparableIssueCount`: число выпусков с comparable yield movement.
- `YieldUpCount`, `YieldDownCount`, `UnchangedCount`.
- `IsProvisional`: true, если observation включает current snapshot.

**Validation rules**:

- Observation строится только из metrics с activity data.
- Missing numeric values остаются null.
- Нулевой оборот не считается активным днем.

## OfzSeasonalityBucket

Профиль сезонного bucket.

**Fields**:

- `Kind`, `Key`, `Label`, `SortOrder`.
- `ObservationCount`: число observation rows.
- `ActiveDayCount`: число rows с оборотом или сделками.
- `MedianTotalValue`, `AverageTotalValue`.
- `MedianNumTrades`.
- `MedianActiveIssueCount`.
- `UpDayShare`, `DownDayShare`.
- `BaselineQuality`: `Strong`, `Weak`, `Insufficient`.
- `Limitations`.

**Validation rules**:

- Bucket с малой историей не скрывается.
- `BaselineQuality = Insufficient` запрещает seasonal finding по этому bucket.
- Missing values не заменяются нулями.

## OfzSeasonalityFinding

Вывод о заметном отклонении activity day от сезонной базы.

**Fields**:

- `Kind`: `HighSeasonalActivity`, `LowSeasonalActivity`,
  `SeasonalityDataLimitation`.
- `TradeDate`.
- `BucketKind`, `BucketKey`, `BucketLabel`.
- `ActualValue`, `BaselineMedianValue`, `ValueRatio`.
- `ActualNumTrades`, `BaselineMedianNumTrades`.
- `BaselineObservationCount`.
- `Limitations`.

**Validation rules**:

- Baseline для finding использует только observations до `TradeDate`.
- Finding создается только при достаточной базе и threshold crossing.
- Текст не содержит recommendation language.

## OfzSeasonalityContext

Seasonality layer внутри `OfzMarketSummary`.

**Fields**:

- `StartDate`, `EndDate`.
- `MinWeekdayBaselineObservations`, `MinMonthBaselineObservations`.
- `HighActivityRatioThreshold`, `LowActivityRatioThreshold`.
- `WeekdayBuckets`.
- `MonthBuckets`.
- `Findings`.
- `Limitations`.

**Relationships**:

- `OfzMarketSummary` содержит `SeasonalityContext`.
- `OfzSummaryFinding` получает seasonality evidence fields.
- `OfzSummarySourceCounts` получает `SeasonalityObservations`.
