#!/usr/bin/env bash
# Build Release Web + Tests, restart CI stack, capture Na de match screenshots (r6+).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${JOBSY_NADMATCH_ARTIFACTS:-/opt/cursor/artifacts/nadematch-r12}"
export JOBSY_NADMATCH_ARTIFACTS="$OUT"
export JOBSY_NADMATCH_SCREENSHOTS=1
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"

cd "$ROOT"
dotnet build Jobsy.sln -c Release

# start-ci-stack does not stop prior API/Web — stale :5201 keeps old Blazor + PageAssetCatalog.
if [[ -f /tmp/jobsy-ci-logs/web.pid ]]; then kill "$(cat /tmp/jobsy-ci-logs/web.pid)" 2>/dev/null || true; fi
if [[ -f /tmp/jobsy-ci-logs/api.pid ]]; then kill "$(cat /tmp/jobsy-ci-logs/api.pid)" 2>/dev/null || true; fi
pkill -f 'Jobsy.Web/bin/Release/net10.0/Jobsy.Web' 2>/dev/null || true
pkill -f 'Jobsy.Api/bin/Release/net10.0/Jobsy.Api' 2>/dev/null || true
sleep 1

bash "$ROOT/.github/scripts/start-ci-stack.sh"

dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~NaDeMatchScreenshotPlaywrightTests.Capture_na_de_match_live_routes_per_role_viewport"

echo "Captures in $OUT ($(find "$OUT" -name '*-capture-*.png' | wc -l) PNGs)"
