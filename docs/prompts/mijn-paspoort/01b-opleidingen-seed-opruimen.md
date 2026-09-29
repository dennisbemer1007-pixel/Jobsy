# 01b · Stop seeding fake course providers in production

> Read `00-README.md` first: §0 rules apply, plus nothing else. Execute this file only after the previous file's PR is open and green.

| | |
|---|---|
| Branch | `cursor/opleidingen-seed-opruimen`, created from `cursor/werkgevers-actief` |
| PR | ONE PR into `acceptatie`, title `fix(training): no demo course providers outside dev/test`. Body starts with `Stacked on #<PR of 01> (cursor/werkgevers-actief)` |
| Mockups | none |

## Why
`TrainingUpskillService.EnsureDefaultsAsync()` runs on **every** `RecommendAsync` / `TrackAsync` / list call. On an empty table it seeds six providers from `Seeds()` + `SkillsAcademy()`, and it re-adds missing workshop offers (`EnsureCompetencyWorkshopsAsync`, `LoiSkillOffer`, `NtiSkillOffer`) and deep links (`EnsureOfferDeepLinksAsync`), in **every environment**, production included. Those providers are demo data:
- "LOI" and "NTI": real schools, but no partnership exists.
- "Zorgcollege Haaglanden", "Praktijkacademie Haaglanden" (base URL `rocmondriaan.nl` with invented workshop paths), "Techniek College Westland", "Logistiek Academy Den Haag" (base URL `s-bb.nl`).

Today, Functiefit's `TrainingOffersBlock` shows them to candidates. Dennis's rule: never show fake providers in production.

## Change (small)
1. **Seed only in Development and Test.**
   - `EnsureDefaultsAsync` returns immediately unless `_environment.IsDevelopment()`, or the environment name is `Test`/`Testing` (check how tests set it; `TrainingUpskillTests` and `RoleFitCheckTests` must keep their seeds), or a config switch `Training:SeedDemoProviders=true` is set.
   - Default: false in `appsettings.json`, true in `appsettings.Development.json`.
   - Move the seed methods into `TrainingDemoSeed` (same file or `Data/`) so it's obvious they're demo data.
2. **Deactivate the demo rows already in acceptatie/production.** Add an EF migration `DeactivateDemoTrainingProviders` that runs `UPDATE "TrainingProviders" SET "IsActive" = false WHERE "Id" IN (…)` for the 6 fixed seed ids `a11a0001-0001-4000-8000-00000000000{1..6}`.
   - **No deletes.** Click logs and FK rows stay.
   - The migration's `Down` sets them back to `true`.
   - Providers an admin created (other ids) aren't touched.
3. **Empty states.** With no active offers, Functiefit's `TrainingOffersBlock` and the career course hints render **nothing**: no empty card, no heading. The tracked-redirect endpoint returns 404 for an inactive offer, as it already does for unknown ids. Check it.
4. `TrainingAdmin.razor` still lists the deactivated providers, so an admin can re-activate one after a real agreement.

## Tests
- `EnsureDefaultsAsync` in the Production environment (and with the config switch off) adds nothing to an empty DB. In Development, or with the switch on, it seeds as before.
- Migration: seed ids → `IsActive = false`; a non-seed provider is unchanged; `Down` restores `true`.
- `RecommendAsync` with only inactive providers returns empty. bUnit: `TrainingOffersBlock` with an empty list renders nothing.
- Existing `TrainingUpskillTests` / `RoleFitCheckTests` stay green (they run with the seeds on).

## Success criteria (all must hold before you open the PR)
- In Production and Acceptatie, no demo provider is active or seeded again, even after a restart and a Functiefit check.
- Development and tests behave as before.
- No rows are deleted.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has: stacked-on line, what and why, screenshots (where there is UI), test list, "Out of scope / deferred".

## Done → next
Push, open the PR, note its number. Then continue with **`02-paspoort-overzicht-mijn-dna.md`**. If anything above is red, stop and report (see 00-README "How to run" step 3).
