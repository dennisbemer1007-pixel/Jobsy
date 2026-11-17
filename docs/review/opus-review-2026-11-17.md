# Lobsy review — 17 november 2026

Reviewer: senior review (AI-agent), branch `acceptatie` @ `8b6c5533a`.
Scope: security, pagina's, klantvriendelijkheid, kans op succes. **Geen productcode gewijzigd.**

---

## 0. Samenvatting (10 regels)

1. De techniek is serieuzer dan bij de meeste startups: de API weigert standaard (FallbackPolicy), het werkgeverszicht onthult gegevens pas stap voor stap, AI-teksten krijgen een feitencheck en de toestemmingsversies zet de server zelf.
2. **P0:** komt de JWT-sleutel niet goed in Render, dan start de API stil met de ontwikkelsleutels die in de repo staan. Iedereen kan dan zelf tokens maken die geaccepteerd worden, ook op acc en prod.
3. **P0:** de CV-extractie (LLM) zet een AI-samenvatting ("Over mij"), werkgevers en certificaten direct in het profiel. Er is geen controle tegen de CV-tekst en de kandidaat hoeft niets te bevestigen. Die "Over mij" gaat mee naar de werkgever. Dat botst met de regel "AI verzint nooit feiten" én met de eigen AI Act-analyse.
4. **P1:** als de geboortedatum ontbreekt, werkt de check op ouderlijke toestemming niet: zonder leeftijd is er geen toestemming nodig. Ook gaat de volledige CV-tekst (naam, telefoon) in productie naar OpenAI.
5. **P1:** de landingspagina en "Hoe werkt Lobsy" negeren de gekozen taal. In het Engels staat de pagina op acc grotendeels in het Nederlands (`LandingText.For` krijgt geen taal mee). Pools, Roemeens en Arabisch staan op acc/prod helemaal uit (`EnabledCultures: nl,en`).
6. **P1:** de `/westland`-pagina noemt tokenprijzen "na 18 november" (dus morgen) die niet kloppen met de code (vacature plaatsen is altijd gratis) en ook niet met de geseedde tokenprijzen.
7. Pagina's: geen horizontale scroll en geen kreeft over knoppen op de 12 geteste publieke pagina's (390 en 1280 px). Laadtijd op acc 0,2–1,1 s. Wel contrastfouten, een dubbele cookietekst op `/banenkaart` en kritieke aria-fouten op de banenkaart.
8. De taal is grotendeels B1 en vriendelijk ("Je naam hoeft niet. Lobsy kent alleen je code."). Wel zijn er veel beelden ("10 lagen schild", "pantser laten vallen"), en de foutmelding "Er ging iets mis." wordt in 39 bestanden gebruikt zonder volgende stap.
9. Product: het sterkste punt is de kandidaat-eerst-aanpak met reistijd op de kaart, plus het scholenproduct met anonieme codes. Het grootste probleem is de scope: 10 rollen, 43 feature flags en 11 mockupmappen in de repo-root, terwijl het verdienmodel (tokens bij acceptatie, Maqqie, Loopbaan-APK) in productie nog uitstaat.
10. **Eerlijk oordeel:** Lobsy *kan* slagen als regionaal product (Westland/Den Haag) met één betalende klantgroep (gemeente óf school). Met de huidige breedte en zonder live verdienmodel is die kans klein. Eerst de P0's, dan versmallen.

---

## 1. Werkwijze en grenzen

- **Code gelezen:** Jobsy.Api, Jobsy.Web, Jobsy.Core, Jobsy.Infrastructure, `render.yaml`, docs (o.a. `docs/paspoort-partners/ai-act-beoordeling.md`, `docs/legal/review-needed.md`).
- **Lokaal gedraaid:** `dotnet build Jobsy.sln` gaf 0 errors en 0 warnings, met .NET 10.0.404. API en Web draaiden in Development met seed. Daarop 25 anonieme routes gescand in nl/en/pl/ro/ar, en anonieme API-calls gecontroleerd (`/api/me`, `/api/applications`, `/api/admin/users`, `/api/teacher/classes` en `/api/me/privacy/export` gaven allemaal 401).
- **Acceptatie zonder inloggen:** 12 publieke pagina's via headless Chrome op 390×844 en 1280×800, met axe-core (WCAG 2 A/AA), meting van overflow en overlap door vaste elementen, en laadtijd.
- **Niet gedaan:** niet ingelogd op acc; geen leerlingsessies; geen formulieren verstuurd; de testsuite niet gedraaid (alleen de build). Ingelogde stromen (onboarding 1–10, leerling, leraar, werkgever) zijn **alleen in code** bekeken, niet visueel.

---

## 2. Bevindingen per onderwerp (kort)

### 2.1 Security en privacy

