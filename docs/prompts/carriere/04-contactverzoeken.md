# 04: /candidate/talent-contacts: contact requests in the journey style + T1–T5

> **Rules (repeated in every file):**
> - Branch from `origin/acceptatie` (file 01) or from the previous file's branch (stacked). ONE PR per file, always into `acceptatie`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`). Never push to `main` or `acceptatie`. No force-pushes. Push only `cursor/carriere-*` branches.
> - Red build/tests or an unmet success criterion: push, open that PR as **draft**, stop and report. Don't start the next file.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/carriere-4` from `cursor/carriere-3` |
| PR title | `Carrière 04: contact requests restyle, share confirm, honest privacy copy, decline reason, Werkgevers gate` |
| Body starts with | `Stacked on #<PR 03> (cursor/carriere-3)` + re-check outcome A, D |
| Mockups | `cr-d6-contactverzoeken`, `cr-d7-contact-delen-bevestigen`, `cr-m6-contactverzoeken` |
| Split if too big | `04a` = §1 + §4 (server: reason, preview, codes), `04b` = §2–§3 + §5 (UI) |

**Goal:** a candidate sees clearly who wants to talk to them and what happens with each answer. **Nothing is shared without a clear "ja"**, and the dialog shows exactly what is shared. The lobster listens with its antennas: "someone thinks you fit — you decide". The copy is true: after 2 days nothing is shared automatically.

Closes: T1, T2, T3, T4, T5.

## 1. Server
- **Decline reason (D14):** `TalentContactRequest.CandidateDeclineReason` (string, max 24, nullable: `NotInterested` | `AlreadyPlaced`). Migration `AddTalentContactDeclineReason` (existing declined rows stay null = "Geen interesse" in the UI).
  - `CandidateRespondAsync` stores it: `alreadyPlaced` ⇒ `AlreadyPlaced`, else on decline `NotInterested`.
  - The DTO exposes `candidateDeclineReason`.
  - The employer talent-pool view shows the label "Geen interesse" / "Al voorzien" next to the declined status; that's the only employer-side change (find the render site with `git grep -n "CandidateDeclined" Jobsy.Web`). Add the two employer strings in 5 languages.
- **Share preview:** `GET api/me/talent-contacts/{id}/share-preview` (Candidate, own request only, 404 otherwise) returns `{ companyName, name, email, phone }`, **exactly** the fields `ToDtoAsync(revealPii: true)` would reveal to the employer.
  - Implement both through one method (`TalentContactPii.For(user)`) so they can't drift. Unit test that preview == revealed.
  - Missing phone ⇒ `phone: null` and the dialog says "Telefoon: niet ingevuld".
- **Accept requires the confirm token:** `POST …/{id}/respond` with `accept = true` requires `confirmedShare = true` in the body, else 400 `{ code: "confirm_share_required" }`. It protects old clients and scripts from a one-click share.
- **Error codes** instead of messages: `not_found` (404), `cannot_respond` (409, e.g. withdrawn or already answered), `confirm_share_required` (400). Keep the rate limit.
- **Gate (D18, Dependency D):**
  - Present: `[RequiresFeature(PlatformFeature.Employers)]` on `CandidateTalentContactsController`.
  - Absent: `ICareerEmployerGate` check → 404 `{ code: "feature_off" }`.
  - With the gate off, `TalentPoolService` notifications for candidates are not created (if that path exists; `git grep "talent-contacts" Jobsy.Infrastructure`).
- Candidates keep being able to answer in `RefundEligible` (as today; D14). No change to token/refund logic.

