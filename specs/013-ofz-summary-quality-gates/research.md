# Research: Quality gates для OFZ summary/export

## Решение 1: automated gates не зависят от `.tmp` export files

**Decision**: использовать compact synthetic input внутри test project.

**Rationale**:

- `.tmp/*.json` является runtime/manual smoke артефактом и игнорируется git.
- Большие реальные экспорты делают тесты шумными, хрупкими и неудобными для
  code review.
- Synthetic input можно точно настроить под coverage всех sections и edge cases.

**Rejected Alternatives**:

- Commit реального `.tmp/ofz-summary-*.json`: отвергнуто из-за размера,
  приватности данных и нестабильности.
- WPF automation для кнопки `Файл`: отвергнуто, потому что feature защищает
  Core contract, а UI smoke остается ручной проверкой.

## Решение 2: gates живут в `CurveAnalyzer.Core.Tests`

**Decision**: добавить `OfzSummaryQualityGateTests` в
`tests/CurveAnalyzer.Core.Tests/Services/`.

**Rationale**:

- `OfzMarketSummaryBuilder` находится в Core.
- Existing summary contract tests уже живут в Core tests.
- No new dependencies and no layer direction changes.

**Rejected Alternatives**:

- Production `QualityGate` service: отвергнуто, пока нет runtime use case.
- Separate CLI tool: отвергнуто как лишняя инфраструктура для первого
  quality-gate слоя.

## Решение 3: assertions проверяют contract surfaces, не точные ранги всех выводов

**Decision**: gate проверяет наличие обязательных секций, sourceCounts,
date-only serialization, required finding kinds и no recommendation wording.

**Rationale**:

- Exact rank всех findings меняется при добавлении новых analytics layers.
- Quality gate должен ловить contract-breaking regressions, а не блокировать
  легитимное улучшение ranking.

**Rejected Alternatives**:

- Snapshot-test всего JSON: отвергнуто из-за высокой хрупкости и большого diff.
- Проверка только schemaVersion: недостаточно, не ловит потерю секций.

## Решение 4: missing-data gate остается отдельным focused scenario

**Decision**: добавить отдельный сценарий для missing special/external values,
а не перегружать full representative scenario.

**Rationale**:

- Full scenario должен быть читаемым и stable.
- Missing-data policy важна сама по себе и лучше диагностируется отдельным
  тестом.

**Rejected Alternatives**:

- Смешать все edge cases в один большой test: ухудшает диагностику.
