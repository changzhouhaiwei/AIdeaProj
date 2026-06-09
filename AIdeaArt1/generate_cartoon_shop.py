"""
全新程序化卡通商店 UI（720×1556）+ 整齐切图。

- 不依赖 商店2.png；风格统一（圆角、描边、渐变块）。
- 切图命名：两位序号 + 英文语义，避免重复子块与混乱重叠。
- 每张切图 RGBA；控件类除底板外尽量只在有效像素留不透明区（按钮/小图可带透明边距）。

用法：python generate_cartoon_shop.py
输出：商店卡通/shop_full.png、商店卡通/slices/*.png、商店卡通/layout.json
"""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "商店卡通"


W, H = 720, 1556
M = 24  # horizontal margin
CARD_W = W - 2 * M
R_CARD = 22
R_BTN = 18
R_PILL = 20
HEADER_H = 268
SCALLOP_R = 14
CONTENT_Y = 300


@dataclass(frozen=True)
class Rect:
    x: int
    y: int
    w: int
    h: int

    def box(self) -> tuple[int, int, int, int]:
        return (self.x, self.y, self.x + self.w, self.y + self.h)

    def as_dict(self) -> dict:
        return {"x": self.x, "y": self.y, "w": self.w, "h": self.h}


# —— 配色（统一卡通调性）——
C_BG = (26, 36, 68, 255)
C_STRIPE_A = (110, 88, 210, 255)
C_STRIPE_B = (72, 118, 220, 255)
C_SCALLOP = (140, 190, 255, 255)
C_CREAM = (252, 244, 228, 255)
C_CREAM_DARK = (235, 220, 198, 255)
C_PURPLE_TOP = (118, 86, 220, 255)
C_PURPLE_TOP2 = (88, 62, 190, 255)
C_GREEN = (58, 214, 120, 255)
C_GREEN_DARK = (36, 160, 88, 255)
C_TEXT = (58, 42, 34, 255)
C_TEXT_MUTE = (110, 96, 88, 255)
C_BACK_BTN = (64, 132, 255, 255)
C_PILL_BG = (32, 38, 62, 255)
C_GOLD = (255, 210, 80, 255)
C_AD_RED = (230, 70, 70, 255)


def load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        r"C:\Windows\Fonts\arialbd.ttf",
        r"C:\Windows\Fonts\segoeuib.ttf",
        r"C:\Windows\Fonts\msyhbd.ttc",
    ]
    for p in candidates:
        try:
            return ImageFont.truetype(p, size)
        except OSError:
            continue
    return ImageFont.load_default()


def new_rgba(size: tuple[int, int], color: tuple[int, int, int, int] = (0, 0, 0, 0)) -> Image.Image:
    im = Image.new("RGBA", size, color)
    return im


def rounded_rect(
    im: Image.Image,
    xy: tuple[int, int, int, int],
    r: int,
    fill,
    outline=None,
    width: int = 1,
) -> None:
    d = ImageDraw.Draw(im)
    d.rounded_rectangle(xy, radius=r, fill=fill, outline=outline, width=width)


def draw_vertical_stripes(im: Image.Image, rect: Rect, stripe: int = 36) -> None:
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = rect.box()
    i = 0
    x = x0
    while x < x1:
        col = C_STRIPE_A if i % 2 == 0 else C_STRIPE_B
        d.rectangle([x, y0, min(x + stripe, x1), y1], fill=col)
        x += stripe
        i += 1


def draw_scallop_edge(im: Image.Image, y_base: int) -> None:
    """在 y_base 处画一排向下半圆扇贝（浅蓝），与顶栏底衔接。"""
    d = ImageDraw.Draw(im)
    x = 0
    while x < W:
        cx = x + SCALLOP_R
        cy = y_base
        d.ellipse(
            [cx - SCALLOP_R, cy - SCALLOP_R, cx + SCALLOP_R, cy + SCALLOP_R],
            fill=C_SCALLOP,
        )
        x += SCALLOP_R * 2


def draw_back_button(im: Image.Image, rect: Rect) -> None:
    cx = rect.x + rect.w // 2
    cy = rect.y + rect.h // 2
    r = min(rect.w, rect.h) // 2 - 2
    d = ImageDraw.Draw(im)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=C_BACK_BTN, outline=(255, 255, 255, 200), width=3)
    # 简笔箭头
    pts = [(cx + 6, cy - 10), (cx - 10, cy), (cx + 6, cy + 10)]
    d.line(pts, fill=(255, 255, 255, 255), width=4)


