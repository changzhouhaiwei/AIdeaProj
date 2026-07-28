"""DEPRECATED path: tileAd1 has moved under ads/tileAd1.

Delegates to ads/tileAd1/embed_resources.py.
"""
from __future__ import annotations

import os
import runpy
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
TARGET = os.path.abspath(os.path.join(HERE, "..", "ads", "tileAd1", "embed_resources.py"))


def main() -> int:
    if not os.path.isfile(TARGET):
        print("Missing migrated script:", TARGET, file=sys.stderr)
        print("Use: python ads/tileAd1/embed_resources.py", file=sys.stderr)
        return 2
    print("Redirect ->", TARGET)
    sys.path.insert(0, os.path.dirname(TARGET))
    runpy.run_path(TARGET, run_name="__main__")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
