# Tasks: Сезонность активности ОФЗ

**Input**: Design documents from `specs/011-ofz-seasonality/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Обязательны для Core-доменной логики, summary findings и JSON contract.

## Phase 1: Setup

- [x] T001 Создать domain read-model `OfzSeasonality.cs` в `src/CurveAnalyzer.Core/Domain/`
- [x] T002 Расширить `OfzMarketSummary` seasonality context/evidence fields и schema version `1.5` в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [x] T003 Добавить `OfzSeasonalityContextOptions` в новый builder file `src/CurveAnalyzer.Core/Services/OfzSeasonalityContextBuilder.cs`

## Phase 2: Foundational

- [x] T004 [P] Добавить Core tests для weekday/month bucket aggregation, missing-vs-zero, insufficient baseline и one-year synthetic dataset performance в `tests/CurveAnalyzer.Core.Tests/Services/OfzSeasonalityContextBuilderTests.cs`
- [x] T005 [P] Добавить summary tests для `seasonalityContext`, source counts и JSON schema `1.5` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [x] T006 Реализовать `OfzSeasonalityContextBuilder` в `src/CurveAnalyzer.Core/Services/OfzSeasonalityContextBuilder.cs`
- [x] T007 Подключить seasonality context к `OfzMarketSummaryBuilder.Build()` через existing `ActivityMetrics` с учетом coupon type filter и signal scope для output findings
- [x] T008 Добавить seasonality source counts в `OfzSummarySourceCounts`

## Phase 3: User Story 1 - Сезонный профиль активности

- [x] T009 [US1] Добавить ViewModel collections/status для weekday/month buckets в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [x] T010 [US1] Добавить вкладку `Сезонность` в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`
- [x] T011 [US1] Добавить formatting converters для bucket labels, млн RUB и ratio в `src/CurveAnalyzer.Presentation.WPF/Converters/OfzSeasonalityConverters.cs`

## Phase 4: User Story 2 - Сравнение с сезонной базой

- [x] T012 [P] [US2] Добавить tests для no-future baseline, high/low seasonal activity findings, coupon type filter и `SignalScope = LastDay` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [x] T013 [US2] Реализовать seasonality findings в `OfzMarketSummaryBuilder`
- [x] T014 [US2] Добавить seasonality evidence fields и drill-down target mapping в `OfzMarketSummary`
- [x] T015 [US2] Проверить no recommendation language для seasonality findings в tests

## Phase 5: User Story 3 - Structured JSON и limitations

- [x] T016 [US3] Обновить JSON contract expectations по `contracts/ofz-seasonality.schema.json`
- [x] T017 [US3] Добавить serialization/roundtrip tests для `seasonalityContext`
- [x] T018 [US3] Отобразить seasonality limitations в UI вкладке `Сезонность`

## Phase 6: Polish & Verification

- [x] T019 Обновить `specs/ofz-analytics-roadmap.md`: `011` закрыт, следующим кандидатом остается `012-ofz-external-factors`
- [x] T020 Выполнить `dotnet build CurveAnalyzer.sln -c Release`
- [x] T021 Выполнить `dotnet test -c Release`
- [x] T022 Выполнить manual quickstart smoke из `specs/011-ofz-seasonality/quickstart.md`

## Dependencies

- Phase 2 зависит от Phase 1.
- US1 зависит от Phase 2.
- US2 зависит от Phase 2 и может выполняться параллельно с US1 после готовности `SeasonalityContext`.
- US3 зависит от Phase 2 и final shape `OfzMarketSummary`.
- Polish после выбранных user stories.

## Parallel Opportunities

- T004 и T005 можно выполнять параллельно.
- T009/T010/T011 можно выполнять после T006-T008, но они меняют разные файлы.
- T012 можно подготовить параллельно с UI задачами.
