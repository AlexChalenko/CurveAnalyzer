# Quickstart: OFZ Market Breadth

## Цель проверки

Проверить, что market breadth:

- показывает, насколько движение доходностей было широким;
- считает концентрацию оборота top-5/top-10;
- показывает вклад типов ОФЗ;
- не использует будущие даты для классификации direction;
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

Точечный Core test project:

```powershell
dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release
```

## Ручной сценарий

1. Запустить WPF-приложение.
2. Открыть `Активность ОФЗ`.
3. Выбрать период с прогретыми данными, например 30-90 торговых дней.
4. Выбрать `Тип = Все`, `Сигналы = Все дни`, `Выводы = 7 дней`.
5. Нажать `Загрузить`.
6. В режиме `Обзор` проверить, что выводы включают breadth facts:
   direction breadth, активные выпуски, concentration или type share.
7. Проверить, что для каждого breadth day visible counts не смешивают
   `not comparable` с `unchanged`.
8. Переключить тип ОФЗ (`ОФЗ-ПД`, `ОФЗ-ПК`, `ОФЗ-ИН`, `ОФЗ-АД`,
   `Валютная`) и проверить, что breadth facts пересчитываются только по
   текущему типу.
9. Выбрать день с breadth сигналом и открыть детализацию: должны быть видны
   top contributors и ограничения данных.
10. Проверить current-day/snapshot/provisional данные: они должны быть явно
    помечены как предварительные.
11. Открыть structured JSON export и проверить, что breadth fields доступны для
    внешнего потребителя.
12. Переключиться между `Обзор`, `Сигналы`, `Детализация` и убедиться, что
    existing heatmap/table/detail charts не остаются со stale значениями.

## Проверка no look-ahead

В Core unit tests собрать ряд по одному выпуску:

- `2026-04-01 yield = 13.00`
- `2026-04-02 yield = 13.10`
- `2026-04-03 yield = 12.90`

Ожидаемо:

- `2026-04-01` -> `NotComparable`
- `2026-04-02` -> `Up`
- `2026-04-03` -> `Down`

Удаление `2026-04-03` не должно менять классификацию `2026-04-02`.

## Условия приемки v1

- Breadth строится без новой миграции БД.
- `OfzMarketSummaryBuilder` не использует будущие записи для direction.
- `OfzActivityViewModel` применяет существующие фильтры и не дублирует
  доменные расчеты.
- Core tests покрывают direction, concentration, type share, limitations,
  structured JSON и no-recommendation language.
- `dotnet build CurveAnalyzer.sln -c Release` проходит.
- `dotnet test -c Release` проходит.
