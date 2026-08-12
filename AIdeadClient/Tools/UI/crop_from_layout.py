#!/usr/bin/env python3
"""Crop Parts from mockup.png using layout.json rects (top-left origin)."""

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
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def chroma_key(img: Image.Image, threshold: int = 245) -> Image.Image:
    """Make near-white / cream pixels transparent (simple placeholder cleanup)."""
    rgba = img.convert("RGBA")
    pixels = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if r >= threshold and g >= threshold and b >= (threshold - 25):
                pixels[x, y] = (r, g, b, 0)
    return rgba


def crop_node(
    mockup: Image.Image,
    node: dict,
    out_dir: Path,
    chroma: bool,
    overwrite: bool,
) -> Path | None:
    sprite = (node.get("sprite") or "").strip()
    if not sprite:
        return None
    rect = node.get("rect") or {}
    x = int(rect.get("x", 0))
    y = int(rect.get("y", 0))
    w = int(rect.get("w", 0))
    h = int(rect.get("h", 0))
    if w <= 0 or h <= 0:
        print(f"skip {node.get('id')}: invalid rect")
        return None

    mw, mh = mockup.size
    x0 = max(0, min(x, mw - 1))
    y0 = max(0, min(y, mh - 1))
    x1 = max(x0 + 1, min(x + w, mw))
    y1 = max(y0 + 1, min(y + h, mh))

    out_path = out_dir / sprite
    if out_path.exists() and not overwrite:
        print(f"keep existing {out_path}")
        return out_path

    crop = mockup.crop((x0, y0, x1, y1))
    if chroma:
        crop = chroma_key(crop)
    else:
        crop = crop.convert("RGBA")

    out_dir.mkdir(parents=True, exist_ok=True)
    crop.save(out_path)
    print(f"wrote {out_path} ({x1 - x0}x{y1 - y0})")
    return out_path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("layout", type=Path, help="Path to layout.json")
    parser.add_argument(
        "--mockup",
        type=Path,
        default=None,
        help="mockup.png (default: sibling of layout)",
    )
    parser.add_argument(
        "--parts",
        type=Path,
        default=None,
        help="Parts output dir (default: <screen>/Parts)",
    )
    parser.add_argument(
        "--chroma",
        action="store_true",
        help="Make near-white pixels transparent",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Overwrite existing Parts files",
    )
    parser.add_argument(
        "--scale",
        type=float,
        default=1.0,
        help="If mockup size != layout canvas, auto scale rects (1=auto detect)",
    )
    args = parser.parse_args()

    layout_path = args.layout.resolve()
    if not layout_path.is_file():
        print(f"layout not found: {layout_path}", file=sys.stderr)
        return 1

    data = load_layout(layout_path)
    screen_dir = layout_path.parent
    mockup_path = (args.mockup or (screen_dir / "mockup.png")).resolve()
    parts_dir = (args.parts or (screen_dir / "Parts")).resolve()

    if not mockup_path.is_file():
        print(f"mockup not found: {mockup_path}", file=sys.stderr)
        return 1

    mockup = Image.open(mockup_path).convert("RGBA")
    canvas = data.get("canvas") or {}
    cw = int(canvas.get("width") or mockup.width)
    ch = int(canvas.get("height") or mockup.height)

    sx = mockup.width / float(cw) if cw else 1.0
    sy = mockup.height / float(ch) if ch else 1.0
    if abs(sx - 1.0) > 0.01 or abs(sy - 1.0) > 0.01:
        print(f"scale rects by ({sx:.3f}, {sy:.3f}) to match mockup {mockup.size}")

    seen: set[str] = set()
    for node in data.get("nodes") or []:
        sprite = (node.get("sprite") or "").strip()
        if not sprite or sprite in seen:
            continue
        seen.add(sprite)
        rect = dict(node.get("rect") or {})
        if sx != 1.0 or sy != 1.0:
            rect = {
                "x": int(round(rect.get("x", 0) * sx)),
                "y": int(round(rect.get("y", 0) * sy)),
                "w": int(round(rect.get("w", 0) * sx)),
                "h": int(round(rect.get("h", 0) * sy)),
            }
            node = {**node, "rect": rect}
        crop_node(mockup, node, parts_dir, args.chroma, args.overwrite)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