| # | Bevinding | Bewijs | Prio |
|---|---|---|---|
| S1 | Zonder PEM valt de API terug op het ontwikkelsleutelpaar uit de repo; in Production volgt alleen een `Console.Error`-melding | `Jobsy.Api/Authorization/AuthorizationExtensions.cs:36-51`; sleutels in `Jobsy.Core/Security/JobsyAccessToken.cs:137+`; `render.yaml:71-72` en `:267` (`sync: false`) | P0 |
| S2 | CV-extractie: LLM-samenvatting + werkgevers/certificaten worden automatisch opgeslagen; "Over mij" gaat in de sollicitatie-snapshot | Prompt vraagt `aboutMe: "korte NL samenvatting"` (`Jobsy.Infrastructure/Services/CvExtractionService.cs:25`); auto-merge `Jobsy.Api/Controllers/MeController.cs:1167-1200`; `Jobsy.Core/Rules/CvProfileMerge.cs:43-47,73-81`; snapshot `ApplicationsController.cs:976,985`, getoond aan werkgever `:309`; `LobsyCvModelFactory.cs:239`. Eigen AI Act-analyse eist "mits de kandidaat de velden controleert" (`docs/paspoort-partners/ai-act-beoordeling.md:166`) | P0 |
| S3 | Onbekende leeftijd gaat door de check heen: geen geboortedatum = geen ouderlijke toestemming nodig | `Jobsy.Core/Privacy/CandidateConsentRules.cs:22-26`; gebruikt in o.a. CV-upload `MeController.cs:1121` | P1 |
| S4 | Productie stuurt de volledige CV-tekst (incl. naam en telefoon) naar OpenAI | `render.yaml:127` ("Production AI stays on OpenAI"); `CvExtractionService.cs:22-24,105`; acc gebruikt Mistral EU (`render.yaml:356-369`) | P1 |
| S5 | ~37 cookie-POST/PUT-endpoints in de Web-BFF met `.DisableAntiforgery()` (o.a. werkgever "bericht versturen"). Wordt afgezwakt door SameSite=Lax | `Jobsy.Web/Hosting/WerkgeverClientEndpoints.cs:12-38`, `SollicitatiesClientEndpoints.cs:13-34`, `AfterMatch*ClientEndpoints.cs`; cookie Lax `Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs:73` | P2 |
| S6 | Web heeft alleen een DefaultPolicy en geen FallbackPolicy; 7 van de 224 pagina's hebben geen auth-attribuut | `AuthServiceCollectionExtensions.cs:449-456`; o.a. `Components/Pages/Public/PilotTest.razor`, `Dev/NaDeMatchPreview.razor:8-11` (geeft 200 "Not found." buiten Dev) | P2 |
| S7 | Leerlingcodes: 6 tekens uit 30 symbolen (~29,5 bit). Brute-force is goed begrensd (50 fouten per klas per uur, daarna 30 min pauze), maar een anonieme gebruiker kan zo een hele klas 30 min blokkeren. Een leraar kan dat opheffen. De Web-limiter gebruikt `RemoteIpAddress` in plaats van `TrustedClientIp` | `Jobsy.Core/Rules/ShortCodeFormat.cs:11-13`; `Jobsy.Core/Scholen/PupilLoginProtection.cs:34-44`; `Jobsy.Infrastructure/Scholen/PupilPortalService.cs:1565-1578`; opheffen `TeacherClassesController.cs:203`; `Jobsy.Web/Program.cs:444-452` vs `:436-437` | P2 |
| S8 | Vacature-HTML wordt opgeschoond met een eigen regex-allowlist en niet met een onderhouden sanitizer. Ik vond geen bypass, maar dit is breekbaar | `Jobsy.Core/Rules/HtmlSanitize.cs:14-16,116-207` | P2 |
| S9 | G78 "waarom past dit": de tekst gaat als `MarkupString` of zonder encode de HTML in (nu gevuld met eigen sjabloontekst) | `Jobsy.Web/Components/Leerling/LeerlingPaspoortG78View.razor:249`; `Jobsy.Web/Scholen/PupilLeerlingSsrParts.cs:314` | P2 |
| S10 | Malwarescan staat standaard uit en staat niet in `render.yaml`; is dan altijd "Clean". Bijlagen bij activiteiten en feedback-screenshots worden alleen op MIME-type gecontroleerd | `Jobsy.Api/appsettings.json:149-150`; `UploadMalwareScanner.cs:41-44`; `Jobsy.Core/Rules/JobSearchActivityAttachmentRules.cs:45-50`; `FeedbackScreenshotCodec.cs:47-88` | P2 |
| S11 | Oefensollicitatiegesprek: de OpenAI-foutbody wordt gelogd (max. 400 tekens) en de antwoorden krijgen geen CandidateFactGuard | `Jobsy.Infrastructure/Services/MockInterviewService.cs:278-282` | P2 |
| S12 | AI Act art. 50: alleen de coach heeft een AI-melding. "Wie ben ik", Kompas, RoleFit en de CultureFit-uitleg missen een label | `Jobsy.Web/Localization/UiStrings.cs:476` (alleen `Assistant.Disclaimer`) | P1 |
| S13 | Privacytekst §9 legt alleen 13/16/18 uit voor kandidaten. Groep 7/8 (onder de 13, school als verwerkingsverantwoordelijke) wordt niet uitgelegd | `Jobsy.Web/Components/Legal/Docs/PrivacyNl.razor:368-386`; jurist-vraag staat nog open in `docs/legal/review-needed.md:176` | P2 |
| S14 | Acc heeft seed-accounts met een bekend wachtwoord; dat is bewust. Wel `noindex` + `robots Disallow: /` (gecontroleerd), maar acc is niet afgeschermd | `render.yaml:251`; `Jobsy.Infrastructure/.../DemoUsersSeeder.cs:46` (waarde niet genoemd) | P3 (ops) |

**AI Act-grens (feiten):** de matching is regelgebaseerd (`MatchScoreCalculator.cs`, `ProfileVacancyMatchCalculator.cs`). Werkgevers zien geen Match% in `EmployerApplicationDto`. De AI-cultuurfit verandert `TotalPercent` niet. Grensgeval: Kandidatenkaart nodigt alleen kandidaten uit vanaf 70% match (`Jobsy.Core/Rules/KandidatenkaartRules.cs:6`; `KandidatenkaartService.cs:494,644`); de flag staat standaard uit (`FeatureFlagRegistry.cs:73`). De partnerpaspoort-PDF leest het AI-verhaal niet (`PassportPdfDownload.cs:15-16`), en `SnapshotWhoAmIJson = null` (`ApplicationsController.cs:1004`). **Maar** S2 brengt via "Over mij" toch LLM-tekst bij de werkgever.

### 2.2 Pagina's (gemeten op acc, 390 en 1280 px)

| Pagina | Laadtijd (load) | Overflow | Bevindingen (axe / visueel) |
|---|---|---|---|
| `/` | 0,9 s / 1,1 s | nee | contrast `.pub-pill` #15803d op #deebe0 = 4,07:1; "Moment 0" #c4704a op wit = 3,64:1; `div.lagen` met `aria-label` zonder role |
| `/login` | 0,3 s | nee | "Moment 0" contrast; placeholder `••••••••` lijkt een al ingevuld wachtwoord |
| `/banenkaart` | 0,5–0,7 s | nee | **lege `<h1>`**; combobox `#discovery-address` mist `aria-expanded/aria-controls` (critical); `role=list` met `role=button`-kinderen; link `.user` zonder naam; **cookiebanner toont beide teksten** ("…Optioneel anonieme statistieken. Cookies voor inloggen. Optioneel statistieken."); twee adresvelden tegelijk zichtbaar; zoek-placeholder afgekapt ("Wat voor werk zoe…") |
| `/leerling` | 0,3–0,5 s | nee | de knop "Kies" staat er ook als JS de keuze al automatisch verstuurt; de kaart is op 390 px niet even breed als het scherm |
| `/westland` | 0,5–0,8 s | nee | CTA wit op #f54a1b = 3,57:1; kicker 3,3:1; **cookie-privacylink wit op #fffcfa = 1,02:1 (onzichtbaar)** |
| `/bestaat-niet-xyz` | 0,2 s | nee | 404 werkt; eyebrow #f54a1b op #fffaf8 = 3,45:1 |
| `/account-maken`, `/hoe-werkt-lobsy`, `/scholen`, `/werkgevers/binnenkort`, `/privacy`, `/contact` | 0,2–0,4 s | nee | geen axe-fouten |

