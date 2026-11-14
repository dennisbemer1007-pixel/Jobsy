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


def pass_fail(value: str) -> tuple[str, str]:
    if value.startswith("n/a"):
        note = "" if value == "n/a" else value
        return "PASS", note
    if value in ("ok", "—", ""):
        if value == "ok":
            return "PASS", ""
        return "—", ""
    short = value.split("\n", 1)[0].strip()
    if len(short) > 56:
        short = short[:53] + "..."
    return "FAIL", short


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
    elif name.startswith("click-audit-kompas"):
        step = "10"
        m7 = re.search(r"click-audit-kompas-[^-]+-(\d{3,4})", name)
        if m7:
            vp = m7.group(1)
    elif name.startswith("click-audit-step-00") or name.startswith("click-audit-rustig"):
        step = "00"
    m4 = re.search(r"click-audit-step-(\d{2})-[^-]+-(\d{3,4})", name)
    if m4:
        step, vp = m4.group(1), m4.group(2)
    m5 = re.search(r"click-audit-step-00-[^-]+-(\d{3,4})", name)
    if m5:
        step, vp = "00", m5.group(1)
    m6 = re.search(r"click-audit-rustig-(\d{3,4})", name)
    if m6 and step == "00":
        vp = m6.group(1)
    return step, vp, rows


def main() -> int:
    art = Path(sys.argv[1] if len(sys.argv) > 1 else "/opt/cursor/artifacts/onboarding-v3-click-audit")
    if not art.is_dir():
        print(f"Missing artifacts dir: {art}", file=sys.stderr)
        return 1

    reports = sorted(p for p in art.glob("click-audit*.txt") if "step-" in p.name)
    entries: list[tuple[str, str, dict[str, str]]] = []
    for p in reports:
        step, vp, rows = parse_report(p)
        entries.append((step, vp, rows))

    entries.sort(key=lambda t: (int(t[0]), int(t[1])))

    lines = [
        "# Onboarding v3 click audit (stap 00–10)",
        "",
        "PASS = ok of toegestaan n/a. FAIL = timeout/hit/navigatie.",
        "",
        "| Stap | VP | Header | Coach | Coach open | Keyboard | Volgende | Terug | Hydration | Notitie |",
        "|------|-----|--------|-------|------------|----------|----------|-------|-----------|---------|",
    ]

    for step, vp, rows in entries:
        header_v = pick(rows, "Header help", "Header account")
        coach_v = pick(rows, "Coach medallion")
        coach_open_v = pick(rows, "Coach open/dicht")
        keyboard_v = pick(rows, "Keyboard (Volgende focus)")
        volgende_v = pick(rows, "Volgende", "Aan de slag", "Beginnen", "Test volgende")
        terug_v = pick(rows, "Terug", "Terug (disabled)", "Test terug")
        hydration_v = pick(rows, "Hydration (circuit)")

        cols = [
            ("Header", header_v),
            ("Coach", coach_v),
            ("Coach open", coach_open_v),
            ("Keyboard", keyboard_v),
            ("Volgende", volgende_v),
            ("Terug", terug_v),
            ("Hydration", hydration_v),
        ]
        rendered = [pass_fail(v) for _, v in cols]
        notes = [f"{name}: {note}" for (name, _), (_, note) in zip(cols, rendered) if note and _ == "FAIL"]
        note_cell = "; ".join(notes) if notes else ""

        lines.append(
            "| {step} | {vp} | {h} | {c} | {co} | {k} | {v} | {t} | {hy} | {note} |".format(
                step=step,
                vp=vp,
                h=rendered[0][0],
                c=rendered[1][0],
                co=rendered[2][0],
                k=rendered[3][0],
                v=rendered[4][0],
                t=rendered[5][0],
                hy=rendered[6][0],
                note=note_cell or "—",
            )
        )

    lines.extend(["", "## Screenshots", ""])
    for png in sorted(art.glob("step-*-viewport.png")):
        lines.append(f"- [`{png.name}`]({png.resolve()})")

    out = art / "CLICK-AUDIT-SUMMARY.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {out} ({len(entries)} reports)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())