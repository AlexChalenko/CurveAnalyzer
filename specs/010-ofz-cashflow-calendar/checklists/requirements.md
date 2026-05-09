# Requirements Checklist: Календарь денежных потоков ОФЗ

**Feature**: `010-ofz-cashflow-calendar`
**Date**: 2026-05-08

## Clarity

- [x] User stories описывают ценность для пользователя.
- [x] Acceptance scenarios проверяемы без знания реализации.
- [x] Edge cases включают missing data, fallback и duplicate-date events.
- [x] Requirements отделяют календарный контекст от инвестиционных рекомендаций.

## Data Quality

- [x] Primary source указан явно.
- [x] Missing numeric fields не трактуются как нули.
- [x] Source/provisional statuses указаны.
- [x] Empty source blocks описаны как limitations.

## Scope

- [x] V1 ограничен existing WPF activity workflow.
- [x] Нет прогнозов, налогового календаря и нового dashboard.
- [x] Existing activity/liquidity/breadth/index workflows сохраняются.

## Testability

- [x] Есть Core/parser/persistence test expectations.
- [x] Есть build/test commands.
- [x] Есть ручной сценарий UI/JSON проверки.
