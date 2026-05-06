# Feature Specification: OFZ Market Breadth

**Feature Branch**: `006-ofz-market-breadth`  
**Created**: 2026-05-06  
**Status**: Draft  
**Input**: User description: "Добавить market breadth аналитику для ОФЗ: ширина движения доходностей, доля активных выпусков, концентрация оборота top-5/top-10 и распределение оборота по типам ОФЗ"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Увидеть ширину движения рынка (Priority: P1)

Пользователь открывает аналитику ОФЗ за выбранный период и видит, насколько движение доходностей было широким: сколько выпусков двигалось вверх, сколько вниз, сколько осталось без значимого изменения, и были ли дни, когда движение затронуло большую часть рынка.

**Why this priority**: Это главный контекст для уже реализованных всплесков активности. Без breadth пользователь видит отдельные бумаги, но не понимает, был ли сигнал локальным или частью общего движения рынка ОФЗ.

**Independent Test**: Можно выбрать период с историей ОФЗ и проверить, что для каждого торгового дня отображается баланс выпусков с ростом/снижением доходности и выделяются дни с широким движением.

**Acceptance Scenarios**:

1. **Given** выбран период с несколькими торговыми днями и доступными доходностями, **When** пользователь открывает обзор market breadth, **Then** он видит по дням количество выпусков с ростом доходности, снижением доходности и без значимого изменения.
2. **Given** в периоде есть день, когда большинство выпусков двигалось в одном направлении, **When** пользователь смотрит выводы, **Then** система выделяет этот день как широкое движение рынка и показывает числовое evidence.
3. **Given** у части выпусков нет достаточной истории для сравнения доходности, **When** строится breadth, **Then** такие выпуски не считаются как нули и попадают в ограничения данных.

---

### User Story 2 - Оценить концентрацию оборота (Priority: P2)

Пользователь видит, насколько дневной оборот был распределен по рынку или сконцентрирован в нескольких выпусках: доля top-5 и top-10 выпусков в общем обороте, а также дни с экстремальной концентрацией.

**Why this priority**: Всплеск оборота может выглядеть как рыночная активность, но фактически быть сконцентрированным в нескольких выпусках. Это меняет интерпретацию сигнала.

**Independent Test**: Можно выбрать день или период и проверить, что сумма оборота top-5/top-10 сравнивается с общим оборотом дня, а выводы показывают дни с высокой концентрацией.

**Acceptance Scenarios**:

1. **Given** за день есть оборот по нескольким выпускам, **When** пользователь смотрит концентрацию, **Then** он видит долю top-5 и top-10 выпусков в общем обороте дня.
2. **Given** один или несколько выпусков дают большую часть оборота дня, **When** строятся выводы, **Then** система показывает концентрацию оборота как отдельный факт, не называя его торговой рекомендацией.
3. **Given** в выбранном фильтре меньше 10 выпусков с оборотом, **When** считается top-10, **Then** система корректно использует доступное количество выпусков и явно показывает базу расчета.

---

### User Story 3 - Сравнить вклад типов ОФЗ (Priority: P3)

Пользователь видит, как оборот и активность распределялись между типами ОФЗ: ОФЗ-ПД, ОФЗ-ПК, ОФЗ-ИН, ОФЗ-АД и валютными выпусками.

**Why this priority**: Разные типы ОФЗ имеют разную экономическую природу. Для market breadth важно понимать, какой сегмент формировал движение и оборот.

**Independent Test**: Можно выбрать период и проверить, что агрегаты по типам суммируются в общий рынок, а фильтр типа ограничивает расчеты выбранным сегментом.

**Acceptance Scenarios**:

1. **Given** выбран режим `Все`, **When** пользователь смотрит распределение оборота по типам, **Then** он видит долю каждого типа ОФЗ за день или период.
2. **Given** выбран конкретный тип ОФЗ, **When** пользователь смотрит breadth, **Then** все метрики и выводы считаются только внутри выбранного типа.
3. **Given** у выпуска тип не определен, **When** строятся сегментные агрегаты, **Then** выпуск не теряется молча и попадает в отдельное ограничение или группу неизвестного типа.

---

### User Story 4 - Разобрать день market breadth (Priority: P4)

Пользователь выбирает интересный день и видит состав сигнала: какие выпуски внесли основной вклад в оборот, какие группы двигали доходность, сколько данных было исключено из расчета.

**Why this priority**: Сводный индекс полезен только если его можно проверить. Drill-down снижает риск неверной интерпретации агрегатов.

**Independent Test**: Можно выбрать день с breadth-сигналом и проверить, что отображаются top contributors, direction counts, concentration и ограничения данных для этого дня.

**Acceptance Scenarios**:

