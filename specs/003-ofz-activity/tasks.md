# Tasks: анализ активности ОФЗ

**Input**: Design documents from `specs/003-ofz-activity/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Новая доменная логика расчета активности требует automated tests. WPF rendering проверяется через build и manual smoke из quickstart.

**Organization**: Задачи сгруппированы по пользовательским историям, чтобы MVP `US1` можно было реализовать и проверить отдельно от heatmap, детализации и scatter.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: можно выполнять параллельно с другими задачами фазы, если файлы не пересекаются
- **[Story]**: пользовательская история из `spec.md`
- Каждая задача указывает конкретный файл или solution path

## Phase 1: Setup

**Purpose**: Зафиксировать baseline и подготовить структуру feature без изменения поведения.

- [X] T001 Запустить baseline validation для `CurveAnalyzer.sln`: `dotnet restore`, `dotnet build CurveAnalyzer.sln -c Release`, `dotnet test -c Release`; зафиксировать результат в `specs/003-ofz-activity/quickstart.md`
- [X] T002 Проверить текущее состояние `src/CurveAnalyzer.Infrastructure/MoexContext.cs`, `src/CurveAnalyzer.Infrastructure/DatabaseInitializer.cs` и локального storage path; подтвердить migration-aware upgrade risk в `specs/003-ofz-activity/research.md`
- [X] T003 [P] Создать директорию `src/CurveAnalyzer.Infrastructure/Migrations` для EF/schema upgrade artifacts
- [X] T004 [P] Создать пустой placeholder для будущих UI converters в `src/CurveAnalyzer.Presentation.WPF/Converters/ActivityHeatmapColorConverter.cs`

---

## Phase 2: Foundational

**Purpose**: Shared domain, persistence and ISS contracts that block all user stories.

**Critical**: User story implementation starts only after this phase compiles and schema initialization is safe for a disposable `zcyc.db` cache.

- [X] T005 [P] Создать domain model `OfzIssue` with `IsRub`, `IsStandardOfz` and currency/type marker classification in `src/CurveAnalyzer.Core/Domain/OfzIssue.cs`
- [X] T006 [P] Создать domain model `OfzDailyTrade` in `src/CurveAnalyzer.Core/Domain/OfzDailyTrade.cs`
- [X] T007 [P] Создать domain models `OfzActivityMetric`, `OfzActivityMetricStatus` and `OfzActivityAnomaly` in `src/CurveAnalyzer.Core/Domain/OfzActivityMetric.cs` and `src/CurveAnalyzer.Core/Domain/OfzActivityAnomaly.cs`
- [X] T008 [P] Создать derived model `OfzActivityHeatmapCell` and `OfzActivityIndexPoint` in `src/CurveAnalyzer.Core/Domain/OfzActivityViews.cs`
- [X] T009 Создать interfaces `IOfzActivityDataService` and `IOfzActivityRepository` in `src/CurveAnalyzer.Application/Interfaces/IOfzActivityDataService.cs` and `src/CurveAnalyzer.Application/Interfaces/IOfzActivityRepository.cs`
- [X] T010 Создать ISS DTO/parser models for `history`, `history.cursor`, `securities`, `marketdata` in `src/CurveAnalyzer.ApiServices/Data/OfzHistoryIssData.cs`
- [X] T011 Добавить `DbSet<OfzIssue>`, `DbSet<OfzDailyTrade>` and `DbSet<OfzActivityLoadState>` mappings and indexes to `src/CurveAnalyzer.Infrastructure/MoexContext.cs`
- [X] T012 Добавить `OfzActivityLoadState` persistence model in `src/CurveAnalyzer.Core/Domain/OfzActivityLoadState.cs`
- [X] T013 Реализовать migration-aware initialization that recreates legacy `EnsureCreated` cache and migrates migration-based databases in `src/CurveAnalyzer.Infrastructure/DatabaseInitializer.cs`
- [X] T014 Создать EF migration artifacts for baseline/current schema and OFZ activity tables in `src/CurveAnalyzer.Infrastructure/Migrations`
- [X] T015 Реализовать repository skeleton for issues, daily trades, load states and date range queries in `src/CurveAnalyzer.Infrastructure/Repositories/OfzActivityRepository.cs`
- [X] T016 Зарегистрировать только foundational persistence service `IOfzActivityRepository` in `src/CurveAnalyzer.Infrastructure/DependencyInjection.cs`
- [X] T017 Запустить `dotnet build CurveAnalyzer.sln -c Release` and выполнить compatibility check для legacy `zcyc.db`: old cache пересоздается, новые OFZ activity tables создаются, migration history корректна

**Checkpoint**: Foundation compiles, migration-based cache initialization works, user stories can start.

---

## Phase 3: User Story 1 - Найти всплески торгов по ОФЗ (Priority: P1) MVP

**Goal**: Пользователь выбирает диапазон дат и видит рейтинг выпусков с наибольшими относительными всплесками торгов.

**Independent Test**: На фиксированном наборе `OfzDailyTrade` расчет возвращает top anomalies, где ликвидный выпуск без необычного отклонения не доминирует только по абсолютному обороту.

### Tests for US1

- [X] T018 [P] [US1] Добавить tests для median baseline, minimum baseline days and missing value statuses in `tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs`
- [X] T019 [P] [US1] Добавить tests для `YieldMove` using `YieldAtWeightedAveragePrice` with `YieldClose` fallback in `tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs`
- [X] T020 [P] [US1] Добавить tests для top anomaly ranking by `ActivityScore` then `Value` in `tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs`

### Implementation for US1

- [X] T021 [US1] Реализовать `OfzActivityAnalyzer.CalculateMetrics`, `GetTopAnomalies` and yield fallback logic in `src/CurveAnalyzer.Core/Services/OfzActivityAnalyzer.cs`
- [X] T022 [US1] Реализовать ISS history paging, current-day snapshot loading and nullable numeric parsing in `src/CurveAnalyzer.ApiServices/OfzActivityOnlineDataService.cs`, then register `IOfzActivityDataService` in `src/CurveAnalyzer.ApiServices/DependencyInjection.cs`
- [X] T023 [US1] Реализовать idempotent save/load of issues, daily trades and load states in `src/CurveAnalyzer.Infrastructure/Repositories/OfzActivityRepository.cs`
- [X] T024 [US1] Реализовать orchestration `WarmUpRecentHistoryAsync`, `LoadActivityAsync` and `GetTopAnomaliesAsync` in `src/CurveAnalyzer.Application/OfzActivityService.cs`: preload recent 120 trading days by day, load selected range plus minimum 20 previous trade records per issue for baseline lookback, then register `OfzActivityService` in `src/CurveAnalyzer.Application/DependencyInjection.cs`
- [X] T025 [US1] Создать `OfzActivityViewModel` with warm-up command/status, date range, loading state, errors and top anomalies collection in `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T026 [US1] Создать `OfzActivityControl.xaml` with warm-up progress, date range controls, progress state, empty state, currency/type markers and top anomalies table in `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T027 [US1] Создать code-behind for activity view in `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml.cs`
- [X] T028 [US1] Добавить navigation command `ShowOfzActivityCommand` and selected chart wiring in `src/CurveAnalyzer.Presentation.WPF/ViewModels/MainViewModel.cs`
- [X] T029 [US1] Добавить navigation button and data template/view mapping for `OfzActivityViewModel` in `src/CurveAnalyzer.Presentation.WPF/Views/MainWindow.xaml`
- [X] T030 [US1] Зарегистрировать `OfzActivityViewModel` and `OfzActivityControl` in `src/CurveAnalyzer.Presentation.WPF/DependencyInjection.cs`
- [X] T031 [US1] Запустить `dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release` and fix activity analyzer regressions in `src/CurveAnalyzer.Core/Services/OfzActivityAnalyzer.cs`
- [X] T032 [US1] Запустить `dotnet build CurveAnalyzer.sln -c Release` and fix US1 compile/binding errors in `src/CurveAnalyzer.Presentation.WPF`

**Checkpoint**: US1 is independently usable: date range selection loads data and top anomalies table shows activity score and yield move.

---

## Phase 4: User Story 2 - Увидеть карту активности рынка (Priority: P2)

**Goal**: Пользователь видит heatmap активности по выпускам и датам, с отличием no-data and insufficient-baseline cells.

**Independent Test**: Выбор диапазона строит matrix cells by `SecId` and `TradeDate`; смена диапазона очищает old cells before showing new results.

### Tests for US2

- [X] T033 [P] [US2] Добавить tests for heatmap cell bucket generation and no-data/insufficient-baseline statuses in `tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs`

### Implementation for US2

- [X] T034 [US2] Реализовать `BuildHeatmapCells` in `src/CurveAnalyzer.Core/Services/OfzActivityAnalyzer.cs`
- [X] T035 [US2] Добавить heatmap result method to `OfzActivityService` in `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [X] T036 [US2] Добавить heatmap collection, selected cell state and range reset behavior to `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T037 [US2] Реализовать bucket-to-brush conversion in `src/CurveAnalyzer.Presentation.WPF/Converters/ActivityHeatmapColorConverter.cs`
- [X] T038 [US2] Добавить heatmap matrix UI and selected-cell interaction to `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T039 [US2] Запустить targeted tests and build: `dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release`, `dotnet build CurveAnalyzer.sln -c Release`

