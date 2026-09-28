Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 15: Documentation: README, architecture, ADRs, onboarding, roles matrix, routes

**Goal:** a new developer (or AI agent) can understand, run and safely change Lobsy within one hour. Docs only.

**Evidence:**
- `README.md` is stale: it says Jobsy, Leaflet and OSRM, while the app uses Lobsy branding and MapLibre.
- There is no ARCHITECTURE, ADR, CONTRIBUTING or ONBOARDING doc.
- The roles and scope model are implicit (see code-review.md §0).
- Mixed NL/EN routes are undocumented.
- Leftovers: root `package.json` (only `pptxgenjs` for `docs/demo/*.pptx`), `WebAssemblyMarker`.
- 387 remote branches, 12 open PRs.

**Do:**
1. `README.md` rewrite (short):
   - what Lobsy is;
   - the stack (.NET 9 Blazor Server + ASP.NET Core API + PostgreSQL + MapLibre, Render);
   - running locally (`.github/scripts/start-ci-stack.sh` or `dotnet run` for Api + Web, Postgres via docker), demo users (point to the seeder, no passwords in the README if they are not already public dev values);
   - running tests (unit vs Playwright);
   - branch flow: feature → `acceptatie` → `main`.
2. `docs/ARCHITECTURE.md`:
   - projects and dependencies (Core ← Infrastructure ← Api; Web → Core, talking to Api via `JobsyApiClient`);
   - auth flow (cookie + API JWT, MFA);
   - authorization (policies, `JobsyRoles`, `CompanyAuthorizationService` scope);
   - static assets and the `?v` guard; the CSS/JS bundle story;
   - background jobs, OpenAI usage, push, payments;
   - a diagram (mermaid).
3. ADRs in `docs/adr/`:
   - `0001-keep-jobsy-code-name.md`: namespaces, DB, headers, storage keys and repo stay "Jobsy"; the user-visible brand is "Lobsy".
   - `0002-thin-controllers.md`: new logic goes in services, not in controllers with `_db` (27 controllers currently inject DbContext); there is no big-bang refactor.
   - `0003-api-contracts.md`: option to add `Jobsy.Contracts` for the ~55–62 mirrored DTOs. Proposed, not decided.
   - `0004-roles-and-scope.md`: link to `docs/security/roles-matrix.md` from prompt 04, with RegionalManager read-only.
4. `CONTRIBUTING.md`: one PR = one change; the mandatory PR template items (build, tests, Playwright for UI, screenshots); the `?v` bump rule; no hand-editing of `app.min.css` after prompt 10; where to put components; UiStrings rules; migrations last after rebase.
5. `docs/ONBOARDING.md`: secrets (names only: where they live in Render/GitHub), Render services (starter plan, 0.5 CPU; see PERF-10), Sentry, CI workflows, how to get Acc access, MFA.
6. `docs/ROUTES.md`: a generated table of all `@page` routes → component → roles (write a small script or test that generates it and fails when it is outdated). Keep the NL/EN mix documented; **do not rename routes**.
7. `Jobsy.Tests/README.md`: test categories and filters, the source-grep tests (412 `File.ReadAllText` asserts) and when to prefer bUnit/Playwright, how to run coverage.
8. A note in `docs/ONBOARDING.md` about the leftovers (package.json/pptx, WebAssemblyMarker) and branch hygiene: propose deleting merged remote branches (list only; **do not delete**).

**Do not touch:** code, workflows, and `.cursor/rules` (except adding links to the new docs).

**Verify:** markdown renders; links resolve (`npx markdown-link-check` or a manual check); ROUTES generation test green; build and tests green.

**Dependency:** best after 03/04 (roles matrix) and 01 (CI description). It can be written in parallel otherwise.
