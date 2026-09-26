#!/usr/bin/env bash
# Start API + Web for CI Playwright smoke (Development + seed).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
export Seed__Enabled=true
export JobsyAuth__AllowDevelopmentAuth=true
export JobsyAuth__AllowStubPayments=true
export VerificationCodes__Pepper="${VerificationCodes__Pepper:-ci-verification-otp-pepper-32chars}"
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export Cors__AllowedOrigins__0=http://localhost:5201
export PublicWebBaseUrl=http://localhost:5201
export ApiBaseUrl=http://localhost:5200/

mkdir -p /tmp/jobsy-ci-logs

echo "Starting Jobsy.Api on :5200 …"
dotnet run --project Jobsy.Api/Jobsy.Api.csproj -c Release --no-build --urls http://127.0.0.1:5200 \
  >/tmp/jobsy-ci-logs/api.log 2>&1 &
echo $! >/tmp/jobsy-ci-logs/api.pid

echo "Starting Jobsy.Web on :5201 …"
dotnet run --project Jobsy.Web/Jobsy.Web.csproj -c Release --no-build --urls http://127.0.0.1:5201 \
  >/tmp/jobsy-ci-logs/web.log 2>&1 &
echo $! >/tmp/jobsy-ci-logs/web.pid

for i in $(seq 1 90); do
  if curl -sf http://127.0.0.1:5200/health >/dev/null \
     && curl -sf -o /dev/null -w "%{http_code}" http://127.0.0.1:5201/ | grep -Eq '200|302|301'; then
    echo "CI stack ready."
    exit 0
  fi
  sleep 2
done

echo "CI stack failed to become ready." >&2
tail -n 80 /tmp/jobsy-ci-logs/api.log >&2 || true
tail -n 80 /tmp/jobsy-ci-logs/web.log >&2 || true
exit 1
