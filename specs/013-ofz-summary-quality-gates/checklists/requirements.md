# Requirements Checklist: Quality gates для OFZ summary/export

**Feature**: `013-ofz-summary-quality-gates`
**Date**: 2026-05-10

## Content Quality

- [x] User stories describe validation outcomes, not implementation mechanics.
- [x] Acceptance scenarios are independently testable.
- [x] Scope excludes new analytics, persistence and WPF layout changes.
- [x] Missing-data behavior is explicit.
- [x] Recommendation/causality wording remains forbidden.

## Requirement Completeness

- [x] Required summary sections are listed.
- [x] Required source counts are listed.
- [x] Schema version expectation is stated.
- [x] Date-only serialization is stated.
- [x] External factor finding presence is stated.
- [x] No fake zero / no fake n/a policy is stated.
- [x] Snapshot/provisional preservation is stated.

## Readiness

- [x] Assumptions are documented.
- [x] Success criteria are measurable.
- [x] Targeted and full validation commands are known.
- [x] No clarification is needed before planning.
