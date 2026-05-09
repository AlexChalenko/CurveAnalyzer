# Feature Specification: Внешние факторы активности ОФЗ

**Feature Branch**: `012-ofz-external-factors`
**Created**: 2026-05-09
**Status**: Draft
**Input**: User description: "Добавить контекст внешних факторов для экрана Активность ОФЗ: ключевая ставка, инфляционные ориентиры, валютные и индексные рыночные ряды, связь с активностью и доходностями без инвестиционных рекомендаций."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Посмотреть внешний фон периода (Priority: P1)

Пользователь открывает экран "Активность ОФЗ" и видит, какой внешний фон был у
выбранного периода: ключевая ставка ЦБ, доступные индексные ряды MOEX,
специализированные метрики ОФЗ-ПК/ОФЗ-ИН и явные ограничения по отсутствующим
факторам.

**Why this priority**: без нейтрального фона пользователь видит всплески и
движения доходности изолированно. Первый ценный срез - объяснить, какие внешние
ряды вообще доступны и насколько они покрывают выбранные даты.

**Independent Test**: построить summary с CBR key rates, index context и
special metrics; проверить, что `externalFactorsContext` содержит factor rows,
coverage, latest observations и limitations без рекомендаций.

**Acceptance Scenarios**:

1. **Given** выбран период с загруженными `CbrKeyRates`, **When** строится
   summary, **Then** контекст показывает последнюю ставку на дату или до даты,
   источник `CbrKeyRate` и coverage по периоду.
2. **Given** в summary уже есть MOEX index context, **When** строится внешний
   фон, **Then** пользователь видит RGBI/RGBITR и duration-сегменты как
   рыночные факторы, не дублируя отдельную индексную вкладку.
3. **Given** для части факторов нет рядов, **When** строится контекст, **Then**
   UI и JSON показывают limitation, а не `0` или скрытую строку.

---

### User Story 2 - Сопоставить активность с факторными движениями (Priority: P2)

Пользователь видит, совпали ли дни активности или движения доходности с
заметным изменением внешнего фактора, при этом текст остается нейтральным:
"совпало с", "на фоне", "без заметного движения фактора", но не "из-за" и не
"покупать/продавать".

**Why this priority**: основной аналитический смысл feature - добавить контекст
к уже найденным activity/yield findings без перехода к причинности или
инвестиционным сигналам.

**Independent Test**: построить synthetic period, где insight day имеет высокий
оборот и изменение RGBI или ключевой ставки; проверить finding с датой,
factor code, factor change, activity evidence и neutral wording.

**Acceptance Scenarios**:

1. **Given** день имеет activity finding и в тот же день заметное движение
   индекса, **When** строится summary, **Then** создается finding внешнего
   фактора с обеими evidence-группами.
2. **Given** день активен, но внешний фактор плоский, **When** строится summary,
   **Then** finding говорит о локальной активности относительно спокойного
   фактора.
3. **Given** факторная точка доступна только как snapshot/provisional, **When**
   она попадает в finding, **Then** finding наследует provisional limitation.

---

### User Story 3 - Сохранить факторный контекст в JSON и UI (Priority: P3)

Пользователь сохраняет structured JSON или копирует summary и получает
машиночитаемый слой `externalFactorsContext` вместе с source counts,
limitations и связями с activity findings.

**Why this priority**: внешний фон должен быть проверяемым и пригодным для
передачи внешнему анализатору, а не только текстовой подсказкой в интерфейсе.

**Independent Test**: сериализовать summary и проверить schema version,
`externalFactorsContext`, factor observations, linked findings и отсутствие
missing-as-zero.

**Acceptance Scenarios**:

1. **Given** summary содержит факторный контекст, **When** пользователь нажимает
   "Файл" или "Копия", **Then** JSON содержит `externalFactorsContext`.
2. **Given** фактор недоступен, **When** строится JSON, **Then** limitation
   указывает missing source, а factor row остается с `availability = Missing`.
3. **Given** пользователь переключает тип ОФЗ, **When** строится summary,
   **Then** market-wide факторы остаются market-wide, а issue/segment связи
   относятся только к выбранному типу.

