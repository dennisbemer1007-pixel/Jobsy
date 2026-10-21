#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
export PATH="${HOME}/.dotnet:${PATH}"
ART="${JOBSY_ONBOARDING_RUSTIG_ARTIFACTS:-/cursor/stores/self/artifacts/onboarding-rustig-start}"
mkdir -p "$ART"
export JOBSY_ONBOARDING_RUSTIG_ARTIFACTS="$ART"
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export VerificationCodes__Pepper="${VerificationCodes__Pepper:-ci-verification-otp-pepper-32charsxx}"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"

dotnet build Jobsy.sln -c Release -v q
bash .github/scripts/start-ci-stack.sh

export JOBSY_CAPTURE_ONBOARDING_RUSTIG=1
export JOBSY_ONBOARDING_START_CLICK_AUDIT=1
export JOBSY_ONBOARDING_STEP_CLICK_AUDIT=1
export JOBSY_WELCOME_AUDIT_MODE=rustig

dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~OnboardingRustigStep03CaptureTests|FullyQualifiedName~OnboardingRustigStep04CaptureTests|FullyQualifiedName~OnboardingRustigStepClickAuditPlaywrightTests.Audit_step03|FullyQualifiedName~OnboardingRustigStepClickAuditPlaywrightTests.Audit_step04"

python3 tools/scripts/onboarding-rustig-start-compare.py "$ART"
ls -la "$ART"/compare-desktop-step-03.png "$ART"/compare-mobile-step-03.png \
  "$ART"/compare-desktop-step-04.png "$ART"/compare-mobile-step-04.png \
  "$ART"/click-audit-step-04-* 2>/dev/null || true
