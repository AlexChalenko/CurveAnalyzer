# Contract: UI seasonality workflows

## Overview tab

В `OfzActivityOverviewControl` добавить вкладку `Сезонность` рядом с existing
summary tabs.

Вкладка показывает:

- статус: число weekday buckets, month buckets, findings и limitations;
- таблицу weekday profile;
- таблицу month profile;
- таблицу seasonality findings;
- limitations.

## Formatting

- День недели показывать на русском: `Пн`, `Вт`, `Ср`, `Чт`, `Пт`.
- Month label показывать как `Янв`, `Фев`, ... или `01`, `02`, если short
  month formatter недоступен.
- Обороты форматировать в млн RUB, как в existing activity UI.
- Missing values показывать `n/a`.
- Ratio показывать как `1.50x`.

## Empty state

- Если истории недостаточно, не показывать пустую "успешную" таблицу.
- Показывать limitation text и нулевой/empty profile только с явным статусом.

## Drill-down

V1 не добавляет отдельный navigation target. Finding может ссылаться на
`SeasonalityDay` через existing selected day context.
