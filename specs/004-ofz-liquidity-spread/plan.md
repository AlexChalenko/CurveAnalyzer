# План реализации: Liquidity & Spread Layer

**Branch**: `004-ofz-liquidity-spread` | **Date**: 2026-05-04 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/004-ofz-liquidity-spread/spec.md`

## Summary

Feature расширяет экран "Активность ОФЗ" слоем ликвидности и spread-контекста:
пользователь видит не только всплеск оборота, но и качество торгов выбранного
выпуска: bid-ask spread, доступный bid/offer context, глубину текущих заявок,
Z/G-spread и объяснимые выводы о слабой или улучшающейся ликвидности.

Технический подход: использовать уже созданный read-model активности ОФЗ как
базу и добавить к нему nullable liquidity/spread поля из MOEX ISS. Исторические
поля, доступные в `history`, сохраняются в дневных строках. Snapshot-only поля
глубины и `marketdata_yields` хранятся/показываются отдельно как текущий
контекст, чтобы не притворяться полной историей. Расчеты liquidity score и
spread signals живут в Core/Application, ISS parsing остается в ApiServices,
persistence - в Infrastructure, chart/view state - в Presentation.

## Technical Context

**Language/Version**: C#; repo закреплен на .NET SDK `10.0.203` через
`global.json`. Supported libraries таргетят `net10.0`; WPF app таргетит
`net10.0-windows10.0.19041`.
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting,
EF Core SQLite, HttpClient, MOEX ISS JSON, LiveChartsCore.SkiaSharpView.WPF.
Новых runtime dependencies не планируется.
**Storage**: SQLite в `%LOCALAPPDATA%\CurveAnalyzer\zcyc.db`. База остается
disposable cache: ZCYC, OFZ activity и liquidity/spread данные можно повторно
скачать из MOEX ISS. Схема обновляется EF migration.
**Testing**: `dotnet restore`, `dotnet build CurveAnalyzer.sln -c Release`,
`dotnet test -c Release`; targeted tests для Core liquidity score/signal rules;
ручная smoke-проверка экрана "Активность ОФЗ".
**Target Platform**: Windows desktop WPF, Windows 10 2004 / build 19041 или выше.
**Project Type**: desktop app с layered architecture.
**Performance Goals**: загрузка и расчет liquidity/spread view для диапазона до
90 торговых дней и примерно 100 выпусков не дольше 10 секунд на локально
прогретых данных; отсутствие liquidity fields не должно замедлять существующие
activity workflows.
**Constraints**: ISS liquidity fields могут быть null или snapshot-only;
историческая глубина стакана не загружается; Core/Application не зависят от WPF,
LiveCharts2 или EF Core; валютные ОФЗ не смешиваются с рублевыми без явной
пометки; feature не является инвестиционной рекомендацией.
**Scale/Scope**: расширение одного existing analytics workflow в supported WPF
app; дневная гранулярность; board `TQOB`; без intraday trades, order book
history, корпоративных/муниципальных облигаций, внешних макро-рядов и
рекомендательных моделей.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Единственный поддерживаемый путь приложения**: PASS. Feature расширяет
  только supported `src/` WPF path и существующий экран активности ОФЗ.
- **II. Прагматичный DDD**: PASS. Новые понятия `OfzLiquidityMetric`,
  `OfzLiquiditySnapshot`, `OfzSpreadSignal` отражают реальные доменные
  правила: spread, depth, качество ликвидности, snapshot/current context.
- **III. Направление зависимостей и composition root**: PASS. ISS/SQLite
  остаются в ApiServices/Infrastructure, chart types остаются в Presentation,
  Core содержит только доменные модели и чистые расчеты.
- **IV. Границы async, UI и данных**: PASS. Синхронизация остается
  асинхронной, отменяемой и ожидаемой; schema changes проходят через EF
  migrations; snapshot-only данные явно отделяются от history.
- **V. Проверяемые и обозримые изменения**: PASS. План задает unit tests для
  liquidity calculations, build/test gates и ручную smoke-проверку.
- **VI. Документация сначала на русском**: PASS. Все feature artifacts ведутся
  на русском с сохранением технических идентификаторов.

## Project Structure

### Documentation (this feature)

```text
specs/004-ofz-liquidity-spread/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── iss-liquidity-data-contract.md
│   └── ui-liquidity-workflows.md
└── tasks.md              # создается на /speckit-tasks
```

### Source Code (repository root)

Planned supported scope:

```text
src/
├── CurveAnalyzer.Core/
│   ├── Domain/
│   │   ├── OfzDailyTrade.cs
│   │   ├── OfzActivityViews.cs
│   │   └── OfzLiquidityViews.cs
│   └── Services/
│       └── OfzActivityAnalyzer.cs
├── CurveAnalyzer.Application/
│   ├── Interfaces/
│   │   └── IOfzActivityRepository.cs
│   ├── OfzActivityLoadResult.cs
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
    ├── Converters/
    │   ├── OfzActivityOverviewConverters.cs
    │   └── OfzIssueDetailSeriesConverter.cs
    ├── ViewModels/
    │   └── OfzActivityViewModel.cs
    └── Views/
        └── OfzActivityControl.xaml

