# Задачи: удаление legacy-проекта и стабилизация supported app

**Входные артефакты**: документы из `specs/001-remove-legacy-project/`
**Предусловия**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Тесты**: тесты обязательны для не-UI доменных расчетов, затронутых миграцией.

**Организация**: задачи сгруппированы по user story, чтобы каждый инкремент можно было реализовать и проверить отдельно после общей основы.

## Формат: `[ID] [P?] [Story] Description`

- **[P]**: задачу можно выполнять параллельно с другими `[P]` задачами в том же этапе.
- **[Story]**: связывает задачу с `US1`, `US2`, `US3` или `US4`.
- Каждая задача называет основной файл или файлы, которые меняет.

---

## Этап 1: подготовка общей технической базы

**Назначение**: закрепить .NET 10 и NuGet policy до архитектурных и legacy-изменений.

- [x] T001 Записать исходное состояние `dotnet --info`, поддерживаемой сборки и текущих NuGet warnings в `specs/001-remove-legacy-project/implementation-report.md`
- [x] T002 Создать .NET 10 SDK policy в `global.json`
- [x] T003 Создать NuGet CPM policy с `ManagePackageVersionsCentrally=true` и версиями пакетов в `Directory.Packages.props`
- [x] T004 [P] Перевести `src/CurveAnalyzer.Core/CurveAnalyzer.Core.csproj` на `net10.0`
- [x] T005 [P] Перевести `src/CurveAnalyzer.Application/CurveAnalyzer.Application.csproj` на `net10.0` и убрать `Version` из `PackageReference`
- [x] T006 [P] Перевести `src/CurveAnalyzer.ApiServices/CurveAnalyzer.ApiServices.csproj` на `net10.0` и убрать `Version` из `PackageReference`
- [x] T007 [P] Перевести `src/CurveAnalyzer.Infrastructure/CurveAnalyzer.Infrastructure.csproj` на `net10.0` и убрать `Version` из `PackageReference`
- [x] T008 Перевести `src/CurveAnalyzer.Presentation.WPF/CurveAnalyzer.Presentation.WPF.csproj` на `net10.0-windows`, убрать `Version` из `PackageReference` и удалить local `LiveChartsCore.SkiaSharpView.WPF` `HintPath`
- [x] T009 Проверить `dotnet restore` для supported projects и зафиксировать результат в `specs/001-remove-legacy-project/implementation-report.md`

---

## Этап 2: фундаментальные prerequisites

**Назначение**: подготовить тестовую и solution-основу, без которой нельзя безопасно удалять legacy и переносить расчеты.

**Критично**: работа по user stories начинается только после завершения этого этапа.

- [x] T010 Создать test project `tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj`
- [x] T011 Добавить `tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj` в `CurveAnalyzer.sln`
- [x] T012 [P] Добавить generated database patterns `zcyc.db`, `zcyc.db-shm`, `zcyc.db-wal` в `.gitignore`
- [x] T013 [P] Создать migration evidence file `specs/001-remove-legacy-project/legacy-inventory.md`
- [x] T014 Проверить сборку supported `src/CurveAnalyzer.Presentation.WPF/CurveAnalyzer.Presentation.WPF.sln` после .NET 10/CPM и записать результат в `specs/001-remove-legacy-project/implementation-report.md`

**Контрольная точка**: .NET 10/CPM baseline готов, тестовый проект существует, можно реализовывать user stories.

---

## Этап 3: User Story 1 - один поддерживаемый путь приложения (Priority: P1) - MVP

**Цель**: root `CurveAnalyzer.sln` становится единственной supported solution, legacy-код не участвует в supported build path.

**Независимая проверка**: `dotnet build CurveAnalyzer.sln -c Release` собирает supported projects без старого root-level WPF project и внешнего `..\MoexData`.

### Реализация US1

