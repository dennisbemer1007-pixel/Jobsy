# GitHub branch-bescherming (handmatig)

De JSON in [`.github/rulesets/`](../.github/rulesets/) is het **gewenste** beleid.
GitHub past rulesets **niet** automatisch toe vanuit de repo — jij moet ze eenmalig aanmaken.

## Waarom handmatig?

Rulesets vereisen repo-adminrechten. Agents kunnen ze niet betrouwbaar via de API zetten zonder speciale tokens. Gebruik onderstaande klikpaden (of plak de JSON via de API als je dat prettiger vindt).

## `main` (productie) — verplicht

Doel: geen directe pushes; alleen PR; 1 goedkeuring (jij); statuscheck `test` groen.

1. GitHub → repo **Jobsy** → **Settings** → **Rules** → **Rulesets** → **New ruleset** → **New branch ruleset**
2. **Ruleset name:** `Protect main (production)`
3. **Enforcement status:** Active
4. **Target branches** → **Include by pattern** → `main`
5. Rules aanvinken:
   - **Restrict deletions**
   - **Block force pushes**
   - **Require a pull request before merging**
     - Required approvals: **1**
     - Dismiss stale pull request approvals when new commits are pushed: **aan**
   - **Require status checks to pass**
     - Require branches to be up to date: **aan**
     - Status checks die moeten slagen: **`test`**  
       (job-naam uit `.github/workflows/pr-tests.yml` — verschijnt pas na de eerste workflow-run)
6. **Bypass list:** leeg laten (of alleen jij, tijdelijk)
7. **Create**

Referentie-JSON: [`.github/rulesets/main.json`](../.github/rulesets/main.json)

## `acceptatie` — verplicht

Doel: alleen via PR; statuscheck `test` groen. (Goedkeuring optioneel — jij mag zelf mergen na groene CI.)

1. **New branch ruleset**
2. **Ruleset name:** `Protect acceptatie`
3. **Enforcement:** Active
4. **Target branches** → Include → `acceptatie`
5. Rules:
   - Restrict deletions
   - Block force pushes
   - Require a pull request before merging (approvals: **0**)
   - Require status checks → **`test`**, up to date: **aan**
6. **Create**

Referentie-JSON: [`.github/rulesets/acceptatie.json`](../.github/rulesets/acceptatie.json)

## Controleren

- Probeer direct te pushen naar `main` → moet geweigerd worden.
- Open een PR naar `acceptatie` zonder groene `test` → merge-knop geblokkeerd.
- PR `acceptatie` → `main` vraagt om **jouw** review + groene checks.

## API (optioneel)

Als je `gh` + admin-token hebt:

```bash
gh api --method POST repos/{owner}/{repo}/rulesets --input .github/rulesets/main.json
gh api --method POST repos/{owner}/{repo}/rulesets --input .github/rulesets/acceptatie.json
```

(Pas velden aan als de GitHub API-versie extra verplichte keys eist.)
