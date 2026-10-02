# 03: Low-risk same-major package bumps

- **Branch:** `cursor/code-health-03-packages-minor`, from `cursor/code-health-02-config`.
- **PR:** into `cursor/code-health-02-config`.
- All versions live in `Directory.Packages.props` (central package management with transitive pinning).
- **No TFM change** in this step. Everything stays on net9.0 and 9.0.20 for the Microsoft.* packages.
- Make one commit per group, so a single bump can be reverted.

## Bumps (versions measured 2 Oct 2026; take the latest patch of the same minor/major at implementation time)

| Package | From | To | Projects | Watch out for |
|---|---|---|---|---|
| Microsoft.Identity.Web | 3.8.3 | **3.15.x** (latest 3.x) | Api | Entra/OIDC sign-in and token acquisition. **Goal: drop the deprecated transitive packages** `Azure.Identity` 1.11.4, `Microsoft.Identity.Client` 4.70.1 and `Microsoft.Identity.Client.Extensions.Msal` 4.61.3. |
| Sentry.AspNetCore | 5.14.1 | **5.16.x** | Api, Web | Errors still reach Sentry. The DSN is not configured locally; check that `UseSentry` options still bind. |
| Swashbuckle.AspNetCore | 7.2.0 | **7.3.x** | Api | `/swagger` (dev only) still renders and the operation IDs are unchanged. |
| AngleSharp | 1.5.0 | **1.8.x** | Infra, Tests | Must stay compatible with bunit 1.x (see the bunit row). |
| MailKit | 4.17.0 | **4.18.x** | Infra | SMTP send path; MimeKit moves with it. |
| QRCoder | 1.6.0 | **1.8.x** | Infra | Possible new `[Obsolete]` on renderer APIs. Fix them in this PR (no new CS0618). |
| QuestPDF | 2025.7.1 | **2025.12.x** | Infra | Same release year, so the licence terms are unchanged. Render the sales PDFs/flyers before and after and compare them visually; attach one before/after image in the PR. |
| WebPush | 1.0.12 | **1.0.13** | Infra | Still pulls `Portable.BouncyCastle` 1.8.1.3 (unmaintained). Note it; don't replace it here. |
| bunit | 1.36.0 | **1.40.x** (latest 1.x) | Tests | Also lifts the transitive `Microsoft.AspNetCore.Components*` from 9.0.0. |
| xunit | 2.9.2 | **2.9.3** | Tests | — |
| Microsoft.NET.Test.Sdk | 17.12.0 | **17.14.x** | Tests | — |
| coverlet.collector | 6.0.2 | **6.0.4** | Tests | — |
| Microsoft.Playwright | 1.49.0 | **1.63.x** | Tests | CI and local runs must reinstall browsers (`playwright.ps1 install --with-deps chromium`). Screenshot or selector drift is possible. |

**Not in this step:** Microsoft.AspNetCore.*, EF Core, Microsoft.Extensions.* and Npgsql (they move to 10.x in 04), and every major bump (08–10).

## Verification

1. `dotnet restore` without NU warnings.
2. `dotnet list Jobsy.sln package --deprecated --include-transitive`:
   - The **only** remaining deprecated entries may be xunit 2.x (`xunit`, `xunit.core`, `xunit.assert`, `xunit.extensibility.*`); they are handled in 09.
   - If Azure.Identity/MSAL are still listed after Identity.Web 3.15.x, pin them in `Directory.Packages.props` (transitive pinning is on) to their current non-deprecated versions (at analysis time: Azure.Identity 1.21.0, Microsoft.Identity.Client / .Extensions.Msal 4.90.x). Add a comment saying why. Paste the before/after `--deprecated` output in the PR.
3. `dotnet list Jobsy.sln package --vulnerable --include-transitive`: none.
4. Release build: warning count ≤ base (new obsoletions from the bumps are fixed here).
5. Full test suite, including the Playwright suites (because of the Playwright bump), on desktop and mobile projects. No new failures.
6. Manual / Playwright smoke test on a local stack:
   - local login (`kandidaat@jobsy.local`)
   - an admin page
   - render one sales PDF
   - send one email through the dev mail sink
7. Fresh-DB migration test and PendingModelChanges pass.

## Acceptance

- The `--deprecated` list contains only xunit 2.x.
- No vulnerable packages.
- Tests: no new failures.
- The warning count did not rise.
- The PR has the version table (from → to, as resolved) plus the outputs of `--outdated`/`--deprecated`.
