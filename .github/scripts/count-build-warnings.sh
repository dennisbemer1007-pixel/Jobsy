#!/usr/bin/env bash
# Count unique compiler/analyzer/package warnings from a `dotnet build` log.
# Key = relativePath(line,col):CODE  or  package:NUxxxx:message
set -euo pipefail

log_file="${1:-}"
if [[ -z "$log_file" || ! -f "$log_file" ]]; then
  echo "usage: $0 <build-log>" >&2
  exit 2
fi

python3 - "$log_file" <<'PY'
import re
import sys
from pathlib import Path

text = Path(sys.argv[1]).read_text(errors="replace")
pat = re.compile(
    r"(?P<path>(?:/workspace/)?[^\s:(]+\.(?:cs|csproj|razor))"
    r"\((?P<line>\d+),(?P<col>\d+)\): warning "
    r"(?P<code>(?:CS|CA|IDE|SYSLIB)\d+)"
)
keys: set[str] = set()
for m in pat.finditer(text):
    path = m.group("path").replace("\\", "/")
    if path.startswith("/workspace/"):
        path = path[len("/workspace/") :]
    # Strip leading ./ 
    if path.startswith("./"):
        path = path[2:]
    keys.add(f"{path}({m.group('line')},{m.group('col')}):{m.group('code')}")

for line in text.splitlines():
    if ": warning NU" not in line:
        continue
    m = re.search(r"warning (NU\d+): ([^\[]+)", line)
    if m:
        keys.add(f"package:{m.group(1)}:{m.group(2).strip()}")

print(len(keys))
PY
