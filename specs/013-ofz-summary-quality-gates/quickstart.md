# Quickstart: Quality gates для OFZ summary/export

## Targeted validation

```powershell
dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release --filter "FullyQualifiedName~OfzSummaryQualityGateTests"
```

Expected:

- all `OfzSummaryQualityGateTests` pass;
- output does not require WPF launch, network calls or `.tmp` JSON files.

## Full validation

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

## Manual smoke after 012 follow-up

1. Запустить WPF-приложение.
2. Открыть `Активность ОФЗ`.
3. Выбрать период `26.03.2026` - `10.05.2026` или аналогичный, где есть index
   context и external factor links.
4. Сохранить JSON через `Файл`.
5. Проверить:
   - `schemaVersion = "1.6"`;
   - `externalFactorsContext.links` не пустой;
   - top-level `findings` содержит `ExternalFactorActivity`;
   - source counts для external factors положительные;
   - missing values не появились как `0`.

Manual smoke не является обязательным для CI, но полезен после изменений UI или
экспортной кнопки.
