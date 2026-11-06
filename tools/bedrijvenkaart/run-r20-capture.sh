#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

WEB_URL="${WEB_URL:-http://127.0.0.1:5201}"
export BEDRIFJKART_CAPTURE_DIR="/opt/cursor/artifacts/bedrijvenkaart-build-r20/live"
export CAPTURE_SET=r20

wait_http() {
  local url="$1"
  for _ in $(seq 1 90); do
    if curl -sf "$url" >/dev/null 2>&1; then
      return 0
    fi
    sleep 2
  done
  return 1
}

dotnet build "$ROOT/Jobsy.Api/Jobsy.Api.csproj" -c Release -v q
dotnet build "$ROOT/Jobsy.Web/Jobsy.Web.csproj" -c Release --no-incremental -v q
for pidfile in /tmp/jobsy-ci-logs/web.pid /tmp/jobsy-ci-logs/api.pid; do
  if [ -f "$pidfile" ]; then kill "$(cat "$pidfile")" 2>/dev/null || true; fi
done

bash "$ROOT/.github/scripts/start-ci-stack.sh"
wait_http "$WEB_URL/bedrijvenkaart"

css_v="$(curl -sf "$WEB_URL/bedrijvenkaart" | grep -o 'bedrijvenkaart.css?v=[^"]*' | head -1)"
echo "Loaded stylesheet: ${css_v:-MISSING}"
test "$css_v" = "bedrijvenkaart.css?v=20261019-03"

profile=""
for _ in $(seq 1 60); do
  profile="$(curl -sf "http://127.0.0.1:5200/api/company-map-initiative/public/12345678/profile" 2>/dev/null || true)"
  if echo "$profile" | grep -q 'bakkerij-korenaar/sfeer-bakkerij'; then
    break
  fi
  sleep 3
done
echo "$profile" | grep -q 'bakkerij-korenaar/sfeer-bakkerij' || { echo "Missing demo sfeer URLs in API profile"; echo "$profile"; exit 1; }

node "$ROOT/tools/bedrijvenkaart/capture-screenshots.mjs" "$WEB_URL"
node "$ROOT/tools/bedrijvenkaart/verify-captures.mjs" "$BEDRIFJKART_CAPTURE_DIR"

echo "R20 artifacts: /opt/cursor/artifacts/bedrijvenkaart-build-r20/"
