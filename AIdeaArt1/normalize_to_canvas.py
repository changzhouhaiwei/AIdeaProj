"""
将任意比例的整图强制对齐为 ui整图rule 规定的 720×1556，便于与 商店2_layout.json 切图坐标一致。

生图模型往往无法精确输出像素，先用本脚本再切图。

用法:
  python normalize_to_canvas.py 输入.png 输出.png --mode cover
  python normalize_to_canvas.py 输入.png 输出.png --mode contain

- cover: 等比放大至完全覆盖画布，居中裁切（不拉伸，可能丢边缘）
- contain: 等比缩放进画布内，居中放置，余下透明边（不拉伸，不裁切）
"""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image

TARGET_W, TARGET_H = 720, 1556


def resize_cover(im: Image.Image, tw: int, th: int) -> Image.Image:
    im = im.convert("RGBA")
    sw, sh = im.size
    scale = max(tw / sw, th / sh)
    nw, nh = int(round(sw * scale)), int(round(sh * scale))
    resized = im.resize((nw, nh), Image.Resampling.LANCZOS)
    x0 = (nw - tw) // 2
    y0 = (nh - th) // 2
    return resized.crop((x0, y0, x0 + tw, y0 + th))


def resize_contain(im: Image.Image, tw: int, th: int) -> Image.Image:
    sw, sh = im.size
    scale = min(tw / sw, th / sh)
    nw, nh = int(round(sw * scale)), int(round(sh * scale))
    resized = im.resize((nw, nh), Image.Resampling.LANCZOS).convert("RGBA")
    canvas = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    x0 = (tw - nw) // 2
    y0 = (th - nh) // 2
    canvas.alpha_composite(resized, (x0, y0))
    return canvas


def main() -> None:
    ap = argparse.ArgumentParser(description="Normalize image to 720×1556")
    ap.add_argument("input", type=Path)
    ap.add_argument("output", type=Path)
    ap.add_argument(
        "--mode",
        choices=("cover", "contain"),
        default="cover",
        help="cover: fill canvas crop center; contain: fit inside transparent pad",
    )
    args = ap.parse_args()

    im = Image.open(args.input).convert("RGBA")
    if im.size == (TARGET_W, TARGET_H):
        out = im
    elif args.mode == "cover":
        out = resize_cover(im, TARGET_W, TARGET_H)
    else:
        out = resize_contain(im, TARGET_W, TARGET_H)

    assert out.size == (TARGET_W, TARGET_H), out.size
    args.output.parent.mkdir(parents=True, exist_ok=True)
    out.save(args.output, "PNG")
    print(f"OK {args.input.name} {im.size} -> {args.output} {out.size}")


if __name__ == "__main__":
    main()
