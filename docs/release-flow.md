# Release-flow Lobsy (Acceptatie eerst)

Niets gaat live op **lobsy.nl** zonder test op Acceptatie en jouw expliciete goedkeuring.

## Kort pad

```
feature-branch  →  PR naar acceptatie  →  test op acceptatie.lobsy.nl
        →  PR acceptatie → main  →  live (lobsy.nl)
```

| Stap | Wat | Waar |
|------|-----|------|
| 1 | Werk op `cursor/…` (of andere feature-branch) | GitHub |
| 2 | Open een PR **naar `acceptatie`** (niet naar `main`) | GitHub |
| 3 | CI (build + tests + Playwright smoke) moet groen zijn | GitHub Actions |
| 4 | Merge naar `acceptatie` → Render deployt Acc automatisch | acceptatie.lobsy.nl |
| 5 | Jij test op Acceptatie (mobiel + rollen) | Browser |
| 6 | Open een PR **`acceptatie` → `main`** (alleen jij merged) | GitHub |
| 7 | Merge naar `main` → Render deployt Production | lobsy.nl |

## Branches

| Branch | Rol | Render |
|--------|-----|--------|
| `acceptatie` | Altijd-bestaande testtak | `lobsy-acc-api` + `lobsy-acc-web` |
| `main` | Alleen goedgekeurde releases | `jobsy-api` + `jobsy-web` (lobsy.nl) |
| `cursor/…` | Feature-werk | Geen directe deploy |

Shortcut **`123`** mag **nooit** naar `main` pushen. Die opent alleen een PR naar `acceptatie` (of merged naar `acceptatie` als checks groen zijn). Promotie naar `main` doe jij zelf.

## Handmatige stappen (eenmalig)

Zie de PR-beschrijving van de safe-release-flow en `.github/rulesets/` voor:

1. **GitHub** — branch protection / rulesets op `main` en `acceptatie`
2. **Render** — per service de juiste Git-branch (`main` vs `acceptatie`)
3. **Secrets** — `JOBSY_E2E_*` voor Acc-smoke; nooit wachtwoorden in code

## Gerelateerd

- Deploy-details: [`deploy-render.md`](deploy-render.md)
- Blueprint: [`../render.yaml`](../render.yaml)
- CI: [`.github/workflows/pr-tests.yml`](../.github/workflows/pr-tests.yml)
