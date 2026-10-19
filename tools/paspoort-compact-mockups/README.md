# Paspoort compact layout mockups (Step 1)

Static HTML/CSS explorations for a shorter `/candidate/paspoort` desktop fold. **Fake user only:** Alex Janssen (`#A-20481`). No product code changes.

## Variants

| ID | Idea |
| --- | --- |
| **A** | Slim greeting bar (~84px) with illustration strip on the right; classic ~1100px column. |
| **B** | No top hero on desktop; greeting lives in the passport card header area. |
| **C** | Uses wide screens (~1680px): sticky passport column, wider main, **illustration in the right margin**; nudge inline beside “Dit ben jij”. |

## View locally

```bash
cd tools/paspoort-compact-mockups
npm install
node scripts/generate-html.mjs
npx --yes serve -p 5199 .
# open http://127.0.0.1:5199/variant-a-slim-bar.html
```

Images are loaded via symlink `www-images` → `Jobsy.Web/wwwroot/images`.

## Regenerate PNG captures

```bash
cd tools/paspoort-compact-mockups
npm install
node scripts/generate-html.mjs
node scripts/capture.mjs
```

Outputs:

- PNGs: `/opt/cursor/artifacts/paspoort-compact-mockups/variant-{a,b,c}-{1440x900,1366x768,1920x1080,390-mobile}.png`
- Coverage table: `above-the-fold-coverage.md` (same folder + copy in this directory)

Desktop fold line = full viewport height (browser chrome excluded). Mobile 390px is a stacked scroll layout by design.
