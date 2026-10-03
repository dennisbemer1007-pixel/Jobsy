# Changelog: Jobsy

## Unreleased

### Added
- Carrière 01 (Web): career API wiring on `/carriere` — fit bands, action kinds, error codes, dream-options/archive client stubs; `UiStringsCareer` for nl/en/pl/ro/ar.
- Carrière 02 (Web): `/carriere` in the ontdekkingsreis style — `CareerClimbScene` with the lobster climbing stone by stone to the golden dream stone, `CareerRail`, `GrowingShellsStepper`, empty state with real job suggestions + job search, overview with "nu aan de beurt", and a calm dream-change dialog that keeps what you achieved (archive restore from the UI). Removes `HorizonArt`, the native `window.confirm`, the blur-commit dream input, the datalist and every percentage; new `features/carriere.css` (`?v=20260930-carriere`) and copy in nl/en/pl/ro/ar incl. `ar` RTL.

- Carrière 04 (Web + API): `/candidate/talent-contacts` in the journey style — visible h1 "Een werkgever wil je spreken", an honest lead, a "Zo werkt het" card (desktop rail, mobile disclosure) and per-request cards with word statuses ("Wacht op jou", "Je zei ja", "Je zei nee", "Gestopt") instead of enum names. Sharing now takes an explicit yes: the share dialog loads `GET api/me/talent-contacts/{id}/share-preview` and lists exactly the naam / e-mail / telefoon the employer will get (missing phone reads "niet ingevuld"), and `POST …/respond` with `accept` requires `confirmedShare`, else `400 confirm_share_required`. Declining stores why (`NotInterested` / `AlreadyPlaced`, migration `AddTalentContactDeclineReason`) and the employer talent-pool view shows "Geen interesse" / "Al voorzien". Dates are Amsterdam wall-clock time in the UI language via `LobsyTime` (never `ToLocalTime()`); API failures surface as codes (`not_found`, `cannot_respond`, `confirm_share_required`), never raw exception text. Werkgevers OFF gates the controller and the page and hides the link on `/candidate/profile`. New `UiStringsTalentCandidate` (`TalentC.*`) in nl/en/pl/ro/ar incl. `ar` RTL; `features/carriere.css` `?v=20260930-carriere4`. Closes T1–T5.

- Carrière 05 (Web + API): `/candidate/hoe-werkt-lobsy` is now five stones in the journey style — "Vijf stenen, in je eigen tempo" with one row per stone in Dennis' order (De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties), a state and a link per row, and the lobster standing on the stone you are on. Done state comes from the new read-only `GET api/me/journey-summary` (Candidate-only, no writes): onboarding finish, a paspoort proof, an active plan with a completed step, a liked/shared vacancy, an application. The stone set lives in the pure `CandidateHowStones` (never read from the nav): Werkgevers OFF drops Banenkaart and Sollicitaties (the h1 count follows), the paspoort flag off swaps stone 2 for "Mijn profiel", and an unknown route drops its stone with a warning instead of linking to a 404. A "Veilig en rustig" card (desktop rail, mobile disclosure) names all five languages in their own language with `lang`/`dir`. New `UiStringsHowLobsyCandidate` (`HowC.*`) in nl/en/pl/ro/ar; `features/carriere.css` `?v=20260930-carriere5`. Closes B14 (this page).

- Carrière 06 (tests + docs): the carrière stack (01–06) is closed off. `CarrierePlaywrightTests` walks the eleven candidate flows end to end on desktop 1440 and mobile 390, in nl/en/pl/ro/ar incl. `ar` RTL, with reduced motion and with Werkgevers OFF, and soft-skips without `JOBSY_E2E_BASE_URL` (same contract as the other Playwright suites). Screenshots land in `artifacts/e2e/carriere`. Cleanup: the unused Horizon dashboard CSS, the `career-dash` rules and the orphaned `LobsyToast` component are gone (`app.min.css?v=20260930-carriere6`), and `POST api/me/career-path/courses/claim` keeps its `410 use_passport_proof` stub until 2026-10-30. `carriere.css` now has a page-weight budget guard. Docs: `docs/ROUTES.md` carrière notes, thirteen candidate scenarios in `docs/TESTSCENARIOS_PER_ROL.md` / `docs/testscenarios-per-rol.csv`, and the stack report in `docs/reports/carriere-stack-report.md` with the native-review strings in `docs/i18n/carriere-review.csv`.

### Added
- Scholen vragensets 03b: results and totals strictly per test. `PupilResult.ScoringVersion` writes the class test’s version (`g78-1` / legacy `1`); migration `AddQuestionSetToAggregates` adds `QuestionSet` on class/year aggregates (backfill + G78 scoring remap); snapshotter and admin/school/teacher views never mix or compare G78 with VO; admin rapportage requires a vragenlijst filter and exports `…-groep78.csv` / `…-vo.csv`.

- Scholen vragensets 02: class level picks the question set. `SchoolLevel.Groep78`, `PupilQuestionSet` on `SchoolClass` (never on pupil codes), `SchoolLevelRules`, migration `AddClassQuestionSet` (existing classes → VO), school portal create/edit form with Basisschool/VO picker and lock rule once codes have started (`409 level_locked`), readable labels everywhere, seed class `7A`.

### Fixed
- Scholen hotfix (vragensets 01): never leave a pupil `Completed` without a `PupilResult` when the result builder fails after the last answer. `SaveAnswerAsync` builds first (status stays `InProgress` + `ResultPending` on failure); login/progress/result self-heal via `EnsureResultAsync`; pupil UI shows “We maken je verhaal klaar” with retry; teacher detail shows “Bezig met afronden”. Group results still read `PupilResult` rows only.

- Carrière 05 (Web): the candidate how-to guide no longer renders the literal `_message`, is `[Authorize(Roles = "Candidate")]` instead of also allowing employer roles and Admin (other roles already land on `/hoe-werkt-lobsy` via `RoleNavCatalog.HowLobsyHrefFor`), and a failed save now keeps you on the page with "Dat lukte niet. Probeer het straks opnieuw." in a `role="alert"` line instead of navigating away with raw exception text. The shared `HowLobsyGuidePanel` CTA dropped its inline `style` for a `how-lobsy-cta` class, and `HowLobsy.*` lost the "Lik" typo and the stale question counts ("korte vragenlijsten" in all five languages).

- Carrière 03 (Web): step detail on `/carriere?stap={n}` (deep-linkable, back button works) with "Wat je nog mist" in claws, a fit band instead of a percentage, real courses (free first, at most one labelled Partnerlink) and "Voeg bewijs toe" into the paspoort Bewijzen tab. Completing a step is now a warm moment in the scene — the old shell falls, the lobster grows a gold new shell and moves up a stone — instead of a toast, announced politely with focus on the new heading, with "Toch nog niet klaar" right there. Werkgevers OFF hides every vacancy link, count and "Nieuw: … vacatures" line; never "0 jaar", never a clickable AI course name. Copy in nl/en/pl/ro/ar. Closes B6 (step), B7 (UI), B8, B9, B13 (step copy), B15.

