# ADR 0001: Keep "Jobsy" as the code name

**Status:** Accepted  
**Date:** 2026-09-28

## Context

The product brand is **Lobsy** (UI copy, domains `lobsy.nl` / `acceptatie.lobsy.nl`, marketing). The repository, .NET namespaces, database name, HTTP headers, localStorage keys, Render service names (`jobsy-api`, `jobsy-web`, `jobsy-db`), and JWT issuer/audience still say **Jobsy**.

A full rename would touch every assembly, migration history, cookies, and deploy Blueprint — high risk for little user value.

## Decision

- **User-visible brand:** Lobsy (strings, logos, legal pages, SEO).
- **Code / infra name:** Jobsy stays — namespaces (`Jobsy.*`), solution/repo name, DB (`JobsyDb`), headers (`X-Jobsy-*`), storage keys, env var prefixes (`JobsyAuth__*`, `JobsyFeatures__*`), and Production Render service names.

## Consequences

- Docs and onboarding must state both names explicitly (see README and ONBOARDING).
- New code continues under `Jobsy.*`; new user-facing copy uses Lobsy.
- Do not rename Production Render services in `render.yaml` (see `docs/deploy-render.md`).
