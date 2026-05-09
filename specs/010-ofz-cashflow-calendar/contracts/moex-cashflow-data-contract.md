# Contract: MOEX ISS cashflow data

## Primary endpoint

```text
GET https://iss.moex.com/iss/statistics/engines/stock/markets/bonds/bondization/{SECID}.json?iss.meta=off
```

## Required blocks

### coupons

Required columns:

- `secid`
- `coupondate`

Optional columns:

- `isin`
- `name`
- `issuevalue`
- `recorddate`
- `startdate`
- `initialfacevalue`
- `facevalue`
- `faceunit`
- `value`
- `valueprc`
- `value_rub`
- `primary_boardid`

Mapping:

- row -> `OfzCashflowEvent(EventType = Coupon)`
- `coupondate` -> `EventDate`
- `recorddate` -> `RecordDate`
- `startdate` -> `StartDate`
- `value` -> `Value`
- `valueprc` -> `ValuePercent`
- `value_rub` -> `ValueRub`

### amortizations

Required columns:

- `secid`
- `amortdate`

Optional columns:

- `isin`
- `name`
- `issuevalue`
- `facevalue`
- `initialfacevalue`
- `faceunit`
- `valueprc`
- `value`
- `value_rub`
- `data_source`
- `primary_boardid`

Mapping:

- `data_source = maturity` -> `EventType = Maturity`
- other rows -> `EventType = Amortization`
- `amortdate` -> `EventDate`

### offers

Required columns:

- `secid`
- `offerdate`

Optional columns:

- `isin`
- `name`
- `issuevalue`
- `offerdatestart`
- `offerdateend`
- `facevalue`
- `faceunit`
- `price`
- `value`
- `agent`
- `offertype`
- `primary_boardid`

Mapping:

- row -> `OfzCashflowEvent(EventType = Offer)`
- `offerdate` -> `EventDate`
- `offerdatestart` -> `StartDate`
- `offerdateend` -> `EndDate`
- `price` -> `Price`

## Error and missing-data rules

- Missing block is allowed and produces limitation.
- Missing date skips only that row with limitation.
- Missing numeric value stays null.
- Coupon `value`, `value_rub` and `valueprc` equal to `0` are treated as
  missing values, because future OFZ-PK coupons can be not fixed yet and ISS may
  expose that state as zero.
- For `RUB`/`SUR` events, missing `ValueRub` is filled from `Value` when
  `Value` exists.
- HTTP/network failure must not break existing activity workflow; it produces
  cashflow limitation.
