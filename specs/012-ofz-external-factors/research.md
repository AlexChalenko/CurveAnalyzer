# Research: Внешние факторы активности ОФЗ

## Решение 1: Источники V1

**Decision**: V1 использует только данные, которые уже участвуют в supported OFZ
workflow:

- `CbrKeyRates` из existing CBR key-rate cache/service;
- `IndexContextDays` и `IndexSegments` из existing MOEX index context;
- `SpecialMetrics` как OFZ-derived факторы (`ImpliedCbrRate`,
  `ImpliedFloatingRateSpread`, `ImpliedInflation`).

**Rationale**: эти ряды уже загружаются, тестируются и имеют modelled
limitations. Это дает внешний фон без расширения сетевого слоя и без новой
модели качества данных.

**Alternatives considered**:

- Добавить новые FX/commodity loaders сразу. Отклонено: это расширяет feature
  за пределы OFZ workflow и требует отдельного source quality model.
- Использовать только index context. Отклонено: ключевая ставка уже доступна и
  является более важным policy factor для ОФЗ-ПК.

## Решение 2: No-lookahead alignment

**Decision**: policy/macro факторы сопоставляются по latest-on-or-before, а
market index факторы - по trade-date observation и existing previous-date
change.

**Rationale**: ключевая ставка не публикуется каждый торговый день, но ее
значение действует до следующего изменения. Индексные ряды имеют торговую дату
и собственный previous-date delta.

**Alternatives considered**:

- Интерполяция между датами. Отклонено: создает синтетические значения и может
  выглядеть как прогноз.
- Поиск ближайшей даты вперед/назад. Отклонено: forward lookup нарушает
  no-lookahead для findings.

## Решение 3: Missing data policy

**Decision**: каждый отсутствующий фактор отражается limitation и availability
`Missing`; missing values не заменяются `0`.

**Rationale**: проект уже придерживается этого правила для spread, liquidity,
special metrics, cashflow и seasonality. Внешние факторы особенно чувствительны
к неверной интерпретации нуля.

## Решение 4: Finding wording

**Decision**: external factor findings используют нейтральные формулировки:
"на фоне", "совпало с", "без заметного движения". Запрещены causality,
forecast и recommendation language.

**Rationale**: feature добавляет контекст, а не торговую модель. Это снижает
риск ошибочного вывода и соответствует roadmap.

## Решение 5: UI placement

**Decision**: добавить вкладку `Факторы` в existing summary panel.

**Rationale**: факторный слой связан с другими summary layers и не должен
становиться отдельным экраном или dashboard.
