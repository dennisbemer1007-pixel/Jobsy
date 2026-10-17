# Vroeg resultaat — onboarding mockups

Drie varianten voor een **eerste beloning** vóór het volledige Kompas (nu pas aan het einde: “Je Kompas staat klaar”).

| Variant | Moment | Bestand |
|--------|--------|---------|
| **A** | Na stap 3 (locatie, beschikbaarheid, vervoer) | `A/early-reward.html` |
| **B** | Na eerste mini-test (5 vragen) | `B/early-reward.html` + `B/desktop-1440.html` |
| **C** | Kompas in header + micro-moment na test 1 | `C/early-reward.html` |

Visuele basis: **richting A — verfijnde kaarten** (`docs/mockups/onboarding-ontwerp/A`, `onboarding-wizard-richting-a.css`).

## PNG’s genereren

```bash
cd docs/mockups/vroeg-resultaat/scripts
npm install playwright  # eenmalig, of vanuit repo-root als playwright al aanwezig is
node capture.mjs
```

Output per variant: `mobile-390x844.png`, `mobile-390x844@2x.png`. Variant B ook `desktop-1440x900.png`. Root: `overview-mobile-variants.png`.

Kopieën staan in de Agent Store: `/cursor/stores/self/vroeg-resultaat/`.

## Juridisch / product (besluit 21)

Het **paspoort-PDF** mag geen testuitkomsten of AI-gegenereerde tekst tonen. **In-app teasers** (zoals deze mockups) zijn wél toegestaan — korte, niet-bindende eerste indrukken vóór het volledige Kompas.

## Aanbeveling desktop

Variant **B** is gekozen voor desktop (1440×900): persoonlijk inzicht direct na de eerste test sluit het best aan op de huidige “Eerste indruk”-finish en de psychometrische flow.
