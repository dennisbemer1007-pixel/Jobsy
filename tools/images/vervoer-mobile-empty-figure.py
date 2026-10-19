#!/usr/bin/env python3
from pathlib import Path
from PIL import Image

out = Path(__file__).resolve().parents[2] / "Jobsy.Web/wwwroot/images/onboarding"
img = Image.new("RGBA", (2, 2), (0, 0, 0, 0))
img.save(out / "vervoer-mobile.png", optimize=True)
img.save(out / "vervoer-mobile.webp", format="WEBP", quality=90, method=6)
print("vervoer-mobile: empty figure overlay (bg carries scene)")