**Checkpoint**: US2 heatmap works independently on loaded activity data and does not retain stale cells.

---

## Phase 5: User Story 3 - Разобрать выбранный выпуск ОФЗ (Priority: P3)

**Goal**: Пользователь выбирает выпуск из рейтинга или heatmap and sees turnover, trades, price and yield history for that issue.

**Independent Test**: Selecting one `SecId` loads only that issue series; selecting another issue replaces all previous detail data.

### Tests for US3

- [X] T040 [P] [US3] Добавить tests for issue detail series ordering and preferred yield/price fallback in `tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs`

### Implementation for US3

- [X] T041 [US3] Добавить issue detail series methods to `OfzActivityAnalyzer` in `src/CurveAnalyzer.Core/Services/OfzActivityAnalyzer.cs`
- [X] T042 [US3] Добавить repository query for one issue across date range in `src/CurveAnalyzer.Infrastructure/Repositories/OfzActivityRepository.cs`
- [X] T043 [US3] Добавить `GetIssueDetailsAsync` to `OfzActivityService` in `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [X] T044 [US3] Добавить selected issue, detail series and stale-detail clearing to `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T045 [US3] Добавить LiveCharts2 detail series converters or presentation series builder in `src/CurveAnalyzer.Presentation.WPF/Converters/OfzIssueDetailSeriesConverter.cs`
- [X] T046 [US3] Добавить detail panel charts and issue header to `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T047 [US3] Запустить targeted tests and build: `dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release`, `dotnet build CurveAnalyzer.sln -c Release`

**Checkpoint**: US3 detail panel updates for selected issue and handles missing yield without hiding turnover.

---

## Phase 6: User Story 4 - Оценить активность по дюрациям (Priority: P4)

**Goal**: Пользователь видит secondary scatter view and activity index for context around duration/yield concentration.

**Independent Test**: For a selected date or range, issues with duration and yield appear on a duration/yield plane with activity-based emphasis.

### Tests for US4

- [X] T048 [P] [US4] Добавить tests for activity index aggregation and scatter point filtering in `tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs`

### Implementation for US4

- [X] T049 [US4] Добавить activity index and duration/yield scatter calculations to `src/CurveAnalyzer.Core/Services/OfzActivityAnalyzer.cs`
- [X] T050 [US4] Добавить activity index and scatter methods to `OfzActivityService` in `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [X] T051 [US4] Добавить scatter/index state to `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T052 [US4] Создать LiveCharts2 scatter/index series converter in `src/CurveAnalyzer.Presentation.WPF/Converters/OfzActivityOverviewConverters.cs`
- [X] T053 [US4] Добавить secondary scatter and activity index UI to `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T054 [US4] Запустить targeted tests and build: `dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release`, `dotnet build CurveAnalyzer.sln -c Release`

