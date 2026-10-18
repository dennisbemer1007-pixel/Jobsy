#!/usr/bin/env python3
"""Build mobile learn-grow assets (runs r10 builder)."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

R10 = Path(__file__).resolve().parent / "build-leren-groeien-hero-mobile-r11.py"

if __name__ == "__main__":
    raise SystemExit(subprocess.call([sys.executable, str(R10)]))
