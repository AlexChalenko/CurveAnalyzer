# Tasks: OFZ Analytics Summary

**Input**: Design documents from `specs/005-ofz-analytics-summary/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/market-summary.schema.json`, `quickstart.md`
**Tests**: Включены, потому что новая доменная логика должна быть проверяемой по конституции и acceptance criteria.
**Organization**: Задачи сгруппированы по user stories, чтобы каждый срез можно было реализовать и проверить независимо.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Можно выполнять параллельно, если файлы не пересекаются.
- **[Story]**: `US1`, `US2`, `US3`, `US4` соответствуют user stories из `spec.md`.
- Все задачи указывают конкретные пути файлов.

## Phase 1: Setup

**Purpose**: Подготовить файлы feature без изменения поведения.

- [X] T001 [P] Создать файл доменной модели summary в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T002 [P] Создать файл builder skeleton в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T003 [P] Создать файл unit-тестов builder в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

---

## Phase 2: Foundational

**Purpose**: Общая модель и правила, без которых нельзя надежно реализовать user stories.

- [X] T004 [P] Реализовать enums `OfzSummaryFindingKind`, `OfzSummaryScope`, `OfzDataLimitationKind`, `OfzIssueFocusReason`, `OfzSummarySignalScope` в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T005 [P] Реализовать модели `OfzMarketSummary`, `OfzSummaryFinding`, `OfzFindingEvidence`, `OfzSegmentSummary`, `OfzIssueFocus`, `OfzDataLimitation`, `OfzSummarySourceCounts` в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T006 Реализовать input/options contract `OfzMarketSummaryInput` и `OfzMarketSummaryOptions` в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T007 Добавить shared test helpers для trades, metrics, liquidity metrics и issues в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

**Checkpoint**: Core model компилируется, но UI еще не использует summary.

---

## Phase 3: User Story 1 - Быстро понять картину рынка (Priority: P1)

**Goal**: Пользователь видит 3-7 главных наблюдений по выбранному периоду без чтения таблиц.

**Independent Test**: Загрузить период с activity/liquidity данными и увидеть ранжированную сводку с числовым evidence.

### Tests for User Story 1

- [X] T008 [P] [US1] Добавить тест `BuildMarketSummary_ReturnsRankedConciseFindingsWithEvidence` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T009 [P] [US1] Добавить тесты `BuildMarketSummary_AddsDataLimitationWhenLiquidityMissing` и `BuildMarketSummary_ReturnsInsufficientDataState` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T010 [P] [US1] Добавить тест `BuildMarketSummary_AvoidsRecommendationLanguage` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 1

- [X] T011 [US1] Реализовать market-wide active date finding в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T012 [US1] Реализовать repeated issue, yield move, weak liquidity и data-quality finding generation в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T013 [US1] Реализовать ranking, max findings limit, empty/insufficient state и missing-vs-zero limitations в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T014 [US1] Добавить свойства `MarketSummary`, `SummaryFindings`, `HasMarketSummary`, `SelectedSummaryFinding` в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T015 [US1] Вызывать `OfzMarketSummaryBuilder` после текущей фильтрации activity/liquidity данных в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T016 [US1] Заменить или дополнить текущий блок `Выводы` отображением `SummaryFindings` в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`

**Checkpoint**: US1 работает независимо: summary появляется после `Загрузить`, содержит evidence и не показывает stale-выводы.

---

## Phase 4: User Story 2 - Разобрать причину сигнала (Priority: P2)

**Goal**: Пользователь выбирает вывод и видит, какие данные сформировали сигнал.

**Independent Test**: Выбрать любой вывод и увидеть дату, выпуск/сегмент, оборот, сделки, score, yield move, spread/liquidity status и data limitations.

### Tests for User Story 2

- [X] T017 [P] [US2] Добавить тест `BuildMarketSummary_AttachesIssueAndDateDrillDown` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T018 [P] [US2] Добавить тест `BuildMarketSummary_MarksSnapshotAndProvisionalEvidence` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 2

- [X] T019 [US2] Добавить `OfzSummaryDrillDown` и связать его с `OfzSummaryFinding` в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T020 [US2] Заполнять drill-down target для issue/date/segment/liquidity findings в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T021 [US2] Реализовать обработку `SelectedSummaryFinding` и загрузку детализации выпуска или выбор heatmap context в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T022 [US2] Добавить UI для раскрытия evidence и limitations выбранного вывода в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`

**Checkpoint**: US2 работает независимо поверх US1: любой вывод проверяем через evidence и drill-down.

---

## Phase 5: User Story 3 - Сравнить сегменты и выпуски (Priority: P3)

**Goal**: Пользователь видит агрегированную картину по ОФЗ-ПД, ОФЗ-ПК, ОФЗ-ИН, ОФЗ-АД и валютным выпускам.

**Independent Test**: Переключить тип ОФЗ и убедиться, что segment summary и findings считаются только по текущему фильтру.

### Tests for User Story 3

- [X] T023 [P] [US3] Добавить тест `BuildMarketSummary_BuildsSegmentSummaries` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T024 [P] [US3] Добавить тест `BuildMarketSummary_HonorsCouponTypeFilterAndMarksCurrency` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 3