**Checkpoint**: US4 gives duration/yield context without blocking US1-US3 workflows when duration or yield is missing.

---

## Phase 7: Polish and Cross-Cutting Validation

**Purpose**: Сквозная проверка, cleanup and documentation sync.

- [X] T055 Запустить full validation: `dotnet restore`, `dotnet build CurveAnalyzer.sln -c Release`, `dotnet test -c Release`
- [X] T056 Выполнить manual smoke from `specs/003-ofz-activity/quickstart.md`, включая preliminary warm-up, диапазон до 90 торговых дней и проверку результата не дольше 10 секунд; document deviations in `specs/003-ofz-activity/quickstart.md`
- [X] T057 Проверить existing chart regressions for Yield Curve, Rate Change and Spread Change using `specs/003-ofz-activity/quickstart.md`
- [X] T058 Проверить `git diff -- AGENTS.md src tests specs/003-ofz-activity` and ensure unrelated ignored files or previous feature docs are not included
- [X] T059 [P] Обновить `specs/003-ofz-activity/quickstart.md` with final validation results and any accepted limitations
- [X] T060 [P] Обновить `specs/003-ofz-activity/contracts/iss-data-contract.md` if implementation uses a narrower `history.columns` set or additional ISS fields
- [X] T061 Упростить legacy SQLite handling: считать `zcyc.db` disposable cache and recreate old `EnsureCreated` DB in `src/CurveAnalyzer.Infrastructure/DatabaseInitializer.cs`
- [X] T062 Добавить minimum baseline median threshold and tests for weak baseline noise in `src/CurveAnalyzer.Core/Services/OfzActivityAnalyzer.cs` and `tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs`
- [X] T063 Нормализовать technical zero yield/duration as missing and show `n/a` in `src/CurveAnalyzer.ApiServices/OfzActivityOnlineDataService.cs`, `src/CurveAnalyzer.Core/Domain/OfzDailyTrade.cs` and `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T064 Зафиксировать partial manual smoke результата загрузки данных in `specs/003-ofz-activity/quickstart.md`
- [X] T065 Поднять OFZ warm-up window до последнего торгового года in `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [X] T066 Подключить OFZ history warm-up after ZCYC sync at startup in `src/CurveAnalyzer.Presentation.WPF/ViewModels/MainViewModel.cs`
- [X] T067 Добавить startup sync status text to `src/CurveAnalyzer.Presentation.WPF/Views/MainWindow.xaml` and validate WPF build
- [X] T068 Добавить дату и числовое форматирование в tooltip detail charts in `src/CurveAnalyzer.Presentation.WPF/Converters/OfzIssueDetailSeriesConverter.cs`
- [X] T069 Выровнять заголовки дат heatmap with cell grid step in `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T070 Закэшировать OFZ activity view during chart navigation to avoid rebuilding heavy heatmap/detail visuals on tab return in `src/CurveAnalyzer.Presentation.WPF/ViewModels/MainViewModel.cs` and `src/CurveAnalyzer.Presentation.WPF/Views/MainWindow.xaml`
- [X] T071 Preserve selected Rate/Spread periods across cached view navigation by making period initialization idempotent in `src/CurveAnalyzer.Presentation.WPF/ViewModels/RateChartViewModel.cs` and `src/CurveAnalyzer.Presentation.WPF/ViewModels/SpreadChartViewModel.cs`

---

## Phase 8: Metadata Enrichment and Activity Insights

**Purpose**: Не смешивать разные типы ОФЗ в анализе и показывать объяснимые выводы по heatmap/top anomalies.

- [X] T072 [P] Обновить `specs/003-ofz-activity/spec.md` and `specs/003-ofz-activity/data-model.md` with coupon/issue metadata classification and deterministic activity insights
- [X] T073 [P] Добавить tests for OFZ coupon type classification in `tests/CurveAnalyzer.Core.Tests/Domain/OfzIssueTests.cs`
- [X] T074 [P] Добавить tests for activity insight generation rules in `tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs`
- [X] T075 Добавить `OfzCouponType` and issue metadata fields to `src/CurveAnalyzer.Core/Domain/OfzIssue.cs`
- [X] T076 Добавить metadata mappings and migration updates for issue metadata fields in `src/CurveAnalyzer.Infrastructure/MoexContext.cs` and `src/CurveAnalyzer.Infrastructure/Migrations`
- [X] T077 Расширить ISS history/current snapshot column mapping for coupon metadata in `src/CurveAnalyzer.ApiServices/OfzActivityOnlineDataService.cs`
- [X] T078 Добавить domain view `OfzActivityInsight` and deterministic insight rules to `src/CurveAnalyzer.Core/Domain/OfzActivityViews.cs` and `src/CurveAnalyzer.Core/Services/OfzActivityAnalyzer.cs`
- [X] T079 Добавить `Insights` state and loading workflow to `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T080 Добавить UI block "Выводы" and issue coupon type columns/markers to `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T081 Обновить `specs/003-ofz-activity/contracts/iss-data-contract.md` with actual metadata columns and mark T060 closed
- [X] T082 Запустить targeted/full validation: `dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release`, `dotnet build CurveAnalyzer.sln -c Release`, `dotnet test -c Release`
- [X] T083 Добавить fallback-классификацию типов ОФЗ по `SECID`/номеру выпуска для старых cache-строк без metadata
- [X] T084 Исправить отображение блока "Выводы": перенос текста, читаемые строки и вертикальный scroll без обрезки
- [X] T085 Повторно запустить targeted tests и Release build после fallback/UI fixes
- [X] T086 Добавить локальный фильтр типа ОФЗ в `OfzActivityViewModel`: Все/ОФЗ-ПД/ОФЗ-ПК/ОФЗ-ИН/ОФЗ-АД/Валютные/Тип n/a
- [X] T087 Добавить `ComboBox` выбора типа в настройки экрана "Активность ОФЗ"
- [X] T088 Проверить Release build после добавления фильтра типа
- [X] T089 Добавить локальный фильтр периода актуальности выводов: 7 дней/14 дней/весь диапазон
- [X] T090 Добавить `ComboBox` выбора периода выводов в настройки экрана "Активность ОФЗ"
- [X] T091 Проверить Release build/test после добавления периода выводов

---

## Phase 9: Detail Chart Rendering Stabilization

**Purpose**: Устранить нестабильный layout LiveCharts2 в скрытых WPF tabs без изменения расчетов активности.

- [X] T092 Исправить переключение детализации "Сегменты/Выпуск": держать оба chart panel в visual tree and switch visibility instead of recreating hidden `TabItem` content in `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T093 Удалить неиспользуемые XAML templates экспериментального lazy tab rendering in `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T094 Проверить manual smoke: оба режима детализации отрисовывают графики после переключения вкладок

---

## Phase 10: Review Fixes and Cleanup

**Purpose**: Закрыть review findings после стабилизации UI and remove dead navigation state.

- [X] T095 Не использовать сегодняшний OFZ snapshot как неизменный cache: перекачивать текущую дату при повторной загрузке in `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [X] T096 Сделать замену дневных OFZ rows атомарной через transaction in `src/CurveAnalyzer.Infrastructure/Repositories/OfzActivityRepository.cs`
- [X] T097 Очищать старые OFZ results before invalid date error in `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T098 Убрать dead `SelectedChart`/DataTemplates after cached view navigation in `src/CurveAnalyzer.Presentation.WPF/ViewModels/MainViewModel.cs`, `src/CurveAnalyzer.Presentation.WPF/Views/MainWindow.xaml` and `src/CurveAnalyzer.Presentation.WPF/App.xaml`
- [X] T099 Убрать unused `HasIssueDetail` state in `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T100 Проверить `dotnet build CurveAnalyzer.sln -c Release` and `dotnet test -c Release`

