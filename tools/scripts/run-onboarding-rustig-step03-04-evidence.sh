#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
export PATH="${HOME}/.dotnet:${PATH}"
ART="${JOBSY_ONBOARDING_RUSTIG_ARTIFACTS:-/cursor/stores/self/artifacts/onboarding-rustig-start}"
mkdir -p "$ART" /tmp/jobsy-ci-logs
export JOBSY_ONBOARDING_RUSTIG_ARTIFACTS="$ART"
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export VerificationCodes__Pepper="${VerificationCodes__Pepper:-ci-verification-otp-pepper-32charsxx}"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"

stop_web() {
  if [[ -f /tmp/jobsy-ci-logs/web.pid ]]; then
    kill "$(cat /tmp/jobsy-ci-logs/web.pid)" 2>/dev/null || true
    sleep 2
  fi
}

start_web() {
  local rustig_enabled="${1:-true}"
  stop_web
  env Onboarding__RustigEnabled="$rustig_enabled" \
    ApiBaseUrl=http://localhost:5200/ \
    PublicWebBaseUrl=http://localhost:5201 \
    dotnet run --project Jobsy.Web/Jobsy.Web.csproj -c Release --no-build --urls http://127.0.0.1:5201 \
    >/tmp/jobsy-ci-logs/web.log 2>&1 &
  echo $! >/tmp/jobsy-ci-logs/web.pid
  for i in $(seq 1 60); do
    code="$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5201/ || true)"
    echo "$code" | grep -Eq '200|302|301' && return 0
    sleep 2
  done
  tail -30 /tmp/jobsy-ci-logs/web.log >&2
  exit 1
}

dotnet build Jobsy.sln -c Release -v q
bash .github/scripts/start-ci-stack.sh

export JOBSY_CAPTURE_ONBOARDING_RUSTIG=1
export JOBSY_ONBOARDING_STEP_CLICK_AUDIT=1
export JOBSY_WELCOME_AUDIT_MODE=rustig

dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStep04CaptureTests|FullyQualifiedName~OnboardingRustigStepClickAuditPlaywrightTests.Audit_step04_clicks"

export JOBSY_WELCOME_AUDIT_MODE=baseline
start_web "false"
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStepClickAuditPlaywrightTests.Audit_step04_clicks"

python3 tools/scripts/onboarding-rustig-start-compare.py "$ART"
ls -la "$ART"/step-04-*-viewport.png "$ART"/compare-*step-04.png "$ART"/click-audit-step-04-* 2>/dev/null || true
