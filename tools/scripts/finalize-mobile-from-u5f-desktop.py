#!/usr/bin/env python3
"""Build 4:1 mobile hero JPEGs from approved u5f desktop panoramas (no raw regen)."""

from __future__ import annotations

import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PAN = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
U5F = "e4517bb44"

_fin_path = ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
_spec = importlib.util.spec_from_file_location("fin", _fin_path)
fin = importlib.util.module_from_spec(_spec)
assert _spec.loader
_spec.loader.exec_module(fin)


def main() -> None:
    import subprocess

    fin.MOBILE = (780, 184)
    fin.MOBILE_RATIO = 4.0
    for slug in fin.SLUGS:
        desk_path = PAN / f"{slug}-desktop.jpg"
        git_path = f"Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/{slug}-desktop.jpg"
        subprocess.run(
            ["git", "show", f"{U5F}:{git_path}"],
            check=True,
            stdout=open(desk_path, "wb"),
        )
        desk = fin.Image.open(desk_path).convert("RGB")
        mob = fin.apply_cream_fades(fin.fit_mobile(desk))
        mob.save(PAN / f"{slug}-mobile.jpg", quality=92, optimize=True)
        bb = fin.figure_bbox(mob) or fin.content_bbox(mob)
        print(f"{slug}: desk {desk.size} -> mob {mob.size} figure {bb}")


if __name__ == "__main__":
    main()