---

### Edge Cases

- Ключевая ставка меняется не каждый торговый день: нужен latest-on-or-before
  lookup без заглядывания в будущее.
- MOEX index point есть только snapshot для текущей даты и не содержит yield или
  duration.
- В выбранном типе ОФЗ нет специальных метрик, но market-wide факторы доступны.
- Внешний ряд короче выбранного периода или начинается после `startDate`.
- Факторная дата выпадает на календарный день без торгов ОФЗ.
- `SignalScope = LastDay` должен фильтровать findings, но не обрезать baseline
  и factor coverage.
- Отсутствующие валютные/товарные ряды не должны блокировать ключевую ставку и
  index context.
- Любой текст feature не должен звучать как прогноз, рейтинг
  привлекательности или инвестиционная рекомендация.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST build an `externalFactorsContext` from factor sources
  already available in the supported OFZ workflow.
- **FR-002**: System MUST include CBR key-rate observations using the latest
  value on or before each trade date; future key-rate observations are not
  allowed for a finding.
- **FR-003**: System MUST include existing MOEX index context points as market
  factors with source kind, provisional flag, direction and meaningful-move
  marker.
- **FR-004**: System MUST include available special metrics such as implied CBR
  rate and implied inflation as OFZ-derived factors, with their existing
  availability and source labels.
- **FR-005**: System MUST represent missing external factor series as explicit
  limitations and MUST NOT replace missing values with zero.
- **FR-006**: System MUST produce external-factor findings only when there is
  both selected-period activity/yield evidence and factor evidence on a
  comparable date.
- **FR-007**: System MUST use neutral wording for factor findings and MUST NOT
  claim causality, forecast price moves, or provide investment recommendations.
- **FR-008**: System MUST propagate snapshot/provisional limitations from
  source factors into factor context and related findings.
- **FR-009**: System MUST expose external factors in the WPF overview as an
  additional tab without replacing existing index, cashflow or seasonality tabs.
- **FR-010**: System MUST include factor context, observations, links,
  limitations and source counts in structured summary JSON.
- **FR-011**: System MUST preserve coupon type filtering for activity/yield
  evidence while keeping market-wide factors clearly marked as market-wide.
- **FR-012**: System MUST keep v1 independent from new persistence tables unless
  implementation discovers that an already loaded factor cannot be represented
  without a narrow schema extension.

### Key Entities *(include if feature involves data)*

- **ExternalFactorContext**: full factor layer for a summary period, including
  factor series, latest observations, links to activity/yield days and
  limitations.
- **ExternalFactorSeries**: one external or derived factor series such as
  `cbr_key_rate`, `RGBI`, `RGBITR`, `implied_inflation`.
- **ExternalFactorObservation**: normalized point with date, value, optional
  daily change, source, availability and provisional marker.
- **ExternalFactorActivityLink**: relation between a factor observation and a
  market/activity/yield event for the same trade date or as-of date.
- **ExternalFactorFinding**: ranked neutral insight generated from a link and
  serialized through existing summary finding infrastructure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a period with cached key-rate and index data, JSON contains
  `externalFactorsContext.factorSeries` with at least CBR key rate and RGBI.
- **SC-002**: For a synthetic active day with a matching factor move, tests
  verify one external-factor finding with no future factor observation.
- **SC-003**: For a period with missing optional factors, summary still builds
  and shows limitations instead of zero-valued factors.
- **SC-004**: Existing full validation succeeds: `dotnet build
  CurveAnalyzer.sln -c Release` and `dotnet test -c Release`.
- **SC-005**: Manual smoke export from the OFZ screen produces schema version
  `1.6` JSON with no recommendation language in factor findings.

## Assumptions

- V1 uses existing loaded data first: `CbrKeyRates`, `IndexContextDays`,
  `IndexSegments` and `SpecialMetrics`.
- New CBR inflation, FX and commodity loaders are not required for the first
  implementation slice; if they are unavailable, the context records missing
  factor limitations.
- External factors are context, not a scoring or recommendation model.
- Existing `OfzMarketSummaryBuilder` remains the composition point for ranked
  findings and structured JSON.
