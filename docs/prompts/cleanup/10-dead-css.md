Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 10: Remove dead CSS rules and add a real minifier step for app.min.css

**Goal:** shrink `app.css` (21,659 lines) by removing rules that no markup uses, and stop hand-syncing `app.min.css`. No visual change.

**Evidence:**
- `docs/review/data/dead-css-rules.tsv` lists 286 rules (~1,590 lines). `SAFE` (272) = every class in the selector has zero references in `.razor/.cs/.js/.html` outside Jobsy.Tests, and there is no dynamic class construction nearby. `DEFER` (14) = possibly built dynamically (string interpolation like `$"status-{x}"`, `class="@(...)"`). The file includes `app.css` and `questionnaire.css` line ranges.
- `app.min.css` is a hand-synced copy (no minifier in the repo). Tests assert on its content.
- CSS duplication is low (0.06%); the problem is size and dead rules (PERF-8 in `/workspace/jobsy-design/review/performance-audit.md` covers CSS splitting; do not do that here).

**Do:**
1. **Re-generate the list first** (line numbers will have shifted after pending PRs): run the same approach (extract class selectors → `rg -w` each class over `Jobsy.Web` `.razor/.cs/.js/.html`, excluding tests), then diff with the TSV. Only remove rules that are SAFE in both.
2. Remove SAFE rules from `app.css` and `questionnaire.css`, in batches of max ~50 rules per commit, grouped by area.
3. Add a pinned minifier (e.g. `lightningcss-cli` or `clean-css-cli` with an exact version in a `tools/css/package.json` + lockfile) and a script `tools/css/build.sh` that produces `app.min.css`. Add a CI step (in `code-quality.yml` from prompt 01) that fails if `app.min.css` is not equal to the build output. Regenerate `app.min.css` once.
4. Bump the `?v` of `app.min.css` via the asset-versions guard mechanism.
5. Update tests that assert removed selectors only if the selector is really dead (such a test is a stale "source grep" test; remove the assert and mention it in the PR).

**Do not touch:** DEFER rules; rules with element/ID/attribute selectors only; `@keyframes` still referenced by `animation:`; CSS custom properties; `banenkaart.css` and map CSS (pending map PRs); any page-specific CSS file changed by pending PRs.

**Verify:**
- Playwright **visual smoke** (screenshots before/after, mobile 390×844 and desktop 1440×900) of `/`, `/banen`, vacancy detail, `/login`, candidate home, match page, a test page, employer dashboard, create vacancy, admin home. Pixel diff within a tiny threshold (attach the diff images).
- Build and tests green.
- `app.css` line count and `app.min.css` size before/after in the PR.

**Dependency:** **wait until ALL pending CSS PRs are merged:** filter-sheet/bottom-nav CSS fix; map-height cookie padding (app.css ~21580); map popup height + app.min.css bump; VacancyDiscovery VacancyCard refactor; VacancyDetail hero/YouTube facade; bottom-nav feedback. Also after prompt 01 (workflow exists) and preferably after prompt 09.
