#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ART="/opt/cursor/artifacts/onboarding-v3-click-audit"
unset JOBSY_ONBOARDING_RUSTIG_ARTIFACTS
mkdir -p "$ART"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"
export JOBSY_WELCOME_AUDIT_MODE=rustig
export JOBSY_ONBOARDING_RUSTIG_ARTIFACTS="$ART"
export JOBSY_ONBOARDING_START_CLICK_AUDIT=1
export JOBSY_ONBOARDING_STEP_CLICK_AUDIT=1
export JOBSY_ONBOARDING_STEP06_CLICK_AUDIT=1
export JOBSY_ONBOARDING_STEP07_CLICK_AUDIT=1
export JOBSY_ONBOARDING_STEP08_CLICK_AUDIT=1
export JOBSY_ONBOARDING_STEP09_CLICK_AUDIT=1
export JOBSY_ONBOARDING_STEP10_CLICK_AUDIT=1
export JOBSY_ONBOARDING_KOMPAS_CLICK_AUDIT=1

cd "$ROOT"
dotnet build Jobsy.Web/Jobsy.Web.csproj -c Release -v q
dotnet build Jobsy.Tests/Jobsy.Tests.csproj -c Release -v q
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build --timeout 45m \
  --filter "FullyQualifiedName~ClickAudit&FullyQualifiedName~Onboarding"

python3 tools/compile-onboarding-click-audit-summary.py "$ART"