- **Huisstijl:** de header op alle publieke pagina's is crème (gemeten `rgb(255,248,231)`) en niet blauw #0a2044. #0a2044 bestaat wel als token (`Jobsy.Web/wwwroot/css/app.css:13,17`). Op de landingspagina is het logo op 390 px heel klein, op `/banenkaart` groot. De headers zijn dus niet consistent.
- **Kreeft:** de kreeft overlapt geen klikbare elementen op de geteste pagina's. Er is geen kreeft-headerstrook (#1339) in de markup. Dode resten staan er nog: `KreeftHeaderStrip.razor` en `candidate-header-strip.css` worden nog geladen in `Login.razor:44` en `Landing.razor:24`.
- **Vertaling/RTL:** `ar` geeft lokaal `<html lang="ar" dir="rtl">` (goed). Op acc staat alleen `nl,en` aan (`Jobsy.Web/appsettings.json:3`, `Jobsy.Api/appsettings.json:3`). Er zijn ~792 EN-kopieën als "vertaling" voor pl/ro/ar (bijv. `UiStringsAfterMatch.cs:13-19`). `EntDataTable.razor:22` toont `"{0} geselecteerd"` letterlijk als aria-label. `WestlandTeaser.razor` is volledig hardcoded Nederlands.

### 2.3 Klantvriendelijkheid

- **Goed:** `/hoe-werkt-lobsy`, `/scholen` en `/account-maken` gebruiken korte zinnen in je-vorm en beloftes als "Werkgevers zien je testantwoorden nooit". De leerling-login is simpel. Inlogfouten zeggen wat je moet doen.
- **Minder:** de beeldtaal (strand, kust, rotsen, diepte, licht, "10 lagen schild", "pantser laten vallen") is voor laaggeletterden en nieuwkomers abstract. Voor anonieme bezoekers staat die bovenaan, vóór de hoofdknop (`/`, `/login`). `Common.Error` = "Er ging iets mis." wordt gebruikt in 39 bestanden en geeft geen volgende stap (`UiStrings.cs:206`). `/scholen` linkt niet naar `/leerling`. `/werkgevers/binnenkort` ("komt in een volgende fase") en `/westland` ("Zet je vacature gratis online") spreken elkaar tegen.
- **Niet visueel getest:** onboarding 1–10, het leerlingtraject, het leraardashboard en het werkgeverportaal (geen login op acc).

### 2.4 Kan de app een succes worden?

**Waardepropositie (sterk):** "eerst jij, dan de baan". De gratis test werkt zonder account, het werk-DNA staat in gewone taal en de banenkaart toont reistijd per fiets, OV of auto. Werkgevers zien pas gegevens na jouw "ja". Voor de doelgroep ver van de arbeidsmarkt is dat een echt onderscheid ten opzichte van vacaturesites die op filters en cv's draaien. Het scholenproduct (klascode, geen account, alleen groepsbeelden voor de leraar) is privacy-vriendelijk en makkelijk in te voeren.

**Feiten die zorgen baren:**
- **Scope:** 10 `UserRole`-waarden (`UserRole.cs:6-20`), 43 flag-definities (`FeatureFlagRegistry.cs:55-101`), ~30 `PlatformFeature`-waarden en 11 mockupmappen in de repo-root (`uwv-mockups*`, `usability4-6`, …).
- **Verdienmodel nog niet live:** vacature plaatsen is gratis (`Jobsy.Core/Rules/VacancyPostingTokenRules.cs:3-14`). Tokens bij acceptatie kosten 0,5 (pilot) of 1 token × €25 (`AcceptCandidatePricingRules.cs:8`), maar alleen achter `EmployerPhase2Enabled`, en die staat standaard uit. Maqqie is alleen een CSV-stub (`FileMaqqieHoursExporter.cs:10-35`, `MaqqieHours:ExportEnabled: false`). Loopbaan-APK is gebouwd maar staat achter een flag die uit staat, met "Prijs op aanvraag" (`WerkgeverApkPackagePricing.cs:5-11`). Aan de kandidaatkant is de enige inkomstenbron de uitgebreide analyse van €2,99.
- **Prijzen spreken elkaar tegen:** `/westland` noemt €25 per token, 10 tokens = €250 en een jaardeal van €3.000 (`WestlandTeaser.razor:143-165`). De seed geeft 10 tokens = €40 (`PlatformSettingsSeeder.cs:17-21`). De specificatie noemt een bureau-jaardeal van €4.000 (`docs/FUNCTIONELE_SPECIFICATIES_LOBSY_PLATFORM.md`).
- **Gemeenten:** er is geen gemeente-rol of -product in de code. Alleen de UWV-toolkit (flag) en mockups bestaan.
- **Geen concurrentie-analyse** in de repo: er zijn geen treffers voor alternatieven als werk.nl, Indeed of Qompas.
- **Doelgroep nieuwkomers/arbeidsmigranten:** de Poolse, Roemeense en Arabische vertalingen staan in productie uit.

**Grootste risico's:**
1. Vertrouwen: één AI-fout over een kwetsbare kandidaat of minderjarige (S2/S3) kan het merk raken bij gemeenten en scholen.
2. Te veel tegelijk, met weinig focus op één betalende klant.
3. Het verdienmodel hangt aan de werkgeverskant, die bewust pas in fase 2 komt. Tot die tijd is er bijna geen omzet.
4. AI Act/AVG: zodra een gemeente of werkgever Lobsy-output gebruikt om kandidaten te selecteren, schuift Lobsy richting hoog risico (`ai-act-beoordeling.md:142`).

**Wat eerst moet om te slagen:** (1) de P0's en de AVG-P1's oplossen; (2) één betalende klantgroep voor 2027 kiezen en daar een licentie voor maken (gemeente-traject óf school-klaslicentie), terwijl kandidaten gratis blijven; (3) de flags terugbrengen tot wat die klant nodig heeft; (4) de taal voor nieuwkomers echt leveren (minimaal pl + ar voor landing, onboarding en banenkaart), of de belofte schrappen; (5) een KPI-pagina, want `/admin/launch` bestaat nog niet (`docs/TESTSCENARIOS_PER_ROL.md:53`).

---

## 3. Takenlijst (gesorteerd op prioriteit)

