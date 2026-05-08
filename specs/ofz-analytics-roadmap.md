# Roadmap аналитики ОФЗ

**Дата обновления**: 2026-05-08
**Контекст**: идеи из `Curve Analyzer.md`, `Curve Analyzer v2.md`,
реализованный экран `Активность ОФЗ`, проверки доступных полей MOEX ISS и
закрытые Spec Kit features до `009-ofz-index-context`.

## Что уже реализовано

- Рейтинг всплесков торгов по ОФЗ относительно собственной исторической базы.
- Heatmap активности выпусков по датам.
- Детализация выпуска: оборот, сделки, цена, доходность.
- Сегментный обзор: индекс активности и распределение `дюрация / доходность`.
- Классификация типов ОФЗ: ОФЗ-ПД, ОФЗ-ПК, ОФЗ-ИН, ОФЗ-АД, валютные,
  `Unknown`.
- Фильтр типа ОФЗ, фильтр периода выводов и фильтр сигналов
  `Все дни` / `Последний день`.
- Выводы по активности без заглядывания в будущее: score считается от
  предыдущих доступных торговых записей.
- Политика текущего дня: intraday snapshot не считается финальной историей.
- Liquidity & Spread layer: bid-ask spread, weak liquidity, Z/G-spread evidence,
  разделение historical metrics и current snapshot.
- Специализированная аналитика ОФЗ-ПК и ОФЗ-ИН: implied floating rate,
  key-rate context/fallback, implied inflation, availability/limitations.
- Market breadth: рост/снижение доходности, сравнимые/несравнимые выпуски,
  концентрация top-5/top-10, доли оборота по типам ОФЗ.
- Индексный контекст MOEX: RGBI, RGBITR, duration-сегменты, history/snapshot
  source, provisional markers, index-backed findings.
- Structured summary JSON schema `1.3`: date-only поля для торговых дат,
  index context, breadth, special metrics, source counts.
- UX для JSON: копирование в clipboard и сохранение structured summary JSON в
  `.json` файл.

## Закрытые этапы Spec Kit

1. `004-ofz-liquidity-spread`: bid-ask spread, глубина, Z/G-spread,
   liquidity insights.
2. `005-ofz-analytics-summary`: ranked findings, segment summary,
   structured JSON, overview modes.
3. `006-ofz-market-breadth`: breadth, концентрация оборота, агрегаты по типам.
4. `007-ofz-floaters-linkers`: классификация типов ОФЗ и normalized type filter.
5. `008-ofz-pk-in-analytics`: отдельная аналитика ОФЗ-ПК и ОФЗ-ИН.
6. `009-ofz-index-context`: RGBI/RGBITR и duration segment context.

## Кандидаты на следующие этапы

### 1. Календарь купонов, оферт и погашений

**Приоритет**: высокий. Это следующий лучший слой после активности,
ликвидности, breadth и индексов, потому что он объясняет часть всплесков
конкретными событиями выпуска.

**Предлагаемый feature id**: `010-ofz-cashflow-calendar`.

**Идея**: объяснять всплески торгов календарными событиями выпуска и показывать
ближайшие события в контексте выбранного периода.

**Данные**:

- `NEXTCOUPON`, `COUPONVALUE`, `COUPONPERCENT`, `COUPONPERIOD`;
- `MATDATE`, `OFFERDATE`, `BUYBACKDATE`, `CALLOPTIONDATE`,
  `PUTOPTIONDATE`;
- `FACEVALUE`, `ISSUESIZE`, `ISSUESIZEPLACED`;
- при необходимости `ACCRUEDINT`, `LOTVALUE`, `FACEUNIT`.

**Визуализации**:

- timeline событий по выбранному выпуску;
- маркеры купонов/оферт/погашений на графике активности;
- таблица ближайших событий по активным выпускам;
- выводы вида "всплеск рядом с купоном/офертой/погашением" без
  инвестиционных рекомендаций.

**Риск**:

- календарь объясняет часть активности, но не заменяет анализ ликвидности;
- для некоторых выпусков события могут отсутствовать или быть snapshot-only;
- нужно явно отделять фактические события от интерпретации причины движения.

### 2. Сезонность торгов

**Приоритет**: низкий.

**Предлагаемый feature id**: `011-ofz-seasonality`.

**Идея**: проверить, есть ли повторяемые паттерны активности по дням недели,
месяцам, налоговым периодам и концу года.

**Визуализации**:

- seasonality heatmap `месяц / день`;
- средняя активность по дням недели;
- сравнение текущего периода с историческим средним.

**Риск**: нужна более длинная история и аккуратная интерпретация, иначе легко
получить шум.

### 3. Корреляции с внешними факторами

**Приоритет**: низкий.

**Предлагаемый feature id**: `012-ofz-external-factors`.

**Идея**: сравнивать доходности/активность ОФЗ с ключевой ставкой, инфляцией,
курсами и товарными индикаторами.

**Возможные источники**:

- Банк России для ключевой ставки и инфляционных рядов;
- MOEX/ISS для индексов и валютных данных.

**Риск**: это расширяет область проекта за пределы MOEX bonds workflow и
требует отдельной модели качества данных.

### 4. Рейтинг привлекательности, прогнозы и стресс-тесты

**Приоритет**: отложить.

**Идея**: объединять доходность, ликвидность, spread, календарь и сценарии
ставок.

**Почему не сейчас**:

- высок риск превратить аналитический экран в инвестиционную рекомендацию;
- нужны более строгие правила, дисклеймеры и тестирование модели;
- текущая полезность выше у объяснимых метрик ликвидности, spread, breadth,
  index context и календарных событий.

## Рекомендуемый порядок

1. `010-ofz-cashflow-calendar`: купоны, оферты, погашения, ближайшие события.
2. `011-ofz-seasonality`: сезонность активности при наличии достаточной
   истории.
3. `012-ofz-external-factors`: внешние факторы только после стабилизации
   bond workflow.
4. Рейтинг/прогнозы/стресс-тесты оставить за пределами ближайшего roadmap.

## Проверенные источники данных

- MOEX ISS reference: <https://iss.moex.com/iss/reference/>
- TQOB current securities/marketdata columns:
  <https://iss.moex.com/iss/engines/stock/markets/bonds/boards/TQOB/securities/columns.json>
- TQOB history columns:
  <https://iss.moex.com/iss/history/engines/stock/markets/bonds/boards/TQOB/securities/columns.json>
- MOEX bond aggregates:
  <https://iss.moex.com/iss/statistics/engines/stock/markets/bonds/aggregates.json>
- MOEX RGBI: <https://www.moex.com/ru/index/RGBI>
- MOEX RGBITR: <https://www.moex.com/a5865>
