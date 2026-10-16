#!/usr/bin/env bash
# Local banenkaart benchmark: LH median (3) + Playwright markers ms. Requires seeded JobsyCi + employers on.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BASE="${BASE:-http://127.0.0.1:5201}"
LABEL="${LABEL:-run}"
OUT="${OUT:-/cursor/stores/self/perf}"
mkdir -p "$OUT"

echo "=== Lighthouse banenkaart ($LABEL) ==="
LH_LINES=$(BASE="$BASE" OUT="/tmp/lh-$LABEL" bash "$ROOT/tools/perf/lighthouse-median.sh" /banenkaart)
echo "$LH_LINES" | tee "$OUT/banenkaart-lighthouse-$LABEL.txt"

echo "=== Playwright markers + screenshots ($LABEL) ==="
export BASE LABEL OUT
node "$OUT/banenkaart-playwright-bench.mjs" 2>&1 | tee "$OUT/banenkaart-playwright-$LABEL.txt"
