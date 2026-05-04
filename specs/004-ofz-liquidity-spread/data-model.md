# Data Model: Liquidity & Spread Layer

## Entity: OfzDailyTrade

Существующая дневная запись торгов расширяется историческими полями
ликвидности и spread.

### New Fields

- `Bid` - котировка покупки на конец/момент дневной history record,
  опционально.
- `Offer` - котировка продажи на конец/момент дневной history record,
  опционально.
- `Spread` - bid-ask spread в процентах к номиналу, если предоставлен ISS,
  опционально.
- `HighBid` - максимальная котировка покупки за сессию, опционально.
- `LowOffer` - минимальная котировка продажи за сессию, опционально.
- `ZSpread` - Z-spread по последней сделке или history value, опционально.
- `ZSpreadAtWeightedAveragePrice` - Z-spread по средневзвешенной цене,
  опционально.
- `ImpliedFloatingRate` - implied floating/RUONIA value для ОФЗ-ПК, если
  доступно, опционально.
- `ImpliedInflation` - implied inflation/BEI value для ОФЗ-ИН, если доступно,
  опционально.
- `ImpliedCbrRate` - implied CBR value, если доступно, опционально.

### Validation Rules

- `Bid`, `Offer`, `Spread`, depth and spread fields are nullable.
- Missing value не нормализуется в `0`.
- Если `Spread` отсутствует, но `Bid` и `Offer` положительные, display layer
  может показывать calculated spread как derived value без сохранения
  исходного ISS поля.
- `Bid <= 0`, `Offer <= 0`, `Spread < 0` не участвуют в уверенном liquidity
  score.

## Entity: OfzLiquiditySnapshot

Текущий snapshot ликвидности по выпуску. Не является полной историей.

### Fields

- `SecId` - код выпуска.
- `BoardId` - режим торгов, ожидается `TQOB`.
- `ObservedAt` - момент локальной загрузки snapshot.
- `TradeDate` - торговая дата snapshot.
- `Bid` - лучшая покупка, опционально.
- `Offer` - лучшая продажа, опционально.
- `Spread` - текущий bid-ask spread, опционально.
- `BidDepth` - объем лучшей покупки, если доступен, опционально.
- `OfferDepth` - объем лучшей продажи, если доступен, опционально.
- `BidDepthTotal` - совокупный спрос, если доступен, опционально.
- `OfferDepthTotal` - совокупное предложение, если доступно, опционально.
- `NumBids` - количество заявок на покупку, если доступно, опционально.
- `NumOffers` - количество заявок на продажу, если доступно, опционально.
- `ValueToday` - текущий оборот, опционально.
- `VolumeToday` - текущий объем в бумагах, опционально.
- `NumTrades` - текущие сделки, опционально.
- `EffectiveYield` - эффективная доходность из `marketdata_yields`,
  опционально.
- `EffectiveYieldAtWeightedAveragePrice` - эффективная доходность по WAPRICE,
  опционально.
- `Duration` - текущая дюрация, опционально.
- `DurationAtWeightedAveragePrice` - дюрация по WAPRICE, опционально.
- `ZSpreadBp` - Z-spread in basis points, опционально.
- `GSpreadBp` - G-spread in basis points, опционально.
- `IsProvisional` - true для current-day snapshot.

### Validation Rules

- Snapshot is current context. Он не должен использоваться как historical row
  для прошлых дат без явной пометки.
- `ObservedAt` обязателен.
- Snapshot replacement is idempotent by `SecId`, `BoardId`, `TradeDate`.

## Entity: OfzLiquidityMetric

Расчетное состояние ликвидности по выпуску и дате/scope.

### Fields

- `SecId`
- `TradeDate`
- `Bid`
- `Offer`
- `Spread`
- `SpreadSource` - `Provided`, `CalculatedFromBidOffer`, `Missing`.
- `BidDepthTotal`
- `OfferDepthTotal`
- `Value`
- `NumTrades`
- `ZSpread`
- `ZSpreadBp`
- `GSpreadBp`
- `LiquidityScore` - explainable score, чем выше, тем хуже/дороже
  ликвидность.
- `LiquidityBucket` - `Good`, `Normal`, `Weak`, `Problem`, `MissingData`.
- `Status` - `Ready`, `MissingQuotes`, `MissingDepth`, `SnapshotOnly`,
  `InsufficientActivity`, `NoData`.

### Validation Rules

- `LiquidityScore` рассчитывается только если есть достаточные quote/spread
  данные.
- Missing quotes/depth не должны трактоваться как хорошая ликвидность.
- Ranking по проблемной ликвидности сортирует сначала `Problem`, затем
  `Weak`, затем score/value evidence.

## Entity: OfzSpreadSignal

Детерминированный сигнал spread/liquidity.

### Fields

- `Kind` - `WideSpread`, `ActivityWithWideSpread`, `MissingQuotesOnActiveDay`,
  `ImprovingSpread`, `DepthImbalance`, `SpreadOutlier`, `ZSpreadContext`.
- `Severity`
- `SecId`
- `ShortName`
- `TradeDate`
- `CouponType`
- `Value`
- `NumTrades`
- `Spread`
- `LiquidityScore`
- `ZSpread`
- `ZSpreadBp`
- `GSpreadBp`
- `Text`

### Validation Rules

- Signal must include numeric evidence when available.
- Signal text must not recommend buy/sell actions.
- Signals honor selected type filter and signal scope.

## Derived Views

### Issue liquidity profile

`OfzIssueLiquidityProfile` объединяет выбранный выпуск, daily activity,
historical spread fields and current snapshot context. Используется в
детализации выпуска.

### Weak liquidity list

Ранжированный список выпусков с широким spread, низкой глубиной, малым
количеством сделок или missing quote data during active trading.

### Liquidity-enhanced duration/yield scatter

Расширение `OfzDurationYieldScatterPoint`: X = duration, Y = yield,
размер = turnover/activity, цвет = liquidity/spread bucket.

### Liquidity insights

Список `OfzSpreadSignal`, отобранный и отсортированный для блока "Выводы".
