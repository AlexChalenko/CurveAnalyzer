# CurveAnalyzer

Поддерживаемый путь приложения: `src/CurveAnalyzer.Presentation.WPF`.

## Проверка

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
```

Если нужно проверить только WPF-приложение во время миграции:

```powershell
dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.csproj -c Release
```

## Архитектура

- `src/CurveAnalyzer.Core` - доменные типы и чистые расчеты.
- `src/CurveAnalyzer.Application` - application use cases и interfaces.
- `src/CurveAnalyzer.ApiServices` - доступ к MOEX ISS.
- `src/CurveAnalyzer.Infrastructure` - SQLite/EF Core persistence.
- `src/CurveAnalyzer.Presentation.WPF` - WPF composition root, ViewModels и Views.
- `tests/CurveAnalyzer.Core.Tests` - focused tests для доменных расчетов.

Legacy root WPF-проект удаляется в рамках `specs/001-remove-legacy-project`.
