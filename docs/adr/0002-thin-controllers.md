# ADR 0002: Thin controllers (no big-bang refactor)

**Status:** Accepted  
**Date:** 2026-09-28

## Context

About **27** API controllers currently inject `JobsyDbContext` (`_db`) and contain query/mutation logic. That couples HTTP concerns to persistence and makes authorization/scope easy to get wrong when copying patterns.

A wholesale rewrite of every controller would be large, conflict-prone, and hard to review.

## Decision

- **New** endpoints and non-trivial changes: put logic in **services** (Infrastructure or dedicated Api services). Controllers validate input, enforce `[Authorize]` / policies, call services, map results.
- **Existing** fat controllers: improve opportunistically when touching that area — no mandatory big-bang extract.
- Prefer reusing `CompanyAuthorizationService` and domain services over new ad-hoc `_db` queries in controllers.

## Consequences

- Code review should reject new heavy `_db` usage in controllers for greenfield endpoints.
- Incremental extractions are welcome; keep PRs focused (one change per PR).
- Tests can target services directly; controller tests stay thin HTTP checks.
