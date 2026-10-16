#!/usr/bin/env bash
# Before: acceptatie.lobsy.nl (legacy postcode UI). After: local stack (API :5200 + Web :5201).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="$PATH:${HOME}/.dotnet"
STORE="${1:-/cursor/stores/self/kennismaken-adres}"
export KENNISMAKEN_STORE="$STORE"
cd "$ROOT"
node "$ROOT/tools/screenshots/capture-kennismaken-adres.mjs"
echo "Saved screenshots under $STORE"
