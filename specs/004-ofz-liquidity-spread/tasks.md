# Tasks: Liquidity & Spread Layer

**Input**: Design documents from `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts\`, `quickstart.md`
**Feature Branch**: `004-ofz-liquidity-spread`

**Tests**: План реализации требует targeted Core tests для liquidity score/signal rules. Тестовые задачи включены и должны идти до соответствующей реализации.

**Organization**: Задачи сгруппированы по user story, чтобы каждую историю можно было реализовать и проверить отдельно после общей foundational-фазы.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: задача может выполняться параллельно после завершения ее зависимостей, потому что использует другой файл или независимый слой.
- **[Story]**: метка user story используется только в story-фазах.
- Все задачи содержат конкретные файлы или директории, которые нужно изменить или проверить.

---

## Phase 1: Setup

**Purpose**: Подтвердить базовое состояние проекта и источников данных перед изменениями.

- [X] T001 Run baseline validation commands and append result to `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`
- [X] T002 Inspect existing OFZ activity flow and record implementation anchors in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`
- [X] T003 [P] Verify current MOEX ISS liquidity columns against `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\contracts\iss-liquidity-data-contract.md`

---

## Phase 2: Foundational

**Purpose**: Общие модели, контракты, ISS mapping и persistence, без которых нельзя надежно реализовать отдельные user stories.

**Critical**: User-story работу не начинать, пока эта фаза не завершена.

- [X] T004 Add failing Core tests for missing-vs-zero spread and calculated spread semantics in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`
- [X] T005 Add failing Core tests for liquidity bucket/ranking rules in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`
- [X] T006 Add failing Core tests for provisional current-day snapshot separation from final history in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`
- [X] T007 Extend daily activity domain row with nullable liquidity/spread fields in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Core\Domain\OfzDailyTrade.cs`
- [X] T008 [P] Add liquidity domain view models and enums in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Core\Domain\OfzLiquidityViews.cs`
- [X] T009 Extend existing activity/scatter view contracts for optional liquidity fields in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Core\Domain\OfzActivityViews.cs`
- [X] T010 Extend OFZ activity repository contracts for liquidity history and snapshots in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Application\Interfaces\IOfzActivityRepository.cs`
- [X] T011 Extend OFZ activity load result with liquidity/snapshot status in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Application\OfzActivityLoadResult.cs`
- [X] T012 [P] Extend ISS history DTOs with nullable liquidity/spread columns in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.ApiServices\Data\OfzHistoryIssData.cs`
- [X] T013 Map MOEX ISS history liquidity columns and current snapshot blocks in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.ApiServices\OfzActivityOnlineDataService.cs`
- [X] T014 Update EF Core model mappings for daily liquidity fields and snapshot rows in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Infrastructure\MoexContext.cs`
- [X] T015 Create EF Core migration for OFZ liquidity/spread storage in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Infrastructure\Migrations\`
- [X] T016 Update repository save/load idempotence for nullable liquidity fields and replaceable snapshots in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Infrastructure\Repositories\OfzActivityRepository.cs`
- [X] T017 Run foundational build/test checkpoint and record result in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`

**Checkpoint**: Core/domain/application contracts, ISS mapping and storage are ready. User-story implementation can start.

---

## Phase 3: User Story 1 - Увидеть качество ликвидности выпуска (Priority: P1)

**Goal**: При выборе выпуска пользователь видит spread, bid/offer context, depth where available, сделки/оборот и понятную оценку ликвидности.

**Independent Test**: Выбрать выпуск с bid/offer data на экране "Активность ОФЗ" и убедиться, что детализация выпуска показывает liquidity summary; выбрать выпуск без bid/offer и увидеть missing-data state, а не нулевой spread.

### Tests for User Story 1

- [X] T018 [US1] Add failing tests for issue liquidity profile construction in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`
- [X] T019 [US1] Add failing tests for issue detail missing-data UI values in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`

### Implementation for User Story 1

- [X] T020 [US1] Implement issue liquidity profile calculation in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Core\Services\OfzActivityAnalyzer.cs`
- [X] T021 [US1] Expose issue liquidity profile through application service in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Application\OfzActivityService.cs`
- [X] T022 [US1] Add selected issue liquidity state to the WPF view model in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\ViewModels\OfzActivityViewModel.cs`
- [X] T023 [P] [US1] Add formatting converters for liquidity summary values in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\Converters\OfzActivityOverviewConverters.cs`
- [X] T024 [US1] Add liquidity summary and snapshot/provisional labels to issue detail UI in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\Views\OfzActivityControl.xaml`
- [X] T025 [US1] Extend issue detail chart series for historical spread where available in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\Converters\OfzIssueDetailSeriesConverter.cs`
- [X] T026 [US1] Run targeted US1 tests and update validation log in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`

