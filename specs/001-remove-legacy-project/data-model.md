# Data Model: supported CurveAnalyzer domain

Этот файл описывает целевую доменную модель для migration plan. Это не схема БД
и не обязательные имена классов; точные имена фиксируются в tasks/implementation.

## TradingDate

**Назначение**: рыночная дата, для которой существуют или запрашиваются ZCYC
данные.

**Поля**:

- `Date`: календарная дата без времени.

**Правила**:

- Сравнение выполняется по date-only значению.
- DateTime с ненулевым time component должен нормализоваться или отклоняться на
  границе создания.

## CurvePeriod

**Назначение**: tenor/duration кривой, по которому выбирается доходность.

**Поля**:

- `Value`: положительное numeric значение period.

**Правила**:

- Значение должно быть больше 0.
- Для spread нужны два разных period.

## YieldPoint

**Назначение**: доходность для одного period на одной trading date.

**Поля**:

- `TradingDate`
- `CurvePeriod`
- `Value`

**Правила**:

- Пара `(TradingDate, CurvePeriod)` уникальна в historical storage.
- `Value` хранится как numeric yield без UI formatting.

## YieldCurve

**Назначение**: набор yield points на одну trading date.

**Поля**:

- `TradingDate`
- `Points`: ordered collection of `YieldPoint`

**Правила**:

- Все points принадлежат одной trading date.
- Periods внутри curve не должны повторяться.
- Empty curve допустима как result "нет данных", но UI должен обрабатывать это
  явно.

## HistoricalSeries

**Назначение**: значения по одному period на множестве trading dates.

**Поля**:

- `CurvePeriod`
- `Points`: ordered collection of `HistoricalPoint`

**Правила**:

- Points упорядочиваются по trading date.
- Duplicate trading dates не допускаются.

## HistoricalPoint

**Назначение**: значение historical series для одной trading date.

**Поля**:

- `TradingDate`
- `Value`

## SpreadSeries

**Назначение**: разница между двумя historical series, выровненными по trading
date.

**Поля**:

- `FirstPeriod`
- `SecondPeriod`
- `Points`: ordered collection of `(TradingDate, SpreadValue)`

**Правила**:

- `FirstPeriod` и `SecondPeriod` должны отличаться.
- Spread считается только для дат, присутствующих в обеих input series.
- Formula remains current behavior: `second.Value - first.Value`.

## SpreadPoint

**Назначение**: calculated spread value для одной trading date.

**Поля**:

- `TradingDate`
- `Value`

## RateOhlcPoint

**Назначение**: weekly OHLC bucket для `Rate Change`.

**Поля**:

- `YearWeek`
- `StartDate`
- `Open`
- `High`
- `Low`
- `Close`

**Правила**:

- Input points сортируются по trading date до расчета.
- `Open` берется из первой точки недели, `Close` из последней.
- `High`/`Low` считаются по всем точкам недели.

## SyncProgress

**Назначение**: UI-neutral progress для синхронизации historical data.

**Поля**:

- `CompletedCount`
- `TotalCount`
- `CurrentDate`
- `Ratio`

**Правила**:

- `Ratio` находится в диапазоне 0..1.
- При отсутствии work progress должен завершаться без division by zero.

## Persistence Entity: ZcycRecord

**Назначение**: infrastructure representation для SQLite.

**Поля**:

- `Id` или existing `Num`
- `TradingDate`
- `Period`
- `Value`

**Правила**:

- Unique index on `(TradingDate, Period)`.
- Entity не должна протекать в UI или domain calculations как единственный
  доменный тип.

## State Transitions

```text
MOEX XML row
  -> ApiServices DTO
  -> YieldPoint/YieldCurve
  -> Application use case result
  -> ViewModel state
  -> Chart rendering

SQLite ZcycRecord
  -> Repository mapping
  -> YieldPoint/YieldCurve/HistoricalSeries
```
