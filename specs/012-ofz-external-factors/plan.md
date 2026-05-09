# Implementation Plan: Внешние факторы активности ОФЗ

**Branch**: `012-ofz-external-factors` | **Date**: 2026-05-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/012-ofz-external-factors/spec.md`

## Summary

Feature добавляет к экрану "Активность ОФЗ" слой `externalFactorsContext`:
ключевая ставка ЦБ, уже загруженный MOEX index context и доступные
OFZ-derived special metrics как нейтральный внешний фон для активности и
движений доходности.

Технический подход: не добавлять новые внешние загрузчики в V1, а нормализовать
уже доступные данные в Core read-model `OfzExternalFactorsContext`, подключить
его к `OfzMarketSummaryBuilder`, поднять JSON schema до `1.6` и добавить вкладку
`Факторы` в existing WPF summary. Отсутствующие FX/commodity/inflation source
series показываются как limitations, а не как пустые/нулевые значения.

## Technical Context

**Language/Version**: C#; repo закреплен на .NET SDK `10.0.203` через
`global.json`. Supported app path: `src/`.
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, EF Core SQLite, existing
Core/Application analytics. Новых runtime dependencies не требуется.
**Storage**: existing SQLite cache already stores CBR key rates and MOEX index
points; V1 не требует новой таблицы.
**Testing**: `dotnet build CurveAnalyzer.sln -c Release`, `dotnet test -c Release`;
targeted Core tests для factor builder, summary findings и JSON contract.
**Target Platform**: Windows desktop WPF.
**Project Type**: desktop app с layered architecture.
**Performance Goals**: для периода до одного торгового года factor context
строится не дольше 3 секунд после готовности existing summary inputs.
**Constraints**: no-lookahead factor alignment; no missing-as-zero; no
recommendation/forecast/causality wording; snapshot/provisional flags
propagate to context and findings.
**Scale/Scope**: один existing analytics workflow; CBR key rate, MOEX index
context, OFZ-derived special metrics; optional unavailable FX/commodity sources
as limitations.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Единственный поддерживаемый путь приложения**: PASS. Feature меняет
  supported `src/` path и SDD docs.
- **II. Прагматичный Domain-Driven Design**: PASS. External factor, observation
  и activity link являются доменными read-model понятиями.
- **III. Направление зависимостей**: PASS. Core строит read-model; WPF только
  отображает; Infrastructure не расширяется в V1.
- **IV. Границы async/UI/data**: PASS. Расчет идет после existing data load без
  новых сетевых операций.
- **V. Проверяемость**: PASS. План включает Core tests и summary JSON tests.
- **VI. Документация сначала на русском**: PASS.

## Project Structure

### Documentation (this feature)

```text
specs/012-ofz-external-factors/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── ofz-external-factors.schema.json
│   └── ui-external-factors-workflows.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   │   ├── OfzExternalFactors.cs
│   │   └── OfzMarketSummary.cs
│   └── Services/
│       ├── OfzExternalFactorsContextBuilder.cs
│       └── OfzMarketSummaryBuilder.cs
└── CurveAnalyzer.Presentation.WPF/
    ├── Converters/
    │   └── OfzExternalFactorsConverters.cs
    ├── ViewModels/
    │   └── OfzActivityViewModel.cs
    └── Views/
        └── OfzActivityOverviewControl.xaml

tests/
└── CurveAnalyzer.Core.Tests/
    ├── Domain/
    │   └── OfzExternalFactorsContractTests.cs
    └── Services/
        ├── OfzExternalFactorsContextBuilderTests.cs
        └── OfzMarketSummaryBuilderTests.cs
```

**Structure Decision**: external factors are a context layer inside
`OfzMarketSummary`, рядом с index/cashflow/seasonality, not a separate dashboard
or persistence feature.

## Phase 0: Research Findings

См. [research.md](./research.md).

Ключевые решения:

- V1 reuses existing `CbrKeyRates`, `IndexContextDays`, `IndexSegments` and
  `SpecialMetrics`.
- New FX/commodity/inflation loaders are out of V1 unless implementation finds
  an already supported source.
- Alignment policy: macro/policy values use latest-on-or-before; market index
  values use trade-date observations and existing previous-date deltas.
- Missing factors become limitations and visible `Missing` availability.
- JSON schema target: `1.6`.

## Phase 1: Design Findings

См. [data-model.md](./data-model.md), [quickstart.md](./quickstart.md) и
контракты в [contracts/](./contracts/).

## Phase 2: Task Planning Approach

Tasks organized test-first:

1. Add Core read-model and builder tests.
2. Implement factor normalization and no-lookahead alignment.
3. Integrate context/findings/source counts into `OfzMarketSummaryBuilder`.
4. Add WPF summary tab and converters.
5. Validate JSON contract and full build/test.

## Verification

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations.
