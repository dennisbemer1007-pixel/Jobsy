# 05. Mijn link & materiaal: link, code, QR, materials, pitch, real prices, flyer endpoint

Read `00-README.md` first (§0, §IA, §R, D2, D14, Dependencies F). Branch `cursor/salesmanager-5` from `cursor/salesmanager-4`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-5` (from `cursor/salesmanager-4`) |
| PR title | `feat(sales): Mijn link & materiaal — QR, materials, pitch, real token prices, safer flyer endpoint` |
| PR body starts with | `Stacked on #<PR 04> (cursor/salesmanager-4)` + a price table (pack → € per token as shown) |
| Mockups | `sm-d2-link-materiaal.png`, `sm-m3-link-qr.png` |
| Split seam | **05a** = page + `SalesPriceQuote` + flyer/prijskaart/visitekaartje + endpoint fix (05.2–05.5). **05b** = presentation PDF + e-mail/WhatsApp texts + pitch (05.6, 05.7) |

## Goal
A salesmanager has everything to sell on one page: their link, code and QR, ready-made materials with their code built in, a 60-second pitch, what the employer gets and what they earn, with **correct prices** taken from the real token packs.

## 05.1 Today (verify first)
- `SalesToolkit.razor` (moved to `/sales/link` in 01): code, flyer PDF via `api/sales-commercial/flyer.pdf?trackingCode=` (`JobsyApiClient.Sales.cs` L101), WhatsApp/mail share, partner link `/partner/{code}` (L99), "Actuele tarieven" from `SalesCommercialSettings.BaseTokenValueEuro` (€ 25).
- Prices: `TokenPricing` packs (1/5/10/50/100; about € 5 → € 3 per token), `SalesPackage` (≈ € 17,50–20 per token), `BaseTokenValueEuro` (€ 25), used by `SalesCommercialService` L73/82/104/114 for the public catalog and `PartnerSales.razor`. Three different "token prices" (admin redesign D10).
- `SalesCommercialController.GetFlyerPdf` (L35): `[AllowAnonymous]`, renders **any** well-formed code (SM-, BM-, IM-) without checking it exists.
- Ambassadeur toolkit: `Components/Pages/Ambassadeur/Toolkit.razor` + `AmbassadeurFlyerPdfService` (`api/ambassadeurs/me/flyers/{kind}`). Parked since 01.10; don't touch or reuse it here.

## 05.2 SalesPriceQuote (D14)
- `ISalesPriceQuote` (Infrastructure): from **active** `TokenPricing` packs → `MinPricePerToken`, `MaxPricePerToken`, `Packs[]` (size, price ex VAT, € per token), and for the common actions (publish a vacancy, highlight, from `TokenSpendCost` / `VacancyTypeTokenCost` as the werkgever "Wat kost wat?" panel reads them) the token cost and "vanaf € x" (= tokens × `MinPricePerToken`).
- Used by: this page, the prijskaart and flyer PDFs, the pitch step "Wat het kost", and the public `/partner/{code}` landing + `GET api/sales-commercial/catalog` (switch their € amounts from `BaseTokenValueEuro` to the quote; the DTO shape stays, values change). `BaseTokenValueEuro` stays in the data and the admin form (no data change); add the admin-side hint "Wordt niet meer getoond aan werkgevers of salesmanagers; prijzen komen uit de tokenpakketten." and list it in the PR under "Overlap for Dennis" (admin D10).
- `SalesPackage` prices are shown only on the prijskaart section "Pakketten via je salesmanager" when active packages exist, with their own price; never mixed into "per token".

## 05.3 Page `/sales/link` (`sm-d2`)
- Header "Mijn link & materiaal", lead "Deel de link. Meldt een werkgever zich aan? Dan hoort hij bij jou en krijg je commissie op zijn aankopen."
- Card "Jouw persoonlijke link": mono field `lobsy.nl/p/{code}` + primary "Kopieer"; "Of geef je code: **{code}**" + pill "Actief"; buttons WhatsApp (`https://wa.me/?text=` with the WhatsApp text 05.6), Mail (`mailto:` with subject/body 05.6), QR downloaden (PNG 1024 px, `?b=qr`), "Bekijk wat de werkgever ziet" (opens `/partner/{code}` in a new tab, no click counted: add `?preview=1` which the landing excludes from counting). Info box: "Zo tellen we een aanmelding voor jou. Klikt iemand op je link? Dan onthouden we dat {AttributionCookieDays} dagen. Of de werkgever vult je code in bij het aanmelden."
- QR card: QR (`SalesQr`, URL `/p/{code}?b=qr`), caption "Scan voor lobsy.nl/p/{code}".
- "Materiaal" grid (6 cards, icon + title + one-line help + type tag + one action; lead "Alles staat al klaar met jouw code"):
  1. Flyer A4 met QR · PDF · Download (05.4)
  2. Visitekaartje met QR · PDF · Download ("Voor op tafel of in je tas. 10 per A4.")
  3. Presentatie voor een klant · PDF · Download (05.6)
  4. E-mail om te sturen · Tekst · Kopieer
  5. Prijskaart · PDF · Download
  6. WhatsApp-bericht · Tekst · Delen