---

## Phase 11: Lifetime and Tooling Hardening

**Purpose**: Убрать долгоживущий EF `DbContext` из WPF VM/service chains and restore EF Core design-time tooling for migrations.

- [X] T101 Перевести infrastructure persistence на `IDbContextFactory<MoexContext>` in `src/CurveAnalyzer.Infrastructure/DependencyInjection.cs`, `src/CurveAnalyzer.Infrastructure/Repositories/ZcycRepository.cs`, `src/CurveAnalyzer.Infrastructure/Repositories/OfzActivityRepository.cs` and `src/CurveAnalyzer.Infrastructure/DatabaseInitializer.cs`
- [X] T102 Уточнить Application/API/Presentation service lifetimes after factory-based persistence in `src/CurveAnalyzer.Application/DependencyInjection.cs`, `src/CurveAnalyzer.ApiServices/DependencyInjection.cs` and `src/CurveAnalyzer.Presentation.WPF/DependencyInjection.cs`
- [X] T103 Добавить design-time factory for EF migrations in `src/CurveAnalyzer.Infrastructure/MoexContextDesignTimeFactory.cs`
- [X] T104 Проверить EF tooling command for `MoexContext` migrations list
- [X] T105 Проверить `dotnet build CurveAnalyzer.sln -c Release` and `dotnet test -c Release`
- [X] T106 Зафиксировать lifetime/tooling validation result in `specs/003-ofz-activity/quickstart.md`

