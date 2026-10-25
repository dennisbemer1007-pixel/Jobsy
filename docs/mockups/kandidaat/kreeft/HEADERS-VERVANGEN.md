# Headerillustraties vervangen: kreeftreis (kandidaat)

**Basis:** `origin/acceptatie` @ `978927742521ebddc70d91df324980b734acb601` (commit 25-10-2026 13:27 Europe/Amsterdam). Alleen gelezen, niets gewijzigd.
**Nieuwe bestanden:** in de repo onder `docs/mockups/kandidaat/kreeft/src/strips/` (hieronder afgekort als `strips/`). Desktop `-desk.jpg` is ~225 px hoog, mobiel `-mob.jpg` is ~105 px hoog.
**Paden:** codepaden zijn relatief aan `Jobsy.Web/`, assetpaden relatief aan `Jobsy.Web/wwwroot/images/`.
**Varianten:** waar een asset ook als `.webp` of `-2x` bestaat, vervang je de hele set (in de tabel staat `(+webp/-2x)`).

> **Variant C:** `CandidateIllustratedHero` rendert standaard variant C (`Features/CandidateHeroCompactPreviewGate.cs`, tenzij `?heroPreview=legacy`). Daarbij wordt `ArtUrl` via `Media/CompactHeroPreviewArt.cs` (`CandHeroArtForVariantC`) omgezet naar een band-C-bestand. Bij die pagina's staan zowel de ingestelde als de werkelijk getoonde asset vermeld. Je kunt het beste de constanten in `Media/CandidateIllustratedImages.cs` en de `*IllustrationAssets.cs`-klassen omzetten, en daarna de mapping in `CompactHeroPreviewArt` naar het nieuwe bestand laten wijzen.

