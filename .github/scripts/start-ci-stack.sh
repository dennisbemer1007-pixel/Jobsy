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
# Playwright WaitForFunctionAsync needs eval; Acc/prod CSP stays without unsafe-eval.
export JOBSY_CSP_ALLOW_UNSAFE_EVAL=1
# Errors 06: the browser suite needs the real 500 page (not the developer exception page) and a
# path that throws on purpose. Both are off by default and are never set on Render.
export Errors__ForceHandler=true
export Errors__EnableTestThrow=true
export VerificationCodes__Pepper="${VerificationCodes__Pepper:-ci-verification-otp-pepper-32chars}"
# Playwright public-pages suites POST /melden after many GETs; keep DSA limits from blocking CI.
export RateLimiting__ReportHourlyPermitLimit=100
export RateLimiting__ReportDailyPermitLimit=200
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

# /health returns before EF migrate+seed finishes (BackgroundService). Wait until
# pins succeed so Playwright does not race an empty schema.
for i in $(seq 1 120); do
  api_health=0
  pins_ok=0
  web_ok=0
  if curl -sf http://127.0.0.1:5200/health >/dev/null; then
    api_health=1
  fi
  pins_code="$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5200/api/vacancies/pins || true)"
  # 200/304 = employers ON. 404 = employers OFF (feature_disabled, decision 20) once /health is up.
  if echo "$pins_code" | grep -Eq '200|304|404'; then
    pins_ok=1
  fi
  web_code="$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5201/ || true)"
  if echo "$web_code" | grep -Eq '200|302|301'; then
    web_ok=1
  fi
  if [[ "$api_health" -eq 1 && "$pins_ok" -eq 1 && "$web_ok" -eq 1 ]]; then
    echo "CI stack ready (health + pins + web)."
    exit 0
  fi
  if (( i % 15 == 0 )); then
    echo "Waiting for stack… health=$api_health pins=$pins_code web=$web_code ($i/120)"
  fi
  sleep 2
done

echo "CI stack failed to become ready." >&2
tail -n 120 /tmp/jobsy-ci-logs/api.log >&2 || true
tail -n 80 /tmp/jobsy-ci-logs/web.log >&2 || true
exit 1
