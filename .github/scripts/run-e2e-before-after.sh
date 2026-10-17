#!/usr/bin/env bash
set -euo pipefail

WORKSPACE="/workspace"
ACC="/tmp/jobsy-acc"
STORE="/cursor/stores/self/fixes"
export PATH="${HOME}/.dotnet:${PATH}"
export ConnectionStrings__JobsyDb="Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres"
export JOBSY_E2E_BASE_URL="http://127.0.0.1:5201"
export JOBSY_E2E_CAPTURE_EMAIL_CODES=1
export JOBSY_E2E_FLOW_CHECK=1

stop_stack() {
  for f in /tmp/jobsy-ci-logs/api.pid /tmp/jobsy-ci-logs/web.pid; do
    [[ -f "$f" ]] && kill "$(cat "$f")" 2>/dev/null || true
    rm -f "$f"
  done
  sleep 2
}

reset_db() {
  sudo -u postgres psql -c 'DROP DATABASE IF EXISTS "JobsyCi";' postgres
  sudo -u postgres psql -c 'CREATE DATABASE "JobsyCi";' postgres
  sudo -u postgres psql -c 'CREATE EXTENSION IF NOT EXISTS postgis;' JobsyCi
}

run_label() {
  local label="$1"
  local code_root="$2"
  echo "=== E2E evidence: $label (code from $code_root) ==="
  stop_stack
  reset_db
  cd "$code_root"
  dotnet build Jobsy.sln -c Release -v q
  bash "$code_root/.github/scripts/start-ci-stack.sh"
  cd "$WORKSPACE"
  dotnet test Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~CandidateEmployerFlowE2ePlaywrightTests" \
    --logger "console;verbosity=minimal" || true
  stop_stack
  rm -rf "$STORE/$label"
  mkdir -p "$STORE"
  cp -a /cursor/stores/self/e2e-flow "$STORE/$label"
  echo "Saved $STORE/$label"
}

mkdir -p "$STORE/compare"
run_label before "$ACC"
run_label after "$WORKSPACE"

# Side-by-side compare for matching PNG names
if command -v compare >/dev/null 2>&1; then
  for before_png in "$STORE/before/screens"/*.png; do
    base="$(basename "$before_png")"
    after_png="$STORE/after/screens/$base"
    if [[ -f "$after_png" ]]; then
      compare -metric AE "$before_png" "$after_png" "$STORE/compare/$base" 2>/dev/null || cp "$after_png" "$STORE/compare/$base"
    fi
  done
  for before_pdf in "$STORE/before/pdf"/*.png; do
    base="$(basename "$before_pdf")"
    after_pdf="$STORE/after/pdf/$base"
    if [[ -f "$after_pdf" ]]; then
      compare -metric AE "$before_pdf" "$after_pdf" "$STORE/compare/pdf-$base" 2>/dev/null || cp "$after_pdf" "$STORE/compare/pdf-$base"
    fi
  done
else
  echo "ImageMagick compare not installed; compare/ contains after copies only where matched."
  for after_png in "$STORE/after/screens"/*.png; do
    cp "$after_png" "$STORE/compare/$(basename "$after_png")"
  done
fi

echo "Done before/after/compare under $STORE"
