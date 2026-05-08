# Tasks: Индексный контекст ОФЗ

**Input**: Design documents from `specs/009-ofz-index-context/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Обязательны для Core-доменной логики, ISS parser и persistence по
конституции проекта.

**Organization**: Задачи сгруппированы по user stories, чтобы каждый инкремент
можно было проверить отдельно.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Подготовить общий domain/application contract surface для index context.

- [X] T001 Создать domain read-model `OfzMarketIndexSeries`, `OfzMarketIndexPoint`, `OfzIndexContextDay`, `OfzIndexDailyMove`, `OfzIndexSegmentContext` в `src/CurveAnalyzer.Core/Domain/OfzMarketIndex.cs`
- [X] T002 Расширить `OfzMarketSummary`, `OfzSummaryFindingKind`, `OfzSummaryScope`, `OfzSummaryDrillDownTarget`, `OfzFindingEvidence` index-полями в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T003 Добавить `IndexContextDays`, `IndexSegments` и schema version update в `OfzMarketSummary` в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T004 Создать `IOfzIndexDataService` для history/current index загрузки в `src/CurveAnalyzer.Application/Interfaces/IOfzIndexDataService.cs`
- [X] T005 Расширить `IOfzActivityRepository` методами чтения/сохранения index points в `src/CurveAnalyzer.Application/Interfaces/IOfzActivityRepository.cs`
- [X] T006 Добавить ISS DTO/table parser для index history/current blocks в `src/CurveAnalyzer.ApiServices/Data/OfzIndexIssData.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Подключить загрузку, cache и Core builder без UI-зависимостей.

**CRITICAL**: User story tasks зависят от этой фазы.

- [X] T007 [P] Добавить Core tests для daily move, missing previous point и missing-vs-zero в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`
- [X] T008 [P] Добавить parser tests для MOEX ISS history/current index blocks в `tests/CurveAnalyzer.Infrastructure.Tests/ApiServices/OfzIndexOnlineDataServiceTests.cs`
- [X] T009 [P] Добавить repository tests для save/get index points и restart cache scenario в `tests/CurveAnalyzer.Infrastructure.Tests/Repositories/OfzActivityRepositoryTests.cs`
- [X] T010 Создать `OfzIndexContextOptions` и skeleton `OfzIndexContextBuilder` в `src/CurveAnalyzer.Core/Services/OfzIndexContextBuilder.cs`
- [X] T011 Реализовать `OfzIndexOnlineDataService` history/current методы в `src/CurveAnalyzer.ApiServices/OfzIndexOnlineDataService.cs`
- [X] T012 Зарегистрировать `IOfzIndexDataService` в `src/CurveAnalyzer.ApiServices/DependencyInjection.cs`
- [X] T013 Добавить `DbSet<OfzMarketIndexPoint>` и EF mapping/indexes в `src/CurveAnalyzer.Infrastructure/MoexContext.cs`
- [X] T014 Добавить EF migration для index points cache в `src/CurveAnalyzer.Infrastructure/Migrations`
- [X] T015 Реализовать repository save/get index points в `src/CurveAnalyzer.Infrastructure/Repositories/OfzActivityRepository.cs`
- [X] T016 Подключить `IOfzIndexDataService` в constructor `OfzActivityService` в `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [X] T017 Добавить ensure/load index points для выбранного периода в `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [X] T018 Передать index points в `OfzMarketSummaryBuilder.Build()` через `OfzMarketSummaryInput` в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`

**Checkpoint**: Application может загрузить и отдать index points для периода без изменений WPF.

---

## Phase 3: User Story 1 - Видеть общий индексный фон ОФЗ (Priority: P1) MVP

**Goal**: Пользователь видит `RGBI`/`RGBITR` по датам выбранного периода и понимает missing/provisional status.

**Independent Test**: загрузить период с index history и проверить, что overview показывает `RGBI`/`RGBITR`, daily change считается только от предыдущей валидной точки, missing values отображаются как `n/a`.

### Tests for User Story 1

- [X] T019 [US1] Добавить Core test для required `RGBI`/`RGBITR` context day в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`
- [X] T020 [US1] Добавить Core test для previous valid point daily change без forward-fill нулей в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`
- [X] T021 [US1] Добавить Core test для missing required series limitation в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`
- [X] T022 [US1] Добавить Core test для provisional snapshot marker в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`

