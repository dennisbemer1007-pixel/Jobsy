#!/usr/bin/env bash
# Restart Jobsy.Api on :5200 (clears in-memory feature-flag cache).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
export ConnectionStrings__JobsyDb="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export Cors__AllowedOrigins__0=http://localhost:5201
export PublicWebBaseUrl="${PublicWebBaseUrl:-http://localhost:5201}"
export ApiBaseUrl="${ApiBaseUrl:-http://localhost:5200/}"
export Seed__Enabled=true
export JobsyAuth__AllowDevelopmentAuth=true

mkdir -p /tmp/jobsy-ci-logs

if [[ -f /tmp/jobsy-ci-logs/api.pid ]]; then
  pid="$(cat /tmp/jobsy-ci-logs/api.pid)"
  if kill -0 "$pid" 2>/dev/null; then
    kill "$pid" 2>/dev/null || true
    wait "$pid" 2>/dev/null || true
  fi
  rm -f /tmp/jobsy-ci-logs/api.pid
fi
pkill -f "Jobsy.Api.*5200" 2>/dev/null || true
sleep 1

echo "Restarting Jobsy.Api on :5200 …"
env JOBSY_E2E_CAPTURE_EMAIL_CODES=1 dotnet run --project Jobsy.Api/Jobsy.Api.csproj -c Release --no-build --urls http://127.0.0.1:5200 \
  >/tmp/jobsy-ci-logs/api.log 2>&1 &
echo $! >/tmp/jobsy-ci-logs/api.pid

for i in $(seq 1 90); do
  if curl -sf http://127.0.0.1:5200/health >/dev/null; then
    echo "Jobsy.Api ready."
    exit 0
  fi
  sleep 1
done

echo "Jobsy.Api failed to restart." >&2
tail -n 80 /tmp/jobsy-ci-logs/api.log >&2 || true
exit 1
