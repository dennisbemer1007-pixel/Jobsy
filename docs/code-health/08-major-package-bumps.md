# 08: Major package bumps (one sub-PR per package)

Stacked: each one branches from the previous sub-PR (08a from `cursor/code-health-07-obsolete-mechanical`). 08d and 08e are analysis-only drafts, docs only, so step 09 branches from **08c**. One package (family) per PR, with the global rules from [00-README](00-README.md). Every sub-PR must:
- bring 0 new warnings
- pass the full tests, the fresh-DB migration test and the API smoke test
- paste the vendor's migration-guide checklist in the PR, with each item marked done / not applicable

## 08a Sentry 5 → 6: `cursor/code-health-08a-sentry6`

- `Sentry.AspNetCore` 5.16.x → latest 6.x (Api, Web).
- Follow Sentry's 6.0 migration guide: removed/renamed options, `SentryOptions` changes, the minimum framework, and the `UseSentry` / `AddSentry` registration.
- Check: the `SENTRY_DSN` env var binding (names unchanged, so **no Render change**); PII scrubbing (`SendDefaultPii` must stay off); sample rates; the environment name (`Lobsy__DeploymentEnvironment`).
- Test: run locally with a dummy DSN pointing at a local capture endpoint, or use `SentryOptions.Transport` in a test, to check that one exception is captured with the right environment and no PII.

## 08b OpenAPI: `cursor/code-health-08b-openapi` (Decision 4: built-in `Microsoft.AspNetCore.OpenApi`)

- Replace Swashbuckle with the built-in **`Microsoft.AspNetCore.OpenApi`** (10.x):
  - `builder.Services.AddOpenApi()`
  - `app.MapOpenApi()` → `/openapi/v1.json`
- Remove `Swashbuckle.AspNetCore` from `Directory.Packages.props` and the Api csproj.
- Inventory first: grep for `Swashbuckle`, `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI`, `IOperationFilter`, `ISchemaFilter`, `IDocumentFilter`, `[SwaggerOperation]`. Port every custom filter to `IOpenApiOperationTransformer` / `IOpenApiSchemaTransformer` / `IOpenApiDocumentTransformer`, e.g. the auth/security scheme and XML comments.
- UI, **Development only**: a small self-hosted viewer (Scalar via `Scalar.AspNetCore`, or the Swagger UI static package) served from `'self'`, with no CDN, so the CSP stays as it is. Or no UI at all, just the JSON.
- Acceptance:
  - In Development, `/openapi/v1.json` returns 200 and contains all controllers and minimal-API endpoints, with operation IDs stable or the changes listed.
  - In **Production and Acceptatie the document and UI return 404** (add a test).
  - No Swashbuckle package left (`dotnet list package`).
  - 0 new warnings; full tests green.

## 08c Microsoft.Identity.Web 3 → 4: `cursor/code-health-08c-identityweb4`

- Latest 4.x (Api). Follow Microsoft's 3 → 4 changelog: options binding, `AddMicrosoftIdentityWebApi`, token-acquisition and certificate-loading changes.
- Re-check `--deprecated --include-transitive` (no Azure.Identity/MSAL deprecations) and remove any pins added in 03 that are no longer needed.
- Tests:
  - `ExternalProviderConfigTests`, `ExternalAuthAndInvitePromotionTests`, `AuthHardeningHotfixTests`, MFA tests
  - Entra issuer validation (`EntraOidcOptionsApplier`)
  - a local/acceptance-like sign-in flow, with the Entra config stubbed

## 08d SixLabors.ImageSharp 3 → 4: `cursor/code-health-08d-imagesharp4` (**analysis-only draft PR**: Decision 1, licence unconfirmed)

- **No package bump and no code change.** The licence is still unconfirmed (Decision 1).
- Open a **draft PR** with only a docs file, `docs/code-health/08d-imagesharp4-analysis.md`, covering:
  - the current licence terms for v4 (Six Labors Split License: free under Apache 2.0 for open source and below a revenue threshold, otherwise commercial; quote the current wording with a link)
  - what that means for Lobsy
  - the API impact on our code (grep the usages)
  - an effort estimate
- **Stop there.** The scope and tests below are for a later PR, once Dennis confirms the licence.
- Scope: `SixLabors.ImageSharp` 4.x + `SixLabors.ImageSharp.Drawing` (matching major), used in Infra and `tools/generate-icons`. API changes in processing, encoders and metadata.
- Tests: image upload/resize pipeline tests, vacancy image proxy, icon generation (`dotnet run --project tools/generate-icons`), with the output compared byte- or visual-wise.

## 08e QuestPDF 2025 → 2026.x: `cursor/code-health-08e-questpdf2026` (**analysis-only draft PR**: Decision 1, licence unconfirmed)

- **No package bump and no code change** (Decision 1). Same as 08d: open a draft PR with `docs/code-health/08e-questpdf2026-analysis.md` covering the current QuestPDF licence (the Community licence has a revenue threshold; quote it with a link), what it means for Lobsy, breaking changes and effort. **Stop there.**
- Later PR, after licence OK:
- Render all PDF types (sales PDFs, flyers, Raamflyer, invoices if any) before and after and compare them visually; attach images.

## Not in 08

AngleSharp/MailKit/QRCoder (done in 03), bunit and xunit (09), MapLibre and html2canvas (10).
