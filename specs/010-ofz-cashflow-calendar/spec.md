# Feature Specification: Календарь денежных потоков ОФЗ

**Feature Branch**: `010-ofz-cashflow-calendar`
**Created**: 2026-05-08
**Status**: Draft
**Input**: User description: "Добавить календарь купонов, оферт, амортизаций и погашений ОФЗ, чтобы объяснять всплески активности событиями выпуска и видеть ближайшие cashflow-события."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Видеть ближайшие события выпуска (Priority: P1)

Пользователь выбирает период активности ОФЗ и видит по активным выпускам ближайшие
купонные даты, амортизации, оферты и погашения, которые попадают в период или
находятся рядом с ним.

**Why this priority**: календарь объясняет часть всплесков торгов реальными
событиями выпуска и дополняет уже реализованные активность, liquidity, breadth и
index context.

**Independent Test**: выбрать период с активными ОФЗ и проверить, что обзор
показывает события по `SECID`, тип события, дату, сумму/процент при наличии и
source status.

**Acceptance Scenarios**:

1. **Given** для выпуска доступны coupon/amortization rows из MOEX ISS,
   **When** пользователь открывает обзор активности, **Then** он видит события
   выпуска в выбранном периоде и ближайшие события рядом с периодом.
2. **Given** поле суммы, процента или record date отсутствует,
   **When** событие показывается в UI и JSON, **Then** отсутствующее поле
   отображается как `n/a`, а не как `0`.
3. **Given** выпуск не имеет будущих событий в источнике,
   **When** строится обзор, **Then** это фиксируется limitation, а не пустым
   "успешным" календарем.

---

### User Story 2 - Связать активность с календарными событиями (Priority: P2)

Пользователь смотрит ranked findings и понимает, какие всплески оборота,
сделок или yield-move пришлись рядом с купоном, офертой, амортизацией или
погашением.

**Why this priority**: это основной аналитический смысл календаря: объяснить
часть локальной активности выпусков проверяемым event evidence.

**Independent Test**: построить activity day и cashflow event в окне `±3`
календарных дня и проверить finding с `SECID`, датой события и расстоянием в
днях без recommendation language.

**Acceptance Scenarios**:

1. **Given** выпуск имеет всплеск активности рядом с coupon date,
   **When** строятся findings, **Then** система добавляет вывод "всплеск рядом
   с купоном" с датой события и числовым evidence.
2. **Given** рядом с активностью есть оферта или погашение,
   **When** пользователь открывает finding, **Then** drill-down показывает
   event type, event date, source и available amounts.
3. **Given** событие далеко от activity day,
   **When** строятся findings, **Then** календарь не превращает его в причину
   всплеска.

---

### User Story 3 - Проверить календарь в детализации выпуска (Priority: P3)

Пользователь открывает выбранный выпуск и видит timeline/table событий: прошлые,
текущие и ближайшие будущие события с датами, суммами, номиналом и источником.

**Why this priority**: детализация выпуска делает выводы проверяемыми и
позволяет смотреть календарь без raw JSON/БД.

**Independent Test**: выбрать выпуск из top contributors и проверить, что detail
panel показывает его события, сортировку по дате и limitation для неполных
полей.

**Acceptance Scenarios**:

1. **Given** пользователь выбирает issue detail,
   **When** для `SECID` есть cashflow events,
   **Then** они отображаются по возрастанию даты с типом и доступными суммами.
2. **Given** несколько событий попадают на одну дату,
   **When** строится timeline,
   **Then** события не схлопываются без явного отображения типа.
3. **Given** событие получено из current/snapshot fallback,
   **When** оно показывается,
   **Then** оно помечается preliminary/snapshot-only.

### Edge Cases

- MOEX `bondization` возвращает пустые `coupons`, `amortizations` или `offers`.
- `data_source = maturity` в `amortizations` означает погашение, а не обычную
  амортизацию.
- У события есть дата, но нет суммы в рублях или процента.
- Дата события выходит за выбранный период, но попадает в окно "рядом с
  периодом".
- Один выпуск имеет несколько событий на одну дату.
- В источнике есть только current fields `NEXTCOUPON`, `OFFERDATE`, `MATDATE`,
  без полного календаря.
- Текущий день или snapshot-derived событие предварительное.
- Нельзя делать вывод, что событие является единственной причиной движения.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST load OFZ cashflow events for active issues from MOEX ISS.
- **FR-002**: System MUST support event types `Coupon`, `Amortization`,
  `Maturity`, `Offer`, `Buyback`, `CallOption`, `PutOption` when data exists.
- **FR-003**: System MUST keep missing amount, percentage, record date and face
  value as null/`n/a`, not zero.
- **FR-004**: System MUST show events inside selected period and a configurable
  near-period window for active issues.
- **FR-005**: System MUST create activity-near-event findings only when an
  issue-level activity fact is within the configured event window.
- **FR-006**: System MUST include event evidence in structured summary JSON.
- **FR-007**: System MUST preserve existing activity, liquidity, breadth,
  special metrics, index context and JSON workflows.
- **FR-008**: System MUST explicitly mark missing calendar, snapshot-only and
  provisional event data.
- **FR-009**: System MUST avoid investment recommendation language in labels,
  findings and limitations.
- **FR-010**: System MUST allow issue-level drill-down of events without opening
  raw ISS payloads or database files.

### Key Entities *(include if feature involves data)*

- **OfzCashflowEvent**: событие выпуска: `SECID`, тип, дата, сумма, процент,
  номинал, валюта, source status и limitations.
- **OfzIssueCashflowCalendar**: календарь одного выпуска с событиями и ближайшим
  событием относительно выбранного периода.
- **OfzCashflowContext**: aggregate для `OfzMarketSummary`: события периода,
  ближайшие события, findings и limitations.
- **OfzCashflowFinding**: summary finding, связывающий issue activity day с
  событием в заданном окне.
- **OfzCashflowLimitation**: причина неполных данных календаря.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a selected period up to one trading year and active issues,
  cashflow context appears within 3 seconds after local activity data is ready
  and cached events are available.
- **SC-002**: 100% of shown events include `SECID`, event type, event date and
  source status.
- **SC-003**: Missing cashflow fields are rendered as `n/a` in UI/JSON and are
  never serialized as fake zero.
- **SC-004**: Event-near-activity findings include distance in days and event
  evidence.
- **SC-005**: Existing `dotnet build CurveAnalyzer.sln -c Release` and relevant
  tests pass.
- **SC-006**: No cashflow finding contains recommendation wording such as
  "купить", "продать", "держать", "buy", "sell" or "recommend".

## Assumptions

- Primary full event calendar source is MOEX ISS
  `statistics/engines/stock/markets/bonds/bondization/{SECID}.json`.
- Current `securities/{SECID}` and `description` endpoints are fallback/context
  sources, not replacements for the full event schedule.
- V1 uses daily/event-date granularity, not intraday event timing.
- Calendar explains possible context and does not claim causal certainty.
