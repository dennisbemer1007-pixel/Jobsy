# Mockup: coach-voorkeuren (SCARF intern)

Alleen ontwerp en statische mockups — geen productiecode.

## Documentatie

- [ONTWERPPLAN.md](./ONTWERPPLAN.md) — vragen, datamodel, coach, privacy, feature flag, testen, bouw-inschatting.

## Screenshots ( gegenereerd )

Run vanuit `tools/screenshots`:

```bash
cd tools/screenshots && npm install && npx playwright install chromium
node capture-scarf-coach-mockup.mjs
```

Output: `/opt/cursor/artifacts/scarf-coach-mockup/`

| Bestand | Inhoud |
|---------|--------|
| `01-vragenstap-paspoort-gegevens-1440.png` | Gegevens-tab met bestaande velden + nieuwe vragen |
| `01-vragenstap-paspoort-gegevens-390.png` | Zelfde, mobiel 390px |
| `02-kreeft-bericht-zekerheid-1440.png` | Kreeft-panel: meer stappen (zekerheid) |
| `02-kreeft-bericht-zekerheid-390.png` | Mobiel |
| `03-kreeft-bericht-autonomie-1440.png` | Kreeft-panel: meer keuzes (autonomie) |
| `03-kreeft-bericht-autonomie-390.png` | Mobiel |

HTML-previews (zelfde build): `previews/*.html`
