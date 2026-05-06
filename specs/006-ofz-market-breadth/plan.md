# Implementation Plan: OFZ Market Breadth

**Branch**: `006-ofz-market-breadth` | **Date**: 2026-05-06 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/006-ofz-market-breadth/spec.md`

## Summary

Добавляем market breadth слой поверх уже загруженных данных активности ОФЗ.
V1 не требует нового источника MOEX ISS и не добавляет persistence: метрики
строятся in-memory из `OfzIssue`, `OfzDailyTrade`, `OfzActivityMetric` и
`OfzLiquidityMetric`, которые уже приходят в экран `Активность ОФЗ`.

Главная цель: показать, было ли движение рынка широким или локальным. Для этого
рассчитываем дневное распределение направлений доходности, долю активных
выпусков, концентрацию оборота top-5/top-10, вклад типов ОФЗ и day drill-down.
Результат расширяет existing `OfzMarketSummary` structured output, чтобы эти же
факты можно было использовать в UI и позже в MCP/API слое.

## Technical Context

**Language/Version**: C# / .NET 10, `net10.0` для Core/Application/Infrastructure, `net10.0-windows10.0.19041` для WPF  
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, LiveChartsCore.SkiaSharpView.WPF, EF Core SQLite, existing MOEX ISS clients  
**Storage**: Existing SQLite через `MoexContext`; market breadth v1 вычисляется в памяти и не сохраняется отдельными snapshot-таблицами  
**Testing**: xUnit v3 в `tests/CurveAnalyzer.Core.Tests`; WPF проверяется сборкой и ручным сценарием  
**Target Platform**: Windows desktop WPF application  
**Project Type**: Desktop app with layered `Core` / `Application` / `Infrastructure` / `Presentation.WPF` architecture  
**Performance Goals**: Построить breadth overview для периода до одного торгового года и примерно 100 выпусков менее чем за 3 секунды после локальной загрузки данных  
**Constraints**: Нет investment recommendations; missing data не трактуется как zero; current-day/snapshot/provisional состояния должны быть явно видны; расчет direction не должен смотреть в будущие даты  
**Scale/Scope**: Один existing экран `Активность ОФЗ`, локальная SQLite-база, issue/day-level агрегаты, segment/day-level агрегаты, structured JSON contract

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Единственный поддерживаемый путь приложения**: PASS. Изменения
  планируются только в supported `src/` пути и existing WPF screen.
- **II. Прагматичный Domain-Driven Design**: PASS. Breadth оформляется как
  доменная read-model модель и pure builder/analyzer в Core без тяжелых
  aggregates/events/repositories.
- **III. Направление зависимостей и composition root**: PASS. Core не зависит
  от WPF, charting или EF. Presentation получает готовые модели через текущий
  application boundary.
- **IV. Границы async, UI и данных**: PASS. Долгие операции остаются в
  `OfzActivityService`; breadth расчет CPU-bound на уже загруженных данных.
- **V. Проверяемые и обозримые изменения**: PASS. Новая доменная логика
  покрывается focused unit tests; UI проверяется сборкой и ручным сценарием.
- **VI. Документация сначала на русском**: PASS. Все feature-артефакты
  описывают поведение на русском, технические идентификаторы не переводятся.

## Project Structure

### Documentation (this feature)

```text
specs/006-ofz-market-breadth/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── market-breadth.schema.json
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   │   ├── OfzActivityViews.cs        # existing activity/detail view models
│   │   ├── OfzMarketSummary.cs        # extend structured summary contract
│   │   └── OfzMarketBreadth.cs        # new breadth domain read-model
│   └── Services/
│       ├── OfzActivityAnalyzer.cs     # existing activity/liquidity helpers
│       └── OfzMarketSummaryBuilder.cs # extend summary builder with breadth
├── CurveAnalyzer.Application/
│   ├── OfzActivityLoadResult.cs
│   └── OfzActivityService.cs
└── CurveAnalyzer.Presentation.WPF/
    ├── ViewModels/
    │   └── OfzActivityViewModel.cs
    ├── Converters/
    │   └── OfzActivityOverviewConverters.cs
    └── Views/
        ├── OfzActivityOverviewControl.xaml
        ├── OfzActivitySignalsControl.xaml
        └── OfzActivityDetailControl.xaml

tests/
└── CurveAnalyzer.Core.Tests/
    └── Services/
        └── OfzMarketSummaryBuilderTests.cs
```

**Structure Decision**: Реализуем как расширение существующего Core summary
слоя. Новые domain-модели могут быть вынесены в `OfzMarketBreadth.cs`, но
построение и ранжирование остаются рядом с `OfzMarketSummaryBuilder`, чтобы не
создавать второй конкурирующий analyzer pipeline.

## Phase 0 Research Decisions

См. [research.md](./research.md).

## Phase 1 Design

- Доменная модель: [data-model.md](./data-model.md)
- Structured contract: [contracts/market-breadth.schema.json](./contracts/market-breadth.schema.json)
- Проверка: [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **I. Supported path**: PASS. Документы и планируемые изменения остаются в
  `src/` и `tests/`.
- **II. DDD**: PASS. Добавляется read-model и pure calculations.
- **III. Dependencies**: PASS. UI получает готовые модели, Core не зависит от
  Presentation.
- **IV. Async/UI/data**: PASS. Нет новых сетевых операций в UI; расчеты
  выполняются после локальной загрузки.
- **V. Проверяемость**: PASS. Tasks должны включить Core tests, build и ручной
  UI сценарий.
- **VI. Русская документация**: PASS.

## Complexity Tracking

Нарушений конституции нет; отдельное complexity justification не требуется.