| # | Prio | Titel | Bundel |
|---|---|---|---|
| 1 | P0 | JWT fail-closed buiten Development | A |
| 2 | P0 | CV-extractie: geen AI-tekst of ongetoetste feiten in profiel of werkgeverssnapshot | B |
| 3 | P1 | Onbekende leeftijd = geblokkeerd (geen fail-open) | B |
| 4 | P1 | CV-tekst zonder PII naar de LLM | B |
| 5 | P1 | AI-label (art. 50) op alle AI-teksten | B |
| 6 | P1 | `LandingText.For` respecteert de taal | C |
| 7 | P1 | Cookiebanner: één tekst op elke pagina | D |
| 8 | P1 | `/westland`: prijzen en data gelijktrekken met de code | E |
| 9 | P2 | CSRF-bescherming voor de Web-BFF-endpoints | A |
| 10 | P2 | FallbackPolicy in Web + dev-pagina's echt 404 | A |
| 11 | P2 | Leerling-login: klasblokkade en limiter-partitie | A |
| 12 | P2 | Vacature-HTML via een onderhouden sanitizer | A |
| 13 | P2 | G78 WhyFit encoden | A |
| 14 | P2 | Uploads: malwarescan aan + magic bytes | A |
| 15 | P2 | Oefensollicitatiegesprek: geen body-logging + feitencheck | B |
| 16 | P2 | Privacytekst groep 7/8 + bewaartermijn school | B |
| 17 | P2 | pl/ro/ar: Engelse kopieën vervangen vóór aanzetten | C |
| 18 | P2 | Hardcoded NL-teksten naar UiStrings (Westland + dialogen) | C |
| 19 | P2 | `EntDataTable` aria-label met `{0}` | C |
| 20 | P2 | Contrast publieke pagina's naar ≥ 4,5:1 | D |
| 21 | P2 | Banenkaart: h1, combobox-aria, één adresveld | D |
| 22 | P2 | Reiskaart `div.lagen` aria + leerling-login "Kies"-knop | D |
| 23 | P2 | Foutmeldingen met volgende stap | E |
| 24 | P2 | Wachtwoord-placeholder en `/scholen` → `/leerling` | E |
| 25 | P3 | Dode kreeft-headerstrook verwijderen + test | D |
| 26 | P3 | Verouderde WhoAmI-teksten en ongebruikte G78-AI-service weg | B |
| 27 | P3 | Acc afschermen (Cloudflare Access/Basic Auth) | A |

### Bundel A — "Security-basis" (taken 1, 9–14, 27)

**1. P0 — JWT fail-closed buiten Development**
- *Waarom:* met de ontwikkelsleutels uit de repo kan iedereen geldige access tokens maken.
- *Bestanden:* `Jobsy.Api/Authorization/AuthorizationExtensions.cs:34-56`; `Jobsy.Core/Security/JobsyAccessToken.cs:135-152`; de Web-kant die `JobsyDevJwtKeys.PrivatePem` gebruikt (zelfde patroon, `:53-55`); `render.yaml:71-72, 177, 267, 422`.
- *Fix:* als `!environment.IsDevelopment()` en de PEM leeg is of geen `BEGIN` bevat, `throw new InvalidOperationException(...)` (de host start dan niet). Voeg een check toe die in niet-Development weigert als de geconfigureerde publieke sleutel gelijk is aan `DevelopmentPublicKeyPem`. Doe hetzelfde voor de Web-signer.
- *Acceptatie:* Production en Acceptatie starten niet zonder eigen PEM-paar. In Development blijft alles werken.
- *Test:* `JwtKeyBootstrapTests`: een `WebApplicationFactory` met `Environment=Production` en een lege PEM laat het opstarten falen. Met PEM = dev-PEM faalt het opstarten ook. Een token dat met de dev-private key is getekend krijgt 401 als er een echte PEM is ingesteld.

**9. P2 — CSRF-bescherming voor de BFF-endpoints**
- *Waarom:* ~37 muterende endpoints met cookie-auth hebben `.DisableAntiforgery()`. Alleen SameSite=Lax beschermt ze, en dat helpt niet tegen same-site subdomeinen.
- *Bestanden:* `Jobsy.Web/Hosting/*ClientEndpoints.cs` (o.a. `WerkgeverClientEndpoints.cs:12-38`, `SollicitatiesClientEndpoints.cs:13-34`, `AfterMatch*ClientEndpoints.cs`, `BanenkaartClientEndpoints.cs:25`, `SavedClientEndpoints.cs:12-14`).
- *Fix:* één endpoint-filter `RequireSameOriginFilter`: weigeren als `Sec-Fetch-Site` aanwezig is en niet `same-origin` is, óf als de `Origin`-host niet gelijk is aan `PublicWebBaseUrl`. Toepassen via een `MapGroup` op alle BFF-routes. Laat `.DisableAntiforgery()` alleen staan waar een token-link het enige bewijs is (mail-afmelden, toestemming).
- *Acceptatie:* een POST met `Origin: https://evil.example` geeft 403; dezelfde POST vanaf de eigen origin werkt.
- *Test:* `BffSameOriginTests` met een Theory over alle gemapte BFF-POST/PUT-routes (route-enumeratie via `EndpointDataSource`).

**10. P2 — FallbackPolicy in Web + dev-pagina's echt 404**
- *Bestanden:* `Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs:449-456`; `Components/Pages/Dev/NaDeMatchPreview.razor:8-11`; `Components/Pages/__Test/PwaVacancyApplySuccessPreview.razor:9-12`; `Public/PilotTest.razor`; `Candidate/SetUnavailableAction.razor`; `Candidate/WithdrawOthersAction.razor`; `Candidate/CandidatePassportRedirect.razor`; `Candidate/OnboardingStartNearbyRedirect.razor`.
- *Fix:* `options.FallbackPolicy = options.DefaultPolicy;` en `[AllowAnonymous]` expliciet zetten op de publieke pagina's. Dev- en testpagina's buiten Development niet mappen of `NotFound` teruggeven.
- *Acceptatie:* elke `@page` heeft een expliciet attribuut of valt onder de fallback. `/dev/...` geeft 404 in Production.
- *Test:* een reflectietest die alle routable components zonder `[Authorize]`/`[AllowAnonymous]` als fout meldt; een HTTP-test voor 404 op `/dev/na-de-match/01` met `Environment=Production`.

