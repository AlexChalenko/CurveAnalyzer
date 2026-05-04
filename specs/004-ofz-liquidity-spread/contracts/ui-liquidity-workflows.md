# Contract: UI workflows для Liquidity & Spread Layer

## Scope

Контракт описывает ожидаемое поведение WPF workflow "Активность ОФЗ" после
добавления слоя ликвидности и spread.

## Existing Workflow Compatibility

- Existing top anomalies, heatmap, issue detail and segments MUST keep working
  when liquidity fields are absent.
- Existing filters remain authoritative:
  - date range;
  - type filter;
  - insights period;
  - signal scope `Все дни` / `Последний день`.
- Liquidity/spread views must honor the same filters unless explicitly labeled
  as current snapshot.

## Issue Detail

When an issue is selected:

- Header shows existing issue identity and type marker.
- Detail shows liquidity summary if quote data exists:
  - bid;
  - offer;
  - spread;
  - depth totals if available;
  - latest liquidity bucket/status.
- Historical chart shows spread over selected dates only for dates where
  historical spread-like data exists.
- Missing liquidity fields are shown as `n/a` or "данных недостаточно", never
  as zero spread.
- Current snapshot block must be labeled as current/provisional context if it
  does not correspond to final history.

## Weak Liquidity List

The view provides a ranked list of issues with weak/problem liquidity.

Each row should include:

- date or scope;
- issue short name;
- SECID;
- OFZ type;
- turnover;
- deal count;
- spread;
- depth context if available;
- liquidity status/bucket;
- reason text.

Sorting defaults:

1. problem/missing-on-active-day severity;
2. liquidity score descending;
3. turnover descending;
4. issue code ascending.

## Duration/Yield Scatter

The existing scatter remains duration/yield based.

Liquidity extension:

- point color or outline maps to liquidity/spread bucket;
- point size can continue to represent activity/turnover;
- tooltip includes spread and liquidity status.

When liquidity data is unavailable:

- point remains visible if existing activity/yield/duration data is valid;
- tooltip says liquidity data is unavailable.

## Insights

Liquidity/spread insights are deterministic and evidence-based.

Allowed insight examples:

- "Широкий spread при высокой активности";
- "Активный выпуск без котировок";
- "Улучшение spread на фоне роста оборота";
- "Дисбаланс глубины спроса/предложения";
- "Z-spread context differs from nearby issues".

Forbidden:

- buy/sell/hold language;
- target price;
- portfolio allocation recommendation;
- certainty claims about future yield movement.

## Empty and Error States

- If no liquidity data exists for the selected range, show a compact empty
  state and keep existing activity charts visible.
- If snapshot load fails, show warning and keep historical activity data.
- If history load succeeds but liquidity columns are null, show missing data
  state without retry loop.

## Manual Smoke Scenarios

1. Select a range where activity data exists and verify existing activity UI.
2. Select a liquid ОФЗ-ПД and verify spread/depth summary.
3. Select an issue with no bid/offer and verify missing data display.
4. Switch type filter between all/ОФЗ-ПД/ОФЗ-ПК/ОФЗ-ИН and verify liquidity
   rows update.
5. Switch signal scope and verify weak-liquidity list and insights update.
