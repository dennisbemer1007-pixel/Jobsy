#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="/opt/cursor/artifacts/voorbeelden-r6"
SESSION_NAME="jobsy-web-capture-voorbeelden-r6"

cleanup() {
  tmux -f /exec-daemon/tmux.portal.conf kill-session -t "=$SESSION_NAME" 2>/dev/null || true
}
trap cleanup EXIT

mkdir -p "${OUT}"

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5201
export Jobsy__CaptureMode=true
export E2E__SkipHttpsRedirect=true
export JOBSY_CAPTURE_OUT="${OUT}"
export JOBSY_CAPTURE_VOORBEELDEN=1
export JOBSY_CAPTURE_DESKTOP_ONLY=1
export JOBSY_CAPTURE_LANG=nl
unset JOBSY_CAPTURE_MOBILE_ONLY JOBSY_TESTBLOKKEN_STEM

dotnet build "${ROOT}/Jobsy.Web/Jobsy.Web.csproj" -v q
dotnet build "${ROOT}/Jobsy.Tools.Capture/Jobsy.Tools.Capture.csproj" -v q

tmux -f /exec-daemon/tmux.portal.conf kill-session -t "=$SESSION_NAME" 2>/dev/null || true
tmux -f /exec-daemon/tmux.portal.conf new-session -d -s "$SESSION_NAME" -c "$ROOT" -- "${SHELL:-zsh}" -l
tmux -f /exec-daemon/tmux.portal.conf send-keys -t "$SESSION_NAME:0.0" "dotnet run --project ${ROOT}/Jobsy.Web/Jobsy.Web.csproj --no-build" C-m

for i in $(seq 1 45); do
  curl -sf -m 2 -o /dev/null "http://localhost:5201/" && break
  sleep 2
done
curl -sf -m 2 -o /dev/null "http://localhost:5201/" || { echo "Web failed to start"; exit 1; }

for stem in voorbeeld-hoe-werkt voorbeeld-leuk; do
  echo "=== ${stem} nl 1440 ==="
  JOBSY_TESTBLOKKEN_STEM="$stem" JOBSY_CAPTURE_OUT="${OUT}" dotnet run --project "${ROOT}/Jobsy.Tools.Capture/Jobsy.Tools.Capture.csproj" --no-build
  sleep 1
done

count=$(find "${OUT}" -name '*.png' | wc -l)
echo "PNG count: ${count} in ${OUT}"
ls -la "${OUT}"
