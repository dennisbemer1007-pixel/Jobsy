#!/usr/bin/env python3
"""Regenerate Jobsy.Tests/asset-versions.json from App.razor + loader ?v= refs."""
from __future__ import annotations

import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WWW = ROOT / "Jobsy.Web" / "wwwroot"
OUT = ROOT / "Jobsy.Tests" / "asset-versions.json"

HTML_REF = re.compile(
    r"""(?i)(?:src|href)\s*=\s*["'](?!https?:|//|data:)([^"']+\?v=[^"']+)["']"""
)
JS_REF = re.compile(r"""["'](/(?:js|css|service-worker)[^"']+\?v=[^"']+)["']""")


def collect_sources() -> list[Path]:
    files = [ROOT / "Jobsy.Web" / "Components" / "App.razor"]
    files.extend((ROOT / "Jobsy.Web" / "Components").rglob("*.razor"))
    files.extend(
        [
            WWW / "js" / "app-core.js",
            WWW / "js" / "maps-loader.js",
            WWW / "js" / "lobsyPush.js",
        ]
    )
    seen: set[Path] = set()
    out: list[Path] = []
    for f in files:
        if f.exists() and f not in seen:
            seen.add(f)
            out.append(f)
    return out


def main() -> int:
    existing = json.loads(OUT.read_text()) if OUT.exists() else {}
    found: dict[str, str] = {}
    for src in collect_sources():
        text = src.read_text(encoding="utf-8")
        pattern = JS_REF if src.suffix == ".js" else HTML_REF
        for m in pattern.finditer(text):
            url = m.group(1)
            if "?v=" not in url:
                continue
            path_part, version = url.split("?v=", 1)
            path_part = path_part.lstrip("~/")
            if path_part.startswith("_framework/"):
                continue
            physical = WWW / path_part
            if not physical.is_file():
                continue
            prev = found.get(path_part)
            if prev is not None and prev != version:
                print(f"conflict: {path_part} has ?v={prev} and ?v={version}", file=sys.stderr)
                return 1
            found[path_part] = version

    # Keep previously tracked keys (e.g. loader-only) and refresh hashes.
    keys = sorted(set(existing) | set(found))
    manifest: dict[str, dict[str, str]] = {}
    for path in keys:
        physical = WWW / path
        if not physical.is_file():
            print(f"skip missing: {path}", file=sys.stderr)
            continue
        version = found.get(path) or existing[path]["v"]
        digest = hashlib.sha256(physical.read_bytes()).hexdigest()
        manifest[path] = {"sha256": digest, "v": version}

    OUT.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({len(manifest)} entries)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
