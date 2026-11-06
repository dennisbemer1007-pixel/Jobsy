# Occupation catalogue

The candidate job list is generated. The .NET build and the tests do not download anything.

## Refresh

```bash
python3 tools/occupations/fetch.py
python3 tools/occupations/build_catalog.py
```

`fetch.py` writes `tools/occupations/.cache/` (gitignored). It downloads ESCO v1.2.1 (Dutch, 31 pages, one request per second), the ESCO–O*NET crosswalk, O*NET 31.0 `career_interest_types.csv` and `occupation_data.csv`, the CBS BRC 2014 editie 2025 workbook, ROA AIS tot 2030 (editie 2026) and the ILO Working Paper 140 score file. URLs and sha256 pins live in `sources.json`. A hash mismatch stops the fetch.

`build_catalog.py` reads that cache plus `Jobsy.Core/Data/Occupations/corrections.json` and writes:

- `Jobsy.Core/Data/Occupations/occupations.nl.json`
- `Jobsy.Core/Data/Occupations/cbs-title-index.json`
- `Jobsy.Core/Data/Occupations/onet-oi.json`
- `Jobsy.Core/Data/Occupations/manifest.json`
- `Jobsy.Core/Data/Occupations/outlook.json`
- `Jobsy.Core/Data/Occupations/occupation-skills.json`
- `Jobsy.Core/Data/Occupations/skills.nl.json`
- `tools/occupations/ilo_tasks_nl_review.csv`
- `tools/occupations/last-diff.md`

Commit those files. Do not commit `.cache/`.

```bash
python3 tools/occupations/build_catalog.py --check
```

`--check` rebuilds into a temp directory and fails when the committed JSON differs. It ignores `generatorCommit` in `manifest.json`, so the hash of the commit that contains the file is not circular. `--check` needs the cache from `fetch.py`.

## Corrections

`corrections.json` stores O*NET codes, never interest numbers. A row changes the catalogue only when `status` is `goedgekeurd`. `concept, wacht op akkoord` and `afgewezen` do nothing.

To approve a row: set `status` to `goedgekeurd`, fill `approvedBy` and `approvedOn`, run `build_catalog.py`, and commit the JSON. No C# change.

`Occupations:PreviewConceptCorrections` (default false) applies concept rows when the host is not Production. Production ignores the flag. The banner is "Voorbeeld: concept-correcties actief".

## Task translations

`ilo_tasks_nl.json` holds one Dutch line for each ILO task that the outlook can show (top two and bottom two per ISCO, ties kept apart). A line with status `concept, wacht op akkoord` is not shown. Set `status` to `goedgekeurd`, fill `reviewedBy` and `reviewedOn`, and commit. The build fails when a goedgekeurd line has an empty `nl`. `draft_ilo_nl.py` can refresh the concept drafts; it does not approve anything. The review sheet is `ilo_tasks_nl_review.csv`.

## Honest advice

`honest_advice.nl.json` stores one short Dutch advice line per ESCO occupation. The candidate app only reads that file. It does not call a model.

The block "Eerlijk advies" shows on carrière and functiefit, under "Toekomst van dit werk", when platform flag `HonestAdviceEnabled` is on (default off). No stored line means the block stays hidden. The outlook source line (ROA / ILO, with peildatum) stays under the advice.

Dutch is the source. `en`, `pl`, `ro` and `ar` are stored next to it (`translations`). The page serves the stored language. A missing or stale translation falls back to Dutch. Nothing is translated live. The translator uses the same `Ai__Provider` / OpenAI / Mistral settings as the rest of Lobsy (same call shape as vacancy translation, temperature 0.2).

A small seed is already committed so the UI can be tried without a full run. Fill the rest offline:

```bash
# hash and peildatum only, no AI
dotnet run --project tools/occupations/HonestAdviceGen -- --stamp

# one occupation, or the whole catalogue
dotnet run --project tools/occupations/HonestAdviceGen -- --ids <esco-id>
dotnet run --project tools/occupations/HonestAdviceGen -- --all --delay-ms 500
dotnet run --project tools/occupations/HonestAdviceGen -- --translate --delay-ms 500
```

`--force` regenerates a line even when its source hash is unchanged. Occupations that already match the current outlook facts are skipped. If the outlook has no demand line and no AI line, the tool stores "Dat weten we niet…" and does not call a model.

Env (do not commit keys):

| Variable | Meaning |
|---|---|
| `Ai__Provider` | `OpenAI` (default) or `Mistral` |
| `OpenAI__ApiKey` or `OPENAI_API_KEY` | OpenAI key |
| `OpenAI__Model` | default `gpt-4o-mini` |
| `Mistral__ApiKey` or `MISTRAL_API_KEY` | Mistral key |
| `Mistral__Model` | default `mistral-small-latest` |
| `Mistral__BaseUrl` | default `https://api.eu.mistral.ai/v1/` |

Commit the JSON after a run. The same file ships to production as an embedded resource.

## Yearly update

In September, after the O*NET August release and the ROA AIS release, pin the new files in `sources.json`, run fetch and build, and commit. Take a new ESCO version when the Commission publishes one. Re-translate new ILO task lines and leave them on concept until a human approves them.
