# 06: Warnings cleanup 2: unused code and signatures

- **Branch:** `cursor/code-health-06-unused`, from `cursor/code-health-05-behaviour`.
- **PR:** into that branch.
- Re-measure first (the counts below are from 2 Oct, net9).

## Scope

| Code | Count (2 Oct) | Fix | Notes |
|---|---:|---|---|
| IDE0060 unused parameter | 35 (Core 14, Infra 8, Web 8, Api 1, Tests 4) | Remove the parameter and update the call sites. If the signature is fixed (interface implementation, delegate, minimal-API handler, DI factory, Razor event callback), rename it to `_`/`_name`, or add an inline `#pragma` with the reason. | **Look for unfinished logic first**: e.g. `Jobsy.Core/Media/VacancyImageUrls.cs` ~l.459 `vacancyId`, `Jobsy.Core/Privacy/PrivacyConstants.cs` ~l.91 `role`, `Jobsy.Core/Scholen/PupilStoryTemplates.cs` ~l.232 `scores`, `TransactionalEmails.Templates3.cs` (2). When a parameter was clearly meant to influence the result (role-based privacy!), **don't delete it silently**: list it in the PR as a question for Dennis, and keep it with a `// TODO(code-health): …` plus a suppression. |
| IDE0052 field assigned but never read | 13 (Api 7, Infra 2, Tests 4) | Remove the field **and** the constructor parameter (e.g. `DeepAnalysisService._environment/_configuration`, `AmbassadeursController._environment`). | Update every `new X(...)` in the tests. DI registration usually needs no change. |
| IDE0051 unused private member | 5 | Delete it (`TransactionalEmails.Bold/F/Fmt`, `CompanyRegistrationService.ValidateSalesOrAmbassadeurTrackingCodeAsync`, …). | Grep for reflection/nameof use first. For `ValidateSalesOrAmbassadeurTrackingCodeAsync`, check whether registration *should* validate tracking codes (the sales attribution tests) before deleting. If unsure, keep it and ask. |
| IDE0059 unnecessary assignment | rest | Same as 05. | — |
| CA1822 member can be static (production code only) | 4 | Make it `static` (`EmailLinks.SupportMailto`, `CompanyVerificationFlowService.ResolveProviderKind/PublicBase`, …). | Check that Razor/DI doesn't call them through an instance in a way that breaks. |

## Acceptance

- None of these codes remain in production projects (Tests: only where 02 suppressed them).
- Every removed parameter that looked like unfinished logic is listed in the PR under "Questions for Dennis".
- The warning count is lower and the baseline is lowered to the exact count.
- Full tests: no new failures. Fresh-DB migration test and PendingModelChanges pass.