## 2. Page (cr-d6 / cr-m6)
- `CandidateTalentContacts.razor`:
  - Replace the `profile-page__header` wrapper (hidden at every width = T1) with the journey shell `journey-page career-page` and `CareerStage`, using the `Listen` scene variant.
  - Left card (desktop ≥ 1024; mobile = a disclosure "Zo werkt het" under the list), h2 "Zo werkt het", subline "Jij beslist, altijd.", four lines with icons:
    - antenna "Een werkgever ziet je paspoort, maar niet je naam, e-mail of telefoon."
    - shield "Pas als jij **ja** zegt, krijgt die werkgever je naam, e-mail en telefoon."
    - clock "Reageer het liefst binnen 2 dagen. Zeg je niets, dan wordt er niets gedeeld."
    - eye-off "Nee zeggen mag. De werkgever ziet alleen “geen interesse”."
    - (D8: "paspoort" → "profiel" when the paspoort flag is off/absent.)
  - Card: eyebrow "Contactverzoeken", **visible** h1 "Een werkgever wil je spreken" (none open: "Contactverzoeken"), lead "Werkgevers zien je naam, e-mail en telefoon pas als jij ja zegt. Nee zeggen mag altijd." (fixes T2).
  - Request cards (open first, then newest):
    - initial avatar, company name (or "Een werkgever" when unknown), status pill, the employer message as a quote
    - **Open** (Pending/RefundEligible): pill "Wacht op jou" (clock); "Reageer het liefst vóór {date}" (§3); actions primary "Ja, deel mijn gegevens" (→ dialog §4), secondary "Ik heb al werk", text "Geen interesse" (both decline immediately with the reason and show a line "Je zei nee. Er is niets gedeeld.").
    - RefundEligible adds nothing scary. The when-line becomes "De tijd is om, maar je kunt nog reageren."
    - **ContactShared:** pill "Je zei ja" (success) + "{date} · Je naam, e-mail en telefoon zijn gedeeld. De werkgever neemt contact met je op."
    - **CandidateDeclined:** pill "Je zei nee" (neutral) + "{date} · Er is niets gedeeld." (+ "· Je had al werk" for AlreadyPlaced).
    - **WithdrawnRefunded:** pill "Gestopt" (neutral) + "De werkgever heeft het verzoek ingetrokken. Er is niets gedeeld."
  - Status text is a localized map (`TalentC.Status.*`), never `row.Status` (T4).
  - Empty: the lobster listening, "Nog geen contactverzoeken. Maak je paspoort sterker, dan vinden werkgevers je sneller." + a link to the paspoort (or the profile, D8).
  - Bubble: "Mijn antennes trillen: iemand vindt dat je past. Jij beslist of je ja zegt." (none open: "Ik luister mee. Komt er een verzoek, dan zie je het hier.").
  - Back link: none in the card (the nav and paspoort link are enough); `Competency.BackToProfile` is no longer used here.
  - Mobile (`cr-m6`): band with the listening lobster, card, no sticky footer (actions are inside each request; buttons full width, stacked, primary first).
- `ex.Message` removed; codes → `TalentC.Err.*`; unknown → `Common.Error` + log.

## 3. Time and dates (T4)
- Format with a new `LobsyTime.ToAmsterdam(DateTime utc)` helper (Web; `TimeZoneInfo` "Europe/Amsterdam" with an IANA/Windows fallback, as `FreePublishRules` does) and the **current UI culture**: `ddd d MMM, HH:mm` for the deadline, `d MMM` for history. Never `ToLocalTime()` (server zone).
- Unit tests: a UTC time in CEST and CET, and all 5 cultures produce a non-empty string without exceptions (`ar` included).

## 4. Share confirm (cr-d7) — T3, D14
- `LobsyFriendlyDialog` with the lobster (56 px) and h2 "Je gegevens delen met {company}?", small "Dit krijgt {company} als je ja zegt:".
- A `<dl>` with Naam / E-mail / Telefoon from the **share preview** (loaded when the dialog opens; loading state; error ⇒ the dialog shows `TalentC.Err.Preview` and no primary button).
- Hint (shield): "Delen kun je niet terugdraaien. De werkgever mag je daarna bellen of mailen over dit werk."
- Buttons: secondary "Nog niet" (closes, nothing sent) · primary "Ja, deel mijn gegevens" → respond with `accept = true, confirmedShare = true`. Busy state; double click can't send twice.
- After success: the card turns into "Je zei ja", live region "Je gegevens zijn gedeeld met {company}.", focus back to the card heading.
- No auto-yes path; Esc/close = no.

## 5. Gate, links and strings
- **Werkgevers OFF (D18):** the page shows `FeatureRouteGate` (Dependency D present) or a calm fallback card "Contactverzoeken staan nu uit." with a link to the paspoort (absent). The link in `Profile.razor` (L637–640) and any paspoort link are hidden. No nav change.
- Strings: new `UiStringsTalentCandidate.cs` (`TalentC.*`) in nl/en/pl/ro/ar. The old candidate `Talent.*` keys that are no longer used (`CandidateLead`, `Declined`, `Accept`, `AlreadyPlaced`, `Decline`, `Accepted`, `CandidateEmpty`, `CandidateTitle`, `RespondBy`, `Status`): `git grep` each. Remove only candidate-only ones that have no other user; employer keys stay.
- `PageHelpDocs` + `PageSeoCatalog` entries updated (private).

## Tests
- API:
  - accept without `confirmedShare` → 400 code; with it → shared
  - share-preview own vs foreign (404); preview == revealed fields
  - decline reasons stored; employer DTO shows the reason
  - gate off → 404/feature page
  - `cannot_respond` for withdrawn
- bUnit:
  - h1 visible (no `profile-page__header` wrapper; assert the h1 isn't inside an element with that class)
  - status pills per status; the RefundEligible copy
  - dialog (lists exact preview, "Nog niet" sends nothing, primary sends once)
  - dates via `LobsyTime`; empty state; gate off
  - no `ex.Message`; `ar` `dir="rtl"`
- The guard tests from 02 are extended to this page.

## Success criteria
- Matches `cr-d6`, `cr-d7`, `cr-m6` (nav excepted).
- The h1 and lead are visible at 390, 768, 1024 and 1440 px.
- No copy claims that details are shared automatically; sharing only after the dialog, with the exact data shown.
- Times are Amsterdam time in the UI culture; statuses are words, not enums.
- With Werkgevers OFF, neither the page nor its link are reachable for candidates.
