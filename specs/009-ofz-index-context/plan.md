# Implementation Plan: Индексный контекст ОФЗ

**Branch**: `009-ofz-index-context` | **Date**: 2026-05-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/009-ofz-index-context/spec.md`

## Summary

Feature добавляет в существующий экран "Активность ОФЗ" фоновый индексный
контекст MOEX: `RGBI`, `RGBITR` и duration-сегменты гос. облигаций. Пользователь
должен видеть, сопровождались ли всплески активности и breadth-выводы общим
движением рынка, и иметь числовое evidence по индексу за выбранную дату.

Технический подход: загрузить историю индексов из MOEX ISS `stock/index`,
сохранить ее в SQLite cache как nullable daily points, построить доменный
`OfzIndexContext` в Core и добавить его в существующий `OfzMarketSummary` и
WPF overview. Current-day значения остаются provisional/snapshot context и не
заменяют исторические ряды нулями.

## Technical Context

**Language/Version**: C#; repo закреплен на .NET SDK `10.0.203` через
`global.json`. Supported libraries таргетят `net10.0`; WPF app таргетит
`net10.0-windows10.0.19041`.
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting,
EF Core SQLite, HttpClient, MOEX ISS JSON, LiveChartsCore.SkiaSharpView.WPF.
Новых runtime dependencies не планируется.
**Storage**: SQLite в `%LOCALAPPDATA%\CurveAnalyzer\zcyc.db`. Добавляется cache
для индексных рядов и EF migration. Индексные данные можно повторно скачать из
MOEX ISS.
**Testing**: `dotnet restore`, `dotnet build CurveAnalyzer.sln -c Release`,
`dotnet test -c Release`; targeted tests для Core index-context rules,
ApiServices parser и Infrastructure persistence; ручная smoke-проверка экрана
"Активность ОФЗ".
**Target Platform**: Windows desktop WPF, Windows 10 2004 / build 19041 или
выше.
**Project Type**: desktop app с layered architecture.
**Performance Goals**: загрузка и расчет index context для периода до одного
торгового года и базового набора до 10 индексных серий не дольше 3 секунд после
прогрева локальных данных; отсутствие индексов не должно замедлять существующие
activity workflows.
**Constraints**: индексный фон не является пересчетом выбранного типа ОФЗ;
missing index/yield/duration не равны нулю; current-day данные помечаются как
предварительные; Core/Application не зависят от WPF, EF Core или chart types;
feature не дает инвестиционных рекомендаций.
**Scale/Scope**: один existing analytics workflow в supported WPF app; дневная
гранулярность; MOEX `stock/index` для `RGBI`, `RGBITR` и duration-сегментов;
без прогнозной модели, внешней макростатистики и отдельного dashboard.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Единственный поддерживаемый путь приложения**: PASS. Feature расширяет
  только supported `src/` WPF path и существующий экран активности ОФЗ.
- **II. Прагматичный Domain-Driven Design**: PASS. Новые понятия
  `OfzMarketIndexSeries`, `OfzMarketIndexPoint`, `OfzIndexContextDay` отражают
  реальные доменные правила: индексный фон, segment series, missing-vs-zero и
  provisional status.
- **III. Направление зависимостей и composition root**: PASS. ISS/SQLite
  остаются в ApiServices/Infrastructure, chart types остаются в Presentation,
  Core содержит только доменные модели и чистые расчеты.
- **IV. Границы async, UI и данных**: PASS. Сетевая загрузка скрыта за
  `IOfzIndexDataService`; persistence использует migration; UI получает
  готовую модель через существующий async workflow.
- **V. Проверяемые и обозримые изменения**: PASS. План задает unit tests,
  parser/persistence tests, build/test gates и ручную smoke-проверку.
- **VI. Документация сначала на русском**: PASS. Все feature artifacts ведутся
  на русском с сохранением технических идентификаторов.

## Project Structure

### Documentation (this feature)

```text
specs/009-ofz-index-context/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── moex-index-data-contract.md
│   ├── ofz-index-context.schema.json
│   └── ui-index-context-workflows.md
├── checklists/
│   └── requirements.md
└── tasks.md              # создается на /speckit-tasks
```

### Source Code (repository root)

Planned supported scope:

```text
src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   │   ├── OfzMarketIndex.cs              # новые index-domain модели
│   │   └── OfzMarketSummary.cs            # index context в summary contract
│   └── Services/
│       ├── OfzIndexContextBuilder.cs      # чистый расчет index context
│       └── OfzMarketSummaryBuilder.cs     # index-backed findings
├── CurveAnalyzer.Application/
│   ├── Interfaces/
│   │   ├── IOfzIndexDataService.cs        # загрузка MOEX index history/current
│   │   └── IOfzActivityRepository.cs      # хранение/чтение index points
│   └── OfzActivityService.cs              # догружает index context
├── CurveAnalyzer.ApiServices/
│   ├── Data/
│   │   └── OfzIndexIssData.cs             # ISS parser models
│   ├── DependencyInjection.cs
│   └── OfzIndexOnlineDataService.cs
├── CurveAnalyzer.Infrastructure/
│   ├── MoexContext.cs
│   ├── Migrations/
│   └── Repositories/
│       └── OfzActivityRepository.cs
└── CurveAnalyzer.Presentation.WPF/
    ├── Converters/
    │   └── OfzIndexContextConverters.cs
    ├── ViewModels/
    │   └── OfzActivityViewModel.cs
    └── Views/
        ├── OfzActivityOverviewControl.xaml
        └── OfzActivitySegmentsDetailControl.xaml

