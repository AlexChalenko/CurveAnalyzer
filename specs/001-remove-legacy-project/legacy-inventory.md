# Legacy inventory: удаление старого root-приложения

Этот файл фиксирует evidence перед удалением legacy root project. Решения здесь
являются gate для удаления `CurveAnalyzer.csproj`, root XAML/code-behind и
legacy folders.

| Workflow | Legacy evidence | Supported evidence | Статус | Решение перед удалением |
|----------|-----------------|--------------------|--------|--------------------------|
| Yield curve by date | `Charts/DailyCurveChart.cs`, `ViewModel/DailyChartViewModel.cs`, `View/DailyChart.xaml`, `Data/DataManager.cs` | `src/CurveAnalyzer.Presentation.WPF/ViewModels/YieldCurveViewModel.cs`, `src/CurveAnalyzer.Presentation.WPF/Views/YieldCurveControl.xaml.cs`, `src/CurveAnalyzer.Application/DataSyncService.cs` | Частично перенесено | Основной сценарий date -> history fallback -> online fallback сохраняется. Duplicate guard для повторной даты и date-range initialization считаются supported behavior polish и проверяются в manual smoke; визуальная разница OxyPlot spline vs LiveCharts line не блокирует удаление legacy. |
| Rate history by period | `Charts/WeeklyRateDynamicChart.cs`, `ViewModel/WeeklyChartViewModel.cs`, `View/WeeklyChart.xaml`, `Tools/MyExtensions.cs` | `src/CurveAnalyzer.Presentation.WPF/ViewModels/RateChartViewModel.cs`, `src/CurveAnalyzer.Presentation.WPF/Views/RateChartControl.xaml.cs`, `src/CurveAnalyzer.Application/Tools/MyExtensions.cs` | Частично перенесено | Weekly OHLC переносится как supported behavior. Legacy ROC indicator через `TALib.NETCore` и отдельную ось `Y2` объявлен out of scope для этой migration feature, потому что это дополнительная chart capability и старый root build уже падал на ROC-related code. |
| Spread between periods | `Charts/DailySpreadChart.cs`, `ViewModel/SpreadChartViewModel.cs`, `View/SpreadChart.xaml`, `Data/Periods.cs` | `src/CurveAnalyzer.Presentation.WPF/ViewModels/SpreadChartViewModel.cs`, `src/CurveAnalyzer.Presentation.WPF/Views/SpreadChartControl.xaml.cs`, `src/CurveAnalyzer.Presentation.WPF/Data/Periods.cs` | Перенесено с риском | Формула совпадает: join по `Tradedate`, значение `Period2 - Period1`. Async update path в supported ViewModel должен быть проверен и при необходимости исправлен до финальной smoke-проверки. |
| Startup sync | `App.xaml.cs`, `MainWindow.xaml.cs`, `ViewModel/MainViewModel.cs`, `Data/DataManager.cs`, `DataProviders/OnlineDataProvider.cs`, `DataProviders/HistoryDataProvider.cs`, `CurveAnalyzer.csproj` | `src/CurveAnalyzer.Presentation.WPF/App.xaml.cs`, `src/CurveAnalyzer.Presentation.WPF/Views/MainWindow.xaml.cs`, `src/CurveAnalyzer.Presentation.WPF/ViewModels/MainViewModel.cs`, `src/CurveAnalyzer.Application/DataSyncService.cs`, `src/CurveAnalyzer.ApiServices/OnlineDataService.cs`, `src/CurveAnalyzer.Infrastructure/Repositories/ZcycRepository.cs`, `src/CurveAnalyzer.Infrastructure/MoexContext.cs` | Частично перенесено | Sync missing historical dates сохраняется. Text status, global unhandled exception dialog и MOEX empty-result fallback считаются usability/error-handling improvements; они не блокируют удаление legacy, но network/storage errors не должны становиться silent failures. `EnsureCreated()` и connection string исправляются отдельными задачами. |

## Закрытие legacy-only behavior gate

- ROC-индикатор rate history: documented out of scope для этой migration feature.
- Duplicate guard/date-range отличия yield curve: не блокируют удаление legacy, проверяются smoke-сценарием.
- Spread async update path: должен быть проверен при manual smoke; при подтвержденной ошибке исправляется до завершения US4.
- Text startup status/global exception dialog: documented out of scope как UX/error-handling follow-up.
- Online MOEX error fallback: не должен приводить к silent failure; конкретная стратегия ошибки остается за application boundary cleanup.
