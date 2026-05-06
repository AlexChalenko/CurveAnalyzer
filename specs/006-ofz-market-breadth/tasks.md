# Tasks: OFZ Market Breadth

**Input**: Design documents from `specs/006-ofz-market-breadth/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Обязательны для Core-доменной логики по конституции проекта.

**Organization**: Задачи сгруппированы по user stories, чтобы каждый инкремент
можно было проверить отдельно.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Подготовить общий contract surface для market breadth.

- [X] T001 Создать domain read-model `MarketBreadthDay`, `YieldDirectionBreakdown`, `TurnoverConcentration`, `TypeTurnoverShare`, `MarketBreadthContributor` в `src/CurveAnalyzer.Core/Domain/OfzMarketBreadth.cs`
- [X] T002 Расширить `OfzSummaryFindingKind`, `OfzSummaryScope`, `OfzSummaryDrillDownTarget`, `OfzFindingEvidence` breadth-полями в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T003 Добавить `BreadthDays` и schema version `1.1` в `OfzMarketSummary` в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T004 Обновить JSON serialization expectations для summary contract в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Подключить breadth pipeline к existing summary builder без UI.

**CRITICAL**: User story tasks зависят от этой фазы.

- [X] T005 Добавить `OfzMarketBreadthOptions` и thresholds для unchanged direction, broad move и concentration в `src/CurveAnalyzer.Core/Domain/OfzMarketBreadth.cs`
- [X] T006 Расширить `OfzMarketSummaryOptions` breadth options в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T007 Добавить skeleton методов построения breadth days и contributors в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T008 Обеспечить применение `StartDate`, `EndDate`, `CouponTypeFilter`, `SignalScope` к breadth input в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`

**Checkpoint**: `OfzMarketSummaryBuilder.Build()` возвращает пустую или заполненную `BreadthDays` коллекцию без изменения UI.

---

## Phase 3: User Story 1 - Увидеть ширину движения рынка (Priority: P1) MVP

**Goal**: Пользователь видит по дням распределение выпусков по направлению доходности.

**Independent Test**: Core test строит несколько выпусков с доходностями и проверяет `YieldUpCount`, `YieldDownCount`, `UnchangedCount`, `NotComparableCount`; удаление будущей даты не меняет прошлую классификацию.

### Tests for User Story 1

- [X] T009 [US1] Добавить тест no-look-ahead для direction classification в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T010 [US1] Добавить тест сумм direction counts и comparable base в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T011 [US1] Добавить тест missing previous/yield попадает в `NotComparable`, а не `Unchanged`, в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 1

- [X] T012 [US1] Реализовать поиск предыдущей доступной доходности выпуска без look-ahead в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T013 [US1] Реализовать классификацию `YieldDirection`, `YieldDirectionBreakdown` и `ActiveIssueShare` в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T014 [US1] Добавить breadth finding для широкого движения доходностей в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T015 [US1] Отобразить breadth finding и evidence в summary block в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`

**Checkpoint**: В structured summary и UI выводах есть объяснимый breadth факт по направлению доходностей.

---

## Phase 4: User Story 2 - Оценить концентрацию оборота (Priority: P2)

**Goal**: Пользователь видит, сконцентрирован ли оборот дня в top-5/top-10 выпусках.

**Independent Test**: Core test строит день с известными оборотами, проверяет `Top5Share`, `Top10Share`, calculation base и поведение при количестве выпусков меньше 10.

### Tests for User Story 2

- [X] T016 [US2] Добавить тест top-5/top-10 concentration на known turnover set в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T017 [US2] Добавить тест top-N base меньше 10 выпусков в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T018 [US2] Добавить тест missing turnover не становится zero-contributor в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 2

- [X] T019 [US2] Реализовать `TurnoverConcentration` и top contributors по обороту в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T020 [US2] Добавить concentration finding с evidence top-5/top-10 в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T021 [US2] Расширить structured JSON contract breadth/concentration fields в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T022 [US2] Показать concentration evidence в выбранном summary finding в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`

**Checkpoint**: По выбранному периоду видна концентрация оборота и база расчета.

---

## Phase 5: User Story 3 - Сравнить вклад типов ОФЗ (Priority: P3)

**Goal**: Пользователь видит вклад ОФЗ-ПД, ОФЗ-ПК, ОФЗ-ИН, ОФЗ-АД, валютных и unknown выпусков.

**Independent Test**: Core test строит mixed coupon type набор и проверяет, что shares суммируются к общему обороту и фильтр типа ограничивает расчет.

### Tests for User Story 3

- [X] T023 [US3] Добавить тест type turnover shares для mixed coupon type input в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T024 [US3] Добавить тест coupon type filter ограничивает breadth days и type shares в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T025 [US3] Добавить тест unknown coupon type не теряется молча в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 3

