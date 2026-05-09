# Implementation Plan: Календарь денежных потоков ОФЗ

**Branch**: `010-ofz-cashflow-calendar` | **Date**: 2026-05-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/010-ofz-cashflow-calendar/spec.md`

## Summary

Feature добавляет к экрану "Активность ОФЗ" календарь событий выпусков:
купоны, амортизации, погашения, оферты и fallback-события из current metadata.
Календарь должен объяснять часть issue-level всплесков активности, показывать
ближайшие события по активным выпускам и попадать в structured JSON.

Технический подход: загрузить MOEX ISS `bondization/{SECID}` по активным
выпускам, сохранить нормализованные `OfzCashflowEvent` в SQLite cache, построить
Core read-model `OfzCashflowContext` и добавить его в `OfzMarketSummary`,
`OfzMarketSummaryBuilder`, `OfzActivityService` и WPF overview/detail.

## Technical Context

**Language/Version**: C#; repo закреплен на .NET SDK `10.0.203` через
`global.json`. Supported app path: `src/`.
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, EF Core SQLite,
HttpClient, MOEX ISS JSON. Новых runtime dependencies не требуется.
**Storage**: SQLite в `%LOCALAPPDATA%\CurveAnalyzer\zcyc.db`; новая таблица
cache для cashflow events, потому что события можно повторно скачать из MOEX ISS.
**Testing**: `dotnet build CurveAnalyzer.sln -c Release`, `dotnet test -c Release`;
targeted Core tests для builder/findings, ApiServices parser tests и
Infrastructure persistence tests.
**Target Platform**: Windows desktop WPF.
**Project Type**: desktop app с layered architecture.
**Performance Goals**: для периода до одного торгового года и активных выпусков
контекст строится не дольше 3 секунд после прогрева cache; отсутствующий
календарь не блокирует existing activity workflow.
**Constraints**: missing cashflow fields не равны нулю; current/snapshot events
помечаются; Core/Application не зависят от WPF/EF/ISS DTO; feature не дает
инвестиционных рекомендаций.
**Scale/Scope**: один existing analytics workflow; daily/event-date
granularity; активные ОФЗ выбранного периода; без прогнозов и налогового
календаря.

## Constitution Check

- **I. Единственный поддерживаемый путь приложения**: PASS. Feature меняет
  только supported `src/` path и SDD docs.
- **II. Прагматичный Domain-Driven Design**: PASS. Новые понятия отражают
  реальные доменные правила: coupon/amortization/maturity/offer dates,
  source status, event window и missing-vs-zero.
- **III. Направление зависимостей**: PASS. ISS/SQLite остаются в
  ApiServices/Infrastructure; Core содержит read-model и чистые расчеты.
- **IV. Границы async/UI/data**: PASS. Сетевой доступ скрыт за
  `IOfzCashflowDataService`; UI получает готовый summary.
- **V. Проверяемость**: PASS. План включает тесты для Core/parser/persistence
  и build/test gates.
- **VI. Документация сначала на русском**: PASS.

## Project Structure

### Documentation

```text
specs/010-ofz-cashflow-calendar/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── moex-cashflow-data-contract.md
│   ├── ofz-cashflow-calendar.schema.json
│   └── ui-cashflow-workflows.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code

```text
src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   │   ├── OfzCashflows.cs
│   │   └── OfzMarketSummary.cs
│   └── Services/
│       ├── OfzCashflowContextBuilder.cs
│       └── OfzMarketSummaryBuilder.cs
├── CurveAnalyzer.Application/
│   ├── Interfaces/
│   │   ├── IOfzCashflowDataService.cs
│   │   └── IOfzActivityRepository.cs
│   └── OfzActivityService.cs
├── CurveAnalyzer.ApiServices/
│   ├── Data/
│   │   └── OfzCashflowIssData.cs
│   ├── DependencyInjection.cs
│   └── OfzCashflowOnlineDataService.cs
├── CurveAnalyzer.Infrastructure/
│   ├── MoexContext.cs
│   ├── Migrations/
│   └── Repositories/
│       └── OfzActivityRepository.cs
└── CurveAnalyzer.Presentation.WPF/
    ├── Converters/
    │   └── OfzCashflowConverters.cs
    ├── ViewModels/
    │   └── OfzActivityViewModel.cs
    └── Views/
        ├── OfzActivityOverviewControl.xaml
        └── OfzActivityIssueDetailControl.xaml
```

**Structure Decision**: сохранить существующий `OfzActivityService` workflow.
Cashflow calendar является еще одним context layer внутри `OfzMarketSummary`, а
не отдельным dashboard.

## Phase 0: Research Findings

См. [research.md](./research.md).

Ключевые решения:

- Primary endpoint:
  `statistics/engines/stock/markets/bonds/bondization/{SECID}.json`.
- Blocks: `coupons`, `amortizations`, `offers`.
- `amortizations.data_source = maturity` трактуется как `Maturity`, остальные
  строки `amortizations` как `Amortization`.
- Current TQOB `securities/{SECID}` и `description` используются как fallback
  для `NEXTCOUPON`, `OFFERDATE`, `MATDATE`, option/buyback fields.
- Event-near-activity default window: `3` календарных дня.
- Cache key: `SecId + EventType + EventDate + SourceKind + optional source key`.

## Phase 1: Design Findings

См. [data-model.md](./data-model.md), [quickstart.md](./quickstart.md) и
контракты в [contracts/](./contracts/).

Целевые contracts:

- `OfzCashflowEvent` хранит нормализованное событие без fake zero.
- `OfzCashflowContext` содержит события периода, ближайшие события, issue
  calendars и limitations.
- `OfzCashflowContextBuilder` связывает issue-level activity rows с событиями в
  заданном окне.
- `IOfzCashflowDataService` загружает события из MOEX ISS.
- `IOfzActivityRepository` хранит/читает cashflow cache.
- `OfzMarketSummary` получает `CashflowContext`, cashflow evidence fields и
  JSON schema bump.

## Constitution Check After Design

- **Единственный поддерживаемый путь**: PASS.
- **Прагматичный DDD**: PASS.
- **Направление зависимостей**: PASS.
- **Async/UI/data boundaries**: PASS.
- **Проверяемость**: PASS.
- **Русская документация**: PASS.

## Complexity Tracking

Нарушений конституции нет.

## План проверки

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

Targeted:

```powershell
dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release
dotnet test tests\CurveAnalyzer.Infrastructure.Tests\CurveAnalyzer.Infrastructure.Tests.csproj -c Release
```

Manual smoke:

- Запустить WPF app.
- Открыть "Активность ОФЗ".
- Загрузить период `24.03.2026` - `08.05.2026`.
- Проверить overview-блок ближайших cashflow events.
- Выбрать выпуск из top contributors и проверить календарь в детализации.
- Проверить structured JSON: `cashflowContext`, `cashflowEvents`,
  `cashflowFindings`, limitations.

## Заметки для реализации

- Сначала Core domain/tests, затем parser/persistence, затем Application и UI.
- Не менять existing activity score, breadth, liquidity, index context.
- Не подставлять нули в missing money/percent fields.
- `specs/` ignored: при коммите использовать `git add -f`.
