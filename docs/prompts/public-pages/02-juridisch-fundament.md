# 02. Legal foundation: LegalDocument component, versions, "In het kort" strings, processor + retention catalogs, legal routes on the public layout

Read `00-README.md` first ("How to run", §0, §IA, D1, D3, D15–D17, Dependencies A). Branch `cursor/public-pages-2` from `cursor/public-hotfix` (or `origin/acceptatie` when PR 01 is merged).

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-2` from `cursor/public-hotfix` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-hotfix)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-2`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - Run the Dependencies checks A–H first and write the outcome at the top of the PR body.

| | |
|---|---|
| Branch | `cursor/public-pages-2` |
| PR title | `feat(legal): LegalDocument foundation (TOC, In het kort in 5 languages, versions, print), processor and retention catalogs, legal pages on the public layout` |
| Mockups | `pb-d03-privacy`, `pb-m03-privacy`, `pb-m08-privacy-arabisch-rtl` (shell, TOC, "In het kort" block, identity card, version line), `pb-d05`/`pb-m05` (audience switch look) |
| Migration | none |
| Split seam | **02a** = component + CSS + strings + layout wiring; **02b** = `LegalProcessors` + `LegalRetention` catalogs + tests |

## Goal
One way to render every legal document: warm public layout, readable on a phone, a summary in your own language, one version constant, and data tables that come from code, not from copy.

## 02.1 Today (verify first)
- `Pages/Legal/Privacy.razor` (241 lines), `AlgemeneVoorwaarden.razor` (172), `Gebruiksvoorwaarden.razor` (144) render under `MainLayout` with `login-page`/`login-card` markup.
  - The dates are typed by hand: Privacy L12 "26 september 2026", AV L11 and GV L11 "2 augustus 2026".
  - `Legal.DocNote` says the text is Dutch only.
- There's no `#cookies` anchor (the landing spec links `/privacy#cookies`).
- `PrivacyConstants` (`Jobsy.Core/Privacy/PrivacyConstants.cs`) holds the retention constants (L13–42).

## 02.2 Layout and render mode
- The 3 legal routes get `[ExcludeFromInteractiveRouting]` and `@layout PublicLayout` (Dependency A present) or `LegalPublicLayout` (absent, see README).
- The existing `@page` routes stay. `PageSeoCatalog` entries stay `Public` (index).
- **Content in 02:** today's text moves unchanged into `LegalDocument` sections (each existing h2 becomes a `LegalSection`; the hand-typed dates become the 02.4 constants). The "In het kort" blocks use the 02.5 keys; where today's section doesn't map to a new key yet, it gets no summary block. 03 and 04 replace the text.
- The logged-in app is untouched: a signed-in user sees the same public page (header shows "Naar mijn start" instead of "Inloggen", as `PublicLayout` does).

## 02.3 `LegalDocument` component (`Components/Legal/LegalDocument.razor`)
Parameters: `DocumentId` (`privacy|terms-employer|terms-candidate`), `Title`, `Lead`, `Sections` (list of `LegalSection { Id, Number, TitleKey, SummaryKey, Body (RenderFragment) }`), optional `Switch` (RenderFragment, used by 04).

Markup (see pb-d03):
- **Hero:** eyebrow chip (emoji + label), h1, lead, meta line "Versie {maand jaar} · geldig vanaf {d MMMM yyyy}" from `LegalDocumentVersions` (culture-formatted, Gregorian, Europe/Amsterdam), plus "Samenvatting in 5 talen".
- **Language note** (only when culture ≠ nl): `Legal.OfficialNote` "De Nederlandse tekst is de officiële versie. De blokken 'In het kort' staan in jouw taal (nl, en, pl, ro, ar)."
- **TOC:**
  - Desktop: sticky left card "Op deze pagina" with numbered links, plus "Mijn gegevens" (`/privacy/data`, privacy only) and "Afdrukken of opslaan als pdf".
  - Mobile: a `<details>` "Inhoud ({n} onderdelen)" above the first section.