**11. P2 — Leerling-login: klasblokkade en limiter**
- *Waarom:* 50 foute pogingen per uur pauzeren een hele klas 30 minuten (`PupilPortalService.cs:1565-1578`). Een anonieme gebruiker kan dat tijdens een les doen. De Web-limiter partitioneert op `RemoteIpAddress` (`Jobsy.Web/Program.cs:444-452`), terwijl andere policies `TrustedClientIp` gebruiken (`:436-437`).
- *Fix:* gebruik `TrustedClientIp.PartitionKey(TrustedClientIp.Resolve(ctx))`. Tel voor de klaspauze alleen fouten uit ≥ 3 verschillende partities, of pauzeer per partitie. Toon de leraar een melding ("Je klas is gepauzeerd — klik om te hervatten"). De endpoint bestaat al: `TeacherClassesController.cs:203`.
- *Acceptatie:* 60 foute pogingen vanaf 1 IP blokkeren alleen dat IP, niet de klas. Het leraardashboard toont de pauzestatus.
- *Test:* uitbreiden in `Jobsy.Tests/Scholen/PupilRateLimitIntegrationTests.cs`.

**12. P2 — Vacature-HTML via een onderhouden sanitizer**
- *Bestanden:* `Jobsy.Core/Rules/HtmlSanitize.cs:116-207` (gerenderd in `VacancyDetail.razor:235`).
- *Fix:* `HtmlSanitizer` (Ganss.Xss, MIT) met dezelfde allowlist (`h2,h3,p,ul,ol,li,strong,b,em,i,u,a,br,span`). Alleen `href` (http/https/mailto), `style` met alleen `text-align` op `p` en een palette-`color` op `span`.
- *Acceptatie:* dezelfde uitvoer voor de bestaande fixtures; een XSS-corpus geeft geen handlers of `javascript:`.
- *Test:* `HtmlSanitizeXssCorpusTests` met minimaal 30 payloads (`<img/src/onerror>`, `<svg onload>`, `<a href="jav&#x61;script:">`, geneste tags).

**13. P2 — G78 WhyFit encoden**
- *Bestanden:* `Jobsy.Web/Components/Leerling/LeerlingPaspoortG78View.razor:249` (`@((MarkupString)job.WhyFit)`); `Jobsy.Web/Scholen/PupilLeerlingSsrParts.cs:314` (`{bodyText}`).
- *Fix:* `@job.WhyFit` en `{E(bodyText)}`. Is HTML nodig, gebruik dan alleen de `BuildHtml()`-variant die zelf encodeert (`PupilG78JobWhyContent.cs:46-78`).
- *Test:* SSR- en bUnit-test met WhyFit `"<img src=x onerror=alert(1)>"` controleert dat de uitvoer `&lt;img` bevat.

**14. P2 — Uploads: malwarescan aan + magic bytes**
- *Bestanden:* `Jobsy.Api/appsettings.json:149-150`; `render.yaml` (nog geen `UploadScan__*`); `Jobsy.Core/Rules/JobSearchActivityAttachmentRules.cs:45-50`; `FeedbackScreenshotCodec.cs:47-88`.
- *Fix:* `UploadScan__Enabled=true` + `Endpoint` op acc/prod (zie `docs/cv-upload-malware-scan.md`), en fail-closed als de scanner niet bereikbaar is. Bij bijlagen en screenshots de magic bytes controleren, zoals `CandidateCvFileRules.TryValidateBytes`.
- *Acceptatie:* EICAR wordt geweigerd op acc. Een `.exe` met `Content-Type: application/pdf` geeft 400.
- *Test:* unit-tests voor beide rules; een integratietest met een nep-scanner die `Infected` teruggeeft.

**27. P3 — Acc afschermen**
- *Waarom:* er staan seed-accounts met een bekend wachtwoord (`render.yaml:251`). Er is wel `noindex`, maar geen toegangsbeperking.
- *Fix (ops):* Cloudflare Access of een IP-allowlist op `acceptatie.lobsy.nl`, met een uitzondering voor health-probes.
- *Acceptatie:* een anonieme bezoeker van buitenaf krijgt een Access-scherm.

### Bundel B — "AI-feiten en AVG" (taken 2–5, 15, 16, 26)

**2. P0 — CV-extractie: geen AI-tekst of ongetoetste feiten**
- *Waarom:* de regel is dat AI nooit feiten over een kandidaat verzint. Nu belanden de LLM-samenvatting en de geëxtraheerde werkgevers en certificaten zonder controle in het profiel, en "Over mij" gaat naar de werkgever.
- *Bestanden:* `Jobsy.Infrastructure/Services/CvExtractionService.cs:15-36,131-160`; `Jobsy.Api/Controllers/MeController.cs:1167-1200`; `Jobsy.Core/Rules/CvProfileMerge.cs:43-47,73-92`; `Jobsy.Api/Controllers/ApplicationsController.cs:976,985`; `Jobsy.Web/Components/Candidate/ProfileSections/CandidateProfileEditor.cs:1379-1390`.
- *Fix:*
  1. Haal `aboutMe` uit de prompt en het contract, of vul het nooit automatisch.
  2. Nieuwe regel `CvExtractionGrounding.Filter(extracted, cvText)`: houd een werkgever, certificaat of opleiding alleen als de genormaliseerde naam letterlijk in `cvText` voorkomt.
  3. Sla geëxtraheerde velden op als *voorstel* (`CandidateUploadedCv.SuggestedFieldsJson`). Neem ze pas over na "Klopt dit?" van de kandidaat, met per veld Ja/Nee.
- *Acceptatie:* na een CV-upload blijft "Over mij" leeg. Een werkgever die niet in de CV-tekst staat wordt nooit opgeslagen. De sollicitatie-snapshot bevat alleen bevestigde velden.
- *Test:* `CvExtractionGroundingTests` (nepwerkgever "Shell BV" niet in de tekst → weg); een integratietest van `POST /api/me/cv` met een stub-LLM die `aboutMe` en een verzonnen werkgever teruggeeft → het profiel is ongewijzigd tot `POST /api/me/cv/confirm`.

**3. P1 — Onbekende leeftijd = geblokkeerd**
- *Bestanden:* `Jobsy.Core/Privacy/CandidateConsentRules.cs:22-26`; de 17 aanroepen van `CanUseCandidateFeatures` in Api/Infrastructure.
- *Fix:* `RequiresParentalConsent` geeft `true` als `DateOfBirth is null`, of er komt een aparte `HasKnownAge`-gate. Daarbij een melding: "Vul eerst je geboortedatum in. Dan weten we of je ouder toestemming moet geven."
- *Acceptatie:* een account zonder geboortedatum kan geen CV uploaden, geen AI-functies gebruiken en niet solliciteren. Met een geboortedatum ≥ 16 werkt alles zoals nu.
- *Test:* een Theory in `CandidateConsentRulesTests` (null / 12 / 15 / 16 / 18); een API-test op `POST /api/me/cv` zonder geboortedatum → 400 met de nieuwe melding.

