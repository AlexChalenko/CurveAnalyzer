# План реализации: анализ активности ОФЗ

**Branch**: `003-ofz-activity` | **Date**: 2026-05-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/003-ofz-activity/spec.md`

## Summary

Feature добавляет новый аналитический экран "Активность ОФЗ": пользователь
выбирает диапазон дат, видит рейтинг всплесков торгов по выпускам ОФЗ, heatmap
относительной активности, детализацию выбранного выпуска и опциональное
распределение активности по дюрации/доходности.

Технический подход: расширить существующий supported WPF path в `src/`,
добавив новый read-model для дневной торговой истории ОФЗ из MOEX ISS. Данные
грузятся по дням из ISS bonds history по board `TQOB`, при старте приложения
заранее прогреваются за последний торговый год и сохраняются в тот же
user-local SQLite storage
через EF migrations; legacy cache может быть пересоздан. Расчеты всплесков живут в
Core/Application без зависимости от WPF или chart types. Presentation получает
отдельный `OfzActivityViewModel` и view с таблицей аномалий, heatmap и деталями
выбранного выпуска.

## Technical Context

**Language/Version**: C#; repo закреплен на .NET SDK `10.0.203` через
`global.json`. Supported libraries таргетят `net10.0`; WPF app таргетит
`net10.0-windows10.0.19041`.
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting,
EF Core SQLite, HttpClient, MOEX ISS JSON/XML, LiveChartsCore.SkiaSharpView.WPF
для графиков в Presentation.
**Storage**: SQLite в `%LOCALAPPDATA%\CurveAnalyzer\zcyc.db`. База считается
локальным cache storage: ZCYC и OFZ history можно повторно скачать из MOEX ISS.
Новая схема создается через EF migrations. Если найден старый
`EnsureCreated`-созданный файл без `__EFMigrationsHistory`, приложение
пересоздает cache-базу вместо сложного сохранения старых строк.
**Testing**: `dotnet restore`, `dotnet build CurveAnalyzer.sln -c Release`,
`dotnet test -c Release`; новые unit tests для расчетов активности и
repository/service tests там, где логика не зависит от WPF rendering; ручная
smoke-проверка нового экрана.
**Target Platform**: Windows desktop WPF, Windows 10 2004 / build 19041 или
выше.
**Project Type**: desktop app с layered architecture.
**Performance Goals**: расчет рейтинга аномалий и обновление экрана для
диапазона до 90 торговых дней и примерно 100 выпусков не дольше 10 секунд на
типовом локальном наборе; последний торговый год истории ОФЗ заранее
прогревается локально при старте, а повторное открытие уже сохраненного
диапазона без сетевой загрузки должно быть заметно быстрее.
**Constraints**: данные ISS могут быть частично пустыми; сетевые операции
должны быть асинхронными и отменяемыми; Core/Application не зависят от WPF,
LiveCharts2 или EF Core; валютные ОФЗ не смешиваются с рублевыми без явной
пометки; feature не является инвестиционной рекомендацией.
**Scale/Scope**: один новый analytics workflow в supported WPF app; дневная
гранулярность; MOEX ISS bonds `TQOB` и агрегаты рынка облигаций; без
intraday trades, order book, корпоративных/муниципальных облигаций и внешних
макро-рядов в первом релизе.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Единственный поддерживаемый путь приложения**: PASS. Feature работает
  только в supported `src/` solution path и не возвращает legacy root project.
- **II. Прагматичный DDD**: PASS. Новые понятия `OfzIssue`,
  `OfzDailyTrade`, `ActivityScore` и `ActivityAnomaly` отражают реальные
  предметные правила и расчеты, а не UI-состояние.
- **III. Направление зависимостей и composition root**: PASS. ISS и EF Core
  остаются в ApiServices/Infrastructure, chart types остаются в Presentation,
  Core содержит только доменные модели и чистые расчеты.
- **IV. Границы async, UI и данных**: PASS. Синхронизация ISS будет
  асинхронной, отменяемой и ожидаемой; persistence обновляется через явную
  schema-upgrade boundary, а не через побочные эффекты конструкторов.
- **V. Проверяемые и обозримые изменения**: PASS. План задает unit tests для
  расчетов активности, build/test gates и ручную smoke-проверку нового экрана.
- **VI. Документация сначала на русском**: PASS. Все feature artifacts ведутся
  на русском с сохранением технических идентификаторов.

## Project Structure

### Documentation (this feature)

```text
specs/003-ofz-activity/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── iss-data-contract.md
│   └── ui-activity-workflows.md
└── tasks.md              # создается на /speckit-tasks
```

### Source Code (repository root)

Planned supported scope:

```text
src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   │   ├── OfzIssue.cs
│   │   ├── OfzDailyTrade.cs
│   │   ├── OfzActivityMetric.cs
│   │   └── OfzActivityAnomaly.cs
│   └── Services/
│       └── OfzActivityAnalyzer.cs
├── CurveAnalyzer.Application/
│   ├── Interfaces/
│   │   ├── IOfzActivityDataService.cs
│   │   └── IOfzActivityRepository.cs
│   └── OfzActivityService.cs
├── CurveAnalyzer.ApiServices/
│   ├── Data/
│   │   └── OfzHistoryIssData.cs
│   └── OfzActivityOnlineDataService.cs
├── CurveAnalyzer.Infrastructure/
│   ├── Repositories/
│   │   └── OfzActivityRepository.cs
│   ├── Migrations/
│   └── MoexContext.cs
└── CurveAnalyzer.Presentation.WPF/
    ├── ViewModels/
    │   └── OfzActivityViewModel.cs
    ├── Views/
    │   └── OfzActivityControl.xaml
    ├── Converters/
    │   └── ActivityHeatmapColorConverter.cs
    └── DependencyInjection.cs

