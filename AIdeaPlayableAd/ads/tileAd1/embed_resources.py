"""Thin wrapper: embed ads/tileAd1 assets into game/main.js via framework tool."""
from __future__ import annotations

import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
TOOL = os.path.join(ROOT, "framework", "build", "embed_assets.py")


def main() -> int:
    cmd = [
        sys.executable,
        TOOL,
        "--target",
        os.path.join(HERE, "game", "main.js"),
        "--res-dir",
        os.path.join(HERE, "assets"),
        "--assets-map",
        os.path.join(HERE, "assets", "embed_map.json"),
    ]
    print(" ".join(cmd))
    return subprocess.call(cmd)


if __name__ == "__main__":
    raise SystemExit(main())
