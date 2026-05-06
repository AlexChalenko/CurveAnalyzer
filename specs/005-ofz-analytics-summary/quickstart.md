# Quickstart: OFZ Analytics Summary

## Цель проверки

Проверить, что новый summary layer:

- строит короткую сводку по выбранному периоду и типу ОФЗ;
- показывает только explainable findings с числовым evidence;
- не превращает missing spread/bid/offer в нули;
- явно помечает snapshot-only и provisional данные;
- не ломает существующие activity, liquidity, heatmap и detail графики.

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

Точечный тестовый проект:

```powershell
dotnet test tests/CurveAnalyzer.Core.Tests/CurveAnalyzer.Core.Tests.csproj -c Release
```

## Ручной сценарий

1. Запустить WPF-приложение.
2. Открыть вкладку `Активность ОФЗ`.
3. Выбрать диапазон с уже прогретыми данными, например последние 30-90
   торговых дней.
4. Нажать `Загрузить`.
5. В режиме `Обзор` проверить, что в блоке `Выводы` видны 3-7 summary findings
   с числами: даты, оборот, сделки, score, spread/liquidity или yield move.
6. В режиме `Обзор` проверить, что карта активности обновляется вместе с
   выбранным периодом и типом ОФЗ.
7. Переключить режим `Сигналы` и проверить таблицы `Всплески активности` и
   `Слабая ликвидность`: строки должны соответствовать текущим фильтрам.
8. Переключить режим `Детализация`, выбрать `Выпуск`, затем `Сегменты` и
   убедиться, что графики создаются только в этом режиме и обновляются при
   выборе строки или ячейки heatmap.
9. Переключить `Тип`: `Все`, `ОФЗ-ПД`, `ОФЗ-ПК`, `ОФЗ-ИН`, `ОФЗ-АД`,
   `Валютная`.
10. Убедиться, что summary, heatmap, таблицы и detail графики обновляются без
   stale-выводов от предыдущего фильтра.
11. Выбрать вывод, связанный с выпуском или датой, и проверить, что в режиме
   `Детализация` обновляется выпуск или строка context для heatmap.
12. Проверить строки с snapshot-only/current-day данными: summary должен явно
   писать ограничение, а не показывать `0` spread.
13. Нажать кнопку `JSON` в блоке `Выводы` и проверить, что structured summary
    копируется в буфер обмена с enum-полями как строками.

## Проверка structured contract

В unit-тестах Core нужно собрать `MarketSummary` из искусственных activity и
liquidity данных и проверить:

- каждый `SummaryFinding` имеет `Kind`, `Priority`, `Scope`, `Text` и
  `Evidence`;
- findings отсортированы по priority;
- no-data / missing-spread cases дают `DataLimitation`;
- текст не содержит запрещенных слов `buy`, `sell`, `купить`, `продать`,
  `держать`, `рекоменд`;
- JSON-compatible модель соответствует
  `contracts/market-summary.schema.json` по обязательным полям.

## Условия приемки v1

- Summary строится без новой миграции БД.
- `OfzActivityViewModel` не делает собственных расчетов ranking/evidence,
  кроме применения текущих фильтров и binding.
- Core analyzer покрыт focused unit tests.
- `dotnet build CurveAnalyzer.sln -c Release` проходит.
- Если full test suite существует, `dotnet test -c Release` проходит.
