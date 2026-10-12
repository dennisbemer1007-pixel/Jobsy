#!/usr/bin/env bash
set -euo pipefail
for f in /tmp/jobsy-ci-logs/api.pid /tmp/jobsy-ci-logs/web.pid; do
  if [[ -f "$f" ]]; then
    pid="$(cat "$f")"
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid" 2>/dev/null || true
      wait "$pid" 2>/dev/null || true
    fi
    rm -f "$f"
  fi
done
pkill -f "Jobsy.Api.*5200" 2>/dev/null || true
pkill -f "Jobsy.Web.*5201" 2>/dev/null || true
