# Tasks: Внешние факторы активности ОФЗ

**Input**: Design documents from `specs/012-ofz-external-factors/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Обязательны для Core builder, no-lookahead alignment, summary
findings и JSON contract.

## Phase 1: Setup

- [x] T001 Создать domain read-model `OfzExternalFactors.cs` в `src/CurveAnalyzer.Core/Domain/`
- [x] T002 Расширить `OfzMarketSummary` external factor context/evidence fields, limitations и schema version `1.6` в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [x] T003 Добавить options/input shape для `OfzExternalFactorsContextBuilder` в `src/CurveAnalyzer.Core/Services/OfzExternalFactorsContextBuilder.cs`

## Phase 2: Foundational

- [x] T004 [P] Добавить Core tests для CBR latest-on-or-before, no future lookup, missing-as-limitation и snapshot/provisional propagation в `tests/CurveAnalyzer.Core.Tests/Services/OfzExternalFactorsContextBuilderTests.cs`
- [x] T005 [P] Добавить summary tests для `externalFactorsContext`, source counts и JSON schema `1.6` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [x] T006 Реализовать `OfzExternalFactorsContextBuilder` для CBR key rate, MOEX index context и OFZ-derived special metrics
- [x] T007 Подключить factor context к `OfzMarketSummaryBuilder.Build()` без новых сетевых операций и без новой persistence schema
- [x] T008 Добавить external factor source counts в `OfzSummarySourceCounts`

## Phase 3: User Story 1 - Внешний фон периода

- [x] T009 [US1] Добавить ViewModel collections/status для factor series и limitations в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [x] T010 [US1] Добавить вкладку `Факторы` в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityOverviewControl.xaml`
- [x] T011 [US1] Добавить formatting converters для factor kind/source/value/change/status в `src/CurveAnalyzer.Presentation.WPF/Converters/OfzExternalFactorsConverters.cs`

## Phase 4: User Story 2 - Связь активности с факторами

- [x] T012 [P] [US2] Добавить tests для activity-with-factor-move, activity-without-factor-move, yield move link и `SignalScope = LastDay` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [x] T013 [US2] Реализовать external factor findings в `OfzMarketSummaryBuilder`
- [x] T014 [US2] Добавить factor evidence fields и drill-down target mapping в `OfzMarketSummary`
- [x] T015 [US2] Проверить neutral/no-recommendation wording для factor findings в tests

## Phase 5: User Story 3 - Structured JSON и contracts

- [x] T016 [US3] Обновить JSON contract expectations по `contracts/ofz-external-factors.schema.json` и `tests/CurveAnalyzer.Core.Tests/Domain/OfzExternalFactorsContractTests.cs`
- [x] T017 [US3] Добавить serialization/roundtrip tests для `externalFactorsContext` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [x] T018 [US3] Отобразить external factor limitations в UI вкладке `Факторы`

## Phase 6: Polish & Verification

- [x] T019 Обновить `specs/ofz-analytics-roadmap.md`: `012` переведен из кандидата в активный этап
- [x] T020 Выполнить `dotnet restore`
- [x] T021 Выполнить `dotnet build CurveAnalyzer.sln -c Release`
- [x] T022 Выполнить `dotnet test -c Release`
- [x] T023 Выполнить manual quickstart smoke из `specs/012-ofz-external-factors/quickstart.md`

## Dependencies

- Phase 2 зависит от Phase 1.
- US1 зависит от Phase 2.
- US2 зависит от Phase 2 и может выполняться параллельно с US1 после готовности
  `ExternalFactorContext`.
- US3 зависит от Phase 2 и final shape `OfzMarketSummary`.
- Polish после выбранных user stories.

## Parallel Opportunities

- T004 и T005 можно выполнять параллельно.
- T009/T010/T011 можно выполнять после T006-T008, но они меняют разные файлы.
- T012 можно подготовить параллельно с UI задачами.
