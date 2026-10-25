# 08e — QuestPDF 2025 → 2026.x (analysis only)

**Status:** Decision 1 — licence unconfirmed. **No package bump and no code change in this PR.**

## Current usage (repo @ code-health-08c)

| Location | Role |
|---|---|
| CPM | `QuestPDF` **2025.12.4** (`Directory.Packages.props`) |
| `Jobsy.Infrastructure` (17 call sites set `LicenseType.Community`) | All PDF generation |

PDF services / builders (Fluent API + `GeneratePdf`):

- Sales: `SalesMaterialsPdfService`, `SalesPayoutProfileService`, `SalesWalletPortalService`, `SalesManagerPayoutService`, `AmbassadeurFlyerPdfService`, `PartnerFlyerPdfService`, `MarketingFlyerPdfService`
- Employer: `EmployerRaamflyerService`
- Finance: `ConsumerInvoiceService`, `TokenPurchaseInvoiceService`, `VatDeclarationService`
- Candidate / assessments: `LobsyCvPdfService`, `AssessmentReportPdfService`, `SampleAssessmentReportPdfService`
- Letters: `VerificationLetterPdfBuilder`
- Scholen: `PupilReportPdfService`, `SchoolCodeListPdfService`

Every generator sets `QuestPDF.Settings.License = LicenseType.Community` before compose.

## Licence (QuestPDF Community — Version 3.0, 6 July 2026)

Sources:

- [Community License](https://www.questpdf.com/license/community.html)
- [License Selection Guide](https://www.questpdf.com/license/guide.html)
- [LICENSE.md on GitHub](https://github.com/QuestPDF/QuestPDF/blob/main/LICENSE.md)
- [Pricing FAQ](https://questpdf.com/pricing.html)

Quoted eligibility for free Community use (small-business category):

> **Small businesses.** An organisation with annual gross revenue under **USD 1,000,000** in its most recently completed fiscal year, measured on a consolidated basis across entities under common control.

Plain-language summary from the vendor:

> QuestPDF is free for individuals and businesses with annual gross revenue under USD 1,000,000, and for charitable organisations, academic institutions, and open-source projects. Public-sector entities and publicly traded companies are not eligible, regardless of revenue. All eligible users may use QuestPDF for commercial purposes. If you stop qualifying, you have a 90-day transition period to purchase a paid license.

Also relevant:

- Community use requires setting `QuestPDF.Settings.License = LicenseType.Community` (already done at every call site).
- The licence shipped with a given release governs that release permanently; future term changes do not rewrite older packages retroactively.
- If revenue exceeds the threshold, the 90-day transition starts at the end of that fiscal period (or on change of control).

### What that means for Lobsy

Lobsy uses QuestPDF as a **direct** dependency across many PDF products. Whether Community remains valid depends on consolidated annual gross revenue and entity type. **Dennis must confirm** before any 2026.x bump:

1. Is Lobsy (and commonly controlled entities) under the **&lt; USD 1,000,000** annual gross revenue threshold for the most recently completed fiscal year (or a good-faith annualised estimate if year 1)?
2. Is Lobsy **not** a public-sector entity and **not** a publicly traded company?
3. If Community does not apply, purchase the appropriate Professional/Enterprise licence before upgrading.

Until that confirmation, stay on **2025.12.4**.

## API / release impact (expected for 2026.x)

Latest NuGet at analysis time: **2026.9.1** (from 2025.12.4).

Notable breaking / behavioural changes on the path from 2025.12 → 2026.9 (vendor release notes):

| Release | Change | Lobsy impact |
|---|---|---|
| 2026.6 | `TranslateX`/`TranslateY` → `OffsetX`/`OffsetY` | Grep on bump; rename if used |
| 2026.7 | `Rotate` origin = center; image gen defaults to white background; `RotateLeft`/`RotateRight` deprecated | Check any rotate usage; PNG preview tests if present |
| 2026.8 | XPS generation removed | **N/A** (we only emit PDF) |
| **2026.9** | System fonts **off** by default (`UseSystemFonts`); missing font families/glyphs **throw**; `FontDiscoveryPaths` → `FontDiscoveryPath`; bundled Lato as archive | **Highest risk** — Docker/CI must ship or register every font family our PDFs reference; otherwise generation fails at runtime |

Also:

- Fluent layout API (`Document.Create`, `Page`, `Column`, `Table`, `Text`, `Image`) remains the primary surface.
- `LicenseType.Community` remains the Community gate — keep setting it (or centralise once in startup if a later PR prefers a single place).
- Binary PDF output can shift slightly (font subsetting, layout rounding, zlib level in 2026.8). Visual/byte comparison of every PDF type is required after a bump.

## Effort (when licence OK)

| Work | Notes |
|---|---|
| CPM bump `2025.12.4` → latest `2026.x` | One-line package change |
| Compile / API fixes | Likely small; touch Infrastructure PDF services only |
| Regression | Render each PDF type before/after; visual compare (sales materials, raamflyer, invoices, CV, assessment, verification letter, scholen reports) |
| Estimate | Medium (many PDF surfaces, mostly mechanical + visual QA) |

## Out of scope here

No `Directory.Packages.props` change, no code edits, no merge recommendation until Dennis confirms the licence path.
