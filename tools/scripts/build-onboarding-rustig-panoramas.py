#!/usr/bin/env python3
"""Legacy entry: finalize generated raw panoramas into rustig-panorama JPEGs.

Place *-desktop-raw.jpg files in artifacts/onboarding-rustig-panorama-raw/
(generate via image model at 16:9 with bottom-weighted composition), then run:

  python3 tools/scripts/finalize-onboarding-rustig-panoramas.py
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FINALIZE = ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"


def main() -> None:
    cmd = [sys.executable, str(FINALIZE), str(RAW)]
    raise SystemExit(subprocess.call(cmd))


if __name__ == "__main__":
    main()
