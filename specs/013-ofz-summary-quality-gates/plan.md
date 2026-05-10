# Implementation Plan: Quality gates для OFZ summary/export

**Branch**: `013-ofz-summary-quality-gates` | **Date**: 2026-05-10 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/013-ofz-summary-quality-gates/spec.md`

## Summary

Feature добавляет повторяемые automated quality gates для итогового
`OfzMarketSummary` и JSON export. Цель: ловить регрессии, которые видны только
на полном summary-сценарии после features 006-012: потеря секций, sourceCounts,
date-only контракта, важных findings, snapshot/provisional limitations или
missing-data политики.

Технический подход: не менять production pipeline и WPF. Реализовать компактный
representative scenario в `CurveAnalyzer.Core.Tests`, добавить assertions для
summary object и serialized JSON, переиспользуя существующий
`OfzMarketSummaryBuilder`. Large `.tmp` exports остаются ручными smoke
артефактами, а не test fixtures.

## Technical Context

**Language/Version**: C#; .NET SDK `10.0.203` через `global.json`.
**Primary Dependencies**: existing `CurveAnalyzer.Core`, `System.Text.Json`,
MSTest/xUnit-style test stack already used by `CurveAnalyzer.Core.Tests`.
**Storage**: N/A. Persistence schema не меняется.
**Testing**: `dotnet test -c Release`; targeted filter
`FullyQualifiedName~OfzSummaryQualityGateTests`.
**Target Platform**: Windows desktop app, но feature тестирует Core contract без
WPF automation.
**Project Type**: desktop app с Core test project.
**Performance Goals**: representative gate должен выполняться за секунды в
обычном test run; не использовать network/file I/O.
**Constraints**: no new network access; no committed `.tmp` exports; no
analytics semantics changes; no recommendation/causality wording.
**Scale/Scope**: один compact representative summary scenario плюс focused
missing-data scenario.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Единственный поддерживаемый путь приложения**: PASS. Изменения только в
  supported `src/` test path и SDD docs; legacy path не трогается.
- **II. Прагматичный Domain-Driven Design**: PASS. Feature не вводит
  production domain abstractions без необходимости; gates работают поверх
  existing domain read-model.
- **III. Направление зависимостей**: PASS. Core tests зависят от Core; WPF и
  Infrastructure не вовлекаются.
- **IV. Границы async/UI/data**: PASS. Нет UI automation, network или
  persistence side effects.
- **V. Проверяемость**: PASS. Feature сама является проверочным слоем и
  включает targeted/full test commands.
- **VI. Документация сначала на русском**: PASS.

## Project Structure

### Documentation (this feature)

```text
specs/013-ofz-summary-quality-gates/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ofz-summary-quality-gates.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
tests/
└── CurveAnalyzer.Core.Tests/
    └── Services/
        └── OfzSummaryQualityGateTests.cs
```

**Structure Decision**: 013 is test-only quality work. New production classes are
not planned unless implementation proves duplication inside tests is harmful.

## Phase 0: Research Findings

См. [research.md](./research.md).

Ключевые решения:

- Не зависеть от `.tmp` JSON exports в automated tests.
- Проверять serialized JSON через `System.Text.Json` и existing enum/string
  options.
- Использовать representative synthetic summary input, covering:
  breadth/index/cashflow/seasonality/externalFactors/specialMetrics.
- Keep gates narrow: they assert contract surfaces, not exact financial
  rankings beyond required finding presence.

## Phase 1: Design Findings

См. [data-model.md](./data-model.md), [quickstart.md](./quickstart.md) и
[contracts/ofz-summary-quality-gates.md](./contracts/ofz-summary-quality-gates.md).

## Phase 2: Task Planning Approach

Tasks are test-first and intentionally narrow:

1. Create representative scenario and quality assertions in Core tests.
2. Add gates for section/sourceCounts/date-only/schema.
3. Add gates for required findings and neutral wording.
4. Add gates for missing-data/no-fake-zero behavior.
5. Run targeted/full validation and update roadmap.

## Verification

```powershell
dotnet restore
dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release --filter "FullyQualifiedName~OfzSummaryQualityGateTests"
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations.