**Checkpoint**: User Story 1 is independently usable as the MVP.

---

## Phase 4: User Story 2 - Найти выпуски с проблемной ликвидностью (Priority: P2)

**Goal**: Пользователь видит ранжированный список weak/problem liquidity выпусков для выбранного диапазона, типа ОФЗ и scope сигналов.

**Independent Test**: Загрузить диапазон с несколькими выпусками и проверить, что weak-liquidity list сортируется по severity/score/turnover, а выпуски без котировок получают отдельную причину.

### Tests for User Story 2

- [X] T027 [US2] Add failing tests for weak liquidity list ordering in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`
- [X] T028 [US2] Add failing tests for missing quotes on active day ranking in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`

### Implementation for User Story 2

- [X] T029 [US2] Implement weak liquidity ranking in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Core\Services\OfzActivityAnalyzer.cs`
- [X] T030 [US2] Expose weak liquidity list through application service in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Application\OfzActivityService.cs`
- [X] T031 [US2] Add weak liquidity collection and filter refresh logic to WPF view model in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\ViewModels\OfzActivityViewModel.cs`
- [X] T032 [US2] Add weak liquidity table or panel to activity UI in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\Views\OfzActivityControl.xaml`
- [X] T033 [US2] Run targeted US2 tests and update validation log in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`

**Checkpoint**: User Story 2 works independently after foundational data exists.

---

## Phase 5: User Story 3 - Сравнить доходность, дюрацию и spread (Priority: P3)

**Goal**: Scatter duration/yield view keeps its existing meaning and adds visible liquidity/spread quality.

**Independent Test**: Выбрать тип ОФЗ и убедиться, что scatter points still show duration/yield while color/outline or tooltip communicates liquidity/spread status.

### Tests for User Story 3

- [X] T034 [P] [US3] Add failing tests for liquidity-enhanced scatter point mapping in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`

### Implementation for User Story 3

- [X] T035 [US3] Add liquidity bucket fields to scatter point construction in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Core\Services\OfzActivityAnalyzer.cs`
- [X] T036 [US3] Pass liquidity-enhanced scatter data through application service in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Application\OfzActivityService.cs`
- [X] T037 [US3] Bind liquidity scatter data in WPF view model in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\ViewModels\OfzActivityViewModel.cs`
- [X] T038 [US3] Update segment scatter chart colors/tooltips in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\Views\OfzActivityControl.xaml`
- [X] T039 [P] [US3] Update scatter chart converters for spread/status tooltip formatting in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\Converters\OfzIssueDetailSeriesConverter.cs`
- [X] T040 [US3] Run targeted US3 tests and update validation log in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`

**Checkpoint**: User Story 3 can be validated without implementing User Story 4.

---

## Phase 6: User Story 4 - Получить объяснимые выводы по spread-аномалиям (Priority: P4)

**Goal**: Блок выводов показывает deterministic spread/liquidity signals with issue, date/scope, numeric evidence and reason, without recommendation language.

**Independent Test**: Загрузить диапазон с wide spread, missing quotes and improving spread cases and verify insights name concrete issues/dates/reasons and avoid buy/sell language.

### Tests for User Story 4