1. **Given** пользователь выбрал день с широким движением, **When** он открывает детализацию дня, **Then** он видит основные выпуски по обороту и распределение направлений доходности.
2. **Given** день содержит provisional или snapshot-only данные, **When** пользователь смотрит детализацию, **Then** система явно помечает предварительный характер данных.

### Edge Cases

- В выбранном периоде нет торговых дней или нет выпусков с валидными доходностями.
- У выпуска нет предыдущей доступной торговой записи для сравнения доходности.
- В выбранном фильтре слишком мало выпусков для meaningful breadth.
- Top-5 или top-10 больше количества выпусков с оборотом.
- Текущий торговый день еще не завершен и должен быть помечен как предварительный.
- У части выпусков отсутствует тип ОФЗ, доходность, оборот или количество сделок.
- Оборот есть, но все direction metrics отсутствуют из-за нехватки доходностей.
- Фильтр типа или периода меняется после построения выводов; старые breadth-выводы не должны оставаться на экране.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST calculate market breadth for the selected period and current OFZ type filter.
- **FR-002**: System MUST classify each comparable issue-day by yield direction: yield up, yield down, unchanged, or not comparable.
- **FR-003**: System MUST calculate daily counts and shares for yield-up, yield-down, unchanged and not-comparable issues.
- **FR-004**: System MUST calculate the share of active issues for each trading day using the existing activity signal concept.
- **FR-005**: System MUST calculate turnover concentration for each trading day, including top-5 share and top-10 share when enough issues are available.
- **FR-006**: System MUST calculate turnover distribution by OFZ type for the selected period and for individual days.
- **FR-007**: Users MUST be able to identify days with broad yield movement, unusually high active-issue share, or unusually high turnover concentration.
- **FR-008**: Users MUST be able to select a breadth day and see the main contributors and data limitations for that day.
- **FR-009**: System MUST respect existing period, type and signal-scope filters when computing breadth metrics and findings.
- **FR-010**: System MUST not treat missing yield, turnover, issue type, or baseline data as zero.
- **FR-011**: System MUST explicitly mark current-day, provisional, snapshot-only and insufficient-baseline data where those states affect breadth interpretation.
- **FR-012**: System MUST avoid investment recommendation language in all breadth findings and labels.
- **FR-013**: System MUST include market breadth facts in the structured analytics output so they can be reused outside the visual UI.
- **FR-014**: System MUST preserve existing activity, liquidity, heatmap and detail workflows while adding breadth analytics.

### Key Entities *(include if feature involves data)*

- **MarketBreadthDay**: Сводка одного торгового дня: дата, количество сравнимых выпусков, direction counts, active issue share, turnover, top-5/top-10 concentration, flags качества данных.
- **YieldDirectionBreakdown**: Распределение выпусков по направлению изменения доходности: рост, снижение, без значимого изменения, не сравнимо.
- **TurnoverConcentration**: Доли оборота top-5 и top-10 выпусков, количество выпусков в базе расчета и список основных contributors.
- **TypeTurnoverShare**: Доля оборота и активности по типу ОФЗ за день или выбранный период.
- **MarketBreadthFinding**: Текстовый вывод с числовым evidence для широкого движения, концентрации оборота, активности или ограничения данных.
- **MarketBreadthLimitation**: Причина, почему часть данных не участвовала в расчете или требует осторожной интерпретации.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a selected period up to one trading year, users can see market breadth overview within 3 seconds after data is loaded locally.
- **SC-002**: For every trading day with comparable data, the UI shows yield-up, yield-down and unchanged counts whose sum equals the comparable issue count.
- **SC-003**: For every trading day with turnover data, top-5 and top-10 concentration are displayed with an explicit calculation base.
- **SC-004**: Changing the OFZ type filter updates breadth metrics and findings so that 100% of shown values match the active filter.
- **SC-005**: Days based on current-day or incomplete data are visibly marked as provisional in all related breadth findings.
- **SC-006**: No breadth finding contains recommendation wording such as "купить", "продать", "держать", "buy", "sell" or "recommend".
- **SC-007**: Users can select a highlighted breadth day and identify the top contributors and excluded-data reasons without opening raw database files.

## Assumptions

- Market breadth v1 uses already loaded MOEX OFZ activity, daily trade, yield and issue metadata data; no new external source is required for the spec.
- Yield direction is evaluated only against the previous available trading record for the same issue; future records must not affect classification.
- Current-day data can be shown for context, but it is preliminary until the trading day is complete.
- The feature is analytical and explanatory; it does not rank issues as investment ideas.
- Existing OFZ type classification is reused: ОФЗ-ПД, ОФЗ-ПК, ОФЗ-ИН, ОФЗ-АД, Валютная and unknown/other where classification is unavailable.
