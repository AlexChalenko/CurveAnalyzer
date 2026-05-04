# Contract: MOEX ISS данные ликвидности и spread по ОФЗ

## Scope

Контракт фиксирует поля ISS, которые нужны feature
`004-ofz-liquidity-spread`. Это contract adapter layer, а не публичный API
приложения.

## Daily History Request

```text
GET https://iss.moex.com/iss/history/engines/stock/markets/bonds/boards/TQOB/securities.json
```

### Query Parameters

- `date=yyyy-MM-dd`
- `iss.meta=off`
- `start=<index>` for pagination.
- `history.columns=...` may be narrowed to used columns.

### Required Blocks

- `history`
- `history.cursor`

### Liquidity/Spread `history` Columns

Existing activity columns remain required from feature `003-ofz-activity`.
Additional useful columns for this feature. ISS may omit some requested
columns from the returned `history.columns`; adapter code must treat them as
optional even when they are listed here:

- `BID`
- `OFFER`
- `HIGHBID`
- `LOWOFFER`
- `ZSPREAD`
- `ZSPREADATWAPRICE`
- `IRICPICLOSE`
- `BEICLOSE`
- `CBRCLOSE`
- `YIELDTOOFFER`
- `CALLOPTIONYIELD`
- `CALLOPTIONDURATION`
- `BONDTYPE`
- `BONDSUBTYPE`

### Rules

- Adapter MUST tolerate missing columns and null numeric values.
- `BID`, `OFFER`, `HIGHBID`, `LOWOFFER` are optional daily history quote
  context, not full order book. Live check on 2026-05-04 for date 2026-04-30
  returned `ZSPREAD`, `ZSPREADATWAPRICE` and type-specific columns, but did
  not return these quote columns in `history.columns`.
- `ZSPREAD` and `ZSPREADATWAPRICE` are historical spread context when provided.
- Null spread-like fields MUST stay null and must not be normalized to zero.

## Current-Day Snapshot Request

```text
GET https://iss.moex.com/iss/engines/stock/markets/bonds/boards/TQOB/securities.json?iss.only=securities,marketdata,marketdata_yields
```

### Useful `marketdata` Columns

- `SECID`
- `BID`
- `OFFER`
- `SPREAD`
- `BIDDEPTH`
- `OFFERDEPTH`
- `BIDDEPTHT`
- `OFFERDEPTHT`
- `NUMBIDS`
- `NUMOFFERS`
- `WAPRICE`
- `YIELDATWAPRICE`
- `DURATION`
- `NUMTRADES`
- `VOLTODAY`
- `VALTODAY`
- `ZSPREAD`
- `ZSPREADATWAPRICE`
- `IRICPICLOSE`
- `BEICLOSE`
- `CBRCLOSE`

### Useful `marketdata_yields` Columns

- `SECID`
- `EFFECTIVEYIELD`
- `DURATION`
- `ZSPREADBP`
- `GSPREADBP`
- `IR`
- `ICPI`
- `BEI`
- `CBR`
- `EFFECTIVEYIELDWAPRICE`
- `DURATIONWAPRICE`
- `YIELDTOOFFER`
- `YIELDLASTCOUPON`

### Rules

- Snapshot fields are current context and may be provisional.
- Snapshot-only depth and `marketdata_yields` fields MUST NOT be backfilled
  into historical dates.
- If current-day snapshot is saved, it MUST remain refreshable and replaceable
  by final history data on later launches.

## Pagination Rules

- Adapter MUST read `history.cursor` columns `INDEX`, `TOTAL`, `PAGESIZE`.
- Adapter MUST continue requests with `start = INDEX + PAGESIZE` while
  `start < TOTAL`.
- Adapter MUST tolerate `TOTAL = 0`.

## Mapping Rules

- `BID` maps to daily/snapshot best bid when present. In practice, current
  `marketdata` is the reliable bid source for MVP.
- `OFFER` maps to daily/snapshot best offer when present. In practice, current
  `marketdata` is the reliable offer source for MVP.
- `SPREAD` maps to current bid-ask spread if provided.
- `HIGHBID` and `LOWOFFER` map to optional daily high-bid / low-offer context
  when ISS returns those columns.
- `BIDDEPTHT` and `OFFERDEPTHT` map to current aggregate bid/offer depth.
- `ZSPREAD`, `ZSPREADATWAPRICE`, `ZSPREADBP`, `GSPREADBP` map to spread
  context, not primary liquidity score.
- `IR`/`IRICPICLOSE`, `BEI`/`BEICLOSE`, `CBR`/`CBRCLOSE`, `ICPI` are
  type-specific context for ОФЗ-ПК and ОФЗ-ИН.

## Error Handling

- Missing liquidity block must not block existing activity load.
- Parse failure for one row should skip/report that row, not discard the whole
  page when the JSON shape remains valid.
- Empty snapshot blocks create `MissingData` UI state, not chart crashes.
