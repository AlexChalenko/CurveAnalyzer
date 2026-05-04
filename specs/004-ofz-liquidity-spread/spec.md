# Feature Specification: Liquidity & Spread Layer

**Feature Branch**: `004-ofz-liquidity-spread`
**Created**: 2026-05-04
**Status**: Draft
**Input**: User description: "Начать следующий этап после активности ОФЗ:
Liquidity & Spread Layer. Добавить анализ ликвидности, bid-ask spread,
глубины заявок и spread-контекста, чтобы объяснять качество торгов по ОФЗ, а
не только всплески оборота."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Увидеть качество ликвидности выпуска (Priority: P1)

Пользователь выбирает выпуск ОФЗ или видит его в рейтинге активности и может
понять, насколько удобно по нему входить или выходить: есть ли узкий spread,
достаточная глубина и нормальное количество сделок.

**Why this priority**: текущий экран показывает всплески оборота, но большой
оборот сам по себе не говорит, что бумага ликвидна и торгуется без заметных
издержек.

**Independent Test**: можно выбрать выпуск с данными по котировкам и проверить,
что экран показывает spread, глубину, количество сделок и понятную оценку
качества ликвидности без необходимости открывать другие вкладки.

**Acceptance Scenarios**:

1. **Given** выбран выпуск с доступными котировками, **When** пользователь
   открывает детализацию выпуска, **Then** он видит текущий или последний
   доступный spread, глубину спроса/предложения и оценку ликвидности.
2. **Given** выпуск имеет высокий оборот, но широкий spread, **When**
   пользователь смотрит выводы по выпуску, **Then** система явно показывает,
   что оборот не равен хорошей ликвидности.

---

### User Story 2 - Найти выпуски с проблемной ликвидностью (Priority: P2)

Пользователь выбирает диапазон и тип ОФЗ и видит список выпусков, где
ликвидность выглядит слабой: широкий spread, низкая глубина, мало сделок или
нестабильные котировки.

**Why this priority**: это позволяет быстро отфильтровать бумаги, где
аномальная активность может быть менее полезной из-за торговых издержек.

**Independent Test**: можно загрузить диапазон с несколькими выпусками и
проверить, что список проблемной ликвидности ранжирует бумаги по понятным
признакам, а не только по обороту.

**Acceptance Scenarios**:

1. **Given** выбран диапазон и тип ОФЗ, **When** пользователь загружает данные,
   **Then** система показывает top выпусков с худшей ликвидностью за выбранный
   период или последнюю доступную дату.
2. **Given** для части выпусков нет котировок или depth-данных, **When**
   строится список проблемной ликвидности, **Then** такие выпуски получают
   отдельную пометку "данных недостаточно", а не ошибочную оценку.

---

### User Story 3 - Сравнить доходность, дюрацию и spread (Priority: P3)

Пользователь видит карту выпусков, где по одной картинке можно сравнить
дюрацию, доходность, оборот и spread-качество.

**Why this priority**: scatter `дюрация / доходность` уже есть, но без
ликвидности он не показывает, насколько реалистично торговать выбранной
бумагой.

**Independent Test**: можно выбрать тип ОФЗ и убедиться, что каждая точка на
карте отображает доходность/дюрацию, а цвет или размер отражает ликвидность и
spread-контекст.

**Acceptance Scenarios**:

1. **Given** в выбранном диапазоне есть выпуски с доходностью, дюрацией и
   spread-данными, **When** пользователь открывает сегментную карту, **Then**
   он видит различия между ликвидными и менее ликвидными бумагами.
2. **Given** пользователь наводит курсор на точку, **When** отображается
   подсказка, **Then** она содержит выпуск, тип ОФЗ, доходность, дюрацию,
   оборот, сделки и spread-показатель.

---

### User Story 4 - Получить объяснимые выводы по spread-аномалиям (Priority: P4)

Пользователь видит короткие выводы, которые объясняют не только всплеск
оборота, но и необычное ухудшение или улучшение ликвидности.

**Why this priority**: выводы помогают быстро понять, на какие выпуски стоит
посмотреть вручную, без превращения экрана в систему рекомендаций.

**Independent Test**: можно загрузить диапазон с разными spread-состояниями и
проверить, что выводы называют конкретные бумаги, даты и причины сигнала.

