#!/usr/bin/env python3
"""Chroma-key UI plates to alpha. Supports green / magenta / red / blue / cyan / auto."""

from __future__ import annotations

import argparse
import math
import sys
from collections import Counter
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow required: pip install Pillow", file=sys.stderr)
    sys.exit(1)

KEY_RGB = {
    "green": (0, 255, 0),
    "magenta": (255, 0, 255),
    "red": (255, 0, 0),
    "blue": (0, 0, 255),
    "cyan": (0, 255, 255),
}


def dist_rgb(r: int, g: int, b: int, tr: int, tg: int, tb: int) -> float:
    dr, dg, db = r - tr, g - tg, b - tb
    return math.sqrt(dr * dr + dg * dg + db * db)


def sample_corner_key(im: Image.Image, inset: int = 4, box: int = 24) -> tuple[int, int, int]:
    """Majority color from four corner patches → chroma key color."""
    rgba = im.convert("RGBA")
    w, h = rgba.size
    samples: list[tuple[int, int, int]] = []
    corners = [
        (inset, inset),
        (w - inset - box, inset),
        (inset, h - inset - box),
        (w - inset - box, h - inset - box),
    ]
    for cx, cy in corners:
        for y in range(max(0, cy), min(h, cy + box)):
            for x in range(max(0, cx), min(w, cx + box)):
                r, g, b, a = rgba.getpixel((x, y))
                if a < 8:
                    continue
                # quantize to reduce noise
                samples.append((r // 8 * 8, g // 8 * 8, b // 8 * 8))
    if not samples:
        return KEY_RGB["green"]
    return Counter(samples).most_common(1)[0][0]


def classify_key_name(rgb: tuple[int, int, int]) -> str:
    r, g, b = rgb
    # saturated channel dominance
    if g >= 140 and g >= r + 35 and g >= b + 35:
        return "green"
    if r >= 140 and b >= 140 and r >= g + 30 and b >= g + 30:
        return "magenta"
    if r >= 150 and r >= g + 40 and r >= b + 40:
        return "red"
    if b >= 150 and b >= r + 40 and b >= g + 40:
        return "blue"
    if g >= 140 and b >= 140 and g >= r + 30 and b >= r + 30:
        return "cyan"
    # fallback: nearest named key
    best, best_d = "green", 1e9
    for name, (tr, tg, tb) in KEY_RGB.items():
        d = dist_rgb(r, g, b, tr, tg, tb)
        if d < best_d:
            best, best_d = name, d
    return best


def is_key_pixel(r: int, g: int, b: int, key: str, tr: int, tg: int, tb: int) -> bool:
    if key == "green":
        return g >= 100 and g >= r + 25 and g >= b + 25
    if key == "magenta":
        return r >= 100 and b >= 100 and r >= g + 25 and b >= g + 25
    if key == "red":
        return r >= 120 and r >= g + 35 and r >= b + 35
    if key == "blue":
        return b >= 120 and b >= r + 35 and b >= g + 35
    if key == "cyan":
        return g >= 100 and b >= 100 and g >= r + 25 and b >= r + 25
    # custom / auto sampled: distance + not too gray
    sat = max(r, g, b) - min(r, g, b)
    return sat >= 25 and dist_rgb(r, g, b, tr, tg, tb) <= 90


def despill(r: int, g: int, b: int, key: str) -> tuple[int, int, int]:
    if key == "green":
        return (r, min(g, max(r, b) + 18), b)
    if key == "magenta":
        m = min(r, b)
        return (min(r, m + 18), g, min(b, m + 18))
    if key == "red":
        return (min(r, max(g, b) + 18), g, b)
    if key == "blue":
        return (r, g, min(b, max(r, g) + 18))
    if key == "cyan":
        return (r, min(g, r + 30), min(b, r + 30))
    return (r, g, b)


def chroma_key(
    im: Image.Image,
    key: str = "auto",
    max_dist: float = 140.0,
    soft: float = 40.0,
) -> tuple[Image.Image, str, tuple[int, int, int]]:
    if key == "auto":
        sampled = sample_corner_key(im)
        key_name = classify_key_name(sampled)
        tr, tg, tb = sampled
        # If classified named key, prefer pure target for distance
        if key_name in KEY_RGB and key_name != "custom":
            # keep sampled for parchment/odd screens; use named for saturated screens
            if key_name in ("green", "magenta", "red", "blue", "cyan"):
                # blend: use sampled if close to named, else sampled as custom
                nr, ng, nb = KEY_RGB[key_name]
                if dist_rgb(sampled[0], sampled[1], sampled[2], nr, ng, nb) < 120:
                    tr, tg, tb = nr, ng, nb
                else:
                    key_name = "custom"
                    tr, tg, tb = sampled
    elif key == "custom":
        tr, tg, tb = sample_corner_key(im)
        key_name = "custom"
    else:
        key_name = key
        tr, tg, tb = KEY_RGB[key]

    rgba = im.convert("RGBA")
    px = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            d = dist_rgb(r, g, b, tr, tg, tb)
            keyed = is_key_pixel(r, g, b, key_name if key_name != "custom" else "green", tr, tg, tb)
            if key_name == "custom":
                sat = max(r, g, b) - min(r, g, b)
                keyed = sat >= 20 and d <= max_dist
            if keyed and d <= max_dist:
                px[x, y] = (0, 0, 0, 0)
            elif keyed and d <= max_dist + soft:
                t = (d - max_dist) / soft
                alpha = int(max(0, min(255, a * t)))
                rr, gg, bb = despill(r, g, b, key_name if key_name in KEY_RGB else "green")
                px[x, y] = (rr, gg, bb, alpha)
            elif keyed:
                rr, gg, bb = despill(r, g, b, key_name if key_name in KEY_RGB else "green")
                px[x, y] = (rr, gg, bb, a)
    return rgba, key_name, (tr, tg, tb)


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
    keyed, key_name, rgb = chroma_key(src, key=key, max_dist=max_dist)
    if crop:
        keyed = tight_crop(keyed)
    out.parent.mkdir(parents=True, exist_ok=True)
    keyed.save(out)
    n = keyed.width * keyed.height
    a0 = sum(1 for r, g, b, a in keyed.getdata() if a < 10)
    print(
        f"{path.name} [key={key_name} rgb={rgb}] -> {out.name}: "
        f"{src.size}->{keyed.size} transparent={a0 / max(n, 1):.1%}"
    )


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("input", type=Path, help="PNG file or directory")
    ap.add_argument("--out", type=Path, default=None)
    ap.add_argument("--max-dist", type=float, default=140.0)
    ap.add_argument("--no-crop", action="store_true")
    ap.add_argument(
        "--key",
        choices=("auto", "green", "magenta", "red", "blue", "cyan", "custom"),
        default="auto",
        help="auto=detect from corner samples; magenta when subject is green",
    )
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
