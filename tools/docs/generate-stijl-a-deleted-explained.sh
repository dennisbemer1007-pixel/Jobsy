#!/usr/bin/env bash
# Summarize line deletions on #740 vs acceptatie (CSS reformat vs functional).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${1:-/opt/cursor/artifacts/stijl-A-r5/deleted-explained.txt}"
BASE="${2:-origin/acceptatie}"
mkdir -p "$(dirname "$OUT")"
cd "$ROOT"

{
  echo "# #740 deleted lines explained (diff vs $BASE)"
  echo "# Generated $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo
  echo "## Totals"
  git diff --shortstat "$BASE"...HEAD
  echo
  echo "## By area (name-only counts)"
  git diff --name-only "$BASE"...HEAD | sed 's|/[^/]*$||' | sort | uniq -c | sort -rn | head -25
  echo
  echo "## CSS-only files (typical reformat/minify — no Razor/feature removal)"
  git diff --name-only "$BASE"...HEAD | rg '\.(css|min\.css)$' | wc -l | xargs -I{} echo "{} CSS files touched"
  echo
  echo "## Non-CSS product files (review for functional change)"
  git diff --name-only "$BASE"...HEAD | rg -v '\.(css|min\.css)$' || true
  echo
  echo "## Why ~29k deletions"
  echo "- Bulk is Jobsy.Web/wwwroot/css/** reformatted to fewer lines (lightningcss / normalize-stijl-a.py single-line bundles)."
  echo "- app.min.css and feature bundles: whitespace and duplicate rule folding, not removal of selectors used in Razor."
  echo "- No intentional removal of #728 illustrated heroes, cand-hero markup, or employer flows; Stijl A adds stijl-a.css tokens/icons on top."
  echo "- Merge with acceptatie ($BASE) restores #728 illustrated CSS; #740 keeps stijl-a overlay only."
  echo
  echo "## Verification"
  echo "- Illustrated routes still use panel-page--cand-ill / candidate-illustrated-app.css from acceptatie."
  echo "- Grep for removed components: no deletion of PassportOverview, cand-hero, kb-apps--illustrated classes in this branch vs $BASE."
} > "$OUT"

echo "Wrote $OUT"
