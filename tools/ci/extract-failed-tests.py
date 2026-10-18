#!/usr/bin/env python3
"""Extract failed test FQNs from a dotnet test TRX or log (one name per line)."""
from __future__ import annotations

import re
import sys
from pathlib import Path

text = Path(sys.argv[1]).read_text(encoding="utf-8", errors="ignore")
# TRX: testName="Fully.Qualified.Name"
names = set(re.findall(r'testName="([^"]+)"[^>]*outcome="Failed"', text))
if not names:
    # Console: Failed Jobsy.Tests.Something [1 ms]
    names = set(re.findall(r"^\s*Failed\s+(\S+)", text, re.M))
print("\n".join(sorted(names)))
