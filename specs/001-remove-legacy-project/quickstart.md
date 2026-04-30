# Quickstart: проверка migration feature

## Предусловия

- Windows с установленным .NET 10 SDK. На момент планирования рабочая машина
  имеет SDK `10.0.203`; после реализации `global.json` должен закреплять
  совместимую .NET 10 SDK policy.
- Рабочая ветка: `001-remove-legacy-project`.
- В рабочем дереве могут быть существующие изменения; перед implementation
  tasks нужно staging делать точечно.

## Baseline до реализации

```powershell
dotnet --info
dotnet restore
dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln -c Release
dotnet list src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.sln package --outdated
```

Ожидаемо до удаления legacy: полный root build может падать, потому что
`CurveAnalyzer.sln` все еще включает старый `CurveAnalyzer.csproj`.

```powershell
dotnet build CurveAnalyzer.sln -c Release
```

## Target validation после реализации

```powershell
dotnet --version
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
dotnet test -c Release
dotnet build src\CurveAnalyzer.Presentation.WPF\CurveAnalyzer.Presentation.WPF.csproj -c Release
dotnet list CurveAnalyzer.sln package --outdated
dotnet list CurveAnalyzer.sln package --vulnerable
dotnet list CurveAnalyzer.sln package --deprecated
```

Ожидаемое target состояние после реализации:

- supported library projects target `net10.0`;
- supported WPF project target `net10.0-windows`;
- package versions находятся в root `Directory.Packages.props`;
- project-level `PackageReference` не содержит `Version`, кроме явно
  документированных исключений;
- `dotnet list ... package --outdated` не показывает обновляемых packages для
  supported projects, кроме зафиксированного chart compatibility exception, если
  он принят в плане.
- `dotnet list ... package --vulnerable` и `--deprecated` не показывают issues.
- `LiveCharts.Wpf 0.9.7` остается documented compatibility exception: сборка
  проходит на `net10.0-windows`, но restore/build выводит `NU1701`.

## Manual smoke

1. Запустить поддерживаемое WPF-приложение из root `CurveAnalyzer.sln`.
2. Проверить, что стартует новый `CurveAnalyzer.Presentation.WPF`.
3. Дождаться завершения sync/progress без UI freeze.
4. Открыть `Yield Curve` и построить кривую по доступной дате.
5. Открыть `Rate Change` и выбрать period.
6. Открыть `Spread Change`, выбрать два разных period и увидеть spread chart.

## Что считать провалом

- Root solution все еще ссылается на старый `CurveAnalyzer.csproj`.
- Supported projects все еще target `net8.0` или `net9.0`.
- NuGet versions остаются размазанными по `.csproj` без `Directory.Packages.props`.
- Supported project требует `..\..\..\LiveCharts2\...dll`.
- Supported build требует checked-in `zcyc.db`.
- Application project зависит от WPF, chart library или `CommunityToolkit.Mvvm.Messaging`.
- Domain calculations остаются только в ViewModels/controls без tests.
