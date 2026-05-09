# Research: Календарь денежных потоков ОФЗ

## MOEX ISS источники

### Primary event calendar

Endpoint:

```text
https://iss.moex.com/iss/statistics/engines/stock/markets/bonds/bondization/{SECID}.json
```

Проверено на `SU26238RMFS4` 2026-05-08. Endpoint возвращает blocks:

- `coupons`: `isin`, `name`, `issuevalue`, `coupondate`, `recorddate`,
  `startdate`, `initialfacevalue`, `facevalue`, `faceunit`, `value`,
  `valueprc`, `value_rub`, `secid`, `primary_boardid`.
- `amortizations`: `isin`, `name`, `issuevalue`, `amortdate`, `facevalue`,
  `initialfacevalue`, `faceunit`, `valueprc`, `value`, `value_rub`,
  `data_source`, `secid`, `primary_boardid`.
- `offers`: `isin`, `name`, `issuevalue`, `offerdate`, `offerdatestart`,
  `offerdateend`, `facevalue`, `faceunit`, `price`, `value`, `agent`,
  `offertype`, `secid`, `primary_boardid`.

Decision: использовать `bondization/{SECID}` как primary source для календаря,
потому что это единственный найденный ISS endpoint с полным набором coupon,
amortization/maturity и offer events.

### Current/fallback metadata

Endpoint:

```text
https://iss.moex.com/iss/engines/stock/markets/bonds/boards/TQOB/securities/{SECID}.json?iss.meta=off&iss.only=securities,marketdata
```

Useful fields: `NEXTCOUPON`, `COUPONVALUE`, `COUPONPERCENT`, `COUPONPERIOD`,
`MATDATE`, `OFFERDATE`, `BUYBACKDATE`, `CALLOPTIONDATE`, `PUTOPTIONDATE`,
`FACEVALUE`, `FACEUNIT`, `ACCRUEDINT`, `LOTVALUE`.

Decision: использовать как fallback/context, если full schedule отсутствует или
для опциональных событий, которых нет в `bondization`.

### Description metadata

Endpoint:

```text
https://iss.moex.com/iss/securities/{SECID}.json?iss.meta=off&iss.only=description&description.columns=name,title,value
```

Useful fields: `ISSUEDATE`, `MATDATE`, `INITIALFACEVALUE`, `FACEUNIT`,
`ISSUESIZE`, `FACEVALUE`, `COUPONFREQUENCY`, `COUPONDATE`, `COUPONPERCENT`,
`COUPONVALUE`.

Decision: использовать только для metadata enrichment или fallback limitations,
не для primary calendar.

## Event classification

- `coupons.*.coupondate` -> `Coupon`.
- `amortizations.*.amortdate` + `data_source = maturity` -> `Maturity`.
- `amortizations.*.amortdate` + другое/пустое `data_source` ->
  `Amortization`.
- `offers.*.offerdate` -> `Offer`.
- `BUYBACKDATE` -> `Buyback`.
- `CALLOPTIONDATE` -> `CallOption`.
- `PUTOPTIONDATE` -> `PutOption`.

## Event window

Decision: default event-near-activity window `3` календарных дня.

Rationale: купонные/offer события могут влиять на оборот до и после даты
события; слишком широкое окно будет давать случайные совпадения.

## Cache

Decision: хранить нормализованные events в SQLite.

Rationale:

- экран активности уже использует локальный cache;
- события редко меняются, но повторный сетевой вызов per `SECID` ухудшит UX;
- cache позволяет строить summary без повторного обращения к ISS.

## Risks

- `bondization` может вернуть пустой календарь для отдельных выпусков.
- Часть полей суммы/процента отсутствует.
- Future events и current metadata могут быть предварительными.
- Календарное совпадение не доказывает причинность, поэтому тексты должны быть
  нейтральными.