- [X] T025 [US3] Реализовать segment aggregation by `OfzCouponType` в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T026 [US3] Реализовать выбор `TopIssues` и segment limitations в `src/CurveAnalyzer.Core/Services/OfzMarketSummaryBuilder.cs`
- [X] T027 [US3] Добавить свойства `SegmentSummaries` и `HasSegmentSummaries` в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T028 [US3] Отобразить segment summary рядом с выводами или в detail area в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`

**Checkpoint**: US3 работает независимо: сегментные итоги обновляются вместе с фильтром типа.

---

## Phase 6: User Story 4 - Получить машиночитаемую сводку (Priority: P4)

**Goal**: Тот же набор выводов доступен как structured summary contract.

**Independent Test**: Построить summary и проверить, что structured output содержит те же findings, evidence, priority, type и limitations, что UI.

### Tests for User Story 4

- [X] T029 [P] [US4] Добавить тест `MarketSummary_SerializesStableContractFields` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [X] T030 [P] [US4] Добавить тест `MarketSummary_StructuredOutputMatchesDisplayedFindings` в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`

### Implementation for User Story 4

- [X] T031 [US4] Добавить `SchemaVersion` и JSON-friendly field defaults в `src/CurveAnalyzer.Core/Domain/OfzMarketSummary.cs`
- [X] T032 [US4] Добавить `StructuredSummaryJson` и `CopySummaryJsonCommand` в `src/CurveAnalyzer.Presentation.WPF/ViewModels/OfzActivityViewModel.cs`
- [X] T033 [US4] Добавить кнопку копирования structured summary JSON в блок summary в `src/CurveAnalyzer.Presentation.WPF/Views/OfzActivityControl.xaml`
- [X] T034 [US4] Сверить обязательные поля модели с `specs/005-ofz-analytics-summary/contracts/market-summary.schema.json`

**Checkpoint**: US4 работает независимо: structured summary можно получить без MCP server.

---

## Phase 7: Polish & Cross-Cutting

**Purpose**: Проверка, cleanup и документация после реализации выбранных stories.

- [X] T035 [P] Обновить `specs/005-ofz-analytics-summary/quickstart.md` по фактическому UI, если реализация изменила ручной сценарий
- [X] T036 Запустить `dotnet test -c Release` и зафиксировать результат в итоговом отчете
- [X] T037 Запустить `dotnet build CurveAnalyzer.sln -c Release` и зафиксировать результат в итоговом отчете
- [X] T038 [P] Добавить lightweight performance smoke test для 90 торговых дней и 100 выпусков в `tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs`
- [ ] T039 Проверить вручную вкладку `Активность ОФЗ`: summary, filters, heatmap, weak liquidity, detail charts, structured JSON
- [X] T040 Убедиться, что рабочее дерево содержит только intended feature files перед commit в `C:\Users\Alexey\source\repos\CurveAnalyzer`
- [X] T041 Разделить detail area вкладки `Активность ОФЗ` на отдельные WPF controls для режимов `Сегменты` и `Выпуск`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: нет зависимостей.
- **Foundational (Phase 2)**: зависит от Setup и блокирует все user stories.
- **US1 (Phase 3)**: MVP; зависит от Foundational.
- **US2 (Phase 4)**: зависит от US1, потому что drill-down выбирает existing summary finding.
- **US3 (Phase 5)**: зависит от Foundational; UI-интеграция проще после US1, но Core segment aggregation можно делать параллельно.
- **US4 (Phase 6)**: зависит от Core model из Foundational и findings из US1.
- **Polish (Phase 7)**: после выбранных user stories.

### User Story Dependencies

- **US1**: обязательный MVP.
- **US2**: расширяет US1 evidence/drill-down.
- **US3**: может идти параллельно с US2 после Foundation, если не редактировать одни и те же ViewModel/XAML участки одновременно.
- **US4**: лучше после US1, чтобы serializable contract соответствовал реальному summary.

### Parallel Opportunities

- T001-T003 можно выполнять параллельно.
- T004-T005 можно выполнять параллельно с T007; T006 зависит от T004-T005.
- Тесты внутри каждого user story (`[P]`) можно писать параллельно.
- Core segment tasks T023-T026 можно делать параллельно с US2 UI tasks T021-T022 после T006.
- Quickstart update T035 можно делать параллельно с финальной ручной проверкой после реализации UI.

## Parallel Example: User Story 1

```text
Task A: T008 [US1] Добавить ranked findings test в tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs
Task B: T009 [US1] Добавить missing liquidity limitation test в tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs
Task C: T010 [US1] Добавить recommendation language guard test в tests/CurveAnalyzer.Core.Tests/Services/OfzMarketSummaryBuilderTests.cs
```

После тестов один исполнитель реализует T011-T013 в `OfzMarketSummaryBuilder.cs`,
а другой готовит T014-T016 в WPF, синхронизируясь по public model names.

## Implementation Strategy

### MVP First

1. Выполнить Phase 1 и Phase 2.
2. Выполнить US1 полностью.
3. Запустить `dotnet test -c Release`.
4. Проверить UI summary на одном диапазоне и одном типе ОФЗ.
5. Остановиться для ручной проверки перед US2/US3/US4, если нужно снизить риск.

### Incremental Delivery

1. US1: summary findings with evidence.
2. US2: drill-down и раскрытие evidence.
3. US3: segment comparison.
4. US4: structured summary JSON.
5. Polish: тесты, сборка, ручной сценарий.

### Notes

- Не добавлять новые EF migrations для v1.
- Не добавлять MCP server в этой feature.
- Не переносить ranking/evidence логику в XAML converters.
- Не использовать recommendation language в summary text.
- Missing data должны оставаться nullable и попадать в `DataLimitation`.
