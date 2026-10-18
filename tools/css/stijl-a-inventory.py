#!/usr/bin/env python3
"""Rough CSS inventory for Stijl A before/after counts (Jobsy.Web/wwwroot/css)."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Jobsy.Web" / "wwwroot" / "css"
GLOBS = ("**/*.css",)


def canonical_radius_tokens(value: str) -> set[str]:
    value = value.strip().lower()
    found: set[str] = set()
    if value == "0":
        return {"0"}
    if "50%" in value:
        found.add("50%")
    if "radius-pill" in value or value in ("999px", "99px"):
        found.add("pill")
    for token in ("var(--r-sm)", "var(--r-md)", "var(--r-lg)"):
        if token in value:
            found.add(token)
    if not found and value:
        found.add(value)
    return found


def scan() -> dict:
    files = [p for pat in GLOBS for p in ROOT.glob(pat) if "lib/" not in str(p)]
    text = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in files)
    radii = re.findall(r"border-radius\s*:\s*([^;}{]+)", text, re.I)
    shadows = re.findall(r"box-shadow\s*:\s*([^;}{]+)", text, re.I)
    gradients = re.findall(r"linear-gradient\s*\(", text, re.I)
    emoji = re.findall(r"[\U0001F300-\U0001FAFF]", text)
    btn_rules = len(re.findall(r"\.btn[^{]*\{", text))
    design_radii: set[str] = set()
    for r in radii:
        design_radii |= canonical_radius_tokens(r.strip())
    shadows_distinct = sorted(
        {s.strip() for s in shadows if "none" not in s.lower()}
    )
    return {
        "files": len(files),
        "border_radius_declarations": len(radii),
        "distinct_radii": len(set(r.strip() for r in radii)),
        "design_radius_tokens": len(design_radii),
        "design_radius_token_list": sorted(design_radii),
        "distinct_non_none_shadow_values": len(shadows_distinct),
        "shadow_value_list": shadows_distinct[:20],
        "box_shadow_declarations": len(shadows),
        "non_none_shadows": sum(1 for s in shadows if "none" not in s.lower()),
        "linear_gradients": len(gradients),
        "emoji_in_css": len(emoji),
        "btn_rule_blocks": btn_rules,
    }


if __name__ == "__main__":
    print(json.dumps(scan(), indent=2))
