#!/usr/bin/env python3
"""Place mockup (left) and built screenshot (right) in one PNG; report pixel diff %."""
from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


def align_to_mock(mock: Image.Image, built: Image.Image) -> Image.Image:
    """Scale built to mock width, crop/pad to mock height (top-aligned)."""
    mock_rgb = mock.convert("RGB")
    built_rgb = built.convert("RGB")
    if built_rgb.width != mock_rgb.width:
        scale = mock_rgb.width / built_rgb.width
        new_h = max(1, int(built_rgb.height * scale))
        built_rgb = built_rgb.resize((mock_rgb.width, new_h), Image.Resampling.LANCZOS)
    if built_rgb.height >= mock_rgb.height:
        return built_rgb.crop((0, 0, mock_rgb.width, mock_rgb.height))
    canvas = Image.new("RGB", mock_rgb.size, (245, 245, 245))
    canvas.paste(built_rgb, (0, 0))
    return canvas


def diff_percent(mock: Image.Image, built: Image.Image) -> float:
    built_fit = align_to_mock(mock, built)
    mock_rgb = mock.convert("RGB")
    diff = ImageChops.difference(mock_rgb, built_fit)
    stat = ImageStat.Stat(diff)
    mean = sum(stat.mean) / len(stat.mean)
    return round(mean / 255.0 * 100.0, 2)


def main() -> int:
    if len(sys.argv) not in (4, 5):
        print(
            "usage: compare_mockup_side_by_side.py <mockup.png> <built.png> <out.png> [metrics.json]",
            file=sys.stderr,
        )
        return 2
    mock_path, built_path, out_path = map(Path, sys.argv[1:4])
    metrics_path = Path(sys.argv[4]) if len(sys.argv) == 5 else out_path.with_suffix(".metrics.json")
    mock = Image.open(mock_path).convert("RGB")
    built = Image.open(built_path).convert("RGB")
    pct = diff_percent(mock, built)
    built_side = align_to_mock(mock, built)
    gap = 20
    canvas = Image.new("RGB", (mock.width + gap + built_side.width, mock.height), (245, 245, 245))
    canvas.paste(mock, (0, 0))
    canvas.paste(built_side, (mock.width + gap, 0))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(out_path)
    metrics_path.write_text(json.dumps({"diff_pct": pct}, indent=2) + "\n", encoding="utf-8")
    print(pct)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
