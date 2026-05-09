# Quickstart: Внешние факторы активности ОФЗ

## Проверка сборки и тестов

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

## Manual smoke

1. Запустить WPF-приложение.
2. Открыть экран `Активность ОФЗ`.
3. Выбрать период с уже загруженными index context и CBR key rates, например
   `25.03.2026` - `09.05.2026`.
4. Нажать `Загрузить` или `Обновить историю`.
5. В summary panel открыть вкладку `Факторы`.
6. Проверить:
   - есть CBR key rate row с source `ЦБ`;
   - есть RGBI/RGBITR rows с source `history` или `snapshot`;
   - missing optional factors отображаются как limitations;
   - тексты findings не содержат рекомендаций.
7. Нажать `Файл` или `Копия` и проверить JSON:
   - `schemaVersion = "1.6"`;
   - есть `externalFactorsContext`;
   - есть `sourceCounts.externalFactorObservations`;
   - missing values не записаны как `0`.

## Expected JSON fragment

```json
{
  "schemaVersion": "1.6",
  "externalFactorsContext": {
    "factorSeries": [
      {
        "code": "cbr_key_rate",
        "source": "CbrKeyRate",
        "availability": "Historical"
      },
      {
        "code": "RGBI",
        "source": "MoexIndex"
      }
    ],
    "links": [],
    "limitations": []
  }
}
```