| nr | scherm | nieuw bestand desktop | nieuw bestand mobiel | route | huidig bestand(en) Razor | huidige asset(s) | actie |
|---|---|---|---|---|---|---|---|
| 01 | Landing | `strips/landing-desk.jpg` | `strips/landing-mob.jpg` | `/` | `Components/Pages/Landing.razor` → `Components/Landing/LandingHero.razor:9` (`--pub-hero-art`) | `candidate-illustrated/finale-hero.jpg` (desk + mob) | vervangt `candidate-illustrated/finale-hero.jpg` |
| 02 | Inloggen (+ account maken / code) | `strips/inloggen-desk.jpg` | `strips/inloggen-mob.jpg` | `/login`, `/account-maken` | `Components/Pages/Login.razor:41`, `Components/Pages/AccountMaken.razor:26` (`--au-art-left/right`, `--au-art-mobile`) | desk: `candidate-illustrated/spots/login-links.jpg` + `spots/login-rechts.jpg` (zijpanelen); mob (alleen account-maken): `candidate-illustrated/kennismaken.jpg` | vervangt `spots/login-links.jpg` + `spots/login-rechts.jpg` (en mob `kennismaken.jpg` op account-maken). Let op: de layout heeft nu twee zijpanelen en geen strip. |
| 03 | Strand · Start (routekaart) | `strips/route-kaart-desk.jpg` | `strips/route-kaart-mob.jpg` | `/candidate/start` | `Onboarding/OnboardingStartWelcomeSsrShell.cs:155-165` (`AppendHero`); Blazor-variant `Components/Candidate/Onboarding/OnboardingRustigWelcome.razor:73` | SSR: `onboarding/rustig-panorama/start-mobile.jpg` (desk + mob); Blazor: `rustig-panorama/start-desktop.jpg` / `start-mobile.jpg` | vervangt `rustig-panorama/start-desktop.jpg` + `start-mobile.jpg` |
| 04 | Kust · kennismaken | `strips/kust-kennismaken-desk.jpg` | `strips/kust-kennismaken-mob.jpg` | `/candidate/start?step=1` | `Components/Pages/Candidate/OnboardingStartSsr.razor:63-64,173` (`OnboardingRustigHero`), `Onboarding/OnboardingRustigPanoramaAssets.cs` | `onboarding/rustig-panorama/step-01-desktop.jpg` / `step-01-mobile.jpg` | vervangt `rustig-panorama/step-01-*.jpg` |
| 05 | Kust · wanneer | `strips/kust-wanneer-desk.jpg` | `strips/kust-wanneer-mob.jpg` | `/candidate/start?step=2` | idem | `rustig-panorama/step-02-desktop.jpg` / `step-02-mobile.jpg` | vervangt `rustig-panorama/step-02-*.jpg` |
| 06 | Kust · vervoer | `strips/kust-vervoer-desk.jpg` | `strips/kust-vervoer-mob.jpg` | `/candidate/start?step=3` | idem | `rustig-panorama/step-03-desktop.jpg` / `step-03-mobile.jpg` | vervangt `rustig-panorama/step-03-*.jpg` |
| 07 | Rotsen · werk | `strips/rotsen-werk-desk.jpg` | `strips/rotsen-werk-mob.jpg` | `/candidate/start?step=4` | idem | `rustig-panorama/step-04-desktop.jpg` / `step-04-mobile.jpg` | vervangt `rustig-panorama/step-04-*.jpg` |
| 08 | Rotsen · opleiding | `strips/rotsen-leren-desk.jpg` | `strips/rotsen-leren-mob.jpg` | `/candidate/start?step=5` | idem | `rustig-panorama/step-05-desktop.jpg` / `step-05-mobile.jpg` | vervangt `rustig-panorama/step-05-*.jpg` |
| 09 | Rotsen · droombaan | `strips/rotsen-droom-desk.jpg` | `strips/rotsen-droom-mob.jpg` | `/candidate/start?step=6` | `Components/Candidate/Onboarding/OnboardingRustigDreamStep.razor`, `Onboarding/OnboardingRustigDreamIllustrationAssets.cs` | `onboarding/rustig-mockup/step-06-droom-professions-u6-aquarel.jpg` / `…-aquarel-mobile.jpg` | vervangt `rustig-mockup/step-06-droom-professions-u6-aquarel(-mobile).jpg` |
| 10 | Diepte · Hoe werk jij | `strips/diepte-werken-desk.jpg` | `strips/diepte-werken-mob.jpg` | `/candidate/start?step=7` (ook toestemmingsscherm); los: `/candidate/competencies` | `Onboarding/OnboardingRustigStep07IllustrationAssets.cs`, `OnboardingRustigTestConsentStep.razor`, `TestJourneyConsentStep.razor:18-20`; los: `TestPageShell` → `TestQuestionCompactHeader.razor` / `TestBlockBackground`, slug via `Jobsy.Core/Rules/TestBlockArt.cs` | wizard: `rustig-mockup/step-07-hoe-werkt-u7a.jpg` / `-mobile.jpg`; consent (oud): `onboarding/wie-ben-jij-reis-lobsy.webp/.jpg`; los: `tests/bg-hoe-je-werkt.jpg` + `tests/figures/hoe-je-werkt-hero.png` (desk) / `hoe-je-werkt-mobhero.png` (mob) (`css/features/tests.css:650-680`) | vervangt `rustig-mockup/step-07-hoe-werkt-u7a(-mobile).jpg` (en eventueel de `tests/*hoe-je-werkt*`-set) |
| 11 | Diepte · Wat vind je leuk | `strips/diepte-leuk-desk.jpg` | `strips/diepte-leuk-mob.jpg` | `/candidate/start?step=8`; los: `/candidate/career` | `OnboardingStartSsr.razor` + `OnboardingRustigPanoramaAssets.cs`; los: als rij 10 | `rustig-panorama/step-08-desktop.jpg` / `step-08-mobile.jpg`; los: `tests/bg-leuk.jpg(.webp)` + `tests/figures/leuk-hero.png` / `leuk-mobhero.png` | vervangt `rustig-panorama/step-08-*.jpg` (en eventueel de `tests/*leuk*`-set) |
| 12 | Diepte · Even rusten | `strips/diepte-pauze-desk.jpg` | `strips/diepte-pauze-mob.jpg` | – | – | – | pagina bestaat nog niet in de app (er is geen pauzescherm tussen de tests) |
| 13 | Diepte · Waar voel je je thuis | `strips/diepte-thuis-desk.jpg` | `strips/diepte-thuis-mob.jpg` | `/candidate/start?step=9`; los: `/candidate/culture` | als rij 11 | `rustig-panorama/step-09-desktop.jpg` / `step-09-mobile.jpg`; los: `tests/bg-sfeer.jpg` + `tests/figures/sfeer-hero.png` / `sfeer-mobhero.png` | vervangt `rustig-panorama/step-09-*.jpg` (en eventueel de `tests/*sfeer*`-set) |
| 14 | Diepte · Wat vind je belangrijk | `strips/diepte-waarden-desk.jpg` | `strips/diepte-waarden-mob.jpg` | `/candidate/start?step=10`; los: `/candidate/values` | als rij 11 | `rustig-panorama/step-10-desktop.jpg` / `step-10-mobile.jpg`; los: `tests/bg-waarden.jpg` + `tests/figures/waarden-hero.png` / `waarden-mobhero.png` | vervangt `rustig-panorama/step-10-*.jpg` (en eventueel de `tests/*waarden*`-set) |
| 15 | Licht · Kompas | `strips/licht-kompas-desk.jpg` | `strips/licht-kompas-mob.jpg` | `/candidate/start/kompas` | `Components/Pages/Candidate/OnboardingKompasReadySsr.razor:112-113`, `OnboardingRustigKompasStep.razor`, `Onboarding/OnboardingRustigKompasIllustrationAssets.cs` | desk: `candidate-illustrated/kompas-hero-band-c.jpg`; mob: `onboarding/rustig-mockup/kompas-c-hero-mobile.jpg` | vervangt `kompas-hero-band-c.jpg` + `rustig-mockup/kompas-c-hero-mobile.jpg` (de hub `/candidate/kompas` heeft geen headerbeeld) |
| 16 | Paspoort | `strips/licht-paspoort-desk.jpg` | `strips/licht-paspoort-mob.jpg` | `/candidate/paspoort` | `CandidatePassportSsr.razor` → `Passport/PassportSsrShells.cs:82-93` | `candidate-illustrated/kompas-hero-wide.jpg` (desk + mob, `KompasHeroWide`) | vervangt `candidate-illustrated/kompas-hero-wide.jpg` (alleen hier; dit bestand wordt ook bij rij 34/37 gebruikt) |
| 17 | Mijn Lobsy (dashboard) | `strips/mijn-lobsy-desk.jpg` | `strips/mijn-lobsy-mob.jpg` | (`/home`) | `Components/Pages/RoleHome.razor` → `Components/Candidate/CandidateHomePanel.razor` | – (een kandidaat op `/` of `/home` gaat naar `/banenkaart`; er is geen dashboard en geen header) | pagina bestaat nog niet in de app |
| 18 (+39 Geen matches, 40 Lobsy-hulp) | Match | `strips/match-desk.jpg` | `strips/match-mob.jpg` | `/candidate/match` | `Components/Pages/Candidate/MatchPage.razor` (lege staat r. 95-101) | geen strip; alleen klein merkteken `brand/lobsy-128.png` (`BrandImages.Png128`, r. 26) | nieuw (er is nu geen header) |
| 19 | Banenkaart | `strips/banenkaart-desk.jpg` | `strips/banenkaart-mob.jpg` | `/banenkaart` | `BanenkaartSsr.razor` → `Banenkaart/BanenkaartSsrShells.cs:296-303` | `candidate-illustrated/banenkaart-hero-strip-desktop.jpg` / `banenkaart-hero-strip-mobile.jpg` (+webp/-2x) | vervangt `banenkaart-hero-strip-desktop*` + `banenkaart-hero-strip-mobile*` |
| 20 | Bedrijvenkaart | `strips/bedrijvenkaart-desk.jpg` | `strips/bedrijvenkaart-mob.jpg` | `/bedrijvenkaart` | `Components/Pages/Bedrijvenkaart.razor` → `Components/Bedrijvenkaart/CompanyMapDiscovery.razor:26-31,48-53` | desk: `candidate-illustrated/bedrijvenkaart-hero-desktop.png`; mob: `bedrijvenkaart-hero-mobile.jpg` (variant C toont de desktop-png) | vervangt `bedrijvenkaart-hero-desktop.png` + `bedrijvenkaart-hero-mobile.jpg` |
| 21 (+22 leeg) | Bewaard | `strips/bewaard-desk.jpg` | `strips/bewaard-mob.jpg` | `/candidate/liked` | `CandidateLikedSsr.razor` → `Saved/SavedSsrShells.cs:67-81` (lege staat r. 104-112) | desk: `candidate-illustrated/vervoer-profession-wide.jpg` (`BewaardBand`); mob: `mob-figures/vervoer-mob-figures.png`; lege staat: `spots/kaart.jpg` (geen header) | vervangt `vervoer-profession-wide.jpg` (alleen hier) + `mob-figures/vervoer-mob-figures.png` |
| 23 (+24 leeg) | Sollicitaties | `strips/sollicitaties-desk.jpg` | `strips/sollicitaties-mob.jpg` | `/candidate/applications` | `CandidateApplicationsSsr.razor` → `Applications/ApplicationsSsrShells.cs:47-59` (lege staat `AppendEmpty` r. 249, zonder beeld); legacy `Components/Pages/Candidate/Applications.razor:42-44` (geen `@page`) | `candidate-illustrated/sollicitaties-hero-strip-desktop.webp` / `sollicitaties-hero-strip-mobile.webp` (+-2x); legacy: `werkervaring-profession-wide.jpg` | vervangt `sollicitaties-hero-strip-desktop*` + `sollicitaties-hero-strip-mobile*` (op de tab sollicitaties) |
| 25 | Sollicitatie toevoegen | `strips/toevoegen-desk.jpg` | `strips/toevoegen-mob.jpg` | `/candidate/external/add` (knop "Sollicitatie toevoegen", `ApplicationsSsrShells.cs:208-215`) | `Components/Pages/Candidate/ExternalVacancyAdd.razor` (alleen `<header>` met h1 en lead) | – | nieuw (er is nu geen header) |
| 26 | Mijn activiteiten | `strips/activiteiten-desk.jpg` | `strips/activiteiten-mob.jpg` | `/candidate/applications?tab=activiteiten` | `Applications/ApplicationsSsrShells.cs` (`AppendActiviteitenPlaceholder`, tekst "Mijn activiteiten komt hier", `UiStringsSollicitaties.cs:60`) | gedeelde strip `sollicitaties-hero-strip-*.webp` | vervangt `sollicitaties-hero-strip-*` op deze tab (de strip moet per tab gekozen worden; de inhoud is nu alleen een placeholder) |
| 28 | Na de match: wat nu | `strips/na-de-match-desk.jpg` | `strips/na-de-match-mob.jpg` | `/candidate/applications/{id}/after-match` (standaardpaneel → `/panel-ssr`, rewrite in `Middleware/AfterMatchPickSlotRouteRewriteMiddleware.cs`) | `Components/Pages/Candidate/CandidateAfterMatchPanelSsr.razor` → `Applications/AfterMatchOutcomeSsrShells.cs:37-42`, `AfterMatchConfirmedSsrShells.cs` | `sollicitaties-hero-strip-desktop.webp` / `sollicitaties-hero-strip-mobile.webp` | vervangt `sollicitaties-hero-strip-*` in de after-match-panelen |
| 29 | Moment kiezen | `strips/moment-desk.jpg` | `strips/moment-mob.jpg` | `…/after-match?panel=pick-slot` (→ `/pick-slot-ssr`), ook `?panel=reschedule` (→ `/reschedule-ssr`) | `Applications/AfterMatchPickSlotSsrShells.cs`, `AfterMatchRescheduleSsrShells.cs` | `sollicitaties-hero-strip-*.webp` | vervangt `sollicitaties-hero-strip-*` (pick-slot/reschedule) |
| 30 | Berichten | `strips/berichten-desk.jpg` | `strips/berichten-mob.jpg` | `…/after-match?panel=messages` (→ `/messages-ssr`) en `/candidate/applications?tab=berichten` | `Applications/AfterMatchMessagesSsrShells.cs`; tab: `ApplicationsSsrShells.cs` / `SollicitatiesBerichtenSsr` | `sollicitaties-hero-strip-*.webp` | vervangt `sollicitaties-hero-strip-*` (berichten) |
| 31 | Nu niet (afwijzing) | `strips/nu-niet-desk.jpg` | `strips/nu-niet-mob.jpg` | `…/after-match?panel=outcome` (uitkomst "nu niet") | `Applications/AfterMatchOutcomeSsrShells.cs:37-42` (ook `AfterMatchSsrErrorShells.cs`) | `sollicitaties-hero-strip-*.webp` | vervangt `sollicitaties-hero-strip-*` (uitkomst afgewezen; per uitkomst kiezen tussen rij 28 en 31) |
| 32 | Carrière | `strips/carriere-desk.jpg` | `strips/carriere-mob.jpg` | `/carriere` (tab `voor-jou`) | `CandidateCareerSsr.razor` → `Career/CareerSsrShells.cs:56-71` | `candidate-illustrated/droombaan-profession-wide.jpg` (desk + mob) | vervangt `droombaan-profession-wide.jpg` |
| 33 | Omscholen | `strips/omscholen-desk.jpg` | `strips/omscholen-mob.jpg` | `/carriere?tab=omscholen` | `Career/CareerSsrShells.cs` (`CareerSsrTabs.Omscholen`); verwant: `/carriere/leren` → `CareerLearnGrow.razor:26` | dezelfde `droombaan-profession-wide.jpg`; `/carriere/leren`: `leren-groeien-hero*` (variant C: `leren-groeien-hero-band-c.jpg`) | vervangt `droombaan-profession-wide.jpg` op deze tab (de strip moet per tab gekozen worden; eventueel ook `leren-groeien-hero*` op `/carriere/leren`) |
| 34 | Wat drijft jou | `strips/wat-drijft-desk.jpg` | `strips/wat-drijft-mob.jpg` | `/candidate/wat-drijft-jou` | `Components/Pages/Candidate/WatDrijftJou.razor:22-28` (`CandidateIllustratedHero`) | ingesteld `kompas-hero-wide.jpg`, getoond (variant C) `kompas-hero-band-c.jpg`; mob `mob-figures/kompas-mob-figures.png` | vervangt `kompas-hero-band-c.jpg` / `kompas-hero-wide.jpg` + `mob-figures/kompas-mob-figures.png` (op deze pagina) |
| 35 | Ondernemerscheck | `strips/ondernemer-desk.jpg` | `strips/ondernemer-mob.jpg` | `/candidate/ondernemerscheck` | `Components/Pages/Candidate/OndernemersCheck.razor:33-40` (intro-figuur), `:166` (resultaat) | intro: `candidate-illustrated/kennismaken.jpg` (`OndernemerIntroHero`); resultaat: `ondernemer-result-vignette.png` | vervangt `kennismaken.jpg` als intro-illustratie (nu een figuur en geen strip; `kennismaken.jpg` wordt ook bij rij 02 gebruikt, dus alleen de verwijzing omzetten) |
| 36 | Adviseur | `strips/adviseur-desk.jpg` | `strips/adviseur-mob.jpg` | `/candidate/adviseur-werk` | `Components/Pages/Candidate/AdviseurWerk.razor` → `Components/UwvMockups/UwvAdviseurSharePanel.razor` | – | nieuw (er is nu geen header) |
| 37 | Profiel | `strips/profiel-desk.jpg` | `strips/profiel-mob.jpg` | `/candidate/profile` (stuurt door naar `/candidate/paspoort` als `CandidatePassportEnabled` aan staat) | `Components/Pages/Candidate/Profile.razor:27` → `Components/Candidate/CandidateKompasGreetingBand.razor:8` | ingesteld `kompas-hero-wide.jpg`, getoond (variant C) `kompas-hero-band-c.jpg` | vervangt `kompas-hero-band-c.jpg` / `kompas-hero-wide.jpg` in `CandidateKompasGreetingBand` |
| 38 | Instellingen | `strips/instellingen-desk.jpg` | `strips/instellingen-mob.jpg` | `/candidate/settings` | `Components/Pages/Candidate/CandidateSettings.razor:16-19` | `candidate-illustrated/beschikbaarheid-profession-wide.jpg` (`InstellingenBand`, variant C hetzelfde); mob `mob-figures/beschikbaarheid-mob-figures.png` | vervangt `beschikbaarheid-profession-wide.jpg` + `mob-figures/beschikbaarheid-mob-figures.png` |

