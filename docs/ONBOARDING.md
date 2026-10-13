# Onboarding — Lobsy / Jobsy

Goal: a new developer or AI agent can run, test, and safely change the app within about an hour. Pair this with [`../README.md`](../README.md) and [`ARCHITECTURE.md`](ARCHITECTURE.md).

## Names

| Surface | Name |
|---------|------|
| Brand / UI / domains | **Lobsy** |
| Repo, namespaces, DB, many env keys | **Jobsy** |

See [ADR 0001](adr/0001-keep-jobsy-code-name.md).

## First hour checklist

1. Install .NET 10 SDK; `dotnet tool restore`; Docker for PostGIS.
2. Start stack (README Option A or B); open `http://localhost:5201`.
3. Log in with a seeded `@jobsy.local` demo user (`DemoUsersSeeder`).
4. Read [`security/roles-matrix.md`](security/roles-matrix.md) and [`ROUTES.md`](ROUTES.md).
5. Run `dotnet test Jobsy.Tests/Jobsy.Tests.csproj` (or a focused filter).
6. Skim [`CONTRIBUTING.md`](../CONTRIBUTING.md) before opening a PR to **`acceptatie`**.

## Secrets (names only)

Never commit secret values. Set them in **Render Dashboard** (per environment) or **GitHub Actions** secrets/vars.

### Render (API / Web) — typical keys

| Key | Where | Notes |
|-----|-------|-------|
| `ConnectionStrings__JobsyDb` | API (+ Web if needed) | From Render Postgres |
| `JobsyAuth__DevelopmentAuthSecret` | API + Web (same env) | Generated in Blueprint |
| `JobsyAuth__LocalSessionSigningKey` | API + Web | HMAC local session |
| `JobsyAuth__ExternalProvisionSecret` | API + Web | OAuth credential provision |
| `JobsyAuth__InternalClientIpSecret` | API + Web | Rate-limit partitioning |
| `JobsyAuth__Jwt__PublicKeyPem` | API | ES256; `sync: false` |
| `JobsyAuth__Jwt__PrivateKeyPem` | Web | Must match API public |
| `JobsyAuth__Jwt__Issuer` / `Audience` | both | `jobsy-web` / `jobsy-api` |
| `CLOUDFLARE_ORIGIN_SECRET` | API (+ CF rule) | Origin header gate |
| `Sentry__Dsn` | API + Web | Error reporting |
| `Mail__Provider` | API + Web | `Resend` or `Lettermint`. Web needs the same value so the privacy page matches the sender. |
| `Mail__ResendApiKey` / `Mail__FromAddress` | API | Resend mail. Also `RESEND_API_KEY`. |
| `Lettermint__ApiKey` | API + Web | Lettermint project token (`LETTERMINT_API_KEY`). An empty key does not fall back to Resend. Web only checks that the key is present. |
| `Mail__AllowedRecipientPattern` | API | Acceptatie only, e.g. `^test-[^@]+@lobsy\.nl$`. Empty on production. |
| `Mail__AllowedRecipientAddresses__0` | API | Optional extra address (the admin) when the pattern is set. |
| `WebPush__Subject` / `PublicKey` / `PrivateKey` | API | VAPID |
| `VerificationCodes__Pepper` | API | OTP hashing |
| `Training__TrackingSecret` | API | Training links |
| Mollie / OpenAI / KVK integration keys | API / admin integrations | Prefer Dashboard or encrypted integration store — not the repo |
| `Ai__Provider` / `Mistral__ApiKey` / `Mistral__BaseUrl` | Acceptatie API **and** web | `OpenAI` (default) or `Mistral`. Production stays on OpenAI. Leave `Mistral__BaseUrl` unset so calls use the EU endpoint `https://api.eu.mistral.ai/v1/`. The privacy page counts Mistral as EU only when the base URL host is `api.eu.mistral.ai`. Quality calls use `Mistral__Model`. Cheap calls (vertaling, CV, vacaturecontrole) use `Mistral__SmallModel` when set. |

Production and Acceptatie must **not** share the same JWT PEMs or auth secrets. Details: [`deploy-render.md`](deploy-render.md), [`../SECURITY.md`](../SECURITY.md).

### GitHub Actions

| Name | Used by |
|------|---------|
| `JOBSY_E2E_BASE_URL` | Acceptatie smoke / live Playwright |
| `JOBSY_E2E_CANDIDATE_EMAIL` / `JOBSY_E2E_CANDIDATE_PASSWORD` | Optional Acc candidate login |
| Other workflow secrets | As defined in `.github/workflows/*` |

## Render services

| Environment | Branch | Services | Public |
|-------------|--------|----------|--------|
| Production | `main` | `jobsy-api`, `jobsy-web`, `jobsy-db` | https://lobsy.nl |
| Acceptatie | `acceptatie` | `lobsy-acc-api`, `lobsy-acc-web`, `lobsy-acc-db` | https://acceptatie.lobsy.nl (also `*.onrender.com`) |