**4. P1 — CV-tekst zonder PII naar de LLM**
- *Bestanden:* `CvExtractionService.cs:22-24,105`; `render.yaml:127`.
- *Fix:* naam, e-mail, telefoon en adres lokaal halen of maskeren (regex + `CandidatePhoneRules`). Stuur `cvText` met `[NAAM]`, `[TELEFOON]`, `[EMAIL]` en `[ADRES]`. Haal `firstName/lastName/phoneNumber` uit de prompt. Of zet ook productie op Mistral EU (beslissing, zie §4).
- *Acceptatie:* het uitgaande request bevat geen e-mailadres of telefoonnummer uit de fixture.
- *Test:* `CvExtractionRedactionTests` met een `HttpMessageHandler`-spy.

**5. P1 — AI-label (art. 50)**
- *Bestanden:* teksten in `UiStringsWhoAmI.cs`, `UiStringsCareer.cs`, `UiStringsEmployerFit.cs`/RoleFit, `UiStringsCulture.cs`; bestaand voorbeeld `UiStrings.cs:476`.
- *Fix:* component `<AiGeneratedLabel />` ("Gemaakt met AI. Klopt iets niet? Pas het aan of meld het.") bij elk door een LLM gemaakt blok. Dus wel bij een LLM-uitkomst, niet bij de lokale fallback.
- *Acceptatie:* het label staat er alleen als de tekst echt van de LLM komt.
- *Test:* een bUnit-test per component met `Source=Ai` → label aanwezig, en met `Source=Local` → geen label.

**15. P2 — Oefensollicitatiegesprek**
- *Bestanden:* `Jobsy.Infrastructure/Services/MockInterviewService.cs:278-282`.
- *Fix:* geen body loggen (alleen de status, zoals `CvExtractionService.cs:112-114`). Antwoorden door `CandidateFactGuard`/`AssistantReplyGuard` halen.
- *Test:* een logger-spy controleert dat er geen body in de logs staat; een guard-test met een verzonnen diploma geeft de fallbacktekst.

**16. P2 — Privacytekst groep 7/8 + bewaartermijn school**
- *Bestanden:* `Jobsy.Web/Components/Legal/Docs/PrivacyNl.razor:368-386`; `LegalRetention`-catalogus; `SchoolRetentionService.cs:255-316`.
- *Fix:* een B1-alinea: "Doe je mee via school? Dan is je school verantwoordelijk. Lobsy kent alleen je code, niet je naam. Aan het eind van het schooljaar wissen we alles." Voeg de schoolretentie toe aan de catalogus.
- *Test:* een snapshot-test van de privacypagina op de nieuwe sectie-id; `LegalRetentionTests` met een schoolregel.

**26. P3 — Opruimen van verouderde AI-paden**
- *Bestanden:* `Jobsy.Web/Localization/UiStringsWhoAmI.cs:53-54,83-84` (ongebruikte teksten die zeggen dat het AI-verhaal meegaat naar werkgevers); de DI-registratie van `PupilG78MirrorService` (`Jobsy.Infrastructure/DependencyInjection.cs:535`).
- *Fix:* verwijderen, zodat niemand ze later weer aansluit.
- *Test:* een test dat `IPupilG78MirrorService` niet resolvebaar is.

### Bundel C — "Taal en vertalingen" (taken 6, 17, 18, 19)

**6. P1 — `LandingText.For` respecteert de taal**
- *Waarom:* 279 van de 285 aanroepen geven geen taal mee, en `UiStrings.Get(key, null)` geeft dan Nederlands. Op acc staat in het Engels "In drie stappen van jou naar werk" en "Soms moet je uit je schild groeien".
- *Bestanden:* `Jobsy.Web/Localization/LandingText.cs:10-25`; aanroepen in `Components/Landing/*.razor`, `Pages/Landing.razor`, `Pages/HowLobsyWorks.razor`, `Pages/Public/GratisDna.razor`, `Public/GratisDna*.razor`, `Layout/Public/PublicFooter.razor`.
- *Fix:* maak `LandingText.For(string key, LandingVariant v, CultureState culture)` verplicht, of voeg een instance-helper `@inject LandingTextService`. Vervang alle aanroepen.
- *Acceptatie:* met `Jobsy.Culture=en` op `/` staat "Three steps from you to work" en geen "In drie stappen".
- *Test:* een HTTP-test die `/`, `/hoe-werkt-lobsy` en `/ontdek` rendert met het en-cookie en controleert dat ≥ 5 kopteksten Engels zijn. Plus een Roslyn- of grep-test dat er geen `LandingText.For(` zonder taalargument meer is.

**17. P2 — pl/ro/ar: Engelse kopieën vervangen vóór aanzetten**
- *Bestanden:* `UiStringsAfterMatch.cs:13-19`, `UiStringsEnterprise.cs:11-17`, `UiStringsInitiative.cs`, `UiStringsKandidatenkaart.cs`, `UiStringsReferenceVerification.cs`, `UiStringsTrainingPartners.cs` (patroon `pl[key]=enText`); onboardingteksten in de pl/ro/ar-blokken van `UiStrings.cs` die nog Engels zijn (bijv. `Onboarding.AddFirstJob`).
- *Fix:* echte vertalingen, eerst voor landing, onboarding, banenkaart en login.
- *Acceptatie:* voor de kandidaatflows heeft pl/ro/ar geen waarde meer die gelijk is aan EN.
- *Test:* `LocalizationParityTests`: voor sleutels met prefix `Onboarding.`, `Landing.`, `Discovery.` en `Login.` mag pl/ro/ar niet gelijk zijn aan en (met een allowlist voor merknamen).

**18. P2 — Hardcoded NL-teksten naar UiStrings**
- *Bestanden:* `Jobsy.Web/Components/Pages/WestlandTeaser.razor` (hele pagina); dialogen `UnsubscribeDialog.razor:189`, `PushBomConfirmDialog.razor:182`, `TokenTopUpDialog.razor:140`.
- *Fix:* sleutels `Westland.*` en `Dialog.*` met nl en en.
- *Test:* een HTTP-test van `/westland` met het en-cookie.

