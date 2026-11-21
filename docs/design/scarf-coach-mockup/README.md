# Mockup: coach-voorkeuren (SCARF intern)

Alleen ontwerp en statische mockups — geen productiecode.

## Documentatie

- [ONTWERPPLAN.md](./ONTWERPPLAN.md)

## Screenshots (in repo)

PNG’s staan in [`screenshots/`](./screenshots/) (ook via PR leesbaar).

| Bestand | Inhoud |
|---------|--------|
| `01-vragenstap-paspoort-gegevens-{1440,390}.png` | Gegevens-tab: strakke grid, volle coach-kaart, voortgang 2/7, tegels met icoon |
| `02-overzicht-na-invullen-{1440,390}.png` | Samenvatting + Aanpassen / Wissen |
| `03-kreeft-past-bij-zekerheid-{1440,390}.png` | Tab Past bij? gevuld + chat (stappen) |
| `04-kreeft-past-bij-autonomie-{1440,390}.png` | Zelfde pagina + chat (keuzes) |
| `05-kreeft-vergelijking-1440.png` | Zekerheid vs autonomie naast elkaar |

HTML-previews: [`previews/`](./previews/) (zelfde namen, `.html`).

## Opnieuw genereren

```bash
cd tools/screenshots && npm install && npx playwright install chromium
node capture-scarf-coach-mockup.mjs
```

Schrijft naar `docs/design/scarf-coach-mockup/screenshots/` en `/opt/cursor/artifacts/scarf-coach-mockup/`.