### Schermen zonder strip (ter info)
| nr | scherm | nieuw beeld (mockup) | route | huidig bestand | huidige asset | actie |
|---|---|---|---|---|---|---|
| 27 | UWV-overzicht (pdf) | geen | (pdf-export) | – | – | geen header nodig |
| 41 | 404 | `illustraties/fout-404.jpg` + `kreeft-kaart.webp` | `/status/404` (+ niet-gevonden) | `Components/Pages/Status/StatusPage.razor:116`, `Components/Errors/NotFoundView.razor:32` | `<LobsyMascot Pose="Waving">` | vervangt `LobsyMascot` |
| 42 | Er ging iets mis | `illustraties/fout-algemeen.jpg` + `kreeft-wachten.webp` | `/Error` | `Components/Pages/Error.razor:40` | `<LobsyMascot Pose="Sitting">` | vervangt `LobsyMascot` |

## Oude mascotte en oud logo

**Oude paarse mascotte**
- Bestanden: `wwwroot/images/brand/mascot-64.webp`, `mascot-128.webp`, `mascot-256.webp`, `mascot-64.png`, `mascot-128.png`, `mascot-coach.webp`, `mascot-coach@2x.webp`. De map `brand/mascot/` is leeg.
- Code:
  - `Media/BrandImages.cs:40-46` (`MascotWebp64/128/256`, `MascotPng128`, `MascotSrcSet56`)
  - `Components/Brand/LobsyLogo.razor:26-31`: alleen bij `UseMascot=true`, en geen enkele aanroeper zet dat
  - `Components/App.razor:278-283`: JS-fallback
  - `mascot-coach*` wordt nergens gebruikt
