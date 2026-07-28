"""Embed PNG (or other binary) assets as base64 data URIs into a game JS/HTML file.

Usage:
  python embed_assets.py --target ads/tileAd1/game/main.js --assets-map ads/tileAd1/assets/embed_map.json
  python embed_assets.py --target path/to/file.js --res-dir path/to/pngs --keys start_bg=start_bg.png,chosen=chosen.png

embed_map.json format:
  { "start_bg": "start_bg.png", "chosen": "chosen.png" }

Looks for BASE64_IMAGES object entries like:
  'key': 'data:image/...', // optional comment
and upserts them. Inserts before 'arrow' key when missing (legacy tileAd1 anchor),
or before the closing }; of BASE64_IMAGES.
"""
from __future__ import annotations

import argparse
import base64
import json
import os
import re
import sys


def data_uri(path: str) -> str:
    ext = os.path.splitext(path)[1].lower()
    mime = {
        ".png": "image/png",
        ".jpg": "image/jpeg",
        ".jpeg": "image/jpeg",
        ".webp": "image/webp",
        ".gif": "image/gif",
    }.get(ext, "application/octet-stream")
    with open(path, "rb") as f:
        b64 = base64.b64encode(f.read()).decode("ascii")
    return "data:%s;base64,%s" % (mime, b64)


def load_map(args) -> dict:
    if args.assets_map:
        with open(args.assets_map, "r", encoding="utf-8") as f:
            return json.load(f)
    mapping = {}
    if args.keys:
        for part in args.keys.split(","):
            part = part.strip()
            if not part or "=" not in part:
                continue
            k, v = part.split("=", 1)
            mapping[k.strip()] = v.strip()
    return mapping


def upsert(text: str, key: str, uri: str, fname: str) -> str:
    entry = "            '%s': '%s', // %s\n" % (key, uri, fname)
    pattern = re.compile(
        r"^[ \t]*'%s'[ \t]*:[ \t]*'[^']*',?.*\r?\n" % re.escape(key),
        re.MULTILINE,
    )
    if pattern.search(text):
        return pattern.sub(entry, text, count=1), "updated"

    # Prefer legacy 'arrow' anchor used by tileAd1
    anchor = re.compile(r"^([ \t]*'arrow'[ \t]*:)", re.MULTILINE)
    m = anchor.search(text)
    if m:
        return text[: m.start()] + entry + text[m.start() :], "inserted"

    # Fallback: before closing of BASE64_IMAGES
    close = re.compile(
        r"(const\s+BASE64_IMAGES\s*=\s*\{[\s\S]*?)(^[ \t]*\};)",
        re.MULTILINE,
    )
    m2 = close.search(text)
    if m2:
        return text[: m2.end(1)] + entry + text[m2.start(2) :], "inserted"

    raise SystemExit("Cannot find BASE64_IMAGES insert point for key %s" % key)


def main() -> int:
    ap = argparse.ArgumentParser(description="Embed assets into BASE64_IMAGES")
    ap.add_argument("--target", required=True, help="JS/HTML file containing BASE64_IMAGES")
    ap.add_argument("--res-dir", default=None, help="Directory of asset files")
    ap.add_argument("--assets-map", default=None, help="JSON map key -> filename")
    ap.add_argument("--keys", default=None, help="key=file,key2=file2")
    args = ap.parse_args()

    mapping = load_map(args)
    if not mapping:
        print("No assets mapped.", file=sys.stderr)
        return 2

    res_dir = args.res_dir
    if not res_dir and args.assets_map:
        res_dir = os.path.dirname(os.path.abspath(args.assets_map))
    if not res_dir:
        res_dir = os.path.dirname(os.path.abspath(args.target))

    with open(args.target, "r", encoding="utf-8") as f:
        text = f.read()

    for key, fname in mapping.items():
        path = fname if os.path.isabs(fname) else os.path.join(res_dir, fname)
        if not os.path.isfile(path):
            raise SystemExit("Missing asset: %s" % path)
        uri = data_uri(path)
        text, action = upsert(text, key, uri, os.path.basename(path))
        print("%s  %s -> %s" % (action, key, path))

    with open(args.target, "w", encoding="utf-8") as f:
        f.write(text)
    print("done. size =", os.path.getsize(args.target))
    return 0


if __name__ == "__main__":
    sys.exit(main())
