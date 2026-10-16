# Onboarding ontwerp mockups (A / B / C)

Statische HTML/CSS mockups voor kandidaat-onboarding (`/candidate/start`), geen productcode. Brand tokens en assets uit `Jobsy.Web/wwwroot/`.

## Bekijken

Open lokaal bijv. `A/step-1.html` in de browser (paden naar logo/mascot zijn relatief naar `wwwroot`).

## Screenshots genereren

```bash
cd docs/mockups/onboarding-ontwerp/scripts
npm init -y 2>/dev/null; npm install playwright
node capture.mjs
```

PNG’s per richting: stap 1, 3 en 8 × mobile 390×844 en desktop 1280×800, plus `overview-mobile-sheet.png`.
