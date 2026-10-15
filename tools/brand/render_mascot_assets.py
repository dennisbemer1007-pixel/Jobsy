#!/usr/bin/env python3
"""Render raster mascot + PWA icons from the inline SVG art (avatar crop, navy tile)."""
from __future__ import annotations

import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WWW = ROOT / "Jobsy.Web" / "wwwroot"

CORAL = (0xF5, 0x4A, 0x1B)
CORAL_D = (0xD9, 0x3A, 0x12)
INK = (0x0F, 0x2D, 0x5C)
NAVY = (0x0A, 0x20, 0x44)


def write_png(path: Path, width: int, height: int, rgba_fn) -> None:
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        for x in range(width):
            raw.extend(rgba_fn(x, y))

    def chunk(tag: bytes, data: bytes) -> bytes:
        crc = zlib.crc32(tag + data) & 0xFFFFFFFF
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", crc)

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(png)


def in_ellipse(x: float, y: float, cx: float, cy: float, rx: float, ry: float) -> bool:
    return ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1


def avatar_rgba(x: int, y: int, size: int) -> bytes:
    # Map pixel into 128×128 art space (avatar crop centered on head + claws).
    u = (x / size) * 80 + 24
    v = (y / size) * 72 + 16
    if in_ellipse(u, v, 64, 38, 26, 24):
        return bytes(CORAL + (255,))
    if 44 <= u <= 84 and 52 <= v <= 108:
        band = int((v - 52) // 11) % 2
        c = CORAL_D if band == 0 else CORAL
        return bytes(c + (255,))
    # Simple claw blobs lower sides
    if in_ellipse(u, v, 34, 92, 14, 18) or in_ellipse(u, v, 94, 92, 14, 18):
        return bytes(CORAL + (255,))
    if in_ellipse(u, v, 52, 36, 5, 4) or in_ellipse(u, v, 76, 36, 5, 4):
        return bytes(INK + (255,))
    return bytes((0, 0, 0, 0))


def icon_rgba(x: int, y: int, size: int) -> bytes:
    if not in_ellipse(x, y, size / 2, size / 2, size / 2, size / 2):
        return bytes(NAVY + (255,))
    # scale avatar into circle
    ax = int((x / size) * 128)
    ay = int((y / size) * 128)
    return avatar_rgba(ax, ay, 128)


def main() -> None:
    def fn_avatar(sz: int):
        return lambda x, y: avatar_rgba(x, y, sz)

    def fn_icon(sz: int):
        return lambda x, y: icon_rgba(x, y, sz)

    for w in (64, 128, 256):
        write_png(WWW / "images" / "brand" / f"mascot-{w}.png", w, w, fn_avatar(w))
    write_png(WWW / "favicon.png", 64, 64, fn_icon(64))
    for name, size in (
        ("icons/icon-192.png", 192),
        ("icons/icon-512.png", 512),
        ("icons/icon-maskable-192.png", 192),
        ("icons/icon-maskable-512.png", 512),
        ("icons/apple-touch-icon.png", 180),
    ):
        write_png(WWW / name, size, size, fn_icon(size))
    print("Wrote mascot PNG + PWA icons under wwwroot")


if __name__ == "__main__":
    main()
