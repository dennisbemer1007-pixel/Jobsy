#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

API_URL="${API_URL:-http://127.0.0.1:5200}"
WEB_URL="${WEB_URL:-http://127.0.0.1:5201}"
export BEDRIFJKART_CAPTURE_DIR="/opt/cursor/artifacts/bedrijvenkaart-build-r10/live"

wait_http() {
  local url="$1"
  local label="$2"
  for _ in $(seq 1 90); do
    if curl -sf "$url" >/dev/null 2>&1; then
      echo "$label ready: $url"
      return 0
    fi
    sleep 2
  done
  echo "Timeout waiting for $label ($url)" >&2
  return 1
}

if ! curl -sf "$API_URL/api/public/companies/12345678" | grep -q Bakkerij; then
  echo "Starting CI stack (API + Web + seed)…"
  bash "$ROOT/.github/scripts/start-ci-stack.sh"
fi

wait_http "$WEB_URL/" "Web"
wait_http "$API_URL/api/public/companies/12345678" "Public company API"

dotnet build "$ROOT/Jobsy.Api/Jobsy.Api.csproj" -c Release -v q
dotnet build "$ROOT/Jobsy.Web/Jobsy.Web.csproj" -c Release --no-incremental -v q
for pidfile in /tmp/jobsy-ci-logs/web.pid /tmp/jobsy-ci-logs/api.pid; do
  if [ -f "$pidfile" ]; then kill "$(cat "$pidfile")" 2>/dev/null || true; fi
done
bash "$ROOT/.github/scripts/start-ci-stack.sh"

wait_http "$WEB_URL/bedrijvenkaart" "Bedrijvenkaart"

node "$ROOT/tools/bedrijvenkaart/capture-screenshots.mjs" "$WEB_URL"
node "$ROOT/tools/bedrijvenkaart/compare-mockups.mjs"
node "$ROOT/tools/bedrijvenkaart/verify-captures.mjs"

echo "R10 artifacts: /opt/cursor/artifacts/bedrijvenkaart-build-r10/{live,compare}"