**19. P2 — `EntDataTable` aria-label met `{0}`**
- *Bestand:* `Jobsy.Web/Components/Ui/Enterprise/EntDataTable.razor:22`.
- *Fix:* `aria-label="@Culture["EntUi.SelectAll"]"` (nieuwe sleutel "Alles selecteren"), of `Culture.Format("EntUi.Selected", SelectedCount)`.
- *Test:* een bUnit-test die controleert dat het aria-label geen `{` bevat.

### Bundel D — "Toegankelijkheid en layout publieke pagina's" (taken 7, 20, 21, 22, 25)

**7. P1 — Cookiebanner: één tekst**
- *Bestanden:* `Jobsy.Web/Components/CookieConsentBanner.razor:17-18`. De verbergregels staan alleen in `wwwroot/css/app.css:14167-14176` en `css/features/public-theme.css:432-437`, niet in `css/features/cookie-consent.css` (die laadt `/banenkaart`).
- *Fix:* de `text-full`/`text-short`-regels naar `cookie-consent.css` verplaatsen. Daarnaast de privacylink: wit op #fffcfa = 1,02:1 op `/westland`, dus kleur `var(--brand)`.
- *Acceptatie:* op `/banenkaart`, `/` en `/westland` is op 390 en 1280 px precies één tekst zichtbaar en is de privacylink leesbaar.
- *Test:* een Playwright-test (bestaand patroon `*PlaywrightTests.cs`) die op 3 pagina's × 2 breedtes het aantal zichtbare `.cookie-consent__text-*` telt (= 1).

**20. P2 — Contrast ≥ 4,5:1**
- *Gemeten:* `.pub-pill`/`.pub-landing__get-tag` #15803d op #deebe0 (4,07); "Moment 0" #c4704a op #fff (3,64); `/westland` `.wl-btn--primary` wit op #f54a1b (3,57), `.wl-kicker` (3,3), `.wl-rate__bonus` (4,25), `.wl-rate__badge` (3,89); 404 `.err-eyebrow` (3,45); notificatiebel "ongelezen" #c9a227 op crème (icoon < 3:1, `app.css:5904-5918`, `features/werkgever.css:65-70`).
- *Fix:* groen #166534, koraal als tekst `--coral-deep` of donkerder (#b8320f), CTA-achtergrond #c53d14 of donkerder, goud als tekst/icoon `--gold-ink` (#5c4a0f).
- *Acceptatie:* axe `color-contrast` = 0 op `/`, `/login`, `/westland` en `/bestaat-niet-xyz` (390 + 1280).
- *Test:* `PublicPagesAxePlaywrightTests` met axe-core en de regels `color-contrast`, `aria-*` en `link-name`.

**21. P2 — Banenkaart a11y en dubbel adresveld**
- *Gemeten:* lege `<h1>`; `#discovery-address` en `#discovery-address-prompt` (role=combobox) missen `aria-expanded`/`aria-controls`; `.strip__track` role=list met role=button-kinderen; `a.user` heeft geen toegankelijke naam. Op 390 en 1280 staan het zijpaneel "Jouw adres" en de overlay "Waar woon je?" tegelijk in beeld. De placeholder "Wat voor werk zoek je?" wordt afgekapt.
- *Bestanden:* `Jobsy.Web/Components/Pages/VacancyDiscovery.razor` (+ de strip- en headercomponenten).
- *Fix:* een zichtbare of visueel verborgen h1 "Banenkaart"; aria-attributen toevoegen; `role=listitem`-wrappers; `aria-label="Inloggen"` op `a.user`. Verberg het zijpaneel-adres zolang de overlay open is. Kortere placeholder ("Welk werk?") of een breder veld.
- *Acceptatie:* axe critical/serious = 0; één adresveld zichtbaar.
- *Test:* in dezelfde axe-test als taak 20, plus een Playwright-assert op het aantal zichtbare `input[placeholder^="Straat"]` (= 1).

**22. P2 — Reiskaart-aria + leerling-login**
- *Bestanden:* het `div.lagen` met `aria-label` zonder role (reiskaart op `/` en `/login`; teksten `UiStringsKreeftReiskaart.cs:30-32`); `Jobsy.Web/Scholen/PupilLeerlingLoginSsrHtml.cs` + `wwwroot/js/leerling-login.js:43-56`.
- *Fix:* `role="img"` (of de tekst zichtbaar maken). Verberg de knop "Kies" met `.js-on`-CSS als het script geladen is. Maak de kaart op 390 px `width:100%` met gelijke marges.
- *Test:* een axe-test op `/login`; een Playwright-test op `/leerling` 390 die controleert dat "Kies" onzichtbaar is en dat de linker- en rechtermarge van de kaart ±2 px gelijk zijn.

