#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
export PATH="${HOME}/.dotnet:${PATH}"

ART="${JOBSY_ONBOARDING_RUSTIG_ARTIFACTS:-/opt/cursor/artifacts/onboarding-rustig-step06}"
MOCK="${JOBSY_ONBOARDING_RUSTIG_MOCKUP_REFS:-/opt/cursor/artifacts/onboarding-rustig-mockup-refs}"
mkdir -p "$ART" "$MOCK" /tmp/jobsy-ci-logs

if [[ ! -f "$MOCK/step-06-desktop.png" ]]; then
  git show origin/cursor/onboarding-rustig-mockups-7e58:Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-06-desktop.jpg >/tmp/step-06-desktop.jpg
  git show origin/cursor/onboarding-rustig-mockups-7e58:Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-06-mobile.jpg >/tmp/step-06-mobile.jpg
  python3 -c "from PIL import Image; Image.open('/tmp/step-06-desktop.jpg').save('$MOCK/step-06-desktop.png'); Image.open('/tmp/step-06-mobile.jpg').save('$MOCK/step-06-mobile.png')"
fi

export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export ConnectionStrings__JobsyDb
export VerificationCodes__Pepper="${VerificationCodes__Pepper:-ci-verification-otp-pepper-32chars!!}"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"
export JOBSY_ONBOARDING_STEP06_CLICK_AUDIT=1
export JOBSY_CAPTURE_ONBOARDING_RUSTIG=1
export JOBSY_ONBOARDING_RUSTIG_ARTIFACTS="$ART"

stop_web_only() {
  if [[ -f /tmp/jobsy-ci-logs/web.pid ]]; then
    kill "$(cat /tmp/jobsy-ci-logs/web.pid)" 2>/dev/null || true
  fi
  sleep 2
}

start_web() {
  local rustig_enabled="${1:-true}"
  env Onboarding__RustigEnabled="$rustig_enabled" \
    ApiBaseUrl=http://localhost:5200/ \
    PublicWebBaseUrl=http://localhost:5201 \
    dotnet run --project Jobsy.Web/Jobsy.Web.csproj -c Release --no-build --urls http://127.0.0.1:5201 \
    >/tmp/jobsy-ci-logs/web.log 2>&1 &
  echo $! >/tmp/jobsy-ci-logs/web.pid
}

if [[ "${SKIP_BUILD:-}" != "1" ]]; then
  dotnet restore Jobsy.sln -v q
  dotnet build Jobsy.sln -c Release -v q
fi

bash "$ROOT/.github/scripts/start-ci-stack.sh"

echo "=== Rustig step 6 (enabled) ==="
export JOBSY_STEP06_AUDIT_MODE=rustig
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStep06CaptureTests" || true
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStep06ClickAuditPlaywrightTests" || true

stop_web_only
start_web "false"
sleep 10
curl -sf http://127.0.0.1:5201/ >/dev/null || true

echo "=== Baseline step 6 (RustigEnabled=false) ==="
export JOBSY_STEP06_AUDIT_MODE=baseline
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStep06ClickAuditPlaywrightTests" || true

python3 "$ROOT/tools/scripts/onboarding-rustig-start-compare.py" "$ART" "$MOCK" || true

echo "Artifacts in $ART"
ls -la "$ART"
