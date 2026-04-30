# План реализации: удаление legacy-проекта и стабилизация supported app

**Ветка**: `001-remove-legacy-project` | **Дата**: 2026-04-30 | **Спека**: [spec.md](./spec.md)
**Входные данные**: спецификация из `specs/001-remove-legacy-project/spec.md`

## Резюме

Миграция должна сделать `src`-приложение единственным поддерживаемым путем
CurveAnalyzer: обновить root solution, перевести supported projects на .NET 10
LTS, централизовать и обновить NuGet-зависимости, удалить старый WPF-проект и
связанные legacy-папки, зафиксировать application/domain/infrastructure границы,
убрать machine-local dependencies и runtime database files из supported source
path.

DDD применяется прагматично: моделируем язык и правила ZCYC/yield-curve домена,
но не вводим aggregates/domain events/factories без конкретного инварианта.

## Технический контекст

**Language/Version**: C#; целевое состояние supported projects: libraries
`net10.0`, WPF app `net10.0-windows`, SDK закреплен через `global.json` на
актуальной установленной LTS-линейке .NET 10. Текущее состояние: libraries
`net8.0`, WPF app `net9.0-windows7.0`; эта feature включает выравнивание TFM.
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting,
EF Core SQLite, MOEX ISS XML, LiveCharts.Wpf.
**Dependency Management**: включить NuGet Central Package Management через
`Directory.Packages.props`; убрать `Version` из project-level
`PackageReference`, кроме явно документированных исключений. Все обновляемые
NuGet packages поднять до latest stable из configured sources на момент
реализации. Зафиксированный snapshot от 2026-04-30: CommunityToolkit.* `8.4.2`,
Microsoft.EntityFrameworkCore.* `10.0.7`, Microsoft.Extensions.Hosting `10.0.7`;
`LiveCharts.Wpf` `0.9.7` остается отдельным compatibility risk/exception, если
не принимаем отдельную chart migration.
**Storage**: SQLite через EF Core. Текущий риск: `Database.EnsureCreated()` и
`Data Source = zcyc.db` в composition root; план переводит это к явной
конфигурации и миграциям/инициализации.
**Testing**: тестов сейчас нет. Добавить `tests/CurveAnalyzer.Core.Tests` на
xUnit для доменных расчетов и, при необходимости, application-service тесты с
in-memory/fake сервисами.
**Target Platform**: Windows desktop WPF.
**Project Type**: desktop app с layered architecture.
**Performance Goals**: UI не должен блокироваться во время sync/load; расчет
spread/rate series должен выполняться без лишней повторной загрузки данных.
**Constraints**: сохранить три primary workflows: yield curve by date, rate
history by period, spread between periods. Не захватывать несвязанные dirty
worktree изменения. Документация на русском.
**Scale/Scope**: один desktop app, пять supported projects под `src`, один
feature migration. Внешнего public API нет.

## Проверка конституции

*GATE: должно пройти до Phase 0 research. Повторная проверка после Phase 1 design.*

- **I. Единственный поддерживаемый путь приложения**: PASS для плана. Текущий
  repo нарушает принцип, потому что root `CurveAnalyzer.sln` включает старый
  `CurveAnalyzer.csproj` и внешний `MoexData`. План устраняет нарушение.
- **II. Прагматичный DDD**: PASS. План использует domain language и расчетные
  сервисы, но не вводит тяжелые DDD-паттерны.
- **III. Направление зависимостей и composition root**: PASS для целевого
  состояния. Текущий `App.xaml.cs` смешивает регистрации; план выносит их в
  layer-specific extension methods.
- **IV. Границы async, UI и данных**: PASS для целевого состояния. Текущие
  `Task.Run`, `ContinueWith`, `ConfigureAwait(false)` без await и
  `IMessenger` в Application фиксируются как migration targets.
- **V. Проверяемые и обозримые изменения**: PASS. План делится на этапы с
  build/test validation.
- **VI. Документация сначала на русском**: PASS. Все новые planning-артефакты
  ведутся на русском, технические идентификаторы не переводятся.

## Структура проекта

### Документация этой feature

```text
specs/001-remove-legacy-project/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── legacy-inventory.md       # создается во время реализации
├── implementation-report.md  # создается и пополняется во время реализации
├── contracts/
│   ├── application-boundaries.md
│   └── ui-workflows.md
└── tasks.md
```

### Исходный код в root репозитория

Целевое supported состояние после реализации:

```text
CurveAnalyzer.sln
global.json                              # SDK policy for .NET 10 builds
Directory.Packages.props                 # centralized NuGet package versions

src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   └── Services/                        # чистые domain calculations, если нужны
├── CurveAnalyzer.Application/
│   ├── Interfaces/
│   ├── Services/
│   └── DependencyInjection.cs
├── CurveAnalyzer.ApiServices/
│   ├── Data/
│   ├── OnlineDataService.cs
│   └── DependencyInjection.cs
├── CurveAnalyzer.Infrastructure/
│   ├── Repositories/
│   ├── Migrations/
│   ├── MoexContext.cs
│   └── DependencyInjection.cs
└── CurveAnalyzer.Presentation.WPF/
    ├── ViewModels/
    ├── Views/
    ├── Data/
    ├── App.xaml
    └── App.xaml.cs

tests/
└── CurveAnalyzer.Core.Tests/
```

Legacy source для удаления или явной миграции перед удалением:

```text
CurveAnalyzer.csproj
App.xaml
App.xaml.cs
MainWindow.xaml
MainWindow.xaml.cs
AssemblyInfo.cs
App.config
Charts/
Data/
DataProviders/
Interfaces/
Theme/
Tools/
View/
ViewModel/
Visual Studio 2022/
zcyc.db
zcyc.db-shm
zcyc.db-wal
```