**25. P3 — Dode kreeft-headerstrook verwijderen**
- *Bestanden:* `KreeftHeaderStrip.razor`, `KreeftHeaderStripHtml`, `wwwroot/css/.../candidate-header-strip.css`; links in `Login.razor:44`, `Landing.razor:24`, `PageAssetCatalog.cs:30`.
- *Fix:* verwijderen (de strook uit #1339 mag niet terugkomen).
- *Test:* de bestaande assert in `SollicitatiesDesktopWidthPlaywrightTests.cs:185-187` + een test dat de asset-catalogus geen `candidate-header-strip` bevat.

### Bundel E — "Klanttaal en consistentie" (taken 8, 23, 24)

**8. P1 — `/westland`: prijzen en data gelijktrekken**
- *Waarom:* de pagina zegt dat na 18 november "standaardtarieven" gelden, met €25 per token en een jaardeal van €3.000 (`WestlandTeaser.razor:134-178`). De code houdt plaatsen altijd gratis (`VacancyPostingTokenRules.cs:3-14`) en de seed geeft 10 tokens = €40 (`PlatformSettingsSeeder.cs:17-21`). `FreePublishRules.cs:11` = 18-11-2026, dus morgen. Daarna klopt "we komen eraan" en "Gratis tot 18 november" niet meer.
- *Fix (na beslissing §4):* de tekst afleiden uit `FreePublishUntil` en de `TokenPricings`, of de pagina na de datum laten doorverwijzen naar de banenkaart. De tekst wordt: "Vacatures plaatsen is gratis. Je betaalt pas als je een kandidaat aanneemt."
- *Acceptatie:* de prijzen op de pagina zijn gelijk aan `TokenPricings` en `AcceptCandidatePricingRules`. Na `FreePublishUntil` staat er geen "komt eraan".
- *Test:* een bUnit- of HTTP-test met een vaste `TimeProvider` op 19-11-2026.

**23. P2 — Foutmeldingen met volgende stap**
- *Bestanden:* `UiStrings.cs:206` (`Common.Error`), gebruikt in 39 bestanden (o.a. `Components/Candidate/Tests/TestConsentGate.razor:132,140`).
- *Fix:* kandidaatpagina's gebruiken `Common.ErrorGeneric` of een specifieke tekst: "Dat lukte niet. Probeer het zo nog eens. Lukt het niet? Mail support@lobsy.nl."
- *Test:* een test dat `Common.Error` niet meer voorkomt in `Components/Candidate/**` en `Components/Leerling/**`.

**24. P2 — Kleine B1-verbeteringen**
- *Bevindingen:* de wachtwoord-placeholder `••••••••` op `/login` lijkt een ingevuld wachtwoord; `/scholen` heeft geen link naar `/leerling` ("Ben je leerling? Log hier in met je code").
- *Fix:* placeholder leeg laten of "Je wachtwoord"; een link op `/scholen` toevoegen.
- *Test:* een HTTP-test op `/scholen` voor `href="/leerling"`.

---

## 4. Productbeslissingen voor Dennis

1. **Wat kost een werkgever na 18 november?** Kies één model en één prijslijst. Nu staan er drie versies: de `/westland`-tekst (€25 per token, jaardeal €3.000), de seed (1 token €5 … 10 tokens €40) en de specificatie (bureau €4.000 per jaar). Daarnaast geldt in de code "plaatsen altijd gratis, betalen bij acceptatie".
2. **Werkgevers in productie aan of niet?** `/werkgevers/binnenkort` zegt "volgende fase", maar `/westland` werft werkgevers voor de start op 18 november. `EmployersEnabled` staat in productie standaard uit (`FeatureFlagRegistry.cs`) en op acc aan.
3. **Welke talen beloof je?** In productie staan alleen nl en en aan. Voor nieuwkomers en arbeidsmigranten zijn pl en ar het verschil. Kies: echt vertalen (taak 17), of de belofte voorlopig niet doen.
4. **AI-provider in productie:** OpenAI (VS) of Mistral EU zoals op acc? Dit raakt de AVG-uitleg en de privacypagina.
5. **CV-extractie:** blijft het een automatische invulling (met bevestiging, taak 2), of alleen een "voorstel"-scherm? En mag AI ooit "Over mij" schrijven? Advies: nee, de kandidaat schrijft dat zelf, eventueel met hulpvragen.
6. **Beeldtaal (strand → licht, "10 lagen schild", "pantser"):** sterk merk, maar abstract voor laaggeletterden. Laat je het testen bij 5 mensen uit de doelgroep? Of toon je het pas ná de eerste stap, en niet vóór de hoofdknop?
7. **Huisstijl van de header:** publieke pagina's hebben een crème header en geen blauwe (#0a2044). Is dat zo bedoeld (plain-stijl), of moet die blauw zijn?
8. **Eerste betalende klant:** gemeente (traject/re-integratie) of school (klaslicentie)? Er bestaat nog geen gemeente-rol in de code. De scholenkant is verder.
9. **Kandidatenkaart (uitnodigen vanaf 70% match):** past dit binnen "eerst kandidaat"? En blijft het buiten de AI Act-hoogrisicogrens als er ooit AI in de match komt (`ai-act-beoordeling.md:126`)?
10. **Ouderlijke toestemming per e-mail:** is een klik op een mail genoeg bewijs? Dit staat open bij de jurist (`docs/legal/review-needed.md:176`).
11. **Scope versmallen:** welke van de 43 flags en 11 mockupmappen gaan weg vóór de eerste klant? Advies: alles wat niet direct bij kandidaat, banenkaart, school of de gekozen betaler hoort, uit de hoofdcode.
12. **Acc publiek of afgeschermd?** Nu is acc open met bekende demo-wachtwoorden (taak 27).

---

## 5. Wat goed is (geen actie nodig)

- De API weigert standaard: `FallbackPolicy = RequireAuthenticatedUser` (`Jobsy.Api/Authorization/AuthorizationExtensions.cs:354-356`). Rollen en bedrijfsclaims komen uit de DB en niet van de client.
- Progressive disclosure: naam pas na Accept, e-mail en telefoon pas na Hired (`LobsyCvAccessRules.cs:11-20`); company scope via `CompanyAuthorizationService.cs:72-117`.
- Leraren zien alleen hun eigen klas (`SchoolScopeService.cs:61-75`). De leerlinglogin werkt pas als de school de verwerkersovereenkomst heeft getekend (`PupilPortalService.cs:289-297`). Leerlingcodes worden met HMAC opgeslagen (`PupilCodeService.cs:99-109`) en aan het eind van het schooljaar gewist (`SchoolRetentionService.cs:286-316`).
- De server zet de toestemmingsversies zelf (`MeController.cs:1257-1278`).
- `CandidateFactGuard` met lokale fallbacks en timeouts (`WhoAmIGenerationService.cs:69-137`). Afwijzingen worden gelogd zonder PII (`AiFactRejectionLog.cs`).
- De partner-PDF leest geen AI-verhaal (`PassportPdfDownload.cs:15-16`); `SnapshotWhoAmIJson = null` (`ApplicationsController.cs:1004`).
- Security-headers op acc (gecontroleerd): CSP met nonce, `X-Frame-Options: DENY`, `nosniff`, Referrer-Policy en Permissions-Policy. Acc heeft `noindex` en `robots Disallow: /`.
- In e-mails worden gebruikerswaarden ge-encodeerd (`EmailRenderer.cs:652-653`); `returnUrl` wordt veilig gemaakt (`AuthRedirects.cs:267-365`).
- Op de geteste publieke pagina's: geen horizontale scroll op 390 px, geen kreeft over knoppen, geen ruwe `{0}` in zichtbare tekst, laadtijd < 1,2 s.
- Build: 0 errors, 0 warnings.

## 6. Niet geverifieerd

- Of op acc en prod in het Render-dashboard echte JWT-PEM's, `CLOUDFLARE_ORIGIN_SECRET` en `UploadScan__*` staan (alleen in de code gezien dat dit kan ontbreken).
- Ingelogde schermen (onboarding 1–10, leerling, leraar, werkgever) op 390 en 1280 px.
- Eén CSP-melding ("inline script") op `/leerling`, twee keer gezien tijdens een run met meerdere pagina's na elkaar. Niet te reproduceren in een schone sessie.
- Of de partnerweergave van het paspoort al ergens live via HTTP te bekijken is (`CanPartnerViewAsync` in `PassportPartnerService.cs:204-256` wordt alleen in tests aangeroepen).
