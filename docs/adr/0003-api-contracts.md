# ADR 0003: Optional shared API contracts project

**Status:** Proposed (not decided)  
**Date:** 2026-09-28

## Context

`Jobsy.Web` talks to `Jobsy.Api` via `JobsyApiClient`. Roughly **55–62** request/response DTO shapes are mirrored (or hand-maintained) on both sides. Drift causes subtle UI bugs and extra review load.

Options discussed in the code cleanup review:

1. Keep mirroring (status quo).
2. Add a `Jobsy.Contracts` (or similar) class library referenced by Api and Web for shared DTOs.
3. Generate client/DTOs from OpenAPI.

## Decision

**Not decided.** Documenting the option so future work can choose deliberately.

If adopted later:

- Start with high-churn DTOs only; avoid moving domain entities into the contracts assembly.
- Keep authorization and persistence types out of the shared package.
- One focused PR; no drive-by renames.

## Consequences

- Until decided, treat Api JSON shapes as the source of truth and update Web DTOs in the same PR.
- Do not create `Jobsy.Contracts` in drive-by cleanup PRs without an explicit product/tech decision.
