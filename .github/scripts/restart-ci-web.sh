#!/usr/bin/env bash
# Restart Jobsy.Web on :5201 (clears in-memory feature-flag cache). API keeps running.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
export ApiBaseUrl="${ApiBaseUrl:-http://localhost:5200/}"
export PublicWebBaseUrl="${PublicWebBaseUrl:-http://localhost:5201}"

mkdir -p /tmp/jobsy-ci-logs

if [[ -f /tmp/jobsy-ci-logs/web.pid ]]; then
  pid="$(cat /tmp/jobsy-ci-logs/web.pid)"
  if kill -0 "$pid" 2>/dev/null; then
    kill "$pid" 2>/dev/null || true
    wait "$pid" 2>/dev/null || true
  fi
  rm -f /tmp/jobsy-ci-logs/web.pid
fi
pkill -f "Jobsy.Web.*5201" 2>/dev/null || true
sleep 1

echo "Restarting Jobsy.Web on :5201 …"
dotnet run --project Jobsy.Web/Jobsy.Web.csproj -c Release --no-build --urls http://127.0.0.1:5201 \
  >/tmp/jobsy-ci-logs/web.log 2>&1 &
echo $! >/tmp/jobsy-ci-logs/web.pid

for i in $(seq 1 60); do
  web_code="$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5201/ || true)"
  if echo "$web_code" | grep -Eq '200|302|301'; then
    echo "Jobsy.Web ready (HTTP $web_code)."
    exit 0
  fi
  sleep 1
done

echo "Jobsy.Web failed to restart." >&2
tail -n 80 /tmp/jobsy-ci-logs/web.log >&2 || true
exit 1
