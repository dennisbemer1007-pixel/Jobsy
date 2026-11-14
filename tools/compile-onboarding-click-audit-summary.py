#!/usr/bin/env python3
"""Compile click-audit *.txt reports into CLICK-AUDIT-SUMMARY.md."""
from __future__ import annotations

import re
import sys
from pathlib import Path


def pick(rows: dict[str, str], *keys: str) -> str:
    for k in keys:
        if k in rows:
            return rows[k]
    for k in keys:
        for rk, rv in rows.items():
            if k.lower() in rk.lower():
                return rv
    return "—"


def parse_report(path: Path) -> tuple[str, str, dict[str, str]]:
    step = "?"
    vp = "?"
    rows: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("mode="):
            m = re.search(r"viewport=(\S+)", line)
            if m:
                vp = m.group(1)
            continue
        if "\t" in line:
            k, v = line.split("\t", 1)
            rows[k.strip()] = v.strip()
    name = path.name
    m = re.search(r"step-(\d{2})", name)
    if m:
        step = m.group(1)
    elif name.startswith("click-audit-step-00"):
        step = "00"
    elif name.startswith("click-audit-rustig-") or "audit-rustig" in name:
        step = "00"
    m2 = re.search(r"step-(\d{2})-", name)
    if m2:
        step = m2.group(1)
    m3 = re.search(r"-(\d{3,4})\.txt", name)
    if m3 and vp == "?":
        vp = m3.group(1)
  # filename patterns: click-audit-step-01-rustig-1440.txt
    m4 = re.search(r"click-audit-step-(\d{2})-[^-]+-(\d{3,4})", name)
    if m4:
        step, vp = m4.group(1), m4.group(2)
    m5 = re.search(r"click-audit-step-00-[^-]+-(\d{3,4})", name)
    if m5:
        step, vp = "00", m5.group(1)
    return step, vp, rows


def main() -> int:
    art = Path(sys.argv[1] if len(sys.argv) > 1 else "/opt/cursor/artifacts/onboarding-v3-click-audit")
    if not art.is_dir():
        print(f"Missing artifacts dir: {art}", file=sys.stderr)
        return 1

    reports = sorted(art.glob("click-audit*.txt"))
    entries: list[tuple[str, str, dict[str, str]]] = []
    for p in reports:
        step, vp, rows = parse_report(p)
        entries.append((step, vp, rows))

    entries.sort(key=lambda t: (int(t[0]), int(t[1])))

    lines = [
        "# Onboarding v3 click audit (stap 00–10)",
        "",
        "Base: lokale CI-stack / acceptatie-rustig. Kolommen: header (help+account), coach, keyboard (Volgende focus), Volgende, Terug, hydration.",
        "",
        "| Stap | VP | Header | Coach | Keyboard | Volgende | Terug | Hydration |",
        "|------|-----|--------|-------|----------|----------|-------|-----------|",
    ]

    def status(*parts: str) -> str:
        vals = [p for p in parts if p and p != "—"]
        if not vals:
            return "—"
        if all(v in ("ok", "n/a") for v in vals):
            return "ok" if any(v == "ok" for v in vals) else "n/a"
        bad = [v for v in vals if v not in ("ok", "n/a")]
        return bad[0][:40] if bad else "ok"

    for step, vp, rows in entries:
        header = status(pick(rows, "Header help", "Header account"))
        coach = pick(rows, "Coach medallion")
        keyboard = pick(rows, "Keyboard (Volgende focus)")
        volgende = status(pick(rows, "Volgende", "Aan de slag", "Beginnen", "Test volgende"))
        terug = status(pick(rows, "Terug", "Terug (disabled)", "Test terug"))
        hydration = pick(rows, "Hydration (circuit)")
        lines.append(
            f"| {step} | {vp} | {header} | {coach} | {keyboard} | {volgende} | {terug} | {hydration} |"
        )

    lines.extend(
        [
            "",
            "## Screenshots",
            "",
        ]
    )
    for png in sorted(art.glob("step-*-viewport.png")):
        lines.append(f"- `{png.name}`")

    out = art / "CLICK-AUDIT-SUMMARY.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {out} ({len(entries)} reports)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
