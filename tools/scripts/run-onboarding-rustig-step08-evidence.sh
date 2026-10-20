#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
export PATH="${HOME}/.dotnet:${PATH}"

ART="${JOBSY_ONBOARDING_RUSTIG_ARTIFACTS:-/opt/cursor/artifacts/onboarding-rustig-step08}"
MOCK="${JOBSY_ONBOARDING_RUSTIG_MOCKUP_REFS:-/opt/cursor/artifacts/onboarding-rustig-mockup-refs}"
MOCKWT="${JOBSY_MOCKUP_WORKTREE:-/tmp/jobsy-onboarding-rustig-mockups-wt}"
mkdir -p "$ART" "$MOCK" /tmp/jobsy-ci-logs

export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export VerificationCodes__Pepper="${VerificationCodes__Pepper:-ci-verification-otp-pepper-32chars!!}"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"
export JOBSY_ONBOARDING_STEP08_CLICK_AUDIT=1
export JOBSY_CAPTURE_ONBOARDING_RUSTIG=1
export JOBSY_ONBOARDING_RUSTIG_ARTIFACTS="$ART"

stop_port() {
  local port="$1"
  pkill -f "127.0.0.1:${port}" 2>/dev/null || true
  sleep 1
}

capture_mockup_refs() {
  if [[ ! -d "$MOCKWT" ]]; then
    git fetch origin cursor/onboarding-rustig-mockups-7e58
    git worktree add "$MOCKWT" origin/cursor/onboarding-rustig-mockups-7e58
  fi
  stop_port 8765
  (cd "$MOCKWT" && dotnet build Jobsy.Web/Jobsy.Web.csproj -c Release -v q)
  cd "$MOCKWT" && dotnet run --project Jobsy.Web/Jobsy.Web.csproj -c Release --no-build --urls http://127.0.0.1:8765 \
    >/tmp/jobsy-ci-logs/mockup-web.log 2>&1 &
  echo $! >/tmp/jobsy-ci-logs/mockup-web.pid
  cd "$ROOT"
  for _ in $(seq 1 90); do
    curl -sf http://127.0.0.1:8765/mockups/onboarding-rustig/start >/dev/null 2>&1 && break
    sleep 2
  done
  python3 "$ROOT/tools/scripts/capture-onboarding-rustig-mockup-viewport.py" step-08-vraag "$MOCK/step-08-desktop.png" --width 1440 --height 900
  python3 "$ROOT/tools/scripts/capture-onboarding-rustig-mockup-viewport.py" step-08-vraag "$MOCK/step-08-mobile.png" --width 390 --height 844
  if [[ -f /tmp/jobsy-ci-logs/mockup-web.pid ]]; then
    kill "$(cat /tmp/jobsy-ci-logs/mockup-web.pid)" 2>/dev/null || true
  fi
}

if [[ "${SKIP_BUILD:-}" != "1" ]]; then
  dotnet restore Jobsy.sln -v q
  dotnet build Jobsy.sln -c Release -v q
fi

capture_mockup_refs

bash "$ROOT/.github/scripts/start-ci-stack.sh"

echo "=== Rustig step 8 (enabled) ==="
export JOBSY_STEP08_AUDIT_MODE=rustig
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStep08CaptureTests" || true
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStep08ClickAuditPlaywrightTests" || true

stop_port 5201
env Onboarding__RustigEnabled=false ApiBaseUrl=http://localhost:5200/ PublicWebBaseUrl=http://localhost:5201 \
  dotnet run --project Jobsy.Web/Jobsy.Web.csproj -c Release --no-build --urls http://127.0.0.1:5201 \
  >/tmp/jobsy-ci-logs/web-baseline.log 2>&1 &
echo $! >/tmp/jobsy-ci-logs/web.pid
sleep 12

echo "=== Baseline step 8 (RustigEnabled=false) ==="
export JOBSY_STEP08_AUDIT_MODE=baseline
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStep08ClickAuditPlaywrightTests" || true

python3 "$ROOT/tools/scripts/onboarding-rustig-start-compare.py" "$ART" "$MOCK" || true

echo "Artifacts in $ART"
ls -la "$ART" "$MOCK"/step-08-*.png 2>/dev/null || true