---

## Phase 12: Current-Day Data Finalization

**Purpose**: Не оставлять intraday snapshot текущего дня как финальные данные после смены торгового дня.

- [X] T107 Перекачивать недавние OFZ activity dates as refreshable window so current-day partial snapshot is replaced by final history data on later launches in `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [X] T108 Проверить `dotnet build CurveAnalyzer.sln -c Release` and `dotnet test -c Release`

---

## Phase 13: Signal Scope Filter

**Purpose**: Дать пользователю выбор между historical rolling view по всему диапазону и сигналами только на последнюю доступную дату.

- [X] T109 Добавить ViewModel-фильтр `Все дни`/`Последний день` for anomalies, insights, activity index and scatter in `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T110 Добавить selector режима сигналов in `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T111 Проверить `dotnet build CurveAnalyzer.sln -c Release` and `dotnet test -c Release`

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 Setup**: no dependencies.
- **Phase 2 Foundational**: depends on Phase 1 and blocks all user stories.
- **Phase 3 US1**: depends on Phase 2 and is the MVP.
- **Phase 4 US2**: depends on Phase 2; can be implemented after US1 or in parallel after shared service contracts exist, but UI integration is simpler after US1.
- **Phase 5 US3**: depends on Phase 2; practically depends on US1 selection surfaces.
- **Phase 6 US4**: depends on Phase 2 and can be delayed without affecting MVP.
- **Phase 7 Polish**: depends on selected user stories being complete.
- **Phase 8 Metadata/Insights**: depends on US1-US3 metrics and issue metadata persistence.
- **Phase 9 Detail Chart Rendering**: depends on US3-US4 detail chart UI and fixes WPF/LiveCharts2 rendering behavior.
- **Phase 10 Review Fixes**: depends on Phase 9 manual smoke and closes review findings before final handoff.
- **Phase 11 Lifetime/Tooling**: depends on Phase 10 cleanup and manual smoke; hardens persistence lifetime and migration tooling before handoff.
- **Phase 12 Current-Day Finalization**: depends on Phase 11 and fixes current-day partial OFZ cache finalization policy.
- **Phase 13 Signal Scope Filter**: depends on Phase 12 and adds UI filtering for last-date signal workflows without changing rolling score calculation.

### User Story Dependencies

- **US1**: no dependencies on other stories after foundation.
- **US2**: can reuse US1 loaded data and viewmodel; independently testable by heatmap state.
- **US3**: depends on ability to select an issue from US1 table or US2 heatmap.
- **US4**: depends on same loaded activity data, but remains optional secondary context.

### Within Each User Story

- Tests before implementation for Core calculations.
- Domain/service calculations before Application orchestration.
- Repository/ISS adapter before ViewModel workflows that need real data.
- ViewModel before XAML binding polish.
- Build/test at each checkpoint before moving to broader UI polish.

## Parallel Opportunities

- T005-T008 can run in parallel after Phase 1.
- T018-T020 can be authored in parallel because they cover separate analyzer scenarios.
- T022 and T023 can run in parallel after interfaces and DTOs exist.
- US2 converter/XAML work T037-T038 can run in parallel with service work T034-T035 after heatmap cell shape is stable.
- US3 detail converter T045 can run in parallel with repository/service work T042-T043 after detail series shape is stable.
- US4 converter/UI work T052-T053 can run in parallel with Core/Application calculations T049-T050 after scatter point shape is stable.

## Parallel Example: US1

```text
Task: "Добавить tests для median baseline, minimum baseline days and missing value statuses in tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs"
Task: "Добавить tests для YieldMove using YieldAtWeightedAveragePrice with YieldClose fallback in tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs"
Task: "Добавить tests для top anomaly ranking by ActivityScore then Value in tests/CurveAnalyzer.Core.Tests/Services/OfzActivityAnalyzerTests.cs"
```

## Parallel Example: US2

```text
Task: "Реализовать bucket-to-brush conversion in src/CurveAnalyzer.Presentation.WPF/Converters/ActivityHeatmapColorConverter.cs"
Task: "Добавить heatmap result method to OfzActivityService in src/CurveAnalyzer.Application/OfzActivityService.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 / US1.
3. Validate US1 with targeted Core tests, Release build and manual top anomalies smoke.
4. Stop for review before adding heatmap/detail complexity if MVP behavior is not stable.

### Incremental Delivery

1. Foundation: domain, persistence, ISS adapter and safe schema upgrade.
2. US1: top anomalies table.
3. US2: heatmap overview.
4. US3: selected issue detail.
5. US4: duration/yield scatter and activity index.
6. Final polish and regression smoke for existing charts.

### Scope Control

- Keep intraday trades, orderbook, macro correlations and recommendation logic out of this feature.
- Do not rewrite existing ZCYC sync unless required for safe shared database initialization.
- Do not introduce chart library dependencies below Presentation.
- Stage ignored SDD files explicitly with `git add -f` only when committing the SDD artifacts.