tests/
└── CurveAnalyzer.Core.Tests/
    └── Services/
        └── OfzActivityAnalyzerTests.cs
```

Out of scope:

```text
legacy root project
external macroeconomic data import
intraday trades/orderbook storage
investment advice/recommendation engine
corporate/municipal bond activity workflows
```

**Structure Decision**: сохранить существующую layered architecture. Новые
расчеты активности живут в Core; orchestration, sync и DTO contracts - в
Application; MOEX ISS parsing - в ApiServices; SQLite persistence - в
Infrastructure; UI state/rendering - в Presentation. Новые chart/heatmap
helpers не протекают ниже Presentation.

## Phase 0: Research Findings

См. [research.md](./research.md).

Ключевые решения:

- Primary source для дневной истории ОФЗ: MOEX ISS
  `history/engines/stock/markets/bonds/boards/TQOB/securities.json`.
- Current-day snapshot: MOEX ISS
  `engines/stock/markets/bonds/boards/TQOB/securities.json?iss.only=securities,marketdata`.
- Aggregated market context: MOEX ISS
  `statistics/engines/stock/markets/bonds/aggregates.json`.
- MVP работает на дневной гранулярности и не хранит intraday trades/orderbook.
- Основная метрика всплеска: отношение дневного `VALUE` к медиане `VALUE` за
  предыдущие 20 доступных торговых записей этого выпуска; минимум 10 базовых
  записей для уверенного score.
- Доходность для движения: `YIELDATWAP`, fallback `YIELDCLOSE`, если
  средневзвешенная доходность отсутствует.
- Heatmap строится как WPF matrix view с цветовым converter, чтобы не вводить
  новый chart dependency risk; LiveCharts2 используется для time-series и
  scatter-графиков.
- Persistence: `zcyc.db` является disposable cache; старая `EnsureCreated`
  база без migration history пересоздается, после чего ZCYC/OFZ данные можно
  скачать заново.

## Phase 1: Design Findings

См. [data-model.md](./data-model.md),
[quickstart.md](./quickstart.md),
[contracts/iss-data-contract.md](./contracts/iss-data-contract.md) и
[contracts/ui-activity-workflows.md](./contracts/ui-activity-workflows.md).

Целевые data/application contracts:

- `OfzIssue` описывает выпуск и валюто/типовую классификацию.
- `OfzDailyTrade` хранит одну дневную запись торгов для выпуска.
- `OfzActivityMetric` хранит расчет относительной активности за дату.
- `OfzActivityAnomaly` связывает выпуск, дату, score и движение доходности.
- `OfzActivityService` обеспечивает предварительную синхронизацию последнего
  торгового года истории по дням, load/sync выбранного диапазона, расчет
  рейтинга, heatmap cells, detail series и activity index.
- ISS adapter обязан поддерживать pagination через `history.cursor` и
  частично пустые numeric поля.

Целевые UI contracts:

- Новый пункт навигации "Активность ОФЗ" открывает отдельный экран.
- Экран принимает date range, показывает top anomalies, heatmap, detail panel
  выбранного выпуска и опциональный scatter "дюрация - доходность".
- Пустой диапазон, недостаточная база сравнения и сетевые ошибки показываются
  как управляемые состояния, не как stale chart data.

## Constitution Check After Design

- **Единственный поддерживаемый путь приложения**: PASS. Все planned edits
  находятся в `src/` и tests supported solution.
- **Прагматичный DDD**: PASS. Новые domain records/services отражают реальные
  инварианты: выпуск, торговая дата, историческая база, относительный score.
- **Направление зависимостей**: PASS. Внешние ISS/SQLite/LiveCharts2 types не
  попадают в Core/Application contracts.
- **Границы async/UI/data**: PASS. План предусматривает async/cancellable sync,
  no fire-and-forget ниже UI boundary и явную schema migration boundary.
- **Проверяемость**: PASS. Определены automated checks и ручные workflows.
- **Русская документация**: PASS.

## Complexity Tracking

Нарушений конституции нет.

## План проверки

Baseline перед реализацией:

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

Target automated checks после реализации:

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

Дополнительные targeted checks:

```powershell
dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release
```

Manual smoke:

- Запустить supported WPF app.
- Открыть "Активность ОФЗ".
- При первом открытии экрана проверить предварительную загрузку недавней
  истории по дням, затем выбрать диапазон до 90 торговых дней с данными.
- Проверить, что top anomalies содержит выпуск, дату, оборот, score и
  yield move или явную пометку отсутствия доходности.
- Проверить heatmap: смена диапазона очищает старые ячейки.
- Выбрать выпуск из рейтинга/heatmap и проверить detail series по обороту и
  доходности.
- Проверить пустой или слишком узкий диапазон: приложение не падает и не
  показывает устаревшие данные.

## Заметки для /speckit-tasks

- Перед кодовыми изменениями проверить текущее состояние `MoexContext` и
  `DatabaseInitializer`: существующий local `zcyc.db` мог быть создан через
  `EnsureCreated`, поэтому новая схема должна либо мигрировать migration-based
  базы, либо пересоздать legacy cache.
- Сначала добавить Core tests для `OfzActivityAnalyzer`, затем Application
  contracts, потом ISS adapter и persistence.
- В `ApiServices` использовать JSON для bonds history endpoints; существующий
  XML ZCYC path оставить без изменения.
- Для ISS history обязательно обработать pagination через `history.cursor` и
  nullable numeric fields.
- В UI не смешивать chart types с Application/Core models: chart series
  создавать только в Presentation converters/view.
- Не захватывать unrelated ignored SDD folders или old migration feature files
  при staging; `specs/` и `.specify/` игнорируются, поэтому SDD files нужно
  stage-ить явно через `git add -f`, если нужен commit.
