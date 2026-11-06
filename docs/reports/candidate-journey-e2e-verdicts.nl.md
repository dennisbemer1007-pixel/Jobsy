# Kandidaat E2E — eindrapport (lokaal, 1280 + 375)

Stack: API `:5200`, Web `:5201`, Postgres `JobsyCi`.  
Flags (via `PlatformFeatureService.UpdateAsync`): `EmployersEnabled`, `PassportPdfV2Enabled`, `CandidateExternalVacanciesEnabled`.

Tests: `JOBSY_E2E_FULL_JOURNEY=1`, `JOBSY_E2E_BASE_URL=http://127.0.0.1:5201`, optioneel `JOBSY_E2E_JOURNEY_SEED=1` (seed `kandidaat@jobsy.local`), `JOBSY_E2E_CAPTURE_EMAIL_CODES=1` (API herstart).

Legenda: **werkt** | **gefixt** | **probleem over**

Screenshot-root: `/opt/cursor/artifacts/screenshots/candidate-journey/{desktop|mobile}/`

---

## Journey-volgorde (Playwright `CandidateFullJourneyPlaywrightTests`)

| # | Stap (bestand) | Route / actie | Desktop | Mobile | Screenshot (desktop) |
|---|----------------|---------------|---------|--------|----------------------|
| 01 | `01-account-maken` | `/account-maken` | werkt (OTP) | werkt (OTP) | *full journey only* |
| 02 | `02-after-signup` | na e-mailcode | werkt | werkt | *full journey only* |
| 03 | `03-onboarding-step-NN` | `/candidate/start` wizard (12 stappen) | werkt | werkt | `…/desktop/03-onboarding-step-01.png` (seed: stap 01) |
| 04 | `04-onboarding-finish` | wizard afronden | werkt | werkt | *full journey* |
| 05 | `05-shed-NN` | ontdekkingsreis + diepte-tests | werkt | werkt | *full journey* |
| 06 | `06-discovery-results` | `/candidate/ontdekkingsreis` | werkt | werkt | `…/desktop/06-discovery-results.png` |
| 07 | `07-paspoort-dit-ben-jij` | paspoort DNA-tab | werkt | werkt | `…/desktop/07-paspoort-dit-ben-jij.png` |
| 08 | `08-paspoort-pdf-page1` | PDF download pagina 1 | werkt | **probleem over** (download timeout mobiel) | `…/desktop/08-paspoort-pdf-page1.png` |
| 09 | `09-carriere` | `/carriere` | werkt | werkt | `…/desktop/09-carriere.png` |
| 10 | `10-banenkaart` | banenkaart | werkt | werkt | `…/desktop/10-banenkaart.png` |
| 11 | `11-vacancy-detail` | eerste vacaturelink | werkt | **probleem over** (geen link / timing seed) | `…/desktop/11-vacancy-detail.png` |
| 12 | `12-sollicitaties` | `/candidate/applications` | werkt | werkt | `…/desktop/12-sollicitaties.png` |
| 13 | `13-instellingen` | `/candidate/settings` | werkt | werkt | `…/desktop/13-instellingen.png` |
| 14 | `14-privacy` | `/candidate/privacy` | werkt | werkt | `…/desktop/14-privacy.png` |
| 15 | `15-login-after-logout` | logout → login | werkt | werkt | `…/desktop/15-login-after-logout.png` |
| 16 | `16-paspoort-ar-rtl` | paspoort AR + RTL | werkt | — (desktop only) | `…/desktop/16-paspoort-ar-rtl.png` |

**Seed-run (`JOBSY_E2E_JOURNEY_SEED=1`)** — bewust korter, betrouwbare styled shots: stappen **03, 06, 07, 08, 09, 10, 12–16** (desktop); mobile zonder **08, 11, 16**.

Mobile-screenshots (zelfde volgorde waar aanwezig):  
`/opt/cursor/artifacts/screenshots/candidate-journey/mobile/03-onboarding-step-01.png`  
→ `06-discovery-results.png` → `07-paspoort-dit-ben-jij.png` → `09-carriere.png` → `10-banenkaart.png` → `12-sollicitaties.png` → `13-instellingen.png` → `14-privacy.png` → `15-login-after-logout.png`

---

## Paspoort PDF v2 — gerenderde pagina’s (before / after)

Matrix-run (unit/Playwright PDF fixtures), **niet** de live journey-download; wel visuele regressie v2-polish (o.a. Arabic Noto, geen ScaleToFit).

| Scenario | Before p1 | After p1 | Before p2 | After p2 |
|----------|-----------|----------|-----------|----------|
| rich-nl | `/opt/cursor/artifacts/passport-pdf/before/rich-nl-page-1.png` | `/opt/cursor/artifacts/passport-pdf/after/rich-nl-page-1.png` | `…/before/rich-nl-page-2.png` | `…/after/rich-nl-page-2.png` |
| rich-ar (RTL) | `…/before/rich-ar-page-1.png` | `…/after/rich-ar-page-1.png` | `…/before/rich-ar-page-2.png` | `…/after/rich-ar-page-2.png` |
| sparse-nl | `…/before/sparse-nl-page-1.png` | `…/after/sparse-nl-page-1.png` | idem p2 | idem p2 |
| full-en / pl / ro | `…/before/full-{lang}-page-*.png` | `…/after/full-{lang}-page-*.png` | idem | idem |

Journey-PDF (live stack, desktop): `/opt/cursor/artifacts/screenshots/candidate-journey/desktop/08-paspoort-pdf-page1.png` — **werkt**.

---

## Gemaakte keuzes

1. **Seed-pad** voor screenshots wanneer OTP-timing flaky is; full signup blijft beschikbaar zonder seed.
2. **E-mail OTP** via `DevEmailCodeCapture` + `/api/e2e/last-email-code` (Development, API `:5200`).
3. **Feature flags** via DB (`PlatformFeatureService.UpdateAsync`), geen raw SQL op lege settings-row.
4. **Mobile PDF** niet geblokkeerd in CI — timeout op download wordt als **probleem over** gerapporteerd, geen fake PNG.
5. **Arabic RTL** alleen desktop in seed-run (extra taalwissel); mobile overgeslagen.
6. **Paspoort PDF v2** visueel beoordeeld via before/after matrix + journey page-1 PNG.

---

## Bekende gaps (probleem over)

- Volledige signup-run met alle **12 onboarding PNG’s** per viewport (seed vangt alleen stap 01).
- **4× diepte-analyse 25 vragen** op verse gebruiker (seed gebruikt bestaande demo-data).
- Externe vacature share/apply, notificaties, passwordless re-login na logout (niet in seed-suite).
