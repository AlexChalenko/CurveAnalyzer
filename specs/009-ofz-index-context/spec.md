# Feature Specification: OFZ Index Context

**Feature Branch**: `009-ofz-index-context`
**Created**: 2026-05-08
**Status**: Draft
**Input**: User description: "Добавить фоновый индексный контекст MOEX для экрана активности ОФЗ: RGBI/RGBITR и duration-сегменты, чтобы понимать, происходят ли всплески активности на фоне общего движения рынка или локально по выпуску."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Видеть общий индексный фон ОФЗ (Priority: P1)

Пользователь открывает экран активности ОФЗ за выбранный период и видит рядом с
уже рассчитанными всплесками общий фон рынка государственных облигаций: движение
ценового индекса, индекса полной доходности, рыночной доходности и duration.

**Why this priority**: без индексного фона пользователь видит активность
отдельных выпусков, но не понимает, совпадает ли она с движением всего рынка
ОФЗ.

**Independent Test**: можно выбрать период с несколькими торговыми днями и
проверить, что обзор показывает индексные значения по тем же датам, что и
активность ОФЗ, а дни без индексных данных явно помечены как ограничения.

**Acceptance Scenarios**:

1. **Given** выбран период с доступной историей ОФЗ и индексными данными,
   **When** пользователь открывает обзор активности, **Then** он видит
   индексный контекст по торговым датам периода.
2. **Given** за часть дат нет индексного значения, **When** строится обзор,
   **Then** эти даты не заполняются нулем и показываются как неполные данные.
3. **Given** выбран текущий или незавершенный торговый день, **When** индексный
   контекст включает этот день, **Then** его предварительный статус виден рядом
   с другими provisional-данными.

---

### User Story 2 - Сравнить всплески активности с движением индекса (Priority: P2)

Пользователь смотрит выводы по активности и понимает, был ли сильный оборот или
широкое движение доходностей частью общего движения рынка ОФЗ или локальным
событием по отдельным выпускам.

**Why this priority**: это главный аналитический смысл индексного слоя:
отделить рыночный фон от специфики выбранного выпуска, сегмента или дня.

**Independent Test**: можно выбрать день с высоким activity score и проверить,
что выводы показывают направление и величину изменения индекса за этот день,
не называя это торговой рекомендацией.

**Acceptance Scenarios**:

1. **Given** в периоде есть день с сильным всплеском активности, **When**
   пользователь смотрит выводы, **Then** система показывает, сопровождался ли
   этот день заметным движением индексного фона.
2. **Given** выпуск имеет всплеск активности, но индексный фон спокоен,
   **When** пользователь открывает детализацию, **Then** локальный характер
   события виден через числовое evidence.
3. **Given** индексный фон движется сильно, но выбранный выпуск не выделяется,
   **When** пользователь смотрит детали выпуска, **Then** система не
   превращает общий рыночный фон в сигнал по этому выпуску.

---

### User Story 3 - Видеть duration-сегменты рынка (Priority: P3)

Пользователь оценивает, какая часть рынка ОФЗ двигалась сильнее: короткие,
средние или длинные выпуски. Это помогает интерпретировать scatter
`дюрация / доходность` и breadth-выводы.

**Why this priority**: duration-сегменты уточняют общий индексный фон и делают
его полезным для сравнения выпусков с разной чувствительностью к ставкам.

**Independent Test**: можно выбрать период и проверить, что сегментные индексные
ряды отображаются только для доступных сегментов, а отсутствующие сегменты
показываются как ограничение данных.

**Acceptance Scenarios**:

1. **Given** для периода доступны индексы duration-сегментов, **When**
   пользователь открывает индексный контекст, **Then** он видит движение
   короткого, среднего и длинного сегментов отдельно от общего индекса.
2. **Given** для части сегментов нет данных, **When** строится экран, **Then**
   доступные сегменты остаются видимыми, а отсутствующие не заменяются нулями.
3. **Given** выбран фильтр конкретного типа ОФЗ, **When** пользователь смотрит
   индексный фон, **Then** общий индексный контекст остается фоном рынка, а не
   пересчитывается как фильтрованный набор выпусков.

---

### User Story 4 - Проверить день через индексный drill-down (Priority: P4)

Пользователь выбирает интересный день и видит состав индексного контекста:
значения индексов, дневные изменения, доходность, duration, доступность данных и
связанные activity/breadth факты.

**Why this priority**: drill-down делает выводы проверяемыми и снижает риск
неверной интерпретации агрегированных фраз.

**Independent Test**: можно выбрать день с индексным движением и проверить, что
детализация дня показывает числовые значения и причины отсутствия данных без
обращения к raw database.