### Implementation for User Story 1

- [X] T023 [US1] Реализовать mapping required whole-market series `RGBI` и `RGBITR` в `src/CurveAnalyzer.Core/Services/OfzIndexContextBuilder.cs`
- [X] T024 [US1] Реализовать расчет `OfzIndexDailyMove` от предыдущей валидной точки в `src/CurveAnalyzer.Core/Services/OfzIndexContextBuilder.cs`
- [X] T025 [US1] Реализовать `OfzIndexContextDay` и required-series limitations в `src/CurveAnalyzer.Core/Services/OfzIndexContextBuilder.cs`
- [X] T026 [US1] Добавить index context output в `OfzMarketSummaryBuilder.Build()` в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T027 [US1] Добавить `IndexContextDays`, `HasIndexContext`, status text и selected latest index state в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T028 [US1] Создать converters для index value/change/status formatting в `src/CurveAnalyzer.Presentation.WPF/Converters/OfzIndexContextConverters.cs`
- [X] T029 [US1] Добавить overview-блок index context для `RGBI`/`RGBITR` в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`
- [X] T030 [US1] Обновить status/header counts для index context в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`

**Checkpoint**: В `Обзор` виден whole-market index context без пустых блоков и без нулей вместо `n/a`.

---

## Phase 4: User Story 2 - Сравнить всплески активности с движением индекса (Priority: P2)

**Goal**: Пользователь видит, совпал ли activity/breadth сигнал с движением индекса или был локальным.

**Independent Test**: Core test строит день с высоким activity score и известным `RGBI` move, затем проверяет index-backed finding и neutral wording.

### Tests for User Story 2

- [X] T031 [US2] Добавить Core test для finding `ActivityWithIndexMove` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T032 [US2] Добавить Core test для finding `ActivityWithoutIndexMove` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T033 [US2] Добавить Core test no-recommendation language для index-backed findings в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T034 [US2] Добавить Core test insight-period filtering для index-backed findings в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 2

- [X] T035 [US2] Добавить thresholds meaningful index move `0.25%` для `Close` и `0.10 п.п.` для `Yield` в `OfzMarketSummaryOptions` в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T036 [US2] Реализовать index-backed activity/breadth findings в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T037 [US2] Добавить index evidence fields и drill-down target mapping в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T038 [US2] Отобразить index evidence в selected summary finding в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T039 [US2] Добавить текстовые строки index-backed findings в overview summary UI в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`

**Checkpoint**: Summary findings объясняют index фон выбранных активных дней без recommendation language.

---

## Phase 5: User Story 3 - Видеть duration-сегменты рынка (Priority: P3)

**Goal**: Пользователь видит короткие/средние/длинные segment series и их period/daily movement.

**Independent Test**: Core test строит набор segment series, проверяет buckets, partial availability и limitations.

### Tests for User Story 3

- [X] T040 [US3] Добавить Core test для duration bucket mapping `RUGBICP1Y`/`RUGBITR1Y` в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`
- [X] T041 [US3] Добавить Core test для partial segment availability limitations в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`
- [X] T042 [US3] Добавить Core test для period change segment context в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`

### Implementation for User Story 3

- [X] T043 [US3] Добавить configured segment series list `RUGBICP1Y`, `RUGBICP3Y`, `RUGBICP5Y`, `RUGBICP7Y+`, `RUGBITR1Y`, `RUGBITR3Y`, `RUGBITR5Y`, `RUGBITR7Y+` в `src/CurveAnalyzer.Core/Services/OfzIndexContextBuilder.cs`
- [X] T044 [US3] Реализовать `OfzIndexSegmentContext` и period change расчет в `src/CurveAnalyzer.Core/Services/OfzIndexContextBuilder.cs`
- [X] T045 [US3] Добавить segment context в `OfzMarketSummary` output в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T046 [US3] Добавить segment context collections/status в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T047 [US3] Добавить duration-segment table или compact panel в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`
- [X] T048 [US3] Обновить formatting converters для segment change/yield/duration в `src/CurveAnalyzer.Presentation.WPF/Converters/OfzIndexContextConverters.cs`

**Checkpoint**: Duration segments отображаются частично и не скрывают whole-market context при отсутствии отдельных series.

---

## Phase 6: User Story 4 - Проверить день через индексный drill-down (Priority: P4)

**Goal**: Пользователь выбирает finding/day и видит index values, previous date, changes, yield, duration, source status и limitations.

**Independent Test**: выбрать index-backed finding или день и проверить, что detail panel показывает числовое evidence без raw database/JSON.

### Tests for User Story 4

- [X] T049 [US4] Добавить Core test для day drill-down data completeness в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`
- [X] T050 [US4] Добавить Core mapping test для finding -> index context day в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 4