- [x] T015 [US1] Сопоставить legacy workflows из `Charts/`, `Data/`, `DataProviders/`, `View/`, `ViewModel/` с supported workflows в `specs/001-remove-legacy-project/legacy-inventory.md`
- [x] T016 [US1] Закрыть legacy-only behavior gate: для каждого unmatched workflow перенести поведение, явно объявить out of scope или зафиксировать documented exception в `specs/001-remove-legacy-project/legacy-inventory.md`
- [x] T017 [US1] Обновить `CurveAnalyzer.sln`: удалить `CurveAnalyzer.csproj` и `..\MoexData\MoexData.csproj`, оставить supported `src` projects и `tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj`
- [x] T018 [US1] Создать contributor entrypoint с supported app path и validation commands в `README.md`
- [x] T019 [US1] Удалить nested solution `src/CurveAnalyzer.Presentation.WPF/CurveAnalyzer.Presentation.WPF.sln` после успешной проверки root solution
- [x] T020 [US1] Удалить legacy root project files `CurveAnalyzer.csproj`, `App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `AssemblyInfo.cs`, `App.config`
- [x] T021 [US1] Удалить legacy root source folders `Charts/`, `Data/`, `DataProviders/`, `Interfaces/`, `Theme/`, `Tools/`, `View/`, `ViewModel/`, `Visual Studio 2022/`
- [x] T022 [US1] Проверить `dotnet build CurveAnalyzer.sln -c Release` и записать результат в `specs/001-remove-legacy-project/implementation-report.md`

**Контрольная точка**: root solution является единственным supported build path.

---

## Этап 4: User Story 2 - понятные архитектурные границы (Priority: P2)

**Цель**: UI, application use cases, domain, persistence и MOEX access разделены явными contracts и DI registrations.

**Независимая проверка**: reviewer видит, что Application не зависит от WPF, chart libraries, EF Core, XML transport details или `CommunityToolkit.Mvvm.Messaging`.

### Реализация US2

- [x] T023 [P] [US2] Нормализовать online-data contract в `src/CurveAnalyzer.Application/Interfaces/IDataService.cs`
- [x] T024 [P] [US2] Нормализовать history contract в `src/CurveAnalyzer.Application/Interfaces/IHistoryDataService.cs`
- [x] T025 [P] [US2] Нормализовать persistence contract в `src/CurveAnalyzer.Application/Interfaces/IZcycRepository.cs`
- [x] T026 [US2] Убрать dependency on legacy namespace `CurveAnalyzer.Interfaces` из `src/CurveAnalyzer.Application/DataSyncService.cs` и `src/CurveAnalyzer.Application/HistoryDataService.cs`
- [x] T027 [US2] Создать UI-neutral progress contract в `src/CurveAnalyzer.Application/SyncProgress.cs`
- [x] T028 [US2] Убрать `IMessenger` и message sending из `src/CurveAnalyzer.Application/DataSyncService.cs`, оставив progress через `IProgress<SyncProgress>` или application result
- [x] T029 [US2] Убрать `.Result` из async flow в `src/CurveAnalyzer.Application/DataSyncService.cs`, заменив чтение task results на awaited values
- [x] T030 [P] [US2] Создать application registration extension в `src/CurveAnalyzer.Application/DependencyInjection.cs`
- [x] T031 [P] [US2] Создать MOEX API registration extension в `src/CurveAnalyzer.ApiServices/DependencyInjection.cs`
- [x] T032 [P] [US2] Создать infrastructure registration extension в `src/CurveAnalyzer.Infrastructure/DependencyInjection.cs`
- [x] T033 [P] [US2] Создать presentation registration extension в `src/CurveAnalyzer.Presentation.WPF/DependencyInjection.cs`
- [x] T034 [US2] Упростить composition root до layer registrations в `src/CurveAnalyzer.Presentation.WPF/App.xaml.cs`
- [x] T035 [US2] Убрать `Database.EnsureCreated()` side effect из `src/CurveAnalyzer.Infrastructure/MoexContext.cs`
- [x] T036 [US2] Добавить явную database initialization/migration точку в `src/CurveAnalyzer.Infrastructure/DatabaseInitializer.cs`
- [x] T037 [US2] Обновить EF queries в `src/CurveAnalyzer.Infrastructure/Repositories/ZcycRepository.cs`: использовать async EF methods, `AsNoTracking`, `CancellationToken` где применимо и не возвращать lazy enumerable поверх `DbContext`
- [x] T038 [US2] Проверить source-level boundaries: в `src/CurveAnalyzer.Application/**/*.cs` и `src/CurveAnalyzer.Application/CurveAnalyzer.Application.csproj` не должно быть зависимостей от WPF, EF Core, chart packages, Presentation-specific packages, `CommunityToolkit.Mvvm.Messaging` или legacy namespace `CurveAnalyzer.Interfaces`

**Контрольная точка**: layer boundaries соответствуют `contracts/application-boundaries.md`.

---

## Этап 5: User Story 3 - легкий DDD там, где он помогает (Priority: P3)

**Цель**: ключевые yield-curve понятия и расчеты имеют явные domain types/services и automated tests без heavy DDD ceremony.

**Независимая проверка**: `dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release` проверяет validation, ordering и spread formula для не-UI расчетов.

### Тесты US3

- [x] T039 [P] [US3] Добавить failing tests для `CurvePeriod` validation в `tests/CurveAnalyzer.Core.Tests/Domain/CurvePeriodTests.cs`
- [x] T040 [P] [US3] Добавить failing tests для `YieldCurve` ordering и duplicate period behavior в `tests/CurveAnalyzer.Core.Tests/Domain/YieldCurveTests.cs`
- [x] T041 [P] [US3] Добавить failing tests для spread alignment и formula `second.Value - first.Value` в `tests/CurveAnalyzer.Core.Tests/Services/SpreadCalculatorTests.cs`
- [x] T042 [P] [US3] Добавить failing tests для rate-history ordering/grouping в `tests/CurveAnalyzer.Core.Tests/Services/RateSeriesGrouperTests.cs`

### Реализация US3

- [x] T043 [P] [US3] Реализовать value objects `TradingDate`, `CurvePeriod`, `YieldPoint` в `src/CurveAnalyzer.Core/Domain/YieldCurvePrimitives.cs`
- [x] T044 [P] [US3] Реализовать domain records `YieldCurve`, `HistoricalSeries`, `SpreadSeries` в `src/CurveAnalyzer.Core/Domain/YieldCurveSeries.cs`
- [x] T045 [US3] Реализовать spread calculation в `src/CurveAnalyzer.Core/Services/SpreadCalculator.cs`
- [x] T046 [US3] Реализовать rate-series grouping в `src/CurveAnalyzer.Core/Services/RateSeriesGrouper.cs`
- [x] T047 [US3] Подключить domain calculation services в `src/CurveAnalyzer.Application/HistoryDataService.cs`
- [x] T048 [US3] Обновить `specs/001-remove-legacy-project/data-model.md`, если implementation names отличаются от плановых names
- [x] T049 [US3] Запустить `dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release` и записать результат в `specs/001-remove-legacy-project/implementation-report.md`

**Контрольная точка**: domain behavior защищен focused tests, DDD остается lightweight.

---

## Этап 6: User Story 4 - гигиена репозитория и повторяемая проверка (Priority: P4)

**Цель**: supported app не зависит от local binary paths, checked-in runtime database files или недокументированных ручных шагов.

**Независимая проверка**: documented quickstart commands выполняются из root solution; package checks не показывают untracked upgrade/security/deprecation issues, кроме documented chart exception.

### Реализация US4

- [x] T050 [US4] Удалить checked-in runtime database files `zcyc.db`, `zcyc.db-shm`, `zcyc.db-wal`, `src/CurveAnalyzer.Presentation.WPF/zcyc.db`
- [x] T051 [US4] Создать runtime configuration file `src/CurveAnalyzer.Presentation.WPF/appsettings.json` с documented SQLite connection string
- [x] T052 [US4] Подключить `src/CurveAnalyzer.Presentation.WPF/appsettings.json` как copied content в `src/CurveAnalyzer.Presentation.WPF/CurveAnalyzer.Presentation.WPF.csproj`
- [x] T053 [US4] Перенести SQLite connection string usage из `src/CurveAnalyzer.Presentation.WPF/App.xaml.cs` в `src/CurveAnalyzer.Infrastructure/DependencyInjection.cs`
- [x] T054 [US4] Проверить отсутствие local binary paths и legacy references в `src/CurveAnalyzer.Presentation.WPF/CurveAnalyzer.Presentation.WPF.csproj`
- [x] T055 [US4] Зафиксировать решение по `LiveCharts.Wpf` compatibility exception или chart migration в `specs/001-remove-legacy-project/implementation-report.md`
- [x] T056 [US4] Запустить `dotnet list CurveAnalyzer.sln package --outdated`, `dotnet list CurveAnalyzer.sln package --vulnerable`, `dotnet list CurveAnalyzer.sln package --deprecated` и записать результат в `specs/001-remove-legacy-project/implementation-report.md`
- [x] T057 [US4] Выполнить manual smoke checklist из `specs/001-remove-legacy-project/quickstart.md` и записать результат в `specs/001-remove-legacy-project/implementation-report.md`

**Контрольная точка**: supported project воспроизводим на другой машине с .NET 10 SDK.

---

## Финальный этап: полировка и сквозные проверки

**Назначение**: финальная согласованность документов, проверки и подготовка к commit/PR.

- [x] T058 [P] Обновить agent/contributor instructions в `AGENTS.md`, если validation commands или supported app path изменились во время реализации
- [x] T059 [P] Обновить `specs/001-remove-legacy-project/quickstart.md` по фактическим командам и documented exceptions
- [x] T060 Проверить отсутствие старых TFM/package versions через search и записать результат в `specs/001-remove-legacy-project/implementation-report.md`
- [x] T061 Запустить финальные `dotnet restore`, `dotnet build CurveAnalyzer.sln -c Release`, `dotnet test -c Release` и записать результат в `specs/001-remove-legacy-project/implementation-report.md`
- [x] T062 Запустить Spec Kit consistency analysis для `specs/001-remove-legacy-project/tasks.md` и записать findings или `no findings` в `specs/001-remove-legacy-project/implementation-report.md`
- [x] T063 Подготовить staged diff только по planned files и проверить его перед commit в `specs/001-remove-legacy-project/implementation-report.md`

---

## Зависимости и порядок выполнения

### Зависимости этапов

- **Этап 1**: без зависимостей.
- **Этап 2**: зависит от Этапа 1.
- **US1**: зависит от Этапа 2; broad legacy folders удаляются только после закрытия `legacy-inventory.md` gate.
- **US2**: зависит от Этапа 2; может стартовать после cleanup build path из US1 или параллельно, если файлы не пересекаются.
- **US3**: зависит от Этапа 2; безопаснее выполнять после стабилизации US2 contracts.
- **US4**: зависит от US1 и US2, потому что удаляет runtime artifacts и завершает repeatability.
- **Финальный этап**: зависит от всех выбранных user stories.

### Зависимости user stories

- **US1 (P1)**: MVP. Формирует один supported app path.
- **US2 (P2)**: Опирается на supported `src` path и убирает boundary leaks.
- **US3 (P3)**: Использует стабилизированные Application/Core boundaries для domain modeling и tests.
- **US4 (P4)**: Завершает repeatable build и package/data hygiene после стабилизации app path.

### Возможности параллельного выполнения

- T004-T007 можно выполнять параллельно после T003.
- T010, T012, T013 можно выполнять параллельно после Этапа 1.
- T023-T025 можно выполнять параллельно.
- T030-T033 можно выполнять параллельно после стабилизации application contracts.
- T039-T042 можно выполнять параллельно после создания test project.
- T043 и T044 можно выполнять параллельно после появления failing domain tests.
- T058 и T059 можно выполнять параллельно на финальном этапе.

---

## Пример параллельного выполнения: US2

```text
Задача: "T023 [P] [US2] Нормализовать online-data contract в src/CurveAnalyzer.Application/Interfaces/IDataService.cs"
Задача: "T024 [P] [US2] Нормализовать history contract в src/CurveAnalyzer.Application/Interfaces/IHistoryDataService.cs"
Задача: "T025 [P] [US2] Нормализовать persistence contract в src/CurveAnalyzer.Application/Interfaces/IZcycRepository.cs"
```

```text
Задача: "T030 [P] [US2] Создать src/CurveAnalyzer.Application/DependencyInjection.cs"
Задача: "T031 [P] [US2] Создать src/CurveAnalyzer.ApiServices/DependencyInjection.cs"
Задача: "T032 [P] [US2] Создать src/CurveAnalyzer.Infrastructure/DependencyInjection.cs"
Задача: "T033 [P] [US2] Создать src/CurveAnalyzer.Presentation.WPF/DependencyInjection.cs"
```

## Пример параллельного выполнения: US3

```text
Задача: "T039 [P] [US3] Добавить CurvePeriod tests в tests/CurveAnalyzer.Core.Tests/Domain/CurvePeriodTests.cs"
Задача: "T040 [P] [US3] Добавить YieldCurve tests в tests/CurveAnalyzer.Core.Tests/Domain/YieldCurveTests.cs"
Задача: "T041 [P] [US3] Добавить SpreadCalculator tests в tests/CurveAnalyzer.Core.Tests/Services/SpreadCalculatorTests.cs"
Задача: "T042 [P] [US3] Добавить RateSeriesGrouper tests в tests/CurveAnalyzer.Core.Tests/Services/RateSeriesGrouperTests.cs"
```

---

## Стратегия реализации

### Сначала MVP

1. Завершить Этап 1 и Этап 2.
2. Завершить US1.
3. Остановиться и проверить root `CurveAnalyzer.sln` build.
4. Только после этого удалять broad legacy folders.

### Инкрементальная поставка

1. Technical baseline: .NET 10 + CPM + restore/build.
2. US1: одна supported solution и entry point.
3. US2: layer boundaries и DI composition cleanup.
4. US3: lightweight DDD и focused tests.
5. US4: repeatable repo hygiene и final validation.

### Правила безопасности

- Работать с dirty tree; не откатывать unrelated changes.
- Удалять legacy files только после того, как `legacy-inventory.md` фиксирует migrate/out-of-scope decisions.
- Считать `LiveCharts.Wpf` documented exception, если он не блокирует .NET 10 build; если блокирует, chart migration обязательна до завершения US4.
- Stage only files intentionally changed для текущей группы задач.
