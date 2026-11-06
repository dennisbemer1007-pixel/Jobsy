# Rollen E2E — verdicts (lokaal, 1280 + 375)

Stack: API `:5200`, Web `:5201`, `JobsyCi`, flags via `PlatformFeatureService.UpdateAsync`:
`EmployersEnabled`, `PassportPdfV2Enabled`, `CandidateExternalVacanciesEnabled`, **`AmbassadorsEnabled`**, `SchoolsEnabled`.

Test: `JOBSY_E2E_ROLES_JOURNEY=1` → `RolesFullJourneyPlaywrightTests`.

Screenshots: `/opt/cursor/artifacts/screenshots/roles-journey/{rol}/{desktop|mobile}/{stap}.png`

Legenda: **werkt** | **gefixt** | **probleem over**

---

## Gast (anoniem)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-banenkaart | `/banenkaart` | werkt | werkt |
| 02-registreren | `/register` | werkt | werkt |
| 03-inloggen | `/login` | werkt | werkt |
| 04-partner | `/partner` | werkt | werkt |
| 05-hoe-werkt-lobsy | `/hoe-werkt-lobsy` | werkt | werkt |
| 06-privacy | `/privacy` | werkt | werkt |

## Filiaalmanager (`ondernemer@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-home | `/home` | werkt | werkt |
| 02-vacatures | `/employer/vacancies` | werkt | werkt |
| 03-sollicitaties | `/branch/applicants` | werkt | werkt |
| 04-nieuwe-vacature | `/branch/vacancies/new` | werkt | werkt |
| 05-talentpool | `/employer/talent` | werkt | werkt |
| 06-tokens | `/branch/tokens` | werkt | werkt |
| 07-overnames | `/employer/takeovers` | werkt | werkt |
| 08-bottomnav-vacatures | bottom nav | — | werkt |

## Regiomanager (`regio@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-home | `/home` | werkt | werkt |
| 02-vacatures | `/employer/vacancies` | werkt | werkt |
| 03-vestigingen | `/regional/branches` | werkt | werkt |
| 04-tokens | `/employer/tokens` | werkt | werkt |

## Bedrijfsmanager (`enterprise@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-home | `/home` | werkt | werkt |
| 02-vacatures | `/employer/vacancies` | werkt | werkt |
| 03-sollicitaties | `/branch/applicants` | werkt | werkt |
| 04-tokens | `/employer/tokens` | werkt | werkt |
| 05-gebruikers | `/employer/users` | werkt | werkt |
| 06-organisatie | `/employer/organization` | werkt | werkt |

## Intermediair (`intermediair@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-home | `/home` | werkt | werkt |
| 02-bedrijvenoverzicht | `/intermediary` | werkt | werkt |
| 03-team | `/intermediary/team` | werkt | werkt |
| 04-vacatures | `/employer/vacancies` | werkt | werkt |
| 05-tokens | `/employer/tokens` | werkt | werkt |

## Salesmanager (`sales@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-home | `/home` | werkt | werkt |
| 02-dashboard | `/salesmanager` | werkt | werkt |
| 03-toolkit | `/salesmanager/toolkit` | werkt | werkt |
| 04-referrals | `/salesmanager/referrals` | werkt | werkt |
| 05-onboarding | `/salesmanager/onboarding` | werkt | werkt |
| 06-facturen | `/salesmanager/invoices` | werkt | werkt |

## Ambassadeur (`ambassadeur@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-home | `/home` | werkt | werkt |
| 02-dashboard | `/ambassadeur` | werkt | werkt |
| 03-toolkit | `/ambassadeur/toolkit` | werkt | werkt |
| 04-financieel | `/ambassadeur/finance` | werkt | werkt |
| 05-onboarding | `/ambassadeur/onboarding` | werkt | werkt |

*Login faalde in eerste runs: **`AmbassadorsEnabled` stond uit** → geactiveerd in E2E seed (gefixt).*

## Admin (`admin@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-home | `/home` | werkt | werkt |
| 02-organisaties | `/admin/organisaties` | werkt | werkt |
| 03-vacatures | `/admin/vacatures` | werkt | werkt |
| 04-gebruikers | `/admin/gebruikers` | werkt | werkt |
| 05-financieel | `/admin/financien` | werkt | werkt |
| 06-instellingen | `/admin/instellingen` | werkt | werkt |
| 07-logging | `/admin/beveiliging/systeemlogs` | werkt | werkt |

## Kandidaat

Volledige journey: **`docs/reports/candidate-journey-e2e-verdicts.nl.md`** / PR **#627** / `CandidateFullJourneyPlaywrightTests`.

