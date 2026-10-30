#!/usr/bin/env bash
# List test FQNs that failed on PR #740 but not on acceptatie (TRX from CI artifacts).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${1:-/opt/cursor/artifacts/stijl-A-r4/ci-pr-only-failures.txt}"
PR_RUN="${PR_CI_RUN_ID:-}"
ACC_RUN="${ACCEPTATIE_CI_RUN_ID:-39145729205}"
REPO="${GITHUB_REPOSITORY:-dennisbemer1007-pixel/Jobsy}"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

extract_trx() {
  local run_id="$1" dest="$2"
  mkdir -p "$dest"
  gh run download "$run_id" -R "$REPO" -D "$dest" 2>/dev/null || true
  find "$dest" -name '*.trx' -print0 | xargs -0 cat > "$dest/combined.trx" 2>/dev/null || true
  if [[ ! -s "$dest/combined.trx" ]]; then
    gh run view "$run_id" -R "$REPO" --log-failed > "$dest/failed.log" 2>/dev/null || true
    python3 "$ROOT/tools/ci/extract-failed-tests.py" "$dest/failed.log" > "$dest/names.txt" || true
  else
    python3 "$ROOT/tools/ci/extract-failed-tests.py" "$dest/combined.trx" > "$dest/names.txt"
  fi
}

if [[ -z "$PR_RUN" ]]; then
  PR_RUN="$(gh run list -R "$REPO" -b cursor/stijl-a-warm-humanistisch-93ef -L 1 --json databaseId -q '.[0].databaseId')"
fi

extract_trx "$PR_RUN" "$TMP/pr"
extract_trx "$ACC_RUN" "$TMP/acc"

comm -23 <(sort -u "$TMP/pr/names.txt" | grep -v '^$' || true) \
         <(sort -u "$TMP/acc/names.txt" | grep -v '^$' || true) > "$OUT" || true

{
  echo "# PR-only failures (PR run $PR_RUN vs acceptatie PR-tests $ACC_RUN)"
  echo "# acceptatie_ref=${ACCEPTATIE_REF:-origin/acceptatie}"
  echo "# Generated $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  cat "$OUT"
} > "${OUT%.txt}-report.txt"

# When empty, still record run ids in the deliverable file header.
if [[ ! -s "$OUT" ]]; then
  {
    echo "# PR CI run: $PR_RUN"
    echo "# Acceptatie baseline PR-tests run: $ACC_RUN"
    echo "# PR-only failure count: 0"
  } > "$OUT"
fi

echo "Wrote $OUT ($(wc -l < "$OUT") lines)"
