#!/usr/bin/env bash
# Usage: BASE=http://127.0.0.1:5201 tools/perf/lighthouse-median.sh path1 path2 ...
set -euo pipefail
BASE="${BASE:-http://127.0.0.1:5201}"
OUT="${OUT:-/tmp/lh-runs}"
mkdir -p "$OUT"
runs=3

median() {
  python3 - "$@" <<'PY'
import sys, statistics
nums = [float(x) for x in sys.argv[1:]]
print(statistics.median(nums))
PY
}

for path in "$@"; do
  slug="${path//\//}"
  [[ -z "$slug" ]] && slug="home"
  for form in desktop mobile; do
    scores=(); fcps=(); lcps=(); tbts=(); clss=()
    for i in $(seq 1 "$runs"); do
      f="$OUT/${slug}-${form}-${i}.json"
      if [[ "$form" == "mobile" ]]; then
        npx --yes lighthouse "${BASE}${path}" --quiet --chrome-flags="--headless --no-sandbox" \
          --only-categories=performance --preset=perf --form-factor=mobile \
          --output=json --output-path="$f" >/dev/null 2>&1
      else
        npx --yes lighthouse "${BASE}${path}" --quiet --chrome-flags="--headless --no-sandbox" \
          --only-categories=performance --preset=desktop \
          --output=json --output-path="$f" >/dev/null 2>&1
      fi
      read score fcp lcp tbt cls <<< "$(python3 - "$f" <<'PY'
import json, sys
with open(sys.argv[1]) as fh:
    j = json.load(fh)
aud = j["audits"]
perf = j["categories"]["performance"]["score"] * 100
def v(id):
    return aud[id].get("numericValue") or 0
print(int(round(perf)), int(v("first-contentful-paint")), int(v("largest-contentful-paint")), int(v("total-blocking-time")), round(v("cumulative-layout-shift"), 3))
PY
)"
      scores+=("$score"); fcps+=("$fcp"); lcps+=("$lcp"); tbts+=("$tbt"); clss+=("$cls")
    done
    ms=$(median "${scores[@]}")
    mf=$(median "${fcps[@]}")
    ml=$(median "${lcps[@]}")
    mt=$(median "${tbts[@]}")
    mc=$(median "${clss[@]}")
    printf '%s|%s|score=%s|fcp=%s|lcp=%s|tbt=%s|cls=%s\n' "$slug" "$form" "$ms" "$mf" "$ml" "$mt" "$mc"
  done
done
