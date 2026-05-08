# UI Contract: Индексный контекст в экране "Активность ОФЗ"

## Overview

В режиме `Обзор` пользователь видит компактный блок индексного фона:

- latest `RGBI` close and daily change;
- latest `RGBITR` close and daily change;
- index yield and duration when available;
- provisional marker for current-day/snapshot data;
- data limitations for missing required series or missing previous point.

The block must not appear as an investment recommendation. Labels should use
neutral wording: "индексный фон", "движение индекса", "данные неполные".

## Findings

Index-backed findings appear in the existing summary findings list.

Finding examples:

- "Активный день совпал с движением RGBI: ..."
- "Всплеск выпуска был локальным относительно спокойного RGBI: ..."
- "Длинный segment moved stronger than whole-market index: ..."
- "Индексный фон неполный: нет предыдущего значения для ..."

Rules:

- Findings include numeric evidence.
- Findings link to selected day/series drill-down where possible.
- Missing values are rendered as `n/a`.
- Current-day findings include preliminary marker.

## Charts

V1 may implement either:

- a compact index-context chart in overview; or
- a background/overlay line near existing activity index chart.

The chosen visualization must preserve existing layout:

- `Обзор`, `Сигналы`, `Детализация` must remain usable;
- existing heatmap, anomaly table, summary findings and issue detail must not
  shift into unreadable states;
- empty index context must show a concise status, not an empty chart area.

## Duration Segments

Duration segment context is shown only for available series. Missing segment
series are listed in limitations.

Suggested display:

- bucket label;
- price series daily/period change;
- total-return series daily/period change;
- latest yield;
- latest duration;
- status.

## Drill-down

When a finding or day is selected, user can inspect:

- trade date;
- index code and name;
- close/current value;
- previous date and daily change;
- yield;
- duration;
- source status: history or snapshot;
- limitations.

The drill-down must be reachable without opening database files or raw JSON.

## Regression constraints

- Existing type filter affects OFZ activity/breadth/special analytics, but
  index context remains market background.
- Existing signal scope and insight period affect which days generate
  index-backed findings.
- Existing `n/a` formatting rules apply to index values.
- No visible `0` should appear for absent index/yield/duration/change.
