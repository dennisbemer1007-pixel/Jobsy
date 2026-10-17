# Ontdekkingsreis — vraagscherm mockups

Statische HTML/CSS mockups voor het shed-vraagscherm (stap 8 · Leuk), ter review vóór implementatie.

| Bestand | Beschrijving |
|---------|--------------|
| `variant-a.html` | Klassieke drie-koloms compositie: volledige rail links, kaart midden, kreeft rechts naast de kaart. Donkere diepte-gradient. Segmenten als pill-tabs. |
| `variant-b.html` | Compacte rail (gegroepeerde stappen), warmere gradient, onderstreping-tabs, kreeft als rustige tipstrip in de kaart. |
| `_shared.css` | Lobsy-tokens, layout, Likert, mobiele voortgangsbalk. |

Assets: `Jobsy.Web/wwwroot/images/brand/` (logo, mascot).

## Screenshots

```bash
cd tools/mockups/ontdekkingsreis-vraag
npm init -y >/dev/null 2>&1
npm install playwright@1.49.1
npx playwright install chromium
node capture-screenshots.mjs
```

Output: `variant-a-desktop-1280x800.png`, `variant-a-mobile-390x844.png`, en hetzelfde voor `variant-b`.