- [X] T051 [US4] Добавить selected index context day state и selection logic в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T052 [US4] Реализовать selection mapping из `OfzSummaryFinding` в index context day в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T053 [US4] Добавить index day detail panel в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`
- [X] T054 [US4] Убедиться, что переключение `Обзор` / `Сигналы` / `Детализация` очищает stale selected index state в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`

**Checkpoint**: Index-backed finding/day можно разобрать без открытия БД или raw JSON.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Проверка, contract sync и UX cleanup.

- [X] T055 Обновить JSON serialization expectations для `contracts/ofz-index-context.schema.json` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T056 Добавить performance smoke test для периода до одного торгового года и 10 index series в `tests/CurveAnalyzer.Core.Tests/Services/OfzIndexContextBuilderTests.cs`
- [X] T057 Проверить отсутствие visible empty chart area для no-index-data case в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`
- [X] T058 Проверить, что type filter не пересчитывает index context как filtered OFZ set, в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T059 Выполнить `dotnet restore` для `CurveAnalyzer.sln`
- [X] T060 Выполнить `dotnet build CurveAnalyzer.sln -c Release` для `CurveAnalyzer.sln`
- [X] T061 Выполнить `dotnet test -c Release` для `CurveAnalyzer.sln`
- [X] T062 Выполнить ручной сценарий из `specs/009-ofz-index-context/quickstart.md`
- [X] T063 Сверить SDD contracts с итоговой реализацией в `specs/009-ofz-index-context/contracts/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: стартует сразу.
- **Phase 2 Foundational**: зависит от Phase 1 и блокирует user stories.
- **US1 / Phase 3**: MVP, зависит от Phase 2.
- **US2 / Phase 4**: зависит от Phase 2 и полезнее после US1, потому что использует `IndexContextDays`.
- **US3 / Phase 5**: зависит от Phase 2; может идти параллельно с US2 после Core builder foundation.
- **US4 / Phase 6**: зависит от US1-US3, потому что drill-down использует output summary/findings/segments.
- **Phase 7 Polish**: после выбранных user stories.

### User Story Dependencies

- **US1**: whole-market index context, MVP.
- **US2**: index-backed findings; требует context days из US1.
- **US3**: duration segment context; расширяет builder/UI независимо от US2.
- **US4**: drill-down поверх US1-US3.

### Parallel Opportunities

- T007, T008, T009 можно делать параллельно, потому что они пишут разные test files/sections.
- После T010-T018 можно параллелить US2 и US3, если shared `OfzIndexContextBuilder` contract уже стабилен.
- Тесты в одном файле `OfzIndexContextBuilderTests.cs` не помечены `[P]`, чтобы избежать конфликтов записи.
- UI задачи в `OfzActivityViewModel.cs` лучше выполнять последовательно.

---

## Implementation Strategy

### MVP First (US1)

1. Завершить T001-T018.
2. Реализовать T019-T030.
3. Запустить targeted Core/Infrastructure tests.
4. Проверить в UI, что `RGBI`/`RGBITR` context появился и не ломает existing overview.

### Incremental Delivery

1. US1: whole-market `RGBI`/`RGBITR` context.
2. US2: index-backed activity/breadth findings.
3. US3: duration segments.
4. US4: day drill-down.
5. Polish: schema, performance, build, tests, manual quickstart.

### Suggested Agent Split

- **Worker A**: Core domain + `OfzIndexContextBuilder` + Core tests (`OfzMarketIndex.cs`, `OfzIndexContextBuilder.cs`, `OfzIndexContextBuilderTests.cs`).
- **Worker B**: ISS/persistence pipeline (`IOfzIndexDataService.cs`, `OfzIndexOnlineDataService.cs`, `OfzIndexIssData.cs`, `MoexContext.cs`, repository, migration, Infrastructure tests).
- **Worker C**: Summary/UI integration after Core/Application contracts stabilize (`OfzMarketSummaryBuilder.cs`, `OfzActivityViewModel.cs`, WPF views/converters).
