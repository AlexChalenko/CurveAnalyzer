# Quickstart: Сезонность активности ОФЗ

## Цель проверки

Проверить, что сезонность:

- строится по existing activity metrics без новых загрузчиков;
- уважает фильтр типа ОФЗ;
- показывает weekday/month profiles;
- не создает выводы при недостаточной базе;
- попадает в structured JSON.

## Автоматическая проверка

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

Targeted:

```powershell
dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release --filter "FullyQualifiedName~OfzSeasonality"
```

## Manual smoke

1. Запустить WPF app.
2. Открыть экран `Активность ОФЗ`.
3. Выбрать период минимум 3-6 месяцев.
4. Нажать `Загрузить`.
5. Открыть `Сводка -> Сезонность`.
6. Проверить weekday profile: buckets, observation counts, median turnover.
7. Проверить month profile и limitations при малой истории.
8. Переключить `Тип`: `Все -> ОФЗ-ПД -> ОФЗ-ПК`.
9. Проверить, что profiles меняются по выбранному набору.
10. Сохранить JSON и проверить `seasonalityContext`.

## Expected JSON markers

- `schemaVersion = 1.5`.
- `seasonalityContext.weekdayBuckets`.
- `seasonalityContext.monthBuckets`.
- `seasonalityContext.findings`.
- `sourceCounts.seasonalityObservations`.

## Negative checks

- В findings нет слов `покупать`, `продавать`, `лучше купить`,
  `рекомендация`.
- Missing seasonal values отображаются как `n/a`, не как `0`.
- При короткой истории есть limitation `InsufficientSeasonalityHistory`.
