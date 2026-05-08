# Quickstart: Индексный контекст ОФЗ

## Цель проверки

Проверить, что index context:

- показывает `RGBI` и `RGBITR` как фон рынка ОФЗ;
- сопоставляет индексные значения с датами activity/breadth;
- считает daily change только от предыдущей торговой точки;
- показывает duration-сегменты, если они доступны;
- не превращает missing data в нули;
- не ломает существующие режимы `Обзор`, `Сигналы`, `Детализация`.

## Команды

Восстановление и сборка:

```powershell
dotnet restore
dotnet build CurveAnalyzer.sln -c Release
```

Тесты:

```powershell
dotnet test -c Release
```

Точечные test projects:

```powershell
dotnet test tests\CurveAnalyzer.Core.Tests\CurveAnalyzer.Core.Tests.csproj -c Release
dotnet test tests\CurveAnalyzer.Infrastructure.Tests\CurveAnalyzer.Infrastructure.Tests.csproj -c Release
```

EF migration list:

```powershell
dotnet ef migrations list --project src\CurveAnalyzer.Infrastructure\CurveAnalyzer.Infrastructure.csproj --startup-project src\CurveAnalyzer.Infrastructure\CurveAnalyzer.Infrastructure.csproj --context MoexContext --configuration Release --no-build
```

## Проверка источника MOEX ISS

Проверить, что history endpoint отдает поля для required series:

```powershell
Invoke-RestMethod "https://iss.moex.com/iss/history/engines/stock/markets/index/securities/RGBI.json?from=2026-03-24&till=2026-05-08&iss.meta=off"
Invoke-RestMethod "https://iss.moex.com/iss/history/engines/stock/markets/index/securities/RGBITR.json?from=2026-03-24&till=2026-05-08&iss.meta=off"
```

Ожидаемо:

- block `history`;
- rows for trading dates;
- columns include `SECID`, `TRADEDATE`, `CLOSE`, `VALUE`, `DURATION`,
  `YIELD`.

## Ручной сценарий

1. Запустить WPF-приложение.
2. Открыть `Активность ОФЗ`.
3. Выбрать период с прогретыми данными, например `24.03.2026` - `08.05.2026`.
4. Выбрать `Тип = Все`, `Сигналы = Все дни`, `Выводы = 7 дней`.
5. Нажать `Загрузить`.
6. В режиме `Обзор` проверить, что виден index context для `RGBI` и `RGBITR`.
7. Проверить, что latest index values показывают дату, close/current value,
   daily change, yield и duration, если поля доступны.
8. Выбрать день с сильным activity score и проверить, что выводы показывают,
   было ли движение индекса в тот же день.
9. Выбрать день без предыдущей index point: daily change должен быть `n/a`, а
   не `0`.
10. Переключить тип ОФЗ (`ОФЗ-ПД`, `ОФЗ-ПК`, `ОФЗ-ИН`) и проверить, что
    индексный фон остается общерыночным context, а не пересчитывается как
    фильтрованный набор выпусков.
11. Проверить duration-segment context: доступные сегменты показываются,
    отсутствующие сегменты перечислены в limitations.
12. Проверить current-day/snapshot/provisional данные: они должны быть явно
    помечены как предварительные.
13. Открыть structured JSON export и проверить, что index context fields
    доступны для внешнего потребителя.
14. Переключиться между `Обзор`, `Сигналы`, `Детализация` и убедиться, что
    heatmap/table/detail charts не остаются со stale значениями.

## Проверка missing-vs-zero

В Core unit tests собрать index points:

- `2026-04-01 RGBI close = 120.00`
- `2026-04-02 RGBI close = null`
- `2026-04-03 RGBI close = 121.00`

Ожидаемо:

- `2026-04-01` -> daily change `n/a`, limitation `MissingPreviousPoint`;
- `2026-04-02` -> close `n/a`, daily change `n/a`, limitation `MissingField`;
- `2026-04-03` -> daily change считается от предыдущей валидной точки
  `2026-04-01`, потому что `2026-04-02` не имеет `Close`.

## Условия приемки v1

- `RGBI` и `RGBITR` загружаются, сохраняются и доступны после перезапуска.
- Duration-segment series загружаются частично без падения всего блока.
- `OfzIndexContextBuilder` не forward-fill missing values.
- `OfzMarketSummaryBuilder` добавляет index-backed findings без recommendation
  language.
- `OfzActivityViewModel` применяет existing period/signal controls и не
  дублирует доменные расчеты.
- `dotnet build CurveAnalyzer.sln -c Release` проходит.
- `dotnet test -c Release` проходит.
