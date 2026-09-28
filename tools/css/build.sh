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
if [[ ! -x "${SCRIPT_DIR}/node_modules/.bin/lightningcss" ]]; then
  npm ci --ignore-scripts
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
