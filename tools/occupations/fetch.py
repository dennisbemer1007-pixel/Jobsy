#!/usr/bin/env python3
"""Download pinned occupation-catalogue inputs into tools/occupations/.cache/.

Stdlib only. Fails loudly when a pinned sha256 does not match, and prints the
hash that was actually downloaded so a human can decide.
"""

from __future__ import annotations

import hashlib
import json
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent
CACHE = ROOT / ".cache"
SOURCES = ROOT / "sources.json"
USER_AGENT = "LobsyOccupationCatalog/1.0 (build tooling; contact via the Jobsy repo)"
ESCO_PAGES = 31


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def download(url: str, dest: Path, attempts: int = 5) -> None:
    dest.parent.mkdir(parents=True, exist_ok=True)
    tmp = dest.with_suffix(dest.suffix + ".part")
    last_error: Exception | None = None
    for attempt in range(attempts):
        try:
            request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT, "Accept": "*/*"})
            with urllib.request.urlopen(request, timeout=120) as response:
                data = response.read()
            tmp.write_bytes(data)
            tmp.replace(dest)
            return
        except (urllib.error.URLError, TimeoutError, OSError) as exc:
            last_error = exc
            wait = 2 ** attempt
            print(f"retry {attempt + 1}/{attempts} for {url}: {exc} (sleep {wait}s)", file=sys.stderr)
            time.sleep(wait)
    raise SystemExit(f"download failed: {url}: {last_error}")


def require_hash(path: Path, expected: str | None) -> str:
    actual = sha256_file(path)
    if expected and actual != expected:
        print(f"sha256 mismatch for {path.name}", file=sys.stderr)
        print(f"  expected {expected}", file=sys.stderr)
        print(f"  actual   {actual}", file=sys.stderr)
        raise SystemExit(2)
    return actual


def fetch_file(item: dict) -> None:
    name = {
        "esco-onet-crosswalk": "esco_onet_crosswalk.csv",
        "onet-31.0-career-interest-types": "career_interest_types.csv",
        "onet-31.0-occupation-data": "occupation_data.csv",
        "cbs-brc-2014-ed2025": "brc2014.xlsx",
    }[item["id"]]
    dest = CACHE / name
    print(f"fetch {item['id']}")
    download(item["url"], dest)
    digest = require_hash(dest, item.get("sha256"))
    print(f"  {name} sha256 {digest}")


def fetch_esco(item: dict) -> None:
    esco_dir = CACHE / "esco"
    esco_dir.mkdir(parents=True, exist_ok=True)
    url_template = item["url"]
    print("fetch esco v1.2.1 nl (1 request/second)")
    for page in range(ESCO_PAGES):
        dest = esco_dir / f"page_{page:02d}.json"
        url = url_template.format(page=page)
        download(url, dest)
        payload = json.loads(dest.read_text(encoding="utf-8"))
        total = payload.get("total")
        if total != 3039:
            raise SystemExit(f"ESCO page {page} total={total}, expected 3039")
        results = (payload.get("_embedded") or {}).get("results") or []
        if page < ESCO_PAGES - 1 and len(results) != 100:
            raise SystemExit(f"ESCO page {page} has {len(results)} results, expected 100")
        if page == ESCO_PAGES - 1 and len(results) != 39:
            raise SystemExit(f"ESCO last page has {len(results)} results, expected 39")
        print(f"  page {page:02d} ok ({len(results)})")
        if page < ESCO_PAGES - 1:
            time.sleep(1)


def main() -> None:
    sources = json.loads(SOURCES.read_text(encoding="utf-8"))
    CACHE.mkdir(parents=True, exist_ok=True)
    for item in sources["inputs"]:
        if item["id"] == "esco-v1.2.1-nl":
            fetch_esco(item)
        else:
            fetch_file(item)
    print("fetch complete")


if __name__ == "__main__":
    main()
