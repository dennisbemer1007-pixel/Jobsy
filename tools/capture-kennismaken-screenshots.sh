#!/usr/bin/env bash
# Styled before (acceptatie live) + after (branch PersonalStep via full PageAssetCatalog CSS).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="$PATH:${HOME}/.dotnet"
STORE="${1:-/cursor/stores/self/kennismaken-adres}"
export KENNISMAKEN_STORE="$STORE"
cd "$ROOT"
node "$ROOT/tools/screenshots/capture-kennismaken-adres.mjs"
echo "Saved screenshots under $STORE"
