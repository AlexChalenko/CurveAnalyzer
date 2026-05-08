# Contract: MOEX ISS данные для индексного контекста ОФЗ

## Источник истории

V1 использует MOEX ISS history endpoint:

```text
GET https://iss.moex.com/iss/history/engines/stock/markets/index/securities/{SECID}.json
```

Параметры:

- `from=YYYY-MM-DD`
- `till=YYYY-MM-DD`
- `iss.meta=off`

Минимальные expected blocks:

- `history.columns`
- `history.data`

Минимальные поля:

- `SECID`
- `TRADEDATE`
- `SHORTNAME`
- `NAME`
- `CLOSE`
- `OPEN`
- `HIGH`
- `LOW`
- `VALUE`
- `DURATION`
- `YIELD`
- `CURRENCYID`
- `TRADE_SESSION_DATE`
- `RECALC_DATE`

Правила parsing:

- Decimal values parse invariantly from JSON numbers.
- Missing/empty values map to null.
- `TRADEDATE` is the primary daily key.
- `TRADE_SESSION_DATE` and `RECALC_DATE` may be stored for diagnostics, but do
  not replace `TRADEDATE` in daily alignment.
- Rows outside requested range are ignored.

## Источник текущего snapshot

V1 может использовать current endpoint только для current-day provisional
context:

```text
GET https://iss.moex.com/iss/engines/stock/markets/index/securities/{SECID}.json
```

Expected blocks:

- `securities`
- `marketdata`

Useful `marketdata` fields:

- `SECID`
- `CURRENTVALUE`
- `LASTVALUE`
- `OPENVALUE`
- `LASTCHANGE`
- `LASTCHANGEPRC`
- `LASTCHANGETOOPEN`
- `LASTCHANGETOOPENPRC`
- `HIGH`
- `LOW`
- `VALTODAY`
- `VOLTODAY`
- `TRADEDATE`
- `TIME`
- `SYSTIME`
- `TRADE_SESSION_DATE`

Snapshot rules:

- Snapshot values are `SourceKind = Snapshot`.
- Snapshot values are `IsProvisional = true`.
- Snapshot values do not silently overwrite historical `SourceKind = History`
  rows.
- If both history and snapshot exist for the same date, UI must identify which
  source was used for current context.

## Required series

Whole-market:

- `RGBI`
- `RGBITR`

Duration segments:

- `RUGBICP1Y`
- `RUGBICP3Y`
- `RUGBICP5Y`
- `RUGBICP7Y+`
- `RUGBITR1Y`
- `RUGBITR3Y`
- `RUGBITR5Y`
- `RUGBITR7Y+`

## Data-quality rules

- Missing `CLOSE`, `YIELD`, `DURATION` or `VALUE` is null, not zero.
- Missing previous point means daily change is unavailable.
- Missing required whole-market series creates `MissingRequiredSeries`
  limitation.
- Missing optional segment series creates segment limitation but does not hide
  whole-market context.
- Current-day data is preliminary until history returns a final row.
