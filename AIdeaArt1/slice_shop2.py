"""
从 商店2.png 按 商店2_layout.json 导出切图（RGBA PNG）。
用法：在 AIdeaArt1 目录执行  python slice_shop2.py
"""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parent
LAYOUT = ROOT / "商店2_layout.json"
SOURCE = ROOT / "商店2.png"
OUT_DIR = ROOT / "商店2_切图"


def clamp_rect(
    im_w: int, im_h: int, x: int, y: int, w: int, h: int
) -> tuple[int, int, int, int]:
    x = max(0, min(x, im_w - 1))
    y = max(0, min(y, im_h - 1))
    w = max(1, min(w, im_w - x))
    h = max(1, min(h, im_h - y))
    return x, y, w, h


def crop_save(
    im: Image.Image, name: str, rect: dict, written: dict[tuple, str]
) -> None:
    x, y, w, h = rect["x"], rect["y"], rect["w"], rect["h"]
    x, y, w, h = clamp_rect(im.width, im.height, x, y, w, h)
    key = (x, y, w, h)
    # 完全相同矩形只保留一份文件（如各卡同款价格按钮）
    if key in written:
        return
    written[key] = name
    box = (x, y, x + w, y + h)
    tile = im.crop(box)
    if tile.mode != "RGBA":
        tile = tile.convert("RGBA")
    safe = "".join(c if c.isalnum() or c in "-_" else "_" for c in name)
    out = OUT_DIR / f"{safe}.png"
    tile.save(out, "PNG")


def main() -> None:
    data = json.loads(LAYOUT.read_text(encoding="utf-8"))
    im = Image.open(SOURCE).convert("RGBA")
    if im.size != (720, 1556):
        raise SystemExit(f"Unexpected source size {im.size}, expected (720, 1556)")

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    written: dict[tuple, str] = {}
    regions = data["regions"]

    for key, val in regions.items():
        if key == "common_price_button_green":
            continue
        if key == "currency_pill":
            fo = val["first_origin"]
            tw, th = val["rect_template"]["w"], val["rect_template"]["h"]
            gap = val["gap"]
            order = val["order"]
            for i, label in enumerate(order):
                x = fo["x"] + i * (tw + gap)
                y = fo["y"]
                crop_save(
                    im,
                    f"{key}_{label}",
                    {"x": x, "y": y, "w": tw, "h": th},
                    written,
                )
            continue
        if "rect" in val:
            crop_save(im, key, val["rect"], written)
        subs = val.get("subparts")
        if isinstance(subs, dict):
            for sk, sr in subs.items():
                crop_save(im, f"{key}__{sk}", sr, written)

    # 布局中的通用绿按钮：复用 Brilliance 卡上的价格按钮切片
    ref = regions.get("card_brilliance_bundle", {}).get("subparts", {}).get("price_btn")
    if ref:
        crop_save(im, "price_button_green", ref, {})

    manifest = {
        "source": SOURCE.name,
        "layout": LAYOUT.name,
        "canvas": [im.width, im.height],
        "outputDir": OUT_DIR.name,
        "files": sorted(p.name for p in OUT_DIR.glob("*.png")),
    }
    (OUT_DIR / "_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"Exported {len(manifest['files'])} PNGs -> {OUT_DIR}")


if __name__ == "__main__":
    main()