- Instance plan: **starter** web services (~always-on; no free-tier spin-down). Postgres **basic-256mb**.
- Capacity is modest (starter ≈ **0.5 CPU** class) — keep payloads and banenkaart work lean; see [`performance.md`](performance.md) and deploy notes (PERF-oriented ops).
- Blueprint: [`../render.yaml`](../render.yaml).

## Sentry

Both API and Web use `Sentry.AspNetCore`. Configure `Sentry__Dsn` per environment in Render (`sync: false`). Use Acc DSN for Acceptatie noise; keep Production separate. Circuit/exception logging also surfaces in Web hosting helpers.

## CI workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| [`pr-tests.yml`](../.github/workflows/pr-tests.yml) | PRs + push `main`/`acceptatie` | Build, unit/integration tests, Playwright smoke vs CI stack |
| [`code-quality.yml`](../.github/workflows/code-quality.yml) | PRs | Warning baseline, whitespace format on changed `.cs`, vulnerable packages, gitleaks |
| [`acceptatie-smoke.yml`](../.github/workflows/acceptatie-smoke.yml) | Push `acceptatie` + schedule | Live Acc smoke |
| [`production-health.yml`](../.github/workflows/production-health.yml) | Push `main` + schedule | Homepage / health / pins |

Local CI stack helper: [`.github/scripts/start-ci-stack.sh`](../.github/scripts/start-ci-stack.sh).

## Acceptatie access

1. Ask a maintainer for Acc URL access and a non-production login (seeded demo accounts exist when Acc seed is enabled; Production demo-login is disabled).
2. Confirm commit: `curl -s https://acceptatie.lobsy.nl/ | grep lobsy-commit` should match `origin/acceptatie`.
3. Open PRs **into `acceptatie`**; only maintainers promote `acceptatie` → `main`.
4. MFA: accounts that require MFA hit `/account/mfa` after password/external login; enroll/verify via API challenge tokens. Use Acc for MFA UX testing — never reuse Production secrets locally.

## MFA (developer notes)

- API issues `MfaChallengeToken` when the profile requires MFA.
- Web stores challenge in cookies, completes verify, then issues the normal auth cookie with `MfaVerified` claim.
- `UseMfaEnforcement` on Web blocks sensitive navigation until verified.

## Leftovers (known, intentional for now)

| Item | Why it exists | Action |
|------|---------------|--------|
| Root [`package.json`](../package.json) | Only dependency: `pptxgenjs` for [`docs/demo`](demo/) PowerPoint generation (`docs/demo/build-pptx.cjs`) | Keep until demo PPTX tooling moves; not part of the .NET app runtime |
| [`Jobsy.Web/WebAssemblyMarker.cs`](../Jobsy.Web/WebAssemblyMarker.cs) | Marker type so tests can host Web via `WebApplicationFactory<WebAssemblyMarker>` — **not** a WASM build | Keep; name is historical |

## Branch hygiene

GitHub currently carries on the order of **~400 remote heads**, many `cursor/*` feature branches already **merged into `acceptatie`**. That slows fetches and confuses agents.

**Proposal (list only — do not delete from this onboarding PR):** maintainers should periodically delete remote branches that are fully merged into `acceptatie` (and closed PRs). Example merged remotes (sample; regenerate with `git branch -r --merged origin/acceptatie`):

- `origin/cursor/admin-gdpr-audit-log-masking-a5fc`
- `origin/cursor/admin-gdpr-support-access-a5fc`
- `origin/cursor/authz-regiomanager-readonly-a5fc`
- `origin/cursor/authz-role-scope-matrix-a5fc`
- `origin/cursor/guardrails-editorconfig-analyzers-a5fc` (if present / merged)
- `origin/cursor/acc-2709-fixes-a5fc`
- `origin/cursor/banenkaart-map-perf-a5fc`
- `origin/cursor/account-menu-logout-fix-a5fc`
- …plus hundreds more `origin/cursor/*` tips already contained in `acceptatie`

Suggested maintainer command (review the list before deleting):

```bash
git fetch origin acceptatie
git branch -r --merged origin/acceptatie | grep 'origin/cursor/' | sed 's|origin/||'
# Then delete selectively, e.g.:
# git push origin --delete <branch>
```

Prefer deleting only after the PR is merged and no follow-up work remains on that tip.

## Next reading

- [`ARCHITECTURE.md`](ARCHITECTURE.md) · [`CONTRIBUTING.md`](../CONTRIBUTING.md) · [`release-flow.md`](release-flow.md)  
- [`../Jobsy.Tests/README.md`](../Jobsy.Tests/README.md) · [`../TESTING.md`](../TESTING.md)  
- ADRs under [`adr/`](adr/)  