def draw_title_shop(im: Image.Image, rect: Rect, font: ImageFont.ImageFont) -> None:
    d = ImageDraw.Draw(im)
    cx = rect.x + rect.w // 2
    cy = rect.y + rect.h // 2
    d.text((cx, cy), "SHOP", font=font, fill=(255, 255, 255, 255), anchor="mm", stroke_width=3, stroke_fill=(40, 40, 80, 255))


def draw_hud_pill(
    im: Image.Image,
    rect: Rect,
    label: str,
    icon_draw,
) -> None:
    rounded_rect(im, rect.box(), R_PILL, C_PILL_BG, (255, 255, 255, 90), 2)
    ix = rect.x + 18
    iy = rect.y + rect.h // 2
    icon_draw(im, ix, iy)
    d = ImageDraw.Draw(im)
    d.text(
        (ix + 34, iy),
        label,
        font=load_font(22),
        fill=(255, 255, 255, 255),
        anchor="lm",
    )


def icon_coin(im: Image.Image, cx: int, cy: int) -> None:
    d = ImageDraw.Draw(im)
    d.ellipse([cx - 12, cy - 12, cx + 12, cy + 12], fill=C_GOLD, outline=(200, 140, 40, 255), width=2)
    d.text((cx, cy), "★", font=load_font(12), fill=(180, 100, 20, 255), anchor="mm")


def icon_wand(im: Image.Image, cx: int, cy: int) -> None:
    d = ImageDraw.Draw(im)
    d.line([cx - 10, cy + 10, cx + 10, cy - 10], fill=(200, 160, 255, 255), width=4)
    d.text((cx + 8, cy - 8), "✦", font=load_font(12), fill=(255, 230, 120, 255), anchor="mm")


def icon_shuffle(im: Image.Image, cx: int, cy: int) -> None:
    d = ImageDraw.Draw(im)
    d.arc([cx - 12, cy - 12, cx + 4, cy + 4], 200, 420, fill=(120, 230, 160, 255), width=3)
    d.arc([cx - 4, cy - 4, cx + 12, cy + 12], 20, 240, fill=(230, 210, 90, 255), width=3)


def icon_undo(im: Image.Image, cx: int, cy: int) -> None:
    d = ImageDraw.Draw(im)
    d.arc([cx - 12, cy - 12, cx + 12, cy + 12], 60, 300, fill=(255, 190, 100, 255), width=4)


def draw_price_button(im: Image.Image, rect: Rect, price: str, font: ImageFont.ImageFont) -> None:
    rounded_rect(im, rect.box(), R_BTN, C_GREEN, C_GREEN_DARK, 2)
    d = ImageDraw.Draw(im)
    cx = rect.x + rect.w // 2
    cy = rect.y + rect.h // 2
    d.text((cx, cy), price, font=font, fill=(255, 255, 255, 255), anchor="mm", stroke_width=2, stroke_fill=(20, 90, 50, 255))


