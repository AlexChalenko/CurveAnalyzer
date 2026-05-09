# Feature Specification: Сезонность активности ОФЗ

**Feature Branch**: `011-ofz-seasonality`
**Created**: 2026-05-09
**Status**: Draft
**Input**: User description: "Начать следующий этап roadmap: сезонность активности ОФЗ; после создания tasks выполнить analyze."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Посмотреть сезонный профиль активности (Priority: P1)

Пользователь открывает экран "Активность ОФЗ" и видит, по каким дням недели и
месяцам в выбранном наборе ОФЗ обычно выше или ниже оборот, число сделок и
количество активных выпусков.

**Why this priority**: без базового профиля сезонности остальные выводы будут
необъяснимыми. Пользователю сначала нужен нейтральный контекст, а не сигнал.

**Independent Test**: построить набор activity metrics с несколькими торговыми
днями разных weekdays/months и проверить, что профиль содержит bucket rows с
наблюдениями, медианным оборотом, сделками и limitation при малой истории.

**Acceptance Scenarios**:

1. **Given** выбран период с достаточной историей, **When** строится summary,
   **Then** система показывает weekday profile с `ObservationCount`,
   `MedianTotalValue`, `MedianNumTrades` и `ActiveIssueCount`.
2. **Given** в периоде есть данные за несколько месяцев, **When** строится
   summary, **Then** система показывает month profile с теми же агрегатами.
3. **Given** для bucket мало наблюдений, **When** строится профиль, **Then**
   bucket остается видимым, но помечается limitation вместо скрытого нуля.

---

### User Story 2 - Сравнить текущий период с сезонной базой (Priority: P2)

Пользователь видит, является ли активность последних дней выше или ниже
исторического поведения для соответствующего дня недели или месяца.

**Why this priority**: это основной аналитический смысл feature: отделить
локальный всплеск от типичного календарного поведения.

**Independent Test**: построить baseline по предыдущим наблюдениям и insight
day с оборотом выше baseline; проверить finding с датой, bucket, ratio и
ограничением при недостаточной базе.

**Acceptance Scenarios**:

1. **Given** понедельники в baseline имеют стабильный медианный оборот, **When**
   понедельник из insight window значительно выше, **Then** создается finding
   "Активность выше сезонной базы" с ratio и числом наблюдений baseline.
2. **Given** baseline для bucket недостаточен, **When** день попадает в этот
   bucket, **Then** finding не создается, а limitation объясняет недостаточную
   сезонную базу.
3. **Given** активность ниже сезонной базы, **When** отклонение достаточно
   заметно, **Then** finding описывает снижение без инвестиционных рекомендаций.

---

### User Story 3 - Сохранить сезонность в structured JSON и UI (Priority: P3)

Пользователь сохраняет structured JSON и получает сезонный контекст вместе с
остальными слоями аналитики; в UI этот контекст доступен отдельной вкладкой.

**Why this priority**: сезонность должна быть пригодна для проверки и передачи
внешнему анализатору, а не только визуальной подсказкой в приложении.

**Independent Test**: построить summary, сериализовать JSON и проверить
`seasonalityContext`, source counts, limitations и отсутствие recommendation
language.

**Acceptance Scenarios**:

1. **Given** summary содержит seasonal buckets, **When** пользователь нажимает
   "Файл" или "Копия", **Then** JSON содержит `seasonalityContext` и
   `seasonalityContext.findings`.
2. **Given** сезонный контекст недоступен из-за короткой истории, **When**
   строится JSON, **Then** в `limitations` есть причина, а поля не заменяются
   фиктивными значениями.
3. **Given** пользователь переключает тип ОФЗ, **When** строится сезонность,
   **Then** профили и findings относятся только к активным выпускам выбранного
   типа.

---

### Edge Cases

- Выбранный период короче минимальной сезонной базы.
- В выбранном типе ОФЗ мало выпусков или мало активных дней.
- В insight window есть current-day snapshot/provisional данные.
- День недели представлен одним наблюдением и не должен давать сильный вывод.
- Месячный профиль строится на одном месяце и должен быть помечен как слабый.
- `SignalScope = LastDay` не должен ломать baseline: baseline остается
  историческим контекстом, а findings фильтруются по scope.
- Нулевые обороты и отсутствующие metrics не должны превращаться в сильные
  сезонные выводы.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST build a seasonality context from existing
  `OfzActivityMetric` data for active issues in the selected coupon type
  filter.
- **FR-002**: System MUST provide weekday profile buckets with observation
  count, active day count, median/average turnover, median trades and median
  active issue count.
- **FR-003**: System MUST provide month profile buckets with the same aggregate
  fields as weekday buckets.
- **FR-004**: System MUST keep insufficient seasonal baseline as explicit
  limitations and MUST NOT replace missing seasonal values with zero.
- **FR-005**: System MUST compare insight-window activity against prior
  same-bucket baseline without using future observations for that finding.
- **FR-006**: System MUST create seasonality findings only when the baseline has
  enough observations and deviation crosses configured thresholds.
- **FR-007**: System MUST include seasonality context, findings and limitations
  in structured summary JSON.
- **FR-008**: System MUST expose seasonality profile in the WPF overview without
  creating a separate dashboard.
- **FR-009**: System MUST respect coupon type filter and signal scope for
  output findings.
- **FR-010**: System MUST avoid investment recommendation wording in seasonality
  findings.

### Key Entities

- **OfzSeasonalityContext**: сезонный слой внутри `OfzMarketSummary`, включая
  profiles, findings, thresholds и limitations.
- **OfzSeasonalityBucket**: агрегат по bucket `Weekday` или `Month` с
  observation counts и activity aggregates.
- **OfzSeasonalityObservation**: дневной агрегат активности по выбранному набору
  выпусков, используемый для расчета buckets.
- **OfzSeasonalityFinding**: вывод о заметном отклонении активности от
  сезонной базы.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Для периода с минимум 20 торговыми днями summary содержит weekday
  profile минимум по 4 bucket values, если данные присутствуют.
- **SC-002**: Для insufficient history UI/JSON показывают limitation, а не
  пустой успешный сезонный профиль.
- **SC-003**: Seasonality context строится за 3 секунды после готовности
  existing activity metrics для периода до одного торгового года.
- **SC-004**: Structured JSON содержит schema version bump и
  `seasonalityContext`; existing `breadth`, `indexContext`, `cashflowContext`
  остаются совместимыми.
- **SC-005**: Автотесты покрывают bucket aggregation, no-future baseline,
  insufficient baseline и no recommendation language.

## Assumptions

- Feature использует уже загруженную историю ОФЗ; новых MOEX endpoints и новых
  таблиц SQLite не требуется.
- V1 ограничивается днями недели и календарными месяцами. Налоговые периоды,
  конец года и внешние факторы остаются за пределами этой feature.
- "Сезонность" является контекстом для сравнения активности, а не прогнозом.
- Пользователь сам выбирает достаточно длинный период, если хочет устойчивый
  профиль.
