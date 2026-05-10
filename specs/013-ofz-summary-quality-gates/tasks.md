# Tasks: Quality gates для OFZ summary/export

**Input**: Design documents from `specs/013-ofz-summary-quality-gates/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Обязательны. Feature является quality-gate слоем для Core summary
contract.

## Phase 1: Setup

- [X] T001 Создать test file `tests/CurveAnalyzer.Core.Tests/Services/OfzSummaryQualityGateTests.cs`
- [X] T002 Добавить representative summary fixture/helper внутри `OfzSummaryQualityGateTests.cs` без network/file I/O
- [X] T003 Добавить JSON serializer options/helper assertions для date-only и enum strings в `OfzSummaryQualityGateTests.cs`

## Phase 2: Foundational Gates

- [X] T004 [P] Добавить gate `FullSummary_ContainsRequiredSectionsAndSchemaVersion16` в `OfzSummaryQualityGateTests.cs`
- [X] T005 [P] Добавить gate `FullSummary_SourceCountsMatchRepresentativeInput` в `OfzSummaryQualityGateTests.cs`
- [X] T006 [P] Добавить gate `FullSummary_SerializesTradeAndEventDatesAsDateOnly` в `OfzSummaryQualityGateTests.cs`

## Phase 3: User Story 1 - Полный summary contract

- [X] T007 [US1] Проверить в gate наличие `breadthDays`, `indexContextDays`, `cashflowContext`, `seasonalityContext`, `externalFactorsContext`, `specialMetrics`, `limitations`, `sourceCounts`
- [X] T008 [US1] Проверить, что representative summary строится с `schemaVersion = "1.6"` и без пустого `externalFactorsContext`
- [X] T009 [US1] Проверить, что gate fail surface имеет понятные Assert-сообщения для missing section/source count

## Phase 4: User Story 2 - Важные findings не теряются

- [X] T010 [US2] Добавить gate `FullSummary_KeepsExternalFactorFindingUnderDefaultLimit` в `OfzSummaryQualityGateTests.cs`
- [X] T011 [US2] Проверить, что при непустых `externalFactorsContext.links` итоговые `findings` содержат `ExternalFactorActivity`
- [X] T012 [US2] Добавить no-recommendation/no-causality wording gate для representative findings/limitations

## Phase 5: User Story 3 - Missing-data policy

- [X] T013 [US3] Добавить focused scenario для missing special/external factor data в `OfzSummaryQualityGateTests.cs`
- [X] T014 [US3] Проверить, что missing значения сериализуются как `null`/missing, не как numeric `0`
- [X] T015 [US3] Проверить, что missing/snapshot/provisional cases содержат соответствующие limitations/flags

## Phase 6: Docs & Verification

- [X] T016 Обновить `specs/ofz-analytics-roadmap.md`: добавить 013 как активный/следующий quality этап
- [X] T017 Выполнить targeted validation: `dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release --filter "FullyQualifiedName~OfzSummaryQualityGateTests"`
- [X] T018 Выполнить `dotnet build CurveAnalyzer.sln -c Release`
- [X] T019 Выполнить `dotnet test -c Release`
- [X] T020 Проверить `git diff --check`

## Dependencies

- T001-T003 блокируют все gates.
- T004-T006 можно выполнять параллельно после setup.
- US1 зависит от foundational gates.
- US2 зависит от representative scenario и текущего finding selection.
- US3 зависит от JSON helper assertions.
- Docs/verification после реализации gates.

## Parallel Opportunities

- T004/T005/T006 можно выполнять параллельно.
- T010/T012 можно выполнять параллельно с T013-T015 после setup.
- T016 можно выполнить параллельно с финальными проверками.
