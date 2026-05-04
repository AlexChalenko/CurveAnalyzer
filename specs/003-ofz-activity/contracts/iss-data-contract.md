# Contract: MOEX ISS данные для активности ОФЗ

## Scope

Этот контракт фиксирует минимальные блоки и поля ISS, которые нужны feature
`003-ofz-activity`. Он описывает ожидания adapter layer, а не публичный API
приложения.

## Daily History Request

```text
GET https://iss.moex.com/iss/history/engines/stock/markets/bonds/boards/TQOB/securities.json
```

### Query Parameters

- `date=yyyy-MM-dd` - торговая дата для загрузки.
- `iss.meta=off` - без metadata, если не требуется динамическое сопоставление.
- `start=<index>` - pagination offset, если `history.cursor` показывает, что
  есть следующие страницы.
- `history.columns=...` - допустимо сузить поля до используемого набора.

### Required Blocks

- `history`
- `history.cursor`

### Required `history` Columns

- `BOARDID`
- `TRADEDATE`
- `SHORTNAME`
- `SECID`
- `NUMTRADES`
- `VALUE`
- `LOW`
- `HIGH`
- `CLOSE`
- `WAPRICE`
- `YIELDCLOSE`
- `OPEN`
- `VOLUME`
- `MATDATE`
- `DURATION`
- `YIELDATWAP`
- `COUPONPERCENT`
- `COUPONVALUE`
- `COUPONPERIOD`
- `COUPONDATE`
- `FACEVALUE`
- `INITIALFACEVALUE`
- `CURRENCYID`
- `FACEUNIT`
- `ZSPREAD`
- `ZSPREADATWAPRICE`
- `BONDTYPE`
- `BONDSUBTYPE`

### Pagination Rules

- Adapter MUST read `history.cursor` columns `INDEX`, `TOTAL`, `PAGESIZE`.
- Adapter MUST continue requests with `start = INDEX + PAGESIZE` while
  `start < TOTAL`.
- Adapter MUST tolerate `TOTAL = 0` as no-data day.

### Nullability Rules

- Numeric market fields can be null and MUST be parsed as nullable values:
  `VALUE`, `VOLUME`, prices, yields, duration and spreads.
- Rows with null `SECID` or `TRADEDATE` are invalid and MUST be skipped or
  reported as parse errors.

## Current-Day Snapshot Request

```text
GET https://iss.moex.com/iss/engines/stock/markets/bonds/boards/TQOB/securities.json?iss.only=securities,marketdata
```

### Used Blocks

- `securities` - issue metadata.
- `marketdata` - current trading metrics.

### Useful `securities` Columns

- `SECID`
- `SHORTNAME`
- `SECNAME`
- `MATDATE`
- `COUPONPERCENT`
- `COUPONVALUE`
- `COUPONPERIOD`
- `NEXTCOUPON`
- `FACEVALUE`
- `INITIALFACEVALUE`
- `FACEUNIT`
- `CURRENCYID`
- `ISIN`
- `ISSUENAME`
- `LISTLEVEL`
- `ISSUESIZE`
- `ISSUESIZEPLACED`
- `BONDTYPE`
- `BONDSUBTYPE`

### Useful `marketdata` Columns

- `SECID`
- `BID`
- `OFFER`
- `SPREAD`
- `LAST`
- `YIELD`
- `WAPRICE`
- `YIELDATWAPRICE`
- `NUMTRADES`
- `VOLTODAY`
- `VALTODAY`
- `DURATION`
- `ZSPREAD`
- `ZSPREADATWAPRICE`

## Aggregates Request

```text
GET https://iss.moex.com/iss/statistics/engines/stock/markets/bonds/aggregates.json
```

### Used Columns

- `TRADEDATE`
- `TYPE_BOND`
- `VOL_PRICE_TRADE_O`
- `VOL_PRICE_TRADE_R`
- `VOL_NOMINAL_O`
- `VOL_NOMINAL_R`
- `AVG_YEARS`

### Rule

Aggregates are optional context. Failure to load aggregates MUST NOT block top
anomalies, heatmap or issue detail workflows.

## Mapping Rules

- `SECID` maps to `OfzIssue.SecId` and `OfzDailyTrade.SecId`.
- `TRADEDATE` maps to `OfzDailyTrade.TradeDate`.
- `VALUE` maps to daily monetary turnover.
- `NUMTRADES` maps to daily trade count.
- `YIELDATWAP` is preferred yield for `YieldMove`; `YIELDCLOSE` is fallback.
- `WAPRICE` is preferred price for detail chart; `CLOSE` is fallback.
- `FACEUNIT` and `CURRENCYID` determine currency labels.
- `BONDTYPE` and `BONDSUBTYPE` are displayed as issue classification when
  available.
- `BONDTYPE`, `BONDSUBTYPE`, `SECNAME`, `ISSUENAME`, `FACEUNIT` and
  `CURRENCYID` determine normalized coupon/issue type:
  fixed, floating, inflation-linked, amortized, currency or unknown.
- `COUPONVALUE`, `COUPONPERCENT`, `COUPONPERIOD`, `COUPONDATE`/`NEXTCOUPON`,
  `FACEVALUE` and `INITIALFACEVALUE` are issue metadata. They can change for
  floating, inflation-linked or amortizing issues, therefore daily trade rows
  remain the source for market metrics while `OfzIssue` stores the latest known
  metadata snapshot.

## Error Handling

- Network failure for one date creates failed load state for that date and
  should allow retry.
- Parse failure for one row should not discard the entire page unless the JSON
  shape itself is invalid.
- Empty `history.data` with valid cursor is treated as no data for the date.