tests/
└── CurveAnalyzer.Core.Tests/
    └── Services/
        └── OfzActivityAnalyzerTests.cs
```

Out of scope:

```text
legacy root project
intraday trades/orderbook storage
full order book history
investment advice/recommendation engine
corporate/municipal bond liquidity workflows
external macroeconomic data import
```

**Structure Decision**: сохранить существующую layered architecture и не
создавать отдельный экран. Liquidity & Spread Layer является расширением
`OfzActivityService`/`OfzActivityViewModel` и существующей вкладки "Активность
ОФЗ". Доменные расчеты остаются в Core; ISS parsing - в ApiServices; SQLite
persistence - в Infrastructure; визуализация - в Presentation.

## Phase 0: Research Findings

См. [research.md](./research.md).

Ключевые решения:

- Historical liquidity source: MOEX ISS
  `history/engines/stock/markets/bonds/boards/TQOB/securities.json`.
- Current liquidity snapshot source: MOEX ISS
  `engines/stock/markets/bonds/boards/TQOB/securities.json?iss.only=securities,marketdata,marketdata_yields`.
- Исторические bid/offer/spread-like поля сохраняются в `OfzDailyTrade`.
- Snapshot-only depth fields (`BIDDEPTHT`, `OFFERDEPTHT`, depth totals,
  `marketdata_yields`) отображаются как текущий контекст, а не historical
  series.
- Liquidity score должен быть объяснимым bucket/rank score, а не моделью
  рекомендации.
- Missing spread/depth не равен нулевому spread и должен иметь отдельный
  статус.

## Phase 1: Design Findings

См. [data-model.md](./data-model.md),
[quickstart.md](./quickstart.md),
[contracts/iss-liquidity-data-contract.md](./contracts/iss-liquidity-data-contract.md)
и [contracts/ui-liquidity-workflows.md](./contracts/ui-liquidity-workflows.md).

Целевые data/application contracts:

- `OfzDailyTrade` расширяется историческими liquidity/spread fields:
  `Bid`, `Offer`, `Spread`, `HighBid`, `LowOffer`, `ZSpread`,
  `ZSpreadAtWeightedAveragePrice`, implied fields where available.
- `OfzLiquiditySnapshot` хранит текущий snapshot bid/offer/depth/yields для
  выпуска.
- `OfzLiquidityMetric` рассчитывает состояние ликвидности по выпуску/дате без
  заглядывания в будущее.
- `OfzSpreadSignal` и `OfzLiquidityInsight` дают deterministic explanations
  для wide spread, activity-with-wide-spread, missing quote data и improving
  spread.
- `OfzActivityService` возвращает liquidity profile/detail alongside existing
  activity data.

Целевые UI contracts:

- Существующая детализация выпуска получает блок ликвидности и spread-history
  там, где доступны данные.
- Таблица/панель проблемной ликвидности использует текущие фильтры типа ОФЗ и
  signal scope.
- Scatter `дюрация / доходность` получает цвет/размер по liquidity/spread
  bucket без ломки текущих activity scenarios.
- Выводы явно маркируют snapshot-only и missing-data cases.

## Constitution Check After Design

- **Единственный поддерживаемый путь приложения**: PASS. План изменяет только
  supported `src/` и docs текущей feature.
- **Прагматичный DDD**: PASS. Введены только доменные сущности с реальными
  инвариантами: snapshot-vs-history, missing-vs-zero, spread status.
- **Направление зависимостей**: PASS. Внешние ISS/EF/LiveCharts2 types не
  попадают в Core/Application contracts.
- **Границы async/UI/data**: PASS. Snapshot-only данные отделены от исторических
  rows; сетевые операции остаются за service boundary.
- **Проверяемость**: PASS. Определены automated checks, targeted unit tests и
  manual smoke.
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
dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release
```

Manual smoke:

- Запустить supported WPF app.
- Открыть "Активность ОФЗ".
- Загрузить диапазон до 90 торговых дней с данными.
- Проверить, что старые scenarios работают: top anomalies, heatmap,
  детализация выпуска, сегменты.
- Выбрать выпуск с bid/offer data и проверить блок ликвидности.
- Проверить выпуск без bid/offer data: UI показывает `n/a`/недостаточно
  данных, а не нулевой spread.
- Проверить типовые фильтры ОФЗ и signal scope на liquidity/spread panels.
- Проверить, что текущий день помечается как предварительный, если используется
  snapshot.

## Заметки для /speckit-tasks

- Сначала добавить Core tests для liquidity score/status/signal rules.
- Затем расширить domain models and repository contracts, после этого ISS
  mapping/persistence.
- Не смешивать snapshot-only depth data with daily history rows без явной
  пометки.
- Не вводить новую chart library.
- Не менять existing activity score calculation.
- Stage ignored SDD files explicitly with `git add -f`.
