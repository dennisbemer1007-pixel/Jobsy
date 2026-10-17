#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
STORE="${LIVEGANG_STORE:-/cursor/stores/self/livegang-fixes}"
CONN="${ConnectionStrings__JobsyDb:-Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres}"
export PATH="${HOME}/.dotnet:${PATH}"
export JOBSY_E2E_BASE_URL="${JOBSY_E2E_BASE_URL:-http://127.0.0.1:5201}"
export ConnectionStrings__JobsyDb="$CONN"
export LIVEGANG_SHOTS=1

mkdir -p "$STORE/before" "$STORE/after" "$STORE/compare"

stop_stack() {
  for f in /tmp/jobsy-ci-logs/api.pid /tmp/jobsy-ci-logs/web.pid; do
    if [[ -f "$f" ]]; then
      kill "$(cat "$f")" 2>/dev/null || true
    fi
  done
  sleep 2
}

run_capture() {
  local phase="$1"
  local dir="$2"
  export LIVEGANG_PHASE="$phase"
  export LIVEGANG_SHOTS_DIR="$dir"
  dotnet build "$ROOT/Jobsy.Tests/Jobsy.Tests.csproj" -c Release -v q
  dotnet test "$ROOT/Jobsy.Tests/Jobsy.Tests.csproj" -c Release --no-build \
    --filter "FullyQualifiedName~LivegangScreenshotCaptureTests.Capture_livegang_evidence_when_requested"
}

enable_employers() {
  sudo -u postgres psql -d JobsyCi -c \
    'UPDATE "PlatformFeatureSettings" SET "EmployersEnabled" = true WHERE "EmployersEnabled" = false;' \
    >/dev/null 2>&1 || true
}

build_and_start() {
  local repo="$1"
  cd "$repo"
  dotnet build Jobsy.sln -c Release -v q
  stop_stack
  chmod +x .github/scripts/start-ci-stack.sh
  .github/scripts/start-ci-stack.sh
  enable_employers
  sleep 8
}

WORKTREE="/tmp/acceptatie-before"
if ! git -C "$ROOT" worktree list --porcelain | grep -q "worktree $WORKTREE"; then
  git -C "$ROOT" worktree add "$WORKTREE" acceptatie
fi

echo "=== BEFORE (acceptatie) ==="
build_and_start "$WORKTREE"
run_capture before "$STORE/before"
stop_stack

echo "=== AFTER (feature branch) ==="
build_and_start "$ROOT"
run_capture after "$STORE/after"
stop_stack

echo "=== Compare PNGs ==="
python3 <<'PY'
import os
from pathlib import Path
try:
    from PIL import Image
except ImportError:
    import subprocess
    subprocess.check_call(["pip", "install", "-q", "pillow"])
    from PIL import Image

store = Path(os.environ.get("LIVEGANG_STORE", "/cursor/stores/self/livegang-fixes"))
before = store / "before"
after = store / "after"
out = store / "compare"
out.mkdir(parents=True, exist_ok=True)

for bf in sorted(before.glob("*.png")):
    af = after / bf.name
    if not af.exists():
        continue
    im_b = Image.open(bf).convert("RGB")
    im_a = Image.open(af).convert("RGB")
    h = max(im_b.height, im_a.height)
    def pad(im):
        canvas = Image.new("RGB", (im.width, h), (245, 242, 238))
        canvas.paste(im, (0, 0))
        return canvas
    im_b, im_a = pad(im_b), pad(im_a)
    combined = Image.new("RGB", (im_b.width + im_a.width + 8, h), (255, 255, 255))
    combined.paste(im_b, (0, 0))
    combined.paste(im_a, (im_b.width + 8, 0))
    combined.save(out / bf.name)
    print("compare", bf.name)
PY

echo "Done. Artifacts in $STORE"
