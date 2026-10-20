#!/usr/bin/env bash
# Local CI stack + welcome click audit + screenshots for onboarding rustig PR1.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
export PATH="${HOME}/.dotnet:${PATH}"

ART="${JOBSY_ONBOARDING_RUSTIG_ARTIFACTS:-/opt/cursor/artifacts/onboarding-rustig-start}"
mkdir -p "$ART" /tmp/jobsy-ci-logs

export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export VerificationCodes__Pepper="${VerificationCodes__Pepper:-ci-verification-otp-pepper-32chars!!}"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"
export JOBSY_ONBOARDING_START_CLICK_AUDIT=1
export JOBSY_ONBOARDING_RUSTIG_ARTIFACTS="$ART"

stop_stack() {
  for f in /tmp/jobsy-ci-logs/web.pid /tmp/jobsy-ci-logs/api.pid; do
    if [[ -f "$f" ]]; then
      kill "$(cat "$f")" 2>/dev/null || true
    fi
  done
  sleep 2
}

stop_web_only() {
  if [[ -f /tmp/jobsy-ci-logs/web.pid ]]; then
    kill "$(cat /tmp/jobsy-ci-logs/web.pid)" 2>/dev/null || true
  fi
  sleep 2
}

start_api() {
  echo "Starting API…"
  env JOBSY_E2E_CAPTURE_EMAIL_CODES=1 \
    Jobsy__TestEmployersEnabled=true \
    Seed__Enabled=true \
    JobsyAuth__AllowDevelopmentAuth=true \
    JobsyAuth__AllowStubPayments=true \
    JobsyAuth__DisableAccountLockout=true \
    JOBSY_CSP_ALLOW_UNSAFE_EVAL=1 \
    VerificationCodes__Pepper="$VerificationCodes__Pepper" \
    ConnectionStrings__JobsyDb="$ConnectionStrings__JobsyDb" \
    Cors__AllowedOrigins__0=http://localhost:5201 \
    PublicWebBaseUrl=http://localhost:5201 \
    ApiBaseUrl=http://localhost:5200/ \
    dotnet run --project Jobsy.Api/Jobsy.Api.csproj -c Release --no-build --urls http://127.0.0.1:5200 \
    >/tmp/jobsy-ci-logs/api.log 2>&1 &
  echo $! >/tmp/jobsy-ci-logs/api.pid
}

start_web() {
  local rustig_enabled="${1:-true}"
  echo "Starting Web (Onboarding:RustigEnabled=$rustig_enabled)…"
  env Onboarding__RustigEnabled="$rustig_enabled" \
    ApiBaseUrl=http://localhost:5200/ \
    PublicWebBaseUrl=http://localhost:5201 \
    dotnet run --project Jobsy.Web/Jobsy.Web.csproj -c Release --no-build --urls http://127.0.0.1:5201 \
    >/tmp/jobsy-ci-logs/web.log 2>&1 &
  echo $! >/tmp/jobsy-ci-logs/web.pid
}

wait_stack() {
  for i in $(seq 1 120); do
    api_health=0
    pins_ok=0
    web_ok=0
    curl -sf http://127.0.0.1:5200/health >/dev/null 2>&1 && api_health=1
    pins_code="$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5200/api/vacancies/pins || true)"
    echo "$pins_code" | grep -Eq '200|304|404' && pins_ok=1
    web_code="$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5201/ || true)"
    echo "$web_code" | grep -Eq '200|302|301' && web_ok=1
    if [[ "$api_health" -eq 1 && "$pins_ok" -eq 1 && "$web_ok" -eq 1 ]]; then
      echo "Stack ready."
      return 0
    fi
    sleep 2
  done
  echo "Stack failed." >&2
  tail -40 /tmp/jobsy-ci-logs/api.log >&2 || true
  tail -40 /tmp/jobsy-ci-logs/web.log >&2 || true
  exit 1
}

if [[ "${SKIP_BUILD:-}" != "1" ]]; then
  echo "Building Release…"
  dotnet restore Jobsy.sln -v q
  dotnet build Jobsy.sln -c Release -v q
fi

stop_stack
chmod +x "$ROOT/.github/scripts/start-ci-stack.sh"
VerificationCodes__Pepper="$VerificationCodes__Pepper" \
  ConnectionStrings__JobsyDb="$ConnectionStrings__JobsyDb" \
  bash "$ROOT/.github/scripts/start-ci-stack.sh"

echo "=== Rustig click audit + screenshots (default Onboarding:RustigEnabled=true) ==="
export JOBSY_WELCOME_AUDIT_MODE=rustig
export JOBSY_CAPTURE_ONBOARDING_RUSTIG=1
if ! dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingStartWelcomeClickAuditPlaywrightTests|FullyQualifiedName~OnboardingRustigWelcomeCaptureTests|FullyQualifiedName~OnboardingRustigWelcomePlaywrightTests"; then
  echo "Rustig audit/capture had failures; see reports in $ART" >&2
fi

stop_web_only
start_web "false"
wait_stack

echo "=== Baseline click audit (RustigEnabled=false) ==="
export JOBSY_WELCOME_AUDIT_MODE=baseline
export JOBSY_CAPTURE_ONBOARDING_RUSTIG=0
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingStartWelcomeClickAuditPlaywrightTests"

echo "Artifacts in $ART"
ls -la "$ART"
