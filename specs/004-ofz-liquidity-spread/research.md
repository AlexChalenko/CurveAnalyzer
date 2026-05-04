# Research: Liquidity & Spread Layer

## Decision: разделить historical liquidity и current snapshot

**Decision**: historical liquidity хранится в дневных rows только для полей,
доступных из ISS `history`; текущая глубина и `marketdata_yields` показываются
как snapshot context.

**Rationale**: ISS `history` содержит дневные spread-like поля, прежде всего
`ZSPREAD`/`ZSPREADATWAPRICE`, но live-проверка 2026-05-04 для даты 2026-04-30
не вернула `BID`/`OFFER`/`HIGHBID`/`LOWOFFER` в `history.columns`. Текущие
`BID`/`OFFER`/`SPREAD`, глубина (`BIDDEPTHT`, `OFFERDEPTHT`) и `GSPREADBP`
доступны в current snapshot blocks. Если смешать эти данные, UI будет
показывать текущую глубину как будто она относится ко всем прошлым датам.

**Alternatives considered**:

- Хранить все snapshot fields в `OfzDailyTrade` как дневную историю. Отклонено:
  будет неверная семантика.
- Не использовать snapshot-only fields. Отклонено: потеряем полезный текущий
  контекст ликвидности.

## Decision: первый релиз без intraday trades и order book history

**Decision**: MVP использует дневную гранулярность и current snapshot; intraday
trades/order book history остаются out of scope.

**Rationale**: существующий экран активности уже построен на дневной истории.
Полная история стакана резко увеличит объем данных, сложность хранения и
производительность UI, но не нужна для первого объяснимого слоя ликвидности.

**Alternatives considered**:

- Загружать intraday trades для точной оценки ликвидности. Отклонено как
  отдельная feature.
- Хранить локальные снимки стакана при каждом запуске. Отклонено: нерегулярная
  выборка будет вводить в заблуждение.

## Decision: missing spread/depth является отдельным статусом

**Decision**: отсутствующие `BID`, `OFFER`, `SPREAD`, depth or yield-spread
поля получают статус `MissingLiquidityData`/`SnapshotOnly`, а не значение `0`.

**Rationale**: нулевой или узкий spread имеет рыночный смысл, а null означает
отсутствие данных. Смешивание этих состояний приведет к ложным выводам о
хорошей ликвидности.

**Alternatives considered**:

- Нормализовать null to 0 для удобства графиков. Отклонено: нарушает смысл
  данных.
- Исключать выпуски с missing fields из UI. Отклонено: пользователь должен
  видеть, почему оценка невозможна.

## Decision: liquidity score должен быть explainable bucket score

**Decision**: liquidity score строится из простых правил и buckets: spread,
relative spread, depth availability, turnover and trade count. Score не
используется как инвестиционная рекомендация.

**Rationale**: пользователь должен понимать, почему выпуск попал в список
проблемной ликвидности. Черный ящик или ML-модель не подходят для первого
релиза.

**Alternatives considered**:

- Машинная модель классификации ликвидности. Отклонено: нет разметки и высокий
  риск переобучения.
- Только raw spread без score. Отклонено: сложно сравнивать выпуски разных
  типов и уровней активности.

## Decision: Z/G-spread использовать как контекст, а не главный ранжирующий фактор

**Decision**: `ZSPREAD`, `ZSPREADATWAPRICE`, `ZSPREADBP`, `GSPREADBP`
отображаются в детализации и scatter/context, но liquidity ranking в MVP
сначала опирается на bid-ask spread, depth, turnover and deals.

**Rationale**: Z/G-spread помогает понять relative value, но может быть
отсутствующим или типозависимым. Bid-ask spread и глубина ближе к вопросу
"можно ли нормально торговать эту бумагу".

**Alternatives considered**:

- Ранжировать сразу по Z-spread. Отклонено: это уже ближе к оценке
  привлекательности, а не ликвидности.

## Decision: UI расширяет существующую вкладку "Активность ОФЗ"

**Decision**: Liquidity & Spread Layer добавляется в существующий workflow:
детализация выпуска, таблица проблемной ликвидности, scatter/segments and
insights.

**Rationale**: пользователь уже выбирает диапазон, тип ОФЗ и signal scope в
этом экране. Новый отдельный экран увеличит навигационную сложность и будет
дублировать загрузку данных.

**Alternatives considered**:

- Отдельная вкладка "Ликвидность ОФЗ". Отклонено для MVP; может появиться
  позже, если слой станет самостоятельным рабочим процессом.

## Decision: сохранять текущую политику неполного дня

**Decision**: current-day snapshot остается provisional и должен
перезагружаться/заменяться final history data на последующих запусках.

**Rationale**: liquidity snapshot текущего дня может быть неполным. Он полезен
для текущего контекста, но не должен становиться неизменной историей.

**Alternatives considered**:

- Не сохранять текущий день вообще. Отклонено: пользователь теряет текущий
  контекст.
- Сохранять как обычную history row. Отклонено: риск stale partial data.