- Advies: verwijzingen omzetten naar het Lobsy-merkteken (`BrandImages.MarkSvg` / `Webp128`) en daarna de bestanden opruimen.

**Oud paars pin-logo**
- `wwwroot/images/brand/lobsy.png` en `lobsy-email.png`: geen codeverwijzingen gevonden.
- `Jobsy.Infrastructure/Assets/lobsy.png` is embedded (`Jobsy.Infrastructure.csproj:26`) en wordt gebruikt in:
  - `Jobsy.Infrastructure/Scholen/Passport/PupilPassportAssets.cs:22` (leerlingpaspoort-pdf)
  - `Jobsy.Infrastructure/Services/PlatformCompanySettingsService.cs:158` (standaardlogo)
- Advies: vervangen door het merkteken.

**`<LobsyMascot>`-component**
- Bestanden: `Components/Brand/LobsyMascot.razor` en `Media/MascotAssets.cs`.
- Het component toont al het nieuwe merkteken (`BrandImages.Webp128`), met CSS-wave/bounce in `public-theme.css:450-480`. Inhoudelijk is het dus de oude "zwaaiende mascotte"-rol.
- Gebruikt in:

| Bestand | Regel(s) |
|---|---|
| `GratisDnaResultView.razor` | 15 |
| `LandingFaq.razor` | 16 |
| `LandingHeroPassport.razor` | 16, 45 |
| `LandingHeroMapDisc.razor` | 34 |
| `LandingKreeft.razor` | 18 |
| `LandingMiniScene.razor` | 8 |
| `LandingWhatYouGet.razor` | 85 |
| `LandingClosing.razor` | 10 |
| `NotFoundView.razor` | 32 |
| `ClosedVacancyView.razor` | 60 |
| `AccessDeniedView.razor` | 70 |
| `ForgotPassword.razor` | 32 |
| `StatusPage.razor` | 58, 86, 116 |
| `Error.razor` | 40 |
| `GratisDna.razor` | 126, 202 |
| `PartnerSales.razor` | 39 |

