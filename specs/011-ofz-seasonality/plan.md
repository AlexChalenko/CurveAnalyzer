# Implementation Plan: Сезонность активности ОФЗ

**Branch**: `011-ofz-seasonality` | **Date**: 2026-05-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/011-ofz-seasonality/spec.md`

## Summary

Feature добавляет к экрану "Активность ОФЗ" сезонный контекст: профили
активности по дням недели и месяцам, сравнение insight window с исторической
сезонной базой и structured JSON слой `seasonalityContext`.

Технический подход: использовать уже рассчитанные `OfzActivityMetric`,
построить чистый Core read-model `OfzSeasonalityContext`,
подключить его к `OfzMarketSummaryBuilder`, добавить UI-вкладку "Сезонность" и
обновить JSON schema до `1.5`. Новые MOEX endpoints, SQLite tables и external
factor loaders не требуются.

## Technical Context

**Language/Version**: C#; repo закреплен на .NET SDK `10.0.203` через
`global.json`. Supported app path: `src/`.
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, EF Core SQLite, existing
Core/Application analytics. Новых runtime dependencies не требуется.
**Storage**: existing SQLite cache для trades/activity; новая таблица не нужна.
**Testing**: `dotnet build CurveAnalyzer.sln -c Release`, `dotnet test -c Release`;
targeted Core tests для seasonality builder и summary findings.
**Target Platform**: Windows desktop WPF.
**Project Type**: desktop app с layered architecture.
**Performance Goals**: для периода до одного торгового года seasonality context
строится не дольше 3 секунд после готовности existing activity metrics.
**Constraints**: не использовать future observations для findings; missing
seasonal values не заменяются нулями; сезонность не является прогнозом или
инвестиционной рекомендацией.
**Scale/Scope**: один existing analytics workflow; weekday/month buckets;
активные ОФЗ выбранного периода и coupon type filter; без налоговых периодов и
внешних факторов.

## Constitution Check

- **I. Единственный поддерживаемый путь приложения**: PASS. Feature меняет
  supported `src/` path и SDD docs.
- **II. Прагматичный Domain-Driven Design**: PASS. Новые понятия отражают
  реальные доменные правила: observation, weekday/month bucket, baseline,
  seasonal deviation.
- **III. Направление зависимостей**: PASS. Core содержит read-model и расчеты;
  WPF только отображает; Infrastructure не расширяется.
- **IV. Границы async/UI/data**: PASS. Расчет выполняется после existing load,
  без новых сетевых операций.
- **V. Проверяемость**: PASS. План включает Core tests и summary JSON tests.
- **VI. Документация сначала на русском**: PASS.

## Project Structure

### Documentation

```text
specs/011-ofz-seasonality/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── ofz-seasonality.schema.json
│   └── ui-seasonality-workflows.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code

```text
src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   │   ├── OfzSeasonality.cs
│   │   └── OfzMarketSummary.cs
│   └── Services/
│       ├── OfzSeasonalityContextBuilder.cs
│       └── OfzMarketSummaryBuilder.cs
└── CurveAnalyzer.Presentation.WPF/
    ├── Converters/
    │   └── OfzSeasonalityConverters.cs
    ├── ViewModels/
    │   └── OfzActivityViewModel.cs
    └── Views/
        └── OfzActivityOverviewControl.xaml

tests/
├── CurveAnalyzer.Core.Tests/
│   └── Services/
│       ├── OfzSeasonalityContextBuilderTests.cs
│       └── OfzMarketSummaryBuilderTests.cs
└── CurveAnalyzer.Infrastructure.Tests/
    └── existing tests unchanged unless serialization contract coverage requires it
```

**Structure Decision**: сезонность является еще одним context layer внутри
`OfzMarketSummary`, рядом с breadth, index context и cashflow context, а не
отдельным dashboard или persistence feature.

## Phase 0: Research Findings

См. [research.md](./research.md).

Ключевые решения:

- Источник данных: existing `OfzActivityMetric` после coupon type filtering.
- Bucket types: `Weekday` и `Month`.
- Observation grain: один торговый день по выбранному набору выпусков.
- Baseline для findings: только предыдущие observation rows того же bucket.
- Minimum baseline: 4 предыдущих observation rows для weekday и 3 для month.
- Deviation thresholds: `>= 1.5x` для высокой активности и `<= 0.67x` для
  низкой активности относительно median baseline.

## Phase 1: Design Findings

См. [data-model.md](./data-model.md), [quickstart.md](./quickstart.md) и
контракты в [contracts/](./contracts/).

Целевые contracts:

- `OfzSeasonalityObservation` хранит дневной агрегат активности.
- `OfzSeasonalityBucket` хранит profile по weekday/month bucket.
- `OfzSeasonalityContext` содержит profiles, findings, thresholds и
  limitations.
- `OfzSeasonalityContextBuilder` строит context из metrics без зависимостей на
  WPF/EF/ISS DTO.
- `OfzMarketSummary` получает `SeasonalityContext`, evidence fields и JSON
  schema bump.

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
dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release --filter "FullyQualifiedName~OfzSeasonality"
dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release --filter "FullyQualifiedName~OfzMarketSummaryBuilderTests"
```

Manual smoke:

- Запустить WPF app.
- Открыть "Активность ОФЗ".
- Загрузить период минимум 3-6 месяцев.
- Переключить `Тип`: `Все`, `ОФЗ-ПД`, `ОФЗ-ПК`.
- Проверить вкладку "Сезонность": weekday/month profiles, limitations.
- Проверить structured JSON: `seasonalityContext`, findings, limitations.

## Заметки для реализации

- Не менять existing activity score, breadth, liquidity, index context,
  cashflow context.
- Не добавлять новые ISS calls.
- Не использовать future observations при построении findings.
- Не подставлять нули в missing seasonal values.
- `specs/` ignored: при коммите использовать `git add -f`.