def draw_bundle_card(
    im: Image.Image,
    rect: Rect,
    title: str,
    price: str,
    accent_gift: tuple[int, int, int, int],
    font_title: ImageFont.ImageFont,
    font_price: ImageFont.ImageFont,
    font_small: ImageFont.ImageFont,
    reward_a: str = "×6800",
    reward_b: str = "×12",
) -> None:
    x, y, w, h = rect.x, rect.y, rect.w, rect.h
    rounded_rect(im, rect.box(), R_CARD, C_CREAM, C_CREAM_DARK, 2)
    top_h = int(h * 0.62)
    inner = (x + 6, y + 6, x + w - 6, y + top_h)
    iw = w - 12
    d = ImageDraw.Draw(im)
    d.rounded_rectangle(inner, radius=R_CARD - 4, fill=C_PURPLE_TOP, outline=None)
    # 简易渐变叠层
    overlay = new_rgba((iw, top_h), (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    for i in range(top_h):
        a = int(40 * (1 - i / top_h))
        od.line([(0, i), (iw, i)], fill=(40, 20, 90, a))
    im.alpha_composite(overlay, (x + 6, y + 6))
    # 礼物块
    gx, gy = x + 28, y + 28
    rounded_rect(im, (gx, gy, gx + 96, gy + 96), 12, accent_gift, (255, 220, 120, 255), 2)
    d = ImageDraw.Draw(im)
    d.line([(gx + 48, gy + 10), (gx + 48, gy + 86)], fill=(255, 230, 140, 255), width=5)
    d.line([(gx + 10, gy + 48), (gx + 86, gy + 48)], fill=(255, 230, 140, 255), width=5)
    # 右侧奖励格
    rx0 = x + w - 240
    ry = y + 18
    for row in range(2):
        for col in range(4):
            bx = rx0 + col * 52
            by = ry + row * 44
            rounded_rect(im, (bx, by, bx + 46, by + 38), 8, (255, 255, 255, 35), (255, 255, 255, 60), 1)
    d.text((rx0 + 8, ry + 4), reward_a, font=font_small, fill=(255, 255, 255, 255), anchor="lt")
    d.text((rx0 + 8, ry + 48), reward_b, font=font_small, fill=(255, 255, 255, 255), anchor="lt")
    # 下条
    bar_y = y + top_h + 8
    d.text((x + 20, bar_y + 8), title, font=font_title, fill=C_TEXT, anchor="lt")
    pr = Rect(x=x + w - 168, y=bar_y + 4, w=148, h=46)
    draw_price_button(im, pr, price, font_price)


def draw_remove_ads_card(im: Image.Image, rect: Rect, font_title: ImageFont.ImageFont, font_price: ImageFont.ImageFont) -> None:
    rounded_rect(im, rect.box(), R_CARD, C_CREAM, C_CREAM_DARK, 2)
    d = ImageDraw.Draw(im)
    cx = rect.x + 56
    cy = rect.y + rect.h // 2
    d.ellipse([cx - 28, cy - 28, cx + 28, cy + 28], fill=(255, 90, 90, 255), outline=(140, 30, 30, 255), width=3)
    d.line([(cx - 16, cy - 16), (cx + 16, cy + 16)], fill=(255, 255, 255, 255), width=4)
    d.text((cx, cy - 2), "ADS", font=load_font(16), fill=(255, 255, 255, 255), anchor="mm")
    d.text((rect.x + 100, rect.y + 22), "Remove ADS", font=font_title, fill=C_TEXT, anchor="lt")
    d.text((rect.x + 100, rect.y + 54), "No Ads  +300 coins", font=load_font(18), fill=C_TEXT_MUTE, anchor="lt")
    pr = Rect(x=rect.x + rect.w - 168, y=rect.y + rect.h // 2 - 23, w=148, h=46)
    draw_price_button(im, pr, "$9.99", font_price)


def draw_coin_pack_card(
    im: Image.Image,
    rect: Rect,
    amount: str,
    price: str,
    font_big: ImageFont.ImageFont,
    font_price: ImageFont.ImageFont,
) -> None:
    rounded_rect(im, rect.box(), R_CARD, C_CREAM, C_CREAM_DARK, 2)
    d = ImageDraw.Draw(im)
    cx = rect.x + 56
    cy = rect.y + rect.h // 2
    d.ellipse([cx - 26, cy - 22, cx + 26, cy + 22], fill=C_GOLD, outline=(200, 140, 40, 255), width=2)
    d.ellipse([cx - 8, cy - 12, cx + 18, cy + 8], fill=(255, 230, 140, 255), outline=(200, 140, 40, 255), width=2)
    d.text((rect.x + 110, cy), amount, font=font_big, fill=C_TEXT, anchor="lm")
    pr = Rect(x=rect.x + rect.w - 168, y=rect.y + rect.h // 2 - 23, w=148, h=46)
    draw_price_button(im, pr, price, font_price)


def build_master() -> tuple[Image.Image, dict[str, Rect]]:
    """合成整图并返回各导出块在整图中的矩形（用于切图）。"""
    slices: dict[str, Rect] = {}
    full = new_rgba((W, H), (0, 0, 0, 0))

    # 主背景
    slices["01_bg_main"] = Rect(0, HEADER_H, W, H - HEADER_H)
    d = ImageDraw.Draw(full)
    d.rectangle([0, HEADER_H, W, H], fill=C_BG)

    # 顶栏条
    slices["02_header_stripes"] = Rect(0, 0, W, HEADER_H)
    hdr = new_rgba((W, HEADER_H), (0, 0, 0, 0))
    draw_vertical_stripes(hdr, slices["02_header_stripes"])
    full.paste(hdr, (0, 0), hdr)

    # 扇贝（单独一层便于替换）
    scallop_y = HEADER_H
    slices["03_header_scallop"] = Rect(0, scallop_y - SCALLOP_R, W, SCALLOP_R * 2)
    sc = new_rgba((W, SCALLOP_R * 2), (0, 0, 0, 0))
    draw_scallop_edge(sc, SCALLOP_R)
    full.alpha_composite(sc, (0, scallop_y - SCALLOP_R))

    # 返回
    slices["04_btn_back"] = Rect(20, 56, 72, 72)
    draw_back_button(full, slices["04_btn_back"])

    # 标题
    slices["05_title_shop"] = Rect(200, 58, 320, 72)
    draw_title_shop(full, slices["05_title_shop"], load_font(44))

    # HUD 四连
    pill_w = 156
    pill_h = 44
    gap = 12
    px0 = 28
    py = 168
    hud_specs = [
        ("06_hud_pill_coins", "3234", icon_coin),
        ("07_hud_pill_wands", "4", icon_wand),
        ("08_hud_pill_shuffle", "4", icon_shuffle),
        ("09_hud_pill_undo", "4", icon_undo),
    ]
    for i, (name, lab, icon) in enumerate(hud_specs):
        r = Rect(px0 + i * (pill_w + gap), py, pill_w, pill_h)
        slices[name] = r
        draw_hud_pill(full, r, lab, icon)

    font_title = load_font(22)
    font_price = load_font(22)
    font_bundle_title = load_font(24)
    font_small = load_font(18)
    font_coin = load_font(28)

    y = CONTENT_Y
    gap = 16

    r1 = Rect(M, y, CARD_W, 240)
    slices["10_card_bundle_brilliance"] = r1
    draw_bundle_card(
        full,
        r1,
        "Brilliance Bundle",
        "$9.99",
        (220, 70, 90, 255),
        font_bundle_title,
        font_price,
        font_small,
        "×6800",
        "×12",
    )
    y += 240 + gap

    r2 = Rect(M, y, CARD_W, 240)
    slices["11_card_bundle_ultimate"] = r2
    draw_bundle_card(
        full,
        r2,
        "Ultimate Bundle",
        "$9.99",
        (150, 90, 220, 255),
        font_bundle_title,
        font_price,
        font_small,
        "×12000",
        "×20",
    )
    y += 240 + gap

    r3 = Rect(M, y, CARD_W, 128)
    slices["12_card_remove_ads"] = r3
    draw_remove_ads_card(full, r3, font_title, font_price)
    y += 128 + gap

    r4 = Rect(M, y, CARD_W, 108)
    slices["13_card_coins_240"] = r4
    draw_coin_pack_card(full, r4, "×240", "$1.99", font_coin, font_price)
    y += 108 + gap

    r5 = Rect(M, y, CARD_W, 108)
    slices["14_card_coins_720"] = r5
    draw_coin_pack_card(full, r5, "×720", "$1.99", font_coin, font_price)

    slices["15_button_price_green"] = Rect(
        M + CARD_W - 168, r5.y + r5.h // 2 - 23, 148, 46
    )
    return full, slices


def export_slice(master: Image.Image, key: str, rect: Rect, out_dir: Path) -> None:
    box = rect.box()
    tile = master.crop(box)
    if tile.mode != "RGBA":
        tile = tile.convert("RGBA")
    tile.save(out_dir / f"{key}.png", "PNG")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    sdir = OUT / "slices"
    sdir.mkdir(parents=True, exist_ok=True)

    master, rects = build_master()
    assert master.size == (W, H), master.size
    master.save(OUT / "shop_full.png", "PNG")

    layout = {
        "title": "卡通商店 UI 布局（程序化生成）",
        "canvas": {"width": W, "height": H},
        "slices": {k: v.as_dict() for k, v in rects.items()},
        "notes": [
            "切图与 shop_full 像素对齐；命名按功能排序。",
            "15_button_price_green 为独立绘制的按钮模板，可与卡片内按钮互换。",
        ],
    }
    (OUT / "layout.json").write_text(json.dumps(layout, ensure_ascii=False, indent=2), encoding="utf-8")

    for k, r in rects.items():
        export_slice(master, k, r, sdir)

    manifest = {
        "full": "shop_full.png",
        "sliceCount": len(list(sdir.glob("*.png"))),
        "files": sorted(p.name for p in sdir.glob("*.png")),
    }
    (OUT / "slices" / "_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"OK -> {OUT} ({manifest['sliceCount']} slices)")


if __name__ == "__main__":
    main()
