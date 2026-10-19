# Na de match r6 captures

- **Artifacts:** `/opt/cursor/artifacts/nadematch-r6` (21 PNGs: 13 routes + kandidaat 360 variants)
- **Root cause (r5 captures stale):** `PageAssetCatalog` still served `na-de-match.css?v=20261018-01` while page links used other `?v=` strings; captures ran against a **leftover** `:5201` process (`--no-build`) with old Blazor + catalog. r6 bumps catalog to **`20261018-21`**, drops duplicate page `<link>` tags, scopes legacy mobile `.band` overlay rules, and **`tools/screenshots/run-nadematch-capture.sh`** rebuilds + kills stale API/Web before capture.
- **Visual gate (mobiel 390):** 01 hero crème then art strip; 06 mail preview `LobsyMark` SVG; 13 feedback label above quote.
- **CI:** `ci-pr-only-failures.txt` empty vs current acceptatie