tests/
├── CurveAnalyzer.Core.Tests/
│   └── Services/
│       ├── OfzIndexContextBuilderTests.cs
│       └── OfzMarketSummaryBuilderTests.cs
├── CurveAnalyzer.Infrastructure.Tests/
│   ├── ApiServices/
│   │   └── OfzIndexOnlineDataServiceTests.cs
│   └── Repositories/
│       └── OfzActivityRepositoryTests.cs
```

Out of scope:

```text
legacy root project
external macroeconomic data
forecast/recommendation engine
intraday index ticks
corporate/municipal bond index workflows
new chart library
separate analytics dashboard
```

**Structure Decision**: сохранить существующую layered architecture и не
создавать отдельный экран. Индексный слой является расширением
`OfzActivityService`/`OfzMarketSummaryBuilder`/`OfzActivityViewModel`. Доменные
расчеты остаются в Core; ISS parsing - в ApiServices; SQLite persistence - в
Infrastructure; визуализация - в Presentation.

## Phase 0: Research Findings

См. [research.md](./research.md).

Ключевые решения:

- Whole-market source: MOEX ISS
  `history/engines/stock/markets/index/securities/{SECID}.json`.
- `RGBI` и `RGBITR` являются обязательными whole-market сериями V1.
- Duration-сегменты V1: `RUGBICP1Y`, `RUGBICP3Y`, `RUGBICP5Y`,
  `RUGBICP7Y+`, `RUGBITR1Y`, `RUGBITR3Y`, `RUGBITR5Y`, `RUGBITR7Y+`.
- Минимальные поля history: `SECID`, `TRADEDATE`, `CLOSE`, `VALUE`,
  `DURATION`, `YIELD`, `CURRENCYID`, `SHORTNAME`, `NAME`.
- Current endpoint используется только как provisional snapshot для выбранного
  текущего дня.
- История индексов хранится в SQLite cache, потому что экран уже работает с
  локальным прогревом данных.
- Daily index move считается только при наличии предыдущей торговой точки той
  же серии.

## Phase 1: Design Findings

См. [data-model.md](./data-model.md),
[quickstart.md](./quickstart.md),
[contracts/moex-index-data-contract.md](./contracts/moex-index-data-contract.md),
[contracts/ofz-index-context.schema.json](./contracts/ofz-index-context.schema.json)
и [contracts/ui-index-context-workflows.md](./contracts/ui-index-context-workflows.md).

Целевые domain/application contracts:

- `OfzMarketIndexSeries` описывает код, тип ряда и роль индекса.
- `OfzMarketIndexPoint` хранит nullable daily values и source status.
- `OfzIndexContextDay` сопоставляет index values с activity/breadth day.
- `OfzIndexContextBuilder` считает daily changes, segment context,
  limitations и index-backed findings без зависимости от UI/EF/ISS.
- `OfzMarketSummary` расширяется index context collection и index findings.
- `OfzActivityService` гарантирует загрузку index points для выбранного периода
  и передает их в summary builder.

Целевые persistence/data contracts:

- `IOfzIndexDataService` загружает index history range и latest snapshot.
- `IOfzActivityRepository` получает методы чтения/сохранения `OfzMarketIndexPoint`.
- `MoexContext` получает таблицу index points с уникальным ключом
  `SecId + TradeDate + SourceKind`.
- ISS adapter не подставляет нули и сохраняет неизвестные поля как null.

Целевые UI contracts:

- Overview показывает компактный блок index context и index-backed findings.
- Activity index chart может получить фоновую линию `RGBI`/`RGBITR` или
  отдельный компактный chart, если overlay ухудшает читаемость.
- Duration-segment context отображается только для доступных рядов.
- Drill-down выбранного summary finding/day показывает index values,
  daily changes, yield, duration и limitations.

## Constitution Check After Design

- **Единственный поддерживаемый путь приложения**: PASS. План изменяет только
  supported `src/` и docs текущей feature.
- **Прагматичный DDD**: PASS. Введены только доменные сущности с реальными
  инвариантами: series role, daily point, missing-vs-zero, previous point,
  provisional status.
- **Направление зависимостей**: PASS. Внешние ISS/EF/LiveCharts types не
  попадают в Core/Application contracts.
- **Границы async/UI/data**: PASS. Сетевые операции остаются за service
  boundary; UI получает готовый context.
- **Проверяемость**: PASS. Определены automated checks, targeted unit tests,
  parser/persistence tests и manual smoke.
- **Русская документация**: PASS.

## Complexity Tracking

Нарушений конституции нет.

## План проверки

Baseline перед реализацией:

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
dotnet ef migrations list --project src\CurveAnalyzer.Infrastructure\CurveAnalyzer.Infrastructure.csproj --startup-project src\CurveAnalyzer.Infrastructure\CurveAnalyzer.Infrastructure.csproj --context MoexContext --configuration Release --no-build
```

