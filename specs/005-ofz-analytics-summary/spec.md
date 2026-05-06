# Feature Specification: OFZ Analytics Summary

**Feature Branch**: `005-ofz-analytics-summary`  
**Created**: 2026-05-04  
**Status**: Draft  
**Input**: User description: "Аналитический слой и сводки по данным ОФЗ: поверх уже загруженных activity/liquidity/spread данных пользователь получает короткую объяснимую картину рынка за выбранный период, список ключевых сигналов, drill-down к выпускам/сегментам и машиночитаемый summary-контракт, пригодный для будущего MCP/API слоя. В v1 без инвестиционных рекомендаций и без новых внешних источников."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Быстро понять картину рынка (Priority: P1)

Пользователь открывает аналитику ОФЗ за выбранный период и видит короткую
сводку: какие сегменты были наиболее активны, где ухудшилась или улучшилась
ликвидность, какие даты выделяются, и есть ли значимые движения доходности.

**Why this priority**: Это основной смысл feature. Без верхнеуровневой сводки
пользователь снова вынужден вручную читать heatmap, таблицы и графики.

**Independent Test**: Можно загрузить диапазон с уже имеющимися activity и
liquidity данными и проверить, что экран показывает 3-7 приоритетных выводов с
числовым evidence и ссылкой на дату/сегмент/выпуск.

**Acceptance Scenarios**:

1. **Given** выбран период с данными ОФЗ, **When** пользователь открывает
   сводку, **Then** он видит ранжированный список ключевых наблюдений с датами,
   сегментами, выпусками и числовыми основаниями.
2. **Given** в периоде есть активность, но нет надежных bid/offer данных,
   **When** строится сводка, **Then** выводы явно отделяют activity-сигналы от
   liquidity/spread-сигналов и не подставляют нулевой spread.

---

### User Story 2 - Разобрать причину сигнала (Priority: P2)

Пользователь выбирает конкретный вывод и видит, какие данные его сформировали:
выпуск или сегмент, дата, оборот, сделки, score, yield move, spread/liquidity
status, и почему этот вывод выше остальных.

**Why this priority**: Выводы должны быть проверяемыми. Пользователь не должен
доверять черному ящику или гадать, из какой таблицы взялась формулировка.

**Independent Test**: Можно выбрать любой вывод и убедиться, что он раскрывает
evidence без перехода к внешним источникам.

**Acceptance Scenarios**:

1. **Given** сводка содержит вывод "активная дата", **When** пользователь
   раскрывает вывод, **Then** он видит количество активных выпусков, суммарный
   оборот, максимальный/медианный score и ссылку на дату в heatmap.
2. **Given** сводка содержит liquidity/spread вывод, **When** пользователь
   раскрывает вывод, **Then** он видит spread source, bucket/status, оборот,
   сделки и пометку snapshot-only, если вывод основан на текущем snapshot.

---

### User Story 3 - Сравнить сегменты и выпуски (Priority: P3)

Пользователь смотрит не только отдельные аномалии, но и агрегированную картину
по типам ОФЗ: ОФЗ-ПД, ОФЗ-ПК, ОФЗ-ИН, ОФЗ-АД и валютные выпуски. Для каждого
сегмента видны активность, ликвидность, движение доходности и ведущие выпуски.

**Why this priority**: Сегментный слой помогает отличить единичный всплеск от
структурного движения внутри типа бумаг.

**Independent Test**: Можно выбрать период и проверить, что сегментные итоги
суммируют только выпуски выбранного типа или явно показывают "Все", не смешивая
валютные выпуски с рублевыми без пометки.

**Acceptance Scenarios**:

1. **Given** выбран тип "ОФЗ-ПД", **When** пользователь смотрит сегментную
   сводку, **Then** итоги, лидеры и выводы считаются только по ОФЗ-ПД.
2. **Given** выбран тип "Все", **When** в данных есть валютные выпуски,
   **Then** валютные выпуски помечены отдельно и не искажают рублевые выводы.

---

### User Story 4 - Получить машиночитаемую сводку (Priority: P4)

Пользователь или будущий локальный агент может получить тот же набор выводов в
структурированном виде: период, фильтры, список summary cards, evidence и
ограничения данных.

**Why this priority**: Это готовит основу для будущего MCP/API слоя, но не
требует в v1 поднимать отдельный сервер.

**Independent Test**: Можно построить сводку и проверить, что каждая карточка
имеет стабильный тип, приоритет, текст, evidence и ссылки на исходные
выпуски/сегменты/даты.

**Acceptance Scenarios**:

1. **Given** сводка построена в UI, **When** запрашивается структурированное
   представление, **Then** оно содержит те же выводы и evidence, что видит
   пользователь.
