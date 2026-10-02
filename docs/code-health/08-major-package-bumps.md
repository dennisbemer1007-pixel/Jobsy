# 08: Major package bumps (one sub-PR per package)

Stacked: each one branches from the previous sub-PR (08a from `cursor/code-health-07-obsolete-mechanical`). One package (family) per PR, with the global rules from [00-README](00-README.md). Every sub-PR must:
- bring 0 new warnings
- pass the full tests, the fresh-DB migration test and the API smoke test
- paste the vendor's migration-guide checklist in the PR, with each item marked done / not applicable

## 08a Sentry 5 → 6: `cursor/code-health-08a-sentry6`

- `Sentry.AspNetCore` 5.16.x → latest 6.x (Api, Web).
- Follow Sentry's 6.0 migration guide: removed/renamed options, `SentryOptions` changes, the minimum framework, and the `UseSentry` / `AddSentry` registration.
- Check: the `SENTRY_DSN` env var binding (names unchanged, so **no Render change**); PII scrubbing (`SendDefaultPii` must stay off); sample rates; the environment name (`Lobsy__DeploymentEnvironment`).
- Test: run locally with a dummy DSN pointing at a local capture endpoint, or use `SentryOptions.Transport` in a test, to check that one exception is captured with the right environment and no PII.

## 08b OpenAPI: `cursor/code-health-08b-openapi`

- Option A (preferred on net10): replace Swashbuckle with the built-in `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`/`MapOpenApi`). Use Swagger UI only in Development, through the Swashbuckle UI package or Scalar.
- Option B: Swashbuckle 7 → 10, which brings `Microsoft.OpenApi` 2.x. Any `IOperationFilter`/`ISchemaFilter` breaks (namespace and model changes).
- Inventory first: grep for `Swashbuckle`, `IOperationFilter`, `ISchemaFilter`, `AddSwaggerGen`, `UseSwagger`. **Ask Dennis** which option to take if there are custom filters; otherwise take A.
- Acceptance: the dev `/swagger` (or `/openapi/v1.json`) works, it is **not** exposed in Production (test: the `Production` environment returns 404), and the JSON contains all controllers.

## 08c Microsoft.Identity.Web 3 → 4: `cursor/code-health-08c-identityweb4`

- Latest 4.x (Api). Follow Microsoft's 3 → 4 changelog: options binding, `AddMicrosoftIdentityWebApi`, token-acquisition and certificate-loading changes.
- Re-check `--deprecated --include-transitive` (no Azure.Identity/MSAL deprecations) and remove any pins added in 03 that are no longer needed.
- Tests:
  - `ExternalProviderConfigTests`, `ExternalAuthAndInvitePromotionTests`, `AuthHardeningHotfixTests`, MFA tests
  - Entra issuer validation (`EntraOidcOptionsApplier`)
  - a local/acceptance-like sign-in flow, with the Entra config stubbed

## 08d SixLabors.ImageSharp 3 → 4: `cursor/code-health-08d-imagesharp4` (blocked: Dennis decides first)

- **Do not start until Dennis confirms the licence.** Six Labors uses the "Six Labors Split License": free under Apache 2.0 for open source and for companies **below a revenue threshold**, otherwise a commercial licence is needed. Check whether that has changed for v4 and whether it applies to Lobsy. Without a confirmation, open a **draft PR with only the analysis** and stop.
- Scope: `SixLabors.ImageSharp` 4.x + `SixLabors.ImageSharp.Drawing` (matching major), used in Infra and `tools/generate-icons`. API changes in processing, encoders and metadata.
- Tests: image upload/resize pipeline tests, vacancy image proxy, icon generation (`dotnet run --project tools/generate-icons`), with the output compared byte- or visual-wise.

## 08e (optional) QuestPDF 2025 → 2026.x: `cursor/code-health-08e-questpdf2026`

- **Licence check as with ImageSharp** (QuestPDF Community licence has a revenue threshold). Dennis confirms first.
- Render all PDF types (sales PDFs, flyers, Raamflyer, invoices if any) before and after and compare them visually; attach images.

## Not in 08

AngleSharp/MailKit/QRCoder (done in 03), bunit and xunit (09), MapLibre and html2canvas (10).
