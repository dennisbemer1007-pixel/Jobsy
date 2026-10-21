#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
export PATH="${HOME}/.dotnet:${PATH}"

mkdir -p /opt/cursor/artifacts /tmp/jobsy-ci-logs

export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export VerificationCodes__Pepper="${VerificationCodes__Pepper:-ci-verification-otp-pepper-32chars!!}"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"
export JOBSY_CAPTURE_PASPOORT_R3=1

if [[ "${SKIP_BUILD:-}" != "1" ]]; then
  dotnet build Jobsy.sln -c Release -v q
fi

bash "$ROOT/.github/scripts/start-ci-stack.sh"

dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~PaspoortRustigR3CaptureTests"

echo "Artifacts:"
ls -la /opt/cursor/artifacts/paspoort-r3-* 2>/dev/null || true
