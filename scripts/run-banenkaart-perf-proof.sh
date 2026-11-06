#!/usr/bin/env bash
# Full banenkaart perf proof: Postgres JobsyCi, 3200+ seed, before (acceptatie) vs after (PR branch).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

chmod +x scripts/ensure-jobsy-ci-postgres.sh .github/scripts/stop-ci-stack.sh .github/scripts/start-ci-stack.sh
scripts/ensure-jobsy-ci-postgres.sh

export ASPNETCORE_ENVIRONMENT=Development
export Seed__Enabled=true
export Seed__BanenkaartPerfCount=3200
export ConnectionStrings__JobsyDb="Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres"
export JOBSY_E2E_BASE_URL=http://127.0.0.1:5201

reset_db() {
  PGPASSWORD=postgres psql -h 127.0.0.1 -U postgres -d postgres -v ON_ERROR_STOP=1 <<'SQL'
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'JobsyCi' AND pid <> pg_backend_pid();
DROP DATABASE IF EXISTS "JobsyCi";
CREATE DATABASE "JobsyCi";
SQL
  PGPASSWORD=postgres psql -h 127.0.0.1 -U postgres -d JobsyCi -c "CREATE EXTENSION IF NOT EXISTS postgis;"
}

employers_on() {
  PGPASSWORD=postgres psql -h 127.0.0.1 -U postgres -d JobsyCi -v ON_ERROR_STOP=1 <<'SQL'
INSERT INTO "PlatformFeatureSettings" (
  "Id", "VacancyContentModerationEnabled", "AuthenticatorEnabled", "InactiveCompanyDays",
  "SessionInactivityTimeoutMinutes", "SupportAccessNotifyAdmins", "SupportAccessNotifySubject",
  "EmployersEnabled", "ProactiveMatchingEnabled", "UpdatedAtUtc")
VALUES (
  'aaaaaaaa-0000-0000-0000-000000000001', true, true, 120, 30, false, false, true, false, NOW() AT TIME ZONE 'UTC')
ON CONFLICT ("Id") DO UPDATE SET "EmployersEnabled" = true, "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc";
SQL
}

wait_index_count() {
  for i in $(seq 1 60); do
    n="$(curl -sf "http://127.0.0.1:5200/api/vacancies/map-view" | python3 -c "import sys,json; print(json.load(sys.stdin).get('pinCount',0))" 2>/dev/null || echo 0)"
    if [[ "$n" -ge 3000 ]]; then
      echo "Discovery index pinCount=$n"
      return 0
    fi
    sleep 3
  done
  echo "Index pinCount still below 3000 (got $n)." >&2
  tail -40 /tmp/jobsy-ci-logs/api.log >&2 || true
  return 1
}

run_measure() {
  local label="$1"
  export JOBSY_BANENKAART_PERF_LABEL="$label"
  dotnet test Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~BanenkaartPerfMeasurement_capture" \
    --results-directory /tmp/banenkaart-perf-results
}

CURRENT_BRANCH="$(git branch --show-current)"
PR_BRANCH="${PR_BRANCH:-$CURRENT_BRANCH}"

echo "=== Phase 1: seed on PR branch ($PR_BRANCH) ==="
git checkout "$PR_BRANCH"
dotnet build Jobsy.sln -c Release -v q
reset_db
.github/scripts/stop-ci-stack.sh || true
.github/scripts/start-ci-stack.sh
sleep 5
employers_on
# Restart API so feature flags + index pick up DB (web can stay).
if [[ -f /tmp/jobsy-ci-logs/api.pid ]]; then kill "$(cat /tmp/jobsy-ci-logs/api.pid)" 2>/dev/null || true; fi
dotnet run --project Jobsy.Api/Jobsy.Api.csproj -c Release --no-build --urls http://127.0.0.1:5200 \
  >/tmp/jobsy-ci-logs/api.log 2>&1 &
echo $! >/tmp/jobsy-ci-logs/api.pid
wait_index_count

echo "=== Phase 2: measure AFTER ($PR_BRANCH) ==="
pwsh Jobsy.Tests/bin/Release/net10.0/playwright.ps1 install chromium 2>/dev/null || true
run_measure "after"

echo "=== Phase 3: measure BEFORE (acceptatie), same DB ==="
.github/scripts/stop-ci-stack.sh || true
git checkout acceptatie
dotnet build Jobsy.sln -c Release -v q
export Seed__BanenkaartPerfCount=0
.github/scripts/start-ci-stack.sh
sleep 3
wait_index_count
run_measure "before"

git checkout "$PR_BRANCH"
echo "Done. Artifacts: banenkaart/metrics-*.json and banenkaart/*.png"
