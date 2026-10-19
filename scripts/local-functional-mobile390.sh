#!/usr/bin/env bash
# Fast loop for mobile-390 onboarding functional gate (Api :5200, Web :5201 must be up).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

export JOBSY_E2E_FUNCTIONAL_GATE=1
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"
export JOBSY_CI_WEB_URL="$JOBSY_E2E_BASE_URL"

FILTER='Suite=FunctionalE2e&(FullyQualifiedName~Kennismaken_step1|FullyQualifiedName~Full_onboarding)&DisplayName~mobile-390'

while [[ "${1:-}" == "--rebuild-web" || "${1:-}" == "--rebuild-tests" ]]; do
  case "$1" in
  --rebuild-web)
    shift
  dotnet build Jobsy.Web/Jobsy.Web.csproj -c Release
  if [[ -f /tmp/jobsy-ci-logs/web.pid ]]; then
    kill "$(cat /tmp/jobsy-ci-logs/web.pid)" 2>/dev/null || true
    sleep 1
  fi
  ASPNETCORE_ENVIRONMENT=Development \
    Jobsy__TestEmployersEnabled=true \
    Seed__Enabled=true \
    JobsyAuth__AllowDevelopmentAuth=true \
    JOBSY_CSP_ALLOW_UNSAFE_EVAL=1 \
    JOBSY_E2E_CAPTURE_EMAIL_CODES=1 \
    ConnectionStrings__JobsyDb='Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres' \
    Cors__AllowedOrigins__0=http://localhost:5201 \
    PublicWebBaseUrl=http://localhost:5201 \
    ApiBaseUrl=http://127.0.0.1:5200/ \
    dotnet run --project Jobsy.Web/Jobsy.Web.csproj -c Release --no-build --urls http://127.0.0.1:5201 \
    >/tmp/jobsy-ci-logs/web.log 2>&1 &
  echo $! >/tmp/jobsy-ci-logs/web.pid
  sleep 2
    ;;
  --rebuild-tests)
    shift
    dotnet build Jobsy.Tests/Jobsy.Tests.csproj -c Release
    ;;
  esac
done

run_one() {
  dotnet test Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build --filter "$1" "${@:2}"
}

run_one 'Suite=FunctionalE2e&FullyQualifiedName~Kennismaken_step1_controls_are_interactive&DisplayName~mobile-390' "$@"
sleep 1
run_one 'Suite=FunctionalE2e&FullyQualifiedName~Full_onboarding_registration&DisplayName~mobile-390' "$@"
