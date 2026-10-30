#!/usr/bin/env bash
# Run full JOBSY_E2E_FLOW_CHECK and copy artifacts to Agent Store fixes/{label}/.
set -euo pipefail

LABEL="${1:?usage: capture-e2e-flow-evidence.sh before|after}"
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
STORE="${JOBSY_E2E_EVIDENCE_ROOT:-/cursor/stores/self/fixes}"
DEST="$STORE/$LABEL"

export PATH="${HOME}/.dotnet:${PATH}"
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"
export JOBSY_E2E_CAPTURE_EMAIL_CODES=1
export JOBSY_E2E_FLOW_CHECK=1

cd "$ROOT"

stop_stack() {
  for f in /tmp/jobsy-ci-logs/api.pid /tmp/jobsy-ci-logs/web.pid; do
    if [[ -f "$f" ]]; then
      kill "$(cat "$f")" 2>/dev/null || true
      rm -f "$f"
    fi
  done
}

reset_db() {
  sudo -u postgres psql -c "DROP DATABASE IF EXISTS \"JobsyCi\";" postgres
  sudo -u postgres psql -c "CREATE DATABASE \"JobsyCi\";" postgres
  sudo -u postgres psql -c "CREATE EXTENSION IF NOT EXISTS postgis;" JobsyCi
}

stop_stack
reset_db

dotnet build Jobsy.sln -c Release -v q
bash "$ROOT/.github/scripts/start-ci-stack.sh"

# Do not use --no-build: MTP/xunit.v3 can report "Zero tests ran" when running the built exe without rebuild.
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release \
  --filter "FullyQualifiedName=Jobsy.Tests.E2eFlow.CandidateEmployerFlowE2ePlaywrightTests.Full_flow_check_with_screenshots_and_metrics"

stop_stack

SRC="${JOBSY_E2E_FLOW_ROOT:-/workspace/artifacts/e2e-flow}"
rm -rf "$DEST"
mkdir -p "$STORE"
cp -a "$SRC" "$DEST"
echo "Evidence copied to $DEST"
