#!/usr/bin/env bash
# Count unique compiler/analyzer/package warnings from a `dotnet build` log.
# Key = relativePath(line,col):CODE  or  package:NUxxxx:message
# Also prints a per-code summary on stderr (code count, sorted) when COUNT_WARNINGS_SUMMARY=1
# or when --summary is passed.
set -euo pipefail

summary=0
log_file=""
for arg in "$@"; do
  case "$arg" in
    --summary) summary=1 ;;
    *) log_file="$arg" ;;
  esac
done

if [[ -z "$log_file" || ! -f "$log_file" ]]; then
  echo "usage: $0 [--summary] <build-log>" >&2
  exit 2
fi

COUNT_WARNINGS_SUMMARY="${COUNT_WARNINGS_SUMMARY:-$summary}" \
python3 - "$log_file" <<'PY'
import os
import re
import subprocess
import sys
from collections import Counter
from pathlib import Path

text = Path(sys.argv[1]).read_text(errors="replace")

# Prefer git toplevel; fall back to GITHUB_WORKSPACE / CWD.
roots: list[str] = []
try:
    top = subprocess.check_output(
        ["git", "rev-parse", "--show-toplevel"], text=True, stderr=subprocess.DEVNULL
    ).strip()
    if top:
        roots.append(top.rstrip("/") + "/")
except Exception:
    pass
gw = os.environ.get("GITHUB_WORKSPACE")
if gw:
    roots.append(gw.rstrip("/") + "/")
roots.append("/workspace/")

pat = re.compile(
    r"(?P<path>[^\s:(]+\.(?:cs|csproj|razor|cshtml))"
    r"\((?P<line>\d+),(?P<col>\d+)\): warning "
    r"(?P<code>(?:CS|CA|IDE|SYSLIB|RZ|ASP|BL)\d+)"
)
keys: set[str] = set()
codes: Counter[str] = Counter()
for m in pat.finditer(text):
    path = m.group("path").replace("\\", "/")
    for root in roots:
        if path.startswith(root):
            path = path[len(root) :]
            break
    if path.startswith("./"):
        path = path[2:]
    key = f"{path}({m.group('line')},{m.group('col')}):{m.group('code')}"
    if key in keys:
        continue
    keys.add(key)
    codes[m.group("code")] += 1

for line in text.splitlines():
    if ": warning NU" not in line:
        continue
    m = re.search(r"warning (NU\d+): ([^\[]+)", line)
    if m:
        # NuGet audit (NU190x) is warning-only in Directory.Build.props; vulnerable-package gate is separate.
        if m.group(1).startswith("NU19"):
            continue
        key = f"package:{m.group(1)}:{m.group(2).strip()}"
        if key in keys:
            continue
        keys.add(key)
        codes[m.group(1)] += 1

print(len(keys))
if os.environ.get("COUNT_WARNINGS_SUMMARY") in ("1", "true", "yes"):
    for code, n in sorted(codes.items(), key=lambda x: (-x[1], x[0])):
        print(f"{code} {n}", file=sys.stderr)
PY
