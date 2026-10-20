#!/usr/bin/env python3
"""Side-by-side mockup vs built onboarding rustig screens."""
from __future__ import annotations

import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    print("Pillow required: pip install Pillow", file=sys.stderr)
    sys.exit(1)

ART = Path(sys.argv[1] if len(sys.argv) > 1 else "/opt/cursor/artifacts/onboarding-rustig-start")
MOCK = Path(sys.argv[2] if len(sys.argv) > 2 else "/opt/cursor/artifacts/onboarding-rustig-mockup-refs")

PAIRS = [
    ("start-desktop.png", "desktop-1440-viewport.png", "compare-desktop-start-g.png"),
    ("start-mobile.png", "mobile-390-viewport.png", "compare-mobile-start-d.png"),
    # Aliases for Dennis naming (start-g / start-d)
    ("step-01-desktop.png", "step-01-desktop-viewport.png", "compare-desktop-step-01.png"),
    ("step-01-mobile.png", "step-01-mobile-viewport.png", "compare-mobile-step-01.png"),
    ("step-02-desktop.png", "step-02-desktop-viewport.png", "compare-desktop-step-02.png"),
    ("step-02-mobile.png", "step-02-mobile-viewport.png", "compare-mobile-step-02.png"),
    ("step-03-desktop.png", "step-03-desktop-viewport.png", "compare-desktop-step-03.png"),
    ("step-03-mobile.png", "step-03-mobile-viewport.png", "compare-mobile-step-03.png"),
    ("step-04-desktop.png", "step-04-desktop-viewport.png", "compare-desktop-step-04.png"),
    ("step-04-mobile.png", "step-04-mobile-viewport.png", "compare-mobile-step-04.png"),
    ("step-05-desktop.png", "step-05-desktop-viewport.png", "compare-desktop-step-05.png"),
    ("step-05-mobile.png", "step-05-mobile-viewport.png", "compare-mobile-step-05.png"),
]


def label(img: Image.Image, text: str) -> Image.Image:
    out = img.copy()
    draw = ImageDraw.Draw(out)
    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 18)
    except OSError:
        font = ImageFont.load_default()
    pad = 8
    bbox = draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.rectangle((0, 0, tw + pad * 2, th + pad * 2), fill=(15, 45, 92, 230))
    draw.text((pad, pad), text, fill=(255, 252, 250), font=font)
    return out


def stitch(left_path: Path, right_path: Path, out_path: Path) -> None:
    if not left_path.is_file():
        print(f"skip {out_path.name}: missing mock {left_path.name}")
        return
    if not right_path.is_file():
        print(f"skip {out_path.name}: missing built {right_path.name}")
        return
    left = Image.open(left_path).convert("RGB")
    right = Image.open(right_path).convert("RGB")
    h = max(left.height, right.height)

    def pad_height(im: Image.Image) -> Image.Image:
        if im.height == h:
            return im
        canvas = Image.new("RGB", (im.width, h), (245, 242, 238))
        canvas.paste(im, (0, 0))
        return canvas

    left = pad_height(label(left, f"Mockup · {left_path.name}"))
    right = pad_height(label(right, f"Built · {right_path.name}"))
    combo = Image.new("RGB", (left.width + right.width, h))
    combo.paste(left, (0, 0))
    combo.paste(right, (left.width, 0))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    combo.save(out_path, optimize=True)
    print(f"wrote {out_path}")


def main() -> None:
    for mock_name, built_name, out_name in PAIRS:
        stitch(MOCK / mock_name, ART / built_name, ART / out_name)


if __name__ == "__main__":
    main()
