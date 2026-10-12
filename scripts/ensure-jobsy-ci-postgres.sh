#!/usr/bin/env bash
# PostGIS Postgres for JobsyCi (local perf / Playwright).
set -euo pipefail

if pg_isready -h 127.0.0.1 -p 5432 -U postgres >/dev/null 2>&1; then
  echo "Postgres already listening on 5432."
  exit 0
fi

if command -v docker >/dev/null 2>&1; then
  if docker ps -a --format '{{.Names}}' | grep -qx jobsy-ci-postgis; then
    docker start jobsy-ci-postgis >/dev/null
  else
    docker run -d --name jobsy-ci-postgis \
      -e POSTGRES_USER=postgres \
      -e POSTGRES_PASSWORD=postgres \
      -e POSTGRES_DB=JobsyCi \
      -p 5432:5432 \
      postgis/postgis:16-3.4 >/dev/null
  fi
  for i in $(seq 1 30); do
    if pg_isready -h 127.0.0.1 -p 5432 -U postgres >/dev/null 2>&1; then
      echo "PostGIS container ready."
      exit 0
    fi
    sleep 2
  done
  echo "Postgres container failed to become ready." >&2
  exit 1
fi

echo "No docker and no postgres on 5432 — install PostGIS or start docker." >&2
exit 1
