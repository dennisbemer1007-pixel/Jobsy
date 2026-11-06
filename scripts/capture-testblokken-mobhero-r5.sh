#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="/opt/cursor/artifacts/testblokken-mobhero-r5"
SESSION_NAME="jobsy-web-capture-mobhero-r5"

cleanup() {
  tmux -f /exec-daemon/tmux.portal.conf kill-session -t "=$SESSION_NAME" 2>/dev/null || true
}
trap cleanup EXIT

mkdir -p "${OUT}"

python3 "${ROOT}/scripts/generate-test-block-hero-figures.py"

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5201
export Jobsy__CaptureMode=true
export E2E__SkipHttpsRedirect=true
export JOBSY_CAPTURE_OUT="${OUT}"
export JOBSY_CAPTURE_MOBILE_ONLY=1

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

for stem in quick-scan-hoe-je-werkt quick-scan-leuk quick-scan-sfeer quick-scan-waarden verdieping-leuk diepte-competenties; do
  echo "=== ${stem} (390) ==="
  JOBSY_TESTBLOKKEN_STEM="$stem" dotnet run --project "${ROOT}/Jobsy.Tools.Capture/Jobsy.Tools.Capture.csproj" --no-build
  sleep 1
done

echo "=== quick-scan-hoe-je-werkt (1440 desktop check) ==="
unset JOBSY_CAPTURE_MOBILE_ONLY
export JOBSY_CAPTURE_DESKTOP_ONLY=1
JOBSY_TESTBLOKKEN_STEM="quick-scan-hoe-je-werkt" dotnet run --project "${ROOT}/Jobsy.Tools.Capture/Jobsy.Tools.Capture.csproj" --no-build
sleep 1

count=$(find "${OUT}" -name '*.png' | wc -l)
echo "PNG count: ${count}"
ls -la "${OUT}"