## Schoolbeheerder (`schoolbeheer@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-dashboard | `/school` | werkt | werkt |
| 02-klassen | `/school/klassen` | werkt | werkt |
| 03-te-doen | `/school/te-doen` | werkt | werkt |
| 04-resultaten | `/school/resultaten` | werkt | werkt |
| 05-leraren | `/school/leraren` | werkt | werkt |

## Leraar (`leraar@jobsy.local`)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-dashboard | `/leraar` | werkt | werkt |
| 02-klas-overzicht | `/leraar/klas/{demo-klas}` | werkt | werkt |
| 03-codes | `…/codes` | werkt | werkt |
| 04-groep | `…/groep` | werkt | werkt |

## Werkcoach (`werkcoach@jobsy.local`, Teacher)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-dashboard | `/leraar` | werkt | werkt |
| 02-klas-overzicht | `/leraar/klas/{demo-klas}` | werkt | werkt |
| 03-droombanen | `…/droombanen` | werkt | werkt |
| 04-materiaal | `…/materiaal` | werkt | werkt |

## Gemeente (`gemeente@jobsy.local` + publiek)

| Stap | Pad | Desktop | Mobile |
|------|-----|---------|--------|
| 01-westland-teaser | `/westland` (anoniem) | werkt | werkt |
| 02-home | `/home` | werkt | werkt |
| 03-vacatures | `/employer/vacancies` | werkt | werkt |
| 04-vestigingen | `/regional/branches` | werkt | werkt |

*Demo-seed (Development): `DemoUsersSeeder` + `appsettings.Development.json` → `Authentication:DemoUsers` (`Jobsy123!`). Klas-id: `DemoSchoolIds.Class1AId`.*

---

## Gemaakte keuzes

1. **Banenkaart voor gast** op `/banenkaart` i.p.v. `/` (marketing-landing blijft op `/`).
2. **Ambassadeur-login** vereist `AmbassadorsEnabled=true`; E2E zet dit mee aan.
3. **Stylesheet-wacht**: `Attached` i.p.v. `Visible` (voorkomt timeout op kaartpagina’s).
4. **Geen volledige vacature-submit** in Playwright — formulier openen + screenshot.
5. **School/gemeente demo** — unieke KVK + verplichte `Company.Location` voor gemeente-demo-bedrijf (gefixt seed-fout).
6. **Werkcoach** = `Teacher` met eigen login; geen aparte `UserRole`.

---

## Screenshot-paden (journey-volgorde)

Prefix: `/opt/cursor/artifacts/screenshots/roles-journey/`

### Gast
- Desktop: `guest/desktop/01-banenkaart.png` → `02-registreren.png` → `03-inloggen.png` → `04-partner.png` → `05-hoe-werkt-lobsy.png` → `06-privacy.png`
- Mobile: zelfde nummering onder `guest/mobile/`

### Filiaalmanager
- Desktop: `branch-manager/desktop/01-home.png` … `07-overnames.png`
- Mobile: `01-home.png` … `07-overnames.png`, plus `08-bottomnav-vacatures.png`

### Regiomanager
- Desktop/mobile: `regional-manager/{desktop|mobile}/01-home.png` → `02-vacatures.png` → `03-vestigingen.png` → `04-tokens.png`

### Bedrijfsmanager
- Desktop/mobile: `enterprise-manager/{desktop|mobile}/01-home.png` … `06-organisatie.png`

### Intermediair
- Desktop/mobile: `intermediary/{desktop|mobile}/01-home.png` … `05-tokens.png`

### Salesmanager
- Desktop/mobile: `sales-manager/{desktop|mobile}/01-home.png` … `06-facturen.png`

### Ambassadeur
- Desktop/mobile: `ambassadeur/{desktop|mobile}/01-home.png` … `05-onboarding.png`

### Admin
- Desktop/mobile: `admin/{desktop|mobile}/01-home.png` … `07-logging.png`

### Schoolbeheerder
- Desktop/mobile: `school-beheer/{desktop|mobile}/01-dashboard.png` … `05-leraren.png`

### Leraar
- Desktop/mobile: `leraar/{desktop|mobile}/01-dashboard.png` … `04-groep.png`

### Werkcoach
- Desktop/mobile: `werkcoach/{desktop|mobile}/01-dashboard.png` … `04-materiaal.png`

### Gemeente
- Desktop/mobile: `gemeente/{desktop|mobile}/01-westland-teaser.png` → `04-vestigingen.png`

**Totaal:** 125 PNG’s (1280×900 desktop, 375×812 mobile) — 12 rollen × desktop + mobile.
