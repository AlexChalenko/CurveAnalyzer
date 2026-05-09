# Research: Сезонность активности ОФЗ

## Источник данных

Decision: использовать existing `OfzActivityMetric`, которые уже проходят
through current activity workflow.

Rationale:

- Данные уже загружаются и фильтруются по active issues/coupon type.
- Activity metrics содержат `TradeDate`, `SecId`, `Value`, `NumTrades`,
  `ActivityScore`, `YieldMove`, `Status`.
- Нет необходимости добавлять новый MOEX endpoint или cache table.

Alternatives considered:

- Загружать отдельные MOEX aggregates по календарным периодам: отклонено,
  потому что это расширяет data layer и может не совпасть с выбранным набором
  выпусков.
- Использовать только market-level total turnover: отклонено, потому что
  потеряется связь с coupon type filter и active issue set.

## Granularity

Decision: observation grain = один торговый день по выбранному набору активных
выпусков.

Rationale:

- UI уже работает с daily heatmap и breadth days.
- Daily aggregate позволяет строить weekday/month buckets без внутридневных
  данных.
- Для sparse issue-level data дневной aggregate устойчивее, чем отдельные
  issuer rows.

## Buckets

Decision: V1 поддерживает `Weekday` и `Month`.

Rationale:

- Эти buckets понятны пользователю и не требуют внешнего календаря.
- Roadmap упоминал сезонность по дням недели и месяцам.
- Налоговые периоды и конец года требуют отдельной модели календаря и должны
  быть отдельной feature или расширением.

## Baseline

Decision: findings сравнивают insight observation только с предыдущими
наблюдениями того же bucket.

Rationale:

- Это сохраняет правило no future leakage.
- Поведение согласуется с existing activity score, где выводы не смотрят в
  будущие записи.

Минимальная база:

- `Weekday`: 4 предыдущих observation rows.
- `Month`: 3 предыдущих observation rows.

Если база меньше, finding не создается, а limitation объясняет слабую историю.

## Metrics

Decision: profile buckets включают:

- `ObservationCount`
- `ActiveDayCount`
- `MedianTotalValue`
- `AverageTotalValue`
- `MedianNumTrades`
- `MedianActiveIssueCount`
- `UpDayShare`
- `DownDayShare`

Rationale:

- Turnover и trades отражают activity.
- Active issue count помогает отличать широкий день от одного шумного выпуска.
- Yield direction shares полезны, но не превращают сезонность в прогноз.

## Thresholds

Decision: default thresholds:

- high activity: current value ratio `>= 1.5x` median baseline;
- low activity: current value ratio `<= 0.67x` median baseline.

Rationale:

- Thresholds достаточно грубые, чтобы не делать вывод из мелкого шума.
- Ratio легко объяснить в UI и JSON.

## UI

Decision: добавить вкладку "Сезонность" в существующий summary tab control.

Rationale:

- Пользователь уже ожидает layers в "Сводка".
- Отдельный dashboard увеличил бы навигационную сложность без новой workflow.

## Open risks

- Короткие периоды будут чаще давать limitations, а не findings.
- Month profile требует длиннее истории, чем weekday profile.
- Сезонность может быть искажена крупными аукционами/новостями; feature должна
  показывать контекст, а не объяснять причину.
