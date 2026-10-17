# Onboarding ontwerp mockups (A / B / C)

Statische HTML/CSS mockups voor kandidaat-onboarding (`/candidate/start`), geen productcode. Brand tokens en assets uit `Jobsy.Web/wwwroot/`.

## Bekijken

Open lokaal bijv. `A/step-1.html` in de browser. Logo en mascot staan in `shared/assets/` (kopie uit `Jobsy.Web/wwwroot/images/brand/`).

## Screenshots genereren

```bash
cd docs/mockups/onboarding-ontwerp/scripts
npm init -y 2>/dev/null; npm install playwright
node capture.mjs
```

**Alleen mobile (390×844)** — desktop blijft de huidige app; geen desktop-mockups.

PNG’s per richting: stap **1**, **1 (adresuggesties open)**, **3**, **5**, **7**, **8**, plus `overview-mobile-sheet.png`.