2. **Given** часть данных отсутствует или является snapshot-only, **When**
   формируется structured summary, **Then** ограничение данных явно записано в
   summary metadata.

---

### Edge Cases

- В выбранном периоде нет данных ОФЗ или загружены только пустые торговые дни.
- Период включает текущий незавершенный день с provisional snapshot.
- Для части выпусков отсутствуют bid/offer/spread, но есть оборот и сделки.
- Один выпуск доминирует по обороту и может скрыть сегментную картину.
- Валютные выпуски попадают в общий фильтр вместе с рублевыми ОФЗ.
- Выводы противоречат друг другу: высокая активность при слабой ликвидности,
  сужение spread при падении цены, или движение доходности без оборота.
- Диапазон слишком короткий для надежного baseline.
- Пользователь меняет фильтры после построения сводки; старые выводы не должны
  оставаться на экране как актуальные.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST build a market summary for the selected OFZ period
  using already available activity, liquidity, spread, yield and issue metadata.
- **FR-002**: System MUST rank summary findings by importance and show no more
  than a concise readable set by default.
- **FR-003**: Each summary finding MUST include numeric evidence sufficient to
  verify why it was generated.
- **FR-004**: Each summary finding MUST identify its scope: market, segment,
  issue, date, liquidity, spread, yield move, or data-quality limitation.
- **FR-005**: System MUST avoid recommendation language such as buy, sell,
  hold, fair value, target, or advice.
- **FR-006**: System MUST explicitly mark provisional/current-day and
  snapshot-only observations.
- **FR-007**: System MUST keep missing data separate from zero values and must
  expose missing-data limitations in summary text and structured output.
- **FR-008**: Users MUST be able to filter summary output by the same OFZ type
  filters used on the activity screen.
- **FR-009**: Users MUST be able to drill down from a summary finding to the
  relevant issue, segment, date or existing chart/table context.
- **FR-010**: System MUST provide segment-level summaries for at least fixed,
  floating, inflation-linked, amortized and currency OFZ groups when data is
  available.
- **FR-011**: System MUST provide a structured summary representation with
  stable finding type, priority, display text, evidence fields and data
  limitations.
- **FR-012**: System MUST show a clear empty or insufficient-data state when the
  selected period cannot support reliable analytics.
- **FR-013**: System MUST preserve existing activity, liquidity and spread
  workflows while adding the summary layer.

### Key Entities *(include if feature involves data)*

- **MarketSummary**: Итоговая сводка за выбранный период и фильтры; содержит
  список выводов, summary metadata и ограничения данных.
- **SummaryFinding**: Один объяснимый вывод с типом, приоритетом, текстом,
  scope и evidence.
- **FindingEvidence**: Числовые основания вывода: даты, SECID, тип ОФЗ,
  оборот, сделки, activity score, yield move, liquidity bucket, spread source,
  snapshot/provisional flags.
- **SegmentSummary**: Агрегированная картина по типу ОФЗ: количество выпусков,
  оборот, активные даты, лидеры, liquidity status и yield context.
- **IssueFocus**: Выпуск, который объясняет конкретный вывод или выделяется в
  сегменте.
- **DataLimitation**: Явное описание missing, provisional или snapshot-only
  данных, влияющих на интерпретацию вывода.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Пользователь может понять 3 главных наблюдения по выбранному
  периоду за 10 секунд без чтения таблиц построчно.
- **SC-002**: 100% summary findings содержат хотя бы одно числовое evidence
  поле и конкретный scope.
- **SC-003**: Structured summary содержит тот же набор выводов, что и UI, без
  потери priority, type и evidence.
- **SC-004**: При отсутствии надежных spread/liquidity данных сводка показывает
  data limitation вместо нулевого или "хорошего" spread.
- **SC-005**: Смена фильтра типа ОФЗ обновляет summary findings без stale
  выводов от предыдущего фильтра.
- **SC-006**: Сводка для диапазона до 90 торговых дней и примерно 100 выпусков
  строится не дольше 10 секунд на локально прогретых данных.
- **SC-007**: В summary text не встречаются запрещенные рекомендательные слова
  из FR-005.

## Assumptions

- V1 использует только уже загружаемые данные MOEX ISS и локальную базу
  приложения; новые внешние источники не добавляются.
- V1 не создает отдельный MCP server. Feature готовит structured summary
  contract, который можно будет использовать в будущей MCP/API фазе.
- Сводка является аналитическим описанием данных, а не инвестиционной
  рекомендацией.
- Existing screen "Активность ОФЗ" остается основной точкой входа.
- Если данных недостаточно для надежного вывода, система должна честно
  показать ограничение, а не пытаться заполнить вывод эвристикой.