- Kandidaat zijn voor kreeft-illustraties (404/fout zie rij 41/42) of voor het merkteken.

**Al nieuw merkteken, niets doen**
- `brand/lobsy-64/128/256.webp/png`, `lobsy-180.png`
- `brand/svg/*` (`lobsy-merkteken.svg`, `lobsy-app-icoon.svg`, `lobsy-monochroom-wit.svg`)
- `images/email/lobsy-mark-72/128.png` (`EmailBrand.cs:57-58`)
- `Jobsy.Infrastructure/Assets/Brand/mascot-128.png` (ondanks de naam)

**Label "Vraag het Lobsy" → "Lobsy-hulp"**
- `Components/LobsyCoach.razor:42` en `Components/LobsyCoachSsr.razor:44` (standaardlabels; avatar `BrandImages.MarkSvg`)
- `Localization/UiStringsTests.cs:55` (`Coach.Ask`)
- `Localization/UiStrings.cs:429` (`Assistant.SubtitleWarm`)
- `Localization/UiStringsAfterMatch.cs:332` (in een zin)
- Geplaatst via `Components/Layout/MainLayout.razor:74-84`:
  - `LobsyCoach`, `LobsyCoachSsr` en `LobsyAssistantChat`
  - de launcher gebruikt `ChatSilhouetteSvg`

## Telling
| actie | aantal strips | rijen |
|---|---|---|
| vervangt | 30 | 01–11, 13–16, 19–21, 23, 26, 28–35, 37, 38 |
| nieuw (er is nu geen header) | 3 | 18 (+39/40), 25, 36 |
| pagina bestaat nog niet in de app | 2 | 12, 17 |
| **totaal** | **35** | |

Daarnaast: 41/42 vervangen `LobsyMascot` door kreeft-illustraties, en 27 heeft geen strip.
Let op: rij 26 en 28–31 delen nu allemaal één strip (`sollicitaties-hero-strip-*`), en rij 32/33 delen `droombaan-profession-wide.jpg`. Voor de nieuwe strips is dus per tab/paneel een eigen asset-keuze nodig.
