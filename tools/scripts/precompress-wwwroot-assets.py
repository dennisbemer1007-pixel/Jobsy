#!/usr/bin/env python3
"""Write .br (quality 11) and .gz sidecars next to large static JS/CSS assets.

Run after regenerating jobMap.min.js (terser). PrecompressedStaticAssetTests fails if
sidecars do not decompress to the same bytes as the source file.
"""
from __future__ import annotations

import brotli
import gzip
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULTS = [
    ROOT / "Jobsy.Web/wwwroot/lib/maplibre/maplibre-gl-csp.js",
    ROOT / "Jobsy.Web/wwwroot/lib/maplibre/maplibre-gl-csp-worker.js",
    ROOT / "Jobsy.Web/wwwroot/js/jobMap.min.js",
    ROOT / "Jobsy.Web/wwwroot/css/features/sollicitaties-page.css",
    ROOT / "Jobsy.Web/wwwroot/css/features/applications.css",
]


def compress_file(path: Path) -> None:
    data = path.read_bytes()
    br = brotli.compress(data, quality=11)
    gz = gzip.compress(data, compresslevel=9)
    path.with_suffix(path.suffix + ".br").write_bytes(br)
    path.with_suffix(path.suffix + ".gz").write_bytes(gz)
    if len(br) >= len(gz):
        raise SystemExit(f"{path.name}: brotli ({len(br)}) must be smaller than gzip ({len(gz)})")


def main(argv: list[str]) -> int:
    paths = [Path(p) for p in argv[1:]] if len(argv) > 1 else DEFAULTS
    for path in paths:
        if not path.is_file():
            print(f"skip missing {path}", file=sys.stderr)
            continue
        compress_file(path)
        print(f"ok {path.name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
