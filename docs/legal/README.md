# Legal pages: where the texts live and how to change them

Covers `/privacy`, `/algemene-voorwaarden` and `/gebruiksvoorwaarden` (public-pages 02–05).
The lawyer review list is [`review-needed.md`](review-needed.md).

## Where the text lives

| What | Where |
|---|---|
| The **official Dutch text** | `Jobsy.Web/Components/Legal/Docs/PrivacyNl.razor`, `AlgemeneVoorwaardenNl.razor`, `GebruiksvoorwaardenNl.razor` — razor markup, one `LegalSection` per section with a stable id |
| The frame (TOC, version line, print, "Wat is er veranderd?") | `Jobsy.Web/Components/Legal/LegalDocument.razor` |
| The terms audience switch | `Jobsy.Web/Components/Legal/TermsPage.razor` + `TermsAudienceSwitch.razor` |
| "In het kort", labels and SEO strings | `Jobsy.Web/Localization/UiStringsLegal.cs` (prefixes `Legal.`, `Privacy.`, `Terms.`) |
| Version + date | `Jobsy.Core/Legal/LegalDocumentVersions.cs` |
| Processor catalog | `Jobsy.Core/Legal/LegalProcessors.cs` → `ProcessorTable.razor` |
| Retention catalog | `Jobsy.Core/Legal/LegalRetention.cs`, read from `Jobsy.Core/Privacy/PrivacyConstants.cs` → `RetentionTable.razor` |
| Age rules (13 / 16 / 18) | `Jobsy.Web/Components/Legal/AgeRulesText.razor`, read from `CandidateConsentRules` |
| Identity (name, address, KvK, btw-id, e-mail) | `ILegalIdentity` / `GET api/site/legal`, rendered by `LegalIdentityCard.razor` |

The Dutch text is the official version (D3). A reader in another language gets the note "the Dutch
text is the official version" plus an "In het kort" block per section in their language; the Dutch
body itself stays on the page with `lang="nl" dir="ltr"`, also in Arabic.

## Rule: no literal identity in markup

Never type Lobsy's name, address, KvK number, btw number or e-mail address in a `.razor` file or a
resource string. Everything comes from `ILegalIdentity` (D1). The database row
(Bedrijfsgegevens) wins. A `Legal:*` value is used only when that database field is empty.
An empty field hides its line — it never becomes a placeholder like `[ADRES]`. The only literals allowed are the
`support@lobsy.nl` / `privacy@lobsy.nl` **defaults** in `LegalOptions`.

Guards: `LegalIdentityCardTests`, `PublicPagesWebGuardTests` (no placeholder pattern in the rendered
HTML) and `PublicPagesPlaywrightTests` (same check in five languages).

## Bumping a version

1. Change the text in the `*Nl.razor` document.
2. Bump `LegalDocumentVersions.Privacy` or `.Terms` (version `"YYYY-MM"` + `EffectiveFrom`).
   Both terms documents share one version on purpose.
3. Add an entry at the top of `PrivacyHistory` / `TermsHistory` with a new
   `Legal.Change.<Doc>.<version>` key; the page shows the last three ("Wat is er veranderd?", D16).
4. Add the summary key in **nl, en, pl, ro and ar** (`UiStringsLegal.cs`) — `LocalizationParityReportTests`
   fails otherwise, and `docs/i18n/untranslated-baseline.txt` may not grow.
5. Describe the change in [`review-needed.md`](review-needed.md): document · section id · version ·
   what changed · open question. Put "⚖️ Lawyer review needed" at the top of the PR body.
6. `PrivacyConstants.CurrentConsentVersion` is the **consent** version and is deliberately separate.
   A new consent round is a product decision, not a side effect of editing a text.

`LegalVersionsTests` keeps versions, history order and the change-log keys in sync.

## "In het kort" keys

Each `LegalSection` carries a `SummaryKey`, e.g. `Privacy.Short.delen` or `Terms.Short.melden`. The
key must exist in all five languages. nl and en are final; pl, ro and ar are dev-team drafts waiting
for a native review, listed in [`../i18n/public-pages-review.md`](../i18n/public-pages-review.md).
Resource values never contain HTML — links are composed in the markup.

## Processor and retention catalogs

- A new processor is a row in `LegalProcessors` with purpose, data, location and transfer basis, plus
  `Status = Planned` (and a `PlannedNoteKey`) while the integration is not live yet — that is how
  Pingen was listed before letter verification shipped (Dependency G). `PrivacyStatementTests` keeps
  the rendered table and the catalog in sync.
- A retention period is **never typed** in the privacy text: add the constant to `PrivacyConstants`
  and a row to `LegalRetention`. `LegalRetentionCatalogTests` checks that every constant has a row.
