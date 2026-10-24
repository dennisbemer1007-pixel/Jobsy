#!/usr/bin/env python3
"""Build mockup | app side-by-side PNG (same layout as compare harness)."""
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "-q", "pillow"])
    from PIL import Image


def side_by_side(mockup: Path, app: Path, out: Path) -> None:
    if not mockup.is_file() or not app.is_file():
        print(f"skip side-by-side missing: mock={mockup.exists()} app={app.exists()}", file=sys.stderr)
        return
    a = Image.open(mockup).convert("RGBA")
    b = Image.open(app).convert("RGBA")
    h = max(a.height, b.height)

    def stretch(im: Image.Image) -> Image.Image:
        w = max(1, int(im.width * (h / im.height)))
        return im.resize((w, h), Image.Resampling.LANCZOS)

    a, b = stretch(a), stretch(b)
    gap = 8
    label_h = 28
    canvas = Image.new("RGBA", (a.width + gap + b.width, h + label_h), (250, 246, 239, 255))
    canvas.paste(a, (0, label_h))
    canvas.paste(b, (a.width + gap, label_h))
    out.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(out, "PNG")


if __name__ == "__main__":
    side_by_side(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]))
