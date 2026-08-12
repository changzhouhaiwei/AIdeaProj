#!/usr/bin/env python3
"""Resize Parts PNGs to layout.json rect sizes so UI SetNativeSize matches design."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow required: pip install Pillow", file=sys.stderr)
    sys.exit(1)


def load_layout(path: Path) -> dict:
    text = path.read_text(encoding="utf-8-sig")
    return json.loads(text)


def collect_sprite_sizes(layout: dict) -> dict[str, tuple[int, int]]:
    """sprite -> (w, h). If reused, keep the largest area (covers toggle shared by 4 buttons)."""
    sizes: dict[str, tuple[int, int]] = {}
    for node in layout.get("nodes") or []:
        sprite = (node.get("sprite") or "").strip()
        if not sprite:
            continue
        rect = node.get("rect") or {}
        w = int(round(float(rect.get("w") or 0)))
        h = int(round(float(rect.get("h") or 0)))
        if w <= 0 or h <= 0:
            continue
        prev = sizes.get(sprite)
        if prev is None or w * h > prev[0] * prev[1]:
            sizes[sprite] = (w, h)
    return sizes


def fit_contain(im: Image.Image, tw: int, th: int) -> Image.Image:
    """Scale preserving aspect into tw x th, centered on transparent canvas."""
    im = im.convert("RGBA")
    src_w, src_h = im.size
    if src_w <= 0 or src_h <= 0:
        return Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    scale = min(tw / src_w, th / src_h)
    nw = max(1, int(round(src_w * scale)))
    nh = max(1, int(round(src_h * scale)))
    scaled = im.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    ox = (tw - nw) // 2
    oy = (th - nh) // 2
    canvas.paste(scaled, (ox, oy), scaled)
    return canvas


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("layout", type=Path, help="layout.json")
    ap.add_argument(
        "--parts",
        type=Path,
        default=None,
        help="Parts dir (default: sibling Parts/)",
    )
    ap.add_argument(
        "--mode",
        choices=("contain", "stretch"),
        default="contain",
        help="contain=keep aspect + pad; stretch=exact force size",
    )
    args = ap.parse_args()

    layout_path = args.layout.resolve()
    parts_dir = (args.parts or (layout_path.parent / "Parts")).resolve()
    layout = load_layout(layout_path)
    sizes = collect_sprite_sizes(layout)
    if not sizes:
        print("no sprites in layout", file=sys.stderr)
        return 1

    for sprite, (tw, th) in sorted(sizes.items()):
        src = parts_dir / sprite
        if not src.is_file():
            print(f"missing {src}")
            continue
        im = Image.open(src).convert("RGBA")
        if args.mode == "stretch":
            out = im.resize((tw, th), Image.Resampling.LANCZOS)
        else:
            out = fit_contain(im, tw, th)
        out.save(src)
        print(f"{sprite}: {im.size} -> {out.size}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
