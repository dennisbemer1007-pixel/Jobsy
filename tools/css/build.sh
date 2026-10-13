#!/usr/bin/env bash
# Build Jobsy.Web/wwwroot/css/app.min.css from app.css via pinned lightningcss-cli.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SRC="${REPO_ROOT}/Jobsy.Web/wwwroot/css/app.css"
OUT="${REPO_ROOT}/Jobsy.Web/wwwroot/css/app.min.css"
TMP="$(mktemp)"
trap 'rm -f "${TMP}"' EXIT

cd "${SCRIPT_DIR}"
# lightningcss-cli postinstall copies the platform binary over the Windows stub.
# --ignore-scripts leaves a text stub that bash executes as "This: command not found" (exit 127).
if [[ ! -x "${SCRIPT_DIR}/node_modules/.bin/lightningcss" ]] \
  || ! "${SCRIPT_DIR}/node_modules/.bin/lightningcss" --help >/dev/null 2>&1; then
  npm ci
fi

"${SCRIPT_DIR}/node_modules/.bin/lightningcss" --minify "${SRC}" -o "${TMP}"

# Normalize to a single trailing newline for stable CI diffs.
python3 - "${TMP}" "${OUT}" <<'PY'
import sys
from pathlib import Path
src, dest = Path(sys.argv[1]), Path(sys.argv[2])
text = src.read_text(encoding="utf-8")
if not text.endswith("\n"):
    text += "\n"
dest.write_text(text, encoding="utf-8")
print(f"wrote {dest} ({dest.stat().st_size} bytes)")
PY
