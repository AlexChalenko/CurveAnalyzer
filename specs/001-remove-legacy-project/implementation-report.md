# Отчет реализации: удаление legacy-проекта и стабилизация supported app

## T001 Baseline

- `dotnet --info`: SDK `10.0.203`, Host runtime `10.0.7`, Windows Desktop Runtime `10.0.7`.
- `global.json`: на момент baseline отсутствовал.
- `dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln -c Release`: успешно, 17 warnings, 0 errors.
- Основные warnings baseline:
  - `NU1701` для `LiveCharts` / `LiveCharts.Wpf 0.9.7` на `net9.0-windows7.0`.
  - `CS8618` nullable warnings в `src/CurveAnalyzer.ApiServices/Data/IssData.cs` и `src/CurveAnalyzer.ApiServices/Data/SecurityIssData.cs`.
- `dotnet list src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln package --outdated`: доступны обновления `CommunityToolkit.Diagnostics` `8.4.0 -> 8.4.2`, `CommunityToolkit.Mvvm` `8.4.0 -> 8.4.2`, `Microsoft.EntityFrameworkCore.Sqlite` `9.0.9 -> 10.0.7`, `Microsoft.Extensions.Hosting` `9.0.9 -> 10.0.7`.

## T009 Restore после .NET 10/CPM

- `dotnet restore src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln`: успешно.
- `dotnet list src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln package --outdated`: обновлений нет для текущих sources.
- Ожидаемое временное ограничение: `dotnet restore` по root `CurveAnalyzer.sln` сейчас падает на legacy `CurveAnalyzer.csproj` с `NU1008`, потому что root `Directory.Packages.props` включает CPM, а legacy project все еще содержит project-level `Version` в `PackageReference`. Это должно исчезнуть после US1, когда legacy project будет удален из root solution.

## T014 Supported build после .NET 10/CPM

- `dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln -c Release`: успешно, 17 warnings, 0 errors.
- Warnings остались теми же категориями:
  - `NU1701` для `LiveCharts` / `LiveCharts.Wpf 0.9.7`, теперь на `net10.0-windows7.0`.
  - `CS8618` nullable warnings в MOEX XML DTO.
- `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release`: test assembly собирается, доступных тестов пока нет. Это ожидаемо до задач US3.

## T022 Root solution build после удаления legacy build path

- `CurveAnalyzer.sln`: удалены legacy `CurveAnalyzer.csproj` и внешний `..\MoexData\MoexData.csproj`.
- Удалены nested solution `src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln`, root legacy project files и legacy source folders из `legacy-inventory.md`.
- `dotnet build CurveAnalyzer.sln -c Release`: успешно, 17 warnings, 0 errors.
- Warnings:
  - `NU1701` для `LiveCharts` / `LiveCharts.Wpf 0.9.7`.
  - `CS8618` nullable warnings в MOEX XML DTO.

## T038 Source-level boundaries после US2

- Application contracts нормализованы: `IDataService`, `IHistoryDataService`, `IZcycRepository` используют async methods и `CancellationToken`.
- `DataSyncService` больше не зависит от `IMessenger`, `DownloadProgressMessage`, `DownloadCompletedMessage` или legacy namespace `CurveAnalyzer.Interfaces`.
- UI-neutral progress contract: `SyncProgress`.
- DI registration вынесен в `AddApplication`, `AddMoexApiServices`, `AddInfrastructure`, `AddPresentation`.
- `Database.EnsureCreated()` удален из конструктора `MoexContext`; явная точка инициализации: `IDatabaseInitializer` / `DatabaseInitializer`.
- Source-check по `src/**/*.cs` не нашел `CurveAnalyzer.Interfaces`, `IMessenger`, `WeakReferenceMessenger`, `.Result`, `.Wait(` или `Task.Run(` вне generated/build artifacts.
- `dotnet build CurveAnalyzer.sln -c Release`: успешно, 4 warnings, 0 errors. Остался documented `NU1701` для `LiveCharts.Wpf`.

## T049 Domain tests после US3

- Red-check перед реализацией: `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release` падал компиляцией из-за отсутствующих `CurvePeriod`, `TradingDate`, `YieldCurve`, `HistoricalSeries`, `SpreadCalculator`, `RateSeriesGrouper`.
- Реализованы lightweight domain types/services:
  - `TradingDate`, `CurvePeriod`, `YieldPoint`
  - `YieldCurve`, `HistoricalSeries`, `SpreadSeries`
  - `SpreadCalculator`, `RateSeriesGrouper`
