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

## Yearly update

In September, after the O*NET August release and the ROA AIS release, pin the new files in `sources.json`, run fetch and build, and commit. Take a new ESCO version when the Commission publishes one. Re-translate new ILO task lines and leave them on concept until a human approves them.
