# Data Model: Календарь денежных потоков ОФЗ

## OfzCashflowEvent

Нормализованное событие выпуска.

**Fields**:

- `SecId`: код выпуска.
- `ShortName`: короткое имя выпуска, если доступно.
- `EventType`: `Coupon`, `Amortization`, `Maturity`, `Offer`, `Buyback`,
  `CallOption`, `PutOption`.
- `EventDate`: дата события.
- `StartDate`: начало купонного периода или offer window, nullable.
- `EndDate`: конец offer window, nullable.
- `RecordDate`: record date для купона, nullable.
- `Value`: сумма в валюте номинала, nullable.
- `ValueRub`: сумма в рублях, nullable.
- `ValuePercent`: процент от номинала, nullable.
- `FaceValue`: номинал на дату события, nullable.
- `InitialFaceValue`: начальный номинал, nullable.
- `FaceUnit`: валюта номинала.
- `Price`: цена оферты, nullable.
- `Agent`: агент оферты, если доступен.
- `OfferType`: тип оферты, если доступен.
- `SourceKind`: `Schedule`, `Snapshot`, `DescriptionFallback`.
- `SourceLabel`: raw block/source label для диагностики.
- `IsProvisional`: true для snapshot/fallback current-day данных.
- `Limitations`: ограничения события.

**Validation rules**:

- `SecId`, `EventType` и `EventDate` обязательны.
- Missing numeric values остаются null.
- Для coupon events нулевые `Value` / `ValueRub` / `ValuePercent` из ISS
  трактуются как отсутствующие значения: для ОФЗ-ПК будущий купон может быть
  еще не определен и приходить как `0`, что не должно отображаться как
  реальный нулевой денежный поток.
- Если `FaceUnit` является `RUB`/`SUR`, а `ValueRub` отсутствует при наличии
  `Value`, `ValueRub` заполняется из `Value`, чтобы не показывать `n/a` для
  рублевых выпусков.
- `Maturity` строится из `amortizations.data_source = maturity`.
- Snapshot/fallback events имеют `SourceKind != Schedule`,
  `IsProvisional = true` и aggregate limitation `SnapshotOnly`.

## OfzIssueCashflowCalendar

Календарь одного выпуска в контексте выбранного периода.

**Fields**:

- `SecId`, `ShortName`, `CouponType`, `CouponTypeMarker`.
- `Events`: все события, отсортированные по `EventDate`, затем `EventType`.
- `PeriodEvents`: события внутри выбранного периода.
- `NearPeriodEvents`: события в окне рядом с периодом.
- `NextEvent`: ближайшее событие на/после `endDate`.
- `PreviousEvent`: ближайшее событие до `startDate`.
- `Limitations`.

**Validation rules**:

- Пустой календарь не равен ошибке загрузки, но дает limitation.
- Несколько событий на дату сохраняются отдельными rows.

## OfzCashflowContext

Cashflow layer внутри `OfzMarketSummary`.

**Fields**:

- `StartDate`, `EndDate`, `EventWindowDays`.
- `IssueCalendars`: calendars по активным выпускам.
- `Events`: плоский список событий периода/near-period.
- `UpcomingEvents`: ближайшие события после `endDate`.
- `Findings`: cashflow-backed summary findings.
- `Limitations`.

**Validation rules**:

- Context строится только по активным выпускам выбранного периода.
- Event-near-activity findings используют issue-level activity date, не общий
  market day без `SECID`.

## OfzCashflowFinding

Вывод, связывающий активность выпуска с событием.

**Fields**:

- `Kind`: `ActivityNearCoupon`, `ActivityNearOffer`,
  `ActivityNearMaturity`, `ActivityNearAmortization`,
  `UpcomingCashflowEvent`, `CashflowDataLimitation`.
- `SecId`, `ShortName`.
- `TradeDate`: дата activity fact.
- `EventDate`: дата события.
- `EventType`.
- `DaysToEvent`: signed distance in days.
- `Value`, `ValueRub`, `ValuePercent`.
- `Evidence`: turnover/trades/yield move + event fields.
- `Limitations`.

**Validation rules**:

- `DaysToEvent` считается как `EventDate - TradeDate`.
- Finding создается только при `Abs(DaysToEvent) <= EventWindowDays`.
- Текст не содержит recommendation language.

## Relationships

- `OfzMarketSummary` содержит `CashflowContext` и cashflow fields в
  `OfzFindingEvidence`.
- `OfzCashflowContext` строится из `OfzCashflowEvent` и issue-level activity
  rows.
- Repository хранит normalized `OfzCashflowEvent`; builder не зависит от EF.
