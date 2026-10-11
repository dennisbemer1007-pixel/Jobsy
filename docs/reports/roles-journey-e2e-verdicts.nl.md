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

Volledige journey: PR **#627** / `CandidateFullJourneyPlaywrightTests`.

## Niet in ROLES_AND_VIEWS / geen demo-seed

| Rol (user-vraag) | Status |
|------------------|--------|
| Werkcoach / coach | **probleem over** — geen aparte rol in matrix; geen `@jobsy.local` seed |
| Leraar / schoolbeheer | **probleem over** — routes `/leraar`, `/school`; geen lokale demo-login in `DemoUsersSeeder` |
| Gemeente / partner (ingelogd) | **probleem over** — publiek `/partner` getest als gast |

---

## Gemaakte keuzes

1. **Banenkaart voor gast** op `/banenkaart` i.p.v. `/` (marketing-landing blijft op `/`).
2. **Ambassadeur-login** vereist `AmbassadorsEnabled=true`; E2E zet dit mee aan.
3. **Stylesheet-wacht**: `Attached` i.p.v. `Visible` (voorkomt timeout op kaartpagina’s).
4. **Geen volledige vacature-submit** in Playwright — formulier openen + screenshot.
5. **School/leraar/coach** buiten scope tot demo-accounts bestaan.
