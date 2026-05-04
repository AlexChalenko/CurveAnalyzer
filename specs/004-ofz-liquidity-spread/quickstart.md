# Quickstart: Liquidity & Spread Layer

## Цель

Проверить, что новый слой ликвидности расширяет экран "Активность ОФЗ" и не
ломает уже реализованные сценарии активности.

## Baseline перед реализацией

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
dotnet ef migrations list --project src\CurveAnalyzer.Infrastructure\CurveAnalyzer.Infrastructure.csproj --startup-project src\CurveAnalyzer.Infrastructure\CurveAnalyzer.Infrastructure.csproj --context MoexContext --configuration Release --no-build
```

## Проверка после реализации

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

Targeted tests:

```powershell
dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release
```

## Manual Smoke

1. Запустить supported WPF app.
2. Открыть "Активность ОФЗ".
3. Выбрать диапазон до 90 торговых дней.
4. Нажать "Загрузить".
5. Проверить existing workflows:
   - top anomalies populated;
   - heatmap visible;
   - issue detail updates on selection;
   - segments view visible.
6. Выбрать выпуск с bid/offer data.
7. Проверить liquidity summary:
   - bid/offer/spread displayed;
   - depth displayed only if available;
   - missing fields shown as `n/a`.
8. Открыть weak liquidity list/panel.
9. Проверить, что rows include issue/date/reason/evidence.
10. Переключить type filter и signal scope.
11. Проверить, что liquidity rows and insights update without stale data.

## Accepted Limitations

- Feature не хранит full order book history.
- Snapshot-only fields may be shown only as current context.
- Missing liquidity data does not block existing activity analytics.
- Signals are analytical observations, not investment recommendations.
- На live-проверке 2026-05-04 `history` не вернул `BID`/`OFFER` columns для
  даты 2026-04-30; bid-ask/depth слой MVP должен опираться на current snapshot,
  а historical issue view - на доступные spread-like поля (`ZSPREAD`,
  `ZSPREADATWAPRICE`) и existing turnover/yield history.

## Implementation Anchors

- Core: `src\CurveAnalyzer.Core\Domain\OfzDailyTrade.cs` already has
  `ZSpread` and `ZSpreadAtWeightedAveragePrice`; do not duplicate them.
- Core: new liquidity types belong in
  `src\CurveAnalyzer.Core\Domain\OfzLiquidityViews.cs`; analyzer rules belong
  in `src\CurveAnalyzer.Core\Services\OfzActivityAnalyzer.cs`.
- Application: keep `OfzActivityLoadResult` constructor stable and add
  init-only liquidity/snapshot properties.
- ApiServices: `IssJsonTable` already tolerates missing columns; extend column
  lists and mapping without changing generic parser behavior.
- Infrastructure: use existing manual EF migration style and keep
  snapshot-only rows separate from `OfzDailyTrades`.
- Presentation: do not reintroduce hidden `TabItem` LiveCharts hosts; current
  radio/visibility pattern avoids hidden-canvas sizing issues.

## Implementation Notes

- Historical rows persist nullable bid/offer/spread-like fields when ISS returns
  them; missing columns remain `null`, not `0`.
- Current-day `marketdata` quote fields are stored separately as
  `OfzLiquiditySnapshot` and remain replaceable/provisional; provisional daily
  trade rows keep turnover/price/yield context but do not persist bid/offer
  spread as historical liquidity.
- Issue detail shows current liquidity snapshot context, keeps historical
  value/trades/price/yield charts and adds historical spread when available.
- Weak liquidity rows are derived from liquidity bucket/status and can include
  active records with missing quotes as a separate reason.
- Segment scatter keeps X = duration and Y = yield; fill/size remains activity
  score, while stroke/tooltip adds liquidity context.
- Existing conclusions area now combines activity insights and deterministic
  liquidity/spread insights. Text is evidence-only and avoids recommendation
  language.

## Validation Log

- 2026-05-04: план создан. Implementation validation pending.
- 2026-05-04: checklist `requirements.md` PASS, 16/16 complete.
- 2026-05-04: baseline `dotnet restore` passed.
- 2026-05-04: baseline `dotnet build CurveAnalyzer.sln -c Release` passed,
  0 warnings, 0 errors.
- 2026-05-04: baseline `dotnet test -c Release` passed, 34 tests.
- 2026-05-04: baseline EF migrations list passed; pending migration:
  `20260504134000_AddOfzLoadStateProvisional`.
- 2026-05-04: MOEX ISS live check completed. History request for 2026-04-30
  returned `ZSPREAD`, `ZSPREADATWAPRICE`, `IRICPICLOSE`, `BEICLOSE`,
  `CBRCLOSE`, `BONDTYPE`, `BONDSUBTYPE`; current snapshot request returned
  `BID`, `OFFER`, `SPREAD`, depth totals and `marketdata_yields`.
- 2026-05-04: Core liquidity checkpoint
  `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release`
  passed, 38 tests.
- 2026-05-04: foundational checkpoint `dotnet build CurveAnalyzer.sln -c Release`
  passed, 0 warnings, 0 errors.
- 2026-05-04: foundational checkpoint `dotnet test -c Release` passed, 38 tests.
- 2026-05-04: EF migrations list passed; pending migrations:
  `20260504134000_AddOfzLoadStateProvisional`,
  `20260504150000_AddOfzLiquidityStorage`.
- 2026-05-04: US1 targeted Core tests
  `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release`
  passed, 39 tests.
- 2026-05-04: US1 WPF compile checkpoint
  `dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.csproj -c Release`
  passed, 0 warnings, 0 errors.
- 2026-05-04: US2 targeted Core tests
  `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release`
  passed, 40 tests.
- 2026-05-04: US2 WPF compile checkpoint
  `dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.csproj -c Release`
  passed, 0 warnings, 0 errors.
- 2026-05-04: US3 targeted Core tests
  `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release`
  passed, 40 tests.
- 2026-05-04: US3 WPF compile checkpoint
  `dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.csproj -c Release`
  passed, 0 warnings, 0 errors.
- 2026-05-04: US4 targeted Core tests
  `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release`
  passed, 42 tests.
- 2026-05-04: US4 WPF compile checkpoint
  `dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.csproj -c Release`
  passed, 0 warnings, 0 errors.
- 2026-05-04: final automated validation `dotnet build CurveAnalyzer.sln -c Release`
  passed, 0 warnings, 0 errors.
- 2026-05-04: final automated validation `dotnet test -c Release` passed,
  42 tests.
- 2026-05-04: `git diff --check` passed; only line-ending normalization
  warnings were reported by Git.
- 2026-05-04: Core regression checkpoint after snapshot/spread cleanup
  `dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release --no-restore`
  passed, 43 tests.
- 2026-05-04: ApiServices compile checkpoint after current snapshot mapping
  cleanup `dotnet build src\CurveAnalyzer.ApiServices\CurveAnalyzer.ApiServices.csproj -c Release --no-restore`
  passed, 0 warnings, 0 errors.
- 2026-05-04: 90-day performance check on synthetic 100 issues / 9000 daily
  rows completed in 196 ms for activity metrics, liquidity metrics, heatmap,
  activity index, duration/yield scatter, weak-liquidity ranking and selected
  issue detail.
- 2026-05-04: manual smoke by user on WPF range 2026-03-20..2026-05-04:
  existing activity heatmap/anomaly table still render; weak-liquidity table
  shows current snapshot spread rows instead of historical missing-quote noise;
  issue detail charts render turnover/trades/price/yield and historical
  Z-spread.
- 2026-05-04: selected issue liquidity detail UX accepted by user during
  manual smoke; liquidity summary is visible above charts, missing fields are
  shown as `n/a`, and snapshot spread is labeled as preliminary/current
  context.
- 2026-05-04: final validation after cleanup `dotnet test -c Release` passed,
  43 tests.
- 2026-05-04: final validation after cleanup
  `dotnet build CurveAnalyzer.sln -c Release` passed, 0 warnings, 0 errors.
