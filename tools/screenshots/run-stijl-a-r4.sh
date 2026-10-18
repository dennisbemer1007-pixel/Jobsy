#!/usr/bin/env bash
# Stijl A r4 matrix: PR branch "after" + acceptatie worktree "before" + pairs.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${STIJL_A_OUT:-/opt/cursor/artifacts/stijl-A-r5}"
WORKTREE="${STIJL_A_ACC_WORKTREE:-/tmp/jobsy-acceptatie-stijl-a-r4}"

export STIJL_A_OUT="$OUT"

echo "Capturing after (PR branch)…"
cd "$ROOT"
STIJL_A_SUBDIR=after dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~StijlAScreenshotMatrixTests.Capture_stijl_a_matrix" --no-restore 2>/dev/null || \
  dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~StijlAScreenshotMatrixTests.Capture_stijl_a_matrix"

git fetch origin acceptatie
if [[ ! -d "$WORKTREE/.git" ]]; then
  git worktree add "$WORKTREE" origin/acceptatie
fi

STUB_FILES=(
  Jobsy.Tests/IllustratedPagesApiStub.cs
  Jobsy.Tests/IllustratedPagesWebFactory.cs
  Jobsy.Tests/IllustratedPagesVacancyStub.cs
  Jobsy.Tests/StijlAScreenshotMatrixTests.cs
  Jobsy.Tests/PassportDeepOnboardingScreenshotTests.cs
)

echo "Syncing illustrated stub tests into acceptatie worktree…"
for f in "${STUB_FILES[@]}"; do
  cp "$ROOT/$f" "$WORKTREE/$f"
done

echo "Capturing before (acceptatie styling)…"
cd "$WORKTREE"
STIJL_A_OUT="$OUT" STIJL_A_SUBDIR=before STIJL_A_LOOSE=1 dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~StijlAScreenshotMatrixTests.Capture_stijl_a_matrix"

cd "$ROOT"
STIJL_A_OUT="$OUT" dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~StijlAScreenshotMatrixTests.Build_stijl_a_pairs" --no-build 2>/dev/null || \
  dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~StijlAScreenshotMatrixTests.Build_stijl_a_pairs"

echo "Done. Pairs in $OUT/pairs"