### Security / legal
- Public-pages 10 (einde van de stack): wat een bezoeker hiervan merkt — de publieke en juridische pagina's (`/hoe-werkt-lobsy`, `/wie-zijn-wij`, `/privacy`, `/algemene-voorwaarden`, `/gebruiksvoorwaarden`, `/partner`, `/{kvk}`, `/melden`, `/privacy/data`) laden nu als volledige HTML in vijf talen (ar van rechts naar links), met een samenvatting "In het kort" in je eigen taal naast de officiële Nederlandse tekst, een bedrijfspagina die alleen bestaat voor een KvK-geverifieerd bedrijf met een echte vacature, een meldknop op vacature- en bedrijfspagina's, en "Mijn gegevens" als bestand dat je downloadt in plaats van JSON op het scherm. Twee bugs die de nieuwe tests vonden zijn meteen gefixt: een onbekende URL gaf wel een 404-status maar een **lege pagina** (de re-execute naar `/status/{code}` vond geen endpoint meer omdat routing impliciet bovenaan de pipeline stond — nu staat `app.UseRouting()` expliciet ná `UseStatusCodePagesWithReExecute`), en `/partner/{code}` zette `robots: index,follow` zodra de code geen geldige salescode was, waardoor een duplicaat van `/partner` indexeerbaar werd (de noindex hangt nu aan de routeparameter). Nieuw: `PublicPagesPlaywrightTests` (9 pagina's × 5 talen × 2 schermbreedtes, screenshots naar `artifacts/playwright-public/`, soft-skip zonder `JOBSY_E2E_BASE_URL`) en `PublicPagesHttpTests` (echte 404's, geen ids/adres/coördinaten in de publieke bedrijfs-JSON, noindex + canonical, sitemap zonder Pending/Failed/concept-bedrijven, geen `ex.Message` in de pagina's van deze stack). Docs: `docs/legal/README.md`, een afgeronde `docs/legal/review-needed.md` met de advocatenlijst, `docs/public-pages-followups.md` en het eindrapport `docs/reports/public-pages-stack-report.md`.
- Public-pages 09 (partner + bedrijfspagina): `/partner` (and `/partner/{code}`) is static SSR on `PublicLayout` with B1 copy — "Vind personeel dichtbij.", three plain-language usp's and a tariff table that leads with the amount **exclusief btw** and names the incl.-btw amount in a smaller line ("Bedragen zijn exclusief 21% btw…"). The stored catalog and pack amounts are the Mollie amounts and therefore incl. btw (`MolliePaymentService.ResolvePackPriceAsync`, `TokenVatPricing.SplitInclVatEuros`), so the excl.-btw column is derived for display and no stored price changed. A type that costs 0 tokens now reads "Gratis" instead of "€ 0,00", and the jargon is gone: `Partner.Usp1`/`Partner.Usp2` (page **and** `PartnerFlyerPdfService`) no longer say "reistijd-matching" or "Funda-model" but "Kandidaten uit de buurt, op reistijd" and "Je vacature op de banenkaart, met een highlight erbij". The three share actions (WhatsApp, mail, flyer) are plain links with no JS, so the flyer download became the new anonymous, rate-limited `GET /partner/flyer.pdf` endpoint. Both partner routes and `/{kvk}` carry `[RequiresFeature(Employers, FallbackPath = "/")]`, so with werkgevers-actief OFF a visitor gets a real 302 to `/` and `/partner` leaves the sitemap.
- Public-pages 09 (bedrijfspagina): `/{kvk}` and `/{kvk}/{vestigingsnummer}` moved from the map chrome to `PublicLayout` (prerendered, list in the first HTML, map after hydration). The page has a breadcrumb back to the banenkaart, a header card with the city, the vacancy count and a "KvK-geverifieerd" chip, an emoji tile from the vacancy category when no logo was uploaded, and vestiging tabs for the branches that actually have a public vacancy. Its JSON-LD `Organization` now takes the origin from the configured `PublicWebBaseUrl` instead of the request Host (`PageSeoResolver.ConfiguredOrigin`) and publishes `addressLocality` only — never a street address, internal id or coordinate (file 01). Every not-found case (unknown or unverified KvK, no public vacancy, failing API) is one real 404 that renders the shared status page body, extracted as `StatusPageContent` so `/status/{code}` and this page cannot drift apart. The inline engagement-claim form is replaced by the DSA link "Klopt er iets niet op deze pagina? Meld het." → `/melden?type=company&id={kvk}`. New `PartnerPage.*` and `CompanyPage.*` keys in `UiStringsPublicInfo` in five languages (ar RTL).
- Public-pages 08 (hoe werkt Lobsy + wie zijn wij): `/hoe-werkt-lobsy` is now static SSR on `PublicLayout` (no `blazor.web.js`, no "Laden…"), so crawlers and slow phones get the whole page in the first HTML, and it joined `StaticIndexablePaths` (sitemap, canonical without the `?voor=` query). Nobody is redirected any more: the audience pills "Voor jou · Voor werkgevers · Voor scholen" are plain links (`?voor=jou|werkgevers|scholen`, server-rendered tab with `aria-current`), a signed-in candidate sees "Voor jou" plus a "Naar je start" button (`FeatureRoutes.HomeFor`), and staff roles get their existing role guide server-side as "Voor jou (je rol)" — the Sales/Ambassadeur tracking code is fetched on the server and the guide renders without the code when that call fails. Content follows `pb-d01`: four step cards (banenkaart · `/ontdek` · `/account-maken` · solliciteren), "Dit beloven we je" and three FAQ items whose price comes from the existing public `api/public/landing-price` (never hard-coded) and whose age answer is the shared `AgeRulesText`. With werkgevers-actief OFF the employer pill disappears and step 1 becomes "Maak je paspoort" with no job-map link. New `HowLobsy.*` keys in `UiStringsPublicInfo` in five languages (ar RTL); the old `HowLobsy.Guest.*` keys are gone and the staff role guides now fall back to English instead of Dutch for pl/ro/ar (`docs/i18n/public-pages-review.md`).
- Public-pages 08 (wie zijn wij, D9/D10): `/wie-zijn-wij` is a static page on `PublicLayout` in five languages instead of admin-editable HTML from the database — hero, three story cards (kreeft · Westland · twee kanten), the founder card (`AboutAssets` manifest: the photo when `wwwroot/images/about/founder.webp` lands, else the emoji avatar) and a contact card with `mailto:` from `Legal:SupportEmail` plus the `LegalIdentityCard`; `privacy@` is only referenced as "read the privacy statement". The admin text editor is removed with it: `/admin/content/paginas` is now just "Werkgeversflyer", `/admin/about` answers 301 → `/admin`, and `GET api/site/about`, `GET/PUT api/settings/about`, `IAboutPageSettingsService`/`AboutPageSettingsService`, the seeder default and the Web client methods are deleted. The `AboutPageSettings` entity and table stay `[Obsolete]` until the cleanup migration in `docs/public-pages-followups.md`.
- Public-pages 07 (mijn gegevens): `/privacy/data` is now `PublicLayout` InteractiveServer **prerender: true** (content is in the first HTML; only the delete dialog needs the circuit). The export button links to the new `GET /privacy/data/export` (Web minimal API, `[Authorize]`, rate limit `export` 5/hour/user): it mints a short-lived access token, forwards to the existing `GET api/privacy/export`, and streams the JSON back as `Content-Disposition: attachment; filename="lobsy-mijn-gegevens-{yyyy-MM-dd}.json"` (Amsterdam date) with `Cache-Control: no-store` — the JSON never enters a page or a Blazor circuit; a failed export redirects to `/privacy/data?export=failed` with a translated message. The API's `PrivacyDataService.ExportAsync` now logs `privacy.export` (user id only). The support-access card reads `Privacy.Data.SupportViewed` with Europe/Amsterdam date + time (`AmsterdamTime`) instead of the admin audit string, and hides itself (logged, no raw error) when the API call fails. `UnsubscribeDialog` gained an opt-in `SkipLogoutNavigation` parameter (default off, no behaviour change elsewhere); this page sets it and submits a hidden antiforgery `POST /account/logout` form via JS interop instead of a GET navigation after account deletion. `UnsubscribeDialog`'s own error states no longer show `ex.Message`; they now show the shared `Common.Error.TryAgain` (5 languages). New `Privacy.Data.*` keys in `UiStringsLegal.cs` (5 languages, pl/ro/ar drafts in `docs/i18n/public-pages-review.md`).
- Public-pages 06 (meldknop ⚖️, DSA notice and action): anyone can report a vacancy or a company page in two clicks. New `/melden` (static SSR form, antiforgery, noindex, works without JS) reachable from "Meld deze vacature" on the vacancy detail and "Klopt er iets niet op deze pagina? Meld het." on `/{kvk}`; `POST api/reports` is anonymous, rate-limited (5/hour and 20/day per visitor IP, partition key never stored) and answers the same shape for an unknown or non-public target (stored as `NoAction` + `"target not public"`, no oracle), de-duplicates the same target + e-mail within 24 h and logs `report.created` with the e-mail redacted. Admins decide on `/admin/vacatures/moderatie` → tab "Meldingen": open first then newest, Wat/Reden/Wanneer (Europe/Amsterdam)/Meldingen/Status, masked reporter e-mail (`PersonalDataMasker`), and a drawer with "Geen actie" / "Beperken" (vacancy only) / "Verwijderen" where Beperken and Verwijderen require a statement of reasons; one decision closes every open report of that target and writes an `IAdminAuditLog` `report.decided` row without free text. Verwijderen archives the vacancy or sets `Company.PublicPageBlockedAtUtc`, after which `PublicCompanyQuery` keeps `/{kvk}` and the sitemap out. Three new mails (`ReportReceived`, `ReportDecided`, `ContentRemoved` in 5 languages); the employer mail carries the escaped reason, the date and "Wil je bezwaar maken? Mail {SupportEmail} binnen 6 maanden." and never the reporter's e-mail. Retention via the daily cleanup job: `ContentReportEmailRetentionDays = 30` clears the reporter e-mail after a decision, `ContentReportRetentionDays = 365` purges the report; both show up in the privacy retention table. No IP address is stored. Migration `AddContentReports`. Review list: `docs/legal/review-needed.md`.
- Public-pages 05 (bedenktijd ⚖️, Dependency C present): the checkout waiver checkbox (`DeepAnalysis.razor`) now renders `Terms.Waiver.Checkbox` word for word instead of the separate, differently worded `DeepPay.Waiver` key — one source of truth with the terms. `DeepAnalysisPricing.WaiverTextVersion` (and so `DeepAnalysisCheckout.WaiverTextVersion` on new checkouts) now follows `LegalDocumentVersions.Terms.Version` instead of the literal `"2026-09"`. The order summary links "Lees meer over bedenktijd" to `/gebruiksvoorwaarden#bedenktijd`, and the receipt mail's small print gained a "Voorwaarden versie" fact row. New `BedenktijdGuardTests` (service + controller) prove `waiverAccepted == false` always returns 400 `waiver_required` with zero checkout rows and zero Mollie calls; `BedenktijdUiTests` prove the pay button stays disabled until ticked and the label is identical in nl/en. Review list: `docs/legal/review-needed.md`.
- Public-pages 04 (voorwaarden ⚖️): `/algemene-voorwaarden` and `/gebruiksvoorwaarden` now render one `TermsPage` with an audience switch ("🏢 Voor werkgevers" / "🙋 Voor kandidaten", plain links, `aria-current` on the active one) and share one version from `LegalDocumentVersions.Terms`. Both open with "Wie is Lobsy?" from `ILegalIdentity` (incl. btw-nummer). Employers: tokens and prices merged with "Prijzen op de tarievenpagina staan exclusief btw. Bij het afrekenen zie je ook het bedrag inclusief btw.", the typos "bulkapakket" / "early-adapterkortingen" fixed, the KvK rule of file 01 spelled out, and the liability cap corrected to "wat je in de 12 maanden vóór de gebeurtenis betaalde, met een minimum van € 250" (D6) instead of "of € 250 als dat lager is". Candidates: the shared `AgeRulesText` (13 / 16 / 18 from `CandidateConsentRules`), a new "Betaalde extra's en bedenktijd" section with the one waiver sentence `Terms.Waiver.Checkbox` that the checkout reuses word for word (D7), and consumer-safe liability with no cap. Both get "Iets melden" (DSA notice and action, reasoned decision, objection within 6 months, contact point for authorities). Old section anchors keep working; werkgevers-actief OFF drops the employer terms from the footer while the switch keeps both pills. Review list: `docs/legal/review-needed.md`.
- Public-pages 03 (privacyverklaring ⚖️): privacy statement rewritten in B1 Dutch with “In het kort” per section in 5 languages. Identity from `ILegalIdentity` plus an explicit “geen functionaris gegevensbescherming”; the full processor table is generated from `LegalProcessors` (14 rows, new: Pingen, OSRM/Transitous/Valhalla, OpenStreetMap tiles, web-push services) with a transfer-basis column, and Render now reads **EU (Frankfurt)** instead of “EU/VS”; retention from `PrivacyConstants` including the five periods that were missing; new `#cookies` section with a cookie/storage table and “Statistieken alleen na ‘Accepteer cookies’”; one shared `AgeRulesText` component for 13 / 16 / 18 from `CandidateConsentRules`; the outdated “API-credentials per e-mail” line replaced by the reveal-once link. Old section anchors keep working. Review list: `docs/legal/review-needed.md`.
- Public-pages 02 (juridisch fundament): one `LegalDocument` component for `/privacy`, `/algemene-voorwaarden` and `/gebruiksvoorwaarden` — static SSR on `PublicLayout`, sticky/mobile table of contents, “In het kort” per section in nl/en/pl/ro/ar (ar right-to-left with an LTR Dutch body), identity card from `Legal:*`, print stylesheet instead of a server pdf, and “Wat is er veranderd?”. Version and date come from `LegalDocumentVersions` (no hand-typed dates); the retention table is generated from `PrivacyConstants` and the processor catalog (`LegalProcessors`) is filled in 03. Footer shows the legal identity line.
- Public-pages hotfix: `Legal:*` config + `ILegalIdentity` + `GET api/site/legal` (no placeholders; mail footer via MailOptions); `/{kvk}` only verified KvK with public vacancies (city only, no ids/coords); `/partner/{code}` noindex + canonical; mailto `%0A` fix; real HTML `/status/{code}` 404 + vacancy 404 status.

### Error pages
- **Foutpagina's (errors 01):** eigen `ErrorLayout` op het publieke thema met Lobsy-mascotte, vriendelijke `/status/{code}`-pagina in vijf talen (nl/en/pl/ro/ar + RTL), echte statuscodes via `UseStatusCodePagesWithReExecute` voor HTML-verzoeken, 500-pagina met foutcode `LB-XXXX` (Sentry-tag `support_code`, ProblemDetails `supportCode`), onbekende vacature geeft nu 404 in plaats van 200, en `noindex` op alle foutresponses. Zie `docs/support-codes.md`, `docs/i18n/errors-review.md` en `docs/errors-followups.md`.
- **Geen toegang / 403 (errors 02):** een signed-in gebruiker zonder de juiste rol krijgt nu een echte 403 op de aangevraagde URL (geen redirect naar `/access-denied`) via `OnRedirectToAccessDenied` + de bestaande `/status/{code}`-re-execute. De pagina toont het account alleen uit de auth-cookie claims (naam, gemaskeerd e-mailadres, rol — geen API/DB-aanroep), met "Naar mijn start" en "Inloggen met een ander account" (`/account/logout?reason=switch` → `/login?returnUrl=`). `reason=employers-off` toont eigen tekst zonder wisselknop (Dependency F aanwezig). `/access-denied` blijft bestaan als directe 403-pagina met dezelfde `AccessDeniedView`. Dependency E (auth switch-account) was afwezig; follow-up in `docs/errors-followups.md`.
- **Vacature gesloten / 410 (errors 03):** een vacature die ooit publiek was (archief, vervuld, of einddatum verstreken) geeft nu een `410 Gone` met minimale publieke data (titel, plaats, categorie — geen bedrijfsnaam, contact of beschrijving) in plaats van een platte 404; nooit-live vacatures (concept, in afwachting, toekomstige startdatum) blijven 404. `GET api/vacancies/{id}/similar?limit=3` levert tot 3 vergelijkbare vacatures (zelfde categorie binnen 25 km, anders dichtstbijzijnde binnen 10 km) via de bestaande discovery-index. `/vacancies/{id}` toont bij een gesloten vacature titel/plaats + maximaal 3 vergelijkbare banen in de `ErrorLayout`-stijl, `noindex`, zonder JobPosting JSON-LD. Werkgevers-uit blijft 302 naar `/` (Dependency F, bestaande gate). Teksten `Status.Gone.*` in vijf talen.

- **Te veel verzoeken / 429 en kleine meldingen (errors 04):** een rate-limited HTML-verzoek krijgt nu de vriendelijke pagina "Even rustig aan" met de wachttijd uit `Retry-After` (en één `<meta http-equiv="refresh">`, minimaal 30 s) in plaats van een lege 429; niet-HTML-verzoeken en de API antwoorden met ProblemDetails `{ code: "rate_limited", retryAfterSeconds, supportCode }` plus `Retry-After`. De reconnect-toast ("Verbinding herstellen…", "De verbinding is weg.", "Je sessie is verlopen.", "Opnieuw laden") komt uit de catalogus en spreekt nu alle vijf talen incl. RTL. Nieuw `InlineErrorBlock` ("Dit stukje laadt nu niet." + Opnieuw + foutcode) voor een kaart/lijst/grafiek die faalt, en de circuit-ErrorBoundary toont dezelfde foutcode (Sentry-tag `support_code`). Nieuw `UserFacingError` zet een exception om in een catalogustekst (`Common.Error.*`) plus foutcode en logt hem één keer; `ex.Message` is weg bij de foutpagina's, de publieke pagina's, de layouts en de kandidaatpagina's, en de ratchet `NoRawExceptionMessageTests` (`docs/errors/ex-message-baseline.txt`, 377 → 322) laat de teller alleen nog zakken. Zie `docs/support-codes.md`, `docs/i18n/errors-review.md` en `docs/errors-followups.md`.

- **Onderhoudsmodus (errors 05):** één schakelaar in Platforminstellingen → Functies (`/admin/instellingen`) zet het platform in onderhoud. Bezoekers krijgen binnen 15 seconden een echte `503` met de kalme pagina "We zijn even aan het klussen" in hun eigen taal (nl/en/pl/ro/ar + RTL), plus `Retry-After` (seconden tot de verwachte eindtijd, geklemd op 60–3600, anders 300), `noindex` en `no-store`; de API antwoordt met ProblemDetails `{ code: "maintenance", retryAfterSeconds, expectedEndUtc, supportCode }`. Admins werken door en zien een rode balk met een link naar de schakelaar; `/login`, `/account/*`, `/status/*`, statics, `/robots.txt` en `/healthz` + `/health` blijven ongemoeid (health checks antwoorden altijd 200). De webserver pollt `api/site/status` elke 15 s en houdt bij een API-storing de laatst bekende stand vast (koude start = uit). Optionele verwachte eindtijd (Amsterdamse tijd) en interne notitie (max 200 tekens, nooit publiek); elke wijziging wordt geaudit als `maintenance.on` / `maintenance.off`. Nieuw: `ops/maintenance/index.html` en `ops/maintenance/cloudflare-500.html` (één bestand, vijf talen, geen scripts) voor Render maintenance mode en Cloudflare. Migratie `AddMaintenanceMode`. Zie `docs/onderhoud.md` voor het runbook en de Cloudflare/Render plan check — Dennis beslist en configureert dat zelf.

- **Foutpagina's afgerond (errors 06):** loopt er iets mis, dan krijg je nu altijd een echte Lobsy-pagina in je eigen taal (Nederlands, Engels, Pools, Roemeens of Arabisch, ook van rechts naar links) in plaats van een lege of Engelse browsermelding — op je telefoon net zo goed als op een laptop, en ook als JavaScript uitstaat. Een pagina die niet bestaat zegt dat gewoon en biedt de banenkaart, de gratis test en hulp aan; een vacature die gesloten is laat tot drie vergelijkbare banen in de buurt zien (of de zoekpagina als er niets in de buurt is); gaat er iets bij ons stuk dan staat er een korte foutcode `LB-XXXX` die je kunt doorgeven aan support (met een kopieerknop en een mailtje dat de code al bevat); staan we even stil voor onderhoud, dan vertelt de pagina dat en wanneer we terug verwachten te zijn. Zoekmachines bewaren deze pagina's niet en een gesloten vacature verdwijnt uit de sitemap. Ook opgelost: een onbekende vacature gaf een kale regel "Vacature niet gevonden." midden in de app in plaats van de vriendelijke 404-pagina. Nieuw: `StatusPagesPlaywrightTests` (browsermatrix over alle statuspagina's × 5 talen × 2 schermbreedtes, plus zonder JavaScript) en `StatusPagesHttpTests` (statuscodes, `Cache-Control: no-store`, `noindex`, `Retry-After`, JSON voor API-routes, health checks). Zie `docs/reports/errors-stack-report.md` voor wat Dennis nog zelf moet beslissen of instellen.

### Security
- Auth login redesign (03): `/login` static SSR on PublicLayout (`au-*` theme), honest status/pause cards, configured providers only, Werkgevers-aware "Bedrijf registreren", Account maken links, LoginHint cookie (no e-mail in URL), `AuthFeatures.PasswordResetAvailable=false` until 05.

- Auth roles/policy (02): SalesManager local MFA required (Ambassadeur not); admins blocked from Google / personal Microsoft; stale privileged sessions without MFA re-login instead of dead-end; device session AuthMethod. Migration `AddDeviceSessionAuthMethod`.

- Auth hotfix: per-visitor trusted client IP for Web→API auth rate limits; typed login/2FA failures (`invalid_credentials`, `locked_out`, `rate_limited`, `invalid_code`, `challenge_expired`, `mfa_locked`); visible “Even pauze” lockout with counter reset and max 1 lockout mail / 24 h; unknown-e-mail lockout parity; dummy-hash timing; 2FA attempt limits + TOTP replay block; recovery-code-used mail; CSP-safe MFA scripts; “Blijf ingelogd” off by default. Migration `AddAuthHardening`.

### Ops
- Acceptatie-only test accounts CLI (`dotnet Jobsy.Api.dll test-accounts seed|cleanup|status`): hard deployment guard, `IsTestAccount`/`IsTestData` flags (migration `AddTestAccountFlags`), MFA exemption while the guard is active, test↔real boundary helpers, admin “Testaccount” badge, mail drop for test→real. Passwords only from `TestAccounts__Password__*` env vars. See `docs/deploy-render.md` and `docs/testaccounts-followups.md`.


## Candidate tests stack (02–07)

- Shared `TestQuestionFlow` + `TestDepthRules`, consent gate, 3-change limit, free pages in ontdekkingsreis shell.
- Uitgebreide test: 5 parts, pause points, inline motivation, offer/checkout in shell; free CTA first on TestDetail.
- Five languages + RTL; deep items via embedded JSON; review CSVs for native pl/ro/ar.
- Playwright soft-skip suite + stack-end report (`docs/reports/tests-stack-end.md`).

## Unreleased

- **E-mail stack (look & safety):** one-button catalog layout, language-aware copy (nl/en/pl/ro/ar + RTL), optional-mail unsubscribe (List-Unsubscribe One-Click), safer links (set-password / API-key reveal / parental consent without secrets in mail), admin preview with fake data and limited test send. See `docs/emails.md`.
- **E-mail 03 (verzending/headers/afmelden):** multipart text+HTML, From/Reply-To via config (`Lobsy <hallo@mail.lobsy.nl>` / `support@lobsy.nl`), RFC 8058 List-Unsubscribe One-Click for optional mails only, `EmailOptOut` + `/mail/afmelden` + `/account/mail-instellingen`, migration `AddEmailOptOuts`, `docs/email-deliverability.md`.
- **E-mail security hotfix:** parental consent only via website POST (`/toestemming`); single-use set-password links replace mailed temporary passwords; API-key reveal-once links (`/koppeling/sleutel`); escaped support-access mail with Europe/Amsterdam expiry; migration `AddOneTimeLinks`.
- Werkgever-aanmelding afronding (11): dashboardbanner “Nog niet zichtbaar voor kandidaten”, checklist + zichtbaarheidspanel, briefcode vanaf het dashboard, suggesties voor nieuwe KvK-vestigingen, privacytekst voor werkgeversaamelding (incl. Pingen), docs en E2E-dekking.
- Maatschappelijke betrokkenheid (09): 6 engagement claims with optional proof, honest labels (Door werkgever opgegeven / Gecontroleerd), admin moderation tab, badges on company page + vacancy cards (max 2), match bonus max +5; `api/companies/{id}/engagement`; SBB auto-check deferred (no public open data).
- Over je bedrijf (08): company branches (max 4, SBI prefill), Zo werken wij sliders → Cultuurscan answers (`Source=Quick`), 3 kernwaarden → `CompanyValuesProfile` in Match; vacancy inherits with per-team pillar override; `/register/bedrijf` + `api/companies/{id}/profile-extras`.
- Access requests (07): `/register/toegang`, BM inbox + day-3 reminder / day-5 admin escalation, ownership transfer = letter + admin, claim unmanaged (intermediary-only) companies, intermediaries keep client link on takeover/claim. Dependencies G still ABSENT.
- Employer verification (06): business e-mail domain match or Pingen/stub letter code, manual check + admin queue `/admin/werkgeververificatie`, FreeMailDomains blocklist.
- Employer registration wizard (search → vestigingen → account + code) with WA theme fallback, referral resolver, geocode-safe manual entry, Microsoft/Google sign-up, and §B review fixes.


## Vacaturecategorieën (flexibel)

- Admin-beheerbare **VacancyCategory** (naam, kleur, tokenprijs, highlight/PushBom, extra velden)
- Standaardcategorieën: Uitzendbureau, Regulier, Highlight, Inclusief, Vrijwilligerswerk, Stageplekken, 65+
- Dynamische create-dropdown + extra velden; kaartfilter, legenda en pin-kleuren; tokenlogica per categorie
- **Geschikt voor 65+**: checkbox bij reguliere vacatures, label (donkerpaars), kaartfilter (65+-categorie + gevlagde regulier), popup/lijst/detail — géén legenda-item



Alle noemenswaardige wijzigingen aan dit project worden in dit bestand bijgehouden.

## [Unreleased]

### Added
- **Landing zonder werkgevers (07):** `-zw` variant on `/` (server-rendered via `IEmployersSwitch` / `ForceVariant`), passport hero + OFF “Wat je krijgt” / “Voor wie”, `/banenkaart` + `/banen` gated OFF → `/`, flag-aware sitemap/robots ETag + JSON-LD without SearchAction, GratisDna OFF copy (“Alleen jij”, unlock list, “Past dit beroep?”), mobile passport chips overflow fix. Dependencies A/B still absent → AlwaysOn + follow-up doc (D20 + Passport.Tab.Fit).
- **De ontdekkingsreis (08):** tests 7–10 with deeper question sets (5/10/25, Cultuur 18), “Weer een laag eraf” shed moment, end screen (`?stap=klaar`) with mini passport + `CompleteMyOnboardingAsync`, Discovery nav slot + short labels, “Verder ontdekken” overview, `OnboardingRoutes.StartPath` for flag-aware entry points. Old wizard kept for flag OFF.
- **Paspoort landing + nav order:** candidate default landing via `FeatureRoutes.HomeFor` when the paspoort flag is ON (not ready → `/candidate/ontdekkingsreis`, ready → `/candidate/paspoort`); nav order Discovery · Passport · Career · Banenkaart · Sollicitaties; `Nav.Banenkaart` label (legacy `Nav.Search` kept for flag OFF).
- **De ontdekkingsreis (07a):** `/candidate/ontdekkingsreis` behind CandidatePassport flag; journey shell (scene, lobster plates, progress rail, save status); wizard v3 step maps; Start + steps 1–2; extracted shared onboarding step components. Steps 3–10 deferred to 07b.
- **Mijn Paspoort · Mijn tests:** depth rows, quota line, locked-report preview, Groei verder course slots (curated `ShowInPassport`); TrainingOffer passport fields + admin; no demo course seeds in production.
- **Mijn Paspoort (flag ON):** `/candidate/paspoort` with overview (DNA ring + stats), tab shell, Mijn DNA tab, derived schalen; nav order Discovery · Passport · Career · Banenkaart · Sollicitaties; Bewaard as tab inside Sollicitaties; classic profile kept when flag OFF.
- **Kandidaat banen E2E + stack report (09):** Playwright S1–S13 (desktop 1440 + mobile 390) for banenkaart, lijst, vacature, sollicitaties, bewaard en Match; soft-skip zonder `JOBSY_E2E_BASE_URL`; docs + stack-eindrapport.
- **Match refresh (08):** calibrated fit pill, "Waarom jij past" per DNA dimension, Hierna column with fit + travel; "Laten schieten" defers to end of deck (D12, never hides); keyboard hints; mobile header with mascot + progress.
- **Sollicitaties + Bewaard (07):** statusgeschiedenis met datum, tijdlijn + "Wat nu?", "Niet gekozen" met vergelijkbare banen; Bewaard-kaarten met statuspillen, unsave+undo.
- **Lijst + vacaturedetail (06):** desktop Kaart/Lijst (`?weergave=lijst`), mobiele Kaart-FAB, fit-panel, reiskaart met vervoerswissel, sticky solliciteer-balk.
- **Uitzendbureau hidden mode (05):** bureau-pin/reistijd, "via uitzendbureau …", geen Route/Street View; kernwaarden/branche/engagement pas zichtbaar na werkgever-aanmelding 08/09.
- **Eerlijke fit % (04):** alleen na cultuur- of waardentest; weergave 55–90 (sterk ≥ 75); why-regel; DNA-balken; dislikes zetten lager ("Staat lager"), nooit verbergen. Werkgeverscores ongewijzigd.
- **Banenkaart start & filters (03):** start op thuisadres · 20 min fietsen; adresveld + PDOK; chips; echte ringen; docked popup.
- **Kandidaat banen fundament (02):** `Kb.*` strings (5 talen), labels, gedeelde kaartonderdelen, feature-gating-placeholders, UX-fixes.
- **Banenkaart hotfix (01):** echte isochronen (decimale contouren), desktop top-match crashfix, idempotente pagehide-shim.
- **Landing gratis test warm (06):** `/ontdek` restyle under `.pub-theme` (start/question/result), compact PublicLayout header, `GratisDnaSignupCard` + mobile sheet, sticky CTA vs cookie banner (`pub-fixed-bottom`), FeedbackWidget on page; behaviour/scoring/storage unchanged.
- **Landing page ON (05):** `/` is static SSR landing (`Landing.razor`, `[NoBlazorRuntime]`), warm `landing.css`, one real vacancy count (D7), FAQ + JSON-LD, signed-in + legacy map deep-link redirects, no MapLibre/blazor.web.js on `/`. Map stays at `/banenkaart` only.
- **Landing banenkaart move (04):** map served at `/banenkaart` (public, indexed; dual `@page "/"` until landing 05), `/banen` → 301 with query preserved, `AuthRedirects.BanenkaartPath` / RoleNav / map deep links / SEO+sitemap retargeted, `LegacyMapQuery` for 05.
- **Landing candidate account (03):** `/account-maken` + `/account-maken/code` (Google, Microsoft, passwordless e-mail code), `EmailSignInChallenge` migration, GratisDna CTAs → `/account-maken?van=ontdek`, `/register?van=ontdek` → 302, Login “Maak gratis account”, merge via existing `GratisDnaMerge`. TermsAcceptedAt also set on new external sign-ups (was missing).
- **Landing mascot (02):** `LobsyMascot` + `MascotAssets` manifest (all poses `Fallback` → today's mascot + CSS transforms), asset contract `docs/brand/mascot-assets.md`, guard/bUnit tests. No new art files.
- **Landing public theme shell (01):** `PublicLayout` + `.pub-theme` (`features/public-theme.css`), `UiStringsLanding` (nl/en/pl/ro/ar), `PublicNavCatalog` ON/OFF, SSR culture (`?lang=` + `/taal/{lang}`), `IEmployersSwitch` seam (`AlwaysOnEmployersSwitch` until feature flags land), static cookie-banner mode, TeaserLayout funnel links → `/` and `/account-maken`.
- **Admin redesign (07a · audit kern):** append-only `AdminAuditEvent` (7 jaar), writer + filter + reflection guard, Auditlog-pagina, admin-writes gelogd, globale zoek Correlatie. `auth.admin.login-failed` deferred (geen extra lookup bij login-fout).
- **Admin redesign (07b · tabs + slots):** 2FA & sessies, Privacy & AVG, Gegevensinzage + Systeemlogs gerestyled; dashboard “Recente beheeracties”, gebruiker-Activiteit, Functies “Opslaan en loggen” + Wijzigingen.
- **Admin redesign (02 · dashboard):** `/admin` met 5 KPI-kaarten, Te doen, systeemstatus en platform-modus; `/admin/te-doen`; moderatiefilter op vacatures (`/admin/vacatures/moderatie`); `GET api/admin/todo` + `GET api/admin/finance/summary`; sidebar count-pills.
- **Admin redesign (01 · shell):** eigen `AdminLayout` met gegroepeerde sidebar, top bar (globale zoek Ctrl/Cmd+K), environment badge, breadcrumbs; Nederlandse admin-URL’s met 301 vanaf oude paden; gedeelde admin UI-primitives; `GET api/admin/search`.
- **Admin redesign (04 · organisaties):** `/admin/organisaties` boom/plat + detailpanel, filters en tellingen op `GET api/admin/companies`; `/admin/organisaties/regios` tabs Domeinen · Regio's; `/admin/organisaties/aanvragen` KvK + overnames (aanvrager-e-mail gemaskeerd voor admin); KvK-retry endpoint.
- **Admin redesign (03 · gebruikers):** `/admin/gebruikers` met rol-tabs, filters (mfa/active), bulkacties, detail drawer (sessies, 2FA-reset via `MfaResetDialog`, support-toegang 15 min default); `/admin/gebruikers/rollen`, `/admin/kandidaten`; Sales-tab lead + uitbetalingen-link; sessie-/block-/bulk-API’s; aggregates zonder full-table load; `MfaResetByAdmin` mail.
- **Admin redesign (06 · financiën):** Omzet & transacties met KPI’s + Transacties/Tokenlog/KPI-tabs; goodwill grant+historie op `/admin/financien/goodwill`; uitbetalingen/btw zonder goodwill-tab (`?tab=goodwill` → goodwill); prijzen met “Wat bepaalt welke prijs?” + overlap-notes. Geen wijziging aan bedragen/VAT/Mollie.
- **Admin redesign (05 · platforminstellingen):** `/admin/instellingen` Functies uit `PlatformSettingsCatalog` (groepen, save-bar, 2FA als policy, activatielinks Acceptatie-only); Algemeen + Integraties & API; prijzen verplaatst naar `/admin/financien/prijzen` tabs; `/admin/sales` → `…/prijzen?tab=sales`.
- **Werkgever redesign 08:** terminologieguards (geen jargon in NL), statuslabels op één plek, dode employer-nav opgeruimd, rechtenmatrix compleet + docs, Playwright-rooktest per rol. Legacy `/employer`‑redirects blijven tot na de release.
- **Werkgever redesign 07:** Kandidaatinzichten als betaalde ontgrendeling met tokens, server-side free/locked split, admin settings.
- **Werkgever redesign 06:** Tokens & facturen overview (BM/VM/RM), verbruik per vestiging with allocate drawer, mutaties, facturen (moved from Bedrijfsprofiel), VM tokenaanvraag + Te doen `TokenRequests`, Partnerprogramma with referrals; `Regional/TokenControl` removed (legacy URL 301s).
- **Werkgever redesign 04:** Sollicitaties pipeline (Nieuw → Aangenomen) + list view, candidate drawer with privacy stages (`LobsyCvAccessRules`), mobile candidate page `/werkgever/sollicitaties/{id}`; API filters `status` / `overdueHours` / `branchIds`. Terminology: Uitgenodigd / Aangenomen. Interne notitie deferred (no employer note field).
- **Werkgever redesign 03:** Vacatures table with status tabs, filters, inline approval, bulk with token cost, plain-Dutch actions, VM/RM variants.
- **Werkgever redesign 02:** dashboard with KPI's, Te doen / Signalen, wervingstrechter, vestigingen table; `/werkgever/te-doen`; `api/werkgever/dashboard` + `api/werkgever/te-doen`; sidebar counts from te-doen. Vacancy `EndDate` present → VacanciesExpiring included.
- **Werkgever redesign 01:** shared `WerkgeverLayout` (sidebar, scope chip, mobile bottom nav), `WerkgeverNav` catalog, Dutch `/werkgever/…` URLs + 301 legacy redirects, Ent* UI primitives under `Components/Ui/Enterprise/` (admin stack should reuse these), rights matrix foundation, D5 BranchManager removed from token purchase.
- **Lobsy voor scholen (01 foundation):** rollen `SchoolAdmin` / `Teacher` (verplichte 2FA), datamodel (School, klassen, codes, progress/resultaten, aggregaten), codegenerator, rights-matrix scaffold, admin Scholen-pagina’s + 3 settings, `SchoolLayout` shell. Feature-switch `SchoolsEnabled` (default uit). Geen leerlingnamen.
- **Lobsy voor scholen (02 school portal):** dashboard + te doen, klassen & codes (CRUD, codelijst PDF/CSV met lege naamkolom), ouderbevestiging, testvenster (409 zonder overeenkomst/ouders), leraren uitnodigen (2FA verplicht), resultaten (per code server-gated via D4), schoolgegevens/privacy/materiaal.
- **Lobsy voor scholen (03 leraar portal):** klasoverzicht met KPIs, leerlingcodes, groepsresultaten (k≥5), droombanen, codedetail (verhaal/PDF stubs), class switcher, testvenster en codelijst voor eigen klassen. Onbekende klas → 404.
- **Lobsy voor scholen (04 leerling login + wizard shell):** aparte `Pupil`-cookie (`Lobsy.Leerling`, niet-persistent, 20/90 min), login school/klas/code met rate limits/lockout, `LeerlingLayout` + Scene/lobster (10 plates), wizard shell met placeholder-vragen en voortgang na elk antwoord. Geen namen/AI/partners.
- **Lobsy voor scholen (05 vragenbank):** 60 B1/A2-items op de 4 bestaande scoringsmodellen (`LikertCategoryScorer`), pauze-eiland hobby/niet-leuk chips, `PupilResultBuilder`, gegenereerde review-doc.
- **Lobsy voor scholen (06 Dit ben jij + droombaan + PDF):** vaste verhaalsjablonen (geen AI), droombaan-checker zonder links/vacatures, QuestPDF on-demand (nooit opgeslagen), leerling- en leraar-PDF.
- **Lobsy voor scholen (07 bewaartermijn + aggregaten):** `SchoolAggregateSnapshotter` (k≥5, droombanen &lt;2 → Overig), `SchoolRetentionHostedService` (altijd aan, ook als SchoolsEnabled=false), early delete (admin school / schooljaar), admin Scholen-rapportage + CSV + dry-run.
- **Sales aanbevelen + AVG + cleanup (09):** redesigned `/sales/aanbevelen` with third-party notice mail + objection link, retention 60/30/30 for applications + documented 7y fiscal / 25m clicks, privacy §6a “Aanmelden via een salesmanager”, ADR 0006, removed old SalesManager page stubs and unused `api/sales-managers/me/*` self-service endpoints (parked ambassadeur code kept), empty inline-style allow-list.

### Added
- **Sales admin uitbetaalrondes (08):** maandelijkse payout run (job 1e werkdag 06:00 Europe/Amsterdam), admin approve/reject per regel, self-billing bij goedkeuring, SEPA pain.001 + CSV, mark paid (sluit payout request), IBAN-hold/consent flags, correctie + toewijzing UI, bank-transfer provider-seam, geparkeerde ambassadeurs-saldo panel. Fallback-tab Uitbetalingen op `/admin/sales-managers` tot admin redesign 06.4.

### Added
- **Sales wallet & uitbetalingen (07):** balances by state, Mutaties/Uitbetalingen/Facturen, payout request (≥ € 50, full available amount), invoice preview, cancel while Requested, self-billing PDF legal text + KOR, jaaroverzicht PDF; old checkout/complete endpoints return 410.

### Added
- **Lobsy Partner (salesmanager) foundation:** mandatory 2FA for SalesManager (+ Ambassadeur when re-enabled), partner data model + migration, `SalesLayout` / `/sales/*` URLs (legacy 301s), labels, and Ambassadeur parked behind `AmbassadorsEnabled` (default off). Existing salesmanagers are forced through 2FA at next sign-in.
- **Sales attribution (03):** 30-day first-click cookie `lobsy_sales_ref`, `/p/{code}` short link, typed code wins over cookie, self-referral guard, admin reassign API + history, daily click counters (no IP/UA), funnel read service.
- **Sales dashboard + Mijn werkgevers (04):** privacy-safe employer list/detail DTOs, dashboard KPIs / monthly bars / funnel / todos / top employers, top-bar search (`Ctrl K`), `/sales/werkgevers` with detail drawer.
- **Sales Mijn link & materiaal (05):** `SalesPriceQuote` from active `TokenPricing` packs, redesigned `/sales/link` (QR, share, materials grid, pitch, commission tiles), personal materials PDFs (`api/sales/me/materials/{kind}.pdf`), presentation PDF, safer public flyer endpoint, `SalesQr` helper.

### Fixed
- **Render `jobsy-api` deploys:** `WebPushSubscriptions` ontbrak in `JobsyDbContextModelSnapshot`. EF Core 9 behandelt dat als `PendingModelChangesWarning` → harde fout in `MigrateAsync` → API-host stopt (Auto-Deploy Failed sinds PWA-commit). Snapshot + regressietest herstellen de deploy.

### Added
- **Kandidaat uitzendbureau hidden mode (05):** bureau pin/travel, "via uitzendbureau …", no Route/Street View; `KbHiddenIntermediaryMask` (KB-FALLBACK(A)). Employer kernwaarden/branche/engagement blocks deferred until werkgever-aanmelding 08/09 (Dep B ABSENT).
- **Kandidaat fit % (honest):** only when culture or values test is done; calibrated display 55–90 (strong ≥ 75); why line; DNA bars; dislike down-rank via `IKbDislikeSource` (KB-FALLBACK(D) returns none). Employer scores unchanged.
- **PWA (native-like):** `manifest.webmanifest` (standalone), iconen 192/512 (+ maskable), `service-worker.js` / `service-worker.published.js` voor shell/asset-caching + Web Push handlers.
- **Web Push:** VAPID + `WebPushSubscriptions`, `api/push/*`, systeemmeldingen via `WebPushNotificationService`; vriendelijke toestemmingsbanner en Profiel-toggle.
- **Calm tech motion:** hardware-accelerated tab-/page-transities, touch active-states, scroll-containment / minder rubber-banding.
- **Kandidaat-profielhub (`/profiel`):** rustige, responsive hub met persoonlijke baseline, DNA/testscores en account/privacy (mock via `CandidateProfileService`). Desktop twee kolommen; mobiel inklapbare kaarten met bottom-nav clearance.
- **Bottom-nav kandidaat (5):** Zoeken · Bewaard · Sollicitaties · Carrière · Profiel.
- **Carrière-dashboard (`/carriere`):** rustige Blazor-pagina met header (huidige rol → stip op de horizon), match-% voortgangsbalk en uitklapbaar stappenplan (skills gap, competenties, actie). Mock-data via `CareerPathService` voor directe UI-tests.

### Changed
- **Kompas-navigatie en Top 10:** de tabbladen (Wie ben ik?, Mijn profiel, …) staan op volle breedte en wrappen met het volledige label. In elke Top 10-tegel blijft het matchpercentage in de kop staan; de toelichting loopt eronder door in plaats van naast het percentage af te knippen. De Top 10 blijft naast de kompas-inhoud zichtbaar.
- **Vakgebied-matching:** vergelijkbare functies en directe vacatures blijven in dezelfde sector (een piloot krijgt geen lab of café). Vacatures tonen harde eisen (keuring, ogentest, fitheid, certificaten, rijbewijs) en een ontbrekende eis is een dealbreaker. Het loopbaanadvies noemt hoe lang het opleidingspad duurt, in stappen.
- **Diepte-analyse UX (150 vragen):** sticky voortgangsbalk met live %-indicatie, onderwerpen-tracker (afgerond / nu / komt nog), info-knop met praktijkvoorbeeld per vraag, en motiverende boosters elke 25 vragen.

### Changed
- **Multidimensionale vacature-matching:** ranking weegt opleiding (niveau/richting), competenties & drijfveren (Wie ben ik? / DISC/OCEAN) en overdraagbare werkervaring — niet alleen een exacte functietitel. Bij bredere matches toont banenkaart/Top 10 een korte AI-onderbouwing.
- **Opleidingen subtiel & deeplinks:** in-context tekstlinks i.p.v. schreeuwende CTA’s; outbound-URL’s moeten altijd op een specifieke cursuspagina landen (geen opleider-homepage).

### Added
- **Wie ben ik?** in Mijn Lobsy Kompas: checklist (profiel, competentie, beroepen, gedragsanalyse); daarna AI-persoonsverhaal, radar, DISC-kwadranten en optionele PDF-bijlage bij het Lobsy-CV.
- **DISC-Analyse** in Mijn Lobsy Kompas: gratis Quick-Scan (25) + optionele diepte-analyse (150, € 2,99); radar + accordeon per gedragsstijl met ontwikkelpunten en regionale workshops; scores wegen mee in Functie-Fit en cultuurfit (gewone taal, geen vaktermen in stap 2/3).

### Changed
- **Mijn competenties:** radar bovenaan; per vaardigheid een accordeon met korte uitleg (naar score) en workshops/cursussen van regionale opleiders (campagne `competence`); geen contactverzoeken of profielformulieren op dit tabblad.
- **Kompas UI-opschoning:** tab **Mijn beste match** (was Mijn beroepen) en **Functiefit checker**; profielsecties (Persoonlijk / Voorkeuren & reistijd / Beschikbaarheid / CV & ervaring) bovenaan; contactverzoeken alleen op tab Mijn profiel; OCEAN-grafiek altijd zichtbaar; beroepen als accordeon met opleidingen per functie; rustigere directe vacatures; vergelijkbare functies herberekent live.

### Added
- **Functie-Fit conversietrechter:** 4 vaste stappen (overall match-% uit reistijd/uren, cultuurfit en formele eisen; waar je matcht; wat je mist; actie/upskilling). Plus AI/lokale **vergelijkbare functies** (opstaprollen) en een live scan van **direct startbare vacatures** in Den Haag/Westland.

### Added
- **Dynamische Functie-Fit op vacature:** optionele `BarrierRequirementsJson` (lage drempel vs zware eisen: diploma’s, VCA/BIG/vliegbrevet, ervaringsjaren/uren). Functie-Fit Checker toetst cultuurfit (OCEAN-pijlers) en een formele checklist; bij cultuur+beschikbaarheid OK maar een papieren gat volgt de opleidingen-CTA. Werkgever stelt de drempel in bij vacature-aanmaken.

### Added
- **Opleidings- & upskill-vliegwiel:** bij een gat in de Functie-Fit Checker (en het Beroepen-kompas) volgt het advies “Volg een korte cursus of omscholing om dit gat te dichten” plus CTA *Bekijk erkende opleidingen voor dit vakgebied*. Landelijke affiliates (LOI/Daisycon, NTI/Awin) met UTM/`ref=lobsy`/`candidate_id` (HMAC, geen e-mail/GUID). Regionale praktijkpartners Den Haag/Westland (zorg, techniek, logistiek) met intake-/start-fee. Admin `/admin/training`: catalogus, conversiematch (click-id / hash / e-mailhash) en maand-CSV. RTBF wist `UserId` op kliks, hashes blijven voor facturatie.

### Added
- **Cultuur & teamfit:** werkgevers kiezen 3–5 cultuurpijlers bij vacaturecreatie/bewerken (`CulturePillarsJson`). Na harde criteria berekent de backend een Cultuur Fit-% uit competentiescores (OpenAI + lokale fallback, geen NAW). Banenkaart, popup en vacaturedetail tonen *Cultuur Fit: Hoog/Midden/Laag* plus Jip-en-Janneke-onderbouwing.

### Added
- **Functie-Fit Checker** (“Past dit bij mij?”) in Mijn Lobsy Kompas: pas te gebruiken na beide gratis 25-vragen quick-scans; OpenAI-toets van een vrije functietitel tegen het kandidaatprofiel (zonder NAW); lokale fallback; upsell naar de 150-vragen diepte-analyse (€ 2,99); knop naar vergelijkbare vacatures op de banenkaart (Den Haag / Westland). Resultaat in privacy-export en RTBF.
- Kandidaatprofiel en `/home`-kompas in **vier tabbladen**: Mijn profiel, Mijn competenties, Mijn beroepen, Past dit bij mij? (Functie-Fit Checker). Inactieve tab-panels blijven in de DOM (`hidden`) zodat flex-layout ze niet stapelt.

### Added
- Loopbaan-PDF na de uitgebreide beroepentest (150): OpenAI-prompt dwingt een hiërarchie af (Super-match 95–100 als kernfit, nooit te laag), Jip-en-Janneke zonder extraversie/neuroticisme, gekleurd Lobsy-logo, *Wat betekent dit voor jou?* (werkplek, taken, banenkaart). Resultaat vult PDF én **Mijn Beroepen-kompas**.
- **Mijn Beroepen-kompas** slaat die algemene beroepen (inclusief zoeksleutels) op in het kandidaatprofiel; de banenkaart vertaalt ze naar actuele advertenties (bijv. verpleegkundige → vacatures in Den Haag/Westland).
- Privacyverklaring en consentversie **2026-09-21**: OpenAI-doorgifte van anonieme beroepentest-antwoorden voor het kompas; export bevat `CompassJson`.
- **Mijn Lobsy Kompas** op `/home` en `/candidate/profile` (kandidaat): drie tabbladen **Mijn profiel** (beschikbaarheid, reistijd, vervoer, rijbewijs), **Mijn competenties** (Quick-Scan 25 vs diepte-analyse 150 + werkstijl) en **Mijn beroepen** (Beroepen-kompas, *Wat betekent dit voor jou?*, loopbaan-PDF).
- Diepte-analyses: **150 unieke, niet-herhalende** Likert-items (competentie: 30 per OCEAN-trek; beroep: 25 per RIASEC-type) met reverse-items; PDF toont domainscores + carrière-advies; matchingtags na afronden.
- Banenkaart (ingelogde kandidaat): live match-% op pin, popup en lijst; filter “alleen >80%”; sorteren op beste match; uitleg waarom. Discover-API: `minMatchPercent` + matchvelden alleen voor kandidaat (private cache).
- **Gescheiden test-architectuur:** competentietest (25 Big Five) en beroepentest (25 RIASEC) elk met eigen 150-vragen diepte-analyse (€ 2,99) en PDF; routes `/candidate/career` en `/candidate/deep-analysis/{kind}`.
- API: `api/employer/talent/*`, `api/me/talent-contacts`, `api/me/deep-analysis`; UI: `/employer/talent`, `/employer/talent-contacts`, `/candidate/talent-contacts`.

### Changed
- Alle Lobsy-platformbedragen (diepte-analyse, flex-marge, uitzend-jaarabonnement, ContactUnlock) zijn admin-configureerbaar via **Settings → Lobsy Flex & talent**.
- Kandidaat **competentietest** uitgebreid van 20 → 25 (RIASEC-tags + match-tags op `CandidateCompetencies`).
- Live **KVK Handelsregister**-koppeling: bij API-key (Admin → Integraties of `Kvk__ApiKey` / `KVK_API_KEY`) zoekt Lobsy echte vestigingen; zonder key blijft de demo-stub. Base URL leeg = `https://api.kvk.nl/api/` (test: `https://api.kvk.nl/test/api/`).
- Register-wizard: kruimelpad, KVK-adres met i-toelichting (vestigingsnummer + SBI achter het i-tje), stil logo, knoptekst ‘Bevestigen’ niet meer afgeknipt, geen ‘Open verificatielink’.
- `/health` en HTML-meta `lobsy-commit` tonen de Render git-SHA zodat productie verifieerbaar is.

### Changed
- Privacyverklaring en consentversie **2026-09-20**: anonieme talentpool-transparantie (tags/scores zonder NAW); ruwe antwoorden blijven kandidaat-only. Diepte-analyse-checkout stub-gated + user-bound; lege deep-save overschrijft niet; privacy-export inclusief deep analysis/checkouts/talent-contacts; Quick-Scan tag-backfill bij migrate.
- Privacyverklaring en consentversie **2026-09-18**: optionele competentietest (antwoorden/scores, alleen kandidaat, export/RTBF, geen werkgever-inzage). Lege `PUT api/me/competencies` overschrijft een bestaande test niet.
- CSS-cachebust `app.min.css?v=20260918-comp` zodat competentie-profiel en Top 10-matches in bestaande browsers aankomen.
- Register: KVK-nummer is leeg buiten Development (geen vooringevulde stub `12345678` op lobsy.nl).
- Render: Production (`jobsy-api` / `lobsy.nl`) wist **alle** bedrijven, vacatures en niet-admin gebruikers bij API-start, ook als `Seed:Enabled` nog aanstaat; houdt `admin@jobsy.local`. Acceptatie (`lobsy-acc-api`) blijft seeden.
- ZAP (Checkmarx): geen exception-/status-tekst meer in publieke HTML; ontbrekende vestiging `/12345678/0001` geeft 404 i.p.v. 500; foutpagina toont alleen een request-referentie. CSP `img-src`/`connect-src` zonder scheme-wildcards (picsum + OpenFreeMap). `X-Content-Type-Options: nosniff` ook op statische files (favicon). Publieke bedrijfs-API op `public-read`. `'unsafe-eval'` blijft nodig voor Blazor Server; OIDC-nonce blijft `SameSite=None` voor Entra.
- Production audit 111: dode CSS/modellen opgeruimd; intermediair kan geen werkgevers overnemen of client-bedrijfsinstellingen/facturen wijzigen of lezen; demo-login alleen bij `AllowDevelopmentAuth`; rate limits op publieke vacature-GETs en `/travel`; analytics-POSTs vereisen cookietoestemming (HMAC in Production); RTBF/intrekken wissen leeftijd/werkvergunning/match; platform BTW-IBAN alleen gemaskeerd in de API.
- CSP: per-request nonce op scripts en het critical-`<style>`-blok; `script-src` zonder `'unsafe-inline'` (inline `onerror`/`onload` weg; logo-fallback via capturing listener). Style-attributen blijven `'unsafe-inline'` voor Razor/MapLibre CSS-variabelen.
- Mozilla Observatory: HSTS `max-age` 2 jaar + `includeSubDomains`; cookies altijd `Secure` op de publieke host (consent/antiforgery/OAuth/sessie); geen tweede Blazor-CSP-header.
- Toegankelijkheid: Filters-knop gebruikt geldige ARIA (`true`/`false`) en houdt `#discovery-filters-desktop` in de DOM; cookiebanner “Accepteer analytics” haalt WCAG AA-contrast (wit op `--coral-deep`).
- Banenkaart: eerste paint gebruikt de precomputed centroid/zoom van actieve pins (geen NL-overzicht, geen herzoom als markers later komen).
- Banenkaart: prerender zet echte pin-coördinaten in `#jobsy-map-boot`; MapLibre boot meteen met markers (geen wachten op Blazor). Homepage toont geen watermark-logo’s meer.
- Homepage toont meteen de kaart: watermarks/chrome blijven weg tot de layout-CSS er is, MapLibre boot vóór de vacaturecatalogus.
- Banenkaart cluster-popup op desktop blijft op zijn plaats bij pagineren (vaste kaart-hoogte, geen her-centreren).
- Banenkaart-popup: vacaturetype-label staat in de chrome-rij links van € (of links van het kruisje). Uitgelicht blijft op de foto.
- Banenkaart laadt MapLibre (CSS, JS, helper-chunks) via `preload`/`fetchpriority=high` zodat pinnen eerder zichtbaar zijn. Worker-preload gebruikt `as="fetch"` (geen extra main-thread script). Leaflet is verwijderd.

### Added
- Render Blueprint: project **Lobsy** met omgevingen **Production** (`jobsy-api` / `jobsy-web` / `jobsy-db`) en **Acceptatie** (`lobsy-acc-*`). Zie `docs/deploy-render.md`.
- PageSpeed/Lighthouse: auditors en crawlers krijgen dezelfde prerender-HTML zonder Blazor-circuit (`blazor.web.js`); echte browsers mappen `unload` naar `pagehide`; `UseWebSockets` + source map voor MapLibre CSP.
- Kandidaat kan een eigen CV (PDF/DOCX) uploaden; OpenAI vult alleen lege profielvelden als ze duidelijk in het CV staan. Lobsy-CV toont bovenaan dat er een eigen CV is. Recensies (werkgever, contactpersoon, e-mail, telefoon) in het profiel; vacature kan een hard minimum aantal recensies eisen. Na Accept ziet de werkgever Lobsy-CV én het geüploade CV.
- Quality gate 456: geüploade CV-bytes wissen bij intrekken; OpenAI-CV-extractie in privacyverklaring/consent; geen OpenAI-response bodies in logs; werkgeverslijst toont recensietelling pas na Accept.
- PageSpeed *niet-gebruikt JavaScript*: worker niet als `as=script` preloaden.
- PageSpeed *kleinere JS-payloads*: MapLibre CSP-build (worker off-thread), minified kaart-JS, `app-core` zonder session/download/richtext, feedback-script pas bij openen van de widget.
- PageSpeed *Efficiënte levensduur voor het cachegeheugen*: statische assets met `?v=` cachen 1 jaar (`immutable`); overige JS/CSS/images/fonts minstens 30 dagen. MapLibre en `blazor.web.js` hebben nu een versie-query.
- Homepage-kaart: PageSpeed-PRs #159–#165 teruggedraaid. MapLibre laadt weer meteen na paint (geen click-only / window.load-gate), desktopkaart vult de kolom (`55dvh` / `70vh`) in plaats van een lege 300px-box.
- Homepage PageSpeed: `app.css` laadt non-blocking (`media=print` + onload), `app-core`/`blazor.web.js` met `defer`, MapLibre pas na idle/IntersectionObserver. `#job-map` reserveert 55dvh om CLS te voorkomen; logo-`<img>` valt terug op Lobsy-mark.
- Banenkaart: Leaflet/Carto vervangen door **MapLibre GL JS + OpenFreeMap** (Liberty standaard, 3D/Bright-switch, groene pins, geen attribution-chrome). Zie `docs/performance.md`.
- Role dashboards (`/home`): geen foutflits meer na login (admin en andere rollen). GET-calls retrien kort terwijl auth settelt; 401 alleen zonder credentials; fouttekst alleen als er echt geen data is.
- Homepage-kaart: first paint toont al pins op de NL-preview; Leaflet warmt meteen na paint (geen lege kaart meer tot de circuit klaar is).
- Transactionele mails: Lobsy-logo als klein PNG + inline CID (niet meer de 568&nbsp;KB remote `lobsy.png` die in clients als gebroken plaatje verscheen).
- Homepage weer snel: Leaflet pas na first paint, picsum-foto’s pas ná de live kaart (lazy, 400×267 op kaarten). Originele unieke foto’s en markers blijven. Zie `docs/performance.md`.
- Vacaturefoto’s: picsum-seeds terug (unieke foto per vacature); SVG-stand-ins worden teruggezet. Kaart-init weer in de werkende volgorde (tegels → clusters). Zie `docs/performance.md`.
- Homepage-kaart: markers weer zichtbaar (cluster niet droppen bij 0×0 bounds; preview-overlay weg na init). Zie `docs/performance.md`.
- Homepage-kaart: prerender van echte Carto-NL-tegels (lokaal webp); Leaflet start op heel NL, niet eerst Den Haag/Null Island. Zie `docs/performance.md`.
- Banenkaart-performance: lokale SVG-placeholders i.p.v. picsum, lazy `<img>` op job cards, WebP-logo, Leaflet pas laden op kaartpagina’s, gebundelde `app-core.js`, Brotli + cache-headers. Zie `docs/performance.md`.
- PageSpeed-vervolg: geen 278 job cards meer in de eerste HTML/mobile-kaart (venster van 12), compacte cookiebanner in first paint, map-loader split discovery/detail.
- Quality gate 456: www-canonical alleen voor bekende hosts (geen Host-header open redirect); Cloudflare image-resize alleen same-origin; `app-core.js` synchroon vóór Blazor; map-loader herstelt na een mislukte load.
- Banenkaart opent uit een warme in-memory vacature-index (refresh elke 15s + direct na publiceren/wijzigen) in plaats van een zware DB-query + OpenAI-vertaling per page-open. Kaart en lijst tonen meteen; locatiefilter volgt daarna.
- Quality gate 456 (feedback-pipeline): pagina-URL zonder query/fragment; RTBF wist ook beschrijving/prompt/rol; screenshots max. 90 dagen ongeacht status; geen dubbele Cursor-launch; opgeslagen prompt blijft behouden bij heropenen.

### Added
- End-to-end feedback-pipeline: globale Feedback-knop (screenshot + metadata), `POST /api/feedback`, admin-datagrid `/admin/feedback`, functionele prompt en Cursor Cloud Agent-koppeling die een PR opent; PR-URL via webhook/poll terug in het grid.
- Prepaid token checkout (“no tokens, no action”): bij onvoldoende saldo blokkeert publish/highlight/PushBom/extend met in-context Mollie-top-up (exact match + bulkapakketten); na webhook/return worden tokens bijgeschreven en de pending actie automatisch uitgevoerd (`PendingTokenAction`).
- Salesmanager multi-level referral (één laag): Admin maakt tier-0 aan; tier-0 dient aanbevelingen in; Admin keurt goed vóór provisioning; tier-1 kan niet verder werven.
- Configureerbare commissies (defaults 15% direct / 3% indirect, max. 1 jaar per ondernemer) op `/admin/sales`; ledger + `RevenueShareLogs` voor indirecte bonus.
- Bedrijfsregistratie: gekozen wachtwoord bij submit, e-mailverificatie, daarna dual login (e-mail/wachtwoord of Microsoft Entra met hetzelfde adres).
- Automatische KVK/SBI-roltoekenning: SBI `78*` → Intermediair; overig Organization-scope → Bedrijfsmanager (EnterpriseManager).
- Quality gate 456: takeover e-mailverificatie vóór inbox/approve; pending `PasswordHash` gewist bij cancel/reject/expiry/anonymize; intermediair-takeover detacht van employer-org.
- Functionele specificatie matching / dagdelen / uren / Arbeidstijdenwet voor kandidaat- en werkgeverskant (`docs/FUNCTIONELE_SPECIFICATIES_MATCHING.md`), inclusief verwijzing vanuit `REQUIREMENTS.md`.
- Admin Sales beheer (`/admin/sales`): basis tokenwaarde (€25), tokenkosten per vacaturetype (Regulier / Stageplek / Vrijwilligerswerk), standaard + First Year / Enterprise pakketten (Silver/Gold/Platinum), highlight-toeslagen.
- Vacaturetype `VacancyKind` op create/publish; publiceerkosten volgen type (incl. nul-tarief voor vrijwilligerswerk).
- Publieke Partner Sales-pagina (`/partner`, `/partner/{code}`) met live tarieven/pakketten, PDF-flyer (QuestPDF) en WhatsApp/mail-deelknoppen.
- Salesmanager toolkit (`/salesmanager/toolkit`) met persoonlijke trackinglink/flyer; registratie via `?ref=` activeert gratis start-highlight op eerste vacature.
- Cursor shortcut `456` (quality gate): full regression + AVG audit + code review.
- Quality-gate fixes: start-highlight only on vestiging + atomic consume (incl. approve path); anonymous catalog read-only; flyer rate-limit + `SM-XXXXXX` validation; type pricing ignores catalog `IsActive`; i18n key parity (nl/en/pl/ro/ar).

## [0.8.0] - 2026-07-25 (Sprint 8: Polish, seed, docs, cleanup)
### Toegevoegd
- Rijke idempotente `Sprint8MetricsSeeder`: spends/pushboms/extensions, time-spread engagement, statusmix-vacatures (Draft/Pending/Archived + intermediair), platform logs, allocations.
- Gedeelde UI: `MetricTile`, `DrilldownGrid`; `ShareVacancyModal` → `ShareModal`.
- Candidate BottomNav: Home → `/home`; Admin-nav gestript (modules blijven op `/home`); Intermediary `/home` gebruikt EmployerHomePanel.

### Opgeruimd
- Dode componenten: `AdminHub`, `ComingSoonPage`.
- Legacy dashboards `/branch` en `/regional` redirecten naar `/home` (aliases `/banen`, `/admin`, `/admin/cockpit` blijven).

### Docs
- `ROLES_AND_VIEWS.md` en `REQUIREMENTS.md` bijgewerkt naar Sprints 4–7 + entry `/` ↔ `/home`.
- `MOCKDATA.md` beschrijft Sprint 8 metrics/logs seed.

### Review-fixes
- Metric drilldowns voor stock-KPI’s (active vacancies / users / companies); applications redacteren PII tot Accept (employers).
- `MetricsKeys.PlatformOnly` naar Core (Api niet meer afhankelijk van Infrastructure type).
- Seeder: geen early-exit op bestaande Spend/Allocation; intermediair-spend gebruikt bestaande vacancy-id; Café-guard voor archived vacancy.
- EmployerHomePanel toont alle metrics (geen `Take(8)`); Intermediary applications-link + `/branch/applicants` authorize.

## [0.7.0] - 2026-07-24 (Sprint 7: Registratie KVK + org-merge)
### Toegevoegd
- Publieke registratieflow `/register`: KVK → vestiging → scope (alleen-vestiging / hele organisatie) → contact → activatie-mail (stub link).
- Unique `KvkEstablishmentId` blijft de vestigingssleutel; activatie maakt `User` + `LocalAuthCredential` (uniek stub-wachtwoord).
- Conflictflow: vestiging al in gebruik → `EstablishmentTakeoverRequest`; inbox `/employer/takeovers`; na goedkeuring org-merge (parent-koppeling + token-allocatie naar organisatie).
- Login valt terug op `POST api/auth/local-login` voor geregistreerde accounts (naast DemoUsers).

### Beveiliging / review-fixes
- Activatietoken is one-time (cleared na gebruik); replay lekt geen wachtwoord; TTL 48u.
- `ActivationUrl` alleen in API-response als `JobsyFeatures:ExposeRegistrationActivationLinks` (Development aan).
- `stub-activation` is Admin-only + Development/feature-flag (niet anoniem).
- Organisatie-overname alleen door EnterpriseManager/Admin; prior owners verliezen memberships.
- Geen nieuwe parent bij bestaande `ParentCompanyId`; siblings met openstaande registratie worden niet geclaimd.
- Dubbele pending-registratie op dezelfde vestiging → conflict; EM claims∩DB her-expand children.

### API
- `api/registration` (submit / activate / kvk establishments / takeovers approve|reject)
- `api/auth/local-login`

## [0.6.0] - 2026-07-24 (Sprint 6: Admin suite)
### Toegevoegd
- Admin platformdashboard (`/home`) met doorklikbare KPI’s; platform-only metrics (`errors`, `companies_employers` / `companies_intermediaries`, users) alleen voor Admin.
- Admin modules: bedrijven/intermediairs, gebruikers, vacatures, financieel (KPI + tokenlog), platform logging, token-grant, WML/salaris, systeeminstellingen, integratie-pings.
- Intermediair-seed/backfill (`CompanyType.Intermediary`) voor admin KPI’s.
- Halfjaarlijkse WML-update stub (`POST api/wages/semi-annual-update`) + `MinimumWageUpdateHostedService` reminder-job (Europe/Amsterdam).
- Settings: token-pack pricing, spend-costs en early-adapter rules upsert via `api/settings`.
- Admin UI grids (`.table-scroll` / `.data-table`) en filters; placeholders (moderatie, masterdata, notificaties) als muted “later” onder `AdminPageShell`.

### API
- Uitbreiding admin-metrics drilldowns; `api/wages` upsert + semi-annual; `api/settings/token-pricing` (+ packs/costs/early-adapter); admin company/user/vacancy/log endpoints.

### Review-fixes
- `SalaryService` leest actuele `MinimumWageRates` uit de DB (hardcoded alleen als fallback) — admin WML-edits gelden voor `/check` en vacaturevalidatie.
- Semi-annual `EffectiveFrom` valt op de due-date (1 jan/1 jul) i.p.v. altijd de volgende periode; reminder-job gebruikt NL-kalender.
- Admin `UserCount` + users-filter tellen ook `UserCompanies`-memberships; `tokens_purchased` telt geen grants meer.
- `GET api/wages` toont alleen huidige effectieve tarieven per leeftijd; intermediair krijgt geen Westland-logo-fallback.

## [0.5.0] - 2026-07-24 (Sprint 5: Bedrijf / Regio / Vestiging UI)
### Toegevoegd
- Employer dashboards met doorklikbare metrics (periode-tabs + drilldown via `api/metrics`, scoped door `CompanyAuthorizationService`).
- Vacaturebeheer: rich-text toolbar, image/video-URL, salaristabel-dropdown, live preview-tegel, Publiceren-popup met token-opties (highlight / pushbom / verlengen).
- Tokens: Mollie-stub checkout (`/tokens/checkout-stub`), vestiging-allocatie (`Allocation` ledger), logs met bedrijfsnaam-filter.
- Vestigingen toevoegen via KVK-stub; Regio’s CRUD; Gebruikers invite-by-email + rollen (e-mail stub).
- Salaristabellen CRUD voor employers; beschikbaar bij vacatureplaatsing.
- Sollicitanten: progressive disclosure — eerst woonplaats/afstand/voorkeuren; na Accept volledige PII.

### API
- `POST api/tokens/checkout`, `POST api/tokens/checkout/complete`, `POST api/tokens/allocate`, `GET api/tokens/packs|costs`
- `api/regions` CRUD, `POST api/companies/from-kvk`, `api/salary-tables`, `api/company-users` (+ invite)
- Employer applications DTO zonder PII tot status Accepted

### Beveiliging / review-fixes
- Checkout-sessies persisted (`TokenPurchaseCheckout`); complete crediteert alleen server-side PackSize; onbekende paymentIds zijn niet betaald.
- Invite blokkeert cross-tenant overname en peer/hogere rollen.
- Salaristabel-upsert controleert ownership (geen IDOR via body CompanyId).
- KVK-vestiging moet matchen op parent-KVK; prefs geredacteerd tot Accept; preview zonder MarkupString XSS.
- `RequireCompanyAccess` faalt closed zonder CompanyId; application react via atomische Pending-update.

## [0.4.0] - 2026-07-24 (Sprint 4: Engagement ledger + token products)
### Toegevoegd
- Publish-opties: base / highlight / pushbom / extend met typed token-debit (`TrySpendMany`) en saldo-check.
- PushBom stub: selecteert `OpenForWork`-kandidaten binnen 10 km (`User.HomeLocation` + PostGIS / Haversine-fallback), schrijft push-logs.
- Verlengen (+14 dagen, `ExtensionCount`) en inactive (`Archived`).
- Onvoldoende tokens bij publish → `PendingApproval` + notificatie naar EnterpriseManager; `POST .../approve-publish` om goed te keuren.
- Employer vacaturebeheer-acties voor productflows; profiel ondersteunt thuislocatie.

### Opgelost (code review)
- PendingApproval alleen via `ApprovePublish` (geen bypass door branch managers).
- Aangevraagde publish-opties blijven bewaard (`RequestedHighlight/PushBom/Extend`) en worden bij goedkeuring afgeschreven.
- Status/flag opnieuw gevalideerd binnen spend-transactie (minder double-spend races).
- Push/e-mail pas ná commit; lege PushBom schrijft geen tokens af.
- UI: Goedkeuren alleen voor EnterpriseManager/Admin.

### Bestond al (bevestigd)
- Like / unlike / share / click engagement-API’s en typed `TokenTransaction` ledger.

## [0.3.0] - 2026-07-24 (Sprint 3: Kandidaat dashboard)
### Toegevoegd
- Kandidaat `/home` met metrics-tegels (sollicitaties / shares / likes / reacties) × dag/week/maand en drilldown-grids.
- Mijn sollicitaties, geliked en gedeeld-pagina's met echte data.
- Profiel: Open for work, voorkeuren (domeinen / reistijd / vervoer) en geboortedatum.
- Sollicitatieflow koppelt `CandidateUserId`, stuurt IEmailService-bevestiging (stub) en optionele Authenticator stub-flag (`JobsyFeatures:AuthenticatorEnabled`).
- Werkgever-reactie (`POST api/applications/{id}/react`) met stub e-mail + push + deeplink.
- `IPushNotificationService` stub en candidate-scoped metrics API (`api/me/metrics`).

### Opgelost
- Duplicate apply checkt nu ook op e-mail; unique indexes op `(VacancyId, CandidateUserId)` en `(VacancyId, CandidateEmail)`.
- Werkgever-react alleen toegestaan bij status Pending (geen spam-notificaties).
- Vacaturedetail toont bestaande sollicitatie na refresh.
- `PublicWebBaseUrl` wijst naar poort 5201; e-mail HTML wordt ge-escaped.

## [0.1.0] - 2026-07-23 (Week 1: Fundament & Demo MVP)
### Toegevoegd
- Initiële mappenstructuur volgens Clean Architecture (.NET 9).
- Database-entiteiten (`User`, `Company`, `Vacancy`, `TokenTransaction`) met Entity Framework Core en PostGIS ondersteuning.
- Automatische Database Seeder met realistische mockdata voor de regio's Westland en Den Haag.
- Basis Web API endpoints voor het opvragen van actieve vacatures.
- Eerste Blazor frontend component met een Funda-achtige split-screen opzet.
- Documentatie bestanden (`REQUIREMENTS.md`, `CONTEXT.md`, `SECURITY.md`, `TESTING.md`, `ARCHITECTURE.md`).

## Auth 06
- Removed `/register/activate` page and activation-link builder; permanent 301 to `/register`. Wizard code step remains.

## Auth 08
- Playwright E2E classes for auth flows + stack-end report (`docs/auth-stack-report.md`); CspSmoke wired into CI smoke.
