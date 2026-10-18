#!/usr/bin/env python3
"""Compare unit-test TRX failure sets: PR run vs acceptatie baseline (39130960671)."""
from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def failed_names(trx: Path) -> set[str]:
    root = ET.parse(trx).getroot()
    return {
        r.get("testName", "")
        for r in root.findall(".//t:UnitTestResult[@outcome='Failed']", NS)
        if r.get("testName")
    }


def defined_names(trx: Path) -> set[str]:
    root = ET.parse(trx).getroot()
    return {
        u.get("name", "")
        for u in root.findall(".//t:UnitTest", NS)
        if u.get("name")
    }


def counters(trx: Path) -> dict[str, str]:
    c = ET.parse(trx).getroot().find("t:ResultSummary/t:Counters", NS)
    return dict(c.attrib) if c is not None else {}


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: compare-failures-with-acceptatie.py <pr.trx> <acceptatie.trx>", file=sys.stderr)
        return 2
    pr_path = Path(sys.argv[1])
    acc_path = Path(sys.argv[2])
    pr = failed_names(pr_path)
    acc = failed_names(acc_path)
    acc_defs = defined_names(acc_path)
    # Only regressions vs acceptatie catalog: tests that existed on acceptatie CI but fail newly on PR.
    new = sorted((pr - acc) & acc_defs)
    pr_only = sorted(pr - acc_defs)
    print("PR failed:", len(pr), "Acceptatie failed:", len(acc))
    print("failed(PR) ⊆ failed(acceptatie):", (pr & acc_defs).issubset(acc))
    print("NEW on PR (in acceptatie catalog, not failed there):", len(new))
    for n in new:
        print(" +", n)
    if pr_only:
        print("PR-only tests (not in acceptatie TRX catalog):", len(pr_only))
        for n in pr_only[:10]:
            print(" ?", n)
        if len(pr_only) > 10:
            print(" ?", f"... and {len(pr_only) - 10} more")
    return 0 if (pr & acc_defs).issubset(acc) else 1


if __name__ == "__main__":
    raise SystemExit(main())
