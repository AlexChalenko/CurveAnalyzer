# Implementation Plan: OFZ Analytics Summary

**Branch**: `005-ofz-analytics-summary` | **Date**: 2026-05-04 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/005-ofz-analytics-summary/spec.md`

## Summary

Добавляем сводный аналитический слой поверх уже загруженных данных ОФЗ:
activity metrics, historical liquidity/spread, current snapshot liquidity,
issue metadata и yield/price history. V1 не добавляет новые внешние источники и
не сохраняет отдельные summary-снимки в БД. Сводка строится как чистая доменная
модель в `Core` из Core-only входов (`OfzIssue`, `OfzActivityMetric`,
`OfzDailyTrade`, `OfzLiquidityMetric`). Текущий `OfzActivityViewModel`
применяет фильтры к `OfzActivityLoadResult`, передает отфильтрованные коллекции
в builder, отображает короткий набор explainable findings и одновременно
держит стабильный structured contract для будущего MCP/API слоя.

## Technical Context

- **Language/Version**: C# / .NET 10, `net10.0` для Core/Application/Infrastructure, `net10.0-windows10.0.19041` для WPF
- **Primary Dependencies**: WPF, CommunityToolkit.Mvvm, LiveChartsCore.SkiaSharpView.WPF, EF Core SQLite, existing MOEX ISS clients
- **Storage**: Existing SQLite via `MoexContext`; new summary is computed in memory and not persisted in v1
- **Testing**: xUnit v3 in `tests/CurveAnalyzer.Core.Tests`; optional WPF build validation
- **Target Platform**: Windows desktop WPF application
- **Project Type**: Desktop app with layered `Core` / `Application` / `Infrastructure` / `Presentation.WPF` architecture
- **Performance Goals**: Build summary for up to 90 trading days and about 100 issues in under 10 seconds on locally warmed data
- **Constraints**: No investment recommendations; no missing-as-zero; current-day and snapshot-only data must be explicitly marked; no new external sources in v1
- **Scale/Scope**: One existing OFZ Activity screen, local SQLite data, approximately 252 warmed trading days, issue-level and segment-level summaries

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Единственный поддерживаемый путь приложения**: PASS. Изменения касаются
  только поддерживаемого `src/` пути и существующего WPF-приложения.
- **II. Прагматичный Domain-Driven Design**: PASS. Summary вводится как
  доменная read-model модель и чистый analyzer без тяжелых aggregates/events.
- **III. Направление зависимостей и composition root**: PASS. Core не зависит
  от WPF/LiveCharts/EF; UI вызывает Core через уже загруженные application
  данные.
- **IV. Границы async, UI и данных**: PASS. Долгие операции остаются в
  `OfzActivityService`; построение summary синхронное и CPU-bound на уже
  загруженных коллекциях.
- **V. Проверяемые и обозримые изменения**: PASS. Новая логика покрывается
  unit-тестами Core; UI проверяется сборкой и ручным сценарием.
- **VI. Документация сначала на русском**: PASS. Все SDD-артефакты feature
  ведутся на русском, технические идентификаторы не переводятся.

## Project Structure

### Documentation (this feature)

```text
specs/005-ofz-analytics-summary/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── market-summary.schema.json
├── checklists/
│   └── requirements.md
└── tasks.md              # создается на следующем шаге /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   │   ├── OfzActivityViews.cs        # existing activity/detail view models
│   │   └── OfzMarketSummary.cs        # new structured summary domain model
│   └── Services/
│       ├── OfzActivityAnalyzer.cs     # existing activity/liquidity helpers
│       └── OfzMarketSummaryBuilder.cs # new pure summary builder
├── CurveAnalyzer.Application/
│   └── OfzActivityLoadResult.cs       # existing loaded data boundary consumed by UI
└── CurveAnalyzer.Presentation.WPF/
    ├── ViewModels/
    │   └── OfzActivityViewModel.cs    # binds summary, filter/drill-down state
    └── Views/
        └── OfzActivityControl.xaml    # summary cards/list and drill-down UI

tests/
└── CurveAnalyzer.Core.Tests/
    └── Services/
        └── OfzMarketSummaryBuilderTests.cs
```

**Structure Decision**: Feature stays inside the existing OFZ Activity vertical
slice. The summary contract is a Core read model, because it must be reusable by
UI now and by a future MCP/API layer later. Core does not reference
`OfzActivityLoadResult`; Presentation unwraps the existing Application result
and passes Core collections into `OfzMarketSummaryBuilder`. No repository or
migration is introduced in v1.

## Phase 0: Research

Research decisions are captured in [research.md](./research.md). Key outcomes:
compute summary from already loaded in-memory data, use a typed finding/evidence
model instead of plain strings, preserve missing/provisional/snapshot flags, and
keep export/API readiness as a JSON-compatible contract without starting a
server.

## Phase 1: Design

Design artifacts:

- [data-model.md](./data-model.md) describes `MarketSummary`,
  `SummaryFinding`, `FindingEvidence`, `SegmentSummary`, `IssueFocus` and
  `DataLimitation`.
- [contracts/market-summary.schema.json](./contracts/market-summary.schema.json)
  documents the structured summary shape.
- [quickstart.md](./quickstart.md) defines verification commands and manual
  checks.

## Post-Design Constitution Check

- **Supported path**: PASS. Planned files are under `src/` and `tests/`.
- **DDD boundary**: PASS. Summary rules are in Core; WPF only formats and
  selects.
- **Dependency direction**: PASS. No Core dependency on Application, EF,
  LiveCharts or WPF.
- **Async/UI/data boundary**: PASS. Existing load path remains async; summary
  computation is deterministic and synchronous.
- **Verification**: PASS. Plan includes focused Core tests plus
  `dotnet build CurveAnalyzer.sln -c Release`.
- **Docs language**: PASS.

## Complexity Tracking

No constitution violations. No extra project, storage table or external service
is planned for v1.
