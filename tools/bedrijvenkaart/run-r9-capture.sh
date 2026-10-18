#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

API_URL="${API_URL:-http://127.0.0.1:5200}"
WEB_URL="${WEB_URL:-http://127.0.0.1:5201}"

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

node "$ROOT/tools/bedrijvenkaart/capture-screenshots.mjs" "$WEB_URL"
node "$ROOT/tools/bedrijvenkaart/compare-mockups.mjs"
node "$ROOT/tools/bedrijvenkaart/verify-captures.mjs"

echo "R9 artifacts: /opt/cursor/artifacts/bedrijvenkaart-build-r9/{live,compare}"