- **Section card:** `<section id="{Id}" aria-labelledby>`, h2 "{n}. {title}", then the **"In het kort"** block (mint tint, 💡 aria-hidden, label `Legal.InShort`, text from `SummaryKey` in the reader's language). Then the Dutch body. When culture ≠ nl, the body is wrapped in `<div lang="nl" dir="ltr">` with a small label `Legal.DutchText` "Nederlandse tekst (officieel):".
- **End:** "Wat is er veranderd?" (last 3 entries of `LegalDocumentVersions.History`, D16).
- **Accessibility:** one h1, h2 per section, TOC is a `<nav aria-label>`, the current section is `aria-current="location"` via `public-pages.js` scroll-spy (no JS → plain links).
- **Print (D17):** `@media print` in `public-pages.css`: hide header/footer/TOC/notes, open `<details>`, show URLs after links, black on white. The button calls `window.print()` from `public-pages.js`, rendered only with JS (`<noscript>` hides it).

## 02.4 Versions (`Jobsy.Core/Legal/LegalDocumentVersions.cs`)
- `public static readonly LegalVersion Privacy = new("2026-10", new DateOnly(2026, 10, 1));` and the same for `Terms` (one version for both terms pages).
- `History` per document: `("2026-10", date, summaryKey)`, `("2026-09", 2026-09-26, …)` for privacy, `("2026-08", 2026-08-02, …)` for terms.
- The final date is set by the PR that finishes 03/04. If the stack lands later, bump the version and date in 03/04 (not in 02) and say so.
- `PrivacyConstants.CurrentConsentVersion` stays unchanged (D15).

## 02.5 Strings (`Localization/UiStringsLegal.cs`)
- `Legal.*` chrome: `OfficialNote`, `InShort`, `DutchText`, `Toc`, `TocMobile` ("Inhoud ({0} onderdelen)"), `Print`, `VersionLine`, `Changes`, `MyData`, `IdentityCard.*` (Naam, Adres, KvK, Btw-nummer, Privacyvragen, Contact).
- `Privacy.Sec.{id}.Title` / `.Summary` and `Terms.Sec.{id}.Title` / `.Summary` for the sections of 03/04. 02 adds the privacy + terms section keys **with final nl/en** texts taken from the mockups (pb-d03, pb-d05, pb-m05), plus pl/ro/ar drafts.
- The ar strings in pb-m08 are a draft; list every pl/ro/ar key in `docs/i18n/public-pages-review.md`.

## 02.6 Identity card + footer legal line
- `Components/Legal/LegalIdentityCard.razor`: a key/value list (Naam, Adres, KvK, Btw-nummer, Privacyvragen or Contact) from `LegalIdentityProvider` (01). A row renders only when its value is set.
- Footer: `PublicLayout` (or `LegalPublicLayout`) shows `© {year} {DisplayName} · {AddressLine} · KvK {KvkNumber}` with empty parts omitted. If `PublicLayout` is present and owned by landing, add it through its footer slot/parameter, or the smallest change to its footer component; say which.

## 02.7 Catalogs (02b)
- `Jobsy.Core/Legal/LegalProcessors.cs`: `record LegalProcessor(string Id, string Name, string Region, string PurposeKey, string DataKey, ProcessorStatus Status, string? TransferBasisKey)`, `ProcessorStatus { Active, Planned }`. Rows are filled in 03 (03.3). 02 adds the type, an empty list and the table component `Components/Legal/ProcessorTable.razor` (columns Partij · Waar · Waarvoor · Welke gegevens; mobile = stacked cards; "nieuw" pill only in the mockup, not in the product).
- `Jobsy.Core/Legal/LegalRetention.cs`: rows built **from `PrivacyConstants`** (and `OneTimeLinkRules`/others when those exist), each `(LabelKey, Func<string> Duration)`. The duration is formatted from the constant ("48 uur", "10 minuten", "30 dagen", "2 jaar" for 730 days, "tot je je account verwijdert"). `Components/Legal/RetentionTable.razor` renders it.
- A test asserts every `public const int …Retention…` in `PrivacyConstants` appears in `LegalRetention` (reflection), so a new constant can't be forgotten.

## 02.8 Tests
- `LegalDocumentRenderTests` (bUnit): TOC links match section ids; "In het kort" uses the culture; the nl body has `lang="nl"` when culture = ar; the page root has `dir="rtl"` for ar; no `style=` attributes.
- `LegalVersionsTests`: History is sorted descending, current = History[0], the version line formats per culture (nl "oktober 2026", en "October 2026", ar Gregorian months).
- `LegalRetentionCatalogTests` (02.7 reflection test).
- `LegalIdentityCardTests`: empty values → no row, no label.
- `PublicPagesCssGuardTests`: `public-pages.css` rules are scoped under `.pub-theme` (or `.pp-theme`), no `!important`, no physical `left/right` properties.
- `LocalizationParityReportTests`, `AssetVersionGuardTests`, `PageSeoTests` green.

## Success criteria
- The three legal routes render as static SSR in the public layout, with no Blazor circuit (no `blazor.web.js` interactive root for these pages).
- Switching to ar gives RTL chrome, ar "In het kort" blocks and an LTR Dutch body.
- No hand-typed date remains in the legal pages; the version line comes from `LegalDocumentVersions`.
- PR body: dependency outcomes A–H, screenshots nl desktop/mobile + ar mobile of a legal page shell.

Done → next: `03-privacy.md`.
