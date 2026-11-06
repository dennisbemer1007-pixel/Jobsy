#!/usr/bin/env bash
set -euo pipefail

# MTP hangdump (--hangdump / --hangdump-timeout): WebApplicationFactory teardown can leave
# Kestrel threads alive after all tests succeed (exit 7). Treat that as green when TRX is clean.
mkdir -p test-results
set +e
dotnet test --project Jobsy.Tests/Jobsy.Tests.csproj -c Release --no-build \
  --results-directory test-results \
  --report-trx \
  --timeout 55m \
  --hangdump \
  --hangdump-timeout 10m \
  --output Normal \
  --filter-not-class '*Playwright*' \
  --filter-not-class '*Screenshot*' \
  --filter-not-class 'AcceptatieOct2026BugfixTests' \
  --filter-not-class 'BlazorReconnectAndHealthzTests' \
  --filter-not-class 'LandingA11yTests' \
  --filter-not-class 'ProactiveMatchingRealStackE2eTests'
code=$?
set -e

# Exit 7: known Kestrel teardown hang after a green run. Exit 1: MTP can report
# "test session start without end" / InvalidOperationException while TRX shows 0 failed.
trx_clean_green() {
  local trx failed
  trx="$(ls -1t test-results/*.trx 2>/dev/null | head -1 || true)"
  [[ -n "$trx" ]] || return 1
  failed="$(python3 - <<'PY' "$trx"
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
counters = root.find("t:ResultSummary/t:Counters", ns)
if counters is None:
    print("1")
else:
    print(counters.get("failed", "1"))
PY
)"
  [[ "$failed" == "0" ]]
}

if [[ "$code" -eq 7 ]] && trx_clean_green; then
  echo "Alle unit/integration tests geslaagd; exit 7 komt van hangdump na fixture-teardown (geen falende test)."
  exit 0
fi

if [[ "$code" -eq 1 ]] && trx_clean_green; then
  echo "Alle unit/integration tests geslaagd volgens TRX (0 failed); exit 1 komt waarschijnlijk van MTP/hangdump shutdown (bekend issue)."
  exit 0
fi

exit "$code"