**Acceptance Scenarios**:

1. **Given** пользователь выбрал день из overview или heatmap, **When**
   открывается детализация дня, **Then** видны индексные значения и дневные
   изменения для этой даты.
2. **Given** индексная дата есть, но часть полей отсутствует, **When**
   пользователь смотрит drill-down, **Then** отсутствующие поля помечены как
   `n/a`, а не как нулевые значения.

### Edge Cases

- В выбранном периоде нет индексных данных.
- Индексные данные есть не на все даты активности ОФЗ.
- Текущий торговый день еще не завершен и индексные значения предварительные.
- Исторический ряд индекса начинается позже выбранного периода.
- Duration-сегмент недоступен, переименован или временно не отдается источником.
- Дневное изменение индекса нельзя посчитать из-за отсутствия предыдущего
  торгового значения.
- Выбран фильтр типа ОФЗ, но индексный фон остается общерыночным контекстом.
- Значение индекса, доходности или duration отсутствует для отдельной строки.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST show OFZ market index context for the selected period.
- **FR-002**: System MUST include both price-index and total-return-index
  context when both series are available.
- **FR-003**: System MUST show index close/value, daily change, yield and
  duration when these values are available.
- **FR-004**: System MUST align index values with OFZ activity dates by trade
  date and MUST NOT forward-fill missing dates without an explicit missing-data
  marker.
- **FR-005**: System MUST show whether an activity or breadth finding happened
  with, against, or without a meaningful index move.
- **FR-006**: Users MUST be able to inspect the index context for a selected
  day without opening raw data files.
- **FR-007**: System MUST support duration-segment index context for available
  short, medium and long government-bond segments.
- **FR-008**: System MUST keep unavailable duration segments visible as data
  limitations rather than silently hiding the whole index context.
- **FR-009**: System MUST preserve existing activity, liquidity, breadth,
  special-metric and issue-detail workflows.
- **FR-010**: System MUST respect the selected date range and insight-period
  settings when computing index-backed findings.
- **FR-011**: System MUST explicitly mark current-day, provisional,
  insufficient-history and missing-index states.
- **FR-012**: System MUST NOT treat missing index values, missing daily changes,
  missing yield or missing duration as zero.
- **FR-013**: System MUST avoid investment recommendation language in all
  index-context labels and findings.
- **FR-014**: System MUST include index-context facts in the structured
  analytics output so they can be reused outside the visual UI.

### Key Entities *(include if feature involves data)*

- **OfzMarketIndexSeries**: индексный ряд рынка ОФЗ: код, название, тип ряда
  (ценовой, полной доходности или duration-сегмент), валюта и доступность.
- **OfzMarketIndexPoint**: значение индекса за торговую дату: close/value, дневное
  изменение, доходность, duration, оборот или расчетная база, статус качества.
- **OfzIndexContextDay**: сводка индексного фона за дату, сопоставленная с
  activity/breadth facts выбранного периода.
- **OfzIndexSegmentContext**: набор доступных duration-сегментов и их движение
  за выбранный период или день.
- **OfzIndexFinding**: текстовый вывод с числовым evidence о связи активности
  ОФЗ с индексным фоном.
- **OfzIndexDataLimitation**: причина неполного индексного контекста: нет ряда,
  нет даты, нет предыдущего значения, предварительные данные или отсутствующее
  поле.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a selected period up to one trading year, users can see index
  context within 3 seconds after local OFZ data is loaded.
- **SC-002**: For every shown index point, users can identify the trade date,
  index code and whether the value is historical, provisional or missing.
- **SC-003**: For every activity day with available index context, users can see
  the same-day index direction and magnitude.
- **SC-004**: Missing index values, missing daily changes and unavailable
  duration segments are displayed as data limitations in 100% of affected
  views.
- **SC-005**: Changing the selected period updates index charts, findings and
  drill-down data so that all shown values belong to the active period.
- **SC-006**: No index-context finding contains recommendation wording such as
  "купить", "продать", "держать", "buy", "sell" or "recommend".
- **SC-007**: Users can select a highlighted day and verify the numeric index
  evidence without reading raw database files.

## Assumptions

- Primary whole-market index context uses MOEX government bond indices `RGBI`
  and `RGBITR`.
- Duration-segment context uses available MOEX government bond segment indices
  such as short, medium and long maturity buckets; exact series names are
  finalized during planning.
- Index context is analytical background, not a recalculation of the selected
  OFZ type filter.
- V1 does not build a predictive model and does not produce investment
  recommendations.
- Existing date, type, signal-scope and insight-period controls remain the
  primary navigation model for the screen.