Target automated checks после реализации:

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

Дополнительные targeted checks:

```powershell
dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release
dotnet test tests\CurveAnalyzer.Infrastructure.Tests\CurveAnalyzer.Infrastructure.Tests.csproj -c Release
```

Manual smoke:

- Запустить supported WPF app.
- Открыть "Активность ОФЗ".
- Загрузить период с прогретыми данными, например `24.03.2026` - `08.05.2026`.
- Проверить, что старые scenarios работают: top anomalies, heatmap, summary,
  breadth, детализация выпуска, спецметрики ОФЗ-ПК/ОФЗ-ИН.
- В режиме `Обзор` проверить блок index context для `RGBI`/`RGBITR`.
- Выбрать день с высоким activity score и проверить index-backed finding.
- Проверить день без предыдущего index point: daily change должен быть `n/a`,
  а не `0`.
- Проверить current-day/provisional marker для snapshot/current значений.
- Переключить тип ОФЗ: индексный фон должен остаться market context, а не
  пересчитаться как фильтрованный набор выпусков.
- Проверить duration-segment context и missing segment limitations.
- Открыть structured JSON export и проверить index context fields.

## Заметки для /speckit-tasks

- Сначала добавить Core domain/read-model и tests для missing-vs-zero,
  previous-point daily move, provisional status и no-recommendation language.
- Затем добавить ISS parser/service и persistence cache с migration.
- После foundation подключить index context в `OfzActivityService` и
  `OfzMarketSummaryBuilder`.
- UI добавлять только после стабильного Core/Application contract.
- Не вводить новую chart library.
- Не менять existing activity score calculation.
- Stage ignored SDD files explicitly with `git add -f`, если нужно коммитить
  спеки.
