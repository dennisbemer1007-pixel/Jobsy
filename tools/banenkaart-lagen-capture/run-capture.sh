#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${BANENKAART_LAGEN_OUT:-/opt/cursor/artifacts/banenkaart-lagen-capture}"
MOCK_ROOT="$OUT/mockup-ref/docs/mockups/kandidaat/banenkaart/lagen"
HOST_PORT="${BANENKAART_LAGEN_PORT:-5201}"
URL_FILE="/tmp/banenkaart-lagen-base-url.txt"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

exec 3>&1 4>&2
log(){ echo "[lagen-capture] $*" >&3; }

cleanup() {
  if [[ -n "${HOST_PID:-}" ]] && kill -0 "$HOST_PID" 2>/dev/null; then
    kill "$HOST_PID" 2>/dev/null || true
    wait "$HOST_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

mkdir -p "$OUT/mockup-ref"
if [[ ! -f "$MOCK_ROOT/desktop/02-banenkaart-lagen-desktop-1440.png" ]]; then
  log "Extract mockups c8ee9298…"
  git -C "$ROOT" archive c8ee9298 docs/mockups/kandidaat/banenkaart | tar -x -C "$OUT/mockup-ref"
fi

if [[ "${BANENKAART_SKIP_BUILD:-}" != "1" ]]; then
  log "Build Release (Web + host)…"
  dotnet build "$ROOT/Jobsy.Web/Jobsy.Web.csproj" -c Release -v q
  dotnet build "$SCRIPT_DIR/Host/BanenkaartLagenCaptureHost.csproj" -c Release -v q
fi

log "Start illustrated host (Release Jobsy.Web)…"
export BANENKAART_LAGEN_URL_FILE="$URL_FILE"
dotnet run --no-build -c Release --project "$SCRIPT_DIR/Host/BanenkaartLagenCaptureHost.csproj" >&3 2>&4 &
HOST_PID=$!

for i in $(seq 1 120); do
  if [[ -f "$URL_FILE" ]]; then
    BASE="$(tr -d '\n' < "$URL_FILE")"
    if curl -sf -o /dev/null "$BASE/banenkaart" 2>/dev/null; then
      break
    fi
  fi
  sleep 1
done
BASE="$(tr -d '\n' < "$URL_FILE" 2>/dev/null || true)"
if [[ -z "$BASE" ]]; then
  echo "Host failed to start" >&2
  exit 1
fi
log "Host up at $BASE"

cd "$SCRIPT_DIR"
if [[ ! -d node_modules/playwright ]]; then
  npm install --silent
fi
npx playwright install chromium

export BANENKAART_LAGEN_BASE_URL="$BASE"
export BANENKAART_LAGEN_OUT="$OUT"
export JOBSY_REPO_ROOT="$ROOT"
export BANENKAART_CAPTURE_DEADLINE_MS="${BANENKAART_CAPTURE_DEADLINE_MS:-540000}"

log "Playwright capture (hard limit 10 min)…"
( cd "$SCRIPT_DIR" && timeout 600 node "$SCRIPT_DIR/capture-lagen.mjs" )

log "Copy to compare folder…"
mkdir -p /opt/cursor/artifacts/compare
cp -a "$OUT"/side-by-side-*.png "$OUT/REPORT.md" "$OUT/popup-photo-metrics.json" /opt/cursor/artifacts/compare/ 2>/dev/null || true

log "Complete."
