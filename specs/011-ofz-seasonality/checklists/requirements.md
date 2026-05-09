# Requirements Checklist: Сезонность активности ОФЗ

**Feature**: `011-ofz-seasonality`
**Date**: 2026-05-09

## Requirements Quality

- [x] Нет placeholders или `NEEDS CLARIFICATION`.
- [x] Requirements отделяют сезонный контекст от прогноза и рекомендаций.
- [x] User stories независимо тестируемы.
- [x] Edge cases покрывают короткую историю, sparse buckets и signal scope.
- [x] Success criteria измеримы и проверяемы.

## Scope Control

- [x] Нет новых внешних источников данных.
- [x] Нет новой БД/migration для v1.
- [x] Налоговые периоды и внешние факторы явно отложены.
- [x] Structured JSON включен в scope.