- [X] T026 [US3] Реализовать расчет `TypeTurnoverShare` по дням и периоду в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T027 [US3] Добавить finding для доминирующего типа ОФЗ без recommendation language в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T028 [US3] Добавить limitations для unknown/currency mixed type share в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T029 [US3] Отобразить type share evidence в summary/details тексте в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`

**Checkpoint**: Переключение типа ОФЗ обновляет breadth metrics и выводы без stale значений.

---

## Phase 6: User Story 4 - Разобрать день market breadth (Priority: P4)

**Goal**: Пользователь выбирает breadth day и видит contributors, direction counts, concentration и limitations.

**Independent Test**: UI/manual scenario выбирает breadth finding/day и проверяет, что детализация показывает состав сигнала и ограничения данных.

### Tests for User Story 4

- [X] T030 [US4] Добавить Core test для day drill-down contributors и limitations в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T031 [US4] Добавить Core test для provisional/snapshot marker на breadth day в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 4

- [X] T032 [US4] Добавить `MarketBreadthDay` drill-down target и mapping finding -> selected day в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T033 [US4] Реализовать выбор breadth day из summary finding в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T034 [US4] Добавить коллекцию/таблицу contributors для выбранного breadth day в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T035 [US4] Добавить компактный UI для breadth day детализации в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`
- [X] T036 [US4] Убедиться, что переключение `Обзор` / `Сигналы` / `Детализация` не создает chart layout regression в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityDetailControl.xaml.cs`

**Checkpoint**: Выбранный breadth day можно разобрать без открытия БД или raw JSON.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Проверка, contract sync и UX cleanup.

- [X] T037 Обновить schema examples/expectations для `contracts/market-breadth.schema.json` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T038 Проверить отсутствие recommendation language в новых breadth findings в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T039 Добавить performance smoke test для периода до одного торгового года и около 100 выпусков в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T040 Обновить status/header text для counts `breadth days`, `direction`, `concentration` в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T041 Выполнить `dotnet build CurveAnalyzer.sln -c Release` для `CurveAnalyzer.sln`
- [X] T042 Выполнить `dotnet test -c Release` для `CurveAnalyzer.sln`
- [X] T043 Выполнить ручной сценарий из `specs/006-ofz-market-breadth/quickstart.md`
- [X] T044 Сделать market breadth заметным в `Обзор`: добавить таблицу breadth days в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`
- [X] T045 Исправить breadth baseline: direction использует историю до окна выводов, а UI показывает только окно выводов

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: стартует сразу.
- **Phase 2 Foundational**: зависит от Phase 1 и блокирует user stories.
- **US1 / Phase 3**: MVP, зависит от Phase 2.
- **US2 / Phase 4**: зависит от Phase 2; может идти параллельно с US1 после общих моделей, но UI evidence удобнее после US1.
- **US3 / Phase 5**: зависит от Phase 2; может идти параллельно с US2.
- **US4 / Phase 6**: зависит от US1-US3, потому что drill-down использует их output.
- **Phase 7 Polish**: после выбранных user stories.

### User Story Dependencies

- **US1**: базовый breadth direction, MVP.
- **US2**: независимо добавляет concentration к breadth day.
- **US3**: независимо добавляет type share к breadth day.
- **US4**: интеграционный drill-down поверх US1-US3.

### Parallel Opportunities

- После T001-T008 можно параллелить US2 и US3, потому что они в основном
  добавляют разные расчетные блоки в builder.
- Тестовые задачи внутри одного файла `OfzMarketSummaryBuilderTests.cs` не
  помечены `[P]`, чтобы избежать конфликтов при параллельной записи.
- UI задачи T033-T036 лучше выполнять последовательно из-за shared
  `OfzActivityViewModel` и XAML.

---

## Implementation Strategy

### MVP First (US1)

1. Завершить T001-T008.
2. Реализовать T009-T015.
3. Запустить Core tests для `OfzMarketSummaryBuilderTests`.
4. Проверить в UI, что breadth finding появился и не ломает существующий summary.

### Incremental Delivery

1. US1: direction breadth.
2. US2: turnover concentration.
3. US3: coupon type share.
4. US4: day drill-down.
5. Polish: contract, build, tests, manual quickstart.

### Suggested Agent Split

- **Worker A**: Core domain + US1 direction logic (`OfzMarketBreadth.cs`, `OfzMarketSummary.cs`, `OfzMarketSummaryBuilder.cs`, tests).
- **Worker B**: US2/US3 calculations and tests in the same Core files after foundation.
- **Worker C**: UI binding/detail after Core output stabilizes (`OfzActivityViewModel.cs`, `OfzActivityDetailControl.xaml`).
