import re
import sys
from pathlib import Path
from PIL import Image

out_dir = Path(sys.argv[1])
files = sorted(out_dir.glob("*.png"))
files = [f for f in files if "contact-" not in f.name]

def screen_key(name: str) -> str:
    stem = name.replace(".png", "")
    m = re.match(r"^(.*)-(nl|en)-(1440|390|1920)$", stem)
    return m.group(1) if m else stem

groups = {}
for f in files:
    groups.setdefault(screen_key(f.name), []).append(f)

pad = 24
bg = (245, 242, 238)

for key, members in sorted(groups.items()):
    for lang in ("nl", "en"):
        desktop = next((m for m in members if f"-{lang}-1920" in m.name), None)
        if desktop is None:
            desktop = next((m for m in members if f"-{lang}-1440" in m.name), None)
        mobile = next((m for m in members if f"-{lang}-390" in m.name), None)
        if desktop is None or mobile is None:
            continue
        d = Image.open(desktop).convert("RGB")
        m = Image.open(mobile).convert("RGB")
        target_h = max(d.height, m.height)
        if d.height != target_h:
            ratio = target_h / d.height
            d = d.resize((int(d.width * ratio), target_h), Image.Resampling.LANCZOS)
        if m.height != target_h:
            ratio = target_h / m.height
            m = m.resize((int(m.width * ratio), target_h), Image.Resampling.LANCZOS)
        sheet_w = d.width + m.width + pad * 3
        sheet_h = target_h + pad * 2
        sheet = Image.new("RGB", (sheet_w, sheet_h), bg)
        sheet.paste(d, (pad, pad))
        sheet.paste(m, (pad * 2 + d.width, pad))
        out = out_dir / f"{key}-contact-{lang}.png"
        sheet.save(out, optimize=True)