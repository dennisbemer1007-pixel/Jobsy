#!/usr/bin/env bash
# Map /cursor/stores/self/e2e-flow artifacts into review folders under fixes/.
set -euo pipefail

STORE="${JOBSY_E2E_EVIDENCE_ROOT:-/cursor/stores/self/fixes}"
SRC="${JOBSY_E2E_FLOW_SRC:-/cursor/stores/self/e2e-flow}"
ART="${JOBSY_E2E_ARTIFACTS:-/opt/cursor/artifacts}"

copy_pair() {
  local dest="$1"
  local slug="$2"
  mkdir -p "$dest"
  for tag in desktop-1440 mobile-390; do
    local f="$SRC/screens/${slug}-${tag}.png"
    if [[ -f "$f" ]]; then
      cp -f "$f" "$dest/${tag}.png"
    fi
  done
}

copy_one() {
  local dest="$1"
  local path="$2"
  local name="$3"
  mkdir -p "$dest"
  if [[ -f "$path" ]]; then
    cp -f "$path" "$dest/$name"
  fi
}

mkdir -p "$STORE"

# 1 Maqqie phase-2
copy_pair "$STORE/01-maqqie-phase2-after-accept" "12-maqqie-chosen"
copy_one "$STORE/01-maqqie-phase2-after-accept" "$SRC/screens/11-accept-open-desktop-1440.png" "accept-open-desktop-1440.png"
copy_one "$STORE/01-maqqie-phase2-after-accept" "$SRC/screens/11-applicant-drawer-desktop-1440.png" "drawer-desktop-1440.png"

# 2 ESCO + dag in het leven
copy_pair "$STORE/02-esco-wizard-and-day-in-life" "05-vacancy-step-01"
copy_pair "$STORE/02-esco-wizard-and-day-in-life" "08-day-in-life"

# 3 No-tests apply path
copy_pair "$STORE/03-no-tests-apply-and-tip" "07-vacancy-match"
copy_pair "$STORE/03-no-tests-apply-and-tip" "09-applied"
copy_pair "$STORE/03-no-tests-apply-and-tip" "07-vacancy-encourage-tip"
copy_pair "$STORE/03-no-tests-apply-and-tip" "09-applied-encourage-tip"
copy_one "$STORE/03-no-tests-apply-and-tip" "$SRC/metrics.json" "metrics.json"

# 4 Empty profile blocked
copy_pair "$STORE/04-empty-profile-blocked" "09-apply-blocked-empty-profile"

# 5 Employer Lobsy CV + PDF pages
copy_one "$STORE/05-employer-lobsy-cv-and-paspoort-pdf" "$SRC/screens/11-attached-document-desktop-1440.png" "attached-document-desktop-1440.png"
copy_one "$STORE/05-employer-lobsy-cv-and-paspoort-pdf" "$SRC/screens/11-lobsy-cv-downloaded-desktop-1440.png" "lobsy-cv-downloaded-desktop-1440.png"
copy_one "$STORE/05-employer-lobsy-cv-and-paspoort-pdf" "$SRC/pdf/employer-lobsy-cv-desktop.pdf" "employer-lobsy-cv-desktop.pdf"
for f in "$SRC/pdf"/paspoort-desktop-1440-page*.png "$SRC/pdf"/paspoort-*-page*.png; do
  [[ -f "$f" ]] || continue
  cp -f "$f" "$STORE/05-employer-lobsy-cv-and-paspoort-pdf/" 2>/dev/null || true
done
copy_one "$STORE/05-employer-lobsy-cv-and-paspoort-pdf" "$SRC/pdf/paspoort-desktop-1440.pdf" "paspoort-all-tests-desktop-1440.pdf"

# 6 Paspoort inline edits
copy_pair "$STORE/06-paspoort-inline-editing" "04-paspoort-proof-experience-saved"
copy_pair "$STORE/06-paspoort-inline-editing" "04-paspoort-data-edit-personal"
copy_pair "$STORE/06-paspoort-inline-editing" "04-paspoort-data-edit-preferences"
copy_pair "$STORE/06-paspoort-inline-editing" "04-paspoort-data-edit-availability"
copy_one "$STORE/06-paspoort-inline-editing" "$SRC/evidence/paspoort-field-table.md" "paspoort-field-table.md"

# 7 Mascot hidden mobile
copy_pair "$STORE/07-mascot-no-overlap-mobile" "07-vacancy-no-coach-mobile"
copy_pair "$STORE/07-mascot-no-overlap-mobile" "13-candidate-notifications"

# 8 Banenkaart filter drawer desktop
copy_one "$STORE/08-banenkaart-filter-drawer-desktop" "$SRC/screens/06-banenkaart-filter-drawer-desktop-1440.png" "desktop-1440.png"

# 9 Notification lists
copy_pair "$STORE/09-notification-lists-dedupe" "10-employer-meldingen"
copy_pair "$STORE/09-notification-lists-dedupe" "13-candidate-notifications"
copy_one "$STORE/09-notification-lists-dedupe" "$SRC/evidence/about-me-revert.txt" "about-me-revert.txt"

# Mirror to artifacts for PR walkthrough
if [[ -d "$ART" ]]; then
  rm -rf "$ART/fixes-evidence"
  cp -a "$STORE" "$ART/fixes-evidence"
fi

echo "Organized evidence under $STORE"