- `HistoryDataService`, `SpreadChartViewModel` и `RateChartControl` используют domain ordering/calculation services.
- `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release`: успешно, 9 tests passed.

## T056 Package checks и US4 hygiene

- Runtime database files удалены: root `zcyc.db`, `zcyc.db-shm`, `zcyc.db-wal`, `src\CurveAnalyzer.Presentation.WPF\zcyc.db`.
- Добавлен `src\CurveAnalyzer.Presentation.WPF\appsettings.json` с `ConnectionStrings:MoexHistory`; файл копируется в output.
- `dotnet list CurveAnalyzer.sln package --outdated`: обновлений нет.
- `dotnet list CurveAnalyzer.sln package --vulnerable`: уязвимых пакетов нет.
- `dotnet list CurveAnalyzer.sln package --deprecated`: deprecated packages нет после перехода test project с `xunit` на `xunit.v3`.
- `LiveCharts.Wpf 0.9.7` остается documented compatibility exception: package не outdated/deprecated/vulnerable, но restore/build предупреждает `NU1701` для `net10.0-windows7.0`.
- Manual smoke checklist из `quickstart.md` выполнен вручную 2026-04-30:
  yield curve by date, rate history by period и spread between periods работают
  корректно.

## T060 Old TFM/package search

- По project/solution файлам вне `bin/obj` не найдено `net8.0`, `net9.0`, `HintPath`, `LiveChartsCore`, `MoexData` или ссылка root `CurveAnalyzer.csproj`.
- Project-level `PackageReference` не содержит `Version`; package versions централизованы в `Directory.Packages.props`.
- Source-check по `src/**/*.cs` вне `bin/obj` не нашел legacy namespace/messaging symbols или blocking async patterns: `CurveAnalyzer.Interfaces`, `CommunityToolkit.Mvvm.Messaging`, `IMessenger`, `WeakReferenceMessenger`, `DownloadProgressMessage`, `DownloadCompletedMessage`, `.Result`, `.Wait(`, `Task.Run(`.

## T061 Final validation

- `dotnet restore`: успешно, только documented `NU1701` для `LiveCharts` / `LiveCharts.Wpf 0.9.7`.
- `dotnet build CurveAnalyzer.sln -c Release`: успешно, 17 warnings, 0 errors.
- Final build warnings:
  - documented `NU1701` для `LiveCharts` / `LiveCharts.Wpf 0.9.7`;
  - existing `CS8618` nullable warnings в MOEX XML DTO (`IssData.cs`, `SecurityIssData.cs`).
- `dotnet test -c Release`: успешно, 9 tests passed.

## T062 Spec Kit consistency analysis

- Pre-hook note: `.specify/extensions.yml` содержит optional `speckit.git.commit` hooks для analyze; auto-commit не выполнялся из-за dirty worktree и отсутствия запроса на commit.
- Findings после ручного smoke:
  - `SC-003` / `T057` закрыт: manual smoke checklist выполнен вручную,
    все основные UI-сценарии работают корректно.
- Constitution drift, найденный перед финальной записью, исправлен: `.specify/memory/constitution.md` обновлен до `1.1.1`, обязательная build-команда теперь `dotnet build CurveAnalyzer.sln -c Release`.
- Critical/high consistency blockers: не найдено.

## T063 Staging status

- Planned files были собраны и проверены перед commit.
- `.agents/skills/` и `.specify/` исключены из staged diff и добавлены в
  `.gitignore` как local Spec Kit tooling.
- `git diff --cached --check`: успешно.
- Commit: `aed9e61 Удалить legacy-проект CurveAnalyzer`.

## Startup responsiveness follow-up

- Обнаружена причина визуального freeze при старте: `OnlineDataService.GetDataForDateAsync` имел async signature, но синхронно открывал `XmlReader.Create(url)` и десериализовал ISS stream на вызывающем потоке.
- Исправление: `OnlineDataService` теперь получает XML через `HttpClient.GetByteArrayAsync(...)`, затем разбирает XML из памяти; internal awaits в `OnlineDataService` и `DataSyncService` используют `ConfigureAwait(false)`.
- `dotnet build CurveAnalyzer.sln -c Release --no-restore`: успешно, warnings только ранее documented `NU1701` и существующие `CS8618` в MOEX DTO.
- `dotnet test -c Release --no-restore`: успешно, 9 tests passed.