- Right column: "Pitch in 60 seconden" (4 numbered steps: Het probleem · Wat Lobsy doet · Wat het kost (from the quote) · Zo start u; plus a tip line), "Wat krijgt de werkgever?" (checklist: "Gratis start-highlight op de eerste vacature ({StartHighlightBonusTokens} tokens)", "Eén vast aanspreekpunt: jij", "Geen abonnement"), "Jouw commissie" (three tiles Jaar 1/2/3 with the **beneficiary's own** rates: 25/10/5, or 20/10/5 for a recommended salesmanager; sub-line "Over elke tokenaankoop van jouw werkgevers, excl. btw. Jaar 1 start bij de eerste aankoop.").
- Mobile (`sm-m3`): QR first (large), code + short link, three buttons (WhatsApp, Mail, Kopieer), the employer-benefit line, "Materiaal" list (Flyer, Pitch; "Alles" opens the full list).

## 05.4 PDFs (QuestPDF, on demand, never stored)
- `ISalesMaterialsPdfService`: `FlyerA4(code)`, `BusinessCards(code)` (10 per A4, crop marks), `PriceCard(code)`; all with the QR (`?b=flyer` for flyer/cards) and prices from `SalesPriceQuote`, strings from `SalesPdf.*`, Lobsy logo, tokens-based colours (the PDF palette the existing flyers use).
- `GET api/sales/me/materials/{kind}.pdf` (`kind` = `flyer|visitekaartje|prijskaart|presentatie`), beneficiary from the signed-in user; file names fixed (`lobsy-flyer-{code}.pdf`). Rate-limited (`public-pdf` limits reused).
- The old personal flyer path in `SalesToolkit` is replaced by this endpoint.

## 05.5 Public flyer endpoint fix
- `GET api/sales-commercial/flyer.pdf` stays anonymous for the **generic** flyer (no code). With `trackingCode`: render the personal flyer **only** when the code belongs to an active beneficiary or active BM/IM partner; otherwise **404** with a generic message ("Deze code kennen we niet."), same timing path (no enumeration oracle beyond what the public landing already reveals). Keep the rate limit. Check who calls it with a code (`JobsyApiClient.Sales.cs` L101 and the partner pages) and keep those working.

## 05.6 Presentation + texts
- `presentatie.pdf`: 7 landscape pages (QuestPDF): Welkom (with the salesmanager's name and company) · Het probleem · Zo werkt Lobsy (banenkaart, 1-tap solliciteren) · Wat kost het (quote) · Wat u krijgt (start-highlight) · Zo start u (QR + code) · Contact (the salesmanager's name, company and account e-mail; no phone number).
- E-mail text (subject + body) and WhatsApp text: `Sales.Materials.EmailSubject/Body`, `…WhatsApp`, with `{link}` placeholder filled by the server; B1, max 80 words; the PR shows both texts for Dennis.

## 05.7 Pitch content
- 4 steps + tip as strings (`Sales.Pitch.*`), reviewed by Dennis in the PR. Step 3 uses the quote: "U betaalt alleen per vacature, met tokens. Vanaf € {min} per token. Geen abonnement."

## Tests
- `SalesPriceQuote`: min/max/per-pack from a seeded pack set; inactive packs ignored; the catalog and `/partner` landing show quote values (not € 25).
- Materials endpoint: own code only; `AM-` codes and ambassadeur accounts get nothing (404 / refused, 01.10); PDFs render (non-empty, contain the code and the `/p/{code}` URL text), no stored files.
- Public flyer: no code → generic 200; active code → 200; unknown/inactive code → 404; BM/IM partner code → 200 (regression).
- Preview link not counted as a click.
- bUnit: copy/share buttons, QR download, commission tiles show 20 % for a recommended salesmanager.

## Success criteria
- `dotnet build` + `dotnet test` green.
- No sales surface (toolkit, flyer, prijskaart, pitch, `/partner` landing) shows `BaseTokenValueEuro`; every price equals a value derivable from the active token packs.
- Every material carries the beneficiary's own code and QR.

Done → next: `06-profiel-afspraken-iban.md`.