**Структурное решение**: root `CurveAnalyzer.sln` должен стать единственной
supported solution. Вложенную `src/CurveAnalyzer.Presentation.WPF/CurveAnalyzer.Presentation.WPF.sln`
удалить после того, как root solution будет собирать supported app, layers и
tests. Это снижает неоднозначность для Visual Studio и CLI.

## Этап 0: итоги research

См. [research.md](./research.md).

Ключевые решения:

- Root `CurveAnalyzer.sln` становится supported solution.
- Supported projects переводятся на .NET 10 LTS до удаления legacy.
- NuGet versions централизуются через `Directory.Packages.props`, packages
  обновляются до latest stable из configured sources.
- DDD используется как lightweight domain modeling, без aggregates/domain events.
- `DataSyncService` остается application use case, но перестает зависеть от
  `CommunityToolkit.Mvvm.Messaging`.
- EF Core остается в `Infrastructure`; `EnsureCreated()` убрать из
  `DbContext` constructor.
- `LiveCharts.Wpf` сохраняется как временный supported chart stack; локальный
  `HintPath` на `LiveChartsCore` удалить.

## Этап 1: итоги design

См. [data-model.md](./data-model.md), [quickstart.md](./quickstart.md),
[contracts/application-boundaries.md](./contracts/application-boundaries.md) и
[contracts/ui-workflows.md](./contracts/ui-workflows.md).

Целевые application/use-case границы:

- `ICurveDataSource` или близкий application-facing contract для чтения online
  и historical curve data.
- `IYieldCurveHistory` или `IZcycRepository` для persistence, без EF-типов в
  Application.
- `YieldCurveService`/`CurveAnalysisService` для формирования yield curve,
  rate-history и spread-series результатов.
- UI progress через `IProgress<SyncProgress>` или application result stream,
  не через `IMessenger` внутри Application.

Целевые DDD элементы:

- Value objects/records: `TradingDate`, `CurvePeriod`, `YieldPoint`.
- Domain records: `YieldCurve`, `HistoricalSeries`, `SpreadSeries`.
- Domain service только для расчетов без IO: `SpreadCalculator`,
  `RateSeriesGrouper` при переносе weekly/rate behavior.
- Repository как application interface остается оправданным из-за SQLite/MOEX
  границы, но не разрастается в generic repository.

## Повторная проверка конституции после design

- **Единственный поддерживаемый путь приложения**: целевая структура оставляет один root
  solution и supported app path.
- **Прагматичный DDD**: план вводит только язык домена, value objects и чистые
  calculation services. Heavy DDD отклонен.
- **Направление зависимостей**: planned dependencies направлены внутрь; composition
  root остается в WPF, но registrations вынесены по слоям.
- **Границы async/UI/data**: planned state убирает application dependency on
  `IMessenger`, не-await fire-and-forget и constructor DB side effects.
- **Проверяемое изменение**: план требует build, tests для domain calculations и
  manual smoke primary workflows.
- **Русская документация**: все артефакты текущей feature на русском.

## Отслеживание сложности

| Отклонение | Зачем нужно | Почему отклонена более простая альтернатива |
|-----------|------------|--------------------------------------|
| Layered multi-project structure | Уже существует и соответствует UI/Application/Core/Infrastructure разделению | Single-project WPF снова смешает UI, MOEX, EF и domain logic |
| Repository boundary | Нужно отделить SQLite persistence от application use cases и тестов | Прямой `DbContext` в Application нарушит dependency direction |
| Temporary LiveCharts.Wpf compatibility warning on .NET 10 | Полная замена chart stack увеличит scope и риск поведения графиков | Для удаления legacy и TFM upgrade сначала убираем local `HintPath`; если `LiveCharts.Wpf` блокирует .NET 10 build, chart migration становится обязательной задачей этой feature |
| NuGet Central Package Management | В repo несколько projects с повторяющимися package families и разными версиями EF Core/Microsoft.Extensions | Оставить версии в `.csproj` проще сейчас, но хуже контролирует drift при миграции на .NET 10 |

## План проверки

Baseline до реализации:

```powershell
dotnet --info
dotnet restore
dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln -c Release
dotnet list src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln package --outdated
```

Ожидаемое временное ограничение до реализации: full
`dotnet build CurveAnalyzer.sln -c Release` may fail while legacy project remains
in the root solution.

Целевая проверка после реализации:

```powershell
dotnet --version
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
dotnet list CurveAnalyzer.sln package --outdated
dotnet list CurveAnalyzer.sln package --vulnerable
dotnet list CurveAnalyzer.sln package --deprecated
```

Ручная smoke-проверка после build:

- Start supported WPF app.
- Verify yield curve can be shown for an available date.
- Verify rate history can be shown for a selected period.
- Verify spread can be shown for two different periods.

## Заметки для /speckit-tasks

- Tasks must avoid touching unrelated dirty changes unless they are directly in
  planned files.
- First task group should pin .NET 10 SDK policy, migrate supported TFMs and
  enable CPM before deleting folders.
- Package update tasks must keep `LiveCharts.Wpf` as an explicit exception or
  replace the chart stack if it blocks .NET 10 validation.
- Solution/project structure cleanup should happen before deleting legacy
  folders.
- Add tests before moving non-UI calculations out of ViewModels/controls.
- Delete legacy files only after supported workflows are mapped or explicitly
  marked out of scope.
- Stage only planned files when preparing commits.