- [X] T041 [US4] Add failing tests for spread signal generation and evidence fields in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`
- [X] T042 [US4] Add failing tests that insight text avoids recommendation language in `C:\Users\Alexey\source\repos\CurveAnalyzer\tests\CurveAnalyzer.Core.Tests\Services\OfzActivityAnalyzerTests.cs`

### Implementation for User Story 4

- [X] T043 [US4] Implement spread/liquidity signal generation in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Core\Services\OfzActivityAnalyzer.cs`
- [X] T044 [US4] Expose liquidity insights through application service in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Application\OfzActivityService.cs`
- [X] T045 [US4] Add liquidity insight state and signal-scope refresh to WPF view model in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\ViewModels\OfzActivityViewModel.cs`
- [X] T046 [US4] Render liquidity/spread insights in existing conclusions area in `C:\Users\Alexey\source\repos\CurveAnalyzer\src\CurveAnalyzer.Presentation.WPF\Views\OfzActivityControl.xaml`
- [X] T047 [US4] Run targeted US4 tests and update validation log in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`

**Checkpoint**: All user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Финальная проверка, документация и регрессионная защита existing workflows.

- [X] T048 [P] Update implementation notes and accepted limitations in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`
- [X] T049 [P] Update ISS/UI contracts if implementation discovers narrower field availability in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\contracts\`
- [X] T050 Verify range performance for up to 90 trading days and record result in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`
- [X] T051 Run full validation `dotnet build CurveAnalyzer.sln -c Release` and `dotnet test -c Release`, then record result in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`
- [X] T052 Run manual smoke scenarios for existing activity workflows and liquidity workflows, then record result in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`
- [X] T053 Verify selected issue liquidity detail is understandable within 10 seconds and record result in `C:\Users\Alexey\source\repos\CurveAnalyzer\specs\004-ofz-liquidity-spread\quickstart.md`
- [X] T054 Review final diff for unrelated changes and stage only intended feature files in `C:\Users\Alexey\source\repos\CurveAnalyzer\`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup and blocks all user stories.
- **US1 (Phase 3)**: depends on Foundational and is the MVP.
- **US2 (Phase 4)**: depends on Foundational; can be done after or parallel with US1 if shared files are coordinated.
- **US3 (Phase 5)**: depends on Foundational; can be done after or parallel with US1/US2 if shared files are coordinated.
- **US4 (Phase 6)**: depends on Foundational; benefits from US2 metrics but should remain independently testable.
- **Polish (Phase 7)**: depends on all selected stories for the release.

### User Story Dependencies

- **US1**: no dependency on other stories after Foundational.
- **US2**: no functional dependency on US1 UI, but reuses foundational liquidity metrics.
- **US3**: no dependency on US1/US2 UI, but reuses foundational scatter and liquidity fields.
- **US4**: reuses foundational metrics and may reuse weak-liquidity ranking concepts from US2; keep signal rules in Core so they are testable independently.

### Within Each User Story

- Tests before implementation.
- Core calculations before Application service exposure.
- Application service before WPF view model.
- View model before XAML binding.
- Story-specific validation before the next story checkpoint.

---

## Parallel Opportunities

- T003 can run after T001 starts because it only checks the ISS contract.
- T008 and T012 can run in parallel after test intent is clear.
- US1/US2/US4 test tasks share `OfzActivityAnalyzerTests.cs`; run them sequentially unless they are split into separate test files during implementation.
- Documentation polish T048 and contract updates T049 can run in parallel after implementation behavior is known.

### Parallel Example: US1

```text
Task: T023 [US1] formatting converters after domain output shape is stable
```

### Parallel Example: US2

```text
Task: T030 [US2] application service exposure after T029
Task: T032 [US2] UI panel after T031 state is available
```

### Parallel Example: US4

```text
Task: T044 [US4] application service exposure after T043
Task: T046 [US4] UI rendering after T045 state is available
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1.
2. Complete Phase 2.
3. Complete Phase 3 (US1).
4. Stop and validate selected issue liquidity detail before adding broader lists or scatter changes.

### Incremental Delivery

1. US1 adds issue-level liquidity context.
2. US2 adds portfolio/range-level weak liquidity discovery.
3. US3 adds cross-issue visual comparison.
4. US4 adds deterministic explanations.
5. Polish validates performance, existing workflow compatibility and docs.

### Risk Controls

- Keep missing liquidity values nullable through all layers.
- Keep snapshot-only depth/current-yield data separate from daily history rows.
- Do not change existing activity score semantics while adding liquidity score.
- Do not introduce buy/sell recommendation text.
- Preserve existing OFZ activity tabs and filters while extending their data.
