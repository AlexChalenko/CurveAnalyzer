# Roadmap аналитики ОФЗ

**Дата обновления**: 2026-05-10
**Контекст**: идеи из `Curve Analyzer.md`, `Curve Analyzer v2.md`,
реализованный экран `Активность ОФЗ`, проверки доступных полей MOEX ISS и
закрытые Spec Kit features до `013-ofz-summary-quality-gates`.

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
- Календарь денежных потоков ОФЗ: купоны, амортизации, погашения, оферты,
  snapshot fallback, ближайшие события и связь активности с событиями выпуска.
- Сезонность активности: weekday/month profiles, no-lookahead seasonal
  baseline, high/low seasonal activity findings и limitations для короткой
  истории.
- Внешние факторы активности: ключевая ставка ЦБ, MOEX index context и
  OFZ-derived special metrics как нейтральный фон, no-lookahead alignment,
  factor links и limitations без подстановки нулей.
- Structured summary JSON schema `1.6`: date-only поля для торговых дат,
  index context, breadth, special metrics, cashflow context, seasonality
  context, external factors context, source counts.
- Quality gates для summary/export contract: автоматические проверки обязательных
  JSON-разделов, `sourceCounts`, date-only дат, сохранения важных findings и
  политики missing/snapshot/provisional данных.
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
7. `010-ofz-cashflow-calendar`: календарь купонов, амортизаций, погашений,
   оферт и cashflow-backed context.
8. `011-ofz-seasonality`: weekday/month сезонность активности, baseline без
   заглядывания в будущее и structured `seasonalityContext`.
9. `012-ofz-external-factors`: внешний факторный слой для активности ОФЗ,
   `externalFactorsContext`, factor-backed findings и JSON schema `1.6`.
10. `013-ofz-summary-quality-gates`: тестовый quality-gate слой для
    `OfzMarketSummary`/JSON export после 006-012 без изменения боевой аналитики.

## Кандидаты на следующие этапы

### 1. Рейтинг привлекательности, прогнозы и стресс-тесты

**Приоритет**: отложить.

**Идея**: объединять доходность, ликвидность, spread, календарь и сценарии
ставок.

**Почему не сейчас**:

- высок риск превратить аналитический экран в инвестиционную рекомендацию;
- нужны более строгие правила, дисклеймеры и тестирование модели;
- текущая полезность выше у объяснимых метрик ликвидности, spread, breadth,
  index context и календарных событий.

## Рекомендуемый порядок

1. После `013-ofz-summary-quality-gates` использовать новый test gate как
   обязательную проверку перед следующими расширениями summary/export.
2. Рейтинг/прогнозы/стресс-тесты оставить за пределами ближайшего roadmap.

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
