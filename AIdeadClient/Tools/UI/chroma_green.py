#!/usr/bin/env python3
"""Chroma-key pure green screen (#00FF00-ish) to alpha, then tight crop."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow required: pip install Pillow", file=sys.stderr)
    sys.exit(1)


def dist_rgb(r: int, g: int, b: int, tr: int, tg: int, tb: int) -> float:
    dr, dg, db = r - tr, g - tg, b - tb
    return math.sqrt(dr * dr + dg * dg + db * db)


def chroma_key(
    im: Image.Image,
    key: str = "green",
    max_dist: float = 140.0,
    soft: float = 40.0,
) -> Image.Image:
    """key: green (#00FF00) or magenta (#FF00FF). Magenta used when asset itself is green."""
    if key == "magenta":
        tr, tg, tb = 255, 0, 255

        def is_key(r: int, g: int, b: int) -> bool:
            return r >= 120 and b >= 120 and r >= g + 30 and b >= g + 30

        def despill(r: int, g: int, b: int) -> tuple[int, int, int]:
            # pull magenta fringe toward neutral
            m = min(r, b)
            return (min(r, m + 20), g, min(b, m + 20))
    else:
        tr, tg, tb = 0, 255, 0

        def is_key(r: int, g: int, b: int) -> bool:
            return g >= 120 and g >= r + 30 and g >= b + 30

        def despill(r: int, g: int, b: int) -> tuple[int, int, int]:
            return (r, min(g, max(r, b) + 20), b)

    rgba = im.convert("RGBA")
    px = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            d = dist_rgb(r, g, b, tr, tg, tb)
            keyed = is_key(r, g, b)
            if keyed and d <= max_dist:
                px[x, y] = (0, 0, 0, 0)
            elif keyed and d <= max_dist + soft:
                t = (d - max_dist) / soft
                alpha = int(max(0, min(255, a * t)))
                rr, gg, bb = despill(r, g, b)
                px[x, y] = (rr, gg, bb, alpha)
            elif keyed:
                rr, gg, bb = despill(r, g, b)
                px[x, y] = (rr, gg, bb, a)
    return rgba


def tight_crop(im: Image.Image, pad: int = 2) -> Image.Image:
    bbox = im.split()[-1].getbbox()
    if not bbox:
        return im
    l, t, r, b = bbox
    l = max(0, l - pad)
    t = max(0, t - pad)
    r = min(im.width, r + pad)
    b = min(im.height, b + pad)
    return im.crop((l, t, r, b))


def process(path: Path, out: Path, max_dist: float, crop: bool, key: str) -> None:
    src = Image.open(path)
    keyed = chroma_key(src, key=key, max_dist=max_dist)
    if crop:
        keyed = tight_crop(keyed)
    out.parent.mkdir(parents=True, exist_ok=True)
    keyed.save(out)
    n = keyed.width * keyed.height
    a0 = sum(1 for p in keyed.getdata() if p[3] < 10)
    print(f"{path.name} [{key}] -> {out.name}: {src.size}->{keyed.size} transparent={a0/max(n,1):.1%}")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("input", type=Path, help="PNG file or directory (raw chroma-screen)")
    ap.add_argument("--out", type=Path, default=None, help="Output file/dir (default overwrite)")
    ap.add_argument("--max-dist", type=float, default=140.0)
    ap.add_argument("--no-crop", action="store_true")
    ap.add_argument("--key", choices=("green", "magenta"), default="green")
    args = ap.parse_args()

    src = args.input.resolve()
    if src.is_dir():
        out_dir = (args.out or src).resolve()
        for p in sorted(src.glob("*.png")):
            if p.name.startswith("_"):
                continue
            process(p, out_dir / p.name, args.max_dist, crop=not args.no_crop, key=args.key)
    else:
        out = (args.out or src).resolve()
        process(src, out, args.max_dist, crop=not args.no_crop, key=args.key)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
