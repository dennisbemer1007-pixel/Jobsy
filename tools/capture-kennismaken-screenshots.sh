#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
STORE="${1:-/cursor/stores/self/kennismaken-adres}"
export PATH="$PATH:${HOME}/.dotnet"
PERSONAL="$ROOT/Jobsy.Web/Components/Candidate/Onboarding/PersonalStep.razor"
TMP_AFTER="$(mktemp)"
TMP_BEFORE="$(mktemp)"
TMP_WORK="$(mktemp)"
cp "$PERSONAL" "$TMP_AFTER"
git show origin/acceptatie:"Jobsy.Web/Components/Candidate/Onboarding/PersonalStep.razor" > "$TMP_BEFORE"

capture_variant() {
  local label="$1"
  local razor_file="$2"
  local out_dir="$STORE/$label"
  mkdir -p "$out_dir/html"
  cp "$PERSONAL" "$TMP_WORK"
  cp "$razor_file" "$PERSONAL"
  dotnet build "$ROOT/Jobsy.sln" -v q >/dev/null
  PERSONAL_STEP_HTML_DIR="$out_dir/html" dotnet test "$ROOT/Jobsy.Tests/Jobsy.Tests.csproj" \
    --filter "FullyQualifiedName~PersonalStepScreenshotHarnessTests.Export_current" --no-build -v q
  cp "$TMP_WORK" "$PERSONAL"
  for vp in "390:844:mobile" "1280:800:desktop"; do
    IFS=: read -r w h name <<< "$vp"
    npx --yes playwright@1.49.1 screenshot \
      --viewport-size="${w},${h}" \
      "file://$out_dir/html/personal-step.html" \
      "$out_dir/even-kennismaken-${name}.png"
    npx --yes playwright@1.49.1 screenshot \
      --viewport-size="${w},${h}" \
      "file://$out_dir/html/personal-step-suggestions.html" \
      "$out_dir/even-kennismaken-suggesties-${name}.png"
  done
}

capture_variant "before" "$TMP_BEFORE"
capture_variant "after" "$TMP_AFTER"
rm -f "$TMP_AFTER" "$TMP_BEFORE" "$TMP_WORK"
echo "Saved screenshots under $STORE"
