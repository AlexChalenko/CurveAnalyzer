# Tasks: Календарь денежных потоков ОФЗ

**Input**: Design documents from `specs/010-ofz-cashflow-calendar/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Обязательны для Core-доменной логики, ISS parser и persistence.

## Phase 1: Setup

- [x] T001 Создать domain read-model `OfzCashflows.cs` в `src/CurveAnalyzer.Core/Domain/`
- [x] T002 Расширить `OfzMarketSummary` cashflow context/evidence fields в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [x] T003 Добавить `IOfzCashflowDataService` в `src/CurveAnalyzer.Application/Interfaces/`
- [x] T004 Расширить `IOfzActivityRepository` методами cashflow cache в `src/CurveAnalyzer.Application/Interfaces/IOfzActivityRepository.cs`
- [x] T005 Добавить ISS DTO/parser helpers в `src/CurveAnalyzer.ApiServices/Data/OfzCashflowIssData.cs`

## Phase 2: Foundational

- [x] T006 [P] Добавить Core tests для event filtering, missing-vs-zero и event-near-activity window в `tests/CurveAnalyzer.Core.Tests/Services/OfzCashflowContextBuilderTests.cs`
- [x] T007 [P] Добавить parser tests для `bondization` blocks в `tests/CurveAnalyzer.Infrastructure.Tests/ApiServices/OfzCashflowOnlineDataServiceTests.cs`
- [x] T008 [P] Добавить repository tests для save/get cashflow events в `tests/CurveAnalyzer.Infrastructure.Tests/Repositories/OfzActivityRepositoryTests.cs`
- [x] T009 Реализовать `OfzCashflowContextBuilder` в `src/CurveAnalyzer.Core/Services/OfzCashflowContextBuilder.cs`
- [x] T010 Реализовать `OfzCashflowOnlineDataService` в `src/CurveAnalyzer.ApiServices/`
- [x] T011 Зарегистрировать `IOfzCashflowDataService` в `src/CurveAnalyzer.ApiServices/DependencyInjection.cs`
- [x] T012 Добавить EF mapping и migration для cashflow events в `src/CurveAnalyzer.Infrastructure/`
- [x] T013 Реализовать repository methods в `src/CurveAnalyzer.Infrastructure/Repositories/OfzActivityRepository.cs`

## Phase 3: User Story 1 - Ближайшие события

- [x] T014 Подключить cashflow loading/cache в `src/CurveAnalyzer.Application/OfzActivityService.cs`
- [x] T015 Передать cashflow events в `OfzMarketSummaryBuilder.Build()` через input model
- [x] T016 Добавить `CashflowContext` в `OfzMarketSummary` output и JSON schema version bump
- [x] T017 Добавить ViewModel collections/status для cashflow context в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [x] T018 Добавить overview-блок cashflow events в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`

## Phase 4: User Story 2 - Activity near event findings

- [x] T019 Добавить tests для findings near coupon/offer/maturity в `OfzMarketSummaryBuilderTests.cs`
- [x] T020 Реализовать cashflow-backed findings в `OfzMarketSummaryBuilder`
- [x] T021 Добавить evidence fields и drill-down target mapping
- [x] T022 Отобразить cashflow evidence в selected finding UI

## Phase 5: User Story 3 - Issue detail calendar

- [x] T023 Добавить selected issue cashflow calendar в `OfzActivityViewModel`
- [x] T024 Добавить issue-detail table в `OfzActivityIssueDetailControl.xaml`
- [x] T025 Добавить converters formatting для event type/date/value/source

## Phase 6: Polish & Verification

- [x] T026 Обновить contracts/schema expectations
- [x] T027 Проверить no recommendation language в tests
- [x] T028 Выполнить `dotnet build CurveAnalyzer.sln -c Release`
- [x] T029 Выполнить `dotnet test -c Release`
- [x] T030 Выполнить manual quickstart smoke

## Dependencies

- Phase 2 зависит от Phase 1.
- US1 зависит от Phase 2.
- US2 зависит от US1 context и builder output.
- US3 зависит от US1 ViewModel state.
- Polish после выбранных user stories.
