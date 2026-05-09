# UI Contract: Cashflow calendar workflows

## Overview

Overview показывает компактный блок `Календарь событий`:

- ближайшие события по активным выпускам;
- события внутри выбранного периода;
- limitation count;
- source/provisional marker.

Empty state:

- если событий нет, показать нейтральный текст о недоступном календаре;
- не показывать пустую таблицу с нулевыми суммами.

## Issue detail

Issue detail показывает таблицу событий выбранного выпуска:

- Date;
- Type;
- Value / ValueRub;
- Percent;
- Face value;
- Source;
- Limitations.

Сортировка: `EventDate`, затем event type priority:
`Coupon`, `Amortization`, `Maturity`, `Offer`, `Buyback`, `CallOption`,
`PutOption`.

## Findings

Cashflow-backed findings отображаются в existing summary findings list.
Drill-down target ведет к issue detail/calendar row или selected event state.

Текст нейтральный:

- допустимо: "активность рядом с купоном";
- нельзя: "купить перед купоном", "продать", "recommend".