**Acceptance Scenarios**:

1. **Given** выпуск имеет одновременно всплеск оборота и расширение spread,
   **When** формируются выводы, **Then** система показывает это как отдельный
   сигнал риска ликвидности.
2. **Given** spread улучшился при росте активности, **When** формируются
   выводы, **Then** система показывает это как улучшение торгового качества,
   но не как инвестиционную рекомендацию.

### Edge Cases

- В выбранном диапазоне есть торговая активность, но нет bid/offer данных.
- Spread равен нулю или технически отсутствует из-за пустых котировок.
- Для части выпусков доступны только текущие котировки, но нет исторической
  серии spread.
- Валютные ОФЗ не должны смешиваться с рублевыми без явной пометки.
- ОФЗ-ПК и ОФЗ-ИН не должны сравниваться с ОФЗ-ПД по обычной доходности без
  учета типа.
- Текущий торговый день может быть неполным и должен быть помечен как
  предварительный.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Пользователь MUST be able to see liquidity metrics for a selected
  OFZ issue, including spread, bid/offer context, deal count and turnover when
  these data are available.
- **FR-002**: System MUST clearly distinguish missing liquidity data from valid
  zero or narrow-spread values.
- **FR-003**: System MUST show a ranked list of issues with weak liquidity for
  the selected range, type filter and signal scope.
- **FR-004**: System MUST keep the existing OFZ type filters applicable to
  liquidity and spread analytics.
- **FR-005**: System MUST provide a duration/yield comparison view where
  liquidity or spread quality is visible without opening each issue manually.
- **FR-006**: System MUST provide deterministic liquidity/spread insights that
  name the issue, date and reason for the signal.
- **FR-007**: System MUST avoid treating preliminary current-day data as final
  history.
- **FR-008**: System MUST not present liquidity or spread signals as investment
  recommendations.
- **FR-009**: User MUST be able to inspect issue-level liquidity history for
  dates where historical spread-like data are available.
- **FR-010**: System MUST degrade gracefully when only current snapshot
  liquidity is available, while still allowing historical turnover/yield
  analysis to work.

### Key Entities

- **Liquidity Snapshot**: A per-issue observation of bid/offer, spread and
  visible quote depth for a trading moment or latest available date.
- **Liquidity Metric**: A normalized assessment of liquidity quality for an
  issue/date based on spread, depth, turnover and deal count.
- **Spread Signal**: A detected unusual state such as wide spread with high
  activity, improving spread with growing turnover, or missing quote data during
  otherwise active trading.
- **Issue Liquidity Profile**: A combined view of issue identity, OFZ type,
  turnover, deals, yield, duration and liquidity metrics.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Пользователь может определить качество ликвидности выбранного
  выпуска за 10 секунд после открытия детализации.
- **SC-002**: Для выбранного диапазона до 90 торговых дней система показывает
  список проблемной ликвидности не дольше чем за 10 секунд на локально
  загруженных данных.
- **SC-003**: В 100% случаев отсутствующие spread/depth данные отображаются
  отдельным состоянием, а не как нулевой spread.
- **SC-004**: Existing activity workflows remain usable: top anomalies, heatmap
  and issue detail still work when liquidity fields are absent.
- **SC-005**: В выводах по ликвидности каждый сигнал содержит выпуск, дату или
  scope, числовой показатель и понятную причину.
- **SC-006**: Пользователь может отфильтровать liquidity/spread view по типу
  ОФЗ и получить результат без смешивания валютных и рублевых выпусков.

## Assumptions

- Первый релиз feature использует данные, доступные в MOEX ISS для режима
  торгов ОФЗ, и не требует intraday trades или order book history.
- Historical liquidity может быть неполной; текущий snapshot допускается как
  отдельный контекст, но не должен притворяться полной историей.
- Liquidity score является объяснимой аналитической метрикой, а не
  рекомендацией купить или продать бумагу.
- Existing screen "Активность ОФЗ" остается основным workflow; новый слой
  расширяет его, а не создает отдельное приложение.
- Для UX сохраняется текущая модель фильтров: диапазон дат, тип ОФЗ, период
  выводов и scope сигналов.
